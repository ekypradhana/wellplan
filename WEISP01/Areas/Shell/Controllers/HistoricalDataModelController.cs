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
    public class HistoricalDataModelController : Controller
    {
        // GET: Shell/HistoricalDataModel
        public ActionResult Index()
        {
            return View();
        }

        public List<string> GetWellListBasedOnEmail()
        {
            var ret = new List<string>();
            var wp = WEISPerson.Populate<WEISPerson>(q: Query.EQ("PersonInfos.Email", WebTools.LoginUser.Email));
            if (wp.Count > 0)
                ret = wp.Select(x => x.WellName).ToList();
            return ret;
        }

        private static string getOPLabel(List<string> BaseOP)
        {
            string LabelBaseOP = "";
            if (BaseOP != null && BaseOP.Count > 0)
            {
                if (BaseOP.Count == 1)
                {
                    LabelBaseOP = BaseOP.FirstOrDefault();
                }
                else
                {
                    var baseops = new List<Int32>();
                    foreach (var i in BaseOP) baseops.Add(Convert.ToInt32(i.Substring(i.Length - 2, 2)));
                    var maxOPYear = baseops.Max();
                    LabelBaseOP = "OP" + maxOPYear.ToString();
                }
            }
            else
            {
                LabelBaseOP = "";
            }
            return LabelBaseOP;
        }

        public JsonResult Calculate(DateTime AsAtDate,
            string[] Regions = null, string[] RigTypes = null,
            string[] Rigs = null,
            string[] WellNames = null, string RollingType = "Week", int RollingNo = 4,
            string[] OPs = null, string OpRelation = "AND")
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

                var allWellActivityNames = new List<BsonDocument>();

                double divider = 1000000;
                var OPLabel = "";
                var totals = new List<BsonDocument>();
                foreach (var dt in dates)
                {
                    var docs = new List<BsonDocument>();
                    var qs = new List<IMongoQuery>();
                    qs.Add(Query.And(Query.LT("Phases.PhSchedule.Start", dt.Date.AddDays(1)), Query.GTE("Phases.PhSchedule.Finish", dt)));
                    if (Regions != null && Regions.Count() > 0) qs.Add(Query.In("Region", new BsonArray(Regions)));
                    if (Rigs != null && Rigs.Count() > 0) qs.Add(Query.In("RigName", new BsonArray(Rigs)));
                    if (RigTypes != null && RigTypes.Count() > 0) qs.Add(Query.In("RigType", new BsonArray(RigTypes)));
                    if (WellNames != null && WellNames.Count() > 0) qs.Add(Query.In("WellName", new BsonArray(WellNames)));
                    var qwas = Query.And(qs);
                    var was = WellActivity.Populate<WellActivity>(qwas);
                    foreach (var wa in was)
                    {
                        var phases = wa.Phases.Where(d => (d.LESchedule.InRange(dt) || d.PhSchedule.InRange(dt)) && d.ActivityType.ToLower().Contains("risk") == false);
                        if(phases.Any())
                        {
                            foreach (var phase in phases)
                            {
                                if (phase != null)
                                {
                                    var wau = WellActivityUpdate.GetById(wa.WellName, wa.UARigSequenceId, phase.PhaseNo, dt, false, true);
                                    var prevWAU = WellActivityUpdate.GetById(wa.WellName, wa.UARigSequenceId, phase.PhaseNo, dt.AddDays(-7), false, false);
                                    var nextWAU = WellActivityUpdate.GetById(wa.WellName, wa.UARigSequenceId, phase.PhaseNo, dt.AddDays(+7), false, false);

                                    var doc = new { wa.WellName, phase.ActivityType }.ToBsonDocument();
                                    var phaseStatus = "ongoing";

                                    if (wau.WellName == null)
                                    {
                                        phaseStatus = "inactive";
                                    }
                                    else
                                    {
                                        if (prevWAU == null)
                                            phaseStatus = "start";
                                        else if (nextWAU == null)
                                            phaseStatus = "finish";
                                    }

                                    doc.Set("PhaseStatus", phaseStatus);
                                    doc.Set("curWAU", wau.WellName != null);
                                    doc.Set("prevWAU", prevWAU != null);
                                    doc.Set("nextWAU", nextWAU != null);

                                    allWellActivityNames.Add(new { wa.WellName, phase.ActivityType }.ToBsonDocument());

                                    var Items = new List<BsonDocument>();

                                    if (wau.Plan.Days == 0 && wau.Plan.Cost == 0) wau.Plan = phase.Plan;
                                    Items.Add(new
                                    {
                                        Title = getOPLabel(wau.Phase.BaseOP),
                                        Source = "XL",
                                        wau.Plan.Days,
                                        Cost = Tools.Div(wau.Plan.Cost, divider)
                                    }.ToBsonDocument());
                                    Items.Add(new
                                    {
                                        Title = "Last Sequence",
                                        Source = "XL",
                                        wau.OP.Days,
                                        Cost = Tools.Div(wau.OP.Cost, divider)
                                    }.ToBsonDocument());
                                    Items.Add(new
                                    {
                                        Title = "AFE",
                                        Source = "EDM",
                                        wau.AFE.Days,
                                        Cost = Tools.Div(wau.AFE.Cost, divider)
                                    }.ToBsonDocument());
                                    Items.Add(new
                                    {
                                        Title = "LE",
                                        Source = "WR",
                                        wau.CurrentWeek.Days,
                                        Cost = Tools.Div(wau.CurrentWeek.Cost, divider)
                                    }.ToBsonDocument());

                                    doc.Set("Items", new BsonArray(Items));
                                    docs.Add(doc);
                                }
                            }
                        }
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

                    //var OPTitles = VersionItems.Where(x=>x.Title != null).Select(x => x.Title).Distinct().ToList();
                    //var OPLatest = getOPLabel(OPTitles);

                    string[] titles = new string[] { "OP", "Last Sequence", "AFE", "LE" };
                    var totalItems = new List<BsonDocument>();
                    foreach (var title in titles)
                    {
                        var totalDays = VersionItems.Where(d => d.Title.Equals(title)).Sum(d => d.Days);
                        var totalCost = VersionItems.Where(d => d.Title.Equals(title)).Sum(d => d.Cost);
                        if (title == "OP")
                        {
                            totalDays = VersionItems.Where(d => d.Title.Equals("OP15") || d.Title.Equals("OP14") || d.Title.Equals("")).Sum(d => d.Days);
                            totalCost = VersionItems.Where(d => d.Title.Equals("OP15") || d.Title.Equals("OP14") || d.Title.Equals("")).Sum(d => d.Cost);
                        }
                        totalItems.Add(new
                        {
                            Title = title,
                            Source = "",
                            Days = totalDays,
                            Cost = totalCost
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
                emptyItem.Add(new { Title = "OP", Source = "XL", Days = 0, Cost = 0 }.ToBsonDocument());
                emptyItem.Add(new { Title = "Last Sequence", Source = "XL", Days = 0, Cost = 0 }.ToBsonDocument());
                emptyItem.Add(new { Title = "AFE", Source = "EDM", Days = 0, Cost = 0 }.ToBsonDocument());
                emptyItem.Add(new { Title = "LE", Source = "WR", Days = 0, Cost = 0 }.ToBsonDocument());

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