using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Text;

using System.IO;


using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class TopLevelAdjusmentController : Controller
    {
        //
        // GET: /Shell/TopLevelAdjusment/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetData(WaterfallBase wb,BizPlanTLASetting TLASetting, int yearStart = 0, int yearFinish = 0, bool isTLA = false,string[] OPs=null,string opRelation="AND")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var email = WebTools.LoginUser.Email;
                var p = wb.GetActivitiesForBizPlan(email);
                
                var unwind = p.SelectMany(d => d.Phases, (x, y) => new
                                            {
                                                Allocation = y.Allocation,
                                                Estimate = y.Estimate,
                                                Country = x.Country,
                                                ShellShare = x.ShellShare,
                                                RFM = x.ReferenceFactorModel,
                                                WellName = x.WellName,
                                                ActivityType = y.ActivityType,
                                                 RigName = x.RigName
                                            }).ToList();

                var anuls = unwind.SelectMany(x => x.Allocation.AnnualyBuckets).GroupBy(x => x.Key).ToList();
                var periodYears = new List<Int32>();


                var maxYear = 2015;
                var minYear = 2015;
                if (anuls.Any())
                {
                    maxYear = anuls.Max(x => Convert.ToInt32(x.Key));
                    minYear = anuls.Min(x => Convert.ToInt32(x.Key));
                }
               
                List<BsonDocument> bs = new List<BsonDocument>();
                foreach (var pAllocation in unwind)
                {
                    BsonDocument bsh = new BsonDocument();
                    bsh.Set("WellName",pAllocation.WellName);
                    bsh.Set("ActivityType", pAllocation.ActivityType);
                    bsh.Set("RigName", pAllocation.RigName);

                    for (var i = minYear; i <= maxYear; i++)
                    {
                        if (pAllocation.Allocation.AnnualyBuckets.Any())
                        {
                            foreach (var panuls in pAllocation.Allocation.AnnualyBuckets)
                            {
                                if (i == Convert.ToInt32(panuls.Key))
                                {
                                    var current = panuls.MeanCostMOD;
                                    var adjusted = panuls.MeanCostMOD;
                                    if (isTLA)
                                    {
                                        var maturityLevel = "0";
                                        if (pAllocation.Estimate.MaturityLevel != null)
                                        {
                                            pAllocation.Estimate.MaturityLevel.Substring(pAllocation.Estimate.MaturityLevel.Length - 1, 1);
                                        }
                                        //var getAdjusted = BizPlanCalculation.calcBizPlan(pAllocation.Allocation.WellName, pAllocation.Allocation.RigName, pAllocation.Allocation.ActivityType, 
                                        //    pAllocation.Country, pAllocation.ShellShare,
                                        //    pAllocation.Estimate.EstimatePeriod, Convert.ToInt32(maturityLevel),
                                        //    pAllocation.Estimate.Services, pAllocation.Estimate.Materials, pAllocation.Estimate.NewTroubleFree.Days, pAllocation.RFM,
                                        //    Tools.Div(pAllocation.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(pAllocation.Estimate.NewTECOPTime.PercentDays, 100),
                                        //    Tools.Div(pAllocation.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(pAllocation.Estimate.NewTECOPTime.PercentCost, 100),
                                        //    pAllocation.Estimate.LongLeadMonthRequired, Tools.Div(pAllocation.Estimate.PercOfMaterialsLongLead, 100), pAllocation.Estimate.SpreadRateTotal,
                                        //    pAllocation.Estimate.NewMean.Days,
                                        //    isTLA,TLASetting.Services,TLASetting.Material,TLASetting.Days,
                                        //    TLASetting.NPTDays,TLASetting.Tangibles,TLASetting.SpreadRate);

                                        BizPlanSummary getAdjusted = new BizPlanSummary(pAllocation.Allocation.WellName, pAllocation.Allocation.RigName, pAllocation.Allocation.ActivityType,
                                            pAllocation.Country, pAllocation.ShellShare, pAllocation.Estimate.EstimatePeriod, Convert.ToInt32(maturityLevel), pAllocation.Estimate.Services,
                                            pAllocation.Estimate.Materials, pAllocation.Estimate.NewTroubleFree.Days, pAllocation.RFM,
                                            Tools.Div(pAllocation.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(pAllocation.Estimate.NewTECOPTime.PercentDays, 100),
                                            Tools.Div(pAllocation.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(pAllocation.Estimate.NewTECOPTime.PercentCost, 100),
                                            pAllocation.Estimate.LongLeadMonthRequired, Tools.Div(pAllocation.Estimate.PercOfMaterialsLongLead, 100), lcf: pAllocation.Estimate.NewLearningCurveFactor);
                                        
                                        //set TLA
                                        getAdjusted.CalcTLA(TLASetting.Days, TLASetting.NPTDays, TLASetting.Material, TLASetting.Services, TLASetting.Tangibles, TLASetting.SpreadRate);
                                        getAdjusted.GeneratePeriodBucket();
                                        getAdjusted.GenerateAnnualyBucket(getAdjusted.MonthlyBuckets);

                                        var adjusted1 = getAdjusted.AnnualyBuckets.Where(x => Convert.ToInt32(x.Key).Equals(i)).FirstOrDefault();
                                        if (adjusted1 != null)
                                        {
                                            adjusted = adjusted1.MeanCostMOD;
                                        }
                                    }
                                    var variance = adjusted - current;
                                    bsh.Set("Current_" + panuls.Key, panuls.MeanCostMOD);
                                    bsh.Set("Adjusted_" + panuls.Key, adjusted);
                                    bsh.Set("Variance_" + panuls.Key, variance);
                                    break;
                                }
                                else
                                {
                                    bsh.Set("Current_" + i, 0);
                                    bsh.Set("Adjusted_" + i, 0);
                                    bsh.Set("Variance_" + i, 0);
                                }
                            }
                        }
                        else
                        {
                            bsh.Set("Current_" + i, 0);
                            bsh.Set("Adjusted_" + i, 0);
                            bsh.Set("Variance_" + i, 0);
                        }
                    }
                    
                    bs.Add(bsh);
                }

                periodYears.Add(minYear);
                periodYears.Add(maxYear);

                return new { Detail = DataHelper.ToDictionaryArray(bs), periodYears = periodYears };
            });
        }


        public JsonResult SaveAdjusment(WaterfallBase wb, BizPlanTLASetting TLASetting, int yearStart = 0, int yearFinish = 0, bool isTLA = false)
        {
            return MvcResultInfo.Execute(() =>
            {
                var email = WebTools.LoginUser.Email;
                var bps = wb.GetActivitiesForBizPlan(email);

                foreach (var bp in bps)
                {
                    var newPhases = new List<BizPlanActivityPhase>();
                    foreach (var Phase in bp.Phases)
                    {
                        var np = Phase;
                        if ( wb.Activities == null || wb.Activities.Count() == 0 || wb.Activities.Contains(Phase.ActivityType))
                        {
                            if (isTLA)
                            {
                                var maturityLevel = Phase.Estimate.MaturityLevel.Substring(Phase.Estimate.MaturityLevel.Length - 1, 1);
                                BizPlanSummary res = new BizPlanSummary(bp.WellName, Phase.Estimate.RigName, Phase.ActivityType,
                                            bp.Country, bp.ShellShare, Phase.Estimate.EstimatePeriod, Convert.ToInt32(maturityLevel), Phase.Estimate.Services,
                                            Phase.Estimate.Materials, Phase.Estimate.NewTroubleFree.Days, bp.ReferenceFactorModel,
                                            Tools.Div(Phase.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(Phase.Estimate.NewTECOPTime.PercentDays, 100),
                                            Tools.Div(Phase.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(Phase.Estimate.NewTECOPTime.PercentCost, 100),
                                            Phase.Estimate.LongLeadMonthRequired, Tools.Div(Phase.Estimate.PercOfMaterialsLongLead, 100), lcf:Phase.Estimate.NewLearningCurveFactor);

                                //set TLA
                                res.CalcTLA(TLASetting.Days, TLASetting.NPTDays, TLASetting.Material, TLASetting.Services, TLASetting.Tangibles, TLASetting.SpreadRate);
                                res.GeneratePeriodBucket();
                                res.GenerateAnnualyBucket(res.MonthlyBuckets);


                                var tlaNptDay = TLASetting.NPTDays;
                                double nptPer = tlaNptDay > 1 ? Tools.Div(tlaNptDay, 100) : tlaNptDay;

                                Phase.Estimate.NewTroubleFree.Days = res.TroubleFree.Days;
                                Phase.Estimate.NewNPTTime.Days = res.NPT.Days;
                                Phase.Estimate.NewNPTTime.Cost = res.NPT.Cost;
                                Phase.Estimate.NewNPTTime.PercentDays = Phase.Estimate.NewNPTTime.PercentDays + (Phase.Estimate.NewNPTTime.PercentDays * nptPer);

                                Phase.Estimate.NewLCFValue.Days = res.NewLCFValue.Days;
                                Phase.Estimate.NewLCFValue.Cost = res.NewLCFValue.Cost;
                                Phase.Estimate.NewBaseValue.Days = res.NewBaseValue.Days;
                                Phase.Estimate.NewBaseValue.Cost = res.NewLCFValue.Cost;
                                //get tangible %
                                //var tangibleValue = res.MaterialLongLeadValue.TangibleValue;
                                //var percOfMLL = Tools.Div(tangibleValue, Phase.Estimate.LongLeadCalc) * 100;
                                //Phase.Estimate.PercOfMaterialsLongLead = percOfMLL;

                                Phase.Estimate.PercOfMaterialsLongLead = Phase.Estimate.PercOfMaterialsLongLead + TLASetting.Tangibles;
                                Phase.Estimate.Services = res.Services;
                                Phase.Estimate.Materials = res.Material;
                                Phase.Estimate.SpreadRateTotal = res.SpreadRateTotal;
                                Phase.Estimate.BurnRate = res.BurnRate;
                                Phase.Estimate.RigRate = res.RigRate;
                                Phase.Estimate.SpreadRate = res.SpreadRateWRig;
                                Phase.Estimate.isMaterialLLSetManually = true;
                                Phase.Estimate.UsingTAApproved = false;

                            }
                        }
                        newPhases.Add(np);
                    }

                    bp.Phases = newPhases;
                    var cfg = Config.GetConfigValue("BizPlanConfig");
                    if (cfg != null && Convert.ToBoolean(cfg))
                        bp.Save(references: new string[] { "updatetowellplan" });
                    else
                        bp.Save();
                }

                return "OK";
            });
        }


        #region Not Used

        //public JsonResult GetBizPlanActivity(string dateStart, string dateFinish, string dateStart2, string dateFinish2, string dateRelation, string[] operatingUnits, string[] projectNames, string[] regions, string[] rigNames, string[] rigTypes, string[] wellNames, string[] activities, string[] exType, float pspreadrate = 0, float pdays = 0, float pnptdays = 0, float ptangibles = 0, float pservices = 0, float pmaterial = 0)
        //{
        //    var ri = new ResultInfo();

        //    //try
        //    //{
        //    //    var dashboardC = new DashboardController();
        //    //    var query = dashboardC.GenerateQueryFromFilterAndDate(dateStart, dateFinish, dateStart2, dateFinish2, dateRelation, operatingUnits,
        //    //        projectNames, regions, rigNames, rigTypes, wellNames, activities, "OpsSchedule.Start", "OpsSchedule.Finish", exType);

        //    //    var t = BizPlanActivity.Populate<BizPlanActivity>(query);
        //    //    List<BsonDocument> res = new List<BsonDocument>();
        //    //    if (t.Count > 0)
        //    //    {
        //    //        foreach (var data in t)
        //    //        {
        //    //            var Activity = data.RigName;
        //    //                BsonDocument d = new BsonDocument();

        //    //                if (string.IsNullOrEmpty(Activity)) { Activity = ""; }
        //    //                d.Set("Activity", Activity);
        //    //                int i = 0;
        //    //                    foreach (var data1 in data.Phases)
        //    //                    {
        //    //                        i++;
        //    //                        var SpreadRate = Convert.ToDouble(data1.Estimate.SpreadRate);
        //    //                        var SpreadRateTotal = Convert.ToDouble(data1.Estimate.SpreadRateTotal);
        //    //                        SpreadRateTotal = 72;
        //    //                        var NptDays = Convert.ToInt16(data1.Estimate.CurrentNPTTime.Days);
        //    //                        var Days = data1.Estimate.CurrentTroubleFree.Days;

        //    //                        var Tangible = Convert.ToDouble(data1.Estimate.Tangibles);
        //    //                        var Material = Convert.ToDouble(data1.Estimate.Materials);
        //    //                        var Services = Convert.ToDouble(data1.Estimate.Services);
        //    //                        var CurrentDays = Days + NptDays + i;
        //    //                        var CurrentCost = SpreadRateTotal + Tangible + Material + Services;
        //    //                        var Current = CurrentDays * CurrentCost;
        //    //                        //define percentage
        //    //                        var percentSpreadRate   = 1-(pspreadrate/100);
        //    //                        var percentDays         = 1-(pdays/100);
        //    //                        var percentNptDays      = 1-(pnptdays/100);
        //    //                        var percentTangible     = 1-(ptangibles);
        //    //                        var percentMaterial     = 1-(pmaterial);
        //    //                        var percentService      = 1-(pservices);

        //    //                        var AdjSpreadrateCost = SpreadRateTotal * percentSpreadRate;
        //    //                        var AdjDays = Days * percentDays + i;
        //    //                        var AdjNtpDays = NptDays * percentNptDays;
        //    //                        var AdjTangible = Tangible * percentTangible;
        //    //                        var AdjMaterial = Material * percentMaterial;
        //    //                        var AdjService = Services * percentService;
        //    //                        var Adjment = (AdjDays + AdjNtpDays) *(AdjSpreadrateCost+AdjTangible+AdjMaterial+AdjService);

        //    //                        d.Set("Current", string.Format("{0:0.000}", Current));
        //    //                        d.Set("Adjusted", string.Format("{0:0.000}", Adjment));
        //    //                    }


        //    //                    d.Set("New", 40000);
        //    //                    res.Add(d);  
        //    //        }

        //    //        ri.Data = DataHelper.ToDictionaryArray(res);
        //    //    }
        //    //}
        //    //catch (Exception e)
        //    //{
        //    //    ri.PushException(e);
        //    //}

        //    return MvcTools.ToJsonResult(ri);
        //}

        //public JsonResult GetDummies()
        //{
        //    var ri = new ResultInfo();
        //    var d = BizPlanActivity.Populate<BizPlanActivity>()
        //    .GroupBy(a => a.RigName).Select(a => a.Key).ToList();
        //    ri.Data = d;
        //    return MvcTools.ToJsonResult(ri);
        //}

        #endregion


    }
}