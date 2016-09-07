using System;
using System.IO;
using System.Collections.Generic;
using System.Data.Common;
using Aspose.Cells;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Configuration;
using Aspose.Cells.Charts;
using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Globalization;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class DataModel2Controller : Controller
    {
        // GET: Shell/DataModel2
        public ActionResult Index()
        {
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

        public ActionResult Browser()
        {
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

            string previousOP = "";
            string nextOP = "";
            var DefaultOP = getBaseOP(out previousOP, out nextOP);
            ViewBag.DefaultOP = DefaultOP;
            ViewBag.PreviousOP = previousOP;
            ViewBag.NextOP = nextOP;

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

        public JsonResult Select_Data(int id, int param = 1)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                WellActivity wa = new WellActivity();
                var q = Query.EQ("_id", id);
                var res = WellActivity.Populate<WellActivity>(q);
                List<BsonDocument> bslist = new List<BsonDocument>();
                var Data = res.SelectMany(x => x.Phases).ToList();
                int op = 14;
                for (var i = 1; i <= param; i++)
                {
                    foreach (var r in Data)
                    {
                        var LvlEstimate = "";
                        if (r.LevelOfEstimate == null || r.LevelOfEstimate.Count() == 0)
                        {
                            LvlEstimate = "";
                        }
                        else
                        {
                            LvlEstimate = r.LevelOfEstimate;
                        }
                        BsonDocument document = new BsonDocument {
                         { "OP"+op, new BsonDocument {
                                 { "Phase",r.ActivityType },
                                 {"LevelEstimate", LvlEstimate},
                                 {"OPName","OP"+op},
                                 {"OPStart",r.PlanSchedule.Start},
                                 {"OPFinsih",r.PlanSchedule.Finish},
                                 {"OPDays",r.Plan.Days},
                                 {"OPCost",r.Plan.Cost},
                                 {"CalcStart",r.CalcOPSchedule.Start},
                                 {"CalcFinish",r.CalcOPSchedule.Finish},
                                 {"CalcDays",r.CalcOP.Days},
                                 {"CalcCost",r.CalcOP.Cost},
                                 {"LSStart",r.PhSchedule.Start},
                                 {"LSFinish",r.PhSchedule.Finish},
                                 {"LSDays",r.OP.Days},
                                 {"LSCost",r.OP.Cost},
                                 {"LEStart",r.LESchedule.Start},
                                 {"LEFinish",r.LESchedule.Finish},
                                 {"LEDays",r.LE.Days},
                                 {"LECost",r.LE.Cost},
                                 {"AFEDays",r.AFE.Days},
                                 {"AFECost",r.AFE.Cost},
                                 {"TQDays",r.TQ.Days},
                                 {"TQCOst",r.TQ.Cost},
                                 {"ActualDays",r.Actual.Days},
                                 {"ActualCost",r.Actual.Cost},
                                 {"WeeklyReport",r.LastWeek},
                                 {"PhaseNo",r.PhaseNo}
                             }
                         }
                     };
                        //var arr = new BsonArray();
                        //arr.Add(new BsonDocument("Phase",r.ActivityType));
                        //var LvlEstimate = r.LevelOfEstimate;
                        //if (LvlEstimate == null || LvlEstimate.Count() == 0)
                        //{
                        //    r.LevelOfEstimate = "";
                        //}
                        //arr.Add(new BsonDocument("OPName", "OP " + op));
                        //arr.Add(new BsonDocument("LevelEstimate", LvlEstimate));
                        //arr.Add(new BsonDocument("OPStart", r.PlanSchedule.Start));
                        //arr.Add(new BsonDocument("OPFinish", r.PlanSchedule.Finish));
                        //arr.Add(new BsonDocument("OPDays", r.Plan.Days));
                        //arr.Add(new BsonDocument("OPCost", r.Plan.Cost));
                        //arr.Add(new BsonDocument("CalcStart", r.CalcOPSchedule.Start));
                        //arr.Add(new BsonDocument("CalcFinish", r.CalcOPSchedule.Finish));
                        //arr.Add(new BsonDocument("CalcDays", r.CalcOP.Days));
                        //arr.Add(new BsonDocument("CalcCost", r.CalcOP.Cost));
                        //arr.Add(new BsonDocument("LSStart", r.PhSchedule.Start));
                        //arr.Add(new BsonDocument("LSFinish", r.PhSchedule.Finish));
                        //arr.Add(new BsonDocument("LSDays", r.OP.Days));
                        //arr.Add(new BsonDocument("LSCost", r.OP.Cost));

                        //document.Add("OP_"+op, arr);

                        bslist.Add(document);
                    }
                    op++;
                }
                return DataHelper.ToDictionaryArray(bslist);
            });
        }

        public JsonResult ViewDataModel(int id, int param = 2)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                WellActivity wa = new WellActivity();
                var q = Query.EQ("_id", id);
                var res = WellActivity.Populate<WellActivity>(q);
                List<BsonDocument> bslist = new List<BsonDocument>();
                var Data = res.SelectMany(x => x.Phases).ToList();
                int op = 14;
                for (var i = 1; i <= param; i++)
                {
                    foreach (var r in Data)
                    {
                        var LvlEstimate = "";
                        if (r.LevelOfEstimate == null || r.LevelOfEstimate.Count() == 0)
                        {
                            LvlEstimate = "";
                        }
                        else
                        {
                            LvlEstimate = r.LevelOfEstimate;
                        }
                        BsonDocument document = new BsonDocument {
                         { "OP", new BsonDocument {
                                 { "Phase",r.ActivityType },
                                 {"LevelEstimate", LvlEstimate},
                                 {"OPName","OP"+op},
                                 {"OPStart",r.PlanSchedule.Start},
                                 {"OPFinsih",r.PlanSchedule.Finish},
                                 {"OPDays",r.Plan.Days},
                                 {"OPCost",r.Plan.Cost},
                                 {"CalcStart",r.CalcOPSchedule.Start},
                                 {"CalcFinish",r.CalcOPSchedule.Finish},
                                 {"CalcDays",r.CalcOP.Days},
                                 {"CalcCost",r.CalcOP.Cost},
                                 {"LSStart",r.PhSchedule.Start},
                                 {"LSFinish",r.PhSchedule.Finish},
                                 {"LSDays",r.OP.Days},
                                 {"LSCost",r.OP.Cost},
                                 {"LEStart",r.LESchedule.Start},
                                 {"LEFinish",r.LESchedule.Finish},
                                 {"LEDays",r.LE.Days},
                                 {"LECost",r.LE.Cost},
                                 {"AFEDays",r.AFE.Days},
                                 {"AFECost",r.AFE.Cost},
                                 {"TQDays",r.TQ.Days},
                                 {"TQCOst",r.TQ.Cost},
                                 {"ActualDays",r.Actual.Days},
                                 {"ActualCost",r.Actual.Cost},
                                 {"WeeklyReport",r.LastWeek},
                                 {"PhaseNo",r.PhaseNo}
                             }
                         }
                     };

                        bslist.Add(document);
                    }
                    op++;
                }
                return DataHelper.ToDictionaryArray(bslist);
            });
        }
        public static string getOPLabel(List<string> BaseOP)
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

        private double GetRatio(DateRange period, WaterfallBase wb)
        {
            if (wb.PeriodView != null && wb.PeriodView.ToLower().Contains("fiscal"))
            {
                wb.DateStart = (wb.DateStart == null ? "" : wb.DateStart);
                wb.DateFinish = (wb.DateFinish == null ? "" : wb.DateFinish);

                string format = "yyyy-MM-dd";
                CultureInfo culture = CultureInfo.InvariantCulture;

                var filter = new DateRange()
                {
                    Start = Tools.ToUTC(wb.DateStart != "" ? DateTime.ParseExact(wb.DateStart, format, culture) : new DateTime(1900, 1, 1)),
                    Finish = Tools.ToUTC(wb.DateFinish != "" ? DateTime.ParseExact(wb.DateFinish, format, culture) : new DateTime(3000, 1, 1))
                };

                var fiscalYears = new List<Int32>();
                if (wb.FiscalYearStart == 0 || wb.FiscalYearFinish == 0)
                {

                }
                else
                {
                    var res = new List<int>();
                    for (var i = wb.FiscalYearStart; i <= wb.FiscalYearFinish; i++)
                        fiscalYears.Add(i);
                }
                var isInvalidFilter = (filter.Start == Tools.DefaultDate);

                if (isInvalidFilter)
                    filter = period;

                var ratios = DateRangeToMonth.ProportionNumDaysPerYear(period, filter).Where(f =>
                {
                    if (fiscalYears.Count > 0)
                        return fiscalYears.Contains(f.Key);

                    return true;
                });

                return ratios.Select(f => f.Value).DefaultIfEmpty(0).Sum(f => f);
            }

            return 1.0;
        }

        public JsonResult SelectNewOP(WaterfallBase wb, int id, List<string> OPs = null, string opRelation = "And")
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                wb.GetWhatData = "OP";
                string previousOP = "";
                string nextOP = "";
                var DefaultOP = getBaseOP(out previousOP, out nextOP);
                WellActivity upd = new WellActivity();
                var a = Query.EQ("_id", id);
                WellActivity ret = WellActivity.Get<WellActivity>(a);
                var selectedPhase = new List<WellActivityPhase>();
                var OPHistories = ret.OPHistories;
                var GetMasterOPs = MasterOP.Populate<MasterOP>();

                var MasterOPs = GetMasterOPs.OrderBy(x => x.Name).ToList();

                //filter statusnya dulu disini
                var matchedPhases = new List<WellActivityPhase>();
                if (ret.Phases.Any())
                {
                    if(wb.Status != null && wb.Status.Count > 0){
                        matchedPhases = ret.Phases.Where(x => wb.Status.Contains(x.BizPlanStatus)).ToList();
                    }
                    else
                    {
                        matchedPhases = ret.Phases;
                    }
                }

                if (matchedPhases.Any())
                {
                    foreach (var ph in matchedPhases)
                    {

                        var islastesLs = true;

                        var logLast = DataHelper.Populate("LogLatestLS", Query.And(
                        Query.EQ("Well_Name", ret.WellName),
                        Query.EQ("Activity_Type", ph.ActivityType),
                        Query.EQ("Rig_Name", ret.RigName)
                        ));


                        if (wb.inlastuploadls)
                        {
                            if (!logLast.Any())
                            {
                                islastesLs = false;
                            }
                        }
                            
                           

                        if (islastesLs) {

                            var OPsList = new List<OPListHelperForDataBrowserGrid>();
                            foreach (var op in MasterOPs)
                            {
                                var oplist = new OPListHelperForDataBrowserGrid();
                                oplist.BaseOP = op.Name;
                                OPsList.Add(oplist);
                            }

                            if (wb.PeriodView != null && wb.PeriodView.ToLower().Contains("fiscal"))
                            {

                            }
                            else
                            {
                                ph.Ratio = 1.0;
                                ph.RatioLS = 1.0;
                                ph.RatioLE = 1.0;
                            }

                            ph.Ratio = GetRatio(ph.PlanSchedule, wb);
                            ph.RatioLS = GetRatio(ph.PhSchedule, wb);
                            ph.RatioLE = GetRatio(ph.LESchedule, wb);

                            ph.CalcOP.Cost *= ph.Ratio;
                            ph.CalcOP.Days *= ph.Ratio;
                            ph.Plan.Cost *= ph.Ratio;
                            ph.Plan.Days *= ph.Ratio;
                            ph.OP.Cost *= ph.RatioLS;
                            ph.OP.Days *= ph.RatioLS;
                            ph.LE.Cost *= ph.RatioLE;
                            ph.LE.Days *= ph.RatioLE;


                            var filterPeriod = wb.FilterPeriod(ph.PlanSchedule);
                            if (!filterPeriod) continue;

                            var wau = ph.GetLastUpdate(ret.WellName, ret.UARigSequenceId, false);
                            //if (wau==null || wau.UpdateVersion.Equals(Tools.GetNearestDay(DateTime.Now, DayOfWeek.Monday))==false)
                            //{
                            //    ph.LE = new WellDrillData();
                            //}

                            if (OPHistories.Any())
                            {
                                var prevOPHist = OPHistories.Where(x => x.Type == previousOP && x.ActivityType.Equals(ph.ActivityType));
                                var getSumPrevDays = 0.0;
                                var getSumPrevCost = 0.0;
                                var getPlanStart = Tools.DefaultDate;
                                var getPlanFinish = Tools.DefaultDate;
                                if (prevOPHist.Any())
                                {
                                    getSumPrevDays = prevOPHist.Sum(x => x.Plan.Days);
                                    getSumPrevCost = prevOPHist.Sum(x => x.Plan.Cost >= 5000  ? x.Plan.Cost : x.Plan.Cost * 1000000);
                                    if (prevOPHist.Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Any())
                                    {
                                        //getPlanStart = prevOPHist.Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Min(x => x.PlanSchedule.Start);
                                        getPlanStart = prevOPHist.Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Min(x => x.PlanSchedule.Start);
                                    }
                                    if (prevOPHist.Where(x => x.PlanSchedule.Finish != Tools.DefaultDate).Any())
                                    {
                                        getPlanFinish = prevOPHist.Where(x => x.PlanSchedule.Finish != Tools.DefaultDate).Max(x => x.PlanSchedule.Finish);
                                    }
                                }
                                //var getSumPrevDays = OPHistories.Where(x => x.Type.Equals(previousOP)).Count() == 0 ? 0.0 : OPHistories.Where(x => x.Type.Equals(previousOP)).Sum(x => x.Plan.Days);
                                //var getSumPrevCost = OPHistories.Where(x => x.Type.Equals(previousOP)).Count() == 0 ? 0.0 : OPHistories.Where(x => x.Type.Equals(previousOP)).Sum(x => x.Plan.Cost);
                                //var getPlanStart = OPHistories.Where(x => x.Type.Equals(previousOP)).Count() == 0 ? Tools.DefaultDate : OPHistories.Where(x => x.Type.Equals(previousOP)).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Min(x => x.PlanSchedule.Start);
                                //var getPlanFinish = OPHistories.Where(x => x.Type.Equals(previousOP)).Count() == 0 ? Tools.DefaultDate : OPHistories.Where(x => x.Type.Equals(previousOP)).Where(x => x.PlanSchedule.Finish != Tools.DefaultDate).Max(x => x.PlanSchedule.Finish);


                                ph.PreviousOPSchedule = new DateRange() { Start = getPlanStart, Finish = getPlanFinish };
                                var ratioOPHist = GetRatio(ph.PreviousOPSchedule, wb);
                                ph.PreviousOP = new WellDrillData() { Days = getSumPrevDays * ratioOPHist, Cost = Tools.Div(getSumPrevCost * ratioOPHist, 1000000) };

                            }
                            else
                            {
                                ph.PreviousOP = new WellDrillData();
                                ph.PreviousOPSchedule = new DateRange();
                            }
                            ph.OP = new WellDrillData() { Days = ph.OP.Days, Cost = Tools.Div(ph.OP.Cost, 1000000) };
                            ph.Plan = new WellDrillData() { Days = ph.Plan.Days, Cost = Tools.Div(ph.Plan.Cost, 1000000) };
                            //ph.LE = new WellDrillData() { Days = new DateRange(Tools.ToUTC(ph.LESchedule.Start, true), Tools.ToUTC(ph.LESchedule.Finish, true)).Days, Cost = Tools.Div(ph.LE.Cost, 1000000) };
                            ph.LE = new WellDrillData() { Days = ph.LE.Days, Cost = Tools.Div(ph.LE.Cost, 1000000) };
                            ph.AFE = new WellDrillData() { Days = ph.AFE.Days, Cost = Tools.Div(ph.AFE.Cost, 1000000) };
                            ph.TQ = new WellDrillData() { Days = ph.TQ.Days, Cost = Tools.Div(ph.TQ.Cost, 1000000) };
                            ph.Actual = new WellDrillData() { Days = ph.Actual.Days, Cost = Tools.Div(ph.Actual.Cost, 1000000) };
                            ph.BIC = new WellDrillData() { Days = ph.BIC.Days, Cost = Tools.Div(ph.BIC.Cost, 1000000) };
                            ph.AggredTarget = new WellDrillData() { Days = ph.AggredTarget.Days, Cost = Tools.Div(ph.AggredTarget.Cost, 1000000) };

                            //if (!ph.BaseOP.Contains(DefaultOP))
                            //{
                            //    ph.PlanSchedule = new DateRange();
                            //    ph.Plan = new WellDrillData();
                            //}
                            //oplist
                            foreach (var op in MasterOPs)
                            {
                                // decide is current op or histories
                                var PlanSchedule = new DateRange();
                                var PlanValue = new WellDrillData() { Days = 0.0, Cost = 0.0 };
                                var isOPHistory = false;
                                if (ph.BaseOP.Any())
                                {
                                    if (ph.BaseOP.OrderByDescending(x => x).FirstOrDefault() == op.Name)
                                    {
                                        //current op
                                        PlanSchedule = ph.PlanSchedule;
                                        PlanValue = ph.Plan;
                                    }
                                    else
                                    {
                                        //histories
                                        //var getHistSchStart = OPHistories.Where(x => x.Type.Equals(op.Name)).Count() == 0 ?
                                        //Tools.DefaultDate : OPHistories.Where(x => x.Type.Equals(op.Name)).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Count() == 0 ?
                                        //Tools.DefaultDate : OPHistories.Where(x => x.Type.Equals(op.Name)).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Min(x => x.PlanSchedule.Start);
                                        //var getHistSchFinish = OPHistories.Where(x => x.Type.Equals(op.Name)).Count() == 0 ?
                                        //Tools.DefaultDate : OPHistories.Where(x => x.Type.Equals(op.Name)).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Count() == 0 ?
                                        //Tools.DefaultDate : OPHistories.Where(x => x.Type.Equals(op.Name)).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Max(x => x.PlanSchedule.Finish);

                                        var getHistSchStart = OPHistories.Where(x => x.Type == op.Name && x.ActivityType == ph.ActivityType).Count() == 0 ?
                                        Tools.DefaultDate : OPHistories.Where(x => x.Type == op.Name && x.ActivityType == ph.ActivityType).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Count() == 0 ?
                                        Tools.DefaultDate : OPHistories.Where(x => x.Type == op.Name && x.ActivityType == ph.ActivityType).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Min(x => x.PlanSchedule.Start);
                                        var getHistSchFinish = OPHistories.Where(x => x.Type == op.Name && x.ActivityType == ph.ActivityType).Count() == 0 ?
                                        Tools.DefaultDate : OPHistories.Where(x => x.Type == op.Name && x.ActivityType == ph.ActivityType).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Count() == 0 ?
                                        Tools.DefaultDate : OPHistories.Where(x => x.Type == op.Name && x.ActivityType == ph.ActivityType).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Max(x => x.PlanSchedule.Finish);


                                        PlanSchedule = new DateRange() { Start = getHistSchStart, Finish = getHistSchFinish };

                                        var GetOPHist = OPHistories.Where(x => x.Type == op.Name && x.ActivityType == ph.ActivityType && x.PlanSchedule.Start != Tools.DefaultDate).FirstOrDefault();
                                        if (GetOPHist != null)
                                        {
                                            var ratioOPHist = GetRatio(GetOPHist.PlanSchedule, wb);
                                            var ratioLSHist = GetRatio(GetOPHist.PhSchedule, wb);
                                            var ratioLEHist = GetRatio(GetOPHist.LESchedule, wb);

                                            GetOPHist.CalcOP.Cost *= ratioOPHist;
                                            GetOPHist.CalcOP.Days *= ratioOPHist;
                                            GetOPHist.Plan.Cost *= ratioOPHist;
                                            GetOPHist.Plan.Days *= ratioOPHist;
                                            GetOPHist.OP.Cost *= ratioLSHist;
                                            GetOPHist.OP.Days *= ratioLSHist;
                                            GetOPHist.LE.Cost *= ratioLEHist;
                                            GetOPHist.LE.Days *= ratioLEHist;
                                            PlanValue.Days = GetOPHist.Plan.Days;


                                            PlanValue.Cost = GetOPHist.Plan.Cost >= 5000 ? Tools.Div(GetOPHist.Plan.Cost, 1000000) : GetOPHist.Plan.Cost;
                                        }
                                        isOPHistory = true;
                                    }
                                }
                                else
                                {
                                    //non op
                                }

                                // set schedule
                                if (OPsList.Where(x => x.BaseOP.Equals(op.Name)).FirstOrDefault().OPSchedule.Start == Tools.DefaultDate)
                                {
                                    OPsList.Where(x => x.BaseOP.Equals(op.Name)).FirstOrDefault().OPSchedule = PlanSchedule;
                                }
                                else
                                {
                                    if (OPsList.Where(x => x.BaseOP.Equals(op.Name)).FirstOrDefault().OPSchedule.Start > PlanSchedule.Start)
                                    {
                                        OPsList.Where(x => x.BaseOP.Equals(op.Name)).FirstOrDefault().OPSchedule.Start = PlanSchedule.Start;
                                    }
                                    if (OPsList.Where(x => x.BaseOP.Equals(op.Name)).FirstOrDefault().OPSchedule.Finish < PlanSchedule.Finish)
                                    {
                                        OPsList.Where(x => x.BaseOP.Equals(op.Name)).FirstOrDefault().OPSchedule.Finish = PlanSchedule.Finish;
                                    }
                                }

                                // set op plan value
                                OPsList.Where(x => x.BaseOP.Equals(op.Name)).FirstOrDefault().OP.Days += Math.Round(PlanValue.Days, 2);
                                OPsList.Where(x => x.BaseOP.Equals(op.Name)).FirstOrDefault().OP.Cost += PlanValue.Cost;
                                ph.OPList = OPsList;
                            }


                            if (OPs != null && OPs.Count > 0)
                            {
                                var BaseOP = ph.BaseOP.ToArray();
                                if (opRelation.ToLower() == "and")
                                {
                                    //match
                                    var match = true;
                                    foreach (var op in OPs)
                                    {
                                        match = Array.Exists(BaseOP, element => element == op);
                                        if (!match) break;
                                    }
                                    if (match)
                                    {
                                        var BaseOPLatest = getOPLabel(ph.BaseOP);
                                        if (BaseOPLatest == "") ph.BaseOP = new List<string>();
                                        else ph.BaseOP = new List<string>() { BaseOPLatest };
                                        selectedPhase.Add(ph);
                                    }
                                }
                                else
                                {
                                    //contains
                                    var match = false;
                                    foreach (var op in OPs)
                                    {
                                        match = Array.Exists(BaseOP, element => element == op);
                                        if (match) break;
                                    }
                                    if (match)
                                    {
                                        var BaseOPLatest = getOPLabel(ph.BaseOP);
                                        if (BaseOPLatest == "") ph.BaseOP = new List<string>();
                                        else ph.BaseOP = new List<string>() { BaseOPLatest };
                                        selectedPhase.Add(ph);
                                    }
                                }
                            }
                            else
                            {
                                var BaseOPLatest = getOPLabel(ph.BaseOP);
                                if (BaseOPLatest == "") ph.BaseOP = new List<string>();
                                else ph.BaseOP = new List<string>() { BaseOPLatest };
                                selectedPhase.Add(ph);
                            }
                        
                        }

                        
                    }
                }
                if (ret.OpsSchedule == null) ret.OpsSchedule = new DateRange();
                //if (ret.PsSchedule == null) ret.OpsSchedule = new DateRange();

                //var wx = WellActivity.Get<WellActivity>(Query.And(Query.EQ("WellName", ret.WellName), Query.EQ("UARigSequenceId", ret.UARigSequenceId)));
                //ret.PsSchedule = GetSchedule2(wx, selectedPhase, DefaultOP);
                //ret.LESchedule = GetSchedule2(wx, selectedPhase, nextOP);
                ret.Phases = selectedPhase;

                return ret;
            });
        }


        public DateRange GetSchedule2( WellActivity wa, List<WellActivityPhase> phases, string selectedOP)
        {

            foreach(var t in phases)
            {
                if (wa.Phases.Where(x => x.ActivityType.Equals(t.ActivityType)).Count() > 0)
                {
                    t.BaseOP = wa.Phases.Where(x => x.ActivityType.Equals(t.ActivityType)).FirstOrDefault().BaseOP;
                }
            }




            var schedule = phases.Where(x => x.BaseOP.Contains(selectedOP));
            if(schedule != null && schedule .Count() > 0 )
            {
                var dtstart = schedule.SelectMany(x => x.OPList).Where(x => x.BaseOP.Equals(selectedOP)).Min(x => x.OPSchedule.Start);
                var dtfinish = schedule.SelectMany(x => x.OPList).Where(x => x.BaseOP.Equals(selectedOP)).Max(x => x.OPSchedule.Finish);

                foreach(var c in schedule)
                {
                    var y = c.BaseOP.OrderByDescending(x => x).FirstOrDefault();
                    c.BaseOP = new List<string>();
                    c.BaseOP.Add(y);
                }

                return new DateRange(dtstart, dtfinish);
            }
            else
                return new DateRange();


        }

        public DateRange GetSchedule(List<WellActivityPhase> phases,string DefaultOP)
        {
            var defaultOpDateList = new List<DateRange>();
            var nonDefaultOpDateList = new List<DateRange>();
            var result = new DateRange();
            if (phases.Any())
            {
                foreach (var ph in phases)
                {
                    if (ph.BaseOP != null || ph.BaseOP.Count > 0)
                    {
                        foreach (var baseop in ph.BaseOP)
                        {
                            if (baseop == DefaultOP)
                            {
                                foreach (var oplist in ph.OPList)
                                {
                                    if (oplist.BaseOP == DefaultOP)
                                    {
                                        defaultOpDateList.Add(oplist.OPSchedule);
                                    }
                                }
                            }
                            else
                            {
                                foreach (var oplist in ph.OPList)
                                {
                                    if (oplist.BaseOP == DefaultOP)
                                    {
                                        nonDefaultOpDateList.Add(oplist.OPSchedule);
                                    }
                                }
                            }
                        }
                    }

                }
            }
            if (defaultOpDateList.Any())
            {
                result.Finish = defaultOpDateList.Max(x => x.Finish);
                result.Start = defaultOpDateList.Min(x => x.Start);
            }
            else
            {
                if (nonDefaultOpDateList.Any())
                {
                    result.Finish = nonDefaultOpDateList.Max(x => x.Finish);
                    result.Start = nonDefaultOpDateList.Min(x => x.Start);
                }
            }
            return result;
        }

        public JsonResult SelectOP(int id, int param = 1)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                WellActivity wa = new WellActivity();
                var q = Query.EQ("_id", id);
                var res = WellActivity.Populate<WellActivity>(q);
                List<BsonDocument> bslist = new List<BsonDocument>();
                var Data = res.SelectMany(x => x.Phases, (x, p) => new
                {
                    WellName = x.WellName,
                    RigName = x.RigName,
                    VirtualPhase = x.VirtualPhase,
                    Phases = p
                });
                //var Data = res.SelectMany(x => x.Phases).ToList();
                int op = 14;
                for (var i = 1; i <= param; i++)
                {
                    foreach (var r in Data)
                    {
                        var LvlEstimate = "";
                        if (r.Phases.LevelOfEstimate == null || r.Phases.LevelOfEstimate.Count() == 0)
                        {
                            LvlEstimate = "";
                        }
                        else
                        {
                            LvlEstimate = r.Phases.LevelOfEstimate;
                        }
                        BsonDocument document = new BsonDocument {
                         { "OP", new BsonDocument {
                                 {"OPName","OP" + op},
                                 {"ActivityType",r.Phases.ActivityType},
                                 {"ShiftFutureEventDate",r.Phases.ShiftFutureEventDate},
                                 {"VirtualPhase",r.VirtualPhase},
                                 {"LevelOfEstimate",LvlEstimate},
                                 {"PlanSchedule", new BsonDocument
                                    {
                                        {"Start",r.Phases.PlanSchedule.Start},
                                        {"Finish",r.Phases.PlanSchedule.Finish}
                                    }
                                 },
                                 {"Plan",new BsonDocument
                                    {
                                        {"Days",r.Phases.Plan.Days},
                                        {"Cost",r.Phases.Plan.Cost}
                                    }
                                 },
                                 {"CalcOPSchedule", new BsonDocument 
                                     {
                                         {"Start",r.Phases.CalcOPSchedule.Start},
                                         {"Finish",r.Phases.CalcOPSchedule.Finish},
                                     }
                                 },
                                 {"CalcOP", new BsonDocument
                                    {
                                        {"Days",r.Phases.CalcOP.Days},
                                        {"Cost",r.Phases.CalcOP.Cost}
                                    }
                                 },
                                 {"PhSchedule", new BsonDocument
                                    {
                                        {"Start",r.Phases.PhSchedule.Start},
                                        {"Finish",r.Phases.PhSchedule.Finish}
                                    }
                                 },{"OP", new BsonDocument
                                     {
                                         {"Days",r.Phases.OP.Days},
                                         {"Cost",r.Phases.OP.Cost}
                                     }
                                 },
                                 {"LESchedule", new BsonDocument
                                    {
                                        {"Start",r.Phases.LESchedule.Start},
                                        {"Finish",r.Phases.LESchedule.Finish}
                                    }
                                 },
                                 {"LE", new BsonDocument
                                     {
                                         {"Days",r.Phases.LE.Days},
                                         {"Cost",r.Phases.LE.Cost}
                                     }
                                 },
                                 {"AFE", new BsonDocument
                                    {
                                        {"Days",r.Phases.AFE.Days},
                                        {"Cost",r.Phases.AFE.Cost}
                                    }
                                 },
                                 {"TQ", new BsonDocument
                                    {
                                        {"Days",r.Phases.TQ.Days},
                                        {"Cost",r.Phases.TQ.Cost}
                                    }
                                 },
                                 {"Actual",new BsonDocument
                                    {
                                        {"Days",r.Phases.Actual.Days},
                                        {"Cost",r.Phases.Actual.Cost}
                                    }
                                 },
                                 {"LWE",new BsonDocument
                                    {
                                        {"Days",r.Phases.LWE.Days},
                                        {"Cost",r.Phases.LWE.Cost}
                                    }
                                 },
                                 {"M1",new BsonDocument
                                    {
                                        {"Days",r.Phases.M1.Days},
                                        {"Cost",r.Phases.M1.Cost}
                                    }
                                 },
                                 {"M2",new BsonDocument
                                    {
                                        {"Days",r.Phases.M2.Days},
                                        {"Cost",r.Phases.M2.Cost}
                                    }
                                 },
                                 {"M3",new BsonDocument
                                    {
                                        {"Days",r.Phases.M3.Days},
                                        {"Cost",r.Phases.M3.Cost}
                                    }
                                 },
                                 {"LastWeek",r.Phases.LastWeek},
                                 {"PhaseNo",r.Phases.PhaseNo}
                             }
                         }
                     };

                        bslist.Add(document);
                    }
                    op++;
                }
                return DataHelper.ToDictionaryArray(bslist);
            });
        }

        public JsonResult SelectPhase(string wellname, string rigname, string activitytype = "")
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                List<IMongoQuery> qs = new List<IMongoQuery>();
                IMongoQuery q = Query.Null;
                qs.Add(Query.EQ("WellName", wellname));
                qs.Add(Query.EQ("RigName", rigname));
                var info = WellActivity.Populate<WellActivity>(Query.And(qs));
                var Data = info.SelectMany(x => x.Phases, (x, p) => new
                {
                    WellName = x.WellName,
                    RigName = x.RigName,
                    AssignTOOps = x.AssignTOOps.Any() ? x.AssignTOOps[0] : "",
                    VirtualPhase = x.VirtualPhase,
                    ActivityType = x.ActivityType,
                    PhaseNo = x.PhaseNo,
                    Phases = p
                }).Where(x => x.Phases.ActivityType.Equals(activitytype));
                return Data;
            });
        }
        public JsonResult SelectPhaseInfo(string wellname, string rigname)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                List<IMongoQuery> qs = new List<IMongoQuery>();
                IMongoQuery q = Query.Null;
                qs.Add(Query.EQ("WellName", wellname));
                qs.Add(Query.EQ("RigName", rigname));
                var info = WellActivityPhaseInfo.Populate<WellActivityPhaseInfo>(Query.And(qs));
                return info;
            });
        }

        public static bool Contains(Array a, object val)
        {
            return Array.IndexOf(a, val) != -1;
        }

        public JsonResult UpdatePhase2(int id, string wellname, string rigname, List<WellActivityPhase> updatedPhases, List<WellActivityPhaseInfo> updatePhaseInfo, WellActivity updateActivity)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {

                var originalActivity = WellActivity.Get<WellActivity>(Query.EQ("_id",id ));


                if (updatedPhases == null)
                    updatedPhases = new List<WellActivityPhase>();
                if (updatePhaseInfo == null)
                    updatePhaseInfo = new List<WellActivityPhaseInfo>();
                string Response = "";
                bool ResponseStatus = false;
                string ResponseId = "";

                foreach (var xx in updatedPhases)
                {
                    foreach (var yy in xx.OPList)
                    {
                        yy.OPSchedule.Start = Tools.ToUTC(yy.OPSchedule.Start, true);
                        yy.OPSchedule.Finish = Tools.ToUTC(yy.OPSchedule.Finish, true);
                    }
                }

                if (originalActivity.RigName != rigname)
                {
                     Response = "Cannot change Rig Name to : " + updateActivity.RigName + ", \nData is not saved.";
                     ResponseStatus = false;
                     ResponseId = "changerig";

                    return new
                    {
                        Response,
                        ResponseStatus,
                        ResponseId
                    };
                }

                if (originalActivity.WellName != wellname)
                {
                    Response = "Cannot change Well Name to : " + updateActivity.WellName + ", \nData is not saved.";
                     ResponseStatus = false;
                     ResponseId = "changewell";

                    return new
                    {
                        Response,
                        ResponseStatus,
                        ResponseId

                    };
                }

                var multiply = 1000000.00;
                List<IMongoQuery> qs = new List<IMongoQuery>();
                qs.Add(Query.EQ("WellName", wellname));
                qs.Add(Query.EQ("RigName", rigname));
                qs.Add(Query.EQ("_id", id));
                var wa = WellActivity.Get<WellActivity>(Query.And(qs));

                wa.Region = updateActivity.Region;
                wa.RigName = updateActivity.RigName;
                wa.RigType = updateActivity.RigType;
                wa.OperatingUnit = updateActivity.OperatingUnit;
                wa.ProjectName = updateActivity.ProjectName;
                wa.AssetName = updateActivity.AssetName;
                wa.WellName = updateActivity.WellName;
                wa.WorkingInterest = updateActivity.WorkingInterest;
                wa.FirmOrOption = updateActivity.FirmOrOption;
                wa.UARigDescription = updateActivity.UARigDescription;
                wa.PerformanceUnit = updateActivity.PerformanceUnit;
                wa.EXType = updateActivity.EXType;
                wa.NonOP = updateActivity.NonOP;

                var newPhases = new List<WellActivityPhase>();
                if (wa.Phases.Any())
                {
                    foreach (var ph in wa.Phases)
                    {
                        if (updatedPhases.Any())
                        {
                            var getPhaseToUpdate = updatedPhases.Where(x => x.PhaseNo.Equals(ph.PhaseNo) && x.ActivityType.Equals(ph.ActivityType)).Count();
                            if (getPhaseToUpdate > 0)
                            {
                                var PhaseToUpdate = updatedPhases.Where(x => x.PhaseNo.Equals(ph.PhaseNo) && x.ActivityType.Equals(ph.ActivityType)).FirstOrDefault();
                                var activeOP = PhaseToUpdate.BaseOP.FirstOrDefault();
                                var Plan = new WellDrillData();
                                var PlanSch = new DateRange();
                                var BaseOPValue = PhaseToUpdate.OPList.Where(x => x.BaseOP.Equals(activeOP)).FirstOrDefault();
                                if (BaseOPValue != null)
                                {
                                    PlanSch = BaseOPValue.OPSchedule;
                                    Plan = new WellDrillData() { Days = PlanSch.Days, Cost = BaseOPValue.OP.Cost };
                                }
                                PhaseToUpdate.PlanSchedule = PlanSch;
                                PhaseToUpdate.Plan.Days = Plan.Days;
                                PhaseToUpdate.Plan.Cost = Plan.Cost * multiply;
                                PhaseToUpdate.LE.Cost *= multiply;
                                PhaseToUpdate.AFE.Cost *= multiply;
                                PhaseToUpdate.OP.Cost *= multiply;
                                PhaseToUpdate.TQ.Cost *= multiply;
                                PhaseToUpdate.AggredTarget.Cost *= multiply;
                                PhaseToUpdate.Actual.Cost *= multiply;
                                PhaseToUpdate.BIC.Cost *= multiply;
                                //PhaseToUpdate.BIC.Cost *= multiply;
                                //PhaseToUpdate.BIC.Cost *= multiply;
                                PhaseToUpdate.BaseOP = ph.BaseOP;

                                PhaseToUpdate.Plan.Cost = Plan.Cost * multiply;

                                //set pushtobizplan
                                PhaseToUpdate.PushToBizPlan = true;
                                newPhases.Add(PhaseToUpdate);
                            }
                            else { newPhases.Add(ph); }
                        }
                        else { newPhases.Add(ph); }
                    }
                }
                //Update OP Histories
                if (wa.OPHistories.Any())
                {
                    foreach (var his in wa.OPHistories)
                    {
                        var phis = updatedPhases.Where(x => x.PhaseNo.Equals(his.PhaseNo) && x.ActivityType.Equals(his.ActivityType))
                            .Select(x => x.OPList);
                        int xx = 0;
                        if (phis.Count() > 0)
                        {
                            var o = phis.FirstOrDefault();
                            for (var i = 0; i <= phis.Count(); i++)
                            {
                                if (his.Type == o[i].BaseOP)
                                {
                                    his.Plan.Cost = o[i].OP.Cost * 1000000;
                                    his.Plan.Days = o[i].OP.Days;
                                    his.PlanSchedule = o[i].OPSchedule;
                                }
                            }
                        }
                    }
                }
                
                wa.Phases = newPhases;

                wa.Save(references: new string[] { "SyncWeeklyReport", "updatetobizplan" });




                // push OP15 value ke MLE
                // cek dulu punya WR or not
                // apakah masih future
                // kalau iya, sikat ke OPnya di MLE

                if (wa.Phases != null && wa.Phases.Count > 0)
                {
                    foreach (var ph in wa.Phases)
                    {
                        var checkWRExist = wa.isHaveWeeklyReport2(wa.WellName, wa.UARigSequenceId, ph.ActivityType);
                        if (!checkWRExist)
                        {
                            var PlanOP15 = new WellDrillData();
                            var AFEOP15 = new WellDrillData();
                            var BICOP15 = new WellDrillData();
                            //cek aktif OP nya dulu, kalau != OP15 berarti ambil dari history
                            if (ph.BaseOP != null && ph.BaseOP.Count > 0 && ph.BaseOP.OrderByDescending(x => x).FirstOrDefault() == "OP15")
                            {
                                //dari body
                                PlanOP15 = ph.Plan;
                                AFEOP15 = ph.AFE;
                                BICOP15 = ph.BIC;
                            }
                            else
                            {
                                //history
                                if (wa.OPHistories != null && wa.OPHistories.Count > 0)
                                {
                                    var getHistory = wa.OPHistories.Where(x => x.Type == "OP15" && x.ActivityType == ph.ActivityType).FirstOrDefault();
                                    if (getHistory != null)
                                    {
                                        PlanOP15 = getHistory.Plan;
                                        AFEOP15 = ph.AFE;
                                        BICOP15 = ph.BIC;
                                    }
                                }
                            }

                            var waum = WellActivityUpdateMonthly.GetById(wa.WellName,wa.UARigSequenceId,ph.PhaseNo,null,true);
                            if (waum != null)
                            {
                                waum.Phase.Plan = PlanOP15;
                                waum.Plan = PlanOP15;
                                waum.Phase.AFE = AFEOP15;
                                waum.AFE = AFEOP15;
                                BICOP15.Cost = BICOP15.Cost > 5000 ? Tools.Div(BICOP15.Cost, 1000000) : BICOP15.Cost;
                                waum.BestInClass = BICOP15;
                                waum.Save();
                            }

                        }
                    }
                }


                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName,
                    WebTools.LoginUser.Email, LogType.UpdatePhase, wa.TableName, url, originalActivity.ToBsonDocument(), wa.ToBsonDocument());


                qs = new List<IMongoQuery>();
                IMongoQuery q = Query.Null;
                qs.Add(Query.EQ("WellName", wa.WellName));
                qs.Add(Query.EQ("SequenceId", wa.UARigSequenceId));
                var pips = WellPIP.Populate<WellPIP>(Query.And(qs));

                string TQTitles = System.Configuration.ConfigurationManager.AppSettings["TQTitles"];
                List<string> tqtitles = TQTitles.Split(',').ToList();
                List<string> titlesLower = new List<string>();
                foreach (string title in tqtitles) titlesLower.Add(title.Trim().ToLower());

                foreach (var pip in pips)
                {
                    var phase = wa.Phases.FirstOrDefault(x => x.PhaseNo == pip.PhaseNo);
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

                Response = "Data Saved!";
                ResponseStatus = true;
                ResponseId = "done";
                return new
                {
                    Response,
                    ResponseStatus,
                    ResponseId
                };

            });
        }

        public ActionResult UpdatePhase(int id, string wellname, string rigname, List<WellActivityPhase> updatedPhases, List<WellActivityPhaseInfo> updatePhaseInfo, WellActivity updateActivity)
        {
            try
            {
                var Response = ""; bool ResponseStatus = false;
                string previousOP = "";
                string nextOP = "";
                var DefaultOP = getBaseOP(out previousOP, out nextOP);
                //updatedPhases = (updatedPhases == null ? new List<WellActivityPhase>() : updatedPhases);
                if (updatedPhases == null) updatedPhases = new List<WellActivityPhase>();
                if (updatePhaseInfo == null) updatePhaseInfo = new List<WellActivityPhaseInfo>();
                //var qupd = Query.EQ("_id", id);
                WellActivity originalActivity = WellActivity.Get<WellActivity>(id);
                WellActivity originalActivity2 = WellActivity.Get<WellActivity>(id);
                WellActivity newActivity = new WellActivity();
                newActivity.Phases = new List<WellActivityPhase>();
                List<IMongoQuery> qss = new List<IMongoQuery>();
                IMongoQuery qu = Query.Null;
                qss.Add(Query.EQ("WellName", wellname));
                qss.Add(Query.EQ("RigName", rigname));
                var OriPhaseInfoActivity = WellActivityPhaseInfo.Populate<WellActivityPhaseInfo>(Query.And(qss));
                WellActivityPhaseInfo newPhaseInfoActivity = new WellActivityPhaseInfo();
                var GetMasterOPs = MasterOP.Populate<MasterOP>();
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
                        var PhaseBaseOP = DefaultOP;
                        if (i.BaseOP.Any())
                        {
                            PhaseBaseOP = i.BaseOP.OrderByDescending(d => d).FirstOrDefault();
                        }
                        int ix = 0;
                        var TempPlan = new WellDrillData() { Days = 0, Cost = 0 };
                        var TempPhShedule = new DateRange(Tools.DefaultDate, Tools.DefaultDate);
                        foreach (var r in GetMasterOPs)
                        {
                            if (wap.OPList[ix].BaseOP.Equals(PhaseBaseOP))
                            {
                                TempPlan = new WellDrillData() { Days = wap.OPList[ix].OP.Days, Cost = wap.OPList[ix].OP.Cost * 1000000 };
                                TempPhShedule = new DateRange(Tools.ToUTC(wap.OPList[ix].OPSchedule.Start), Tools.ToUTC(wap.OPList[ix].OPSchedule.Finish));
                            }
                            ix++;
                        }

                        if (i.BaseOP.Count() > 1)
                        {
                            if (i.BaseOP[0].Equals(DefaultOP) || i.BaseOP[1].Equals(DefaultOP))
                            {
                                //wap.Plan.Cost = wap.Plan.Cost * 1000000;
                                newActivity.Phases.Add(new WellActivityPhase
                                {
                                    AFE = new WellDrillData() { Days = wap.AFE.Days, Cost = wap.AFE.Cost * 1000000 },
                                    PlanSchedule = TempPhShedule,
                                    AFESchedule = wap.AFESchedule,
                                    ActivityDesc = wap.ActivityDesc,
                                    ActivityDescEst = wap.ActivityDescEst,
                                    ActivityType = wap.ActivityType,
                                    Actual = new WellDrillData() { Days = wap.Actual.Days, Cost = wap.Actual.Cost * 1000000 },
                                    CSOGroup = wap.CSOGroup,
                                    EscalationGroup = wap.EscalationGroup,
                                    LevelOfEstimate = wap.LevelOfEstimate,
                                    //Plan = new WellDrillData() { Days = wap.Plan.Days, Cost = wap.Plan.Cost * 1000000 },
                                    Plan = TempPlan,
                                    FundingType = wap.FundingType,
                                    LE = new WellDrillData() { Days = wap.LE.Days, Cost = wap.LE.Cost * 1000000 },
                                    LESchedule = wap.LESchedule,
                                    LWE = wap.LWE,
                                    M1 = wap.M1,
                                    M2 = wap.M2,
                                    M3 = wap.M3,
                                    OP = new WellDrillData() { Days = wap.OP.Days, Cost = wap.OP.Cost * 1000000 },
                                    PIPs = wap.PIPs,
                                    PhSchedule = new DateRange(Tools.ToUTC(wap.PhSchedule.Start), Tools.ToUTC(wap.PhSchedule.Finish)),
                                    PhaseNo = wap.PhaseNo,
                                    RiskFlag = wap.RiskFlag,
                                    TQ = new WellDrillData() { Days = wap.TQ.Days, Cost = wap.TQ.Cost * 1000000 },
                                    BIC = new WellDrillData() { Days = wap.BIC.Days, Cost = wap.BIC.Cost * 1000000 },
                                    BaseOP = wap.BaseOP,
                                    //VirtualPhase = wap.VirtualPhase

                                });
                                Response = "Data Saved!";
                                ResponseStatus = true;
                            }
                            else
                            {
                                newActivity.Phases.Add(i);
                                Response = "You can't update, because Current OP is " + DefaultOP;
                            }
                        }
                        else if (i.BaseOP.Count() == 1)
                        {
                            if (!i.BaseOP[0].Equals(DefaultOP))
                            {
                                newActivity.Phases.Add(i);
                                Response = "You can't update, because Current OP is " + DefaultOP;
                            }
                            else
                            {
                                //wap.Plan.Cost = wap.Plan.Cost * 1000000;
                                newActivity.Phases.Add(new WellActivityPhase
                                {
                                    AFE = new WellDrillData() { Days = wap.AFE.Days, Cost = wap.AFE.Cost * 1000000 },
                                    PlanSchedule = TempPhShedule,
                                    AFESchedule = wap.AFESchedule,
                                    ActivityDesc = wap.ActivityDesc,
                                    ActivityDescEst = wap.ActivityDescEst,
                                    ActivityType = wap.ActivityType,
                                    Actual = new WellDrillData() { Days = wap.Actual.Days, Cost = wap.Actual.Cost * 1000000 },
                                    CSOGroup = wap.CSOGroup,
                                    EscalationGroup = wap.EscalationGroup,
                                    LevelOfEstimate = wap.LevelOfEstimate,
                                    //Plan = new WellDrillData() { Days = wap.Plan.Days, Cost = wap.Plan.Cost * 1000000 },
                                    Plan = TempPlan,
                                    FundingType = wap.FundingType,
                                    LE = new WellDrillData() { Days = wap.LE.Days, Cost = wap.LE.Cost * 1000000 },
                                    LESchedule = wap.LESchedule,
                                    LWE = wap.LWE,
                                    M1 = wap.M1,
                                    M2 = wap.M2,
                                    M3 = wap.M3,
                                    OP = new WellDrillData() { Days = wap.OP.Days, Cost = wap.OP.Cost * 1000000 },
                                    PIPs = wap.PIPs,
                                    PhSchedule = new DateRange(Tools.ToUTC(wap.PhSchedule.Start), Tools.ToUTC(wap.PhSchedule.Finish)),
                                    PhaseNo = wap.PhaseNo,
                                    RiskFlag = wap.RiskFlag,
                                    TQ = new WellDrillData() { Days = wap.TQ.Days, Cost = wap.TQ.Cost * 1000000 },
                                    BIC = new WellDrillData() { Days = wap.BIC.Days, Cost = wap.BIC.Cost * 1000000 },
                                    BaseOP = wap.BaseOP,
                                    //VirtualPhase = wap.VirtualPhase

                                });
                                Response = "Data Saved!";
                                ResponseStatus = true;
                            }
                        }
                        else
                        {
                            newActivity.Phases.Add(i);
                            Response = "You can't update, because base OP is empty";
                        }

                    }
                    else
                    {
                        newActivity.Phases.Add(i);
                        Response = "Data Saved!";
                        ResponseStatus = true;
                    }
                }

                //OP History
                foreach (var ophis in originalActivity.OPHistories)
                {
                    newActivity.OPHistories.Add(ophis);
                }

                #region for PhaseInfo Activ
                //foreach (var j in OriPhaseInfoActivity)
                //{
                //    if (updatePhaseInfo.Where(d => d.PhaseNo.Equals(j.PhaseNo)).Count() > 0)
                //    {
                //        WellActivityPhaseInfo wapInfo = updatePhaseInfo.FirstOrDefault(x => x.PhaseNo.Equals(j.PhaseNo));
                //        newPhaseInfoActivity._id = wapInfo._id;
                //        newPhaseInfoActivity.WellActivityId = wapInfo.WellActivityId;
                //        newPhaseInfoActivity.WellName = wapInfo.WellName;
                //        newPhaseInfoActivity.ActivityType = wapInfo.ActivityType;
                //        newPhaseInfoActivity.RigName = wapInfo.RigName;
                //        newPhaseInfoActivity.SequenceId = wapInfo.SequenceId;
                //        newPhaseInfoActivity.PhaseNo = wapInfo.PhaseNo;
                //        newPhaseInfoActivity.LoE = wapInfo.LoE;
                //        newPhaseInfoActivity.TotalDuration = wapInfo.TotalDuration;
                //        newPhaseInfoActivity.TroubleFree = wapInfo.TroubleFree;
                //        newPhaseInfoActivity.Trouble = wapInfo.Trouble;
                //        newPhaseInfoActivity.Contigency = wapInfo.Contigency;
                //        newPhaseInfoActivity.TQ = wapInfo.TQ;
                //        newPhaseInfoActivity.BIC = wapInfo.BIC;
                //        newPhaseInfoActivity.LTA2 = wapInfo.LTA2;
                //        newPhaseInfoActivity.SinceLTA2 = wapInfo.SinceLTA2;
                //        newPhaseInfoActivity.BurnRate = wapInfo.BurnRate;
                //        newPhaseInfoActivity.EscalationInflation = wapInfo.EscalationInflation;
                //        newPhaseInfoActivity.CSO = wapInfo.CSO;
                //        newPhaseInfoActivity.TotalCostIncludePortf = wapInfo.TotalCostIncludePortf;
                //        newPhaseInfoActivity.LLMonth = wapInfo.LLMonth;
                //        newPhaseInfoActivity.LLAmount = wapInfo.LLAmount;
                //        newPhaseInfoActivity.CostEscalatedInflated = wapInfo.CostEscalatedInflated;
                //        newPhaseInfoActivity.TotalCostWithEscInflCSO = wapInfo.TotalCostWithEscInflCSO;
                //        newPhaseInfoActivity.TQMeasures = wapInfo.TQMeasures;
                //        newPhaseInfoActivity.LineOfBusiness = wapInfo.LineOfBusiness;
                //        newPhaseInfoActivity.ActivityCategory = wapInfo.ActivityCategory;
                //        newPhaseInfoActivity.SpreadRate = wapInfo.SpreadRate;
                //        newPhaseInfoActivity.MRI = wapInfo.MRI;
                //        newPhaseInfoActivity.CompletionType = wapInfo.CompletionType;
                //        newPhaseInfoActivity.CompletionZone = wapInfo.CompletionZone;
                //        newPhaseInfoActivity.BrineDensity = wapInfo.BrineDensity;
                //        newPhaseInfoActivity.EstimatingRangeType = wapInfo.EstimatingRangeType;
                //        newPhaseInfoActivity.DeterministicLowRange = wapInfo.DeterministicLowRange;
                //        newPhaseInfoActivity.DeterministicHigh = wapInfo.DeterministicHigh;
                //        newPhaseInfoActivity.ProbabilisticP10 = wapInfo.ProbabilisticP10;
                //        newPhaseInfoActivity.ProbabilisticP90 = wapInfo.ProbabilisticP90;
                //        newPhaseInfoActivity.WaterDepthMD = wapInfo.WaterDepthMD;
                //        newPhaseInfoActivity.TotalWellDepthMD = wapInfo.TotalWellDepthMD;
                //        newPhaseInfoActivity.LearningCurveFactor = wapInfo.LearningCurveFactor;
                //        newPhaseInfoActivity.MaturityLevel = wapInfo.MaturityLevel;
                //        newPhaseInfoActivity.ReferenceFactorModel = wapInfo.ReferenceFactorModel;
                //        newPhaseInfoActivity.OverrideFactor = wapInfo.OverrideFactor;
                //        newPhaseInfoActivity.NPT = wapInfo.NPT;
                //        newPhaseInfoActivity.TECOP = wapInfo.TECOP;
                //        newPhaseInfoActivity.Mean = wapInfo.Mean;
                //        newPhaseInfoActivity.MeanCostEDM = wapInfo.MeanCostEDM;
                //        newPhaseInfoActivity.Currency = wapInfo.Currency;
                //        newPhaseInfoActivity.USDCost = wapInfo.USDCost;
                //        newPhaseInfoActivity.ProjectValueDriver = wapInfo.ProjectValueDriver;
                //        newPhaseInfoActivity.ValueDriverEstimate = wapInfo.ValueDriverEstimate;
                //        newPhaseInfoActivity.TQTreshold = wapInfo.TQTreshold;
                //        newPhaseInfoActivity.TQGap = wapInfo.TQGap;
                //        newPhaseInfoActivity.BICTreshold = wapInfo.BICTreshold;
                //        newPhaseInfoActivity.BICGap = wapInfo.BICGap;
                //        newPhaseInfoActivity.PerformanceScore = wapInfo.PerformanceScore;
                //        newPhaseInfoActivity.Save();
                //    }
                //}
                #endregion

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
                newActivity.AssignTOOps = updateActivity.AssignTOOps;

                newActivity.Save(references: new string[] { "SyncWeeklyReport" });


                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName,
                    WebTools.LoginUser.Email, LogType.UpdatePhase, newActivity.TableName, url, originalActivity2.ToBsonDocument(), newActivity.ToBsonDocument());

                List<IMongoQuery> qs = new List<IMongoQuery>();
                IMongoQuery q = Query.Null;
                qs.Add(Query.EQ("WellName", newActivity.WellName));
                qs.Add(Query.EQ("SequenceId", newActivity.UARigSequenceId));
                var pips = WellPIP.Populate<WellPIP>(Query.And(qs));

                string TQTitles = System.Configuration.ConfigurationManager.AppSettings["TQTitles"];
                List<string> tqtitles = TQTitles.Split(',').ToList();
                List<string> titlesLower = new List<string>();
                foreach (string title in tqtitles) titlesLower.Add(title.Trim().ToLower());

                foreach (var pip in pips)
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

                return Json(new { Success = true, Response = Response, ResponseStatus = ResponseStatus }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = Tools.PushException(e) + e.StackTrace }, JsonRequestBehavior.AllowGet);
            };
        }

        public ActionResult SaveNewPhase(int ActivityId, DateTime PhStart, DateTime PhFinish, string ActivityType, bool Virtual = false, bool Shift = false)
        {
            try
            {
                bool UpdStatus = true; string MyMessage = "";
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

                WellActivity activity = WellActivity.Get<WellActivity>(Query.EQ("_id", ActivityId));
                activity.Phases = (activity.Phases == null ? new List<WellActivityPhase>() : activity.Phases);

                //check
                string strMsg = string.Format("WellName : {0}, Activity Type : {1}, ", activity.WellName, activity.Phases.FirstOrDefault().ActivityType);
                var t = WellActivity.isHaveWeeklyReport(activity.WellName, activity.UARigSequenceId, ActivityType);
                if ((PhStart != Tools.DefaultDate) && t == false)
                {
                    var LeDays = (Tools.ToUTC(PhFinish).AddDays(activity.Phases.FirstOrDefault().LE.Days)) - Tools.ToUTC(PhStart);
                    var phase = new WellActivityPhase()
                    {
                        PhSchedule = new DateRange(Tools.ToUTC(PhStart), Tools.ToUTC(PhFinish)),
                        OP = new WellDrillData()
                        {
                            Cost = 0,
                            Days = (Tools.ToUTC(PhFinish) - Tools.ToUTC(PhStart)).TotalDays
                        },

                        PlanSchedule = new DateRange(Tools.ToUTC(PhStart), Tools.ToUTC(PhFinish)),
                        LESchedule = new DateRange() { Start = Tools.ToUTC(PhStart), Finish = Tools.ToUTC(PhFinish).AddDays(activity.Phases.FirstOrDefault().LE.Days) },

                        LE = new WellDrillData()
                        {
                            Cost = 0,
                            Days = LeDays.TotalDays
                        },
                        Plan = new WellDrillData()
                        {
                            Cost = 0,
                            Days = (Tools.ToUTC(PhFinish) - Tools.ToUTC(PhStart)).TotalDays
                        },

                        ActivityType = ActivityType,
                        PhaseNo = (activity.Phases.Count == 0 ? 0 : activity.Phases.Max(x => x.PhaseNo)) + 1,
                        VirtualPhase = Virtual,
                        ShiftFutureEventDate = Shift,
                        BaseOP = new List<string>() { DefaultOP },
                        EventCreatedFrom = "DataBrowser",
                        IsPostOP = true
                    };
                    activity.Phases.Add(phase);
                    activity.Save();

                    var mLe = activity.CreateNewActivityUpdateMonthly(activity, phase.PhaseNo, "DataBrowser");

                    var qsElement = new List<IMongoQuery>();
                    qsElement.Add(Query.EQ("WellName", activity.WellName));
                    qsElement.Add(Query.EQ("SequenceId", activity.UARigSequenceId));
                    qsElement.Add(Query.EQ("ActivityType", ActivityType));
                    //qsElement.Add(Query.EQ("RigName", wellActivity.RigName));
                    var queryElem = Query.And(qsElement);
                    var dtElement = WellPIP.Get<WellPIP>(queryElem);
                    var mLEPIP = new BsonDocument();
                    if (dtElement != null)
                    {
                        mLEPIP = dtElement.UpdateMonthlyFromPIP(dtElement);
                    }

                    string url = (HttpContext.Request).Path.ToString();
                    WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName,
                        WebTools.LoginUser.Email, LogType.AddNewPhase, activity.TableName, url, activity.ToBsonDocument(), phase.ToBsonDocument());
                    MyMessage = "Data Saved!";
                }
                else
                {
                    UpdStatus = false;
                    MyMessage = "Can't Add event, because " + strMsg + " has active Weekly Report.";
                }

                return Json(new { Success = UpdStatus, Message = MyMessage }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult DeletePhase(int id, int PhaseNo, string ActivityType = "")
        {
            try
            {
                WellActivity wa = WellActivity.Get<WellActivity>(Query.EQ("_id", id));

                bool isSuccessDel = false;
                WellActivityPhase phaseDelete = new WellActivityPhase();
                if (wa.Phases.Where(d => d.PhaseNo.Equals(PhaseNo)).Count() > 0)
                {
                    WellActivityPhase phase = wa.Phases.FirstOrDefault(d => d.PhaseNo.Equals(PhaseNo));

                    var queries1 = new List<IMongoQuery>();
                    queries1.Add(Query.EQ("WellName", wa.WellName));
                    queries1.Add(Query.EQ("SequenceId", wa.UARigSequenceId));
                    queries1.Add(Query.EQ("Phase.PhaseNo", PhaseNo));
                    queries1.Add(Query.EQ("Phase.ActivityType", ActivityType));

                    var queries2 = new List<IMongoQuery>();
                    queries2.Add(Query.EQ("WellName", wa.WellName));
                    queries2.Add(Query.EQ("SequenceId", wa.UARigSequenceId));
                    queries2.Add(Query.EQ("PhaseNo", PhaseNo));
                    queries2.Add(Query.EQ("Phase.ActivityType", ActivityType));

                    if (WellActivityUpdate.Populate<WellActivityUpdate>(Query.And(queries1.ToArray())).Count > 0)
                    {
                        return Json(new { Success = false, Message = "Phase that used in Weekly Report cannot be deleted!" }, JsonRequestBehavior.AllowGet);
                    }
                    else if (WellPIP.Populate<WellPIP>(Query.And(queries2.ToArray())).Count > 0)
                    {
                        return Json(new { Success = false, Message = "Phase that used in PIP Configuration cannot be deleted!" }, JsonRequestBehavior.AllowGet);
                    }
                    //MLE
                    //else if (WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(Query.And(queries1.ToArray())).Count > 0)
                    //{
                    //    return Json(new { Success = false, Message = "Phase that used in Monthly Report cannot be deleted!" }, JsonRequestBehavior.AllowGet);
                    //}
                    phaseDelete = phase;
                    isSuccessDel = true;
                }

                List<WellActivityPhase> wap = new List<WellActivityPhase>();
                if (wa.Phases.Any())
                {
                    wa.Phases.RemoveAll(x => x.ActivityType.Equals(ActivityType) && x.PhaseNo == PhaseNo);
                    wa.Save();
                }

                #region delete Bizlan
                if (isSuccessDel)
                {
                    var qs = new List<IMongoQuery>();
                    qs.Add(Query.EQ("WellName", wa.WellName));
                    qs.Add(Query.EQ("UARigSequenceId", wa.UARigSequenceId));
                    qs.Add(Query.EQ("Phases.Estimate.RigName", wa.RigName));
                    qs.Add(Query.EQ("Phases.PhaseNo", PhaseNo));
                    qs.Add(Query.EQ("Phases.ActivityType", ActivityType));

                    var actv = BizPlanActivity.Get<BizPlanActivity>(Query.And(qs));
                    if (actv != null)
                    {
                        //DataHelper.Delete(actv.TableName, Query.EQ("_id", id));
                        actv.Phases.RemoveAll(x => x.ActivityType.Equals(ActivityType) && x.PhaseNo == PhaseNo);
                        actv.Save();
                        DataHelper.Save(actv.TableName, actv.ToBsonDocument());

                    }
                }

                #endregion


                if (isSuccessDel)
                {
                    string url = (HttpContext.Request).Path.ToString();
                    WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName,
                  WebTools.LoginUser.Email, LogType.DeletePhase, wa.TableName, url, phaseDelete.ToBsonDocument(), null);

                }

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }
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

        private static double getPlanValueForDefaultOP(List<WellActivityPhase> Phases, string DateStart, string DateFinish, string DateStart2, string DateFinish2,
            string DateRelation, string DaysOrCost = "Days", string DefaultOP = "OP15")
        {
            var ret = 0.0;
            if (Phases.Any())
            {
                foreach (var Phase in Phases)
                {
                    var target = Phase.PlanSchedule;
                    DateRelation = (DateRelation == null ? "AND" : DateRelation);
                    DateStart = (DateStart == null ? "" : DateStart);
                    DateFinish = (DateFinish == null ? "" : DateFinish);
                    DateStart2 = (DateStart2 == null ? "" : DateStart2);
                    DateFinish2 = (DateFinish2 == null ? "" : DateFinish2);

                    string format = "yyyy-MM-dd";
                    CultureInfo culture = CultureInfo.InvariantCulture;

                    var periodStart = Tools.ToUTC(DateStart != "" ? DateTime.ParseExact(DateStart, format, culture) : new DateTime(1900, 1, 1));
                    var periodFinish = Tools.ToUTC(DateFinish != "" ? DateTime.ParseExact(DateFinish, format, culture) : new DateTime(3000, 1, 1));
                    var periodStart2 = Tools.ToUTC(DateStart2 != "" ? DateTime.ParseExact(DateStart2, format, culture) : new DateTime(1900, 1, 1));
                    var periodFinish2 = Tools.ToUTC(DateFinish2 != "" ? DateTime.ParseExact(DateFinish2, format, culture) : new DateTime(3000, 1, 1));

                    bool start = false;
                    bool finish = false;

                    if (!DateStart.Equals("") && !DateFinish.Equals(""))
                        start = (target.Start >= periodStart) && (target.Start <= periodFinish);
                    else if (!DateStart.Equals(""))
                        start = (target.Start >= periodStart);
                    else if (!DateFinish.Equals(""))
                        start = (target.Start <= periodFinish);
                    else
                        start = true;

                    if (!DateStart2.Equals("") && !DateFinish2.Equals(""))
                        finish = (target.Finish >= periodStart2) && (target.Finish <= periodFinish2);
                    else if (!DateStart2.Equals(""))
                        finish = (target.Finish >= periodStart2);
                    else if (!DateFinish2.Equals(""))
                        finish = (target.Finish <= periodFinish2);
                    else
                        finish = true;

                    var matchPeriod = (DateRelation.Equals("AND") ? (start && finish) : (start || finish));

                    if (matchPeriod)
                    {
                        if (Phase.BaseOP.Contains(DefaultOP))
                        {
                            if (DaysOrCost.ToLower().Equals("days"))
                            {
                                ret += Phase.Plan.Days;
                            }
                            else
                            {
                                ret += Phase.Plan.Cost;
                            }
                        }

                    }
                }
            }

            return ret;
        }
        public JsonResult GetWellSequenceInfo2(WaterfallBase wb)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                string previousOP = "";
                string nextOP = "";
                var DefaultOP = getBaseOP(out previousOP, out nextOP);

                var GetMasterOPs = MasterOP.Populate<MasterOP>();

                var division = 1000000.0;
                wb.GetWhatData = "OP";
                //if (wb.OPs == null)
                //{
                //    var OPs = new List<string>();
                //    foreach (var op in GetMasterOPs.OrderBy(x => x.Name).ToList())
                //    {
                //        OPs.Add(op.Name);
                //    }
                //    wb.OPs = OPs;
                //}
                var bizplan = wb.GetActivitiesForBizPlan(WebTools.LoginUser.Email);
                wb.inlastuploadls = false;
                var wa = wb.GetActivitiesAndPIPs(false,bizplan);
                var raw = new List<WellActivity>();
                if (wa.Activities.Any()) {
                    raw.AddRange(wa.Activities.Where(x=>x.Phases != null && x.Phases.Count > 0).ToList());
                }

                //WebTools.LoginUser.LineOfBusiness
                //var raw2 = raw.Where(x => x.WellName.Equals(new BsonArray()));
                var final2 = new List<object>();

                foreach (var d in raw)
                {

                    #region panjang
                    if (d.WellName == "BRUTUS A6 ST2")
                    {
                        var st = "";
                    }

                    //panjang update
                    //var PreviousPsStart = Tools.DefaultDate;
                    //var PreviousPsFinish = Tools.DefaultDate;
                    //var PsStart = Tools.DefaultDate;
                    //var PsFinish = Tools.DefaultDate;

                    //var PreviousLEStart = Tools.DefaultDate;
                    //var PreviousLEFinish = Tools.DefaultDate;
                    //var CurrentLEStart = Tools.DefaultDate;
                    //var CurrentLEFinish = Tools.DefaultDate;
                    //var LEStart = Tools.DefaultDate;
                    //var LEEnd = Tools.DefaultDate;


                    //var PlanDuration = 0.0;
                    //var PlanCost = 0.0;
                    //var PreviousPlanDuration = 0.0;
                    //var PreviousPlanCost = 0.0;

                    //var CurrentLEDuration = 0.0;
                    //var CurrentLECost = 0.0;
                    //var PreviousLEDuration = 0.0;
                    //var PreviousLECost = 0.0;
                    //var LEDuration = 0.0;
                    //var LECost = 0.0;

                    //if (wb.OPs != null)
                    //{
                    //    if (wb.OPs.Count() == 1)
                    //    {
                    //        if (wb.OPs.FirstOrDefault() == previousOP)
                    //        {
                    //            var hh = d.Phases.Where(x => x.BaseOP.Contains(previousOP));

                    //        }
                    //        else
                    //        {
                    //            var hh = d.Phases.Where(x => x.BaseOP.Contains(DefaultOP));
                    //        }
                    //    }
                    //}

                    //var checkHistory = d.OPHistories.Where(x => x.Type.Equals(previousOP) && x.PlanSchedule.Start > Tools.DefaultDate && x.PlanSchedule.Finish > Tools.DefaultDate);
                    //if (checkHistory.Any())
                    //{
                    //    PreviousPsStart = checkHistory.Min(x => x.PlanSchedule.Start);
                    //    PreviousPsFinish = checkHistory.Max(x => x.PlanSchedule.Finish);

                    //    PreviousPlanDuration = checkHistory.Sum(x => x.Plan.Days);
                    //    PreviousPlanCost = checkHistory.Sum(x => x.Plan.Cost);
                    //}
                    //#region
                    ////else
                    ////{
                    ////    if (d.Phases.Any(x => x.BaseOP.Contains(previousOP)))
                    ////    {
                    ////        var vl = d.Phases.Where(x => x.BaseOP.Contains(previousOP) && x.PlanSchedule.Start > Tools.DefaultDate);
                    ////        if (vl.Any())
                    ////        {
                    ////            PreviousPsStart = vl.Min(x => x.PlanSchedule.Start);
                    ////            PreviousPsFinish = vl.Max(x => x.PlanSchedule.Finish);

                    ////            PreviousPlanDuration = vl.Sum(x => x.Plan.Days);
                    ////            PreviousPlanCost = vl.Sum(x => x.Plan.Cost);
                    ////        }
                    ////    }
                    ////}
                    //#endregion
                    //if (d.Phases.Any(x => x.BaseOP.Contains(DefaultOP)))
                    //{
                    //    var vl = d.Phases.Where(x => x.BaseOP.Contains(DefaultOP) && x.PlanSchedule.Start > Tools.DefaultDate);
                    //    if (vl.Any())
                    //    {
                    //        PsStart = vl.Min(x => x.PlanSchedule.Start);
                    //        PsFinish = vl.Max(x => x.PlanSchedule.Finish);

                    //        PlanDuration = vl.Sum(x => x.Plan.Days);
                    //        PlanCost = vl.Sum(x => x.Plan.Cost);
                    //    }
                    //}

                    //if (wb.OPs != null)
                    //{
                    //    if (wb.OPs.Contains(previousOP))
                    //    {
                    //        checkHistory = d.OPHistories.Where(x => x.Type.Equals(previousOP) && x.LESchedule.Start > Tools.DefaultDate && x.LESchedule.Finish > Tools.DefaultDate);
                    //        if (checkHistory.Any())
                    //        {
                    //            foreach (var xx in checkHistory)
                    //            {
                    //                if (LEStart == Tools.DefaultDate)
                    //                    LEStart = xx.LESchedule.Start;
                    //                if (xx.LESchedule.Start < LEStart)
                    //                    LEStart = xx.LESchedule.Start;
                    //                if (xx.LESchedule.Finish > LEEnd)
                    //                    LEEnd = xx.LESchedule.Finish;

                    //                LEDuration += new DateRange(LEStart, LEEnd).Days;
                    //                LECost += xx.LE.Cost;
                    //            }
                    //        }
                    //    }

                    //    if (wb.OPs.Contains(DefaultOP))//wb.opRelation.ToLower() == "and"
                    //    {
                    //        if (d.Phases.Any(x => x.BaseOP.Contains(DefaultOP)))
                    //        {
                    //            var vl = d.Phases.Where(x => x.BaseOP.Contains(DefaultOP) && x.LESchedule.Start > Tools.DefaultDate);
                    //            foreach (var xx in vl)
                    //            {
                    //                if (LEStart == Tools.DefaultDate)
                    //                    LEStart = xx.LESchedule.Start;
                    //                if (xx.LESchedule.Start < LEStart)
                    //                    LEStart = xx.LESchedule.Start;
                    //                if (xx.LESchedule.Finish > LEEnd)
                    //                    LEEnd = xx.LESchedule.Finish;

                    //                LEDuration += new DateRange(LEStart, LEEnd).Days;
                    //                LECost += xx.LE.Cost;
                    //            }
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //        checkHistory = d.OPHistories.Where(x => x.Type.Equals(previousOP) && x.LESchedule.Start > Tools.DefaultDate && x.LESchedule.Finish > Tools.DefaultDate);
                    //        if (checkHistory.Any())
                    //        {
                    //            foreach (var xx in checkHistory)
                    //            {
                    //                if (LEStart == Tools.DefaultDate)
                    //                    LEStart = xx.LESchedule.Start;
                    //                if (xx.LESchedule.Start < LEStart)
                    //                    LEStart = xx.LESchedule.Start;
                    //                if (xx.LESchedule.Finish > LEEnd)
                    //                    LEEnd = xx.LESchedule.Finish;

                    //                LEDuration += new DateRange(LEStart, LEEnd).Days;
                    //                LECost += xx.LE.Cost;
                    //            }
                    //        }

                    //        if (d.Phases.Any(x => x.BaseOP.Contains(DefaultOP)))
                    //        {
                    //            var vl = d.Phases.Where(x => x.BaseOP.Contains(DefaultOP) && x.LESchedule.Start > Tools.DefaultDate);
                    //            foreach (var xx in vl)
                    //            {
                    //                if (LEStart == Tools.DefaultDate)
                    //                    LEStart = xx.LESchedule.Start;
                    //                if (xx.LESchedule.Start < LEStart)
                    //                    LEStart = xx.LESchedule.Start;
                    //                if (xx.LESchedule.Finish > LEEnd)
                    //                    LEEnd = xx.LESchedule.Finish;

                    //                LEDuration += new DateRange(LEStart, LEEnd).Days;
                    //                LECost += xx.LE.Cost;
                    //            }
                    //        }
                    //}



                    //var LEAvailable = new List<WellActivityPhase>();
                    //if (wb.BaseOP == null)
                    //    LEAvailable = d.Phases.Where(x => x.LESchedule.Start > Tools.DefaultDate).ToList();// x.ActivityType.ToLower().Contains("risk") == false &&
                    //else
                    //    LEAvailable = d.Phases.Where(x => x.BaseOP.Contains(wb.BaseOP) && x.LESchedule.Start > Tools.DefaultDate).ToList();// x.ActivityType.ToLower().Contains("risk") == false &&

                    //if (LEAvailable.Any())
                    //{
                    //    LEStart = LEAvailable.Min(x => x.LESchedule.Start);
                    //    LEEnd = LEAvailable.Max(x => x.LESchedule.Finish);
                    //}
                    //foreach (var xx in LEAvailable)
                    //{
                    //    LEDuration += new DateRange(xx.LESchedule.Start, xx.LESchedule.Finish).Days;
                    //    LECost += xx.LE.Cost;
                    //}

                    #endregion

                    //old

                    var PreviousPsStart = Tools.DefaultDate;
                    var PreviousPsFinish = Tools.DefaultDate;
                    var PsStart = Tools.DefaultDate;
                    var PsFinish = Tools.DefaultDate;



                    var PreviousLEStart = Tools.DefaultDate;
                    var PreviousLEFinish = Tools.DefaultDate;
                    var CurrentLEStart = Tools.DefaultDate;
                    var CurrentLEFinish = Tools.DefaultDate;
                    var LEStart = Tools.DefaultDate;
                    var LEEnd = Tools.DefaultDate;

                    var OpsDuration = 0.0;
                    var OpsCost = 0.0;

                    var PlanDuration = 0.0;
                    var PlanCost = 0.0;
                    var PreviousPlanDuration = 0.0;
                    var PreviousPlanCost = 0.0;

                    var LEDays_DefaultOP = 0.0;
                    var LECost_DefaultOP = 0.0;
                    var LEDays_PreviousOP = 0.0;
                    var LECost_PreviousOP = 0.0;
                    var LEDays_CrossedOP = 0.0;
                    var LECost_CrossedOP = 0.0;
                    var LEDays_NonOP = 0.0;
                    var LECost_NonOP = 0.0;

                    var LEDays = 0.0;
                    var LECost = 0.0;

                    #region PlanSchedule
                    if (d.OPHistories.Any())
                    {
                        PreviousPsStart = d.OPHistories.Where(x => x.Type != null && x.Type.Equals(previousOP)).Count() == 0 ?
                            Tools.DefaultDate : d.OPHistories.Where(x => x.Type != null && x.Type.Equals(previousOP)).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Count() == 0 ?
                            Tools.DefaultDate : d.OPHistories.Where(x => x.Type != null && x.Type.Equals(previousOP)).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Min(x => x.PlanSchedule.Start);

                        PreviousPsFinish = d.OPHistories.Where(x => x.Type != null && x.Type.Equals(previousOP)).Count() == 0 ?
                            Tools.DefaultDate : d.OPHistories.Where(x => x.Type != null && x.Type.Equals(previousOP)).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Count() == 0 ?
                            Tools.DefaultDate : d.OPHistories.Where(x => x.Type != null && x.Type.Equals(previousOP)).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Max(x => x.PlanSchedule.Finish);
                    }
                    PsStart = d.Phases.Where(x => x.BaseOP.Contains(DefaultOP)).Count() == 0 ?
                        Tools.DefaultDate : d.Phases.Where(x => x.BaseOP.Contains(DefaultOP)).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Count() == 0 ?
                        Tools.DefaultDate : d.Phases.Where(x => x.BaseOP.Contains(DefaultOP)).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Min(x => x.PlanSchedule.Start);

                    PsFinish = d.Phases.Where(x => x.BaseOP.Contains(DefaultOP)).Count() == 0 ?
                        Tools.DefaultDate : d.Phases.Where(x => x.BaseOP.Contains(DefaultOP)).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Count() == 0 ?
                        Tools.DefaultDate : d.Phases.Where(x => x.BaseOP.Contains(DefaultOP)).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Max(x => x.PlanSchedule.Finish);
                    #endregion

                    var OPsList = new List<OPListHelperForDataBrowserGrid>();
                    var MasterOPs = GetMasterOPs.OrderBy(x => x.Name).ToList();
                    foreach (var op in MasterOPs)
                    {
                        var oplist = new OPListHelperForDataBrowserGrid();
                        oplist.BaseOP = op.Name;
                        OPsList.Add(oplist);
                    }

                    if (d.Phases.Any())
                    {
                        foreach (var ph in d.Phases)
                        {

                            foreach (var op in MasterOPs)
                            {
                                // decide is current op or histories
                                var PlanSchedule = new DateRange();
                                var PlanValue = new WellDrillData() { Days = 0.0, Cost = 0.0 };
                                var isOPHistory = false;
                                if (ph.BaseOP.Any())
                                {
                                    if (ph.BaseOP.OrderByDescending(x => x).FirstOrDefault() == op.Name)
                                    {
                                        //current op
                                        PlanSchedule = ph.PlanSchedule;
                                        PlanValue = ph.Plan;
                                    }
                                    else
                                    {
                                        //histories
                                        var getHistSchStart = d.OPHistories.Where(x => x.Type != null && x.Type.Equals(op.Name)).Count() == 0 ?
                                        Tools.DefaultDate : d.OPHistories.Where(x => x.Type != null && x.Type.Equals(op.Name)).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Count() == 0 ?
                                        Tools.DefaultDate : d.OPHistories.Where(x => x.Type != null && x.Type.Equals(op.Name)).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Min(x => x.PlanSchedule.Start);
                                        var getHistSchFinish = d.OPHistories.Where(x => x.Type != null && x.Type.Equals(op.Name)).Count() == 0 ?
                                        Tools.DefaultDate : d.OPHistories.Where(x => x.Type != null && x.Type.Equals(op.Name)).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Count() == 0 ?
                                        Tools.DefaultDate : d.OPHistories.Where(x => x.Type != null && x.Type.Equals(op.Name)).Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Max(x => x.PlanSchedule.Finish);
                                        PlanSchedule = new DateRange() { Start = getHistSchStart, Finish = getHistSchFinish };

                                        var GetOPHist = d.OPHistories.Where(x => x.Type != null && x.Type.Equals(op.Name) && null != x.ActivityType && x.ActivityType.Equals(ph.ActivityType) && x.PlanSchedule.Start != Tools.DefaultDate).FirstOrDefault();
                                        if (GetOPHist != null)
                                        {
                                            var ratioOPHist = GetRatio(GetOPHist.PlanSchedule, wb);
                                            var ratioLSHist = GetRatio(GetOPHist.PhSchedule, wb);
                                            var ratioLEHist = GetRatio(GetOPHist.LESchedule, wb);

                                            GetOPHist.CalcOP.Cost *= ratioOPHist;
                                            GetOPHist.CalcOP.Days *= ratioOPHist;
                                            GetOPHist.Plan.Cost *= ratioOPHist;
                                            if (GetOPHist.Plan.Cost < 5000) GetOPHist.Plan.Cost = GetOPHist.Plan.Cost * 1000000;
                                            GetOPHist.Plan.Days *= ratioOPHist;
                                            GetOPHist.OP.Cost *= ratioLSHist;
                                            GetOPHist.OP.Days *= ratioLSHist;
                                            GetOPHist.LE.Cost *= ratioLEHist;
                                            GetOPHist.LE.Days *= ratioLEHist;
                                            PlanValue = GetOPHist.Plan;
                                        }
                                        isOPHistory = true;
                                    }
                                }
                                else
                                {
                                    //non op
                                }
                                var tempOPSchedule = new DateRange();
                                // set schedule
                                if (OPsList.Where(x => x.BaseOP.Equals(op.Name)).FirstOrDefault().OPSchedule.Start == Tools.DefaultDate)
                                {
                                    var a = OPsList.FirstOrDefault(x => x.BaseOP.Equals(op.Name));
                                    if (a.OPSchedule.Start < PlanSchedule.Start)
                                        a.OPSchedule.Start = PlanSchedule.Start;


                                    if(PlanSchedule.Finish > a.OPSchedule.Finish)
                                        a.OPSchedule.Finish = PlanSchedule.Finish;
                                    //OPsList.Where(x => x.BaseOP.Equals(op.Name)).FirstOrDefault().OPSchedule.Start = PlanSchedule.Start;
                                    //OPsList.Where(x => x.BaseOP.Equals(op.Name)).FirstOrDefault().OPSchedule = tempOPSchedule;
                                }
                                else
                                {

                                    if (OPsList.Where(x => x.BaseOP.Equals(op.Name)).FirstOrDefault().OPSchedule.Start > PlanSchedule.Start && PlanSchedule.Start != Tools.DefaultDate)
                                    {
                                        OPsList.Where(x => x.BaseOP.Equals(op.Name)).FirstOrDefault().OPSchedule.Start = PlanSchedule.Start;
                                    }
                                    if (OPsList.Where(x => x.BaseOP.Equals(op.Name)).FirstOrDefault().OPSchedule.Finish < PlanSchedule.Finish)
                                    {
                                        OPsList.Where(x => x.BaseOP.Equals(op.Name)).FirstOrDefault().OPSchedule.Finish = PlanSchedule.Finish;
                                    }
                                }

                                // set op plan value
                                OPsList.Where(x => x.BaseOP.Equals(op.Name)).FirstOrDefault().OP.Days += PlanValue.Days;
                                OPsList.Where(x => x.BaseOP.Equals(op.Name)).FirstOrDefault().OP.Cost += Tools.Div(PlanValue.Cost, 1000000);
                            }

                            //var result = new List<OPListHelperForDataBrowserGrid>();
                            //SetOpListDate(OPsList,out result);

                            //OPsList = result;

                            var OPHist = d.OPHistories.Where(x => x.Type != null && x.Type.Equals(previousOP) && x.ActivityType.Equals(ph.ActivityType) && x.PhaseNo == ph.PhaseNo && x.PlanSchedule.Start != Tools.DefaultDate).FirstOrDefault();
                            var OPHistNonOP = d.OPHistories.Where(x => x.Type != null && x.Type.Equals("") && x.ActivityType.Equals(ph.ActivityType) && x.PhaseNo == ph.PhaseNo && x.PlanSchedule.Start != Tools.DefaultDate).FirstOrDefault();

                            //OpsDuration += ph.OP != null ? ph.OP.Days : 0.0;
                            OpsDuration += ph.PhSchedule != null ? ph.PhSchedule.Days : 0.0;
                            OpsCost += ph.OP != null ? ph.OP.Cost : 0.0;
                            LEDays += ph.LE.Days != null ? ph.LE.Days : 0;

                            if (OPHist != null)
                            {

                                var ratioOPHist = GetRatio(OPHist.PlanSchedule, wb);
                                var ratioLSHist = GetRatio(OPHist.PhSchedule, wb);
                                var ratioLEHist = GetRatio(OPHist.LESchedule, wb);

                                OPHist.CalcOP.Cost *= ratioOPHist;
                                OPHist.CalcOP.Days *= ratioOPHist;
                                OPHist.Plan.Cost *= ratioOPHist;
                                if (OPHist.Plan.Cost < 5000) OPHist.Plan.Cost = OPHist.Plan.Cost * 1000000;
                                OPHist.Plan.Days *= ratioOPHist;
                                OPHist.OP.Cost *= ratioLSHist;
                                OPHist.OP.Days *= ratioLSHist;
                                OPHist.LE.Cost *= ratioLEHist;
                                OPHist.LE.Days *= ratioLEHist;

                                PreviousPlanDuration += OPHist.Plan.Days;
                                PreviousPlanCost += OPHist.Plan.Cost;

                                //(b) LE 2014 = select OP base 2014, no date filter, get sum of LE
                                var LE14_Days = 0.0;
                                var LE14_Cost = 0.0;
                                var checkWR = new WellActivity();
                                if (checkWR.isHaveWeeklyReport2(d.WellName, d.UARigSequenceId, ph.ActivityType))
                                {
                                    LE14_Days = OPHist.LE.Days;
                                    LE14_Cost = OPHist.LE.Cost;
                                }
                                LECost_PreviousOP += LE14_Cost;
                                LEDays_PreviousOP += LE14_Days;
                            }

                            if (OPHistNonOP != null)
                            {
                                //(d) NON OP
                                LECost_NonOP += OPHistNonOP.LE.Cost;
                                LEDays_NonOP += OPHistNonOP.LE.Days;
                            }

                            if (ph.BaseOP.Contains(DefaultOP))
                            {
                                PlanDuration += ph.Plan.Days;
                                PlanCost += ph.Plan.Cost;

                                // a. LE 2015  (default) = select OP base 2015, no date filter, get sum of LE
                                LECost_DefaultOP += ph.LE.Cost;
                                LEDays_DefaultOP += ph.LE.Days;

                                if (ph.BaseOP.Contains(previousOP))
                                {
                                    //(c) LE 2014 & 2015 Cross = select OP base 2014 and 2015, OP relation AND, get sum of LE
                                    LECost_CrossedOP += ph.LE.Cost;
                                    LEDays_CrossedOP += ph.LE.Days;
                                }
                            }

                        }
                    }
                    //LE OPs
                    #region LE OPs
                    if (wb.OPs != null && wb.OPs.Count > 0)
                    {
                        if (wb.OPs.Count == 1)
                        {
                            if (wb.OPs.FirstOrDefault() == DefaultOP)
                            {
                                //LEDays = LEDays_DefaultOP;
                                LECost = LECost_DefaultOP;
                            }
                            else
                            {
                                //LEDays = LEDays_PreviousOP;
                                LECost = LECost_PreviousOP;
                            }
                        }
                        else
                        {
                            if (wb.opRelation.ToLower() == "and")
                            {
                                //LEDays = LEDays_CrossedOP;
                                LECost = LECost_CrossedOP;
                            }
                            else
                            {
                                //ALL LE = select 2014 & 2015, OP relation OR, no date filter, get sum of LE should be equal to (a-c)+(b-c)+c
                                //a = DefaultOP, b = PreviousOP, c = CrossedOP
                                //LEDays = (LEDays_DefaultOP - LEDays_CrossedOP) + (LEDays_PreviousOP - LEDays_CrossedOP) + LEDays_CrossedOP;
                                LECost = (LECost_DefaultOP - LECost_CrossedOP) + (LECost_PreviousOP - LECost_CrossedOP) + LECost_CrossedOP;
                            }
                        }
                    }
                    else
                    {
                        //OP15 + OP14 + Non OP
                        //LEDays = LEDays_DefaultOP + LEDays_PreviousOP + LEDays_NonOP;
                        LECost = LECost_DefaultOP + LECost_PreviousOP + LECost_NonOP;
                    }
                    #endregion
                    List<string> listPlan = new List<string>();
                    foreach (var rplan in d.Phases)
                    {
                        var planComment = "";
                        planComment = rplan.IsInPlan == true ? planComment = "In Plan" : "Out Of Plan";
                        listPlan.Add(planComment);
                    }

                    final2.Add(new
                    {
                        _id = d._id,
                        Region = d.Region,
                        OperatingUnit = d.OperatingUnit,
                        UARigSequenceId = d.UARigSequenceId,
                        RigType = d.RigType,
                        RigName = d.RigName,
                        ProjectName = d.ProjectName,
                        WellName = d.WellName,
                        AssetName = d.AssetName,
                        NonOP = d.NonOP,
                        WorkingInterest = d.WorkingInterest,
                        //FirmOrOption = d.FirmOrOption,
                        IsInPlan = listPlan.Distinct(),
                        PsDuration = d.PsSchedule != null ? d.PsSchedule.Days : 0,
                        PlanDuration = PlanDuration,
                        PlanCost = Tools.Div(PlanCost, division),
                        PreviousPlanDuration = PreviousPlanDuration,
                        PreviousPlanCost = Tools.Div(PreviousPlanCost, division),
                        //OpsCost = Tools.Div(d.Phases.Count() > 0 ? d.Phases
                        //     .Where(e => e.ActivityType.ToLower().Contains("risk") == false)
                        //     .DefaultIfEmpty(new WellActivityPhase())
                        //     .Sum(e => e.OP.Cost) : 0, division),
                        //OpsDuration = d.OpsSchedule != null ? d.OpsSchedule.Days : 0,
                        OpsCost = Tools.Div(OpsCost, division),
                        OpsDuration = OpsDuration,
                        OpsStart = d.OpsSchedule != null ? d.OpsSchedule.Start : Tools.DefaultDate,
                        OpsFinish = d.OpsSchedule != null ? d.OpsSchedule.Finish : Tools.DefaultDate,
                        PhDuration = (d.Phases.Count() > 0 ? d.Phases.Sum(x => x.PhSchedule == null ? 0 : x.PhSchedule.Days) : 0),
                        UARigDescription = String.Format("{0} - {1}", d.UARigSequenceId, d.UARigDescription == null ? "" : d.UARigDescription),
                        PhRiskDuration = (d.Phases.Count() > 0 ? d.Phases.Where(x => x.RiskFlag == null ? false : !x.RiskFlag.Equals("")).Sum(x => x.PhSchedule == null ? 0 : x.PhSchedule.Days) : 0),
                        PhStartForFilter = (d.Phases.Count() > 0 ? d.Phases.Min(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Start) : Tools.DefaultDate),
                        PhFinishForFilter = (d.Phases.Count() > 0 ? d.Phases.Max(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Finish) : Tools.DefaultDate),
                        PsStartForFilter = (d.PsSchedule == null ? Tools.DefaultDate : d.PsSchedule.Start),
                        PsFinishForFilter = (d.PsSchedule == null ? Tools.DefaultDate : d.PsSchedule.Finish),
                        PsStart = PsStart,
                        PsFinish = PsFinish,
                        PreviousPsStart = PreviousPsStart,
                        PreviousPsFinish = PreviousPsFinish,
                        PhCost = (d.Phases.Count() > 0 ? (d.Phases.Sum(e => e.OP.Cost) / division) : 0),
                        AFEStart = (d.Phases.Count() > 0 ? d.Phases.Min(e => e.AFESchedule == null ? Tools.DefaultDate : e.AFESchedule.Start) : Tools.DefaultDate).ToString("dd-MMM-yy"),
                        AFEFinish = (d.Phases.Count() > 0 ? d.Phases.Max(e => e.AFESchedule == null ? Tools.DefaultDate : e.AFESchedule.Finish) : Tools.DefaultDate).ToString("dd-MMM-yy"),
                        AFEDuration = (d.Phases.Count() > 0 ? d.Phases.Sum(e => ((WellDrillData)Tools.Coelesce(new object[] { e.AFE, new WellDrillData() })).Days) : 0),
                        AFECost = (d.Phases.Count() > 0 ? (d.Phases.Sum(e => ((WellDrillData)Tools.Coelesce(new object[] { e.AFE, new WellDrillData() })).Cost) / division) : 0),
                        LEStart = d.LESchedule != null ? d.LESchedule.Start : Tools.DefaultDate,
                        LEFinish = d.LESchedule != null ? d.LESchedule.Finish : Tools.DefaultDate,
                        LEDuration = LEDays,//d.LESchedule != null ? d.LESchedule.Days : 0,
                        LECost = Tools.Div(LECost, division),//d.Phases.Count() > 0 ? d.Phases.Where(e => e.ActivityType.ToLower().Contains("risk") == false).DefaultIfEmpty(new WellActivityPhase()).Sum(e => e.LE.Cost)
                        VirtualPhase = d.VirtualPhase,
                        ShiftFutureEventDate = d.ShiftFutureEventDate,
                        TQDays = (d.Phases.Count() > 0 ? d.Phases.Sum(e => ((WellDrillData)Tools.Coelesce(new object[] { e.TQ, new WellDrillData() })).Days) : 0),
                        TQCost = (d.Phases.Count() > 0 ? (d.Phases.Sum(e => ((WellDrillData)Tools.Coelesce(new object[] { e.TQ, new WellDrillData() })).Cost) / division) : 0),
                        OPList = OPsList
                        //BICDays = (d.Phases.Count() > 0 ? d.Phases.Sum(e => ((WellDrillData)Tools.Coelesce(new object[] { e.BIC, new WellDrillData() })).Days) : 0),
                        //BICCost = (d.Phases.Count() > 0 ? (d.Phases.Sum(e => ((WellDrillData)Tools.Coelesce(new object[] { e.BIC, new WellDrillData() })).Cost) / division) : 0)
                    });
                }

                var final = final2.Select(x => x.ToBsonDocument()).OrderBy(x => x.GetString("RigName")).ThenBy(x => x.GetDateTime("PsStartForFilter")).ToList();

                return DataHelper.ToDictionaryArray(final);
            });
        }

        bool IsInPlanCheck(string well, string seq, string events, List<bool> InPlan)
        {
            var d = WellActivity.Get<WellActivity>(
                     Query.And(
                         Query.EQ("WellName", well),
                         Query.EQ("UARigSequenceId", seq),
                         Query.EQ("Phases.ActivityType", events),
                         Query.In("Phases.IsInPlan", new BsonArray(InPlan))
                     )
                 );
            bool isInPlan = false;
            if (d != null)
            {
                isInPlan = true;
            }
            return isInPlan;
        }

        public void SetOpListDate(List<OPListHelperForDataBrowserGrid> datas, out List<OPListHelperForDataBrowserGrid> result)
        {
            var get = new List<OPListHelperForDataBrowserGrid>();
            var StartDate = datas.Where(x => x.OPSchedule.Start != Tools.DefaultDate);
            DateTime MinStartDate = Tools.DefaultDate;
            if (StartDate.Any())
            {
                MinStartDate = StartDate.Min(x => x.OPSchedule.Start);
            }
            var FinishDate = datas.Where(x => x.OPSchedule.Finish != Tools.DefaultDate);
            DateTime MinFinishDate = Tools.DefaultDate;
            if (FinishDate.Any())
            {
                MinFinishDate = FinishDate.Min(x => x.OPSchedule.Finish);
            }
            foreach (var helper in datas)
            {
                if (MinStartDate != Tools.DefaultDate)
                {
                    if (helper.OPSchedule.Start == Tools.DefaultDate)
                    {
                        helper.OPSchedule.Start = MinStartDate;
                    }
                }
                if (MinFinishDate != Tools.DefaultDate)
                {
                    if (helper.OPSchedule.Finish == Tools.DefaultDate)
                    {
                        helper.OPSchedule.Finish = MinFinishDate;
                    }
                }
                get.Add(helper);

            }
            result = get;
        }


        public ActionResult SaveActivity(WellActivity wellActivity,bool IsInPlan, string eventName, bool virtualPhase = false, bool shiftFutureEventDate = false, bool isNewWell = false, string LineofBusiness = "")
        {
            try
            {
                bool isSaved = false; string MyMessage = "";
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
                string previousOP = "";
                string nextOP = "";
                var DefaultOP = getBaseOP(out previousOP, out nextOP);
                List<string> ActiveOp = new List<string>();
                ActiveOp.Add(DefaultOP);
                string strMsg = string.Format("WellName : {0}, Activity Type : {1}, ", wellActivity.WellName, eventName);
                wellActivity.Phases = new List<WellActivityPhase>();
                WellActivityPhase phase = new WellActivityPhase();
                //Check Weekly Report
                var t = WellActivity.isHaveWeeklyReport(wellActivity.WellName, wellActivity.UARigSequenceId, eventName);
                if ((wellActivity.OpsSchedule.Start != Tools.DefaultDate) && t == false)
                {
                    isSaved = true;
                    phase.IsInPlan = IsInPlan;
                    phase.PlanSchedule = wellActivity.PsSchedule;
                    phase.Plan.Days = wellActivity.PsSchedule.Days;
                    phase.PhSchedule = wellActivity.OpsSchedule;
                    phase.OP.Days = wellActivity.OpsSchedule.Days;
                    phase.EventCreatedFrom = "DataBrowser";
                    phase.PushToBizPlan = true;
                    var LEDays = phase.LE;
                    double LENewDays = 0.0;
                    if (LEDays == null)
                    {
                        LENewDays = 0.0;
                    }
                    else
                    {
                        LENewDays = LEDays.Days;
                    }
                    phase.LESchedule = new DateRange() { Start = phase.PhSchedule.Start, Finish = phase.PhSchedule.Start.AddDays(LENewDays) };
                    //phase.VirtualPhase = virtualPhase;
                    //phase.ShiftFutureEventDate = shiftFutureEventDate;
                    phase.ActivityType = eventName;
                    phase.PhaseNo = 1;
                    phase.BaseOP = ActiveOp;
                    phase.IsPostOP = true;
                    wellActivity.Phases.Add(phase);
                    wellActivity.VirtualPhase = virtualPhase;
                    wellActivity.ShiftFutureEventDate = shiftFutureEventDate;

                    wellActivity.Save("WEISWellActivities", references: new string[] { "updatetobizplan" });

                    if (isNewWell)
                    {
                        var lob = new List<string>();
                        lob.Add(LineofBusiness);
                        var newWell = new WellInfo();
                        newWell._id = wellActivity.WellName;
                        newWell.LoBs = lob;
                        try
                        {
                            newWell.Save();
                        }
                        catch (Exception e)
                        {

                        }
                    }

                    var mLe = wellActivity.CreateNewActivityUpdateMonthly(wellActivity, phase.PhaseNo, "DataBrowser");

                    string url = (HttpContext.Request).Path.ToString();
                    WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName,
                        WebTools.LoginUser.Email, LogType.NewWellActivity, wellActivity.TableName, url, wellActivity.ToBsonDocument(), null);

                    var qsElement = new List<IMongoQuery>();
                    qsElement.Add(Query.EQ("WellName", wellActivity.WellName));
                    qsElement.Add(Query.EQ("SequenceId", wellActivity.UARigSequenceId));
                    qsElement.Add(Query.EQ("ActivityType", phase.ActivityType));
                    var queryElem = Query.And(qsElement);
                    var dtElement = WellPIP.Get<WellPIP>(queryElem);
                    if (dtElement != null)
                    {
                        var mLEPIP = dtElement.UpdateMonthlyFromPIP(dtElement);
                    }
                    return Json(new { Success = isSaved, WellActivityId = wellActivity._id, WellName = wellActivity.WellName, RigName = wellActivity.RigName }, JsonRequestBehavior.AllowGet);
                    //return new { WellActivityId = wellActivity._id, WellName = wellActivity.WellName, RigName = wellActivity.RigName };
                }
                else
                {
                    MyMessage = "Can't Add event, because " + strMsg + " has active Weekly Report.";
                    return Json(new { Success = isSaved, Message = MyMessage }, JsonRequestBehavior.AllowGet);
                }

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult DeleteActivity(int id)
        {
            try
            {
                WellActivity activity = WellActivity.Get<WellActivity>(Query.EQ("_id", id));
                bool isSuccessDel = false;

                if (WellActivityUpdate.Populate<WellActivityUpdate>(Query.EQ("SequenceId", activity.UARigSequenceId)).Count > 0)
                {
                    return Json(new { Success = false, Message = "Activity that have Phase used in Weekly Report cannot be deleted!" }, JsonRequestBehavior.AllowGet);
                }
                else if (WellPIP.Populate<WellPIP>(Query.EQ("SequenceId", activity.UARigSequenceId)).Count > 0)
                {
                    return Json(new { Success = false, Message = "Activity that have Phase used in PIP Configuration cannot be deleted!" }, JsonRequestBehavior.AllowGet);
                }
                isSuccessDel = true;
                DataHelper.Delete("WEISWellActivities", Query.EQ("_id", id));

                #region del busplan
                //var getbuz = BizPlanActivity.Get<BizPlanActivity>(id);
                //if (getbuz != null)
                //{
                //    getbuz.Delete();
                //}
                if (isSuccessDel)
                {
                    var qs = new List<IMongoQuery>();
                    qs.Add(Query.EQ("WellName", activity.WellName));
                    qs.Add(Query.EQ("UARigSequenceId", activity.UARigSequenceId));
                    qs.Add(Query.EQ("RigName", activity.RigName));
                    var getbuz = BizPlanActivity.Get<BizPlanActivity>(Query.And(qs));
                    if (getbuz != null)
                    {
                        DataHelper.Delete(getbuz.TableName, Query.EQ("_id", Convert.ToInt32(getbuz._id)));
                    }
                }

                #endregion
                if (isSuccessDel)
                {
                    string url = (HttpContext.Request).Path.ToString();
                    WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName,
                  WebTools.LoginUser.Email, LogType.DeleteActivity, activity.TableName, url, activity.ToBsonDocument(), null);

                }

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public double isThousand(double value)
        {
            if (value >= 0)
            {
                if (value >= 5000)
                {
                    value = Tools.Div(value, 1000000);
                }
            }
            else
            {
                if (value * (-1) >= 5000)
                {
                    value = Tools.Div(value, 1000000);
                }
            }

            return value;
        }

        public JsonResult Export(List<int> ID = null, WaterfallBase wbs = null, List<String> OPs = null, string opRelation = "AND")//
        {
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "DataBrowserExportTemplate.xlsx");
            //List<WellActivity> wellactiv = new List<WellActivity>();
            //if (ID != null)
            //{
            //    foreach (var id in ID)
            //    {
            //        WellActivity well = GetWellActivity(wbs, id, OPs, opRelation);
            //        wellactiv.Add(well);

            //    }
            //}
            var bizplan = wbs.GetActivitiesForBizPlan(WebTools.LoginUser.Email,ignoreLSCheck:true);
            wbs.inlastuploadls = false;
            var wa = wbs.GetActivitiesAndPIPs(false, bizplan, isIgnoreUploadLS: true);
            var raw = new List<WellActivity>();
            if (wa.Activities.Any())
            {
                raw.AddRange(wa.Activities.Where(x => x.Phases != null && x.Phases.Count > 0).ToList());
            }

            Workbook wb = new Workbook(xlst);
            var ws = wb.Worksheets[0];
            ws.Name = "Data Browser";
            int startRow = 3;
            int totalRow = 0;
            int SetFormat = 2;
            foreach (var well in raw)
            {
                if (well.Phases != null)
                {
                    Cells Cells = ws.Cells;
                    foreach (var phs in well.Phases)
                    {
                        totalRow++;
                        Cells["A" + startRow.ToString()].Value = well._id.ToString();
                        //add
                        Cells["B" + startRow.ToString()].Value = well.RigType;
                        Cells["C" + startRow.ToString()].Value = well.ProjectName;
                        Cells["D" + startRow.ToString()].Value = well.AssetName;
                        Cells["E" + startRow.ToString()].Value = well.Region;
                        Cells["F" + startRow.ToString()].Value = well.OperatingUnit;

                        Cells["G" + startRow.ToString()].Value = well.RigName;
                        Cells["H" + startRow.ToString()].Value = well.WellName;
                        Cells["I" + startRow.ToString()].Value = well.UARigSequenceId;


                        Cells["J" + startRow.ToString()].Value = phs.ActivityType;
                        Cells["K" + startRow.ToString()].Value = ConvertBaseOP(phs.BaseOP);


                        var lastop = phs.BaseOP.OrderByDescending(x => x).FirstOrDefault();

                        switch (lastop)
                        {
                            case "OP16":
                                {
                                    #region last OP 16

                                    try
                                    {

                                        var yyy = (well.OPHistories != null && well.OPHistories.Count() > 0) ? well.OPHistories.Where(x => x.ActivityType.Equals(phs.ActivityType) && x.Type.Equals("OP14")) : null;
                                        if (yyy != null && yyy.Count() > 0)
                                        {
                                            var op14 = yyy.FirstOrDefault();
                                            // 14
                                            Cells["L" + startRow.ToString()].Value = op14.PlanSchedule.Start.ToString("MM/dd/yyyy") == "01/01/1900" || op14.PlanSchedule.Start.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : op14.PlanSchedule.Start;
                                            Cells["M" + startRow.ToString()].Value = op14.PlanSchedule.Finish.ToString("MM/dd/yyyy") == "01/01/1900" || op14.PlanSchedule.Finish.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : op14.PlanSchedule.Finish;
                                            Cells["N" + startRow.ToString()].Value = op14.Plan.Days;
                                            Cells["O" + startRow.ToString()].Value = isThousand(op14.Plan.Cost);
                                        }
                                        var ttt = (well.OPHistories != null && well.OPHistories.Count() > 0) ? well.OPHistories.Where(x => x.ActivityType.Equals(phs.ActivityType) && x.Type.Equals("OP15")) : null;
                                        if (ttt != null && ttt.Count() > 0)
                                        {
                                            var op15 = ttt.FirstOrDefault();
                                            // 15
                                            Cells["P" + startRow.ToString()].Value = op15.PlanSchedule.Start.ToString("MM/dd/yyyy") == "01/01/1900" || op15.PlanSchedule.Start.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : op15.PlanSchedule.Start;
                                            Cells["Q" + startRow.ToString()].Value = op15.PlanSchedule.Finish.ToString("MM/dd/yyyy") == "01/01/1900" || op15.PlanSchedule.Finish.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : op15.PlanSchedule.Finish;
                                            Cells["R" + startRow.ToString()].Value = op15.Plan.Days;
                                            Cells["S" + startRow.ToString()].Value = isThousand(op15.Plan.Cost);
                                        }
                                    }
                                    catch(Exception ex)
                                    {

                                    }


                                    // 16
                                    Cells["T" + startRow.ToString()].Value = phs.PlanSchedule.Start.ToString("MM/dd/yyyy") == "01/01/1900" || phs.PlanSchedule.Start.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : phs.PlanSchedule.Start;
                                    Cells["U" + startRow.ToString()].Value = phs.PlanSchedule.Finish.ToString("MM/dd/yyyy") == "01/01/1900" || phs.PlanSchedule.Finish.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : phs.PlanSchedule.Finish;
                                    Cells["V" + startRow.ToString()].Value = phs.Plan.Days;
                                    Cells["W" + startRow.ToString()].Value = isThousand(phs.Plan.Cost);

                                    #endregion

                                    break;
                                }
                            case "OP15":
                                {
                                    #region last OP 16

                                    var yyy =  ( well.OPHistories != null &&   well.OPHistories.Count() > 0 ) ?  well.OPHistories.Where(x => x.Type !=null && x.ActivityType.Equals(phs.ActivityType) && x.Type.Equals("OP14")) : null;
                                    if (yyy != null && yyy.Count() > 0)
                                    {
                                        var op14 = yyy.FirstOrDefault();
                                        // 14
                                        Cells["L" + startRow.ToString()].Value = op14.PlanSchedule.Start.ToString("MM/dd/yyyy") == "01/01/1900" || op14.PlanSchedule.Start.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : op14.PlanSchedule.Start;
                                        Cells["M" + startRow.ToString()].Value = op14.PlanSchedule.Finish.ToString("MM/dd/yyyy") == "01/01/1900" || op14.PlanSchedule.Finish.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : op14.PlanSchedule.Finish;
                                        Cells["N" + startRow.ToString()].Value = op14.Plan.Days;
                                        Cells["O" + startRow.ToString()].Value = isThousand(op14.Plan.Cost);
                                    }

                                    // 15
                                    Cells["P" + startRow.ToString()].Value = phs.PlanSchedule.Start.ToString("MM/dd/yyyy") == "01/01/1900" || phs.PlanSchedule.Start.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : phs.PlanSchedule.Start;
                                    Cells["Q" + startRow.ToString()].Value = phs.PlanSchedule.Finish.ToString("MM/dd/yyyy") == "01/01/1900" || phs.PlanSchedule.Finish.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : phs.PlanSchedule.Finish;
                                    Cells["R" + startRow.ToString()].Value = phs.Plan.Days;
                                    Cells["S" + startRow.ToString()].Value = isThousand(phs.Plan.Cost);

                                    #endregion
                                    break;
                                }
                            default:
                                {
                                    // 14
                                    Cells["L" + startRow.ToString()].Value = phs.PlanSchedule.Start.ToString("MM/dd/yyyy") == "01/01/1900" || phs.PlanSchedule.Start.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : phs.PlanSchedule.Start;
                                    Cells["M" + startRow.ToString()].Value = phs.PlanSchedule.Finish.ToString("MM/dd/yyyy") == "01/01/1900" || phs.PlanSchedule.Finish.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : phs.PlanSchedule.Finish;
                                    Cells["N" + startRow.ToString()].Value = phs.Plan.Days;
                                    Cells["O" + startRow.ToString()].Value = isThousand(phs.Plan.Cost);
                                    break;
                                }
                        }

                        // ls
                        Cells["X" + startRow.ToString()].Value = phs.PhSchedule.Start.ToString("MM/dd/yyyy") == "01/01/1900" || phs.PlanSchedule.Start.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : phs.PhSchedule.Start;
                        Cells["Y" + startRow.ToString()].Value = phs.PhSchedule.Finish.ToString("MM/dd/yyyy") == "01/01/1900" || phs.PlanSchedule.Finish.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : phs.PhSchedule.Finish;
                        Cells["Z" + startRow.ToString()].Value = phs.PhSchedule.Days; //phs.OP.Days;
                        Cells["AA" + startRow.ToString()].Value = isThousand(phs.OP.Cost);

                        // le

                        Cells["AB" + startRow.ToString()].Value = phs.LESchedule.Start.ToString("MM/dd/yyyy") == "01/01/1900" || phs.LESchedule.Start.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : phs.LESchedule.Start;
                        Cells["AC" + startRow.ToString()].Value = phs.LESchedule.Finish.ToString("MM/dd/yyyy") == "01/01/1900" || phs.LESchedule.Finish.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : phs.LESchedule.Finish;
                        Cells["AD" + startRow.ToString()].Value = phs.LE.Days;
                        Cells["AE" + startRow.ToString()].Value = isThousand(phs.LE.Cost);

                        // afe
                        Cells["AF" + startRow.ToString()].Value = phs.AFE.Days;
                        Cells["AG" + startRow.ToString()].Value = isThousand(phs.AFE.Cost);

                        // tq
                        Cells["AH" + startRow.ToString()].Value = phs.TQ.Days;
                        Cells["AI" + startRow.ToString()].Value = isThousand(phs.TQ.Cost);

                        // bic
                        Cells["AJ" + startRow.ToString()].Value = phs.BIC.Days;
                        Cells["AK" + startRow.ToString()].Value = isThousand(phs.BIC.Cost);
                        // target
                        Cells["AL" + startRow.ToString()].Value = phs.AggredTarget.Days;
                        Cells["AM" + startRow.ToString()].Value = isThousand(phs.AggredTarget.Cost);
                        // actual
                        Cells["AN" + startRow.ToString()].Value = phs.Actual.Days;
                        Cells["AO" + startRow.ToString()].Value = isThousand(phs.Actual.Cost);

                        Cells["AP" + startRow.ToString()].Value = phs.LastWeek.ToString("MM/dd/yyyy") == "01/01/0001" || phs.LastWeek.ToString("MM/dd/yyyy") == "01/01/1900" || phs.LastWeek.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : phs.LastWeek;
                        Cells["AQ" + startRow.ToString()].Value = phs.LastMonth.ToString("MM/dd/yyyy") == "01/01/0001" || phs.LastMonth.ToString("MM/dd/yyyy") == "01/01/1900" || phs.LastWeek.ToString("MM/dd/yyyy") == "12/31/1899" ? (DateTime?)null : phs.LastMonth;
                        Cells["AR" + startRow.ToString()].Value = well.WorkingInterest <= 1 ? (well.WorkingInterest * 100) + " %" : well.WorkingInterest + " %";
                        Cells["AS" + startRow.ToString()].Value = phs.FundingType;
                        Cells["AT" + startRow.ToString()].Value = phs.IsInPlan ? "YES" : "NO";

                        Style styleStart = Cells["L" + startRow.ToString()].GetStyle();
                        styleStart.Custom = "m/d/yyyy";
                        Cells["L" + startRow.ToString()].SetStyle(styleStart);
                        Cells["M" + startRow.ToString()].SetStyle(styleStart);
                        Cells["P" + startRow.ToString()].SetStyle(styleStart);
                        Cells["Q" + startRow.ToString()].SetStyle(styleStart);
                        Cells["T" + startRow.ToString()].SetStyle(styleStart);
                        Cells["U" + startRow.ToString()].SetStyle(styleStart);
                        Cells["X" + startRow.ToString()].SetStyle(styleStart);
                        Cells["Y" + startRow.ToString()].SetStyle(styleStart);
                        Cells["A" + startRow.ToString()].SetStyle(styleStart);
                        Cells["B" + startRow.ToString()].SetStyle(styleStart);
                        Cells["AP" + startRow.ToString()].SetStyle(styleStart);
                        Cells["AQ" + startRow.ToString()].SetStyle(styleStart);
                        startRow++; SetFormat++;
                    }
                    
                    //wb.Worksheets[0].AutoFitColumns();
                }
            }
            wb.Worksheets[0].AutoFitColumns();
            var timeFileName = Tools.ToUTC(DateTime.Now).ToString("yyyy-MM-dd-hhmmss");            
            var newFileNameSingle = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                     String.Format("DataBrowser-{0}.xlsx", timeFileName));

            string returnName = String.Format("DataBrowser-{0}.xlsx", timeFileName);

            wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            return Json(new { Success = true, Path = returnName }, JsonRequestBehavior.AllowGet);
        }

        public FileResult DownloadBrowserFile(string stringName, DateTime date)
        {
            //string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "DataBrowserExportTemplate.xlsx");
            var res = Path.Combine(Server.MapPath("~/App_Data/Temp"), stringName);
            //rename file
            var retstringName = "DataBrowser-" + date.ToString("yyyy-MM-dd-hhmmss") + ".xlsx";

            return File(res, Tools.GetContentType(".xlsx"), retstringName);
        }

        public WellActivity GetWellActivity(WaterfallBase wb = null, int id = 0, List<String> OPs = null, string opRelation = "AND")
        {
            wb.GetWhatData = "OP";
            string previousOP = "";
            string nextOP = "";
            var DefaultOP = getBaseOP(out previousOP, out nextOP);
            WellActivity upd = new WellActivity();
            var a = Query.EQ("_id", id);
            WellActivity ret = WellActivity.Get<WellActivity>(a);
            var selectedPhase = new List<WellActivityPhase>();
            var OPHistories = ret.OPHistories;

            if (ret.Phases.Any())
            {
                foreach (var ph in ret.Phases)
                {

                    var islastesLs = true;

                    var logLast = DataHelper.Populate("LogLatestLS", Query.And(
                    Query.EQ("Well_Name", ret.WellName),
                    Query.EQ("Activity_Type", ph.ActivityType),
                    Query.EQ("Rig_Name", ret.RigName)
                    ));

                    if (wb.inlastuploadls)
                    {
                        if (!logLast.Any())
                        {
                            islastesLs = false;
                        }
                    }
                    if (islastesLs)
                    {
                        var filterPeriod = wb.FilterPeriod(ph.PlanSchedule);
                        if (!filterPeriod) continue;

                        var wau = ph.GetLastUpdate(ret.WellName, ret.UARigSequenceId, false);


                        if (OPHistories.Any())
                        {
                            var prevOPHist = OPHistories.Where(x => x.Type != null && x.Type.Equals(previousOP) && x.ActivityType.Equals(ph.ActivityType));
                            var getSumPrevDays = 0.0;
                            var getSumPrevCost = 0.0;
                            var getPlanStart = Tools.DefaultDate;
                            var getPlanFinish = Tools.DefaultDate;
                            if (prevOPHist.Any())
                            {
                                getSumPrevDays = prevOPHist.Sum(x => x.Plan.Days);
                                getSumPrevCost = prevOPHist.Sum(x => x.Plan.Cost);
                                if (prevOPHist.Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Any())
                                {
                                    getPlanStart = prevOPHist.Where(x => x.PlanSchedule.Start != Tools.DefaultDate).Min(x => x.PlanSchedule.Start);
                                }
                                if (prevOPHist.Where(x => x.PlanSchedule.Finish != Tools.DefaultDate).Any())
                                {
                                    getPlanFinish = prevOPHist.Where(x => x.PlanSchedule.Finish != Tools.DefaultDate).Max(x => x.PlanSchedule.Finish);
                                }
                            }

                            ph.PreviousOP = new WellDrillData() { Days = getSumPrevDays, Cost = Tools.Div(getSumPrevCost, 1000000) };
                            ph.PreviousOPSchedule = new DateRange() { Start = getPlanStart, Finish = getPlanFinish };

                        }
                        else
                        {
                            ph.PreviousOP = new WellDrillData();
                            ph.PreviousOPSchedule = new DateRange();
                        }
                        ph.OP = new WellDrillData() { Days = ph.OP.Days, Cost = Tools.Div(ph.OP.Cost, 1000000) };
                        ph.Plan = new WellDrillData() { Days = ph.Plan.Days, Cost = Tools.Div(ph.Plan.Cost, 1000000) };
                        //ph.LE = new WellDrillData() { Days = new DateRange(Tools.ToUTC(ph.LESchedule.Start, true), Tools.ToUTC(ph.LESchedule.Finish, true)).Days, Cost = Tools.Div(ph.LE.Cost, 1000000) };
                        ph.LE = new WellDrillData() { Days = ph.LE.Days, Cost = Tools.Div(ph.LE.Cost, 1000000) };
                        ph.AFE = new WellDrillData() { Days = ph.AFE.Days, Cost = Tools.Div(ph.AFE.Cost, 1000000) };
                        ph.TQ = new WellDrillData() { Days = ph.TQ.Days, Cost = Tools.Div(ph.TQ.Cost, 1000000) };
                        ph.Actual = new WellDrillData() { Days = ph.Actual.Days, Cost = Tools.Div(ph.Actual.Cost, 1000000) };

                        if (!ph.BaseOP.Contains(DefaultOP))
                        {
                            ph.PlanSchedule = new DateRange();
                            ph.Plan = new WellDrillData();
                        }

                        if (OPs != null && OPs.Count > 0)
                        {
                            var BaseOP = ph.BaseOP.ToArray();
                            if (opRelation.ToLower() == "and")
                            {
                                //match
                                var match = true;
                                foreach (var op in OPs)
                                {
                                    match = Array.Exists(BaseOP, element => element == op);
                                    if (!match) break;
                                }
                                if (match)
                                    selectedPhase.Add(ph);
                            }
                            else
                            {
                                //contains
                                var match = false;
                                foreach (var op in OPs)
                                {
                                    match = Array.Exists(BaseOP, element => element == op);
                                    if (match) break;
                                }
                                if (match)
                                    selectedPhase.Add(ph);
                            }
                        }
                        else
                        {
                            selectedPhase.Add(ph);
                        }
                    }

                    
                }
            }
            if (ret.OpsSchedule == null) ret.OpsSchedule = new DateRange();
            if (ret.PsSchedule == null) ret.OpsSchedule = new DateRange();

            ret.Phases = selectedPhase;
            return ret;
        }

        public string ConvertBaseOP(List<string> BaseOP)
        {
            string result = "";
            if (BaseOP != null)
            {
                foreach (var Ops in BaseOP)
                {
                    if (BaseOP.IndexOf(Ops) == (BaseOP.Count - 1))
                    {
                        result += Ops;
                    }
                    else
                    {
                        result += Ops + " ,";
                    }
                }
            }
            return result;
        }
    }

}