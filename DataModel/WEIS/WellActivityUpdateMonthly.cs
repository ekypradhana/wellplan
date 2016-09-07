using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Aspose.Cells;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ECIS.Client.WEIS
{
    [BsonIgnoreExtraElements]
    public class WellActivityUpdateMonthly : ECISModel
    {
        public WellActivityUpdateMonthly()
        {
            this.UpdateVersion = DateTime.Now;
            this.UpdateVersion = Tools.ToDateTime(this.UpdateVersion.ToString(), true);
            if (this.UpdateVersion.DayOfWeek != DayOfWeek.Monday)
            {
                int dow = (int)this.UpdateVersion.DayOfWeek;
                this.UpdateVersion = this.UpdateVersion.AddDays(-(dow - 1));
            }
            OpsSchedule = new DateRange();
        }

        public string WellEngineer { get; set; }
        public string Status { get; set; }
        public bool Archived { get; set; }
        public override string TableName
        {
            get
            {
                return "WEISWellActivityUpdatesMonthly";
            }
        }
        public WellActivityPhase GetWellActivity()
        {
            try
            {
                List<IMongoQuery> qs = new List<IMongoQuery>();
                qs.Add(Query.EQ("WellName", WellName));
                qs.Add(Query.EQ("Phases.PhaseNo", Phase.PhaseNo));
                qs.Add(Query.EQ("UARigSequenceId", SequenceId));
                WellActivity wa = WellActivity.Get<WellActivity>(Query.And(qs));
                var p = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(Phase.ActivityType));
                return p;
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("Error Processing {0}. Error Command: {1}", WellName, Tools.PushException(e), e.StackTrace));
            }
        }

        DateTime _updateVersion;
        public DateTime UpdateVersion
        {
            get
            {
                return _updateVersion;
            }

            set
            {
                _updateVersion = Tools.ToUTC(value);
            }
        }
        public void Calc()
        {
            #region PIP
            var pip = WellPIP.GetByOpsActivity(this.SequenceId, this.Phase.ActivityType, this.WellName);
            if (pip != null)
            {
                #region NoNeed
                //var deleted = new List<PIPElement>();
                //foreach (var e in elements)
                //{
                //    var exist = pipElements.FirstOrDefault(d=>d.ElementId.Equals(e.ElementId));
                //    if (exist == null) 
                //        deleted.Add(exist);
                //    //else
                //    //{
                //    //    exist.Title = e.Title;
                //    //    exist.DaysPlanImprovement = e.DaysPlanImprovement;
                //    //    exist.DaysPlanRisk = e.DaysPlanRisk;
                //    //    exist.CostPlanImprovement = e.CostPlanImprovement;
                //    //    exist.CostPlanRisk = e.CostPlanRisk;
                //    //}
                //}
                //foreach (var d in deleted) elements.Remove(d);

                ////-- add
                //foreach (var e in pipElements)
                //{
                //    var add = elements.FirstOrDefault(d=>d.ElementId.Equals(e.ElementId));
                //    if (add == null) elements.Add(e);
                //}
                #endregion

                //--- sync PIP element between Update and PIP Config
                var es = new List<PIPElement>();
                foreach (var o in pip.Elements)
                {
                    var x = Elements.FirstOrDefault(d1 => d1.ElementId.Equals(o.ElementId));
                    if (x == null)
                    {
                        x = new PIPElement();
                        x = o;
                        //continue;
                    }
                    var n = x != null ? x : o;
                    //if (n != o)
                    //{
                    //    n.CostPlanImprovement = o.CostPlanImprovement;
                    //    n.CostPlanRisk = o.CostPlanRisk;
                    //    n.DaysPlanImprovement = o.DaysPlanImprovement;
                    //    n.DaysPlanRisk = o.DaysPlanRisk;
                    //}
                    
                    //sync completion data with PIP
                    n.Completion = o.Completion;
                    n.DaysCurrentWeekImprovement = o.DaysCurrentWeekImprovement;
                    n.DaysCurrentWeekRisk = o.DaysCurrentWeekRisk;
                    n.CostCurrentWeekImprovement = o.CostCurrentWeekImprovement;
                    n.CostCurrentWeekRisk = o.CostCurrentWeekRisk;
                    n.Period = o.Period; //add by eky on sept,23 in order to be used to checking the overlapping of CRElements
                    n.AssignTOOps = o.AssignTOOps;
                    List<PIPAllocation> allocs = new List<PIPAllocation>();
                    foreach (var oAlloc in o.Allocations)
                    {
                        var xAlloc = n.Allocations.FirstOrDefault(d => d.Period.Year.Equals(oAlloc.Period.Year) && d.Period.Month.Equals(oAlloc.Period.Month));
                        var nAlloc = xAlloc == null ? oAlloc : xAlloc;
                        n.CostPlanImprovement = o.CostPlanImprovement;
                        n.CostPlanRisk = o.CostPlanRisk;
                        n.DaysPlanImprovement = o.DaysPlanImprovement;
                        n.DaysPlanRisk = o.DaysPlanRisk;

                        nAlloc.CostPlanImprovement = oAlloc.CostPlanImprovement;
                        nAlloc.CostPlanRisk = oAlloc.CostPlanRisk;
                        nAlloc.DaysPlanImprovement = oAlloc.DaysPlanImprovement;
                        nAlloc.DaysPlanRisk = oAlloc.DaysPlanRisk;
                        allocs.Add(nAlloc);
                    }
                    n.Allocations = allocs;
                    es.Add(n);
                }
                Elements = es;

                //var crs = new List<PIPElement>();
                //foreach (var o in pip.CRElements)
                //{
                //    var x = CRElements.FirstOrDefault(d1 => d1.ElementId.Equals(o.ElementId));
                //    if (x == null)
                //    {
                //        x = new PIPElement();
                //        x = o;
                //        //continue;
                //    }
                //    var n = x != null ? x : o;
                //    n.Completion = o.Completion;
                //    n.DaysCurrentWeekImprovement = o.DaysCurrentWeekImprovement;
                //    n.DaysCurrentWeekRisk = o.DaysCurrentWeekRisk;
                //    n.CostCurrentWeekImprovement = o.CostCurrentWeekImprovement;
                //    n.CostCurrentWeekRisk = o.CostCurrentWeekRisk;
                //    n.Period = o.Period; //add by eky on sept,23 in order to be used to checking the overlapping of CRElements
                //    n.AssignTOOps = o.AssignTOOps;
                //    List<PIPAllocation> allocs = new List<PIPAllocation>();
                //    foreach (var oAlloc in o.Allocations)
                //    {
                //        var xAlloc = n.Allocations.FirstOrDefault(d => d.Period.Year.Equals(oAlloc.Period.Year) && d.Period.Month.Equals(oAlloc.Period.Month));
                //        var nAlloc = xAlloc == null ? oAlloc : xAlloc;
                //        n.CostPlanImprovement = o.CostPlanImprovement;
                //        n.CostPlanRisk = o.CostPlanRisk;
                //        n.DaysPlanImprovement = o.DaysPlanImprovement;
                //        n.DaysPlanRisk = o.DaysPlanRisk;

                //        nAlloc.CostPlanImprovement = oAlloc.CostPlanImprovement;
                //        nAlloc.CostPlanRisk = oAlloc.CostPlanRisk;
                //        nAlloc.DaysPlanImprovement = oAlloc.DaysPlanImprovement;
                //        nAlloc.DaysPlanRisk = oAlloc.DaysPlanRisk;
                //        allocs.Add(nAlloc);
                //    }
                //    n.Allocations = allocs;
                //    crs.Add(n);
                //}
                //CRElements = crs;

                var before = GetBefore();
                if (before == null)
                {
                    before = new WellActivityUpdateMonthly();
                    before.Elements = pip.Elements;
                    foreach (var e in before.Elements)
                    {
                        e.CostCurrentWeekImprovement = 0;
                        e.CostCurrentWeekRisk = 0;
                        e.DaysCurrentWeekImprovement = 0;
                        e.DaysCurrentWeekRisk = 0;
                    }
                }
                foreach (var e in Elements)
                {
                    var be = before.Elements.FirstOrDefault(d => d.ElementId.Equals(e.ElementId));
                    if (be == null) be = new PIPElement();
                    e.CostLastWeekImprovement = be.CostCurrentWeekImprovement == 0 ?
                        be.CostPlanImprovement : be.CostCurrentWeekImprovement;
                    e.CostLastWeekRisk = be.CostCurrentWeekRisk == 0 ?
                        be.CostPlanRisk : be.CostCurrentWeekRisk;
                    e.DaysLastWeekImprovement = be.DaysCurrentWeekImprovement == 0 ?
                        be.DaysPlanImprovement : be.DaysCurrentWeekImprovement;
                    e.DaysLastWeekRisk = be.DaysCurrentWeekRisk == 0 ?
                        be.DaysPlanRisk : be.DaysCurrentWeekRisk;
                }
            }
            #endregion

            #region Phase
            WellActivity wa = WellActivity.GetByUniqueKey(WellName, SequenceId);
            if (wa != null)
            {
                var act = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(Phase.ActivityType) && d.BaseOP != null && d.BaseOP.Any() && d.BaseOP.OrderByDescending(c => c).FirstOrDefault().Equals(this.Phase.BaseOP));

                if (act != null)
                {
                    OP = act.OP;
                    AFE = act.AFE;
                    Plan = act.Plan;
                    TQ = act.TQ;



                    //if (CurrentWeek.Days == 0) CurrentWeek.Days = LastWeek.Days;
                    //if (CurrentWeek.Cost == 0) CurrentWeek.Cost = LastWeek.Cost;

                    //if (CurrentWeek.Days == 0 && CurrentWeek.Cost == 0)
                    //{
                    //    if (CurrentWeek.Days == 0) CurrentWeek.Days = act.LE.Days;
                    //    if (CurrentWeek.Cost == 0) CurrentWeek.Cost = act.LE.Cost;
                    //}

                    //if (CurrentWeek.Days == 0 && CurrentWeek.Cost == 0)
                    //{
                    //    if (CurrentWeek.Days == 0) CurrentWeek.Days = act.OP.Days;
                    //    if (CurrentWeek.Cost == 0) CurrentWeek.Cost = act.OP.Cost;
                    //}
                }
                else
                {
                    // jika wellplan 16 & monthly nya 15. ambil ke history 

                    var hst =
                        wa.OPHistories.FirstOrDefault(
                            x => x.ActivityType.Equals(Phase.ActivityType) && Phase.BaseOP != null &&  Phase.BaseOP.Any() && this.Phase.BaseOP.OrderByDescending(c => c).FirstOrDefault().Equals(this.Phase.BaseOP));
                    //FirstOrDefault(d => d.ActivityType.Equals(Phase.ActivityType) && d.BaseOP != null && d.BaseOP.OrderByDescending(c => c).FirstOrDefault().Equals(this.Phase.BaseOP));
                    if (hst != null)
                    {
                        OP = hst.OP;
                        AFE = hst.AFE;
                        Plan = hst.Plan;
                        TQ = hst.TQ;
                    }
                }

                if (act == null || act.LastWeek > UpdateVersion)
                {
                    var wact = WellActivityActual.GetById(WellName, SequenceId, UpdateVersion, true);
                    if (wact != null)
                    {
                        var wactphase = wact.GetActual(Phase.PhaseNo);
                        if (wactphase != null)
                        {
                            Actual = wactphase.Data;
                        }
                        var wactafe = wact.GetAFE(Phase.PhaseNo);
                        if (wactafe != null)
                        {
                            AFE = wactafe.Data;
                        }
                    }
                }

                var u = GetBefore();
                if (u != null)
                {
                    LastWeek = u.CurrentWeek;
                    if (Site == "" || Site == null)
                    {
                        Site = u.Site;
                    }
                    if (WellType == "" || WellType == null)
                    {
                        WellType = u.WellType;
                    }
                    if (Objective == "" || Objective == null)
                    {
                        Objective = u.Objective;
                    }
                    if (Contractor == "" || Contractor == null)
                    {
                        Contractor = u.Contractor;
                    }
                    if (RigSuperintendent == "" || RigSuperintendent == null)
                    {
                        RigSuperintendent = u.RigSuperintendent;
                    }
                    if (Company == "" || Company == null)
                    {
                        Company = u.Company;
                    }
                    if (Project == "" || Project == null)
                    {
                        Project = u.Project;
                    }
                    if (EventType == "" || EventType == null)
                    {
                        EventType = u.EventType;
                    }
                    if (EventStartDate == DateTime.MinValue)
                    {
                        EventStartDate = u.EventStartDate;
                    }
                    if (WorkUnit == "" || WorkUnit == null)
                    {
                        WorkUnit = u.WorkUnit;
                    }
                    if (OriginalSpudDate == DateTime.MinValue)
                    {
                        OriginalSpudDate = u.OriginalSpudDate;
                    }
                }
                else
                {
                    //LastWeek = OP;
                }

                //if (CurrentWeek.Days == 0 && CurrentWeek.Cost == 0)
                //    CurrentWeek = LastWeek;

                //q = Query.And(Query.EQ("WellName", WellName), Query.EQ("SequenceId", SequenceId), 
                //    Query.GT("UpdateVersion", UpdateVersion));
                //u = WellActivityUpdate.Get<WellActivityUpdate>(q, sort: SortBy.Ascending("UpdateVersion"));
                //if (u==null)
                //{
                //    var ph = wa.GetPhase(Phase.PhaseNo);
                //    ph.Actual = Actual;
                //    ph.LE = CurrentWeek;
                //    wa.Save();
                //}
            }
            #endregion
        }
        public string Country { get; set; }
        public string AssetName { get; set; }
        public bool NewWell { get; set; }
        public string WellName { get; set; }
        public string SequenceId { get; set; }

        [BsonIgnore]
        public string RigName { get; set; }

        private WellActivityPhase _phase;
        public WellActivityPhase Phase
        {
            get
            {
                if (_phase == null) _phase = new WellActivityPhase();
                return _phase;
            }
            set
            {
                _phase = value;
            }
        }
        public string ExecutiveSummary { get; set; }
        public string PlannedOperation { get; set; }
        public string SupplementReason { get; set; }
        public bool SupplementLast7Days { get; set; }
        private List<PIPElement> _elements;
        public List<PIPElement> Elements
        {
            get
            {
                if (_elements == null) _elements = new List<PIPElement>();
                return _elements;
            }
            set { _elements = value; }
        }
        private List<PIPElement> _crelements;
        public List<PIPElement> CRElements
        {
            get
            {
                if (_crelements == null) _crelements = new List<PIPElement>();
                return _crelements;
            }
            set { _crelements = value; }
        }
        private WellDrillData _drillData;
        public WellDrillData CurrentWeek
        {
            get
            {
                if (_currentweek == null) _currentweek = new WellDrillData();
                return _currentweek;
            }

            set
            {
                _currentweek = value;
            }
        }
        public WellDrillData CurrentMonth
        {
            get
            {
                if (_drillData == null) _drillData = new WellDrillData();
                return _drillData;
            }

            set
            {
                _drillData = value;
            }
        }
        private WellDrillData _currentweek;
        private WellDrillData _op;
        private WellDrillData _plan;
        private WellDrillData _afe;
        private WellDrillData _actual;
        private WellDrillData _lastWeek;
        private WellDrillData _tqgaps;
        private WellDrillData _npthours;

        public WellDrillData Plan
        {
            get
            {
                if (_plan == null) _plan = new WellDrillData();
                return _plan;
            }
            set { _plan = value; }
        }
        public WellDrillData OP
        {
            get
            {
                if (_op == null) _op = new WellDrillData();
                return _op;
            }
            set { _op = value; }
        }
        public WellDrillData AFE
        {
            get
            {
                if (_afe == null) _afe = new WellDrillData();
                return _afe;
            }
            set { _afe = value; }
        }
        public WellDrillData Actual
        {
            get
            {
                if (_actual == null) _actual = new WellDrillData();
                return _actual;
            }
            set { _actual = value; }
        }
        public WellDrillData LastWeek
        {
            get
            {
                if (_lastWeek == null) _lastWeek = new WellDrillData();
                return _lastWeek;
            }
            set { _lastWeek = value; }
        }
        public WellDrillData TQ
        {
            get
            {
                if (_tqgaps == null) _tqgaps = new WellDrillData();
                return _tqgaps;
            }
            set { _tqgaps = value; }
        }
        public WellDrillData NPT
        {
            get
            {
                if (_npthours == null) _npthours = new WellDrillData();
                return _npthours;
            }
            set { _npthours = value; }
        }

        #region Added by yoga 01102015
        private WellDrillData _delta;
        private WellDrillData _capitalreductionscm;
        private WellDrillData _capitalreductionother;
        private WellDrillData _capitalefficiency;
        private WellDrillData _sequence;

        public WellDrillData Delta
        {
            get
            {
                if (_delta == null) _delta = new WellDrillData();
                return _delta;
            }
            set { _delta = value; }
        }
        public WellDrillData CapitalReductionSCM
        {
            get
            {
                if (_capitalreductionscm == null) _capitalreductionscm = new WellDrillData();
                return _capitalreductionscm;
            }
            set { _capitalreductionscm = value; }
        }
        public WellDrillData CapitalReductionOther
        {
            get
            {
                if (_capitalreductionother == null) _capitalreductionother = new WellDrillData();
                return _capitalreductionother;
            }
            set { _capitalreductionother = value; }
        }
        public WellDrillData CapitalEfficiency
        {
            get
            {
                if (_capitalefficiency == null) _capitalefficiency = new WellDrillData();
                return _capitalefficiency;
            }
            set { _capitalefficiency = value; }
        }
        public WellDrillData Sequence
        {
            get
            {
                if (_sequence == null) _sequence = new WellDrillData();
                return _sequence;
            }
            set { _sequence = value; }
        }
        #endregion

        public string OperationSummary { get; set; }


        public WellActivityPhase GetWellActivityPhase()
        {
            var q = Query.And(Query.EQ("WellName", WellName), Query.EQ("UARigSequenceId", SequenceId));


            var wa = WellActivity.Get<WellActivity>(q);
            if (wa != null)
            {
                var wap = wa.Phases.Where(x => x.ActivityType.Equals(Phase.ActivityType));
                if(wap != null && wap.Count() > 0)
                {
                    return wap.FirstOrDefault();
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            if (references != null && references.Contains("isActualLE"))
            {
                this.Phase.IsActualLE = true;
                if (this.Phase.LE == null)
                {
                    this.Phase.LE = new WellDrillData();
                }
                this.Phase.LE.Cost = this.CurrentWeek.Cost;
                this.Phase.LE.Days = this.CurrentWeek.Days;
            }

            if (this.Elements != null && this.Elements.Count() > 0)
            {
                var realiz = this.Elements.Where(x => x.Completion.Equals("Realized"));
                if (realiz != null && references != null && references.Contains("SyncPIP"))
                {
                    var realComp = realiz.Where(x => x.Classification != null && x.Classification.Equals("Competitive Scope")).ToList();
                    var realeff = realiz.Where(x => x.Classification != null && x.Classification.Equals("Efficient Execution")).ToList();
                    var realtech = realiz.Where(x => x.Classification != null && x.Classification.Equals("Technology and Innovation")).ToList();
                    var realsupply = realiz.Where(x => x.Classification != null && x.Classification.Equals("Supply Chain Transformation")).ToList();

                    //Competitive Scope
                    this.RealizedPIPElemCompetitiveScope.Cost = realComp.Any() ? realComp
                        .Sum(y => y.CostCurrentWeekImprovement + y.CostCurrentWeekRisk) : 0;
                    this.RealizedPIPElemCompetitiveScope.Days = realComp.Any() ? realComp
                        .Sum(y => y.DaysCurrentWeekImprovement + y.DaysCurrentWeekRisk) : 0;

                    //Efficient Execution
                    this.RealizedPIPElemEfficientExecution.Cost = realeff.Any() ? realeff
                        .Sum(y => y.CostCurrentWeekImprovement + y.CostCurrentWeekRisk) : 0;
                    this.RealizedPIPElemEfficientExecution.Days = realeff.Any() ? realeff
                        .Sum(y => y.DaysCurrentWeekImprovement + y.DaysCurrentWeekRisk) : 0;

                    //TechnologyAndInnovation
                    this.RealizedPIPElemTechnologyAndInnovation.Cost = realtech.Any() ? realtech
                        .Sum(y => y.CostCurrentWeekImprovement + y.CostCurrentWeekRisk) : 0;
                    this.RealizedPIPElemTechnologyAndInnovation.Days = realtech.Any() ? realtech
                        .Sum(y => y.DaysCurrentWeekImprovement + y.DaysCurrentWeekRisk) : 0;

                    //TechnologyAndInnovation
                    this.RealizedPIPElemSupplyChainTransformation.Cost = realsupply.Any() ? realsupply
                        .Sum(y => y.CostCurrentWeekImprovement + y.CostCurrentWeekRisk) : 0;
                    this.RealizedPIPElemSupplyChainTransformation.Days = realsupply.Any() ? realsupply
                        .Sum(y => y.DaysCurrentWeekImprovement + y.DaysCurrentWeekRisk) : 0;
                }

                if (realiz != null && realiz.Count() > 0)
                {

                    var realComp = realiz.Where(x => x.Classification != null && x.Classification.Equals("Competitive Scope")).ToList();
                    var realeff = realiz.Where(x => x.Classification != null && x.Classification.Equals("Efficient Execution")).ToList();
                    var realtech = realiz.Where(x => x.Classification != null && x.Classification.Equals("Technology and Innovation")).ToList();
                    var realsupply = realiz.Where(x => x.Classification != null && x.Classification.Equals("Supply Chain Transformation")).ToList();


                    //Competitive Scope
                    this.RealizedPIPElemCompetitiveScope.Cost = realComp.Any() ? realComp
                        .Sum(y => y.CostCurrentWeekImprovement + y.CostCurrentWeekRisk) : 0;
                    this.RealizedPIPElemCompetitiveScope.Days =  realComp.Any() ? realComp
                        .Sum(y => y.DaysCurrentWeekImprovement + y.DaysCurrentWeekRisk) : 0;

                    //Efficient Execution
                    this.RealizedPIPElemEfficientExecution.Cost = realeff.Any() ? realeff
                        .Sum(y => y.CostCurrentWeekImprovement + y.CostCurrentWeekRisk) : 0;
                    this.RealizedPIPElemEfficientExecution.Days = realeff.Any() ? realeff
                        .Sum(y => y.DaysCurrentWeekImprovement + y.DaysCurrentWeekRisk) : 0;

                    //TechnologyAndInnovation
                    this.RealizedPIPElemTechnologyAndInnovation.Cost = realtech.Any() ? realtech
                        .Sum(y => y.CostCurrentWeekImprovement + y.CostCurrentWeekRisk) : 0;
                    this.RealizedPIPElemTechnologyAndInnovation.Days = realtech.Any() ? realtech
                        .Sum(y => y.DaysCurrentWeekImprovement + y.DaysCurrentWeekRisk) : 0;

                    //TechnologyAndInnovation
                    this.RealizedPIPElemSupplyChainTransformation.Cost = realsupply.Any() ? realsupply
                        .Sum(y => y.CostCurrentWeekImprovement + y.CostCurrentWeekRisk) : 0;
                    this.RealizedPIPElemSupplyChainTransformation.Days = realsupply.Any() ? realsupply
                        .Sum(y => y.DaysCurrentWeekImprovement + y.DaysCurrentWeekRisk) : 0;
                }
            }

            if (references != null && references.Contains("updateLeStatus"))
            {
                this.Phase.IsActualLE = true;
                if (this.Phase.LE == null)
                {
                    this.Phase.LE = new WellDrillData();
                }
                this.Phase.LE.Cost = this.Phase.LE.Cost;
                this.Phase.LE.Days = this.Phase.LE.Days;
            }

            _id = String.Format("W{0}S{1}A{2}D{3:yyyyMMdd}",
                WellName.Replace(" ", "").Replace("-", ""),
                SequenceId,
                Phase.ActivityType.Replace(" ", "").Replace("-", ""),
                UpdateVersion);
            return this.ToBsonDocument();
        }
        public override BsonDocument PostSave(BsonDocument doc, string[] references = null)
        {
            //doc = base.PostSave(doc);
            //-- get latest update
            var before = GetBefore();
            var after = GetAfter();
            if (references == null) references = new string[] { };

            //--- update other table if this is the latest
            if (after == null)
            {
                //--- get well PIP
                var ignorePIP = references.Count() >= 1 && references[0].ToLower().Equals("ignorewellpip");
                var calcLESchedule = references.Count() >= 2 && references[1].ToLower().Equals("calcleschedule");
                if (!ignorePIP)
                {
                    WellPIP pip = WellPIP.GetByWellActivity(WellName, Phase.ActivityType);
                    if (pip != null)
                    {
                        foreach (var e in Elements)
                        {
                            var p = pip.Elements.FirstOrDefault(d => d.ElementId.Equals(e.ElementId));
                            if (p != null)
                            {
                                p.Completion = e.Completion;
                                p.DaysCurrentWeekImprovement = e.DaysCurrentWeekImprovement;
                                p.DaysCurrentWeekRisk = e.DaysCurrentWeekRisk;
                                p.CostCurrentWeekImprovement = e.CostCurrentWeekImprovement;
                                p.CostCurrentWeekRisk = e.CostCurrentWeekRisk;
                                p.Allocations = e.Allocations;
                                p.isNewElement = false;
                                p.Title = e.Title;
                                p.AssignTOOps = e.AssignTOOps;
                            }
                            else if (e.isNewElement)
                            {
                                pip.Elements.Add(e);
                            }
                        }
                        pip.Save(references: new string[] { "IgnoreWAUM" });
                    }
                }

                var q = Query.And(Query.EQ("WellName", WellName), Query.EQ("UARigSequenceId", SequenceId), Query.EQ("Phases.ActivityType", Phase.ActivityType));
                var wa = WellActivity.Get<WellActivity>(q);
                WellActivityPhase ph = null;
                if (wa != null)
                    ph = wa.Phases.FirstOrDefault(d => d.PhaseNo.Equals(Phase.PhaseNo));
                //-- update last estimate on wa
                if (ph != null && after == null)
                {
                    if (references != null && references.Contains("initiateProcess"))
                    {

                    }
                    else
                    {
                        if (ph != null)
                        {
                            ph.initLEFromWeeklyReport(this);
                        }

                        if ((references != null && references.Contains("updateLeStatus")) || (references != null && references.Contains("isActualLE")))
                        {
                            ph.IsActualLE = true;
                            if (ph.LE == null)
                            {
                                ph.LE = new WellDrillData();
                            }
                            ph.LE.Cost = this.Phase.LE.Cost;
                            ph.LE.Days = this.Phase.LE.Days;
                        }

                        wa.Save();

                        if (calcLESchedule)
                        {
                            WellActivity.UpdateLESchedule(new string[] { wa.RigName });
                        }
                    }
                    
                }

            }

            return this.ToBsonDocument();
        }
        public WellActivityUpdateMonthly GetBefore()
        {
            //-- get latest update
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("Phase.ActivityType", Phase.ActivityType));
            qs.Add(Query.EQ("SequenceId", SequenceId));
            qs.Add(Query.LT("UpdateVersion", UpdateVersion));
            var latest = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(Query.And(qs), SortBy.Descending("UpdateVersion"));
            return latest;
        }
        public WellActivityUpdateMonthly GetAfter()
        {
            //-- get latest update
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("Phase.ActivityType", Phase.ActivityType));
            qs.Add(Query.EQ("SequenceId", SequenceId));
            qs.Add(Query.GT("UpdateVersion", UpdateVersion));
            var latest = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(Query.And(qs), SortBy.Ascending("UpdateVersion"));
            return latest;
        }
        public static WellActivityUpdateMonthly GetById(
            string wellName,
            string sequenceId = null,
            int phaseNo = 0,
            //string activityType = null,
            DateTime? update = null,
            bool getLatest = false,
            bool newIfNull = false)
        {
            update = update == null ? DateTime.MaxValue : Tools.ToUTC(update);
            List<IMongoQuery> qs = new List<IMongoQuery>();
            if (String.IsNullOrEmpty(wellName) == false) qs.Add(Query.EQ("WellName", wellName));
            if (sequenceId != null) qs.Add(Query.EQ("SequenceId", sequenceId));
            qs.Add(Query.EQ("Phase.PhaseNo", phaseNo));
            //if (activityType != null) qs.Add(Query.EQ("ActivityType", activityType));
            if (update != null)
            {
                DateTime d = Tools.ToUTC((DateTime)update);
                if (getLatest) qs.Add(Query.LTE("UpdateVersion", d));
                else qs.Add(Query.EQ("UpdateVersion", d));
            }
            IMongoQuery q = Query.And(qs);
            WellActivityUpdateMonthly r = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(q, SortBy.Descending("UpdateVersion"));
            if (newIfNull && r == null) r = new WellActivityUpdateMonthly();
            return r;
        }

        public override void PostDelete()
        {
            var latest = GetBefore();
            var after = GetAfter();
            if (after == null && latest != null) latest.Save("", "CalcLESchedule");
        }

        public static List<WellActivityUpdateMonthly> PopulateLast(
            string wellName,
            string sequenceId,
            int phaseNo = 0,
            //string activityType = null,
            DateTime? update = null,
            int take = 1)
        {
            //update = Tools.ToUTC(update);
            List<IMongoQuery> qs = new List<IMongoQuery>();
            if (String.IsNullOrEmpty(wellName) == false) qs.Add(Query.EQ("WellName", wellName));
            if (sequenceId != null) qs.Add(Query.EQ("SequenceId", sequenceId));
            if (phaseNo > 0) qs.Add(Query.EQ("PhaseNo", phaseNo));
            //if (activityType != null) qs.Add(Query.EQ("ActivityType", activityType));
            if (update != null)
            {
                DateTime d = Tools.ToUTC((DateTime)update);
                qs.Add(Query.LTE("UpdatedVersion", d));
            }
            IMongoQuery q = Query.And(qs);
            var r = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q, take, sort: SortBy.Descending("UpdateVersion"));
            return r;
        }


        public string Company { get; set; }

        public string Site { get; set; }
        public string Project { get; set; }
        public string WellType { get; set; }
        public string EventType { get; set; }
        public string Objective { get; set; }

        DateTime _EventStartDate;

        public DateTime EventStartDate
        {
            get
            {
                return _EventStartDate;
            }

            set
            {
                _EventStartDate = Tools.ToUTC(value);
            }
        }
        public string Contractor { get; set; }
        public string WorkUnit { get; set; }
        public string RigSuperintendent { get; set; }

        DateTime _OriginalSpudDate;
        public DateTime OriginalSpudDate
        {
            get
            {
                return _OriginalSpudDate;
            }

            set
            {
                _OriginalSpudDate = Tools.ToUTC(value);
            }
        }

        private string GetPersonNameByRole(string RoleId)
        {
            string StrWellName = WellName;
            string StrSeqId = SequenceId;
            if (WellName == null) StrWellName = "";
            if (SequenceId == null) StrSeqId = "";
            List<IMongoQuery> queries = new List<IMongoQuery>();
            queries.Add(Query.EQ("WellName", StrWellName));
            queries.Add(Query.EQ("SequenceId", StrSeqId));

            //BsonArray qactivity = new BsonArray();
            //foreach (var activity in Phases.Select(d => d.ActivityType)) qactivity.Add(activity);
            //queries.Add(Query.In("ActivityType", qactivity));

            //BsonArray qphases = new BsonArray();
            //foreach (var phaseNo in Phases.Select(d => d.PhaseNo)) qphases.Add(phaseNo);
            //queries.Add(Query.In("PhaseNo", qphases));

            IMongoQuery query = queries.Count() > 0 ? Query.And(queries.ToArray()) : null;

            try
            {
                return DataHelper.Populate<WEISPerson>("WEISPersons", query)
                    .FirstOrDefault()
                    .PersonInfos
                    .Where(d => d.RoleId.Equals(RoleId))
                    .FirstOrDefault()
                    .FullName;
            }
            catch (Exception e)
            {
                return "";
            }
        }

        [BsonIgnore]
        public string TeamLead
        {
            get { return GetPersonNameByRole("TEAM-LEAD"); }
        }

        [BsonIgnore]
        public string OptimizationEngineer
        {
            get { return GetPersonNameByRole("OPTMZ-ENG"); }
        }

        [BsonIgnore]
        public string LeadEngineer
        {
            get { return GetPersonNameByRole("LEAD-ENG"); }
        }

        [BsonIgnore]
        public double RealizedDays { get; set; }
        [BsonIgnore]
        public double RealizedCost { get; set; }
        [BsonIgnore]
        public double UnRealizedDays { get; set; }
        [BsonIgnore]
        public double UnRealizedCost { get; set; }
        [BsonIgnore]
        public double GapsDays { get; set; }
        [BsonIgnore]
        public double GapsCost { get; set; }

        private static double getValuePIP(string DefaultOP, List<PIPElement> PIPElements, string which, string what = "Days")
        {
            PIPElements.ForEach(x =>
            {
                if (x.Classification == null) x.Classification = string.Empty;
            });
            if (what == "Days")
            {
                var gg = PIPElements.Where(x => x.Classification.ToString().ToLower().Equals(which.ToLower()) && x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).ToList();
                var hh = PIPElements.Where(x => x.Classification.ToString().ToLower().Equals(which.ToLower()) && x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP) && x.Title.Equals("Deepening enabled design")).ToList();

                var dt = PIPElements.Where(x => x.Classification.ToString().ToLower().Equals(which.ToLower()) && x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.DaysCurrentWeekImprovement) +
                        PIPElements.Where(x => x.Classification.ToString().ToLower().Equals(which.ToLower()) && x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.DaysCurrentWeekRisk);
                return dt;
            }
            else
            {
                var gg = PIPElements.Where(x => x.Classification.ToString().ToLower().Equals(which.ToLower()) && x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).ToList();
                var hh = PIPElements.Where(x => x.Classification.ToString().ToLower().Equals(which.ToLower()) && x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP) && x.Title.Equals("Deepening enabled design")).ToList();
                var dt = PIPElements.Where(x => x.Classification.ToString().ToLower().Equals(which.ToLower()) && x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.CostCurrentWeekImprovement) +
                        PIPElements.Where(x => x.Classification.ToString().ToLower().Equals(which.ToLower()) && x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.CostCurrentWeekRisk);
                return dt;
            }
        }

        public static string getBaseOP(out string previousOP, out string nextOP)
        {
            string DefaultOP = "OP15";
            if (Config.GetConfigValue("BaseOPConfig") != null)
            {
                DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
            }
            else
            {
                var config1 = Config.Get<Config>("BaseOPConfig") ?? new Config() { _id = "BaseOPConfig", ConfigModule = "BaseOPDefault" };
                config1.ConfigValue = DefaultOP;
                config1.Save();
            }

            var DefaultOPYear = Convert.ToInt32(DefaultOP.Substring(DefaultOP.Length - 2, 2));
            nextOP = "OP" + ((DefaultOPYear + 1).ToString());
            previousOP = "OP" + ((DefaultOPYear - 1).ToString());
            return DefaultOP;
        }

        [BsonIgnore]
        public WellDrillData CalculatedLE
        {
            get
            {
                string previousOP = "";
                string nextOP = "";
                var DefaultOP = getBaseOP(out previousOP, out nextOP);

                //var realizedElements = this.Elements.Where(d => d.Completion.Equals("Realized") && d.Classification != null).DefaultIfEmpty(new PIPElement());

                //var totalRealizedCost = this.Elements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement) +
                //    this.Elements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.CostCurrentWeekRisk);

                //var totalRealizedDays = this.Elements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement) +
                //    this.Elements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.DaysCurrentWeekRisk);


                //var totalRealizedCostCurrentOP = PIPElements.Where(x => x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.CostCurrentWeekImprovement) +
                //    PIPElements.Where(x => x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.CostCurrentWeekRisk);

                //var totalRealizedDaysCurrentOP = PIPElements.Where(x => x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.DaysCurrentWeekImprovement) +
                //    PIPElements.Where(x => x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.DaysCurrentWeekRisk);

                var totalRealizedSupplyChainTransformationDays = getValuePIP(DefaultOP, this.Elements, "Supply Chain Transformation");
                var totalRealizedCompetitiveScopeDays = getValuePIP(DefaultOP, this.Elements, "Competitive Scope");
                var totalRealizedEfficientExecutionDays = getValuePIP(DefaultOP, this.Elements, "Efficient Execution");
                var totalRealizedTechnologyandInnovationDays = getValuePIP(DefaultOP, this.Elements, "Technology and Innovation");

                var totalRealizedSupplyChainTransformationCost = getValuePIP(DefaultOP, this.Elements, "Supply Chain Transformation", "Cost");
                var totalRealizedCompetitiveScopeCost = getValuePIP(DefaultOP, this.Elements, "Competitive Scope", "Cost");
                var totalRealizedEfficientExecutionCost = getValuePIP(DefaultOP, this.Elements, "Efficient Execution", "Cost");
                var totalRealizedTechnologyandInnovationCost = getValuePIP(DefaultOP, this.Elements, "Technology and Innovation", "Cost");

                var totalRealizedDaysCurrentOP = totalRealizedSupplyChainTransformationDays +
                                                 totalRealizedCompetitiveScopeDays + totalRealizedEfficientExecutionDays +
                                                 totalRealizedTechnologyandInnovationDays;

                var totalRealizedCostCurrentOP = totalRealizedSupplyChainTransformationCost +
                                                 totalRealizedCompetitiveScopeCost + totalRealizedEfficientExecutionCost +
                                                 totalRealizedTechnologyandInnovationCost;

                // Days

                var leDays = this.CurrentWeek.Days;
                var opDays = this.Plan.Days;

                var totalBankDays = this.BankedSavingsCompetitiveScope.Days + this.BankedSavingsEfficientExecution.Days + this.BankedSavingsSupplyChainTransformation.Days + this.BankedSavingsTechnologyAndInnovation.Days;
                var totalRealizedDays = totalRealizedDaysCurrentOP;//realizedElements.Sum(d => d.DaysCurrentWeekRisk);//d.DaysCurrentWeekImprovement + 
                var totalRealizedBankDays = totalBankDays + totalRealizedDays;

                var deltaDays = leDays - opDays;

                var calculatedDeltaDays = deltaDays - totalRealizedBankDays;
                var calculatedLEDays = opDays + totalRealizedBankDays;//leDays - calculatedDeltaDays;

                // Cost

                var leCost = this.CurrentWeek.Cost;
                var opCost = this.Plan.Cost / 1000000;

                var totalBankCost = this.BankedSavingsCompetitiveScope.Cost + this.BankedSavingsEfficientExecution.Cost + this.BankedSavingsSupplyChainTransformation.Cost + this.BankedSavingsTechnologyAndInnovation.Cost;
                var totalRealizedCost = totalRealizedCostCurrentOP;//realizedElements.Sum(d => d.CostCurrentWeekRisk);//d.CostCurrentWeekImprovement +
                var totalRealizedBankCost = totalBankCost + totalRealizedCost;

                var deltaCost = leCost - opCost;

                var calculatedDeltaCost = deltaCost - totalRealizedBankCost;
                var calculatedLECost = opCost + totalRealizedBankCost;//leCost - calculatedDeltaCost;

                return new WellDrillData()
                {
                    Days = calculatedLEDays,
                    Cost = calculatedLECost
                };
            }
        }


        [BsonIgnore]
        public List<YearCalc> DaysOfYearCalc { get; set; }

        public override void PostGet()
        {

            //if (this.Elements != null && this.Elements.Count() > 0)
            //{
            //    var RD = this.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekRisk + x.DaysCurrentWeekImprovement);
            //    var RC = this.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekRisk + x.CostCurrentWeekImprovement);

            //    var URD = this.Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekRisk + x.DaysCurrentWeekImprovement);
            //    var URC = this.Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekRisk + x.CostCurrentWeekImprovement);

            //    this.RealizedDays = Math.Round(RD,1);
            //    this.RealizedCost = Math.Round(RC,1);
            //    this.UnRealizedDays = Math.Round(URD,1);
            //    this.UnRealizedCost = Math.Round(URC,1);

            //    //Gap = LE - (OP14 + Realized PIP)
            //    var LEDays = this.Elements.Sum(x => x.DaysCurrentWeekRisk + x.DaysCurrentWeekImprovement);
            //    var LECost = this.Elements.Sum(x => x.CostCurrentWeekRisk + x.CostCurrentWeekImprovement);
            //    var OPDays = this.Elements.Sum(x => x.DaysPlanRisk + x.DaysPlanImprovement);
            //    var OPCost = this.Elements.Sum(x => x.CostPlanRisk + x.CostPlanImprovement);
            //    this.GapsDays = LEDays - (OPDays + RD);
            //    this.GapsCost = LECost - (OPCost + RC);
            //}
            //base.PostGet();

            //if (this.CRElements == null || this.CRElements.Count == 0)
            //{
            //    var qs = new List<IMongoQuery>();
            //    qs.Add(Query.EQ("WellName",this.WellName));
            //    qs.Add(Query.EQ("Phases.ActivityType",this.Phase.ActivityType));
            //    qs.Add(Query.EQ("UARigSequenceId",this.SequenceId));
            //    var RigName = WellActivity.Get<WellActivity>(Query.And(qs)).RigName+"_CR";

            //    var getPIPCR = WellPIP.Get<WellPIP>(Query.EQ("WellName", RigName));
            //    this.CRElements = getPIPCR.Elements;
            //}
            base.PostGet();
        }

        public static List<BsonDocument> GetPercentageComplete2(DateRange dr, string basedOn, List<string> Projects = null)
        {
            List<BsonDocument> bdocs = new List<BsonDocument>();
            DateTime start = new DateTime(dr.Start.Year, dr.Start.Month, dr.Start.Day);
            DateTime finish = new DateTime(dr.Finish.Year, dr.Finish.Month, dr.Finish.Day);
            var q = Query.Null;
            if (Projects != null && Projects.Count > 0)
            {
                q = Query.In("ProjectName", new BsonArray(Projects.ToArray()));
            }
            var WAs = WellActivity.Populate<WellActivity>(q);
            var ListProjects = WAs.Where(x => x.ProjectName != null).Select(x => x.ProjectName).Distinct().ToList();
            foreach (var t in ListProjects)
            {
                var EventMatchedProject1 = WAs.Where(x => t.Equals(x.ProjectName));
                if (EventMatchedProject1 != null)
                {
                    var EventMatchedProject = EventMatchedProject1.SelectMany(d => d.Phases, (p, d) => new
                                            {
                                                RigName = p.RigName,
                                                WellName = p.WellName,
                                                SequenceId = p.UARigSequenceId,
                                                ActivityType = d.ActivityType,
                                                PhSchedule = d.PhSchedule,
                                                PhStartYear = d.PhSchedule == null ? Tools.DefaultDate.Year : d.PhSchedule.Start.Year,
                                                IsActualLE = d.IsActualLE
                                            }).ToList();
                    dr.Start = start;
                    while (dr.Start <= dr.Finish)
                    {
                        BsonDocument d = new BsonDocument();
                        d.Set("_id", dr.Start.ToString("yyyy") + "-" + t);
                        d.Set("Date", dr.Start);
                        d.Set("ProjectName", t);
                        var matchedDataToYear = EventMatchedProject.Where(x => x.PhStartYear == dr.Start.Year).ToList();
                        if (matchedDataToYear != null && matchedDataToYear.Count > 0)
                        {
                            //ada event di tahun tsb
                            var totalData = matchedDataToYear.Where(x => x.PhStartYear == dr.Start.Year).ToList();
                            var adaLE = matchedDataToYear.Where(x => x.PhStartYear == dr.Start.Year && x.IsActualLE == true).Count();
                            var nonLE = matchedDataToYear.Where(x => x.PhStartYear == dr.Start.Year && x.IsActualLE == false).Count();

                            BsonArray detail = new BsonArray();
                            foreach (var b in totalData)
                            {
                                //var b = a.ToList().OrderByDescending(x => x.UpdateVersion).FirstOrDefault();
                                var contentDetail = new BsonDocument();
                                contentDetail.Set("WellName", b.WellName);
                                contentDetail.Set("SequenceId", b.SequenceId);
                                var RigName = "";
                                var getRigName = WellActivity.Get<WellActivity>(Query.And(new List<IMongoQuery>() { Query.EQ("WellName", b.WellName), Query.EQ("UARigSequenceId", b.SequenceId), Query.EQ("Phases.ActivityType", b.ActivityType) }));
                                if (getRigName != null)
                                {
                                    RigName = getRigName.RigName;
                                }
                                contentDetail.Set("RigName", RigName);
                                contentDetail.Set("IsActualLE", b.IsActualLE);
                                contentDetail.Set("ActivityType", b.ActivityType);
                                detail.Add(contentDetail);
                            }
                            d.Set("DetailHaveLE", detail);

                            d.Set("CountHaveLE", adaLE);
                            d.Set("CountDontHaveLE", nonLE);
                            d.Set("Total", totalData.Count);
                            var le = Tools.Div(Convert.ToDouble(adaLE), Convert.ToDouble(totalData.Count));
                            d.Set("LePercent", Math.Round(le, 2));
                            d.Set("adaEvent", true);
                        }
                        else
                        {
                            //no event
                            d.Set("CountHaveLE", 0);
                            d.Set("CountDontHaveLE", 0);
                            d.Set("Total", 0);
                            d.Set("LePercent", 0);
                            d.Set("adaEvent", false);
                            d.Set("DetailHaveLE", new BsonArray());
                        }

                        bdocs.Add(d);
                        dr.Start = dr.Start.AddYears(1);
                    }
                }
                else
                {
                    dr.Start = start;
                    while (dr.Start <= dr.Finish)
                    {
                        //no event
                        BsonDocument d = new BsonDocument();
                        d.Set("_id", dr.Start.ToString("yyyy") + "-" + t);
                        d.Set("Date", dr.Start);
                        d.Set("ProjectName", t);
                        d.Set("CountHaveLE", 0);
                        d.Set("CountDontHaveLE", 0);
                        d.Set("Total", 0);
                        d.Set("LePercent", 0);
                        d.Set("adaLE", false);
                        d.Set("DetailHaveLE", new BsonArray());

                        bdocs.Add(d);
                        dr.Start = dr.Start.AddYears(1);
                    }
                }
            }
            return bdocs;
        }

        public static List<BsonDocument> GetPercentageComplete(DateRange dr, string basedOn)
        {
            //DateRange dr = new DateRange(new DateTime(2015, 1, 1), new DateTime(2016, 12, 31));
            DateTime start = new DateTime(dr.Start.Year, dr.Start.Month, dr.Start.Day);
            List<BsonDocument> pipes = new List<BsonDocument>();
            string AggQuery = @"{ 
                                   $group : 
                                   {
                                        _id: 
                                        {
                                            ProjectName : '$ProjectName' ,
                                        },
                                    }
                                }";
            pipes.Add(BsonSerializer.Deserialize<BsonDocument>(AggQuery));
            var res = DataHelper.Aggregate("WEISWellActivities", pipes);
            List<BsonDocument> bdocs = new List<BsonDocument>();
            foreach (var t in res)
            {
                var projectName = BsonHelper.GetString(t["_id"].ToBsonDocument(), "ProjectName");
                var dataAll = new List<BsonDocument>();
                if (basedOn.ToLower() == "monthly")
                {
                    var montlhy = DataHelper.Populate("WEISWellActivityUpdatesMonthly", Query.EQ("Project", projectName));
                    dataAll.AddRange(montlhy);
                }
                if (basedOn.ToLower() == "weekly")
                {
                    var weekly = DataHelper.Populate("WEISWellActivityUpdates", Query.EQ("Project", projectName));
                    dataAll.AddRange(weekly);
                }

                dr.Start = start;
                while (dr.Start <= dr.Finish)
                {
                    BsonDocument d = new BsonDocument();
                    d.Set("_id", dr.Start.ToString("yyyy") + "-" + projectName);
                    d.Set("Date", dr.Start);
                    d.Set("ProjectName", projectName);

                    if (dataAll.Where(x => BsonHelper.GetDateTime(x, "UpdateVersion").Year == dr.Start.Year).Count() > 0)
                    {
                        //var data1 = dataAll.Where(x => BsonHelper.GetDateTime(x, "UpdateVersion").Year == dr.Start.Year).OrderByDescending(x => BsonHelper.GetDateTime(x, "UpdateVersion")).FirstOrDefault();
                        foreach (var a in dataAll)
                        {
                            a.Set("ActivityType", BsonHelper.GetString(a, "Phase.ActivityType"));
                        }
                        var data1 = dataAll.GroupBy(x => new { a = x.GetElement("WellName"), b = x.GetElement("SequenceId"), c = x.GetElement("ActivityType") });

                        var data2 = new List<BsonDocument>();

                        foreach (var a in data1)
                        {
                            var b = a.ToList().Where(x => BsonHelper.GetDateTime(x.ToBsonDocument(), "UpdateVersion").Year == dr.Start.Year).OrderByDescending(x => BsonHelper.GetDateTime(x.ToBsonDocument(), "UpdateVersion")).FirstOrDefault();
                            data2.Add(b);
                        }


                        var total = data2.Where(x => BsonHelper.GetDateTime(x, "UpdateVersion").Year == dr.Start.Year).ToList();
                        var getDocElements = data2.Where(x => BsonHelper.Get(x, "Elements") != BsonNull.Value).ToList();
                        if (getDocElements != null && getDocElements.Count > 0)
                        {

                            var adaLE = getDocElements.Where(x => BsonHelper.Get(x, "Elements").AsBsonArray.Where(z => BsonHelper.GetString(z.ToBsonDocument(), "Completion").Equals("Realized"))
                                        .Sum(y => BsonHelper.GetDouble(y.ToBsonDocument(), "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(y.ToBsonDocument(), "DaysCurrentWeekRisk")) != 0).ToList();
                            var nonLe = getDocElements.Where(x => BsonHelper.Get(x, "Elements").AsBsonArray.Where(z => BsonHelper.GetString(z.ToBsonDocument(), "Completion").Equals("Realized"))
                                       .Sum(y => BsonHelper.GetDouble(y.ToBsonDocument(), "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(y.ToBsonDocument(), "DaysCurrentWeekRisk")) == 0).ToList();
                            // var adaLE = BsonHelper.Get(getDocElements.ToBsonDocument(), "Elements").AsBsonArray.Where(x => BsonHelper.GetString(x.ToBsonDocument(), "Completion").Equals("Realized") && (BsonHelper.GetDouble(x.ToBsonDocument(), "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x.ToBsonDocument(), "DaysCurrentWeekRisk") != 0)).ToList();
                            //var nonLe = BsonHelper.Get(getDocElements.ToBsonDocument(), "Elements").AsBsonArray.Where(x => BsonHelper.GetString(x.ToBsonDocument(), "Completion").Equals("Realized") && (BsonHelper.GetDouble(x.ToBsonDocument(), "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x.ToBsonDocument(), "DaysCurrentWeekRisk") == 0)).ToList();
                            d.Set("CountHaveLE", System.Convert.ToDouble(adaLE.Count));
                            d.Set("CountDontHaveLE", System.Convert.ToDouble(nonLe.Count));
                            d.Set("Total", System.Convert.ToDouble(total.Count));

                            BsonArray detail = new BsonArray();
                            foreach (var m in adaLE)
                            {
                                var WellName = BsonHelper.GetString(m.ToBsonDocument(), "WellName");
                                var SequenceId = BsonHelper.GetString(m.ToBsonDocument(), "SequenceId");
                                var getRigName = WellActivity.Get<WellActivity>(Query.And(Query.EQ("WellName", WellName), Query.EQ("UARigSequenceId", SequenceId)));
                                var RigName = "";
                                if (getRigName != null)
                                {
                                    RigName = getRigName.RigName;
                                }
                                var Elements = BsonHelper.Get(m.ToBsonDocument(), "Elements").AsBsonArray.Where(x => BsonHelper.GetString(x.ToBsonDocument(), "Completion").Equals("Realized"));
                                if (Elements != null && Elements.Count() > 0)
                                {
                                    foreach (var e in Elements)
                                    {
                                        var contentDetail = new BsonDocument();
                                        var TotalLECost = Elements.Sum(x => BsonHelper.GetDouble(x.ToBsonDocument(), "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x.ToBsonDocument(), "CostCurrentWeekRisk"));
                                        var TotalLEDays = Elements.Sum(x => BsonHelper.GetDouble(x.ToBsonDocument(), "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x.ToBsonDocument(), "DaysCurrentWeekRisk"));
                                        var ActivityType = BsonHelper.GetString(m.ToBsonDocument(), "Phase.ActivityType");
                                        var UpdateVersion = BsonHelper.GetDateTime(m.ToBsonDocument(), "UpdateVersion");
                                        var Idea = BsonHelper.GetString(e.ToBsonDocument(), "Title");
                                        contentDetail.Set("WellName", WellName);
                                        contentDetail.Set("SequenceId", SequenceId);
                                        contentDetail.Set("RigName", RigName);
                                        contentDetail.Set("TotalLECost", TotalLECost);
                                        contentDetail.Set("TotalLEDays", TotalLEDays);
                                        contentDetail.Set("ActivityType", ActivityType);
                                        contentDetail.Set("UpdateVersion", UpdateVersion);
                                        contentDetail.Set("Idea", Idea);
                                        detail.Add(contentDetail);
                                    }
                                }
                            }

                            d.Set("DetailHaveLE", detail);

                            if (adaLE.Count == 0)
                            {
                                d.Set("LePercent", 0);
                            }
                            else
                            {
                                var le = Tools.Div(Convert.ToDouble(adaLE.Count()), Convert.ToDouble(total.Count()));
                                d.Set("LePercent", Math.Round(System.Convert.ToDouble(le), 2));
                            }
                        }
                    }
                    else
                    {
                        // warna putih
                        d.Set("CountHaveLE", 0);
                        d.Set("CountDontHaveLE", 0);
                        d.Set("Total", 0);
                        d.Set("LePercent", 0);
                    }

                    bdocs.Add(d);
                    dr.Start = dr.Start.AddYears(1);
                }

            }
            return bdocs;


        }

        [BsonIgnore]
        public WellDrillData _bankedSavingsSupplyChainTransformation;
        [BsonIgnore]
        public WellDrillData _bankedSavingsCompetitiveScope;
        [BsonIgnore]
        public WellDrillData _bankedSavingsEfficientExecution;
        [BsonIgnore]
        public WellDrillData _bankedSavingsTechnologyAndInnovation;
        [BsonIgnore]
        public WellDrillData _bestInClass;

        public WellDrillData BankedSavingsSupplyChainTransformation
        {
            get
            {
                if (_bankedSavingsSupplyChainTransformation == null) _bankedSavingsSupplyChainTransformation = new WellDrillData();
                return _bankedSavingsSupplyChainTransformation;
            }
            set { _bankedSavingsSupplyChainTransformation = value; }
        }

        public WellDrillData BankedSavingsCompetitiveScope
        {
            get
            {
                if (_bankedSavingsCompetitiveScope == null) _bankedSavingsCompetitiveScope = new WellDrillData();
                return _bankedSavingsCompetitiveScope;
            }
            set { _bankedSavingsCompetitiveScope = value; }
        }

        public WellDrillData BankedSavingsEfficientExecution
        {
            get
            {
                if (_bankedSavingsEfficientExecution == null) _bankedSavingsEfficientExecution = new WellDrillData();
                return _bankedSavingsEfficientExecution;
            }
            set { _bankedSavingsEfficientExecution = value; }
        }

        public WellDrillData BankedSavingsTechnologyAndInnovation
        {
            get
            {
                if (_bankedSavingsTechnologyAndInnovation == null) _bankedSavingsTechnologyAndInnovation = new WellDrillData();
                return _bankedSavingsTechnologyAndInnovation;
            }
            set { _bankedSavingsTechnologyAndInnovation = value; }
        }



        [BsonIgnore]
        public WellDrillData _RealizedPIPElemSupplyChainTransformation;
        [BsonIgnore]
        public WellDrillData _RealizedPIPElemCompetitiveScope;
        [BsonIgnore]
        public WellDrillData _RealizedPIPElemEfficientExecution;
        [BsonIgnore]
        public WellDrillData _RealizedPIPElemTechnologyAndInnovation;

        public WellDrillData RealizedPIPElemSupplyChainTransformation
        {
            get
            {
                if (_RealizedPIPElemSupplyChainTransformation == null) _RealizedPIPElemSupplyChainTransformation = new WellDrillData();
                return _RealizedPIPElemSupplyChainTransformation;
            }
            set { _RealizedPIPElemSupplyChainTransformation = value; }
        }

        public WellDrillData RealizedPIPElemCompetitiveScope
        {
            get
            {
                if (_RealizedPIPElemCompetitiveScope == null) _RealizedPIPElemCompetitiveScope = new WellDrillData();
                return _RealizedPIPElemCompetitiveScope;
            }
            set { _RealizedPIPElemCompetitiveScope = value; }
        }

        public WellDrillData RealizedPIPElemEfficientExecution
        {
            get
            {
                if (_RealizedPIPElemEfficientExecution == null) _RealizedPIPElemEfficientExecution = new WellDrillData();
                return _RealizedPIPElemEfficientExecution;
            }
            set { _RealizedPIPElemEfficientExecution = value; }
        }

        public WellDrillData RealizedPIPElemTechnologyAndInnovation
        {
            get
            {
                if (_RealizedPIPElemTechnologyAndInnovation == null) _RealizedPIPElemTechnologyAndInnovation = new WellDrillData();
                return _RealizedPIPElemTechnologyAndInnovation;
            }
            set { _RealizedPIPElemTechnologyAndInnovation = value; }
        }


        public WellDrillData BestInClass
        {
            get
            {
                if (_bestInClass == null) _bestInClass = new WellDrillData();
                return _bestInClass;
            }
            set { _bestInClass = value; }
        }

        public string CurrentEstimateMaturity { set; get; }
        public string NewEstimateMaturity { set; get; }
        [BsonIgnore]
        public List<WEISComment> Comments { set; get; }
        public string Comment { set; get; }

        [BsonIgnore]
        public DateRange OpsSchedule { set; get; }

        public string InitiateBy { get; set; }
        public DateTime InitiateDate { get; set; }

        public string LastUpdateBy { get; set; }

        public string SubmitBy { get; set; }

    }

    public class MonthlyLEComment
    {
        public MonthlyLEComment()
        {
            Date = DateTime.Now;
        }
        public DateTime Date { get; set; }
        public string UserName { get; set; }
        public string Comment { get; set; }
    }
    public class YearCalc
    {
        public int Year { get; set; }
        public float Value { get; set; }
        public string Header { get; set; }
        public string Title { get; set; }
    }


}