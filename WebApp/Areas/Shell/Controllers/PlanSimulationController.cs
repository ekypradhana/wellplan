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
using System.IO;
using Aspose.Cells;
using System.Web.Configuration;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class PlanSimulationController : Controller
    {
        // GET: Shell/DataModel
        public ActionResult Index()
        {
            ViewBag.isReadOnly = "1";
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

        public JsonResult InitiateSimulation(List<Int32> WellActivityIds, string Title)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                if (WellActivityIds != null && WellActivityIds.Count > 0)
                {
                    var prevSimulation = WellPlanSimulation.Populate<WellPlanSimulation>(Query.EQ("Title", Title));
                    if (prevSimulation.Any())
                    {
                        return "EXISTS";
                    }

                    var wpxs = new WellPlanSimulation();
                    wpxs.Title = Title;
                    foreach (var x in WellActivityIds)
                    {
                        var wa = WellActivity.Get<WellActivity>(Query.EQ("_id", x));
                        wpxs.WellPlans.Add(wa);
                    }
                    wpxs.Save();
                    return "OK";
                }
                else
                {
                    return "NullID";
                }
            });
        }
        public JsonResult CommitSimulation(string id, DateTime latestSequenceDate)
        {
            try
            {
                List<BsonDocument> docs = new List<BsonDocument>();
                var sim = DataHelper.Populate("WEISWellPlanSimulation", Query.EQ("_id", id));
                if (sim != null && sim.Count > 0)
                {

                    var ttt = (sim.FirstOrDefault()["WellPlans"] as BsonValue) as BsonArray;
                    List<BsonDocument> dRes = new List<BsonDocument>();
                    foreach (var tes in ttt)
                    {
                        var dxc = tes.AsBsonDocument;
                        dRes.Add(dxc);
                    }

                    var uw = BsonHelper.Unwind(dRes, "Phases", "", new List<string> { "RigType", "RigName", "WellName", "UARigSequenceId", "_id" });
                    foreach (var u in uw)
                    {
                        u.Set("WellActivityId", u.GetInt32("_id"));
                        u.Remove("_id");
                    }

                    var grpRig = uw.GroupBy(x => BsonHelper.GetString(x, "RigName"));

                    foreach (var g in grpRig)
                    {
                        var res = g.OrderBy(x => BsonHelper.GetDateTime(x, "PhSchedule" + ".Start"));

                        foreach (var dx in res)
                        {
                            BsonDocument d = new BsonDocument();
                            d.Set("Rig_Type", dx.GetString("RigType"));
                            d.Set("Rig_Name", dx.GetString("RigName"));
                            d.Set("Well_Name", dx.GetString("WellName"));
                            d.Set("Activity_Type", dx.GetString("ActivityType"));
                            d.Set("Activity_Description", dx.GetString("ActivityDesc"));


                            d.Set("Start_Date", dx.GetDateTime("PhSchedule" + ".Start"));
                            d.Set("End_Date", dx.GetDateTime("PhSchedule" + ".Finish"));

                            DateTime st = dx.GetDateTime("PhSchedule" + ".Start");
                            DateTime fn = dx.GetDateTime("PhSchedule" + ".Finish");
                            DateRange rng = new DateRange(st, fn);
                            d.Set("Year_-_Start", st.Year);
                            d.Set("Year_-_End", fn.Year);
                            //d.Set("Days", rng.Days);
                            docs.Add(d);
                        }
                    }
                }

                #region Save simulation to OPNAME
                string CollectionName = "OP" + latestSequenceDate.ToString("yyyyMMdd") + "V2";
                DataHelper.Delete(CollectionName);
                DataHelper.Save(CollectionName, docs);
                #endregion

                #region Backup Current WellActivity to _WellActivity_" + nowDate2
                DateTime dateNow = DateTime.Now;
                dateNow = DateTime.Now;
                string nowDate2 = dateNow.ToString("yyyyMMddhhmm");

                var currentDB = DataHelper.Populate("WEISWellActivities");
                DataHelper.Save("_WellActivity_" + nowDate2, currentDB);
                #endregion

                #region do logic
                WellActivity.LoadLastSequence(CollectionName);
                #endregion

                return Json(new { Data = "", Result = "OK", Message = "Load WellActivity Done" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message, Trace = ex.StackTrace }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult Populate(string id = "")
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                //var wps = DataHelper.Populate<WellPlanSimulation>("WEISWellPlanSimulation");
                var wps = WellPlanSimulation.Populate<WellPlanSimulation>();

                return wps;
            });
        }

        public JsonResult DetailSimulation(string id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument b) =>
            {
                var q = Query.EQ("_id", id);
                var wps = WellPlanSimulation.Get<WellPlanSimulation>(q);
                var raw = wps.WellPlans;

                var division = 1000000.0;
                var final = raw.Select(d =>
                   {
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
                           PsDuration = d.PsSchedule.Days,
                           PlanDuration = d.Phases.Sum(p => p.Plan.Days),
                           PlanCost = Tools.Div(d.Phases.Sum(p => p.Plan.Cost), 1000000),
                           //OpsStart = (d.Phases.Count() > 0 ? d.Phases
                           //     .Where(e=>e.ActivityType.ToLower().Contains("risk")==false)
                           //     .DefaultIfEmpty(new WellActivityPhase())
                           //     .Min(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Start) : Tools.DefaultDate),
                           //OpsFinish = (d.Phases.Count() > 0 ? d.Phases.Max(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Finish) : Tools.DefaultDate),
                           //OpsDuration = (d.Phases.Count() > 0 ? d.Phases.Sum(e => ((DateRange)Tools.Coelesce(new object[] { e.PhSchedule, new DateRange() })).Days) : 0),
                           OpsCost = Tools.Div(d.Phases.Count() > 0 ? d.Phases
                                .Where(e => e.ActivityType.ToLower().Contains("risk") == false)
                                .DefaultIfEmpty(new WellActivityPhase())
                                .Sum(e => e.OP.Cost) : 0, division),
                           OpsStart = d.OpsSchedule.Start,
                           OpsFinish = d.OpsSchedule.Finish,
                           OpsDuration = d.OpsSchedule.Days,
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
                           LEStart = d.LESchedule.Start,
                           LEFinish = d.LESchedule.Finish,
                           LEDuration = d.LESchedule.Days,
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

        public ActionResult Browser()
        {
            ViewBag.isReadOnly = "1";
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

        private WellDrillData GetSummary(List<WellActivity> was, string element)
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

        public JsonResult Calc(DataModeler model = null)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                List<IMongoQuery> qs = new List<IMongoQuery>();
                qs.Add(WebTools.GetWellActivitiesQuery("WellName", ""));
                IMongoQuery q = Query.Null;
                if (model != null)
                {
                    if (model.Regions.Count > 0) qs.Add(Query.In("Region", new BsonArray(model.Regions)));
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

        public JsonResult Select(int id, string simulationId)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                WellPlanSimulation wps = WellPlanSimulation.Get<WellPlanSimulation>(Query.EQ("_id", simulationId));

                WellActivity ret = wps.WellPlans.Where(x => Convert.ToInt32(x._id).Equals(id)).FirstOrDefault();
                //if (ret == null)
                //{
                //    ret = new WellActivity();
                //}
                //foreach (var ph in ret.Phases)
                //{
                //    var wau = ph.GetLastUpdate(ret.WellName, ret.UARigSequenceId, false);
                //    //if (wau==null || wau.UpdateVersion.Equals(Tools.GetNearestDay(DateTime.Now, DayOfWeek.Monday))==false)
                //    //{
                //    //    ph.LE = new WellDrillData();
                //    //}
                //}
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

        public ActionResult UpdatePhase(List<WellActivityPhase> updatedPhases, string simulationId, int WellPlanId)
        {
            try
            {
                //updatedPhases = (updatedPhases == null ? new List<WellActivityPhase>() : updatedPhases);
                if (updatedPhases == null) updatedPhases = new List<WellActivityPhase>();

                WellPlanSimulation simulation = WellPlanSimulation.Get<WellPlanSimulation>(Query.EQ("_id", simulationId));
                var SelectedWellPlan = simulation.WellPlans.Where(x => x._id.Equals(WellPlanId)).FirstOrDefault();
                var NewPhases = new List<WellActivityPhase>();

                int[] updatedPhaseNo = new int[200];
                int arrCount = 0;
                foreach (var upd in updatedPhases)
                {
                    updatedPhaseNo[arrCount] = upd.PhaseNo;
                    arrCount++;
                }

                foreach (var i in SelectedWellPlan.Phases)
                {
                    if (updatedPhases.Where(d => d.PhaseNo.Equals(i.PhaseNo)).Count() > 0)
                    {
                        WellActivityPhase wap = updatedPhases.FirstOrDefault(x => x.PhaseNo.Equals(i.PhaseNo));
                        NewPhases.Add(new WellActivityPhase
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
                            PhSchedule = new DateRange(Tools.ToUTC(wap.PhSchedule.Start), Tools.ToUTC(wap.PhSchedule.Finish)),
                            PhaseNo = wap.PhaseNo,
                            RiskFlag = wap.RiskFlag,
                            TQ = wap.TQ
                            //VirtualPhase = wap.VirtualPhase
                        });
                    }
                    else
                    {
                        NewPhases.Add(i);
                    }
                }


                var NewWellPlans = new List<WellActivity>();
                SelectedWellPlan.Phases = NewPhases;
                foreach (var x in simulation.WellPlans)
                {
                    if (x._id == SelectedWellPlan._id)
                    {
                        NewWellPlans.Add(SelectedWellPlan);
                    }
                    else
                    {
                        NewWellPlans.Add(x);
                    }
                }
                simulation.WellPlans = NewWellPlans;
                simulation.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = Tools.PushException(e) + e.StackTrace }, JsonRequestBehavior.AllowGet);
            };
        }


        public ActionResult SaveNewPhase(string SimulationId, int ActivityId, DateTime PhStart, DateTime PhFinish, string ActivityType, bool Virtual = false, bool Shift = false)
        {
            try
            {
                WellPlanSimulation wps = WellPlanSimulation.Get<WellPlanSimulation>(SimulationId);
                List<WellActivity> was = new List<WellActivity>();
                foreach (var x in wps.WellPlans)
                {
                    if (Convert.ToInt32(x._id).Equals(ActivityId))
                    {
                        WellActivity wa = x;

                        wa.Phases = (wa.Phases == null ? new List<WellActivityPhase>() : wa.Phases);
                        wa.Phases.Add(new WellActivityPhase()
                        {
                            PhSchedule = new DateRange(Tools.ToUTC(PhStart), Tools.ToUTC(PhFinish)),
                            OP = new WellDrillData()
                            {
                                Cost = 0,
                                Days = (Tools.ToUTC(PhFinish) - Tools.ToUTC(PhStart)).TotalDays
                            },
                            ActivityType = ActivityType,
                            PhaseNo = (wa.Phases.Count == 0 ? 0 : wa.Phases.Max(d => d.PhaseNo)) + 1,
                            VirtualPhase = Virtual,
                            ShiftFutureEventDate = Shift

                        });
                        was.Add(wa);
                    }
                    else
                    {
                        was.Add(x);
                    }
                }
                wps._id = SimulationId;
                wps.WellPlans = was;
                wps.Save();
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult DeletePhase(string SimulationId, int ActivityId, int PhaseNo)
        {
            try
            {
                WellPlanSimulation wps = WellPlanSimulation.Get<WellPlanSimulation>(SimulationId);
                List<WellActivity> was = new List<WellActivity>();
                foreach (var x in wps.WellPlans)
                {
                    if (Convert.ToInt32(x._id).Equals(ActivityId))
                    {
                        var wa = x;
                        List<WellActivityPhase> wap = new List<WellActivityPhase>();
                        foreach (var p in wa.Phases)
                        {
                            if (!p.PhaseNo.Equals(PhaseNo))
                            {
                                wap.Add(p);
                            }
                        }
                        wa.Phases = wap;
                        was.Add(wa);
                    }
                    else
                    {
                        was.Add(x);
                    }
                }
                wps._id = SimulationId;
                wps.WellPlans = was;
                wps.Save();
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult DeleteSimulation(string id)
        {
            try
            {
                DataHelper.Delete("WEISWellPlanSimulation", Query.EQ("_id", id));
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetWellSequenceInfo(string dateStart, string dateFinish, string dateStart2, string dateFinish2, string dateRelation, string[] operatingUnits, string[] projectNames, string[] regions, string[] rigNames, string[] rigTypes, string[] wellNames, string[] exType)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var dashboardC = new DashboardController();
                var division = 1000000.0;
                var query = dashboardC.GenerateQueryFromFilterAndDate(dateStart, dateFinish, dateStart2, dateFinish2, dateRelation, operatingUnits,
                    projectNames, regions, rigNames, rigTypes, wellNames, "OpsSchedule.Start", "OpsSchedule.Finish", exType);
                var raw = WellActivity.Populate<WellActivity>(query);
                var final = raw
                   .Select(d =>
                   {
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
                           PsDuration = d.PsSchedule.Days,
                           PlanDuration = d.Phases.Sum(p => p.Plan.Days),
                           PlanCost = Tools.Div(d.Phases.Sum(p => p.Plan.Cost), 1000000),
                           //OpsStart = (d.Phases.Count() > 0 ? d.Phases
                           //     .Where(e=>e.ActivityType.ToLower().Contains("risk")==false)
                           //     .DefaultIfEmpty(new WellActivityPhase())
                           //     .Min(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Start) : Tools.DefaultDate),
                           //OpsFinish = (d.Phases.Count() > 0 ? d.Phases.Max(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Finish) : Tools.DefaultDate),
                           //OpsDuration = (d.Phases.Count() > 0 ? d.Phases.Sum(e => ((DateRange)Tools.Coelesce(new object[] { e.PhSchedule, new DateRange() })).Days) : 0),
                           OpsCost = Tools.Div(d.Phases.Count() > 0 ? d.Phases
                                .Where(e => e.ActivityType.ToLower().Contains("risk") == false)
                                .DefaultIfEmpty(new WellActivityPhase())
                                .Sum(e => e.OP.Cost) : 0, division),
                           OpsStart = d.OpsSchedule.Start,
                           OpsFinish = d.OpsSchedule.Finish,
                           OpsDuration = d.OpsSchedule.Days,
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
                           LEStart = d.LESchedule.Start,
                           LEFinish = d.LESchedule.Finish,
                           LEDuration = d.LESchedule.Days,
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

        public JsonResult SaveActivity(WellActivity wellActivity, string eventName, bool virtualPhase = false, bool shiftFutureEventDate = false)
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
                        var number = String.Format("UA{0}", ECIS.Core.SequenceNo.Get("UARigSequenceId").ClaimAsInt());
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
                    return new
                    {
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



        #region Yoga

        private List<SequenceData> PrepareData(SequenceParam param)
        {
            var wellByRole = WebTools.GetWellActivitiesQuery("WellName", "");

            var rigTypes = (param.rigTypes == null ? new List<string>() : param.rigTypes).ToArray();
            var rigNames = (param.rigNames == null ? new List<string>() : param.rigNames).ToArray();
            var wellNames = (param.wellNames == null ? new List<string>() : param.wellNames).ToArray();

            List<SequenceData> results = new List<SequenceData>();

            if (rigTypes.Count() == 0 && rigNames.Count() == 0 && wellNames.Count() == 0)
                return results;

            string aggregateCond1 = "{ $unwind: '$Phases' }";
            string aggregateCond2 = "{ $group:{_id:'$RigName', psStartDate:{$min:'$PsSchedule.Start'}, psFinishDate:{$max:'$PsSchedule.Finish'}, phStartDate:{$min:'$Phases.PhSchedule.Start'}, phFinishDate:{$max:'$Phases.PhSchedule.Finish'} } }";
            List<BsonDocument> pipelines = new List<BsonDocument>();
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
            List<BsonDocument> aggregates = DataHelper.Aggregate("WEISWellActivities", pipelines);

            var rigThatHaveWellNames = new List<string>();
            if (wellNames.Count() > 0)
                rigThatHaveWellNames = WellActivity.Populate<WellActivity>(Query.In("WellName", new BsonArray(wellNames)))
                    .GroupBy(d => d.RigName)
                    .Select(d => d.Key)
                    .ToList();

            foreach (var aggregate in aggregates)
            {
                String RigName = aggregate.GetString("_id");

                if (rigThatHaveWellNames.Count() > 0)
                {
                    if (!rigThatHaveWellNames.Contains(RigName))
                        continue;
                }
                else
                {
                    if (rigNames.Count() > 0 && !rigNames.Contains(RigName))
                        continue;
                }

                IMongoQuery query = null;
                List<IMongoQuery> queryAll = new List<IMongoQuery>();
                queryAll.Add(wellByRole);
                List<BsonValue> rigTypesBson = new List<BsonValue>();
                foreach (var rigType in rigTypes) rigTypesBson.Add(rigType);
                if (rigTypesBson.Count() > 0) queryAll.Add(Query.In("RigType", rigTypesBson));
                queryAll.Add(Query.EQ("RigName", RigName));
                if (queryAll.Count() > 0) query = Query.And(queryAll.ToArray());

                var allActivities = WellActivity.Populate<WellActivity>(query);

                if (!allActivities.Select(d => d.RigName).ToArray().Contains(RigName))
                    continue;

                DateTime PSStartDate = aggregate.GetDateTime("psStartDate");
                DateTime PSFinishDate = aggregate.GetDateTime("psFinishDate");
                DateTime PHStartDate = aggregate.GetDateTime("phStartDate");
                DateTime PHFinishDate = aggregate.GetDateTime("phFinishDate");

                DateTime StartDate = (PSStartDate > PHStartDate ? PHStartDate : PSStartDate);
                DateTime FinishDate = (PSFinishDate < PHFinishDate ? PHFinishDate : PSFinishDate);
                //DateTime StartDate = PSStartDate;
                //DateTime FinishDate = PSFinishDate;

                if (StartDate.Date.Equals(FinishDate.Date))
                {
                    FinishDate = FinishDate.AddMonths(12 - FinishDate.Month);
                }

                var Activities = allActivities
                    .Where(d => d.VirtualPhase != true)
                    .Select(d =>
                    {
                        d.Phases = d.Phases.Where(e => e.ActivityType.ToLower().Contains("risk") == false && e.VirtualPhase != true).ToList();
                        return d;
                    })
                    .Where(d => (wellNames.Count() > 0) ? (wellNames.Contains(d.WellName)) : true)
                    .ToList();

                List<BsonValue> SequenceIds = new List<BsonValue>();
                foreach (var a in Activities)
                    SequenceIds.Add(a.UARigSequenceId);

                var ActivitySequences = WellActivityUpdate.Populate<WellActivityUpdate>(Query.In("SequenceId", SequenceIds));

                results.Add(new SequenceData()
                {
                    RigName = RigName,
                    Activities = Activities,
                    ActivitiySequences = ActivitySequences,
                    DateRange = new DateRange()
                    {
                        Start = StartDate,
                        Finish = FinishDate
                    }
                });
            }

            return results;
        }

        private List<SequenceData> PrepareDataSimulation(WellPlanSimulation simulation)
        {

            var rigTypes = simulation.WellPlans.Select(x => x.RigType).Distinct().ToArray();//;(param.rigTypes == null ? new List<string>() : param.rigTypes).ToArray();
            var rigNames = simulation.WellPlans.Select(x => x.RigName).Distinct().ToArray();
            var wellNames = simulation.WellPlans.Select(x => x.WellName).Distinct().ToArray();

            List<SequenceData> results = new List<SequenceData>();

            if (rigTypes.Count() == 0 && rigNames.Count() == 0 && wellNames.Count() == 0)
                return results;



            string aggregateCond1 = "{ $unwind: '$Phases' }";
            string aggregateCond2 = "{ $group:{_id:'$RigName', psStartDate:{$min:'$PsSchedule.Start'}, psFinishDate:{$max:'$PsSchedule.Finish'}, phStartDate:{$min:'$Phases.PhSchedule.Start'}, phFinishDate:{$max:'$Phases.PhSchedule.Finish'} } }";
            List<BsonDocument> pipelines = new List<BsonDocument>();
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));

            List<BsonDocument> aggregates = DataHelper.Aggregate("WEISWellActivities", pipelines);

            var rigThatHaveWellNames = new List<string>();
            if (wellNames.Count() > 0)
                rigThatHaveWellNames = simulation.WellPlans.Where(x => wellNames.Contains(x.WellName)).ToList()// WellActivity.Populate<WellActivity>(Query.In("WellName", new BsonArray(wellNames)))
                    .GroupBy(d => d.RigName)
                    .Select(d => d.Key)
                    .ToList();

            foreach (var aggregate in aggregates)
            {
                String RigName = aggregate.GetString("_id");

                if (rigThatHaveWellNames.Count() > 0)
                {
                    if (!rigThatHaveWellNames.Contains(RigName))
                        continue;
                }
                else
                {
                    if (rigNames.Count() > 0 && !rigNames.Contains(RigName))
                        continue;
                }

                IMongoQuery query = null;
                List<IMongoQuery> queryAll = new List<IMongoQuery>();
                //queryAll.Add(wellByRole);
                List<BsonValue> rigTypesBson = new List<BsonValue>();
                foreach (var rigType in rigTypes) rigTypesBson.Add(rigType);
                if (rigTypesBson.Count() > 0) queryAll.Add(Query.In("RigType", rigTypesBson));
                queryAll.Add(Query.EQ("RigName", RigName));
                if (queryAll.Count() > 0) query = Query.And(queryAll.ToArray());

                var allActivities = simulation.WellPlans.Where(d => d.RigName.Equals(RigName));

                if (!allActivities.Select(d => d.RigName).ToArray().Contains(RigName))
                    continue;

                DateTime PSStartDate = aggregate.GetDateTime("psStartDate");
                DateTime PSFinishDate = aggregate.GetDateTime("psFinishDate");
                DateTime PHStartDate = aggregate.GetDateTime("phStartDate");
                DateTime PHFinishDate = aggregate.GetDateTime("phFinishDate");

                DateTime StartDate = (PSStartDate > PHStartDate ? PHStartDate : PSStartDate);
                DateTime FinishDate = (PSFinishDate < PHFinishDate ? PHFinishDate : PSFinishDate);
                //DateTime StartDate = PSStartDate;
                //DateTime FinishDate = PSFinishDate;

                if (StartDate.Date.Equals(FinishDate.Date))
                {
                    FinishDate = FinishDate.AddMonths(12 - FinishDate.Month);
                }

                var Activities = allActivities
                    .Where(d => d.VirtualPhase != true)
                    .Select(d =>
                    {
                        d.Phases = d.Phases.Where(e => e.ActivityType.ToLower().Contains("risk") == false && e.VirtualPhase != true).ToList();
                        return d;
                    })
                    .Where(d => (wellNames.Count() > 0) ? (wellNames.Contains(d.WellName)) : true)
                    .ToList();

                List<BsonValue> SequenceIds = new List<BsonValue>();
                foreach (var a in Activities)
                    SequenceIds.Add(a.UARigSequenceId);

                var ActivitySequences = WellActivityUpdate.Populate<WellActivityUpdate>(Query.In("SequenceId", SequenceIds));

                results.Add(new SequenceData()
                {
                    RigName = RigName,
                    Activities = Activities,
                    ActivitiySequences = ActivitySequences,
                    DateRange = new DateRange()
                    {
                        Start = StartDate,
                        Finish = FinishDate
                    }
                });
            }

            return results;
        }

        public List<SequenceData> ConvertWellActivityToWellPlans(List<WellActivity> was)
        {
            SequenceParam param = new SequenceParam();

            foreach (var wa in was)
            {
                var rigName = wa.RigName;
                var rigType = wa.RigType;
                var wellName = wa.WellName;
                param.rigNames.Add(rigName);
                param.rigTypes.Add(rigType);
                param.wellNames.Add(wellName);
            }
            param.wellNames = param.wellNames.Distinct().ToList();
            param.rigNames = param.rigNames.Distinct().ToList();
            param.rigTypes = param.wellNames.Distinct().ToList();

            var res = PrepareData(param);
            return res;
        }

        public JsonResult PrepareDataFromSimulation(string wellPlanSimulationId = "U20150819024903")
        {

            var simData = WellPlanSimulation.Get<WellPlanSimulation>(Query.EQ("_id", wellPlanSimulationId));

            if (simData == null)
                return null;
            else
            {
                if (simData.WellPlans == null || simData.WellPlans.Count <= 0)
                    return null;
                else
                {

                    //var rigNames = simData.WellPlans.Select(x => x.RigName).Distinct();
                    //var rigTypes = simData.WellPlans.Select(x => x.RigType).Distinct();
                    //var wellNames = simData.WellPlans.Select(x => x.WellName).Distinct();
                    //SequenceParam param = new SequenceParam();
                    //param.rigNames = rigNames == null ? null : rigNames.ToList();
                    //param.rigTypes = rigTypes == null ? null : rigTypes.ToList();
                    //param.wellNames = wellNames == null ? null : wellNames.ToList();

                    var res = PrepareDataSimulation(simData);
                    return MvcResultInfo.Execute(null, (BsonDocument doc) => new { Items = res });
                }

            }
        }

        public JsonResult SaveDataForSimulation(string wellPlanSimulationId,
            List<SequenceSaveParam> changes = null,
            List<SequenceSaveParam> deleted = null,
            List<SequenceSaveParam> added = null)
        {
            changes = changes ?? new List<SequenceSaveParam>();
            deleted = deleted ?? new List<SequenceSaveParam>();
            added = added ?? new List<SequenceSaveParam>();

            ResultInfo ri = new ResultInfo();

            try
            {
                WellPlanSimulation plan = WellPlanSimulation.Get<WellPlanSimulation>(Query.EQ("_id", wellPlanSimulationId)) ?? new WellPlanSimulation();
                WellPlanSimulation planBackup = plan;
                foreach (var item in changes)
                {
                    for (var i = 0; i < plan.WellPlans.Count; i++)
                    {
                        var a = plan.WellPlans[i];

                        if (!(a.WellName.Equals(item.Well) && a.RigName.Equals(item.Rig) && a.UARigSequenceId.Equals(item.UARigSequenceId)))
                            continue;

                        for (var j = 0; j < plan.WellPlans[i].Phases.Count; j++)
                        {
                            var p = a.Phases[j];

                            if (!(p.ActivityType.Equals(item.ActivityType) && p.PhaseNo.Equals(item.PhaseNo)))
                                continue;

                            plan.WellPlans[i].Phases[j].PhSchedule = new DateRange()
                            {
                                Start = Tools.ToUTC(item.AfterDateStart.Value, true),
                                Finish = Tools.ToUTC(item.AfterDateFinish.Value, true)
                            };
                        }
                    }
                }

                if (changes.Count > 0)
                {
                    plan.Delete();
                    plan.Save();
                }

                foreach (var item in deleted)
                {
                    for (var i = 0; i < plan.WellPlans.Count; i++)
                    {
                        var a = plan.WellPlans[i];

                        if (!(a.WellName.Equals(item.Well) && a.RigName.Equals(item.Rig) && a.UARigSequenceId.Equals(item.UARigSequenceId)))
                            continue;

                        plan.WellPlans[i].Phases = a.Phases.Where(d =>
                            !(d.ActivityType.Equals(item.ActivityType) && d.PhaseNo.Equals(item.PhaseNo))
                        ).ToList();
                    }
                }

                if (deleted.Count > 0)
                {
                    plan.Delete();
                    plan.Save();
                }

                foreach (var item in added)
                {
                    for (var i = 0; i < plan.WellPlans.Count; i++)
                    {
                        var a = plan.WellPlans[i];

                        if (!(a.WellName.Equals(item.Well) && a.RigName.Equals(item.Rig) && a.UARigSequenceId.Equals(item.UARigSequenceId)))
                            continue;

                        plan.WellPlans[i].Phases = a.Phases.Where(d =>
                            !(d.ActivityType.Equals(item.ActivityType) && d.PhaseNo.Equals(item.PhaseNo))
                        ).ToList();
                    }

                    var originalWellPlan = planBackup.WellPlans.FirstOrDefault(d => d.WellName.Equals(item.Well) && d.RigName.Equals(item.Rig) && d.UARigSequenceId.Equals(item.UARigSequenceId)) ?? new WellActivity();
                    var originalPhase = originalWellPlan.Phases.FirstOrDefault(d => (d.ActivityType.Equals(item.ActivityType) && d.PhaseNo.Equals(item.PhaseNo))) ?? new WellActivityPhase();

                    var isWellExists = plan.WellPlans.Any(d => d.WellName.Equals(item.Well) && d.RigName.Equals(item.ToRig) && d.UARigSequenceId.Equals(item.UARigSequenceId));
                    if (!isWellExists)
                    {
                        WellActivity clonedWellPlan = originalWellPlan;
                        clonedWellPlan.RigName = item.ToRig;
                        clonedWellPlan.Phases = new List<WellActivityPhase>();
                        plan.WellPlans.Add(clonedWellPlan);
                    }

                    var currentPhase = originalPhase;
                    currentPhase.PhSchedule = new DateRange()
                    {
                        Start = Tools.ToUTC(item.AfterDateStart.Value, true),
                        Finish = Tools.ToUTC(item.AfterDateFinish.Value, true)
                    };
                    var currentWellPlanIndex = plan.WellPlans.FindIndex(d => d.WellName.Equals(item.Well) && d.RigName.Equals(item.ToRig) && d.UARigSequenceId.Equals(item.UARigSequenceId));
                    plan.WellPlans[currentWellPlanIndex].Phases.Add(currentPhase);
                }

                if (added.Count > 0)
                {
                    plan.Delete();
                    plan.Save();
                }

                return PrepareDataFromSimulation(wellPlanSimulationId);
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }


        public FileResult ExportSimulationToExcel(string simulationId = "U20150825013627", string DateRangeTitle = "PhSchedule")
        {
            List<BsonDocument> docs = new List<BsonDocument>();
            var sim = DataHelper.Populate("WEISWellPlanSimulation", Query.EQ("_id", simulationId));
            if (sim != null && sim.Count > 0)
            {

                var ttt = (sim.FirstOrDefault()["WellPlans"] as BsonValue) as BsonArray;
                List<BsonDocument> dRes = new List<BsonDocument>();
                foreach (var tes in ttt)
                {
                    var dxc = tes.AsBsonDocument;
                    dRes.Add(dxc);
                }

                var uw = BsonHelper.Unwind(dRes, "Phases", "", new List<string> { "RigType", "RigName", "WellName", "UARigSequenceId", "_id" });
                foreach (var u in uw)
                {
                    u.Set("WellActivityId", u.GetInt32("_id"));
                    u.Remove("_id");
                }

                var grpRig = uw.GroupBy(x => BsonHelper.GetString(x, "RigName"));

                foreach (var g in grpRig)
                {
                    var res = g.OrderBy(x => BsonHelper.GetDateTime(x, "PhSchedule" + ".Start"));

                    foreach (var dx in res)
                    {
                        BsonDocument d = new BsonDocument();
                        d.Set("Rig Type", dx.GetString("RigType"));
                        d.Set("Rig Name", dx.GetString("RigName"));
                        d.Set("Well Name", dx.GetString("WellName"));
                        d.Set("Activity Type", dx.GetString("ActivityType"));
                        d.Set("Activity Description", dx.GetString("ActivityDesc"));


                        d.Set("Activity Start Date", dx.GetDateTime("PhSchedule" + ".Start"));
                        d.Set("Activity End Date", dx.GetDateTime("PhSchedule" + ".Finish"));

                        DateTime st = dx.GetDateTime("PhSchedule" + ".Start");
                        DateTime fn = dx.GetDateTime("PhSchedule" + ".Finish");
                        DateRange rng = new DateRange(st, fn);
                        d.Set("Year - Start", st.Year);
                        d.Set("Year - End", fn.Year);
                        d.Set("Days", rng.Days);
                        docs.Add(d);
                    }
                }
            }



            #region Update by Yoga
            string filenameDonwload = String.Format("WEIS-LS-" + DateTime.Now.ToString("MMM-dd-yyy") + ".xlsx");
            string fileName = Path.Combine(Server.MapPath("~/App_Data/Temp"), filenameDonwload);

            string template = Path.Combine(Server.MapPath("~/App_Data/Temp"), "Latest_Sequence_WEIS_template.xlsx");
            Workbook workbookTemplate = new Workbook(template);
            var worksheetTemplate = workbookTemplate.Worksheets.FirstOrDefault();
            if (System.IO.File.Exists(template) == false)
                throw new Exception("Template file is not exist: " + template);
            int i = 2;
            foreach (var t in docs)
            {
                worksheetTemplate.Cells["A" + i.ToString()].PutValue(t.GetString("Rig Type"));
                worksheetTemplate.Cells["B" + i.ToString()].PutValue(t.GetString("Rig Name"));
                worksheetTemplate.Cells["C" + i.ToString()].PutValue(t.GetString("Well Name"));
                worksheetTemplate.Cells["D" + i.ToString()].PutValue(t.GetString("Activity Type"));
                worksheetTemplate.Cells["E" + i.ToString()].PutValue(t.GetString("Activity Description"));
                worksheetTemplate.Cells["F" + i.ToString()].PutValue(t.GetDateTime("Activity Start Date"));
                worksheetTemplate.Cells["G" + i.ToString()].PutValue(t.GetDateTime("Activity End Date"));
                worksheetTemplate.Cells["H" + i.ToString()].PutValue(t.GetInt32("Year - Start"));
                worksheetTemplate.Cells["I" + i.ToString()].PutValue(t.GetInt32("Year - End"));
                worksheetTemplate.Cells["J" + i.ToString()].PutValue(t.GetInt32("Days"));
                i++;
            }

            string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["DocumentPath"]);
            var filename = DateTime.Now.ToString("ddMMyyyy") + ("-LSSimulation.xlsx");
            var newFileName = folder + @"\" + filename;
            workbookTemplate.Save(newFileName, Aspose.Cells.SaveFormat.Xlsx);
            return File(newFileName, Tools.GetContentType(".xlsx"), filename);

            #endregion
        }



        #endregion
    }
}