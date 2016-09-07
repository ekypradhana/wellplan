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
    public class BizPlanActivity : ECISModel
    {

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
                }
                else
                {
                    p.PhaseInfo = new WellActivityPhaseInfo();
                }


                if (WellName != null && p.ActivityType != null && RigName != null)
                {
                    var t = BizPlanAllocation.Get<BizPlanAllocation>(
                    Query.And(
                        Query.EQ("WellName", WellName),
                        Query.EQ("ActivityType", p.ActivityType),
                        Query.EQ("RigName", RigName)
                            )
                    );

                    p.Allocation = t;
                }

            }

            base.PostGet();
        }

        public static List<BizPlanActivity> GetAll(IMongoQuery q, int take = 0, int skip = 0, SortByBuilder sort = null, string[] fields = null, bool isMergeWithWellPlan = false, IMongoQuery queryWoLe = null)
        {

            List<WellActivity> activities = new List<WellActivity>();
            if (isMergeWithWellPlan)
            {
                activities = WellActivity.Populate<WellActivity>(q, take, skip, sort, fields);
            }
            var populates = BizPlanActivity.Populate<BizPlanActivity>(q, take, skip, sort, fields);
            var allocations = BizPlanAllocation.Populate<BizPlanAllocation>();

            if (populates.Count < activities.Count)
            {
                //when filter project not work
                //populates = BizPlanActivity.Populate<BizPlanActivity>(queryWoLe, take, skip, sort, fields);
                populates = BizPlanActivity.Populate<BizPlanActivity>(q, take, skip, sort, fields);
            }


            foreach (var pop in populates)
            {
                if (isMergeWithWellPlan)
                {
                    //var plan = activities.Where(x => x.WellName.Equals(pop.WellName) && x.RigName.Equals(pop.RigName)).ToList();
                    var plan = activities.Where(x => x.WellName.Equals(pop.WellName) && x.RigName.Equals(pop.RigName) && x.UARigSequenceId.Equals(pop.UARigSequenceId)).ToList();
                    if (plan.Any())
                    {

                        var firstplan = plan.FirstOrDefault();
                        pop.LESchedule = firstplan.LESchedule;

                        if (firstplan.OPHistories != null && firstplan.OPHistories.Count() > 0)
                        {
                            pop.OPHistories = firstplan.OPHistories;
                        }

                    }


                    foreach (var ph in pop.Phases)
                    {
                        var aloc = allocations.Where(x => x.WellName.Equals(pop.WellName) && x.ActivityType.Equals(ph.ActivityType) && x.RigName.Equals(ph.Estimate.RigName) && x.UARigSequenceId != null && x.UARigSequenceId.Equals(pop.UARigSequenceId)
                            //&& (x.EstimatePeriod.Start.Date == ph.Estimate.EstimatePeriod.Start.Date && x.EstimatePeriod.Finish.Date == ph.Estimate.EstimatePeriod.Finish.Date)

                            );
                        if (aloc != null && aloc.Count() > 0)
                            ph.Allocation = aloc.OrderByDescending(x => x.LastUpdate).FirstOrDefault();
                    }

                }
                else
                {
                    foreach (var ph in pop.Phases)
                    {
                        var aloc = allocations.Where(x => x.WellName.Equals(pop.WellName) && x.ActivityType.Equals(ph.ActivityType) && x.RigName.Equals(ph.Estimate.RigName) && x.UARigSequenceId != null && x.UARigSequenceId.Equals(pop.UARigSequenceId)
                            //&& (x.EstimatePeriod.Start.Date == ph.Estimate.EstimatePeriod.Start.Date && x.EstimatePeriod.Finish.Date == ph.Estimate.EstimatePeriod.Finish.Date)

                          );
                        if (aloc != null && aloc.Count() > 0)

                            ph.Allocation = aloc.OrderByDescending(x => x.LastUpdate).FirstOrDefault();
                    }
                }

            }

            return populates;
        }


        public static List<BizPlanActivity> GetAll2(IMongoQuery q, int take = 0, int skip = 0, SortByBuilder sort = null, string[] fields = null, bool isMergeWithWellPlan = false, IMongoQuery queryWoLe = null)
        {

            List<WellActivity> activities = new List<WellActivity>();
            if (isMergeWithWellPlan)
            {
                activities = WellActivity.Populate<WellActivity>(q, take, skip, sort, fields);
            }
            var populates = BizPlanActivity.Populate<BizPlanActivity>(q, take, skip, sort, fields);
            var allocations = BizPlanAllocation.Populate<BizPlanAllocation>(Query.NE("UARigSequenceId", ""));

            if (populates.Count < activities.Count)
            {
                //when filter project not work
                //populates = BizPlanActivity.Populate<BizPlanActivity>(queryWoLe, take, skip, sort, fields);
                populates = BizPlanActivity.Populate<BizPlanActivity>(q, take, skip, sort, fields);
            }


            foreach (var pop in populates)
            {
                if (isMergeWithWellPlan)
                {
                    var plan = activities.Where(x => x.WellName.Equals(pop.WellName) && x.RigName.Equals(pop.RigName)).ToList();
                    if (plan.Any())
                    {
                        pop.LESchedule = plan.FirstOrDefault().LESchedule;
                    }

                    foreach (var ph in pop.Phases)
                    {
                        var aloc = allocations.Where(x => x.WellName.Equals(pop.WellName) && x.ActivityType.Equals(ph.ActivityType) && x.RigName.Equals(pop.RigName) && x.UARigSequenceId != null && x.UARigSequenceId.Equals(pop.UARigSequenceId)
                             && (x.EstimatePeriod.Start.Date == ph.Estimate.EstimatePeriod.Start.Date && x.EstimatePeriod.Finish.Date == ph.Estimate.EstimatePeriod.Finish.Date)

                            );
                        if (aloc != null && aloc.Count() > 0)
                            ph.Allocation = aloc.FirstOrDefault();
                    }

                }
                else
                {
                    foreach (var ph in pop.Phases)
                    {
                        var aloc = allocations.Where(x => x.WellName.Equals(pop.WellName) && x.ActivityType.Equals(ph.ActivityType) && x.RigName.Equals(pop.RigName) && x.UARigSequenceId != null && x.UARigSequenceId.Equals(pop.UARigSequenceId)
                          );
                        if (aloc != null && aloc.Count() > 0)

                            ph.Allocation = aloc.FirstOrDefault();
                    }
                }

            }

            return populates;
        }

        private string GetPersonNameByRole(string RoleId)
        {
            List<IMongoQuery> queries = new List<IMongoQuery>();
            if (WellName != null)
                queries.Add(Query.EQ("WellName", WellName));
            if (UARigSequenceId != null)
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

        public string Country { get; set; }

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



        public BizPlanActivity()
        {
            Phases = new List<BizPlanActivityPhase>();
        }
        public override string TableName
        {
            get { return "WEISBizPlanActivities"; }
        }
        [BsonIgnore]
        public DateTime? WeeklyReport { get; set; }
        [BsonIgnore]
        public DateTime? MonthlyReport { get; set; }
        public string BizPlanId { get; set; }

        public bool NonOP { get; set; }
        public string Region { get; set; }
        public string RigType { get; set; }
        public string RigName { get; set; }
        public string OperatingUnit { get; set; }
        public string ProjectName { get; set; }
        public string AssetName { get; set; }
        public string LineOfBusiness { get; set; }
        public string WellName { get; set; }
        public double WorkingInterest { get; set; }
        public string FirmOrOption { get; set; }
        public string UARigSequenceId { get; set; }
        public string UARigDescription { get; set; }

        public bool VirtualPhase { get; set; }
        public bool ShiftFutureEventDate { get; set; }
        public string WellEngineer { get; set; }
        public string CWIEngineer { get; set; }
        public bool isInPlan { get; set; }

        public bool isFirstInitiate { get; set; }


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

        public List<BizPlanActivityPhase> Phases { get; set; }
        public BizPlanActivityPhase GetPhase(string activityType)
        {
            if (Phases == null) Phases = new List<BizPlanActivityPhase>();
            return Phases.FirstOrDefault(d => d.ActivityType.Equals(activityType));
        }
        public BizPlanActivityPhase GetPhase(int phaseNo)
        {
            if (Phases == null) Phases = new List<BizPlanActivityPhase>();
            return Phases.FirstOrDefault(d => d.PhaseNo.Equals(phaseNo));
        }


        private string GetPersonEmailByRole(string RoleId)
        {
            List<IMongoQuery> queries = new List<IMongoQuery>();
            if (WellName != null)
                queries.Add(Query.EQ("WellName", WellName));
            if (UARigSequenceId != null)
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

                // set only first
                if (Phases.Count > 0)
                {
                    this.DataInputBy = this.Phases.FirstOrDefault().Estimate.LastUpdateBy;
                }
            }

            //foreach(var t in Phases)
            //{
            //    t.PlanSchedule.Start = t.Estimate.EstimatePeriod.Start;
            //    t.PlanSchedule.Finish = t.Estimate.EstimatePeriod.Finish;
            //    t.Plan.Cost = t.Estimate.MeanCostMOD;
            //    t.Plan.Days = t.Estimate.NewMean.Days;
            //}

            if (Phases.Count > 0)
            {
                PsSchedule = new DateRange
                {
                    Start = Phases.Where(d => d.PlanSchedule.Finish > Tools.DefaultDate).Count() == 0 ? Tools.DefaultDate :
                        Phases.Where(d => d.PlanSchedule.Finish > Tools.DefaultDate).Min(d => d.PlanSchedule.Start),
                    Finish = Phases.Where(d => d.PlanSchedule.Finish > Tools.DefaultDate).Count() == 0 ? Tools.DefaultDate :
                        Phases.Where(d => d.PlanSchedule.Finish > Tools.DefaultDate).Max(d => d.PlanSchedule.Finish)
                };
                OpsSchedule = new DateRange
                {
                    Start = Phases.Where(d => d.PhSchedule.Finish > Tools.DefaultDate).Count() == 0 ? Tools.DefaultDate :
                        Phases.Where(d => d.PhSchedule.Finish > Tools.DefaultDate).Min(d => d.PhSchedule.Start),
                    Finish = Phases.Where(d => d.PhSchedule.Finish > Tools.DefaultDate).Count() == 0 ? Tools.DefaultDate :
                        Phases.Where(d => d.PhSchedule.Finish > Tools.DefaultDate).Max(d => d.PhSchedule.Finish)
                };
            }

            return this.ToBsonDocument();
        }

        public bool CheckPhaseIsExistInWellPlan(string wellName, string rigName, string activityType,
            string sequenceId, string baseOP)
        {
            var t = WellActivity.Get<WellActivity>(Query.And(
                Query.EQ("WellName", WellName),
                Query.EQ("RigName", rigName),
                Query.EQ("UARigSequenceId", sequenceId),
                Query.EQ("Phases.ActivityType", activityType),
                Query.In("Phases.BaseOP", new BsonArray(new string[] { baseOP }))
                ));
            if (t == null)
                return false;
            else
                return true;
        }

        #region oldPostSave
        //public override BsonDocument PostSave(BsonDocument doc, string[] references = null)
        //{
        //    //foreach (var p in this.Phases)
        //    //{
        //    //    BizPlanAllocation.SaveBizPlanAllocation(this);
        //    //}

        //    var ignoreCalc = false;

        //    if (references != null && references.Count() > 0)
        //    {
        //        if (references.ToList().Contains("updatetowellplan"))
        //        {

        //            var varianceRigNames = this.Phases.Select(x => x.Estimate.RigName).Distinct().ToList();

        //            foreach (var rigs in varianceRigNames)
        //            {
        //                // update to wellPlan
        //                var biz = this.ToBsonDocument();
        //                biz.Set("_id", 0);
        //                biz.Remove("Country");
        //                biz.Remove("BizPlanId");
        //                biz.Remove("LineOfBusiness");
        //                biz.Remove("WellEngineer");
        //                biz.Remove("CWIEngineer");
        //                biz.Remove("isInPlan");
        //                biz.Remove("isFirstInitiate");
        //                biz.Remove("Currency");
        //                biz.Remove("ShellShare");
        //                biz.Remove("Status");
        //                biz.Remove("DataInputBy");
        //                biz.Remove("ReferenceFactorModel");


        //                var wa = BsonHelper.Deserialize<WellActivity>(biz);
        //                var NewBaseOP = "";
        //                wa.RigName = rigs;
        //                wa.Phases = new List<WellActivityPhase>();
        //                var respectivePhase = this.Phases.Where(x => x.Estimate.RigName.Equals(rigs)).ToList();

        //                // default active op
        //                string DefaultOP = "OP15";
        //                if (Config.GetConfigValue("BaseOPConfig") != null)
        //                {
        //                    DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
        //                }

        //                foreach (var rp in respectivePhase)
        //                {
        //                    var ph = BsonHelper.Deserialize<WellActivityPhase>(rp.ToBsonDocument());
        //                    if (rp.Estimate.SaveToOP.Equals(DefaultOP))
        //                    {
        //                        ph.IsPostOP = true;
        //                    }
        //                    if (!ph.BaseOP.Any()) ph.BaseOP = new List<string>();
        //                    if(!ph.BaseOP.Contains(rp.Estimate.SaveToOP)) ph.BaseOP.Add(rp.Estimate.SaveToOP);
        //                    wa.Phases.Add(ph);
        //                }

        //                var wellPlan = WellActivity.Get<WellActivity>(Query.And(
        //                    Query.EQ("WellName", wa.WellName),
        //                    Query.EQ("RigName", wa.RigName),
        //                    Query.EQ("UARigSequenceId", wa.UARigSequenceId)
        //                    ));

        //                if (wellPlan == null)
        //                {
        //                    wa.Save();
        //                }
        //                else
        //                {
        //                    foreach (var p in wa.Phases)
        //                    {
        //                        var activeOP = p.BaseOP.OrderByDescending(d => d).FirstOrDefault();
        //                        var adapase = wellPlan.Phases.Where(x => x.ActivityType.Equals(p.ActivityType));
        //                        if (adapase != null && adapase.Count() > 0)
        //                        {
        //                            // ada phase

        //                            // save phase to OPHistory
        //                            var OPHist = new OPHistory();
        //                            OPHist.Type = adapase.FirstOrDefault().BaseOP.OrderByDescending(d => d).FirstOrDefault();
        //                            OPHist.WellName = WellName;
        //                            OPHist.ActivityType = p.ActivityType;
        //                            OPHist.UARigSequenceId = UARigSequenceId;
        //                            OPHist.RigName = RigName;
        //                            OPHist.PhaseNo = p.PhaseNo;
        //                            OPHist.CalcOP = p.CalcOP;
        //                            OPHist.CalcOPSchedule = p.CalcOPSchedule;
        //                            OPHist.LE = p.LE;
        //                            OPHist.LESchedule = p.LESchedule;
        //                            OPHist.LWE = p.LWE;
        //                            OPHist.LWESchedule = p.LWESchedule;
        //                            OPHist.LME = p.LME;
        //                            OPHist.LMESchedule = p.LMESchedule;
        //                            OPHist.OP = p.OP;
        //                            OPHist.PhSchedule = p.PhSchedule;
        //                            OPHist.Plan = p.Plan;
        //                            OPHist.PlanSchedule = p.PlanSchedule;
        //                            OPHist.AFE = p.AFE;
        //                            OPHist.TQ = p.TQ;
        //                            OPHist.Actual = p.Actual;

        //                            //if this is active OP, no need to save it to OP History
        //                            if (OPHist.Type != activeOP)
        //                            {
        //                                if (!wellPlan.OPHistories.Any())
        //                                {
        //                                    wellPlan.OPHistories = new List<OPHistory>();
        //                                }
        //                                if (wellPlan.OPHistories.Any() && wellPlan.OPHistories.Where(d => d.Type.Equals(OPHist.Type) && d.ActivityType.Equals(OPHist.ActivityType)).Any())
        //                                {
        //                                    var rm_OPhist = wellPlan.OPHistories.Where(d => d.Type.Equals(OPHist.Type) && d.ActivityType.Equals(OPHist.ActivityType)).ToList();
        //                                    foreach (var rm in rm_OPhist)
        //                                    {
        //                                        wellPlan.OPHistories.Remove(rm);
        //                                    }
        //                                }
        //                                wellPlan.OPHistories.Add(OPHist);
        //                            }

        //                            var remov = wellPlan.Phases.Where(x => x.ActivityType.Equals(p.ActivityType)).FirstOrDefault();
        //                            var oldBaseOP = remov.BaseOP;
        //                            wellPlan.Phases.Remove(remov);

        //                            var add = wa.Phases.Where(x => x.ActivityType.Equals(p.ActivityType)).FirstOrDefault();
        //                            foreach (var op in oldBaseOP)
        //                            {
        //                                if (!add.BaseOP.Contains(op)) add.BaseOP.Add(op);
        //                            }
        //                            //if (!add.BaseOP.Any())
        //                            //{
        //                            //    add.BaseOP = oldBaseOP;
        //                            //}
        //                            //else
        //                            //{
        //                            //    if (!add.BaseOP.Contains(oldBaseOP))
        //                            //    {

        //                            //    }
        //                            //}
        //                            wellPlan.Phases.Add(add);
        //                        }
        //                        else
        //                        {
        //                            wellPlan.Phases.Add(p);
        //                        }
        //                    }
        //                    wellPlan.Save();
        //                }

        //                //save to phase info
        //                var getActId = WellActivity.Get<WellActivity>(Query.And(
        //                    Query.EQ("WellName", wa.WellName),
        //                    Query.EQ("RigName", wa.RigName),
        //                    Query.EQ("UARigSequenceId", wa.UARigSequenceId)
        //                    ));

        //                foreach (var ph in respectivePhase)
        //                {
        //                    var phInfo = new WellActivityPhaseInfo();
        //                    phInfo.WellActivityId = getActId._id.ToString();
        //                    phInfo.WellName = wa.WellName;
        //                    phInfo.RigName = wa.RigName;
        //                    phInfo.ActivityType = ph.ActivityType;
        //                    phInfo.SequenceId = wa.UARigSequenceId;
        //                    phInfo.PhaseNo = ph.PhaseNo;
        //                    var maturityLevel = ph.Estimate.MaturityLevel.Substring(ph.Estimate.MaturityLevel.Length - 1, 1);
        //                    phInfo.LoE = maturityLevel;
        //                    phInfo.TroubleFree = ph.Estimate.NewTroubleFree;

        //                    if (ph.Estimate.WellValueDriver != null)
        //                    {
        //                        if (ph.Estimate.WellValueDriver.ToUpper() == "COST")
        //                        {
        //                            phInfo.TQ = new WellDrillData() { Cost = ph.Estimate.TQValueDriver };
        //                            phInfo.BIC = new WellDrillData() { Cost = ph.Estimate.BICValueDriver };

        //                        }
        //                        else
        //                        {
        //                            phInfo.TQ = new WellDrillData() { Days = ph.Estimate.TQValueDriver };
        //                            phInfo.BIC = new WellDrillData() { Days = ph.Estimate.BICValueDriver };
        //                        }
        //                    }

        //                    phInfo.BurnRate = ph.Estimate.BurnRate;

        //                    phInfo.LLMonth = ph.Estimate.LongLeadMonthRequired.ToString();
        //                    phInfo.LLAmount = ph.Estimate.LongLeadCalc;

        //                    //phInfo.TotalDuration
        //                    //phInfo.Trouble
        //                    //phInfo.Contigency
        //                    //phInfo.SinceLTA2
        //                    //phInfo.LTA2
        //                    //phInfo.EscalationInflation
        //                    //phInfo.CSO
        //                    //phInfo.TotalCostIncludePortf
        //                    //phInfo.CostEscalatedInflated
        //                    //phInfo.TotalCostWithEscInflCSO
        //                    //phInfo.TQMeasures

        //                    phInfo.Save();

        //                }


        //            }

        //        }

        //        if (references.ToList().Contains("ignoreCalc"))
        //        {
        //            ignoreCalc = true;
        //        }
        //    }

        //    if (!ignoreCalc)
        //    {
        //        BizPlanAllocation.SaveBizPlanAllocation(this);
        //    }

        //    return this.ToBsonDocument();
        //}
        #endregion

        public override BsonDocument PostSave(BsonDocument doc, string[] references = null)
        {

            if (references != null && references.Count() > 0)
            {


                if (!references.ToList().Contains("ignoreCalc"))
                {
                    BizPlanAllocation.SaveBizPlanAllocation(this);
                }

                if (this.WellName.Equals("CALYPSO"))
                {

                }

                if (references.ToList().Contains("updatetowellplan"))
                {
                    //if(references.ToList().Contains("addplanfrombizplan"))
                    //{
                    //    this.Phases.LastOrDefault().BaseOP.Add()
                    //}
                    // default active op
                    string DefaultOP = "OP15";
                    if (Config.GetConfigValue("BaseOPConfig") != null)
                    {
                        DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
                    }

                    //data.Phases.FirstOrDefault().PlanSchedule = data.Phases.FirstOrDefault().Estimate.EstimatePeriod;
                    //data.Phases.FirstOrDefault().Plan.Cost = data.Phases.FirstOrDefault().Estimate.MeanCostMOD;
                    //data.Phases.FirstOrDefault().Plan.Days = data.Phases.FirstOrDefault().Estimate.EstimatePeriod.Days;

                    var bizplanToPush = this;
                    var phaseToPush = this.Phases.Where(x => x.PushToWellPlan.Equals(true)).FirstOrDefault();
                    phaseToPush.PlanSchedule = phaseToPush.Estimate.EstimatePeriod;
                    phaseToPush.Plan.Cost = phaseToPush.Estimate.MeanCostMOD;
                    //phaseToPush.Plan.Days = phaseToPush.Estimate.EstimatePeriod.Days;
                    phaseToPush.Plan.Days = phaseToPush.Estimate.NewMean.Days;
                    bizplanToPush.Phases = new List<BizPlanActivityPhase>() { phaseToPush };

                    var waPhaseConvert = new WellActivityPhase();
                    waPhaseConvert.ActivityType = phaseToPush.ActivityType;
                    waPhaseConvert.PlanSchedule = phaseToPush.PlanSchedule;
                    waPhaseConvert.PhSchedule = phaseToPush.PhSchedule;
                    waPhaseConvert.Plan = phaseToPush.Plan;


                    var qs = new List<IMongoQuery>();
                    qs.Add(Query.EQ("WellName", bizplanToPush.WellName));
                    qs.Add(Query.EQ("UARigSequenceId", bizplanToPush.UARigSequenceId));
                    qs.Add(Query.EQ("RigName", bizplanToPush.RigName));

                    // Rig Sequence ID no uniq add this logic
                    qs.Add(Query.EQ("Phases.ActivityType", phaseToPush.ActivityType));


                    #region HARUS DI REMARK KARENA MENGAKIBATKAN SEQ ID SAMA DI 2 DOCUMENT WELLPLAN
                    //qs.Add(Query.EQ("Region", bizplanToPush.Region));
                    //qs.Add(Query.EQ("OperatingUnit", bizplanToPush.OperatingUnit));
                    //qs.Add(Query.EQ("ProjectName", bizplanToPush.ProjectName));
                    //qs.Add(Query.EQ("AssetName", bizplanToPush.AssetName));
                    #endregion


                    var getWA = WellActivity.Get<WellActivity>(Query.And(qs));
                    if (getWA == null)
                    {
                        // bind to existing
                        qs = new List<IMongoQuery>();
                        qs.Add(Query.EQ("WellName", bizplanToPush.WellName));
                        qs.Add(Query.EQ("UARigSequenceId", bizplanToPush.UARigSequenceId));
                        qs.Add(Query.EQ("RigName", bizplanToPush.RigName));

                        getWA = WellActivity.Get<WellActivity>(Query.And(qs));

                        if (getWA != null)
                        {
                            #region rebind to Existing WellPlan
                            //check the wellplan phase
                            getWA.Region = bizplanToPush.Region;
                            getWA.OperatingUnit = bizplanToPush.OperatingUnit;
                            getWA.ProjectName = bizplanToPush.ProjectName;
                            getWA.AssetName = bizplanToPush.AssetName;
                            getWA.RigType = bizplanToPush.RigType;
                            getWA.PerformanceUnit = bizplanToPush.PerformanceUnit;
                            getWA.FirmOrOption = bizplanToPush.FirmOrOption;
                            getWA.WorkingInterest = bizplanToPush.ShellShare;
                            getWA.UARigDescription = bizplanToPush.UARigDescription;
                            //getWA.EXType = bizplanToPush.


                            if (getWA.Phases.Any())
                            {
                                var waPhase = new List<WellActivityPhase>();
                                var foundMatchActivityType = false;
                                foreach (var p in getWA.Phases)
                                {
                                    if (p.ActivityType == phaseToPush.ActivityType)
                                    {
                                        //match activityType, play with OPHistory if current OP != saveToOP from bizplan estimate
                                        //only update the PlanSchedule and Plan value

                                        foundMatchActivityType = true;
                                        var saveToOP = phaseToPush.Estimate.SaveToOP;
                                        var currentOP = p.BaseOP.Any() ? p.BaseOP.OrderByDescending(d => d).FirstOrDefault() : DefaultOP;

                                        #region OP Histories

                                        if (currentOP != saveToOP)
                                        {
                                            //play with OP Histories
                                            var OPHist = new OPHistory();
                                            OPHist.Type = currentOP;
                                            OPHist.WellName = WellName;
                                            OPHist.ActivityType = p.ActivityType;
                                            OPHist.UARigSequenceId = UARigSequenceId;
                                            OPHist.RigName = RigName;
                                            OPHist.PhaseNo = p.PhaseNo;
                                            OPHist.CalcOP = p.CalcOP;
                                            OPHist.CalcOPSchedule = p.CalcOPSchedule;
                                            OPHist.LE = p.LE;
                                            OPHist.LESchedule = p.LESchedule;
                                            OPHist.LWE = p.LWE;
                                            OPHist.LWESchedule = p.LWESchedule;
                                            OPHist.LME = p.LME;
                                            OPHist.LMESchedule = p.LMESchedule;
                                            OPHist.OP = p.OP;
                                            OPHist.PhSchedule = p.PhSchedule;
                                            OPHist.Plan = p.Plan;
                                            OPHist.PlanSchedule = p.PlanSchedule;
                                            OPHist.AFE = p.AFE;
                                            OPHist.TQ = p.TQ;
                                            OPHist.Actual = p.Actual;

                                            if (!getWA.OPHistories.Any())
                                            {
                                                getWA.OPHistories = new List<OPHistory>();
                                            }
                                            if (getWA.OPHistories.Any() && getWA.OPHistories.Where(d => d.Type != null && d.Type.Equals(OPHist.Type) && d.ActivityType.Equals(OPHist.ActivityType)).Any())
                                            {
                                                var rm_OPhist = getWA.OPHistories.Where(d => d.Type != null && d.Type.Equals(OPHist.Type) && d.ActivityType.Equals(OPHist.ActivityType)).ToList();
                                                foreach (var rm in rm_OPhist)
                                                {
                                                    getWA.OPHistories.Remove(rm);
                                                }
                                            }
                                            getWA.OPHistories.Add(OPHist);

                                            //update phase
                                            if (!p.BaseOP.Any()) p.BaseOP = new List<string>();
                                            p.BaseOP.Add(saveToOP);
                                            p.PlanSchedule = phaseToPush.PlanSchedule;
                                            p.Plan = phaseToPush.Plan;
                                            p.IsPostOP = false;
                                        }
                                        else
                                        {
                                            p.PlanSchedule = phaseToPush.PlanSchedule;
                                            p.Plan = phaseToPush.Plan;

                                            if (currentOP == DefaultOP)
                                            {
                                                //this is the POST OP
                                                p.IsPostOP = true;
                                            }
                                            else
                                            {
                                                p.IsPostOP = false;
                                            }
                                        }
                                        p.IsInPlan = this.isInPlan;
                                        p.FundingType = phaseToPush.FundingType;

                                        #endregion
                                    }
                                    else
                                    {
                                        //p.IsInPlan = this.isInPlan;
                                        //getWA.Phases.Add(p);
                                    }
                                }
                                if (!foundMatchActivityType)
                                {
                                    //push new phase
                                    var saveToOP = phaseToPush.Estimate.SaveToOP;
                                    waPhaseConvert.PhaseNo = phaseToPush.PhaseNo;

                                    waPhaseConvert.BaseOP = new List<string>();
                                    waPhaseConvert.BaseOP.Add(saveToOP);
                                    waPhaseConvert.FundingType = phaseToPush.FundingType;
                                    waPhaseConvert.PlanSchedule = new DateRange(phaseToPush.Estimate.EventStartDate, phaseToPush.Estimate.EventEndDate);
                                    waPhaseConvert.Plan = new WellDrillData();
                                    waPhaseConvert.Plan.Cost = phaseToPush.Estimate.MeanCostMOD;//new DateRange(phaseToPush.Estimate.EventStartDate, phaseToPush.Estimate.EventEndDate);
                                    waPhaseConvert.Plan.Days = phaseToPush.Estimate.NewMean.Days;//new DateRange(phaseToPush.Estimate.EventStartDate, phaseToPush.Estimate.EventEndDate);

                                    getWA.Phases.Add(waPhaseConvert);
                                }
                            }
                            else
                            {
                                //push as new Phase

                                var saveToOP = phaseToPush.Estimate.SaveToOP;

                                waPhaseConvert.PhaseNo = phaseToPush.PhaseNo;
                                waPhaseConvert.FundingType = phaseToPush.FundingType;
                                waPhaseConvert.BaseOP = new List<string>();
                                waPhaseConvert.BaseOP.Add(saveToOP);
                                waPhaseConvert.PlanSchedule = new DateRange(phaseToPush.Estimate.EventStartDate, phaseToPush.Estimate.EventEndDate);
                                waPhaseConvert.Plan = new WellDrillData();
                                waPhaseConvert.Plan.Cost = phaseToPush.Estimate.MeanCostMOD;//new DateRange(phaseToPush.Estimate.EventStartDate, phaseToPush.Estimate.EventEndDate);
                                waPhaseConvert.Plan.Days = phaseToPush.Estimate.NewMean.Days;//new DateRange(phaseToPush.Estimate.EventStartDate, phaseToPush.Estimate.EventEndDate);

                                getWA.Phases = new List<WellActivityPhase>() { waPhaseConvert };

                            }
                            getWA.Save();

                            #endregion
                        }
                        else
                        {

                            // recheck one more time 
                            qs = new List<IMongoQuery>();
                            qs.Add(Query.EQ("WellName", bizplanToPush.WellName));
                            qs.Add(Query.EQ("RigName", bizplanToPush.RigName));
                            // Rig Sequence ID no uniq add this logic
                            qs.Add(Query.EQ("Phases.ActivityType", phaseToPush.ActivityType));

                            getWA = WellActivity.Get<WellActivity>(Query.And(qs));
                            if(getWA == null)
                            {
                                #region just save it as new wellplan
                                var newWA = bizplanToPush.ToBsonDocument();
                                newWA.Set("_id", 0);
                                newWA.Remove("Country");
                                newWA.Remove("BizPlanId");
                                newWA.Remove("LineOfBusiness");
                                newWA.Remove("WellEngineer");
                                newWA.Remove("CWIEngineer");
                                newWA.Remove("isInPlan");
                                newWA.Remove("isFirstInitiate");
                                newWA.Remove("Currency");
                                newWA.Remove("ShellShare");
                                newWA.Remove("Status");
                                newWA.Remove("DataInputBy");
                                newWA.Remove("ReferenceFactorModel");


                                var newWAToPush = BsonHelper.Deserialize<WellActivity>(newWA);

                                if (references.ToList().Contains("addplanfrombizplan"))
                                {
                                    var saveToOP = phaseToPush.Estimate.SaveToOP;
                                    var avails = this.Phases.Where(x => x.ActivityType.Equals(phaseToPush.ActivityType));
                                    if (avails != null && avails.Count() > 0)
                                    {
                                        var y = avails.LastOrDefault();
                                        y.BaseOP = new List<string>();
                                        y.BaseOP.Add(phaseToPush.Estimate.SaveToOP);


                                        var wpavails = newWAToPush.Phases.Where(x => x.ActivityType.Equals(phaseToPush.ActivityType));
                                        if (wpavails != null && wpavails.Count() > 0)
                                        {
                                            var wpx = wpavails.LastOrDefault();
                                            wpx.BaseOP = new List<string>();
                                            wpx.BaseOP.Add(phaseToPush.Estimate.SaveToOP);
                                        }
                                        //newWAToPush.Phases.Where(x=>)
                                    }
                                }

                                newWAToPush.Save();
                                #endregion

                            }
                            else
                            {
                                #region rebind to Existing WellPlan
                                //check the wellplan phase
                                getWA.Region = bizplanToPush.Region;
                                getWA.OperatingUnit = bizplanToPush.OperatingUnit;
                                getWA.ProjectName = bizplanToPush.ProjectName;
                                getWA.AssetName = bizplanToPush.AssetName;
                                getWA.RigType = bizplanToPush.RigType;
                                getWA.PerformanceUnit = bizplanToPush.PerformanceUnit;
                                getWA.FirmOrOption = bizplanToPush.FirmOrOption;
                                getWA.WorkingInterest = bizplanToPush.ShellShare;
                                getWA.UARigDescription = bizplanToPush.UARigDescription;
                                //getWA.EXType = bizplanToPush.


                                if (getWA.Phases.Any())
                                {
                                    var waPhase = new List<WellActivityPhase>();
                                    var foundMatchActivityType = false;
                                    foreach (var p in getWA.Phases)
                                    {
                                        if (p.ActivityType == phaseToPush.ActivityType)
                                        {
                                            //match activityType, play with OPHistory if current OP != saveToOP from bizplan estimate
                                            //only update the PlanSchedule and Plan value

                                            foundMatchActivityType = true;
                                            var saveToOP = phaseToPush.Estimate.SaveToOP;
                                            var currentOP = p.BaseOP.Any() ? p.BaseOP.OrderByDescending(d => d).FirstOrDefault() : DefaultOP;

                                            #region OP Histories

                                            if (currentOP != saveToOP)
                                            {
                                                //play with OP Histories
                                                var OPHist = new OPHistory();
                                                OPHist.Type = currentOP;
                                                OPHist.WellName = WellName;
                                                OPHist.ActivityType = p.ActivityType;
                                                OPHist.UARigSequenceId = UARigSequenceId;
                                                OPHist.RigName = RigName;
                                                OPHist.PhaseNo = p.PhaseNo;
                                                OPHist.CalcOP = p.CalcOP;
                                                OPHist.CalcOPSchedule = p.CalcOPSchedule;
                                                OPHist.LE = p.LE;
                                                OPHist.LESchedule = p.LESchedule;
                                                OPHist.LWE = p.LWE;
                                                OPHist.LWESchedule = p.LWESchedule;
                                                OPHist.LME = p.LME;
                                                OPHist.LMESchedule = p.LMESchedule;
                                                OPHist.OP = p.OP;
                                                OPHist.PhSchedule = p.PhSchedule;
                                                OPHist.Plan = p.Plan;
                                                OPHist.PlanSchedule = p.PlanSchedule;
                                                OPHist.AFE = p.AFE;
                                                OPHist.TQ = p.TQ;
                                                OPHist.Actual = p.Actual;

                                                if (!getWA.OPHistories.Any())
                                                {
                                                    getWA.OPHistories = new List<OPHistory>();
                                                }
                                                if (getWA.OPHistories.Any() && getWA.OPHistories.Where(d => d.Type != null && d.Type.Equals(OPHist.Type) && d.ActivityType.Equals(OPHist.ActivityType)).Any())
                                                {
                                                    var rm_OPhist = getWA.OPHistories.Where(d => d.Type != null && d.Type.Equals(OPHist.Type) && d.ActivityType.Equals(OPHist.ActivityType)).ToList();
                                                    foreach (var rm in rm_OPhist)
                                                    {
                                                        getWA.OPHistories.Remove(rm);
                                                    }
                                                }
                                                getWA.OPHistories.Add(OPHist);

                                                //update phase
                                                if (!p.BaseOP.Any()) p.BaseOP = new List<string>();
                                                p.BaseOP.Add(saveToOP);
                                                p.PlanSchedule = phaseToPush.PlanSchedule;
                                                p.Plan = phaseToPush.Plan;
                                                p.IsPostOP = false;
                                            }
                                            else
                                            {
                                                p.PlanSchedule = phaseToPush.PlanSchedule;
                                                p.Plan = phaseToPush.Plan;

                                                if (currentOP == DefaultOP)
                                                {
                                                    //this is the POST OP
                                                    p.IsPostOP = true;
                                                }
                                                else
                                                {
                                                    p.IsPostOP = false;
                                                }
                                            }
                                            p.IsInPlan = this.isInPlan;
                                            p.FundingType = phaseToPush.FundingType;

                                            #endregion
                                        }
                                        else
                                        {
                                            //p.IsInPlan = this.isInPlan;
                                            //getWA.Phases.Add(p);
                                        }
                                    }
                                    if (!foundMatchActivityType)
                                    {
                                        //push new phase
                                        var saveToOP = phaseToPush.Estimate.SaveToOP;
                                        waPhaseConvert.PhaseNo = phaseToPush.PhaseNo;

                                        waPhaseConvert.BaseOP = new List<string>();
                                        waPhaseConvert.BaseOP.Add(saveToOP);
                                        waPhaseConvert.FundingType = phaseToPush.FundingType;
                                        waPhaseConvert.PlanSchedule = new DateRange(phaseToPush.Estimate.EventStartDate, phaseToPush.Estimate.EventEndDate);
                                        waPhaseConvert.Plan = new WellDrillData();
                                        waPhaseConvert.Plan.Cost = phaseToPush.Estimate.MeanCostMOD;//new DateRange(phaseToPush.Estimate.EventStartDate, phaseToPush.Estimate.EventEndDate);
                                        waPhaseConvert.Plan.Days = phaseToPush.Estimate.NewMean.Days;//new DateRange(phaseToPush.Estimate.EventStartDate, phaseToPush.Estimate.EventEndDate);

                                        getWA.Phases.Add(waPhaseConvert);
                                    }
                                }
                                else
                                {
                                    //push as new Phase

                                    var saveToOP = phaseToPush.Estimate.SaveToOP;

                                    waPhaseConvert.PhaseNo = phaseToPush.PhaseNo;
                                    waPhaseConvert.FundingType = phaseToPush.FundingType;
                                    waPhaseConvert.BaseOP = new List<string>();
                                    waPhaseConvert.BaseOP.Add(saveToOP);
                                    waPhaseConvert.PlanSchedule = new DateRange(phaseToPush.Estimate.EventStartDate, phaseToPush.Estimate.EventEndDate);
                                    waPhaseConvert.Plan = new WellDrillData();
                                    waPhaseConvert.Plan.Cost = phaseToPush.Estimate.MeanCostMOD;//new DateRange(phaseToPush.Estimate.EventStartDate, phaseToPush.Estimate.EventEndDate);
                                    waPhaseConvert.Plan.Days = phaseToPush.Estimate.NewMean.Days;//new DateRange(phaseToPush.Estimate.EventStartDate, phaseToPush.Estimate.EventEndDate);

                                    getWA.Phases = new List<WellActivityPhase>() { waPhaseConvert };

                                }
                                getWA.Save();

                                #endregion
                            }
                        }

                    }
                    else
                    {
                        //check the wellplan phase
                        getWA.Region = bizplanToPush.Region;
                        getWA.OperatingUnit = bizplanToPush.OperatingUnit;
                        getWA.ProjectName = bizplanToPush.ProjectName;
                        getWA.AssetName = bizplanToPush.AssetName;
                        getWA.RigType = bizplanToPush.RigType;
                        getWA.PerformanceUnit = bizplanToPush.PerformanceUnit;
                        getWA.FirmOrOption = bizplanToPush.FirmOrOption;
                        getWA.WorkingInterest = bizplanToPush.ShellShare;
                        getWA.UARigDescription = bizplanToPush.UARigDescription;
                        //getWA.EXType = bizplanToPush.

                        if (getWA.Phases.Any())
                        {
                            var waPhase = new List<WellActivityPhase>();
                            var foundMatchActivityType = false;
                            foreach (var p in getWA.Phases)
                            {
                                if (p.ActivityType == phaseToPush.ActivityType)
                                {
                                    //match activityType, play with OPHistory if current OP != saveToOP from bizplan estimate
                                    //only update the PlanSchedule and Plan value
                                    foundMatchActivityType = true;
                                    var saveToOP = phaseToPush.Estimate.SaveToOP;
                                    var currentOP = p.BaseOP.Any() ? p.BaseOP.OrderByDescending(d => d).FirstOrDefault() : DefaultOP;

                                    #region OP Histories

                                    if (currentOP != saveToOP)
                                    {
                                        //play with OP Histories
                                        var OPHist = new OPHistory();
                                        OPHist.Type = currentOP;
                                        OPHist.WellName = WellName;
                                        OPHist.ActivityType = p.ActivityType;
                                        OPHist.UARigSequenceId = UARigSequenceId;
                                        OPHist.RigName = RigName;
                                        OPHist.PhaseNo = p.PhaseNo;
                                        OPHist.CalcOP = p.CalcOP;
                                        OPHist.CalcOPSchedule = p.CalcOPSchedule;
                                        OPHist.LE = p.LE;
                                        OPHist.LESchedule = p.LESchedule;
                                        OPHist.LWE = p.LWE;
                                        OPHist.LWESchedule = p.LWESchedule;
                                        OPHist.LME = p.LME;
                                        OPHist.LMESchedule = p.LMESchedule;
                                        OPHist.OP = p.OP;
                                        OPHist.PhSchedule = p.PhSchedule;
                                        OPHist.Plan = p.Plan;
                                        OPHist.PlanSchedule = p.PlanSchedule;
                                        OPHist.AFE = p.AFE;
                                        OPHist.TQ = p.TQ;
                                        OPHist.Actual = p.Actual;

                                        if (!getWA.OPHistories.Any())
                                        {
                                            getWA.OPHistories = new List<OPHistory>();
                                        }
                                        if (getWA.OPHistories.Any() && getWA.OPHistories.Where(d => d.Type != null && d.Type.Equals(OPHist.Type) && d.ActivityType.Equals(OPHist.ActivityType)).Any())
                                        {
                                            var rm_OPhist = getWA.OPHistories.Where(d => d.Type != null && d.Type.Equals(OPHist.Type) && d.ActivityType.Equals(OPHist.ActivityType)).ToList();
                                            foreach (var rm in rm_OPhist)
                                            {
                                                getWA.OPHistories.Remove(rm);
                                            }
                                        }
                                        getWA.OPHistories.Add(OPHist);

                                        //update phase
                                        if (!p.BaseOP.Any()) p.BaseOP = new List<string>();
                                        p.BaseOP.Add(saveToOP);
                                        p.PlanSchedule = phaseToPush.PlanSchedule;
                                        p.Plan = phaseToPush.Plan;
                                        p.IsPostOP = false;
                                    }
                                    else
                                    {
                                        p.PlanSchedule = phaseToPush.PlanSchedule;
                                        p.Plan = phaseToPush.Plan;

                                        if (currentOP == DefaultOP)
                                        {
                                            //this is the POST OP
                                            p.IsPostOP = true;
                                        }
                                        else
                                        {
                                            p.IsPostOP = false;
                                        }
                                    }
                                    p.IsInPlan = this.isInPlan;
                                    p.FundingType = phaseToPush.FundingType;

                                    #endregion
                                }
                                else
                                {
                                    //p.IsInPlan = this.isInPlan;
                                    waPhase.Add(p);
                                }
                            }
                            if (!foundMatchActivityType)
                            {
                                //push new phase
                                waPhase.Add(waPhaseConvert);
                            }
                        }
                        else
                        {
                            //push as new Phase
                            getWA.Phases = new List<WellActivityPhase>() { waPhaseConvert };
                        }
                        getWA.Save();
                    }
                    #region save to phase info
                    var getActId = WellActivity.Get<WellActivity>(Query.And(
                        Query.EQ("WellName", bizplanToPush.WellName),
                        Query.EQ("RigName", bizplanToPush.RigName),
                        Query.EQ("UARigSequenceId", bizplanToPush.UARigSequenceId)
                        ));

                    var ph = phaseToPush;
                    var phInfo = new WellActivityPhaseInfo();
                    phInfo.WellActivityId = getActId._id.ToString();
                    phInfo.WellName = bizplanToPush.WellName;
                    phInfo.RigName = bizplanToPush.RigName;
                    phInfo.ActivityType = ph.ActivityType;
                    phInfo.SequenceId = bizplanToPush.UARigSequenceId;
                    phInfo.PhaseNo = ph.PhaseNo;
                    var maturityLevel = ph.Estimate.MaturityLevel.Substring(ph.Estimate.MaturityLevel.Length - 1, 1);
                    phInfo.LoE = maturityLevel;
                    phInfo.TroubleFree = ph.Estimate.NewTroubleFree;

                    if (ph.Estimate.WellValueDriver != null)
                    {
                        if (ph.Estimate.WellValueDriver.ToUpper() == "COST")
                        {
                            phInfo.TQ = new WellDrillData() { Cost = ph.Estimate.TQValueDriver };
                            phInfo.BIC = new WellDrillData() { Cost = ph.Estimate.BICValueDriver };

                        }
                        else
                        {
                            phInfo.TQ = new WellDrillData() { Days = ph.Estimate.TQValueDriver };
                            phInfo.BIC = new WellDrillData() { Days = ph.Estimate.BICValueDriver };
                        }
                    }

                    phInfo.BurnRate = ph.Estimate.BurnRateUSD;
                    phInfo.SpreadRate = ph.Estimate.SpreadRateTotalUSD;
                    phInfo.MRI = ph.Estimate.MechanicalRiskIndex;
                    phInfo.CompletionType = ph.Estimate.CompletionType;
                    try
                    {
                        phInfo.CompletionZone = Convert.ToInt32(ph.Estimate.CompletionInfo);

                    }
                    catch (Exception ex)
                    {
                        phInfo.CompletionZone = 0;
                    }
                    phInfo.BrineDensity = ph.Estimate.BrineDensity;

                    phInfo.LLMonth = ph.Estimate.LongLeadMonthRequired.ToString();
                    phInfo.LLAmount = ph.Estimate.LongLeadCalc;


                    phInfo.LineOfBusiness = bizplanToPush.LineOfBusiness;
                    phInfo.WorkingInterest = bizplanToPush.WorkingInterest;
                    //phInfo.PlanningClassification = bizplanToPush.FirmOrOption;
                    phInfo.ActivityCategory = ph.ActivityCategory;
                    phInfo.DrillingOfCasing = ph.Estimate.NumberOfCasings;

                    phInfo.ReferenceFactorModel = ReferenceFactorModel;
                    phInfo.OPType = ph.Estimate.SaveToOP;

                    phInfo.WaterDepthMD = ph.Estimate.WaterDepth;
                    phInfo.TotalWellDepthMD = ph.Estimate.WellDepth;
                    phInfo.LearningCurveFactor = ph.Estimate.NewLearningCurveFactor;
                    try
                    {
                        phInfo.MaturityLevel = string.IsNullOrEmpty(ph.Estimate.MaturityLevel) == false ? ph.Estimate.MaturityLevel.Split(' ')[1] : "";

                    }
                    catch (Exception ex)
                    {

                    }

                    phInfo.PerformanceScore = ph.Estimate.PerformanceScore;
                    phInfo.TQGap = ph.Estimate.TQ.Gap;
                    phInfo.BICGap = ph.Estimate.BIC.Gap;

                    phInfo.TQTreshold = ph.Estimate.TQ.Threshold;
                    phInfo.BICTreshold = ph.Estimate.BIC.Threshold;
                    phInfo.ProjectValueDriver = ph.Estimate.ProjectValueDriver;

                    phInfo.NPT.PercentTime = ph.Estimate.NewNPTTime.PercentDays;
                    phInfo.NPT.PercentCost = ph.Estimate.NewNPTTime.PercentCost;
                    phInfo.NPT.Days = ph.Estimate.NewNPTTime.Days;
                    phInfo.NPT.Cost = ph.Estimate.NewNPTTime.Cost;

                    phInfo.TECOP.PercentTime = ph.Estimate.NewTECOPTime.PercentDays;
                    phInfo.TECOP.PercentCost = ph.Estimate.NewTECOPTime.PercentCost;
                    phInfo.TECOP.Days = ph.Estimate.NewTECOPTime.Days;
                    phInfo.TECOP.Cost = ph.Estimate.NewTECOPTime.Cost;


                    phInfo.Mean.Days = ph.Estimate.NewMean.Days;
                    phInfo.Mean.Cost = ph.Estimate.NewMean.Cost;
                    phInfo.Currency = ph.Estimate.Currency;
                    phInfo.Mean.Cost = ph.Estimate.NewMean.Cost;
                    phInfo.MeanCostEDM.Cost = ph.Estimate.NewMean.Cost;
                    phInfo.MeanCostEDM.Days = ph.Estimate.NewMean.Days;

                    phInfo.TroubleFree.Cost = ph.Estimate.NewTroubleFree.Cost;
                    phInfo.MeanCostEDM.Cost = ph.Estimate.NewMean.Cost;

                    phInfo.USDCost.CSO = ph.Allocation.AnnualyBuckets.Sum(x => x.CSOCost);
                    phInfo.USDCost.Escalation = ph.Allocation.AnnualyBuckets.Sum(x => x.EscCostTotal);
                    phInfo.USDCost.Inflation = ph.Allocation.AnnualyBuckets.Sum(x => x.InflationCost);
                    phInfo.USDCost.MeanCostMOD = ph.Allocation.AnnualyBuckets.Sum(x => x.MeanCostMOD);


                    phInfo.USDCost.NPT = ph.Estimate.NPTCostUSD;
                    phInfo.USDCost.TECOP = ph.Estimate.TECOPCostUSD;
                    phInfo.USDCost.TroubleFree = ph.Estimate.NewTroubleFreeUSD;
                    phInfo.USDCost.MeanCostEDM = ph.Estimate.MeanUSD;


                    //                sp.EstRangeType = pi.EstimatingRangeType;
                    //                sp.DeterminLowRange = pi.DeterministicLowRange;
                    //                sp.DeterminHigh = pi.DeterministicHigh;

                    //                sp.ProbP10 = pi.ProbabilisticP10;
                    //                sp.ProbP90 = pi.ProbabilisticP90;

                    //                sp.WaterDepth = pi.WaterDepthMD;
                    //                sp.TotalWaterDepth = pi.TotalWellDepthMD;

                    //                sp.LCFactor = pi.LearningCurveFactor;
                    //                sp.MaturityRisk = "Level " + pi.MaturityLevel;
                    //                sp.RFM = pi.ReferenceFactorModel;

                    //                sp.SequenceOnRig = pi.RigSequenceId;

                    //                sp.TroubleFree.Days = pi.TroubleFree.Days  * y.Proportion ;
                    //                sp.NPT.PercentDays = pi.NPT.PercentTime;
                    //                sp.TECOP.PercentDays = pi.TECOP.PercentTime;
                    //                sp.OverrideFactor.Time = pi.OverrideFactor.Time;
                    //                sp.TimeOverrideFactors = sp.OverrideFactor.Time == true ? "No" : "Yes";

                    //                sp.NPT.Days = pi.NPT.Days * y.Proportion;
                    //                sp.TECOP.Days = pi.TECOP.Days * y.Proportion;


                    //                sp.MeanTime = pi.Mean.Days * y.Proportion;

                    //                sp.TroubleFree.Cost = pi.TroubleFree.Cost * y.Proportion;
                    //                sp.Currency = pi.Currency;

                    //                sp.NPT.PercentCost = pi.NPT.PercentCost;
                    //                sp.TECOP.PercentCost = pi.TECOP.PercentCost;
                    //                sp.CostOverrideFactors = pi.OverrideFactor.Cost == true ? "No" : "Yes";

                    //                sp.NPT.Cost = pi.NPT.Cost * y.Proportion;
                    //                sp.TECOP.Cost = pi.TECOP.Cost * y.Proportion;
                    //                sp.MeanCostEDM = pi.MeanCostEDM.Cost * y.Proportion;


                    //                sp.TroubleFreeUSD = pi.USDCost.TroubleFree * y.Proportion;
                    //                sp.NPTUSD = pi.USDCost.NPT * y.Proportion;
                    //                sp.TECOPUSD = pi.USDCost.TECOP * y.Proportion;
                    //                sp.MeanCostEDMUSD = pi.USDCost.MeanCostEDM * y.Proportion;
                    //                sp.EscCostUSD = pi.USDCost.Escalation * y.Proportion;
                    //                sp.CSOCostUSD = pi.USDCost.CSO * y.Proportion;
                    //                sp.InflationCostUSD = pi.USDCost.Inflation * y.Proportion;
                    //                sp.MeanCostMODUSD = pi.USDCost.MeanCostMOD * y.Proportion;


                    //                sp.ProjectValueDriver = pi.ProjectValueDriver;

                    //                sp.TQ.Threshold = pi.TQTreshold;
                    //                sp.TQ.Gap = pi.TQGap;
                    //                sp.BIC.Threshold = pi.BICTreshold;
                    //                sp.BIC.Gap = pi.BICGap;
                    //                sp.PerfScore = pi.PerformanceScore;

                    phInfo.Save();

                    #endregion
                } //end update to wellplan


            }
            else
            {
                BizPlanAllocation.SaveBizPlanAllocation(this);
            }



            return this.ToBsonDocument();

        }


        public static BizPlanActivity GetByUniqueKey(
            string wellname,
            string rigsequenceid,
            bool create = false)
        {
            var q = Query.And(Query.EQ("WellName", wellname), Query.EQ("UARigSequenceId", rigsequenceid));
            BizPlanActivity ret = BizPlanActivity.Get<BizPlanActivity>(q);
            if (ret == null && create)
            {
                ret = new BizPlanActivity();
                ret._id = 0;
                ret.UARigSequenceId = rigsequenceid;
                ret.WellName = wellname;
            }
            return ret;
        }

        [BsonIgnore]
        public int PhaseNo { get; set; }
        [BsonIgnore]
        public string ActivityType { get; set; }
        //[BsonIgnore]
        public string EXType { get; set; }

        //added by eky, based on CR 11092015
        public string Currency { get; set; }
        public double ShellShare { get; set; }
        public string Status { get; set; }

        public string DataInputBy { get; set; }

        public string ReferenceFactorModel { get; set; }

        public SavePlanResult SavePlanMethod(BizPlanActivity data, string updatedby, string status = "Draft", bool overrideLastUpdate = true)
        {
            try
            {

                //set selectedCurrency di estimate sesuai dengan currency yg di select di UI
                data.Phases.FirstOrDefault().Estimate.SelectedCurrency = data.Currency;
                data.Phases.FirstOrDefault().Estimate.Country = data.Country;
                data.Phases.FirstOrDefault().Estimate.Currency = data.Currency;
                //convert dulu semua cost ke USD
                var Currencyx = data.Currency;
                if (!Currencyx.Trim().ToUpper().Equals("USD"))
                {
                    var datas = MacroEconomic.Populate<MacroEconomic>(Query.EQ("Currency", Currencyx));
                    if (datas.Any())
                    {
                        var cx = datas.Where(x => x.Currency.Equals(Currencyx)).FirstOrDefault();

                        var rate = cx.ExchangeRate.AnnualValues.Where(x => x.Year == data.Phases.FirstOrDefault().Estimate.EstimatePeriod.Start.Year).Any() ?
                                cx.ExchangeRate.AnnualValues.Where(x => x.Year == data.Phases.FirstOrDefault().Estimate.EstimatePeriod.Start.Year).FirstOrDefault().Value
                                : 0;

                        // dibagi rate : untuk jadi USD
                        data.Currency = "USD";
                        data.Phases.FirstOrDefault().Estimate.Currency = "USD";
                        data.Phases.FirstOrDefault().Estimate.Services = Tools.Div(data.Phases.FirstOrDefault().Estimate.Services, rate);
                        data.Phases.FirstOrDefault().Estimate.Materials = Tools.Div(data.Phases.FirstOrDefault().Estimate.Materials, rate);

                        data.Phases.FirstOrDefault().Estimate.SpreadRate = Tools.Div(data.Phases.FirstOrDefault().Estimate.SpreadRate, rate);

                        data.Phases.FirstOrDefault().Estimate.SpreadRateTotal = Tools.Div(data.Phases.FirstOrDefault().Estimate.SpreadRateTotal, rate);

                        data.Phases.FirstOrDefault().Estimate.BurnRate = Tools.Div(data.Phases.FirstOrDefault().Estimate.BurnRate, rate);

                        data.Phases.FirstOrDefault().Estimate.RigRate = Tools.Div(data.Phases.FirstOrDefault().Estimate.RigRate, rate);

                        data.Phases.FirstOrDefault().Estimate.ShellShareCalc = Tools.Div(data.Phases.FirstOrDefault().Estimate.ShellShareCalc, rate);
                        data.Phases.FirstOrDefault().Estimate.MeanCostMOD = Tools.Div(data.Phases.FirstOrDefault().Estimate.MeanCostMOD, rate);

                        data.Phases.FirstOrDefault().Estimate.TroubleFreeBeforeLC.Cost = Tools.Div(data.Phases.FirstOrDefault().Estimate.TroubleFreeBeforeLC.Cost, rate);
                        data.Phases.FirstOrDefault().Estimate.NewTroubleFree.Cost = Tools.Div(data.Phases.FirstOrDefault().Estimate.NewTroubleFree.Cost, rate);
                        data.Phases.FirstOrDefault().Estimate.NewNPTTime.Cost = Tools.Div(data.Phases.FirstOrDefault().Estimate.NewNPTTime.Cost, rate);
                        data.Phases.FirstOrDefault().Estimate.NewMean.Cost = Tools.Div(data.Phases.FirstOrDefault().Estimate.NewMean.Cost, rate);
                        data.Phases.FirstOrDefault().Estimate.NewTECOPTime.Cost = Tools.Div(data.Phases.FirstOrDefault().Estimate.NewTECOPTime.Cost, rate);
                        data.Phases.FirstOrDefault().Estimate.isFirstInitiate = false;

                    }
                }

                data.Phases.FirstOrDefault().Estimate.ServicesUSD = data.Phases.FirstOrDefault().Estimate.Services;
                data.Phases.FirstOrDefault().Estimate.MaterialsUSD = data.Phases.FirstOrDefault().Estimate.Materials;
                data.Phases.FirstOrDefault().Estimate.SpreadRateUSD = data.Phases.FirstOrDefault().Estimate.SpreadRate;
                data.Phases.FirstOrDefault().Estimate.SpreadRateTotalUSD = data.Phases.FirstOrDefault().Estimate.SpreadRateTotal;
                data.Phases.FirstOrDefault().Estimate.RigRatesUSD = data.Phases.FirstOrDefault().Estimate.RigRate;
                data.Phases.FirstOrDefault().Estimate.BurnRateUSD = data.Phases.FirstOrDefault().Estimate.BurnRate;
                data.Phases.FirstOrDefault().Estimate.MeanUSD = data.Phases.FirstOrDefault().Estimate.NewMean.Cost;
                data.Phases.FirstOrDefault().Estimate.NewTroubleFreeUSD = data.Phases.FirstOrDefault().Estimate.NewTroubleFree.Cost;
                data.Phases.FirstOrDefault().Estimate.NPTCostUSD = data.Phases.FirstOrDefault().Estimate.NewNPTTime.Cost;
                data.Phases.FirstOrDefault().Estimate.TECOPCostUSD = data.Phases.FirstOrDefault().Estimate.NewTECOPTime.Cost;

                //flag this phase as PushToWellPlan
                data.Phases.FirstOrDefault().PushToWellPlan = true;

                if (overrideLastUpdate)
                {
                    data.Phases.FirstOrDefault().Estimate.LastUpdate = Tools.ToUTC(DateTime.Now);
                    data.Phases.FirstOrDefault().Estimate.LastUpdateBy = updatedby;
                }
                //change the plan value for this Phase
                //data.Phases.FirstOrDefault().PlanSchedule = data.Phases.FirstOrDefault().Estimate.EstimatePeriod;
                //data.Phases.FirstOrDefault().Plan.Cost = data.Phases.FirstOrDefault().Estimate.MeanCostMOD;
                //data.Phases.FirstOrDefault().Plan.Days = data.Phases.FirstOrDefault().Estimate.EstimatePeriod.Days;

                var busplan = BizPlanActivity.Get<BizPlanActivity>(data._id);
                var OtherPhases = busplan.Phases;
                var tempActivity = data.Phases.FirstOrDefault().ActivityType;
                data.isFirstInitiate = false;
                var cfg = DataHelper.Get("SharedConfigTable", Query.EQ("_id", "BizPlanConfig")).GetBool("ConfigValue");
                //var isUpdateToWellPlan = cfg.ToBsonDocument().GetBool("ConfigValue");

                data.WorkingInterest = data.ShellShare;
                var newPhases = new List<BizPlanActivityPhase>();
                if (OtherPhases != null && OtherPhases.Count > 0)
                {
                    var x = data.Phases.FirstOrDefault();
                    foreach (var d in OtherPhases)
                    {
                        if (d.PhaseNo == x.PhaseNo)
                        {
                            x.Estimate.Currency = d.Estimate.Currency;
                            x.Estimate.Country = d.Estimate.Country;
                            x.LevelOfEstimate = x.Estimate.MaturityLevel;
                            x.Estimate.EventStartDate = Tools.ToUTC(x.Estimate.EstimatePeriod.Start, true);
                            x.Estimate.EventEndDate = Tools.ToUTC(x.Estimate.EstimatePeriod.Finish);
                            x.Estimate.EstimatePeriod = new DateRange() { Start = Tools.ToUTC(x.Estimate.EventStartDate, true), Finish = Tools.ToUTC(x.Estimate.EventEndDate) };
                            x.Estimate.isFirstInitiate = false;
                            newPhases.Add(x);
                        }
                        else
                        {
                            newPhases.Add(d);
                        }
                    }
                }

                data.Phases = newPhases;
                //data.DataInputBy = updatedby;

                //string strMsg = string.Format("WellName : {0}, SequenceID : {1}, Activity Type : {2}, ", data.WellName, data.UARigSequenceId, tempActivity);
                string strMsg = string.Format(" {0} - {1} ", data.WellName, tempActivity);
                //data.Phases.FirstOrDefault().Estimate.Status = status;
                if (status.ToLower().Equals("complete") || status.ToLower().Equals("modified"))
                {
                    if (cfg)
                    {
                        // check before update well Plan
                        if (WellActivity.isHaveWeeklyReport(data.WellName, data.UARigSequenceId, data.Phases.FirstOrDefault().ActivityType))
                        {
                            data.Save();
                            return new SavePlanResult() { Success = false, Message = "Save was successfull! \n" + strMsg };
                            //Json(new { Success = false, Messages = "Business Plan succesfully saved but not pushed to WellPlan because it has active Weekly Report" }, JsonRequestBehavior.AllowGet);
                        }
                        if (WellActivity.isHaveMonthlyLEReport(data.WellName, data.UARigSequenceId, data.Phases.FirstOrDefault().ActivityType))
                        {
                            data.Save(references: new string[] { "updatetowellplan" });
                            return new SavePlanResult() { Success = false, Message = "Save was successfull! \n" + strMsg };
                            //return Json(new { Success = false, Messages = "Business Plan succesfully saved also Well Activity has been updated" }, JsonRequestBehavior.AllowGet);
                        }

                        if (WellActivity.Get<WellActivity>(  
                            Query.And( 
                                Query.EQ("WellName",  data.WellName),  
                                Query.EQ("UARigSequenceId",  data.UARigSequenceId), 
                                Query.EQ("Phases.ActivityType",  data.Phases.FirstOrDefault().ActivityType) 
                                    )
                                ) != null)
                        {
                            data.Save(references: new string[] { "updatetowellplan", "addplanfrombizplan" });
                            // decopling
                            return new SavePlanResult() { Success = false, Message = "Save was successfull! \n" + strMsg };
                        }
                        else
                        {
                            data.Save(references: new string[] { "updatetowellplan" });
                            // decopling
                            return new SavePlanResult() { Success = false, Message = "Save was successfull! \n" + strMsg };
                            //return Json(new { Success = true, Messages = " Well Activity has been updated! \n Status Changed to :" + status + "\n" + strMsg }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    else
                    {
                        data.Save();
                        return new SavePlanResult() { Success = false, Message = "Save was successfull! \n" + strMsg };
                        //return Json(new { Success = true, Messages = " Business Plan Data has been Updated! \n Status Changed to : " + status + "\n" + strMsg }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    data.Save();
                    return new SavePlanResult() { Success = false, Message = "Save was successfull! \n" + strMsg };
                    //return Json(new { Success = true, Messages = "Business Plan Data has been Updated! Status Changed to : " + status + "\n" + strMsg }, JsonRequestBehavior.AllowGet);
                }

            }
            catch (Exception e)
            {
                return new SavePlanResult() { Success = false, Message = e.Message };
                //return Json(new { Success = false, Messages = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class SavePlanResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }

    }

    public class BizPlan : ECISModel
    {
        //public string PlanType { get; set; }
        public BizPlan()
        {
        }
        public override string TableName
        {
            get { return "WEISBizPlans"; }
        }

        public static bool GetBizPlanConfig()
        {
            var pics = Config.GetConfigValue("BizPlanConfig", "");
            if (!pics.Equals(""))
                return Convert.ToBoolean(pics);
            else
                return false;
        }
        public static void UpdateBizplanToWellActivities(bool value)
        {
            try
            {

                Config cfgPic = new Config
                {
                    _id = "BizPlanConfig",
                    ConfigModule = "BizPlan",
                    ConfigValue = value
                };
                cfgPic.Save();
            }
            catch (Exception ex)
            {

            }
        }

        [BsonIgnore]
        public List<BizPlanActivity> BizPlanActivities { get; set; }


        internal static MongoDatabase _db;
        internal static MongoDatabase GetDb(string connection, string database)
        {
            if (_db == null)
            {
                var connectionString = connection;
                var client = new MongoClient("mongodb://" + connectionString);//+ ":27017");
                MongoServer server = client.GetServer();
                MongoDatabase t = server.GetDatabase(database);
                _db = t;
            }
            return _db;
        }

        public static string GetLatestMDBCollection(string colNamePrefix = "")
        {

            string host = System.Configuration.ConfigurationSettings.AppSettings["ServerHost"];
            string db = System.Configuration.ConfigurationSettings.AppSettings["ServerDb"];
            MongoDatabase mongo = GetDb(host, db);

            List<int> res = new List<int>();

            foreach (string t in mongo.GetCollectionNames().ToList())
            {
                if (t.Contains(colNamePrefix))
                {
                    res.Add(Convert.ToInt32(t.Replace(colNamePrefix, "")));
                }
            }



            return colNamePrefix + res.Max().ToString();
        }

        // 1. Get latest _OP_xxxxxxx
        // 2. clear BizPlanActifityInfos 
        // 3. save to BizPlanActifityInfos  
        // 4. loop weiswellactivity and search for every infos data

        public static void GetLatestUploadedOP()
        {
            // 1.
            string dbm = GetLatestMDBCollection("_OP_");
            // 2.
            DataHelper.Delete("WellActivityPhaseInfos");
            // 3.
            var activities = WellActivity.Populate<WellActivity>();

            // 4. Populate LatestOP Data
            var latestData = DataHelper.Populate(dbm);

            foreach (var wa in activities)
            {
                foreach (var ph in wa.Phases)
                {

                    var haveDatas = latestData.Where(x => x.GetString("Well_Name").Equals(wa.WellName) &&
                        x.GetString("Activity_Type\r\n(PTW_Buckets)").Equals(ph.ActivityType) &&
                        x.GetString("Rig_Name").Equals(wa.RigName)
                        );
                    if (haveDatas != null && haveDatas.Count() > 0)
                    {
                        var mst = haveDatas.FirstOrDefault();

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
                    }

                }
            }

            //WellActivity.LoadOP(dbm, false);

        }

        public static void SyncFromWellPlan(bool isSave = true)
        {
            DataHelper.Delete("WEISBizPlanActivities");
            DataHelper.Delete("WEISBizPlanAllocation");
            // reset sequence 
            //BsonDocument dd = new BsonDocument();
            //dd.Set("_id", "WEISBizPlanActivities");
            //dd.Set("Title", "WEISBizPlanActivities");
            //dd.Set("NextNo", 1);
            //dd.Set("Format", "");
            //DataHelper.Save("SequenceNos", dd);



            var wps = WellActivity.Populate<WellActivity>();//Query.And(Query.GT("OpsSchedule.Start", Tools.DefaultDate), Query.GT("OpsSchedule.Finish", Tools.DefaultDate)));

            List<BsonDocument> bdocs = new List<BsonDocument>();
            foreach (var t in wps)
            {
                BsonDocument doc = new BsonDocument();
                doc = t.ToBsonDocument();
                doc.Set("BizPlanId", "OPPlan");
                bdocs.Add(doc);
            }

            List<BizPlanActivity> results = new List<BizPlanActivity>();
            foreach (var b in bdocs)
            {
                var myObj = BsonSerializer.Deserialize<BizPlanActivity>(b);
                results.Add(myObj);
            }

            try
            {

                foreach (var res in results)
                {

                    var newPhases = new List<BizPlanActivityPhase>();
                    if (res.Phases.Any())
                    {
                        foreach (var ph in res.Phases)
                        //.Where(x => WellActivity.isHaveWeeklyReport(res.WellName, res.UARigSequenceId, x.ActivityType) == false ))
                        //&& WellActivity.isHaveMonthlyLEReport(res.WellName, res.UARigSequenceId, x.ActivityType) == false))
                        {

                            var t = WellActivity.isHaveWeeklyReport(res.WellName, res.UARigSequenceId, ph.ActivityType);
                            if ((ph.PhSchedule.Start != Tools.DefaultDate) && t == false)
                            {
                                //ph.LESchedule = new DateRange() { Start = ph.PhSchedule.Start, Finish = ph.PhSchedule.Start.AddDays(ph.LE.Days) };
                                var LEDays = ph.LE;
                                double LENewDays = 0.0;
                                if (LEDays == null)
                                {
                                    LENewDays = 0.0;
                                }
                                else
                                {
                                    LENewDays = LEDays.Days;
                                }
                                ph.LESchedule = new DateRange() { Start = ph.PhSchedule.Start, Finish = ph.PhSchedule.Start.AddDays(LENewDays) };
                                newPhases.Add(ph);
                            }
                        }
                    }
                    res.Phases = newPhases;
                }
            }
            catch (Exception ex)
            {

            }
            if (isSave)
            {
                var newBizPlan = new List<BizPlanActivity>();
                foreach (var x in results)
                {
                    x.Country = "United States";
                    x.ReferenceFactorModel = "default";
                    x.Currency = "USD";
                    x.ShellShare = 20;
                    foreach (var p in x.Phases)
                    {

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


                        if (p.PhSchedule.Start > Tools.DefaultDate)
                        {
                            p.Estimate.EventStartDate = p.PhSchedule.Start;
                        }
                        else
                        {
                            p.Estimate.EventStartDate = x.OpsSchedule.Start;
                        }
                        p.Estimate.RigName = x.RigName;
                        p.Estimate.UsingTAApproved = true;
                        p.Estimate.MaturityLevel = "Type 0";
                        p.Estimate.isFirstInitiate = true;

                        // new 
                        p.Estimate.Country = "United States";
                        p.Estimate.Currency = "USD";
                        //p.Estimate.ShellWorkingInterest = x.WorkingInterest;

                        //string DefaultOP = "OP15";
                        //if (Config.GetConfigValue("BaseOPConfig") != null)
                        //{
                        //    DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
                        //}

                        p.Estimate.SaveToOP = "OP15";
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

                        var getLatest = WellActivityUpdate.GetById(x.WellName, x.UARigSequenceId, p.PhaseNo, null, true);
                        if (getLatest != null)
                        {
                            p.Estimate.CurrentNPTTime = new WellDrillPercentData() { Days = getLatest.NPT.Days, Cost = getLatest.NPT.Cost };
                        }
                        else
                        {
                            p.Estimate.CurrentNPTTime = new WellDrillPercentData() { Days = 0, Cost = 0 };
                        }

                        //TECOP
                        var TECOPTime = p.Estimate.NewTroubleFree.Days * Tools.Div(Tools.Div(getMaturityRisk.TECOPTime, 100), 1 - Tools.Div(getMaturityRisk.TECOPTime, 100));
                        var TECOPCost = p.Estimate.NewTroubleFree.Cost * Tools.Div(Tools.Div(getMaturityRisk.TECOPCost, 100), 1 - Tools.Div(getMaturityRisk.TECOPCost, 100));

                        p.Estimate.NewTECOPTime = new WellDrillPercentData() { PercentDays = getMaturityRisk.TECOPTime, PercentCost = getMaturityRisk.TECOPCost, Days = TECOPTime, Cost = TECOPCost };

                        p.Estimate.EventEndDate = p.Estimate.EventStartDate.AddDays(p.Estimate.NewTroubleFree.Days + p.Estimate.NewNPTTime.Days + p.Estimate.NewTECOPTime.Days);


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
                        var services = 0;
                        var materials = 0;
                        p.Estimate.Services = services;
                        p.Estimate.Materials = materials;

                        var qs = new List<IMongoQuery>();
                        qs.Add(Query.EQ("WellName", x.WellName));
                        qs.Add(Query.EQ("UARigSequenceId", x.UARigSequenceId));
                        qs.Add(Query.EQ("RigName", x.RigName));
                        var q = Query.And(qs);
                        var wa = WellActivity.Get<WellActivity>(q);
                        if (wa != null)
                        {
                            var workingInterest = wa.WorkingInterest;
                            if (workingInterest <= 1)
                            {
                                x.ShellShare = workingInterest * 100;
                            }
                            else
                            {
                                x.ShellShare = workingInterest;
                            }
                        }

                        p.Estimate.WellValueDriver = "";
                        p.Estimate.Status = "";
                        p.Estimate.SelectedCurrency = "USD";

                    }
                    newBizPlan.Add(x);
                    if (x.Phases.Count() > 0)
                    {
                        x.Save();
                        foreach (var p in x.Phases)
                        {
                            p.PushToWellPlan = true;
                        }
                        BizPlanAllocation.SaveBizPlanAllocation(x);
                    }
                }
                //DataHelper.Save("WEISBizPlanActivities", bdocs);
            }

            var lastid = BizPlanActivity.Get<BizPlanActivity>(null, sort: SortBy.Descending("_id"));
            int id = Convert.ToInt32(lastid._id);
            BsonDocument dd = new BsonDocument();
            dd.Set("_id", "WEISBizPlanActivities");
            dd.Set("Title", "WEISBizPlanActivities");
            dd.Set("NextNo", id + 1);
            dd.Set("Format", "");
            DataHelper.Save("SequenceNos", dd);

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
        public static void SyncFromUpload(List<BsonDocument> datas, out List<string> errorMessages, string updatedby)
        {
            List<string> errormsg = new List<string>();
            #region RFM Check
            var rfms = datas.Select(x => BsonHelper.GetString(x, "Reference_Factor_Model")).Distinct().ToList();
            List<string> notavailRfm = new List<string>();
            var groupcase = WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>(null, 0, 0, null, new string[] { "GroupCase" }).Select(x => x.GroupCase).OrderBy(x => x.Any())
                .ToList<string>().Distinct();
            foreach (var y in rfms)
            {
                if (!groupcase.Select(x => x.ToLower().Trim()).Contains(y.ToLower().Trim()))
                    notavailRfm.Add(y);
            }
            if (notavailRfm.Count > 0)
            {
                var msg = string.Join(", ", notavailRfm);
                //throw new Exception("WEIS found that some of Refference Factor Model doesn't exist : " + msg);
                errormsg.Add(msg);
            }
            #endregion

            string PreviousOP = "";
            string NextOP = "";
            string DefaultOP = getBaseOP(out PreviousOP, out NextOP);

            datas = datas.Where(x => x.GetString("Activity_Type") != "").ToList();
            foreach (var b in datas)
            {
                #region read Data
                var saveToOP = b.GetString("Save_To_OP");
                var _stat = b.GetString("Status").ToLower();
                var stat = "";
                switch (_stat)
                {
                    case "draft":
                        stat = "Draft";
                        break;
                    case "complete":
                        stat = "Complete";
                        break;
                    case "modified":
                        stat = "Modified";
                        break;
                    default:
                        stat = "Meta Data Missing";
                        break;
                }

                var lob = b.GetString("Line_Of_Business");
                var region = b.GetString("Region");
                var country = b.GetString("Country");
                var currency = b.GetString("Currency");
                var operationUnit = b.GetString("Operating_Unit");
                var performanceUnit = b.GetString("Performance_Unit");
                var asset = b.GetString("Asset");
                var project = b.GetString("Project");
                var wellName = b.GetString("Well_Name");
                var activityType = b.GetString("Activity_Type");
                var activityCategory = b.GetString("Activity_Category");
                var scopeDesc = b.GetString("Scope_Description");
                var workingInterest = b.GetDouble("Shell_Working_Interest");
                var fundingType = b.GetString("Funding_Type");
                var learningCurveFactor = b.GetDouble("Learning_Curve_Factor_%");

                var inPlan = false;
                var _inPlan = b.GetString("In_Plan");
                if (_inPlan != null)
                {
                    if (_inPlan.Trim().ToLower() == "yes")
                    {
                        inPlan = true;
                    }
                }


                var rfm = "";
                if (groupcase.Contains(b.GetString("Reference_Factor_Model")))
                    rfm = b.GetString("Reference_Factor_Model");
                else
                    rfm = "default";

                var rigName = b.GetString("Rig_Name");
                var rigType = b.GetString("Rig_Type");
                var rigSeqId = b.GetString("UA_Rig_Sequence_Id");

                DateTime PlanStart = Tools.DefaultDate;
                DateTime PlanFinish = Tools.DefaultDate;
                DateTime LSStart = Tools.DefaultDate;
                DateTime LSFinish = Tools.DefaultDate;
                #region Date

                try
                {
                    if (!b.GetString("OP16_Start").Trim().Equals(""))
                    {
                        PlanStart =
                            Tools.ToUTC(DateTime.ParseExact(b.GetString("OP16_Start"), "yyyy-MM-dd HH:mm:ss",
                                System.Globalization.CultureInfo.InvariantCulture).Date);
                        LSStart = Tools.ToUTC(DateTime.ParseExact(b.GetString("OP16_Start"), "yyyy-MM-dd HH:mm:ss",
                                System.Globalization.CultureInfo.InvariantCulture).Date);
                    }
                }
                catch (Exception ex)
                {

                    //throw new   Exception("")
                }

                //try
                //{
                //    if (!b.GetString("OP_Finish").Trim().Equals(""))
                //    {
                //        PlanFinish =
                //            Tools.ToUTC(DateTime.ParseExact(b.GetString("OP_Finish"), "yyyy-MM-dd hh:mm:ss",
                //                System.Globalization.CultureInfo.InvariantCulture));
                //        LSFinish =
                //            Tools.ToUTC(DateTime.ParseExact(b.GetString("OP_Finish"), "yyyy-MM-dd hh:mm:ss",
                //                System.Globalization.CultureInfo.InvariantCulture));
                //    }
                //}
                //catch { }

                //try
                //{
                //    if (!b.GetString("Operation_Start").Trim().Equals(""))
                //        LSStart = DateTime.ParseExact(b.GetString("Operation_Start"), "yyyy-MM-dd hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                //}
                //catch { }
                //try
                //{
                //    if (!b.GetString("Operation_Finish").Trim().Equals(""))
                //        LSFinish = DateTime.ParseExact(b.GetString("Operation_Finish"), "yyyy-MM-dd hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                //}
                //catch { }
                #endregion
                var maturityRisk = b.GetString("Estimate_Maturity");

                if (!maturityRisk.Trim().Equals(""))
                {
                    var tempMat = maturityRisk.Split(' '); maturityRisk = "Type " + tempMat[1];
                }
                var services = b.GetDouble("Services_USD_$");
                var material = b.GetDouble("Material_USD_$");
                var longleadperc = b.GetDouble("Material_Long_Lead_%");
                var longleadmonth = b.GetDouble("Long_Lead_Required_Month");
                var troublefreetime = b.GetDouble("Trouble_Free_Time");
                var npttimeperc = b.GetDouble("NPT_Time_%");
                var nptcostperc = b.GetDouble("NPT_Cost_%");
                var tecoptimeperc = b.GetDouble("TECOP_Time_%");
                var tecopcostperc = b.GetDouble("TECOP_Cost_%");

                #region Well Value Driver
                var wellvaluedriver = b.GetString("Wells_Value_Driver");
                if (!wellvaluedriver.Trim().Equals(""))
                {
                    var tempMat = wellvaluedriver.Replace(" ", "").ToLower();
                    switch (tempMat)
                    {
                        case "totaldays":
                            {
                                wellvaluedriver = "Total Days";
                                break;
                            }
                        case "dryholedays":
                            {
                                wellvaluedriver = "Dry Hole Days";
                                break;
                            }
                        case "cost":
                            {
                                wellvaluedriver = "Cost";
                                break;
                            }
                        case "benchmarknotavail":
                            {
                                wellvaluedriver = "Benchmark Not Avail";
                                break;
                            }
                        default:
                            {
                                wellvaluedriver = "";
                                break;
                            }
                    }
                }
                #endregion

                var dryholedays = b.GetDouble("Dry_Hole_Days");
                var tqvalue = b.GetDouble("TQ_for_Value_Driver");
                var bicvalue = b.GetDouble("BIC_for_Value_Driver");
                var performancescore = b.GetString("Performance_Score");
                var waterdepth = b.GetDouble("Water_Depth");
                var totalwaterdepth = b.GetDouble("Total_Well_Depth");
                var nofocasing = b.GetDouble("Number_of_Casing");
                var mechanicalriskindex = b.GetDouble("Mechanical_Risk_Index");
                var brinedensity = b.GetDouble("Brine_Density");
                var complInfo = b.GetString("Completion_Info");
                var complType = b.GetString("Completion_Type");
                var noOfZone = b.GetDouble("Number_of_Zones");

                #endregion
                var q = Query.And(
                    Query.EQ("WellName", wellName),
                    Query.EQ("Phases.ActivityType", activityType),
                    Query.EQ("Phases.Estimate.RigName", rigName),
                    Query.EQ("UARigSequenceId", rigSeqId)
                    );

                var bp = BizPlanActivity.Get<BizPlanActivity>(q);
                if (bp != null)
                {
                    bp.Phases = bp.Phases.Where(x => !String.IsNullOrEmpty(x.ActivityType)).ToList();
                    #region Update
                    #region check for Missing Meta Data
                    if (String.IsNullOrEmpty(performanceUnit) || String.IsNullOrEmpty(asset) || String.IsNullOrEmpty(region) || String.IsNullOrEmpty(project) || String.IsNullOrEmpty(country) || String.IsNullOrEmpty(rfm) || rfm=="Default")
                    {
                        stat = "Meta Data Missing";
                    }
                    #endregion
                    bp.BizPlanId = "OPPlan";
                    bp.Status = stat;
                    bp.LineOfBusiness = lob;
                    bp.Region = region;
                    bp.Country = country;
                    bp.Currency = currency;
                    bp.OperatingUnit = operationUnit;
                    bp.PerformanceUnit = performanceUnit;
                    bp.AssetName = asset;
                    bp.ProjectName = project;
                    bp.WorkingInterest = workingInterest;
                    bp.ShellShare = workingInterest * 100;

                    bp.isInPlan = inPlan;
                    //bp.FirmOrOption = planningClass;
                    bp.ReferenceFactorModel = rfm;
                    bp.RigType = rigType;
                    bp.DataInputBy = updatedby;
                    //bp.WellName = wellName;
                    //bp.RigName = rigName;
                    //bp.UARigSequenceId = rigSeqId;

                    var nPh = bp.Phases.Where(x => x.ActivityType.Equals(activityType)).FirstOrDefault();// && x.Estimate.SaveToOP.Equals(saveToOP)
                    bool isNewPhase = false;
                    if (nPh == null)
                    {
                        nPh = new BizPlanActivityPhase();
                        nPh.Estimate = new BizPlanActivityEstimate();
                        isNewPhase = true;
                    }
                    nPh.ActivityCategory = activityCategory;
                    nPh.ActivityType = activityType;
                    nPh.ActivityDesc = scopeDesc;
                    nPh.FundingType = fundingType;

                    nPh.Estimate.NewLearningCurveFactor = learningCurveFactor;//(learningCurveFactor > 0 && learningCurveFactor <= 1) ? learningCurveFactor * 100 : learningCurveFactor;//learningCurveFactor < 1 && learningCurveFactor != 0 ? (learningCurveFactor) : Tools.Div(learningCurveFactor, 100);
                    nPh.Estimate.SaveToOP = saveToOP;
                    nPh.Estimate.Country = country;
                    //nPh.Estimate.ShellShareCalc = workingInterest;
                    nPh.Estimate.RigName = rigName;
                    //If update, no need to set LS!!
                    //nPh.Estimate.EventStartDate = LSStart;
                    //nPh.Estimate.EventEndDate = LSFinish;
                    nPh.Estimate.MaturityLevel = maturityRisk;
                    nPh.Estimate.Services = services;
                    nPh.Estimate.Materials = material;
                    nPh.Estimate.PercOfMaterialsLongLead = longleadperc * 100;//(longleadperc < 1 ? longleadperc * 100 : longleadperc);//(longleadperc > 0 && longleadperc <= 1) ? longleadperc * 100 : longleadperc;// > 0 && longleadperc != 0 ? (learningCurveFactor) : Tools.Div(longleadperc, 100);
                    nPh.Estimate.LongLeadMonthRequired = longleadmonth;

                    nPh.Estimate.NewTroubleFree.Days = troublefreetime;



                    nPh.Estimate.NewNPTTime.PercentDays = (npttimeperc > 0 && npttimeperc <= 1) ? npttimeperc * 100 : npttimeperc;
                    nPh.Estimate.NewNPTTime.PercentCost = (nptcostperc > 0 && nptcostperc <= 1) ? nptcostperc * 100 : nptcostperc;
                    nPh.Estimate.NewTECOPTime.PercentDays = (tecoptimeperc > 0 && tecoptimeperc <= 1) ? tecoptimeperc * 100 : tecoptimeperc;
                    nPh.Estimate.NewTECOPTime.PercentCost = (tecopcostperc > 0 && tecopcostperc <= 1) ? tecopcostperc * 100 : tecopcostperc;

                    nPh.Estimate.WellValueDriver = wellvaluedriver;
                    nPh.Estimate.DryHoleDays = dryholedays;
                    nPh.Estimate.TQValueDriver = tqvalue;
                    nPh.Estimate.BICValueDriver = bicvalue;
                    nPh.Estimate.PerformanceScore = performancescore;
                    nPh.Estimate.WaterDepth = waterdepth;
                    nPh.Estimate.WellDepth = totalwaterdepth;
                    nPh.Estimate.NumberOfCasings = nofocasing;
                    nPh.Estimate.MechanicalRiskIndex = mechanicalriskindex;
                    nPh.Estimate.BrineDensity = brinedensity;
                    nPh.Estimate.CompletionInfo = complInfo;
                    nPh.Estimate.CompletionType = complType;
                    nPh.Estimate.NumberOfCompletionZones = noOfZone;


                    #region Phase Info
                    // do phase info after process done
                    nPh.PhaseInfo = new WellActivityPhaseInfo();
                    #endregion

                    #region Detail

                    nPh.Estimate.UsingTAApproved = false;
                    nPh.Estimate.IsUsingLCFfromRFM = false;
                    nPh.Estimate.isFirstInitiate = false;
                    nPh.Estimate.isMaterialLLSetManually = true;

                    // take from wellPlan if update 
                    nPh.Estimate.CurrentTroubleFree = new WellDrillData();
                    nPh.Estimate.TroubleFreeBeforeLC = new WellDrillData() { Days = troublefreetime, Cost = 0 };
                    nPh.Estimate.NewTroubleFree = new WellDrillData() { Days = troublefreetime, Cost = 0 };

                    #region unused
                    //var getMaturityRisk = MaturityRiskMatrix.Get<MaturityRiskMatrix>(Query.EQ("Title", nPh.Estimate.MaturityLevel)) ?? new MaturityRiskMatrix();
                    //var NPTTime = nPh.Estimate.NewTroubleFree.Days *
                    //    Tools.Div(Tools.Div(getMaturityRisk.NPTTime, 100), 1 - Tools.Div(getMaturityRisk.NPTTime, 100));
                    //var NPTCost = nPh.Estimate.NewTroubleFree.Cost *
                    //    Tools.Div(Tools.Div(getMaturityRisk.NPTCost, 100), 1 - Tools.Div(getMaturityRisk.NPTCost, 100));

                    //nPh.Estimate.NewNPTTime = new WellDrillPercentData()
                    //{
                    //    Days = NPTTime,
                    //    Cost = NPTCost,
                    //    PercentCost = getMaturityRisk.NPTCost,
                    //    PercentDays = getMaturityRisk.NPTTime
                    //};

                    // current NPT
                    //var getLatest = WellActivityUpdate.GetById(
                    //    bp.WellName, bp.UARigSequenceId, nPh.PhaseNo, null, true);
                    //if (getLatest != null)
                    //    nPh.Estimate.CurrentNPTTime = new WellDrillPercentData() { Days = getLatest.NPT.Days, Cost = getLatest.NPT.Cost };
                    //else
                    //    nPh.Estimate.CurrentNPTTime = new WellDrillPercentData() { Days = 0, Cost = 0 };


                    //var TECOPTime = nPh.Estimate.NewTroubleFree.Days *
                    //    Tools.Div(Tools.Div(getMaturityRisk.TECOPTime, 100), 1 - Tools.Div(getMaturityRisk.TECOPTime, 100));
                    //var TECOPCost = nPh.Estimate.NewTroubleFree.Cost *
                    //    Tools.Div(Tools.Div(getMaturityRisk.TECOPCost, 100), 1 - Tools.Div(getMaturityRisk.TECOPCost, 100));

                    //nPh.Estimate.NewTECOPTime = new WellDrillPercentData()
                    //{
                    //    PercentDays = getMaturityRisk.TECOPTime,
                    //    PercentCost = getMaturityRisk.TECOPCost,
                    //    Days = TECOPTime,
                    //    Cost = TECOPCost
                    //};
                    #endregion

                    //var learningCurveCalc = nPh.Estimate.NewTroubleFree.Days;
                    //if (learningCurveFactor != 0)
                    //{
                    //    var lcfCalc = troublefreetime *
                    //                        (learningCurveFactor < 1
                    //                            ? learningCurveFactor
                    //                            : Tools.Div(learningCurveFactor, 100));
                    //    learningCurveCalc = learningCurveCalc - lcfCalc;
                    //}

                    //nPh.Estimate.EventEndDate
                    //PlanFinish = nPh.Estimate.EventStartDate.AddDays(nPh.Estimate.NewTroubleFree.Days + nPh.Estimate.NewNPTTime.Days + nPh.Estimate.NewTECOPTime.Days + learningCurveCalc);

                    //nPh.Estimate.NewTroubleFreeUSD = nPh.Estimate.NewTroubleFree.Cost;
                    //nPh.Estimate.NPTCostUSD = nPh.Estimate.NewNPTTime.Cost;
                    //nPh.Estimate.TECOPCostUSD = nPh.Estimate.NewTECOPTime.Cost;
                    //nPh.Estimate.MeanUSD = nPh.Estimate.NewMean.Cost;



                    //nPh.Plan.Days = new DateRange(PlanStart, PlanFinish).Days + 1;
                    //nPh.OP.Days = new DateRange(LSStart, LSFinish).Days + 1;


                    //material long lead
                    var DateEscStartMaterial = nPh.Estimate.EventStartDate;
                    var tangibleValue = longleadperc * 100;
                    var monthRequired = longleadmonth;
                    var getMonthLongLead = System.Convert.ToInt32(-1 * Math.Round(monthRequired));
                    DateEscStartMaterial = nPh.Estimate.EventStartDate.AddMonths(getMonthLongLead);

                    #region remarked because it is same with set to manually

                    //var actType = "";
                    //if (nPh.ActivityType != null)
                    //{
                    //    var getActCategory = ActivityMaster.Get<ActivityMaster>(Query.EQ("_id", nPh.ActivityType));
                    //    if (getActCategory != null)
                    //        actType = getActCategory.ActivityCategory;
                    //}

                    //if (actType != null && actType != "")
                    //{
                    //    var year = nPh.Estimate.EventStartDate.Year;
                    //    var getLongLead = LongLead.Get<LongLead>(Query.EQ("Title", actType));
                    //    if (getLongLead != null && getLongLead.Details != null && getLongLead.Details.Count > 0)
                    //    {
                    //        var getTangible = getLongLead.Details.Where(y => y.Year.Equals(year)).FirstOrDefault();
                    //        if (getTangible != null)
                    //        {
                    //            tangibleValue = getTangible.TangibleValue;
                    //            monthRequired = getTangible.MonthRequiredValue;
                    //            var getMonthLongLead = System.Convert.ToInt32(-1 * Math.Round(monthRequired));
                    //            DateEscStartMaterial = nPh.Estimate.EventStartDate.AddMonths(getMonthLongLead);
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //    tangibleValue = 0;
                    //    monthRequired = 0;
                    //    var getMonthLongLead = System.Convert.ToInt32(-1 * Math.Round(monthRequired));
                    //    DateEscStartMaterial = nPh.Estimate.EventStartDate.AddMonths(getMonthLongLead);
                    //}

                    //if (DateEscStartMaterial < DateTime.Now)
                    //    DateEscStartMaterial = DateTime.Now;
                    #endregion

                    nPh.Estimate.StartEscDateMaterial = DateEscStartMaterial;
                    nPh.Estimate.PercOfMaterialsLongLead = tangibleValue;
                    nPh.Estimate.LongLeadMonthRequired = monthRequired;
                    nPh.Estimate.WellValueDriver = wellvaluedriver;
                    nPh.Estimate.Status = stat;

                    #endregion


                    #region recalculate

                    double iServices = services;
                    double iMaterials = material;

                    if (!currency.Trim().ToUpper().Equals("USD"))
                    {
                        var getCur = MacroEconomic.Populate<MacroEconomic>(
                            Query.And(
                                Query.EQ("Currency", currency),
                                Query.EQ("BaseOP", saveToOP)
                                )
                            );
                        if (getCur.Any())
                        {
                            var cx = getCur.Where(x => x.Currency.Equals(currency)).FirstOrDefault();

                            var rate = cx.ExchangeRate.AnnualValues.Where(x => x.Year == PlanStart.Year).Any() ?
                                    cx.ExchangeRate.AnnualValues.Where(x => x.Year == PlanStart.Year).FirstOrDefault().Value
                                    : 0;

                            // dibagi rate : untuk jadi USD
                            iServices = Tools.Div(iServices, rate);
                            iMaterials = Tools.Div(iMaterials, rate);
                        }
                        else
                        {
                            iServices = Tools.Div(iServices, 1);
                            iMaterials = Tools.Div(iMaterials, 1);
                        }
                    }

                    var maturityLevel = maturityRisk.Substring(maturityRisk.Length - 1, 1);

                    //var newTroubleFreeDays = learningCurveCalc;
                    //nPh.Estimate.NewTroubleFree.Days = learningCurveCalc;



                    BizPlanSummary res = new BizPlanSummary(wellName, rigName, activityType, country,
                        workingInterest, new DateRange() { Start = PlanStart, Finish = PlanStart }, Convert.ToInt32(maturityLevel),
                        iServices, iMaterials, nPh.Estimate.TroubleFreeBeforeLC.Days, rfm,
                        npttimeperc > 1 ? Tools.Div(npttimeperc, 100) : npttimeperc,
                        tecoptimeperc > 1 ? Tools.Div(tecoptimeperc, 100) : tecoptimeperc,
                        nptcostperc > 1 ? Tools.Div(nptcostperc, 100) : nptcostperc,
                        tecopcostperc > 1 ? Tools.Div(tecopcostperc, 100) : tecopcostperc,
                        longleadmonth,
                        longleadperc > 1 ? Tools.Div(longleadperc, 100) : longleadperc,
                        baseOP: saveToOP, lcf: learningCurveFactor);

                    res.GeneratePeriodBucket();
                    res.GenerateAnnualyBucket(res.MonthlyBuckets);

                    var finishDate = res.EventPeriod.Start.Date.AddDays(res.MeanTime);
                    res.EventPeriod.Finish = finishDate; //.AddDays(1);
                    //nPh.Estimate.Status = stat;

                    nPh.Estimate.SelectedCurrency = currency;
                    nPh.Estimate.Currency = "USD";

                    nPh.Estimate.NewBaseValue.Days = res.NewBaseValue.Days;
                    nPh.Estimate.NewBaseValue.Cost = res.NewBaseValue.Cost;
                    nPh.Estimate.NewLCFValue.Days = res.NewLCFValue.Days;
                    nPh.Estimate.NewLCFValue.Cost = res.NewLCFValue.Cost;

                    nPh.Estimate.SpreadRate = res.SpreadRateWRig;
                    nPh.Estimate.SpreadRateTotal = res.SpreadRateTotal;
                    nPh.Estimate.SpreadRateUSD = res.SpreadRateWRig;
                    nPh.Estimate.SpreadRateTotalUSD = res.SpreadRateTotal;

                    nPh.Estimate.BurnRate = res.BurnRate;
                    nPh.Estimate.BurnRateUSD = res.BurnRate;
                    nPh.Estimate.RigRate = res.RigRate;
                    nPh.Estimate.RigRatesUSD = res.RigRate;

                    nPh.Estimate.NewTroubleFree.Cost = res.TroubleFreeCostUSD;
                    nPh.Estimate.NewTroubleFreeUSD = res.TroubleFreeCostUSD;
                    nPh.Estimate.NewNPTTime.Cost = res.NPTCostUSD;
                    nPh.Estimate.NewTECOPTime.Cost = res.TECOPCostUSD;

                    nPh.Estimate.NewNPTTime.Days = res.NPT.Days;
                    nPh.Estimate.NewTECOPTime.Days = res.TECOP.Days;

                    nPh.Estimate.NPTCostUSD = res.NPTCostUSD;
                    nPh.Estimate.TECOPCostUSD = res.TECOPCostUSD;
                    nPh.Estimate.NewMean.Cost = res.MeanCostEDM;
                    nPh.Estimate.NewMean.Days = res.MeanTime;
                    nPh.Estimate.MeanUSD = res.MeanCostEDM;
                    nPh.Estimate.MeanCostMOD = res.MeanCostMOD;
                    nPh.Estimate.ShellShareCalc = res.ShellShareCost;

                    nPh.Estimate.LongLeadCalc = nPh.Estimate.Materials * Tools.Div(nPh.Estimate.PercOfMaterialsLongLead, 100);

                    nPh.PushToWellPlan = true;

                    if (nPh.Estimate.SaveToOP == DefaultOP)
                    {
                        nPh.PlanSchedule.Start = PlanStart;
                        nPh.PlanSchedule.Finish = PlanFinish;
                    }
                    else
                    {
                        var PeriodFinish = res.EventPeriod.Start.Date.AddDays(res.MeanTime);
                        nPh.Estimate.EstimatePeriod.Start = PlanStart;
                        nPh.Estimate.EstimatePeriod.Finish = PeriodFinish.AddDays(1);
                        nPh.Estimate.EventStartDate = PlanStart;
                        nPh.Estimate.EventEndDate = PeriodFinish.AddDays(1);
                    }

                    #endregion
                    bp.Phases = new List<BizPlanActivityPhase>() { nPh };
                    //if (isNewPhase)
                    //    bp.Phases.Add(nPh);
                    //else
                    //bp.Phases.Add(nPh);
                    #endregion

                    //var p = bp.Phases.FirstOrDefault();

                    //p.Estimate.EstimatePeriod.Start = Tools.ToUTC(p.Estimate.EstimatePeriod.Start, true);
                    //p.Estimate.EstimatePeriod.Finish = Tools.ToUTC(p.Estimate.EstimatePeriod.Finish);
                    //p.Estimate.EventStartDate = Tools.ToUTC(p.Estimate.EventStartDate, true);
                    //p.Estimate.EventEndDate = Tools.ToUTC(p.Estimate.EventEndDate);


                    ////var maturityLevel = p.Estimate.MaturityLevel.Substring(p.Estimate.MaturityLevel.Length - 1, 1);

                    ////double iServices = p.Estimate.Services;
                    ////double iMaterials = p.Estimate.Materials;


                    //bp.Phases.FirstOrDefault().Estimate.NewTroubleFree.Days = bp.Phases.FirstOrDefault().Estimate.TroubleFreeBeforeLC.Days;

                    //BizPlanSummary bisCal = new BizPlanSummary(bp.WellName, p.Estimate.RigName, p.ActivityType, bp.Country,
                    //    bp.ShellShare, p.Estimate.EstimatePeriod, Convert.ToInt32(p.Estimate.MaturityLevel.Replace("Type", "")),
                    //    iServices, iMaterials,
                    //    p.Estimate.TroubleFreeBeforeLC.Days,
                    //    bp.ReferenceFactorModel,
                    //    Tools.Div(p.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentDays, 100),
                    //    Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100),
                    //    p.Estimate.LongLeadMonthRequired, Tools.Div(p.Estimate.PercOfMaterialsLongLead, 100), baseOP: p.Estimate.SaveToOP,
                    //    isGetExcRateByCurrency: true,
                    //    lcf: p.Estimate.NewLearningCurveFactor
                    //    );

                    //bisCal.GeneratePeriodBucket();

                    var saveMethod = bp.SavePlanMethod(bp, updatedby, stat);
                    errormsg.Add(saveMethod.Message);
                }
                else
                {

                }
                #region Update 11 May, no record found, dont make new!!
                //else
                //{
                //    q = Query.And(
                //    Query.EQ("WellName", wellName),
                //    Query.EQ("Phases.Estimate.RigName", rigName)
                //    );
                //    bp = BizPlanActivity.Get<BizPlanActivity>(q);
                //    if (bp != null)
                //    {
                //        bp.Phases = bp.Phases.Where(x => !String.IsNullOrEmpty(x.ActivityType)).ToList();
                //        #region add to Phases

                //        bp.BizPlanId = "OPPlan";
                //        bp.Status = stat;
                //        bp.LineOfBusiness = lob;
                //        bp.Region = region;
                //        bp.Country = country;
                //        bp.Currency = currency;
                //        bp.OperatingUnit = operationUnit;
                //        bp.PerformanceUnit = performanceUnit;
                //        bp.AssetName = asset;
                //        bp.ProjectName = project;

                //        bp.isInPlan = inPlan;
                //        //bp.FirmOrOption = planningClass;
                //        bp.ReferenceFactorModel = rfm;
                //        bp.RigType = rigType;
                //        bp.WorkingInterest = workingInterest;
                //        bp.ShellShare = workingInterest * 100;

                //        //bp.WellName = wellName;
                //        //bp.RigName = rigName;
                //        //bp.UARigSequenceId = rigSeqId;

                //        var nPh = bp.Phases.FirstOrDefault(x => x.ActivityType.Equals(activityType) && x.Estimate.SaveToOP.Equals(saveToOP));
                //        bool isNewPhase = false;
                //        if (nPh == null)
                //        {
                //            nPh = new BizPlanActivityPhase();
                //            nPh.Estimate = new BizPlanActivityEstimate();
                //            isNewPhase = true;
                //            nPh.PhaseNo = bp.Phases.Select(x => x.PhaseNo).Max() + 1;
                //        }
                //        nPh.ActivityCategory = activityCategory;
                //        nPh.ActivityType = activityType;
                //        nPh.ActivityDesc = scopeDesc;
                //        nPh.FundingType = fundingType;
                //        //nPh.PlanSchedule.Start = PlanStart;
                //        //nPh.PlanSchedule.Finish = PlanFinish;
                //        //nPh.PhSchedule.Start = LSStart;
                //        //nPh.PhSchedule.Finish = LSFinish;

                //        //nPh.Plan.Days = new DateRange(PlanStart, PlanFinish).Days + 1;
                //        //nPh.OP.Days = new DateRange(LSStart, LSFinish).Days + 1;

                //        nPh.Estimate.NewLearningCurveFactor = learningCurveFactor;
                //        nPh.Estimate.SaveToOP = saveToOP;
                //        nPh.Estimate.Country = country;
                //        nPh.Estimate.ShellShareCalc = workingInterest;
                //        nPh.Estimate.RigName = rigName;
                //        nPh.Estimate.EventStartDate = LSStart;
                //        nPh.Estimate.EventEndDate = LSFinish;
                //        nPh.Estimate.MaturityLevel = maturityRisk;
                //        nPh.Estimate.Services = services;
                //        nPh.Estimate.Materials = material;
                //        nPh.Estimate.LongLeadCalc = longleadperc;
                //        nPh.Estimate.LongLeadMonthRequired = longleadmonth;

                //        nPh.Estimate.NewTroubleFree.Days = troublefreetime;

                //        nPh.Estimate.NewNPTTime.PercentDays = npttimeperc;
                //        nPh.Estimate.NewNPTTime.PercentCost = nptcostperc;
                //        nPh.Estimate.NewTECOPTime.PercentDays = tecoptimeperc;
                //        nPh.Estimate.NewTECOPTime.PercentCost = tecopcostperc;

                //        nPh.Estimate.WellValueDriver = wellvaluedriver;
                //        nPh.Estimate.DryHoleDays = dryholedays;
                //        nPh.Estimate.TQ.Threshold = tqvalue;
                //        nPh.Estimate.BIC.Threshold = bicvalue;
                //        nPh.Estimate.PerformanceScore = performancescore;
                //        nPh.Estimate.WaterDepth = waterdepth;
                //        nPh.Estimate.WaterDepth = totalwaterdepth;
                //        nPh.Estimate.NumberOfCasings = nofocasing;
                //        nPh.Estimate.MechanicalRiskIndex = mechanicalriskindex;
                //        nPh.Estimate.BrineDensity = brinedensity;
                //        nPh.Estimate.CompletionInfo = complInfo;
                //        nPh.Estimate.CompletionType = complType;
                //        nPh.Estimate.NumberOfCompletionZones = noOfZone;

                //        #region Phase Info
                //        // do phase info after process done
                //        nPh.PhaseInfo = new WellActivityPhaseInfo();
                //        #endregion

                //        #region Detail

                //        nPh.Estimate.UsingTAApproved = false;
                //        nPh.Estimate.IsUsingLCFfromRFM = false;
                //        // take from wellPlan if update 
                //        nPh.Estimate.CurrentTroubleFree = new WellDrillData();
                //        nPh.Estimate.NewTroubleFree = new WellDrillData() { Days = troublefreetime, Cost = 0 };


                //        //var getMaturityRisk = MaturityRiskMatrix.Get<MaturityRiskMatrix>(Query.EQ("Title", nPh.Estimate.MaturityLevel)) ?? new MaturityRiskMatrix();
                //        //var NPTTime = nPh.Estimate.NewTroubleFree.Days *
                //        //    Tools.Div(Tools.Div(getMaturityRisk.NPTTime, 100), 1 - Tools.Div(getMaturityRisk.NPTTime, 100));
                //        //var NPTCost = nPh.Estimate.NewTroubleFree.Cost *
                //        //    Tools.Div(Tools.Div(getMaturityRisk.NPTCost, 100), 1 - Tools.Div(getMaturityRisk.NPTCost, 100));

                //        //nPh.Estimate.NewNPTTime = new WellDrillPercentData()
                //        //{
                //        //    Days = NPTTime,
                //        //    Cost = NPTCost,
                //        //    PercentCost = getMaturityRisk.NPTCost,
                //        //    PercentDays = getMaturityRisk.NPTTime
                //        //};

                //        //// current NPT
                //        //var getLatest = WellActivityUpdate.GetById(
                //        //    bp.WellName, bp.UARigSequenceId, nPh.PhaseNo, null, true);
                //        //if (getLatest != null)
                //        //    nPh.Estimate.CurrentNPTTime = new WellDrillPercentData() { Days = getLatest.NPT.Days, Cost = getLatest.NPT.Cost };
                //        //else
                //        //    nPh.Estimate.CurrentNPTTime = new WellDrillPercentData() { Days = 0, Cost = 0 };


                //        //var TECOPTime = nPh.Estimate.NewTroubleFree.Days *
                //        //    Tools.Div(Tools.Div(getMaturityRisk.TECOPTime, 100), 1 - Tools.Div(getMaturityRisk.TECOPTime, 100));
                //        //var TECOPCost = nPh.Estimate.NewTroubleFree.Cost *
                //        //    Tools.Div(Tools.Div(getMaturityRisk.TECOPCost, 100), 1 - Tools.Div(getMaturityRisk.TECOPCost, 100));

                //        //nPh.Estimate.NewTECOPTime = new WellDrillPercentData()
                //        //{
                //        //    PercentDays = getMaturityRisk.TECOPTime,
                //        //    PercentCost = getMaturityRisk.TECOPCost,
                //        //    Days = TECOPTime,
                //        //    Cost = TECOPCost
                //        //};

                //        var learningCurveCalc = nPh.Estimate.NewTroubleFree.Days;
                //        if (learningCurveFactor != 0)
                //        {
                //            var lcfCalc = nPh.Estimate.NewTroubleFree.Days *
                //                                (learningCurveFactor < 1
                //                                    ? learningCurveFactor
                //                                    : Tools.Div(learningCurveFactor, 100));
                //            learningCurveCalc = learningCurveCalc - lcfCalc;
                //        }

                //        //nPh.Estimate.EventEndDate =
                //        //    nPh.Estimate.EventStartDate.AddDays(
                //        //    nPh.Estimate.NewTroubleFree.Days + nPh.Estimate.NewNPTTime.Days + nPh.Estimate.NewTECOPTime.Days + learningCurveCalc);

                //        //nPh.Estimate.NewTroubleFreeUSD = nPh.Estimate.NewTroubleFree.Cost;
                //        //nPh.Estimate.NPTCostUSD = nPh.Estimate.NewNPTTime.Cost;
                //        //nPh.Estimate.TECOPCostUSD = nPh.Estimate.NewTECOPTime.Cost;
                //        //nPh.Estimate.MeanUSD = nPh.Estimate.NewMean.Cost;

                //        //nPh.Estimate.EstimatePeriod = new DateRange()
                //        //{
                //        //    Start = nPh.Estimate.EventStartDate,
                //        //    Finish = nPh.Estimate.EventEndDate
                //        //};


                //        PlanFinish = nPh.Estimate.EventStartDate.AddDays(nPh.Estimate.NewTroubleFree.Days + nPh.Estimate.NewNPTTime.Days + nPh.Estimate.NewTECOPTime.Days + learningCurveCalc);

                //        nPh.Estimate.NewTroubleFreeUSD = nPh.Estimate.NewTroubleFree.Cost;
                //        nPh.Estimate.NPTCostUSD = nPh.Estimate.NewNPTTime.Cost;
                //        nPh.Estimate.TECOPCostUSD = nPh.Estimate.NewTECOPTime.Cost;
                //        nPh.Estimate.MeanUSD = nPh.Estimate.NewMean.Cost;

                //        if (nPh.Estimate.SaveToOP == DefaultOP)
                //        {
                //            nPh.PlanSchedule.Start = PlanStart;
                //            nPh.PlanSchedule.Finish = PlanFinish;
                //        }
                //        else
                //        {
                //            nPh.Estimate.EstimatePeriod.Start = PlanStart;
                //            nPh.Estimate.EstimatePeriod.Finish = PlanFinish;
                //            nPh.Estimate.EventStartDate = PlanStart;
                //            nPh.Estimate.EventEndDate = PlanFinish;
                //        }

                //        nPh.PhSchedule.Start = PlanStart;
                //        nPh.PhSchedule.Finish = PlanFinish;
                //        nPh.Plan.Days = new DateRange(PlanStart, PlanFinish).Days + 1;
                //        nPh.OP.Days = new DateRange(PlanStart, PlanFinish).Days + 1;


                //        //material long lead
                //        var DateEscStartMaterial = nPh.Estimate.EventStartDate;
                //        var tangibleValue = 0.0;
                //        var monthRequired = 0.0;
                //        var actType = "";
                //        var getActCategory = ActivityMaster.Get<ActivityMaster>(Query.EQ("_id", nPh.ActivityType));
                //        if (getActCategory != null)
                //            actType = getActCategory.ActivityCategory;

                //        if (actType != null && actType != "")
                //        {
                //            var year = nPh.Estimate.EventStartDate.Year;
                //            var getLongLead = LongLead.Get<LongLead>(Query.EQ("Title", actType));
                //            if (getLongLead != null && getLongLead.Details != null && getLongLead.Details.Count > 0)
                //            {
                //                var getTangible = getLongLead.Details.Where(y => y.Year.Equals(year)).FirstOrDefault();
                //                if (getTangible != null)
                //                {
                //                    tangibleValue = getTangible.TangibleValue;
                //                    monthRequired = getTangible.MonthRequiredValue;
                //                    var getMonthLongLead = System.Convert.ToInt32(-1 * Math.Round(monthRequired));
                //                    DateEscStartMaterial = nPh.Estimate.EventStartDate.AddMonths(getMonthLongLead);
                //                }
                //            }
                //        }
                //        else
                //        {
                //            tangibleValue = 0;
                //            monthRequired = 0;
                //            var getMonthLongLead = System.Convert.ToInt32(-1 * Math.Round(monthRequired));
                //            DateEscStartMaterial = nPh.Estimate.EventStartDate.AddMonths(getMonthLongLead);
                //        }

                //        if (DateEscStartMaterial < DateTime.Now)
                //            DateEscStartMaterial = DateTime.Now;

                //        nPh.Estimate.StartEscDateMaterial = DateEscStartMaterial;
                //        nPh.Estimate.PercOfMaterialsLongLead = tangibleValue;
                //        nPh.Estimate.LongLeadMonthRequired = monthRequired;
                //        nPh.Estimate.WellValueDriver = wellvaluedriver;
                //        nPh.Estimate.Status = "";

                //        #endregion

                //        if (isNewPhase)
                //            bp.Phases.Add(nPh);
                //        #endregion
                //    }
                //    else
                //    {
                //        #region new BizPlan

                //        bp = new BizPlanActivity();
                //        bp.BizPlanId = "OPPlan";
                //        bp.Phases = new List<BizPlanActivityPhase>();
                //        bp.LineOfBusiness = lob;
                //        bp.Region = region;
                //        bp.Country = country;
                //        bp.Currency = currency;
                //        bp.OperatingUnit = operationUnit;
                //        bp.PerformanceUnit = performanceUnit;
                //        bp.AssetName = asset;
                //        bp.ProjectName = project;
                //        bp.WellName = wellName;

                //        bp.isInPlan = inPlan;
                //        //bp.FirmOrOption = planningClass;
                //        bp.ReferenceFactorModel = rfm;
                //        bp.RigName = rigName;
                //        bp.RigType = rigType;
                //        bp.UARigSequenceId = rigSeqId;

                //        bp.WorkingInterest = workingInterest;
                //        bp.ShellShare = workingInterest * 100;

                //        var nPh = new BizPlanActivityPhase();
                //        nPh.ActivityCategory = activityCategory;
                //        nPh.ActivityType = activityType;
                //        nPh.ActivityDesc = scopeDesc;
                //        nPh.FundingType = fundingType;
                //        nPh.PlanSchedule.Start = PlanStart;
                //        nPh.PlanSchedule.Finish = PlanFinish;
                //        nPh.PhSchedule.Start = LSStart;
                //        nPh.PhSchedule.Finish = LSFinish;
                //        nPh.Plan.Days = new DateRange(PlanStart, PlanFinish).Days + 1;
                //        nPh.OP.Days = new DateRange(LSStart, LSFinish).Days + 1;
                //        nPh.PhaseNo = 1;

                //        nPh.Estimate = new BizPlanActivityEstimate();
                //        nPh.Estimate.NewLearningCurveFactor = learningCurveFactor;
                //        nPh.Estimate.SaveToOP = saveToOP;
                //        nPh.Estimate.Country = country;
                //        nPh.Estimate.ShellShareCalc = workingInterest;
                //        nPh.Estimate.RigName = rigName;
                //        nPh.Estimate.EventStartDate = LSStart;
                //        nPh.Estimate.EventEndDate = LSFinish;
                //        nPh.Estimate.MaturityLevel = maturityRisk;
                //        nPh.Estimate.Services = services;
                //        nPh.Estimate.Materials = material;
                //        nPh.Estimate.LongLeadCalc = longleadperc;
                //        nPh.Estimate.LongLeadMonthRequired = longleadmonth;

                //        nPh.Estimate.NewTroubleFree.Days = troublefreetime;

                //        nPh.Estimate.NewNPTTime.PercentDays = npttimeperc;
                //        nPh.Estimate.NewNPTTime.PercentCost = nptcostperc;
                //        nPh.Estimate.NewTECOPTime.PercentDays = tecoptimeperc;
                //        nPh.Estimate.NewTECOPTime.PercentCost = tecopcostperc;

                //        nPh.Estimate.WellValueDriver = wellvaluedriver;
                //        nPh.Estimate.DryHoleDays = dryholedays;
                //        nPh.Estimate.TQ.Threshold = tqvalue;
                //        nPh.Estimate.BIC.Threshold = bicvalue;
                //        nPh.Estimate.PerformanceScore = performancescore;
                //        nPh.Estimate.WaterDepth = waterdepth;
                //        nPh.Estimate.WaterDepth = totalwaterdepth;
                //        nPh.Estimate.NumberOfCasings = nofocasing;
                //        nPh.Estimate.MechanicalRiskIndex = mechanicalriskindex;
                //        nPh.Estimate.BrineDensity = brinedensity;
                //        nPh.Estimate.CompletionInfo = complInfo;
                //        nPh.Estimate.CompletionType = complType;
                //        nPh.Estimate.NumberOfCompletionZones = noOfZone;

                //        #region Phase Info
                //        // do phase info after process done
                //        nPh.PhaseInfo = new WellActivityPhaseInfo();
                //        #endregion

                //        #region Detail

                //        nPh.Estimate.UsingTAApproved = false;
                //        nPh.Estimate.IsUsingLCFfromRFM = false;
                //        // take from wellPlan if update 
                //        nPh.Estimate.CurrentTroubleFree = new WellDrillData();
                //        nPh.Estimate.NewTroubleFree = new WellDrillData() { Days = troublefreetime, Cost = 0 };


                //        //var getMaturityRisk = MaturityRiskMatrix.Get<MaturityRiskMatrix>(Query.EQ("Title", nPh.Estimate.MaturityLevel)) ?? new MaturityRiskMatrix();
                //        //var NPTTime = nPh.Estimate.NewTroubleFree.Days *
                //        //    Tools.Div(Tools.Div(getMaturityRisk.NPTTime, 100), 1 - Tools.Div(getMaturityRisk.NPTTime, 100));
                //        //var NPTCost = nPh.Estimate.NewTroubleFree.Cost *
                //        //    Tools.Div(Tools.Div(getMaturityRisk.NPTCost, 100), 1 - Tools.Div(getMaturityRisk.NPTCost, 100));

                //        //nPh.Estimate.NewNPTTime = new WellDrillPercentData()
                //        //{
                //        //    Days = NPTTime,
                //        //    Cost = NPTCost,
                //        //    PercentCost = getMaturityRisk.NPTCost,
                //        //    PercentDays = getMaturityRisk.NPTTime
                //        //};

                //        //// current NPT
                //        //var getLatest = WellActivityUpdate.GetById(
                //        //    bp.WellName, bp.UARigSequenceId, nPh.PhaseNo, null, true);
                //        //if (getLatest != null)
                //        //    nPh.Estimate.CurrentNPTTime = new WellDrillPercentData() { Days = getLatest.NPT.Days, Cost = getLatest.NPT.Cost };
                //        //else
                //        //    nPh.Estimate.CurrentNPTTime = new WellDrillPercentData() { Days = 0, Cost = 0 };


                //        //var TECOPTime = nPh.Estimate.NewTroubleFree.Days *
                //        //    Tools.Div(Tools.Div(getMaturityRisk.TECOPTime, 100), 1 - Tools.Div(getMaturityRisk.TECOPTime, 100));
                //        //var TECOPCost = nPh.Estimate.NewTroubleFree.Cost *
                //        //    Tools.Div(Tools.Div(getMaturityRisk.TECOPCost, 100), 1 - Tools.Div(getMaturityRisk.TECOPCost, 100));

                //        //nPh.Estimate.NewTECOPTime = new WellDrillPercentData()
                //        //{
                //        //    PercentDays = getMaturityRisk.TECOPTime,
                //        //    PercentCost = getMaturityRisk.TECOPCost,
                //        //    Days = TECOPTime,
                //        //    Cost = TECOPCost
                //        //};

                //        var learningCurveCalc = nPh.Estimate.NewTroubleFree.Days;
                //        if (learningCurveFactor != 0)
                //        {
                //            var lcfCalc = nPh.Estimate.NewTroubleFree.Days *
                //                                (learningCurveFactor < 1
                //                                    ? learningCurveFactor
                //                                    : Tools.Div(learningCurveFactor, 100));
                //            learningCurveCalc = learningCurveCalc - lcfCalc;
                //        }

                //        nPh.Estimate.EventEndDate =
                //            nPh.Estimate.EventStartDate.AddDays(
                //            nPh.Estimate.NewTroubleFree.Days + nPh.Estimate.NewNPTTime.Days + nPh.Estimate.NewTECOPTime.Days + learningCurveCalc);

                //        nPh.Estimate.NewTroubleFreeUSD = nPh.Estimate.NewTroubleFree.Cost;
                //        nPh.Estimate.NPTCostUSD = nPh.Estimate.NewNPTTime.Cost;
                //        nPh.Estimate.TECOPCostUSD = nPh.Estimate.NewTECOPTime.Cost;
                //        nPh.Estimate.MeanUSD = nPh.Estimate.NewMean.Cost;

                //        nPh.Estimate.EstimatePeriod = new DateRange()
                //        {
                //            Start = nPh.Estimate.EventStartDate,
                //            Finish = nPh.Estimate.EventEndDate
                //        };


                //        //material long lead
                //        var DateEscStartMaterial = nPh.Estimate.EventStartDate;
                //        var tangibleValue = 0.0;
                //        var monthRequired = 0.0;
                //        var actType = "";
                //        var getActCategory = ActivityMaster.Get<ActivityMaster>(Query.EQ("_id", nPh.ActivityType));
                //        if (getActCategory != null)
                //            actType = getActCategory.ActivityCategory;

                //        if (actType != null && actType != "")
                //        {
                //            var year = nPh.Estimate.EventStartDate.Year;
                //            var getLongLead = LongLead.Get<LongLead>(Query.EQ("Title", actType));
                //            if (getLongLead != null && getLongLead.Details != null && getLongLead.Details.Count > 0)
                //            {
                //                var getTangible = getLongLead.Details.Where(y => y.Year.Equals(year)).FirstOrDefault();
                //                if (getTangible != null)
                //                {
                //                    tangibleValue = getTangible.TangibleValue;
                //                    monthRequired = getTangible.MonthRequiredValue;
                //                    var getMonthLongLead = System.Convert.ToInt32(-1 * Math.Round(monthRequired));
                //                    DateEscStartMaterial = nPh.Estimate.EventStartDate.AddMonths(getMonthLongLead);
                //                }
                //            }
                //        }
                //        else
                //        {
                //            tangibleValue = 0;
                //            monthRequired = 0;
                //            var getMonthLongLead = System.Convert.ToInt32(-1 * Math.Round(monthRequired));
                //            DateEscStartMaterial = nPh.Estimate.EventStartDate.AddMonths(getMonthLongLead);
                //        }

                //        if (DateEscStartMaterial < DateTime.Now)
                //            DateEscStartMaterial = DateTime.Now;

                //        nPh.Estimate.StartEscDateMaterial = DateEscStartMaterial;
                //        nPh.Estimate.PercOfMaterialsLongLead = tangibleValue;
                //        nPh.Estimate.LongLeadMonthRequired = monthRequired;
                //        nPh.Estimate.WellValueDriver = wellvaluedriver;
                //        nPh.Estimate.Status = "";

                //        #endregion

                //        bp.Phases.Add(nPh);
                //        #endregion
                //    }

                //}
                #endregion


            }

            // refresh Well Name
            var wellNames = datas.Select(x => BsonHelper.GetString(x, "Well_Name").Trim()).Distinct().ToList();
            foreach (var w in wellNames)
            {
                var wi = WellInfo.Get<WellInfo>(Query.EQ("_id", w));
                if (wi == null)
                {
                    wi = new WellInfo();
                    wi._id = w.ToString();
                    wi.Save();
                }
            }
            errorMessages = errormsg;
        }

        public static void SyncFromUploadLS(BizPlanActivity bpa, WellActivity wa, WellActivityPhase wphase)
        {
            if (bpa == null)
            {
                //new Bizplan Activity
                var newBP = BsonSerializer.Deserialize<BizPlanActivity>(wa.ToBsonDocument().Set("BizPlanId", "OPPlan"));
                var newBPPhases = new List<BizPlanActivityPhase>();
                if (wa.Phases.Any())
                {
                    foreach (var ph in wa.Phases)
                    {
                        if (ph.ActivityType.Equals(wphase.ActivityType))
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

                                x.Country = "United States";
                                x.ReferenceFactorModel = "default";
                                x.Currency = "USD";

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
                                p.Estimate.Country = "United States";
                                p.Estimate.Currency = "USD";
                                p.Estimate.SaveToOP = "OP15";
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
                                p.Estimate.Status = "";
                                p.Estimate.SelectedCurrency = "USD";

                                #endregion


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
                //add or update phase di bizplan
                if (bpa.Phases.Where(x => x.ActivityType.Contains(wphase.ActivityType)).Any())
                {
                    //update phase
                    var matchPhase = bpa.Phases.Where(x => x.ActivityType.Contains(wphase.ActivityType)).FirstOrDefault();
                    matchPhase.PhSchedule = wphase.PhSchedule;
                    matchPhase.OP.Days = wphase.OP.Days;
                    bpa.Save();
                }
                else
                {
                    //add phase
                    var newBPPhase = new BizPlanActivityPhase();

                    var newPhases = BsonSerializer.Deserialize<BizPlanActivityPhase>(wphase.ToBsonDocument());

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

                        x.Country = "United States";
                        x.ReferenceFactorModel = "default";
                        x.Currency = "USD";

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
                        p.Estimate.Country = "United States";
                        p.Estimate.Currency = "USD";
                        p.Estimate.SaveToOP = "OP15";
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
                        p.Estimate.Status = "";
                        p.Estimate.SelectedCurrency = "USD";

                        #endregion


                        #endregion
                        bpa.Phases.Add(newPhases);

                        bpa.Save();
                        BizPlanAllocation.SaveBizPlanAllocation(bpa);

                        var lastid = BizPlanActivity.Get<BizPlanActivity>(null, sort: SortBy.Descending("_id"));
                        int id = Convert.ToInt32(lastid._id);
                        BsonDocument dd = new BsonDocument();
                        dd.Set("_id", "WEISBizPlanActivities");
                        dd.Set("Title", "WEISBizPlanActivities");
                        dd.Set("NextNo", id + 1);
                        dd.Set("Format", "");
                        DataHelper.Save("SequenceNos", dd);

                    }

                }
            }
        }

        public static void RefreshBizplanWithLatestUploadedLS(string colName = "OP20160516V2")
        {
            DataHelper.Delete("SyncStatus_" + colName);
            var colls = DataHelper.Populate(colName);
            foreach (var t in colls)
            {
                try
                {
                    var rigName = t.GetString("Rig_Name");
                    var wellName = t.GetString("Well_Name");
                    var activityType = t.GetString("Activity_Type");

                    var sd = DateTime.ParseExact(BsonHelper.GetString(t, "Start_Date"), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                    var fd = DateTime.ParseExact(BsonHelper.GetString(t, "End_Date"), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

                    var q = Query.And(
                        Query.EQ("WellName", wellName),
                        Query.EQ("Phases.ActivityType", activityType),
                        Query.EQ("Phases.Estimate.RigName", rigName)
                        );

                    var bp = BizPlanActivity.Get<BizPlanActivity>(q);
                    if (bp != null)
                    {
                        #region update LS bizplan

                        var y = bp.Phases.Where(x => x.ActivityType.Equals(activityType));
                        if (y != null)
                        {
                            y.FirstOrDefault().PhSchedule.Start = sd;
                            y.FirstOrDefault().PhSchedule.Finish = fd;
                        }
                        DataHelper.Save(bp.TableName, bp.ToBsonDocument());
                        t.Set("StatusSyncBizplan", "Update LS");
                        DataHelper.Save("SyncStatus_" + colName, t.ToBsonDocument());
                        #endregion
                    }
                    else
                    {

                        q = Query.And(
                        Query.EQ("WellName", wellName),
                        Query.EQ("Phases.ActivityType", activityType),
                        Query.EQ("RigName", rigName)
                        );
                        var y = WellActivity.Get<WellActivity>(q);


                        q = Query.And(
                        Query.EQ("WellName", wellName),
                        Query.EQ("UARigSequenceId", y.UARigSequenceId),
                        Query.EQ("Phases.Estimate.RigName", rigName)
                        );

                        var bpsameRigName = BizPlanActivity.Get<BizPlanActivity>(q);
                        if (bpsameRigName != null)
                        {
                            // add phases
                            var phase = y.Phases.Where(x => x.ActivityType.Equals(activityType)).FirstOrDefault();
                            BizPlan.SyncFromUploadLS(bpsameRigName, y, phase);

                            t.Set("StatusSyncBizplan", "New Biz Plan - add phases");
                            DataHelper.Save("SyncStatus_" + colName, t.ToBsonDocument());

                        }
                        else
                        {
                            var phase = y.Phases.Where(x => x.ActivityType.Equals(activityType)).FirstOrDefault();
                            BizPlan.SyncFromUploadLS(null, y, phase);

                            t.Set("StatusSyncBizplan", "New Biz Plan ");
                            DataHelper.Save("SyncStatus_" + colName, t.ToBsonDocument());
                        }

                    }
                }
                catch (Exception ex)
                {

                }
            }
        }



        public override void PostGet()
        {
            // fill data from bizzpalnACtivities
            base.PostGet();
        }
    }

    [BsonIgnoreExtraElements]
    public class BizPlanActivityPhase
    {
        public BizPlanActivityPhase()
        {
            Estimate = new BizPlanActivityEstimate();
            TeamLead = new Person();
            WellEngineer = new Person();
            LeadEngineer = new Person();
            PhaseInfo = new WellActivityPhaseInfo();

            Allocation = new BizPlanAllocation();
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


        public BizPlanAllocation Allocation { get; set; }

        public WellActivityPhaseInfo PhaseInfo { get; set; }

        public BizPlanActivityEstimate Estimate { get; set; }

        //public List<BizPlanAllocation> Allocations { get; set; }

        public static double GetCurrentOPLELSValueOfYear(string WellName, string RigName, string ActivityType, int year, string which = "CurrentOP")
        {
            var comparator = 0.0;
            var wellPlan = WellActivity.Get<WellActivity>(Query.And(
                Query.EQ("WellName", WellName),
                Query.EQ("RigName", RigName),
                Query.EQ("Phases.ActivityType", ActivityType)
            ));

            if (wellPlan != null)
            {
                var wellPlanPhase = wellPlan.Phases.FirstOrDefault(e => ActivityType.Equals(e.ActivityType));
                var phasePeriod = wellPlanPhase.PhSchedule ?? new DateRange(new DateTime(year, 1, 1), new DateTime(year, 1, 1));

                var periodEachYears = DateRangeToMonth.NumDaysPerYear(phasePeriod);
                var totalDayEachYear = (new DateTime(year, 12, 31) - new DateTime(year, 1, 1)).Days;
                var ratioForThisYear = 0.0;

                if (periodEachYears.ContainsKey(year))
                    ratioForThisYear = (periodEachYears[year] / totalDayEachYear);

                if (which.Equals("CurrentOP"))
                    comparator = wellPlanPhase.CalcOP.Cost * ratioForThisYear;
                else if (which.Equals("CurrentLE"))
                    comparator = wellPlanPhase.LE.Cost * ratioForThisYear;
                else if (which.Equals("CurrentLS"))
                    comparator = wellPlanPhase.OP.Cost * ratioForThisYear;
            }

            return comparator;
        }

        public bool isLatestLS { get; set; }
        public DateTime LatestLSDate { get; set; }

        public int PhaseNo { get; set; }
        public string ActivityType { get; set; }
        public string ActivityDesc { get; set; }
        public string ActivityCategory { get; set; }
        public string RiskFlag { get; set; }
        public string ActivityDescEst { get; set; }
        private DateRange _LESchedule;
        private DateRange _LWESchedule;
        private DateRange _PlanSchedule;
        private DateRange _PhSchedule;
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
        private WellDrillData _op, _afe, _le, _lwe, _tq, _actual, _m1, _m2, _m3, _aggredtarget;
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

        [BsonIgnore]
        public double Ratio { set; get; }

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

        // [BsonIgnore]
        public WellDrillData CalcOP { set; get; }
        [BsonIgnore]
        public Dictionary<int, double> AnnualProportions { get; set; }
        public DateRange AFESchedule { get; set; }
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

        public WellDrillData CalculatedEstimate()
        {
            return new WellDrillData()
            {
                Days = this.CalculatedLE().Days - this.CalculatedAFE().Days,
                Cost = this.CalculatedLE().Cost - this.CalculatedAFE().Cost
            };
        }

        [BsonIgnore]
        public bool PushToWellPlan { get; set; }

    }

    public class BizPlanActivityPerson : BizPlanActivityPhase
    {
        public string WellName { get; set; }
        public string UARigSequenceId { get; set; }
    }
}
