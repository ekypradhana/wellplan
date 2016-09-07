using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;

using ECIS.Core;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using Microsoft.AspNet.Identity;
using ECIS.Identity;
using ECIS.AppServer.Models;
using ECIS.Client.WEIS;
using WEISDataTrail;
using Microsoft.Owin.Security;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.IO;
using System.Web.Configuration;

using ECIS.Core.DataSerializer;
using System.Text;
using Aspose.Cells;

namespace ECIS.AppServer.Areas.DataLayer.Controllers
{
    public class DataLayerController : Controller
    {
        //
        // GET: /DataLayer/DataLayer/
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult ReserveConfig()
        {
            return View();
        }

        public JsonResult GetReserveConfig()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var t = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "WEISDataReserveConfig"));
                if (t.Any())
                {

                    DataReserveConfig cfg = BsonHelper.Deserialize<DataReserveConfig>(t.FirstOrDefault().ToBsonDocument().Get("ConfigValue").ToBsonDocument());
                    cfg.GetListReservedDate(cfg.WeekType);
                    var restotal = cfg.AllReservedDate;
                    var resmonths = cfg.MonthlyReserveDate;
                    var resdaily = cfg.DailyReserveDate;
                    var resweek = cfg.WeeklyReserveDate;
                    var LockStatus = cfg.LockStatus;
                    ri.Data = cfg;
                }
                else
                {
                    // default data

                    DataReserveConfig resv = new DataReserveConfig();
                    resv.DailyRunningTime = "01:00";
                    resv.MonthType = "First date of month";
                    resv.WeekType = "Tuesday";
                    resv.NoOfDays = 10;
                    resv.NoOfWeek = 1;
                    resv.NoOfMonth = 1;
                    resv.LockStatus = "Unlock";
                    resv.DateFilter = DateTime.Now;
                    Config c = new Config();
                    c._id = "WEISDataReserveConfig";
                    c.ConfigModule = "DataLayer";
                    c.ConfigValue = resv.ToBsonDocument();
                    c.Save();

                    var tx = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "WEISDataReserveConfig"));
                    DataReserveConfig cfg = BsonHelper.Deserialize<DataReserveConfig>(tx.FirstOrDefault().ToBsonDocument().Get("ConfigValue").ToBsonDocument());
                    cfg.GetListReservedDate(cfg.WeekType);
                    var restotal = cfg.AllReservedDate;
                    var resmonths = cfg.MonthlyReserveDate;
                    var resdaily = cfg.DailyReserveDate;
                    var resweek = cfg.WeeklyReserveDate;
                    var LockStatus = cfg.LockStatus;


                    ri.Data = cfg;

                }
            }
            catch (Exception ex)
            {
                ri.Data = "";
                ri.PushException(ex);
            }
            return MvcTools.ToJsonResult(ri);

        }


        public JsonResult GetTransactTrailByDate(int DateId, string CollectionName, int skip, int take)
        {
            return MvcResultInfo.Execute(() =>
            {
                int countData = 0;
                var datas = this.GetTransact(DateId, CollectionName, skip, take, out countData);

                return new { Data = DataHelper.ToDictionaryArray(datas), CountData = countData };
            });
        }



        public List<BsonDocument> GetTransact(int DateId, string CollectionName, int skip, int take, out int countData)
        {
            //int DateId = Convert.ToInt32(Date.ToString("yyyyMMdd"));
            List<BsonDocument> result = new List<BsonDocument>();
            List<BsonDocument> qscount = new List<BsonDocument>();

            switch (CollectionName)
            {

                case "WEISWellActivities_tr":
                    {
                        #region WEISWellActivities_tr
                        List<BsonDocument> qs = new List<BsonDocument>();
                        string m1 = @"{ $match: { 'DateId' : { '$eq' : '" + DateId.ToString() + "' } } }";
                        string p1 = @"{ $project: {
	                                    'WellName' : '$obj.WellName',
	                                    'RigName' : '$obj.RigName',
	                                    'OPHistories' : '$obj.OPHistories',
	                                    'Phases' : '$obj.Phases',
                                    } }";
                        string u1 = @"{ $unwind: '$Phases' }";
                        string s1 = @"{ $skip: " + skip + " }";
                        string l1 = @"{ $limit: " + take + " }";
                        string p2 = @"{  $project :  {
	                                        '_id' : '$_id',
	                                        'WellName' : '$WellName',
	                                        'RigName' : '$RigName',
	                                        'ActivityType' : '$Phases.ActivityType',
	                                        'OPHistories' : '$OPHistories',
	                                        'BaseOP' : '$Phases.BaseOP',
	                                        'AFE' : '$Phases.AFE',
	                                        'Plan' : '$Phases.Plan',
	                                        'Actual' : '$Phases.Actual',
	                                        'OP' : '$Phases.OP',
	                                        'LE' : '$Phases.LE',
	                                        'LWE' : '$Phases.LWE',
	                                        'LME' : '$Phases.LME',
                                        }}";
                        qs.Add(BsonSerializer.Deserialize<BsonDocument>(m1));
                        qs.Add(BsonSerializer.Deserialize<BsonDocument>(p1));
                        qs.Add(BsonSerializer.Deserialize<BsonDocument>(u1));
                        qs.Add(BsonSerializer.Deserialize<BsonDocument>(s1));
                        qs.Add(BsonSerializer.Deserialize<BsonDocument>(l1));
                        qs.Add(BsonSerializer.Deserialize<BsonDocument>(p2));
                        var res = ExtendedMDB.Aggregate("WEISWellActivities_tr", qs);





                        string c = @" {$group :  {
  	                            _id : 1,
	                            count : { $sum  : 1}  
                            }}";
                        qscount.Add(BsonSerializer.Deserialize<BsonDocument>(m1));
                        qscount.Add(BsonSerializer.Deserialize<BsonDocument>(p1));
                        qscount.Add(BsonSerializer.Deserialize<BsonDocument>(u1));
                        qscount.Add(BsonSerializer.Deserialize<BsonDocument>(p2));
                        qscount.Add(BsonSerializer.Deserialize<BsonDocument>(c));
                        countData = ExtendedMDB.Aggregate("WEISWellActivities_tr", qscount).FirstOrDefault().GetInt32("count");


                        result = res;
                        #endregion
                        break;
                    }
                case "WEISWellActivityUpdates_tr":
                    {
                        #region WEISWellActivityUpdates_tr
                        List<BsonDocument> qs = new List<BsonDocument>();
                        string m1 = @"{ $match: { 'DateId' : { '$eq' : '" + DateId.ToString() + "' } } }";
                        string p1 = @"{ $project: {
	                                    'WellName' : '$obj.WellName',
	                                    'RigName' : '$obj.RigName',
	                                    'ActivityType' : '$obj.Phase.ActivityType',
	                                    'CurrentWeek' : '$obj.CurrentWeek',
	                                    'Plan' : '$obj.Plan',
	                                    'OP' : '$obj.OP',
	                                    'AFE' : '$obj.AFE',
	                                    'Actual' : '$obj.Actual',
	                                    'LastWeek' : '$obj.LastWeek',
	                                    'TQ' : '$obj.TQ',
	                                    'NPT' : '$obj.NPT',
                                    }}";
                        string s1 = @"{ $skip: " + skip + " }";
                        string l1 = @"{ $limit: " + take + " }";
                        qs.Add(BsonSerializer.Deserialize<BsonDocument>(m1));
                        qs.Add(BsonSerializer.Deserialize<BsonDocument>(p1));
                        qs.Add(BsonSerializer.Deserialize<BsonDocument>(s1));
                        qs.Add(BsonSerializer.Deserialize<BsonDocument>(l1));
                        var res = ExtendedMDB.Aggregate("WEISWellActivityUpdates_tr", qs);
                        result = res;


                        string c = @" {$group :  {
  	                            _id : 1,
	                            count : { $sum  : 1}  
                            }}";
                        qscount.Add(BsonSerializer.Deserialize<BsonDocument>(m1));
                        qscount.Add(BsonSerializer.Deserialize<BsonDocument>(p1));
                        qscount.Add(BsonSerializer.Deserialize<BsonDocument>(c));
                        countData = ExtendedMDB.Aggregate("WEISWellActivityUpdates_tr", qscount).FirstOrDefault().GetInt32("count");

                        #endregion
                        break;
                    }
                case "WEISWellPIPs_tr":
                    {
                        #region WEISWellPIPs_tr
                        List<BsonDocument> qs = new List<BsonDocument>();
                        string m1 = @"{ $match: { 'DateId' : { '$eq' : '" + DateId.ToString() + "' } } }";
                        string p1 = @"{ $project: {
	                                            'WellName' : '$obj.WellName',
	
	                                            'ActivityType' : '$obj.ActivityType',
	                                            'Elements' : '$obj.Elements',
	                                            'CRElements' : '$obj.CRElements',
                                            }}";
                        string s1 = @"{ $skip: " + skip + " }";
                        string l1 = @"{ $limit: " + take + " }";
                        qs.Add(BsonSerializer.Deserialize<BsonDocument>(m1));
                        qs.Add(BsonSerializer.Deserialize<BsonDocument>(p1));
                        qs.Add(BsonSerializer.Deserialize<BsonDocument>(s1));
                        qs.Add(BsonSerializer.Deserialize<BsonDocument>(l1));
                        var res = ExtendedMDB.Aggregate("WEISWellPIPs_tr", qs);


                        List<BsonDocument> pipresult = new List<BsonDocument>();
                        List<BsonDocument> pipcrresult = new List<BsonDocument>();

                        foreach (var pip in res)
                        {
                            var resout = BsonHelper.Deserialize<WellPIP>(pip);
                            BsonDocument pipr = new BsonDocument();

                            var y = resout.Elements.Select(x => x.ToBsonDocument());//.Select()
                            pipr.Set("WellName", resout.WellName);
                            pipr.Set("ActivityType", resout.ActivityType);
                            pipr.Set("Type", "Well");

                            var planDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                            var planCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                            var leDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                            var leCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));

                            pipr.Set("planDays", planDays);
                            pipr.Set("planCost", planCost);
                            pipr.Set("leDays", leDays);
                            pipr.Set("leCost", leCost);


                            var rl = y.ToList().Where(x => x.GetString("Completion").Equals("Realized"));
                            var unRl = y.ToList().Where(x => !x.GetString("Completion").Equals("Realized"));
                            //realize
                            var planDaysRealize = rl.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                            var planCostRealize = rl.ToList().Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                            var leDaysRealize = rl.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                            var leCostRealize = rl.ToList().Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));

                            pipr.Set("planDaysRealize", planDaysRealize);
                            pipr.Set("planCostRealize", planCostRealize);
                            pipr.Set("leDaysRealize", leDaysRealize);
                            pipr.Set("leCostRealize", leCostRealize);

                            //unrealize
                            var planDaysUnRealize = unRl.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                            var planCostUnRealize = unRl.ToList().Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                            var leDaysUnRealize = unRl.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                            var leCostUnRealize = unRl.ToList().Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));

                            pipr.Set("planDaysUnRealize", planDaysUnRealize);
                            pipr.Set("planCostUnRealize", planCostUnRealize);
                            pipr.Set("leDaysUnRealize", leDaysUnRealize);
                            pipr.Set("leCostUnRealize", leCostUnRealize);
                            pipresult.Add(pipr);


                            /// CR Elements 
                            BsonDocument pipcr = new BsonDocument();

                            var crs = resout.CRElements.Select(x => x.ToBsonDocument());//.Select()
                            pipcr.Set("WellName", resout.WellName);
                            pipcr.Set("ActivityType", resout.ActivityType);
                            pipcr.Set("Type", "Rig");

                            var planDaysCR = crs.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                            var planCostCR = crs.ToList().Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                            var leDaysCR = crs.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                            var leCostCR = crs.ToList().Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));

                            pipcr.Set("planDays", planDaysCR);
                            pipcr.Set("planCost", planCostCR);
                            pipcr.Set("leDays", leDaysCR);
                            pipcr.Set("leCost", leCostCR);


                            var rlcr = crs.ToList().Where(x => x.GetString("Completion").Equals("Realized"));
                            var unRlcr = crs.ToList().Where(x => !x.GetString("Completion").Equals("Realized"));
                            //realize
                            var planDaysRealizeCR = rlcr.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                            var planCostRealizeCR = rlcr.ToList().Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                            var leDaysRealizeCR = rlcr.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                            var leCostRealizeCR = rlcr.ToList().Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));

                            pipcr.Set("planDaysRealize", planDaysRealizeCR);
                            pipcr.Set("planCostRealize", planCostRealizeCR);
                            pipcr.Set("leDaysRealize", leDaysRealizeCR);
                            pipcr.Set("leCostRealize", leCostRealizeCR);

                            //unrealize
                            var planDaysUnRealizeCR = unRlcr.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                            var planCostUnRealizeCR = unRlcr.ToList().Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                            var leDaysUnRealizeCR = unRlcr.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                            var leCostUnRealizeCR = unRlcr.ToList().Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));

                            pipcr.Set("planDaysUnRealize", planDaysUnRealizeCR);
                            pipcr.Set("planCostUnRealize", planCostUnRealizeCR);
                            pipcr.Set("leDaysUnRealize", leDaysUnRealizeCR);
                            pipcr.Set("leCostUnRealize", leCostUnRealizeCR);


                            pipresult.Add(pipcr);

                        }

                        result = pipresult;
                        string c = @" {$group :  {
  	                            _id : 1,
	                            count : { $sum  : 1}  
                            }}";
                        qscount.Add(BsonSerializer.Deserialize<BsonDocument>(m1));
                        qscount.Add(BsonSerializer.Deserialize<BsonDocument>(p1));
                        qscount.Add(BsonSerializer.Deserialize<BsonDocument>(c));
                        countData = ExtendedMDB.Aggregate("WEISWellPIPs_tr", qscount).FirstOrDefault().GetInt32("count");

                        #endregion

                        break;
                    }
                default:
                    {
                        throw new Exception("There is no Transact Collection Name ");
                        countData = 0;
                        break;
                    }
            }

            return result;
        }

        public DataReserveConfig DataReserverConfig(out DayOfWeek WeekType)
        {
            var t = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "WEISDataReserveConfig"));
            DataReserveConfig cfg = new DataReserveConfig();
            if (t.Any())
            {
                cfg = BsonHelper.Deserialize<DataReserveConfig>(t.FirstOrDefault().ToBsonDocument().Get("ConfigValue").ToBsonDocument());
            }
            else
            {
                // default data

                DataReserveConfig resv = new DataReserveConfig();
                resv.DailyRunningTime = "01:00";
                resv.MonthType = "First date of month";
                resv.WeekType = "Tuesday";
                resv.NoOfDays = 10;
                resv.NoOfWeek = 1;
                resv.NoOfMonth = 1;
                resv.LockStatus = "Unlock";
                Config c = new Config();
                c._id = "WEISDataReserveConfig";
                c.ConfigModule = "DataLayer";
                c.ConfigValue = resv.ToBsonDocument();
                c.Save();

                var tx = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "WEISDataReserveConfig"));
                cfg = BsonHelper.Deserialize<DataReserveConfig>(tx.FirstOrDefault().ToBsonDocument().Get("ConfigValue").ToBsonDocument());
            }

            switch (cfg.WeekType)
            {
                case "Sunday":
                    WeekType = DayOfWeek.Sunday;
                    break;
                case "Monday":
                    WeekType = DayOfWeek.Monday;
                    break;
                case "Tuesday":
                    WeekType = DayOfWeek.Tuesday;
                    break;
                case "Wednesday":
                    WeekType = DayOfWeek.Wednesday;
                    break;
                case "Thursday":
                    WeekType = DayOfWeek.Thursday;
                    break;
                case "Friday":
                    WeekType = DayOfWeek.Friday;
                    break;
                case "Saturday":
                    WeekType = DayOfWeek.Saturday;
                    break;
                default:
                    WeekType = DayOfWeek.Tuesday;
                    break;
            }

            return cfg;

        }

        public static string getBaseOP(out string previousOP, out string nextOP)
        {
            string DefaultOP = "OP15";
            if (Config.GetConfigValue("BaseOPConfig") != null)
            {
                DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
            }
            else
            {
                var config1 = Config.Get<Config>("BaseOPConfig") ?? new Config() { _id = "BaseOPConfig", ConfigModule = "BaseOPDefault" };
                config1.ConfigValue = DefaultOP;
                config1.Save();
            }

            var DefaultOPYear = Convert.ToInt32(DefaultOP.Substring(DefaultOP.Length - 2, 2));
            nextOP = "OP" + ((DefaultOPYear + 1).ToString());
            previousOP = "OP" + ((DefaultOPYear - 1).ToString());
            return DefaultOP;
        }
        public JsonResult GetDataMasterAndWellPlan2(DateTime parmDate, string parmType, int parmRolling, string parmOP, string parmResultType)
        {
            return MvcResultInfo.Execute(() =>
            {
                string PreviousOP = "";
                string NextOP = "";
                string DefaultOP = getBaseOP(out PreviousOP, out NextOP);
                var Division = 1000000.00;
                parmRolling = 7;
                var Periods = new List<DateTime>();
                var dateFinish = new DateTime();
                DateTime dateStart = new DateTime();
                var qs = new List<IMongoQuery>();
                var t = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "WEISDataReserveConfig"));
                DataReserveConfig cfg = new DataReserveConfig();
                #region getConfig
                if (t.Any())
                {
                    cfg = BsonHelper.Deserialize<DataReserveConfig>(t.FirstOrDefault().ToBsonDocument().Get("ConfigValue").ToBsonDocument());
                }
                else
                {
                    // default data

                    DataReserveConfig resv = new DataReserveConfig();
                    resv.DailyRunningTime = "01:00";
                    resv.MonthType = "First date of month";
                    resv.WeekType = "Tuesday";
                    resv.NoOfDays = 10;
                    resv.NoOfWeek = 1;
                    resv.NoOfMonth = 1;
                    resv.LockStatus = "Unlock";
                    Config c = new Config();
                    c._id = "WEISDataReserveConfig";
                    c.ConfigModule = "DataLayer";
                    c.ConfigValue = resv.ToBsonDocument();
                    c.Save();

                    var tx = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "WEISDataReserveConfig"));
                    cfg = BsonHelper.Deserialize<DataReserveConfig>(tx.FirstOrDefault().ToBsonDocument().Get("ConfigValue").ToBsonDocument());
                }
                #endregion


                #region query builder
                cfg.DateFilter = parmDate;
                if (parmResultType.ToLower().Contains("days"))
                {
                    cfg.NoOfDays = parmRolling;
                    Periods = cfg.GetlastDates(parmDate,7,true);
                    Periods = Periods.OrderByDescending(x => x.Date).ToList();
                }
                else
                {
                    if (parmResultType.ToLower().Contains("months"))
                    {
                        cfg.NoOfMonth = parmRolling;
                        Periods = cfg.GetlastMonths(parmDate,7,true);
                        Periods = Periods.OrderByDescending(x => x.Date).ToList();
                    }
                    else if (parmResultType.ToLower().Contains("weeks"))
                    {
                        cfg.NoOfWeek = parmRolling;
                        Periods = cfg.GetlastWeeks(cfg.WeekType.ToString(),parmDate);
                        Periods = Periods.OrderByDescending(x => x.Date).ToList();
                    }
                    else
                    {
                        cfg.NoOfMonth = parmRolling;
                        Periods = cfg.GetlastQuarter(parmDate);
                        Periods = Periods.OrderByDescending(x => x.Date).ToList();
                    }
                }
                #endregion

                dateStart = Periods.Min(x => x.Date);
                dateFinish = Periods.Max(x => x.Date);
                qs.Add(Query.LTE("TrailDate", Tools.ToUTC(new DateTime(dateFinish.Year, dateFinish.Month, dateFinish.Day, 23, 59, 59))));
                qs.Add(Query.GTE("TrailDate", Tools.ToUTC(new DateTime(dateStart.Year, dateStart.Month, dateStart.Day, 0, 0, 0))));
                var q = Query.And(qs); //Query.In("TrailDate", new BsonArray(Periods.ToArray()));
                List<CollectionSummary> csData = ExtendedMDB.Populate<CollectionSummary>("WEISCollectionSummary_tr", q);
                var MasterDatas = new List<ResultMasterData>();
                if (csData.Any())
                {
                    #region getMasterData
                    var MasterColls = csData.OrderByDescending(x => x.TrailDate).FirstOrDefault();
                    var Masters = MasterColls.Masters.Select(x => x.TableName).ToList();
                    foreach (var x in Masters)
                    {
                        var eachDoc = new ResultMasterData();
                        eachDoc.Coll = x;
                        var val = new List<int>();
                        foreach (var i in Periods)
                        {
                            var filter = csData.Where(d => d.DateId.ToString().Equals(i.ToString("yyyyMMdd")));
                            var sum = filter.Sum(d => d.Masters.Where(z => z.TableName.Equals(x)).Sum(z => z.Count));
                            val.Add(sum);
                        }
                        eachDoc.data = val;
                        MasterDatas.Add(eachDoc);
                    }
                    #endregion
                }

                #region getWellPlans
                var WellPlans = new List<ResultTrxData>();
                var WeeklyReports = new List<ResultTrxData>();

                List<int> per = new List<int>();
                per = Periods.Select(x => Convert.ToInt32(x.ToString("yyyyMMdd"))).ToList<int>();
                var datas = ExtendedMDB.Populate("WEISCollectionSummary_tr", Query.In("DateId", new BsonArray(per)));

                List<WellItems> items = new List<WellItems>();

                foreach (var i in Periods)
                {
                    var version = i.ToString("dd-MMM-yyyy");
                    var eachWellPlan = new ResultTrxData();
                    var eachWeeklyReport = new ResultTrxData();
                    if (parmType.ToLower().Contains("monthly") || parmType.ToLower().Contains("quarterly"))
                    {
                        version = i.ToString("MMM-yyyy");
                    }
                    eachWellPlan.Version = version;
                    eachWeeklyReport.Version = version;
                    var filter = csData.Where(x => x.TrailDate.Date == i.Date).OrderByDescending(x => x.TrailDate).FirstOrDefault();
                    if (filter != null)
                    {
                        #region summary well activity
                        //var WP_OPCost = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Cost);
                        //var WP_OPDays = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Days);
                        var WP_OPCost = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Cost);
                        var WP_OPDays = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Days);

                        var PrevOP = filter.WellPlan.Details.Select(x => x.PrevOP);
                        var WP_LastOPCost = 0.0;
                        var WP_LastOPDays = 0.0;

                        if (PrevOP != null)
                        {
                            WP_LastOPCost = PrevOP.Sum(x=>x.OP.Cost);
                            WP_LastOPDays = PrevOP.Sum(x => x.OP.Days);

                        }
                        
                        var WP_LEDays = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Days);
                        var WP_LECost = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Cost);
                        var WP_LSDays = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LS.Days);
                        var WP_LSCost = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LS.Cost);
                        var WP_AFEDays = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.AFE.Days);
                        var WP_AFECost = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.AFE.Cost);
                        var WP_MLEDays = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.MLE.Days);
                        var WP_MLECost = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.MLE.Cost);
                        var WP_items = new List<WellDrillDataForDL>();
                        WP_items.Add(new WellDrillDataForDL() { Title = "Current OP", Days = WP_OPDays, Cost = Tools.Div(WP_OPCost, Division) });
                        WP_items.Add(new WellDrillDataForDL() { Title = "Last OP", Days = WP_LastOPDays, Cost = Tools.Div(WP_LastOPCost, Division) });
                        WP_items.Add(new WellDrillDataForDL() { Title = "LE", Days = WP_LEDays, Cost = Tools.Div(WP_LECost, Division) });
                        WP_items.Add(new WellDrillDataForDL() { Title = "LS", Days = WP_LSDays, Cost = Tools.Div(WP_LSCost, Division) });
                        WP_items.Add(new WellDrillDataForDL() { Title = "AFE", Days = WP_AFEDays, Cost = Tools.Div(WP_AFECost, Division) });
                        WP_items.Add(new WellDrillDataForDL() { Title = "MLE", Days = WP_MLEDays, Cost = Tools.Div(WP_MLECost, Division) });
                        eachWellPlan.Items = WP_items;
                        #endregion

                        #region summary weekly report
                        var WR_OPCost = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Cost);
                        var WR_OPDays = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Days);
                        var WR_LEDays = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Days);
                        var WR_LECost = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Cost);
                        var WR_LSDays = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LS.Days);
                        var WR_LSCost = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LS.Cost);
                        var WR_AFEDays = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.AFE.Days);
                        var WR_AFECost = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.AFE.Cost);
                        var WR_MLEDays = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.MLE.Days);
                        var WR_MLECost = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.MLE.Cost);
                        var WR_items = new List<WellDrillDataForDL>();
                        WR_items.Add(new WellDrillDataForDL() { Title = parmOP, Days = WR_OPDays, Cost = Tools.Div(WR_OPCost, Division) });
                        WR_items.Add(new WellDrillDataForDL() { Title = "LE", Days = WR_LEDays, Cost = Tools.Div(WR_LECost, Division) });
                        WR_items.Add(new WellDrillDataForDL() { Title = "LS", Days = WR_LSDays, Cost = Tools.Div(WR_LSCost, Division) });
                        WR_items.Add(new WellDrillDataForDL() { Title = "AFE", Days = WR_AFEDays, Cost = Tools.Div(WR_AFECost, Division) });

                        eachWeeklyReport.Items = WR_items;
                        #endregion

                        #region detail well activities
                        //var dataWellPlans = ExtendedMDB.Populate("WEISWellActivities_tr", Query.EQ("DateId", i.ToString("yyyyMMdd")), take: 10, sort: SortBy.Descending("LastUpdate"));

                        var _pips = ExtendedMDB.Populate("WEISWellActivities_tr", q: Query.And(Query.GTE("TrailDate", Tools.ToUTC(new DateTime(i.Year, i.Month, i.Day, 0, 0, 0))), Query.LTE("TrailDate", Tools.ToUTC(new DateTime(i.Year, i.Month, i.Day, 0, 0, 0).AddDays(1).AddSeconds(-1)))), take: 10, sort: SortBy.Descending("obj.LastUpdate"));
                        var dataWellPlans = _pips.Select(x => x.GetDoc("obj"));

                        #region Will Find another best way
                        //string project1 = "{$project:{'LastUpdate':'$obj.LastUpdate','Object':'$obj','DateId':'$DateId','Phases':'$obj.Phases'}}";
                        //string unwind = @"{$unwind:'$Phases'}";
                        //string project2 = @"{$project:{'LastUpdate':'$LastUpdate','Object':'$Object','DateId':'$DateId','Phases':'$Phases','BaseOP':'$Phases.BaseOP'}}";
                        //string match1 = @"{$match:{'BaseOP':'"+parmOP+"'}}";
                        //string match2 = @"{$match:{'DateId':'" + i.ToString("yyyyMMdd") + "'}}";
                        //string sorts = @"{$sort:{'LastUpdate':"+-1+"}}";
                        //string limit = @"{$limit:"+10+"}";
                        //string project3 = @"{$project:{'LastUpdate':'$LastUpdate','Object':'$Object','DateId':'$DateId','Phases':'$Phases','BaseOP':'$BaseOP'}}";

                        //List<BsonDocument> pipelines = new List<BsonDocument>();
                        //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(project1));
                        //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(unwind));
                        //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(project2));
                        //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(match1));
                        //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(match2));
                        //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(sorts));
                        //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(limit));
                        //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(project3));
                        //List<BsonDocument> aggregate = ExtendedMDB.Aggregate("WEISWellActivities_tr", pipelines);



                        //var wellPlanTrail = dataWellPlans.Where(x => BsonHelper.GetString(x, "DateId") == Convert.ToString(i.ToString("yyyyMMdd")));
                        //var dataobj = wellPlanTrail.Select(x => BsonHelper.Get(x, "obj")).ToList();
                        //var dataPhase = aggregate.Select(x => BsonHelper.Get(x, "Phases")).ToList();
                        //List<WellActivity> activtyTemp = new List<WellActivity>();


                        #endregion
                        foreach (var tx in dataWellPlans)
                        {
                            var actv = BsonHelper.Deserialize<WellActivity>(tx.ToBsonDocument());
                            var phase = actv.Phases.Where(x => x.BaseOP.Contains(parmOP)).FirstOrDefault();
                            var hist = actv.OPHistories.Where(x => x.Type.Equals(PreviousOP)).FirstOrDefault() ?? new OPHistory();

                            WellItems sdld = new WellItems();
                            if (phase == null)
                            {
                                sdld.ActivityType = "";
                                sdld.WellName = "";
                                sdld.BaseOP = "";
                                sdld.RigName = "";
                                sdld.Items = new List<WellDrillData>();
                                sdld.Items.Add(new WellDrillData
                                {
                                    Identifier = "Current OP",
                                    Days = 0,
                                    Cost = 0
                                });
                                sdld.Items.Add(new WellDrillData
                                {
                                    Identifier = "Last OP",
                                    Days = 0,
                                    Cost = 0
                                });
                                sdld.Items.Add(new WellDrillData
                                {
                                    Identifier = "LE",
                                    Days = 0,
                                    Cost = 0
                                });
                                sdld.Items.Add(new WellDrillData
                                {
                                    Identifier = "LS",
                                    Days = 0,
                                    Cost = 0
                                });
                                sdld.Items.Add(new WellDrillData
                                {
                                    Identifier = "AFE",
                                    Days = 0,
                                    Cost = 0
                                });
                                sdld.Items.Add(new WellDrillData
                                {
                                    Identifier = "MLE",
                                    Days = 0,
                                    Cost = 0
                                });
                            }
                            else
                            {
                                sdld.ActivityType = phase.ActivityType;
                                sdld.WellName = actv.WellName;
                                sdld.BaseOP = phase.BaseOP.ToString();
                                sdld.RigName = actv.RigName;
                                sdld.Items = new List<WellDrillData>();
                                sdld.Items.Add(new WellDrillData
                                {
                                    Identifier = "Current OP",
                                    Days = phase.Plan.Days,
                                    Cost = phase.Plan.Cost
                                });
                                sdld.Items.Add(new WellDrillData
                                {
                                    Identifier = "Last OP",
                                    Days = hist.Plan.Days,
                                    Cost = hist.Plan.Cost
                                });
                                sdld.Items.Add(new WellDrillData
                                {
                                    Identifier = "LE",
                                    Days = phase.LE.Days + hist.LE.Days,
                                    Cost = phase.LE.Cost + hist.LE.Cost
                                });
                                sdld.Items.Add(new WellDrillData
                                {
                                    Identifier = "LS",
                                    Days = phase.OP.Days + hist.OP.Days,
                                    Cost = phase.OP.Cost + hist.OP.Cost
                                });
                                sdld.Items.Add(new WellDrillData
                                {
                                    Identifier = "AFE",
                                    Days = phase.AFE.Days + hist.AFE.Days,
                                    Cost = phase.AFE.Cost + hist.AFE.Cost
                                });
                                sdld.Items.Add(new WellDrillData
                                {
                                    Identifier = "MLE",
                                    Days = phase.LME.Days + hist.LME.Days,
                                    Cost = phase.LME.Cost + hist.LME.Cost
                                });
                            }

                            eachWellPlan.WellItems.Add(sdld);
                        }
                        #endregion

                        #region detail weekly report
                        var _detailWeeklyReport = ExtendedMDB.Populate("WEISWellActivityUpdates_tr", q: Query.And(Query.GTE("TrailDate", Tools.ToUTC(new DateTime(i.Year, i.Month, i.Day, 0, 0, 0))), Query.LTE("TrailDate", Tools.ToUTC(new DateTime(i.Year, i.Month, i.Day, 0, 0, 0).AddDays(1).AddSeconds(-1)))), take: 10, sort: SortBy.Descending("obj.LastUpdate"));
                        var dataWeeklyReport = _detailWeeklyReport.Select(x => x.GetDoc("obj"));
                        foreach (var wtx in dataWeeklyReport)
                        {
                            var weeklyact = BsonHelper.Deserialize<WellActivityUpdate>(wtx.ToBsonDocument());
                            var weekphase = weeklyact.Phase;

                            WellItems sdld = new WellItems();
                            var oplist = new List<string>();
                            if (weekphase.BaseOP.Count() == 1)
                            {
                                oplist.Add(weekphase.BaseOP[0]);
                            }
                            if (weekphase.BaseOP.Count() > 1)
                            {
                                oplist.Add(weekphase.BaseOP[0]);
                                oplist.Add(weekphase.BaseOP[1]);
                            }

                            sdld.ActivityType = weekphase.ActivityType;
                            sdld.WellName = weeklyact.WellName;
                            sdld.BaseOP = weekphase.BaseOP.ToString();
                            sdld.Items = new List<WellDrillData>();
                            sdld.Items.Add(new WellDrillData
                            {
                                Identifier = "OP",
                                Days = weekphase.Plan.Days,
                                Cost = weekphase.Plan.Cost
                            });
                            sdld.Items.Add(new WellDrillData
                            {
                                Identifier = "LE",
                                Days = weekphase.LE.Days,
                                Cost = weekphase.LE.Cost
                            });
                            sdld.Items.Add(new WellDrillData
                            {
                                Identifier = "LS",
                                Days = weekphase.OP.Days,
                                Cost = weekphase.OP.Cost
                            });
                            sdld.Items.Add(new WellDrillData
                            {
                                Identifier = "AFE",
                                Days = weekphase.AFE.Days,
                                Cost = weekphase.AFE.Cost
                            });


                            eachWeeklyReport.WellItems.Add(sdld);

                        }
                        #endregion

                    }
                    WellPlans.Add(eachWellPlan);
                    WeeklyReports.Add(eachWeeklyReport);
                }
                #endregion

                return new { Periods = Periods.Distinct().Select(x => x.Date.ToString("dd-MMM-yyyy")).ToList(), res = MasterDatas, WellPlans, WeeklyReports };
            });
        }

        //public JsonResult Detail(DateTime parmDate, string parmType, int parmRolling, string parmOP,
        //    string parmResultType)
        //{

        //}
        public JsonResult GetDataPIPAndBizPlan2(DateTime parmDate, string parmType, int parmRolling, string parmOP, string parmResultType)
        {
            return MvcResultInfo.Execute(() =>
            {
                //var wellPIPs = ExtendedMDB.Populate<CollectionSummary>("WEISWellPIPs_tr");
                var Division = 1;//1000000.00;
                parmRolling = 7;
                var Periods = new List<DateTime>();
                var dateFinish = new DateTime();
                DateTime dateStart = new DateTime();
                var qs = new List<IMongoQuery>();
                var t = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "WEISDataReserveConfig"));
                DataReserveConfig cfg = new DataReserveConfig();
                #region getConfig
                if (t.Any())
                {
                    cfg = BsonHelper.Deserialize<DataReserveConfig>(t.FirstOrDefault().ToBsonDocument().Get("ConfigValue").ToBsonDocument());
                }
                else
                {
                    // default data

                    DataReserveConfig resv = new DataReserveConfig();
                    resv.DailyRunningTime = "01:00";
                    resv.MonthType = "First date of month";
                    resv.WeekType = "Tuesday";
                    resv.NoOfDays = 10;
                    resv.NoOfWeek = 1;
                    resv.NoOfMonth = 1;
                    resv.LockStatus = "Unlock";
                    Config c = new Config();
                    c._id = "WEISDataReserveConfig";
                    c.ConfigModule = "DataLayer";
                    c.ConfigValue = resv.ToBsonDocument();
                    c.Save();

                    var tx = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "WEISDataReserveConfig"));
                    cfg = BsonHelper.Deserialize<DataReserveConfig>(tx.FirstOrDefault().ToBsonDocument().Get("ConfigValue").ToBsonDocument());
                }
                #endregion


                #region query builder
                cfg.DateFilter = parmDate;
                if (parmResultType.ToLower().Contains("days"))
                {
                    cfg.NoOfDays = parmRolling;
                    Periods = cfg.GetDailyReserveDate();
                    Periods = Periods.OrderByDescending(x => x.Date).ToList();
                }
                else
                {
                    if (parmResultType.ToLower().Contains("months"))
                    {
                        cfg.NoOfMonth = parmRolling;
                        Periods = cfg.GetMonthlyReserveDate();
                        Periods = Periods.OrderByDescending(x => x.Date).ToList();
                    }
                    else if (parmResultType.ToLower().Contains("weeks"))
                    {
                        cfg.NoOfWeek = parmRolling;
                        Periods = cfg.GetWeeklyReserveDate(cfg.WeekType.ToString());
                        Periods = Periods.OrderByDescending(x => x.Date).ToList();
                    }
                    else
                    {
                        cfg.NoOfMonth = parmRolling;
                        Periods = cfg.GetMonthlyReserveDate();
                        Periods = Periods.OrderByDescending(x => x.Date).ToList();
                    }
                }
                #endregion

                dateStart = Periods.Min(x => x.Date);
                dateFinish = Periods.Max(x => x.Date);
                qs.Add(Query.LTE("TrailDate", Tools.ToUTC(new DateTime(dateFinish.Year, dateFinish.Month, dateFinish.Day, 23, 59, 59))));
                qs.Add(Query.GTE("TrailDate", Tools.ToUTC(new DateTime(dateStart.Year, dateStart.Month, dateStart.Day, 0, 0, 0))));
                var q = Query.And(qs); //Query.In("TrailDate", new BsonArray(Periods.ToArray()));
                List<CollectionSummary> csData = ExtendedMDB.Populate<CollectionSummary>("WEISCollectionSummary_tr", q);

                #region getDatas
                var BizPlans = new List<ResultTrxData>();
                var PIPs = new List<ResultTrxData>();
                var RigPIPs = new List<ResultTrxData>();
                var res = "OK";
                if (!csData.Any())
                {
                    res = "NOK";
                }
                var counterPeriod = 0;
                var listWellName = new List<string>();
                var listActivityType = new List<string>();
                foreach (var i in Periods)
                {
                    counterPeriod++;
                    var _pips = new List<BsonDocument>();
                    if (counterPeriod > 1)
                    {
                        if (listWellName.Any() && listActivityType.Any())
                        {
                            _pips = ExtendedMDB.Populate("WEISWellPIPs_tr", q: Query.And(
                                Query.GTE("TrailDate", Tools.ToUTC(new DateTime(i.Year, i.Month, i.Day, 0, 0, 0))),
                                Query.LTE("TrailDate",
                                    Tools.ToUTC(new DateTime(i.Year, i.Month, i.Day, 0, 0, 0).AddDays(1).AddSeconds(-1))),
                                Query.In("WellName", new BsonArray(listWellName)),
                                Query.In("ActivityType", new BsonArray(listActivityType))
                                ),
                                take: 100,
                                sort: SortBy.Descending("obj.LastUpdate"));
                            _pips = _pips.OrderBy(x => x.GetString("WellName")).ThenBy(x => x.GetString("ActivityType")).Take(10).ToList();

                        }
                        else
                        {
                            _pips = ExtendedMDB.Populate("WEISWellPIPs_tr", q: Query.And(
                            Query.GTE("TrailDate", Tools.ToUTC(new DateTime(i.Year, i.Month, i.Day, 0, 0, 0))),
                            Query.LTE("TrailDate", Tools.ToUTC(new DateTime(i.Year, i.Month, i.Day, 0, 0, 0).AddDays(1).AddSeconds(-1)))
                            ),
                            take: 10,
                            sort: SortBy.Descending("obj.LastUpdate"));
                        }
                    }
                    else
                    {
                        _pips = ExtendedMDB.Populate("WEISWellPIPs_tr", q: Query.And(Query.GTE("TrailDate", Tools.ToUTC(new DateTime(i.Year, i.Month, i.Day, 0, 0, 0))), Query.LTE("TrailDate", Tools.ToUTC(new DateTime(i.Year, i.Month, i.Day, 0, 0, 0).AddDays(1).AddSeconds(-1)))), take: 10, sort: SortBy.Descending("obj.LastUpdate"));
                        if (_pips.Any())
                        {
                            _pips = _pips.OrderBy(x => x.GetString("WellName")).ThenBy(x => x.GetString("ActivityType")).ToList();
                            listWellName = _pips.Select(x => x.GetString("WellName")).ToList();
                            listActivityType = _pips.Select(x => x.GetString("ActivityType")).ToList();
                        }
                    }

                    //_pips = ExtendedMDB.Populate("WEISWellPIPs_tr", q: Query.And(Query.GTE("TrailDate", Tools.ToUTC(new DateTime(i.Year, i.Month, i.Day, 0, 0, 0))), Query.LTE("TrailDate", Tools.ToUTC(new DateTime(i.Year, i.Month, i.Day, 0, 0, 0).AddDays(1).AddSeconds(-1)))), take: 10, sort: SortBy.Descending("obj.LastUpdate"));
                    var pips = _pips.Select(x => BsonHelper.Deserialize<WellPIP>(x.GetDoc("obj")));

                    var eachBizPlan = new ResultTrxData();
                    var eachPIP = new ResultTrxData();
                    var eachRigPIP = new ResultTrxData();

                    var Details = new List<ResultTrxData>();

                    var version = i.ToString("dd-MMM-yyyy");
                    if (parmType.ToLower().Contains("monthly") || parmType.ToLower().Contains("quarterly"))
                    {
                        version = i.ToString("MMM-yyyy");
                    }
                    eachBizPlan.Version = version;
                    eachPIP.Version = version;
                    eachRigPIP.Version = version;
                    var filter = csData.Where(x => x.TrailDate.Date == i.Date).OrderByDescending(x => x.TrailDate).FirstOrDefault();
                    if (filter != null)
                    {
                        #region Bizplan
                        var BP_OPCost = filter.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Cost);
                        var BP_OPDays = filter.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Days);
                        var BP_LEDays = filter.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Days);
                        var BP_LECost = filter.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Cost);
                        var BP_LSDays = filter.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LS.Days);
                        var BP_LSCost = filter.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LS.Cost);
                        var BP_items = new List<WellDrillDataForDL>();
                        BP_items.Add(new WellDrillDataForDL() { Title = parmOP, Days = BP_OPDays, Cost = Tools.Div(BP_OPCost, Division) });
                        BP_items.Add(new WellDrillDataForDL() { Title = "LE", Days = BP_LEDays, Cost = Tools.Div(BP_LECost, Division) });
                        BP_items.Add(new WellDrillDataForDL() { Title = "LS", Days = BP_LSDays, Cost = Tools.Div(BP_LSCost, Division) });
                        eachBizPlan.Items = BP_items;
                        #endregion

                        #region PIP
                        var PIP_OPCost = filter.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Cost);
                        var PIP_OPDays = filter.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Days);
                        var PIP_LEDays = filter.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Days);
                        var PIP_LECost = filter.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Cost);

                        var PIP_OPCostRealize = filter.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OPRealized.Cost);
                        var PIP_OPDaysRealize = filter.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OPRealized.Days);
                        var PIP_LEDaysRealize = filter.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LERealized.Days);
                        var PIP_LECostRealize = filter.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LERealized.Cost);

                        var PIP_OPCostUnRealize = filter.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OPUnRealized.Cost);
                        var PIP_OPDaysUnRealize = filter.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OPUnRealized.Days);
                        var PIP_LEDaysUnRealize = filter.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LEUnRealized.Days);
                        var PIP_LECostUnRealize = filter.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LEUnRealized.Cost);

                        var PIP_items = new List<WellDrillDataForDL>();
                        PIP_items.Add(new WellDrillDataForDL() { Title = "Original Estimate", Days = PIP_OPDays, Cost = Tools.Div(PIP_OPCost, Division) });
                        PIP_items.Add(new WellDrillDataForDL() { Title = "Current Estimate", Days = PIP_LEDays, Cost = Tools.Div(PIP_LECost, Division) });
                        PIP_items.Add(new WellDrillDataForDL() { Title = "Original Estimate Realize", Days = PIP_OPDaysRealize, Cost = Tools.Div(PIP_OPCostRealize, Division) });
                        PIP_items.Add(new WellDrillDataForDL() { Title = "Current Estimate Realize", Days = PIP_LEDaysRealize, Cost = Tools.Div(PIP_LECostRealize, Division) });
                        PIP_items.Add(new WellDrillDataForDL() { Title = "Original Estimate UnRealize", Days = PIP_OPDaysUnRealize, Cost = Tools.Div(PIP_OPCostUnRealize, Division) });
                        PIP_items.Add(new WellDrillDataForDL() { Title = "Current Estimate UnRealize", Days = PIP_LEDaysUnRealize, Cost = Tools.Div(PIP_LECostUnRealize, Division) });
                        eachPIP.Items = PIP_items;

                        #endregion

                        #region Rig PIP
                        PIP_OPCost = filter.RigPIP.Details.Sum(x => x.OP.Cost);
                        PIP_OPDays = filter.RigPIP.Details.Sum(x => x.OP.Days);
                        PIP_LEDays = filter.RigPIP.Details.Sum(x => x.LE.Days);
                        PIP_LECost = filter.RigPIP.Details.Sum(x => x.LE.Cost);

                        PIP_OPCostRealize = filter.RigPIP.Details.Sum(x => x.OPRealized.Cost);
                        PIP_OPDaysRealize = filter.RigPIP.Details.Sum(x => x.OPRealized.Days);
                        PIP_LEDaysRealize = filter.RigPIP.Details.Sum(x => x.LERealized.Days);
                        PIP_LECostRealize = filter.RigPIP.Details.Sum(x => x.LERealized.Cost);

                        PIP_OPCostUnRealize = filter.RigPIP.Details.Sum(x => x.OPUnRealized.Cost);
                        PIP_OPDaysUnRealize = filter.RigPIP.Details.Sum(x => x.OPUnRealized.Days);
                        PIP_LEDaysUnRealize = filter.RigPIP.Details.Sum(x => x.LEUnRealized.Days);
                        PIP_LECostUnRealize = filter.RigPIP.Details.Sum(x => x.LEUnRealized.Cost);

                        PIP_items = new List<WellDrillDataForDL>();
                        PIP_items.Add(new WellDrillDataForDL() { Title = "Original Estimate", Days = PIP_OPDays, Cost = Tools.Div(PIP_OPCost, 1) });
                        PIP_items.Add(new WellDrillDataForDL() { Title = "Current Estimate", Days = PIP_LEDays, Cost = Tools.Div(PIP_LECost, 1) });
                        PIP_items.Add(new WellDrillDataForDL() { Title = "Original Estimate Realize", Days = PIP_OPDaysRealize, Cost = Tools.Div(PIP_OPCostRealize, 1) });
                        PIP_items.Add(new WellDrillDataForDL() { Title = "Current Estimate Realize", Days = PIP_LEDaysRealize, Cost = Tools.Div(PIP_LECostRealize, 1) });
                        PIP_items.Add(new WellDrillDataForDL() { Title = "Original Estimate UnRealize", Days = PIP_OPDaysUnRealize, Cost = Tools.Div(PIP_OPCostUnRealize, 1) });
                        PIP_items.Add(new WellDrillDataForDL() { Title = "Current Estimate UnRealize", Days = PIP_LEDaysUnRealize, Cost = Tools.Div(PIP_LECostUnRealize, 1) });
                        eachRigPIP.Items = PIP_items;
                        #endregion


                        #region History PIP
                        //eachPIP.WellItems = new List<WellItems>();
                        foreach (var yy in pips)
                        {
                            var bb = new WellItems();
                            bb.ActivityType = yy.ActivityType;
                            bb.WellName = yy.WellName;
                            var y = yy.Elements.Select(x => x.ToBsonDocument());//.Select()
                            var planDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                            var planCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                            var leDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                            var leCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));

                            var rl = y.ToList().Where(x => x.GetString("Completion").Equals("Realized"));
                            var unRl = y.ToList().Where(x => !x.GetString("Completion").Equals("Realized"));
                            //realize
                            var planDaysRealize = rl.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                            var planCostRealize = rl.ToList().Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                            var leDaysRealize = rl.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                            var leCostRealize = rl.ToList().Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));

                            //unrealize
                            var planDaysUnRealize = unRl.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                            var planCostUnRealize = unRl.ToList().Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                            var leDaysUnRealize = unRl.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                            var leCostUnRealize = unRl.ToList().Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));

                            var hh = new List<WellDrillData>();
                            hh.Add(new WellDrillData() { Identifier = "Original Estimate", Days = planDays, Cost = Tools.Div(planCost, 1) });
                            hh.Add(new WellDrillData() { Identifier = "Current Estimate", Days = leDays, Cost = Tools.Div(leCost, 1) });
                            hh.Add(new WellDrillData() { Identifier = "Original Estimate Realize", Days = planDaysRealize, Cost = Tools.Div(planCostRealize, 1) });
                            hh.Add(new WellDrillData() { Identifier = "Current Estimate Realize", Days = leDaysRealize, Cost = Tools.Div(leCostRealize, 1) });
                            hh.Add(new WellDrillData() { Identifier = "Original Estimate UnRealize", Days = planDaysUnRealize, Cost = Tools.Div(planCostUnRealize, 1) });
                            hh.Add(new WellDrillData() { Identifier = "Current Estimate UnRealize", Days = leDaysUnRealize, Cost = Tools.Div(leCostUnRealize, 1) });

                            bb.Items = hh;
                            eachPIP.WellItems.Add(bb);
                        }
                        #endregion

                        #region History Rig PIP
                        //eachRigPIP.WellItems = new List<WellItems>();
                        foreach (var yy in pips)
                        {
                            var bb = new WellItems();
                            bb.ActivityType = yy.ActivityType;
                            bb.WellName = yy.WellName;
                            var y = yy.CRElements.Select(x => x.ToBsonDocument());//.Select()
                            var planDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                            var planCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                            var leDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                            var leCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));

                            var rl = y.ToList().Where(x => x.GetString("Completion").Equals("Realized"));
                            var unRl = y.ToList().Where(x => !x.GetString("Completion").Equals("Realized"));
                            //realize
                            var planDaysRealize = rl.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                            var planCostRealize = rl.ToList().Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                            var leDaysRealize = rl.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                            var leCostRealize = rl.ToList().Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));

                            //unrealize
                            var planDaysUnRealize = unRl.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                            var planCostUnRealize = unRl.ToList().Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                            var leDaysUnRealize = unRl.ToList().Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                            var leCostUnRealize = unRl.ToList().Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));

                            var hh = new List<WellDrillData>();
                            hh.Add(new WellDrillData() { Identifier = "Original Estimate", Days = planDays, Cost = Tools.Div(planCost, 1) });
                            hh.Add(new WellDrillData() { Identifier = "Current Estimate", Days = leDays, Cost = Tools.Div(leCost, 1) });
                            hh.Add(new WellDrillData() { Identifier = "Original Estimate Realize", Days = planDaysRealize, Cost = Tools.Div(planCostRealize, 1) });
                            hh.Add(new WellDrillData() { Identifier = "Current Estimate Realize", Days = leDaysRealize, Cost = Tools.Div(leCostRealize, 1) });
                            hh.Add(new WellDrillData() { Identifier = "Original Estimate UnRealize", Days = planDaysUnRealize, Cost = Tools.Div(planCostUnRealize, 1) });
                            hh.Add(new WellDrillData() { Identifier = "Current Estimate UnRealize", Days = leDaysUnRealize, Cost = Tools.Div(leCostUnRealize, 1) });

                            bb.Items = hh;
                            eachRigPIP.WellItems.Add(bb);
                        }
                        #endregion

                    }
                    BizPlans.Add(eachBizPlan);
                    PIPs.Add(eachPIP);
                    RigPIPs.Add(eachRigPIP);

                    #region Get detail

                    #endregion

                }
                #endregion

                return new { BizPlans, PIPs, RigPIPs, Result = res };
            });
        }


        public JsonResult GetDataWeeklyReport(DateTime parmDate, string parmType, int parmRolling, string parmOP, string parmResultType)
        {
            return MvcResultInfo.Execute(() =>
            {
                var Division = 1000000.00;
                parmRolling = 7;
                var Periods = new List<DateTime>();
                var dateFinish = new DateTime();
                DateTime dateStart = new DateTime();
                var qs = new List<IMongoQuery>();
                var t = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "WEISDataReserveConfig"));
                DataReserveConfig cfg = new DataReserveConfig();
                #region getConfig
                if (t.Any())
                {
                    cfg = BsonHelper.Deserialize<DataReserveConfig>(t.FirstOrDefault().ToBsonDocument().Get("ConfigValue").ToBsonDocument());
                }
                else
                {
                    // default data

                    DataReserveConfig resv = new DataReserveConfig();
                    resv.DailyRunningTime = "01:00";
                    resv.MonthType = "First date of month";
                    resv.WeekType = "Tuesday";
                    resv.NoOfDays = 10;
                    resv.NoOfWeek = 1;
                    resv.NoOfMonth = 1;
                    resv.LockStatus = "Unlock";
                    Config c = new Config();
                    c._id = "WEISDataReserveConfig";
                    c.ConfigModule = "DataLayer";
                    c.ConfigValue = resv.ToBsonDocument();
                    c.Save();

                    var tx = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "WEISDataReserveConfig"));
                    cfg = BsonHelper.Deserialize<DataReserveConfig>(tx.FirstOrDefault().ToBsonDocument().Get("ConfigValue").ToBsonDocument());
                }
                #endregion


                #region query builder
                cfg.DateFilter = parmDate;
                if (parmResultType.ToLower().Contains("days"))
                {
                    cfg.NoOfDays = parmRolling;
                    Periods = cfg.GetDailyReserveDate();
                    Periods = Periods.OrderByDescending(x => x.Date).ToList();
                }
                else
                {
                    if (parmResultType.ToLower().Contains("months"))
                    {
                        cfg.NoOfMonth = parmRolling;
                        Periods = cfg.GetMonthlyReserveDate();
                        Periods = Periods.OrderByDescending(x => x.Date).ToList();
                    }
                    else if (parmResultType.ToLower().Contains("weeks"))
                    {
                        cfg.NoOfWeek = parmRolling;
                        Periods = cfg.GetWeeklyReserveDate(cfg.WeekType.ToString());
                        Periods = Periods.OrderByDescending(x => x.Date).ToList();
                    }
                    else
                    {
                        cfg.NoOfMonth = parmRolling;
                        Periods = cfg.GetMonthlyReserveDate();
                        Periods = Periods.OrderByDescending(x => x.Date).ToList();
                    }
                }
                #endregion

                dateStart = Periods.Min(x => x.Date);
                dateFinish = Periods.Max(x => x.Date);
                qs.Add(Query.LTE("TrailDate", Tools.ToUTC(new DateTime(dateFinish.Year, dateFinish.Month, dateFinish.Day, 23, 59, 59))));
                qs.Add(Query.GTE("TrailDate", Tools.ToUTC(new DateTime(dateStart.Year, dateStart.Month, dateStart.Day, 0, 0, 0))));
                var q = Query.And(qs); //Query.In("TrailDate", new BsonArray(Periods.ToArray()));
                List<CollectionSummary> csData = ExtendedMDB.Populate<CollectionSummary>("WEISCollectionSummary_tr", q);

                #region getDatas WR
                var WeeklyReports = new List<ResultTrxData>();
                var res = "OK";
                if (!csData.Any())
                {
                    res = "NOK";
                }
                foreach (var i in Periods)
                {
                    var eachWeek = new ResultTrxData();
                    var version = i.ToString("dd-MMM-yyyy");
                    if (parmType.ToLower().Contains("monthly") || parmType.ToLower().Contains("quarterly"))
                    {
                        version = i.ToString("MMM-yyyy");
                    }
                    eachWeek.Version = version;
                    var filter = csData.Where(x => x.TrailDate.Date == i.Date).OrderByDescending(x => x.TrailDate).FirstOrDefault();
                    if (filter != null)
                    {
                        #region WeeklyReport
                        var BP_OPCost = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Cost);
                        var BP_OPDays = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Days);
                        var BP_LEDays = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Days);
                        var BP_LECost = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Cost);
                        var BP_LSDays = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LS.Days);
                        var BP_LSCost = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LS.Cost);
                        var BP_AFEDays = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.AFE.Days);
                        var BP_AFECost = filter.WeeklyReport.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.AFE.Cost);

                        var BP_items = new List<WellDrillDataForDL>();
                        BP_items.Add(new WellDrillDataForDL() { Title = parmOP, Days = BP_OPDays, Cost = Tools.Div(BP_OPCost, Division) });
                        BP_items.Add(new WellDrillDataForDL() { Title = "LE", Days = BP_LEDays, Cost = Tools.Div(BP_LECost, Division) });
                        BP_items.Add(new WellDrillDataForDL() { Title = "LS", Days = BP_LSDays, Cost = Tools.Div(BP_LSCost, Division) });
                        BP_items.Add(new WellDrillDataForDL() { Title = "OP", Days = BP_OPDays, Cost = Tools.Div(BP_OPCost, Division) });
                        BP_items.Add(new WellDrillDataForDL() { Title = "AFE", Days = BP_AFEDays, Cost = Tools.Div(BP_AFECost, Division) });
                        eachWeek.Items = BP_items;
                        #endregion

                    }
                    WeeklyReports.Add(eachWeek);
                }
                #endregion

                return new { WeeklyReports, Result = res };
            });
        }

        public JsonResult GetDataDetailMaster(DateTime parmDate, string tableName)
        {
            return MvcResultInfo.Execute(() =>
            {
                var qs = new List<IMongoQuery>();
                int date = Convert.ToInt32(parmDate.ToString("yyyyMMdd"));
                qs.Add(Query.GTE("DateId", date - 1));
                qs.Add(Query.LTE("DateId", date));
                var q = Query.And(qs);
                List<CollectionSummary> csData = ExtendedMDB.Populate<CollectionSummary>("WEISCollectionSummary_tr", q);
                List<MasterSummary> masterdetail = new List<MasterSummary>();
                List<BsonDocument> datas = new List<BsonDocument>();
                foreach (var data in csData.OrderByDescending(o => o.DateId))
                {
                    foreach (var mstr in data.Masters)
                    {
                        if (mstr.TableName.ToLower().Equals(tableName.ToLower()))
                        {
                            List<BsonDocument> dtl = new List<BsonDocument>();
                            foreach (var detail in mstr.Datas)
                            {

                                var d = new BsonDocument();
                                d.Set("Title", detail);
                                dtl.Add(d);
                            }
                            BsonDocument dt = new BsonDocument();
                            dt.Set("DateId", data.TrailDate.ToString("dd-MMM-yyyy"));
                            dt.Set("Items", new BsonArray(dtl));
                            datas.Add(dt);
                            //masterdetail.Add(mstr);
                            break;
                        }
                    }
                }
                if (datas.Count < 2)
                {
                    if (datas.Count == 0)
                    {

                        for (int i = -1; i < 1; i++)
                        {
                            BsonDocument d = new BsonDocument();
                            d.Set("DateId", parmDate.AddDays(i).ToString("dd-MMM-yyyy"));
                            List<BsonDocument> dtl = new List<BsonDocument>();
                            var x = new BsonDocument { { "Title", "Data Not Found" } };
                            dtl.Add(x);
                            d.Set("Items", new BsonArray(dtl));
                            datas.Add(d);
                        }
                    }
                    else
                    {
                        BsonDocument d = new BsonDocument();

                        int dt = csData.FirstOrDefault().DateId;
                        if (date == dt)
                        {
                            d.Set("DateId", parmDate.AddDays(-1).ToString("dd-MMM-yyyy"));
                        }
                        else
                        {
                            d.Set("DateId", parmDate.ToString("dd-MMM-yyyy"));
                        }
                        List<BsonDocument> dtl = new List<BsonDocument>();
                        var x = new BsonDocument { { "Title", "Data Not Found" } };
                        dtl.Add(x);
                        d.Set("Items", new BsonArray(dtl));
                        datas.Add(d);
                    }

                }
                return new { DetailData = DataHelper.ToDictionaryArray(datas) };
            });
        }

        private int _getEachPIPAndBizPlan(CollectionSummary filter, out ResultTrxData eachBizPlan, out ResultTrxData eachPIP, out ResultTrxData eachWellPlan, string parmOP, double Division, string parmType)
        {
            eachBizPlan = new ResultTrxData();
            eachPIP = new ResultTrxData();
            eachWellPlan = new ResultTrxData();

            var version = filter.TrailDate.ToString("dd-MMM-yyyy");
            if (parmType.ToLower().Contains("monthly") || parmType.ToLower().Contains("quarterly"))
            {
                version = filter.TrailDate.ToString("MMM-yyyy");
            }

            eachBizPlan.Version = version;
            eachPIP.Version = version;
            eachWellPlan.Version = version;

            var BP_OPCost = filter.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Cost);
            var BP_OPDays = filter.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Days);
            var BP_LEDays = filter.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Days);
            var BP_LECost = filter.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Cost);
            var BP_LSDays = filter.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LS.Days);
            var BP_LSCost = filter.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LS.Cost);
            var BP_items = new List<WellDrillDataForDL>();
            BP_items.Add(new WellDrillDataForDL() { Title = parmOP, Days = BP_OPDays, Cost = Tools.Div(BP_OPCost, Division) });
            BP_items.Add(new WellDrillDataForDL() { Title = "LE", Days = BP_LEDays, Cost = Tools.Div(BP_LECost, Division) });
            BP_items.Add(new WellDrillDataForDL() { Title = "LS", Days = BP_LSDays, Cost = Tools.Div(BP_LSCost, Division) });
            eachBizPlan.Items = BP_items;

            var PIP_OPCost = filter.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Cost);
            var PIP_OPDays = filter.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Days);
            var PIP_LEDays = filter.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Days);
            var PIP_LECost = filter.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Cost);
            var PIP_items = new List<WellDrillDataForDL>();
            PIP_items.Add(new WellDrillDataForDL() { Title = "Plan", Days = PIP_OPDays, Cost = Tools.Div(PIP_OPCost, Division) });
            PIP_items.Add(new WellDrillDataForDL() { Title = "LE", Days = PIP_LEDays, Cost = Tools.Div(PIP_LECost, Division) });
            eachPIP.Items = PIP_items;

            var WP_OPCost = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Cost);
            var WP_OPDays = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Days);
            var WP_LEDays = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Days);
            var WP_LECost = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Cost);
            var WP_LSDays = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LS.Days);
            var WP_LSCost = filter.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LS.Cost);
            var WP_items = new List<WellDrillDataForDL>();
            WP_items.Add(new WellDrillDataForDL() { Title = parmOP, Days = WP_OPDays, Cost = Tools.Div(WP_OPCost, Division) });
            WP_items.Add(new WellDrillDataForDL() { Title = "LE", Days = WP_LEDays, Cost = Tools.Div(WP_LECost, Division) });
            WP_items.Add(new WellDrillDataForDL() { Title = "LS", Days = WP_LSDays, Cost = Tools.Div(WP_LSCost, Division) });
            eachWellPlan.Items = WP_items;

            return 1;
        }

        public JsonResult GetDataMaster(DateTime parmDate, string parmType, int parmRolling, string parmOP, string parmResultType)
        {
            return MvcResultInfo.Execute(() =>
            {
                var Division = 1000000;
                var startDates = new List<DateTime>();
                var Periods = new List<string>();
                for (var i = 0; i < parmRolling; i++)
                {
                    if (parmType.ToLower().Equals("weekly"))
                    {
                        DateTime start = parmDate.AddDays(i * 7 * -1);
                        startDates.Add(start);
                        Periods.Add(start.ToString("dd-MMM-yyyy"));
                    }
                    else if (parmType.ToLower().Equals("monthly"))
                    {
                        DateTime start = parmDate.AddMonths(i * -1);
                        startDates.Add(start);
                        Periods.Add(start.ToString("MMM-yyyy"));
                    }
                    else
                    {
                        DateTime start = parmDate.AddDays(i * -1);
                        startDates.Add(start);
                        Periods.Add(start.ToString("dd-MMM-yyyy"));
                    }
                }



                var qs = new List<IMongoQuery>();
                var maxDateWeekly = startDates.Max().AddDays(7);
                var minDateWeekly = startDates.Min();

                var maxDateMonthly = new DateTime(startDates.Max().Year, startDates.Max().Month, DateTime.DaysInMonth(startDates.Max().Year, startDates.Max().Month));
                var minDateMonthly = new DateTime(startDates.Min().Year, startDates.Min().Month, 1);

                var maxDateDaily = startDates.Max();
                var minDateDaily = startDates.Min();

                if (parmType.ToLower().Equals("weekly"))
                {
                    qs.Add(Query.LTE("TrailDate", maxDateWeekly));
                    qs.Add(Query.GTE("TrailDate", minDateWeekly));
                }
                else if (parmType.ToLower().Equals("monthly"))
                {
                    qs.Add(Query.LTE("TrailDate", maxDateMonthly));
                    qs.Add(Query.GTE("TrailDate", minDateMonthly));
                }
                else
                {
                    qs.Add(Query.LTE("DateId", Convert.ToInt32(maxDateDaily.ToString("yyyyMMdd"))));
                    qs.Add(Query.GTE("DateId", Convert.ToInt32(minDateDaily.ToString("yyyyMMdd"))));
                }

                var getData = ExtendedMDB.Populate<CollectionSummary>("WEISCollectionSummary_tr", Query.And(qs));
                var MasterColls = getData.OrderByDescending(x => x.TrailDate).FirstOrDefault();
                var Masters = MasterColls.Masters.Select(x => x.TableName).ToList();

                var MasterDatas = new List<ResultMasterData>();

                foreach (var x in Masters)
                {
                    var eachDoc = new ResultMasterData();
                    eachDoc.Coll = x;
                    var val = new List<int>();
                    foreach (var i in startDates)
                    {
                        var minFilter = new DateTime(i.Year, i.Month, 1);
                        var maxFilter = new DateTime(i.Year, i.Month, DateTime.DaysInMonth(i.Year, i.Month));
                        if (parmType.ToLower().Equals("weekly"))
                        {
                            minFilter = i;
                            maxFilter = i.AddDays(7);
                        }
                        var filter = getData.Where(d => d.DateId.ToString().Equals(i.ToString("yyyyMMdd")));
                        var sum = filter.Sum(d => d.Masters.Where(z => z.TableName.Equals(x)).Sum(z => z.Count));
                        val.Add(sum);
                    }
                    eachDoc.data = val;
                    MasterDatas.Add(eachDoc);
                }

                var WellPlans = new List<ResultTrxData>();
                var BizPlans = new List<ResultTrxData>();
                var PIPs = new List<ResultTrxData>();
                foreach (var i in startDates)
                {
                    var eachWellPlan = new ResultTrxData();
                    var eachBizPlan = new ResultTrxData();
                    var eachPIP = new ResultTrxData();
                    var version = parmType.ToLower().Equals("monthly") ? i.ToString("MMM-yyyy") : i.ToString("dd-MMM-yyyy");
                    eachWellPlan.Version = version;
                    eachBizPlan.Version = version;
                    eachPIP.Version = version;

                    var minFilter = new DateTime(i.Year, i.Month, 1);
                    var maxFilter = new DateTime(i.Year, i.Month, DateTime.DaysInMonth(i.Year, i.Month));
                    if (parmType.ToLower().Equals("weekly"))
                    {
                        minFilter = i;
                        maxFilter = i.AddDays(7);
                    }
                    var filter = getData.Where(d => d.DateId.ToString().Equals(i.ToString("yyyyMMdd")));
                    var WP_OPCost = filter.Sum(d => d.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Cost));
                    var WP_OPDays = filter.Sum(d => d.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Days));
                    var WP_LEDays = filter.Sum(d => d.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Days));
                    var WP_LECost = filter.Sum(d => d.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Cost));
                    var WP_LSDays = filter.Sum(d => d.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LS.Days));
                    var WP_LSCost = filter.Sum(d => d.WellPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LS.Cost));
                    var WP_items = new List<WellDrillDataForDL>();
                    WP_items.Add(new WellDrillDataForDL() { Title = parmOP, Days = WP_OPDays, Cost = Tools.Div(WP_OPCost, Division) });
                    WP_items.Add(new WellDrillDataForDL() { Title = "LE", Days = WP_LEDays, Cost = Tools.Div(WP_LECost, Division) });
                    WP_items.Add(new WellDrillDataForDL() { Title = "LS", Days = WP_LSDays, Cost = Tools.Div(WP_LSCost, Division) });
                    eachWellPlan.Items = WP_items;
                    WellPlans.Add(eachWellPlan);

                    var BP_OPCost = filter.Sum(d => d.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Cost));
                    var BP_OPDays = filter.Sum(d => d.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Days));
                    var BP_LEDays = filter.Sum(d => d.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Days));
                    var BP_LECost = filter.Sum(d => d.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Cost));
                    var BP_LSDays = filter.Sum(d => d.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LS.Days));
                    var BP_LSCost = filter.Sum(d => d.BizPlan.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LS.Cost));
                    var BP_items = new List<WellDrillDataForDL>();
                    BP_items.Add(new WellDrillDataForDL() { Title = parmOP, Days = BP_OPDays, Cost = Tools.Div(BP_OPCost, Division) });
                    BP_items.Add(new WellDrillDataForDL() { Title = "LE", Days = BP_LEDays, Cost = Tools.Div(BP_LECost, Division) });
                    BP_items.Add(new WellDrillDataForDL() { Title = "LS", Days = BP_LSDays, Cost = Tools.Div(BP_LSCost, Division) });
                    eachBizPlan.Items = BP_items;
                    BizPlans.Add(eachBizPlan);

                    var PIP_OPCost = filter.Sum(d => d.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Cost));
                    var PIP_OPDays = filter.Sum(d => d.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.OP.Days));
                    var PIP_LEDays = filter.Sum(d => d.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Days));
                    var PIP_LECost = filter.Sum(d => d.PIP.Details.Where(x => x.OPType.Contains(parmOP)).Sum(x => x.LE.Cost));
                    var PIP_items = new List<WellDrillDataForDL>();
                    PIP_items.Add(new WellDrillDataForDL() { Title = "Plan", Days = PIP_OPDays, Cost = Tools.Div(PIP_OPCost, Division) });
                    PIP_items.Add(new WellDrillDataForDL() { Title = "LE", Days = PIP_LEDays, Cost = Tools.Div(PIP_LECost, Division) });
                    eachPIP.Items = PIP_items;
                    PIPs.Add(eachPIP);
                }

                return new
                {
                    Periods,
                    res = MasterDatas,
                    WellPlans,
                    BizPlans,
                    PIPs
                };
            });
        }

        public JsonResult SaveConfig(string daily, int noofdays, int noofweek, string weektype, int noofmonth, string monthtype, string lockstatus = "Unlock")
        {

            ResultInfo ri = new ResultInfo();
            try
            {

                //var ts = DateTime.ParseExact(daily.ToLower(), "hh:mm tt",
                //             System.Globalization.CultureInfo.InvariantCulture).TimeOfDay;
                //before update
                var t = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "WEISDataReserveConfig"));
                DataReserveConfig cfgread = BsonHelper.Deserialize<DataReserveConfig>(t.FirstOrDefault().ToBsonDocument().Get("ConfigValue").ToBsonDocument());
                cfgread.GetListReservedDate(cfgread.WeekType);


                DataReserveConfig resv = new DataReserveConfig();
                resv.DailyRunningTime = daily;
                resv.MonthType = monthtype;
                resv.WeekType = weektype;
                resv.NoOfDays = noofdays;
                resv.NoOfWeek = noofweek;
                resv.NoOfMonth = noofmonth;
                resv.LockStatus = lockstatus;
                Config cfg = new Config();
                cfg._id = "WEISDataReserveConfig";
                cfg.ConfigModule = "DataLayer";
                cfg.ConfigValue = resv.ToBsonDocument();
                cfg.Save();

                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(
                    WebTools.LoginUser.UserName,
                    WebTools.LoginUser.Email,
                    LogType.Update, "WEISDataReserveConfig", url, cfgread.ToBsonDocument(), cfg.ToBsonDocument());


                ri.Result = "OK";
                //ri.Data = config;
            }
            catch (Exception ex)
            {
                ri.Data = "";
                ri.PushException(ex);
            }
            return MvcTools.ToJsonResult(ri);

        }

        public JsonResult LastestBatchInfo()
        {
            return MvcResultInfo.Execute(() =>
            {

                var dataLast = ExtendedMDB.Populate("WEISCollectionSummary_tr", sort: SortBy.Descending(new string[] { "DateId" }), take: 1, fields: new string[] { "TrailDate" });
                var dataFirst = ExtendedMDB.Populate("WEISCollectionSummary_tr", sort: SortBy.Ascending(new string[] { "DateId" }), take: 1, fields: new string[] { "TrailDate" });
                var FisrtData = dataFirst.Any() ? dataFirst.FirstOrDefault().GetDateTime("TrailDate") : Tools.DefaultDate;
                var LastData = dataLast.Any() ? dataLast.FirstOrDefault().GetDateTime("TrailDate") : Tools.DefaultDate;
                var temp = new List<string>();
                var datatemp = new BsonDocument();

                temp.Add(FisrtData.ToString());
                temp.Add(LastData.ToString());
                return temp;
            });

        }
        internal class ResultMasterData
        {
            public string Coll { get; set; }
            public List<int> data { get; set; }
        }

        internal class ResultTrxData
        {
            public string Version { get; set; }
            public List<WellDrillDataForDL> Items { get; set; }
            public List<WellItems> WellItems { get; set; }

            public ResultTrxData()
            {
                Items = new List<WellDrillDataForDL>();
                WellItems = new List<WellItems>();
            }
        }

        internal class WellDrillDataForDL : WellDrillData
        {
            public string Title { get; set; }

            public WellDrillDataForDL()
            {
            }

        }

        internal class WellItems
        {
            public string WellName { get; set; }
            public string ActivityType { get; set; }
            public string RigName { get; set; }
            public string BaseOP { get; set; }
            public List<WellDrillData> Items { get; set; }
            public WellItems()
            {
                Items = new List<WellDrillData>();
            }
        }



    }

}