using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;

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
using System.Globalization;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class MonthlyReportNewController : Controller
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
            var financialCalendarLastActive = WEISFinancialCalendar.Get<WEISFinancialCalendar>(Query.EQ("Status", "Active"));
            if (financialCalendarLastActive != null)
            {
                ViewBag.StartDate = Tools.ToUTC(financialCalendarLastActive.MonthYear);
            }

            ViewBag.FullName = "";

            try
            {
                var user = DataHelper.Get<ECIS.Identity.IdentityUser>("Users", Query.EQ("UserName", WebTools.LoginUser.UserName));
                if (user != null)
                {
                    ViewBag.FullName = user.FullName;
                }
            }
            catch (Exception e)
            {

            }

            return View();
        }

        public ActionResult Report()
        {
            return View();
        }

        public ActionResult DashboardLE()
        {
            return View();
        }

        public JsonResult GetDataDashboardLE(bool onlyLE, string yearStart, string yearFinish, string basedOn, List<string> Projects = null)
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


        public JsonResult SaveNewPIP(string ActivityUpdateId, string Title, DateTime ActivityStart, DateTime ActivityEnd, double PlanDaysOpp, double PlanDaysRisk, double PlanCostOpp, double PlanCostRisk, string Classification, string PerformanceUnit, string ActionParty, List<WEISPersonInfo> ActionParties, string Theme, string Completion, bool isPositive)
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
                    Period = new DateRange(ActivityStart, ActivityEnd),
                    Classification = Classification,
                    ActionParty = ActionParty,
                    ActionParties = ActionParties,
                    PerformanceUnit = PerformanceUnit,
                    Completion = Completion,
                    Theme = Theme,
                    isNewElement = true,
                    isPositive = isPositive
                };
                wau.Elements.Add(pip);
                wau.Save();
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


        public List<WellActivityUpdateMonthly> MonthlyReportInitiate(DateTime StartDate, string StartComment, List<String> WellActivityIds, bool withSendMail = false, bool isFromInitiate = false)
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
                    variables.Add("WellName", wa.WellName);
                    variables.Add("ActivityType", phase.ActivityType);
                    variables.Add("UpdateVersion", String.Format("{0:dd-MMM-yyyy}", dt));
                    variables.Add("DueDate", String.Format("{0:dd-MMM-yyyy}", dt));
                    variables.Add("UrlAction", "http://" + Request.Url.Host + Url.Action("index", "weeklyreport"));
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
                        //wau.Phase.LE = new WellDrillData();
                        wau.UpdateVersion = StartDate;
                        wau.Save();

                        if (withSendMail)
                        {
                            Email.Send("WRInitiate",
                                persons.Select(d => d.Email).ToArray(),
                                ccs.Count == 0 ? null : ccs.Select(d => d.Email).ToArray(),
                                variables: variables,
                                developerModeEmail: WebTools.LoginUser.Email);
                            waus.Add(wau);
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

            return waus;
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

        public JsonResult Search(string Date = "", List<string> Regions = null, List<string> OperatingUnits = null, List<string> RigTypes = null, List<string> RigNames = null, List<string> Projects = null, List<string> WellNames = null, List<string> Activities = null, List<string> PerformanceUnits = null, string Status = null, string OPType = "All", bool IncludeDiffOfLEAndCalcLE = true, bool IncludeNotEnteredLE = true, string DateStart = "", string DateStart2 = "", string DateFinish = "", string DateFinish2 = "", string DateRelation = "OR")
        {
            var qa = new List<IMongoQuery>();
            if (Regions != null) qa.Add(Query.In("Region", new BsonArray(Regions)));
            if (OperatingUnits != null) qa.Add(Query.In("OperatingUnit", new BsonArray(OperatingUnits)));
            if (RigTypes != null) qa.Add(Query.In("RigType", new BsonArray(RigTypes)));
            if (RigNames != null) qa.Add(Query.In("RigName", new BsonArray(RigNames)));
            if (Projects != null) qa.Add(Query.In("ProjectName", new BsonArray(Projects)));

            if (qa.Any())
            {
                var innerWellNames = WellActivity.Populate<WellActivity>(Query.And(qa)).Select(d => d.WellName).Distinct().ToList();
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
            List<WellActivity> wa = WellActivity.Populate<WellActivity>(qWa);
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
            if (WellNames != null) qs.Add(Query.In("WellName", new BsonArray(WellNames)));
            if (Activities != null) qs.Add(Query.In("Phase.ActivityType", new BsonArray(Activities)));
            if (PerformanceUnits != null) qs.Add(Query.Or(
                Query.In("Elements.PerformanceUnit", new BsonArray(PerformanceUnits)),
                Query.In("CRElements.PerformanceUnit", new BsonArray(PerformanceUnits))
            ));
            if (Status != null && Status != "") qs.Add(Query.EQ("Status", Status));
            if (qs.Count > 0) q = Query.And(qs);
            //List<WellActivityUpdateMonthly> was1 = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q);

            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var dashboardC = new DashboardController();

                List<WellActivityUpdateMonthly> was = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q)
                    .Where(d =>
                    {
                        d.OpsSchedule = new DateRange();

                        var isIncludeDiffOfLEAndCalcLE = true;
                        var isIncludeNotEnteredLE = true;
                        var isLE = false;

                        var waOfWaum = WellActivity.Get<WellActivity>(Query.And(
                            Query.EQ("UARigSequenceId", d.SequenceId),
                            Query.EQ("Phases.ActivityType", d.Phase.ActivityType),
                            Query.EQ("WellName", d.WellName)
                        ));

                        if (waOfWaum != null)
                        {
                            var phOfWaum = waOfWaum.Phases.FirstOrDefault(e => d.Phase.ActivityType.Equals(e.ActivityType));
                            if (phOfWaum != null)
                            {
                                isLE = dashboardC.dateBetween(phOfWaum.PhSchedule, DateStart, DateFinish, DateStart2, DateFinish2, DateRelation);
                                d.OpsSchedule = phOfWaum.PhSchedule;
                            }
                        }

                        var leDays = d.CurrentWeek.Days;
                        var leCost = d.CurrentWeek.Cost / 1000000;
                        var cleDays = d.CalculatedLE.Days;
                        var cleCost = d.CalculatedLE.Cost;

                        var diffDays = Math.Abs(leDays - cleDays);
                        var diffCost = Math.Abs(leCost - cleCost);

                        var isDaysSame = diffDays <= 0.005;
                        var isCostSame = diffCost <= 0.005;

                        if (!IncludeDiffOfLEAndCalcLE)
                        {
                            if (!isDaysSame || !isCostSame)
                            {
                                isIncludeDiffOfLEAndCalcLE = false;
                            }
                        }

                        if (!IncludeNotEnteredLE)
                        {
                            isIncludeNotEnteredLE = d.Phase.IsActualLE;
                        }

                        return isLE && isIncludeDiffOfLEAndCalcLE && isIncludeNotEnteredLE;
                    })
                    .OrderBy(x => x.WellName).ToList();
                return was;
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

        public List<MonthlyEventsBasedOnMonth> GetActivitiesBasedOnMonth(DateTime y)
        {

            IMongoQuery q1 = Query.GT("Phases.PhSchedule.Start", Tools.ToUTC(y));
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

        public JsonResult Select(string id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                WellActivityUpdateMonthly raw = GetUpdateData(id);
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

                var totalRealizedCost = PIPElements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.CostCurrentWeekImprovement) +
                    PIPElements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.CostCurrentWeekRisk);

                var totalRealizedDays = PIPElements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.DaysCurrentWeekImprovement) +
                    PIPElements.Where(x => x.Completion.ToString().Equals("Realized")).Sum(x => x.DaysCurrentWeekRisk);

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

                if (wa != null)
                {
                    rigName = wa.RigName;

                    var ph = wa.Phases.FirstOrDefault(e => e.ActivityType.Equals(raw.Phase.ActivityType));
                    if (ph != null)
                    {
                        raw.Phase.IsActualLE = ph.IsActualLE;
                    }
                }

                return new
                {
                    totalRealizedCost = totalRealizedCost,
                    totalRealizedDays = totalRealizedDays,
                    Record = raw,
                    HasEDM = act != null,
                    RigName = rigName
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
                            pip.Save();

                            WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email, LogType.UpdateOnDelete,
                                pip.TableName, url, pip.ToBsonDocument(), temppip.ToBsonDocument());

                            //Delete Comments
                            new CommentController().DeleteComment("WeeklyReport", ActivityUpdateId, ElementId);

                            return Json(new { Success = true, Note = "Element in PIP also changed" }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    return Json(new { Success = true, Note = "Only element in WAU changed" }, JsonRequestBehavior.AllowGet);
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
                                catch (Exception e) { }
                                try
                                {
                                    result.CRUnrealised = elementsReduction
                                        .Where(f => !f.Completion.Equals("Realized"))
                                        .DefaultIfEmpty(new PIPElement())
                                        .Sum(f => GetDayOrCost(f, DayOrCost));
                                }
                                catch (Exception e) { }
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

        public JsonResult Save(WellActivityUpdateMonthly model)
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
                        Element.Period = pe.Period;
                        ListElement.Add(Element);
                    }
                    else
                    {
                        ListElement.Add(x);
                    }
                }
                NewWau.Elements = ListElement;

                NewWau.Save(references: new string[] { "", "CalcLeSchedule", "updateLeStatus" });

                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                    LogType.Update, NewWau.TableName, url, temp.ToBsonDocument(), NewWau.ToBsonDocument());

                //string url = (HttpContext.Request).Path.ToString();
                //WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                //    LogType.Insert, NewWau.TableName, url, NewWau.ToBsonDocument(), null);

                return NewWau;
            });

        }
        public ActionResult PrintDocument(string id, string WellName)
        {
            ViewBag.Id = id;
            ViewBag.WellName = WellName;
            return View();
        }

        public FileResult Print2Pdf(string id)
        {
            var newFileName = Export2Pdf(id);

            #region Update by Yoga

            return File(newFileName, Tools.GetContentType(".pdf"), Path.GetFileName(newFileName));

            #endregion
            //return File(newFileName, Tools.GetContentType(".pdf"), newFileName + ".pdf");
        }

        public string Export2Pdf(string id)
        {
            return Export2Pdf(new string[] { id });
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

        public string Export2Pdf(string[] ids)
        {
            ///--- change this to
            /// open 2 template, and create 2 ws object from each template
            /// create new excel file --- WR_{0}.pdf, where 0 is Date of Report in format ddMMMyyyy
            /// iterate for each id
            /// copy respective template into new worksheet based on wr for respective and populate the data accordingly
            /// save the file
            /// return the filename
            //string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "wrtemplate.xlsx");
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "wrtemplateDefault.xlsx");
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
                    wb = new Workbook(xlst);
                    var ws = wb.Worksheets[0];
                    WellActivityPhase a = wau.GetWellActivity();
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
                       String.Format("WR-{0}-{1}.pdf", idx, DateTime.Now.ToString("dd-MMM-yyyy")));

                    ws.Cells["D7"].Value = wau.Company;
                    ws.Cells["D8"].Value = wau.Project;
                    ws.Cells["D9"].Value = wau.Site;
                    ws.Cells["D10"].Value = wau.WellName;
                    ws.Cells["D11"].Value = wau.WellType;
                    ws.Cells["D12"].Value = wau.GetWellActivity().ActivityType; //wau.EventType;
                    ws.Cells["D13"].Value = wau.Objective;
                    ws.Cells["D14"].Value = a.PhSchedule.Start;
                    ws.Cells["I7"].Value = wau.Contractor;
                    ws.Cells["I8"].Value = wau.WorkUnit;
                    ws.Cells["I9"].Value = wau.RigSuperintendent;
                    ws.Cells["I10"].Value = wau.WellEngineer;
                    ws.Cells["I11"].Value = wau.OriginalSpudDate;
                    ws.Cells["I12"].Value = wau.OriginalSpudDate > Tools.DefaultDate ? (wau.UpdateVersion - wau.OriginalSpudDate).TotalDays : 0;
                    ws.Cells["I13"].Value = wau.Actual.Days;
                    ws.Cells["I14"].Value = wau.OP.Days;


                    ws.Cells["B3"].Value = String.Format("WELL {0}", wau.WellName);
                    ws.Cells["B4"].Value = String.Format("Date: {0:dd-MMMM-yyyy}", wau.UpdateVersion);


                    //ws.Cells["B17"].Value = wau.ExecutiveSummary == null ? "" : wau.ExecutiveSummary.Replace("\n", " ");
                    //ws.Cells["B26"].Value = wau.OperationSummary == null ? "" : wau.OperationSummary.Replace("\n", " "); // Last 7 Day Summary 
                    //ws.Cells["B42"].Value = wau.PlannedOperation == null ? "" : wau.PlannedOperation.Replace("\n", " "); // Planned Summary

                    ws.Cells["B17"].Value = wau.ExecutiveSummary;
                    ws.Cells["B19"].Value = wau.OperationSummary; // Last 7 Day Summary 
                    ws.Cells["B21"].Value = wau.PlannedOperation; // Planned Summary
                    ws.Cells["B23"].Value = wau.SupplementReason; // Supplement Reason

                    if (!wau.SupplementLast7Days)
                    {
                        ws.Cells.HideRow(21);
                        ws.Cells.HideRow(22);
                    }

                    ws.Cells["C26"].Value = Tools.Div(wau.AFE.Cost, 1000000);
                    ws.Cells["H26"].Value = wau.AFE.Days;
                    ws.Cells["C27"].Value = Tools.Div(wau.Actual.Cost, 1000000);
                    ws.Cells["H27"].Value = wau.Actual.Days;
                    ws.Cells["C28"].Value = Tools.Div(wau.CurrentWeek.Cost, 1000000);
                    ws.Cells["H28"].Value = wau.CurrentWeek.Days;
                    // OP
                    ws.Cells["C29"].Value = Tools.Div(wau.Plan.Cost, 1000000);
                    ws.Cells["H29"].Value = wau.Plan.Days;

                    // NPT
                    double npts = 0;
                    if (wau.Actual.Days > 0)
                        npts = wau.NPT.Days * wau.Actual.Days * 24;
                    else
                        npts = 0;

                    // NPT Hours
                    ws.Cells["C30"].Value = Math.Round(npts, 1); // +(" (hours)");

                    // % NPT
                    ws.Cells["H30"].Value = Math.Round((wau.NPT.Days * 100), 1); // +(" %");

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

            if (workbooks.Count > 1)
            {
                workbooks[0].Save(newFileName, Aspose.Cells.SaveFormat.Pdf);
                return newFileName;
            }
            else
            {
                workbooks[0].Save(newFileNameSingle, Aspose.Cells.SaveFormat.Pdf);
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


        public double getLEPIPClass(List<WellPIP> PIPs, string Classification, string DaysorCost)
        {
            var PIPElements = PIPs.SelectMany(x => x.Elements, (x, e) => new
            {
                Elements = e
            });
            var ret = PIPElements.Where(x => null != x.Elements.Classification && x.Elements.Classification.ToLower().Equals(Classification.ToLower()) && x.Elements.Completion.Equals("Realized")).Any() ?
                    PIPElements.Where(x => null != x.Elements.Classification && x.Elements.Classification.ToLower().Equals(Classification.ToLower()) && x.Elements.Completion.Equals("Realized"))
                    .Sum(x => DaysorCost.ToLower().Equals("cost") ? x.Elements.CostCurrentWeekImprovement + x.Elements.CostCurrentWeekRisk : x.Elements.DaysCurrentWeekImprovement + x.Elements.DaysCurrentWeekRisk) : 0.0;
            return ret;
        }

        public JsonResult GetData(WaterfallBase wb)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                
                WaterfallReportController wrc = new WaterfallReportController();

                if (wb.firmoption.Equals("notall"))
                {
                    throw new Exception("Please select either Firm, Option or Blank to display data");
                }

                if (wb.WellNames == null || !wb.WellNames.Any())
                {
                    if (wrc.GetWellListBasedOnEmail().Any(x => x != "*"))
                    {
                        wb.WellNames = wrc.GetWellListBasedOnEmail();
                    }
                }

                var wbForOP = wb;
                //wbForOP.GetWhatData = "OP";
                var waForOP = wbForOP.GetActivitiesAndPIPs();

                var wa = wb.GetActivitiesAndPIPs();

                var result = new List<WaterfallMonthlyLE>();

                var uwPhase = wa.Activities.SelectMany(x => x.Phases, (x, e) => new
                {
                    PhSchedule = e.PhSchedule
                }).ToList();

                if (!uwPhase.Any())
                {
                    throw new Exception("Monthly LE Data for this month is not initiated.");
                }

                var PhaseMin = uwPhase.Min(x => x.PhSchedule.Start);
                var PhaseMinAfterDefaultDate = new DateTime();
                if (PhaseMin == Tools.DefaultDate)
                {
                    PhaseMinAfterDefaultDate = uwPhase.Where(x => x.PhSchedule.Start > Tools.DefaultDate).Count() > 0 ? uwPhase.Where(x => x.PhSchedule.Start > Tools.DefaultDate).Min(x => x.PhSchedule.Start) : Tools.DefaultDate;
                }
                else
                {
                    PhaseMinAfterDefaultDate = PhaseMin;
                }
                var PhaseMax = uwPhase.Max(x => x.PhSchedule.Finish);

                var PIPElements = wa.PIPs.SelectMany(x => x.Elements, (x, e) => new
                {
                    Elements = e
                });

                var TotalLEClass1 = getLEPIPClass(wa.PIPs, "Efficient Execution", wb.DayOrCost);
                var TotalLEClass2 = getLEPIPClass(wa.PIPs, "Competitive Scope", wb.DayOrCost);
                var TotalLEClass3 = getLEPIPClass(wa.PIPs, "Supply Chain Transformation", wb.DayOrCost);
                var TotalLEClass4 = getLEPIPClass(wa.PIPs, "Technology and Innovation", wb.DayOrCost);

                var TotalLEClass5 = PIPElements.Where(x => null != x.Elements.Classification && x.Elements.Classification.ToLower() != "Efficient Execution".ToLower()
                                    && x.Elements.Classification.ToLower() != "Competitive Scope".ToLower()
                                    && x.Elements.Classification.ToLower() != "Supply Chain Transformation".ToLower()
                                    && x.Elements.Classification.ToLower() != "Technology and Innovation".ToLower()
                                    && x.Elements.Completion.Equals("Realized")).Any() ?
                                    PIPElements.Where(x => null != x.Elements.Classification && x.Elements.Classification.ToLower() != "Efficient Execution".ToLower()
                                    && x.Elements.Classification.ToLower() != "Competitive Scope".ToLower()
                                    && x.Elements.Classification.ToLower() != "Supply Chain Transformation".ToLower()
                                    && x.Elements.Classification.ToLower() != "Technology and Innovation".ToLower()
                                    && x.Elements.Completion.Equals("Realized"))
                                    .Sum(x => wb.DayOrCost.ToLower().Equals("cost") ? x.Elements.CostCurrentWeekImprovement + x.Elements.CostCurrentWeekRisk : x.Elements.CostCurrentWeekImprovement + x.Elements.CostCurrentWeekRisk) : 0.0;

                var banked1 = 0.0;
                var banked2 = 0.0;
                var banked3 = 0.0;
                var banked4 = 0.0;

                foreach (var a in wa.Activities)
                {
                    if (a.Phases != null && a.Phases.Count > 0)
                    {
                        foreach (var aph in a.Phases)
                        {
                            var getWAUM = WellActivityUpdateMonthly.GetById(a.WellName, a.UARigSequenceId, aph.PhaseNo, null, true);
                            if (getWAUM != null)
                            {
                                if (wb.ShellShare)
                                {

                                    banked1 += wb.DayOrCost.ToLower().Equals("cost") ? (getWAUM.BankedSavingsEfficientExecution.Cost * ( a.WorkingInterest > 1 ? Tools.Div(a.WorkingInterest,100 ) : a.WorkingInterest) ) : getWAUM.BankedSavingsEfficientExecution.Days;
                                    banked2 += wb.DayOrCost.ToLower().Equals("cost") ? (getWAUM.BankedSavingsCompetitiveScope.Cost * (a.WorkingInterest > 1 ? Tools.Div(a.WorkingInterest, 100) : a.WorkingInterest)) : getWAUM.BankedSavingsCompetitiveScope.Days;
                                    banked3 += wb.DayOrCost.ToLower().Equals("cost") ? (getWAUM.BankedSavingsSupplyChainTransformation.Cost * (a.WorkingInterest > 1 ? Tools.Div(a.WorkingInterest, 100) : a.WorkingInterest)) : getWAUM.BankedSavingsSupplyChainTransformation.Days;
                                    banked4 += wb.DayOrCost.ToLower().Equals("cost") ? (getWAUM.BankedSavingsTechnologyAndInnovation.Cost * (a.WorkingInterest > 1 ? Tools.Div(a.WorkingInterest, 100) : a.WorkingInterest)) : getWAUM.BankedSavingsTechnologyAndInnovation.Days;

                                }
                                else
                                {
                                    banked1 += wb.DayOrCost.ToLower().Equals("cost") ? getWAUM.BankedSavingsEfficientExecution.Cost : getWAUM.BankedSavingsEfficientExecution.Days;
                                    banked2 += wb.DayOrCost.ToLower().Equals("cost") ? getWAUM.BankedSavingsCompetitiveScope.Cost : getWAUM.BankedSavingsCompetitiveScope.Days;
                                    banked3 += wb.DayOrCost.ToLower().Equals("cost") ? getWAUM.BankedSavingsSupplyChainTransformation.Cost : getWAUM.BankedSavingsSupplyChainTransformation.Days;
                                    banked4 += wb.DayOrCost.ToLower().Equals("cost") ? getWAUM.BankedSavingsTechnologyAndInnovation.Cost : getWAUM.BankedSavingsTechnologyAndInnovation.Days;

                                }
                            }
                        }
                    }
                }

                var TotalElement1 = TotalLEClass1 + banked1;
                var TotalElement2 = TotalLEClass2 + banked2;
                var TotalElement3 = TotalLEClass3 + banked3;
                var TotalElement4 = TotalLEClass4 + banked4;
                var TotalElement5 = TotalLEClass5;

                var GAP = wa.TotalLE - (waForOP.TotalOP + TotalElement1 + TotalElement2 + TotalElement3 + TotalElement4 + TotalElement5);

                result.Add(new WaterfallMonthlyLE() { title = BaseOPTitle(wb.BaseOP), value = waForOP.TotalOP });
                result.Add(new WaterfallMonthlyLE() { title = "Efficient Execution", value = TotalElement1, valueOP = 0, summary = null });
                result.Add(new WaterfallMonthlyLE() { title = "Competitive Scope", value = TotalElement2 });
                result.Add(new WaterfallMonthlyLE() { title = "Supply Chain Transformation", value = TotalElement3 });
                result.Add(new WaterfallMonthlyLE() { title = "Technology and Innovation", value = TotalElement4 });

                if (TotalLEClass5 > 0)
                    result.Add(new WaterfallMonthlyLE() { title = "All Others", value = TotalLEClass5 });
                result.Add(new WaterfallMonthlyLE() { title = "Accelerated Scope", value = GAP });
                result.Add(new WaterfallMonthlyLE() { title = "LE", value = wa.TotalLE });

                string DefaultOP = "OP15";
                DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
                var getGridData = new List<WaterFallHelper>();
                if (wb.BaseOP != DefaultOP)
                {
                    getGridData = GetValueWaterFall(wa.Activities, wa.PIPs, wb.BaseOP, wb);
                }
                else
                {
                    getGridData = GetValueWaterFallForDefaultOP(waForOP.Activities, waForOP.PIPs, wb.BaseOP, wb);
                }
                var DetailGridData = new List<BsonDocument>();

                foreach (var a in getGridData)
                {
                    var yearsOP = a.Values.GroupBy(x => x.Year).Select(x => x.Key).ToList();
                    var yearsPIP = a.PIPValues.GroupBy(x => x.Year).Select(x => x.Key).ToList();
                    var yearsAll = new List<Int32>();
                    yearsAll.AddRange(yearsOP);
                    yearsAll.AddRange(yearsPIP);
                    var years = yearsAll.Distinct().ToList();

                    var BankedSavingsSupplyChainTransformationCost = 0.0;
                    var BankedSavingsSupplyChainTransformationDays = 0.0;
                    var BankedSavingsCompetitiveScopeCost = 0.0;
                    var BankedSavingsCompetitiveScopeDays = 0.0;
                    var BankedSavingsTechnologyAndInnovationCost = 0.0;
                    var BankedSavingsTechnologyAndInnovationDays = 0.0;
                    var BankedSavingsEfficientExecutionCost = 0.0;
                    var BankedSavingsEfficientExecutionDays = 0.0;

                    var getWAUM = WellActivityUpdateMonthly.GetById(a.WellName, a.SequenceID, a.PhaseNo, null, true);
                    if (getWAUM != null)
                    {
                        BankedSavingsSupplyChainTransformationCost = Tools.Div(getWAUM.BankedSavingsSupplyChainTransformation.Cost, years.Count);
                        BankedSavingsSupplyChainTransformationDays = Tools.Div(getWAUM.BankedSavingsSupplyChainTransformation.Days, years.Count);
                        BankedSavingsCompetitiveScopeCost = Tools.Div(getWAUM.BankedSavingsCompetitiveScope.Cost, years.Count);
                        BankedSavingsCompetitiveScopeDays = Tools.Div(getWAUM.BankedSavingsCompetitiveScope.Days, years.Count);
                        BankedSavingsTechnologyAndInnovationCost = Tools.Div(getWAUM.BankedSavingsTechnologyAndInnovation.Cost, years.Count);
                        BankedSavingsTechnologyAndInnovationDays = Tools.Div(getWAUM.BankedSavingsTechnologyAndInnovation.Days, years.Count);
                        BankedSavingsEfficientExecutionCost = Tools.Div(getWAUM.BankedSavingsEfficientExecution.Cost, years.Count);
                        BankedSavingsEfficientExecutionDays = Tools.Div(getWAUM.BankedSavingsEfficientExecution.Days, years.Count);
                    }

                    foreach (var y in years)
                    {
                        var getOPCost = Tools.Div(a.Values.Where(x => x.Year.Equals(y) && x.Identity.Equals("OP")).Sum(x => x.Value.Cost), 1000000);
                        var getOPDays = a.Values.Where(x => x.Year.Equals(y) && x.Identity.Equals("OP")).Sum(x => x.Value.Days);
                        var getLECost = Tools.Div(a.Values.Where(x => x.Year.Equals(y) && x.Identity.Equals("LE")).Sum(x => x.Value.Cost), 1000000);
                        var getLEDays = a.Values.Where(x => x.Year.Equals(y) && x.Identity.Equals("LE")).Sum(x => x.Value.Days);
                        var WellName = a.WellName;
                        var ActivityType = a.ActivityType;

                        var Eff_Cost = Tools.Div(a.PIPValues.Where(x => x.Year.Equals(y) && x.PIPClassification.Equals("Efficient Execution")).Sum(x => x.Value.Cost), 1) + BankedSavingsEfficientExecutionCost;
                        var Eff_Days = a.PIPValues.Where(x => x.Year.Equals(y) && x.PIPClassification.Equals("Efficient Execution")).Sum(x => x.Value.Days) + BankedSavingsEfficientExecutionDays;
                        var Comp_Cost = Tools.Div(a.PIPValues.Where(x => x.Year.Equals(y) && x.PIPClassification.Equals("Competitive Scope")).Sum(x => x.Value.Cost), 1) + BankedSavingsCompetitiveScopeCost;
                        var Comp_Days = a.PIPValues.Where(x => x.Year.Equals(y) && x.PIPClassification.Equals("Competitive Scope")).Sum(x => x.Value.Days) + BankedSavingsCompetitiveScopeDays;
                        var SCT_Cost = Tools.Div(a.PIPValues.Where(x => x.Year.Equals(y) && x.PIPClassification.Equals("Supply Chain Transformation")).Sum(x => x.Value.Cost), 1) + BankedSavingsSupplyChainTransformationCost;
                        var SCT_Days = a.PIPValues.Where(x => x.Year.Equals(y) && x.PIPClassification.Equals("Supply Chain Transformation")).Sum(x => x.Value.Days) + BankedSavingsSupplyChainTransformationDays;
                        var TI_Days = a.PIPValues.Where(x => x.Year.Equals(y) && x.PIPClassification.ToLower().Equals("technology and innovation")).Sum(x => x.Value.Days) + BankedSavingsTechnologyAndInnovationDays;
                        var TI_Cost = Tools.Div(a.PIPValues.Where(x => x.Year.Equals(y) && x.PIPClassification.ToLower().Equals("technology and innovation")).Sum(x => x.Value.Cost), 1) + BankedSavingsTechnologyAndInnovationCost;

                        var bdoc = new BsonDocument();
                        bdoc.Set("Year", Convert.ToInt16(y));
                        bdoc.Set("WellName", WellName);
                        bdoc.Set("ActivityType", ActivityType);
                        bdoc.Set("OP", wb.DayOrCost.ToLower().Equals("cost") ? getOPCost : getOPDays);
                        bdoc.Set("EfficientExecution", wb.DayOrCost.ToLower().Equals("cost") ? Eff_Cost : Eff_Days);
                        bdoc.Set("CompetitiveScope", wb.DayOrCost.ToLower().Equals("cost") ? Comp_Cost : Comp_Days);
                        bdoc.Set("SupplyChainTransformation", wb.DayOrCost.ToLower().Equals("cost") ? SCT_Cost : SCT_Days);
                        bdoc.Set("TechnologyAndInnovation", wb.DayOrCost.ToLower().Equals("cost") ? TI_Cost : TI_Days);
                        bdoc.Set("LE", wb.DayOrCost.ToLower().Equals("cost") ? getLECost : getLEDays);
                        DetailGridData.Add(bdoc);
                    }
                }

                var DataFiscalYear = DetailGridData.GroupBy(x => BsonHelper.GetInt16(x, "Year")).Select(x => new
                {
                    Year = x.Key,
                    OP = x.Sum(d => BsonHelper.GetDouble(d, "OP")),
                    Efficient_Execution = x.Sum(d => BsonHelper.GetDouble(d, "EfficientExecution")),
                    Competitive_Scope = x.Sum(d => BsonHelper.GetDouble(d, "CompetitiveScope")),
                    Supply_Chain_Transformation = x.Sum(d => BsonHelper.GetDouble(d, "SupplyChainTransformation")),
                    Technology_and_Innovation = x.Sum(d => BsonHelper.GetDouble(d, "TechnologyAndInnovation")),
                    LE = x.Sum(d => BsonHelper.GetDouble(d, "LE"))
                });

                DataFiscalYear = DataFiscalYear.ToList().OrderByDescending(x => BsonHelper.GetInt16(x.ToBsonDocument(), "Year")).ToList();
                DetailGridData = DetailGridData.ToList().OrderByDescending(x => BsonHelper.GetInt16(x, "Year")).ThenBy(x => BsonHelper.GetString(x, "WellName")).ToList();
                return new { Chart = result, DetailGrid = DataHelper.ToDictionaryArray(DetailGridData), DataFiscalYear };


                //return new { Chart = result };
            });
        }

        public List<WaterFallHelper> GetValueWaterFall(List<WellActivity> activities, List<WellPIP> pips, string BaseOP, WaterfallBase wb)
        {
            string DefaultOP = "OP15";
            DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
            List<WaterFallHelper> results = new List<WaterFallHelper>();
            foreach (var actv in activities)
            {
                if (actv.Phases.Any())
                {
                    foreach (var p in actv.Phases)
                    {
                        // find history
                        var availHis = actv.OPHistories.Where(x => x.ActivityType.Equals(p.ActivityType) && x.PhaseNo.Equals(p.PhaseNo) && x.PlanSchedule.Start != Tools.DefaultDate);
                        if (availHis.Any())
                        {
                            var his = availHis.FirstOrDefault();

                            WaterFallHelper helper = new WaterFallHelper();
                            helper.OP = his.Plan;
                            helper.LE = his.LE;
                            helper.BaseOP = BaseOP;
                            helper.OPSchedule = his.PlanSchedule;
                            helper.LESchedule = his.LESchedule;
                            helper.WellName = his.WellName;
                            helper.ActivityType = his.ActivityType;
                            helper.SequenceID = his.UARigSequenceId;
                            helper.PhaseNo = his.PhaseNo;
                            List<WaterFallHelplerValue> values = new List<WaterFallHelplerValue>();

                            var planschedule = his.PlanSchedule;
                            var yresults = DateRangeToMonth.SplitedDateRangeYearly(planschedule);
                            // Plan
                            foreach (var res in yresults)
                            {
                                WaterFallHelplerValue val = new WaterFallHelplerValue();
                                val.Identity = "OP";
                                val.NoOfDay = res.NoOfDay;
                                val.SplitedDate = res.SplitedDateRange;
                                val.Year = res.Year;
                                val.Proportion = res.Proportion;
                                val.Value.Cost = res.Proportion * his.Plan.Cost;
                                val.Value.Days = res.Proportion * his.Plan.Days;
                                helper.Values.Add(val);
                            }

                            var leschedule = his.LESchedule;
                            var leresults = DateRangeToMonth.SplitedDateRangeYearly(leschedule);
                            // LE
                            foreach (var res in leresults)
                            {
                                WaterFallHelplerValue val = new WaterFallHelplerValue();
                                val.Identity = "LE";
                                val.NoOfDay = res.NoOfDay;
                                val.SplitedDate = res.SplitedDateRange;
                                val.Year = res.Year;
                                val.Proportion = res.Proportion;
                                var wa = new WellActivity();
                                if (wa.isHaveWeeklyReport2(his.WellName, his.UARigSequenceId, his.ActivityType))
                                {
                                    if (his.ActivityType.ToLower().Contains("risk"))
                                    {
                                        val.Value.Cost = 0.0;
                                        val.Value.Days = 0.0;
                                    }
                                    else
                                    {
                                        //var ratioLE = wb.GetRatio(his.LESchedule);
                                        val.Value.Cost = res.Proportion * his.LE.Cost;
                                        val.Value.Days = res.Proportion * his.LE.Days;
                                    }
                                }
                                else
                                {
                                    val.Value.Cost = 0.0;
                                    val.Value.Days = 0.0;
                                }

                                helper.Values.Add(val);
                            }
                            //results.Add(helper);

                            var isHavePIP = pips.Where(x => x.WellName.Equals(helper.WellName) &&
                                x.ActivityType.Equals(helper.ActivityType) &&
                                x.SequenceId.Equals(helper.SequenceID) &&
                                x.PhaseNo.Equals(helper.PhaseNo));
                            if (isHavePIP.Any())
                            {
                                var firstPIP = isHavePIP.FirstOrDefault();
                                foreach (var elem in firstPIP.Elements.Where(x => x.Completion.Equals("Realized")))
                                {
                                    var elemenperiod = elem.Period;
                                    var elemenresult = DateRangeToMonth.SplitedDateRangeYearly(elemenperiod);
                                    var pipLERealDays = elem.DaysCurrentWeekImprovement + elem.DaysCurrentWeekRisk;
                                    var pipLERealCost = elem.CostCurrentWeekImprovement + elem.CostCurrentWeekRisk;

                                    // PIP element
                                    foreach (var res in elemenresult)
                                    {
                                        WaterFallHelplerValue val = new WaterFallHelplerValue();
                                        val.Identity = "PIP";
                                        val.PIPIdea = elem.Title;
                                        val.PIPClassification = elem.Classification;
                                        val.NoOfDay = res.NoOfDay;
                                        val.SplitedDate = res.SplitedDateRange;
                                        val.Year = res.Year;
                                        val.Proportion = res.Proportion;
                                        val.Value.Cost = res.Proportion * pipLERealCost;
                                        val.Value.Days = res.Proportion * pipLERealDays;
                                        helper.PIPValues.Add(val);
                                    }
                                }
                            }
                            results.Add(helper);
                        }
                    }
                }
            }

            return results;
        }

        public List<WaterFallHelper> GetValueWaterFallForDefaultOP(List<WellActivity> activities, List<WellPIP> pips, string BaseOP, WaterfallBase wb)
        {
            string DefaultOP = "OP15";
            DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
            List<WaterFallHelper> results = new List<WaterFallHelper>();
            foreach (var actv in activities)
            {
                if (actv.Phases.Any())
                {
                    foreach (var p in actv.Phases)
                    {
                        // find history
                        //var availHis = actv.OPHistories.Where(x => x.ActivityType.Equals(p.ActivityType) && x.PhaseNo.Equals(p.PhaseNo) && x.PlanSchedule.Start!= Tools.DefaultDate);
                        if (p.BaseOP.Contains(DefaultOP))
                        {
                            var his = p;

                            WaterFallHelper helper = new WaterFallHelper();
                            helper.OP = his.Plan;
                            helper.LE = his.LE;
                            helper.BaseOP = BaseOP;
                            helper.OPSchedule = his.PlanSchedule;
                            helper.LESchedule = his.LESchedule;
                            helper.WellName = actv.WellName;
                            helper.ActivityType = his.ActivityType;
                            helper.SequenceID = actv.UARigSequenceId;
                            helper.PhaseNo = his.PhaseNo;
                            List<WaterFallHelplerValue> values = new List<WaterFallHelplerValue>();

                            var planschedule = his.PlanSchedule;
                            var yresults = DateRangeToMonth.SplitedDateRangeYearly(planschedule);
                            // Plan
                            foreach (var res in yresults)
                            {
                                WaterFallHelplerValue val = new WaterFallHelplerValue();
                                val.Identity = "OP";
                                val.NoOfDay = res.NoOfDay;
                                val.SplitedDate = res.SplitedDateRange;
                                val.Year = res.Year;
                                val.Proportion = res.Proportion;

                                if (p.BaseOP.OrderByDescending(x=>x).FirstOrDefault().Equals(BaseOP))
                                {
                                    val.Value.Cost = res.Proportion * his.Plan.Cost;
                                    val.Value.Days = res.Proportion * his.Plan.Days;

                                }
                                else
                                {
                                    var welld = actv.OPHistories.FirstOrDefault(x => x.Type.Equals(BaseOP) && x.ActivityType.Equals(p.ActivityType));
                                    if(welld != null )
                                    {
                                        val.Value.Cost = res.Proportion * welld.Plan.Cost;
                                        val.Value.Days = res.Proportion * welld.Plan.Days;

                                    }


                                }

                                helper.Values.Add(val);
                            }

                            var leschedule = his.LESchedule;
                            var leresults = DateRangeToMonth.SplitedDateRangeYearly(leschedule);
                            // LE
                            foreach (var res in leresults)
                            {
                                WaterFallHelplerValue val = new WaterFallHelplerValue();
                                val.Identity = "LE";
                                val.NoOfDay = res.NoOfDay;
                                val.SplitedDate = res.SplitedDateRange;
                                val.Year = res.Year;
                                val.Proportion = res.Proportion;
                                var wa = new WellActivity();
                                if (his.ActivityType.ToLower().Contains("risk"))
                                {
                                    val.Value.Cost = 0.0;
                                    val.Value.Days = 0.0;
                                }
                                else
                                {
                                    //var ratioLE = wb.GetRatio(his.LESchedule);
                                    val.Value.Cost = res.Proportion * his.LE.Cost;
                                    val.Value.Days = res.Proportion * his.LE.Days;
                                }

                                helper.Values.Add(val);
                            }
                            //results.Add(helper);

                            var isHavePIP = pips.Where(x => x.WellName.Equals(helper.WellName) &&
                                x.ActivityType.Equals(helper.ActivityType) &&
                                x.SequenceId.Equals(helper.SequenceID) &&
                                x.PhaseNo.Equals(helper.PhaseNo));
                            if (isHavePIP.Any())
                            {
                                var firstPIP = isHavePIP.FirstOrDefault();
                                foreach (var elem in firstPIP.Elements.Where(x => x.Completion.Equals("Realized")))
                                {
                                    var elemenperiod = elem.Period;
                                    var elemenresult = DateRangeToMonth.SplitedDateRangeYearly(elemenperiod);
                                    var pipLERealDays = elem.DaysCurrentWeekImprovement + elem.DaysCurrentWeekRisk;
                                    var pipLERealCost = elem.CostCurrentWeekImprovement + elem.CostCurrentWeekRisk;

                                    // PIP element
                                    foreach (var res in elemenresult)
                                    {
                                        WaterFallHelplerValue val = new WaterFallHelplerValue();
                                        val.Identity = "PIP";
                                        val.PIPIdea = elem.Title;
                                        val.PIPClassification = elem.Classification;
                                        val.NoOfDay = res.NoOfDay;
                                        val.SplitedDate = res.SplitedDateRange;
                                        val.Year = res.Year;
                                        val.Proportion = res.Proportion;
                                        val.Value.Cost = res.Proportion * pipLERealCost;
                                        val.Value.Days = res.Proportion * pipLERealDays;
                                        helper.PIPValues.Add(val);
                                    }
                                }
                            }
                            results.Add(helper);
                        }
                    }
                }
            }

            return results;
        }

        public string BaseOPTitle(string BaseOP)
        {
            var builder = new StringBuilder();
            int count = 0;
            foreach (var c in BaseOP)
            {
                builder.Append(c);
                if ((++count) == 2)
                {
                    builder.Append('-');
                }
            }
            BaseOP = builder.ToString();
            return BaseOP;
        }

        public double getLEPIPClassForGrid(List<GridDetailView> getPropElement, string Classification, int yearStart)
        {
            var ret = getPropElement.Where(x => x.Year == yearStart && null != x.Classification && x.Classification.ToLower().Equals(Classification.ToLower()) && x.Completion.Equals("Realized"))
                .Sum(x => x.LE * x.Ratio);
            return ret;
        }

        public JsonResult GetDataGrid(WaterfallBase wb)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                string DefaultOP = "OP15";
                if (Config.GetConfigValue("BaseOPConfig") != null)
                {
                    DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
                }

                WaterfallReportController wrc = new WaterfallReportController();
                if (wb.WellNames == null || !wb.WellNames.Any())
                {
                    if (wrc.GetWellListBasedOnEmail().Any(x => x != "*"))
                    {
                        wb.WellNames = wrc.GetWellListBasedOnEmail();
                    }
                }
                var wbForOP = wb;
                wbForOP.GetWhatData = "OP";
                var waForOP = wbForOP.GetActivitiesAndPIPs();

                var wa = wb.GetActivitiesAndPIPs();

                var uwPhase = wa.Activities.SelectMany(x => x.Phases, (x, e) => new
                {
                    PhSchedule = e.PhSchedule
                }).ToList();

                if (!uwPhase.Any())
                {
                    throw new Exception("Monthly LE Data for this month is not inisiate.");
                }

                var PhaseMin = uwPhase.Min(x => x.PhSchedule.Start);
                var PhaseMinAfterDefaultDate = new DateTime();
                if (PhaseMin == Tools.DefaultDate)
                {
                    PhaseMinAfterDefaultDate = uwPhase.Where(x => x.PhSchedule.Start > Tools.DefaultDate).Min(x => x.PhSchedule.Start);
                }
                else
                {
                    PhaseMinAfterDefaultDate = PhaseMin;
                }
                var PhaseMax = uwPhase.Max(x => x.PhSchedule.Finish);

                //grid datas
                var yearStart = 0;
                var yearFinish = 0;
                string format = "yyyy-MM-dd";
                CultureInfo culture = CultureInfo.InvariantCulture;
                var periodStart = new DateTime();
                var periodFinish = new DateTime();
                periodStart = Tools.ToUTC(wb.DateStart != "" ? DateTime.ParseExact(wb.DateStart, format, culture) : PhaseMin);

                if (periodStart == Tools.DefaultDate)
                {
                    periodStart = new DateTime(2012, 1, 1);

                }
                if (wb.PeriodView != "Project View")
                {
                    periodFinish = Tools.ToUTC(wb.DateFinish != "" ? DateTime.ParseExact(wb.DateFinish, format, culture) : PhaseMax);
                }
                else
                {
                    periodFinish = Tools.ToUTC(wb.DateFinish2 != "" ? DateTime.ParseExact(wb.DateFinish2, format, culture) : PhaseMax);
                }
                yearStart = periodStart.Year;
                yearFinish = periodFinish.Year;
                var dataGrid = new List<BsonDocument>();
                var Detail2 = new List<DetailGridView2>();
                var division = wb.DayOrCost.ToLower().Equals("cost") ? 1000000 : 1;
                var TotOP = 0.0;

                while (yearStart <= yearFinish)
                {
                    if (yearStart == Tools.DefaultDate.Year || yearStart >= PhaseMinAfterDefaultDate.Year)
                    {
                        //get OP and LE
                        var getPropActForOP = waForOP.Activities.SelectMany(x => x.Phases, (x, e) => new
                        {

                            //OP = Tools.Div(wb.DayOrCost.ToLower().Equals("cost") ? e.Plan.Cost : e.Plan.Days, e.Ratio),
                            //LE = Tools.Div(wb.DayOrCost.ToLower().Equals("cost") ? e.LE.Cost : e.LE.Days, e.Ratio),
                            //Ratio = e.AnnualProportions.Where(y => y.Key.Equals(yearStart)).FirstOrDefault().Value,
                            //WellName = x.WellName,
                            //ActivityType = e.ActivityType,
                            //SequenceId = x.UARigSequenceId
                            Activity = x,
                            Phase = e
                        })
                        .Select(x =>
                        {
                            var div = (wb.DayOrCost.Equals("Cost") ? 1000000.0 : 1.0);
                            var OP = 0.0;
                            var TQ = 0.0;
                            var Ratio = 0.0;
                            Ratio = x.Phase.AnnualProportions.Where(y => y.Key.Equals(yearStart)).FirstOrDefault().Value;

                            if (wb.BaseOP.ToLower() == DefaultOP.ToLower())
                            {
                                if (x.Phase.BaseOP.Contains(DefaultOP))
                                {
                                    //OP = Tools.Div(x.Phase.Plan.ToBsonDocument().GetDouble(wb.DayOrCost), div);
                                    OP = x.Phase.Plan.Cost;
                                }
                            }
                            else
                            {
                                var OPHist = x.Activity.OPHistories.Where(d => d.Type.Equals(wb.BaseOP) && d.ActivityType.Equals(x.Phase.ActivityType) && d.PhaseNo == x.Phase.PhaseNo && d.PlanSchedule.Start != Tools.DefaultDate).FirstOrDefault();
                                if (OPHist != null)
                                {
                                    if (wb.DayOrCost.ToLower() == "cost")
                                    {
                                        OP = OPHist.Plan.Cost;

                                    }
                                    else
                                    {
                                        OP = OPHist.Plan.Days;
                                    }
                                }
                            }
                            return new
                            {
                                Ratio = Ratio,
                                OP = OP,
                                WellName = x.Activity.WellName,
                                ActivityType = x.Phase.ActivityType,
                                SequenceId = x.Activity.UARigSequenceId
                            };
                        }).Where(d => d.OP != 0).ToList();
                        var getPropAct = wa.Activities.SelectMany(x => x.Phases, (x, e) => new
                        {
                            //OP = Tools.Div(e.CalcOP.Cost, e.Ratio),
                            OP = Tools.Div(wb.DayOrCost.ToLower().Equals("cost") ? e.Plan.Cost : e.Plan.Days, e.Ratio),
                            LE = Tools.Div(wb.DayOrCost.ToLower().Equals("cost") ? e.LE.Cost : e.LE.Days, e.Ratio),
                            Ratio = e.AnnualProportions.Where(y => y.Key.Equals(yearStart)).FirstOrDefault().Value,
                            WellName = x.WellName,
                            ActivityType = e.ActivityType,
                            SequenceId = x.UARigSequenceId,
                            PhaseNo = e.PhaseNo
                        }).ToList();
                        var calcOP = 0.0;
                        var calcLE = 0.0;
                        if (wb.PeriodView != "Project View")
                        {
                            calcOP = Tools.Div(getPropActForOP.Sum(c => c.OP * c.Ratio), division);
                            calcLE = Tools.Div(getPropAct.Sum(c => c.LE * c.Ratio), division);
                        }
                        else
                        {
                            if (periodFinish.Year - periodStart.Year > 0)
                            {
                                calcOP = Tools.Div(getPropActForOP.Sum(c => c.OP * c.Ratio), division);
                                calcLE = Tools.Div(getPropAct.Sum(c => c.LE * c.Ratio), division);
                            }
                            else
                            {
                                calcOP = Tools.Div(getPropActForOP.Sum(c => c.OP), division);
                                calcLE = Tools.Div(getPropAct.Sum(c => c.LE), division);
                            }
                        }
                        var getPropElement = new List<GridDetailView>();
                        foreach (var x in wa.PIPs)
                        {
                            if (x.Elements != null && x.Elements.Count > 0)
                            {
                                foreach (var e in x.Elements)
                                {
                                    var calcPeriod = DateRangeToMonth.GetRangeInYear(e.Period, yearStart);
                                    var detailview = new GridDetailView
                                             {
                                                 LE = Tools.Div(wb.DayOrCost.ToLower().Equals("cost") ? e.CostCurrentWeekImprovement + e.CostCurrentWeekRisk : e.DaysCurrentWeekImprovement + e.DaysCurrentWeekRisk, e.RatioElement),
                                                 //LE = e.CostCurrentWeekImprovement + e.CostCurrentWeekRisk,
                                                 Ratio = e.AnnualProportions.Where(y => y.Key.Equals(yearStart)).FirstOrDefault().Value,
                                                 Completion = e.Completion.ToString(),
                                                 Classification = e.Classification,
                                                 Idea = e.Title,
                                                 WellName = x.WellName,
                                                 ActivityType = x.ActivityType,
                                                 Year = yearStart,
                                                 Period = calcPeriod,
                                                 DaysNumber = calcPeriod.Start == Tools.DefaultDate ? 0 : Convert.ToInt32(calcPeriod.Days) + 1,
                                                 oPeriod = e.Period,
                                                 oDaysNumber = e.Period.Start == Tools.DefaultDate ? 0 : Convert.ToInt32(e.Period.Days) + 1,
                                                 SequenceId = x.SequenceId
                                             };
                                    getPropElement.Add(detailview);
                                }
                            }
                        }
                        var LE_Competitive_Scope = getLEPIPClassForGrid(getPropElement, "Competitive Scope", yearStart);
                        var LE_Supply_Chain_Transformation = getLEPIPClassForGrid(getPropElement, "Supply Chain Transformation", yearStart);
                        var LE_Efficient_Execution = getLEPIPClassForGrid(getPropElement, "Efficient Execution", yearStart);
                        var LE_Technology_and_Innovation = getLEPIPClassForGrid(getPropElement, "Technology and Innovation", yearStart);
                        var LE_AllOthers = getPropElement.Where(x => x.Year == yearStart && null != x.Classification && !x.Classification.ToLower().Equals("Technology and Innovation".ToLower()) &&
                                                        !x.Classification.ToLower().Equals("Supply Chain Transformation".ToLower()) &&
                                                        !x.Classification.ToLower().Equals("Competitive Scope".ToLower()) &&
                                                        !x.Classification.ToLower().Equals("Efficient Execution".ToLower()) && x.Completion.Equals("Realized")).Sum(x => x.LE * x.Ratio);

                        var banked1 = 0.0;
                        var banked2 = 0.0;
                        var banked3 = 0.0;
                        var banked4 = 0.0;

                        foreach (var aa in getPropAct)
                        {
                            var DetailView2 = new DetailGridView2();
                            DetailView2.Year = yearStart;
                            DetailView2.WellName = aa.WellName;
                            DetailView2.ActivityType = aa.ActivityType;


                            //DetailView2.OP = Tools.Div(getPropActForOP.Where(x => x.WellName.Equals(aa.WellName) && x.ActivityType.Equals(aa.ActivityType) && x.SequenceId.Equals(aa.SequenceId)).Sum(c => wb.PeriodView != "Project View" || periodFinish.Year - periodStart.Year > 0 ? c.OP * c.Ratio : c.OP), division);
                            var Pembilang = getPropActForOP.Where(x => x.WellName.Equals(aa.WellName) && x.ActivityType.Equals(aa.ActivityType) && x.SequenceId.Equals(aa.SequenceId)).FirstOrDefault();
                            if (Pembilang != null)
                            {
                                DetailView2.OP = Tools.Div(wb.PeriodView != "Project View" || periodFinish.Year - periodStart.Year > 0 ? Pembilang.OP * Pembilang.Ratio : Pembilang.OP, division);
                            }
                            else
                            {
                                DetailView2.OP = 0;
                            }

                            DetailView2.LE = Tools.Div(getPropAct.Where(x => x.WellName.Equals(aa.WellName) && x.ActivityType.Equals(aa.ActivityType) && x.SequenceId.Equals(aa.SequenceId)).Sum(c => wb.PeriodView != "Project View" || periodFinish.Year - periodStart.Year > 0 ? c.LE * c.Ratio : c.LE), division);

                            DetailView2.SupplyChainTransformation = getPropElement.Where(x => null != x.Classification && x.Classification.ToLower().Equals("Supply Chain Transformation".ToLower()) && x.Completion.Equals("Realized") && x.WellName.Equals(aa.WellName) && x.ActivityType.Equals(aa.ActivityType) && x.SequenceId.Equals(aa.SequenceId)).Sum(x => x.LE * x.Ratio);
                            DetailView2.CompetitiveScope = getPropElement.Where(x => null != x.Classification && x.Classification.ToLower().Equals("Competitive Scope".ToLower()) && x.Completion.Equals("Realized") && x.WellName.Equals(aa.WellName) && x.ActivityType.Equals(aa.ActivityType) && x.SequenceId.Equals(aa.SequenceId)).Sum(x => x.LE * x.Ratio);
                            DetailView2.TechnologyAndInnovation = getPropElement.Where(x => null != x.Classification && x.Classification.ToLower().Equals("Technology And Innovation".ToLower()) && x.Completion.Equals("Realized") && x.WellName.Equals(aa.WellName) && x.ActivityType.Equals(aa.ActivityType) && x.SequenceId.Equals(aa.SequenceId)).Sum(x => x.LE * x.Ratio);
                            DetailView2.EfficientExecution = getPropElement.Where(x => null != x.Classification && x.Classification.ToLower().Equals("Efficient Execution".ToLower()) && x.Completion.Equals("Realized") && x.WellName.Equals(aa.WellName) && x.ActivityType.Equals(aa.ActivityType) && x.SequenceId.Equals(aa.SequenceId)).Sum(x => x.LE * x.Ratio);
                            DetailView2.AllOthers = getPropElement.Where(x => null != x.Classification && !x.Classification.ToLower().Equals("Technology and Innovation".ToLower()) &&
                                                        !x.Classification.ToLower().Equals("Supply Chain Transformation".ToLower()) &&
                                                        !x.Classification.ToLower().Equals("Competitive Scope".ToLower()) &&
                                                        !x.Classification.ToLower().Equals("Efficient Execution".ToLower()) && x.Completion.Equals("Realized")
                                                        && x.WellName.Equals(aa.WellName) && x.ActivityType.Equals(aa.ActivityType) && x.SequenceId.Equals(aa.SequenceId))
                                                        .Sum(x => x.LE * x.Ratio);

                            TotOP += DetailView2.OP;
                            var getWAUM = WellActivityUpdateMonthly.GetById(aa.WellName, aa.SequenceId, aa.PhaseNo, null, true);
                            if (getWAUM != null)
                            {
                                banked1 += wb.DayOrCost.ToLower().Equals("cost") ? getWAUM.BankedSavingsSupplyChainTransformation.Cost : getWAUM.BankedSavingsSupplyChainTransformation.Days;
                                banked2 += wb.DayOrCost.ToLower().Equals("cost") ? getWAUM.BankedSavingsCompetitiveScope.Cost : getWAUM.BankedSavingsCompetitiveScope.Days;
                                banked3 += wb.DayOrCost.ToLower().Equals("cost") ? getWAUM.BankedSavingsTechnologyAndInnovation.Cost : getWAUM.BankedSavingsTechnologyAndInnovation.Days;
                                banked4 += wb.DayOrCost.ToLower().Equals("cost") ? getWAUM.BankedSavingsEfficientExecution.Cost : getWAUM.BankedSavingsEfficientExecution.Days;

                                DetailView2.SupplyChainTransformation += wb.DayOrCost.ToLower().Equals("cost") ? getWAUM.BankedSavingsSupplyChainTransformation.Cost : getWAUM.BankedSavingsSupplyChainTransformation.Days;
                                DetailView2.CompetitiveScope += wb.DayOrCost.ToLower().Equals("cost") ? getWAUM.BankedSavingsCompetitiveScope.Cost : getWAUM.BankedSavingsCompetitiveScope.Days;
                                DetailView2.TechnologyAndInnovation += wb.DayOrCost.ToLower().Equals("cost") ? getWAUM.BankedSavingsTechnologyAndInnovation.Cost : getWAUM.BankedSavingsTechnologyAndInnovation.Days;
                                DetailView2.EfficientExecution += wb.DayOrCost.ToLower().Equals("cost") ? getWAUM.BankedSavingsEfficientExecution.Cost : getWAUM.BankedSavingsEfficientExecution.Days;
                            }

                            if (DetailView2.OP + DetailView2.LE + DetailView2.SupplyChainTransformation + DetailView2.CompetitiveScope + DetailView2.TechnologyAndInnovation + DetailView2.EfficientExecution + DetailView2.AllOthers != 0)
                            {
                                Detail2.Add(DetailView2);
                            }

                        }

                        var res = new
                        {
                            Year = yearStart,
                            OP = calcOP,
                            //OP=wa.TotalOP,
                            Competitive_Scope = LE_Competitive_Scope + banked2,
                            Supply_Chain_Transformation = LE_Supply_Chain_Transformation + banked1,
                            Efficient_Execution = LE_Efficient_Execution + banked4,
                            Technology_and_Innovation = LE_Technology_and_Innovation + banked3,
                            All_Others = LE_AllOthers,
                            LE = calcLE
                        };

                        dataGrid.Add(res.ToBsonDocument());
                    }
                    yearStart++;
                }

                var resultCheck = TotOP;
                return new { Grid = DataHelper.ToDictionaryArray(dataGrid), Detail = Detail2.ToList() };
                //return new { Chart = result };
            });
        }

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

        public JsonResult FinancialCalendarInitiate(int id)
        {
            var ri = new ResultInfo();

            try
            {
                var sampleWAUM = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(null);
                var now = DateTime.Now;
                if (sampleWAUM == null)
                {
                    var calendar = WEISFinancialCalendar.Get<WEISFinancialCalendar>(Query.EQ("_id", id));
                    var monthYear = Tools.ToUTC(new DateTime(calendar.MonthYear.Year, calendar.MonthYear.Month, now.Day));
                    var wellActivityIds = GetActivitiesBasedOnMonth(monthYear)
                        .Select(d => d._id + "|" + d.ActivityType).ToList();

                    MonthlyReportInitiate(calendar.MonthYear, "", wellActivityIds, false, true);

                    calendar.Status = "Active";
                    calendar.Save();
                }
                else
                {
                    var calendar = WEISFinancialCalendar.Get<WEISFinancialCalendar>(Query.EQ("_id", id));
                    var monthYear = Tools.ToUTC(new DateTime(calendar.MonthYear.Year, calendar.MonthYear.Month, now.Day));
                    var calendars = WEISFinancialCalendar.Populate<WEISFinancialCalendar>(Query.LT("MonthYear", monthYear));

                    calendars.ForEach(d =>
                    {
                        d.Status = "Closed";
                        d.Save();
                    });

                    var prevMonthlyReports = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(Query.EQ("UpdateVersion", calendar.MonthYear.AddMonths(-1)));

                    prevMonthlyReports.ForEach(d =>
                    {
                        d._id = null;
                        d.UpdateVersion = calendar.MonthYear;
                        d.Status = "In-Progress";
                        d.Save();
                    });

                    calendar.Status = "Active";
                    calendar.Save();
                }
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }
    }

    //public class WaterfallMonthlyLE
    //{
    //    public string title { set; get; }
    //    public double value { set; get; }
    //    public double valueOP { set; get; }
    //    public string summary { set; get; }
    //    public List<AnnualValue> Details { get; set; }
    //    public WaterfallMonthlyLE()
    //    {
    //        Details = new List<AnnualValue>();
    //    }

    //}

    //public class AnnualValue
    //{
    //    public int Year { set; get; }
    //    public double Value { set; get; }
    //    public string Title { set; get; }
    //}

    //class MonthlyReportAndGridResult
    //{
    //    public List<MonthlyOPHelperDetails> OPs { get; set; }
    //    public List<MonthlyLEHelperDetails> LEs { get; set; }
    //    public Dictionary<string, object>[] Grid { set; get; }
    //    public List<WaterfallMonthlyLE> Chart { set; get; }
    //    public List<Dictionary<string, object>> Grid2 { set; get; }
    //}

    //public class MonthlyElementBase
    //{
    //    // elements
    //    public double oDaysPlan { get; set; }
    //    public double oDaysCurrentWeek { get; set; }
    //    public double oCostPlan { get; set; }
    //    public double oCostCurrentWeek { get; set; }

    //    // elements
    //    public double DaysPlan { get; set; }
    //    public double DaysCurrentWeek { get; set; }
    //    public double CostPlan { get; set; }
    //    public double CostCurrentWeek { get; set; }
    //}

    //public class MonthlyEventsBasedOnMonth
    //{
    //    public string _id { set; get; }
    //    public string WellName { set; get; }
    //    public string UARigSequenceId { set; get; }
    //    public string RigName { set; get; }
    //    public string AssetName { set; get; }
    //    public string ActivityType { set; get; }
    //    public DateRange PhSchedule { set; get; }
    //    public DateRange AFESchedule { set; get; }
    //}

    //#region New Class Helper
    //public class MonthlyElementHelper : MonthlyElementBase
    //{
    //    public int Year { get; set; }
    //    //public double Value { get; set; }
    //    public string Classified { get; set; }
    //    public string Title { get; set; }

    //    public double BankedSave { get; set; }

    //    public List<MonthlyElementHelperDetails> Details { get; set; }
    //    public List<MonthlyOPHelperDetails> OPDetails { get; set; }
    //    public List<MonthlyLEHelperDetails> LEDetails { get; set; }

    //    //public MonthlyElementBase Realized { get; set; }
    //    //public MonthlyElementBase Unrealized { get; set; }

    //    // op
    //    public double OPCost { get; set; }
    //    public double OPDays { get; set; }

    //    // op with LE
    //    //public double OPWithLeCost { get; set; }
    //    //public double OPWithLeDays { get; set; }

    //    // LE
    //    public double LECost { get; set; }
    //    public double LEDays { get; set; }

    //    public MonthlyElementHelper()
    //    {
    //        Details = new List<MonthlyElementHelperDetails>();
    //        OPDetails = new List<MonthlyOPHelperDetails>();
    //        LEDetails = new List<MonthlyLEHelperDetails>();
    //    }
    //}

    //public class MonthlyOPWithLEHelperDetails
    //{
    //    public int Year { get; set; }
    //    public double Value { get; set; }


    //    public string WellName { get; set; }
    //    public string RigName { get; set; }
    //    public string ActivityType { get; set; }
    //    public string SequenceId { get; set; }

    //    public DateRange oPhSchedule { get; set; }
    //    public DateRange PhSchedule { get; set; }
    //    public double NumOfDayinThisYear { get; set; }

    //    public DateRange oLESchedule { get; set; }
    //    public DateRange LESchedule { get; set; }

    //    public WellDrillData oOP { get; set; }
    //    public WellDrillData OP { get; set; }

    //    public WellDrillData oLE { get; set; }
    //    public WellDrillData LE { get; set; }

    //    public WellDrillData OPwithLE { get; set; }


    //    public MonthlyOPWithLEHelperDetails()
    //    {
    //        oOP = new WellDrillData();
    //        OP = new WellDrillData();
    //        oLE = new WellDrillData();
    //        LE = new WellDrillData();

    //        OPwithLE = new WellDrillData();
    //    }


    //}

    //public class MonthlyLEHelper
    //{
    //    public double Days { get; set; }
    //    public double Cost { get; set; }
    //    public List<MonthlyLEHelperDetails> LEDetails = new List<MonthlyLEHelperDetails>();
    //    public MonthlyLEHelper()
    //    {
    //        LEDetails = new List<MonthlyLEHelperDetails>();
    //    }
    //}
    //public class MonthlyOPHelper
    //{
    //    public double Days { get; set; }
    //    public double Cost { get; set; }
    //    public List<MonthlyOPHelperDetails> OPDetails = new List<MonthlyOPHelperDetails>();
    //    public MonthlyOPHelper()
    //    {
    //        OPDetails = new List<MonthlyOPHelperDetails>();
    //    }
    //}

    //public class MonthlyOPHelperDetails
    //{
    //    public int Year { get; set; }
    //    public double Value { get; set; }


    //    public string WellName { get; set; }
    //    public string RigName { get; set; }
    //    public string ActivityType { get; set; }
    //    public string SequenceId { get; set; }

    //    public DateRange oPhSchedule { get; set; }
    //    public DateRange PhSchedule { get; set; }
    //    public double NumOfDayinThisYear { get; set; }

    //    public WellDrillData oOP { get; set; }
    //    public WellDrillData OP { get; set; }

    //    public MonthlyOPHelperDetails()
    //    {
    //        oOP = new WellDrillData();
    //        OP = new WellDrillData();
    //    }


    //}

    //public class MonthlyLEHelperDetails
    //{
    //    public int Year { get; set; }
    //    public double Value { get; set; }


    //    public string WellName { get; set; }
    //    public string RigName { get; set; }
    //    public string ActivityType { get; set; }
    //    public string SequenceId { get; set; }

    //    public DateRange oLESchedule { get; set; }
    //    public DateRange LESchedule { get; set; }
    //    public double NumOfDayinThisYear { get; set; }

    //    public WellDrillData oLE { get; set; }
    //    public WellDrillData LE { get; set; }

    //    public MonthlyLEHelperDetails()
    //    {
    //        oLE = new WellDrillData();
    //        LE = new WellDrillData();
    //    }


    //}

    //public class WaterfallMonthlyLEClassification
    //{
    //    public string Title { set; get; }
    //    public double Realized { set; get; }
    //    public double Unrealized { set; get; }
    //}

    //public class WaterfallMonthlyLEStacked
    //{
    //    public double OP { set; get; }
    //    public double OPwLE { set; get; }
    //    public double Gap { set; get; }
    //    public double LE { set; get; }
    //    public List<WaterfallMonthlyLEClassification> Classifications { set; get; }
    //}

    //public class MonthlyElementHelperDetails
    //{
    //    public string PIPID { get; set; }
    //    public string Idea { get; set; }
    //    public int Year { get; set; }
    //    public double Value { get; set; }
    //    public object Completion { get; set; }
    //    public string Classified { get; set; }
    //    public string Title { get; set; }
    //    public int ItemId { get; set; }

    //    public string WellName { get; set; }
    //    public string RigName { get; set; }
    //    public string ActivityType { get; set; }
    //    public string SequenceId { get; set; }

    //    public DateRange oPeriod { get; set; }
    //    public DateRange Period { get; set; }
    //    public double NumOfDayinThisYear { get; set; }

    //    public double BankSaved { get; set; }

    //    public double oDaysPlanImprovement { get; set; }
    //    public double oDaysPlanRisk { get; set; }
    //    public double oDaysCurrentWeekImprovement { get; set; }
    //    public double oDaysCurrentWeekRisk { get; set; }
    //    public double oCostPlanImprovement { get; set; }
    //    public double oCostPlanRisk { get; set; }
    //    public double oCostCurrentWeekImprovement { get; set; }
    //    public double oCostCurrentWeekRisk { get; set; }

    //    public double DaysPlanImprovement { get; set; }
    //    public double DaysPlanRisk { get; set; }
    //    public double DaysCurrentWeekImprovement { get; set; }
    //    public double DaysCurrentWeekRisk { get; set; }
    //    public double CostPlanImprovement { get; set; }
    //    public double CostPlanRisk { get; set; }
    //    public double CostCurrentWeekImprovement { get; set; }
    //    public double CostCurrentWeekRisk { get; set; }

    //    //public bool isIncludeLe { get; set; }

    //}
    //#endregion
    public class GridDetailView
    {
        public int Year { set; get; }
        public string WellName { set; get; }
        public string ActivityType { set; get; }
        public string Idea { set; get; }
        public DateRange Period { set; get; }
        public DateRange oPeriod { set; get; }
        public Double Ratio { get; set; }
        public Double LE { get; set; }
        public string Completion { get; set; }
        public string Classification { get; set; }
        public int DaysNumber { get; set; }
        public int oDaysNumber { get; set; }
        public string SequenceId { get; set; }
    }

    internal class DetailGridView2
    {
        public int Year { get; set; }
        public string WellName { get; set; }
        public string ActivityType { get; set; }
        public double OP { get; set; }
        public double CompetitiveScope { get; set; }
        public double SupplyChainTransformation { get; set; }
        public double TechnologyAndInnovation { get; set; }
        public double EfficientExecution { get; set; }
        public double AllOthers { get; set; }
        public double LE { get; set; }
    }
}