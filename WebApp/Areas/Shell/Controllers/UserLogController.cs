using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Globalization;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class UserLogController : Controller
    {
        //
        // GET: /Shell/UserLog/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetDataLogUser(string[] UserName, string[] RigName, string[] WellName, DateTime? PeriodStart = null, DateTime? PeriodFinish = null, string GroupedBy = "")
        {
            try
            {
                var Data = new List<UserLogData>();
                if ((GroupedBy == "WellName") || (GroupedBy == "RigName"))
                {
                    var qWAs = new List<IMongoQuery>();
                    if (RigName != null)
                        if (RigName.Length > 0)
                            qWAs.Add(Query.In("RigName", new BsonArray(RigName)));
                    if (WellName != null)
                        if (WellName.Length > 0)
                            qWAs.Add(Query.In("WellName", new BsonArray(WellName)));

                    var qWA = qWAs.Count > 0 ? Query.And(qWAs) : Query.Null;
                    var WA = WellActivity.Populate<WellActivity>(qWA)
                                .Select(x => new
                                {
                                    WellName = x.WellName,
                                    RigName = x.RigName
                                }).ToList();

                    var UserLogDatas = new List<UserLogData>();
                    if (WA != null)
                    {
                        if (WA.Count > 0)
                        {
                            foreach (var x in WA)
                            {
                                //get person infos
                                var getPerson = WEISPerson.Populate<WEISPerson>(Query.In("WellName", new BsonArray(new string[] { "*", x.WellName })));
                                var ListEmails = getPerson.SelectMany(d => d.PersonInfos, (d, p) => new
                                {
                                    Email = p.Email
                                })
                                .Select(d => d.Email)
                                .Distinct()
                                .ToList();
                                var qUserLogs = new List<IMongoQuery>();
                                qUserLogs.Add(Query.In("Email", new BsonArray(ListEmails)));
                                qUserLogs.Add(Query.EQ("LogType", "Login"));
                                if (UserName != null && UserName.Length > 0)
                                    qUserLogs.Add(Query.In("UserName", new BsonArray(UserName)));
                                if (PeriodFinish != null)
                                    qUserLogs.Add(Query.LTE("Time", PeriodFinish));
                                if (PeriodStart != null)
                                    qUserLogs.Add(Query.GTE("Time", PeriodStart));
                                qUserLogs.Add(Query.EQ("LogType", "Login"));
                                var getLogData = WEISUserLog.Populate<WEISUserLog>(Query.And(qUserLogs));
                                if (getLogData != null)
                                {
                                    if (getLogData.Count > 0)
                                    {
                                        foreach (var log in getLogData)
                                        {
                                            var LogData = new UserLogData();
                                            LogData.WellName = x.WellName;
                                            LogData.RigName = x.RigName;
                                            LogData.UserName = log.UserName;
                                            LogData.Duration = log.Duration;
                                            UserLogDatas.Add(LogData);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    var DataGroup = UserLogDatas.GroupBy(x => x.WellName);
                    if (GroupedBy == "RigName")
                    {
                        DataGroup = UserLogDatas.GroupBy(x => x.RigName);
                    }

                    Data = DataGroup.Select(d => new UserLogData
                    {
                        Key = d.Key,
                        NumberOfLogin = d.Count(),
                        Duration = d.Sum(x => x.Duration),
                        NumberOfUniqueUser = d.Select(x => x.UserName).Distinct().ToList().Count
                    }).ToList();
                }
                else
                {
                    var qUserLogs = new List<IMongoQuery>();
                    qUserLogs.Add(Query.EQ("LogType", "Login"));
                    if (UserName != null && UserName.Length > 0)
                        qUserLogs.Add(Query.In("UserName", new BsonArray(UserName)));
                    if (PeriodFinish != null)
                        qUserLogs.Add(Query.LTE("Time", PeriodFinish));
                    if (PeriodStart != null)
                        qUserLogs.Add(Query.GTE("Time", PeriodStart));
                    var getLogData = WEISUserLog.Populate<WEISUserLog>(Query.And(qUserLogs));
                    if (GroupedBy == "UserName")
                    {
                        Data = getLogData.GroupBy(x => x.UserName).Select(x => new UserLogData
                                    {
                                        Key = x.Key,
                                        Duration = x.Sum(d => d.Duration),
                                        NumberOfLogin = x.Count(),
                                        NumberOfUniqueUser = x.Select(d => d.UserName).Distinct().ToList().Count
                                    }).ToList();
                    }

                    if (GroupedBy == "Monthly")
                    {
                        Data = getLogData.GroupBy(x => new DateIsland(x.Time).MonthId).Select(x => 
                        {
                            var year = x.Key.ToString().Substring(0,4);
                            var month = x.Key.ToString().Substring(4, 2);
                            var a = DateTime.ParseExact(year+"-"+month+"-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);//Convert.ToDateTime("01-" + month + "-" + year).ToString("MMM-yyyy");
                            return new UserLogData
                            {
                                Key = a.ToString("MMM-yyyy"),
                                Duration = x.Sum(d => d.Duration),
                                NumberOfLogin = x.Count(),
                                NumberOfUniqueUser = x.Select(d => d.UserName).Distinct().ToList().Count
                            };
                        }).ToList();
                    }
                    if (GroupedBy == "Daily")
                    {
                        Data = getLogData.GroupBy(x => x.Time.Date).Select(x => new UserLogData
                        {
                            Key = x.Key.ToString("yyyy/MM/dd"),
                            Duration = x.Sum(d => d.Duration),
                            NumberOfLogin = x.Count(),
                            NumberOfUniqueUser = x.Select(d => d.UserName).Distinct().ToList().Count
                        }).ToList();
                    }
                    if (GroupedBy == "Weekly")
                    {
                        Data = getLogData.GroupBy(x => new DateIsland(x.Time).WeekId).Select(x => 
                        {
                            var year = x.Key.ToString().Substring(0, 4);
                            var week = x.Key.ToString().Substring(4);
                            return new UserLogData
                            {
                                Key = "Week#" + week+"/"+year,
                                Duration = x.Sum(d => d.Duration),
                                NumberOfLogin = x.Count(),
                                NumberOfUniqueUser = x.Select(d => d.UserName).Distinct().ToList().Count
                            };
                        }).ToList();
                    }

                }

                
                

                //var Grouped = UserLogDatas.GroupBy(x => x.UserName).Select(x => new 
                //{
                //    Key = x.Key,
                //    Duration = x.Sum(d=>d.Duration),
                //    NumberOfLogin = x.Count(),
                //    NumberOfUniqueUser = x.Select(d=>d.UserName).Distinct().ToList().Count
                //}).ToList();

                var MaxDuration = Data.Max(x => x.Duration);
                var MinDuration = Data.Min(x => x.Duration);
                var MaxLogin = Data.Max(x => x.NumberOfLogin);
                var MinLogin = Data.Min(x => x.NumberOfLogin);
                var MaxUnique = Data.Max(x => x.NumberOfUniqueUser);
                var MinUnique = Data.Min(x => x.NumberOfUniqueUser);

                MaxDuration = MaxDuration + (Math.Round(0.5 * MaxDuration, 0));
                MinDuration = MinDuration - (Math.Round(0.5 * MinDuration, 0));
                MaxLogin = MaxLogin + (Math.Round(0.5 * MaxLogin, 0));
                MinLogin = MinLogin - (Math.Round(0.5 * MinLogin, 0));
                MaxUnique = MaxUnique + (Math.Round(0.5 * MaxUnique, 0));
                MinUnique = MinUnique - (Math.Round(0.5 * MinUnique, 0));

                return Json(new { Data = Data, Key = GroupedBy,MaxDuration,MinDuration,MaxLogin,MinLogin,MaxUnique,MinUnique, Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch(Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }



	}
}