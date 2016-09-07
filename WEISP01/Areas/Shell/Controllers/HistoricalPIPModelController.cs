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

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth]
    public class HistoricalPIPModelController : Controller
    {
        // GET: Shell/HistoricalDataModel
        public ActionResult Index()
        {
            return View();
        }

        private bool IsPhaseExistAt(string wellName, string activityType, DateTime date, IMongoQuery queryFilter)
        {
            var query = Query.And(
                Query.LT("Elements.Period.Start", date.Date.AddDays(1)),
                Query.GTE("Elements.Period.Finish", date),
                Query.EQ("WellName", wellName),
                Query.EQ("ActivityType", activityType)
            );
            if (queryFilter != null) query = Query.And(query, queryFilter);
            var pips = WellPIP.Populate<WellPIP>(query);

            return pips.Where(pip =>
            {
                if (pip.WellName.Contains("_CR"))
                    return false;
                if (pip.ActivityType.ToLower().Contains("risk"))
                    return false;
                if (pip.Elements == null)
                    return false;
                if (!pip.Elements.Any())
                    return false;

                pip.Elements = pip.Elements.Where(d => d.Period.InRange(date)).ToList();

                if (!pip.Elements.Any())
                    return false;

                return true;
            }).Any();
        }

        public JsonResult Calculate(DateTime AsAtDate,
            string[] Regions = null, string[] RigTypes = null,
            string[] Rigs = null,
            string[] WellNames = null, string RollingType = "Week", int RollingNo = 4)
        {
            return MvcResultInfo.Execute(() =>
            {
                DateTime dateTo = Tools.GetNearestDay(Tools.ToUTC(AsAtDate, true), DayOfWeek.Monday);

                var dates = Enumerable.Range(1, RollingNo).Select(d =>
                {
                    int idx = d - 1;
                    DateTime idxDate = idx == 0 ? dateTo : dateTo.AddDays(-7 * idx);
                    return idxDate;
                })
                .OrderByDescending(d => d)
                .ToList();

                string[] titles = new string[] { "OP-14", "Realized", "Unrealized", "Total" };
                var allWellActivityNames = new List<BsonDocument>();

                var wellInRegions = new List<string>();
                var wellInRigs = new List<string>();
                var wellInRigTypes = new List<string>();

                if (Regions != null && Regions.Count() > 0)
                    wellInRegions = WellActivity.Populate<WellActivity>(Query.In("Region", new BsonArray(Regions)), fields: new string[] { "WellName" }).Select(d => d.WellName).ToList();
                if (Rigs != null && Rigs.Count() > 0)
                    wellInRigs = WellActivity.Populate<WellActivity>(Query.In("RigName", new BsonArray(Rigs)), fields: new string[] { "WellName" }).Select(d => d.WellName).ToList();
                if (RigTypes != null && RigTypes.Count() > 0)
                    wellInRigTypes = WellActivity.Populate<WellActivity>(Query.In("RigType", new BsonArray(RigTypes)), fields: new string[] { "WellName" }).Select(d => d.WellName).ToList();

                var queryFilterRaw = new List<IMongoQuery>();
                if (wellInRegions.Any())
                    queryFilterRaw.Add(Query.In("WellName", new BsonArray(wellInRegions)));
                if (wellInRigs.Any())
                    queryFilterRaw.Add(Query.In("WellName", new BsonArray(wellInRigs)));
                if (wellInRigTypes.Any())
                    queryFilterRaw.Add(Query.In("WellName", new BsonArray(wellInRigTypes)));
                if (WellNames != null && WellNames.Count() > 0)
                    queryFilterRaw.Add(Query.In("WellName", new BsonArray(WellNames)));
                var queryFilter = (queryFilterRaw.Any() ? Query.And(queryFilterRaw) : null);

                //double divider = 1000000;
                var totals = new List<BsonDocument>();
                foreach (var dt in dates)
                {
                    var docs = new List<BsonDocument>();
                    var qs = new List<IMongoQuery>();
                    qs.Add(Query.And(Query.LT("Elements.Period.Start", dt.Date.AddDays(1)), Query.GTE("Elements.Period.Finish", dt)));
                    if (queryFilter != null) qs.Add(queryFilter);
                    var qwas = Query.And(qs);
                    var pips = WellPIP.Populate<WellPIP>(qwas);
                    foreach (var pip in pips)
                    {
                        if (pip.WellName.Contains("_CR"))
                            continue;
                        if (pip.ActivityType.ToLower().Contains("risk")) 
                            continue;
                        if (pip.Elements == null)
                            continue;
                        if (!pip.Elements.Any())
                            continue;

                        pip.Elements = pip.Elements.Where(d => d.Period.InRange(dt)).ToList();

                        if (!pip.Elements.Any())
                            continue;

                        //var phase = wa.Phases.FirstOrDefault(d => (d.LESchedule.InRange(dt) || d.PhSchedule.InRange(dt)) && d.ActivityType.ToLower().Contains("risk") == false);
                        //if (phase != null)
                        //{

                        //var wau = WellActivityUpdate.GetById(wa.WellName, wa.UARigSequenceId, phase.PhaseNo, dt, false, true);
                        //var prevWAU = WellActivityUpdate.GetById(wa.WellName, wa.UARigSequenceId, phase.PhaseNo, dt.AddDays(-7), false, false);
                        //var nextWAU = WellActivityUpdate.GetById(wa.WellName, wa.UARigSequenceId, phase.PhaseNo, dt.AddDays(+7), false, false);

                        var prevPIP = IsPhaseExistAt(pip.WellName, pip.ActivityType, dt.AddDays(-7), queryFilter);
                        var nextPIP = IsPhaseExistAt(pip.WellName, pip.ActivityType, dt.AddDays(+7), queryFilter);

                        var wa = WellActivity.Get<WellActivity>(Query.And(
                            Query.EQ("WellName", pip.WellName), 
                            Query.EQ("Phases.ActivityType", pip.ActivityType)
                        ));

                        var doc = new { 
                            pip.WellName, 
                            pip.ActivityType, 
                            RigName = ((wa == null) ? "" : wa.RigName) 
                        }.ToBsonDocument();
                        var phaseStatus = "ongoing";

                        if (pip.WellName == null)
                        {
                            phaseStatus = "inactive";
                        }
                        else
                        {
                            if (!prevPIP)
                                phaseStatus = "start";
                            else if (!nextPIP)
                                phaseStatus = "finish";
                        }

                        doc.Set("PhaseStatus", phaseStatus);
                        //doc.Set("curWAU", wau.WellName != null);
                        doc.Set("curWAU", true);
                        doc.Set("prevWAU", !prevPIP);
                        doc.Set("nextWAU", !nextPIP);

                        allWellActivityNames.Add(new { pip.WellName, pip.ActivityType }.ToBsonDocument());

                        var Items = new List<BsonDocument>();

                        var forPlan = pip.Elements.DefaultIfEmpty(new PIPElement());
                        Items.Add(new
                        {
                            Title = "OP-14",
                            Source = "",
                            Days = 0 - forPlan.Sum(d => d.DaysPlanImprovement + d.DaysPlanRisk),
                            Cost = 0 - forPlan.Sum(d => d.CostPlanImprovement + d.CostPlanRisk)
                        }.ToBsonDocument());
                            
                        var forRealized = pip.Elements.Where(d => Convert.ToString(d.Completion).ToLower().Equals("realized")).DefaultIfEmpty(new PIPElement());
                        Items.Add(new
                        {
                            Title = "Realized",
                            Source = "",
                            Days = 0 - forRealized.Sum(d => d.DaysCurrentWeekImprovement + d.DaysCurrentWeekRisk),
                            Cost = 0 - forRealized.Sum(d => d.CostCurrentWeekImprovement + d.CostCurrentWeekRisk)
                        }.ToBsonDocument());

                        var forUnrealized = pip.Elements.Where(d => !Convert.ToString(d.Completion).ToLower().Equals("realized")).DefaultIfEmpty(new PIPElement());
                        Items.Add(new
                        {
                            Title = "Unrealized",
                            Source = "EDM",
                            Days = 0 - forUnrealized.Sum(d => d.DaysCurrentWeekImprovement + d.DaysCurrentWeekRisk),
                            Cost = 0 - forUnrealized.Sum(d => d.CostCurrentWeekImprovement + d.CostCurrentWeekRisk)
                        }.ToBsonDocument());

                        Items.Add(new
                        {
                            Title = "Total",
                            Source = "",
                            Days = 0 - forPlan.Sum(d => d.DaysCurrentWeekImprovement + d.DaysCurrentWeekRisk),
                            Cost = 0 - forPlan.Sum(d => d.DaysCurrentWeekImprovement + d.DaysCurrentWeekRisk)
                        }.ToBsonDocument());

                        doc.Set("Items", new BsonArray(Items));
                        docs.Add(doc);
                    }

                    var VersionItems = docs.SelectMany(d => d.Get("Items").AsBsonArray.Select(e => e.ToBsonDocument()),
                        (p, d) =>
                        {
                            return new
                            {
                                Title = d.GetString("Title"),
                                Days = d.GetDouble("Days"),
                                Cost = d.GetDouble("Cost"),
                                Source = d.GetString("Source")
                            };
                        })
                        .ToList();
                    var docTotal = new
                    {
                        Version = dt
                    }.ToBsonDocument();

                    var totalItems = new List<BsonDocument>();
                    foreach (var title in titles)
                    {
                        totalItems.Add(new
                        {
                            Title = title,
                            Source = "",
                            Days = VersionItems.Where(d => d.Title.Equals(title)).Sum(d => d.Days),
                            Cost = VersionItems.Where(d => d.Title.Equals(title)).Sum(d => d.Cost)
                        }.ToBsonDocument());
                    }

                    docs = docs.OrderBy(d => d.GetString("WellName")).ToList();

                    docTotal.Set("Items", new BsonArray(totalItems));
                    docTotal.Set("WellItems", new BsonArray(docs));
                    totals.Add(docTotal);
                }

                allWellActivityNames = allWellActivityNames
                    .GroupBy(d => d)
                    .Select(d => d.Key)
                    .OrderBy(d => d.GetString("WellName"))
                    .ThenBy(d => d.GetString("ActivityType"))
                    .ToList();

                totals = totals.OrderByDescending(d => d.GetDateTime("Version")).ToList();

                var emptyItem = new List<BsonDocument>();
                foreach (var title in titles)
                {
                    emptyItem.Add(new { Title = title, Source = "", Days = 0, Cost = 0 }.ToBsonDocument());
                }

                return new
                {
                    Details = DataHelper.ToDictionaryArray(totals),
                    WellActivityNames = DataHelper.ToDictionaryArray(allWellActivityNames),
                    EmptyItem = DataHelper.ToDictionaryArray(emptyItem)
                };
            });
        }
    }
}