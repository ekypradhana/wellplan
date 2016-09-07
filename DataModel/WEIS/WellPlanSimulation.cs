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
    public class WellPlanSimulation : ECISModel
    {
        public WellPlanSimulation()
        {
            WellPlans = new List<WellActivity>();
        }
        public override string TableName
        {
            get { return "WEISWellPlanSimulation"; }
        }

        public Object WellActivityId { get; set; }

        public List<WellActivity> WellPlans { get; set; }

        [BsonIgnore]
        public SequenceData SequenceData { get; set; }

        public override BsonDocument PostSave(BsonDocument doc, string[] references = null)
        {
            return doc;
        }
        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            var res = ResetOpsLatestAndFinishDate(this);
            if (_id == null)
            {
                _id = String.Format("U{0}", res.LastUpdate.ToString("yyyyMMddhhmmss"));
                return res.ToBsonDocument();
            }
            else
                return res.ToBsonDocument();
        }

        public static bool CheckRigConflict(string wellPlanSimulationId, out Dictionary<int, Dictionary<BsonDocument, BsonDocument>> PhaseActivityConflict, string DateRangeTitle = "PhSchedule")
        {
            PhaseActivityConflict = new Dictionary<int, Dictionary<BsonDocument, BsonDocument>>(); ;
            int indexKey = 0;
            var sim = WellPlanSimulation.Get<WellPlanSimulation>(Query.EQ("_id", wellPlanSimulationId));
            var wps = sim.WellPlans;

            List<BsonDocument> per = new List<BsonDocument>();
            foreach (var w in wps)
                per.Add(w.ToBsonDocument());
            var uw = BsonHelper.Unwind(per, "Phases", "", new List<string> { "RigType", "RigName", "WellName", "UARigSequenceId", "_id" });
            foreach (var u in uw)
            {
                u.Set("WellActivityId", u.GetInt32("_id"));
                u.Remove("_id");
            }

            // check conflict with Current Rig Activity
            var grpRig = uw.GroupBy(x => BsonHelper.GetString(x, "RigName"));

            List<bool> boolindic = new List<bool>();
            foreach (var g in grpRig)
            {
                var res = g.OrderBy(x => BsonHelper.GetDateTime(x, DateRangeTitle + ".Start"));

                int index = res.Count() - 1;
                DateTime FinisPrev = new DateTime();

                int j = 0;
                for (int i = 0; i <= index; i++)
                {
                    if (j == 0)
                    {
                        FinisPrev = res.ToList()[j].GetDateTime(DateRangeTitle + ".Finish");
                        j++;
                    }
                    else if (j <= index)
                    {
                        DateTime st = new DateTime();
                        DateTime fn = new DateTime();
                        st = res.ToList()[j].GetDateTime(DateRangeTitle + ".Start");
                        fn = res.ToList()[j].GetDateTime(DateRangeTitle + ".Finish");
                        DateRange dr = new DateRange();
                        dr.Start = st;
                        dr.Finish = fn;

                        bool isCrash = DateRangeToMonth.isDateInsideofRange(FinisPrev, dr);

                        if (isCrash)
                        {
                            Dictionary<BsonDocument, BsonDocument> doc = new Dictionary<BsonDocument, BsonDocument>();
                            doc.Add(res.ToList()[j], res.ToList()[j - 1]);
                            PhaseActivityConflict.Add(indexKey + 1, doc);
                            indexKey++;
                            boolindic.Add(true);
                        }
                        FinisPrev = fn;
                        j++;
                    }
                }
            }

            if (boolindic.Contains(true))
                return true;
            else
                return false;
        }


        public static WellPlanSimulation ResetOpsLatestAndFinishDate(WellPlanSimulation wps)
        {
            foreach (var wp in wps.WellPlans)
            {
                DateRange newOpsSchedule = new DateRange();

                var dr = wp.Phases.Select(x => x.PhSchedule).ToList();
                newOpsSchedule.Start = dr.Min(x => x.Start);
                newOpsSchedule.Finish = dr.Max(x => x.Finish);
                wp.OpsSchedule = newOpsSchedule;
            }

            return wps;
        }
       

    }

    
}
