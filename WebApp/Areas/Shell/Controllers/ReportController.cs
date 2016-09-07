using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
using System.Globalization;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class ReportController : Controller
    {
        // GET: Shell/Report
        public ActionResult Index()
        {
            return View();
        }

        #region Waterfall
        public ActionResult Waterfall()
        {
            return View();
        }

        public Double GetDayOrCost(WellDrillData d, string DayorCost = "Day")
        {
            double ret = 0;
            if (DayorCost.Equals("Day")) ret = d.Days;
            if (DayorCost.Equals("Cost")) ret = d.Cost;
            return ret;
        }

        public Double GetDayOrCost(PIPElement d, string DayorCost = "Day", string element = "Plan")
        {
            double division = 1;
            double ret = 0;
            switch (element)
            {
                case "Plan":
                    if (DayorCost.Equals("Day")) ret = d.DaysPlanImprovement + d.DaysPlanRisk;
                    if (DayorCost.Equals("Cost")) ret = Tools.Div(d.CostPlanImprovement + d.CostPlanRisk, division);
                    break;

                case "Actual":
                    if (DayorCost.Equals("Day")) ret = d.DaysActualImprovement + d.DaysActualRisk;
                    if (DayorCost.Equals("Cost")) ret = Tools.Div(d.CostActualImprovement + d.CostActualRisk, division);
                    break;

                case "LE":
                    if (DayorCost.Equals("Day")) ret = d.DaysCurrentWeekImprovement + d.DaysCurrentWeekRisk;
                    if (DayorCost.Equals("Cost")) ret = Tools.Div(d.CostCurrentWeekImprovement + d.CostCurrentWeekRisk, division);
                    break;
            }
            return ret;
        }

        private IMongoQuery GenerateFilterQueryForWaterfall(
            string periodStartBegin = "",
            string periodStartEnd = "",
            string periodFinishBegin = "",
            string periodFinishEnd = "",
            string dateRelation = "AND",
            List<string> operatingUnits = null,
            List<string> projectNames = null,
            List<string> regions = null,
            List<string> rigNames = null,
            List<string> rigTypes = null,
            List<string> wellNames = null,
            List<string> activities = null,
            //bool activeWell = false,
            string activeWell = "Per Last Sequence",
            string periodBase = "By Last Sequence",
            List<string> exType = null,
            bool wellwithpipcheck = true
            )
        {
            List<IMongoQuery> queries = new List<IMongoQuery>();
            queries.Add(WebTools.GetWellActivitiesQuery("WellName", ""));

            if (operatingUnits != null) if (operatingUnits.Count > 0)
                    queries.Add(Query.In("OperatingUnit", new BsonArray(operatingUnits)));
            if (projectNames != null) if (projectNames.Count > 0)
                    queries.Add(Query.In("ProjectName", new BsonArray(projectNames)));
            if (regions != null) if (regions.Count > 0)
                    queries.Add(Query.In("Region", new BsonArray(regions)));
            if (rigNames != null) if (rigNames.Count > 0)
                    queries.Add(Query.In("RigName", new BsonArray(rigNames)));
            if (rigTypes != null) if (rigTypes.Count > 0)
                    queries.Add(Query.In("RigType", new BsonArray(rigTypes)));
            if (wellNames != null) if (wellNames.Count > 0)
                    queries.Add(Query.In("WellName", new BsonArray(wellNames)));

            if (activities != null) if (activities.Count > 0)
                    queries.Add(Query.In("Phases.ActivityType", new BsonArray(activities)));

            if (exType != null) if (exType.Count > 0)
                    queries.Add(Query.In("EXType", new BsonArray(exType)));

            List<IMongoQuery> dateQueries = new List<IMongoQuery>();

            string fieldPeriodStart = "";
            string fieldPeriodEnd = "";
            if (periodBase.ToLower().Contains("sequence"))
            {
                fieldPeriodStart = "Phases.PhSchedule.Start";
                fieldPeriodEnd = "Phases.PhSchedule.Finish";
            }
            else
            {
                fieldPeriodStart = "Phases.LESchedule.Start";
                fieldPeriodEnd = "Phases.LESchedule.Finish";
            }

            DateTime dateStartBegin = new DateTime();
            DateTime dateStartEnd = new DateTime();
            DateTime dateFinishBegin = new DateTime();
            DateTime dateFinishEnd = new DateTime();

            if (periodStartBegin.Trim() != "")
                dateStartBegin = DateTime.ParseExact(periodStartBegin, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(-7);
            else
                dateStartBegin = new DateTime(3000, 01, 01);
            if (periodStartEnd.Trim() != "")

                dateStartEnd = DateTime.ParseExact(periodStartEnd, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(7);
            else
                dateStartEnd = new DateTime(3000, 01, 01);

            if (periodFinishBegin.Trim() != "")
                dateFinishBegin = DateTime.ParseExact(periodFinishBegin, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(-7);
            else
                dateFinishBegin = new DateTime(3000, 01, 01);

            if (periodFinishEnd.Trim() != "")
                dateFinishEnd = DateTime.ParseExact(periodFinishEnd, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(7);
            else
                dateFinishEnd = new DateTime(3000, 01, 01);


            if (!periodStartBegin.Equals("") && !periodStartEnd.Equals(""))
                dateQueries.Add(Query.And(
                    Query.GTE(fieldPeriodStart, dateStartBegin),
                    Query.LTE(fieldPeriodStart, dateStartEnd)
                ));
            else if (!periodStartBegin.Equals(""))
                dateQueries.Add(
                    Query.GTE(fieldPeriodStart, dateStartBegin)
                );
            else if (!periodStartEnd.Equals(""))
                dateQueries.Add(
                    Query.LTE(fieldPeriodStart, dateStartEnd)
                );

            if (!periodFinishBegin.Equals("") && !periodFinishEnd.Equals(""))
                dateQueries.Add(Query.And(
                    Query.GTE(fieldPeriodEnd, dateFinishBegin),
                    Query.LTE(fieldPeriodEnd, dateFinishEnd)
                ));
            else if (!periodFinishBegin.Equals(""))
                dateQueries.Add(
                    Query.GTE(fieldPeriodEnd, dateFinishBegin)
                );
            else if (!periodFinishEnd.Equals(""))
                dateQueries.Add(
                    Query.LTE(fieldPeriodEnd, dateFinishEnd)
                );
            if (dateQueries.Count > 0)
                queries.Add(dateRelation.ToUpper().Equals("AND") ? Query.And(dateQueries.ToArray()) : Query.Or(dateQueries.ToArray()));

            return (queries.Count > 0 ? Query.And(queries) : null);
        }

        private double GetRatio(DateRange value, DateRange range)
        {
            double ratio = 1;
            if (value.Start < range.Start || value.Finish > range.Finish)
            {
                DateRange newRange = new DateRange
                {
                    Start = value.Start,
                    Finish = value.Finish
                };
                if (newRange.Start < range.Start) newRange.Start = range.Start;
                if (newRange.Finish > range.Finish) newRange.Finish = range.Finish;
                ratio = Tools.Div((double)newRange.Days, (double)value.Days);
            }
            return ratio;
        }

        private List<WellActivity> PopulateWaterfallData(
            IMongoQuery query,
            string dateStart = "",
            string dateFinish = "",
            string dateStart2 = "",
            string dateFinish2 = "",
            string dateRelation = "AND",
            List<string> activities = null,
            string activeWell = "Per Last Sequence",
            string periodBase = "By Last Sequence", bool wellwithpipcheck = true
            )
        {
            var was = WellActivity.Populate<WellActivity>(query);
            var final = was
                .Where(d =>
                {
                    var dateRange = new DateRange();
                    var ph = d.Phases.Where(e=>e.ActivityType.ToLower().Contains("risk")==false);
                    if (ph == null) return false;
                    if (periodBase.ToLower().Contains("sequence"))
                    {
                        dateRange.Start = ph.Min(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Start));
                        dateRange.Finish = ph.Max(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Finish));
                    }
                    else
                    {
                        dateRange.Start = ph.Min(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Start));
                        dateRange.Finish = ph.Max(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Finish));
                    }
                    return new DashboardController().dateBetween(dateRange, dateStart, dateFinish, dateStart2, dateFinish2, dateRelation);
                });

            var results = new List<WellActivity>();
            foreach (var t in final)
            {
                var yu = t.Phases.Count(e => e.LastWeek.CompareTo(Tools.DefaultDate) > 0);
                if (yu > 0)
                {
                    results.Add(t);
                }
            }

            return final.Select(d =>
             {
                 if (activities != null)
                 {
                     if (activities.Count > 0)
                     {
                         d.Phases = d.Phases.Where(e =>
                         {
                             var activityFilter = activities.Contains(e.ActivityType);
                             var riskFilter = !e.ActivityType.ToLower().Contains("risk");
                             return (riskFilter && activityFilter);
                         }).ToList();
                     }
                 }
                 return d;
             })
             .OrderBy(d => d.WellName)
             .ToList();
        }

        private WaterfallBaseResult GetWaterfallBaseData(WaterfallParam param)
        {
            param.performanceUnits = (param.performanceUnits == null ? new List<string>() : param.performanceUnits);

            IMongoQuery query = GenerateFilterQueryForWaterfall(param.dateStart, param.dateFinish, param.dateStart2, param.dateFinish2, param.dateRelation,
                param.operatingUnits, param.projectNames, param.regions, param.rigNames, param.rigTypes, 
                param.wellNames, param.activities, param.activeWell, param.periodBase, param.exType, param.wellwithpipcheck);

            var dtStart1 = Tools.ToDateTime(param.dateStart, true);
            var dtFinish1 = Tools.ToDateTime(param.dateFinish, true);
            var dtStart2 = Tools.ToDateTime(param.dateStart2, true);
            var dtFinish2 = Tools.ToDateTime(param.dateFinish2, true);
            if (dtFinish1 == Tools.DefaultDate) dtFinish1 = new DateTime(3000, 1, 1);
            if (dtFinish2 == Tools.DefaultDate) dtFinish2 = new DateTime(3000, 1, 1);

            var was = PopulateWaterfallData(query, param.dateStart, param.dateFinish, param.dateStart2, param.dateFinish2, param.dateRelation, param.activities, param.activeWell, param.periodBase, param.wellwithpipcheck);

            //bool useUpdateIfNull = true;
            DateRange ratioRange = new DateRange(Tools.ToDateTime(param.dateStart), Tools.ToDateTime(param.dateFinish2));
            var pips = new List<WFPIPElement>();
            foreach (var wa in was)
            {
                double planRatio = GetRatio(wa.Phases[0].PlanSchedule, ratioRange);
                double leRatio = GetRatio(wa.Phases[0].LESchedule, ratioRange);

                //wa.GetUpdate(dtFinish2, useUpdateIfNull, true);
                wa.Phases[0].OP.Days = planRatio * wa.Phases[0].OP.Days;
                wa.Phases[0].OP.Cost = planRatio * wa.Phases[0].OP.Cost;
                wa.Phases[0].TQ.Days = planRatio * wa.Phases[0].TQ.Days;
                wa.Phases[0].TQ.Cost = planRatio * wa.Phases[0].TQ.Cost;
                wa.Phases[0].LE.Days = leRatio * wa.Phases[0].LE.Days;
                wa.Phases[0].LE.Cost = leRatio * wa.Phases[0].LE.Cost;
                
                foreach (var ph in wa.Phases)
                {
                    List<IMongoQuery> qpips = new List<IMongoQuery>();
                    qpips.Add(Query.EQ("WellName",wa.WellName));
                    qpips.Add(Query.EQ("SequenceId", wa.UARigSequenceId));
                    qpips.Add(Query.EQ("PhaseNo", ph.PhaseNo));
                    ph.PIPs = new List<PIPElement>();
                    WellPIP wp = WellPIP.Get<WellPIP>(Query.And(qpips));
                    if (wp != null)
                    {
                        foreach (var pi in wp.Elements)
                        {
                            WFPIPElement wfpip = BsonSerializer.Deserialize<WFPIPElement>(pi.ToBsonDocument());
                            wfpip.RigName = wa.RigName;
                            wfpip.WellName = wa.WellName;

                            if (param.performanceUnits.Count > 0)
                            {
                                if (param.performanceUnits.Contains(pi.PerformanceUnit))
                                {
                                    ph.PIPs.Add(wfpip);
                                    pips.Add(wfpip);
                                }
                            }
                            else
                            {
                                ph.PIPs.Add(wfpip);
                                pips.Add(wfpip);
                            }
                        }
                    }
                }
            }

            //--- this should only active if no other selection other than rig
            bool validRig = param.isValidForCRSelection();
           
            if(validRig){
                var lqcrs = new List<IMongoQuery>();
                lqcrs.Add(Query.EQ("Type", "Reduction"));
                if (param.rigNames!= null && param.rigNames.Count > 0) lqcrs.Add(Query.In("WellName", 
                    new BsonArray(param.rigNames.Select(d=>d + "_CR"))));
                var qcrs = lqcrs.Count == 0 ? Query.Null : Query.And(lqcrs);
                var crs = WellPIP.Populate<WellPIP>(qcrs);
                foreach (var cr in crs)
                {
                    foreach (var pe in cr.Elements)
                    {
                        WFPIPElement wfpe = new WFPIPElement();
                        WFPIPElement wfpip = BsonSerializer.Deserialize<WFPIPElement>(pe.ToBsonDocument());
                        wfpip.RigName = cr.RigName;
                        wfpip.WellName = cr.RigName+"_CR";
                        pips.Add(wfpip);
                    }
                }
            }

            var dateNow = DateTime.Now;
            var final = was.SelectMany(d => d.Phases, (d, e) => new ListWaterfall()
            {
                Activity = d,
                Phase = e
            })
            .Where(d => !d.Phase.ActivityType.ToLower().Contains("risk"))
            .Where(d =>
            {

                var PhStartForFilter = d.Phase.PhSchedule.Start;
                var PhFinishForFilter = d.Phase.PhSchedule.Finish;
                var LEStartForFilter = d.Phase.LESchedule.Start;
                var LEFinishForFilter = d.Phase.LESchedule.Finish;

                return new DashboardController().FilterByActiveWell(param.activeWell, PhStartForFilter, PhFinishForFilter, LEStartForFilter, LEFinishForFilter);
            })
            .ToList();

            if (param.wellwithpipcheck != false)
            {
                int c = final.Count();
                int i = 0;
                while (i < c)
                {
                    int x = c - i - 1;
                    WellActivity wac = final[x].Activity;
                    WellActivityPhase wap = final[x].Phase;
                    if (!WellActivity.isHasWellPip(wac, wap))
                    {
                        final.Remove(final[x]);
                    }
                    i++;
                }
            }

            return new WaterfallBaseResult()
            {
                ListWaterfall = final,
                ListPIPElement = pips
            };
        }

        public JsonResult GetWaterfallData1(WaterfallParam param)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var division = param.DayOrCost.Equals("Day") ? 1 : 1000000;
                var data = GetWaterfallBaseData(param);
                var was = data.ListWaterfall;
                var pips = data.ListPIPElement;

                var dtStart1 = Tools.ToDateTime(param.dateStart, true);
                var dtFinish1 = Tools.ToDateTime(param.dateFinish, true);
                var dtStart2 = Tools.ToDateTime(param.dateStart2, true);
                var dtFinish2 = Tools.ToDateTime(param.dateFinish2, true);
                if (dtFinish1 == Tools.DefaultDate) dtFinish1 = new DateTime(3000, 1, 1);
                if (dtFinish2 == Tools.DefaultDate) dtFinish2 = new DateTime(3000, 1, 1);

                var TQ = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.TQ, param.DayOrCost)), division);
                var Actual = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.Actual, param.DayOrCost)), division);
                var LE = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.LE, param.DayOrCost)), division);
                var AFE = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.AFE, param.DayOrCost)), division);
                var OP = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.OP, param.DayOrCost)), division);

                var final = new List<WaterfallItem>();
                if (param.Layout.Contains("OP"))
                {
                    final.Add(new WaterfallItem(0, "OP", OP, ""));
                }

                var groupPIPS = pips.GroupBy(d => d.ToBsonDocument().GetString(param.GroupBy))
                    .Select(d => new
                    {
                        d.Key,
                        Plan = d.Sum(e => GetDayOrCost(e, param.DayOrCost, "Plan")),
                        LE = d.Sum(e => GetDayOrCost(e, param.DayOrCost, "Actual"))
                    })
                    .ToList();

                if (param.Layout.Contains("TQ"))
                {
                    if (param.Layout.Contains("OP"))
                    {
                        double gap = 0;
                        foreach (var gp in groupPIPS
                            .Where(d => (!param.IncludeZero && d.Plan != 0) || param.IncludeZero)
                            .OrderByDescending(d => d.Plan))
                        {
                            final.Add(new WaterfallItem(0.1, gp.Key + " (P)", gp.Plan, ""));
                            gap += gp.Plan;
                        }
                        if (gap + OP != TQ)
                        {
                            final.Add(new WaterfallItem(0.1, "Gaps (P)", TQ - (gap + OP), ""));
                        }
                    }
                    final.Add(new WaterfallItem(1, "TQ/Agreed Target", TQ, param.Layout.Contains("OP") ? "total" : ""));
                }

                if (param.Layout.Contains("LE"))
                {
                    if (param.Layout.Equals("OP2LE"))
                    {
                        double gap = 0;
                        foreach (var gp in groupPIPS
                            .Where(d => (!param.IncludeZero && d.LE != 0) || param.IncludeZero)
                            .OrderByDescending(d => d.LE))
                        {
                            final.Add(new WaterfallItem(1.1, gp.Key, gp.LE, ""));
                            gap += gp.LE;
                        }
                        if (gap + OP != LE)
                        {
                            final.Add(new WaterfallItem(0.1, "Gaps", LE - (gap + OP), ""));
                        }
                    }
                    else if (param.Layout.Contains("TQ2LE"))
                    {
                        double gap = 0;
                        foreach (var gp in groupPIPS
                            .Where(d => (!param.IncludeZero && (d.LE - d.Plan) != 0) || param.IncludeZero)
                            .OrderByDescending(d => (d.LE - d.Plan)))
                        {
                            final.Add(new WaterfallItem(1.1, gp.Key + " (E)", gp.LE - gp.Plan, ""));
                            gap += (gp.LE - gp.Plan);
                        }
                        if (gap + TQ != LE)
                        {
                            final.Add(new WaterfallItem(0.1, "Gaps", LE - (gap + TQ), ""));
                        }
                    }
                    final.Add(new WaterfallItem(2, "LE", LE, "total"));
                }

                var highestLine = final.Sum(d => d.Value);
                foreach (var f in final)
                {
                    f.TrendLines.Add(AFE);
                    f.TrendLines.Add(TQ);
                }

                return final;
            });
        }

        public JsonResult GetWaterfallData2(WaterfallParam param)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var division = param.DayOrCost.Equals("Day") ? 1 : 1000000;
                var data = GetWaterfallBaseData(param);
                var was = data.ListWaterfall;
                var pips = data.ListPIPElement;

                var dtStart1 = Tools.ToDateTime(param.dateStart, true);
                var dtFinish1 = Tools.ToDateTime(param.dateFinish, true);
                var dtStart2 = Tools.ToDateTime(param.dateStart2, true);
                var dtFinish2 = Tools.ToDateTime(param.dateFinish2, true);
                if (dtFinish1 == Tools.DefaultDate) dtFinish1 = new DateTime(3000, 1, 1);
                if (dtFinish2 == Tools.DefaultDate) dtFinish2 = new DateTime(3000, 1, 1);

                var TQ = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.TQ, param.DayOrCost)), division);
                var Actual = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.Actual, param.DayOrCost)), division);
                var LE = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.LE, param.DayOrCost)), division);
                var AFE = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.AFE, param.DayOrCost)), division);
                var OP = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.Plan, param.DayOrCost)), division);

                var finalTq = new List<WaterfallItem>();
                var finalLe = new List<WaterfallItem>();
                finalTq.Add(new WaterfallItem(0, "OP", OP, ""));
                finalLe.Add(new WaterfallItem(0, "OP", OP, ""));
                
                var groupPIPS = pips.GroupBy(d => d.ToBsonDocument().GetString(param.GroupBy))
                    .Select(d => new
                    {
                        d.Key,
                        Plan = d.Sum(e => GetDayOrCost(e, param.DayOrCost, "Plan")),
                        LE = d.Sum(e => GetDayOrCost(e, param.DayOrCost, "LE"))
                    })
                    .ToList();

                //-- TQ Gaps
                double gap = 0;
                foreach (var gp in groupPIPS
                    .Where(d => (!param.IncludeZero && d.Plan != 0) || param.IncludeZero)
                    .OrderByDescending(d => d.Plan))
                {
                    finalTq.Add(new WaterfallItem(0.1, gp.Key, gp.Plan, ""));
                    gap += gp.Plan;
                }
                if (gap + OP != TQ)
                {
                    finalTq.Add(new WaterfallItem(0.1, "Gaps", TQ - (gap + OP), ""));
                }
                finalTq.Add(new WaterfallItem(1, "TQ / Agreed Target", TQ, "total"));

                //-- LE & LE gaps
                gap = 0;
                foreach (var gp in groupPIPS
                    .Where(d => (!param.IncludeZero && d.LE != 0) || param.IncludeZero)
                    .OrderByDescending(d => d.LE))
                {
                    finalLe.Add(new WaterfallItem(1.1, gp.Key, gp.LE, ""));
                    gap += gp.LE;
                }
                if (gap + OP != LE)
                {
                    finalLe.Add(new WaterfallItem(0.1, "Gaps", LE - (gap + OP), ""));
                }
                finalLe.Add(new WaterfallItem(2, "LE", LE, "total"));

                foreach (var f in finalTq)
                {
                    f.TrendLines.Add(AFE);
                    f.TrendLines.Add(TQ);
                }

                foreach (var f in finalLe)
                {
                    f.TrendLines.Add(AFE);
                    f.TrendLines.Add(TQ);
                }

                double maxValue = new Double[] { OP, AFE, TQ, LE }.Max() * 1.3;
                double minValue = new Double[] { 0, OP, AFE, TQ, LE }.Min();

                return new
                {
                    MinHeight = minValue,
                    MaxHeight = maxValue,
                    OP2TQ = finalTq,
                    OP2LE = finalLe
                };
            });
        }

        public JsonResult GetWaterfallData3(WaterfallParam param)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var division = param.DayOrCost.Equals("Day") ? 1 : 1000000;
                var data = GetWaterfallBaseData(param);
                var was = data.ListWaterfall;
                var pips = data.ListPIPElement;

                var dtStart1 = Tools.ToDateTime(param.dateStart, true);
                var dtFinish1 = Tools.ToDateTime(param.dateFinish, true);
                var dtStart2 = Tools.ToDateTime(param.dateStart2, true);
                var dtFinish2 = Tools.ToDateTime(param.dateFinish2, true);
                if (dtFinish1 == Tools.DefaultDate) dtFinish1 = new DateTime(3000, 1, 1);
                if (dtFinish2 == Tools.DefaultDate) dtFinish2 = new DateTime(3000, 1, 1);

                var TQ = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.TQ, param.DayOrCost)), division);
                var Actual = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.Actual, param.DayOrCost)), division);
                var LE = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.LE, param.DayOrCost)), division);
                var AFE = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.AFE, param.DayOrCost)), division);
                var OP = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.Plan, param.DayOrCost)), division);

                var final = new List<WaterfallItem>();
                if (param.Layout.Contains("OP"))
                {
                    final.Add(new WaterfallItem(0, "OP", OP, ""));
                }

                var groupPIPS = pips.GroupBy(d => d.ToBsonDocument().GetString(param.GroupBy))
                    .Select(d => new
                    {
                        d.Key,
                        Plan = d.Sum(e => GetDayOrCost(e, param.DayOrCost, "Plan")),
                        LE = d.Sum(e => GetDayOrCost(e, param.DayOrCost, "LE"))
                    })
                    .ToList();

                if (param.Layout.Contains("TQ"))
                {
                    if (param.Layout.Contains("OP"))
                    {
                        double gap = 0;
                        foreach (var gp in groupPIPS
                            .Where(d => (!param.IncludeZero && d.Plan != 0) || param.IncludeZero)
                            .OrderByDescending(d => d.Plan))
                        {
                            final.Add(new WaterfallItem(0.1, gp.Key + " (P)", gp.Plan, ""));
                            gap += gp.Plan;
                        }
                        if (gap + OP != TQ)
                        {
                            final.Add(new WaterfallItem(0.1, "Gaps (P)", TQ - (gap + OP), ""));
                        }
                    }
                    final.Add(new WaterfallItem(1, "TQ/Agreed Target", TQ, param.Layout.Contains("OP") ? "total" : ""));
                }

                if (param.Layout.Contains("LE"))
                {
                    if (param.Layout.Equals("OP2LE"))
                    {
                        double gap = 0;
                        foreach (var gp in groupPIPS
                            .Where(d => (!param.IncludeZero && d.LE != 0) || param.IncludeZero)
                            .OrderByDescending(d => d.LE))
                        {
                            final.Add(new WaterfallItem(1.1, gp.Key, gp.LE, ""));
                            gap += gp.LE;
                        }
                        if (gap + OP != LE)
                        {
                            final.Add(new WaterfallItem(0.1, "Gaps", LE - (gap + OP), ""));
                        }
                    }
                    else if (param.Layout.Contains("TQ2LE"))
                    {
                        double gap = 0;
                        foreach (var gp in groupPIPS
                            .Where(d => (!param.IncludeZero && (d.LE - d.Plan) != 0) || param.IncludeZero)
                            .OrderByDescending(d => (d.LE - d.Plan)))
                        {
                            final.Add(new WaterfallItem(1.1, gp.Key + " (E)", gp.LE - gp.Plan, ""));
                            gap += (gp.LE - gp.Plan);
                        }
                        if (gap + TQ != LE)
                        {
                            final.Add(new WaterfallItem(0.1, "Gaps", LE - (gap + TQ), ""));
                        }
                    }
                    final.Add(new WaterfallItem(2, "LE", LE, "total"));
                }

                var highestLine = final.Sum(d => d.Value);
                foreach (var f in final)
                {
                    f.TrendLines.Add(AFE);
                    f.TrendLines.Add(TQ);
                }

                return final;
            });
        }
       
        public JsonResult GetWaterfallMultiSeries(WaterfallParam param)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var division = param.DayOrCost.Equals("Day") ? 1 : 1000000;
                var data = GetWaterfallBaseData(param);
                var was = data.ListWaterfall;
                var pips = data.ListPIPElement;

                var dtStart1 = Tools.ToDateTime(param.dateStart, true);
                var dtFinish1 = Tools.ToDateTime(param.dateFinish, true);
                var dtStart2 = Tools.ToDateTime(param.dateStart2, true);
                var dtFinish2 = Tools.ToDateTime(param.dateFinish2, true);
                if (dtFinish1 == Tools.DefaultDate) dtFinish1 = new DateTime(3000, 1, 1);
                if (dtFinish2 == Tools.DefaultDate) dtFinish2 = new DateTime(3000, 1, 1);

                var TQ = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.TQ, param.DayOrCost)), division);
                var Actual = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.Actual, param.DayOrCost)), division);
                var LE = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.LE, param.DayOrCost)), division);
                var AFE = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.AFE, param.DayOrCost)), division);
                var OP = Tools.Div(was.Sum(d => GetDayOrCost(d.Phase.Plan, param.DayOrCost)), division);

                var final = new List<WaterfallMultiSeriesItem>();
                final.Add(new WaterfallMultiSeriesItem("OP",0,OP,0));
               
                var groupPIPS = pips.GroupBy(d => d.ToBsonDocument().GetString(param.GroupBy))
                    .Select(d => new
                    {
                        d.Key,
                        Plan = d.Sum(e => GetDayOrCost(e, param.DayOrCost, "Plan")),
                        LE = d.Sum(e => GetDayOrCost(e, param.DayOrCost, "LE"))
                    })
                    .OrderBy(d=>d.Plan)
                    .ToList();
                double offset1 = final[0].Serie1;
                double offset2 = offset1;
                foreach (var p in groupPIPS)
                {
                    var i = new WaterfallMultiSeriesItem(p.Key, final.Count(), p.Plan, p.LE==0 ? p.Plan : p.LE);
                    CalcMultiSeriesItem(i, ref offset1, ref offset2);
                    final.Add(i);
                }

                var gaps = new WaterfallMultiSeriesItem("Gaps", final.Count(), 0, 0);
                gaps.Serie1 = TQ - offset1;
                gaps.Serie2 = LE - offset2;
                CalcMultiSeriesItem(gaps, ref offset1, ref offset2);
                final.Add(gaps);

                final.Add(new WaterfallMultiSeriesItem("TQ/Agreed Target", final.Count(), TQ, 0));
                final.Add(new WaterfallMultiSeriesItem("LE", final.Count(), 0, LE));
                
                //var highestLine = final.Sum(d => d.Value);
                foreach (var f in final)
                {
                    f.TrendLines.Add(AFE);
                }

                return final;
            });
        }

        private void CalcMultiSeriesItem(WaterfallMultiSeriesItem i, ref double offset1, ref double offset2)
        {
            if (i.Serie1 < 0)
            {
                offset1 = offset1 + i.Serie1;
                i.Ref1 = "Plan";
            }
            if (i.Serie2 < 0)
            {
                offset2 = offset2 + i.Serie2;
                i.Ref2 = "Plan";
            }
            i.Offset1 = offset1;
            i.Offset2 = offset2;
            if (i.Serie1 > 0)
            {
                offset1 = offset1 + i.Serie1;
                i.Ref1 = "Risk";
            }
            if (i.Serie2 > 0)
            {
                offset2 = offset2 + i.Serie2;
                i.Ref2 = "Risk";
            }
            i.Serie1 = Math.Abs(i.Serie1);
            i.Serie2 = Math.Abs(i.Serie2);
        }

        public JsonResult GetWaterfallDataForGrid(WaterfallParam param)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var division = 1000000;
                var final = GetWaterfallBaseData(param).ListWaterfall;

                var final2 = final.Select(d =>{
                    var newData = new
                    {
                        RigName = d.Activity.RigName,
                        WellName = d.Activity.WellName,
                        Start = d.Phase.PhSchedule.Start,
                        Finish = d.Phase.PhSchedule.Finish,
                        ActivityType = d.Phase.ActivityType,
                        OPCost = GetDayOrCost(d.Phase.Plan, "Cost") / division,
                        OPDays = GetDayOrCost(d.Phase.Plan, "Day"),
                        PlanPIPOppCost = d.Phase.PIPs.Sum(f => f.CostPlanImprovement),
                        PlanPIPOppDays = d.Phase.PIPs.Sum(f => f.DaysPlanImprovement),
                        PlanPIPRiskCost = d.Phase.PIPs.Sum(f => f.CostPlanRisk),
                        PlanPIPRiskDays = d.Phase.PIPs.Sum(f => f.DaysPlanRisk),
                        GapsCost = (GetDayOrCost(d.Phase.TQ, "Cost") - GetDayOrCost(d.Phase.OP, "Cost") - d.Phase.PIPs.Sum(f => f.CostPlanImprovement + f.CostPlanRisk)) / division,
                        GapsDays = GetDayOrCost(d.Phase.TQ, "Day") - GetDayOrCost(d.Phase.OP, "Day") - d.Phase.PIPs.Sum(f => f.DaysPlanImprovement + f.DaysPlanRisk),
                        TQCost = GetDayOrCost(d.Phase.TQ, "Cost") / division,
                        TQDays = GetDayOrCost(d.Phase.TQ, "Day"),
                        LECost = GetDayOrCost(d.Phase.LE, "Cost") / division,
                        LEDays = GetDayOrCost(d.Phase.LE, "Day"),
                        Type = "CE"
                    };
                    return newData;
                }).ToList();
                   

                #region add capital reduction
                bool validRig = param.isValidForCRSelection();
                if (validRig) { 
                    var WellNames_CR = final.Select(x => x.Activity.RigName + "_CR").ToList();
                    var GetPIPCR = WellPIP.Populate<WellPIP>(Query.In("WellName", new BsonArray(WellNames_CR)));
                    if (GetPIPCR != null && GetPIPCR.Count > 0)
                    {
                        foreach (var x in GetPIPCR)
                        {
                            var RigName = x.WellName.Replace("_CR", "");
                            //var getWA = WellActivity.Populate<WellActivity>(Query.EQ("RigName", RigName));
                            var periodStart = x.Elements.Min(d => d.Period.Start);
                            var periodFinish = x.Elements.Max(d => d.Period.Finish);
                            var LEDays = x.Elements.Sum(d => d.DaysCurrentWeekImprovement) + x.Elements.Sum(d => d.DaysCurrentWeekRisk);
                            var LECost = x.Elements.Sum(d => d.CostCurrentWeekImprovement) + x.Elements.Sum(d => d.CostCurrentWeekRisk);
                            var res = new
                            {
                                RigName = RigName,
                                WellName = RigName.ToUpper(),
                                Start = periodStart,
                                Finish = periodFinish,
                                ActivityType = "",
                                OPCost = 0.0,
                                OPDays = 0.0,
                                PlanPIPOppCost = x.Elements.Sum(f => f.CostPlanImprovement), //d.Phase.PIPs.Sum(f => f.CostPlanImprovement) / division,
                                PlanPIPOppDays = x.Elements.Sum(f => f.DaysPlanImprovement), //d.Phase.PIPs.Sum(f => f.DaysPlanImprovement),
                                PlanPIPRiskCost = x.Elements.Sum(f => f.CostPlanRisk), //d.Phase.PIPs.Sum(f => f.CostPlanRisk) / division,
                                PlanPIPRiskDays = x.Elements.Sum(f => f.DaysPlanRisk), //d.Phase.PIPs.Sum(f => f.DaysPlanRisk),
                                GapsCost = (0 - (x.Elements.Sum(f => f.CostPlanImprovement + f.CostPlanRisk))) / division,  //(GetDayOrCost(d.Phase.TQ, "Cost") - GetDayOrCost(d.Phase.OP, "Cost") - d.Phase.PIPs.Sum(f => f.CostPlanImprovement + f.CostPlanRisk)) / division,
                                GapsDays = (0 - (x.Elements.Sum(f => f.DaysPlanImprovement + f.DaysPlanRisk))) / division, //GetDayOrCost(d.Phase.TQ, "Day") - GetDayOrCost(d.Phase.OP, "Day") - d.Phase.PIPs.Sum(f => f.DaysPlanImprovement + f.DaysPlanRisk),
                                TQCost = 0.0, //getWA.Sum(d=>d.Phases.Sum(f=>f.TQ.Cost)) / division, //GetDayOrCost(d.Phase.TQ, "Cost") / division,
                                TQDays = 0.0, //getWA.Sum(d=>d.Phases.Sum(f=>f.TQ.Days)), //GetDayOrCost(d.Phase.TQ, "Day"),
                                LECost = LECost, //getWA.Sum(d => d.Phases.Sum(f => f.LE.Cost)) / division, //GetDayOrCost(d.Phase.LE, "Cost") / division,
                                LEDays = LEDays, //getWA.Sum(d => d.Phases.Sum(f => f.LE.Days)),//GetDayOrCost(d.Phase.LE, "Day")
                                Type = "CR"
                            };

                            final2.Add(res);
                        }
                    }
                }

                #endregion

                return final2.Where(d =>
                {
                    if (param.IncludeZero) return true;

                    if (param.DayOrCost.Equals("Day"))
                    {
                        return (d.OPDays != 0 || d.PlanPIPOppDays != 0 || d.PlanPIPRiskDays != 0 || d.GapsDays != 0 || d.TQDays != 0 || d.LEDays != 0);
                    }
                    else
                    {
                        return (d.OPCost != 0 || d.PlanPIPOppCost != 0 || d.PlanPIPRiskCost != 0 || d.GapsCost != 0 || d.TQCost != 0 || d.LECost != 0);
                    }
                }).OrderBy(x=>x.WellName).ToList();
            });
        }
        #endregion

        #region Cummulative
        private double pipCumulative(string CumulativeDataType, string what, PIPAllocation allocation)
        {
            if (CumulativeDataType.Equals("Total"))
            {
                if (what.Equals("Day"))
                    return -(allocation.DaysPlanImprovement + allocation.DaysPlanRisk);
                else if (what.Equals("LEDay"))
                    return -(allocation.LEDays == 0 ? allocation.DaysPlanImprovement + allocation.DaysPlanRisk : allocation.LEDays);
                //return -(allocation.LEDays);
                else if (what.Equals("LECost"))
                    return -(allocation.LECost == 0 ? allocation.CostPlanImprovement + allocation.CostPlanRisk : allocation.LECost);
                //return -(allocation.LECost);
                else
                    return -(allocation.CostPlanImprovement + allocation.CostPlanRisk);
            }
            else if (CumulativeDataType.Equals("Improvement"))
            {
                if (what.Equals("Day"))
                    return -allocation.DaysPlanImprovement;
                else if (what.Equals("LEDay"))
                    return -(allocation.LEDays == 0 ? allocation.DaysPlanImprovement + allocation.DaysPlanRisk : allocation.LEDays);
                //return -(allocation.LEDays);
                else if (what.Equals("LECost"))
                    return -(allocation.LECost == 0 ? allocation.CostPlanImprovement + allocation.CostPlanRisk : allocation.LECost);
                //return -(allocation.LECost);
                else
                    return -allocation.CostPlanImprovement;
            }
            else
            {
                if (what.Equals("Day"))
                    return allocation.DaysPlanRisk;
                else if (what.Equals("LEDays"))
                    return -(allocation.LEDays == 0 ? allocation.DaysPlanImprovement + allocation.DaysPlanRisk : allocation.LEDays);
                //return -(allocation.LEDays);
                else if (what.Equals("LECost"))
                    return -(allocation.LECost == 0 ? allocation.CostPlanImprovement + allocation.CostPlanRisk : allocation.LECost);
                //return -(allocation.LECost);
                else
                    return allocation.CostPlanRisk;
            }
        }

        private IMongoQuery GenerateFilterQueryForCumulative(
            List<string> operatingUnits = null,
            List<string> projectNames = null,
            List<string> regions = null,
            List<string> rigNames = null,
            List<string> rigTypes = null,
            List<string> wellNames = null,
            string activeWell = "Per Last Sequence",
            string periodBase = "By Last Sequence", List<string> exType = null
            )
        {
            List<IMongoQuery> queries = new List<IMongoQuery>();
            queries.Add(WebTools.GetWellActivitiesQuery("WellName", ""));

            if (operatingUnits != null) if (operatingUnits.Count > 0)
                    queries.Add(Query.In("OperatingUnit", new BsonArray(operatingUnits)));
            if (projectNames != null) if (projectNames.Count > 0)
                    queries.Add(Query.In("ProjectName", new BsonArray(projectNames)));
            if (regions != null) if (regions.Count > 0)
                    queries.Add(Query.In("Region", new BsonArray(regions)));
            if (rigNames != null) if (rigNames.Count > 0)
                    queries.Add(Query.In("RigName", new BsonArray(rigNames)));
            if (rigTypes != null) if (rigTypes.Count > 0)
                    queries.Add(Query.In("RigType", new BsonArray(rigTypes)));
            if (wellNames != null) if (wellNames.Count > 0)
                    queries.Add(Query.In("WellName", new BsonArray(wellNames)));

            if (exType != null) if (exType.Count > 0)
                    queries.Add(Query.In("EXType", new BsonArray(exType)));

            queries.Add(Query.GT("Phases.PhSchedule.Start", Tools.DefaultDate));

            //if (activeWell)
            //{
            //    var dateNow = DateTime.Now;
            //    queries.Add(Query.And(
            //            Query.GTE("Phases.PhSchedule.Finish", dateNow),
            //            Query.LTE("Phases.PhSchedule.Start", dateNow)
            //        ));
            //}

            return (queries.Count > 0 ? Query.And(queries.ToArray()) : null);
        }

        private bool PeriodBetween(DateTime period, string dateStart = "", string dateFinish = "")
        {
            if (!dateStart.Equals("") && !dateFinish.Equals(""))
                return (period.CompareTo(Tools.ToDateTime(dateStart, true)) >= 0) &&
                       (period.CompareTo(Tools.ToDateTime(dateFinish, true)) <= 0);
            else if (!dateStart.Equals(""))
                return (period.CompareTo(Tools.ToDateTime(dateStart, true)) >= 0);
            else if (!dateFinish.Equals(""))
                return (period.CompareTo(Tools.ToDateTime(dateFinish, true)) <= 0);
            else
                return true;




        }

        public JsonResult GetCumulativeData(
            string dateStart = "",
            string dateFinish = "",
            string dateStart2 = "",
            string dateFinish2 = "",
            string dateRelation = "AND",
            List<string> operatingUnits = null,
            List<string> projectNames = null,
            List<string> regions = null,
            List<string> rigNames = null,
            List<string> rigTypes = null,
            List<string> wellNames = null,
            List<string> activities = null,
            List<string> performanceUnits = null,
            string activeWell = "Per Last Sequence",
            string periodBase = "By Last Sequence",
            List<string> exType = null,
            int year = -1)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                bool isFilterAllocation = (year > -1);
                performanceUnits = (performanceUnits == null ? new List<string>() : performanceUnits);

                var query = GenerateFilterQueryForCumulative(operatingUnits, projectNames, regions, rigNames, rigTypes, wellNames, activeWell, periodBase, exType);
                var was = WellActivity.Populate<WellActivity>(query)
                    .SelectMany(d => d.Phases, (w, p) => new { w.WellName,w.RigName, p.ActivityType, p.PhSchedule, p.LESchedule, w.Phases });

                if (activities != null) if (activities.Count > 0)
                        was = was.Where(d => activities.Contains(d.ActivityType));

                var years = new List<string>();

                //was = was.Where(d => {
                //    var dateRange = new DateRange();

                //    if (periodBase.ToLower().Contains("sequence"))
                //    {
                //        dateRange.Start = d.PhSchedule.Start == null ? Tools.DefaultDate : d.PhSchedule.Start;
                //        dateRange.Finish = d.PhSchedule.Finish == null ? Tools.DefaultDate : d.PhSchedule.Finish;
                //    }
                //    else
                //    {
                //        dateRange.Start = d.LESchedule.Start == null ? Tools.DefaultDate : d.LESchedule.Start;
                //        dateRange.Finish = d.LESchedule.Finish == null ? Tools.DefaultDate : d.LESchedule.Finish;
                //    }

                //    return new DashboardController().dateBetween(dateRange, dateStart, dateFinish, dateStart2, dateFinish2, dateRelation)
                //        && (d.Phases.Count(e => e.LastWeek.CompareTo(Tools.DefaultDate) > 0) > 0);
                //});

                if (activeWell != "None")
                {
                    was = was.Where(d =>
                    {
                        var PhStartForFilter = d.PhSchedule.Start;
                        var PhFinishForFilter = d.PhSchedule.Finish;
                        var LEStartForFilter = d.LESchedule.Start;
                        var LEFinishForFilter = d.LESchedule.Finish;

                        return new DashboardController().FilterByActiveWell(activeWell, PhStartForFilter, PhFinishForFilter, LEStartForFilter, LEFinishForFilter);

                    });

                    //var dateNow = DateTime.Now;
                    //was = was.Where(d => dateNow <= d.PhSchedule.Finish && dateNow >= d.PhSchedule.Start);
                }
                List<PIPAllocation> pips = new List<PIPAllocation>();
                foreach (var wa in was)
                {
                    var queryPIP = Query.And(Query.EQ("WellName", wa.WellName), Query.EQ("ActivityType", wa.ActivityType));
                    var _pip = WellPIP.Populate<WellPIP>(queryPIP)
                        .SelectMany(d => d.Elements)
                        .Where(d =>
                        {
                            var isPerformanceUnit = (performanceUnits.Count > 0 ? performanceUnits.Contains(d.PerformanceUnit) : true);
                            var isPeriodStart = PeriodBetween(d.Period.Start, dateStart, dateFinish);
                            var isPerioFinish = PeriodBetween(d.Period.Finish, dateStart2, dateFinish2);
                            var isPeriod = (dateRelation.ToUpper().Equals("AND")
                                ? (isPeriodStart && isPerioFinish)
                                : (isPeriodStart || isPerioFinish));

                            return (isPerformanceUnit && isPeriod);
                        })
                        .Where(d => (performanceUnits.Count > 0 ? performanceUnits.Contains(d.PerformanceUnit) : true))
                        .SelectMany(d => d.Allocations);

                    _pip.GroupBy(d => d.Period.Year.ToString())
                        .Select(d => d.Key.ToString()).ToList()
                        .ForEach(d =>
                        {
                            if (!years.Contains(d))
                                years.Add(d);
                        });

                    var pip = _pip
                        .Where(d => (isFilterAllocation ? (d.Period.Year == year) : true))
                        .ToList();
                    pips.AddRange(pip);
                }

                bool validRig = true;
                if (wellNames != null || (performanceUnits != null && performanceUnits.Count>0) || activities != null) validRig = false;
                if (validRig)
                {
                    var WellNames_CR = was.Select(x => x.RigName + "_CR").Distinct().ToList();
                    if (WellNames_CR != null && WellNames_CR.Count > 0)
                    {
                        foreach (var wpcr in WellNames_CR)
                        {
                            var _pip = WellPIP.Populate<WellPIP>(Query.EQ("WellName", wpcr))
                            .SelectMany(d => d.Elements)
                            .Where(d =>
                            {
                                var isPerformanceUnit = (performanceUnits.Count > 0 ? performanceUnits.Contains(d.PerformanceUnit) : true);
                                var isPeriodStart = PeriodBetween(d.Period.Start, dateStart, dateFinish);
                                var isPerioFinish = PeriodBetween(d.Period.Finish, dateStart2, dateFinish2);
                                var isPeriod = (dateRelation.ToUpper().Equals("AND")
                                    ? (isPeriodStart && isPerioFinish)
                                    : (isPeriodStart || isPerioFinish));

                                return (isPerformanceUnit && isPeriod);
                            })
                            .Where(d => (performanceUnits.Count > 0 ? performanceUnits.Contains(d.PerformanceUnit) : true))
                            .SelectMany(d => d.Allocations);

                            _pip.GroupBy(d => d.Period.Year.ToString())
                                .Select(d => d.Key.ToString()).ToList()
                                .ForEach(d =>
                                {
                                    if (!years.Contains(d))
                                        years.Add(d);
                                });

                            var pip = _pip
                                .Where(d => (isFilterAllocation ? (d.Period.Year == year) : true))
                                .ToList();
                            pips.AddRange(pip);
                        }
                    }
                }

                var final = pips.GroupBy(d => new
                {
                    PeriodId = String.Format("{0:yyyyMM}", d.Period),
                    PeriodName = String.Format("{0:MMM yy}", d.Period)
                })
                .Select(d => new
                {
                    d.Key.PeriodId,
                    d.Key.PeriodName,
                    DaysImprovement = -d.Sum(e => e.DaysPlanImprovement),
                    DaysRisk = d.Sum(e => e.DaysPlanRisk),
                    Days = -d.Sum(e => e.DaysPlanImprovement + e.DaysPlanRisk),
                    Cost = -d.Sum(e => e.CostPlanImprovement + e.CostPlanRisk),
                    CostImprovement = -d.Sum(e => e.CostPlanImprovement),
                    CostRisk = d.Sum(e => e.CostPlanRisk),
                    LEDays = -d.Sum(e => e.LEDays == 0 ? e.DaysPlanImprovement + e.DaysPlanRisk : e.LEDays),
                    LECost = -d.Sum(e => e.LECost == 0 ? e.CostPlanImprovement + e.CostPlanRisk : e.LECost),
                    //LEDays = -d.Sum(e => e.DaysPlanImprovement + e.DaysPlanRisk),
                    //LECost = d.Sum(e => e.CostPlanImprovement + e.CostPlanRisk)
                }.ToBsonDocument())
                .ToList();

                double daysCum = 0;
                double LEDaysCum = 0;
                double LECostCum = 0;
                double costCum = 0;
                double daysCumImprovement = 0;
                double costCumImprovement = 0;
                double daysCumRisk = 0;
                double costCumRisk = 0;
                foreach (var d in final.OrderBy(d => d.GetDouble("PeriodId")))
                {
                    daysCum += d.GetDouble("Days");
                    daysCumImprovement += d.GetDouble("DaysImprovement");
                    daysCumRisk += d.GetDouble("DaysRisk");
                    costCum += d.GetDouble("Cost");
                    costCumImprovement += d.GetDouble("CostImprovement");
                    costCumRisk += d.GetDouble("CostRisk");
                    LEDaysCum += d.GetDouble("LEDays");
                    LECostCum += d.GetDouble("LECost");
                    d.Set("LEDays", LEDaysCum);
                    d.Set("LECost", LECostCum);
                    d.Set("Days", daysCum);
                    d.Set("DaysImprovement", daysCumImprovement);
                    d.Set("DaysRisk", daysCumRisk);
                    d.Set("Cost", costCum);
                    d.Set("CostImprovement", costCumImprovement);
                    d.Set("CostRisk", costCumRisk);
                }

                return new
                {
                    Data = DataHelper.ToDictionaryArray(final.OrderBy(d => d.GetDouble("PeriodId"))),
                    Years = years.GroupBy(d => d).Select(d => d.Key).ToList()
                };
            });
        }

        public JsonResult GetCumulativeDataForGrid(
            string dateStart = "",
            string dateFinish = "",
            string dateStart2 = "",
            string dateFinish2 = "",
            string dateRelation = "AND",
            List<string> operatingUnits = null,
            List<string> projectNames = null,
            List<string> regions = null,
            List<string> rigNames = null,
            List<string> rigTypes = null,
            List<string> wellNames = null,
            List<string> activities = null,
            List<string> performanceUnits = null,
            string CumulativeDataType = "Total",
            string activeWell = "Per Last Sequence",
            string periodBase = "By Last Sequence",
            List<string> exType = null
            )
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                performanceUnits = (performanceUnits == null ? new List<string>() : performanceUnits);

                var query = GenerateFilterQueryForCumulative(operatingUnits, projectNames, regions, rigNames, rigTypes, wellNames, activeWell, periodBase, exType);
                var was = WellActivity.Populate<WellActivity>(query)
                    .SelectMany(d => d.Phases, (w, p) => new { w.WellName, w.RigName, w.UARigSequenceId, p.ActivityType, p.PhSchedule, p.LESchedule, w.Phases });

                if (activities != null) if (activities.Count > 0)
                        was = was.Where(d => activities.Contains(d.ActivityType));

                //was = was.Where(d =>
                //{
                //    var dateRange = new DateRange();

                //    if (periodBase.ToLower().Contains("sequence"))
                //    {
                //        dateRange.Start = d.PhSchedule.Start == null ? Tools.DefaultDate : d.PhSchedule.Start;
                //        dateRange.Finish = d.PhSchedule.Finish == null ? Tools.DefaultDate : d.PhSchedule.Finish;
                //    }
                //    else
                //    {
                //        dateRange.Start = d.LESchedule.Start == null ? Tools.DefaultDate : d.LESchedule.Start;
                //        dateRange.Finish = d.LESchedule.Finish == null ? Tools.DefaultDate : d.LESchedule.Finish;
                //    }

                //    return new DashboardController().dateBetween(dateRange, dateStart, dateFinish, dateStart2, dateFinish2, dateRelation)
                //        && (d.Phases.Count(e => e.LastWeek.CompareTo(Tools.DefaultDate) > 0) > 0);
                //});

                if (activeWell != "None")
                {
                    was = was.Where(d =>
                    {
                        var PhStartForFilter = d.PhSchedule.Start;
                        var PhFinishForFilter = d.PhSchedule.Finish;
                        var LEStartForFilter = d.LESchedule.Start;
                        var LEFinishForFilter = d.LESchedule.Finish;

                        return new DashboardController().FilterByActiveWell(activeWell, PhStartForFilter, PhFinishForFilter, LEStartForFilter, LEFinishForFilter);

                    });

                    //var dateNow = DateTime.Now;
                    //was = was.Where(d => dateNow <= d.PhSchedule.Finish && dateNow >= d.PhSchedule.Start);
                }


                //if (activeWell)
                //{
                //    var dateNow = DateTime.Now;
                //    was = was.Where(d => dateNow <= d.PhSchedule.Finish && dateNow >= d.PhSchedule.Start);
                //}

                List<BsonDocument> pips = new List<BsonDocument>();
                foreach (var wa in was)
                {
                    var queryPIP = Query.And(Query.EQ("WellName", wa.WellName), Query.EQ("ActivityType", wa.ActivityType));
                    var pip = WellPIP.Populate<WellPIP>(queryPIP)
                        .SelectMany(d => d.Elements, (d, e) => new
                        {
                            WellName = d.WellName,
                            ActivityType = d.ActivityType,
                            Type = d.Type,
                            SequenceId = d.SequenceId,
                            Element = e,
                            Allocations = e.Allocations
                        })
                        .Where(d => (performanceUnits.Count > 0 ? performanceUnits.Contains(d.Element.PerformanceUnit) : true))
                        .SelectMany(d => d.Allocations, (d, e) => new
                        {
                            WellName = d.WellName,
                            ActivityType = d.ActivityType,
                            Type = d.Type,
                            SequenceId = d.SequenceId,
                            Element = d.Element,
                            Allocation = e,
                            Period = e.Period,
                            PeriodMonth = String.Format("{0:MMM}", e.Period),
                            PeriodMonthYear = String.Format("{0:MMM_yy}", e.Period),
                        })
                        .Where(d => PeriodBetween(d.Period, dateStart, dateFinish2))
                        .Select(d => d.ToBsonDocument())
                        .ToList();
                    pips.AddRange(pip);
                }

                bool validRig = true;
                if (wellNames != null || (performanceUnits != null && performanceUnits.Count>0) || activities != null) validRig = false;
                if (validRig)
                {
                    var RigNames = was.Select(x => x.RigName).Distinct().ToList();
                    foreach (var wa_cr in RigNames)
                    {
                        var queryPIP = Query.And(Query.EQ("WellName", wa_cr + "_CR"));
                        var getpip = WellPIP.Populate<WellPIP>(queryPIP);
                        if (getpip != null && getpip.Count > 0)
                        {
                            var pip = getpip.SelectMany(d => d.Elements, (d, e) => new
                                {
                                    WellName = d.WellName,
                                    ActivityType = d.ActivityType,
                                    Type = d.Type,
                                    SequenceId = d.SequenceId,
                                    Element = e,
                                    Allocations = e.Allocations
                                })
                                .Where(d => (performanceUnits.Count > 0 ? performanceUnits.Contains(d.Element.PerformanceUnit) : true))
                                .SelectMany(d => d.Allocations, (d, e) => new
                                {
                                    WellName = d.WellName,
                                    ActivityType = d.ActivityType,
                                    Type = d.Type,
                                    SequenceId = d.SequenceId,
                                    Element = d.Element,
                                    Allocation = e,
                                    Period = e.Period,
                                    PeriodMonth = String.Format("{0:MMM}", e.Period),
                                    PeriodMonthYear = String.Format("{0:MMM_yy}", e.Period),
                                })
                                .Where(d => PeriodBetween(d.Period, dateStart, dateFinish2))
                                .Select(d => d.ToBsonDocument())
                                .ToList();
                            pips.AddRange(pip);
                        }
                    }
                }

                var dataGrouped = pips
                    .GroupBy(d => new { WellName = d.GetString("WellName"), ActivityType = d.GetString("ActivityType"), Type = d.GetString("Type"), SequenceId = d.GetString("SequenceId") })
                    .OrderBy(d => d.Key.WellName)
                    .ThenBy(d => d.Key.ActivityType);

                var periods = pips
                    .OrderBy(d => d.GetDateTime("Period")) //d.Allocation.Period)
                    .Select(d => String.Format("{0:MMM_yy}", d.GetDateTime("Period")))
                    .GroupBy(d => d)
                    .Select(d => d.Key);

                List<BsonDocument> dataDays = new List<BsonDocument>();
                List<BsonDocument> dataCost = new List<BsonDocument>();
                List<BsonDocument> dataLEDays = new List<BsonDocument>();
                List<BsonDocument> dataLECost = new List<BsonDocument>();

                foreach (var eachData in dataGrouped)
                {
                    var sumCost = 0.0;
                    var sumDay = 0.0;
                    var sumLECost = 0.0;
                    var sumLEDay = 0.0;
                    var eachDocumentCost = new BsonDocument();
                    var eachDocumentDay = new BsonDocument();
                    var eachDocumentLECost = new BsonDocument();
                    var eachDocumentLEDay = new BsonDocument();

                    string RigName = "";
                    string WellName = "";
                    if (eachData.Key.Type != "Reduction")
                    {
                        var wa = was.Where(x => x.WellName == eachData.Key.WellName && 
                            x.ActivityType == eachData.Key.ActivityType && 
                            x.UARigSequenceId == eachData.Key.SequenceId).FirstOrDefault();
                        if(wa!=null)RigName = wa.RigName;
                        WellName = eachData.Key.WellName;
                    }
                    else
                    {
                        RigName = eachData.Key.WellName.Replace("_CR","");
                        WellName = eachData.Key.WellName.Replace("_CR","");
                    }
                    eachDocumentCost.Add("WellName", WellName);
                    eachDocumentCost.Add("ActivityType", eachData.Key.ActivityType);
                    eachDocumentCost.Add("Type", eachData.Key.Type);
                    eachDocumentCost.Add("RigName", RigName);
                    eachDocumentDay.Add("WellName", WellName);
                    eachDocumentDay.Add("ActivityType", eachData.Key.ActivityType);
                    eachDocumentDay.Add("Type", eachData.Key.Type);
                    eachDocumentDay.Add("RigName", RigName);
                    eachDocumentLEDay.Add("WellName", WellName);
                    eachDocumentLEDay.Add("ActivityType", eachData.Key.ActivityType);
                    eachDocumentLEDay.Add("Type", eachData.Key.Type);
                    eachDocumentLEDay.Add("RigName", RigName);
                    eachDocumentLECost.Add("ActivityType", eachData.Key.ActivityType);
                    eachDocumentLECost.Add("WellName", WellName);
                    eachDocumentLECost.Add("Type", eachData.Key.Type);
                    eachDocumentLECost.Add("RigName", RigName);

                    foreach (var eachPeriod in periods)
                    {
                        var pretotal = eachData.Where(e => e.GetString("PeriodMonthYear").Equals(eachPeriod));

                        var cost = sumCost = pretotal.Sum(e => pipCumulative(CumulativeDataType, "Cost",
                            BsonSerializer.Deserialize<PIPAllocation>(e.Get("Allocation").AsBsonDocument))) + sumCost;
                        eachDocumentCost.Add(eachPeriod, cost);

                        var day = sumDay = pretotal.Sum(e => pipCumulative(CumulativeDataType, "Day",
                            BsonSerializer.Deserialize<PIPAllocation>(e.Get("Allocation").AsBsonDocument))) + sumDay;
                        eachDocumentDay.Add(eachPeriod, day);

                        var LEDay = sumLEDay = pretotal.Sum(e => pipCumulative(CumulativeDataType, "LEDay",
                            BsonSerializer.Deserialize<PIPAllocation>(e.Get("Allocation").AsBsonDocument))) + sumLEDay;
                        eachDocumentLEDay.Add(eachPeriod, LEDay);

                        var LECost = sumLECost = pretotal.Sum(e => pipCumulative(CumulativeDataType, "LECost",
                            BsonSerializer.Deserialize<PIPAllocation>(e.Get("Allocation").AsBsonDocument))) + sumLECost;
                        eachDocumentLECost.Add(eachPeriod, LECost);
                    }

                    dataCost.Add(eachDocumentCost);
                    dataDays.Add(eachDocumentDay);
                    dataLECost.Add(eachDocumentLECost);
                    dataLEDays.Add(eachDocumentLEDay);
                }

                return new
                {
                    Periods = periods,
                    DataDay = DataHelper.ToDictionaryArray(dataDays),
                    DataCost = DataHelper.ToDictionaryArray(dataCost),
                    DataLEDay = DataHelper.ToDictionaryArray(dataLEDays),
                    DataLECost = DataHelper.ToDictionaryArray(dataLECost),
                };
            });
        }
        #endregion
    }

    class WFPIPElement : PIPElement
    {
        public string WellName { get; set; }
        public string RigName { get; set; }
    }

    public class WaterfallMultiSeriesItem
    {
        public WaterfallMultiSeriesItem(string title="", int index=0, 
            double serie1=0, double serie2=0,
            double offset1 = 0, double offset2 = 0, 
            string ref1="", string ref2="")
        {
            Title = title;
            Index = index;
            Serie1 = serie1;
            Serie2 = serie2;
            Offset1 = offset1;
            Offset2 = offset2;
            Ref1 = ref1;
            Ref2 = ref2;
        }
        public string Title { get; set; }
        public int Index { get; set; }
        public double Offset1 { get; set; }
        public double Offset2 { get; set; }
        public double Serie1 { get; set; }
        public double Serie2 { get; set; }
        public string Ref1 { get; set; }
        public string Ref2 { get; set; }

        List<double> _trendLines;
        public List<double> TrendLines
        {
            get
            {
                if (_trendLines == null) _trendLines = new List<double>();
                return _trendLines;
            }

            set
            {
                _trendLines = value;
            }
        }
    }

    public class WaterfallItem
    {
        public WaterfallItem(double categoryIndex, string title, double value, string itemType)
        {
            CategoryIndex = categoryIndex;
            Title = title;
            Value = value;
            ItemType = itemType;
        }

        List<double> _trendLines;
        public List<double> TrendLines
        {
            get
            {
                if (_trendLines == null) _trendLines = new List<double>();
                return _trendLines;
            }

            set
            {
                _trendLines = value;
            }
        }
        public double CategoryIndex { get; set; }
        public string Title { get; set; }
        public string ItemType { get; set; }
        public double Value { get; set; }
    }

    public class WaterfallParam
    {
        public string Layout { set; get; }
        public string GroupBy { set; get; }
        public string DayOrCost { set; get; }
        public bool IncludeZero { set; get; }
        public string dateStart { set; get; }
        public string dateFinish { set; get; }
        public string dateStart2 { set; get; }
        public string dateFinish2 { set; get; }
        public string dateRelation { set; get; }
        public List<string> operatingUnits { set; get; }
        public List<string> projectNames { set; get; }
        public List<string> regions { set; get; }
        public List<string> rigNames { set; get; }
        public List<string> rigTypes { set; get; }
        public List<string> wellNames { set; get; }
        public List<string> activities { set; get; }
        public List<string> performanceUnits { set; get; }
        public string activeWell { set; get; }
        public string periodBase { set; get; }
        public List<string> exType { set; get; }
        public bool wellwithpipcheck { set; get; }

        public bool isValidForCRSelection()
        {
            bool isValid = true;
            if (!(this.activities == null && this.wellNames == null && (this.performanceUnits == null || this.performanceUnits.Count==0))) isValid = false;
            return isValid;
        }

        public WaterfallParam()
        {
            Layout = "OP2LE";
            GroupBy = "Classification";
            IncludeZero = false;
            DayOrCost = "Day";
            dateStart = "";
            dateFinish = "";
            dateStart2 = "";
            dateFinish2 = "";
            dateRelation = "AND";
            operatingUnits = null;
            projectNames = null;
            regions = null;
            rigNames = null;
            rigTypes = null;
            wellNames = null;
            activities = null;
            performanceUnits = null;
            activeWell = "Per Last Sequence";
            periodBase = "By Last Sequence";
            exType = null;
            wellwithpipcheck = true;
        }
    }

    class ListWaterfall
    {
        public WellActivity Activity { set; get; }
        public WellActivityPhase Phase { set; get; }
        public ListWaterfall()
        {
            Activity = new WellActivity();
            Phase = new WellActivityPhase();
        }
    }

    class WaterfallBaseResult
    {
        public List<ListWaterfall> ListWaterfall { set; get; }
        public List<WFPIPElement> ListPIPElement { set; get; }
        public WaterfallBaseResult()
        {
            ListWaterfall = new List<ListWaterfall>();
            ListPIPElement = new List<WFPIPElement>();
        }
    }
}