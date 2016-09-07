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
    public class Excelbusplan 
    {
        public string Status { get; set; }
        public string LineOfBusiness { get; set; }
        public string RigType { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
        public string OperatingUnit { get; set; }
        public string PerformanceUnit { get; set; }
        public string AssetName { get; set; }
        public string ProjectName { get; set; }
        public string RigName { get; set; }
        public string WellName { get; set; }
        public string ActivityType { get; set; }
        public string RigSeqId { get; set; }
        public string Currency { get; set; }
        public double ShellShare { get; set; }
        public string DataInputBy { get; set; }
        public string SaveToOP { get; set; }
        public DateTime LastSubmitted {  get; set;}
        public bool InPlan { get; set; }
        public string RFM { get; set; }
        public string FundingType { get; set; }
        public DateRange Event { get; set; }
        public double EventDays { get; set; }
        public double RigRate { get; set; }
        public double Services { get; set; }
        public double Materials { get; set; }
        public bool isMaterialLLSetManually { get; set; }
        public double PercOfMaterialsLongLead { get; set; }
        public double LLRequiredMonth { get; set; }
        public double LLCalc { get; set; }
        public double SpreadRate { get; set; }
        public double BurnRate { get; set; }
        public double SpreadRateTotal { get; set; }
        public bool UseTAApproved { get; set; }
        public WellDrillData TroubleFree { get; set; }
        public bool IsUsingLCFfromRFM { get; set; }
        public double LCFParameter { get; set; }
        public WellDrillDataPercent NPT { get; set; }
        public WellDrillDataPercent TECOP { get; set; }
        public WellDrillData LCF { get; set; }
        public WellDrillData Base { get; set; }
        public WellDrillData BaseCalc { get; set; }
        public WellDrillData Mean { get; set; }
        public double TroubleFreeCost { get; set; }
        public double NptCost {get;set;}
        public double TecopCost { get; set; }
        public double MeanCost { get; set; }

        public double TroubleFreeCostUSD { get; set; }
        public double NptCostUSD { get; set; }
        public double TecopCostUSD { get; set; }
        public double MeanCostUSD { get; set; }


        public string MaturityLevel { get; set; }
        public string WellValueDriver { get; set; }
        public WVA WVANotSelected { get; set; }
        public WellDrillData MeanWVA { get; set; }
        public WVA WVATotalDays { get; set; }
        public WVA WVATotalCost { get; set; }
        public double DryHoleDays { get; set; }
        public WVA WVADryHoleDays { get; set; }
        public WVA WVABenchMark { get; set; }
        public string PerformanceScore { get; set; }
        public double WaterDepth { get; set; }
        public double WellDepth { get; set; }
        public double NumberOfCasings { get; set; }
        public double MechanicalRiskIndex { get; set; }
        public double MeanCostEDM { get; set; }
        public double MeanCostRealTerm { get; set; }
        public double MeanCostMOD { get; set; }
        public double ShellShareCost { get; set; }
        public double EscalationRig { get; set; }
        public double EscalationService { get; set; }
        public double EscalationMaterials { get; set; }
        public double EscalationCost { get; set; }
        public double CSOCost { get; set; }
        public double InflationCost { get; set; }
        public Int32 BusplanId { get; set; }
        public string latestLS { get; set; }
        public DateTime LSStartDate { get; set; }
        public BizPlanActivityPhase Phase { get; set; }


        public Excelbusplan()
        {
            Status = "";
            LineOfBusiness ="";
            RigType = "";
            Region = "";
            Country = "";
            OperatingUnit = "";
            PerformanceUnit = "";
            AssetName = "";
            ProjectName = "";
            RigName = "";
            WellName = "";
            ActivityType = "";
            RigSeqId = "";
            Currency = "";
            ShellShare = 0.00;
            DataInputBy = "";
            SaveToOP = "";
            //LastSubmitted =
            InPlan = false;
            RFM = "";
            FundingType = "";
            Event = new DateRange();
            EventDays = 0.00;
            RigRate = 0.00;
            Services = 0.00;
            Materials = 0.00;
            isMaterialLLSetManually = false;
            PercOfMaterialsLongLead = 0.00;
            LLRequiredMonth = 0.00;
            LLCalc = 0.00;
            SpreadRate = 0.00;
            BurnRate = 0.00;
            SpreadRateTotal = 0.00;
            UseTAApproved = false;
            TroubleFree = new WellDrillData();
            IsUsingLCFfromRFM = false;
            LCFParameter = 0.00;
            NPT = new WellDrillDataPercent();
            TECOP = new WellDrillDataPercent();
            LCF = new WellDrillData();
            Base = new WellDrillData();
            BaseCalc = new WellDrillData();
            Mean = new WellDrillData();
            TroubleFreeCost = 0.00;
            NptCost = 0.00;
            TecopCost = 0.00;
            MeanCost = 0.00;
            MaturityLevel = "";
            WellValueDriver = "";
            MeanWVA = new WellDrillData();
            WVATotalDays = new WVA();
            WVATotalCost = new WVA();
            WVADryHoleDays = new WVA();
            WVABenchMark = new WVA();
            DryHoleDays = 0.00;
            PerformanceScore = "";
            WaterDepth = 0;
            WellDepth = 0 ;
            NumberOfCasings = 0;
            MechanicalRiskIndex = 0;
            MeanCostEDM = 0.00;
            MeanCostRealTerm = 0.00;
            MeanCostMOD = 0.00;
            ShellShareCost = 0.00;
            EscalationRig = 0.00;
            EscalationService = 0.00;
            EscalationMaterials = 0.00;
            EscalationCost = 0.00;
            CSOCost = 0.00;
            InflationCost = 0.00;
            Phase = new BizPlanActivityPhase();
            latestLS = "";
            LSStartDate = new DateTime(1900, 1, 1);
        }
    }

    public class WVA
    {
        public WellDrillData TQValueDriver { get; set; }
        public WellDrillData TQGap { get; set; }
        public WellDrillData BICValueDriver { get; set; }
        public WellDrillData BICGap { get; set; }

        public WVA()
        {
            TQValueDriver = new WellDrillData();
            TQGap = new WellDrillData();
            BICValueDriver = new WellDrillData();
            BICGap = new WellDrillData();
        }
    }
}
