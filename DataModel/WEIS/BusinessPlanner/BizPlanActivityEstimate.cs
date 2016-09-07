
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
    [BsonIgnoreExtraElements]
    public class BizPlanActivityEstimate
    {
        public BizPlanActivityEstimate()
        {
            CurrentTroubleFree = new WellDrillData();
            NewTroubleFree = new WellDrillData();
            CurrentNPTTime = new WellDrillPercentData();
            NewNPTTime = new WellDrillPercentData();
            CurrentTECOPTime = new WellDrillPercentData();
            NewTECOPTime = new WellDrillPercentData();
            CurrentMean = new WellDrillData();
            NewMean = new WellDrillData();
            TQ = new WellValueData();
            BIC = new WellValueData();
            //EstimatingRange = new EstimatingRange();
            Summary = new EstimateSummary();
            ///FiscalYears = new List<YearCalc>();
            ///
            EstimatePeriod = new DateRange();
            isMaterialLLSetManually = false;
            TroubleFreeBeforeLC = new WellDrillData();

            NewLCFValue = new WellDrillData();
            NewBaseValue = new WellDrillData();
        }

        public DateTime LastUpdate { get; set; }
        public string LastUpdateBy { get; set; }

        // New in Estimate
        public string Country { get; set; }
        //public double ShellWorkingInterest { get; set; }
        public string Currency { get; set; }
        // -------------
        public string SaveToOP { get; set; }
        public bool isGenerateFromSync { get; set; }
        //

        public string Status { get; set; }
        public string MaturityLevel { get; set; }
        //public string FundingType { get; set; }
        public double WaterDepth { get; set; }
        public double WellDepth { get; set; }

        public DateTime ProjectStartDate { get; set; }
        public double RigRate { get; set; }
        public string RigName { get; set; }
        public double SpreadRate { get; set; }
        public double BurnRate { get; set; }
        public double SpreadRateTotal { get; set; }

        // learning curve factor
        // public double CurrentLearningCurveFactor { get; set; }
        public double NewLearningCurveFactor { get; set; }
        public bool IsUsingLCFfromRFM { get; set; }
        public bool IsUsingMaturityLevel { get; set; }

        // Cost and Time Estimate
        public WellDrillData CurrentTroubleFree { get; set; }
        public WellDrillData NewTroubleFree { get; set; }
        public WellDrillData TroubleFreeBeforeLC { get; set; }
        public WellDrillPercentData CurrentNPTTime { get; set; }
        public WellDrillPercentData NewNPTTime { get; set; }
        public WellDrillPercentData CurrentTECOPTime { get; set; }
        public WellDrillPercentData NewTECOPTime { get; set; }
        public WellDrillData CurrentMean { get; set; }
        public WellDrillData NewMean { get; set; }

        public WellDrillData NewLCFValue { get; set; }
        public WellDrillData NewBaseValue { get; set; }

        // USD Convert
        public double CurrentTroubleFreeUSD { get; set; }
        public double NewTroubleFreeUSD { get; set; }
        public double NPTCostUSD { get; set; }
        public double TECOPCostUSD { get; set; }
        public double MeanUSD { get; set; }

        // Project Value Driver
        public string ProjectValueDriver { get; set; }
        public double ValueDriver { get; set; }
        public WellValueData TQ { get; set; }
        public WellValueData BIC { get; set; }

        public string PerformanceScore
        {
            get;
            set;
        }

        // Scope Description
        //public string DrillingOfCasings { get; set; }
        //public string MRI { get; set; }
        //public string ofCompletionZones { get; set; }
        public double BrineDensity { get; set; }

        // added by eky
        //public EstimatingRange EstimatingRange { get; set; }
        //public string CurrentEstimateMaturity { get; set; }
        //public string NewEstimateMaturity { get; set; }
        //public string CurrentRigName { get; set; }
        //public string NewRigName { get; set; }
        //public string CompletionType { get; set; }
        public bool isMaterialLLSetManually { get; set; }

        //added based on change request
        public bool UsingTAApproved { get; set; }
        public DateTime EventStartDate { get; set; }
        public DateTime EventEndDate { get; set; }
        public DateRange EstimatePeriod { get; set; }
        public double Services { get; set; }
        public double Materials { get; set; }
        public double PercOfMaterialsLongLead { get; set; }
        public double LongLeadCalc { get; set; }
        public double LongLeadMonthRequired { get; set; }
        public DateTime StartEscDateMaterial { get; set; }


        //usd conversion
        public double ServicesUSD { get; set; }
        public double MaterialsUSD { get; set; }
        public double RigRatesUSD { get; set; }
        public double SpreadRateUSD { get; set; }
        public double BurnRateUSD { get; set; }
        public double SpreadRateTotalUSD { get; set; }

        //summary
        public double MeanCostMOD { get; set; }
        public double ShellShareCalc { get; set; }

        public EstimateSummary Summary { get; set; }

        // Drilling Specific
        public double NumberOfCasings { get; set; }
        public double MechanicalRiskIndex { get; set; }

        // Completions Specific
        public string CompletionInfo { get; set; }
        public string CompletionType { get; set; }
        public double NumberOfCompletionZones { get; set; }
        //public string BrineDensity { get; set; }

        public string WellValueDriver { get; set; }
        public double TQValueDriver { get; set; }
        public double BICValueDriver { get; set; }

        public double DryHoleDays { get; set; }

        public string SelectedCurrency { get; set; }

        public bool isFirstInitiate { get; set; }
    }

    public class EstimateSummary
    {
        public DateRange Period { get; set; }
        public double MeanCostEDM { get; set; }
        public double MeanCostMOD { get; set; }
        public double ShellShareCalc { get; set; }
    }

    public class WellValueData
    {

        public string Identifier { get; set; }
        public double Gap { get; set; }
        public double Threshold { get; set; }
    }

    public class WellDrillPercentData : WellDrillData
    {
        public double PercentDays { get; set; }
        public double PercentCost { get; set; }
    }

    public class EstimatingRange
    {

        public string Type { get; set; }
        public string RangeStartTitle { get; set; }
        public double RangeStartValue { get; set; }
        public string RangeEndTitle { get; set; }
        public double RangeEndValue { get; set; }
    }

    public class WellPlanData
    {
        public string Units { get; set; }
        public double Value { get; set; }
    }
}




