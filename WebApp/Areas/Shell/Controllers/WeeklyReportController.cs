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

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class WeeklyReportController : Controller
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

            ViewBag.isReadOnly = "0";
            if (_isReadOnly())
            {
                ViewBag.isRO = "1";
            }
            ViewBag.WordCount = getWordCount();
            return View();
        }

        public int getWordCount(){
            var wc = WEISAppsConfig.Get<WEISAppsConfig>(Query.EQ("_id",1));
            if(wc == null){
                return 0;
            }else{
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

            var wau = WellActivityUpdate.Populate<WellActivityUpdate>();
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

        public JsonResult WFStart(DateTime StartDate, string StartComment, List<String> WellActivityIds)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {

                //List<int> WellActvID = new List<int>();
                //string[] _id = WellActivityId.Split(',');
                var dt = Tools.GetNearestDay(Tools.ToDateTime(String.Format("{0:dd-MMM-yyyy}", StartDate), true), DayOfWeek.Monday);
                var qwaus = new List<IMongoQuery>();
                qwaus.Add(Query.EQ("UpdateVersion", dt));
                var q = qwaus.Count == 0 ? Query.Null : Query.And(qwaus);
                List<WellActivityUpdate> waus = WellActivityUpdate.Populate<WellActivityUpdate>(q);

                foreach (string word in WellActivityIds)
                {
                    string[] ids = word.Split('|');
                    int waId = Tools.ToInt32(ids[0]);
                    string activityType = ids[1];
                    WellActivity wa = WellActivity.Get<WellActivity>(waId);
                    var a = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(activityType));

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

                    if (a != null)
                    {
                        //if (a.PhSchedule.InRange(dt)) {
                        JsonResult emailSent = null;
                        Dictionary<string, string> variables = new Dictionary<string, string>();
                        variables.Add("WellName", wa.WellName);
                        variables.Add("ActivityType", a.ActivityType);
                        variables.Add("UpdateVersion", String.Format("{0:dd-MMM-yyyy}", dt));
                        variables.Add("DueDate", String.Format("{0:dd-MMM-yyyy}", dt));
                        variables.Add("UrlAction", "http://" + Request.Url.Host + Url.Action("index", "weeklyreport"));
                        var persons = a.GetPersonsInRole(wa.WellName,
                            Config.GetConfigValue("WEISWRPICRoles", "WELL-ENG").ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries));
                        var ccs = a.GetPersonsInRole(wa.WellName,
                            Config.GetConfigValue("WEISWRReviewersRoles", "").ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries));
                        WellActivityUpdate wau = null;
                        if (waus.Count > 0)
                        {
                            wau = waus.FirstOrDefault(
                                e => e.WellName.Equals(wa.WellName)
                                && (e.Phase == null ? "" : e.Phase.ActivityType).Equals(a.ActivityType)
                                && e.SequenceId.Equals(wa.UARigSequenceId)
                                && e.UpdateVersion.Equals(dt));
                        }
                        if (wau == null)
                        {
                            wau = new WellActivityUpdate();
                            //wau._id = String.Format("{0}_{1:yyyyMMdd}", wa.UARigSequenceId, dt);
                            //wau.Country = "USA";
                            wau.AssetName = wa.AssetName;
                            wau.NewWell = false;
                            wau.WellName = wa.WellName;
                            wau.SequenceId = wa.UARigSequenceId;
                            wau.Phase.ActivityType = a.ActivityType;
                            wau.Phase.ActivityDesc = a.ActivityDesc;
                            wau.Phase.PhaseNo = a.PhaseNo;
                            wau.Status = "In-Progress";
                            wau.UpdateVersion = dt;
                            wau.Elements = PIPElement;

                            //get before
                            var qBefore = new List<IMongoQuery>();
                            qBefore.Add(Query.EQ("WellName", wau.WellName));
                            qBefore.Add(Query.EQ("Phase.ActivityType", wau.Phase.ActivityType));
                            qBefore.Add(Query.EQ("SequenceId", wau.SequenceId));
                            qBefore.Add(Query.LT("UpdateVersion", wau.UpdateVersion));
                            var before = WellActivityUpdate.Get<WellActivityUpdate>(Query.And(qBefore), SortBy.Descending("UpdateVersion"));

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
                                wau.EventStartDate = a.PhSchedule.Start;
                            }
                            wau.Calc();
                            //wau.Phase.LE = new WellDrillData();
                            wau.Save();

                            Email.Send("WRInitiate",
                                persons.Select(d => d.Email).ToArray(),
                                ccs.Count == 0 ? null : ccs.Select(d => d.Email).ToArray(),
                                variables: variables,
                                developerModeEmail: WebTools.LoginUser.Email);
                            waus.Add(wau);
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
                //List<WellActivityUpdate> waus = WellActivityUpdate.Populate<WellActivityUpdate>(q);
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
                //                wau = new WellActivityUpdate();
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
            });
        }

        public JsonResult Search(DateTime? SearchDate = null, List<string> SearchWellNames = null, string SearchStatus = null, string SearchOPType = "All")
        {
            //filter wa first based on is OP14 or NOTOP14
            var qWa = Query.Null;
            if (SearchWellNames != null) qWa = Query.In("WellName", new BsonArray(SearchWellNames));
            List<WellActivity> wa = WellActivity.Populate<WellActivity>(qWa);
            List<string> WellNames = new List<string>();
            if (SearchOPType == "All")
            {
                foreach (var x in wa)
                {
                    WellNames.Add(x.WellName);
                }
            }
            else
            {
                if (SearchOPType == "True")
                {
                    foreach (var x in wa.Where(d => d.NonOP.Equals(true)))
                    {
                        WellNames.Add(x.WellName);
                    }
                }
                else
                {
                    foreach (var x in wa.Where(d => d.NonOP.Equals(false)))
                    {
                        WellNames.Add(x.WellName);
                    }
                }
            }

            SearchWellNames = WellNames;

            var wau = WellActivityUpdate.Get<WellActivityUpdate>(Query.Null, SortBy.Descending("UpdateVersion"));
            //DateTime dtThisMonday = wau != null ? Tools.GetNearestDay(wau.UpdateVersion.AddDays(-3),DayOfWeek.Monday) : Tools.GetNearestDay(DateTime.Now, DayOfWeek.Monday);
            DateTime dtThisMonday = Tools.GetNearestDay(DateTime.Now, DayOfWeek.Monday);
            var q = Query.Null;
            List<IMongoQuery> qs = new List<IMongoQuery>();
            qs.Add(WebTools.GetWellActivitiesQuery("WellName", "Phase.ActivityType"));
            var qdist = Query.And(Query.EQ("Status", "Distributed"), Query.EQ("UpdateVersion", dtThisMonday));
            var qnondist = Query.NE("Status", "Distributed");
            var qstatus = Query.Or(new[] { qdist, qnondist });
            qs.Add(qstatus);
            //if (SearchDate != null) qs.Add(Query.EQ("UpdateVersion", Tools.ToUTC((DateTime)SearchDate)));
            if (SearchDate != null) qs.Add(Query.EQ("UpdateVersion", Tools.GetNearestDay((DateTime)SearchDate, DayOfWeek.Monday)));
            if (SearchWellNames != null) qs.Add(Query.In("WellName", new BsonArray(SearchWellNames)));
            if (SearchStatus != null && SearchStatus != "") qs.Add(Query.EQ("Status", SearchStatus));
            if (qs.Count > 0) q = Query.And(qs);
            List<WellActivityUpdate> was1 = WellActivityUpdate.Populate<WellActivityUpdate>(q);

            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                List<WellActivityUpdate> was = WellActivityUpdate.Populate<WellActivityUpdate>(q).OrderBy(x => x.WellName).ToList();
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

            List<WellActivityUpdate> was1 = WellActivityUpdate.Populate<WellActivityUpdate>(queryFix);

            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                List<WellActivityUpdate> was = WellActivityUpdate.Populate<WellActivityUpdate>(queryFix).OrderByDescending(x => x.UpdateVersion).ToList();
                return was;
            });
        }


        public JsonResult Distribute(List<string> ids)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                var q = Query.In("_id", new BsonArray(ids));
                List<string> filenames = new List<string>();
                List<WellActivityUpdate> was = WellActivityUpdate.Populate<WellActivityUpdate>(q).OrderBy(x => x.WellName).ToList();
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
                List<WellActivityUpdate> was = WellActivityUpdate.Populate<WellActivityUpdate>(q).OrderBy(x => x.WellName).ToList();
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

                    variables.Add("UrlAction", "http://" + Request.Url.Host + Url.Action("index", "weeklyreport"));
                    variables.Add("List", WellNames);
                    Email.Send("WRReminder",
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
                y = DateTime.ParseExact(SearchDate, "dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);

            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                var q = Query.Null;
                List<IMongoQuery> qas = new List<IMongoQuery>();
                if (WellNames != null) qas.Add(Query.In("WellName", new BsonArray(WellNames)));
                if (WellActivityIds != null) qas.Add(Query.In("Phases.ActivityType", new BsonArray(WellActivityIds)));
                if (qas.Count > 0) q = Query.And(qas);

                IMongoQuery q1 = Query.GTE("Phases.PhSchedule.Start", Tools.ToUTC(y));
                IMongoQuery q2 = Query.LTE("Phases.PhSchedule.Finish", Tools.ToUTC(y));
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
                IMongoQuery q1 = Query.LTE("Phases.PhSchedule.Start", Tools.ToUTC(y));
                IMongoQuery q2 = Query.GTE("Phases.PhSchedule.Finish", Tools.ToUTC(y));
                //var q3 = Query.EQ("WellName", "BRUTUS A1 ST4");
                var qb = Query.And(q1, q2);
                //string Risk = "Risk";
                //IMongoQuery qRisk = Query.Matches("Phases.ActivityType",
                //    new BsonRegularExpression(
                //        new Regex(Risk.ToLower(), RegexOptions.IgnoreCase)));
                //var qa = Query.Not(qRisk);
                //var q = Query.And(qa,qb);
                List<WellActivity> was1 = WellActivity.Populate<WellActivity>(qb);

                foreach (var c in was1)
                {
                    //List<WellActivityPhase> phaseDel = c.Phases.Where(t => t.PhSchedule.Start > Tools.ToUTC(y) || t.PhSchedule.Finish < Tools.ToUTC(y)).ToList();
                    List<WellActivityPhase> phaseDel = c.Phases.Where(t => t.PhSchedule.Start > Tools.ToUTC(y) || t.PhSchedule.Finish < Tools.ToUTC(y) || t.ActivityType.Contains("RISK")).ToList();

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
                WellActivityUpdate upd = new WellActivityUpdate();
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
                var wa = WellActivityUpdate.Get<WellActivityUpdate>(id);
                if (wa != null)
                {
                    wa.Status = "Submitted";
                    wa.Save(references:new string[]{"","CalcLeSchedule"});
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
                WellActivityUpdate raw = GetUpdateData(id);
                //foreach (var e in raw.Elements)
                //{
                //    e.CostPlanImprovement = e.CostPlanImprovement + e.CostPlanRisk;
                //    e.DaysPlanImprovement = e.DaysPlanImprovement + e.DaysPlanRisk;
                //}
                raw.Actual.Cost = Tools.Div(raw.Actual.Cost, 1000000);
                WellActivityActual act = WellActivityActual.GetById(raw.WellName, raw.SequenceId, null, true, false);
                return new{
                    Record = raw,
                    HasEDM = act!=null
                };
            });
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

        public JsonResult GetWaterfallData(WellActivityUpdate wau, string DayOrCost = "Day", string GroupBy = "Day", bool IncludeZero = false)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var wellPIPController = new WellPIPController();
                var division = 1000000.0;
                var Target = DayOrCost.Equals("Day") ? wau.CurrentWeek.Days : wau.CurrentWeek.Cost;
                var AFE = DayOrCost.Equals("Day") ? wau.AFE.Days : wau.AFE.Cost;
                var OP = DayOrCost.Equals("Day") ? wau.OP.Days : wau.OP.Cost;

                if (DayOrCost.Equals("Cost"))
                {
                    Target /= division;
                    AFE /= division;
                    OP /= division;
                }

                List<PIPElement> PIPs = new List<PIPElement>();
                WellPIP wpip = WellPIP.GetByWellActivity(wau.WellName, wau.Phase.ActivityType);
                if (wpip != null) PIPs.AddRange(wpip.Elements);
                var final = new List<WaterfallItem>();
                final.Add(new WaterfallItem(0, "AFE", AFE, ""));

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
                                LE = LE==0 ? Plan : LE
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
                        final.Add(new WaterfallItem(0.1, "Gaps (P)", Target - (gap + AFE), ""));
                    }
                }
                else
                {
                    final.Add(new WaterfallItem(0.1, "Gaps (P)", Target - AFE, ""));
                }


                final.Add(new WaterfallItem(1, "LE", Target, "total"));

                return final;
            });
        }

        private WellActivityUpdate GetUpdateData(string id)
        {
            WellActivityUpdate upd = new WellActivityUpdate();
            var wa = WellActivityUpdate.Get<WellActivityUpdate>(id);
            if (wa != null) wa.Calc();

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

        public JsonResult Save(WellActivityUpdate model)
        {
            #region Update WellPIP with only the Latest WellActivityUpdate
            //List<IMongoQuery> qs = new List<IMongoQuery>();
            //qs.Add(Query.EQ("WellName", model.WellName));
            //qs.Add(Query.EQ("SequenceId", model.SequenceId));

            //var listActivityUpdate = DataHelper.Populate<WellActivityUpdate>(model.TableName, Query.And(qs));

            //if (listActivityUpdate != null && listActivityUpdate.Count > 0)
            //{
            //    WellActivityUpdate updater = new WellActivityUpdate();
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
                //    model._id = SequenceNo.Get(new WellActivityUpdate().TableName).ClaimAsInt();
                //}
                //model.CurrentWeek.Cost = model.CurrentWeek.Cost * 1000000;

                List<PIPElement> ListElement = new List<PIPElement>();
                PIPElement Element = new PIPElement();
                List<PIPAllocation> ListAllocation = new List<PIPAllocation>();
                PIPAllocation Allocation = new PIPAllocation();
                WellActivityUpdate NewWau = new WellActivityUpdate();
                WellActivityUpdate wau = WellActivityUpdate.Get<WellActivityUpdate>(model._id);

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

                NewWau.Save(references: new string[] { "", "CalcLeSchedule" });
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
            //WellActivityUpdate wau = WellActivityUpdate.Get<WellActivityUpdate>(id);
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
                 String.Format("WR-{0}.pdf", DateTime.Now.ToString("ddMMMyy")));

            string newFileNameSingle = "";

            Workbook wb = new Workbook();
            foreach (string id in ids)
            {
                WellActivityUpdate wau = WellActivityUpdate.Get<WellActivityUpdate>(id);
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
                       String.Format("WR-{0}-{1}.pdf", idx, DateTime.Now.ToString("ddMMMyy")));

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
                    WellActivityUpdate DataAct = WellActivityUpdate.Get<WellActivityUpdate>(query);

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
                var wa = WellActivityUpdate.Get<WellActivityUpdate>(id);
                if (wa != null)
                {
                    wa.Status = "In-Progress";
                    wa.Save();
                }
                return wa;
            });
        }

        public JsonResult GetDataAllocation(string id, int ElementId)
        {
            try
            {

                var query = Query.EQ("_id", id);
                WellActivityUpdate wp = WellActivityUpdate.Get<WellActivityUpdate>(query);
                PIPElement Elements = new PIPElement();

                if (wp.Elements.Count > 0)
                {
                    Elements = wp.Elements.Where(x => x.ElementId == ElementId).FirstOrDefault();
                }

                if(Elements==null)
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
                WellActivityUpdate wau = WellActivityUpdate.Get<WellActivityUpdate>(query);

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
        //    var latest = WellActivityUpdate.Get<WellActivityUpdate>(Query.And(qs), SortBy.Descending("UpdateVersion"));
        //    return latest;
        //}

        public JsonResult Delete(string id)
        {
            try
            {
                var wau = WellActivityUpdate.Get<WellActivityUpdate>(id);
                if (wau.Status != "In-Progress")
                {
                    throw new Exception("Data which the status is not In-Progress cannot be deleted!");
                }
                else
                {
                    wau.Delete();
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
                WellActivityUpdate NewWAU = new WellActivityUpdate();

                WellActivityUpdate wau = WellActivityUpdate.Get<WellActivityUpdate>(q);
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

        public JsonResult GetComment(string ActivityUpdateId, string ElementID)
        {
            var au = WellActivityUpdate.Get<WellActivityUpdate>(Query.EQ("_id", ActivityUpdateId));
            List<object> WAUIds = new List<object>();
            if (au != null)
            {
                var qGetWAUs = Query.And(new IMongoQuery[] { 
                    Query.EQ("WellName", au.WellName),
                    Query.EQ("SequenceId", au.SequenceId),
                    Query.EQ("Phase.ActivityType", au.Phase.ActivityType),
                }.ToList());
                var ListWAUs = WellActivityUpdate.Populate<WellActivityUpdate>(qGetWAUs);
                if (ListWAUs != null && ListWAUs.Count > 0)
                {
                    foreach(var wau in ListWAUs){
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


            return Json(new { Data = data.OrderByDescending(x=>x.LastUpdate) }, JsonRequestBehavior.AllowGet);
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
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }
    }
}