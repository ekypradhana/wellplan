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
using MongoDB.Bson.Serialization;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class WellPIPController : Controller
    {
        // GET: Shell/AdminWell
        public ActionResult Index()
        {
            var appIssue = new ECIS.AppServer.Areas.Issue.Controllers.AppIssueController();
            ViewBag.IsAdministrator = appIssue.isRole("ADMINISTRATORS");
            ViewBag.IsAppSupport = appIssue.isRole("APP-SUPPORTS");

            ViewBag.isReadOnly = "0";
            if (_isReadOnly())
            {
                ViewBag.isRO = "1";
            }

            ViewBag.isAdmin = "0";
            if (_isAdmin())
            {
                ViewBag.isAdmin = "1";
            }


            return View();
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

        public JsonResult Search(SearchParams ps)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                List<IMongoQuery> qs = new List<IMongoQuery>();
                qs.Add(WebTools.GetWellActivitiesQuery("WellName", ""));
                if (ps.operatingUnits != null && ps.operatingUnits.Count > 0) qs.Add(Query.In("OperatingUnit", new BsonArray(ps.operatingUnits)));
                if (ps.rigTypes != null && ps.rigTypes.Count > 0) qs.Add(Query.In("RigType", new BsonArray(ps.rigTypes)));
                if (ps.rigNames != null && ps.rigNames.Count > 0) qs.Add(Query.In("RigName", new BsonArray(ps.rigNames)));
                if (ps.projectNames != null && ps.projectNames.Count > 0) qs.Add(Query.In("ProjectName", new BsonArray(ps.projectNames)));
                if (ps.wellNames != null && ps.wellNames.Count > 0) qs.Add(Query.In("WellName", new BsonArray(ps.wellNames)));
                if (ps.activities != null && ps.activities.Count > 0) qs.Add(Query.In("Phases.ActivityType", new BsonArray(ps.activities)));

                string NoneWellMatchWithEXType = "";

                if (ps.exType != null && ps.exType.Count > 0)
                {
                    //qs.Add(Query.In("EXType", new BsonArray(ps.exType)));

                    if (qs.Contains(Query.In("WellName", new BsonArray(ps.wellNames))))
                    {
                        qs.Remove(Query.In("WellName", new BsonArray(ps.wellNames)));
                        string[] wellInculde = WellActivity.Populate<WellActivity>(Query.In("EXType", new BsonArray(ps.exType))).Select(x => x.WellName).ToArray();
                        List<string> wellPick = new List<string>();
                        foreach (string w in ps.wellNames)
                        {
                            if (wellInculde.Contains(w))
                            {
                                wellPick.Add(w);
                            }
                        }
                        if (wellPick.Count > 0)
                            qs.Add(Query.In("WellName", new BsonArray(wellPick)));
                        else
                        {
                            NoneWellMatchWithEXType = "NoneWellMatchWithEXType";
                            qs.Add(Query.EQ("WellName", NoneWellMatchWithEXType));
                        }
                    }
                    else
                    {
                        string[] wellInculde = WellActivity.Populate<WellActivity>(Query.In("EXType", new BsonArray(ps.exType))).Select(x => x.WellName).ToArray();
                        qs.Add(Query.In("WellName", new BsonArray(wellInculde)));
                    }
                }

                var q = qs.Count == 0 ? Query.Null : Query.And(qs);
                var wells = new List<BsonDocument>();
                if (q != Query.Null)
                {
                    var aggrArgs = new List<BsonDocument>();
                    aggrArgs.Add(new BsonDocument().Set("$match", q.ToBsonDocument()));
                    aggrArgs.Add(new BsonDocument().Set("$group", BsonSerializer.Deserialize<BsonDocument>("{_id: {'WellName':'$WellName','RigName':'$RigName','SequenceId':'$UARigSequenceId'} }")));
                    wells = DataHelper.Aggregate(new WellActivity().TableName, aggrArgs)
                        .Select(d =>
                            new
                            {
                                WellName = d.GetString("_id.WellName"),
                                RigName = d.GetString("_id.RigName"),
                                SequenceId = d.GetString("_id.SequenceId")
                            }.ToBsonDocument()
                        ).ToList();
                }

                List<string> WellName_CR = new List<string>();
                var ret = new List<WellPIP>();
                if (wells.Count > 0)
                {
                    foreach (var x in wells)
                    {
                        var w = x.GetString("WellName");
                        var SeqId = x.GetString("SequenceId");
                        List<IMongoQuery> queriesForPIP = new List<IMongoQuery>();
                        queriesForPIP.Add(Query.EQ("WellName", w));
                        queriesForPIP.Add(Query.EQ("SequenceId", SeqId));
                        if (ps.activities != null && ps.activities.Count > 0)
                            queriesForPIP.Add(Query.In("ActivityType", new BsonArray(ps.activities)));
                        if (ps.performanceUnits != null && ps.performanceUnits.Count > 0)
                            queriesForPIP.Add(Query.In("Elements.PerformanceUnit", new BsonArray(ps.performanceUnits)));
                        IMongoQuery query = Query.And(queriesForPIP.ToArray());

                        ret.AddRange(WellPIP.Populate<WellPIP>(query,
                            fields: new string[] { "WellName", "SequenceId", "Version", "Status", "ActivityType", "_id", "PhaseNo", "Type", "Elements" }));

                        WellName_CR.Add(x.GetString("RigName") + "_CR");

                    }



                }
                else
                {
                    if (!NoneWellMatchWithEXType.Trim().Equals("NoneWellMatchWithEXType"))
                    {
                        IMongoQuery queryForPIP = null;
                        if (ps.performanceUnits != null && ps.performanceUnits.Count > 0)
                            queryForPIP = Query.In("Elements.PerformanceUnit", new BsonArray(ps.performanceUnits));
                        ret = WellPIP.Populate<WellPIP>(queryForPIP,
                            fields: new string[] { "WellName", "SequenceId", "Status", "Version", "ActivityType", "_id", "PhaseNo", "Type", "Elements" });
                    }
                    else
                    {
                        ret = new List<WellPIP>();
                    }
                }



                //var searchCR = false;
                //IMongoQuery query_CR = Query.Null;
                //if (ps.rigNames != null && ps.rigNames.Count > 0)
                //{
                //    List<string> WellNames_CR = new List<string>();
                //    foreach (var x in ps.rigNames)
                //    {
                //        WellNames_CR.Add(x + "_CR");
                //    }

                //    query_CR = Query.In("WellName", new BsonArray(WellNames_CR));
                //}
                //else
                //{
                //    query_CR = Query.Matches("WellName", new BsonRegularExpression("_CR"));
                //}

                if (WellName_CR != null && WellName_CR.Count > 0)
                {
                    var query_CR = Query.In("WellName", new BsonArray(WellName_CR));
                    List<WellPIP> getPIPCR = WellPIP.Populate<WellPIP>(query_CR);
                    if (getPIPCR != null && getPIPCR.Count > 0)
                    {
                        foreach (var x in getPIPCR)
                        {
                            ret.Add(x);
                        }
                    }
                }

                var final = ret.OrderBy(w => w._id).ToList();
                if (ps.PIPType != "All")
                {
                    final = ret.Where(x => x.Type.Equals(ps.PIPType)).OrderBy(w => w._id).ToList();
                }


                //var final = ret.OrderBy(w => w._id).ToList();
                List<WellPIP> wip = new List<WellPIP>();

                foreach (var x in final)
                {
                    List<IMongoQuery> abc = new List<IMongoQuery>();
                    var WellName = x.WellName;
                    var SequenceId = x.SequenceId;
                    var ActivityType = x.ActivityType;
                    abc.Add(Query.EQ("WellName", WellName));
                    //abc.Add(Query.EQ("ActivityType", ActivityType));
                    abc.Add(Query.EQ("UARigSequenceId", SequenceId));
                    IMongoQuery ab = Query.And(abc.ToArray());
                    var getWA = WellActivity.Get<WellActivity>(ab);

                    if (getWA != null)
                    {
                        var PhaseNo = x.PhaseNo;
                        var getPhase = getWA.Phases.Where(a => a.PhaseNo == PhaseNo).FirstOrDefault();
                        if (getPhase != null)
                        {
                            x.OPSchedule = getPhase != null ? getPhase.PhSchedule : null;
                            x.AFESchedule = getPhase != null ? getPhase.AFESchedule : null;

                            if (getPhase.PhSchedule == null) getPhase.PhSchedule = new DateRange();
                            x.OPStart = getPhase != null ? getPhase.PhSchedule.Start : DateTime.MinValue;
                            x.OPFinish = getPhase != null ? getPhase.PhSchedule.Finish : DateTime.MinValue;

                            if (getPhase.AFESchedule == null) getPhase.AFESchedule = new DateRange();
                            x.AFEStart = getPhase != null ? getPhase.AFESchedule.Start : DateTime.MinValue;
                            x.AFEFinish = getPhase != null ? getPhase.AFESchedule.Finish : DateTime.MinValue;
                        }
                    }
                    else
                    {
                        if (x.OPSchedule == null) x.OPSchedule = new DateRange();
                        if (x.AFESchedule == null) x.AFESchedule = new DateRange();
                        x.OPSchedule.Start = DateTime.MinValue;
                        x.OPSchedule.Finish = DateTime.MinValue;
                        x.AFESchedule.Start = DateTime.MinValue;
                        x.AFESchedule.Finish = DateTime.MinValue;

                        x.OPStart = DateTime.MinValue;
                        x.OPFinish = DateTime.MinValue;
                        x.AFEStart = DateTime.MinValue;
                        x.AFEFinish = DateTime.MinValue;

                    }
                }


                return final;
            });
        }

        public JsonResult SearchForAddNew(List<string> WellNames, List<string> WellActivityIds, List<string> RigNames, string type)
        {

            List<WellActivity> was = new List<WellActivity>();
            if (type == "Efficient")
            {
                var q = Query.Null;
                List<IMongoQuery> qs = new List<IMongoQuery>();
                if (WellNames != null) qs.Add(Query.In("WellName", new BsonArray(WellNames)));
                if (WellActivityIds != null) qs.Add(Query.In("Phases.ActivityType", new BsonArray(WellActivityIds)));
                if (qs.Count > 0) q = Query.And(qs);

                List<WellActivity> wa = WellActivity.Populate<WellActivity>(q);
                foreach (var x in wa)
                {
                    var WellName = x.WellName;
                    var SequenceId = x.UARigSequenceId;
                    foreach (var y in x.Phases)
                    {
                        var PhaseNo = y.PhaseNo;
                        var ActivityType = y.ActivityType;
                        var a = Query.Null;
                        List<IMongoQuery> ab = new List<IMongoQuery>();
                        ab.Add(Query.EQ("WellName", WellName));
                        ab.Add(Query.EQ("SequenceId", SequenceId));
                        ab.Add(Query.EQ("PhaseNo", PhaseNo));
                        ab.Add(Query.EQ("ActivityType", ActivityType));
                        a = Query.And(ab.ToArray());
                        WellPIP CheckPIP = WellPIP.Get<WellPIP>(a);
                        if (CheckPIP == null)
                        {
                            WellActivity c = new WellActivity();
                            c._id = x._id;
                            c.PhaseNo = y.PhaseNo;
                            c.WellName = WellName;
                            c.UARigSequenceId = SequenceId;
                            c.ActivityType = ActivityType;
                            was.Add(c);
                        }
                    }
                }
            }
            else
            {
                if (RigNames != null && RigNames.Count > 0)
                {
                    foreach (var x in RigNames)
                    {
                        WellPIP CheckPIP = WellPIP.Get<WellPIP>(Query.EQ("WellName", x + "_CR"));
                        if (CheckPIP == null)
                        {
                            WellActivity c = new WellActivity();
                            c.WellName = x + "_CR";
                            c.UARigSequenceId = "";
                            c.ActivityType = "";
                            c.PhaseNo = 0;
                            was.Add(c);
                        }
                    }
                }
            }
            return Json(new { Success = true, Data = was.ToArray() }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CreatePIPDoc(int _id, int PhaseNo)
        {
            var wa = WellActivity.Get<WellActivity>(_id);
            WellPIP wp = new WellPIP();
            WellActivityPhase Phase = new WellActivityPhase();

            Phase = wa.Phases.Where(x => x.PhaseNo == PhaseNo).FirstOrDefault();

            string WellName = wa.WellName;
            string SequenceId = wa.UARigSequenceId;
            string ActivityType = Phase.ActivityType;

            wp.WellName = wa.WellName;
            wp.SequenceId = wa.UARigSequenceId;
            wp.PhaseNo = Phase.PhaseNo;
            wp.ActivityType = Phase.ActivityType;
            wp.Status = "Draft";
            wp.Type = "Efficient";
            wp.Save();

            //var dataSave = wp.ToBsonDocument();
            //DataHelper.Save(wp.TableName, dataSave);

            //Get the id of new saved PIP
            var q = Query.Null;
            List<IMongoQuery> qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("SequenceId", SequenceId));
            qs.Add(Query.EQ("ActivityType", ActivityType));
            qs.Add(Query.EQ("PhaseNo", PhaseNo));
            q = Query.And(qs);

            var getPIP = WellPIP.Get<WellPIP>(q);

            return Json(new { Success = true, PIPId = getPIP._id, WellName = WellName, ActivityType = ActivityType }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CreatePIPDocForCR(string WellName)
        {
            WellPIP wp = new WellPIP();
            wp.WellName = WellName;
            wp.SequenceId = "";
            wp.PhaseNo = 0;
            wp.ActivityType = "";
            wp.Status = "Draft";
            wp.Type = "Reduction";
            wp.Save();

            //var dataSave = wp.ToBsonDocument();
            //DataHelper.Save(wp.TableName, dataSave);

            //Get the id of new saved PIP
            var q = Query.Null;
            List<IMongoQuery> qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            //qs.Add(Query.EQ("SequenceId", SequenceId));
            //qs.Add(Query.EQ("ActivityType", ActivityType));
            //qs.Add(Query.EQ("PhaseNo", PhaseNo));
            q = Query.And(qs);

            var getPIP = WellPIP.Get<WellPIP>(q);

            return Json(new { Success = true, PIPId = getPIP._id, WellName = WellName, ActivityType = "" }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult DeletePIPDoc(string _id)
        {
            try
            {
                var pip = WellPIP.Get<WellPIP>(_id);

                var q = Query.Null;
                List<IMongoQuery> qs = new List<IMongoQuery>();
                qs.Add(Query.EQ("WellName", pip.WellName));
                qs.Add(Query.EQ("SequenceId", pip.SequenceId));
                qs.Add(Query.EQ("Phase.PhaseNo", pip.PhaseNo));
                qs.Add(Query.EQ("Phase.ActivityType", pip.ActivityType));
                q = Query.And(qs);
                var was1 = WellActivityUpdate.Get<WellActivityUpdate>(q);
                if (was1 != null)
                {
                    throw new Exception("Cannot delete data because there is existing weekly report!");
                }
                else
                {
                    DataHelper.Delete(pip.TableName, Query.EQ("_id", _id));
                }
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }
        public JsonResult Get(object id)
        {
            var wp = WellPIP.Get<WellPIP>(id);
            string WellName = wp.WellName;
            string SequenceId = wp.SequenceId;
            string ActivityType = wp.ActivityType;
            int PhaseNo = wp.PhaseNo;

            var q = Query.Null;
            List<IMongoQuery> qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("SequenceId", SequenceId));
            qs.Add(Query.EQ("Phase.ActivityType", ActivityType));
            q = Query.And(qs);

            var checkWAU = WellActivityUpdate.Get<WellActivityUpdate>(q);
            bool wau = true;
            if (checkWAU != null)
            {
                wau = false;
            }

            //(from a in wp.Elements
            // select a).ToList().ForEach((a) =>
            //             {
            //                 a.Completion = a.Completion * 100;
            //             });

            #region getPhaseinWA

            //var q = Query.Null;
            //List<IMongoQuery> qs = new List<IMongoQuery>();
            //qs.Add(Query.EQ("WellName", WellName));
            //qs.Add(Query.EQ("UARigSequenceId", SequenceId));
            //q = Query.And(qs);
            //var wa = WellActivity.Get<WellActivity>(q);
            //if (wa != null)
            //{
            //    var LatestVersion = wau.OrderByDescending(x => x.UpdateVersion).FirstOrDefault();
            //    var MatchedPhase = wa.Phases.Where(x => x.PhaseNo == PhaseNo).FirstOrDefault();
            //    var LEDays = MatchedPhase.LE.Days;
            //    var LECost = MatchedPhase.LE.Cost;
            //    foreach (var x in wp.Elements)
            //    {
            //        if (wp != null)
            //        {
            //            x.LECost = LECost;
            //            x.LEDays = LEDays;
            //        }
            //    }


            //    DateTime UpdateVersion = wau.UpdateVersion;

            //    foreach (var x in wp.Elements)
            //    {
            //        if (wp != null)
            //        {
            //            var a = new DateRange { Start = UpdateVersion, Finish = x.Period.Start };
            //            var b = new DateRange { Start = x.Period.Finish, Finish = x.Period.Start };
            //            x.CompletionPerc = System.Math.Round(Tools.Div(a.Days, b.Days, 0, 2), 1);
            //            if (x.CompletionPerc < 0) x.CompletionPerc = 0;
            //            if (x.CompletionPerc > 1) x.CompletionPerc = 1;
            //        }
            //    }
            //}
            #endregion

            return Json(new { Success = true, Data = wp, WAU = wau }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LoadFromOps()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var was = WellActivity.Populate<WellActivity>();
                WellPIP.UpdateFromWellOps(was);
                return "OK";
            });
        }

        public JsonResult Save(WellPIP w)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                w.Save();
                return w;
            });
        }

        public JsonResult SaveNewPIP(string PIPId, string Title, DateTime ActivityStart, DateTime ActivityEnd, double PlanDaysOpp, double PlanDaysRisk, double PlanCostOpp, double PlanCostRisk, string Classification, string PerformanceUnit, string ActionParty, List<WEISPersonInfo> ActionParties, string Theme)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                //WellPIP wp_update = new WellPIP();

                DateRange Periode = new DateRange(ActivityStart, ActivityEnd);

                var Elements = wp.Elements;

                var oldElements = new List<PIPElement>();
                foreach (var x in Elements)
                {
                    var newElement = x;
                    newElement.isNewElement = false;
                    oldElements.Add(newElement);
                }

                oldElements.Add(new PIPElement
                {
                    Title = Title,
                    DaysPlanImprovement = PlanDaysOpp,
                    CostPlanImprovement = PlanCostOpp,
                    DaysPlanRisk = PlanDaysRisk,
                    CostPlanRisk = PlanCostRisk,
                    Period = Periode,
                    Classification = Classification,
                    ActionParty = ActionParty,
                    ActionParties = ActionParties,
                    PerformanceUnit = PerformanceUnit,
                    Theme = Theme,
                    isNewElement = true
                });

                //var dataSave = wp.ToBsonDocument();

                //DataHelper.Save(wp.TableName, dataSave);
                wp.Elements = oldElements;
                wp.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult SaveNewPerformanceMetrics(string PIPId, string Title, double Schedule, double Cost)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                var PerfMetrics = wp.PerformanceMetrics;


                PerfMetrics.Add(new PIPPerformanceMetrics
                {
                    Title = Title,
                    Schedule = Schedule,
                    Cost = Cost
                });

                //var dataSave = wp.ToBsonDocument();

                //DataHelper.Save(wp.TableName, dataSave);

                wp.Save();

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

        public JsonResult SaveNewProjectMilestone(string PIPId, string Title, DateTime Period)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                var ProjectMilestone = wp.ProjectMilestones;


                ProjectMilestone.Add(new PIPProjectMilestones
                {
                    Title = Title,
                    Period = Period
                });

                //var dataSave = wp.ToBsonDocument();

                //DataHelper.Save(wp.TableName, dataSave);

                wp.Save();

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

        public JsonResult SaveAllocation(string PIPId, int ElementId, List<PIPAllocation> Allocation)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                //WellPIP wp_update = new WellPIP();


                var Elements = wp.Elements.Where(x => x.ElementId == ElementId).FirstOrDefault();
                Elements.Allocations = new List<PIPAllocation>();

                int y = 1;
                if (Allocation != null)
                {
                    foreach (var t in Allocation)
                    {
                        Elements.Allocations.Add(new PIPAllocation
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
                }

                wp.Save();

                //var dataSave = wp.ToBsonDocument();

                //DataHelper.Save(wp.TableName, dataSave);

                #region noneed
                ////save allocation to activity update
                //string WellName = wp.WellName;
                //string ActivityType = wp.ActivityType;
                //string SequenceId = wp.SequenceId;


                //WellActivityUpdate NewWau = new WellActivityUpdate();
                //PIPElement NewElement = new PIPElement();
                //List<PIPElement> NewListElement = new List<PIPElement>();

                //List<IMongoQuery> qs = new List<IMongoQuery>();
                //IMongoQuery q = Query.Null;
                //qs.Add(Query.EQ("WellName", WellName));
                //qs.Add(Query.EQ("SequenceId", SequenceId));
                //qs.Add(Query.EQ("Phase.ActivityType", ActivityType));
                //q = Query.And(qs);

                //var wau = WellActivityUpdate.Populate<WellActivityUpdate>(q);
                //List<PIPAllocation> NewAllocation = new List<PIPAllocation>();
                //foreach (var x in wau)
                //{
                //    foreach (var a in x.Elements)
                //    {
                //        //find match element id
                //        if (a.ElementId == ElementId)
                //        {
                //            int b = 1;
                //            List<PIPAllocation> na = new List<PIPAllocation>();
                //            foreach (var t in Allocation)
                //            {
                //                na.Add(new PIPAllocation
                //                {
                //                    AllocationID = b,
                //                    CostPlanImprovement = Math.Round(t.CostPlanImprovement, 1),
                //                    CostPlanRisk = Math.Round(t.CostPlanRisk, 1),
                //                    DaysPlanImprovement = Math.Round(t.DaysPlanImprovement, 1),
                //                    Period = t.Period,
                //                    DaysPlanRisk = Math.Round(t.DaysPlanRisk, 1),
                //                    LEDays = Math.Round(t.LEDays, 1),
                //                    LECost = Math.Round(t.LECost, 1)
                //                });
                //                b++;
                //            }
                //            NewAllocation = na;
                //        }
                //        else
                //        {
                //            NewAllocation = a.Allocations;
                //        }
                //        NewElement = a;
                //        NewElement.Allocations = NewAllocation;
                //        NewListElement.Add(NewElement);
                //    }
                //    NewWau = x;
                //    NewWau.Elements = NewListElement;
                //    DataHelper.Save(NewWau.TableName, NewWau.ToBsonDocument());
                //}
                #endregion

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
                return Json(new { Success = false, Message = e.Message + e.StackTrace }, JsonRequestBehavior.AllowGet);
            };
        }
        public static bool Contains(Array a, object val)
        {
            return Array.IndexOf(a, val) != -1;
        }

        public JsonResult UpdatePIP(string PIPId, List<PIPElement> updated)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                string WellName = wp.WellName;
                string ActivityType = wp.ActivityType;
                string SequenceId = wp.SequenceId;


                int[] updatedElementId = new int[200];
                int arrCount = 0;
                foreach (var upd in updated)
                {
                    updatedElementId[arrCount] = upd.ElementId;
                    arrCount++;
                }

                #region update elements on PIP
                var OriginalPIP = wp.Elements;
                List<PIPElement> UpdatedPIP = new List<PIPElement>();

                PIPElement pe = new PIPElement();

                foreach (var i in OriginalPIP)
                {
                    var currentElementId = i.ElementId;
                    if (Contains(updatedElementId, i.ElementId))
                    {
                        // if found match ElementId

                        pe = updated.FirstOrDefault(x => x.ElementId.Equals(i.ElementId));
                        List<PIPAllocation> Allocation = new List<PIPAllocation>();

                        if (i.DaysPlanImprovement != pe.DaysPlanImprovement ||
                            i.CostPlanImprovement != pe.CostPlanImprovement ||
                            i.DaysPlanRisk != pe.DaysPlanRisk ||
                            i.CostPlanRisk != pe.CostPlanRisk ||
                            i.CostCurrentWeekImprovement != pe.CostCurrentWeekImprovement ||
                            i.DaysCurrentWeekImprovement != pe.DaysCurrentWeekImprovement ||
                            i.CostCurrentWeekRisk != pe.CostCurrentWeekRisk ||
                            i.DaysCurrentWeekRisk != pe.DaysCurrentWeekRisk)
                        {
                            Allocation = null;
                        }
                        else
                        {
                            Allocation = i.Allocations;
                        }

                        UpdatedPIP.Add(new PIPElement
                        {
                            ElementId = pe.ElementId,
                            Title = pe.Title,
                            DaysPlanImprovement = pe.DaysPlanImprovement,
                            CostPlanImprovement = pe.CostPlanImprovement,
                            DaysPlanRisk = pe.DaysPlanRisk,
                            CostPlanRisk = pe.CostPlanRisk,
                            Period = pe.Period,
                            Classification = pe.Classification,
                            ActionParty = pe.ActionParty,
                            ActionParties = i.ActionParties,
                            Allocations = Allocation,
                            PerformanceUnit = pe.PerformanceUnit == null ? "" : pe.PerformanceUnit,
                            Completion = pe.Completion,
                            DaysCurrentWeekImprovement = pe.DaysCurrentWeekImprovement,
                            DaysCurrentWeekRisk = pe.DaysCurrentWeekRisk,
                            CostCurrentWeekImprovement = pe.CostCurrentWeekImprovement,
                            CostCurrentWeekRisk = pe.CostCurrentWeekRisk,
                            Theme = pe.Theme
                        });

                        //updatedPhase.Phases.Add(updated.FirstOrDefault(x=>x.PhaseNo.Equals(i.PhaseNo)));
                    }
                    else
                    {
                        UpdatedPIP.Add(i);
                    }
                }

                wp.Elements = UpdatedPIP;
                wp.Save(references: new string[] { "isUpdate" });
                #endregion

                #region Update Allocation in activity update
                WellActivityUpdate NewWau = new WellActivityUpdate();
                List<PIPAllocation> NewAllocation = new List<PIPAllocation>();
                PIPElement NewElement = new PIPElement();
                PIPElement WauElement = new PIPElement();
                List<PIPElement> NewListElement = new List<PIPElement>();

                //List<IMongoQuery> qs = new List<IMongoQuery>();
                //IMongoQuery q = Query.Null;
                //qs.Add(Query.EQ("WellName", WellName));
                //qs.Add(Query.EQ("SequenceId", SequenceId));
                //qs.Add(Query.EQ("Phase.ActivityType", ActivityType));
                //q = Query.And(qs);

                //var wau = WellActivityUpdate.Populate<WellActivityUpdate>(q);
                //var wau = WellActivityUpdate.GetById(wp.WellName, wp.SequenceId, wp.PhaseNo);
                //foreach (var a in wau.Elements)
                //{
                //    //find match element id
                //    var currentElementId = a.ElementId;
                //    if (Contains(updatedElementId, a.ElementId))
                //    {
                //        NewElement = updated.FirstOrDefault(c => c.ElementId.Equals(a.ElementId));

                //        List<PIPAllocation> Allocation = new List<PIPAllocation>();
                //        Allocation = UpdatedPIP.Where(d1 => d1.ElementId.Equals(a.ElementId)).Select(d2 => d2.Allocations).FirstOrDefault();
                //        WauElement = a;
                //        WauElement.DaysCurrentWeekImprovement = NewElement.DaysCurrentWeekImprovement;
                //        WauElement.DaysCurrentWeekRisk = NewElement.DaysCurrentWeekRisk;
                //        WauElement.CostCurrentWeekImprovement = NewElement.CostCurrentWeekImprovement;
                //        WauElement.CostCurrentWeekRisk = NewElement.CostCurrentWeekRisk;
                //        WauElement.Period = NewElement.Period;
                //        WauElement.Allocations = Allocation;
                //        NewListElement.Add(WauElement);
                //    }
                //    else
                //    {
                //        NewListElement.Add(a);
                //    }
                //}
                //wau.Elements = NewListElement;
                //wau.Save();
                #endregion



                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = Tools.PushException(e) + e.StackTrace }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult DeletePIP(string PIPId, int ElementId)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                wp.Elements.RemoveAll(x => x.ElementId == ElementId);

                wp.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult DeletePerformanceMetrics(string PIPId, PIPPerformanceMetrics DataToRemove)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                wp.PerformanceMetrics.RemoveAll(x => x.Title == DataToRemove.Title
                                                && x.Schedule == DataToRemove.Schedule
                                                && x.Cost == DataToRemove.Cost);

                wp.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }
        public JsonResult DeleteProjectMilestone(string PIPId, string Title)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                wp.ProjectMilestones.RemoveAll(x => x.Title == Title);
                wp.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }


        public JsonResult UpdatePerfMetrics(string PIPId, List<PIPPerformanceMetrics> updated)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                wp.PerformanceMetrics = updated;

                wp.Save();

                // check performance metric 
                // <add key="TQTitles" value="Agreed Target,Top Quartile,Performance Target"/>
                // update wellActivityPhase TQ

                //wp.ActivityType
                //wp.WellName
                //wp.PhaseNo   

                string TQTitles = System.Configuration.ConfigurationManager.AppSettings["TQTitles"];
                List<string> tqtitles = TQTitles.Split(',').ToList();
                List<string> titlesLower = new List<string>();

                foreach (string title in tqtitles) titlesLower.Add(title.Trim().ToLower());

                foreach (var t in updated.Where(x => titlesLower.Contains(x.Title.Trim().ToLower())))
                {
                    string utitle = t.Title;
                    double ucost = t.Cost;
                    double udays = t.Schedule;

                    List<IMongoQuery> qs = new List<IMongoQuery>();
                    IMongoQuery q = Query.Null;
                    qs.Add(Query.EQ("WellName", wp.WellName));
                    qs.Add(Query.EQ("UARigSequenceId", wp.SequenceId));
                    WellActivity activity = WellActivity.Get<WellActivity>(Query.And(qs));
                    var phases = activity.Phases.Where(x => x.PhaseNo == wp.PhaseNo);
                    if (phases != null && phases.Count() > 0)
                    {
                        phases.FirstOrDefault().TQ.Cost = ucost * 1000000;
                        phases.FirstOrDefault().TQ.Days = udays;
                    }
                    activity.Save();
                }


                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult UpdateProjectMilestone(string PIPId, List<PIPProjectMilestones> updated)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                wp.ProjectMilestones = updated;

                wp.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }
        public JsonResult SaveProjectInfos(string PIPId, string Field, string ProjectType, string Scaled, int CostLevel, string OptmzEngEmail = "", string TeamLeadEmail = "", string LeadEngEmail = "")
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                wp.Field = Field;
                wp.ProjectType = ProjectType;
                wp.Scaled = Scaled;
                wp.CostLevel = CostLevel;

                wp.Save();

                #region save personinfos

                string WellName = wp.WellName;
                string SequenceId = wp.SequenceId;
                int PhaseNo = wp.PhaseNo;
                string ActivityType = wp.ActivityType;

                List<IMongoQuery> q = new List<IMongoQuery>();
                q.Add(Query.EQ("WellName", WellName));
                q.Add(Query.EQ("SequenceId", SequenceId));
                q.Add(Query.EQ("PhaseNo", PhaseNo));
                //q.Add(Query.EQ("ActivityType", ActivityType));
                IMongoQuery qPerson = Query.And(q);

                WEISPerson OldPerson = WEISPerson.Get<WEISPerson>(qPerson);
                List<WEISPersonInfo> OldPersonInfos = new List<WEISPersonInfo>();
                if (OldPerson != null)
                    OldPersonInfos = OldPerson.PersonInfos;

                List<WEISPersonInfo> NewPersonInfos = new List<WEISPersonInfo>();

                if (TeamLeadEmail == "")
                {
                    //take from old value of persons
                    var getPerson = OldPersonInfos.Where(x => x.RoleId.Equals("TEAM-LEAD")).FirstOrDefault();
                    if (getPerson != null)
                    {
                        WEISPersonInfo person = new WEISPersonInfo();
                        person.FullName = getPerson.FullName;
                        person.Email = getPerson.Email;
                        person.RoleId = "TEAM-LEAD";
                        NewPersonInfos.Add(person);
                    }
                }
                else
                {
                    //take from users
                    WEISPersonInfo person = new WEISPersonInfo();
                    var getPerson = DataHelper.Populate("Users", Query.EQ("Email", TeamLeadEmail));
                    person.FullName = getPerson.Select(d => d.GetString("FullName")).FirstOrDefault();
                    person.Email = getPerson.Select(d => d.GetString("Email")).FirstOrDefault();
                    person.RoleId = "TEAM-LEAD";
                    NewPersonInfos.Add(person);
                }

                if (LeadEngEmail == "")
                {
                    //take from old value of persons
                    var getPerson = OldPersonInfos.Where(x => x.RoleId.Equals("LEAD-ENG")).FirstOrDefault();
                    WEISPersonInfo person = new WEISPersonInfo();
                    if (getPerson != null)
                    {
                        person.FullName = getPerson.FullName;
                        person.Email = getPerson.Email;
                        person.RoleId = "LEAD-ENG";
                        NewPersonInfos.Add(person);
                    }
                }
                else
                {
                    //take from users
                    WEISPersonInfo person = new WEISPersonInfo();
                    var getPerson = DataHelper.Populate("Users", Query.EQ("Email", LeadEngEmail));
                    person.FullName = getPerson.Select(d => d.GetString("FullName")).FirstOrDefault();
                    person.Email = getPerson.Select(d => d.GetString("Email")).FirstOrDefault();
                    person.RoleId = "LEAD-ENG";
                    NewPersonInfos.Add(person);
                }

                if (OptmzEngEmail == "")
                {
                    //take from old value of persons
                    var getPerson = OldPersonInfos.Where(x => x.RoleId.Equals("OPTMZ-ENG")).FirstOrDefault();
                    WEISPersonInfo person = new WEISPersonInfo();
                    if (getPerson != null)
                    {
                        person.FullName = getPerson.FullName;
                        person.Email = getPerson.Email;
                        person.RoleId = "OPTMZ-ENG";
                        NewPersonInfos.Add(person);

                    }
                }
                else
                {
                    //take from users
                    WEISPersonInfo person = new WEISPersonInfo();
                    var getPerson = DataHelper.Populate("Users", Query.EQ("Email", OptmzEngEmail));
                    person.FullName = getPerson.Select(d => d.GetString("FullName")).FirstOrDefault();
                    person.Email = getPerson.Select(d => d.GetString("Email")).FirstOrDefault();
                    person.RoleId = "OPTMZ-ENG";
                    NewPersonInfos.Add(person);
                }

                // append another exist role in list
                foreach (var x in OldPersonInfos.Where(x => x.RoleId != "TEAM-LEAD" && x.RoleId != "LEAD-ENG" && x.RoleId != "OPTMZ-ENG"))
                {
                    WEISPersonInfo person = new WEISPersonInfo();
                    person.FullName = x.FullName;
                    person.Email = x.Email;
                    person.RoleId = x.RoleId;
                    NewPersonInfos.Add(person);
                }

                //OldPerson.WellName = WellName;
                //OldPerson.ActivityType = ActivityType;
                //OldPerson.PhaseNo = PhaseNo;
                //OldPerson.SequenceId = SequenceId;
                //OldPerson.PersonInfos = NewPersonInfos;
                //OldPerson.Save();

                WEISPerson NewPerson = new WEISPerson();
                NewPerson.WellName = WellName;
                NewPerson.ActivityType = ActivityType;
                NewPerson.PhaseNo = PhaseNo;
                NewPerson.SequenceId = SequenceId;
                NewPerson.PersonInfos = NewPersonInfos;
                NewPerson.Save();

                #endregion

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult GetDataAllocation(string PIPId, int ElementId)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                var Elements = wp.Elements.Where(x => x.ElementId == ElementId).FirstOrDefault();

                //double TotalDays = (Elements.Period.Finish - Elements.Period.Start).TotalDays;
                //double diff = Tools.Div(TotalDays, 30);
                //var mthNumber = Math.Ceiling(diff);

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

        public JsonResult SelectActivity(string WellName, string ActivityType, string id)
        {

            ResultInfo ri = new ResultInfo();
            try
            {
                IMongoQuery query = null;
                List<IMongoQuery> queries = new List<IMongoQuery>();
                if (id == null || id == "")
                {
                    queries.Add(Query.EQ("WellName", WellName));
                    queries.Add(Query.EQ("Phases.ActivityType", ActivityType));
                }
                else
                {
                    var wp = WellPIP.Get<WellPIP>(id);
                    queries.Add(Query.EQ("UARigSequenceId", wp.SequenceId));
                    queries.Add(Query.EQ("WellName", wp.WellName));
                    /* NEW ADD WELL NAME */

                }
                query = (queries.Count() > 0 ? Query.And(queries.ToArray()) : null);

                DateTime PhaseMin = Tools.DefaultDate, PhaseMax = Tools.DefaultDate;
                WellActivity res = WellActivity.Get<WellActivity>(query);
                if (res != null)
                {
                    res.Phases = res.Phases.Where(d => d.ActivityType.Equals(ActivityType)).ToList();
                    PhaseMin = res.Phases.Count > 0 ? res.Phases.Min(x => x.PhSchedule.Start) : Tools.DefaultDate;
                    PhaseMax = res.Phases.Count > 0 ? res.Phases.Max(x => x.PhSchedule.Finish) : Tools.DefaultDate;
                }

                ri.Data = new
                {
                    Data = res,
                    PhaseMin = PhaseMin,
                    PhaseMax = PhaseMax
                };

            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }
        public JsonResult SelectActivityForCR(string WellName)
        {

            ResultInfo ri = new ResultInfo();
            try
            {
                DateTime PhaseMin = Tools.DefaultDate, PhaseMax = Tools.DefaultDate;
                var res = new Dictionary<string, string>();

                var getPersons1 = DataHelper.Populate<WEISPerson>("WEISPersons", Query.EQ("WellName", WellName));
                if (getPersons1 != null && getPersons1.Count > 0)
                {
                    string TeamLeadEmail = "";
                    string OptimizationEngineerEmail = "";
                    string LeadEngineerEmail = "";
                    string TeamLead = "";
                    string OptimizationEngineer = "";
                    string LeadEngineer = "";

                    if (getPersons1.FirstOrDefault().PersonInfos != null && getPersons1.FirstOrDefault().PersonInfos.Count > 0)
                    {
                        var getPersons = getPersons1.FirstOrDefault().PersonInfos;

                        TeamLeadEmail = getPersons
                            .Where(d => d.RoleId.Equals("TEAM-LEAD"))
                            .FirstOrDefault()
                            .Email;
                        OptimizationEngineerEmail = getPersons
                            .Where(d => d.RoleId.Equals("OPTMZ-ENG"))
                            .FirstOrDefault()
                            .Email;
                        LeadEngineerEmail = getPersons
                            .Where(d => d.RoleId.Equals("LEAD-ENG"))
                            .FirstOrDefault()
                            .Email;

                        TeamLead = getPersons
                            .Where(d => d.RoleId.Equals("TEAM-LEAD"))
                            .FirstOrDefault()
                            .FullName;
                        OptimizationEngineer = getPersons
                            .Where(d => d.RoleId.Equals("OPTMZ-ENG"))
                            .FirstOrDefault()
                            .FullName;
                        LeadEngineer = getPersons
                            .Where(d => d.RoleId.Equals("LEAD-ENG"))
                            .FirstOrDefault()
                            .FullName;
                    }
                    res.Add("TeamLeadEmail", TeamLeadEmail);
                    res.Add("OptimizationEngineerEmail", OptimizationEngineerEmail);
                    res.Add("LeadEngineerEmail", LeadEngineerEmail);
                    res.Add("TeamLead", TeamLead);
                    res.Add("OptimizationEngineer", OptimizationEngineer);
                    res.Add("LeadEngineer", LeadEngineer);
                }
                else
                {
                    res.Add("TeamLeadEmail", "");
                    res.Add("OptimizationEngineerEmail", "");
                    res.Add("LeadEngineerEmail", "");
                    res.Add("TeamLead", "");
                    res.Add("OptimizationEngineer", "");
                    res.Add("LeadEngineer", "");
                }

                ri.Data = new
                {
                    Data = res,
                    PhaseMin = PhaseMin,
                    PhaseMax = PhaseMax
                };

            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
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

        public JsonResult GetWaterfallDataForCR(
            string Layout = "OP2TQ",
            string GroupBy = "Classification", bool IncludeZero = false,
            string DayOrCost = "Day",
            string RigName = "",
            List<PIPElement> PIPs = null
            )
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var division = 1000000.0;

                DateTime today = Tools.ToUTC(DateTime.Today);
                DateTime periodStart = Tools.ToUTC(new DateTime(today.Year, 01, 01));
                DateTime periodFinish = Tools.ToUTC(new DateTime(today.Year, 12, 31));

                DateTime dateStartBegin = periodStart.AddDays(-7);
                DateTime dateStartEnd = periodStart.AddDays(7);
                DateTime dateFinishBegin = periodFinish.AddDays(-7);
                DateTime dateFinishEnd = periodFinish.AddDays(7);

                var queries = new List<IMongoQuery>();
                queries.Add(Query.EQ("RigName", RigName));

                var qPeriods = new List<IMongoQuery>();
                List<IMongoQuery> dateQueries = new List<IMongoQuery>();
                dateQueries.Add(Query.And(
                    Query.GTE("Phases.PhSchedule.Start", dateStartBegin),
                    Query.LTE("Phases.PhSchedule.Start", dateFinishEnd)
                ));
                dateQueries.Add(Query.And(
                    Query.GTE("Phases.PhSchedule.Finish", dateStartBegin),
                    Query.LTE("Phases.PhSchedule.Finish", dateFinishEnd)
                ));
                queries.Add(Query.Or(dateQueries.ToArray()));
                var query = Query.And(queries.ToArray());

                var PopWA1 = WellActivity.Populate<WellActivity>(query)
                            .Where(d =>
                            {
                                var dateRange = new DateRange();
                                dateRange.Start = d.Phases.Min(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Start));
                                dateRange.Finish = d.Phases.Max(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Finish));
                                return new DashboardController().dateBetween(dateRange, periodStart.ToString("yyyy-MM-dd"), periodFinish.ToString("yyyy-MM-dd"), periodStart.ToString("yyyy-MM-dd"), periodFinish.ToString("yyyy-MM-dd"), "OR");
                            });
                var PopWA2 = PopWA1
                             .Select(d =>
                             {
                                 d.Phases = d.Phases.Where(e =>
                                 {
                                     var riskFilter = !e.ActivityType.ToLower().Contains("risk");
                                     return (riskFilter);
                                 }).ToList();
                                 return d;
                             })
                             .OrderBy(d => d.WellName)
                             .ToList();
                var PopWA = PopWA2.SelectMany(d => d.Phases, (d, e) => new ListWaterfall()
                {
                    Activity = d,
                    Phase = e
                })
                            .Where(d => !d.Phase.ActivityType.ToLower().Contains("risk"))
                            .ToList();

                int c = PopWA.Count();
                int i = 0;
                while (i < c)
                {
                    int x = c - i - 1;
                    WellActivity wac = PopWA[x].Activity;
                    WellActivityPhase wap = PopWA[x].Phase;
                    if (!WellActivity.isHasWellPip(wac, wap))
                    {
                        PopWA.Remove(PopWA[x]);
                    }
                    i++;
                }

                double SumOPDays = PopWA.Sum(x => x.Phase.OP.Days);
                double SumOPCost = PopWA.Sum(x => x.Phase.OP.Cost);
                double SumAFEDays = PopWA.Sum(x => x.Phase.AFE.Days);
                double SumAFECost = PopWA.Sum(x => x.Phase.AFE.Cost);
                double SumTQDays = PopWA.Sum(x => x.Phase.TQ.Days);
                double SumTQCost = PopWA.Sum(x => x.Phase.TQ.Cost);

                if (SumAFEDays == 0)
                {
                    SumAFEDays = SumOPDays;
                    SumAFECost = SumOPCost;
                }

                if (SumTQDays == 0)
                {
                    SumTQDays = SumOPDays * 0.75;
                    SumTQCost = SumOPCost * 0.75;
                }

                var TQ = DayOrCost.Equals("Day") ? SumTQDays : SumTQCost;
                var AFE = DayOrCost.Equals("Day") ? SumAFEDays : SumAFECost;
                var OP = DayOrCost.Equals("Day") ? SumOPDays : SumAFECost;

                if (DayOrCost.Equals("Cost"))
                {
                    TQ /= division;
                    AFE /= division;
                    OP /= division;
                }

                var final = new List<WaterfallItem>();
                if (Layout.Contains("OP"))
                {
                    final.Add(new WaterfallItem(0, "OP", OP, ""));
                }

                if (PIPs != null && PIPs.Count > 0)
                {
                    var groupPIPS = PIPs.GroupBy(d => d.ToBsonDocument().GetString(GroupBy))
                        .Select(d =>
                        {
                            var Plan = d.Sum(e => GetDayOrCost(e, DayOrCost, "Plan"));
                            var LE = d.Sum(e => GetDayOrCost(e, DayOrCost, "LE"));

                            if (DayOrCost.Equals("Cost"))
                            {
                                //Plan /= division;
                                //LE /= division;
                            }

                            return new
                            {
                                d.Key,
                                Plan = Plan,
                                LE = LE
                            };
                        })
                        .ToList();

                    if (Layout.Contains("TQ"))
                    {
                        if (Layout.Contains("OP"))
                        {
                            double gap = 0;
                            foreach (var gp in groupPIPS
                                .Where(d => (!IncludeZero && d.Plan != 0) || IncludeZero)
                                .OrderByDescending(d => d.Plan))
                            {
                                final.Add(new WaterfallItem(0.1, gp.Key == "BsonNull" ? "(P)" : gp.Key + "(P)", gp.Plan, ""));
                                gap += gp.Plan;
                            }
                            if (gap + OP != TQ)
                            {
                                final.Add(new WaterfallItem(0.1, "Gaps (P)", TQ - (gap + OP), ""));
                            }
                        }
                        final.Add(new WaterfallItem(1, "TQ/Agreed Target", TQ, Layout.Contains("OP") ? "total" : ""));
                    }
                }

                var highestLine = final.Sum(d => d.Value);
                foreach (var f in final)
                {
                    f.TrendLines.Add(AFE);
                    f.TrendLines.Add(TQ);
                }

                return final;
            });
        }

        public JsonResult GetWaterfallData(
            string Layout = "OP2TQ",
            string GroupBy = "Classification", bool IncludeZero = false,
            string DayOrCost = "Day",
            string WellName = "",
            string SequenceId = "",
            string ActivityType = "",
            List<PIPElement> PIPs = null)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var division = 1000000.0;
                IMongoQuery q = Query.Null;
                var qs = new List<IMongoQuery>();
                qs.Add(Query.EQ("WellName", WellName));
                qs.Add(Query.EQ("UARigSequenceId", SequenceId));
                var wa = WellActivity.Get<WellActivity>(Query.And(qs));
                if (wa == null) throw new Exception("No well plan defined for this activity, Waterfall can't be generated");
                var act = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(ActivityType));
                if (act.AFE.Days == 0) act.AFE = act.OP;
                if (act.TQ.Days == 0) act.TQ = new WellDrillData
                {
                    Days = act.OP.Days * 0.75,
                    Cost = act.OP.Cost * 0.75
                };
                var TQ = DayOrCost.Equals("Day") ? act.TQ.Days : act.TQ.Cost;
                var AFE = DayOrCost.Equals("Day") ? act.AFE.Days : act.AFE.Cost;
                var OP = DayOrCost.Equals("Day") ? act.Plan.Days : act.Plan.Cost;

                if (DayOrCost.Equals("Cost"))
                {
                    TQ /= division;
                    AFE /= division;
                    OP /= division;
                }

                var final = new List<WaterfallItem>();
                if (Layout.Contains("OP"))
                {
                    final.Add(new WaterfallItem(0, "OP", OP, ""));
                }

                if (PIPs != null && PIPs.Count > 0)
                {
                    var groupPIPS = PIPs.GroupBy(d => d.ToBsonDocument().GetString(GroupBy))
                        .Select(d =>
                        {
                            var Plan = d.Sum(e => GetDayOrCost(e, DayOrCost, "Plan"));
                            var LE = d.Sum(e => GetDayOrCost(e, DayOrCost, "LE"));

                            if (DayOrCost.Equals("Cost"))
                            {
                                //Plan /= division;
                                //LE /= division;
                            }

                            return new
                            {
                                d.Key,
                                Plan = Plan,
                                LE = LE
                            };
                        })
                        .ToList();

                    if (Layout.Contains("TQ"))
                    {
                        if (Layout.Contains("OP"))
                        {
                            double gap = 0;
                            foreach (var gp in groupPIPS
                                .Where(d => (!IncludeZero && d.Plan != 0) || IncludeZero)
                                .OrderByDescending(d => d.Plan))
                            {
                                final.Add(new WaterfallItem(0.1, gp.Key == "BsonNull" ? "(P)" : gp.Key + "(P)", gp.Plan, ""));
                                gap += gp.Plan;
                            }
                            if (gap + OP != TQ)
                            {
                                final.Add(new WaterfallItem(0.1, "Gaps (P)", TQ - (gap + OP), ""));
                            }
                        }
                        final.Add(new WaterfallItem(1, "TQ/Agreed Target", TQ, Layout.Contains("OP") ? "total" : ""));
                    }
                }

                var highestLine = final.Sum(d => d.Value);
                foreach (var f in final)
                {
                    f.TrendLines.Add(AFE);
                    f.TrendLines.Add(TQ);
                }

                return final;
            });
        }

        public JsonResult GetPIPClassification()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                List<string> Name = new List<string>();
                var wpc = WellPIPClassifications.Populate<WellPIPClassifications>();
                foreach (var x in wpc)
                {
                    Name.Add(x.Name);
                }
                return Name.ToArray();
            });
        }

        public JsonResult GetActionParty(string PIPId, int ElementId)
        {
            try
            {
                IMongoQuery q = Query.EQ("_id", PIPId);

                WellPIP wp = WellPIP.Get<WellPIP>(q);

                //PIPElement element = new PIPElement();
                var Elements = wp.Elements.Where(x => x.ElementId == ElementId).FirstOrDefault();

                var ActionParties = new List<WEISPersonInfo>();
                if (Elements.ActionParties != null && Elements.ActionParties.Count > 0)
                {
                    foreach (var x in Elements.ActionParties)
                    {
                        var WPI = new WEISPersonInfo();
                        WPI.FullName = x.FullName == null ? "" : x.FullName;
                        WPI.Email = x.Email == null ? "" : x.Email;
                        WPI.RoleId = x.RoleId == null ? "" : x.RoleId;
                        ActionParties.Add(WPI);
                    }
                }
                Elements.ActionParties = ActionParties;

                return Json(new { Success = true, Data = Elements }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult SaveActionParty(string PIPId, int ElementId, List<WEISPersonInfo> ActionParties)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                var OriginalPIP = wp.Elements;
                List<PIPElement> UpdatedPIP = new List<PIPElement>();

                PIPElement pe = new PIPElement();

                foreach (var i in OriginalPIP)
                {
                    if (i.ElementId == ElementId)
                    {
                        // if found match ElementId
                        pe = i;
                        pe.ActionParties = ActionParties;
                        UpdatedPIP.Add(pe);
                    }
                    else
                    {
                        UpdatedPIP.Add(i);
                    }
                }

                wp.Elements = UpdatedPIP;

                wp.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult SaveComment(string PIPId, int ElementId, int ParentId, string Comment)
        {
            try
            {
                string User = WebTools.LoginUser.UserName;
                string Email = WebTools.LoginUser.Email;
                string FullName = WebTools.LoginUser.UserName;
                string ReferenceType = "WellPIP";
                string Reference1 = PIPId;
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

        public JsonResult DeleteComment(int id)
        {
            try
            {
                WEISComment.Get<WEISComment>(Query.EQ("_id", id)).Delete();
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult GetComment(string PIPId, int ElementId)
        {
            var q = Query.Null;
            List<IMongoQuery> qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("ReferenceType", "WellPIP"));
            qs.Add(Query.EQ("Reference1", PIPId));
            qs.Add(Query.EQ("Reference2", ElementId.ToString()));
            q = Query.And(qs);
            List<WEISComment> wc = WEISComment.Populate<WEISComment>(q);

            var pip = WellPIP.Get<WellPIP>(Query.EQ("_id", PIPId));
            if (pip != null)
            {
                var wellName = pip.WellName;
                var qwau = Query.And(Query.EQ("WellName", wellName), Query.EQ("Phase.ActivityType", pip.ActivityType));
                var sort = new SortByBuilder().Descending("LastUpdate");
                var au = WellActivityUpdate.Populate<WellActivityUpdate>(q: qwau, sort: sort);
                if (au == null) au = new List<WellActivityUpdate>();

                if (au.Count > 0)
                {
                    var auids = au.Select(d => d._id.ToString());
                    var qpip = new List<IMongoQuery>();
                    qpip.Add(Query.EQ("ReferenceType", "WeeklyReport"));
                    qpip.Add(Query.In("Reference1", new BsonArray(auids)));
                    qpip.Add(Query.EQ("Reference2", ElementId.ToString()));

                    var pc = WEISComment.Populate<WEISComment>(Query.And(qpip));

                    if (pc != null)
                    {
                        if (pc.Count > 0)
                        {
                            wc = wc.Concat(pc).ToList();
                        }
                    }
                }
            }

            return Json(new { Data = wc.OrderByDescending(x => x.LastUpdate) }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult ResetSingleAllocation(string PIPId, int ElementId)
        {
            return MvcResultInfo.Execute(() =>
            {
                #region reset single
                //string WellName = "PRINCESS P8";
                //string SequenceId = "3062";
                //string ActivityType = "WHOLE COMPLETION EVENT";
                //int ElementId = 3;

                List<IMongoQuery> qs = new List<IMongoQuery>();
                IMongoQuery q = Query.EQ("_id", PIPId);


                //qs.Add(Query.EQ("WellName", WellName));
                //qs.Add(Query.EQ("ActivityType", ActivityType));
                //qs.Add(Query.EQ("SequenceId", SequenceId));
                //q = Query.And(qs);

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

    }
}