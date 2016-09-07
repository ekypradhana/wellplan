using ECIS.Core;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECIS.Client.WEIS
{
    public class BizPlanCalculation : ECISModel
    {
        public override string TableName
        {
            get { return "BizPlanCalculation"; }
        }
        public override MongoDB.Bson.BsonDocument PreSave(MongoDB.Bson.BsonDocument doc, string[] references = null)
        {
            _id = string.Format("W{0}R{1}A{2}", WellName.Replace(" ", ""), RigName.Replace(" ", ""), ActivityType.Replace(" ", ""));
            return this.ToBsonDocument();
        }
        #region Attribute
        public string WellName { get; set; }
        public string RigName { get; set; }
        public string Country { get; set; }
        public string ActivityType { get; set; }
        public string RefFactorTitle { get; set; }

        public double TroubleFreeCostUSD { get; set; }
        public double NPTCostUSD { get; set; }
        public double TECOPCostUSD { get; set; }
        public double MeanCostEDMUSD { get; set; }

        public double ShellShare { get; set; }
        public DateRange EventDate { get; set; }
        public DateTime OriginalEventStartDate { get; set; }
        public double MeanTime { get; set; }

        public int MaturityLevel { get; set; }
        public DateTime MaterialLeadDate { get; set; }
        public DateTime NowDate { get { return DateTime.Now.Date; } }
        double FirstRate = 0;

        public double ServicesCost { get; set; }
        public double EscServicesCost { get; set; }
        public double MaterialCost { get; set; }
        public FYLongLead MaterialLongLead { get; set; }
        //public LongLead MaterialLongLeads { get; set; }
        public double EscMaterialCost { get; set; }
        /// <summary>
        /// blended RigRate
        /// </summary>
        public double RigRate { get; set; }
        public double RigCost { get; set; }
        public double RigCostTotal { get; set; }
        public double MaterialCostTotal { get; set; }
        public double ServicesCostTotal { get; set; }
        public List<FYValue> RigCostMonthly { get; set; }
        public List<FYValue> RigCostAnnualy { get; set; }
        public List<RigEscDetail> RigEscCostAnnualy { get; set; }
        public List<RigCostTotalDetail> RigCostAnnualyWithEsc { get; set; }
        public double TotalRigCostPlusEsc { get; set; }
        public double RigEscCost { get; set; }
        public double EscTotalCost { get; set; }
        public double TroubleFreeTime { get; set; }
        //public double NPTTime { get; set; }
        //public double TECOPTime { get; set; }
        public double NPTCost { get; set; }
        public double NPTDays { get; set; }
        public double TECOPCost { get; set; }
        public double TECOPDays { get; set; }
        public double SpreadRateWORig { get; set; }
        public double BurnRate { get; set; }
        public double SpreadRateTotal { get; set; }
        public double TroubleFreeCost { get; set; }
        public double MeanCostEDM { get; set; }

        public double CSOCost { get; set; }
        public double InflationCost { get; set; }
        public double MeanCostRealTerm { get; set; }
        public double MeanCostMOD { get; set; }
        public double ShellShareCost { get; set; }

        public WEISReferenceFactorModel RefFactorModels { get; set; }
        //public MaturityRiskMatrix MaturityRisk { get; set; }
        public Dictionary<int, double> RigRates { get; set; }
        public Dictionary<int, double> ExchangeRates { get; set; }

        public DateTime FinishOperationDate { get; set; }
        #endregion

        #region _temp

        //public List<ServiceDetail> EscServicesCostMonthly { get; set; }
        //public List<FYValue> ServicesCostMonthly { get; set; }
        //public double MaterialCostActive { get; set; }
        //public List<FYValue> MaterialCostActiveMonthly { get; set; }
        //public double MaterialLongLeadCostOnly { get; set; }
        ////public double LongLeadCost { get; set; }
        //public List<FYValue> MaterialLongLeadCostOnlyMonthly { get; set; }
        //public double MaterialTotalWithEsc { get; set; }
        //public List<FYValue> MaterialTotalWithEscMonthly { get; set; }
        //public List<FYValue> EscMaterialCostMonthly { get; set; }
        //public double MaterialCostWithoutEsc { get; set; }
        //public List<FYValue> MaterialCostWithoutEscMonthly { get; set; }
        //public double RigRateMeanTime { get; set; }
        //public List<RigDetail> RigEscCostMonthly { get; set; }
        //public List<FYValue> EscTotalCostMonthly { get; set; }
        //public List<FYValue> TroubleFreeCostMonthly { get; set; }
        //public List<FYValue> MeanCostEDMMonthly { get; set; }

        // public List<FYValue> CSOCostMonthly { get; set; }
        //public List<FYValue> InflationCostMonthly { get; set; }
        //public List<FYValue> MeanCostRealTermMonthly { get; set; }
        //public List<FYValue> MeanCostMODMonthly { get; set; }
        //public List<FYValue> ShellShareCostMonthly { get; set; }

        #endregion

        public static BizPlanCalculation calcBizPlan(string wellName,
            string rigName,
            string activityType,
            string country,
            double shellShare,
            DateRange eventPeriod,
            int maturityLevel,
            double services,
            double material,
            double troubleFreeTime,
            string refFactorModel,
            
            string baseop ,

            double NPTRate = 0,
            double TECOPRate = 0,
            double NPTRateCost = 0,
            double TECOPRateCost = 0,
            double monthRequired = 0,
            double tangibleValue = 0,
            double currentSpreadRateTotal = 0,
            double meanTime = 0,
            // tla
            bool isTLA = false,
            double tlaServices = 0,
            double tlaMaterial = 0,
            double tlaTroubleDays = 0,
            double tlaNptDay = 0,
            double tlaTangible = 0,
            double tlaSpreadrate = 0

            )
        {
            if (!isTLA)
            {
                var res = new BizPlanCalculation(wellName, rigName, activityType, country, shellShare, eventPeriod, maturityLevel, services, material, troubleFreeTime, refFactorModel, baseop, NPTRate, TECOPRate, NPTRateCost, TECOPRateCost,
                    monthRequired, tangibleValue);
                return res;
            }
            else
            {
                // tla
                var res = new BizPlanCalculation(wellName, rigName, activityType, country, shellShare, eventPeriod, maturityLevel, services, material, troubleFreeTime, refFactorModel, baseop, NPTRate, TECOPRate, NPTRateCost, TECOPRateCost,
                        monthRequired, tangibleValue,
                        isTLA, tlaServices, tlaMaterial, tlaTroubleDays, tlaNptDay, tlaTangible, tlaSpreadrate, currentSpreadRateTotal, meanTime);
                return res;
            }
        }

        public BizPlanCalculation(string wellName,
            string rigName,
            string activityType,
            string country,
            double shellShare,
            DateRange eventPeriod,
            int maturityLevel,
            double services,
            double material,
            double troubleFreeTime,
            string refFactorModel,
            string baseop,
            double NPTRate = 0,
            double TECOPRate = 0,
            double NPTRateCost = 0,
            double TECOPRateCost = 0,
            double monthRequired = 0,
            double tangibleValue = 0

            )
        {
            ShellShare = shellShare > 1 ? shellShare / 100 : shellShare;
            WellName = wellName;
            RigName = rigName;
            ActivityType = activityType;
            EventDate = eventPeriod;
            MaturityLevel = maturityLevel;
            Country = country;
            OriginalEventStartDate = EventDate.Start;
            //8
            TroubleFreeTime = troubleFreeTime;
            RefFactorTitle = refFactorModel;

            var baseTime = troubleFreeTime + troubleFreeTime * NPTRate / (1 - NPTRate);
            FinishOperationDate = EventDate.Start.AddDays(troubleFreeTime - 1);

            MaterialCost = material;
            ServicesCost = services;

            SetExchangeRates(baseop);
            SetRefFactorModels();
            SetMaterialEscalatedDate(monthRequired);
            #region SetMaterialLongLead
            MaterialLongLead = new FYLongLead();
            MaterialLongLead.MonthRequiredValue = monthRequired;
            MaterialLongLead.Year = eventPeriod.Start.Year;
            MaterialLongLead.TangibleValue = tangibleValue;
            #endregion

            #region Rig, Services, Material
            var RigCostMonthly = GetRigCostMonthly();
            var ServicesCostMonthly = GetServicesCostMonthly();
            var MaterialCostMonthlyLL = GetMaterialCostLongLeadMonthly();

            var plusActive = TroubleFreeTime;
            eventPeriod.Finish = EventDate.Start.AddDays(Convert.ToInt32(plusActive) - 1);
            var MaterialCostMonthlyActive = GetMaterialCostActiveMonthly();

            #endregion

            List<FYValue> combine = new List<FYValue>();
            foreach (var y in MaterialCostMonthlyLL)
                combine.Add(y);

            foreach (var y in MaterialCostMonthlyActive)
            {
                var uu = combine.Where(x => x.Id == y.Id);
                if (uu.Any())
                {
                    DateTime start = uu.FirstOrDefault().Period.Start;
                    DateTime finish = y.Period.Finish;
                    combine.Where(x => x.Id == y.Id).FirstOrDefault().Value = y.Value + uu.FirstOrDefault().Value;
                    combine.Where(x => x.Id == y.Id).FirstOrDefault().Period = new DateRange(start, finish);
                    combine.Where(x => x.Id == y.Id).FirstOrDefault().Type = "LLActive";
                    combine.Where(x => x.Id == y.Id).FirstOrDefault().RigDays = RigCostMonthly.Where(x => x.Id == y.Id).ToList().Sum(x => x.RigDays);

                }
                else
                {
                    y.RigDays = RigCostMonthly.Where(x => x.Id == y.Id).ToList().Sum(x => x.RigDays);
                    combine.Add(y);

                }
            }

            #region NPT

            // new : NPT dan TECOP days dihitung di luar LL + Aktif, 
            // Period LL, NPT + Tecop saat ini, Start = Current Finish, Finish = Finish.addDays(tft)
            List<FYValue> NPTMonthly = new List<FYValue>();
            if (NPTRate > 0)
            {
                NPTDays = Tools.Div((TroubleFreeTime * NPTRate), (1 - NPTRate));
                var tft = NPTDays;
                var NPTRange = new DateRange(eventPeriod.Finish, eventPeriod.Finish.AddDays(tft));
                NPTMonthly = GetNPTMonthly(NPTRange);
                eventPeriod.Finish = EventDate.Finish.AddDays(Convert.ToInt32(tft));
            }


            if (NPTMonthly.Count > 0)
            {
                foreach (var y in NPTMonthly)
                {
                    var uu = combine.Where(x => x.Id == y.Id);
                    if (uu.Any())
                    {
                        DateTime start = uu.FirstOrDefault().Period.Start;
                        DateTime finish = y.Period.Finish;
                        //combine.Where(x => x.Id == y.Id).FirstOrDefault().Value = y.Value; // +uu.FirstOrDefault().Value;
                        combine.Where(x => x.Id == y.Id).FirstOrDefault().Period = new DateRange(start, finish);
                        combine.Where(x => x.Id == y.Id).FirstOrDefault().Type = "ActiveTECOPNPT";
                    }
                    else
                    {
                        var ty = new FYValue();
                        ty.Id = y.Id;
                        ty.Type = "ActiveTECOPNPT";
                        ty.Value = 0;
                        ty.Period = y.Period;
                        combine.Add(ty);

                    }
                }
            }
            #endregion

            List<BizPlanAllocationDetail> details = new List<BizPlanAllocationDetail>();
            //int ix = 0;
            int blnper = 0;
            foreach (var c in combine)
            {
                BizPlanAllocationDetail det = new BizPlanAllocationDetail();

                det.Key = c.Id.ToString();
                det.Period = c.Period;
                det.NoOfDays = c.Period.Days + 1;
                if (!c.Type.Equals("LL") && blnper == 0)
                {
                    det.MaterialCost = material * (1 - tangibleValue);
                    blnper++;
                }
                else if (c.Type.Equals("LL"))
                {
                    det.MaterialCost = c.Value;
                }
                //det.MaterialCost = c.Value;
                var rigAvail = RigCostMonthly.Where(x => x.Id == c.Id);
                if (rigAvail.Any())
                    det.RigCost = rigAvail.FirstOrDefault().Value;
                var serAvail = ServicesCostMonthly.Where(x => x.Id == c.Id);
                if (serAvail.Any())
                    det.ServiceCost = serAvail.FirstOrDefault().Value;

                // before 1 dec 2015
                var troubleFreeCost = det.MaterialCost + det.ServiceCost + det.RigCost;
                det.TroubleFreeCost = troubleFreeCost;

                // NPT days
                var nptAvail = NPTMonthly.Select(x => x.Id).Contains(c.Id);
                if (nptAvail)
                {
                    det.NPTDays = Tools.Div((troubleFreeTime * NPTRate), (1 - NPTRate));
                }

                details.Add(det);
            }

            Monthly = new List<BizPlanAllocationDetail>();
            Monthly = details;

            RigCostTotal = Monthly.Select(x => x.RigCost).Sum();
            MaterialCostTotal = Monthly.Select(x => x.MaterialCost).Sum();
            ServicesCostTotal = Monthly.Select(x => x.ServiceCost).Sum();
            TroubleFreeCost = Monthly.Select(x => x.TroubleFreeCost).Sum();
            var nptDaysTotal = Monthly.Select(x => x.NPTDays).Sum();
            RigRate = Tools.Div(RigCostTotal, troubleFreeTime);

            //BurnRate = +Tools.Div(ServicesCostTotal + MaterialCostTotal + (RigRate * baseTime), baseTime);
            //SpreadRateWORig = Tools.Div(ServicesCostTotal, baseTime);
            //SpreadRateTotal = RigRate + SpreadRateWORig;
            var isNotZero = Monthly.Where(x => x.NPTDays != 0);
            if (isNotZero.Any())
            {
                NPTDays = Monthly.Where(x => x.NPTDays != 0).FirstOrDefault().NPTDays;
            }
            //NPTDays = Monthly.Where(x => x.NPTDays != 0).FirstOrDefault().NPTDays;
            NPTCost = (TroubleFreeCost - MaterialCostTotal) * Tools.Div(NPTRateCost, 1 - NPTRateCost);
            TECOPCost = TECOPRateCost * (TroubleFreeCost + NPTCost);
            //TECOPDays = TECOPRate * (baseTime * Tools.Div(SpreadRateTotal, BurnRate));
            TECOPDays = TECOPRate * (troubleFreeTime + NPTDays) * Tools.Div((TroubleFreeCost + NPTCost - material), (TroubleFreeCost + NPTCost));
            //MeanTime = baseTime + TECOPDays;
            MeanTime = troubleFreeTime + TECOPDays + NPTDays;
            var newFInish = EventDate.Finish.AddDays(TECOPDays);

            if (!combine.Max(x => x.Id).ToString().Equals(newFInish.ToString("yyyyMM")))
            {
                var newDr = new DateRange(EventDate.Finish, newFInish);
                var dayPerMonth = DateRangeToMonth.NumDaysPerMonth(newDr);

                foreach (var dpm in dayPerMonth)
                {
                    var dd = details.Where(x => x.Key == dpm.Key.ToString());
                    BizPlanAllocationDetail det = null;

                    var tecop = Tools.Div(dpm.Value.FirstOrDefault().Value, TECOPDays, 3, 1) * TECOPDays;


                    if (dd.Any())
                    {
                        det = dd.FirstOrDefault();
                        det.TECOPDays = tecop;
                    }
                    else
                    {
                        det = new BizPlanAllocationDetail();
                        det.TECOPDays = tecop;
                        det.Key = dpm.Key.ToString();
                        details.Add(det);
                        combine.Add(new FYValue
                        {
                            Id = dpm.Key,
                            Period = dpm.Value.FirstOrDefault().Key,
                            Value = tecop, //Tools.Div(dpm.Value.FirstOrDefault().Value, TECOPDays, 3, 1) * TECOPCost,
                            Type = "TECOP"
                        });
                    }
                }

            }
            else
            {
                var dd = details.Where(x => x.Key == newFInish.ToString("yyyyMM"));// b.Key.ToString());

                var daysno = combine.Where(x => x.Id == Convert.ToInt32(newFInish.ToString("yyyyMM"))).Select(x => x.Value).FirstOrDefault();
                BizPlanAllocationDetail det = null;
                var tecop = Tools.Div(daysno, TECOPDays, 3, 1) * TECOPDays;
                if (dd.Any())
                {
                    det = dd.FirstOrDefault();
                    det.TECOPDays = TECOPDays;
                }
                else
                {
                    det = new BizPlanAllocationDetail();
                    det.TECOPDays = tecop;
                    det.Period = combine.Where(x => x.Id == Convert.ToInt32(newFInish.ToString("yyyyMM"))).FirstOrDefault().Period;
                    det.Key = newFInish.ToString("yyyyMM"); // dd.FirstOrDefault().Key;
                    details.Add(det);
                }
            }

            #region Escalation Calculation

            var operationFinish = eventPeriod.Start.AddDays(TroubleFreeTime - 1);
            var validPeriod = new DateRange(eventPeriod.Start, operationFinish);
            double durationRemain = MeanTime;
            bool startComsumpt = false;
            foreach (var c in combine.OrderBy(x => x.Id))
            {
                var dd = details.Where(x => x.Key == c.Id.ToString());
                var det = dd.FirstOrDefault();
                det.RigDays = c.RigDays;
                det.TECOPDays = 0;
                det.NPTDays = 0;
                if (det.RigDays > 0)
                {
                    startComsumpt = true;
                    durationRemain -= det.RigDays;
                }
                if (startComsumpt)
                {
                    if (durationRemain > 0)
                    {
                        if (durationRemain > det.NPTDays)
                            durationRemain -= det.NPTDays;
                        else
                            det.NPTDays = durationRemain;
                    }

                    if (durationRemain > 0)
                    {
                        if (durationRemain > det.TECOPDays)
                            durationRemain -= det.TECOPDays;
                        else
                            det.TECOPDays = durationRemain;
                    }
                }
            }

            if (durationRemain > 0)
            {
                var det = details.OrderByDescending(d => d.Key).FirstOrDefault();
                det.TECOPDays += durationRemain;
            }
            double dummyCheck = details.Sum(d => d.RigDays + d.NPTDays + d.TECOPDays) - MeanTime;

            var meancostEDMRigRates = Tools.Div(((RigRate * troubleFreeTime) +
                ((RigRate * troubleFreeTime) * (NPTRateCost / (1 - NPTRateCost))) +
                ((RigRate * troubleFreeTime) + ((RigRate * troubleFreeTime) * (NPTRateCost / (1 - NPTRateCost)))) * TECOPRateCost), MeanTime);

            DateRange hitAllDay = new DateRange(combine.OrderBy(x => x.Period.Start).Select(x => x.Period.Start).FirstOrDefault(),
                combine.OrderByDescending(x => x.Period.Finish).Select(x => x.Period.Finish).FirstOrDefault()
                );
            int blnpertama = 0;
            double totalActiveDays = MeanTime;

            foreach (var c in combine)
            {
                var dd = details.Where(x => x.Key == c.Id.ToString());
                var det = dd.FirstOrDefault();

                det.NPTCost = Tools.Div(((det.NPTDays + det.TECOPDays + det.RigDays) * NPTCost), MeanTime);
                if (c.Type.Equals("LL"))
                    det.TECOPCost = TECOPRateCost * det.MaterialCost;
                else
                    det.TECOPCost = Tools.Div(((det.NPTDays + det.TECOPDays + det.RigDays) * TECOPCost), MeanTime);

                // Mean Cost EDM
                det.MeanCostEDM = det.TroubleFreeCost + det.NPTCost + det.TECOPCost;

                // rig Escal 
                CalcRigCost();
                if (det.RigDays > 0)
                {
                    var cosmon = RigCostMonthly.Where(x => x.Id == c.Id);
                    if (cosmon.Any())
                    {
                        //if (ix > 0)
                        //{
                        //old
                        //var rigEscCost = cosmon.FirstOrDefault().Value - (det.RigDays * FirstRate);

                        var RigRateNowYear = RigCostAnnualy.Where(x => x.Id.ToString().Contains(DateTime.Now.Year.ToString()));
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
                                        bool isInRange = DateRangeToMonth.isDateInsideEqualofRange(c.Period.Start, idl.Period);
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
                                        bool isInRange = DateRangeToMonth.isDateInsideEqualofRange(c.Period.Start, idl.Period);
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

                        var rigEscCost = (det.NoOfDays * meancostEDMRigRates) * (Tools.Div((rateSpecNow - rateNow), rateNow));
                        det.EscalationCostEDMRig = rigEscCost;
                        //}
                        //ix++;
                    }
                    else
                        det.EscalationCostEDMRig = 0;
                }


                // services escal
                var servicRefModel = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Service Escalation Factors"));
                double serviceEscVal = 0;
                if (servicRefModel.Any())
                {
                    var ess = servicRefModel.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + c.Id.ToString().Substring(0, 4)));
                    if (ess.Any())
                        serviceEscVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                }
                // new service escal
                if (c.Type.Contains("Active"))
                {

                    if (c.Type.Equals("ActiveTECOPNPT"))
                    {
                        //DateRange activeDate = new DateRange(c.Period.Start, eventPeriod.Finish.Date);
                        var calcSer = Tools.Div(totalActiveDays, MeanTime) * serviceEscVal * (((services + (services * (Tools.Div(NPTRateCost, (1 - NPTRateCost))) +
                     TECOPRateCost * ((services + (services * (Tools.Div(NPTRateCost, (1 - NPTRateCost))))))))));
                        det.EscalationCostEDMServices = calcSer;

                        det.ActiveDays = totalActiveDays;
                    }
                    else
                    {
                        var calcSer = Tools.Div(c.Period.Days + 1, MeanTime) * serviceEscVal * (((services + (services * (Tools.Div(NPTRateCost, (1 - NPTRateCost))) +
                       TECOPRateCost * ((services + (services * (Tools.Div(NPTRateCost, (1 - NPTRateCost))))))))));
                        det.EscalationCostEDMServices = calcSer;

                        det.ActiveDays = c.Period.Days + 1;
                    }
                    totalActiveDays = totalActiveDays - (c.Period.Days + 1);


                }
                //det.EscalationCostEDMServices = det.ServiceCost * serviceEscVal;

                // material escal
                var matRefModel = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Material Escalation Factors"));
                double matescval = 0; // compound mat esc value annualy
                if (matRefModel.Any())
                {
                    var ess = matRefModel.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + c.Id.ToString().Substring(0, 4)));
                    if (ess.Any())
                        matescval = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                }

                // modif material esc
                if (c.Type.Contains("Active"))
                {
                    if (blnpertama == 0)
                    {

                        var calc2 = matescval * (material * (1 - tangibleValue)) * (1 + TECOPRateCost);
                        // var MatescModif = c.Value * matescval * (1 + TECOPRateCost);
                        det.EscalationCostEDMMaterial = calc2;
                        blnpertama++;

                    }
                }
                else if (c.Type.Contains("LL"))
                {
                    var MatescModif = c.Value * matescval * (1 + TECOPRateCost);
                    det.EscalationCostEDMMaterial = MatescModif;
                    det.isLL = true;
                }

                // old before update formula 07/12/2015
                //var matper = matescval * c.Value;
                //det.EscalationCostEDMMaterial = matper;

                // total esc
                det.EscalationCostEDMTotal = det.EscalationCostEDMMaterial + det.EscalationCostEDMRig + det.EscalationCostEDMServices;

                // CSO 
                var csoFc = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("CSO Factors"));
                double csoFcVal = 0;
                if (matRefModel.Any())
                {
                    var ess = csoFc.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + c.Id.ToString().Substring(0, 4)));
                    if (ess.Any())
                        csoFcVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                }

                // CSO Cost EDM
                det.CSOCostEDM = csoFcVal * (det.MeanCostEDM + det.EscalationCostEDMTotal);

                // Inflation 
                var inf = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Inflation Factors"));
                double infVal = 0;
                if (matRefModel.Any())
                {
                    var ess = inf.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + c.Id.ToString().Substring(0, 4)));
                    if (ess.Any())
                        infVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                }

                // Inflation Cost EDM
                det.InflationCostEDM = infVal * (det.MeanCostEDM + det.EscalationCostEDMTotal + det.CSOCostEDM);

                // Mean Cost MOD
                det.MeanCostMOD = det.MeanCostRealTerm + det.CSOCostEDM + det.InflationCostEDM;

                // Mean Cost Real Term
                //det.MeanCostRealTerm = det.MeanCostEDM + det.EscalationCostEDMTotal; -> old
                det.MeanCostRealTerm = Tools.Div(det.MeanCostMOD, (1 + infVal)); // -> Mean Cost Real Terms = Mean Cost MOD / (1 + inflation rate)

                // Shell share 
                det.ShellShareMOD = ShellShare * det.MeanCostMOD;
            }

            #endregion
            TECOPCost = Monthly.Select(x => x.TECOPCost).Sum();
            BurnRate = Tools.Div(details.Sum(x => x.MeanCostEDM), MeanTime); //+Tools.Div(ServicesCostTotal + MaterialCostTotal + (RigRate * baseTime), baseTime);
            //SpreadRateWORig = Tools.Div(ServicesCostTotal, baseTime);
            SpreadRateWORig = Tools.Div((1 + TECOPRateCost) * (services + ((services * (Tools.Div(NPTRateCost, (1 - NPTRateCost)))
                ))), MeanTime);
            //Tools.Div(ServicesCostTotal, baseTime);
            //SpreadRateTotal = RigRate + SpreadRateWORig;

            RigEscCost = Monthly.Select(x => x.EscalationCostEDMRig).Sum();
            EscMaterialCost = Monthly.Select(x => x.EscalationCostEDMMaterial).Sum();
            EscServicesCost = Monthly.Select(x => x.EscalationCostEDMServices).Sum();

            CSOCost = Monthly.Select(x => x.CSOCostEDM).Sum();
            InflationCost = Monthly.Select(x => x.InflationCostEDM).Sum();

            MeanCostEDM = Monthly.Select(x => x.MeanCostEDM).Sum();

            SpreadRateTotal = Tools.Div((MeanCostEDM - (material + (material * TECOPRateCost))), MeanTime);//RigRate + SpreadRateWORig;

            MeanCostMOD = Monthly.Select(x => x.MeanCostMOD).Sum();
            MeanCostRealTerm = Monthly.Select(x => x.MeanCostRealTerm).Sum();
            ShellShareCost = Monthly.Select(x => x.ShellShareMOD).Sum();

            double rate = 0;
            List<double> rateEx = new List<double>();

            for (int i = eventPeriod.Start.Year; i <= eventPeriod.Finish.Year; i++)
            {
                var ada = ExchangeRates.Where(x => x.Key == i);
                if (ada.Any())
                    rateEx.Add(ada.FirstOrDefault().Value);
            }

            if (rateEx.Count == 0) rate = 1; else rate = rateEx.Average();

            TroubleFreeCostUSD = TroubleFreeCost * rate;
            NPTCostUSD = NPTCost * rate;
            TECOPCostUSD = TECOPCost * rate;
            MeanCostEDMUSD = MeanCostEDM * rate;

            SetAnnualy();
            SetQuarter();

            EventDate.Finish = newFInish;

        }


        public double ReverseCalculation(
           string wellName,
            string rigName,
            string activityType,
            string country,
            double shellShare,
            DateRange eventPeriod,
            int maturityLevel,
            double services,
            double material,
            double troubleFreeTime,
            string refFactorModel,
            double NPTRate,
            double TECOPRate,
            double NPTRateCost,
            double TECOPRateCost,
            double monthRequired,
            double tangibleValue,
            double tlaSpreadRate,
            double CurrentSpreadRate,
            double meanTime)
        {
            //BizPlanCalculation bpn = new BizPlanCalculation(wellName, rigName, activityType, country, shellShare, eventPeriod, maturityLevel, services, material, troubleFreeTime, refFactorModel, NPTRate, TECOPRate, NPTRateCost, TECOPRateCost, monthRequired, tangibleValue);
            var spreadCurrent = CurrentSpreadRate;
            var tla = tlaSpreadRate;
            if (tla > 1)
                tlaSpreadRate = Tools.Div(tlaSpreadRate, 100);

            var updatedSpreadRate = spreadCurrent + (spreadCurrent * tlaSpreadRate);

            // Spread Rate Total = [  Mean cost - { Materials + ( Materials * TECOP Cost % ) }  ]  /   Mean Days
            // Mean Cost EDM = Trouble Free Cost + NPT Cost + TECOP Cost
            // Trouble Free Cost = (Trouble Free Days * Rig Rate) + Services + Materials

            var mce = (updatedSpreadRate * meanTime) + (material + (material * TECOPRateCost));

            // x = (mce) - (mce * npt) + (mat * npt) + (tecop * mat * npt) / (1 + tecop + tecop * npt)
            var firstEq = mce - (mce * NPTRateCost) + (material * NPTRateCost) + (TECOPRateCost * material * NPTRateCost);
            var secondEq = (1 + TECOPRateCost + (TECOPRateCost * NPTRateCost));

            var troubleFreeCost = Tools.Div(firstEq, secondEq);
            //var troubleFreeCostCurrent = bpn.TroubleFreeCost;

            var NPTCostNew = (troubleFreeCost - material) * Tools.Div(NPTRateCost, 1 - NPTRateCost);
            //var NPTCostCurrent = bpn.NPTCost; 
            var TECOPCostNew = TECOPRateCost * (troubleFreeCost - NPTCostNew);
            //var TECOPCostCurret = bpn.TECOPCost; 

            // RigRateNow = (troubleFreeCost - services - material) / troubleFreeTime 
            var rigRateNow = Tools.Div(troubleFreeCost - material - services, troubleFreeTime);

            return rigRateNow;
            //var curTroubleFreeCost = x


        }

        /// <summary>
        /// TLA Calculation
        /// </summary>
        /// <param name="wellName"></param>
        /// <param name="rigName"></param>
        /// <param name="activityType"></param>
        /// <param name="country"></param>
        /// <param name="shellShare"></param>
        /// <param name="eventPeriod"></param>
        /// <param name="maturityLevel"></param>
        /// <param name="services"></param>
        /// <param name="material"></param>
        /// <param name="troubleFreeTime"></param>
        /// <param name="refFactorModel"></param>
        /// <param name="NPTRate"></param>
        /// <param name="TECOPRate"></param>
        /// <param name="NPTRateCost"></param>
        /// <param name="TECOPRateCost"></param>
        /// <param name="monthRequired"></param>
        /// <param name="tangibleValue"></param>
        /// <param name="isTLA"></param>
        /// <param name="tlaServices"></param>
        /// <param name="tlaMaterial"></param>
        /// <param name="tlaTroubleDays"></param>
        /// <param name="tlaNptDay"></param>
        /// <param name="tlaTangible"></param>
        /// <param name="tlaSpreadrate"></param>
        public BizPlanCalculation(string wellName,
                  string rigName,
                  string activityType,
                  string country,
                  double shellShare,
                  DateRange eventPeriod,
                  int maturityLevel,
                  double services,
                  double material,
                  double troubleFreeTime,
                  string refFactorModel,
            
            string baseop,

                  double NPTRate = 0,
                  double TECOPRate = 0,
                  double NPTRateCost = 0,
                  double TECOPRateCost = 0,
                  double monthRequired = 0,
                  double tangibleValue = 0,
                  bool isTLA = false,
                  double tlaServices = 0,
                  double tlaMaterial = 0,
                  double tlaTroubleDays = 0,
                  double tlaNptDay = 0,
                  double tlaTangible = 0,
                  double tlaSpreadrate = 0, double currentSpreadRateTotal = 0,
            double meanTime = 0)
        {
            ShellShare = shellShare > 1 ? shellShare / 100 : shellShare;
            WellName = wellName;
            RigName = rigName;
            ActivityType = activityType;
            EventDate = eventPeriod;
            MaturityLevel = maturityLevel;
            Country = country;
            OriginalEventStartDate = EventDate.Start;
            //8
            TroubleFreeTime = troubleFreeTime;
            RefFactorTitle = refFactorModel;


            // Trouble Free Cost TLA
            if (isTLA)
            {
                double tfdPercen = tlaTroubleDays > 1 ? Tools.Div(tlaTroubleDays, 100) : tlaTroubleDays;
                TroubleFreeTime = TroubleFreeTime + (tfdPercen * TroubleFreeTime);
            }

            // nPT
            if (isTLA)
            {
                double nptPer = tlaNptDay > 1 ? Tools.Div(tlaNptDay, 100) : tlaNptDay;
                NPTRate = NPTRate + (NPTRate * nptPer);
            }

            var baseTime = troubleFreeTime + troubleFreeTime * NPTRate / (1 - NPTRate);
            FinishOperationDate = EventDate.Start.AddDays(troubleFreeTime - 1);

            // Material Cost TLA
            MaterialCost = material;
            if (isTLA)
            {
                double matPerc = tlaMaterial > 1 ? Tools.Div(tlaMaterial, 100) : tlaMaterial;
                MaterialCost = MaterialCost + (matPerc * MaterialCost);
            }

            // Service Cost TLA
            ServicesCost = services;
            if (isTLA)
            {
                double sPerc = tlaServices > 1 ? Tools.Div(tlaServices, 100) : tlaServices;
                ServicesCost = ServicesCost + (sPerc * ServicesCost);
            }

            //var baseTime = troubleFreeTime + troubleFreeTime * NPTRate / (1 - NPTRate);
            //FinishOperationDate = EventDate.Start.AddDays(troubleFreeTime - 1);
            //MaterialCost = material;
            //ServicesCost = services;

            SetExchangeRates(baseop);
            SetRefFactorModels();
            SetMaterialEscalatedDate(monthRequired);
            #region SetMaterialLongLead
            MaterialLongLead = new FYLongLead();
            MaterialLongLead.MonthRequiredValue = monthRequired;
            MaterialLongLead.Year = eventPeriod.Start.Year;
            MaterialLongLead.TangibleValue = tangibleValue;

            // Tangible Value
            if (isTLA)
            {
                double tlaTangiblePerc = tlaTangible > 1 ? Tools.Div(tlaTangible, 100) : tlaTangible;
                MaterialLongLead.TangibleValue = MaterialLongLead.TangibleValue + (tlaTangiblePerc * MaterialLongLead.TangibleValue);
            }
            #endregion


            // SpreadRate Total
            double RigRateFromTla = 0;
            bool isSpread = false;
            if (isTLA && tlaSpreadrate != 0)
            {
                RigRateFromTla = ReverseCalculation(wellName, rigName, activityType, country, shellShare, eventPeriod, maturityLevel, services, material, troubleFreeTime, refFactorModel, NPTRate, TECOPRate, NPTRateCost, TECOPRateCost, monthRequired, tangibleValue,
                  tlaSpreadrate, currentSpreadRateTotal, meanTime);
                isSpread = true;
            }

            #region Rig, Services, Material
            var RigCostMonthly = GetRigCostMonthly(isSpread, RigRateFromTla);
            var ServicesCostMonthly = GetServicesCostMonthly();
            var MaterialCostMonthlyLL = GetMaterialCostLongLeadMonthly();

            var plusActive = TroubleFreeTime;
            eventPeriod.Finish = EventDate.Start.AddDays(Convert.ToInt32(plusActive) - 1);
            var MaterialCostMonthlyActive = GetMaterialCostActiveMonthly();

            #endregion

            List<FYValue> combine = new List<FYValue>();
            foreach (var y in MaterialCostMonthlyLL)
                combine.Add(y);

            foreach (var y in MaterialCostMonthlyActive)
            {
                var uu = combine.Where(x => x.Id == y.Id);
                if (uu.Any())
                {
                    DateTime start = uu.FirstOrDefault().Period.Start;
                    DateTime finish = y.Period.Finish;
                    combine.Where(x => x.Id == y.Id).FirstOrDefault().Value = y.Value + uu.FirstOrDefault().Value;
                    combine.Where(x => x.Id == y.Id).FirstOrDefault().Period = new DateRange(start, finish);
                    combine.Where(x => x.Id == y.Id).FirstOrDefault().Type = "LLActive";
                    combine.Where(x => x.Id == y.Id).FirstOrDefault().RigDays = RigCostMonthly.Where(x => x.Id == y.Id).ToList().Sum(x => x.RigDays);

                }
                else
                {
                    y.RigDays = RigCostMonthly.Where(x => x.Id == y.Id).ToList().Sum(x => x.RigDays);
                    combine.Add(y);

                }
            }

            #region NPT

            // new : NPT dan TECOP days dihitung di luar LL + Aktif, 
            // Period LL, NPT + Tecop saat ini, Start = Current Finish, Finish = Finish.addDays(tft)
            List<FYValue> NPTMonthly = new List<FYValue>();
            if (NPTRate > 0)
            {
                NPTDays = Tools.Div((TroubleFreeTime * NPTRate), (1 - NPTRate));
                var tft = NPTDays;
                var NPTRange = new DateRange(eventPeriod.Finish, eventPeriod.Finish.AddDays(tft));
                NPTMonthly = GetNPTMonthly(NPTRange);
                eventPeriod.Finish = EventDate.Finish.AddDays(Convert.ToInt32(tft));
            }


            if (NPTMonthly.Count > 0)
            {
                foreach (var y in NPTMonthly)
                {
                    var uu = combine.Where(x => x.Id == y.Id);
                    if (uu.Any())
                    {
                        DateTime start = uu.FirstOrDefault().Period.Start;
                        DateTime finish = y.Period.Finish;
                        //combine.Where(x => x.Id == y.Id).FirstOrDefault().Value = y.Value; // +uu.FirstOrDefault().Value;
                        combine.Where(x => x.Id == y.Id).FirstOrDefault().Period = new DateRange(start, finish);
                        combine.Where(x => x.Id == y.Id).FirstOrDefault().Type = "ActiveTECOPNPT";
                    }
                    else
                    {
                        var ty = new FYValue();
                        ty.Id = y.Id;
                        ty.Type = "ActiveTECOPNPT";
                        ty.Value = 0;
                        ty.Period = y.Period;
                        combine.Add(ty);

                    }
                }
            }
            #endregion

            List<BizPlanAllocationDetail> details = new List<BizPlanAllocationDetail>();
            //int ix = 0;
            int blnper = 0;
            foreach (var c in combine)
            {
                BizPlanAllocationDetail det = new BizPlanAllocationDetail();

                det.Key = c.Id.ToString();
                det.Period = c.Period;
                det.NoOfDays = c.Period.Days + 1;
                if (!c.Type.Equals("LL") && blnper == 0)
                {
                    det.MaterialCost = material * (1 - tangibleValue);
                    blnper++;
                }
                else if (c.Type.Equals("LL"))
                {
                    det.MaterialCost = c.Value;
                }
                //det.MaterialCost = c.Value;
                var rigAvail = RigCostMonthly.Where(x => x.Id == c.Id);
                if (rigAvail.Any())
                    det.RigCost = rigAvail.FirstOrDefault().Value;
                var serAvail = ServicesCostMonthly.Where(x => x.Id == c.Id);
                if (serAvail.Any())
                    det.ServiceCost = serAvail.FirstOrDefault().Value;

                // before 1 dec 2015
                var troubleFreeCost = det.MaterialCost + det.ServiceCost + det.RigCost;
                det.TroubleFreeCost = troubleFreeCost;

                // NPT days
                var nptAvail = NPTMonthly.Select(x => x.Id).Contains(c.Id);
                if (nptAvail)
                {
                    det.NPTDays = Tools.Div((troubleFreeTime * NPTRate), (1 - NPTRate));
                }

                details.Add(det);
            }

            Monthly = new List<BizPlanAllocationDetail>();
            Monthly = details;

            RigCostTotal = Monthly.Select(x => x.RigCost).Sum();
            MaterialCostTotal = Monthly.Select(x => x.MaterialCost).Sum();
            ServicesCostTotal = Monthly.Select(x => x.ServiceCost).Sum();
            TroubleFreeCost = Monthly.Select(x => x.TroubleFreeCost).Sum();
            var nptDaysTotal = Monthly.Select(x => x.NPTDays).Sum();


            if (isTLA && tlaSpreadrate != 0)
                RigRate = RigRateFromTla;
            else
                RigRate = Tools.Div(RigCostTotal, troubleFreeTime);

            //BurnRate = +Tools.Div(ServicesCostTotal + MaterialCostTotal + (RigRate * baseTime), baseTime);
            //SpreadRateWORig = Tools.Div(ServicesCostTotal, baseTime);
            //SpreadRateTotal = RigRate + SpreadRateWORig;

            //NPTDays = Monthly.Where(x => x.NPTDays != 0).FirstOrDefault().NPTDays;
            var isNotZero = Monthly.Where(x => x.NPTDays != 0);
            if (isNotZero.Any())
            {
                NPTDays = Monthly.Where(x => x.NPTDays != 0).FirstOrDefault().NPTDays;
            }
            NPTCost = (TroubleFreeCost - MaterialCostTotal) * Tools.Div(NPTRateCost, 1 - NPTRateCost);
            TECOPCost = TECOPRateCost * (TroubleFreeCost + NPTCost);
            //TECOPDays = TECOPRate * (baseTime * Tools.Div(SpreadRateTotal, BurnRate));
            TECOPDays = TECOPRate * (troubleFreeTime + NPTDays) * Tools.Div((TroubleFreeCost + NPTCost - material), (TroubleFreeCost + NPTCost));
            //MeanTime = baseTime + TECOPDays;
            MeanTime = troubleFreeTime + TECOPDays + NPTDays;
            var newFInish = EventDate.Finish.AddDays(TECOPDays);

            if (!combine.Max(x => x.Id).ToString().Equals(newFInish.ToString("yyyyMM")))
            {
                var newDr = new DateRange(EventDate.Finish, newFInish);
                var dayPerMonth = DateRangeToMonth.NumDaysPerMonth(newDr);

                foreach (var dpm in dayPerMonth)
                {
                    var dd = details.Where(x => x.Key == dpm.Key.ToString());
                    BizPlanAllocationDetail det = null;

                    var tecop = Tools.Div(dpm.Value.FirstOrDefault().Value, TECOPDays, 3, 1) * TECOPDays;


                    if (dd.Any())
                    {
                        det = dd.FirstOrDefault();
                        det.TECOPDays = tecop;
                    }
                    else
                    {
                        det = new BizPlanAllocationDetail();
                        det.TECOPDays = tecop;
                        det.Key = dpm.Key.ToString();
                        details.Add(det);
                        combine.Add(new FYValue
                        {
                            Id = dpm.Key,
                            Period = dpm.Value.FirstOrDefault().Key,
                            Value = tecop, //Tools.Div(dpm.Value.FirstOrDefault().Value, TECOPDays, 3, 1) * TECOPCost,
                            Type = "TECOP"
                        });
                    }
                }

            }
            else
            {
                var dd = details.Where(x => x.Key == newFInish.ToString("yyyyMM"));// b.Key.ToString());

                var daysno = combine.Where(x => x.Id == Convert.ToInt32(newFInish.ToString("yyyyMM"))).Select(x => x.Value).FirstOrDefault();
                BizPlanAllocationDetail det = null;
                var tecop = Tools.Div(daysno, TECOPDays, 3, 1) * TECOPDays;
                if (dd.Any())
                {
                    det = dd.FirstOrDefault();
                    det.TECOPDays = TECOPDays;
                }
                else
                {
                    det = new BizPlanAllocationDetail();
                    det.TECOPDays = tecop;
                    det.Period = combine.Where(x => x.Id == Convert.ToInt32(newFInish.ToString("yyyyMM"))).FirstOrDefault().Period;
                    det.Key = newFInish.ToString("yyyyMM"); // dd.FirstOrDefault().Key;
                    details.Add(det);
                }
            }

            #region Escalation Calculation

            var operationFinish = eventPeriod.Start.AddDays(TroubleFreeTime - 1);
            var validPeriod = new DateRange(eventPeriod.Start, operationFinish);
            double durationRemain = MeanTime;
            bool startComsumpt = false;
            foreach (var c in combine.OrderBy(x => x.Id))
            {
                var dd = details.Where(x => x.Key == c.Id.ToString());
                var det = dd.FirstOrDefault();
                det.RigDays = c.RigDays;
                det.TECOPDays = 0;
                det.NPTDays = 0;
                if (det.RigDays > 0)
                {
                    startComsumpt = true;
                    durationRemain -= det.RigDays;
                }
                if (startComsumpt)
                {
                    if (durationRemain > 0)
                    {
                        if (durationRemain > det.NPTDays)
                            durationRemain -= det.NPTDays;
                        else
                            det.NPTDays = durationRemain;
                    }

                    if (durationRemain > 0)
                    {
                        if (durationRemain > det.TECOPDays)
                            durationRemain -= det.TECOPDays;
                        else
                            det.TECOPDays = durationRemain;
                    }
                }
            }

            if (durationRemain > 0)
            {
                var det = details.OrderByDescending(d => d.Key).FirstOrDefault();
                det.TECOPDays += durationRemain;
            }
            double dummyCheck = details.Sum(d => d.RigDays + d.NPTDays + d.TECOPDays) - MeanTime;

            var meancostEDMRigRates = Tools.Div(((RigRate * troubleFreeTime) +
                ((RigRate * troubleFreeTime) * (NPTRateCost / (1 - NPTRateCost))) +
                ((RigRate * troubleFreeTime) + ((RigRate * troubleFreeTime) * (NPTRateCost / (1 - NPTRateCost)))) * TECOPRateCost), MeanTime);

            DateRange hitAllDay = new DateRange(combine.OrderBy(x => x.Period.Start).Select(x => x.Period.Start).FirstOrDefault(),
                combine.OrderByDescending(x => x.Period.Finish).Select(x => x.Period.Finish).FirstOrDefault()
                );
            int blnpertama = 0;
            double totalActiveDays = MeanTime;

            foreach (var c in combine)
            {
                var dd = details.Where(x => x.Key == c.Id.ToString());
                var det = dd.FirstOrDefault();

                det.NPTCost = Tools.Div(((det.NPTDays + det.TECOPDays + det.RigDays) * NPTCost), MeanTime);
                if (c.Type.Equals("LL"))
                    det.TECOPCost = TECOPRateCost * det.MaterialCost;
                else
                    det.TECOPCost = Tools.Div(((det.NPTDays + det.TECOPDays + det.RigDays) * TECOPCost), MeanTime);

                // Mean Cost EDM
                det.MeanCostEDM = det.TroubleFreeCost + det.NPTCost + det.TECOPCost;

                // rig Escal 
                CalcRigCost(isTLA, RigRateFromTla);
                if (det.RigDays > 0)
                {
                    var cosmon = RigCostMonthly.Where(x => x.Id == c.Id);
                    if (cosmon.Any())
                    {
                        //if (ix > 0)
                        //{
                        //old
                        //var rigEscCost = cosmon.FirstOrDefault().Value - (det.RigDays * FirstRate);

                        var RigRateNowYear = RigCostAnnualy.Where(x => x.Id.ToString().Contains(DateTime.Now.Year.ToString()));
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
                                        bool isInRange = DateRangeToMonth.isDateInsideEqualofRange(c.Period.Start, idl.Period);
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
                                        bool isInRange = DateRangeToMonth.isDateInsideEqualofRange(c.Period.Start, idl.Period);
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

                        var rigEscCost = (det.NoOfDays * meancostEDMRigRates) * (Tools.Div((rateSpecNow - rateNow), rateNow));
                        det.EscalationCostEDMRig = rigEscCost;
                        //}
                        //ix++;
                    }
                    else
                        det.EscalationCostEDMRig = 0;
                }


                // services escal
                var servicRefModel = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Service Escalation Factors"));
                double serviceEscVal = 0;
                if (servicRefModel.Any())
                {
                    var ess = servicRefModel.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + c.Id.ToString().Substring(0, 4)));
                    if (ess.Any())
                        serviceEscVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                }
                // new service escal
                if (c.Type.Contains("Active"))
                {

                    if (c.Type.Equals("ActiveTECOPNPT"))
                    {
                        //DateRange activeDate = new DateRange(c.Period.Start, eventPeriod.Finish.Date);
                        var calcSer = Tools.Div(totalActiveDays, MeanTime) * serviceEscVal * (((services + (services * (Tools.Div(NPTRateCost, (1 - NPTRateCost))) +
                     TECOPRateCost * ((services + (services * (Tools.Div(NPTRateCost, (1 - NPTRateCost))))))))));

                        det.ActiveDays = totalActiveDays;
                    }
                    else
                    {
                        var calcSer = Tools.Div(c.Period.Days + 1, MeanTime) * serviceEscVal * (((services + (services * (Tools.Div(NPTRateCost, (1 - NPTRateCost))) +
                       TECOPRateCost * ((services + (services * (Tools.Div(NPTRateCost, (1 - NPTRateCost))))))))));
                        det.EscalationCostEDMServices = calcSer;

                        det.ActiveDays = c.Period.Days + 1;
                    }
                    totalActiveDays = totalActiveDays - (c.Period.Days + 1);


                }
                //det.EscalationCostEDMServices = det.ServiceCost * serviceEscVal;

                // material escal
                var matRefModel = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Material Escalation Factors"));
                double matescval = 0; // compound mat esc value annualy
                if (matRefModel.Any())
                {
                    var ess = matRefModel.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + c.Id.ToString().Substring(0, 4)));
                    if (ess.Any())
                        matescval = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                }

                // modif material esc
                if (c.Type.Contains("Active"))
                {
                    if (blnpertama == 0)
                    {

                        var calc2 = matescval * (material * (1 - tangibleValue)) * (1 + TECOPRateCost);
                        // var MatescModif = c.Value * matescval * (1 + TECOPRateCost);
                        det.EscalationCostEDMMaterial = calc2;
                        blnpertama++;

                    }
                }
                else if (c.Type.Contains("LL"))
                {
                    var MatescModif = c.Value * matescval * (1 + TECOPRateCost);
                    det.EscalationCostEDMMaterial = MatescModif;
                    det.isLL = true;
                }

                // old before update formula 07/12/2015
                //var matper = matescval * c.Value;
                //det.EscalationCostEDMMaterial = matper;

                // total esc
                det.EscalationCostEDMTotal = det.EscalationCostEDMMaterial + det.EscalationCostEDMRig + det.EscalationCostEDMServices;

                // CSO 
                var csoFc = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("CSO Factors"));
                double csoFcVal = 0;
                if (matRefModel.Any())
                {
                    var ess = csoFc.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + c.Id.ToString().Substring(0, 4)));
                    if (ess.Any())
                        csoFcVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                }

                // CSO Cost EDM
                det.CSOCostEDM = csoFcVal * (det.MeanCostEDM + det.EscalationCostEDMTotal);

                // Inflation 
                var inf = RefFactorModels.SubjectMatters.Where(x => x.Key.Equals("Inflation Factors"));
                double infVal = 0;
                if (matRefModel.Any())
                {
                    var ess = inf.FirstOrDefault().Value.Where(x => x.Key.Equals("Year_" + c.Id.ToString().Substring(0, 4)));
                    if (ess.Any())
                        infVal = ess.FirstOrDefault().Value > 1 ? Tools.Div(ess.FirstOrDefault().Value, 100) : ess.FirstOrDefault().Value;
                }

                // Inflation Cost EDM
                det.InflationCostEDM = infVal * (det.MeanCostEDM + det.EscalationCostEDMTotal + det.CSOCostEDM);

                // Mean Cost MOD
                det.MeanCostMOD = det.MeanCostRealTerm + det.CSOCostEDM + det.InflationCostEDM;

                // Mean Cost Real Term
                //det.MeanCostRealTerm = det.MeanCostEDM + det.EscalationCostEDMTotal; -> old
                det.MeanCostRealTerm = Tools.Div(det.MeanCostMOD, (1 + infVal)); // -> Mean Cost Real Terms = Mean Cost MOD / (1 + inflation rate)

                // Shell share 
                det.ShellShareMOD = ShellShare * det.MeanCostMOD;
            }

            #endregion
            TECOPCost = Monthly.Select(x => x.TECOPCost).Sum();
            BurnRate = Tools.Div(details.Sum(x => x.MeanCostEDM), MeanTime); //+Tools.Div(ServicesCostTotal + MaterialCostTotal + (RigRate * baseTime), baseTime);
            //SpreadRateWORig = Tools.Div(ServicesCostTotal, baseTime);
            SpreadRateWORig = Tools.Div((1 + TECOPRateCost) * (services + ((services * (Tools.Div(NPTRateCost, (1 - NPTRateCost)))
                ))), MeanTime);
            //Tools.Div(ServicesCostTotal, baseTime);
            //SpreadRateTotal = RigRate + SpreadRateWORig;

            RigEscCost = Monthly.Select(x => x.EscalationCostEDMRig).Sum();
            EscMaterialCost = Monthly.Select(x => x.EscalationCostEDMMaterial).Sum();
            EscServicesCost = Monthly.Select(x => x.EscalationCostEDMServices).Sum();

            CSOCost = Monthly.Select(x => x.CSOCostEDM).Sum();
            InflationCost = Monthly.Select(x => x.InflationCostEDM).Sum();

            MeanCostEDM = Monthly.Select(x => x.MeanCostEDM).Sum();

            SpreadRateTotal = Tools.Div((MeanCostEDM - (material + (material * TECOPRateCost))), MeanTime);//RigRate + SpreadRateWORig;

            MeanCostMOD = Monthly.Select(x => x.MeanCostMOD).Sum();
            MeanCostRealTerm = Monthly.Select(x => x.MeanCostRealTerm).Sum();
            ShellShareCost = Monthly.Select(x => x.ShellShareMOD).Sum();

            double rate = 0;
            List<double> rateEx = new List<double>();

            for (int i = eventPeriod.Start.Year; i <= eventPeriod.Finish.Year; i++)
            {
                var ada = ExchangeRates.Where(x => x.Key == i);
                if (ada.Any())
                    rateEx.Add(ada.FirstOrDefault().Value);
            }

            if (rateEx.Count == 0) rate = 1; else rate = rateEx.Average();

            TroubleFreeCostUSD = TroubleFreeCost * rate;
            NPTCostUSD = NPTCost * rate;
            TECOPCostUSD = TECOPCost * rate;
            MeanCostEDMUSD = MeanCostEDM * rate;

            SetAnnualy();
            SetQuarter();

            EventDate.Finish = newFInish;

        }



        public List<BizPlanAllocationDetail> Annualy { get; set; }

        public List<BizPlanAllocationDetail> Monthly { get; set; }

        public static List<BizPlanAllocationDetail> SetQuarter(List<BizPlanAllocationDetail> Monthlyx)
        {
            #region Quarter

            List<BizPlanAllocationDetail> quarter = new List<BizPlanAllocationDetail>();
            foreach (var year in Monthlyx)
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
            List<BizPlanAllocationDetail> Qurter = new List<BizPlanAllocationDetail>();
            foreach (var year in Monthlyx.GroupBy(x => x.Key.ToString()))
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

                Qurter.Add(ann);
            }
            return Qurter;
            #endregion
        }

        public void SetAnnualy()
        {
            Annualy = new List<BizPlanAllocationDetail>();

            #region Annualy
            List<BizPlanAllocationDetail> annually = new List<BizPlanAllocationDetail>();
            foreach (var year in Monthly.GroupBy(x => x.Key.ToString().Substring(0, 4)))
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


                ann.SpreadRateWORig = year.ToList().Sum(x => x.SpreadRateWORig);
                ann.SpreadRateTotal = year.ToList().Sum(x => x.SpreadRateTotal);
                ann.BurnRate = year.ToList().Sum(x => x.BurnRate);

                ann.TECOPCost = year.ToList().Sum(x => x.TECOPCost);
                ann.NPTCost = year.ToList().Sum(x => x.NPTCost);


                Annualy.Add(ann);
            }
            #endregion

        }

        public void SetQuarter()
        {
            List<BizPlanAllocationDetail> detemp = new List<BizPlanAllocationDetail>();
            foreach (var mon in Monthly)
            {
                BizPlanAllocationDetail m = new BizPlanAllocationDetail();
                m.CSOCostEDM = mon.CSOCostEDM;
                m.EscalationCostEDMMaterial = mon.EscalationCostEDMMaterial;
                m.EscalationCostEDMRig = mon.EscalationCostEDMRig;
                m.EscalationCostEDMServices = mon.EscalationCostEDMServices;
                m.EscalationCostEDMTotal = mon.EscalationCostEDMTotal;
                m.InflationCostEDM = mon.InflationCostEDM;
                m.isMaterialAdd = mon.isMaterialAdd;
                m.Key = mon.Key;
                m.MaterialCost = mon.MaterialCost;
                m.MeanCostEDM = mon.MeanCostEDM;
                m.MeanCostMOD = mon.MeanCostMOD;
                m.MeanCostRealTerm = mon.MeanCostRealTerm;
                m.NoOfDays = mon.NoOfDays;
                m.Period = mon.Period;
                m.Proportion = mon.Proportion;
                m.RigCost = mon.RigCost;
                m.ServiceCost = mon.ServiceCost;
                m.ShellShareMOD = mon.ShellShareMOD;
                m.TroubleFreeCost = mon.TroubleFreeCost;

                m.SpreadRateWORig = mon.SpreadRateWORig; //year.ToList().Sum(x => x.SpreadRateWORig);
                m.SpreadRateTotal = mon.SpreadRateTotal;//year.ToList().Sum(x => x.SpreadRateTotal);
                m.BurnRate = mon.BurnRate; // year.ToList().Sum(x => x.BurnRate);

                m.TECOPCost = mon.TECOPCost;//year.ToList().Sum(x => x.TECOPCost);
                m.NPTCost = mon.NPTCost; //year.ToList().Sum(x => x.NPTCost);

                detemp.Add(m);
            }

            Quarterly = SetQuarter(detemp);
        }

        public void SetMonthly()
        {
            Monthly = new List<BizPlanAllocationDetail>();
            Quarterly = new List<BizPlanAllocationDetail>();
            Annualy = new List<BizPlanAllocationDetail>();
            DateTime eventStartDate = EventDate.Start.Date;
            DateTime eventEndDate = EventDate.Finish.Date;
            DateTime materialShiftDate = MaterialLeadDate.Date; // GetMaterialShiftDate(p.ActivityType, eventStartDate, out percentTangible, out monthlonglead);

            if (materialShiftDate <= DateTime.Now.Date)
                materialShiftDate = DateTime.Now.Date;

            if (eventStartDate <= DateTime.Now)
                eventStartDate = DateTime.Now;

            DateRange fullMonthPeriod = new DateRange(materialShiftDate, eventEndDate);

            // cross dalam 1 bulan (shift dan material)

            #region monthly

            var fullMonths = DateRangeToMonth.NumDaysPerMonth(fullMonthPeriod);
            double TotalDay = fullMonthPeriod.Days + 1;
            List<BizPlanAllocationDetail> details = new List<BizPlanAllocationDetail>();
            foreach (var u in fullMonths)
            {
                BizPlanAllocationDetail det = new BizPlanAllocationDetail();
                det.EscalationCostEDMTotal = det.EscalationCostEDMMaterial + det.EscalationCostEDMRig + det.EscalationCostEDMServices;
                det.Key = u.Key.ToString();

                if (RigCostMonthly.Where(x => x.Id == u.Key).Any())
                    det.RigCost = RigCostMonthly.Where(x => x.Id == u.Key).Sum(x => x.Value);
                det.NoOfDays = u.Value.FirstOrDefault().Value;
                det.Period = u.Value.FirstOrDefault().Key;
                det.Proportion = Tools.Div(u.Value.FirstOrDefault().Value, TotalDay);
                Monthly.Add(det);
            }



            #region Annualy
            List<BizPlanAllocationDetail> annually = new List<BizPlanAllocationDetail>();
            foreach (var year in Monthly.GroupBy(x => x.Key.ToString().Substring(0, 4)))
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

                Annualy.Add(ann);
            }
            #endregion

            List<BizPlanAllocationDetail> detemp = new List<BizPlanAllocationDetail>();
            foreach (var mon in Monthly)
            {
                BizPlanAllocationDetail m = new BizPlanAllocationDetail();
                m.CSOCostEDM = mon.CSOCostEDM;
                m.EscalationCostEDMMaterial = mon.EscalationCostEDMMaterial;
                m.EscalationCostEDMRig = mon.EscalationCostEDMRig;
                m.EscalationCostEDMServices = mon.EscalationCostEDMServices;
                m.EscalationCostEDMTotal = mon.EscalationCostEDMTotal;
                m.InflationCostEDM = mon.InflationCostEDM;
                m.isMaterialAdd = mon.isMaterialAdd;
                m.Key = mon.Key;
                m.MaterialCost = mon.MaterialCost;
                m.MeanCostEDM = mon.MeanCostEDM;
                m.MeanCostMOD = mon.MeanCostMOD;
                m.MeanCostRealTerm = mon.MeanCostRealTerm;
                m.NoOfDays = mon.NoOfDays;
                m.Period = mon.Period;
                m.Proportion = mon.Proportion;
                m.RigCost = mon.RigCost;
                m.ServiceCost = mon.ServiceCost;
                m.ShellShareMOD = mon.ShellShareMOD;
                m.TroubleFreeCost = mon.TroubleFreeCost;
                detemp.Add(m);
            }

            Quarterly = SetQuarter(detemp);

            #endregion

        }

        public List<BizPlanAllocationDetail> Quarterly { get; set; }

        public void SetMaterialEscalatedDate(double monthrequired)
        {
            //var matLongLead = MaterialLongLeads.Details.Where(x => x.Year == EventDate.Start.Year);
            double monthReq = monthrequired;
            //if (matLongLead.Any())
            //    monthReq = matLongLead.FirstOrDefault().MonthRequiredValue;

            var LeadDate = EventDate.Start.AddMonths(-1 * (Convert.ToInt32(monthReq)));
            MaterialLeadDate = new DateTime();

            if (NowDate.Date >= EventDate.Start.Date)
            {
                EventDate.Start = NowDate.Date;
                MaterialLeadDate = NowDate.Date;
            }
            else
            {
                if (LeadDate.Date > NowDate.Date)
                {
                    // hitung full leadDate 
                    MaterialLeadDate = LeadDate.Date;
                }
                else
                {
                    LeadDate = NowDate.Date;
                    MaterialLeadDate = LeadDate.Date;
                }
            }
        }

        public List<FYValue> GetMaterialCostActiveMonthly()
        {
            var materialCostActiveMonthly = new List<FYValue>();
            // 0.7
            var percentage = 1 - MaterialLongLead.TangibleValue;

            var dayPerMonth = DateRangeToMonth.NumDaysPerMonth(new DateRange(EventDate.Start.Date, FinishOperationDate));
            var totalDay = dayPerMonth.Sum(x => x.Value.Select(y => y.Value).ToList().Sum());
            foreach (var t in dayPerMonth)
            {
                double perCent = 0;
                var totDays = t.Value.Sum(x => x.Value);
                perCent = Tools.Div(totDays, totalDay);
                materialCostActiveMonthly.Add(
                    new FYValue
                    {
                        Id = t.Key,
                        Value = perCent * MaterialCost * percentage,
                        Period = t.Value.FirstOrDefault().Key,
                        Type = "Active"
                    });
            }
            return materialCostActiveMonthly;
        }

        public List<FYValue> GetMaterialCostLongLeadMonthly()
        {
            // Long Lead Only 
            DateTime masterialLeadDate = MaterialLeadDate.Date;
            DateTime startDate = EventDate.Start.Date;
            var materialLongLeadCostOnlyMonthly = new List<FYValue>();
            var ll = new FYValue();
            ll.Id = Convert.ToInt32(masterialLeadDate.ToString("yyyyMM"));
            ll.Value = MaterialLongLead.TangibleValue * MaterialCost;
            ll.Period = new DateRange(masterialLeadDate, startDate);
            ll.Type = "LL";

            materialLongLeadCostOnlyMonthly.Add(ll);
            return materialLongLeadCostOnlyMonthly;

        }

        public List<FYValue> GetServicesCostMonthly()
        {
            var servicesCostMonthly = new List<FYValue>();
            var dayPerMonth = DateRangeToMonth.NumDaysPerMonth(new DateRange(EventDate.Start.Date, FinishOperationDate.Date));
            var totalDay = dayPerMonth.Sum(x => x.Value.Select(y => y.Value).ToList().Sum());
            foreach (var t in dayPerMonth)
            {
                double perCent = 0;
                var totDays = t.Value.Sum(x => x.Value);
                perCent = Tools.Div(totDays, totalDay);
                servicesCostMonthly.Add(
                    new FYValue
                    {
                        Id = t.Key,
                        Value = perCent * ServicesCost,
                        Period = t.Value.FirstOrDefault().Key,
                        Type = "Services"
                    });
            }

            return servicesCostMonthly;
        }

        public List<FYValue> GetNPTMonthly(DateRange range)
        {
            var bucketMonthly = new List<FYValue>();
            var dayPerMonth = DateRangeToMonth.NumDaysPerMonth(range);
            var totalDay = dayPerMonth.Sum(x => x.Value.Select(y => y.Value).ToList().Sum());
            foreach (var t in dayPerMonth)
            {
                double perCent = 0;
                var totDays = t.Value.Sum(x => x.Value);
                perCent = Tools.Div(totDays, totalDay);
                bucketMonthly.Add(
                    new FYValue
                    {
                        Id = t.Key,
                        Value = perCent * ServicesCost,
                        Period = t.Value.FirstOrDefault().Key,
                        Type = "NPT"
                    });
            }

            return bucketMonthly;
        }

        public List<FYValue> GetRigCostMonthly(bool isTLA = false, double RigRateTla = 0)
        {
            // get rigrate new 
            var rigRates = RigRatesNew.Populate<RigRatesNew>(Query.EQ("Title", RigName));

            Dictionary<DateTime, double> rigPerDays = new Dictionary<DateTime, double>();
            #region Calculate Rate Per Day
            for (DateTime i = EventDate.Start; i <= FinishOperationDate; i = i.AddDays(1))
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
                                    //rigPerDays.Add(i, rateThisDay);
                                    break;
                                }
                                else
                                {
                                    rateThisDay = 0;
                                }
                            }
                            rigPerDays.Add(i, rateThisDay);
                        }
                    }
                }
                else
                {
                    // active rate
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
                    }
                }
            }

            #endregion
            // calculate 


            var rigRate = Tools.Div(rigPerDays.Sum(x => x.Value), rigPerDays.Count());

            if (isTLA)
            {
                rigRate = RigRateTla;
            }


            var rigCostMonthly = new List<FYValue>();
            foreach (var bln in rigPerDays.GroupBy(x => Convert.ToInt32(x.Key.ToString("yyyyMMdd").Substring(0, 6))))
            {
                if (isTLA)
                {
                    DateRange dr = new DateRange(bln.ToList().Select(x => x.Key.Date).Min(), bln.ToList().Select(x => x.Key.Date).Max());
                    rigCostMonthly.Add(new FYValue
                    {
                        Id = bln.Key,
                        Period = dr,
                        Value = (dr.Days + 1) * RigRateTla,
                        Type = "Active",
                        RigDays = bln.Count()
                    });
                }
                else
                {
                    rigCostMonthly.Add(new FYValue
                    {
                        Id = bln.Key,
                        Period = new DateRange(bln.ToList().Select(x => x.Key.Date).Min(), bln.ToList().Select(x => x.Key.Date).Max()),
                        Value = bln.Sum(x => x.Value),
                        Type = "Active",
                        RigDays = bln.Count()
                    });
                }

            }
            return rigCostMonthly;
        }

        public void SetRigCostAnnual()
        {
            Dictionary<int, double> rigTahunan = new Dictionary<int, double>();

            // get rigrate new 
            var rigRates = RigRatesNew.Populate<RigRatesNew>(Query.EQ("Title", RigName));

            Dictionary<DateTime, double> rigPerDays = new Dictionary<DateTime, double>();
            RigCostAnnualy = new List<FYValue>();
            foreach (var bln in rigPerDays.GroupBy(x => Convert.ToInt32(x.Key.ToString("yyyyMMdd").Substring(0, 4))))
            {
                RigCostAnnualy.Add(new FYValue
                {
                    Id = bln.Key,
                    Period = new DateRange(bln.ToList().Select(x => x.Key.Date).Min(), bln.ToList().Select(x => x.Key.Date).Max()),
                    Value = bln.Sum(x => x.Value)
                });
            }

            RigEscCostAnnualy = new List<RigEscDetail>();
            int awal = 0;
            FYValue prev = new FYValue();
            foreach (var tahunan in RigCostAnnualy.OrderBy(x => x.Id))
            {
                if (awal == 0)
                {
                    prev = tahunan;
                    RigEscCostAnnualy.Add(new RigEscDetail
                    {
                        Id = tahunan.Id,
                        Period = tahunan.Period, // new DateRange(tahunan.ToList().Select(x => x.Key.Date).Min(), bln.ToList().Select(x => x.Key.Date).Max()),
                        Value = 0,
                        Percent = 0
                    });
                }
                else
                {

                    var valueNow = Tools.Div(tahunan.Value, tahunan.Period.Days + 1);
                    var yearBefore = Tools.Div(prev.Value, prev.Period.Days + 1);

                    var percentNow = (Tools.Div(valueNow, yearBefore) - 1);
                    RigEscCostAnnualy.Add(new RigEscDetail
                    {
                        Id = tahunan.Id,
                        Period = tahunan.Period, // new DateRange(tahunan.ToList().Select(x => x.Key.Date).Min(), bln.ToList().Select(x => x.Key.Date).Max()),
                        Value = percentNow * tahunan.Value,
                        Percent = percentNow * 100
                    });
                    prev = tahunan;
                }
                awal++;
            }
        }

        public void SetExchangeRates(string baseop)
        {
            Dictionary<int, double> exchangeRates = new Dictionary<int, double>();
            for (int i = EventDate.Start.Year; i <= EventDate.Finish.Year; i++)
            {
                exchangeRates.Add(i, BizPlanAllocation.GetExchangeRate(Country, i, baseop));
            }
            ExchangeRates = exchangeRates;
        }


        public void SetRefFactorModels()
        {
            RefFactorModels = new WEISReferenceFactorModel();
            var y = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(Query.EQ("GroupCase", RefFactorTitle));
            RefFactorModels = y;
        }

        public void CalcRigCost(bool isTla = false, double rigRateTla = 0)
        {
            Dictionary<int, double> rigTahunan = new Dictionary<int, double>();

            // get rigrate new 
            var rigRates = RigRatesNew.Populate<RigRatesNew>(Query.EQ("Title", RigName));

            Dictionary<DateTime, double> rigPerDays = new Dictionary<DateTime, double>();

            #region Calculate Rate Per Day
            for (DateTime i = EventDate.Start; i <= EventDate.Finish; i = i.AddDays(1))
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
                            int x = 0;
                            foreach (var y in idleRates)
                            {
                                var isInRange = DateRangeToMonth.isDateInsideEqualofRange(i, y.Period);
                                if (isInRange)
                                {
                                    if (FirstRate == 0)
                                    {
                                        FirstRate = y.Value;
                                    }
                                    rateThisDay = y.Value;
                                    //rigPerDays.Add(i, rateThisDay);
                                    break;
                                }
                                else
                                {
                                    rateThisDay = 0;
                                }
                                x++;
                            }
                            rigPerDays.Add(i, rateThisDay);
                        }
                    }
                }
                else
                {
                    // active rate
                    if (rigRates.Any())
                    {
                        var idleRates = rigRates.FirstOrDefault().Values.Where(X => !X.Type.Equals("IDLE"));
                        if (idleRates.Any())
                        {
                            double rateThisDay = 0;
                            int x = 0;
                            foreach (var y in idleRates)
                            {
                                var isInRange = DateRangeToMonth.isDateInsideEqualofRange(i, y.Period);
                                if (isInRange)
                                {
                                    if (FirstRate == 0)
                                    {
                                        FirstRate = y.Value;

                                    }
                                    rateThisDay = y.Value;
                                    break;
                                }
                                else
                                    rateThisDay = 0;
                                x++;
                            }
                            rigPerDays.Add(i, rateThisDay);
                        }
                    }
                }
            }

            #endregion
            // calcigulate 

            if (isTla)
            {
                RigRate = rigRateTla;
            }
            else
            {
                RigRate = Tools.Div(rigPerDays.Sum(x => x.Value), rigPerDays.Count());
            }

            RigCostMonthly = new List<FYValue>();
            foreach (var bln in rigPerDays.GroupBy(x => Convert.ToInt32(x.Key.ToString("yyyyMMdd").Substring(0, 6))))
            {
                RigCostMonthly.Add(new FYValue
                {
                    Id = bln.Key,
                    Period = new DateRange(bln.ToList().Select(x => x.Key.Date).Min(), bln.ToList().Select(x => x.Key.Date).Max()),
                    Value = bln.Sum(x => x.Value),
                    RigDays = bln.Count()
                });
            }

            if (isTla)
            {
                RigCost = rigPerDays.Count() * rigRateTla;
            }
            else
            {
                RigCost = rigPerDays.Sum(x => x.Value);
            }


            // rig cost annual without esc  
            RigCostAnnualy = new List<FYValue>();
            foreach (var bln in rigPerDays.GroupBy(x => Convert.ToInt32(x.Key.ToString("yyyyMMdd").Substring(0, 4))))
            {
                RigCostAnnualy.Add(new FYValue
                {
                    Id = bln.Key,
                    Period = new DateRange(bln.ToList().Select(x => x.Key.Date).Min(), bln.ToList().Select(x => x.Key.Date).Max()),
                    Value = bln.Sum(x => x.Value)
                });
            }

            // calc esc only 
            RigEscCostAnnualy = new List<RigEscDetail>();
            int awal = 0;
            FYValue prev = new FYValue();
            foreach (var tahunan in RigCostAnnualy.OrderBy(x => x.Id))
            {
                if (awal == 0)
                {
                    prev = tahunan;
                    RigEscCostAnnualy.Add(new RigEscDetail
                    {
                        Id = tahunan.Id,
                        Period = tahunan.Period, // new DateRange(tahunan.ToList().Select(x => x.Key.Date).Min(), bln.ToList().Select(x => x.Key.Date).Max()),
                        Value = 0,
                        Percent = 0
                    });
                }
                else
                {

                    var valueNow = Tools.Div(tahunan.Value, tahunan.Period.Days + 1);
                    var yearBefore = Tools.Div(prev.Value, prev.Period.Days + 1);

                    var percentNow = (Tools.Div(valueNow, yearBefore) - 1);
                    RigEscCostAnnualy.Add(new RigEscDetail
                    {
                        Id = tahunan.Id,
                        Period = tahunan.Period, // new DateRange(tahunan.ToList().Select(x => x.Key.Date).Min(), bln.ToList().Select(x => x.Key.Date).Max()),
                        Value = percentNow * tahunan.Value,
                        Percent = percentNow * 100
                    });
                    prev = tahunan;
                }
                awal++;
            }

            // calc rig Cost + Esc
            RigCostAnnualyWithEsc = new List<RigCostTotalDetail>();
            foreach (var rWithNoEsc in RigCostAnnualy)
            {
                RigCostAnnualyWithEsc.Add(new RigCostTotalDetail
                {
                    Id = rWithNoEsc.Id,
                    Period = rWithNoEsc.Period, // new DateRange(tahunan.ToList().Select(x => x.Key.Date).Min(), bln.ToList().Select(x => x.Key.Date).Max()),
                    Value = RigEscCostAnnualy.Where(x => x.Id == rWithNoEsc.Id).FirstOrDefault().Value +
                    rWithNoEsc.Value,
                    EscCost = RigEscCostAnnualy.Where(x => x.Id == rWithNoEsc.Id).FirstOrDefault().Value,
                    Original = rWithNoEsc.Value
                });
            }

            TotalRigCostPlusEsc = RigCostAnnualyWithEsc.Sum(x => x.Value);
        }


    }

    public class RigCostTotalDetail : FYValue
    {
        public double EscCost { get; set; }
        public double Original { get; set; }
    }
    public class FYValue
    {
        public int Id { get; set; }
        public DateRange Period { get; set; }
        public double Value { get; set; }
        public string Type { get; set; }
        public double RigDays { get; set; }
    }
    public class RigEscDetail : FYValue
    {
        public double Percent { get; set; }
    }
    public class MaterialDetail : FYValue
    {
        public double TangibleValue { get; set; }
        public double MaterialCost { get; set; }
    }
    public class ServiceDetail : FYValue
    {
        public double ServiceEsc { get; set; }
        public double ServiceCost { get; set; }
    }
    public class RigDetail : FYValue
    {
        public double RigEsc { get; set; }
    }
}
