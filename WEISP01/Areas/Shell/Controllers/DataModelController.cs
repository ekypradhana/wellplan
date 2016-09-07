using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class DataModelController : Controller
    {
        // GET: Shell/DataModel
        public ActionResult Index()
        {
           

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

        public ActionResult Browser()
        {
            ViewBag.isReadOnly = "0";
            if (_isReadOnly())
            {
                ViewBag.isRO = "1";
            }
            return View();
        }

        public JsonResult Update()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    foreach (var ph in wa.Phases)
                    {
                        ph.AFESchedule = ph.PhSchedule;
                        ph.LESchedule = ph.PhSchedule;
                        //ph.AFEDuration = ph.Duration;
                        //ph.AFECost = ph.Cost;
                        //ph.LEDuration = ph.Duration;
                        //ph.LECost = new WellNumberBreakdown();
                        //ph.LECost.TroubleFree = ph.AFECost.TroubleFree  * 1.1;
                        //ph.LECost.Trouble = ph.AFECost.Trouble * 1.1;
                        //ph.LECost.Contingency = ph.AFECost.Contingency * 1.1;
                        //ph.LECost.EscalationInflation = ph.AFECost.EscalationInflation * 1.1;
                        //ph.LECost.CSO = ph.AFECost.CSO * 1.1;
                        var now = DateTime.Now;
                        if (ph.LESchedule.Start.CompareTo(now) <= 0 && ph.LESchedule.Finish.CompareTo(now) >= 0)
                        {
                            ph.Status = "Active";
                        }
                        else if (ph.LESchedule.Finish.CompareTo(now) < 0)
                        {
                            ph.Status = "Done";
                        }
                        else
                        {
                            ph.Status = "Draft";
                        }
                    }
                }
                return was;
            });
        }

        private WellDrillData GetSummary(List<WellActivity> was,string element)
        {
            WellDrillData ret = was.Select(d => new WellDrillData
                    {
                        Days = d.Phases.Sum(e => e.ToBsonDocument().GetDoc(element).GetDouble("Days")),
                        Cost = d.Phases.Sum(e => e.ToBsonDocument().GetDoc(element).GetDouble("Cost"))
                    })
                    .GroupBy(d => 1)
                    .Select(d => new WellDrillData
                    {
                        Days = d.Sum(e => e.Days),
                        Cost = d.Sum(e => e.Cost)
                    }).FirstOrDefault();
            if (ret == null) ret = new WellDrillData();
            return ret;
        }

        public JsonResult Calc(DataModeler model = null){
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                List<IMongoQuery> qs = new List<IMongoQuery>();
                qs.Add(WebTools.GetWellActivitiesQuery("WellName", ""));
                IMongoQuery q = Query.Null;
                if (model != null)
                {
                    if (model.Regions.Count > 0) qs.Add(Query.In("Region",new BsonArray(model.Regions)));
                    if (model.OperatingUnits.Count > 0) qs.Add(Query.In("OperatingUnit", new BsonArray(model.OperatingUnits)));
                    if (model.RigTypes.Count > 0) qs.Add(Query.In("RigType", new BsonArray(model.RigTypes)));
                    if (model.RigNames.Count > 0) qs.Add(Query.In("RigName", new BsonArray(model.RigNames)));
                    if (model.ProjectNames.Count > 0) qs.Add(Query.In("ProjectName", new BsonArray(model.ProjectNames)));
                    if (model.WellNames.Count > 0) qs.Add(Query.In("WellName", new BsonArray(model.WellNames)));
                    if (qs.Count > 0) q = Query.And(qs);
                }

                var was = WellActivity.Populate<WellActivity>(q);
                var AFE = new WellDrillData();
                var LE = new WellDrillData();
                var LW = new WellDrillData();
                var OP = new WellDrillData();

                foreach (var wa in was)
                {
                    wa.GetUpdate(DateTime.Now, true);
                }

                OP = GetSummary(was, "OP");
                AFE = GetSummary(was, "AFE");
                LW = GetSummary(was, "LWE");
                LE = GetSummary(was, "LE");

                return new
                {
                    OP = OP,
                    AFE = AFE,
                    LE = LE,
                    LW = LW
                };
            });
        }

        public ActionResult EditWellSequenceInfo(string id)
        {
            ViewBag.id = id;
            return View();
        }

        public JsonResult GetRigNames()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                List<RigNameMaster> res = DataHelper.Populate<RigNameMaster>("WEISRigNames");
                ri.Data = res;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult GetWellNames()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                List<WellNameMaster> res = DataHelper.Populate<WellNameMaster>("WEISWellNames", Query.NE("IsVirtualWell", true));
                ri.Data = res;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult Select(int id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                WellActivity upd = new WellActivity();
                var a = Query.EQ("_id", id);
                WellActivity ret = WellActivity.Get<WellActivity>(a);
                foreach (var ph in ret.Phases)
                {
                    var wau = ph.GetLastUpdate(ret.WellName, ret.UARigSequenceId, false);
                    //if (wau==null || wau.UpdateVersion.Equals(Tools.GetNearestDay(DateTime.Now, DayOfWeek.Monday))==false)
                    //{
                    //    ph.LE = new WellDrillData();
                    //}
                }
                return ret;
            });
        }

        public JsonResult Select_Phase(int id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                WellActivity upd = new WellActivity();
                var a = Query.EQ("_id", id);
                WellActivity ret = WellActivity.Get<WellActivity>(a);
                return ret;
            });
        }

        public static bool Contains(Array a, object val)
        {
            return Array.IndexOf(a, val) != -1;
        }

        public ActionResult UpdatePhase(int id, List<WellActivityPhase> updatedPhases, WellActivity updateActivity)
        {
            try
            {
                //updatedPhases = (updatedPhases == null ? new List<WellActivityPhase>() : updatedPhases);
                if (updatedPhases == null) updatedPhases = new List<WellActivityPhase>();

                WellActivity originalActivity = WellActivity.Get<WellActivity>(Query.EQ("_id", id));
                WellActivity newActivity = new WellActivity();
                newActivity.Phases = new List<WellActivityPhase>();

                int[] updatedPhaseNo = new int[200];
                int arrCount = 0;
                foreach (var upd in updatedPhases)
                {
                    updatedPhaseNo[arrCount] = upd.PhaseNo;
                    arrCount++;
                }

                foreach (var i in originalActivity.Phases)
                {
                    if (updatedPhases.Where(d => d.PhaseNo.Equals(i.PhaseNo)).Count() > 0)
                    {
                        WellActivityPhase wap = updatedPhases.FirstOrDefault(x => x.PhaseNo.Equals(i.PhaseNo));
                        newActivity.Phases.Add(new WellActivityPhase
                        { 
                            AFE = wap.AFE,
                            PlanSchedule = wap.PlanSchedule,
                            AFESchedule = wap.AFESchedule,
                            ActivityDesc = wap.ActivityDesc,
                            ActivityDescEst = wap.ActivityDescEst,
                            ActivityType = wap.ActivityType,
                            Actual = wap.Actual,
                            CSOGroup = wap.CSOGroup,
                            EscalationGroup = wap.EscalationGroup,
                            LevelOfEstimate = wap.LevelOfEstimate,
                            Plan = wap.Plan, 
                            FundingType = wap.FundingType,
                            LE = wap.LE,
                            LESchedule = wap.LESchedule,
                            LWE = wap.LWE,
                            M1 = wap.M1,
                            M2 = wap.M2,
                            M3 = wap.M3,
                            OP = wap.OP,
                            PIPs = wap.PIPs,
                            PhSchedule = new DateRange (Tools.ToUTC(wap.PhSchedule.Start),Tools.ToUTC(wap.PhSchedule.Finish)),
                            PhaseNo = wap.PhaseNo,
                            RiskFlag = wap.RiskFlag,
                            TQ = wap.TQ
                            //VirtualPhase = wap.VirtualPhase
                        });
                    }
                    else
                    {
                        newActivity.Phases.Add(i);
                    }
                }

                newActivity._id = originalActivity._id;
                newActivity.Region = updateActivity.Region;
                newActivity.RigName = updateActivity.RigName;
                newActivity.RigType = updateActivity.RigType;
                newActivity.OperatingUnit = updateActivity.OperatingUnit;
                newActivity.ProjectName = updateActivity.ProjectName;
                newActivity.AssetName = updateActivity.AssetName;
                newActivity.WellName = updateActivity.WellName;
                newActivity.WorkingInterest = updateActivity.WorkingInterest;
                newActivity.FirmOrOption = updateActivity.FirmOrOption;
                newActivity.UARigSequenceId = originalActivity.UARigSequenceId;
                newActivity.UARigDescription = updateActivity.UARigDescription;
                newActivity.OpsSchedule = updateActivity.OpsSchedule;
                newActivity.PsSchedule = updateActivity.PsSchedule;
                newActivity.PerformanceUnit = updateActivity.PerformanceUnit;
                newActivity.EXType = updateActivity.EXType;
                newActivity.NonOP = updateActivity.NonOP;

                newActivity.VirtualPhase = updateActivity.VirtualPhase;
                newActivity.ShiftFutureEventDate = updateActivity.ShiftFutureEventDate;


                newActivity.Save(references:new string[]{"SyncWeeklyReport"});

                List<IMongoQuery> qs = new List<IMongoQuery>();
                IMongoQuery q = Query.Null;
                qs.Add(Query.EQ("WellName", newActivity.WellName));
                qs.Add(Query.EQ("SequenceId", newActivity.UARigSequenceId));
                var pips = WellPIP.Populate<WellPIP>(Query.And(qs));

                string TQTitles = System.Configuration.ConfigurationManager.AppSettings["TQTitles"];
                List<string> tqtitles = TQTitles.Split(',').ToList();
                List<string> titlesLower = new List<string>();
                foreach (string title in tqtitles) titlesLower.Add(title.Trim().ToLower());

                foreach(var pip in pips)
                {
                    var phase = newActivity.Phases.FirstOrDefault(x => x.PhaseNo == pip.PhaseNo);
                    if (phase != null) 
                    {
                        foreach (var pm in pip.PerformanceMetrics.Where(x => titlesLower.Contains(x.Title.Trim().ToLower())))
                        {
                            pm.Schedule = phase.TQ.Days;
                            pm.Cost = phase.TQ.Cost / 1000000;
                        }
                        pip.Save();
                    }
                }
                
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = Tools.PushException(e) + e.StackTrace}, JsonRequestBehavior.AllowGet);
            };
        }

        public ActionResult SaveNewPhase(int ActivityId, DateTime PhStart, DateTime PhFinish, string ActivityType, bool Virtual = false, bool Shift = false)
        {
            try
            {
                WellActivity activity = WellActivity.Get<WellActivity>(Query.EQ("_id", ActivityId));
                activity.Phases = (activity.Phases == null ? new List<WellActivityPhase>() : activity.Phases);
                activity.Phases.Add(new WellActivityPhase()
                {
                    PhSchedule = new DateRange(Tools.ToUTC(PhStart), Tools.ToUTC(PhFinish)),
                    OP = new WellDrillData()
                    {
                        Cost = 0,
                        Days = (Tools.ToUTC(PhFinish) - Tools.ToUTC(PhStart)).TotalDays
                    },
                    ActivityType = ActivityType,
                    PhaseNo = (activity.Phases.Count == 0 ? 0 : activity.Phases.Max(x => x.PhaseNo)) + 1,
                    VirtualPhase = Virtual,
                    ShiftFutureEventDate = Shift

                });
                activity.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult DeletePhase(int id, int PhaseNo)
        {
            try
            {
                WellActivity wa = WellActivity.Get<WellActivity>(Query.EQ("_id", id));

                if (wa.Phases.Where(d => d.PhaseNo.Equals(PhaseNo)).Count() > 0) {
                    WellActivityPhase phase = wa.Phases.FirstOrDefault(d => d.PhaseNo.Equals(PhaseNo));

                    var queries1 = new List<IMongoQuery>();
                    queries1.Add(Query.EQ("SequenceId", wa.UARigSequenceId));
                    queries1.Add(Query.EQ("Phase.PhaseNo", PhaseNo));

                    var queries2 = new List<IMongoQuery>();
                    queries2.Add(Query.EQ("SequenceId", wa.UARigSequenceId));
                    queries2.Add(Query.EQ("PhaseNo", PhaseNo));

                    if (WellActivityUpdate.Populate<WellActivityUpdate>(Query.And(queries1.ToArray())).Count > 0) {
                        return Json(new { Success = false, Message = "Phase that used in Weekly Report cannot be deleted!" }, JsonRequestBehavior.AllowGet);
                    }
                    else if (WellPIP.Populate<WellPIP>(Query.And(queries2.ToArray())).Count > 0)
                    {
                        return Json(new { Success = false, Message = "Phase that used in PIP Configuration cannot be deleted!" }, JsonRequestBehavior.AllowGet);
                    }
                }

                List<WellActivityPhase> wap = new List<WellActivityPhase>();

                foreach (var i in wa.Phases)
                {
                    if (i.PhaseNo != PhaseNo)
                    {
                        wap.Add(i);
                    }
                }

                wa.Phases = wap;

                wa.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetWellSequenceInfo(string dateStart, string dateFinish, string dateStart2, string dateFinish2, string dateRelation, string[] operatingUnits, string[] projectNames, string[] regions, string[] rigNames, string[] rigTypes, string[] wellNames, string [] exType)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var dashboardC = new DashboardController();
                var division = 1000000.0;
                var query = dashboardC.GenerateQueryFromFilterAndDate(dateStart, dateFinish, dateStart2, dateFinish2, dateRelation, operatingUnits, 
                    projectNames, regions, rigNames, rigTypes, wellNames, null, "OpsSchedule.Start", "OpsSchedule.Finish", exType);
                var raw = WellActivity.Populate<WellActivity>(query);
                var final = raw
                   .Select(d => {
                       d.Phases = (d.Phases == null ? new List<WellActivityPhase>() : d.Phases);

                       return new
                       {
                           _id = d._id,
                           Region = d.Region,
                           OperatingUnit = d.OperatingUnit,
                           d.UARigSequenceId,
                           RigType = d.RigType,
                           RigName = d.RigName,
                           ProjectName = d.ProjectName,
                           WellName = d.WellName,
                           AssetName = d.AssetName,
                           NonOP = d.NonOP,
                           WorkingInterest = d.WorkingInterest,
                           FirmOrOption = d.FirmOrOption,
                           PsDuration = d.PsSchedule != null ? d.PsSchedule.Days : 0,
                           PlanDuration = d.Phases.Sum(p => p.Plan.Days),
                           PlanCost = Tools.Div(d.Phases.Sum(p=>p.Plan.Cost),1000000),
                           //OpsStart = (d.Phases.Count() > 0 ? d.Phases
                           //     .Where(e=>e.ActivityType.ToLower().Contains("risk")==false)
                           //     .DefaultIfEmpty(new WellActivityPhase())
                           //     .Min(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Start) : Tools.DefaultDate),
                           //OpsFinish = (d.Phases.Count() > 0 ? d.Phases.Max(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Finish) : Tools.DefaultDate),
                           //OpsDuration = (d.Phases.Count() > 0 ? d.Phases.Sum(e => ((DateRange)Tools.Coelesce(new object[] { e.PhSchedule, new DateRange() })).Days) : 0),
                           OpsCost = Tools.Div(d.Phases.Count() > 0 ? d.Phases
                                .Where(e=>e.ActivityType.ToLower().Contains("risk")==false)
                                .DefaultIfEmpty(new WellActivityPhase())
                                .Sum(e => e.OP.Cost) : 0, division),
                           OpsStart = d.OpsSchedule != null ? d.OpsSchedule.Start : Tools.DefaultDate,
                           OpsFinish = d.OpsSchedule != null ? d.OpsSchedule.Finish : Tools.DefaultDate,
                           OpsDuration = d.OpsSchedule != null ? d.OpsSchedule.Days : 0,
                           //OpsCost = d
                           PhDuration = (d.Phases.Count() > 0 ? d.Phases.Sum(x => x.PhSchedule == null ? 0 : x.PhSchedule.Days) : 0),
                           UARigDescription = String.Format("{0} - {1}", d.UARigSequenceId, d.UARigDescription == null ? "" : d.UARigDescription),
                           PhRiskDuration = (d.Phases.Count() > 0 ? d.Phases.Where(x => x.RiskFlag == null ? false : !x.RiskFlag.Equals("")).Sum(x => x.PhSchedule == null ? 0 : x.PhSchedule.Days) : 0),
                           PhStartForFilter = (d.Phases.Count() > 0 ? d.Phases.Min(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Start) : Tools.DefaultDate),
                           PhFinishForFilter = (d.Phases.Count() > 0 ? d.Phases.Max(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Finish) : Tools.DefaultDate),
                           PsStartForFilter = (d.PsSchedule == null ? Tools.DefaultDate : d.PsSchedule.Start),
                           PsFinishForFilter = (d.PsSchedule == null ? Tools.DefaultDate : d.PsSchedule.Finish),
                           PsStart = (d.PsSchedule == null ? Tools.DefaultDate : d.PsSchedule.Start),
                           PsFinish = (d.PsSchedule == null ? Tools.DefaultDate : d.PsSchedule.Finish),
                           PhCost = (d.Phases.Count() > 0 ? (d.Phases.Sum(e => e.OP.Cost) / division) : 0),
                           AFEStart = (d.Phases.Count() > 0 ? d.Phases.Min(e => e.AFESchedule == null ? Tools.DefaultDate : e.AFESchedule.Start) : Tools.DefaultDate).ToString("dd-MMM-yy"),
                           AFEFinish = (d.Phases.Count() > 0 ? d.Phases.Max(e => e.AFESchedule == null ? Tools.DefaultDate : e.AFESchedule.Finish) : Tools.DefaultDate).ToString("dd-MMM-yy"),
                           AFEDuration = (d.Phases.Count() > 0 ? d.Phases.Sum(e => ((WellDrillData)Tools.Coelesce(new object[] { e.AFE, new WellDrillData() })).Days) : 0),
                           AFECost = (d.Phases.Count() > 0 ? (d.Phases.Sum(e => ((WellDrillData)Tools.Coelesce(new object[] { e.AFE, new WellDrillData() })).Cost) / division) : 0),
                           LEStart = d.LESchedule != null ? d.LESchedule.Start : Tools.DefaultDate,
                           LEFinish = d.LESchedule != null ? d.LESchedule.Finish : Tools.DefaultDate,
                           LEDuration = d.LESchedule != null ? d.LESchedule.Days : 0,
                           LECost = Tools.Div(d.Phases.Count() > 0 ? d.Phases
                                .Where(e => e.ActivityType.ToLower().Contains("risk") == false)
                                .DefaultIfEmpty(new WellActivityPhase())
                                .Sum(e => e.LE.Cost) : 0, division),
                           VirtualPhase = d.VirtualPhase,
                           ShiftFutureEventDate = d.ShiftFutureEventDate
                       };
                   })
                   //.Where(d => dashboardC.dateBetween(new DateRange() { Start = d.PsStartForFilter, Finish = d.PsFinishForFilter }, dateStart, dateFinish, dateStart2, dateFinish2, dateRelation))
                   .OrderBy(d => d.RigName)
                   .ThenBy(d => d.PsStartForFilter)
                   .ToList();
                return final;
            });
        }

        public JsonResult SaveActivity(WellActivity wellActivity, string eventName, bool virtualPhase = false, bool shiftFutureEventDate=false)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                try
                {
                    if ((wellActivity.UARigSequenceId == null ? "" : wellActivity.UARigSequenceId).Trim().Equals(""))
                    {
                        string aggregateCond1 = "{ $group: { _id: '$_id', maxSequenceId: { $max: '$UARigSequenceId' }}}";
                        string aggregateCond2 = "{ $limit: 1 }";
                        List<BsonDocument> pipes = new List<BsonDocument>();
                        pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
                        pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
                        var number = String.Format("UA{0}",ECIS.Core.SequenceNo.Get("UARigSequenceId").ClaimAsInt());
                        wellActivity.UARigSequenceId = number;
                    }

                    wellActivity.Phases = new List<WellActivityPhase>();
                    WellActivityPhase phase = new WellActivityPhase();
                    //phase.VirtualPhase = virtualPhase;
                    //phase.ShiftFutureEventDate = shiftFutureEventDate;
                    phase.ActivityType = eventName;
                    phase.PhaseNo = 1;
                    wellActivity.Phases.Add(phase);
                    wellActivity.VirtualPhase = virtualPhase;
                    wellActivity.ShiftFutureEventDate = shiftFutureEventDate;

                    wellActivity.Save("WEISWellActivities");
                    return wellActivity._id;
                }
                catch (Exception e)
                {
                    return new {
                        Message = e.Message
                    };
                }
            });
        }

        public JsonResult DeleteActivity(int id)
        {
            try
            {
                WellActivity activity = WellActivity.Get<WellActivity>(Query.EQ("_id", id));
                if (WellActivityUpdate.Populate<WellActivityUpdate>(Query.EQ("SequenceId", activity.UARigSequenceId)).Count > 0)
                {
                    return Json(new { Success = false, Message = "Activity that have Phase used in Weekly Report cannot be deleted!" }, JsonRequestBehavior.AllowGet);
                }
                else if (WellPIP.Populate<WellPIP>(Query.EQ("SequenceId", activity.UARigSequenceId)).Count > 0)
                {
                    return Json(new { Success = false, Message = "Activity that have Phase used in PIP Configuration cannot be deleted!" }, JsonRequestBehavior.AllowGet);
                }

                DataHelper.Delete("WEISWellActivities", Query.EQ("_id", id));

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}