using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECIS.Client.WEIS;
using ECIS.Core;
using System.Reflection;

using MongoDB.Bson;
using System.IO;
using MongoDB.Driver.Builders;
using MongoDB.Driver;


using MongoDB;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
using System.Configuration;
namespace WEISDataTrail
{
    class Program
    {
        //private static string assmbly = "ECIS.Client.WEIS";
        static void Main(string[] args)
        {

            //ECIS.Core.License.ConfigFolder = System.IO.Directory.GetCurrentDirectory() + @"\License";
            ECIS.Core.License.ConfigFolder = System.Configuration.ConfigurationManager.AppSettings["License"];
            ECIS.Core.License.LoadLicense();
            ////var exTime = System.Configuration.ConfigurationManager.AppSettings["ExecuteTime"];
            //var isUseExecuteTime = System.Configuration.ConfigurationManager.AppSettings["isUseExecuteTime"];

            //DateTime prevDateExec = new DateTime();
            //WriteScreen("WEIS Data Trail Running...", false);
            //WriteScreen("in order to capture weis data, please doesn't close this application", false);
            //DateTime lastCapture = new DateTime();
            //if (isUseExecuteTime.Equals("1"))
            //{
            //    while (true)
            //    {
            //        // get DB Config
            //        var t = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "WEISDataReserveConfig"));

            //        DataReserveConfig cfg = BsonHelper.Deserialize<DataReserveConfig>(t.FirstOrDefault().ToBsonDocument().Get("ConfigValue").ToBsonDocument());
            //        cfg.GetListReservedDate(cfg.WeekType);
            //        if (DateTime.Now.ToString("HH:mm").Equals(cfg.DailyRunningTime.Trim()) && prevDateExec.Date != DateTime.Now.Date)
            //        {
            //            Start();
            //            prevDateExec = DateTime.Now.Date;
            //            System.Threading.Thread.Sleep(100);


            //            List<DateTime> keepdDataDates = cfg.AllReservedDate;
            //            keepdDataDates.Add(DateTime.Now.Date);
            //            string[] collections = System.Configuration.ConfigurationManager.AppSettings["Collections"].ToString().Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "").ToString().Split(',');
            //            ReserveAndClean(keepdDataDates, collections.ToList());
            //            lastCapture = DateTime.Now; 
            //        }
            //        Console.Clear();
            //        WriteScreen("WEIS Data Trail Running...", false);
            //        WriteScreen("in order to capture weis data, please doesn't close this application", false);
            //        WriteScreen("Last data capture success at : " + lastCapture.ToString("yyyy-MM-dd hh:mm:ss") , false);

            //        System.Threading.Thread.Sleep(30000);
            //    }
            //}
            //else
            //{
            //    Start();
            //}

            // Structure Data Layer
            //ECIS.Core.License.ConfigFolder = ConfigurationManager.AppSettings["License"];// System.IO.Directory.GetCurrentDirectory() + @"\License";
            //ECIS.Core.License.LoadLicense();

            //WriteScreen("SDL  Running...", false);

            //var parmDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
            ////var parmDate = Tools.ToUTC(DateTime.Now);
            //var parmResultType = "last 7 days";
            //var parmType = "daily";
            //var parmRolling = 1;
            //var parmOP = "OP15";
            //var wellNames = new List<string>();
            //string result = GetDataWellPlanAndPip(parmDate, parmType, parmRolling, parmOP, parmResultType, wellNames);
            Snapshot();
            DataLayer();
            WriteScreen("SDL  Stop...", false);


        }

        public static void Snapshot()
        {
            string title = "=========== ECIS.DataTrail Start : " + DateTime.Now.ToString("dd MMM yyyy") + " =============";
            string processStart = "Process start at : " + DateTime.Now.ToString("yyyy MMM dd hh:mm:ss");

            WriteScreen(title);
            WriteScreen(processStart);


            var hostTarget = System.Configuration.ConfigurationManager.AppSettings["ServerHost2"];
            var dbTarget = System.Configuration.ConfigurationManager.AppSettings["ServerDb2"];

            var host = System.Configuration.ConfigurationManager.AppSettings["ServerHost"];
            var db = System.Configuration.ConfigurationManager.AppSettings["ServerDb"];


            WriteScreen("Audit Trail Origin, Host : " + host + " , DB Name : " + db);
            WriteScreen("Audit Trail Target, Host : " + hostTarget + " , DB Name : " + dbTarget);

            string LogFile = System.Configuration.ConfigurationManager.AppSettings["LogFile"];
            LogFile = LogFile.Trim();
            if (LogFile.Equals(""))
            {
                WriteScreen("LogFile is not defined");
                WaitAndClose();
            }
            else
            {
                if (!File.Exists(@LogFile))
                {
                    var t = File.Create(@LogFile);
                    t.Close();
                }
            }


            string[] collections = System.Configuration.ConfigurationManager.AppSettings["Collections"].ToString().Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "").ToString().Split(',');
            DateTime dt = Tools.ToUTC(DateTime.Now);
            WriteScreen("Write Audit Trail for Date : " + dt.ToString("yyyy-MMM-dd hh:mm:ss"));

            string[] masters = System.Configuration.ConfigurationManager.AppSettings["Masters"].ToString().Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "").ToString().Split(',');

            // summary 
            CollectionSummary smr = new CollectionSummary();
            smr.TrailDate = dt;
            foreach (var xo in collections.Where(x => !x.Trim().Equals("")))
            {
                WriteScreen("Write Audit Trail for Collection : " + xo);

                var datas = DataHelper.Populate(xo);
                foreach (var y in datas)
                {
                    GenericModel<BsonDocument> dync = new GenericModel<BsonDocument>(y, xo, dt);
                }


                if (masters.Contains(xo))
                {
                    // masters
                    MasterSummary mst = new MasterSummary();
                    mst.Count = datas.Count();
                    mst.TableName = xo;
                    mst.Datas = datas.Select(x => BsonHelper.GetString(x, "_id")).ToList();
                    smr.Masters.Add(mst);

                }
                else
                {
                    // transact
                    switch (xo)
                    {
                        case "WEISWellPIPs":
                            {
                                #region PIP

                                #region WellPIP
                                TranscSummary pip = new TranscSummary("WEISWellPIPs");

                                List<SummaryDetailPIP> Details = new List<SummaryDetailPIP>();

                                var uwindplanData = BsonHelper.Unwind(datas.ToList(), "Elements");
                                pip.Count = uwindplanData.Count();

                                var grp = uwindplanData.GroupBy(x => BsonHelper.Get(x, "AssignTOOps"));

                                foreach (var y in grp)
                                {
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


                                    var type = y.Key.AsBsonArray.ToString().Replace("[", "").Replace("]", "");

                                    WellDrillData plan = new WellDrillData();
                                    plan.Days = planDays;
                                    plan.Cost = planCost;
                                    WellDrillData le = new WellDrillData();
                                    le.Days = leDays;
                                    le.Cost = leCost;

                                    WellDrillData planRealize = new WellDrillData();
                                    planRealize.Days = planDaysRealize;
                                    planRealize.Cost = planCostRealize;
                                    WellDrillData planUnRealize = new WellDrillData();
                                    planUnRealize.Days = planDaysUnRealize;
                                    planUnRealize.Cost = planCostUnRealize;

                                    WellDrillData leRealize = new WellDrillData();
                                    leRealize.Days = leDaysRealize;
                                    leRealize.Cost = leCostRealize;
                                    WellDrillData leUnRealize = new WellDrillData();
                                    leUnRealize.Days = leDaysUnRealize;
                                    leUnRealize.Cost = leCostUnRealize;


                                    Details.Add(new SummaryDetailPIP()
                                    {
                                        LE = le,
                                        OP = plan,
                                        OPType = type,
                                        LEUnRealized = leUnRealize,
                                        LERealized = leRealize,
                                        OPRealized = planRealize,
                                        OPUnRealized = planUnRealize,
                                        ReliazedPIP = rl.Count(),
                                        UnReliazedPIP = unRl.Count()
                                    });
                                }
                                pip.Details = Details;
                                smr.PIP = pip;

                                #endregion

                                #region CR Elements
                                var pipCR = new TranscSummary("WEISWellPIPs");
                                var DetailsCR = new List<SummaryDetailPIP>();

                                var uwindplanDataCR = BsonHelper.Unwind(datas.ToList(), "CRElements");
                                pipCR.Count = uwindplanDataCR.Count();
                                var grpCR = uwindplanDataCR.GroupBy(x => BsonHelper.Get(x, "AssignTOOps"));

                                foreach (var y in grpCR)
                                {
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


                                    var type = y.Key.AsBsonArray.ToString().Replace("[", "").Replace("]", "");

                                    WellDrillData plan = new WellDrillData();
                                    plan.Days = planDays;
                                    plan.Cost = planCost;
                                    WellDrillData le = new WellDrillData();
                                    le.Days = leDays;
                                    le.Cost = leCost;

                                    WellDrillData planRealize = new WellDrillData();
                                    planRealize.Days = planDaysRealize;
                                    planRealize.Cost = planCostRealize;
                                    WellDrillData planUnRealize = new WellDrillData();
                                    planUnRealize.Days = planDaysUnRealize;
                                    planUnRealize.Cost = planCostUnRealize;

                                    WellDrillData leRealize = new WellDrillData();
                                    leRealize.Days = leDaysRealize;
                                    leRealize.Cost = leCostRealize;
                                    WellDrillData leUnRealize = new WellDrillData();
                                    leUnRealize.Days = leDaysUnRealize;
                                    leUnRealize.Cost = leCostUnRealize;

                                    DetailsCR.Add(new SummaryDetailPIP()
                                    {
                                        LE = le,
                                        OP = plan,
                                        OPType = type,
                                        LEUnRealized = leUnRealize,
                                        LERealized = leRealize,
                                        OPRealized = planRealize,
                                        OPUnRealized = planUnRealize,
                                        ReliazedPIP = rl.Count(),
                                        UnReliazedPIP = unRl.Count()
                                    });
                                }
                                pipCR.Details = DetailsCR;
                                smr.RigPIP = pipCR;

                                #endregion

                                break;
                                #endregion
                            }
                        case "WEISWellActivityUpdatesMonthly":
                            {
                                #region MLE
                                TranscSummary wp = new TranscSummary("WEISWellActivityUpdatesMonthly");
                                wp.Count = datas.Count();
                                List<SummaryDetailPIP> Details = new List<SummaryDetailPIP>();
                                var grp = datas.GroupBy(x => BsonHelper.Get(x, "Phase.BaseOP"));

                                foreach (var y in grp)
                                {
                                    var planDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "Plan.Days"));
                                    var planCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "Plan.Cost"));
                                    var leDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "LE.Days"));
                                    var leCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "LE.Cost"));
                                    var lsDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "OP.Days"));
                                    var lsCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "OP.Cost"));

                                    var afeDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "AFE.Days"));
                                    var afeCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "AFE.Cost"));
                                    var mleDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "MLE.Days"));
                                    var mleCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "MLE.Cost"));

                                    var type = y.Key.AsBsonArray.ToString().Replace("[", "").Replace("]", "");

                                    WellDrillData plan = new WellDrillData();
                                    plan.Days = planDays;
                                    plan.Cost = planCost;
                                    WellDrillData le = new WellDrillData();
                                    le.Days = leDays;
                                    le.Cost = leCost;
                                    WellDrillData ls = new WellDrillData();
                                    ls.Days = lsDays;
                                    ls.Cost = lsCost;
                                    WellDrillData mle = new WellDrillData();
                                    mle.Days = mleDays;
                                    mle.Cost = mleCost;
                                    WellDrillData afe = new WellDrillData();
                                    afe.Days = afeDays;
                                    afe.Cost = afeCost;

                                    Details.Add(new SummaryDetailPIP
                                    {
                                        LE = le,
                                        OP = plan,
                                        LS = ls,
                                        MLE = mle,
                                        AFE = afe,
                                        OPType = type
                                    });
                                }
                                wp.Details = Details;
                                smr.MonthlyReport = wp;

                                break;
                                #endregion
                            }
                        case "WEISWellActivityUpdates":
                            {
                                #region Weekly Report
                                TranscSummary wp = new TranscSummary("WEISWellActivityUpdates");
                                wp.Count = datas.Count();

                                List<SummaryDetailPIP> Details = new List<SummaryDetailPIP>();
                                var grp = datas.GroupBy(x => BsonHelper.Get(x, "Phase.BaseOP"));

                                foreach (var y in grp)
                                {
                                    var planDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "Plan.Days"));
                                    var planCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "Plan.Cost"));
                                    var leDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "LE.Days"));
                                    var leCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "LE.Cost"));
                                    var lsDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "OP.Days"));
                                    var lsCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "OP.Cost"));

                                    var type = y.Key.AsBsonArray.ToString().Replace("[", "").Replace("]", "");

                                    WellDrillData plan = new WellDrillData();
                                    plan.Days = planDays;
                                    plan.Cost = planCost;
                                    WellDrillData le = new WellDrillData();
                                    le.Days = leDays;
                                    le.Cost = leCost;
                                    WellDrillData ls = new WellDrillData();
                                    ls.Days = lsDays;
                                    ls.Cost = lsCost;
                                    Details.Add(new SummaryDetailPIP
                                    {
                                        LE = le,
                                        OP = plan,
                                        LS = ls,
                                        OPType = type
                                    });
                                }
                                wp.Details = Details;
                                smr.WeeklyReport = wp;
                                break;
                                #endregion
                            }
                        case "WEISBizPlanActivities":
                            {
                                #region Biz Plan
                                TranscSummary bp = new TranscSummary("WEISBizPlanActivities");
                                bp.Count = datas.Count();

                                List<SummaryDetailPIP> Details = new List<SummaryDetailPIP>();

                                var planData = datas.Select(x => BsonHelper.Get(x, "Phases")).ToList();
                                var uwindplanData = BsonHelper.Unwind(datas.ToList(), "Phases");

                                var grp = uwindplanData.GroupBy(x => BsonHelper.Get(x, "BaseOP"));

                                foreach (var y in grp)
                                {
                                    var planDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "Plan.Days"));
                                    var planCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "Plan.Cost"));
                                    var leDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "LE.Days"));
                                    var leCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "LE.Cost"));
                                    var lsDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "OP.Days"));
                                    var lsCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "OP.Cost"));

                                    var type = y.Key.AsBsonArray.ToString().Replace("[", "").Replace("]", "");

                                    WellDrillData plan = new WellDrillData();
                                    plan.Days = planDays;
                                    plan.Cost = planCost;
                                    WellDrillData le = new WellDrillData();
                                    le.Days = leDays;
                                    le.Cost = leCost;
                                    WellDrillData ls = new WellDrillData();
                                    ls.Days = lsDays;
                                    ls.Cost = lsCost;
                                    Details.Add(new SummaryDetailPIP
                                    {
                                        LE = le,
                                        OP = plan,
                                        LS = ls,
                                        OPType = type
                                    });
                                }
                                bp.Details = Details;
                                smr.BizPlan = bp;
                                break;
                                #endregion
                            }
                        case "WEISWellActivities":
                            {
                                #region Well Plan

                                smr.WellPlan = WellPlanSumaryCalc(datas);

                                break;
                                #endregion
                            }
                    }
                }

            }
            WriteScreen("Save Summary to : WEISCollectionSummary_tr");
            smr.SaveSummary();

            WriteScreen("Process Finish at : " + DateTime.Now.ToString("yyyy MMM dd hh:mm:ss"));
            WriteScreen("==================================================================");
            #region Class Alias
            //string[] temps = System.Configuration.ConfigurationManager.AppSettings["Classes"].ToString().Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "").ToString().Split(',');
            //var activities = DataHelper.Populate<MasterActivity>("WEISActivities");
            //List<GenericModel<MasterActivity>> result = new List<GenericModel<MasterActivity>>();
            //foreach (var t in activities)
            //{
            //    GenericModel<MasterActivity> dync = new GenericModel<MasterActivity>(t);
            //    dync.SaveTrail();
            //}

            //var handle = Activator.CreateInstance(assmbly, xo);
            //object obj = handle.Unwrap();

            //PropertyInfo pTable = obj.GetType().GetProperty("TableName");
            //string TableName = pTable.GetValue(obj, null) == null ? "" : pTable.GetValue(obj, null).ToString();

            //var datas = DataHelper.Populate(TableName);


            //foreach (var y in datas)
            //{
            //    GenericModel<object> dync = new GenericModel<object>(y, TableName, obj);
            //}
            #endregion
        }

        public static TranscSummary WellPlanSumaryCalc(List<BsonDocument> datas)
        {
            TranscSummary wp = new TranscSummary("WEISWellActivities");
            wp.Count = datas.Count();
            List<SummaryDetailPIP> Details = new List<SummaryDetailPIP>();
            var uwindplanData = BsonHelper.Unwind(datas.ToList(), "Phases", elementsToCopy: new string[] { "OPHistories" });
            var grp = uwindplanData.GroupBy(x => BsonHelper.Get(x, "BaseOP"));
            foreach (var y in grp)
            {

                if (y.Key.ToString().Split(',').Count() > 1)
                {
                    var ops = y.Key.ToString().Split(',').Select(x => x.Trim().Replace("[","").Replace("]",""));
                    string DefaultOP = "OP15";
                    if (Config.GetConfigValue("BaseOPConfig") != null)
                        DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();

                    string PrevOP = "OP" + (Convert.ToInt32(DefaultOP.Replace("OP", "")) - 1).ToString();
                    //string NextOP = "OP" + (Convert.ToInt32(DefaultOP.Replace("OP", "")) + 1).ToString();
                    SummaryDetailPIP opdet = new SummaryDetailPIP();

                    foreach(var bop in ops)
                    {
                        if(!bop.Equals(DefaultOP))
                        {
                            // prev, take in history
                            var histor = y.ToList().SelectMany(x => BsonHelper.Get(x, "OPHistories").AsBsonArray).ToList().Where(x=> BsonHelper.GetString(x.ToBsonDocument(),"Type").Equals(bop)).Select(x=>x.ToBsonDocument());
                            opdet.OPType = bop;
                            opdet.OP.Days = histor.ToList().Sum(x => BsonHelper.GetDouble(x, "Plan.Days"));
                            opdet.OP.Cost = histor.ToList().Sum(x => BsonHelper.GetDouble(x, "Plan.Cost"));

                            opdet.LS.Days = histor.ToList().Sum(x => BsonHelper.GetDouble(x, "OP.Days"));
                            opdet.LS.Cost = histor.ToList().Sum(x => BsonHelper.GetDouble(x, "OP.Cost"));

                            opdet.LE.Days = histor.ToList().Sum(x => BsonHelper.GetDouble(x, "LE.Days"));
                            opdet.LE.Cost = histor.ToList().Sum(x => BsonHelper.GetDouble(x, "LE.Cost"));

                            opdet.MLE.Days = histor.ToList().Sum(x => BsonHelper.GetDouble(x, "MLE.Days"));
                            opdet.MLE.Cost = histor.ToList().Sum(x => BsonHelper.GetDouble(x, "MLE.Cost"));

                            opdet.AFE.Days = histor.ToList().Sum(x => BsonHelper.GetDouble(x, "AFE.Days"));
                            opdet.AFE.Cost = histor.ToList().Sum(x => BsonHelper.GetDouble(x, "AFE.Cost"));
                        }
                    }

                    // header (current)
                    var planDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "Plan.Days"));
                    var planCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "Plan.Cost"));
                    var leDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "LE.Days"));
                    var leCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "LE.Cost"));
                    var lsDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "OP.Days"));
                    var lsCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "OP.Cost"));

                    var afeDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "AFE.Days"));
                    var afeCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "AFE.Cost"));
                    var mleDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "MLE.Days"));
                    var mleCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "MLE.Cost"));

                    var type = y.Key.AsBsonArray.ToString().Replace("[", "").Replace("]", "");

                    WellDrillData plan = new WellDrillData();
                    plan.Days = planDays;
                    plan.Cost = planCost;
                    WellDrillData le = new WellDrillData();
                    le.Days = leDays;
                    le.Cost = leCost;
                    WellDrillData ls = new WellDrillData();
                    ls.Days = lsDays;
                    ls.Cost = lsCost;
                    WellDrillData mle = new WellDrillData();
                    mle.Days = mleDays;
                    mle.Cost = mleCost;
                    WellDrillData afe = new WellDrillData();
                    afe.Days = afeDays;
                    afe.Cost = afeCost;

                    Details.Add(new SummaryDetailPIP
                    {
                        LE = le,
                        OP = plan,
                        LS = ls,
                        MLE = mle,
                        AFE = afe,
                        OPType = type,
                        PrevOP = opdet
                    });
                }
                else
                {
                    // header (current)
                    var planDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "Plan.Days"));
                    var planCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "Plan.Cost"));
                    var leDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "LE.Days"));
                    var leCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "LE.Cost"));
                    var lsDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "OP.Days"));
                    var lsCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "OP.Cost"));
                    var afeDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "AFE.Days"));
                    var afeCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "AFE.Cost"));
                    var mleDays = y.ToList().Sum(x => BsonHelper.GetDouble(x, "MLE.Days"));
                    var mleCost = y.ToList().Sum(x => BsonHelper.GetDouble(x, "MLE.Cost"));

                    var type = y.Key.AsBsonArray.ToString().Replace("[", "").Replace("]", "");

                    WellDrillData plan = new WellDrillData();
                    plan.Days = planDays;
                    plan.Cost = planCost;
                    WellDrillData le = new WellDrillData();
                    le.Days = leDays;
                    le.Cost = leCost;
                    WellDrillData ls = new WellDrillData();
                    ls.Days = lsDays;
                    ls.Cost = lsCost;
                    WellDrillData mle = new WellDrillData();
                    mle.Days = mleDays;
                    mle.Cost = mleCost;
                    WellDrillData afe = new WellDrillData();
                    afe.Days = afeDays;
                    afe.Cost = afeCost;

                    Details.Add(new SummaryDetailPIP
                    {
                        LE = le,
                        OP = plan,
                        LS = ls,
                        MLE = mle,
                        AFE = afe,
                        OPType = type,
                        PrevOP = new SummaryDetailPIP()
                    });
                }


            }
            wp.Details = Details;


            return wp;
        }

        public static void ReserveAndClean(List<DateTime> reservedDate, List<string> collection)
        {
            string title = "=========== Clear and Reserve data Trail Start : " + DateTime.Now.ToString("dd MMM yyyy") + " =============";
            string processStart = "Process start at : " + DateTime.Now.ToString("yyyy MMM dd hh:mm:ss");

            WriteScreen("Collections to Clean up : " + string.Join(" | ", collection));
            WriteScreen("Data Reserved by Date : " + string.Join(" | ", reservedDate.Select(x => x.Date.ToString("yyyy-MM-dd")).ToList()));

            foreach (var t in collection)
            {
                WriteScreen("Cleaning : " + t);
                IMongoQuery q = null;
                q = Query.Not(Query.In("DateId", new BsonArray(reservedDate.Select(x => x.Date.ToString("yyyyMMdd")))));
                ExtendedMDB.Delete(t + "_tr", q);
            }

            WriteScreen("Process Clear and Reserve data Trail Finish at : " + DateTime.Now.ToString("yyyy MMM dd hh:mm:ss"));
            WriteScreen("==================================================================");
        }

        public static void WriteScreen(string message, bool isWriteLog = true)
        {
            if(isWriteLog)
                WriteLog(new string[] { message });
            string msg = string.Format("{0} {1}", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"), message);
            Console.WriteLine(msg);
        }

        public static void WaitAndClose()
        {
            Console.WriteLine("Press any key to close..");
            Console.ReadLine();
            Environment.Exit(0);
        }

        public static void WriteLog(string[] message, string filePath = "")
        {
            if (filePath.Equals(""))
                filePath = System.Configuration.ConfigurationManager.AppSettings["LogFile"];
            if (!File.Exists(@filePath))
            {
                var t = File.Create(@filePath);
                t.Close();
            }
            string mm = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

            for (var i = 0; i <= message.Count() - 1; i++)
            {
                mm = mm + " " + message[i];
            }

            StreamWriter sw = File.AppendText(filePath);
            sw.WriteLine(mm);
            sw.Close();

        }

        private static ResultPhase GetResultPhase(BsonDocument phasedoc, List<DateTime> Periods)
        {
            var phase = BsonHelper.Deserialize<WellActivityPhase>(phasedoc.GetDoc("Phase").ToBsonDocument());
            var WellName = phasedoc.GetString("WellName").ToUpper();
            var SequenceId = phasedoc.GetString("SequenceId").ToUpper();
            var ActivityType = phase.ActivityType.ToUpper();
            var WellEvents = new List<ResultTrxData>();
            WellItems well = new WellItems();
            well.ActivityType = ActivityType;
            well.WellName = WellName;

            var newweeklyreport = getWeeklyReport(WellName, SequenceId, ActivityType);
            var newmonthlyreport = getMonthlyReport(WellName, SequenceId, ActivityType);


            DateTime MaxPeriod = Periods.OrderByDescending(x => x).ToList().FirstOrDefault();
            DateTime MinPeriod = Periods.OrderByDescending(x => x).ToList().FirstOrDefault();
            bool checkFromMonthly = false;
            DateTime LastDate = new DateTime();

            #region Weekly Check
            if (newweeklyreport != null)
            {

                List<DateTime> datesweekly = newweeklyreport.Select(s => s.UpdateVersion).OrderBy(x => x).ToList();
                DateTime MaxWeekly = datesweekly.OrderByDescending(x => x).ToList().FirstOrDefault();
                DateTime MinWeekly = datesweekly.OrderByDescending(x => x).ToList().LastOrDefault();
                if (MaxWeekly <= MinPeriod) //All From Weekly
                {
                    foreach (var pr in Periods.OrderBy(x => x).ToList())
                    {
                        WellActivityUpdate wellactivity = new WellActivityUpdate();
                        var resultReport = new ResultTrxData();
                        wellactivity = newweeklyreport.FirstOrDefault(x => x.UpdateVersion.Date.Equals(MaxWeekly.Date));
                        well = GetWellItems(wellactivity.Phase);
                        resultReport.Version = pr.ToString("dd-MMM-yyyy");
                        resultReport.WellItems.Add(well);
                        WellEvents.Add(resultReport);
                    }

                }
                else if (MinWeekly == MaxPeriod)
                {
                    WellActivityUpdate wellactivity = new WellActivityUpdate();
                    var resultReport = new ResultTrxData();
                    wellactivity = newweeklyreport.FirstOrDefault(x => x.UpdateVersion.Date.Equals(MaxPeriod.Date));
                    well = GetWellItems(wellactivity.Phase);
                    resultReport.Version = MaxPeriod.ToString("dd-MMM-yyyy");
                    resultReport.WellItems.Add(well);
                    WellEvents.Add(resultReport);
                    checkFromMonthly = true;
                }
                else
                {
                    foreach (var pr in Periods.OrderByDescending(x => x).ToList())
                    {
                        DateTime setDate = GetNearDate(pr, datesweekly);
                        if (!setDate.Year.Equals(1900))
                        {
                            WellActivityUpdate wellactivity = new WellActivityUpdate();
                            var resultReport = new ResultTrxData();
                            wellactivity = newweeklyreport.FirstOrDefault(x => x.UpdateVersion.Date.Equals(setDate.Date));
                            well = GetWellItems(wellactivity.Phase);
                            resultReport.Version = pr.ToString("dd-MMM-yyyy");
                            resultReport.WellItems.Add(well);
                            WellEvents.Add(resultReport);
                        }
                        else
                        {
                            LastDate = pr;
                            checkFromMonthly = true;
                        }


                    }
                }

            }
            else
            {
                checkFromMonthly = true;
                LastDate = Periods.OrderByDescending(x => x).ToList().LastOrDefault();
            }
            #endregion

            #region Monthly Check
            if (checkFromMonthly)
            {
                if (newmonthlyreport != null)
                {
                    List<DateTime> datesmonthly = newmonthlyreport.Select(s => s.UpdateVersion).OrderBy(x => x).ToList();
                    DateTime MaxMonthly = datesmonthly.OrderByDescending(x => x).ToList().FirstOrDefault();
                    DateTime MinMonthly = datesmonthly.OrderByDescending(x => x).ToList().LastOrDefault();
                    if (MaxMonthly <= MinPeriod) //All From Weekly
                    {
                        foreach (var pr in Periods.OrderBy(x => x).ToList())
                        {
                            WellActivityUpdateMonthly wellactivity = new WellActivityUpdateMonthly();
                            var resultReport = new ResultTrxData();
                            wellactivity = newmonthlyreport.FirstOrDefault(x => x.UpdateVersion.Date.Equals(MaxMonthly.Date));
                            well = GetWellItems(wellactivity.Phase);
                            resultReport.Version = pr.ToString("dd-MMM-yyyy");
                            resultReport.WellItems.Add(well);
                            WellEvents.Add(resultReport);
                        }

                    }
                    else if (MinMonthly == MaxPeriod)
                    {
                        WellActivityUpdateMonthly wellactivity = new WellActivityUpdateMonthly();
                        var resultReport = new ResultTrxData();
                        wellactivity = newmonthlyreport.FirstOrDefault(x => x.UpdateVersion.Date.Equals(MaxPeriod.Date));
                        well = GetWellItems(wellactivity.Phase);
                        resultReport.Version = MaxPeriod.ToString("dd-MMM-yyyy");
                        resultReport.WellItems.Add(well);
                        WellEvents.Add(resultReport);
                        checkFromMonthly = true;
                    }
                    else
                    {
                        var xx = new List<DateTime>();

                        foreach (var pr in Periods.OrderByDescending(x => x).ToList().Where(v => v >= LastDate))
                        {
                            DateTime setDate = GetNearDate(pr, datesmonthly);
                            var resultReport = new ResultTrxData();
                            if (!setDate.Year.Equals(1900))
                            {
                                WellActivityUpdateMonthly wellactivity = new WellActivityUpdateMonthly();

                                wellactivity = newmonthlyreport.FirstOrDefault(x => x.UpdateVersion.Date.Equals(setDate.Date));
                                well = GetWellItems(wellactivity.Phase);
                                resultReport.Version = pr.ToString("dd-MMM-yyyy");
                                resultReport.WellItems.Add(well);
                                WellEvents.Add(resultReport);
                            }
                            else
                            {
                                well = GetWellItems(phase);
                                resultReport.Version = pr.ToString("dd-MMM-yyyy");
                                resultReport.WellItems.Add(well);
                                WellEvents.Add(resultReport);
                            }


                        }
                    }

                }
                else
                {
                    foreach (var pr in Periods.OrderBy(x => x).ToList())
                    {
                        var resultReport = new ResultTrxData();
                        well = GetWellItems(phase);
                        resultReport.Version = pr.ToString("dd-MMM-yyyy");
                        resultReport.WellItems.Add(well);
                        WellEvents.Add(resultReport);
                    }
                }

            }

            #endregion

            var resultphase = new ResultPhase();
            resultphase.ActivityType = ActivityType;
            resultphase.WellName = WellName;
            resultphase.SequenceId = SequenceId;
            resultphase.Phase = WellEvents;

            //WellPhase.Add(resultphase);

            return resultphase;
        }

        private static OPHistory getHistoryReport(string well, string seq, string events)
        {
            var t = WellActivity.Get<WellActivity>(Query.And(
                    Query.EQ("WellName", well),
                    Query.EQ("OPHistories.UARigSequenceId", seq),
                    Query.EQ("OPHistories.ActivityType", events)
                ));
            if (t != null)
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


        public static string GetDataWellPlanAndPip(DateTime parmDate, string parmType, int parmRolling, string parmOP, string parmResultType, List<string> wellNames)
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

            dateStart = Periods.Min(x => x.Date);
            dateFinish = Periods.Max(x => x.Date);

            var AllWellPlans = new List<ResultTrxData>();
            var AllPIP = new List<ResultTrxData>();
            var AllRigPIP = new List<ResultTrxData>();

            List<int> per = new List<int>();
            per = Periods.Select(x => Convert.ToInt32(x.ToString("yyyyMMdd"))).ToList<int>();
            var arrstr = new List<string>();
            //wellNames.Add("L3-NW2");
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

            if (wellNames != null)
                match = @"{$match:{'WellName':{'$in': " + new BsonArray(arrstr) + " }}}";
            string match1 = @"{$match:{ $or:
                                        [{'Phases.PlanSchedule.Start':{'$ne':ISODate('1900-01-01T00:00:00.000+0000')}},
                                         {'Phases.PhSchedule.Start':{'$ne':ISODate('1900-01-01T00:00:00.000+0000')}}
                                        ]}}"; //OP Active
            //string match2 = @"{$match:{'Phases.PhSchedule.Start':{'$ne':ISODate('1900-01-01T00:00:00.000+0000')}}}"; //LS Active
            string sorts = @"{$sort:{'LastUpdate':" + -1 + "}}";

            string project3 = @"{$project:{'LastUpdate':'$LastUpdate','Phase':'$Phases','WellName':'$WellName','SequenceId':'$SequenceId','RigName':'$RigName','History':'$History'}}";
            string limit = @"{ $limit: " + 50 + " }";



            List<BsonDocument> pipelines = new List<BsonDocument>();
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(project1));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(unwind));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(project2));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(match1));
            //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(match2));
            //if (wellNames != null)
            //    pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(match));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(sorts));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(project3));
            //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(limit));
            List<BsonDocument> aggregate = DataHelper.Aggregate(new WellActivity().TableName, pipelines);

            List<WellItems> items = new List<WellItems>();
            #region Detail Well Plan
            foreach (var i in Periods)
            {
                Console.WriteLine(DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss"));
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
                    var DateWeekly = new DateTime(1900, 1, 1);

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

            //foreach (var i in Periods)
            //{

            //    #region Top TEN
            //    ResultTrxData well = new ResultTrxData();
            //    ResultTrxData pip = new ResultTrxData();
            //    ResultTrxData rigpip = new ResultTrxData();

            //    well = FilterEvent(AllWellPlans.FirstOrDefault(x => x.Version.Equals(i.ToString("dd-MMM-yyyy"))), "well");
            //    pip = FilterEvent(AllPIP.FirstOrDefault(x => x.Version.Equals(i.ToString("dd-MMM-yyyy"))), "pip");
            //    rigpip = FilterEvent(AllRigPIP.FirstOrDefault(x => x.Version.Equals(i.ToString("dd-MMM-yyyy"))), "rigpip");


            //    WellPlans.Add(well);
            //    PIP.Add(pip);
            //    RigPIP.Add(rigpip);
            //    #endregion
            //}

            var Periode = Periods.Distinct().Select(x => x.Date.ToString("dd-MMM-yyyy")).ToList();
            var xx = WellPlans;
            var yy = PIP;
            var zz = RigPIP;
            var aa = AllWellPlans;
            var bb = AllPIP;
            var cc = AllRigPIP;
            DataHelper.Delete("_WEISWellActivityUpdateSdl");
            foreach (var i in AllWellPlans)
            {
                DataHelper.Save("_WEISWellActivityUpdateSdl", i.ToBsonDocument());
            }
            DataHelper.Delete("_WEISPipUpdateSdl");
            foreach (var i in AllPIP)
            {
                DataHelper.Save("_WEISPipUpdateSdl", i.ToBsonDocument());
            }
            DataHelper.Delete("_WEISRigPipUpdateSdl");
            foreach (var i in AllRigPIP)
            {
                DataHelper.Save("_WEISRigPipUpdateSdl", i.ToBsonDocument());
            }

            return "OK";
        }

        public static void DataLayer()
        {
            #region query
            //{'Phases.BaseOP':{'$not':{'$size':0}}},
            string aggregateCond1 = "{ $unwind: '$Phases' }";
            string match1 = @"{$match:
                            { 
  
                              '$and' : [
  	  	                                                      

                                   {'Phases.BaseOP':{'$not':{'$size':0}}},
                                    
  		                            
                              ]
  
                            }
                        }";
            string project = @"{$project:
                            {
	                        'OPHistories' : '$OPHistories',
	                        'Region' : '$Region', 
	                        'RigType' : '$RigType',
                            'RigName' : '$RigName',
                            'SequenceId' : '$UARigSequenceId',
                            'PhaseNo' : '$Phases.PhaseNo',
	                        'WellName' : '$WellName',
	                        'ActivityType' : '$Phases.ActivityType',
	                        'BaseOP' : '$Phases.BaseOP',
	                        'Plan' : '$Phases.Plan',
	                        'OP' : '$Phases.OP',
	                        'LE' : '$Phases.LE',
	                        'AFE' : '$Phases.AFE',
	                        'LastWeek' : '$Phases.LastWeek',
	                        'LastMonth' : '$Phases.LastMonth',
	                        'OPSchedule' : '$Phases.OPSchedule',
	                        'PlanSchedule' : '$Phases.PlanSchedule',
	                        'LESchedule' : '$Phases.LESchedule',
	                        'AFESchedule' : '$Phases.AFESchedule',
                            'PHSchedule' : '$Phases.PhSchedule',      
	
                            }
                        }";
            #endregion

            #region Period

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


            #region query builder
            var parmDate = Tools.ToUTC(DateTime.Now);
            cfg.DateFilter = parmDate;
            cfg.NoOfDays = parmRolling;
            cfg.NoOfMonth = parmRolling;
            cfg.NoOfWeek = parmRolling;

            var perioddays = cfg.GetlastDates(parmDate, 7, true);
            var periodweek = cfg.GetlastWeeks(cfg.WeekType.ToString(), parmDate);
            var periodmonth = cfg.GetlastMonths(parmDate, 7, true);
            var periodquarter = cfg.GetlastQuarter(parmDate);

            foreach (var dt in perioddays)
            {
                Periods.Add(dt);
            }
            foreach (var dt in periodweek)
            {
                Periods.Add(dt);
            }
            foreach (var dt in periodmonth)
            {
                Periods.Add(dt);
            }
            foreach (var dt in periodquarter)
            {
                Periods.Add(dt);
            }
            Periods = Periods.Distinct().Select(x => x.Date.Date).OrderByDescending(x => x.Date).ToList();
            #endregion

            #endregion
            List<BsonDocument> pipelines = new List<BsonDocument>();
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
            //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(match1));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(project));
            //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
            List<BsonDocument> aggregates = DataHelper.Aggregate(new WellActivity().TableName, pipelines);

            DateTime xoo = DateTime.Now;
            Console.WriteLine("Data Layer Start On :"+DateTime.Now.ToString(" hh : mm : ss"));
            var length = aggregates.Count;
            int modulo = aggregates.Count % 10;
            int divided = aggregates.Count / 10;
            int totaldiv = divided + modulo;
            //DataHelper.Delete("_WEISCollectionSDL2");

           
            #region
            //Parallel.Invoke(() =>
            //    {
            //        Console.WriteLine("First");
            //        GetBindWPWR(aggregates.Skip(0).Take(divided).ToList(), Periods);
            //    },
            //    () =>
            //    { Console.WriteLine("Second"); 
            //        GetBindWPWR(aggregates.Skip(1 * divided).Take(divided).ToList(), Periods); 
            //    },
            //    () =>
            //    { Console.WriteLine("Third"); GetBindWPWR(aggregates.Skip(2 * divided).Take(divided).ToList(), Periods); },
            //    () =>
            //    { Console.WriteLine("Fourth"); GetBindWPWR(aggregates.Skip(3 * divided).Take(divided).ToList(), Periods); },
            //    () =>
            //    { Console.WriteLine("Fifth"); GetBindWPWR(aggregates.Skip(4 * divided).Take(divided).ToList(), Periods); },
            //    () =>
            //    { Console.WriteLine("Sixth"); GetBindWPWR(aggregates.Skip(5 * divided).Take(divided).ToList(), Periods); },
            //    () =>
            //    { Console.WriteLine("Seventh"); GetBindWPWR(aggregates.Skip(6 * divided).Take(divided).ToList(), Periods); },
            //    () =>
            //    { Console.WriteLine("Eighth"); GetBindWPWR(aggregates.Skip(7 * divided).Take(divided).ToList(), Periods); },
            //    () =>
            //    { Console.WriteLine("Ninth"); GetBindWPWR(aggregates.Skip(8 * divided).Take(divided).ToList(), Periods); },
            //    () =>
            //    { Console.WriteLine("Tenth"); GetBindWPWR(aggregates.Skip(9 * divided).Take(divided + modulo).ToList(), Periods); }
            // );
            #endregion


            GetBindWPWR(aggregates, Periods);
            GetSummary(Periods);
            GroupByWellRig(Periods);

            Console.WriteLine("Data Layer Finish On : " + DateTime.Now.ToString("hh : mm : ss"));
            Console.WriteLine("Total Time in processing data layer : "+(DateTime.Now - xoo).TotalSeconds);
        }


        public static void GetBindWPWR(List<BsonDocument> waUw, List<DateTime> wrPer)
        {
            DataHelper.Delete("_WEISCollectionSDL");
            List<SDLHelper> sdls = new List<SDLHelper>();
            var totalevent = 0;
            foreach (var t in waUw)
            {

                var des = BsonHelper.Deserialize<SDLHelper>(t);
                //des._id = totalevent.ToString()+des.WellName.ToUpper()+des.ActivityType.ToUpper();
                des._id = totalevent++;
                foreach (var y in wrPer)
                {
                    OPHistory history = des.OPHistories.FirstOrDefault(x => x.ActivityType.Equals(des.ActivityType) && x.Type.Equals("OP14") && x.PhaseNo == des.PhaseNo && x.PlanSchedule.Start != Tools.DefaultDate);

                    var yyy = WellActivityUpdate.GetById(des.WellName, des.SequenceId, des.PhaseNo, Tools.ToUTC(y.Date), true, false);
                    var LECost = 0.0;
                    var LEDays = 0.0;
                    var OPCost = 0.0;//LS
                    var OPDays = 0.0;
                    var PlanDays = 0.0;
                    var PlanCost = 0.0;
                    var PlanPrevCost = 0.0;
                    var PlanPrevDays = 0.0;
                    var Div = 1000000;
                    var LeStatus = false;
                    if (history == null || !des.BaseOP.Contains("OP14"))
                    {
                        history = new OPHistory();
                        PlanPrevCost = 0;
                        PlanPrevDays = 0;
                    }
                    else
                    {
                        PlanPrevDays = history.Plan.Days;
                        PlanPrevCost = Tools.Div(history.Plan.Cost, Div);
                    }
                    if (des.BaseOP.Contains("OP15"))
                    {
                        if (!des.ActivityType.ToLower().Contains("risk"))
                        {
                            LECost = Tools.Div(des.LE.Cost, Div);
                            LEDays = des.LE.Days;
                        }

                    }
                    if (des.BaseOP.Contains("OP15"))
                    {
                        PlanDays = des.Plan.Days;
                        PlanCost = Tools.Div(des.Plan.Cost, Div);
                    }
                    if (des.OP != null && des.BaseOP.Contains("OP15"))
                    {
                        OPCost = Tools.Div(des.OP.Cost, Div);
                        OPDays = Tools.Div(des.OP.Days, 1);
                    }

                    //For PIP
                    var Elements = new List<PIPElement>();
                    var CRElements = new List<PIPElement>();
                    if (des.BaseOP.Contains("OP15") || des.BaseOP.Contains("OP14"))
                    {

                        var pip = WellPIP.Get<WellPIP>(
                               Query.And(
                                   Query.EQ("WellName", des.WellName),
                                   Query.EQ("SequenceId", des.SequenceId),
                            //Query.EQ("PhaseNo", des.PhaseNo),
                                   Query.EQ("ActivityType", des.ActivityType)
                               )
                               );
                        if (pip != null)
                        {

                            if (pip.Elements != null)
                            {
                                Elements = pip.Elements.Where(f => f.AssignTOOps.Contains("OP15") || f.AssignTOOps.Contains("OP14")).ToList();
                            }
                            CRElements = pip.CRElements;
                        }
                    }

                    if (yyy != null)
                    {
                        #region check weekly
                        //Well Activity
                        SDLDetails det = new SDLDetails();

                        det.SelectDate = y;
                        det.WRNearSelectedDate = yyy.UpdateVersion;
                        det.WRPlan = new WellDrillData { Identifier = "Current OP", Days = PlanDays, Cost = PlanCost };
                        det.WRPrevPlan = new WellDrillData { Identifier = "Previous OP", Days = PlanPrevDays, Cost = PlanPrevCost };
                        det.WRLE = new WellDrillData { Identifier = "LE", Days = LEDays, Cost = LECost };
                        det.WROP = new WellDrillData { Identifier = "LS", Days = OPDays, Cost = OPCost };
                        det.WRAFE = new WellDrillData { Identifier = "AFE", Days = yyy.AFE.Days, Cost = Tools.Div(yyy.AFE.Cost, Div) };

                        des.WRDetails.Add(det);


                        //PIP
                        SDLPIPDetails pipdet = new SDLPIPDetails();

                        var PlanPipDays = Elements.Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                        var PlanPipCost = Elements.Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                        var LePipDays = Elements.Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                        var LePipCost = Elements.Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                        var PlanPipRealizedDays = Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                        var PlanPipRealizedCost = Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                        var LePipRealizedDays
                            = yyy.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                        var LePipRealizedCost = yyy.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                        var PlanPipUnRealizedDays = Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                        var PlanPipUnRealizedCost = Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                        var LePipUnRealizedDays = Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                        var LePipUnRealizedCost = Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);


                        pipdet.SelectDate = y;
                        pipdet.WRNearSelectedDate = yyy.UpdateVersion;
                        pipdet.OriEst = new WellDrillData() { Identifier = "Original Estimate", Days = PlanPipDays, Cost = Tools.Div(PlanPipCost, 1) };
                        pipdet.CurEst = new WellDrillData() { Identifier = "Current Estimate", Days = LePipDays, Cost = Tools.Div(LePipCost, 1) };
                        pipdet.OriEstRealize = new WellDrillData() { Identifier = "Original Estimate Realize", Days = PlanPipRealizedDays, Cost = Tools.Div(PlanPipRealizedCost, 1) };
                        pipdet.CurEstRealize = new WellDrillData() { Identifier = "Current Estimate Realize", Days = LePipRealizedDays, Cost = Tools.Div(LePipRealizedCost, 1) };
                        pipdet.OriEstUnRealize = new WellDrillData() { Identifier = "Original Estimate UnRealize", Days = PlanPipUnRealizedDays, Cost = Tools.Div(PlanPipUnRealizedCost, 1) };
                        pipdet.CurEstUnRealize = new WellDrillData() { Identifier = "Current Estimate UnRealize", Days = LePipUnRealizedDays, Cost = Tools.Div(LePipUnRealizedCost, 1) };
                        des.PIPDetails.Add(pipdet);

                        //RIG PIP
                        var rigpipdet = new SDLPIPDetails();

                        var PlanRigPipDays = CRElements.Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                        var PlanRigPipCost = CRElements.Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                        var LeRigPipDays = CRElements.Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                        var LeRigPipCost = CRElements.Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                        var PlanRigPipRealizedDays = CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                        var PlanRigPipRealizedCost = CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                        var LeRigPipRealizedDays
                            = yyy.CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                        var LeRigPipRealizedCost = CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                        var PlanRigPipUnRealizedDays = CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                        var PlanRigPipUnRealizedCost = CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                        var LeRigPipUnRealizedDays = CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                        var LeRigPipUnRealizedCost = CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);


                        rigpipdet.SelectDate = y;
                        rigpipdet.WRNearSelectedDate = yyy.UpdateVersion;
                        rigpipdet.OriEst = new WellDrillData() { Identifier = "Original Estimate", Days = PlanRigPipDays, Cost = Tools.Div(PlanRigPipCost, 1) };
                        rigpipdet.CurEst = new WellDrillData() { Identifier = "Current Estimate", Days = LeRigPipDays, Cost = Tools.Div(LeRigPipCost, 1) };
                        rigpipdet.OriEstRealize = new WellDrillData() { Identifier = "Original Estimate Realize", Days = PlanRigPipRealizedDays, Cost = Tools.Div(PlanRigPipRealizedCost, 1) };
                        rigpipdet.CurEstRealize = new WellDrillData() { Identifier = "Current Estimate Realize", Days = LeRigPipRealizedDays, Cost = Tools.Div(LeRigPipRealizedCost, 1) };
                        rigpipdet.OriEstUnRealize = new WellDrillData() { Identifier = "Original Estimate UnRealize", Days = PlanRigPipUnRealizedDays, Cost = Tools.Div(PlanRigPipUnRealizedCost, 1) };
                        rigpipdet.CurEstUnRealize = new WellDrillData() { Identifier = "Current Estimate UnRealize", Days = LeRigPipUnRealizedDays, Cost = Tools.Div(LeRigPipUnRealizedCost, 1) };

                        des.RigPIPDetails.Add(rigpipdet);
                        #endregion

                    }
                    else
                    {
                        // check monthly 

                        var monts = WellActivityUpdateMonthly.GetById(des.WellName, des.SequenceId, des.PhaseNo, Tools.ToUTC(y.Date), true, false);

                        if (monts != null)
                        {
                            #region inisiasi monthly
                            //Well Activity
                            SDLDetails det = new SDLDetails();

                            det.SelectDate = y;
                            det.WRNearSelectedDate = monts.UpdateVersion;
                            det.WRPlan = new WellDrillData { Identifier = "Current OP", Days = PlanDays, Cost = PlanCost };
                            det.WRPrevPlan = new WellDrillData { Identifier = "Previous OP", Days = PlanPrevDays, Cost = PlanPrevCost };
                            det.WRLE = new WellDrillData { Identifier = "LE", Days = LEDays, Cost = LECost };
                            det.WROP = new WellDrillData { Identifier = "LS", Days = OPDays, Cost = OPCost };
                            det.WRAFE = new WellDrillData { Identifier = "AFE", Days = monts.AFE.Days, Cost = Tools.Div(monts.AFE.Cost, Div) };

                            des.WRDetails.Add(det);


                            //PIP
                            SDLPIPDetails pipdet = new SDLPIPDetails();

                            var PlanPipDays = Elements.Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                            var PlanPipCost = Elements.Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                            var LePipDays = Elements.Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                            var LePipCost = Elements.Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                            var PlanPipRealizedDays = Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                            var PlanPipRealizedCost = Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                            var LePipRealizedDays
                                = Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                            var LePipRealizedCost = Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                            var PlanPipUnRealizedDays = Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                            var PlanPipUnRealizedCost = Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                            var LePipUnRealizedDays = Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                            var LePipUnRealizedCost = Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);


                            pipdet.SelectDate = y;
                            pipdet.WRNearSelectedDate = monts.UpdateVersion;
                            pipdet.OriEst = new WellDrillData() { Identifier = "Original Estimate", Days = PlanPipDays, Cost = Tools.Div(PlanPipCost, 1) };
                            pipdet.CurEst = new WellDrillData() { Identifier = "Current Estimate", Days = LePipDays, Cost = Tools.Div(LePipCost, 1) };
                            pipdet.OriEstRealize = new WellDrillData() { Identifier = "Original Estimate Realize", Days = PlanPipRealizedDays, Cost = Tools.Div(PlanPipRealizedCost, 1) };
                            pipdet.CurEstRealize = new WellDrillData() { Identifier = "Current Estimate Realize", Days = LePipRealizedDays, Cost = Tools.Div(LePipRealizedCost, 1) };
                            pipdet.OriEstUnRealize = new WellDrillData() { Identifier = "Original Estimate UnRealize", Days = PlanPipUnRealizedDays, Cost = Tools.Div(PlanPipUnRealizedCost, 1) };
                            pipdet.CurEstUnRealize = new WellDrillData() { Identifier = "Current Estimate UnRealize", Days = LePipUnRealizedDays, Cost = Tools.Div(LePipUnRealizedCost, 1) };
                            des.PIPDetails.Add(pipdet);

                            //RIG PIP
                            var rigpipdet = new SDLPIPDetails();

                            var PlanRigPipDays = CRElements.Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                            var PlanRigPipCost = CRElements.Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                            var LeRigPipDays = CRElements.Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                            var LeRigPipCost = CRElements.Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                            var PlanRigPipRealizedDays = CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                            var PlanRigPipRealizedCost = CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                            var LeRigPipRealizedDays
                                = CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                            var LeRigPipRealizedCost = CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                            var PlanRigPipUnRealizedDays = CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                            var PlanRigPipUnRealizedCost = CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                            var LeRigPipUnRealizedDays = CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                            var LeRigPipUnRealizedCost = CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);


                            rigpipdet.SelectDate = y;
                            rigpipdet.WRNearSelectedDate = monts.UpdateVersion;
                            rigpipdet.OriEst = new WellDrillData() { Identifier = "Original Estimate", Days = PlanRigPipDays, Cost = Tools.Div(PlanRigPipCost, 1) };
                            rigpipdet.CurEst = new WellDrillData() { Identifier = "Current Estimate", Days = LeRigPipDays, Cost = Tools.Div(LeRigPipCost, 1) };
                            rigpipdet.OriEstRealize = new WellDrillData() { Identifier = "Original Estimate Realize", Days = PlanRigPipRealizedDays, Cost = Tools.Div(PlanRigPipRealizedCost, 1) };
                            rigpipdet.CurEstRealize = new WellDrillData() { Identifier = "Current Estimate Realize", Days = LeRigPipRealizedDays, Cost = Tools.Div(LeRigPipRealizedCost, 1) };
                            rigpipdet.OriEstUnRealize = new WellDrillData() { Identifier = "Original Estimate UnRealize", Days = PlanRigPipUnRealizedDays, Cost = Tools.Div(PlanRigPipUnRealizedCost, 1) };
                            rigpipdet.CurEstUnRealize = new WellDrillData() { Identifier = "Current Estimate UnRealize", Days = LeRigPipUnRealizedDays, Cost = Tools.Div(LeRigPipUnRealizedCost, 1) };

                            des.RigPIPDetails.Add(rigpipdet);
                            #endregion
                        }
                        else
                        {
                            // no monthly

                            #region inisiasi Non Week and Non Month
                            //Well Activity
                            SDLDetails det = new SDLDetails();

                            det.SelectDate = y;
                            det.WRNearSelectedDate = y;
                            det.WRPlan = new WellDrillData { Identifier = "Current OP", Days = PlanDays, Cost = PlanCost };
                            det.WRPrevPlan = new WellDrillData { Identifier = "Previous OP", Days = PlanPrevDays, Cost = PlanPrevCost };
                            det.WRLE = new WellDrillData { Identifier = "LE", Days = LEDays, Cost = LECost };
                            det.WROP = new WellDrillData { Identifier = "LS", Days = OPDays, Cost = OPCost };
                            det.WRAFE = new WellDrillData { Identifier = "AFE", Days = des.AFE.Days, Cost = Tools.Div(des.AFE.Cost, Div) };

                            des.WRDetails.Add(det);

                            //PIP
                            SDLPIPDetails pipdet = new SDLPIPDetails();

                            var PlanPipDays = Elements.Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                            var PlanPipCost = Elements.Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                            var LePipDays = Elements.Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                            var LePipCost = Elements.Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                            var PlanPipRealizedDays = Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                            var PlanPipRealizedCost = Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                            var LePipRealizedDays
                                = Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                            var LePipRealizedCost = Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                            var PlanPipUnRealizedDays = Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                            var PlanPipUnRealizedCost = Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                            var LePipUnRealizedDays = Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                            var LePipUnRealizedCost = Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);


                            pipdet.SelectDate = y;
                            pipdet.WRNearSelectedDate = y;
                            pipdet.OriEst = new WellDrillData() { Identifier = "Original Estimate", Days = PlanPipDays, Cost = Tools.Div(PlanPipCost, 1) };
                            pipdet.CurEst = new WellDrillData() { Identifier = "Current Estimate", Days = LePipDays, Cost = Tools.Div(LePipCost, 1) };
                            pipdet.OriEstRealize = new WellDrillData() { Identifier = "Original Estimate Realize", Days = PlanPipRealizedDays, Cost = Tools.Div(PlanPipRealizedCost, 1) };
                            pipdet.CurEstRealize = new WellDrillData() { Identifier = "Current Estimate Realize", Days = LePipRealizedDays, Cost = Tools.Div(LePipRealizedCost, 1) };
                            pipdet.OriEstUnRealize = new WellDrillData() { Identifier = "Original Estimate UnRealize", Days = PlanPipUnRealizedDays, Cost = Tools.Div(PlanPipUnRealizedCost, 1) };
                            pipdet.CurEstUnRealize = new WellDrillData() { Identifier = "Current Estimate UnRealize", Days = LePipUnRealizedDays, Cost = Tools.Div(LePipUnRealizedCost, 1) };
                            des.PIPDetails.Add(pipdet);

                            //RIG PIP
                            var rigpipdet = new SDLPIPDetails();

                            var PlanRigPipDays = CRElements.Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                            var PlanRigPipCost = CRElements.Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                            var LeRigPipDays = CRElements.Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                            var LeRigPipCost = CRElements.Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                            var PlanRigPipRealizedDays = CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                            var PlanRigPipRealizedCost = CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                            var LeRigPipRealizedDays
                                = CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                            var LeRigPipRealizedCost = CRElements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);

                            var PlanRigPipUnRealizedDays = CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysPlanImprovement + x.DaysPlanRisk);
                            var PlanRigPipUnRealizedCost = CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostPlanImprovement + x.CostPlanRisk);
                            var LeRigPipUnRealizedDays = CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                            var LeRigPipUnRealizedCost = CRElements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk);


                            rigpipdet.SelectDate = y;
                            rigpipdet.WRNearSelectedDate = y;
                            rigpipdet.OriEst = new WellDrillData() { Identifier = "Original Estimate", Days = PlanRigPipDays, Cost = Tools.Div(PlanRigPipCost, 1) };
                            rigpipdet.CurEst = new WellDrillData() { Identifier = "Current Estimate", Days = LeRigPipDays, Cost = Tools.Div(LeRigPipCost, 1) };
                            rigpipdet.OriEstRealize = new WellDrillData() { Identifier = "Original Estimate Realize", Days = PlanRigPipRealizedDays, Cost = Tools.Div(PlanRigPipRealizedCost, 1) };
                            rigpipdet.CurEstRealize = new WellDrillData() { Identifier = "Current Estimate Realize", Days = LeRigPipRealizedDays, Cost = Tools.Div(LeRigPipRealizedCost, 1) };
                            rigpipdet.OriEstUnRealize = new WellDrillData() { Identifier = "Original Estimate UnRealize", Days = PlanRigPipUnRealizedDays, Cost = Tools.Div(PlanRigPipUnRealizedCost, 1) };
                            rigpipdet.CurEstUnRealize = new WellDrillData() { Identifier = "Current Estimate UnRealize", Days = LeRigPipUnRealizedDays, Cost = Tools.Div(LeRigPipUnRealizedCost, 1) };

                            des.RigPIPDetails.Add(rigpipdet);
                            #endregion
                        }

                    }
                    //break;
                }
                sdls.Add(des);
                if (des == null)
                {
                    des = new SDLHelper();
                }
                DataHelper.Save("_WEISCollectionSDL", des.ToBsonDocument());
            }

            #region
            //List<SDLDetails> SummaryWell = new List<SDLDetails>();
            //DataHelper.Delete("_WEISCollectionSummarySDL");
            //foreach (var dt in wrPer)
            //{
            //    var summary = new SDLSummary();
            //    summary.Date = dt;
            //    //Well
            //    var PlanDays = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WRPlan.Days);
            //    var PlanCost = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WRPlan.Cost);

            //    var PlanPrevDays = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WRPrevPlan.Days);
            //    var PlanPrevCost = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WRPrevPlan.Cost);

            //    var LSDays = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WROP.Days);
            //    var LSCost = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WROP.Cost);

            //    var LEDays = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WRLE.Days);
            //    var LECost = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WRLE.Cost);

            //    var AFEDays = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WRAFE.Days);
            //    var AFECost = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WRAFE.Cost);

            //    var sum = new SDLDetails();
            //    sum.SelectDate = dt;
            //    sum.WRNearSelectedDate = dt;
            //    sum.WRPlan = new WellDrillData { Identifier = "Current OP", Days = PlanDays, Cost =Tools.Div(PlanCost,1) };
            //    sum.WRPrevPlan = new WellDrillData { Identifier = "Previous OP", Days = PlanPrevDays, Cost =Tools.Div(PlanPrevCost,1)  };
            //    sum.WRLE = new WellDrillData { Identifier = "LE", Days = LEDays, Cost = Tools.Div(LECost,1) };
            //    sum.WROP = new WellDrillData { Identifier = "LS", Days = LSDays, Cost = Tools.Div(LSCost,1) };
            //    sum.WRAFE = new WellDrillData { Identifier = "AFE", Days = AFEDays, Cost = Tools.Div(AFECost, 1) };
            //    summary.WRDetails = sum;
            //    //PIP
            //    var OriEstDays = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEst.Days);
            //    var OriEstCost = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEst.Cost);

            //    var CurEstDays = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEst.Days);
            //    var CurEstCost = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEst.Cost);

            //    var OriEstDaysRealize = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEstRealize.Days);
            //    var OriEstCostRealize = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEstRealize.Cost);

            //    var CurEstDaysRealize = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEstRealize.Days);
            //    var CurEstCostRealize = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEstRealize.Cost);

            //    var OriEstDaysUnRealize = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEstUnRealize.Days);
            //    var OriEstCostUnRealize = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEstUnRealize.Cost);

            //    var CurEstDaysUnRealize = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEstUnRealize.Days);
            //    var CurEstCostUnRealize = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEstUnRealize.Cost);

            //    var sumPIPDetails = new SDLPIPDetails();
            //    sumPIPDetails.SelectDate = dt;
            //    sumPIPDetails.WRNearSelectedDate = dt;
            //    sumPIPDetails.OriEst = new WellDrillData() { Identifier = "Original Estimate", Days = OriEstDays, Cost = Tools.Div(OriEstCost, 1) };
            //    sumPIPDetails.CurEst = new WellDrillData() { Identifier = "Current Estimate", Days = CurEstDays, Cost = Tools.Div(CurEstCost, 1) };
            //    sumPIPDetails.OriEstRealize = new WellDrillData() { Identifier = "Original Estimate Realize", Days = OriEstDaysRealize, Cost = Tools.Div(OriEstCostRealize, 1) };
            //    sumPIPDetails.CurEstRealize = new WellDrillData() { Identifier = "Current Estimate Realize", Days = CurEstDaysRealize, Cost = Tools.Div(CurEstCostRealize, 1) };
            //    sumPIPDetails.OriEstUnRealize = new WellDrillData() { Identifier = "Original Estimate UnRealize", Days = OriEstDaysUnRealize, Cost = Tools.Div(OriEstCostUnRealize, 1) };
            //    sumPIPDetails.CurEstUnRealize = new WellDrillData() { Identifier = "Current Estimate UnRealize", Days = CurEstDaysUnRealize, Cost = Tools.Div(CurEstCostUnRealize, 1) };


            //    //Rig PIP
            //    var OriEstDaysRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEst.Days);
            //    var OriEstCostRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEst.Cost);

            //    var CurEstDaysRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEst.Days);
            //    var CurEstCostRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEst.Cost);

            //    var OriEstDaysRealizeRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEstRealize.Days);
            //    var OriEstCostRealizeRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEstRealize.Cost);

            //    var CurEstDaysRealizeRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEstRealize.Days);
            //    var CurEstCostRealizeRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEstRealize.Cost);

            //    var OriEstDaysUnRealizeRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEstUnRealize.Days);
            //    var OriEstCostUnRealizeRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEstUnRealize.Cost);

            //    var CurEstDaysUnRealizeRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEstUnRealize.Days);
            //    var CurEstCostUnRealizeRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEstUnRealize.Cost);

            //    var sumRigPIPDetails = new SDLPIPDetails();
            //    sumRigPIPDetails.SelectDate = dt;
            //    sumRigPIPDetails.WRNearSelectedDate = dt;
            //    sumRigPIPDetails.OriEst = new WellDrillData() { Identifier = "Original Estimate", Days = OriEstDaysRig, Cost = Tools.Div(OriEstCostRig, 1) };
            //    sumRigPIPDetails.CurEst = new WellDrillData() { Identifier = "Current Estimate", Days = CurEstDaysRig, Cost = Tools.Div(CurEstCostRig, 1) };
            //    sumRigPIPDetails.OriEstRealize = new WellDrillData() { Identifier = "Original Estimate Realize", Days = OriEstDaysRealizeRig, Cost = Tools.Div(OriEstCostRealizeRig, 1) };
            //    sumRigPIPDetails.CurEstRealize = new WellDrillData() { Identifier = "Current Estimate Realize", Days = CurEstDaysRealizeRig, Cost = Tools.Div(CurEstCostRealizeRig, 1) };
            //    sumRigPIPDetails.OriEstUnRealize = new WellDrillData() { Identifier = "Original Estimate UnRealize", Days = OriEstDaysUnRealizeRig, Cost = Tools.Div(OriEstCostUnRealizeRig, 1) };
            //    sumRigPIPDetails.CurEstUnRealize = new WellDrillData() { Identifier = "Current Estimate UnRealize", Days = CurEstDaysUnRealizeRig, Cost = Tools.Div(CurEstCostUnRealizeRig, 1) };

            //    summary.WRDetails = sum;
            //    summary.PIPDetails = sumPIPDetails;
            //    summary.RigPIPDetails = sumRigPIPDetails;

            //    DataHelper.Save("_WEISCollectionSummarySDL", summary.ToBsonDocument());
            //    //break;
            //} Summary
            #endregion
            

        }

        public static void GetSummary(List<DateTime> Periods)
        {
            var populate = DataHelper.Populate("_WEISCollectionSDL");
            var sdls = new List<SDLHelper>();
            foreach (var data in populate)
            {
                var dt = BsonHelper.Deserialize<SDLHelper>(data);
                if (!dt.WRDetails.Any())
                {
                    var t = 0;
                }
                
                sdls.Add(dt);
            }
            var Div = 1000000;
            var SummaryWell = new List<SDLDetails>();
            DataHelper.Delete("_WEISCollectionSummarySDL");
            foreach (var dt in Periods)
            {
                var summary = new SDLSummary();
                summary.Date = dt;
                //Well
                var PlanDays = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WRPlan.Days);
                var PlanCost = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WRPlan.Cost);

                var PlanPrevDays = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WRPrevPlan.Days);
                var PlanPrevCost = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WRPrevPlan.Cost);

                var LSDays = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WROP.Days);
                var LSCost = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WROP.Cost);

                var LEDays = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WRLE.Days);
                var LECost = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WRLE.Cost);

                var AFEDays = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WRAFE.Days);
                var AFECost = sdls.Sum(x => x.WRDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).WRAFE.Cost);

                var sumWRDetails = new SDLDetails();
                sumWRDetails.SelectDate = dt;
                sumWRDetails.WRNearSelectedDate = dt;
                sumWRDetails.WRPlan = new WellDrillData { Identifier = "Current OP", Days = PlanDays, Cost = PlanCost };
                sumWRDetails.WRPrevPlan = new WellDrillData { Identifier = "Previous OP", Days = PlanPrevDays, Cost = PlanPrevCost };
                sumWRDetails.WRLE = new WellDrillData { Identifier = "LE", Days = LEDays, Cost = LECost };
                sumWRDetails.WROP = new WellDrillData { Identifier = "LS", Days = LSDays, Cost = LSCost };
                sumWRDetails.WRAFE = new WellDrillData { Identifier = "AFE", Days = AFEDays, Cost = AFECost };


                //PIP
                var OriEstDays = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEst.Days);
                var OriEstCost = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEst.Cost);

                var CurEstDays = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEst.Days);
                var CurEstCost = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEst.Cost);

                var OriEstDaysRealize = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEstRealize.Days);
                var OriEstCostRealize = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEstRealize.Cost);

                var CurEstDaysRealize = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEstRealize.Days);
                var CurEstCostRealize = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEstRealize.Cost);

                var OriEstDaysUnRealize = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEstUnRealize.Days);
                var OriEstCostUnRealize = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEstUnRealize.Cost);

                var CurEstDaysUnRealize = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEstUnRealize.Days);
                var CurEstCostUnRealize = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEstUnRealize.Cost);

                var sumPIPDetails = new SDLPIPDetails();
                sumPIPDetails.SelectDate = dt;
                sumPIPDetails.WRNearSelectedDate = dt;
                sumPIPDetails.OriEst = new WellDrillData() { Identifier = "Original Estimate", Days = OriEstDays, Cost = OriEstCost };
                sumPIPDetails.CurEst = new WellDrillData() { Identifier = "Current Estimate", Days = CurEstDays, Cost = CurEstCost };
                sumPIPDetails.OriEstRealize = new WellDrillData() { Identifier = "Original Estimate Realize", Days = OriEstDaysRealize, Cost = OriEstCostRealize };
                sumPIPDetails.CurEstRealize = new WellDrillData() { Identifier = "Current Estimate Realize", Days = CurEstDaysRealize, Cost = CurEstCostRealize };
                sumPIPDetails.OriEstUnRealize = new WellDrillData() { Identifier = "Original Estimate UnRealize", Days = OriEstDaysUnRealize, Cost = OriEstCostUnRealize };
                sumPIPDetails.CurEstUnRealize = new WellDrillData() { Identifier = "Current Estimate UnRealize", Days = CurEstDaysUnRealize, Cost = CurEstCostUnRealize };


                //Rig PIP
                var OriEstDaysRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEst.Days);
                var OriEstCostRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEst.Cost);

                var CurEstDaysRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEst.Days);
                var CurEstCostRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEst.Cost);

                var OriEstDaysRealizeRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEstRealize.Days);
                var OriEstCostRealizeRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEstRealize.Cost);

                var CurEstDaysRealizeRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEstRealize.Days);
                var CurEstCostRealizeRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEstRealize.Cost);

                var OriEstDaysUnRealizeRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEstUnRealize.Days);
                var OriEstCostUnRealizeRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).OriEstUnRealize.Cost);

                var CurEstDaysUnRealizeRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEstUnRealize.Days);
                var CurEstCostUnRealizeRig = sdls.Sum(x => x.PIPDetails.FirstOrDefault(y => y.SelectDate.Date.Equals(dt.Date)).CurEstUnRealize.Cost);

                var sumRigPIPDetails = new SDLPIPDetails();
                sumRigPIPDetails.SelectDate = dt;
                sumRigPIPDetails.WRNearSelectedDate = dt;
                sumRigPIPDetails.OriEst = new WellDrillData() { Identifier = "Original Estimate", Days = OriEstDaysRig, Cost = OriEstCostRig };
                sumRigPIPDetails.CurEst = new WellDrillData() { Identifier = "Current Estimate", Days = CurEstDaysRig, Cost = CurEstCostRig };
                sumRigPIPDetails.OriEstRealize = new WellDrillData() { Identifier = "Original Estimate Realize", Days = OriEstDaysRealizeRig, Cost = OriEstCostRealizeRig };
                sumRigPIPDetails.CurEstRealize = new WellDrillData() { Identifier = "Current Estimate Realize", Days = CurEstDaysRealizeRig, Cost = CurEstCostRealizeRig };
                sumRigPIPDetails.OriEstUnRealize = new WellDrillData() { Identifier = "Original Estimate UnRealize", Days = OriEstDaysUnRealizeRig, Cost = OriEstCostUnRealizeRig };
                sumRigPIPDetails.CurEstUnRealize = new WellDrillData() { Identifier = "Current Estimate UnRealize", Days = CurEstDaysUnRealizeRig, Cost = CurEstCostUnRealizeRig };

                summary.WRDetails = sumWRDetails;
                summary.PIPDetails = sumPIPDetails;
                summary.RigPIPDetails = sumRigPIPDetails;

                DataHelper.Save("_WEISCollectionSummarySDL", summary.ToBsonDocument());
            }
        }

        public static string GetAllDetail(List<SDLHelper> grp, List<DateTime> Periods, out List<SDLDetails> WRDetails, out List<SDLPIPDetails> PIPDetails, out List<SDLPIPDetails> RigPIPDetails)
        {
            var xx = new List<SDLDetails>();
            var yy = new List<SDLPIPDetails>();
            var zz = new List<SDLPIPDetails>();
            foreach (var per in Periods)
            {
                var welldetails = new SDLDetails();
                welldetails.SelectDate = per;
                welldetails.WRNearSelectedDate = per;


                var pipdetails = new SDLPIPDetails();
                pipdetails.SelectDate = per;
                pipdetails.WRNearSelectedDate = per;

                var rigpipdetails = new SDLPIPDetails();
                rigpipdetails.SelectDate = per;
                rigpipdetails.WRNearSelectedDate = per;

                foreach (var grp2 in grp)
                {
                    //Well

                    welldetails.WRPlan.Days += grp2.WRDetails.Where(x => x.SelectDate.Date.Equals(per.Date)).FirstOrDefault().WRPlan.Days;
                    welldetails.WRPlan.Cost += grp2.WRDetails.Where(x => x.SelectDate.Date.Equals(per.Date)).FirstOrDefault().WRPlan.Cost;

                    welldetails.WRPrevPlan.Days += grp2.WRDetails.Where(x => x.SelectDate.Date.Equals(per.Date)).FirstOrDefault().WRPrevPlan.Days;
                    welldetails.WRPrevPlan.Cost += grp2.WRDetails.Where(x => x.SelectDate.Date.Equals(per.Date)).FirstOrDefault().WRPrevPlan.Cost;

                    welldetails.WRLE.Days += grp2.WRDetails.Where(x => x.SelectDate.Date.Equals(per.Date)).FirstOrDefault().WRLE.Days;
                    welldetails.WRLE.Cost += grp2.WRDetails.Where(x => x.SelectDate.Date.Equals(per.Date)).FirstOrDefault().WRLE.Cost;

                    welldetails.WROP.Days += grp2.WRDetails.Where(x => x.SelectDate.Date.Equals(per.Date)).FirstOrDefault().WROP.Days;
                    welldetails.WROP.Cost += grp2.WRDetails.Where(x => x.SelectDate.Date.Equals(per.Date)).FirstOrDefault().WROP.Cost;

                    welldetails.WRAFE.Days += grp2.WRDetails.Where(x => x.SelectDate.Date.Equals(per.Date)).FirstOrDefault().WRAFE.Days;
                    welldetails.WRAFE.Cost += grp2.WRDetails.Where(x => x.SelectDate.Date.Equals(per.Date)).FirstOrDefault().WRAFE.Cost;

                }
                welldetails.WRPlan.Identifier = "Current OP";
                //welldetails.WRPlan.Cost =Tools.Div(welldetails.WRPlan.Cost,1000000);
                welldetails.WRPrevPlan.Identifier = "Previous OP";
                //welldetails.WRPrevPlan.Cost = Tools.Div(welldetails.WRPrevPlan.Cost, 1000000);
                welldetails.WRAFE.Identifier = "AFE";
                //welldetails.WRAFE.Cost = Tools.Div(welldetails.WRAFE.Cost, 1000000);
                welldetails.WRLE.Identifier = "LE";
                //welldetails.WRLE.Cost = Tools.Div(welldetails.WRLE.Cost, 1000000);
                welldetails.WROP.Identifier = "LS";
                //welldetails.WROP.Cost = Tools.Div(welldetails.WROP.Cost, 1000000);
                xx.Add(welldetails);
                yy.Add(pipdetails);
                zz.Add(rigpipdetails);

            }
            WRDetails = xx;
            PIPDetails = yy;
            RigPIPDetails = zz;
            return "OK";
        }

        public static void GroupByWellRig(List<DateTime> Periods)
        {
            List<SDLHelper> Datas = new List<SDLHelper>();
            var populate = DataHelper.Populate("_WEISCollectionSDL");
            foreach (var data in populate)
            {
                var helper = BsonHelper.Deserialize<SDLHelper>(data);
                Datas.Add(helper);

            }

            var groupByWell = Datas.GroupBy(x => x.WellName).Select(grp => grp.ToList()).ToList();
            List<SDLHelper> ListGroupByWell = new List<SDLHelper>();
            var doc = 1;
            DataHelper.Delete("_WEISCollectionSDLByWellName");
            var totalWellCost = 0.0;
            var totalWellDays = 0.0;
            foreach (var grp in groupByWell)
            {
                var sdlhelp = new SDLHelper();
                sdlhelp._id = doc++;
                sdlhelp.WellName = grp.FirstOrDefault().WellName;
                var WRDetails = new List<SDLDetails>();
                var PIPDetails = new List<SDLPIPDetails>();
                var RigPIPDetails = new List<SDLPIPDetails>();

                string status = GetAllDetail(grp, Periods, out WRDetails, out PIPDetails, out RigPIPDetails);
                sdlhelp.WRDetails = WRDetails;
                //sdlhelp.PIPDetails = PIPDetails;
                //sdlhelp.RigPIPDetails = RigPIPDetails;
                DataHelper.Save("_WEISCollectionSDLByWellName", sdlhelp.ToBsonDocument());
                ListGroupByWell.Add(sdlhelp);
                totalWellCost += sdlhelp.WRDetails.FirstOrDefault().WRPlan.Cost;
                totalWellDays += sdlhelp.WRDetails.FirstOrDefault().WRPlan.Days;

            }

            var groupByRig = Datas.GroupBy(x => x.RigName).Select(grp => grp.ToList()).ToList();
            List<SDLHelper> ListGroupByRig = new List<SDLHelper>();
            doc = 1;
            var totalRigCost = 0.0;
            var totalRigDays = 0.0;
            DataHelper.Delete("_WEISCollectionSDLByRigName");
            foreach (var grp in groupByRig)
            {
                var sdlhelp = new SDLHelper();
                sdlhelp._id = doc++;
                sdlhelp.RigName = grp.FirstOrDefault().WellName;
                var WRDetails = new List<SDLDetails>();
                var PIPDetails = new List<SDLPIPDetails>();
                var RigPIPDetails = new List<SDLPIPDetails>();

                string status = GetAllDetail(grp, Periods, out WRDetails, out PIPDetails, out RigPIPDetails);
                sdlhelp.WRDetails = WRDetails;
                //sdlhelp.PIPDetails = PIPDetails;
                //sdlhelp.RigPIPDetails = RigPIPDetails;
                DataHelper.Save("_WEISCollectionSDLByRigName", sdlhelp.ToBsonDocument());
                ListGroupByWell.Add(sdlhelp);
                totalRigCost += sdlhelp.WRDetails.FirstOrDefault().WRPlan.Cost;
                totalRigDays += sdlhelp.WRDetails.FirstOrDefault().WRPlan.Days;

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

        public class SDLSummary
        {
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

        private static ResultTrxData FilterEvent(ResultTrxData data, string item)
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

        private static CustomeVariable SetCustomeValueWeekly(WellActivityUpdate wellactivity, OPHistory History)
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

        private static CustomeVariable SetCustomeValueMonthly(WellActivityUpdateMonthly wellactivity, OPHistory History)
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

        internal class SummaryPhase
        {
            public int Date { get; set; }
            public WellItems Items { get; set; }
        }

        internal class DateWeeklyMonthly
        {
            public string WellName { get; set; }
            public string ActivityType { get; set; }
            public string SequenceId { get; set; }
            public string From { get; set; }
            public List<string> Date { get; set; }
        }
    }
}
