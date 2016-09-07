using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using System.Configuration;

namespace ECIS.ConsoleApp.SysHealthCheck
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ECIS.Core.License.ConfigFolder = ConfigurationManager.AppSettings["License"];// System.IO.Directory.GetCurrentDirectory() + @"\License";
            ECIS.Core.License.LoadLicense();

            ConsoleWrite("Start...");

            var parmDate = new DateTime(2016, 3, 28, 0, 0, 0);
            var parmResultType = "last 7 days";
            var parmType = "daily";
            var parmRolling = 1;
            var parmOP = "OP15";
            var wellNames = new List<string>();
            string result = GetDataWellPlanAndPip(parmDate, parmType, parmRolling, parmOP, parmResultType,wellNames);

            
            ConsoleWrite("done");
        }

        private static ResultPhase GetResultPhase(BsonDocument phasedoc,List<DateTime> Periods)
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
                return t.OrderByDescending(x=>x.UpdateVersion).ToList();
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
