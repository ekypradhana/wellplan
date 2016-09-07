using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization.Attributes;

namespace ECIS.Client.WEIS
{
    public class WellPIPElementHistory : PIPElementLoE
    {
        public DateTime LogDate { get; set; }
        public string LogAction { get; set; }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            LogDate = DateTime.Now;
            System.Threading.Thread.Sleep(1);
            _id = String.Format("P{0}-E{1}-L{2}-U{3:yyyyMMddhhmmssfff}", PIPId.Replace(" ", ""), ElementId.ToString(), LevelOfEstimate, LogDate);
            return this.ToBsonDocument();
        }

        public override BsonDocument PostSave(BsonDocument doc, string[] references = null)
        {
            return this.ToBsonDocument();
        }

        public static WellPIPElementHistory CopyFromElementLoE(PIPElementLoE e)
        {
            WellPIPElementHistory h = new WellPIPElementHistory();
            h.PIPId = e.PIPId;
            h.Realized = e.Realized;

            h.UpdateBy = e.UpdateBy;

            h.DaysPlanImprovement = e.DaysPlanImprovement;
            h.DaysPlanRisk = e.DaysPlanRisk;
            h.DaysActualImprovement = e.DaysActualImprovement;
            h.DaysActualRisk = e.DaysActualRisk;
            h.DaysLastWeekImprovement = e.DaysLastWeekImprovement;
            h.DaysLastWeekRisk = e.DaysLastWeekRisk;
            h.DaysCurrentWeekImprovement = e.DaysCurrentWeekImprovement;
            h.DaysCurrentWeekRisk = e.DaysCurrentWeekRisk;
            h.CostPlanImprovement = e.CostPlanImprovement;
            h.CostPlanRisk = e.CostPlanRisk;
            h.CostActualImprovement = e.CostActualImprovement;
            h.CostActualRisk = e.CostActualRisk;
            h.CostCurrentWeekImprovement = e.CostCurrentWeekImprovement;
            h.CostCurrentWeekRisk = e.CostCurrentWeekRisk;
            h.CostLastWeekImprovement = e.CostLastWeekImprovement;
            h.CostLastWeekRisk = e.CostLastWeekRisk;
            h.Classification = e.Classification;
            h.Theme = e.Theme;
            h.ActionParty = e.ActionParty;
            h.PerformanceUnit = e.PerformanceUnit;

            h.Allocations = e.Allocations;

            h.LevelOfEstimate = e.LevelOfEstimate;
            h.Period = e.Period;

            return h;
        }

        public override string TableName
        {
            get { return "WEISWellPIPElementHistories"; }
        }

        public static List<WellPIPElementHistory> GetHistories(string PIPId, int ElementId, int LevelofEstimate)
        {
            var datas = DataHelper.Populate<WellPIPElementHistory>("WEISWellPIPElementHistories", Query.And(Query.EQ("PIPId", PIPId), Query.EQ("ElementId", ElementId), Query.EQ("LevelOfEstimate", LevelofEstimate)));
            return datas.OrderByDescending(x => x.LastUpdate).ToList();
        }

        public static WellPIPElementHistory GetLastHistory(string PIPId, int ElementId, int LevelofEstimate)
        {
            var datas = DataHelper.Populate<WellPIPElementHistory>("WEISWellPIPElementHistories", Query.And(Query.EQ("PIPId", PIPId), Query.EQ("ElementId", ElementId), Query.EQ("LevelOfEstimate", LevelofEstimate)));
            if (datas != null && datas.Count() > 0)
            {
                return datas.OrderByDescending(x => x.LastUpdate).FirstOrDefault();
            }
            else
                return null;
        }

        public static List<WellPIPElementHistory> GetHistories(string PIPId, int ElementId)
        {
            var datas = DataHelper.Populate<WellPIPElementHistory>("WEISWellPIPElementHistories", Query.And(Query.EQ("PIPId", PIPId), Query.EQ("ElementId", ElementId)));
            return datas.OrderByDescending(x => x.LastUpdate).ToList();
        }

        public static List<PIPElement> GetLOV(string WellName, string SeqId, string ActvType)
        {
            var t = DataHelper.Populate("WEISWellPIPs", Query.And(Query.EQ("WellName", WellName), Query.EQ("SequenceId", SeqId), Query.EQ("ActivityType", ActvType)));
            var tdata = DataHelper.Populate<WellPIP>("WEISWellPIPs", Query.And(Query.EQ("WellName", WellName), Query.EQ("SequenceId", SeqId), Query.EQ("ActivityType", ActvType)));

            if (t != null && t.Count() > 0)
            {
                // set LOV
                var datas = DataHelper.Populate<PIPElementLoE>("WEISWellPIPElementLoE", 
                    Query.And(Query.EQ("PIPId", BsonHelper.GetString(t.FirstOrDefault(),  "_id") )));
                
                foreach (var ele in tdata.FirstOrDefault().Elements)
                {
                    if (datas.Where(x => x.ElementId == ele.ElementId).Count() > 0)
                    {
                        ele.LevelOfEstimate = datas.Where(x => x.ElementId == ele.ElementId).OrderByDescending(x=>x.LevelOfEstimate).FirstOrDefault().LevelOfEstimate;
                    }
                    else
                    {
                        ele.LevelOfEstimate = 0;
                    }

                }
            }
            return tdata.FirstOrDefault().Elements;
            //var datas = DataHelper.Populate<WellPIPElementHistory>("WEISWellPIPElementHistories", Query.And(Query.EQ("PIPId", PIPId), Query.EQ("ElementId", ElementId)));
            //return //datas.OrderByDescending(x => x.LastUpdate).ToList();
        }

        public static WellPIPElementHistory GetLastHistory(string PIPId, int ElementId)
        {
            var datas = DataHelper.Populate<WellPIPElementHistory>("WEISWellPIPElementHistories", Query.And(Query.EQ("PIPId", PIPId), Query.EQ("ElementId", ElementId)));
            if (datas != null && datas.Count() > 0)
            {
                return datas.OrderByDescending(x => x.LastUpdate).FirstOrDefault();
            }
            else
                return null;
        }

    }
}
