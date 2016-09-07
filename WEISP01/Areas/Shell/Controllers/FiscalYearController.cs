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
    public class FiscalYearController : Controller
    {
        //
        // GET: /Shell/FiscalYear/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetBizPlanActivity(string dateStart, string dateFinish, string dateStart2, string dateFinish2, string dateRelation, string[] operatingUnits, string[] projectNames, string[] regions, string[] rigNames, string[] rigTypes, string[] wellNames, string[] exType)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var dashboardC = new DashboardController();
                var division = 1000000.0;
                var query = dashboardC.GenerateQueryFromFilterAndDate(dateStart, dateFinish, dateStart2, dateFinish2, dateRelation, operatingUnits,
                    projectNames, regions, rigNames, rigTypes, wellNames, null, "OpsSchedule.Start", "OpsSchedule.Finish", exType);
                var raw = BizPlanActivity.Populate<BizPlanActivity>(query);
                var final = raw
                   .Select(d =>
                   {
                       d.Phases = (d.Phases == null ? new List<BizPlanActivityPhase>() : d.Phases);

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
                           PlanCost = Tools.Div(d.Phases.Sum(p => p.Plan.Cost), 1000000),
                           //OpsStart = (d.Phases.Count() > 0 ? d.Phases
                           //     .Where(e=>e.ActivityType.ToLower().Contains("risk")==false)
                           //     .DefaultIfEmpty(new WellActivityPhase())
                           //     .Min(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Start) : Tools.DefaultDate),
                           //OpsFinish = (d.Phases.Count() > 0 ? d.Phases.Max(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Finish) : Tools.DefaultDate),
                           //OpsDuration = (d.Phases.Count() > 0 ? d.Phases.Sum(e => ((DateRange)Tools.Coelesce(new object[] { e.PhSchedule, new DateRange() })).Days) : 0),
                           OpsCost = Tools.Div(d.Phases.Count() > 0 ? d.Phases
                                .Where(e => e.ActivityType.ToLower().Contains("risk") == false)
                                .DefaultIfEmpty(new BizPlanActivityPhase())
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
                                .DefaultIfEmpty(new BizPlanActivityPhase())
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

        public JsonResult GetRigRates()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                List<BizMaster> res = BizMaster.Populate<BizMaster>();
                List<RigRatesSimple> rigrates = new List<RigRatesSimple>();
                foreach (var a in res)
                {
                    if (a.RigRate.Value != null && a.RigRate.Value.Count > 0)
                    {
                        foreach (var b in a.RigRate.Value)
                        {
                            var newRR = new RigRatesSimple();
                            newRR.RigName = a.RigName;
                            newRR.Year = b.Year;
                            newRR.Value = b.Value;
                            rigrates.Add(newRR);
                        }
                    }
                }
                ri.Data = rigrates;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
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
                BizPlanActivity upd = new BizPlanActivity();
                var a = Query.EQ("_id", id);
                BizPlanActivity ret = BizPlanActivity.Get<BizPlanActivity>(a);
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
	}
}