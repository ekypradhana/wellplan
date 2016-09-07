using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using System.Globalization;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using System.Collections.Specialized;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class DashboardController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public IMongoQuery GenerateQueryFromFilter(string[] operatingUnits, string[] projectNames, string[] regions, string[] rigNames, string[] rigTypes, string[] wellNames, string[] exType)
        {
            List<IMongoQuery> queries = new List<IMongoQuery>();
            queries.Add(WebTools.GetWellActivitiesQuery("WellName", ""));

            if (operatingUnits != null)
                if (operatingUnits.Length > 0)
                    queries.Add(Query.In("OperatingUnit", new BsonArray(operatingUnits)));

            if (projectNames != null)
                if (projectNames.Length > 0)
                    queries.Add(Query.In("ProjectName", new BsonArray(projectNames)));

            if (regions != null)
                if (regions.Length > 0)
                    queries.Add(Query.In("Region", new BsonArray(regions)));

            if (rigNames != null)
                if (rigNames.Length > 0)
                    queries.Add(Query.In("RigName", new BsonArray(rigNames)));

            if (rigTypes != null)
                if (rigTypes.Length > 0)
                    queries.Add(Query.In("RigType", new BsonArray(rigTypes)));

            if (wellNames != null)
                if (wellNames.Length > 0)
                    queries.Add(Query.In("WellName", new BsonArray(wellNames)));

            if (exType != null)
                if (exType.Length > 0)
                    queries.Add(Query.In("EXType", new BsonArray(exType)));

            if (queries.Count > 0)
                return Query.And(queries.ToArray());

            return null;
        }

        public IMongoQuery GenerateQueryFromFilterAndDate(string periodStartBegin, string periodStartEnd, string periodFinishBegin, string periodFinishEnd, string dateRelation, string[] operatingUnits, string[] projectNames, string[] regions, string[] rigNames, string[] rigTypes, string[] wellNames, string startDateConst = "Phases.PhSchedule.Start", string finishDateConst = "Phases.PhSchedule.Finish", string[] exType = null, bool isExpandOneWeek = false)
        {
            string format = "yyyy-MM-dd";
            dateRelation = (dateRelation == null ? "AND" : dateRelation);
            periodStartBegin = (periodStartBegin == null ? "" : periodStartBegin);
            periodStartEnd = (periodStartEnd == null ? "" : periodStartEnd);
            periodFinishBegin = (periodFinishBegin == null ? "" : periodFinishBegin);
            periodFinishEnd = (periodFinishEnd == null ? "" : periodFinishEnd);

            List<IMongoQuery> queries = new List<IMongoQuery>();
            List<IMongoQuery> dateQuery = new List<IMongoQuery>();

            var baseQuery = GenerateQueryFromFilter(operatingUnits, projectNames, regions, rigNames, rigTypes, wellNames, exType);
            CultureInfo culture = CultureInfo.InvariantCulture;

            if (!periodStartBegin.Equals("") && !periodStartEnd.Equals(""))
                dateQuery.Add(Query.And(
                    Query.GTE(startDateConst, DateTime.ParseExact(periodStartBegin, format, culture).AddDays(isExpandOneWeek ? -7 : 0)),
                    Query.LTE(startDateConst, DateTime.ParseExact(periodStartEnd, format, culture).AddDays(isExpandOneWeek ? 7 : 0))
                ));
            else if (!periodStartBegin.Equals(""))
                dateQuery.Add(
                    Query.GTE(startDateConst, DateTime.ParseExact(periodStartBegin, format, culture).AddDays(isExpandOneWeek ? -7 : 0))
                );
            else if (!periodStartEnd.Equals(""))
                dateQuery.Add(
                    Query.LTE(startDateConst, DateTime.ParseExact(periodStartEnd, format, culture).AddDays(isExpandOneWeek ? 7 : 0))
                );

            if (!periodFinishBegin.Equals("") && !periodFinishEnd.Equals(""))
                dateQuery.Add(Query.And(
                    Query.GTE(finishDateConst, DateTime.ParseExact(periodFinishBegin, format, culture).AddDays(isExpandOneWeek ? -7 : 0)),
                    Query.LTE(finishDateConst, DateTime.ParseExact(periodFinishEnd, format, culture).AddDays(isExpandOneWeek ? 7 : 0))
                ));
            else if (!periodFinishBegin.Equals(""))
                dateQuery.Add(
                    Query.GTE(finishDateConst, DateTime.ParseExact(periodFinishBegin, format, culture).AddDays(isExpandOneWeek ? -7 : 0))
                );
            else if (!periodFinishEnd.Equals(""))
                dateQuery.Add(
                    Query.LTE(finishDateConst, DateTime.ParseExact(periodFinishEnd, format, culture).AddDays(isExpandOneWeek ? 7 : 0))
                );

            if (baseQuery != null)
                queries.Add(baseQuery);

            if (dateQuery.Count > 0)
                queries.Add(dateRelation.Equals("AND") ? Query.And(dateQuery.ToArray()) : Query.Or(dateQuery.ToArray()));

            if (queries.Count > 0)
                return Query.And(queries.ToArray());

            return null;
        }

        public JsonResult GetAvailableFilter(string[] get, string key, string[] val, string[] regions, string[] operatingUnits, string[] rigTypes)
        {
            get = get ?? new string[] { };
            key = key ?? "";
            val = val ?? new string[] { };
            regions = regions ?? new string[] { };
            operatingUnits = operatingUnits ?? new string[] { };
            rigTypes = rigTypes ?? new string[] { };

            IMongoQuery query = null;

            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                List<string> rigNames = new List<string>();
                List<string> projectNames = new List<string>();
                List<string> wellNames = new List<string>();

                if (new string[] { "Region", "OperatingUnit", "RigType" }.Contains(key))
                {
                    List<IMongoQuery> queries = new List<IMongoQuery>();
                    if (regions.Length > 0)
                        queries.Add(Query.In("Region", new BsonArray(regions)));
                    if (operatingUnits.Length > 0)
                        queries.Add(Query.In("OperatingUnit", new BsonArray(operatingUnits)));
                    if (rigTypes.Length > 0)
                        queries.Add(Query.In("RigType", new BsonArray(rigTypes)));

                    if (queries.Count > 0)
                        query = Query.And(queries.ToArray());
                }
                else
                {
                    query = Query.In(key, new BsonArray(val));
                }
                List<WellActivity> activities = (query != null) ? WellActivity.Populate<WellActivity>(query) : WellActivity.Populate<WellActivity>();

                foreach (string k in get)
                {
                    if (k.Equals("RigName"))
                        rigNames = activities.Where(d => d.RigName != null).Select(d => new { d.RigName }).OrderBy(d => d.RigName).GroupBy(d => d.RigName).Select(d => d.Key).ToList();
                    else if (k.Equals("ProjectName"))
                        projectNames = activities.Where(d => d.ProjectName != null).Select(d => new { d.ProjectName }).OrderBy(d => d.ProjectName).GroupBy(d => d.ProjectName).Select(d => d.Key).ToList();
                    else if (k.Equals("WellName"))
                        wellNames = activities.Where(d => d.WellName != null).Select(d => new { d.WellName }).OrderBy(d => d.WellName).GroupBy(d => d.WellName).Select(d => d.Key).ToList();
                }

                return new
                {
                    RigNames = rigNames,
                    ProjectNames = projectNames,
                    WellNames = wellNames
                };
            });
        }

        public bool dateBetween(DateRange target, string periodStartBegin, string periodStartEnd, string periodFinishBegin, string periodFinishEnd, string dateRelation)
        {
            dateRelation = (dateRelation == null ? "AND" : dateRelation);
            periodStartBegin = (periodStartBegin == null ? "" : periodStartBegin);
            periodStartEnd = (periodStartEnd == null ? "" : periodStartEnd);
            periodFinishBegin = (periodFinishBegin == null ? "" : periodFinishBegin);
            periodFinishEnd = (periodFinishEnd == null ? "" : periodFinishEnd);

            string format = "yyyy-MM-dd";
            CultureInfo culture = CultureInfo.InvariantCulture;

            bool start = false;
            bool finish = false;

            if (!periodStartBegin.Equals("") && !periodStartEnd.Equals(""))
                start = (new DateIsland(target.Start).WeekId >= new DateIsland(DateTime.ParseExact(periodStartBegin, format, culture)).WeekId)
                    && (new DateIsland(target.Start).WeekId <= new DateIsland(DateTime.ParseExact(periodStartEnd, format, culture)).WeekId);
            else if (!periodStartBegin.Equals(""))
                start = (new DateIsland(target.Start).WeekId >= new DateIsland(DateTime.ParseExact(periodStartBegin, format, culture)).WeekId);
            else if (!periodStartEnd.Equals(""))
                start = (new DateIsland(target.Start).WeekId <= new DateIsland(DateTime.ParseExact(periodStartEnd, format, culture)).WeekId);
            else
                start = true;

            if (!periodFinishBegin.Equals("") && !periodFinishEnd.Equals(""))
                finish = (new DateIsland(target.Finish).WeekId >= new DateIsland(DateTime.ParseExact(periodFinishBegin, format, culture)).WeekId)
                    && (new DateIsland(target.Finish).WeekId <= new DateIsland(DateTime.ParseExact(periodFinishEnd, format, culture)).WeekId);
            else if (!periodFinishBegin.Equals(""))
                finish = (new DateIsland(target.Finish).WeekId >= new DateIsland(DateTime.ParseExact(periodFinishBegin, format, culture)).WeekId);
            else if (!periodFinishEnd.Equals(""))
                finish = (new DateIsland(target.Finish).WeekId <= new DateIsland(DateTime.ParseExact(periodFinishEnd, format, culture)).WeekId);
            else
                finish = true;

            return (dateRelation.Equals("AND") ? (start && finish) : (start || finish));
        }

        public bool FilterByActiveWell(string activeWell,
            // per last sequence
            DateTime PhScheduleStart, DateTime PhScheduleFinish,
            // per last estimate
            DateTime LEScheduleStart, DateTime LEScheduleFinish)
        {
            var dateNow = DateTime.Now;
            var dateNowWeek = new DateIsland(dateNow).WeekId;
            var PhStartWeek = new DateIsland(PhScheduleStart).WeekId;
            var PhFinishWeek = new DateIsland(PhScheduleFinish).WeekId;
            var LEStartWeek = new DateIsland(LEScheduleStart).WeekId;
            var LEFinishWeek = new DateIsland(LEScheduleFinish).WeekId;

            activeWell = activeWell.ToLower();

            if (activeWell.Contains("sequence") && activeWell.Contains("estimate"))
                return ((dateNowWeek <= PhFinishWeek && dateNowWeek >= PhStartWeek)
                    || (dateNowWeek <= LEFinishWeek && dateNowWeek >= LEStartWeek));
            else if (activeWell.Contains("estimate"))
                return (dateNowWeek <= LEFinishWeek && dateNowWeek >= LEStartWeek);
            else if (activeWell.Contains("sequence"))
                return (dateNowWeek <= PhFinishWeek && dateNowWeek >= PhStartWeek);


            return true;
        }

        private bool isPeriodBaseLastSequence(string periodBase)
        {
            periodBase = periodBase.ToLower();
            return (periodBase.Contains("sequence") && !periodBase.Contains("estimate"));
        }

        public JsonResult GetSequenceInfoGroupedByWellName(string periodBase, string dateStart, string dateFinish, string dateStart2, string dateFinish2, string dateRelation, string[] operatingUnits, string[] projectNames, string[] regions, string[] rigNames, string[] rigTypes, string[] wellNames, string activeWell, string[] exType, string sortBy = "days", string sortType = "asc")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var division = 1000000.0;
                var isLastSequence = isPeriodBaseLastSequence(periodBase);
                var startDateConst = (isLastSequence ? "Phases.PhSchedule.Start" : "Phases.LESchedule.Start");
                var finishDateConst = (isLastSequence ? "Phases.PhSchedule.Finish" : "Phases.LESchedule.Finish");
                var query = GenerateQueryFromFilterAndDate(dateStart, dateFinish, dateStart2, dateFinish2, dateRelation, operatingUnits, projectNames, regions, rigNames, rigTypes, wellNames, startDateConst, finishDateConst, exType, true);
                var final = WellActivity.Populate<WellActivity>(query)
                    .Where(d =>
                    {
                        var dateRange = new DateRange();

                        if (isLastSequence)
                        {
                            dateRange.Start = d.Phases.Min(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Start));
                            dateRange.Finish = d.Phases.Max(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Finish));
                        }
                        else
                        {
                            dateRange.Start = d.Phases.Min(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Start));
                            dateRange.Finish = d.Phases.Max(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Finish));
                        }

                        return dateBetween(dateRange, dateStart, dateFinish, dateStart2, dateFinish2, dateRelation)
                            && (d.Phases.Count(e => e.LastWeek.CompareTo(Tools.DefaultDate) > 0) > 0);
                    })
                    .GroupBy(d => d.WellName)
                    .Select(d =>
                    {
                        var PhStart = ((Func<IEnumerable<WellActivity>, DateTime>)(x =>
                            d.Count() > 0 ? d.Min(e => e.OpsSchedule.Start) : Tools.DefaultDate
                        ))(d.Where(e => e.OpsSchedule.Start > Tools.DefaultDate));
                        var PhFinish = ((Func<IEnumerable<WellActivity>, DateTime>)(x =>
                            d.Count() > 0 ? d.Max(e => e.OpsSchedule.Finish) : Tools.DefaultDate
                        ))(d.Where(e => e.OpsSchedule.Finish > Tools.DefaultDate));
                        var LEStart = ((Func<IEnumerable<WellActivity>, DateTime>)(x =>
                            d.Count() > 0 ? d.Min(e => e.LESchedule.Start) : Tools.DefaultDate
                        ))(d.Where(e => e.LESchedule.Start > Tools.DefaultDate));
                        var LEFinish = ((Func<IEnumerable<WellActivity>, DateTime>)(x =>
                            d.Count() > 0 ? d.Max(e => e.LESchedule.Finish) : Tools.DefaultDate
                        ))(d.Where(e => e.LESchedule.Finish > Tools.DefaultDate));

                        return new
                        {
                            RigName = d.FirstOrDefault().RigName,
                            WellName = d.Key,
                            TotalAFEDays = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).DefaultIfEmpty().Sum(f => f.CalculatedAFE().Days)),
                            TotalAFECost = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).DefaultIfEmpty().Sum(f => f.CalculatedAFE().Cost)) / division,
                            TotalLEDays = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).DefaultIfEmpty().Sum(f => f.CalculatedLE().Days)),
                            TotalLECost = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).DefaultIfEmpty().Sum(f => f.CalculatedLE().Cost)) / division,
                            TotalDuration = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).DefaultIfEmpty().Sum(f => f.CalculatedEstimate().Days)),
                            TotalCost = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).DefaultIfEmpty().Sum(f => f.CalculatedEstimate().Cost)) / division,
                            PhStartForFilter = PhStart,
                            PhFinishForFilter = PhFinish,
                            LEStartForFilter = LEStart,
                            LEFinishForFilter = LEFinish,
                            PhStart = PhStart.ToString("dd MMM yy"),
                            PhFinish = PhFinish.ToString("dd MMM yy")
                        };
                    })
                    .Where(d => FilterByActiveWell(activeWell, d.PhStartForFilter, d.PhFinishForFilter, d.LEStartForFilter, d.LEFinishForFilter));

                switch (sortBy)
                {
                    case "days":
                        if (sortType == "asc")
                            final = final.OrderBy(d => d.TotalDuration).ToList();
                        else
                            final = final.OrderByDescending(d => d.TotalDuration).ToList();
                        break;
                    case "cost":
                        if (sortType == "asc")
                            final = final.OrderBy(d => d.TotalCost).ToList();
                        else
                            final = final.OrderByDescending(d => d.TotalCost).ToList();
                        break;
                }

                double LEDaysTotal = 0;
                double LECostTotal = 0;
                double AFEDaysTotal = 0;
                double AFECostTotal = 0;
                double DurationTotal = 0;
                double CostTotal = 0;
                double DurationMin = 0;
                double CostMin = 0;
                double Min = 0;

                if (final.Count() > 0)
                {
                    LEDaysTotal = final.Sum(d => d.TotalLEDays);
                    LECostTotal = final.Sum(d => d.TotalLECost);
                    AFEDaysTotal = final.Sum(d => d.TotalAFEDays);
                    AFECostTotal = final.Sum(d => d.TotalAFECost);
                    DurationTotal = final.Sum(d => d.TotalDuration);
                    CostTotal = final.Sum(d => d.TotalCost);

                    DurationMin = final.Min(d => d.TotalDuration);
                    CostMin = final.Min(d => d.TotalCost);
                    Min = DurationMin;

                    if (Min > CostMin)
                        Min = CostMin;
                }

                return new
                {
                    Data = final,
                    LEDaysTotal,
                    LECostTotal,
                    AFECostTotal,
                    AFEDaysTotal,
                    DurationTotal,
                    CostTotal,
                    Min
                };
            });
        }

        public JsonResult GetSequenceInfoGroupedByActivity(string periodBase, string dateStart, string dateFinish, string dateStart2, string dateFinish2, string dateRelation, string[] operatingUnits, string[] projectNames, string[] regions, string[] rigNames, string[] rigTypes, string[] wellNames, string activeWell, string[] exType, string sortBy = "days", string sortType = "asc")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var division = 1000000.0;
                var isLastSequence = isPeriodBaseLastSequence(periodBase);
                var startDateConst = (isLastSequence ? "Phases.PhSchedule.Start" : "Phases.LESchedule.Start");
                var finishDateConst = (isLastSequence ? "Phases.PhSchedule.Finish" : "Phases.LESchedule.Finish");
                var query = GenerateQueryFromFilterAndDate(dateStart, dateFinish, dateStart2, dateFinish2, dateRelation, operatingUnits, projectNames, regions, rigNames, rigTypes, wellNames, startDateConst, finishDateConst, exType, true);

                var final = WellActivity.Populate<WellActivity>(query)
                    .Select(d =>
                    {
                        d.Phases = d.Phases.Where(e =>
                        {
                            var dateRange = new DateRange();

                            if (isLastSequence)
                            {
                                dateRange = e.PhSchedule;
                            }
                            else
                            {
                                dateRange = e.LESchedule;
                            }

                            return dateBetween(dateRange, dateStart, dateFinish, dateStart2, dateFinish2, dateRelation)
                                && (e.LastWeek.CompareTo(Tools.DefaultDate) > 0);
                        }).ToList();
                        return d;
                    })
                    .SelectMany(d => d.Phases, (d, e) => new
                    {
                        ActivityType = e.ActivityType,
                        WellName = d.WellName,
                        RigName = d.RigName,
                        EstimateFirstOilDate = d.EstimateFirstOilDate,
                        FirstOilDate = d.FirstOilDate,
                        PhSchedule = e.PhSchedule,
                        LESchedule = e.LESchedule,
                        Plan = e.Plan,
                        AFE = e.AFE,
                        LE = e.LE,
                        CalculatedAFE = e.CalculatedAFE(),
                        CalculatedLE = e.CalculatedLE(),
                        CalculatedEstimate = e.CalculatedEstimate()
                    })
                    .Where(d => !d.ActivityType.ToLower().Contains("risk"))
                    .Select(d =>
                    {
                        var PhStart = d.PhSchedule == null ? Tools.DefaultDate : d.PhSchedule.Start;
                        var PhFinish = d.PhSchedule == null ? Tools.DefaultDate : d.PhSchedule.Finish;
                        var LEStart = d.LESchedule == null ? Tools.DefaultDate : d.LESchedule.Start;
                        var LEFinish = d.LESchedule == null ? Tools.DefaultDate : d.LESchedule.Finish;

                        return new
                        {
                            RigName = d.RigName,
                            WellName = d.WellName,
                            ActivityType = d.ActivityType,
                            TotalAFEDays = d.CalculatedAFE.Days,
                            TotalAFECost = Tools.Div(d.CalculatedAFE.Cost, division),
                            TotalLEDays = d.CalculatedLE.Days,
                            TotalLECost = Tools.Div(d.CalculatedLE.Cost, division),
                            TotalOPDays = d.Plan.Days,
                            TotalOPCost = Tools.Div(d.Plan.Cost, division),
                            TotalDuration = d.CalculatedEstimate.Days,
                            TotalCost = Tools.Div(d.CalculatedEstimate.Cost, division),
                            TotalOPVarDays = d.CalculatedLE.Days - d.Plan.Days,
                            TotalOPVarCost = Tools.Div(d.CalculatedLE.Cost - d.Plan.Cost, division),
                            PhStartForFilter = PhStart,
                            PhFinishForFilter = PhFinish,
                            LEStartForFilter = LEStart,
                            LEFinishForFilter = LEFinish,
                            PhStart = PhStart.ToString("dd MMM yy"),
                            PhFinish = PhFinish.ToString("dd MMM yy"),
                        };
                    })
                    .Where(d => FilterByActiveWell(activeWell, d.PhStartForFilter, d.PhFinishForFilter, d.LEStartForFilter, d.LEFinishForFilter));

                switch (sortBy)
                {
                    case "days":
                        if (sortType == "asc")
                            final = final.OrderBy(d => d.TotalDuration).ToList();
                        else
                            final = final.OrderByDescending(d => d.TotalDuration).ToList();
                        break;
                    case "cost":
                        if (sortType == "asc")
                            final = final.OrderBy(d => d.TotalCost).ToList();
                        else
                            final = final.OrderByDescending(d => d.TotalCost).ToList();
                        break;
                }

                double LEDaysTotal = 0;
                double LECostTotal = 0;
                double AFEDaysTotal = 0;
                double AFECostTotal = 0;
                double OPDaysTotal = 0;
                double OPCostTotal = 0;
                double DurationTotal = 0;
                double CostTotal = 0;
                double DurationMin = 0;
                double CostMin = 0;
                double Min = 0;

                if (final.Count() > 0)
                {
                    LEDaysTotal = final.Sum(d => d.TotalLEDays);
                    LECostTotal = final.Sum(d => d.TotalLECost);
                    AFEDaysTotal = final.Sum(d => d.TotalAFEDays);
                    AFECostTotal = final.Sum(d => d.TotalAFECost);
                    OPDaysTotal = final.Sum(d => d.TotalOPDays);
                    OPCostTotal = final.Sum(d => d.TotalOPCost);
                    DurationTotal = final.Sum(d => d.TotalDuration);
                    CostTotal = final.Sum(d => d.TotalCost);

                    DurationMin = final.Min(d => d.TotalDuration);
                    CostMin = final.Min(d => d.TotalCost);
                    Min = DurationMin;

                    if (Min > CostMin)
                        Min = CostMin;
                }

                return new
                {
                    Data = final,
                    LEDaysTotal,
                    LECostTotal,
                    AFECostTotal,
                    AFEDaysTotal,
                    DurationTotal,
                    CostTotal,
                    OPDaysTotal,
                    OPCostTotal,
                    Min
                };
            });
        }

        public JsonResult GetSequenceInfoGroupedByRigName(string periodBase, string dateStart, string dateFinish, string dateStart2, string dateFinish2, string dateRelation, string[] operatingUnits, string[] projectNames, string[] regions, string[] rigNames, string[] rigTypes, string[] wellNames, string activeWell, string[] exType, string sortBy = "days", string sortType = "asc")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var division = 1000000.0;
                var isLastSequence = isPeriodBaseLastSequence(periodBase);
                var startDateConst = (isLastSequence ? "Phases.PhSchedule.Start" : "Phases.LESchedule.Start");
                var finishDateConst = (isLastSequence ? "Phases.PhSchedule.Finish" : "Phases.LESchedule.Finish");
                var query = GenerateQueryFromFilterAndDate(dateStart, dateFinish, dateStart2, dateFinish2, dateRelation, operatingUnits, projectNames, regions, rigNames, rigTypes, wellNames, startDateConst, finishDateConst, exType, true);

                var final = WellActivity.Populate<WellActivity>(query)
                    .Where(d =>
                    {
                        var dateRange = new DateRange();

                        if (isLastSequence)
                        {
                            dateRange.Start = d.Phases.Min(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Start));
                            dateRange.Finish = d.Phases.Max(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Finish));
                        }
                        else
                        {
                            dateRange.Start = d.Phases.Min(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Start));
                            dateRange.Finish = d.Phases.Max(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Finish));
                        }

                        return dateBetween(dateRange, dateStart, dateFinish, dateStart2, dateFinish2, dateRelation)
                            && (d.Phases.Count(e => e.LastWeek.CompareTo(Tools.DefaultDate) > 0) > 0);
                    })
                    .GroupBy(d => d.WellName)
                    .Select(d =>
                    {
                        var PhStart = ((Func<IEnumerable<WellActivity>, DateTime>)(x =>
                            d.Count() > 0 ? d.Min(e => e.OpsSchedule.Start) : Tools.DefaultDate
                        ))(d.Where(e => e.OpsSchedule.Start > Tools.DefaultDate));
                        var PhFinish = ((Func<IEnumerable<WellActivity>, DateTime>)(x =>
                            d.Count() > 0 ? d.Max(e => e.OpsSchedule.Finish) : Tools.DefaultDate
                        ))(d.Where(e => e.OpsSchedule.Finish > Tools.DefaultDate));
                        var LEStart = ((Func<IEnumerable<WellActivity>, DateTime>)(x =>
                            d.Count() > 0 ? d.Min(e => e.LESchedule.Start) : Tools.DefaultDate
                        ))(d.Where(e => e.LESchedule.Start > Tools.DefaultDate));
                        var LEFinish = ((Func<IEnumerable<WellActivity>, DateTime>)(x =>
                            d.Count() > 0 ? d.Max(e => e.LESchedule.Finish) : Tools.DefaultDate
                        ))(d.Where(e => e.LESchedule.Finish > Tools.DefaultDate));

                        return new
                        {
                            RigName = d.First().RigName,
                            WellName = d.Key,
                            TotalDuration = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).Sum(f => f.CalculatedEstimate().Days)),
                            TotalCost = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).Sum(f => f.CalculatedEstimate().Cost)) / division,
                            TotalAFEDays = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).Sum(f => f.CalculatedAFE().Days)),
                            TotalAFECost = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).Sum(f => f.CalculatedAFE().Cost)) / division,
                            TotalLEDays = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).Sum(f => f.CalculatedLE().Days)),
                            TotalLECost = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).Sum(f => f.CalculatedLE().Cost)) / division,
                            PhStart = PhStart,
                            PhFinish = PhFinish,
                            LEStart = LEStart,
                            LEFinish = LEFinish
                        };
                    })
                    .GroupBy(d => d.RigName)
                    .Select(d =>
                    {
                        var PhStart = d.Min(e => e.PhStart);
                        var PhFinish = d.Max(e => e.PhFinish);
                        var LEStart = d.Min(e => e.LEStart);
                        var LEFinish = d.Max(e => e.LEFinish);

                        return new
                        {
                            RigName = d.Key,
                            TotalDuration = d.Sum(e => e.TotalDuration),
                            TotalCost = d.Sum(e => e.TotalCost),
                            TotalAFEDays = d.Sum(e => e.TotalAFEDays),
                            TotalAFECost = d.Sum(e => e.TotalAFECost),
                            TotalLEDays = d.Sum(e => e.TotalLEDays),
                            TotalLECost = d.Sum(e => e.TotalLECost),
                            PhStartForFilter = PhStart,
                            PhFinishForFilter = PhFinish,
                            LEStartForFilter = LEStart,
                            LEFinishForFilter = LEFinish,
                            PhStart = PhStart.ToString("dd MMM yy"),
                            PhFinish = PhFinish.ToString("dd MMM yy")
                        };
                    })
                    .Where(d => FilterByActiveWell(activeWell, d.PhStartForFilter, d.PhFinishForFilter, d.LEStartForFilter, d.LEFinishForFilter));

                switch (sortBy)
                {
                    case "days":
                        if (sortType == "asc")
                            final = final.OrderBy(d => d.TotalDuration).ToList();
                        else
                        {
                            final = final.OrderByDescending(d => d.TotalDuration).ToList();
                        }
                        break;
                    case "cost":
                        if (sortType == "asc")
                        {
                            final = final.OrderBy(d => d.TotalCost).ToList();
                        }
                        else
                        {
                            final = final.OrderByDescending(d => d.TotalCost).ToList();
                        }
                        break;
                }
                double LEDaysTotal = 0;
                double LECostTotal = 0;
                double AFEDaysTotal = 0;
                double AFECostTotal = 0;
                double DurationTotal = 0;
                double CostTotal = 0;
                double DurationMin = 0;
                double CostMin = 0;
                double Min = 0;

                if (final.Count() > 0)
                {
                    LEDaysTotal = final.Sum(d => d.TotalLEDays);
                    LECostTotal = final.Sum(d => d.TotalLECost);
                    AFEDaysTotal = final.Sum(d => d.TotalAFEDays);
                    AFECostTotal = final.Sum(d => d.TotalAFECost);
                    DurationTotal = final.Sum(d => d.TotalDuration);
                    CostTotal = final.Sum(d => d.TotalCost);

                    DurationMin = final.Min(d => d.TotalDuration);
                    CostMin = final.Min(d => d.TotalCost);
                    Min = DurationMin;

                    if (Min > CostMin)
                        Min = CostMin;
                }

                return new
                {
                    Data = final,
                    LEDaysTotal,
                    LECostTotal,
                    AFECostTotal,
                    AFEDaysTotal,
                    DurationTotal,
                    CostTotal,
                    Min
                };
            });
        }

        public JsonResult GetSequenceInfoGroupedByRigVarianceAndOP14(string periodBase, string dateStart, string dateFinish, string dateStart2, string dateFinish2, string dateRelation, string[] operatingUnits, string[] projectNames, string[] regions, string[] rigNames, string[] rigTypes, string[] wellNames, string activeWell, string[] exType, string sortBy = "days", string sortType = "asc")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var division = 1000000.0;
                var isLastSequence = isPeriodBaseLastSequence(periodBase);
                var startDateConst = (isLastSequence ? "Phases.PhSchedule.Start" : "Phases.LESchedule.Start");
                var finishDateConst = (isLastSequence ? "Phases.PhSchedule.Finish" : "Phases.LESchedule.Finish");
                var query = GenerateQueryFromFilterAndDate(dateStart, dateFinish, dateStart2, dateFinish2, dateRelation, operatingUnits, projectNames, regions, rigNames, rigTypes, wellNames, startDateConst, finishDateConst, exType, true);

                var all = WellActivity.Populate<WellActivity>(query);

                var itemForGrid = all
                    .Where(d =>
                    {
                        var dateRange = new DateRange();

                        if (isLastSequence)
                        {
                            dateRange.Start = d.Phases.Min(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Start));
                            dateRange.Finish = d.Phases.Max(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Finish));
                        }
                        else
                        {
                            dateRange.Start = d.Phases.Min(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Start));
                            dateRange.Finish = d.Phases.Max(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Finish));
                        }

                        return dateBetween(dateRange, dateStart, dateFinish, dateStart2, dateFinish2, dateRelation)
                            && (d.Phases.Count(e => e.LastWeek.CompareTo(Tools.DefaultDate) > 0) > 0);
                    })
                    .GroupBy(d => d.WellName)
                    .Select(d =>
                    {
                        var PhStart = ((Func<IEnumerable<WellActivity>, DateTime>)(x =>
                            d.Count() > 0 ? d.Min(e => e.OpsSchedule.Start) : Tools.DefaultDate
                        ))(d.Where(e => e.OpsSchedule.Start > Tools.DefaultDate));
                        var PhFinish = ((Func<IEnumerable<WellActivity>, DateTime>)(x =>
                            d.Count() > 0 ? d.Max(e => e.OpsSchedule.Finish) : Tools.DefaultDate
                        ))(d.Where(e => e.OpsSchedule.Finish > Tools.DefaultDate));
                        var LEStart = ((Func<IEnumerable<WellActivity>, DateTime>)(x =>
                            d.Count() > 0 ? d.Min(e => e.LESchedule.Start) : Tools.DefaultDate
                        ))(d.Where(e => e.LESchedule.Start > Tools.DefaultDate));
                        var LEFinish = ((Func<IEnumerable<WellActivity>, DateTime>)(x =>
                            d.Count() > 0 ? d.Max(e => e.LESchedule.Finish) : Tools.DefaultDate
                        ))(d.Where(e => e.LESchedule.Finish > Tools.DefaultDate));

                        return new
                        {
                            RigName = d.First().RigName,
                            WellName = d.Key,
                            TotalDuration = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).Sum(f => f.CalculatedEstimate().Days)),
                            TotalCost = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).Sum(f => f.CalculatedEstimate().Cost)) / division,
                            TotalOPDays = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).Sum(f => f.Plan.Days)),
                            TotalOPCost = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).Sum(f => f.Plan.Cost)) / division,
                            TotalLEDays = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).Sum(f => f.CalculatedLE().Days)),
                            TotalLECost = d.Sum(e => e.Phases.Where(f =>
                                !f.ActivityType.ToLower().Contains("risk")).Sum(f => f.CalculatedLE().Cost)) / division,
                            PhStart = PhStart,
                            PhFinish = PhFinish,
                            LEStart = LEStart,
                            LEFinish = LEFinish
                        };
                    })
                    .GroupBy(d => d.RigName)
                    .Select(d =>
                    {
                        var PhStart = d.Min(e => e.PhStart);
                        var PhFinish = d.Max(e => e.PhFinish);
                        var LEStart = d.Min(e => e.LEStart);
                        var LEFinish = d.Max(e => e.LEFinish);

                        return new
                        {
                            RigName = d.Key,
                            TotalDuration = d.Sum(e => e.TotalDuration),
                            TotalCost = d.Sum(e => e.TotalCost),
                            TotalOPDays = d.Sum(e => e.TotalOPDays),
                            TotalOPCost = d.Sum(e => e.TotalOPCost),
                            TotalLEDays = d.Sum(e => e.TotalLEDays),
                            TotalLECost = d.Sum(e => e.TotalLECost),
                            PhStartForFilter = PhStart,
                            PhFinishForFilter = PhFinish,
                            LEStartForFilter = LEStart,
                            LEFinishForFilter = LEFinish,
                            PhStart = PhStart.ToString("dd MMM yy"),
                            PhFinish = PhFinish.ToString("dd MMM yy")
                        };
                    })
                    .Where(d => FilterByActiveWell(activeWell, d.PhStartForFilter, d.PhFinishForFilter, d.LEStartForFilter, d.LEFinishForFilter));

                List<BsonDocument> itemForChartBreakdownByWellRaw = new List<BsonDocument>();
                List<BsonDocument> itemForChartBreakdownByActivitiesRaw = new List<BsonDocument>();
                List<string> categories = new List<string>();

                var ii = 0;
                var jj = 0;
                var kk = 0;
                var colorCost = new string[] { "#f4b350", "#F9F76E", "#FFDE96" };
                var colorDuration = new string[] { "#d33", "#FF5656", "#980A0A" };

                foreach (var i in itemForGrid)
                {
                    categories.Add(i.RigName);

                    var allEachSubByWell = all
                        .Where(d => d.RigName.Equals(i.RigName))
                        .Where(d =>
                        {
                            var dateRange = new DateRange();

                            if (isLastSequence)
                            {
                                dateRange.Start = d.Phases.Min(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Start));
                                dateRange.Finish = d.Phases.Max(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Finish));
                            }
                            else
                            {
                                dateRange.Start = d.Phases.Min(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Start));
                                dateRange.Finish = d.Phases.Max(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Finish));
                            }

                            return dateBetween(dateRange, dateStart, dateFinish, dateStart2, dateFinish2, dateRelation);
                        })
                        .GroupBy(d => d.WellName)
                        .Select(d =>
                        {
                            var PhStart = d.Min(e => e.Phases.Min(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Start)));
                            var PhFinish = d.Max(e => e.Phases.Max(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Finish)));

                            return new
                            {
                                RigName = i.RigName,
                                WellName = d.Key,
                                TotalOPDays = d.Sum(e => e.Phases.Where(f =>
                                    !f.ActivityType.ToLower().Contains("risk")).Sum(f => f.AFE.Days)),
                                TotalOPCost = d.Sum(e => e.Phases.Where(f =>
                                    !f.ActivityType.ToLower().Contains("risk")).Sum(f => f.AFE.Cost)) / division,
                                TotalDuration = d.Sum(e => e.Phases.Where(f =>
                                    !f.ActivityType.ToLower().Contains("risk")).Sum(f => f.CalculatedEstimate().Days)),
                                TotalCost = d.Sum(e => e.Phases.Where(f =>
                                    !f.ActivityType.ToLower().Contains("risk")).Sum(f => f.CalculatedEstimate().Cost)) / division,
                                PhStart = PhStart,
                                PhFinish = PhFinish
                            };
                        })
                        .OrderBy(d => d.WellName);

                    jj = 0;
                    kk = 0;
                    foreach (var j in allEachSubByWell)
                    {
                        var bsonCost = new BsonDocument();
                        BsonArray dataCost = new BsonArray();
                        for (var l = 0; l < ii; l++) dataCost.Add(0);
                        dataCost.Add(j.TotalCost);
                        bsonCost.Add("name", j.WellName);
                        bsonCost.Add("data", dataCost);
                        bsonCost.Add("color", colorCost[kk % (colorCost.Count() - 1)]);
                        bsonCost.Add("stack", "Cost");
                        bsonCost.Add("suffix", "(in USD million)");
                        bsonCost.Add("rig", j.RigName);
                        itemForChartBreakdownByWellRaw.Add(bsonCost);
                        if (dataCost[dataCost.Count() - 1] > 0) kk++;

                        var bsonDuration = new BsonDocument();
                        BsonArray dataDuration = new BsonArray();
                        for (var l = 0; l < ii; l++) dataDuration.Add(0);
                        dataDuration.Add(j.TotalDuration);
                        bsonDuration.Add("name", j.WellName);
                        bsonDuration.Add("data", dataDuration);
                        bsonDuration.Add("color", colorDuration[jj % (colorDuration.Count() - 1)]);
                        bsonDuration.Add("stack", "Duration");
                        bsonDuration.Add("suffix", "days");
                        bsonDuration.Add("rig", j.RigName);
                        itemForChartBreakdownByWellRaw.Add(bsonDuration);
                        if (dataDuration[dataDuration.Count() - 1] > 0) jj++;
                    }

                    var allEachSubByActivities = all
                        .Where(d => d.RigName.Equals(i.RigName))
                        .Where(d =>
                        {
                            var dateRange = new DateRange();

                            if (isLastSequence)
                            {
                                dateRange.Start = d.Phases.Min(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Start));
                                dateRange.Finish = d.Phases.Max(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Finish));
                            }
                            else
                            {
                                dateRange.Start = d.Phases.Min(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Start));
                                dateRange.Finish = d.Phases.Max(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Finish));
                            }

                            return dateBetween(dateRange, dateStart, dateFinish, dateStart2, dateFinish2, dateRelation);
                        })
                        .SelectMany(d => d.Phases, (d, e) => new
                        {
                            RigName = i.RigName,
                            ActivityType = e.ActivityType,
                            CalculatedEstimate = e.CalculatedEstimate(),
                            PhSchedule = e.PhSchedule,
                            OP = e.OP
                        })
                        .Where(d => !d.ActivityType.ToLower().Contains("risk"))
                        .GroupBy(d => d.ActivityType)
                        .Select(d => new
                        {
                            RigName = i.RigName,
                            ActivityType = d.Key,
                            TotalOPDays = d.Sum(e => e.OP.Days),
                            TotalOPCost = d.Sum(e => e.OP.Cost) / division,
                            TotalDuration = d.Sum(e => e.CalculatedEstimate.Days),
                            TotalCost = d.Sum(e => e.CalculatedEstimate.Cost) / division,
                            PhStart = d.Min(e => e.PhSchedule.Start),
                            PhFinish = d.Max(e => e.PhSchedule.Finish)
                        })
                        .OrderBy(d => d.ActivityType);

                    jj = 0;
                    kk = 0;
                    foreach (var k in allEachSubByActivities)
                    {
                        var bsonCost = new BsonDocument();
                        BsonArray dataCost = new BsonArray();
                        for (var l = 0; l < ii; l++) dataCost.Add(0);
                        dataCost.Add(k.TotalCost);
                        bsonCost.Add("name", k.ActivityType);
                        bsonCost.Add("data", dataCost);
                        bsonCost.Add("color", colorCost[kk % (colorCost.Count() - 1)]);
                        bsonCost.Add("stack", "Cost");
                        bsonCost.Add("suffix", "(in USD million)");
                        bsonCost.Add("rig", k.RigName);
                        itemForChartBreakdownByActivitiesRaw.Add(bsonCost);
                        if (dataCost[dataCost.Count() - 1] > 0) kk++;

                        var bsonDuration = new BsonDocument();
                        BsonArray dataDuration = new BsonArray();
                        for (var l = 0; l < ii; l++) dataDuration.Add(0);
                        dataDuration.Add(k.TotalDuration);
                        bsonDuration.Add("name", k.ActivityType);
                        bsonDuration.Add("data", dataDuration);
                        bsonDuration.Add("color", colorDuration[jj % (colorDuration.Count() - 1)]);
                        bsonDuration.Add("stack", "Duration");
                        bsonDuration.Add("suffix", "days");
                        bsonDuration.Add("rig", k.RigName);
                        itemForChartBreakdownByActivitiesRaw.Add(bsonDuration);
                        if (dataDuration[dataDuration.Count() - 1] > 0) jj++;
                    }

                    ii++;
                }

                switch (sortBy)
                {
                    case "days":
                        if (sortType == "asc")
                            itemForGrid = itemForGrid.OrderBy(d => d.TotalDuration).ToList();
                        else
                            itemForGrid = itemForGrid.OrderByDescending(d => d.TotalDuration).ToList();
                        break;
                    case "cost":
                        if (sortType == "asc")
                            itemForGrid = itemForGrid.OrderBy(d => d.TotalCost).ToList();
                        else
                            itemForGrid = itemForGrid.OrderByDescending(d => d.TotalCost).ToList();
                        break;
                }

                var LEDaysTotal = itemForGrid.Sum(d => d.TotalLEDays);
                var LECostTotal = itemForGrid.Sum(d => d.TotalLECost);
                var OPDaysTotal = itemForGrid.Sum(d => d.TotalOPDays);
                var OPCostTotal = itemForGrid.Sum(d => d.TotalOPCost);

                var DurationTotal = itemForGrid.Sum(d => d.TotalDuration);
                var CostTotal = itemForGrid.Sum(d => d.TotalCost);
                return new
                {
                    DurationTotal,
                    CostTotal,
                    LEDaysTotal,
                    LECostTotal,
                    OPDaysTotal,
                    OPCostTotal,
                    GridItems = itemForGrid,
                    RigVarianceAndOP14ByActivity = new
                    {
                        Series = DataHelper.ToDictionaryArray(itemForChartBreakdownByActivitiesRaw),
                        Categories = categories
                    },
                    RigVarianceAndOP14ByWellName = new
                    {
                        Series = DataHelper.ToDictionaryArray(itemForChartBreakdownByWellRaw),
                        Categories = categories
                    }
                };
            });
        }

        public JsonResult GetSequenceInfoGroupedByWellProducingDay(string periodBase, string dateStart, string dateFinish, string dateStart2, string dateFinish2, string dateRelation, string[] operatingUnits, string[] projectNames, string[] regions, string[] rigNames, string[] rigTypes, string[] wellNames, string activeWell, string[] exType)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var isLastSequence = isPeriodBaseLastSequence(periodBase);
                var startDateConst = (isLastSequence ? "Phases.PhSchedule.Start" : "Phases.LESchedule.Start");
                var finishDateConst = (isLastSequence ? "Phases.PhSchedule.Finish" : "Phases.LESchedule.Finish");
                var query = GenerateQueryFromFilterAndDate(dateStart, dateFinish, dateStart2, dateFinish2, dateRelation, operatingUnits, projectNames, regions, rigNames, rigTypes, wellNames, startDateConst, finishDateConst, exType, true);
                var final = WellActivity.Populate<WellActivity>(query)
                    .Where(d =>
                    {
                        var dateRange = new DateRange();

                        if (isLastSequence)
                        {
                            dateRange.Start = d.Phases.Min(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Start));
                            dateRange.Finish = d.Phases.Max(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Finish));
                        }
                        else
                        {
                            dateRange.Start = d.Phases.Min(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Start));
                            dateRange.Finish = d.Phases.Max(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Finish));
                        }

                        return dateBetween(dateRange, dateStart, dateFinish, dateStart2, dateFinish2, dateRelation);
                    })
                    .GroupBy(d => d.WellName)
                    .Select(d =>
                    {
                        var estimateFirstOilDate = d.Max(e => e.EstimateFirstOilDate);
                        var lastDateOfEstimateFirstOilDateYear = new DateTime(estimateFirstOilDate.Year, 12, 31);
                        var estimateTotalDays = (lastDateOfEstimateFirstOilDateYear.Date - estimateFirstOilDate.Date).TotalDays;
                        estimateTotalDays = (estimateFirstOilDate.Year == 1 || estimateFirstOilDate.Date == Tools.DefaultDate.Date) ? 0 : estimateTotalDays;

                        var firstOilDate = d.Max(e => e.FirstOilDate);
                        var lastDateOfFirstOilDateYear = new DateTime(firstOilDate.Year, 12, 31);
                        if (firstOilDate == Tools.DefaultDate) lastDateOfFirstOilDateYear = firstOilDate;
                        var totalDays = (lastDateOfFirstOilDateYear.Date - firstOilDate.Date).TotalDays;
                        totalDays = (firstOilDate.Year == 1 || firstOilDate.Date == Tools.DefaultDate.Date) ? 0 : totalDays;

                        var PhStart = d.Min(e => e.Phases.Min(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Start)));
                        var PhFinish = d.Max(e => e.Phases.Max(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Finish)));
                        var LEStart = d.Min(e => e.Phases.Min(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Start)));
                        var LEFinish = d.Max(e => e.Phases.Max(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Finish)));

                        var estimateFirstOilDateString = (int.Parse(estimateFirstOilDate.ToString("yyyy")) <= 1900 ? "" : estimateFirstOilDate.ToString("dd MMM yy"));
                        var firstOilDateString = (int.Parse(firstOilDate.ToString("yyyy")) <= 1900 ? "" : firstOilDate.ToString("dd MMM yy"));

                        return new
                        {
                            WellName = d.Key,
                            EstimateFirstOilDate = estimateFirstOilDateString,
                            EstimateTotalDays = estimateTotalDays,
                            FirstOilDate = firstOilDateString,
                            ActualTotalDays = totalDays,
                            PhStart = PhStart,
                            PhFinish = PhFinish,
                            LEStart = LEStart,
                            LEFinish = LEFinish
                        };
                    })
                    .Where(d => FilterByActiveWell(activeWell, d.PhStart, d.PhFinish, d.LEStart, d.LEFinish))
                    .OrderBy(d => d.WellName)
                    .ThenBy(d => d.PhStart);

                return final;
            });
        }

        public JsonResult GetWellForDocument(string dateStart, string dateFinish, string dateStart2, string dateFinish2, string dateRelation, string[] operatingUnits, string[] projectNames, string[] regions, string[] rigNames, string[] rigTypes, string[] wellNames)
        {
            var query = GenerateQueryFromFilterAndDate(dateStart, dateFinish, dateStart2, dateFinish2, dateRelation, operatingUnits, projectNames, regions, rigNames, rigTypes, wellNames, isExpandOneWeek: true);
            var wa = WellActivity.Populate<WellActivity>(query);
            List<DocumentManagementHeader> DocHeader = new List<DocumentManagementHeader>();

            List<string> WellNames = new List<string>();


            var result = new ResultInfo();
            foreach (var x in wa)
            {
                WellNames.Add(x.WellName);
            }

            if (WellNames.Count > 0)
            {
                var q = Query.In("WellName", new BsonArray(WellNames));
                List<WellActivityDocument> DataDocument = DataHelper.Populate<WellActivityDocument>("WEISWellActivityDocuments", Query.In("WellName", new BsonArray(WellNames)));


                foreach (var x in DataDocument)
                {
                    foreach (var y in x.Files)
                    {
                        DocHeader.Add(new DocumentManagementHeader
                        {
                            FileName = y.FileName,
                            FileNo = y.FileNo,
                            FileTitle = y.Title,
                            FileDescription = y.Description,
                            ContentType = y.ContentType,
                            ActivityUpdateId = x.ActivityUpdateId,
                            ActivityDesc = x.ActivityDesc,
                            ActivityType = x.ActivityType,
                            WellName = x.WellName,
                            UpdateVersion = x.UpdateVersion,
                            Type = y.Type,
                            Link = y.Link,
                            FileSize = y.FileSize
                        });
                    }
                }

                result.Data = new
                {
                    Data = DocHeader.ToArray()
                };
                return MvcTools.ToJsonResult(result.Data, JsonRequestBehavior.AllowGet);

            }
            else
            {
                WellActivityDocument DataDocument = new WellActivityDocument();
                result.Data = new
                {
                    Data = DataDocument
                };
                return MvcTools.ToJsonResult(result.Data, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetOperationSummaryScoreCard(string periodBase, string dateStart, string dateFinish, string dateStart2, string dateFinish2, string dateRelation, string[] operatingUnits, string[] projectNames, string[] regions, string[] rigNames, string[] rigTypes, string[] wellNames, string activeWell, string[] exType, string type = "ScoreCard")
        {
            #region Mas Arief
            //return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            //{
            //    var query = GenerateQueryFromFilterAndDate(dateStart, dateFinish, dateStart2, dateFinish2, dateRelation, operatingUnits, projectNames, regions, rigNames, rigTypes, wellNames);
            //    var final = WellActivity.Populate<WellActivity>(query)
            //        .Where(d =>
            //        {
            //            var PhSchedule = new DateRange()
            //            {
            //                Start = d.Phases.Min(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Start)),
            //                Finish = d.Phases.Max(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Finish))
            //            };

            //            return dateBetween(PhSchedule, dateStart, dateFinish, dateStart2, dateFinish2, dateRelation);
            //        })
            //        .Select(d =>
            //        {
            //            d.Phases.RemoveAll(e => e.ActivityType.ToLower().Contains("risk"));
            //            return d;
            //        })
            //        .GroupBy(d => d.WellName)
            //        .Select(d =>
            //        {
            //            var estimateFirstOilDate = d.Max(e => e.EstimateFirstOilDate);
            //            var lastDateOfEstimateFirstOilDateYear = new DateTime(estimateFirstOilDate.Year, 12, 31);
            //            var estimateTotalDays = (lastDateOfEstimateFirstOilDateYear.Date - estimateFirstOilDate.Date).TotalDays;
            //            estimateTotalDays = (estimateFirstOilDate.Year == 1 || estimateFirstOilDate.Date == Tools.DefaultDate.Date) ? 0 : estimateTotalDays;

            //            var firstOilDate = d.Max(e => e.FirstOilDate);
            //            var lastDateOfFirstOilDateYear = new DateTime(firstOilDate.Year, 12, 31);
            //            if (firstOilDate == Tools.DefaultDate) lastDateOfFirstOilDateYear = firstOilDate;
            //            var totalDays = (lastDateOfFirstOilDateYear.Date - firstOilDate.Date).TotalDays;
            //            totalDays = (firstOilDate.Year == 1 || firstOilDate.Date == Tools.DefaultDate.Date) ? 0 : totalDays;

            //            var PhStart = d.Min(e => e.Phases.Min(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Start)));
            //            var PhFinish = d.Max(e => e.Phases.Max(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Finish)));

            //            var estimateFirstOilDateString = (int.Parse(estimateFirstOilDate.ToString("yyyy")) <= 1900 ? "" : estimateFirstOilDate.ToString("dd MMM yy"));
            //            var firstOilDateString = (int.Parse(firstOilDate.ToString("yyyy")) <= 1900 ? "" : firstOilDate.ToString("dd MMM yy"));

            //            //var OP = d.Sum(e => e.Phases.Sum(f => f.OP.Cost));
            //            var OP = d.Sum(e => e.Phases.Sum(f => f.Plan.Cost));
            //            var AFE = d.Sum(e => e.Phases.Sum(f => f.AFE.Cost));
            //            var LE = d.Sum(e => e.Phases.Sum(f => f.LE.Cost));
            //            var LeadEngs = d.SelectMany(e => e.Phases, (e, f) => new
            //            {
            //                LeadEngineer = e.LeadEngineer
            //            });

            //            return new
            //            {
            //                WellName = d.Key,
            //                OP = Tools.Div(OP, 1000000),
            //                OP14Total = 0,
            //                AFE = Tools.Div(AFE, 1000000),
            //                LE = Tools.Div(LE, 1000000),
            //                LeadEngineer = LeadEngs,

            //                EstimateFirstOilDate = estimateFirstOilDateString,
            //                EstimateTotalDays = estimateTotalDays,
            //                FirstOilDate = firstOilDateString,
            //                ActualTotalDays = totalDays,
            //                PhStart = PhStart,
            //                PhFinish = PhFinish
            //            };
            //        })
            //        .Where(d => FilterByActiveWellAndVarianceData(activeWell, varianceData, d.PhStart, d.PhFinish, d.EstimateTotalDays, d.ActualTotalDays))
            //        .OrderBy(d => d.WellName)
            //        .ThenBy(d => d.PhStart);

            //    var OP14Total = final.Sum(d=>d.OP);

            //    return new
            //    {
            //        Raw = final,
            //        OP14Total
            //    };
            //});
            #endregion


            DateTime PeriodCurrentStart = new DateTime(DateTime.Now.Year, 1, 1);
            DateTime PeriodCurrentEnd = new DateTime(DateTime.Now.Year, 12, 31);

            //var query = GenerateQueryFromFilter(operatingUnits, projectNames, regions, rigNames, rigTypes, wellNames,exType);

            var isLastSequence = isPeriodBaseLastSequence(periodBase);
            var startDateConst = (isLastSequence ? "Phases.PhSchedule.Start" : "Phases.LESchedule.Start");
            var finishDateConst = (isLastSequence ? "Phases.PhSchedule.Finish" : "Phases.LESchedule.Finish");

            if (type == "ScoreCard")
            {
                if (activeWell == "None")
                {
                    activeWell = (isLastSequence ? "Per Last Sequence" : "Per Last Estimate");
                }
            }

            var query = GenerateQueryFromFilterAndDate(dateStart, dateFinish, dateStart2, dateFinish2, dateRelation, operatingUnits, projectNames, regions, rigNames, rigTypes, wellNames, startDateConst, finishDateConst, exType, true);

            var baseData = WellActivity.Populate<WellActivity>(query);//.Where(d=>d.Phases.Where(e=>e.LESchedule.Start > DateTime.MinValue));
            List<PersonOnWell> Persons = new List<PersonOnWell>();
            List<PersonOnWell> fullnames = new List<PersonOnWell>();
            foreach (var bd in baseData)
            {
                string WellName = bd.WellName;
                string SequenceId = bd.UARigSequenceId;
                var y = DataHelper.Populate<WEISPerson>("WEISPersons", Query.EQ("WellName", WellName));
                foreach (WEISPerson w in y)
                {
                    foreach (var i in w.PersonInfos.Where(u => u.RoleId.Equals("LEAD-ENG")))
                    {
                        fullnames.Add(new PersonOnWell
                        {
                            FullName = i.FullName,
                            WellName = WellName
                        });
                    }
                }
            }

            DateRange range = new DateRange();
            var mindate = range.Start;
            var final = baseData
                //.Where(d => !d.Phases.Where(e => e.LESchedule.Start == mindate).Any())
                .Where(d =>
                {
                    var dateRange = new DateRange();

                    if (isLastSequence)
                    {
                        dateRange.Start = d.Phases.Min(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Start));
                        dateRange.Finish = d.Phases.Max(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Finish));
                    }
                    else
                    {
                        dateRange.Start = d.Phases.Min(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Start));
                        dateRange.Finish = d.Phases.Max(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Finish));
                    }

                    return dateBetween(dateRange, dateStart, dateFinish, dateStart2, dateFinish2, dateRelation)
                        && (d.Phases.Count(e => e.LastWeek.CompareTo(Tools.DefaultDate) > 0) > 0);
                })
                .SelectMany(d => d.Phases, (d, e) => new
                {
                    ActivityType = e.ActivityType,
                    WellName = d.WellName,
                    EstimateFirstOilDate = d.EstimateFirstOilDate,
                    FirstOilDate = d.FirstOilDate,
                    PhSchedule = e.PhSchedule,
                    LESchedule = e.LESchedule,
                    Plan = e.Plan,
                    AFE = e.AFE,
                    LE = e.LE
                })
                .Where(d => !d.ActivityType.ToLower().Contains("risk"))
                //.Where(d => d.LESchedule.Start.ToString() != "1/1/1900 12:00:00 AM" && d.LESchedule.Finish.ToString() != "1/1/1900 12:00:00 AM")
                .GroupBy(d => d.WellName)
                .Select(d =>
                {
                    var estimateFirstOilDate = d.Max(e => e.EstimateFirstOilDate);
                    var lastDateOfEstimateFirstOilDateYear = new DateTime(estimateFirstOilDate.Year, 12, 31);
                    var estimateTotalDays = (lastDateOfEstimateFirstOilDateYear.Date - estimateFirstOilDate.Date).TotalDays;
                    estimateTotalDays = (estimateFirstOilDate.Year == 1 || estimateFirstOilDate.Date == Tools.DefaultDate.Date) ? 0 : estimateTotalDays;

                    var firstOilDate = d.Max(e => e.FirstOilDate);
                    var lastDateOfFirstOilDateYear = new DateTime(firstOilDate.Year, 12, 31);
                    if (firstOilDate == Tools.DefaultDate) lastDateOfFirstOilDateYear = firstOilDate;
                    var totalDays = (lastDateOfFirstOilDateYear.Date - firstOilDate.Date).TotalDays;
                    totalDays = (firstOilDate.Year == 1 || firstOilDate.Date == Tools.DefaultDate.Date) ? 0 : totalDays;

                    var PhStart = d.Min(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Start));
                    var PhFinish = d.Max(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Finish));
                    var LEStart = d.Min(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Start));
                    var LEFinish = d.Max(f => (f.LESchedule == null ? Tools.DefaultDate : f.LESchedule.Finish));

                    var estimateFirstOilDateString = (int.Parse(estimateFirstOilDate.ToString("yyyy")) <= 1900 ? "" : estimateFirstOilDate.ToString("dd MMM yy"));
                    var firstOilDateString = (int.Parse(firstOilDate.ToString("yyyy")) <= 1900 ? "" : firstOilDate.ToString("dd MMM yy"));

                    var OP = d.Sum(f => f.Plan.Cost);
                    var AFE = d.Sum(f => f.AFE.Cost);
                    var LE = d.Sum(f => f.LE.Cost);
                    var LeadEngs = fullnames.Where(e => e.WellName.Equals(d.Key)).Select(e => e.FullName).ToArray();

                    var LE_CurrentYear = d.Where(e => e.LESchedule.Start >= PeriodCurrentStart && e.LESchedule.Finish <= PeriodCurrentEnd).Sum(f => f.LE.Cost);
                    var LE_PreviousYear = d.Where(e => e.LESchedule.Start >= PeriodCurrentStart.AddYears(-1) && e.LESchedule.Start < PeriodCurrentStart && e.LESchedule.Finish >= PeriodCurrentStart && e.LESchedule.Finish <= PeriodCurrentEnd).Sum(f => f.LE.Cost);
                    var LE_CurrentNextYear = d.Where(e => e.LESchedule.Start >= PeriodCurrentStart && e.LESchedule.Start <= PeriodCurrentEnd && e.LESchedule.Finish > PeriodCurrentEnd && e.LESchedule.Finish <= PeriodCurrentEnd.AddYears(1)).Sum(f => f.LE.Cost);
                    var LE_NextYear = d.Where(e => e.LESchedule.Start >= PeriodCurrentStart.AddYears(1)).Sum(f => f.LE.Cost);

                    return new

                    {
                        WellName = d.Key,
                        OP = Tools.Div(OP, 1000000),
                        AFE = Tools.Div(AFE, 1000000),
                        LE = Tools.Div(LE, 1000000),
                        LE_CurrentYear = Tools.Div(LE_CurrentYear, 1000000),
                        LE_PreviousYear = Tools.Div(LE_PreviousYear, 1000000),
                        LE_CurrentNextYear = Tools.Div(LE_CurrentNextYear, 1000000),
                        LE_NextYear = Tools.Div(LE_NextYear, 1000000),
                        LeadEngineer = LeadEngs,

                        EstimateFirstOilDate = estimateFirstOilDateString,
                        EstimateTotalDays = estimateTotalDays,
                        FirstOilDate = firstOilDateString,
                        ActualTotalDays = totalDays,
                        PhStart = PhStart,
                        PhFinish = PhFinish,
                        LEStart = LEStart,
                        LEFinish = LEFinish,
                        PhSchedule = d.Select(f => f.PhSchedule)
                    };
                })
                //.Where(d => FilterByActiveWell(activeWell, d.PhStart, d.PhFinish, d.LEStart, d.LEFinish))
                .OrderBy(d => d.WellName)
                .ThenBy(d => d.LEStart);

            if (type != "LEYE")
            {
                final.Where(d => FilterByActiveWell(activeWell, d.PhStart, d.PhFinish, d.LEStart, d.LEFinish));
            }

            var period = new
            {
                LE_CurrentYear = "Start: " + PeriodCurrentStart.Year.ToString() +  " | End: " + PeriodCurrentStart.Year.ToString(),
                LE_PreviousYear = "Start: " + PeriodCurrentStart.AddYears(-1).Year.ToString() + " | End: " + PeriodCurrentStart.Year.ToString(),
                LE_CurrentNextYear = "Start: " + PeriodCurrentStart.Year.ToString() + " | End: " + PeriodCurrentStart.AddYears(1).Year.ToString(),
                LE_NextYear = "Start: " + PeriodCurrentStart.AddYears(1).Year.ToString() + " | End: ~"
            };

            var total = new
            {
                OP14Total = final.Sum(d => d.OP),
                AFETotal = final.Sum(d => d.AFE),
                LETotal = final.Sum(d => d.LE),
                LE_CurrentYearTotal = final.Sum(d => d.LE_CurrentYear),
                LE_PreviousYearTotal = final.Sum(d => d.LE_PreviousYear),
                LE_CurrentNextYearTotal = final.Sum(d => d.LE_CurrentNextYear),
                LE_NextYearTotal = final.Sum(d => d.LE_NextYear)
            };

            return Json(new { Success = true, Data = final, Period = period, Total = total }, JsonRequestBehavior.AllowGet);

        }
    }
}