using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using ECIS.Core;
using ECIS.Biz.Common;

using Newtonsoft.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization.Attributes;

namespace ECIS.Client.WEIS
{
    [BsonIgnoreExtraElements]
    public class WellActivityPhaseInfo : ECISModel
    {

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            if (this._id == null)
            {
                this._id = String.Format("WA{0}W{1}S{2}P{3}O{4}E{5}", WellActivityId, WellName, SequenceId, PhaseNo, OPType, ActivityType);
            }
            
            return this.ToBsonDocument();

        }

        public override string TableName
        {
            get { return "WEISWellActivityPhaseInfos"; }
        }

        public WellActivityPhaseInfo()
        {
            TotalDuration = new WellDrillData();
            TroubleFree = new WellDrillData();
            Trouble = new WellDrillData();
            Contigency = new WellDrillData();
            TQ = new WellDrillData();
            AggredTarget = new WellDrillData();
            BIC = new WellDrillData();
            LTA2 = new WellDrillData();
            NPT = new WellDrillDataPercent();
            TECOP = new WellDrillDataPercent();
            TQMeasures = new TopQuartileMeasures();
            Mean = new WellDrillDataPercent();
            OverrideFactor = new OvverrideFactor();
            MeanCostEDM = new WellDrillData();
            USDCost = new CostinUSD();

        }

        public string WellActivityId { get; set; }
        public string WellName { get; set; }
        public string ActivityType { get; set; }
        public string RigName { get; set; }
        public string SequenceId { get; set; }
        public int RigSequenceId { get; set; }
        public int PhaseNo { get; set; }
        public string OPType { get; set; }

        public string ScopeDescription { get; set; }


        public string LoE { get; set; }
        public WellDrillData TotalDuration { get; set; }
        public WellDrillData TroubleFree { get; set; }
        public WellDrillData Trouble { get; set; }
        public WellDrillData Contigency { get; set; }
        public WellDrillData TQ { get; set; }
        public WellDrillData AggredTarget { get; set; }
        public WellDrillData BIC { get; set; }
        public WellDrillData LTA2 { get; set; }
        public string SinceLTA2 { get; set; }

        public double BurnRate { get; set; }
        public double EscalationInflation { get; set; }
        public double CSO { get; set; }
        public double TotalCostIncludePortf { get; set; }

        public string LLMonth { get; set; }
        public double LLAmount { get; set; }
        public string PlanningClassification { get; set; }

        

        public double CostEscalatedInflated { get; set; }
        public double WorkingInterest { get; set; }
        public double TotalCostWithEscInflCSO { get; set; }

        public TopQuartileMeasures TQMeasures { get; set; }
        // Upload OP 15 new fields
        public string LineOfBusiness { get; set; }
        public string ActivityCategory { get; set; }
        public double SpreadRate { get; set; }
        public double MRI { get; set; }
        public string CompletionType { get; set; }
        public int CompletionZone { get; set; }
        public double BrineDensity { get; set; }
        public string EstimatingRangeType { get; set; }
        public double DeterministicLowRange { get; set; }
        public double DeterministicHigh { get; set; }
        public double ProbabilisticP10 { get; set; }
        public double ProbabilisticP90 { get; set; }
        public double WaterDepthMD { get; set; }
        public double TotalWellDepthMD { get; set; }
        public double LearningCurveFactor { get; set; }
        public string MaturityLevel { get; set; }
        public string ReferenceFactorModel { get; set; }
        //public int SequenceOnRig { get; set; }
        public double DrillingOfCasing { get; set; }


        public OvverrideFactor OverrideFactor { get; set; }

        public WellDrillDataPercent NPT { get; set; }
        public WellDrillDataPercent TECOP { get; set; }
        public WellDrillDataPercent Mean { get; set; }
        public WellDrillData MeanCostEDM { get; set; }

        public string Currency { get; set; }
        public CostinUSD USDCost { get; set; }

        public string ProjectValueDriver { get; set; }
        public double ValueDriverEstimate { get; set; }

        public double TQTreshold { get; set; }
        public double TQGap { get; set; }

        public double BICTreshold { get; set; }
        public double BICGap { get; set; }

        public string PerformanceScore { get; set; }

    }

    public class CostinUSD
    {
        public double TroubleFree { get; set; }
        public double NPT { get; set; }
        public double TECOP { get; set; }
        public double MeanCostEDM { get; set; }
        public double Escalation { get; set; }
        public double CSO { get; set; }
        public double Inflation { get; set; }
        public double MeanCostMOD { get; set; }
    }


    public class OvverrideFactor
    {
        public bool Cost { get; set; }
        public bool Time { get; set; }
    }
    public class TopQuartileMeasures
    {
        public WellDrillData TQDuration { get; set; }
        public WellDrillData BICDuration { get; set; }
        public bool TQTargetperPIP { get; set; }
        public bool BICperPIP { get; set; }
    }
}
