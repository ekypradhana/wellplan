using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using System.Globalization;

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

namespace ECIS.AppServer.Areas.DataLayer2.Controllers
{
    public class DataLayer2Controller : Controller
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


        public JsonResult GetTransactTrailByDate(List<string> wellNames,int DateId, string CollectionName, int skip, int take) //List<Dictionary<string, object[]>> WellItems,
        {
            return MvcResultInfo.Execute(() =>
            {
                int countData = 0;
                int dat = DateId % 100;
                int mon = (DateId / 100) % 100;
                int yea = DateId / 10000;

                var date = new DateTime(yea, mon, dat);
                #region
                //if (countData != 0)
                //{
                //    for (var i = skip; i < take + skip; i++)
                //    {
                //        WellItems data = new WellItems();

                //        var a = Request.Params["WellItems[" + i + "][Items][0][Days]"].ToString();
                //        var aa = Request.Params["WellItems[" + i + "][Items][0][Cost]"].ToString();

                //        var b = Request.Params["WellItems[" + i + "][Items][1][Days]"].ToString();
                //        var bb = Request.Params["WellItems[" + i + "][Items][1][Cost]"].ToString();

                //        var c = Request.Params["WellItems[" + i + "][Items][2][Days]"].ToString();
                //        var cc = Request.Params["WellItems[" + i + "][Items][2][Cost]"].ToString();

                //        var d = Request.Params["WellItems[" + i + "][Items][3][Days]"].ToString();
                //        var dd = Request.Params["WellItems[" + i + "][Items][3][Cost]"].ToString();

                //        var e = Request.Params["WellItems[" + i + "][Items][4][Days]"].ToString();
                //        var ee = Request.Params["WellItems[" + i + "][Items][4][Cost]"].ToString();

                //        var f = Request.Params["WellItems[" + i + "][Items][5][Days]"].ToString();
                //        var ff = Request.Params["WellItems[" + i + "][Items][5][Cost]"].ToString();


                //        data.WellName = Request.Params["WellItems[" + i + "][WellName]"].ToString();
                //        data.RigName = Request.Params["WellItems[" + i + "][RigName]"].ToString();
                //        data.ActivityType = Request.Params["WellItems[" + i + "][ActivityType]"].ToString();
                //        data.BaseOP = Request.Params["WellItems[" + i + "][BaseOP]"].ToString();
                //        List<WellDrillData> Items = new List<WellDrillData>();
                //        if (CollectionName.ToLower().Equals("weiswellactivities_tr"))
                //        {
                //            Items.Add(new WellDrillData { Identifier = "Current OP", Days = Convert.ToDouble(a), Cost = Convert.ToDouble(aa) });
                //            Items.Add(new WellDrillData { Identifier = "Previous OP", Days = Convert.ToDouble(b), Cost = Convert.ToDouble(bb) });
                //            Items.Add(new WellDrillData { Identifier = "LE", Days = Convert.ToDouble(c), Cost = Convert.ToDouble(cc) });
                //            Items.Add(new WellDrillData { Identifier = "LS", Days = Convert.ToDouble(d), Cost = Convert.ToDouble(dd) });
                //            Items.Add(new WellDrillData { Identifier = "AFE", Days = Convert.ToDouble(e), Cost = Convert.ToDouble(ee) });
                //            Items.Add(new WellDrillData { Identifier = "MLE", Days = Convert.ToDouble(f), Cost = Convert.ToDouble(ff) });
                //        }
                //        else
                //        {
                //            Items.Add(new WellDrillData { Identifier = "Original Estimate", Days = Convert.ToDouble(a), Cost = Convert.ToDouble(aa) });
                //            Items.Add(new WellDrillData { Identifier = "Current Estimate", Days = Convert.ToDouble(b), Cost = Convert.ToDouble(bb) });
                //            Items.Add(new WellDrillData { Identifier = "Original Estimate Realize", Days = Convert.ToDouble(c), Cost = Convert.ToDouble(cc) });
                //            Items.Add(new WellDrillData { Identifier = "Current Estimate Realize", Days = Convert.ToDouble(d), Cost = Convert.ToDouble(dd) });
                //            Items.Add(new WellDrillData { Identifier = "Original Estimate UnRealize", Days = Convert.ToDouble(e), Cost = Convert.ToDouble(ee) });
                //            Items.Add(new WellDrillData { Identifier = "Current Estimate UnRealize", Days = Convert.ToDouble(f), Cost = Convert.ToDouble(ff) });
                //        }

                //        data.Items = Items;
                //        Record.Add(data);
                //    }
                //}
                
                #endregion
                List<WellItems> Record = new List<WellItems>();

                var ParamsWellnames = Request.Params["wellNames[]"];
                if (ParamsWellnames != null)
                {
                    var stringParam = ParamsWellnames.Split(',');
                    wellNames = new List<string>();
                    foreach (var i in stringParam)
                    {
                        wellNames.Add(i);
                    }
                }



                ResultTrxData data = GetResultTrxData(wellNames,date, skip, CollectionName);
                //ResultTrxData dt = new ResultTrxData();
                data.Version = date.ToString("dd-MMM-yyyy");
                //data.WellItems = Record;
                if (data.WellItems == null)
                    countData = 0;
                else
                    countData = data.WellItems.Count;
                var datas = this.GetTransact(data, CollectionName, skip, take);
                return new { Data = DataHelper.ToDictionaryArray(datas), CountData = countData };
            });
        }

        public List<BsonDocument> GetTransact(ResultTrxData source, string CollectionName, int skip, int take)
        {
            //int DateId = Convert.ToInt32(Date.ToString("yyyyMMdd"));
            List<BsonDocument> result = new List<BsonDocument>();
            List<BsonDocument> qscount = new List<BsonDocument>();


            switch (CollectionName)
            {

                case "WEISWellActivities_tr":
                    {
                        #region WEISWellActivities_tr
                        var datas = source.WellItems
                            .Select(t => new
                            {
                                WellName = t.WellName,
                                RigName = t.RigName,
                                ActivityType = t.ActivityType,
                                BaseOP = t.BaseOP,
                                AFE = t.Items.FirstOrDefault(x => x.Identifier.Equals("AFE")),
                                CurrentOP = t.Items.FirstOrDefault(x => x.Identifier.Equals("Current OP")),
                                LastOP = t.Items.FirstOrDefault(x => x.Identifier.Equals("Previous OP")),
                                LE = t.Items.FirstOrDefault(x => x.Identifier.Equals("LE")),
                                LME = t.Items.FirstOrDefault(x => x.Identifier.Equals("MLE")),
                                LS = t.Items.FirstOrDefault(x => x.Identifier.Equals("OP"))
                            }).Skip(skip).Take(10).ToList();

                        List<BsonDocument> res = new List<BsonDocument>();
                        if (datas.Any())
                        {
                            foreach (var dt in datas)
                            {
                                BsonDocument dts = new BsonDocument();

                                dts.Set("WellName", dt.WellName);
                                dts.Set("RigName", dt.RigName);
                                dts.Set("ActivityType", dt.ActivityType);
                                dts.Set("BaseOP", dt.BaseOP);
                                var afe = dt.AFE == null ? new WellDrillData { Identifier = "AFE", Days = 0, Cost = 0 } : dt.AFE;
                                dts.Set("AFE", afe.ToBsonDocument());
                                var currentop = dt.CurrentOP == null ? new WellDrillData { Identifier = "Current OP", Days = 0, Cost = 0 } : dt.CurrentOP;
                                dts.Set("CurrentOP", currentop.ToBsonDocument());
                                var lastop = dt.LastOP == null ? new WellDrillData { Identifier = "Previous OP", Days = 0, Cost = 0 } : dt.LastOP;
                                dts.Set("LastOP", lastop.ToBsonDocument());
                                var le = dt.LE == null ? new WellDrillData { Identifier = "LE", Days = 0, Cost = 0 } : dt.LE;
                                dts.Set("LE", le.ToBsonDocument());
                                var lme = dt.LME == null ? new WellDrillData { Identifier = "LME", Days = 0, Cost = 0 } : dt.LME;
                                dts.Set("LME", lme.ToBsonDocument());
                                var ls = dt.LS == null ? new WellDrillData { Identifier = "LS", Days = 0, Cost = 0 } : dt.LS;
                                dts.Set("LS", ls.ToBsonDocument());
                                res.Add(dts);
                            }
                        }

                        result = res;
                        #endregion
                        break;
                    }
                case "WEISWellPIPs_tr_well":
                    {
                        #region WEISWellPIPs_tr_well
                        var datas = source.WellItems
                            .Select(t => new
                            {
                                WellName = t.WellName,
                                RigName = t.RigName,
                                ActivityType = t.ActivityType,
                                planRealize = t.Items.FirstOrDefault(x => x.Identifier.Equals("Original Estimate Realize")),
                                leRealize = t.Items.FirstOrDefault(x => x.Identifier.Equals("Current Estimate Realize")),
                                planUnRealize = t.Items.FirstOrDefault(x => x.Identifier.Equals("Original Estimate UnRealize")),
                                leUnRealize = t.Items.FirstOrDefault(x => x.Identifier.Equals("Current Estimate UnRealize")),
                                plan = t.Items.FirstOrDefault(x => x.Identifier.Equals("Original Estimate")),
                                le = t.Items.FirstOrDefault(x => x.Identifier.Equals("Current Estimate"))
                            }).Skip(skip).Take(10).ToList();

                        List<BsonDocument> res = new List<BsonDocument>();
                        if (datas.Any())
                        {
                            foreach (var dt in datas)
                            {
                                BsonDocument dts = new BsonDocument();
                                dts.Set("WellName", dt.WellName);
                                dts.Set("RigName", dt.RigName);
                                dts.Set("ActivityType", dt.ActivityType);
                                var plan = dt.plan == null ? new WellDrillData { Identifier = "Original Estimate", Days = 0, Cost = 0 } : dt.plan;
                                dts.Set("plan", plan.ToBsonDocument());
                                var le = dt.le == null ? new WellDrillData { Identifier = "Current Estimate", Days = 0, Cost = 0 } : dt.le;
                                dts.Set("le", le.ToBsonDocument());
                                var planrealize = dt.planRealize == null ? new WellDrillData { Identifier = "Original Estimate Realize", Days = 0, Cost = 0 } : dt.planRealize;
                                dts.Set("planRealize", planrealize.ToBsonDocument());
                                var lerealize = dt.leRealize == null ? new WellDrillData { Identifier = "Current Estimate Realize", Days = 0, Cost = 0 } : dt.leRealize;
                                dts.Set("leRealize", lerealize.ToBsonDocument());
                                var planunrealize = dt.planUnRealize == null ? new WellDrillData { Identifier = "Original Estimate UnRealize", Days = 0, Cost = 0 } : dt.planUnRealize;
                                dts.Set("planUnRealize", planunrealize.ToBsonDocument());
                                var leunrealize = dt.leUnRealize == null ? new WellDrillData { Identifier = "Current Estimate UnRealize", Days = 0, Cost = 0 } : dt.leUnRealize;
                                dts.Set("leUnRealize", leunrealize.ToBsonDocument());
                                res.Add(dts);
                            }
                        }

                        result = res;
                        #endregion
                        break;
                    }
                case "WEISWellPIPs_tr_rig":
                    {
                        #region WEISWellPIPs_tr_well
                        var datas = source.WellItems
                            .Select(t => new
                            {
                                WellName = t.WellName,
                                RigName = t.RigName,
                                ActivityType = t.ActivityType,
                                planRealize = t.Items.FirstOrDefault(x => x.Identifier.Equals("Original Estimate Realize")),
                                leRealize = t.Items.FirstOrDefault(x => x.Identifier.Equals("Current Estimate Realize")),
                                planUnRealize = t.Items.FirstOrDefault(x => x.Identifier.Equals("Original Estimate UnRealize")),
                                leUnRealize = t.Items.FirstOrDefault(x => x.Identifier.Equals("Current Estimate UnRealize")),
                                plan = t.Items.FirstOrDefault(x => x.Identifier.Equals("Original Estimate")),
                                le = t.Items.FirstOrDefault(x => x.Identifier.Equals("Current Estimate"))
                            }).Skip(skip).Take(10).ToList();

                        List<BsonDocument> res = new List<BsonDocument>();
                        if (datas.Any())
                        {
                            foreach (var dt in datas)
                            {
                                BsonDocument dts = new BsonDocument();
                                dts.Set("WellName", dt.WellName);
                                dts.Set("RigName", dt.RigName);
                                dts.Set("ActivityType", dt.ActivityType);
                                var plan = dt.plan == null ? new WellDrillData { Identifier = "Original Estimate", Days = 0, Cost = 0 } : dt.plan;
                                dts.Set("plan", plan.ToBsonDocument());
                                var le = dt.le == null ? new WellDrillData { Identifier = "Current Estimate", Days = 0, Cost = 0 } : dt.le;
                                dts.Set("le", le.ToBsonDocument());
                                var planrealize = dt.planRealize == null ? new WellDrillData { Identifier = "Original Estimate Realize", Days = 0, Cost = 0 } : dt.planRealize;
                                dts.Set("planRealize", planrealize.ToBsonDocument());
                                var lerealize = dt.leRealize == null ? new WellDrillData { Identifier = "Current Estimate Realize", Days = 0, Cost = 0 } : dt.leRealize;
                                dts.Set("leRealize", lerealize.ToBsonDocument());
                                var planunrealize = dt.planUnRealize == null ? new WellDrillData { Identifier = "Original Estimate UnRealize", Days = 0, Cost = 0 } : dt.planUnRealize;
                                dts.Set("planUnRealize", planunrealize.ToBsonDocument());
                                var leunrealize = dt.leUnRealize == null ? new WellDrillData { Identifier = "Current Estimate UnRealize", Days = 0, Cost = 0 } : dt.leUnRealize;
                                dts.Set("leUnRealize", leunrealize.ToBsonDocument());
                                res.Add(dts);
                            }
                        }

                        result = res;
                        #endregion
                        break;
                    }

                default:
                    {
                        throw new Exception("There is no Transact Collection Name ");
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

        public class ResultTrxData
        {
            public Object _id { get; set; }
            public string Version { get; set; }
            public List<WellDrillDataForDL> Items { get; set; }
            public List<WellItems> WellItems { get; set; }

            public ResultTrxData()
            {
                Items = new List<WellDrillDataForDL>();
                WellItems = new List<WellItems>();
            }
        }

        public class WellDrillDataForDL : WellDrillData
        {
            public string Title { get; set; }

            public WellDrillDataForDL()
            {
            }

        }

        public class WellItems
        {
            public string WellName { get; set; }
            public string ActivityType { get; set; }
            public string RigName { get; set; }
            public string BaseOP { get; set; }
            public bool IsWeekly { get; set; }
            public DateTime DateWeekly { get; set; }
            public List<WellDrillData> Items { get; set; }
            public WellItems()
            {
                Items = new List<WellDrillData>();
                DateWeekly = new DateTime();
            }
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

        public ResultTrxData GetResultTrxData(List<string> wellnames,DateTime date, int skip, string CollectionName)
        {
            var arrstr = new List<string>();
            if (wellnames != null)
            {
                foreach (var text in wellnames)
                {
                    arrstr.Add("'" + text + "'");
                }
            }
            string project1 = "{$project:{'LastUpdate':'$LastUpdate','Phases':'$Phases','WellName':'$WellName','RigName':'$RigName','SequenceId':'$UARigSequenceId','History':'$OPHistories'}}";
            string unwind = @"{$unwind:'$Phases'}";
            string project2 = @"{$project:{'LastUpdate':'$LastUpdate','WellName':'$WellName','RigName':'$RigName','Phases':'$Phases','SequenceId':'$SequenceId','History':'$History'}}";
            string match = "";
            if (wellnames != null)
            {
                match = @"{$match:{'WellName':{'$in':" + new BsonArray(arrstr) + "}}}";
            }
            
            string sorts = @"{$sort:{'LastUpdate':" + -1 + "}}";

            string project3 = @"{$project:{'LastUpdate':'$LastUpdate','Phase':'$Phases','WellName':'$WellName','SequenceId':'$SequenceId','RigName':'$RigName','History':'$History'}}";
            //string limit = @"{ $limit: " + 10 + " }";
            //string SKIP = @"{ $skip: " + skip * 10 + " }";

            List<BsonDocument> pipelines = new List<BsonDocument>();
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(project1));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(unwind));            
            
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(project2));
            if (wellnames != null)
            {
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(match));
            }
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(sorts));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(project3));
            //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(limit));
            //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(SKIP));
            List<BsonDocument> aggregate = DataHelper.Aggregate(new WellActivity().TableName, pipelines);
            var result = new ResultTrxData();
            result.Version = date.ToString("dd-MMM-yyyy");
            result.WellItems = new List<WellItems>();
            foreach (var tx in aggregate)
            {

                var phase = BsonHelper.Deserialize<WellActivityPhase>(tx.GetDoc("Phase").ToBsonDocument());
                var history = BsonHelper.Deserialize<WellActivityPhase>(tx.GetDoc("Phase").ToBsonDocument());
                var WellName = tx.GetString("WellName").ToUpper();
                var SequenceId = tx.GetString("SequenceId").ToUpper();
                var ActivityType = phase.ActivityType.ToUpper();
                var RigName = tx.GetString("RigName");
                var PhaseNo = phase.PhaseNo;
                var weeklyreport = getWeeklyReport(WellName, SequenceId, ActivityType);
                var monthlyreport = getMonthlyReport(WellName, SequenceId, ActivityType);
                var pipreport = getPipReport(WellName, RigName, PhaseNo, SequenceId, ActivityType);

                var custome = new CustomeVariable(); 
                var historyreport = getHistoryReport(WellName, SequenceId, ActivityType);

                var IsWeekly = false;
                var DateWeekly = new DateTime(1900, 1, 1);


                if (weeklyreport != null)
                {
                    var weeklydates = weeklyreport.Select(x => x.UpdateVersion).ToList();
                    DateTime weekDate = GetNearDate(date, weeklydates);

                    if (!weekDate.Year.Equals(1900))
                    {
                        IsWeekly = true;
                        DateWeekly = weekDate;
                        WellActivityUpdate wellactivity = new WellActivityUpdate();
                        wellactivity = weeklyreport.FirstOrDefault(x => x.UpdateVersion.Date.Equals(weekDate.Date));
                        custome = SetCustomeValueWeekly(wellactivity,historyreport);
                    }
                    else
                    {
                        if (monthlyreport != null)
                        {
                            var monthlydates = monthlyreport.Select(x => x.UpdateVersion).ToList();
                            DateTime monthDate = GetNearDate(date, monthlydates);
                            WellActivityUpdateMonthly wellactivity = new WellActivityUpdateMonthly();
                            if (!monthDate.Year.Equals(1900))
                            {
                                wellactivity = monthlyreport.FirstOrDefault(x => x.UpdateVersion.Date.Equals(monthDate.Date));
                                custome = SetCustomeValueMonthly(wellactivity,historyreport);
                            }
                            else
                            {
                                if (pipreport != null)
                                {
                                    custome = SetCustomeValuePip(pipreport);
                                }

                            }
                        }
                    }

                }
                else if (monthlyreport != null)
                {
                    var monthlydates = monthlyreport.Select(x => x.UpdateVersion).ToList();
                    DateTime monthDate = GetNearDate(date, monthlydates);
                    WellActivityUpdateMonthly wellactivity = new WellActivityUpdateMonthly();
                    if (!monthDate.Year.Equals(1900))
                    {
                        //Well Activities
                        wellactivity = monthlyreport.FirstOrDefault(x => x.UpdateVersion.Date.Equals(monthDate.Date));
                        custome = SetCustomeValueMonthly(wellactivity,historyreport);
                    }
                    else
                    {
                        if (pipreport != null)
                        {
                            custome = SetCustomeValuePip(pipreport);
                        }
                    }
                }
                else
                {
                    if (pipreport != null)
                    {
                        custome = SetCustomeValuePip(pipreport);
                    }
                }

                WellItems wellitem = new WellItems();
                wellitem.ActivityType = ActivityType;
                wellitem.WellName = WellName;
                wellitem.BaseOP = phase.BaseOP.Count <= 0 ? "" : phase.BaseOP.FirstOrDefault().ToString();
                wellitem.RigName = RigName;
                wellitem.DateWeekly = DateWeekly;
                wellitem.IsWeekly = IsWeekly;

                if (CollectionName.ToLower().Equals("weiswellactivities_tr"))
                {
                    //Well Activities
                    wellitem.Items.Add(new WellDrillData { Identifier = "Current OP", Days = custome.OpDays, Cost = custome.OpCost });
                    wellitem.Items.Add(new WellDrillData { Identifier = "Previous OP", Days = custome.LastOpDays, Cost = custome.LastOpCost });
                    wellitem.Items.Add(new WellDrillData { Identifier = "LE", Days = custome.LeDays, Cost = custome.LeCost });
                    wellitem.Items.Add(new WellDrillData { Identifier = "LS", Days = custome.LsDays, Cost = custome.LsCost });
                    wellitem.Items.Add(new WellDrillData { Identifier = "AFE", Days = custome.AfeDays, Cost = custome.AfeCost });
                    wellitem.Items.Add(new WellDrillData { Identifier = "MLE", Days = custome.MleDays, Cost = custome.MleCost });
                }
                else if (CollectionName.ToLower().Equals("weiswellpips_tr_well"))//weiswellpips_tr_well
                {
                    //PIP
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Original Estimate", Days = custome.PlanPipDays, Cost = Tools.Div(custome.PlanPipCost, 1) });
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Current Estimate", Days = custome.LePipDays, Cost = Tools.Div(custome.LePipCost, 1) });
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Original Estimate Realize", Days = custome.PlanPipRealizedDays, Cost = Tools.Div(custome.PlanPipRealizedCost, 1) });
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Current Estimate Realize", Days = custome.LePipRealizedDays, Cost = Tools.Div(custome.LePipRealizedCost, 1) });
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Original Estimate UnRealize", Days = custome.PlanPipUnRealizedDays, Cost = Tools.Div(custome.PlanPipUnRealizedCost, 1) });
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Current Estimate UnRealize", Days = custome.LePipUnRealizedDays, Cost = Tools.Div(custome.LePipUnRealizedCost, 1) });
                }
                else if (CollectionName.ToLower().Equals("weiswellpips_tr_rig")) //weiswellpips_tr_rig
                {
                    //Rig PIP
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Original Estimate", Days = custome.PlanRigPipDays, Cost = Tools.Div(custome.PlanRigPipCost, 1) });
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Current Estimate", Days = custome.LeRigPipDays, Cost = Tools.Div(custome.LeRigPipCost, 1) });
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Original Estimate Realize", Days = custome.PlanRigPipRealizedDays, Cost = Tools.Div(custome.PlanRigPipRealizedCost, 1) });
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Current Estimate Realize", Days = custome.LeRigPipRealizedDays, Cost = Tools.Div(custome.LeRigPipRealizedCost, 1) });
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Original Estimate UnRealize", Days = custome.PlanRigPipUnRealizedDays, Cost = Tools.Div(custome.PlanRigPipUnRealizedCost, 1) });
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Current Estimate UnRealize", Days = custome.LeRigPipUnRealizedDays, Cost = Tools.Div(custome.LeRigPipUnRealizedCost, 1) });
                    
                }
                result.WellItems.Add(wellitem);
            }
            return result;
        }

        public JsonResult GetDataWellPlanAndPip2(DateTime parmDate, string parmType, int parmRolling, string parmOP, string parmResultType, List<string> wellNames)
        {
            return MvcResultInfo.Execute(() =>
            {
                string PreviousOP= "";
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

                    Periods = cfg.GetlastDates(parmDate, 7, true);

                    //Periods = cfg.GetDailyReserveDate();
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

                var AllWellPlans = new List<ResultTrxData>();
                var AllPIP = new List<ResultTrxData>();
                var AllRigPIP = new List<ResultTrxData>();

                List<int> per = new List<int>();
                per = Periods.Select(x => Convert.ToInt32(x.ToString("yyyyMMdd"))).ToList<int>();
                var arrstr = new List<string>();
                if (wellNames != null)
                {
                    foreach (var text in wellNames)
                    {
                        arrstr.Add("'" + text + "'");
                    }
                }
                
                string project1 = "{$project:{'LastUpdate':'$LastUpdate','Phases':'$Phases','WellName':'$WellName','RigName':'$RigName','SequenceId':'$UARigSequenceId','History':'$OPHistories'}}";
                string unwind = @"{$unwind:'$Phases'}";
                string project2 = @"{$project:{'LastUpdate':'$LastUpdate','WellName':'$WellName','RigName':'$RigName','Phases':'$Phases','SequenceId':'$SequenceId','History':'$History'}}";
                string match = "";
                if(wellNames != null)
                    match = @"{$match:{'WellName':{'$in': " + new BsonArray(arrstr) + " }}}";

                string sorts = @"{$sort:{'LastUpdate':" + -1 + "}}";

                string project3 = @"{$project:{'LastUpdate':'$LastUpdate','Phase':'$Phases','WellName':'$WellName','SequenceId':'$SequenceId','RigName':'$RigName','History':'$History'}}";
                string limit = @"{ $limit: " + 50 + " }";

                

                List<BsonDocument> pipelines = new List<BsonDocument>();
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(project1));
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(unwind));
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(project2));
                if (wellNames != null)
                    pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(match));
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(sorts));
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(project3));
                //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(limit));
                List<BsonDocument> aggregate = DataHelper.Aggregate(new WellActivity().TableName, pipelines);

                List<WellItems> items = new List<WellItems>();
                #region Detail Well Plan
                foreach (var i in Periods)
                {
                    var version = i.ToString("dd-MMM-yyyy");
                    var eachWellPlan = new ResultTrxData();
                    var eachPip = new ResultTrxData();
                    var eachRigPip = new ResultTrxData();

                    var alleachWellPlan = new ResultTrxData();
                    var alleachPip = new ResultTrxData();
                    var alleachRigPip = new ResultTrxData();
                    if (parmType.ToLower().Contains("monthly") || parmType.ToLower().Contains("quarterly"))
                    {
                        version = i.ToString("MMM-yyyy");
                    }

                    var dataWellPlans = aggregate;

                    var SumOpDays = 0.0; var SumOpCost = 0.0;
                    var SumLastOpDays = 0.0; var SumLastOpCost = 0.0;
                    var SumLeDays = 0.0; var SumLeCost = 0.0;
                    var SumLsDays = 0.0; var SumLsCost = 0.0;
                    var SumAfeDays = 0.0; var SumAfeCost = 0.0;
                    var SumMleDays = 0.0; var SumMleCost = 0.0;
                    //PIP
                    var SumPlanDaysPip = 0.0; var SumPlanCostPip = 0.0;
                    var SumLeDaysPip = 0.0; var SumLeCostPip = 0.0;

                    var SumPlanDaysRealizePip = 0.0; var SumPlanCostRealizePip = 0.0;
                    var SumLeDaysRealizePip = 0.0; var SumLeCostRealizePip = 0.0;

                    var SumPlanDaysUnRealizePip = 0.0; var SumPlanCostUnRealizePip = 0.0;
                    var SumLeDaysUnRealizePip = 0.0; var SumLeCostUnRealizePip = 0.0;

                    //Rig PIP
                    var SumPlanDaysRigPip = 0.0; var SumPlanCostRigPip = 0.0;
                    var SumLeDaysRigPip = 0.0; var SumLeCostRigPip = 0.0;

                    var SumPlanDaysRealizeRigPip = 0.0; var SumPlanCostRealizeRigPip = 0.0;
                    var SumLeDaysRealizeRigPip = 0.0; var SumLeCostRealizeRigPip = 0.0;

                    var SumPlanDaysUnRealizeRigPip = 0.0; var SumPlanCostUnRealizeRigPip = 0.0;
                    var SumLeDaysUnRealizeRigPip = 0.0; var SumLeCostUnRealizeRigPip = 0.0;

                    eachWellPlan.Version = version;
                    eachPip.Version = version;
                    eachRigPip.Version = version;

                    alleachWellPlan.Version = version;
                    alleachPip.Version = version;
                    alleachRigPip.Version = version;

                    foreach (var tx in dataWellPlans)
                    {

                        var phase = BsonHelper.Deserialize<WellActivityPhase>(tx.GetDoc("Phase").ToBsonDocument());                        
                        
                        var WellName = tx.GetString("WellName").ToUpper();
                        var SequenceId = tx.GetString("SequenceId").ToUpper();
                        var ActivityType = phase.ActivityType.ToUpper();
                        var RigName = tx.GetString("RigName");
                        var PhaseNo = phase.PhaseNo;
                        var weeklyreport = getWeeklyReport(WellName, SequenceId, ActivityType);
                        var monthlyreport = getMonthlyReport(WellName, SequenceId, ActivityType);
                        var pipreport = getPipReport(WellName, RigName, PhaseNo, SequenceId, ActivityType);
                        var historyreport = getHistoryReport(WellName, SequenceId, ActivityType);
                        var custome = new CustomeVariable();

                        var IsWeekly = false;
                        var DateWeekly = new DateTime(1900,1,1);

                        if (weeklyreport != null)
                        {
                            var weeklydates = weeklyreport.Select(x => x.UpdateVersion).ToList();
                            DateTime weekDate = GetNearDate(i, weeklydates);

                            if (!weekDate.Year.Equals(1900))
                            {
                                IsWeekly = true;
                                DateWeekly = weekDate;
                                WellActivityUpdate wellactivity = new WellActivityUpdate();
                                wellactivity = weeklyreport.FirstOrDefault(x => x.UpdateVersion.Date.Equals(weekDate.Date));
                                custome = SetCustomeValueWeekly(wellactivity, historyreport);
                            }
                            else
                            {
                                if (monthlyreport != null)
                                {
                                    var monthlydates = monthlyreport.Select(x => x.UpdateVersion).ToList();
                                    DateTime monthDate = GetNearDate(i, monthlydates);
                                    WellActivityUpdateMonthly wellactivity = new WellActivityUpdateMonthly();
                                    if (!monthDate.Year.Equals(1900))
                                    {
                                        wellactivity = monthlyreport.FirstOrDefault(x => x.UpdateVersion.Date.Equals(monthDate.Date));
                                        custome = SetCustomeValueMonthly(wellactivity, historyreport);
                                    }
                                    else
                                    {
                                        if (pipreport != null)
                                        {
                                            custome = SetCustomeValuePip(pipreport);
                                        }

                                    }
                                }
                            }

                        }
                        else if (monthlyreport != null)
                        {
                            var monthlydates = monthlyreport.Select(x => x.UpdateVersion).ToList();
                            DateTime monthDate = GetNearDate(i, monthlydates);
                            WellActivityUpdateMonthly wellactivity = new WellActivityUpdateMonthly();
                            if (!monthDate.Year.Equals(1900))
                            {
                                //Well Activities
                                wellactivity = monthlyreport.FirstOrDefault(x => x.UpdateVersion.Date.Equals(monthDate.Date));
                                custome = SetCustomeValueMonthly(wellactivity, historyreport);
                            }
                            else
                            {
                                if (pipreport != null)
                                {
                                    custome = SetCustomeValuePip(pipreport);
                                }
                            }
                        }
                        else
                        {
                            if (pipreport != null)
                            {
                                custome = SetCustomeValuePip(pipreport);
                            }
                        }
                        #region All Item
                        WellItems allwellitem = new WellItems();
                        WellItems allpipitem = new WellItems();
                        WellItems allrigpipitem = new WellItems();

                        allwellitem.ActivityType = ActivityType;
                        allwellitem.WellName = WellName;
                        allwellitem.BaseOP = phase.BaseOP.Count <= 0 ? "" : phase.BaseOP.FirstOrDefault().ToString();
                        allwellitem.RigName = RigName;
                        allwellitem.DateWeekly = DateWeekly;
                        allwellitem.IsWeekly = IsWeekly;

                        allpipitem.ActivityType = ActivityType;
                        allpipitem.WellName = WellName;
                        allpipitem.BaseOP = phase.BaseOP.Count <= 0 ? "" : phase.BaseOP.FirstOrDefault().ToString();
                        allpipitem.RigName = RigName;
                        allpipitem.DateWeekly = DateWeekly;
                        allpipitem.IsWeekly = IsWeekly;

                        allrigpipitem.ActivityType = ActivityType;
                        allrigpipitem.WellName = WellName;
                        allrigpipitem.BaseOP = phase.BaseOP.Count <= 0 ? "" : phase.BaseOP.FirstOrDefault().ToString();
                        allrigpipitem.RigName = RigName;
                        allrigpipitem.DateWeekly = DateWeekly;
                        allrigpipitem.IsWeekly = IsWeekly;

                        
                        allwellitem.Items.Add(new WellDrillData { Identifier = "Current OP", Days = custome.OpDays, Cost = custome.OpCost });
                        allwellitem.Items.Add(new WellDrillData { Identifier = "Previous OP", Days = custome.LastOpDays, Cost = custome.LastOpCost });
                        allwellitem.Items.Add(new WellDrillData { Identifier = "LE", Days = custome.LeDays, Cost = custome.LeCost });
                        allwellitem.Items.Add(new WellDrillData { Identifier = "LS", Days = custome.LsDays, Cost = custome.LsCost });
                        allwellitem.Items.Add(new WellDrillData { Identifier = "AFE", Days = custome.AfeDays, Cost = custome.AfeCost });
                        allwellitem.Items.Add(new WellDrillData { Identifier = "MLE", Days = custome.MleDays, Cost = custome.MleCost });

                        alleachWellPlan.WellItems.Add(allwellitem);

                        //PIP
                        allpipitem.Items = new List<WellDrillData>();
                        allpipitem.Items.Add(new WellDrillData() { Identifier = "Original Estimate", Days = custome.PlanPipDays, Cost = Tools.Div(custome.PlanPipCost, 1) });
                        allpipitem.Items.Add(new WellDrillData() { Identifier = "Current Estimate", Days = custome.LePipDays, Cost = Tools.Div(custome.LePipCost, 1) });
                        allpipitem.Items.Add(new WellDrillData() { Identifier = "Original Estimate Realize", Days = custome.PlanPipRealizedDays, Cost = Tools.Div(custome.PlanPipRealizedCost, 1) });
                        allpipitem.Items.Add(new WellDrillData() { Identifier = "Current Estimate Realize", Days = custome.LePipRealizedDays, Cost = Tools.Div(custome.LePipRealizedCost, 1) });
                        allpipitem.Items.Add(new WellDrillData() { Identifier = "Original Estimate UnRealize", Days = custome.PlanPipUnRealizedDays, Cost = Tools.Div(custome.PlanPipUnRealizedCost, 1) });
                        allpipitem.Items.Add(new WellDrillData() { Identifier = "Current Estimate UnRealize", Days = custome.LePipUnRealizedDays, Cost = Tools.Div(custome.LePipUnRealizedCost, 1) });
                        alleachPip.WellItems.Add(allpipitem);

                        //Rig PIP
                        allrigpipitem.Items = new List<WellDrillData>();
                        allrigpipitem.Items.Add(new WellDrillData() { Identifier = "Original Estimate", Days = custome.PlanRigPipDays, Cost = Tools.Div(custome.PlanRigPipCost, 1) });
                        allrigpipitem.Items.Add(new WellDrillData() { Identifier = "Current Estimate", Days = custome.LeRigPipDays, Cost = Tools.Div(custome.LeRigPipCost, 1) });
                        allrigpipitem.Items.Add(new WellDrillData() { Identifier = "Original Estimate Realize", Days = custome.PlanRigPipRealizedDays, Cost = Tools.Div(custome.PlanRigPipRealizedCost, 1) });
                        allrigpipitem.Items.Add(new WellDrillData() { Identifier = "Current Estimate Realize", Days = custome.LeRigPipRealizedDays, Cost = Tools.Div(custome.LeRigPipRealizedCost, 1) });
                        allrigpipitem.Items.Add(new WellDrillData() { Identifier = "Original Estimate UnRealize", Days = custome.PlanRigPipUnRealizedDays, Cost = Tools.Div(custome.PlanRigPipUnRealizedCost, 1) });
                        allrigpipitem.Items.Add(new WellDrillData() { Identifier = "Current Estimate UnRealize", Days = custome.LeRigPipUnRealizedDays, Cost = Tools.Div(custome.LeRigPipUnRealizedCost, 1) });
                        alleachRigPip.WellItems.Add(allrigpipitem);

                        #endregion


                        #region Populate Summary
                        //Well Activities
                        SumOpDays += custome.OpDays; SumOpCost += custome.OpCost;
                        SumLastOpDays += custome.LastOpDays; SumLastOpCost += custome.LastOpCost;
                        SumLeDays += custome.LeDays; SumLeCost += custome.LeCost;
                        SumLsDays += custome.LsDays; SumLsCost += custome.LsCost;
                        SumAfeDays += custome.AfeDays; SumAfeCost += custome.AfeCost;
                        SumMleDays += custome.MleDays; SumMleCost += custome.MleCost;

                        //PIP
                        SumPlanDaysPip += custome.PlanPipDays; SumPlanCostPip = custome.PlanPipCost;
                        SumLeDaysPip += custome.LePipDays; SumLeCostPip = custome.LePipCost;

                        SumPlanDaysRealizePip += custome.PlanPipRealizedDays; SumPlanCostRealizePip += custome.PlanPipRealizedCost;
                        SumLeDaysRealizePip += custome.LePipRealizedDays; SumLeCostRealizePip += custome.LePipRealizedCost;

                        SumPlanDaysUnRealizePip += custome.PlanPipUnRealizedDays; SumPlanCostUnRealizePip += custome.PlanPipUnRealizedCost;
                        SumLeDaysUnRealizePip += custome.LePipUnRealizedDays; SumLeCostUnRealizePip += custome.LePipUnRealizedCost;

                        //Rig PIP
                        SumPlanDaysRigPip += custome.PlanRigPipDays; SumPlanCostRigPip += custome.PlanRigPipCost;
                        SumLeDaysRigPip += custome.LeRigPipDays; SumLeCostRigPip += custome.LeRigPipCost;

                        SumPlanDaysRealizeRigPip += custome.PlanRigPipRealizedDays; SumPlanCostRealizeRigPip += custome.PlanRigPipRealizedCost;
                        SumLeDaysRealizeRigPip += custome.LeRigPipRealizedDays; SumLeCostRealizeRigPip += custome.LeRigPipRealizedCost;

                        SumPlanDaysUnRealizeRigPip += custome.PlanRigPipUnRealizedDays; SumPlanCostUnRealizeRigPip += custome.PlanRigPipUnRealizedCost;
                        SumLeDaysUnRealizeRigPip += custome.LeRigPipUnRealizedDays; SumLeCostUnRealizeRigPip += custome.LeRigPipUnRealizedCost;

                        #endregion
                        
                    }
                #endregion

                    #region Summary Well Plan

                    var WP_items = new List<WellDrillDataForDL>();
                    WP_items.Add(new WellDrillDataForDL() { /*Title = parmOP */ Title = "Current OP", Days = SumOpDays, Cost = Tools.Div(SumOpCost, Division) });
                    WP_items.Add(new WellDrillDataForDL() { /*Title = parmOP */ Title = "Previous OP", Days = SumLastOpDays, Cost = Tools.Div(SumLastOpCost, Division) });
                    WP_items.Add(new WellDrillDataForDL() { Title = "LE", Days = SumLeDays, Cost = Tools.Div(SumLeCost, Division) });
                    WP_items.Add(new WellDrillDataForDL() { Title = "LS", Days = SumLsDays, Cost = Tools.Div(SumLsCost, Division) });
                    WP_items.Add(new WellDrillDataForDL() { Title = "AFE", Days = SumAfeDays, Cost = Tools.Div(SumAfeCost, Division) });
                    WP_items.Add(new WellDrillDataForDL() { Title = "MLE", Days = SumMleDays, Cost = Tools.Div(SumMleCost, Division) });
                    eachWellPlan.Items = WP_items;
                    alleachWellPlan.Items = WP_items;
                    #endregion

                    #region Summary PIP
                    var PIP_items = new List<WellDrillDataForDL>();
                    PIP_items.Add(new WellDrillDataForDL() { Title = "Original Estimate", Days = SumPlanDaysPip, Cost = Tools.Div(SumPlanCostPip, 1) });
                    PIP_items.Add(new WellDrillDataForDL() { Title = "Current Estimate", Days = SumLeDaysPip, Cost = Tools.Div(SumLeCostPip, 1) });
                    PIP_items.Add(new WellDrillDataForDL() { Title = "Original Estimate Realize", Days = SumPlanDaysRealizePip, Cost = Tools.Div(SumPlanCostRealizePip, 1) });
                    PIP_items.Add(new WellDrillDataForDL() { Title = "Current Estimate Realize", Days = SumLeDaysRealizePip, Cost = Tools.Div(SumLeCostRealizePip, 1) });
                    PIP_items.Add(new WellDrillDataForDL() { Title = "Original Estimate UnRealize", Days = SumPlanDaysUnRealizePip, Cost = Tools.Div(SumPlanCostUnRealizePip, 1) });
                    PIP_items.Add(new WellDrillDataForDL() { Title = "Current Estimate UnRealize", Days = SumLeDaysUnRealizePip, Cost = Tools.Div(SumLeCostUnRealizePip, 1) });
                    eachPip.Items = PIP_items;
                    alleachPip.Items = PIP_items;
                    #endregion

                    #region Summary Rig PIP
                    PIP_items = new List<WellDrillDataForDL>();
                    PIP_items.Add(new WellDrillDataForDL() { Title = "Original Estimate", Days = SumPlanDaysRigPip, Cost = Tools.Div(SumPlanCostRigPip, 1) });
                    PIP_items.Add(new WellDrillDataForDL() { Title = "Current Estimate", Days = SumLeDaysRigPip, Cost = Tools.Div(SumLeCostRigPip, 1) });
                    PIP_items.Add(new WellDrillDataForDL() { Title = "Original Estimate Realize", Days = SumPlanDaysRealizeRigPip, Cost = Tools.Div(SumPlanCostRealizeRigPip, 1) });
                    PIP_items.Add(new WellDrillDataForDL() { Title = "Current Estimate Realize", Days = SumLeDaysRealizeRigPip, Cost = Tools.Div(SumLeCostRealizeRigPip, 1) });
                    PIP_items.Add(new WellDrillDataForDL() { Title = "Original Estimate UnRealize", Days = SumPlanDaysUnRealizeRigPip, Cost = Tools.Div(SumPlanCostUnRealizeRigPip, 1) });
                    PIP_items.Add(new WellDrillDataForDL() { Title = "Current Estimate UnRealize", Days = SumLeDaysUnRealizeRigPip, Cost = Tools.Div(SumLeCostUnRealizeRigPip, 1) });
                    eachRigPip.Items = PIP_items;
                    alleachRigPip.Items = PIP_items;
                    #endregion

                    //WellPlans.Add(eachWellPlan);
                    //PIP.Add(eachPip);
                    //RigPIP.Add(eachRigPip);

                    AllWellPlans.Add(alleachWellPlan);
                    AllPIP.Add(alleachPip);
                    AllRigPIP.Add(alleachRigPip);
                }

                var WellPlans = new List<ResultTrxData>();
                var PIP = new List<ResultTrxData>();
                var RigPIP = new List<ResultTrxData>();
                
                foreach (var i in Periods)
                {
                    
                    #region Top TEN
                    ResultTrxData well = new ResultTrxData();
                    ResultTrxData pip = new ResultTrxData();
                    ResultTrxData rigpip = new ResultTrxData();
                    
                    well = FilterEvent(AllWellPlans.FirstOrDefault(x => x.Version.Equals(i.ToString("dd-MMM-yyyy")) ), "well");
                    pip = FilterEvent(AllPIP.FirstOrDefault(x => x.Version.Equals(i.ToString("dd-MMM-yyyy"))), "pip");
                    rigpip = FilterEvent(AllRigPIP.FirstOrDefault(x => x.Version.Equals(i.ToString("dd-MMM-yyyy"))), "rigpip");


                    WellPlans.Add(well);
                    PIP.Add(pip);
                    RigPIP.Add(rigpip);
                    #endregion
                }

                return new { Periods = Periods.Distinct().Select(x => x.Date.ToString("dd-MMM-yyyy")).ToList(), WellPlans, PIP, RigPIP, AllWellPlans, AllPIP, AllRigPIP };
            });


        }

        public JsonResult GetDataWellPlanGroupBy(string parmResultType)
        {
            return MvcResultInfo.Execute(() =>
            {
                string PreviousOP = "";
                string NextOP = "";
                string DefaultOP = getBaseOP(out PreviousOP, out NextOP);
                var Division = 1000000.00;
                var parmRolling = 7;
                var Periods = new List<DateTime>();
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
                var parmDate = Tools.ToUTC(DateTime.Now);
                #region query builder
                cfg.DateFilter = parmDate;
                if (parmResultType.ToLower().Contains("days"))
                {
                    cfg.NoOfDays = parmRolling;

                    Periods = cfg.GetlastDates(parmDate, 7, true);

                    //Periods = cfg.GetDailyReserveDate();
                    Periods = Periods.OrderByDescending(x => x.Date).ToList();
                }
                else
                {
                    if (parmResultType.ToLower().Contains("months"))
                    {
                        cfg.NoOfMonth = parmRolling;
                        Periods = cfg.GetlastMonths(parmDate, 7, true);
                        Periods = Periods.OrderByDescending(x => x.Date).ToList();
                    }
                    else if (parmResultType.ToLower().Contains("weeks"))
                    {
                        cfg.NoOfWeek = parmRolling;
                        Periods = cfg.GetlastWeeks(cfg.WeekType.ToString(), parmDate);
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


                var WellPlans = new List<ResultTrxData>();
                var RigPlans = new List<ResultTrxData>();
                //Well Activities
                var populatedetailwell = DataHelper.Populate("_WEISCollectionSDLByWellName", null, 10).OrderByDescending(x => x.GetString("WellName")).ToList();
                var populatedetailrig = DataHelper.Populate("_WEISCollectionSDLByRigName", null, 10).OrderByDescending(x => x.GetString("RigName")).ToList();
                var populatesummary = DataHelper.Populate("_WEISCollectionSummarySDL", Query.And(Query.In("Date", new BsonArray(Periods)))).OrderByDescending(x => x.GetString("Date")).ToList();
                foreach (var pr in Periods)
                {

                    var priod = Tools.ToUTC(new DateTime(pr.Year, pr.Month, pr.Day, 0, 0, 0));
                    ResultTrxData well = new ResultTrxData();
                    well.Version = pr.ToString("dd-MMM-yyyy");                    
                    var wellitems = new List<WellDrillDataForDL>();
                    foreach (var sum in populatesummary)
                    {
                        var sumDate = BsonHelper.GetDateTime(sum, "Date");

                        if (sumDate.Equals(priod))
                        {
                            var sumdeserialize = BsonHelper.Deserialize<SDLSummary>(sum);
                            //Well
                            wellitems.Add(new WellDrillDataForDL() { Identifier = sumdeserialize.WRDetails.WRPlan.Identifier, Days = sumdeserialize.WRDetails.WRPlan.Days, Cost = sumdeserialize.WRDetails.WRPlan.Cost });
                            wellitems.Add(new WellDrillDataForDL() { Identifier = sumdeserialize.WRDetails.WRPrevPlan.Identifier, Days = sumdeserialize.WRDetails.WRPrevPlan.Days, Cost = sumdeserialize.WRDetails.WRPrevPlan.Cost });
                            wellitems.Add(new WellDrillDataForDL() { Identifier = sumdeserialize.WRDetails.WROP.Identifier, Days = sumdeserialize.WRDetails.WROP.Days, Cost = sumdeserialize.WRDetails.WROP.Cost });
                            wellitems.Add(new WellDrillDataForDL() { Identifier = sumdeserialize.WRDetails.WRLE.Identifier, Days = sumdeserialize.WRDetails.WRLE.Days, Cost = sumdeserialize.WRDetails.WRLE.Cost });
                            wellitems.Add(new WellDrillDataForDL() { Identifier = sumdeserialize.WRDetails.WRAFE.Identifier, Days = sumdeserialize.WRDetails.WRAFE.Days, Cost = sumdeserialize.WRDetails.WRAFE.Cost });
                            break;
                        }
                    }
                    well.Items = wellitems;

                    ResultTrxData rig = new ResultTrxData();
                    rig.Version = pr.ToString("dd-MMM-yyyy");
                    rig.Items = wellitems;

                    List<WellItems> WellItems = new List<WellItems>();
                    foreach (var dtl in populatedetailwell)
                    {
                        var detail = BsonHelper.Deserialize<SDLHelper>(dtl);
                        //Well
                        var wells = new WellItems();
                        wells.ActivityType = detail.ActivityType;
                        wells.BaseOP = detail.BaseOP.FirstOrDefault();
                        wells.DateWeekly = detail.LastWeek;
                        wells.IsWeekly = false;
                        wells.RigName = detail.RigName;
                        wells.WellName = detail.WellName;

                        wells.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPlan.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPlan.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPlan.Cost });

                        wells.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPrevPlan.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPrevPlan.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPrevPlan.Cost });

                        wells.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRLE.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRLE.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRLE.Cost });

                        wells.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WROP.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WROP.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WROP.Cost });

                        wells.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRAFE.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRAFE.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRAFE.Cost });
                        WellItems.Add(wells);

                    }
                    well.WellItems = WellItems;


                    List<WellItems> RigItems = new List<WellItems>();
                    foreach (var dtl in populatedetailrig)
                    {
                        var detail = BsonHelper.Deserialize<SDLHelper>(dtl);
                        //Well
                        var rigs = new WellItems();
                        rigs.ActivityType = detail.ActivityType;
                        rigs.BaseOP = detail.BaseOP.FirstOrDefault();
                        rigs.DateWeekly = detail.LastWeek;
                        rigs.IsWeekly = false;
                        rigs.RigName = detail.RigName;
                        rigs.WellName = detail.WellName;

                        rigs.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPlan.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPlan.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPlan.Cost });

                        rigs.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPrevPlan.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPrevPlan.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPrevPlan.Cost });

                        rigs.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRLE.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRLE.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRLE.Cost });

                        rigs.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WROP.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WROP.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WROP.Cost });

                        rigs.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRAFE.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRAFE.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRAFE.Cost });
                        RigItems.Add(rigs);

                    }
                    rig.WellItems = RigItems;


                    WellPlans.Add(well);
                    RigPlans.Add(rig);
                    //break;
                }


                return new { Periods = Periods.Distinct().Select(x => x.Date.ToString("dd-MMM-yyyy")).ToList(), WellPlans, RigPlans };
            });


        }

        public JsonResult GetDataWellPlanAndPip(string parmResultType)
        {
            return MvcResultInfo.Execute(() =>
            {
                string PreviousOP= "";
                string NextOP = "";
                string DefaultOP = getBaseOP(out PreviousOP, out NextOP);
                var Division = 1000000.00;
                var parmRolling = 7;
                var Periods = new List<DateTime>();
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
                var parmDate = Tools.ToUTC(DateTime.Now);
                #region query builder
                cfg.DateFilter = parmDate;
                if (parmResultType.ToLower().Contains("days"))
                {
                    cfg.NoOfDays = parmRolling;

                    Periods = cfg.GetlastDates(parmDate, 7, true);

                    //Periods = cfg.GetDailyReserveDate();
                    Periods = Periods.OrderByDescending(x => x.Date).ToList();
                }
                else
                {
                    if (parmResultType.ToLower().Contains("months"))
                    {
                        cfg.NoOfMonth = parmRolling;
                        Periods = cfg.GetlastMonths(parmDate, 7, true);
                        Periods = Periods.OrderByDescending(x => x.Date).ToList();
                    }
                    else if (parmResultType.ToLower().Contains("weeks"))
                    {
                        cfg.NoOfWeek = parmRolling;
                        Periods = cfg.GetlastWeeks(cfg.WeekType.ToString(), parmDate);
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


                var WellPlans = new List<ResultTrxData>();
                var PIP = new List<ResultTrxData>();
                var RigPIP = new List<ResultTrxData>();
                //Well Activities
                var populatedetail = DataHelper.Populate("_WEISCollectionSDL",null,10).OrderByDescending(x=>x.GetString("WellName")).ToList();
                var populatesummary = DataHelper.Populate("_WEISCollectionSummarySDL", Query.And(Query.In("Date", new BsonArray(Periods)))).OrderByDescending(x => x.GetString("Date")).ToList();
                foreach (var pr in Periods.OrderByDescending(x=>x.Date).ToList())
                {

                    var priod = Tools.ToUTC(new DateTime(pr.Year, pr.Month, pr.Day, 0, 0, 0));
                    ResultTrxData well = new ResultTrxData();
                    well.Version = pr.ToString("dd-MMM-yyyy");

                    ResultTrxData pip = new ResultTrxData();
                    pip.Version = pr.ToString("dd-MMM-yyyy");

                    ResultTrxData rig = new ResultTrxData();
                    rig.Version = pr.ToString("dd-MMM-yyyy");

                    var items = new List<WellDrillDataForDL>();
                    var pipitems = new List<WellDrillDataForDL>();
                    var rigpipitems = new List<WellDrillDataForDL>();
                    //ISODate('1900-01-01T00:00:00.000+0000')
                    foreach (var sum in populatesummary)
                    {
                        var sumDate = BsonHelper.GetDateTime(sum, "Date");

                        if (sumDate.Equals(priod))
                        {
                            var sumdeserialize = BsonHelper.Deserialize<SDLSummary>(sum);
                            //Well
                            items.Add(new WellDrillDataForDL() { Identifier = sumdeserialize.WRDetails.WRPlan.Identifier, Days = sumdeserialize.WRDetails.WRPlan.Days, Cost = sumdeserialize.WRDetails.WRPlan.Cost});
                            items.Add(new WellDrillDataForDL() { Identifier = sumdeserialize.WRDetails.WRPrevPlan.Identifier, Days = sumdeserialize.WRDetails.WRPrevPlan.Days, Cost = sumdeserialize.WRDetails.WRPrevPlan.Cost });
                            items.Add(new WellDrillDataForDL() { Identifier = sumdeserialize.WRDetails.WROP.Identifier, Days = sumdeserialize.WRDetails.WROP.Days, Cost = sumdeserialize.WRDetails.WROP.Cost });
                            items.Add(new WellDrillDataForDL() { Identifier = sumdeserialize.WRDetails.WRLE.Identifier, Days = sumdeserialize.WRDetails.WRLE.Days, Cost = sumdeserialize.WRDetails.WRLE.Cost });
                            items.Add(new WellDrillDataForDL() { Identifier = sumdeserialize.WRDetails.WRAFE.Identifier, Days = sumdeserialize.WRDetails.WRAFE.Days, Cost = sumdeserialize.WRDetails.WRAFE.Cost });
                            //PIP
                            pipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.PIPDetails.OriEst.Identifier, Days = sumdeserialize.PIPDetails.OriEst.Days, Cost = sumdeserialize.PIPDetails.OriEst.Cost });
                            pipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.PIPDetails.CurEst.Identifier, Days = sumdeserialize.PIPDetails.CurEst.Days, Cost = sumdeserialize.PIPDetails.CurEst.Cost });
                            pipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.PIPDetails.OriEstRealize.Identifier, Days = sumdeserialize.PIPDetails.OriEstRealize.Days, Cost = sumdeserialize.PIPDetails.OriEstRealize.Cost });
                            pipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.PIPDetails.CurEstRealize.Identifier, Days = sumdeserialize.PIPDetails.CurEstRealize.Days, Cost = sumdeserialize.PIPDetails.CurEstRealize.Cost });
                            pipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.PIPDetails.OriEstUnRealize.Identifier, Days = sumdeserialize.PIPDetails.OriEstUnRealize.Days, Cost = sumdeserialize.PIPDetails.OriEstUnRealize.Cost });
                            pipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.PIPDetails.CurEstUnRealize.Identifier, Days = sumdeserialize.PIPDetails.CurEstUnRealize.Days, Cost = sumdeserialize.PIPDetails.CurEstUnRealize.Cost });
                            //Rig PIP
                            rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.RigPIPDetails.OriEst.Identifier, Days = sumdeserialize.RigPIPDetails.OriEst.Days, Cost = sumdeserialize.RigPIPDetails.OriEst.Cost });
                            rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.RigPIPDetails.CurEst.Identifier, Days = sumdeserialize.RigPIPDetails.CurEst.Days, Cost = sumdeserialize.RigPIPDetails.CurEst.Cost });
                            rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.RigPIPDetails.OriEstRealize.Identifier, Days = sumdeserialize.RigPIPDetails.OriEstRealize.Days, Cost = sumdeserialize.RigPIPDetails.OriEstRealize.Cost });
                            rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.RigPIPDetails.CurEstRealize.Identifier, Days = sumdeserialize.RigPIPDetails.CurEstRealize.Days, Cost = sumdeserialize.RigPIPDetails.CurEstRealize.Cost });
                            rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.RigPIPDetails.OriEstUnRealize.Identifier, Days = sumdeserialize.RigPIPDetails.OriEstUnRealize.Days, Cost = sumdeserialize.RigPIPDetails.OriEstUnRealize.Cost });
                            rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.RigPIPDetails.CurEstUnRealize.Identifier, Days = sumdeserialize.RigPIPDetails.CurEstUnRealize.Days, Cost = sumdeserialize.RigPIPDetails.CurEstUnRealize.Cost });
                           
                        }
                    }
                    well.Items = items;
                    pip.Items = pipitems;
                    rig.Items = rigpipitems;

                    List<WellItems> wellitems = new List<WellItems>();
                    List<WellItems> pipwellitems = new List<WellItems>();
                    List<WellItems> rigwellitems = new List<WellItems>();
                    foreach (var dtl in populatedetail)
                    {
                        var detail = BsonHelper.Deserialize<SDLHelper>(dtl);
                        //Well
                        var wells = new WellItems();
                        wells.ActivityType = detail.ActivityType;
                        wells.BaseOP = detail.BaseOP.FirstOrDefault();
                        wells.DateWeekly = detail.LastWeek;
                        wells.IsWeekly = false;
                        wells.RigName = detail.RigName;
                        wells.WellName = detail.WellName;

                        wells.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPlan.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPlan.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPlan.Cost });

                        wells.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPrevPlan.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPrevPlan.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPrevPlan.Cost });

                        wells.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRLE.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRLE.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRLE.Cost });

                        wells.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WROP.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WROP.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WROP.Cost });

                        wells.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRAFE.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRAFE.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRAFE.Cost });
                        wellitems.Add(wells);


                        //PIP
                        var pips = new WellItems();
                        pips.ActivityType = detail.ActivityType;
                        pips.BaseOP = detail.BaseOP.FirstOrDefault();
                        pips.DateWeekly = detail.LastWeek;
                        pips.IsWeekly = false;
                        pips.RigName = detail.RigName;
                        pips.WellName = detail.WellName;

                        pips.Items.Add(new WellDrillData() { Identifier = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEst.Identifier, Days = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEst.Days, Cost = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEst.Cost });

                        pips.Items.Add(new WellDrillData() { Identifier = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEst.Identifier, Days = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEst.Days, Cost = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEst.Cost });

                        pips.Items.Add(new WellDrillData() { Identifier = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstRealize.Identifier, Days = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstRealize.Days, Cost = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstRealize.Cost });

                        pips.Items.Add(new WellDrillData() { Identifier = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstRealize.Identifier, Days = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstRealize.Days, Cost = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstRealize.Cost });

                        pips.Items.Add(new WellDrillData() { Identifier = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstUnRealize.Identifier, Days = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstUnRealize.Days, Cost = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstUnRealize.Cost });

                        pips.Items.Add(new WellDrillData() { Identifier = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstUnRealize.Identifier, Days = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstUnRealize.Days, Cost = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstUnRealize.Cost });

                        pipwellitems.Add(pips);



                        //Rig PIP
                        var rigs = new WellItems();
                        rigs.ActivityType = detail.ActivityType;
                        rigs.BaseOP = detail.BaseOP.FirstOrDefault();
                        rigs.DateWeekly = detail.LastWeek;
                        rigs.IsWeekly = false;
                        rigs.RigName = detail.RigName;
                        rigs.WellName = detail.WellName;

                        rigs.Items.Add(new WellDrillData() { Identifier = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEst.Identifier, Days = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEst.Days, Cost = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEst.Cost });

                        rigs.Items.Add(new WellDrillData() { Identifier = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEst.Identifier, Days = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEst.Days, Cost = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEst.Cost });

                        rigs.Items.Add(new WellDrillData() { Identifier = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstRealize.Identifier, Days = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstRealize.Days, Cost = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstRealize.Cost });

                        rigs.Items.Add(new WellDrillData() { Identifier = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstRealize.Identifier, Days = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstRealize.Days, Cost = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstRealize.Cost });

                        rigs.Items.Add(new WellDrillData() { Identifier = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstUnRealize.Identifier, Days = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstUnRealize.Days, Cost = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstUnRealize.Cost });

                        rigs.Items.Add(new WellDrillData() { Identifier = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstUnRealize.Identifier, Days = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstUnRealize.Days, Cost = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstUnRealize.Cost });

                        rigwellitems.Add(rigs);
                    }
                    well.WellItems = wellitems;
                    pip.WellItems = pipwellitems;
                    rig.WellItems = rigwellitems;

                    WellPlans.Add(well);
                    PIP.Add(pip);
                    RigPIP.Add(rig);
                    //break;
                }


                return new { Periods = Periods.Distinct().Select(x => x.Date.ToString("dd-MMM-yyyy")).ToList(), WellPlans, PIP, RigPIP };
            });


        }

        public JsonResult GetDataWellPlanPage(string parmResultType, int skip,string EventsWellRig)
        {
            return MvcResultInfo.Execute(() =>
            {
                string PreviousOP = "";
                string NextOP = "";
                string DefaultOP = getBaseOP(out PreviousOP, out NextOP);
                var Division = 1000000.00;
                var parmRolling = 7;
                var Periods = new List<DateTime>();
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
                var parmDate = Tools.ToUTC(DateTime.Now);
                #region query builder
                cfg.DateFilter = parmDate;
                if (parmResultType.ToLower().Contains("days"))
                {
                    cfg.NoOfDays = parmRolling;

                    Periods = cfg.GetlastDates(parmDate, 7, true);

                    //Periods = cfg.GetDailyReserveDate();
                    Periods = Periods.OrderByDescending(x => x.Date).ToList();
                }
                else
                {
                    if (parmResultType.ToLower().Contains("months"))
                    {
                        cfg.NoOfMonth = parmRolling;
                        Periods = cfg.GetlastMonths(parmDate, 7, true);
                        Periods = Periods.OrderByDescending(x => x.Date).ToList();
                    }
                    else if (parmResultType.ToLower().Contains("weeks"))
                    {
                        cfg.NoOfWeek = parmRolling;
                        Periods = cfg.GetlastWeeks(cfg.WeekType.ToString(), parmDate);
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


                var WellPlans = new List<ResultTrxData>();
                var PIP = new List<ResultTrxData>();
                var RigPIP = new List<ResultTrxData>();
                //Well Activities
                var populatedetail = DataHelper.Populate("_WEISCollectionSDL", null, 10, skip * 10).OrderByDescending(x => x.GetString("WellName")).ToList();
                if (EventsWellRig.ToLower().Equals("well"))
                {
                    populatedetail = DataHelper.Populate("_WEISCollectionSDLByWellName", null, 10, skip * 10).OrderByDescending(x => x.GetString("WellName")).ToList();
                }
                else if (EventsWellRig.ToLower().Equals("rig"))
                {
                    populatedetail = DataHelper.Populate("_WEISCollectionSDLByRigName", null, 10, skip * 10).OrderByDescending(x => x.GetString("WellName")).ToList();
                }
                
                var populatesummary = DataHelper.Populate("_WEISCollectionSummarySDL", Query.And(Query.In("Date", new BsonArray(Periods)))).OrderByDescending(x => x.GetString("Date")).ToList();
                foreach (var pr in Periods)
                {

                    var priod = Tools.ToUTC(new DateTime(pr.Year, pr.Month, pr.Day, 0, 0, 0));
                    ResultTrxData well = new ResultTrxData();
                    well.Version = pr.ToString("dd-MMM-yyyy");
                    var items = new List<WellDrillDataForDL>();
                    foreach (var sum in populatesummary)
                    {
                        var sumDate = BsonHelper.GetDateTime(sum, "Date");

                        if (sumDate.Equals(priod))
                        {
                            var sumdeserialize = BsonHelper.Deserialize<SDLSummary>(sum);
                            //Well
                            items.Add(new WellDrillDataForDL() { Identifier = sumdeserialize.WRDetails.WRPlan.Identifier, Days = sumdeserialize.WRDetails.WRPlan.Days, Cost = sumdeserialize.WRDetails.WRPlan.Cost });
                            items.Add(new WellDrillDataForDL() { Identifier = sumdeserialize.WRDetails.WRPrevPlan.Identifier, Days = sumdeserialize.WRDetails.WRPrevPlan.Days, Cost = sumdeserialize.WRDetails.WRPrevPlan.Cost });
                            items.Add(new WellDrillDataForDL() { Identifier = sumdeserialize.WRDetails.WROP.Identifier, Days = sumdeserialize.WRDetails.WROP.Days, Cost = sumdeserialize.WRDetails.WROP.Cost });
                            items.Add(new WellDrillDataForDL() { Identifier = sumdeserialize.WRDetails.WRLE.Identifier, Days = sumdeserialize.WRDetails.WRLE.Days, Cost = sumdeserialize.WRDetails.WRLE.Cost });
                            items.Add(new WellDrillDataForDL() { Identifier = sumdeserialize.WRDetails.WRAFE.Identifier, Days = sumdeserialize.WRDetails.WRAFE.Days, Cost = sumdeserialize.WRDetails.WRAFE.Cost });
                            break;
                        }
                    }
                    well.Items = items;
                    List<WellItems> wellitems = new List<WellItems>();
                    foreach (var dtl in populatedetail)
                    {
                        var detail = BsonHelper.Deserialize<SDLHelper>(dtl);
                        //Well
                        var wells = new WellItems();
                        wells.ActivityType = detail.ActivityType;
                        wells.BaseOP = detail.BaseOP.FirstOrDefault();
                        wells.DateWeekly = detail.LastWeek;
                        wells.IsWeekly = false;
                        wells.RigName = detail.RigName;
                        wells.WellName = detail.WellName;

                        wells.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPlan.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPlan.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPlan.Cost });

                        wells.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPrevPlan.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPrevPlan.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRPrevPlan.Cost });

                        wells.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRLE.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRLE.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRLE.Cost });

                        wells.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WROP.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WROP.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WROP.Cost });

                        wells.Items.Add(new WellDrillData() { Identifier = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRAFE.Identifier, Days = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRAFE.Days, Cost = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).WRAFE.Cost });
                        wellitems.Add(wells);

                    }
                    well.WellItems = wellitems;

                    WellPlans.Add(well);
                }


                return new { Periods = Periods.Distinct().Select(x => x.Date.ToString("dd-MMM-yyyy")).ToList(), WellPlans};
            });


        }

        public JsonResult GetDataRigPipPage(string parmResultType, int skip,string RIGPIP)
        {
            return MvcResultInfo.Execute(() =>
            {
                string PreviousOP = "";
                string NextOP = "";
                string DefaultOP = getBaseOP(out PreviousOP, out NextOP);
                var Division = 1000000.00;
                var parmRolling = 7;
                var Periods = new List<DateTime>();
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
                var parmDate = Tools.ToUTC(DateTime.Now);
                #region query builder
                cfg.DateFilter = parmDate;
                if (parmResultType.ToLower().Contains("days"))
                {
                    cfg.NoOfDays = parmRolling;

                    Periods = cfg.GetlastDates(parmDate, 7, true);

                    //Periods = cfg.GetDailyReserveDate();
                    Periods = Periods.OrderByDescending(x => x.Date).ToList();
                }
                else
                {
                    if (parmResultType.ToLower().Contains("months"))
                    {
                        cfg.NoOfMonth = parmRolling;
                        Periods = cfg.GetlastMonths(parmDate, 7, true);
                        Periods = Periods.OrderByDescending(x => x.Date).ToList();
                    }
                    else if (parmResultType.ToLower().Contains("weeks"))
                    {
                        cfg.NoOfWeek = parmRolling;
                        Periods = cfg.GetlastWeeks(cfg.WeekType.ToString(), parmDate);
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


                var RigPIP = new List<ResultTrxData>();
                //Well Activities
                var populatedetail = DataHelper.Populate("_WEISCollectionSDL", null, 10, skip*10).OrderByDescending(x => x.GetString("WellName")).ToList();
                var populatesummary = DataHelper.Populate("_WEISCollectionSummarySDL", Query.And(Query.In("Date", new BsonArray(Periods)))).OrderByDescending(x => x.GetString("Date")).ToList();
                foreach (var pr in Periods)
                {

                    var priod = Tools.ToUTC(new DateTime(pr.Year, pr.Month, pr.Day, 0, 0, 0));
                    ResultTrxData rig = new ResultTrxData();
                    rig.Version = pr.ToString("dd-MMM-yyyy");

                    var rigpipitems = new List<WellDrillDataForDL>();
                    //ISODate('1900-01-01T00:00:00.000+0000')
                    foreach (var sum in populatesummary)
                    {
                        var sumDate = BsonHelper.GetDateTime(sum, "Date");

                        if (sumDate.Equals(priod))
                        {
                            var sumdeserialize = BsonHelper.Deserialize<SDLSummary>(sum);
                            if (RIGPIP.ToLower().Equals("pip"))
                            {
                                //PIP
                                rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.PIPDetails.OriEst.Identifier, Days = sumdeserialize.PIPDetails.OriEst.Days, Cost = sumdeserialize.PIPDetails.OriEst.Cost });
                                rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.PIPDetails.CurEst.Identifier, Days = sumdeserialize.PIPDetails.CurEst.Days, Cost = sumdeserialize.PIPDetails.CurEst.Cost });
                                rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.PIPDetails.OriEstRealize.Identifier, Days = sumdeserialize.PIPDetails.OriEstRealize.Days, Cost = sumdeserialize.PIPDetails.OriEstRealize.Cost });
                                rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.PIPDetails.CurEstRealize.Identifier, Days = sumdeserialize.PIPDetails.CurEstRealize.Days, Cost = sumdeserialize.PIPDetails.CurEstRealize.Cost });
                                rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.PIPDetails.OriEstUnRealize.Identifier, Days = sumdeserialize.PIPDetails.OriEstUnRealize.Days, Cost = sumdeserialize.PIPDetails.OriEstUnRealize.Cost });
                                rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.PIPDetails.CurEstUnRealize.Identifier, Days = sumdeserialize.PIPDetails.CurEstUnRealize.Days, Cost = sumdeserialize.PIPDetails.CurEstUnRealize.Cost });

                            }
                            else
                            {
                                //Rig PIP
                                rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.RigPIPDetails.OriEst.Identifier, Days = sumdeserialize.RigPIPDetails.OriEst.Days, Cost = sumdeserialize.RigPIPDetails.OriEst.Cost });
                                rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.RigPIPDetails.CurEst.Identifier, Days = sumdeserialize.RigPIPDetails.CurEst.Days, Cost = sumdeserialize.RigPIPDetails.CurEst.Cost });
                                rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.RigPIPDetails.OriEstRealize.Identifier, Days = sumdeserialize.RigPIPDetails.OriEstRealize.Days, Cost = sumdeserialize.RigPIPDetails.OriEstRealize.Cost });
                                rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.RigPIPDetails.CurEstRealize.Identifier, Days = sumdeserialize.RigPIPDetails.CurEstRealize.Days, Cost = sumdeserialize.RigPIPDetails.CurEstRealize.Cost });
                                rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.RigPIPDetails.OriEstUnRealize.Identifier, Days = sumdeserialize.RigPIPDetails.OriEstUnRealize.Days, Cost = sumdeserialize.RigPIPDetails.OriEstUnRealize.Cost });
                                rigpipitems.Add(new WellDrillDataForDL() { Title = sumdeserialize.RigPIPDetails.CurEstUnRealize.Identifier, Days = sumdeserialize.RigPIPDetails.CurEstUnRealize.Days, Cost = sumdeserialize.RigPIPDetails.CurEstUnRealize.Cost });
                            
                            }
                            break;
                        }
                    }
                    rig.Items = rigpipitems;

                    List<WellItems> rigwellitems = new List<WellItems>();
                    foreach (var dtl in populatedetail)
                    {
                        var detail = BsonHelper.Deserialize<SDLHelper>(dtl);
                        //PIP
                        var rigs = new WellItems();
                        rigs.ActivityType = detail.ActivityType;
                        rigs.BaseOP = detail.BaseOP.FirstOrDefault();
                        rigs.DateWeekly = detail.LastWeek;
                        rigs.IsWeekly = false;
                        rigs.RigName = detail.RigName;
                        rigs.WellName = detail.WellName;

                        if (RIGPIP.ToLower().Equals("pip"))
                        {
                            rigs.Items.Add(new WellDrillData() { Identifier = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEst.Identifier, Days = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEst.Days, Cost = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEst.Cost });

                            rigs.Items.Add(new WellDrillData() { Identifier = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEst.Identifier, Days = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEst.Days, Cost = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEst.Cost });

                            rigs.Items.Add(new WellDrillData() { Identifier = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstRealize.Identifier, Days = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstRealize.Days, Cost = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstRealize.Cost });

                            rigs.Items.Add(new WellDrillData() { Identifier = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstRealize.Identifier, Days = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstRealize.Days, Cost = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstRealize.Cost });

                            rigs.Items.Add(new WellDrillData() { Identifier = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstUnRealize.Identifier, Days = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstUnRealize.Days, Cost = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstUnRealize.Cost });

                            rigs.Items.Add(new WellDrillData() { Identifier = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstUnRealize.Identifier, Days = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstUnRealize.Days, Cost = detail.PIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstUnRealize.Cost });

                        }
                        else
                        {

                            rigs.Items.Add(new WellDrillData() { Identifier = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEst.Identifier, Days = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEst.Days, Cost = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEst.Cost });

                            rigs.Items.Add(new WellDrillData() { Identifier = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEst.Identifier, Days = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEst.Days, Cost = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEst.Cost });

                            rigs.Items.Add(new WellDrillData() { Identifier = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstRealize.Identifier, Days = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstRealize.Days, Cost = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstRealize.Cost });

                            rigs.Items.Add(new WellDrillData() { Identifier = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstRealize.Identifier, Days = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstRealize.Days, Cost = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstRealize.Cost });

                            rigs.Items.Add(new WellDrillData() { Identifier = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstUnRealize.Identifier, Days = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstUnRealize.Days, Cost = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).OriEstUnRealize.Cost });

                            rigs.Items.Add(new WellDrillData() { Identifier = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstUnRealize.Identifier, Days = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstUnRealize.Days, Cost = detail.RigPIPDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(priod)).CurEstUnRealize.Cost });

                        }


                        rigwellitems.Add(rigs);
                    }
                    rig.WellItems = rigwellitems;
                    RigPIP.Add(rig);
                }


                return new { Periods = Periods.Distinct().Select(x => x.Date.ToString("dd-MMM-yyyy")).ToList(), RigPIP };
            });


        }

        private static ResultTrxData FilterEvent(ResultTrxData data,string item)
        {
            var result = new ResultTrxData();
            var WellItems = new List<WellItems>();
            foreach (var evnt in data.WellItems.Where(x => x.IsWeekly).OrderByDescending(x => x.DateWeekly.Date).Take(10))
            {
                WellItems wellitem = new WellItems();
                
                wellitem.ActivityType = evnt.ActivityType;
                wellitem.WellName = evnt.WellName;
                wellitem.BaseOP = evnt.BaseOP;
                wellitem.RigName = evnt.RigName;
                wellitem.DateWeekly = evnt.DateWeekly;
                wellitem.IsWeekly = evnt.IsWeekly;

                var aDays = item == "well" ? evnt.Items.FirstOrDefault(x => x.Identifier.Equals("Current OP")).Days : evnt.Items.FirstOrDefault(x => x.Identifier.Equals("Original Estimate")).Days;
                var aCost = item == "well" ? evnt.Items.FirstOrDefault(x => x.Identifier.Equals("Current OP")).Cost : evnt.Items.FirstOrDefault(x => x.Identifier.Equals("Original Estimate")).Cost;

                var bDays = item == "well" ? evnt.Items.FirstOrDefault(x => x.Identifier.Equals("Previous OP")).Days : evnt.Items.FirstOrDefault(x => x.Identifier.Equals("Current Estimate")).Days;
                var bCost = item == "well" ? evnt.Items.FirstOrDefault(x => x.Identifier.Equals("Previous OP")).Cost : evnt.Items.FirstOrDefault(x => x.Identifier.Equals("Current Estimate")).Cost;

                var cDays = item == "well" ? evnt.Items.FirstOrDefault(x => x.Identifier.Equals("LE")).Days : evnt.Items.FirstOrDefault(x => x.Identifier.Equals("Original Estimate Realize")).Days;
                var cCost = item == "well" ? evnt.Items.FirstOrDefault(x => x.Identifier.Equals("LE")).Cost : evnt.Items.FirstOrDefault(x => x.Identifier.Equals("Original Estimate Realize")).Cost;

                var dDays = item == "well" ? evnt.Items.FirstOrDefault(x => x.Identifier.Equals("LS")).Days : evnt.Items.FirstOrDefault(x => x.Identifier.Equals("Current Estimate Realize")).Days;
                var dCost = item == "well" ? evnt.Items.FirstOrDefault(x => x.Identifier.Equals("LS")).Cost : evnt.Items.FirstOrDefault(x => x.Identifier.Equals("Current Estimate Realize")).Cost;

                var eDays = item == "well" ? evnt.Items.FirstOrDefault(x => x.Identifier.Equals("AFE")).Days : evnt.Items.FirstOrDefault(x => x.Identifier.Equals("Original Estimate UnRealize")).Days;
                var eCost = item == "well" ? evnt.Items.FirstOrDefault(x => x.Identifier.Equals("AFE")).Cost : evnt.Items.FirstOrDefault(x => x.Identifier.Equals("Original Estimate UnRealize")).Cost;

                var fDays = item == "well" ? evnt.Items.FirstOrDefault(x => x.Identifier.Equals("MLE")).Days : evnt.Items.FirstOrDefault(x => x.Identifier.Equals("Current Estimate UnRealize")).Days;
                var fCost = item == "well" ? evnt.Items.FirstOrDefault(x => x.Identifier.Equals("MLE")).Cost : evnt.Items.FirstOrDefault(x => x.Identifier.Equals("Current Estimate UnRealize")).Cost;


                wellitem.Items = new List<WellDrillData>();
                if (item == "well")
                {
                    wellitem.Items.Add(new WellDrillData { Identifier = "Current OP", Days = aDays, Cost = aCost });
                    wellitem.Items.Add(new WellDrillData { Identifier = "Previous OP", Days = bDays, Cost = bCost });
                    wellitem.Items.Add(new WellDrillData { Identifier = "LE", Days = cDays, Cost = cCost });
                    wellitem.Items.Add(new WellDrillData { Identifier = "LS", Days = dDays, Cost = dCost });
                    wellitem.Items.Add(new WellDrillData { Identifier = "AFE", Days = eDays, Cost = eCost });
                    wellitem.Items.Add(new WellDrillData { Identifier = "MLE", Days = fDays, Cost = fCost });
                }
                else
                {
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Original Estimate", Days = aDays, Cost = Tools.Div(aCost, 1) });
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Current Estimate", Days = bDays, Cost = Tools.Div(bCost, 1) });
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Original Estimate Realize", Days = cDays, Cost = Tools.Div(cCost, 1) });
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Current Estimate Realize", Days = dDays, Cost = Tools.Div(dCost, 1) });
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Original Estimate UnRealize", Days = eDays, Cost = Tools.Div(eCost, 1) });
                    wellitem.Items.Add(new WellDrillData() { Identifier = "Current Estimate UnRealize", Days = fDays, Cost = Tools.Div(fCost, 1) });
                }


                WellItems.Add(wellitem);

            }
            result.WellItems = WellItems;
            result.Items = data.Items;
            result.Version = data.Version;
            return result;
        }

        private static CustomeVariable SetCustomeValueWeekly(WellActivityUpdate wellactivity,OPHistory History)
        {

            //Well Activities
            var custome = new CustomeVariable();
            custome.OpDays = wellactivity.Plan.Days; custome.OpCost = wellactivity.Plan.Cost;
            custome.LastOpDays = History == null ? 0 : History.Plan.Days; custome.LastOpCost = History == null ? 0 : History.Plan.Cost;
            custome.LeDays = wellactivity.CurrentWeek.Days; custome.LeCost = wellactivity.CurrentWeek.Cost;
            custome.LsDays = wellactivity.OP.Days; custome.LsCost = wellactivity.OP.Cost;
            custome.AfeDays = wellactivity.AFE.Days; custome.AfeCost = wellactivity.AFE.Cost;
            custome.MleDays = wellactivity.Phase.LME.Days; custome.MleCost = wellactivity.Phase.LME.Cost;

            //Pip
            if (wellactivity.Elements.Any())
            {
                custome.PlanPipDays = wellactivity.Elements.Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanPipCost = wellactivity.Elements.Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LePipDays = wellactivity.Elements.Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LePipCost = wellactivity.Elements.Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                custome.PlanPipRealizedDays = wellactivity.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanPipRealizedCost = wellactivity.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LePipRealizedDays
                    = wellactivity.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LePipRealizedCost = wellactivity.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                custome.PlanPipUnRealizedDays = wellactivity.Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanPipUnRealizedCost = wellactivity.Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LePipUnRealizedDays = wellactivity.Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LePipUnRealizedCost = wellactivity.Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);
            }

            //Rig Pip
            if (wellactivity.CRElements.Any())
            {
                custome.PlanRigPipDays = wellactivity.CRElements.Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanRigPipCost = wellactivity.CRElements.Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LeRigPipDays = wellactivity.CRElements.Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LeRigPipCost = wellactivity.CRElements.Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                custome.PlanRigPipRealizedDays = wellactivity.CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanRigPipRealizedCost = wellactivity.CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LeRigPipRealizedDays
                    = wellactivity.CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LeRigPipRealizedCost = wellactivity.CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                custome.PlanRigPipUnRealizedDays = wellactivity.CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanPipUnRealizedCost = wellactivity.CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LePipUnRealizedDays = wellactivity.CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LePipUnRealizedCost = wellactivity.CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);
            }
            return custome;
        }

        private static CustomeVariable SetCustomeValueMonthly(WellActivityUpdateMonthly wellactivity,OPHistory History)
        {

            //Well Activities
            var custome = new CustomeVariable();
            custome.OpDays = wellactivity.Phase.Plan.Days; custome.OpCost = wellactivity.Phase.Plan.Cost;
            custome.LastOpDays = History == null ? 0 : History.Plan.Days; custome.LastOpCost = History == null ? 0 : History.Plan.Cost;
            custome.LeDays = wellactivity.Phase.LE.Days; custome.LeCost = wellactivity.Phase.LE.Cost;
            custome.LsDays = wellactivity.Phase.OP.Days; custome.LsCost = wellactivity.Phase.OP.Cost;
            custome.AfeDays = wellactivity.Phase.AFE.Days; custome.AfeCost = wellactivity.Phase.AFE.Cost;
            custome.MleDays = wellactivity.Phase.LME.Days; custome.MleCost = wellactivity.Phase.LME.Cost;

            //Pip
            if (wellactivity.Elements.Any())
            {
                custome.PlanPipDays = wellactivity.Elements.Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanPipCost = wellactivity.Elements.Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LePipDays = wellactivity.Elements.Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LePipCost = wellactivity.Elements.Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                custome.PlanPipRealizedDays = wellactivity.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanPipRealizedCost = wellactivity.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LePipRealizedDays
                    = wellactivity.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LePipRealizedCost = wellactivity.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                custome.PlanPipUnRealizedDays = wellactivity.Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanPipUnRealizedCost = wellactivity.Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LePipUnRealizedDays = wellactivity.Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LePipUnRealizedCost = wellactivity.Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);
            }

            //Rig Pip
            if (wellactivity.CRElements.Any())
            {
                custome.PlanRigPipDays = wellactivity.CRElements.Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanRigPipCost = wellactivity.CRElements.Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LeRigPipDays = wellactivity.CRElements.Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LeRigPipCost = wellactivity.CRElements.Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                custome.PlanRigPipRealizedDays = wellactivity.CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanRigPipRealizedCost = wellactivity.CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LeRigPipRealizedDays
                    = wellactivity.CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LeRigPipRealizedCost = wellactivity.CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                custome.PlanRigPipUnRealizedDays = wellactivity.CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanPipUnRealizedCost = wellactivity.CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LePipUnRealizedDays = wellactivity.CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LePipUnRealizedCost = wellactivity.CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);
            }
            return custome;
        }

        private static CustomeVariable SetCustomeValuePip(WellPIP wellactivity)
        {
            var custome = new CustomeVariable();
            //Pip
            if (wellactivity.Elements.Any())
            {
                custome.PlanPipDays = wellactivity.Elements.Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanPipCost = wellactivity.Elements.Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LePipDays = wellactivity.Elements.Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LePipCost = wellactivity.Elements.Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                custome.PlanPipRealizedDays = wellactivity.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanPipRealizedCost = wellactivity.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LePipRealizedDays
                    = wellactivity.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LePipRealizedCost = wellactivity.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                custome.PlanPipUnRealizedDays = wellactivity.Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanPipUnRealizedCost = wellactivity.Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LePipUnRealizedDays = wellactivity.Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LePipUnRealizedCost = wellactivity.Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);
            }

            //Rig Pip
            if (wellactivity.CRElements.Any())
            {
                custome.PlanRigPipDays = wellactivity.CRElements.Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanRigPipCost = wellactivity.CRElements.Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LeRigPipDays = wellactivity.CRElements.Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LeRigPipCost = wellactivity.CRElements.Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                custome.PlanRigPipRealizedDays = wellactivity.CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanRigPipRealizedCost = wellactivity.CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LeRigPipRealizedDays
                    = wellactivity.CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LeRigPipRealizedCost = wellactivity.CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                custome.PlanRigPipUnRealizedDays = wellactivity.CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                custome.PlanPipUnRealizedCost = wellactivity.CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                custome.LePipUnRealizedDays = wellactivity.CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                custome.LePipUnRealizedCost = wellactivity.CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);
            }
            return custome;
        }

        private static WellItems GetWellItems(WellActivityPhase phase)
        {
            var sdld = new WellItems();
            sdld.BaseOP = phase.BaseOP.ToString();
            sdld.Items = new List<WellDrillData>();
            sdld.Items.Add(new WellDrillData
            {
                Identifier = "OP",
                Days = phase.Plan.Days,
                Cost = phase.Plan.Cost
            });
            sdld.Items.Add(new WellDrillData
            {
                Identifier = "LE",
                Days = phase.LE.Days,
                Cost = phase.LE.Cost
            });
            sdld.Items.Add(new WellDrillData
            {
                Identifier = "LS",
                Days = phase.OP.Days,
                Cost = phase.OP.Cost
            });
            sdld.Items.Add(new WellDrillData
            {
                Identifier = "AFE",
                Days = phase.AFE.Days,
                Cost = phase.AFE.Cost
            });
            sdld.Items.Add(new WellDrillData
            {
                Identifier = "MLE",
                Days = phase.LME.Days,
                Cost = phase.LME.Cost
            });

            return sdld;
        }

        private static OPHistory getHistoryReport(string well, string seq, string events)
        {
            var t = WellActivity.Get<WellActivity>(Query.And(
                    Query.EQ("WellName", well),
                    Query.EQ("OPHistories.UARigSequenceId", seq),
                    Query.EQ("OPHistories.ActivityType", events)
                ));
            if(t!= null)
            {
                if (t.OPHistories.Any())
                {
                    return t.OPHistories.FirstOrDefault(x => x.UARigSequenceId.Equals(seq) && x.ActivityType.Equals(events));
                }
                else
                    return null;
            }
            else
            {
                return null;

            }
        }

        private static List<WellActivityUpdate> getWeeklyReport(string well, string seq, string events)
        {
            var t = WellActivityUpdate.Populate<WellActivityUpdate>(
                Query.And(
                    Query.EQ("WellName", well),
                    Query.EQ("SequenceId", seq),
                    Query.EQ("Phase.ActivityType", events)
                )
                );
            if (t != null && t.Count() > 0)
                return t.OrderByDescending(x => x.UpdateVersion).ToList();
            return null;
        }

        private static List<WellActivityUpdateMonthly> getMonthlyReport(string well, string seq, string events)
        {
            var t = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(
                Query.And(
                    Query.EQ("WellName", well),
                    Query.EQ("SequenceId", seq),
                    Query.EQ("Phase.ActivityType", events)
                )
                );
            if (t != null && t.Count() > 0)
            {
                t = t.OrderByDescending(x => x.UpdateVersion).ToList();
                return t;
            }
            return null;
        }

        private static WellPIP getPipReport(string well, string rig, int phaseno, string seq, string events)
        {
            var t = WellPIP.Get<WellPIP>(
                Query.And(
                    Query.EQ("WellName", well),
                    Query.EQ("SequenceId", seq),
                    Query.EQ("PhaseNo", phaseno),
                    Query.EQ("ActivityType", events)
                )
                );

            return t;
        }

        public JsonResult ExportSDLToExcell(string parmResultType,string GroupBy)
        {
            string PreviousOP= "";
                string NextOP = "";
                string DefaultOP = getBaseOP(out PreviousOP, out NextOP);
                var Division = 1000000.00;
                var parmRolling = 7;
                var Periods = new List<DateTime>();
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
                var parmDate = Tools.ToUTC(DateTime.Now);
                #region query builder
                cfg.DateFilter = parmDate;
                if (parmResultType.ToLower().Contains("days"))
                {
                    cfg.NoOfDays = parmRolling;

                    Periods = cfg.GetlastDates(parmDate, 7, true);

                    //Periods = cfg.GetDailyReserveDate();
                    Periods = Periods.OrderByDescending(x => x.Date).ToList();
                }
                else
                {
                    if (parmResultType.ToLower().Contains("months"))
                    {
                        cfg.NoOfMonth = parmRolling;
                        Periods = cfg.GetlastMonths(parmDate, 7, true);
                        Periods = Periods.OrderByDescending(x => x.Date).ToList();
                    }
                    else if (parmResultType.ToLower().Contains("weeks"))
                    {
                        cfg.NoOfWeek = parmRolling;
                        Periods = cfg.GetlastWeeks(cfg.WeekType.ToString(), parmDate);
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
            string Template = "";

            int startRow = 3; int startRightColDate = 0; int startLeftColDate = 0;//int startRightColDate = 6;
            List<BsonDocument> populatedetail = new List<BsonDocument>();
            if (GroupBy.ToLower().Equals("event"))
            {
                Template = "SDLEventTemplate.xlsx";
                startRightColDate = 6;
                populatedetail = DataHelper.Populate("_WEISCollectionSDL").OrderByDescending(x => x.GetString("ActivityType")).ToList();
            }
            else if (GroupBy.ToLower().Equals("well"))
            {
                Template = "SDLWellTemplate.xlsx";
                startRightColDate = 3;
                populatedetail = DataHelper.Populate("_WEISCollectionSDLByWellName").OrderByDescending(x => x.GetString("WellName")).ToList();
            }
            else if (GroupBy.ToLower().Equals("rig"))
            {
                Template = "SDLRigTemplate.xlsx";
                startRightColDate = 3;
                populatedetail = DataHelper.Populate("_WEISCollectionSDLByRigName").OrderByDescending(x => x.GetString("RigName")).ToList();
            }

            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), Template);
            Workbook wb = new Workbook(xlst);
            var ws = wb.Worksheets[0];
            
            #region Date Header
            var run = true;
            while (run)
            {
                foreach (var per in Periods)
                {
                    if (startRightColDate == 27)
                    {
                        startLeftColDate++;
                        startRightColDate = 1;
                    }
                    List<int> ColumnArray = new List<int>();
                    ColumnArray.Add(startLeftColDate);
                    ColumnArray.Add(startRightColDate);

                    ws.Cells[GetColumnExcell(ColumnArray) + startRow.ToString()].Value = per.ToString("dd-MMM-yyyy");
                    //Console.WriteLine(GetColumnExcell(ColumnArray));
                    startRightColDate++;
                    if (startLeftColDate == 2 && startRightColDate == 26)
                    {
                        startRightColDate = 6; 
                        startLeftColDate = 0;
                        run = false;
                        startRow++;
                        break;
                    }
                }

            }

            #endregion
            #region Detail
            foreach (var dtl in populatedetail.OrderBy(x => x.GetString("ActivityType")).ToList())
            {
                var detail = BsonHelper.Deserialize<SDLHelper>(dtl);
                //Well
                var wells = new WellItems();
                wells.ActivityType = detail.ActivityType;
                wells.BaseOP = detail.BaseOP.FirstOrDefault();
                wells.DateWeekly = detail.LastWeek;
                wells.IsWeekly = false;
                wells.RigName = detail.RigName;
                wells.WellName = detail.WellName;
                if (detail.WRDetails != null)
                {
                    ws.Cells["A" + startRow.ToString()].Value = detail._id;
                    if (GroupBy.ToLower().Equals("event"))
                    {
                        startRightColDate = 6;
                        ws.Cells["B" + startRow.ToString()].Value = detail.ActivityType;
                        ws.Cells["C" + startRow.ToString()].Value = detail.WellName;
                        ws.Cells["D" + startRow.ToString()].Value = detail.RigName;
                        ws.Cells["E" + startRow.ToString()].Value = ConvertBaseOP(detail.BaseOP);
                    }
                    else if (GroupBy.ToLower().Equals("well"))
                    {
                        startRightColDate = 3;
                        ws.Cells["B" + startRow.ToString()].Value = detail.WellName;
                    }
                    else
                    {
                        startRightColDate = 3;
                        ws.Cells["B" + startRow.ToString()].Value = detail.RigName;
                    }                    
                    startLeftColDate = 0;  run = true;
                    while (run)
                    {
                        foreach (var per in Periods)
                        {
                            
                            List<int> ColumnArray = new List<int>();
                            ColumnArray.Add(startLeftColDate);
                            ColumnArray.Add(startRightColDate);
                            //Previous OP
                            ws.Cells[GetColumnExcell(ColumnArray) + startRow.ToString()].Value = detail.WRDetails.FirstOrDefault(x=>x.SelectDate.Date.Equals(per.Date)).WRPrevPlan.Days;
                            ColumnArray = GetColumnArray(startLeftColDate, (startRightColDate + (1 * 7)));
                            ws.Cells[GetColumnExcell(ColumnArray) + startRow.ToString()].Value = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(per.Date)).WRPrevPlan.Cost;
                            
                            //Current OP                            
                            ColumnArray = GetColumnArray(startLeftColDate, (startRightColDate + (2 * 7)));
                            ws.Cells[GetColumnExcell(ColumnArray) + startRow.ToString()].Value = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(per.Date)).WRPlan.Days;
                            ColumnArray = GetColumnArray(startLeftColDate, (startRightColDate + (3 * 7)));
                            ws.Cells[GetColumnExcell(ColumnArray) + startRow.ToString()].Value = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(per.Date)).WRPlan.Cost;
                            //Latest Sequence
                            ColumnArray = GetColumnArray(startLeftColDate, (startRightColDate + (4 * 7)));
                            ws.Cells[GetColumnExcell(ColumnArray) + startRow.ToString()].Value = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(per.Date)).WROP.Days;
                            ColumnArray = GetColumnArray(startLeftColDate, (startRightColDate + (5 * 7)));
                            ws.Cells[GetColumnExcell(ColumnArray) + startRow.ToString()].Value = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(per.Date)).WROP.Cost;
                            //LE
                            ColumnArray = GetColumnArray(startLeftColDate, (startRightColDate + (6 * 7)));
                            ws.Cells[GetColumnExcell(ColumnArray) + startRow.ToString()].Value = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(per.Date)).WRLE.Days;
                            ColumnArray = GetColumnArray(startLeftColDate, (startRightColDate + (7 * 7)));
                            ws.Cells[GetColumnExcell(ColumnArray) + startRow.ToString()].Value = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(per.Date)).WRLE.Cost;
                            //AFE
                            ColumnArray = GetColumnArray(startLeftColDate, (startRightColDate + (8 * 7)));
                            ws.Cells[GetColumnExcell(ColumnArray) + startRow.ToString()].Value = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(per.Date)).WRAFE.Days;
                            ColumnArray = GetColumnArray(startLeftColDate, (startRightColDate + (9 * 7)));
                            ws.Cells[GetColumnExcell(ColumnArray) + startRow.ToString()].Value = detail.WRDetails.FirstOrDefault(x => x.SelectDate.Date.Equals(per.Date)).WRAFE.Cost;

                            startRightColDate++;
                            //if (startRightColDate == 13)
                            //{
                            //    run = false;
                            //    startRightColDate = 6;
                            //    startLeftColDate = 0;
                            //    break;
                            //}
                        }
                        run = false;
                    }                    
                }
                startRow++;
            }
            #endregion
            var filename = "";
            if (GroupBy.ToLower().ToLower().Equals("event"))
                filename = "EventDataLayer";
            else if (GroupBy.ToLower().ToLower().Equals("well"))
                filename = "WellDataLayer";
            else
                filename = "RigDataLayer";
            var newFileNameSingle = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                     String.Format(filename+"-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy")));

            string returnName = String.Format(filename + "-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy"));

            wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            
            return Json(new { Success = true, Path = returnName }, JsonRequestBehavior.AllowGet);

           // return File(res, Tools.GetContentType(".xlsx"), retstringName);
        }

        public FileResult DownLoadFileSDL(string FileName)
        {
            var res = Path.Combine(Server.MapPath("~/App_Data/Temp"), FileName);

            //var retstringName = "DataLayerByEvent-" + DateTime.UtcNow.ToString("MMM-yyyy") + ".xlsx";
            return File(res, Tools.GetContentType(".xlsx"), FileName);
        }

        private static string GetColumnExcell(List<int> column)
        {
            var result = "";
            foreach (var i in column)
            {
                if(i != 0)
                    result += ConvertToString(i);
            }
            return result.Trim();
        }

        private static List<int> GetColumnArray(int startLeftColDate,int startRightColDate)
        {
            if (startRightColDate >= 27 && startRightColDate <= 53)
            {
                startLeftColDate =1;
                startRightColDate -= 26 ;
            }
            else if (startRightColDate >= 53)
            {
                startLeftColDate = 2;
                startRightColDate -= 52;
            }
            return new List<int>() { startLeftColDate, startRightColDate };
        }

        public static string ConvertToString(int angka)
        {
            //72
            if (angka == 1) return "A";
            else if (angka == 2) return "B";
            else if (angka == 3) return "C";
            else if (angka == 4) return "D";
            else if (angka == 5) return "E";
            else if (angka == 6) return "F";
            else if (angka == 7) return "G";
            else if (angka == 8) return "H";
            else if (angka == 9) return "I";
            else if (angka == 10) return "J";
            else if (angka == 11) return "K";
            else if (angka == 12) return "L";
            else if (angka == 13) return "M";
            else if (angka == 14) return "N";
            else if (angka == 15) return "O";
            else if (angka == 16) return "P";
            else if (angka == 17) return "Q";
            else if (angka == 18) return "R";
            else if (angka == 19) return "S";
            else if (angka == 20) return "T";
            else if (angka == 21) return "U";
            else if (angka == 22) return "V";
            else if (angka == 23) return "W";
            else if (angka == 24) return "X";
            else if (angka == 25) return "Y";
            else return "Z";
        }

        private static string ConvertBaseOP(List<string> BaseOP)
        {
            string result = "";
            if (BaseOP != null || BaseOP.Any())
            {
                foreach (var i in BaseOP.OrderBy(x=>x).ToList())
                {
                    if (BaseOP.IndexOf(i) == BaseOP.Count - 1)
                    {
                        result += i;
                    }
                    else
                    {
                        result += i + ", ";
                    }
                    
                }
            }
            return result;
        }

        public class SDLPIPDetails
        {
            public DateTime SelectDate { get; set; }
            public DateTime WRNearSelectedDate { get; set; }

            public WellDrillData OriEst { get; set; }
            public WellDrillData CurEst { get; set; }
            public WellDrillData OriEstRealize { get; set; }
            public WellDrillData CurEstRealize { get; set; }
            public WellDrillData OriEstUnRealize { get; set; }
            public WellDrillData CurEstUnRealize { get; set; }


            public SDLPIPDetails()
            {
                OriEst = new WellDrillData();
                CurEst = new WellDrillData();
                OriEstRealize = new WellDrillData();
                CurEstRealize = new WellDrillData();
                OriEstUnRealize = new WellDrillData();
                CurEstUnRealize = new WellDrillData();

            }
        }
        public class SDLHelper
        {
            public int _id { get; set; }
            public int PhaseNo { get; set; }
            public string RigName { get; set; }
            public string SequenceId { get; set; }
            public List<OPHistory> OPHistories { get; set; }
            public string Region { get; set; }
            public string RigType { get; set; }
            public string WellName { get; set; }
            public string ActivityType { get; set; }

            public List<string> BaseOP { get; set; }
            public WellDrillData Plan { get; set; }
            public WellDrillData OP { get; set; }
            public WellDrillData LE { get; set; }
            public WellDrillData AFE { get; set; }
            public DateTime LastWeek { get; set; }
            public DateTime LastMonth { get; set; }

            public DateRange PlanSchedule { get; set; }
            public DateRange AFESchedule { get; set; }
            public DateRange LESchedule { get; set; }
            public DateRange OPSchedule { get; set; }
            public DateRange PHSchedule { get; set; }

            public List<SDLDetails> WRDetails { get; set; }
            public List<SDLPIPDetails> PIPDetails { get; set; }
            public List<SDLPIPDetails> RigPIPDetails { get; set; }

            public SDLHelper()
            {
                OPHistories = new List<OPHistory>();
                LESchedule = new DateRange();
                BaseOP = new List<string>();
                Plan = new WellDrillData();
                OP = new WellDrillData();
                LE = new WellDrillData();
                AFE = new WellDrillData();
                LastWeek = new DateTime();
                LastMonth = new DateTime();
                PlanSchedule = new DateRange();
                AFESchedule = new DateRange();
                OPSchedule = new DateRange();
                PHSchedule = new DateRange();

                WRDetails = new List<SDLDetails>();
                PIPDetails = new List<SDLPIPDetails>();
                RigPIPDetails = new List<SDLPIPDetails>();
            }
        }
        public class SDLDetails
        {
            public DateTime SelectDate { get; set; }
            public DateTime WRNearSelectedDate { get; set; }

            public WellDrillData WRPlan { get; set; }
            public WellDrillData WRPrevPlan { get; set; }
            public WellDrillData WROP { get; set; }
            public WellDrillData WRLE { get; set; }
            public WellDrillData WRAFE { get; set; }


            public SDLDetails()
            {
                WRPlan = new WellDrillData();
                WRPrevPlan = new WellDrillData();
                WROP = new WellDrillData();
                WRLE = new WellDrillData();
                WRAFE = new WellDrillData();

            }
        }
        public class SDLSummary
        {
            public Object _id { get; set; }
            public DateTime Date { get; set; }
            public SDLDetails WRDetails { get; set; }
            public SDLPIPDetails PIPDetails { get; set; }
            public SDLPIPDetails RigPIPDetails { get; set; }
            public SDLSummary()
            {
                WRDetails = new SDLDetails();
                PIPDetails = new SDLPIPDetails();
                RigPIPDetails = new SDLPIPDetails();
            }
        }

        private class CustomeVariable
        {
            //Well Activities
            public double OpDays { get; set; }
            public double OpCost { get; set; }
            public double LastOpDays { get; set; }
            public double LastOpCost { get; set; }
            public double LeDays { get; set; }
            public double LeCost { get; set; }
            public double LsDays { get; set; }
            public double LsCost { get; set; }
            public double AfeDays { get; set; }
            public double AfeCost { get; set; }
            public double MleDays { get; set; }
            public double MleCost { get; set; }

            //PIP

            public double PlanPipDays { get; set; }
            public double PlanPipCost { get; set; }
            public double LePipDays { get; set; }
            public double LePipCost { get; set; }
            public double PlanPipRealizedDays { get; set; }
            public double PlanPipRealizedCost { get; set; }
            public double LePipRealizedDays { get; set; }
            public double LePipRealizedCost { get; set; }
            public double PlanPipUnRealizedDays { get; set; }
            public double PlanPipUnRealizedCost { get; set; }
            public double LePipUnRealizedDays { get; set; }
            public double LePipUnRealizedCost { get; set; }

            //Rig PIP

            public double PlanRigPipDays { get; set; }
            public double PlanRigPipCost { get; set; }
            public double LeRigPipDays { get; set; }
            public double LeRigPipCost { get; set; }
            public double PlanRigPipRealizedDays { get; set; }
            public double PlanRigPipRealizedCost { get; set; }
            public double LeRigPipRealizedDays { get; set; }
            public double LeRigPipRealizedCost { get; set; }
            public double PlanRigPipUnRealizedDays { get; set; }
            public double PlanRigPipUnRealizedCost { get; set; }
            public double LeRigPipUnRealizedDays { get; set; }
            public double LeRigPipUnRealizedCost { get; set; }
        }

        public static void ConsoleWrite(string title)
        {
            var qs = String.Format("{0} {1}", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), title);
            Console.WriteLine(qs);
        }

        public static DateTime GetNearDate(DateTime date, List<DateTime> listdate)
        {
            var result = new DateTime(1900, 1, 1);
            foreach (var dt in listdate.OrderByDescending(o => o).ToList())
            {
                if (date >= dt)
                {
                    return dt;
                }
            }
            return result;
        }

        internal class ResultPhase
        {
            public string WellName { get; set; }
            public string ActivityType { get; set; }
            public string SequenceId { get; set; }
            public List<ResultTrxData> Phase { get; set; }
        }


    }

}