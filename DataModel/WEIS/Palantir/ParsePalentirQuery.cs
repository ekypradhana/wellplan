using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
//using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using System.Globalization;
using ECIS.Core;
using ECIS.Client.WEIS;

namespace ECIS.Client.WEIS
{
    public class ParsePalentirQuery
    {
        public string MoneyType { get; set; }
        public string Currency { get; set; }
        public string SSTG { get; set; }
        public string InPlan { get; set; }

        public List<string> LineOfBusiness { get; set; }
        public List<string> Asset { get; set; }
        public List<string> FundingType { get; set; }

        public List<string> UpdateBy { get; set; }
        public string DateStart { get; set; }
        public string DateFinish { get; set; }

        public bool isGenerated { get; set; }
        public bool isSTOS { get; set; }

        #region CAPEX
        public List<string> CaseNames { get; set; }
        public List<string> MapName { get; set; }
        #endregion

        #region PMaster
        public List<string> PlanningEntityId { get; set; }
        public List<string> ReportingEntity { get; set; }
        public List<string> ActivityEntity { get; set; }
        public List<string> PlanningEntity { get; set; }
        public List<string> ActivityEntityId { get; set; }
        #endregion

        public IMongoQuery ParseQuery()
        {
            var result = Parse();
            return result;
        }

        private IMongoQuery Parse()
        {
            var q = new List<IMongoQuery>();

            #region CAPEX
            if (CaseNames != null && CaseNames.Count > 0)
            {
                q.Add(Query.In("CaseName", new BsonArray(CaseNames)));
            }

            if (MapName != null)
            {
                foreach (string mapN in MapName)
                {
                    q.Add(Query.EQ("MapName", mapN));
                }
            }
            #endregion

            #region PMaster
            if (PlanningEntityId != null && PlanningEntityId.Count > 0)
            {
                q.Add(Query.In("PlanningEntityID", new BsonArray(PlanningEntityId)));
            }

            if (PlanningEntity != null && PlanningEntity.Count > 0)
            {
                q.Add(Query.In("PlanningEntity", new BsonArray(PlanningEntity)));
            }

            if (ActivityEntity != null && ActivityEntity.Count > 0)
            {
                q.Add(Query.In("ActivityEntity", new BsonArray(ActivityEntity)));
            }

            if (ReportingEntity != null && ReportingEntity.Count > 0)
            {
                q.Add(Query.In("ReportEntity", new BsonArray(ReportingEntity)));
            }

            if (ActivityEntityId != null && ActivityEntityId.Count > 0)
            {
                q.Add(Query.In("ActivityEntityID", new BsonArray(ActivityEntityId)));
            }
            #endregion
            
            if (Asset != null && Asset.Count > 0)
            {
                q.Add(Query.In("AssetName", new BsonArray(Asset)));
            }

            string field = isGenerated ? "Phases.FundingType" : "FundingType";
            if (FundingType != null && FundingType.Count > 0)
            {
                q.Add(Query.In(field, new BsonArray(FundingType)));
            }

            if (UpdateBy != null && UpdateBy.Count > 0)
            {
                q.Add(Query.In("UpdateBy", new BsonArray(UpdateBy)));
            }

            if (isGenerated)
            {
                string defaultInPlan = string.IsNullOrEmpty(InPlan) ? "Both" : InPlan;
                string fieldinpan = isSTOS ? "isInPlan" : "IsInPlan";
                switch (defaultInPlan)
                {
                    case "Yes":
                        q.Add(Query.EQ(fieldinpan, true));
                        break;
                    case "No":
                        q.Add(Query.EQ(fieldinpan, false));
                        break;
                    case "Both":
                        q.Add(Query.In(fieldinpan, new BsonArray(new bool[] { true, false })));
                        break;
                }
            }            

            string fieldLoB = isSTOS ? "LineOfBusiness" : "LoB";
            if (LineOfBusiness != null && LineOfBusiness.Count > 0)
            {
                q.Add(Query.In(fieldLoB, new BsonArray(LineOfBusiness)));
            }

            if (!string.IsNullOrEmpty(DateStart) && !string.IsNullOrEmpty(DateFinish))
            {
                string format = "dd-MMM-yyyy";
                CultureInfo culture = CultureInfo.InvariantCulture;
                var filter = new DateRange()
                {
                    Start = Tools.ToUTC(DateStart != "" ? DateTime.ParseExact(DateStart, format, culture) : new DateTime(1900, 1, 1)),
                    Finish = Tools.ToUTC(DateFinish != "" ? DateTime.ParseExact(DateFinish, format, culture) : new DateTime(3000, 1, 1))
                };
                q.Add(Query.And(Query.GTE("UpdateVersion", filter.Start), Query.LT("UpdateVersion", filter.Finish.AddDays(1))));
            }

            if (q.Count > 0)
                return Query.And(q);

            return null;
        }

        public List<string> STOSFilter(List<string> distSelect)
        {
            List<string> tempList = new List<string>();
            foreach (var lb in distSelect)
            {
                string a = lb;
                if (string.IsNullOrEmpty(lb))
                {
                    a = null;
                }
                tempList.Add(a);
            }
            return tempList;
        }

        private static readonly string[] PMasterFields = {
                "_id", "LastUpdate", "UpdateBy", "ReportEntity", "PlanningEntity", "PlanningEntityID",
                "ActivityEntity", "ActivityEntityID", "Prob", "UpdateVersion", "WellName", "RigName", "MapName", "LoB", "AssetName",
                "ActivityType", "UARigSequenceId", "ActivityCategory", "BaseOP", "FundingType", "ProjectName",
                "WorkingInterest", "IsInPlan", "FirmOption", "PhSchedule.Start", "PhSchedule.Finish", "OP.Identifier",
                "OP.Days", "OP.Cost", "OP.Comment", "PlanSchedule.Start", "PlanSchedule.Finish", "Plan.Identifier",
                "Plan.Days", "Plan.Cost", "Plan.Comment", "PlanMODSS", "PlanEDM", "PlanEDMSS", "PlanRT", "PlanRTSS","Allocation"
            };

        public string[] MoneyTypeFields()
        {
            var list = new List<string>(PMasterFields);
            //MoneyType = string.IsNullOrEmpty(MoneyType) ? "EDM" : MoneyType;
            switch (MoneyType)
            {
                case "EDM":
                    list.Remove("Plan.Identifier");
                    list.Remove("Plan.Days");
                    list.Remove("Plan.Cost");
                    list.Remove("Plan.Comment");
                    list.Remove("PlanMODSS");
                    list.Remove("PlanRT");
                    list.Remove("PlanRTSS");
                    break;
                case "MOD":
                    list.Remove("PlanEDM");
                    list.Remove("PlanEDMSS");
                    list.Remove("PlanRT");
                    list.Remove("PlanRTSS");
                    break;
                case "RT":
                    list.Remove("Plan.Identifier");
                    list.Remove("Plan.Days");
                    list.Remove("Plan.Cost");
                    list.Remove("Plan.Comment");
                    list.Remove("PlanMODSS");
                    list.Remove("PlanEDM");
                    list.Remove("PlanEDMSS");
                    break;
            }

            return list.ToArray();
        }
    }

    public class FilterGeneratePlanningReportMap : ParsePalentirQuery
    {
        public List<string> Asset { get; set; }
        public List<string> FundingType { get; set; }
        public List<string> LineOfBusiness { get; set; }
    }

    public class CurrentCurrency
    {
        public string Currency { get; set; }
        public double Value { get; set; }
        public string BaseOP { get; set; }
        public int CurrentYear { get; set; }
        public double TotalCost { get; set; }
    }

    public class InflationRate {
        public AnnualHelper Inflation;
        public string Currency;
        public string Country;

        public InflationRate()
        {
            Inflation = new AnnualHelper();
        }
    }

    public class CalcCostCurrency
    {
        public CurrentCurrency GetCurrentCurrency(string currency)
        {
            //List<CurrentCurrency> CurrencyList = new List<CurrentCurrency>();
            var q = new List<IMongoQuery>();
            var baseOP = DataHelper.Get("SharedConfigTable", Query.EQ("ConfigModule", "BaseOPDefault")).Select(x => x.Value).ToList();
            q.Add(Query.EQ("BaseOP", baseOP[3].ToString()));

            if (currency.ToString() == "USD")
            {
                q.Add(Query.EQ("Country", "United States"));
            }
            q.Add(Query.EQ("Currency", currency));

            var getcurrency = MacroEconomic.Populate<MacroEconomic>(Query.And(q)).Select(x => x.ExchangeRate.AnnualValues).ToList();
            CurrentCurrency curr = new CurrentCurrency();
            curr.BaseOP = baseOP[3].ToString();
            curr.Currency = currency;
            curr.CurrentYear = Convert.ToInt32(DateTime.Now.Year.ToString());
            var value = getcurrency.ElementAt(0).Where(x => x.Year == Convert.ToInt32(DateTime.Now.Year.ToString())).Select(x => x.Value).ToList();
            curr.Value = value[0];

            return curr;
        }

        public InflationRate GetInflation(string currency, string country)
        {
            var getCurrentCurrency = GetCurrentCurrency(currency);
            var baseOP = DataHelper.Get("SharedConfigTable", Query.EQ("ConfigModule", "BaseOPDefault")).Select(x => x.Value).ToList();
            var macroEc = MacroEconomic.Get<MacroEconomic>(Query.And(
                Query.EQ("Currency", currency), 
                Query.EQ("Country", country), 
                Query.EQ("BaseOP", baseOP[3].ToString())));
            InflationRate ir = new InflationRate();
            ir.Currency = currency;
            ir.Country = country;

            var getInf = macroEc.Inflation.AnnualValues.Where(x => x.Year == getCurrentCurrency.CurrentYear).FirstOrDefault().ToBsonDocument();
            ir.Inflation.Desc = getInf.GetString("Desc");
            ir.Inflation.Source = getInf.GetString("Source");
            ir.Inflation.Value = getInf.GetDouble("Value");
            ir.Inflation.Year = getInf.GetInt32("Year");

            return ir;
        }
    }

    public class SSTG
    {
        public void GetSSTG(ParsePalentirQuery pq, PDetail CapexSummary, out PDetail cs)
        {
            cs = new PDetail();
            //string curr = string.IsNullOrEmpty(currency.ElementAt(0)) ? "USD" : currency.ElementAt(0);
            //string curr = currency.ElementAt(0) == "Select Currency ..." ? "USD" : currency.ElementAt(0); 
            CalcCostCurrency cr = new CalcCostCurrency();
            var getCurrentCurrency = cr.GetCurrentCurrency(pq.Currency);

            //sstg = string.IsNullOrEmpty(sstg) ? "Total Gross" : "Shell Share";
            switch (pq.SSTG)
            {
                case "Shell Share":
                    if (pq.MoneyType.Equals("EDM"))
                    {
                        cs.CapitalCompletionPDDevInTang.EDM = getCurrentCurrency.Value * CapexSummary.CapitalCompletionPDDevInTang.EDMSS;
                        cs.CapitalCompletionPDDevTang.EDM = getCurrentCurrency.Value * CapexSummary.CapitalCompletionPDDevTang.EDMSS;
                        cs.CapitalDrillingPDDevInTang.EDM = getCurrentCurrency.Value * CapexSummary.CapitalDrillingPDDevInTang.EDMSS;
                        cs.CapitalDrillingPDDevTang.EDM = getCurrentCurrency.Value * CapexSummary.CapitalDrillingPDDevTang.EDMSS;
                        cs.CapitalExpenDRDVAWells.EDM = getCurrentCurrency.Value * CapexSummary.CapitalExpenDRDVAWells.EDMSS;
                        cs.CapitalExpenDRSubSeaWells.EDM = getCurrentCurrency.Value * CapexSummary.CapitalExpenDRSubSeaWells.EDMSS;
                        cs.ContigencyInTangWells.EDM = getCurrentCurrency.Value * CapexSummary.ContigencyInTangWells.EDMSS;
                        cs.ContigencyTangWells.EDM = getCurrentCurrency.Value * CapexSummary.ContigencyTangWells.EDMSS;
                        cs.EPEXCompletionB2ExplInTang.EDM = getCurrentCurrency.Value * CapexSummary.EPEXCompletionB2ExplInTang.EDMSS;
                        cs.EPEXCompletionB2ExplTang.EDM = getCurrentCurrency.Value * CapexSummary.EPEXCompletionB2ExplTang.EDMSS;
                        cs.EPEXDrillingB2ExplInTang.EDM = getCurrentCurrency.Value * CapexSummary.EPEXDrillingB2ExplInTang.EDMSS;
                        cs.EPEXDrillingB2ExplTang.EDM = getCurrentCurrency.Value * CapexSummary.EPEXDrillingB2ExplTang.EDMSS;
                        cs.OPCostIdleRig.EDM = getCurrentCurrency.Value * CapexSummary.OPCostIdleRig.EDMSS;
                    }
                    else if (pq.MoneyType.Equals("MOD"))
                    {
                        cs.CapitalCompletionPDDevInTang.MOD = getCurrentCurrency.Value * CapexSummary.CapitalCompletionPDDevInTang.MODSS;
                        cs.CapitalCompletionPDDevTang.MOD = getCurrentCurrency.Value * CapexSummary.CapitalCompletionPDDevTang.MODSS;
                        cs.CapitalDrillingPDDevInTang.MOD = getCurrentCurrency.Value * CapexSummary.CapitalDrillingPDDevInTang.MODSS;
                        cs.CapitalDrillingPDDevTang.MOD = getCurrentCurrency.Value * CapexSummary.CapitalDrillingPDDevTang.MODSS;
                        cs.CapitalExpenDRDVAWells.MOD = getCurrentCurrency.Value * CapexSummary.CapitalExpenDRDVAWells.MODSS;
                        cs.CapitalExpenDRSubSeaWells.MOD = getCurrentCurrency.Value * CapexSummary.CapitalExpenDRSubSeaWells.MODSS;
                        cs.ContigencyInTangWells.MOD = getCurrentCurrency.Value * CapexSummary.ContigencyInTangWells.MODSS;
                        cs.ContigencyTangWells.MOD = getCurrentCurrency.Value * CapexSummary.ContigencyTangWells.MODSS;
                        cs.EPEXCompletionB2ExplInTang.MOD = getCurrentCurrency.Value * CapexSummary.EPEXCompletionB2ExplInTang.MODSS;
                        cs.EPEXCompletionB2ExplTang.MOD = getCurrentCurrency.Value * CapexSummary.EPEXCompletionB2ExplTang.MODSS;
                        cs.EPEXDrillingB2ExplInTang.MOD = getCurrentCurrency.Value * CapexSummary.EPEXDrillingB2ExplInTang.MODSS;
                        cs.EPEXDrillingB2ExplTang.MOD = getCurrentCurrency.Value * CapexSummary.EPEXDrillingB2ExplTang.MODSS;
                        cs.OPCostIdleRig.MOD = getCurrentCurrency.Value * CapexSummary.OPCostIdleRig.MODSS;
                    }
                    else
                    {
                        cs.CapitalCompletionPDDevInTang.RealTerm = getCurrentCurrency.Value * CapexSummary.CapitalCompletionPDDevInTang.RealTermSS;
                        cs.CapitalCompletionPDDevTang.RealTerm = getCurrentCurrency.Value * CapexSummary.CapitalCompletionPDDevTang.RealTermSS;
                        cs.CapitalDrillingPDDevInTang.RealTerm = getCurrentCurrency.Value * CapexSummary.CapitalDrillingPDDevInTang.RealTermSS;
                        cs.CapitalDrillingPDDevTang.RealTerm = getCurrentCurrency.Value * CapexSummary.CapitalDrillingPDDevTang.RealTermSS;
                        cs.CapitalExpenDRDVAWells.RealTerm = getCurrentCurrency.Value * CapexSummary.CapitalExpenDRDVAWells.RealTermSS;
                        cs.CapitalExpenDRSubSeaWells.RealTerm = getCurrentCurrency.Value * CapexSummary.CapitalExpenDRSubSeaWells.RealTermSS;
                        cs.ContigencyInTangWells.RealTerm = getCurrentCurrency.Value * CapexSummary.ContigencyInTangWells.RealTermSS;
                        cs.ContigencyTangWells.RealTerm = getCurrentCurrency.Value * CapexSummary.ContigencyTangWells.RealTermSS;
                        cs.EPEXCompletionB2ExplInTang.RealTerm = getCurrentCurrency.Value * CapexSummary.EPEXCompletionB2ExplInTang.RealTermSS;
                        cs.EPEXCompletionB2ExplTang.RealTerm = getCurrentCurrency.Value * CapexSummary.EPEXCompletionB2ExplTang.RealTermSS;
                        cs.EPEXDrillingB2ExplInTang.RealTerm = getCurrentCurrency.Value * CapexSummary.EPEXDrillingB2ExplInTang.RealTermSS;
                        cs.EPEXDrillingB2ExplTang.RealTerm = getCurrentCurrency.Value * CapexSummary.EPEXDrillingB2ExplTang.RealTermSS;
                        cs.OPCostIdleRig.RealTerm = getCurrentCurrency.Value * CapexSummary.OPCostIdleRig.RealTermSS;
                    }
                    break;
                case "Total Gross":
                    if (pq.MoneyType.Equals("EDM"))
                    {
                        cs.CapitalCompletionPDDevInTang.EDM = getCurrentCurrency.Value * CapexSummary.CapitalCompletionPDDevInTang.EDM;
                        cs.CapitalCompletionPDDevTang.EDM = getCurrentCurrency.Value * CapexSummary.CapitalCompletionPDDevTang.EDM;
                        cs.CapitalDrillingPDDevInTang.EDM = getCurrentCurrency.Value * CapexSummary.CapitalDrillingPDDevInTang.EDM;
                        cs.CapitalDrillingPDDevTang.EDM = getCurrentCurrency.Value * CapexSummary.CapitalDrillingPDDevTang.EDM;
                        cs.CapitalExpenDRDVAWells.EDM = getCurrentCurrency.Value * CapexSummary.CapitalExpenDRDVAWells.EDM;
                        cs.CapitalExpenDRSubSeaWells.EDM = getCurrentCurrency.Value * CapexSummary.CapitalExpenDRSubSeaWells.EDM;
                        cs.ContigencyInTangWells.EDM = getCurrentCurrency.Value * CapexSummary.ContigencyInTangWells.EDM;
                        cs.ContigencyTangWells.EDM = getCurrentCurrency.Value * CapexSummary.ContigencyTangWells.EDM;
                        cs.EPEXCompletionB2ExplInTang.EDM = getCurrentCurrency.Value * CapexSummary.EPEXCompletionB2ExplInTang.EDM;
                        cs.EPEXCompletionB2ExplTang.EDM = getCurrentCurrency.Value * CapexSummary.EPEXCompletionB2ExplTang.EDM;
                        cs.EPEXDrillingB2ExplInTang.EDM = getCurrentCurrency.Value * CapexSummary.EPEXDrillingB2ExplInTang.EDM;
                        cs.EPEXDrillingB2ExplTang.EDM = getCurrentCurrency.Value * CapexSummary.EPEXDrillingB2ExplTang.EDM;
                        cs.OPCostIdleRig.EDM = getCurrentCurrency.Value * CapexSummary.OPCostIdleRig.EDM;
                    }
                    else if (pq.MoneyType.Equals("MOD"))
                    {
                        cs.CapitalCompletionPDDevInTang.MOD = getCurrentCurrency.Value * CapexSummary.CapitalCompletionPDDevInTang.MOD;
                        cs.CapitalCompletionPDDevTang.MOD = getCurrentCurrency.Value * CapexSummary.CapitalCompletionPDDevTang.MOD;
                        cs.CapitalDrillingPDDevInTang.MOD = getCurrentCurrency.Value * CapexSummary.CapitalDrillingPDDevInTang.MOD;
                        cs.CapitalDrillingPDDevTang.MOD = getCurrentCurrency.Value * CapexSummary.CapitalDrillingPDDevTang.MOD;
                        cs.CapitalExpenDRDVAWells.MOD = getCurrentCurrency.Value * CapexSummary.CapitalExpenDRDVAWells.MOD;
                        cs.CapitalExpenDRSubSeaWells.MOD = getCurrentCurrency.Value * CapexSummary.CapitalExpenDRSubSeaWells.MOD;
                        cs.ContigencyInTangWells.MOD = getCurrentCurrency.Value * CapexSummary.ContigencyInTangWells.MOD;
                        cs.ContigencyTangWells.MOD = getCurrentCurrency.Value * CapexSummary.ContigencyTangWells.MOD;
                        cs.EPEXCompletionB2ExplInTang.MOD = getCurrentCurrency.Value * CapexSummary.EPEXCompletionB2ExplInTang.MOD;
                        cs.EPEXCompletionB2ExplTang.MOD = getCurrentCurrency.Value * CapexSummary.EPEXCompletionB2ExplTang.MOD;
                        cs.EPEXDrillingB2ExplInTang.MOD = getCurrentCurrency.Value * CapexSummary.EPEXDrillingB2ExplInTang.MOD;
                        cs.EPEXDrillingB2ExplTang.MOD = getCurrentCurrency.Value * CapexSummary.EPEXDrillingB2ExplTang.MOD;
                        cs.OPCostIdleRig.MOD = getCurrentCurrency.Value * CapexSummary.OPCostIdleRig.MOD;
                    }
                    else
                    {
                        cs.CapitalCompletionPDDevInTang.RealTerm = getCurrentCurrency.Value * CapexSummary.CapitalCompletionPDDevInTang.RealTerm;
                        cs.CapitalCompletionPDDevTang.RealTerm = getCurrentCurrency.Value * CapexSummary.CapitalCompletionPDDevTang.RealTerm;
                        cs.CapitalDrillingPDDevInTang.RealTerm = getCurrentCurrency.Value * CapexSummary.CapitalDrillingPDDevInTang.RealTerm;
                        cs.CapitalDrillingPDDevTang.RealTerm = getCurrentCurrency.Value * CapexSummary.CapitalDrillingPDDevTang.RealTerm;
                        cs.CapitalExpenDRDVAWells.RealTerm = getCurrentCurrency.Value * CapexSummary.CapitalExpenDRDVAWells.RealTerm;
                        cs.CapitalExpenDRSubSeaWells.RealTerm = getCurrentCurrency.Value * CapexSummary.CapitalExpenDRSubSeaWells.RealTerm;
                        cs.ContigencyInTangWells.RealTerm = getCurrentCurrency.Value * CapexSummary.ContigencyInTangWells.RealTerm;
                        cs.ContigencyTangWells.RealTerm = getCurrentCurrency.Value * CapexSummary.ContigencyTangWells.RealTerm;
                        cs.EPEXCompletionB2ExplInTang.RealTerm = getCurrentCurrency.Value * CapexSummary.EPEXCompletionB2ExplInTang.RealTerm;
                        cs.EPEXCompletionB2ExplTang.RealTerm = getCurrentCurrency.Value * CapexSummary.EPEXCompletionB2ExplTang.RealTerm;
                        cs.EPEXDrillingB2ExplInTang.RealTerm = getCurrentCurrency.Value * CapexSummary.EPEXDrillingB2ExplInTang.RealTerm;
                        cs.EPEXDrillingB2ExplTang.RealTerm = getCurrentCurrency.Value * CapexSummary.EPEXDrillingB2ExplTang.RealTerm;
                        cs.OPCostIdleRig.RealTerm = getCurrentCurrency.Value * CapexSummary.OPCostIdleRig.RealTerm;
                    }
                    break;
            }
            //cs = CapexSummary;
        }

        public List<PMasterResult> GetSSTGPMaster(string sstg, string currency, List<PMasterResult> Details)
        {
            //string curr = string.IsNullOrEmpty(currency.ElementAt(0)) ? "USD" : currency.ElementAt(0);
            //string curr = currency.ElementAt(0) == "Select Currency ..." ? "USD" : currency.ElementAt(0); 
            CalcCostCurrency cr = new CalcCostCurrency();
            var getCurrentCurrency = cr.GetCurrentCurrency(currency);

            //sstg = string.IsNullOrEmpty(sstg) ? "Total Gross" : "Shell Share";
            switch (sstg)
            {
                case "Shell Share":
                    foreach (var detail in Details)
                    {
                        if (detail.Details.Any())
                        {
                            detail.Details.ForEach(x => {
                                    x.value = x.valueSS * getCurrentCurrency.Value;
                            });
                        }
                    }
                    break;
                case "Total Gross":
                    foreach (var detail in Details)
                    {
                        if (detail.Details.Any())
                        {
                            detail.Details.ForEach(x =>
                            {
                                    x.value = x.value * getCurrentCurrency.Value;
                            });
                        }
                    }
                    break;
            }

            return Details;
        }

        public List<PMasterStandardResult> GetSSTGPMaster(string sstg, string currency, List<PMasterStandardResult> Details)
        {
            //string curr = string.IsNullOrEmpty(currency.ElementAt(0)) ? "USD" : currency.ElementAt(0);
            //string curr = currency.ElementAt(0) == "Select Currency ..." ? "USD" : currency.ElementAt(0); 
            CalcCostCurrency cr = new CalcCostCurrency();
            var getCurrentCurrency = cr.GetCurrentCurrency(currency);

            //sstg = string.IsNullOrEmpty(sstg) ? "Total Gross" : "Shell Share";
            switch (sstg)
            {
                case "Shell Share":
                    foreach (var detail in Details)
                    {
                        if (detail.StandardDetails.Any())
                        {
                            detail.StandardDetails.ForEach(x =>
                            {
                                    x.value = x.valueSS * getCurrentCurrency.Value;
                            });
                        }
                    }
                    break;
                case "Total Gross":
                    foreach (var detail in Details)
                    {
                        if (detail.StandardDetails.Any())
                        {
                            detail.StandardDetails.ForEach(x =>
                            {
                                    x.value = x.value * getCurrentCurrency.Value;

                            });
                        }
                    }
                    break;
            }

            return Details;
        }

        public List<STOSResult> SstgSTOS(string sstg, string currency, List<STOSResult> stos)
        {
            //string curr = currency.ElementAt(0) == "Select Currency ..." ? "USD" : currency.ElementAt(0); 
            //string curr = string.IsNullOrEmpty(currency.ElementAt(0)) ? "USD" : currency.ElementAt(0); 
            CalcCostCurrency cr = new CalcCostCurrency();
            var getCurrentCurrency = cr.GetCurrentCurrency(currency);

            //sstg = string.IsNullOrEmpty(sstg) ? "Total Gross" : "Shell Share";
            switch (sstg)
            {
                case "Shell Share":
                    foreach (var item in stos)
                    {
                        if (item.Details.Any())
                        {
                            item.Details.ForEach(x =>
                            {
                                if (x.valueSS != 0)
                                {
                                    x.value = x.WithSSValue * getCurrentCurrency.Value;
                                    x.WithSSValue = 0;
                                }
                            });
                        }
                    }
                    break;
                case "Total Gross":
                    foreach (var item in stos)
                    {
                        if (item.Details.Any())
                        {
                            item.Details.ForEach(x => {
                                if (x.valueSS != 0)
                                {
                                    x.value = x.value * getCurrentCurrency.Value;
                                }
                            });
                        }
                    }
                    break;
            }
            return stos;
        }
    }
}
