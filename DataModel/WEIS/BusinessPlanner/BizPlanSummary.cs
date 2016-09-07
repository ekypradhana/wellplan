using ECIS.Core;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECIS.Client.WEIS
{
    public class BizPlanSummary
    {

        #region Parameter
        public string WellName { get; set; }
        public string RigName { get; set; }
        public string ActivityType { get; set; }
        public string Country { get; set; }

        public double ShellShare { get; set; }
        public DateRange EventPeriod { get; set; }

        public int MaturityLevel { get; set; }
        public double Services { get; set; }
        public double Material { get; set; }

        public string RefFactorModel { get; set; }
        public double NPTRate { get; set; }
        public double TECOPRate { get; set; }
        public double NPTRateCost { get; set; }
        public double TECOPRateCost { get; set; }
        public string BaseOP { get; set; }
        #endregion
        #region Calculate
        public Dictionary<int, double> ExchangeRates { get; set; }
        public WEISReferenceFactorModel RefFactorModels { get; set; }
        public DateRange MaterialLongLeadPeriod { get; set; }
        public FYLongLead MaterialLongLeadValue { get; set; }
        public DateTime FinishOpsDate { get; set; }
        public DateTime FinishActiveDate { get; set; }
        public Dictionary<DateTime, double> RigRatePerDays { get; set; }

        public double RigRate { get; set; }
        public double LLI { get; set; }
        public double MeanTime { get; set; }
        public WellDrillData NPT { get; set; }
        public WellDrillData TECOP { get; set; }
        public double TroubleFreeBeforeLC { get; set; }
        public WellDrillData TroubleFree { get; set; }
        public double MeanCostEDM { get; set; }
        public double BurnRate { get; set; }
        public double SpreadRateWRig { get; set; }
        public double SpreadRateTotal { get; set; }

        public double MeanCostRealTerm { get; set; }
        public double MeanCostMOD { get; set; }
        public double ShellShareCost { get; set; }

        public double TroubleFreeCostUSD { get; set; }
        public double NPTCostUSD { get; set; }
        public double TECOPCostUSD { get; set; }
        public double MeanCostEDMUSD { get; set; }


        public List<BizPlanPeriodBucket> MonthlyBuckets { get; set; }
        public List<BizPlanPeriodBucket> AnnualyBuckets { get; set; }
        public List<BizPlanPeriodBucket> QuarterBuckets { get; set; }
        #endregion

        public double LCF { get; set; }
        public WellDrillData NewBaseValue { get; set; }
        public WellDrillData NewLCFValue { get; set; }


        public BizPlanSummary(string wellName,
            string rigName,
            string activitytype,
            string country,
            double shellshare,
            DateRange eventperiod,
            int maturitylevel,
            double services,
            double material,
            double troublefreetime,
            string reffactormodel,
            double nptrate,
            double tecoprate,
            double nptratecost,
            double tecopratecost,
            double monthrequired,
            double tangiblevalue,

            // Learning CUrve Factor
            double lcf,

            string baseOP = "OP15",
            string currency = "USD",
            bool isGetExcRateByCurrency = false)
        {
            NewBaseValue = new WellDrillData();
            NewLCFValue = new WellDrillData();
            MonthlyBuckets = new List<BizPlanPeriodBucket>();
            AnnualyBuckets = new List<BizPlanPeriodBucket>();
            QuarterBuckets = new List<BizPlanPeriodBucket>();
            EventPeriod = new DateRange();
            NPT = new WellDrillData();
            TECOP = new WellDrillData();
            TroubleFree = new WellDrillData();
            ExchangeRates = new Dictionary<int, double>();
            RefFactorModels = new WEISReferenceFactorModel();

            ShellShare = shellshare > 1 ? Tools.Div(shellshare, 100) : shellshare;
            WellName = wellName;
            RigName = rigName;
            ActivityType = activitytype;
            EventPeriod = eventperiod;
            MaturityLevel = maturitylevel;
            Country = country;
            //TroubleFreeBeforeLC = troublefreetime;
            TroubleFree.Days = troublefreetime;
            RefFactorModel = reffactormodel;
            Material = material;
            Services = services;
            BaseOP = baseOP;

            NPTRate = nptrate;
            NPTRateCost = nptratecost;
            TECOPRateCost = tecopratecost;
            TECOPRate = tecoprate;

            LCF = lcf > 1 ? Tools.Div(lcf, 100) : lcf;


            // Refference Factor Model
            if (reffactormodel == null)
                reffactormodel = "";


            #region update trouble free
            // update TroubleFree with LC

            var qsrfm = new List<IMongoQuery>();
            qsrfm.Add(Query.EQ("GroupCase", reffactormodel));
            qsrfm.Add(Query.EQ("BaseOP", baseOP));
            qsrfm.Add(Query.EQ("Country", country));

            RefFactorModels = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(Query.And(qsrfm));
            //RefFactorModels = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(Query.EQ("GroupCase", reffactormodel));

            if (RefFactorModels == null)
            {
                // create defaul
                RefFactorModels = WEISReferenceFactorModel.CreateDefaultRFM(reffactormodel, country, baseOP);
                #region
                //RefFactorModels = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(Query.EQ("GroupCase", RefFactorModel));
                //qsrfm = new List<IMongoQuery>();
                //qsrfm.Add(Query.EQ("GroupCase", RefFactorModel));
                //qsrfm.Add(Query.EQ("BaseOP", BaseOP));

                //RefFactorModels = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(Query.And(qsrfm));
                #endregion
            }
            // learning curve
            var lcRefModel = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Learning Curve Factors"));
            double lcVal = 0;
            if (lcRefModel.Any())
            {
                var ess = lcRefModel.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + eventperiod.Start.Year.ToString()));
                if (ess.Any())
                    lcVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                //if (lcVal < 0)
                //    lcVal = Tools.Div(lcVal, divRFM);

            }

            //TroubleFree.Days = TroubleFree.Days - (TroubleFree.Days * lcVal);

            #endregion

            #region Material Long Lead
            MaterialLongLeadValue = new FYLongLead();
            MaterialLongLeadValue.MonthRequiredValue = monthrequired;
            MaterialLongLeadValue.Year = eventperiod.Start.Year;
            MaterialLongLeadValue.TangibleValue = tangiblevalue;
            #endregion

            LLI = Material * MaterialLongLeadValue.TangibleValue;

            var baseTime = troublefreetime + Tools.Div((troublefreetime * NPTRate), (1 - NPTRate));

            //remarked because of 0 value if the end date is january
            //FinishOpsDate = EventPeriod.Start.AddDays(troublefreetime - 1);

            FinishOpsDate = EventPeriod.Start.AddDays(baseTime);

            // RigRate

            RigRatePerDays = new Dictionary<DateTime, double>();
            double rRate = 0;
            RigRatePerDays = GetRigRate(out rRate);
            RigRate = rRate;

            NPT.Days = Tools.Div(TroubleFree.Days * NPTRate, (1 - NPTRate)); // same calc

            NewLCFValue.Days = lcf * (TroubleFree.Days + NPT.Days);
            NewBaseValue.Days = TroubleFree.Days + NPT.Days;

            // old
            //TECOP.Days = Tools.Div(TECOPRate * (TroubleFree.Days + NPT.Days) * (TroubleFree.Cost + NPT.Cost - Material), (TroubleFree.Cost + NPT.Cost));

            // TECOP Time (days) = ( TECOP% * {Trouble Free Days + NPT Days - LCF Days} )  *  
            //[ ( Trouble Free Cost + NPT Cost - Materials) / ( Trouble Free Cost + NPT Cost ) ]


            //            TECOP Time (days) = ( TECOP% * {Trouble Free Days + NPT Days - LCF Days} )  *  [ ( Trouble Free Cost + NPT Cost - Materials) / ( Trouble Free Cost + NPT Cost ) ]
            //Mean Time (days) =  ( Trouble Free Days + NPT Days + TECOP Days - LCF Days )

            TroubleFree.Cost = (TroubleFree.Days * RigRate) + Services + Material;
            NPT.Cost = (TroubleFree.Cost - Material) * Tools.Div(NPTRateCost, (1 - NPTRateCost));

            TECOP.Days = (TECOPRate * (TroubleFree.Days + NPT.Days - NewLCFValue.Days)) * (Tools.Div(TroubleFree.Cost + NPT.Cost - Material, TroubleFree.Cost + NPT.Cost));
            MeanTime = NPT.Days + TECOP.Days + TroubleFree.Days - NewLCFValue.Days;

            NewLCFValue.Cost = lcf * (TroubleFree.Cost + NPT.Cost);
            NewBaseValue.Cost = TroubleFree.Cost + NPT.Cost;



            // old  TECOP.Cost = TECOPRateCost * (TroubleFree.Cost + NPT.Cost);
            TECOP.Cost = TECOPRateCost * (TroubleFree.Cost + NPT.Cost - NewLCFValue.Cost);
            // old MeanCostEDM = TroubleFree.Cost + NPT.Cost + TECOP.Cost;
            MeanCostEDM = (TroubleFree.Cost + NPT.Cost + TECOP.Cost) - NewLCFValue.Cost;

            FinishActiveDate = EventPeriod.Start.AddDays(MeanTime);


            #region eky : agak bingung sama logic ini
            // tadinya waktu panggil fungsi ini dari fungsi busplan/GetDataFiscalYearOnBizPlanEntry2, param : isGetExcRateByCurrency ngga diisi, jadi false ngikutin default
            // sehingga menyebabkan result berubah waktu change country, padahal RFM nya sama, itu karena dia masuk ke block yg false, dan exchange rate jadi ngikutin exchange rate dari country
            // padahal kalau tak pikir pikir, harusnya ngga perlu, lha wong masuk ke sini kan sudah dalam bentuk USD semua inputtannya (diconvert dulu di depan)
            // jadi solvingnya param : isGetExcRateByCurrency diset ke true, tp masih ngganjel aja, apa di module lain butuh logic ini juga?
            if (isGetExcRateByCurrency)
            {
                // Exchange Rate
                for (int i = EventPeriod.Start.Year; i <= FinishActiveDate.Year; i++)
                    ExchangeRates.Add(i, BizPlanAllocation.GetExchangeRateByCurrency(currency, i, baseOP));
            }
            else
            {
                // Exchange Rate
                for (int i = EventPeriod.Start.Year; i <= FinishActiveDate.Year; i++)
                    ExchangeRates.Add(i, BizPlanAllocation.GetExchangeRate(Country, i, baseOP));
            }
            #endregion




            BurnRate = Tools.Div(MeanCostEDM, MeanTime);
            SpreadRateWRig = Tools.Div((1 + TECOPRateCost) * (Services + (Services * (Tools.Div(NPTRateCost, (1 - NPTRateCost))))), MeanTime);
            SpreadRateTotal = Tools.Div((MeanCostEDM - (Material + (Material * TECOPRateCost))), MeanTime);
            #region Calc Lead Period
            var LeadDate = EventPeriod.Start.AddMonths(-1 * (Convert.ToInt32(monthrequired)));
            DateTime materialLeadDate = new DateTime();

            if (DateTime.Now.Date >= EventPeriod.Start.Date)
            {
                //EventPeriod.Start = DateTime.Now.Date;
                //materialLeadDate = DateTime.Now.Date;
                materialLeadDate = LeadDate.Date;
            }
            else
            {
                if (LeadDate.Date > DateTime.Now.Date)
                {
                    // hitung full leadDate 
                    materialLeadDate = LeadDate.Date;
                }
                else
                {
                    //LeadDate = DateTime.Now.Date;
                    materialLeadDate = LeadDate.Date;
                }
            }
            MaterialLongLeadPeriod = new DateRange(materialLeadDate, materialLeadDate);
            #endregion

            TroubleFreeCostUSD = ExchangeRates.Where(x => x.Key == eventperiod.Start.Year).Any() ? ExchangeRates.FirstOrDefault().Value * TroubleFree.Cost : 0;
            NPTCostUSD = ExchangeRates.Where(x => x.Key == eventperiod.Start.Year).Any() ? ExchangeRates.FirstOrDefault().Value * NPT.Cost : 0;
            TECOPCostUSD = ExchangeRates.Where(x => x.Key == eventperiod.Start.Year).Any() ? ExchangeRates.FirstOrDefault().Value * TECOP.Cost : 0;
            MeanCostEDMUSD = ExchangeRates.Where(x => x.Key == eventperiod.Start.Year).Any() ? ExchangeRates.FirstOrDefault().Value * MeanCostEDM : 0;

        }

        public Dictionary<DateTime, double> GetRigRate(out double rigRate)
        {
            var rigRates = RigRatesNew.Populate<RigRatesNew>(Query.EQ("Title", RigName));
            Dictionary<DateTime, double> rigPerDays = new Dictionary<DateTime, double>();
            #region Calculate Rate Per Day

            DateTime nowDate;
            //if (DateTime.Now.Month < 7)
            //    nowDate = new DateTime(DateTime.Now.Year - 1, 7, 1);
            //else
            //    nowDate = new DateTime(DateTime.Now.Year, 7, 1);

            nowDate = new DateTime(DateTime.Now.Year, 7, 1);
            double reteJuly = 0;


            for (DateTime i = EventPeriod.Start; i <= FinishOpsDate; i = i.AddDays(1))
            {
                if (ActivityType.Contains("IDLE"))
                {
                    // rate idle 
                    if (rigRates.Any())
                    {
                        var idleRates = rigRates.FirstOrDefault().Values.Where(X => X.Type.Equals("IDLE"));
                        if (idleRates.Any())
                        {
                            double rateThisDay = 0;
                            foreach (var y in idleRates)
                            {
                                var isInRange = DateRangeToMonth.isDateInsideEqualofRange(i, y.Period);
                                if (isInRange)
                                {
                                    rateThisDay = y.Value;
                                    break;
                                }
                                else
                                {
                                    rateThisDay = 0;
                                }
                            }
                            rigPerDays.Add(i, rateThisDay);
                        }
                        // get 1 July  Rig Rate
                        if (idleRates.Any())
                        {
                            foreach (var y in idleRates)
                            {
                                var isInRange = DateRangeToMonth.isDateInsideEqualofRange(nowDate, y.Period);
                                if (isInRange)
                                {
                                    reteJuly = y.Value;
                                    break;
                                }
                                else
                                    reteJuly = 0;
                            }
                        }
                    }
                }
                else
                {
                    if (rigRates.Any())
                    {
                        var idleRates = rigRates.FirstOrDefault().Values.Where(X => !X.Type.Equals("IDLE"));
                        if (idleRates.Any())
                        {
                            double rateThisDay = 0;
                            foreach (var y in idleRates)
                            {
                                var isInRange = DateRangeToMonth.isDateInsideEqualofRange(i, y.Period);
                                if (isInRange)
                                {
                                    rateThisDay = y.Value;
                                    break;
                                }
                                else
                                    rateThisDay = 0;
                            }
                            rigPerDays.Add(i, rateThisDay);
                        }
                        // get 1 July  Rig Rate
                        if (idleRates.Any())
                        {
                            foreach (var y in idleRates)
                            {
                                var isInRange = DateRangeToMonth.isDateInsideEqualofRange(nowDate, y.Period);
                                if (isInRange)
                                {
                                    reteJuly = y.Value;
                                    break;
                                }
                                else
                                    reteJuly = 0;
                            }

                        }
                    }

                }
            }
            #endregion

            rigRate = reteJuly;

            return rigPerDays;
        }

        public void GeneratePeriodBucket()
        {
            List<BizPlanPeriodBucket> buckets = new List<BizPlanPeriodBucket>();
            double remainTecopCost = TECOP.Cost;
            double remainMeanDays = MeanTime;
            #region LL Bucket
            BizPlanPeriodBucket llbucket = new BizPlanPeriodBucket();
            llbucket.Key = MaterialLongLeadPeriod.Start.ToString("yyyyMM");
            llbucket.Type = "LL";
            llbucket.Period = MaterialLongLeadPeriod;
            llbucket.MeanDays = 0;
            llbucket.RigCost = 0;
            llbucket.MaterialCost = Material * MaterialLongLeadValue.TangibleValue;
            llbucket.ServiceCost = 0;
            llbucket.TroubleFreeCost = llbucket.RigCost + llbucket.MaterialCost + llbucket.ServiceCost;
            llbucket.NPTCost = 0;
            //old llbucket.TECOPCost = Material * (MaterialLongLeadValue.TangibleValue) * TECOPRateCost;


            llbucket.EscCostRig = 0;
            llbucket.EscCostServices = 0;


            if (RefFactorModels == null)
            {

                var qsrfm = new List<IMongoQuery>();
                qsrfm.Add(Query.EQ("GroupCase", RefFactorModel));
                qsrfm.Add(Query.EQ("BaseOP", BaseOP));
                qsrfm.Add(Query.EQ("Country", Country));

                RefFactorModels = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(Query.And(qsrfm));

                //RefFactorModels = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(Query.EQ("GroupCase", RefFactorModel));
                //var qsrfm = new List<IMongoQuery>();
                //qsrfm.Add(Query.EQ("GroupCase", RefFactorModel));
                //qsrfm.Add(Query.EQ("BaseOP", BaseOP));

                //RefFactorModels = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(Query.And(qsrfm));
            }
            // esc material
            var matllRefModel = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Material Escalation Factors"));
            double matllEscVal = 0;
            if (matllRefModel.Any())
            {
                var ess = matllRefModel.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + llbucket.Key.Substring(0, 4)));
                if (ess.Any())
                    matllEscVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                //if (matllEscVal > 0)
                //matllEscVal = Tools.Div(matllEscVal, 100);

            }


            //llbucket.EscCostMaterial = matllEscVal * (Material * (MaterialLongLeadValue.TangibleValue)) * (1 + TECOPRateCost);
            llbucket.EscCostMaterial = matllEscVal * (Material * (MaterialLongLeadValue.TangibleValue)) * (1 + TECOPRateCost) * (1 - LCF);

            llbucket.EscCostTotal = llbucket.EscCostMaterial + llbucket.EscCostRig + llbucket.EscCostServices;

            llbucket.LCCost = this.LCF * (llbucket.TroubleFreeCost + llbucket.NPTCost);

            // new TECOP
            llbucket.TECOPCost = TECOPRateCost * ((llbucket.TroubleFreeCost + llbucket.NPTCost) * (1 - LCF)); //Material * (MaterialLongLeadValue.TangibleValue) * TECOPRateCost;
            remainTecopCost -= llbucket.TECOPCost;
            llbucket.MeanCostEDM = llbucket.TroubleFreeCost + llbucket.NPTCost + llbucket.TECOPCost - llbucket.LCCost;


            // CSO Cost
            var csollFc = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("CSO Factors"));
            double csollFcVal = 0;
            if (csollFc.Any())
            {
                var ess = csollFc.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + llbucket.Key.Substring(0, 4)));
                if (ess.Any())
                    csollFcVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                //if (csollFcVal < 0)
                //    csollFcVal = Tools.Div(csollFcVal, 100);
            }
            llbucket.CSOCost = csollFcVal * (llbucket.MeanCostEDM + llbucket.EscCostTotal);

            // Inflation Cost EDM
            var infll = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Inflation Factors"));
            double infllVal = 0;
            if (infll.Any())
            {
                var ess = infll.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + llbucket.Key.Substring(0, 4)));
                if (ess.Any())
                    infllVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                //if (infllVal < 0)
                //    infllVal = Tools.Div(infllVal, 100);
            }
            llbucket.InflationCost = infllVal * (llbucket.MeanCostEDM + llbucket.EscCostTotal + llbucket.CSOCost);

            llbucket.MeanCostMOD = llbucket.MeanCostEDM + llbucket.EscCostTotal + llbucket.CSOCost + llbucket.InflationCost;
            llbucket.MeanCostRealTerm = Tools.Div(llbucket.MeanCostMOD, (1 + infllVal));
            llbucket.ShellShare = llbucket.MeanCostMOD * ShellShare;


            llbucket.MeanCostMODSS = llbucket.MeanCostMOD * ShellShare;
            llbucket.MeanCostRealTermSS = llbucket.MeanCostRealTerm * ShellShare;
            llbucket.MeanCostEDMSS = llbucket.MeanCostEDM * ShellShare;

            buckets.Add(llbucket);

            #endregion
            // TECOP COST Di Prorate ke Active 
            #region Active
            var activeBuckets = DateRangeToMonth.NumDaysPerMonth(new DateRange(EventPeriod.Start, FinishOpsDate));

            int index = 0;

            //Mean Cost EDM Rig Rate = {   (Rig Rate * Trouble Free Days) + [ (Rig Rate * Trouble Free Days) * ( NPT Cost % * ( 1 – NPT Cost % ) ) ] + 
            //    TECOP Cost % * (Rig Rate * Trouble Free Days) + [ (Rig Rate * Trouble Free Days) * ( NPT Cost % * ( 1 – NPT Cost % ) ) ]  }   / Mean Time Days

            var meancostEDMRigRates = Tools.Div(((RigRate * TroubleFree.Days) +
               ((RigRate * TroubleFree.Days) * Tools.Div(NPTRateCost, (1 - NPTRateCost))) +
               ((RigRate * TroubleFree.Days) + ((RigRate * TroubleFree.Days) * Tools.Div(NPTRateCost, (1 - NPTRateCost)))) * TECOPRateCost), MeanTime);

            double materialonactive = Material * (1 - MaterialLongLeadValue.TangibleValue);

            double servicecostremain = Services;

            foreach (var r in activeBuckets)
            {

                index += 1;
                BizPlanPeriodBucket llactv = new BizPlanPeriodBucket();
                llactv.Key = r.Key.ToString();
                llactv.Type = "Active";
                llactv.Period = r.Value.FirstOrDefault().Key;

                llactv.MeanCostEDMRigRate = meancostEDMRigRates;


                llactv.MeanDays = llactv.Period.Days + 1;
                remainMeanDays -= llactv.MeanDays;

                if (remainMeanDays < 0)
                {
                    llactv.MeanDays = llactv.MeanDays + remainMeanDays;
                    remainMeanDays = 0.0;
                }

                //llactv.RigCost = llactv.MeanDays * RigRate;
                DateTime start = r.Value.FirstOrDefault().Key.Start;
                DateTime finish = r.Value.FirstOrDefault().Key.Finish;

                //llactv.RigCost = RigRate * llactv.MeanDays; //RigRatePerDays.Where(x => (x.Key >= start) && (x.Key <= finish)).Sum(x => x.Value);
                //llactv.RigCost = Tools.Div(llactv.MeanDays, MeanTime) * RigRate * TroubleFree.Days;

                if (llactv.MeanDays <= 0)
                {
                    llactv.MeanDays = 0;
                }

                if (llactv.MeanDays > MeanTime)
                {
                    llactv.MeanDays = MeanTime;
                }
                llactv.RigCost = Tools.Div(llactv.MeanDays, MeanTime) * RigRate * TroubleFree.Days;


                if (index == 1)
                    llactv.MaterialCost = materialonactive;
                else
                    llactv.MaterialCost = 0;
                llactv.ServiceCost = Tools.Div(llactv.MeanDays, MeanTime) * Services;

                servicecostremain -= llactv.ServiceCost;

                //if (servicecostremain <= 0)
                //    break;

                llactv.TroubleFreeCost = llactv.RigCost + llactv.MaterialCost + llactv.ServiceCost;
                llactv.NPTCost = Tools.Div(llactv.MeanDays, MeanTime) * NPT.Cost; // (llactv.TroubleFreeCost - llactv.MaterialCost) * Tools.Div(NPTRateCost, 1 - NPTRateCost); ;
                //llactv.TECOPCost = TECOPRateCost * (llactv.TroubleFreeCost + llactv.NPTCost);// Material * (MaterialLongLeadValue.TangibleValue) * TECOPRateCost;
                //llactv.TECOPCost = Tools.Div(
                //    llactv.TroubleFreeCost, (TroubleFree.Cost - llbucket.TroubleFreeCost)
                //    ) * (TECOP.Cost - llbucket.TECOPCost);


                // old tecop cost
                //llactv.TECOPCost = Tools.Div(llactv.MeanDays, MeanTime) * (TECOP.Cost - llbucket.TECOPCost);

                remainTecopCost -= llactv.TECOPCost;
                #region Rig Escalation
                // (MEAN Days in Plan Year * Mean Cost EDM Rig Rate) * [ (Plan Year Rig Rate  -  EDM Year Rig Rate) / EDM Year Rig Rate) ]
                double rateNow = 0;
                double rateSpecNow = 0;
                #region cari di database
                var rigRates = RigRatesNew.Populate<RigRatesNew>(Query.EQ("Title", RigName));
                Dictionary<DateTime, double> rigs = new Dictionary<DateTime, double>();


                DateTime nowDate;
                //if (DateTime.Now.Month < 7)
                //{
                //    nowDate = new DateTime(DateTime.Now.Year - 1, 7, 1);
                //}
                //else
                //{
                nowDate = new DateTime(DateTime.Now.Year, 7, 1);
                //}

                if (rigRates.Any())
                {
                    var values = rigRates.FirstOrDefault().Values;
                    if (ActivityType.Contains("IDLE"))
                    {
                        var valIdle = values.Where(x => x.Type.Equals("IDLE"));
                        if (valIdle.Any())
                        {
                            foreach (var idl in valIdle)
                            {
                                bool isInRange = DateRangeToMonth.isDateInsideEqualofRange(nowDate, idl.Period);
                                if (isInRange)
                                {
                                    rateNow = idl.Value;
                                    break;
                                }
                            }

                            foreach (var idl in valIdle)
                            {
                                bool isInRange = DateRangeToMonth.isDateInsideEqualofRange(r.Value.FirstOrDefault().Key.Start, idl.Period);
                                if (isInRange)
                                {
                                    rateSpecNow = idl.Value;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        var valIdle = values.Where(x => !x.Type.Equals("IDLE"));
                        if (valIdle.Any())
                        {
                            foreach (var idl in valIdle)
                            {
                                bool isInRange = DateRangeToMonth.isDateInsideEqualofRange(nowDate, idl.Period);
                                if (isInRange)
                                {
                                    rateNow = idl.Value;
                                    break;
                                }
                            }
                            foreach (var idl in valIdle)
                            {
                                bool isInRange = DateRangeToMonth.isDateInsideEqualofRange(r.Value.FirstOrDefault().Key.Start, idl.Period);
                                if (isInRange)
                                {
                                    rateSpecNow = idl.Value;
                                    break;
                                }
                            }
                        }
                    }
                }
                #endregion
                // persentasi harian per bulan
                double percentage = 0.0;
                double valueBulanan = 0.0;
                Dictionary<DateTime, double> blns = new Dictionary<DateTime, double>();

                var remainmeanactive = llactv.MeanDays;

                for (DateTime d = llactv.Period.Start.Date; d <= llactv.Period.Finish.Date; )
                {
                    var pengali = 1.0;
                    //RigRatePerDays.Where(x=>x.)
                    if (remainmeanactive < 1 && remainmeanactive>0)
                        pengali = remainmeanactive;

                    if (remainmeanactive <= 0)
                        pengali = 0;

                    var founddata = RigRatePerDays.Where(x => x.Key.Date.Equals(d));
                    if (founddata != null && founddata.Count() > 0)
                    {
                        var datenya = founddata.FirstOrDefault().Key;
                        var valuenya = founddata.FirstOrDefault().Value;

                        percentage = (Tools.Div((valuenya - rateNow), rateNow)) * pengali;
                        valueBulanan = valueBulanan + (meancostEDMRigRates * percentage);
                        blns.Add(d, (meancostEDMRigRates * percentage));
                    }
                    remainmeanactive -= 1;
                    d = d.AddDays(1);
                }
                var total = blns.Sum(x => x.Value);
                double rigEscCost = 0.0;
                rigEscCost = valueBulanan; //(llactv.MeanDays * meancostEDMRigRates) * (Tools.Div((rateSpecNow - rateNow), rateNow));
                llactv.EscCostRig = rigEscCost;
                #endregion

                #region Services Escalation
                // services escal
                var servicRefModel = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Service Escalation Factors"));
                double serviceEscVal = 0;
                if (servicRefModel.Any())
                {
                    var ess = servicRefModel.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + llactv.Key.Substring(0, 4)));
                    if (ess.Any())
                        serviceEscVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                    //if (serviceEscVal < 0)
                    //    serviceEscVal = Tools.Div(serviceEscVal, 100);
                }
                double calcSer = 0;




                #region new calc
                var NPTServicesYear = Tools.Div(llactv.MeanDays, MeanTime) * Services * Tools.Div(NPTRate, (1 - NPTRate));
                var ServicesYear = Tools.Div(llactv.MeanDays, MeanTime) * Services;

                calcSer = serviceEscVal * (NPTServicesYear + ServicesYear) * (1 + TECOPRateCost) * (1 - LCF);
                #endregion

                #region old calc
                //if (EventPeriod.Start.Year == EventPeriod.Finish.Year)
                //{
                //    // cek
                //    calcSer = Tools.Div(llactv.MeanDays, MeanTime) * serviceEscVal *
                //        (((Services + (Services * (Tools.Div(NPTRateCost, (1 - NPTRateCost))) + TECOPRateCost * ((Services + (Services * (Tools.Div(NPTRateCost, (1 - NPTRateCost))))))))));

                //}
                //else
                //{
                //    calcSer = Tools.Div(llactv.MeanDays, MeanTime) * serviceEscVal * (((Services + (Services * (Tools.Div(NPTRateCost, (1 - NPTRateCost))) +
                //    TECOPRateCost * ((Services + (Services * (Tools.Div(NPTRateCost, (1 - NPTRateCost))))))))));

                //}
                #endregion
                llactv.EscCostServices = calcSer;

                #endregion

                #region Material Escalation

                // material escal
                var matRefModel = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Material Escalation Factors"));
                double matescval = 0; // compound mat esc value annualy
                if (matRefModel.Any())
                {
                    var ess = matRefModel.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + llactv.Key.Substring(0, 4)));
                    if (ess.Any())
                        matescval = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                    //if (matescval < 0)
                    //    matescval = Tools.Div(matescval, 100);
                }
                if (index == 1)
                {
                    //llbucket.EscCostMaterial = matllEscVal * (Material * (MaterialLongLeadValue.TangibleValue)) * (1 + TECOPRateCost);

                    //var calc2 = matescval * (Material * (1 - MaterialLongLeadValue.TangibleValue)) * (1 + TECOPRateCost);
                    var calc2 = matescval * (Material * (1 - MaterialLongLeadValue.TangibleValue)) * (1 + TECOPRateCost) * (1 - LCF);
                    llactv.EscCostMaterial = calc2;
                }

                #endregion

                llactv.EscCostTotal = llactv.EscCostMaterial + llactv.EscCostRig + llactv.EscCostServices;

                llactv.LCCost = this.LCF * (llactv.TroubleFreeCost + llactv.NPTCost);


                // new tecop active 
                llactv.TECOPCost = TECOPRateCost * ((llactv.TroubleFreeCost + llactv.NPTCost) * (1 - LCF)); //Material * (MaterialLongLeadValue.TangibleValue) * TECOPRateCost;
                llactv.MeanCostEDM = llactv.TroubleFreeCost + llactv.NPTCost + llactv.TECOPCost - llactv.LCCost;

                // CSO Cost
                var csoFc = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("CSO Factors"));
                double csoFcVal = 0;
                if (matRefModel.Any())
                {
                    var ess = csoFc.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + llactv.Key.Substring(0, 4)));
                    if (ess.Any())
                        csoFcVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                    //if (csoFcVal < 0)
                    //    csoFcVal = Tools.Div(csoFcVal, 100);
                }
                llactv.CSOCost = csoFcVal * (llactv.MeanCostEDM + llactv.EscCostTotal);

                // Inflation Cost EDM
                var inf = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Inflation Factors"));
                double infVal = 0;
                if (matRefModel.Any())
                {
                    var ess = inf.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + llactv.Key.Substring(0, 4)));
                    if (ess.Any())
                        infVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                    //if (infVal < 0)
                    //    infVal = Tools.Div(infVal, 100);
                }
                llactv.InflationCost = infVal * (llactv.MeanCostEDM + llactv.EscCostTotal + llactv.CSOCost);

                llactv.MeanCostMOD = llactv.MeanCostEDM + llactv.EscCostTotal + llactv.CSOCost + llactv.InflationCost;
                llactv.MeanCostRealTerm = Tools.Div(llactv.MeanCostMOD, (1 + infVal));
                llactv.ShellShare = llactv.MeanCostMOD * ShellShare;


                llactv.MeanCostMODSS = llactv.MeanCostMOD * ShellShare;
                llactv.MeanCostRealTermSS = llactv.MeanCostRealTerm * ShellShare;
                llactv.MeanCostEDMSS = llactv.MeanCostEDM * ShellShare;

                buckets.Add(llactv);
            }
            #endregion

            string checkerFirstMonthActive = "";
            if(buckets != null && buckets.Count() > 0 && buckets.Where(x=>x.Type.Equals("Active")).Count() > 0)
            {
                checkerFirstMonthActive = buckets.OrderBy(x => x.Key).FirstOrDefault().Key;
            }

            #region Contigency
            DateTime ctgyStart = FinishOpsDate.AddDays(1);
            var contigencyBuckets = DateRangeToMonth.NumDaysPerMonth(new DateRange(ctgyStart, FinishActiveDate.AddDays(1)));

            if (remainMeanDays <= 0)
            {
                remainMeanDays = 0;
            }

            foreach (var r in contigencyBuckets)
            {
                BizPlanPeriodBucket ctgs = new BizPlanPeriodBucket();
                ctgs.Key = r.Key.ToString();
                ctgs.Type = "Contigency";
                ctgs.Period = r.Value.FirstOrDefault().Key;

               // ctgs.MeanDays = remainMeanDays;
                //remainMeanDays -= ctgs.MeanDays;
                ctgs.MeanDays = r.Value.FirstOrDefault().Value;
                if (ctgs.MeanDays > remainMeanDays)
                {
                    ctgs.MeanDays = remainMeanDays;
                }
                remainMeanDays -= ctgs.MeanDays;
                //ctgs.MeanDays = r.Value.FirstOrDefault().Value;
                
                //if (ctgs.MeanDays > remainMeanDays)
                //{
                //    ctgs.MeanDays = remainMeanDays;
                //}
                //remainMeanDays -= ctgs.MeanDays;



                if (ctgs.MeanDays <= 0)
                    ctgs.MeanDays = 0;

                ctgs.RigCost = Tools.Div(ctgs.MeanDays, MeanTime) * RigRate * TroubleFree.Days;

                ctgs.MaterialCost = 0;
                ctgs.ServiceCost = Tools.Div(ctgs.MeanDays, MeanTime) * Services;

                ctgs.TroubleFreeCost = ctgs.RigCost + ctgs.MaterialCost + ctgs.ServiceCost;
                //ctgs.NPTCost = (ctgs.TroubleFreeCost - ctgs.MaterialCost) * Tools.Div(NPTRateCost, 1 - NPTRateCost); ;
                ctgs.NPTCost = Tools.Div(ctgs.MeanDays, MeanTime) * NPT.Cost;
                //ctgs.TECOPCost = Tools.Div(
                //    ctgs.TroubleFreeCost, (TroubleFree.Cost - llbucket.TroubleFreeCost)
                //    ) * (TECOP.Cost - llbucket.TECOPCost);

                // old tecop cost
                //ctgs.TECOPCost = Tools.Div(ctgs.MeanDays, MeanTime) * (TECOP.Cost - llbucket.TECOPCost);

                remainTecopCost -= ctgs.TECOPCost;
                #region Rig Escalation
                // (MEAN Days in Plan Year * Mean Cost EDM Rig Rate) * [ (Plan Year Rig Rate  -  EDM Year Rig Rate) / EDM Year Rig Rate) ]
                double rateNow = 0;
                double rateSpecNow = 0;
                #region cari di database
                var rigRates = RigRatesNew.Populate<RigRatesNew>(Query.EQ("Title", RigName));
                Dictionary<DateTime, double> rigs = new Dictionary<DateTime, double>();
                if (rigRates.Any())
                {
                    var values = rigRates.FirstOrDefault().Values;
                    if (ActivityType.Contains("IDLE"))
                    {
                        var valIdle = values.Where(x => x.Type.Equals("IDLE"));
                        if (valIdle.Any())
                        {
                            foreach (var idl in valIdle)
                            {
                                bool isInRange = DateRangeToMonth.isDateInsideEqualofRange(DateTime.Now.Date, idl.Period);
                                if (isInRange)
                                {
                                    rateNow = idl.Value;
                                    break;
                                }
                            }

                            foreach (var idl in valIdle)
                            {
                                bool isInRange = DateRangeToMonth.isDateInsideEqualofRange(r.Value.FirstOrDefault().Key.Start, idl.Period);
                                if (isInRange)
                                {
                                    rateSpecNow = idl.Value;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        var valIdle = values.Where(x => !x.Type.Equals("IDLE"));
                        if (valIdle.Any())
                        {
                            foreach (var idl in valIdle)
                            {
                                bool isInRange = DateRangeToMonth.isDateInsideEqualofRange(DateTime.Now.Date, idl.Period);
                                if (isInRange)
                                {
                                    rateNow = idl.Value;
                                    break;
                                }
                            }
                            foreach (var idl in valIdle)
                            {
                                bool isInRange = DateRangeToMonth.isDateInsideEqualofRange(r.Value.FirstOrDefault().Key.Start, idl.Period);
                                if (isInRange)
                                {
                                    rateSpecNow = idl.Value;
                                    break;
                                }
                            }
                        }
                    }
                }
                #endregion
                var rigEscCost = (ctgs.MeanDays * meancostEDMRigRates) * (Tools.Div((rateSpecNow - rateNow), rateNow));
                ctgs.EscCostRig = rigEscCost;
                #endregion

                #region Services Escalation
                // services escal
                var servicRefModel = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Service Escalation Factors"));
                double serviceEscVal = 0;
                if (servicRefModel.Any())
                {
                    var ess = servicRefModel.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + ctgs.Key.Substring(0, 4)));
                    if (ess.Any())
                        serviceEscVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                    //if (serviceEscVal < 0)
                    //    serviceEscVal = Tools.Div(serviceEscVal, 100);
                }


                #region new calc
                var NPTServicesYear = Tools.Div(ctgs.MeanDays, MeanTime) * Services * Tools.Div(NPTRate, (1 - NPTRate));
                var ServicesYear = Tools.Div(ctgs.MeanDays, MeanTime) * Services;

                var calcSer = serviceEscVal * (NPTServicesYear + ServicesYear) * (1 + TECOPRateCost) * (1 - LCF);
                ctgs.EscCostServices = calcSer;
                #endregion

                #region old calc
                //var calcSer = Tools.Div(ctgs.MeanDays, MeanTime) * serviceEscVal * (((Services + (Services * (Tools.Div(NPTRateCost, (1 - NPTRateCost))) +
                //TECOPRateCost * ((Services + (Services * (Tools.Div(NPTRateCost, (1 - NPTRateCost))))))))));
                //ctgs.EscCostServices = calcSer;

                #endregion

                #endregion

                #region Material Escalation

                // material escal
                var matRefModel = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Material Escalation Factors"));
                double matescval = 0; // compound mat esc value annualy
                if (matRefModel.Any())
                {
                    var ess = matRefModel.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + ctgs.Key.Substring(0, 4)));
                    if (ess.Any())
                        matescval = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                    //if (matescval < 0)
                    //    matescval = Tools.Div(matescval, 100);
                }
                if (index == 1 && checkerFirstMonthActive != ctgs.Key)
                {
                    var calc2 = matescval * (Material * (1 - MaterialLongLeadValue.TangibleValue)) * (1 + TECOPRateCost) * (1 - LCF);
                    //var calc2 = matescval * (Material * (1 - MaterialLongLeadValue.TangibleValue)) * (1 + TECOPRateCost);
                    ctgs.EscCostMaterial = calc2;
                }
                index++;
                #endregion

                ctgs.EscCostTotal = ctgs.EscCostMaterial + ctgs.EscCostRig + ctgs.EscCostServices;

                ctgs.LCCost = this.LCF * (ctgs.TroubleFreeCost + ctgs.NPTCost);

                // new tecop cost
                ctgs.TECOPCost = TECOPRateCost * ((ctgs.TroubleFreeCost + ctgs.NPTCost) * (1 - LCF));
                ctgs.MeanCostEDM = ctgs.TroubleFreeCost + ctgs.NPTCost + ctgs.TECOPCost - ctgs.LCCost;

                // CSO Cost
                var csoFc = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("CSO Factors"));
                double csoFcVal = 0;
                if (matRefModel.Any())
                {
                    var ess = csoFc.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + ctgs.Key.Substring(0, 4)));
                    if (ess.Any())
                        csoFcVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                    //if (csoFcVal < 0)
                    //    csoFcVal = Tools.Div(csoFcVal, 100);
                }
                ctgs.CSOCost = csoFcVal * (ctgs.MeanCostEDM + ctgs.EscCostTotal);

                // Inflation Cost EDM
                var inf = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Inflation Factors"));
                double infVal = 0;
                if (matRefModel.Any())
                {
                    var ess = inf.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + ctgs.Key.Substring(0, 4)));
                    if (ess.Any())
                        infVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                    //if (infVal < 0)
                    //    infVal = Tools.Div(infVal, 100);
                }
                ctgs.InflationCost = infVal * (ctgs.MeanCostEDM + ctgs.EscCostTotal + ctgs.CSOCost);

                ctgs.MeanCostMOD = ctgs.MeanCostEDM + ctgs.EscCostTotal + ctgs.CSOCost + ctgs.InflationCost;
                ctgs.MeanCostRealTerm = Tools.Div(ctgs.MeanCostMOD, (1 + infVal));
                ctgs.ShellShare = ctgs.MeanCostMOD * ShellShare;

                ctgs.MeanCostMODSS = ctgs.MeanCostMOD * ShellShare;
                ctgs.MeanCostRealTermSS = ctgs.MeanCostRealTerm * ShellShare;
                ctgs.MeanCostEDMSS = ctgs.MeanCostEDM * ShellShare;

                buckets.Add(ctgs);
            }
            #endregion

            this.MonthlyBuckets = buckets;

            MeanCostRealTerm = MonthlyBuckets.Sum(x => x.MeanCostRealTerm);
            MeanCostMOD = MonthlyBuckets.Sum(x => x.MeanCostMOD);

            ShellShareCost = MonthlyBuckets.Sum(x => x.ShellShare);
        }

        public void GenerateAnnualyBucket(List<BizPlanPeriodBucket> MonthlyBuckets)
        {
            this.AnnualyBuckets = new List<BizPlanPeriodBucket>();

            foreach (var m in MonthlyBuckets.GroupBy(x => x.Key.Substring(0, 4)))
            {
                BizPlanPeriodBucket anBuc = new BizPlanPeriodBucket();
                anBuc.Key = m.Key;
                anBuc.Type = String.Join(",", m.ToList().Select(x => x.Type).Distinct().ToArray());
                DateTime mStart = m.ToList().Select(x => x.Period.Start).OrderBy(d => d.Date).FirstOrDefault();
                DateTime mFinish = m.ToList().Select(x => x.Period.Finish).OrderByDescending(d => d.Date).FirstOrDefault();
                anBuc.Period = new DateRange(mStart, mFinish);
                anBuc.MeanDays = m.ToList().Sum(x => x.MeanDays);
                anBuc.RigCost = m.ToList().Sum(x => x.RigCost);
                anBuc.MaterialCost = m.ToList().Sum(x => x.MaterialCost);
                anBuc.ServiceCost = m.ToList().Sum(x => x.ServiceCost);
                anBuc.TroubleFreeCost = m.ToList().Sum(x => x.TroubleFreeCost);
                anBuc.NPTCost = m.ToList().Sum(x => x.NPTCost);
                anBuc.TECOPCost = m.ToList().Sum(x => x.TECOPCost);
                anBuc.EscCostRig = m.ToList().Sum(x => x.EscCostRig);
                anBuc.EscCostServices = m.ToList().Sum(x => x.EscCostServices);
                anBuc.EscCostMaterial = m.ToList().Sum(x => x.EscCostMaterial);
                anBuc.EscCostTotal = m.ToList().Sum(x => x.EscCostTotal);
                anBuc.MeanCostEDM = m.ToList().Sum(x => x.MeanCostEDM);
                anBuc.CSOCost = m.ToList().Sum(x => x.CSOCost);
                anBuc.InflationCost = m.ToList().Sum(x => x.InflationCost);
                anBuc.MeanCostMOD = m.ToList().Sum(x => x.MeanCostMOD);
                anBuc.MeanCostRealTerm = m.ToList().Sum(x => x.MeanCostRealTerm);
                anBuc.ShellShare = m.ToList().Sum(x => x.ShellShare);

                anBuc.MeanCostEDMSS = m.ToList().Sum(x => x.MeanCostEDMSS);
                anBuc.MeanCostMODSS = m.ToList().Sum(x => x.MeanCostMODSS);
                anBuc.MeanCostRealTermSS = m.ToList().Sum(x => x.MeanCostRealTermSS);

                AnnualyBuckets.Add(anBuc);
            }
        }

        public void GenerateQuarterBucket(List<BizPlanPeriodBucket> MonthlyBuckets)
        {
            List<BizPlanPeriodBucket> monthtemp = new List<BizPlanPeriodBucket>();
            foreach (var m in MonthlyBuckets)
            {
                BizPlanPeriodBucket o = new BizPlanPeriodBucket();
                o.Key = m.Key;
                o.Type = m.Type; // String.Join(",", m.ToList().Select(x => x.Type).Distinct().ToArray());
                o.Period = m.Period;
                o.MeanDays = m.MeanDays;
                o.RigCost = m.RigCost;
                o.MaterialCost = m.MaterialCost;
                o.ServiceCost = m.ServiceCost;
                o.TroubleFreeCost = m.TroubleFreeCost;
                o.NPTCost = m.NPTCost;
                o.TECOPCost = m.TECOPCost;
                o.EscCostRig = m.EscCostRig;
                o.EscCostServices = m.EscCostServices;
                o.EscCostMaterial = m.EscCostMaterial;
                o.EscCostTotal = m.EscCostTotal;
                o.MeanCostEDM = m.MeanCostEDM;
                o.CSOCost = m.CSOCost;
                o.InflationCost = m.InflationCost;
                o.MeanCostMOD = m.MeanCostMOD;
                o.MeanCostRealTerm = m.MeanCostRealTerm;
                o.ShellShare = m.ShellShare;

                o.MeanCostEDMSS = m.MeanCostEDMSS;
                o.MeanCostMODSS = m.MeanCostMODSS;
                o.MeanCostRealTermSS = m.MeanCostRealTermSS;

                monthtemp.Add(o);
            }

            this.QuarterBuckets = new List<BizPlanPeriodBucket>();

            List<BizPlanPeriodBucket> quarter = new List<BizPlanPeriodBucket>();
            foreach (var year in monthtemp)
            {
                string keyCheck = year.Key.Substring(4, 2);
                if (keyCheck.Equals("01") || keyCheck.Equals("02") || keyCheck.Equals("03"))
                    // Q1
                    year.Key = year.Key.Substring(0, 4) + "01";
                else if (keyCheck.Equals("04") || keyCheck.Equals("05") || keyCheck.Equals("06"))
                    // Q2
                    year.Key = year.Key.Substring(0, 4) + "02";
                else if (keyCheck.Equals("07") || keyCheck.Equals("08") || keyCheck.Equals("09"))
                    // Q3
                    year.Key = year.Key.Substring(0, 4) + "03";
                else if (keyCheck.Equals("10") || keyCheck.Equals("11") || keyCheck.Equals("12"))
                    // Q4
                    year.Key = year.Key.Substring(0, 4) + "04";
            }
            List<BizPlanPeriodBucket> Qurter = new List<BizPlanPeriodBucket>();
            foreach (var m in monthtemp.GroupBy(x => x.Key.ToString()))
            {
                BizPlanPeriodBucket anBuc = new BizPlanPeriodBucket();
                anBuc.Key = m.Key;
                anBuc.Type = String.Join(",", m.ToList().Select(x => x.Type).Distinct().ToArray());
                DateTime mStart = m.ToList().Select(x => x.Period.Start).OrderBy(d => d.Date).FirstOrDefault();
                DateTime mFinish = m.ToList().Select(x => x.Period.Finish).OrderByDescending(d => d.Date).FirstOrDefault();
                anBuc.Period = new DateRange(mStart, mFinish);
                anBuc.MeanDays = m.ToList().Sum(x => x.MeanDays);
                anBuc.RigCost = m.ToList().Sum(x => x.RigCost);
                anBuc.MaterialCost = m.ToList().Sum(x => x.MaterialCost);
                anBuc.ServiceCost = m.ToList().Sum(x => x.ServiceCost);
                anBuc.TroubleFreeCost = m.ToList().Sum(x => x.TroubleFreeCost);
                anBuc.NPTCost = m.ToList().Sum(x => x.NPTCost);
                anBuc.TECOPCost = m.ToList().Sum(x => x.TECOPCost);
                anBuc.EscCostRig = m.ToList().Sum(x => x.EscCostRig);
                anBuc.EscCostServices = m.ToList().Sum(x => x.EscCostServices);
                anBuc.EscCostMaterial = m.ToList().Sum(x => x.EscCostMaterial);
                anBuc.EscCostTotal = m.ToList().Sum(x => x.EscCostTotal);
                anBuc.MeanCostEDM = m.ToList().Sum(x => x.MeanCostEDM);
                anBuc.CSOCost = m.ToList().Sum(x => x.CSOCost);
                anBuc.InflationCost = m.ToList().Sum(x => x.InflationCost);
                anBuc.MeanCostMOD = m.ToList().Sum(x => x.MeanCostMOD);
                anBuc.MeanCostRealTerm = m.ToList().Sum(x => x.MeanCostRealTerm);
                anBuc.ShellShare = m.ToList().Sum(x => x.ShellShare);

                anBuc.MeanCostEDMSS = m.ToList().Sum(x => x.MeanCostEDMSS);
                anBuc.MeanCostMODSS = m.ToList().Sum(x => x.MeanCostMODSS);
                anBuc.MeanCostRealTermSS = m.ToList().Sum(x => x.MeanCostRealTermSS);

                Qurter.Add(anBuc);
            }
            this.QuarterBuckets = Qurter;

        }

        public BizPlanSummary CalcTLA(double tlaTroubleDays, double tlaNptDay, double tlaMaterial, double tlaServices, double tlaTangible, double tlaSpreadRate)
        {
            double tfdPercen = tlaTroubleDays > 1 ? Tools.Div(tlaTroubleDays, 100) : tlaTroubleDays;
            TroubleFree.Days = TroubleFree.Days + (tfdPercen * TroubleFree.Days);

            double matPerc = tlaMaterial > 1 ? Tools.Div(tlaMaterial, 100) : tlaMaterial;
            Material = Material + (matPerc * Material);

            double sPerc = tlaServices > 1 ? Tools.Div(tlaServices, 100) : tlaServices;
            Services = Services + (sPerc * Services);

            double tlaTangiblePerc = tlaTangible > 1 ? Tools.Div(tlaTangible, 100) : tlaTangible;
            MaterialLongLeadValue.TangibleValue = MaterialLongLeadValue.TangibleValue + (tlaTangiblePerc * MaterialLongLeadValue.TangibleValue);

            // reverse NPT Days + TLA to get NPT Rate
            //   NPT.Days = Tools.Div(TroubleFree.Days * NPTRate, (1 - NPTRate));            
            double nptDays = tlaNptDay > 1 ? Tools.Div(tlaNptDay, 100) : tlaNptDay;
            nptDays = NPT.Days + (nptDays * NPT.Days);
            NPTRate = Tools.Div(nptDays, (TroubleFree.Days + nptDays));

            #region  reverse spread rate to get Rig Rate and RigRatePerDays
            var spreadCurrent = SpreadRateTotal;
            var tla = tlaSpreadRate;
            if (tla > 1)
                tlaSpreadRate = Tools.Div(tlaSpreadRate, 100);
            var updatedSpreadRate = spreadCurrent + (spreadCurrent * tlaSpreadRate);

            var mce = (updatedSpreadRate * MeanTime) + (Material + (Material * TECOPRateCost));
            var firstEq = mce - (mce * NPTRateCost) + (Material * NPTRateCost) + (TECOPRateCost * Material * NPTRateCost);
            var secondEq = (1 + TECOPRateCost + (TECOPRateCost * NPTRateCost));
            var troubleFreeCost = Tools.Div(firstEq, secondEq);
            var NPTCostNew = (troubleFreeCost - Material) * Tools.Div(NPTRateCost, 1 - NPTRateCost);
            var TECOPCostNew = TECOPRateCost * (troubleFreeCost - NPTCostNew);
            RigRate = Tools.Div(troubleFreeCost - Material - Services, TroubleFree.Days);
            RigRatePerDays = new Dictionary<DateTime, double>();
            for (DateTime i = EventPeriod.Start; i <= FinishOpsDate; i = i.AddDays(1))
            {
                RigRatePerDays.Add(i, RigRate);
            }
            #endregion

            var result = this.ReCalcForTla(
                this.WellName, this.RigName, this.ActivityType, this.Country, this.ShellShare, this.EventPeriod, this.MaturityLevel,
                this.Services,
                this.Material,
                this.TroubleFree.Days,
                this.RefFactorModel,
                NPTRate, this.TECOPRate,
                this.NPTRateCost,
                this.TECOPRateCost,
                this.MaterialLongLeadValue.MonthRequiredValue,
                this.MaterialLongLeadValue.TangibleValue,
                RigRate,
                RigRatePerDays
                );

            return result;
        }

        public BizPlanSummary ReCalcForTla(string wellName,
            string rigName,
            string activitytype,
            string country,
            double shellshare,
            DateRange eventperiod,
            int maturitylevel,
            double services,
            double material,
            double troublefreetime,
            string reffactormodel,
            double nptrate,
            double tecoprate,
            double nptratecost,
            double tecopratecost,
            double monthrequired,
            double tangiblevalue,
            double rigRate,
            Dictionary<DateTime, double> rigRatePerDays,
            string baseOP = "OP15"
            )
        {

            MonthlyBuckets = new List<BizPlanPeriodBucket>();
            AnnualyBuckets = new List<BizPlanPeriodBucket>();
            QuarterBuckets = new List<BizPlanPeriodBucket>();
            EventPeriod = new DateRange();
            NPT = new WellDrillData();
            TECOP = new WellDrillData();
            TroubleFree = new WellDrillData();
            ExchangeRates = new Dictionary<int, double>();
            RefFactorModels = new WEISReferenceFactorModel();

            ShellShare = shellshare > 1 ? Tools.Div(shellshare, 100) : shellshare;
            WellName = wellName;
            RigName = rigName;
            ActivityType = activitytype;
            EventPeriod = eventperiod;
            MaturityLevel = maturitylevel;
            Country = country;
            TroubleFree.Days = troublefreetime;
            RefFactorModel = reffactormodel;
            Material = material;
            Services = services;
            BaseOP = baseOP;

            NPTRate = nptrate;
            NPTRateCost = nptratecost;
            TECOPRateCost = tecopratecost;
            TECOPRate = tecoprate;

            // Exchange Rate
            for (int i = EventPeriod.Start.Year; i <= EventPeriod.Finish.Year; i++)
            {
                ExchangeRates.Add(i, BizPlanAllocation.GetExchangeRate(Country, i, baseOP));
            }

            // Refference Factor Model
            if (reffactormodel == null)
            {
                reffactormodel = "";
            }

            var qsrfm = new List<IMongoQuery>();
            qsrfm.Add(Query.EQ("GroupCase", reffactormodel));
            qsrfm.Add(Query.EQ("BaseOP", baseOP));

            RefFactorModels = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(Query.And(qsrfm));


            #region Material Long Lead
            MaterialLongLeadValue = new FYLongLead();
            MaterialLongLeadValue.MonthRequiredValue = monthrequired;
            MaterialLongLeadValue.Year = eventperiod.Start.Year;
            MaterialLongLeadValue.TangibleValue = tangiblevalue;
            #endregion

            LLI = Material * MaterialLongLeadValue.TangibleValue;

            var baseTime = troublefreetime + Tools.Div((troublefreetime * NPTRate), (1 - NPTRate));
            FinishOpsDate = EventPeriod.Start.AddDays(troublefreetime - 1);

            // RigRate
            RigRatePerDays = new Dictionary<DateTime, double>();
            double rRate = 0;
            RigRatePerDays = GetRigRate(out rRate);
            RigRate = rRate;

            NPT.Days = Tools.Div(TroubleFree.Days * NPTRate, (1 - NPTRate));
            TroubleFree.Cost = (TroubleFree.Days * RigRate) + Services + Material;
            NPT.Cost = (TroubleFree.Cost - Material) * Tools.Div(NPTRateCost, (1 - NPTRateCost));
            TECOP.Days = Tools.Div(TECOPRate * (TroubleFree.Days + NPT.Days) * (TroubleFree.Cost + NPT.Cost - Material), (TroubleFree.Cost + NPT.Cost));
            TECOP.Cost = TECOPRateCost * (TroubleFree.Cost + NPT.Cost);
            MeanCostEDM = TroubleFree.Cost + NPT.Cost + TECOP.Cost;

            MeanTime = NPT.Days + TECOP.Days + TroubleFree.Days;
            FinishActiveDate = EventPeriod.Start.AddDays(MeanTime - 1);

            BurnRate = Tools.Div(MeanCostEDM, MeanTime);
            SpreadRateWRig = Tools.Div((1 + TECOPRateCost) * (Services + (Services * (Tools.Div(NPTRateCost, (1 - NPTRateCost))))), MeanTime);
            SpreadRateTotal = Tools.Div((MeanCostEDM - (Material + (Material * TECOPRateCost))), MeanTime);
            #region Calc Lead Period
            var LeadDate = EventPeriod.Start.AddMonths(-1 * (Convert.ToInt32(monthrequired)));
            DateTime materialLeadDate = new DateTime();

            if (DateTime.Now.Date >= EventPeriod.Start.Date)
            {
                EventPeriod.Start = DateTime.Now.Date;
                materialLeadDate = DateTime.Now.Date;
            }
            else
            {
                if (LeadDate.Date > DateTime.Now.Date)
                {
                    // hitung full leadDate 
                    materialLeadDate = LeadDate.Date;
                }
                else
                {
                    LeadDate = DateTime.Now.Date;
                    materialLeadDate = LeadDate.Date;
                }
            }
            MaterialLongLeadPeriod = new DateRange(materialLeadDate, materialLeadDate);
            #endregion

            TroubleFreeCostUSD = ExchangeRates.Where(x => x.Key == eventperiod.Start.Year).Any() ? ExchangeRates.FirstOrDefault().Value * TroubleFree.Cost : 0;
            NPTCostUSD = ExchangeRates.Where(x => x.Key == eventperiod.Start.Year).Any() ? ExchangeRates.FirstOrDefault().Value * NPT.Cost : 0;
            TECOPCostUSD = ExchangeRates.Where(x => x.Key == eventperiod.Start.Year).Any() ? ExchangeRates.FirstOrDefault().Value * TECOP.Cost : 0;
            MeanCostEDMUSD = ExchangeRates.Where(x => x.Key == eventperiod.Start.Year).Any() ? ExchangeRates.FirstOrDefault().Value * MeanCostEDM : 0;

            this.GeneratePeriodBucket();
            this.GenerateAnnualyBucket(this.MonthlyBuckets);
            this.GenerateQuarterBucket(this.MonthlyBuckets);

            return this;
        }
    }
}
