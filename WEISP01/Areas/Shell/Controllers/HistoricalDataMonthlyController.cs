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
    public class HistoricalDataMonthlyController : Controller
    {
        //
        // GET: /Shell/HistoricalDataMonthly/
        public ActionResult Index()
        {
            return View();
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
            string[] Rigs = null, string[] ProjectNames = null,
            string[] WellNames = null, string RollingType = "Week", int RollingNo = 4,
            string[] OPs=null,string OpRelation ="AND")
        {
            return MvcResultInfo.Execute(() =>
            {
                DateTime dateTo = Tools.ToUTC(new DateTime(AsAtDate.Year, AsAtDate.Month, 1));

                var dates = Enumerable.Range(1, RollingNo).Select(d =>
                {
                    int idx = d - 1;
                    DateTime idxDate = (idx == 0) ? dateTo : dateTo.AddMonths((-1)*idx);
                    return idxDate;
                })
                .OrderByDescending(d => d)
                .ToList();

                var allWellActivityNames = new List<BsonDocument>();

                double divider = 1000000;
                var totals = new List<BsonDocument>();
                foreach (var dt in dates)
                {
                    var docs = new List<BsonDocument>();

                    var qw = new List<IMongoQuery>();
                    if (Regions != null && Regions.Count() > 0) qw.Add(Query.In("Region", new BsonArray(Regions)));
                    if (Rigs != null && Rigs.Count() > 0) qw.Add(Query.In("RigName", new BsonArray(Rigs)));
                    if (RigTypes != null && RigTypes.Count() > 0) qw.Add(Query.In("RigType", new BsonArray(RigTypes)));
                    if (ProjectNames != null && ProjectNames.Count() > 0) qw.Add(Query.In("ProjectName", new BsonArray(ProjectNames)));
                    if (qw.Any())
                    {
                        var allWa = WellActivity.Populate<WellActivity>(Query.And(qw), fields: new string[] { "WellName" });
                        if (allWa.Any())
                            WellNames = allWa.Select(d => d.WellName).ToArray();
                    }

                    var qs = new List<IMongoQuery>();
                    qs.Add(Query.And(Query.LT("UpdateVersion", dt.Date.AddDays(1)), Query.GTE("UpdateVersion", dt)));
                    if (WellNames != null && WellNames.Count() > 0) qs.Add(Query.In("WellName", new BsonArray(WellNames)));
                    var qwas = Query.And(qs);
                    var waums = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(qwas);

                    foreach (var wa in waums)
                    {
                            var wau = WellActivityUpdateMonthly.GetById(wa.WellName, wa.SequenceId, wa.Phase.PhaseNo, dt, false, true);
                            var prevWAU = WellActivityUpdateMonthly.GetById(wa.WellName, wa.SequenceId, wa.Phase.PhaseNo, dt.AddMonths(-1), false, false);
                            var nextWAU = WellActivityUpdateMonthly.GetById(wa.WellName, wa.SequenceId, wa.Phase.PhaseNo, dt.AddMonths(+1), false, false);

                            var doc = new { wa.WellName, wa.Phase.ActivityType }.ToBsonDocument();
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

                            allWellActivityNames.Add(new { wa.WellName, wa.Phase.ActivityType }.ToBsonDocument());

                            var Items = new List<BsonDocument>();

                            if (wau.Plan.Days == 0 && wau.Plan.Cost == 0) wau.Plan = wa.Phase.Plan;

                            //var opwle = new WellDrillData();
                            //var wplan = WellActivity.Get<WellActivity>(Query.And(
                            //    Query.EQ("WellName", wau.WellName),
                            //    Query.EQ("UARigSequenceId", wau.SequenceId),
                            //    Query.EQ("Phases.ActivityType", wau.Phase.ActivityType)
                            //));
                            //if (wplan != null)
                            //{
                            //    var wphase = wplan.Phases.FirstOrDefault(d => wau.Phase.ActivityType.Equals(d.ActivityType));
                            //    if (wphase != null)
                            //    {
                            //        var wwr = WellActivityUpdate.Get<WellActivityUpdate>(Query.And(
                            //            Query.EQ("WellName", wau.WellName),
                            //            Query.EQ("SequenceId", wau.SequenceId),
                            //            Query.EQ("Phase.ActivityType", wau.Phase.ActivityType)
                            //        ));

                            //        if (wwr != null)
                            //        {
                            //            opwle = wphase.Plan;
                            //        }
                            //    }
                            //}

                            var delta = new WellDrillData()
                            {
                                Days = wau.CurrentWeek.Days - wau.Plan.Days,
                                Cost = wau.CurrentWeek.Cost - wau.Plan.Cost
                            };
                            var realizedPIPs = new WellDrillData()
                            {
                                Cost = wau.Elements.Where(d => d.Completion.Equals("Realized")).DefaultIfEmpty(new PIPElement()).Sum(d => d.CostCurrentWeekImprovement + d.CostCurrentWeekRisk),
                                Days = wau.Elements.Where(d => d.Completion.Equals("Realized")).DefaultIfEmpty(new PIPElement()).Sum(d => d.DaysCurrentWeekImprovement + d.DaysCurrentWeekRisk)
                            };
                            var bankedSavings = new WellDrillData()
                            {
                                Cost = wau.BankedSavingsCompetitiveScope.Cost + wau.BankedSavingsEfficientExecution.Cost + wau.BankedSavingsSupplyChainTransformation.Cost + wau.BankedSavingsTechnologyAndInnovation.Cost,
                                Days = wau.BankedSavingsCompetitiveScope.Days + wau.BankedSavingsEfficientExecution.Days + wau.BankedSavingsSupplyChainTransformation.Days + wau.BankedSavingsTechnologyAndInnovation.Days
                            };

                            //var phase = wau.GetWellActivityPhase();

                            Items.Add(new
                            {
                                Title = getOPLabel(wau.Phase.BaseOP),
                                Source = "",

                                //phase.CalcOP.Days,
                                //Cost = Tools.Div(phase.CalcOP.Cost,divider)

                               wau.Plan.Days,
                               Cost = Tools.Div(wau.Plan.Cost, divider)
                            }.ToBsonDocument());
                            Items.Add(new
                            {
                                Title = "Realized PIPs",
                                Source = "",
                                realizedPIPs.Days,
                                Cost = realizedPIPs.Cost
                            }.ToBsonDocument());
                            Items.Add(new
                            {
                                Title = "Banked Savings",
                                Source = "",
                                bankedSavings.Days,
                                Cost = bankedSavings.Cost
                            }.ToBsonDocument());

                            Items.Add(new
                            {
                                Title = "LE",
                                Source = "",
                                //phase.LE.Days,
                                //Cost = Tools.Div(phase.LE.Cost,divider)
                                wau.CurrentWeek.Days,
                                Cost = Tools.Div(wau.CurrentWeek.Cost, divider)
                            }.ToBsonDocument());
                            Items.Add(new
                            {
                                Title = "Calc. LE",
                                Source = "",
                                wau.CalculatedLE.Days,
                                Cost = Tools.Div(wau.CalculatedLE.Cost, divider)
                            }.ToBsonDocument());
                            Items.Add(new
                            {
                                Title = "LE - Calc. LE",
                                Source = "",
                                Days = wau.CurrentWeek.Days - wau.CalculatedLE.Days,
                                Cost = Tools.Div(wau.CurrentWeek.Cost - wau.CalculatedLE.Cost, divider)
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

                    string[] titles = new string[] { "OP", "Realized PIPs", "Banked Savings", "LE", "Calc. LE", "LE - Calc. LE" };
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
                emptyItem.Add(new { Title = "OP", Source = "", Days = 0, Cost = 0 }.ToBsonDocument());
                emptyItem.Add(new { Title = "Realized PIPs", Source = "", Days = 0, Cost = 0 }.ToBsonDocument());
                emptyItem.Add(new { Title = "Banked Savings", Source = "", Days = 0, Cost = 0 }.ToBsonDocument());
                emptyItem.Add(new { Title = "LE", Source = "", Days = 0, Cost = 0 }.ToBsonDocument());
                emptyItem.Add(new { Title = "Calc. LE", Source = "", Days = 0, Cost = 0 }.ToBsonDocument());
                emptyItem.Add(new { Title = "LE - Calc. LE", Source = "", Days = 0, Cost = 0 }.ToBsonDocument());

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