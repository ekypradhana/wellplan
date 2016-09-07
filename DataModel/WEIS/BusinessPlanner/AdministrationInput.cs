using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ECIS.Client.WEIS
{
    public class AdministrationInput
    {
        public static void ResetRecalculaeBusPlanStatus()
        {
            var config = Config.Get<Config>("BizPlanRecalculateActivitiesStatus");
            if (config != null)
            {
                config.Delete();
            }
        }

        public static bool IsRecalculateBusPlan()
        {
            var config = Config.Get<Config>("BizPlanRecalculateActivitiesStatus");
            return (config != null);
        }

        public static double GetRecalculationProgress()
        {
            var config = Config.Get<Config>("BizPlanRecalculateActivitiesStatus");
            if (config == null)
            {
                return 0;
            }

            return Convert.ToDouble(config.ConfigValue);
        }

        public static void SavePlanOnRFM(BizPlanActivity activity)
        {
            if (activity.Phases.Any())
            {
                foreach (var p in activity.Phases)
                {

                    if (p.Estimate.Status != null && p.Estimate.Status != "")
                    {


                        BizPlanAllocation a = new BizPlanAllocation();
                        a.ActivityType = p.ActivityType;
                        a.WellName = activity.WellName;
                        a.RigName = p.Estimate.RigName;
                        a.EstimatePeriod = p.Estimate.EstimatePeriod;
                        a.SaveToOP = p.Estimate.SaveToOP;
                        a.UARigSequenceId = activity.UARigSequenceId;
                        a.WorkingInterest = activity.WorkingInterest;



                        var maturityLevel = p.Estimate.MaturityLevel.Substring(p.Estimate.MaturityLevel.Length - 1, 1);

                        // new logic

                        //save value TroubleFreeCostBeforeLC.Days to NewTroubleFree.Days
                        p.Estimate.NewTroubleFree.Days = p.Estimate.TroubleFreeBeforeLC.Days;

                        BizPlanSummary calc = new BizPlanSummary(activity.WellName, p.Estimate.RigName, p.ActivityType,
                            activity.Country, activity.ShellShare, p.Estimate.EstimatePeriod, Convert.ToInt32(maturityLevel),
                            p.Estimate.Services, p.Estimate.Materials, p.Estimate.TroubleFreeBeforeLC.Days, activity.ReferenceFactorModel,
                            Tools.Div(p.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentDays, 100),
                            Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100),
                            p.Estimate.LongLeadMonthRequired, Tools.Div(p.Estimate.PercOfMaterialsLongLead, 100), baseOP: p.Estimate.SaveToOP, lcf: p.Estimate.NewLearningCurveFactor, isGetExcRateByCurrency: true);
                        calc.GeneratePeriodBucket();
                        calc.GenerateAnnualyBucket(calc.MonthlyBuckets);
                        calc.GenerateQuarterBucket(calc.MonthlyBuckets);

                        a.AnnualyBuckets = calc.AnnualyBuckets;
                        a.MonthlyBuckets = calc.MonthlyBuckets;
                        a.QuarterlyBuckets = calc.QuarterBuckets;

                        p.Allocation = a;
                        a.Save();
                        p.Estimate.NewMean.Days = calc.MeanTime;
                        p.Estimate.EstimatePeriod.Finish = p.Estimate.EstimatePeriod.Start.AddDays(calc.MeanTime);
                        p.Estimate.EventEndDate = p.Estimate.EstimatePeriod.Finish;
                        p.Estimate.EventStartDate = p.Estimate.EstimatePeriod.Start;

                        p.Estimate.NewLCFValue.Days = Math.Round(calc.NewLCFValue.Days, 2);
                        p.Estimate.NewLCFValue.Cost = Math.Round(calc.NewLCFValue.Cost, 2);
                        p.Estimate.NewBaseValue.Days = Math.Round(calc.NewBaseValue.Days, 2);
                        p.Estimate.NewBaseValue.Cost = Math.Round(calc.NewBaseValue.Cost, 2);
                        p.Estimate.SpreadRate = calc.SpreadRateWRig;
                        p.Estimate.SpreadRateTotal = calc.SpreadRateTotal;
                        p.Estimate.RigRate = calc.RigRate;
                        p.Estimate.BurnRate = calc.BurnRate;

                        //cost things
                        p.Estimate.MeanCostMOD = calc.MeanCostMOD;
                        p.PushToWellPlan = true;

                        p.Estimate.NewTroubleFree.Cost = calc.TroubleFree.Cost;
                        p.Estimate.Materials = calc.Material;
                        p.Estimate.Services = calc.Services;
                        p.Estimate.NewNPTTime.Cost = calc.NPT.Cost;
                        p.Estimate.NewTECOPTime.Cost = calc.TECOP.Cost;
                        p.Estimate.NewMean.Cost = calc.MeanCostEDM;

                        //USD
                        p.Estimate.BurnRateUSD = p.Estimate.BurnRate;
                        p.Estimate.MaterialsUSD = p.Estimate.Materials;
                        p.Estimate.MeanUSD = p.Estimate.NewMean.Cost;
                        p.Estimate.NewTroubleFreeUSD = p.Estimate.NewTroubleFree.Cost;
                        p.Estimate.NPTCostUSD = p.Estimate.NewNPTTime.Cost;
                        p.Estimate.RigRatesUSD = p.Estimate.RigRate;
                        p.Estimate.ServicesUSD = p.Estimate.Services;
                        p.Estimate.SpreadRateTotalUSD = p.Estimate.SpreadRateTotal;
                        p.Estimate.SpreadRateUSD = p.Estimate.SpreadRate;
                        p.Estimate.TECOPCostUSD = p.Estimate.NewTECOPTime.Cost;

                        if (!p.Estimate.Status.Trim().ToLower().Equals("draft"))
                        {
                            var wa = WellActivity.Get<WellActivity>(
                            Query.And(
                                Query.EQ("WellName", activity.WellName),
                                Query.EQ("UARigSequenceId", activity.UARigSequenceId),
                                Query.EQ("Phases.ActivityType", p.ActivityType)
                            )
                            );

                            if (wa != null)
                            {
                                var pa = wa.Phases.Where(x => x.ActivityType.Equals(p.ActivityType));

                                if (pa != null)
                                {
                                    var singlePa = pa.FirstOrDefault();
                                    singlePa.PlanSchedule = p.Estimate.EstimatePeriod;
                                    singlePa.Plan.Cost = p.Estimate.MeanCostMOD;
                                    singlePa.Plan.Days = p.Estimate.NewMean.Days;

                                    wa.Save();
                                }
                            }
                        }

                        //p.Estimate.LastUpdate = activity.LastUpdate;
                    }
                }



                activity.Save();



            }
        }

        public static void RecalculateBusPlan()
        {
            if (AdministrationInput.IsRecalculateBusPlan())
            {
                throw new Exception("Cannot save because recalculation process on bus plan data is not yet finished. Please Wait for a moment.");
            }

            Task.Run(() =>
            {
                var config = Config.Get<Config>("BizPlanRecalculateActivitiesStatus");
                if (config == null)
                {
                    config = new Config()
                    {
                        _id = "BizPlanRecalculateActivitiesStatus",
                        ConfigModule = "BizPlan"
                    };
                }
                config.ConfigValue = 0;
                config.Save();

                var allData = BizPlanActivity.GetAll(null);
                double many = allData.Count;
                double i = 0;

                try
                {
                    allData.ForEach(d =>
                    {
                        SavePlanOnRFM(d);
                        i++;
                        var progress = Tools.Div(i, many);
                        config.ConfigValue = progress;
                        config.Save();
                    });
                }
                catch (Exception e)
                {
                    var f = e.Message + " " + e.StackTrace;
                }

                config.Delete();
            });
        }
    }
}
