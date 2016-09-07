using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.RegularExpressions;

namespace ECIS.Client.WEIS
{
    public class CostDetail
    {
        public double EDM { get; set; }
        public double EDMSS { get; set; }
        public double MOD { get; set; }
        public double MODSS { get; set; }
        public double RealTerm { get; set; }
        public double RealTermSS { get; set; }
        public CostDetail()
        {
            EDM = 0.0;
            EDMSS = 0.0;
            MOD = 0.0;
            RealTerm = 0.0;
            MODSS = 0.0;
            RealTermSS = 0.0;
        }
    }

    public class CapexCalc
    {
        public double MaterialCost { get; set; }
        public double RigCost { get; set; }
        public double ServicesCost { get; set; }

        public double EDMCost { get; set; }
        public double MODCost { get; set; }
        public double RTCost { get; set; }
        public double EDMCostSS { get; set; }
        public double MODCostSS { get; set; }
        public double RTCostSS { get; set; }

        public double tecopmat { get; set; }
        public double tecopIntangDelta { get; set; }
        public double tecopmatSS { get; set; }
        public double tecopIntangDeltaSS { get; set; }

        public double modtecopmat { get; set; }
        public double modtecopIntangDelta { get; set; }
        public double modtecopmatSS { get; set; }
        public double modtecopIntangDeltaSS { get; set; }


        public double rttecopmat { get; set; }
        public double rttecopIntangDelta { get; set; }
        public double rttecopmatSS { get; set; }
        public double rttecopIntangDeltaSS { get; set; }


        public double lcfCostTangibles { get; set; }
        public double lcfCostInTangibles { get; set; }
        public double tecoppercent { get; set; }
        public double nptpercent { get; set; }

        public double tecoppercent_ { get; set; }



        public double tecopIntangEquation { get; set; }

        public double EDMTangibles { get; set; }
        public double EDMInTangibles { get; set; }
        public double escTangibles { get; set; }
        public double escInTangibles { get; set; }


        public double CSOPercent { get; set; }
        public double CSOCostTangibles { get; set; }
        public double CSOCostInTangibles { get; set; }

        public double inflationPercent { get; set; }
        public double inflationCostTangibles { get; set; }
        public double inflationCostInTangibles { get; set; }

        public double modTangibles { get; set; }
        public double modInTangibles { get; set; }

        public double RTTangibles { get; set; }
        public double RTInTangibles { get; set; }

        public double shellshare { get; set; }

        public double EDMTanSS { get; set; }
        public double EDMINTANSS { get; set; }
        public double MODTanSS { get; set; }
        public double MODINTANSS { get; set; }
        public double RTTanSS { get; set; }
        public double RTINTANSS { get; set; }


        public double TECOPServices { get; set; }
        public double MODServices { get; set; }
        public double TECOPRig { get; set; }
        public double MODRig { get; set; }
        public double EscPercentRig { get; set; }

    }

    public class PDetail
    {

        public CapexCalc Calc { get; set; }

        public CostDetail CapitalDrillingPDDevTang { get; set; }
        public CostDetail CapitalDrillingPDDevInTang { get; set; }
        //public CostDetail CapitalDrillingPDDevTangSS { get; set; }
        //public CostDetail CapitalDrillingPDDevInTangSS { get; set; }

        public CostDetail CapitalCompletionPDDevTang { get; set; }
        public CostDetail CapitalCompletionPDDevInTang { get; set; }
        //public CostDetail CapitalCompletionPDDevTangSS { get; set; }
        //public CostDetail CapitalCompletionPDDevInTangSS { get; set; }

        public CostDetail EPEXDrillingB2ExplTang { get; set; }
        public CostDetail EPEXDrillingB2ExplInTang { get; set; }
        //public CostDetail EPEXDrillingB2ExplTangSS { get; set; }
        //public CostDetail EPEXDrillingB2ExplInTangSS { get; set; }

        public CostDetail EPEXCompletionB2ExplTang { get; set; }
        public CostDetail EPEXCompletionB2ExplInTang { get; set; }
        //public CostDetail EPEXCompletionB2ExplTangSS { get; set; }
        //public CostDetail EPEXCompletionB2ExplInTangSS { get; set; }

        public CostDetail CapitalExpenDRDVAWells { get; set; }
        public CostDetail CapitalExpenDRSubSeaWells { get; set; }
        //public CostDetail CapitalExpenDRDVAWellsSS { get; set; }
        //public CostDetail CapitalExpenDRSubSeaWellsSS { get; set; }

        public CostDetail OPCostIdleRig { get; set; }
        //public CostDetail OPCostIdleRigSS { get; set; }

        public CostDetail ContigencyTangWells { get; set; }
        public CostDetail ContigencyInTangWells { get; set; }
        //public CostDetail ContigencyTangWellsSS { get; set; }
        //public CostDetail ContigencyInTangWellsSS { get; set; }

        public string Currency { get; set; }
        public PDetail()
        {

            Calc = new CapexCalc();

            CapitalDrillingPDDevTang = new CostDetail();
            CapitalDrillingPDDevInTang = new CostDetail();

            CapitalCompletionPDDevTang = new CostDetail();
            CapitalCompletionPDDevInTang = new CostDetail();

            EPEXDrillingB2ExplTang = new CostDetail();
            EPEXDrillingB2ExplInTang = new CostDetail();

            EPEXCompletionB2ExplTang = new CostDetail();
            EPEXCompletionB2ExplInTang = new CostDetail();

            CapitalExpenDRDVAWells = new CostDetail();
            CapitalExpenDRSubSeaWells = new CostDetail();

            OPCostIdleRig = new CostDetail();

            ContigencyTangWells = new CostDetail();
            ContigencyInTangWells = new CostDetail();

        }
    }

    public class PDetails : PDetail
    {
        public int DateId { get; set; }
        public string Type { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class PCapex : ECISModel
    {
        public override string TableName
        {
            get { return "WEISPalantirCapex"; }
            //get { return "WEISPalantirMapCapex"; }
        }

        public string UpdateBy { get; set; }
        public string CaseName { get; set; }
        public string MapName { get; set; }
        public bool IsInPlan { get; set; }
        public DateTime UpdateVersion { get; set; }
        public double ShellShare { get; set; }

        public string WellName { get; set; }
        public string RigName { get; set; }
        public string ActivityType { get; set; }
        public string PlanningClassification { get; set; }
        public string UARigSequenceId { get; set; }
        public string ActivityCategory { get; set; }
        public string BaseOP { get; set; }
        public string FundingType { get; set; }
        public string ProjectName { get; set; }

        public string FirmOption { get; set; }

        public string AssetName { get; set; }
        public string LoB { get; set; }

        public DateRange PhSchedule { get; set; }
        public WellDrillData OP { get; set; }


        public DateRange PlanSchedule { get; set; }
        public WellDrillData Plan { get; set; }

        public PDetail CapexSummary { get; set; }
        public List<PDetails> CapexDetails { get; set; }
        public BizPlanAllocation Allocation { get; set; }
        public BizPlanActivityEstimate Estimate { get; set; }
        public string RFM { get; set; }


        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            _id = String.Format("W{0}S{1}A{2}R{3}D{4}M{5}",
            WellName.Replace(" ", "").Replace("-", ""),
            UARigSequenceId,
            ActivityType.Replace(" ", "").Replace("-", ""),
            RigName.Replace(" ", "").Replace("-", ""),
            UpdateBy,
            MapName.Replace(" ", "").Replace("-", "")
            );

            return this.ToBsonDocument();
        }

        public PCapex()
        {
            PhSchedule = new DateRange();
            OP = new WellDrillData();
            PlanSchedule = new DateRange();
            Plan = new WellDrillData();
            CapexSummary = new PDetail();
            CapexDetails = new List<PDetails>();
            UpdateBy = "";
            CaseName = "";
            MapName = "";
            WellName = "";
            RigName = "";
            ActivityType = "";
            UARigSequenceId = "";
            ActivityCategory = "";
            BaseOP = "";
            FundingType = "";
            ProjectName = "";
            FirmOption = "";
            AssetName = "";
            LoB = "";

        }

        public static bool isHaveBizPlan(string well, string seq, string events, out BizPlanActivity outbiz)
        {
            BizPlanActivity bz = null;
            var t = BizPlanActivity.Populate<BizPlanActivity>(
                Query.And(
                    Query.EQ("WellName", well),
                    Query.EQ("UARigSequenceId", seq),
                    Query.EQ("Phases.ActivityType", events)
                )
                );
            if (t != null && t.Count() > 0)
            {
                bz = t.FirstOrDefault();
                outbiz = bz;
                return true;

            }
            else
            {
                outbiz = bz;
                return false;
            }
        }

        #region marked temporary
        //public List<PCapex> Transforms(WellActivity wa, string updateBy, string caseName, DateRange startmonthrangetofinish, bool isAggr)
        //{
        //    DateTime nowDate = DateTime.Now.Date;

        //    List<PCapex> result = new List<PCapex>();
        //    foreach (var t in wa.Phases.Where(x => x.PhSchedule.Start != null && !WellActivity.isHaveWeeklyReport(wa.WellName, wa.UARigSequenceId, x.ActivityType)))
        //    {
        //        PCapex p = new PCapex();
        //        p.WellName = wa.WellName;
        //        p.RigName = wa.RigName;
        //        p.UARigSequenceId = wa.UARigSequenceId;
        //        p.ActivityType = t.ActivityType;
        //        p.CaseName = caseName;
        //        p.UpdateVersion = Tools.ToUTC(nowDate, true);

        //        var actgh = DataHelper.Get("WEISActivities", Query.EQ("_id", p.ActivityType));

        //        var actcat = actgh == null ? "" : actgh.GetString("ActivityCategory");
        //        p.ActivityCategory = actcat == null ? "" : actcat;

        //        if (p.ActivityType.Equals("RIG - IDLE"))
        //            p.ActivityCategory = p.ActivityType;
        //        p.BaseOP = t.BaseOP == null ? "" : (t.BaseOP.Count <= 0 ? "" : t.BaseOP.OrderByDescending(x => x).FirstOrDefault());
        //        p.ProjectName = wa.ProjectName;
        //        p.FundingType = t.FundingType;
        //        p.PhSchedule = t.PhSchedule;
        //        p.OP = t.OP;
        //        p.PlanSchedule = t.PlanSchedule;
        //        p.Plan = t.Plan;
        //        p.UpdateBy = updateBy;
        //        p.IsInPlan = t.IsInPlan;
        //        p.FirmOption = wa.FirmOrOption;
        //        p.AssetName = wa.AssetName;

        //        p = DetailSummary(p, startmonthrangetofinish);
        //        result.Add(p);
        //    }
        //    return result;
        //}

        //private PCapex DetailSummary(PCapex p, DateRange startmonthrangetofinish)
        //{
        //    BizPlanActivity bp = null;
        //    isHaveBizPlan(p.WellName, p.UARigSequenceId, p.ActivityType, out bp);

        //    if (bp != null)
        //    {
        //        p.LoB = bp.LineOfBusiness;
        //        var ishaveplan = bp.Phases.Where(x => x.ActivityType.Equals(p.ActivityType));
        //        if (ishaveplan != null)
        //        {
        //            var pi = ishaveplan.FirstOrDefault();
        //            if (!string.IsNullOrEmpty(pi.Estimate.SaveToOP))
        //            {

        //                p.CapexSummary.Currency = pi.Estimate.Currency;

        //                var aloc = BizPlanAllocation.Get<BizPlanAllocation>(Query.And(
        //                    Query.EQ("WellName", p.WellName),
        //                    Query.EQ("ActivityType", pi.ActivityType),
        //                    Query.EQ("RigName", p.RigName),
        //                    Query.EQ("SaveToOP", pi.Estimate.SaveToOP)

        //                    ));

        //                #region Summary
        //                if (aloc != null)
        //                {
        //                    if (p.FundingType != null && (p.ActivityCategory.Equals("DRILLING") && p.FundingType.Equals("C2E")))
        //                    {
        //                        p.CapexSummary.EPEXDrillingB2ExplTang.EDM = aloc.AnnualyBuckets.Sum(x => x.MaterialCost);
        //                        p.CapexSummary.EPEXDrillingB2ExplInTang.EDM = aloc.AnnualyBuckets.Sum(x => x.ServiceCost) + aloc.AnnualyBuckets.Sum(x => x.RigCost);
        //                        p.CapexSummary.EPEXDrillingB2ExplTang.EDMSS = aloc.AnnualyBuckets.Sum(x => x.ShellShare * x.MaterialCost);
        //                        p.CapexSummary.EPEXDrillingB2ExplInTang.EDMSS = aloc.AnnualyBuckets.Sum(x => x.ServiceCost * x.ShellShare) + aloc.AnnualyBuckets.Sum(x => x.RigCost * x.ShellShare);

        //                    }
        //                    if (p.FundingType != null && (p.ActivityCategory.Equals("COMPLETION") && p.FundingType.Equals("C2E")))
        //                    {
        //                        p.CapexSummary.EPEXCompletionB2ExplTang.EDM = aloc.AnnualyBuckets.Sum(x => x.MaterialCost);
        //                        p.CapexSummary.EPEXCompletionB2ExplInTang.EDM = aloc.AnnualyBuckets.Sum(x => x.ServiceCost) + aloc.AnnualyBuckets.Sum(x => x.RigCost);
        //                        p.CapexSummary.EPEXCompletionB2ExplTang.EDMSS = aloc.AnnualyBuckets.Sum(x => x.MaterialCost * x.ShellShare);
        //                        p.CapexSummary.EPEXCompletionB2ExplInTang.EDMSS = aloc.AnnualyBuckets.Sum(x => x.ServiceCost * x.ShellShare) + aloc.AnnualyBuckets.Sum(x => x.RigCost * x.ShellShare);
        //                    }

        //                    if (p.FundingType != null && (p.ActivityCategory.Equals("DRILLING") && p.FundingType.Equals("CAPEX")))
        //                    {
        //                        p.CapexSummary.CapitalDrillingPDDevTang.EDM = aloc.AnnualyBuckets.Sum(x => x.MaterialCost);
        //                        p.CapexSummary.CapitalDrillingPDDevInTang.EDM = aloc.AnnualyBuckets.Sum(x => x.ServiceCost) + aloc.AnnualyBuckets.Sum(x => x.RigCost);
        //                        p.CapexSummary.CapitalDrillingPDDevTang.EDMSS = aloc.AnnualyBuckets.Sum(x => x.MaterialCost * x.ShellShare);
        //                        p.CapexSummary.CapitalDrillingPDDevInTang.EDMSS = aloc.AnnualyBuckets.Sum(x => x.ServiceCost * x.ShellShare) + aloc.AnnualyBuckets.Sum(x => x.RigCost * x.ShellShare);
        //                    }
        //                    if (p.FundingType != null && (p.ActivityCategory.Equals("COMPLETION") && p.FundingType.Equals("CAPEX")))
        //                    {
        //                        p.CapexSummary.CapitalCompletionPDDevTang.EDM = aloc.AnnualyBuckets.Sum(x => x.MaterialCost);
        //                        p.CapexSummary.CapitalCompletionPDDevInTang.EDM = aloc.AnnualyBuckets.Sum(x => x.ServiceCost) + aloc.AnnualyBuckets.Sum(x => x.RigCost);
        //                        p.CapexSummary.CapitalCompletionPDDevTang.EDMSS = aloc.AnnualyBuckets.Sum(x => x.MaterialCost * x.ShellShare);
        //                        p.CapexSummary.CapitalCompletionPDDevInTang.EDMSS = aloc.AnnualyBuckets.Sum(x => x.ServiceCost * x.ShellShare) + aloc.AnnualyBuckets.Sum(x => x.RigCost * x.ShellShare);
        //                    }
        //                    if (p.FundingType != null && (p.ActivityCategory.Equals("ABANDONMENT") && p.FundingType.Equals("ABEX")))
        //                    {
        //                        p.CapexSummary.CapitalExpenDRDVAWells.EDM = aloc.AnnualyBuckets.Sum(x => x.MaterialCost);
        //                        p.CapexSummary.CapitalExpenDRSubSeaWells.EDM = aloc.AnnualyBuckets.Sum(x => x.ServiceCost) + aloc.AnnualyBuckets.Sum(x => x.RigCost);
        //                        p.CapexSummary.CapitalExpenDRDVAWells.EDMSS = aloc.AnnualyBuckets.Sum(x => x.MaterialCost * x.ShellShare);
        //                        p.CapexSummary.CapitalExpenDRSubSeaWells.EDMSS = aloc.AnnualyBuckets.Sum(x => x.ServiceCost * x.ShellShare) + aloc.AnnualyBuckets.Sum(x => x.RigCost * x.ShellShare);
        //                    }
        //                    if (p.FundingType != null && (p.ActivityType.Contains("RIG") && p.ActivityType.Contains("IDLE")) && p.FundingType.Equals("OPEX"))
        //                    {
        //                        p.CapexSummary.OPCostIdleRig.EDM = aloc.AnnualyBuckets.Sum(x => x.MaterialCost) +
        //                            aloc.AnnualyBuckets.Sum(x => x.RigCost) +
        //                            aloc.AnnualyBuckets.Sum(x => x.ServiceCost);
        //                        p.CapexSummary.OPCostIdleRig.EDMSS = aloc.AnnualyBuckets.Sum(x => x.MaterialCost * x.ShellShare) +
        //                            aloc.AnnualyBuckets.Sum(x => x.RigCost * x.ShellShare) +
        //                            aloc.AnnualyBuckets.Sum(x => x.ServiceCost * x.ShellShare);
        //                    }
        //                    if (p.FundingType != null && (p.FundingType.Equals("CAPEX") || p.FundingType.Equals("C2E")))
        //                    {
        //                        p.CapexSummary.ContigencyTangWells.EDM = aloc.AnnualyBuckets.Sum(x => x.MaterialCost);
        //                        p.CapexSummary.ContigencyInTangWells.EDM = aloc.AnnualyBuckets.Sum(x => x.ServiceCost) + aloc.AnnualyBuckets.Sum(x => x.RigCost);
        //                        p.CapexSummary.ContigencyTangWells.EDMSS = aloc.AnnualyBuckets.Sum(x => x.MaterialCost * x.ShellShare);
        //                        p.CapexSummary.ContigencyInTangWells.EDMSS = aloc.AnnualyBuckets.Sum(x => x.ServiceCost * x.ShellShare) + aloc.AnnualyBuckets.Sum(x => x.RigCost * x.ShellShare);
        //                    }
        //                }

        //                #endregion

        //                #region Details
        //                if (startmonthrangetofinish.Start != Tools.DefaultDate)
        //                {
        //                    if (startmonthrangetofinish.Finish >= startmonthrangetofinish.Start)
        //                    {
        //                        var dateToCalc = p.PlanSchedule;
        //                        var dateRentang = startmonthrangetofinish;

        //                        var monthlyBuckets = DateRangeToMonth.SplitedDateRangeMonthly(dateToCalc);
        //                        var details = this.CalcDetails(p.CapexSummary, monthlyBuckets);
        //                        p.CapexDetails.AddRange(details);

        //                    }
        //                    else
        //                        throw new Exception("Range Date cannot be Default Date : startmonthrangetofinish");
        //                }
        //                else
        //                    throw new Exception("Range Date cannot be Default Date : startmonthrangetofinish");
        //                #endregion
        //            }

        //        }

        //    }

        //    return p;
        //}
        #endregion

        public List<PCapex> TransformsLSFromBusPlan(BizPlanActivity wa, string activityType, List<BsonDocument> WeisActivities)
        {
            try
            {

                DateTime nowDate = DateTime.Now.Date;

                List<PCapex> result = new List<PCapex>();
                foreach (var t in wa.Phases.Where(x => x.ActivityType.Equals(activityType)))//.Where(x => x.PhSchedule.Start != null && !WellActivity.isHaveWeeklyReport(wa.WellName, wa.UARigSequenceId, x.ActivityType)))
                {
                    PCapex p = new PCapex();
                    p.WellName = wa.WellName;
                    p.RigName = t.Estimate.RigName;
                    p.UARigSequenceId = wa.UARigSequenceId;
                    p.ActivityType = t.ActivityType;
                    //p.CaseName = caseName;
                    //p.UpdateVersion = Tools.ToUTC(nowDate, true);
                    p.FundingType = t.FundingType;
                    var actgh = WeisActivities.FirstOrDefault(x => x.GetString("_id").Equals(p.ActivityType));

                    var actcat = actgh == null ? "" : actgh.GetString("ActivityCategory");
                    p.ActivityCategory = actcat == null ? "" : actcat;

                    if (p.ActivityType.Equals("RIG - IDLE"))
                        p.ActivityCategory = p.ActivityType;

                    if (
                        p.ActivityCategory == "ABANDONMENT" ||
                        p.ActivityCategory == "COMPLETION" ||
                        p.ActivityCategory == "DRILLING" ||
                        p.ActivityCategory == "RIG - IDLE"
                        )
                    {
                    }
                    else
                    {
                        p.ActivityCategory = "";

                    }

                    p.BaseOP = t.Estimate.SaveToOP;
                    p.ProjectName = wa.ProjectName;

                    //p.PhSchedule = t.PhSchedule;
                    p.OP = t.OP;
                    //p.PlanSchedule = t.Estimate.EstimatePeriod; //t.PlanSchedule;
                    p.Plan = t.Plan;
                    //p.UpdateBy = updateBy;
                    p.IsInPlan = wa.isInPlan;// t.IsInPlan;
                    p.FirmOption = wa.isInPlan == true ? "In Plan" : "Not in Plan";
                    p.AssetName = wa.AssetName;
                    //p.MapName = mapName;
                    //p.Allocation = t.Allocation;
                    p.Estimate = t.Estimate;
                    p.LoB = wa.LineOfBusiness;
                    p.RFM = wa.ReferenceFactorModel;

                    p.ShellShare = wa.WorkingInterest;


                    //var qsrfm = new List<IMongoQuery>();
                    //qsrfm.Add(Query.EQ("GroupCase", p.RFM));
                    //qsrfm.Add(Query.EQ("BaseOP", t.Estimate.SaveToOP));
                    //qsrfm.Add(Query.EQ("Country", t.Estimate.Country));

                    //var RefFactorModels = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(Query.And(qsrfm));

                    //p = DetailSummaryLS(p, startmonthrangetofinish, RefFactorModels);
                    //if (p == null)
                    //{
                    //    return null;
                    //}
                    p.PhSchedule = null;
                    p.PlanSchedule = null;
                    p.CapexSummary = null;
                    p.CapexDetails = null;
                    p.Allocation = null;
                    p.Estimate = null;
                    result.Add(p);
                }
                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public PCapex TransformsLSFromBusPlan(BizPlanActivity wa, string mapName, string activityType, string updateBy, string caseName, string ActivityCategory, DateRange startmonthrangetofinish)
        {
            try
            {

                DateTime nowDate = DateTime.Now.Date;

                List<PCapex> result = new List<PCapex>();
                foreach (var t in wa.Phases.Where(x => x.ActivityType.Equals(activityType)))//.Where(x => x.PhSchedule.Start != null && !WellActivity.isHaveWeeklyReport(wa.WellName, wa.UARigSequenceId, x.ActivityType)))
                {


                    #region calculate
                    PCapex p = new PCapex();
                    p.WellName = wa.WellName;
                    p.RigName = wa.RigName;
                    p.UARigSequenceId = wa.UARigSequenceId;
                    p.ActivityType = t.ActivityType;
                    p.CaseName = caseName;
                    //p.UpdateVersion = Tools.ToUTC(nowDate, true);
                    p.FundingType = t.FundingType;
                    p.ActivityCategory = ActivityCategory;
                    p.ShellShare = wa.ShellShare;
                    p.BaseOP = t.Estimate.SaveToOP;
                    p.ProjectName = wa.ProjectName;

                    //p.PhSchedule = t.PhSchedule;
                    p.OP = t.OP;
                    //p.PlanSchedule = t.Estimate.EstimatePeriod; //t.PlanSchedule;
                    p.Plan = t.Plan;
                    //p.UpdateBy = updateBy;
                    p.IsInPlan = wa.isInPlan;// t.IsInPlan;
                    p.FirmOption = wa.isInPlan == true ? "In Plan" : "Not in Plan";
                    p.AssetName = wa.AssetName;
                    //p.MapName = mapName;
                    //p.Allocation = t.Allocation;
                    //p.Estimate = t.Estimate;
                    p.LoB = wa.LineOfBusiness;
                    p.RFM = wa.ReferenceFactorModel;


                    //var qsrfm = new List<IMongoQuery>();
                    //qsrfm.Add(Query.EQ("GroupCase", p.RFM));
                    //qsrfm.Add(Query.EQ("BaseOP", t.Estimate.SaveToOP));
                    //qsrfm.Add(Query.EQ("Country", t.Estimate.Country));

                    //var RefFactorModels = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(Query.And(qsrfm));

                    //p = DetailSummaryLS(p, startmonthrangetofinish, RefFactorModels);
                    //if (p == null)
                    //{
                    //    return null;
                    //}
                    //result.Add(p);
                    p.CapexDetails = null;
                    p.CapexSummary = null;
                    return p;
                    #endregion
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public List<PCapex> TransformsLS(WellActivity wa, string mapName, string activityType, string updateBy, string caseName, DateRange startmonthrangetofinish, bool isAggr, List<BsonDocument> WeisActivities)
        {
            try
            {

                DateTime nowDate = DateTime.Now.Date;

                List<PCapex> result = new List<PCapex>();
                foreach (var t in wa.Phases.Where(x => x.ActivityType.Equals(activityType)))//.Where(x => x.PhSchedule.Start != null && !WellActivity.isHaveWeeklyReport(wa.WellName, wa.UARigSequenceId, x.ActivityType)))
                {
                    //PCapex p = new PCapex();
                    WellName = wa.WellName;
                    RigName = wa.RigName;
                    UARigSequenceId = wa.UARigSequenceId;
                    ActivityType = t.ActivityType;
                    CaseName = caseName;
                    UpdateVersion = Tools.ToUTC(nowDate, true);
                    var actgh = WeisActivities.FirstOrDefault(x => x.GetString("_id").Equals(ActivityType));

                    var actcat = actgh == null ? "" : actgh.GetString("ActivityCategory");
                    ActivityCategory = actcat == null ? "" : actcat;

                    if (ActivityType.Equals("RIG - IDLE"))
                        ActivityCategory = ActivityType;

                    if (
                        ActivityCategory == "ABANDONMENT" ||
                        ActivityCategory == "COMPLETION" ||
                        ActivityCategory == "DRILLING" ||
                        ActivityCategory == "RIG - IDLE"
                        )
                    {
                    }
                    else
                    {
                        ActivityCategory = "";

                    }

                    BaseOP = t.BaseOP == null ? "" : (t.BaseOP.Count <= 0 ? "" : t.BaseOP.OrderByDescending(x => x).FirstOrDefault());
                    ProjectName = wa.ProjectName;
                    FundingType = t.FundingType;
                    PhSchedule = t.PhSchedule;
                    OP = t.OP;
                    PlanSchedule = t.PlanSchedule;
                    Plan = t.Plan;
                    UpdateBy = updateBy;
                    IsInPlan = t.IsInPlan;
                    FirmOption = wa.FirmOrOption;
                    AssetName = wa.AssetName;
                    MapName = mapName;

                    #region marked
                    //p = DetailSummaryLS(p, startmonthrangetofinish);
                    //result.Add(p);

                    //PCapex p = new PCapex();
                    //p.WellName = wa.WellName;
                    //p.RigName = wa.RigName;
                    //p.UARigSequenceId = wa.UARigSequenceId;
                    //p.ActivityType = t.ActivityType;
                    //p.CaseName = caseName;
                    //p.UpdateVersion = Tools.ToUTC(nowDate, true);
                    //var actgh = WeisActivities.FirstOrDefault(x => x.GetString("_id").Equals(p.ActivityType));

                    //var actcat = actgh == null ? "" : actgh.GetString("ActivityCategory");
                    //p.ActivityCategory = actcat == null ? "" : actcat;

                    //if (p.ActivityType.Equals("RIG - IDLE"))
                    //    p.ActivityCategory = p.ActivityType;

                    //if (
                    //    p.ActivityCategory == "ABANDONMENT" ||
                    //    p.ActivityCategory == "COMPLETION" ||
                    //    p.ActivityCategory == "DRILLING" ||
                    //    p.ActivityCategory == "RIG - IDLE"
                    //    )
                    //{
                    //}
                    //else
                    //{
                    //    p.ActivityCategory = "";

                    //}

                    //p.BaseOP = t.BaseOP == null ? "" : (t.BaseOP.Count <= 0 ? "" : t.BaseOP.OrderByDescending(x => x).FirstOrDefault());
                    //p.ProjectName = wa.ProjectName;
                    //p.FundingType = t.FundingType;
                    //p.PhSchedule = t.PhSchedule;
                    //p.OP = t.OP;
                    //p.PlanSchedule = t.PlanSchedule;
                    //p.Plan = t.Plan;
                    //p.UpdateBy = updateBy;
                    //p.IsInPlan = t.IsInPlan;
                    //p.FirmOption = wa.FirmOrOption;
                    //p.AssetName = wa.AssetName;
                    //p.IsAggregate = isAggr;
                    //p.MapName = mapName;

                    //p = DetailSummaryLS(p, startmonthrangetofinish);
                    //result.Add(p);
                    #endregion
                }
                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public PCapex DetailSummaryLS(PCapex p, DateRange startmonthrangetofinish, WEISReferenceFactorModel RefFactorModels)
        {
            try
            {
                if (p.Estimate.SaveToOP != null && p.Estimate.SaveToOP != "")
                {
                    if (p.CapexSummary == null)
                        p.CapexSummary = new PDetail();
                    if (p.CapexDetails == null)
                        p.CapexDetails = new List<PDetails>();
                    p.CapexSummary.Currency = p.Estimate.Currency;
                    
                    #region Summary
                    if (p.Allocation != null)
                    {
                        List<PDetails> details = new List<PDetails>();
                        if (startmonthrangetofinish.Start != Tools.DefaultDate)
                        {
                            if (startmonthrangetofinish.Finish >= startmonthrangetofinish.Start)
                            {
                                foreach (var monthBucket in p.Allocation.MonthlyBuckets)
                                {
                                    if (Convert.ToInt32(monthBucket.Key) < 202201)
                                    {

                                        CapexCalc c = new CapexCalc();
                                        #region new Calcualtion

                                        c.MaterialCost = monthBucket.MaterialCost;
                                        c.RigCost = monthBucket.RigCost;
                                        c.ServicesCost = monthBucket.ServiceCost;

                                        c.lcfCostTangibles = p.Estimate.NewLearningCurveFactor * monthBucket.MaterialCost;
                                        c.lcfCostInTangibles = p.Estimate.NewLearningCurveFactor * (monthBucket.RigCost + monthBucket.ServiceCost + monthBucket.NPTCost);


                                        c.tecoppercent = Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100); // Tools.Div(monthBucket.TECOPCost, monthBucket.TroubleFreeCost);
                                        c.nptpercent = Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100);

                                        //     xTECOP_COST/(xMATERIAL_COST+(xRIG_COST+xSERVICE_COST)*(1/(1-xNPT_PERCENT)))
                                        c.tecoppercent_ = Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100);

                                        //Tools.Div(monthBucket.TECOPCost,
                                        //(monthBucket.MaterialCost + (monthBucket.RigCost + monthBucket.ServiceCost) * (Tools.Div(1, (1 - p.Estimate.NewNPTTime.PercentCost))))
                                        //);


                                        c.tecopmat = (monthBucket.MaterialCost - c.lcfCostTangibles) * c.tecoppercent; //EDM TECOP TANGIBLE
                                        c.tecopIntangDelta = monthBucket.TECOPCost - c.tecopmat; //EDM TECOP INTANGIBLE
                                        c.tecopIntangEquation = c.tecoppercent_ * (
                                                                    (monthBucket.RigCost + monthBucket.ServiceCost) *
                                                                        (Tools.Div(1, (1 - c.nptpercent))) - c.lcfCostInTangibles
                                                                    );

                                        c.EDMTangibles = monthBucket.MaterialCost + c.tecopmat - c.lcfCostTangibles;
                                        c.EDMInTangibles = monthBucket.RigCost + monthBucket.ServiceCost + monthBucket.NPTCost + c.tecopIntangEquation - c.lcfCostInTangibles;

                                        c.escTangibles = monthBucket.EscCostMaterial;
                                        c.escInTangibles = monthBucket.EscCostRig + monthBucket.EscCostServices;


                                        var csollFc = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("CSO Factors"));
                                        double csollFcVal = 0;
                                        if (csollFc.Any())
                                        {
                                            var ess = csollFc.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + monthBucket.Key.Substring(0, 4)));
                                            if (ess.Any())
                                                csollFcVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                                            //if (csollFcVal < 0)
                                            //    csollFcVal = Tools.Div(csollFcVal, 100);
                                        }


                                        //c.CSOPercent = csollFc; // Tools.Div(monthBucket.CSOCost, monthBucket.MeanCostEDM + monthBucket.EscCostMaterial);
                                        //c.CSOPercent = Math.Round(c.CSOPercent, 2);

                                        c.CSOPercent = csollFcVal;

                                        c.CSOCostTangibles = c.CSOPercent * (c.EDMTangibles + c.escTangibles);
                                        c.CSOCostInTangibles = c.CSOPercent * (c.EDMInTangibles + c.escInTangibles);


                                        var infll = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Inflation Factors"));
                                        double infllVal = 0;
                                        if (infll.Any())
                                        {
                                            var ess = infll.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + monthBucket.Key.Substring(0, 4)));
                                            if (ess.Any())
                                                infllVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                                            //if (infllVal < 0)
                                            //    infllVal = Tools.Div(infllVal, 100);
                                        }

                                        c.inflationPercent = infllVal;// Tools.Div(monthBucket.InflationCost, (monthBucket.MeanCostEDM + monthBucket.EscCostTotal + monthBucket.CSOCost));
                                        c.inflationCostTangibles = c.inflationPercent * (c.EDMTangibles + c.escTangibles + c.CSOCostTangibles);
                                        c.inflationCostInTangibles = c.inflationPercent * (c.EDMInTangibles + c.escInTangibles + c.CSOCostInTangibles);

                                        c.modTangibles = c.EDMTangibles + c.escTangibles + c.CSOCostTangibles + c.inflationCostTangibles;
                                        c.modInTangibles = c.EDMInTangibles + c.escInTangibles + c.CSOCostInTangibles + c.inflationCostInTangibles;

                                        c.RTTangibles = Tools.Div(c.modTangibles, (1 + c.inflationPercent));
                                        c.RTInTangibles = Tools.Div(c.modInTangibles, (1 + c.inflationPercent));

                                        c.shellshare = Tools.Div(p.ShellShare, 100);///   //Tools.Div(monthBucket.MeanCostMOD, monthBucket.MeanCostMODSS);

                                        c.EDMTanSS = c.EDMTangibles * c.shellshare;
                                        c.EDMINTANSS = c.EDMInTangibles * c.shellshare;
                                        c.MODTanSS = c.modTangibles * c.shellshare;
                                        c.MODINTANSS = c.modInTangibles * c.shellshare;
                                        c.RTTanSS = c.RTTangibles * c.shellshare;
                                        c.RTINTANSS = c.RTInTangibles * c.shellshare;

                                        c.tecopmatSS = c.tecopmat * c.shellshare; //EDM TANGIBLE SHELL SHARE
                                        c.tecopIntangDeltaSS = c.tecopIntangDelta * c.shellshare; //EDM INTANGIBLE SHELL SHARE


                                        var matllRefModel = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Material Escalation Factors"));
                                        double matllEscVal = 0;
                                        if (matllRefModel.Any())
                                        {
                                            var ess = matllRefModel.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + monthBucket.Key.Substring(0, 4)));
                                            if (ess.Any())
                                                matllEscVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                                        }

                                       
                                    


                                        // learning curve
                                        var lcRefModel = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Learning Curve Factors"));
                                        double lcVal = 0;
                                        if (lcRefModel.Any())
                                        {
                                            var ess = lcRefModel.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + monthBucket.Key.Substring(0, 4)));
                                            if (ess.Any())
                                                lcVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;


                                        }

                                        c.TECOPServices = monthBucket.ServiceCost * c.tecoppercent * (1 - lcVal) * Tools.Div(1, (1 - c.nptpercent));
                                        c.MODServices = c.TECOPServices * (1 + matllEscVal) * (1 + csollFcVal) * (1 + infllVal);
                                        c.TECOPRig = monthBucket.RigCost * c.tecoppercent * (1 - lcVal) * Tools.Div(1, (1 - c.nptpercent));
                                        c.EscPercentRig = Tools.Div(monthBucket.EscCostRig,
                                            monthBucket.RigCost * Tools.Div(1, (1 - c.nptpercent)) * (1 + c.tecoppercent) * (1 - lcVal)
                                            );
                                        c.MODRig = c.TECOPRig * (1 + c.EscPercentRig) * (1 + csollFcVal) * (1 + infllVal);

                                        //Opcosts Idle Rig
                                        c.EDMCost = monthBucket.MeanCostEDM;
                                        c.EDMCostSS = c.EDMCost * c.shellshare;

                                        c.MODCost = monthBucket.MeanCostMOD;
                                        c.MODCostSS = c.MODCost * c.shellshare;

                                        c.RTCost = monthBucket.MeanCostRealTerm;
                                        c.RTCostSS = c.RTCost * c.shellshare;

                                        c.modtecopmat = c.tecopmat * (1 + matllEscVal) * (1 + csollFcVal) * (1 + infllVal); //TANGIBLE
                                        c.modtecopIntangDelta = c.MODRig + c.MODServices;  //c.tecopmat - c.modtecopmat; //INTANGIBLE

                                        c.modtecopmatSS = c.modtecopmat * c.shellshare;  //TANGIBLE SHELL SHARE
                                        c.modtecopIntangDeltaSS = c.modtecopIntangDelta * c.shellshare; //INTANGIBLE SHELL SHARE

                                        c.rttecopmat = Tools.Div(c.modtecopmat, (1 + infllVal)); //TANGIBLE
                                        c.rttecopIntangDelta = c.RTTangibles - c.rttecopmat; // c.modtecopmat - c.rttecopmat; //INTANGIBLE

                                        c.rttecopmatSS = c.rttecopmat * c.shellshare;  //TANGIBLE SHELL SHARE
                                        c.rttecopIntangDeltaSS = c.rttecopIntangDelta * c.shellshare; //INTANGIBLE SHELL SHARE

                                        #endregion

                                        var detail = SplitDetails(p, Convert.ToInt32(monthBucket.Key), "Monthly", c);
                                        detail.Calc = c;
                                        details.Add(detail);
                                    }
                                }

                                foreach (var annualBucket in p.Allocation.AnnualyBuckets)
                                {
                                    if (Convert.ToInt32(annualBucket.Key) > 2021)
                                    {
                                        CapexCalc c = new CapexCalc();
                                        #region new Calcualtion

                                        c.MaterialCost = annualBucket.MaterialCost;
                                        c.RigCost = annualBucket.RigCost;
                                        c.ServicesCost = annualBucket.ServiceCost;

                                        c.lcfCostTangibles = p.Estimate.NewLearningCurveFactor * annualBucket.MaterialCost;
                                        c.lcfCostInTangibles = p.Estimate.NewLearningCurveFactor * (annualBucket.RigCost + annualBucket.ServiceCost + annualBucket.NPTCost);


                                        c.tecoppercent = Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100); // Tools.Div(monthBucket.TECOPCost, monthBucket.TroubleFreeCost);
                                        c.nptpercent = Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100);

                                        //     xTECOP_COST/(xMATERIAL_COST+(xRIG_COST+xSERVICE_COST)*(1/(1-xNPT_PERCENT)))
                                        c.tecoppercent_ = Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100);



                                        c.tecopmat = (annualBucket.MaterialCost - c.lcfCostTangibles) * c.tecoppercent;
                                        c.tecopIntangDelta = annualBucket.TECOPCost - c.tecopmat;
                                        c.tecopIntangEquation = c.tecoppercent_ * (
                                                                    (annualBucket.RigCost + annualBucket.ServiceCost) *
                                                                        (Tools.Div(1, (1 - c.nptpercent))) - c.lcfCostInTangibles
                                                                    );

                                        c.EDMTangibles = annualBucket.MaterialCost + c.tecopmat - c.lcfCostTangibles;
                                        c.EDMInTangibles = annualBucket.RigCost + annualBucket.ServiceCost + annualBucket.NPTCost + c.tecopIntangEquation - c.lcfCostInTangibles;

                                        c.escTangibles = annualBucket.EscCostMaterial;
                                        c.escInTangibles = annualBucket.EscCostRig + annualBucket.EscCostServices;

                                        var csollFc = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("CSO Factors"));
                                        double csollFcVal = 0;
                                        if (csollFc.Any())
                                        {
                                            var ess = csollFc.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + annualBucket.Key));
                                            if (ess.Any())
                                                csollFcVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                                            //if (csollFcVal < 0)
                                            //    csollFcVal = Tools.Div(csollFcVal, 100);
                                        }

                                        //c.CSOPercent = Tools.Div(annualBucket.CSOCost, annualBucket.MeanCostEDM + annualBucket.EscCostMaterial);
                                        //c.CSOPercent = Math.Round(c.CSOPercent, 2);

                                        c.CSOPercent = csollFcVal;

                                        c.CSOCostTangibles = c.CSOPercent * (c.EDMTangibles + c.escTangibles);
                                        c.CSOCostInTangibles = c.CSOPercent * (c.EDMInTangibles + c.escInTangibles);

                                        var infll = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Inflation Factors"));
                                        double infllVal = 0;
                                        if (infll.Any())
                                        {
                                            var ess = infll.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + annualBucket.Key));
                                            if (ess.Any())
                                                infllVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                                            //if (infllVal < 0)
                                            //    infllVal = Tools.Div(infllVal, 100);
                                        }

                                        c.inflationPercent = infllVal; // Tools.Div(annualBucket.InflationCost, (annualBucket.MeanCostEDM + annualBucket.EscCostTotal + annualBucket.CSOCost));
                                        c.inflationCostTangibles = c.inflationPercent * (c.EDMTangibles + c.escTangibles + c.CSOCostTangibles);
                                        c.inflationCostInTangibles = c.inflationPercent * (c.EDMInTangibles + c.escInTangibles + c.CSOCostInTangibles);

                                        c.modTangibles = c.EDMTangibles + c.escTangibles + c.CSOCostTangibles + c.inflationCostTangibles;
                                        c.modInTangibles = c.EDMInTangibles + c.escInTangibles + c.CSOCostInTangibles + c.inflationCostInTangibles;

                                        c.RTTangibles = Tools.Div(c.modTangibles, (1 + c.inflationPercent));
                                        c.RTInTangibles = Tools.Div(c.modInTangibles, (1 + c.inflationPercent));

                                        c.shellshare = Tools.Div(p.ShellShare, 100);/// 

                                        c.EDMTanSS = c.EDMTangibles * c.shellshare;
                                        c.EDMINTANSS = c.EDMInTangibles * c.shellshare;
                                        c.MODTanSS = c.modTangibles * c.shellshare;
                                        c.MODINTANSS = c.modInTangibles * c.shellshare;
                                        c.RTTanSS = c.RTTangibles * c.shellshare;
                                        c.RTINTANSS = c.RTInTangibles * c.shellshare;

                                        c.tecopmatSS = c.tecopmat * c.shellshare;
                                        c.tecopIntangDeltaSS = c.tecopIntangDelta * c.shellshare;



                                        var matllRefModel = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Material Escalation Factors"));
                                        double matllEscVal = 0;
                                        if (matllRefModel.Any())
                                        {
                                            var ess = matllRefModel.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + annualBucket.Key));
                                            if (ess.Any())
                                                matllEscVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                                        }

                                     

                                       

                                        // learning curve
                                        var lcRefModel = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Learning Curve Factors"));
                                        double lcVal = 0;
                                        if (lcRefModel.Any())
                                        {
                                            var ess = lcRefModel.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + annualBucket.Key.Substring(0, 4)));
                                            if (ess.Any())
                                                lcVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;


                                        }

                                        c.TECOPServices = annualBucket.ServiceCost * c.tecoppercent * (1 - lcVal) * Tools.Div(1, (1 - c.nptpercent));
                                        c.MODServices = c.TECOPServices * (1 + matllEscVal) * (1 + csollFcVal) * (1 + infllVal);
                                        c.TECOPRig = annualBucket.RigCost * c.tecoppercent * (1 - lcVal) * Tools.Div(1, (1 - c.nptpercent));
                                        c.EscPercentRig = Tools.Div(annualBucket.EscCostRig,
                                            annualBucket.RigCost * Tools.Div(1, (1 - c.nptpercent)) * (1 + c.tecoppercent) * (1 - lcVal)
                                            );
                                        c.MODRig = c.TECOPRig * (1 + c.EscPercentRig) * (1 + csollFcVal) * (1 + infllVal);

                                        //Opcosts Idle Rig
                                        c.EDMCost = annualBucket.MeanCostEDM;
                                        c.EDMCostSS = c.EDMCost * c.shellshare;

                                        c.MODCost = annualBucket.MeanCostMOD;
                                        c.MODCostSS = c.MODCost * c.shellshare;

                                        c.RTCost = annualBucket.MeanCostRealTerm;
                                        c.RTCostSS = c.RTCost * c.shellshare;

                                        c.modtecopmat = c.tecopmat * (1 + matllEscVal) * (1 + csollFcVal) * (1 + infllVal); //TANGIBLE
                                        c.modtecopIntangDelta = c.MODRig + c.MODServices;  //c.tecopmat - c.modtecopmat; //INTANGIBLE

                                        c.modtecopmatSS = c.modtecopmat * c.shellshare;  //TANGIBLE SHELL SHARE
                                        c.modtecopIntangDeltaSS = c.modtecopIntangDelta * c.shellshare; //INTANGIBLE SHELL SHARE

                                        c.rttecopmat = Tools.Div(c.modtecopmat, (1 + infllVal)); //TANGIBLE
                                        c.rttecopIntangDelta = c.RTTangibles - c.rttecopmat; // c.modtecopmat - c.rttecopmat; //INTANGIBLE

                                        c.rttecopmatSS = c.rttecopmat * c.shellshare;  //TANGIBLE SHELL SHARE
                                        c.rttecopIntangDeltaSS = c.rttecopIntangDelta * c.shellshare; //INTANGIBLE SHELL SHARE

                                        #endregion
                                        var detail = SplitDetails(p, Convert.ToInt32(annualBucket.Key), "Annual", c);
                                        detail.Calc = c;
                                        details.Add(detail);
                                    }
                                }
                                p.CapexDetails = details;
                            }
                        }
                        else
                        {
                            throw new Exception("Range Date cannot be Default Date : startmonthrangetofinish");
                        }
                    }
                    else
                    {
                        throw new Exception("Range Date cannot be Default Date : startmonthrangetofinish");
                    }
                    #endregion
                }

                return p;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public PDetails SplitDetails(PCapex p, int monthOrAnnualId, string type,
           CapexCalc c)
        {

            if (monthOrAnnualId == 201907)
            {

            }

            PDetails detail = new PDetails();
            if (p.FundingType != null && (p.ActivityCategory.Equals("DRILLING") && p.FundingType.Equals("C2E")))
            {
                #region 1st Pair (Expex Drilling B2 Expl Tangible & Expex Drilling B2 Expl Intangible)
                //EDM
                detail.EPEXDrillingB2ExplTang.EDM = c.EDMTangibles;
                detail.EPEXDrillingB2ExplInTang.EDM = c.EDMInTangibles;
                detail.EPEXDrillingB2ExplTang.EDMSS = c.EDMTanSS;
                detail.EPEXDrillingB2ExplInTang.EDMSS = c.EDMINTANSS;

                //MOD : MOD Total Materials = EDM Total Materials + CSO Total Materials + Escalation Total Materials
                detail.EPEXDrillingB2ExplTang.MOD = c.modTangibles;
                detail.EPEXDrillingB2ExplInTang.MOD = c.modInTangibles;
                detail.EPEXDrillingB2ExplTang.MODSS = c.MODTanSS;
                detail.EPEXDrillingB2ExplInTang.MODSS = c.MODINTANSS;

                //RT
                detail.EPEXDrillingB2ExplTang.RealTerm = c.RTTangibles;
                detail.EPEXDrillingB2ExplInTang.RealTerm = c.RTInTangibles;
                detail.EPEXDrillingB2ExplTang.RealTermSS = c.RTTanSS;
                detail.EPEXDrillingB2ExplInTang.RealTermSS = c.RTINTANSS;


                #endregion
            }
            if (p.FundingType != null && (p.ActivityCategory.Equals("COMPLETION") && p.FundingType.Equals("C2E")))
            {
                #region 2nd Pair (Expex Completion B2 Expl Tangible & Expex Completion B2 Expl Intangible)
                //EDM
                detail.EPEXCompletionB2ExplTang.EDM = c.EDMTangibles;
                detail.EPEXCompletionB2ExplInTang.EDM = c.EDMInTangibles;
                detail.EPEXCompletionB2ExplTang.EDMSS = c.EDMTanSS;
                detail.EPEXCompletionB2ExplInTang.EDMSS = c.EDMINTANSS;

                //MOD : MOD Total Materials = EDM Total Materials + CSO Total Materials + Escalation Total Materials
                detail.EPEXCompletionB2ExplTang.MOD = c.modTangibles;
                detail.EPEXCompletionB2ExplInTang.MOD = c.modInTangibles;
                detail.EPEXCompletionB2ExplTang.MODSS = c.MODTanSS;
                detail.EPEXCompletionB2ExplInTang.MODSS = c.MODINTANSS;

                //RT
                detail.EPEXCompletionB2ExplTang.RealTerm = c.RTTangibles;
                detail.EPEXCompletionB2ExplInTang.RealTerm = c.RTInTangibles;
                detail.EPEXCompletionB2ExplTang.RealTermSS = c.RTTanSS;
                detail.EPEXCompletionB2ExplInTang.RealTermSS = c.RTINTANSS;
                # endregion
            }
            if (p.FundingType != null && (p.ActivityCategory.Equals("DRILLING") && p.FundingType.Equals("CAPEX")))
            {
                #region 3th Pair (Capital Drilling PD Dev Tangible & Capital Drilling PD Dev Intangible)
                //EDM
                detail.CapitalDrillingPDDevTang.EDM = c.EDMTangibles;
                detail.CapitalDrillingPDDevInTang.EDM = c.EDMInTangibles;
                detail.CapitalDrillingPDDevTang.EDMSS = c.EDMTanSS;
                detail.CapitalDrillingPDDevInTang.EDMSS = c.EDMINTANSS;

                //MOD : MOD Total Materials = EDM Total Materials + CSO Total Materials + Escalation Total Materials
                detail.CapitalDrillingPDDevTang.MOD = c.modTangibles;
                detail.CapitalDrillingPDDevInTang.MOD = c.modInTangibles;
                detail.CapitalDrillingPDDevTang.MODSS = c.MODTanSS;
                detail.CapitalDrillingPDDevInTang.MODSS = c.MODINTANSS;

                //RT
                detail.CapitalDrillingPDDevTang.RealTerm = c.RTTangibles;
                detail.CapitalDrillingPDDevInTang.RealTerm = c.RTInTangibles;
                detail.CapitalDrillingPDDevTang.RealTermSS = c.RTTanSS;
                detail.CapitalDrillingPDDevInTang.RealTermSS = c.RTINTANSS;
                #endregion
            }
            if (p.FundingType != null && (p.ActivityCategory.Equals("COMPLETION") && p.FundingType.Equals("CAPEX")))
            {
                #region 4th Pair (Capital Completion PD Dev Tangible & Capital Completion PD Dev Intangible)
                //EDM
                detail.CapitalCompletionPDDevTang.EDM = c.EDMTangibles;
                detail.CapitalCompletionPDDevInTang.EDM = c.EDMInTangibles;
                detail.CapitalCompletionPDDevTang.EDMSS = c.EDMTanSS;
                detail.CapitalCompletionPDDevInTang.EDMSS = c.EDMINTANSS;

                //MOD : MOD Total Materials = EDM Total Materials + CSO Total Materials + Escalation Total Materials
                detail.CapitalCompletionPDDevTang.MOD = c.modTangibles;
                detail.CapitalCompletionPDDevInTang.MOD = c.modInTangibles;
                detail.CapitalCompletionPDDevTang.MODSS = c.MODTanSS;
                detail.CapitalCompletionPDDevInTang.MODSS = c.MODINTANSS;

                //RT
                detail.CapitalCompletionPDDevTang.RealTerm = c.RTTangibles;
                detail.CapitalCompletionPDDevInTang.RealTerm = c.RTInTangibles;
                detail.CapitalCompletionPDDevTang.RealTermSS = c.RTTanSS;
                detail.CapitalCompletionPDDevInTang.RealTermSS = c.RTINTANSS;
                #endregion
            }
            if (p.FundingType != null && (p.ActivityCategory.Equals("ABANDONMENT") && p.FundingType.Equals("ABEX")))
            {
                #region 5th Pair (Capital Expenditure D&R DVA Wells & Capital Expenditure D&R Subsea Wells)
                //EDM
                detail.CapitalExpenDRDVAWells.EDM = c.EDMTangibles;
                detail.CapitalExpenDRSubSeaWells.EDM = c.EDMInTangibles;
                detail.CapitalExpenDRDVAWells.EDMSS = c.EDMTanSS;
                detail.CapitalExpenDRSubSeaWells.EDMSS = c.EDMINTANSS;

                //MOD : MOD Total Materials = EDM Total Materials + CSO Total Materials + Escalation Total Materials
                detail.CapitalExpenDRDVAWells.MOD = c.modTangibles;
                detail.CapitalExpenDRSubSeaWells.MOD = c.modInTangibles;
                detail.CapitalExpenDRDVAWells.MODSS = c.MODTanSS;
                detail.CapitalExpenDRSubSeaWells.MODSS = c.MODINTANSS;

                //RT
                detail.CapitalExpenDRDVAWells.RealTerm = c.RTTangibles;
                detail.CapitalExpenDRSubSeaWells.RealTerm = c.RTInTangibles;
                detail.CapitalExpenDRDVAWells.RealTermSS = c.RTTanSS;
                detail.CapitalExpenDRSubSeaWells.RealTermSS = c.RTINTANSS;
                #endregion
            }
            if (p.FundingType != null && (p.ActivityType.ToUpper().Contains("RIG - IDLE")) && p.FundingType.Equals("OPEX"))
            {
                #region Opcosts Idle Rig


                //EDM
                detail.OPCostIdleRig.EDM = c.EDMCost;
                detail.OPCostIdleRig.MOD = c.MODCost;
                detail.OPCostIdleRig.RealTerm = c.RTCost;

                detail.OPCostIdleRig.EDMSS = c.EDMCostSS;
                detail.OPCostIdleRig.MODSS = c.MODCostSS;
                detail.OPCostIdleRig.RealTermSS = c.RTCostSS;
                #endregion
            }
            if (p.FundingType != null && (p.FundingType.Equals("CAPEX") || p.FundingType.Equals("C2E")))
            {
                #region 6th Pair (Contingency Tangible Wells & Contingency Intangible Wells)
                //EDM
                detail.ContigencyTangWells.EDM = c.tecopmat;
                detail.ContigencyInTangWells.EDM = c.tecopIntangDelta;
                detail.ContigencyTangWells.EDMSS = c.tecopmatSS;
                detail.ContigencyInTangWells.EDMSS = c.tecopIntangDeltaSS;

                //MOD : MOD Total Materials = EDM Total Materials + CSO Total Materials + Escalation Total Materials
                detail.ContigencyTangWells.MOD = c.modtecopmat;
                detail.ContigencyInTangWells.MOD = c.modtecopIntangDelta;
                detail.ContigencyTangWells.MODSS = c.modtecopmatSS;
                detail.ContigencyInTangWells.MODSS = c.modtecopIntangDeltaSS;

                //RT
                detail.ContigencyTangWells.RealTerm = Tools.Div(detail.ContigencyTangWells.MOD, (1 + c.inflationPercent));
                detail.ContigencyInTangWells.RealTerm = Tools.Div(detail.ContigencyInTangWells.MOD, (1 + c.inflationPercent));
                detail.ContigencyTangWells.RealTermSS = Tools.Div(detail.ContigencyTangWells.MODSS, (1 + c.inflationPercent));
                detail.ContigencyInTangWells.RealTermSS = Tools.Div(detail.ContigencyInTangWells.MODSS, (1 + c.inflationPercent)); 
                #endregion
            }
            detail.Type = type;
            detail.DateId = monthOrAnnualId;
            detail.Currency = p.CapexSummary.Currency;
            return detail;
        }
        
        public List<PCapex> ReSyncMapFromBizplan(List<PCapex> list, string mapnames)
        {

            try
            {
                var bizplans = BizPlanActivity.GetAll2(Query.Null);
                List<PCapex> liPcapex = new List<PCapex>();
                foreach (var l in list)
                {
                    var getEst = bizplans.Where(x => x.WellName != null && x.WellName == l.WellName
                                && x.UARigSequenceId != null && x.UARigSequenceId == l.UARigSequenceId
                                && x.Phases != null && x.Phases.Where(d => d.ActivityType != null && d.ActivityType == l.ActivityType).Count() > 0
                        ).FirstOrDefault();
                    if (getEst != null)
                    {
                        var dr = new DateRange(new DateTime(2015, 1, 1), new DateTime(2040, 12, 31));
                        var phase = getEst.Phases.Where(x => x.ActivityType != null && x.ActivityType == l.ActivityType).FirstOrDefault();
                        l.Estimate = phase.Estimate;
                        l.Allocation = phase.Allocation;
                        l.UpdateBy = l.UpdateBy;// WebTools.LoginUser.UserName;
                        l.MapName = mapnames;

                        var qsrfm = new List<IMongoQuery>();
                        qsrfm.Add(Query.EQ("GroupCase", l.RFM));
                        qsrfm.Add(Query.EQ("BaseOP", phase.Estimate.SaveToOP));
                        qsrfm.Add(Query.EQ("Country", phase.Estimate.Country == "" ? getEst.Country : phase.Estimate.Country));

                        var RefFactorModels = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(Query.And(qsrfm));
                        //PDetail detail = new PDetail();
                        //l.CapexSummary = detail;
                        var aResult = l.DetailSummaryLS(l, dr, RefFactorModels);
                        if (aResult != null)
                        {
                            aResult.Save();
                            liPcapex.Add(aResult);
                        }
                    }

                }
                return liPcapex;
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        public bool AggregatingPCapex(List<PCapex> capex, string mapname, string updateby, DateTime createDate, int monthIdStart = 201601, int monthIdFinish = 206612, int onlyYearValueStart = 202201)
        {
            var grp = capex.GroupBy(x => x.CaseName);
            foreach (var t in grp)
            {
                var data = t.ToList().SelectMany(x => x.CapexDetails);
                // process monthly 
                if (monthIdStart == 202201)
                {
                    monthIdStart = 201601;
                }

                PCapexAgg ag = new PCapexAgg();
                ag.CaseName = t.Key;
                ag.UpdateVersion = createDate;
                ag.UpdateBy = updateby;
                ag.MapName = mapname;

                List<PDetails> ld = new List<PDetails>();
                do
                {
                    #region binding the dta
                    var datainclude = data.Where(x => x.DateId == monthIdStart).ToList();
                    if (datainclude.Any())
                    {

                        PDetails d = new PDetails();

                        ///EDM
                        #region EDM
                        d.CapitalCompletionPDDevInTang.EDM = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.EDM);
                        d.CapitalCompletionPDDevTang.EDM = datainclude.Sum(x => x.CapitalCompletionPDDevTang.EDM);
                        d.CapitalCompletionPDDevInTang.EDMSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.EDMSS);
                        d.CapitalCompletionPDDevTang.EDMSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.EDMSS);

                        d.CapitalDrillingPDDevInTang.EDM = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.EDM);
                        d.CapitalDrillingPDDevTang.EDM = datainclude.Sum(x => x.CapitalDrillingPDDevTang.EDM);
                        d.CapitalDrillingPDDevInTang.EDMSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.EDMSS);
                        d.CapitalDrillingPDDevTang.EDMSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.EDMSS);

                        d.CapitalExpenDRDVAWells.EDM = datainclude.Sum(x => x.CapitalExpenDRDVAWells.EDM);
                        d.CapitalExpenDRSubSeaWells.EDM = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.EDM);
                        d.CapitalExpenDRDVAWells.EDMSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.EDMSS);
                        d.CapitalExpenDRSubSeaWells.EDMSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.EDMSS);

                        d.ContigencyInTangWells.EDM = datainclude.Sum(x => x.ContigencyInTangWells.EDM);
                        d.ContigencyTangWells.EDM = datainclude.Sum(x => x.ContigencyTangWells.EDM);
                        d.ContigencyInTangWells.EDMSS = datainclude.Sum(x => x.ContigencyInTangWells.EDMSS);
                        d.ContigencyTangWells.EDMSS = datainclude.Sum(x => x.ContigencyTangWells.EDMSS);

                        d.EPEXCompletionB2ExplInTang.EDM = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.EDM);
                        d.EPEXCompletionB2ExplTang.EDM = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.EDM);
                        d.EPEXCompletionB2ExplInTang.EDMSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.EDMSS);
                        d.EPEXCompletionB2ExplTang.EDMSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.EDMSS);

                        d.EPEXDrillingB2ExplInTang.EDM = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.EDM);
                        d.EPEXDrillingB2ExplTang.EDM = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.EDM);
                        d.EPEXDrillingB2ExplInTang.EDMSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.EDMSS);
                        d.EPEXDrillingB2ExplTang.EDMSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.EDMSS);

                        d.OPCostIdleRig.EDM = datainclude.Sum(x => x.OPCostIdleRig.EDM);
                        d.OPCostIdleRig.EDMSS = datainclude.Sum(x => x.OPCostIdleRig.EDMSS);
                        #endregion EDM

                        ///MOD
                        #region MOD
                        d.CapitalCompletionPDDevInTang.MOD = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.MOD);
                        d.CapitalCompletionPDDevTang.MOD = datainclude.Sum(x => x.CapitalCompletionPDDevTang.MOD);
                        d.CapitalCompletionPDDevInTang.MODSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.MODSS);
                        d.CapitalCompletionPDDevTang.MODSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.MODSS);

                        d.CapitalDrillingPDDevInTang.MOD = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.MOD);
                        d.CapitalDrillingPDDevTang.MOD = datainclude.Sum(x => x.CapitalDrillingPDDevTang.MOD);
                        d.CapitalDrillingPDDevInTang.MODSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.MODSS);
                        d.CapitalDrillingPDDevTang.MODSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.MODSS);

                        d.CapitalExpenDRDVAWells.MOD = datainclude.Sum(x => x.CapitalExpenDRDVAWells.MOD);
                        d.CapitalExpenDRSubSeaWells.MOD = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.MOD);
                        d.CapitalExpenDRDVAWells.MODSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.MODSS);
                        d.CapitalExpenDRSubSeaWells.MODSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.MODSS);

                        d.ContigencyInTangWells.MOD = datainclude.Sum(x => x.ContigencyInTangWells.MOD);
                        d.ContigencyTangWells.MOD = datainclude.Sum(x => x.ContigencyTangWells.MOD);
                        d.ContigencyInTangWells.MODSS = datainclude.Sum(x => x.ContigencyInTangWells.MODSS);
                        d.ContigencyTangWells.MODSS = datainclude.Sum(x => x.ContigencyTangWells.MODSS);

                        d.EPEXCompletionB2ExplInTang.MOD = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.MOD);
                        d.EPEXCompletionB2ExplTang.MOD = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.MOD);
                        d.EPEXCompletionB2ExplInTang.MODSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.MODSS);
                        d.EPEXCompletionB2ExplTang.MODSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.MODSS);

                        d.EPEXDrillingB2ExplInTang.MOD = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.MOD);
                        d.EPEXDrillingB2ExplTang.MOD = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.MOD);
                        d.EPEXDrillingB2ExplInTang.MODSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.MODSS);
                        d.EPEXDrillingB2ExplTang.MODSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.MODSS);

                        d.OPCostIdleRig.MOD = datainclude.Sum(x => x.OPCostIdleRig.MOD);
                        d.OPCostIdleRig.MODSS = datainclude.Sum(x => x.OPCostIdleRig.MODSS);
                        #endregion MOD

                        ///RT
                        #region RT
                        d.CapitalCompletionPDDevInTang.RealTerm = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.RealTerm);
                        d.CapitalCompletionPDDevTang.RealTerm = datainclude.Sum(x => x.CapitalCompletionPDDevTang.RealTerm);
                        d.CapitalCompletionPDDevInTang.RealTermSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.RealTermSS);
                        d.CapitalCompletionPDDevTang.RealTermSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.RealTermSS);

                        d.CapitalDrillingPDDevInTang.RealTerm = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.RealTerm);
                        d.CapitalDrillingPDDevTang.RealTerm = datainclude.Sum(x => x.CapitalDrillingPDDevTang.RealTerm);
                        d.CapitalDrillingPDDevInTang.RealTermSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.RealTermSS);
                        d.CapitalDrillingPDDevTang.RealTermSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.RealTermSS);

                        d.CapitalExpenDRDVAWells.RealTerm = datainclude.Sum(x => x.CapitalExpenDRDVAWells.RealTerm);
                        d.CapitalExpenDRSubSeaWells.RealTerm = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.RealTerm);
                        d.CapitalExpenDRDVAWells.RealTermSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.RealTermSS);
                        d.CapitalExpenDRSubSeaWells.RealTermSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.RealTermSS);

                        d.ContigencyInTangWells.RealTerm = datainclude.Sum(x => x.ContigencyInTangWells.RealTerm);
                        d.ContigencyTangWells.RealTerm = datainclude.Sum(x => x.ContigencyTangWells.RealTerm);
                        d.ContigencyInTangWells.RealTermSS = datainclude.Sum(x => x.ContigencyInTangWells.RealTermSS);
                        d.ContigencyTangWells.RealTermSS = datainclude.Sum(x => x.ContigencyTangWells.RealTermSS);

                        d.EPEXCompletionB2ExplInTang.RealTerm = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.RealTerm);
                        d.EPEXCompletionB2ExplTang.RealTerm = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.RealTerm);
                        d.EPEXCompletionB2ExplInTang.RealTermSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.RealTermSS);
                        d.EPEXCompletionB2ExplTang.RealTermSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.RealTermSS);

                        d.EPEXDrillingB2ExplInTang.RealTerm = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.RealTerm);
                        d.EPEXDrillingB2ExplTang.RealTerm = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.RealTerm);
                        d.EPEXDrillingB2ExplInTang.RealTermSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.RealTermSS);
                        d.EPEXDrillingB2ExplTang.RealTermSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.RealTermSS);

                        d.OPCostIdleRig.RealTerm = datainclude.Sum(x => x.OPCostIdleRig.RealTerm);
                        d.OPCostIdleRig.RealTermSS = datainclude.Sum(x => x.OPCostIdleRig.RealTermSS);
                        #endregion MOD

                        d.Currency = datainclude.FirstOrDefault().Currency;
                        d.DateId = monthIdStart;
                        d.Type = "Monthly";
                        ld.Add(d);
                    }
                    else
                    {
                        PDetails d = new PDetails();
                        d.DateId = monthIdStart;
                        d.Type = "Monthly";
                        ld.Add(d);

                    }
                    #endregion
                    monthIdStart = AddMonthId(monthIdStart, 1);
                } while (monthIdStart <= monthIdFinish && (monthIdStart != onlyYearValueStart));

                var yearonlystart = Convert.ToInt32(onlyYearValueStart);
                var yearonlyfinish = Convert.ToInt32(monthIdFinish);
                do
                {
                    #region binding the dta
                    var datainclude = data.Where(x => Convert.ToInt32(x.DateId.ToString().Substring(0, 4)) == Convert.ToInt32(yearonlystart.ToString().Substring(0, 4))).ToList();
                    if (datainclude.Any())
                    {
                        PDetails d = new PDetails();

                        ///EDM
                        #region EDM
                        d.CapitalCompletionPDDevInTang.EDM = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.EDM);
                        d.CapitalCompletionPDDevTang.EDM = datainclude.Sum(x => x.CapitalCompletionPDDevTang.EDM);
                        d.CapitalCompletionPDDevInTang.EDMSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.EDMSS);
                        d.CapitalCompletionPDDevTang.EDMSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.EDMSS);

                        d.CapitalDrillingPDDevInTang.EDM = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.EDM);
                        d.CapitalDrillingPDDevTang.EDM = datainclude.Sum(x => x.CapitalDrillingPDDevTang.EDM);
                        d.CapitalDrillingPDDevInTang.EDMSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.EDMSS);
                        d.CapitalDrillingPDDevTang.EDMSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.EDMSS);

                        d.CapitalExpenDRDVAWells.EDM = datainclude.Sum(x => x.CapitalExpenDRDVAWells.EDM);
                        d.CapitalExpenDRSubSeaWells.EDM = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.EDM);
                        d.CapitalExpenDRDVAWells.EDMSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.EDMSS);
                        d.CapitalExpenDRSubSeaWells.EDMSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.EDMSS);

                        d.ContigencyInTangWells.EDM = datainclude.Sum(x => x.ContigencyInTangWells.EDM);
                        d.ContigencyTangWells.EDM = datainclude.Sum(x => x.ContigencyTangWells.EDM);
                        d.ContigencyInTangWells.EDMSS = datainclude.Sum(x => x.ContigencyInTangWells.EDMSS);
                        d.ContigencyTangWells.EDMSS = datainclude.Sum(x => x.ContigencyTangWells.EDMSS);

                        d.EPEXCompletionB2ExplInTang.EDM = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.EDM);
                        d.EPEXCompletionB2ExplTang.EDM = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.EDM);
                        d.EPEXCompletionB2ExplInTang.EDMSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.EDMSS);
                        d.EPEXCompletionB2ExplTang.EDMSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.EDMSS);

                        d.EPEXDrillingB2ExplInTang.EDM = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.EDM);
                        d.EPEXDrillingB2ExplTang.EDM = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.EDM);
                        d.EPEXDrillingB2ExplInTang.EDMSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.EDMSS);
                        d.EPEXDrillingB2ExplTang.EDMSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.EDMSS);

                        d.OPCostIdleRig.EDM = datainclude.Sum(x => x.OPCostIdleRig.EDM);
                        d.OPCostIdleRig.EDMSS = datainclude.Sum(x => x.OPCostIdleRig.EDMSS);
                        #endregion EDM

                        ///MOD
                        #region MOD
                        d.CapitalCompletionPDDevInTang.MOD = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.MOD);
                        d.CapitalCompletionPDDevTang.MOD = datainclude.Sum(x => x.CapitalCompletionPDDevTang.MOD);
                        d.CapitalCompletionPDDevInTang.MODSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.MODSS);
                        d.CapitalCompletionPDDevTang.MODSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.MODSS);

                        d.CapitalDrillingPDDevInTang.MOD = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.MOD);
                        d.CapitalDrillingPDDevTang.MOD = datainclude.Sum(x => x.CapitalDrillingPDDevTang.MOD);
                        d.CapitalDrillingPDDevInTang.MODSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.MODSS);
                        d.CapitalDrillingPDDevTang.MODSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.MODSS);

                        d.CapitalExpenDRDVAWells.MOD = datainclude.Sum(x => x.CapitalExpenDRDVAWells.MOD);
                        d.CapitalExpenDRSubSeaWells.MOD = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.MOD);
                        d.CapitalExpenDRDVAWells.MODSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.MODSS);
                        d.CapitalExpenDRSubSeaWells.MODSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.MODSS);

                        d.ContigencyInTangWells.MOD = datainclude.Sum(x => x.ContigencyInTangWells.MOD);
                        d.ContigencyTangWells.MOD = datainclude.Sum(x => x.ContigencyTangWells.MOD);
                        d.ContigencyInTangWells.MODSS = datainclude.Sum(x => x.ContigencyInTangWells.MODSS);
                        d.ContigencyTangWells.MODSS = datainclude.Sum(x => x.ContigencyTangWells.MODSS);

                        d.EPEXCompletionB2ExplInTang.MOD = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.MOD);
                        d.EPEXCompletionB2ExplTang.MOD = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.MOD);
                        d.EPEXCompletionB2ExplInTang.MODSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.MODSS);
                        d.EPEXCompletionB2ExplTang.MODSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.MODSS);

                        d.EPEXDrillingB2ExplInTang.MOD = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.MOD);
                        d.EPEXDrillingB2ExplTang.MOD = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.MOD);
                        d.EPEXDrillingB2ExplInTang.MODSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.MODSS);
                        d.EPEXDrillingB2ExplTang.MODSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.MODSS);

                        d.OPCostIdleRig.MOD = datainclude.Sum(x => x.OPCostIdleRig.MOD);
                        d.OPCostIdleRig.MODSS = datainclude.Sum(x => x.OPCostIdleRig.MODSS);
                        #endregion MOD

                        ///rt
                        #region rt
                        d.CapitalCompletionPDDevInTang.RealTerm = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.RealTerm);
                        d.CapitalCompletionPDDevTang.RealTerm = datainclude.Sum(x => x.CapitalCompletionPDDevTang.RealTerm);
                        d.CapitalCompletionPDDevInTang.RealTermSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.RealTermSS);
                        d.CapitalCompletionPDDevTang.RealTermSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.RealTermSS);

                        d.CapitalDrillingPDDevInTang.RealTerm = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.RealTerm);
                        d.CapitalDrillingPDDevTang.RealTerm = datainclude.Sum(x => x.CapitalDrillingPDDevTang.RealTerm);
                        d.CapitalDrillingPDDevInTang.RealTermSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.RealTermSS);
                        d.CapitalDrillingPDDevTang.RealTermSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.RealTermSS);

                        d.CapitalExpenDRDVAWells.RealTerm = datainclude.Sum(x => x.CapitalExpenDRDVAWells.RealTerm);
                        d.CapitalExpenDRSubSeaWells.RealTerm = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.RealTerm);
                        d.CapitalExpenDRDVAWells.RealTermSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.RealTermSS);
                        d.CapitalExpenDRSubSeaWells.RealTermSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.RealTermSS);

                        d.ContigencyInTangWells.RealTerm = datainclude.Sum(x => x.ContigencyInTangWells.RealTerm);
                        d.ContigencyTangWells.RealTerm = datainclude.Sum(x => x.ContigencyTangWells.RealTerm);
                        d.ContigencyInTangWells.RealTermSS = datainclude.Sum(x => x.ContigencyInTangWells.RealTermSS);
                        d.ContigencyTangWells.RealTermSS = datainclude.Sum(x => x.ContigencyTangWells.RealTermSS);

                        d.EPEXCompletionB2ExplInTang.RealTerm = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.RealTerm);
                        d.EPEXCompletionB2ExplTang.RealTerm = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.RealTerm);
                        d.EPEXCompletionB2ExplInTang.RealTermSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.RealTermSS);
                        d.EPEXCompletionB2ExplTang.RealTermSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.RealTermSS);

                        d.EPEXDrillingB2ExplInTang.RealTerm = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.RealTerm);
                        d.EPEXDrillingB2ExplTang.RealTerm = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.RealTerm);
                        d.EPEXDrillingB2ExplInTang.RealTermSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.RealTermSS);
                        d.EPEXDrillingB2ExplTang.RealTermSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.RealTermSS);

                        d.OPCostIdleRig.RealTerm = datainclude.Sum(x => x.OPCostIdleRig.RealTerm);
                        d.OPCostIdleRig.RealTermSS = datainclude.Sum(x => x.OPCostIdleRig.RealTermSS);
                        #endregion MOD
                        d.Currency = datainclude.FirstOrDefault().Currency;
                        d.DateId = yearonlystart;
                        d.Type = "Annual";
                        ld.Add(d);
                    }
                    else
                    {
                        PDetails d = new PDetails();
                        d.DateId = yearonlystart;
                        d.Type = "Annual";
                        ld.Add(d);

                    }
                    #endregion

                    yearonlystart = yearonlystart + 100;
                } while (yearonlystart <= yearonlyfinish);

                ag.CapexSummary = ld;
                ag.Save();
                #region tutup sek!
                //List<int> aaa = new List<int>();
                //do
                //{
                //    //aaa.Add(monthIdStart);
                //    #region binding the dta
                //    var datainclude = data.Where(x => x.DateId == monthIdStart).ToList();
                //    if (datainclude.Any())
                //    {
                //        PCapexAgg ag = new PCapexAgg();
                //        ag.MonthId = monthIdStart;
                //        ag.CaseName = t.Key;
                //        //ag.IsInPlan = ;

                //        ///EDM
                //        #region EDM
                //        ag.CapexSummary.CapitalCompletionPDDevInTang.EDM = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.EDM);
                //        ag.CapexSummary.CapitalCompletionPDDevTang.EDM = datainclude.Sum(x => x.CapitalCompletionPDDevTang.EDM);
                //        ag.CapexSummary.CapitalCompletionPDDevInTang.EDMSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.EDMSS);
                //        ag.CapexSummary.CapitalCompletionPDDevTang.EDMSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.EDMSS);

                //        ag.CapexSummary.CapitalDrillingPDDevInTang.EDM = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.EDM);
                //        ag.CapexSummary.CapitalDrillingPDDevTang.EDM = datainclude.Sum(x => x.CapitalDrillingPDDevTang.EDM);
                //        ag.CapexSummary.CapitalDrillingPDDevInTang.EDMSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.EDMSS);
                //        ag.CapexSummary.CapitalDrillingPDDevTang.EDMSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.EDMSS);

                //        ag.CapexSummary.CapitalExpenDRDVAWells.EDM = datainclude.Sum(x => x.CapitalExpenDRDVAWells.EDM);
                //        ag.CapexSummary.CapitalExpenDRSubSeaWells.EDM = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.EDM);
                //        ag.CapexSummary.CapitalExpenDRDVAWells.EDMSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.EDMSS);
                //        ag.CapexSummary.CapitalExpenDRSubSeaWells.EDMSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.EDMSS);

                //        ag.CapexSummary.ContigencyInTangWells.EDM = datainclude.Sum(x => x.ContigencyInTangWells.EDM);
                //        ag.CapexSummary.ContigencyTangWells.EDM = datainclude.Sum(x => x.ContigencyTangWells.EDM);
                //        ag.CapexSummary.ContigencyInTangWells.EDMSS = datainclude.Sum(x => x.ContigencyInTangWells.EDMSS);
                //        ag.CapexSummary.ContigencyTangWells.EDMSS = datainclude.Sum(x => x.ContigencyTangWells.EDMSS);

                //        ag.CapexSummary.EPEXCompletionB2ExplInTang.EDM = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.EDM);
                //        ag.CapexSummary.EPEXCompletionB2ExplTang.EDM = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.EDM);
                //        ag.CapexSummary.EPEXCompletionB2ExplInTang.EDMSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.EDMSS);
                //        ag.CapexSummary.EPEXCompletionB2ExplTang.EDMSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.EDMSS);

                //        ag.CapexSummary.EPEXDrillingB2ExplInTang.EDM = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.EDM);
                //        ag.CapexSummary.EPEXDrillingB2ExplTang.EDM = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.EDM);
                //        ag.CapexSummary.EPEXDrillingB2ExplInTang.EDMSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.EDMSS);
                //        ag.CapexSummary.EPEXDrillingB2ExplTang.EDMSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.EDMSS);

                //        ag.CapexSummary.OPCostIdleRig.EDM = datainclude.Sum(x => x.OPCostIdleRig.EDM);
                //        ag.CapexSummary.OPCostIdleRig.EDMSS = datainclude.Sum(x => x.OPCostIdleRig.EDMSS);
                //        #endregion EDM

                //        ///MOD
                //        #region MOD
                //        ag.CapexSummary.CapitalCompletionPDDevInTang.MOD = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.MOD);
                //        ag.CapexSummary.CapitalCompletionPDDevTang.MOD = datainclude.Sum(x => x.CapitalCompletionPDDevTang.MOD);
                //        ag.CapexSummary.CapitalCompletionPDDevInTang.MODSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.MODSS);
                //        ag.CapexSummary.CapitalCompletionPDDevTang.MODSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.MODSS);

                //        ag.CapexSummary.CapitalDrillingPDDevInTang.MOD = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.MOD);
                //        ag.CapexSummary.CapitalDrillingPDDevTang.MOD = datainclude.Sum(x => x.CapitalDrillingPDDevTang.MOD);
                //        ag.CapexSummary.CapitalDrillingPDDevInTang.MODSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.MODSS);
                //        ag.CapexSummary.CapitalDrillingPDDevTang.MODSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.MODSS);

                //        ag.CapexSummary.CapitalExpenDRDVAWells.MOD = datainclude.Sum(x => x.CapitalExpenDRDVAWells.MOD);
                //        ag.CapexSummary.CapitalExpenDRSubSeaWells.MOD = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.MOD);
                //        ag.CapexSummary.CapitalExpenDRDVAWells.MODSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.MODSS);
                //        ag.CapexSummary.CapitalExpenDRSubSeaWells.MODSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.MODSS);

                //        ag.CapexSummary.ContigencyInTangWells.MOD = datainclude.Sum(x => x.ContigencyInTangWells.MOD);
                //        ag.CapexSummary.ContigencyTangWells.MOD = datainclude.Sum(x => x.ContigencyTangWells.MOD);
                //        ag.CapexSummary.ContigencyInTangWells.MODSS = datainclude.Sum(x => x.ContigencyInTangWells.MODSS);
                //        ag.CapexSummary.ContigencyTangWells.MODSS = datainclude.Sum(x => x.ContigencyTangWells.MODSS);

                //        ag.CapexSummary.EPEXCompletionB2ExplInTang.MOD = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.MOD);
                //        ag.CapexSummary.EPEXCompletionB2ExplTang.MOD = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.MOD);
                //        ag.CapexSummary.EPEXCompletionB2ExplInTang.MODSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.MODSS);
                //        ag.CapexSummary.EPEXCompletionB2ExplTang.MODSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.MODSS);

                //        ag.CapexSummary.EPEXDrillingB2ExplInTang.MOD = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.MOD);
                //        ag.CapexSummary.EPEXDrillingB2ExplTang.MOD = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.MOD);
                //        ag.CapexSummary.EPEXDrillingB2ExplInTang.MODSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.MODSS);
                //        ag.CapexSummary.EPEXDrillingB2ExplTang.MODSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.MODSS);

                //        ag.CapexSummary.OPCostIdleRig.MOD = datainclude.Sum(x => x.OPCostIdleRig.MOD);
                //        ag.CapexSummary.OPCostIdleRig.MODSS = datainclude.Sum(x => x.OPCostIdleRig.MODSS);
                //        #endregion MOD

                //        ///RT
                //        #region RT
                //        ag.CapexSummary.CapitalCompletionPDDevInTang.RealTerm = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.RealTerm);
                //        ag.CapexSummary.CapitalCompletionPDDevTang.RealTerm = datainclude.Sum(x => x.CapitalCompletionPDDevTang.RealTerm);
                //        ag.CapexSummary.CapitalCompletionPDDevInTang.RealTermSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.RealTermSS);
                //        ag.CapexSummary.CapitalCompletionPDDevTang.RealTermSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.RealTermSS);

                //        ag.CapexSummary.CapitalDrillingPDDevInTang.RealTerm = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.RealTerm);
                //        ag.CapexSummary.CapitalDrillingPDDevTang.RealTerm = datainclude.Sum(x => x.CapitalDrillingPDDevTang.RealTerm);
                //        ag.CapexSummary.CapitalDrillingPDDevInTang.RealTermSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.RealTermSS);
                //        ag.CapexSummary.CapitalDrillingPDDevTang.RealTermSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.RealTermSS);

                //        ag.CapexSummary.CapitalExpenDRDVAWells.RealTerm = datainclude.Sum(x => x.CapitalExpenDRDVAWells.RealTerm);
                //        ag.CapexSummary.CapitalExpenDRSubSeaWells.RealTerm = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.RealTerm);
                //        ag.CapexSummary.CapitalExpenDRDVAWells.RealTermSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.RealTermSS);
                //        ag.CapexSummary.CapitalExpenDRSubSeaWells.RealTermSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.RealTermSS);

                //        ag.CapexSummary.ContigencyInTangWells.RealTerm = datainclude.Sum(x => x.ContigencyInTangWells.RealTerm);
                //        ag.CapexSummary.ContigencyTangWells.RealTerm = datainclude.Sum(x => x.ContigencyTangWells.RealTerm);
                //        ag.CapexSummary.ContigencyInTangWells.RealTermSS = datainclude.Sum(x => x.ContigencyInTangWells.RealTermSS);
                //        ag.CapexSummary.ContigencyTangWells.RealTermSS = datainclude.Sum(x => x.ContigencyTangWells.RealTermSS);

                //        ag.CapexSummary.EPEXCompletionB2ExplInTang.RealTerm = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.RealTerm);
                //        ag.CapexSummary.EPEXCompletionB2ExplTang.RealTerm = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.RealTerm);
                //        ag.CapexSummary.EPEXCompletionB2ExplInTang.RealTermSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.RealTermSS);
                //        ag.CapexSummary.EPEXCompletionB2ExplTang.RealTermSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.RealTermSS);

                //        ag.CapexSummary.EPEXDrillingB2ExplInTang.RealTerm = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.RealTerm);
                //        ag.CapexSummary.EPEXDrillingB2ExplTang.RealTerm = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.RealTerm);
                //        ag.CapexSummary.EPEXDrillingB2ExplInTang.RealTermSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.RealTermSS);
                //        ag.CapexSummary.EPEXDrillingB2ExplTang.RealTermSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.RealTermSS);

                //        ag.CapexSummary.OPCostIdleRig.RealTerm = datainclude.Sum(x => x.OPCostIdleRig.RealTerm);
                //        ag.CapexSummary.OPCostIdleRig.RealTermSS = datainclude.Sum(x => x.OPCostIdleRig.RealTermSS);
                //        #endregion MOD

                //        ag.CapexSummary.Currency = datainclude.FirstOrDefault().Currency;
                //        res.Add(ag);
                //    }
                //    else
                //    {
                //        PCapexAgg ag = new PCapexAgg();
                //        ag.MonthId = monthIdStart;
                //        ag.CaseName = t.Key;
                //        res.Add(ag);

                //    }
                //    #endregion
                //    monthIdStart = AddMonthId(monthIdStart, 1);
                //} while (monthIdStart <= monthIdFinish && (monthIdStart != onlyYearValueStart));

                ////var lastMoth = aaa.Max();

                //var yearonlystart = Convert.ToInt32(onlyYearValueStart);
                //var yearonlyfinish = Convert.ToInt32(monthIdFinish);
                //do
                //{
                //    aaa.Add(Convert.ToInt32(yearonlystart));
                //    #region binding the dta
                //    var datainclude = data.Where(x => Convert.ToInt32(x.DateId.ToString().Substring(0, 4)) == Convert.ToInt32(yearonlystart.ToString().Substring(0, 4))).ToList();
                //    if (datainclude.Any())
                //    {
                //        PCapexAgg ag = new PCapexAgg();
                //        ag.MonthId = yearonlystart;
                //        ag.CaseName = t.Key;

                //        ///EDM
                //        #region EDM
                //        ag.CapexSummary.CapitalCompletionPDDevInTang.EDM = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.EDM);
                //        ag.CapexSummary.CapitalCompletionPDDevTang.EDM = datainclude.Sum(x => x.CapitalCompletionPDDevTang.EDM);
                //        ag.CapexSummary.CapitalCompletionPDDevInTang.EDMSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.EDMSS);
                //        ag.CapexSummary.CapitalCompletionPDDevTang.EDMSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.EDMSS);

                //        ag.CapexSummary.CapitalDrillingPDDevInTang.EDM = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.EDM);
                //        ag.CapexSummary.CapitalDrillingPDDevTang.EDM = datainclude.Sum(x => x.CapitalDrillingPDDevTang.EDM);
                //        ag.CapexSummary.CapitalDrillingPDDevInTang.EDMSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.EDMSS);
                //        ag.CapexSummary.CapitalDrillingPDDevTang.EDMSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.EDMSS);

                //        ag.CapexSummary.CapitalExpenDRDVAWells.EDM = datainclude.Sum(x => x.CapitalExpenDRDVAWells.EDM);
                //        ag.CapexSummary.CapitalExpenDRSubSeaWells.EDM = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.EDM);
                //        ag.CapexSummary.CapitalExpenDRDVAWells.EDMSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.EDMSS);
                //        ag.CapexSummary.CapitalExpenDRSubSeaWells.EDMSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.EDMSS);

                //        ag.CapexSummary.ContigencyInTangWells.EDM = datainclude.Sum(x => x.ContigencyInTangWells.EDM);
                //        ag.CapexSummary.ContigencyTangWells.EDM = datainclude.Sum(x => x.ContigencyTangWells.EDM);
                //        ag.CapexSummary.ContigencyInTangWells.EDMSS = datainclude.Sum(x => x.ContigencyInTangWells.EDMSS);
                //        ag.CapexSummary.ContigencyTangWells.EDMSS = datainclude.Sum(x => x.ContigencyTangWells.EDMSS);

                //        ag.CapexSummary.EPEXCompletionB2ExplInTang.EDM = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.EDM);
                //        ag.CapexSummary.EPEXCompletionB2ExplTang.EDM = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.EDM);
                //        ag.CapexSummary.EPEXCompletionB2ExplInTang.EDMSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.EDMSS);
                //        ag.CapexSummary.EPEXCompletionB2ExplTang.EDMSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.EDMSS);

                //        ag.CapexSummary.EPEXDrillingB2ExplInTang.EDM = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.EDM);
                //        ag.CapexSummary.EPEXDrillingB2ExplTang.EDM = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.EDM);
                //        ag.CapexSummary.EPEXDrillingB2ExplInTang.EDMSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.EDMSS);
                //        ag.CapexSummary.EPEXDrillingB2ExplTang.EDMSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.EDMSS);

                //        ag.CapexSummary.OPCostIdleRig.EDM = datainclude.Sum(x => x.OPCostIdleRig.EDM);
                //        ag.CapexSummary.OPCostIdleRig.EDMSS = datainclude.Sum(x => x.OPCostIdleRig.EDMSS);
                //        #endregion EDM

                //        ///MOD
                //        #region MOD
                //        ag.CapexSummary.CapitalCompletionPDDevInTang.MOD = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.MOD);
                //        ag.CapexSummary.CapitalCompletionPDDevTang.MOD = datainclude.Sum(x => x.CapitalCompletionPDDevTang.MOD);
                //        ag.CapexSummary.CapitalCompletionPDDevInTang.MODSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.MODSS);
                //        ag.CapexSummary.CapitalCompletionPDDevTang.MODSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.MODSS);

                //        ag.CapexSummary.CapitalDrillingPDDevInTang.MOD = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.MOD);
                //        ag.CapexSummary.CapitalDrillingPDDevTang.MOD = datainclude.Sum(x => x.CapitalDrillingPDDevTang.MOD);
                //        ag.CapexSummary.CapitalDrillingPDDevInTang.MODSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.MODSS);
                //        ag.CapexSummary.CapitalDrillingPDDevTang.MODSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.MODSS);

                //        ag.CapexSummary.CapitalExpenDRDVAWells.MOD = datainclude.Sum(x => x.CapitalExpenDRDVAWells.MOD);
                //        ag.CapexSummary.CapitalExpenDRSubSeaWells.MOD = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.MOD);
                //        ag.CapexSummary.CapitalExpenDRDVAWells.MODSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.MODSS);
                //        ag.CapexSummary.CapitalExpenDRSubSeaWells.MODSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.MODSS);

                //        ag.CapexSummary.ContigencyInTangWells.MOD = datainclude.Sum(x => x.ContigencyInTangWells.MOD);
                //        ag.CapexSummary.ContigencyTangWells.MOD = datainclude.Sum(x => x.ContigencyTangWells.MOD);
                //        ag.CapexSummary.ContigencyInTangWells.MODSS = datainclude.Sum(x => x.ContigencyInTangWells.MODSS);
                //        ag.CapexSummary.ContigencyTangWells.MODSS = datainclude.Sum(x => x.ContigencyTangWells.MODSS);

                //        ag.CapexSummary.EPEXCompletionB2ExplInTang.MOD = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.MOD);
                //        ag.CapexSummary.EPEXCompletionB2ExplTang.MOD = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.MOD);
                //        ag.CapexSummary.EPEXCompletionB2ExplInTang.MODSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.MODSS);
                //        ag.CapexSummary.EPEXCompletionB2ExplTang.MODSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.MODSS);

                //        ag.CapexSummary.EPEXDrillingB2ExplInTang.MOD = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.MOD);
                //        ag.CapexSummary.EPEXDrillingB2ExplTang.MOD = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.MOD);
                //        ag.CapexSummary.EPEXDrillingB2ExplInTang.MODSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.MODSS);
                //        ag.CapexSummary.EPEXDrillingB2ExplTang.MODSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.MODSS);

                //        ag.CapexSummary.OPCostIdleRig.MOD = datainclude.Sum(x => x.OPCostIdleRig.MOD);
                //        ag.CapexSummary.OPCostIdleRig.MODSS = datainclude.Sum(x => x.OPCostIdleRig.MODSS);
                //        #endregion MOD

                //        ///rt
                //        #region rt
                //        ag.CapexSummary.CapitalCompletionPDDevInTang.RealTerm = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.RealTerm);
                //        ag.CapexSummary.CapitalCompletionPDDevTang.RealTerm = datainclude.Sum(x => x.CapitalCompletionPDDevTang.RealTerm);
                //        ag.CapexSummary.CapitalCompletionPDDevInTang.RealTermSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.RealTermSS);
                //        ag.CapexSummary.CapitalCompletionPDDevTang.RealTermSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.RealTermSS);

                //        ag.CapexSummary.CapitalDrillingPDDevInTang.RealTerm = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.RealTerm);
                //        ag.CapexSummary.CapitalDrillingPDDevTang.RealTerm = datainclude.Sum(x => x.CapitalDrillingPDDevTang.RealTerm);
                //        ag.CapexSummary.CapitalDrillingPDDevInTang.RealTermSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.RealTermSS);
                //        ag.CapexSummary.CapitalDrillingPDDevTang.RealTermSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.RealTermSS);

                //        ag.CapexSummary.CapitalExpenDRDVAWells.RealTerm = datainclude.Sum(x => x.CapitalExpenDRDVAWells.RealTerm);
                //        ag.CapexSummary.CapitalExpenDRSubSeaWells.RealTerm = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.RealTerm);
                //        ag.CapexSummary.CapitalExpenDRDVAWells.RealTermSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.RealTermSS);
                //        ag.CapexSummary.CapitalExpenDRSubSeaWells.RealTermSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.RealTermSS);

                //        ag.CapexSummary.ContigencyInTangWells.RealTerm = datainclude.Sum(x => x.ContigencyInTangWells.RealTerm);
                //        ag.CapexSummary.ContigencyTangWells.RealTerm = datainclude.Sum(x => x.ContigencyTangWells.RealTerm);
                //        ag.CapexSummary.ContigencyInTangWells.RealTermSS = datainclude.Sum(x => x.ContigencyInTangWells.RealTermSS);
                //        ag.CapexSummary.ContigencyTangWells.RealTermSS = datainclude.Sum(x => x.ContigencyTangWells.RealTermSS);

                //        ag.CapexSummary.EPEXCompletionB2ExplInTang.RealTerm = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.RealTerm);
                //        ag.CapexSummary.EPEXCompletionB2ExplTang.RealTerm = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.RealTerm);
                //        ag.CapexSummary.EPEXCompletionB2ExplInTang.RealTermSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.RealTermSS);
                //        ag.CapexSummary.EPEXCompletionB2ExplTang.RealTermSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.RealTermSS);

                //        ag.CapexSummary.EPEXDrillingB2ExplInTang.RealTerm = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.RealTerm);
                //        ag.CapexSummary.EPEXDrillingB2ExplTang.RealTerm = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.RealTerm);
                //        ag.CapexSummary.EPEXDrillingB2ExplInTang.RealTermSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.RealTermSS);
                //        ag.CapexSummary.EPEXDrillingB2ExplTang.RealTermSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.RealTermSS);

                //        ag.CapexSummary.OPCostIdleRig.RealTerm = datainclude.Sum(x => x.OPCostIdleRig.RealTerm);
                //        ag.CapexSummary.OPCostIdleRig.RealTermSS = datainclude.Sum(x => x.OPCostIdleRig.RealTermSS);
                //        #endregion MOD

                //        res.Add(ag);
                //    }
                //    else
                //    {
                //        PCapexAgg ag = new PCapexAgg();
                //        ag.MonthId = yearonlystart;
                //        ag.CaseName = t.Key;
                //        res.Add(ag);

                //    }
                //    #endregion

                //    yearonlystart = yearonlystart + 100;
                //} while (yearonlystart <= yearonlyfinish);

                //var lastMoth = aaa.Max();

                // process annual
                #endregion
            }
            return true;
        }

        public int AddMonthId(int monthId, int increment, int passifvalue = 12)
        {
            var year = Convert.ToInt32(monthId.ToString().Substring(0, 4));
            var month = Convert.ToInt32(monthId.ToString().Substring(4, 2));

            //List<string> list = new List<string>();

            for (int i = 1; i <= increment; i++)
            {

                if ((month + 1) > passifvalue)
                {
                    month = 1;
                    year++;
                }
                else
                {
                    month++;
                }
                // list.Add(year + month.ToString("00"));

            }
            return Convert.ToInt32(year + month.ToString("00"));
        }

        public List<PDetails> CalcDetails(PDetail summary, List<NumberOfDayPerMonth> monthlyBuckets)
        {
            List<PDetails> result = new List<PDetails>();
            foreach (var mb in monthlyBuckets.OrderBy(x => x.MonthId))
            {
                PDetails ps = new PDetails();
                ps.CapitalCompletionPDDevInTang.EDM = summary.CapitalCompletionPDDevInTang.EDM * mb.Proportion;
                ps.CapitalCompletionPDDevInTang.MOD = summary.CapitalCompletionPDDevInTang.MOD * mb.Proportion;

                ps.CapitalCompletionPDDevTang.EDM = summary.CapitalCompletionPDDevTang.EDM * mb.Proportion;
                ps.CapitalCompletionPDDevTang.MOD = summary.CapitalCompletionPDDevTang.MOD * mb.Proportion;

                ps.CapitalDrillingPDDevInTang.EDM = summary.CapitalDrillingPDDevInTang.EDM * mb.Proportion;
                ps.CapitalDrillingPDDevInTang.MOD = summary.CapitalDrillingPDDevInTang.MOD * mb.Proportion;

                ps.CapitalDrillingPDDevTang.EDM = summary.CapitalDrillingPDDevTang.EDM * mb.Proportion;
                ps.CapitalDrillingPDDevTang.MOD = summary.CapitalDrillingPDDevTang.MOD * mb.Proportion;

                ps.CapitalExpenDRDVAWells.EDM = summary.CapitalExpenDRDVAWells.EDM * mb.Proportion;
                ps.CapitalExpenDRDVAWells.MOD = summary.CapitalExpenDRDVAWells.MOD * mb.Proportion;

                ps.CapitalExpenDRSubSeaWells.EDM = summary.CapitalExpenDRSubSeaWells.EDM * mb.Proportion;
                ps.CapitalExpenDRSubSeaWells.MOD = summary.CapitalExpenDRSubSeaWells.MOD * mb.Proportion;

                ps.ContigencyInTangWells.EDM = summary.ContigencyInTangWells.EDM * mb.Proportion;
                ps.ContigencyInTangWells.MOD = summary.ContigencyInTangWells.MOD * mb.Proportion;

                ps.ContigencyTangWells.EDM = summary.ContigencyTangWells.EDM * mb.Proportion;
                ps.ContigencyTangWells.MOD = summary.ContigencyTangWells.MOD * mb.Proportion;

                ps.EPEXCompletionB2ExplInTang.EDM = summary.EPEXCompletionB2ExplInTang.EDM * mb.Proportion;
                ps.EPEXCompletionB2ExplInTang.MOD = summary.EPEXCompletionB2ExplInTang.MOD * mb.Proportion;

                ps.EPEXCompletionB2ExplTang.EDM = summary.EPEXCompletionB2ExplTang.EDM * mb.Proportion;
                ps.EPEXCompletionB2ExplTang.MOD = summary.EPEXCompletionB2ExplTang.MOD * mb.Proportion;

                ps.EPEXDrillingB2ExplInTang.EDM = summary.EPEXDrillingB2ExplInTang.EDM * mb.Proportion;
                ps.EPEXDrillingB2ExplInTang.MOD = summary.EPEXDrillingB2ExplInTang.MOD * mb.Proportion;

                ps.EPEXDrillingB2ExplTang.EDM = summary.EPEXDrillingB2ExplTang.EDM * mb.Proportion;
                ps.EPEXDrillingB2ExplTang.MOD = summary.EPEXDrillingB2ExplTang.MOD * mb.Proportion;

                ps.OPCostIdleRig.EDM = summary.OPCostIdleRig.EDM * mb.Proportion;
                ps.OPCostIdleRig.MOD = summary.OPCostIdleRig.MOD * mb.Proportion;

                ps.CapitalCompletionPDDevInTang.EDMSS = summary.CapitalCompletionPDDevInTang.EDMSS * mb.Proportion;
                ps.CapitalCompletionPDDevInTang.MODSS = summary.CapitalCompletionPDDevInTang.MODSS * mb.Proportion;

                ps.CapitalCompletionPDDevTang.EDMSS = summary.CapitalCompletionPDDevTang.EDMSS * mb.Proportion;
                ps.CapitalCompletionPDDevTang.MODSS = summary.CapitalCompletionPDDevTang.MODSS * mb.Proportion;

                ps.CapitalDrillingPDDevInTang.EDMSS = summary.CapitalDrillingPDDevInTang.EDMSS * mb.Proportion;
                ps.CapitalDrillingPDDevInTang.MODSS = summary.CapitalDrillingPDDevInTang.MODSS * mb.Proportion;

                ps.CapitalDrillingPDDevTang.EDMSS = summary.CapitalDrillingPDDevTang.EDMSS * mb.Proportion;
                ps.CapitalDrillingPDDevTang.MODSS = summary.CapitalDrillingPDDevTang.MODSS * mb.Proportion;

                ps.CapitalExpenDRDVAWells.EDMSS = summary.CapitalExpenDRDVAWells.EDMSS * mb.Proportion;
                ps.CapitalExpenDRDVAWells.MODSS = summary.CapitalExpenDRDVAWells.MODSS * mb.Proportion;

                ps.CapitalExpenDRSubSeaWells.EDMSS = summary.CapitalExpenDRSubSeaWells.EDMSS * mb.Proportion;
                ps.CapitalExpenDRSubSeaWells.MODSS = summary.CapitalExpenDRSubSeaWells.MODSS * mb.Proportion;

                ps.ContigencyInTangWells.EDMSS = summary.ContigencyInTangWells.EDMSS * mb.Proportion;
                ps.ContigencyInTangWells.MODSS = summary.ContigencyInTangWells.MODSS * mb.Proportion;

                ps.ContigencyTangWells.EDMSS = summary.ContigencyTangWells.EDMSS * mb.Proportion;
                ps.ContigencyTangWells.MODSS = summary.ContigencyTangWells.MODSS * mb.Proportion;

                ps.EPEXCompletionB2ExplInTang.EDMSS = summary.EPEXCompletionB2ExplInTang.EDMSS * mb.Proportion;
                ps.EPEXCompletionB2ExplInTang.MODSS = summary.EPEXCompletionB2ExplInTang.MODSS * mb.Proportion;

                ps.EPEXCompletionB2ExplTang.EDMSS = summary.EPEXCompletionB2ExplTang.EDMSS * mb.Proportion;
                ps.EPEXCompletionB2ExplTang.MODSS = summary.EPEXCompletionB2ExplTang.MODSS * mb.Proportion;

                ps.EPEXDrillingB2ExplInTang.EDMSS = summary.EPEXDrillingB2ExplInTang.EDMSS * mb.Proportion;
                ps.EPEXDrillingB2ExplInTang.MODSS = summary.EPEXDrillingB2ExplInTang.MODSS * mb.Proportion;

                ps.EPEXDrillingB2ExplTang.EDMSS = summary.EPEXDrillingB2ExplTang.EDMSS * mb.Proportion;
                ps.EPEXDrillingB2ExplTang.MODSS = summary.EPEXDrillingB2ExplTang.MODSS * mb.Proportion;

                ps.OPCostIdleRig.EDMSS = summary.OPCostIdleRig.EDMSS * mb.Proportion;
                ps.OPCostIdleRig.MODSS = summary.OPCostIdleRig.MODSS * mb.Proportion;

                ps.Type = "Monthly";
                ps.DateId = mb.MonthId;

                ps.Currency = summary.Currency;

                result.Add(ps);
            }
            return result;
        }

        #region marked temporary
        //public PCapex GetListCapex(BsonDocument data, string updateBy)
        //{
        //    PCapex result = new PCapex();

        //    try
        //    {


        //        result.CaseName = data.GetString("Case_Name");
        //        result.ActivityCategory = data.GetString("Activity_Category");
        //        result.WellName = data.GetString("Well_Name");
        //        result.ActivityType = data.GetString("Activity_Type");
        //        result.FundingType = data.GetString("Funding_Type");
        //        result.FirmOption = data.GetString("Planning_Classification");
        //        result.RigName = data.GetString("Rig_Name");
        //        result.UpdateVersion = Tools.ToUTC(DateTime.Now, true);
        //        if (result.WellName.Equals("Bonga BNW 690 p4"))
        //        {

        //        }
        //        var getWellAcitvity = WellActivity.Get<WellActivity>(Query.And(Query.EQ("WellName", result.WellName),
        //            //Query.M("Phases.ActivityType", result.ActivityType),
        //            Query.EQ("RigName", result.RigName)));
        //        if (getWellAcitvity == null)
        //        {
        //            return null;
        //        }

        //        result.UARigSequenceId = getWellAcitvity.UARigSequenceId;
        //        result.ProjectName = getWellAcitvity.ProjectName;
        //        result.UpdateBy = updateBy;
        //        //result.FirmOption = getWellAcitvity.FirmOrOption;
        //        result.AssetName = getWellAcitvity.AssetName;


        //        var actype = getWellAcitvity.Phases.Where(x => x.ActivityType.Contains(result.ActivityType));//.PhSchedule.Start != null && !WellActivity.isHaveWeeklyReport(getWellAcitvity.WellName, getWellAcitvity.UARigSequenceId, x.ActivityType));
        //        //var actype = getWellAcitvity.Phases.Where(x => x.PhSchedule.Start != null && !WellActivity.isHaveWeeklyReport(getWellAcitvity.WellName, getWellAcitvity.UARigSequenceId, x.ActivityType));

        //        if (actype != null && actype.Count() > 0)
        //        {
        //            var t = actype.FirstOrDefault(x => x.ActivityType.Contains(result.ActivityType));
        //            result.BaseOP = t.BaseOP == null ? "" : (t.BaseOP.Count <= 0 ? "" : t.BaseOP.OrderByDescending(x => x).FirstOrDefault());
        //            result.PhSchedule = t.PhSchedule;
        //            result.OP = t.OP;
        //            result.PlanSchedule = t.PlanSchedule;
        //            result.Plan = t.Plan;
        //            result.IsInPlan = t.IsInPlan;
        //        }

        //        var startmonthrangetofinish = new DateRange(new DateTime(2015, 1, 1), new DateTime(2040, 12, 31));
        //        //result = DetailSummary(result, startmonthrangetofinish);

        //        if (string.IsNullOrEmpty(result.CaseName))
        //        {

        //        }

        //        return result;
        //    }
        //    catch (Exception ce)
        //    {
        //        return null;

        //    }
        //}
        #endregion
    }

    public class CapexUploadModel
    {
        public string Case_Name { get; set; }
        public string Activity_Category { get; set; }
        public string Well_Name { get; set; }
        public string Activity_Type { get; set; }
        public string Funding_Type { get; set; }
        public string Planning_Classification { get; set; }
        public string Rig_Name { get; set; }
        public string UARigSequenceId { get; set; }
    }

    public class PCapexAgg : ECISModel
    {
        public override string TableName
        {
            get { return "WEISPalantirCapexAgg"; }
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            //var newId = SequenceNo.Get(this.TableName).ClaimAsInt();
            //_id = newId;
            _id = String.Format("C{0}U{1}M{2}",
                CaseName.Replace(" ", "").Replace("-", ""),
                UpdateBy,
                MapName.Replace(" ", "").Replace("-", "")
            );
            return this.ToBsonDocument();
        }

        public string UpdateBy { get; set; }
        public DateTime UpdateVersion { get; set; }
        public string CaseName { get; set; }
        //public int MonthId { get; set; }
        public string MapName { get; set; }
        public List<PDetails> CapexSummary { get; set; }
        //public bool IsInPlan { get; set; }

        public PCapexAgg()
        {
            CapexSummary = new List<PDetails>();
        }
    }

    public class PCapexAggOld : ECISModel
    {
        public override string TableName
        {
            get { return "WEISPalantirCapexAgg"; }
        }

        //public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        //{
        //    //var newId = SequenceNo.Get(this.TableName).ClaimAsInt();
        //    //_id = newId;
        //    _id = String.Format("C{0}U{1}M{2}",
        //        CaseName.Replace(" ", "").Replace("-", ""),
        //        UpdateBy,
        //        MapName.Replace(" ", "").Replace("-", "")
        //    );
        //    return this.ToBsonDocument();
        //}

        public string UpdateBy { get; set; }
        public DateTime UpdateVersion { get; set; }
        public string CaseName { get; set; }
        public int MonthId { get; set; }
        public string MapName { get; set; }
        public PDetails CapexSummary { get; set; }
        //public bool IsInPlan { get; set; }

        public PCapexAggOld()
        {
            CapexSummary = new PDetails();
        }
    }

    public class PCapexAggHorz : ECISModel
    {
        public override string TableName
        {
            get { return "WEISPalantirCapexAggHorizontal"; }
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            var newId = SequenceNo.Get(this.TableName).ClaimAsInt();
            _id = newId;
            //_id = String.Format("C{0}U{1}M{2}F{3}",
            //    CaseName = string.IsNullOrEmpty(CaseName) ? "" : CaseName.Replace(" ", "").Replace("-", ""),
            //    UpdateBy,
            //    MapName.Replace(" ", "").Replace("-", ""),
            //    Field.Replace(" ", "").Replace("-", "")
            //);
            return this.ToBsonDocument();
        }

        public string UpdateBy { get; set; }
        //public DateTime UpdateVersion { get; set; }
        public string MapName { get; set; }
        public string CaseName { get; set; }
        public string Field { get; set; }
        public List<CapexHorDetail> Details { get; set; }

        public PCapexAggHorz()
        {
            UpdateBy = "";
            CaseName = "";
            Field = "";
            Details = new List<CapexHorDetail>();
        }

        List<string> CAPEXFields = new List<string>
        {
            {"CAPITAL DRILLING PD Dev Tangible (mm)" } ,
            {"CAPITAL DRILLING PD DEV INTANGIBLE (mm)" } ,
            {"CAPITAL COMPLETION PD DEV TANGIBLE (mm)" } ,
            {"CAPITAL COMPLETION PD DEV INTANGIBLE (mm)" } ,
            {"EXPEX DRILLING B2 EXPL TANGIBLE (mm)" } ,
            {"EXPEX DRILLING B2 EXPL INTANGIBLE (mm)" } ,
            {"EXPEX COMPLETION B2 EXPL TANGIBLE (mm)" } ,
            {"EXPEX COMPLETION B2 EXPL INTANGIBLE (mm)" } ,
            {"CAPITAL EXPENDITURE D&R DVA WELLS (mm)" } ,
            {"CAPITAL EXPENDITURE D&R SUBSEA WELLS (mm)" } ,
            {"OPCOSTS IDLE RIG (mm)" } ,
            {"CONTINGENCY TANGIBLE WELLS (mm)" } ,
            {"CONTINGENCY INTANGIBLE WELLS (mm)" }
        };

        public bool HorizontalAggCapex(List<PCapex> list, string username, DateTime updateVer, ParsePalentirQuery pq, string mapName, int monthIdStart = 201601, int monthIdFinish = 206612, int onlyYearValueStart = 202201)
        {
            try
            {
                // resync from bizplan
                PCapex px = new PCapex();
                //list = px.ReSyncMapFromBizplan(list, list.FirstOrDefault().MapName);

                var grp = list.GroupBy(x => x.CaseName);
                foreach (var t in grp)
                {
                    foreach (var f in CAPEXFields)
                    {
                        PCapexAggHorz ph = new PCapexAggHorz();
                        if (monthIdStart == 202201)
                        {
                            monthIdStart = 201601;
                        }
                        ph.CaseName = t.Key;
                        ph.Field = f;
                        ph.UpdateBy = username;
                        ph.MapName = mapName;
                        var data = t.ToList().SelectMany(x => x.CapexDetails);
                        int cols = 2;
                        do
                        {
                            #region binding the data
                            var datainclude = data.Where(x => x.DateId == monthIdStart).ToList();
                            CapexHorDetail cd = new CapexHorDetail();
                            if (datainclude.Any())
                            {
                                #region binding data

                                if (f == "CAPITAL DRILLING PD Dev Tangible (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalDrillingPDDevTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalDrillingPDDevTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalDrillingPDDevTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.RealTermSS);
                                }
                                else if (f == "CAPITAL DRILLING PD DEV INTANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.RealTermSS);
                                }
                                else if (f == "CAPITAL COMPLETION PD DEV TANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalCompletionPDDevTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalCompletionPDDevTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalCompletionPDDevTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.RealTermSS);
                                }
                                else if (f == "CAPITAL COMPLETION PD DEV INTANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.RealTermSS);
                                }
                                else if (f == "EXPEX DRILLING B2 EXPL TANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.RealTermSS);
                                }
                                else if (f == "EXPEX DRILLING B2 EXPL INTANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.RealTermSS);
                                }
                                else if (f == "EXPEX COMPLETION B2 EXPL TANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.RealTermSS);
                                }
                                else if (f == "EXPEX COMPLETION B2 EXPL INTANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.RealTermSS);
                                }
                                else if (f == "CAPITAL EXPENDITURE D&R DVA WELLS (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalExpenDRDVAWells.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalExpenDRDVAWells.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalExpenDRDVAWells.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.RealTermSS);
                                }
                                else if (f == "CAPITAL EXPENDITURE D&R SUBSEA WELLS (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.RealTermSS);
                                }
                                else if (f == "OPCOSTS IDLE RIG (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.OPCostIdleRig.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.OPCostIdleRig.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.OPCostIdleRig.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.OPCostIdleRig.MODSS);
                                    cd.RT = datainclude.Sum(x => x.OPCostIdleRig.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.OPCostIdleRig.RealTermSS);
                                }
                                else if (f == "CONTINGENCY TANGIBLE WELLS (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.ContigencyTangWells.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.ContigencyTangWells.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.ContigencyTangWells.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.ContigencyTangWells.MODSS);
                                    cd.RT = datainclude.Sum(x => x.ContigencyTangWells.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.ContigencyTangWells.RealTermSS);
                                }
                                else if (f == "CONTINGENCY INTANGIBLE WELLS (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.ContigencyInTangWells.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.ContigencyInTangWells.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.ContigencyInTangWells.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.ContigencyInTangWells.MODSS);
                                    cd.RT = datainclude.Sum(x => x.ContigencyInTangWells.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.ContigencyInTangWells.RealTermSS);
                                }
                                cd.DateId = monthIdStart;
                                cd.Type = "Monthly";
                                cd.Column = cols;
                                ph.Details.Add(cd);
                                #endregion
                            }
                            else
                            {
                                cd.DateId = monthIdStart;
                                cd.Type = "Monthly";
                                cd.Column = cols;
                                ph.Details.Add(cd);
                            }
                            #endregion
                            monthIdStart = px.AddMonthId(monthIdStart, 1);
                            cols++;
                        } while (monthIdStart <= monthIdFinish && (monthIdStart != onlyYearValueStart));

                        var yearonlystart = Convert.ToInt32(onlyYearValueStart);
                        var yearonlyfinish = Convert.ToInt32(monthIdFinish);
                        do
                        {
                            //aaa.Add(Convert.ToInt32(yearonlystart));
                            #region binding the data
                            var datainclude = data.Where(x => Convert.ToInt32(x.DateId.ToString().Substring(0, 4)) == Convert.ToInt32(yearonlystart.ToString().Substring(0, 4))).ToList();
                            CapexHorDetail cd = new CapexHorDetail();
                            if (datainclude.Any())
                            {
                                #region binding data

                                if (f == "CAPITAL DRILLING PD Dev Tangible (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalDrillingPDDevTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalDrillingPDDevTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalDrillingPDDevTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.RealTermSS);
                                }
                                else if (f == "CAPITAL DRILLING PD DEV INTANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.RealTermSS);
                                }
                                else if (f == "CAPITAL COMPLETION PD DEV TANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalCompletionPDDevTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalCompletionPDDevTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalCompletionPDDevTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.RealTermSS);
                                }
                                else if (f == "CAPITAL COMPLETION PD DEV INTANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.RealTermSS);
                                }
                                else if (f == "EXPEX DRILLING B2 EXPL TANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.RealTermSS);
                                }
                                else if (f == "EXPEX DRILLING B2 EXPL INTANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.RealTermSS);
                                }
                                else if (f == "EXPEX COMPLETION B2 EXPL TANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.RealTermSS);
                                }
                                else if (f == "EXPEX COMPLETION B2 EXPL INTANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.RealTermSS);
                                }
                                else if (f == "CAPITAL EXPENDITURE D&R DVA WELLS (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalExpenDRDVAWells.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalExpenDRDVAWells.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalExpenDRDVAWells.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.RealTermSS);
                                }
                                else if (f == "CAPITAL EXPENDITURE D&R SUBSEA WELLS (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.RealTermSS);
                                }
                                else if (f == "OPCOSTS IDLE RIG (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.OPCostIdleRig.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.OPCostIdleRig.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.OPCostIdleRig.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.OPCostIdleRig.MODSS);
                                    cd.RT = datainclude.Sum(x => x.OPCostIdleRig.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.OPCostIdleRig.RealTermSS);
                                }
                                else if (f == "CONTINGENCY TANGIBLE WELLS (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.ContigencyTangWells.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.ContigencyTangWells.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.ContigencyTangWells.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.ContigencyTangWells.MODSS);
                                    cd.RT = datainclude.Sum(x => x.ContigencyTangWells.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.ContigencyTangWells.RealTermSS);
                                }
                                else if (f == "CONTINGENCY INTANGIBLE WELLS (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.ContigencyInTangWells.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.ContigencyInTangWells.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.ContigencyInTangWells.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.ContigencyInTangWells.MODSS);
                                    cd.RT = datainclude.Sum(x => x.ContigencyInTangWells.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.ContigencyInTangWells.RealTermSS);
                                }
                                cd.DateId = yearonlystart;
                                cd.Type = "Annual";
                                cd.Column = cols;
                                ph.Details.Add(cd);
                                #endregion

                            }
                            else
                            {
                                cd.DateId = yearonlystart;
                                cd.Type = "Annual";
                                cd.Column = cols;
                                ph.Details.Add(cd);
                            }
                            #endregion

                            yearonlystart = yearonlystart + 100;
                            cols++;
                        } while (yearonlystart <= yearonlyfinish);

                        ph.Save();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                return false;
            }            
        }
    }

    public class CapexHorDetail
    {
        public int DateId { get; set; }
        public string Type { get; set; }
        public int Column { get; set; }
        public double EDM { get; set; }
        public double EDMSS { get; set; }
        public double MOD { get; set; }
        public double MODSS { get; set; }
        public double RT { get; set; }
        public double RTSS { get; set; }
    }




    public class PCapexAggHorizon : ECISModel
    {
        public override string TableName
        {
            get { return "WEISPalantirCapexAggHorizontal"; }
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            //var newId = SequenceNo.Get(this.TableName).ClaimAsInt();
            //_id = newId;
            _id = String.Format("M{0}C{1}",
                MapName.Replace(" ", "").Replace("-", ""),
                CaseName.Replace(" ", "").Replace("-", ""));
            return this.ToBsonDocument();
        }

        public string UpdateBy { get; set; }
        public string MapName { get; set; }
        public string CaseName { get; set; }
        //public string Field { get; set; }
        //public List<CapexHorizonDetail> Details { get; set; }

        public List<CapexHorizonDetailEx> HorizonDetails { get; set; }

        public PCapexAggHorizon()
        {
            UpdateBy = "";
            HorizonDetails = new List<CapexHorizonDetailEx>();
            //CaseName = "";
            //Field = "";
            //Details = new List<CapexHorizonDetail>();
        }

        List<string> CAPEXFields = new List<string>
        {
            {"CAPITAL DRILLING PD Dev Tangible (mm)" } ,
            {"CAPITAL DRILLING PD DEV INTANGIBLE (mm)" } ,
            {"CAPITAL COMPLETION PD DEV TANGIBLE (mm)" } ,
            {"CAPITAL COMPLETION PD DEV INTANGIBLE (mm)" } ,
            {"EXPEX DRILLING B2 EXPL TANGIBLE (mm)" } ,
            {"EXPEX DRILLING B2 EXPL INTANGIBLE (mm)" } ,
            {"EXPEX COMPLETION B2 EXPL TANGIBLE (mm)" } ,
            {"EXPEX COMPLETION B2 EXPL INTANGIBLE (mm)" } ,
            {"CAPITAL EXPENDITURE D&R DVA WELLS (mm)" } ,
            {"CAPITAL EXPENDITURE D&R SUBSEA WELLS (mm)" } ,
            {"OPCOSTS IDLE RIG (mm)" } ,
            {"CONTINGENCY TANGIBLE WELLS (mm)" } ,
            {"CONTINGENCY INTANGIBLE WELLS (mm)" }
        };

        public bool HorizontalAggCapex(List<PCapex> list, string username, DateTime updateVer, ParsePalentirQuery pq, string mapName, int monthIdStart = 201601, int monthIdFinish = 206612, int onlyYearValueStart = 202201)
        {
            try
            {
                // resync from bizplan
                PCapex px = new PCapex();
                //list = px.ReSyncMapFromBizplan(list, list.FirstOrDefault().MapName);

                var grp = list.GroupBy(x => x.CaseName);
                foreach (var t in grp)
                {
                    PCapexAggHorizon phs = new PCapexAggHorizon();
                    phs.CaseName = t.Key;
                    phs.UpdateBy = username;
                    phs.MapName = mapName;

                    foreach (var f in CAPEXFields)
                    {
                        CapexHorizonDetailEx chorex = new CapexHorizonDetailEx();
                        chorex.Field = f;
                        //PCapexAggHorizon ph = new PCapexAggHorizon();
                        if (monthIdStart == 202201)
                        {
                            monthIdStart = 201601;
                        }
                        //ph.CaseName = t.Key;
                        ////ph.Field = f;
                        //ph.UpdateBy = username;
                        //ph.MapName = mapName;
                        var data = t.ToList().SelectMany(x => x.CapexDetails);
                        int cols = 2;
                        do
                        {
                            #region binding the data
                            var datainclude = data.Where(x => x.DateId == monthIdStart).ToList();
                            CapexHorizonDetail cd = new CapexHorizonDetail();
                            if (datainclude.Any())
                            {
                                #region binding data

                                if (f == "CAPITAL DRILLING PD Dev Tangible (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalDrillingPDDevTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalDrillingPDDevTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalDrillingPDDevTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.RealTermSS);
                                }
                                else if (f == "CAPITAL DRILLING PD DEV INTANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.RealTermSS);
                                }
                                else if (f == "CAPITAL COMPLETION PD DEV TANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalCompletionPDDevTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalCompletionPDDevTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalCompletionPDDevTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.RealTermSS);
                                }
                                else if (f == "CAPITAL COMPLETION PD DEV INTANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.RealTermSS);
                                }
                                else if (f == "EXPEX DRILLING B2 EXPL TANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.RealTermSS);
                                }
                                else if (f == "EXPEX DRILLING B2 EXPL INTANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.RealTermSS);
                                }
                                else if (f == "EXPEX COMPLETION B2 EXPL TANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.RealTermSS);
                                }
                                else if (f == "EXPEX COMPLETION B2 EXPL INTANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.RealTermSS);
                                }
                                else if (f == "CAPITAL EXPENDITURE D&R DVA WELLS (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalExpenDRDVAWells.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalExpenDRDVAWells.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalExpenDRDVAWells.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.RealTermSS);
                                }
                                else if (f == "CAPITAL EXPENDITURE D&R SUBSEA WELLS (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.RealTermSS);
                                }
                                else if (f == "OPCOSTS IDLE RIG (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.OPCostIdleRig.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.OPCostIdleRig.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.OPCostIdleRig.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.OPCostIdleRig.MODSS);
                                    cd.RT = datainclude.Sum(x => x.OPCostIdleRig.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.OPCostIdleRig.RealTermSS);
                                }
                                else if (f == "CONTINGENCY TANGIBLE WELLS (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.ContigencyTangWells.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.ContigencyTangWells.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.ContigencyTangWells.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.ContigencyTangWells.MODSS);
                                    cd.RT = datainclude.Sum(x => x.ContigencyTangWells.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.ContigencyTangWells.RealTermSS);
                                }
                                else if (f == "CONTINGENCY INTANGIBLE WELLS (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.ContigencyInTangWells.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.ContigencyInTangWells.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.ContigencyInTangWells.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.ContigencyInTangWells.MODSS);
                                    cd.RT = datainclude.Sum(x => x.ContigencyInTangWells.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.ContigencyInTangWells.RealTermSS);
                                }
                                cd.DateId = monthIdStart;
                                cd.Type = "Monthly";
                                cd.Column = cols;
                                //ph.Details.Add(cd);
                                chorex.Details.Add(cd);
                                #endregion
                            }
                            else
                            {
                                cd.DateId = monthIdStart;
                                cd.Type = "Monthly";
                                cd.Column = cols;
                                //ph.Details.Add(cd);
                                chorex.Details.Add(cd);
                            }
                            #endregion
                            monthIdStart = px.AddMonthId(monthIdStart, 1);
                            cols++;
                        } while (monthIdStart <= monthIdFinish && (monthIdStart != onlyYearValueStart));

                        var yearonlystart = Convert.ToInt32(onlyYearValueStart);
                        var yearonlyfinish = Convert.ToInt32(monthIdFinish);
                        do
                        {
                            //aaa.Add(Convert.ToInt32(yearonlystart));
                            #region binding the data
                            var datainclude = data.Where(x => Convert.ToInt32(x.DateId.ToString().Substring(0, 4)) == Convert.ToInt32(yearonlystart.ToString().Substring(0, 4))).ToList();
                            CapexHorizonDetail cd = new CapexHorizonDetail();
                            if (datainclude.Any())
                            {
                                #region binding data

                                if (f == "CAPITAL DRILLING PD Dev Tangible (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalDrillingPDDevTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalDrillingPDDevTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalDrillingPDDevTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalDrillingPDDevTang.RealTermSS);
                                }
                                else if (f == "CAPITAL DRILLING PD DEV INTANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalDrillingPDDevInTang.RealTermSS);
                                }
                                else if (f == "CAPITAL COMPLETION PD DEV TANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalCompletionPDDevTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalCompletionPDDevTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalCompletionPDDevTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalCompletionPDDevTang.RealTermSS);
                                }
                                else if (f == "CAPITAL COMPLETION PD DEV INTANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalCompletionPDDevInTang.RealTermSS);
                                }
                                else if (f == "EXPEX DRILLING B2 EXPL TANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.EPEXDrillingB2ExplTang.RealTermSS);
                                }
                                else if (f == "EXPEX DRILLING B2 EXPL INTANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.EPEXDrillingB2ExplInTang.RealTermSS);
                                }
                                else if (f == "EXPEX COMPLETION B2 EXPL TANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.EPEXCompletionB2ExplTang.RealTermSS);
                                }
                                else if (f == "EXPEX COMPLETION B2 EXPL INTANGIBLE (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.MODSS);
                                    cd.RT = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.EPEXCompletionB2ExplInTang.RealTermSS);
                                }
                                else if (f == "CAPITAL EXPENDITURE D&R DVA WELLS (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalExpenDRDVAWells.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalExpenDRDVAWells.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalExpenDRDVAWells.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalExpenDRDVAWells.RealTermSS);
                                }
                                else if (f == "CAPITAL EXPENDITURE D&R SUBSEA WELLS (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.MODSS);
                                    cd.RT = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.CapitalExpenDRSubSeaWells.RealTermSS);
                                }
                                else if (f == "OPCOSTS IDLE RIG (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.OPCostIdleRig.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.OPCostIdleRig.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.OPCostIdleRig.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.OPCostIdleRig.MODSS);
                                    cd.RT = datainclude.Sum(x => x.OPCostIdleRig.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.OPCostIdleRig.RealTermSS);
                                }
                                else if (f == "CONTINGENCY TANGIBLE WELLS (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.ContigencyTangWells.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.ContigencyTangWells.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.ContigencyTangWells.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.ContigencyTangWells.MODSS);
                                    cd.RT = datainclude.Sum(x => x.ContigencyTangWells.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.ContigencyTangWells.RealTermSS);
                                }
                                else if (f == "CONTINGENCY INTANGIBLE WELLS (mm)")
                                {
                                    cd.EDM = datainclude.Sum(x => x.ContigencyInTangWells.EDM);
                                    cd.EDMSS = datainclude.Sum(x => x.ContigencyInTangWells.EDMSS);
                                    cd.MOD = datainclude.Sum(x => x.ContigencyInTangWells.MOD);
                                    cd.MODSS = datainclude.Sum(x => x.ContigencyInTangWells.MODSS);
                                    cd.RT = datainclude.Sum(x => x.ContigencyInTangWells.RealTerm);
                                    cd.RTSS = datainclude.Sum(x => x.ContigencyInTangWells.RealTermSS);
                                }
                                cd.DateId = yearonlystart;
                                cd.Type = "Annual";
                                cd.Column = cols;
                                //ph.Details.Add(cd);
                                chorex.Details.Add(cd);
                                #endregion

                            }
                            else
                            {
                                cd.DateId = yearonlystart;
                                cd.Type = "Annual";
                                cd.Column = cols;
                                //ph.Details.Add(cd);
                                chorex.Details.Add(cd);
                            }
                            #endregion

                            yearonlystart = yearonlystart + 100;
                            cols++;
                        } while (yearonlystart <= yearonlyfinish);

                        //ph.Save();
                        phs.HorizonDetails.Add(chorex);
                    }

                    
                    //phs.HorizonDetails
                    phs.Save();
                }

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }

    public class CapexHorizonDetailEx 
    {
        public string Field { get; set; }
        public List<CapexHorizonDetail> Details { get; set; }

        public CapexHorizonDetailEx() 
        {
            Field = "";
            Details = new List<CapexHorizonDetail>();
        }
    }

    public class CapexHorizonDetail
    {
        public int DateId { get; set; }
        public string Type { get; set; }
        public int Column { get; set; }
        public double EDM { get; set; }
        public double EDMSS { get; set; }
        public double MOD { get; set; }
        public double MODSS { get; set; }
        public double RT { get; set; }
        public double RTSS { get; set; }
    }
}
