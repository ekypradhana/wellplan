using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECIS.Client.WEIS
{

    public class STOS : ECISModel
    {
        public override string TableName
        {
            get { return "WEISPalantirSTOS"; }
        }

        #region WellActivities
        public string Region { get; set; }
        public string RigType { get; set; }
        public string RigName { get; set; }
        public string ProjectName { get; set; }
        public string AssetName { get; set; }
        public string WellName { get; set; }
        public string FirmOrOption { get; set; }
        public string UARigSequenceId { get; set; }
        #endregion


        public string ActivityName { get; set; }
        public string SpendType { get; set; } // funding Type
        public string UpdateBy { get; set; }
        public string MapName { get; set; }
        public string CostEstimator { get; set; }
        public string Status { get; set; }
        public string EstimateType { get; set; }
        public string Currency { get; set; }
        public double Contingency { get; set; }
        public string ActivityDescription { get; set; }
        public string ReferenceFactorModel { get; set; }
        //public string Escalation { get; set; }
        //public double OwnersCost { get; set; }
        public bool IsInPlan { get; set; }

        #region Mapping
        //public string ActivityDesc { get; set; }
        public string RegretConsequences { get; set; }
        public string Rank { get; set; }
        public string PMaster { get; set; }
        public string PMasterCategory { get; set; }
        public string SponsorFunction { get; set; }
        public string Revenue { get; set; }
        public string AssuranceLevel { get; set; }
        public string Regrets { get; set; }
        public double POE { get; set; }
        #endregion

        #region STOS Filter
        public string LineofBusiness { get; set; }
        //public string Asset { get; set; }
        public string FundingType { get; set; }
        #endregion

        //public List<PValueDetail> Details { get; set; }

        //public List<BizPlanAllocation> Allocations { get; set; }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            _id = String.Format("W{0}S{1}A{2}U{3}M{4}",
                ActivityName.Replace(" ", "").Replace("-", ""),
                UARigSequenceId,
                RigName.Replace(" ", "").Replace("-", ""),
                UpdateBy.Replace(" ", "").Replace("-", ""),
                MapName
                );
            return this.ToBsonDocument();
        }

        public STOS()
        {
            //Details = new List<PValueDetail>();
            //Allocations = new List<BizPlanAllocation>();
        }

        public static bool isHaveBizPlan(string well, string seq, out BizPlanActivity outbiz)
        {
            BizPlanActivity bz = null;
            var t = BizPlanActivity.Populate<BizPlanActivity>(
                Query.And(
                    Query.EQ("WellName", well),
                    Query.EQ("UARigSequenceId", seq)
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

        #region remark transform wellactivity original
        //public List<STOS> Transforms(List<WellActivity> was, string UpdateBy,
        //    int yearstart = 2015, int yearfinish = 2060)
        //{
        //    List<STOS> results = new List<STOS>();
        //    #region Header
        //    foreach (var wa in was)
        //    {
        //        BizPlanActivity bp = null;
        //        var bizplan = isHaveBizPlan(wa.WellName, wa.UARigSequenceId, out bp);

        //        var alloc = BizPlanAllocation.Populate<BizPlanAllocation>(Query.EQ("WellName", wa.WellName));

        //        STOS srig = new STOS();

        //        srig.RigType = wa.RigType;
        //        srig.RigName = wa.RigName;
        //        srig.ProjectName = wa.ProjectName;
        //        srig.AssetName = wa.AssetName;
        //        srig.WellName = wa.WellName;
        //        srig.FirmOrOption = wa.FirmOrOption;
        //        srig.UARigSequenceId = wa.UARigSequenceId;
        //        srig.Region = wa.Region;
        //        srig.FirmOrOption = wa.FirmOrOption;

        //        #region Material and Services
        //        srig.ActivityName = srig.WellName + " Mat & Serv.";
        //        srig.SpendType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().FundingType : "";
        //        srig.IsInPlan = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().IsInPlan : false;

        //        #region Mapping
        //        srig.ActivityDesc = "";
        //        srig.RegretConsequences = "";
        //        srig.Rank = "";
        //        srig.PMaster = "";
        //        srig.PMasterCategory = "";
        //        srig.SponsorFunction = "";
        //        srig.Revenue = "";
        //        srig.AssuranceLevel = "";
        //        srig.Regrets = "";
        //        srig.POE = 0.0;

        //        #endregion
        //        srig.CostEstimator = UpdateBy;

        //        srig.Status = "";
        //        srig.EstimateType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().LevelOfEstimate : "";

        //        if (bizplan && bp != null && bp.Phases.Count() > 0)
        //        {
        //            srig.OwnersCost = 0.0;
        //            srig.Contingency = bp.Phases.FirstOrDefault().Estimate.NewTECOPTime.PercentCost;
        //            srig.Currency = "USD";

        //            if (bp.ReferenceFactorModel != null && bp.ReferenceFactorModel.ToUpper().Contains("ONSHORE"))
        //                srig.Escalation = "Wells Materials/Services Onshore";
        //            else if (bp.ReferenceFactorModel != null && bp.ReferenceFactorModel.ToUpper().Contains("OFFSHORE"))
        //                srig.Escalation = "Wells Materials/Services Offshore";
        //            else
        //                srig.Escalation = "";


        //            if (alloc.Any())
        //            {
        //                srig.Allocations = alloc;

        //                foreach (var t in srig.Allocations)
        //                {
        //                    var wo = t.WorkingInterest;
        //                    foreach (var y in t.AnnualyBuckets)
        //                    {
        //                        y.WorkingInterest = wo;
        //                    }
        //                }

        //                var grouped = srig.Allocations.SelectMany(x => x.AnnualyBuckets).GroupBy(x => x.Key);
        //                grouped = grouped.OrderBy(x => x.Key);
        //                foreach (var t in grouped)
        //                {
        //                    // NPT RATE : Tools.Div( x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)) 
        //                    srig.Details.Add(new PValueDetail
        //                    {
        //                        DateId = Convert.ToInt32(t.Key),
        //                        Type = "Year",
        //                        value = t.Sum(x => (x.ServiceCost + x.MaterialCost) + (x.ServiceCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)))),
        //                        WithSSValue = t.Sum(x => ((x.ServiceCost * x.WorkingInterest) + (x.MaterialCost * x.WorkingInterest)) +
        //                            ((x.ServiceCost * x.WorkingInterest) * Tools.Div((x.NPTCost * x.WorkingInterest), ((x.TroubleFreeCost * x.WorkingInterest) - (x.MaterialCost * x.WorkingInterest)))))

        //                    });
        //                }
        //            }

        //        }

        //        #endregion
        //        results.Add(srig);

        //        srig = new STOS();
        //        srig.RigType = wa.RigType;
        //        srig.RigName = wa.RigName;
        //        srig.ProjectName = wa.ProjectName;
        //        srig.AssetName = wa.AssetName;
        //        srig.WellName = wa.WellName;
        //        srig.FirmOrOption = wa.FirmOrOption;
        //        srig.UARigSequenceId = wa.UARigSequenceId;
        //        srig.Region = wa.Region;
        //        srig.FirmOrOption = wa.FirmOrOption;
        //        #region Rig
        //        srig.ActivityName = srig.WellName + " Rig.";
        //        srig.SpendType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().FundingType : "";
        //        srig.IsInPlan = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().IsInPlan : false;

        //        #region Mapping
        //        srig.ActivityDesc = "";
        //        srig.RegretConsequences = "";
        //        srig.Rank = "";
        //        srig.PMaster = "";
        //        srig.PMasterCategory = "";
        //        srig.SponsorFunction = "";
        //        srig.Revenue = "";
        //        srig.AssuranceLevel = "";
        //        srig.Regrets = "";
        //        srig.POE = 0.0;

        //        #endregion
        //        srig.CostEstimator = UpdateBy;

        //        srig.Status = "";
        //        srig.EstimateType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().LevelOfEstimate : "";


        //        if (bizplan && bp != null && bp.Phases.Count() > 0)
        //        {
        //            srig.OwnersCost = 0.0;
        //            srig.Contingency = bp.Phases.FirstOrDefault().Estimate.NewTECOPTime.PercentCost;
        //            srig.Currency = "USD";
        //            if (bp.ReferenceFactorModel != null && bp.ReferenceFactorModel.ToUpper().Contains("ONSHORE"))
        //                srig.Escalation = "Land Rigs";
        //            else if (bp.ReferenceFactorModel != null && bp.ReferenceFactorModel.ToUpper().Contains("OFFSHORE"))
        //                srig.Escalation = "Offshore Rigs";
        //            else
        //                srig.Escalation = "";


        //            if (alloc.Any())
        //            {
        //                srig.Allocations = alloc;

        //                foreach (var t in srig.Allocations)
        //                {
        //                    var wo = t.WorkingInterest;
        //                    foreach (var y in t.AnnualyBuckets)
        //                    {
        //                        y.WorkingInterest = wo;
        //                    }
        //                }

        //                var grouped = srig.Allocations.SelectMany(x => x.AnnualyBuckets).GroupBy(x => x.Key);
        //                grouped = grouped.OrderBy(x => x.Key);
        //                foreach (var t in grouped)
        //                {
        //                    // NPT RATE : Tools.Div( x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)) 
        //                    srig.Details.Add(new PValueDetail
        //                    {
        //                        DateId = Convert.ToInt32(t.Key),
        //                        Type = "Year",
        //                        value = t.Sum(x => x.RigCost + (x.RigCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)))),
        //                        WithSSValue = t.Sum(x => (x.RigCost * x.WorkingInterest) + ((x.RigCost * x.WorkingInterest) * Tools.Div(
        //                            (x.NPTCost * x.WorkingInterest), ((x.TroubleFreeCost * x.WorkingInterest) - (x.MaterialCost * x.WorkingInterest)))))
        //                        //,
        //                        //WithSSValue = t.Sum(x => (x.RigCost *  )  + (x.RigCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost))))

        //                    });
        //                }
        //            }

        //        }

        //        #endregion

        //        results.Add(srig);

        //    }
        //    #endregion



        //    return results;
        //}
        #endregion

        #region remark transform bizplan original
        //public List<STOS> Transforms(List<BizPlanActivity> was, string UpdateBy,
        //int yearstart = 2015, int yearfinish = 2060)
        //{
        //    List<STOS> results = new List<STOS>();
        //    #region Header
        //    foreach (var wa in was)
        //    {
        //        BizPlanActivity bp = wa;
        //        //var bizplan = isHaveBizPlan(wa.WellName, wa.UARigSequenceId, out bp);

        //        var alloc = BizPlanAllocation.Populate<BizPlanAllocation>(Query.EQ("WellName", wa.WellName));

        //        STOS srig = new STOS();

        //        srig.RigType = wa.RigType;
        //        srig.RigName = wa.RigName;
        //        srig.ProjectName = wa.ProjectName;
        //        srig.AssetName = wa.AssetName;
        //        srig.WellName = wa.WellName;
        //        srig.FirmOrOption = wa.FirmOrOption;
        //        srig.UARigSequenceId = wa.UARigSequenceId;
        //        srig.Region = wa.Region;
        //        srig.FirmOrOption = wa.FirmOrOption;

        //        #region Material and Services
        //        srig.ActivityName = srig.WellName + " Mat & Serv.";
        //        srig.SpendType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().FundingType : "";
        //        srig.IsInPlan = wa.Phases.Count() > 0 ? wa.isInPlan : false;// wa.Phases.FirstOrDefault().IsInPlan : false;
        //        srig.LineofBusiness = wa.LineOfBusiness != "JV/NOV" ? wa.LineOfBusiness : "";

        //        #region Mapping
        //        srig.ActivityDesc = "";
        //        srig.RegretConsequences = "";
        //        srig.Rank = "";
        //        srig.PMaster = "";
        //        srig.PMasterCategory = "";
        //        srig.SponsorFunction = "";
        //        srig.Revenue = "";
        //        srig.AssuranceLevel = "";
        //        srig.Regrets = "";
        //        srig.POE = 0.0;

        //        #endregion
        //        srig.CostEstimator = UpdateBy;

        //        srig.Status = "";
        //        srig.EstimateType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().LevelOfEstimate : "";

        //        if (bp != null && bp.Phases.Count() > 0)
        //        {
        //            srig.OwnersCost = 0.0;
        //            srig.Contingency = bp.Phases.FirstOrDefault().Estimate.NewTECOPTime.PercentCost;
        //            srig.Currency = "USD";

        //            if (bp.ReferenceFactorModel != null && bp.ReferenceFactorModel.ToUpper().Contains("ONSHORE"))
        //                srig.Escalation = "Wells Materials/Services Onshore";
        //            else if (bp.ReferenceFactorModel != null && bp.ReferenceFactorModel.ToUpper().Contains("OFFSHORE"))
        //                srig.Escalation = "Wells Materials/Services Offshore";
        //            else
        //                srig.Escalation = "";

        //            if (alloc.Any())
        //            {
        //                srig.Allocations = alloc;

        //                foreach (var t in srig.Allocations)
        //                {
        //                    var wo = t.WorkingInterest;
        //                    foreach (var y in t.AnnualyBuckets)
        //                    {
        //                        y.WorkingInterest = wo;
        //                    }
        //                }

        //                var grouped = srig.Allocations.SelectMany(x => x.AnnualyBuckets).GroupBy(x => x.Key);
        //                grouped = grouped.OrderBy(x => x.Key);
        //                foreach (var t in grouped)
        //                {
        //                    // NPT RATE : Tools.Div( x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)) 
        //                    srig.Details.Add(new PValueDetail
        //                    {
        //                        DateId = Convert.ToInt32(t.Key),
        //                        Type = "Year",
        //                        value = t.Sum(x => (x.ServiceCost + x.MaterialCost) + (x.ServiceCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)))),
        //                        WithSSValue = t.Sum(x => ((x.ServiceCost * x.WorkingInterest) + (x.MaterialCost * x.WorkingInterest)) +
        //                            ((x.ServiceCost * x.WorkingInterest) * Tools.Div((x.NPTCost * x.WorkingInterest), ((x.TroubleFreeCost * x.WorkingInterest) - (x.MaterialCost * x.WorkingInterest)))))

        //                    });
        //                }
        //            }

        //        }

        //        #endregion
        //        results.Add(srig);

        //        srig = new STOS();
        //        srig.RigType = wa.RigType;
        //        srig.RigName = wa.RigName;
        //        srig.ProjectName = wa.ProjectName;
        //        srig.AssetName = wa.AssetName;
        //        srig.WellName = wa.WellName;
        //        srig.FirmOrOption = wa.FirmOrOption;
        //        srig.UARigSequenceId = wa.UARigSequenceId;
        //        srig.Region = wa.Region;
        //        srig.FirmOrOption = wa.FirmOrOption;
        //        #region Rig
        //        srig.ActivityName = srig.WellName + " Rig.";
        //        srig.SpendType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().FundingType : "";
        //        srig.IsInPlan = wa.Phases.Count() > 0 ? wa.isInPlan : false;// wa.Phases.FirstOrDefault().IsInPlan : false;
        //        srig.LineofBusiness = wa.LineOfBusiness;

        //        #region Mapping
        //        srig.ActivityDesc = "";
        //        srig.RegretConsequences = "";
        //        srig.Rank = "";
        //        srig.PMaster = "";
        //        srig.PMasterCategory = "";
        //        srig.SponsorFunction = "";
        //        srig.Revenue = "";
        //        srig.AssuranceLevel = "";
        //        srig.Regrets = "";
        //        srig.POE = 0.0;

        //        #endregion
        //        srig.CostEstimator = UpdateBy;

        //        srig.Status = "";
        //        srig.EstimateType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().LevelOfEstimate : "";


        //        if (bp != null && bp.Phases.Count() > 0)
        //        {
        //            srig.OwnersCost = 0.0;
        //            srig.Contingency = bp.Phases.FirstOrDefault().Estimate.NewTECOPTime.PercentCost;
        //            srig.Currency = "USD";
        //            if (bp.ReferenceFactorModel != null && bp.ReferenceFactorModel.ToUpper().Contains("ONSHORE"))
        //                srig.Escalation = "Land Rigs";
        //            else if (bp.ReferenceFactorModel != null && bp.ReferenceFactorModel.ToUpper().Contains("OFFSHORE"))
        //                srig.Escalation = "Offshore Rigs";
        //            else
        //                srig.Escalation = "";


        //            if (alloc.Any())
        //            {
        //                srig.Allocations = alloc;

        //                foreach (var t in srig.Allocations)
        //                {
        //                    var wo = t.WorkingInterest;
        //                    foreach (var y in t.AnnualyBuckets)
        //                    {
        //                        y.WorkingInterest = wo;
        //                    }
        //                }

        //                var grouped = srig.Allocations.SelectMany(x => x.AnnualyBuckets).GroupBy(x => x.Key);
        //                grouped = grouped.OrderBy(x => x.Key);
        //                foreach (var t in grouped)
        //                {
        //                    // NPT RATE : Tools.Div( x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)) 
        //                    srig.Details.Add(new PValueDetail
        //                    {
        //                        DateId = Convert.ToInt32(t.Key),
        //                        Type = "Year",
        //                        value = t.Sum(x => x.RigCost + (x.RigCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)))),
        //                        WithSSValue = t.Sum(x => (x.RigCost * x.WorkingInterest) + ((x.RigCost * x.WorkingInterest) * Tools.Div(
        //                            (x.NPTCost * x.WorkingInterest), ((x.TroubleFreeCost * x.WorkingInterest) - (x.MaterialCost * x.WorkingInterest)))))
        //                        //,
        //                        //WithSSValue = t.Sum(x => (x.RigCost *  )  + (x.RigCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost))))

        //                    });
        //                }
        //            }

        //        }

        //        #endregion

        //        results.Add(srig);

        //    }
        //    #endregion



        //    return results;
        //}
        #endregion

        public List<STOS> TransformsToMapBizPlanAs(BizPlanActivity wa, string UpdateBy,
        int yearstart = 2015, int yearfinish = 2060)
        {
            List<STOS> results = new List<STOS>();
            #region Header
            BizPlanActivity bp = wa;
            STOS srig = new STOS();

            srig.UpdateBy = UpdateBy;
            srig.RigType = wa.RigType;
            srig.RigName = wa.RigName;
            srig.ProjectName = wa.ProjectName;
            srig.AssetName = wa.AssetName;
            srig.WellName = wa.WellName;
            srig.FirmOrOption = wa.FirmOrOption;
            srig.UARigSequenceId = wa.UARigSequenceId;
            srig.Region = wa.Region;
            srig.FirmOrOption = wa.FirmOrOption;

            #region header map
            srig.ActivityName = srig.WellName;
            srig.SpendType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().FundingType : "";
            srig.IsInPlan = wa.Phases.Count() > 0 ? wa.isInPlan : false;
            srig.LineofBusiness = wa.LineOfBusiness;
            srig.CostEstimator = UpdateBy;

            srig.Status = "";
            srig.EstimateType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().LevelOfEstimate : "";

            if (bp != null && bp.Phases.Count() > 0)
            {
                srig.Contingency = bp.Phases.FirstOrDefault().Estimate.NewTECOPTime.PercentCost;
                srig.Currency = bp.Phases.FirstOrDefault().Estimate.Currency; //"USD";
                srig.ReferenceFactorModel = bp.ReferenceFactorModel;
            }

            #endregion
            results.Add(srig);
            #endregion
            return results;
        }

        public List<STOS> TransformsToMapBizPlan(List<BizPlanActivity> was, string UpdateBy,
        int yearstart = 2015, int yearfinish = 2060)
        {
            List<STOS> results = new List<STOS>();
            #region Header
            foreach (var wa in was)
            {
                BizPlanActivity bp = wa;
                //var bizplan = isHaveBizPlan(wa.WellName, wa.UARigSequenceId, out bp);

                ////var alloc = BizPlanAllocation.Populate<BizPlanAllocation>(Query.EQ("WellName", wa.WellName));

                STOS srig = new STOS();

                srig.UpdateBy = UpdateBy;
                srig.RigType = wa.RigType;
                srig.RigName = wa.RigName;
                srig.ProjectName = wa.ProjectName;
                srig.AssetName = wa.AssetName;
                srig.WellName = wa.WellName;
                srig.FirmOrOption = wa.FirmOrOption;
                srig.UARigSequenceId = wa.UARigSequenceId;
                srig.Region = wa.Region;
                srig.FirmOrOption = wa.FirmOrOption;

                #region header map
                srig.ActivityName = srig.WellName;
                srig.SpendType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().FundingType : "";
                srig.IsInPlan = wa.Phases.Count() > 0 ? wa.isInPlan : false;
                srig.LineofBusiness = wa.LineOfBusiness;
                srig.CostEstimator = UpdateBy;

                srig.Status = "";
                srig.EstimateType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().LevelOfEstimate : "";

                if (bp != null && bp.Phases.Count() > 0)
                {
                    srig.Contingency = bp.Phases.FirstOrDefault().Estimate.NewTECOPTime.PercentCost;
                    srig.Currency = bp.Phases.FirstOrDefault().Estimate.Currency; //"USD";
                    srig.ReferenceFactorModel = bp.ReferenceFactorModel;

                    #region Details & Allocations
                    //if (alloc.Any())
                    //{
                    //    srig.Allocations = alloc;

                    //    foreach (var t in srig.Allocations)
                    //    {
                    //        var wo = t.WorkingInterest;
                    //        foreach (var y in t.AnnualyBuckets)
                    //        {
                    //            y.WorkingInterest = wo;
                    //        }
                    //    }

                    //    var grouped = srig.Allocations.SelectMany(x => x.AnnualyBuckets).GroupBy(x => x.Key);
                    //    grouped = grouped.OrderBy(x => x.Key);
                    //    foreach (var t in grouped)
                    //    {
                    //        // NPT RATE : Tools.Div( x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)) 
                    //        srig.Details.Add(new PValueDetail
                    //        {
                    //            DateId = Convert.ToInt32(t.Key),
                    //            Type = "Year",
                    //            value = t.Sum(x => (x.ServiceCost + x.MaterialCost) + (x.ServiceCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)))),
                    //            WithSSValue = t.Sum(x => ((x.ServiceCost * x.WorkingInterest) + (x.MaterialCost * x.WorkingInterest)) +
                    //                ((x.ServiceCost * x.WorkingInterest) * Tools.Div((x.NPTCost * x.WorkingInterest), ((x.TroubleFreeCost * x.WorkingInterest) - (x.MaterialCost * x.WorkingInterest)))))

                    //        });
                    //    }
                    //}
                    #endregion 

                }
                
                #endregion
                results.Add(srig);
            }
            #endregion



            return results;
        }

        //string ActivityDesc,
        //    string RegretConsequences, string Rank, string PMaster, string PMasterCategory, string SponsorFunction,
        //    string Revenue, string AssuranceLevel, string Regrets, double POE,
        public List<STOSResult> TransformsRes(List<STOS> listSTOS, string UpdateBy, 
            int yearstart = 2016, int yearfinish = 2066) 
        {
            List<STOSResult> results = new List<STOSResult>();
            List<PValueDetail> valueDetailsMS = new List<PValueDetail>();
            List<PValueDetail> valueDetailsRig = new List<PValueDetail>();

            #region Header
            foreach (var wa in listSTOS)
            {
                //BizPlanActivity bp = new BizPlanActivity();//wa;
                var alloc = BizPlanAllocation.Populate<BizPlanAllocation>(Query.EQ("WellName", wa.WellName));

                STOSResult srig = new STOSResult();

                srig.UpdateBy = UpdateBy;
                srig.RigType = wa.RigType;
                srig.RigName = wa.RigName;
                srig.ProjectName = wa.ProjectName;
                srig.AssetName = wa.AssetName;
                srig.WellName = wa.WellName;
                srig.FirmOrOption = wa.FirmOrOption;
                srig.UARigSequenceId = wa.UARigSequenceId;
                srig.Region = wa.Region;
                srig.FirmOrOption = wa.FirmOrOption;

                #region Material and Services
                srig.ActivityName = srig.WellName + " Mat & Serv.";
                srig.SpendType = wa.SpendType;
                srig.IsInPlan = wa.IsInPlan; // wa.Phases.FirstOrDefault().IsInPlan : false;
                srig.LineofBusiness = wa.LineofBusiness != "JV/NOV" ? wa.LineofBusiness : "";

                #region Mapping
                srig.ActivityDesc = wa.ActivityDescription;
                srig.RegretConsequences = wa.RegretConsequences;
                srig.Rank = wa.Rank;
                srig.PMaster = wa.PMaster;
                srig.PMasterCategory = wa.PMasterCategory;
                srig.SponsorFunction = wa.SponsorFunction;
                srig.Revenue = wa.Revenue;
                srig.AssuranceLevel = wa.AssuranceLevel;
                srig.Regrets = wa.Regrets;
                srig.POE = wa.POE;

                #endregion
                #region Material and Services Cost
                srig.CostEstimator = UpdateBy;

                srig.Status = "";
                srig.EstimateType = wa.EstimateType;

                
                    srig.OwnersCost = 0.0;
                    srig.Contingency = wa.Contingency;//bp.Phases.FirstOrDefault().Estimate.NewTECOPTime.PercentCost;
                    srig.Currency = wa.Currency;

                    if (wa.ReferenceFactorModel != null && wa.ReferenceFactorModel.ToUpper().Contains("ONSHORE"))
                        srig.Escalation = "Wells Materials/Services Onshore";
                    else if (wa.ReferenceFactorModel != null && wa.ReferenceFactorModel.ToUpper().Contains("OFFSHORE"))
                        srig.Escalation = "Wells Materials/Services Offshore";
                    else
                        srig.Escalation = "";

                    #region Details & Allocations
                    if (alloc.Any())
                    {
                        srig.Allocations = alloc;

                        foreach (var t in srig.Allocations)
                        {
                            var wo = t.WorkingInterest;
                            foreach (var y in t.AnnualyBuckets)
                            {
                                y.WorkingInterest = wo;
                            }
                        }

                        var yearstartms = yearstart;
                        var yearfinishms = yearfinish;

                        //var grouped = srig.Allocations.SelectMany(x => x.AnnualyBuckets).GroupBy(x => x.Key);
                        //grouped = grouped.OrderBy(x => x.Key);
                        do
                        {
                            var grouped = srig.Allocations.SelectMany(x => x.AnnualyBuckets).GroupBy(x => x.Key);
                            grouped = grouped.OrderBy(x => x.Key);
                            foreach (var t in grouped)
                            {
                                // NPT RATE : Tools.Div( x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)) 
                                if (yearstartms.ToString() == t.Key)
                                {
                                    valueDetailsMS.Add(new PValueDetail
                                    {
                                        DateId = yearstartms,//Convert.ToInt32(t.Key),
                                        Type = "Year",
                                        value = t.Sum(x => (x.ServiceCost + x.MaterialCost) + (x.ServiceCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)))),
                                        valueSS = t.Sum(x => (x.ServiceCost + x.MaterialCost) + (x.ServiceCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)))),
                                        WithSSValue = t.Sum(x => ((x.ServiceCost * x.WorkingInterest) + (x.MaterialCost * x.WorkingInterest)) +
                                            ((x.ServiceCost * x.WorkingInterest) * Tools.Div((x.NPTCost * x.WorkingInterest), ((x.TroubleFreeCost * x.WorkingInterest) - (x.MaterialCost * x.WorkingInterest)))))

                                    });
                                }
                                else
                                {
                                    valueDetailsMS.Add(new PValueDetail
                                    {
                                        DateId = yearstartms,
                                        Type = "Year",
                                        value = 0,
                                        valueSS = 0,
                                        WithSSValue = 0
                                    });
                                }
                            }
                            yearstartms = yearstartms + 1;
                        } while (yearstartms <= yearfinishms);

                        var tResMa = valueDetailsMS.GroupBy(x => x.DateId);
                        foreach (var y in tResMa)
                        {
                            srig.Details.Add(new PValueDetail
                            {
                                DateId = y.Key,
                                Type = y.FirstOrDefault().Type,
                                value = y.Sum(x => x.value),
                                valueSS = y.Sum(x => x.valueSS),
                                WithSSValue = y.Sum(x => x.WithSSValue)
                            });

                        }
                        
                    }
                    #endregion

                    
                
                #endregion Material and Services Cost

                #endregion
                results.Add(srig);

                srig = new STOSResult();
                srig.UpdateBy = UpdateBy;
                srig.RigType = wa.RigType;
                srig.RigName = wa.RigName;
                srig.ProjectName = wa.ProjectName;
                srig.AssetName = wa.AssetName;
                srig.WellName = wa.WellName;
                srig.FirmOrOption = wa.FirmOrOption;
                srig.UARigSequenceId = wa.UARigSequenceId;
                srig.Region = wa.Region;
                srig.FirmOrOption = wa.FirmOrOption;
                #region Rig
                srig.ActivityName = srig.WellName + " Rig.";
                srig.SpendType = wa.SpendType;
                srig.IsInPlan = wa.IsInPlan;// wa.Phases.FirstOrDefault().IsInPlan : false;
                srig.LineofBusiness = wa.LineofBusiness;

                #region Mapping
                srig.ActivityDesc = wa.ActivityDescription;
                srig.RegretConsequences = wa.RegretConsequences;
                srig.Rank = wa.Rank;
                srig.PMaster = wa.PMaster;
                srig.PMasterCategory = wa.PMasterCategory;
                srig.SponsorFunction = wa.SponsorFunction;
                srig.Revenue = wa.Revenue;
                srig.AssuranceLevel = wa.AssuranceLevel;
                srig.Regrets = wa.Regrets;
                srig.POE = wa.POE;

                #endregion
                #region Rig Cost
                srig.CostEstimator = UpdateBy;

                srig.Status = "";
                srig.EstimateType = wa.EstimateType;

                
                    srig.OwnersCost = 0.0;
                    srig.Contingency = wa.Contingency;
                    srig.Currency = "USD";
                    if (wa.ReferenceFactorModel != null && wa.ReferenceFactorModel.ToUpper().Contains("ONSHORE"))
                        srig.Escalation = "Land Rigs";
                    else if (wa.ReferenceFactorModel != null && wa.ReferenceFactorModel.ToUpper().Contains("OFFSHORE"))
                        srig.Escalation = "Offshore Rigs";
                    else
                        srig.Escalation = "";

                    #region Details & Allocations
                    if (alloc.Any())
                    {
                        srig.Allocations = alloc;

                        foreach (var t in srig.Allocations)
                        {
                            var wo = t.WorkingInterest;
                            foreach (var y in t.AnnualyBuckets)
                            {
                                y.WorkingInterest = wo;
                            }
                        }

                        var yearstartrig = yearstart;
                        var yearfinishrig = yearfinish;
                        do
                        {
                            var grouped = srig.Allocations.SelectMany(x => x.AnnualyBuckets).GroupBy(x => x.Key);
                            grouped = grouped.OrderBy(x => x.Key);
                            foreach (var t in grouped)
                            {
                                if (yearstartrig.ToString() == t.Key)
                                {
                                    // NPT RATE : Tools.Div( x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)) 
                                    valueDetailsRig.Add(new PValueDetail
                                    {
                                        DateId = yearstartrig,//Convert.ToInt32(t.Key),
                                        Type = "Year",
                                        value = t.Sum(x => x.RigCost + (x.RigCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)))),
                                        valueSS = t.Sum(x => x.RigCost + (x.RigCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)))),
                                        WithSSValue = t.Sum(x => (x.RigCost * x.WorkingInterest) + ((x.RigCost * x.WorkingInterest) * Tools.Div(
                                            (x.NPTCost * x.WorkingInterest), ((x.TroubleFreeCost * x.WorkingInterest) - (x.MaterialCost * x.WorkingInterest)))))
                                        //,
                                        //WithSSValue = t.Sum(x => (x.RigCost *  )  + (x.RigCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost))))

                                    });
                                }
                                else
                                {
                                    valueDetailsRig.Add(new PValueDetail
                                    {
                                        DateId = yearstartrig,
                                        Type = "Year",
                                        value = 0,
                                        valueSS = 0,
                                        WithSSValue = 0
                                    });
                                }
                            }
                            yearstartrig = yearstartrig + 1;
                        } while (yearstartrig <= yearfinishrig);

                        var tResRig = valueDetailsRig.GroupBy(x => x.DateId);
                        foreach (var y in tResRig)
                        {
                            srig.Details.Add(new PValueDetail
                            {
                                DateId = y.Key,
                                Type = y.FirstOrDefault().Type,
                                value = y.Sum(x => x.value),
                                valueSS = y.Sum(x => x.valueSS),
                                WithSSValue = y.Sum(x => x.WithSSValue)
                            });

                        }
                    }
                    #endregion

                #endregion

                #endregion

                results.Add(srig);

            }
            #endregion

            return results;
        }
    }

    public class STOSResult : ECISModel
    {
        public override string TableName
        {
            get { return "WEISPalantirSTOSResult"; }
        }

        #region WellActivities
        public string Region { get; set; }
        public string RigType { get; set; }
        public string RigName { get; set; }
        public string ProjectName { get; set; }
        public string AssetName { get; set; }
        public string WellName { get; set; }
        public string FirmOrOption { get; set; }
        public string UARigSequenceId { get; set; }
        #endregion


        public string ActivityName { get; set; }
        //public string Project { get; set; }
        public string SpendType { get; set; } // funding Type
        public string UpdateBy { get; set; }
        public string MapName { get; set; }

        #region Mapping
        public string ActivityDesc { get; set; }
        public string RegretConsequences { get; set; }
        public string Rank { get; set; }
        public string PMaster { get; set; }
        public string PMasterCategory { get; set; }
        public string SponsorFunction { get; set; }
        public string Revenue { get; set; }
        public string AssuranceLevel { get; set; }
        public string Regrets { get; set; }
        public double POE { get; set; }
        #endregion

        public string CostEstimator { get; set; }        
        public string Status { get; set; }
        public string EstimateType { get; set; }        
        public string Currency { get; set; }        
        public string Escalation { get; set; }
        public double OwnersCost { get; set; }
        public double Contingency { get; set; }
        public bool IsInPlan { get; set; }

        #region STOS Filter
        public string LineofBusiness { get; set; }
        //public List<string> Asset { get; set; }
        public string FundingType { get; set; }
        #endregion

        public List<PValueDetail> Details { get; set; }

        public List<BizPlanAllocation> Allocations { get; set; }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            _id = String.Format("W{0}S{1}A{2}U{3}M{4}",
                ActivityName.Replace(" ", "").Replace("-", ""),
                UARigSequenceId,
                RigName.Replace(" ", "").Replace("-", ""),
                UpdateBy.Replace(" ", "").Replace("-", ""),
                MapName
                );
            return this.ToBsonDocument();
        }

        public STOSResult()
        {
            Details = new List<PValueDetail>();
            Allocations = new List<BizPlanAllocation>();
        }

        public static bool isHaveBizPlan(string well, string seq, out BizPlanActivity outbiz)
        {
            BizPlanActivity bz = null;
            var t = BizPlanActivity.Populate<BizPlanActivity>(
                Query.And(
                    Query.EQ("WellName", well),
                    Query.EQ("UARigSequenceId", seq)
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

        public List<STOSResult> Transforms(List<WellActivity> was, string UpdateBy,
            int yearstart = 2015, int yearfinish = 2060)
        {
            List<STOSResult> results = new List<STOSResult>();
            #region Header
            foreach (var wa in was)
            {
                BizPlanActivity bp = null;
                var bizplan = isHaveBizPlan(wa.WellName, wa.UARigSequenceId, out bp);

                var alloc = BizPlanAllocation.Populate<BizPlanAllocation>(Query.EQ("WellName", wa.WellName));

                STOSResult srig = new STOSResult();

                srig.RigType = wa.RigType;
                srig.RigName = wa.RigName;
                srig.ProjectName = wa.ProjectName;
                srig.AssetName = wa.AssetName;
                srig.WellName = wa.WellName;
                srig.FirmOrOption = wa.FirmOrOption;
                srig.UARigSequenceId = wa.UARigSequenceId;
                srig.Region = wa.Region;
                srig.FirmOrOption = wa.FirmOrOption;

                #region Material and Services
                srig.ActivityName = srig.WellName + " Mat & Serv.";
                srig.SpendType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().FundingType : "";
                //srig.IsInPlan = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().IsInPlan : false;

                #region Mapping
                srig.ActivityDesc = "";
                srig.RegretConsequences = "";
                srig.Rank = "";
                srig.PMaster = "";
                srig.PMasterCategory = "";
                srig.SponsorFunction = "";
                srig.Revenue = "";
                srig.AssuranceLevel = "";
                srig.Regrets = "";
                srig.POE = 0.0;

                #endregion
                srig.CostEstimator = UpdateBy;

                srig.Status = "";
                srig.EstimateType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().LevelOfEstimate : "";

                if (bizplan && bp != null && bp.Phases.Count() > 0)
                {
                    srig.OwnersCost = 0.0;
                    srig.Contingency = bp.Phases.FirstOrDefault().Estimate.NewTECOPTime.PercentCost;
                    srig.Currency = "USD";

                    if (bp.ReferenceFactorModel != null && bp.ReferenceFactorModel.ToUpper().Contains("ONSHORE"))
                        srig.Escalation = "Wells Materials/Services Onshore";
                    else if (bp.ReferenceFactorModel != null && bp.ReferenceFactorModel.ToUpper().Contains("OFFSHORE"))
                        srig.Escalation = "Wells Materials/Services Offshore";
                    else
                        srig.Escalation = "";


                    if (alloc.Any())
                    {
                        srig.Allocations = alloc;

                        foreach (var t in srig.Allocations)
                        {
                            var wo = t.WorkingInterest;
                            foreach (var y in t.AnnualyBuckets)
                            {
                                y.WorkingInterest = wo;
                            }
                        }

                        var grouped = srig.Allocations.SelectMany(x => x.AnnualyBuckets).GroupBy(x => x.Key);
                        grouped = grouped.OrderBy(x => x.Key);
                        foreach (var t in grouped)
                        {
                            // NPT RATE : Tools.Div( x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)) 
                            srig.Details.Add(new PValueDetail
                            {
                                DateId = Convert.ToInt32(t.Key),
                                Type = "Year",
                                value = t.Sum(x => (x.ServiceCost + x.MaterialCost) + (x.ServiceCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)))),
                                WithSSValue = t.Sum(x => ((x.ServiceCost * x.WorkingInterest) + (x.MaterialCost * x.WorkingInterest)) +
                                    ((x.ServiceCost * x.WorkingInterest) * Tools.Div((x.NPTCost * x.WorkingInterest), ((x.TroubleFreeCost * x.WorkingInterest) - (x.MaterialCost * x.WorkingInterest)))))

                            });
                        }
                    }

                }

                #endregion
                results.Add(srig);

                srig = new STOSResult();
                srig.RigType = wa.RigType;
                srig.RigName = wa.RigName;
                srig.ProjectName = wa.ProjectName;
                srig.AssetName = wa.AssetName;
                srig.WellName = wa.WellName;
                srig.FirmOrOption = wa.FirmOrOption;
                srig.UARigSequenceId = wa.UARigSequenceId;
                srig.Region = wa.Region;
                srig.FirmOrOption = wa.FirmOrOption;
                #region Rig
                srig.ActivityName = srig.WellName + " Rig.";
                srig.SpendType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().FundingType : "";
                //srig.IsInPlan = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().IsInPlan : false;

                #region Mapping
                srig.ActivityDesc = "";
                srig.RegretConsequences = "";
                srig.Rank = "";
                srig.PMaster = "";
                srig.PMasterCategory = "";
                srig.SponsorFunction = "";
                srig.Revenue = "";
                srig.AssuranceLevel = "";
                srig.Regrets = "";
                srig.POE = 0.0;

                #endregion
                srig.CostEstimator = UpdateBy;

                srig.Status = "";
                srig.EstimateType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().LevelOfEstimate : "";


                if (bizplan && bp != null && bp.Phases.Count() > 0)
                {
                    srig.OwnersCost = 0.0;
                    srig.Contingency = bp.Phases.FirstOrDefault().Estimate.NewTECOPTime.PercentCost;
                    srig.Currency = "USD";
                    if (bp.ReferenceFactorModel != null && bp.ReferenceFactorModel.ToUpper().Contains("ONSHORE"))
                        srig.Escalation = "Land Rigs";
                    else if (bp.ReferenceFactorModel != null && bp.ReferenceFactorModel.ToUpper().Contains("OFFSHORE"))
                        srig.Escalation = "Offshore Rigs";
                    else
                        srig.Escalation = "";


                    if (alloc.Any())
                    {
                        srig.Allocations = alloc;

                        foreach (var t in srig.Allocations)
                        {
                            var wo = t.WorkingInterest;
                            foreach (var y in t.AnnualyBuckets)
                            {
                                y.WorkingInterest = wo;
                            }
                        }

                        var grouped = srig.Allocations.SelectMany(x => x.AnnualyBuckets).GroupBy(x => x.Key);
                        grouped = grouped.OrderBy(x => x.Key);
                        foreach (var t in grouped)
                        {
                            // NPT RATE : Tools.Div( x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)) 
                            srig.Details.Add(new PValueDetail
                            {
                                DateId = Convert.ToInt32(t.Key),
                                Type = "Year",
                                value = t.Sum(x => x.RigCost + (x.RigCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)))),
                                WithSSValue = t.Sum(x => (x.RigCost * x.WorkingInterest) + ((x.RigCost * x.WorkingInterest) * Tools.Div(
                                    (x.NPTCost * x.WorkingInterest), ((x.TroubleFreeCost * x.WorkingInterest) - (x.MaterialCost * x.WorkingInterest)))))
                                //,
                                //WithSSValue = t.Sum(x => (x.RigCost *  )  + (x.RigCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost))))

                            });
                        }
                    }

                }

                #endregion

                results.Add(srig);

            }
            #endregion



            return results;
        }

        public List<STOSResult> Transforms(List<BizPlanActivity> was, string UpdateBy,
        int yearstart = 2015, int yearfinish = 2060)
        {
            List<STOSResult> results = new List<STOSResult>();
            #region Header
            foreach (var wa in was)
            {
                BizPlanActivity bp = wa;
                //var bizplan = isHaveBizPlan(wa.WellName, wa.UARigSequenceId, out bp);

                var alloc = BizPlanAllocation.Populate<BizPlanAllocation>(Query.EQ("WellName", wa.WellName));

                STOSResult srig = new STOSResult();

                srig.RigType = wa.RigType;
                srig.RigName = wa.RigName;
                srig.ProjectName = wa.ProjectName;
                srig.AssetName = wa.AssetName;
                srig.WellName = wa.WellName;
                srig.FirmOrOption = wa.FirmOrOption;
                srig.UARigSequenceId = wa.UARigSequenceId;
                srig.Region = wa.Region;
                srig.FirmOrOption = wa.FirmOrOption;

                #region Material and Services
                srig.ActivityName = srig.WellName + " Mat & Serv.";
                srig.SpendType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().FundingType : "";
                //srig.IsInPlan = wa.Phases.Count() > 0 ? wa.isInPlan : false;// wa.Phases.FirstOrDefault().IsInPlan : false;
                srig.LineofBusiness = wa.LineOfBusiness != "JV/NOV" ? wa.LineOfBusiness : "";

                #region Mapping
                srig.ActivityDesc = "";
                srig.RegretConsequences = "";
                srig.Rank = "";
                srig.PMaster = "";
                srig.PMasterCategory = "";
                srig.SponsorFunction = "";
                srig.Revenue = "";
                srig.AssuranceLevel = "";
                srig.Regrets = "";
                srig.POE = 0.0;

                #endregion
                #region Material and Services Cost
                srig.CostEstimator = UpdateBy;

                srig.Status = "";
                srig.EstimateType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().LevelOfEstimate : "";

                if (bp != null && bp.Phases.Count() > 0)
                {
                    srig.OwnersCost = 0.0;
                    srig.Contingency = bp.Phases.FirstOrDefault().Estimate.NewTECOPTime.PercentCost;
                    srig.Currency = "USD";

                    if (bp.ReferenceFactorModel != null && bp.ReferenceFactorModel.ToUpper().Contains("ONSHORE"))
                        srig.Escalation = "Wells Materials/Services Onshore";
                    else if (bp.ReferenceFactorModel != null && bp.ReferenceFactorModel.ToUpper().Contains("OFFSHORE"))
                        srig.Escalation = "Wells Materials/Services Offshore";
                    else
                        srig.Escalation = "";

                    if (alloc.Any())
                    {
                        srig.Allocations = alloc;

                        foreach (var t in srig.Allocations)
                        {
                            var wo = t.WorkingInterest;
                            foreach (var y in t.AnnualyBuckets)
                            {
                                y.WorkingInterest = wo;
                            }
                        }

                        var grouped = srig.Allocations.SelectMany(x => x.AnnualyBuckets).GroupBy(x => x.Key);
                        grouped = grouped.OrderBy(x => x.Key);
                        foreach (var t in grouped)
                        {
                            // NPT RATE : Tools.Div( x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)) 
                            srig.Details.Add(new PValueDetail
                            {
                                DateId = Convert.ToInt32(t.Key),
                                Type = "Year",
                                value = t.Sum(x => (x.ServiceCost + x.MaterialCost) + (x.ServiceCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)))),
                                WithSSValue = t.Sum(x => ((x.ServiceCost * x.WorkingInterest) + (x.MaterialCost * x.WorkingInterest)) +
                                    ((x.ServiceCost * x.WorkingInterest) * Tools.Div((x.NPTCost * x.WorkingInterest), ((x.TroubleFreeCost * x.WorkingInterest) - (x.MaterialCost * x.WorkingInterest)))))

                            });
                        }
                    }

                }
                #endregion Material and Services Cost

                #endregion
                results.Add(srig);

                srig = new STOSResult();
                srig.RigType = wa.RigType;
                srig.RigName = wa.RigName;
                srig.ProjectName = wa.ProjectName;
                srig.AssetName = wa.AssetName;
                srig.WellName = wa.WellName;
                srig.FirmOrOption = wa.FirmOrOption;
                srig.UARigSequenceId = wa.UARigSequenceId;
                srig.Region = wa.Region;
                srig.FirmOrOption = wa.FirmOrOption;
                #region Rig
                srig.ActivityName = srig.WellName + " Rig.";
                srig.SpendType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().FundingType : "";
                //srig.IsInPlan = wa.Phases.Count() > 0 ? wa.isInPlan : false;// wa.Phases.FirstOrDefault().IsInPlan : false;
                srig.LineofBusiness = wa.LineOfBusiness;

                #region Mapping
                srig.ActivityDesc = "";
                srig.RegretConsequences = "";
                srig.Rank = "";
                srig.PMaster = "";
                srig.PMasterCategory = "";
                srig.SponsorFunction = "";
                srig.Revenue = "";
                srig.AssuranceLevel = "";
                srig.Regrets = "";
                srig.POE = 0.0;

                #endregion
                #region Rig Cost
                srig.CostEstimator = UpdateBy;

                srig.Status = "";
                srig.EstimateType = wa.Phases.Count() > 0 ? wa.Phases.FirstOrDefault().LevelOfEstimate : "";


                if (bp != null && bp.Phases.Count() > 0)
                {
                    srig.OwnersCost = 0.0;
                    srig.Contingency = bp.Phases.FirstOrDefault().Estimate.NewTECOPTime.PercentCost;
                    srig.Currency = "USD";
                    if (bp.ReferenceFactorModel != null && bp.ReferenceFactorModel.ToUpper().Contains("ONSHORE"))
                        srig.Escalation = "Land Rigs";
                    else if (bp.ReferenceFactorModel != null && bp.ReferenceFactorModel.ToUpper().Contains("OFFSHORE"))
                        srig.Escalation = "Offshore Rigs";
                    else
                        srig.Escalation = "";


                    if (alloc.Any())
                    {
                        srig.Allocations = alloc;

                        foreach (var t in srig.Allocations)
                        {
                            var wo = t.WorkingInterest;
                            foreach (var y in t.AnnualyBuckets)
                            {
                                y.WorkingInterest = wo;
                            }
                        }

                        var grouped = srig.Allocations.SelectMany(x => x.AnnualyBuckets).GroupBy(x => x.Key);
                        grouped = grouped.OrderBy(x => x.Key);
                        foreach (var t in grouped)
                        {
                            // NPT RATE : Tools.Div( x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)) 
                            srig.Details.Add(new PValueDetail
                            {
                                DateId = Convert.ToInt32(t.Key),
                                Type = "Year",
                                value = t.Sum(x => x.RigCost + (x.RigCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost)))),
                                WithSSValue = t.Sum(x => (x.RigCost * x.WorkingInterest) + ((x.RigCost * x.WorkingInterest) * Tools.Div(
                                    (x.NPTCost * x.WorkingInterest), ((x.TroubleFreeCost * x.WorkingInterest) - (x.MaterialCost * x.WorkingInterest)))))
                                //,
                                //WithSSValue = t.Sum(x => (x.RigCost *  )  + (x.RigCost * Tools.Div(x.NPTCost, (x.TroubleFreeCost - x.MaterialCost))))

                            });
                        }
                    }

                }
                #endregion

                #endregion

                results.Add(srig);

            }
            #endregion



            return results;
        }
    }
}
