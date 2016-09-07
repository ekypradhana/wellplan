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

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class TestController : Controller
    {
        // GET: Shell/Test
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Email()
        {
            return View();
        }

        public JsonResult ChangeWAUElement()
        {
            return MvcResultInfo.Execute(() =>
            {
                var qs = Query.And(
                        Query.EQ("WellName", "RAM POWELL A7"),
                        Query.EQ("Phase.ActivityType", "WHOLE COMPLETION EVENT")
                    );
                var wau = WellActivityUpdate.Get<WellActivityUpdate>(qs);
                if (wau != null)
                {
                    wau.Delete();
                    wau.Phase.ActivityType = "WHOLE DRILLING EVENT";
                    wau.Phase.PhaseNo = 1;
                    wau.Save();
                }
                return "OK";
            });
        }

        public JsonResult SyncWAULE()
        {
            return MvcResultInfo.Execute(() =>
            {
                var waus = WellActivityUpdate.Populate<WellActivityUpdate>(Query.EQ("Status", "In-Progress"));
                foreach (var wau in waus)
                {
                    wau.Calc();
                    wau.Save();
                }

                return "OK";
            });
        }

        public JsonResult SyncPIP()
        {
            return MvcResultInfo.Execute(() =>
            {
                var wps = WellPIP.Populate<WellPIP>()
                    .Select(d =>
                    {
                        d.Save();
                        var wau = WellActivityUpdate.GetById(d.WellName, d.SequenceId, d.PhaseNo, null, true);
                        if (wau != null) wau.Save();
                        return d;
                    });

                return "OK";
            });
        }

        public JsonResult SyncPIPElement()
        {
            return MvcResultInfo.Execute(() =>
            {
                var pips = WellPIP.Populate<WellPIP>();
                foreach (var pip in pips)
                {
                    var wau = WellActivityUpdate.GetById(pip.WellName, pip.SequenceId, pip.PhaseNo, null, true, false);
                    foreach (var e in pip.Elements)
                    {
                        if(wau==null)
                            e.ResetAllocation(false, null, true, true, true);
                        else
                            e.ResetAllocation(true,wau,true,true,false);
                    }
                    pip.Save();
                }

                return "OK";
            });
        }

        public JsonResult Fix(bool init = false)
        {
            return MvcResultInfo.Execute(() =>
            {
                if (init == true)
                {
                    WellActivity.SyncSequenceId();

                    var was = WellActivity.Populate<WellActivity>();
                    foreach (var wa in was)
                    {
                        DateTime dtFrom = wa.PsSchedule.Start;
                        DateTime dtTo;
                        foreach (var ph in wa.Phases)
                        {
                            //if (ph.PlanSchedule == null || ph.PlanSchedule.Start.Year < 2000) ph.PlanSchedule = ph.PhSchedule;
                            dtTo = dtFrom.AddDays(ph.Plan.Days);
                            ph.PlanSchedule = new DateRange(dtFrom, dtTo);
                            //if (ph.PhSchedule == null || ph.PhSchedule.Start.Year < 2000) ph.PhSchedule = ph.PlanSchedule;
                            ph.LESchedule = ph.PhSchedule;
                            dtFrom = dtTo.AddDays(1);
                        }
                        wa.Save();
                    }
                }

                #region load last WR and update
                var wauKeys = WellActivityUpdate.Populate<WellActivityUpdate>()
                    .GroupBy(d => new { d.WellName, d.SequenceId, d.Phase.ActivityType })
                    .Select(d => new
                    {
                        Keys = d.Key,
                        UpdateVersion = d.Max(x => x.UpdateVersion)
                    })
                    .ToList();
                foreach (var wauKey in wauKeys)
                {
                    var wau = WellActivityUpdate.Get<WellActivityUpdate>(Query.And(
                            Query.EQ("WellName", wauKey.Keys.WellName),
                            Query.EQ("SequenceId", wauKey.Keys.SequenceId),
                            Query.EQ("Phase.ActivityType", wauKey.Keys.ActivityType),
                            Query.EQ("UpdateVersion", wauKey.UpdateVersion)
                        ));
                    if (wau != null)
                    {
                        wau.Calc();
                        wau.Save();
                    }
                }
                #endregion

                WellActivity.UpdateLESchedule();
                return "OK";
            });
        }

        public JsonResult LoadLS(string id="OP201502V2")
        {
            return MvcResultInfo.Execute(() =>
            {
                WellActivity.LoadLastSequence(id);
                return "OK";
            });
        }

        public JsonResult RenameUserToLower()
        {
            return MvcResultInfo.Execute(() =>
            {
                var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                var userMgr = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));
                var users = DataHelper.Populate<IdentityUser>("Users");
                foreach (var usr in users)
                {
                    var u = userMgr.FindByName(usr.UserName);
                    if (u != null)
                    {
                        u.UserName = u.UserName.ToLower();
                        u.Email = u.Email.ToLower();
                        userMgr.Update(u);
                    }
                }

                var wps = WEISPerson.Populate<WEISPerson>();
                foreach (var wp in wps)
                {
                    foreach (var pi in wp.PersonInfos)
                    {
                        pi.Email = pi.Email.ToLower();
                    }
                    wp.Save();
                }

                return "ok";
            });
        }

        public JsonResult SyncPhase()
        {
            return MvcResultInfo.Execute(() =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    foreach (var ph in wa.Phases)
                    {
                        var wau = WellActivityUpdate.GetById(wa.WellName, wa.UARigSequenceId, ph.PhaseNo, null, true, false);
                        if (wau == null)
                        {
                            ph.LE = new WellDrillData();
                        }
                        else
                        {
                            if (wau.CurrentWeek.Days == 0 && wau.CurrentWeek.Cost == 0)
                            {
                                wau.Calc();
                                var prev = wau.GetBefore();
                                if (prev != null)
                                {
                                    ph.LastWeek = prev.UpdateVersion;
                                    ph.PreviousWeek = prev.UpdateVersion.AddDays(-7);
                                    ph.LE = prev.CurrentWeek;
                                    ph.LWE = prev.LastWeek;
                                }
                            }
                            else
                            {
                                ph.LastWeek = wau.UpdateVersion;
                                ph.PreviousWeek = wau.UpdateVersion.AddDays(-7);
                                ph.LE = wau.CurrentWeek;
                                ph.LWE = wau.LastWeek;
                            }
                        }

                        var act = WellActivityActual.GetById(wa.WellName, wa.UARigSequenceId, null, true, false);
                        if (act == null)
                        {
                            ph.Actual = new WellDrillData();
                            ph.AFE = new WellDrillData();
                        }
                        else
                        {
                            var phAct = act.Actual.FirstOrDefault(d => d.PhaseNo.Equals(ph.PhaseNo));
                            if (phAct != null) ph.Actual = phAct.Data; else ph.Actual = new WellDrillData();
                            var phAFE = act.AFE.FirstOrDefault(d => d.PhaseNo.Equals(ph.PhaseNo));
                            //if (phAFE!=null && phAFE.Data.Days == 0 && phAFE.Data.Cost == 0)
                            //{
                            //    var bfr = act.GetBefore();
                            //    if (bfr != null)
                            //    {
                            //        var phAFEBefore = act.AFE.FirstOrDefault(d => d.PhaseNo.Equals(ph.PhaseNo));
                            //        if (phAFEBefore != null) phAFE = phAFEBefore;
                            //    }
                            //}
                            if (phAFE != null) ph.AFE = phAFE.Data; else ph.AFE = new WellDrillData();
                        }

                        if (ph.LevelOfEstimate == null) ph.LevelOfEstimate = "n/a";
                        if (ph.Plan.Cost == 0) ph.Plan.Cost = ph.OP.Cost;

                        ph.OP.Days = ph.PhSchedule.Days;
                    }
                    wa.Save();
                }
                return "OK";
            });
            //return MvcTools.ToJsonResult(email.SendEmail());
        }

        public JsonResult SendEmail(EmailModel email)
        {
            return MvcResultInfo.Execute(() =>
            {
                //email.Host = "smtp.office365.com";
                //email.Port = 587;
                //email.UserName = "mailer@eaciit.com";
                //email.Password = "Ruka6309";
                //email.From = "mailer@eaciit.com";
                //email.To = "yoga.bangkit@eaciit.com";
                //email.Subject = "Test Send Email";
                //email.Message = "Test Send Email\n";
                //email.Message += "Mail Server Configuration:\n";
                //email.Message += "SMTP Server : " + email.Host +":"+email.Port.ToString() + "\n";
                //email.Message += "Username : " + email.UserName + "\n";
                //email.Message += "Password : " + email.Password + "\n";
                //email.TLS = true;
                var s = email.SendEmail();
                if (s != "OK") throw new Exception(s);
                return "OK";
            });
            //return MvcTools.ToJsonResult(email.SendEmail());
        }

        public JsonResult DummyBC()
        {
            return MvcResultInfo.Execute(() =>
            {
                var email = ECIS.Client.WEIS.Email.Get<ECIS.Client.WEIS.Email>("DummyBC");
                var users = DataHelper.Populate<IdentityUser>("Users");
                var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                var userManager = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));
                foreach (var u in users.Where(d => d.HasChangePassword == false))
                {
                    if (new string[] { "hector.romero", "navdeep", "eaciit" }.Contains(u.UserName.ToLower()) == false)
                    {
                        var np = Tools.GenerateRandomString(8);
                        //var np = "Password.1";
                        var user = userManager.FindByName(u.UserName);
                        var isFirstLogin = !user.HasChangePassword;
                        String hashpassword = userManager.PasswordHasher.HashPassword(np);
                        user.PasswordHash = hashpassword;
                        userManager.Update(user);

                        Dictionary<string, string> vars = new Dictionary<string, string>();
                        vars.Add("FullName", u.FullName);
                        vars.Add("Email", u.Email);
                        vars.Add("UserName", u.UserName);
                        vars.Add("Password", np);
                        vars.Add("UrlAction", "http://" + Request.Url.Host + Url.Action("Index", "Home"));

                        var ri = email.Send(new string[] { u.Email }, new string[] { "hector.romero@shell.com" }, vars, null, "arief@eaciit.com");
                        //var ri = email.Send(new string[] { u.Email }, new string[] { "arief@eaciit.com" }, vars, null, "arief@eaciit.com");
                        if (ri.Result != "OK") throw new Exception(ri.Message + ri.Trace);
                    }
                }

                return "OK";
            });
            //return MvcTools.ToJsonResult(email.SendEmail());
        }

        public JsonResult DummyBC2()
        {
            return MvcResultInfo.Execute(() =>
            {
                var email = ECIS.Client.WEIS.Email.Get<ECIS.Client.WEIS.Email>("DummyBC");
                var users = DataHelper.Populate<IdentityUser>("Users");
                var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                var userManager = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));
                foreach (var u in users)
                {
                    if (new string[] { "eaciit" }.Contains(u.UserName.ToLower()) == true)
                    {
                        //var np = Tools.GenerateRandomString(8);
                        var np = "W31sAdmin";
                        var user = userManager.FindByName(u.UserName);
                        var isFirstLogin = !user.HasChangePassword;
                        String hashpassword = userManager.PasswordHasher.HashPassword(np);
                        user.PasswordHash = hashpassword;
                        userManager.Update(user);

                        Dictionary<string, string> vars = new Dictionary<string, string>();
                        vars.Add("FullName", u.FullName);
                        vars.Add("Email", u.Email);
                        vars.Add("UserName", u.UserName);
                        vars.Add("Password", np);
                        vars.Add("UrlAction", "http://" + Request.Url.Host + Url.Action("Index", "Home"));

                        //var ri = email.Send(new string[] { u.Email }, new string[] { "hector.romero@eaciit.com" }, vars, null, "arief@eaciit.com");
                        //var ri = email.Send(new string[] { u.Email }, new string[] { "arief@eaciit.com" }, vars, null, "arief@eaciit.com");
                        //if (ri.Result != "OK") throw new Exception(ri.Message + ri.Trace);
                    }
                }

                return "OK";
            });
            //return MvcTools.ToJsonResult(email.SendEmail());
        }


        public ActionResult Validation()
        {
            return View();
        }

        public ResultInfo TestMail()
        {
            return Tools.SendMail("Default", "Send email from API", "Ini adalah kiriman email dari API",
                new string[] { "arief@eaciit.com", "adarmawan.2006@gmail.com", "ariefda@hotmail.com" });
        }

        public JsonResult LoadPerson()
        {
            List<string> RoleIds = new List<string>();
            return MvcResultInfo.Execute(() =>
            {
                var tmps = DataHelper.Populate("_tmpPersons").OrderBy(d => d.GetString("WellName")).ToList();
                foreach (var tmp in tmps)
                {
                    var wellName = tmp.GetString("WellName");
                    var activity = tmp.GetString("WellActivity");
                    var email = tmp.GetString("Email");
                    WEISPerson person = null;
                    var wa = WellActivity.Get<WellActivity>(Query.And(
                            Query.EQ("WellName", wellName),
                            Query.EQ("Phases.ActivityType", activity)
                        ));
                    if (wa != null)
                    {
                        var act = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(activity));
                        if (act != null)
                        {
                            person = WEISPerson.Get<WEISPerson>(Query.And(
                                    Query.EQ("WellName", wellName),
                                    Query.EQ("ActivityType", activity)
                                ));
                            if (person == null)
                            {
                                person = new WEISPerson();
                                person.WellName = wellName;
                                person.SequenceId = wa.UARigSequenceId;
                                person.ActivityType = act.ActivityType;
                                person.PhaseNo = act.PhaseNo;
                                person.PersonInfos = new List<WEISPersonInfo>();
                            }
                            else
                            {
                                person.PersonInfos.RemoveAll(d => d.Email.Equals(""));
                            }
                            var p = person.PersonInfos.FirstOrDefault(d => d.Email.Equals(email));
                            if (p == null)
                            {
                                p = new WEISPersonInfo
                                {
                                    Email = email
                                };
                                person.PersonInfos.Add(p);
                            }
                            else
                            {
                                p.Email = email;
                            }
                            p.FullName = tmp.GetString("FullName");
                            p.RoleId = tmp.GetString("Role");
                            person.Save();
                            if (RoleIds.Contains(p.RoleId) == false)
                                RoleIds.Add(p.RoleId);

                            var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                            var userMgr = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));
                            var user = DataHelper.Get<IdentityUser>("Users", Query.EQ("Email", email));
                            if (user == null)
                            {
                                userMgr.Create(new IdentityUser
                                {
                                    UserName = tmp.GetString("UserName").ToLower(),
                                    Email = email,
                                    FullName = tmp.GetString("FullName"),
                                    Enable = true,
                                    ADUser = false,
                                    ConfirmedAtUtc = Tools.ToUTC(DateTime.Now)
                                }, "Password.1");
                            }
                            else
                            {
                                user.FullName = tmp.GetString("FullName");
                                user.UserName = tmp.GetString("UserName").ToLower();
                                userMgr.Update(user);
                            }
                        }
                    }
                }

                foreach (var roleId in RoleIds)
                {
                    var role = WEISRole.Get<WEISRole>(roleId);
                    if (role == null)
                    {
                        role = new WEISRole
                        {
                            _id = roleId,
                            Title = roleId,
                            RoleName = roleId
                        };
                    }
                    else
                    {
                        role.RoleName = roleId;
                    }
                    role.Save();
                }

                return "OK";
            });
        }

        public JsonResult SetPerson()
        {
            return MvcResultInfo.Execute(() =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    foreach (var ph in wa.Phases)
                    {
                        List<IMongoQuery> qs = new List<IMongoQuery>();
                        qs.Add(Query.And(Query.EQ("ProjectInformation.Title", "Common Well Name"),
                            Query.EQ("ProjectInformation.Detail", wa.WellName)));
                        qs.Add(Query.And(Query.EQ("ProjectInformation.Title", "Activity Type"),
                            Query.EQ("ProjectInformation.Detail", ph.ActivityType)));
                        var qpip = Query.And(qs);
                        BsonDocument docRaw = DataHelper.Get("PIP", qpip);
                        if (docRaw != null)
                        {
                            qs.Clear();
                            qs.Add(Query.EQ("WellName", wa.WellName));
                            qs.Add(Query.EQ("ActivityType", ph.ActivityType));
                            var qpersons = Query.And(qs);
                            WEISPerson WEISPerson = WEISPerson.Get<WEISPerson>(qpersons);
                            if (WEISPerson == null)
                            {
                                WEISPerson = new WEISPerson();
                                WEISPerson.WellName = wa.WellName;
                                WEISPerson.ActivityType = ph.ActivityType;
                            }
                            WEISPerson.SequenceId = wa.UARigSequenceId;
                            WEISPerson.PhaseNo = ph.PhaseNo;
                            WEISPerson.PersonInfos.Clear();
                            List<BsonDocument> docInfos = docRaw.Get("ProjectInformation").AsBsonArray.Select(d => d.ToBsonDocument()).ToList();

                            setPersonFromDoc(WEISPerson, docInfos, "Team Lead", "TEAM-LEAD");
                            setPersonFromDoc(WEISPerson, docInfos, "Lead Engineer", "LEAD-ENG");
                            setPersonFromDoc(WEISPerson, docInfos, "Optimization Engineer", "OPTMZ-ENG");
                            WEISPerson.Save();
                        }
                    }
                }
                return "OK";
            });
        }

        private void setPersonFromDoc(WEISPerson p, List<BsonDocument> docs, string title, string roleid)
        {
            BsonDocument doc = docs.FirstOrDefault(d => d.GetString("Title").Equals(title));
            if (doc != null)
            {
                p.PersonInfos.Add(new WEISPersonInfo
                {
                    FullName = doc.GetString("Detail"),
                    Email = "",
                    RoleId = roleid
                });
            }
        }

        public JsonResult Load()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                WellActivity.LoadOP();
                return "OK";
            });
        }

        public JsonResult UpdatePIPAllocation()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var pips = WellPIP.Populate<WellPIP>();
                foreach (var pip in pips)
                {
                    pip.Save();
                }
                return "OK";
            });
        }

        public JsonResult UpdatePIPOtherInfo()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var pips = WellPIP.Populate<WellPIP>();
                var pipOlds = DataHelper.Populate<WellPIP>("WEISWellPIPs_copy");
                foreach (var pip in pips)
                {
                    var old = pipOlds.FirstOrDefault(d => d.WellName.Equals(pip.WellName) && d.ActivityType.Equals(pip.ActivityType));
                    if (old != null)
                    {
                        pip.PerformanceMetrics = old.PerformanceMetrics;
                        pip.ProjectMilestones = old.ProjectMilestones;
                        pip.Scaled = old.Scaled;
                        pip.Field = old.Field;
                        pip.Save();
                    }
                }
                return "OK";
            });
        }

        public JsonResult AddPerformanceUnitElement()
        {
            var pips = WellPIP.Populate<WellPIP>();
            var pipmst = DataHelper.Populate("_PIP_20150108");

            foreach (var pip in pips)
            {
                foreach (var elem in pip.Elements)
                {

                    var pm = pipmst.Where(x => BsonHelper.GetString(x, "Well_Name").Equals(pip.WellName) &&
                    BsonHelper.GetString(x, "Activity_Type").Equals(pip.ActivityType) &&
                    BsonHelper.GetString(x, "Idea").Equals(elem.Title)
                    );

                    if (pm.Count() > 0)
                        elem.PerformanceUnit = BsonHelper.GetString(pm.FirstOrDefault(), "Performance_Unit");
                    else
                        elem.PerformanceUnit = "";
                }
                pip.Save();
            }

            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                return pips;
            });
        }

        public JsonResult GetLast(int id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var wa = WellActivity.Get<WellActivity>(id);
                if (wa != null) wa.GetUpdate(DateTime.Now, true);
                return wa;
            });
        }

        public JsonResult UpdateEDM()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var acts = ActivityMaster.Populate<ActivityMaster>();
                foreach (var a in acts)
                {
                    if (a._id.ToString().Contains("RISK")) a.EDMActivityId = "RISK";
                    else if (a._id.ToString().Contains("COM")) a.EDMActivityId = "COM";
                    else if (a._id.ToString().Contains("DRIL")) a.EDMActivityId = "DRL";
                    else if (a._id.ToString().Contains("ABAN")) a.EDMActivityId = "ABA";
                    else a.EDMActivityId = "";

                    a.Save();
                }
                return "OK";
            });
        }

        public JsonResult UpdateWaus()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var waus = WellActivityUpdate.Populate<WellActivityUpdate>().OrderBy(d => d.UpdateVersion);
                foreach (var wau in waus)
                {
                    wau.Calc();
                    wau.Save();
                }
                return "OK";
            });
        }

        public JsonResult UpdateActual()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var dt = Tools.GetNearestDay(new DateTime(2014, 12, 1), DayOfWeek.Monday);

                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    bool waSave = false;
                    var well = WellInfo.Get<WellInfo>(wa.WellName);
                    if (String.IsNullOrEmpty(well.EDMWellId) == false)
                    {
                        foreach (var ph in wa.Phases)
                        {
                            var act = ActivityMaster.Get<ActivityMaster>(ph.ActivityType);
                            if (String.IsNullOrEmpty(act.EDMActivityId) == false)
                            {
                                var qafe = Query.And(
                                        Query.EQ("WELLID", well.EDMWellId),
                                        Query.EQ("EVENTCODE", act.EDMActivityId)
                                    );
                                var afe = DataHelper.Get("_WellEventAFE", qafe, SortBy.Descending("DAYSONLOCATION"));
                                if (afe != null)
                                {
                                    waSave = true;
                                    var actual = WellActivityActual.GetById(wa.WellName, wa.UARigSequenceId, DateTime.Now, false, true);
                                    actual.Update(ph.PhaseNo,
                                        new WellDrillData { Days = afe.GetDouble("DAYSONLOCATION"), Cost = afe.GetDouble("ACTUALCOST") });
                                    actual.Save();

                                    ph.Actual = actual.GetActual(ph.PhaseNo).Data;
                                    ph.AFE = new WellDrillData
                                    {
                                        Days = afe.GetDouble("AFEDAYS"),
                                        Cost = afe.GetDouble("AFECOST")
                                    };
                                }
                            }
                        }
                    }
                    if (waSave) wa.Save();
                }

                var q = Query.EQ("UpdateVersion", dt);
                return WellActivityActual.Populate<WellActivityActual>(q);
            });
        }

        public JsonResult UpdateWell()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var wells = WellInfo.Populate<WellInfo>();
                foreach (var w in wells)
                {
                    var j = DataHelper.Get("WEISWellJunction", Query.EQ("OUR_WELL_ID", w._id.ToString()));
                    if (j != null && String.IsNullOrEmpty(j.GetString("SHELL_WELL_ID")) == false)
                    {
                        w.EDMWellId = j.GetString("SHELL_WELL_ID");
                        w.EDMWellName = j.GetString("SHELL_WELL_NAME");
                    }
                    w.Save();
                }
                var q = Query.GT("EDMWellId", "");
                return WellInfo.Populate<WellInfo>(q);
            });
        }

        public JsonResult UpdateOPDays()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    foreach (var ph in wa.Phases)
                    {
                        if (ph.PhSchedule.Start > Tools.DefaultDate && ph.PhSchedule.Finish > Tools.DefaultDate)
                        {
                            ph.OP.Days = ph.PhSchedule.Days;
                        }
                    }
                    wa.Save();
                }
                return "OK";
            });
        }

        public JsonResult UpdateTheme()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var themes = DataHelper.Populate("_tmpTheme");
                var wps = WellPIP.Populate<WellPIP>();
                foreach (var wp in wps)
                {
                    foreach (var e in wp.Elements)
                    {
                        var t = themes.FirstOrDefault(d => d.GetString("_id").Equals(e.Classification));
                        if (t != null)
                        {
                            e.Theme = t.GetString("Theme");
                        }
                    }
                    wp.Save();
                }
                return "OK";
            });
        }

        public JsonResult Update()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    //var phIndex = 1;
                    //foreach (var ph in wa.Phases)
                    //{
                    //    ph.PhaseNo = phIndex;
                    //    phIndex++;
                    //}
                    wa.Save();
                }

                var waus = WellActivityUpdate.Populate<WellActivityUpdate>();
                foreach (var wau in waus)
                {
                    var wa = WellActivity.GetByUniqueKey(wau.WellName, wau.SequenceId);
                    if (wa != null)
                    {
                        var a = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(wau.Phase.ActivityType));
                        if (a != null)
                        {
                            wau.Phase.PhaseNo = a.PhaseNo;
                            wau.Save();
                        }
                    }
                }

                var pips = WellPIP.Populate<WellPIP>();
                foreach (var pip in pips)
                {
                    var wa = WellActivity.GetByUniqueKey(pip.WellName, pip.SequenceId);
                    if (wa != null)
                    {
                        var a = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(pip.ActivityType));
                        if (a != null)
                        {
                            pip.PhaseNo = a.PhaseNo;
                            pip.Save();
                        }
                    }
                }
                return "OK";
            });
        }

        public JsonResult PIP(string tablename = "")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    foreach (var a in wa.Phases)
                    {
                        var wp = WellPIP.GetByOpsActivity(wa.UARigSequenceId, a.PhaseNo);
                        if (wp == null)
                        {
                            wp = new WellPIP();
                            wp.SequenceId = wa.UARigSequenceId;
                            wp.PhaseNo = a.PhaseNo;
                            wp.ActivityType = a.ActivityType;
                            wp.WellName = wa.WellName;
                        }
                        var q = Query.And(
                                Query.EQ("ProjectInformation.Detail", wa.WellName),
                                Query.EQ("ProjectInformation.Detail", a.ActivityType)
                            );
                        wp.Elements.Clear();
                        var pip = DataHelper.Get(tablename, q);
                        if (pip != null && pip.HasElement("OpportunityRiskData"))
                        {
                            var els = pip.GetValue("OpportunityRiskData").AsBsonArray;
                            foreach (var el in els)
                            {
                                var eld = el.ToBsonDocument();
                                var pe = new PIPElement();
                                pe.ElementId = wp.Elements.Count() + 1;
                                pe.Title = eld.GetString("Idea");
                                pe.Classification = eld.GetString("Classification");
                                pe.DaysPlanImprovement = -eld.GetDouble("Schedule_Opportunity");
                                pe.DaysPlanRisk = eld.GetDouble("Schedule_Risk");
                                pe.CostPlanImprovement = -eld.GetDouble("Cost_Opportunity");
                                pe.CostPlanRisk = eld.GetDouble("Cost_Risk");
                                wp.Elements.Add(pe);
                            }
                        }
                        wp.Status = wp.Elements.Count > 0 ? "Publish" : "Draft";
                        wp.Save();
                    }
                }
                return "OK";
            });
        }
        public JsonResult PIPLoad(string tablename = "")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var pips = DataHelper.Populate(tablename)
                    .OrderBy(d=>d.GetString("Well_Name"))
                    .ThenBy(d=>d.GetString("Activity_Type"))
                    .ToList();
                string prevWellName = "", prevActType = "";
                bool saveIt = false;
                WellPIP wp = null;
                foreach (var pip in pips)
                {
                    //bool newStart = false;
                    var wellname = pip.GetString("Well_Name");
                    var actType = pip.GetString("Activity_Type");
                    if(prevWellName!=wellname || prevActType!=actType){
                        if(wp!=null && saveIt)wp.Save();
                        //newStart = true;
                        saveIt = true;
                        prevWellName = wellname;
                        prevActType = actType;
                        var qwas = new List<IMongoQuery>();
                        var SequenceId = "";
                        var PhaseNo = 0;
                        qwas.Add(Query.EQ("WellName", wellname));
                        qwas.Add(Query.EQ("Phases.ActivityType",actType));
                        var was = WellActivity.Populate<WellActivity>(Query.And(qwas))
                            .Select(d=>{
                                d.Phases = d.Phases.Where(e=>e.ActivityType.Equals(actType)).ToList();
                                return d;
                            })
                            //.OrderBy(d=>d.Phases[0].PlanSchedule.Finish)
                            .ToList();
                        if(was.Count>0){
                            SequenceId = was[0].UARigSequenceId;
                            PhaseNo = was[0].Phases[0].PhaseNo;
                        }

                        saveIt = false;
                        wp = WellPIP.GetByWellActivity(wellname, actType);
                        if(wp==null){
                            saveIt = true;
                            wp = new WellPIP();
                            wp.WellName = wellname;
                            wp.SequenceId = SequenceId;
                            wp.PhaseNo = PhaseNo;
                            wp.ActivityType = actType;
                            wp.Status = "Publish";
                            //wp.Elements.Clear();
                        }
                    }

                    if (wp != null)
                    {
                        var idea = pip.GetString("Idea");
                        var pe = wp.Elements.FirstOrDefault(d => d.Title.Equals(idea));
                        if (pe == null)
                        {
                            pe = new PIPElement();
                            pe.ElementId = wp.Elements.Count() + 1;
                            pe.Title = idea;
                            pe.PerformanceUnit = pip.GetString("Performance_Unit");
                            pe.Theme = pip.GetString("Theme");
                            pe.Classification = pip.GetString("High_Level_Classification");
                            pe.DaysPlanImprovement = pip.GetDouble("Opportunity__Days_(-)");
                            pe.DaysPlanRisk = pip.GetDouble("Risk__Days_(+)");
                            pe.CostPlanImprovement = pip.GetDouble("Opportunity_Cost_MM__(-)");
                            pe.CostPlanRisk = pip.GetDouble("Risk_Cost_MM_(+)");
                            pe.LECost = pe.CostActualImprovement + pe.CostActualRisk;
                            pe.LEDays = pe.DaysActualImprovement + pe.DaysActualRisk;
                            pe.Period = new DateRange(
                                    pip.GetDateTime("Activity_Start"),
                                    pip.GetDateTime("Activity_End")
                                );
                            wp.Elements.Add(pe);
                            pe.ResetAllocation();
                        }
                    }
                }
                if(wp!=null && saveIt)wp.Save();
                        
                return "OK";
            });
        }

        public JsonResult UpdatePIPPhaseNo()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var pips = WellPIP.Populate<WellPIP>();
                foreach (var pip in pips)
                {
                    pip.Delete();
                    var q = Query.And(
                            Query.EQ("WellName", pip.WellName),
                            Query.EQ("Phases.ActivityType", pip.ActivityType)
                        );
                    var wa = WellActivity.Get<WellActivity>(q);
                    if (wa != null)
                    {
                        var ph = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(pip.ActivityType));
                        if (ph != null)
                        {
                            pip.SequenceId = wa.UARigSequenceId;
                            pip.PhaseNo = ph.PhaseNo;
                            pip.Status = "Publish";
                            pip.Save();
                        }
                        else
                        {
                            pip.Status = "Draft";
                            pip.Save();
                        }
                    }
                    else
                    {
                        pip.Status = "Draft";
                        pip.Save();
                    }
                }
                return "OK";
            });
        }

        public JsonResult UpdateTQ()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    foreach (var ph in wa.Phases)
                    {
                        var q = Query.And(
                                Query.EQ("Well_Name", wa.WellName),
                                Query.EQ("Activity_Type\r\n(PTW_Buckets)", ph.ActivityType)
                            );
                        var op = DataHelper.Get("OP", q);
                        if (op != null)
                        {
                            var TQ_Days = op.GetDouble("DURATION_TQ");
                            var TQ_Cost = op.GetDouble("TQ_Cost");
                            if (TQ_Days > 0)
                                ph.TQ.Days = TQ_Days;
                            if (TQ_Cost > 0)
                                ph.TQ.Cost = TQ_Cost;
                        }
                    }
                    wa.Save();
                }
                return "OK";
            });
        }

        public JsonResult UpdatePlan()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    foreach (var ph in wa.Phases)
                    {
                        var q = Query.And(
                                Query.EQ("Well_Name", wa.WellName),
                                Query.EQ("Activity_Type\r\n(PTW_Buckets)", ph.ActivityType)
                            );
                        var op = DataHelper.Get("OP", q);
                        if (op != null)
                        {
                            var Days = op.GetDouble("DURATION_TOTAL_");
                            var Cost = op.GetDouble("COST_TOTAL_");
                            if (ph.Plan == null) ph.Plan = new WellDrillData();
                            if (ph.Plan.Days == 0) ph.Plan.Days = Days;
                            if (ph.Plan.Cost == 0) ph.Plan.Cost = Cost;
                        }
                    }
                    wa.Save();
                }
                return "OK";
            });
        }

        public JsonResult TestConvertActual()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {

                #region Process Read Last Actual
                DataHelper.Delete("_OracleActual_LastData");
                List<WellActivityActual> activityActuals = new List<WellActivityActual>();
                List<WellActivityActual> actMax = new List<WellActivityActual>();

                DateTime date = new DateTime(2015, 01, 05);

                var y = DataHelper.Populate("_OracleActual").Where(x => BsonHelper.GetDateTime(x, "REPORTDATE") >= date);

                List<string> wells = y.Select(x => BsonHelper.GetString(x, "WELLNAME")).Distinct().ToList();

                List<BsonDocument> LastUpdater = new List<BsonDocument>();  // last update single Wellname and Code

                foreach (string x in wells)
                {
                    var wellSameCode = y.Where(n => BsonHelper.GetString(n, "WELLNAME").Equals(x)).ToList();
                    List<string> codes = wellSameCode.Select(u => BsonHelper.GetString(u, "EVENTCODE")).Distinct().ToList();
                    foreach (string code in codes)
                    {
                        var lastwellCode = y.Where(n => BsonHelper.GetString(n, "WELLNAME").Equals(x) && BsonHelper.GetString(n, "EVENTCODE").Equals(code)).OrderByDescending(h => BsonHelper.GetDateTime(h, "REPORTDATE")).FirstOrDefault();
                        LastUpdater.Add(lastwellCode);
                        DataHelper.Save("_OracleActual_LastData", lastwellCode.ToBsonDocument());
                    }
                }

                #endregion

                DataHelper.Delete("_OracleActual_Failed");
                DataHelper.Delete("_OracleActual_Success");
                foreach (var t in y)
                {
                    var actv = WellActivityActual.Convert(t);

                    if (actv == null)
                    {
                        DataHelper.Save("_OracleActual_Failed", t);
                    }
                    else
                    {
                        actv.Save();
                    }
                }
                return "OK";
            });
        }

        public object ErrorAllocation(object PIPId, int ElementId,string WellName, string ActivityType, string note, string SequenceId)
        {
            var ret = new { PIPId = PIPId, ElementId = ElementId, WellName = WellName, ActivityType = ActivityType,SequenceId=SequenceId, Note = note };
            return ret;
        }

        public JsonResult CheckNotValidAllocations()
        {
            List<object> res = new List<object>();
            //var q = Query.EQ("WellName", "BRUTUS A4 ST6");
            //var PIPs = WellPIP.Populate<WellPIP>(q);
            var PIPs = WellPIP.Populate<WellPIP>();
            foreach (var pip in PIPs)
            {
                foreach (var element in pip.Elements)
                {
                    var PeriodStart = element.Period.Start.ToString("MM-yyyy");
                    var PeriodFinish = element.Period.Finish.ToString("MM-yyyy");
                    var numOfAllocation = element.Allocations.Count;
                    var AllocationStart = element.Allocations.OrderBy(x => x.Period).FirstOrDefault().Period.ToString("MM-yyyy");
                    var AllocationFinish = element.Allocations.OrderByDescending(x => x.Period).FirstOrDefault().Period.ToString("MM-yyyy");
                    var dt = element.Period.Start;
                    int mthNumber = 1;
                    var loop = true;
                    while (loop)
                    {
                        if (dt.ToString("MM-yyyy") == PeriodFinish)
                        {
                            loop = false;
                        }
                        else
                        {
                            mthNumber++;
                            dt = dt.AddMonths(1);
                        }
                    }
                    //Check number of allocation
                    if (mthNumber > 0)
                    {
                        if (numOfAllocation != mthNumber)
                        {
                            res.Add(ErrorAllocation(pip._id, element.ElementId, pip.WellName, pip.ActivityType, "Number of Allocation doesnt match with period!", pip.SequenceId));
                        }
                        else
                        {
                            //Check Allocation start
                            if (PeriodStart != AllocationStart)
                            {
                                res.Add(ErrorAllocation(pip._id, element.ElementId, pip.WellName, pip.ActivityType, "Allocation Start doesnt match with Element Period Start", pip.SequenceId));
                            }
                            else
                            {
                                //Check Allocation finish
                                if (PeriodFinish != AllocationFinish)
                                {
                                    res.Add(ErrorAllocation(pip._id, element.ElementId, pip.WellName, pip.ActivityType, "Allocation Finish doesnt match with Element Period Finish", pip.SequenceId));
                                }
                            }
                        }
                    }
                }
            }

            //return MvcTools.ToJsonResult(res);
            return Json(new { Total = res.Count, Data = res }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult FixNotValidAllocations()
        {
            List<object> res = new List<object>();
            //var q = Query.EQ("WellName", "BRUTUS A4 ST6");
            //var PIPs = WellPIP.Populate<WellPIP>(q);
            var PIPs = WellPIP.Populate<WellPIP>();
            foreach (var pip in PIPs)
            {
                foreach (var element in pip.Elements)
                {
                    var PeriodStart = element.Period.Start.ToString("MM-yyyy");
                    var PeriodFinish = element.Period.Finish.ToString("MM-yyyy");
                    var numOfAllocation = element.Allocations.Count;
                    var AllocationStart = element.Allocations.OrderBy(x => x.Period).FirstOrDefault().Period.ToString("MM-yyyy");
                    var AllocationFinish = element.Allocations.OrderByDescending(x => x.Period).FirstOrDefault().Period.ToString("MM-yyyy");
                    var dt = element.Period.Start;
                    int mthNumber = 1;
                    var loop = true;
                    while (loop)
                    {
                        if (dt.ToString("MM-yyyy") == PeriodFinish)
                        {
                            loop = false;
                        }
                        else
                        {
                            mthNumber++;
                            dt = dt.AddMonths(1);
                        }
                    }
                    //Check number of allocation
                    if (mthNumber > 0)
                    {
                        if (numOfAllocation != mthNumber)
                        {
                            res.Add(ErrorAllocation(pip._id, element.ElementId, pip.WellName, pip.ActivityType, "Number of Allocation doesnt match with period!", pip.SequenceId));
                        }
                        else
                        {
                            //Check Allocation start
                            if (PeriodStart != AllocationStart)
                            {
                                res.Add(ErrorAllocation(pip._id, element.ElementId, pip.WellName, pip.ActivityType, "Allocation Start doesnt match with Element Period Start", pip.SequenceId));
                            }
                            else
                            {
                                //Check Allocation finish
                                if (PeriodFinish != AllocationFinish)
                                {
                                    res.Add(ErrorAllocation(pip._id, element.ElementId, pip.WellName, pip.ActivityType, "Allocation Finish doesnt match with Element Period Finish", pip.SequenceId));
                                }
                            }
                        }
                    }
                }
            }

            foreach (var x in res)
            {
                ResetSingleAllocation(x.ToBsonDocument().GetString("WellName"), x.ToBsonDocument().GetString("ActivityType"), x.ToBsonDocument().GetString("SequenceId"), x.ToBsonDocument().GetInt32("ElementId"));
            }

            //return MvcTools.ToJsonResult(res);
            return Json(new { Total = res.Count, FixedAllocations = res }, JsonRequestBehavior.AllowGet);
        }

        public static void ResetSingleAllocation(string WellName, string ActivityType, string SequenceId, int ElementId = 0)
        {

            List<IMongoQuery> qs = new List<IMongoQuery>();
            IMongoQuery q = Query.Null;


            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("ActivityType", ActivityType));
            qs.Add(Query.EQ("SequenceId", SequenceId));
            q = Query.And(qs);

            List<PIPElement> ListElement = new List<PIPElement>();
            PIPElement Element = new PIPElement();
            List<PIPAllocation> Allocation = new List<PIPAllocation>();
            WellPIP NewPIP = new WellPIP();

            WellPIP pip = WellPIP.Get<WellPIP>(q);
            NewPIP = pip;
            foreach (var x in pip.Elements)
            {
                Element = x;
                if (ElementId != 0)
                {
                    if (x.ElementId == ElementId)
                        Element.Allocations = null;
                }
                ListElement.Add(Element);
            }
            NewPIP.Elements = ListElement;
            NewPIP.Save();
        }

        public JsonResult ResetAllocation()
        {
            return MvcResultInfo.Execute(() =>
            {
                #region reset single
                //string WellName = "RAM POWELL A7";
                //string SequenceId = "2670";
                //string ActivityType = "WHOLE COMPLETION EVENT";

                //List<IMongoQuery> qs = new List<IMongoQuery>();
                //IMongoQuery q = Query.Null;


                //qs.Add(Query.EQ("WellName", WellName));
                //qs.Add(Query.EQ("ActivityType", ActivityType));
                //qs.Add(Query.EQ("SequenceId", SequenceId));
                //q = Query.And(qs);

                //List<PIPElement> ListElement = new List<PIPElement>();
                //PIPElement Element = new PIPElement();
                //List<PIPAllocation> Allocation = new List<PIPAllocation>();
                //WellPIP NewPIP = new WellPIP();

                //WellPIP pip = WellPIP.Get<WellPIP>(q);
                //NewPIP = pip;
                //foreach (var x in pip.Elements)
                //{
                //    Element = x;
                //    Element.Allocations = null;
                //    ListElement.Add(Element);
                //}
                //NewPIP.Elements = ListElement;
                //NewPIP.Save();
                #endregion

                List<WellPIP> ListPIP = WellPIP.Populate<WellPIP>();
                WellPIP NewPIP = new WellPIP();
                PIPElement NewElement = new PIPElement();
                foreach (var x in ListPIP)
                {
                    NewPIP = x;
                    List<PIPElement> ListElement = new List<PIPElement>();
                    foreach (var y in x.Elements)
                    {
                        NewElement = y;
                        NewElement.Allocations = null;
                        ListElement.Add(NewElement);
                    }
                    NewPIP.Elements = ListElement;
                    NewPIP.Save();
                }
                return "OK";
            });
        }

        public JsonResult ResetEXTypeAllActivity()
        {
            return MvcResultInfo.Execute(() =>
            {
                List<WellActivity> WellActivities = WellPIP.Populate<WellActivity>();
                foreach (var x in WellActivities)
                {
                    x.EXType = "";
                    x.Save();
                }
                return "OK";
            });
        }

        public JsonResult ResetWellActivityUpdatePIP()
        {
            return MvcResultInfo.Execute(() =>
            {
                #region Old
                //List<WellActivityUpdate> ListWAU = WellActivityUpdate.Populate<WellActivityUpdate>();
                //WellActivityUpdate NewWAU = new WellActivityUpdate();
                //foreach (var x in ListWAU)
                //{
                //    NewWAU = x;
                //    List<IMongoQuery> qs = new List<IMongoQuery>();
                //    IMongoQuery q = Query.Null;
                //    qs.Add(Query.EQ("WellName", x.WellName));
                //    qs.Add(Query.EQ("ActivityType", x.Phase.ActivityType));
                //    qs.Add(Query.EQ("SequenceId", x.SequenceId));
                //    q = Query.And(qs);
                //    WellPIP PIP = WellPIP.Get<WellPIP>(q);
                //    if (PIP != null)
                //    {
                //        NewWAU.Elements = PIP.Elements;
                //        NewWAU.Save();
                //    }
                //}
                #endregion

                List<WellActivityUpdate> waus = WellActivityUpdate
                    .Populate<WellActivityUpdate>()
                    //.Where(d => d.WellName.Equals("RAM POWELL A7"))
                    .OrderBy(d => d.UpdateVersion)
                    .ToList();
                foreach (var x in waus)
                {
                    List<IMongoQuery> qs = new List<IMongoQuery>();
                    IMongoQuery q = Query.Null;
                    qs.Add(Query.EQ("WellName", x.WellName));
                    qs.Add(Query.EQ("ActivityType", x.Phase.ActivityType));
                    qs.Add(Query.EQ("SequenceId", x.SequenceId));
                    q = Query.And(qs);
                    WellPIP pip = WellPIP.Get<WellPIP>(q);
                    if (pip != null)
                    {
                        foreach (var e in x.Elements)
                        {
                            var pipElement = pip.Elements.FirstOrDefault(d => d.ElementId.Equals(e.ElementId));
                            if (pipElement != null)
                            {
                                e.Period = pipElement.Period;
                                e.Allocations = null;
                                e.ResetAllocation();
                                pipElement.DaysCurrentWeekImprovement = e.DaysCurrentWeekImprovement;
                                pipElement.DaysCurrentWeekRisk = e.DaysCurrentWeekRisk;
                                pipElement.CostCurrentWeekImprovement = e.CostCurrentWeekImprovement;
                                pipElement.CostCurrentWeekRisk = e.CostCurrentWeekRisk;
                                pipElement.Allocations = null;
                                pipElement.ResetAllocation();
                            }
                        }
                        pip.Save();
                    }
                    x.Save();
                }
                return "OK";
            });
        }

        public JsonResult UpdateUserPersonFromExcel(string logPath = @"D:\LogUser.txt", string inputpath = @"D:\Security Matrix (rev2).xlsx")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                #region 27012015 Update User Role, Security Matrix


                ECIS.Core.DataSerializer.ExtractionHelper e = new Core.DataSerializer.ExtractionHelper();
                var datauser = e.Extract(inputpath);

                System.Text.StringBuilder sb = new System.Text.StringBuilder();


                sb.Append("######## Update Security Matrix From Excel Files ######## \n");
                sb.Append("######## File Path : " + inputpath + " # \n");
                sb.Append("######## Log File Save To : " + logPath + " # \n\n\n");

                int i = 0;
                foreach (var t in datauser)
                {
                    i++;
                    string Rig_Name = BsonHelper.GetString(t, "Rig_Name");
                    string Well_Name = BsonHelper.GetString(t, "Well_Name");
                    Well_Name = Well_Name.Trim();
                    string u = "";

                    if (Well_Name.Equals("COUGAR A-01"))
                    {
                        u = "asda";
                        Console.WriteLine(Well_Name);

                    }
                    u = "sdasdas";
                    string Activity = BsonHelper.GetString(t, "Activity");
                    Activity = Activity.Trim();

                    string Role = BsonHelper.GetString(t, "Role");
                    Role = Role.Trim();

                    string User_Name = BsonHelper.GetString(t, "User_Name");
                    string Email = BsonHelper.GetString(t, "Email");
                    string Full_Name = BsonHelper.GetString(t, "Full_Name");

                    sb.Append("# --------- Processing ( " + i.ToString() + " ) ---------  #\n");
                    sb.Append("  Rigname  : " + Rig_Name + "\n");
                    sb.Append("  Well_Name  : " + Well_Name + "\n");
                    sb.Append("  Activity  : " + Activity + "\n");
                    sb.Append("  Role  : " + Role + "\n");
                    sb.Append("  User_Name  : " + User_Name + "\n");
                    sb.Append("  Email  : " + Email + "\n");
                    sb.Append("  Full_Name  : " + Full_Name + "\n");


                    // create WEISRoles
                    var role = WEISRole.Get<WEISRole>(Query.EQ("_id", Role));
                    if (role == null)
                    {
                        sb.Append("$ Saving new Role $" + "\n");
                        // insert new role 
                        WEISRole r = new WEISRole();
                        r._id = Role;
                        r.Title = Role;
                        r.RoleName = Role;
                        r.Save();
                    }
                    else
                    {
                        sb.Append("$ Role " + Role + " Already Exist in WEISRoles $" + "\n");
                    }

                    // create Users
                    var user = DataHelper.Populate("Users", Query.Matches("Email", new BsonRegularExpression(Email.Trim().ToLower(), "i")));
                    if (user == null || user.Count <= 0)
                    {
                        // new user
                        //Tools.
                        sb.Append("-- Saving new User --" + "\n");
                        var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                        var userMgr = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));
                        IdentityUser use = new IdentityUser();

                        use.UserName = User_Name;
                        use.Email = Email;
                        use.FullName = Full_Name;
                        use.Enable = true;
                        use.ADUser = false;
                        use.ConfirmedAtUtc = Tools.ToUTC(DateTime.Now);
                        use.HasChangePassword = false;

                        String hashpassword = userMgr.PasswordHasher.HashPassword(Tools.GenerateRandomString(8));
                        use.PasswordHash = hashpassword;

                        DataHelper.Save("Users", use.ToBsonDocument());

                    }
                    else
                    {
                        sb.Append("$ User " + User_Name + "  Already Exist in WEIS Users $" + "\n");
                    }

                    // update WEISPersons
                    List<IMongoQuery> qs = new List<IMongoQuery>();
                    qs.Add(Query.EQ("WellName", Well_Name.Trim()));
                    var wellActivity = DataHelper.Populate<WellActivity>("WEISWellActivities", Query.And(qs));
                    if (wellActivity != null && wellActivity.Count > 0)
                    {
                        string seqid = wellActivity.FirstOrDefault().UARigSequenceId;
                        var phase = wellActivity.FirstOrDefault().Phases.Where(x => x.ActivityType.Equals(Activity));
                        if (phase != null && phase.Count() > 0)
                        {
                            #region contains on phases
                            string actvName = phase.FirstOrDefault().ActivityType;
                            int PhaseNo = phase.FirstOrDefault().PhaseNo;
                            qs.Add(Query.EQ("SequenceId", seqid));
                            qs.Add(Query.EQ("ActivityType", actvName));
                            qs.Add(Query.EQ("PhaseNo", PhaseNo));
                            var persons = DataHelper.Populate<WEISPerson>("WEISPersons", Query.And(qs));

                            if (persons != null && persons.Count() > 0)
                            {
                                var person = persons.FirstOrDefault();
                                int personExist = person.PersonInfos.Where(x => x.Email.Trim().ToLower().Equals(Email.Trim().ToLower())).Count();
                                if (personExist <= 0)
                                {
                                    // add personInfo
                                    WEISPersonInfo pi = new WEISPersonInfo();
                                    pi.Email = Email;
                                    pi.FullName = Full_Name;
                                    pi.RoleId = Role;

                                    person.PersonInfos.Add(pi);
                                    person.Save();
                                    sb.Append("++ Adding PersonInfo : " + pi.Email + " to WEISPerson : " + person._id + "\n");
                                }
                                else
                                {
                                    sb.Append("&& Person Info already in WEISPerson" + person._id + "\n");
                                }
                            }
                            else
                            {
                                // tidak ada di PersonInfo
                                sb.Append("===========> No data Person Info in WEISPerson ||" + "\n");

                                sb.Append("Insert new WEISPerson : " + Email + "\n");
                                WEISPerson person = new WEISPerson();
                                person.SequenceId = seqid;
                                person.WellName = Well_Name;
                                person.ActivityType = Activity;
                                person.PhaseNo = PhaseNo;

                                WEISPersonInfo info = new WEISPersonInfo();
                                info.FullName = Full_Name;
                                info.Email = Email;
                                info.RoleId = Role;
                                person.PersonInfos.Add(info);// = new List<WEISPersonInfo>();

                                person.Save();
                            }
                            #endregion
                        }
                        else
                        {
                            #region No phases
                            // Tidak ada Phases yg name Type nya sama
                            sb.Append("===========> No Phases ++ Activity Type : " + Activity + "\n");
                            sb.Append("Insert  new Activity to WEISActivities : " + Activity + "\n");

                            ActivityMaster actvMaster = new ActivityMaster();
                            actvMaster._id = Activity;
                            actvMaster.EDMActivityId = "";
                            actvMaster.Save();

                            qs.Add(Query.EQ("ActivityType", Activity));
                            var persons = DataHelper.Populate<WEISPerson>("WEISPersons", Query.And(qs));
                            if (persons != null && persons.Count() > 0)
                            {
                                var person = persons.FirstOrDefault();
                                int personExist = person.PersonInfos.Where(x => x.Email.Trim().ToLower().Equals(Email.Trim().ToLower())).Count();
                                if (personExist <= 0)
                                {
                                    // add personInfo
                                    WEISPersonInfo pi = new WEISPersonInfo();
                                    pi.Email = Email;
                                    pi.FullName = Full_Name;
                                    pi.RoleId = Role;

                                    person.PersonInfos.Add(pi);
                                    person.Save();
                                    sb.Append("++ Adding PersonInfo : " + pi.Email + " to WEISPerson : " + person._id + "\n");
                                }
                                else
                                {
                                    sb.Append("&& Person Info already in WEISPerson " + person._id + "\n");
                                }
                            }
                            else
                            {
                                // tidak ada di PersonInfo
                                sb.Append("===========> No data Person Info in WEISPerson ||" + "\n");

                                sb.Append("Insert new WEISPerson : " + Email + "\n");
                                WEISPerson person = new WEISPerson();
                                person.SequenceId = seqid;
                                person.WellName = Well_Name;
                                person.ActivityType = Activity;
                                person.PhaseNo = 0;

                                WEISPersonInfo info = new WEISPersonInfo();
                                info.FullName = Full_Name;
                                info.Email = Email;
                                info.RoleId = Role;
                                person.PersonInfos.Add(info);// = new List<WEISPersonInfo>();

                                person.Save();
                            }
                            #endregion

                        }

                    }
                    else
                    {
                        #region dont have wellActivity
                        sb.Append("===========> No Data Well Name && : " + Well_Name + "\n");
                        sb.Append("Insert new Well Name to WEISWellNames : " + Well_Name + "\n");
                        var well = DataHelper.Populate<WellInfo>("WEISWellNames", Query.EQ("_id", Well_Name));
                        if (well == null || well.Count() <= 0)
                        {
                            WellInfo wellName = new WellInfo();
                            wellName._id = Well_Name;
                            wellName.Save();
                        }
                        sb.Append("Insert  new Activity to WEISActivities : " + Activity + "\n");
                        var actv = DataHelper.Populate<ActivityMaster>("WEISActivities", Query.EQ("_id", Activity));
                        if (actv == null || actv.Count() <= 0)
                        {
                            ActivityMaster actvmstr = new ActivityMaster();
                            actvmstr._id = Activity;
                            actvmstr.Save();
                        }


                        qs.Add(Query.EQ("ActivityType", Activity));
                        var persons = DataHelper.Populate<WEISPerson>("WEISPersons", Query.And(qs));

                        if (persons != null && persons.Count() > 0)
                        {
                            var person = persons.FirstOrDefault();
                            int personExist = person.PersonInfos.Where(x => x.Email.Trim().ToLower().Equals(Email.Trim().ToLower())).Count();
                            if (personExist <= 0)
                            {
                                // add personInfo
                                WEISPersonInfo pi = new WEISPersonInfo();
                                pi.Email = Email;
                                pi.FullName = Full_Name;
                                pi.RoleId = Role;

                                person.PersonInfos.Add(pi);
                                person.Save();
                                sb.Append("++ Adding PersonInfo : " + pi.Email + " to WEISPerson : " + person._id + "\n");
                            }
                            else
                            {
                                sb.Append("&& Person Info already in WEISPerson " + person._id + "\n");
                            }
                        }
                        else
                        {
                            // tidak ada di PersonInfo
                            sb.Append("===========> No data Person Info in WEISPerson ||" + "\n");

                            sb.Append("Insert new WEISPerson : " + Email + "\n");
                            WEISPerson person = new WEISPerson();
                            person.SequenceId = "";
                            person.WellName = Well_Name;
                            person.ActivityType = Activity;
                            person.PhaseNo = 0;

                            WEISPersonInfo info = new WEISPersonInfo();
                            info.FullName = Full_Name;
                            info.Email = Email;
                            info.RoleId = Role;
                            person.PersonInfos.Add(info);// = new List<WEISPersonInfo>();

                            person.Save();
                        }
                        #endregion
                    }
                    sb.Append("# --------- End Processing -----------  #" + "\n\n\n");

                }
                //File.WriteAllText(logPath, sb.ToString());
                #endregion

                return sb.ToString();
            });
        }

        public JsonResult ResetSingleAllocation()
        {
            return MvcResultInfo.Execute(() =>
            {
                #region reset single
                string WellName = "PRINCESS P8";
                string SequenceId = "3062";
                string ActivityType = "WHOLE COMPLETION EVENT";
                int ElementId = 3;

                List<IMongoQuery> qs = new List<IMongoQuery>();
                IMongoQuery q = Query.Null;


                qs.Add(Query.EQ("WellName", WellName));
                qs.Add(Query.EQ("ActivityType", ActivityType));
                qs.Add(Query.EQ("SequenceId", SequenceId));
                q = Query.And(qs);

                List<PIPElement> ListElement = new List<PIPElement>();
                PIPElement Element = new PIPElement();
                List<PIPAllocation> Allocation = new List<PIPAllocation>();
                WellPIP NewPIP = new WellPIP();

                WellPIP pip = WellPIP.Get<WellPIP>(q);
                NewPIP = pip;
                foreach (var x in pip.Elements)
                {
                    Element = x;
                    if (Element.ElementId == ElementId)
                    {
                        Element.Allocations = null;
                    }
                    ListElement.Add(Element);
                }
                NewPIP.Elements = ListElement;
                NewPIP.Save();

                #endregion
                return "OK";
            });
        }


        public JsonResult LoadLastestSequence()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                ECIS.Core.DataSerializer.ExtractionHelper e = new ECIS.Core.DataSerializer.ExtractionHelper();
                string Path = @"D:\Latest_Sequence_Feb_1_2015.xlsx";
                Aspose.Cells.Workbook wb = new Aspose.Cells.Workbook(Path);
                var latestsequence = e.ExtractWithReplace(wb, "A1", "I341", 0);
                DateTime d = new DateTime(2015, 02, 01);

                DataHelper.Delete("OP" + d.ToString("yyyyMM") + "V2");

                foreach (var t in latestsequence)
                {
                    DataHelper.Save("OP" + d.ToString("yyyyMM") + "V2", t);
                }

                var rows = DataHelper.Populate("OP201502V2");
                foreach (var y in rows)
                {
                    DateTime sd = new DateTime();
                    sd = DateTime.ParseExact(BsonHelper.GetString(y, "Start_Date"), "yyyy-MM-dd hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                    DateTime ed = new DateTime();
                    ed = DateTime.ParseExact(BsonHelper.GetString(y, "End_Date"), "yyyy-MM-dd hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                    y.Set("Start_Date", Tools.ToUTC(sd));
                    y.Set("End_Date", Tools.ToUTC(ed));
                    DataHelper.Save("OP" + d.ToString("yyyyMM") + "V2", y);
                }

                return latestsequence.ToString();
            });

        }

        public JsonResult BuildThemeNameTable()
        {
            var wp = WellPIP.Populate<WellPIP>()
                    .SelectMany(d => d.Elements, (d, e) => new
                    {
                        Theme = e.Theme
                    })
                    .Distinct()
                    .Select(x => x.Theme)
                    .ToList();

            foreach (var t in wp)
            {
                var theme = new WellPIPThemes();
                theme.Name = t;
                theme.Save();
            }
            return Json(wp, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ReFixElementAllocation()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var PIP = WellPIP.Populate<WellPIP>();
                List<string> result = new List<string>();
                foreach (var x in PIP)
                {
                    WellPIP wp = new WellPIP();
                    var WellName = x.WellName;
                    var SequenceId = x.SequenceId;
                    var PhaseNo = x.PhaseNo;

                    wp = x;

                    if (x.Elements.Count > 0)
                    {
                        wp.Elements = CheckElementAllocation(x.Elements);
                        wp.Save();
                    }
                    if (wp.Elements != x.Elements)
                    {
                        result.Add(WellName + "|" + x.ActivityType + "|" + SequenceId);
                    }
                

                    List<IMongoQuery> queries = new List<IMongoQuery>();
                    queries.Add(Query.EQ("WellName", WellName));
                    queries.Add(Query.EQ("SequenceId", SequenceId));
                    queries.Add(Query.EQ("Phase.PhaseNo", PhaseNo));
                    queries.Add(Query.EQ("Phase.ActivityType", x.ActivityType));
                    var queryWAU = Query.And(queries);

                    var wau = WellActivityUpdate.Populate<WellActivityUpdate>(queryWAU)
                                .OrderByDescending(d => d.UpdateVersion)
                                .FirstOrDefault();
                    if (wau != null)
                    {
                        if (wau.Elements.Count > 0)
                        {
                            wau.Elements = CheckElementAllocation(wau.Elements);
                            wau.Save();
                        }
                    }

                }

                return result;
            });
        }

        private static List<PIPElement> CheckElementAllocation(List<PIPElement> element)
        {
            List<PIPElement> NewElement = new List<PIPElement>();
            foreach (var e in element)
            {
                bool isEdited = false;
                PIPElement el = new PIPElement();
                el = e;
                if (e.Allocations.Count > 0)
                {
                    foreach (var alloc in e.Allocations)
                    {
                        if (alloc.LECost != 0 || alloc.LEDays != 0)
                        {
                            isEdited = true;
                        }
                    }
                }

                if(!isEdited){
                    if (e.Allocations.Count > 0)
                    {
                        el.Allocations = ReFixAllocation(e.Allocations);
                    }
                }
                NewElement.Add(el);
            }

            return NewElement;
        }

        private static List<PIPAllocation> ReFixAllocation(List<PIPAllocation> allocations)
        {
            List<PIPAllocation> NewAllocation = new List<PIPAllocation>();
            foreach (var a in allocations)
            {
                PIPAllocation alloc = new PIPAllocation();
                var LEDays = a.DaysPlanImprovement + a.DaysPlanRisk;
                var LECost = a.CostPlanImprovement + a.CostPlanRisk;
                alloc.DaysPlanImprovement = a.DaysPlanImprovement;
                alloc.DaysPlanRisk = a.DaysPlanRisk;
                alloc.CostPlanImprovement = a.CostPlanImprovement;
                alloc.CostPlanRisk = a.CostPlanRisk;
                alloc.LEDays = LEDays;
                alloc.LECost = LECost;
                alloc.AllocationID = a.AllocationID;
                alloc.Period = a.Period;
                NewAllocation.Add(alloc);
            }

            return NewAllocation;
        }

        public List<PIPAllocation> ResetAllocationWithoutLE(PIPElement Element)
        {
            var dt = Element.Period.Start;
            int mthNumber = 1;
            while (dt < Element.Period.Finish)
            {
                mthNumber++;
                dt = dt.AddMonths(1);
            }

            var CostPlanImprovement = Element.CostPlanImprovement;
            var CostPlanRisk = Element.CostPlanRisk;
            var DaysPlanImprovement = Element.DaysPlanImprovement;
            var DaysPlanRisk = Element.DaysPlanRisk;

            //if (DaysCurrentWeekImprovement == 0) DaysCurrentWeekImprovement = Element.DaysPlanImprovement;
            //if (DaysCurrentWeekRisk == 0) DaysCurrentWeekRisk = Element.DaysPlanRisk;
            //if (CostCurrentWeekImprovement == 0) CostCurrentWeekImprovement = Element.CostPlanImprovement;
            //if (CostCurrentWeekRisk == 0) CostCurrentWeekRisk = Element.CostPlanRisk;

            var newAllocations = new List<PIPAllocation>();
            var totalAlloc = new PIPAllocation();
            for (var mthIdx = 0; mthIdx < mthNumber; mthIdx++)
            {
                dt = Element.Period.Start.AddMonths(mthIdx);
                if (dt > Element.Period.Finish) dt = Element.Period.Finish;
                var newAlloc = Element.Allocations.FirstOrDefault(d => d.Period.Year.Equals(dt.Year) && d.Period.Month.Equals(dt.Month));
                if (newAlloc == null)
                {
                    newAlloc = new PIPAllocation
                    {
                        AllocationID = mthIdx + 1,
                        Period = dt,
                        LEDays = 0, //Math.Round(Tools.Div(DaysCurrentWeekImprovement + DaysCurrentWeekRisk, mthNumber), 1),
                        LECost = 0 //Math.Round(Tools.Div(CostCurrentWeekImprovement + CostCurrentWeekRisk, mthNumber), 1)
                    };
                }

                newAlloc.CostPlanImprovement = Tools.Div(CostPlanImprovement, mthNumber);
                newAlloc.CostPlanRisk = Tools.Div(CostPlanRisk, mthNumber);
                newAlloc.DaysPlanImprovement = Tools.Div(DaysPlanImprovement, mthNumber);
                newAlloc.DaysPlanRisk = Tools.Div(DaysPlanRisk, mthNumber);

                totalAlloc.CostPlanImprovement += newAlloc.CostPlanImprovement;
                totalAlloc.CostPlanRisk += newAlloc.CostPlanRisk;
                totalAlloc.DaysPlanImprovement += newAlloc.DaysPlanImprovement;
                totalAlloc.DaysPlanRisk += newAlloc.DaysPlanRisk;

                if (totalAlloc.CostPlanImprovement < CostPlanImprovement)
                {
                    newAlloc.CostPlanImprovement -= totalAlloc.CostPlanImprovement - CostPlanImprovement;
                    totalAlloc.CostPlanImprovement = CostPlanImprovement;
                }

                if (totalAlloc.CostPlanRisk > CostPlanRisk)
                {
                    newAlloc.CostPlanRisk -= totalAlloc.CostPlanRisk - CostPlanRisk;
                    totalAlloc.CostPlanRisk = CostPlanRisk;
                }

                if (totalAlloc.DaysPlanImprovement < DaysPlanImprovement)
                {
                    newAlloc.DaysPlanImprovement -= totalAlloc.DaysPlanImprovement - DaysPlanImprovement;
                    totalAlloc.DaysPlanImprovement = DaysPlanImprovement;
                }

                if (totalAlloc.DaysPlanRisk > DaysPlanRisk)
                {
                    newAlloc.DaysPlanRisk -= totalAlloc.DaysPlanRisk - DaysPlanRisk;
                    totalAlloc.DaysPlanRisk = DaysPlanRisk;
                }
                newAllocations.Add(newAlloc);

            }

            return newAllocations;

        }

        public JsonResult ResetPIPType()
        {
            var pip = WellPIP.Populate<WellPIP>();
            foreach (var x in pip)
            {
                var newpip = x;
                newpip.Type = "Efficient";
                newpip.Save();
            }
            return null;
        }
        public JsonResult ResetNullPIPType()
        {
            var pip = WellPIP.Populate<WellPIP>();
            foreach (var x in pip)
            {
                if (x.Type == null || x.Type == "")
                {
                    var newpip = x;
                    newpip.Type = "Efficient";
                    newpip.Save();
                }
            }
            return null;
        }

        public JsonResult ReFixElementAllocationWithoutLE()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                //List<IMongoQuery> q1 = new List<IMongoQuery>();
                //q1.Add(Query.EQ("WellName", "APPO PNE3"));
                //q1.Add(Query.EQ("Phases.ActivityType", "WHOLE DRILLING EVENT"));
                //var q2 = Query.And(q1);
                //var WA = WellActivity.Populate<WellActivity>(q2);
                var WA = WellActivity.Populate<WellActivity>();
                foreach (var eachWA in WA)
                {
                    var WellName = eachWA.WellName;
                    var SequenceId = eachWA.UARigSequenceId;
                    foreach (var phase in eachWA.Phases)
                    {
                        var PhaseNo = phase.PhaseNo;
                        var ActivityType = phase.ActivityType;

                        List<IMongoQuery> queries = new List<IMongoQuery>();
                        queries.Add(Query.EQ("WellName", WellName));
                        queries.Add(Query.EQ("SequenceId", SequenceId));
                        queries.Add(Query.EQ("PhaseNo", PhaseNo));
                        queries.Add(Query.EQ("ActivityType", ActivityType));
                        var query = Query.And(queries);

                        #region PIP

                        var PIP = WellPIP.Get<WellPIP>(query);
                        var newPIP = PIP;
                        var NewPIPElements = new List<PIPElement>();
                        if (PIP != null)
                        {
                            if (PIP.Elements != null && PIP.Elements.Count > 0)
                            {
                                foreach (var el in PIP.Elements)
                                {
                                    var NewElement = el;
                                    NewElement.Allocations = ResetAllocationWithoutLE(el);
                                    NewPIPElements.Add(NewElement);
                                }
                                newPIP.Elements = NewPIPElements;

                                DataHelper.Save(PIP.TableName, newPIP.ToBsonDocument());
                            }
                        }
                        #endregion

                        List<IMongoQuery> queriesWAU = new List<IMongoQuery>();
                        queriesWAU.Add(Query.EQ("WellName", WellName));
                        queriesWAU.Add(Query.EQ("SequenceId", SequenceId));
                        queriesWAU.Add(Query.EQ("Phase.PhaseNo", PhaseNo));
                        queriesWAU.Add(Query.EQ("Phase.ActivityType", ActivityType));
                        var queryWAU = Query.And(queriesWAU);

                        var WAU = WellActivityUpdate.Populate<WellActivityUpdate>(queryWAU)
                                    .OrderByDescending(x => x.UpdateVersion);
                        if (WAU != null && WAU.Count() > 0)
                        {
                            var LatestWAU = WAU.FirstOrDefault();
                            if (LatestWAU.Elements != null && LatestWAU.Elements.Count > 0)
                            {
                                //LatestWAU.Elements = NewPIPElements;
                                foreach (var t in NewPIPElements)
                                {
                                    LatestWAU.Elements.Add(t);
                                }
                            }
                            else
                            {
                                LatestWAU.Elements = new List<PIPElement>();
                                foreach (var t in NewPIPElements)
                                {
                                    LatestWAU.Elements.Add(t);
                                }
                                //LatestWAU.Elements = NewPIPElements;
                            }
                            DataHelper.Save(LatestWAU.TableName, LatestWAU.ToBsonDocument());
                        }


                    }
                }

                return "ok";
                
                
                //var PIP = WellPIP.Populate<WellPIP>();
                //List<string> result = new List<string>();
                //foreach (var x in PIP)
                //{
                //    WellPIP wp = new WellPIP();
                //    var WellName = x.WellName;
                //    var SequenceId = x.SequenceId;
                //    var PhaseNo = x.PhaseNo;

                //    wp = x;

                //    if (x.Elements.Count > 0)
                //    {
                //        wp.Elements = CheckElementAllocation(x.Elements);
                //        wp.Save();
                //    }
                //    if (wp.Elements != x.Elements)
                //    {
                //        result.Add(WellName + "|" + x.ActivityType + "|" + SequenceId);
                //    }


                //    List<IMongoQuery> queries = new List<IMongoQuery>();
                //    queries.Add(Query.EQ("WellName", WellName));
                //    queries.Add(Query.EQ("SequenceId", SequenceId));
                //    queries.Add(Query.EQ("Phase.PhaseNo", PhaseNo));
                //    queries.Add(Query.EQ("Phase.ActivityType", x.ActivityType));
                //    var queryWAU = Query.And(queries);

                //    var wau = WellActivityUpdate.Populate<WellActivityUpdate>(queryWAU)
                //                .OrderByDescending(d => d.UpdateVersion)
                //                .FirstOrDefault();
                //    if (wau != null)
                //    {
                //        if (wau.Elements.Count > 0)
                //        {
                //            wau.Elements = CheckElementAllocation(wau.Elements);
                //            wau.Save();
                //        }
                //    }

                //}

                //return result;
            });
        }

        public JsonResult TestDistribute(List<string> ids)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                
                Dictionary<string, string> variables = new Dictionary<string, string>();
                //variables.Add("WellName", wa.WellName);
                //variables.Add("ActivityType", wa.Phase.ActivityType);
                //variables.Add("UpdateVersion", String.Format("{0:dd-MMM-yyyy}", wa.UpdateVersion));
                //variables.Add("DueDate", String.Format("{0:dd-MMM-yyyy}", wa.UpdateVersion));

                List<string> toEmails = new List<string>() { "eky.pradhana@eaciit.com", "yoga.bangkit@eaciit.com", "noval.agung@eaciit.com" };
                List<string> ccEmails = new List<string>() { "eky.pradhana@eaciit.com", "yoga.bangkit@eaciit.com", "noval.agung@eaciit.com" };

                variables.Add("UrlAction", "http://" + Request.Url.Host + Url.Action("index", "weeklyreport"));
                variables.Add("List", "WellName");
                ECIS.Client.WEIS.Email.Send("WRDistribute",
                        toEmails.ToArray(),
                        ccMails: ccEmails.ToArray(),
                        variables: variables,
                        //attachments: filenames,
                        //developerModeEmail: "eky.pradhana@eaciit.com");
                        developerModeEmail: WebTools.LoginUser.Email);
                return "OK";
            });
        }

    }

    public class EmailModel
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool TLS { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string SendEmail()
        {
            String ret = "OK";
            try
            {
                ResultInfo ri = Tools.SendMail(From, Subject, Message, Host, Port, UserName, Password, TLS, new string[] { To });
                if (ri.Result != "OK") ret = ri.Message;
            }
            catch (Exception e)
            {
                ret = Tools.PushException(e);
            }
            Status = ret;
            return ret;
        }
    }
}