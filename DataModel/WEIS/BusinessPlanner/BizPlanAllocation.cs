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
using ECIS.Biz.Common;

namespace ECIS.Client.WEIS
{
    [BsonIgnoreExtraElements]
    public class BizPlanAllocation : ECIS.Core.ECISModel
    {
        public override string TableName
        {
            get { return "WEISBizPlanAllocation"; }
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            this._id = String.Format("W{0}A{1}R{2}O{3}S{4}", WellName, ActivityType, RigName, SaveToOP,UARigSequenceId);
            doc = this.ToBsonDocument();
            return doc;
        }

        public string WellName { get; set; }
        public string ActivityType { get; set; }
        public string RigName { get; set; }
        public string ProjectName { get; set; }
        public string SaveToOP { get; set; }
        public string UARigSequenceId { get; set; }
        public DateRange EstimatePeriod { get; set; }


        public double WorkingInterest { get; set; }


        public List<BizPlanAllocationDetail> Annualy { get; set; }
        public List<BizPlanPeriodBucket> AnnualyBuckets { get; set; }
        public List<BizPlanAllocationDetail> Monthly { get; set; }
        public List<BizPlanPeriodBucket> MonthlyBuckets { get; set; }
        public List<BizPlanAllocationDetail> Quarterly { get; set; }
        public List<BizPlanPeriodBucket> QuarterlyBuckets { get; set; }

        public BizPlanAllocation()
        {
            Annualy = new List<BizPlanAllocationDetail>();
            AnnualyBuckets = new List<BizPlanPeriodBucket>();
            Monthly = new List<BizPlanAllocationDetail>();
            MonthlyBuckets = new List<BizPlanPeriodBucket>();
            Quarterly = new List<BizPlanAllocationDetail>();
            QuarterlyBuckets = new List<BizPlanPeriodBucket>();
        }



        public static void SaveBizPlanAllocation(
            string wellname,
            string activity,
            string rigname,
            string projectname,
            DateRange estimateperiod,
            string savetoOP,
            List<BizPlanPeriodBucket> annual,
            List<BizPlanPeriodBucket> monthly,
            List<BizPlanPeriodBucket> quarter,
            double workingInterest
            )
        {
            BizPlanAllocation a = new BizPlanAllocation();
            a.ActivityType = activity;
            a.WellName = wellname;
            a.RigName = rigname;
            a.ProjectName = projectname;
            a.EstimatePeriod = estimateperiod;
            a.SaveToOP = savetoOP;

            a.WorkingInterest = workingInterest;

            a.AnnualyBuckets = annual;
            a.MonthlyBuckets = monthly;
            a.QuarterlyBuckets = quarter;

            a.Save();
        }

        public static void SaveBizPlanAllocation(BizPlanActivity activity)
        {
            foreach (var p in activity.Phases)
            {
                if (p.PushToWellPlan)
                {

                    BizPlanAllocation a = new BizPlanAllocation();
                    a.ActivityType = p.ActivityType;
                    a.WellName = activity.WellName;
                    a.RigName = p.Estimate.RigName;
                    a.ProjectName = activity.ProjectName;
                    a.EstimatePeriod = p.Estimate.EstimatePeriod;
                    a.SaveToOP = p.Estimate.SaveToOP;
                    a.UARigSequenceId = activity.UARigSequenceId;
                    
                    a.WorkingInterest = activity.WorkingInterest;

                    var maturityLevel = p.Estimate.MaturityLevel.Substring(p.Estimate.MaturityLevel.Length - 1, 1);
                    //var calc = BizPlanCalculation.calcBizPlan(activity.WellName, p.Estimate.RigName, p.ActivityType,
                    //    activity.Country, activity.ShellShare, p.Estimate.EstimatePeriod, Convert.ToInt32(maturityLevel), p.Estimate.Services,
                    //    p.Estimate.Materials, p.Estimate.NewTroubleFree.Days, activity.ReferenceFactorModel,
                    //    Tools.Div(p.Estimate.NewNPTTime.PercentDays, 100),
                    //    Tools.Div(p.Estimate.NewTECOPTime.PercentDays, 100), Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100),
                    //    Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100), p.Estimate.LongLeadMonthRequired, Tools.Div(p.Estimate.PercOfMaterialsLongLead, 100),
                    //    p.Estimate.SpreadRateTotal);
                    BizPlanSummary calc = new BizPlanSummary(activity.WellName, p.Estimate.RigName, p.ActivityType,
                        activity.Country, activity.ShellShare, p.Estimate.EstimatePeriod, Convert.ToInt32(maturityLevel),
                        p.Estimate.Services, p.Estimate.Materials, p.Estimate.TroubleFreeBeforeLC.Days, activity.ReferenceFactorModel,
                        Tools.Div(p.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentDays, 100),
                        Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100),
                        p.Estimate.LongLeadMonthRequired, Tools.Div(p.Estimate.PercOfMaterialsLongLead, 100), baseOP: p.Estimate.SaveToOP, lcf:p.Estimate.NewLearningCurveFactor);
                    calc.GeneratePeriodBucket();
                    calc.GenerateAnnualyBucket(calc.MonthlyBuckets);
                    calc.GenerateQuarterBucket(calc.MonthlyBuckets);

                    a.AnnualyBuckets = calc.AnnualyBuckets;
                    a.MonthlyBuckets = calc.MonthlyBuckets;
                    a.QuarterlyBuckets = calc.QuarterBuckets;

                    a.Save();

                    foreach(var pxv in activity.Phases.Where(x=>x.ActivityType.Equals(p.ActivityType)))
                    {
                        pxv.Allocation = a;
                    }



                    #region save to PMaster Maps
                        var qsPMaster = new List<IMongoQuery>();
                        qsPMaster.Add(Query.EQ("WellName", activity.WellName));
                        qsPMaster.Add(Query.EQ("RigName", p.Estimate.RigName));
                        qsPMaster.Add(Query.EQ("UARigSequenceId", activity.UARigSequenceId));
                        qsPMaster.Add(Query.EQ("ActivityType", p.ActivityType));
                        var qPMaster = Query.And(qsPMaster);

                        //save to PMaster
                        var pMasterMonths = PMaster.Populate<PMaster>(qPMaster);
                        foreach (var pMasterMonthly in pMasterMonths){
                            if (pMasterMonthly != null)
                            {
                                pMasterMonthly.Allocation = a;
                                pMasterMonthly.IsInPlan = activity.isInPlan;
                                pMasterMonthly.FirmOption = activity.isInPlan ? "In Plan" : "Not In Plan";
                                DataHelper.Save(pMasterMonthly.TableName, pMasterMonthly.ToBsonDocument());
                            }
                        }

                        //save to PMasterStandard
                        var pMasterStandards = PMasterStandard.Populate<PMasterStandard>(qPMaster);
                        foreach (var pMasterStandard in pMasterStandards)
                        {
                            if (pMasterStandard != null)
                            {
                                pMasterStandard.Allocation = a;
                                pMasterStandard.IsInPlan = activity.isInPlan;
                                pMasterStandard.FirmOption = activity.isInPlan ? "In Plan" : "Not In Plan";
                                DataHelper.Save(pMasterStandard.TableName, pMasterStandard.ToBsonDocument());
                            }
                        }
                    #endregion
                }


            }

            DataHelper.Save(activity.TableName, activity.ToBsonDocument());
        }


        public static Dictionary<int, double> CalcMaterialEscalation(BizPlanActivityPhase p,
            WEISReferenceFactorModel refFactor,
            string country = "",
            string type = "annualy"
            )
        {
            if (type.Equals("annualy"))
            {
                #region annualy
                DateTime eventStartDate = p.Estimate.EventStartDate;
                var t = LongLead.GetLongLead(p.ActivityType, eventStartDate.Year);

                double monthLongLead = t == null ? 0 : t.MonthRequiredValue;
                double tabngible = t == null ? 0 : Tools.Div(t.TangibleValue, 100);

                DateTime startEscMaterial = p.Estimate.EventStartDate.AddMonths(Convert.ToInt32((-1) * monthLongLead));

                // menentukan bulan mundur 

                if (startEscMaterial.Date <= DateTime.Now.Date)
                    startEscMaterial = DateTime.Now.Date;


                double MaterialsUSDCur = p.Estimate.MaterialsUSD;

                int finishYear = eventStartDate.Year;
                double totCurMatEscl = tabngible * MaterialsUSDCur;

                double matEsclActualYear = MaterialsUSDCur - totCurMatEscl;

                if (startEscMaterial > eventStartDate)
                    return null;

                DateRange dr = new DateRange(startEscMaterial, eventStartDate);

                var perYear = DateRangeToMonth.NumDaysPerYear(dr);

                //WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>(Query.EQ())

                Dictionary<int, double> propMatEscalPerYear = new Dictionary<int, double>();

                var totalDays = perYear.Sum(x => x.Value);

                foreach (var px in perYear)
                {
                    var servcEscalatMat = refFactor.SubjectMatters.Where(x => x.Key.Equals("Material Escalation Factors"));
                    if (servcEscalatMat != null && servcEscalatMat.Count() > 0)
                    {
                        var escValMat = servcEscalatMat.FirstOrDefault();//.Where(x => x.Key == a.Key);

                        var yearValue = escValMat.Value.ToList().Where(x => x.Key.Equals("Year_" + px.Key));
                        if (yearValue != null && yearValue.Count() > 0)
                        {
                            double result = 0;

                            double prop = px.Value / totalDays;
                            var firstVal = prop * totCurMatEscl;

                            var matEscal = 0;

                            // get Matrial Escalation
                            var matEscVal = refFactor.SubjectMatters["Material Escalation Factors"]["Year_" + px.Key.ToString()];
                            if (matEscVal != null)
                            {
                                result = firstVal + (firstVal * (matEscVal / 100));
                                //annuall.Where(x=>x)
                            }
                            else
                                result = firstVal;
                            propMatEscalPerYear.Add(px.Key, result);

                        }
                    }
                }

                if (propMatEscalPerYear != null && propMatEscalPerYear.Count() > 0)
                {
                    var maxKey = propMatEscalPerYear.Select(x => Convert.ToInt32(x.Key)).Max();

                    if (propMatEscalPerYear.Select(x => Convert.ToInt32(x.Key)).Max() == finishYear)
                    {
                        propMatEscalPerYear[finishYear] = propMatEscalPerYear[finishYear] + matEsclActualYear;
                    }
                    else if (propMatEscalPerYear.Select(x => Convert.ToInt32(x.Key)).Max() < finishYear)
                    {
                        propMatEscalPerYear.Add(finishYear, matEsclActualYear);
                    }
                    return propMatEscalPerYear;
                }
                else
                    return null;
                #endregion
            }
            else if (type.Equals("monthly"))
            {
                #region month
                DateTime eventStartDate = p.Estimate.EventStartDate;
                var t = LongLead.GetLongLead(p.ActivityType, eventStartDate.Year);

                double monthLongLead = t == null ? 0 : t.MonthRequiredValue;
                double tabngible = t == null ? 0 : Tools.Div(t.TangibleValue, 100);

                DateTime startEscMaterial = p.Estimate.EventStartDate.AddMonths(Convert.ToInt32((-1) * monthLongLead));

                // menentukan bulan mundur 

                if (startEscMaterial.Date <= DateTime.Now.Date)
                    startEscMaterial = DateTime.Now.Date;


                double MaterialsUSDCur = p.Estimate.MaterialsUSD;

                int finishMonth = Convert.ToInt32(eventStartDate.ToString("yyyyMM"));
                // tangible di tahun yg bersangkutan
                double totCurMatEscl = tabngible * MaterialsUSDCur;

                double matEsclActualYear = MaterialsUSDCur - totCurMatEscl;

                if (startEscMaterial > eventStartDate)
                    return null;

                DateRange dr = new DateRange(startEscMaterial, eventStartDate);

                DateRange outDr = new DateRange();

                var perMonth = DateRangeToMonth.NumDaysPerMonth(dr);

                //WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>(Query.EQ())

                Dictionary<int, double> propMatEscalPerYear = new Dictionary<int, double>();

                var totalDays = perMonth.SelectMany(y => y.Value).ToList().Sum(x => x.Value);

                foreach (var pm in perMonth)
                {
                    var servcEscalatMat = refFactor.SubjectMatters.Where(x => x.Key.Equals("Material Escalation Factors"));
                    if (servcEscalatMat != null && servcEscalatMat.Count() > 0)
                    {
                        var escValMat = servcEscalatMat.FirstOrDefault();//.Where(x => x.Key == a.Key);

                        var yearValue = escValMat.Value.ToList().Where(x => x.Key.Equals("Year_" + pm.Key.ToString().Substring(0, 4)));
                        if (yearValue != null && yearValue.Count() > 0)
                        {
                            double result = 0;

                            double val = pm.Value.Sum(x => x.Value);//.SelectMany(x=>x.Value).ToList().Sum();

                            double prop = Tools.Div(val, totalDays);
                            var firstVal = prop * totCurMatEscl;

                            var matEscal = 0;

                            // get Matrial Escalation
                            var matEscVal = refFactor.SubjectMatters["Material Escalation Factors"]["Year_" + pm.Key.ToString().Substring(0, 4)];
                            if (matEscVal != null)
                            {
                                result = firstVal + (firstVal * (matEscVal / 100));
                                //annuall.Where(x=>x)
                            }
                            else
                                result = firstVal;
                            propMatEscalPerYear.Add(pm.Key, result);

                        }
                    }
                }

                if (propMatEscalPerYear != null && propMatEscalPerYear.Count() > 0)
                {
                    var maxKey = propMatEscalPerYear.Select(x => Convert.ToInt32(x.Key)).Max();

                    if (propMatEscalPerYear.Select(x => Convert.ToInt32(x.Key)).Max() == finishMonth)
                    {
                        propMatEscalPerYear[finishMonth] = propMatEscalPerYear[finishMonth] + matEsclActualYear;
                    }
                    else if (propMatEscalPerYear.Select(x => Convert.ToInt32(x.Key)).Max() < finishMonth)
                    {
                        propMatEscalPerYear.Add(finishMonth, matEsclActualYear);
                    }
                    return propMatEscalPerYear;
                }
                else
                    return null;
                #endregion
            }
            else if (type.Equals("quarter"))
            {
                #region quarter
                DateTime eventStartDate = p.Estimate.EventStartDate;
                var t = LongLead.GetLongLead(p.ActivityType, eventStartDate.Year);

                double monthLongLead = t == null ? 0 : t.MonthRequiredValue;
                double tabngible = t == null ? 0 : Tools.Div(t.TangibleValue, 100);

                DateTime startEscMaterial = p.Estimate.EventStartDate.AddMonths(Convert.ToInt32((-1) * monthLongLead));

                // menentukan bulan mundur 

                if (startEscMaterial.Date <= DateTime.Now.Date)
                    startEscMaterial = DateTime.Now.Date;


                double MaterialsUSDCur = p.Estimate.MaterialsUSD;

                DateIsland d = new DateIsland(eventStartDate);
                int finishQuarter = Convert.ToInt32(d.QtrId);
                double totCurMatEscl = tabngible * MaterialsUSDCur;

                double matEsclActualYear = MaterialsUSDCur - totCurMatEscl;

                if (startEscMaterial > eventStartDate)
                    return null;

                DateRange dr = new DateRange(startEscMaterial, eventStartDate);

                var perQuarter = DateRangeToMonth.NumDaysPerQuarter(dr);

                //WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>(Query.EQ())

                Dictionary<int, double> propMatEscalPerYear = new Dictionary<int, double>();

                var totalDays = perQuarter.Sum(x => x.Value);

                foreach (var pq in perQuarter)
                {
                    var servcEscalatMat = refFactor.SubjectMatters.Where(x => x.Key.Equals("Material Escalation Factors"));
                    if (servcEscalatMat != null && servcEscalatMat.Count() > 0)
                    {
                        var escValMat = servcEscalatMat.FirstOrDefault();//.Where(x => x.Key == a.Key);

                        var yearValue = escValMat.Value.ToList().Where(x => x.Key.Equals("Year_" + pq.Key.ToString().Substring(0, 4)));
                        if (yearValue != null && yearValue.Count() > 0)
                        {
                            double result = 0;

                            double prop = pq.Value / totalDays;
                            var firstVal = prop * totCurMatEscl;

                            var matEscal = 0;

                            // get Matrial Escalation
                            var matEscVal = refFactor.SubjectMatters["Material Escalation Factors"]["Year_" + pq.Key.ToString().Substring(0, 4)];
                            if (matEscVal != null)
                            {
                                result = firstVal + (firstVal * (matEscVal / 100));
                                //annuall.Where(x=>x)
                            }
                            else
                                result = firstVal;
                            propMatEscalPerYear.Add(pq.Key, result);

                        }
                    }
                }

                if (propMatEscalPerYear != null && propMatEscalPerYear.Count() > 0)
                {
                    var maxKey = propMatEscalPerYear.Select(x => Convert.ToInt32(x.Key)).Max();

                    if (propMatEscalPerYear.Select(x => Convert.ToInt32(x.Key)).Max() == finishQuarter)
                    {
                        propMatEscalPerYear[finishQuarter] = propMatEscalPerYear[finishQuarter] + matEsclActualYear;
                    }
                    else if (propMatEscalPerYear.Select(x => Convert.ToInt32(x.Key)).Max() < finishQuarter)
                    {
                        propMatEscalPerYear.Add(finishQuarter, matEsclActualYear);
                    }
                    return propMatEscalPerYear;
                }
                else
                    return null;
                #endregion

            }
            else
                return null;


        }

        public static DateTime GetMaterialShiftDate(string activityType, DateTime eventStart, out double percenttangible, out double monthlonglead)
        {

            var t = LongLead.GetLongLead(activityType, eventStart.Year);

            double monthLongLead = t == null ? 0 : t.MonthRequiredValue;
            double tabngible = t == null ? 0 : Tools.Div(t.TangibleValue, 100);

            percenttangible = tabngible;
            monthlonglead = monthLongLead;

            DateTime startEscMaterial = eventStart.AddMonths(Convert.ToInt32((-1) * monthLongLead));

            return startEscMaterial;
        }

        public static double GetRefferenceFactorModel(string country,
            string groupCase, int year, string subject = "Service Escalation Factors"
            )
        {
            //List<WEISReferenceFactorModel> facmods = new List<WEISReferenceFactorModel>();
            string cty = "*";
            if (country.Trim().Equals("") || country.Trim().ToLower().Equals("world"))
                cty = "*";
            else
                cty = country;

            string rff = "*";
            if (groupCase == null)
                rff = "*";
            else if (groupCase.Trim().Equals(""))
                rff = "*";
            else
                rff = groupCase;

            IMongoQuery q = Query.And(
                Query.EQ("GroupCase", rff),
                Query.EQ("Country", cty),
                Query.EQ("WellName", "*"),
                Query.EQ("ActivityType", "*")
                );

            var facMod = WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>(q);

            WEISReferenceFactorModel refFacMod = new WEISReferenceFactorModel();
            if (facMod != null && facMod.Count() > 0)
            {
                refFacMod = facMod.FirstOrDefault();
            }

            double percEscServi = 0;
            var servcEscalat = refFacMod.SubjectMatters.Where(x => x.Key.Equals(subject));
            if (servcEscalat.Any())
            {
                var escVal = servcEscalat.FirstOrDefault();//.Where(x => x.Key == a.Key);

                var yearValue = escVal.Value.ToList().Where(x => x.Key.Equals("Year_" + year.ToString()));
                if (yearValue != null && yearValue.Count() > 0)
                {
                    percEscServi = yearValue.FirstOrDefault().Value;
                    percEscServi = Tools.Div(percEscServi, 100);
                }
            }
            return percEscServi;
        }

        public static double GetRigEscalation(string rigName)
        {
            double rigEscal = 0;
            var rigEscn = RigEscalation.Populate<RigEscalation>();
            if (rigEscn.Any())
            {
                var availRigscals = rigEscn.Where(x => x.RigName.Equals(rigName));
                if (availRigscals.Any())
                {
                    rigEscal = availRigscals.FirstOrDefault().Value;
                    rigEscal = Tools.Div(rigEscal, 100);
                }
            }
            return rigEscal;
        }


        public static double GetRigRate(string rigName, int year)
        {

            double result = 0;
            var rigRate = RigRates.Get<RigRates>(Query.EQ("Title", rigName));
            if (rigRate != null)
            {
                var xYear = rigRate.Value.Where(x => x.Year == year);
                if (xYear.Any())
                {
                    result = xYear.FirstOrDefault().Value;
                }
            }

            return result;
        }

        public static double GetExchangeRate(string country, int year, string baseop)
        {
            double excRate = 1;


            var data = MacroEconomic.Get<MacroEconomic>(
               Query.And(
               Query.EQ("Country", country),
               Query.EQ("BaseOP", baseop)

               )
               );

            if (data != null)
            {
                if (data.ExchangeRate.AnnualValues.Any())
                {
                    var ada = data.ExchangeRate.AnnualValues.Where(x => x.Year == year);
                    if (ada.Any())
                    {
                        excRate = ada.FirstOrDefault().Value;
                        return excRate; ;
                    }
                }
            }
            return excRate;
        }


        public static double GetExchangeRateByCurrency(string currency, int year, string baseop)
        {
            double excRate = 1;
            var data = MacroEconomic.Get<MacroEconomic>(
                Query.And(
                Query.EQ("Currency", currency),
                Query.EQ("BaseOP", baseop)

                )
                );
            if (data != null)
            {
                if (data.ExchangeRate.AnnualValues.Any())
                {
                    var ada = data.ExchangeRate.AnnualValues.Where(x => x.Year == year);
                    if (ada.Any())
                    {
                        excRate = ada.FirstOrDefault().Value;
                        return excRate; ;
                    }
                }
            }
            return excRate;
        }

        public static BizPlanAllocationDetail CalcBizPlanDetail(BizPlanActivityPhase p, int periodId,
            int noOfDays,
            int totalDays,
            int shiftDays,
            int totalDayActive,
            double rigEscal,
            string country,
            string groupCase,
            double shellShare,
            bool isMaterial = true

            )
        {
            BizPlanAllocationDetail a = new BizPlanAllocationDetail();
            a.Key = periodId.ToString();
            a.NoOfDays = noOfDays;
            a.Proportion = Tools.Div(a.NoOfDays, totalDays);


            var t = LongLead.GetLongLead(p.ActivityType, p.Estimate.EventStartDate.Year);
            double monthLongLead = t == null ? 0 : t.MonthRequiredValue;
            double tabngible = t == null ? 0 : Tools.Div(t.TangibleValue, 100);

            double materialUSD = p.Estimate.MaterialsUSD;
            // tangible di tahun yg bersangkutan
            double materialShift = tabngible * materialUSD;
            double materialActual = materialUSD - materialShift;

            #region RigCost
            if (!isMaterial)
            {
                var rigCost = p.Estimate.RigRatesUSD;
                a.RigCost = Tools.Div(Convert.ToDouble(noOfDays), Convert.ToDouble(totalDayActive)) * rigCost;
            }
            #endregion

            #region Service Cost
            if (!isMaterial)
            {
                var ss = p.Estimate.ServicesUSD;
                a.ServiceCost = Tools.Div(Convert.ToDouble(noOfDays), Convert.ToDouble(totalDayActive)) * ss;
            }
            #endregion

            #region Material Cost
            var matcos = p.Estimate.MaterialsUSD;
            var perc = p.Estimate.PercOfMaterialsLongLead;
            var matcosForShiftPeriod = matcos * Tools.Div(perc, 100);
            var matcostForActivePeriod = matcos - matcosForShiftPeriod;

            if (!isMaterial)
                a.MaterialCost = Tools.Div(Convert.ToDouble(noOfDays), Convert.ToDouble(totalDayActive)) * matcostForActivePeriod;
            else
                a.MaterialCost = Tools.Div(Convert.ToDouble(noOfDays), Convert.ToDouble(shiftDays)) * matcosForShiftPeriod;

            #endregion


            #region Trobule Free Cost
            a.TroubleFreeCost = a.RigCost + a.ServiceCost + a.MaterialCost;
            #endregion

            #region 3 - CalcMaterial
            //EscalationCostEDMMaterial 
            double matCostSplit = Tools.Div(Convert.ToDouble(noOfDays), Convert.ToDouble(shiftDays)) * materialShift; // CalcMaterialEscalation(p, refFacMod, type: "monthly");
            var matescalPercent = GetRefferenceFactorModel(country, groupCase, Convert.ToInt32(periodId.ToString().Substring(0, 4)), "Material Escalation Factors");
            matCostSplit = matCostSplit + (matCostSplit * matescalPercent);

            a.EscalationCostEDMMaterial = matCostSplit; // a.NoOfDays * percEscMaterial * p.Estimate.MaterialsUSD;

            #endregion



            // 1
            //EscalationCostEDMRig 
            if (!isMaterial)
                a.EscalationCostEDMRig = a.NoOfDays * p.Estimate.RigRatesUSD * (rigEscal - 1);

            //// 2
            //EscalationCostEDMServices 
            if (!isMaterial)
            {
                var percEscServi = GetRefferenceFactorModel(country, groupCase, Convert.ToInt32(periodId.ToString().Substring(0, 4)), "Service Escalation Factors");
                a.EscalationCostEDMServices = a.NoOfDays * percEscServi * p.Estimate.ServicesUSD;
            }

            //// 4
            //EscalationCostEDMTotal 
            a.EscalationCostEDMTotal = a.EscalationCostEDMRig + a.EscalationCostEDMMaterial + a.EscalationCostEDMServices;



            //// 7
            //MeanCostEDM 
            //if (!isMaterial)
            //{
            var nptpercent = Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100);
            var tecoppercent = Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100);

            var meancostTemp = (nptpercent * a.EscalationCostEDMTotal) +
                (a.EscalationCostEDMTotal) +
                (a.EscalationCostEDMTotal * tecoppercent);
            //}

            a.MeanCostEDM = meancostTemp;


            //var MeanCostEDMUSD = p.Estimate.MeanUSD;
            var MeanCostEDMUSD = a.MeanCostEDM;


            //// 5
            //CSOCostEDM 
            double cso = GetRefferenceFactorModel(country, groupCase, Convert.ToInt32(periodId.ToString().Substring(0, 4)), "CSO Factors");
            a.CSOCostEDM = (cso * (MeanCostEDMUSD + a.EscalationCostEDMTotal));//(((a.Prop ortion * p.Estimate.NewMean.Days * p.Estimate.MeanUSD) + a.EscalationCostEDMTotal) * CSOFactor);

            //// 6
            //InflationCostEDM 
            double inflation = GetRefferenceFactorModel(country, groupCase, Convert.ToInt32(periodId.ToString().Substring(0, 4)), "Inflation Factors");
            a.InflationCostEDM = (inflation) * (MeanCostEDMUSD + a.EscalationCostEDMTotal + a.CSOCostEDM);



            //// 8
            //MeanCostRealTerm 
            a.MeanCostRealTerm = (MeanCostEDMUSD + a.EscalationCostEDMTotal);

            //// 9
            //MeanCostMOD 
            a.MeanCostMOD = (a.MeanCostRealTerm + a.CSOCostEDM + a.InflationCostEDM);

            //// 10
            //ShellShareMOD 
            a.ShellShareMOD = (Tools.Div(shellShare, 100) * a.MeanCostMOD);

            return a;
        }

        public static List<BizPlanAllocationDetail> CalculateBizPlanAllocation(BizPlanActivityPhase p, string country, string groupCase, double shellShare)
        {
            double percentTangible = 0;
            double monthlonglead = 0;
            DateTime eventStartDate = p.Estimate.EventStartDate;
            DateTime eventEndDate = p.Estimate.EventEndDate;
            //DateTime materialShiftDate = GetMaterialShiftDate(p.ActivityType, eventStartDate, out percentTangible, out monthlonglead);
            DateTime materialShiftDate = p.Estimate.StartEscDateMaterial;
            var rigEscal = GetRigEscalation(p.Estimate.RigName);

            if (materialShiftDate <= DateTime.Now.Date)
                materialShiftDate = DateTime.Now.Date;

            if (eventStartDate <= DateTime.Now)
                eventStartDate = DateTime.Now;

            DateRange fullMonthPeriod = new DateRange(materialShiftDate, eventEndDate);

            // cross dalam 1 bulan (shift dan material)


            #region monthly

            var fullMonths = DateRangeToMonth.NumDaysPerMonth(fullMonthPeriod);
            var totalDaysFull = fullMonthPeriod.Days + 1;
            var totalDaysShift = new DateRange(materialShiftDate, eventStartDate).Days + 1; // fullMonthPeriod.Days + 1;
            //var totalDaysActual = new DateRange(p.Estimate.EventStartDate, p.Estimate.EventEndDate).Days + 1;
            var totalDaysActual = new DateRange(eventStartDate, p.Estimate.EventEndDate).Days + 1;

            List<BizPlanAllocationDetail> details = new List<BizPlanAllocationDetail>();
            foreach (var u in fullMonths)
            {
                if (u.Key < Convert.ToInt32(eventStartDate.ToString("yyyyMM")))
                {
                    // hitung material saja
                    var res = CalcBizPlanDetail(p, u.Key, Convert.ToInt32(u.Value.FirstOrDefault().Value), Convert.ToInt32(totalDaysFull), Convert.ToInt32(totalDaysShift),
                        Convert.ToInt32(totalDaysActual), rigEscal, country, groupCase, shellShare, true);
                    res.isMaterialAdd = true;
                    res.Period = u.Value.Keys.FirstOrDefault();
                    details.Add(res);
                }
                else if (u.Key == Convert.ToInt32(eventStartDate.ToString("yyyyMM")))
                {
                    // hitung pembagiannya
                    int month = Convert.ToInt32(u.Key.ToString().Substring(4, 2));
                    int year = Convert.ToInt32(u.Key.ToString().Substring(0, 4));

                    if (materialShiftDate.Date < eventStartDate.Date)
                    {

                        DateTime startThisMonthMaterial = new DateTime(year, month, 1);
                        DateTime finishThisMonthMaterial = eventStartDate.AddDays(-1);
                        double noDaysInsideFirst = new DateRange(startThisMonthMaterial, finishThisMonthMaterial).Days + 1;
                        var res = CalcBizPlanDetail(p, u.Key, Convert.ToInt32(noDaysInsideFirst), Convert.ToInt32(totalDaysFull), Convert.ToInt32(totalDaysShift), Convert.ToInt32(totalDaysActual), rigEscal, country, groupCase, shellShare, true);
                        res.isMaterialAdd = true;
                        res.Period = new DateRange(startThisMonthMaterial, finishThisMonthMaterial);
                        details.Add(res);
                    }

                    if (eventStartDate.Date < eventEndDate.Date)
                    {

                        DateTime startThisMonthActual = eventStartDate;
                        DateTime finishThisMonthActual = new DateTime(year, month,
                            DateTime.DaysInMonth(year, month));
                        if (finishThisMonthActual > eventEndDate)
                            finishThisMonthActual = eventEndDate;
                        double noDaysInsideLast = new DateRange(startThisMonthActual, finishThisMonthActual).Days + 1;
                        var resx = CalcBizPlanDetail(p, u.Key, Convert.ToInt32(noDaysInsideLast), Convert.ToInt32(totalDaysFull), Convert.ToInt32(totalDaysShift), Convert.ToInt32(totalDaysActual), rigEscal, country, groupCase, shellShare, false);
                        resx.Period = new DateRange(startThisMonthActual, finishThisMonthActual);

                        details.Add(resx);
                    }

                    #region Gabung
                    //BizPlanAllocationDetail restot = new BizPlanAllocationDetail();
                    //restot.MaterialCost = res.MaterialCost + resx.MaterialCost;
                    //restot.ServiceCost = res.ServiceCost + resx.ServiceCost;
                    //restot.RigCost = res.RigCost + resx.RigCost;

                    //restot.EscalationCostEDMRig = res.EscalationCostEDMRig + resx.EscalationCostEDMRig;
                    //restot.EscalationCostEDMMaterial = res.EscalationCostEDMMaterial + resx.EscalationCostEDMMaterial;
                    //restot.EscalationCostEDMServices = res.EscalationCostEDMServices + resx.EscalationCostEDMServices;
                    //restot.EscalationCostEDMTotal = res.EscalationCostEDMTotal + resx.EscalationCostEDMTotal;

                    //restot.TroubleFreeCost = res.TroubleFreeCost + resx.TroubleFreeCost;


                    //restot.CSOCostEDM = res.CSOCostEDM + resx.CSOCostEDM;
                    //restot.InflationCostEDM = res.InflationCostEDM + resx.InflationCostEDM;
                    //restot.MeanCostEDM = res.MeanCostEDM + resx.MeanCostEDM;
                    //restot.MeanCostRealTerm = res.MeanCostRealTerm + resx.MeanCostRealTerm;

                    //restot.MeanCostMOD = res.MeanCostMOD + resx.MeanCostMOD;
                    //restot.ShellShareMOD = res.ShellShareMOD + resx.ShellShareMOD;


                    //restot.Key = res.Key;
                    //restot.Period.Start = res.Period.Start;
                    //restot.Period.Finish = resx.Period.Finish;
                    //restot.isMaterialAdd = false;


                    //details.Add(restot);
                    #endregion

                }
                else // >
                {
                    // hitung actual saja
                    var res = CalcBizPlanDetail(p, u.Key, Convert.ToInt32(u.Value.FirstOrDefault().Value), Convert.ToInt32(totalDaysFull), Convert.ToInt32(totalDaysShift), Convert.ToInt32(totalDaysActual), rigEscal, country, groupCase, shellShare, false);
                    res.Period = u.Value.Keys.FirstOrDefault();
                    details.Add(res);

                }
            }

            return details;
            #endregion
        }



        public static List<BizPlanAllocationDetail> SplitAllocationAnnualy(BizPlanActivityPhase p,
            double shellShare,
            List<RigEscalation> rigEscalation = null,
            string type = "annualy",
            string rigName = "",
            string refFactorModel = "",
            string country = "")
        {
            double rigEscal = GetRigEscalation(p.Estimate.RigName);


            //calculate bulanan dulu
            List<BizPlanAllocationDetail> alloc = new List<BizPlanAllocationDetail>();
            alloc = CalculateBizPlanAllocation(p, country, refFactorModel, shellShare);

            if (type.Equals("annualy"))
            {
                #region Annualy
                List<BizPlanAllocationDetail> annually = new List<BizPlanAllocationDetail>();
                foreach (var year in alloc.GroupBy(x => x.Key.ToString().Substring(0, 4)))
                {
                    BizPlanAllocationDetail ann = new BizPlanAllocationDetail();

                    ann.CSOCostEDM = year.ToList().Sum(x => x.CSOCostEDM);
                    ann.EscalationCostEDMMaterial = year.ToList().Sum(x => x.EscalationCostEDMMaterial);
                    ann.EscalationCostEDMRig = year.ToList().Sum(x => x.EscalationCostEDMRig);
                    ann.EscalationCostEDMServices = year.ToList().Sum(x => x.EscalationCostEDMServices);
                    ann.EscalationCostEDMTotal = year.ToList().Sum(x => x.EscalationCostEDMTotal);

                    ann.InflationCostEDM = year.ToList().Sum(x => x.InflationCostEDM);
                    ann.isMaterialAdd = true;// year.ToList().Sum(x => x.InflationCostEDM);
                    ann.Key = year.Key;
                    ann.MaterialCost = year.ToList().Sum(x => x.MaterialCost);
                    ann.MeanCostMOD = year.ToList().Sum(x => x.MeanCostMOD);
                    ann.MeanCostEDM = year.ToList().Sum(x => x.MeanCostEDM);
                    ann.MeanCostRealTerm = year.ToList().Sum(x => x.MeanCostRealTerm);
                    ann.NoOfDays = year.ToList().Sum(x => x.NoOfDays);
                    ann.Period.Start = year.ToList().Select(x => x.Period.Start).Min();
                    ann.Period.Finish = year.ToList().Select(x => x.Period.Finish).Max();
                    ann.Proportion = year.ToList().Sum(x => x.Proportion);
                    ann.RigCost = year.ToList().Sum(x => x.RigCost);
                    ann.ServiceCost = year.ToList().Sum(x => x.ServiceCost);
                    ann.ShellShareMOD = year.ToList().Sum(x => x.ShellShareMOD);
                    ann.TroubleFreeCost = year.ToList().Sum(x => x.TroubleFreeCost);

                    annually.Add(ann);
                }
                #endregion
                return annually;
            }
            else if (type.Equals("monthly"))
            {
                return alloc;
            }
            else
            {
                #region Quarter
                List<BizPlanAllocationDetail> quarter = new List<BizPlanAllocationDetail>();
                foreach (var year in alloc)
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

                foreach (var year in alloc.GroupBy(x => x.Key.ToString()))
                {
                    BizPlanAllocationDetail ann = new BizPlanAllocationDetail();

                    ann.CSOCostEDM = year.ToList().Sum(x => x.CSOCostEDM);
                    ann.EscalationCostEDMMaterial = year.ToList().Sum(x => x.EscalationCostEDMMaterial);
                    ann.EscalationCostEDMRig = year.ToList().Sum(x => x.EscalationCostEDMRig);
                    ann.EscalationCostEDMServices = year.ToList().Sum(x => x.EscalationCostEDMServices);
                    ann.EscalationCostEDMTotal = year.ToList().Sum(x => x.EscalationCostEDMTotal);

                    ann.InflationCostEDM = year.ToList().Sum(x => x.InflationCostEDM);
                    ann.isMaterialAdd = true;// year.ToList().Sum(x => x.InflationCostEDM);
                    ann.Key = year.Key;
                    ann.MaterialCost = year.ToList().Sum(x => x.MaterialCost);
                    ann.MeanCostMOD = year.ToList().Sum(x => x.MeanCostMOD);
                    ann.MeanCostEDM = year.ToList().Sum(x => x.MeanCostEDM);
                    ann.MeanCostRealTerm = year.ToList().Sum(x => x.MeanCostRealTerm);
                    ann.NoOfDays = year.ToList().Sum(x => x.NoOfDays);
                    ann.Period.Start = year.ToList().Select(x => x.Period.Start).Min();
                    ann.Period.Finish = year.ToList().Select(x => x.Period.Finish).Max();
                    ann.Proportion = year.ToList().Sum(x => x.Proportion);
                    ann.RigCost = year.ToList().Sum(x => x.RigCost);
                    ann.ServiceCost = year.ToList().Sum(x => x.ServiceCost);
                    ann.ShellShareMOD = year.ToList().Sum(x => x.ShellShareMOD);
                    ann.TroubleFreeCost = year.ToList().Sum(x => x.TroubleFreeCost);

                    quarter.Add(ann);
                }
                #endregion
                return quarter;
            }

        }

        /// <summary>
        /// type : annualy, monthly, quarterly
        /// </summary>
        /// <param name="q"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<BsonDocument> TransformSummaryBizAllocationToBson(List<BizPlanActivity> p, string type = "annualy", List<string> status = null)
        {

            List<BizPlanAllocation> alocs = new List<BizPlanAllocation>();
            var totalUniqueActivities = 0;
            foreach (var bp in p)
            {
                foreach (var phs in bp.Phases)
                {
                    if (!WellActivity.isHaveWeeklyReport(bp.WellName, bp.UARigSequenceId, phs.ActivityType))
                    {
                        if (phs.Allocation != null)
                        {
                            alocs.Add(phs.Allocation);
                        }
                        else
                        {
                            phs.Allocation = new BizPlanAllocation();
                            alocs.Add(phs.Allocation);
                        }
                        totalUniqueActivities++;
                    }
                }
            }


            var aloc = alocs; // p.SelectMany(x => x.Phases).Select(y => y.Allocation).Where(x => x != null).ToList();
            //var alocs = p.SelectMany(x => x.Phases).Where(x => x.Allocation == null).ToList();//.Select(y => y.Allocation).ToList();
            if (status != null && status.Count > 0)
            {
                aloc = p.SelectMany(x => x.Phases).Where(x => status.Contains(x.Estimate.Status)).Select(y => y.Allocation).Where(x => x != null).ToList();
            }


            string[] title = new string[] { "RigCost", "MaterialCost", "ServiceCost", "TroubleFreeCost","NPTCost","TECOPCost", "EscCostRig","EscCostServices"
            , "EscCostMaterial", "EscCostTotal", "CSOCost", "InflationCost", "MeanCostEDM", "MeanCostEDMSS", "MeanCostRealTerm", "MeanCostRealTermSS", "MeanCostMOD", "MeanCostMODSS"};

            string[] titleBagus = new string[] { "Rig Cost", "Material Cost", "Service Cost", "Trouble Free Cost","NPT Cost","TECOP Cost", "Escalation Cost Rig","Escalation Cost Services"
            , "Escalation Cost Material", "Escalation Cost Total", "CSO Cost", "Inflation Cost", "Mean Cost EDM", "Mean Cost EDM With Shell Share", "Mean Cost Real Term", "Mean Cost Real Term With Shell Share", "Mean Cost MOD", "Mean Cost MOD With Shell Share"};

            var anuls = aloc.SelectMany(x => x.AnnualyBuckets);
            int maxYear = 2015;
            int minYear = 2015;
            if (anuls.Any())
            {
                maxYear = anuls.Max(x => Convert.ToInt32(x.Key));
                minYear = anuls.Where(x => Convert.ToInt32(x.Key) > 2000).Min(x => Convert.ToInt32(x.Key));
            }
            var mons = aloc.SelectMany(x => x.MonthlyBuckets);
            var quars = aloc.SelectMany(x => x.QuarterlyBuckets);

            if (type.Equals("monthly"))
            {
                var ind = 0;
                List<BsonDocument> docsMonth = new List<BsonDocument>();
                foreach (var t in title.ToList())
                {
                    BsonDocument doc = new BsonDocument();
                    doc.Set("Title", t);
                    doc.Set("MaxYear", maxYear);
                    doc.Set("MinYear", minYear);
                    doc.Set("totalUniqueActivities", totalUniqueActivities);

                    double value = 0;
                    var prevkey = "";
                    DateTime prevKeyDate = new DateTime();
                    foreach (var a in mons.OrderBy(x => System.Convert.ToInt32(x.Key)))
                    {
                        var nowKey = a.Key;
                        DateTime nowkeyDate = new DateTime(System.Convert.ToInt32(a.Key.Substring(0, 4)), System.Convert.ToInt32(a.Key.Substring(4, 2)), 1);
                        if (!prevkey.Equals(nowKey))
                        {
                            value = 0;

                                value = a.ToBsonDocument().GetDouble(t);
                                doc.Set(nowkeyDate.ToString("MMMyyyy"), double.IsNaN(value) ? 0 : value);
                                prevkey = a.Key;
                                prevKeyDate = nowkeyDate;
                        }
                        else
                        {
                                value = value + a.ToBsonDocument().GetDouble(t);
                                doc.Set(nowkeyDate.ToString("MMMyyyy"), double.IsNaN(value) ? 0 : value);
                        }
                    }

                    doc.Set("TitleBagus", titleBagus[ind]);
                    if ((t == "EscCostTotal") || (t == "TroubleFreeCost") || (t == "MeanCostEDM") || (t == "MeanCostRealTerm") || (t == "MeanCostMOD") || (t == "ShellShare") || (t == "MeanCostEDMSS") || (t == "MeanCostRealTermSS") || (t == "MeanCostMODSS"))
                    {
                        doc.Set("ColumnClass", "isSummaryField");
                    }
                    else
                    {
                        doc.Set("ColumnClass", "");
                    }
                    ind++;
                    docsMonth.Add(doc);
                }
                return docsMonth;
            }
            else if (type.Equals("annualy"))
            {
                var ind = 0;
                List<BsonDocument> docsYear = new List<BsonDocument>();
                foreach (var t in title.ToList())
                {
                    BsonDocument doc = new BsonDocument();
                    var TotalAllYears = 0.0;
                    doc.Set("Title", t);
                    doc.Set("MaxYear", maxYear);
                    doc.Set("MinYear", minYear);
                    doc.Set("totalUniqueActivities", totalUniqueActivities);

                    double value = 0;
                    var prevkey = "";
                    foreach (var a in anuls.OrderBy(x => System.Convert.ToInt32(x.Key)))
                    {
                        var nowKey = "FY" + a.Key;

                        if (!prevkey.Equals(nowKey))
                        {
                            value = 0;
                            value = a.ToBsonDocument().GetDouble(t);
                            if (t.Equals("MeanCostMOD"))
                            {
                                var x = 0;
                            }
                            doc.Set("FY" + a.Key, double.IsNaN(value) ? 0 : value);

                            prevkey = "FY" + a.Key;

                            TotalAllYears += value;
                        }
                        else
                        {
                            value = value + a.ToBsonDocument().GetDouble(t);
                            if (t.Equals("MeanCostMOD"))
                            {
                                var x = 0;
                            }
                            doc.Set("FY" + a.Key, double.IsNaN(value) ? 0 : value);
                            TotalAllYears += value;
                        }
                    }


                    doc.Set("TotalAllYears", TotalAllYears);
                    doc.Set("TitleBagus", titleBagus[ind]);
                    if ((t == "EscCostTotal") || (t == "TroubleFreeCost") || (t == "MeanCostEDM") || (t == "MeanCostRealTerm") || (t == "MeanCostMOD") || (t == "ShellShare") || (t == "MeanCostEDMSS") || (t == "MeanCostRealTermSS") || (t == "MeanCostMODSS"))
                    {
                        doc.Set("ColumnClass", "isSummaryField");
                    }
                    else
                    {
                        doc.Set("ColumnClass", "");
                    }
                    ind++;
                    docsYear.Add(doc);
                }
                return docsYear;
            }
            else
            {
                var ind = 0;
                List<BsonDocument> docsQuar = new List<BsonDocument>();
                foreach (var t in title.ToList())
                {
                    BsonDocument doc = new BsonDocument();
                    doc.Set("Title", t);
                    doc.Set("MaxYear", maxYear);
                    doc.Set("MinYear", minYear);
                    doc.Set("totalUniqueActivities", totalUniqueActivities);

                    double value = 0;
                    var prevkey = "";
                    DateTime prevKeyDate = new DateTime();
                    foreach (var a in quars.OrderBy(x => System.Convert.ToInt32(x.Key)))
                    {
                        var nowKey = a.Key;
                        DateTime nowkeyDate = new DateTime(System.Convert.ToInt32(a.Key.Substring(0, 4)), System.Convert.ToInt32(a.Key.Substring(4, 2)), 1);
                        if (!prevkey.Equals(nowKey))
                        {
                            value = 0;
                            value = a.ToBsonDocument().GetDouble(t);

                            BsonDocument header = new BsonDocument();
                            DateTime dkey = new DateTime(
                                Convert.ToInt32(a.Key.Substring(0, 4)), Convert.ToInt32(a.Key.Substring(4, 2)), 1
                                );


                            //header.Set("Year", "Q" + dkey.Month.ToString() + "_" + dkey.ToString("yyyy"));

                            doc.Set("Q" + dkey.Month.ToString() + "_" + dkey.ToString("yyyy"), double.IsNaN(value) ? 0 : value);
                            prevkey = a.Key;
                            prevKeyDate = nowkeyDate;
                        }
                        else
                        {
                            value = value + a.ToBsonDocument().GetDouble(t);
                            //doc.Set("Q" + nowkeyDate.ToString("MM") + nowkeyDate.ToString("yyyy"), double.IsNaN(value) ? 0 : value);
                            doc.Set("Q" + nowkeyDate.Month.ToString() +"_"+ nowkeyDate.ToString("yyyy"), double.IsNaN(value) ? 0 : value);
                        }
                    }
                    doc.Set("TitleBagus", titleBagus[ind]);
                    if ((t == "EscCostTotal") || (t == "TroubleFreeCost") || (t == "MeanCostEDM") || (t == "MeanCostRealTerm") || (t == "MeanCostMOD") || (t == "ShellShare") || (t == "MeanCostEDMSS") || (t == "MeanCostRealTermSS") || (t == "MeanCostMODSS"))
                    {
                        doc.Set("ColumnClass", "isSummaryField");
                    }
                    else
                    {
                        doc.Set("ColumnClass", "");
                    }
                    ind++;
                    docsQuar.Add(doc);
                }
                return docsQuar;
            }


        }

        /// <summary>
        /// type : annualy, monthly, quarterly
        /// </summary>
        /// <param name="p"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// 

        public static DateRange getEstimatePeriode(string WellName, string RigName, string ActivityType)
        {
            var listQ = new List<IMongoQuery>();
            listQ.Add(Query.EQ("WellName", WellName));
            listQ.Add(Query.EQ("RigName", RigName));
            listQ.Add(Query.EQ("Phases.ActivityType", ActivityType));
            var getbp = BizPlanActivity.Get<BizPlanActivity>(Query.And(listQ));
            return getbp.Phases.Where(x => x.ActivityType.Equals(ActivityType)).FirstOrDefault().Estimate.EstimatePeriod;
        }

        public static List<BsonDocument> TransformDetailBizAllocationToBson(List<BizPlanActivity> p, string type = "annualy", List<string> status = null)
        {
            List<BizPlanAllocation> alocs = new List<BizPlanAllocation>();
            foreach (var bp in p)
            {
                foreach(var phs in bp.Phases)
                {
                    if(!WellActivity.isHaveWeeklyReport(bp.WellName, bp.UARigSequenceId, phs.ActivityType))
                    {
                        if(phs.Allocation != null)
                        {
                            alocs.Add(phs.Allocation);
                        }
                        else
                        {
                            phs.Allocation = new BizPlanAllocation();
                        }

                    }
                }
            }

            var aloc = alocs; // p.SelectMany(x => x.Phases).Select(y => y.Allocation).Where(x => x != null).ToList();
            if (status != null && status.Count > 0)
            {
                aloc = p.SelectMany(x => x.Phases).Where(x => status.Contains(x.Estimate.Status)).Select(y => y.Allocation).Where(x => x != null).ToList();
            }	

            string[] title = new string[] { "RigCost", "MaterialCost", "ServiceCost", "TroubleFreeCost","NPTCost","TECOPCost", "EscCostRig","EscCostServices"
            , "EscCostMaterial", "EscCostTotal","MeanCostEDM", "CSOCost", "InflationCost", "MeanCostEDM", "MeanCostRealTerm", "MeanCostMOD","ShellShare", "MeanCostEDMSS", "MeanCostRealTermSS", "MeanCostMODSS" };

            var anuls = aloc.SelectMany(x => x.AnnualyBuckets);
            var mons = aloc.SelectMany(x => x.MonthlyBuckets);
            var quars = aloc.SelectMany(x => x.QuarterlyBuckets);

            if (type.Equals("monthly"))
            {
                List<BsonDocument> docsMonth = new List<BsonDocument>();
                foreach (var d in aloc)
                    docsMonth.Add(d.ToBsonDocument());
                var uw = BsonHelper.Unwind(docsMonth.ToList(), "MonthlyBuckets", "", new List<string> { "RigName", "WellName", "ActivityType", "EstimatePeriod", "UARigSequenceId", "ProjectName" });
                docsMonth = new List<BsonDocument>();
                foreach (var u in uw.GroupBy(x => BsonHelper.GetString(x, "Key")))
                {
                    BsonDocument header = new BsonDocument();
                    DateTime dkey = new DateTime(Convert.ToInt32(u.Key.Substring(0, 4)), Convert.ToInt32(u.Key.Substring(4, 2)), 1);
                    header.Set("dkey", dkey);
                    header.Set("Year", dkey.ToString("MMMyyyy"));

                    List<BsonDocument> details = new List<BsonDocument>();
                    p.ForEach(d =>
                    {
                        if (d.Phases != null && d.Phases.Count > 0)
                        {
                            d.Phases.ForEach(e =>
                            {
                                var each = new { WellName = d.WellName, RigName = e.Estimate.RigName, ProjectName = d.ProjectName, ActivityType = e.ActivityType, LESchedule = new DateRange(), Type = "" }.ToBsonDocument();

                                var target = u.Where(f => f.GetString("RigName").Equals(e.Estimate.RigName) && f.GetString("WellName").Equals(d.WellName)  
                                     && f.GetString("ActivityType").Equals(e.ActivityType)
                                    && f.GetString("UARigSequenceId").Equals(d.UARigSequenceId));

                                if (target != null && target.Count() > 0)
                                {
                                    //, UARigSequenceId = d.UARigSequenceId 
                                    each.Set("LESchedule", BsonHelper.GetDoc(target.FirstOrDefault(), "EstimatePeriod"));
                                    each.Set("UARigSequenceId", BsonHelper.GetString(target.FirstOrDefault(), "UARigSequenceId"));
                                    //each.Set("Type", BsonHelper.GetString(target, "Type"));
                                    //each.Set("EstimatePeriod", BsonHelper.GetDoc(target, "Estimate.EstimatePeriod"));
                                    foreach (var t in title)
                                    {
                                        double value = target.Sum(x => BsonHelper.GetDouble(x, t));
                                        each.Set(t, double.IsNaN(value) ? 0 : value);
                                    }
                                    details.Add(each);
                                }
                                //else
                                //{
                                //    foreach (var t in title)
                                //        each.Set(t, 0.0);
                                //}

                                //details.Add(each);
                            });
                        }
                    });
                    header.Set("Data", new BsonArray(details));

                    docsMonth.Add(header);
                }
                return docsMonth;
            }
            else if (type.Equals("annualy"))
            {
                List<BsonDocument> docsMonth = new List<BsonDocument>();
                foreach (var d in aloc)
                    docsMonth.Add(d.ToBsonDocument());
                var uw = BsonHelper.Unwind(docsMonth.ToList(), "AnnualyBuckets", "", new List<string> { "RigName", "WellName", "ActivityType", "EstimatePeriod", "UARigSequenceId", "ProjectName" });
                docsMonth = new List<BsonDocument>();
                foreach (var u in uw.GroupBy(x => BsonHelper.GetString(x, "Key")))
                {
                    BsonDocument header = new BsonDocument();
                    header.Set("Year", Convert.ToInt32(u.Key));

                    List<BsonDocument> details = new List<BsonDocument>();
                    p.ForEach(d =>
                    {
                        if (d.Phases != null && d.Phases.Count > 0)
                        {
                            d.Phases.ForEach(e =>
                            {
                                var each = new { WellName = d.WellName, RigName = e.Estimate.RigName, ProjectName = d.ProjectName, ActivityType = e.ActivityType, LESchedule = new DateRange(), Type = "", EstimatePeriod = new DateRange() }.ToBsonDocument();

                                var target = u.FirstOrDefault(f => f.GetString("RigName").Equals(e.Estimate.RigName) && f.GetString("WellName").Equals(d.WellName) 
                                    && f.GetString("ActivityType").Equals(e.ActivityType)
                                    && f.GetString("UARigSequenceId").Equals(d.UARigSequenceId)
                                    );

                                if (target != null)
                                {
                                    each.Set("LESchedule", BsonHelper.GetDoc(target, "EstimatePeriod"));
                                    each.Set("Type", BsonHelper.GetString(target, "Type"));
                                    each.Set("UARigSequenceId", BsonHelper.GetString(target, "UARigSequenceId"));
                                    //each.Set("EstimatePeriod", BsonHelper.GetDoc(target, "Estimate.EstimatePeriod"));
                                    foreach (var t in title)
                                    {
                                        double value = BsonHelper.GetDouble(target, t);
                                        each.Set(t, double.IsNaN(value) ? 0 : value);
                                    }
                                    details.Add(each);
                                }
                                //else
                                //{
                                //    foreach (var t in title)
                                //        each.Set(t, 0.0);
                                //}

                                //details.Add(each);
                            });
                        }
                    });
                    header.Set("Data", new BsonArray(details));

                    docsMonth.Add(header);
                }
                return docsMonth;
            }
            else
            {
                List<BsonDocument> docsMonth = new List<BsonDocument>();
                foreach (var d in aloc)
                    docsMonth.Add(d.ToBsonDocument());
                var uw = BsonHelper.Unwind(docsMonth.ToList(), "QuarterlyBuckets", "", new List<string> { "RigName", "WellName", "ActivityType", "EstimatePeriod", "UARigSequenceId", "ProjectName" });
                docsMonth = new List<BsonDocument>();
                foreach (var u in uw.GroupBy(x => BsonHelper.GetString(x, "Key")))
                {
                    BsonDocument header = new BsonDocument();
                    DateTime dkey = new DateTime(Convert.ToInt32(u.Key.Substring(0, 4)), Convert.ToInt32(u.Key.Substring(4, 2)), 1);
                    header.Set("dkey", dkey);
                    header.Set("Year", "Q" + dkey.Month.ToString() + "_" + dkey.ToString("yyyy"));

                    List<BsonDocument> details = new List<BsonDocument>();
                    p.ForEach(d =>
                    {
                        if (d.Phases != null && d.Phases.Count > 0)
                        {
                            d.Phases.ForEach(e =>
                            {
                                var each = new { WellName = d.WellName, RigName = e.Estimate.RigName, ProjectName = d.ProjectName, ActivityType = e.ActivityType, LESchedule = new DateRange(), Type = "" }.ToBsonDocument();

                                var target = u.Where(f => f.GetString("RigName").Equals(e.Estimate.RigName) && f.GetString("WellName").Equals(d.WellName)  
                                    && f.GetString("ActivityType").Equals(e.ActivityType)
                                    && f.GetString("UARigSequenceId").Equals(d.UARigSequenceId)
                                    );

                                if (target != null && target.Count() > 0)
                                {
                                    each.Set("LESchedule", BsonHelper.GetDoc(target.FirstOrDefault(), "EstimatePeriod"));
                                    //each.Set("Type", BsonHelper.GetString(target, "Type"));
                                    //each.Set("EstimatePeriod", BsonHelper.GetDoc(target, "Estimate.EstimatePeriod"));
                                    each.Set("UARigSequenceId", BsonHelper.GetString(target.FirstOrDefault(), "UARigSequenceId"));
                                    foreach (var t in title)
                                    {
                                        double value = target.Sum(x => BsonHelper.GetDouble(x, t));
                                        each.Set(t, double.IsNaN(value) ? 0 : value);
                                    }
                                    details.Add(each);
                                }
                                //else
                                //{
                                //    foreach (var t in title)
                                //        each.Set(t, 0.0);
                                //}

                                //details.Add(each);
                            });
                        }
                    });
                    header.Set("Data", new BsonArray(details));

                    docsMonth.Add(header);
                }
                return docsMonth;
            }


        }


    }



    public class BizPlanAllocationDetail
    {

        public BizPlanAllocationDetail()
        {
            Period = new DateRange();
        }
        // 1
        public double EscalationCostEDMRig { get; set; }
        // 2
        public double EscalationCostEDMServices { get; set; }
        // 3
        public double EscalationCostEDMMaterial { get; set; }
        // 4
        public double EscalationCostEDMTotal { get; set; }
        // 5
        public double CSOCostEDM { get; set; }
        // 6
        public double InflationCostEDM { get; set; }
        // 7
        public double MeanCostEDM { get; set; }
        // 8
        public double MeanCostRealTerm { get; set; }
        // 9
        public double MeanCostMOD { get; set; }
        // 10
        public double ShellShareMOD { get; set; }

        public bool isMaterialAdd { get; set; }

        public string Key { get; set; }
        public double NoOfDays { get; set; }

        public double Proportion { get; set; }

        public DateRange Period { get; set; }

        public double RigCost { get; set; }
        public double MaterialCost { get; set; }
        public double ServiceCost { get; set; }
        public double TroubleFreeCost { get; set; }

        public double NPTCost { get; set; }
        public double TECOPCost { get; set; }

        public double NPTDays { get; set; }
        public double TECOPDays { get; set; }

        public double SpreadRateWORig { get; set; }
        public double SpreadRateTotal { get; set; }
        public double BurnRate { get; set; }


        public double RigDays { get; set; }

        public bool isLL { get; set; }

        public double ActiveDays { get; set; }
    }
}
