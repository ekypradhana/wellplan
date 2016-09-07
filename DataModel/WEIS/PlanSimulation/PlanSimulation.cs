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
    public class PlanSimulation : ECISModel
    {
        public PlanSimulation()
        {
            Phases = new List<WellPlanSimulationPhase>();
        }
        public override string TableName
        {
            get { return "WEISPlanSimulation"; }
        }
        public string SimulationId { get; set; }
        public bool isNew { get; set; }
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

        public List<WellPlanSimulationPhase> Phases { get; set; }
        public WellPlanSimulationPhase GetPhase(string activityType)
        {
            if (Phases == null) Phases = new List<WellPlanSimulationPhase>();
            return Phases.FirstOrDefault(d => d.ActivityType.Equals(activityType));
        }
        public WellPlanSimulationPhase GetPhase(int phaseNo)
        {
            if (Phases == null) Phases = new List<WellPlanSimulationPhase>();
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
                ph.Plan.Days = ph.PlanSchedule.Days;
                ph.OP.Days = ph.PhSchedule.Days;

                ph.VirtualPhase = this.VirtualPhase;
                ph.ShiftFutureEventDate = this.ShiftFutureEventDate;
            }

            var noRisks = Phases.Where(d => (d.ActivityType ?? "").ToLower().Contains("risk") == false);
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

            // save Plan Simulation Buckets
            if (this.Phases != null)
            {
                List<BsonDocument> docs = new List<BsonDocument>();
                docs.Add(this.ToBsonDocument());
                var uwind = BsonHelper.Unwind(docs, "Phases", "", new string[] { "SimulationId","WellName", "RigName","UARigSequenceId" });
                foreach (var t in uwind)
                {
                    PlanSimulationBucket b = new PlanSimulationBucket();
                    b.SimulationId = this._id.ToString();
                    b.LevelOfEstimate = BsonHelper.GetString(t, "LevelOfEstimate");// this.Lev;
                    b.ExType = BsonHelper.GetString(t, "ExType");
                    b.GenerateBucket(t);
                    b.Save();
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

        public static void LoadLastSequence(string tableName = "OP201502V2")
        {
            List<WellActivity> was = WellActivity.Populate<WellActivity>();

            //---- reset WA Phase LS Date
            foreach (var wa in was)
            {
                foreach (var ph in wa.Phases)
                {
                    // if doesnt have WR 
                    if (!WellActivity.isHaveWeeklyReport(wa.WellName, wa.UARigSequenceId, ph.ActivityType))
                        ph.PhSchedule = new DateRange();
                }
            }

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
                WellActivity wa = was.FirstOrDefault(d => d.WellName.Equals(wellname)
                    && d.RigName.Equals(rigName)
                    && d.Phases.Select(e => e.ActivityType).Contains(eventCodeId));
                if (wa == null)
                {
                    wa = was.FirstOrDefault(d => d.WellName.Equals(wellname)
                        && d.RigName.Equals(rigName)
                        && d.Phases.Select(e => e.ActivityType).Contains(eventCodeId) == false);
                    if (wa != null)
                    {
                        createWa = false;
                    }
                    //else
                    //{
                    //    var qwell = Query.And(
                    //    Query.EQ("WellName", wellname),
                    //    Query.EQ("Phases.ActivityType", eventCodeId));
                    //    wa = WellActivity.Get<WellActivity>(qwell);
                    //    if (wa != null)
                    //    {
                    //        createWa = false;
                    //        was.Add(wa);
                    //    }
                    //}
                }
                else
                {
                    var prevMst = msts
                        .OrderBy(d => d.GetDateTime("Start_Date"))
                        .FirstOrDefault(d => d.GetString("Well_Name").Equals(wellname) && d.GetString("Activity_Type").Equals(eventCodeId));
                    if (prevMst.Get("_id").Equals(mst.Get("_id")))
                        createWa = false;
                }
                if (createWa)
                {
                    wa = new WellActivity();
                    well = WellInfo.Get<WellInfo>(wellname);
                    if (well == null)
                    {
                        well = new WellInfo();
                        well._id = wellname;
                        well.Title = wellname;
                        wa.NonOP = true;
                        well.Save();
                    }
                    wa.WellName = wellname;
                    wa.Phases = new List<WellActivityPhase>();
                    was.Add(wa);
                }
                wa.RigName = rigName;
                wa.RigType = rigType;
                WellActivityPhase ph = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(eventCodeId));
                if (ph == null)
                {
                    ph = new WellActivityPhase();
                    ph.PhaseNo = wa.Phases.Count == 0 ? 1 : wa.Phases.Max(d => d.PhaseNo) + 1;
                    ph.ActivityType = eventCodeId;
                    ph.PlanSchedule = new DateRange();
                    //ph.PlanSchedule = new DateRange { Start = dateStart, Finish = dateEnd };
                    wa.Phases.Add(ph);
                }
                ph.ActivityDesc = eventDescription;
                ph.PhSchedule = new DateRange { Start = dateStart, Finish = dateEnd };
                ph.OP.Days = ph.PhSchedule.Days + 1;
                if (ph.PhSchedule.Start.Year > 2000)
                {
                    if (ph.PreviousWeek.Year < 2000) ph.LWE = ph.OP;
                    if (ph.LastWeek.Year < 2000) ph.LE = ph.OP;
                }
                else
                {
                    ph.LWE = new WellDrillData();
                    ph.LE = new WellDrillData();
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

            WellActivity.SyncSequenceId();
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

        public static void SyncSequenceId(IMongoQuery query = null)
        {
            var waActuals = WellActivityActual.Populate<WellActivityActual>(query == null ? Query.Null : query)
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

            var waSequences = WellActivityUpdate.Populate<WellActivityUpdate>()
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
                        Query.EQ("ActivityType", x.ActivityType)
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
                        waUpdate = was.FirstOrDefault(d => d.WellName.Equals(ph.WellName) && d.UARigSequenceId.Equals(ph.UARigSequenceId));
                        if (waUpdate != null) updates.Add(waUpdate);
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
                        waUpdate = was.FirstOrDefault(d => d.WellName.Equals(ph.WellName) && d.UARigSequenceId.Equals(ph.UARigSequenceId));
                        if (waUpdate != null) updates.Add(waUpdate);
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
                    well.Phases.Where(x => x.ActivityType.Equals(BsonHelper.GetString(datapergroup.FirstOrDefault(), "ActivityType"))).FirstOrDefault().CalcOPSchedule =
                        well.Phases.Where(x => x.ActivityType.Equals(BsonHelper.GetString(datapergroup.FirstOrDefault(), "ActivityType"))).FirstOrDefault().PhSchedule;
                    well.Phases.Where(x => x.ActivityType.Equals(BsonHelper.GetString(datapergroup.FirstOrDefault(), "ActivityType"))).FirstOrDefault().CalcOP =
                        well.Phases.Where(x => x.ActivityType.Equals(BsonHelper.GetString(datapergroup.FirstOrDefault(), "ActivityType"))).FirstOrDefault().OP;
                    well.Save();
                }

            }

        }


    }
}




