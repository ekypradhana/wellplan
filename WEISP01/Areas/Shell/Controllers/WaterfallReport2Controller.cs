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
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using ECIS.Identity;
using System.Text.RegularExpressions;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class WaterfallReport2Controller : Controller
    { 
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetData(WaterfallBase wb, string which)
        {
            return MvcResultInfo.Execute(() =>
            {
                var wa = wb.GetActivitiesAndPIPs();

                if (which.Equals("Waterfall"))
                {
                    var DataLE = ParseWaterfallData(wb, wa, "LE", "LE");
                    var DataTQ = ParseWaterfallData(wb, wa, "TQ/Agreed Target", "Plan");

                    var DataRealizedLE = ParseWaterfallRealizedData(wb, wa, "LE", "LE");
                    var DataRealizedTQ = ParseWaterfallRealizedData(wb, wa, "TQ/Agreed Target", "Plan");

                    var DataGrid = ParseWaterfallGridData(wb, wa);

                    return new
                    {
                        Waterfall = new
                        {
                            //OP = wa.TotalOP,
                            OP = 0, //wa.Activities.Sum(x=>x.Phases.Sum(y=>y.OPHistories.Where(d=>d.Type.Equals(wb.BaseOP)).Sum(z=>wb.DayOrCost == "Cost" ? z.OP.Cost : z.OP.Days))),
                            LE = wa.TotalLE,
                            TQ = wa.TotalTQ,
                            DataLE = DataHelper.ToDictionaryArray(DataLE),
                            DataTQ = DataHelper.ToDictionaryArray(DataTQ),
                            MaxHeight = (new Double[] { wa.TotalOP, wa.TotalLE, wa.TotalTQ }.Max() * 1.2),
                        },
                        WaterfallRealized = new
                        {
                            //OP = wa.TotalOP,
                            OP = 0,//wa.Activities.Sum(x => x.Phases.Sum(y => y.OPHistories.Where(d => d.Type.Equals(wb.BaseOP)).Sum(z => wb.DayOrCost == "Cost" ? z.OP.Cost : z.OP.Days))),
                            LE = wa.TotalLE,
                            TQ = wa.TotalTQ,
                            DataLE = DataHelper.ToDictionaryArray(DataRealizedLE),
                            DataTQ = DataHelper.ToDictionaryArray(DataRealizedTQ),
                            MaxHeight = (new Double[] { wa.TotalOP, wa.TotalLE, wa.TotalTQ }.Max() * 1.2),
                        },
                        WaterfallGrid = DataHelper.ToDictionaryArray(DataGrid)
                    };
                }
                else
                {
                    var waUnwinded = wa.Activities.SelectMany(d => d.Phases, (d, e) => new ActivityAndPhase() { Activity = d, Phase = e }).ToList();

                    var CumulativeGrid = new List<BsonDocument>();
                    var CumulativeGridColumns = new Dictionary<string, List<Dictionary<string, object>>>();
                    var CumulativeChart = new List<BsonDocument>();
                    var AllocationYears = new List<Int32>();

                    ParseCumulativeData(wb, wa, out CumulativeGrid, out CumulativeGridColumns, out CumulativeChart, out AllocationYears);

                    return new
                    {
                        CumulativeGrid = new
                        {
                            Data = DataHelper.ToDictionaryArray(CumulativeGrid),
                            Columns = CumulativeGridColumns
                        },
                        CumulativeChart = new
                        {
                            Data = DataHelper.ToDictionaryArray(CumulativeChart),
                            AllocationYears = AllocationYears
                        }
                    };
                }
            });
        }

        public List<BsonDocument> ParseWaterfallData(WaterfallBase wb, ActivitiesAndPIPs ANP, string OPorLElabel, string OPorLE)
        {
            var series = new List<BsonDocument>();
            series.Add(new { Category = "OP", Value = ANP.TotalOP, Summary = "" }.ToBsonDocument());

            var total = ("LE".Equals(OPorLElabel) ? ANP.TotalLE : ANP.TotalTQ);
            var gap = 0.0;

            var elementsLE = ANP.PIPs.SelectMany(d => d.Elements, (d, e) => new { PIP = d, Elements = e })
                .GroupBy(d =>
                {
                    if (wb.GroupBy.Equals("WellName"))
                        return d.PIP.WellName;

                    if (wb.GroupBy.Equals("RigName"))
                        return d.PIP.RigName;

                    return d.Elements.ToBsonDocument().GetString(wb.GroupBy);
                })
                .Select(d => new
                {
                    Key = d.Key == "" ? "All Others" : d.Key,
                    Value = d.Sum(e => GetDayOrCost(wb, e.Elements, OPorLE))
                })
                .Where(d => (!wb.IncludeZero && d.Value != 0) || wb.IncludeZero)
                .OrderByDescending(d => d.Value);

            foreach (var each in elementsLE)
            {
                series.Add(new { Category = each.Key, Value = each.Value, Summary = "" }.ToBsonDocument());
                gap += each.Value;
            }

            if (gap + ANP.TotalOP != total)
            {
                series.Add(new { Category = "Gap", Value = (total - (gap + ANP.TotalOP)), Summary = "" }.ToBsonDocument());
            }

            series.Add(new { Category = OPorLElabel, Value = total, Summary = "total" }.ToBsonDocument());

            return series;
        }

        public List<BsonDocument> ParseWaterfallRealizedData(WaterfallBase wb, ActivitiesAndPIPs ANP, string OPorLElabel, string OPorLE)
        {
            var series = new List<BsonDocument>();
            series.Add(new { Category = "OP", Value = ANP.TotalOP, Summary = "" }.ToBsonDocument());

            var total = ("LE".Equals(OPorLElabel) ? ANP.TotalLE : ANP.TotalTQ);
            var gap = 0.0;

            var elementsLE = ANP.PIPs
                .Select(d =>
                {
                    if (d.Elements == null)
                        d.Elements = new List<PIPElement>();

                    if (wb.IncludeCR && false)
                    {
                        if (d.CRElements == null)
                            d.CRElements = new List<PIPElement>();

                        d.Elements.AddRange(d.CRElements);
                    }

                    return d;
                })
                .SelectMany(d => d.Elements, (d, e) => new { PIP = d, Elements = e })
                .GroupBy(d =>
                {
                    if (wb.GroupBy.Equals("WellName"))
                    {
                        return new
                        {
                            GroupBy = d.PIP.WellName,
                            Realized = "Realized".Equals(Convert.ToString(d.Elements.Completion))
                        };
                    }

                    if (wb.GroupBy.Equals("RigName"))
                    {
                        return new
                        {
                            GroupBy = d.PIP.RigName,
                            Realized = "Realized".Equals(Convert.ToString(d.Elements.Completion))
                        };
                    }

                    return new
                    {
                        GroupBy = d.Elements.ToBsonDocument().GetString(wb.GroupBy),
                        Realized = "Realized".Equals(Convert.ToString(d.Elements.Completion))
                    };
                })
                .Select(d =>  new
                {
                    GroupBy = d.Key.GroupBy == "" ? "All Others" : d.Key.GroupBy,
                    Realized = d.Key.Realized,
                    Value = d.Sum(e => GetDayOrCost(wb, e.Elements, OPorLE))
                })
                .Where(d => (!wb.IncludeZero && d.Value != 0) || wb.IncludeZero)
                .OrderByDescending(d => d.Value);

            foreach (var each in elementsLE.Where(d => d.Realized).OrderBy(d => d.Value))
            {
                series.Add(new { Category = each.GroupBy, Value = each.Value, Summary = "", IsRealized = true }.ToBsonDocument());
                gap += each.Value;
            }

            if (gap + ANP.TotalOP != total)
            {
                series.Add(new { Category = "Gap to " + OPorLElabel, Value = (total - (gap + ANP.TotalOP)), Summary = "" }.ToBsonDocument());
            }

            series.Add(new { Category = OPorLElabel, Value = 0, Summary = "runningTotal" }.ToBsonDocument());

            foreach (var each in elementsLE.Where(d => !d.Realized).OrderBy(d => d.Value))
            {
                series.Add(new { Category = each.GroupBy + " ", Value = each.Value, Summary = "", IsRealized = false }.ToBsonDocument());
                gap += each.Value;
            }

            return series;
        }

        public List<BsonDocument> ParseWaterfallGridData(WaterfallBase wb, ActivitiesAndPIPs ANP)
        {
            return ANP.Activities.SelectMany(d => d.Phases, (d, e) => new { Activity = d, Phase = e })
                .Select(d =>
                {
                    var div = (wb.DayOrCost.Equals("Cost") ? 1000000.0 : 1.0);

                    var OP = d.Phase.CalcOP.ToBsonDocument().GetDouble(wb.DayOrCost) / div;
                    var TQ = d.Phase.TQ.ToBsonDocument().GetDouble(wb.DayOrCost) / div;

                    var LE = d.Phase.LE.ToBsonDocument().GetDouble(wb.DayOrCost);
                    if (LE == 0) LE = d.Phase.OP.ToBsonDocument().GetDouble(wb.DayOrCost);
                    if (LE == 0) LE = d.Phase.AFE.ToBsonDocument().GetDouble(wb.DayOrCost);

                    LE /= div;

                    var LEPIPOpp = d.Phase.PIPs.Sum(f => f.ToBsonDocument().GetDouble(wb.DayOrCost + "CurrentWeekImprovement"));
                    var LEPIPRisk = d.Phase.PIPs.Sum(f => f.ToBsonDocument().GetDouble(wb.DayOrCost + "CurrentWeekRisk"));

                    return new
                    {
                        WellName = d.Activity.WellName,
                        ActivityType = d.Phase.ActivityType,
                        RigName = d.Activity.RigName,
                        Type = "CE",
                        LEPIPOpp = LEPIPOpp,
                        LEPIPRisk = LEPIPRisk,
                        Gaps = (OP - LE + (LEPIPOpp + LEPIPRisk)),
                        OP = OP,
                        TQ = TQ,
                        LE = LE
                    }.ToBsonDocument();
                })
                .Where(d => 
                    d.GetDouble("OP") != 0 || 
                    d.GetDouble("TQ") != 0 || 
                    d.GetDouble("LE") != 0 || 
                    d.GetDouble("LEPIPOpp") != 0 || 
                    d.GetDouble("LEPIPRisk") != 0 || 
                    d.GetDouble("Gaps") != 0)
                .OrderBy(d => d.GetString("WellName"))
                .ThenBy(d => d.GetString("ActivityType"))
                .ToList();
        }

        public void ParseCumulativeData(WaterfallBase wb, ActivitiesAndPIPs ANP, out List<BsonDocument> CumulativeGrid, out Dictionary<string, List<Dictionary<string, object>>> CumulativeGridColumns, out List<BsonDocument> CumulativeChart, out List<Int32> AllocationYears)
        {
            var shouldInDate = wb.GetFilterDateRange();
            var columnsRaw = new Dictionary<string, List<BsonDocument>>()
            {
                { "Cost", CreateGridCumulativeFieldsHeader() },
                { "LECost", CreateGridCumulativeFieldsHeader() },
                { "Days", CreateGridCumulativeFieldsHeader() },
                { "LEDays", CreateGridCumulativeFieldsHeader() }
            };

            var isFirstActivity = true;

            var allRaw = ANP.Activities
                .SelectMany(d => d.Phases, (d, e) => new { d.WellName, d.RigName, Phase = e })
                .SelectMany(d => d.Phase.PIPs, (d, e) => new { d.WellName, d.RigName, d.Phase.ActivityType, Element = e })
                .Where(d =>
                {
                    var isPerformaceUnitFiltered = (wb.PerformanceUnits != null && wb.PerformanceUnits.Count > 0) ? wb.PerformanceUnits.Contains(d.Element.PerformanceUnit) : true;
                    var isPeriodFiltered = wb.FilterPeriod(d.Element.Period);

                    return isPerformaceUnitFiltered && isPeriodFiltered;
                });

            var years = allRaw.SelectMany(d => d.Element.Allocations).Where(d => d.Period.Year >= 2000).GroupBy(d => d.Period.Year).Select(d => d.Key).OrderBy(d => d).ToList();
            
            var all = allRaw.Select(d =>
                {
                    if (wb.AllocationYear != 0)
                        d.Element.Allocations = d.Element.Allocations.Where(e =>
                        {
                            var startYear = e.Period >= new DateTime(wb.AllocationYear, 1, 1);
                            var finishYear = e.Period <= new DateTime(wb.AllocationYear, 12, 31);

                            return startYear && finishYear;
                        }).ToList();

                    return d;
                });

            #region grid

            var grid = all.GroupBy(d => new { d.WellName, d.RigName, d.ActivityType })
                .Select(d =>
                {
                    var div = (wb.DayOrCost.Equals("Cost") ? 1000000.0 : 1.0);

                    var res = new
                    {
                        WellName = d.Key.WellName,
                        ActivityType = d.Key.ActivityType,
                        RigName = d.Key.RigName,
                        Type = "CE",
                    }.ToBsonDocument();

                    DateTime dateIterator;
                    DateTime dateMax;
                    
                    if (wb.AllocationYear == 0) 
                    {
                        dateIterator = shouldInDate.Start;
                        dateMax = shouldInDate.Finish;
                    } else {
                        dateIterator = Tools.ToUTC(new DateTime(wb.AllocationYear, 1, 1));
                        dateMax = Tools.ToUTC(new DateTime(wb.AllocationYear, 12, 31));
                    }

                    var allocationsRaw = d.Select(e => e.Element).SelectMany(e => e.Allocations);
                    
                    var sumAllCost = 0.0;
                    var sumAllLECost = 0.0;
                    var sumAllDays = 0.0;
                    var sumAllLEDays = 0.0;

                    while (Convert.ToInt32(dateIterator.ToString("yyyyMM")) <= Convert.ToInt32(dateMax.ToString("yyyyMM")))
                    {
                        var allocations = allocationsRaw.Where(e => e.Period != Tools.DefaultDate && e.Period <= dateIterator).DefaultIfEmpty(new PIPAllocation());

                        var cost = allocations.Sum(e => wb.CalculatePipCumulative("Cost", e));
                        var leCost = allocations.Sum(e => wb.CalculatePipCumulative("LECost", e));
                        var days = allocations.Sum(e => wb.CalculatePipCumulative("Days", e));
                        var leDays = allocations.Sum(e => wb.CalculatePipCumulative("LEDays", e));

                        var yearMonth = dateIterator.ToString("MMM_yyyy");

                        var nameOfCost = yearMonth + "_Cost";
                        var nameOfLECost = yearMonth + "_LE_Cost";
                        var nameOfDays = yearMonth + "_Days";
                        var nameOfLEDays = yearMonth + "_LE_Days";

                        res.Set(nameOfCost, cost);
                        res.Set(nameOfLECost, leCost);
                        res.Set(nameOfDays, days);
                        res.Set(nameOfLEDays, leDays);

                        if (isFirstActivity)
                        {
                            columnsRaw["Cost"].Add(CreateGridCumulativeField(nameOfCost, yearMonth));
                            columnsRaw["LECost"].Add(CreateGridCumulativeField(nameOfLECost, yearMonth));
                            columnsRaw["Days"].Add(CreateGridCumulativeField(nameOfDays, yearMonth));
                            columnsRaw["LEDays"].Add(CreateGridCumulativeField(nameOfLEDays, yearMonth));
                        }

                        sumAllCost += cost;
                        sumAllLECost += leCost;
                        sumAllDays += days;
                        sumAllLEDays += leDays;

                        dateIterator = dateIterator.AddMonths(1);
                    }

                    res.Set("ValueFlag", sumAllCost + sumAllLECost + sumAllDays + sumAllLEDays);

                    isFirstActivity = false;

                    return res;
                })
                .Where(d => d.GetDouble("ValueFlag") != 0)
                .OrderBy(d => d.GetString("WellName"))
                .ThenBy(d => d.GetString("ActivityType"))
                .ToList();

            var columnsAll = new Dictionary<string, List<Dictionary<string, object>>>();
            foreach (var each in columnsRaw)
                columnsAll[each.Key] = each.Value.Select(d => d.ToDictionary()).ToList();

            #endregion grid

            #region chart

            var rawFiltered = all.SelectMany(d => d.Element.Allocations);
            var chart = rawFiltered.GroupBy(d => new
            {
                PeriodId = d.Period.ToString("yyyyMM"),
                PeriodName = d.Period.ToString("MMM yy")
            })
            .Select(d =>
            {
                var s = rawFiltered.Where(e => Convert.ToInt32(e.Period.ToString("yyyyMM")) <= Convert.ToInt32(d.Key.PeriodId));
                var r = new
                {
                    PeriodId = d.Key.PeriodId,
                    PeriodName = d.Key.PeriodName,
                    CostImprovement = 0,
                    CostRisk = 0,
                    Cost = 0,
                    DaysImprovement = 0,
                    DaysRisk = 0,
                    Days = 0,
                    LECost = 0,
                    LEDays = 0,
                }.ToBsonDocument();

                try { r.Set("CostImprovement", -s.Sum(e => e.CostPlanImprovement)); }
                catch (Exception e) { }
                try { r.Set("CostRisk", d.Sum(e => e.CostPlanRisk)); }
                catch (Exception e) { }
                try { r.Set("Cost", -s.Sum(e => e.CostPlanImprovement + e.CostPlanRisk)); }
                catch (Exception e) { }
                try { r.Set("DaysImprovement", -s.Sum(e => e.DaysPlanImprovement)); }
                catch (Exception e) { }
                try { r.Set("DaysRisk", d.Sum(e => e.DaysPlanRisk)); }
                catch (Exception e) { }
                try { r.Set("Days", -s.Sum(e => e.DaysPlanImprovement + e.DaysPlanRisk)); }
                catch (Exception e) { }
                try { r.Set("LECost", -s.Sum(e => e.LECost)); }
                catch (Exception e) { }
                try { r.Set("LEDays", -s.Sum(e => e.LEDays)); }
                catch (Exception e) { }
                
                return r;
            })
            .OrderBy(d => d.GetInt32("PeriodId"))
            .ToList();

            #endregion

            CumulativeGridColumns = columnsAll;
            CumulativeGrid = grid;
            CumulativeChart = chart;
            AllocationYears = years;
        }

        private List<BsonDocument> CreateGridCumulativeFieldsHeader()
        {
            return new BsonDocument[] {
                new { field = "WellName", locked = true, title = "Well", width = 220 }.ToBsonDocument(),
                new { field = "ActivityType", locked = true, title = "Activity", width = 220 }.ToBsonDocument(),
                new { field = "RigName", locked = true, title = "Rig", width = 220 }.ToBsonDocument(),
                new { field = "Type", locked = true, title = "Type", width = 100 }.ToBsonDocument(),
            }.ToList();
        }

        private BsonDocument CreateGridCumulativeField(string field, string yearMonth)
        {
            return new {
                field = field,
                title = yearMonth.Replace("_", " "),
                width = 80,
                format = "{0:N1}",
                attributes = new { style = "text-align: right;" }
            }.ToBsonDocument();
        }

        public Double GetDayOrCost(WaterfallBase wb, PIPElement d, string element = "Plan")
        {
            double division = 1;
            double ret = 0;
            switch (element)
            {
                case "Plan":
                    if (wb.DayOrCost.Equals("Days")) ret = d.DaysPlanImprovement + d.DaysPlanRisk;
                    if (wb.DayOrCost.Equals("Cost")) ret = Tools.Div(d.CostPlanImprovement + d.CostPlanRisk, division);
                    break;

                case "Actual":
                    if (wb.DayOrCost.Equals("Days")) ret = d.DaysActualImprovement + d.DaysActualRisk;
                    if (wb.DayOrCost.Equals("Cost")) ret = Tools.Div(d.CostActualImprovement + d.CostActualRisk, division);
                    break;

                case "LE":
                    if (wb.DayOrCost.Equals("Days")) ret = d.DaysCurrentWeekImprovement + d.DaysCurrentWeekRisk;
                    if (wb.DayOrCost.Equals("Cost")) ret = Tools.Div(d.CostCurrentWeekImprovement + d.CostCurrentWeekRisk, division);
                    break;
            }
            return ret;
        }
	}

}