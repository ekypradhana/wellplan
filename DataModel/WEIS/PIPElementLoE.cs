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
    public class PIPElementLoE : ECISModel
    {
        public string PIPId { get; set; }

        public int ElementId { get; set; }
        //public string Title { get; set; }
        public bool Realized { get; set; }

        public double RealizedDays { get; set; }
        public double RealizedCost { get; set; }
        public bool isPositive { get; set; }

        public double DaysPlanImprovement { get; set; }
        public double DaysPlanRisk { get; set; }
        public double DaysActualImprovement { get; set; }
        public double DaysActualRisk { get; set; }
        public double DaysLastWeekImprovement { get; set; }
        public double DaysLastWeekRisk { get; set; }
        public double DaysCurrentWeekImprovement { get; set; }
        public double DaysCurrentWeekRisk { get; set; }
        public double CostPlanImprovement { get; set; }
        public double CostPlanRisk { get; set; }
        public double CostActualImprovement { get; set; }
        public double CostActualRisk { get; set; }
        public double CostCurrentWeekImprovement { get; set; }
        public double CostCurrentWeekRisk { get; set; }
        public double CostLastWeekImprovement { get; set; }
        public double CostLastWeekRisk { get; set; }
        public string Classification { get; set; }
        public string Theme { get; set; }
        public string ActionParty { get; set; }
        private string _completion;
        public object Completion
        {
            get
            {
                return this._completion;
            }
            set
            {
                this._completion = Convert.ToString(value);
            }
        }

        public int LevelOfEstimate { get; set; }

        public string PerformanceUnit { get; set; }
        public DateRange _range;
        public DateRange Period
        {
            get
            {
                if (_range == null) _range = new DateRange { Start = Tools.DefaultDate, Finish = Tools.DefaultDate };
                return _range;
            }

            set
            {
                _range = value;
            }
        }


        public List<PIPAllocation> Allocations { get; set; }

        //public List<WellPIPElementHistory> LoEHistories { get; set; }

        public string UpdateBy { get; set; }

        public override BsonDocument PostSave(BsonDocument doc, string[] references = null)
        {
            var y = WellPIPElementHistory.CopyFromElementLoE(this);
            y.LogAction = "insert/update";
            y.Save();

            // check and remove higher LoEs
            var LoEs = GetPIPElementLoEs(this.PIPId, this.ElementId);
            if (LoEs != null && LoEs.Count > 0)
            {
                var haveLoesGreater =  LoEs.Where(x => x.LevelOfEstimate > this.LevelOfEstimate);
                if (haveLoesGreater != null && haveLoesGreater.Count() > 0)
                {
                    foreach (var loeg in haveLoesGreater)
                    {
                        // delete existing LoE
                        DataHelper.Delete(this.TableName, Query.And(Query.EQ("PIPId", loeg.PIPId), Query.EQ("ElementId", loeg.ElementId), Query.EQ("LevelOfEstimate", loeg.LevelOfEstimate)));
                        // create log
                        var delete = WellPIPElementHistory.CopyFromElementLoE(loeg);
                        delete.LogAction = "delete";
                        delete.Save();
                    }
                }
            }

            return this.ToBsonDocument();
        }

        public void RemoveHigherLOEs(int currentLoE)
        { 
            
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            _id = String.Format("P{0}-E{1}-L{2}", PIPId.Replace(" ", ""), ElementId.ToString(), LevelOfEstimate);
            return this.ToBsonDocument();
        }

        public override string TableName
        {
            get { return "WEISWellPIPElementLoE"; }
        }


        public static PIPElementLoE GetPIPElementLoE(string PIPId, int ElementId, int LevelofEstimate)
        {
            var datas = DataHelper.Populate<PIPElementLoE>("WEISWellPIPElementLoE", Query.And(Query.EQ("PIPId", PIPId), Query.EQ("ElementId", ElementId), Query.EQ("LevelOfEstimate", LevelofEstimate)));
            if (datas != null && datas.Count > 0)
                return datas.FirstOrDefault();
            else
                return datas.FirstOrDefault();
        }

        public static List<PIPElementLoE> GetPIPElementLoEs(string PIPId, int ElementId)
        {
            var datas = DataHelper.Populate<PIPElementLoE>("WEISWellPIPElementLoE", Query.And(Query.EQ("PIPId", PIPId), Query.EQ("ElementId", ElementId)));
            if (datas != null && datas.Count > 0)
                return datas.ToList();
            else
                return null;
        }

        public void BulkSaveLevelOfEstimate(string PIPId, List<int> ElementIds, int LoE, string UpdateBy)
        {
            var res = GetPIPElement(PIPId, ElementIds);

            foreach (var t in res)
            {
                var tr = TransformElementToLOE(t, PIPId, UpdateBy);
                tr.LevelOfEstimate = LoE;
                tr.Save();
            }
        }

        public static List<PIPElement> GetPIPElementWithLoFValues(string PIPId)
        {
            var datas = DataHelper.Populate<WellPIP>("WEISWellPIPs", Query.EQ("_id", PIPId));
            if (datas != null && datas.Count > 0)
            {
                if (datas.FirstOrDefault().Elements != null && datas.FirstOrDefault().Elements.Count > 0)
                {
                    var elemenst = datas.FirstOrDefault().Elements;
                    if (elemenst != null && elemenst.Count() > 0)
                    {
                        foreach (var y in elemenst)
                        {
                            var eos = GetPIPElementLoEs(PIPId, y.ElementId);
                            if (eos != null && eos.Count > 0)
                            {
                                if (eos.Where(x => x.PIPId == PIPId && x.ElementId == y.ElementId) != null && eos.Where(x => x.PIPId == PIPId && x.ElementId == y.ElementId).Count() > 0)
                                {
                                    y.LevelOfEstimate = eos.Where(x => x.PIPId == PIPId && x.ElementId == y.ElementId).OrderByDescending(x => x.LevelOfEstimate).FirstOrDefault()
                                        .LevelOfEstimate;
                                }
                                else
                                    y.LevelOfEstimate = 0;
                            }
                            else
                            {
                                y.LevelOfEstimate = 0;
                            }

                        }
                        return elemenst;
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

        public static List<PIPElement> GetPIPElement(string PIPId, List<int> ElementIds)
        {
            var datas = DataHelper.Populate<WellPIP>("WEISWellPIPs", Query.EQ("_id", PIPId));
            if (datas != null && datas.Count > 0)
            {
                if (datas.FirstOrDefault().Elements != null && datas.FirstOrDefault().Elements.Count > 0)
                {
                    var elemenst = datas.FirstOrDefault().Elements; 
                    var res = elemenst .Where(x=> ElementIds.Contains(x.ElementId)).ToList();

                    if (res != null && res.Count() > 0)
                    {
                        return res;
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

        public static PIPElement GetPIPElement(string PIPId, int ElementId)
        {
            var datas = DataHelper.Populate<WellPIP>("WEISWellPIPs", Query.EQ("_id", PIPId));
            if (datas != null && datas.Count > 0)
            {
                if (datas.FirstOrDefault().Elements != null && datas.FirstOrDefault().Elements.Count > 0)
                {
                    var ele = datas.FirstOrDefault().Elements.Where(x => x.ElementId == ElementId);
                    if (ele != null && ele.Count() > 0)
                        return ele.FirstOrDefault();
                    else
                        return null;
                }
                else
                    return null;
            }
            else
                return null;
        }

        public static PIPElementLoE TransformElementToLOE(PIPElement e, string PIPId, string UpdateBy)
        {
            PIPElementLoE h = new PIPElementLoE();
            h.PIPId = PIPId;
            h.Realized = e.Realized;

            h.UpdateBy = UpdateBy;

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

            h.ElementId = e.ElementId;

            //h.RealizedCost = e.RealizedCost;
            //h.RealizedDays = e.RealizedDays;
            h.isPositive = e.isPositive;
            
            return h;
        }
    }

}
