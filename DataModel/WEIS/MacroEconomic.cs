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
    public class AnnualHelper
    {
        public int Year { get; set; }
        public double Value { get; set; }
        public string Desc { get; set; }
        public string Source { get; set; }
    }

    public class MacroEconomic : ECISModel
    {
        public MacroEconomic()
        {
            InterestRateShort = new InterestRate();
            InterestRateLong = new InterestRate();
            ExchangeRate = new ExchangeRate();
            Inflation = new Inflation();
            GDP = new GDP();
            //CSO = new CSO();
            //MarketEscalation = new MarketEscalation();
            //GlobalMarketEscalation = new GlobalMarketEscalation();
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            _id = String.Format("C{0}X{1}W{2}R{3}E{4}", Country.Replace(" ", ""), BaseOP.Replace(" ", ""),
                WellName == null?"":WellName.Replace(" ", ""), 
                RigName == null?"":RigName.Replace(" ", ""),
                ActivityType == null?"":ActivityType.Replace(" ", "")
                );
            return this.ToBsonDocument();
        }
        public string Currency { get; set; }
        public string SwiftCode { get; set; }
        public string Category { get; set; }
        public string Continent { get; set; }
        public string MajorCountry { get; set; }

        public string Country { get; set; }
        public string WellName { get; set; }
        public string RigName { get; set; }
        public string ActivityType { get; set; }
        public string BaseOP { get; set; }

        public Inflation Inflation { get; set; }
        public MarketEscalation MarketEscalation { get; set; }
        public GlobalMarketEscalation GlobalMarketEscalation { get; set; }

        public InterestRate InterestRateShort { get; set; }
        public InterestRate InterestRateLong { get; set; }
        public ExchangeRate ExchangeRate { get; set; }
        
        public GDP GDP { get; set; }
        //public CSO CSO { get; set; }
        //public MarketEscalation MarketEscalation { get; set; }
        //public GlobalMarketEscalation GlobalMarketEscalation { get; set; }

        public override string TableName
        {
            get { return "WEISMacroEconomics"; }
        }
    }

    public class InterestRate
    {
        public InterestRate()
        {
            AnnualValues = new List<AnnualHelper>();
        }

        public string Title { get; set; }
        public string Source { get; set; }
        public bool SeasonallyAdjusted { get; set; }
        public double BaseYearPrice { get; set; }
        public double BaseYearIndex { get; set; }
        public int HistoricalEndYear { get; set; }
        public int HistoricalEndQuarter { get; set; }
        public DateTime DateOfLastUpdate { get; set; }
        public string SourceDetail { get; set; }
        public string AdditionalSourceDetail { get; set; }
        public string Location { get; set; }
        public string IndicatorCode { get; set; }

        public List<AnnualHelper> AnnualValues { get; set; }
    }

    public class ExchangeRate
    {
        public ExchangeRate()
        {
            AnnualValues = new List<AnnualHelper>();
        }
        public List<AnnualHelper> AnnualValues { get; set; }
    }

    public class Inflation
    {
        public Inflation()
        {
            AnnualValues = new List<AnnualHelper>();
        }
        public List<AnnualHelper> AnnualValues { get; set; }

        public double Forecast { get; set; }
    }

    public class GDP
    {
        public GDP()
        {
            AnnualValues = new List<AnnualHelper>();
        }

        public double GDPLevel { get; set; }

        public List<AnnualHelper> AnnualValues { get; set; }
    }

    public class MarketEscalation
    {
        public List<FYExpenseProfile> FiscalYears { get; set; }
        public MarketEscalation()
        {
            FiscalYears = new List<FYExpenseProfile>();
        }
    }

    public class GlobalMarketEscalation
    {
        public string RigName { get; set; }
        public double RigCostWeight { get; set; }
        public double MaterialCostWeight { get; set; }
        public double ServicesCostWeight { get; set; }
        public string EscalationFactor { get; set; }
    }

   


    public class CSO
    {
        // only for FY15 to FY20 
        public List<FYExpenseProfile> CSOValues { get; set; }
        public CSO()
        {
            CSOValues = new List<FYExpenseProfile>();
        }
    }


    






}





