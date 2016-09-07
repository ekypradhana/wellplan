using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using ECIS.Client.WEIS;
using System.Web.Configuration;
using System.IO;
using System.Text.RegularExpressions;

using Aspose.Cells;
using Aspose.Pdf;
using Aspose.Pdf.Generator;
using System.Threading;
using System.Threading.Tasks;
using LoadFormat = Aspose.Cells.LoadFormat;
using LoadOptions = Aspose.Cells.LoadOptions;
using Aspose.Pdf.Text;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class MonthlyReportController : Controller
    {
        // GET: Shell/WeeklyReport
        public ActionResult Index()
        {
            var appIssue = new ECIS.AppServer.Areas.Issue.Controllers.AppIssueController();
            ViewBag.IsAdministrator = appIssue.isRole("ADMINISTRATORS");
            ViewBag.IsAppSupport = appIssue.isRole("APP-SUPPORTS");

            ViewBag.isAdmin = "0";
            if (_isAdmin())
            {
                ViewBag.isAdmin = "1";
            }

            ViewBag.isRO = "0";
            if (_isReadOnly())
            {
                ViewBag.isRO = "1";
            }
            ViewBag.WordCount = getWordCount();

            var now = DateTime.Now;
            ViewBag.StartDate = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
            var lastUpdateVersion = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q: null, take: 1, sort: SortBy.Descending("UpdateVersion"));
            //var financialCalendarLastActive = WEISFinancialCalendar.Get<WEISFinancialCalendar>(Query.EQ("Status", "Active"));
            if (lastUpdateVersion.Any())
            {
                ViewBag.StartDate = Tools.ToUTC(lastUpdateVersion.FirstOrDefault().UpdateVersion);
            }


            string previousOP = "";
            string nextOP = "";
            var dm = new DataModel2Controller();
            var DefaultOP = getBaseOP(out previousOP, out nextOP);
            ViewBag.DefaultOP = DefaultOP;
            ViewBag.PreviousOP = previousOP;
            ViewBag.NextOP = nextOP;


            ViewBag.FullName = "";

            try
            {
                var user = DataHelper.Get<ECIS.Identity.IdentityUser>("Users", Query.EQ("UserName", WebTools.LoginUser.UserName));
                if (user != null)
                {
                    ViewBag.FullName = user.FullName;
                }
            }
            catch (Exception) { }
            var lsInfo = new PIPAnalysisController().GetLatestLsDate();
            ViewBag.LatestLS = lsInfo;
            return View();
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

        public ActionResult Report()
        {
            return View();
        }

        public ActionResult DashboardLE()
        {
            var lts = new PIPAnalysisController().GetLatestLsDate();//new pipana().GetLatestLsDate();
            ViewBag.LatestLS = lts;
            return View();
        }

        private WellActivityUpdateMonthly countShellShare(WellActivityUpdateMonthly waum, double WorkingInterest)
        {

            waum.Plan.Cost = waum.Plan.Cost * WorkingInterest;
            waum.OP.Cost = waum.OP.Cost * WorkingInterest;
            waum.CurrentWeek.Cost = waum.CurrentWeek.Cost * WorkingInterest;
            waum.CurrentMonth.Cost = waum.CurrentMonth.Cost * WorkingInterest;
            waum.Phase.Plan.Cost = waum.Phase.Plan.Cost * WorkingInterest;
            waum.Phase.OP.Cost = waum.Phase.OP.Cost * WorkingInterest;
            waum.Phase.LE.Cost = waum.Phase.LE.Cost * WorkingInterest;
            waum.Phase.LWE.Cost = waum.Phase.LWE.Cost * WorkingInterest;
            waum.AFE.Cost = waum.AFE.Cost * WorkingInterest;
            waum.Phase.AFE.Cost = waum.Phase.AFE.Cost * WorkingInterest;

            return waum;
        }

        public JsonResult GetWaterFallDataConfigDataSource(string ActivityId, string showBy = "Days", double CalcLEDays = 0, double CalcLECost = 0, bool ShellShare = false)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var wau1 = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(Query.EQ("_id", ActivityId));
                var wau = new WellActivityUpdateMonthly();
                var qs = new List<IMongoQuery>();
                if (ShellShare)
                {
                    var WorkingInterest = 0.0;
                    qs.Add(Query.EQ("WellName", wau1.WellName));
                    qs.Add(Query.EQ("UARigSequenceId", wau1.SequenceId));
                    qs.Add(Query.EQ("Phases.ActivityType", wau1.Phase.ActivityType));
                    var getWorkingInterest = WellActivity.Get<WellActivity>(Query.And(qs));
                    if (getWorkingInterest != null)
                    {
                        WorkingInterest = getWorkingInterest.WorkingInterest > 1 ? Tools.Div(getWorkingInterest.WorkingInterest, 100) : getWorkingInterest.WorkingInterest;
                    }
                    wau = countShellShare(wau1, WorkingInterest);
                    CalcLECost = CalcLECost * WorkingInterest;
                }
                else
                {
                    wau = wau1;
                }
                var results = WaterfallStacked.GetWaterfallData(wau, CalcLECost: CalcLECost, CalcLEDays: CalcLEDays, ShellShare: ShellShare);

                //Proses Ulang
                var list = new string[]
                {
                    "Bottom", "Plan", 
                    "RealizedCompetitiveScopeOppPos", "RealizedSupplyChainTransformationOppPos", "RealizedEfficientExecutionOppPos", "RealizedTechnologyandInnovationOppPos",
                    "RealizedCompetitiveScopeOppNeg", "RealizedSupplyChainTransformationOppNeg", "RealizedEfficientExecutionOppNeg", "RealizedTechnologyandInnovationOppNeg",
                    "RealizedCompetitiveScopeRiskPos", "RealizedSupplyChainTransformationRiskPos", "RealizedEfficientExecutionRiskPos", "RealizedTechnologyandInnovationRiskPos", 
                    "RealizedCompetitiveScopeRiskNeg", "RealizedSupplyChainTransformationRiskNeg", "RealizedEfficientExecutionRiskNeg", "RealizedTechnologyandInnovationRiskNeg", 
                    "ADSCompetitiveScopeNeg", "ADSSupplyChainTransformationNeg", "ADSEfficientExecutionNeg", "ADSTechnologyandInnovationNeg", 
                    "ADSCompetitiveScopePos", "ADSSupplyChainTransformationPos", "ADSEfficientExecutionPos", "ADSTechnologyandInnovationPos", 
                    "LE",
                    "UnrealizedCompetitiveScopeOppPos", "UnrealizedSupplyChainTransformationOppPos", "UnrealizedEfficientExecutionOppPos", "UnrealizedTechnologyandInnovationOppPos",
                    "UnrealizedCompetitiveScopeOppNeg", "UnrealizedSupplyChainTransformationOppNeg", "UnrealizedEfficientExecutionOppNeg", "UnrealizedTechnologyandInnovationOppNeg",
                    "UnrealizedCompetitiveScopeRiskPos", "UnrealizedSupplyChainTransformationRiskPos", "UnrealizedEfficientExecutionRiskPos", "UnrealizedTechnologyandInnovationRiskPos", 
                    "UnrealizedCompetitiveScopeRiskNeg", "UnrealizedSupplyChainTransformationRiskNeg", "UnrealizedEfficientExecutionRiskNeg", "UnrealizedTechnologyandInnovationRiskNeg", 
                    "UnriskedUpside", "TOP"
                };

                var get = showBy;//"Days";
                var sortedData = new List<BsonDocument>();
                var series = new List<BsonDocument>();
                var categories = new List<string>();
                var dataTable = new BsonDocument();
                var max = 0.0;
                if (results.Any())
                {

                    //for data chart
                    var upsd = 0.0;
                    var res = results.Select(x => x.ToBsonDocument());
                    foreach (var xx in res)
                    {
                        var plusVL = 0.0;
                        #region for Data Table
                        if (xx.GetString("Title").Contains("OP-15"))
                        {
                            var vl = xx.GetDouble("Stack.Plan." + get);
                            dataTable.Set("OP", vl);
                        }

                        if (xx.GetString("Title").Contains("Unrisked Upside"))
                        {
                            var vl = xx.GetDouble("Stack.UnriskedUpside." + get);
                            dataTable.Set("Unrisked", vl);
                        }

                        if (xx.GetString("Title").Contains("Realized PIPs Opp"))
                        {
                            var vl = xx.GetDouble("Stack.RealizedCompetitiveScopeOppPos." + get) + xx.GetDouble("Stack.RealizedCompetitiveScopeOppNeg." + get);
                            plusVL = dataTable.GetDouble("RealizedCompetitiveScopeOpp");
                            dataTable.Set("RealizedCompetitiveScopeOpp", vl + plusVL);

                            vl = xx.GetDouble("Stack.RealizedSupplyChainTransformationOppPos." + get) + xx.GetDouble("Stack.RealizedSupplyChainTransformationOppNeg." + get);
                            plusVL = dataTable.GetDouble("RealizedSupplyChainTransformationOpp");
                            dataTable.Set("RealizedSupplyChainTransformationOpp", vl + plusVL);

                            vl = xx.GetDouble("Stack.RealizedEfficientExecutionOppPos." + get) + xx.GetDouble("Stack.RealizedEfficientExecutionOppNeg." + get);
                            plusVL = dataTable.GetDouble("RealizedEfficientExecutionOpp");
                            dataTable.Set("RealizedEfficientExecutionOpp", vl + plusVL);

                            vl = xx.GetDouble("Stack.RealizedTechnologyandInnovationOppPos." + get) + xx.GetDouble("Stack.RealizedTechnologyandInnovationOppNeg." + get);
                            plusVL = dataTable.GetDouble("RealizedTechnologyandInnovationOpp");
                            dataTable.Set("RealizedTechnologyandInnovationOpp", vl + plusVL);
                        }

                        if (xx.GetString("Title").Contains("Realized PIPs Risk"))
                        {
                            var vl = xx.GetDouble("Stack.RealizedCompetitiveScopeRiskPos." + get) + xx.GetDouble("Stack.RealizedCompetitiveScopeRiskNeg." + get);
                            plusVL = dataTable.GetDouble("RealizedCompetitiveScopeRisk");
                            dataTable.Set("RealizedCompetitiveScopeRisk", vl + plusVL);

                            vl = xx.GetDouble("Stack.RealizedSupplyChainTransformationRiskPos." + get) + xx.GetDouble("Stack.RealizedSupplyChainTransformationRiskNeg." + get);
                            plusVL = dataTable.GetDouble("RealizedSupplyChainTransformationRisk");
                            dataTable.Set("RealizedSupplyChainTransformationRisk", vl + plusVL);

                            vl = xx.GetDouble("Stack.RealizedEfficientExecutionRiskPos." + get) + xx.GetDouble("Stack.RealizedEfficientExecutionRiskNeg." + get);
                            plusVL = dataTable.GetDouble("RealizedEfficientExecutionRisk");
                            dataTable.Set("RealizedEfficientExecutionRisk", vl + plusVL);

                            vl = xx.GetDouble("Stack.RealizedTechnologyandInnovationRiskPos." + get) + xx.GetDouble("Stack.RealizedTechnologyandInnovationRiskNeg." + get);
                            plusVL = dataTable.GetDouble("RealizedTechnologyandInnovationRisk");
                            dataTable.Set("RealizedTechnologyandInnovationRisk", vl + plusVL);
                        }

                        if (xx.GetString("Title").Contains("Additional Banked Saving Risk"))
                        {
                            var vl = xx.GetDouble("Stack.ADSCompetitiveScopePos." + get) + xx.GetDouble("Stack.ADSCompetitiveScopeNeg." + get);
                            plusVL = dataTable.GetDouble("ADSCompetitiveScope");
                            dataTable.Set("ADSCompetitiveScope", vl + plusVL);

                            vl = xx.GetDouble("Stack.ADSSupplyChainTransformationPos." + get) + xx.GetDouble("Stack.ADSSupplyChainTransformationNeg." + get);
                            plusVL = dataTable.GetDouble("ADSSupplyChainTransformation");
                            dataTable.Set("ADSSupplyChainTransformation", vl + plusVL);

                            vl = xx.GetDouble("Stack.ADSEfficientExecutionPos." + get) + xx.GetDouble("Stack.ADSEfficientExecutionNeg." + get);
                            plusVL = dataTable.GetDouble("ADSEfficientExecution");
                            dataTable.Set("ADSEfficientExecution", vl + plusVL);

                            vl = xx.GetDouble("Stack.ADSTechnologyandInnovationPos." + get) + xx.GetDouble("Stack.ADSTechnologyandInnovationNeg." + get);
                            plusVL = dataTable.GetDouble("ADSTechnologyandInnovation");
                            dataTable.Set("ADSTechnologyandInnovation", vl + plusVL);
                        }

                        if (xx.GetString("Title").Contains("LE"))
                        {
                            var vl = xx.GetDouble("Stack.LE." + get);
                            dataTable.Set("LE", vl);
                            upsd = vl;
                        }

                        if (xx.GetString("Title").Contains("Unrealized PIPs Opp"))
                        {
                            var vl = xx.GetDouble("Stack.UnrealizedCompetitiveScopeOppPos." + get) + xx.GetDouble("Stack.UnrealizedCompetitiveScopeOppNeg." + get);
                            plusVL = dataTable.GetDouble("UnRealizedCompetitiveScopeOpp");
                            dataTable.Set("UnRealizedCompetitiveScopeOpp", vl + plusVL);
                            upsd = upsd + vl;

                            vl = xx.GetDouble("Stack.UnrealizedSupplyChainTransformationOppPos." + get) + xx.GetDouble("Stack.UnrealizedSupplyChainTransformationOppNeg." + get);
                            plusVL = dataTable.GetDouble("UnRealizedSupplyChainTransformationOpp");
                            dataTable.Set("UnRealizedSupplyChainTransformationOpp", vl + plusVL);
                            upsd = upsd + vl;

                            vl = xx.GetDouble("Stack.UnrealizedEfficientExecutionOppPos." + get) + xx.GetDouble("Stack.UnrealizedEfficientExecutionOppNeg." + get);
                            plusVL = dataTable.GetDouble("UnRealizedEfficientExecutionOpp");
                            dataTable.Set("UnRealizedEfficientExecutionOpp", vl + plusVL);
                            upsd = upsd + vl;

                            vl = xx.GetDouble("Stack.UnrealizedTechnologyandInnovationOppPos." + get) + xx.GetDouble("Stack.UnrealizedTechnologyandInnovationOppNeg." + get);
                            plusVL = dataTable.GetDouble("UnRealizedTechnologyandInnovationOpp");
                            dataTable.Set("UnRealizedTechnologyandInnovationOpp", vl + plusVL);
                            upsd = upsd + vl;
                        }

                        if (xx.GetString("Title").Contains("Unrealized PIPs Risk"))
                        {
                            var vl = xx.GetDouble("Stack.UnrealizedCompetitiveScopeRiskPos." + get) + xx.GetDouble("Stack.UnrealizedCompetitiveScopeRiskNeg." + get);
                            plusVL = dataTable.GetDouble("UnRealizedCompetitiveScopeRisk");
                            dataTable.Set("UnRealizedCompetitiveScopeRisk", vl + plusVL);
                            upsd = upsd + vl;

                            vl = xx.GetDouble("Stack.UnrealizedSupplyChainTransformationRiskPos." + get) + xx.GetDouble("Stack.UnrealizedSupplyChainTransformationRiskNeg." + get);
                            plusVL = dataTable.GetDouble("UnRealizedSupplyChainTransformationRisk");
                            dataTable.Set("UnRealizedSupplyChainTransformationRisk", vl + plusVL);
                            upsd = upsd + vl;

                            vl = xx.GetDouble("Stack.UnrealizedEfficientExecutionRiskPos." + get) + xx.GetDouble("Stack.UnrealizedEfficientExecutionRiskNeg." + get);
                            plusVL = dataTable.GetDouble("UnRealizedEfficientExecutionRisk");
                            dataTable.Set("UnRealizedEfficientExecutionRisk", vl + plusVL);
                            upsd = upsd + vl;

                            vl = xx.GetDouble("Stack.UnrealizedTechnologyandInnovationRiskPos." + get) + xx.GetDouble("Stack.UnrealizedTechnologyandInnovationRiskNeg." + get);
                            plusVL = dataTable.GetDouble("UnRealizedTechnologyandInnovationRisk");
                            dataTable.Set("UnRealizedTechnologyandInnovationRisk", vl + plusVL);
                            upsd = upsd + vl;
                        }

                        if (xx.GetString("Title").Contains("Unrisked Upside"))
                        {
                            dataTable.Set("Upside", upsd);
                        }
                        #endregion

                        var ct = 0;
                        foreach (var yy in list)
                        {
                            if (yy == "Bottom" || yy == "TOP")
                            {

                            }
                            else
                            {
                                var id = xx.GetDouble("Stack." + yy + "." + get);
                                if (id != 0)
                                    ct++;
                            }
                        }
                        if (ct > 0)
                        {
                            sortedData.Add(xx);
                        }
                        //sortedData.Add(xx);
                    }
                    //sortedData = res;
                    //categories
                    categories = sortedData.Select(x => x.GetString("Title").Replace("Neg", "").Replace("Pos", "")).ToList();//.Replace("Neg", "").Replace("Pos", "")

                    var tempTOP = new List<double>();
                    //series
                    foreach (var xx in list)
                    {
                        if (xx == "ADSTechnologyandInnovationNeg")
                        {
                            var d = "";
                        }
                        var mx = 0.0;
                        var mn = 0.0;
                        var _data = sortedData.Select(x => Math.Round(x.GetDouble("Stack." + xx + "." + get), 2));
                        var ttl = sortedData.Select(x => x.GetString("Stack." + xx + ".Identifier"));
                        if (_data.Any())
                        {
                            mx = _data.Max();
                            mn = _data.Min();
                        }
                        var data = new
                        {
                            name = ttl.FirstOrDefault() == "BsonNull" ? "OP-15" : ttl.FirstOrDefault(),
                            data = _data,
                            stack = "yes",
                        }.ToBsonDocument();
                        if (xx == "Bottom" || xx == "TOP")
                        {
                            data.Set("color", "transparent");
                            if (xx == "TOP")
                            {
                                data.Set("labels", new BsonDocument().Set("visible", true).Set("position", "insideBase").Set("padding", "0"));
                            }
                            else
                            {
                                data.Set("tooltip", new BsonDocument().Set("visible", false));
                            }
                        }
                        else
                        {
                            //count TOP. jumlah semua element dan akan jadi top.
                            var ix = 0;
                            foreach (var d in _data)
                            {
                                if (tempTOP.Count() < _data.Count())
                                    tempTOP.Add(Math.Round(d, 2));
                                else
                                    tempTOP[ix] = Math.Round(tempTOP[ix] + d, 2);
                                ix++;
                            }

                            data.Set("color", GetColor(xx.Replace("Pos", "").Replace("Neg", "")));
                            var minVal = 0.0;
                            if(_data.Any())
                                minVal = _data.Min();
                            if (minVal < 0)
                            {
                                data.Set("tooltip",
                                    new BsonDocument().Set("visible", true)
                                        .Set("template",
                                            "#if(series.name != 'total'){# #= series.name #: -#= value # #}#"));
                                var __data = _data.Select(x => Math.Abs(x));
                                data.Set("data", new BsonArray(__data));
                            }
                            else
                            {
                                data.Set("tooltip",
                                    new BsonDocument().Set("visible", true)
                                        .Set("template",
                                            "#if(series.name != 'total'){# #= series.name #: #= value # #}#"));
                            }
                        }

                        //else
                        //{
                        if (xx == "Bottom" || xx == "TOP")
                        {
                            if (xx == "TOP")
                            {
                                if (tempTOP.Any())
                                {
                                    if (tempTOP.Min() < 0)
                                    {
                                        var listMinus = tempTOP.Where(x => x < 0);
                                        var st = "#";
                                        foreach (var min in listMinus)
                                        {
                                            st = st + " if(value == " + Math.Abs(min) + "){# -#:value# #}else";
                                        }
                                        st = st + "{# #:value# #}#";
                                        data.Set("labels",
                                            new BsonDocument().Set("visible", true)
                                                .Set("position", "insideBase")
                                                .Set("padding", "0")
                                                .Set("template", st));
                                    }
                                }
                                tempTOP = tempTOP.Select(x => Math.Abs(x)).ToList();
                                data.Set("data", new BsonArray(tempTOP));
                            }
                            series.Add(data);
                        }
                        else
                        {
                            if (mx != 0 || mn != 0)
                            {
                                series.Add(data);
                            }
                        }
                        //}
                        if (tempTOP.Any())
                        {
                            max = tempTOP.Max();
                        }
                    }

                }


                return new
                {
                    categories = categories,
                    series = series.Select(x => x.ToDictionary()),
                    max = Math.Round(max * 1.2, 2),
                    tableData = dataTable.ToDictionary()
                };
            });
        }
        public JsonResult GetDataDashboardLE(bool onlyLE, string yearStart, string yearFinish, string basedOn, List<string> Projects = null, List<string> OPs = null, string OpRelation = "AND")
        {
            DateTime parmDate = new DateTime(Convert.ToInt32(yearStart), 01, 01);
            DateTime parmDate2 = new DateTime(Convert.ToInt32(yearFinish), 12, 31);
            DateRange dr = new DateRange(parmDate, parmDate2);


            var t = WellActivityUpdateMonthly.GetPercentageComplete2(dr, basedOn, Projects)
                    .GroupBy(x => x.GetString("ProjectName"));

            List<BsonDocument> res = new List<BsonDocument>();
            foreach (var grp in t)
            {
                BsonDocument d = new BsonDocument();
                d.Set("Project", grp.Key);
                var addToList = true;

                foreach (var data in grp.ToList())
                {
                    var yu = BsonHelper.GetString(data, "_id").Split('-');
                    var amuont = BsonHelper.GetDouble(data, "LePercent");
                    string result = "";
                    if (BsonHelper.GetBool(data, "adaEvent") == true)
                    {
                        if (amuont >= 0.9)
                        {
                            result = "over90";
                        }
                        else if (amuont >= 0.75 && amuont < 90)
                        {
                            result = "over75";
                        }
                        else
                        {
                            result = "under75";
                        }
                    }
                    else
                    {
                        result = "NoData";
                    }
                    var detail = BsonHelper.Get(data, "DetailHaveLE");
                    var contentHaveLE = new BsonDocument();
                    if (!detail.IsBsonNull && detail.AsBsonArray.Count > 0)
                    {
                        contentHaveLE.Set("Count", BsonHelper.GetDouble(data, "CountHaveLE"));
                        contentHaveLE.Set("Detail", detail);
                        d.Set("CountHaveLE" + yu[0].ToString(), contentHaveLE);
                        //d.Set("DetailHaveLE", detail);
                    }
                    else
                    {
                        contentHaveLE.Set("Count", BsonHelper.GetDouble(data, "CountHaveLE"));
                        contentHaveLE.Set("Detail", new BsonArray());
                        d.Set("CountHaveLE" + yu[0].ToString(), contentHaveLE);
                        //d.Set("CountHaveLE" + yu[0].ToString(), new BsonArray());
                    }
                    d.Set("CountDontHaveLE" + yu[0].ToString(), BsonHelper.GetDouble(data, "CountDontHaveLE"));
                    d.Set("Total" + yu[0].ToString(), BsonHelper.GetDouble(data, "Total"));
                    d.Set("y" + yu[0].ToString(), result);

                    //if (!addToList)
                    //{
                    //    if (!onlyLE)
                    //    {
                    //        addToList = true;
                    //    }
                    //    else
                    //    {
                    //        if (result != "NoData")
                    //        {
                    //            addToList = true;
                    //        }
                    //    }
                    //}
                }
                if (addToList)
                {
                    res.Add(d);
                }
            }



            return Json(new { Success = true, Data = DataHelper.ToDictionaryArray(res.OrderBy(x => x.GetString("Project"))) }, JsonRequestBehavior.AllowGet);
        }

        public int getWordCount()
        {
            var wc = WEISAppsConfig.Get<WEISAppsConfig>(Query.EQ("_id", 1));
            if (wc == null)
            {
                return 0;
            }
            else
            {
                return wc.WordCount;
            }
        }

        public ActionResult Archive()
        {
            var appIssue = new ECIS.AppServer.Areas.Issue.Controllers.AppIssueController();
            ViewBag.IsAdministrator = appIssue.isRole("ADMINISTRATORS");
            ViewBag.IsAppSupport = appIssue.isRole("APP-SUPPORTS");

            ViewBag.isAdmin = "0";
            if (_isAdmin())
            {
                ViewBag.isAdmin = "1";
            }

            ViewBag.isReadOnly = "0";
            if (_isReadOnly())
            {
                ViewBag.isRO = "1";
            }
            ViewBag.WordCount = getWordCount();

            var wau = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>();
            ViewBag.DateFrom = wau.OrderBy(x => x.UpdateVersion).FirstOrDefault().UpdateVersion.ToString("dd-MMM-yyyy");
            ViewBag.DateTo = wau.OrderByDescending(x => x.UpdateVersion).FirstOrDefault().UpdateVersion.ToString("dd-MMM-yyyy");

            return View();
        }

        private static bool _isReadOnly()
        {
            var Email = WebTools.LoginUser.Email;
            List<string> Roles = WEISPerson.GetRolesByEmail(Email);
            var ro = Roles.Where(x => x.ToUpper() == "RO-ALL");
            bool isRO = false;
            if (ro.Count() > 0)
            {
                isRO = true;
            }
            return isRO;
        }

        private static bool _isAdmin()
        {
            var Email = WebTools.LoginUser.Email;
            List<string> Roles = WEISPerson.GetRolesByEmail(Email);
            var admin = Roles.Where(x => x.ToUpper() == "ADMINISTRATORS" || x.ToLower().Contains("app-support"));
            bool isAdmin = false;
            if (admin.Count() > 0)
            {
                isAdmin = true;
            }
            return isAdmin;
        }

        public JsonResult CheckPIPAvailability(string ActivityUpdateId)
        {
            try
            {
                var wau = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(Query.EQ("_id", ActivityUpdateId));
                if (wau != null)
                {
                    WellPIP pipCheck = WellPIP.GetByWellActivity(wau.WellName, wau.Phase.ActivityType);
                    if (pipCheck == null)
                    {
                        var message = "Cannot add new PIP. PIP document for well name: <b>" + wau.WellName + "</b> and activity <b>" + wau.Phase.ActivityType + "</b> is not created yet! You could create one from <b>PIP Configuration</b> menu.";
                        return Json(new { Success = false, Message = message }, JsonRequestBehavior.AllowGet);
                    }
                }

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public JsonResult SaveNewPIP(string ActivityUpdateId, List<string> AssignToOP, string Title, DateTime ActivityStart, DateTime ActivityEnd, double PlanDaysOpp, double PlanDaysRisk, double PlanCostOpp, double PlanCostRisk, string Classification, string PerformanceUnit, string ActionParty, List<WEISPersonInfo> ActionParties, string Theme, string Completion, bool isPositive)
        {
            try
            {
                var wau = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(Query.EQ("_id", ActivityUpdateId));

                var elementId = 0;
                if (wau.Elements == null)
                    wau.Elements = new List<PIPElement>();
                if (wau.Elements.Any())
                    elementId = wau.Elements.Max(d => d.ElementId);

                var pip = new PIPElement
                {
                    ElementId = (elementId + 1),
                    Title = Title,
                    DaysPlanImprovement = PlanDaysOpp,
                    CostPlanImprovement = PlanCostOpp,
                    DaysPlanRisk = PlanDaysRisk,
                    CostPlanRisk = PlanCostRisk,

                    DaysCurrentWeekImprovement = PlanDaysOpp,
                    CostCurrentWeekImprovement = PlanCostOpp,
                    DaysCurrentWeekRisk = PlanDaysRisk,
                    CostCurrentWeekRisk = PlanCostRisk,

                    Period = new DateRange(ActivityStart, ActivityEnd),
                    Classification = Classification,
                    ActionParty = ActionParty,
                    ActionParties = ActionParties,
                    PerformanceUnit = PerformanceUnit,
                    Completion = Completion,
                    Theme = Theme,
                    isNewElement = true,
                    isPositive = isPositive,
                    AssignTOOps = AssignToOP
                };
                wau.Elements.Add(pip);
                wau.Save(references: new string[] { "", "CalcLeSchedule", "isActualLE" });
                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                    LogType.Insert, wau.TableName, url, wau.ToBsonDocument(), null);


                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult WFStart(DateTime StartDate, string StartComment, List<String> WellActivityIds, bool isSendEmail = true)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                return MonthlyReportInitiate(StartDate, StartComment, WellActivityIds, isSendEmail);
            });
        }


        public List<WellActivityUpdateMonthly> MonthlyReportInitiate(DateTime StartDate, string StartComment, List<String> WellActivityIds, bool withSendMail = false, bool isFromInitiate = false, string pUser = null, string pEmail = null)
        {
            //List<int> WellActvID = new List<int>();
            //string[] _id = WellActivityId.Split(',');
            var dt = StartDate;// Tools.GetNearestDay(Tools.ToDateTime(String.Format("{0:dd-MMM-yyyy}", StartDate), true), DayOfWeek.Monday);
            var qwaus = new List<IMongoQuery>();
            qwaus.Add(Query.EQ("UpdateVersion", dt));
            var q = qwaus.Count == 0 ? Query.Null : Query.And(qwaus);
            List<WellActivityUpdateMonthly> waus = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q);

            foreach (string word in WellActivityIds)
            {
                string[] ids = word.Split('|');
                int waId = Tools.ToInt32(ids[0]);
                string activityType = ids[1];
                WellActivity wa = WellActivity.Get<WellActivity>(waId);

                var phase = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(activityType));
                if (isFromInitiate)
                {
                    if (phase != null) phase.IsActualLE = false;

                    DataHelper.Save(wa.TableName, wa.ToBsonDocument());
                }

                List<IMongoQuery> qs = new List<IMongoQuery>();
                IMongoQuery qpip = Query.Null;
                qs.Add(Query.EQ("WellName", wa.WellName));
                qs.Add(Query.EQ("SequenceId", wa.UARigSequenceId));
                qs.Add(Query.EQ("ActivityType", activityType));
                qpip = Query.And(qs);
                WellPIP pip = WellPIP.Get<WellPIP>(qpip);
                List<PIPElement> PIPElement = new List<PIPElement>();
                try
                {
                    if (pip == null)
                    {
                        pip = new WellPIP();
                    }

                    if (pip.Elements == null)
                        PIPElement = new List<PIPElement>();
                    else if (pip.Elements.Count == 0)
                        PIPElement = new List<PIPElement>();
                    else
                        PIPElement = pip.Elements;
                }
                catch (Exception e)
                {
                    PIPElement = new List<PIPElement>();
                }

                //add CRElements
                List<PIPElement> CRElements = new List<PIPElement>();
                try
                {

                    if (pip.CRElements == null)
                        CRElements = new List<PIPElement>();
                    else if (pip.CRElements.Count == 0)
                        CRElements = new List<PIPElement>();
                    else
                        CRElements = pip.CRElements;
                }
                catch (Exception e)
                {
                    CRElements = new List<PIPElement>();
                }

                if (phase != null)
                {
                    //if (a.PhSchedule.InRange(dt)) {
                    JsonResult emailSent = null;
                    Dictionary<string, string> variables = new Dictionary<string, string>();
                    var persons = phase.GetPersonsInRole(wa.WellName,
                        Config.GetConfigValue("WEISWRPICRoles", "WELL-ENG").ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries));
                    var ccs = phase.GetPersonsInRole(wa.WellName,
                        Config.GetConfigValue("WEISWRReviewersRoles", "").ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries));
                    WellActivityUpdateMonthly wau = null;
                    if (waus.Count > 0)
                    {
                        wau = waus.FirstOrDefault(
                            e => e.WellName.Equals(wa.WellName)
                            && (e.Phase == null ? "" : e.Phase.ActivityType).Equals(phase.ActivityType)
                            && e.SequenceId.Equals(wa.UARigSequenceId)
                            && e.UpdateVersion.Equals(dt));
                    }
                    if (wau == null)
                    {
                        wau = new WellActivityUpdateMonthly();
                        //wau._id = String.Format("{0}_{1:yyyyMMdd}", wa.UARigSequenceId, dt);
                        //wau.Country = "USA";
                        wau.AssetName = wa.AssetName;
                        wau.NewWell = false;
                        wau.WellName = wa.WellName;
                        wau.SequenceId = wa.UARigSequenceId;

                        wau.Phase = phase;

                        wau.Phase.ActivityType = phase.ActivityType;
                        wau.Phase.ActivityDesc = phase.ActivityDesc;
                        wau.Phase.PhaseNo = phase.PhaseNo;
                        wau.Phase.BaseOP = phase.BaseOP;
                        wau.Phase.EventCreatedFrom = "FinancialCalendar";
                        wau.Status = "In-Progress";
                        wau.UpdateVersion = dt;
                        wau.Elements = PIPElement;
                        wau.CRElements = CRElements;

                        //get before
                        var qBefore = new List<IMongoQuery>();
                        qBefore.Add(Query.EQ("WellName", wau.WellName));
                        qBefore.Add(Query.EQ("Phase.ActivityType", wau.Phase.ActivityType));
                        qBefore.Add(Query.EQ("SequenceId", wau.SequenceId));
                        qBefore.Add(Query.LT("UpdateVersion", wau.UpdateVersion));
                        var before = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(Query.And(qBefore), SortBy.Descending("UpdateVersion"));

                        if (before != null)
                        {
                            // add from latest wau
                            wau.Site = before.Site;
                            wau.Project = before.Project;
                            wau.WellType = before.WellType;
                            wau.Objective = before.Objective;
                            wau.Contractor = before.Contractor;
                            wau.RigSuperintendent = before.RigSuperintendent;
                            wau.Company = before.Company;
                            wau.EventStartDate = before.EventStartDate;
                            wau.EventType = before.EventType;
                            wau.WorkUnit = before.WorkUnit;
                            wau.OriginalSpudDate = before.OriginalSpudDate;
                        }
                        else
                        {
                            // add from wa
                            wau.Project = wa.ProjectName;
                            wau.EventType = wa.ActivityType;
                            wau.EventStartDate = phase.PhSchedule.Start;
                        }
                        wau.Calc();
                        // reset LE 
                        wau.CurrentWeek = new WellDrillData();

                        //wau.Phase.LE = new WellDrillData();
                        wau.UpdateVersion = StartDate;
                        wau.InitiateBy = pUser;
                        wau.InitiateDate = Tools.ToUTC(DateTime.Now);
                        //DataHelper.Save(wau.TableName, wau.ToBsonDocument());

                        //check apakah punya activity weekly update dan valid LS
                        var weeklyActivityUpdate = WellActivityUpdate.Get<WellActivityUpdate>(
                            Query.And(
                                Query.EQ("WellName", wau.WellName),
                                Query.EQ("SequenceId", wau.SequenceId),
                                Query.EQ("Phase.ActivityType", wau.Phase.ActivityType)
                            )
                            );

                        var isValidLS = wau.Phase.PhSchedule.Start > Tools.DefaultDate && wau.Phase.PhSchedule.Finish > Tools.DefaultDate;

                        if (weeklyActivityUpdate == null && isValidLS && !wau.Phase.ActivityType.Contains("FLOWBACK"))
                        {
                            wau.Save(references: new string[] { "initiateProcess" });

                            if (withSendMail)
                            {
                                variables.Add("WellName", wa.WellName);
                                variables.Add("ActivityType", phase.ActivityType);
                                variables.Add("UpdateVersion", String.Format("{0:dd-MMM-yyyy}", dt));
                                variables.Add("DueDate", String.Format("{0:dd-MMM-yyyy}", dt));
                                variables.Add("UrlAction", "http://" + Request.Url.Host + Url.Action("index", "weeklyreport"));
                                Email.Send("WRInitiate",
                                    persons.Select(d => d.Email).ToArray(),
                                    ccs.Count == 0 ? null : ccs.Select(d => d.Email).ToArray(),
                                    variables: variables,
                                    developerModeEmail: pEmail);
                                waus.Add(wau);
                            }
                        }

                    }
                    else
                    {
                        //wau.Status = "In-Progress";
                        //emailSent = Email.Send("WRInitiate",
                        //    new string[] { teamLeadEmail, leadEngineerEmail },
                        //    variables: variables,
                        //    developerModeEmail: WebTools.LoginUser.Email);
                        //wau.Save();
                    }
                    //}
                }
            }

            #region comment
            //IMongoQuery q = Query.Null;
            //List<IMongoQuery> qs = new List<IMongoQuery>();
            //qs.Add(Query.EQ("UpdateVersion", dt));
            //if (qs.Count > 0) q = Query.And(qs);
            //List<WellActivityUpdateMonthly> waus = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q);
            ////List<WellActivity> was = WellActivity.Populate<WellActivity>(qWellAc);
            //foreach (var wa in was)
            //{
            //    foreach (var a in wa.Phases)
            //    {
            //        if (a.PhSchedule.InRange(dt))
            //        {
            //            JsonResult emailSent = null;
            //            Dictionary<string, string> variables = new Dictionary<string, string>();
            //            variables.Add("WellName", wa.WellName);
            //            variables.Add("ActivityType", a.ActivityType);
            //            variables.Add("UpdateVersion", String.Format("{0:dd-MMM-yyyy}", dt));
            //            variables.Add("DueDate", String.Format("{0:dd-MMM-yyyy}", dt));
            //            variables.Add("UrlAction", "http://" + Request.Url.Host + Url.Action("index", "weeklyreport"));
            //            string teamLeadEmail = a.TeamLead == null ? "" : a.TeamLead.Email==null ? "" : a.TeamLead.Email;
            //            string leadEngineerEmail = a.LeadEngineer == null ? "" : a.LeadEngineer.Email == null ? "" : a.LeadEngineer.Email;

            //            var wau = waus.FirstOrDefault(
            //                e => e.WellName.Equals(wa.WellName)
            //                && e.Phase.ActivityType.Equals(a.ActivityType)
            //                && e.SequenceId.Equals(wa.UARigSequenceId)
            //                && e.UpdateVersion.Equals(dt));
            //            if (wau == null)
            //            {
            //                wau = new WellActivityUpdateMonthly();
            //                wau._id = String.Format("{0}_{1:yyyyMMdd}", wa.UARigSequenceId, dt);
            //                //wau.Country = "USA";
            //                wau.AssetName = wa.AssetName;
            //                wau.NewWell = false;
            //                wau.WellName = wa.WellName;
            //                wau.SequenceId = wa.UARigSequenceId;
            //                wau.Phase.ActivityType = a.ActivityType;
            //                wau.Phase.ActivityDesc = a.ActivityDesc;
            //                wau.Phase.PhaseNo = a.PhaseNo;
            //                wau.Status = "In-Progress";
            //                wau.UpdateVersion = dt;
            //                wau.Save();
            //                emailSent = Email.Send("WRInitiate", 
            //                    new string[] {teamLeadEmail, leadEngineerEmail},
            //                    variables: variables,
            //                    developerModeEmail:WebTools.LoginUser.Email);
            //                waus.Add(wau);
            //            }
            //            else
            //            {
            //                wau.Status = "In-Progress";
            //                emailSent = Email.Send("WRInitiate",
            //                    new string[] { teamLeadEmail, leadEngineerEmail },
            //                    variables: variables,
            //                    developerModeEmail: WebTools.LoginUser.Email);
            //                wau.Save();
            //            }
            //        }
            //    }
            //};
            #endregion

            return waus;
        }

        public void CreateSettingMail()
        {
            var id = "MRInitiate";
            //var LastUpdate = DateTime.Now;
            var Title = "Email when workflow for monthly report is being started";
            var Subject = "Please provide feedback for the Weekly Report for {WellName} - {ActivityType} for the week of {UpdateVersion}";
            var Body = "Please provide feedback for the Monthly Report for {WellName} - {ActivityType} for the month of {UpdateVersion}";
            var SMTPConfig = "Default";
            Email SetTemplate = new Email();
            SetTemplate._id = id;
            SetTemplate.Title = Title;
            SetTemplate.LastUpdate = DateTime.Now;
            SetTemplate.Subject = Subject;
            SetTemplate.Body = Body;
            SetTemplate.SMTPConfig = SMTPConfig;
            DataHelper.Save("WEISEmailTemplates", SetTemplate.ToBsonDocument());
        }

        public List<BsonDocument> GetFilterQueries(
            out List<WellActivity> filteredData,
            string Date = "",
            List<string> Regions = null,
            List<string> OperatingUnits = null,
            List<string> RigTypes = null,
            List<string> RigNames = null,
            List<string> Projects = null,
            List<string> WellNames = null,
            List<string> PerformanceUnits = null,
            List<string> Activities = null,

            string DateStart = "",
            string DateStart2 = "",
            string DateFinish = "",
            string DateFinish2 = "",
            string DateRelation = "OR")
        {
            var qa = new List<IMongoQuery>();
            if (Regions != null) qa.Add(Query.In("Region", new BsonArray(Regions)));
            if (OperatingUnits != null) qa.Add(Query.In("OperatingUnit", new BsonArray(OperatingUnits)));
            if (RigTypes != null) qa.Add(Query.In("RigType", new BsonArray(RigTypes)));
            if (RigNames != null) qa.Add(Query.In("RigName", new BsonArray(RigNames)));
            if (Projects != null) qa.Add(Query.In("ProjectName", new BsonArray(Projects)));
            if (WellNames != null) qa.Add(Query.In("WellName", new BsonArray(WellNames)));
            if (PerformanceUnits != null) qa.Add(Query.In("PerformanceUnit", new BsonArray(PerformanceUnits)));


            DateTime dateStart = Tools.DefaultDate;
            DateTime dateFinish = Tools.DefaultDate;
            DateTime dateStart2 = Tools.DefaultDate;
            DateTime dateFinish2 = Tools.DefaultDate;
            if (!DateStart.Equals("") && !DateFinish.Equals(""))
            {
                dateStart = DateTime.ParseExact(DateStart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                dateFinish = DateTime.ParseExact(DateFinish, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            }
            if (!DateStart2.Equals("") && !DateFinish2.Equals(""))
            {
                dateStart2 = DateTime.ParseExact(DateStart2, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                dateFinish2 = DateTime.ParseExact(DateFinish2, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            }

            if (!dateStart.Equals("") && dateStart2.Equals(""))
            {
                qa.Add(
                                Query.And(
                                    Query.GTE("Phases.PhSchedule.Start", Tools.ToUTC(dateStart)),
                                    Query.LT("Phases.PhSchedule.Start", Tools.ToUTC(dateStart.AddDays(1)))
                                )
                        );
            }
            else if (dateStart.Equals("") && !dateStart2.Equals(""))
            {
                qa.Add(
                                Query.And(
                                    Query.GTE("Phases.PhSchedule.Finish", Tools.ToUTC(dateStart)),
                                    Query.LT("Phases.PhSchedule.Finish", Tools.ToUTC(dateStart.AddDays(1)))
                                )
                        );

            }
            else if (!DateStart.Equals("") && !DateStart2.Equals(""))
            {
                if (DateRelation.Equals("OR"))
                {
                    qa.Add(Query.Or(
                                Query.And(
                                    Query.GTE("Phases.PhSchedule.Start", Tools.ToUTC(dateStart)),
                                    Query.LT("Phases.PhSchedule.Start", Tools.ToUTC(dateStart.AddDays(1)))
                                ),
                                Query.And(
                                    Query.GTE("Phases.PhSchedule.Finish", Tools.ToUTC(dateStart)),
                                    Query.LT("Phases.PhSchedule.Finish", Tools.ToUTC(dateStart.AddDays(1)))
                                )
                            )
                        );
                }
                else
                {
                    qa.Add(Query.And(
                                Query.And(
                                    Query.GTE("Phases.PhSchedule.Start", Tools.ToUTC(dateStart)),
                                    Query.LT("Phases.PhSchedule.Start", Tools.ToUTC(dateStart.AddDays(1)))
                                ),
                                Query.And(
                                    Query.GTE("Phases.PhSchedule.Finish", Tools.ToUTC(dateStart)),
                                    Query.LT("Phases.PhSchedule.Finish", Tools.ToUTC(dateStart.AddDays(1)))
                                )
                            )
                        );
                }
            }





            List<WellActivity> filteredDatas = new List<WellActivity>();
            if (qa.Count() > 0)
                filteredDatas = WellActivity.Populate<WellActivity>(Query.And(qa)).ToList();
            else
                filteredDatas = WellActivity.Populate<WellActivity>().ToList();

            filteredData = filteredDatas;

            List<BsonDocument> datasx = new List<BsonDocument>();
            foreach (var wa in filteredDatas)
            {
                foreach (var p in wa.Phases.Where(x => x.IsActualLE))
                {
                    // tidak usa di cek lg, karena ada batch CheckAndApplyisLEActual - testcontroller
                    var week = WellActivityUpdate.Get<WellActivityUpdate>(Query.And(
                        Query.EQ("WellName", wa.WellName),
                        Query.EQ("SequenceId", wa.UARigSequenceId),
                        Query.EQ("Phase.ActivityType", p.ActivityType)
                        ), new SortByBuilder().Descending("UpdateVersion"));
                    if (week == null)
                    {
                        var month = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(Query.And(
                        Query.EQ("WellName", wa.WellName),
                        Query.EQ("SequenceId", wa.UARigSequenceId),
                        Query.EQ("Phase.ActivityType", p.ActivityType),
                        Query.EQ("Phase.IsActualLE", true)
                        ), new SortByBuilder().Descending("UpdateVersion"));

                        if (month != null)
                            datasx.Add(month.ToBsonDocument());
                    }
                    else
                    {
                        datasx.Add(week.ToBsonDocument());
                    }
                }
            }

            return datasx;
        }


        public List<BsonDocument> GetFilterQueriesOpLe(
          out List<WellActivity> filteredData,
          string Date = "",
          List<string> Regions = null,
          List<string> OperatingUnits = null,
          List<string> RigTypes = null,
          List<string> RigNames = null,
          List<string> Projects = null,
          List<string> WellNames = null,
          List<string> PerformanceUnits = null,
          List<string> Activities = null,

          string DateStart = "",
          string DateStart2 = "",
          string DateFinish = "",
          string DateFinish2 = "",
          string DateRelation = "OR")
        {
            var qa = new List<IMongoQuery>();
            if (Regions != null) qa.Add(Query.In("Region", new BsonArray(Regions)));
            if (OperatingUnits != null) qa.Add(Query.In("OperatingUnit", new BsonArray(OperatingUnits)));
            if (RigTypes != null) qa.Add(Query.In("RigType", new BsonArray(RigTypes)));
            if (RigNames != null) qa.Add(Query.In("RigName", new BsonArray(RigNames)));
            if (Projects != null) qa.Add(Query.In("ProjectName", new BsonArray(Projects)));
            if (WellNames != null) qa.Add(Query.In("WellName", new BsonArray(WellNames)));
            if (PerformanceUnits != null) qa.Add(Query.In("PerformanceUnit", new BsonArray(PerformanceUnits)));


            DateTime dateStart = Tools.DefaultDate;
            DateTime dateFinish = Tools.DefaultDate;
            DateTime dateStart2 = Tools.DefaultDate;
            DateTime dateFinish2 = Tools.DefaultDate;
            if (!DateStart.Equals("") && !DateFinish.Equals(""))
            {
                dateStart = DateTime.ParseExact(DateStart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                dateFinish = DateTime.ParseExact(DateFinish, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            }
            if (!DateStart2.Equals("") && !DateFinish2.Equals(""))
            {
                dateStart2 = DateTime.ParseExact(DateStart2, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                dateFinish2 = DateTime.ParseExact(DateFinish2, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            }

            if (!dateStart.Equals("") && dateStart2.Equals(""))
            {
                qa.Add(
                                Query.And(
                                    Query.GTE("Phases.PhSchedule.Start", Tools.ToUTC(dateStart)),
                                    Query.LT("Phases.PhSchedule.Start", Tools.ToUTC(dateStart.AddDays(1)))
                                )
                        );
            }
            else if (dateStart.Equals("") && !dateStart2.Equals(""))
            {
                qa.Add(
                                Query.And(
                                    Query.GTE("Phases.PhSchedule.Finish", Tools.ToUTC(dateStart)),
                                    Query.LT("Phases.PhSchedule.Finish", Tools.ToUTC(dateStart.AddDays(1)))
                                )
                        );

            }
            else if (!DateStart.Equals("") && !DateStart2.Equals(""))
            {
                if (DateRelation.Equals("OR"))
                {
                    qa.Add(Query.Or(
                                Query.And(
                                    Query.GTE("Phases.PhSchedule.Start", Tools.ToUTC(dateStart)),
                                    Query.LT("Phases.PhSchedule.Start", Tools.ToUTC(dateStart.AddDays(1)))
                                ),
                                Query.And(
                                    Query.GTE("Phases.PhSchedule.Finish", Tools.ToUTC(dateStart)),
                                    Query.LT("Phases.PhSchedule.Finish", Tools.ToUTC(dateStart.AddDays(1)))
                                )
                            )
                        );
                }
                else
                {
                    qa.Add(Query.And(
                                Query.And(
                                    Query.GTE("Phases.PhSchedule.Start", Tools.ToUTC(dateStart)),
                                    Query.LT("Phases.PhSchedule.Start", Tools.ToUTC(dateStart.AddDays(1)))
                                ),
                                Query.And(
                                    Query.GTE("Phases.PhSchedule.Finish", Tools.ToUTC(dateStart)),
                                    Query.LT("Phases.PhSchedule.Finish", Tools.ToUTC(dateStart.AddDays(1)))
                                )
                            )
                        );
                }
            }





            List<WellActivity> filteredDatas = new List<WellActivity>();
            if (qa.Count() > 0)
                filteredDatas = WellActivity.Populate<WellActivity>(Query.And(qa)).ToList();
            else
                filteredDatas = WellActivity.Populate<WellActivity>().ToList();

            filteredData = filteredDatas;


            return null;
        }

        public JsonResult Search(string Date = "", List<string> Regions = null, List<string> OperatingUnits = null,
            List<string> RigTypes = null, List<string> RigNames = null, List<string> Projects = null,
            List<string> WellNames = null, List<string> Activities = null, List<string> ActivitiesCategory = null,
            List<string> PerformanceUnits = null, string Status = null, string OPType = "All",
            bool IncludeDiffOfLEAndCalcLE = true,
            bool IncludeNotEnteredLE = true,
            bool IncludeLeEqualCalc = true,
            string DateStart = "", string DateStart2 = "", string DateFinish = "", string DateFinish2 = "", string DateRelation = "OR",
            List<string> OPs = null, string OpRelation = "AND", bool doesnthavewr = true)
        {
            var qa = new List<IMongoQuery>();
            if (Regions != null) qa.Add(Query.In("Region", new BsonArray(Regions)));
            if (OperatingUnits != null) qa.Add(Query.In("OperatingUnit", new BsonArray(OperatingUnits)));
            if (RigTypes != null) qa.Add(Query.In("RigType", new BsonArray(RigTypes)));
            if (RigNames != null) qa.Add(Query.In("RigName", new BsonArray(RigNames)));
            if (Projects != null) qa.Add(Query.In("ProjectName", new BsonArray(Projects)));

            if (qa.Any())
            {
                var innerWellNames = WellActivity.Populate<WellActivity>(Query.And(qa), 0, 0, null, new string[] { "WellName" }).Select(d => d.WellName).Distinct().ToList();
                if (innerWellNames.Any())
                {
                    if (WellNames == null)
                    {
                        WellNames = new List<string>();
                    }
                    WellNames.AddRange(innerWellNames);
                }
            }



            //filter wa first based on is OP14 or NOTOP14
            var qWa = Query.Null;
            if (WellNames != null) qWa = Query.In("WellName", new BsonArray(WellNames));
            var actvInFilter = WellActivity.Populate<WellActivity>(qWa, 0, 0, null);
            var wa = actvInFilter.Select(x => new { x.WellName, x.NonOP }); // WellActivity.Populate<WellActivity>(qWa, 0, 0, null, new string[] { "WellName", "NonOP" });
            List<string> AllWellNames = new List<string>();
            if (OPType == "All")
            {
                foreach (var x in wa)
                {
                    AllWellNames.Add(x.WellName);
                }
            }
            else
            {
                if (OPType == "True")
                {
                    foreach (var x in wa.Where(d => d.NonOP.Equals(true)))
                    {
                        AllWellNames.Add(x.WellName);
                    }
                }
                else
                {
                    foreach (var x in wa.Where(d => d.NonOP.Equals(false)))
                    {
                        AllWellNames.Add(x.WellName);
                    }
                }
            }

            WellNames = AllWellNames;

            var wau = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(Query.Null, SortBy.Descending("UpdateVersion"));
            //DateTime dtThisMonday = wau != null ? Tools.GetNearestDay(wau.UpdateVersion.AddDays(-3),DayOfWeek.Monday) : Tools.GetNearestDay(DateTime.Now, DayOfWeek.Monday);
            DateTime dtThisMonday = Tools.GetNearestDay(DateTime.Now, DayOfWeek.Monday);
            //DateTime dtThisMonday = SearchDate == null ? ;
            var q = Query.Null;
            List<IMongoQuery> qs = new List<IMongoQuery>();
            qs.Add(WebTools.GetWellActivitiesQuery("WellName", "Phase.ActivityType"));
            var qdist = Query.And(Query.EQ("Status", "Distributed"), Query.EQ("UpdateVersion", dtThisMonday));
            var qnondist = Query.NE("Status", "Distributed");
            var qstatus = Query.Or(new[] { qdist, qnondist });
            qs.Add(qstatus);
            //if (SearchDate != null) qs.Add(Query.EQ("UpdateVersion", Tools.ToUTC((DateTime)SearchDate)));
            if (!String.IsNullOrEmpty(Date))
            {
                var y = DateTime.ParseExact("01-" + Date, "dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
                qs.Add(Query.EQ("UpdateVersion", Tools.ToUTC(y)));
            }
            if (WellNames != null)
            {
                var WellName = WellNames.Distinct().ToArray();
                qs.Add(Query.In("WellName", new BsonArray(WellName)));
            };
            if (Activities != null)
            {
                qs.Add(Query.In("Phase.ActivityType", new BsonArray(Activities)));
            }
            else
            {
                if (ActivitiesCategory != null && ActivitiesCategory.Count > 0)
                {
                    var getAct = ActivityMaster.Populate<ActivityMaster>(Query.In("ActivityCategory", new BsonArray(ActivitiesCategory)));
                    if (getAct.Any())
                    {
                        qs.Add(Query.In("Phase.ActivityType", new BsonArray(getAct.Select(d => d._id.ToString()).ToList())));
                    }
                }
            }
            if (PerformanceUnits != null) qs.Add(Query.Or(
                Query.In("Elements.PerformanceUnit", new BsonArray(PerformanceUnits)),
                Query.In("CRElements.PerformanceUnit", new BsonArray(PerformanceUnits))
            ));
            if (Status != null && Status != "") qs.Add(Query.EQ("Status", Status));

            if (OPs != null)
            {
                if (OPs.Count == 1)
                {
                    qs.Add(Query.EQ("Phase.BaseOP", OPs.FirstOrDefault()));
                }
                else
                {
                    if (OpRelation.ToLower() == "or")
                    {
                        qs.Add(Query.In("Phase.BaseOP", new BsonArray(OPs)));
                    }
                    else if (OpRelation.ToLower() == "not")
                    {
                        qs.Add(Query.NotIn("Phase.BaseOP", new BsonArray(OPs)));
                    }
                    else
                    {
                        List<IMongoQuery> subQuery = new List<IMongoQuery>();
                        foreach (var op in OPs)
                        {
                            subQuery.Add(Query.EQ("Phases.BaseOP", op));
                        }
                        //queries.Add(Query.ElemMatch("Phases", Query.And(Query.EQ("BaseOP", new BsonArray(OPs)))));
                        subQuery.Add((Query.Size("Phases.BaseOP", OPs.Count)));
                        qs.Add(Query.And(subQuery));
                    }
                }

            }
            if (qs.Count > 0) q = Query.And(qs);
            //List<WellActivityUpdateMonthly> was1 = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q);

            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var dashboardC = new DashboardController();

                List<WellActivityUpdateMonthly> was = new List<WellActivityUpdateMonthly>();

                List<WellActivityUpdateMonthly> monthlies = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q);

                List<WellActivityHelper> checkWR = new List<WellActivityHelper>();
                List<WellActivityHelper> checkWRs = new List<WellActivityHelper>();
                foreach(var y in monthlies)
                {
                    checkWRs.Add(new WellActivityHelper { SequenceId = y.SequenceId, ActivityType = y.Phase.ActivityType });

                }
                checkWR = WellActivity.CheckhaveWeeklyReport(checkWRs);

                foreach (var t in monthlies)
                {

                    List<WellActivity> wxs = actvInFilter.Where(x => x.WellName.Equals(t.WellName) &&
                        x.UARigSequenceId.Equals(t.SequenceId) &&
                        x.Phases.Where(y => y.ActivityType.Equals(t.Phase.ActivityType)).Count() > 0
                        ).ToList();
                    WellActivity wx = null;
                    if (wxs.Any())
                    {
                        wx = new WellActivity();
                        wx = wxs.FirstOrDefault();
                    }
                    //= WellActivity.Get<WellActivity>(Query.And(
                    //    Query.EQ("UARigSequenceId", t.SequenceId),
                    //    Query.EQ("Phases.ActivityType", t.Phase.ActivityType),
                    //    Query.EQ("WellName", t.WellName)
                    //));

                    var leDays = t.CurrentWeek.Days;
                    var leCost = t.CurrentWeek.Cost / 1000000;
                    var cleDays = t.CalculatedLE.Days;
                    var cleCost = t.CalculatedLE.Cost;

                    var diffDays = Math.Abs(leDays - cleDays);
                    var diffCost = Math.Abs(leCost - cleCost);

                    var isDaysSame = diffDays <= 0.005;
                    var isCostSame = diffCost <= 0.005;
                    bool isLE = false, isActual = false;
                    if (wx != null)
                    {
                        // cek filter
                        var phschedule = wx.Phases.Where(x => x.ActivityType.Equals(t.Phase.ActivityType)).FirstOrDefault().PhSchedule;

                        t.OpsSchedule = phschedule;
                        // is LE
                        isLE = dashboardC.dateBetween(phschedule, DateStart, DateFinish, DateStart2, DateFinish2, DateRelation);
                        // is Actal LE
                        isActual = wx.Phases.Where(x => x.ActivityType.Equals(t.Phase.ActivityType)).FirstOrDefault().IsActualLE;
                    }
                    else
                    {

                    }

                    // is LE Days Cost Equal
                    bool isLEDayCostEqual = false;
                    if (isDaysSame && isCostSame)
                        isLEDayCostEqual = true;
                    else
                        isLEDayCostEqual = false;


                    // is LE days Cost not Equal
                    bool isLenotcalcLE = false;
                    if (!isDaysSame || !isCostSame)
                        isLenotcalcLE = true;
                    else
                        isLenotcalcLE = false;

                    if (isLE)
                    {
                        var lenotentr = false;
                        var diffLenotequal = false;
                        var leequal = false;

                        if (IncludeNotEnteredLE)
                        {
                            if (!isActual)
                            {
                                lenotentr = true;
                            }
                        }
                        if (IncludeDiffOfLEAndCalcLE)
                        {
                            if (isLenotcalcLE && isActual)
                            {
                                diffLenotequal = true;
                            }
                        }

                        if (IncludeLeEqualCalc)
                        {
                            if (isLEDayCostEqual && isActual)
                            {
                                leequal = true;
                            }
                        }

                        //var isHaveWeeklyReport = WellActivity.isHaveWeeklyReport(t.WellName, t.SequenceId, t.Phase.ActivityType);

                        if (lenotentr || diffLenotequal || leequal)
                        {
                            if (doesnthavewr)
                            {
                                var tt = checkWR.FirstOrDefault(x => x.SequenceId.Equals(t.SequenceId) && x.ActivityType.Equals(t.Phase.ActivityType));
                                if (tt == null )
                                {
                                    was.Add(t);

                                }
                                else
                                {

                                }

                            }
                            else
                                was.Add(t);
                        }
                    }


                }
                #region remark
                //List<WellActivityUpdateMonthly> was = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q)
                //    .Where(d =>
                //    {
                //        d.OpsSchedule = new DateRange();

                //        var isIncludeDiffOfLEAndCalcLE = false;
                //        var isIncludeNotEnteredLE = false;
                //        var isincludeLeEqualCalc = false;

                //        var isLE = false;


                //        var waOfWaum = WellActivity.Get<WellActivity>(Query.And(
                //            Query.EQ("UARigSequenceId", d.SequenceId),
                //            Query.EQ("Phases.ActivityType", d.Phase.ActivityType),
                //            Query.EQ("WellName", d.WellName)
                //        ));

                //        if (waOfWaum != null)
                //        {
                //            var phOfWaum = waOfWaum.Phases.FirstOrDefault(e => d.Phase.ActivityType.Equals(e.ActivityType));
                //            if (phOfWaum != null)
                //            {
                //                isLE = dashboardC.dateBetween(phOfWaum.PhSchedule, DateStart, DateFinish, DateStart2, DateFinish2, DateRelation);
                //                d.OpsSchedule = phOfWaum.PhSchedule;
                //            }
                //        }

                //        var leDays = d.CurrentWeek.Days;
                //        var leCost = d.CurrentWeek.Cost / 1000000;
                //        var cleDays = d.CalculatedLE.Days;
                //        var cleCost = d.CalculatedLE.Cost;

                //        var diffDays = Math.Abs(leDays - cleDays);
                //        var diffCost = Math.Abs(leCost - cleCost);

                //        var isDaysSame = diffDays <= 0.005;
                //        var isCostSame = diffCost <= 0.005;


                //        // klo centang true
                //        if (IncludeNotEnteredLE)
                //        {
                //            if (!d.Phase.IsActualLE)
                //                isIncludeNotEnteredLE = true;
                //            else
                //                isIncludeNotEnteredLE = false;
                //        }
                //        else
                //            isIncludeNotEnteredLE = false;



                //        if (IncludeDiffOfLEAndCalcLE)
                //        {
                //            if (!isDaysSame || !isCostSame)
                //                isIncludeDiffOfLEAndCalcLE = true;
                //            else
                //                isIncludeDiffOfLEAndCalcLE = false;
                //        }
                //        else
                //            isIncludeDiffOfLEAndCalcLE = false;


                //        if (includeLeEqualCalc)
                //        {
                //            if (isDaysSame && isCostSame)
                //                isincludeLeEqualCalc = true;
                //            else
                //                isincludeLeEqualCalc = false;
                //        }
                //        else
                //            isincludeLeEqualCalc = false;





                //        return (isLE && isIncludeNotEnteredLE) && (isIncludeDiffOfLEAndCalcLE || isincludeLeEqualCalc);
                //    })
                //    .OrderBy(x => x.WellName).ToList();
                #endregion

                var ret = was.Where(x => x.OpsSchedule.Start != Tools.DefaultDate).Select(x => new
                {
                    x._id,
                    x.WellName,
                    x.Status,
                    x.Phase,
                    x.OpsSchedule,
                    x.Plan,
                    x.CurrentWeek,
                    x.CalculatedLE
                });

                return ret;
            });
        }

        public JsonResult lsInfo()
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                var lsInfo = new PIPAnalysisController().GetLatestLsDate();
                return lsInfo;
            });
        }

        public JsonResult SearchDistributed(DateTime? SearchDateFrom = null, DateTime? SearchDateTo = null, List<string> SearchWellNames = null, List<string> SearchActivities = null, string SearchStatus = null, string SearchKeyword = null)
        {
            var q = Query.Null;
            var qKeyword = Query.Null;
            var queryFix = Query.Null;
            List<IMongoQuery> qs = new List<IMongoQuery>();
            List<IMongoQuery> qsKeyword = new List<IMongoQuery>();

            //qs.Add(WebTools.GetWellActivitiesQuery("WellName", "Phase.ActivityType"));
            qs.Add(Query.LT("UpdateVersion", Tools.ToUTC((Tools.GetNearestDay(DateTime.Now, DayOfWeek.Monday)))));
            if (SearchDateFrom == null && SearchDateTo == null)
            {
                //-- do nothing
            }
            else
            {
                //if (SearchDateFrom != null) qs.Add(Query.GTE("UpdateVersion", Tools.ToUTC((DateTime)SearchDateFrom)));
                //if (SearchDateTo != null) qs.Add(Query.LTE("UpdateVersion", Tools.ToUTC((DateTime)SearchDateTo)));
                if (SearchDateTo != null) qs.Add(Query.LTE("UpdateVersion", Tools.GetNearestDay((DateTime)SearchDateTo, DayOfWeek.Monday)));
                if (SearchDateFrom != null) qs.Add(Query.GTE("UpdateVersion", Tools.GetNearestDay((DateTime)SearchDateFrom, DayOfWeek.Monday)));
            }

            if (SearchWellNames != null) qs.Add(Query.In("WellName", new BsonArray(SearchWellNames)));
            if (SearchActivities != null) qs.Add(Query.In("Phase.ActivityType", new BsonArray(SearchActivities)));
            if (SearchStatus != null && SearchStatus != "") qs.Add(Query.EQ("Status", SearchStatus));
            if (qs.Count > 0) q = Query.And(qs);

            if (SearchKeyword != "")
            {
                qsKeyword.Add(Query.Matches("ExecutiveSummary", new BsonRegularExpression(
                        new Regex(SearchKeyword.ToLower(), RegexOptions.IgnoreCase))));
                qsKeyword.Add(Query.Matches("PlannedOperation", new BsonRegularExpression(
                    new Regex(SearchKeyword.ToLower(), RegexOptions.IgnoreCase))));
                qsKeyword.Add(Query.Matches("SupplementReason", new BsonRegularExpression(
                    new Regex(SearchKeyword.ToLower(), RegexOptions.IgnoreCase))));
                qsKeyword.Add(Query.Matches("Elements.Title", new BsonRegularExpression(
                    new Regex(SearchKeyword.ToLower(), RegexOptions.IgnoreCase))));

                qKeyword = Query.Or(qsKeyword);
                queryFix = Query.And(q, qKeyword);
            }
            else
            {
                queryFix = q;
            }

            List<WellActivityUpdateMonthly> was1 = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(queryFix);

            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                List<WellActivityUpdateMonthly> was = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(queryFix).OrderByDescending(x => x.UpdateVersion).ToList();
                return was;
            });
        }


        public JsonResult Distribute(List<string> ids)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                var q = Query.In("_id", new BsonArray(ids));
                List<string> filenames = new List<string>();
                List<WellActivityUpdateMonthly> was = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q).OrderBy(x => x.WellName).ToList();
                string WellNames = "";
                List<string> toEmails = new List<string>();
                List<string> ccEmails = new List<string>();

                string filename = Export2Pdf(ids.ToArray());
                filenames.Add(filename);

                foreach (var wa in was)
                {
                    if (wa.Status.Equals("Submitted"))
                    {
                        wa.Status = "Distributed";
                        wa.Save();

                        string url = (HttpContext.Request).Path.ToString();
                        WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email, LogType.UpdateStatusToDistributed,
                            wa.TableName, url, wa.ToBsonDocument(), null);


                        WellNames += "- " + wa.WellName + " (" + wa.Phase.ActivityType + ")\r\n";
                        var tos = wa.Phase.GetPersonsInRole(wa.WellName,
                           Config.GetConfigValue("WEISWRPICRoles", "WELL-ENG").ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries))
                           .Select(dx => dx.Email).ToArray();
                        var ccs = wa.Phase.GetPersonsInRole(wa.WellName,
                                Config.GetConfigValue("WEISWRReviewersRoles", "").ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries))
                                .Select(dx => dx.Email).ToArray();
                        toEmails.AddRange(tos); ccEmails.AddRange(ccs);
                    }
                }

                Dictionary<string, string> variables = new Dictionary<string, string>();
                //variables.Add("WellName", wa.WellName);
                //variables.Add("ActivityType", wa.Phase.ActivityType);
                //variables.Add("UpdateVersion", String.Format("{0:dd-MMM-yyyy}", wa.UpdateVersion));
                //variables.Add("DueDate", String.Format("{0:dd-MMM-yyyy}", wa.UpdateVersion));

                variables.Add("UrlAction", "http://" + Request.Url.Host + Url.Action("index", "weeklyreport"));
                variables.Add("List", WellNames);
                Email.Send("WRDistribute",
                        toEmails.ToArray(),
                        ccMails: ccEmails.ToArray(),
                        variables: variables,
                        attachments: filenames,
                    //developerModeEmail: "eky.pradhana@eaciit.com");
                        developerModeEmail: WebTools.LoginUser.Email);
                return "OK";
            });
        }
        public JsonResult SendReminder(List<string> ids)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                var q = Query.In("_id", new BsonArray(ids));
                List<string> filenames = new List<string>();
                List<WellActivityUpdateMonthly> was = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q).OrderBy(x => x.WellName).ToList();
                string WellNames = "";

                foreach (var wa in was)
                {
                    List<string> toEmails = new List<string>();
                    List<string> ccEmails = new List<string>();
                    var tos = wa.Phase.GetPersonsInRole(wa.WellName,
                        Config.GetConfigValue("WEISWRPICRoles", "WELL-ENG").ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries))
                        .Select(dx => dx.Email).ToArray();
                    var ccs = wa.Phase.GetPersonsInRole(wa.WellName,
                            Config.GetConfigValue("WEISWRReviewersRoles", "").ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries))
                            .Select(dx => dx.Email).ToArray();
                    toEmails.AddRange(tos); ccEmails.AddRange(ccs);

                    Dictionary<string, string> variables = new Dictionary<string, string>();
                    variables.Add("WellName", wa.WellName);
                    variables.Add("ActivityType", wa.Phase.ActivityType);
                    variables.Add("UpdateVersion", String.Format("{0:dd-MMM-yyyy}", wa.UpdateVersion));
                    variables.Add("DueDate", String.Format("{0:dd-MMM-yyyy}", wa.UpdateVersion));

                    variables.Add("UrlAction", "http://" + Request.Url.Host + Url.Action("index", "monthlyreport"));
                    variables.Add("List", WellNames);
                    Email.Send("MRReminder",
                            toEmails.ToArray(),
                            ccMails: ccEmails.ToArray(),
                            variables: variables,
                            attachments: filenames,
                        //developerModeEmail: "eky.pradhana@eaciit.com");
                            developerModeEmail: WebTools.LoginUser.Email);
                }

                return "OK";
            });
        }

        public JsonResult GetAddActList(string SearchDate, List<string> WellNames, List<string> WellActivityIds)
        {
            DateTime y = DateTime.Now;
            if (!String.IsNullOrEmpty(SearchDate))
                y = DateTime.ParseExact(SearchDate, "MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);

            DateTime ylast = new DateTime(y.Year, y.Month, DateTime.DaysInMonth(y.Year, y.Month));

            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                var q = Query.Null;
                List<IMongoQuery> qas = new List<IMongoQuery>();
                if (WellNames != null) qas.Add(Query.In("WellName", new BsonArray(WellNames)));
                if (WellActivityIds != null) qas.Add(Query.In("Phases.ActivityType", new BsonArray(WellActivityIds)));
                if (qas.Count > 0) q = Query.And(qas);

                IMongoQuery q1 = Query.GTE("Phases.PhSchedule.Start", Tools.ToUTC(y));
                IMongoQuery q2 = Query.LTE("Phases.PhSchedule.Finish", Tools.ToUTC(ylast));
                var qDate = Query.Or(q1, q2);

                //string Risk = "Risk";
                //IMongoQuery qRisk = Query.Matches("Phases.ActivityType",
                //    new BsonRegularExpression(
                //        new Regex(Risk.ToLower(), RegexOptions.IgnoreCase)));
                //var qa = Query.Not(qRisk);

                IMongoQuery qFinal = Query.And(q, qDate);

                List<WellActivity> was1 = WellActivity.Populate<WellActivity>(qFinal);

                foreach (var c in was1)
                {
                    List<WellActivityPhase> phaseDel = c.Phases.Where(t => (t.PhSchedule.Start < Tools.ToUTC(y) && t.PhSchedule.Finish > Tools.ToUTC(y)) || t.ActivityType.Contains("RISK")).ToList();
                    if (phaseDel.Count > 0)
                    {
                        foreach (WellActivityPhase p in phaseDel)
                        {
                            c.Phases.Remove(p);
                        };
                    }
                }


                foreach (var x in was1)
                {
                    // Delete phases that exist in activityupdates on the selected date
                    if (x.Phases.Count > 0)
                    {
                        List<WellActivityPhase> dels = new List<WellActivityPhase>();
                        foreach (WellActivityPhase a in x.Phases)
                        {
                            List<IMongoQuery> qs = new List<IMongoQuery>();
                            IMongoQuery qua = Query.Null;
                            qs.Add(Query.EQ("WellName", x.WellName));
                            qs.Add(Query.EQ("SequenceId", x.UARigSequenceId));
                            qs.Add(Query.EQ("Phase.PhaseNo", a.PhaseNo));
                            qs.Add(Query.EQ("Phase.ActivityType", a.ActivityType));
                            qs.Add(Query.EQ("UpdateVersion", Tools.ToUTC(y)));
                            qua = Query.And(qs);
                            var wau = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(qua);
                            if (wau != null)
                            {
                                dels.Add(a);
                            }
                        }

                        if (dels.Count > 0)
                        {
                            foreach (var az in dels)
                            {
                                x.Phases.Remove(az);
                            }

                        }
                    }
                }

                //var yu = was1.Where(x => x.Phases.Count > 0).ToList();
                var yu = was1.Where(x => x.Phases.Count > 0)
                    .SelectMany(x => x.Phases, (x, p) => new
                    {
                        _id = String.Format("{0}|{1}", x._id, p.ActivityType),
                        x.WellName,
                        x.UARigSequenceId,
                        x.RigName,
                        x.AssetName,
                        p.ActivityType,
                        p.PhSchedule,
                        p.AFESchedule
                    })
                    .ToList();
                return yu;
            });

        }

        public JsonResult GetWork(string SearchDate)
        {
            DateTime y = DateTime.Now;
            if (!String.IsNullOrEmpty(SearchDate))
                y = DateTime.ParseExact(SearchDate, "dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);

            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                return GetActivitiesBasedOnMonth(y);
            });
        }

        public List<MonthlyEventsBasedOnMonth> GetActivitiesBasedOnMonth(DateTime y, string WellName = null, string ActivityType = null, string SequenceId = null)
        {
            IMongoQuery queries = null;
            //queries.Add(Query.GT("Phases.PhSchedule.Start", Tools.ToUTC(y)));
            if (WellName != null && ActivityType != null && SequenceId != null)
            {
                queries = Query.And(Query.EQ("WellName", WellName), Query.EQ("UARigSequenceId", SequenceId), Query.EQ("Phases.ActivityType", ActivityType));
            }

            //IMongoQuery q1 = Query.GT("Phases.PhSchedule.Start", Tools.ToUTC(y));
            List<WellActivity> was1 = WellActivity.Populate<WellActivity>(queries);

            //if (was1.Where(x => x.WellName.Equals("APPO STAT FAILURE PROTEUS 001")).Any())
            //{

            //}
            // hapus phase jika : 
            // t.PhSchedule.Start < y
            // t.ActivityType.Contains("RISK")
            foreach (var c in was1)
            {
                //List<WellActivityPhase> phaseDel = c.Phases.Where(t => t.PhSchedule.Start > Tools.ToUTC(y) || t.PhSchedule.Finish < Tools.ToUTC(y)).ToList();
                List<WellActivityPhase> phaseDel = c.Phases.Where(t => t.PhSchedule.Start < Tools.ToUTC(y) || t.ActivityType.Contains("RISK")).ToList();
                if (phaseDel.Count > 0)
                {
                    foreach (WellActivityPhase p in phaseDel)
                    {
                        c.Phases.Remove(p);
                    };
                }
            }

            foreach (var x in was1)
            {
                // Delete phases that exist in activityupdates on the selected date
                if (x.Phases.Count > 0)
                {
                    List<WellActivityPhase> dels = new List<WellActivityPhase>();
                    foreach (WellActivityPhase a in x.Phases)
                    {
                        List<IMongoQuery> qs = new List<IMongoQuery>();
                        IMongoQuery qua = Query.Null;
                        qs.Add(Query.EQ("WellName", x.WellName));
                        qs.Add(Query.EQ("SequenceId", x.UARigSequenceId));
                        qs.Add(Query.EQ("Phase.PhaseNo", a.PhaseNo));
                        qs.Add(Query.EQ("Phase.ActivityType", a.ActivityType));
                        qs.Add(Query.EQ("UpdateVersion", Tools.ToUTC(y)));
                        qua = Query.And(qs);
                        var wau = WellActivityUpdate.Get<WellActivityUpdate>(qua);
                        if (wau != null)
                        {
                            dels.Add(a);
                        }
                    }

                    if (dels.Count > 0)
                    {
                        foreach (var az in dels)
                        {
                            x.Phases.Remove(az);
                        }

                    }
                }
            }

            var yu = was1.Where(x => x.Phases.Count > 0)
                .SelectMany(x => x.Phases, (x, p) => new MonthlyEventsBasedOnMonth()
                {
                    _id = String.Format("{0}|{1}", x._id, p.ActivityType),
                    WellName = x.WellName,
                    UARigSequenceId = x.UARigSequenceId,
                    RigName = x.RigName,
                    AssetName = x.AssetName,
                    ActivityType = p.ActivityType,
                    PhSchedule = p.PhSchedule,
                    AFESchedule = p.AFESchedule
                })
                .ToList();
            return yu;
        }

        public List<MonthlyEventsBasedOnMonth> GetActivitiesStartedFromMonth(DateTime y)
        {
            IMongoQuery q1 = Query.GTE("Phases.PhSchedule.Start", Tools.ToUTC(y));
            List<WellActivity> was1 = WellActivity.Populate<WellActivity>(q1);

            // hapus phase jika : 
            // t.PhSchedule.Start < y
            // t.ActivityType.Contains("RISK")
            foreach (var c in was1)
            {
                //List<WellActivityPhase> phaseDel = c.Phases.Where(t => t.PhSchedule.Start > Tools.ToUTC(y) || t.PhSchedule.Finish < Tools.ToUTC(y)).ToList();
                List<WellActivityPhase> phaseDel = c.Phases.Where(t => t.PhSchedule.Start < Tools.ToUTC(y) || t.ActivityType.Contains("RISK")).ToList();

                if (phaseDel.Count > 0)
                {
                    foreach (WellActivityPhase p in phaseDel)
                    {
                        c.Phases.Remove(p);
                    };
                }
            }


            foreach (var x in was1)
            {
                // Delete phases that exist in activityupdates on the selected date
                if (x.Phases.Count > 0)
                {
                    List<WellActivityPhase> dels = new List<WellActivityPhase>();
                    foreach (WellActivityPhase a in x.Phases)
                    {
                        List<IMongoQuery> qs = new List<IMongoQuery>();
                        IMongoQuery qua = Query.Null;
                        qs.Add(Query.EQ("WellName", x.WellName));
                        qs.Add(Query.EQ("SequenceId", x.UARigSequenceId));
                        qs.Add(Query.EQ("Phase.PhaseNo", a.PhaseNo));
                        qs.Add(Query.EQ("Phase.ActivityType", a.ActivityType));
                        qs.Add(Query.EQ("UpdateVersion", Tools.ToUTC(y)));
                        qua = Query.And(qs);
                        var wau = WellActivityUpdate.Get<WellActivityUpdate>(qua);
                        if (wau != null)
                        {
                            dels.Add(a);
                        }
                    }

                    if (dels.Count > 0)
                    {
                        foreach (var az in dels)
                        {
                            x.Phases.Remove(az);
                        }

                    }
                }
            }

            var yu = was1.Where(x => x.Phases.Count > 0)
                .SelectMany(x => x.Phases, (x, p) => new MonthlyEventsBasedOnMonth()
                {
                    _id = String.Format("{0}|{1}", x._id, p.ActivityType),
                    WellName = x.WellName,
                    UARigSequenceId = x.UARigSequenceId,
                    RigName = x.RigName,
                    AssetName = x.AssetName,
                    ActivityType = p.ActivityType,
                    PhSchedule = p.PhSchedule,
                    AFESchedule = p.AFESchedule
                })
                .ToList();
            return yu;
        }

        public JsonResult GetSequences(Dictionary<string, object> doc)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                IMongoQuery q = Query.EQ("Phases.Status", "Active");
                List<WellActivity> was = WellActivity.Populate<WellActivity>(q);
                List<WellActivity> ret = new List<WellActivity>();
                foreach (var w in was)
                {
                    foreach (var p in w.Phases.Where(x => x.Status.Equals("Active")))
                    {
                        var r = BsonSerializer.Deserialize<WellActivity>(w.ToBsonDocument());
                        r.Phases.Clear();
                        r.Phases.Add(p);
                        ret.Add(r);
                    }
                }
                return ret;
            });
        }

        public JsonResult SelectSequence(string id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                WellActivityUpdateMonthly upd = new WellActivityUpdateMonthly();
                var wa = WellActivity.Get<WellActivity>(Query.EQ("UARigSequenceId", id));
                if (wa != null)
                {
                    upd.Country = "USA";
                    upd.NewWell = false;
                    upd.WellName = wa.WellName;
                    upd.AssetName = wa.AssetName;
                    upd.SequenceId = wa.UARigSequenceId;
                    var phase = wa.Phases.FirstOrDefault(x => x.Status.Equals("Active"));
                    if (phase != null)
                    {
                        upd.Phase.ActivityType = wa.Phases[0].ActivityType;
                        upd.Phase.ActivityDesc = wa.Phases[0].ActivityDesc;
                    }
                }
                return new
                {
                    Record = upd
                };
            });
        }

        public JsonResult Submit(string id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                var wa = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(id);
                if (wa != null)
                {
                    wa.Status = "Submitted";
                    wa.SubmitBy = WebTools.LoginUser.UserName;
                    wa.Save(references: new string[] { "", "CalcLeSchedule" });

                    string url = (HttpContext.Request).Path.ToString();
                    WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email, LogType.UpdateStatusToSubmitted,
                        wa.TableName, url, wa.ToBsonDocument(), null);


                    var fileName = Export2Pdf(id);

                    Dictionary<string, string> variables = new Dictionary<string, string>();
                    variables.Add("WellName", wa.WellName);
                    variables.Add("ActivityType", wa.Phase.ActivityType);
                    variables.Add("UpdateVersion", String.Format("{0:dd-MMM-yyyy}", wa.UpdateVersion));
                    variables.Add("DueDate", String.Format("{0:dd-MMM-yyyy}", wa.UpdateVersion));
                    variables.Add("UrlAction", "http://" + Request.Url.Host + Url.Action("index", "weeklyreport"));
                    var toEmails = wa.Phase.GetPersonsInRole(wa.WellName,
                                Config.GetConfigValue("WEISWRPICRoles", "WELL-ENG").ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries))
                                .Select(dx => dx.Email).ToArray();
                    var ccEmails = wa.Phase.GetPersonsInRole(wa.WellName,
                                Config.GetConfigValue("WEISWRReviewersRoles", "WELL-ENG").ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries))
                                .Select(dx => dx.Email).ToArray();
                    Email.Send("WRSubmit",
                        toMails: toEmails, ccMails: ccEmails, variables: variables,
                            attachments: new string[] { fileName },
                            developerModeEmail: WebTools.LoginUser.Email);
                }
                return wa;
            });
        }

        private static double getValuePIP(string DefaultOP, List<PIPElement> PIPElements, string which, string what = "Days")
        {
            PIPElements.ForEach(x =>
            {
                if (x.Classification == null) x.Classification = string.Empty;
            });
            if (what == "Days")
            {
                var gg = PIPElements.Where(x => x.Classification.ToString().ToLower().Equals(which.ToLower()) && x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).ToList();
                var hh = PIPElements.Where(x => x.Classification.ToString().ToLower().Equals(which.ToLower()) && x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP) && x.Title.Equals("Deepening enabled design")).ToList();

                var dt = PIPElements.Where(x => x.Classification.ToString().ToLower().Equals(which.ToLower()) && x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.DaysCurrentWeekImprovement) +
                        PIPElements.Where(x => x.Classification.ToString().ToLower().Equals(which.ToLower()) && x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.DaysCurrentWeekRisk);
                return dt;
            }
            else
            {
                var gg = PIPElements.Where(x => x.Classification.ToString().ToLower().Equals(which.ToLower()) && x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).ToList();
                var hh = PIPElements.Where(x => x.Classification.ToString().ToLower().Equals(which.ToLower()) && x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP) && x.Title.Equals("Deepening enabled design")).ToList();
                var dt = PIPElements.Where(x => x.Classification.ToString().ToLower().Equals(which.ToLower()) && x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.CostCurrentWeekImprovement) +
                        PIPElements.Where(x => x.Classification.ToString().ToLower().Equals(which.ToLower()) && x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.CostCurrentWeekRisk);
                return dt;
            }
        }

        public JsonResult Select(string id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                WellActivityUpdateMonthly raw = GetUpdateData(id);

                string previousOP = "";
                string nextOP = "";
                var dm = new DataModel2Controller();
                var DefaultOP = getBaseOP(out previousOP, out nextOP);

                //foreach (var e in raw.Elements)
                //{
                //    e.CostPlanImprovement = e.CostPlanImprovement + e.CostPlanRisk;
                //    e.DaysPlanImprovement = e.DaysPlanImprovement + e.DaysPlanRisk;
                //}
                raw.Actual.Cost = Tools.Div(raw.Actual.Cost, 1000000);
                WellActivityActual act = WellActivityActual.GetById(raw.WellName, raw.SequenceId, null, true, false);

                //decide which RIG Element is shown or not
                List<PIPElement> RigPIPs = new List<PIPElement>();
                var PIPElements = raw.Elements;
                var PIPElem = raw.Elements.FirstOrDefault(x => x.Title.Equals("9-3/8 liner bowspring vs centralizer subs"));
                var totalRealizedCost = PIPElements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement) +
                    PIPElements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.CostCurrentWeekRisk);

                var totalRealizedDays = PIPElements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement) +
                    PIPElements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.DaysCurrentWeekRisk);


                //var totalRealizedCostCurrentOP = PIPElements.Where(x => x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.CostCurrentWeekImprovement) +
                //    PIPElements.Where(x => x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.CostCurrentWeekRisk);

                //var totalRealizedDaysCurrentOP = PIPElements.Where(x => x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.DaysCurrentWeekImprovement) +
                //    PIPElements.Where(x => x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.DaysCurrentWeekRisk);

                var totalRealizedSupplyChainTransformationDays = getValuePIP(DefaultOP, PIPElements, "Supply Chain Transformation");
                var totalRealizedCompetitiveScopeDays = getValuePIP(DefaultOP, PIPElements, "Competitive Scope");
                var totalRealizedEfficientExecutionDays = getValuePIP(DefaultOP, PIPElements, "Efficient Execution");
                var totalRealizedTechnologyandInnovationDays = getValuePIP(DefaultOP, PIPElements, "Technology and Innovation");



                var totalRealizedSupplyChainTransformationCost = getValuePIP(DefaultOP, PIPElements, "Supply Chain Transformation", "Cost");
                var totalRealizedCompetitiveScopeCost = getValuePIP(DefaultOP, PIPElements, "Competitive Scope", "Cost");
                var totalRealizedEfficientExecutionCost = getValuePIP(DefaultOP, PIPElements, "Efficient Execution", "Cost");
                var totalRealizedTechnologyandInnovationCost = getValuePIP(DefaultOP, PIPElements, "Technology and Innovation", "Cost");


                var totalRealizedDaysCurrentOP = totalRealizedSupplyChainTransformationDays +
                                                 totalRealizedCompetitiveScopeDays + totalRealizedEfficientExecutionDays +
                                                 totalRealizedTechnologyandInnovationDays;

                var totalRealizedCostCurrentOP = totalRealizedSupplyChainTransformationCost +
                                                 totalRealizedCompetitiveScopeCost + totalRealizedEfficientExecutionCost +
                                                 totalRealizedTechnologyandInnovationCost;

                //raw.Elements =
                //    raw.Elements.Where(
                //        x => x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).ToList();

                if (raw.CRElements != null && raw.CRElements.Count > 0)
                {
                    foreach (var cre in raw.CRElements)
                    {
                        var isOverlapping = false;
                        foreach (var pip in PIPElements)
                        {
                            string msg = "";
                            double overlappingDays = 0.0;
                            isOverlapping = DateRangeToMonth.isDateRangeOverlaping(cre.Period, pip.Period, out msg, out overlappingDays);
                            if (isOverlapping)
                            {
                                RigPIPs.Add(cre);
                                break;
                            }
                        }
                    }
                    raw.CRElements = RigPIPs;
                }
                else
                {
                    raw.CRElements = RigPIPs;
                }

                var rigName = "";

                var wa = WellActivity.Get<WellActivity>(Query.And(
                    Query.EQ("WellName", raw.WellName),
                    Query.EQ("UARigSequenceId", raw.SequenceId),
                    Query.EQ("Phases.ActivityType", raw.Phase.ActivityType)
                ));
                if (raw.OpsSchedule == null)
                {
                    raw.OpsSchedule = new DateRange();
                }
                if (wa != null)
                {
                    rigName = wa.RigName;
                    var ph = wa.Phases.FirstOrDefault(e => e.ActivityType.Equals(raw.Phase.ActivityType));
                    if (ph != null)
                    {
                        raw.Phase.IsActualLE = ph.IsActualLE;
                        raw.OpsSchedule = ph.PhSchedule;
                    }
                }

                raw.Comment = "";
                foreach (var xx in raw.Elements)
                {
                    xx.Comments = RenderComment((string)raw._id, xx.ElementId.ToString());
                    //xx.Comments = new List<WEISComment>();
                    //xx.Comments.Add(new WEISComment()
                    //{
                    //    Comment = 
                    //});
                }

                //get comments
                string frm = string.Format("{0}||{1}||{2}", raw.WellName, raw.Phase.ActivityType, raw.SequenceId);

                var monthlyReportComments = WEISComment.Populate<WEISComment>(q: Query.And(Query.EQ("ReferenceType", "MonthlyReport"),
                    Query.EQ("Reference1", frm)
                    )) ?? new List<WEISComment>();
                if (monthlyReportComments.Any())
                {
                    raw.Comments = monthlyReportComments.OrderByDescending(x => x.LastUpdate).ToList();
                }

                var isHaveWeeklyReport = WellActivity.isHaveWeeklyReport(raw.WellName, raw.SequenceId, raw.Phase.ActivityType);

                return new
                {
                    totalRealizedCost = totalRealizedCost,
                    totalRealizedDays = totalRealizedDays,
                    Record = raw,
                    HaveWeeklyReport = isHaveWeeklyReport,
                    HasEDM = act != null,
                    RigName = rigName,
                    totalRealizedCostCurrentOP,
                    totalRealizedDaysCurrentOP,
                    totalRealizedCompetitiveScopeDays,
                    totalRealizedEfficientExecutionDays,
                    totalRealizedSupplyChainTransformationDays,
                    totalRealizedTechnologyandInnovationDays,
                    totalRealizedCompetitiveScopeCost,
                    totalRealizedEfficientExecutionCost,
                    totalRealizedSupplyChainTransformationCost,
                    totalRealizedTechnologyandInnovationCost,
                };
            });
        }

        public JsonResult DeleteElement(string ActivityUpdateId, int ElementId)
        {
            var wau = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(Query.EQ("_id", ActivityUpdateId));
            var temp = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(Query.EQ("_id", ActivityUpdateId));
            if (wau != null)
            {
                if (wau.Elements != null)
                {
                    wau.Elements = wau.Elements.Where(d => d.ElementId != ElementId).ToList();
                    wau.Save();

                    string url = (HttpContext.Request).Path.ToString();
                    WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email, LogType.UpdateOnDelete,
                        wau.TableName, url, temp.ToBsonDocument(), wau.ToBsonDocument());


                    var pip = WellPIP.GetByWellActivity(wau.WellName, wau.Phase.ActivityType);
                    var temppip = WellPIP.GetByWellActivity(wau.WellName, wau.Phase.ActivityType);
                    if (pip != null)
                    {
                        if (pip.Elements != null)
                        {
                            pip.Elements = pip.Elements.Where(d => d.ElementId != ElementId).ToList();
                            pip.Save(references: new string[] { });

                            WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email, LogType.UpdateOnDelete,
                                pip.TableName, url, pip.ToBsonDocument(), temppip.ToBsonDocument());

                            //Delete Comments
                            new CommentController().DeleteComment("WeeklyReport", ActivityUpdateId, ElementId);

                            return Json(new { Success = true, Note = "PIP Element Deleted" }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    return Json(new { Success = true, Note = "PIP Element Deleted." }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
        }

        public Double GetDayOrCost(PIPElement d, string DayorCost = "Day", string element = "Plan")
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

        public JsonResult GetWaterfallData(WellActivityUpdateMonthly wau,
            string DayOrCost = "Day", string BaseView = "OP",
            string GroupBy = "Day", bool IncludeZero = false,
            bool IncludeCR = false, bool ByRealised = false)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var wellPIPController = new WellPIPController();
                var division = 1000000.0;
                var Target = DayOrCost.Equals("Day") ? wau.CurrentWeek.Days : wau.CurrentWeek.Cost;
                var AFE = DayOrCost.Equals("Day") ? wau.AFE.Days : wau.AFE.Cost;
                var OP = DayOrCost.Equals("Day") ? wau.Plan.Days : wau.Plan.Cost;
                var Start = BaseView == "OP" && wau.Plan.Days != 0 ? OP : AFE;
                var StartTitle = BaseView == "OP" && wau.Plan.Days != 0 ? "OP" : "AFE";

                if (DayOrCost.Equals("Cost"))
                {
                    if (Target > 10000)
                    {
                        Target /= division;
                    }
                    //AFE /= division;
                    //OP /= division;

                    if (Start > 10000)
                    {
                        Start /= division;
                    }
                }

                List<PIPElement> PIPs = new List<PIPElement>();
                WellPIP wpip = WellPIP.GetByWellActivity(wau.WellName, wau.Phase.ActivityType);
                if (wpip != null) PIPs.AddRange(wpip.Elements);
                var final = new List<WaterfallItem>();
                //final.Add(new WaterfallItem(0, "OP", OP, ""));
                final.Add(new WaterfallItem(0, StartTitle, Start, ""));

                foreach (var pip in PIPs)
                {
                    var e = wau.Elements.FirstOrDefault(d => d.ElementId.Equals(pip.ElementId));
                    if (e != null)
                    {
                        pip.DaysCurrentWeekImprovement = e.DaysCurrentWeekImprovement;
                        pip.DaysCurrentWeekRisk = e.DaysCurrentWeekRisk;
                        pip.CostCurrentWeekImprovement = e.CostCurrentWeekImprovement;
                        pip.CostCurrentWeekRisk = e.CostCurrentWeekRisk;
                    }
                }

                if (ByRealised)
                {
                    var LE = Target;

                    if (PIPs == null)
                    {
                        return new
                        {
                            MinHeight = (new Double[] { 0, OP, AFE, LE }.Min()),
                            MaxHeight = (new Double[] { OP, AFE, LE }.Max() * 1.3),
                            OP = OP,
                            AFE = AFE,
                            LE = LE,
                            GapsLE = LE - (0 + OP),
                            DataLE = new List<WaterfallItemByRealised>()
                        };
                    }

                    var groupPIPS = PIPs
                        .GroupBy(d => new
                        {
                            GroupBy = d.ToBsonDocument().GetString(GroupBy),
                            Completion = Convert.ToString(d.Completion)
                        })
                        .Select(d => new
                        {
                            GroupBy = d.Key.GroupBy,
                            Completion = d.Key.Completion,
                            LE = d.Sum(e => GetDayOrCost(e, DayOrCost, "LE")),
                            IsPositive = (d.FirstOrDefault() ?? new WFPIPElement()).isPositive
                        })
                        .ToList();

                    var gapLE = 0.0;
                    var dataLE = groupPIPS.Where(d => (!IncludeZero && d.LE != 0) || IncludeZero)
                        .GroupBy(d => d.GroupBy)
                        .Select(d =>
                        {
                            gapLE += d.Sum(e => e.LE);

                            var result = new WaterfallItemByRealised()
                            {
                                Title = d.Key,
                                IsPositive = d.FirstOrDefault().IsPositive
                            };

                            foreach (var eachSubGroup in d.GroupBy(e => e.Completion == "Realized" ? "Realised" : "Unrealised"))
                            {
                                if (eachSubGroup.Key == "Realised")
                                    result.Realised = eachSubGroup.Sum(e => e.LE);
                                else
                                    result.Unrealised = eachSubGroup.Sum(e => e.LE);
                            }

                            if (wpip.Type == "Reduction" && IncludeCR)
                            {
                                var elementsReduction = wpip.CRElements
                                    .Where(f => f.ToBsonDocument().GetString(GroupBy).Equals(d.Key));

                                try
                                {
                                    result.CRRealised = elementsReduction
                                        .Where(f => f.Completion.Equals("Realized"))
                                        .DefaultIfEmpty(new PIPElement())
                                        .Sum(f => GetDayOrCost(f, DayOrCost));
                                }
                                catch (Exception) { }
                                try
                                {
                                    result.CRUnrealised = elementsReduction
                                        .Where(f => !f.Completion.Equals("Realized"))
                                        .DefaultIfEmpty(new PIPElement())
                                        .Sum(f => GetDayOrCost(f, DayOrCost));
                                }
                                catch (Exception) { }
                            }

                            result.Realized = Math.Abs(result.CRRealised + result.Realised);
                            result.Unrealized = Math.Abs(result.CRUnrealised + result.Unrealised);

                            return result;
                        }).ToList();

                    return new
                    {
                        MinHeight = (new Double[] { 0, OP, AFE, LE }.Min()),
                        MaxHeight = (new Double[] { OP, AFE, LE }.Max() * 1.3),
                        OP = OP,
                        AFE = AFE,
                        LE = LE,
                        StartTitle = StartTitle,
                        GapsLE = LE - (gapLE + OP),
                        DataLE = dataLE,
                    };
                }

                if (PIPs.Count > 0)
                {
                    var groupPIPS = PIPs.GroupBy(d => d.ToBsonDocument().GetString(GroupBy))
                        .Select(d =>
                        {
                            var Plan = d.Sum(e => GetDayOrCost(e, DayOrCost, "Plan"));
                            var LE = d.Sum(e => GetDayOrCost(e, DayOrCost, "LE"));

                            return new
                            {
                                d.Key,
                                Plan = Plan,
                                LE = LE == 0 ? Plan : LE
                            };
                        })
                        .ToList();

                    double gap = 0;
                    foreach (var gp in groupPIPS
                        .Where(d => (!IncludeZero && d.LE != 0) || IncludeZero)
                        .OrderByDescending(d => d.LE))
                    {
                        final.Add(new WaterfallItem(0.1, gp.Key + "(P)", gp.LE, ""));
                        gap += gp.LE;
                    }
                    if (gap + AFE != Target)
                    {
                        final.Add(new WaterfallItem(0.1, "Gap (P)", Target - (gap + Start), ""));
                    }
                }
                else
                {
                    final.Add(new WaterfallItem(0.1, "Gap (P)", Target - Start, ""));
                }


                final.Add(new WaterfallItem(1, "LE", Target, "total"));

                return final;
            });
        }

        private WellActivityUpdateMonthly GetUpdateData(string id)
        {
            WellActivityUpdateMonthly upd = new WellActivityUpdateMonthly();
            var wa = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(id);
            if (wa != null)
            {
                wa.Calc();
                var LEDaysRealizedRisk = wa.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekRisk);
                var LEDaysRealizedOpp = wa.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement);
                var RD = LEDaysRealizedRisk + LEDaysRealizedOpp;
                var RC = wa.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekRisk + x.CostCurrentWeekImprovement);

                var URD = wa.Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekRisk + x.DaysCurrentWeekImprovement);
                var URC = wa.Elements.Where(x => !x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekRisk + x.CostCurrentWeekImprovement);

                wa.RealizedDays = Math.Round(RD, 1);
                wa.RealizedCost = Math.Round(RC, 1);
                wa.UnRealizedDays = Math.Round(URD, 1);
                wa.UnRealizedCost = Math.Round(URC, 1);

                //Gap = LE - (OP14 + Realized PIP)
                var LEDays = wa.Elements.Sum(x => x.DaysCurrentWeekRisk + x.DaysCurrentWeekImprovement);
                var LECost = wa.Elements.Sum(x => x.CostCurrentWeekRisk + x.CostCurrentWeekImprovement);
                var OPDays = wa.Elements.Sum(x => x.DaysPlanRisk + x.DaysPlanImprovement);
                var OPCost = wa.Elements.Sum(x => x.CostPlanRisk + x.CostPlanImprovement);
                //wa.GapsDays = Math.Round(wa.CurrentWeek.Days - (wa.Plan.Days + RD),1);
                //wa.GapsCost = Math.Round(Tools.Div(wa.CurrentWeek.Cost - (wa.Plan.Cost + RC),1000000),1);
                wa.GapsDays = Math.Round(LEDays - (OPDays + RD), 1);
                wa.GapsCost = Math.Round(LECost - (OPCost + RC), 1);
            }


            //string WellName = wa.WellName;
            //var WellActivity = wa.GetWellActivity().ActivityType;

            //var wp = WellPIP.GetByWellActivity(WellName, WellActivity);
            ////if(wp!=null)wa.Elements = wp.Elements;

            //DateTime UpdateVersion = wa.UpdateVersion;
            //#region old ... need to be changed
            ////foreach (var x in wa.Elements)
            ////{
            ////    if (wp != null)
            ////    {
            ////        var o = wp.Elements.FirstOrDefault(d1 => d1.ElementId.Equals(x.ElementId));
            ////        if (o != null)
            ////        {
            ////            var a = new DateRange { Start = UpdateVersion, Finish = o.Period.Start };
            ////            var b = new DateRange { Start = o.Period.Finish, Finish = o.Period.Start };
            ////            x.CompletionPerc = System.Math.Round(Tools.Div(a.Days, b.Days, 0, 2), 1);
            ////            if (x.CompletionPerc < 0) x.CompletionPerc = 0;
            ////            if (x.CompletionPerc > 1) x.CompletionPerc = 1;
            ////        }
            ////    }
            ////}
            //#endregion

            //var es = new List<PIPElement>();
            //foreach (var o in wp.Elements)
            //{   
            //    var x = wp.Elements.FirstOrDefault(d1 => d1.ElementId.Equals(o.ElementId));
            //    //if (x != null)
            //    //{
            //    //    var a = new DateRange { Start = UpdateVersion, Finish = o.Period.Start };
            //    //    var b = new DateRange { Start = o.Period.Finish, Finish = o.Period.Start };
            //    //    x.CompletionPerc = System.Math.Round(Tools.Div(a.Days, b.Days, 0, 2), 1);
            //    //    if (x.CompletionPerc < 0) x.CompletionPerc = 0;
            //    //    if (x.CompletionPerc > 1) x.CompletionPerc = 1;
            //    //}
            //    if (x != null) es.Add(x); else es.Add(o);
            //}
            //wa.Elements = es;
            return wa;
        }

        public JsonResult RecalcPIPElements(List<PIPElement> Elements)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                string previousOP = "";
                string nextOP = "";
                var dm = new DataModel2Controller();
                var DefaultOP = getBaseOP(out previousOP, out nextOP);
                var PIPElements = Elements;

                var totalRealizedCost = PIPElements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement) +
                    PIPElements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.CostCurrentWeekRisk);

                var totalRealizedDays = PIPElements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement) +
                    PIPElements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.DaysCurrentWeekRisk);


                //var totalRealizedCostCurrentOP = PIPElements.Where(x => x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.CostCurrentWeekImprovement) +
                //    PIPElements.Where(x => x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.CostCurrentWeekRisk);

                //var totalRealizedDaysCurrentOP = PIPElements.Where(x => x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.DaysCurrentWeekImprovement) +
                //    PIPElements.Where(x => x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.DaysCurrentWeekRisk);

                var totalRealizedSupplyChainTransformationDays = getValuePIP(DefaultOP, PIPElements, "Supply Chain Transformation");
                var totalRealizedCompetitiveScopeDays = getValuePIP(DefaultOP, PIPElements, "Competitive Scope");
                var totalRealizedEfficientExecutionDays = getValuePIP(DefaultOP, PIPElements, "Efficient Execution");
                var totalRealizedTechnologyandInnovationDays = getValuePIP(DefaultOP, PIPElements, "Technology and Innovation");

                var totalRealizedSupplyChainTransformationCost = getValuePIP(DefaultOP, PIPElements, "Supply Chain Transformation", "Cost");
                var totalRealizedCompetitiveScopeCost = getValuePIP(DefaultOP, PIPElements, "Competitive Scope", "Cost");
                var totalRealizedEfficientExecutionCost = getValuePIP(DefaultOP, PIPElements, "Efficient Execution", "Cost");
                var totalRealizedTechnologyandInnovationCost = getValuePIP(DefaultOP, PIPElements, "Technology and Innovation", "Cost");

                var totalRealizedDaysCurrentOP = totalRealizedSupplyChainTransformationDays +
                                                 totalRealizedCompetitiveScopeDays + totalRealizedEfficientExecutionDays +
                                                 totalRealizedTechnologyandInnovationDays;

                var totalRealizedCostCurrentOP = totalRealizedSupplyChainTransformationCost +
                                                 totalRealizedCompetitiveScopeCost + totalRealizedEfficientExecutionCost +
                                                 totalRealizedTechnologyandInnovationCost;

                return new
                {
                    totalRealizedCost = totalRealizedCost,
                    totalRealizedDays = totalRealizedDays,
                    totalRealizedCostCurrentOP,
                    totalRealizedDaysCurrentOP,
                    totalRealizedCompetitiveScopeDays,
                    totalRealizedEfficientExecutionDays,
                    totalRealizedSupplyChainTransformationDays,
                    totalRealizedTechnologyandInnovationDays,
                    totalRealizedCompetitiveScopeCost,
                    totalRealizedEfficientExecutionCost,
                    totalRealizedSupplyChainTransformationCost,
                    totalRealizedTechnologyandInnovationCost
                };
            });
        }

        public JsonResult SavePDFChart()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                HttpPostedFileBase file = Request.Files[0];
                var nm = DateTime.Now.ToString("ddmmyyyyHHmmss") + ".pdf";
                var fnnm = "TempChartPDF" + nm;
                var pth = Path.Combine(Server.MapPath("~/App_Data/Temp"), fnnm);
                file.SaveAs(pth);
                //string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"]);
                //bool exists = System.IO.Directory.Exists(folder);

                //string fileNameReplace = DateTime.Now.ToString("yyyMMddhhmmss") + "-" + fileName;

                //if (!exists)
                //    System.IO.Directory.CreateDirectory(folder);

                //string filepath = System.IO.Path.Combine(folder, fileNameReplace);
                //files.SaveAs(filepath);
                return fnnm;
            });
        }
        public JsonResult Save(WellActivityUpdateMonthly model, bool isForPrint = false) //isForPrint
        {
            #region Update WellPIP with only the Latest WellActivityUpdateMonthly
            //List<IMongoQuery> qs = new List<IMongoQuery>();
            //qs.Add(Query.EQ("WellName", model.WellName));
            //qs.Add(Query.EQ("SequenceId", model.SequenceId));

            //var listActivityUpdate = DataHelper.Populate<WellActivityUpdateMonthly>(model.TableName, Query.And(qs));

            //if (listActivityUpdate != null && listActivityUpdate.Count > 0)
            //{
            //    WellActivityUpdateMonthly updater = new WellActivityUpdateMonthly();
            //    if (listActivityUpdate.Count() > 1)
            //        updater = listActivityUpdate.OrderByDescending(x => x.UpdateVersion).FirstOrDefault();
            //    else
            //        updater = listActivityUpdate.FirstOrDefault();

            //    // Update Well PIP Element only Completion 
            //    var pip = DataHelper.Populate<WellPIP>("WEISWellPIPs", Query.And(qs));
            //    if (pip != null && pip.Count > 0)
            //    {
            //        WellPIP wellPip = pip.FirstOrDefault();
            //        foreach (var elemenUp in updater.Elements)
            //        {
            //            if (wellPip.Elements.Where(x => x.ElementId.Equals(elemenUp.ElementId)).Count() > 0)
            //            {
            //                wellPip.Elements.Where(x => x.ElementId.Equals(elemenUp.ElementId)).FirstOrDefault().Completion = elemenUp.Completion;
            //            }
            //        }
            //        wellPip.Save();
            //    }
            //}
            #endregion
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                model.Actual.Cost *= 1000000;

                //var existing = 
                //if (existing != null)
                //{
                //    model._id = existing._id;
                //}
                //else
                //{
                //    model._id = SequenceNo.Get(new WellActivityUpdateMonthly().TableName).ClaimAsInt();
                //}
                //model.CurrentWeek.Cost = model.CurrentWeek.Cost * 1000000;

                List<PIPElement> ListElement = new List<PIPElement>();
                PIPElement Element = new PIPElement();
                List<PIPAllocation> ListAllocation = new List<PIPAllocation>();
                PIPAllocation Allocation = new PIPAllocation();
                WellActivityUpdateMonthly NewWau = new WellActivityUpdateMonthly();
                WellActivityUpdateMonthly wau = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(model._id);
                WellActivityUpdateMonthly temp = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(model._id);

                NewWau = model;

                foreach (var x in model.Elements)
                {
                    Element = x;
                    PIPElement pe = wau.Elements.Where(a => a.ElementId.Equals(x.ElementId)).FirstOrDefault();
                    List<PIPAllocation> NewAllocation = new List<PIPAllocation>();

                    if (pe != null)
                    {
                        if (x.CostCurrentWeekImprovement != pe.CostCurrentWeekImprovement ||
                                x.DaysCurrentWeekImprovement != pe.DaysCurrentWeekImprovement ||
                                x.CostCurrentWeekRisk != pe.CostCurrentWeekRisk ||
                                x.DaysCurrentWeekRisk != pe.DaysCurrentWeekRisk)
                        {
                            //NewAllocation = null;
                            double TotalDays = (pe.Period.Finish - pe.Period.Start).TotalDays;
                            double diff = Tools.Div(TotalDays, 30);
                            var mthNumber = Math.Ceiling(diff);
                            for (var mthIdx = 0; mthIdx < mthNumber; mthIdx++)
                            {
                                var dt = pe.Period.Start.AddMonths(mthIdx);
                                if (dt > pe.Period.Finish) dt = pe.Period.Finish;
                                NewAllocation.Add(new PIPAllocation
                                {
                                    AllocationID = mthIdx + 1,
                                    Period = dt,
                                    CostPlanImprovement = Math.Round(Tools.Div(pe.CostPlanImprovement, mthNumber), 1),
                                    CostPlanRisk = Math.Round(Tools.Div(pe.CostPlanRisk, mthNumber), 1),
                                    DaysPlanImprovement = Math.Round(Tools.Div(pe.DaysPlanImprovement, mthNumber), 1),
                                    DaysPlanRisk = Math.Round(Tools.Div(pe.DaysPlanRisk, mthNumber), 1),
                                    LEDays = Math.Round(Tools.Div(x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk, mthNumber), 1),
                                    LECost = Math.Round(Tools.Div(x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk, mthNumber), 1)
                                });
                            }
                        }
                        else
                        {
                            NewAllocation = pe.Allocations;
                        }

                        //decide is positive or negative?
                        Element.isPositive = true;
                        if ((Element.CostPlanImprovement + Element.CostPlanRisk) - (Element.CostCurrentWeekImprovement + Element.CostCurrentWeekRisk) >= 0)
                        {
                            Element.isPositive = false;
                        }

                        Element.Allocations = NewAllocation;

                        //Element.Period = pe.Period;
                        //Element.AssignTOOps = pe.AssignTOOps.Distinct().ToList();

                        ListElement.Add(Element);
                    }
                    else
                    {
                        ListElement.Add(x);
                    }
                }

                NewWau.LastUpdateBy = WebTools.LoginUser.UserName;

                NewWau.Elements = ListElement;



                NewWau.Save(references: new string[] { "", "CalcLeSchedule", "isActualLE" });

                ///create comment to weis comment
                if (!isForPrint)
                    CreateMonthlyComment(NewWau.WellName, NewWau.Phase.ActivityType, NewWau.SequenceId, NewWau.Comment, NewWau._id.ToString());

                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                    LogType.Update, NewWau.TableName, url, temp.ToBsonDocument(), NewWau.ToBsonDocument());

                //string url = (HttpContext.Request).Path.ToString();
                //WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                //    LogType.Insert, NewWau.TableName, url, NewWau.ToBsonDocument(), null);

                return NewWau;
            });

        }

        public void CreateMonthlyComment(string wellName, string activity, string sequenceid, string comment, string _id)
        {
            string frm = string.Format("{0}||{1}||{2}", wellName, activity, sequenceid);

            var weisCom = new WEISComment();
            weisCom.Comment = comment;
            weisCom.User = WebTools.LoginUser.UserName;
            weisCom.Email = WebTools.LoginUser.Email;
            weisCom.ReferenceType = "MonthlyReport";
            weisCom.Reference1 = frm;
            weisCom.Reference2 = _id;//RigName ?? "";
            //weisCom.ParentId = _id;
            weisCom.Save();
        }
        public ActionResult PrintDocument(string id, string WellName)
        {
            ViewBag.Id = id;
            ViewBag.WellName = WellName;
            return View();
        }

        public FileResult Print2Pdf(string id, string pdfNm1 = "", string pdfNm2 = "")
        {
            var newFileName = Export2Pdf(id, pdfNm1, pdfNm2);
            WellActivityUpdateMonthly raw = GetUpdateData(id);

            #region Update by Yoga

            return File(newFileName, Tools.GetContentType(".pdf"), Path.GetFileName(newFileName));

            #endregion
            //return File(newFileName, Tools.GetContentType(".pdf"), newFileName + ".pdf");
        }

        public string Export2Pdf(string id, string fileName1 = "", string fileName2 = "")
        {
            return Export2Pdf(new string[] { id }, fileName1, fileName2);
            //string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "wrtemplate.xlsx");
            //string xlst2 = Path.Combine(Server.MapPath("~/App_Data/Temp"), "wrtemplate2.xlsx");
            //if (System.IO.File.Exists(xlst) == false) throw new Exception("Template file is not exist: " + xlst);
            //WellActivityUpdateMonthly wau = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(id);
            //#region Change by Yoga 14.01.2015
            //string idx = wau.WellName.Replace("/", "")
            //            .Replace("\\", "")
            //            .Replace(":", "")
            //            .Replace("*", "")
            //            .Replace("?", "")
            //            .Replace("\"", "")
            //            .Replace("<", "")
            //            .Replace(">", "")
            //            .Replace("|", "").Replace(" ", "").Trim();

            //string newFileName = Path.Combine(Server.MapPath("~/App_Data/Temp"),
            //   String.Format("WR-{0}-{1}.pdf", idx, DateTime.Now.ToString("ddMMMyy")));
            ////string newFileName = Path.Combine(Server.MapPath("~/App_Data/Temp"),
            ////   String.Format("WR_{0}.pdf", id));
            //#endregion

            //if (wau.SupplementLast7Days)
            //{
            //    #region Template 1 
            //    if (wau != null)
            //    {
            //        var wb = new Workbook(xlst);
            //        var ws = wb.Worksheets[0];
            //        WellActivityPhase a = wau.GetWellActivity();

            //        ws.Cells["D7"].Value = wau.Company;
            //        ws.Cells["D8"].Value = wau.Project;
            //        ws.Cells["D9"].Value = wau.Site;
            //        ws.Cells["D10"].Value = wau.WellName;
            //        ws.Cells["D11"].Value = wau.WellType;
            //        ws.Cells["D12"].Value = wau.EventType;
            //        ws.Cells["D13"].Value = wau.Objective;
            //        ws.Cells["D14"].Value = a.PhSchedule.Start;
            //        ws.Cells["E7"].Value = wau.Contractor;
            //        ws.Cells["E8"].Value = wau.WorkUnit;
            //        ws.Cells["E9"].Value = wau.RigSuperintendent;
            //        ws.Cells["E10"].Value = wau.WellEngineer;
            //        ws.Cells["E11"].Value = wau.OriginalSpudDate;
            //        ws.Cells["E12"].Value = wau.OriginalSpudDate > Tools.DefaultDate ? (wau.UpdateVersion - wau.OriginalSpudDate).TotalDays : 0;
            //        ws.Cells["E13"].Value = wau.Actual.Days;
            //        ws.Cells["E14"].Value = wau.OP.Days;


            //        ws.Cells["B3"].Value = String.Format("WELL {0}", wau.WellName);
            //        ws.Cells["B4"].Value = String.Format("Date: {0:dd-MMMM-yyyy}", wau.UpdateVersion);

            //        ws.Cells["B17"].Value = wau.ExecutiveSummary;
            //        ws.Cells["B26"].Value = wau.OperationSummary;
            //        ws.Cells["B35"].Value = wau.PlannedOperation;
            //        ws.Cells["B44"].Value = wau.SupplementReason;

            //        ws.Cells["C54"].Value = Tools.Div(wau.AFE.Cost, 1000000);
            //        ws.Cells["H54"].Value = wau.AFE.Days;
            //        ws.Cells["C55"].Value = Tools.Div(wau.Actual.Cost, 1000000);
            //        ws.Cells["H55"].Value = wau.Actual.Days;
            //        ws.Cells["C56"].Value = Tools.Div(wau.CurrentWeek.Cost, 1000000);
            //        ws.Cells["H56"].Value = wau.CurrentWeek.Days;

            //        //var idxPip = 40;
            //        //foreach (var pip in wau.Elements)
            //        //{
            //        //    ws.Cells["B" + idxPip.ToString()].Value = pip.Classification;
            //        //    ws.Cells["C" + idxPip.ToString()].Value = pip.Title;
            //        //    ws.Cells["F" + idxPip.ToString()].Value = pip.DaysPlanImprovement < 0 ? "Opp" : "Risk";
            //        //    ws.Cells["G" + idxPip.ToString()].Value = pip.CostPlanImprovement + pip.CostPlanRisk;
            //        //    ws.Cells["H" + idxPip.ToString()].Value = pip.CostLastWeekImprovement + pip.CostLastWeekRisk;
            //        //    ws.Cells["I" + idxPip.ToString()].Value = pip.CostCurrentWeekImprovement + pip.CostCurrentWeekRisk;
            //        //    ws.Cells["J" + idxPip.ToString()].Value = pip.DaysPlanImprovement + pip.DaysPlanRisk;
            //        //    ws.Cells["K" + idxPip.ToString()].Value = pip.DaysLastWeekImprovement + pip.DaysLastWeekRisk;
            //        //    ws.Cells["L" + idxPip.ToString()].Value = pip.DaysCurrentWeekImprovement + pip.DaysCurrentWeekRisk;
            //        //    idxPip += 1;
            //        //}

            //        wb.Save(newFileName, Aspose.Cells.SaveFormat.Pdf);
            //    }
            //    #endregion
            //}
            //else
            //{
            //    #region Template 2
            //    if (wau != null)
            //    {
            //        var wb = new Workbook(xlst2);
            //        var ws = wb.Worksheets[0];
            //        WellActivityPhase a = wau.GetWellActivity();

            //        ws.Cells["D7"].Value = wau.Company;
            //        ws.Cells["D8"].Value = wau.Project;
            //        ws.Cells["D9"].Value = wau.Site;
            //        ws.Cells["D10"].Value = wau.WellName;
            //        ws.Cells["D11"].Value = wau.WellType;
            //        ws.Cells["D12"].Value = wau.EventType;
            //        ws.Cells["D13"].Value = wau.Objective;
            //        ws.Cells["D14"].Value = a.PhSchedule.Start;
            //        ws.Cells["E7"].Value = wau.Contractor;
            //        ws.Cells["E8"].Value = wau.WorkUnit;
            //        ws.Cells["E9"].Value = wau.RigSuperintendent;
            //        ws.Cells["E10"].Value = wau.WellEngineer;
            //        ws.Cells["E11"].Value = wau.OriginalSpudDate;
            //        ws.Cells["E12"].Value = wau.OriginalSpudDate > Tools.DefaultDate ? (wau.UpdateVersion - wau.OriginalSpudDate).TotalDays : 0;
            //        ws.Cells["E13"].Value = wau.Actual.Days;
            //        ws.Cells["E14"].Value = wau.OP.Days;


            //        ws.Cells["B3"].Value = String.Format("WELL {0}", wau.WellName);
            //        ws.Cells["B4"].Value = String.Format("Date: {0:dd-MMMM-yyyy}", wau.UpdateVersion);

            //        ws.Cells["B17"].Value = wau.ExecutiveSummary;
            //        ws.Cells["B29"].Value = wau.OperationSummary; // Last 7 Day Summary 
            //        ws.Cells["B42"].Value = wau.PlannedOperation; // Planned Summary
            //        //ws.Cells["B44"].Value = wau.SupplementReason;

            //        ws.Cells["C55"].Value = Tools.Div(wau.AFE.Cost, 1000000);
            //        ws.Cells["H55"].Value = wau.AFE.Days;
            //        ws.Cells["C56"].Value = Tools.Div(wau.Actual.Cost, 1000000);
            //        ws.Cells["H56"].Value = wau.Actual.Days;
            //        ws.Cells["C57"].Value = Tools.Div(wau.CurrentWeek.Cost, 1000000);
            //        ws.Cells["H57"].Value = wau.CurrentWeek.Days;

            //        //var idxPip = 40;
            //        //foreach (var pip in wau.Elements)
            //        //{
            //        //    ws.Cells["B" + idxPip.ToString()].Value = pip.Classification;
            //        //    ws.Cells["C" + idxPip.ToString()].Value = pip.Title;
            //        //    ws.Cells["F" + idxPip.ToString()].Value = pip.DaysPlanImprovement < 0 ? "Opp" : "Risk";
            //        //    ws.Cells["G" + idxPip.ToString()].Value = pip.CostPlanImprovement + pip.CostPlanRisk;
            //        //    ws.Cells["H" + idxPip.ToString()].Value = pip.CostLastWeekImprovement + pip.CostLastWeekRisk;
            //        //    ws.Cells["I" + idxPip.ToString()].Value = pip.CostCurrentWeekImprovement + pip.CostCurrentWeekRisk;
            //        //    ws.Cells["J" + idxPip.ToString()].Value = pip.DaysPlanImprovement + pip.DaysPlanRisk;
            //        //    ws.Cells["K" + idxPip.ToString()].Value = pip.DaysLastWeekImprovement + pip.DaysLastWeekRisk;
            //        //    ws.Cells["L" + idxPip.ToString()].Value = pip.DaysCurrentWeekImprovement + pip.DaysCurrentWeekRisk;
            //        //    idxPip += 1;
            //        //}

            //        wb.Save(newFileName, Aspose.Cells.SaveFormat.Pdf);
            //    }
            //    #endregion
            //}

            //return newFileName;
        }

        public string Export2Pdf(string[] ids, string fileName1 = "", string fileName2 = "")
        {
            ///--- change this to
            /// open 2 template, and create 2 ws object from each template
            /// create new excel file --- WR_{0}.pdf, where 0 is Date of Report in format ddMMMyyyy
            /// iterate for each id
            /// copy respective template into new worksheet based on wr for respective and populate the data accordingly
            /// save the file
            /// return the filename
            //string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "wrtemplate.xlsx");
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "monthlyLEWorflowTemplate.xlsx");
            if (System.IO.File.Exists(xlst) == false) throw new Exception("Template file is not exist: " + xlst);
            //if (System.IO.File.Exists(xlst2) == false) throw new Exception("Template file is not exist: " + xlst2);

            List<Workbook> workbooks = new List<Workbook>();
            int sheet = 0;

            string newFileName = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                 String.Format("WR-{0}.pdf", DateTime.Now.ToString("dd-MMM-yyyy")));

            string newFileNameSingle = "";

            Workbook wb = new Workbook();
            foreach (string id in ids)
            {
                WellActivityUpdateMonthly wau = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(id);
                #region Change by Yoga 14.01.2015
                //string idx = wau.WellName.Replace("/", "")
                //            .Replace("\\", "")
                //            .Replace(":", "")
                //            .Replace("*", "")
                //            .Replace("?", "")
                //            .Replace("\"", "")
                //            .Replace("<", "")
                //            .Replace(">", "")
                //            .Replace("|", "").Replace(" ", "").Trim();

                //newFileName = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                //   String.Format("WR-{0}-{1}.pdf", idx, DateTime.Now.ToString("ddMMMyy")));
                //string newFileName = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                //   String.Format("WR_{0}.pdf", id));
                #endregion

                if (wau != null)
                {
                    string previousOP = "";
                    string nextOP = "";
                    var DefaultOP = getBaseOP(out previousOP, out nextOP);
                    wb = new Workbook(xlst);
                    var ws = wb.Worksheets[0];
                    WellActivityPhase a = wau.GetWellActivity();
                    List<PIPElement> RigPIPs = new List<PIPElement>();
                    var PIPElements = wau.Elements;

                    var totalRealizedCost = PIPElements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement) +
                        PIPElements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.CostCurrentWeekRisk);

                    var totalRealizedDays = PIPElements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement) +
                        PIPElements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.DaysCurrentWeekRisk);


                    //var totalRealizedCostCurrentOP = PIPElements.Where(x => x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.CostCurrentWeekImprovement) +
                    //    PIPElements.Where(x => x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.CostCurrentWeekRisk);

                    //var totalRealizedDaysCurrentOP = PIPElements.Where(x => x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.DaysCurrentWeekImprovement) +
                    //    PIPElements.Where(x => x.Completion.ToString().Equals("Realized") && x.AssignTOOps.Contains(DefaultOP)).Sum(x => x.DaysCurrentWeekRisk);

                    var totalRealizedSupplyChainTransformationDays = getValuePIP(DefaultOP, PIPElements, "Supply Chain Transformation");
                    var totalRealizedCompetitiveScopeDays = getValuePIP(DefaultOP, PIPElements, "Competitive Scope");
                    var totalRealizedEfficientExecutionDays = getValuePIP(DefaultOP, PIPElements, "Efficient Execution");
                    var totalRealizedTechnologyandInnovationDays = getValuePIP(DefaultOP, PIPElements, "Technology and Innovation");

                    var totalRealizedSupplyChainTransformationCost = getValuePIP(DefaultOP, PIPElements, "Supply Chain Transformation", "Cost");
                    var totalRealizedCompetitiveScopeCost = getValuePIP(DefaultOP, PIPElements, "Competitive Scope", "Cost");
                    var totalRealizedEfficientExecutionCost = getValuePIP(DefaultOP, PIPElements, "Efficient Execution", "Cost");
                    var totalRealizedTechnologyandInnovationCost = getValuePIP(DefaultOP, PIPElements, "Technology and Innovation", "Cost");

                    var totalRealizedDaysCurrentOP = totalRealizedSupplyChainTransformationDays +
                                                 totalRealizedCompetitiveScopeDays + totalRealizedEfficientExecutionDays +
                                                 totalRealizedTechnologyandInnovationDays;

                    var totalRealizedCostCurrentOP = totalRealizedSupplyChainTransformationCost +
                                                 totalRealizedCompetitiveScopeCost + totalRealizedEfficientExecutionCost +
                                                 totalRealizedTechnologyandInnovationCost;


                    if (a == null)
                        throw new Exception(String.Format("Unable to process: {0} {1}. Please check respective Well Plan setting", wau.WellName, wau.Phase.ActivityType));
                    string idx = wau.WellName.Replace("/", "")
                  .Replace("\\", "")
                  .Replace(":", "")
                  .Replace("*", "")
                  .Replace("?", "")
                  .Replace("\"", "")
                  .Replace("<", "")
                  .Replace(">", "")
                  .Replace("|", "").Replace(" ", "").Trim();

                    newFileNameSingle = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                       String.Format("WR-{0}-{1}.pdf", idx, DateTime.Now.ToString("dd-MMMM-yyyy")));

                    var wa = WellActivity.Get<WellActivity>(Query.And(
                        Query.EQ("WellName", wau.WellName),
                        Query.EQ("UARigSequenceId", wau.SequenceId),
                        Query.EQ("Phases.ActivityType", wau.Phase.ActivityType)
                    )) ?? new WellActivity();

                    //header
                    ws.Cells["B3"].Value = wa.WellName;
                    ws.Cells["B4"].Value = "Date: " + wau.EventStartDate.ToString("dd-MMM-yyyy");

                    ws.Cells["D7"].Value = wa.RigName;
                    ws.Cells["D8"].Value = wau.WellName;
                    ws.Cells["D9"].Value = wau.GetWellActivity().ActivityType;
                    ws.Cells["I7"].Value = wau.EventStartDate;
                    ws.Cells["I8"].Value = wau.OriginalSpudDate > Tools.DefaultDate ? (wau.UpdateVersion - wau.OriginalSpudDate).TotalDays : 0; //wau.OriginalSpudDate;
                    //ws.Cells["D12"].Value = wau.GetWellActivity().ActivityType; //wau.EventType;
                    //ws.Cells["D13"].Value = wau.Objective;
                    //ws.Cells["D14"].Value = a.PhSchedule.Start;
                    //ws.Cells["I7"].Value = wau.Contractor;
                    //ws.Cells["I8"].Value = wau.WorkUnit;
                    //ws.Cells["I9"].Value = wau.RigSuperintendent;
                    //ws.Cells["I10"].Value = wau.WellEngineer;
                    //ws.Cells["I11"].Value = wau.OriginalSpudDate;
                    //ws.Cells["I12"].Value = wau.OriginalSpudDate > Tools.DefaultDate ? (wau.UpdateVersion - wau.OriginalSpudDate).TotalDays : 0;
                    //ws.Cells["I13"].Value = wau.Actual.Days;
                    //ws.Cells["I14"].Value = wau.OP.Days;

                    ws.Cells["E14"].Value = wau.Plan.Days == 0 ? "" : Math.Round(wau.Plan.Days, 2).ToString();
                    ws.Cells["F14"].Value = wau.Plan.Cost == 0 ? "" : Math.Round(Tools.Div(wau.Plan.Cost, 1000000), 2).ToString();
                    ws.Cells["E15"].Value = wau.AFE.Days == 0 ? "" : Math.Round(wau.AFE.Days, 2).ToString();
                    ws.Cells["F15"].Value = wau.AFE.Cost == 0 ? "" : Math.Round(Tools.Div(wau.AFE.Cost, 1000000), 2).ToString();
                    ws.Cells["E16"].Value = wau.LastWeek.Days == 0 ? "" : Math.Round(wau.LastWeek.Days, 2).ToString();
                    ws.Cells["F16"].Value = wau.LastWeek.Cost == 0 ? "" : Math.Round(Tools.Div(wau.LastWeek.Cost, 1000000), 2).ToString();
                    ws.Cells["E17"].Value = wau.TQ.Days == 0 ? "" : Math.Round(wau.TQ.Days, 2).ToString();
                    ws.Cells["F17"].Value = wau.TQ.Cost == 0 ? "" : Math.Round(Tools.Div(wau.TQ.Cost, 1000000), 2).ToString();
                    ws.Cells["E18"].Value = wau.CurrentWeek.Days == 0 ? "" : Math.Round(wau.CurrentWeek.Days, 2).ToString();
                    ws.Cells["F18"].Value = wau.CurrentWeek.Cost == 0 ? "" : Math.Round(Tools.Div(wau.CurrentWeek.Cost, 1000000), 2).ToString();
                    ws.Cells["E19"].Value = wau.CalculatedLE.Days == 0 ? "" : Math.Round(wau.CalculatedLE.Days, 2).ToString();
                    ws.Cells["F19"].Value = wau.CalculatedLE.Cost == 0 ? "" : Math.Round(Tools.Div(wau.CalculatedLE.Cost, 1), 2).ToString();// I dont know why, but the value already devide by 1000000
                    ws.Cells["E20"].Value = "";//wau.CalculatedLE.Days, 2); in view showing as 0. so, i follow it
                    ws.Cells["F20"].Value = "";//wau.CalculatedLE.Cost;
                    ws.Cells["E21"].Value = wau.BestInClass.Days == 0 ? "" : Math.Round(wau.BestInClass.Days, 2).ToString();
                    ws.Cells["F21"].Value = wau.BestInClass.Cost == 0 ? "" : Math.Round(Tools.Div(wau.BestInClass.Cost, 1000000), 2).ToString();
                    ws.Cells["E22"].Value = "";//wau.BestInClass.Days, 2);in view showing as 0. so, i follow it
                    ws.Cells["F22"].Value = "";//wau.BestInClass.Cost;

                    ws.Cells["E24"].Value = wau.Phase.LevelOfEstimate;//wau.BestInClass.Cost;
                    //LevelOfEstimate

                    //Realized PIPs
                    ws.Cells["K14"].Value = totalRealizedSupplyChainTransformationDays == 0 ? "" : Math.Round(totalRealizedSupplyChainTransformationDays, 2).ToString();
                    ws.Cells["L14"].Value = totalRealizedSupplyChainTransformationCost == 0 ? "" : Math.Round(Tools.Div(totalRealizedSupplyChainTransformationCost, 1), 2).ToString();
                    ws.Cells["K15"].Value = totalRealizedCompetitiveScopeDays == 0 ? "" : Math.Round(totalRealizedCompetitiveScopeDays, 2).ToString();
                    ws.Cells["L15"].Value = totalRealizedCompetitiveScopeCost == 0 ? "" : Math.Round(Tools.Div(totalRealizedCompetitiveScopeCost, 1), 2).ToString();
                    ws.Cells["K16"].Value = totalRealizedEfficientExecutionDays == 0 ? "" : Math.Round(totalRealizedEfficientExecutionDays, 2).ToString();
                    ws.Cells["L16"].Value = totalRealizedEfficientExecutionCost == 0 ? "" : Math.Round(Tools.Div(totalRealizedEfficientExecutionCost, 1), 2).ToString();
                    ws.Cells["K17"].Value = totalRealizedTechnologyandInnovationDays == 0 ? "" : Math.Round(totalRealizedTechnologyandInnovationDays, 2).ToString();
                    ws.Cells["L17"].Value = totalRealizedTechnologyandInnovationCost == 0 ? "" : Math.Round(Tools.Div(totalRealizedTechnologyandInnovationCost, 1), 2).ToString();
                    ws.Cells["K18"].Value = totalRealizedDaysCurrentOP == 0 ? "" : Math.Round(totalRealizedDaysCurrentOP, 2).ToString();
                    ws.Cells["L18"].Value = totalRealizedCostCurrentOP == 0 ? "" : Math.Round(Tools.Div(totalRealizedCostCurrentOP, 1), 2).ToString();

                    //banked saving wau.BankedSavingsSupplyChainTransformation + wau.BankedSavingsCompetitiveScope + wau.BankedSavingsEfficientExecution + wau.BankedSavingsTechnologyAndInnovation
                    ws.Cells["K22"].Value = wau.BankedSavingsSupplyChainTransformation.Days == 0 ? "" : Math.Round(wau.BankedSavingsSupplyChainTransformation.Days, 2).ToString();
                    ws.Cells["L22"].Value = wau.BankedSavingsSupplyChainTransformation.Cost == 0 ? "" : Math.Round(Tools.Div(wau.BankedSavingsSupplyChainTransformation.Cost, 1), 2).ToString();
                    ws.Cells["K23"].Value = wau.BankedSavingsCompetitiveScope.Days == 0 ? "" : Math.Round(wau.BankedSavingsCompetitiveScope.Days, 2).ToString();
                    ws.Cells["L23"].Value = wau.BankedSavingsCompetitiveScope.Cost == 0 ? "" : Math.Round(Tools.Div(wau.BankedSavingsCompetitiveScope.Cost, 1), 2).ToString();
                    ws.Cells["K24"].Value = wau.BankedSavingsEfficientExecution.Days == 0 ? "" : Math.Round(wau.BankedSavingsEfficientExecution.Days, 2).ToString();
                    ws.Cells["L24"].Value = wau.BankedSavingsEfficientExecution.Cost == 0 ? "" : Math.Round(Tools.Div(wau.BankedSavingsEfficientExecution.Cost, 1), 2).ToString();
                    ws.Cells["K25"].Value = wau.BankedSavingsTechnologyAndInnovation.Days == 0 ? "" : Math.Round(wau.BankedSavingsTechnologyAndInnovation.Days, 2).ToString();
                    ws.Cells["L25"].Value = wau.BankedSavingsTechnologyAndInnovation.Cost == 0 ? "" : Math.Round(Tools.Div(wau.BankedSavingsTechnologyAndInnovation.Cost, 1), 2).ToString();

                    var daysTotThis = wau.BankedSavingsSupplyChainTransformation.Days +
                                      wau.BankedSavingsCompetitiveScope.Days +
                                      wau.BankedSavingsEfficientExecution.Days +
                                      wau.BankedSavingsTechnologyAndInnovation.Days;
                    var costTotThis = wau.BankedSavingsSupplyChainTransformation.Cost +
                                      wau.BankedSavingsCompetitiveScope.Cost +
                                      wau.BankedSavingsEfficientExecution.Cost +
                                      wau.BankedSavingsTechnologyAndInnovation.Cost;
                    //total banked saving
                    ws.Cells["K26"].Value = daysTotThis == 0 ? "" : Math.Round(daysTotThis, 2).ToString();
                    ws.Cells["L26"].Value = costTotThis == 0 ? "" : Math.Round(Tools.Div(costTotThis, 1), 2).ToString();

                    var calcDeltaDays = wau.CurrentWeek.Days - wau.Plan.Days;
                    var calcDeltaCost = wau.CurrentWeek.Cost - wau.Plan.Cost;
                    //le - op
                    ws.Cells["K28"].Value = calcDeltaDays == 0 ? "" : Math.Round(calcDeltaDays, 2).ToString();
                    ws.Cells["L28"].Value = calcDeltaCost == 0 ? "" : Math.Round(Tools.Div(calcDeltaCost, 1000000), 2).ToString();

                    //Realized PIPs + Banked Savings		
                    ws.Cells["K29"].Value = (totalRealizedDaysCurrentOP + daysTotThis) == 0 ? "" : Math.Round(totalRealizedDaysCurrentOP + daysTotThis, 2).ToString();
                    ws.Cells["L29"].Value = (totalRealizedCostCurrentOP + costTotThis) == 0 ? "" : Math.Round(Tools.Div(totalRealizedCostCurrentOP + costTotThis, 1), 2).ToString();


                    ws.Cells["K30"].Value = "";//Math.Round(totalRealizedDaysCurrentOP + daysTotThis, 2);
                    ws.Cells["L30"].Value = "";//Math.Round(Tools.Div(totalRealizedCostCurrentOP + costTotThis, 1000000), 2);


                    var monthlyReportComments = WEISComment.Populate<WEISComment>(q: Query.And(Query.EQ("ReferenceType", "MonthlyReport"), Query.EQ("Reference1", wau.WellName), Query.EQ("Reference2", wau.RigName ?? ""))) ?? new List<WEISComment>();
                    if (monthlyReportComments.Any())
                    {
                        wau.Comments = monthlyReportComments.OrderByDescending(x => x.LastUpdate).ToList();
                    }
                    var st = 32;
                    if (wau.Comments == null)
                    {
                        wau.Comments = new List<WEISComment>();
                    }
                    var max = wau.Comments.Take(5).Count();
                    var idxxx = 0;
                    foreach (var xx in wau.Comments.Take(5))
                    {
                        idxxx++;
                        Range range = ws.Cells.CreateRange("B" + st, "G" + st);
                        range.Merge();
                        range.Value = xx.User + " | " + xx.LastUpdate.ToString("dddd dd, MMMM yyyy  HH:mm:ss");
                        range.SetOutlineBorder(BorderType.TopBorder, CellBorderType.Thin, System.Drawing.Color.Black);
                        //range.SetOutlineBorder(BorderType.BottomBorder, CellBorderType.Thin, System.Drawing.Color.Black);
                        //range.SetOutlineBorder(BorderType.RightBorder, CellBorderType.Thin, System.Drawing.Color.Black);
                        range.SetOutlineBorder(BorderType.LeftBorder, CellBorderType.Medium, System.Drawing.Color.Black);

                        range = ws.Cells.CreateRange("H" + st, "L" + st);
                        range.Merge();
                        //range.Value = xx.LastUpdate.ToString("dddd dd, MMMM yyyy  HH:mm:ss");
                        range.SetOutlineBorder(BorderType.TopBorder, CellBorderType.Thin, System.Drawing.Color.Black);
                        range.SetOutlineBorder(BorderType.BottomBorder, CellBorderType.Thin, System.Drawing.Color.Black);
                        range.SetOutlineBorder(BorderType.RightBorder, CellBorderType.Medium, System.Drawing.Color.Black);
                        //range.SetOutlineBorder(BorderType.LeftBorder, CellBorderType.Thin, System.Drawing.Color.Black);

                        st++;

                        range = ws.Cells.CreateRange("B" + st, "L" + (st + 1));
                        range.Merge();
                        range.Value = xx.Comment;//.LastUpdate.ToString("dddd dd, MMMM yyyy  HH:mm:ss");
                        range.SetOutlineBorder(BorderType.TopBorder, CellBorderType.Thin, System.Drawing.Color.Black);
                        //range.SetOutlineBorder(BorderType.BottomBorder, CellBorderType.Thin, System.Drawing.Color.Black);
                        range.SetOutlineBorder(BorderType.RightBorder, CellBorderType.Medium, System.Drawing.Color.Black);
                        range.SetOutlineBorder(BorderType.LeftBorder, CellBorderType.Medium, System.Drawing.Color.Black);

                        range = ws.Cells.CreateRange("B" + (st + 2), "L" + (st + 2));
                        range.Merge();
                        range.SetOutlineBorder(BorderType.RightBorder, CellBorderType.Medium, System.Drawing.Color.Black);
                        range.SetOutlineBorder(BorderType.LeftBorder, CellBorderType.Medium, System.Drawing.Color.Black);
                        if (max == idxxx)
                        {
                            range.SetOutlineBorder(BorderType.BottomBorder, CellBorderType.Medium, System.Drawing.Color.Black);
                        }

                        st = st + 3;
                    }

                    //ws.Cells["D8"].Value = wau.Project;
                    //ws.Cells["D9"].Value = wau.Site;
                    //ws.Cells["D10"].Value = wau.WellName;
                    //ws.Cells["D11"].Value = wau.WellType;
                    //ws.Cells["D12"].Value = wau.GetWellActivity().ActivityType; //wau.EventType;
                    //ws.Cells["D13"].Value = wau.Objective;
                    //ws.Cells["D14"].Value = a.PhSchedule.Start;
                    //ws.Cells["I7"].Value = wau.Contractor;
                    //ws.Cells["I8"].Value = wau.WorkUnit;
                    //ws.Cells["I9"].Value = wau.RigSuperintendent;
                    //ws.Cells["I10"].Value = wau.WellEngineer;
                    //ws.Cells["I11"].Value = wau.OriginalSpudDate;
                    //ws.Cells["I12"].Value = wau.OriginalSpudDate > Tools.DefaultDate ? (wau.UpdateVersion - wau.OriginalSpudDate).TotalDays : 0;
                    //ws.Cells["I13"].Value = wau.Actual.Days;
                    //ws.Cells["I14"].Value = wau.OP.Days;


                    //ws.Cells["B3"].Value = String.Format("WELL {0}", wau.WellName);
                    //ws.Cells["B4"].Value = String.Format("Date: {0:dd-MMMM-yyyy}", wau.UpdateVersion);


                    ////ws.Cells["B17"].Value = wau.ExecutiveSummary == null ? "" : wau.ExecutiveSummary.Replace("\n", " ");
                    ////ws.Cells["B26"].Value = wau.OperationSummary == null ? "" : wau.OperationSummary.Replace("\n", " "); // Last 7 Day Summary 
                    ////ws.Cells["B42"].Value = wau.PlannedOperation == null ? "" : wau.PlannedOperation.Replace("\n", " "); // Planned Summary

                    //ws.Cells["B17"].Value = wau.ExecutiveSummary;
                    //ws.Cells["B19"].Value = wau.OperationSummary; // Last 7 Day Summary 
                    //ws.Cells["B21"].Value = wau.PlannedOperation; // Planned Summary
                    //ws.Cells["B23"].Value = wau.SupplementReason; // Supplement Reason

                    //if (!wau.SupplementLast7Days)
                    //{
                    //    ws.Cells.HideRow(21);
                    //    ws.Cells.HideRow(22);
                    //}

                    //ws.Cells["C26"].Value = Tools.Div(wau.AFE.Cost, 1000000);
                    //ws.Cells["H26"].Value = wau.AFE.Days;
                    //ws.Cells["C27"].Value = Tools.Div(wau.Actual.Cost, 1000000);
                    //ws.Cells["H27"].Value = wau.Actual.Days;
                    //ws.Cells["C28"].Value = Tools.Div(wau.CurrentWeek.Cost, 1000000);
                    //ws.Cells["H28"].Value = wau.CurrentWeek.Days;
                    //// OP
                    //ws.Cells["C29"].Value = Tools.Div(wau.Plan.Cost, 1000000);
                    //ws.Cells["H29"].Value = wau.Plan.Days;

                    //// NPT
                    //double npts = 0;
                    //if (wau.Actual.Days > 0)
                    //    npts = wau.NPT.Days * wau.Actual.Days * 24;
                    //else
                    //    npts = 0;

                    //// NPT Hours
                    //ws.Cells["C30"].Value = Math.Round(npts, 1); // +(" (hours)");

                    //// % NPT
                    //ws.Cells["H30"].Value = Math.Round((wau.NPT.Days * 100), 1); // +(" %");

                    #region remark
                    //var idxPip = 40;
                    //foreach (var pip in wau.Elements)
                    //{
                    //    ws.Cells["B" + idxPip.ToString()].Value = pip.Classification;
                    //    ws.Cells["C" + idxPip.ToString()].Value = pip.Title;
                    //    ws.Cells["F" + idxPip.ToString()].Value = pip.DaysPlanImprovement < 0 ? "Opp" : "Risk";
                    //    ws.Cells["G" + idxPip.ToString()].Value = pip.CostPlanImprovement + pip.CostPlanRisk;
                    //    ws.Cells["H" + idxPip.ToString()].Value = pip.CostLastWeekImprovement + pip.CostLastWeekRisk;
                    //    ws.Cells["I" + idxPip.ToString()].Value = pip.CostCurrentWeekImprovement + pip.CostCurrentWeekRisk;
                    //    ws.Cells["J" + idxPip.ToString()].Value = pip.DaysPlanImprovement + pip.DaysPlanRisk;
                    //    ws.Cells["K" + idxPip.ToString()].Value = pip.DaysLastWeekImprovement + pip.DaysLastWeekRisk;
                    //    ws.Cells["L" + idxPip.ToString()].Value = pip.DaysCurrentWeekImprovement + pip.DaysCurrentWeekRisk;
                    //    idxPip += 1;
                    //}
                    #endregion
                    workbooks.Add(wb);

                    //Instantiate LoadOptions specified by the LoadFormat.
                    //LoadOptions loadOptions = new LoadOptions(LoadFormat.Auto);

                    //Create a Workbook object and opening the file from its path
                    //Workbook wb2 = new Workbook(Path.Combine(Server.MapPath("~/App_Data/Temp"), fileName));

                    //wb.Save(newFileName, Aspose.Cells.SaveFormat.Pdf);
                }

                sheet++;
            }


            int idxSheet = 1;
            foreach (Workbook w in workbooks.Skip(1))
            {
                idxSheet++;
                var docWellName = w.Worksheets[0].Cells["D10"].Value;
                var dateString = w.Worksheets[0].Cells["B4"].Value;
                w.Worksheets[0].Name = "Sheet" + idxSheet.ToString();
                workbooks[0].Combine(w);
                //wbEmpty.Worksheets.Add(w.Worksheets[0].Name + "-" +y.ToString() );
            }
            var firstFileName = "";
            var fn1 = Path.Combine(Server.MapPath("~/App_Data/Temp"), fileName1);
            var fn2 = Path.Combine(Server.MapPath("~/App_Data/Temp"), fileName2);
            if (workbooks.Count > 1)
            {
                workbooks[0].Save(newFileName, Aspose.Cells.SaveFormat.Pdf);
                //combine PDF
                // Open the first document
                Document pdfDocument1 = new Document(newFileName);
                // Open the second document
                Document pdfDocument2 = new Document(fn1);
                Document pdfDocument3 = new Document(fn2);
                pdfDocument1.Pages.Add(pdfDocument2.Pages);
                pdfDocument1.Pages.Add(pdfDocument3.Pages);
                pdfDocument1.Save(newFileName);

                return newFileName;
            }
            else
            {
                workbooks[0].Save(newFileNameSingle, Aspose.Cells.SaveFormat.Pdf);
                //combine PDF
                // Open the first document
                Document pdfDocument1 = new Document(newFileNameSingle);
                // Open the second document
                Document pdfDocument2 = new Document(fn1);
                Document pdfDocument3 = new Document(fn2);

                var pageToDelete = 0;
                for (int i = 1; i <= pdfDocument1.Pages.Count; i++)
                {
                    //remove blank page
                    TextAbsorber textAbsorber = new TextAbsorber();
                    //accept the absorber for all the pages
                    pdfDocument1.Pages[i].Accept(textAbsorber);
                    //get the extracted text
                    string extractedText = textAbsorber.Text;
                    if (extractedText.Trim() == "")
                    {
                        pageToDelete = i;
                    }
                }
                if (pageToDelete > 0)
                {
                    pdfDocument1.Pages.Delete(pageToDelete);
                }

                pdfDocument1.Pages.Add(pdfDocument2.Pages);
                pdfDocument1.Pages.Add(pdfDocument3.Pages);
                pdfDocument1.Save(newFileNameSingle);

                try
                {
                    System.IO.File.Delete(fn1);
                    System.IO.File.Delete(fn2);
                }
                catch (Exception e)
                {
                    throw;
                }

                return newFileNameSingle;
            }

            //wbEmpty.Save(newFileName, Aspose.Cells.SaveFormat.Pdf);
        }


        public JsonResult SelectActivityByWellName(string WellName)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                WellActivity upd = new WellActivity();
                var a = Query.EQ("WellName", WellName);
                WellActivity ret = WellActivity.Get<WellActivity>(a);
                return ret;
            });
        }
        public JsonResult SelectPIP(string WellName)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                WellPIP upd = new WellPIP();
                var a = Query.EQ("WellName", WellName);
                WellPIP ret = WellPIP.Get<WellPIP>(a);
                return ret;
            });
        }

        [HttpPost]
        public JsonResult UploadSupportingDocuments()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                string Title = Convert.ToString((object)Request["Title"]);
                string Description = Convert.ToString((object)Request["Description"]);
                string ActivityId = Convert.ToString((object)Request["ActivityId"]);
                string Type = Convert.ToString((object)Request["Type"]);
                string Link = Convert.ToString((object)Request["Link"]);


                string fileName = "";
                string ContentType = "";
                double FileSize = 0;
                if (Type == "File")
                {

                    //type : file

                    HttpPostedFileBase file = Request.Files["fileUpload"];
                    int fileSize = file.ContentLength;
                    fileName = file.FileName;
                    ContentType = file.ContentType;
                    FileSize = file.ContentLength;
                    string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["DocumentPath"], ActivityId);
                    bool exists = System.IO.Directory.Exists(folder);

                    if (!exists)
                        System.IO.Directory.CreateDirectory(folder);

                    string filepath = System.IO.Path.Combine(folder, fileName);
                    file.SaveAs(filepath);

                }
                else
                {
                    //type : link
                }

                WellActivityDocument wad = new WellActivityDocument();
                var q = Query.EQ("ActivityUpdateId", ActivityId);
                WellActivityDocument dataExist = WellActivityDocument.Get<WellActivityDocument>(q);
                if (dataExist != null)
                {
                    dataExist.ActivityUpdateId = ActivityId;
                    dataExist.Files.Add(new DocumentItem
                    {
                        FileName = fileName,
                        Title = Title,
                        Description = Description,
                        ContentType = ContentType,
                        FileNo = dataExist.Files.Count() == 0 ? 0 : dataExist.Files.Max(x => x.FileNo) + 1,
                        Type = Type,
                        Link = Link,
                        FileSize = FileSize
                    });

                    dataExist.Save();
                }
                else
                {
                    var query = Query.EQ("_id", ActivityId);
                    WellActivityUpdateMonthly DataAct = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(query);

                    WellActivityDocument a = new WellActivityDocument();
                    List<DocumentItem> docItem = new List<DocumentItem>();
                    docItem.Add(new DocumentItem
                    {
                        FileName = fileName,
                        Title = Title,
                        Description = Description,
                        ContentType = ContentType,
                        FileNo = 0,
                        Type = Type,
                        Link = Link,
                        FileSize = FileSize
                    });
                    a.Files = docItem;
                    a.ActivityUpdateId = ActivityId;
                    a.UpdateVersion = DataAct.UpdateVersion;
                    a.WellName = DataAct.WellName;
                    a.ActivityDesc = DataAct.Phase.ActivityDesc;
                    a.ActivityType = DataAct.Phase.ActivityType;
                    a.Save();
                }


            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            //ri.CalcLapseTime();
            return ri.ToJsonResult();

        }

        public JsonResult GetDocuments(string ActivityId)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                WellActivityDocument wad = new WellActivityDocument();
                var q = Query.EQ("ActivityUpdateId", ActivityId);
                WellActivityDocument ret = WellActivityDocument.Get<WellActivityDocument>(q);
                //if (ret == null) ret = new WellActivityDocument();
                //ret.Files = new List<DocumentItem>();
                return ret;
            });
        }
        public JsonResult DeleteDocument(string ActivityUpdateId, int FileNo)
        {
            try
            {
                WellActivityDocument wad = new WellActivityDocument();
                List<DocumentItem> docItem = new List<DocumentItem>();
                DocumentItem DeletedDoc = new DocumentItem();

                var q = Query.EQ("ActivityUpdateId", ActivityUpdateId);
                WellActivityDocument data = WellActivityDocument.Get<WellActivityDocument>(q);

                wad._id = data._id;
                wad.ActivityUpdateId = data.ActivityUpdateId;
                foreach (var x in data.Files)
                {
                    if (FileNo != x.FileNo)
                    {
                        docItem.Add(x);
                    }
                    else
                    {
                        DeletedDoc = x;
                    }
                }

                string Folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["DocumentPath"], ActivityUpdateId);
                string filepath = System.IO.Path.Combine(Folder, DeletedDoc.FileName);
                //File.Delete(filepath);
                System.IO.File.Delete(filepath);

                wad.Files = docItem;
                wad.Save();



                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }

        }

        public ActionResult DownloadDocument(string ActivityUpdateId, int FileNo)
        {
            WellActivityDocument wad = new WellActivityDocument();
            DocumentItem docItem = new DocumentItem();

            var q = Query.EQ("ActivityUpdateId", ActivityUpdateId);
            WellActivityDocument data = WellActivityDocument.Get<WellActivityDocument>(q);

            docItem = data.Files.FirstOrDefault(x => x.FileNo.Equals(FileNo));
            string FileName = docItem.FileName;
            string ContentType = docItem.ContentType;

            string Folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["DocumentPath"], ActivityUpdateId);
            string filepath = System.IO.Path.Combine(Folder, FileName);

            if (System.IO.File.Exists(filepath))
            {
                Response.AddHeader("Content-Disposition", "attachment;filename=" + FileName);
                Response.WriteFile(filepath);
                Response.End();
                return null;
            }
            else
            {
                return View();
            }


            //return File(filepath, ContentType);
        }

        public JsonResult Reopen(string id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                var wa = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(id);
                var temp = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(id);
                if (wa != null)
                {
                    wa.Status = "In-Progress";
                    wa.Save();

                    string url = (HttpContext.Request).Path.ToString();
                    WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                        LogType.UpdateOnReopen, temp.TableName, url, temp.ToBsonDocument(), wa.ToBsonDocument());

                }
                return wa;
            });
        }

        public JsonResult GetDataAllocation(string id, int ElementId)
        {
            try
            {

                var query = Query.EQ("_id", id);
                WellActivityUpdateMonthly wp = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(query);
                PIPElement Elements = new PIPElement();

                if (wp.Elements.Count > 0)
                {
                    Elements = wp.Elements.Where(x => x.ElementId == ElementId).FirstOrDefault();
                }

                if (Elements == null)
                {
                    List<IMongoQuery> qs = new List<IMongoQuery>();
                    IMongoQuery q = Query.Null;
                    qs.Add(Query.EQ("WellName", wp.WellName));
                    qs.Add(Query.EQ("SequenceId", wp.SequenceId));
                    qs.Add(Query.EQ("ActivityType", wp.Phase.ActivityType));
                    q = Query.And(qs);
                    WellPIP pip = WellPIP.Get<WellPIP>(q);
                    Elements = pip.Elements.Where(x => x.ElementId == ElementId).FirstOrDefault();
                }

                if (Elements == null)
                {
                    Elements = new PIPElement();
                    Elements.Allocations = new List<PIPAllocation>();
                }

                if (Elements.Allocations == null) Elements.ResetAllocation();
                //var mthNumber = Elements.Allocations.Count();

                var dt = Elements.Period.Start;
                int mthNumber = 0;
                while (dt < Elements.Period.Finish)
                {
                    mthNumber++;
                    dt = dt.AddMonths(1);
                }

                return Json(new { Success = true, Data = Elements.Allocations.ToArray(), monthDiff = mthNumber }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult SaveAllocation(string id, int ElementId, List<PIPAllocation> Allocation)
        {
            try
            {

                var query = Query.EQ("_id", id);
                WellActivityUpdateMonthly wau = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(query);

                //WellPIP wp_update = new WellPIP();


                var Elements = wau.Elements.Where(x => x.ElementId == ElementId).FirstOrDefault();
                List<PIPAllocation> ListAllocation = new List<PIPAllocation>();

                int y = 1;
                foreach (var t in Allocation)
                {
                    ListAllocation.Add(new PIPAllocation
                    {
                        AllocationID = y,
                        //CostPlanImprovement = Math.Round(t.CostPlanImprovement, 2),
                        //CostPlanRisk = Math.Round(t.CostPlanRisk, 2),
                        //DaysPlanImprovement = Math.Round(t.DaysPlanImprovement, 2),
                        //Period = t.Period,
                        //DaysPlanRisk = Math.Round(t.DaysPlanRisk, 2),
                        //LEDays = Math.Round(t.LEDays, 2),
                        //LECost = Math.Round(t.LECost, 2)
                        CostPlanImprovement = t.CostPlanImprovement,
                        CostPlanRisk = t.CostPlanRisk,
                        DaysPlanImprovement = t.DaysPlanImprovement,
                        Period = t.Period,
                        DaysPlanRisk = t.DaysPlanRisk,
                        LEDays = t.LEDays,
                        LECost = t.LECost
                    });
                    y++;
                }

                Elements.Allocations = ListAllocation;

                wau.Save();

                //var dataSave = wau.ToBsonDocument();

                //DataHelper.Save(wau.TableName, dataSave);

                ////save allocation to activity update
                //string WellName = wau.WellName;
                //string ActivityType = wau.Phase.ActivityType;
                //string SequenceId = wau.SequenceId;





                //WellPIP NewWp = new WellPIP();
                //List<PIPAllocation> NewAllocation = new List<PIPAllocation>();
                //PIPElement NewElement = new PIPElement();
                //List<PIPElement> NewListElement = new List<PIPElement>();

                //List<IMongoQuery> qs = new List<IMongoQuery>();
                //IMongoQuery q = Query.Null;
                //qs.Add(Query.EQ("WellName", WellName));
                //qs.Add(Query.EQ("SequenceId", SequenceId));
                //qs.Add(Query.EQ("ActivityType", ActivityType));
                //q = Query.And(qs);

                //var wp = WellPIP.Populate<WellPIP>(q);
                //foreach (var x in wp)
                //{
                //    foreach (var a in x.Elements)
                //    {
                //        //find match element id
                //        if (a.ElementId == ElementId)
                //        {
                //            int b = 1;
                //            foreach (var t in Allocation)
                //            {
                //                NewAllocation.Add(new PIPAllocation
                //                {
                //                    AllocationID = b,
                //                    CostPlanImprovement = t.CostPlanImprovement,
                //                    CostPlanRisk = t.CostPlanRisk,
                //                    DaysPlanImprovement = t.DaysPlanImprovement,
                //                    Period = t.Period,
                //                    DaysPlanRisk = t.DaysPlanRisk,
                //                    LEDays = t.LEDays,
                //                    LECost = t.LECost
                //                });
                //                b++;
                //            }
                //        }
                //        else
                //        {
                //            NewAllocation = a.Allocations;
                //        }
                //        NewElement = a;
                //        NewElement.Allocations = NewAllocation;
                //        NewListElement.Add(NewElement);
                //    }
                //    NewWp = x;
                //    NewWp.Elements = NewListElement;
                //    DataHelper.Save(NewWp.TableName, NewWp.ToBsonDocument());
                //}

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);



                //var result = new ResultInfo();
                //result.Data = new
                //{
                //    Data = updated.ToArray(),
                //    Origin = wa
                //};
                //return MvcTools.ToJsonResult(result.Data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        //private bool _isLatest(string WellName, string ActivityType, string SequenceId, DateTime UpdateVersion)
        //{
        //    var qs = new List<IMongoQuery>();
        //    qs.Add(Query.EQ("WellName", WellName));
        //    qs.Add(Query.EQ("Phase.ActivityType", Phase.ActivityType));
        //    qs.Add(Query.EQ("SequenceId", SequenceId));
        //    qs.Add(Query.LT("UpdateVersion", UpdateVersion));
        //    var latest = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(Query.And(qs), SortBy.Descending("UpdateVersion"));
        //    return latest;
        //}

        public JsonResult Delete(string id)
        {
            try
            {
                var wau = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(id);
                if (wau.Status != "In-Progress")
                {
                    throw new Exception("Data which the status is not In-Progress cannot be deleted!");
                }
                else
                {
                    wau.Delete();
                    string url = (HttpContext.Request).Path.ToString();
                    WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email, LogType.Delete,
                        wau.TableName, url, wau.ToBsonDocument(), null);


                }

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult ResetSingleAllocation(string Id, int ElementId)
        {
            return MvcResultInfo.Execute(() =>
            {
                #region reset single
                //string WellName = "PRINCESS P8";
                //string SequenceId = "3062";
                //string ActivityType = "WHOLE COMPLETION EVENT";
                //int ElementId = 3;

                List<IMongoQuery> qs = new List<IMongoQuery>();
                IMongoQuery q = Query.EQ("_id", Id);


                //qs.Add(Query.EQ("WellName", WellName));
                //qs.Add(Query.EQ("ActivityType", ActivityType));
                //qs.Add(Query.EQ("SequenceId", SequenceId));
                //q = Query.And(qs);

                List<PIPElement> ListElement = new List<PIPElement>();
                PIPElement Element = new PIPElement();
                List<PIPAllocation> Allocation = new List<PIPAllocation>();
                WellActivityUpdateMonthly NewWAU = new WellActivityUpdateMonthly();

                WellActivityUpdateMonthly wau = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(q);
                NewWAU = wau;
                foreach (var x in wau.Elements)
                {
                    Element = x;
                    if (Element.ElementId == ElementId)
                    {
                        Element.Allocations = null;
                    }
                    ListElement.Add(Element);
                }
                NewWAU.Elements = ListElement;
                NewWAU.Save();

                #endregion
                return "OK";
            });
        }

        public JsonResult GetComment(string ActivityUpdateId, string ElementID, bool isRead = false)
        {
            var au = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(Query.EQ("_id", ActivityUpdateId));
            List<object> WAUIds = new List<object>();
            if (au != null)
            {
                var qGetWAUs = Query.And(new IMongoQuery[] { 
                    Query.EQ("WellName", au.WellName),
                    Query.EQ("SequenceId", au.SequenceId),
                    Query.EQ("Phase.ActivityType", au.Phase.ActivityType),
                }.ToList());
                var ListWAUs = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(qGetWAUs);
                if (ListWAUs != null && ListWAUs.Count > 0)
                {
                    foreach (var wau in ListWAUs)
                    {
                        WAUIds.Add(wau._id);
                    }
                }
            }

            var query = Query.And(new IMongoQuery[] { 
                Query.EQ("ReferenceType", "WeeklyReport"),
                Query.In("Reference1", new BsonArray(WAUIds)),
                Query.EQ("Reference2", ElementID),
            }.ToList());
            var data = WEISComment.Populate<WEISComment>(query);


            if (au != null)
            {
                var wellName = au.WellName;
                var qpip = Query.And(Query.EQ("WellName", wellName), Query.EQ("ActivityType", au.Phase.ActivityType));
                var sort = new SortByBuilder().Descending("LastUpdate");
                var pip = WellPIP.Populate<WellPIP>(q: qpip, sort: sort);
                if (pip == null) pip = new List<WellPIP>();

                if (pip.Count > 0)
                {
                    var pipids = pip.Select(d => d._id.ToString());
                    var qau = new List<IMongoQuery>();
                    qau.Add(Query.EQ("ReferenceType", "WellPIP"));
                    qau.Add(Query.In("Reference1", new BsonArray(pipids)));
                    qau.Add(Query.EQ("Reference2", ElementID.ToString()));
                    var qaus = Query.And(qau);
                    var ac = WEISComment.Populate<WEISComment>(Query.And(qau));

                    if (ac != null)
                    {
                        if (ac.Count > 0)
                        {
                            data = data.Concat(ac).ToList();
                        }
                    }
                }
            }

            if (isRead)
            {
                new CommentController().setReadComment(data);
            }

            return Json(new { Data = data.OrderByDescending(x => x.LastUpdate) }, JsonRequestBehavior.AllowGet);
        }

        public List<WEISComment> RenderComment(string ActivityUpdateId, string ElementID, bool isRead = false)
        {
            var au = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(Query.EQ("_id", ActivityUpdateId));
            List<object> WAUIds = new List<object>();
            if (au != null)
            {
                var qGetWAUs = Query.And(new IMongoQuery[] { 
                    Query.EQ("WellName", au.WellName),
                    Query.EQ("SequenceId", au.SequenceId),
                    Query.EQ("Phase.ActivityType", au.Phase.ActivityType),
                }.ToList());
                var ListWAUs = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(qGetWAUs);
                if (ListWAUs != null && ListWAUs.Count > 0)
                {
                    foreach (var wau in ListWAUs)
                    {
                        WAUIds.Add(wau._id);
                    }
                }
            }

            var query = Query.And(new IMongoQuery[] { 
                Query.EQ("ReferenceType", "WeeklyReport"),
                Query.In("Reference1", new BsonArray(WAUIds)),
                Query.EQ("Reference2", ElementID),
            }.ToList());
            var data = WEISComment.Populate<WEISComment>(query);


            if (au != null)
            {
                var wellName = au.WellName;
                var qpip = Query.And(Query.EQ("WellName", wellName), Query.EQ("ActivityType", au.Phase.ActivityType));
                var sort = new SortByBuilder().Descending("LastUpdate");
                var pip = WellPIP.Populate<WellPIP>(q: qpip, sort: sort);
                if (pip == null) pip = new List<WellPIP>();

                if (pip.Count > 0)
                {
                    var pipids = pip.Select(d => d._id.ToString());
                    var qau = new List<IMongoQuery>();
                    qau.Add(Query.EQ("ReferenceType", "WellPIP"));
                    qau.Add(Query.In("Reference1", new BsonArray(pipids)));
                    qau.Add(Query.EQ("Reference2", ElementID.ToString()));
                    var qaus = Query.And(qau);
                    var ac = WEISComment.Populate<WEISComment>(Query.And(qau));

                    if (ac != null)
                    {
                        if (ac.Count > 0)
                        {
                            data = data.Concat(ac).ToList();
                        }
                    }
                }
            }

            if (isRead)
            {
                new CommentController().setReadComment(data);
            }

            return data;
        }
        public JsonResult SaveComment(string ActivityUpdateId, int ElementId, int ParentId, string Comment)
        {
            try
            {
                string User = WebTools.LoginUser.UserName;
                string Email = WebTools.LoginUser.Email;
                string FullName = WebTools.LoginUser.UserName;
                string ReferenceType = "WeeklyReport";
                string Reference1 = ActivityUpdateId;
                string Reference2 = ElementId.ToString();


                WEISComment wc = new WEISComment();
                wc.User = User;
                wc.Email = Email;
                wc.FullName = FullName;
                wc.ReferenceType = ReferenceType;
                wc.Reference1 = Reference1;
                wc.Reference2 = Reference2;
                wc.Comment = Comment;
                wc.ParentId = ParentId;

                wc.Save();
                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email, LogType.Insert,
                    wc.TableName, url, wc.ToBsonDocument(), null);

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }


        public JsonResult GetSummary(string PIPId = "", string BaseOP = "OP15")
        {
            var query = Query.EQ("_id", PIPId);
            var wp = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(query);
            var El = wp.SelectMany(d => d.Elements, (d, e) => new
            {
                AssignedToOP = e.AssignTOOps,
                Completion = e.Completion,
                DaysPlanImprovement = e.DaysPlanImprovement,
                DaysPlanRisk = e.DaysPlanRisk,
                CostPlanImprovement = e.CostPlanImprovement,
                CostPlanRisk = e.CostPlanRisk,
                CostCurrentWeekImprovement = e.CostCurrentWeekImprovement,
                CostCurrentWeekRisk = e.CostCurrentWeekRisk,
                DaysCurrentWeekImprovement = e.DaysCurrentWeekImprovement,
                DaysCurrentWeekRisk = e.DaysCurrentWeekRisk

            });

            var CREl = wp.SelectMany(x => x.CRElements, (x, e) => new
            {
                AssignedToOP = e.AssignTOOps,
                Completion = e.Completion,
                DaysPlanImprovement = e.DaysPlanImprovement,
                DaysPlanRisk = e.DaysPlanRisk,
                CostPlanImprovement = e.CostPlanImprovement,
                CostPlanRisk = e.CostPlanRisk,
                CostCurrentWeekImprovement = e.CostCurrentWeekImprovement,
                CostCurrentWeekRisk = e.CostCurrentWeekRisk,
                DaysCurrentWeekImprovement = e.DaysCurrentWeekImprovement,
                DaysCurrentWeekRisk = e.DaysCurrentWeekRisk
            });
            int i = 0; int j = 0;
            List<BsonDocument> bslist = new List<BsonDocument>();
            double DaysPlanImprovement_Real = 0; double DaysPlanImprovement_NotReal = 0; double DaysPlanImprovement_Total = 0;
            double DaysPlanRisk_Real = 0; double DaysPlanRisk_NotReal = 0; double DaysPlanRisk_Total = 0;
            double CostPlanImprovement_Real = 0; double CostPlanImprovement_NotReal = 0; double CostPlanImprovement_Total = 0;
            double CostPlanRisk_Real = 0; double CostPlanRisk_NotReal = 0; double CostPlanRisk_Total = 0;
            double CostCurrentWeekImprovement_Real = 0; double CostCurrentWeekImprovement_NotReal = 0; double
                CostCurrentWeekImprovement_Total = 0;
            double CostCurrentWeekRisk_Real = 0; double CostCurrentWeekRisk_NotReal = 0; double CostCurrentWeekRisk_Total = 0;
            double DaysCurrentWeekImprovement_Real = 0; double DaysCurrentWeekImprovement_NotReal = 0; double DaysCurrentWeekImprovement_Total = 0;
            double DaysCurrentWeekRisk_Real = 0; double DaysCurrentWeekRisk_NotReal = 0; double DaysCurrentWeekRisk_Total = 0;
            if (El.Any())
            {
                foreach (var r in El.Where(x => x.AssignedToOP.Contains(BaseOP)))
                {
                    if (r.Completion.Equals("Realized"))
                    {
                        DaysPlanImprovement_Real += r.DaysPlanImprovement;
                        DaysPlanRisk_Real += r.DaysPlanRisk;
                        CostPlanImprovement_Real += r.CostPlanImprovement;
                        CostPlanRisk_Real += r.CostPlanRisk;
                        CostCurrentWeekImprovement_Real += r.CostCurrentWeekImprovement;
                        CostCurrentWeekRisk_Real += r.CostCurrentWeekRisk;
                        DaysCurrentWeekImprovement_Real += r.DaysCurrentWeekImprovement;
                        DaysCurrentWeekRisk_Real += r.DaysCurrentWeekRisk;
                    }
                    else
                    {
                        DaysPlanImprovement_NotReal += r.DaysPlanImprovement;
                        DaysPlanRisk_NotReal += r.DaysPlanRisk;
                        CostPlanImprovement_NotReal += r.CostPlanImprovement;
                        CostPlanRisk_NotReal += r.CostPlanRisk;
                        CostCurrentWeekImprovement_NotReal += r.CostCurrentWeekImprovement;
                        CostCurrentWeekRisk_NotReal += r.CostCurrentWeekRisk;
                        DaysCurrentWeekImprovement_NotReal += r.DaysCurrentWeekImprovement;
                        DaysCurrentWeekRisk_NotReal += r.DaysCurrentWeekRisk;
                    }
                    i++;
                }
            }

            DaysPlanImprovement_Total = DaysPlanImprovement_Real + DaysPlanImprovement_NotReal;
            DaysPlanRisk_Total = DaysPlanRisk_Real + DaysPlanRisk_NotReal;
            CostPlanImprovement_Total = CostPlanImprovement_Real + CostPlanImprovement_NotReal;
            CostPlanRisk_Total = CostPlanRisk_Real + CostPlanRisk_NotReal;
            CostCurrentWeekImprovement_Total = CostCurrentWeekImprovement_Real + CostCurrentWeekImprovement_NotReal;
            CostCurrentWeekRisk_Total = CostCurrentWeekRisk_Real + CostCurrentWeekRisk_NotReal;
            DaysCurrentWeekImprovement_Total = DaysCurrentWeekImprovement_Real + DaysCurrentWeekImprovement_NotReal;
            DaysCurrentWeekRisk_Total = DaysCurrentWeekRisk_Real + DaysCurrentWeekRisk_NotReal;
            for (var x = 0; x < 3; x++)
            {

                if (x == 0)
                {
                    BsonDocument document = new BsonDocument {
                        {"Completion","Realized"},
                        {"Type_PIP","Well/ _Project PIP"},
                        {"DaysPlanImprovement",DaysPlanImprovement_Real},
                        {"DaysPlanRisk",DaysPlanRisk_Real},
                        {"CostPlanImprovement",CostPlanImprovement_Real},
                        {"CostPlanRisk",CostPlanRisk_Real},
                        {"CostCurrentWeekImprovement",CostCurrentWeekImprovement_Real},
                        {"CostCurrentWeekRisk",CostCurrentWeekRisk_Real},
                        {"DaysCurrentWeekImprovement",DaysCurrentWeekImprovement_Real},
                        {"DaysCurrentWeekRisk",DaysCurrentWeekRisk_Real}
                    };
                    bslist.Add(document);
                }
                else if (x == 1)
                {
                    BsonDocument document = new BsonDocument {
                        {"Completion","Not Realized"},
                        {"Type_PIP","Well/ _Project PIP"},
                        {"DaysPlanImprovement",DaysPlanImprovement_NotReal},
                        {"DaysPlanRisk",DaysPlanRisk_NotReal},
                        {"CostPlanImprovement",CostPlanImprovement_NotReal},
                        {"CostPlanRisk",CostPlanRisk_NotReal},
                        {"CostCurrentWeekImprovement",CostCurrentWeekImprovement_NotReal},
                        {"CostCurrentWeekRisk",CostCurrentWeekRisk_NotReal},
                        {"DaysCurrentWeekImprovement",DaysCurrentWeekImprovement_NotReal},
                        {"DaysCurrentWeekRisk",DaysCurrentWeekRisk_NotReal}
                    };
                    bslist.Add(document);
                }
                else
                {
                    BsonDocument document = new BsonDocument {
                        {"Completion","Total"},
                        {"Type_PIP","Well/ _Project PIP"},
                        {"DaysPlanImprovement",DaysPlanImprovement_Total},
                        {"DaysPlanRisk",DaysPlanRisk_Total},
                        {"CostPlanImprovement",CostPlanImprovement_Total},
                        {"CostPlanRisk",CostPlanRisk_Total},
                        {"CostCurrentWeekImprovement",CostCurrentWeekImprovement_Total},
                        {"CostCurrentWeekRisk",CostCurrentWeekRisk_Total},
                        {"DaysCurrentWeekImprovement",DaysCurrentWeekImprovement_Total},
                        {"DaysCurrentWeekRisk",DaysCurrentWeekRisk_Total}
                    };
                    bslist.Add(document);
                }

            }
            //CR Element

            //reset variable to zero
            DaysPlanImprovement_Real = 0; DaysPlanImprovement_NotReal = 0; DaysPlanImprovement_Total = 0;
            DaysPlanRisk_Real = 0; DaysPlanRisk_NotReal = 0; DaysPlanRisk_Total = 0;
            CostPlanImprovement_Real = 0; CostPlanImprovement_NotReal = 0; CostPlanImprovement_Total = 0;
            CostPlanRisk_Real = 0; CostPlanRisk_NotReal = 0; CostPlanRisk_Total = 0;
            CostCurrentWeekImprovement_Real = 0; CostCurrentWeekImprovement_NotReal = 0;
            CostCurrentWeekImprovement_Total = 0;
            CostCurrentWeekRisk_Real = 0; CostCurrentWeekRisk_NotReal = 0; CostCurrentWeekRisk_Total = 0;
            DaysCurrentWeekImprovement_Real = 0; DaysCurrentWeekImprovement_NotReal = 0; DaysCurrentWeekImprovement_Total = 0;
            DaysCurrentWeekRisk_Real = 0; DaysCurrentWeekRisk_NotReal = 0; DaysCurrentWeekRisk_Total = 0;

            if (CREl.Any())
            {
                foreach (var r in CREl.Where(x => x.AssignedToOP.Contains(BaseOP)))
                {
                    if (r.Completion.Equals("Realized"))
                    {
                        DaysPlanImprovement_Real += r.DaysPlanImprovement;
                        DaysPlanRisk_Real += r.DaysPlanRisk;
                        CostPlanImprovement_Real += r.CostPlanImprovement;
                        CostPlanRisk_Real += r.CostPlanRisk;
                        CostCurrentWeekImprovement_Real += r.CostCurrentWeekImprovement;
                        CostCurrentWeekRisk_Real += r.CostCurrentWeekRisk;
                        DaysCurrentWeekImprovement_Real += r.DaysCurrentWeekImprovement;
                        DaysCurrentWeekRisk_Real += r.DaysCurrentWeekRisk;
                    }
                    else
                    {
                        DaysPlanImprovement_NotReal += r.DaysPlanImprovement;
                        DaysPlanRisk_NotReal += r.DaysPlanRisk;
                        CostPlanImprovement_NotReal += r.CostPlanImprovement;
                        CostPlanRisk_NotReal += r.CostPlanRisk;
                        CostCurrentWeekImprovement_NotReal += r.CostCurrentWeekImprovement;
                        CostCurrentWeekRisk_NotReal += r.CostCurrentWeekRisk;
                        DaysCurrentWeekImprovement_NotReal += r.DaysCurrentWeekImprovement;
                        DaysCurrentWeekRisk_NotReal += r.DaysCurrentWeekRisk;
                    }
                    j++;
                }
            }

            DaysPlanImprovement_Total = DaysPlanImprovement_Real + DaysPlanImprovement_NotReal;
            DaysPlanRisk_Total = DaysPlanRisk_Real + DaysPlanRisk_NotReal;
            CostPlanImprovement_Total = CostPlanImprovement_Real + CostPlanImprovement_NotReal;
            CostPlanRisk_Total = CostPlanRisk_Real + CostPlanRisk_NotReal;
            CostCurrentWeekImprovement_Total = CostCurrentWeekImprovement_Real + CostCurrentWeekImprovement_NotReal;
            CostCurrentWeekRisk_Total = CostCurrentWeekRisk_Real + CostCurrentWeekRisk_NotReal;
            DaysCurrentWeekImprovement_Total = DaysCurrentWeekImprovement_Real + DaysCurrentWeekImprovement_NotReal;
            DaysCurrentWeekRisk_Total = DaysCurrentWeekRisk_Real + DaysCurrentWeekRisk_NotReal;

            for (var x = 0; x < 3; x++)
            {

                if (x == 0)
                {
                    BsonDocument document = new BsonDocument {
                        {"Completion","Realized"},
                        {"Type_PIP","Rig/ _General SCM"},
                        {"DaysPlanImprovement",DaysPlanImprovement_Real},
                        {"DaysPlanRisk",DaysPlanRisk_Real},
                        {"CostPlanImprovement",CostPlanImprovement_Real},
                        {"CostPlanRisk",CostPlanRisk_Real},
                        {"CostCurrentWeekImprovement",CostCurrentWeekImprovement_Real},
                        {"CostCurrentWeekRisk",CostCurrentWeekRisk_Real},
                        {"DaysCurrentWeekImprovement",DaysCurrentWeekImprovement_Real},
                        {"DaysCurrentWeekRisk",DaysCurrentWeekRisk_Real}
                    };
                    bslist.Add(document);
                }
                else if (x == 1)
                {
                    BsonDocument document = new BsonDocument {
                        {"Completion","Not Realized"},
                        {"Type_PIP","Rig/ _General SCM"},
                        {"DaysPlanImprovement",DaysPlanImprovement_NotReal},
                        {"DaysPlanRisk",DaysPlanRisk_NotReal},
                        {"CostPlanImprovement",CostPlanImprovement_NotReal},
                        {"CostPlanRisk",CostPlanRisk_NotReal},
                        {"CostCurrentWeekImprovement",CostCurrentWeekImprovement_NotReal},
                        {"CostCurrentWeekRisk",CostCurrentWeekRisk_NotReal},
                        {"DaysCurrentWeekImprovement",DaysCurrentWeekImprovement_NotReal},
                        {"DaysCurrentWeekRisk",DaysCurrentWeekRisk_NotReal}
                    };
                    bslist.Add(document);
                }
                else
                {
                    BsonDocument document = new BsonDocument {
                        {"Completion","Total"},
                        {"Type_PIP","Rig/ _General SCM"},
                        {"DaysPlanImprovement",DaysPlanImprovement_Total},
                        {"DaysPlanRisk",DaysPlanRisk_Total},
                        {"CostPlanImprovement",CostPlanImprovement_Total},
                        {"CostPlanRisk",CostPlanRisk_Total},
                        {"CostCurrentWeekImprovement",CostCurrentWeekImprovement_Total},
                        {"CostCurrentWeekRisk",CostCurrentWeekRisk_Total},
                        {"DaysCurrentWeekImprovement",DaysCurrentWeekImprovement_Total},
                        {"DaysCurrentWeekRisk",DaysCurrentWeekRisk_Total}
                    };
                    bslist.Add(document);
                }

            }

            return Json(new { Success = true, Data = DataHelper.ToDictionaryArray(bslist) }, JsonRequestBehavior.AllowGet);
        }

        #region New Calc Monthl LE Chart

        public Dictionary<int, double> GetWeightEachYear(int year, DateRange range, out int numDays, out DateRange dr, bool isRealized = true)
        {

            Dictionary<int, double> result = new Dictionary<int, double>();
            // between 
            double totalduration = (range.Finish - range.Start).Days + 1;
            //double tdur = (range.Finish - range.Start).TotalDays;
            dr = new DateRange();

            var dt = Tools.ToUTC(range.Start);
            var df = Tools.ToUTC(range.Finish);

            if (isRealized)
            {
                // 2
                if (range.Start.Year == year && range.Finish.Year > year)
                {
                    var startdate = range.Start;
                    var lastdate = new DateTime(range.Start.Year, 12, 31);
                    var diff = (lastdate - startdate).Days + 1;
                    numDays = diff;
                    dr.Start = startdate;
                    dr.Finish = lastdate;
                    var pembagi = Tools.Div(diff, totalduration);
                    result.Add(year, Math.Round(pembagi, 2));
                }
                else
                    // 3
                    if (range.Start.Year < year && range.Finish.Year > year)
                    {
                        var startdate = new DateTime(year, 1, 1);
                        var lastdate = new DateTime(year, 12, 31);
                        var diff = (lastdate - startdate).Days + 1;
                        numDays = diff;
                        dr.Start = startdate;
                        dr.Finish = lastdate;
                        var pembagi = Tools.Div(diff, totalduration);
                        result.Add(year, Math.Round(pembagi, 2));
                    }
                    else
                        // 6
                        if (year == range.Finish.Year && year == range.Start.Year)
                        {
                            var startdate = range.Start;
                            var lastdate = range.Finish;
                            var diff = (lastdate - startdate).Days + 1;
                            numDays = diff;
                            dr.Start = startdate;
                            dr.Finish = lastdate;
                            var pembagi = Tools.Div(diff, totalduration);
                            result.Add(year, Math.Round(pembagi, 2));
                        }
                        else
                            // 4
                            if (range.Finish.Year == year && range.Start.Year != year)
                            {
                                var startdate = new DateTime(year, 1, 1);
                                var lastdate = range.Finish;
                                var diff = (lastdate - startdate).Days + 1;
                                numDays = diff;
                                dr.Start = startdate;
                                dr.Finish = lastdate;
                                var pembagi = Tools.Div(diff, totalduration);
                                result.Add(year, Math.Round(pembagi, 2));
                            }

                            else
                            // 1 dan 5
                            //if (year < range.Start.Year || year > range.Finish.Year)
                            {
                                numDays = 0;
                                dr.Start = Tools.DefaultDate;
                                dr.Finish = Tools.DefaultDate;
                                result.Add(year, 0);
                            }
            }
            else
            {
                numDays = 0;
                dr.Start = Tools.DefaultDate;
                dr.Finish = Tools.DefaultDate;
                result.Add(year, 0);
            }
            return result;
        }

        public List<MonthlyElementHelper> CalcEachYear(WellActivityUpdateMonthly waum, List<PIPElement> datas, List<int> yearsCalc, string Title)
        {
            var res = new Dictionary<int, double>();

            #region PIPs
            List<MonthlyElementHelperDetails> details = new List<MonthlyElementHelperDetails>();
            foreach (var item in datas)
            {
                //bool checkedx = isIncludeLE(waum, item);
                foreach (var y in yearsCalc)
                {
                    int numBDay = 0;
                    DateRange dr = new DateRange();

                    bool isRealized = item.Completion.Equals("Realized") ? true : false;

                    if (isRealized)
                    {

                    }
                    var t = GetWeightEachYear(y, item.Period, out numBDay, out dr, isRealized);
                    string tit = "";
                    if (Title.Equals("Classification"))
                        tit = item.Classification;
                    if (Title.Equals("Theme"))
                        tit = item.Theme;
                    if (Title.Equals("Idea"))
                        tit = item.Title;
                    if (Title.Equals("PerformanceUnit"))
                        tit = item.PerformanceUnit;

                    var val = t.Where(x => x.Key == y).FirstOrDefault().Value;

                    details.Add(new MonthlyElementHelperDetails
                    {
                        Idea = item.Title,
                        Year = y,
                        Classified = Title,
                        Title = tit,
                        Value = val,
                        ItemId = item.ElementId,
                        NumOfDayinThisYear = numBDay,
                        oPeriod = item.Period,
                        Period = dr,
                        Completion = item.Completion,
                        //isIncludeLe  = checkedx,

                        WellName = waum.WellName,
                        RigName = "",
                        ActivityType = waum.Phase.ActivityType,
                        SequenceId = waum.SequenceId,

                        oDaysPlanImprovement = item.DaysPlanImprovement,
                        oDaysPlanRisk = item.DaysPlanRisk,
                        oDaysCurrentWeekImprovement = item.DaysCurrentWeekImprovement,
                        oDaysCurrentWeekRisk = item.DaysCurrentWeekRisk,
                        oCostPlanImprovement = item.CostPlanImprovement,
                        oCostPlanRisk = item.CostPlanRisk,
                        oCostCurrentWeekImprovement = item.CostCurrentWeekImprovement,
                        oCostCurrentWeekRisk = item.CostCurrentWeekRisk,

                        DaysPlanImprovement = val * item.DaysPlanImprovement,
                        DaysPlanRisk = val * item.DaysPlanRisk,
                        DaysCurrentWeekImprovement = val * item.DaysCurrentWeekImprovement,
                        DaysCurrentWeekRisk = val * item.DaysCurrentWeekRisk,
                        CostPlanImprovement = val * item.CostPlanImprovement,
                        CostPlanRisk = val * item.CostPlanRisk,
                        CostCurrentWeekImprovement = val * item.CostCurrentWeekImprovement,
                        CostCurrentWeekRisk = val * item.CostCurrentWeekRisk
                    });
                }

            }

            List<MonthlyElementHelper> rexs = new List<MonthlyElementHelper>();
            foreach (var y in details.GroupBy(x => x.Year))
            {
                foreach (var o in y.GroupBy(x => x.Title))
                {
                    MonthlyElementHelper rex = new MonthlyElementHelper();
                    rex.Classified = o.FirstOrDefault().Classified;
                    rex.Title = o.FirstOrDefault().Title;
                    rex.Year = o.FirstOrDefault().Year;
                    rex.oDaysPlan = o.Where(x => x.Completion.Equals("Realized")).Sum(x => (x.oDaysPlanImprovement + x.oDaysPlanRisk));
                    rex.oDaysCurrentWeek = o.Where(x => x.Completion.Equals("Realized")).Sum(x => (x.oDaysCurrentWeekImprovement + x.oDaysCurrentWeekRisk));
                    rex.oCostPlan = o.Where(x => x.Completion.Equals("Realized")).Sum(x => (x.oCostPlanImprovement + x.oCostPlanRisk));
                    rex.oCostCurrentWeek = o.Where(x => x.Completion.Equals("Realized")).Sum(x => (x.oCostCurrentWeekImprovement + x.oCostCurrentWeekRisk));

                    rex.DaysPlan = o.Where(x => x.Completion.Equals("Realized")).Sum(x => (x.DaysPlanImprovement + x.DaysPlanRisk) * x.Value);
                    rex.DaysCurrentWeek = o.Where(x => x.Completion.Equals("Realized")).Sum(x => (x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk));
                    rex.CostPlan = o.Where(x => x.Completion.Equals("Realized")).Sum(x => (x.CostPlanImprovement + x.CostPlanRisk) * x.Value);
                    rex.CostCurrentWeek = o.Where(x => x.Completion.Equals("Realized")).Sum(x => (x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk));

                    var groupRealized = o.Where(d => d.Completion.Equals("Realized")).DefaultIfEmpty(new MonthlyElementHelperDetails());

                    //rex.Realized = new MonthlyElementBase()
                    //{
                    //    oDaysPlan = groupRealized.Sum(x => (x.oDaysPlanImprovement + x.oDaysPlanRisk)),
                    //    oDaysCurrentWeek = groupRealized.Sum(x => (x.oDaysCurrentWeekImprovement + x.oDaysCurrentWeekRisk)),
                    //    oCostPlan = groupRealized.Sum(x => (x.oCostPlanImprovement + x.oCostPlanRisk)),
                    //    oCostCurrentWeek = groupRealized.Sum(x => (x.oCostCurrentWeekImprovement + x.oCostCurrentWeekRisk)),

                    //    DaysPlan = groupRealized.Sum(x => (x.DaysPlanImprovement + x.DaysPlanRisk) * x.Value),
                    //    DaysCurrentWeek = groupRealized.Sum(x => (x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk)),
                    //    CostPlan = groupRealized.Sum(x => (x.CostPlanImprovement + x.CostPlanRisk) * x.Value),
                    //    CostCurrentWeek = groupRealized.Sum(x => (x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk))
                    //};

                    //var groupUnrealized = o.Where(d => !d.Completion.Equals("Realized")).DefaultIfEmpty(new MonthlyElementHelperDetails());

                    //rex.Unrealized = new MonthlyElementBase()
                    //{
                    //    oDaysPlan = groupUnrealized.Sum(x => (x.oDaysPlanImprovement + x.oDaysPlanRisk)),
                    //    oDaysCurrentWeek = groupUnrealized.Sum(x => (x.oDaysCurrentWeekImprovement + x.oDaysCurrentWeekRisk)),
                    //    oCostPlan = groupUnrealized.Sum(x => (x.oCostPlanImprovement + x.oCostPlanRisk)),
                    //    oCostCurrentWeek = groupUnrealized.Sum(x => (x.oCostCurrentWeekImprovement + x.oCostCurrentWeekRisk)),

                    //    DaysPlan = groupUnrealized.Sum(x => (x.DaysPlanImprovement + x.DaysPlanRisk)),
                    //    DaysCurrentWeek = groupUnrealized.Sum(x => (x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk)),
                    //    CostPlan = groupUnrealized.Sum(x => (x.CostPlanImprovement + x.CostPlanRisk)),
                    //    CostCurrentWeek = groupUnrealized.Sum(x => (x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk))
                    //};

                    rex.Details = o.ToList();
                    rexs.Add(rex);
                }
            }
            #endregion

            var q = Query.And(Query.EQ("UARigSequenceId", waum.SequenceId), Query.EQ("WellName", waum.WellName), Query.EQ("Phases.ActivityType", waum.Phase.ActivityType));
            var was = WellActivity.Get<WellActivity>(q);

            #region OP calc
            List<MonthlyOPHelperDetails> Ops = new List<MonthlyOPHelperDetails>();
            if (was != null)
            {
                var phase = was.Phases.Where(x => x.ActivityType == waum.Phase.ActivityType);
                if (phase != null && phase.Count() > 0)
                {
                    string RigName = DataHelper.Get<WellActivity>("WEISWellActivities", q).RigName;

                    DateRange oprange = phase.FirstOrDefault().PlanSchedule;
                    var op = phase.FirstOrDefault().Plan;

                    foreach (var y in yearsCalc)
                    {
                        MonthlyOPHelperDetails p = new MonthlyOPHelperDetails();

                        int numDays = 0;
                        DateRange dr = new DateRange();
                        Dictionary<int, double> resop = GetWeightEachYear(y, oprange, out numDays, out dr);
                        p.ActivityType = phase.FirstOrDefault().ActivityType;
                        p.NumOfDayinThisYear = numDays;
                        p.OP.Cost = phase.FirstOrDefault().Plan.Cost * resop.FirstOrDefault().Value;
                        p.OP.Days = phase.FirstOrDefault().Plan.Days * resop.FirstOrDefault().Value;
                        p.oOP.Cost = phase.FirstOrDefault().Plan.Cost;
                        p.oOP.Days = phase.FirstOrDefault().Plan.Days;
                        p.PhSchedule = dr;
                        p.oPhSchedule = oprange;
                        p.RigName = RigName;
                        p.SequenceId = was.UARigSequenceId;
                        p.Value = resop.FirstOrDefault().Value;
                        p.Year = resop.FirstOrDefault().Key;
                        Ops.Add(p);
                    }
                }
            }

            //foreach (var rx in rexs)
            //{
            //    rx.OPDetails.AddRange(Ops.Where(x => x.Year == rx.Year).ToList());
            //    rx.OPCost = Ops.Where(x => x.Year == rx.Year).Sum(x => x.OP.Cost);
            //    rx.OPDays = Ops.Where(x => x.Year == rx.Year).Sum(x => x.OP.Days);
            //}
            #endregion

            #region OP With LE Calc

            List<MonthlyOPWithLEHelperDetails> OpwLes = new List<MonthlyOPWithLEHelperDetails>();
            if (was != null)
            {
                var phase = was.Phases.Where(x => x.ActivityType == waum.Phase.ActivityType);
                if (phase != null && phase.Count() > 0)
                {
                    string RigName = DataHelper.Get<WellActivity>("WEISWellActivities", q).RigName;

                    DateRange oprange = phase.FirstOrDefault().PlanSchedule;
                    DateRange lerange = phase.FirstOrDefault().LESchedule;
                    var op = phase.FirstOrDefault().Plan;
                    var le = phase.FirstOrDefault().LE;

                    foreach (var y in yearsCalc)
                    {
                        MonthlyOPWithLEHelperDetails p = new MonthlyOPWithLEHelperDetails();

                        int numDays = 0;
                        DateRange dr = new DateRange();
                        DateRange drle = new DateRange();
                        Dictionary<int, double> resop = GetWeightEachYear(y, oprange, out numDays, out dr);
                        Dictionary<int, double> resle = GetWeightEachYear(y, lerange, out numDays, out drle);
                        p.ActivityType = phase.FirstOrDefault().ActivityType;
                        p.NumOfDayinThisYear = numDays;
                        p.OP.Cost = phase.FirstOrDefault().Plan.Cost * resop.FirstOrDefault().Value;
                        p.OP.Days = phase.FirstOrDefault().OP.Days * resop.FirstOrDefault().Value;

                        p.LE.Cost = phase.FirstOrDefault().LE.Cost * resop.FirstOrDefault().Value;
                        p.LE.Days = phase.FirstOrDefault().LE.Days * resop.FirstOrDefault().Value;

                        p.oOP.Cost = phase.FirstOrDefault().Plan.Cost;
                        p.oOP.Days = phase.FirstOrDefault().Plan.Days;

                        p.oLE.Cost = phase.FirstOrDefault().LE.Cost;
                        p.oLE.Days = phase.FirstOrDefault().LE.Days;

                        p.PhSchedule = dr;
                        p.oPhSchedule = oprange;

                        p.LESchedule = drle;
                        p.oLESchedule = lerange;

                        if (p.oLE.Cost != 0)
                        {
                            p.OPwithLE.Cost = p.OP.Cost;
                        }

                        if (p.oLE.Days != 0)
                        {
                            p.OPwithLE.Days = p.OP.Days;
                        }

                        p.RigName = RigName;
                        p.SequenceId = was.UARigSequenceId;
                        p.Value = resop.FirstOrDefault().Value;
                        p.Year = resop.FirstOrDefault().Key;
                        OpwLes.Add(p);
                    }
                }
            }

            //foreach (var rx in rexs)
            //{
            //    rx.OPWithLeCost = OpwLes.Where(x => x.Year == rx.Year).Sum(x => x.OPwithLE.Cost);
            //    rx.OPWithLeDays = OpwLes.Where(x => x.Year == rx.Year).Sum(x => x.OPwithLE.Days);
            //}

            #endregion

            #region LE Calc

            List<MonthlyLEHelperDetails> Les = new List<MonthlyLEHelperDetails>();
            if (was != null)
            {
                var phase = was.Phases.Where(x => x.ActivityType == waum.Phase.ActivityType);
                if (phase != null && phase.Count() > 0)
                {
                    string RigName = DataHelper.Get<WellActivity>("WEISWellActivities", q).RigName;

                    DateRange lerange = phase.FirstOrDefault().LESchedule;
                    var le = phase.FirstOrDefault().LE;

                    foreach (var y in yearsCalc)
                    {
                        MonthlyLEHelperDetails p = new MonthlyLEHelperDetails();

                        int numDays = 0;
                        DateRange dr = new DateRange();
                        Dictionary<int, double> resop = GetWeightEachYear(y, lerange, out numDays, out dr);
                        p.ActivityType = phase.FirstOrDefault().ActivityType;
                        p.NumOfDayinThisYear = numDays;
                        p.LE.Cost = phase.FirstOrDefault().LE.Cost * resop.FirstOrDefault().Value;
                        p.LE.Days = phase.FirstOrDefault().LE.Days * resop.FirstOrDefault().Value;
                        p.oLE.Cost = phase.FirstOrDefault().LE.Cost;
                        p.oLE.Days = phase.FirstOrDefault().LE.Days;
                        p.LESchedule = dr;
                        p.oLESchedule = lerange;
                        p.RigName = RigName;
                        p.SequenceId = was.UARigSequenceId;
                        p.Value = resop.FirstOrDefault().Value;
                        p.Year = resop.FirstOrDefault().Key;
                        Les.Add(p);
                    }
                }
            }

            //foreach (var rx in rexs)
            //{
            //    rx.LEDetails.AddRange(Les.Where(x => x.Year == rx.Year).ToList());
            //    rx.LECost = Les.Where(x => x.Year == rx.Year).Sum(x => x.LE.Cost);
            //    rx.LEDays = Les.Where(x => x.Year == rx.Year).Sum(x => x.LE.Days);
            //}

            #endregion
            return rexs;
        }



        public MonthlyLEHelper MonthlyChartCalcLE(object datas,
         List<int> yearsCalc,
         string BreakdownBy = "Classification",
         string ShowBy = "Days", string LoadData = "Monthly")
        {

            List<BsonDocument> doscs = new List<BsonDocument>();
            List<WellActivityUpdate> dataWAU = new List<WellActivityUpdate>();
            List<WellActivityUpdateMonthly> dataWAUM = new List<WellActivityUpdateMonthly>();

            if (datas.GetType() == typeof(List<WellActivityUpdate>))
            {
                #region WellActivityUpdate
                dataWAU = (List<WellActivityUpdate>)datas;
                foreach (var p in dataWAU)
                {
                    var r = p.ToBsonDocument();
                    r.Set("ActivityType", BsonHelper.GetString(r, "Phase.ActivityType"));
                    r.Set("PhaseNo", BsonHelper.GetInt32(r, "Phase.PhaseNo"));

                    var actv = WellActivity.Get<WellActivity>(Query.And(
                        Query.EQ("WellName", p.WellName),
                        Query.EQ("UARigSequenceId", p.SequenceId)
                        ));

                    if (actv != null)
                    {

                        var phase = actv.Phases.Where(x => x.ActivityType.Equals(BsonHelper.GetString(r, "ActivityType")) &&
                            x.PhaseNo == BsonHelper.GetInt32(r, "PhaseNo")
                            ).FirstOrDefault();

                        r.Set("RigName", actv.RigName);

                        if (phase != null)
                        {
                            r.Set("WP-LESchedule", phase.LESchedule == null ? new DateRange().ToBsonDocument() : phase.LESchedule.ToBsonDocument());
                            r.Set("WP-LWESchedule", phase.LWESchedule == null ? new DateRange().ToBsonDocument() : phase.LWESchedule.ToBsonDocument());
                            r.Set("WP-PlanSchedule", phase.PlanSchedule == null ? new DateRange().ToBsonDocument() : phase.PlanSchedule.ToBsonDocument());
                            r.Set("WP-PhSchedule", phase.PhSchedule == null ? new DateRange().ToBsonDocument() : phase.PhSchedule.ToBsonDocument());
                            r.Set("WP-AFESchedule", phase.AFESchedule == null ? new DateRange().ToBsonDocument() : phase.AFESchedule.ToBsonDocument());

                            r.Set("WP-TQ", phase.TQ == null ? new WellDrillData().ToBsonDocument() : phase.TQ.ToBsonDocument());
                            r.Set("WP-AFE", phase.AFE == null ? new WellDrillData().ToBsonDocument() : phase.AFE.ToBsonDocument());
                            r.Set("WP-Plan", phase.Plan == null ? new WellDrillData().ToBsonDocument() : phase.Plan.ToBsonDocument());
                            r.Set("WP-Actual", phase.Actual == null ? new WellDrillData().ToBsonDocument() : phase.Actual.ToBsonDocument());
                            r.Set("WP-OP", phase.OP == null ? new WellDrillData().ToBsonDocument() : phase.OP.ToBsonDocument());
                            r.Set("WP-LE", phase.LE == null ? new WellDrillData().ToBsonDocument() : phase.LE.ToBsonDocument());
                            r.Set("WP-LWE", phase.LWE == null ? new WellDrillData().ToBsonDocument() : phase.LWE.ToBsonDocument());
                        }
                        else
                        {
                            r.Set("WP-LESchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-LWESchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-PlanSchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-PhSchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-AFESchedule", new DateRange().ToBsonDocument());

                            r.Set("WP-TQ", new WellDrillData().ToBsonDocument());
                            r.Set("WP-AFE", new WellDrillData().ToBsonDocument());
                            r.Set("WP-Plan", new WellDrillData().ToBsonDocument());
                            r.Set("WP-Actual", new WellDrillData().ToBsonDocument());
                            r.Set("WP-OP", new WellDrillData().ToBsonDocument());
                            r.Set("WP-LE", new WellDrillData().ToBsonDocument());
                            r.Set("WP-LWE", new WellDrillData().ToBsonDocument());
                        }

                        doscs.Add(r);
                    }
                    else
                    {
                        r.Set("RigName", "");


                        r.Set("WP-LESchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-LWESchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-PlanSchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-PhSchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-AFESchedule", new DateRange().ToBsonDocument());

                        r.Set("WP-TQ", new WellDrillData().ToBsonDocument());
                        r.Set("WP-AFE", new WellDrillData().ToBsonDocument());
                        r.Set("WP-Plan", new WellDrillData().ToBsonDocument());
                        r.Set("WP-Actual", new WellDrillData().ToBsonDocument());
                        r.Set("WP-OP", new WellDrillData().ToBsonDocument());
                        r.Set("WP-LE", new WellDrillData().ToBsonDocument());
                        r.Set("WP-LWE", new WellDrillData().ToBsonDocument());
                    }
                }

                #endregion
            }
            else
            {
                #region WellActivityUpdateMonthly
                dataWAUM = (List<WellActivityUpdateMonthly>)datas;
                foreach (var p in dataWAUM.ToList())
                {
                    var r = p.ToBsonDocument();
                    r.Set("ActivityType", BsonHelper.GetString(r, "Phase.ActivityType"));
                    r.Set("PhaseNo", BsonHelper.GetInt32(r, "Phase.PhaseNo"));

                    var actv = WellActivity.Get<WellActivity>(Query.And(
                        Query.EQ("WellName", p.WellName),
                        Query.EQ("UARigSequenceId", p.SequenceId)
                        ));

                    if (actv != null)
                    {

                        var phase = actv.Phases.Where(x => x.ActivityType.Equals(BsonHelper.GetString(r, "ActivityType")) &&
                            x.PhaseNo == BsonHelper.GetInt32(r, "PhaseNo")
                            ).FirstOrDefault();

                        r.Set("RigName", actv.RigName);

                        if (phase != null)
                        {
                            r.Set("WP-LESchedule", phase.LESchedule == null ? new DateRange().ToBsonDocument() : phase.LESchedule.ToBsonDocument());
                            r.Set("WP-LWESchedule", phase.LWESchedule == null ? new DateRange().ToBsonDocument() : phase.LWESchedule.ToBsonDocument());
                            r.Set("WP-PlanSchedule", phase.PlanSchedule == null ? new DateRange().ToBsonDocument() : phase.PlanSchedule.ToBsonDocument());
                            r.Set("WP-PhSchedule", phase.PhSchedule == null ? new DateRange().ToBsonDocument() : phase.PhSchedule.ToBsonDocument());
                            r.Set("WP-AFESchedule", phase.AFESchedule == null ? new DateRange().ToBsonDocument() : phase.AFESchedule.ToBsonDocument());

                            r.Set("WP-TQ", phase.TQ == null ? new WellDrillData().ToBsonDocument() : phase.TQ.ToBsonDocument());
                            r.Set("WP-AFE", phase.AFE == null ? new WellDrillData().ToBsonDocument() : phase.AFE.ToBsonDocument());
                            r.Set("WP-Plan", phase.Plan == null ? new WellDrillData().ToBsonDocument() : phase.Plan.ToBsonDocument());
                            r.Set("WP-Actual", phase.Actual == null ? new WellDrillData().ToBsonDocument() : phase.Actual.ToBsonDocument());
                            r.Set("WP-OP", phase.OP == null ? new WellDrillData().ToBsonDocument() : phase.OP.ToBsonDocument());
                            r.Set("WP-LE", phase.LE == null ? new WellDrillData().ToBsonDocument() : phase.LE.ToBsonDocument());
                            r.Set("WP-LWE", phase.LWE == null ? new WellDrillData().ToBsonDocument() : phase.LWE.ToBsonDocument());
                        }
                        else
                        {
                            r.Set("WP-LESchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-LWESchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-PlanSchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-PhSchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-AFESchedule", new DateRange().ToBsonDocument());

                            r.Set("WP-TQ", new WellDrillData().ToBsonDocument());
                            r.Set("WP-AFE", new WellDrillData().ToBsonDocument());
                            r.Set("WP-Plan", new WellDrillData().ToBsonDocument());
                            r.Set("WP-Actual", new WellDrillData().ToBsonDocument());
                            r.Set("WP-OP", new WellDrillData().ToBsonDocument());
                            r.Set("WP-LE", new WellDrillData().ToBsonDocument());
                            r.Set("WP-LWE", new WellDrillData().ToBsonDocument());
                        }

                        doscs.Add(r);
                    }
                    else
                    {
                        r.Set("RigName", "");

                        r.Set("WP-LESchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-LWESchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-PlanSchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-PhSchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-AFESchedule", new DateRange().ToBsonDocument());

                        r.Set("WP-TQ", new WellDrillData().ToBsonDocument());
                        r.Set("WP-AFE", new WellDrillData().ToBsonDocument());
                        r.Set("WP-Plan", new WellDrillData().ToBsonDocument());
                        r.Set("WP-Actual", new WellDrillData().ToBsonDocument());
                        r.Set("WP-OP", new WellDrillData().ToBsonDocument());
                        r.Set("WP-LE", new WellDrillData().ToBsonDocument());
                        r.Set("WP-LWE", new WellDrillData().ToBsonDocument());
                    }
                }
                #endregion
            }

            #region LE calc
            List<MonthlyLEHelperDetails> LEs = new List<MonthlyLEHelperDetails>();
            //foreach (var xreal in uwindAll.GroupBy(x => BsonHelper.GetString(x, "_id")))
            foreach (var doc in doscs)
            {
                var real = doc;
                foreach (var year in yearsCalc)
                {
                    MonthlyLEHelperDetails p = new MonthlyLEHelperDetails();

                    int numDays = 0;
                    DateRange dr = new DateRange(BsonHelper.GetDateTime(real, "WP-LESchedule.Start"), BsonHelper.GetDateTime(real, "WP-LESchedule.Finish"));
                    DateRange outdate = new DateRange();
                    double res = GetWeightEachYear(year, dr, out numDays, out outdate).FirstOrDefault().Value;
                    p.ActivityType = real.GetString("ActivityType"); // phase.FirstOrDefault().ActivityType;
                    p.NumOfDayinThisYear = numDays;
                    p.LE.Cost = Tools.Div(real.GetDouble("WP-LE.Cost") * res, 1000000); //phase.FirstOrDefault().Plan.Cost * resop.FirstOrDefault().Value;
                    p.LE.Days = real.GetDouble("WP-LE.Days") * res;
                    p.oLE.Cost = Tools.Div(real.GetDouble("WP-LE.Cost"), 1000000); //phase.FirstOrDefault().Plan.Cost * resop.FirstOrDefault().Value;
                    p.oLE.Days = real.GetDouble("WP-LE.Days");

                    p.LESchedule = outdate;
                    p.oLESchedule = dr;
                    p.RigName = real.GetString("RigName");
                    p.SequenceId = real.GetString("SequenceId");
                    p.WellName = real.GetString("WellName");

                    p.Value = res;
                    p.Year = year;
                    LEs.Add(p);
                }

            }

            #endregion


            MonthlyLEHelper rx = new MonthlyLEHelper();
            rx.LEDetails.AddRange(LEs.ToList());
            rx.Days = rx.LEDetails.Sum(x => x.LE.Days);
            rx.Cost = rx.LEDetails.Sum(x => x.LE.Cost);


            return rx;
        }

        public MonthlyOPHelper MonthlyChartCalcOP(object datas,
         List<int> yearsCalc,
         string BreakdownBy = "Classification",
         string ShowBy = "Days", string LoadData = "Monthly")
        {

            List<BsonDocument> doscs = new List<BsonDocument>();
            List<WellActivityUpdate> dataWAU = new List<WellActivityUpdate>();
            List<WellActivityUpdateMonthly> dataWAUM = new List<WellActivityUpdateMonthly>();

            if (datas.GetType() == typeof(List<WellActivityUpdate>))
            {
                #region WellActivityUpdate
                dataWAU = (List<WellActivityUpdate>)datas;
                foreach (var p in dataWAU)
                {
                    var r = p.ToBsonDocument();
                    r.Set("ActivityType", BsonHelper.GetString(r, "Phase.ActivityType"));
                    r.Set("PhaseNo", BsonHelper.GetInt32(r, "Phase.PhaseNo"));

                    var actv = WellActivity.Get<WellActivity>(Query.And(
                        Query.EQ("WellName", p.WellName),
                        Query.EQ("UARigSequenceId", p.SequenceId)
                        ));

                    if (actv != null)
                    {

                        var phase = actv.Phases.Where(x => x.ActivityType.Equals(BsonHelper.GetString(r, "ActivityType")) &&
                            x.PhaseNo == BsonHelper.GetInt32(r, "PhaseNo")
                            ).FirstOrDefault();

                        r.Set("RigName", actv.RigName);

                        if (phase != null)
                        {
                            r.Set("WP-LESchedule", phase.LESchedule == null ? new DateRange().ToBsonDocument() : phase.LESchedule.ToBsonDocument());
                            r.Set("WP-LWESchedule", phase.LWESchedule == null ? new DateRange().ToBsonDocument() : phase.LWESchedule.ToBsonDocument());
                            r.Set("WP-PlanSchedule", phase.PlanSchedule == null ? new DateRange().ToBsonDocument() : phase.PlanSchedule.ToBsonDocument());
                            r.Set("WP-PhSchedule", phase.PhSchedule == null ? new DateRange().ToBsonDocument() : phase.PhSchedule.ToBsonDocument());
                            r.Set("WP-AFESchedule", phase.AFESchedule == null ? new DateRange().ToBsonDocument() : phase.AFESchedule.ToBsonDocument());

                            r.Set("WP-TQ", phase.TQ == null ? new WellDrillData().ToBsonDocument() : phase.TQ.ToBsonDocument());
                            r.Set("WP-AFE", phase.AFE == null ? new WellDrillData().ToBsonDocument() : phase.AFE.ToBsonDocument());
                            r.Set("WP-Plan", phase.Plan == null ? new WellDrillData().ToBsonDocument() : phase.Plan.ToBsonDocument());
                            r.Set("WP-Actual", phase.Actual == null ? new WellDrillData().ToBsonDocument() : phase.Actual.ToBsonDocument());
                            r.Set("WP-OP", phase.OP == null ? new WellDrillData().ToBsonDocument() : phase.OP.ToBsonDocument());
                            r.Set("WP-LE", phase.LE == null ? new WellDrillData().ToBsonDocument() : phase.LE.ToBsonDocument());
                            r.Set("WP-LWE", phase.LWE == null ? new WellDrillData().ToBsonDocument() : phase.LWE.ToBsonDocument());
                        }
                        else
                        {
                            r.Set("WP-LESchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-LWESchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-PlanSchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-PhSchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-AFESchedule", new DateRange().ToBsonDocument());

                            r.Set("WP-TQ", new WellDrillData().ToBsonDocument());
                            r.Set("WP-AFE", new WellDrillData().ToBsonDocument());
                            r.Set("WP-Plan", new WellDrillData().ToBsonDocument());
                            r.Set("WP-Actual", new WellDrillData().ToBsonDocument());
                            r.Set("WP-OP", new WellDrillData().ToBsonDocument());
                            r.Set("WP-LE", new WellDrillData().ToBsonDocument());
                            r.Set("WP-LWE", new WellDrillData().ToBsonDocument());
                        }

                        doscs.Add(r);
                    }
                    else
                    {
                        r.Set("RigName", "");


                        r.Set("WP-LESchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-LWESchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-PlanSchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-PhSchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-AFESchedule", new DateRange().ToBsonDocument());

                        r.Set("WP-TQ", new WellDrillData().ToBsonDocument());
                        r.Set("WP-AFE", new WellDrillData().ToBsonDocument());
                        r.Set("WP-Plan", new WellDrillData().ToBsonDocument());
                        r.Set("WP-Actual", new WellDrillData().ToBsonDocument());
                        r.Set("WP-OP", new WellDrillData().ToBsonDocument());
                        r.Set("WP-LE", new WellDrillData().ToBsonDocument());
                        r.Set("WP-LWE", new WellDrillData().ToBsonDocument());
                    }
                }

                #endregion
            }
            else
            {
                #region WellActivityUpdateMonthly
                dataWAUM = (List<WellActivityUpdateMonthly>)datas;
                foreach (var p in dataWAUM.ToList())
                {
                    var r = p.ToBsonDocument();
                    r.Set("ActivityType", BsonHelper.GetString(r, "Phase.ActivityType"));
                    r.Set("PhaseNo", BsonHelper.GetInt32(r, "Phase.PhaseNo"));

                    var actv = WellActivity.Get<WellActivity>(Query.And(
                        Query.EQ("WellName", p.WellName),
                        Query.EQ("UARigSequenceId", p.SequenceId)
                        ));

                    if (actv != null)
                    {

                        var phase = actv.Phases.Where(x => x.ActivityType.Equals(BsonHelper.GetString(r, "ActivityType")) &&
                            x.PhaseNo == BsonHelper.GetInt32(r, "PhaseNo")
                            ).FirstOrDefault();

                        r.Set("RigName", actv.RigName);

                        if (phase != null)
                        {
                            r.Set("WP-LESchedule", phase.LESchedule == null ? new DateRange().ToBsonDocument() : phase.LESchedule.ToBsonDocument());
                            r.Set("WP-LWESchedule", phase.LWESchedule == null ? new DateRange().ToBsonDocument() : phase.LWESchedule.ToBsonDocument());
                            r.Set("WP-PlanSchedule", phase.PlanSchedule == null ? new DateRange().ToBsonDocument() : phase.PlanSchedule.ToBsonDocument());
                            r.Set("WP-PhSchedule", phase.PhSchedule == null ? new DateRange().ToBsonDocument() : phase.PhSchedule.ToBsonDocument());
                            r.Set("WP-AFESchedule", phase.AFESchedule == null ? new DateRange().ToBsonDocument() : phase.AFESchedule.ToBsonDocument());

                            r.Set("WP-TQ", phase.TQ == null ? new WellDrillData().ToBsonDocument() : phase.TQ.ToBsonDocument());
                            r.Set("WP-AFE", phase.AFE == null ? new WellDrillData().ToBsonDocument() : phase.AFE.ToBsonDocument());
                            r.Set("WP-Plan", phase.Plan == null ? new WellDrillData().ToBsonDocument() : phase.Plan.ToBsonDocument());
                            r.Set("WP-Actual", phase.Actual == null ? new WellDrillData().ToBsonDocument() : phase.Actual.ToBsonDocument());
                            r.Set("WP-OP", phase.OP == null ? new WellDrillData().ToBsonDocument() : phase.OP.ToBsonDocument());
                            r.Set("WP-LE", phase.LE == null ? new WellDrillData().ToBsonDocument() : phase.LE.ToBsonDocument());
                            r.Set("WP-LWE", phase.LWE == null ? new WellDrillData().ToBsonDocument() : phase.LWE.ToBsonDocument());
                        }
                        else
                        {
                            r.Set("WP-LESchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-LWESchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-PlanSchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-PhSchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-AFESchedule", new DateRange().ToBsonDocument());

                            r.Set("WP-TQ", new WellDrillData().ToBsonDocument());
                            r.Set("WP-AFE", new WellDrillData().ToBsonDocument());
                            r.Set("WP-Plan", new WellDrillData().ToBsonDocument());
                            r.Set("WP-Actual", new WellDrillData().ToBsonDocument());
                            r.Set("WP-OP", new WellDrillData().ToBsonDocument());
                            r.Set("WP-LE", new WellDrillData().ToBsonDocument());
                            r.Set("WP-LWE", new WellDrillData().ToBsonDocument());
                        }

                        doscs.Add(r);
                    }
                    else
                    {
                        r.Set("RigName", "");

                        r.Set("WP-LESchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-LWESchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-PlanSchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-PhSchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-AFESchedule", new DateRange().ToBsonDocument());

                        r.Set("WP-TQ", new WellDrillData().ToBsonDocument());
                        r.Set("WP-AFE", new WellDrillData().ToBsonDocument());
                        r.Set("WP-Plan", new WellDrillData().ToBsonDocument());
                        r.Set("WP-Actual", new WellDrillData().ToBsonDocument());
                        r.Set("WP-OP", new WellDrillData().ToBsonDocument());
                        r.Set("WP-LE", new WellDrillData().ToBsonDocument());
                        r.Set("WP-LWE", new WellDrillData().ToBsonDocument());
                    }
                }
                #endregion
            }

            #region OP calc
            List<MonthlyOPHelperDetails> Ops = new List<MonthlyOPHelperDetails>();
            //foreach (var xreal in uwindAll.GroupBy(x => BsonHelper.GetString(x, "_id")))
            foreach (var doc in doscs)
            {
                var real = doc;
                foreach (var year in yearsCalc)
                {
                    MonthlyOPHelperDetails p = new MonthlyOPHelperDetails();

                    int numDays = 0;
                    DateRange dr = new DateRange(BsonHelper.GetDateTime(real, "WP-PlanSchedule.Start"), BsonHelper.GetDateTime(real, "WP-PlanSchedule.Finish"));
                    DateRange outdate = new DateRange();
                    double res = GetWeightEachYear(year, dr, out numDays, out outdate).FirstOrDefault().Value;
                    p.ActivityType = real.GetString("ActivityType"); // phase.FirstOrDefault().ActivityType;
                    p.NumOfDayinThisYear = numDays;
                    p.OP.Cost = Tools.Div(real.GetDouble("WP-Plan.Cost") * res, 1000000); //phase.FirstOrDefault().Plan.Cost * resop.FirstOrDefault().Value;
                    p.OP.Days = real.GetDouble("WP-Plan.Days") * res;
                    p.oOP.Cost = Tools.Div(real.GetDouble("WP-Plan.Cost"), 1000000); //phase.FirstOrDefault().Plan.Cost * resop.FirstOrDefault().Value;
                    p.oOP.Days = real.GetDouble("WP-Plan.Days");

                    p.PhSchedule = outdate;
                    p.oPhSchedule = dr;
                    p.RigName = real.GetString("RigName");
                    p.SequenceId = real.GetString("SequenceId");
                    p.WellName = real.GetString("WellName");

                    p.Value = res;
                    p.Year = year;
                    Ops.Add(p);
                }

            }

            #endregion


            // op
            //rx.OPDetails.AddRange(Ops.Where(x => x.Year == rx.Year).ToList());
            //rx.OPCost = Ops.Where(x => x.Year == rx.Year).Sum(x => x.OP.Cost);
            //rx.OPDays = Ops.Where(x => x.Year == rx.Year).Sum(x => x.OP.Days);

            MonthlyOPHelper rx = new MonthlyOPHelper();
            rx.OPDetails.AddRange(Ops.ToList());
            rx.Days = rx.OPDetails.Sum(x => x.OP.Days);
            rx.Cost = rx.OPDetails.Sum(x => x.OP.Cost);

            // op with LE
            //foreach (var rx in rexs)
            //{
            //    rx.OPWithLeCost = OpwLes.Where(x => x.Year == rx.Year).Sum(x => x.OPwithLE.Cost);
            //    rx.OPWithLeDays = OpwLes.Where(x => x.Year == rx.Year).Sum(x => x.OPwithLE.Days);
            //}

            return rx;
        }


        public MonthlyOPHelper MonthlyChartCalcOPFromWellPlan(List<WellActivity> datas,
         List<int> yearsCalc,
         string BreakdownBy = "Classification",
         string ShowBy = "Cost", string LoadData = "Monthly")
        {

            #region OP calc

            List<MonthlyOPHelperDetails> Ops = new List<MonthlyOPHelperDetails>();

            foreach (var w in datas)
            {
                foreach (var t in w.Phases)
                {
                    foreach (var y in yearsCalc)
                    {
                        MonthlyOPHelperDetails p = new MonthlyOPHelperDetails();

                        int numDays = 0;
                        DateRange dr = t.PlanSchedule; // new DateRange(BsonHelper.GetDateTime(real, "WP-PlanSchedule.Start"), BsonHelper.GetDateTime(real, "WP-PlanSchedule.Finish"));
                        DateRange outdate = new DateRange();
                        double res = GetWeightEachYear(y, dr, out numDays, out outdate).FirstOrDefault().Value;
                        p.ActivityType = t.ActivityType; // phase.FirstOrDefault().ActivityType;
                        p.NumOfDayinThisYear = numDays;
                        p.OP.Cost = Tools.Div((t.OP.Cost * res), 1000000); //phase.FirstOrDefault().Plan.Cost * resop.FirstOrDefault().Value;
                        p.OP.Days = t.OP.Days * res;
                        p.oOP.Cost = Tools.Div((t.OP.Cost), 1000000);//Tools.Div(real.GetDouble("WP-Plan.Cost"), 1000000); //phase.FirstOrDefault().Plan.Cost * resop.FirstOrDefault().Value;
                        p.oOP.Days = t.OP.Days * res;//real.GetDouble("WP-Plan.Days");

                        p.PhSchedule = outdate;
                        p.oPhSchedule = dr;
                        p.RigName = w.RigName;// real.GetString("RigName");
                        p.SequenceId = w.UARigSequenceId;// real.GetString("SequenceId");
                        p.WellName = w.WellName;// real.GetString("WellName");

                        p.Value = res;
                        p.Year = y;
                        Ops.Add(p);
                    }

                    var d = Ops.Sum(x => x.OP.Cost);

                }
            }

            #endregion


            MonthlyOPHelper rx = new MonthlyOPHelper();
            rx.OPDetails.AddRange(Ops.ToList());
            rx.Days = Ops.Sum(x => x.OP.Days); //rx.OPDetails.Sum(x => x.OP.Days);
            rx.Cost = Ops.Sum(x => x.OP.Cost); ;//rx.OPDetails.Sum(x => x.OP.Cost);

            rx.Cost = Tools.Div(rx.Cost, 1000000);

            return rx;
        }

        public MonthlyLEHelper MonthlyChartCalcLEFromWellPlan(List<WellActivity> datas,
     List<int> yearsCalc,
     string BreakdownBy = "Classification",
     string ShowBy = "Cost", string LoadData = "Monthly")
        {

            #region LE calc

            List<MonthlyLEHelperDetails> LEs = new List<MonthlyLEHelperDetails>();

            foreach (var w in datas)
            {
                foreach (var t in w.Phases)
                {
                    foreach (var y in yearsCalc)
                    {
                        MonthlyLEHelperDetails p = new MonthlyLEHelperDetails();

                        int numDays = 0;
                        DateRange dr = t.LESchedule; // new DateRange(BsonHelper.GetDateTime(real, "WP-PlanSchedule.Start"), BsonHelper.GetDateTime(real, "WP-PlanSchedule.Finish"));
                        DateRange outdate = new DateRange();
                        double res = GetWeightEachYear(y, dr, out numDays, out outdate).FirstOrDefault().Value;
                        p.ActivityType = t.ActivityType; // phase.FirstOrDefault().ActivityType;
                        p.NumOfDayinThisYear = numDays;
                        p.LE.Cost = Tools.Div(t.LE.Cost * res, 1000000); //phase.FirstOrDefault().Plan.Cost * resop.FirstOrDefault().Value;
                        p.LE.Days = t.LE.Days * res;
                        p.oLE.Cost = Tools.Div(t.LE.Cost, 1000000);//Tools.Div(real.GetDouble("WP-Plan.Cost"), 1000000); //phase.FirstOrDefault().Plan.Cost * resop.FirstOrDefault().Value;
                        p.oLE.Days = t.LE.Days * res;//real.GetDouble("WP-Plan.Days");

                        p.LESchedule = outdate;
                        p.oLESchedule = dr;
                        p.RigName = w.RigName;// real.GetString("RigName");
                        p.SequenceId = w.UARigSequenceId;// real.GetString("SequenceId");
                        p.WellName = w.WellName;// real.GetString("WellName");

                        p.Value = res;
                        p.Year = y;
                        LEs.Add(p);
                    }

                }
            }

            #endregion


            MonthlyLEHelper rx = new MonthlyLEHelper();
            rx.LEDetails.AddRange(LEs.ToList());
            rx.Days = rx.LEDetails.Sum(x => x.LE.Days);
            rx.Cost = rx.LEDetails.Sum(x => x.LE.Cost);


            return rx;
        }




        public List<MonthlyElementHelper> MonthlyChartCalc(object datas,
         List<int> yearsCalc,
         string BreakdownBy = "Classification",
         string ShowBy = "Days", string LoadData = "Monthly")
        {
            List<BsonDocument> doscs = new List<BsonDocument>();
            List<WellActivityUpdate> dataWAU = new List<WellActivityUpdate>();
            List<WellActivityUpdateMonthly> dataWAUM = new List<WellActivityUpdateMonthly>();

            if (datas.GetType() == typeof(List<WellActivityUpdate>))
            {
                #region WellActivityUpdate
                dataWAU = (List<WellActivityUpdate>)datas;
                foreach (var p in dataWAU)
                {
                    var r = p.ToBsonDocument();
                    r.Set("ActivityType", BsonHelper.GetString(r, "Phase.ActivityType"));
                    r.Set("PhaseNo", BsonHelper.GetInt32(r, "Phase.PhaseNo"));

                    var actv = WellActivity.Get<WellActivity>(Query.And(
                        Query.EQ("WellName", p.WellName),
                        Query.EQ("UARigSequenceId", p.SequenceId)
                        ));

                    if (actv != null)
                    {

                        var phase = actv.Phases.Where(x => x.ActivityType.Equals(BsonHelper.GetString(r, "ActivityType")) &&
                            x.PhaseNo == BsonHelper.GetInt32(r, "PhaseNo")
                            ).FirstOrDefault();

                        r.Set("RigName", actv.RigName);

                        if (phase != null)
                        {
                            r.Set("WP-LESchedule", phase.LESchedule == null ? new DateRange().ToBsonDocument() : phase.LESchedule.ToBsonDocument());
                            r.Set("WP-LWESchedule", phase.LWESchedule == null ? new DateRange().ToBsonDocument() : phase.LWESchedule.ToBsonDocument());
                            r.Set("WP-PlanSchedule", phase.PlanSchedule == null ? new DateRange().ToBsonDocument() : phase.PlanSchedule.ToBsonDocument());
                            r.Set("WP-PhSchedule", phase.PhSchedule == null ? new DateRange().ToBsonDocument() : phase.PhSchedule.ToBsonDocument());
                            r.Set("WP-AFESchedule", phase.AFESchedule == null ? new DateRange().ToBsonDocument() : phase.AFESchedule.ToBsonDocument());

                            r.Set("WP-TQ", phase.TQ == null ? new WellDrillData().ToBsonDocument() : phase.TQ.ToBsonDocument());
                            r.Set("WP-AFE", phase.AFE == null ? new WellDrillData().ToBsonDocument() : phase.AFE.ToBsonDocument());
                            r.Set("WP-Plan", phase.Plan == null ? new WellDrillData().ToBsonDocument() : phase.Plan.ToBsonDocument());
                            r.Set("WP-Actual", phase.Actual == null ? new WellDrillData().ToBsonDocument() : phase.Actual.ToBsonDocument());
                            r.Set("WP-OP", phase.OP == null ? new WellDrillData().ToBsonDocument() : phase.OP.ToBsonDocument());
                            r.Set("WP-LE", phase.LE == null ? new WellDrillData().ToBsonDocument() : phase.LE.ToBsonDocument());
                            r.Set("WP-LWE", phase.LWE == null ? new WellDrillData().ToBsonDocument() : phase.LWE.ToBsonDocument());
                        }
                        else
                        {
                            r.Set("WP-LESchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-LWESchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-PlanSchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-PhSchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-AFESchedule", new DateRange().ToBsonDocument());

                            r.Set("WP-TQ", new WellDrillData().ToBsonDocument());
                            r.Set("WP-AFE", new WellDrillData().ToBsonDocument());
                            r.Set("WP-Plan", new WellDrillData().ToBsonDocument());
                            r.Set("WP-Actual", new WellDrillData().ToBsonDocument());
                            r.Set("WP-OP", new WellDrillData().ToBsonDocument());
                            r.Set("WP-LE", new WellDrillData().ToBsonDocument());
                            r.Set("WP-LWE", new WellDrillData().ToBsonDocument());
                        }

                        doscs.Add(r);
                    }
                    else
                    {
                        r.Set("RigName", "");


                        r.Set("WP-LESchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-LWESchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-PlanSchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-PhSchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-AFESchedule", new DateRange().ToBsonDocument());

                        r.Set("WP-TQ", new WellDrillData().ToBsonDocument());
                        r.Set("WP-AFE", new WellDrillData().ToBsonDocument());
                        r.Set("WP-Plan", new WellDrillData().ToBsonDocument());
                        r.Set("WP-Actual", new WellDrillData().ToBsonDocument());
                        r.Set("WP-OP", new WellDrillData().ToBsonDocument());
                        r.Set("WP-LE", new WellDrillData().ToBsonDocument());
                        r.Set("WP-LWE", new WellDrillData().ToBsonDocument());
                    }
                }

                #endregion
            }
            else
            {
                #region WellActivityUpdateMonthly
                dataWAUM = (List<WellActivityUpdateMonthly>)datas;
                foreach (var p in dataWAUM.ToList())
                {
                    var r = p.ToBsonDocument();
                    r.Set("ActivityType", BsonHelper.GetString(r, "Phase.ActivityType"));
                    r.Set("PhaseNo", BsonHelper.GetInt32(r, "Phase.PhaseNo"));

                    var actv = WellActivity.Get<WellActivity>(Query.And(
                        Query.EQ("WellName", p.WellName),
                        Query.EQ("UARigSequenceId", p.SequenceId)
                        ));

                    if (actv != null)
                    {

                        var phase = actv.Phases.Where(x => x.ActivityType.Equals(BsonHelper.GetString(r, "ActivityType")) &&
                            x.PhaseNo == BsonHelper.GetInt32(r, "PhaseNo")
                            ).FirstOrDefault();

                        r.Set("RigName", actv.RigName);

                        if (phase != null)
                        {
                            r.Set("WP-LESchedule", phase.LESchedule == null ? new DateRange().ToBsonDocument() : phase.LESchedule.ToBsonDocument());
                            r.Set("WP-LWESchedule", phase.LWESchedule == null ? new DateRange().ToBsonDocument() : phase.LWESchedule.ToBsonDocument());
                            r.Set("WP-PlanSchedule", phase.PlanSchedule == null ? new DateRange().ToBsonDocument() : phase.PlanSchedule.ToBsonDocument());
                            r.Set("WP-PhSchedule", phase.PhSchedule == null ? new DateRange().ToBsonDocument() : phase.PhSchedule.ToBsonDocument());
                            r.Set("WP-AFESchedule", phase.AFESchedule == null ? new DateRange().ToBsonDocument() : phase.AFESchedule.ToBsonDocument());

                            r.Set("WP-TQ", phase.TQ == null ? new WellDrillData().ToBsonDocument() : phase.TQ.ToBsonDocument());
                            r.Set("WP-AFE", phase.AFE == null ? new WellDrillData().ToBsonDocument() : phase.AFE.ToBsonDocument());
                            r.Set("WP-Plan", phase.Plan == null ? new WellDrillData().ToBsonDocument() : phase.Plan.ToBsonDocument());
                            r.Set("WP-Actual", phase.Actual == null ? new WellDrillData().ToBsonDocument() : phase.Actual.ToBsonDocument());
                            r.Set("WP-OP", phase.OP == null ? new WellDrillData().ToBsonDocument() : phase.OP.ToBsonDocument());
                            r.Set("WP-LE", phase.LE == null ? new WellDrillData().ToBsonDocument() : phase.LE.ToBsonDocument());
                            r.Set("WP-LWE", phase.LWE == null ? new WellDrillData().ToBsonDocument() : phase.LWE.ToBsonDocument());

                            r.Set("RealizedPIPElemCompetitiveScope", p.RealizedPIPElemCompetitiveScope.ToBsonDocument());
                            r.Set("RealizedPIPElemEfficientExecution", p.RealizedPIPElemEfficientExecution.ToBsonDocument());
                            r.Set("RealizedPIPElemSupplyChainTransformation", p.RealizedPIPElemSupplyChainTransformation.ToBsonDocument());
                            r.Set("RealizedPIPElemTechnologyAndInnovation", p.RealizedPIPElemTechnologyAndInnovation.ToBsonDocument());

                            r.Set("BankedSavingsCompetitiveScope", p.BankedSavingsCompetitiveScope.ToBsonDocument());
                            r.Set("BankedSavingsEfficientExecution", p.BankedSavingsEfficientExecution.ToBsonDocument());
                            r.Set("BankedSavingsSupplyChainTransformation", p.BankedSavingsSupplyChainTransformation.ToBsonDocument());
                            r.Set("BankedSavingsTechnologyAndInnovation", p.BankedSavingsTechnologyAndInnovation.ToBsonDocument());


                        }
                        else
                        {
                            r.Set("WP-LESchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-LWESchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-PlanSchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-PhSchedule", new DateRange().ToBsonDocument());
                            r.Set("WP-AFESchedule", new DateRange().ToBsonDocument());

                            r.Set("WP-TQ", new WellDrillData().ToBsonDocument());
                            r.Set("WP-AFE", new WellDrillData().ToBsonDocument());
                            r.Set("WP-Plan", new WellDrillData().ToBsonDocument());
                            r.Set("WP-Actual", new WellDrillData().ToBsonDocument());
                            r.Set("WP-OP", new WellDrillData().ToBsonDocument());
                            r.Set("WP-LE", new WellDrillData().ToBsonDocument());
                            r.Set("WP-LWE", new WellDrillData().ToBsonDocument());

                            r.Set("RealizedPIPElemCompetitiveScope", new WellDrillData().ToBsonDocument());
                            r.Set("RealizedPIPElemEfficientExecution", new WellDrillData().ToBsonDocument());
                            r.Set("RealizedPIPElemSupplyChainTransformation", new WellDrillData().ToBsonDocument());
                            r.Set("RealizedPIPElemTechnologyAndInnovation", new WellDrillData().ToBsonDocument());

                            r.Set("BankedSavingsCompetitiveScope", new WellDrillData().ToBsonDocument());
                            r.Set("BankedSavingsEfficientExecution", new WellDrillData().ToBsonDocument());
                            r.Set("BankedSavingsSupplyChainTransformation", new WellDrillData().ToBsonDocument());
                            r.Set("BankedSavingsTechnologyAndInnovation", new WellDrillData().ToBsonDocument());
                        }

                        doscs.Add(r);
                    }
                    else
                    {
                        #region else
                        r.Set("RigName", "");


                        r.Set("WP-LESchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-LWESchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-PlanSchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-PhSchedule", new DateRange().ToBsonDocument());
                        r.Set("WP-AFESchedule", new DateRange().ToBsonDocument());

                        r.Set("WP-TQ", new WellDrillData().ToBsonDocument());
                        r.Set("WP-AFE", new WellDrillData().ToBsonDocument());
                        r.Set("WP-Plan", new WellDrillData().ToBsonDocument());
                        r.Set("WP-Actual", new WellDrillData().ToBsonDocument());
                        r.Set("WP-OP", new WellDrillData().ToBsonDocument());
                        r.Set("WP-LE", new WellDrillData().ToBsonDocument());
                        r.Set("WP-LWE", new WellDrillData().ToBsonDocument());

                        r.Set("RealizedPIPElemCompetitiveScope", new WellDrillData().ToBsonDocument());
                        r.Set("RealizedPIPElemEfficientExecution", new WellDrillData().ToBsonDocument());
                        r.Set("RealizedPIPElemSupplyChainTransformation", new WellDrillData().ToBsonDocument());
                        r.Set("RealizedPIPElemTechnologyAndInnovation", new WellDrillData().ToBsonDocument());

                        r.Set("BankedSavingsCompetitiveScope", new WellDrillData().ToBsonDocument());
                        r.Set("BankedSavingsEfficientExecution", new WellDrillData().ToBsonDocument());
                        r.Set("BankedSavingsSupplyChainTransformation", new WellDrillData().ToBsonDocument());
                        r.Set("BankedSavingsTechnologyAndInnovation", new WellDrillData().ToBsonDocument());

                        #endregion
                    }
                }
                #endregion
            }


            string[] arr = { "_id", "WellName", "SequenceId", "ActivityType", "PhaseNo",
            "RigName",   "WP-LESchedule" ,        "WP-LWESchedule",      "WP-PlanSchedule",  "WP-PhSchedule", "WP-AFESchedule",
                           "WP-TQ", "WP-AFE", "WP-Plan", "WP-Actual", "WP-OP", "WP-LE", "WP-LWE", "WP-OP",
                           "BankedSavingsCompetitiveScope", "BankedSavingsEfficientExecution", 
                           "BankedSavingsSupplyChainTransformation", "BankedSavingsTechnologyAndInnovation"
                           };
            var uwindAll = BsonHelper.Unwind(doscs, "Elements", "", arr.ToList());

            var realizedonly = uwindAll.Where(x => x.GetString("Completion").Equals("Realized")).ToList();

            Dictionary<int, double> results = new Dictionary<int, double>();
            List<int> years = yearsCalc;

            #region PISs

            List<MonthlyElementHelperDetails> details = new List<MonthlyElementHelperDetails>();
            foreach (var real in realizedonly)
            {
                foreach (var year in years)
                {
                    if (BsonHelper.GetString(real, "Title").Equals("Maximize target size (simple BHA)"))
                    {

                    }

                    int daynum = 0;
                    DateRange dr = new DateRange(BsonHelper.GetDateTime(real, "Period.Start"), BsonHelper.GetDateTime(real, "Period.Finish"));
                    DateRange outdate = new DateRange();
                    var res = GetWeightEachYear(year, dr, out daynum, out outdate).FirstOrDefault().Value;
                    string tit = "";
                    string clas = "";
                    if (BreakdownBy.Equals("Classification"))
                    {
                        tit = real.GetString("Classification"); clas = "Classification";
                    }
                    else if (BreakdownBy.Equals("Theme"))
                    {
                        tit = real.GetString("Theme"); clas = "Theme";
                    }
                    else
                    {
                        tit = real.GetString("PerformanceUnit"); clas = "PerformanceUnit";
                    }


                    var dd = doscs.Where(x => BsonHelper.GetString(x, "_id").Equals(real.GetString("_parentid")));
                    double banksave = 0;

                    if (real.GetString("Classification").Equals("Supply Chain Transformation"))
                        banksave = dd.FirstOrDefault().GetDouble("BankedSavingsSupplyChainTransformation.Cost");
                    else if (real.GetString("Classification").Equals("Competitive Scope"))
                        banksave = dd.FirstOrDefault().GetDouble("BankedSavingsCompetitiveScope.Cost");
                    else if (real.GetString("Classification").Equals("Efficient Execution"))
                        banksave = dd.FirstOrDefault().GetDouble("BankedSavingsEfficientExecution.Cost");
                    else if (real.GetString("Classification").Equals("Technology and Innovation"))
                        banksave = dd.FirstOrDefault().GetDouble("BankedSavingsTechnologyAndInnovation.Cost");

                    details.Add(new MonthlyElementHelperDetails
                    {
                        PIPID = real.GetString("_id"),
                        BankSaved = banksave * res,
                        Idea = real.GetString("Title"),
                        Year = year,
                        Classified = "Classification",
                        Title = tit,
                        Value = res,
                        ItemId = real.GetInt32("ElementId"),
                        NumOfDayinThisYear = daynum,
                        oPeriod = dr,
                        Period = outdate,
                        Completion = real.GetString("Completion"),
                        //isIncludeLe  = checkedx,

                        WellName = real.GetString("WellName"),
                        RigName = real.GetString("RigName"),
                        ActivityType = real.GetString("ActivityType"),
                        SequenceId = real.GetString("SequenceId "),

                        oDaysPlanImprovement = real.GetDouble("DaysPlanImprovement"),
                        oDaysPlanRisk = real.GetDouble("DaysPlanRisk"),
                        oDaysCurrentWeekImprovement = real.GetDouble("DaysCurrentWeekImprovement"),
                        oDaysCurrentWeekRisk = real.GetDouble("DaysCurrentWeekRisk"),
                        oCostPlanImprovement = real.GetDouble("CostPlanImprovement"),
                        oCostPlanRisk = real.GetDouble("CostPlanRisk"),
                        oCostCurrentWeekImprovement = real.GetDouble("CostCurrentWeekImprovement"),
                        oCostCurrentWeekRisk = real.GetDouble("CostCurrentWeekRisk"),

                        DaysPlanImprovement = res * real.GetDouble("DaysPlanImprovement"),
                        DaysPlanRisk = res * real.GetDouble("DaysPlanRisk"),
                        DaysCurrentWeekImprovement = res * real.GetDouble("DaysCurrentWeekImprovement"),
                        DaysCurrentWeekRisk = res * real.GetDouble("DaysCurrentWeekRisk"),
                        CostPlanImprovement = res * real.GetDouble("CostPlanImprovement"),
                        CostPlanRisk = res * real.GetDouble("CostPlanRisk"),
                        CostCurrentWeekImprovement = res * real.GetDouble("CostCurrentWeekImprovement"),
                        CostCurrentWeekRisk = res * real.GetDouble("CostCurrentWeekRisk")
                    });
                }

            }
            #endregion

            List<string> validCategories = new List<string>();
            validCategories.Add("Supply Chain Transformation");
            validCategories.Add("Competitive Scope");
            validCategories.Add("Efficient Execution");
            validCategories.Add("Technology and Innovation");



            List<MonthlyElementHelper> rexs = new List<MonthlyElementHelper>();

            foreach (var year in years)
            {
                var inyear = details.Where(x => year == x.Year);
                List<MonthlyElementHelper> selaincateg = new List<MonthlyElementHelper>();

                foreach (var o in inyear.GroupBy(x => x.Title))
                {


                    if (validCategories.Contains(o.Key))
                    {
                        MonthlyElementHelper rex = new MonthlyElementHelper();
                        rex.Classified = o.FirstOrDefault().Classified;
                        rex.Title = o.FirstOrDefault().Title;
                        rex.Year = o.FirstOrDefault().Year;

                        rex.oDaysPlan = o.Sum(x => (x.oDaysPlanImprovement + x.oDaysPlanRisk));
                        rex.oDaysCurrentWeek = o.Sum(x => (x.oDaysCurrentWeekImprovement + x.oDaysCurrentWeekRisk));
                        rex.oCostPlan = o.Sum(x => (x.oCostPlanImprovement + x.oCostPlanRisk));
                        rex.oCostCurrentWeek = o.Sum(x => (x.oCostCurrentWeekImprovement + x.oCostCurrentWeekRisk));

                        rex.DaysPlan = o.Sum(x => (x.DaysPlanImprovement + x.DaysPlanRisk));
                        rex.DaysCurrentWeek = o.Sum(x => (x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk));
                        rex.CostPlan = o.Sum(x => (x.CostPlanImprovement + x.CostPlanRisk));
                        rex.CostCurrentWeek = o.Sum(x => (x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk));
                        rex.BankedSave = o.GroupBy(e => e.PIPID).Select(d => d.FirstOrDefault().BankSaved).DefaultIfEmpty(0).Sum(d => d);

                        rex.Details = o.ToList();
                        rexs.Add(rex);
                    }
                    else
                    {
                        MonthlyElementHelper rex = new MonthlyElementHelper();
                        rex.Classified = o.FirstOrDefault().Classified;
                        rex.Title = "All Others";
                        rex.Year = o.FirstOrDefault().Year;

                        rex.oDaysPlan = o.Sum(x => (x.oDaysPlanImprovement + x.oDaysPlanRisk));
                        rex.oDaysCurrentWeek = o.Sum(x => (x.oDaysCurrentWeekImprovement + x.oDaysCurrentWeekRisk));
                        rex.oCostPlan = o.Sum(x => (x.oCostPlanImprovement + x.oCostPlanRisk));
                        rex.oCostCurrentWeek = o.Sum(x => (x.oCostCurrentWeekImprovement + x.oCostCurrentWeekRisk));

                        rex.DaysPlan = o.Sum(x => (x.DaysPlanImprovement + x.DaysPlanRisk));
                        rex.DaysCurrentWeek = o.Sum(x => (x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk));
                        rex.CostPlan = o.Sum(x => (x.CostPlanImprovement + x.CostPlanRisk));
                        rex.CostCurrentWeek = o.Sum(x => (x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk));
                        rex.BankedSave = o.GroupBy(e => e.PIPID).Select(d => d.FirstOrDefault().BankSaved).DefaultIfEmpty(0).Sum(d => d);

                        rex.Details = o.ToList();
                        selaincateg.Add(rex);
                    }
                }

                if (selaincateg.Count() > 0)
                {
                    MonthlyElementHelper rex = new MonthlyElementHelper();
                    rex.Classified = selaincateg.FirstOrDefault().Classified;
                    rex.Title = "All Others";
                    rex.Year = selaincateg.FirstOrDefault().Year;

                    rex.oDaysPlan = selaincateg.Sum(x => (x.oDaysPlan));
                    rex.oDaysCurrentWeek = selaincateg.Sum(x => (x.oCostPlan));
                    rex.oCostPlan = selaincateg.Sum(x => (x.oCostPlan));
                    rex.oCostCurrentWeek = selaincateg.Sum(x => (x.oDaysCurrentWeek));

                    rex.DaysPlan = selaincateg.Sum(x => (x.DaysPlan));
                    rex.DaysCurrentWeek = selaincateg.Sum(x => (x.CostPlan));
                    rex.CostPlan = selaincateg.Sum(x => (x.CostPlan));
                    rex.CostCurrentWeek = selaincateg.Sum(x => (x.DaysCurrentWeek));
                    //rex.BankedSave = o.GroupBy(e => e.PIPID).Select(d => d.FirstOrDefault().BankSaved).DefaultIfEmpty(0).Sum(d => d);

                    foreach (var p in selaincateg)
                    {
                        rex.Details.AddRange(p.Details.ToList());
                    }
                    rexs.Add(rex);
                }
            }
            return rexs;
        }


        #endregion

        public JsonResult GetWaterfallOfMonthlyLE(
             DateTime monthlysequence,
            List<Int32> YearsCalc,

            string Date = "",
            List<string> Regions = null,
            List<string> OperatingUnits = null,
            List<string> RigTypes = null,
            List<string> RigNames = null,
            List<string> Projects = null,
            List<string> WellNames = null,
            List<string> Activities = null,
            List<string> PerformanceUnits = null,
            string Status = null,
            string OPType = "All",
            bool IncludeDiffOfLEAndCalcLE = true,
            bool IncludeNotEnteredLE = true,
            string DateStart = "",
            string DateStart2 = "",
            string DateFinish = "",
            string DateFinish2 = "",
            string DateRelation = "OR",


            string plan = "OP-14",
            string date = "",
            string BreakdownBy = "Classification",
            string ShowBy = "Cost",
            string LoadData = "Monthly"
            )
        {
            var ri = new ResultInfo();
            try
            {
                List<WellActivity> filteredData, filteredDataOpLe = new List<WellActivity>();
                var qs = GetFilterQueries(out filteredData, date, Regions, OperatingUnits, RigTypes, RigNames, Projects, WellNames, PerformanceUnits, Activities, DateStart, DateStart2, DateFinish, DateFinish2, DateRelation
                    );

                GetFilterQueriesOpLe(out filteredDataOpLe, date, Regions, OperatingUnits, RigTypes, RigNames, Projects, WellNames, PerformanceUnits, Activities, DateStart, DateStart2, DateFinish, DateFinish2, DateRelation
                   );

                #region Default year
                if (YearsCalc == null)
                {
                    var range = WellActivity.Populate<WellActivity>(null);
                    var opschedule = range.Select(x => x.PsSchedule);

                    YearsCalc = new List<int>();
                    foreach (var t in opschedule)
                    {
                        if (t.Start.Year > 2000 && !YearsCalc.Contains(t.Start.Year))
                            YearsCalc.Add(t.Start.Year);
                        if (t.Finish.Year > 2000 && !YearsCalc.Contains(t.Finish.Year))
                            YearsCalc.Add(t.Finish.Year);
                    }

                    int minyear = YearsCalc.Min();
                    int maxyear = YearsCalc.Max();
                    //YearsCalc.Clear();
                    List<int> yearcals = new List<int>();
                    for (int x = minyear; x <= maxyear; x++)
                    {
                        yearcals.Add(x);
                    }

                    YearsCalc = new List<int>();
                    YearsCalc = yearcals;

                }



                #endregion
                #region Append With Weekly
                //List<BsonDocument> bdocs = new List<BsonDocument>();
                List<WellActivityUpdateMonthly> monthlies = new List<WellActivityUpdateMonthly>();
                List<WellActivityUpdate> weekly = new List<WellActivityUpdate>();

                List<MonthlyElementHelper> pipsValues = new List<MonthlyElementHelper>();
                List<MonthlyElementHelper> pipsValuesWeekly = new List<MonthlyElementHelper>();
                List<MonthlyElementHelper> pipsValuesCombine = new List<MonthlyElementHelper>();

                MonthlyOPHelper OPValues = new MonthlyOPHelper();
                MonthlyLEHelper LEValues = new MonthlyLEHelper();

                double bankcostsupplychain = 0;
                double bankcostcompetitive = 0;
                double bankcostefficient = 0;
                double bankcosttechnology = 0;

                double CalcLECost = 0;

                if (LoadData.Equals("Monthly"))
                {
                    foreach (var g in qs)
                    {
                        monthlies.Add(BsonSerializer.Deserialize<WellActivityUpdateMonthly>(g.ToBsonDocument()));
                    }
                    #region obsolete
                    // monthlies = (List<WellActivityUpdate>)gs; //WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(Query.And(queries));
                    //if (RigNames != null && RigNames.Count() > 0)
                    //{
                    //    foreach (var p in monthlies.ToList())
                    //    {
                    //        var actv = WellActivity.Get<WellActivity>(Query.And(
                    //            Query.EQ("WellName", p.WellName),
                    //            Query.EQ("UARigSequenceId", p.SequenceId)
                    //            ));

                    //        if (actv != null)
                    //        {

                    //            p.RigName = actv.RigName;
                    //            //r.Set("RigName", actv.RigName);
                    //        }
                    //    }

                    // monthlies = monthlies.Where(x => RigNames.Contains(x.RigName)).ToList();
                    //}

                    #endregion

                    pipsValues = MonthlyChartCalc(monthlies, YearsCalc, BreakdownBy);

                    bankcostsupplychain = monthlies.Sum(x => x.BankedSavingsSupplyChainTransformation == null ? 0 : x.BankedSavingsSupplyChainTransformation.Cost);
                    bankcostcompetitive = monthlies.Sum(x => x.BankedSavingsCompetitiveScope == null ? 0 : x.BankedSavingsCompetitiveScope.Cost);
                    bankcostefficient = monthlies.Sum(x => x.BankedSavingsEfficientExecution == null ? 0 : x.BankedSavingsEfficientExecution.Cost);
                    bankcosttechnology = monthlies.Sum(x => x.BankedSavingsTechnologyAndInnovation == null ? 0 : x.BankedSavingsTechnologyAndInnovation.Cost);


                    string[] categories = { "Competitive Scope", "Efficient Execution", "Supply Chain Transformation", "Technology and Innovation", "All Others" };

                    Dictionary<string, string> categsad = pipsValues.ToDictionary(x => x.Year.ToString() + "-" + x.Title, x => x.Title);
                    Dictionary<string, string> addCateg = new Dictionary<string, string>();
                    foreach (var year in YearsCalc)
                    {
                        foreach (var c in categories)
                        {
                            if (!categsad.Keys.Contains(year.ToString() + "-" + c.ToString()))
                            {
                                addCateg.Add(year.ToString() + "-" + c.ToString(), c);
                            }
                        }
                    }

                    foreach (var t in addCateg)
                    {
                        MonthlyElementHelper newCateg = new MonthlyElementHelper();
                        newCateg.Title = t.Value;
                        newCateg.Year = Convert.ToInt32(t.Key.Split('-')[0]);
                        pipsValues.Add(newCateg);
                    }




                    OPValues = MonthlyChartCalcOPFromWellPlan(filteredDataOpLe, YearsCalc, BreakdownBy);
                    LEValues = MonthlyChartCalcLEFromWellPlan(filteredDataOpLe, YearsCalc, BreakdownBy);
                }


                #endregion

                #region Monthly Data
                var result = new List<WaterfallMonthlyLE>();

                var grp = pipsValues.GroupBy(x => x.Year).ToList();

                List<WaterfallMonthlyLE> elementsOfClassificationDay = new List<WaterfallMonthlyLE>();
                List<WaterfallMonthlyLE> elementsOfClassificationCost = new List<WaterfallMonthlyLE>();

                foreach (var y in pipsValues.GroupBy(x => x.Title))
                {
                    WaterfallMonthlyLE gcost = new WaterfallMonthlyLE();
                    WaterfallMonthlyLE gdays = new WaterfallMonthlyLE();
                    var elemenDaysCurTotal = y.ToList().Sum(x => x.DaysCurrentWeek);
                    var elemenCostCurTotal = y.ToList().Sum(x => x.CostCurrentWeek);

                    gcost.title = y.Key;
                    gdays.title = y.Key;

                    gdays.value = elemenDaysCurTotal;
                    gcost.value = elemenCostCurTotal;// *1000000;

                    elementsOfClassificationDay.Add(gdays);
                    elementsOfClassificationCost.Add(gcost);
                }


                foreach (var t in pipsValues)
                {
                    t.CostCurrentWeek = t.CostCurrentWeek;// *1000000;
                    t.oCostCurrentWeek = t.oCostCurrentWeek;// *1000000;
                    foreach (var y in t.Details)
                    {
                        y.CostCurrentWeekImprovement = y.CostCurrentWeekImprovement;// *1000000;
                        y.CostCurrentWeekRisk = y.CostCurrentWeekRisk;// *1000000;
                        y.oCostCurrentWeekImprovement = y.oCostCurrentWeekImprovement;// *1000000;
                        y.oCostCurrentWeekRisk = y.oCostCurrentWeekRisk;// *1000000;

                    }

                }

                foreach (var t in elementsOfClassificationCost)
                {
                    if (t.title.Equals("Supply Chain Transformation"))
                        t.value = t.value + (bankcostsupplychain);//* 1000000);
                    if (t.title.Equals("Efficient Execution"))
                        t.value = t.value + (bankcostefficient);//* 1000000);

                    if (t.title.Equals("Technology and Innovation"))
                        t.value = t.value + (bankcosttechnology);//* 1000000);

                    if (t.title.Equals("Competitive Scope"))
                        t.value = t.value + (bankcostcompetitive);//* 1000000);
                }

                CalcLECost = OPValues.Cost + elementsOfClassificationCost.Sum(x => x.value);


                if (ShowBy.Equals("Days"))
                {
                    //result.Add(new WaterfallMonthlyLE() { title = plan, valueOP = OPValues.Days * -1 });
                    result.Add(new WaterfallMonthlyLE() { title = plan, value = OPValues.Days });
                    //result.Add(new WaterfallMonthlyLE() { title = "OP w/ LE", value = 0 * -1 });
                    result.AddRange(elementsOfClassificationDay);
                    //result.Add(new WaterfallMonthlyLE() { title = "Gap to LE", value = GAPDays });
                    result.Add(new WaterfallMonthlyLE() { title = "Gap to LE", value = 0 });
                    //result.Add(new WaterfallMonthlyLE() { title = "LE", value = LEDays *-1 });
                    result.Add(new WaterfallMonthlyLE() { title = "LE" });
                }
                else
                {
                    var totalElementsRealizedAndBank = elementsOfClassificationCost.Sum(d => d.value);

                    var GAP = LEValues.Cost - ((OPValues.Cost * 1000000) + totalElementsRealizedAndBank);

                    var delta = LEValues.Cost - OPValues.Cost;
                    var calculatedDelta = delta - totalElementsRealizedAndBank;
                    var calcLE = LEValues.Cost - calculatedDelta;

                    var gap = calcLE - (OPValues.Cost + totalElementsRealizedAndBank);

                    result.Add(new WaterfallMonthlyLE() { title = plan, value = OPValues.Cost * 1000000 });
                    result.AddRange(elementsOfClassificationCost);
                    result.Add(new WaterfallMonthlyLE() { title = "Gap to LE", value = GAP });
                    result.Add(new WaterfallMonthlyLE() { title = "LE", value = LEValues.Cost }); //LEValues.Cost });
                }

                var grid2 = pipsValues.GroupBy(d => d.Year).Select(d =>
                {
                    var mil = 1;// 1000000;

                    var totalClassification1 = d.Where(e => e.Title.ToLower().Equals("Competitive Scope".ToLower()))
                        .DefaultIfEmpty(new MonthlyElementHelper()).Sum(e => e.CostCurrentWeek + (e.BankedSave * mil));
                    var totalClassification2 = d.Where(e => e.Title.ToLower().Equals("Supply Chain Transformation".ToLower()))
                        .DefaultIfEmpty(new MonthlyElementHelper()).Sum(e => e.CostCurrentWeek + (e.BankedSave * mil));
                    var totalClassification3 = d.Where(e => e.Title.ToLower().Equals("Efficient Execution".ToLower()))
                        .DefaultIfEmpty(new MonthlyElementHelper()).Sum(e => e.CostCurrentWeek + (e.BankedSave * mil));
                    var totalClassification4 = d.Where(e => e.Title.ToLower().Equals("Technology and Innovation".ToLower()))
                        .DefaultIfEmpty(new MonthlyElementHelper()).Sum(e => e.CostCurrentWeek + (e.BankedSave * mil));

                    var op = OPValues.OPDetails.Where(e => e.Year == d.Key).Sum(e => e.OP.Cost);
                    var le = LEValues.LEDetails.Where(e => e.Year == d.Key).Sum(e => e.LE.Cost);

                    var totalElementsRealizedAndBank = d.Sum(e => e.CostCurrentWeek + (e.BankedSave * mil));
                    var details = d.SelectMany(e => e.Details).OrderBy(e => e.WellName).ToList();

                    var delta = le - op;
                    var calculatedDelta = delta - totalElementsRealizedAndBank;
                    var calculatedLE = le - calculatedDelta;
                    var gap = calculatedLE - (op + totalElementsRealizedAndBank);

                    var res = new
                    {
                        Year = d.Key,
                        OP = op,
                        Competitive_Scope = totalClassification1,
                        Supply_Chain_Transformation = totalClassification2,
                        Efficient_Execution = totalClassification3,
                        Technology_and_Innovation = totalClassification4,
                        LE = le,
                        Details = details
                    };

                    return res.ToBsonDocument();
                }).OrderBy(d => d.GetInt64("Year"));

                List<BsonDocument> docs = new List<BsonDocument>();
                foreach (var p in pipsValues)
                {
                    docs.Add(p.ToBsonDocument());
                }

                var grid = DataHelper.ToDictionaryArray(docs);// DataHelper.ToDictionaryArray(DataHelper.Populate("MonthlyValue"));

                ri.Data = new MonthlyReportAndGridResult()
                {
                    Chart = result,
                    Grid = grid,
                    OPs = OPValues.OPDetails,
                    LEs = LEValues.LEDetails,
                    Grid2 = DataHelper.ToDictionaryArray(grid2).ToList()
                };
                #endregion Monthly Data


            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public ActionResult FinancialCalendar()
        {
            var latest = WEISFinancialCalendar.Get<WEISFinancialCalendar>(null, SortBy.Descending("MonthYear"));
            if (latest == null)
            {
                ViewBag.LatestMonthYear = DateTime.Now.Year;
            }
            else
            {
                ViewBag.LatestMonthYear = latest.MonthYear.Year + 1;
            }
            return View();
        }

        public JsonResult FinancialCalendarAddMonth(int year)
        {
            var ri = new ResultInfo();

            try
            {
                var now = DateTime.Now;
                var dateTimeNow = Tools.ToUTC(new DateTime(now.Year, now.Month, 1), true);
                var dateTime = Tools.ToUTC(new DateTime(year, 1, 1));
                var maxDateTime = dateTime.AddYears(1);
                var isEmptyBefore = !WEISFinancialCalendar.Populate<WEISFinancialCalendar>().Any();

                var isExist = WEISFinancialCalendar.Get<WEISFinancialCalendar>(Query.EQ("MonthYear", dateTime));
                if (isExist != null)
                {
                    ri.PushException(new Exception("Already added"));
                }
                else
                {
                    while (dateTime < maxDateTime)
                    {
                        new WEISFinancialCalendar()
                        {
                            MonthYear = Tools.ToUTC(dateTime),
                            Status = (isEmptyBefore && dateTime < dateTimeNow) ? "Closed" : "Not Yet Started"
                        }.Save();

                        dateTime = dateTime.AddMonths(1);
                    }
                }
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult FinancialCalendarGetData()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                return WEISFinancialCalendar.Populate<WEISFinancialCalendar>(null, sort: SortBy.Ascending("MonthYear"));
            });
        }

        public async Task<string> RunCalendar(int id)
        {
            await Task.Run(() =>
            {
                FinancialCalendarInitiate(id);
            });
            return "OK";
        }

        public async Task<string> BgWorker()
        {
            var percentage = "";
            var config = Config.Get<Config>("FinansialCalenderCounter");
            if (config == null)
            {
                config = new Config()
                {
                    _id = "FinansialCalenderCounter",
                    ConfigModule = "FinansialCalender"
                };
            }
            config.ConfigValue = 0;
            config.Save();
            var progressHandler = new Progress<string>(value =>
            {
                percentage = value;
            });
            var progress = progressHandler as IProgress<string>;
            await Task.Run(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    if (progress != null)
                        progress.Report("" + i);
                    config.ConfigValue = i;
                    config.Save();
                    Thread.Sleep(100);
                }
            });
            config.Delete();
            percentage = "Completed.";
            return percentage;
        }



        public JsonResult GetCalculationStatus(int Counter = 0)
        {
            var ri = new ResultInfo();
            try
            {
                if (Counter >= 20)
                {
                    // reset calculation status when too long
                    ResetRecalculaeBusPlanStatus();
                }

                var result = new { IsRecalculate = true, Progress = 0.0 }.ToBsonDocument();
                var status = IsRecalculateBusPlan();
                result.Set("IsRecalculate", status);

                if (status)
                {
                    var progress = GetRecalculationProgress();
                    result.Set("Progress", progress);
                }

                ri.Data = result.ToDictionary();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult GetProgresStatus()
        {
            var ri = new ResultInfo();
            var y = DataHelper.Populate("temp_finansial", null, 1, 0, null, SortBy.Descending("_id"));
            if (y.Count == 0)
            {
                var ys = new BsonDocument().Set("_id", 0).Set("Progress", "None");
                y.Add(ys);
            }

            ri.Data = y.Select(x => x.ToDictionary());
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult CheckCalendarStatus(string id)
        {
            var ri = new ResultInfo();
            var r = WEISFinancialCalendar.Get<WEISFinancialCalendar>(id).Status;
            ri.Data = r;
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult UpdateCalendarStatus(string id)
        {
            var ri = new ResultInfo();
            var r = WEISFinancialCalendar.Get<WEISFinancialCalendar>(id);
            r.Save();
            ri.Data = "OK";
            return MvcTools.ToJsonResult(ri);
        }

        public static void ResetRecalculaeBusPlanStatus()
        {
            var config = Config.Get<Config>("FinansialCalenderCounter");
            if (config != null)
            {
                config.Delete();
            }
        }

        public static bool IsRecalculateBusPlan()
        {
            var config = Config.Get<Config>("FinansialCalenderCounter");
            return (config != null);
        }

        public static double GetRecalculationProgress()
        {
            var config = Config.Get<Config>("FinansialCalenderCounter");
            if (config == null)
            {
                return 0;
            }

            return Convert.ToDouble(config.ConfigValue);
        }


        public List<PIPElement> GetCRElementsFromExisting(string wellname, string seqid, string activity)
        {
            var t = WellPIP.Get<WellPIP>(Query.And(
                 Query.EQ("WellName", wellname),
                 Query.EQ("SequenceId", seqid),
                 Query.EQ("ActivityType", activity)
                 ));

            if (t != null)
            {
                return t.CRElements;
            }
            return new List<PIPElement>();
        }

        public JsonResult FinancialCalendarInitiate(int id)
        {
            var ri = new ResultInfo();
            var pUser = WebTools.LoginUser.UserName;
            var pEmail = WebTools.LoginUser.Email;
            try
            {
                var Progress = "Working";

                DataHelper.Save("temp_finansial", new BsonDocument().Set("_id", id).Set("Progress", Progress));
                Task.Run(() =>
                {

                    var sampleWAUM = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(null);
                    var now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    var calendar = WEISFinancialCalendar.Get<WEISFinancialCalendar>(Query.EQ("_id", id));
                    var monthYear = Tools.ToUTC(new DateTime(calendar.MonthYear.Year, calendar.MonthYear.Month, now.Day));
                    var rawWellActivityIds = GetActivitiesBasedOnMonth(monthYear);
                    if (sampleWAUM == null)
                    {
                        var wellActivityIds = rawWellActivityIds.Select(d => d._id + "|" + d.ActivityType).ToList();

                        MonthlyReportInitiate(calendar.MonthYear, "", wellActivityIds, false, true, pUser, pEmail);

                        calendar.Status = "Active";
                        calendar.Save();
                    }
                    else
                    {
                        var calendars = WEISFinancialCalendar.Populate<WEISFinancialCalendar>(Query.LT("MonthYear", monthYear));

                        calendars.ForEach(d =>
                        {
                            d.Status = "Closed";
                            d.Save();
                        });

                        var prevMonthlyReports = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(Query.EQ("UpdateVersion", calendar.MonthYear.AddMonths(-1)));

                        // looking for new activity
                        var newActivity = new List<MonthlyEventsBasedOnMonth>();
                        foreach (var xx in rawWellActivityIds)
                        {
                            var finder = prevMonthlyReports.FirstOrDefault(x => x.WellName == xx.WellName && x.Phase.ActivityType == xx.ActivityType && x.SequenceId == xx.UARigSequenceId);
                            if (finder == null)
                            {
                                newActivity.Add(xx);
                            }
                        }
                        //saving the new activities
                        if (newActivity.Any())
                        {
                            var wellActivityIds = newActivity.Select(d => d._id + "|" + d.ActivityType).ToList();
                            MonthlyReportInitiate(calendar.MonthYear, "", wellActivityIds, false, true, pUser, pEmail);
                        }

                        //save the old registered activities
                        prevMonthlyReports.ForEach(d =>
                        {
                            var weeklyActivityUpdate = WellActivityUpdate.Get<WellActivityUpdate>(
                            Query.And(
                                Query.EQ("WellName", d.WellName),
                                Query.EQ("SequenceId", d.SequenceId),
                                Query.EQ("Phase.ActivityType", d.Phase.ActivityType)
                            )
                            );

                            d._id = null;
                            d.UpdateVersion = calendar.MonthYear;
                            d.Status = "In-Progress";
                            d.InitiateBy = pUser;
                            d.InitiateDate = Tools.ToUTC(DateTime.Now);
                            if (d.CRElements.Any())
                            {
                                d.CRElements = new MonthlyReportController().GetCRElementsFromExisting(d.WellName, d.SequenceId, d.Phase.ActivityType);
                            }

                            if (weeklyActivityUpdate == null && d.Phase.PhSchedule.Start > Tools.DefaultDate && d.Phase.PhSchedule.Finish > Tools.DefaultDate && !d.Phase.ActivityType.Contains("FLOWBACK"))
                            {
                                d.Save();
                            }
                        });



                        calendar.Status = "Active";
                        calendar.Save();
                    }
                    //Task.Run(() => BgWorker());
                }).ContinueWith(t => DataHelper.Save("temp_finansial", new BsonDocument().Set("_id", id).Set("Progress", "Done")));


            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public string GetColor(string key)
        {
            var listColor = new List<Color>();
            listColor.Add(new Color() { color = "#002060", key = "Plan" });
            listColor.Add(new Color() { color = "#92D050", key = "RealizedCompetitiveScopeOpp" });
            listColor.Add(new Color() { color = "#00B050", key = "RealizedSupplyChainTransformationOpp" });
            listColor.Add(new Color() { color = "#00B0F0", key = "RealizedEfficientExecutionOpp" });
            listColor.Add(new Color() { color = "#92CDDC", key = "RealizedTechnologyandInnovationOpp" });
            listColor.Add(new Color() { color = "#FF0000", key = "RealizedCompetitiveScopeRisk" });
            listColor.Add(new Color() { color = "#FFC000", key = "RealizedSupplyChainTransformationRisk" });
            listColor.Add(new Color() { color = "#AA1AC7", key = "RealizedEfficientExecutionRisk" });
            listColor.Add(new Color() { color = "#945757", key = "RealizedTechnologyandInnovationRisk" });
            listColor.Add(new Color() { color = "#92D050", key = "ADSCompetitiveScope" });
            listColor.Add(new Color() { color = "#00B050", key = "ADSSupplyChainTransformation" });
            listColor.Add(new Color() { color = "#00B0F0", key = "ADSEfficientExecution" });
            listColor.Add(new Color() { color = "#92CDDC", key = "ADSTechnologyandInnovation" });
            listColor.Add(new Color() { color = "#0070C0", key = "LE" });
            listColor.Add(new Color() { color = "#92D050", key = "UnrealizedCompetitiveScopeOpp" });
            listColor.Add(new Color() { color = "#00B050", key = "UnrealizedSupplyChainTransformationOpp" });
            listColor.Add(new Color() { color = "#00B0F0", key = "UnrealizedEfficientExecutionOpp" });
            listColor.Add(new Color() { color = "#92CDDC", key = "UnrealizedTechnologyandInnovationOpp" });
            listColor.Add(new Color() { color = "#FF0000", key = "UnrealizedCompetitiveScopeRisk" });
            listColor.Add(new Color() { color = "#FFC000", key = "UnrealizedSupplyChainTransformationRisk" });
            listColor.Add(new Color() { color = "#AA1AC7", key = "UnrealizedEfficientExecutionRisk" });
            listColor.Add(new Color() { color = "#945757", key = "UnrealizedTechnologyandInnovationRisk" });
            listColor.Add(new Color() { color = "#333333", key = "UnriskedUpside" });

            var ret = listColor.FirstOrDefault(x => x.key == key) ?? new Color();
            return ret.color;
        }

    }

    public class WaterfallMonthlyLE
    {
        public string title { set; get; }
        public double value { set; get; }
        public double valueOP { set; get; }
        public string summary { set; get; }
        public List<AnnualValue> Details { get; set; }
        public WaterfallMonthlyLE()
        {
            Details = new List<AnnualValue>();
        }

    }

    public class AnnualValue
    {
        public int Year { set; get; }
        public double Value { set; get; }
        public string Title { set; get; }
    }

    class MonthlyReportAndGridResult
    {
        public List<MonthlyOPHelperDetails> OPs { get; set; }
        public List<MonthlyLEHelperDetails> LEs { get; set; }
        public Dictionary<string, object>[] Grid { set; get; }
        public List<WaterfallMonthlyLE> Chart { set; get; }
        public List<Dictionary<string, object>> Grid2 { set; get; }
    }

    public class MonthlyElementBase
    {
        // elements
        public double oDaysPlan { get; set; }
        public double oDaysCurrentWeek { get; set; }
        public double oCostPlan { get; set; }
        public double oCostCurrentWeek { get; set; }

        // elements
        public double DaysPlan { get; set; }
        public double DaysCurrentWeek { get; set; }
        public double CostPlan { get; set; }
        public double CostCurrentWeek { get; set; }
    }

    public class MonthlyEventsBasedOnMonth
    {
        public string _id { set; get; }
        public string WellName { set; get; }
        public string UARigSequenceId { set; get; }
        public string RigName { set; get; }
        public string AssetName { set; get; }
        public string ActivityType { set; get; }
        public DateRange PhSchedule { set; get; }
        public DateRange AFESchedule { set; get; }
    }

    #region New Class Helper
    public class MonthlyElementHelper : MonthlyElementBase
    {
        public int Year { get; set; }
        //public double Value { get; set; }
        public string Classified { get; set; }
        public string Title { get; set; }

        public double BankedSave { get; set; }

        public List<MonthlyElementHelperDetails> Details { get; set; }
        public List<MonthlyOPHelperDetails> OPDetails { get; set; }
        public List<MonthlyLEHelperDetails> LEDetails { get; set; }

        //public MonthlyElementBase Realized { get; set; }
        //public MonthlyElementBase Unrealized { get; set; }

        // op
        public double OPCost { get; set; }
        public double OPDays { get; set; }

        // op with LE
        //public double OPWithLeCost { get; set; }
        //public double OPWithLeDays { get; set; }

        // LE
        public double LECost { get; set; }
        public double LEDays { get; set; }

        public MonthlyElementHelper()
        {
            Details = new List<MonthlyElementHelperDetails>();
            OPDetails = new List<MonthlyOPHelperDetails>();
            LEDetails = new List<MonthlyLEHelperDetails>();
        }
    }

    public class MonthlyOPWithLEHelperDetails
    {
        public int Year { get; set; }
        public double Value { get; set; }


        public string WellName { get; set; }
        public string RigName { get; set; }
        public string ActivityType { get; set; }
        public string SequenceId { get; set; }

        public DateRange oPhSchedule { get; set; }
        public DateRange PhSchedule { get; set; }
        public double NumOfDayinThisYear { get; set; }

        public DateRange oLESchedule { get; set; }
        public DateRange LESchedule { get; set; }

        public WellDrillData oOP { get; set; }
        public WellDrillData OP { get; set; }

        public WellDrillData oLE { get; set; }
        public WellDrillData LE { get; set; }

        public WellDrillData OPwithLE { get; set; }


        public MonthlyOPWithLEHelperDetails()
        {
            oOP = new WellDrillData();
            OP = new WellDrillData();
            oLE = new WellDrillData();
            LE = new WellDrillData();

            OPwithLE = new WellDrillData();
        }


    }

    public class MonthlyLEHelper
    {
        public double Days { get; set; }
        public double Cost { get; set; }
        public List<MonthlyLEHelperDetails> LEDetails = new List<MonthlyLEHelperDetails>();
        public MonthlyLEHelper()
        {
            LEDetails = new List<MonthlyLEHelperDetails>();
        }
    }
    public class MonthlyOPHelper
    {
        public double Days { get; set; }
        public double Cost { get; set; }
        public List<MonthlyOPHelperDetails> OPDetails = new List<MonthlyOPHelperDetails>();
        public MonthlyOPHelper()
        {
            OPDetails = new List<MonthlyOPHelperDetails>();
        }
    }

    public class MonthlyOPHelperDetails
    {
        public int Year { get; set; }
        public double Value { get; set; }


        public string WellName { get; set; }
        public string RigName { get; set; }
        public string ActivityType { get; set; }
        public string SequenceId { get; set; }

        public DateRange oPhSchedule { get; set; }
        public DateRange PhSchedule { get; set; }
        public double NumOfDayinThisYear { get; set; }

        public WellDrillData oOP { get; set; }
        public WellDrillData OP { get; set; }

        public MonthlyOPHelperDetails()
        {
            oOP = new WellDrillData();
            OP = new WellDrillData();
        }


    }

    public class MonthlyLEHelperDetails
    {
        public int Year { get; set; }
        public double Value { get; set; }


        public string WellName { get; set; }
        public string RigName { get; set; }
        public string ActivityType { get; set; }
        public string SequenceId { get; set; }

        public DateRange oLESchedule { get; set; }
        public DateRange LESchedule { get; set; }
        public double NumOfDayinThisYear { get; set; }

        public WellDrillData oLE { get; set; }
        public WellDrillData LE { get; set; }

        public MonthlyLEHelperDetails()
        {
            oLE = new WellDrillData();
            LE = new WellDrillData();
        }


    }

    public class WaterfallMonthlyLEClassification
    {
        public string Title { set; get; }
        public double Realized { set; get; }
        public double Unrealized { set; get; }
    }

    public class WaterfallMonthlyLEStacked
    {
        public double OP { set; get; }
        public double OPwLE { set; get; }
        public double Gap { set; get; }
        public double LE { set; get; }
        public List<WaterfallMonthlyLEClassification> Classifications { set; get; }
    }

    public class MonthlyElementHelperDetails
    {
        public string PIPID { get; set; }
        public string Idea { get; set; }
        public int Year { get; set; }
        public double Value { get; set; }
        public object Completion { get; set; }
        public string Classified { get; set; }
        public string Title { get; set; }
        public int ItemId { get; set; }

        public string WellName { get; set; }
        public string RigName { get; set; }
        public string ActivityType { get; set; }
        public string SequenceId { get; set; }

        public DateRange oPeriod { get; set; }
        public DateRange Period { get; set; }
        public double NumOfDayinThisYear { get; set; }

        public double BankSaved { get; set; }

        public double oDaysPlanImprovement { get; set; }
        public double oDaysPlanRisk { get; set; }
        public double oDaysCurrentWeekImprovement { get; set; }
        public double oDaysCurrentWeekRisk { get; set; }
        public double oCostPlanImprovement { get; set; }
        public double oCostPlanRisk { get; set; }
        public double oCostCurrentWeekImprovement { get; set; }
        public double oCostCurrentWeekRisk { get; set; }

        public double DaysPlanImprovement { get; set; }
        public double DaysPlanRisk { get; set; }
        public double DaysCurrentWeekImprovement { get; set; }
        public double DaysCurrentWeekRisk { get; set; }
        public double CostPlanImprovement { get; set; }
        public double CostPlanRisk { get; set; }
        public double CostCurrentWeekImprovement { get; set; }
        public double CostCurrentWeekRisk { get; set; }

        //public bool isIncludeLe { get; set; }

    }
    #endregion

    public class Color
    {
        public string key { get; set; }
        public string color { get; set; }
    }
}