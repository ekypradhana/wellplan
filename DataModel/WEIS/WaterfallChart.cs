using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace ECIS.Client.WEIS
{
    /// <summary>
    /// PIP Analysis - Type 1: Lv0 Estimate --
    /// </summary>
    public class WaterfallLvl0Estimate
    {
        public string Type { get; set; }

        public double Performance { get; set; }
        public List<WaterfallAttribute> IncludedInOPs { get; set; }
        public double OP { get; set; }
        public List<WaterfallAttribute> Opportunities { get; set; }
        public double LE { get; set; }

        public double OPLine { get; set; }
        public double TQTarget { get; set; }

        public WaterfallLvl0Estimate()
        {
            IncludedInOPs = new List<WaterfallAttribute>();
            Opportunities = new List<WaterfallAttribute>();
        }

        public static Double GetDayOrCost(PIPElement d, string DayorCost = "Day", string element = "Plan")
        {
            double ret = 0;
            switch (element)
            {
                case "Plan":
                    if (DayorCost.Equals("Day")) ret = d.DaysPlanImprovement + d.DaysPlanRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostPlanImprovement + d.CostPlanRisk;
                    break;

                case "Actual":
                    if (DayorCost.Equals("Day")) ret = d.DaysActualImprovement + d.DaysActualRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostActualImprovement + d.CostActualRisk;
                    break;

                case "LE":
                    if (DayorCost.Equals("Day")) ret = d.DaysCurrentWeekImprovement + d.DaysCurrentWeekRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostCurrentWeekImprovement + d.CostCurrentWeekRisk;
                    break;
            }
            return ret;
        }


        public static WaterfallLvl0Estimate LoadData(
            string GroupBy = "Classification",
            string DayOrCost = "Day",
            string WellName = "",
            string SequenceId = "",
            string ActivityType = "",
            List<PIPElement> PIPs = null)
        {
            var division = 1000000.0;
            IMongoQuery q = Query.Null;
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("UARigSequenceId", SequenceId));
            var wa = WellActivity.Get<WellActivity>(Query.And(qs));
            if (wa == null) throw new Exception("No well plan defined for this activity, Waterfall can't be generated");
            var act = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(ActivityType));
            if (act.AFE.Days == 0) act.AFE = act.OP;
            if (act.TQ.Days == 0) act.TQ = new WellDrillData
            {
                Days = act.OP.Days * 0.75,
                Cost = act.OP.Cost * 0.75
            };
            var TQ = DayOrCost.Equals("Day") ? act.TQ.Days : act.TQ.Cost;
            var AFE = DayOrCost.Equals("Day") ? act.AFE.Days : act.AFE.Cost;
            var OP = DayOrCost.Equals("Day") ? act.OP.Days : act.Plan.Cost;

            if (DayOrCost.Equals("Cost"))
            {
                TQ /= division;
                AFE /= division;
                OP /= division;
            }
            WaterfallLvl0Estimate res = new WaterfallLvl0Estimate();
            res.Performance = OP;
            res.TQTarget = TQ;

            if (DayOrCost.Equals("Day"))
            {
                res.OP = act.Plan.Days;
                res.OPLine = act.Plan.Days;
                res.LE = act.LWE.Days;

            }
            else
            {
                res.OP = act.Plan.Cost;
                res.OPLine = act.Plan.Cost;
                res.LE = act.LWE.Cost;
            }

            res.Type = DayOrCost;


            if (PIPs != null && PIPs.Count > 0)
            {

                var groupLoV = PIPs.GroupBy(d => d.LevelOfEstimate);

                foreach (var y in groupLoV.ToList())
                {
                    if (y.Key > 2) // opportunities
                    {
                        var groupPIPS = y.ToList().GroupBy(d => d.ToBsonDocument().GetString(GroupBy));

                        if (DayOrCost.Equals("Day"))
                        {
                            foreach (var gp in groupPIPS)
                            {
                                res.Opportunities.Add(new WaterfallAttribute { Title = gp.Key, Value = gp.Sum(x => x.DaysPlanImprovement) });
                            }
                        }
                        else
                        {
                            foreach (var gp in groupPIPS)
                            {
                                res.Opportunities.Add(new WaterfallAttribute { Title = gp.Key, Value = gp.Sum(x => x.CostPlanImprovement) });
                            }
                        }
                    }
                    else
                    {
                        var groupPIPS = y.ToList().GroupBy(d => d.ToBsonDocument().GetString(GroupBy));

                        if (DayOrCost.Equals("Day"))
                        {
                            foreach (var gp in groupPIPS)
                            {
                                res.IncludedInOPs.Add(new WaterfallAttribute { Title = gp.Key, Value = gp.Sum(x => x.DaysPlanImprovement) });
                            }
                        }
                        else
                        {
                            foreach (var gp in groupPIPS)
                            {
                                res.IncludedInOPs.Add(new WaterfallAttribute { Title = gp.Key, Value = gp.Sum(x => x.CostPlanImprovement) });
                            }
                        }
                        // included
                    }
                }
            }

            // group penutupan
            if (res.IncludedInOPs.Count() > 0)
            {
                var gr = res.IncludedInOPs.GroupBy(x => x.Title).ToList();
                List<WaterfallAttribute> atts = new List<WaterfallAttribute>();

                foreach (var g in gr)
                {
                    WaterfallAttribute att = new WaterfallAttribute();
                    att.Value = g.Sum(x => x.Value);
                    att.Title = g.Key;
                    atts.Add(att);
                }

                res.IncludedInOPs.Clear();
                res.IncludedInOPs = atts;
            }

            if (res.Opportunities.Count() > 0)
            {
                var gr = res.Opportunities.GroupBy(x => x.Title);
                List<WaterfallAttribute> atts = new List<WaterfallAttribute>();
                foreach (var g in gr.ToList())
                {
                    WaterfallAttribute att = new WaterfallAttribute();
                    att.Title = g.Key;
                    att.Value = g.Sum(x => x.Value);
                    atts.Add(att);
                }
                res.Opportunities.Clear();
                res.Opportunities = atts;
            }


            if (DayOrCost.Equals("Cost"))
            {
                //res.LE = res.LE / division;
                //res.OPLine = res.OPLine / division;
                res.OP = res.OP / division;
            }

            return res;

        }


    }

    /// <summary>
    /// PIP Analysis - Type 2: Lv3 / DG3 Estimate --
    /// </summary>
    public class WaterfallDG3Estimate
    {
        public string Type { get; set; }

        public double Performance { get; set; }
        public List<WaterfallAttribute> Realised { get; set; }
        public List<WaterfallAttribute> Remaining { get; set; }
        public double LE { get; set; }

        public double OPLine { get; set; }
        public double TQTarget { get; set; }

        public WaterfallDG3Estimate()
        {
            Realised = new List<WaterfallAttribute>();
            Remaining = new List<WaterfallAttribute>();
        }

        public static WaterfallDG3Estimate LoadData(
            string GroupBy = "Classification",
            string DayOrCost = "Day",
            string WellName = "",
            string SequenceId = "",
            string ActivityType = "",
            List<PIPElement> PIPs = null)
        {
            var division = 1000000.0;
            IMongoQuery q = Query.Null;
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("UARigSequenceId", SequenceId));
            var wa = WellActivity.Get<WellActivity>(Query.And(qs));
            if (wa == null) throw new Exception("No well plan defined for this activity, Waterfall can't be generated");
            var act = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(ActivityType));
            if (act.AFE.Days == 0) act.AFE = act.OP;
            if (act.TQ.Days == 0) act.TQ = new WellDrillData
            {
                Days = act.OP.Days * 0.75,
                Cost = act.OP.Cost * 0.75
            };
            var TQ = DayOrCost.Equals("Day") ? act.TQ.Days : act.TQ.Cost;
            var AFE = DayOrCost.Equals("Day") ? act.AFE.Days : act.AFE.Cost;
            var OP = DayOrCost.Equals("Day") ? act.Plan.Days : act.Plan.Cost;

            if (DayOrCost.Equals("Cost"))
            {
                TQ /= division;
                AFE /= division;
                OP /= division;
            }
            WaterfallDG3Estimate res = new WaterfallDG3Estimate();
            res.Performance = OP;
            res.TQTarget = TQ;

            if (DayOrCost.Equals("Day"))
            {
                res.OPLine = act.Plan.Days;
                res.LE = act.LWE.Days;

            }
            else
            {
                res.OPLine = act.Plan.Cost;
                res.LE = act.LWE.Cost;
            }

            res.Type = DayOrCost;


            if (PIPs != null && PIPs.Count > 0)
            {
                var groupPIPS = PIPs.Where(z => z.Realized == true).GroupBy(d => d.ToBsonDocument().GetString(GroupBy));
                foreach (var y in groupPIPS.ToList())
                {
                    if (DayOrCost.Equals("Day"))
                        res.Realised.Add(new WaterfallAttribute { Title = y.Key, Value = y.Sum(x => x.DaysPlanImprovement) });
                    else
                        res.Realised.Add(new WaterfallAttribute { Title = y.Key, Value = y.Sum(x => x.CostPlanImprovement) });
                }

                var groupPIPSNotRealize = PIPs.Where(z => z.Realized == false).GroupBy(d => d.ToBsonDocument().GetString(GroupBy));
                foreach (var y in groupPIPSNotRealize.ToList())
                {
                    if (DayOrCost.Equals("Day"))
                        res.Remaining.Add(new WaterfallAttribute { Title = y.Key, Value = y.Sum(x => x.DaysPlanImprovement) });
                    else
                        res.Remaining.Add(new WaterfallAttribute { Title = y.Key, Value = y.Sum(x => x.CostPlanImprovement) });
                }
            }
            return res;

        }
    }

    /// <summary>
    /// PIP Analysis - Type 3: Lv4 / AFE Estimate
    /// </summary>
    public class WaterfallAFEEstimate
    {
        public string Type { get; set; }

        public double Performance { get; set; }
        public List<WaterfallAttribute> IncludedInOP { get; set; }

        public double OP { get; set; }
        public List<WaterfallAttribute> Realised { get; set; }

        public double AFE { get; set; }
        public List<WaterfallAttribute> Remaining { get; set; }

        public double LE { get; set; }

        public double OPLine { get; set; }
        public double TQTarget { get; set; }

        public WaterfallAFEEstimate()
        {
            IncludedInOP = new List<WaterfallAttribute>();
            Realised = new List<WaterfallAttribute>();
            Remaining = new List<WaterfallAttribute>();
        }

        public static Double GetDayOrCost(PIPElement d, string DayorCost = "Day", string element = "Plan")
        {
            double ret = 0;
            switch (element)
            {
                case "Plan":
                    if (DayorCost.Equals("Day")) ret = d.DaysPlanImprovement + d.DaysPlanRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostPlanImprovement + d.CostPlanRisk;
                    break;

                case "Actual":
                    if (DayorCost.Equals("Day")) ret = d.DaysActualImprovement + d.DaysActualRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostActualImprovement + d.CostActualRisk;
                    break;

                case "LE":
                    if (DayorCost.Equals("Day")) ret = d.DaysCurrentWeekImprovement + d.DaysCurrentWeekRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostCurrentWeekImprovement + d.CostCurrentWeekRisk;
                    break;
            }
            return ret;
        }


        public static WaterfallAFEEstimate LoadData(
            string GroupBy = "Classification",
            string DayOrCost = "Day",
            string WellName = "",
            string SequenceId = "",
            string ActivityType = "",
            List<PIPElement> PIPs = null)
        {
            var division = 1000000.0;
            IMongoQuery q = Query.Null;
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("UARigSequenceId", SequenceId));
            var wa = WellActivity.Get<WellActivity>(Query.And(qs));
            if (wa == null) throw new Exception("No well plan defined for this activity, Waterfall can't be generated");
            var act = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(ActivityType));
            if (act.AFE.Days == 0) act.AFE = act.OP;
            if (act.TQ.Days == 0) act.TQ = new WellDrillData
            {
                Days = act.OP.Days * 0.75,
                Cost = act.OP.Cost * 0.75
            };
            var TQ = DayOrCost.Equals("Day") ? act.TQ.Days : act.TQ.Cost;
            var AFE = DayOrCost.Equals("Day") ? act.AFE.Days : act.AFE.Cost;
            var OP = DayOrCost.Equals("Day") ? act.Plan.Days : act.Plan.Cost;

            if (DayOrCost.Equals("Cost"))
            {
                TQ /= division;
                AFE /= division;
                OP /= division;
            }
            WaterfallAFEEstimate res = new WaterfallAFEEstimate();
            res.Performance = OP;
            res.TQTarget = TQ;
            res.AFE = AFE;
            if (DayOrCost.Equals("Day"))
            {
                res.OP = act.Plan.Days;
                res.OPLine = act.Plan.Days;
                res.LE = act.LWE.Days;

            }
            else
            {
                res.OP = act.Plan.Cost;
                res.OPLine = act.Plan.Cost;
                res.LE = act.LWE.Cost;
            }
            res.Type = DayOrCost;


            if (PIPs != null && PIPs.Count > 0)
            {
                var groupPIPS = PIPs.GroupBy(d => d.ToBsonDocument().GetString(GroupBy));

                var resVal = groupPIPS.Select(d =>
                        {
                            var Plan = d.Sum(e => GetDayOrCost(e, DayOrCost, "Plan"));
                            var LE = d.Sum(e => GetDayOrCost(e, DayOrCost, "LE"));

                            if (DayOrCost.Equals("Cost"))
                            {
                                //Plan /= division;
                                //LE /= division;
                            }

                            return new
                            {
                                d.Key,
                                Plan = Plan,
                                LE = LE
                            };
                        })
                        .ToList();

                foreach (var y in groupPIPS.ToList())
                {
                    // IncludedIn OP
                    if (DayOrCost.Equals("Day"))
                        res.IncludedInOP.Add(new WaterfallAttribute { Title = y.Key, Value = y.Sum(x => x.DaysCurrentWeekImprovement) });
                    else
                        res.IncludedInOP.Add(new WaterfallAttribute { Title = y.Key, Value = y.Sum(x => x.CostCurrentWeekImprovement) });

                }
                // realized

                var groupPIPCateg = PIPs.Where(z => z.Realized == true).GroupBy(d => d.ToBsonDocument().GetString(GroupBy));
                foreach (var o in groupPIPCateg.ToList())
                {
                    if (DayOrCost.Equals("Day"))
                        res.Realised.Add(new WaterfallAttribute { Title = o.Key, Value = o.Sum(j => j.DaysPlanImprovement) });
                    else
                        res.Realised.Add(new WaterfallAttribute { Title = o.Key, Value = o.Sum(j => j.CostPlanImprovement) });
                }

                //Remainign
                var groupPIPSNotRealize = PIPs.Where(z => z.Realized == false).GroupBy(d => d.ToBsonDocument().GetString(GroupBy));
                foreach (var n in groupPIPSNotRealize.ToList())
                {
                    if (DayOrCost.Equals("Day"))
                        res.Remaining.Add(new WaterfallAttribute { Title = n.Key, Value = n.Sum(j => j.DaysPlanImprovement) });
                    else
                        res.Remaining.Add(new WaterfallAttribute { Title = n.Key, Value = n.Sum(j => j.CostPlanImprovement) });
                }
            }
            return res;

        }


    }

    /// <summary>
    /// PIP Analysis - Type 4: Execution Estimate 1
    /// </summary>
    public class WaterfallExecutionEstimate
    {
        public string Type { get; set; }

        public double Performance { get; set; }
        public List<WaterfallAttribute> IncludedInOP { get; set; }

        public double OP { get; set; }
        public List<WaterfallAttribute> Realised { get; set; }

        public double AFE { get; set; }
        public List<WaterfallAttribute> RealisedAFE { get; set; }

        public double LE { get; set; }
        public List<WaterfallAttribute> Remaining { get; set; }

        public double OPLine { get; set; }
        public double TQTarget { get; set; }

        public WaterfallExecutionEstimate()
        {
            Realised = new List<WaterfallAttribute>();
            RealisedAFE = new List<WaterfallAttribute>();
            Remaining = new List<WaterfallAttribute>();
            IncludedInOP = new List<WaterfallAttribute>();
        }

        public static Double GetDayOrCost(PIPElement d, string DayorCost = "Day", string element = "Plan")
        {
            double ret = 0;
            switch (element)
            {
                case "Plan":
                    if (DayorCost.Equals("Day")) ret = d.DaysPlanImprovement + d.DaysPlanRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostPlanImprovement + d.CostPlanRisk;
                    break;

                case "Actual":
                    if (DayorCost.Equals("Day")) ret = d.DaysActualImprovement + d.DaysActualRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostActualImprovement + d.CostActualRisk;
                    break;

                case "LE":
                    if (DayorCost.Equals("Day")) ret = d.DaysCurrentWeekImprovement + d.DaysCurrentWeekRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostCurrentWeekImprovement + d.CostCurrentWeekRisk;
                    break;
            }
            return ret;
        }

        public static WaterfallExecutionEstimate LoadData(
          string GroupBy = "Classification",
          string DayOrCost = "Day",
          string WellName = "",
          string SequenceId = "",
          string ActivityType = "",
          List<PIPElement> PIPs = null)
        {
            var division = 1000000.0;
            IMongoQuery q = Query.Null;
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("UARigSequenceId", SequenceId));
            var wa = WellActivity.Get<WellActivity>(Query.And(qs));
            if (wa == null) throw new Exception("No well plan defined for this activity, Waterfall can't be generated");
            var act = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(ActivityType));
            if (act.AFE.Days == 0) act.AFE = act.OP;
            if (act.TQ.Days == 0) act.TQ = new WellDrillData
            {
                Days = act.OP.Days * 0.75,
                Cost = act.OP.Cost * 0.75
            };
            var TQ = DayOrCost.Equals("Day") ? act.TQ.Days : act.TQ.Cost;
            var AFE = DayOrCost.Equals("Day") ? act.AFE.Days : act.AFE.Cost;
            var OP = DayOrCost.Equals("Day") ? act.Plan.Days : act.Plan.Cost;

            if (DayOrCost.Equals("Cost"))
            {
                TQ /= division;
                AFE /= division;
                OP /= division;
            }
            WaterfallExecutionEstimate res = new WaterfallExecutionEstimate();
            res.Performance = OP;
            res.TQTarget = TQ;
            res.AFE = AFE;
            if (DayOrCost.Equals("Day"))
            {
                res.OP = act.Plan.Days;
                res.OPLine = act.Plan.Days;
                res.LE = act.LWE.Days;

            }
            else
            {
                res.OP = act.Plan.Cost;
                res.OPLine = act.Plan.Cost;
                res.LE = act.LWE.Cost;
            }
            res.Type = DayOrCost;


            if (PIPs != null && PIPs.Count > 0)
            {
                var groupPIPS = PIPs.GroupBy(d => d.ToBsonDocument().GetString(GroupBy));

                var resVal = groupPIPS.Select(d =>
                {
                    var Plan = d.Sum(e => GetDayOrCost(e, DayOrCost, "Plan"));
                    var LE = d.Sum(e => GetDayOrCost(e, DayOrCost, "LE"));

                    if (DayOrCost.Equals("Cost"))
                    {
                        //Plan /= division;
                        //LE /= division;
                    }

                    return new
                    {
                        d.Key,
                        Plan = Plan,
                        LE = LE
                    };
                })
                        .ToList();

                foreach (var y in groupPIPS.ToList())
                {
                    // IncludedIn OP
                    if (DayOrCost.Equals("Day"))
                    {

                        var sumDay = y.ToList().Sum(x => x.DaysCurrentWeekImprovement);
                        res.IncludedInOP.Add(new WaterfallAttribute { Title = y.Key, Value = sumDay });

                    }
                    else
                        res.IncludedInOP.Add(new WaterfallAttribute { Title = y.Key, Value = y.ToList().Sum(x => x.CostCurrentWeekImprovement) });

                }
                // realized

                var groupPIPCateg = PIPs.Where(z => z.Realized == true).GroupBy(d => d.ToBsonDocument().GetString(GroupBy));
                foreach (var o in groupPIPCateg.ToList())
                {
                    if (DayOrCost.Equals("Day"))
                        res.Realised.Add(new WaterfallAttribute { Title = o.Key, Value = o.Sum(j => j.DaysPlanImprovement) });
                    else
                        res.Realised.Add(new WaterfallAttribute { Title = o.Key, Value = o.Sum(j => j.CostPlanImprovement) });
                }

                //Remainign
                var groupPIPSNotRealize = PIPs.Where(z => z.Realized == false).GroupBy(d => d.ToBsonDocument().GetString(GroupBy));
                foreach (var n in groupPIPSNotRealize.ToList())
                {
                    if (DayOrCost.Equals("Day"))
                        res.Remaining.Add(new WaterfallAttribute { Title = n.Key, Value = n.Sum(j => j.DaysPlanImprovement) });
                    else
                        res.Remaining.Add(new WaterfallAttribute { Title = n.Key, Value = n.Sum(j => j.CostPlanImprovement) });
                }
            }
            return res;

        }
    }

    /// <summary>
    /// PIP Analysis - Type 5: Execution Estimate 2
    /// </summary>
    public class WaterfallExecutionEstimate2
    {
        public string Type { get; set; }


        public WaterfallExecutionEstimate2()
        {
            AFEBreakdowns = new List<WaterfallAttribute>();
            LEBreakdowns = new List<WaterfallAttribute>();
        }

        public double AFE { get; set; }
        public List<WaterfallAttribute> AFEBreakdowns { get; set; }

        public double LE { get; set; }
        public List<WaterfallAttribute> LEBreakdowns { get; set; }

        public double OPLine { get; set; }
        public double TQTarget { get; set; }

        public static Double GetDayOrCost(PIPElement d, string DayorCost = "Day", string element = "Plan")
        {
            double ret = 0;
            switch (element)
            {
                case "Plan":
                    if (DayorCost.Equals("Day")) ret = d.DaysPlanImprovement + d.DaysPlanRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostPlanImprovement + d.CostPlanRisk;
                    break;

                case "Actual":
                    if (DayorCost.Equals("Day")) ret = d.DaysActualImprovement + d.DaysActualRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostActualImprovement + d.CostActualRisk;
                    break;

                case "LE":
                    if (DayorCost.Equals("Day")) ret = d.DaysCurrentWeekImprovement + d.DaysCurrentWeekRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostCurrentWeekImprovement + d.CostCurrentWeekRisk;
                    break;
            }
            return ret;
        }

        public static WaterfallExecutionEstimate2 LoadData(
          string GroupBy = "Classification",
          string DayOrCost = "Day",
          string WellName = "",
          string SequenceId = "",
          string ActivityType = "",
          List<PIPElement> PIPs = null)
        {
            var division = 1000000.0;
            IMongoQuery q = Query.Null;
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("UARigSequenceId", SequenceId));
            var wa = WellActivity.Get<WellActivity>(Query.And(qs));
            if (wa == null) throw new Exception("No well plan defined for this activity, Waterfall can't be generated");
            var act = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(ActivityType));
            if (act.AFE.Days == 0) act.AFE = act.OP;
            if (act.TQ.Days == 0) act.TQ = new WellDrillData
            {
                Days = act.OP.Days * 0.75,
                Cost = act.OP.Cost * 0.75
            };
            var TQ = DayOrCost.Equals("Day") ? act.TQ.Days : act.TQ.Cost;
            var AFE = DayOrCost.Equals("Day") ? act.AFE.Days : act.AFE.Cost;
            var OP = DayOrCost.Equals("Day") ? act.Plan.Days : act.Plan.Cost;

            if (DayOrCost.Equals("Cost"))
            {
                TQ /= division;
                AFE /= division;
                OP /= division;
            }
            WaterfallExecutionEstimate2 res = new WaterfallExecutionEstimate2();
            //res.Performance = OP;
            res.TQTarget = TQ;
            res.AFE = AFE;
            if (DayOrCost.Equals("Day"))
            {
                //res.OP = act.Plan.Days;
                res.OPLine = act.Plan.Days;
                res.LE = act.LWE.Days;

            }
            else
            {
                //res.OP = act.Plan.Cost;
                res.OPLine = act.Plan.Cost;
                res.LE = act.LWE.Cost;
            }
            res.Type = DayOrCost;


            if (PIPs != null && PIPs.Count > 0)
            {
                var groupPIPS = PIPs.GroupBy(d => d.ToBsonDocument().GetString(GroupBy));

                var resVal = groupPIPS.Select(d =>
                {
                    var Plan = d.Sum(e => GetDayOrCost(e, DayOrCost, "Plan"));
                    var LE = d.Sum(e => GetDayOrCost(e, DayOrCost, "LE"));

                    if (DayOrCost.Equals("Cost"))
                    {
                        //Plan /= division;
                        //LE /= division;
                    }

                    return new
                    {
                        d.Key,
                        Plan = Plan,
                        LE = LE
                    };
                })
                        .ToList();

                // realized

                var groupPIPCateg = PIPs.Where(z => z.Realized == true).GroupBy(d => d.ToBsonDocument().GetString(GroupBy));
                foreach (var o in groupPIPCateg.ToList())
                {
                    if (DayOrCost.Equals("Day"))
                        res.AFEBreakdowns.Add(new WaterfallAttribute { Title = o.Key, Value = o.Sum(j => j.DaysPlanImprovement) });
                    else
                        res.AFEBreakdowns.Add(new WaterfallAttribute { Title = o.Key, Value = o.Sum(j => j.CostPlanImprovement) });
                }

                //Remainign
                var groupPIPSNotRealize = PIPs.Where(z => z.Realized == false).GroupBy(d => d.ToBsonDocument().GetString(GroupBy));
                foreach (var n in groupPIPSNotRealize.ToList())
                {
                    if (DayOrCost.Equals("Day"))
                        res.AFEBreakdowns.Add(new WaterfallAttribute { Title = n.Key, Value = n.Sum(j => j.DaysPlanImprovement) });
                    else
                        res.AFEBreakdowns.Add(new WaterfallAttribute { Title = n.Key, Value = n.Sum(j => j.CostPlanImprovement) });
                }


            }
            return res;
        }
    }

    /// <summary>
    /// PIP Analysis - Type 6: Final
    /// </summary>
    public class WaterfallFinal
    {
        public string Type { get; set; }


        public double OP { get; set; }
        public List<WaterfallAttribute> RealiseDuringPlannings { get; set; }

        public double Actual { get; set; }
        public List<WaterfallAttribute> RealiseDuringOperations { get; set; }

        public double OPLine { get; set; }
        public double TQTarget { get; set; }

        public WaterfallFinal()
        {
            RealiseDuringPlannings = new List<WaterfallAttribute>();
            RealiseDuringOperations = new List<WaterfallAttribute>();
        }

        public static Double GetDayOrCost(PIPElement d, string DayorCost = "Day", string element = "Plan")
        {
            double ret = 0;
            switch (element)
            {
                case "Plan":
                    if (DayorCost.Equals("Day")) ret = d.DaysPlanImprovement + d.DaysPlanRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostPlanImprovement + d.CostPlanRisk;
                    break;

                case "Actual":
                    if (DayorCost.Equals("Day")) ret = d.DaysActualImprovement + d.DaysActualRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostActualImprovement + d.CostActualRisk;
                    break;

                case "LE":
                    if (DayorCost.Equals("Day")) ret = d.DaysCurrentWeekImprovement + d.DaysCurrentWeekRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostCurrentWeekImprovement + d.CostCurrentWeekRisk;
                    break;
            }
            return ret;
        }

        public static WaterfallFinal LoadData(
          string GroupBy = "Classification",
          string DayOrCost = "Day",
          string WellName = "",
          string SequenceId = "",
          string ActivityType = "",
          List<PIPElement> PIPs = null)
        {
            var division = 1000000.0;
            IMongoQuery q = Query.Null;
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("UARigSequenceId", SequenceId));
            var wa = WellActivity.Get<WellActivity>(Query.And(qs));
            if (wa == null) throw new Exception("No well plan defined for this activity, Waterfall can't be generated");
            var act = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(ActivityType));
            if (act.AFE.Days == 0) act.AFE = act.OP;
            if (act.TQ.Days == 0) act.TQ = new WellDrillData
            {
                Days = act.OP.Days * 0.75,
                Cost = act.OP.Cost * 0.75
            };
            var TQ = DayOrCost.Equals("Day") ? act.TQ.Days : act.TQ.Cost;
            var AFE = DayOrCost.Equals("Day") ? act.AFE.Days : act.AFE.Cost;
            var OP = DayOrCost.Equals("Day") ? act.Plan.Days : act.Plan.Cost;

            if (DayOrCost.Equals("Cost"))
            {
                TQ /= division;
                AFE /= division;
                OP /= division;
            }
            WaterfallFinal res = new WaterfallFinal();
            if (DayOrCost.Equals("Day"))
            {
                res.OP = act.Plan.Days;
                res.OPLine = act.Plan.Days;
                res.Actual = act.Actual.Days;

            }
            else
            {
                res.OP = act.Plan.Cost;
                res.OPLine = act.Plan.Cost;
                res.Actual = act.Actual.Cost;
            }
            res.Type = DayOrCost;

            if (PIPs != null && PIPs.Count > 0)
            {
                var groupPIPS = PIPs.GroupBy(d => d.ToBsonDocument().GetString(GroupBy));

                var resVal = groupPIPS.Select(d =>
                {
                    var Plan = d.Sum(e => GetDayOrCost(e, DayOrCost, "Plan"));
                    var LE = d.Sum(e => GetDayOrCost(e, DayOrCost, "LE"));

                    if (DayOrCost.Equals("Cost"))
                    {
                        //Plan /= division;
                        //LE /= division;
                    }

                    return new
                    {
                        d.Key,
                        Plan = Plan,
                        LE = LE
                    };
                })
                        .ToList();

                // realized

                var groupPIPCateg = PIPs.Where(z => z.Realized == true).GroupBy(d => d.ToBsonDocument().GetString(GroupBy));
                foreach (var o in groupPIPCateg.ToList())
                {
                    if (DayOrCost.Equals("Day"))
                        res.RealiseDuringPlannings.Add(new WaterfallAttribute { Title = o.Key, Value = o.Sum(j => j.DaysPlanImprovement) });
                    else
                        res.RealiseDuringPlannings.Add(new WaterfallAttribute { Title = o.Key, Value = o.Sum(j => j.CostPlanImprovement) });
                }

                //Remainign
                var groupPIPSNotRealize = PIPs.Where(z => z.Realized == false).GroupBy(d => d.ToBsonDocument().GetString(GroupBy));
                foreach (var n in groupPIPSNotRealize.ToList())
                {
                    if (DayOrCost.Equals("Day"))
                        res.RealiseDuringOperations.Add(new WaterfallAttribute { Title = n.Key, Value = n.Sum(j => j.DaysPlanImprovement) });
                    else
                        res.RealiseDuringOperations.Add(new WaterfallAttribute { Title = n.Key, Value = n.Sum(j => j.CostPlanImprovement) });
                }
            }
            return res;
        }
    }

    /// <summary>
    /// PIP Analysis - Type 7: Cummulative cost saving analysis  
    /// </summary>
    public class WaterfallAnalysis
    {
        public double Delta { get; set; }
        public List<WaterfallAnalysisAttribute> Attributes { get; set; }

        public WaterfallAnalysis()
        {
            this.Attributes = new List<WaterfallAnalysisAttribute>();
        }

        public static WaterfallAnalysis LoadData(
                string GroupBy,
                string WellName,
                string SequenceId,
                string ActivityType,
                List<PIPElement> PIPs = null)
        {

            var wellpip = WellPIP.Populate<WellPIP>(Query.And(Query.EQ("WellName", WellName),
                 Query.EQ("SequenceId", SequenceId),
                  Query.EQ("ActivityType", ActivityType)
                ));

            if (wellpip == null) throw new Exception("No well plan defined for this activity, Waterfall can't be generated");

            var singleData = wellpip.FirstOrDefault();
            var division = 1000000.0;

            if (singleData.Elements == null || singleData.Elements.Count <= 0) throw new Exception("No PIP Elements defined for this PIP, Chart can't be generated");

            List<EachMonthCalc> results = new List<EachMonthCalc>();

            foreach (var gbClas in singleData.Elements.GroupBy(x => x.ToBsonDocument().GetString(GroupBy)))
            {
                foreach (var data in gbClas.ToList())
                {
                    var periodCals = DateRangeToMonth.GetListDayOfMonths(data.Period);
                    var costPlanImprovement = data.CostPlanImprovement;

                    var totalNumberDays = periodCals.Sum(y => y.Value);

                    double previousCost = 0;

                    foreach (var cal in periodCals.OrderBy(x => x.Key))
                    {
                        var valuePerMonth = (cal.Value / totalNumberDays); // percentage value each month
                        // Cost
                        var costValuePerMonth = (valuePerMonth * costPlanImprovement) * division;
                        if (costValuePerMonth < 0)
                        {
                            costValuePerMonth = costValuePerMonth * -1;
                        }

                        previousCost = costValuePerMonth + previousCost;
                        EachMonthCalc calc = new EachMonthCalc();
                        calc.ElementId = data.ElementId;
                        calc.MonthId = cal.Key;
                        calc.Cost = previousCost;
                        calc.Title = gbClas.Key;
                        results.Add(calc);
                    }
                }
            }

            var groupbyMOnth = results.GroupBy(x => x.MonthId);

            List<WaterfallAnalysisAttribute> resTotal = new List<WaterfallAnalysisAttribute>();

            foreach (var r in groupbyMOnth)
            {
                WaterfallAnalysisAttribute wf = new WaterfallAnalysisAttribute();
                wf.Axis = DateTime.ParseExact(r.Key, "yyyyMM", System.Globalization.CultureInfo.InvariantCulture);
                wf.Details = new List<WaterfallAttribute>();

                foreach (var cls in r.ToList().GroupBy(x => x.Title))
                {
                    var sumVal = cls.Sum(x => x.Cost);
                    WaterfallAttribute hO = new WaterfallAttribute();
                    hO.Title = cls.Key;
                    hO.Value = sumVal;

                    wf.Details.Add(hO);
                }
                resTotal.Add(wf);

            }
            return new WaterfallAnalysis { Attributes = resTotal, Delta = 0 };
        }
    }

    /// <summary>
    /// Detail attribute class for WaterfallAnalysis
    /// </summary>
    public class WaterfallAnalysisAttribute
    {
        public DateTime Axis { get; set; }
        public List<WaterfallAttribute> Details { get; set; }
        public WaterfallAnalysisAttribute()
        {
            this.Details = new List<WaterfallAttribute>();
        }

    }

    /// <summary>
    /// Helper class for Calculate, Split DateRange to Separated Days of Month
    /// </summary>
    public class EachMonthCalc
    {
        public int ElementId { get; set; }
        public string MonthId { get; set; }
        public double Cost { get; set; }
        public string Title { get; set; }
    }

    /// <summary>
    /// Detail attribute class for global Waterfall class
    /// </summary>
    public class WaterfallAttribute
    {
        public string Title { get; set; }
        public double Value { get; set; }
    }


    public class WaterfallHelper
    {
        public WellDrillData Bottom { get; set; }
        public WellDrillData Plan { get; set; }

        public WellDrillData RealizedCompetitiveScopeOppNeg { get; set; }
        public WellDrillData RealizedSupplyChainTransformationOppNeg { get; set; }
        public WellDrillData RealizedEfficientExecutionOppNeg { get; set; }
        public WellDrillData RealizedTechnologyandInnovationOppNeg { get; set; }

        public WellDrillData RealizedCompetitiveScopeOppPos { get; set; }
        public WellDrillData RealizedSupplyChainTransformationOppPos { get; set; }
        public WellDrillData RealizedEfficientExecutionOppPos { get; set; }
        public WellDrillData RealizedTechnologyandInnovationOppPos { get; set; }

        public WellDrillData RealizedCompetitiveScopeRiskPos { get; set; }
        public WellDrillData RealizedSupplyChainTransformationRiskPos { get; set; }
        public WellDrillData RealizedEfficientExecutionRiskPos { get; set; }
        public WellDrillData RealizedTechnologyandInnovationRiskPos { get; set; }

        public WellDrillData RealizedCompetitiveScopeRiskNeg { get; set; }
        public WellDrillData RealizedSupplyChainTransformationRiskNeg { get; set; }
        public WellDrillData RealizedEfficientExecutionRiskNeg { get; set; }
        public WellDrillData RealizedTechnologyandInnovationRiskNeg { get; set; }



        public WellDrillData ADSCompetitiveScopeNeg { get; set; }
        public WellDrillData ADSSupplyChainTransformationNeg { get; set; }
        public WellDrillData ADSEfficientExecutionNeg { get; set; }
        public WellDrillData ADSTechnologyandInnovationNeg { get; set; }

        public WellDrillData ADSCompetitiveScopePos { get; set; }
        public WellDrillData ADSSupplyChainTransformationPos { get; set; }
        public WellDrillData ADSEfficientExecutionPos { get; set; }
        public WellDrillData ADSTechnologyandInnovationPos { get; set; }

        public WellDrillData LE { get; set; }

        public WellDrillData UnrealizedCompetitiveScopeOppNeg { get; set; }
        public WellDrillData UnrealizedSupplyChainTransformationOppNeg { get; set; }
        public WellDrillData UnrealizedEfficientExecutionOppNeg { get; set; }
        public WellDrillData UnrealizedTechnologyandInnovationOppNeg { get; set; }

        public WellDrillData UnrealizedCompetitiveScopeOppPos { get; set; }
        public WellDrillData UnrealizedSupplyChainTransformationOppPos { get; set; }
        public WellDrillData UnrealizedEfficientExecutionOppPos { get; set; }
        public WellDrillData UnrealizedTechnologyandInnovationOppPos { get; set; }

        public WellDrillData UnrealizedCompetitiveScopeRiskPos { get; set; }
        public WellDrillData UnrealizedSupplyChainTransformationRiskPos { get; set; }
        public WellDrillData UnrealizedEfficientExecutionRiskPos { get; set; }
        public WellDrillData UnrealizedTechnologyandInnovationRiskPos { get; set; }

        public WellDrillData UnrealizedCompetitiveScopeRiskNeg { get; set; }
        public WellDrillData UnrealizedSupplyChainTransformationRiskNeg { get; set; }
        public WellDrillData UnrealizedEfficientExecutionRiskNeg { get; set; }
        public WellDrillData UnrealizedTechnologyandInnovationRiskNeg { get; set; }

        public WellDrillData UnriskedUpside { get; set; }
        public WellDrillData TOP { get; set; }

        public WaterfallHelper()
        {
            Bottom = new WellDrillData(); Bottom.Identifier = "Bottom";
            Plan = new WellDrillData();

            RealizedCompetitiveScopeOppNeg = new WellDrillData();
            RealizedSupplyChainTransformationOppNeg = new WellDrillData();
            RealizedEfficientExecutionOppNeg = new WellDrillData();
            RealizedTechnologyandInnovationOppNeg = new WellDrillData();

            RealizedCompetitiveScopeOppPos = new WellDrillData();
            RealizedSupplyChainTransformationOppPos = new WellDrillData();
            RealizedEfficientExecutionOppPos = new WellDrillData();
            RealizedTechnologyandInnovationOppPos = new WellDrillData();

            RealizedCompetitiveScopeRiskPos = new WellDrillData();
            RealizedSupplyChainTransformationRiskPos = new WellDrillData();
            RealizedEfficientExecutionRiskPos = new WellDrillData();
            RealizedTechnologyandInnovationRiskPos = new WellDrillData();

            RealizedCompetitiveScopeRiskNeg = new WellDrillData();
            RealizedSupplyChainTransformationRiskNeg = new WellDrillData();
            RealizedEfficientExecutionRiskNeg = new WellDrillData();
            RealizedTechnologyandInnovationRiskNeg = new WellDrillData();


            ADSCompetitiveScopeNeg = new WellDrillData();
            ADSSupplyChainTransformationNeg = new WellDrillData();
            ADSEfficientExecutionNeg = new WellDrillData();
            ADSTechnologyandInnovationNeg = new WellDrillData();

            ADSCompetitiveScopePos = new WellDrillData();
            ADSSupplyChainTransformationPos = new WellDrillData();
            ADSEfficientExecutionPos = new WellDrillData();
            ADSTechnologyandInnovationPos = new WellDrillData();

            LE = new WellDrillData();

            UnrealizedCompetitiveScopeOppNeg = new WellDrillData();
            UnrealizedSupplyChainTransformationOppNeg = new WellDrillData();
            UnrealizedEfficientExecutionOppNeg = new WellDrillData();
            UnrealizedTechnologyandInnovationOppNeg = new WellDrillData();

            UnrealizedCompetitiveScopeOppPos = new WellDrillData();
            UnrealizedSupplyChainTransformationOppPos = new WellDrillData();
            UnrealizedEfficientExecutionOppPos = new WellDrillData();
            UnrealizedTechnologyandInnovationOppPos = new WellDrillData();

            UnrealizedCompetitiveScopeRiskPos = new WellDrillData();
            UnrealizedSupplyChainTransformationRiskPos = new WellDrillData();
            UnrealizedEfficientExecutionRiskPos = new WellDrillData();
            UnrealizedTechnologyandInnovationRiskPos = new WellDrillData();

            UnrealizedCompetitiveScopeRiskNeg = new WellDrillData();
            UnrealizedSupplyChainTransformationRiskNeg = new WellDrillData();
            UnrealizedEfficientExecutionRiskNeg = new WellDrillData();
            UnrealizedTechnologyandInnovationRiskNeg = new WellDrillData();

            UnriskedUpside = new WellDrillData();
            TOP = new WellDrillData();
            TOP.Identifier = "TOP";
            Bottom.Identifier = "Bottom";
            Plan.Identifier = "Plan";

            RealizedCompetitiveScopeOppNeg.Identifier = "Realized Competitive Scope Opp Neg";
            RealizedSupplyChainTransformationOppNeg.Identifier = "Realized Supply Chain Transformation Opp Neg";
            RealizedEfficientExecutionOppNeg.Identifier = "Realized Efficient Execution Opp Neg";
            RealizedTechnologyandInnovationOppNeg.Identifier = "Realized Technology and Innovation Opp Neg";
            RealizedCompetitiveScopeRiskNeg.Identifier = "Realized Competitive Scope Risk Neg";
            RealizedSupplyChainTransformationRiskNeg.Identifier = "Realized Supply Chain Transformation Risk Neg";
            RealizedEfficientExecutionRiskNeg.Identifier = "Realized Efficient Execution Risk Neg";
            RealizedTechnologyandInnovationRiskNeg.Identifier = "Realized Technology and Innovation Risk Neg";
            ADSCompetitiveScopeNeg.Identifier = "ADS Competitive Scope Neg";
            ADSSupplyChainTransformationNeg.Identifier = "ADS Supply Chain Transformation Neg";
            ADSEfficientExecutionNeg.Identifier = "ADS Efficient Execution Neg";
            ADSTechnologyandInnovationNeg.Identifier = "ADS Technology and Innovation Neg";


            RealizedCompetitiveScopeOppPos.Identifier = "Realized Competitive Scope Opp Pos";
            RealizedSupplyChainTransformationOppPos.Identifier = "Realized Supply Chain Transformation Opp Pos";
            RealizedEfficientExecutionOppPos.Identifier = "Realized Efficient Execution Opp Pos";
            RealizedTechnologyandInnovationOppPos.Identifier = "Realized Technology and Innovation Opp Pos";
            RealizedCompetitiveScopeRiskPos.Identifier = "Realized Competitive Scope Risk Pos";
            RealizedSupplyChainTransformationRiskPos.Identifier = "Realized Supply Chain Transformation Risk Pos";
            RealizedEfficientExecutionRiskPos.Identifier = "Realized Efficient Execution Risk Pos";
            RealizedTechnologyandInnovationRiskPos.Identifier = "Realized Technology and Innovation Risk Pos";
            ADSCompetitiveScopePos.Identifier = "ADS Competitive Scope Pos";
            ADSSupplyChainTransformationPos.Identifier = "ADS Supply Chain Transformation Pos";
            ADSEfficientExecutionPos.Identifier = "ADS Efficient Execution Pos";
            ADSTechnologyandInnovationPos.Identifier = "ADS Technology and Innovation Pos";

            LE.Identifier = "LE";

            UnrealizedCompetitiveScopeOppNeg.Identifier = "Unrealized Competitive Scope Opp Neg";
            UnrealizedSupplyChainTransformationOppNeg.Identifier = "Unrealized Supply Chain Transformation Opp Neg";
            UnrealizedEfficientExecutionOppNeg.Identifier = "Unrealized Efficient Execution Opp Neg";
            UnrealizedTechnologyandInnovationOppNeg.Identifier = "Unrealized Technology and Innovation Opp Neg";
            UnrealizedCompetitiveScopeRiskNeg.Identifier = "Unrealized Competitive Scope Risk Neg";
            UnrealizedSupplyChainTransformationRiskNeg.Identifier = "Unrealized Supply Chain Transformation Risk Neg";
            UnrealizedEfficientExecutionRiskNeg.Identifier = "nrealized Efficient Execution Risk Neg";
            UnrealizedTechnologyandInnovationRiskNeg.Identifier = "Unrealized Technology and Innovation Risk Neg";


            UnrealizedCompetitiveScopeOppPos.Identifier = "Unrealized Competitive Scope Opp Pos";
            UnrealizedSupplyChainTransformationOppPos.Identifier = "Unrealized Supply Chain Transformation Opp Pos";
            UnrealizedEfficientExecutionOppPos.Identifier = "Unrealized Efficient Execution Opp Pos";
            UnrealizedTechnologyandInnovationOppPos.Identifier = "Unrealized Technology and Innovation Opp Pos";
            UnrealizedCompetitiveScopeRiskPos.Identifier = "Unrealized Competitive Scope Risk Pos";
            UnrealizedSupplyChainTransformationRiskPos.Identifier = "Unrealized Supply Chain Transformation Risk Pos";
            UnrealizedEfficientExecutionRiskPos.Identifier = "nrealized Efficient Execution Risk Pos";
            UnrealizedTechnologyandInnovationRiskPos.Identifier = "Unrealized Technology and Innovation Risk Pos";


            UnriskedUpside.Identifier = "Unrisked Upside";
        }

        #region Array stack
        //var arrays = new string[] {
        //        "Bottom", 
        //        "Plan", 

        //        "Realized Competitive Scope Opp",
        //        "Realized Supply Chain Transformation Opp",
        //        "Realized Efficient Execution Opp",
        //        "Realized Technology and Innovation Opp",

        //        "Realized Competitive Scope Risk",
        //        "Realized Supply Chain Transformation Risk",
        //        "Realized Efficient Execution Risk",
        //        "Realized Technology and Innovation Risk",

        //        "ADS Competitive Scope",
        //        "ADS Supply Chain Transformation",
        //        "ADS Efficient Execution",
        //        "ADS Technology and Innovation",

        //        "LE",

        //        "Unrealized Competitive Scope Opp",
        //        "Unrealized Supply Chain Transformation Opp",
        //        "Unrealized Efficient Execution Opp",
        //        "Unrealized Technology and Innovation Opp",

        //        "Unrealized Competitive Scope Risk",
        //        "Unrealized Supply Chain Transformation Risk",
        //        "Unrealized Efficient Execution Risk",
        //        "Unrealized Technology and Innovation Risk",

        //        "Unrisked Upside",
        //        "TOP"
        //    };
        #endregion

    }

    public class WaterfallStacked
    {
        public string Title { get; set; }
        public int Order { get; set; }
        public WellDrillData Value { get; set; }
        public WaterfallHelper Stack { get; set; }

        public WaterfallStacked()
        {
            Stack = new WaterfallHelper();
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

        public static List<WaterfallStacked> GetWaterfallData(WellActivityUpdateMonthly wau,
         string DayOrCost = "Day", string BaseView = "OP",
         string GroupBy = "Day", bool IncludeZero = false,
         bool IncludeCR = false, bool ByRealised = false,
            double CalcLEDays = 0, double CalcLECost = 0, bool ShellShare = false)
        {
            //var wellPIPController = new WellPIPController();
            var division = 1000000.0;
            var Target = DayOrCost.Equals("Day") ? wau.CurrentWeek.Days : wau.CurrentWeek.Cost;
            var AFE = DayOrCost.Equals("Day") ? wau.AFE.Days : wau.AFE.Cost;
            var OP = DayOrCost.Equals("Day") ? wau.Plan.Days : wau.Plan.Cost;
            var Start = BaseView == "OP" && wau.Plan.Days != 0 ? OP : AFE;
            var StartTitle = BaseView == "OP" && wau.Plan.Days != 0 ? "OP" : "AFE";


            var curOP = "OP15";
            var wellPlan = WellActivity.Get<WellActivity>(Query.And(
                 Query.EQ("WellName", wau.WellName),
                 Query.EQ("UARigSequenceId", wau.SequenceId)
                 ));
            if (wellPlan != null)
            {
                curOP = wellPlan.Phases.Where(x => x.ActivityType.Equals(wau.Phase.ActivityType)).FirstOrDefault().BaseOP.OrderByDescending(x => x).FirstOrDefault();
            }

            var CurrentOP = Config.GetConfigValue("BaseOPConfig").ToString();


            if (DayOrCost.Equals("Cost"))
            {
                if (Target > 10000)
                {
                    Target /= division;
                }
                if (Start > 10000)
                {
                    Start /= division;
                }
            }

            List<PIPElement> PIPs = new List<PIPElement>();
            WellPIP wpip = WellPIP.GetByWellActivity(wau.WellName, wau.Phase.ActivityType) ?? new WellPIP();
            if (wpip != null) PIPs.AddRange(wpip.Elements);

            
            var WorkingInterest = 1.0;
            if (ShellShare)
            {
                WorkingInterest = wellPlan.WorkingInterest > 1 ? Tools.Div(wellPlan.WorkingInterest, 100) : wellPlan.WorkingInterest;
            }
            var phases = wellPlan.Phases.Where(x => x.ActivityType.Equals(wau.Phase.ActivityType)).FirstOrDefault();

            if(curOP.Equals(CurrentOP))
            {
                phases.Plan.Cost = phases.Plan.Cost * WorkingInterest;
            }
            else
            {
                var y = wellPlan.OPHistories.Where(x=>x.Type.Equals(CurrentOP) && x.ActivityType.Equals(wau.Phase.ActivityType)) ;
                if(y!=null && y.Count()>0)
                {
                    var data = y.FirstOrDefault();
                    phases.Plan.Cost = data.Plan.Cost < 5000 ? data.Plan.Cost * 1000000 : data.Plan.Cost;
                }
            }

            phases.OP.Cost = phases.OP.Cost * WorkingInterest;
            phases.LE.Cost = phases.LE.Cost * WorkingInterest;
            phases.LWE.Cost = phases.LWE.Cost * WorkingInterest;
            phases.AFE.Cost = phases.AFE.Cost * WorkingInterest;

            foreach (var xx in wpip.Elements)
            {
                if (ShellShare)
                {

                    xx.CostCurrentWeekImprovement = xx.CostCurrentWeekImprovement * WorkingInterest;
                    xx.CostCurrentWeekRisk = xx.CostCurrentWeekRisk * WorkingInterest;
                    xx.CostPlanImprovement = xx.CostPlanImprovement * WorkingInterest;
                    xx.CostPlanRisk = xx.CostPlanRisk * WorkingInterest;

                    //xx.CostLastWeekImprovement = xx.CostLastWeekImprovement * WorkingInterest;
                    //xx.CostLastWeekRisk = xx.CostLastWeekRisk * WorkingInterest;
                    //xx.CostActualImprovement = xx.CostActualImprovement * WorkingInterest;
                    //xx.CostActualRisk = xx.CostActualRisk * WorkingInterest;

                }

            }

            if (ShellShare)
            {
                wau.BankedSavingsCompetitiveScope.Cost = wau.BankedSavingsCompetitiveScope.Cost * WorkingInterest;
                wau.BankedSavingsEfficientExecution.Cost = wau.BankedSavingsEfficientExecution.Cost * WorkingInterest;
                wau.BankedSavingsSupplyChainTransformation.Cost = wau.BankedSavingsSupplyChainTransformation.Cost *
                                                                  WorkingInterest;
                wau.BankedSavingsTechnologyAndInnovation.Cost = wau.BankedSavingsTechnologyAndInnovation.Cost *
                                                                WorkingInterest;

                wau.RealizedPIPElemCompetitiveScope.Cost = wau.RealizedPIPElemCompetitiveScope.Cost * WorkingInterest;
                wau.RealizedPIPElemEfficientExecution.Cost = wau.RealizedPIPElemEfficientExecution.Cost * WorkingInterest;
                wau.RealizedPIPElemSupplyChainTransformation.Cost = wau.RealizedPIPElemSupplyChainTransformation.Cost *
                                                                    WorkingInterest;
                wau.RealizedPIPElemTechnologyAndInnovation.Cost = wau.RealizedPIPElemTechnologyAndInnovation.Cost *
                                                                  WorkingInterest;
            }

            List<WaterfallStacked> results = new List<WaterfallStacked>();

            string previousOP = "";
            string nextOP = "";
            var DefaultOP = getBaseOP(out previousOP, out nextOP);

            #region Array Bar
            var arraybar = new string[] {
                "OP-15", 
                "Realized PIPs Opp Neg",
                "Realized PIPs Opp Pos",
                "Realized PIPs Risk Pos",
                "Realized PIPs Risk Neg",
                "Additional Banked Saving Risk Neg",
                "Additional Banked Saving Risk Pos",
                "LE",
                "Unrealized PIPs Opp Neg",
                "Unrealized PIPs Opp Pos",
                "Unrealized PIPs Risk Pos",
                "Unrealized PIPs Risk Neg",
                "Unrisked Upside",
            };
            #endregion

            #region populate value

            var tot = wpip.Elements.Where(x => x.Completion.Equals("Realized")).Select(x => new WellDrillData { Days = x.DaysPlanImprovement, Cost = x.CostPlanImprovement });
            WellDrillData totRealizedPIP = new WellDrillData();
            totRealizedPIP.Cost = tot.Select(x => x.Cost).Sum();
            totRealizedPIP.Days = tot.Select(x => x.Days).Sum();

            WellDrillData actualLE = new WellDrillData();
            actualLE = phases.LE;

            ///cal
            var leDays = 0.00;
            var leCost = 0.00;

            var totalBankDays = wau.BankedSavingsCompetitiveScope.Days +
                                wau.BankedSavingsEfficientExecution.Days +
                                wau.BankedSavingsSupplyChainTransformation.Days +
                                wau.BankedSavingsTechnologyAndInnovation.Days;
            var totalBankCost = wau.BankedSavingsCompetitiveScope.Cost +
                                wau.BankedSavingsEfficientExecution.Cost +
                                wau.BankedSavingsSupplyChainTransformation.Cost +
                                wau.BankedSavingsTechnologyAndInnovation.Cost;



            var totalRealizeDays = wau.RealizedPIPElemCompetitiveScope.Days +
                                wau.RealizedPIPElemEfficientExecution.Days +
                                wau.RealizedPIPElemSupplyChainTransformation.Days +
                                wau.RealizedPIPElemTechnologyAndInnovation.Days;
            var totalRealizeCost = wau.RealizedPIPElemCompetitiveScope.Cost +
                                wau.RealizedPIPElemEfficientExecution.Cost +
                                wau.RealizedPIPElemSupplyChainTransformation.Cost +
                                wau.RealizedPIPElemTechnologyAndInnovation.Cost;

            WellDrillData calcLE = new WellDrillData();
            calcLE.Cost = Tools.Div(phases.LE.Cost, 1000000) + totRealizedPIP.Cost;
            calcLE.Days = phases.LE.Days + totRealizedPIP.Days;


            WellDrillData plan = new WellDrillData();
            plan.Cost = Tools.Div(phases.Plan.Cost, 1000000);


            if (curOP.Equals(CurrentOP))
            {
                phases.Plan.Days = phases.Plan.Days;// *WorkingInterest;
                plan.Days = phases.Plan.Days;
            }
            else
            {
                var y = wellPlan.OPHistories.Where(x => x.Type.Equals(CurrentOP) && x.ActivityType.Equals(wau.Phase.ActivityType));
                if (y != null && y.Count() > 0)
                {
                    var data = y.FirstOrDefault();
                    phases.Plan.Days = data.Plan.Days;
                    plan.Days = phases.Plan.Days;
                }
            }

            //plan.Days = phases.Plan.Days;

            WellDrillData totalElementBankedReal = new WellDrillData();
            totalElementBankedReal.Cost = totalBankCost + totalRealizeCost;
            totalElementBankedReal.Days = totalBankDays + totalRealizeDays;

            //
            var curWeek = wau.CurrentWeek;
            var pl = wau.Plan;
            pl.Cost = Tools.Div(pl.Cost, 1000000);
            curWeek.Cost = Tools.Div(curWeek.Cost, 1000000);
            var _calcDeltaCost = (curWeek.Cost - pl.Cost) - totalElementBankedReal.Cost;
            var _calcDeltaDays = (curWeek.Days - pl.Days) - totalElementBankedReal.Days;

            var LEFinalCost = curWeek.Cost - _calcDeltaCost;
            var LEFinalDays = curWeek.Days - _calcDeltaDays;

            WellDrillData LEFinal = new WellDrillData();
            LEFinal.Days = CalcLEDays; // LEFinalDays;
            LEFinal.Cost = CalcLECost;

            #region PIP Realized Neg
            var realOPPEffiNeg = wpip.Elements.Where(x => x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Efficient Execution"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekImprovement <= 0).Sum(h => h.CostCurrentWeekImprovement),
                        Days = o.Where(x => x.DaysCurrentWeekImprovement <= 0).Sum(h => h.DaysCurrentWeekImprovement)
                    }).ToList();

            var realOPPSupNeg = wpip.Elements.Where(x => x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Supply Chain Transformation"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekImprovement <= 0).Sum(h => h.CostCurrentWeekImprovement),
                        Days = o.Where(x => x.DaysCurrentWeekImprovement <= 0).Sum(h => h.DaysCurrentWeekImprovement)
                    }).ToList();

            var realOPPComNeg = wpip.Elements.Where(x => x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Competitive Scope"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekImprovement <= 0).Sum(h => h.CostCurrentWeekImprovement),
                        Days = o.Where(x => x.DaysCurrentWeekImprovement <= 0).Sum(h => h.DaysCurrentWeekImprovement)
                    }).ToList();


            var realOPPTechNeg = wpip.Elements.Where(x => x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
               .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Technology and Innovation"))
               .Select(o =>
                   new WellDrillData
                   {
                       Cost = o.Where(x => x.CostCurrentWeekImprovement <= 0).Sum(h => h.CostCurrentWeekImprovement),
                       Days = o.Where(x => x.DaysCurrentWeekImprovement <= 0).Sum(h => h.DaysCurrentWeekImprovement)
                   }).ToList();
            #endregion

            #region PIP Realized Pos
            var realOPPEffiPos = wpip.Elements.Where(x => x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Efficient Execution"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekImprovement > 0).Sum(h => h.CostCurrentWeekImprovement),
                        Days = o.Where(x => x.DaysCurrentWeekImprovement > 0).Sum(h => h.DaysCurrentWeekImprovement)
                    }).ToList();

            var realOPPSupPos = wpip.Elements.Where(x => x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Supply Chain Transformation"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekImprovement > 0).Sum(h => h.CostCurrentWeekImprovement),
                        Days = o.Where(x => x.DaysCurrentWeekImprovement > 0).Sum(h => h.DaysCurrentWeekImprovement)
                    }).ToList();

            var realOPPComPos = wpip.Elements.Where(x => x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Competitive Scope"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekImprovement > 0).Sum(h => h.CostCurrentWeekImprovement),
                        Days = o.Where(x => x.DaysCurrentWeekImprovement > 0).Sum(h => h.DaysCurrentWeekImprovement)
                    }).ToList();


            var realOPPTechPos = wpip.Elements.Where(x => x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
               .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Technology and Innovation"))
               .Select(o =>
                   new WellDrillData
                   {
                       Cost = o.Where(x => x.CostCurrentWeekImprovement > 0).Sum(h => h.CostCurrentWeekImprovement),
                       Days = o.Where(x => x.DaysCurrentWeekImprovement > 0).Sum(h => h.DaysCurrentWeekImprovement)
                   }).ToList();
            #endregion


            #region PIP Realized RISK Pos
            var realRISKEffiPos = wpip.Elements.Where(x => x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Efficient Execution"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekRisk > 0).Sum(h => h.CostCurrentWeekRisk),
                        Days = o.Where(x => x.DaysCurrentWeekRisk > 0).Sum(h => h.DaysCurrentWeekRisk)
                    }).ToList();
            var realRISKSupPos = wpip.Elements.Where(x => x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Supply Chain Transformation"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekRisk > 0).Sum(h => h.CostCurrentWeekRisk),
                        Days = o.Where(x => x.DaysCurrentWeekRisk > 0).Sum(h => h.DaysCurrentWeekRisk)
                    }).ToList();
            var realRISKComPos = wpip.Elements.Where(x => x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Competitive Scope"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekRisk > 0).Sum(h => h.CostCurrentWeekRisk),
                        Days = o.Where(x => x.DaysCurrentWeekRisk > 0).Sum(h => h.DaysCurrentWeekRisk)
                    }).ToList();
            var realRISKTechPos = wpip.Elements.Where(x => x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
               .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Technology and Innovation"))
               .Select(o =>
                   new WellDrillData
                   {
                       Cost = o.Where(x => x.CostCurrentWeekRisk > 0).Sum(h => h.CostCurrentWeekRisk),
                       Days = o.Where(x => x.DaysCurrentWeekRisk > 0).Sum(h => h.DaysCurrentWeekRisk)
                   }).ToList();

            #endregion

            #region PIP Realized RISK Neg
            var realRISKEffiNeg = wpip.Elements.Where(x => x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Efficient Execution"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekRisk <= 0).Sum(h => h.CostCurrentWeekRisk),
                        Days = o.Where(x => x.DaysCurrentWeekRisk <= 0).Sum(h => h.DaysCurrentWeekRisk)
                    }).ToList();
            var realRISKSupNeg = wpip.Elements.Where(x => x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Supply Chain Transformation"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekRisk <= 0).Sum(h => h.CostCurrentWeekRisk),
                        Days = o.Where(x => x.DaysCurrentWeekRisk <= 0).Sum(h => h.DaysCurrentWeekRisk)
                    }).ToList();
            var realRISKComNeg = wpip.Elements.Where(x => x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Competitive Scope"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekRisk <= 0).Sum(h => h.CostCurrentWeekRisk),
                        Days = o.Where(x => x.DaysCurrentWeekRisk <= 0).Sum(h => h.DaysCurrentWeekRisk)
                    }).ToList();
            var realRISKTechNeg = wpip.Elements.Where(x => x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
               .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Technology and Innovation"))
               .Select(o =>
                   new WellDrillData
                   {
                       Cost = o.Where(x => x.CostCurrentWeekRisk <= 0).Sum(h => h.CostCurrentWeekRisk),
                       Days = o.Where(x => x.DaysCurrentWeekRisk <= 0).Sum(h => h.DaysCurrentWeekRisk)
                   }).ToList();

            #endregion


            #region ADB

            var adbEffiCostPos = wau.BankedSavingsEfficientExecution.Cost >= 0 ? wau.BankedSavingsEfficientExecution.Cost : 0;
            var adbEffiDaysPos = wau.BankedSavingsEfficientExecution.Days >= 0 ? wau.BankedSavingsEfficientExecution.Days : 0;
            var adbComCostPos = wau.BankedSavingsCompetitiveScope.Cost >= 0 ? wau.BankedSavingsCompetitiveScope.Cost : 0;
            var adbComDaysPos = wau.BankedSavingsCompetitiveScope.Days >= 0 ? wau.BankedSavingsCompetitiveScope.Days : 0;
            var adbSupCostPos = wau.BankedSavingsSupplyChainTransformation.Cost >= 0 ? wau.BankedSavingsSupplyChainTransformation.Cost : 0;
            var adbSupDaysPos = wau.BankedSavingsSupplyChainTransformation.Days >= 0 ? wau.BankedSavingsSupplyChainTransformation.Days : 0;
            var adbTecCostPos = wau.BankedSavingsTechnologyAndInnovation.Cost >= 0 ? wau.BankedSavingsTechnologyAndInnovation.Cost : 0;
            var adbTecDaysPos = wau.BankedSavingsTechnologyAndInnovation.Days >= 0 ? wau.BankedSavingsTechnologyAndInnovation.Days : 0;

            var adbEffiCostNeg = wau.BankedSavingsEfficientExecution.Cost < 0 ? wau.BankedSavingsEfficientExecution.Cost : 0;
            var adbEffiDaysNeg = wau.BankedSavingsEfficientExecution.Days < 0 ? wau.BankedSavingsEfficientExecution.Days : 0;
            var adbComCostNeg = wau.BankedSavingsCompetitiveScope.Cost < 0 ? wau.BankedSavingsCompetitiveScope.Cost : 0;
            var adbComDaysNeg = wau.BankedSavingsCompetitiveScope.Days < 0 ? wau.BankedSavingsCompetitiveScope.Days : 0;
            var adbSupCostNeg = wau.BankedSavingsSupplyChainTransformation.Cost < 0 ? wau.BankedSavingsSupplyChainTransformation.Cost : 0;
            var adbSupDaysNeg = wau.BankedSavingsSupplyChainTransformation.Days < 0 ? wau.BankedSavingsSupplyChainTransformation.Days : 0;
            var adbTecCostNeg = wau.BankedSavingsTechnologyAndInnovation.Cost < 0 ? wau.BankedSavingsTechnologyAndInnovation.Cost : 0;
            var adbTecDaysNeg = wau.BankedSavingsTechnologyAndInnovation.Days < 0 ? wau.BankedSavingsTechnologyAndInnovation.Days : 0;
            #endregion

            // bar 4
            var LE = phases.LE;

            #region Unrealized OPP Neg
            var unrealOPPEffiNeg = wpip.Elements.Where(x => !x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Efficient Execution"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekImprovement <= 0).Sum(h => h.CostCurrentWeekImprovement),
                        Days = o.Where(x => x.DaysCurrentWeekImprovement <= 0).Sum(h => h.DaysCurrentWeekImprovement)
                    }).ToList();
            var unrealOPPSupNeg = wpip.Elements.Where(x => !x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Supply Chain Transformation"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekImprovement <= 0).Sum(h => h.CostCurrentWeekImprovement),
                        Days = o.Where(x => x.DaysCurrentWeekImprovement <= 0).Sum(h => h.DaysCurrentWeekImprovement)
                    }).ToList();
            var unrealOPPComNeg = wpip.Elements.Where(x => !x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Competitive Scope"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekImprovement <= 0).Sum(h => h.CostCurrentWeekImprovement),
                        Days = o.Where(x => x.DaysCurrentWeekImprovement <= 0).Sum(h => h.DaysCurrentWeekImprovement)
                    }).ToList();
            var unrealOPPTechNeg = wpip.Elements.Where(x => !x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
               .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Technology and Innovation"))
               .Select(o =>
                   new WellDrillData
                   {
                       Cost = o.Where(x => x.CostCurrentWeekImprovement <= 0).Sum(h => h.CostCurrentWeekImprovement),
                       Days = o.Where(x => x.DaysCurrentWeekImprovement <= 0).Sum(h => h.DaysCurrentWeekImprovement)
                   }).ToList();

            #endregion


            #region Unrealized OPP Pos
            var unrealOPPEffiPos = wpip.Elements.Where(x => !x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Efficient Execution"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekImprovement > 0).Sum(h => h.CostCurrentWeekImprovement),
                        Days = o.Where(x => x.DaysCurrentWeekImprovement > 0).Sum(h => h.DaysCurrentWeekImprovement)
                    }).ToList();
            var unrealOPPSupPos = wpip.Elements.Where(x => !x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Supply Chain Transformation"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekImprovement > 0).Sum(h => h.CostCurrentWeekImprovement),
                        Days = o.Where(x => x.DaysCurrentWeekImprovement > 0).Sum(h => h.DaysCurrentWeekImprovement)
                    }).ToList();
            var unrealOPPComPos = wpip.Elements.Where(x => !x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Competitive Scope"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekImprovement > 0).Sum(h => h.CostCurrentWeekImprovement),
                        Days = o.Where(x => x.DaysCurrentWeekImprovement > 0).Sum(h => h.DaysCurrentWeekImprovement)
                    }).ToList();
            var unrealOPPTechPos = wpip.Elements.Where(x => !x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
               .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Technology and Innovation"))
               .Select(o =>
                   new WellDrillData
                   {
                       Cost = o.Where(x => x.CostCurrentWeekImprovement > 0).Sum(h => h.CostCurrentWeekImprovement),
                       Days = o.Where(x => x.DaysCurrentWeekImprovement > 0).Sum(h => h.DaysCurrentWeekImprovement)
                   }).ToList();

            #endregion


            #region Unrealized PIP Risk Pos
            var unrealRISKEffiPos = wpip.Elements.Where(x => !x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Efficient Execution"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekRisk >= 0).Sum(h => h.CostCurrentWeekRisk),
                        Days = o.Where(x => x.DaysCurrentWeekRisk >= 0).Sum(h => h.DaysCurrentWeekRisk)
                    }).ToList();
            var unrealRISKSupPos = wpip.Elements.Where(x => !x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Supply Chain Transformation"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekRisk >= 0).Sum(h => h.CostCurrentWeekRisk),
                        Days = o.Where(x => x.DaysCurrentWeekRisk >= 0).Sum(h => h.DaysCurrentWeekRisk)
                    }).ToList();
            var unrealRISKComPos = wpip.Elements.Where(x => !x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Competitive Scope"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekRisk >= 0).Sum(h => h.CostCurrentWeekRisk),
                        Days = o.Where(x => x.DaysCurrentWeekRisk >= 0).Sum(h => h.DaysCurrentWeekRisk)
                    }).ToList();
            var unrealRISKTechPos = wpip.Elements.Where(x => !x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
               .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Technology and Innovation"))
               .Select(o =>
                   new WellDrillData
                   {
                       Cost = o.Where(x => x.CostCurrentWeekRisk >= 0).Sum(h => h.CostCurrentWeekRisk),
                       Days = o.Where(x => x.DaysCurrentWeekRisk >= 0).Sum(h => h.DaysCurrentWeekRisk)
                   }).ToList();

            #endregion


            #region Unrealized PIP Risk Neg
            var unrealRISKEffiNeg = wpip.Elements.Where(x => !x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Efficient Execution"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekRisk < 0).Sum(h => h.CostCurrentWeekRisk),
                        Days = o.Where(x => x.DaysCurrentWeekRisk < 0).Sum(h => h.DaysCurrentWeekRisk)
                    }).ToList();
            var unrealRISKSupNeg = wpip.Elements.Where(x => !x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Supply Chain Transformation"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekRisk < 0).Sum(h => h.CostCurrentWeekRisk),
                        Days = o.Where(x => x.DaysCurrentWeekRisk < 0).Sum(h => h.DaysCurrentWeekRisk)
                    }).ToList();
            var unrealRISKComNeg = wpip.Elements.Where(x => !x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
                .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Competitive Scope"))
                .Select(o =>
                    new WellDrillData
                    {
                        Cost = o.Where(x => x.CostCurrentWeekRisk < 0).Sum(h => h.CostCurrentWeekRisk),
                        Days = o.Where(x => x.DaysCurrentWeekRisk < 0).Sum(h => h.DaysCurrentWeekRisk)
                    }).ToList();
            var unrealRISKTechNeg = wpip.Elements.Where(x => !x.Completion.Equals("Realized") && x.AssignTOOps.Contains(DefaultOP))
               .GroupBy(x => x.Classification).Where(x => x.Key.Equals("Technology and Innovation"))
               .Select(o =>
                   new WellDrillData
                   {
                       Cost = o.Where(x => x.CostCurrentWeekRisk < 0).Sum(h => h.CostCurrentWeekRisk),
                       Days = o.Where(x => x.DaysCurrentWeekRisk < 0).Sum(h => h.DaysCurrentWeekRisk)
                   }).ToList();

            #endregion

            #endregion

            WellDrillData sumRealOppNeg = new WellDrillData();
            WellDrillData sumRealOppPos = new WellDrillData();


            WellDrillData sumRealRiskNeg = new WellDrillData();
            WellDrillData sumRealRiskPos = new WellDrillData();

            WellDrillData sumRealRiskPlusBottom = new WellDrillData();

            WellDrillData sumAdbPos = new WellDrillData();
            WellDrillData sumAdbNeg = new WellDrillData();



            WellDrillData sumUnRealOppPos = new WellDrillData();
            WellDrillData sumUnRealOppNeg = new WellDrillData();

            WellDrillData sumunRealRiskPos = new WellDrillData();
            WellDrillData sumunRealRiskNeg = new WellDrillData();


            WellDrillData sumUnRealOppBottom = new WellDrillData();
            WellDrillData sumunRealRiskPlusBottom = new WellDrillData();

            var bottomCost = 0.0;
            var bottomDays = 0.0;
            foreach (var arr in arraybar)
            {
                WaterfallStacked barOp = new WaterfallStacked();
                barOp.Title = arr.ToString();
                if (arr == "Realized PIPs Risk Neg")
                {
                    var g = "";
                }
                switch (arr)
                {
                    case "OP-15":
                        {
                            barOp.Stack.Plan = plan;
                            bottomCost = plan.Cost;
                            bottomDays = plan.Days;
                            break;
                        }
                    case "Realized PIPs Opp Neg":
                        {
                            #region Realized PIPs Opp Neg
                            barOp.Stack.RealizedCompetitiveScopeOppNeg = realOPPComNeg.Count == 0 ? new WellDrillData() : realOPPComNeg.FirstOrDefault();
                            barOp.Stack.RealizedEfficientExecutionOppNeg = realOPPEffiNeg.Count == 0 ? new WellDrillData() : realOPPEffiNeg.FirstOrDefault();
                            barOp.Stack.RealizedSupplyChainTransformationOppNeg = realOPPSupNeg.Count == 0 ? new WellDrillData() : realOPPSupNeg.FirstOrDefault();
                            barOp.Stack.RealizedTechnologyandInnovationOppNeg = realOPPTechNeg.Count == 0 ? new WellDrillData() : realOPPTechNeg.FirstOrDefault();

                            sumRealOppNeg.Cost = barOp.Stack.RealizedCompetitiveScopeOppNeg.Cost +
                                barOp.Stack.RealizedEfficientExecutionOppNeg.Cost +
                                barOp.Stack.RealizedSupplyChainTransformationOppNeg.Cost +
                                barOp.Stack.RealizedTechnologyandInnovationOppNeg.Cost;

                            sumRealOppNeg.Days = barOp.Stack.RealizedCompetitiveScopeOppNeg.Days +
                               barOp.Stack.RealizedEfficientExecutionOppNeg.Days +
                               barOp.Stack.RealizedSupplyChainTransformationOppNeg.Days +
                               barOp.Stack.RealizedTechnologyandInnovationOppNeg.Days;

                            if (sumRealOppNeg.Cost < 0)
                                bottomCost = bottomCost + sumRealOppNeg.Cost;
                            if (sumRealOppNeg.Days < 0)
                                bottomDays = bottomDays + sumRealOppNeg.Days;

                            barOp.Stack.Bottom.Cost = bottomCost; //plan.Cost - (sumRealOpp.Cost < 0 ? (sumRealOpp.Cost * -1) : sumRealOpp.Cost);
                            barOp.Stack.Bottom.Days = bottomDays; //plan.Days - (sumRealOpp.Days < 0 ? (sumRealOpp.Days * -1) : sumRealOpp.Days);

                            if (sumRealOppNeg.Cost > 0)
                                bottomCost = bottomCost + sumRealOppNeg.Cost;
                            if (sumRealOppNeg.Days > 0)
                                bottomDays = bottomDays + sumRealOppNeg.Days;

                            break;
                            #endregion
                        }
                    case "Realized PIPs Opp Pos":
                        {
                            #region Realized PIPs Opp Pos
                            barOp.Stack.RealizedCompetitiveScopeOppPos = realOPPComPos.Count == 0 ? new WellDrillData() : realOPPComPos.FirstOrDefault();
                            barOp.Stack.RealizedEfficientExecutionOppPos = realOPPEffiPos.Count == 0 ? new WellDrillData() : realOPPEffiPos.FirstOrDefault();
                            barOp.Stack.RealizedSupplyChainTransformationOppPos = realOPPSupPos.Count == 0 ? new WellDrillData() : realOPPSupPos.FirstOrDefault();
                            barOp.Stack.RealizedTechnologyandInnovationOppPos = realOPPTechPos.Count == 0 ? new WellDrillData() : realOPPTechPos.FirstOrDefault();

                            sumRealOppPos.Cost = barOp.Stack.RealizedCompetitiveScopeOppPos.Cost +
                                barOp.Stack.RealizedEfficientExecutionOppPos.Cost +
                                barOp.Stack.RealizedSupplyChainTransformationOppPos.Cost +
                                barOp.Stack.RealizedTechnologyandInnovationOppPos.Cost;

                            sumRealOppPos.Days = barOp.Stack.RealizedCompetitiveScopeOppPos.Days +
                               barOp.Stack.RealizedEfficientExecutionOppPos.Days +
                               barOp.Stack.RealizedSupplyChainTransformationOppPos.Days +
                               barOp.Stack.RealizedTechnologyandInnovationOppPos.Days;

                            if (sumRealOppPos.Cost < 0)
                                bottomCost = bottomCost + sumRealOppPos.Cost;
                            if (sumRealOppPos.Days < 0)
                                bottomDays = bottomDays + sumRealOppPos.Days;

                            barOp.Stack.Bottom.Cost = bottomCost; //plan.Cost - (sumRealOpp.Cost < 0 ? (sumRealOpp.Cost * -1) : sumRealOpp.Cost);
                            barOp.Stack.Bottom.Days = bottomDays; //plan.Days - (sumRealOpp.Days < 0 ? (sumRealOpp.Days * -1) : sumRealOpp.Days);

                            if (sumRealOppPos.Cost > 0)
                                bottomCost = bottomCost + sumRealOppPos.Cost;
                            if (sumRealOppPos.Days > 0)
                                bottomDays = bottomDays + sumRealOppPos.Days;

                            break;
                            #endregion
                        }
                    case "Realized PIPs Risk Pos":
                        {
                            #region Realized PIPs RISK Pos
                            barOp.Stack.RealizedCompetitiveScopeRiskPos = realRISKComPos.Count == 0 ? new WellDrillData() : realRISKComPos.FirstOrDefault();
                            barOp.Stack.RealizedEfficientExecutionRiskPos = realRISKEffiPos.Count == 0 ? new WellDrillData() : realRISKEffiPos.FirstOrDefault();
                            barOp.Stack.RealizedSupplyChainTransformationRiskPos = realRISKSupPos.Count == 0 ? new WellDrillData() : realRISKSupPos.FirstOrDefault();
                            barOp.Stack.RealizedTechnologyandInnovationRiskPos = realRISKTechPos.Count == 0 ? new WellDrillData() : realRISKTechPos.FirstOrDefault();

                            sumRealRiskPos.Cost = barOp.Stack.RealizedCompetitiveScopeRiskPos.Cost +
                                    barOp.Stack.RealizedEfficientExecutionRiskPos.Cost +
                                    barOp.Stack.RealizedSupplyChainTransformationRiskPos.Cost +
                                    barOp.Stack.RealizedTechnologyandInnovationRiskPos.Cost;
                            sumRealRiskPos.Days = barOp.Stack.RealizedCompetitiveScopeRiskPos.Days +
                               barOp.Stack.RealizedEfficientExecutionRiskPos.Days +
                               barOp.Stack.RealizedSupplyChainTransformationRiskPos.Days +
                               barOp.Stack.RealizedTechnologyandInnovationRiskPos.Days;

                            if (sumRealRiskPos.Cost < 0)
                                bottomCost = bottomCost + sumRealRiskPos.Cost;
                            if (sumRealRiskPos.Days < 0)
                                bottomDays = bottomDays + sumRealRiskPos.Days;

                            barOp.Stack.Bottom.Cost = bottomCost;//(sumRealOpp.Cost < 0 ? (-1 * sumRealOpp.Cost) : sumRealOpp.Cost);
                            barOp.Stack.Bottom.Days = bottomDays;//(sumRealOpp.Days < 0 ? (-1 * sumRealOpp.Days) : sumRealOpp.Days);

                            if (sumRealRiskPos.Cost > 0)
                                bottomCost = bottomCost + sumRealRiskPos.Cost;
                            if (sumRealRiskPos.Days > 0)
                                bottomDays = bottomDays + sumRealRiskPos.Days;

                            //barOp.Stack.TOP.Cost = plan.Cost - (barOp.Stack.Bottom.Cost < 0 ? (barOp.Stack.Bottom.Cost * -1) : barOp.Stack.Bottom.Cost) + (sumRealRisk.Cost < 0 ? (sumRealRisk.Cost * -1) : sumRealRisk.Cost);
                            //barOp.Stack.TOP.Days = plan.Days - (barOp.Stack.Bottom.Days < 0 ? (barOp.Stack.Bottom.Days * -1) : barOp.Stack.Bottom.Days) + (sumRealRisk.Days < 0 ? (sumRealRisk.Days * -1) : sumRealRisk.Days);

                            //sumRealRiskPlusBottom.Cost = bottomCost;//barOp.Stack.Bottom.Cost + (sumRealRisk.Cost < 0 ? (sumRealRisk.Cost * -1) : sumRealRisk.Cost);
                            //sumRealRiskPlusBottom.Days = bottomDays;//barOp.Stack.Bottom.Days + (sumRealRisk.Days < 0 ? (sumRealRisk.Days * -1) : sumRealRisk.Days);

                            break;
                            #endregion
                        }
                    case "Realized PIPs Risk Neg":
                        {
                            #region Realized PIPs RISK Neg
                            barOp.Stack.RealizedCompetitiveScopeRiskNeg = realRISKComNeg.Count == 0 ? new WellDrillData() : realRISKComNeg.FirstOrDefault();
                            barOp.Stack.RealizedEfficientExecutionRiskNeg = realRISKEffiNeg.Count == 0 ? new WellDrillData() : realRISKEffiNeg.FirstOrDefault();
                            barOp.Stack.RealizedSupplyChainTransformationRiskNeg = realRISKSupNeg.Count == 0 ? new WellDrillData() : realRISKSupNeg.FirstOrDefault();
                            barOp.Stack.RealizedTechnologyandInnovationRiskNeg = realRISKTechNeg.Count == 0 ? new WellDrillData() : realRISKTechNeg.FirstOrDefault();

                            sumRealRiskNeg.Cost = barOp.Stack.RealizedCompetitiveScopeRiskNeg.Cost +
                                    barOp.Stack.RealizedEfficientExecutionRiskNeg.Cost +
                                    barOp.Stack.RealizedSupplyChainTransformationRiskNeg.Cost +
                                    barOp.Stack.RealizedTechnologyandInnovationRiskNeg.Cost;
                            sumRealRiskNeg.Days = barOp.Stack.RealizedCompetitiveScopeRiskNeg.Days +
                               barOp.Stack.RealizedEfficientExecutionRiskNeg.Days +
                               barOp.Stack.RealizedSupplyChainTransformationRiskNeg.Days +
                               barOp.Stack.RealizedTechnologyandInnovationRiskNeg.Days;

                            if (sumRealRiskNeg.Cost < 0)
                                bottomCost = bottomCost + sumRealRiskNeg.Cost;
                            if (sumRealRiskNeg.Days < 0)
                                bottomDays = bottomDays + sumRealRiskNeg.Days;

                            barOp.Stack.Bottom.Cost = bottomCost;//(sumRealOpp.Cost < 0 ? (-1 * sumRealOpp.Cost) : sumRealOpp.Cost);
                            barOp.Stack.Bottom.Days = bottomDays;//(sumRealOpp.Days < 0 ? (-1 * sumRealOpp.Days) : sumRealOpp.Days);

                            if (sumRealRiskNeg.Cost > 0)
                                bottomCost = bottomCost + sumRealRiskNeg.Cost;
                            if (sumRealRiskNeg.Days > 0)
                                bottomDays = bottomDays + sumRealRiskNeg.Days;

                            //barOp.Stack.TOP.Cost = plan.Cost - (barOp.Stack.Bottom.Cost < 0 ? (barOp.Stack.Bottom.Cost * -1) : barOp.Stack.Bottom.Cost) + (sumRealRisk.Cost < 0 ? (sumRealRisk.Cost * -1) : sumRealRisk.Cost);
                            //barOp.Stack.TOP.Days = plan.Days - (barOp.Stack.Bottom.Days < 0 ? (barOp.Stack.Bottom.Days * -1) : barOp.Stack.Bottom.Days) + (sumRealRisk.Days < 0 ? (sumRealRisk.Days * -1) : sumRealRisk.Days);

                            //sumRealRiskPlusBottom.Cost = bottomCost;//barOp.Stack.Bottom.Cost + (sumRealRisk.Cost < 0 ? (sumRealRisk.Cost * -1) : sumRealRisk.Cost);
                            //sumRealRiskPlusBottom.Days = bottomDays;//barOp.Stack.Bottom.Days + (sumRealRisk.Days < 0 ? (sumRealRisk.Days * -1) : sumRealRisk.Days);

                            break;
                            #endregion
                        }
                    case "Additional Banked Saving Risk Pos":
                        {
                            #region Additional Banked Saving Risk Pos
                            barOp.Stack.ADSCompetitiveScopePos.Cost = adbComCostPos;
                            barOp.Stack.ADSCompetitiveScopePos.Days = adbComDaysPos;

                            barOp.Stack.ADSEfficientExecutionPos.Cost = adbEffiCostPos;
                            barOp.Stack.ADSEfficientExecutionPos.Days = adbEffiDaysPos;

                            barOp.Stack.ADSSupplyChainTransformationPos.Cost = adbSupCostPos;
                            barOp.Stack.ADSSupplyChainTransformationPos.Days = adbSupDaysPos;

                            barOp.Stack.ADSTechnologyandInnovationPos.Cost = adbTecCostPos;
                            barOp.Stack.ADSTechnologyandInnovationPos.Days = adbTecDaysPos;


                            sumAdbPos.Cost = barOp.Stack.ADSCompetitiveScopePos.Cost +
                                    barOp.Stack.ADSEfficientExecutionPos.Cost +
                                    barOp.Stack.ADSSupplyChainTransformationPos.Cost +
                                    barOp.Stack.ADSTechnologyandInnovationPos.Cost;

                            sumAdbPos.Days = barOp.Stack.ADSCompetitiveScopePos.Days +
                                    barOp.Stack.ADSEfficientExecutionPos.Days +
                                    barOp.Stack.ADSSupplyChainTransformationPos.Days +
                                    barOp.Stack.ADSTechnologyandInnovationPos.Days;


                            if (sumAdbPos.Cost < 0)
                                bottomCost = bottomCost + sumAdbPos.Cost;
                            if (sumAdbPos.Days < 0)
                                bottomDays = bottomDays + sumAdbPos.Days;

                            barOp.Stack.Bottom.Cost = bottomCost;//(sumRealOpp.Cost < 0 ? (-1 * sumRealOpp.Cost) : sumRealOpp.Cost);
                            barOp.Stack.Bottom.Days = bottomDays;//(sumRealOpp.Days < 0 ? (-1 * sumRealOpp.Days) : sumRealOpp.Days);

                            if (sumAdbPos.Cost > 0)
                                bottomCost = bottomCost + sumAdbPos.Cost;
                            if (sumAdbPos.Days > 0)
                                bottomDays = bottomDays + sumAdbPos.Days;
                            //barOp.Stack.TOP.Cost = plan.Cost -
                            //    barOp.Stack.Bottom.Cost + (sumAdb.Cost < 0 ? (sumAdb.Cost * -1) : sumAdb.Cost);
                            //barOp.Stack.TOP.Days = plan.Days -
                            //    barOp.Stack.Bottom.Days + (sumAdb.Days < 0 ? (sumAdb.Days * -1) : sumAdb.Days);


                            break;

                            #endregion
                        }

                    case "Additional Banked Saving Risk Neg":
                        {
                            #region Additional Banked Saving Risk Neg
                            barOp.Stack.ADSCompetitiveScopeNeg.Cost = adbComCostNeg;
                            barOp.Stack.ADSCompetitiveScopeNeg.Days = adbComDaysNeg;

                            barOp.Stack.ADSEfficientExecutionNeg.Cost = adbEffiCostNeg;
                            barOp.Stack.ADSEfficientExecutionNeg.Days = adbEffiDaysNeg;

                            barOp.Stack.ADSSupplyChainTransformationNeg.Cost = adbSupCostNeg;
                            barOp.Stack.ADSSupplyChainTransformationNeg.Days = adbSupDaysNeg;

                            barOp.Stack.ADSTechnologyandInnovationNeg.Cost = adbTecCostNeg;
                            barOp.Stack.ADSTechnologyandInnovationNeg.Days = adbTecDaysNeg;


                            sumAdbNeg.Cost = barOp.Stack.ADSCompetitiveScopeNeg.Cost +
                                    barOp.Stack.ADSEfficientExecutionNeg.Cost +
                                    barOp.Stack.ADSSupplyChainTransformationNeg.Cost +
                                    barOp.Stack.ADSTechnologyandInnovationNeg.Cost;

                            sumAdbNeg.Days = barOp.Stack.ADSCompetitiveScopeNeg.Days +
                                    barOp.Stack.ADSEfficientExecutionNeg.Days +
                                    barOp.Stack.ADSSupplyChainTransformationNeg.Days +
                                    barOp.Stack.ADSTechnologyandInnovationNeg.Days;

                            if (sumAdbNeg.Cost < 0)
                                bottomCost = bottomCost + sumAdbNeg.Cost;
                            if (sumAdbNeg.Days < 0)
                                bottomDays = bottomDays + sumAdbNeg.Days;

                            barOp.Stack.Bottom.Cost = bottomCost;//(sumRealOpp.Cost < 0 ? (-1 * sumRealOpp.Cost) : sumRealOpp.Cost);
                            barOp.Stack.Bottom.Days = bottomDays;//(sumRealOpp.Days < 0 ? (-1 * sumRealOpp.Days) : sumRealOpp.Days);

                            if (sumAdbNeg.Cost > 0)
                                bottomCost = bottomCost + sumAdbNeg.Cost;
                            if (sumAdbNeg.Days > 0)
                                bottomDays = bottomDays + sumAdbNeg.Days;

                            //barOp.Stack.TOP.Cost = plan.Cost -
                            //    barOp.Stack.Bottom.Cost + (sumAdb.Cost < 0 ? (sumAdb.Cost * -1) : sumAdb.Cost);
                            //barOp.Stack.TOP.Days = plan.Days -
                            //    barOp.Stack.Bottom.Days + (sumAdb.Days < 0 ? (sumAdb.Days * -1) : sumAdb.Days);


                            break;

                            #endregion
                        }
                    case "LE":
                        {
                            barOp.Stack.LE = LEFinal;
                            bottomCost = barOp.Stack.LE.Cost;
                            bottomDays = barOp.Stack.LE.Days;
                            break;
                        }
                    case "Unrealized PIPs Opp Neg":
                        {

                            #region Unrealized PIPs Opp Neg

                            barOp.Stack.UnrealizedCompetitiveScopeOppNeg = unrealOPPComNeg.Count == 0 ? new WellDrillData() : unrealOPPComNeg.FirstOrDefault();
                            barOp.Stack.UnrealizedEfficientExecutionOppNeg = unrealOPPEffiNeg.Count == 0 ? new WellDrillData() : unrealOPPEffiNeg.FirstOrDefault();
                            barOp.Stack.UnrealizedSupplyChainTransformationOppNeg = unrealOPPSupNeg.Count == 0 ? new WellDrillData() : unrealOPPSupNeg.FirstOrDefault();
                            barOp.Stack.UnrealizedTechnologyandInnovationOppNeg = unrealOPPTechNeg.Count == 0 ? new WellDrillData() : unrealOPPTechNeg.FirstOrDefault();

                            sumUnRealOppNeg.Cost = barOp.Stack.UnrealizedCompetitiveScopeOppNeg.Cost +
                                barOp.Stack.UnrealizedEfficientExecutionOppNeg.Cost +
                                barOp.Stack.UnrealizedSupplyChainTransformationOppNeg.Cost +
                                barOp.Stack.UnrealizedTechnologyandInnovationOppNeg.Cost;
                            sumUnRealOppNeg.Days = barOp.Stack.UnrealizedCompetitiveScopeOppNeg.Days +
                                 barOp.Stack.UnrealizedEfficientExecutionOppNeg.Days +
                                 barOp.Stack.UnrealizedSupplyChainTransformationOppNeg.Days +
                                 barOp.Stack.UnrealizedTechnologyandInnovationOppNeg.Days;

                            if (sumUnRealOppNeg.Cost < 0)
                                bottomCost = bottomCost + sumUnRealOppNeg.Cost;
                            if (sumUnRealOppNeg.Days < 0)
                                bottomDays = bottomDays + sumUnRealOppNeg.Days;

                            barOp.Stack.Bottom.Cost = bottomCost;//(sumRealOpp.Cost < 0 ? (-1 * sumRealOpp.Cost) : sumRealOpp.Cost);
                            barOp.Stack.Bottom.Days = bottomDays;//(sumRealOpp.Days < 0 ? (-1 * sumRealOpp.Days) : sumRealOpp.Days);

                            if (sumUnRealOppNeg.Cost > 0)
                                bottomCost = bottomCost + sumUnRealOppNeg.Cost;
                            if (sumUnRealOppNeg.Days > 0)
                                bottomDays = bottomDays + sumUnRealOppNeg.Days;

                            break;

                            #endregion
                        }
                    case "Unrealized PIPs Opp Pos":
                        {

                            #region Unrealized PIPs Opp Pos

                            barOp.Stack.UnrealizedCompetitiveScopeOppPos = unrealOPPComPos.Count == 0 ? new WellDrillData() : unrealOPPComPos.FirstOrDefault();
                            barOp.Stack.UnrealizedEfficientExecutionOppPos = unrealOPPEffiPos.Count == 0 ? new WellDrillData() : unrealOPPEffiPos.FirstOrDefault();
                            barOp.Stack.UnrealizedSupplyChainTransformationOppPos = unrealOPPSupPos.Count == 0 ? new WellDrillData() : unrealOPPSupPos.FirstOrDefault();
                            barOp.Stack.UnrealizedTechnologyandInnovationOppPos = unrealOPPTechPos.Count == 0 ? new WellDrillData() : unrealOPPTechPos.FirstOrDefault();

                            sumUnRealOppPos.Cost = barOp.Stack.UnrealizedCompetitiveScopeOppPos.Cost +
                                barOp.Stack.UnrealizedEfficientExecutionOppPos.Cost +
                                barOp.Stack.UnrealizedSupplyChainTransformationOppPos.Cost +
                                barOp.Stack.UnrealizedTechnologyandInnovationOppPos.Cost;
                            sumUnRealOppPos.Days = barOp.Stack.UnrealizedCompetitiveScopeOppPos.Days +
                                 barOp.Stack.UnrealizedEfficientExecutionOppPos.Days +
                                 barOp.Stack.UnrealizedSupplyChainTransformationOppPos.Days +
                                 barOp.Stack.UnrealizedTechnologyandInnovationOppPos.Days;

                            if (sumUnRealOppPos.Cost < 0)
                                bottomCost = bottomCost + sumUnRealOppPos.Cost;
                            if (sumUnRealOppPos.Days < 0)
                                bottomDays = bottomDays + sumUnRealOppPos.Days;

                            barOp.Stack.Bottom.Cost = bottomCost;//(sumRealOpp.Cost < 0 ? (-1 * sumRealOpp.Cost) : sumRealOpp.Cost);
                            barOp.Stack.Bottom.Days = bottomDays;//(sumRealOpp.Days < 0 ? (-1 * sumRealOpp.Days) : sumRealOpp.Days);

                            if (sumUnRealOppPos.Cost > 0)
                                bottomCost = bottomCost + sumUnRealOppPos.Cost;
                            if (sumUnRealOppPos.Days > 0)
                                bottomDays = bottomDays + sumUnRealOppPos.Days;

                            break;

                            #endregion
                        }
                    case "Unrealized PIPs Risk Pos":
                        {
                            #region Unrealized PIPs Risk Pos
                            barOp.Stack.UnrealizedCompetitiveScopeRiskPos = unrealRISKComPos.Count == 0 ? new WellDrillData() : unrealRISKComPos.FirstOrDefault();
                            barOp.Stack.UnrealizedEfficientExecutionRiskPos = unrealRISKEffiPos.Count == 0 ? new WellDrillData() : unrealRISKEffiPos.FirstOrDefault();
                            barOp.Stack.UnrealizedSupplyChainTransformationRiskPos = unrealRISKSupPos.Count == 0 ? new WellDrillData() : unrealRISKSupPos.FirstOrDefault();
                            barOp.Stack.UnrealizedTechnologyandInnovationRiskPos = unrealRISKTechPos.Count == 0 ? new WellDrillData() : unrealRISKTechPos.FirstOrDefault();


                            sumunRealRiskPos.Cost = barOp.Stack.UnrealizedCompetitiveScopeRiskPos.Cost +
                                    barOp.Stack.UnrealizedEfficientExecutionRiskPos.Cost +
                                    barOp.Stack.UnrealizedSupplyChainTransformationRiskPos.Cost +
                                    barOp.Stack.UnrealizedTechnologyandInnovationRiskPos.Cost;
                            sumunRealRiskPos.Days = barOp.Stack.UnrealizedCompetitiveScopeRiskPos.Days +
                                    barOp.Stack.UnrealizedEfficientExecutionRiskPos.Days +
                                    barOp.Stack.UnrealizedSupplyChainTransformationRiskPos.Days +
                                    barOp.Stack.UnrealizedTechnologyandInnovationRiskPos.Days;


                            if (sumunRealRiskPos.Cost < 0)
                                bottomCost = bottomCost + sumunRealRiskPos.Cost;
                            if (sumunRealRiskPos.Days < 0)
                                bottomDays = bottomDays + sumunRealRiskPos.Days;

                            barOp.Stack.Bottom.Cost = bottomCost;//(sumRealOpp.Cost < 0 ? (-1 * sumRealOpp.Cost) : sumRealOpp.Cost);
                            barOp.Stack.Bottom.Days = bottomDays;//(sumRealOpp.Days < 0 ? (-1 * sumRealOpp.Days) : sumRealOpp.Days);

                            if (sumunRealRiskPos.Cost > 0)
                                bottomCost = bottomCost + sumunRealRiskPos.Cost;
                            if (sumunRealRiskPos.Days > 0)
                                bottomDays = bottomDays + sumunRealRiskPos.Days;


                            //barOp.Stack.TOP.Cost = calcLE.Cost - barOp.Stack.Bottom.Cost + (sumRealRisk.Cost < 0 ? (sumRealRisk.Cost * -1) : sumRealRisk.Cost);
                            //barOp.Stack.TOP.Days = calcLE.Days - barOp.Stack.Bottom.Days + (sumRealRisk.Days < 0 ? (sumRealRisk.Days * -1) : sumRealRisk.Days);

                            //sumunRealRiskPlusBottom.Cost = barOp.Stack.Bottom.Cost + (sumunRealRisk.Cost < 0 ? (sumunRealRisk.Cost * -1) : sumunRealRisk.Cost);
                            //sumunRealRiskPlusBottom.Days = barOp.Stack.Bottom.Days + (sumunRealRisk.Days < 0 ? (sumunRealRisk.Days * -1) : sumunRealRisk.Days);


                            break;
                            #endregion
                        }
                    case "Unrealized PIPs Risk Neg":
                        {
                            #region Unrealized PIPs Risk Neg
                            barOp.Stack.UnrealizedCompetitiveScopeRiskNeg = unrealRISKComNeg.Count == 0 ? new WellDrillData() : unrealRISKComNeg.FirstOrDefault();
                            barOp.Stack.UnrealizedEfficientExecutionRiskNeg = unrealRISKEffiNeg.Count == 0 ? new WellDrillData() : unrealRISKEffiNeg.FirstOrDefault();
                            barOp.Stack.UnrealizedSupplyChainTransformationRiskNeg = unrealRISKSupNeg.Count == 0 ? new WellDrillData() : unrealRISKSupNeg.FirstOrDefault();
                            barOp.Stack.UnrealizedTechnologyandInnovationRiskNeg = unrealRISKTechNeg.Count == 0 ? new WellDrillData() : unrealRISKTechNeg.FirstOrDefault();


                            sumunRealRiskNeg.Cost = barOp.Stack.UnrealizedCompetitiveScopeRiskNeg.Cost +
                                    barOp.Stack.UnrealizedEfficientExecutionRiskNeg.Cost +
                                    barOp.Stack.UnrealizedSupplyChainTransformationRiskNeg.Cost +
                                    barOp.Stack.UnrealizedTechnologyandInnovationRiskNeg.Cost;
                            sumunRealRiskNeg.Days = barOp.Stack.UnrealizedCompetitiveScopeRiskNeg.Days +
                                    barOp.Stack.UnrealizedEfficientExecutionRiskNeg.Days +
                                    barOp.Stack.UnrealizedSupplyChainTransformationRiskNeg.Days +
                                    barOp.Stack.UnrealizedTechnologyandInnovationRiskNeg.Days;


                            if (sumunRealRiskNeg.Cost < 0)
                                bottomCost = bottomCost + sumunRealRiskNeg.Cost;
                            if (sumunRealRiskNeg.Days < 0)
                                bottomDays = bottomDays + sumunRealRiskNeg.Days;

                            barOp.Stack.Bottom.Cost = bottomCost;//(sumRealOpp.Cost < 0 ? (-1 * sumRealOpp.Cost) : sumRealOpp.Cost);
                            barOp.Stack.Bottom.Days = bottomDays;//(sumRealOpp.Days < 0 ? (-1 * sumRealOpp.Days) : sumRealOpp.Days);

                            if (sumunRealRiskNeg.Cost > 0)
                                bottomCost = bottomCost + sumunRealRiskNeg.Cost;
                            if (sumunRealRiskNeg.Days > 0)
                                bottomDays = bottomDays + sumunRealRiskNeg.Days;

                            //barOp.Stack.TOP.Cost = calcLE.Cost - barOp.Stack.Bottom.Cost + (sumRealRisk.Cost < 0 ? (sumRealRisk.Cost * -1) : sumRealRisk.Cost);
                            //barOp.Stack.TOP.Days = calcLE.Days - barOp.Stack.Bottom.Days + (sumRealRisk.Days < 0 ? (sumRealRisk.Days * -1) : sumRealRisk.Days);

                            //sumunRealRiskPlusBottom.Cost = barOp.Stack.Bottom.Cost + (sumunRealRisk.Cost < 0 ? (sumunRealRisk.Cost * -1) : sumunRealRisk.Cost);
                            //sumunRealRiskPlusBottom.Days = barOp.Stack.Bottom.Days + (sumunRealRisk.Days < 0 ? (sumunRealRisk.Days * -1) : sumunRealRisk.Days);


                            break;
                            #endregion
                        }
                    default:
                        {
                            //if (sumUnRealOpp.Cost > 0)
                            //    bottomCost = bottomCost + sumunRealRisk.Cost;

                            //if (sumunRealRisk.Days > 0)
                            //    bottomDays = bottomDays + sumunRealRisk.Days;

                            barOp.Stack.UnriskedUpside.Cost = bottomCost; //sumunRealRiskPlusBottom;
                            barOp.Stack.UnriskedUpside.Days = bottomDays; //sumunRealRiskPlusBottom;
                            // Upside
                            break;
                        }
                }
                results.Add(barOp);

            }

            return results;
        }


    }

}
