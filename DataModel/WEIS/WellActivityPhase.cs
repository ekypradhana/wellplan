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
    public class WellActivityPhase
    {
        public int SequenceOnRig { get; set; }

        public WellActivityPhase()
        {
        }
        private List<string> _baseop;
        public List<string> BaseOP
        {
            get
            {
                if (_baseop == null) _baseop = new List<string>();
                return _baseop;
            }
            set { _baseop = value; }
        }

        [BsonIgnore]
        public WellDrillData PreviousOP { get; set; }

        [BsonIgnore]
        public DateRange PreviousOPSchedule { get; set; }


        [BsonIgnore]
        public Dictionary<int, double> AnnualProportions { get; set; }

        [BsonIgnore]
        public WellActivityPhaseInfo PhaseInfo { get; set; }

        public bool IsInPlan { set; get; }

        [BsonIgnore]
        public double Ratio { set; get; }
        [BsonIgnore]
        public double RatioLS { set; get; }
        [BsonIgnore]
        public double RatioLE { set; get; }
        [BsonIgnore]
        public WellDrillData BankedSavingsSupplyChainTransformation { set; get; }
        [BsonIgnore]
        public WellDrillData BankedSavingsCompetitiveScope { set; get; }
        [BsonIgnore]
        public WellDrillData BankedSavingsTechnologyAndInnovation { set; get; }
        [BsonIgnore]
        public WellDrillData BankedSavingsEfficientExecution { set; get; }
        [BsonIgnore]
        public List<OPListHelperForDataBrowserGrid> OPList { get; set; }

        public int PhaseNo { get; set; }
        public string ActivityType { get; set; }
        public string ActivityDesc { get; set; }
        public string RiskFlag { get; set; }
        public string ActivityDescEst { get; set; }
        private DateRange _LESchedule;
        private DateRange _LWESchedule;
        private DateRange _PlanSchedule;
        private DateRange _PhSchedule;


        private DateRange _CalcOPSchedule;
        public DateRange CalcOPSchedule
        {
            get
            {
                if (_CalcOPSchedule == null) _CalcOPSchedule = new DateRange();
                return _CalcOPSchedule;
            }
            set { _CalcOPSchedule = value; }
        }


        public bool VirtualPhase { get; set; }
        public bool ShiftFutureEventDate { get; set; }
        public DateRange LESchedule
        {
            get
            {
                if (_LESchedule == null) _LESchedule = new DateRange();
                return _LESchedule;
            }
            set { _LESchedule = value; }
        }
        public DateRange LWESchedule
        {
            get
            {
                if (_LWESchedule == null) _LWESchedule = new DateRange();
                return _LWESchedule;
            }
            set { _LWESchedule = value; }
        }
        public DateRange PlanSchedule
        {
            get
            {
                if (_PlanSchedule == null) _PlanSchedule = new DateRange();
                return _PlanSchedule;
            }

            set
            {
                _PlanSchedule = value;
            }
        }
        public DateRange PhSchedule
        {
            get
            {
                if (_PhSchedule == null) _PhSchedule = new DateRange();
                return _PhSchedule;
            }

            set
            {
                _PhSchedule = value;
            }
        }
        //public Double PhDuration
        //{
        //    get
        //    {
        //        return PhSchedule == null ? 0 : PhSchedule.Days;
        //    }
        //}
        public string FundingType { get; set; }
        public string Grouping { get; set; }
        public string EscalationGroup { get; set; }
        public string CSOGroup { get; set; }
        public Person TeamLead { get; set; }
        public Person LeadEngineer { get; set; }
        public Person WellEngineer { get; set; }

        public bool IsActualLE { get; set; }
        public string LatestWeeklyReportStatus { get; set; }

        //public string DataFromWhere { get; set; }
        /// <summary>
        /// null, ManualAdded, UploadLS, UploadOP
        /// </summary>
        public string EventCreatedFrom { get; set; } // UploadLS, FinancialCalendar, DataBrowser

        //public bool PostedOP { get; set; } 

        public List<Person> GetPersonsInRole(string wellName, string roleId)
        {
            return GetPersonsInRole(wellName, new string[] { roleId });
        }
        public List<Person> GetPersonsInRole(string wellName, string[] roleIds)
        {
            List<Person> ret = new List<Person>();
            var q = Query.And(
                    Query.In("WellName", new BsonArray(new string[] { wellName, "*" })),
                    Query.In("ActivityType", new BsonArray(new string[] { ActivityType, "*" }))
                );
            var ps = WEISPerson.Populate<WEISPerson>(q);
            var persons = ps.SelectMany(d => d.PersonInfos)
                .Where(d => roleIds.Contains(d.RoleId))
                .Select(d => new Person
                {
                    FullName = d.FullName,
                    Email = d.Email
                })
                .ToList();
            ret.AddRange(persons);
            return ret;
        }
        public string LevelOfEstimate { get; set; }
        private List<PIPElement> _pips;
        [BsonIgnore]
        public List<PIPElement> PIPs
        {
            get
            {
                if (_pips == null) _pips = new List<PIPElement>();
                return _pips;
            }
            set { _pips = value; }
        }
        private WellDrillData _op, _afe, _le, _lwe, _tq, _actual, _m1, _m2, _m3, _calcop, _bic, _aggredtarget;
        public WellDrillData TQ
        {
            get
            {
                if (_tq == null) _tq = new WellDrillData();
                return _tq;
            }
            set
            {
                _tq = value;
            }
        }
        public WellDrillData AggredTarget
        {
            get
            {
                if (_aggredtarget == null) _aggredtarget = new WellDrillData();
                return _aggredtarget;
            }
            set
            {
                _aggredtarget = value;
            }
        }
        public WellDrillData BIC
        {
            get
            {
                if (_bic == null) _bic = new WellDrillData();
                return _bic;
            }
            set
            {
                _bic = value;
            }
        }

        public bool IsPostOP { get; set; }

        public WellDrillData AFE
        {
            get
            {
                if (_afe == null) _afe = new WellDrillData();
                return _afe;
            }
            set
            {
                _afe = value;
            }
        }
        WellDrillData _plan;
        public WellDrillData Plan
        {
            get
            {
                if (_plan == null) _plan = new WellDrillData();
                return _plan;
            }
            set
            {
                _plan = value;
            }
        }
        public WellDrillData Actual
        {
            get
            {
                if (_actual == null) _actual = new WellDrillData();
                return _actual;
            }
            set
            {
                _actual = value;
            }
        }
        public WellDrillData CalcOP
        {
            get
            {
                if (_calcop == null) _calcop = new WellDrillData();
                return _calcop;
            }
            set
            {
                _calcop = value;
            }
        }

        public WellDrillData OP
        {
            get
            {
                if (_op == null) _op = new WellDrillData();
                return _op;
            }
            set
            {
                _op = value;
            }
        }
        public WellDrillData LE
        {
            get
            {
                if (_le == null) _le = new WellDrillData();
                return _le;
            }
            set
            {
                _le = value;
            }
        }
        public WellDrillData LWE
        {
            get
            {
                if (_lwe == null) _lwe = new WellDrillData();
                return _lwe;
            }
            set
            {
                _lwe = value;
            }
        }
        public WellDrillData M1
        {
            get
            {
                if (_m1 == null) _m1 = new WellDrillData();
                return _m1;
            }
            set
            {
                _m1 = value;
            }
        }
        public WellDrillData M2
        {
            get
            {
                if (_m2 == null) _m2 = new WellDrillData();
                return _m2;
            }
            set
            {
                _m2 = value;
            }
        }
        public WellDrillData M3
        {
            get
            {
                if (_m3 == null) _m3 = new WellDrillData();
                return _m3;
            }
            set
            {
                _m3 = value;
            }
        }
        DateTime _lastWeek, _previousWeek;
        public DateTime LastWeek
        {
            get
            {
                return _lastWeek;
            }
            set
            {
                _lastWeek = Tools.ToUTC(value, true);
            }
        }
        public DateTime PreviousWeek
        {
            get
            {
                return _previousWeek;
            }
            set
            {
                _previousWeek = Tools.ToUTC(value, true);
            }
        }

        private DateRange _LMESchedule;
        public DateRange LMESchedule
        {
            get
            {
                if (_LMESchedule == null) _LMESchedule = new DateRange();
                return _LMESchedule;
            }
            set { _LMESchedule = value; }
        }

        private WellDrillData _lme;
        public WellDrillData LME
        {
            get
            {
                if (_lme == null) _lme = new WellDrillData();
                return _lme;
            }
            set
            {
                _lme = value;
            }
        }


        DateTime _lastMonth, _previousMonth;
        public DateTime LastMonth
        {
            get
            {
                return _lastMonth;
            }
            set
            {
                _lastMonth = Tools.ToUTC(value, true);
            }
        }
        public DateTime PreviousMonth
        {
            get
            {
                return _previousMonth;
            }
            set
            {
                _previousMonth = Tools.ToUTC(value, true);
            }
        }

        private DateRange _AFESchedule { get; set; }
        public DateRange AFESchedule
        {
            get
            {
                if (_AFESchedule == null) _AFESchedule = new DateRange();
                return _AFESchedule;
            }
            set { _AFESchedule = value; }
        }
        public string Status { get; set; }
        public WellActivityUpdate GetLastUpdate(string WellName, string SequenceId, bool defaultIsNotExist)
        {
            var last = WellActivityUpdate.GetById(WellName, SequenceId, PhaseNo, null, true);
            if (last == null && defaultIsNotExist) last = new WellActivityUpdate();
            return last;
        }
        public void GetUpdate(string SequenceId, DateTime dt, bool useForecastIfNull = false, bool trimToFinishDate = false)
        {
            var last = WellActivityUpdate.GetById("", SequenceId, PhaseNo, (DateTime?)dt, true);
            if (last != null)
            {
                this.LE = last.CurrentWeek;
                this.LWE = last.LastWeek;
                this.Actual = last.Actual;

                if (this.OP.Days == 0) this.OP = last.OP;
                if (this.AFE.Days == 0) this.AFE = last.AFE;

                this.PIPs.Clear();
                foreach (var e in last.Elements)
                {
                    this.PIPs.Add(e);
                }
            }
            else if (useForecastIfNull == true)
            {
                this.LE = this.AFE;
                this.LWE = new WellDrillData();
                this.Actual = this.AFE;

                var pips = WellPIP.GetByOpsActivity(SequenceId, PhaseNo);
                if (pips != null)
                {
                    this.PIPs.Clear();
                    foreach (var pip in pips.Elements)
                    {
                        PIPs.Add(pip);
                    }
                }
            }

            if (this.AFE.Days == 0) this.AFE.Days = this.OP.Days;
            if (this.AFE.Cost == 0) this.AFE.Cost = this.OP.Cost;
            if (this.TQ.Days == 0) this.TQ.Days = this.OP.Days * 0.75;
            if (this.TQ.Cost == 0) this.TQ.Cost = this.OP.Cost * 0.75;

            if (trimToFinishDate && this.PhSchedule.Finish.CompareTo(dt) > 0)
            {
                var ratio = Tools.Div((double)(dt - PhSchedule.Start).TotalDays, (double)OP.Days);
                if (ratio < 0)
                    ratio = 0;
                if (PhSchedule.Start.AddDays(OP.Days).CompareTo(dt) > 0)
                {
                    OP.Days = ratio * OP.Days;
                    OP.Cost = ratio * OP.Cost;
                }

                if (PhSchedule.Start.AddDays(AFE.Days).CompareTo(dt) > 0)
                {
                    AFE.Days = ratio * AFE.Days;
                    AFE.Cost = ratio * AFE.Cost;
                }

                if (PhSchedule.Start.AddDays(LE.Days).CompareTo(dt) > 0)
                {
                    LE.Days = ratio * LE.Days;
                    LE.Cost = ratio * LE.Cost;
                }

                if (PhSchedule.Start.AddDays(TQ.Days).CompareTo(dt) > 0)
                {
                    TQ.Days = ratio * TQ.Days;
                    TQ.Cost = ratio * TQ.Cost;
                }
            }
        }

        public WellDrillData CalculatedOP()
        {
            if (this.Plan == null) return new WellDrillData();
            else return this.Plan;
        }

        public WellDrillData CalculatedAFE()
        {
            if (this.AFE == null) return new WellDrillData();
            else return this.AFE;
        }

        public WellDrillData CalculatedLE()
        {
            if (this.AFE == null || (this.AFE.Days == 0 && this.AFE.Cost == 0))
                return new WellDrillData();

            if (this.LE == null || (this.LE.Days == 0 && this.LE.Cost == 0))
            {
                return this.OP;
            }
            else
                return this.LE;
        }

        public void initLEFromWeeklyReport(WellActivityUpdate wau)
        {
            var before = wau.GetBefore();
            LE = wau.CurrentWeek;
            if (wau.EventStartDate.Year > 2000)
            {
                LESchedule.Start = Tools.ToUTC(wau.EventStartDate, true);
            }
            else
            {
                if (wau.Actual.Days == 0)
                {
                    LESchedule.Start = PhSchedule.Start;
                }
                else
                {
                    LESchedule.Start = wau.UpdateVersion.AddDays(-wau.Actual.Days);
                }
            }
            if (LE.Days == 0) LE.Days = OP.Days;
            LESchedule.Finish = LESchedule.Start.AddDays(LE.Days == 0 ? 0 : LE.Days);
            LastWeek = wau.UpdateVersion;

            if (before == null)
            {
                LWESchedule = PhSchedule;
                LWE = OP;
                PreviousWeek = Tools.DefaultDate;
            }
            else
            {
                LWE = before.CurrentWeek;
                if (before.Actual.Days == 0)
                {
                    LWESchedule.Start = PhSchedule.Start;
                }
                else
                {
                    LWESchedule.Start = before.UpdateVersion.AddDays(-before.Actual.Days);
                }
                if (LWE.Days == 0) LWE.Days = OP.Days;
                LWESchedule.Finish = LWESchedule.Start.AddDays(LWE.Days == 0 ? 0 : LWE.Days);
                PreviousWeek = before.UpdateVersion;
            }
        }

        public void initLEFromWeeklyReport(WellActivityUpdateMonthly wau)
        {
            var before = wau.GetBefore();
            LE = wau.CurrentWeek;
            if (wau.EventStartDate.Year > 2000)
            {
                LESchedule.Start = Tools.ToUTC(wau.EventStartDate, true);
            }
            else
            {
                if (wau.Actual.Days == 0)
                {
                    LESchedule.Start = PhSchedule.Start;
                }
                else
                {
                    LESchedule.Start = wau.UpdateVersion.AddDays(-wau.Actual.Days);
                }
            }
            if (LE.Days == 0) LE.Days = OP.Days;
            LESchedule.Finish = LESchedule.Start.AddDays(LE.Days == 0 ? 0 : LE.Days);
            //LastWeek = wau.UpdateVersion;
            LastMonth = wau.UpdateVersion;

            if (before == null)
            {
                LMESchedule = PhSchedule;
                LME = OP;
                PreviousMonth = Tools.DefaultDate;
            }
            else
            {
                LWE = before.CurrentMonth;
                if (before.Actual.Days == 0)
                {
                    LMESchedule.Start = PhSchedule.Start;
                }
                else
                {
                    LMESchedule.Start = before.UpdateVersion.AddDays(-before.Actual.Days);
                }
                if (LME.Days == 0) LME.Days = OP.Days;
                LMESchedule.Finish = LMESchedule.Start.AddDays(LME.Days == 0 ? 0 : LME.Days);
                PreviousMonth = before.UpdateVersion;
            }
        }

        public WellDrillData CalculatedEstimate()
        {
            return new WellDrillData()
            {
                Days = this.CalculatedLE().Days - this.CalculatedAFE().Days,
                Cost = this.CalculatedLE().Cost - this.CalculatedAFE().Cost
            };
        }
        public WellDrillData CalculatedEstimateOP()
        {
            return new WellDrillData()
            {
                Days = this.CalculatedLE().Days - this.CalculatedOP().Days,
                Cost = this.CalculatedLE().Cost - this.CalculatedOP().Cost
            };
        }

        [BsonIgnore]
        public string userupdate { get; set; }

        [BsonIgnore]
        public List<ProrateDetails> ProrateInfos { get; set; }
        [BsonIgnore]
        public string ActivityCategory { get; set; }

        [BsonIgnore]
        public bool PushToBizPlan { get; set; }
        [BsonIgnore]
        public string BizPlanStatus { get; set; }
    }

    public class ProrateDetails 
    {
        public string Title { get; set; }
        public NumberOfDayPerYear Info { get; set; }
        public WellDrillData Value   { get; set; }
        public ProrateDetails()
        {
            Info = new NumberOfDayPerYear();
            Value = new WellDrillData();
        }
    }

    public class WellActivityPerson : WellActivityPhase
    {
        public string WellName { get; set; }
        public string UARigSequenceId { get; set; }
    }

    public class OPListHelperForDataBrowserGrid
    {
        public string BaseOP { get; set; }
        public DateRange OPSchedule { get; set; }
        public WellDrillData OP { get; set; }
        public OPListHelperForDataBrowserGrid()
        {
            OPSchedule = new DateRange();
            OP = new WellDrillData();
        }
    }

}