using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;



namespace ECIS.Client.WEIS
{
    public class BizPlanTLA : ECISModel
    {
        public override string TableName
        {
            get { return "WEISBizPlanTLAs"; }
        }

        public BizPlanTLASetting Setting { get; set; }

        public BizPlanActivityEstimate CurrentEstimate { get; set; }
        public BizPlanActivityEstimate UpdatedEstimate { get; set; }

        public BizPlanAllocation CurrentAllocation { get; set; }
        public BizPlanAllocation UpdatedAllocation { get; set; }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            this._id = String.Format("W{0}A{1}R{2}", CurrentAllocation.WellName, CurrentAllocation.ActivityType, CurrentAllocation.RigName);
            doc = this.ToBsonDocument();
            return doc;
        }

        public BizPlanTLA()
        {
            Setting = new BizPlanTLASetting();
            UpdatedEstimate = new BizPlanActivityEstimate();
            CurrentEstimate = new BizPlanActivityEstimate();
            CurrentAllocation = new BizPlanAllocation();
            UpdatedAllocation = new BizPlanAllocation();


        }

        public override BsonDocument PostSave(BsonDocument doc, string[] references = null)
        {
            // Must be Update to Allocation and BizPlanActivity to take effect, 
            IMongoQuery q = null;
            q = Query.And(Query.EQ("WellName", UpdatedAllocation.WellName), Query.EQ("Phases.ActivityType", UpdatedAllocation.ActivityType), Query.EQ("RigName", UpdatedAllocation.RigName));
            var populates = BizPlanActivity.GetAll(q);
            if (populates.Any())
            {
                var pop = populates.FirstOrDefault();
                var phases = pop.Phases.Where(x => x.ActivityType.Equals(UpdatedAllocation.ActivityType));
                if (phases.Any())
                {
                    phases.FirstOrDefault().Allocation = UpdatedAllocation;
                    phases.FirstOrDefault().Estimate = UpdatedEstimate;

                    // save activity and rebuild Allocation automatically : BizPlanAllocation.SaveBizPlanAllocation(BizPlanActivity activity)
                    pop.Save();
                }
            }
            return this.ToBsonDocument();
        }


        public BizPlanCalculation CalcTLA(string wellName, string activityType, string rigName, BizPlanTLASetting set, string country, string rfm, string BaseOP = "OP15")
        {
            IMongoQuery q = null;
            q = Query.And(Query.EQ("WellName", wellName), Query.EQ("Phases.ActivityType", activityType), Query.EQ("RigName", rigName));
            var populates = BizPlanActivity.GetAll(q);
            if (populates.Any())
            {

                var actv = populates.FirstOrDefault();
                var phases = actv.Phases.Where(x => x.ActivityType.Equals(activityType));
                if (phases.Any())
                {
                    CurrentEstimate = phases.FirstOrDefault().Estimate;
                    CurrentAllocation = phases.FirstOrDefault().Allocation;
                    var maturityLevel = CurrentEstimate.MaturityLevel.Substring(CurrentEstimate.MaturityLevel.Length - 1, 1);
                    var calculated = BizPlanCalculation.calcBizPlan(CurrentAllocation.WellName,
                                    CurrentAllocation.RigName,
                                    CurrentAllocation.ActivityType,
                                    country,
                                    actv.ShellShare,
                                    CurrentEstimate.EstimatePeriod,
                                    Convert.ToInt32(maturityLevel),
                                    CurrentEstimate.Services,
                                    CurrentEstimate.Materials,
                                    CurrentEstimate.NewTroubleFree.Days,
                                    rfm,BaseOP,
                                    CurrentEstimate.NewNPTTime.PercentDays > 1 ? Tools.Div(CurrentEstimate.NewNPTTime.PercentDays, 100) : CurrentEstimate.NewNPTTime.PercentDays,
                                    CurrentEstimate.NewTECOPTime.PercentDays > 1 ? Tools.Div(CurrentEstimate.NewTECOPTime.PercentDays, 100) : CurrentEstimate.NewTECOPTime.PercentDays,
                                    CurrentEstimate.NewNPTTime.PercentCost > 1 ? Tools.Div(CurrentEstimate.NewNPTTime.PercentCost, 100) : CurrentEstimate.NewNPTTime.PercentCost,
                                    CurrentEstimate.NewTECOPTime.PercentCost > 1 ? Tools.Div(CurrentEstimate.NewTECOPTime.PercentCost, 100) : CurrentEstimate.NewTECOPTime.PercentCost,
                                    CurrentEstimate.LongLeadMonthRequired,
                                    CurrentEstimate.PercOfMaterialsLongLead, CurrentEstimate.SpreadRateTotal,CurrentEstimate.NewMean.Days,
                                    true,
                                   set.SpreadRate,
                                   set.Days,
                                   set.NPTDays,
                                   set.Tangibles,
                                   set.Services,
                                   set.Material
                                    );
                    return calculated;
                }
            }
            return null;
        }
    }

}
