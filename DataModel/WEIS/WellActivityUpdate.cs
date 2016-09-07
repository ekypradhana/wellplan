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
    public class WellActivityUpdate : ECISModel
    {
        public WellActivityUpdate()
        {
            this.UpdateVersion = DateTime.Now;
            this.UpdateVersion = Tools.ToDateTime(this.UpdateVersion.ToString(), true);
            if (this.UpdateVersion.DayOfWeek != DayOfWeek.Monday)
            {
                int dow = (int)this.UpdateVersion.DayOfWeek;
                this.UpdateVersion = this.UpdateVersion.AddDays(-(dow - 1));
            }
        }
        public string WellEngineer { get; set; }
        public string Status { get; set; }
        public bool Archived { get; set; }
        public override string TableName
        {
            get
            {
                return "WEISWellActivityUpdates";
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
            var pip = WellPIP.GetByOpsActivity(this.SequenceId, this.Phase.ActivityType,this.WellName);
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
                    n.CostAvoidance = o.CostAvoidance;
                    n.Title = o.Title;

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

                var before = GetBefore();
                if (before == null)
                {
                    before = new WellActivityUpdate();
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
                var act = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(Phase.ActivityType));
                if (act != null)
                {
                    OP = act.OP;
                    AFE = act.AFE;
                    Plan = act.Plan;
                    TQ = act.TQ;

                    if (CurrentWeek.Days == 0) CurrentWeek.Days = LastWeek.Days;
                    if (CurrentWeek.Cost == 0) CurrentWeek.Cost = LastWeek.Cost;

                    if (CurrentWeek.Days == 0 && CurrentWeek.Cost == 0)
                    {
                        if (CurrentWeek.Days == 0) CurrentWeek.Days = act.LE.Days;
                        if (CurrentWeek.Cost == 0) CurrentWeek.Cost = act.LE.Cost;
                    }

                    if (CurrentWeek.Days == 0 && CurrentWeek.Cost == 0)
                    {
                        if (CurrentWeek.Days == 0) CurrentWeek.Days = act.OP.Days;
                        if (CurrentWeek.Cost == 0) CurrentWeek.Cost = act.OP.Cost;
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
                    LastWeek = OP;
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

        //public bool IsActualLE { get; set; }

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
                if (_drillData == null) _drillData = new WellDrillData();
                return _drillData;
            }

            set
            {
                _drillData = value;
            }
        }
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
        public string OperationSummary { get; set; }
        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            this.UpdateVersion = Tools.ToDateTime(this.UpdateVersion.ToString(), true);
            if (this.UpdateVersion.DayOfWeek != DayOfWeek.Monday)
            {
                int dow = (int)this.UpdateVersion.DayOfWeek;
                this.UpdateVersion = this.UpdateVersion.AddDays(-(dow - 1));
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
                var ignoreInitLE = references.Count() >= 1 && references.Contains("ignoreInitLE");
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
                                p.CostAvoidance = e.CostAvoidance;
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
                        pip.Save(references: new string[] { "IgnoreWAU" });
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
                    if (ignoreInitLE)
                    {
                    }
                    else
                    {
                        if (ph != null)
                        {
                            ph.initLEFromWeeklyReport(this);
                        }
                        wa.Save();
                    }
                    if (calcLESchedule)
                    {
                        WellActivity.UpdateLESchedule(new string[] { wa.RigName });
                    }
                }

            }

            if (references != null && references.Contains("updateLeStatus"))
            {
                var wa = WellActivity.Get<WellActivity>(Query.And(
                    Query.EQ("WellName", this.WellName),
                    Query.EQ("UARigSequenceId", this.SequenceId)
                    ));
                if(wa!=null)
                {
                    foreach(var p in wa.Phases.Where(x=>x.ActivityType.Equals(this.Phase.ActivityType)))
                    {
                        p.IsActualLE = true;
                    }
                    wa.Save();
                }

            }

            return this.ToBsonDocument();
        }
        public WellActivityUpdate GetBefore()
        {
            //-- get latest update
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("Phase.ActivityType", Phase.ActivityType));
            qs.Add(Query.EQ("SequenceId", SequenceId));
            qs.Add(Query.LT("UpdateVersion", UpdateVersion));
            var latest = WellActivityUpdate.Get<WellActivityUpdate>(Query.And(qs), SortBy.Descending("UpdateVersion"));
            return latest;
        }
        public WellActivityUpdate GetAfter()
        {
            //-- get latest update
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("Phase.ActivityType", Phase.ActivityType));
            qs.Add(Query.EQ("SequenceId", SequenceId));
            qs.Add(Query.GT("UpdateVersion", UpdateVersion));
            var latest = WellActivityUpdate.Get<WellActivityUpdate>(Query.And(qs), SortBy.Ascending("UpdateVersion"));
            return latest;
        }
        public static WellActivityUpdate GetById(
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
            WellActivityUpdate r = WellActivityUpdate.Get<WellActivityUpdate>(q, SortBy.Descending("UpdateVersion"));
            if (newIfNull && r == null) r = new WellActivityUpdate();
            return r;
        }

        public static List<WellActivityUpdate> GetLatestMonthly(
           DateTime month)
        {
            List<IMongoQuery> qs = new List<IMongoQuery>();
            DateTime from = new DateTime(month.Year,month.Month,1);
            DateTime to = new DateTime(month.Year,month.Month, DateTime.DaysInMonth(month.Year,month.Month));

             qs.Add(Query.GTE("UpdateVersion",  Tools.ToUTC(from) ));
             qs.Add(Query.LT("UpdateVersion",  Tools.ToUTC(to.AddDays(1)) ));
            IMongoQuery q = Query.And(qs);
            List<WellActivityUpdate> r = WellActivityUpdate.Populate<WellActivityUpdate>(q, 0, 0, SortBy.Descending("UpdateVersion"));
            if (r == null) 
                r = new List<WellActivityUpdate>();
            List<WellActivityUpdate> result = new List<WellActivityUpdate>();
            foreach (var res in r.GroupBy(x => x.WellName))
            {
                var data = res.ToList().OrderByDescending(x => x.UpdateVersion).FirstOrDefault();
                result.Add(data);
            }
            return result;
        }

        public static List<WellActivityUpdate> GetLatestMonthly(
         DateRange daterange, string status)
        {
            List<IMongoQuery> qs = new List<IMongoQuery>();
            DateTime from = new DateTime(daterange.Start.Year, daterange.Start.Month, daterange.Start.Day);
            DateTime to = new DateTime(daterange.Finish.Year, daterange.Finish.Month, daterange.Finish.Day);

            qs.Add(Query.GTE("UpdateVersion", Tools.ToUTC(from)));
            qs.Add(Query.LT("UpdateVersion", Tools.ToUTC(to.AddDays(1))));
            qs.Add(Query.EQ("Status", status));
            IMongoQuery q = Query.And(qs);
            List<WellActivityUpdate> r = WellActivityUpdate.Populate<WellActivityUpdate>(q, 0, 0, SortBy.Descending("UpdateVersion"));
            if (r == null)
                r = new List<WellActivityUpdate>();
            List<WellActivityUpdate> result = new List<WellActivityUpdate>();
            foreach (var res in r.GroupBy(x => x.WellName))
            {
                var data = res.ToList().OrderByDescending(x => x.UpdateVersion).FirstOrDefault();
                result.Add(data);
            }
            return result;
        }

        public override void PostDelete()
        {
            var latest = GetBefore();
            var after = GetAfter();
            if (after == null && latest != null) latest.Save("", "CalcLESchedule");
        }

        public static List<WellActivityUpdate> PopulateLast(
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
            var r = WellActivityUpdate.Populate<WellActivityUpdate>(q, take, sort: SortBy.Descending("UpdateVersion"));
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

        public static void UpdateLatestStatusOnAllWellPlanWhichHaveWeeklyReport()
        {
            WellActivity.Populate<WellActivity>().ForEach(d =>
            {
                if (d.Phases.Any())
                {
                    d.Phases.ForEach(e =>
                    {
                        string status = null;

                        var wau = WellActivityUpdate.Get<WellActivityUpdate>(Query.And(
                            Query.EQ("WellName", d.WellName),
                            Query.EQ("Phase.ActivityType", e.ActivityType),
                            Query.EQ("SequenceId", d.UARigSequenceId)
                        ), SortBy.Descending("UpdateVersion"));

                        if (wau != null)
                        {
                            if (wau.Status != null && wau.Status != "")
                            {
                                status = wau.Status;
                            }
                        }

                        UpdateLatestStatusOnWellPlan(d.WellName, e.ActivityType, d.UARigSequenceId, status);
                    });
                }
            });
        }

        public static void UpdateLatestStatusOnWellPlan(string WellName, string ActivityType, string SequenceId, string status)
        {
            var wa = WellActivity.Get<WellActivity>(Query.And(
                Query.EQ("WellName", WellName),
                Query.EQ("Phases.ActivityType", ActivityType),
                Query.EQ("UARigSequenceId", SequenceId)
            ));

            var phase = wa.Phases.FirstOrDefault(d => ActivityType.Equals(d.ActivityType));
            if (phase != null)
            {
                phase.LatestWeeklyReportStatus = status;
            }
            DataHelper.Save("WEISWellActivities", wa.ToBsonDocument());
            //wa.Save();
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

        public WellDrillData GetLatestActual()
        {
            var actual = new WellDrillData();
            var qs = new List<IMongoQuery>();

            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("SequenceId", SequenceId));
            qs.Add(Query.EQ("Phase.PhaseNo", Phase.PhaseNo));
            qs.Add(Query.EQ("Phase.ActivityType", Phase.ActivityType));
            qs.Add(Query.LTE("UpdateVersion",UpdateVersion));
            var wau = WellActivityUpdate.Populate<WellActivityUpdate>(Query.And(qs));
            if (wau.Any())
            {
                actual = wau.OrderByDescending(x => x.UpdateVersion).FirstOrDefault().Actual;
            }
            return actual;
        }


    }
}