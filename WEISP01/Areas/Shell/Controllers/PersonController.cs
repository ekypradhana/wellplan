using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Globalization;
using System.Text.RegularExpressions;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using System.Collections.Specialized;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth(WEISRoles = "Administrators")]
    public class PersonController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Role()
        {
            return View();
        }

        public JsonResult GetWellActivities(string[] WellNames, string[] Activities)
        {
            List<IMongoQuery> queries = new List<IMongoQuery>();
            if (WellNames != null)
                if (WellNames.Length > 0)
                    queries.Add(Query.In("WellName", new BsonArray(WellNames)));

            if (Activities != null)
                if (Activities.Length > 0)
                    queries.Add(Query.In("Phases.ActivityType", new BsonArray(Activities)));

            var q = Query.Null;
            if (queries.Count > 0)
                q = Query.And(queries.ToArray());

            var wa = WellActivity.Populate<WellActivity>(q);

            List<WellActivityPerson> wap = new List<WellActivityPerson>();

            foreach (var x in wa)
            {
                foreach (var y in x.Phases)
                {
                    wap.Add(new WellActivityPerson
                    {
                        WellName = x.WellName,
                        UARigSequenceId = x.UARigSequenceId,
                        ActivityType = y.ActivityType,
                        PhaseNo = y.PhaseNo
                    });
                }
            }
            var result = new ResultInfo();
            result.Data = new
            {
                Data = wap.ToArray()
            };
            return MvcTools.ToJsonResult(result.Data, JsonRequestBehavior.AllowGet);

        }

        public JsonResult GetPersons(string WellName, int PhaseNo)
        {

            var result = new ResultInfo();
            List<IMongoQuery> queries = new List<IMongoQuery>();
            queries.Add(Query.EQ("WellName", WellName));
            queries.Add(Query.EQ("PhaseNo",PhaseNo));

            var wp = WEISPerson.Get<WEISPerson>(Query.And(queries));

            //List<WEISPersonInfo> wpi = new List<WEISPersonInfo>();

            //foreach (var x in wp.PersonInfos)
            //{
            //    wpi.Add(new WEISPersonInfo
            //    {
            //        Email = x.Email,
            //        FullName = x.FullName,
            //        RoleId = x.RoleId
            //    });
            //}
            result.Data = new
            {
                Data = wp
            };


            return MvcTools.ToJsonResult(result.Data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult SavePersonInfo(string WellName, int PhaseNo, string SequenceId, string ActivityType, List<WEISPersonInfo> PersonInfos)
        {

            try
            {
                WEISPerson wp = new WEISPerson();
                wp.WellName = WellName;
                wp.PhaseNo = PhaseNo;
                wp.SequenceId = SequenceId;
                wp.ActivityType = ActivityType;
                wp.PersonInfos = PersonInfos;

                wp.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetRole(string search = "")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                IMongoQuery query = null;

                if (!search.Trim().Equals(""))
                {
                    List<IMongoQuery> queries = new List<IMongoQuery>();

                    queries.Add(Query.Matches("_id",
                        new BsonRegularExpression(new Regex(search.ToLower(),
                            RegexOptions.IgnoreCase))));

                    queries.Add(Query.Matches("RoleName",
                        new BsonRegularExpression(new Regex(search.ToLower(),
                            RegexOptions.IgnoreCase))));

                    query = Query.Or(queries.ToArray());
                }

                return WEISRole.Populate<WEISRole>(query)
                    .Select(d => new
                    {
                        RoleID = d._id,
                        RoleName = d.RoleName,
                        HasPersons = d.HasPersons
                    })
                    .OrderBy(d => d.RoleID);
            });
        }

        public JsonResult DeleteRole(string[] roleIDs)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                try
                {
                    int thatHasPerson = 0;

                    foreach (var roleID in roleIDs)
                    {
                        List<IMongoQuery> queries = new List<IMongoQuery>();
                        queries.Add(Query.EQ("_id", roleID));

                        var result = WEISRole.Populate<WEISRole>(Query.And(queries.ToArray()));
                        if (result.Count() > 0)
                        {
                            if (result.FirstOrDefault().HasPersons)
                            {
                                thatHasPerson++;
                                continue;
                            }
                        }

                        DataHelper.Delete("WEISRoles", Query.EQ("_id", roleID));
                    }

                    if (thatHasPerson == roleIDs.Count()) {
                        return new
                        {
                            Success = false,
                            Message = "Roles being used cannot be deleted."
                        };
                    }

                    return new
                    {
                        Success = true
                    };

                }
                catch (Exception e)
                {
                    return new
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
            });
        }

        public JsonResult SaveRole(string roleName, string roleID, bool isEdit = false)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                try
                {
                    List<IMongoQuery> queries = new List<IMongoQuery>();
                    queries.Add(Query.EQ("RoleName", roleName));
                    if (!isEdit) queries.Add(Query.EQ("_id", roleID));

                    if (WEISRole.Populate<WEISRole>(Query.Or(queries.ToArray())).Count() > 0)
                    {
                        return new
                        {
                            Success = false,
                            Message = "Same Role ID / Name already exists"
                        };
                    }

                    var newRole = new BsonDocument();

                    if (isEdit)
                        DataHelper.Delete("WEISRoles", Query.EQ("_id", roleID));

                    newRole.Set("_id", roleID);
                    newRole.Set("RoleName", roleName);
                    DataHelper.Save("WEISRoles", newRole);

                    return new
                    {
                        Success = true
                    };
                }
                catch (Exception e)
                {
                    return new
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
            });
        }


    }
}