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
    public class PlanSimulationHeader : ECISModel
    {

        public override BsonDocument PostSave(BsonDocument doc, string[] references = null)
        {

           
            return this.ToBsonDocument();
        }

        public override string TableName
        {
            get { return "WEISPlanSimulationHeader"; }
        }
        public string CreatedBy { get; set; }
        public string CopyFrom { get; set; }
        public bool Locked { get;set;}

        public List<PlanSimulationComparison> Comparisons { get; set; }

        private List<PlanValue> _annval;
        private List<PlanSimulationBucket> _buckets;
        private int _temp = 0;
        public List<PlanValue> AnnualValues
        {
            get
            {
                //if (_temp == 0)
                //    return GetAnnualValues();
                //else
                    return _annval;

                //return _annval;
            }
            set
            {
                _annval = value;
            }
        }

        public PlanSimulationHeader()
        {
            Comparisons = new List<PlanSimulationComparison>();
        }

        public List<PlanValue> GetAnnualValues()
        {
            var buckets = PlanSimulationBucket.Populate<PlanSimulationBucket>(Query.EQ("SimulationId", this._id.ToString()));
            if (buckets.Any())
            {
                DateRange dr = new DateRange(buckets.Select(x => x.SimSchedule.Start).Where(x => x.Year > 1900).ToList().Min(), buckets.Select(x => x.SimSchedule.Finish).Max());
                var result = new List<PlanValue>();
                var result2 = new List<PlanValue>();

                foreach (var i in buckets)
                {
                    var simValuesBuckets = i.SimulationValues.GroupBy(x => x.DateId);
                    foreach (var b in simValuesBuckets)
                    {
                        PlanValue pv = new PlanValue();
                        pv.DateId = b.Key;
                        pv.Cost = b.Sum(x => x.Cost);
                        pv.DateRange = new DateRange(
                            b.ToList().Min(y => y.DateRange.Start),
                            b.ToList().Max(y => y.DateRange.Finish)
                                                );
                        pv.Days = b.Sum(x => x.Days);
                        result.Add(pv);
                    }
                }

                var resu = result.GroupBy(x => x.DateId);
                foreach (var b in resu)
                {
                    PlanValue pv = new PlanValue();
                    pv.DateId = b.Key;
                    pv.Cost = b.Sum(x => x.Cost);
                    pv.DateRange = new DateRange(
                        b.ToList().Min(y => y.DateRange.Start),
                        b.ToList().Max(y => y.DateRange.Finish)
                                            );
                    pv.Days = b.Sum(x => x.Days);
                    result2.Add(pv);
                }
                return result2.OrderBy(x => x.DateId).ToList();
            }
            return new List<PlanValue>();
            //DataHelper.Save("WEISPlanSimulationHeader", this.ToBsonDocument());
        }

        private List<PlanValue> GetAnnualValuesCompare()
        {

            if (_buckets.Any())
            {
                DateRange dr = new DateRange(_buckets.Select(x => x.SimSchedule.Start).Where(x => x.Year > 1900).ToList().Min(), _buckets.Select(x => x.SimSchedule.Finish).Max());
                var result = new List<PlanValue>();
                var result2 = new List<PlanValue>();

                foreach (var i in _buckets)
                {
                    var simValuesBuckets = i.SimulationValues.GroupBy(x => x.DateId);
                    foreach (var b in simValuesBuckets)
                    {
                        PlanValue pv = new PlanValue();
                        pv.DateId = b.Key;
                        pv.Cost = b.Sum(x => x.Cost);
                        pv.DateRange = new DateRange(
                            b.ToList().Min(y => y.DateRange.Start),
                            b.ToList().Max(y => y.DateRange.Finish)
                                                );
                        pv.Days = b.Sum(x => x.Days);
                        result.Add(pv);
                    }
                }

                var resu = result.GroupBy(x => x.DateId);
                foreach (var b in resu)
                {
                    PlanValue pv = new PlanValue();
                    pv.DateId = b.Key;
                    pv.Cost = b.Sum(x => x.Cost);
                    pv.DateRange = new DateRange(
                        b.ToList().Min(y => y.DateRange.Start),
                        b.ToList().Max(y => y.DateRange.Finish)
                                            );
                    pv.Days = b.Sum(x => x.Days);
                    result2.Add(pv);
                }
                return result2.OrderBy(x => x.DateId).ToList();
            }
            return new List<PlanValue>();
            //DataHelper.Save("WEISPlanSimulationHeader", this.ToBsonDocument());
        }



        public void BindComparison(string param)
        {

            // param sim ID
            if (param != null && param.Length > 3 && param.Substring(0, 3).Equals("SIM"))
            {
                string[] cx = param.Split('-');
                var s = PlanSimulationHeader.Get<PlanSimulationHeader>(Query.EQ("_id", cx[0]));

                PlanSimulationComparison newComp = BsonSerializer.Deserialize<PlanSimulationComparison>(s.ToBsonDocument());
                newComp.CompNo = Comparisons.Count() > 0 ? Comparisons.Max(x => x.CompNo) + 1 : 1;


                Comparisons.Add(newComp);
                this.Save();
            }
            else
            {
                PlanSimulationHeader h = new PlanSimulationHeader();
                h = PlanSimulationHeader.Get<PlanSimulationHeader>(Query.EQ("_id", this._id.ToString()));


                var buckets = PlanSimulationBucket.Populate<PlanSimulationBucket>(Query.EQ("SimulationId", this._id.ToString()));
                if (buckets.Any())
                {
                    #region pick from buckets
                    foreach (var t in buckets)
                    {
                        // param current data
                        if (param.Contains("OP"))
                        {
                            t.Sim = t.OP;
                            t.SimSchedule = t.OPSchedule;
                            t.SimulationValues = new List<PlanValue>();
                            t.SimulationValues = t.Values.Where(x => x.Title.Equals("OP")).ToList();
                            h.CopyFrom = "Current OP";
                        }
                        else if (param.Contains("LE"))
                        {
                            t.Sim = t.LE;
                            t.SimSchedule = t.LESchedule;
                            t.SimulationValues = new List<PlanValue>();
                            t.SimulationValues = t.Values.Where(x => x.Title.Equals("LE")).ToList();
                            h.CopyFrom = "Current LE";

                        }
                        else if (param.Contains("LS"))
                        {
                            t.Sim = t.LS;
                            t.SimSchedule = t.LSSchedule;
                            t.SimulationValues = new List<PlanValue>();
                            t.SimulationValues = t.Values.Where(x => x.Title.Equals("LS")).ToList();
                            h.CopyFrom = "Current LS";

                        }
                    }

                    DateRange dr = new DateRange(buckets.Select(x => x.SimSchedule.Start).Where(x => x.Year > 1900).ToList().Min(), buckets.Select(x => x.SimSchedule.Finish).Max());
                    var result = new List<PlanValue>();
                    var result2 = new List<PlanValue>();

                    foreach (var i in buckets)
                    {
                        var simValuesBuckets = i.SimulationValues.GroupBy(x => x.DateId);
                        foreach (var b in simValuesBuckets.ToList())
                        {
                            PlanValue pv = new PlanValue();
                            pv.DateId = b.Key;
                            pv.Cost = b.Sum(x => x.Cost);
                            pv.DateRange = new DateRange(
                                b.ToList().Min(y => y.DateRange.Start),
                                b.ToList().Max(y => y.DateRange.Finish)
                                                    );
                            pv.Days = b.Sum(x => x.Days);
                            result.Add(pv);
                        }
                    }

                    var resu = result.GroupBy(x => x.DateId);
                    foreach (var b in resu)
                    {
                        PlanValue pv = new PlanValue();
                        pv.DateId = b.Key;
                        pv.Cost = b.Sum(x => x.Cost);
                        pv.DateRange = new DateRange(
                            b.ToList().Min(y => y.DateRange.Start),
                            b.ToList().Max(y => y.DateRange.Finish)
                                                );
                        pv.Days = b.Sum(x => x.Days);
                        result2.Add(pv);
                    }

                    //h.AnnualValues = result2;

                    PlanSimulationComparison newComp = new PlanSimulationComparison();

                    newComp._id = h._id;
                    newComp.Title = h.Title;
                    newComp.AnnualValues = new List<PlanValue>();
                    newComp._temp = 1;
                    foreach(var y in result2)
                        newComp.AnnualValues.Add(y);

                    newComp.CreatedBy = h.CreatedBy;
                    newComp.CopyFrom = h.CopyFrom;
                    newComp.Comparisons = new List<PlanSimulationComparison>();

                        //BsonSerializer.Deserialize<PlanSimulationComparison>(h.ToBsonDocument());
                    newComp.CompNo = Comparisons.Count() > 0 ? Comparisons.Max(x => x.CompNo) + 1 : 1;

                    this.Comparisons.Add(newComp);
                    this.Save();
                    #endregion
                }
                else
                {
                    // pick from current WellPlan
                    var wps = WellActivity.Populate<WellActivity>();
                    foreach (var x in wps)
                    {
                        if (x.Phases.Any())
                        {

                            foreach (var t in x.Phases)
                            {
                                PlanSimulationBucket b = new PlanSimulationBucket();
                                b.SimulationId = this._id.ToString();
                                b.LevelOfEstimate = t.LevelOfEstimate;
                                b.ExType = x.EXType; // BsonHelper.GetString(t, "ExType");
                                b.GenerateBucket(t, "0", x.WellName, x.RigName,x.UARigSequenceId);
                                //b.Save();
                                buckets.Add(b);
                            }
                        }
                    }
                    _buckets = new List<PlanSimulationBucket>();
                    _buckets = buckets;
                    _temp = 1;

                    foreach (var t in _buckets)
                    {
                        if (param.Contains("OP"))
                        {
                            t.Sim = t.OP;
                            t.SimSchedule = t.OPSchedule;
                            t.SimulationValues = new List<PlanValue>();
                            t.SimulationValues = t.Values.Where(x => x.Title.Equals("OP")).ToList();
                            h.CopyFrom = "Current OP";
                        }
                        else if (param.Contains("LE"))
                        {
                            t.Sim = t.LE;
                            t.SimSchedule = t.LESchedule;
                            t.SimulationValues = new List<PlanValue>();
                            t.SimulationValues = t.Values.Where(x => x.Title.Equals("LE")).ToList();
                            h.CopyFrom = "Current LE";

                        }
                        else if (param.Contains("LS"))
                        {
                            t.Sim = t.LS;
                            t.SimSchedule = t.LSSchedule;
                            t.SimulationValues = new List<PlanValue>();
                            t.SimulationValues = t.Values.Where(x => x.Title.Equals("LS")).ToList();
                            h.CopyFrom = "Current LS";

                        }
                    }
                    this.CopyFrom = param;
                    _annval = new List<PlanValue>();

                    foreach (var y in GetAnnualValuesCompare())
                    {
                        _annval.Add(y);

                    }
                    h.AnnualValues = _annval;

                    PlanSimulationComparison newComp = BsonSerializer.Deserialize<PlanSimulationComparison>(h.ToBsonDocument());
                    newComp.CompNo = Comparisons.Count() > 0 ? Comparisons.Max(x => x.CompNo) + 1 : 1;


                    Comparisons.Add(newComp);

                    this.Comparisons.Add(newComp);
                    this.Save();
                }



            }
        }
    }

    public class PlanSimulationComparison : PlanSimulationHeader{
        public int CompNo { get; set; }

    }

}
