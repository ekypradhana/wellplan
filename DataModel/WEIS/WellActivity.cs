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
    public class WellActivity : ECISModel
    {
        public WellActivity()
        {
            Phases = new List<WellActivityPhase>();
        }
        public override string TableName
        {
            get { return "WEISWellActivities"; }
        }
        private List<OPHistory> _ophistories;
        public List<OPHistory> OPHistories
        {
            get
            {
                if (_ophistories == null) _ophistories = new List<OPHistory>();
                return _ophistories;
            }
            set { _ophistories = value; }
        }

        public bool NonOP { get; set; }
        public string Region { get; set; }
        public string RigType { get; set; }
        public string RigName { get; set; }
        public string OperatingUnit { get; set; }
        public string ProjectName { get; set; }
        public string AssetName { get; set; }
        public string WellName { get; set; }
        public double WorkingInterest { get; set; }
        public string FirmOrOption { get; set; }
        public string UARigSequenceId { get; set; }
        public string UARigDescription { get; set; }

        public bool VirtualPhase { get; set; }
        public bool ShiftFutureEventDate { get; set; }

        private Dictionary<string, WellDrillData> _targets;
        public Dictionary<string, WellDrillData> Targets
        {
            get
            {
                if (_targets == null)
                {
                    _targets = new Dictionary<string, WellDrillData>();
                    _targets.Add("M0", new WellDrillData());
                    _targets.Add("M1", new WellDrillData());
                    _targets.Add("M2", new WellDrillData());
                    _targets.Add("M3", new WellDrillData());
                    _targets.Add("M4", new WellDrillData());
                }
                return _targets;
            }
            set
            {
                _targets = value;
            }
        }
        public double OpsDuration
        {
            get
            {
                return OpsSchedule == null ? 0 : OpsSchedule.Days;
            }
        }
        private DateRange _LESchedule;
        public DateRange OpsSchedule { get; set; }
        public DateRange PsSchedule { get; set; }
        public DateRange LESchedule
        {
            get
            {
                if (_LESchedule == null) _LESchedule = new DateRange();
                return _LESchedule;
            }
            set { _LESchedule = value; }
        }
        private DateRange _AFESchedule;
        [BsonIgnore]
        public DateRange AFESchedule
        {
            get
            {
                if (_AFESchedule == null) _AFESchedule = new DateRange();
                return _AFESchedule;
            }
            set { _AFESchedule = value; }
        }

        public List<WellActivityPhase> Phases { get; set; }
        public WellActivityPhase GetPhase(string activityType)
        {
            if (Phases == null) Phases = new List<WellActivityPhase>();
            return Phases.FirstOrDefault(d => d.ActivityType.Equals(activityType));
        }
        public WellActivityPhase GetPhase(int phaseNo)
        {
            if (Phases == null) Phases = new List<WellActivityPhase>();
            return Phases.FirstOrDefault(d => d.PhaseNo.Equals(phaseNo));
        }

        private string GetPersonNameByRole(string RoleId)
        {
            List<IMongoQuery> queries = new List<IMongoQuery>();
            queries.Add(Query.EQ("WellName", WellName));
            queries.Add(Query.EQ("SequenceId", UARigSequenceId));

            BsonArray qactivity = new BsonArray();
            foreach (var activity in Phases.Select(d => d.ActivityType)) qactivity.Add(activity);
            queries.Add(Query.In("ActivityType", qactivity));

            BsonArray qphases = new BsonArray();
            foreach (var phaseNo in Phases.Select(d => d.PhaseNo)) qphases.Add(phaseNo);
            queries.Add(Query.In("PhaseNo", qphases));

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

        public static DateTime GetLastExecuteDateLS()
        {
            var latest = LogLatestLSDate.Get<LogLatestLSDate>(null);
            if (latest == null)
                latest = new LogLatestLSDate() { Executed_At = Tools.DefaultDate };
            DateTime dd = Tools.ToUTC(latest.Executed_At);
            return dd;
        }

        public static DateTime GetLSDate()
        {
            var latest = LogLatestLSDate.Get<LogLatestLSDate>(null);
            if (latest == null)
                latest = new LogLatestLSDate() { LSDate = Tools.DefaultDate };
            DateTime dd = Tools.ToUTC(latest.LSDate);
            return dd;
        }
        public static void SetLastDateUploadedLS(DateTime? exeAt, DateTime LSDate, string executeBy)
        {
            var rec = LogLatestLSDate.Get<LogLatestLSDate>(null) ?? new LogLatestLSDate();
            if (exeAt != null)
            {
                rec.LSDate = Tools.ToUTC(LSDate, true);
                rec.Executed_At = Tools.ToUTC(exeAt.Value);
            }
            else
            {
                rec.LSDate = Tools.ToUTC(LSDate, true);
            }
            rec.Executed_By = executeBy;
            DataHelper.Delete(rec.TableName);
            rec.Save();
        }
        public BsonDocument CreateNewActivityUpdateMonthly(WellActivity wa, int PhaseToSave, string SourceFrom)
        {
            var wau = new WellActivityUpdateMonthly();
            var pss = wa.Phases;
            if (pss != null)
            {
                if (pss.Any())
                {
                    var singlePhase = pss.FirstOrDefault(x => x.PhaseNo == PhaseToSave);
                    if (singlePhase != null && singlePhase.PhSchedule.Start > Tools.DefaultDate &&
                        singlePhase.PhSchedule.Start > DateTime.Now && !isHaveMonthlyLEReport(wa.WellName, wa.UARigSequenceId, singlePhase.ActivityType))
                    {

                        //var fiscal = WEISFinancialCalendar.Get<WEISFinancialCalendar>(Query.EQ("Status", "Active"));

                        var lastMLEDate = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(null, sort: SortBy.Descending("UpdateVersion"));

                        wau.WellName = wa.WellName;
                        wau.SequenceId = wa.UARigSequenceId ?? "";


                        string DefaultOP = "OP15";
                        if (Config.GetConfigValue("BaseOPConfig") != null)
                        {
                            DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
                        }

                        if (singlePhase.BaseOP != null && singlePhase.BaseOP.Count() > 0 && singlePhase.BaseOP.OrderByDescending(x => x.ToString()).FirstOrDefault()
                            .Equals(DefaultOP))
                        {
                            wau.Plan = singlePhase.Plan;
                            wau.OP = singlePhase.OP;
                            wau.AFE = singlePhase.AFE;
                            wau.CurrentWeek = singlePhase.LE;

                            wau.Status = "In-Progress";
                            wau.Phase = singlePhase;
                            wau.UpdateVersion = Tools.ToUTC(lastMLEDate.UpdateVersion);
                            wau.LastUpdate = Tools.ToUTC(DateTime.Now.Date);
                            var preSv = wau.PreSave(wau.ToBsonDocument());
                            preSv.Set("UpdateVersion", wau.UpdateVersion);
                            DataHelper.Save(new WellActivityUpdateMonthly().TableName, preSv.ToBsonDocument());
                            return preSv;
                        }
                        else
                        {
                            if (wa.OPHistories != null && wa.OPHistories.Count() > 0)
                            {
                                var ophis = wa.OPHistories.Where(x => x.WellName.Equals(wa.WellName) && x.ActivityType.Equals(singlePhase.ActivityType) && x.Type.Equals(DefaultOP));
                                if (ophis != null && ophis.Count() > 0)
                                {
                                    var oph = ophis.FirstOrDefault();
                                    wau.Plan = oph.Plan;
                                    wau.OP = singlePhase.OP;
                                    wau.AFE = singlePhase.AFE;
                                    wau.CurrentWeek = singlePhase.LE;

                                    wau.Status = "In-Progress";
                                    wau.Phase = new WellActivityPhase();
                                    wau.Phase.SequenceOnRig = singlePhase.SequenceOnRig;
                                    wau.Phase.BaseOP = singlePhase.BaseOP;
                                    wau.Phase.IsInPlan = singlePhase.IsInPlan;
                                    wau.Phase.PhaseNo = singlePhase.PhaseNo;
                                    wau.Phase.ActivityType = singlePhase.ActivityType;
                                    wau.Phase.ActivityDesc = singlePhase.ActivityDesc;

                                    wau.Phase.CalcOPSchedule = new DateRange();
                                    wau.Phase.CalcOPSchedule.Start = singlePhase.CalcOPSchedule.Start;
                                    wau.Phase.CalcOPSchedule.Finish = singlePhase.CalcOPSchedule.Finish;

                                    wau.Phase.VirtualPhase = singlePhase.VirtualPhase;
                                    wau.Phase.ShiftFutureEventDate = singlePhase.ShiftFutureEventDate;


                                    wau.Phase.LESchedule = new DateRange();
                                    wau.Phase.LESchedule.Start = singlePhase.LESchedule.Start;
                                    wau.Phase.LESchedule.Finish = singlePhase.LESchedule.Finish;

                                    wau.Phase.LMESchedule = new DateRange();
                                    wau.Phase.LMESchedule.Start = singlePhase.LMESchedule.Start;
                                    wau.Phase.LMESchedule.Finish = singlePhase.LMESchedule.Finish;


                                    wau.Phase.LWESchedule = new DateRange();
                                    wau.Phase.LWESchedule.Start = singlePhase.LWESchedule.Start;
                                    wau.Phase.LWESchedule.Finish = singlePhase.LWESchedule.Finish;


                                    wau.Phase.PlanSchedule = new DateRange();
                                    wau.Phase.PlanSchedule.Start = singlePhase.PlanSchedule.Start;
                                    wau.Phase.PlanSchedule.Finish = singlePhase.PlanSchedule.Finish;

                                    wau.Phase.PhSchedule = new DateRange();
                                    wau.Phase.PhSchedule.Start = singlePhase.PhSchedule.Start;
                                    wau.Phase.PhSchedule.Finish = singlePhase.PhSchedule.Finish;

                                    wau.Phase.FundingType = singlePhase.FundingType;
                                    wau.Phase.IsActualLE = singlePhase.IsActualLE;
                                    wau.Phase.LevelOfEstimate = singlePhase.LevelOfEstimate;


                                    wau.TQ = new WellDrillData();
                                    wau.TQ.Days = singlePhase.TQ.Days;
                                    wau.TQ.Cost = singlePhase.TQ.Cost;

                                    wau.Phase.AggredTarget = new WellDrillData();
                                    wau.Phase.AggredTarget.Days = singlePhase.AggredTarget.Days;
                                    wau.Phase.AggredTarget.Cost = singlePhase.AggredTarget.Cost;


                                    wau.Phase.BIC = new WellDrillData();
                                    wau.Phase.BIC.Days = singlePhase.BIC.Days;
                                    wau.Phase.BIC.Cost = singlePhase.BIC.Cost;

                                    wau.Phase.IsPostOP = singlePhase.IsPostOP;


                                    wau.Phase.AFE = new WellDrillData();
                                    wau.Phase.AFE.Days = singlePhase.AFE.Days;
                                    wau.Phase.AFE.Cost = singlePhase.AFE.Cost;


                                    wau.Phase.Plan = new WellDrillData();
                                    wau.Phase.Plan.Days = oph.Plan.Days;
                                    wau.Phase.Plan.Cost = oph.Plan.Cost;


                                    wau.Phase.Actual = new WellDrillData();
                                    wau.Phase.Actual.Days = singlePhase.Actual.Days;
                                    wau.Phase.Actual.Cost = singlePhase.Actual.Cost;



                                    wau.Phase.CalcOP = new WellDrillData();
                                    wau.Phase.CalcOP.Days = singlePhase.CalcOP.Days;
                                    wau.Phase.CalcOP.Cost = singlePhase.CalcOP.Cost;


                                    wau.Phase.OP = new WellDrillData();
                                    wau.Phase.OP.Days = singlePhase.OP.Days;
                                    wau.Phase.OP.Cost = singlePhase.OP.Cost;


                                    wau.Phase.LE = new WellDrillData();
                                    wau.Phase.LE.Days = singlePhase.LE.Days;
                                    wau.Phase.LE.Cost = singlePhase.LE.Cost;


                                    wau.Phase.LWE = new WellDrillData();
                                    wau.Phase.LWE.Days = singlePhase.LWE.Days;
                                    wau.Phase.LWE.Cost = singlePhase.LWE.Cost;

                                    wau.Phase.LME = new WellDrillData();
                                    wau.Phase.LME.Days = singlePhase.LME.Days;
                                    wau.Phase.LME.Cost = singlePhase.LME.Cost;

                                    wau.Phase.LastMonth = singlePhase.LastMonth;

                                    wau.UpdateVersion = Tools.ToUTC(lastMLEDate.UpdateVersion);
                                    wau.LastUpdate = Tools.ToUTC(DateTime.Now.Date);
                                    var preSv = wau.PreSave(wau.ToBsonDocument());
                                    preSv.Set("UpdateVersion", wau.UpdateVersion);
                                    DataHelper.Save(new WellActivityUpdateMonthly().TableName, preSv.ToBsonDocument());
                                    return preSv;
                                }
                                else
                                {
                                    wau.Plan = singlePhase.Plan;
                                    wau.OP = singlePhase.OP;
                                    wau.AFE = singlePhase.AFE;
                                    wau.CurrentWeek = singlePhase.LE;

                                    wau.Status = "In-Progress";
                                    wau.Phase = singlePhase;
                                    wau.UpdateVersion = Tools.ToUTC(lastMLEDate.UpdateVersion);
                                    wau.LastUpdate = Tools.ToUTC(DateTime.Now.Date);
                                    var preSv = wau.PreSave(wau.ToBsonDocument());
                                    preSv.Set("UpdateVersion", wau.UpdateVersion);
                                    DataHelper.Save(new WellActivityUpdateMonthly().TableName, preSv.ToBsonDocument());
                                    return preSv;
                                }
                            }
                            else
                            {
                                wau.Plan = singlePhase.Plan;
                                wau.OP = singlePhase.OP;
                                wau.AFE = singlePhase.AFE;
                                wau.CurrentWeek = singlePhase.LE;

                                wau.Status = "In-Progress";
                                wau.Phase = singlePhase;
                                wau.UpdateVersion = Tools.ToUTC(lastMLEDate.UpdateVersion);
                                wau.LastUpdate = Tools.ToUTC(DateTime.Now.Date);
                                var preSv = wau.PreSave(wau.ToBsonDocument());
                                preSv.Set("UpdateVersion", wau.UpdateVersion);
                                DataHelper.Save(new WellActivityUpdateMonthly().TableName, preSv.ToBsonDocument());
                                return preSv;
                            }
                        }


                    }
                    else
                        return null;
                }
                else
                    return null;
            }
            else
                return null;
        }
        private string GetPersonEmailByRole(string RoleId)
        {
            List<IMongoQuery> queries = new List<IMongoQuery>();
            queries.Add(Query.EQ("WellName", WellName));
            queries.Add(Query.EQ("SequenceId", UARigSequenceId));

            BsonArray qactivity = new BsonArray();
            foreach (var activity in Phases.Select(d => d.ActivityType)) qactivity.Add(activity);
            queries.Add(Query.In("ActivityType", qactivity));

            BsonArray qphases = new BsonArray();
            foreach (var phaseNo in Phases.Select(d => d.PhaseNo)) qphases.Add(phaseNo);
            queries.Add(Query.In("PhaseNo", qphases));

            IMongoQuery query = queries.Count() > 0 ? Query.And(queries.ToArray()) : null;

            try
            {
                return DataHelper.Populate<WEISPerson>("WEISPersons", query)
                    .FirstOrDefault()
                    .PersonInfos
                    .Where(d => d.RoleId.Equals(RoleId))
                    .FirstOrDefault()
                    .Email;
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
        public string TeamLeadEmail
        {
            get { return GetPersonEmailByRole("TEAM-LEAD"); }
        }

        [BsonIgnore]
        public string OptimizationEngineerEmail
        {
            get { return GetPersonEmailByRole("OPTMZ-ENG"); }
        }

        [BsonIgnore]
        public string LeadEngineerEmail
        {
            get { return GetPersonEmailByRole("LEAD-ENG"); }
        }

        [BsonIgnore]
        public DateTime EstimateFirstOilDate
        {
            get
            {
                if (Phases == null || Phases.Count == 0) return Tools.DefaultDate;
                var phCompletion = Phases.FirstOrDefault(e => e.ActivityType.ToLower().Contains("completion"));
                if (phCompletion != null)
                    return phCompletion.LE.Days == 0 ? phCompletion.PhSchedule.Finish : phCompletion.PhSchedule.Start.AddDays(phCompletion.LE.Days);
                else
                    return Tools.DefaultDate;
            }
        }
        public string PerformanceUnit { get; set; }

        private DateTime _firstOilDate;
        public DateTime FirstOilDate
        {
            get { return _firstOilDate; }
            set { _firstOilDate = value; }
        }


        private List<string> _AssignTOOps;
        public List<string> AssignTOOps
        {
            get
            {
                if (_AssignTOOps == null) _AssignTOOps = new List<string>();
                return _AssignTOOps;
            }
            set { _AssignTOOps = value; }
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            if (doc.GetInt64("_id") == 0)
            {
                var newId = SequenceNo.Get(this.TableName).ClaimAsInt();
                _id = newId;
                if (UARigSequenceId == null || UARigSequenceId == "")
                {
                    int newUAID = SequenceNo.Get("UARigSequenceId").ClaimAsInt();
                    UARigSequenceId = String.Format("UA{0}", newUAID);
                }
            }
            foreach (var ph in Phases)
            {
                //if (ph.LE == null) ph.LE = new WellDrillData();
                //if (ph.LE.Days == 0 && ph.LE.Cost == 0)
                //{
                //    if (ph.AFE.Days != 0 || ph.AFE.Cost != 0)
                //    {
                //        ph.LE = ph.AFE;
                //    }
                //    else
                //    {
                //        ph.LE = ph.OP;
                //    }
                //}


                //if (references != null && references.Contains("manualDaysValue"))
                //{

                //}
                //else
                //{
                //    ph.Plan.Days = ph.PlanSchedule.Days;
                //    ph.OP.Days = ph.PhSchedule.Days;
                //}

                ph.VirtualPhase = this.VirtualPhase;
                ph.ShiftFutureEventDate = this.ShiftFutureEventDate;
            }

            var noRisks = Phases.Where(d => d.ActivityType.ToLower().Contains("risk") == false);
            if (noRisks.Count() > 0)
            {
                OpsSchedule = new DateRange
                {
                    Start = noRisks.Where(d => d.PhSchedule.Start > Tools.DefaultDate).Count() == 0 ? Tools.DefaultDate : noRisks.Where(d => d.PhSchedule.Start > Tools.DefaultDate).Min(d => d.PhSchedule.Start),
                    Finish = noRisks.Where(d => d.PhSchedule.Start > Tools.DefaultDate).Count() == 0 ? Tools.DefaultDate : noRisks.Where(d => d.PhSchedule.Finish > Tools.DefaultDate).Max(d => d.PhSchedule.Finish)
                };

                LESchedule = new DateRange
                {
                    Start = noRisks.Where(d => d.LESchedule.Start > Tools.DefaultDate).Count() == 0 ? Tools.DefaultDate :
                        noRisks.Where(d => d.LESchedule.Start > Tools.DefaultDate).Min(d => d.LESchedule.Start),
                    Finish = noRisks.Where(d => d.LESchedule.Finish > Tools.DefaultDate).Count() == 0 ? Tools.DefaultDate :
                        noRisks.Where(d => d.LESchedule.Finish > Tools.DefaultDate).Max(d => d.LESchedule.Finish)
                };
            }
            if (Phases.Count > 0)
            {
                PsSchedule = new DateRange
                {
                    Start = Phases.Where(d => d.PlanSchedule.Finish > Tools.DefaultDate).Count() == 0 ? Tools.DefaultDate :
                        Phases.Where(d => d.PlanSchedule.Finish > Tools.DefaultDate).Min(d => d.PlanSchedule.Start),
                    Finish = Phases.Where(d => d.PlanSchedule.Finish > Tools.DefaultDate).Count() == 0 ? Tools.DefaultDate :
                        Phases.Where(d => d.PlanSchedule.Finish > Tools.DefaultDate).Max(d => d.PlanSchedule.Finish)
                };
            }
            return this.ToBsonDocument();
        }

        public override void PostGet()
        {
            foreach (var p in this.Phases)
            {
                var infos = WellActivityPhaseInfo.Populate<WellActivityPhaseInfo>(
                    Query.And(
                        Query.EQ("WellName", this.WellName),
                        Query.EQ("SequenceId", this.UARigSequenceId)
                    ));
                if (infos != null && infos.Count() > 0)
                {
                    var haveInfo = infos.Where(x => x.PhaseNo == p.PhaseNo).ToList();
                    if (haveInfo != null && haveInfo.Count() > 0)
                    {
                        p.PhaseInfo = haveInfo.FirstOrDefault();
                    }
                    else
                    {
                        p.PhaseInfo = new WellActivityPhaseInfo();
                    }
                }
                else
                {
                    p.PhaseInfo = new WellActivityPhaseInfo();
                }

                p.BizPlanStatus = "";
                var qsBP = new List<IMongoQuery>();
                qsBP.Add(Query.EQ("WellName", WellName));
                qsBP.Add(Query.EQ("UARigSequenceId", UARigSequenceId));
                qsBP.Add(Query.EQ("Phases.ActivityType", p.ActivityType));
                var bizplan = BizPlanActivity.Get<BizPlanActivity>(Query.And(qsBP));
                if (bizplan != null)
                {
                    var matchedPhase = bizplan.Phases.Where(x => x.ActivityType == p.ActivityType).FirstOrDefault();
                    if (matchedPhase != null)
                    {
                        p.BizPlanStatus = matchedPhase.Estimate.Status;
                    }
                }

            }
            base.PostGet();
        }

        public override BsonDocument PostSave(BsonDocument doc, string[] references = null)
        {
            if (references == null) references = new string[] { };
            if (references.Count() > 0 && references[0] == "SyncWeeklyReport")
            {
                //-- get Actual
                WellActivityActual actual = WellActivityActual.GetById(WellName, UARigSequenceId, (DateTime?)DateTime.Now.ToUniversalTime().Date, false, false);
                if (actual == null)
                {
                    actual = new WellActivityActual();
                    actual.WellName = WellName;
                    actual.SequenceId = UARigSequenceId;
                    actual.UpdateVersion = Tools.ToUTC(DateTime.Now, true);
                    actual.Actual = new List<WellActivityActualItem>();
                    actual.AFE = new List<WellActivityActualItem>();
                }
                foreach (var p in Phases)
                {
                    WellActivityActualItem afe = actual.AFE.FirstOrDefault(d => d.PhaseNo.Equals(p.PhaseNo));
                    if (afe == null || (afe != null
                        && afe.Data.Days != p.AFE.Days && afe.Data.Cost != p.AFE.Cost
                        && p.AFE.Days != 0))
                    {
                        if (afe == null)
                        {
                            afe = new WellActivityActualItem(p.PhaseNo);
                            actual.AFE.Add(afe);
                        }
                        afe.Data = p.AFE;
                        afe.Data.Comment = "Manual Update at " + DateTime.Now.ToString();
                    }

                    WellActivityActualItem act = actual.Actual.FirstOrDefault(d => d.PhaseNo.Equals(p.PhaseNo));
                    if (act == null || (act != null
                        && act.Data.Days != p.Actual.Days && act.Data.Cost != p.Actual.Cost
                        && p.Actual.Days != 0))
                    {
                        if (act == null)
                        {
                            act = new WellActivityActualItem(p.PhaseNo);
                            actual.Actual.Add(act);
                        }
                        act.Data = p.Actual;
                        act.Data.Comment = "Manual Update at " + DateTime.Now.ToString();
                    }
                }
                actual.Save(references: new string[] { "UnsyncWA" });
            }

            if (references.Count() > 0 && references.ToList().Where(x => x.ToLower().Equals("updatetobizplan")).Count() > 0)
            {

                if(this.WellName.Equals("CALYPSO"))
                {

                }
                //Push to bizplan
                var wa = this;
                var qsBP = new List<IMongoQuery>();
                qsBP.Add(Query.EQ("WellName", this.WellName));
                qsBP.Add(Query.EQ("UARigSequenceId", this.UARigSequenceId));
                qsBP.Add(Query.EQ("RigName", this.RigName));
                var bpa = BizPlanActivity.Get<BizPlanActivity>(Query.And(qsBP));
                if (bpa == null)
                {
                    //create new bizplan

                    var newBP = BsonSerializer.Deserialize<BizPlanActivity>(wa.ToBsonDocument().Set("BizPlanId", "OPPlan"));
                    var newBPPhases = new List<BizPlanActivityPhase>();
                    if (wa.Phases.Any())
                    {
                        foreach (var ph in wa.Phases)
                        {
                            if (ph.PushToBizPlan)
                            {
                                #region convert to bizplan phase

                                var newPhases = BsonSerializer.Deserialize<BizPlanActivityPhase>(ph.ToBsonDocument());

                                var t = WellActivity.isHaveWeeklyReport(wa.WellName, wa.UARigSequenceId, newPhases.ActivityType);
                                if ((newPhases.PhSchedule.Start > DateTime.Now) && t == false)
                                {
                                    var LEDays = newPhases.LE;
                                    double LENewDays = 0.0;
                                    if (LEDays == null)
                                    {
                                        LENewDays = 0.0;
                                    }
                                    else
                                    {
                                        LENewDays = LEDays.Days;
                                    }
                                    newPhases.LESchedule = new DateRange() { Start = newPhases.PhSchedule.Start, Finish = newPhases.PhSchedule.Start.AddDays(LENewDays) };

                                    #region calc estimate
                                    var x = newBP;
                                    var p = newPhases;

                                    x.Country = "";
                                    x.ReferenceFactorModel = "";
                                    x.Currency = "";



                                    if (wa.WorkingInterest <= 1)
                                        x.ShellShare = wa.WorkingInterest * 100;
                                    else
                                        x.ShellShare = wa.WorkingInterest;

                                    #region phase info
                                    var infos = WellActivityPhaseInfo.Populate<WellActivityPhaseInfo>(
                                                Query.And(
                                                    Query.EQ("WellName", x.WellName),
                                                    Query.EQ("SequenceId", x.UARigSequenceId)
                                                ));
                                    if (infos != null && infos.Count() > 0)
                                    {
                                        var haveInfo = infos.Where(d => d.PhaseNo == p.PhaseNo).ToList();
                                        if (haveInfo != null && haveInfo.Count() > 0)
                                        {
                                            p.PhaseInfo = haveInfo.FirstOrDefault();
                                        }
                                        else
                                        {
                                            p.PhaseInfo = new WellActivityPhaseInfo();
                                        }
                                    }
                                    else
                                    {
                                        p.PhaseInfo = new WellActivityPhaseInfo();
                                    }

                                    #endregion

                                    #region estimate

                                    p.Estimate.EventStartDate = p.PhSchedule.Start;
                                    p.Estimate.RigName = x.RigName;
                                    p.Estimate.UsingTAApproved = true;
                                    p.Estimate.MaturityLevel = "Type 0";
                                    p.Estimate.isFirstInitiate = true;
                                    p.Estimate.Country = "";
                                    p.Estimate.Currency = "";
                                    p.Estimate.SaveToOP = "OP16";
                                    p.Estimate.isGenerateFromSync = true;

                                    //TROUBLE FREE
                                    if (p.CalcOP != null)
                                    {
                                        p.Estimate.CurrentTroubleFree = new WellDrillData() { Days = p.CalcOP.Days, Cost = Tools.Div(p.CalcOP.Cost, 1000000) };
                                    }

                                    if (p.LE != null && p.LE.Days > 0)
                                    {
                                        p.Estimate.NewTroubleFree = new WellDrillData() { Days = p.LE.Days, Cost = Tools.Div(p.LE.Cost, 1000000) };
                                        p.Estimate.TroubleFreeBeforeLC = new WellDrillData() { Days = p.LE.Days, Cost = Tools.Div(p.LE.Cost, 1000000) };
                                    }

                                    //NPT
                                    var getMaturityRisk = MaturityRiskMatrix.Get<MaturityRiskMatrix>(Query.EQ("Title", "Type 0")) ?? new MaturityRiskMatrix();
                                    var NPTTime = p.Estimate.NewTroubleFree.Days * Tools.Div(Tools.Div(getMaturityRisk.NPTTime, 100), 1 - Tools.Div(getMaturityRisk.NPTTime, 100));
                                    var NPTCost = p.Estimate.NewTroubleFree.Cost * Tools.Div(Tools.Div(getMaturityRisk.NPTCost, 100), 1 - Tools.Div(getMaturityRisk.NPTCost, 100));

                                    p.Estimate.NewNPTTime = new WellDrillPercentData() { Days = NPTTime, Cost = NPTCost, PercentCost = getMaturityRisk.NPTCost, PercentDays = getMaturityRisk.NPTTime };

                                    p.Estimate.CurrentNPTTime = new WellDrillPercentData() { Days = 0, Cost = 0 };

                                    //TECOP
                                    var TECOPTime = p.Estimate.NewTroubleFree.Days * Tools.Div(Tools.Div(getMaturityRisk.TECOPTime, 100), 1 - Tools.Div(getMaturityRisk.TECOPTime, 100));
                                    var TECOPCost = p.Estimate.NewTroubleFree.Cost * Tools.Div(Tools.Div(getMaturityRisk.TECOPCost, 100), 1 - Tools.Div(getMaturityRisk.TECOPCost, 100));

                                    p.Estimate.NewTECOPTime = new WellDrillPercentData() { PercentDays = getMaturityRisk.TECOPTime, PercentCost = getMaturityRisk.TECOPCost, Days = TECOPTime, Cost = TECOPCost };

                                    p.Estimate.NewMean.Days = p.Estimate.NewTroubleFree.Days + p.Estimate.NewNPTTime.Days + p.Estimate.NewTECOPTime.Days;
                                    p.Estimate.EventEndDate = p.Estimate.EventStartDate.AddDays(p.Estimate.NewMean.Days);


                                    //USD Convert
                                    p.Estimate.NewTroubleFreeUSD = p.Estimate.NewTroubleFree.Cost;
                                    p.Estimate.NPTCostUSD = p.Estimate.NewNPTTime.Cost;
                                    p.Estimate.TECOPCostUSD = p.Estimate.NewTECOPTime.Cost;
                                    p.Estimate.MeanUSD = p.Estimate.NewMean.Cost;

                                    //Period
                                    p.Estimate.EstimatePeriod = new DateRange() { Start = p.Estimate.EventStartDate, Finish = p.Estimate.EventEndDate };

                                    //material long lead
                                    var DateEscStartMaterial = p.Estimate.EventStartDate;
                                    var tangibleValue = 0.0;
                                    var monthRequired = 0.0;
                                    var actType = "";

                                    //getActCategory
                                    var getActCategory = ActivityMaster.Get<ActivityMaster>(Query.EQ("_id", p.ActivityType));
                                    if (getActCategory != null)
                                    {
                                        actType = getActCategory.ActivityCategory;
                                    }

                                    if (actType != null && actType != "")
                                    {
                                        var year = p.Estimate.EventStartDate.Year;
                                        var getLongLead = LongLead.Get<LongLead>(Query.EQ("Title", actType));
                                        if (getLongLead != null && getLongLead.Details != null && getLongLead.Details.Count > 0)
                                        {
                                            var getTangible = getLongLead.Details.Where(y => y.Year.Equals(year)).FirstOrDefault();
                                            if (getTangible != null)
                                            {
                                                tangibleValue = getTangible.TangibleValue;
                                                monthRequired = getTangible.MonthRequiredValue;
                                                var getMonthLongLead = Convert.ToInt32(-1 * Math.Round(monthRequired));
                                                DateEscStartMaterial = p.Estimate.EventStartDate.AddMonths(getMonthLongLead);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        tangibleValue = 0;
                                        monthRequired = 0;
                                        var getMonthLongLead = Convert.ToInt32(-1 * Math.Round(monthRequired));
                                        DateEscStartMaterial = p.Estimate.EventStartDate.AddMonths(getMonthLongLead);
                                    }

                                    if (DateEscStartMaterial < DateTime.Now)
                                    {
                                        DateEscStartMaterial = DateTime.Now;
                                    }

                                    p.Estimate.StartEscDateMaterial = DateEscStartMaterial;
                                    p.Estimate.PercOfMaterialsLongLead = tangibleValue;
                                    p.Estimate.LongLeadMonthRequired = monthRequired;

                                    var rigRates = BizPlanAllocation.GetRigRate(p.Estimate.RigName, p.Estimate.EventStartDate.Year);

                                    p.Estimate.RigRate = rigRates;
                                    p.Estimate.Services = 0;
                                    p.Estimate.Materials = 0;

                                    p.Estimate.WellValueDriver = "";
                                    p.Estimate.Status = "Draft";
                                    p.Estimate.SelectedCurrency = "";
                                    //meta data missing
                                    if (String.IsNullOrEmpty(newBP.PerformanceUnit) || String.IsNullOrEmpty(newBP.AssetName) || String.IsNullOrEmpty(newBP.Region) || String.IsNullOrEmpty(newBP.ProjectName) || String.IsNullOrEmpty(newBP.Country) || String.IsNullOrEmpty(newBP.ReferenceFactorModel) || newBP.ReferenceFactorModel == "default")
                                    {
                                        p.Estimate.Status = "Meta Data Missing";
                                    }
                                    #endregion


                                    p.Estimate.LastUpdate = Tools.ToUTC(DateTime.Now);
                                    p.Estimate.LastUpdateBy = ph.userupdate;
                                    x.DataInputBy = ph.userupdate;
                                    #endregion
                                    newBPPhases.Add(newPhases);
                                }
                                #endregion
                            }
                        }
                    }
                    newBP.Phases = newBPPhases;

                    newBP.Save();
                    BizPlanAllocation.SaveBizPlanAllocation(newBP);

                    var lastid = BizPlanActivity.Get<BizPlanActivity>(null, sort: SortBy.Descending("_id"));
                    int id = Convert.ToInt32(lastid._id);
                    BsonDocument dd = new BsonDocument();
                    dd.Set("_id", "WEISBizPlanActivities");
                    dd.Set("Title", "WEISBizPlanActivities");
                    dd.Set("NextNo", id + 1);
                    dd.Set("Format", "");
                    DataHelper.Save("SequenceNos", dd);

                }
                else
                {

                    //update bizplan phases from wellactivity phase where push to bizplan = true
                    bpa.Region = wa.Region;
                    bpa.OperatingUnit = wa.OperatingUnit;
                    bpa.RigType = wa.RigType;
                    bpa.ProjectName = wa.ProjectName;
                    bpa.AssetName = wa.AssetName;
                    bpa.PerformanceUnit = wa.PerformanceUnit;
                    bpa.FirmOrOption = wa.FirmOrOption;
                    bpa.WorkingInterest = wa.WorkingInterest;
                    bpa.ShellShare = wa.WorkingInterest;
                    bpa.UARigDescription = wa.UARigDescription;
                    if (!bpa.Phases.Any())
                        bpa.Phases = new List<BizPlanActivityPhase>();

                    var newBizPlanPhases = new List<BizPlanActivityPhase>();

                    if (wa.Phases.Any())
                    {
                        foreach (var ph in wa.Phases)
                        {
                            if (ph.PushToBizPlan)
                            {

                                var checkMatchedBPPhase = bpa.Phases.Where(x => x.ActivityType.Equals(ph.ActivityType));
                                if (checkMatchedBPPhase.Any())
                                {
                                    var matchedBPPhase = checkMatchedBPPhase.FirstOrDefault();
                                    var t = WellActivity.isHaveWeeklyReport(wa.WellName, wa.UARigSequenceId, ph.ActivityType);
                                    if ((ph.PhSchedule.Start > DateTime.Now) && t == false)
                                    {
                                        //update value PhSchedule, OP
                                        matchedBPPhase.OP = ph.OP;
                                        matchedBPPhase.PhSchedule = ph.PhSchedule;
                                    }
                                    else
                                    {
                                        //respective bizplan phase has to be deleted
                                        bpa.Phases.Remove(matchedBPPhase);
                                    }
                                }
                                else
                                {
                                    //create new BizPlanPhase

                                    #region convert to bizplan phase

                                    var newPhases = BsonSerializer.Deserialize<BizPlanActivityPhase>(ph.ToBsonDocument());

                                    var t = WellActivity.isHaveWeeklyReport(wa.WellName, wa.UARigSequenceId, newPhases.ActivityType);
                                    if ((newPhases.PhSchedule.Start > DateTime.Now) && t == false)
                                    {
                                        var LEDays = newPhases.LE;
                                        double LENewDays = 0.0;
                                        if (LEDays == null)
                                        {
                                            LENewDays = 0.0;
                                        }
                                        else
                                        {
                                            LENewDays = LEDays.Days;
                                        }
                                        newPhases.LESchedule = new DateRange() { Start = newPhases.PhSchedule.Start, Finish = newPhases.PhSchedule.Start.AddDays(LENewDays) };

                                        #region calc estimate
                                        var x = bpa;
                                        var p = newPhases;

                                        x.Country = "";
                                        x.ReferenceFactorModel = "";
                                        x.Currency = "";

                                        if (wa.WorkingInterest <= 1)
                                            x.ShellShare = wa.WorkingInterest * 100;
                                        else
                                            x.ShellShare = wa.WorkingInterest;

                                        #region phase info
                                        var infos = WellActivityPhaseInfo.Populate<WellActivityPhaseInfo>(
                                                    Query.And(
                                                        Query.EQ("WellName", x.WellName),
                                                        Query.EQ("SequenceId", x.UARigSequenceId)
                                                    ));
                                        if (infos != null && infos.Count() > 0)
                                        {
                                            var haveInfo = infos.Where(d => d.PhaseNo == p.PhaseNo).ToList();
                                            if (haveInfo != null && haveInfo.Count() > 0)
                                            {
                                                p.PhaseInfo = haveInfo.FirstOrDefault();
                                            }
                                            else
                                            {
                                                p.PhaseInfo = new WellActivityPhaseInfo();
                                            }
                                        }
                                        else
                                        {
                                            p.PhaseInfo = new WellActivityPhaseInfo();
                                        }

                                        #endregion

                                        #region estimate

                                        p.Estimate.EventStartDate = p.PhSchedule.Start;
                                        p.Estimate.RigName = x.RigName;
                                        p.Estimate.UsingTAApproved = true;
                                        p.Estimate.MaturityLevel = "Type 0";
                                        p.Estimate.isFirstInitiate = true;
                                        p.Estimate.Country = "";
                                        p.Estimate.Currency = "";
                                        p.Estimate.SaveToOP = "OP16";
                                        p.Estimate.isGenerateFromSync = true;

                                        //TROUBLE FREE
                                        if (p.CalcOP != null)
                                        {
                                            p.Estimate.CurrentTroubleFree = new WellDrillData() { Days = p.CalcOP.Days, Cost = Tools.Div(p.CalcOP.Cost, 1000000) };
                                        }

                                        if (p.LE != null && p.LE.Days > 0)
                                        {
                                            p.Estimate.NewTroubleFree = new WellDrillData() { Days = p.LE.Days, Cost = Tools.Div(p.LE.Cost, 1000000) };
                                            p.Estimate.TroubleFreeBeforeLC = new WellDrillData() { Days = p.LE.Days, Cost = Tools.Div(p.LE.Cost, 1000000) };
                                        }

                                        //NPT
                                        var getMaturityRisk = MaturityRiskMatrix.Get<MaturityRiskMatrix>(Query.EQ("Title", "Type 0")) ?? new MaturityRiskMatrix();
                                        var NPTTime = p.Estimate.NewTroubleFree.Days * Tools.Div(Tools.Div(getMaturityRisk.NPTTime, 100), 1 - Tools.Div(getMaturityRisk.NPTTime, 100));
                                        var NPTCost = p.Estimate.NewTroubleFree.Cost * Tools.Div(Tools.Div(getMaturityRisk.NPTCost, 100), 1 - Tools.Div(getMaturityRisk.NPTCost, 100));

                                        p.Estimate.NewNPTTime = new WellDrillPercentData() { Days = NPTTime, Cost = NPTCost, PercentCost = getMaturityRisk.NPTCost, PercentDays = getMaturityRisk.NPTTime };

                                        p.Estimate.CurrentNPTTime = new WellDrillPercentData() { Days = 0, Cost = 0 };

                                        //TECOP
                                        var TECOPTime = p.Estimate.NewTroubleFree.Days * Tools.Div(Tools.Div(getMaturityRisk.TECOPTime, 100), 1 - Tools.Div(getMaturityRisk.TECOPTime, 100));
                                        var TECOPCost = p.Estimate.NewTroubleFree.Cost * Tools.Div(Tools.Div(getMaturityRisk.TECOPCost, 100), 1 - Tools.Div(getMaturityRisk.TECOPCost, 100));

                                        p.Estimate.NewTECOPTime = new WellDrillPercentData() { PercentDays = getMaturityRisk.TECOPTime, PercentCost = getMaturityRisk.TECOPCost, Days = TECOPTime, Cost = TECOPCost };

                                        p.Estimate.NewMean.Days = p.Estimate.NewTroubleFree.Days + p.Estimate.NewNPTTime.Days + p.Estimate.NewTECOPTime.Days;
                                        p.Estimate.EventEndDate = p.Estimate.EventStartDate.AddDays(p.Estimate.NewMean.Days);


                                        //USD Convert
                                        p.Estimate.NewTroubleFreeUSD = p.Estimate.NewTroubleFree.Cost;
                                        p.Estimate.NPTCostUSD = p.Estimate.NewNPTTime.Cost;
                                        p.Estimate.TECOPCostUSD = p.Estimate.NewTECOPTime.Cost;
                                        p.Estimate.MeanUSD = p.Estimate.NewMean.Cost;

                                        //Period
                                        p.Estimate.EstimatePeriod = new DateRange() { Start = p.Estimate.EventStartDate, Finish = p.Estimate.EventEndDate };

                                        //material long lead
                                        var DateEscStartMaterial = p.Estimate.EventStartDate;
                                        var tangibleValue = 0.0;
                                        var monthRequired = 0.0;
                                        var actType = "";

                                        //getActCategory
                                        var getActCategory = ActivityMaster.Get<ActivityMaster>(Query.EQ("_id", p.ActivityType));
                                        if (getActCategory != null)
                                        {
                                            actType = getActCategory.ActivityCategory;
                                        }

                                        if (actType != null && actType != "")
                                        {
                                            var year = p.Estimate.EventStartDate.Year;
                                            var getLongLead = LongLead.Get<LongLead>(Query.EQ("Title", actType));
                                            if (getLongLead != null && getLongLead.Details != null && getLongLead.Details.Count > 0)
                                            {
                                                var getTangible = getLongLead.Details.Where(y => y.Year.Equals(year)).FirstOrDefault();
                                                if (getTangible != null)
                                                {
                                                    tangibleValue = getTangible.TangibleValue;
                                                    monthRequired = getTangible.MonthRequiredValue;
                                                    var getMonthLongLead = Convert.ToInt32(-1 * Math.Round(monthRequired));
                                                    DateEscStartMaterial = p.Estimate.EventStartDate.AddMonths(getMonthLongLead);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            tangibleValue = 0;
                                            monthRequired = 0;
                                            var getMonthLongLead = Convert.ToInt32(-1 * Math.Round(monthRequired));
                                            DateEscStartMaterial = p.Estimate.EventStartDate.AddMonths(getMonthLongLead);
                                        }

                                        if (DateEscStartMaterial < DateTime.Now)
                                        {
                                            DateEscStartMaterial = DateTime.Now;
                                        }

                                        p.Estimate.StartEscDateMaterial = DateEscStartMaterial;
                                        p.Estimate.PercOfMaterialsLongLead = tangibleValue;
                                        p.Estimate.LongLeadMonthRequired = monthRequired;

                                        var rigRates = BizPlanAllocation.GetRigRate(p.Estimate.RigName, p.Estimate.EventStartDate.Year);

                                        p.Estimate.RigRate = rigRates;
                                        p.Estimate.Services = 0;
                                        p.Estimate.Materials = 0;

                                        p.Estimate.WellValueDriver = "";
                                        p.Estimate.Status = "Draft";
                                        p.Estimate.SelectedCurrency = "";


                                        p.Estimate.LastUpdate = Tools.ToUTC(DateTime.Now);
                                        p.Estimate.LastUpdateBy = ph.userupdate;
                                        x.DataInputBy = ph.userupdate;

                                        //meta data missing
                                        if (String.IsNullOrEmpty(bpa.ProjectName) || String.IsNullOrEmpty(bpa.Country) || String.IsNullOrEmpty(bpa.ReferenceFactorModel) || bpa.ReferenceFactorModel == "default")
                                        {
                                            p.Estimate.Status = "Meta Data Missing";
                                        }
                                        #endregion


                                        #endregion
                                        bpa.Phases.Add(newPhases);
                                    }
                                    #endregion

                                }
                            }
                        }
                    }
                    bpa.Save();
                }
            }
            return this.ToBsonDocument();
        }
        public static WellActivity GetByUniqueKey(
            string wellname,
            string rigsequenceid,
            bool create = false)
        {
            var q = Query.And(Query.EQ("WellName", wellname), Query.EQ("UARigSequenceId", rigsequenceid));
            WellActivity ret = WellActivity.Get<WellActivity>(q);
            if (ret == null && create)
            {
                ret = new WellActivity();
                ret._id = 0;
                ret.UARigSequenceId = rigsequenceid;
                ret.WellName = wellname;
            }
            return ret;
        }

        public static WellActivity GetByWellAndLastOP(
           string wellname,
           string op,
           bool create = false)
        {
            var q = Query.EQ("WellName", wellname);
            var res = WellActivity.Populate<WellActivity>(q);
            WellActivity ret = new WellActivity();
            if (res.Any())
            {
                var y = res.OrderByDescending(x => x.LastUpdate).FirstOrDefault();
                if (y.AssignTOOps.Contains(op))
                {
                    return y;
                }
                else
                {
                    ret._id = 0;
                    ret.WellName = wellname;
                    return ret;
                }
            }
            else
            {
                ret._id = 0;
                ret.WellName = wellname;
                return ret;

            }
        }
        public bool isHaveWeeklyReport2(string well, string seq, string events)
        {
            var t = WellActivityUpdate.Populate<WellActivityUpdate>(
                Query.And(
                    Query.EQ("WellName", well),
                    Query.EQ("SequenceId", seq),
                    Query.EQ("Phase.ActivityType", events)
                )
                );
            if (t != null && t.Count() > 0)
                return true;
            return false;
        }

        public static List<WellActivityHelper> CheckhaveWeeklyReport(List<WellActivityHelper> sequenceActivities)
        {
            string gabung = "";
            foreach (var t in sequenceActivities)
            {
                gabung = gabung + "{'SequenceId' : '" + t.SequenceId + "' , 'Phase.ActivityType' : '" + t.ActivityType + "'},";
            }


            string aggregateCond2 = @" {'$group' :{'_id' : { 'Seqid' : '$SequenceId', 'ActivityType' : '$Phase.ActivityType'  }}}";
            List<BsonDocument> pipelines = new List<BsonDocument>();

            string match = "";
            if (!gabung.Trim().Equals(""))
            {
                match = @"{'$match':  {'$or' :[ " + gabung + " ]}  }";
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(match));
            }

            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
            List<BsonDocument> aggregates = DataHelper.Aggregate(new WellActivityUpdate().TableName, pipelines);


            List<WellActivityHelper> results = new List<WellActivityHelper>();
            foreach (var a in aggregates)
            {
                results.Add(new WellActivityHelper { SequenceId = BsonHelper.GetString(a, "_id.Seqid"), ActivityType = BsonHelper.GetString(a, "_id.ActivityType") });
            }
            return results;

        }

        public static bool isHaveWeeklyReport(string well, string seq, string events)
        {
            var t = WellActivityUpdate.Populate<WellActivityUpdate>(
                Query.And(
                    Query.EQ("WellName", well),
                    Query.EQ("SequenceId", seq),
                    Query.EQ("Phase.ActivityType", events)
                )
                );
            if (t != null && t.Count() > 0)
                return true;
            return false;
        }
        public static List<WellActivityUpdate> CheckWeeklyReport(string well, string seq, string events)
        {
            var t = WellActivityUpdate.Populate<WellActivityUpdate>(
                Query.And(
                    Query.EQ("WellName", well),
                    Query.EQ("SequenceId", seq),
                    Query.EQ("Phase.ActivityType", events)
                )
                );
            if (t.Any())
                return t.OrderByDescending(x => x.UpdateVersion).ToList();
            else
                return null;
        }

        public static bool isHaveMonthlyLEReport(string well, string seq, string events)
        {
            var t = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(
                Query.And(
                    Query.EQ("WellName", well),
                    Query.EQ("SequenceId", seq),
                    Query.EQ("Phase.ActivityType", events)
                )
                );
            if (t != null && t.Count() > 0)
            {
                var lastMonthly = t.OrderByDescending(x => x.UpdateVersion).FirstOrDefault();
                if (lastMonthly.Phase.IsActualLE == true)
                    return true;
                else
                    return false;
            }
            return false;
        }

        public static DateRange GetAFESchedulePhases(WellActivity wa)
        {
            DateRange afesc = new DateRange();
            afesc.Start = Tools.DefaultDate;
            afesc.Finish = Tools.DefaultDate;
            if (wa.Phases.Any())
            {
                afesc.Start = wa.Phases.Min(x => x.AFESchedule.Start);
                afesc.Finish = wa.Phases.Max(x => x.AFESchedule.Finish);
            }
            return afesc;
        }
        public static DateRange GetAFESchedulePhases(List<WellActivity> was)
        {
            DateRange afesc = new DateRange();
            afesc.Start = Tools.DefaultDate;
            afesc.Finish = Tools.DefaultDate;
            if (was.Any())
            {
                afesc.Start = was.SelectMany(x => x.Phases).Select(x => x.AFESchedule.Start).Min();
                afesc.Finish = was.SelectMany(x => x.Phases).Select(x => x.AFESchedule.Finish).Max();
            }
            return afesc;
        }

        public static void LoadOP(string tableName = "_OP14_20150108", bool isSaveWa = true)
        {
            //DataHelper.Delete(new WellActivity().TableName);
            WellActivity prevWA = new WellActivity();
            prevWA.UARigSequenceId = "";
            List<BsonDocument> msts = DataHelper.Populate(tableName)
                //.Where(d=>d.GetString("Well_Name").Equals("MARS A3 Inj BHST"))
                .OrderBy(d => d.GetString("_id")).ToList();
            bool newSequenceId = true;
            DateTime lastFinishDate = new DateTime();
            foreach (var mst in msts)
            {
                var totalDuration = mst.GetDouble("DURATION_TOTAL_DURATION");
                var activityType = mst.GetString("Activity_Type\r\n(PTW_Buckets)");


                if (totalDuration != 0 && activityType != "")
                {
                    var UARigSequenceId = mst.GetString("ID", prevWA.UARigSequenceId);
                    if (UARigSequenceId.Contains("_"))
                    {
                        var ids = UARigSequenceId.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                        UARigSequenceId = ids[0];
                    }
                    WellActivity wa = WellActivity.GetByUniqueKey(mst.GetString("Well_Name"), UARigSequenceId, true);
                    if (Convert.ToInt64(wa._id) == 0)
                    {
                        wa.EXType = "";
                        string ext = mst.GetString("Funding_Type", prevWA.EXType);
                        if (ext.Trim().ToUpper().Equals("EXPEX"))
                            wa.EXType = "EXPEX";
                        else if (ext.Trim().ToUpper().Equals("ABEX"))
                            wa.EXType = "ABEX";
                        else if (ext.Trim().ToUpper().Contains("CAPEX"))
                            wa.EXType = "CAPEX";
                        else wa.EXType = "OPEX";

                        wa.Region = mst.GetString("Region", prevWA.Region);
                        wa.RigType = mst.GetString("Rig_Type", prevWA.RigType);
                        wa.RigName = mst.GetString("Rig_Name", prevWA.RigName);
                        wa.OperatingUnit = mst.GetString("Operating_Unit", prevWA.OperatingUnit);
                        wa.PerformanceUnit = mst.GetString("Performance_Unit", prevWA.PerformanceUnit);
                        wa.AssetName = mst.GetString("Asset_Name", prevWA.AssetName);
                        wa.ProjectName = mst.GetString("Project_Name", prevWA.ProjectName);
                        wa.WellName = mst.GetString("Well_Name", prevWA.WellName);
                        wa.WorkingInterest = mst.GetDouble("Working_Interest", 0);
                        wa.FirmOrOption = mst.GetString("Firm_or_Option", prevWA.FirmOrOption);
                        wa.UARigDescription = mst.GetString("Description", prevWA.UARigDescription);
                        wa.OpsSchedule = new DateRange(mst.GetDateTime("Ops_Start", true), mst.GetDateTime("Ops_Finish", true));
                        //if (mst.GetDateTime("PS_Start", true) < new DateTime(2000,1,1))
                        //{
                        //    wa.PsSchedule = new DateRange(mst.GetDateTime("PS_Finish", true), mst.GetDateTime("PS_Dur", true));
                        //}
                        //else
                        //{
                        //    wa.PsSchedule = new DateRange(mst.GetDateTime("PS_Start", true), mst.GetDateTime("PS_Finish", true));
                        //}
                        newSequenceId = true;
                    }
                    else
                    {
                        newSequenceId = false;
                    }

                    wa.UARigSequenceId = UARigSequenceId;


                    WellActivityPhase ph = new WellActivityPhase();
                    ph.ActivityType = activityType;
                    ph.ActivityDesc = mst.GetString("Activity_Desc \r\n(Rig_Seq_desc)");
                    ph.ActivityDescEst = mst.GetString("Activity_Desc \r\n(PTW_Est_name)");
                    ph.RiskFlag = mst.GetString("Risk_Flag", "");
                    ph.FundingType = mst.GetString("Funding_Type", "");
                    ph.Grouping = mst.GetString("Grouping_(Expl,_Major_Proj,_Other)", "");
                    ph.EscalationGroup = mst.GetString("Escalation_Group");
                    ph.CSOGroup = mst.GetString("CSO_Group");
                    ph.LevelOfEstimate = mst.GetString("Level_of_Estimate", "");
                    //ph.AFESchedule = new DateRange(mst.GetDateTime("Start LE"), mst.GetDateTime("Finish LE"));

                    if (newSequenceId)
                    {
                        lastFinishDate = wa.OpsSchedule.Start.AddDays(mst.GetInt32("DURATION_TOTAL_DURATION"));
                        ph.PhSchedule = new DateRange(wa.OpsSchedule.Start, lastFinishDate);
                    }
                    else
                    {
                        var startDate = mst.GetDateTime("Ops_Start", true);
                        if (startDate.Year < 2000) startDate = lastFinishDate.AddDays(1);
                        lastFinishDate = startDate.AddDays(mst.GetInt32("DURATION_TOTAL_DURATION"));
                        ph.PhSchedule = new DateRange(startDate, lastFinishDate);
                    }
                    ph.PlanSchedule = new DateRange(mst.GetDateTime("Ph_Start"), mst.GetDateTime("Ph_Finish"));
                    if (ph.PhSchedule.Start.Year < 2000) ph.PhSchedule = ph.PlanSchedule;
                    ph.AFESchedule = ph.PhSchedule;
                    ph.LESchedule = ph.PhSchedule;
                    ph.AFE = new WellDrillData();
                    ph.OP = new WellDrillData
                    {
                        Days = ph.PhSchedule.Days,
                        Cost = mst.GetDouble("COST_TOTAL_COST")
                    };
                    ph.Plan = ph.OP;
                    ph.TQ = new WellDrillData
                    {
                        Days = mst.GetDouble("MEASURES_TQ_Duration"),
                        Cost = mst.GetDouble("MEASURES_TQ_Cost")
                    };
                    ph.LE = new WellDrillData
                    {
                        Days = mst.GetDouble("DURATION_TOTAL_DURATION"),
                        Cost = mst.GetDouble("COST_TOTAL_COST")
                    };
                    ph.PhaseNo = wa.Phases.Count + 1;
                    wa.Phases.Add(ph);
                    wa.PsSchedule = new DateRange(wa.Phases.Min(d => d.PlanSchedule.Start), wa.Phases.Max(d => d.PlanSchedule.Finish));
                    var noRiskPhases = wa.Phases.Where(d => d.ActivityType.ToLower().Contains("risk") == false).ToList();
                    if (noRiskPhases != null && noRiskPhases.Count > 0)
                        wa.OpsSchedule = new DateRange(noRiskPhases.Min(e => e.PhSchedule.Start), noRiskPhases.Max(e => e.PhSchedule.Finish));
                    //wa.Phases.RemoveAll(d => d.ActivityType.Equals("n/a"));

                    if (isSaveWa)
                        wa.Save();

                    prevWA = wa;

                    #region PhaseInfo
                    WellActivityPhaseInfo pinfo = new WellActivityPhaseInfo();
                    pinfo.RigName = wa.RigName;// doc.GetString("Rig_Name");
                    pinfo.WellName = wa.WellName;//doc.GetString("Well_Name");
                    pinfo.ActivityType = ph.ActivityType;// doc.GetString("Activity_Type\r\n(PTW_Buckets)");
                    pinfo.SequenceId = wa.UARigSequenceId;
                    pinfo.WellActivityId = wa._id.ToString();

                    if (wa.Phases.Where(x => x.ActivityType.Equals(pinfo.ActivityType)) != null &&
                        wa.Phases.Where(x => x.ActivityType.Equals(pinfo.ActivityType)).Count() > 0
                        )
                        pinfo.PhaseNo = wa.Phases.Where(x => x.ActivityType.Equals(pinfo.ActivityType)).FirstOrDefault().PhaseNo;
                    else
                        pinfo.PhaseNo = wa.Phases.Count() + 1;

                    //WellActivity.Get<WellActivity>(Query.And(
                    //Query.EQ("WellName", wa.WellName),
                    //Query.EQ("UARigSequenceId", wa.UARigSequenceId)
                    //)
                    //).Phases.Where(x => x.ActivityType.Equals(pinfo.ActivityType)).FirstOrDefault().PhaseNo;//   ph.PhaseNo;
                    pinfo.LoE = mst.GetString("Level_of_Estimate");

                    pinfo.TotalDuration = new WellDrillData
                    {
                        Days = mst.GetDouble("DURATION_TOTAL_DURATION"),
                        Cost = mst.GetDouble("COST_TOTAL_COST")
                    };
                    pinfo.TroubleFree = new WellDrillData
                    {
                        Days = mst.GetDouble("DURATION_Trouble_Free"),
                        Cost = mst.GetDouble("COST_Trouble_Free")
                    };

                    pinfo.Trouble = new WellDrillData
                    {
                        Days = mst.GetDouble("DURATION_Trouble"),
                        Cost = mst.GetDouble("COST_Trouble")
                    };

                    pinfo.Contigency = new WellDrillData
                    {
                        Days = mst.GetDouble("DURATION_Contigency"),
                        Cost = mst.GetDouble("COST_Contigency")
                    };
                    pinfo.TQ = new WellDrillData
                    {
                        Days = mst.GetDouble("TQ_Duration"),
                        Cost = mst.GetDouble("TQ_Cost")
                    };
                    pinfo.BIC = new WellDrillData
                    {
                        Days = mst.GetDouble("BIC_Duration"),
                        Cost = mst.GetDouble("BIC_Cost")
                    };
                    pinfo.LTA2 = new WellDrillData
                    {
                        Days = mst.GetDouble("LTA2_Duration"),
                        Cost = mst.GetDouble("LTA2_Cost")
                    };

                    pinfo.SinceLTA2 = mst.GetString("DURATION_New_(Since_LTA2)");

                    pinfo.BurnRate = mst.GetDouble("Burn_Rate");
                    pinfo.EscalationInflation = mst.GetDouble("EscalationInflation");
                    pinfo.CSO = mst.GetDouble("COST_CSO");
                    pinfo.TotalCostIncludePortf = mst.GetDouble("COST_Total_Cost_(Incl_Portfolio_Risk)");

                    pinfo.LLAmount = mst.GetDouble("LL_Amount");
                    pinfo.LLMonth = mst.GetString("LL_Month");

                    pinfo.CostEscalatedInflated = mst.GetDouble("COST_Escalation__Inflation");
                    pinfo.TotalCostWithEscInflCSO = mst.GetDouble("COST_Total_Cost_(Incl_Portfolio_Risk)");

                    TopQuartileMeasures ma = new TopQuartileMeasures();
                    ma.BICDuration = new WellDrillData
                    {
                        Days = mst.GetDouble("MEASURES_BIC_Duration"),
                        Cost = mst.GetDouble("MEASURES_BIC_Cost")
                    };
                    ma.TQDuration = new WellDrillData
                    {
                        Days = mst.GetDouble("MEASURES_TQ_Duration"),
                        Cost = mst.GetDouble("MEASURES_TQ_Cost")
                    };
                    ma.TQTargetperPIP = mst.GetString("MEASURES_TQ/Target_Per_PIP").Trim().Equals("") ? false :
                        mst.GetString("MEASURES_TQ/Target_Per_PIP").Trim().ToUpper().Equals("Y") ? true : false;
                    ma.BICperPIP = mst.GetString("MEASURES_BIC_Per_PIP").Trim().Equals("") ? false :
                        mst.GetString("MEASURES_BIC_Per_PIP").Trim().ToUpper().Equals("Y") ? true : false;

                    pinfo.TQMeasures = ma;
                    pinfo.Save();
                    #endregion


                    //if (mst.GetString("GM_Engr", "") != "") wa.AddPerson(ph, new WEISPersonInfo
                    //{
                    //    FullName = mst.GetString("GM_Engr"),
                    //    Email = "",
                    //    RoleId = "GM"
                    //});

                    //if (mst.GetString("GM_Ops", "") != "") wa.AddPerson(ph, new WEISPersonInfo
                    //{
                    //    FullName = mst.GetString("GM_Ops"),
                    //    Email = "",
                    //    RoleId = "GM-OPS"
                    //});
                }
            }

            var was = WellActivity.Populate<WellActivity>().Where(d => d.Phases.Select(e => e.ActivityType).Contains("n/a"));
            foreach (var wa in was)
            {
                wa.Phases.RemoveAll(d => d.ActivityType.Equals("n/a"));
                if (wa.Phases.Count == 0)
                {
                    if (isSaveWa)
                        wa.Delete();
                }
                else
                {
                    if (isSaveWa)
                        wa.Save();
                }

            }

            WellActivity.SyncSequenceId();
            WellActivity.UpdateLESchedule();
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
        public static void LoadLastSequence(List<WellActivity> was, string tableName = "OP201502V2", string userinput = "")
        {
            string previousOP = "";
            string nextOP = "";
            var DefaultOP = getBaseOP(out previousOP, out nextOP);

            if (!was.Any())
            {
                was = WellActivity.Populate<WellActivity>();
            }

            #region pak rauli : For now, Do Not check and Reset LS.
            //---- reset WA Phase LS Date
            //foreach (var wa in was)
            //{
            //    foreach (var ph in wa.Phases)
            //    {
            //        // if doesnt have WR 
            //        if (!WellActivity.isHaveWeeklyReport(wa.WellName, wa.UARigSequenceId, ph.ActivityType) && ph.PhSchedule.Start != Tools.DefaultDate)
            //            ph.PhSchedule = new DateRange();
            //    }
            //}
            #endregion

            List<BsonDocument> msts = DataHelper.Populate(tableName).OrderBy(d => d.GetString("_id")).ToList();
            foreach (var mst in msts)
            {
                //-- this is for debug purpose, please mark as comment on production 
                var isDebug = false;
                if (mst.GetString("Well_Name").ToLower().Equals("aransas"))
                    isDebug = true;
                //-- init vars
                var rigType = mst.GetString("Rig_Type");
                var rigName = mst.GetString("Rig_Name");
                var wellname = mst.GetString("Well_Name");

                var eventCodeId = mst.GetString("Activity_Type");
                var eventDescription = mst.GetString("Activity_Description");
                var dateStart = Tools.ToUTC(mst.GetDateTime("Start_Date", true));
                var dateEnd = Tools.ToUTC(mst.GetDateTime("End_Date", true));
                WellInfo well = null;

                //--- get wa from was
                var createWa = true;
                WellActivity wa = null;
                //List<WellActivity> checkwawpOnly = new List<WellActivity>();
                WellActivity checkwawpOnly = null;
                try
                {
                    checkwawpOnly = was.FirstOrDefault(x => x.WellName.Equals(wellname) && x.RigName.Equals(rigName)); // was.ToList().Where(d => d.WellName.Equals(wellname) && d.RigName.Equals(rigName));
                }
                catch (Exception ex)
                {
                    checkwawpOnly = null;
                }
                if (checkwawpOnly == null)
                { }
                else
                {

                    #region old
                    // var checkwa = was.FirstOrDefault(d => d.WellName.Equals(wellname)
                    // && d.RigName.Equals(rigName)
                    //&& d.Phases.FirstOrDefault(e => e.ActivityType.Equals(eventCodeId)) != null );
                    #endregion
                    wa = checkwawpOnly;
                    createWa = false;
                    #region old

                    //if (checkwawpOnly.Phases.FirstOrDefault(x=>x.ActivityType.Equals(eventCodeId)) == null)
                    //{
                    //    // ada wellNya
                    //    wa = checkwawpOnly;
                    //    // wa = was.FirstOrDefault(d => d.WellName.Equals(wellname)
                    //   //&& d.RigName.Equals(rigName));

                    //}
                    //else
                    //{
                    //    wa = was.FirstOrDefault(d => d.WellName.Equals(wellname)
                    //   && d.RigName.Equals(rigName)
                    //  && d.Phases.FirstOrDefault(e => e.ActivityType.Equals(eventCodeId)) != null);
                    //}
                    #endregion
                }

                if (wa != null)
                #region old

                //{
                //    wa = was.FirstOrDefault(d => d.WellName.Equals(wellname)
                //        && d.RigName.Equals(rigName));
                //    //&& d.Phases.Select(e => e.ActivityType).Contains(eventCodeId) == false);
                //    if (wa != null)
                //    {
                //        createWa = false;
                //    }
                //    //else
                //    //{
                //    //    var qwell = Query.And(
                //    //    Query.EQ("WellName", wellname),
                //    //    Query.EQ("Phases.ActivityType", eventCodeId));
                //    //    wa = WellActivity.Get<WellActivity>(qwell);
                //    //    if (wa != null)
                //    //    {
                //    //        createWa = false;
                //    //        was.Add(wa);
                //    //    }
                //    //}
                //}
                //else
                #endregion
                {
                    var prevMst = msts
                        .OrderBy(d => d.GetDateTime("Start_Date"))
                        .FirstOrDefault(d => d.GetString("Well_Name").Equals(wellname) && d.GetString("Activity_Type").Equals(eventCodeId));
                    if (prevMst.Get("_id").Equals(mst.Get("_id")))
                        createWa = false;
                }

                if (createWa)
                {
                    try
                    {
                        wa = new WellActivity();
                        try
                        {
                            well = WellInfo.Get<WellInfo>(wellname);
                            if (well == null)
                            {
                                well = new WellInfo();
                                well._id = wellname;
                                well.Title = wellname;
                                wa.NonOP = true;
                                well.Save();
                            }
                        }
                        catch (Exception ex)
                        {
                            if (well == null)
                            {
                                well = new WellInfo();
                                well._id = wellname;
                                well.Title = wellname;
                                wa.NonOP = true;
                                well.Save();
                            }
                        }

                        wa.WellName = wellname;
                        wa.Phases = new List<WellActivityPhase>();
                        was.Add(wa);
                    }
                    catch (Exception ex)
                    {

                    }
                }
                wa.RigName = rigName;
                wa.RigType = rigType;
                WellActivityPhase ph = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(eventCodeId));
                if (ph == null)
                {
                    ph = new WellActivityPhase();
                    ph.BaseOP = new List<string>() { DefaultOP };
                    ph.PhaseNo = wa.Phases.Count == 0 ? 1 : wa.Phases.Max(d => d.PhaseNo) + 1;
                    ph.ActivityType = eventCodeId;
                    ph.PlanSchedule = new DateRange();
                    //ph.PlanSchedule = new DateRange { Start = dateStart, Finish = dateEnd };

                    ph.PhSchedule = new DateRange();
                    ph.PhSchedule = new DateRange { Start = dateStart, Finish = dateEnd };
                    ph.EventCreatedFrom = "UploadLS";
                    ph.IsPostOP = true;
                    ph.PushToBizPlan = true;
                    ph.userupdate = userinput;

                    wa.OpsSchedule = new DateRange();
                    wa.LESchedule = new DateRange();
                    wa.Phases.Add(ph);

                    wa.Save(references: new string[] { "updatetobizplan" });
                    //IF NEW, CREATE DATA IN WEISWellActivityUpdatesMonthly
                    var dd = wa.CreateNewActivityUpdateMonthly(wa, ph.PhaseNo, "UploadLS");
                }

                ph.userupdate = userinput;


                if (!WellActivity.isHaveMonthlyLEReport(wa.WellName, wa.UARigSequenceId, ph.ActivityType))
                {
                    var dd = wa.CreateNewActivityUpdateMonthly(wa, ph.PhaseNo, "UploadLS");
                }

                ph.ActivityDesc = eventDescription;
                ph.PhSchedule = new DateRange { Start = dateStart, Finish = dateEnd };
                ph.OP.Days = ph.PhSchedule.Days + 1;
                if (ph.PhSchedule.Start.Year > 2000)
                {
                    // Kondisi ngereset LE karena Weekly Report
                    // saat ini ada Monthly LE jadi kondisi ini di ByPass

                    //if (ph.PreviousWeek.Year < 2000) ph.LWE = ph.OP;
                    //if (ph.LastWeek.Year < 2000) ph.LE = ph.OP;
                }
                else
                {
                    ph.LWE = new WellDrillData();
                    ph.LE = new WellDrillData();
                    ph.LME = new WellDrillData();
                }


                var checkbizplan = BizPlanActivity.Get<BizPlanActivity>(Query.And(Query.EQ("WellName", wa.WellName), Query.EQ("RigName", wa.RigName), Query.EQ("UARigSequenceId", wa.UARigSequenceId)));
                if (checkbizplan == null)
                {
                    // add bizplanactity
                    ph.PushToBizPlan = true;
                    wa.Save(references: new string[] { "updatetobizplan" });
                }
                else
                {
                    if (checkbizplan.Phases != null && checkbizplan.Phases.Count() > 0)
                    {
                        var havephases = checkbizplan.Phases.Where(x => x.ActivityType.Equals(ph.ActivityType));
                        if (havephases != null && havephases.Count() > 0)
                        { }
                        else
                        {
                            // add phases
                            ph.PushToBizPlan = true;
                            wa.Save(references: new string[] { "updatetobizplan" });
                        }
                    }
                }
                // calculated OP 
            }

            //---- reset WA Phase LE & LWE if not schedule is defined, because it means that respective event actually flagged as inactive from system
            foreach (var wa in was)
            {
                foreach (var ph in wa.Phases)
                {
                    if (ph.PhSchedule.Start.Year < 2000)
                    {
                        ph.LE = new WellDrillData();
                        ph.LWE = new WellDrillData();
                        ph.LESchedule = new DateRange();
                        ph.LWESchedule = new DateRange();
                    }
                }
            }

            foreach (var wa in was)
            {
                wa.Save();
            }

            WellActivity.SyncSequenceId2();
            WellActivity.UpdateLESchedule();


        }

        public WEISPerson AddPerson(WellActivityPhase ph, WEISPersonInfo personInfo)
        {
            var q = Query.And(Query.EQ("WellName", WellName), Query.EQ("ActivityType", ph.ActivityType));
            WEISPerson ret = WEISPerson.Get<WEISPerson>(q);
            if (ret == null)
            {
                ret = new WEISPerson
                {
                    WellName = WellName,
                    ActivityType = ph.ActivityType,
                    SequenceId = UARigSequenceId,
                    PhaseNo = ph.PhaseNo
                };
            }
            var existingInfo = ret.PersonInfos.FirstOrDefault(d => d.Email.Equals(personInfo.Email) && d.FullName.Equals(personInfo.FullName));
            if (existingInfo != null) ret.PersonInfos.Remove(existingInfo);
            ret.PersonInfos.Add(existingInfo);
            return ret;
        }

        public static void SyncSequenceId2(IMongoQuery query = null)
        {
            var waActuals = WellActivityActual.Populate<WellActivityActual>(query == null ? Query.Null : query, fields: new string[] { "WellName", "SequenceId" })
                .GroupBy(d => new { d.WellName, d.SequenceId })
                .Select(d => new { d.Key.WellName, d.Key.SequenceId });
            var wass = WellActivity.Populate<WellActivity>();//Query.And(Query.EQ("WellName", x.WellName))
            foreach (var x in waActuals)
            {
                var wa = wass.FirstOrDefault(c => c.WellName == x.WellName && c.UARigSequenceId == x.SequenceId);
                if (wa == null)
                {
                    // wass.Where(c => c.WellName.Equals(x.WellName));//
                    var was = WellActivity.Populate<WellActivity>(Query.And(Query.EQ("WellName", x.WellName)));
                    if (was.Count() > 0)
                    {
                        var sequenceId = was
                        .OrderBy(d => (d.OpsSchedule.Finish - DateTime.Now).TotalDays)
                        .First().UARigSequenceId;

                        var xus = WellActivityActual.Populate<WellActivityActual>(Query.And(
                            Query.EQ("WellName", x.WellName),
                            Query.EQ("SequenceId", x.SequenceId)));
                        foreach (var xu in xus)
                        {
                            xu.Delete();
                            xu.SequenceId = sequenceId;
                            xu.Save();
                        }
                    }
                }
            }


            var waSequences = WellActivityUpdate.Populate<WellActivityUpdate>(q: null, fields: new string[] { "WellName", "SequenceId", "Phase.ActivityType", "UpdateVersion" })
                    .GroupBy(d => new { d.WellName, d.SequenceId, d.Phase.ActivityType })
                    .Select(d => new
                    {
                        Key = d.Key,
                        UpdateVersion = d.Max(x => x.UpdateVersion)
                    })
                    .ToList();
            foreach (var x in waSequences)
            {
                var wa = wass.FirstOrDefault(c =>
                {
                    var rt = 0;
                    if (c.WellName == x.Key.WellName)
                        rt++;
                    if (c.UARigSequenceId == x.Key.SequenceId)
                        rt++;

                    foreach (var ps in c.Phases)
                    {
                        if (ps.ActivityType == x.Key.ActivityType)
                            rt++;
                    }

                    return rt >= 3;
                });

                if (wa == null)
                {
                    var was = wass.Where(c =>
                    {
                        var rt = 0;
                        if (c.WellName == x.Key.WellName)
                            rt++;

                        foreach (var ps in c.Phases)
                        {
                            if (ps.ActivityType == x.Key.ActivityType)
                                rt++;
                        }

                        return rt >= 2;
                    });
                    if (was.Count() > 0)
                    {
                        var sequenceId = was
                        .OrderBy(d => (d.OpsSchedule.Finish - x.UpdateVersion).TotalDays)
                        .First().UARigSequenceId;

                        var waUpdates = WellActivityUpdate.Populate<WellActivityUpdate>(Query.And(
                                Query.EQ("WellName", x.Key.WellName),
                                Query.EQ("SequenceId", x.Key.SequenceId),
                                Query.EQ("Phase.ActivityType", x.Key.ActivityType)
                            ));
                        foreach (var w in waUpdates)
                        {
                            w.Delete();
                            w.SequenceId = sequenceId;
                            w.Save();
                        }
                    }
                }
            }

            var waPips = WellPIP.Populate<WellPIP>();
            foreach (var x in waPips)
            {
                var wa = wass.FirstOrDefault(c =>
                {
                    var rt = 0;
                    if (c.WellName == x.WellName)
                        rt++;
                    if (c.UARigSequenceId == x.SequenceId)
                        rt++;

                    foreach (var ps in c.Phases)
                    {
                        if (ps.ActivityType == x.ActivityType)
                            rt++;
                    }

                    return rt >= 3;
                });
                if (wa == null)
                {
                    var was = wass.Where(c =>
                    {
                        var rt = 0;
                        if (c.WellName == x.WellName)
                            rt++;

                        foreach (var ps in c.Phases)
                        {
                            if (ps.ActivityType == x.ActivityType)
                                rt++;
                        }

                        return rt >= 2;
                    });
                    //var was = WellActivity.Populate<WellActivity>(Query.And(Query.EQ("WellName", x.WellName), Query.EQ("Phases.ActivityType", x.ActivityType)));
                    if (was.Count() > 0)
                    {
                        var sequenceId = was
                        .OrderBy(d => (d.OpsSchedule.Finish - DateTime.Now).TotalDays)
                        .First().UARigSequenceId;

                        x.Delete();
                        x.SequenceId = sequenceId;
                        x.Save();
                    }
                }
            }
        }
        public static void SyncSequenceId(IMongoQuery query = null)
        {
            var waActuals = WellActivityActual.Populate<WellActivityActual>(query == null ? Query.Null : query, fields: new string[] { "WellName", "SequenceId" })
                .GroupBy(d => new { d.WellName, d.SequenceId })
                .Select(d => new { d.Key.WellName, d.Key.SequenceId });
            foreach (var x in waActuals)
            {
                var wa = WellActivity.Get<WellActivity>(Query.And(
                        Query.EQ("WellName", x.WellName),
                        Query.EQ("UARigSequenceId", x.SequenceId)
                    ));
                if (wa == null)
                {
                    var was = WellActivity.Populate<WellActivity>(Query.And(Query.EQ("WellName", x.WellName)));
                    if (was.Count() > 0)
                    {
                        var sequenceId = was
                        .OrderBy(d => (d.OpsSchedule.Finish - DateTime.Now).TotalDays)
                        .First().UARigSequenceId;

                        var xus = WellActivityActual.Populate<WellActivityActual>(Query.And(
                            Query.EQ("WellName", x.WellName),
                            Query.EQ("SequenceId", x.SequenceId)));
                        foreach (var xu in xus)
                        {
                            xu.Delete();
                            xu.SequenceId = sequenceId;
                            xu.Save();
                        }
                    }
                }
            }

            var waSequences = WellActivityUpdate.Populate<WellActivityUpdate>(q: null, fields: new string[] { "WellName", "SequenceId", "Phase.ActivityType" })
                    .GroupBy(d => new { d.WellName, d.SequenceId, d.Phase.ActivityType })
                    .Select(d => new
                    {
                        Key = d.Key,
                        UpdateVersion = d.Max(x => x.UpdateVersion)
                    })
                    .ToList();
            foreach (var x in waSequences)
            {
                var wa = WellActivity.Get<WellActivity>(Query.And(
                        Query.EQ("WellName", x.Key.WellName),
                        Query.EQ("UARigSequenceId", x.Key.SequenceId),
                        Query.EQ("Phases.ActivityType", x.Key.ActivityType)
                    ));
                if (wa == null)
                {
                    var was = WellActivity.Populate<WellActivity>(Query.And(Query.EQ("WellName", x.Key.WellName), Query.EQ("Phases.ActivityType", x.Key.ActivityType)));
                    if (was.Count() > 0)
                    {
                        var sequenceId = was
                        .OrderBy(d => (d.OpsSchedule.Finish - x.UpdateVersion).TotalDays)
                        .First().UARigSequenceId;

                        var waUpdates = WellActivityUpdate.Populate<WellActivityUpdate>(Query.And(
                                Query.EQ("WellName", x.Key.WellName),
                                Query.EQ("SequenceId", x.Key.SequenceId),
                                Query.EQ("Phase.ActivityType", x.Key.ActivityType)
                            ));
                        foreach (var w in waUpdates)
                        {
                            w.Delete();
                            w.SequenceId = sequenceId;
                            w.Save();
                        }
                    }
                }
            }

            var waPips = WellPIP.Populate<WellPIP>();
            foreach (var x in waPips)
            {
                var wa = WellActivity.Get<WellActivity>(Query.And(
                        Query.EQ("WellName", x.WellName),
                        Query.EQ("UARigSequenceId", x.SequenceId),
                        Query.EQ("Phases.ActivityType", x.ActivityType)
                    ));
                if (wa == null)
                {
                    var was = WellActivity.Populate<WellActivity>(Query.And(Query.EQ("WellName", x.WellName), Query.EQ("Phases.ActivityType", x.ActivityType)));
                    if (was.Count() > 0)
                    {
                        var sequenceId = was
                        .OrderBy(d => (d.OpsSchedule.Finish - DateTime.Now).TotalDays)
                        .First().UARigSequenceId;

                        x.Delete();
                        x.SequenceId = sequenceId;
                        x.Save();
                    }
                }
            }
        }

        public void GetUpdate(DateTime dt, bool useDefaultIfNull = false, bool trimToFinishDate = false)
        {
            foreach (var ph in Phases)
            {
                ph.GetUpdate(this.UARigSequenceId, dt, useDefaultIfNull, trimToFinishDate);
            }
        }

        public static bool isHasWellPip(WellActivity wa, WellActivityPhase wp)
        {
            //var wx = DataHelper.Populate("WEISWellActivities", Query.And(
            //           Query.EQ("WellName", wa.WellName),
            //           Query.EQ("UARigSequenceId", wa.UARigSequenceId),
            //           Query.EQ("ActivityType", wa.ActivityType)
            //       ));
            var wx = DataHelper.Populate<WellPIP>("WEISWellPIPs", Query.And(
                       Query.EQ("WellName", wa.WellName),
                       Query.EQ("SequenceId", wa.UARigSequenceId),
                       Query.EQ("PhaseNo", wp.PhaseNo)
                   ));
            if (wx == null || wx.Count <= 0)
                return false;
            else
                return true;
        }

        public static void UpdateLESchedule(string[] rignames = null, bool initLE = true)
        {
            IMongoQuery q = Query.Null;
            bool isDebug = false;
            if (rignames != null) q = Query.In("RigName", new BsonArray(rignames));
            //--- populate was
            List<WellActivity> was = WellActivity.Populate<WellActivity>(q);

            //--- select all phases
            List<WellActivity> phases = was.SelectMany(d => d.Phases, (p, d) => new WellActivity
            {
                RigName = p.RigName,
                WellName = p.WellName,
                UARigSequenceId = p.UARigSequenceId,
                Phases = new WellActivityPhase[] { d }.ToList()
            })
            .Where(d => d.Phases[0].PhSchedule.Start != Tools.DefaultDate && d.Phases[0].ActivityType.ToLower().Contains("risk") == false)
            .OrderBy(d => d.RigName).ThenBy(d => d.Phases[0].PhSchedule.Start)
            .ToList();

            //--- arrange phase
            List<WellActivity> updates = new List<WellActivity>();
            string rigName = "";
            bool newRig = false;
            double leadDays = 0;
            double leadDaysLW = 0;
            DateTime lastFinishDate = Tools.DefaultDate;
            bool isPastEvent = false;
            bool isPastEventLW = false;
            foreach (var ph in phases)
            {
                //if (ph.WellName.ToLower().Equals("aransas"))
                //{
                //    isDebug = true;
                //}
                if (ph.RigName != rigName)
                {
                    rigName = ph.RigName;
                    lastFinishDate = Tools.DefaultDate;
                    newRig = true;
                    leadDays = 0;
                    leadDaysLW = 0;
                    isPastEvent = true;
                    isPastEventLW = true;
                }

                //--- is respective event on the past
                //--- and recalc only if not past event
                if (isPastEvent) isPastEvent = ph.Phases[0].PhSchedule.Finish < Tools.ToUTC(DateTime.Now, true).AddDays(-7);
                if (isPastEventLW) isPastEventLW = ph.Phases[0].PhSchedule.Finish < Tools.ToUTC(DateTime.Now, true).AddDays(-14);
                //if (ph.Phases[0].LE.Days > 0)
                //{
                //    ph.Phases[0].LESchedule.Finish = ph.Phases[0].LESchedule.Start.AddDays(ph.Phases[0].LE.Days);
                //}
                //if (ph.Phases[0].LE.Days > 0)
                //{
                //    ph.Phases[0].LESchedule.Finish = ph.Phases[0].LESchedule.Start.AddDays(ph.Phases[0].LE.Days);
                //}
                bool isActiveWell = ph.Phases[0].LESchedule.InRange(DateTime.Now);
                bool isActiveWellLW = ph.Phases[0].LWESchedule.InRange(DateTime.Now);
                if (!isPastEvent || isActiveWell)
                {
                    //--- initialize LE Days
                    bool fromDateHasBeenSet = false;
                    if (initLE)
                    {
                        WellActivityUpdate wau = WellActivityUpdate
                            .GetById(ph.WellName, ph.UARigSequenceId, ph.Phases[0].PhaseNo, null, true);
                        if (wau != null)
                        {
                            ph.Phases[0].initLEFromWeeklyReport(wau);
                            if (wau.EventStartDate.Year > 2000) fromDateHasBeenSet = true;
                        }
                    }

                    if (ph.Phases[0].PhSchedule.Start.Year > 2000 || isActiveWell)
                    {
                        #region Calc LE base on shift difference
                        if (ph.Phases[0].LE.Days == 0) ph.Phases[0].LE.Days = ph.Phases[0].OP.Days;
                        if (ph.Phases[0].LastWeek >= Tools.DefaultDate)
                        {
                            if (fromDateHasBeenSet) leadDays += (ph.Phases[0].LESchedule.Start - ph.Phases[0].PhSchedule.Start).TotalDays;
                            leadDays += ph.Phases[0].LE.Days - ph.Phases[0].PhSchedule.Days;
                        }
                        DateTime dateFrom = fromDateHasBeenSet ? ph.Phases[0].LESchedule.Start :
                            isActiveWell ? ph.Phases[0].PhSchedule.Start : ph.Phases[0].PhSchedule.Start.AddDays(leadDays);
                        DateTime dateTo = dateFrom.AddDays(ph.Phases[0].LE.Days == 0 ? 0 :
                            ph.Phases[0].LE.Days);
                        #endregion
                        ph.Phases[0].LESchedule = new DateRange(dateFrom, dateTo);
                    }
                    else
                    {
                        ph.Phases[0].LE = new WellDrillData();
                        ph.Phases[0].LESchedule = new DateRange();
                    }

                    var waUpdate = updates.FirstOrDefault(d => d.WellName.Equals(ph.WellName) && d.UARigSequenceId.Equals(ph.UARigSequenceId));
                    if (waUpdate == null)
                    {
                        try
                        {
                            waUpdate = was.FirstOrDefault(d => d.WellName.Equals(ph.WellName) && d.UARigSequenceId.Equals(ph.UARigSequenceId));
                            if (waUpdate != null)
                                updates.Add(waUpdate);
                        }
                        catch (Exception ex)
                        {


                        }
                    }
                    if (waUpdate != null)
                    {
                        var phase = waUpdate.Phases.FirstOrDefault(d => d.PhaseNo.Equals(ph.Phases[0].PhaseNo));
                        if (phase != null)
                        {
                            phase = ph.Phases[0];
                        }
                    }
                }

                if ((!isPastEventLW && ph.Phases[0].PhSchedule.Start.Year > 2000) || isActiveWellLW)
                {

                    if (ph.Phases[0].PhSchedule.Start.Year > 2000 || isActiveWellLW)
                    {
                        if (ph.Phases[0].LWE.Days == 0) ph.Phases[0].LWE.Days = ph.Phases[0].OP.Days;
                        #region Calc LE base on shift difference
                        DateTime dateFrom = isActiveWellLW ? ph.Phases[0].PhSchedule.Start : ph.Phases[0].PhSchedule.Start.AddDays(leadDaysLW);
                        if (ph.Phases[0].PreviousWeek >= Tools.DefaultDate) leadDaysLW += ph.Phases[0].LWE.Days - ph.Phases[0].PhSchedule.Days;
                        DateTime dateTo = dateFrom.AddDays(ph.Phases[0].LWE.Days == 0 ?
                            0 : ph.Phases[0].LWE.Days);
                        #endregion

                        ph.Phases[0].LWESchedule = new DateRange(dateFrom, dateTo);
                    }
                    else
                    {
                        ph.Phases[0].LWE = new WellDrillData();
                        ph.Phases[0].LWESchedule = new DateRange();
                    }

                    var waUpdate = updates.FirstOrDefault(d => d.WellName.Equals(ph.WellName) && d.UARigSequenceId.Equals(ph.UARigSequenceId));
                    if (waUpdate == null)
                    {
                        try
                        {

                            waUpdate = was.FirstOrDefault(d => d.WellName.Equals(ph.WellName) && d.UARigSequenceId.Equals(ph.UARigSequenceId));
                            if (waUpdate != null) updates.Add(waUpdate);
                        }
                        catch (Exception ex)
                        {

                        }

                    }
                    if (waUpdate != null)
                    {
                        var phase = waUpdate.Phases.FirstOrDefault(d => d.PhaseNo.Equals(ph.Phases[0].PhaseNo));
                        if (phase != null)
                        {
                            //phase.DataFromWhere = "LS Resource";
                            phase = ph.Phases[0];
                        }
                    }
                }
                newRig = false;
            }

            //--- save the range
            foreach (var waUpdate in updates)
                waUpdate.Save();
        }

        public static void UpdateSchedule(string[] rignames = null, string type = "OP")
        {
            IMongoQuery q = null;
            if (rignames != null) q = Query.In("RigName", new BsonArray(rignames));
            //--- populate was
            List<WellActivity> was = WellActivity.Populate<WellActivity>(q);

            //--- select all phases
            List<WellActivity> phases = was.SelectMany(d => d.Phases, (p, d) => new WellActivity
            {
                RigName = p.RigName,
                WellName = p.WellName,
                UARigSequenceId = p.UARigSequenceId,
                Phases = new WellActivityPhase[] { d }.ToList()
            })
            .Where(d => d.Phases[0].PhSchedule.Start != Tools.DefaultDate && d.Phases[0].ActivityType.ToLower().Contains("risk") == false)
            .OrderBy(d => d.RigName).ThenBy(d => d.Phases[0].PhSchedule.Start)
            .ToList();
        }

        [BsonIgnore]
        public int PhaseNo { get; set; }
        [BsonIgnore]
        public string ActivityType { get; set; }
        //[BsonIgnore]
        public string EXType { get; set; }

        public static void ProcessCalculatedOP()
        {
            var allwells = DataHelper.Populate("WEISWellActivities");
            var uw = BsonHelper.Unwind(allwells, "Phases", "", new List<string> { "RigName", "WellName", "UARigSequenceId" });

            var grouped = uw.GroupBy(x => new BsonDocument { x.GetElement("WellName"), x.GetElement("ActivityType"), x.GetElement("UARigSequenceId") });

            foreach (var t in grouped.ToList())
            {

                var datapergroup = t.ToList();
                var wellName = BsonHelper.GetString(t.Key, "WellName");

                if (wellName.Equals("BULLY 1 RIG IDLE 2015"))
                {

                }

                var seqId = BsonHelper.GetString(t.Key, "UARigSequenceId");
                var activity = BsonHelper.GetString(t.Key, "ActivityType");

                if (datapergroup.Count() > 1)
                {
                    bool isHaveValidLS = false;
                    foreach (var w in datapergroup)
                    {
                        var datetime = w.GetDateTime("PhSchedule.Start");
                        if (datetime > Tools.DefaultDate)
                        {
                            isHaveValidLS = true;
                        }
                    }

                    bool ishaveweekly = WellActivity.isHaveWeeklyReport(wellName, seqId, activity);
                    bool ishavemonthly = WellActivity.isHaveMonthlyLEReport(wellName, seqId, activity);

                    if (isHaveValidLS)
                    {
                        var rigName = BsonHelper.GetString(datapergroup.FirstOrDefault(), "RigName");

                        var well = WellActivity.Get<WellActivity>(
                            Query.And(
                            Query.EQ("WellName", wellName),
                            Query.EQ("RigName", rigName),
                            Query.EQ("UARigSequenceId", seqId)
                            )
                            );

                        var wellPhase = well.Phases.Where(x => x.ActivityType.Equals(activity));
                        foreach (var p in wellPhase)
                        {
                            if (p.PhSchedule.Start > Tools.DefaultDate && p.PhSchedule.Finish > Tools.DefaultDate)
                            {
                                p.CalcOPSchedule = p.PhSchedule;
                                p.CalcOP = p.OP;
                            }
                            else
                            {
                                p.CalcOPSchedule = new DateRange();
                                p.CalcOP = new WellDrillData();
                                p.PlanSchedule = new DateRange();
                                p.Plan = new WellDrillData();
                            }
                            well.Save();
                        }
                    }
                    else
                    {
                        var rigName = BsonHelper.GetString(datapergroup.FirstOrDefault(), "RigName");

                        var well = WellActivity.Get<WellActivity>(
                             Query.And(
                             Query.EQ("WellName", wellName),
                             Query.EQ("RigName", rigName),
                             Query.EQ("UARigSequenceId", seqId)
                             )
                             );

                        var wellPhase = well.Phases.Where(x => x.ActivityType.Equals(activity));
                        foreach (var p in wellPhase)
                        {
                            p.CalcOPSchedule = p.PlanSchedule;
                            p.CalcOP = p.Plan;
                            well.Save();
                        }
                    }
                }
                else
                {
                    var rigName = BsonHelper.GetString(datapergroup.FirstOrDefault(), "RigName");
                    var well = WellActivity.Get<WellActivity>(
                        Query.And(
                        Query.EQ("WellName", wellName),
                        Query.EQ("RigName", rigName),
                        Query.EQ("UARigSequenceId", seqId)
                        )
                        );
                    var datals = datapergroup.FirstOrDefault();
                    var gg = BsonHelper.GetString(datapergroup.FirstOrDefault(), "ActivityType");
                    if (gg == null)
                    {

                    }
                    well.Phases.Where(x => x.ActivityType.Equals(BsonHelper.GetString(datapergroup.FirstOrDefault(), "ActivityType"))).FirstOrDefault().CalcOPSchedule =
                        well.Phases.Where(x => x.ActivityType.Equals(BsonHelper.GetString(datapergroup.FirstOrDefault(), "ActivityType"))).FirstOrDefault().PhSchedule;
                    well.Phases.Where(x => x.ActivityType.Equals(BsonHelper.GetString(datapergroup.FirstOrDefault(), "ActivityType"))).FirstOrDefault().CalcOP =
                        well.Phases.Where(x => x.ActivityType.Equals(BsonHelper.GetString(datapergroup.FirstOrDefault(), "ActivityType"))).FirstOrDefault().OP;
                    well.Save();
                }

            }

        }

        public void ProratingPhases()
        {
            if (this.Phases.Any())
            {
                foreach (var d in this.Phases)
                {
                    d.ProrateInfos = new List<ProrateDetails>();
                    // plan = 
                    if (d.PlanSchedule != null && d.PlanSchedule.Start != Tools.DefaultDate)
                    {
                        var splitted = DateRangeToMonth.SplitedDateRangeYearly(d.PlanSchedule);
                        foreach (var s in splitted)
                        {
                            ProrateDetails planPro = new ProrateDetails();
                            planPro.Title = "OP";
                            planPro.Info = s;
                            planPro.Value.Days = s.NoOfDay;
                            planPro.Value.Cost = s.Proportion * d.Plan.Cost;
                            d.ProrateInfos.Add(planPro);
                        }
                    }


                    // ls = 
                    if (d.PhSchedule != null && d.PhSchedule.Start != Tools.DefaultDate)
                    {
                        var phslplit = DateRangeToMonth.SplitedDateRangeYearly(d.PhSchedule);
                        foreach (var s in phslplit)
                        {
                            ProrateDetails planPro = new ProrateDetails();
                            planPro.Title = "LS";
                            planPro.Info = s;
                            planPro.Value.Days = s.NoOfDay;
                            planPro.Value.Cost = s.Proportion * d.OP.Cost;
                            d.ProrateInfos.Add(planPro);
                        }
                    }

                    // ls = 
                    if (d.LESchedule != null && d.LESchedule.Start != Tools.DefaultDate)
                    {
                        var leplit = DateRangeToMonth.SplitedDateRangeYearly(d.LESchedule);
                        foreach (var s in leplit)
                        {
                            ProrateDetails planPro = new ProrateDetails();
                            planPro.Title = "LE";
                            planPro.Info = s;
                            planPro.Value.Days = s.NoOfDay;
                            planPro.Value.Cost = s.Proportion * d.LE.Cost;
                            d.ProrateInfos.Add(planPro);
                        }
                    }

                    // AFE = 
                    if (d.AFESchedule != null && d.AFESchedule.Start != Tools.DefaultDate)
                    {
                        var afepit = DateRangeToMonth.SplitedDateRangeYearly(d.AFESchedule);
                        foreach (var s in afepit)
                        {
                            ProrateDetails planPro = new ProrateDetails();
                            planPro.Title = "AFE";
                            planPro.Info = s;
                            planPro.Value.Days = s.NoOfDay;
                            planPro.Value.Cost = s.Proportion * d.AFE.Cost;
                            d.ProrateInfos.Add(planPro);
                        }
                    }
                }
            }
        }

    }

    public class WellActivityHelper
    {
        public string SequenceId { get; set; }
        public string ActivityType { get; set; }
    }

    public class LogLatestLSDate : ECISModel
    {
        public override string TableName
        {
            get { return "LogLatestLSDate"; }
        }
        public DateTime LSDate { get; set; }
        public DateTime Executed_At { get; set; }
        public string Executed_By { get; set; }
    }

    public class WellPlanWeather : ECISModel
    {
        public string OriginalId { get; set; }
        public string SourceFrom { get; set; }
        public string WellName { get; set; }
        public string RigName { get; set; }
        public string SequenceId { get; set; }
        public string Desc { get; set; }

        private BizPlanActivityPhase _Bizphase;
        public BizPlanActivityPhase BizPhase
        {
            get
            {
                if (_Bizphase == null) _Bizphase = new BizPlanActivityPhase();
                return _Bizphase;
            }
            set
            {
                _Bizphase = value;
            }
        }

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

        public override string TableName
        {
            get { return "_WEISWellActivityWeathers"; }
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            string actv = Phase == null ? (BizPhase == null ? "" : BizPhase.ActivityType) : Phase.ActivityType;

            _id = String.Format("W{0}S{1}A{2}P{3}F{4}D{5:yyyyMMdd}",
                WellName.Replace(" ", "").Replace("-", ""),
                SequenceId,
                actv,
                Phase.PhaseNo,
                SourceFrom.Trim().Replace(" ",""),
                Tools.ToUTC(DateTime.Now));
            return this.ToBsonDocument();
        }
    }
}




