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
    public class Spotfire
    {
        public string BaseOP { get; set; }

        public string LoB { get; set; }
        public string Region { get; set; }
        public string OperatingUnit { get; set; }
        public string Asset { get; set; }
        public string Project { get; set; }
        public string WellName { get; set; }
        public string ActivityCategory { get; set; }
        public string ActivityType { get; set; }
        public string ScopeDesc { get; set; }
        public string WellType { get; set; }
        public double DrillingOfCasing { get; set; }
        public double SpreadRateUSDDay { get; set; }
        public double BurnRateUSDDay { get; set; }
        public double MRI { get; set; }
        public string CompletionType { get; set; }
        public int CompletionZone { get; set; }
        public double BrineDensity { get; set; }
        public string EstRangeType { get; set; }
        public double DeterminLowRange { get; set; }
        public double DeterminHigh { get; set; }
        public double ProbP10 { get; set; }
        public double ProbP90 { get; set; }
        public double WaterDepth { get; set; }
        public double TotalWaterDepth { get; set; }
        public double LCFactor { get; set; }
        public double WorkingInterest { get; set; }
        public string PlanningClassification { get; set; }
        public string MaturityRisk { get; set; }
        public string FundingType { get; set; }
        public string RFM { get; set; }
        public string RigName { get; set; }
        public string RigType { get; set; }
        public int SequenceOnRig { get; set; }
        public string SequenceId { get; set; }
        public string Start { get; set; }
        public string Finish { get; set; }
        public DateRange PlanSchedule { get; set; }
        public int PlanYear { get; set; }

        public WellDrillData TroubleFree { get; set; }
        public WellDrillPercentData NPT { get; set; }
        public WellDrillPercentData TECOP { get; set; }
        public double MeanCostEDM { get; set; }

        public OvverrideFactor OverrideFactor { get; set; }
        public string TimeOverrideFactors { get; set; }
        public string CostOverrideFactors { get; set; }
        

        public double MeanTime { get; set; }
        public string Currency { get; set; }

        public double TroubleFreeUSD { get; set; }
        public double NPTUSD { get; set; }
        public double TECOPUSD { get; set; }
        public double MeanCostEDMUSD { get; set; }
        public double EscCostUSD { get; set; }
        public double CSOCostUSD { get; set; }
        public double InflationCostUSD { get; set; }
        public double MeanCostMODUSD { get; set; }
        public double MeanCostRTUSD { get; set; }

        public string ProjectValueDriver { get; set; }

        public WellValueData TQ { get; set; }
        public WellValueData BIC { get; set; }
        public string PerfScore { get; set; }
        public int ActivityCount { get; set; }
        public string ScheduleID { get; set; }


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

        public Spotfire()
        {
            TQ = new WellValueData();
            BIC = new WellValueData();
            PlanSchedule = new DateRange();
            TroubleFree = new WellDrillData();
            NPT = new WellDrillPercentData();
            TECOP = new WellDrillPercentData();

            #region
            TimeOverrideFactors = "";

            LoB = "";
            Region = "";
            OperatingUnit = "";
            Asset = "";
            Project = "";
            WellName = "";
            ActivityCategory = "";
            ActivityType = "";
            ScopeDesc = "";
            WellType = "";
            DrillingOfCasing = 0.0;
            SpreadRateUSDDay = 0.0;
            BurnRateUSDDay = 0.0;
            MRI = 0.0;
            CompletionType = "";
            CompletionZone = 0;
            BrineDensity = 0.0;
            EstRangeType = "";
            DeterminLowRange = 0.0;
            DeterminHigh = 0.0;
            ProbP10 = 0.0;
            ProbP90 = 0.0;
            WaterDepth = 0.0;
            TotalWaterDepth = 0.0;
            LCFactor = 0.0;
            WorkingInterest = 0.0;
            PlanningClassification = "";
            MaturityRisk = "";
            FundingType = "";
            RFM = "";
            RigName = "";
            RigType = "";
            SequenceOnRig = 0;
            SequenceId = "";

            PlanYear = 0;

            MeanCostEDM = 0.0;

            OverrideFactor = new OvverrideFactor();
            MeanTime = 0.0;
            Currency = "";

            TroubleFreeUSD = 0.0;
            NPTUSD = 0.0;
            TECOPUSD = 0.0;
            MeanCostEDMUSD = 0.0;
            EscCostUSD = 0.0;
            CSOCostUSD = 0.0;
            InflationCostUSD = 0.0;
            MeanCostMODUSD = 0.0;
            MeanCostRTUSD = 0.0;

            ProjectValueDriver = "";


            PerfScore = "";
            ActivityCount = 0;
            ScheduleID = "";


            SSTroubleFreeUSD = 0.0;
            SSNPTUSD = 0.0;
            SSTECOPUSD = 0.0;
            SSMeanCostEDMUSD = 0.0;
            SSEscCostUSD = 0.0;
            SSCSOCostUSD = 0.0;
            SSInflationCostUSD = 0.0;
            SSMeanCostMODUSD = 0.0;
            SSMeanCostRTUSD = 0.0;

            OPScope = "";

            #endregion
        }

    }
}
