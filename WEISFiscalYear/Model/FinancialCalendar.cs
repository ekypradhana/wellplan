using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECIS.Client.WEIS;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace WEISFiscalYear.Model
{
    class FinancialCalendar
    {
        public string LogPath { get; set; }
        public void WriteLog(string Text = null)
        {
            var Now = DateTime.Now;
            var ST = String.Format("{0} : {1}", String.Format("{0:dd-MMM-yyyy  HH:mm:ss}", Now), Text);

            Console.WriteLine(ST);
            File.AppendAllText(LogPath, ST + Environment.NewLine);
        }
        public void RunFiscal(DateTime Now, string fileNameLog)
        {
            LogPath = fileNameLog;
            var sampleWAUM = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(null);
            var now = new DateTime(Now.Year, Now.Month, 1);
            var monthYear = Tools.ToUTC(new DateTime(now.Year, now.Month, now.Day));
            var rawWellActivityIds = GetActivitiesBasedOnMonth(monthYear);
            if (sampleWAUM == null)
            {
                var wellActivityIds = rawWellActivityIds.Select(d => d._id + "|" + d.ActivityType).ToList();
                MonthlyReportInitiate(monthYear, "", wellActivityIds, false, true);
            }
            else
            {
                var lastUpdateVersion = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q: null, take: 1, sort: SortBy.Descending("UpdateVersion"));
                var prevMonthlyReports = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(Query.EQ("UpdateVersion", lastUpdateVersion.FirstOrDefault().UpdateVersion));

                //save the old registered activities   ---  TAKE ELEMENT AND CRELEMENT FRESH FROM PIP
                var wausDef = new List<WellActivityUpdate>();
                var pips = new List<WellPIP>();
                var wellActs = new List<WellActivity>();
                if (prevMonthlyReports.Any())
                {
                    var wells = prevMonthlyReports.Select(x => x.WellName).ToList();
                    var seqs = prevMonthlyReports.Select(x => x.SequenceId).ToList();
                    var activities = prevMonthlyReports.Select(x => x.Phase.ActivityType).ToList();
                    wausDef = WellActivityUpdate.Populate<WellActivityUpdate>(Query.And(Query.In("WellName", new BsonArray(wells)), Query.EQ("SequenceId", new BsonArray(seqs)), Query.EQ("Phase.ActivityType", new BsonArray(activities))));
                    pips = WellPIP.Populate<WellPIP>(Query.And(Query.In("WellName", new BsonArray(wells)), Query.EQ("SequenceId", new BsonArray(seqs)), Query.EQ("ActivityType", new BsonArray(activities))));
                    wellActs = WellActivity.Populate<WellActivity>(Query.And(Query.In("WellName", new BsonArray(wells)), Query.EQ("UARigSequenceId", new BsonArray(seqs)), Query.EQ("Phases.ActivityType", new BsonArray(activities))));
                }
                
                foreach (var d in prevMonthlyReports)
                {
                    var weeklyActivityUpdate =
                        wausDef.FirstOrDefault(
                            x =>
                                x.WellName.Equals(d.WellName) && x.SequenceId.Equals(d.SequenceId) &&
                                x.Phase.ActivityType.Equals(d.Phase.ActivityType));
                    //WellActivityUpdate.Get<WellActivityUpdate>(Query.And(Query.EQ("WellName", d.WellName), Query.EQ("SequenceId", d.SequenceId), Query.EQ("Phase.ActivityType", d.Phase.ActivityType)));
                    List<PIPElement> PIPElement = new List<PIPElement>();
                    List<PIPElement> CRElements = new List<PIPElement>();

                    #region Take Element and CR Element from PIP
                    //List<IMongoQuery> qs = new List<IMongoQuery>();
                    //IMongoQuery qpip = Query.Null;
                    //qs.Add(Query.EQ("WellName", d.WellName));
                    //qs.Add(Query.EQ("SequenceId", d.SequenceId));
                    //qs.Add(Query.EQ("ActivityType", d.Phase.ActivityType));
                    //qpip = Query.And(qs);
                    WellPIP pip =
                        pips.FirstOrDefault(
                            x =>
                                x.WellName.Equals(d.WellName) && x.SequenceId.Equals(d.SequenceId) &&
                                x.ActivityType.Equals(d.Phase.ActivityType));
                    //WellPIP.Get<WellPIP>(qpip);

                    var ggg = wellActs.FirstOrDefault(x => x.WellName.Equals(d.WellName) && x.UARigSequenceId.Equals(d.SequenceId));//&& x.ActivityType.Equals(d.Phase.ActivityType)

                    if (ggg != null)
                    {
                        var tp = ggg.Phases.FirstOrDefault(x => x.ActivityType == d.Phase.ActivityType);
                        if (tp != null)
                        {
                            if (d.Phase.PhSchedule.Start <= Tools.DefaultDate)
                                d.Phase.PhSchedule = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().PhSchedule;

                            if (d.Phase.PlanSchedule.Start <= Tools.DefaultDate)
                                d.Phase.PlanSchedule = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().PlanSchedule;

                            if (d.Phase.LESchedule.Start <= Tools.DefaultDate)
                                d.Phase.LESchedule = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().LESchedule;

                            if (d.Phase.AFESchedule.Start <= Tools.DefaultDate)
                                d.Phase.AFESchedule = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().AFESchedule;

                            //d.CurrentWeek = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().LE;
                            //d.Plan = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().Plan;
                            //d.OP = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().OP;
                            //d.AFE = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().AFE;
                            //d.TQ = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().TQ;
                            //d.Phase.TQ = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().TQ;
                            //d.Phase.AFE = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().AFE;
                            //d.Phase.AggredTarget = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().AggredTarget;
                        }
                    }


                    //if (d.CurrentWeek != )
                    //    d.Phase.LESchedule = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().LESchedule;


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
                    #endregion

                    d._id = null;
                    d.UpdateVersion = monthYear;
                    d.Status = "In-Progress";
                    d.InitiateDate = Tools.ToUTC(DateTime.Now);
                    if (PIPElement.Any())
                        d.Elements = PIPElement;
                    if (CRElements.Any())
                        d.CRElements = CRElements;

                    //if (!d.CRElements.Any())
                    //{
                    //    d.CRElements = GetCRElementsFromExisting(d.WellName, d.SequenceId, d.Phase.ActivityType);
                    //}

                    if (weeklyActivityUpdate == null && d.Phase.PhSchedule.Start > Tools.DefaultDate &&
                        d.Phase.PhSchedule.Finish > Tools.DefaultDate && !d.Phase.ActivityType.Contains("FLOWBACK"))
                    {
                        WriteLog(String.Format("Record Updated on Monthly LE. \n "+
                            "Well Name     : {0} \n "+
                            "Activity Type : {1} \n "+
                            "Rig Sequence  : {2}", d.WellName, d.Phase.ActivityType, d.SequenceId));
                        d.Save(references: new string[] { });
                    }
                };


                // looking for new activity
                lastUpdateVersion = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q: null, take: 1, sort: SortBy.Descending("UpdateVersion"));
                prevMonthlyReports = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(Query.EQ("UpdateVersion", lastUpdateVersion.FirstOrDefault().UpdateVersion));
                var newActivity = new List<MonthlyEventsBasedOnMonth>();
                foreach (var xx in rawWellActivityIds)
                {
                    var finder = prevMonthlyReports.FirstOrDefault(x => x.WellName == xx.WellName && x.Phase.ActivityType == xx.ActivityType && x.SequenceId == xx.UARigSequenceId);
                    if (finder == null)
                        newActivity.Add(xx);
                }
                //saving the new activities
                if (newActivity.Any())
                {
                    var wellActivityIds = newActivity.Select(d => d._id + "|" + d.ActivityType).ToList();
                    MonthlyReportInitiate(monthYear, "", wellActivityIds, false, true);
                }
            }
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

        public List<WellActivityUpdateMonthly> MonthlyReportInitiate(DateTime StartDate, string StartComment, List<String> WellActivityIds, bool withSendMail = false, bool isFromInitiate = false, string pUser = null, string pEmail = null)
        {
            //List<int> WellActvID = new List<int>();
            //string[] _id = WellActivityId.Split(',');
            var dt = StartDate;// Tools.GetNearestDay(Tools.ToDateTime(String.Format("{0:dd-MMM-yyyy}", StartDate), true), DayOfWeek.Monday);
            var qwaus = new List<IMongoQuery>();
            qwaus.Add(Query.EQ("UpdateVersion", dt));
            var q = qwaus.Count == 0 ? Query.Null : Query.And(qwaus);
            List<WellActivityUpdateMonthly> waus = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q);

            //if (WellActivityIds.Any())
            //{
            //    WellActivityIds.Select(x => x.)
            //}
            var was = WellActivity.Populate<WellActivity>();
            var wellPIPs = WellPIP.Populate<WellPIP>();

            foreach (string word in WellActivityIds)
            {
                string[] ids = word.Split('|');
                int waId = Tools.ToInt32(ids[0]);
                string activityType = ids[1];
                WellActivity wa = was.FirstOrDefault(x => x._id.Equals(waId));//WellActivity.Get<WellActivity>(waId);

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
                WellPIP pip = wellPIPs.FirstOrDefault(x => x._id.Equals(qpip));//WellPIP.Get<WellPIP>(qpip);
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
                    Dictionary<string, string> variables = new Dictionary<string, string>();
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
                            WriteLog(String.Format("No Record on Monthly Update. Fresh From PIP \n "+
                                "Well Name     : {0} \n "+
                                "Activity Type : {1} \n "+
                                "Rig Sequence  : {2}", wau.WellName, wau.Phase.ActivityType, wau.SequenceId));
                            wau.Save(references: new string[] { });//"initiateProcess"

                            //if (withSendMail)
                            //{
                            //    variables.Add("WellName", wa.WellName);
                            //    variables.Add("ActivityType", phase.ActivityType);
                            //    variables.Add("UpdateVersion", String.Format("{0:dd-MMM-yyyy}", dt));
                            //    variables.Add("DueDate", String.Format("{0:dd-MMM-yyyy}", dt));
                            //    variables.Add("UrlAction", "http://" + Request.Url.Host + Url.Action("index", "weeklyreport"));
                            //    Email.Send("WRInitiate",
                            //        persons.Select(d => d.Email).ToArray(),
                            //        ccs.Count == 0 ? null : ccs.Select(d => d.Email).ToArray(),
                            //        variables: variables,
                            //        developerModeEmail: pEmail);
                            //    waus.Add(wau);
                            //}
                        }

                    }
                    else
                    {
                        WriteLog(String.Format("Record exist on Monthly Update \n Well Name: {0} \n Activity Type : {1} \n Rig Sequence : {2}", wau.WellName, wau.Phase.ActivityType, wau.SequenceId));
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
    }
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