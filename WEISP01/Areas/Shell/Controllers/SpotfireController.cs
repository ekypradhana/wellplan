using ECIS.Client.WEIS;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class SpotfireController : Controller
    {
        //
        // GET: /Shell/Spotfire/
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Spotfire2");
            //return View();
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

        private double GetRatio(DateRange period, WaterfallBase wb)
        {
            if (wb.PeriodView != null && wb.PeriodView.ToLower().Contains("fiscal"))
            {
                wb.DateStart = (wb.DateStart == null ? "" : wb.DateStart);
                wb.DateFinish = (wb.DateFinish == null ? "" : wb.DateFinish);

                string format = "yyyy-MM-dd";
                CultureInfo culture = CultureInfo.InvariantCulture;

                var filter = new DateRange()
                {
                    Start = Tools.ToUTC(wb.DateStart != "" ? DateTime.ParseExact(wb.DateStart, format, culture) : new DateTime(1900, 1, 1)),
                    Finish = Tools.ToUTC(wb.DateFinish != "" ? DateTime.ParseExact(wb.DateFinish, format, culture) : new DateTime(3000, 1, 1))
                };

                var fiscalYears = new List<Int32>();
                if (wb.FiscalYearStart == 0 || wb.FiscalYearFinish == 0)
                {

                }
                else
                {
                    var res = new List<int>();
                    for (var i = wb.FiscalYearStart; i <= wb.FiscalYearFinish; i++)
                        fiscalYears.Add(i);
                }
                var isInvalidFilter = (filter.Start == Tools.DefaultDate);

                if (isInvalidFilter)
                    filter = period;

                var ratios = DateRangeToMonth.ProportionNumDaysPerYear(period, filter).Where(f =>
                {
                    if (fiscalYears.Count > 0)
                        return fiscalYears.Contains(f.Key);

                    return true;
                });

                return ratios.Select(f => f.Value).DefaultIfEmpty(0).Sum(f => f);
            }

            return 1.0;
        }

        public static BizPlanActivity GetBizPlan(string well, string seq, string events)
        {
            try
            {

                var t = BizPlanActivity.Populate<BizPlanActivity>(
                 Query.And(
                     Query.EQ("WellName", well),
                     Query.EQ("UARigSequenceId", seq),
                     Query.EQ("Phases.ActivityType", events)
                 )
                 );
                if (t != null && t.Count() > 0)
                {
                    if (t.FirstOrDefault().Phases.Where(x => x.ActivityType.Equals(events)).Count() > 0)
                    {
                        var ph = t.FirstOrDefault().Phases.Where(x => x.ActivityType.Equals(events)).FirstOrDefault();

                        return t.FirstOrDefault();
                    }
                    else
                        return null;
                }
                else
                {
                    return null;

                }

            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public static bool isHaveBizPlan(string well, string seq, string events, List<string> status)
        {
            try
            {
                if (status != null)
                {
                    var t = BizPlanActivity.Populate<BizPlanActivity>(
                     Query.And(
                         Query.EQ("WellName", well),
                         Query.EQ("UARigSequenceId", seq),
                         Query.EQ("Phases.ActivityType", events)
                     )
                     );
                    if (t != null && t.Count() > 0)
                    {
                        if (t.FirstOrDefault().Phases.Where(x => x.ActivityType.Equals(events)).Count() > 0)
                        {
                            var ph = t.FirstOrDefault().Phases.Where(x => x.ActivityType.Equals(events)).FirstOrDefault();

                            if (status.Contains(ph.Estimate.Status.Trim()))
                                return true;
                            else
                                return false;
                        }
                        else
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public List<Spotfire> GetSpot(WellActivity wa, List<string> Status, string BaseOP = "")
        {
            List<Spotfire> spots = new List<Spotfire>();
            foreach (var t in wa.Phases)
            {
                var cStatus = true;

                if (Status.Any())
                {

                    cStatus = isHaveBizPlan(wa.WellName, wa.UARigSequenceId, t.ActivityType, Status);

                }
                var bp = GetBizPlan(wa.WellName, wa.UARigSequenceId, t.ActivityType);
                if ((
                    (bp != null && bp.Phases.Where(x => x.ActivityType.Equals(t.ActivityType)).Count() > 0) || t.PlanSchedule.Start != Tools.DefaultDate)
                    && cStatus)
                {
                    #region Query
                    var q = Query.Null;
                    if (BaseOP.Equals(""))
                    {
                        q = Query.And(
                           Query.EQ("WellName", wa.WellName),
                           Query.EQ("WellActivityId", wa._id.ToString()),
                           Query.EQ("ActivityType", t.ActivityType),
                           Query.EQ("RigName", wa.RigName),
                           Query.EQ("SequenceId", wa.UARigSequenceId)
                       );
                    }
                    else
                    {
                        q = Query.And(
                           Query.EQ("WellName", wa.WellName),
                           Query.EQ("WellActivityId", wa._id.ToString()),
                           Query.EQ("ActivityType", t.ActivityType),
                           Query.EQ("RigName", wa.RigName),
                           Query.EQ("SequenceId", wa.UARigSequenceId),
                           Query.EQ("OPType", BaseOP)
                       );
                    }


                    #endregion


                    var bpexist = bp != null ? bp.Phases.Where(x => x.ActivityType.Equals(t.ActivityType)).FirstOrDefault() : null;
                    if (bpexist != null && bpexist.Estimate.Status == "Draft")
                    {

                        // ada bizplan Draft
                        var years = DateRangeToMonth.SplitedDateRangeYearly(bpexist.Estimate.EstimatePeriod);
                        if (years.Any())
                        {
                            //var pi = WellActivityPhaseInfo.Get<WellActivityPhaseInfo>(q);
                            foreach (var y in years)
                            {
                                #region Plan Schedule Pull From BizPlan

                                try
                                {
                                    var Div = 1000000;
                                    Spotfire sp = new Spotfire();
                                    sp.Region = bp.Region;
                                    sp.OperatingUnit = bp.OperatingUnit;
                                    sp.Asset = bp.AssetName;
                                    sp.Project = bp.ProjectName;
                                    sp.WellName = bp.WellName;
                                    sp.ActivityType = bpexist.ActivityType;


                                    sp.FundingType = bpexist.FundingType;
                                    sp.RigName = bp.RigName;
                                    sp.RigType = bp.RigType;

                                    sp.PlanSchedule.Start = bpexist.Estimate.EstimatePeriod.Start; // t.PlanSchedule.Start;
                                    sp.PlanSchedule.Finish = bpexist.Estimate.EstimatePeriod.Finish;
                                    sp.PlanYear = y.Year;
                                    sp.ScheduleID = bp.WellName + " - " + bpexist.ActivityType;
                                    sp.WorkingInterest = bp.ShellShare > 1 ? Tools.Div(bp.ShellShare, 100) : bp.ShellShare;
                                    string isInPlan = bp.isInPlan != null ? bp.isInPlan ? "In Plan " : "Out Of Plan " : "";
                                    sp.PlanningClassification = isInPlan; // +bp.FirmOrOption;

                                    sp.MeanTime = bpexist.Estimate.NewMean.Days;
                                    sp.LoB = bp.LineOfBusiness;

                                    var actCat = DataHelper.Get("WEISActivities", Query.EQ("_id", bpexist.ActivityType)); //ActivityCategory.Get<ActivityCategory>(Query.EQ("Title", x.ActivityCategory));
                                    if(actCat != null)
                                    {
                                        sp.ActivityCategory = BsonHelper.GetString(actCat, "ActivityCategory");

                                    }

                                    sp.ScopeDesc = bpexist.PhaseInfo.ScopeDescription;
                                    sp.BaseOP = bpexist.Estimate.SaveToOP;
                                    sp.SpreadRateUSDDay = Tools.Div(bpexist.Estimate.SpreadRateTotalUSD,1000);
                                    sp.BurnRateUSDDay =Tools.Div(bpexist.Estimate.BurnRateUSD,1000);
                                    sp.MRI = bpexist.Estimate.MechanicalRiskIndex;
                                    sp.CompletionType = bpexist.Estimate.CompletionType;
                                    sp.CompletionZone = Convert.ToInt32( bpexist.Estimate.NumberOfCompletionZones);
                                    sp.BrineDensity = bpexist.Estimate.BrineDensity;

                                    sp.EstRangeType = bpexist.PhaseInfo.EstimatingRangeType;
                                    sp.DeterminLowRange = bpexist.PhaseInfo.DeterministicLowRange;
                                    sp.DeterminHigh = bpexist.PhaseInfo.DeterministicHigh;

                                    sp.ProbP10 = bpexist.PhaseInfo.ProbabilisticP10;
                                    sp.ProbP90 = bpexist.PhaseInfo.ProbabilisticP90;

                                    sp.WaterDepth = bpexist.PhaseInfo.WaterDepthMD;
                                    sp.TotalWaterDepth = bpexist.PhaseInfo.TotalWellDepthMD;

                                    sp.LCFactor = bpexist.PhaseInfo.LearningCurveFactor > 1 ? Tools.Div(bpexist.PhaseInfo.LearningCurveFactor, 100) : bpexist.PhaseInfo.LearningCurveFactor;
                                    sp.MaturityRisk = "Type " + bpexist.PhaseInfo.MaturityLevel;
                                    sp.RFM = bpexist.PhaseInfo.ReferenceFactorModel;

                                    sp.SequenceOnRig = bpexist.PhaseInfo.RigSequenceId;

                                    sp.TroubleFree.Days = bpexist.PhaseInfo.TroubleFree.Days * y.Proportion;
                                    sp.NPT.PercentDays = bpexist.PhaseInfo.NPT.PercentTime > 1 ? Tools.Div(bpexist.PhaseInfo.NPT.PercentTime, 100) : bpexist.PhaseInfo.NPT.PercentTime;
                                    sp.TECOP.PercentDays = bpexist.PhaseInfo.TECOP.PercentTime > 1 ? Tools.Div(bpexist.PhaseInfo.TECOP.PercentTime, 100) : bpexist.PhaseInfo.TECOP.PercentTime;
                                    sp.OverrideFactor.Time = bpexist.PhaseInfo.OverrideFactor.Time;
                                    sp.TimeOverrideFactors = sp.OverrideFactor.Time == true ? "No" : "Yes";

                                    sp.NPT.Days = bpexist.PhaseInfo.NPT.Days * y.Proportion;
                                    sp.TECOP.Days = bpexist.PhaseInfo.TECOP.Days * y.Proportion;


                                    sp.MeanTime = bpexist.PhaseInfo.Mean.Days * y.Proportion;

                                    sp.TroubleFree.Cost = isMillion(bpexist.PhaseInfo.TroubleFree.Cost) * y.Proportion;
                                    sp.Currency =bpexist.Estimate.Currency; // bpexistas.Estimate.Currency;

                                    sp.NPT.PercentCost = bpexist.PhaseInfo.NPT.PercentCost > 1 ? Tools.Div(bpexist.PhaseInfo.NPT.PercentCost, 100) : bpexist.PhaseInfo.NPT.PercentCost;
                                    sp.TECOP.PercentCost = bpexist.PhaseInfo.TECOP.PercentCost > 1 ? Tools.Div(bpexist.PhaseInfo.TECOP.PercentCost, 100) : bpexist.PhaseInfo.TECOP.PercentCost;
                                    sp.CostOverrideFactors = bpexist.PhaseInfo.OverrideFactor.Cost == true ? "No" : "Yes";

                                    sp.NPT.Cost = isMillion(bpexist.PhaseInfo.NPT.Cost) * y.Proportion;
                                    sp.TECOP.Cost = isMillion(bpexist.PhaseInfo.TECOP.Cost) * y.Proportion;
                                    sp.MeanCostEDM = isMillion(bpexist.PhaseInfo.MeanCostEDM.Cost) * y.Proportion;


                                    sp.TroubleFreeUSD = isMillion(bpexist.PhaseInfo.USDCost.TroubleFree) * y.Proportion;
                                    sp.NPTUSD = isMillion(bpexist.PhaseInfo.USDCost.NPT) * y.Proportion;
                                    sp.TECOPUSD = isMillion(bpexist.PhaseInfo.USDCost.TECOP) * y.Proportion;
                                    sp.MeanCostEDMUSD = isMillion(bpexist.PhaseInfo.USDCost.MeanCostEDM) * y.Proportion;
                                    sp.EscCostUSD = isMillion(bpexist.PhaseInfo.USDCost.Escalation) * y.Proportion;
                                    sp.CSOCostUSD = isMillion(bpexist.PhaseInfo.USDCost.CSO) * y.Proportion;
                                    sp.InflationCostUSD = isMillion(bpexist.PhaseInfo.USDCost.Inflation) * y.Proportion;
                                    sp.MeanCostMODUSD = isMillion(bpexist.PhaseInfo.USDCost.MeanCostMOD) * y.Proportion;


                                    sp.ProjectValueDriver = bpexist.Estimate.WellValueDriver;
                                    sp.TQ.Threshold = isMillion(bpexist.Estimate.TQValueDriver);

                                    var WellValueDriver = bpexist.Estimate.WellValueDriver;

                                    if (WellValueDriver == "Total Days")
                                    {
                                        sp.TQ.Gap = bpexist.Estimate.NewMean.Days - bpexist.Estimate.TQValueDriver;
                                    }
                                    else if (WellValueDriver == "Cost")
                                    {
                                        sp.TQ.Gap = isMillion(bpexist.Estimate.NewMean.Cost - (bpexist.Estimate.TQValueDriver * 1000000));
                                    }
                                    else if (WellValueDriver == "Dry Hole Days")
                                    {
                                        sp.TQ.Gap = bpexist.Estimate.DryHoleDays - bpexist.Estimate.TQValueDriver;
                                    }
                                    else
                                    {
                                        sp.TQ.Gap = 0.0;
                                    }

                                    sp.BIC.Threshold = isMillion(bpexist.Estimate.BICValueDriver);

                                    if (WellValueDriver == "Total Days")
                                    {
                                        sp.BIC.Gap = bpexist.Estimate.NewMean.Days - bpexist.Estimate.BICValueDriver;
                                    }
                                    else if (WellValueDriver == "Cost")
                                    {
                                        sp.BIC.Gap = isMillion(bpexist.Estimate.NewMean.Cost - (bpexist.Estimate.BICValueDriver * 1000000));
                                    }
                                    else if (WellValueDriver == "Dry Hole Days")
                                    {
                                        sp.BIC.Gap = bpexist.Estimate.DryHoleDays - bpexist.Estimate.BICValueDriver;
                                    }
                                    else
                                    {
                                        sp.BIC.Gap = 0.0;
                                    }
                                    sp.PerfScore = bpexist.PhaseInfo.PerformanceScore;

                                    sp.MeanCostRTUSD = sp.MeanCostEDMUSD + sp.EscCostUSD;

                                   
                                    sp.SSTroubleFreeUSD = isMillion(sp.TroubleFreeUSD) * sp.WorkingInterest;
                                    sp.SSNPTUSD = isMillion(sp.NPTUSD) * sp.WorkingInterest;
                                    sp.SSTECOPUSD = isMillion(sp.TECOPUSD) * sp.WorkingInterest;
                                    sp.SSMeanCostEDMUSD = isMillion(sp.MeanCostEDMUSD) * sp.WorkingInterest;
                                    sp.SSEscCostUSD = isMillion(sp.EscCostUSD) * sp.WorkingInterest;
                                    sp.SSCSOCostUSD = isMillion(sp.CSOCostUSD) * sp.WorkingInterest;
                                    sp.SSInflationCostUSD = isMillion(sp.InflationCostUSD) * sp.WorkingInterest;
                                    sp.SSMeanCostMODUSD = isMillion(sp.MeanCostMODUSD) * sp.WorkingInterest;
                                    sp.SSMeanCostRTUSD = isMillion(sp.MeanCostRTUSD) * sp.WorkingInterest; 

                                    if (bpexist.BaseOP.Any())
                                    {
                                        if (bpexist.BaseOP.OrderByDescending(x => x.ToString()).FirstOrDefault().Equals("OP14"))
                                        {
                                            sp.OPScope = "Only in OP14";
                                        }
                                        else
                                        {
                                            if (bpexist.BaseOP.OrderByDescending(x => x.ToString()).FirstOrDefault().Equals("OP15"))
                                            {
                                                if (t.IsPostOP == true)
                                                    sp.OPScope = "New in OP15";
                                                else
                                                    sp.OPScope = "Equivalent Scope";
                                            }
                                        }
                                    }

                                    #region not available in WEIS
                                    sp.WellType = ""; // { HPHT, Select, [blank] } ?
                                    sp.DrillingOfCasing = bpexist.PhaseInfo.DrillingOfCasing;

                                    #endregion


                                    spots.Add(sp);
                                }
                                catch (Exception ex)
                                {

                                }

                                #endregion
                            }
                        }

                    }
                    else if (bpexist != null && (bpexist.Estimate.Status != null? bpexist.Estimate.Status.Equals("Complete") || bpexist.Estimate.Status.Equals("Modified") :false))
                    {
                        // ambil dari wellactivities
                        #region Pull from WellPlan
                        var years = DateRangeToMonth.SplitedDateRangeYearly(t.PlanSchedule);
                        if (years.Any())
                        {
                            var pi = WellActivityPhaseInfo.Get<WellActivityPhaseInfo>(q);
                            foreach (var y in years)
                            {
                                try
                                {
                                    Spotfire sp = new Spotfire();
                                    sp.Region = wa.Region;
                                    sp.OperatingUnit = wa.OperatingUnit;
                                    sp.Asset = wa.AssetName;
                                    sp.Project = wa.ProjectName;
                                    sp.WellName = wa.WellName;
                                    sp.ActivityType = t.ActivityType;


                                    sp.FundingType = t.FundingType;
                                    sp.RigName = wa.RigName;
                                    sp.RigType = wa.RigType;

                                    sp.PlanSchedule.Start = t.PlanSchedule.Start;
                                    sp.PlanSchedule.Finish = t.PlanSchedule.Finish;

                                    sp.PlanYear = y.Year;

                                    sp.ScheduleID = wa.WellName + " - " + t.ActivityType;


                                    if (pi != null)
                                    {
                                        var EstimateStatus = false;
                                        if (Status.Contains("Complete") || Status.Contains("Modified"))
                                            EstimateStatus = isHaveBizPlan(wa.WellName, wa.UARigSequenceId, t.ActivityType, Status);
                                        sp.BaseOP = pi.OPType;
                                        if (EstimateStatus)
                                            sp.WorkingInterest = pi.WorkingInterest > 1 ? Tools.Div(pi.WorkingInterest, 100) : pi.WorkingInterest;
                                        else
                                            sp.WorkingInterest = wa.WorkingInterest > 1 ? Tools.Div(wa.WorkingInterest, 100) : wa.WorkingInterest;
                                        string isInPlan = t.IsInPlan != null ? t.IsInPlan ? "In Plan " : "Out Of Plan " : "";
                                        sp.PlanningClassification = isInPlan;// +pi.PlanningClassification;
                                        sp.MeanTime = pi.Mean.Days;
                                        sp.LoB = pi.LineOfBusiness;
                                        sp.ActivityCategory = pi.ActivityCategory;
                                        sp.ScopeDesc = pi.ScopeDescription;
                                        sp.SpreadRateUSDDay = isMillion(pi.SpreadRate);
                                        sp.BurnRateUSDDay = isMillion(pi.BurnRate);
                                        sp.MRI = pi.MRI;
                                        sp.CompletionType = pi.CompletionType;
                                        sp.CompletionZone = pi.CompletionZone;
                                        sp.BrineDensity = pi.BrineDensity;

                                        sp.EstRangeType = pi.EstimatingRangeType;
                                        sp.DeterminLowRange = pi.DeterministicLowRange;
                                        sp.DeterminHigh = pi.DeterministicHigh;

                                        sp.ProbP10 = pi.ProbabilisticP10;
                                        sp.ProbP90 = pi.ProbabilisticP90;

                                        sp.WaterDepth = pi.WaterDepthMD;
                                        sp.TotalWaterDepth = pi.TotalWellDepthMD;

                                        sp.LCFactor = pi.LearningCurveFactor > 1 ? Tools.Div(pi.LearningCurveFactor, 100) : pi.LearningCurveFactor;
                                        sp.MaturityRisk = "Type " + pi.MaturityLevel;
                                        sp.RFM = pi.ReferenceFactorModel;

                                        sp.SequenceOnRig = pi.RigSequenceId;

                                        sp.TroubleFree.Days = pi.TroubleFree.Days * y.Proportion;
                                        sp.NPT.PercentDays = pi.NPT.PercentTime > 1 ? Tools.Div(pi.NPT.PercentTime, 100) : pi.NPT.PercentTime;
                                        sp.TECOP.PercentDays = pi.TECOP.PercentTime > 1 ? Tools.Div(pi.TECOP.PercentTime, 100) : pi.TECOP.PercentTime;
                                        sp.OverrideFactor.Time = pi.OverrideFactor.Time;
                                        sp.TimeOverrideFactors = sp.OverrideFactor.Time == true ? "No" : "Yes";

                                        sp.NPT.Days = pi.NPT.Days * y.Proportion;
                                        sp.TECOP.Days = pi.TECOP.Days * y.Proportion;


                                        sp.MeanTime = pi.Mean.Days * y.Proportion;

                                        sp.TroubleFree.Cost = isMillion(pi.TroubleFree.Cost) * y.Proportion;
                                        sp.Currency = pi.Currency;

                                        sp.NPT.PercentCost = pi.NPT.PercentCost > 1 ? Tools.Div(pi.NPT.PercentCost, 100) : pi.NPT.PercentCost;
                                        sp.TECOP.PercentCost = pi.TECOP.PercentCost > 1 ? Tools.Div(pi.TECOP.PercentCost, 100) : pi.TECOP.PercentCost;
                                        sp.CostOverrideFactors = pi.OverrideFactor.Cost == true ? "No" : "Yes";

                                        sp.NPT.Cost = isMillion(pi.NPT.Cost) * y.Proportion;
                                        sp.TECOP.Cost = isMillion(pi.TECOP.Cost) * y.Proportion;
                                        sp.MeanCostEDM = isMillion(pi.MeanCostEDM.Cost) * y.Proportion;


                                        sp.TroubleFreeUSD = isMillion(pi.USDCost.TroubleFree) * y.Proportion;
                                        sp.NPTUSD = isMillion(pi.USDCost.NPT) * y.Proportion;
                                        sp.TECOPUSD = isMillion(pi.USDCost.TECOP) * y.Proportion;
                                        sp.MeanCostEDMUSD = isMillion(pi.USDCost.MeanCostEDM) * y.Proportion;
                                        sp.EscCostUSD = isMillion(pi.USDCost.Escalation) * y.Proportion;
                                        sp.CSOCostUSD = isMillion(pi.USDCost.CSO) * y.Proportion;
                                        sp.InflationCostUSD = isMillion(pi.USDCost.Inflation) * y.Proportion;
                                        sp.MeanCostMODUSD = isMillion(pi.USDCost.MeanCostMOD) * y.Proportion;


                                        sp.ProjectValueDriver = pi.ProjectValueDriver;

                                        sp.TQ.Threshold = isMillion(pi.TQTreshold);
                                        sp.TQ.Gap = isMillion(pi.TQGap);
                                        sp.BIC.Threshold = isMillion(pi.BICTreshold);
                                        sp.BIC.Gap = isMillion(pi.BICGap);
                                        sp.PerfScore = pi.PerformanceScore;

                                        sp.MeanCostRTUSD = sp.MeanCostEDMUSD + sp.EscCostUSD;

                                        /*
                                           public double SSTroubleFreeUSD { get; set; }
                                            public double SSNPTUSD { get; set; }
                                            public double SSTECOPUSD { get; set; }
                                            public double SSMeanCostEDMUSD { get; set; }
                                            public double SSEscCostUSD { get; set; }
                                            public double SSCSOCostUSD { get; set; }
                                            public double SSInflationCostUSD { get; set; }
                                            public double SSMeanCostMODUSD { get; set; }
                                            public double SSMeanCostRTUSD { get; set; }

                                            public string OPScope { get; set; }
                                         */
                                        sp.SSTroubleFreeUSD = isMillion(sp.TroubleFreeUSD) * sp.WorkingInterest;
                                        sp.SSNPTUSD = isMillion(sp.NPTUSD) * sp.WorkingInterest;
                                        sp.SSTECOPUSD = isMillion(sp.TECOPUSD) * sp.WorkingInterest;
                                        sp.SSMeanCostEDMUSD = isMillion(sp.MeanCostEDMUSD) * sp.WorkingInterest;
                                        sp.SSEscCostUSD = isMillion(sp.EscCostUSD) * sp.WorkingInterest;
                                        sp.SSCSOCostUSD = isMillion(sp.CSOCostUSD) * sp.WorkingInterest;
                                        sp.SSInflationCostUSD = isMillion(sp.InflationCostUSD) * sp.WorkingInterest;
                                        sp.SSMeanCostMODUSD = isMillion(sp.MeanCostMODUSD) * sp.WorkingInterest;
                                        sp.SSMeanCostRTUSD = isMillion(sp.MeanCostRTUSD) * sp.WorkingInterest;

                                        if (t.BaseOP.Any())
                                        {
                                            if (t.BaseOP.OrderByDescending(x => x.ToString()).FirstOrDefault().Equals("OP14"))
                                            {
                                                sp.OPScope = "Only in OP14";
                                            }
                                            else
                                            {
                                                if (t.BaseOP.OrderByDescending(x => x.ToString()).FirstOrDefault().Equals("OP15"))
                                                {
                                                    if (t.IsPostOP == true)
                                                        sp.OPScope = "New in OP15";
                                                    else
                                                        sp.OPScope = "Equivalent Scope";
                                                }
                                            }
                                        }
                                    }

                                    #region not available in WEIS
                                    sp.WellType = ""; // { HPHT, Select, [blank] } ?
                                    sp.DrillingOfCasing = pi.DrillingOfCasing;
                                    //sp.ActivityCount = 
                                    //sp.ScheduleID = 

                                    #endregion


                                    spots.Add(sp);
                                }
                                catch (Exception ex)
                                {

                                }
                            }
                        }
                        #endregion

                    }


                }

            }
            return spots;
        }

        public List<Spotfire> GetSpotOnlyFromBusplan(BizPlanActivity wa, List<string> Status, string BaseOP = "")
        {
            string PreviousOP = "";
            string NextOP = "";
            string DefaultOP = getBaseOP(out PreviousOP, out NextOP);
            List<Spotfire> spots = new List<Spotfire>();
            foreach (var t in wa.Phases)
            {
                var bp = wa;
               
                    #region Query
                    var q = Query.Null;
                    if (BaseOP.Equals(""))
                    {
                        q = Query.And(
                           Query.EQ("WellName", wa.WellName),
                           Query.EQ("WellActivityId", wa._id.ToString()),
                           Query.EQ("ActivityType", t.ActivityType),
                           Query.EQ("RigName", wa.RigName),
                           Query.EQ("SequenceId", wa.UARigSequenceId)
                       );
                    }
                    else
                    {
                        q = Query.And(
                           Query.EQ("WellName", wa.WellName),
                           Query.EQ("WellActivityId", wa._id.ToString()),
                           Query.EQ("ActivityType", t.ActivityType),
                           Query.EQ("RigName", wa.RigName),
                           Query.EQ("SequenceId", wa.UARigSequenceId),
                           Query.EQ("OPType", BaseOP)
                       );
                    }


                    #endregion
                    var maturityLevel = "";
                    if (t.Estimate.MaturityLevel != null)
                        maturityLevel = t.Estimate.MaturityLevel.Substring(t.Estimate.MaturityLevel.Length - 1, 1);
                    else
                        maturityLevel = "-1";
                    double iServices = t.Estimate.Services;
                    double iMaterials = t.Estimate.Materials;
                    var bpexist = t;
                    BizPlanSummary res = new BizPlanSummary(wa.WellName, t.Estimate.RigName, t.ActivityType, t.Estimate.Country,
                        wa.ShellShare, t.Estimate.EstimatePeriod, Convert.ToInt32(maturityLevel),
                        iServices, iMaterials,
                        t.Estimate.TroubleFreeBeforeLC.Days,
                        wa.ReferenceFactorModel,
                        Tools.Div(t.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(t.Estimate.NewTECOPTime.PercentDays, 100),
                        Tools.Div(t.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(t.Estimate.NewTECOPTime.PercentCost, 100),
                        t.Estimate.LongLeadMonthRequired, Tools.Div(t.Estimate.PercOfMaterialsLongLead, 100), baseOP: t.Estimate.SaveToOP, isGetExcRateByCurrency: true,
                        lcf: t.Estimate.NewLearningCurveFactor
                        );
                        res.GeneratePeriodBucket();

                        var years = DateRangeToMonth.SplitedDateRangeYearly(bpexist.Estimate.EstimatePeriod);
                        if (years.Any())
                        {
                            //var pi = WellActivityPhaseInfo.Get<WellActivityPhaseInfo>(q);
                            foreach (var y in years)
                            {
                                #region Plan Schedule Pull From BizPlan

                                try
                                {
                                    var Div = 1000000;
                                    Spotfire sp = new Spotfire();
                                    sp.Region = bp.Region;
                                    sp.OperatingUnit = bp.OperatingUnit;
                                    sp.Asset = bp.AssetName;
                                    sp.Project = bp.ProjectName;
                                    sp.WellName = bp.WellName;
                                    sp.ActivityType = bpexist.ActivityType;
                                    sp.SequenceId = bp.UARigSequenceId;

                                    sp.FundingType = bpexist.FundingType;
                                    sp.RigName = bp.RigName;
                                    sp.RigType = bp.RigType;
                                    
                                    sp.PlanSchedule.Start = Tools.ToUTC( bpexist.Estimate.EstimatePeriod.Start, true); // t.PlanSchedule.Start;
                                    sp.PlanSchedule.Finish = Tools.ToUTC(bpexist.Estimate.EstimatePeriod.Finish, true);
                                    sp.PlanYear = y.Year;
                                    sp.ScheduleID = bp.WellName + " - " + bpexist.ActivityType;
                                    sp.WorkingInterest = bp.ShellShare > 1 ? Tools.Div(bp.ShellShare, 100) : bp.ShellShare;
                                    string isInPlan = bp.isInPlan != null ? bp.isInPlan ? "In Plan " : "Out Of Plan " : "";
                                    sp.PlanningClassification = isInPlan;// +bp.FirmOrOption;

                                    sp.MeanTime = bpexist.Estimate.NewMean.Days;
                                    sp.LoB = bp.LineOfBusiness;

                                    var actCat = DataHelper.Get("WEISActivities", Query.EQ("_id", bpexist.ActivityType)); //ActivityCategory.Get<ActivityCategory>(Query.EQ("Title", x.ActivityCategory));
                                    if (actCat != null)
                                    {
                                        sp.ActivityCategory = BsonHelper.GetString(actCat, "ActivityCategory");

                                    }

                                    sp.ScopeDesc = bpexist.PhaseInfo.ScopeDescription;
                                    sp.BaseOP = bpexist.Estimate.SaveToOP;
                                    sp.SpreadRateUSDDay = Tools.Div(bpexist.Estimate.SpreadRateTotalUSD, 1000);
                                    sp.BurnRateUSDDay = Tools.Div(bpexist.Estimate.BurnRateUSD, 1000);
                                    sp.MRI = bpexist.Estimate.MechanicalRiskIndex;
                                    sp.CompletionType = bpexist.Estimate.CompletionType;
                                    sp.CompletionZone = Convert.ToInt32(bpexist.Estimate.NumberOfCompletionZones);
                                    sp.BrineDensity = bpexist.Estimate.BrineDensity;

                                    sp.EstRangeType = bpexist.PhaseInfo.EstimatingRangeType;
                                    sp.DeterminLowRange = bpexist.PhaseInfo.DeterministicLowRange;
                                    sp.DeterminHigh = bpexist.PhaseInfo.DeterministicHigh;

                                    sp.ProbP10 = bpexist.PhaseInfo.ProbabilisticP10;
                                    sp.ProbP90 = bpexist.PhaseInfo.ProbabilisticP90;

                                    sp.WaterDepth = bpexist.Estimate.WaterDepth;
                                    sp.TotalWaterDepth = bpexist.Estimate.WellDepth;

                                    sp.LCFactor = bpexist.Estimate.NewLearningCurveFactor > 1 ? Tools.Div(bpexist.Estimate.NewLearningCurveFactor, 100) : bpexist.Estimate.NewLearningCurveFactor;
                                    sp.MaturityRisk = bpexist.Estimate.MaturityLevel;
                                    sp.RFM = bp.ReferenceFactorModel;

                                    sp.SequenceOnRig = bpexist.PhaseInfo.RigSequenceId;

                                    sp.TroubleFree.Days = bpexist.Estimate.TroubleFreeBeforeLC.Days * y.Proportion;
                                    sp.NPT.PercentDays = bpexist.Estimate.NewNPTTime.PercentDays > 1 ? Tools.Div(bpexist.Estimate.NewNPTTime.PercentDays, 100) : bpexist.Estimate.NewNPTTime.PercentDays;
                                    sp.TECOP.PercentDays = bpexist.Estimate.NewTECOPTime.PercentDays > 1 ? Tools.Div(bpexist.Estimate.NewTECOPTime.PercentDays, 100) : bpexist.Estimate.NewTECOPTime.PercentDays;
                                    sp.OverrideFactor.Time = bpexist.PhaseInfo.OverrideFactor.Time;
                                    sp.TimeOverrideFactors = sp.OverrideFactor.Time == true ? "No" : "Yes";

                                    sp.NPT.Days = res.NPT.Days * y.Proportion;
                                    sp.TECOP.Days = res.TECOP.Days * y.Proportion;


                                    sp.MeanTime = res.MeanTime * y.Proportion;

                                    sp.TroubleFree.Cost = Tools.Div(res.TroubleFree.Cost,1000000) * y.Proportion;
                                    sp.Currency = bpexist.Estimate.Currency;

                                    //sp.NPT.PercentCost = bpexist.PhaseInfo.NPT.PercentCost > 1 ? Tools.Div(bpexist.PhaseInfo.NPT.PercentCost, 100) : bpexist.PhaseInfo.NPT.PercentCost;
                                    //sp.TECOP.PercentCost = bpexist.PhaseInfo.TECOP.PercentCost > 1 ? Tools.Div(bpexist.PhaseInfo.TECOP.PercentCost, 100) : bpexist.PhaseInfo.TECOP.PercentCost;

                                    sp.NPT.PercentCost = bpexist.Estimate.NewNPTTime.PercentCost > 1 ? Tools.Div(bpexist.Estimate.NewNPTTime.PercentCost, 100) : bpexist.Estimate.NewNPTTime.PercentCost;
                                    sp.TECOP.PercentCost = bpexist.Estimate.NewTECOPTime.PercentCost > 1 ? Tools.Div(bpexist.Estimate.NewTECOPTime.PercentCost, 100) : bpexist.Estimate.NewTECOPTime.PercentCost;
                                  
                                    sp.CostOverrideFactors = bpexist.PhaseInfo.OverrideFactor.Cost == true ? "No" : "Yes";

                                    sp.NPT.Cost = Tools.Div(res.NPT.Cost, 1000000) * y.Proportion;
                                    sp.TECOP.Cost = Tools.Div(res.TECOP.Cost,1000000) * y.Proportion;
                                    sp.MeanCostEDM = Tools.Div(res.MeanCostEDM,1000000) * y.Proportion;


                                    sp.TroubleFreeUSD = Tools.Div(res.TroubleFreeCostUSD,1000000) * y.Proportion;
                                    sp.NPTUSD = Tools.Div(res.NPTCostUSD,1000000) * y.Proportion;
                                    sp.TECOPUSD = Tools.Div(res.TECOPCostUSD, 1000000) * y.Proportion;
                                    sp.MeanCostEDMUSD = Tools.Div(res.MeanCostEDMUSD, 1000000) * y.Proportion;
                                    sp.EscCostUSD = Tools.Div(bpexist.Allocation.AnnualyBuckets.Sum(x => x.EscCostTotal), 1000000) * y.Proportion;
                                    sp.CSOCostUSD = Tools.Div(bpexist.Allocation.AnnualyBuckets.Sum(x => x.CSOCost), 1000000) * y.Proportion;
                                    sp.InflationCostUSD = Tools.Div(bpexist.Allocation.AnnualyBuckets.Sum(x => x.InflationCost), 1000000) * y.Proportion;
                                    sp.MeanCostMODUSD = Tools.Div(res.MeanCostMOD,1000000) * y.Proportion;

                                    sp.ProjectValueDriver = bpexist.Estimate.WellValueDriver;
                                    sp.TQ.Threshold = Tools.Div(bpexist.Estimate.TQValueDriver,1000000);

                                    var WellValueDriver = bpexist.Estimate.WellValueDriver;

                                    if (WellValueDriver == "Total Days")
                                    {
                                        sp.TQ.Gap = bpexist.Estimate.NewMean.Days - bpexist.Estimate.TQValueDriver;
                                    }
                                    else if (WellValueDriver == "Cost")
                                    {
                                        sp.TQ.Gap = Tools.Div(bpexist.Estimate.NewMean.Cost - (bpexist.Estimate.TQValueDriver * 1000000),1000000);
                                    }
                                    else if (WellValueDriver == "Dry Hole Days")
                                    {
                                        sp.TQ.Gap = bpexist.Estimate.DryHoleDays - bpexist.Estimate.TQValueDriver;
                                    }
                                    else
                                    {
                                        sp.TQ.Gap = 0.0;
                                    }

                                    sp.BIC.Threshold = Tools.Div(bpexist.Estimate.BICValueDriver,1000000);

                                    if (WellValueDriver == "Total Days")
                                    {
                                        sp.BIC.Gap = bpexist.Estimate.NewMean.Days - bpexist.Estimate.BICValueDriver;
                                    }
                                    else if (WellValueDriver == "Cost")
                                    {
                                        sp.BIC.Gap = Tools.Div(bpexist.Estimate.NewMean.Cost - (bpexist.Estimate.BICValueDriver * 1000000),1000000);
                                    }
                                    else if (WellValueDriver == "Dry Hole Days")
                                    {
                                        sp.BIC.Gap = bpexist.Estimate.DryHoleDays - bpexist.Estimate.BICValueDriver;
                                    }
                                    else
                                    {
                                        sp.BIC.Gap = 0.0;
                                    }

                                    sp.PerfScore = bpexist.PhaseInfo.PerformanceScore;
                                    sp.MeanCostRTUSD = Tools.Div((bpexist.Allocation.AnnualyBuckets.Sum(x => x.MeanCostRealTerm) * y.Proportion),1000000); // sp.MeanCostEDMUSD + sp.EscCostUSD;


                                    sp.SSTroubleFreeUSD = (sp.TroubleFreeUSD) * sp.WorkingInterest;
                                    sp.SSNPTUSD = (sp.NPTUSD) * sp.WorkingInterest;
                                    sp.SSTECOPUSD = (sp.TECOPUSD) * sp.WorkingInterest;
                                    sp.SSMeanCostEDMUSD = (sp.MeanCostEDMUSD) * sp.WorkingInterest;
                                    sp.SSEscCostUSD = (sp.EscCostUSD) * sp.WorkingInterest;
                                    sp.SSCSOCostUSD = (sp.CSOCostUSD) * sp.WorkingInterest;
                                    sp.SSInflationCostUSD = (sp.InflationCostUSD) * sp.WorkingInterest;
                                    sp.SSMeanCostMODUSD = (sp.MeanCostMODUSD) * sp.WorkingInterest;
                                    sp.SSMeanCostRTUSD = (sp.MeanCostRTUSD) * sp.WorkingInterest;

                                    if (bpexist.BaseOP.Any())
                                    {
                                        //if (bpexist.BaseOP.OrderByDescending(x => x.ToString()).FirstOrDefault().Equals("OP14"))
                                        //{
                                        //    sp.OPScope = "Only in OP14";
                                        //}
                                        //else
                                        //{
                                            if (bpexist.BaseOP.Contains("OP15"))
                                                sp.OPScope = "Equivalent Scope";
                                            else
                                                sp.OPScope = "Only in OP16";
                                        //}
                                    }
                                    else
                                    {
                                        sp.OPScope = "Only in OP16";
                                    }

                                    #region not available in WEIS
                                    sp.WellType = ""; // { HPHT, Select, [blank] } ?
                                    sp.DrillingOfCasing = bpexist.PhaseInfo.DrillingOfCasing;

                                    #endregion


                                    spots.Add(sp);
                                }
                                catch (Exception ex)
                                {

                                }

                                #endregion
                            }
                        }

            }
            return spots;
        }

        public double isMillion(double value)
        {
            if (value >= 0)
            {
                if (value >= 100000)
                {
                    value = Tools.Div(value, 1000000);
                }
            }
            else
            {
                if (value * (-1) >= 100000)
                {
                    value = Tools.Div(value, 1000000);
                }
            }

            return value;
        }

        public JsonResult GetWellSequenceInfo2(WaterfallBase wb, List<string> Status)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                string previousOP = "";
                string nextOP = "";
                var DefaultOP = getBaseOP(out previousOP, out nextOP);
                if (Status == null)
                {
                    Status = new List<string>();
                }
                var GetMasterOPs = MasterOP.Populate<MasterOP>();

                var wa = wb.GetActivitiesAndPIPsSpotFire(false);

                var raw = new List<WellActivity>();
                if (wa.Activities.Any()) raw.AddRange(wa.Activities);

                List<Spotfire> spots = new List<Spotfire>();
                foreach (var t in raw)
                {
                    var sps = GetSpot(t, Status, (wb.OPs != null && wb.OPs.Count() > 0) ? wb.OPs.LastOrDefault() : "");
                    if (sps.Any())
                        spots.AddRange(sps);
                }


                List<BsonDocument> docs = new List<BsonDocument>();
                foreach (var t in spots)
                {
                    docs.Add(t.ToBsonDocument());
                }

                return DataHelper.ToDictionaryArray(docs);
            });
        }
       
        public JsonResult GetDataSpotFire(WaterfallBase wb, List<string> Status)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var lsInfo = new PIPAnalysisController().GetLatestLsDate();
                string PreviousOP = "";
                string NextOP = "";
                string DefaultOP = getBaseOP(out PreviousOP, out NextOP);
                if (Status == null)
                {
                    Status = new List<string>();
                }
                var division = 1000000.0;
                var now = Tools.ToUTC(DateTime.Now);
                var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
                var email = WebTools.LoginUser.Email;
                wb.PeriodBase = "By Last Sequence";
                var raw = wb.GetActivitiesForBizPlan(email);
                var accessibleWells = WebTools.GetWellActivities();

                var final2 = new List<object>();

                List<Spotfire> spots = new List<Spotfire>();
                foreach (var t in raw)
                {
                    var sps = GetSpotOnlyFromBusplan(t, Status, (wb.OPs != null && wb.OPs.Count() > 0) ? wb.OPs.LastOrDefault() : "");
                    if (sps.Any())
                        spots.AddRange(sps);
                }


                List<BsonDocument> docs = new List<BsonDocument>();
                foreach (var t in spots)
                {
                    var tx = t.ToBsonDocument();

                    tx.Set("Start", t.PlanSchedule.Start.ToString("yyyy/MM/dd HH:mm:ss"));
                    tx.Set("Finish", t.PlanSchedule.Finish.ToString("yyyy/MM/dd HH:mm:ss"));

                    docs.Add(tx);
                }

                int totalGroup = 0;
                if (spots.Any())
                {
                    totalGroup = spots.GroupBy(x => new { x.SequenceId, x.ActivityType }).ToList().Count;
                }



                return new{docs = DataHelper.ToDictionaryArray(docs),TotalGroup=totalGroup};
            });
        }


        public void SetOpListDate(List<OPListHelperForDataBrowserGrid> datas, out List<OPListHelperForDataBrowserGrid> result)
        {
            var get = new List<OPListHelperForDataBrowserGrid>();
            var StartDate = datas.Where(x => x.OPSchedule.Start != Tools.DefaultDate);
            DateTime MinStartDate = Tools.DefaultDate;
            if (StartDate.Any())
            {
                MinStartDate = StartDate.Min(x => x.OPSchedule.Start);
            }
            var FinishDate = datas.Where(x => x.OPSchedule.Finish != Tools.DefaultDate);
            DateTime MinFinishDate = Tools.DefaultDate;
            if (FinishDate.Any())
            {
                MinFinishDate = FinishDate.Min(x => x.OPSchedule.Finish);
            }
            foreach (var helper in datas)
            {
                if (MinStartDate != Tools.DefaultDate)
                {
                    if (helper.OPSchedule.Start == Tools.DefaultDate)
                    {
                        helper.OPSchedule.Start = MinStartDate;
                    }
                }
                if (MinFinishDate != Tools.DefaultDate)
                {
                    if (helper.OPSchedule.Finish == Tools.DefaultDate)
                    {
                        helper.OPSchedule.Finish = MinFinishDate;
                    }
                }
                get.Add(helper);

            }
            result = get;
        }


    }
}