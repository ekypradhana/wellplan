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
                        dateRange.Start = ph.DefaultIfEmpty(new WellActivityPhase()).Min(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Start));
                        dateRange.Finish = ph.DefaultIfEmpty(new WellActivityPhase()).Max(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Finish));
                    }
                    else
                    {
                        dateRange.Start = ph.DefaultIfEmpty(new WellActivityPhase()).Min(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Start));
                        dateRange.Finish = ph.DefaultIfEmpty(new WellActivityPhase()).Max(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Finish));
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

        private PIPElement PIPElement(PIPElement pipA, PIPElement pipB)
        {
            pipA.DaysActualImprovement += pipB.DaysActualImprovement;
            pipA.DaysActualRisk += pipB.DaysActualRisk;
            pipA.DaysCurrentWeekImprovement += pipB.DaysCurrentWeekImprovement;
            pipA.DaysCurrentWeekRisk += pipB.DaysCurrentWeekRisk;
            pipA.DaysLastWeekImprovement += pipB.DaysLastWeekImprovement;
            pipA.DaysLastWeekRisk += pipB.DaysLastWeekRisk;
            pipA.DaysPlanImprovement += pipB.DaysPlanImprovement;
            pipA.DaysPlanRisk += pipB.DaysPlanRisk;

            pipA.CostActualImprovement += pipB.CostActualImprovement;
            pipA.CostActualRisk += pipB.CostActualRisk;
            pipA.CostCurrentWeekImprovement += pipB.CostCurrentWeekImprovement;
            pipA.CostCurrentWeekRisk += pipB.CostCurrentWeekRisk;
            pipA.CostLastWeekImprovement += pipB.CostLastWeekImprovement;
            pipA.CostLastWeekRisk += pipB.CostLastWeekRisk;
            pipA.CostPlanImprovement += pipB.CostPlanImprovement;
            pipA.CostPlanRisk += pipB.CostPlanRisk;

            return pipA;
        }

        private WaterfallBaseResult GetWaterfallBaseData(WaterfallParam param)
        {
            param.performanceUnits = (param.performanceUnits == null ? new List<string>() : param.performanceUnits);

            IMongoQuery query = GenerateFilterQueryForWaterfall(param.dateStart, param.dateFinish, param.dateStart2, param.dateFinish2, param.dateRelation,
                param.operatingUnits, param.projectNames, param.regions, param.rigNames, param.rigTypes, 
                param.wellNames, param.activities, param.activeWell, param.periodBase, param.exType, param.wellwithpipcheck);

            var was = PopulateWaterfallData(query, param.dateStart, param.dateFinish, param.dateStart2, param.dateFinish2, param.dateRelation, param.activities, param.activeWell, param.periodBase, param.wellwithpipcheck);

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

            var pips = new List<WFPIPElement>();
            var crpips = new List<WFPIPElement>();

            for (var i = 0; i < final.Count; i++)
            {
                var d = final[i];

                List<IMongoQuery> qpips = new List<IMongoQuery>();
                qpips.Add(Query.EQ("WellName", d.Activity.WellName));
                qpips.Add(Query.EQ("SequenceId", d.Activity.UARigSequenceId));
                qpips.Add(Query.EQ("ActivityType", d.Phase.ActivityType));
                //qpips.Add(Query.EQ("PhaseNo", d.Phase.PhaseNo));

                d.Phase.PIPs = new List<PIPElement>();

                WellPIP wp = WellPIP.Get<WellPIP>(Query.And(qpips));
                if (wp != null)
                {
                    foreach (var pi in (wp.Elements ?? new List<PIPElement>()))
                    {
                        WFPIPElement wfpipElement = BsonSerializer.Deserialize<WFPIPElement>(pi.ToBsonDocument());
                        wfpipElement.RigName = d.Activity.RigName;
                        wfpipElement.WellName = d.Activity.WellName;
                        wfpipElement.ActivityType = d.Phase.ActivityType;

                        if (param.performanceUnits.Count > 0)
                        {
                            if (param.performanceUnits.Contains(pi.PerformanceUnit))
                            {
                                if (pips.Contains(wfpipElement))
                                    continue;

                                pips.Add(wfpipElement);
                                d.Phase.PIPs.Add(wfpipElement);
                            }
                        }
                        else
                        {
                            if (pips.Contains(wfpipElement))
                                continue;

                            pips.Add(wfpipElement);
                            d.Phase.PIPs.Add(wfpipElement);
                        }
                    }

                    foreach (var pi in (wp.CRElements ?? new List<PIPElement>()))
                    {
                        WFPIPElement wfpipElement = BsonSerializer.Deserialize<WFPIPElement>(pi.ToBsonDocument());
                        wfpipElement.RigName = d.Activity.RigName;
                        wfpipElement.WellName = d.Activity.WellName;
                        wfpipElement.ActivityType = d.Phase.ActivityType;

                        if (param.performanceUnits.Count > 0)
                        {
                            if (param.performanceUnits.Contains(pi.PerformanceUnit))
                            {
                                if (crpips.Contains(wfpipElement))
                                    continue;

                                crpips.Add(wfpipElement);
                            }
                        }
                        else
                        {
                            if (crpips.Contains(wfpipElement))
                                continue;

                            crpips.Add(wfpipElement);
                        }
                    }
                }
            }

            return new WaterfallBaseResult()
            {
                ListWaterfall = final,
                ListPIPElement = pips ?? new List<WFPIPElement>(),
                ListPIPCRElement = crpips ?? new List<WFPIPElement>()
            };
        }

        public JsonResult GetWaterfallDataAll(WaterfallParam param)
        {
            var division = param.DayOrCost.Equals("Day") ? 1 : 1000000;
            var data = GetWaterfallBaseData(param);
            var was = data.ListWaterfall;

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

            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var a = GetWaterfallData2(data, param, TQ, Actual, LE, AFE, OP);
                var b = GetWaterfallData2ByRealisedOrNot(data, param, TQ, Actual, LE, AFE, OP);
                var c = GetWaterfallDataForGrid(data, param);

                return new
                {
                    Waterfall2 = a,
                    WaterfallByRealised = b,
                    WaterfallForGrid = c,
                };
            });
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
                            final.Add(new WaterfallItem(0.1, "Gap (P)", TQ - (gap + OP), ""));
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
                            final.Add(new WaterfallItem(0.1, "Gap", LE - (gap + OP), ""));
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
                            final.Add(new WaterfallItem(0.1, "Gap", LE - (gap + TQ), ""));
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

        private WaterfallDataResult GetWaterfallData2(WaterfallBaseResult data, WaterfallParam param, double TQ, double Actual, double LE, double AFE, double OP)
        {
            try
            {
                var division = param.DayOrCost.Equals("Day") ? 1 : 1000000;
                var was = data.ListWaterfall;
                var pips = data.ListPIPElement;
                var crpips = data.ListPIPCRElement;

                var finalTq = new List<WaterfallItem>();
                var finalLe = new List<WaterfallItem>();
                finalTq.Add(new WaterfallItem(0, "OP", OP, ""));
                finalLe.Add(new WaterfallItem(0, "OP", OP, ""));
                
                var groupPIPS = pips.GroupBy(d => d.ToBsonDocument().GetString(param.GroupBy))
                    .Select(d => new WaterfallPIPRaw()
                    {
                        Key = d.Key == "" ? "All Others" : d.Key,
                        Plan = d.Sum(e => GetDayOrCost(e, param.DayOrCost, "Plan")),
                        LE = d.Sum(e => GetDayOrCost(e, param.DayOrCost, "LE"))
                    })
                    .ToList();

                if (param.IncludeCR)
                {
                    var groupCRPIPS = crpips.GroupBy(d => d.ToBsonDocument().GetString(param.GroupBy))
                        .Select(d => new WaterfallPIPRaw()
                        {
                            Key = d.Key == "" ? "All Others" : d.Key,
                            Plan = d.Sum(e => GetDayOrCost(e, param.DayOrCost, "Plan")),
                            LE = d.Sum(e => GetDayOrCost(e, param.DayOrCost, "LE"))
                        })
                        .ToList();

                    groupCRPIPS.ForEach(d =>
                    {
                        var eachPIP = groupPIPS.FirstOrDefault(e => e.Key.Equals(d.Key));
                        if (eachPIP != null)
                        {
                            eachPIP.Plan += d.Plan;
                            eachPIP.LE += d.LE;
                        }
                        else
                        {
                            groupPIPS.Add(new WaterfallPIPRaw()
                            {
                                Key = d.Key == "" ? "All Others" : d.Key,
                                Plan = d.Plan,
                                LE = d.LE
                            });
                        }
                    });
                }

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
                    finalTq.Add(new WaterfallItem(0.1, "Gap", TQ - (gap + OP), ""));
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
                    finalLe.Add(new WaterfallItem(0.1, "Gap", LE - (gap + OP), ""));
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

                double maxValue = new Double[] { OP, AFE, TQ, LE }.Max() * 1.2;
                double minValue = new Double[] { 0, OP, AFE, TQ, LE }.Min();

                return new WaterfallDataResult()
                {
                    MinHeight = minValue,
                    MaxHeight = maxValue,
                    OP2TQ = finalTq,
                    OP2LE = finalLe
                };
            }
            catch (Exception e)
            {
                return new WaterfallDataResult() { MessageIfError = e };
            }
        }

        private WaterfallDataResultByRealised GetWaterfallData2ByRealisedOrNot(WaterfallBaseResult data, WaterfallParam param, double TQ, double Actual, double LE, double AFE, double OP)
        {
            try
            {
                var division = param.DayOrCost.Equals("Day") ? 1 : 1000000;
                var was = data.ListWaterfall;
                var pips = data.ListPIPElement;
                var crpips = data.ListPIPCRElement;

                if (param.IncludeCR)
                {
                    crpips.ForEach(d =>
                    {
                        var exists = pips.FirstOrDefault(e => e.ToBsonDocument().GetString(param.GroupBy).Equals(d.ToBsonDocument().GetString(param.GroupBy)));
                        if (exists == null)
                        {
                            pips.Add(new WFPIPElement()
                            {
                                ActionParty = d.ActionParty,
                                Classification = d.Classification,
                                Completion = d.Completion,
                                PerformanceUnit = d.PerformanceUnit,
                                Theme = d.Theme,
                                Title = d.Title
                            });
                        }
                    });
                }

                var groupPIPS = pips
                    .GroupBy(d => new {
                        GroupBy = d.ToBsonDocument().GetString(param.GroupBy),
                        Completion = Convert.ToString(d.Completion)
                    })
                    .Select(d => new
                    {
                        GroupBy = d.Key.GroupBy,
                        Completion = d.Key.Completion,
                        Plan = d.Sum(e => GetDayOrCost(e, param.DayOrCost, "Plan")),
                        LE = d.Sum(e => GetDayOrCost(e, param.DayOrCost, "LE")),
                        IsPositive = (d.FirstOrDefault() ?? new WFPIPElement()).CalculateIsPositive()
                    })
                    .ToList();

                var gapLE = 0.0;
                var dataLE = groupPIPS.GroupBy(d => d.GroupBy)
                    .Select(d =>
                    {
                        gapLE += d.Sum(e => e.LE);

                        var result = new WaterfallItemByRealised()
                        {
                            Title = d.Key == "" ? "All Others" : d.Key,
                            IsPositive = d.FirstOrDefault().IsPositive
                        };

                        foreach (var eachSubGroup in d.GroupBy(e => e.Completion == "Realized" ? "Realised" : "Unrealised"))
                        {
                            if (eachSubGroup.Key == "Realised")
                                result.Realised = eachSubGroup.Sum(e => e.LE);
                            else
                                result.Unrealised = eachSubGroup.Sum(e => e.LE);
                        }

                        var crElementsReduction = crpips
                            .Where(f => f.ToBsonDocument().GetString(param.GroupBy).Equals(d.Key));

                        if (param.IncludeCR)
                        {
                            try
                            {
                                result.CRRealised = crElementsReduction
                                    .Where(f => f.Completion.Equals("Realized"))
                                    .DefaultIfEmpty(new PIPElement())
                                    .Sum(f => GetDayOrCost(f, param.DayOrCost, "LE"));
                            }
                            catch (Exception e) { }
                            try
                            {
                                result.CRUnrealised = crElementsReduction
                                    .Where(f => !f.Completion.Equals("Realized"))
                                    .DefaultIfEmpty(new PIPElement())
                                    .Sum(f => GetDayOrCost(f, param.DayOrCost, "LE"));
                            }
                            catch (Exception e) { }
                        }

                        result.Realized = result.CRRealised + result.Realised;
                        result.Unrealized = result.CRUnrealised + result.Unrealised;

                        return result;
                    })
                .Where(d => (!param.IncludeZero && (d.Realized != 0 || d.Unrealized != 0)) || param.IncludeZero)
                .ToList();

                if (param.GroupBy.Equals("Classification") && param.IncludeZero)
                {
                    var visiblePIPs = dataLE.Select(d => d.Title).Distinct();
                    WellPIPClassifications.Populate<WellPIPClassifications>().ForEach(d =>
                    {
                        if (visiblePIPs.Any(e => e.Equals(d.Name)))
                            return;

                        dataLE.Add(new WaterfallItemByRealised() { Title = d.Name });
                    });
                }

                var gapTQ = 0.0;
                var dataTQ = groupPIPS.Where(d => (!param.IncludeZero && d.Plan != 0) || param.IncludeZero)
                    .GroupBy(d => d.GroupBy)
                    .Select(d =>
                    {
                        gapTQ += d.Sum(e => e.Plan);

                        var result = new WaterfallItemByRealised()
                        {
                            Title = d.Key == "" ? "All Others" : d.Key,
                            IsPositive = d.FirstOrDefault().IsPositive
                        };

                        foreach (var eachSubGroup in d.GroupBy(e => e.Completion == "Realized" ? "Realised" : "Unrealised"))
                        {
                            if (eachSubGroup.Key == "Realised")
                                result.Realised = eachSubGroup.Sum(e => e.Plan);
                            else
                                result.Unrealised = eachSubGroup.Sum(e => e.Plan);
                        }

                        var crElementsReduction = crpips
                            .Where(f => f.ToBsonDocument().GetString(param.GroupBy).Equals(d.Key));

                        if (param.IncludeCR)
                        {
                            try
                            {
                                result.CRRealised = crElementsReduction
                                    .Where(f => f.Completion.Equals("Realized"))
                                    .DefaultIfEmpty(new PIPElement())
                                    .Sum(f => GetDayOrCost(f, param.DayOrCost, "Plan"));
                            }
                            catch (Exception e) { }
                            try
                            {
                                result.CRUnrealised = crElementsReduction
                                    .Where(f => !f.Completion.Equals("Realized"))
                                    .DefaultIfEmpty(new PIPElement())
                                    .Sum(f => GetDayOrCost(f, param.DayOrCost, "Plan"));
                            }
                            catch (Exception e) { }
                        }

                        result.Realized = result.CRRealised + result.Realised;
                        result.Unrealized = result.CRUnrealised + result.Unrealised;

                        return result;
                    })
                .Where(d => (!param.IncludeZero && (d.Realized != 0 || d.Unrealized != 0)) || param.IncludeZero)
                .ToList();

                if (param.GroupBy.Equals("Classification") && param.IncludeZero)
                {
                    var visiblePIPs = dataTQ.Select(d => d.Title).Distinct();
                    WellPIPClassifications.Populate<WellPIPClassifications>().ForEach(d =>
                    {
                        if (visiblePIPs.Any(e => e.Equals(d.Name)))
                            return;

                        dataTQ.Add(new WaterfallItemByRealised() { Title = d.Name });
                    });
                }

                return new WaterfallDataResultByRealised()
                {
                    MinHeight = (new Double[] { 0, OP, AFE, TQ, LE }.Min()),
                    MaxHeight = (new Double[] { OP, AFE, TQ, LE }.Max() * 1.2),
                    OP = OP,
                    AFE = AFE,
                    LE = LE,
                    TQ = TQ,
                    GapsLE = LE - (gapLE + OP),
                    GapsTQ = TQ - (gapTQ + OP),
                    DataLE = dataLE,
                    DataTQ = dataTQ
                };
            }
            catch (Exception e)
            {
                return new WaterfallDataResultByRealised() { MessageIfError = e };
            }
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
                            final.Add(new WaterfallItem(0.1, "Gap (P)", TQ - (gap + OP), ""));
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
                            final.Add(new WaterfallItem(0.1, "Gap", LE - (gap + OP), ""));
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
                            final.Add(new WaterfallItem(0.1, "Gap", LE - (gap + TQ), ""));
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

                var gaps = new WaterfallMultiSeriesItem("Gap", final.Count(), 0, 0);
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

        private WaterfallDataResultForGrid GetWaterfallDataForGrid(WaterfallBaseResult data, WaterfallParam param)
        {
            try
            {
                var division = 1000000;
                var final2 = data.ListWaterfall.Select(d =>
                {
                    var OPCost = GetDayOrCost(d.Phase.Plan, "Cost") / division;
                    var OPDays = GetDayOrCost(d.Phase.Plan, "Day");
                    var LECost = GetDayOrCost(d.Phase.LE, "Cost") / division;
                    var LEDays = GetDayOrCost(d.Phase.LE, "Day");
                    var TQCost = GetDayOrCost(d.Phase.TQ, "Cost") / division;
                    var TQDays = GetDayOrCost(d.Phase.TQ, "Day");

                    var LEPIPOppCost = d.Phase.PIPs.Sum(f => f.CostCurrentWeekImprovement);
                    var LEPIPOppDays = d.Phase.PIPs.Sum(f => f.DaysCurrentWeekImprovement);
                    var LEPIPRiskCost = d.Phase.PIPs.Sum(f => f.CostCurrentWeekRisk);
                    var LEPIPRiskDays = d.Phase.PIPs.Sum(f => f.DaysCurrentWeekRisk);

                    return new WaterfallGrid()
                    {
                        RigName = d.Activity.RigName,
                        WellName = d.Activity.WellName,
                        Start = d.Phase.PhSchedule.Start,
                        Finish = d.Phase.PhSchedule.Finish,
                        ActivityType = d.Phase.ActivityType,
                        OPCost = OPCost,
                        OPDays = OPDays,
                        LEPIPOppCost = LEPIPOppCost,
                        LEPIPOppDays = LEPIPOppDays,
                        LEPIPRiskCost = LEPIPRiskCost,
                        LEPIPRiskDays = LEPIPRiskDays,
                        GapsCost = (OPCost - LECost + (LEPIPOppCost + LEPIPRiskCost)),
                        GapsDays = (OPDays - LEDays + (LEPIPOppDays + LEPIPRiskDays)),
                        TQCost = TQCost,
                        TQDays = TQDays,
                        LECost = LECost,
                        LEDays = TQDays,
                        Type = "CE"
                    };
                }).ToList();

                if (param.IncludeCR)
                {
                    data.ListPIPCRElement.ForEach(d =>
                    {
                        var OPCost = 0;
                        var OPDays = 0;
                        var LECost = 0;
                        var LEDays = 0;
                        var TQCost = 0;
                        var TQDays = 0;

                        var LEPIPOppCost = d.CostCurrentWeekImprovement;
                        var LEPIPOppDays = d.DaysCurrentWeekImprovement;
                        var LEPIPRiskCost = d.CostCurrentWeekRisk;
                        var LEPIPRiskDays = d.DaysCurrentWeekRisk;

                        var target = final2.FirstOrDefault(e => e.WellName.Equals(d.WellName) && e.RigName.Equals(d.RigName) && e.ActivityType.Equals(d.ActivityType));
                        if (target == null)
                        {

                            final2.Add(new WaterfallGrid()
                            {
                                RigName = d.RigName,
                                WellName = d.WellName,
                                Start = DateTime.Now,
                                Finish = DateTime.Now,
                                ActivityType = d.ActivityType,
                                OPCost = OPCost,
                                OPDays = OPDays,
                                LEPIPOppCost = LEPIPOppCost,
                                LEPIPOppDays = LEPIPOppDays,
                                LEPIPRiskCost = LEPIPRiskCost,
                                LEPIPRiskDays = LEPIPRiskDays,
                                GapsCost = (OPCost - LECost + (LEPIPOppCost + LEPIPRiskCost)),
                                GapsDays = (OPDays - LEDays + (LEPIPOppDays + LEPIPRiskDays)),
                                TQCost = TQCost,
                                TQDays = TQDays,
                                LECost = LECost,
                                LEDays = TQDays,
                                Type = "CE"
                            });
                        }
                        else
                        {
                            target.LEPIPOppCost += LEPIPOppCost;
                            target.LEPIPOppDays += LEPIPOppDays;
                            target.LEPIPRiskCost += LEPIPRiskCost;
                            target.LEPIPRiskDays += LEPIPOppCost;
                            target.GapsCost = (target.OPCost - target.LECost + (target.LEPIPOppCost + target.LEPIPRiskCost));
                            target.GapsDays = (target.OPDays - target.LEDays + (target.LEPIPOppDays + target.LEPIPRiskDays));
                        }
                    });
                }

                var final = final2.Where(d =>
                {
                    if (param.IncludeZero) return true;

                    if (param.DayOrCost.Equals("Day"))
                    {
                        return (d.OPDays != 0 || d.LEPIPOppDays != 0 || d.LEPIPRiskDays != 0 || d.GapsDays != 0 || d.TQDays != 0 || d.LEDays != 0);
                    }
                    else
                    {
                        return (d.OPCost != 0 || d.LEPIPOppCost != 0 || d.LEPIPRiskCost != 0 || d.GapsCost != 0 || d.TQCost != 0 || d.LECost != 0);
                    }
                }).OrderBy(x => x.WellName).ToList();

                return new WaterfallDataResultForGrid() { grid = final };
            }
            catch (Exception e)
            {
                return new WaterfallDataResultForGrid() { MessageIfError = e };
            }
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
                else if (what.Equals("LECost"))
                    return -(allocation.LECost == 0 ? allocation.CostPlanImprovement + allocation.CostPlanRisk : allocation.LECost);
                else
                    return -(allocation.CostPlanImprovement + allocation.CostPlanRisk);
            }
            else if (CumulativeDataType.Equals("Improvement"))
            {
                if (what.Equals("Day"))
                    return -allocation.DaysPlanImprovement;
                else if (what.Equals("LEDay"))
                    return -(allocation.LEDays == 0 ? allocation.DaysPlanImprovement + allocation.DaysPlanRisk : allocation.LEDays);
                else if (what.Equals("LECost"))
                    return -(allocation.LECost == 0 ? allocation.CostPlanImprovement + allocation.CostPlanRisk : allocation.LECost);
                else
                    return -allocation.CostPlanImprovement;
            }
            else
            {
                if (what.Equals("Day"))
                    return allocation.DaysPlanRisk;
                else if (what.Equals("LEDays"))
                    return -(allocation.LEDays == 0 ? allocation.DaysPlanImprovement + allocation.DaysPlanRisk : allocation.LEDays);
                else if (what.Equals("LECost"))
                    return -(allocation.LECost == 0 ? allocation.CostPlanImprovement + allocation.CostPlanRisk : allocation.LECost);
                else
                    return allocation.CostPlanRisk;
            }
        }

        private IMongoQuery GenerateFilterQueryForCumulative(CumulativeFilter filter)
        {
            List<IMongoQuery> queries = new List<IMongoQuery>();
            queries.Add(WebTools.GetWellActivitiesQuery("WellName", ""));

            if (filter.operatingUnits != null) if (filter.operatingUnits.Count > 0)
                    queries.Add(Query.In("OperatingUnit", new BsonArray(filter.operatingUnits)));
            if (filter.projectNames != null) if (filter.projectNames.Count > 0)
                    queries.Add(Query.In("ProjectName", new BsonArray(filter.projectNames)));
            if (filter.regions != null) if (filter.regions.Count > 0)
                    queries.Add(Query.In("Region", new BsonArray(filter.regions)));
            if (filter.rigNames != null) if (filter.rigNames.Count > 0)
                    queries.Add(Query.In("RigName", new BsonArray(filter.rigNames)));
            if (filter.rigTypes != null) if (filter.rigTypes.Count > 0)
                    queries.Add(Query.In("RigType", new BsonArray(filter.rigTypes)));
            if (filter.wellNames != null) if (filter.wellNames.Count > 0)
                    queries.Add(Query.In("WellName", new BsonArray(filter.wellNames)));

            if (filter.exType != null) if (filter.exType.Count > 0)
                    queries.Add(Query.In("EXType", new BsonArray(filter.exType)));

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

        public JsonResult GetCumulativeDataAll(
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
            List<string> exType = null,
            int year = -1)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var filter = new CumulativeFilter()
                {
                    activeWell = activeWell,
                    activities = activities,
                    cumulativeDataType = CumulativeDataType,
                    dateFinish = dateFinish,
                    dateFinish2 = dateFinish2,
                    dateRelation = dateRelation,
                    dateStart = dateStart,
                    dateStart2 = dateStart2,
                    exType = exType,
                    operatingUnits = operatingUnits,
                    performanceUnits = (performanceUnits == null ? new List<string>() : performanceUnits),
                    periodBase = periodBase,
                    projectNames = projectNames,
                    regions = regions,
                    rigNames = rigNames,
                    rigTypes = rigTypes,
                    wellNames = wellNames,
                    year = year
                };

                var query = GenerateFilterQueryForCumulative(filter);
                var was = WellActivity.Populate<WellActivity>(query)
                    .SelectMany(d => d.Phases, (w, p) => new { w.WellName, w.RigName, w.UARigSequenceId, p.ActivityType, p.PhSchedule, p.LESchedule, w.Phases })
                    .Select(d => new CumulativeBaseResult()
                    {
                        WellName = d.WellName,
                        RigName = d.RigName,
                        UARigSequenceId = d.UARigSequenceId,
                        ActivityType = d.ActivityType,
                        PhSchedule = d.PhSchedule,
                        LESchedule = d.LESchedule,
                        Phases = d.Phases
                    });

                if (activities != null) if (activities.Count > 0)
                        was = was.Where(d => activities.Contains(d.ActivityType));

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
                }

                var data = was.ToList();

                return new
                {
                    Chart = GetCumulativeDataForChart(data, filter),
                    Grid = GetCumulativeDataForGrid(data, filter),
                };
            });
        }

        private CumulativeDataResult GetCumulativeDataForChart(List<CumulativeBaseResult> was, CumulativeFilter filter)
        {
            try
            {
                var years = new List<string>();

                bool isFilterAllocation = (filter.year > -1);

                List<PIPAllocation> pips = new List<PIPAllocation>();
                foreach (var wa in was)
                {
                    var queryPIP = Query.And(Query.EQ("WellName", wa.WellName), Query.EQ("ActivityType", wa.ActivityType));
                    var _pip = WellPIP.Populate<WellPIP>(queryPIP)
                        .SelectMany(d => d.Elements)
                        .Where(d =>
                        {
                            var isPerformanceUnit = (filter.performanceUnits.Count > 0 ? filter.performanceUnits.Contains(d.PerformanceUnit) : true);
                            var isPeriodStart = PeriodBetween(d.Period.Start, filter.dateStart, filter.dateFinish);
                            var isPerioFinish = PeriodBetween(d.Period.Finish, filter.dateStart2, filter.dateFinish2);
                            var isPeriod = (filter.dateRelation.ToUpper().Equals("AND")
                                ? (isPeriodStart && isPerioFinish)
                                : (isPeriodStart || isPerioFinish));

                            var res = (isPerformanceUnit && isPeriod);
                            return res;
                        })
                        .Where(d => (filter.performanceUnits.Count > 0 ? filter.performanceUnits.Contains(d.PerformanceUnit) : true))
                        .Where(d => Tools.DefaultDate != d.Period.Start)
                        .SelectMany(d => d.Allocations);

                    _pip.GroupBy(d => d.Period.Year.ToString())
                        .Select(d => d.Key.ToString()).ToList()
                        .ForEach(d =>
                        {
                            if (!years.Contains(d))
                                years.Add(d);
                        });

                    var pip = _pip
                        .Where(d => (isFilterAllocation ? (d.Period.Year == filter.year) : true))
                        .ToList();
                    pips.AddRange(pip);
                }

                bool validRig = true;
                if (filter.wellNames != null || (filter.performanceUnits != null && filter.performanceUnits.Count > 0) || filter.activities != null) validRig = false;
                if (validRig && filter.IncludeCR)
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
                                var isPerformanceUnit = (filter.performanceUnits.Count > 0 ? filter.performanceUnits.Contains(d.PerformanceUnit) : true);
                                var isPeriodStart = PeriodBetween(d.Period.Start, filter.dateStart, filter.dateFinish);
                                var isPerioFinish = PeriodBetween(d.Period.Finish, filter.dateStart2, filter.dateFinish2);
                                var isPeriod = (filter.dateRelation.ToUpper().Equals("AND")
                                    ? (isPeriodStart && isPerioFinish)
                                    : (isPeriodStart || isPerioFinish));

                                return (isPerformanceUnit && isPeriod);
                            })
                            .Where(d => (filter.performanceUnits.Count > 0 ? filter.performanceUnits.Contains(d.PerformanceUnit) : true))
                            .Where(d => Tools.DefaultDate != d.Period.Start)
                            .SelectMany(d => d.Allocations);

                            _pip.GroupBy(d => d.Period.Year.ToString())
                                .Select(d => d.Key.ToString()).ToList()
                                .ForEach(d =>
                                {
                                    if (!years.Contains(d))
                                        years.Add(d);
                                });

                            var pip = _pip
                                .Where(d => (isFilterAllocation ? (d.Period.Year == filter.year) : true))
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

                return new CumulativeDataResult()
                {
                    Data = DataHelper.ToDictionaryArray(final.OrderBy(d => d.GetDouble("PeriodId"))).ToList(),
                    Years = years.GroupBy(d => d).Select(d => d.Key).ToList()
                };
            }
            catch (Exception e) {
                return new CumulativeDataResult() { MessageIfError = e };
            }
        }

        private CumulativeDataResultGrid GetCumulativeDataForGrid(List<CumulativeBaseResult> was, CumulativeFilter filter)
        {
            try
            {
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
                            Allocations = e.Allocations,
                            Period = e.Period
                        })
                        .Where(d => (filter.performanceUnits.Count > 0 ? filter.performanceUnits.Contains(d.Element.PerformanceUnit) : true))
                        .Where(d => Tools.DefaultDate != d.Period.Start)
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
                        .Where(d => PeriodBetween(d.Period, filter.dateStart, filter.dateFinish2))
                        .Select(d => d.ToBsonDocument())
                        .ToList();
                    pips.AddRange(pip);
                }

                bool validRig = true;
                if (filter.wellNames != null || (filter.performanceUnits != null && filter.performanceUnits.Count > 0) || filter.activities != null) validRig = false;
                if (validRig && filter.IncludeCR)
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
                                .Where(d => (filter.performanceUnits.Count > 0 ? filter.performanceUnits.Contains(d.Element.PerformanceUnit) : true))
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
                                .Where(d => PeriodBetween(d.Period, filter.dateStart, filter.dateFinish2))
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
                        var pretotal = eachData.Where(e => e.GetString("PeriodMonthYear").Equals(eachPeriod)).DefaultIfEmpty();

                        var addCost = 0.0;
                        var addDay = 0.0;
                        var addLEDay = 0.0;
                        var addLECost = 0.0;

                        try { addCost = pretotal.Sum(e => (e == BsonNull.Value) ? 0 : pipCumulative(filter.cumulativeDataType, "Cost",
                                BsonSerializer.Deserialize<PIPAllocation>(e.Get("Allocation").AsBsonDocument))); } catch (Exception e) { }
                        try
                        {
                            addDay = pretotal.Sum(e => (e == BsonNull.Value) ? 0 : pipCumulative(filter.cumulativeDataType, "Day",
                            BsonSerializer.Deserialize<PIPAllocation>(e.Get("Allocation").AsBsonDocument))); } catch (Exception e) { }
                        try
                        {
                            addLEDay = pretotal.Sum(e => (e == BsonNull.Value) ? 0 : pipCumulative(filter.cumulativeDataType, "LEDay",
                                BsonSerializer.Deserialize<PIPAllocation>(e.Get("Allocation").AsBsonDocument))); } catch (Exception e) { }
                        try
                        {
                            addLECost = pretotal.Sum(e => (e == BsonNull.Value) ? 0 : pipCumulative(filter.cumulativeDataType, "LECost",
                                BsonSerializer.Deserialize<PIPAllocation>(e.Get("Allocation").AsBsonDocument))); } catch (Exception e) { }

                        var cost = sumCost = addCost + sumCost;
                        eachDocumentCost.Add(eachPeriod, cost);

                        var day = sumDay = addDay + sumDay;
                        eachDocumentDay.Add(eachPeriod, Convert.ToDouble(day.ToString("N4")));

                        var LEDay = sumLEDay = addLEDay + sumLEDay;
                        eachDocumentLEDay.Add(eachPeriod, Convert.ToDouble(LEDay.ToString("N4")));

                        var LECost = sumLECost = addLECost + sumLECost;
                        eachDocumentLECost.Add(eachPeriod, Convert.ToDouble(LECost.ToString("N4")));
                    }

                    dataCost.Add(eachDocumentCost);
                    dataDays.Add(eachDocumentDay);
                    dataLECost.Add(eachDocumentLECost);
                    dataLEDays.Add(eachDocumentLEDay);
                }

                return new CumulativeDataResultGrid()
                {
                    Periods = periods.ToList(),
                    DataDay = DataHelper.ToDictionaryArray(dataDays).ToList(),
                    DataCost = DataHelper.ToDictionaryArray(dataCost).ToList(),
                    DataLEDay = DataHelper.ToDictionaryArray(dataLEDays).ToList(),
                    DataLECost = DataHelper.ToDictionaryArray(dataLECost).ToList(),
                };
            }
            catch (Exception e)
            {
                return new CumulativeDataResultGrid() { MessageIfError = e };
            }
        }
        #endregion
    }

    class WFPIPElement : PIPElement
    {
        public string WellName { get; set; }
        public string RigName { get; set; }
        public string ActivityType { get; set; }
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
        public WaterfallItem()
        {

        }

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

    public class WaterfallItemRealisedOrNot : WaterfallItem
    {
        public WaterfallItemRealisedOrNot(double categoryIndex, string title, double value, string itemType, string completion)
        {
            CategoryIndex = categoryIndex;
            Title = title;
            Value = value;
            ItemType = itemType;
            Completion = completion;
        }

        public string Completion { get; set; }
    }

    public class WaterfallItemByRealised
    {
        public WaterfallItemByRealised()
        {

        }

        public WaterfallItemByRealised(string title, double realised, double unrealised, double crRealised, double crUnrealised, double realized, double unrealized, bool isPositive)
        {
            Title = title;
            Realised = realised;
            Unrealised = unrealised;
            CRRealised = crRealised;
            CRUnrealised = crUnrealised;
            Realized = realized;
            Unrealized = unrealized;
            IsPositive = isPositive;
        }

        public string Title { get; set; }
        public double Realised { get; set; }
        public double Unrealised { get; set; }
        public double CRRealised { get; set; }
        public double CRUnrealised { get; set; }
        public double Realized { get; set; }
        public double Unrealized { get; set; }
        public bool IsPositive { get; set; }
    }

    public class WaterfallParam
    {
        public string Layout { set; get; }
        public string GroupBy { set; get; }
        public string DayOrCost { set; get; }
        public bool IncludeZero { set; get; }
        public bool IncludeCR { set; get; }
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
        public List<WFPIPElement> ListPIPCRElement { set; get; }

        public WaterfallBaseResult()
        {
            ListWaterfall = new List<ListWaterfall>();
            ListPIPElement = new List<WFPIPElement>();
            ListPIPCRElement = new List<WFPIPElement>();
        }
    }

    class WaterfallPIPRaw
    {
        public string Key { set; get; }
        public double Plan { set; get; }
        public double LE { set; get; }
    }

    class WaterfallGrid
    {
        public string RigName { set; get; }
        public string WellName { set; get; }
        public DateTime Start { set; get; }
        public DateTime Finish { set; get; }
        public string ActivityType { set; get; }
        public double OPCost { set; get; }
        public double OPDays { set; get; }
        public double LEPIPOppCost { set; get; }
        public double LEPIPOppDays { set; get; }
        public double LEPIPRiskCost { set; get; }
        public double LEPIPRiskDays { set; get; }
        public double GapsCost { set; get; }
        public double GapsDays { set; get; }
        public double TQCost { set; get; }
        public double TQDays { set; get; }
        public double LECost { set; get; }
        public double LEDays { set; get; }
        public string Type { set; get; }
    }

    class WaterfallDataResult
    {
        public double MinHeight { set; get; }
        public double MaxHeight { set; get; }
        public List<WaterfallItem> OP2TQ { set; get; }
        public List<WaterfallItem> OP2LE { set; get; }
        public Exception MessageIfError { set; get; }

        public WaterfallDataResult()
        {
            OP2TQ = new List<WaterfallItem>();
            OP2LE = new List<WaterfallItem>();
        }
    }

    class WaterfallDataResultByRealised
    {
        public double MinHeight { set; get; }
        public double MaxHeight { set; get; }
        public double OP { set; get; }
        public double AFE { set; get; }
        public double LE { set; get; }
        public double TQ { set; get; }
        public double GapsLE { set; get; }
        public double GapsTQ { set; get; }
        public List<WaterfallItemByRealised> DataLE { set; get; }
        public List<WaterfallItemByRealised> DataTQ { set; get; }
        public Exception MessageIfError { set; get; }

        public WaterfallDataResultByRealised()
        {
            DataLE = new List<WaterfallItemByRealised>();
            DataTQ = new List<WaterfallItemByRealised>();
        }
    }

    class WaterfallDataResultForGrid
    {
        public List<WaterfallGrid> grid { set; get; }
        public Exception MessageIfError { set; get; }
    }

    class CumulativeFilter
    {
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
        public string cumulativeDataType { set; get; }
        public string activeWell { set; get; }
        public string periodBase { set; get; }
        public List<string> exType { set; get; }
        public bool IncludeCR { set; get; }
        public int year { set; get; }

        public CumulativeFilter()
        {
            operatingUnits = new List<string>();
            projectNames = new List<string>();
            regions = new List<string>();
            rigNames = new List<string>();
            rigTypes = new List<string>();
            wellNames = new List<string>();
            activities = new List<string>();
            exType = new List<string>();
        }
    }


    class CumulativeBaseResult
    {
        public string WellName { set; get; }
        public string RigName { set; get; }
        public string UARigSequenceId { set; get; }
        public string ActivityType { set; get; }
        public DateRange PhSchedule { set; get; }
        public DateRange LESchedule { set; get; }
        public List<WellActivityPhase> Phases { set; get; }

        public CumulativeBaseResult()
        {
            Phases = new List<WellActivityPhase>();
        }
    }

    class CumulativeDataResult
    {
        public List<Dictionary<string, object>> Data { set; get; }
        public List<string> Years { set; get; }
        public Exception MessageIfError { set; get; }

        public CumulativeDataResult()
        {
            Data = new List<Dictionary<string, object>>();
            Years = new List<string>();
        }
    }

    class CumulativeDataResultGrid
    {
        public List<string> Periods { set; get; }
        public List<Dictionary<string, object>> DataDay { set; get; }
        public List<Dictionary<string, object>> DataCost { set; get; }
        public List<Dictionary<string, object>> DataLEDay { set; get; }
        public List<Dictionary<string, object>> DataLECost { set; get; }
        public Exception MessageIfError { set; get; }

        public CumulativeDataResultGrid()
        {
            Periods = new List<string>();
            DataDay = new List<Dictionary<string, object>>();
            DataCost = new List<Dictionary<string, object>>();
            DataLEDay = new List<Dictionary<string, object>>();
            DataLECost = new List<Dictionary<string, object>>();
        }
    }
}