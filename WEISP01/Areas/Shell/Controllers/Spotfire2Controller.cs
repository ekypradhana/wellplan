using System.IO;
using Aspose.Cells;
using Aspose.Cells.Pivot;
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
    public class Spotfire2Controller : Controller
    {
        //
        // GET: /Shell/SpotFire2/
        public ActionResult Index()
        {
            return View();
        }

        public FileResult Download(string file, string fileReturn)
        {
            var res = Path.Combine(Server.MapPath("~/App_Data/Temp"), file);
            return File(res, Tools.GetContentType(".xlsx"), fileReturn);
        }

        public List<Spotfire> GetSpot(List<BizPlanActivity> wa, out Int32 TotalUniqueActivities, string BaseOP = "")
        {
            List<Spotfire> spots = new List<Spotfire>();
            TotalUniqueActivities = 0;
            var actCats = DataHelper.Populate("WEISActivities");
            var macroEconomics = MacroEconomic.Populate<MacroEconomic>();
            foreach (var bp in wa)
            {
                foreach (var bpexist in bp.Phases)
                {
                    try
                    {
                        List<BizPlanAllocation> alocs = new List<BizPlanAllocation>();

                        if (WellActivity.isHaveWeeklyReport(bp.WellName, bp.UARigSequenceId, bpexist.ActivityType))
                        {
                            break;
                        }


                        #region recalc

                        bpexist.Estimate.EstimatePeriod.Start = Tools.ToUTC(bpexist.Estimate.EstimatePeriod.Start, true);
                        bpexist.Estimate.EstimatePeriod.Finish = Tools.ToUTC(bpexist.Estimate.EstimatePeriod.Finish);
                        bpexist.Estimate.EventStartDate = Tools.ToUTC(bpexist.Estimate.EventStartDate, true);
                        bpexist.Estimate.EventEndDate = Tools.ToUTC(bpexist.Estimate.EventEndDate);

                        string level = "Type 0";
                        var maturityLevel = bpexist.Estimate.MaturityLevel == null ? level.Substring(level.Length - 1, 1) : bpexist.Estimate.MaturityLevel.Substring(bpexist.Estimate.MaturityLevel.Length - 1, 1);

                        double iServices = bpexist.Estimate.Services;
                        double iMaterials = bpexist.Estimate.Materials;

                        if (bpexist.Estimate.Currency == null)
                        {
                            bpexist.Estimate.Currency = "USD";
                        }
                        if (!bpexist.Estimate.Currency.Trim().ToUpper().Equals("USD"))
                        {
                            var datas = macroEconomics.Where(x => x.Currency == bpexist.Estimate.Currency && x.BaseOP == bpexist.Estimate.SaveToOP);
                            if (datas.Any())
                            {
                                var cx = datas.Where(x => x.Currency.Equals(bpexist.Estimate.Currency)).FirstOrDefault();

                                var rate = cx.ExchangeRate.AnnualValues.Where(x => x.Year == bpexist.Estimate.EstimatePeriod.Start.Year).Any() ?
                                        cx.ExchangeRate.AnnualValues.Where(x => x.Year == bpexist.Estimate.EstimatePeriod.Start.Year).FirstOrDefault().Value
                                        : 0;

                                // dibagi rate : untuk jadi USD
                                iServices = Tools.Div(iServices, rate);
                                iMaterials = Tools.Div(iMaterials, rate);
                            }
                            else
                            {
                                iServices = Tools.Div(iServices, 1);
                                iMaterials = Tools.Div(iMaterials, 1);
                            }
                        }


                        BizPlanSummary bisCal = new BizPlanSummary(bp.WellName, bpexist.Estimate.RigName, bpexist.ActivityType, bp.Country==null? "" :bp.Country,
                            bp.ShellShare, bpexist.Estimate.EstimatePeriod, bpexist.Estimate.MaturityLevel==null? 0 :Convert.ToInt32(bpexist.Estimate.MaturityLevel.Replace("Type", "")),
                            iServices, iMaterials,
                            bpexist.Estimate.TroubleFreeBeforeLC.Days,
                            bp.ReferenceFactorModel == null ? "" : bp.ReferenceFactorModel,
                            Tools.Div(bpexist.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(bpexist.Estimate.NewTECOPTime.PercentDays, 100),
                            Tools.Div(bpexist.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(bpexist.Estimate.NewTECOPTime.PercentCost, 100),
                            bpexist.Estimate.LongLeadMonthRequired, Tools.Div(bpexist.Estimate.PercOfMaterialsLongLead, 100), baseOP: bpexist.Estimate.SaveToOP == null ? "" : bpexist.Estimate.SaveToOP,
                            isGetExcRateByCurrency: true,
                            lcf: bpexist.Estimate.NewLearningCurveFactor
                            );

                        bisCal.GeneratePeriodBucket();
                        bisCal.GenerateAnnualyBucket(bisCal.MonthlyBuckets);
                        #endregion

                        TotalUniqueActivities++;
                        var aloc = alocs;// bpexist.Allocation;

                        var prop = DateRangeToMonth.SplitedDateRangeYearly(new DateRange(bpexist.Estimate.EventStartDate, bpexist.Estimate.EventEndDate));

                        foreach (var y in bpexist.Allocation.AnnualyBuckets)
                        {
                            #region aloc loop

                            var proportion = prop.Where(x => x.Year == Convert.ToInt32(y.Key)).FirstOrDefault();

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

                            sp.PlanSchedule.Start = Tools.ToUTC(bpexist.Estimate.EstimatePeriod.Start, true); // t.PlanSchedule.Start;
                            sp.PlanSchedule.Finish = Tools.ToUTC(bpexist.Estimate.EstimatePeriod.Finish, true);

                            sp.Start = Tools.ToUTC(bpexist.Estimate.EventStartDate, true).ToString("yyyy/MM/dd HH:mm:ss"); // t.PlanSchedule.Start;
                            sp.Finish = Tools.ToUTC(bpexist.Estimate.EventEndDate, true).ToString("yyyy/MM/dd HH:mm:ss"); // t.PlanSchedule.Start;

                           

                            sp.PlanYear = Convert.ToInt32(y.Key);
                            sp.ScheduleID = bp.WellName + " - " + bpexist.ActivityType;
                            sp.WorkingInterest = bp.ShellShare > 1 ? Tools.Div(bp.ShellShare, 100) : bp.ShellShare;
                            string isInPlan = bp.isInPlan != null ? bp.isInPlan ? "In Plan " : "Out Of Plan " : "";
                            sp.PlanningClassification = isInPlan;// +bp.FirmOrOption;

                            sp.MeanTime = bpexist.Estimate.NewMean.Days;
                            sp.LoB = bp.LineOfBusiness;
                            var actCat = actCats.FirstOrDefault(x => x.GetString("_id") == bpexist.ActivityType);
                            //var actCat = DataHelper.Get("WEISActivities", Query.EQ("_id", bpexist.ActivityType)); //ActivityCategory.Get<ActivityCategory>(Query.EQ("Title", x.ActivityCategory));
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
                            sp.RFM = bp.ReferenceFactorModel.ToLower() == "default" ? bpexist.PhaseInfo.ReferenceFactorModel : bp.ReferenceFactorModel;

                            sp.SequenceOnRig = bpexist.PhaseInfo.RigSequenceId;

                            sp.TroubleFree.Days = bpexist.Estimate.TroubleFreeBeforeLC.Days * (proportion != null ?  proportion.Proportion : 0);
                            sp.NPT.PercentDays = bpexist.Estimate.NewNPTTime.PercentDays > 1 ? Tools.Div(bpexist.Estimate.NewNPTTime.PercentDays, 100) : bpexist.Estimate.NewNPTTime.PercentDays;
                            sp.TECOP.PercentDays = bpexist.Estimate.NewTECOPTime.PercentDays > 1 ? Tools.Div(bpexist.Estimate.NewTECOPTime.PercentDays, 100) : bpexist.Estimate.NewTECOPTime.PercentDays;
                            sp.OverrideFactor.Time = bpexist.PhaseInfo.OverrideFactor.Time;
                            sp.TimeOverrideFactors = sp.OverrideFactor.Time == true ? "No" : "Yes";

                            var tcpx = bisCal.TECOP.Days;
                            var nptx = bisCal.NPT.Days;

                            //sp.NPT.Days = bpexist.Estimate.NewNPTTime.Days * (proportion != null ? proportion.Proportion : 0);
                            sp.NPT.Days = nptx * (proportion != null ? proportion.Proportion : 0);
                            //sp.TECOP.Days = bpexist.Estimate.NewTECOPTime.Days * (proportion != null ? Math.Round(proportion.Proportion, 2) : 0);
                            //sp.TECOP.Days = Math.Round(tcpx, 2) * (proportion != null ? Math.Round(proportion.Proportion, 2) : 0);
                            var propTecop = proportion != null ? proportion.Proportion : 0;
                            sp.TECOP.Days = Math.Round(tcpx * propTecop, 2);

                            sp.MeanTime = bpexist.Estimate.NewMean.Days* (proportion != null ?  proportion.Proportion : 0);

                            if (bpexist.Estimate.Currency.Equals("USD"))
                            {
                                sp.TroubleFree.Cost = Tools.Div(y.TroubleFreeCost, 1000000); ; // ools.Div(bpexist.Estimate.NewTroubleFree.Cost, 1000000) * (proportion != null ? proportion.Proportion : 0);
                                sp.NPT.Cost = Tools.Div(y.NPTCost, 1000000); //Tools.Div(bpexist.Estimate.NewNPTTime.Cost, 1000000) * (proportion != null ? proportion.Proportion : 0);
                                sp.TECOP.Cost = Tools.Div(y.TECOPCost, 1000000); //Tools.Div(bpexist.Estimate.NewTECOPTime.Cost, 1000000) * (proportion != null ? proportion.Proportion : 0);
                                sp.MeanCostEDM = Tools.Div(y.MeanCostEDM, 1000000); ////Tools.Div(bpexist.Estimate.NewMean.Cost, 1000000) * (proportion != null ? proportion.Proportion : 0);
                            }
                            else
                            {
                                var rate = BizPlanAllocation.GetExchangeRateByCurrency(bpexist.Estimate.Currency, Convert.ToInt32(y.Key), bpexist.Estimate.SaveToOP);

                                sp.TroubleFree.Cost = Tools.Div(y.TroubleFreeCost, 1000000) * rate;  // ools.Div(bpexist.Estimate.NewTroubleFree.Cost, 1000000) * (proportion != null ? proportion.Proportion : 0);
                                sp.NPT.Cost = Tools.Div(y.NPTCost, 1000000) * rate; //Tools.Div(bpexist.Estimate.NewNPTTime.Cost, 1000000) * (proportion != null ? proportion.Proportion : 0);
                                sp.TECOP.Cost = Tools.Div(y.TECOPCost, 1000000) * rate; //Tools.Div(bpexist.Estimate.NewTECOPTime.Cost, 1000000) * (proportion != null ? proportion.Proportion : 0);
                                sp.MeanCostEDM = Tools.Div(y.MeanCostEDM, 1000000) * rate; 
                            }

                            sp.Currency = bpexist.Estimate.Currency;

                            //sp.NPT.PercentCost = bpexist.PhaseInfo.NPT.PercentCost > 1 ? Tools.Div(bpexist.PhaseInfo.NPT.PercentCost, 100) : bpexist.PhaseInfo.NPT.PercentCost;
                            //sp.TECOP.PercentCost = bpexist.PhaseInfo.TECOP.PercentCost > 1 ? Tools.Div(bpexist.PhaseInfo.TECOP.PercentCost, 100) : bpexist.PhaseInfo.TECOP.PercentCost;

                            sp.NPT.PercentCost = bpexist.Estimate.NewNPTTime.PercentCost > 1 ? Tools.Div(bpexist.Estimate.NewNPTTime.PercentCost, 100) : bpexist.Estimate.NewNPTTime.PercentCost;
                            sp.TECOP.PercentCost = bpexist.Estimate.NewTECOPTime.PercentCost > 1 ? Tools.Div(bpexist.Estimate.NewTECOPTime.PercentCost, 100) : bpexist.Estimate.NewTECOPTime.PercentCost;

                            sp.CostOverrideFactors = bpexist.PhaseInfo.OverrideFactor.Cost == true ? "No" : "Yes";

                            


                            sp.TroubleFreeUSD = Tools.Div(y.TroubleFreeCost, 1000000);
                            sp.NPTUSD = Tools.Div(y.NPTCost, 1000000);
                            sp.TECOPUSD = Tools.Div(y.TECOPCost, 1000000);
                            sp.MeanCostEDMUSD = Tools.Div(y.MeanCostEDM, 1000000);
                            sp.EscCostUSD = Tools.Div(y.EscCostTotal, 1000000);
                            sp.CSOCostUSD = Tools.Div(y.CSOCost, 1000000);
                            sp.InflationCostUSD = Tools.Div(y.InflationCost, 1000000);
                            sp.MeanCostMODUSD = Tools.Div(y.MeanCostMOD, 1000000);

                            sp.ProjectValueDriver = bpexist.Estimate.WellValueDriver;
                            //sp.TQ.Threshold = Tools.Div(bpexist.Estimate.TQValueDriver, 1000000);

                            #region additional
                            var WellValueDriver = bpexist.Estimate.WellValueDriver;

                            if (WellValueDriver == "Total Days")
                            {
                                sp.TQ.Gap = bpexist.Estimate.NewMean.Days - bpexist.Estimate.TQValueDriver;
                                sp.TQ.Threshold = bpexist.Estimate.TQValueDriver;
                            }
                            else if (WellValueDriver == "Cost")
                            {
                                sp.TQ.Gap = Tools.Div(bpexist.Estimate.NewMean.Cost - (bpexist.Estimate.TQValueDriver * 1000000), 1000000);
                                sp.TQ.Threshold = Tools.Div(bpexist.Estimate.TQValueDriver, 1000000);
                            }
                            else if (WellValueDriver == "Dry Hole Days")
                            {
                                sp.TQ.Gap = bpexist.Estimate.DryHoleDays - bpexist.Estimate.TQValueDriver;
                                sp.TQ.Threshold = bpexist.Estimate.TQValueDriver;
                            }
                            else
                            {
                                sp.TQ.Gap = 0.0;
                                sp.TQ.Threshold = 0.0;
                            }

                            //sp.BIC.Threshold = Tools.Div(bpexist.Estimate.BICValueDriver, 1000000);

                            if (WellValueDriver == "Total Days")
                            {
                                sp.BIC.Gap = bpexist.Estimate.NewMean.Days - bpexist.Estimate.BICValueDriver;
                                sp.BIC.Threshold = bpexist.Estimate.BICValueDriver;
                            }
                            else if (WellValueDriver == "Cost")
                            {
                                sp.BIC.Gap = Tools.Div(bpexist.Estimate.NewMean.Cost - (bpexist.Estimate.BICValueDriver * 1000000), 1000000);
                                sp.BIC.Threshold = Tools.Div(bpexist.Estimate.BICValueDriver, 1000000);
                            }
                            else if (WellValueDriver == "Dry Hole Days")
                            {
                                sp.BIC.Gap = bpexist.Estimate.DryHoleDays - bpexist.Estimate.BICValueDriver;
                                sp.BIC.Threshold = bpexist.Estimate.BICValueDriver;
                            }
                            else
                            {
                                sp.BIC.Gap = 0.0;
                                sp.BIC.Threshold = 0.0;
                            }

                            sp.PerfScore = bpexist.PhaseInfo.PerformanceScore;

                            #endregion

                            sp.MeanCostRTUSD = Tools.Div(y.MeanCostRealTerm, 1000000); // Tools.Div((bpexist.Allocation.AnnualyBuckets.Sum(x => x.MeanCostRealTerm) * y.Proportion), 1000000); // sp.MeanCostEDMUSD + sp.EscCostUSD;

                            var percentSS = bp.WorkingInterest > 1 ? Tools.Div(bp.WorkingInterest, 100) : bp.WorkingInterest;

                            sp.SSTroubleFreeUSD = sp.TroubleFreeUSD * percentSS;
                            sp.SSNPTUSD = Tools.Div(y.NPTCost * percentSS, 1000000);
                            sp.SSTECOPUSD = Tools.Div(y.TECOPCost * percentSS, 1000000);
                            sp.SSMeanCostEDMUSD = Tools.Div(y.MeanCostEDMSS, 1000000);
                            sp.SSEscCostUSD = Tools.Div(y.EscCostTotal * percentSS, 1000000);
                            sp.SSCSOCostUSD = Tools.Div(y.CSOCost * percentSS, 1000000);
                            sp.SSInflationCostUSD = Tools.Div(y.InflationCost * percentSS, 1000000);
                            sp.SSMeanCostMODUSD = Tools.Div(y.MeanCostMODSS, 1000000);
                            sp.SSMeanCostRTUSD = Tools.Div(y.MeanCostRealTermSS, 1000000);

                            if (bpexist.BaseOP.Any())
                            {
                                if (bpexist.BaseOP.Contains("OP15"))
                                    sp.OPScope = "Equivalent Scope";
                                else
                                    sp.OPScope = "Only in OP16";
                            }
                            else
                            {
                                sp.OPScope = "Only in OP16";
                            }

                            #endregion

                            spots.Add(sp);
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                    



                }

            }
            return spots;
        }

        public JsonResult GetDataFiscalYear2(WaterfallBase wb, string ViewBy, int yearStart = 0, int yearFinish = 0, List<string> Status = null)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {

                try
                {


                    var email = WebTools.LoginUser.Email;
                    var getraw = wb.GetActivitiesForBizPlanForFiscalYear(email);
                    List<BizPlanAllocation> allocations = new List<BizPlanAllocation>();
                    //if (wb.OPs != null && wb.OPs.Count() > 0)
                    //{
                    //    var qbaseOP = Query.In("SaveToOP", new BsonArray(wb.OPs));
                    //    allocations = BizPlanAllocation.Populate<BizPlanAllocation>(qbaseOP);
                    //}
                    //else
                    //{
                    //    allocations = BizPlanAllocation.Populate<BizPlanAllocation>();
                    //}

                    var periodBase = "annualy";

                    var periodYears = new List<Int32>();
                    var periodMonths = new List<string>();

                    List<Spotfire> sps = new List<Spotfire>();
                    var totalUniqueActivities = 0;
                    var t =  GetSpot(getraw, out totalUniqueActivities);
                    var totalGroup = 0;
                    if (t.Any())
                    {
                        totalGroup = t.GroupBy(x => new { x.SequenceId }).ToList().Count;
                    }
                    return new { Data = t, TotalGroup = totalGroup, totalUniqueActivities };
                    //var detail = BizPlanAllocation.TransformDetailBizAllocationSpotfire(getraw, periodBase, Status);

                }
                catch (Exception e)
                {
                    return new
                    {
                        Message = e.Message
                    };
                }


            });
        }

        public JsonResult DownloadData(WaterfallBase wb, string ViewBy, int yearStart = 0, int yearFinish = 0, List<string> Status = null)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var email = WebTools.LoginUser.Email;
                var getraw = wb.GetActivitiesForBizPlanForFiscalYear(email);
                List<BizPlanAllocation> allocations = new List<BizPlanAllocation>();
                if (wb.OPs != null && wb.OPs.Count() > 0)
                {
                    var qbaseOP = Query.In("SaveToOP", new BsonArray(wb.OPs));
                    allocations = BizPlanAllocation.Populate<BizPlanAllocation>(qbaseOP);
                }
                else
                {
                    allocations = BizPlanAllocation.Populate<BizPlanAllocation>();
                }

                var periodBase = "annualy";

                var periodYears = new List<Int32>();
                var periodMonths = new List<string>();


                var RFMChecking = new List<object>();
                foreach (var rw in getraw)
                {
                    if (String.IsNullOrEmpty(rw.ReferenceFactorModel) || rw.ReferenceFactorModel.ToLower() == "default")
                    {
                        foreach (var phase in rw.Phases)
                        {
                            var ti = new
                            {
                                WellName = rw.WellName,
                                UARigSequenceId = rw.UARigSequenceId,
                                ActivityType = phase.ActivityType,
                                rw.RigName,
                                Project = rw.ProjectName,
                                rw.Country,
                                ReferenceFactorModel = rw.ReferenceFactorModel
                            };
                            RFMChecking.Add(ti);
                        }
                    }
                }


                List<Spotfire> sps = new List<Spotfire>();
                var totalUniqueActivities = 0;
                var t = GetSpot(getraw,out totalUniqueActivities);
                var totalGroup = 0;
                if (t.Any())
                {
                    totalGroup = t.GroupBy(x => new { x.SequenceId, x.ActivityType }).ToList().Count;
                }
                var Datas = t;

                #region Fill the data

                var res = Path.Combine(Server.MapPath("~/App_Data/Temp"), "SpotfireGridPivotTemplate.xlsx");
                Workbook worbbook = new Workbook(res);
                var sheet1 = worbbook.Worksheets[0];
                var sheet2 = worbbook.Worksheets[1];
                sheet1.Name = "Data";
                Cells cells = sheet1.Cells;

                var walkingIdx = 2; var Index = 1;
                foreach (var data in Datas.OrderBy(x => x.PlanYear))
                {
                    //cell = cells["A" + walkingIdx];
                    //cell.PutValue(data.BaseOP);
                    Cell cell = cells["A" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center});
                    cell.PutValue(data.LoB);
                    cell = cells["B" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.Region);
                    cell = cells["C" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.OperatingUnit);
                    cell = cells["D" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.Asset);
                    cell = cells["E" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.Project);
                    cell = cells["F" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.WellName);
                    cell = cells["G" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.ActivityCategory);
                    cell = cells["H" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.ActivityType);
                    cell = cells["I" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.ScopeDesc);
                    cell = cells["J" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.WellType);
                    cell = cells["K" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.DrillingOfCasing);
                    cell = cells["L" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.SpreadRateUSDDay);
                    cell = cells["M" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.BurnRateUSDDay);
                    cell = cells["N" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.MRI);
                    cell = cells["O" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.CompletionType);
                    cell = cells["P" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.CompletionZone);
                    cell = cells["Q" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.BrineDensity);
                    cell = cells["R" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.EstRangeType);
                    cell = cells["S" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.DeterminLowRange);
                    cell = cells["T" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.DeterminHigh);
                    cell = cells["U" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.ProbP10);
                    cell = cells["V" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.ProbP90);
                    cell = cells["W" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.WaterDepth);
                    cell = cells["X" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.TotalWaterDepth);
                    cell = cells["Y" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.LCFactor);
                    cell = cells["Z" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.WorkingInterest);
                    cell = cells["AA" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.PlanningClassification);
                    cell = cells["AB" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.MaturityRisk);
                    cell = cells["AC" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.FundingType);
                    cell = cells["AD" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.RFM);
                    cell = cells["AE" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.RigName);
                    cell = cells["AF" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.RigType);
                    cell = cells["AG" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.SequenceOnRig);
                    cell = cells["AH" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.Start);
                    cell = cells["AI" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.PlanYear);
                    cell = cells["AJ" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.Finish);
                    cell = cells["AK" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.TroubleFree.Days);
                    cell = cells["AL" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.NPT.PercentDays);
                    cell = cells["AM" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.TECOP.PercentDays);
                    cell = cells["AN" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.TimeOverrideFactors);
                    cell = cells["AO" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.NPT.Days);
                    cell = cells["AP" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.TECOP.Days);
                    cell = cells["AQ" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.MeanTime);
                    cell = cells["AR" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.TroubleFree.Cost);
                    cell = cells["AS" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.Currency);
                    cell = cells["AT" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.NPT.PercentCost);
                    cell = cells["AU" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.TECOP.PercentCost);
                    cell = cells["AV" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.CostOverrideFactors);
                    cell = cells["AW" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.NPT.Cost);
                    cell = cells["AX" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.TECOP.Cost);
                    cell = cells["AY" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.MeanCostEDM);
                    cell = cells["AZ" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.TroubleFreeUSD);
                    cell = cells["BA" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.NPTUSD);
                    cell = cells["BB" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.TECOPUSD);
                    cell = cells["BC" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.MeanCostEDMUSD);
                    cell = cells["BD" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.EscCostUSD);
                    cell = cells["BE" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.CSOCostUSD);
                    cell = cells["BF" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.InflationCostUSD);
                    cell = cells["BG" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.MeanCostMODUSD);
                    cell = cells["BH" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.ProjectValueDriver);
                    cell = cells["BI" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.TQ.Threshold);
                    cell = cells["BJ" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.BIC.Threshold);
                    cell = cells["BK" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.TQ.Gap);
                    cell = cells["BL" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.BIC.Gap);
                    cell = cells["BM" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.PerfScore);
                    cell = cells["BN" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.ActivityCount);
                    cell = cells["BO" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.ScheduleID);
                    cell = cells["BP" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.SSTroubleFreeUSD);
                    cell = cells["BQ" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.SSNPTUSD);
                    cell = cells["BR" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.SSTECOPUSD);
                    cell = cells["BS" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.SSMeanCostEDMUSD);
                    cell = cells["BT" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.SSEscCostUSD);
                    cell = cells["BU" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.SSCSOCostUSD);
                    cell = cells["BV" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.SSInflationCostUSD);
                    cell = cells["BW" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.SSMeanCostMODUSD);
                    cell = cells["BX" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.MeanCostRTUSD);
                    cell = cells["BY" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.SSMeanCostRTUSD);
                    cell = cells["BZ" + walkingIdx];
                    //cell.SetStyle(new Style() { IsTextWrapped = true, VerticalAlignment = TextAlignmentType.Center });
                    cell.PutValue(data.OPScope);

                    cells[Index, 37].SetStyle(new Style() { Number = 10 });
                    cells[Index, 38].SetStyle(new Style() { Number = 10 });
                    cells[Index, 45].SetStyle(new Style() { Number = 10 });
                    cells[Index, 46].SetStyle(new Style() { Number = 10 });
                    walkingIdx++; Index++;
                }
                for (int i = 0; i < 9; i++)
                {
                    sheet1.AutoFitColumn(i);
                }
                sheet1.AutoFitColumn(32);
                sheet1.AutoFitColumn(66);
                sheet1.AutoFitColumn(77);
                //sheet1.AutoFitRows();
                #endregion
                
                sheet2.Name = "PivotTable";
                cells = sheet2.Cells;
                var cellx = cells["B1"];
                cellx.PutValue("Pivot Table");
                Style style = cellx.GetStyle();
                style.Font.Size = 20;
                style.Font.Color = System.Drawing.Color.Red;
                cellx.SetStyle(style);
                //Getting the pivottables collection in the sheet
                Aspose.Cells.Pivot.PivotTableCollection pivotTables = sheet2.PivotTables;
                //Adding a PivotTable to the worksheet
                int index = pivotTables.Add("=Data!A1:BZ" + (walkingIdx-1), "B13", "PivotTable1");
                //Accessing the instance of the newly added PivotTable
                Aspose.Cells.Pivot.PivotTable pivotTable = pivotTables[index];


                //styling pivot table
                pivotTable.PivotTableStyleType = PivotTableStyleType.PivotTableStyleLight16;
                
                //Showing the grand totals
                pivotTable.RowGrand = true;
                pivotTable.ColumnGrand = true;
                
                //Setting the PivotTable report is automatically formatted
                pivotTable.IsAutoFormat = true;

                //Setting the PivotTable autoformat type.
                pivotTable.AutoFormatType = Aspose.Cells.Pivot.PivotTableAutoFormatType.Classic;//6
                pivotTable.AddFieldToArea(Aspose.Cells.Pivot.PivotFieldType.Row, 5);//E
                pivotTable.AddFieldToArea(Aspose.Cells.Pivot.PivotFieldType.Row, 7);//H
                pivotTable.AddFieldToArea(Aspose.Cells.Pivot.PivotFieldType.Column, 34);//AI
                pivotTable.AddFieldToArea(Aspose.Cells.Pivot.PivotFieldType.Data, 58);//BG

                //Setting the number format of the first data field
                pivotTable.DataFields[0].NumberFormat = "#,##0.00";
                //pivotTable.DataFields[0].ShowSubtotalAtTop = true;
                pivotTable.ColumnFields[0].ShowSubtotalAtTop = true;
                pivotTable.RowFields[0].ShowSubtotalAtTop = true;
                //pivotTable.DataFields[0].InsertBlankRow = false;
                //pivotTable.RowFields[0].InsertBlankRow = true;
                //pivotTable.RowFields[0].ShowInOutlineForm = true;
                //pivotTable.RowFields[0].InsertBlankRow = true;
                //pivotTable.ColumnFields[0].SetSubtotals(PivotFieldSubtotalType.Automatic, false);
                pivotTable.ShowInCompactForm();
                
                //for (int i = 0; i < pivotTable.RowFields.Count; i++)
                //{
                //    pivotTable.RowFields[i].ShowCompact = true;
                //} 

                ////Filter
                pivotTable.AddFieldToArea(Aspose.Cells.Pivot.PivotFieldType.Page, "Line Of Business");
                pivotTable.AddFieldToArea(Aspose.Cells.Pivot.PivotFieldType.Page, "Region");
                pivotTable.AddFieldToArea(Aspose.Cells.Pivot.PivotFieldType.Page, "Operating Unit");
                pivotTable.AddFieldToArea(Aspose.Cells.Pivot.PivotFieldType.Page, "Asset");
                pivotTable.AddFieldToArea(Aspose.Cells.Pivot.PivotFieldType.Page, "Project");
                pivotTable.AddFieldToArea(Aspose.Cells.Pivot.PivotFieldType.Page, "Activity Category");
                pivotTable.AddFieldToArea(Aspose.Cells.Pivot.PivotFieldType.Page, "Funding Type");
                pivotTable.AddFieldToArea(Aspose.Cells.Pivot.PivotFieldType.Page, "Rig Type");
                pivotTable.AddFieldToArea(Aspose.Cells.Pivot.PivotFieldType.Page, "Rig Name");


                pivotTable.RefreshData();
                pivotTable.CalculateData();

                var nm = "Spotfire-" + DateTime.Now.ToString("yyyy-MM-dd HHmmss") + ".xlsx";
                var retstringName = Path.Combine(Server.MapPath("~/App_Data/Temp"), nm);
                worbbook.Save(retstringName, Aspose.Cells.SaveFormat.Xlsx);


                return new
                {
                    nm,
                    RFMChecking
                };

            });
        }
    }
}