using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;

namespace ECIS.Client.WEIS
{
    public class WellActivityActual : ECISModel
    {
        public override BsonDocument PreSave(BsonDocument doc, string[] references=null)
        {
            _id = String.Format("W{0}S{1}U{2:yyyyMMdd}", WellName.Replace(" ", ""), SequenceId, UpdateVersion);
            return this.ToBsonDocument();
        }

        public WellActivityActual GetBefore()
        {
            //-- get latest update
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("SequenceId", SequenceId));
            var latest = WellActivityActual.Get<WellActivityActual>(Query.And(qs), SortBy.Descending("UpdateVersion"));
            return latest;
        }

        public WellActivityActual GetAfter()
        {
            //-- get latest update
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("SequenceId", SequenceId));
            qs.Add(Query.GT("UpdateVersion", UpdateVersion));
            var latest = WellActivityActual.Get<WellActivityActual>(Query.And(qs), SortBy.Ascending("UpdateVersion"));
            return latest;
        }

        public override BsonDocument PostSave(BsonDocument doc, string[] references=null)
        {
            if (references == null) references = new string[] { };
            bool dontSyncWA = references.Count() > 0 && references[0] == "UnsyncWA";
            bool dontSyncWR = references.Count() > 1 && references[1] == "UnsyncWR";
            var next = GetAfter();
            var current = GetBefore();
            if (next==null && (!dontSyncWA))
            {
                // update well  Activity 
                var qs = new List<IMongoQuery>();
                qs.Add(Query.EQ("WellName", WellName));
                qs.Add(Query.EQ("UARigSequenceId", SequenceId));

                var activity = DataHelper.Populate<WellActivity>("WEISWellActivities", Query.And(qs));  //WellActivity.Get<WellActivity>(Query.And(qs));
                if (activity != null && activity.Count > 0)
                {
                    var a = activity.FirstOrDefault();
                    foreach (var ph in a.Phases)
                    {
                        var afe = AFE.FirstOrDefault(d => d.PhaseNo.Equals(ph.PhaseNo));
                        var actual = Actual.FirstOrDefault(d => d.PhaseNo.Equals(ph.PhaseNo));
                        if (afe != null)
                        {
                            ph.AFE = afe.Data;
                        }
                        if (actual != null)
                        {
                            ph.Actual = actual.Data;
                        }
                    }
                    a.Save();
                }
            }

            if (next == null && (!dontSyncWR))
            {
                foreach (var afe in AFE)
                {
                    var qUpdates = new List<IMongoQuery>();
                    qUpdates.Add(Query.EQ("WellName", WellName));
                    qUpdates.Add(Query.EQ("SequenceId", SequenceId));
                    qUpdates.Add(Query.EQ("Phase.PhaseNo", afe.PhaseNo));
                    qUpdates.Add(Query.GTE("UpdateVersion", Tools.GetNearestDay(UpdateVersion, DayOfWeek.Monday)));
                    var waus = WellActivityUpdate.Populate<WellActivityUpdate>(Query.And(qUpdates));
                    foreach (var wau in waus)
                    {
                        var actual = Actual.FirstOrDefault(d => d.PhaseNo.Equals(afe.PhaseNo));
                        wau.Calc();
                        wau.Save();
                    }
                }
            }

            return this.ToBsonDocument();
        }
        public override string TableName
        {
            get { return "WEISActivityActual"; }
        }
        public string WellName { get; set; }
        public string SequenceId { get; set; }

        private DateTime _updateVersion;
        public DateTime UpdateVersion
        {
            get { return _updateVersion; }
            set { _updateVersion = Tools.ToDateTime(Tools.ToUTC(value).ToString(),true); }
        }
        private List<WellActivityActualItem> _ActualData;
        public List<WellActivityActualItem> Actual
        {
            get
            {
                if (_ActualData == null) _ActualData = new List<WellActivityActualItem>();
                return _ActualData;
            }
            set { _ActualData = value; }
        }
        private List<WellActivityActualItem> _AFE;
        public List<WellActivityActualItem> AFE
        {
            get
            {
                if (_AFE == null) _AFE = new List<WellActivityActualItem>();
                return _AFE;
            }
            set { _AFE = value; }
        }

        public static WellActivityActual GetById(
                string wellName, string sequenceId, DateTime? updateVersion = null,
                bool getLatest = false,
                bool newIfDefault = false)
        {
            var dt = updateVersion==null ? DateTime.MaxValue : Tools.ToDateTime(updateVersion.ToString(), true);
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", wellName));
            qs.Add(Query.EQ("SequenceId", sequenceId));
            if (getLatest) qs.Add(Query.LTE("UpdateVersion", dt));
            else qs.Add(Query.EQ("UpdateVersion", dt));
            var q = qs.Count == 0 ? Query.Null : Query.And(qs);
            WellActivityActual ret = WellActivityActual.Get<WellActivityActual>(q, SortBy.Descending(new string[] { "UpdateVersion" }));
            if (ret == null && newIfDefault)
            {
                ret = new WellActivityActual();
                ret.WellName = wellName;
                ret.SequenceId = sequenceId;
                ret.UpdateVersion = dt;
            }
            return ret;
        }
        public WellActivityActualItem GetActual(int phaseNo)
        {
            if (Actual.FirstOrDefault(d => d.PhaseNo.Equals(phaseNo)) == null) Actual.Add(new WellActivityActualItem(phaseNo));
            var dt = Actual.FirstOrDefault(d => d.PhaseNo.Equals(phaseNo));
            return dt;
        }
        public WellActivityActualItem GetAFE(int phaseNo)
        {
            if (AFE.FirstOrDefault(d => d.PhaseNo.Equals(phaseNo)) == null) AFE.Add(new WellActivityActualItem(phaseNo));
            var dt = AFE.FirstOrDefault(d => d.PhaseNo.Equals(phaseNo));
            return dt;
        }
        public void UpdateDay(int phaseNo, double value, bool saveToDb = false)
        {
            WellActivityActualItem v = GetActual(phaseNo);
            v.Data.Days = value;
            if (saveToDb) this.Save();
        }
        public void UpdateCost(int phaseNo, double value, bool saveToDb = false)
        {
            WellActivityActualItem v = GetActual(phaseNo);
            v.Data.Cost = value;
            if (saveToDb) this.Save();
        }
        public void Update(int phaseNo, WellDrillData value, bool saveToDb = false)
        {
            WellActivityActualItem v = GetActual(phaseNo);
            v.Data = value;
            if (saveToDb) this.Save();
        }

        public static WellActivityActual Convert(BsonDocument ori)
        {
            string oWellName = BsonHelper.GetString(ori, "WELLNAME");
            string oEventCode = BsonHelper.GetString(ori, "EVENTCODE");
            string oWellId = BsonHelper.GetString(ori, "WELLID");

            var wellMatch = DataHelper.Populate("WEISWellJunction", Query.EQ("SHELL_WELL_ID", oWellId));
            string wellName = wellMatch.FirstOrDefault() != null ? BsonHelper.GetString(wellMatch.FirstOrDefault(), "OUR_WELL_ID") : "";
            
            if (oEventCode.Trim().Equals("DRO") || oEventCode.Trim().Equals("DRL")) oEventCode = "DRL,DRO";

            var activityTypeMatch = DataHelper.Populate("WEISActivities", Query.EQ("EDMActivityId", oEventCode));
            Console.WriteLine("activityTypeMatch : " + activityTypeMatch != null ? activityTypeMatch.ToString() : "null");
            List<string> activityType = new List<string>();
            if (activityTypeMatch != null && activityTypeMatch.Count > 0)
                activityType = activityTypeMatch.Select(x => BsonHelper.GetString(x, "_id")).ToList();

            BsonArray qactivity = new BsonArray();
            foreach (var activity in activityType) qactivity.Add(activity);

            Console.WriteLine("Oracle #" + oWellName + " | " + oEventCode);

            WellActivityActualItem actual = new WellActivityActualItem(0);
            List<WellActivityActualItem> acts = new List<WellActivityActualItem>();

            WellActivityActualItem afe = new WellActivityActualItem(0);
            List<WellActivityActualItem> afes = new List<WellActivityActualItem>();

            if (wellName != string.Empty && activityType.Count() > 0)
            {
                var qs = new List<IMongoQuery>();
                qs.Add(Query.EQ("WellName", wellName));
                qs.Add(Query.In("Phases.ActivityType", qactivity));

                var weisWellActivities = DataHelper.Populate<WellActivity>("WEISWellActivities", Query.And(qs));
                Console.WriteLine(" " + oWellName + " | " + oEventCode);
                if (weisWellActivities.Count > 0)
                {
                    WellActivityPhase phase = weisWellActivities.FirstOrDefault().Phases.Where(x => activityType.Contains(x.ActivityType)).FirstOrDefault();
                    Console.WriteLine("MongoDB #" + weisWellActivities.FirstOrDefault().WellName + " | " + phase.ActivityType);
                    actual.PhaseNo = phase.PhaseNo;
                    actual.Data = new WellDrillData
                    {
                        Cost = BsonHelper.GetDouble(ori, "ACTUALCOST"),
                        Days = BsonHelper.GetDouble(ori, "DAYSONLOCATION")
                    };

                    acts.Add(actual);

                    afe.PhaseNo = phase.PhaseNo;
                    afe.Data = new WellDrillData
                    {
                        Cost = BsonHelper.GetDouble(ori, "AFECOST"),
                        Days = BsonHelper.GetDouble(ori, "AFEDAYS")
                    };

                    afes.Add(afe);

                    return new WellActivityActual()
                    {
                        Actual = acts,
                        AFE = afes,
                        SequenceId = weisWellActivities.FirstOrDefault().UARigSequenceId,
                        WellName = wellName,
                        UpdateVersion = Tools.ToUTC(BsonHelper.GetDateTime(ori, "REPORTDATE")),
                        // LastUpdate
                    };
                }
            }
            return null;
        }

        public static bool UpdateWellActivityUpdate(WellActivityActual wellActual, out string Output)
        {
            
            int PhaseNo = wellActual.Actual.FirstOrDefault().PhaseNo;

            List<IMongoQuery> qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", wellActual.WellName));
            qs.Add(Query.EQ("SequenceId", wellActual.SequenceId));
            qs.Add(Query.EQ("Phase.PhaseNo", PhaseNo));

            try
            {
                var wellUpdate = WellActivityUpdate.Get<WellActivityUpdate>(Query.And(qs), SortBy.Descending("UpdateVersion"));

                if (wellUpdate != null)
                {
                    wellUpdate.AFE.Cost = wellActual.AFE.FirstOrDefault().Data.Cost;
                    wellUpdate.AFE.Days = wellActual.AFE.FirstOrDefault().Data.Days;
                    wellUpdate.Actual.Cost = wellActual.Actual.FirstOrDefault().Data.Cost;
                    wellUpdate.Actual.Days = wellActual.Actual.FirstOrDefault().Data.Days;

                    wellUpdate.Phase.AFE.Cost = wellActual.AFE.FirstOrDefault().Data.Cost;
                    wellUpdate.Phase.AFE.Days = wellActual.AFE.FirstOrDefault().Data.Days;
                    wellUpdate.Phase.Actual.Cost = wellActual.Actual.FirstOrDefault().Data.Cost;
                    wellUpdate.Phase.Actual.Days = wellActual.Actual.FirstOrDefault().Data.Days;

                    wellUpdate.Save();
                    Output = "SUCCESS update : \n" + wellUpdate._id.ToString();
                    return true;
                }
                else
                {
                    Output = "NO WellActivityUpdate for : \n" + wellActual._id.ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Output = "Error : \n" + ex.Message + "\n" + ex.InnerException + "\n" + ex.StackTrace;
                return false;

            }

        }

        public static List<WellActivityActual> GetLastActual(List<WellActivityActual> ActualGroup)
        {
            List<WellActivityActual> actMax = new List<WellActivityActual>();

            if (ActualGroup.Count > 0)
            {
                foreach (var t in ActualGroup.GroupBy(x => x.WellName))
                {
                    foreach (var g in t.GroupBy(x => x.SequenceId))
                    {
                        if (g.OrderByDescending(x => x.UpdateVersion) != null)
                        {
                            actMax.Add(g.OrderByDescending(x => x.UpdateVersion).FirstOrDefault());
                            break;
                        }
                    }
                }
            }

            return actMax;
        }

        public static List<WellActivityActual> GetLastData(List<WellActivityActual> ActualGroup)
        {
            List<WellActivityActual> actMax = new List<WellActivityActual>();
            string args = @"{ $group: 
                            { 
                                _id: { WellName : '$WellName' , SequenceId : '$SequenceId'}, 
                               
                                UpdateVersion: { $last: '$UpdateVersion' },
                                count: { $sum: 1 } 
                            } 
                        }";
            List<BsonDocument> pipelines = new List<BsonDocument>();
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(args));
            List<BsonDocument> aggregates = DataHelper.Aggregate("WEISActivityActual", pipelines);

            foreach (var aggregate in aggregates)
            {
                string WellName = BsonHelper.GetString(aggregate, "_id.WellName");
                string SeqId = BsonHelper.GetString(aggregate, "_id.SequenceId");
                DateTime UpdateVersion = BsonHelper.GetDateTime(aggregate, "UpdateVersion");

                List<IMongoQuery> qs = new List<IMongoQuery>();
                qs.Add(Query.EQ("WellName", WellName));
                qs.Add(Query.EQ("SequenceId", SeqId));
                qs.Add(Query.EQ("UpdateVersion", Tools.ToUTC(UpdateVersion)));

                var y = DataHelper.Populate<WellActivityActual>("WEISActivityActual", Query.And(qs)).FirstOrDefault();
                actMax.Add(y);
            }
            return actMax;
        }
    }

    public class WellActivityActualItem
    {
        public WellActivityActualItem(int phaseNo)
        {
            Data = new WellDrillData();
            PhaseNo = phaseNo;
        }
        public int PhaseNo { get; set; }
        public WellDrillData Data { get; set; }
    }
}
