using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.IO;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Aspose.Cells;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class BusplanSPController : Controller
    {
        private int PeriodeCount;
        // GET: Shell/DataModel
        public ActionResult Index()
        {
            //ViewBag.isReadOnly = "0";
            //if (_isReadOnly())
            //{
            //    ViewBag.isRO = "1";
            //}
            ViewBag.UserName = WebTools.LoginUser.UserName;
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

            string PreviousOP = "";
            string NextOP = "";
            string DefaultOP = getBaseOP(out PreviousOP, out NextOP);
            ViewBag.DefaultOP = DefaultOP;
            var lsInfo = new PIPAnalysisController().GetLatestLsDate();
            ViewBag.LatestLS = lsInfo;
            return View();
        }

        public ActionResult FiscalYear()
        {
            ViewBag.isReadOnly = "0";
            if (_isReadOnly())
            {
                ViewBag.isRO = "1";
            }
            return View();
        }

        public ActionResult BusplanTracker()
        {
            ViewBag.isReadOnly = "0";
            if (_isReadOnly())
            {
                ViewBag.isRO = "1";
            }
            string PreviousOP = "";
            string NextOP = "";
            string DefaultOP = getBaseOP(out PreviousOP, out NextOP);
            ViewBag.DefaultOP = DefaultOP;
            var lsInfo = new PIPAnalysisController().GetLatestLsDate();
            ViewBag.LatestLS = lsInfo;
            return View();
        }

        private static bool _isReadOnly()
        {
            //var Email = WebTools.LoginUser.Email;
            //List<string> Roles = WEISPerson.GetRolesByEmail(Email);
            //var ro = Roles.Where(x => x.ToUpper() == "RO-ALL");
            //bool isRO = false;
            //if (ro.Count() > 0)
            //{
            //    isRO = true;
            //}
            var isRO = new ReferenceFactorController().IsAllowedToEdit();
            return !isRO;
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

        public JsonResult CheckBizPlanConfig()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var cfg = Config.GetConfigValue("BizPlanConfig");
                if (cfg != null && Convert.ToBoolean(cfg))
                    return true;
                else
                    return false;
            });
        }

        public static List<BsonDocument> GetPercentageComplete(DateRange dr, string basedOn, List<string> Projects = null)
        {
            List<BsonDocument> bdocs = new List<BsonDocument>();
            DateTime start = new DateTime(dr.Start.Year, dr.Start.Month, dr.Start.Day);
            DateTime finish = new DateTime(dr.Finish.Year, dr.Finish.Month, dr.Finish.Day);
            var Imongos = new List<IMongoQuery>();
            //Imongos.Add(Query.EQ("Phases.Estimate.SaveToOP", "OP16"));
            //Imongos.Add(Query.Or(Query.EQ("Phases.Estimate.Status", "Complete"), Query.EQ("Phases.Estimate.Status", "Modified")));
            if (Projects != null && Projects.Count > 0)
            {
                Imongos.Add(Query.In("ProjectName", new BsonArray(Projects.ToArray())));
            }
            var WAs = BizPlanActivity.Populate<BizPlanActivity>(Imongos.Count > 0 ? Query.And(Imongos) : null);
            var ListProjects = WAs.Where(x => x.ProjectName != null).Select(x => x.ProjectName).Distinct().ToList();
            foreach (var t in ListProjects)
            {
                var EventMatchedProject1 = WAs.Where(x => t.Equals(x.ProjectName));
                if (EventMatchedProject1 != null)
                {
                    //Status complete dan Modified
                    var EventMatchedProject = EventMatchedProject1.SelectMany(d => d.Phases, (p, d) => new
                    {
                        RigName = p.RigName,
                        WellName = p.WellName,
                        SequenceId = p.UARigSequenceId,
                        ActivityType = d.ActivityType,
                        PhSchedule = d.PhSchedule,
                        PhStartYear = d.PhSchedule == null ? Tools.DefaultDate.Year : d.PhSchedule.Start.Year,
                        IsActualLE = d.IsActualLE,
                        Status = d.Estimate.Status,
                        SaveToOP = d.Estimate.SaveToOP
                    }).ToList();
                    dr.Start = start;
                    while (dr.Start <= dr.Finish)
                    {
                        BsonDocument d = new BsonDocument();
                        d.Set("_id", dr.Start.ToString("yyyy") + "-" + t);
                        d.Set("Date", dr.Start);
                        d.Set("ProjectName", t);
                        var matchedDataToYear = EventMatchedProject.Where(x => x.PhStartYear == dr.Start.Year).ToList();
                        if (matchedDataToYear != null && matchedDataToYear.Count > 0)
                        {
                            //ada event di tahun tsb
                            //isActual LE dr Plan RigName,WellName,SeqID,ActivityType

                            var adaLE = 0; var nonLE = 0;
                            var totOP16 = 0; var totOtherOP16 = 0;

                            var totalData = matchedDataToYear.Where(x => x.PhStartYear == dr.Start.Year).ToList();

                            BsonArray detail = new BsonArray();
                            foreach (var b in totalData)
                            {

                                //Imongos = new List<IMongoQuery>();
                                //var startDate = new DateTime(b.PhStartYear,1,1);
                                //var endDate = start.AddYears(1).AddDays(-1);

                                //Imongos.Add(Query.GTE("Phases.PhSchedule.Start", startDate));

                                //Imongos.Add(Query.LTE("Phases.PhSchedule.Start", endDate));

                                //if (b.WellName != null || b.WellName != "")
                                //{
                                //    Imongos.Add(Query.EQ("WellName", b.WellName));
                                //}
                                //if (b.RigName != null || b.RigName != "")
                                //{
                                //    Imongos.Add(Query.EQ("RigName", b.RigName));
                                //}
                                //if (b.SequenceId != null || b.SequenceId != "")
                                //{
                                //    Imongos.Add(Query.EQ("UARigSequenceId", b.SequenceId));
                                //}
                                //if (b.ActivityType != null || b.ActivityType != "")
                                //{
                                //    Imongos.Add(Query.EQ("Phases.ActivityType", b.ActivityType));
                                //}
                                ////var plans = WellActivity.Populate<WellActivity>(Query.And(Imongos))
                                ////    .SelectMany(x => x.Phases, (p, e) => new { 
                                ////        IsActualLE = e.IsActualLE,
                                ////        ActivityType = e.ActivityType
                                ////    }).ToList().Where(x=>x.ActivityType.ToUpper().Equals(b.ActivityType.ToUpper()));
                                //var plan = WellActivity.Get<WellActivity>(Query.And(Imongos)) ;

                                //var phases = plan != null ? plan.Phases.Select(x => new
                                //{
                                //    IsActualLE = x.IsActualLE,
                                //    ActivityType = x.ActivityType,
                                //    PhStartYear = x.PhSchedule == null ? Tools.DefaultDate.Year : x.PhSchedule.Start.Year
                                //}).Where(x => x.ActivityType.ToUpper().Equals(b.ActivityType.ToUpper()) && x.PhStartYear == dr.Start.Year).ToList() : null;

                                //if (phases != null)
                                //{
                                //    if (phases.Any())
                                //    {
                                //        adaLE += phases.Where(x => x.IsActualLE && x.PhStartYear == dr.Start.Year).ToList() != null ? phases.Where(x => x.IsActualLE && x.PhStartYear == dr.Start.Year).ToList().Count : 0 ;
                                //        nonLE += phases.Where(x => !x.IsActualLE && x.PhStartYear == dr.Start.Year).ToList() != null ? phases.Where(x => !x.IsActualLE && x.PhStartYear == dr.Start.Year).ToList().Count : 0 ; 

                                //    }

                                //}


                                //var b = a.ToList().OrderByDescending(x => x.UpdateVersion).FirstOrDefault();
                                var saveto = b.SaveToOP;
                                var stat = b.Status;
                                bool StatusOP16 = false;

                                if (saveto != null)
                                {
                                    if (stat != null)
                                    {
                                        StatusOP16 = b.SaveToOP.ToUpper().Equals("OP16") ? (b.Status.ToUpper().Equals("MODIFIED") || b.Status.ToUpper().Equals("COMPLETE")) : false;
                                    }

                                }

                                if (StatusOP16)
                                    totOP16++;
                                else
                                    totOtherOP16++;
                                var contentDetail = new BsonDocument();
                                contentDetail.Set("WellName", b.WellName);
                                contentDetail.Set("SequenceId", b.SequenceId);
                                var RigName = "";
                                var getRigName = WellActivity.Get<WellActivity>(Query.And(new List<IMongoQuery>() { Query.EQ("WellName", b.WellName), Query.EQ("UARigSequenceId", b.SequenceId), Query.EQ("Phases.ActivityType", b.ActivityType) }));
                                if (getRigName != null)
                                {
                                    RigName = getRigName.RigName;
                                }
                                contentDetail.Set("RigName", RigName);
                                //contentDetail.Set("IsActualLE", b.IsActualLE);
                                //if (phases != null)
                                //{
                                //    contentDetail.Set("IsActualLE", phases.Any() ? phases.FirstOrDefault().IsActualLE : false);                              
                                //}
                                //else
                                //{
                                //    contentDetail.Set("IsActualLE", false);                                
                                //}
                                contentDetail.Set("ActivityType", b.ActivityType);
                                contentDetail.Set("Status", stat != null ? stat : "");
                                detail.Add(contentDetail);
                            }
                            d.Set("DetailHaveLE", detail);

                            d.Set("CountHaveLE", totOP16);
                            d.Set("CountDontHaveLE", totOtherOP16);
                            d.Set("Total", totalData.Count);
                            var le = Tools.Div(Convert.ToDouble(totOP16), Convert.ToDouble(totalData.Count));
                            d.Set("LePercent", Math.Round(le, 2));
                            d.Set("adaEvent", true);
                        }
                        else
                        {
                            //no event
                            d.Set("CountHaveLE", 0);
                            d.Set("CountDontHaveLE", 0);
                            d.Set("Total", 0);
                            d.Set("LePercent", 0);
                            d.Set("adaEvent", false);
                            d.Set("DetailHaveLE", new BsonArray());
                        }

                        bdocs.Add(d);
                        dr.Start = dr.Start.AddYears(1);
                    }
                }
                else
                {
                    dr.Start = start;
                    while (dr.Start <= dr.Finish)
                    {
                        //no event
                        BsonDocument d = new BsonDocument();
                        d.Set("_id", dr.Start.ToString("yyyy") + "-" + t);
                        d.Set("Date", dr.Start);
                        d.Set("ProjectName", t);
                        d.Set("CountHaveLE", 0);
                        d.Set("CountDontHaveLE", 0);
                        d.Set("Total", 0);
                        d.Set("LePercent", 0);
                        d.Set("adaLE", false);
                        d.Set("DetailHaveLE", new BsonArray());

                        bdocs.Add(d);
                        dr.Start = dr.Start.AddYears(1);
                    }
                }
            }
            return bdocs;
        }


        public JsonResult GetDataTracker(bool onlyLE, string yearStart, string yearFinish, string basedOn, List<string> Projects = null, string OpRelation = "AND")
        {
            DateTime parmDate = new DateTime(Convert.ToInt32(yearStart), 01, 01);
            DateTime parmDate2 = new DateTime(Convert.ToInt32(yearFinish), 12, 31);
            DateRange dr = new DateRange(parmDate, parmDate2);


            var t = GetPercentageComplete(dr, basedOn, Projects).GroupBy(x => x.GetString("ProjectName"));

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


        public ActionResult SavePlan(BizPlanActivity data, bool saveToPlan, string status = "Draft")
        {

            //set status dari header ke estimate
            data.Phases.FirstOrDefault().Estimate.Status = status;

            //save value TroubleFreeCostBeforeLC.Days to NewTroubleFree.Days
            data.Phases.FirstOrDefault().Estimate.NewTroubleFree.Days = data.Phases.FirstOrDefault().Estimate.TroubleFreeBeforeLC.Days;

            var newbpa = new BizPlanActivity();
            var saveProcess = newbpa.SavePlanMethod(data, WebTools.LoginUser.UserName, status, true);
            return Json(new { Success = saveProcess.Success, Messages = saveProcess.Message }, JsonRequestBehavior.AllowGet);
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

        public JsonResult GetRigRates()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                List<RigRates> res = RigRates.Populate<RigRates>();
                List<RigRatesSimple> rigrates = new List<RigRatesSimple>();
                foreach (var a in res)
                {
                    if (a.Value != null && a.Value.Count > 0)
                    {
                        foreach (var b in a.Value)
                        {
                            var newRR = new RigRatesSimple();
                            newRR.RigName = a.Title;
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

        internal List<string> removeOldOPs(List<string> ops, string activeop)
        {
            var aco = Convert.ToInt32(activeop.Replace("OP", ""));
            List<string> ret = new List<string>();
            foreach (var t in ops)
            {
                var y = Convert.ToInt32(t.Replace("OP", ""));
                if (y >= aco)
                {
                    ret.Add(t);
                }
            }
            return ret;
        }

        public JsonResult GetMasterOPs()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var res = MasterOP.Populate<MasterOP>().Select(x => x.Name).ToList<string>();
                var activeOP = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "BaseOPConfig"));
                string curBaseOP = "OP15";
                if (activeOP.Any())
                {
                    curBaseOP = activeOP.FirstOrDefault().GetString("ConfigValue");
                }

                ri.Data = removeOldOPs(res.ToList(), curBaseOP);
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        private List<string> GetMasterOP()
        {
            var res = MasterOP.Populate<MasterOP>().Select(x => x.Name).ToList<string>();
            var activeOP = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "BaseOPConfig"));
            string curBaseOP = "OP15";
            if (activeOP.Any())
            {
                curBaseOP = activeOP.FirstOrDefault().GetString("ConfigValue");
            }

            return removeOldOPs(res.ToList(), curBaseOP);
        }

        public JsonResult GetRFMs(string BaseOP = "OP15", string Country = "United States")
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var rfm = new List<ProjectReferenceFactor>();
                var qs = new List<IMongoQuery>();
                qs.Add(Query.EQ("BaseOP", BaseOP));
                qs.Add(Query.EQ("Country", Country));

                var q = Query.And(qs);
                var getRFMs = WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>(q);
                if (getRFMs.Any())
                {
                    var rfms = getRFMs.Select(x => x.GroupCase).ToList();
                    rfm = ProjectReferenceFactor.Populate<ProjectReferenceFactor>(Query.In("ReferenceFactorModels", new BsonArray(rfms.ToArray())));
                }
                ri.Data = rfm;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }


        public JsonResult GetRFM(string GroupCase, string BaseOP, string Country, DateTime EventStartYear)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var g = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(

                    Query.And(
                    Query.EQ("Country", Country),
                    Query.EQ("GroupCase", GroupCase),
                    Query.EQ("BaseOP", BaseOP)

                    )
                    );

                double lcfactor = 0;
                if (g != null)
                {
                    var sm = g.SubjectMatters;
                    var lcf = sm.Where(x => x.Key.Equals("Learning Curve Factors")).FirstOrDefault();
                    var xxlcfactor = lcf.Value.Where(x => x.Key.Equals("Year_" + EventStartYear.Year.ToString())).FirstOrDefault().Value;
                    lcfactor = xxlcfactor;
                    //rfm = ProjectReferenceFactor.Populate<ProjectReferenceFactor>(Query.In("ReferenceFactorModels", new BsonArray(rfms.ToArray())));
                }
                ri.Data = lcfactor;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }


        public JsonResult GetLearningCurve(string BaseOP = "OP15")
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var rfm = new List<ProjectReferenceFactor>();
                var q = Query.EQ("BaseOP", BaseOP);
                var getRFMs = WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>(q);
                if (getRFMs.Any())
                {
                    var rfms = getRFMs.Select(x => x.GroupCase).ToList();
                    rfm = ProjectReferenceFactor.Populate<ProjectReferenceFactor>(Query.In("ReferenceFactorModels", new BsonArray(rfms.ToArray())));
                }

                if (rfm.Any())
                {
                    var yy = rfm.SelectMany(x => x.ReferenceFactorModels).Distinct().ToList<string>();

                    var refcatmod = WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>(
                        Query.And(
                            Query.In("GroupCase", new BsonArray(yy.ToArray())),
                            Query.EQ("BaseOP", BaseOP)
                                ));
                }

                ri.Data = rfm;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }


        public JsonResult getBaseOP()
        {
            //return DefaultOP;
            ResultInfo ri = new ResultInfo();
            try
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
                var nextOP = "OP" + ((DefaultOPYear).ToString());
                var previousOP = "OP" + ((DefaultOPYear - 1).ToString());
                ri.Data = new { previousOP = previousOP, nextOP = nextOP };
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult GetMaturityRisk()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var mr = MaturityRiskMatrix.Populate<MaturityRiskMatrix>();
                var mr_titleOnly = mr.Select(x => x.Title).ToList();
                ri.Data = new { MaturityRisk = mr, MaturityRiskTitleOnly = mr_titleOnly };
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }


        public JsonResult GetActivityCategory(string ActivityType)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var mr = ActivityMaster.Get<ActivityMaster>(Query.EQ("_id", ActivityType));
                if (mr != null)
                {
                    var mr_titleOnly = mr.ActivityCategory;
                    ri.Data = mr_titleOnly;
                }
                else
                    ri.Data = "";

            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }


        public JsonResult GetExchangeRates()
        {
            ResultInfo ri = new ResultInfo();
            try
            {

                var config3 = Config.Get<Config>("VisibleCurrency");
                if (config3 == null)
                {
                    List<MacroEconomic> res = MacroEconomic.Populate<MacroEconomic>(Query.NE("Currency", ""));
                    List<ExchangeRatesSimple> ex = new List<ExchangeRatesSimple>();
                    if (res != null && res.Count > 0)
                    {
                        foreach (var a in res)
                        {
                            if (a.ExchangeRate != null && a.ExchangeRate.AnnualValues.Count > 0)
                            {
                                foreach (var b in a.ExchangeRate.AnnualValues)
                                {
                                    var newRR = new ExchangeRatesSimple();
                                    newRR.Country = a.Country;
                                    newRR.Currency = a.Currency;
                                    newRR.Year = b.Year;
                                    newRR.Value = b.Value;
                                    newRR.BaseOP = a.BaseOP;
                                    ex.Add(newRR);
                                }
                            }
                        }
                    }
                    ri.Data = ex;
                }
                else
                {
                    var strlist = config3.ConfigValue.ToString().Split(',');

                    List<MacroEconomic> res = MacroEconomic.Populate<MacroEconomic>(Query.In("Currency", new BsonArray(strlist.ToList())));
                    List<ExchangeRatesSimple> ex = new List<ExchangeRatesSimple>();
                    if (res != null && res.Count > 0)
                    {
                        foreach (var a in res)
                        {
                            if (a.ExchangeRate != null && a.ExchangeRate.AnnualValues.Count > 0)
                            {
                                foreach (var b in a.ExchangeRate.AnnualValues)
                                {
                                    var newRR = new ExchangeRatesSimple();
                                    newRR.Country = a.Country;
                                    newRR.Currency = a.Currency;
                                    newRR.Year = b.Year;
                                    newRR.Value = b.Value;
                                    newRR.BaseOP = a.BaseOP;
                                    ex.Add(newRR);
                                }
                            }
                        }
                    }
                    ri.Data = ex;
                }

            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult GetLongLeads()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                List<LongLead> res = LongLead.Populate<LongLead>();
                List<LongLeadsSimple> longLeads = new List<LongLeadsSimple>();
                foreach (var a in res)
                {
                    if (a.Details != null && a.Details.Count > 0)
                    {
                        foreach (var b in a.Details)
                        {
                            var newRR = new LongLeadsSimple();
                            newRR.Title = a.Title;
                            newRR.Year = b.Year;
                            newRR.Value = b.TangibleValue;
                            newRR.MonthLead = b.MonthRequiredValue;
                            longLeads.Add(newRR);
                        }
                    }
                }
                ri.Data = longLeads;
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

        public JsonResult Select(int id, List<string> BaseOP = null, string opRelation = "", int PhaseNo = 0)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {

                List<string> was = WebTools.GetWellActivities();
                List<string> WellActivities = new List<string>();

                foreach (var wa in was)
                {
                    string[] waParts = wa.Split(new char[] { '|' });
                    string wellName = waParts[0];
                    string activityType = waParts[1];

                    WellActivities.Add(activityType);
                }

                BizPlanActivity upd = new BizPlanActivity();
                var a = Query.EQ("_id", id);
                var now = Tools.ToUTC(DateTime.Now);
                var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
                BizPlanActivity ret = BizPlanActivity.Get<BizPlanActivity>(a);
                List<BizPlanActivityPhase> phases = new List<BizPlanActivityPhase>();
                var tempPhase = new List<object>();
                foreach (var ph in ret.Phases) //.Where(x => x.LESchedule.Finish >= earlyThisMonth))
                {
                    if (ph.PhaseNo == PhaseNo)
                    {
                        ret.EXType = ph.FundingType;
                        var wau = ph.GetLastUpdate(ret.WellName, ret.UARigSequenceId, false);
                        if (ph.Estimate.MaturityLevel == null) ph.Estimate.MaturityLevel = "";


                        if (WellActivities.Contains("*") || WellActivities.Contains(ph.ActivityType))
                        {
                            //ph.OP.Cost = Tools.Div(ph.OP.Cost, 1000000);
                            //ph.Plan.Cost = Tools.Div(ph.Plan.Cost, 1000000);
                            //ph.LE.Cost = Tools.Div(ph.LE.Cost, 1000000);
                            //ph.AFE.Cost = Tools.Div(ph.AFE.Cost, 1000000);
                            //ph.TQ.Cost = Tools.Div(ph.TQ.Cost, 1000000);
                            //ph.Actual.Cost = Tools.Div(ph.Actual.Cost, 1000000);
                            //if (BaseOP != null)
                            //{
                            //    if (FunctionHelper.CompareBaseOP(BaseOP.ToArray(), ph.BaseOP.ToArray(), opRelation))
                            //    {
                            //        phases.Add(ph);
                            //    }
                            //}
                            //else
                            //{
                            //    phases.Add(ph);
                            //}
                            //if (ph.Estimate.isFirstInitiate)
                            //{
                            //    ph.Estimate.EventStartDate = Tools.DefaultDate;
                            //    ph.Estimate.EventEndDate = Tools.DefaultDate;
                            //    ph.Estimate.NewMean.Days = 0;
                            //    ph.Estimate.MeanCostMOD = 0;
                            //}

                            //ph.Estimate.MeanCostMOD = Tools.Div(ph.Estimate.MeanCostMOD, 1000000);
                            ph.Estimate.MeanCostMOD = ph.Estimate.MeanCostMOD;
                            phases.Add(ph);
                        }

                        //convert SelectedCurrency to USD (for material and service cost only)
                        var Currencyx = ph.Estimate.SelectedCurrency;
                        if (Currencyx != null && !Currencyx.Trim().ToUpper().Equals("USD"))
                        {
                            var datas = MacroEconomic.Populate<MacroEconomic>(Query.EQ("Currency", Currencyx));
                            if (datas.Any())
                            {
                                var cx = datas.Where(x => x.Currency.Equals(Currencyx)).FirstOrDefault();

                                var rate = cx.ExchangeRate.AnnualValues.Where(x => x.Year == ph.Estimate.EstimatePeriod.Start.Year).Any() ?
                                        cx.ExchangeRate.AnnualValues.Where(x => x.Year == ph.Estimate.EstimatePeriod.Start.Year).FirstOrDefault().Value
                                        : 0;

                                ph.Estimate.Services = ph.Estimate.Services * rate;
                                ph.Estimate.Materials = ph.Estimate.Materials * rate;

                            }
                        }
                        else
                        {
                            ph.Estimate.SelectedCurrency = ret.Currency;
                        }

                        if (ph.Estimate.isFirstInitiate) ph.Estimate.IsUsingLCFfromRFM = true;
                    }
                }
                ret.Phases = phases;

                //var weellActUpts = new List<WellActivityUpdate>();// WellActivityUpdate.Populate<WellActivityUpdate>(Query.And(Query.EQ("WellName", ret.WellName),Query.EQ("SequenceId", ret.UARigSequenceId),Query.EQ("Phase.ActivityType", ret.ActivityType)));
                //var mlyWellActUpts = new List<WellActivityUpdateMonthly>();// WellActivityUpdateMonthly.Populate<WellActivityUpdate>(Query.And(Query.EQ("WellName", ret.WellName), Query.EQ("SequenceId", ret.UARigSequenceId), Query.EQ("Phase.ActivityType", ret.ActivityType)));
                //if (ret.WellName != null && ret.UARigSequenceId != null && ret.ActivityType != null)
                //{
                //    weellActUpts = WellActivityUpdate.Populate<WellActivityUpdate>(Query.And(Query.EQ("WellName", ret.WellName),Query.EQ("SequenceId", ret.UARigSequenceId),Query.EQ("Phase.ActivityType", ret.ActivityType)));
                //    mlyWellActUpts = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(Query.And(Query.EQ("WellName", ret.WellName), Query.EQ("SequenceId", ret.UARigSequenceId), Query.EQ("Phase.ActivityType", ret.ActivityType)));
                //}

                //if (weellActUpts.Any())
                //    ret.WeeklyReport = mlyWellActUpts.OrderBy(x => x.UpdateVersion).FirstOrDefault().UpdateVersion;

                //if (mlyWellActUpts.Any())
                //    ret.MonthlyReport = mlyWellActUpts.OrderBy(x => x.UpdateVersion).FirstOrDefault().UpdateVersion;
                ret.LastUpdate = Tools.ToUTC(ret.LastUpdate);
                if (ret.Phases.Any() && ret.Phases.FirstOrDefault().Estimate != null)
                {
                    ret.Status = ret.Phases.FirstOrDefault().Estimate.Status;
                    if (ret.Phases.FirstOrDefault().Estimate.SaveToOP == null)
                        ret.Phases.FirstOrDefault().Estimate.SaveToOP = "";
                }

                ret.RigName = ret.Phases.FirstOrDefault().Estimate.RigName;

                return ret;
            });
        }

        public JsonResult GetWeeklyAndMonthlyUpdate(string WellName, string UARigSequenceId, string ActivityType)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                DateTime? WellActUpd = Tools.DefaultDate;
                DateTime? WellActUpdMly = Tools.DefaultDate;
                var weellActUpts = new List<WellActivityUpdate>();// WellActivityUpdate.Populate<WellActivityUpdate>(Query.And(Query.EQ("WellName", ret.WellName),Query.EQ("SequenceId", ret.UARigSequenceId),Query.EQ("Phase.ActivityType", ret.ActivityType)));
                var mlyWellActUpts = new List<WellActivityUpdateMonthly>();// WellActivityUpdateMonthly.Populate<WellActivityUpdate>(Query.And(Query.EQ("WellName", ret.WellName), Query.EQ("SequenceId", ret.UARigSequenceId), Query.EQ("Phase.ActivityType", ret.ActivityType)));
                if (WellName != null && UARigSequenceId != null && ActivityType != null)
                {
                    weellActUpts = WellActivityUpdate.Populate<WellActivityUpdate>(Query.And(Query.EQ("WellName", WellName), Query.EQ("SequenceId", UARigSequenceId), Query.EQ("Phase.ActivityType", ActivityType)));
                    mlyWellActUpts = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(Query.And(Query.EQ("WellName", WellName), Query.EQ("SequenceId", UARigSequenceId), Query.EQ("Phase.ActivityType", ActivityType)));
                }

                if (weellActUpts.Any())
                    WellActUpd = weellActUpts.OrderByDescending(x => x.UpdateVersion).FirstOrDefault().UpdateVersion;

                if (mlyWellActUpts.Any())
                    WellActUpdMly = mlyWellActUpts.OrderByDescending(x => x.UpdateVersion).FirstOrDefault().UpdateVersion;
                return new
                {
                    WellActUpd = WellActUpd,
                    WellActUpdMly = WellActUpdMly
                };
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

        private bool isMatchOP(List<string> BasePhaseOP, List<string> OPs, string opRelation)
        {
            var isMatchBaseOP = true;
            var BaseOP = BasePhaseOP.ToArray();
            if (opRelation.ToLower() == "and")
            {
                var match = true;
                foreach (var op in OPs)
                {
                    match = Array.Exists(BaseOP, element => element == op);
                    if (!match)
                    {
                        isMatchBaseOP = false;
                        break;
                    }
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
            }
            return isMatchBaseOP;
        }

        public static bool Contains(Array a, object val)
        {
            return Array.IndexOf(a, val) != -1;
        }

        public ActionResult UpdatePhase(int id, List<BizPlanActivityPhase> updatedPhases, BizPlanActivity updateActivity)
        {
            try
            {
                BizPlanActivity originalActivity = BizPlanActivity.Get<BizPlanActivity>(Query.EQ("_id", id));
                originalActivity.RigName = updateActivity.RigName;
                originalActivity.WellName = updateActivity.WellName;
                originalActivity.PsSchedule = updateActivity.PsSchedule;
                originalActivity.OpsSchedule = updateActivity.OpsSchedule;
                originalActivity.LESchedule = updateActivity.OpsSchedule;
                originalActivity.NonOP = updateActivity.NonOP;
                originalActivity.Region = updateActivity.Region;
                originalActivity.OperatingUnit = updateActivity.OperatingUnit;
                originalActivity.RigType = updateActivity.RigType;
                originalActivity.ProjectName = updateActivity.ProjectName;
                originalActivity.AssetName = updateActivity.AssetName;
                originalActivity.FirmOrOption = updateActivity.FirmOrOption;
                originalActivity.WorkingInterest = updateActivity.WorkingInterest;
                originalActivity.ShellShare = updateActivity.ShellShare;
                originalActivity.UARigDescription = updateActivity.UARigDescription;
                originalActivity.EXType = updateActivity.EXType;
                originalActivity.ShiftFutureEventDate = updateActivity.ShiftFutureEventDate;
                originalActivity.VirtualPhase = updateActivity.VirtualPhase;
                originalActivity.ReferenceFactorModel = updateActivity.ReferenceFactorModel;
                originalActivity.PerformanceUnit = updateActivity.PerformanceUnit;
                var status = "";
                if (updatedPhases.FirstOrDefault() != null)
                {
                    var other = originalActivity.Phases.Where(x => !x.PhaseNo.Equals(updatedPhases.FirstOrDefault().PhaseNo)).ToList();
                    var sg = originalActivity.Phases.FirstOrDefault(x => x.PhaseNo.Equals(updatedPhases.FirstOrDefault().PhaseNo));
                    originalActivity.Phases = other;
                    if (sg != null)
                    {
                        sg.FundingType = originalActivity.EXType;
                        originalActivity.Phases.Add(sg);
                        status = sg.Estimate.Status;
                        if (status.ToLower().Equals("complete") || status.ToLower().Equals("modified"))
                            sg.PushToWellPlan = true;
                    }
                    originalActivity.Phases.OrderBy(x => x.PhaseNo);
                }
                if (status.ToLower().Equals("complete") || status.ToLower().Equals("modified"))
                {
                    originalActivity.Save(references: new string[] { "updatetowellplan" });
                }
                else
                {
                    originalActivity.Save();
                }

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
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

                BizPlanActivity activity = BizPlanActivity.Get<BizPlanActivity>(Query.EQ("_id", ActivityId));
                activity.Phases = (activity.Phases == null ? new List<BizPlanActivityPhase>() : activity.Phases);
                var estimate = new BizPlanActivityEstimate();

                var services = 0;
                var materials = 0;
                estimate.Services = services;
                estimate.Materials = materials;
                estimate.EventStartDate = activity.PsSchedule.Start;
                estimate.EventEndDate = activity.PsSchedule.Finish;
                estimate.EstimatePeriod.Start = activity.PsSchedule.Start;
                estimate.EstimatePeriod.Finish = activity.PsSchedule.Finish;
                estimate.MaturityLevel = "Type 0";
                estimate.RigName = activity.RigName;
                estimate.NewTroubleFreeUSD = estimate.NewTroubleFree.Cost;
                estimate.NPTCostUSD = estimate.NewNPTTime.Cost;
                estimate.TECOPCostUSD = estimate.NewTECOPTime.Cost;
                estimate.MeanUSD = estimate.NewMean.Cost;

                estimate.ServicesUSD = services;
                estimate.MaterialsUSD = materials;


                var getMaturityRisk = MaturityRiskMatrix.Get<MaturityRiskMatrix>(Query.EQ("Title", "Type 0")) ?? new MaturityRiskMatrix();
                var NPTTime = estimate.NewTroubleFree.Days * Tools.Div(Tools.Div(getMaturityRisk.NPTTime, 100), 1 - Tools.Div(getMaturityRisk.NPTTime, 100));
                var NPTCost = estimate.NewTroubleFree.Cost * Tools.Div(Tools.Div(getMaturityRisk.NPTCost, 100), 1 - Tools.Div(getMaturityRisk.NPTCost, 100));

                estimate.NewNPTTime = new WellDrillPercentData() { Days = NPTTime, Cost = NPTCost, PercentCost = getMaturityRisk.NPTCost, PercentDays = getMaturityRisk.NPTTime };


                estimate.CurrentNPTTime = new WellDrillPercentData() { Days = 0, Cost = 0 };


                //TECOP
                var TECOPTime = estimate.NewTroubleFree.Days * Tools.Div(Tools.Div(getMaturityRisk.TECOPTime, 100), 1 - Tools.Div(getMaturityRisk.TECOPTime, 100));
                var TECOPCost = estimate.NewTroubleFree.Cost * Tools.Div(Tools.Div(getMaturityRisk.TECOPCost, 100), 1 - Tools.Div(getMaturityRisk.TECOPCost, 100));

                estimate.NewTECOPTime = new WellDrillPercentData() { PercentDays = getMaturityRisk.TECOPTime, PercentCost = getMaturityRisk.TECOPCost, Days = TECOPTime, Cost = TECOPCost };


                //material long lead
                var DateEscStartMaterial = estimate.EventStartDate;
                var tangibleValue = 0.0;
                var monthRequired = 0.0;
                var actType = "";
                //if (ActivityType.ToUpper().Contains("DRILLING"))
                //{
                //    actType = "DRILLING";
                //}
                //else if (ActivityType.ToUpper().Contains("COMPLETION"))
                //{
                //    actType = "COMPLETION";
                //}
                //else if (ActivityType.ToUpper().Contains("ABANDONMENT"))
                //{
                //    actType = "ABANDONMENT";
                //}

                //getActCategory
                var getActCategory = ActivityMaster.Get<ActivityMaster>(Query.EQ("_id", ActivityType));
                if (getActCategory != null)
                {
                    actType = getActCategory.ActivityCategory;
                }

                if (actType != "")
                {
                    var year = estimate.EventStartDate.Year;
                    var getLongLead = LongLead.Get<LongLead>(Query.EQ("Title", actType));
                    if (getLongLead != null && getLongLead.Details != null && getLongLead.Details.Count > 0)
                    {
                        var getTangible = getLongLead.Details.Where(y => y.Year.Equals(year)).FirstOrDefault();
                        if (getTangible != null)
                        {
                            tangibleValue = getTangible.TangibleValue;
                            monthRequired = getTangible.MonthRequiredValue;
                            var getMonthLongLead = Convert.ToInt32(-1 * Math.Round(monthRequired));
                            DateEscStartMaterial = estimate.EventStartDate.AddMonths(getMonthLongLead);
                        }
                    }
                }
                else
                {
                    tangibleValue = 0;
                    monthRequired = 0;
                    var getMonthLongLead = Convert.ToInt32(-1 * Math.Round(monthRequired));
                    DateEscStartMaterial = estimate.EventStartDate.AddMonths(getMonthLongLead);
                }

                if (DateEscStartMaterial < DateTime.Now)
                {
                    DateEscStartMaterial = DateTime.Now;
                }

                estimate.StartEscDateMaterial = DateEscStartMaterial;
                estimate.PercOfMaterialsLongLead = tangibleValue;
                estimate.LongLeadMonthRequired = monthRequired;

                var dr = new DateRange() { Start = Tools.ToUTC(PhStart), Finish = Tools.ToUTC(PhFinish) };
                estimate.NewTroubleFree.Days = dr.Days;


                activity.Phases.Add(new BizPlanActivityPhase()
                {
                    PhSchedule = new DateRange(Tools.ToUTC(PhStart), Tools.ToUTC(PhFinish)),
                    LESchedule = new DateRange(Tools.ToUTC(PhStart), Tools.ToUTC(PhFinish)),
                    OP = new WellDrillData()
                    {
                        Cost = 0,
                        Days = (Tools.ToUTC(PhFinish) - Tools.ToUTC(PhStart)).TotalDays
                    },
                    LE = new WellDrillData()
                    {
                        Cost = 0,
                        Days = (Tools.ToUTC(PhFinish) - Tools.ToUTC(PhStart)).TotalDays
                    },
                    ActivityType = ActivityType,
                    PhaseNo = (activity.Phases.Count == 0 ? 0 : activity.Phases.Max(x => x.PhaseNo)) + 1,
                    VirtualPhase = Virtual,
                    ShiftFutureEventDate = Shift,
                    Estimate = estimate,
                    FundingType = "",
                    BaseOP = new List<string>() { DefaultOP }
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
                BizPlanActivity wa = BizPlanActivity.Get<BizPlanActivity>(Query.EQ("_id", id));

                if (wa.Phases.Where(d => d.PhaseNo.Equals(PhaseNo)).Count() > 0)
                {
                    BizPlanActivityPhase phase = wa.Phases.FirstOrDefault(d => d.PhaseNo.Equals(PhaseNo));

                    var queries1 = new List<IMongoQuery>();
                    queries1.Add(Query.EQ("SequenceId", wa.UARigSequenceId));
                    queries1.Add(Query.EQ("Phase.PhaseNo", PhaseNo));

                    var queries2 = new List<IMongoQuery>();
                    queries2.Add(Query.EQ("SequenceId", wa.UARigSequenceId));
                    queries2.Add(Query.EQ("PhaseNo", PhaseNo));

                    if (WellActivityUpdate.Populate<WellActivityUpdate>(Query.And(queries1.ToArray())).Count > 0)
                    {
                        return Json(new { Success = false, Message = "Phase that used in Weekly Report cannot be deleted!" }, JsonRequestBehavior.AllowGet);
                    }
                    else if (WellPIP.Populate<WellPIP>(Query.And(queries2.ToArray())).Count > 0)
                    {
                        return Json(new { Success = false, Message = "Phase that used in PIP Configuration cannot be deleted!" }, JsonRequestBehavior.AllowGet);
                    }
                }

                List<BizPlanActivityPhase> wap = new List<BizPlanActivityPhase>();

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


        public JsonResult GetBizPlanActivity(WaterfallBase wb, List<string> Status)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var lsInfo = new PIPAnalysisController().GetLatestLsDate();
                string PreviousOP = "";
                string NextOP = "";
                string DefaultOP = getBaseOP(out PreviousOP, out NextOP);
                if (Status == null)
                {
                    Status = new List<string>();
                }

                var division = 1000000.0;
                var now = Tools.ToUTC(DateTime.Now);
                var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
                var email = WebTools.LoginUser.Email;
                wb.PeriodBase = "By Last Sequence";
                var raw = wb.GetActivitiesForBizPlan(email);
                var accessibleWells = WebTools.GetWellActivities();

                var final2 = new List<object>();
                var OPstate = GetMasterOP();

                foreach (var d in raw)
                {
                    var PreviousPsStart = Tools.DefaultDate;
                    var PreviousPsFinish = Tools.DefaultDate;
                    var PsStart = Tools.DefaultDate;
                    var PsFinish = Tools.DefaultDate;

                    var PlanDuration = 0.0;
                    var PlanCost = 0.0;
                    var PreviousPlanDuration = 0.0;
                    var PreviousPlanCost = 0.0;

                    var checkHistory = d.OPHistories.Where(x => null != x.Type && x.Type.Equals(PreviousOP) && x.PlanSchedule.Start > Tools.DefaultDate && x.PlanSchedule.Finish > Tools.DefaultDate);
                    if (checkHistory.Any())
                    {
                        PreviousPsStart = checkHistory.Min(x => x.PlanSchedule.Start);
                        PreviousPsFinish = checkHistory.Max(x => x.PlanSchedule.Finish);

                        PreviousPlanDuration = checkHistory.Sum(x => x.Plan.Days);
                        PreviousPlanCost = checkHistory.Sum(x => x.Plan.Cost);
                    }
                    else
                    {
                        if (d.Phases.Any(x => x.BaseOP.Contains(PreviousOP)))
                        {
                            var vl = d.Phases.Where(x => x.BaseOP.Contains(PreviousOP) && x.PlanSchedule.Start > Tools.DefaultDate);
                            if (vl.Any())
                            {
                                PreviousPsStart = vl.Min(x => x.PlanSchedule.Start);
                                PreviousPsFinish = vl.Max(x => x.PlanSchedule.Finish);

                                PreviousPlanDuration = vl.Sum(x => x.Plan.Days);
                                PreviousPlanCost = vl.Sum(x => x.Plan.Cost);
                            }
                        }
                    }

                    var basesops = wb.OPs == null ? new List<string>() : wb.OPs;


                    List<BizPlanActivityPhase> phases = new List<BizPlanActivityPhase>();
                    if (basesops.Count() == 1 && basesops.FirstOrDefault().Trim().Equals(""))
                    {
                        phases = d.Phases;
                    }
                    else if (basesops.Count() == 0)
                    {
                        phases = d.Phases;
                    }
                    else
                    {
                        phases = d.Phases.Where(x => basesops.ToList().Contains(x.Estimate.SaveToOP)).ToList();
                    }


                    if (Status.ToList().Any())
                    {
                        phases = phases.Where(x => Status.Contains(x.Estimate.Status)).ToList();
                    }
                    //else
                    //{
                    //    phases = phases.Where(x => x.Estimate.Status != null && !x.Estimate.Status.ToString().Trim().Equals("")).ToList();

                    //}

                    switch (wb.showdataby)
                    {
                        case "1":
                            {
                                phases = d.Phases.Where(x => WellActivity.isHaveWeeklyReport(d.WellName, d.UARigSequenceId, x.ActivityType) == true).ToList();
                                break;
                            }
                        case "2":
                            {
                                phases = d.Phases.Where(x => WellActivity.isHaveMonthlyLEReport(d.WellName, d.UARigSequenceId, x.ActivityType) == true).ToList();
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }


                    foreach (var p in phases)
                    {
                        //check roles
                        var check = WebTools.CheckAccessibleWellAndActivity(accessibleWells, d.WellName, p.ActivityType);
                        if (check)
                        {
                            var OPEstimateCurr = new List<object>();
                            var OPEstimatePrev = new List<object>();
                            var OPEstimate = new List<object>();
                            int i = 0;
                            foreach (var r in OPstate)
                            {
                                if (r == DefaultOP)//r == p.Estimate.SaveToOP && 
                                {
                                    var syncFromWellPlan = p.Estimate.isGenerateFromSync;

                                    var y = WellActivity.Get<WellActivity>(Query.And(

                                        Query.EQ("WellName", d.WellName),
                                        Query.EQ("Phases.ActivityType", p.ActivityType),
                                        Query.EQ("UARigSequenceId", d.UARigSequenceId)));
                                    if (y != null)
                                    {

                                        var isOphise =
                                            y.OPHistories.Where(
                                            x => x.Type != null && x.Type.Equals(r) && x.ActivityType.Equals(p.ActivityType));

                                        if (isOphise != null && isOphise.Count() > 0)
                                        {

                                            var yoyoy = isOphise.FirstOrDefault();

                                            OPEstimate.Add(
                                                new
                                                {
                                                    OPEstimate = r,
                                                    EstimatePeriodStart = Tools.ToUTC(yoyoy.PlanSchedule.Start),
                                                    EstimatePeriodEnd = Tools.ToUTC(yoyoy.PlanSchedule.Finish),
                                                    EstimateDays = yoyoy.Plan.Days,
                                                    EstimateCost = Tools.Div(yoyoy.Plan.Cost < 5000 ? yoyoy.Plan.Cost * 1000000 : yoyoy.Plan.Cost, 1000000)
                                                }
                                                );
                                        }
                                        else
                                        {
                                            var phadas = y.Phases.Where(x => x.ActivityType.Equals(p.ActivityType) && (x.BaseOP != null && x.BaseOP.Count() > 0) && x.BaseOP.LastOrDefault().Equals(r));

                                            if (phadas != null && phadas.Count() > 0)
                                            {
                                                var phada = phadas.LastOrDefault();
                                                OPEstimate.Add(
                                                new
                                                {
                                                    OPEstimate = r,
                                                    EstimatePeriodStart = Tools.ToUTC(phada.PlanSchedule.Start),
                                                    EstimatePeriodEnd = Tools.ToUTC(phada.PlanSchedule.Start),
                                                    EstimateDays = phada.Plan.Days,
                                                    EstimateCost = Tools.Div(phada.Plan.Cost, 1000000)
                                                }
                                                );
                                            }
                                            else
                                            {
                                                OPEstimate.Add(
                                                    new
                                                    {
                                                        OPEstimate = r,
                                                        EstimatePeriodStart = Tools.DefaultDate,
                                                        EstimatePeriodEnd = Tools.DefaultDate,
                                                        EstimateDays = 0.0,
                                                        EstimateCost = 0.0
                                                    }
                                                    );
                                            }

                                        }
                                    }
                                    else
                                    {
                                        OPEstimate.Add(
                                               new
                                               {
                                                   OPEstimate = r,
                                                   EstimatePeriodStart = Tools.DefaultDate,
                                                   EstimatePeriodEnd = Tools.DefaultDate,
                                                   EstimateDays = 0.0,
                                                   EstimateCost = 0.0
                                               }
                                               );
                                    }

                                    //if (syncFromWellPlan)
                                    //{
                                    //    OPEstimate.Add(
                                    //        new
                                    //        {
                                    //            OPEstimate = r,
                                    //            EstimatePeriodStart = Tools.ToUTC(p.PlanSchedule.Start),
                                    //            EstimatePeriodEnd = Tools.ToUTC(p.PlanSchedule.Finish),
                                    //            EstimateDays = p.Plan.Days,
                                    //            EstimateCost = Tools.Div(p.Plan.Cost, 1000000)
                                    //        }
                                    //        );
                                    //}
                                    //else
                                    //{
                                    //    OPEstimate.Add(
                                    //       new
                                    //       {
                                    //           OPEstimate = r,
                                    //           EstimatePeriodStart = Tools.DefaultDate,
                                    //           EstimatePeriodEnd = Tools.DefaultDate,
                                    //           EstimateDays = 0.0,
                                    //           EstimateCost = 0.0
                                    //       }
                                    //       );
                                    //}
                                }
                                else if (r == p.Estimate.SaveToOP && r == NextOP)
                                {
                                    p.Estimate.EstimatePeriod.Start = Tools.ToUTC(p.Estimate.EstimatePeriod.Start, true);
                                    p.Estimate.EstimatePeriod.Finish = Tools.ToUTC(p.Estimate.EstimatePeriod.Finish);
                                    p.Estimate.EventStartDate = Tools.ToUTC(p.Estimate.EventStartDate, true);
                                    p.Estimate.EventEndDate = Tools.ToUTC(p.Estimate.EventEndDate);


                                    var maturityLevel = p.Estimate.MaturityLevel.Substring(p.Estimate.MaturityLevel.Length - 1, 1);

                                    double iServices = p.Estimate.Services;
                                    double iMaterials = p.Estimate.Materials;



                                    BizPlanSummary bisCal = new BizPlanSummary(d.WellName, p.Estimate.RigName, p.ActivityType, d.Country,
                                        d.ShellShare, p.Estimate.EstimatePeriod, Convert.ToInt32(p.Estimate.MaturityLevel.Replace("Type", "")),
                                        iServices, iMaterials,
                                        p.Estimate.TroubleFreeBeforeLC.Days,
                                        d.ReferenceFactorModel,
                                        Tools.Div(p.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentDays, 100),
                                        Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100),
                                        p.Estimate.LongLeadMonthRequired, Tools.Div(p.Estimate.PercOfMaterialsLongLead, 100), baseOP: p.Estimate.SaveToOP,
                                        isGetExcRateByCurrency: true,
                                        lcf: p.Estimate.NewLearningCurveFactor
                                        );

                                    bisCal.GeneratePeriodBucket();

                                    //BizPlanSummary bisCal = new BizPlanSummary(d.WellName, d.RigName, p.ActivityType, d.Country,
                                    //d.ShellShare, p.Estimate.EstimatePeriod, Convert.ToInt32(p.Estimate.MaturityLevel.Replace("Type", "")),
                                    //p.Estimate.Services, p.Estimate.Materials,
                                    //p.Estimate.TroubleFreeBeforeLC.Days,
                                    //d.ReferenceFactorModel,
                                    //Tools.Div(p.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentDays, 100),
                                    //Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100),
                                    //p.Estimate.LongLeadMonthRequired, Tools.Div(p.Estimate.PercOfMaterialsLongLead, 100), baseOP: p.Estimate.SaveToOP, isGetExcRateByCurrency: true,
                                    //lcf: p.Estimate.NewLearningCurveFactor
                                    //);

                                    //bisCal.GeneratePeriodBucket();
                                    OPEstimate.Add(
                                        new
                                        {
                                            OPEstimate = p.Estimate.SaveToOP,
                                            EstimatePeriodStart = Tools.ToUTC(p.Estimate.EventStartDate),
                                            EstimatePeriodEnd = p.Estimate.EventStartDate.Date.AddDays(bisCal.MeanTime),
                                            //EstimatePeriodEnd = Tools.ToUTC(p.Estimate.EstimatePeriod.Finish),
                                            //EstimateDays = p.Estimate.NewMean.Days,
                                            EstimateDays = bisCal.MeanTime,
                                            EstimateCost = Tools.Div(bisCal.MeanCostMOD, 1000000)
                                            //EstimateCost = Tools.Div(p.Estimate.MeanCostMOD, 1000000)
                                        }
                                        );
                                }
                                else
                                {
                                    OPEstimate.Add(
                                        new
                                        {
                                            OPEstimate = r,
                                            EstimatePeriodStart = Tools.DefaultDate,
                                            EstimatePeriodEnd = Tools.DefaultDate,
                                            EstimateDays = 0,
                                            EstimateCost = 0.0
                                        }
                                        );
                                }
                                //if (r.Equals(p.Estimate.SaveToOP) && i == 0 || r.Equals(p.Estimate.SaveToOP) && i == 1)
                                //{
                                //    OPEstimate.Add(
                                //       new
                                //       {
                                //           OPEstimate = p.Estimate.SaveToOP,
                                //           EstimatePeriodStart = p.Estimate.EstimatePeriod.Start,
                                //           EstimatePeriodEnd = p.Estimate.EstimatePeriod.Finish,
                                //           EstimateDays = p.Estimate.CurrentMean.Days,
                                //           EstimateCost = p.Estimate.MeanUSD
                                //       }
                                //    );
                                //}
                                //else
                                //{
                                //    OPEstimate.Add(
                                //   new
                                //   {
                                //       OPEstimate = r,
                                //       EstimatePeriodStart = Tools.DefaultDate,
                                //       EstimatePeriodEnd = Tools.DefaultDate,
                                //       EstimateDays = 0,
                                //       EstimateCost = 0.0
                                //   }
                                //   );
                                //}
                                i++;
                            }

                            var isHaveWeeklyReport = WellActivity.isHaveWeeklyReport(d.WellName, d.UARigSequenceId, p.ActivityType);
                            if (isHaveWeeklyReport)
                            {

                            }
                            if (!isHaveWeeklyReport)
                            {
                                var InPlan = wb.isInPlan;
                                var isInPlan = true;
                                if (InPlan.Count > 0)
                                {
                                    if (!(d.isInPlan == InPlan.FirstOrDefault()))
                                    {
                                        isInPlan = false;
                                    }
                                }
                                if (isInPlan)
                                {
                                    final2.Add(new
                                    {
                                        _id = d._id,
                                        Status = p.Estimate.Status,
                                        Region = d.Region,
                                        OperatingUnit = d.OperatingUnit,
                                        d.UARigSequenceId,
                                        RigType = d.RigType,
                                        RigName = p.Estimate.RigName,
                                        ProjectName = d.ProjectName,
                                        WellName = d.WellName,
                                        PhaseNo = p.PhaseNo,
                                        ActivityType = p.ActivityType,
                                        AssetName = d.AssetName,
                                        NonOP = d.NonOP,
                                        WorkingInterest = d.WorkingInterest <= 1 ? d.WorkingInterest * 100 : d.WorkingInterest,
                                        FirmOrOption = d.FirmOrOption,
                                        PsDuration = d.PsSchedule != null ? d.PsSchedule.Days : 0,

                                        SaveToOP = p.Estimate.SaveToOP,

                                        PlanDuration = p.Plan != null ? p.Plan.Days : 0.0,
                                        PlanCost = Tools.Div(p.Plan != null ? p.Plan.Cost : 0.0, division),
                                        //PreviousPlanDuration = PreviousPlanDuration,
                                        //PreviousPlanCost = Tools.Div(PreviousPlanCost, division),

                                        OpsCost = Tools.Div(p.OP != null ? p.OP.Cost : 0.0, division),
                                        OpsStart = p.PhSchedule != null ? Tools.ToUTC(p.PhSchedule.Start) : Tools.DefaultDate,
                                        OpsFinish = p.PhSchedule != null ? Tools.ToUTC(p.PhSchedule.Finish) : Tools.DefaultDate,
                                        OpsDuration = p.PhSchedule != null ? p.PhSchedule.Days : 0,

                                        PhDuration = (d.Phases.Count() > 0 ? d.Phases.Sum(x => x.PhSchedule == null ? 0 : x.PhSchedule.Days) : 0),
                                        UARigDescription = String.Format("{0} - {1}", d.UARigSequenceId, d.UARigDescription == null ? "" : d.UARigDescription),
                                        PhRiskDuration = (d.Phases.Count() > 0 ? d.Phases.Where(x => x.RiskFlag == null ? false : !x.RiskFlag.Equals("")).Sum(x => x.PhSchedule == null ? 0 : x.PhSchedule.Days) : 0),
                                        PhStartForFilter = (d.Phases.Count() > 0 ? d.Phases.Min(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Start) : Tools.DefaultDate),
                                        PhFinishForFilter = (d.Phases.Count() > 0 ? d.Phases.Max(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Finish) : Tools.DefaultDate),
                                        PsStartForFilter = (d.PsSchedule == null ? Tools.DefaultDate : d.PsSchedule.Start),
                                        PsFinishForFilter = (d.PsSchedule == null ? Tools.DefaultDate : d.PsSchedule.Finish),


                                        PsStart = p.Plan != null ? p.PlanSchedule.Start : Tools.DefaultDate,
                                        PsFinish = p.Plan != null ? p.PlanSchedule.Finish : Tools.DefaultDate,
                                        //PreviousPsStart = PreviousPsStart,
                                        //PreviousPsFinish = PreviousPsFinish,

                                        PhCost = (d.Phases.Count() > 0 ? (d.Phases.Sum(e => e.OP.Cost) / division) : 0),
                                        AFEStart = (p.AFESchedule != null ? p.AFESchedule.Start : Tools.DefaultDate).ToString("dd-MMM-yy"),
                                        AFEFinish = (p.AFESchedule != null ? p.AFESchedule.Start : Tools.DefaultDate).ToString("dd-MMM-yy"),
                                        AFEDuration = p.AFE != null ? p.AFE.Days : p.AFE.Cost,
                                        AFECost = Tools.Div(p.AFE != null ? p.AFE.Cost : 0.0, division),
                                        LEStart = p.LESchedule != null ? Tools.ToUTC(p.LESchedule.Start) : Tools.DefaultDate,
                                        LEFinish = p.LESchedule != null ? Tools.ToUTC(p.LESchedule.Finish) : Tools.DefaultDate,
                                        LEDuration = p.LESchedule != null ? p.LESchedule.Days : 0,
                                        LECost = Tools.Div(p.LE != null ? p.LE.Cost : 0, division),
                                        VirtualPhase = d.VirtualPhase,
                                        ShiftFutureEventDate = d.ShiftFutureEventDate,
                                        ShellShare = d.ShellShare,
                                        OPEstimate = OPEstimate,
                                        LSInfo = lsInfo
                                        //BaseOP = GetBaseOP(d.Phases)
                                    });
                                }

                            }
                        }
                    }
                }

                #region testing purpose

                //DataHelper.Delete("_testResultBP");
                //int iiu=1;
                //foreach (var yu in final2)
                //{
                //    var y = yu.ToBsonDocument();

                //    BsonDocument d = new BsonDocument();
                //    d.Set("RigName", y.GetString("RigName"));
                //    d.Set("WellName", y.GetString("WellName"));
                //    d.Set("ActivityType", y.GetString("ActivityType"));
                //    d.Set("UARigSequenceId", y.GetString("UARigSequenceId"));

                //    var yui  = y.Get("OPEstimate");
                //    d.Set("OPEstimate", yui);
                //    d.Set("_id", iiu);

                //    DataHelper.Save("_testResultBP", d);
                //    iiu++;

                //}

                #endregion

                return final2;
            });
        }

        public List<FilterHelper> Sorting(List<FilterHelper> datas, List<Dictionary<string, string>> sorts = null)
        {
            var sort = sorts.FirstOrDefault();
            var skey = sort.FirstOrDefault().Value;
            var sby = sort.LastOrDefault().Value;

            if (sby.Equals("asc"))
            {
                #region ASC
                switch (skey)
                {
                    case "Status":
                        {
                            datas = datas.OrderBy(x => x.Status).ToList();
                            break;
                        }
                    case "RigName":
                        {
                            datas = datas.OrderBy(x => x.RigName).ToList();

                            break;
                        }
                    case "WellName":
                        {
                            datas = datas.OrderBy(x => x.WellName).ToList();

                            break;
                        }
                    case "ActivityType":
                        {
                            datas = datas.OrderBy(x => x.ActivityType).ToList();
                            break;
                        }
                    case "UARigSequenceId":
                        {
                            datas = datas.OrderBy(x => x.UARigSequenceId).ToList();
                            break;
                        }
                    case "SaveToOP":
                        {
                            datas = datas.OrderBy(x => x.SaveToOP).ToList();
                            break;
                        }



                    case "date":
                        {
                            datas = datas.OrderBy(x => x.PhSchedule.Start).ToList();
                            break;
                        }
                    case "Region":
                        {
                            datas = datas.OrderBy(x => x.Region).ToList();
                            break;
                        }
                    case "OperatingUnit":
                        {
                            datas = datas.OrderBy(x => x.OperatingUnit).ToList();
                            break;
                        }
                    case "RigType":
                        {
                            datas = datas.OrderBy(x => x.RigType).ToList();
                            break;
                        }
                    case "ProjectName":
                        {
                            datas = datas.OrderBy(x => x.ProjectName).ToList();
                            break;
                        }
                    case "AssetName":
                        {
                            datas = datas.OrderBy(x => x.AssetName).ToList();
                            break;
                        }
                    case "ShellShare":
                        {
                            datas = datas.OrderBy(x => x.AssetName).ToList();
                            break;
                        }

                    default:
                        {
                            break;
                        }
                }
                #endregion
            }
            else
            {
                #region DESC
                switch (skey)
                {
                    case "Status":
                        {
                            datas = datas.OrderByDescending(x => x.Status).ToList();
                            break;
                        }
                    case "RigName":
                        {
                            datas = datas.OrderByDescending(x => x.RigName).ToList();

                            break;
                        }
                    case "WellName":
                        {
                            datas = datas.OrderByDescending(x => x.WellName).ToList();

                            break;
                        }
                    case "ActivityType":
                        {
                            datas = datas.OrderByDescending(x => x.ActivityType).ToList();
                            break;
                        }
                    case "UARigSequenceId":
                        {
                            datas = datas.OrderByDescending(x => x.UARigSequenceId).ToList();
                            break;
                        }
                    case "SaveToOP":
                        {
                            datas = datas.OrderByDescending(x => x.SaveToOP).ToList();
                            break;
                        }



                    case "date":
                        {
                            datas = datas.OrderByDescending(x => x.PhSchedule.Start).ToList();
                            break;
                        }
                    case "Region":
                        {
                            datas = datas.OrderByDescending(x => x.Region).ToList();
                            break;
                        }
                    case "OperatingUnit":
                        {
                            datas = datas.OrderByDescending(x => x.OperatingUnit).ToList();
                            break;
                        }
                    case "RigType":
                        {
                            datas = datas.OrderByDescending(x => x.RigType).ToList();
                            break;
                        }
                    case "ProjectName":
                        {
                            datas = datas.OrderByDescending(x => x.ProjectName).ToList();
                            break;
                        }
                    case "AssetName":
                        {
                            datas = datas.OrderByDescending(x => x.AssetName).ToList();
                            break;
                        }
                    case "ShellShare":
                        {
                            datas = datas.OrderByDescending(x => x.AssetName).ToList();
                            break;
                        }

                    default:
                        {
                            break;
                        }
                }
                #endregion
            }

            return datas;
        }

        public List<BusplanOutput> SortingOPEstimate(List<BusplanOutput> datas, List<Dictionary<string, string>> sorts = null)
        {
            var sort = sorts.FirstOrDefault();
            var skey = sort.FirstOrDefault().Value;
            var sby = sort.LastOrDefault().Value;
            foreach (var t in datas)
            {
                int i = 1;
                foreach (var item in t.OPEstimate.OrderBy(x => x.GetType().GetProperty("OPEstimate").GetValue(x, null)))
                {

                    #region Extract value
                    System.Reflection.PropertyInfo opestimate = item.GetType().GetProperty("OPEstimate");
                    string Type = (String)(opestimate.GetValue(item, null));

                    System.Reflection.PropertyInfo estimatePeriodStart = item.GetType().GetProperty("EstimatePeriodStart");
                    DateTime EstimatePeriodStart = (DateTime)(estimatePeriodStart.GetValue(item, null));

                    System.Reflection.PropertyInfo estimatePeriodEnd = item.GetType().GetProperty("EstimatePeriodEnd");
                    DateTime EstimatePeriodEnd = (DateTime)(estimatePeriodEnd.GetValue(item, null));

                    System.Reflection.PropertyInfo estimateDays = item.GetType().GetProperty("EstimateDays");
                    double EstimateDays = (double)(estimateDays.GetValue(item, null));

                    System.Reflection.PropertyInfo estimateCost = item.GetType().GetProperty("EstimateCost");
                    double EstimateCost = (double)(estimateCost.GetValue(item, null));

                    if (i <= 1)
                    {
                        t.PrevOPEst.OPEstimate = Type;
                        t.PrevOPEst.EstimatePeriodStart = EstimatePeriodStart;
                        t.PrevOPEst.EstimatePeriodEnd = EstimatePeriodEnd;
                        t.PrevOPEst.EstimateDays = EstimateDays;
                        t.PrevOPEst.EstimateCost = EstimateCost;
                    }
                    else
                    {
                        t.NextOPEst.OPEstimate = Type;
                        t.NextOPEst.EstimatePeriodStart = EstimatePeriodStart;
                        t.NextOPEst.EstimatePeriodEnd = EstimatePeriodEnd;
                        t.NextOPEst.EstimateDays = EstimateDays;
                        t.NextOPEst.EstimateCost = EstimateCost;
                    }

                    i++;
                    #endregion
                }
            }

            if (sby.Equals("asc"))
            {
                #region ASC
                switch (skey)
                {
                    case "OPEstimate[0].EstimatePeriodStart":
                        {
                            datas = datas.OrderBy(x => x.PrevOPEst.EstimatePeriodStart).ToList();
                            break;
                        }
                    case "OPEstimate[0].EstimatePeriodEnd":
                        {
                            datas = datas.OrderBy(x => x.PrevOPEst.EstimatePeriodEnd).ToList();

                            break;
                        }
                    case "OPEstimate[0].EstimateDays":
                        {
                            datas = datas.OrderBy(x => x.PrevOPEst.EstimateDays).ToList();
                            break;
                        }
                    case "OPEstimate[0].EstimateCost":
                        {
                            datas = datas.OrderBy(x => x.PrevOPEst.EstimateCost).ToList();
                            break;
                        }

                    case "OPEstimate[1].EstimatePeriodStart":
                        {
                            datas = datas.OrderBy(x => x.NextOPEst.EstimatePeriodStart).ToList();
                            break;
                        }
                    case "OPEstimate[1].EstimatePeriodEnd":
                        {
                            datas = datas.OrderBy(x => x.NextOPEst.EstimatePeriodEnd).ToList();

                            break;
                        }
                    case "OPEstimate[1].EstimateDays":
                        {
                            datas = datas.OrderBy(x => x.NextOPEst.EstimateDays).ToList();
                            break;
                        }
                    case "OPEstimate[1].EstimateCost":
                        {
                            datas = datas.OrderBy(x => x.NextOPEst.EstimateCost).ToList();
                            break;
                        }

                    default:
                        {
                            break;
                        }
                }
                #endregion
            }
            else
            {
                #region DESC
                switch (skey)
                {
                    case "OPEstimate[0].EstimatePeriodStart":
                        {
                            datas = datas.OrderByDescending(x => x.PrevOPEst.EstimatePeriodStart).ToList();
                            break;
                        }
                    case "OPEstimate[0].EstimatePeriodEnd":
                        {
                            datas = datas.OrderByDescending(x => x.PrevOPEst.EstimatePeriodEnd).ToList();

                            break;
                        }
                    case "OPEstimate[0].EstimateDays":
                        {
                            datas = datas.OrderByDescending(x => x.PrevOPEst.EstimateDays).ToList();
                            break;
                        }
                    case "OPEstimate[0].EstimateCost":
                        {
                            datas = datas.OrderByDescending(x => x.PrevOPEst.EstimateCost).ToList();
                            break;
                        }

                    case "OPEstimate[1].EstimatePeriodStart":
                        {
                            datas = datas.OrderByDescending(x => x.NextOPEst.EstimatePeriodStart).ToList();
                            break;
                        }
                    case "OPEstimate[1].EstimatePeriodEnd":
                        {
                            datas = datas.OrderByDescending(x => x.NextOPEst.EstimatePeriodEnd).ToList();

                            break;
                        }
                    case "OPEstimate[1].EstimateDays":
                        {
                            datas = datas.OrderByDescending(x => x.NextOPEst.EstimateDays).ToList();
                            break;
                        }
                    case "OPEstimate[1].EstimateCost":
                        {
                            datas = datas.OrderByDescending(x => x.NextOPEst.EstimateCost).ToList();
                            break;
                        }

                    default:
                        {
                            break;
                        }
                }
                #endregion
            }

            return datas;
        }

        public List<FilterHelper> SortingOPEstimate(List<FilterHelper> datas, List<Dictionary<string, string>> sorts = null)
        {
            var sort = sorts.FirstOrDefault();
            var skey = sort.FirstOrDefault().Value;
            var sby = sort.LastOrDefault().Value;
            foreach (var t in datas)
            {
                int i = 1;
                foreach (var item in t.OPEstimates.OrderBy(x => x.OPEstimate))
                {

                    #region Extract value

                    if (i <= 1)
                    {
                        t.PrevOPEst.OPEstimate = item.OPEstimate;
                        t.PrevOPEst.EstimatePeriodStart = item.EstimatePeriodStart;
                        t.PrevOPEst.EstimatePeriodEnd = item.EstimatePeriodEnd;
                        t.PrevOPEst.EstimateDays = item.EstimateDays;
                        t.PrevOPEst.EstimateCost = item.EstimateCost;
                    }
                    else
                    {
                        t.NextOPEst.OPEstimate = item.OPEstimate;
                        t.NextOPEst.EstimatePeriodStart = item.EstimatePeriodStart;
                        t.NextOPEst.EstimatePeriodEnd = item.EstimatePeriodEnd;
                        t.NextOPEst.EstimateDays = item.EstimateDays;
                        t.NextOPEst.EstimateCost = item.EstimateCost;
                    }

                    i++;
                    #endregion
                }
            }

            if (sby.Equals("asc"))
            {
                #region ASC
                switch (skey)
                {
                    case "OPEstimate[0].EstimatePeriodStart":
                        {
                            datas = datas.OrderBy(x => x.PrevOPEst.EstimatePeriodStart).ToList();
                            break;
                        }
                    case "OPEstimate[0].EstimatePeriodEnd":
                        {
                            datas = datas.OrderBy(x => x.PrevOPEst.EstimatePeriodEnd).ToList();

                            break;
                        }
                    case "OPEstimate[0].EstimateDays":
                        {
                            datas = datas.OrderBy(x => x.PrevOPEst.EstimateDays).ToList();
                            break;
                        }
                    case "OPEstimate[0].EstimateCost":
                        {
                            datas = datas.OrderBy(x => x.PrevOPEst.EstimateCost).ToList();
                            break;
                        }

                    case "OPEstimate[1].EstimatePeriodStart":
                        {
                            datas = datas.OrderBy(x => x.NextOPEst.EstimatePeriodStart).ToList();
                            break;
                        }
                    case "OPEstimate[1].EstimatePeriodEnd":
                        {
                            datas = datas.OrderBy(x => x.NextOPEst.EstimatePeriodEnd).ToList();

                            break;
                        }
                    case "OPEstimate[1].EstimateDays":
                        {
                            datas = datas.OrderBy(x => x.NextOPEst.EstimateDays).ToList();
                            break;
                        }
                    case "OPEstimate[1].EstimateCost":
                        {
                            datas = datas.OrderBy(x => x.NextOPEst.EstimateCost).ToList();
                            break;
                        }

                    default:
                        {
                            break;
                        }
                }
                #endregion
            }
            else
            {
                #region DESC
                switch (skey)
                {
                    case "OPEstimate[0].EstimatePeriodStart":
                        {
                            datas = datas.OrderByDescending(x => x.PrevOPEst.EstimatePeriodStart).ToList();
                            break;
                        }
                    case "OPEstimate[0].EstimatePeriodEnd":
                        {
                            datas = datas.OrderByDescending(x => x.PrevOPEst.EstimatePeriodEnd).ToList();

                            break;
                        }
                    case "OPEstimate[0].EstimateDays":
                        {
                            datas = datas.OrderByDescending(x => x.PrevOPEst.EstimateDays).ToList();
                            break;
                        }
                    case "OPEstimate[0].EstimateCost":
                        {
                            datas = datas.OrderByDescending(x => x.PrevOPEst.EstimateCost).ToList();
                            break;
                        }

                    case "OPEstimate[1].EstimatePeriodStart":
                        {
                            datas = datas.OrderByDescending(x => x.NextOPEst.EstimatePeriodStart).ToList();
                            break;
                        }
                    case "OPEstimate[1].EstimatePeriodEnd":
                        {
                            datas = datas.OrderByDescending(x => x.NextOPEst.EstimatePeriodEnd).ToList();

                            break;
                        }
                    case "OPEstimate[1].EstimateDays":
                        {
                            datas = datas.OrderByDescending(x => x.NextOPEst.EstimateDays).ToList();
                            break;
                        }
                    case "OPEstimate[1].EstimateCost":
                        {
                            datas = datas.OrderByDescending(x => x.NextOPEst.EstimateCost).ToList();
                            break;
                        }

                    default:
                        {
                            break;
                        }
                }
                #endregion
            }

            return datas;
        }

        public List<FilterHelper> BuildOPEstimate(List<FilterHelper> raw)
        {
            string PreviousOP = "";
            string NextOP = "";
            string DefaultOP = getBaseOP(out PreviousOP, out NextOP);

            var OPEstimateCurr = new List<object>();
            var OPEstimatePrev = new List<object>();
            var OPEstimate = new List<object>();
            int i = 0;
            var OPstate = GetMasterOP();

            foreach (var d in raw)
            {
                foreach (var r in OPstate)
                {
                    #region OP State

                    if (r == DefaultOP)//r == p.Estimate.SaveToOP && 
                    {
                        var syncFromWellPlan = d.Estimate.isGenerateFromSync;
                        var isOphise =
                            d.OPHistories.Where(
                            x => x.Type != null && x.Type.Equals(r) && x.ActivityType.Equals(d.ActivityType));

                        if (isOphise != null && isOphise.Count() > 0)
                        {
                            var yoyoy = isOphise.FirstOrDefault();
                            d.OPEstimates.Add(
                                new OPEstimates
                                {
                                    OPEstimate = r,
                                    EstimatePeriodStart = Tools.ToUTC(yoyoy.PlanSchedule.Start),
                                    EstimatePeriodEnd = Tools.ToUTC(yoyoy.PlanSchedule.Finish),
                                    EstimateDays = yoyoy.Plan.Days,
                                    EstimateCost = Tools.Div(yoyoy.Plan.Cost < 5000 ? yoyoy.Plan.Cost * 1000000 : yoyoy.Plan.Cost, 1000000)
                                });
                        }
                        else
                        {
                            if (d.BaseOP != null && d.BaseOP.Count() > 0 && d.BaseOP.LastOrDefault().Equals(r))
                            {
                                d.OPEstimates.Add(
                                 new OPEstimates
                                 {
                                     OPEstimate = r,
                                     EstimatePeriodStart = Tools.ToUTC(d.PlanSchedule.Start),
                                     EstimatePeriodEnd = Tools.ToUTC(d.PlanSchedule.Finish),
                                     EstimateDays = d.Plan.Days,
                                     EstimateCost = Tools.Div(d.Plan.Cost, 1000000)
                                 });
                            }
                            else
                            {
                                d.OPEstimates.Add(
                                 new OPEstimates
                                 {
                                     OPEstimate = r,
                                     EstimatePeriodStart = Tools.DefaultDate,
                                     EstimatePeriodEnd = Tools.DefaultDate,
                                     EstimateDays = 0.0,
                                     EstimateCost = 0.0
                                 });
                            }

                        }
                    }
                    else if (r == d.SaveToOP && r == NextOP)
                    {
                        d.Estimate.EstimatePeriod.Start = Tools.ToUTC(d.Estimate.EstimatePeriod.Start, true);
                        d.Estimate.EstimatePeriod.Finish = Tools.ToUTC(d.Estimate.EstimatePeriod.Finish);
                        d.Estimate.EventStartDate = Tools.ToUTC(d.Estimate.EventStartDate, true);
                        d.Estimate.EventEndDate = Tools.ToUTC(d.Estimate.EventEndDate);

                        var maturityLevel = d.Estimate.MaturityLevel.Substring(d.Estimate.MaturityLevel.Length - 1, 1);

                        double iServices = d.Estimate.Services;
                        double iMaterials = d.Estimate.Materials;

                        //BizPlanSummary bisCal = new BizPlanSummary(d.WellName, d.Estimate.RigName, d.ActivityType, d.Country, // == null ? "" : d.Country,
                        BizPlanSummary bisCal = new BizPlanSummary(d.WellName, d.Estimate.RigName, d.ActivityType, d.Country == null ? "" : d.Country,
                            d.ShellShare, d.Estimate.EstimatePeriod, Convert.ToInt32(d.Estimate.MaturityLevel.Replace("Type", "")),
                            iServices, iMaterials,
                            d.Estimate.TroubleFreeBeforeLC.Days,
                            d.ReferenceFactorModel,
                            Tools.Div(d.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(d.Estimate.NewTECOPTime.PercentDays, 100),
                            Tools.Div(d.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(d.Estimate.NewTECOPTime.PercentCost, 100),
                            d.Estimate.LongLeadMonthRequired, Tools.Div(d.Estimate.PercOfMaterialsLongLead, 100), baseOP: d.Estimate.SaveToOP,
                            isGetExcRateByCurrency: true,
                            lcf: d.Estimate.NewLearningCurveFactor
                            );

                        bisCal.GeneratePeriodBucket();

                        d.OPEstimates.Add(
                                new OPEstimates
                                {
                                    OPEstimate = d.Estimate.SaveToOP,
                                    EstimatePeriodStart = Tools.ToUTC(d.Estimate.EventStartDate),
                                    EstimatePeriodEnd = d.Estimate.EventStartDate.Date.AddDays(bisCal.MeanTime),
                                    EstimateDays = bisCal.MeanTime,
                                    EstimateCost = Tools.Div(bisCal.MeanCostMOD, 1000000)
                                }
                            );
                    }
                    else
                    {
                        d.OPEstimates.Add(
                                new OPEstimates
                                {
                                    OPEstimate = r,
                                    EstimatePeriodStart = Tools.DefaultDate,
                                    EstimatePeriodEnd = Tools.DefaultDate,
                                    EstimateDays = 0,
                                    EstimateCost = 0.0
                                }
                            );
                    }

                    i++;

                    #endregion
                }
            }
            return raw;
        }


        public JsonResult ExportDetailServerPage(WaterfallBase wb)
        {
            string PreviousOP = "";
            string NextOP = "";
            string DefaultOP = getBaseOP(out PreviousOP, out NextOP);
            var accessibleWells = WebTools.GetWellActivities();
            DateTime notw = DateTime.Now; // ---------------------------------------------------------------------------------------
            var lsInfo = new PIPAnalysisController().GetLatestLsDate();
            var division = 1000000.0;
            wb.PeriodBase = "By Last Sequence";

            FilterHelper fh = new FilterHelper();
            int coutdata = 0;

            var raw = fh.GetActivitiesForBizPlanWithServerPage(wb, out coutdata, 0, 0, WebTools.LoginUser.Email);
            FilterHelper fithel = new FilterHelper();
            raw = fithel.FilterDatahaveWeekly(raw);
            foreach (var d in raw)
                d.UARigDescription = String.Format("{0} - {1}", d.UARigSequenceId, d.UARigDescription == null ? "" : d.UARigDescription);
            var lastDateExecutedLS = WellActivity.GetLastExecuteDateLS();
            var logLast = DataHelper.Populate("LogLatestLS", Query.And(Query.EQ("Executed_At", Tools.ToUTC(lastDateExecutedLS))));
            int startRow = 4; int IndexSheet = 1; int SetFormat = 3;
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "Template_Bisplan_Cals.xlsx");
            Workbook wbook = new Workbook(xlst);

            var Messages = "";
            var RFMChecking = new List<object>();
            var ids = raw.Select(x => x._id).Distinct().ToList<int>();
            var  bpsinIDs = BizPlanActivity.Populate<BizPlanActivity>(Query.In("_id", new BsonArray(ids)));
            try
            {
                if (raw != null && raw.Count > 0)
                {
                    //eky's note : function to generate List<ExcelBusplan> is moved to a function GenerateExportDetailData
                    //because it will be used too in checker batch to check the MOD diff between BPIT and Export Detail
                    List<Excelbusplan> edetail = GenerateExportDetailData(raw, PreviousOP, accessibleWells, wb, bpsinIDs);

                    Worksheet ws = wbook.Worksheets[0];
                    Cells cells = ws.Cells;
                    cells.InsertRows(startRow, edetail.Count);
                    foreach (var r in edetail)
                    {
                        #region filter busplan have no rfm

                        var checkingBisplan = raw.FirstOrDefault(x => x.WellName == r.WellName && x.UARigSequenceId == r.RigSeqId && x.ActivityType == r.ActivityType);
                        if (checkingBisplan != null)
                        {
                            if (String.IsNullOrEmpty(checkingBisplan.ReferenceFactorModel) || checkingBisplan.ReferenceFactorModel.ToLower() == "default")
                            {
                                var t = new
                                {
                                    WellName = r.WellName,
                                    UARigSequenceId = r.RigSeqId,
                                    ActivityType = r.ActivityType,
                                    r.RigName,
                                    Project = r.ProjectName,
                                    r.Country,
                                    ReferenceFactorModel = checkingBisplan.ReferenceFactorModel

                                };
                                RFMChecking.Add(t);
                            }
                        }
                        //        qsWellPlan.Add(Query.EQ("WellName", WellName));
                        //qsWellPlan.Add(Query.EQ("Phases.ActivityType", ActivityType));
                        //qsWellPlan.Add(Query.EQ("Phases.PhaseNo", PhaseNo));
                        //qsWellPlan.Add(Query.EQ("UARigSequenceId", SequenceId));
                        #endregion
                        var dataAnyLS = logLast.Where(x => x.GetString("Well_Name") == r.WellName && x.GetString("Activity_Type") == r.ActivityType && x.GetString("Rig_Name") == r.RigName);

                        if (String.IsNullOrEmpty(wb.inlastuploadlsBoth))
                        {
                            if (wb.inlastuploadls == true)
                            {
                                if (dataAnyLS.Any())
                                    //r.latestLS = dataAnyLS.FirstOrDefault().GetDateTime("Start_Date").ToString("MM/dd/yyyy");
                                    r.latestLS = "Yes";
                            }
                            else if (wb.inlastuploadls == false)
                            {
                                var checkAnyLS = logLast.Any(x =>
                                      x.GetString("Well_Name") != r.WellName &&
                                      x.GetString("Activity_Type") != r.ActivityType &&
                                      x.GetString("Rig_Name") != r.RigName
                                 );
                                if (checkAnyLS || !logLast.Any())
                                    r.latestLS = "No";
                            }
                        }
                        else  
                        {
                            if (dataAnyLS.Any())
                                //r.latestLS = dataAnyLS.FirstOrDefault().GetDateTime("Start_Date").ToString("MM/dd/yyyy");
                                r.latestLS = "Yes";
                            else
                            {
                                r.latestLS = "No";
                            }
                        }
                        cells["A" + startRow.ToString()].PutValue(r.Status);
                        cells["B" + startRow.ToString()].PutValue(r.LineOfBusiness);
                        cells["C" + startRow.ToString()].PutValue(r.RigType);
                        cells["D" + startRow.ToString()].PutValue(r.Region);
                        cells["E" + startRow.ToString()].PutValue(r.Country == null ? "" : r.Country);
                        cells["F" + startRow.ToString()].PutValue(r.OperatingUnit);
                        cells["G" + startRow.ToString()].PutValue(r.PerformanceUnit);
                        cells["H" + startRow.ToString()].PutValue(r.AssetName);
                        cells["I" + startRow.ToString()].PutValue(r.ProjectName);
                        cells["J" + startRow.ToString()].PutValue(r.RigName);
                        cells["K" + startRow.ToString()].PutValue(r.WellName);
                        cells["L" + startRow.ToString()].PutValue(r.ActivityType);
                        cells["M" + startRow.ToString()].PutValue(r.RigSeqId);
                        cells["N" + startRow.ToString()].PutValue(r.Currency == null ? "" : r.Currency);
                        cells["O" + startRow.ToString()].PutValue(Tools.Div(r.ShellShare, 100));
                        cells["P" + startRow.ToString()].PutValue(r.DataInputBy);
                        cells["Q" + startRow.ToString()].PutValue(r.SaveToOP == null ? "" : r.SaveToOP);
                        Style styleStart = cells["W" + startRow.ToString()].GetStyle();
                        styleStart.Custom = "m/d/yyyy";
                        cells["R" + startRow.ToString()].PutValue(r.LastSubmitted);

                        cells["S" + startRow.ToString()].PutValue(r.InPlan ? "Yes" : "No");
                        //ls ingo lsInfo
                        cells["T" + startRow.ToString()].PutValue(r.latestLS);
                        //ref model
                        cells["U" + startRow.ToString()].PutValue(r.RFM);
                        cells["V" + startRow.ToString()].PutValue(r.FundingType);

                        //Uni
                        cells["W" + startRow.ToString()].PutValue(r.Event.Start);
                        cells["X" + startRow.ToString()].PutValue(r.Event.Finish);
                        cells["Y" + startRow.ToString()].PutValue(Math.Round(r.EventDays, 2));

                        cells["W" + startRow.ToString()].SetStyle(styleStart);
                        cells["X" + startRow.ToString()].SetStyle(styleStart);
                        cells["R" + startRow.ToString()].SetStyle(styleStart);

                        cells["Z" + startRow.ToString()].PutValue(Math.Round(r.RigRate));
                        cells["AA" + startRow.ToString()].PutValue(r.Services);
                        cells["AB" + startRow.ToString()].PutValue(r.Materials);
                        cells["AC" + startRow.ToString()].PutValue(r.isMaterialLLSetManually ? "Yes" : "No");
                        cells["AD" + startRow.ToString()].PutValue(r.PercOfMaterialsLongLead);
                        cells["AE" + startRow.ToString()].PutValue(r.LLRequiredMonth);
                        cells["AF" + startRow.ToString()].PutValue(r.LLCalc);
                        cells["AG" + startRow.ToString()].PutValue(r.SpreadRate);
                        cells["AH" + startRow.ToString()].PutValue(r.BurnRate);
                        cells["AI" + startRow.ToString()].PutValue(r.SpreadRateTotal);
                        //OP16 Days

                        cells["AJ" + startRow.ToString()].PutValue(r.IsUsingLCFfromRFM ? "Yes" : "No");
                        cells["AK" + startRow.ToString()].PutValue(r.TroubleFree.Days);
                        cells["AL" + startRow.ToString()].PutValue(r.UseTAApproved ? "Yes" : "No");
                        cells["AM" + startRow.ToString()].PutValue(r.LCFParameter);
                        cells["AN" + startRow.ToString()].PutValue(Tools.Div(r.NPT.PercentTime, 100));
                        cells["AO" + startRow.ToString()].PutValue(Tools.Div(r.TECOP.PercentTime, 100));
                        cells["AP" + startRow.ToString()].PutValue(r.NPT.Days);
                        cells["AQ" + startRow.ToString()].PutValue(r.LCF.Days);
                        cells["AR" + startRow.ToString()].PutValue(Math.Round(r.Base.Days, 2));
                        cells["AS" + startRow.ToString()].PutValue(Math.Round(r.Base.Days - r.LCF.Days, 2));
                        cells["AT" + startRow.ToString()].PutValue(Math.Round(r.TECOP.Days, 2));
                        cells["AU" + startRow.ToString()].PutValue(Math.Round(r.Mean.Days, 2));
                        //Cost Estimate p16    B



                        cells["AV" + startRow.ToString()].PutValue(r.TroubleFreeCost);
                        cells["AW" + startRow.ToString()].PutValue(r.NPT.PercentCost);
                        cells["AX" + startRow.ToString()].PutValue(r.TECOP.PercentCost);
                        cells["AY" + startRow.ToString()].PutValue(r.NPT.Cost);
                        cells["AZ" + startRow.ToString()].PutValue(r.LCF.Cost);
                        cells["BA" + startRow.ToString()].PutValue(r.Base.Cost);
                        cells["BB" + startRow.ToString()].PutValue(r.BaseCalc.Cost);
                        cells["BC" + startRow.ToString()].PutValue(r.TECOP.Cost);
                        //cells["BA" + startRow.ToString()].PutValue(r.MeanCost);
                        cells["BD" + startRow.ToString()].PutValue(r.MeanCostEDM);
                        //MeanCostEDM
                        //CONVERSIBN TO USD
                        cells["BE" + startRow.ToString()].PutValue(r.TroubleFreeCostUSD);
                        cells["BF" + startRow.ToString()].PutValue(r.NptCostUSD);
                        cells["BG" + startRow.ToString()].PutValue(r.TecopCostUSD);
                        //cells["BE" + startRow.ToString()].PutValue(r.MeanCostUSD);
                        cells["BH" + startRow.ToString()].PutValue(r.MeanCostEDM);
                        //level   B
                        cells["BI" + startRow.ToString()].PutValue(r.MaturityLevel);
                        //well value driver Days
                        cells["BJ" + startRow.ToString()].PutValue(r.WellValueDriver);
                        cells["BK" + startRow.ToString()].PutValue(r.MeanWVA.Days); //EDM DAYS
                        cells["BL" + startRow.ToString()].PutValue(r.WVATotalDays.TQValueDriver.Days); //TQ For Value Driver
                        cells["BM" + startRow.ToString()].PutValue(r.WVATotalDays.TQGap.Days); // TQ GAP ok
                        cells["BN" + startRow.ToString()].PutValue(r.WVATotalDays.TQGap.Cost); //TQ Gap Cost (calc)  
                        cells["BO" + startRow.ToString()].PutValue(r.WVATotalDays.TQValueDriver.Cost); //TQ Total Cost (calc) (USD)
                        cells["BP" + startRow.ToString()].PutValue(r.WVATotalDays.BICValueDriver.Days);
                        cells["BQ" + startRow.ToString()].PutValue(r.WVATotalDays.BICGap.Days); // BIC Gap
                        cells["BR" + startRow.ToString()].PutValue(r.WVATotalDays.BICGap.Cost);
                        cells["BS" + startRow.ToString()].PutValue(r.WVATotalDays.BICValueDriver.Cost);
                        //Dry Hole Days
                        cells["BT" + startRow.ToString()].PutValue(r.DryHoleDays);
                        cells["BU" + startRow.ToString()].PutValue(r.WVADryHoleDays.TQValueDriver.Days); // TQ For Value Driver
                        cells["BV" + startRow.ToString()].PutValue(r.WVADryHoleDays.TQGap.Days); //TQ GAP
                        cells["BW" + startRow.ToString()].PutValue(r.WVADryHoleDays.BICValueDriver.Days);
                        cells["BX" + startRow.ToString()].PutValue(r.WVADryHoleDays.BICGap.Cost);
                        //Well value Cost
                        cells["BY" + startRow.ToString()].PutValue(r.MeanWVA.Cost); // EDM COST
                        cells["BZ" + startRow.ToString()].PutValue(r.WVATotalCost.TQValueDriver.Cost);//DRIVER
                        cells["CA" + startRow.ToString()].PutValue(r.WVATotalCost.TQGap.Cost); // GAP
                        //bic
                        cells["CB" + startRow.ToString()].PutValue(r.WVATotalCost.BICValueDriver.Cost);
                        cells["CC" + startRow.ToString()].PutValue(r.WVATotalCost.BICGap.Cost);

                        //Perfom
                        cells["CD" + startRow.ToString()].PutValue(r.PerformanceScore);
                        cells["CE" + startRow.ToString()].PutValue(r.WaterDepth);
                        cells["CF" + startRow.ToString()].PutValue(r.WellDepth);
                        cells["CG" + startRow.ToString()].PutValue(r.NumberOfCasings);
                        cells["CH" + startRow.ToString()].PutValue(r.MechanicalRiskIndex);
                        //Escalation
                        cells["CI" + startRow.ToString()].PutValue(r.EscalationRig);
                        cells["CJ" + startRow.ToString()].PutValue(r.EscalationService);
                        cells["CK" + startRow.ToString()].PutValue(r.EscalationMaterials);
                        cells["CL" + startRow.ToString()].PutValue(r.EscalationCost);
                        cells["CM" + startRow.ToString()].PutValue(r.CSOCost);
                        cells["CN" + startRow.ToString()].PutValue(r.InflationCost);
                        //EVENT SUMMARY                      PutValue(
                        cells["CO" + startRow.ToString()].PutValue(r.MeanCostEDM);
                        cells["CP" + startRow.ToString()].PutValue(r.MeanCostRealTerm);
                        cells["CQ" + startRow.ToString()].PutValue(r.MeanCostMOD);
                        cells["CR" + startRow.ToString()].PutValue(r.ShellShareCost);

                        ////Text Align Right
                        cells[SetFormat, 14].SetStyle(new Style() { Number = 10 });
                        cells["X" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["Y" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["Z" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AA" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AB" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AD" + SetFormat].SetStyle(new Style() { Number = 10 });
                        cells["AE" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AF" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AG" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AH" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AI" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["AK" + SetFormat].SetStyle(new Style() { Number = 5 });
                        cells["AM" + SetFormat].SetStyle(new Style() { Custom = "0.00000" });
                        cells["AN" + SetFormat].SetStyle(new Style() { Number = 10 });
                        cells["AO" + SetFormat].SetStyle(new Style() { Number = 10 });
                        cells["AP" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["AQ" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["AR" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AS" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["AT" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["AU" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AV" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AW" + SetFormat].SetStyle(new Style() { Number = 10 });
                        cells["AX" + SetFormat].SetStyle(new Style() { Number = 10 });
                        cells["AY" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AZ" + SetFormat].SetStyle(new Style() { Number = 4 });

                        cells["BA" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BB" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BC" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BD" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BE" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BF" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BG" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BH" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BI" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["BJ" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["BK" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["BL" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["BM" + SetFormat].SetStyle(new Style() { Number = 4 });

                        cells["BN" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BO" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BP" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["BQ" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BR" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BS" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BT" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BU" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["BV" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["BW" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["BX" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BY" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BZ" + SetFormat].SetStyle(new Style() { Number = 2 });

                        cells["CA" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CB" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["CC" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CD" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["CE" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CF" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CG" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CH" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CI" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CJ" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CK" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CL" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CM" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CN" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CO" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CP" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CQ" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CR" + SetFormat].SetStyle(new Style() { Number = 4 });
                        startRow++; SetFormat++;
                    }
                    wbook.Worksheets[0].AutoFitColumns();
                }
            }
            //catch (InvalidCastException e)
            catch (InvalidCastException e)
            {
                var cda = Messages;
                throw;
            }


            var newFileNameSingle = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                     String.Format("Business Plan Detail-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("yyyy-MM-dd-hhmmss")));//Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss")));

            //string returnName = String.Format("Business Plan Detail-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss"));
            string returnName = String.Format("Business Plan Detail-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("yyyy-MM-dd-hhmmss"));

            wbook.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            return Json(new { Success = true, Path = returnName, RFMChecking }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetBizPlanActivityServerPage(WaterfallBase wb, List<string> Status, int take = 10, int skip = 0, List<Dictionary<string, string>> sorts = null)
        {
            //ResultInfo ri = new ResultInfo();
            //try
            //{
            return MvcResultInfo.Execute(null, document =>
            {
                DateTime notw = DateTime.Now; // ---------------------------------------------------------------------------------------
                var lsInfo = new PIPAnalysisController().GetLatestLsDate();
                //string PreviousOP = "";
                //string NextOP = "";
                //string DefaultOP = getBaseOP(out PreviousOP, out NextOP);
                if (Status == null)
                    Status = new List<string>();

                var division = 1000000.0;
                //var now = Tools.ToUTC(DateTime.Now);
                //var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
                //var email = WebTools.LoginUser.Email;
                wb.PeriodBase = "By Last Sequence";

                FilterHelper fh = new FilterHelper();
                int coutdata = 0;
                var raw = fh.GetActivitiesForBizPlanWithServerPage(wb, out coutdata, take, skip, WebTools.LoginUser.Email, serversorts: sorts);

                foreach (var d in raw)
                {
                    d.UARigDescription = String.Format("{0} - {1}", d.UARigSequenceId, d.UARigDescription == null ? "" : d.UARigDescription);
                }

                //var accessibleWells = WebTools.GetWellActivities();
                //var final2 = new List<object>();
                //var OPstate = GetMasterOP();
                List<BusplanOutput> bOutput = new List<BusplanOutput>();
                double PhRiskDuration = 0;
                var PhStartForFilter = (raw == null || raw.Count() <= 0) ? Tools.DefaultDate : raw.Min(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Start);
                var PhFinishForFilter = (raw == null || raw.Count() <= 0) ? Tools.DefaultDate : raw.Max(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Finish);
                var PsStartForFilter = (raw == null || raw.Count() <= 0) ? Tools.DefaultDate : raw.Min(e => e.PsSchedule == null ? Tools.DefaultDate : e.PsSchedule.Start);
                var PsFinishForFilter = (raw == null || raw.Count() <= 0) ? Tools.DefaultDate : raw.Max(e => e.PsSchedule == null ? Tools.DefaultDate : e.PsSchedule.Start);

                FilterHelper fithel = new FilterHelper();

                //var datasblmwr = raw.Count();

                raw = fithel.FilterDatahaveWeekly(raw);

                //var datasesuda = raw.Count();
                //var selisih = datasblmwr - datasesuda;

                //var sec1 = (DateTime.Now - notw).Milliseconds;
                // --------------------------------------------------------------------------------------------------------------
                //DateTime notw3 = DateTime.Now;
                List<FilterHelper> resultsClean = new List<FilterHelper>();
                resultsClean = BuildOPEstimate(raw);

                //var sec2 = (DateTime.Now - notw3).Milliseconds;
                // --------------------------------------------------------------------------------------------------------------

                DateTime notw4 = DateTime.Now;

                foreach (var y in resultsClean)
                {
                    if (y.OPEstimates.Count() > 1)
                    {
                        y.PrevOPEst.OPEstimate = y.OPEstimates.OrderBy(x => x.OPEstimate).FirstOrDefault().OPEstimate;
                        y.PrevOPEst.EstimateCost = y.OPEstimates.OrderBy(x => x.OPEstimate).FirstOrDefault().EstimateCost;
                        y.PrevOPEst.EstimateDays = y.OPEstimates.OrderBy(x => x.OPEstimate).FirstOrDefault().EstimateDays;
                        y.PrevOPEst.EstimatePeriodEnd = y.OPEstimates.OrderBy(x => x.OPEstimate).FirstOrDefault().EstimatePeriodEnd;
                        y.PrevOPEst.EstimatePeriodStart = y.OPEstimates.OrderBy(x => x.OPEstimate).FirstOrDefault().EstimatePeriodStart;

                        y.NextOPEst.OPEstimate = y.OPEstimates.OrderByDescending(x => x.OPEstimate).FirstOrDefault().OPEstimate;
                        y.NextOPEst.EstimateCost = y.OPEstimates.OrderByDescending(x => x.OPEstimate).FirstOrDefault().EstimateCost;
                        y.NextOPEst.EstimateDays = y.OPEstimates.OrderByDescending(x => x.OPEstimate).FirstOrDefault().EstimateDays;
                        y.NextOPEst.EstimatePeriodEnd = y.OPEstimates.OrderByDescending(x => x.OPEstimate).FirstOrDefault().EstimatePeriodEnd;
                        y.NextOPEst.EstimatePeriodStart = y.OPEstimates.OrderByDescending(x => x.OPEstimate).FirstOrDefault().EstimatePeriodStart;
                    }
                    else
                    {
                        y.PrevOPEst.OPEstimate = y.OPEstimates.FirstOrDefault().OPEstimate;
                        y.PrevOPEst.EstimateCost = y.OPEstimates.FirstOrDefault().EstimateCost;
                        y.PrevOPEst.EstimateDays = y.OPEstimates.FirstOrDefault().EstimateDays;
                        y.PrevOPEst.EstimatePeriodEnd = y.OPEstimates.FirstOrDefault().EstimatePeriodEnd;
                        y.PrevOPEst.EstimatePeriodStart = y.OPEstimates.FirstOrDefault().EstimatePeriodStart;
                    }

                }

                if (sorts != null)
                {
                    //resultsClean = Sorting(resultsClean, sorts);
                    //resultsClean = SortingOPEstimate(resultsClean, sorts);
                }

                foreach (var d in resultsClean)//.Skip(skip).Take(take))
                {
                    #region out object
                    var bixout = new BusplanOutput();
                    bixout._id = d._id;
                    bixout.Status = d.Status;
                    bixout.Region = d.Region;
                    bixout.UARigSequenceId = d.UARigSequenceId;
                    bixout.RigType = d.RigType;
                    bixout.RigName = d.RigName;
                    bixout.ProjectName = d.ProjectName;
                    bixout.WellName = d.WellName;
                    bixout.PhaseNo = d.PhaseNo;
                    bixout.ActivityType = d.ActivityType;
                    bixout.AssetName = d.AssetName;
                    bixout.NonOP = "";
                    bixout.WorkingInterest = d.WorkingInterest <= 1 ? d.WorkingInterest * 100 : d.WorkingInterest;
                    bixout.FirmOrOption = d.FirmOrOption;
                    bixout.PsDuration = d.PsSchedule != null ? d.PsSchedule.Days : 0;
                    bixout.SaveToOP = d.SaveToOP;
                    bixout.PlanDuration = d.Plan == null ? 0 : d.Plan.Days;
                    bixout.PlanCost = d.Plan == null ? 0 : d.Plan.Cost;
                    bixout.OpsCost = Tools.Div(d.OP != null ? d.OP.Cost : 0.0, division);
                    bixout.OpsStart = d.PhSchedule != null ? Tools.ToUTC(d.PhSchedule.Start) : Tools.DefaultDate;
                    bixout.OpsFinish = d.PhSchedule != null ? Tools.ToUTC(d.PhSchedule.Finish) : Tools.DefaultDate;
                    bixout.OpsDuration = d.PhSchedule != null ? d.PhSchedule.Days : 0;
                    bixout.PhDuration = 0;
                    bixout.UARigDescription = d.UARigDescription;
                    if (d.RiskFlag == null ? false : !d.RiskFlag.Equals(""))
                    {
                        PhRiskDuration = d.PhSchedule == null ? 0 : d.PhSchedule.Days;
                        PhRiskDuration += PhRiskDuration;
                    }
                    bixout.PhRiskDuration = 0;
                    bixout.PhStartForFilter = PhStartForFilter;
                    bixout.PhFinishForFilter = PhFinishForFilter;
                    bixout.PsStartForFilter = PsStartForFilter;
                    bixout.PsFinishForFilter = PsFinishForFilter;
                    bixout.PsStart = d.Plan != null ? d.PlanSchedule.Start : Tools.DefaultDate;
                    bixout.PsFinish = d.Plan != null ? d.PlanSchedule.Finish : Tools.DefaultDate;

                    bixout.VirtualPhase = d.VirtualPhase;
                    bixout.ShiftFutureEventDate = d.ShiftFutureEventDate;
                    bixout.ShellShare = d.ShellShare;
                    bixout.OPEstimate = new List<object>();
                    bixout.OPEstimate.Add(new
                    {
                        OPEstimate = d.PrevOPEst.OPEstimate,
                        EstimatePeriodStart = d.PrevOPEst.EstimatePeriodStart,
                        EstimatePeriodEnd = d.PrevOPEst.EstimatePeriodEnd,
                        EstimateDays = d.PrevOPEst.EstimateDays,
                        EstimateCost = d.PrevOPEst.EstimateCost
                    });
                    bixout.OPEstimate.Add(new
                    {
                        OPEstimate = d.NextOPEst.OPEstimate,
                        EstimatePeriodStart = d.NextOPEst.EstimatePeriodStart,
                        EstimatePeriodEnd = d.NextOPEst.EstimatePeriodEnd,
                        EstimateDays = d.NextOPEst.EstimateDays,
                        EstimateCost = d.NextOPEst.EstimateCost
                    });
                    bixout.LSInfo = lsInfo;
                    bOutput.Add(bixout);

                    #endregion
                }

                var sec3 = (DateTime.Now - notw4).Milliseconds;

                return new { Data = bOutput, Total = coutdata };
                //}
                //catch (InvalidCastException e)
                //{

                //    throw;
                //}
                //return MvcTools.ToJsonResult(ri);
            });
        }

        public Int32 GetBizPlanActivityTotalData(WaterfallBase wb, List<string> Status)
        {
            Int32 TotalData = 0;
            string PreviousOP = "";
            string NextOP = "";
            string DefaultOP = getBaseOP(out PreviousOP, out NextOP);
            if (Status == null)
            {
                Status = new List<string>();
            }
            var email = WebTools.LoginUser.Email;
            wb.PeriodBase = "By Last Sequence";
            var raw = wb.GetActivitiesForBizPlan(email);
            var accessibleWells = WebTools.GetWellActivities();
            var OPstate = GetMasterOP();

            foreach (var d in raw)
            {
                var basesops = wb.OPs == null ? new List<string>() : wb.OPs;

                List<BizPlanActivityPhase> phases = new List<BizPlanActivityPhase>();
                if (basesops.Count() == 1 && basesops.FirstOrDefault().Trim().Equals(""))
                {
                    phases = d.Phases;
                }
                else if (basesops.Count() == 0)
                {
                    phases = d.Phases;
                }
                else
                {
                    phases = d.Phases.Where(x => basesops.ToList().Contains(x.Estimate.SaveToOP)).ToList();
                }

                if (Status.ToList().Any())
                {
                    phases = phases.Where(x => Status.Contains(x.Estimate.Status)).ToList();
                }
                switch (wb.showdataby)
                {
                    case "1":
                        {
                            phases = d.Phases.Where(x => WellActivity.isHaveWeeklyReport(d.WellName, d.UARigSequenceId, x.ActivityType) == true).ToList();
                            break;
                        }
                    case "2":
                        {
                            phases = d.Phases.Where(x => WellActivity.isHaveMonthlyLEReport(d.WellName, d.UARigSequenceId, x.ActivityType) == true).ToList();
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }


                foreach (var p in phases)
                {
                    //check roles
                    var check = WebTools.CheckAccessibleWellAndActivity(accessibleWells, d.WellName, p.ActivityType);
                    if (check)
                    {
                        var isHaveWeeklyReport = WellActivity.isHaveWeeklyReport(d.WellName, d.UARigSequenceId, p.ActivityType);
                        if (isHaveWeeklyReport)
                        {

                        }
                        if (!isHaveWeeklyReport)
                        {
                            TotalData++;
                        }
                    }
                }
            }


            return TotalData;
        }
        public JsonResult GetBizPlanActivityComplete(WaterfallBase wb, List<string> status)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                if (status == null)
                    status = new List<string>();
                var division = 1000000.0;
                var now = Tools.ToUTC(DateTime.Now);
                var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
                var email = WebTools.LoginUser.Email;
                var raw = wb.GetActivitiesForBizPlanCatalog(email);
                var accessibleWells = WebTools.GetWellActivities();

                string previousOP = "";
                string nextOP = "";
                var final2 = new List<object>();


                foreach (var d in raw)
                {
                    var PreviousPsStart = Tools.DefaultDate;
                    var PreviousPsFinish = Tools.DefaultDate;
                    var PsStart = Tools.DefaultDate;
                    var PsFinish = Tools.DefaultDate;

                    var PlanDuration = 0.0;
                    var PlanCost = 0.0;
                    var PreviousPlanDuration = 0.0;
                    var PreviousPlanCost = 0.0;

                    var checkHistory = d.OPHistories.Where(x => null != x.Type && x.Type.Equals(previousOP) && x.PlanSchedule.Start > Tools.DefaultDate && x.PlanSchedule.Finish > Tools.DefaultDate);
                    if (checkHistory.Any())
                    {
                        PreviousPsStart = checkHistory.Min(x => x.PlanSchedule.Start);
                        PreviousPsFinish = checkHistory.Max(x => x.PlanSchedule.Finish);

                        PreviousPlanDuration = checkHistory.Sum(x => x.Plan.Days);
                        PreviousPlanCost = checkHistory.Sum(x => x.Plan.Cost);
                    }
                    else
                    {
                        if (d.Phases.Any(x => x.BaseOP.Contains(previousOP)))
                        {
                            var vl = d.Phases.Where(x => x.BaseOP.Contains(previousOP) && x.PlanSchedule.Start > Tools.DefaultDate);
                            if (vl.Any())
                            {
                                PreviousPsStart = vl.Min(x => x.PlanSchedule.Start);
                                PreviousPsFinish = vl.Max(x => x.PlanSchedule.Finish);

                                PreviousPlanDuration = vl.Sum(x => x.Plan.Days);
                                PreviousPlanCost = vl.Sum(x => x.Plan.Cost);
                            }
                        }
                    }

                    var basesops = wb.OPs == null ? new List<string>() : wb.OPs;

                    List<BizPlanActivityPhase> phases = new List<BizPlanActivityPhase>();
                    if (basesops.Count() == 1 && basesops.FirstOrDefault().Trim().Equals(""))
                    {
                        phases = d.Phases;
                    }
                    else if (basesops.Count() == 0)
                    {
                        phases = d.Phases;
                    }
                    else
                    {
                        phases = d.Phases.Where(x => basesops.ToList().Contains(x.Estimate.SaveToOP)).ToList();
                    }

                    if (status.ToList().Any())
                    {
                        phases = phases.Where(x => status.Contains(x.Estimate.Status)).ToList();
                    }
                    else
                    {
                        phases = phases.Where(x => x.Estimate.Status != null && !x.Estimate.Status.ToString().Trim().Equals("")).ToList();

                    }
                    if (phases.Any())
                    {
                        foreach (var p in phases)
                        {
                            //check roles

                            var check = WebTools.CheckAccessibleWellAndActivity(accessibleWells, d.WellName, p.ActivityType);
                            if (check)
                            {
                                final2.Add(new
                                {
                                    _id = d._id,
                                    Region = d.Region,
                                    OperatingUnit = d.OperatingUnit,
                                    d.UARigSequenceId,
                                    RigType = d.RigType,
                                    RigName = d.RigName,
                                    ProjectName = d.ProjectName,
                                    WellName = d.WellName,
                                    PhaseNo = p.PhaseNo,
                                    ActivityType = p.ActivityType,
                                    AssetName = d.AssetName,
                                    NonOP = d.NonOP,
                                    WorkingInterest = d.WorkingInterest <= 1 ? d.WorkingInterest * 100 : d.WorkingInterest,
                                    FirmOrOption = d.FirmOrOption,
                                    PsDuration = d.PsSchedule != null ? d.PsSchedule.Days : 0,

                                    SaveToOP = p.Estimate.SaveToOP,

                                    PlanDuration = p.Plan != null ? p.Plan.Days : 0.0,
                                    PlanCost = Tools.Div(p.Plan != null ? p.Plan.Cost : 0.0, division),
                                    //PreviousPlanDuration = PreviousPlanDuration,
                                    //PreviousPlanCost = Tools.Div(PreviousPlanCost, division),

                                    OpsCost = Tools.Div(p.OP != null ? p.OP.Cost : 0.0, division),
                                    OpsStart = p.PhSchedule != null ? p.PhSchedule.Start : Tools.DefaultDate,
                                    OpsFinish = p.PhSchedule != null ? p.PhSchedule.Finish : Tools.DefaultDate,
                                    OpsDuration = p.PhSchedule != null ? p.PhSchedule.Days : 0,

                                    PhDuration = (d.Phases.Count() > 0 ? d.Phases.Sum(x => x.PhSchedule == null ? 0 : x.PhSchedule.Days) : 0),
                                    UARigDescription = String.Format("{0} - {1}", d.UARigSequenceId, d.UARigDescription == null ? "" : d.UARigDescription),
                                    PhRiskDuration = (d.Phases.Count() > 0 ? d.Phases.Where(x => x.RiskFlag == null ? false : !x.RiskFlag.Equals("")).Sum(x => x.PhSchedule == null ? 0 : x.PhSchedule.Days) : 0),
                                    PhStartForFilter = (d.Phases.Count() > 0 ? d.Phases.Min(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Start) : Tools.DefaultDate),
                                    PhFinishForFilter = (d.Phases.Count() > 0 ? d.Phases.Max(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Finish) : Tools.DefaultDate),
                                    PsStartForFilter = (d.PsSchedule == null ? Tools.DefaultDate : d.PsSchedule.Start),
                                    PsFinishForFilter = (d.PsSchedule == null ? Tools.DefaultDate : d.PsSchedule.Finish),


                                    PsStart = p.Plan != null ? p.PlanSchedule.Start : Tools.DefaultDate,
                                    PsFinish = p.Plan != null ? p.PlanSchedule.Finish : Tools.DefaultDate,
                                    //PreviousPsStart = PreviousPsStart,
                                    //PreviousPsFinish = PreviousPsFinish,

                                    PhCost = (d.Phases.Count() > 0 ? (d.Phases.Sum(e => e.OP.Cost) / division) : 0),
                                    AFEStart = (p.AFESchedule != null ? p.AFESchedule.Start : Tools.DefaultDate).ToString("dd-MMM-yy"),
                                    AFEFinish = (p.AFESchedule != null ? p.AFESchedule.Start : Tools.DefaultDate).ToString("dd-MMM-yy"),
                                    AFEDuration = p.AFE != null ? p.AFE.Days : p.AFE.Cost,
                                    AFECost = Tools.Div(p.AFE != null ? p.AFE.Cost : 0.0, division),
                                    LEStart = p.LESchedule != null ? p.LESchedule.Start : Tools.DefaultDate,
                                    LEFinish = p.LESchedule != null ? p.LESchedule.Finish : Tools.DefaultDate,
                                    LEDuration = p.LESchedule != null ? p.LESchedule.Days : 0,
                                    LECost = Tools.Div(p.LE != null ? p.LE.Cost : 0, division),
                                    VirtualPhase = d.VirtualPhase,
                                    ShiftFutureEventDate = d.ShiftFutureEventDate,
                                    ShellShare = d.ShellShare,
                                    Status = p.Estimate.Status
                                    //BaseOP = GetBaseOP(d.Phases)
                                });
                            }
                        }
                    }

                }
                return final2;
            });
        }

        public BizPlanActivity FilterBizPlanActivityPhase(int id, List<string> BaseOP = null, string opRelation = "")
        {
            List<string> was = WebTools.GetWellActivities();
            List<string> WellActivities = new List<string>();

            foreach (var wa in was)
            {
                string[] waParts = wa.Split(new char[] { '|' });
                string wellName = waParts[0];
                string activityType = waParts[1];

                WellActivities.Add(activityType);
            }

            BizPlanActivity upd = new BizPlanActivity();
            var a = Query.EQ("_id", id);
            var now = Tools.ToUTC(DateTime.Now);
            var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
            BizPlanActivity ret = BizPlanActivity.Get<BizPlanActivity>(a);
            List<BizPlanActivityPhase> phases = new List<BizPlanActivityPhase>();
            foreach (var ph in ret.Phases) //.Where(x => x.LESchedule.Finish >= earlyThisMonth))
            {
                var wau = ph.GetLastUpdate(ret.WellName, ret.UARigSequenceId, false);

                if (ph.Estimate.MaturityLevel == null) ph.Estimate.MaturityLevel = "";

                if (WellActivities.Contains("*") || WellActivities.Contains(ph.ActivityType))
                {
                    ph.OP.Cost = Tools.Div(ph.OP.Cost, 1000000);
                    ph.Plan.Cost = Tools.Div(ph.Plan.Cost, 1000000);
                    ph.LE.Cost = Tools.Div(ph.LE.Cost, 1000000);
                    ph.AFE.Cost = Tools.Div(ph.AFE.Cost, 1000000);
                    ph.TQ.Cost = Tools.Div(ph.TQ.Cost, 1000000);
                    ph.Actual.Cost = Tools.Div(ph.Actual.Cost, 1000000);
                    if (BaseOP != null)
                    {
                        if (FunctionHelper.CompareBaseOP(BaseOP.ToArray(), ph.BaseOP.ToArray(), opRelation))
                        {
                            phases.Add(ph);
                        }
                    }
                    else
                    {
                        phases.Add(ph);
                    }

                }
                //if (wau==null || wau.UpdateVersion.Equals(Tools.GetNearestDay(DateTime.Now, DayOfWeek.Monday))==false)
                //{
                //    ph.LE = new WellDrillData();
                //}
            }
            ret.Phases = phases;
            return ret;
        }

        public JsonResult Export(WaterfallBase wbs)
        {
            var division = 1000000.0;
            var now = Tools.ToUTC(DateTime.Now);
            var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
            var email = WebTools.LoginUser.Email;
            var raw = wbs.GetActivitiesForBizPlan(email);
            var accessibleWells = WebTools.GetWellActivities();
            string previousOP = "";
            string nextOP = "";
            var datas = new List<object>();

            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "BizplanExportTemplate.xlsx");

            var MasterActivities = ActivityMaster.Populate<ActivityMaster>();

            Workbook wb = new Workbook(xlst);
            var ws = wb.Worksheets[0];
            int startRow = 2;

            if (raw != null && raw.Count > 0)
            {
                foreach (var d in raw)
                {
                    var PreviousPsStart = Tools.DefaultDate;
                    var PreviousPsFinish = Tools.DefaultDate;
                    var PsStart = Tools.DefaultDate;
                    var PsFinish = Tools.DefaultDate;

                    var PlanDuration = 0.0;
                    var PlanCost = 0.0;
                    var PreviousPlanDuration = 0.0;
                    var PreviousPlanCost = 0.0;

                    var checkHistory = d.OPHistories.Where(x => null != x.Type && x.Type.Equals(previousOP) && x.PlanSchedule.Start > Tools.DefaultDate && x.PlanSchedule.Finish > Tools.DefaultDate);
                    if (checkHistory.Any())
                    {
                        PreviousPsStart = checkHistory.Min(x => x.PlanSchedule.Start);
                        PreviousPsFinish = checkHistory.Max(x => x.PlanSchedule.Finish);

                        PreviousPlanDuration = checkHistory.Sum(x => x.Plan.Days);
                        PreviousPlanCost = checkHistory.Sum(x => x.Plan.Cost);
                    }
                    else
                    {
                        if (d.Phases.Any(x => x.BaseOP.Contains(previousOP)))
                        {
                            var vl = d.Phases.Where(x => x.BaseOP.Contains(previousOP) && x.PlanSchedule.Start > Tools.DefaultDate);
                            if (vl.Any())
                            {
                                PreviousPsStart = vl.Min(x => x.PlanSchedule.Start);
                                PreviousPsFinish = vl.Max(x => x.PlanSchedule.Finish);

                                PreviousPlanDuration = vl.Sum(x => x.Plan.Days);
                                PreviousPlanCost = vl.Sum(x => x.Plan.Cost);
                            }
                        }
                    }

                    if (d.Phases.Any())
                    {
                        foreach (var p in d.Phases)
                        {
                            var check = WebTools.CheckAccessibleWellAndActivity(accessibleWells, d.WellName, p.ActivityType);
                            if (check)
                            {
                                ws.Cells["A" + startRow.ToString()].Value = d.LineOfBusiness;
                                ws.Cells["B" + startRow.ToString()].Value = d.Region;
                                ws.Cells["C" + startRow.ToString()].Value = d.OperatingUnit;
                                ws.Cells["D" + startRow.ToString()].Value = d.PerformanceUnit;
                                ws.Cells["E" + startRow.ToString()].Value = d.AssetName;
                                ws.Cells["F" + startRow.ToString()].Value = d.ProjectName;
                                ws.Cells["G" + startRow.ToString()].Value = d.WellName;
                                ws.Cells["H" + startRow.ToString()].Value = p.ActivityType;
                                ws.Cells["I" + startRow.ToString()].Value = MasterActivities.Where(x => x._id.Equals(p.ActivityType)).FirstOrDefault() == null ? "" : MasterActivities.Where(x => x._id.Equals(p.ActivityType)).FirstOrDefault().ActivityCategory;
                                ws.Cells["J" + startRow.ToString()].Value = p.ActivityDesc; //scope desc
                                ws.Cells["K" + startRow.ToString()].Value = p.Estimate.SpreadRate;
                                ws.Cells["L" + startRow.ToString()].Value = p.Estimate.BurnRate;
                                ws.Cells["M" + startRow.ToString()].Value = ""; //MRI
                                ws.Cells["N" + startRow.ToString()].Value = ""; //Completion Type
                                ws.Cells["O" + startRow.ToString()].Value = p.Estimate.NumberOfCompletionZones;
                                ws.Cells["P" + startRow.ToString()].Value = p.Estimate.BrineDensity;
                                ws.Cells["Q" + startRow.ToString()].Value = ""; //est range tipe
                                ws.Cells["R" + startRow.ToString()].Value = ""; //Deterministic Low Range
                                ws.Cells["S" + startRow.ToString()].Value = ""; //Deterministic High
                                ws.Cells["T" + startRow.ToString()].Value = ""; //Probabilistic P10
                                ws.Cells["U" + startRow.ToString()].Value = ""; //Probabilistic P90
                                ws.Cells["V" + startRow.ToString()].Value = p.Estimate.WaterDepth;
                                ws.Cells["W" + startRow.ToString()].Value = p.Estimate.WellDepth;
                                ws.Cells["X" + startRow.ToString()].Value = ""; //Learning Curve Factor
                                ws.Cells["Y" + startRow.ToString()].Value = d.ShellShare;
                                ws.Cells["Z" + startRow.ToString()].Value = d.FirmOrOption;
                                ws.Cells["AA" + startRow.ToString()].Value = p.Estimate.MaturityLevel;

                                ws.Cells["AB" + startRow.ToString()].Value = p.FundingType;
                                ws.Cells["AC" + startRow.ToString()].Value = d.ReferenceFactorModel;
                                ws.Cells["AD" + startRow.ToString()].Value = p.Estimate.RigName;
                                ws.Cells["AE" + startRow.ToString()].Value = d.RigType;
                                ws.Cells["AF" + startRow.ToString()].Value = d.UARigSequenceId;
                                ws.Cells["AG" + startRow.ToString()].Value = p.Estimate.EstimatePeriod.Start.ToString("dd-MMM-yyyy");
                                ws.Cells["AH" + startRow.ToString()].Value = p.Estimate.EstimatePeriod.Finish.ToString("dd-MMM-yyyy");
                                ws.Cells["AI" + startRow.ToString()].Value = p.Estimate.NewTroubleFree.Days;
                                ws.Cells["AJ" + startRow.ToString()].Value = p.Estimate.NewNPTTime.PercentDays;
                                ws.Cells["AK" + startRow.ToString()].Value = p.Estimate.NewTECOPTime.PercentDays;
                                ws.Cells["AL" + startRow.ToString()].Value = ""; //Override Time Factors
                                ws.Cells["AM" + startRow.ToString()].Value = p.Estimate.NewNPTTime.Days;
                                ws.Cells["AN" + startRow.ToString()].Value = p.Estimate.NewTECOPTime.Days;
                                ws.Cells["AO" + startRow.ToString()].Value = p.Estimate.NewMean.Days;
                                ws.Cells["AP" + startRow.ToString()].Value = p.Estimate.NewTroubleFree.Cost;
                                ws.Cells["AQ" + startRow.ToString()].Value = p.Estimate.Currency;
                                ws.Cells["AR" + startRow.ToString()].Value = p.Estimate.NewNPTTime.PercentCost;
                                ws.Cells["AS" + startRow.ToString()].Value = p.Estimate.NewTECOPTime.PercentCost;
                                ws.Cells["AT" + startRow.ToString()].Value = ""; //Override Cost Factors
                                ws.Cells["AU" + startRow.ToString()].Value = p.Estimate.NewNPTTime.Cost;
                                ws.Cells["AV" + startRow.ToString()].Value = p.Estimate.NewTECOPTime.Cost;
                                ws.Cells["AW" + startRow.ToString()].Value = p.Estimate.NewMean.Cost;
                                ws.Cells["AX" + startRow.ToString()].Value = p.Estimate.NewTroubleFree.Cost;
                                ws.Cells["AY" + startRow.ToString()].Value = p.Estimate.NewNPTTime.Cost;
                                ws.Cells["AZ" + startRow.ToString()].Value = p.Estimate.NewTECOPTime.Cost;

                                ws.Cells["BA" + startRow.ToString()].Value = p.Estimate.NewMean.Cost;

                                ws.Cells["BB" + startRow.ToString()].Value = p.Allocation.AnnualyBuckets.Any() ? p.Allocation.AnnualyBuckets.Sum(x => x.EscCostTotal) : 0.0;
                                ws.Cells["BC" + startRow.ToString()].Value = p.Allocation.AnnualyBuckets.Any() ? p.Allocation.AnnualyBuckets.Sum(x => x.CSOCost) : 0.0;
                                ws.Cells["BD" + startRow.ToString()].Value = p.Allocation.AnnualyBuckets.Any() ? p.Allocation.AnnualyBuckets.Sum(x => x.InflationCost) : 0.0;
                                ws.Cells["BE" + startRow.ToString()].Value = p.Allocation.AnnualyBuckets.Any() ? p.Allocation.AnnualyBuckets.Sum(x => x.MeanCostMOD) : 0.0;
                                ws.Cells["BF" + startRow.ToString()].Value = p.Estimate.ProjectValueDriver;
                                ws.Cells["BG" + startRow.ToString()].Value = ""; //Value Driver Related Estimate
                                ws.Cells["BH" + startRow.ToString()].Value = p.Estimate.TQ.Threshold;
                                ws.Cells["BI" + startRow.ToString()].Value = p.Estimate.BIC.Threshold;
                                ws.Cells["BJ" + startRow.ToString()].Value = p.Estimate.TQ.Gap;
                                ws.Cells["BK" + startRow.ToString()].Value = p.Estimate.BIC.Gap;
                                ws.Cells["BL" + startRow.ToString()].Value = p.Estimate.PerformanceScore;

                                startRow++;
                            }
                        }
                    }
                }
            }

            var newFileNameSingle = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                     String.Format("Business Plan-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("yyyy-MM-dd-hhmmss")));

            string returnName = String.Format("Business Plan-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("yyyy-MM-dd-hhmmss"));

            wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            return Json(new { Success = true, Path = returnName }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ExportSummary(WaterfallBase wbs)
        {
            var division = 1000000.0;
            var now = Tools.ToUTC(DateTime.Now);
            var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
            var email = WebTools.LoginUser.Email;
            var raw = wbs.GetActivitiesForBizPlan(email);
            var accessibleWells = WebTools.GetWellActivities();
            string previousOP = "";
            string nextOP = "";
            var datas = new List<object>();

            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "Template_Bisplan_Cals.xlsx");

            var MasterActivities = ActivityMaster.Populate<ActivityMaster>();

            Workbook wb = new Workbook(xlst);
            var ws = wb.Worksheets[0];
            int startRow = 4; int IndexSheet = 1;

            #region Define_Header //wil be used for next

            //wb.Worksheets[IndexSheet].Cells["A" + 0].Value = "Line Of Business";
            //wb.Worksheets[IndexSheet].Cells["B" + 0].Value = "Region";
            //wb.Worksheets[IndexSheet].Cells["C" + 0].Value = "Rig Name";
            //wb.Worksheets[IndexSheet].Cells["D" + 0].Value = "Project Name";
            //wb.Worksheets[IndexSheet].Cells["E" + 0].Value = "OP Start";
            //wb.Worksheets[IndexSheet].Cells["F" + 0].Value = "OP Finish";
            //wb.Worksheets[IndexSheet].Cells["G" + 0].Value = "Ecs Cost Rig";
            //wb.Worksheets[IndexSheet].Cells["H" + 0].Value = "Ecs Cost Services";
            //wb.Worksheets[IndexSheet].Cells["I" + 0].Value = "Ecs Cost Materials";
            //wb.Worksheets[IndexSheet].Cells["J" + 0].Value = "Ecs Cost Total";
            //wb.Worksheets[IndexSheet].Cells["K" + 0].Value = "CSO Cost";
            //wb.Worksheets[IndexSheet].Cells["L" + 0].Value = "Inflation Cost";

            //wb.Worksheets[IndexSheet].Cells["M" + 0].Value = "Mean Cost EDM";
            //wb.Worksheets[IndexSheet].Cells["N" + 0].Value = "Mean Cost EDM With Shell Share";

            //wb.Worksheets[IndexSheet].Cells["O" + 0].Value = "Mean Cost Real Term";
            //wb.Worksheets[IndexSheet].Cells["P" + 0].Value = "Mean Cost Real Term With Shell Share";

            //wb.Worksheets[IndexSheet].Cells["Q" + 0].Value = "Mean Cost MOD";
            //wb.Worksheets[IndexSheet].Cells["R" + 0].Value = "Mean Cost MOD With Shell Share";

            //wb.Worksheets[IndexSheet].Cells.Merge(0, 0, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 1, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 2, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 3, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 4, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 5, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 6, 2, 1);

            //wb.Worksheets[IndexSheet].Cells.Merge(0, 7, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 8, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 9, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 10, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 11, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 12, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 13, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 14, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 15, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 16, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 17, 2, 1);
            #endregion
            var Messages = "";
            try
            {
                if (raw != null && raw.Count > 0)
                {
                    foreach (var d in raw)
                    {
                        var PreviousPsStart = Tools.DefaultDate;
                        var PreviousPsFinish = Tools.DefaultDate;
                        var PsStart = Tools.DefaultDate;
                        var PsFinish = Tools.DefaultDate;

                        var PlanDuration = 0.0;
                        var PlanCost = 0.0;
                        var PreviousPlanDuration = 0.0;
                        var PreviousPlanCost = 0.0;

                        var checkHistory = d.OPHistories.Where(x => null != x.Type && x.Type.Equals(previousOP) && x.PlanSchedule.Start > Tools.DefaultDate && x.PlanSchedule.Finish > Tools.DefaultDate);
                        if (checkHistory.Any())
                        {
                            PreviousPsStart = checkHistory.Min(x => x.PlanSchedule.Start);
                            PreviousPsFinish = checkHistory.Max(x => x.PlanSchedule.Finish);

                            PreviousPlanDuration = checkHistory.Sum(x => x.Plan.Days);
                            PreviousPlanCost = checkHistory.Sum(x => x.Plan.Cost);
                        }
                        else
                        {
                            if (d.Phases.Any(x => x.BaseOP.Contains(previousOP)))
                            {
                                var vl = d.Phases.Where(x => x.BaseOP.Contains(previousOP) && x.PlanSchedule.Start > Tools.DefaultDate);
                                if (vl.Any())
                                {
                                    PreviousPsStart = vl.Min(x => x.PlanSchedule.Start);
                                    PreviousPsFinish = vl.Max(x => x.PlanSchedule.Finish);

                                    PreviousPlanDuration = vl.Sum(x => x.Plan.Days);
                                    PreviousPlanCost = vl.Sum(x => x.Plan.Cost);
                                }
                            }
                        }

                        if (d.Phases.Any())
                        {
                            foreach (var p in d.Phases)
                            {
                                var check = WebTools.CheckAccessibleWellAndActivity(accessibleWells, d.WellName, p.ActivityType);
                                if (check)
                                {
                                    var Macro = MacroEconomic.Get<MacroEconomic>(
                                    Query.And(
                                        Query.EQ("Currency", d.Currency),
                                        Query.EQ("BaseOP", p.Estimate.SaveToOP),
                                        Query.EQ("Country", p.Estimate.Country)
                                        )
                                    );
                                    var rate = Macro.ExchangeRate.AnnualValues.Where(x => x.Year == p.Estimate.EstimatePeriod.Start.Year)
                                        .Any() ? Macro.ExchangeRate.AnnualValues.Where(x => x.Year == p.Estimate.EstimatePeriod.Start.Year).FirstOrDefault().Value : 0;
                                    //get Phase Info
                                    var CurrentTroubleFree = new WellDrillData() { Days = 0.0, Cost = 0.0 };
                                    var CurrentNPTTime = new WellDrillData() { Days = 0.0, Cost = 0.0 };
                                    var CurrentTECOPTime = new WellDrillData() { Days = 0.0, Cost = 0.0 };
                                    var CurrentMeanTime = new WellDrillData() { Days = 0.0, Cost = 0.0 };

                                    var qsPhaseInfo = new List<IMongoQuery>();
                                    qsPhaseInfo.Add(Query.EQ("WellName", d.WellName));
                                    qsPhaseInfo.Add(Query.EQ("ActivityType", p.ActivityType));
                                    qsPhaseInfo.Add(Query.EQ("RigName", d.RigName));
                                    qsPhaseInfo.Add(Query.EQ("SequenceId", d.UARigSequenceId));
                                    qsPhaseInfo.Add(Query.EQ("WellActivityId", d._id.ToString()));
                                    var getPhaseInfo = WellActivityPhaseInfo.Get<WellActivityPhaseInfo>(Query.And(qsPhaseInfo));
                                    if (getPhaseInfo != null)
                                    {
                                        CurrentTroubleFree = new WellDrillData() { Days = getPhaseInfo.TroubleFree.Days, Cost = getPhaseInfo.TroubleFree.Cost * 1000000 };
                                        CurrentNPTTime = new WellDrillData() { Days = getPhaseInfo.NPT.Days, Cost = getPhaseInfo.NPT.Cost * 1000000 };
                                        CurrentTECOPTime = new WellDrillData() { Days = getPhaseInfo.TECOP.Days, Cost = getPhaseInfo.TECOP.Cost * 1000000 };
                                        CurrentMeanTime.Days = getPhaseInfo.Mean.Days;
                                        CurrentMeanTime.Cost = getPhaseInfo.MeanCostEDM.Cost * 1000000;
                                    }

                                    ws.Cells["A" + startRow.ToString()].Value = d.LineOfBusiness;
                                    ws.Cells["B" + startRow.ToString()].Value = d.Region;
                                    ws.Cells["C" + startRow.ToString()].Value = d.Country;
                                    ws.Cells["D" + startRow.ToString()].Value = d.OperatingUnit;
                                    ws.Cells["E" + startRow.ToString()].Value = d.PerformanceUnit;
                                    ws.Cells["F" + startRow.ToString()].Value = d.AssetName;
                                    ws.Cells["G" + startRow.ToString()].Value = d.ProjectName;
                                    ws.Cells["H" + startRow.ToString()].Value = d.RigName;
                                    ws.Cells["I" + startRow.ToString()].Value = d.WellName;
                                    ws.Cells["J" + startRow.ToString()].Value = d.Currency;
                                    ws.Cells["K" + startRow.ToString()].Value = d.ShellShare;
                                    ws.Cells["L" + startRow.ToString()].Value = p.Estimate.SaveToOP;
                                    ws.Cells["M" + startRow.ToString()].Value = p.Estimate.Status == "" ? "Draft" : p.Estimate.Status;
                                    ws.Cells["N" + startRow.ToString()].Value = d.isInPlan ? "Yes" : "No";
                                    ws.Cells["O" + startRow.ToString()].Value = p.ActivityType;
                                    ws.Cells["P" + startRow.ToString()].Value = p.FundingType;
                                    //ref model
                                    ws.Cells["Q" + startRow.ToString()].Value = d.ReferenceFactorModel;
                                    ws.Cells["R" + startRow.ToString()].Value = d.DataInputBy;
                                    ws.Cells["S" + startRow.ToString()].PutValue(d.LastUpdate);
                                    //Uni
                                    ws.Cells["T" + startRow.ToString()].PutValue(p.Estimate.EventStartDate);
                                    ws.Cells["U" + startRow.ToString()].PutValue(p.Estimate.EventEndDate);
                                    ws.Cells["V" + startRow.ToString()].Value = Math.Round(p.Estimate.NewMean.Days, 2);
                                    Style styleStart = ws.Cells["T" + startRow.ToString()].GetStyle();
                                    styleStart.Custom = "m/d/yyyy";

                                    ws.Cells["S" + startRow.ToString()].SetStyle(styleStart);
                                    ws.Cells["T" + startRow.ToString()].SetStyle(styleStart);
                                    ws.Cells["U" + startRow.ToString()].SetStyle(styleStart);

                                    ws.Cells["W" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.RigRate);
                                    ws.Cells["X" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.Services);
                                    ws.Cells["Y" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.Materials);
                                    ws.Cells["Z" + startRow.ToString()].Value = Tools.Div(p.Estimate.PercOfMaterialsLongLead, 100);
                                    ws.Cells["AA" + startRow.ToString()].Value = p.Estimate.LongLeadMonthRequired;
                                    ws.Cells["AB" + startRow.ToString()].Value = p.Estimate.LongLeadCalc;
                                    ws.Cells["AC" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.SpreadRate);
                                    ws.Cells["AD" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.BurnRate);
                                    ws.Cells["AE" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.SpreadRateTotal);
                                    //TIME ESTIMATE
                                    ws.Cells["AF" + startRow.ToString()].Value = CurrentTroubleFree.Days;
                                    ws.Cells["AG" + startRow.ToString()].Value = 0;
                                    ws.Cells["AH" + startRow.ToString()].Value = p.Estimate.CurrentNPTTime.PercentDays;
                                    ws.Cells["AI" + startRow.ToString()].Value = p.Estimate.CurrentTECOPTime.PercentDays;
                                    ws.Cells["AJ" + startRow.ToString()].Value = CurrentNPTTime.Days;
                                    ws.Cells["AK" + startRow.ToString()].Value = 0;
                                    ws.Cells["AL" + startRow.ToString()].Value = 0;
                                    ws.Cells["AM" + startRow.ToString()].Value = 0;
                                    ws.Cells["AN" + startRow.ToString()].Value = CurrentTECOPTime.Days;
                                    ws.Cells["AO" + startRow.ToString()].Value = CurrentMeanTime.Days;
                                    //OP16
                                    ws.Cells["AP" + startRow.ToString()].Value = p.Estimate.NewTroubleFree.Days;
                                    ws.Cells["AQ" + startRow.ToString()].Value = p.Estimate.NewLearningCurveFactor;
                                    ws.Cells["AR" + startRow.ToString()].Value = p.Estimate.NewNPTTime.PercentDays;
                                    ws.Cells["AS" + startRow.ToString()].Value = p.Estimate.NewTECOPTime.PercentDays;
                                    ws.Cells["AT" + startRow.ToString()].Value = p.Estimate.NewNPTTime.Days;
                                    ws.Cells["AU" + startRow.ToString()].Value = p.Estimate.NewLearningCurveFactor;
                                    ws.Cells["AV" + startRow.ToString()].Value = Math.Round(p.Estimate.NewBaseValue.Days, 2);
                                    ws.Cells["AW" + startRow.ToString()].Value = Math.Round(p.Estimate.NewBaseValue.Days - p.Estimate.NewLCFValue.Days, 2);
                                    ws.Cells["AX" + startRow.ToString()].Value = Math.Round(p.Estimate.NewTECOPTime.Days, 2);
                                    ws.Cells["AY" + startRow.ToString()].Value = Math.Round(p.Estimate.NewMean.Days, 2);

                                    //COST ESTIMATE
                                    ws.Cells["AZ" + startRow.ToString()].Value = String.Format("{0:n2}", CurrentTroubleFree.Cost);
                                    ws.Cells["BA" + startRow.ToString()].Value = 0;
                                    ws.Cells["BB" + startRow.ToString()].Value = 0;
                                    ws.Cells["BC" + startRow.ToString()].Value = String.Format("{0:n2}", CurrentNPTTime.Cost);
                                    ws.Cells["BD" + startRow.ToString()].Value = 0;
                                    ws.Cells["BE" + startRow.ToString()].Value = 0;
                                    ws.Cells["BF" + startRow.ToString()].Value = 0;
                                    ws.Cells["BG" + startRow.ToString()].Value = String.Format("{0:n2}", CurrentTECOPTime.Cost);
                                    ws.Cells["BH" + startRow.ToString()].Value = String.Format("{0:n2}", CurrentMeanTime.Cost);
                                    //0p16    B
                                    ws.Cells["BI" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.NewTroubleFree.Cost);
                                    ws.Cells["BJ" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.NewNPTTime.PercentCost);
                                    ws.Cells["BK" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.NewTECOPTime.PercentCost);
                                    ws.Cells["BL" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.NewNPTTime.Cost);
                                    ws.Cells["BM" + startRow.ToString()].Value = Math.Round(p.Estimate.NewLearningCurveFactor * (p.Estimate.NewTroubleFree.Cost + p.Estimate.NewNPTTime.Cost), 2);
                                    var pBaseValue = p.Estimate.NewTroubleFreeUSD + p.Estimate.NPTCostUSD;
                                    ws.Cells["BN" + startRow.ToString()].Value = String.Format("{0:n2}", (pBaseValue));
                                    ws.Cells["BO" + startRow.ToString()].Value = String.Format("{0:n2}", pBaseValue - p.Estimate.NewLCFValue.Cost);
                                    ws.Cells["BP" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.NewTECOPTime.Cost);
                                    ws.Cells["BQ" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.NewMean.Cost);
                                    //CONVERSIBN TO USD
                                    ws.Cells["BR" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.NewTroubleFreeUSD);
                                    ws.Cells["BS" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.NPTCostUSD);
                                    ws.Cells["BT" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.TECOPCostUSD);
                                    ws.Cells["BU" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.MeanUSD);
                                    //level   B
                                    ws.Cells["BV" + startRow.ToString()].Value = p.Estimate.MaturityLevel;
                                    //Wells Value Driver Not Selected
                                    //tq
                                    ws.Cells["BW" + startRow.ToString()].Value = 0.00;
                                    ws.Cells["BX" + startRow.ToString()].Value = 0.00;
                                    //bic
                                    ws.Cells["BY" + startRow.ToString()].Value = 0.00;
                                    ws.Cells["BZ" + startRow.ToString()].Value = 0.00;
                                    //calc TQ
                                    var _TQGapTotalDays = Math.Round(p.Estimate.NewMean.Days - p.Estimate.TQValueDriver, 0);
                                    var _TQGapDry = p.Estimate.DryHoleDays - p.Estimate.TQValueDriver;
                                    var _BICGapDry = p.Estimate.DryHoleDays - p.Estimate.BICValueDriver;
                                    var _TQGapCost = p.Estimate.NewMean.Cost - (p.Estimate.TQValueDriver * 1000000);
                                    var _BICGapCost = p.Estimate.NewMean.Cost - (p.Estimate.BICValueDriver * 1000000);
                                    var _BICGapTotalDays = Math.Round(p.Estimate.NewMean.Days - p.Estimate.BICValueDriver, 0);
                                    //Wells Value Driver Total Days
                                    var TQGapCostCalc = Tools.Div(_TQGapTotalDays, rate) * p.Estimate.SpreadRateTotal;
                                    var TQTotalCost = Tools.Div(p.Estimate.NewMean.Cost, rate) - TQGapCostCalc;
                                    ws.Cells["CA" + startRow.ToString()].Value = Math.Round(p.Estimate.NewMean.Days, 2); //EDM DAYS
                                    ws.Cells["CB" + startRow.ToString()].Value = Math.Round(p.Estimate.TQValueDriver, 2); //TQ For Value Driver
                                    ws.Cells["CC" + startRow.ToString()].Value = Math.Round(_TQGapTotalDays, 2); // TQ GAP ok
                                    ws.Cells["CD" + startRow.ToString()].Value = String.Format("{0:n2}", (TQGapCostCalc)); //TQ Gap Cost (calc)  
                                    ws.Cells["CE" + startRow.ToString()].Value = String.Format("{0:n2}", (TQTotalCost)); //TQ Total Cost (calc) (USD)
                                    var BICGapCostCalc = Tools.Div(_BICGapTotalDays, rate) * p.Estimate.SpreadRateTotal;
                                    var BICTotalCost = Tools.Div(p.Estimate.NewMean.Cost, rate) - BICGapCostCalc;
                                    ws.Cells["CF" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.BICValueDriver);
                                    ws.Cells["CG" + startRow.ToString()].Value = Math.Round(_BICGapTotalDays, 2); // BIC Gap
                                    ws.Cells["CH" + startRow.ToString()].Value = String.Format("{0:n2}", Math.Round(BICGapCostCalc));
                                    ws.Cells["CI" + startRow.ToString()].Value = String.Format("{0:n2}", (BICTotalCost));
                                    //Dry Hole Days
                                    var TQGapDry = Tools.Div(_TQGapDry, rate) * p.Estimate.SpreadRateTotal;
                                    ws.Cells["CJ" + startRow.ToString()].Value = p.Estimate.DryHoleDays;
                                    ws.Cells["CK" + startRow.ToString()].Value = Math.Round(p.Estimate.TQValueDriver, 2); // TQ For Value Driver
                                    ws.Cells["CL" + startRow.ToString()].Value = Math.Round(_TQGapDry, 2); //TQ GAP
                                    var BICGapDry = Tools.Div(_BICGapDry, rate) * p.Estimate.SpreadRateTotal;
                                    ws.Cells["CM" + startRow.ToString()].Value = Math.Round(_BICGapDry, 2);
                                    ws.Cells["CN" + startRow.ToString()].Value = Math.Round(BICGapDry, 2);
                                    //Well value Cost

                                    var TQGapCost = p.Estimate.NewMean.Cost - (p.Estimate.TQValueDriver * 1000000);
                                    ws.Cells["CO" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.NewMean.Cost); // EDM COST
                                    ws.Cells["CP" + startRow.ToString()].Value = String.Format("{0:n2}", (p.Estimate.TQValueDriver));//DRIVER
                                    ws.Cells["CQ" + startRow.ToString()].Value = String.Format("{0:n2}", (_TQGapCost)); ; // GAP
                                    //bic
                                    ws.Cells["CR" + startRow.ToString()].Value = String.Format("{0:n2}", (p.Estimate.BICValueDriver));
                                    ws.Cells["CS" + startRow.ToString()].Value = String.Format("{0:n2}", (_BICGapCost));
                                    //Benmachmark
                                    ws.Cells["CT" + startRow.ToString()].Value = Math.Round(p.Estimate.TQValueDriver, 2);
                                    ws.Cells["CU" + startRow.ToString()].Value = 0.00;
                                    ws.Cells["CV" + startRow.ToString()].Value = Math.Round(p.Estimate.BICValueDriver, 2);
                                    ws.Cells["CW" + startRow.ToString()].Value = 0.00;
                                    //Perfom
                                    ws.Cells["CX" + startRow.ToString()].Value = p.Estimate.PerformanceScore;
                                    ws.Cells["CY" + startRow.ToString()].Value = p.Estimate.WaterDepth;
                                    ws.Cells["CZ" + startRow.ToString()].Value = p.Estimate.WellDepth;
                                    ws.Cells["DA" + startRow.ToString()].Value = p.Estimate.NumberOfCasings;
                                    ws.Cells["DB" + startRow.ToString()].Value = p.Estimate.MechanicalRiskIndex;
                                    //EVENT SUMMARY
                                    Messages = "WellName:" + d.WellName + " Act:" + p.ActivityType;
                                    var pMeanCost = p.Allocation == null ? 0 : p.Allocation.AnnualyBuckets.Sum(x => x.MeanCostRealTerm);
                                    ws.Cells["DC" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.NewMean.Cost);
                                    ws.Cells["DD" + startRow.ToString()].Value = String.Format("{0:n2}", pMeanCost);
                                    ws.Cells["DE" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.MeanCostMOD);
                                    ws.Cells["DF" + startRow.ToString()].Value = String.Format("{0:n2}", p.Estimate.ShellShareCalc);
                                    startRow++;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var cda = Messages;
                throw;
            }


            var newFileNameSingle = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                     String.Format("Business Plan-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("yyyy-MM-dd-hhmmss")));

            string returnName = String.Format("Business Plan-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("yyyy-MM-dd-hhmmss"));

            wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            return Json(new { Success = true, Path = returnName }, JsonRequestBehavior.AllowGet);
        }

        public List<Excelbusplan> GenerateExportDetailData(List<BizPlanActivity> raw, string PreviousOP, List<string> accessibleWells, WaterfallBase wb)
        {
            List<Excelbusplan> edetail = new List<Excelbusplan>();
            if (raw != null && raw.Count > 0)
            {
                foreach (var d in raw)
                {
                    var PreviousPsStart = Tools.DefaultDate;
                    var PreviousPsFinish = Tools.DefaultDate;
                    var PsStart = Tools.DefaultDate;
                    var PsFinish = Tools.DefaultDate;

                    var PlanDuration = 0.0;
                    var PlanCost = 0.0;
                    var PreviousPlanDuration = 0.0;
                    var PreviousPlanCost = 0.0;

                    var checkHistory = d.OPHistories.Where(x => null != x.Type && x.Type.Equals(PreviousOP) && x.PlanSchedule.Start > Tools.DefaultDate && x.PlanSchedule.Finish > Tools.DefaultDate);
                    if (checkHistory.Any())
                    {
                        PreviousPsStart = checkHistory.Min(x => x.PlanSchedule.Start);
                        PreviousPsFinish = checkHistory.Max(x => x.PlanSchedule.Finish);

                        PreviousPlanDuration = checkHistory.Sum(x => x.Plan.Days);
                        PreviousPlanCost = checkHistory.Sum(x => x.Plan.Cost);
                    }
                    else
                    {
                        if (d.Phases.Any(x => x.BaseOP.Contains(PreviousOP)))
                        {
                            var vl = d.Phases.Where(x => x.BaseOP.Contains(PreviousOP) && x.PlanSchedule.Start > Tools.DefaultDate);
                            if (vl.Any())
                            {
                                PreviousPsStart = vl.Min(x => x.PlanSchedule.Start);
                                PreviousPsFinish = vl.Max(x => x.PlanSchedule.Finish);

                                PreviousPlanDuration = vl.Sum(x => x.Plan.Days);
                                PreviousPlanCost = vl.Sum(x => x.Plan.Cost);
                            }
                        }
                    }

                    var basesops = wb.OPs == null ? new List<string>() : wb.OPs;


                    List<BizPlanActivityPhase> phases = new List<BizPlanActivityPhase>();
                    if (basesops.Count() == 1 && basesops.FirstOrDefault().Trim().Equals(""))
                    {
                        phases = d.Phases;
                    }
                    else if (basesops.Count() == 0)
                    {
                        phases = d.Phases;
                    }
                    else
                    {
                        phases = d.Phases.Where(x => basesops.ToList().Contains(x.Estimate.SaveToOP)).ToList();
                    }

                    if (phases.Any())
                    {
                        foreach (var p in phases)
                        {
                            //check roles
                            var check = WebTools.CheckAccessibleWellAndActivity(accessibleWells, d.WellName, p.ActivityType);
                            if (check)
                            {
                                var OPEstimateCurr = new List<object>();
                                var OPEstimatePrev = new List<object>();
                                var OPEstimate = new List<object>();
                                int i = 0;

                                var isHaveWeeklyReport = WellActivity.isHaveWeeklyReport(d.WellName, d.UARigSequenceId, p.ActivityType);

                                if (!isHaveWeeklyReport)
                                {

                                    p.Estimate.EstimatePeriod.Start = Tools.ToUTC(p.Estimate.EstimatePeriod.Start, true);
                                    p.Estimate.EstimatePeriod.Finish = Tools.ToUTC(p.Estimate.EstimatePeriod.Finish);
                                    p.Estimate.EventStartDate = Tools.ToUTC(p.Estimate.EventStartDate, true);
                                    p.Estimate.EventEndDate = Tools.ToUTC(p.Estimate.EventEndDate);

                                    var Macro = MacroEconomic.Get<MacroEconomic>(
                                    Query.And(
                                    Query.EQ("Currency", d.Currency == null ? "" : d.Currency),
                                    Query.EQ("BaseOP", p.Estimate.SaveToOP == null ? "" : p.Estimate.SaveToOP),
                                    Query.EQ("Country", p.Estimate.Country == null ? "" : d.Country)
                                        )
                                    );
                                    var rate = 0;
                                    if (Macro == null)
                                    {
                                        rate = 0;
                                    }
                                    else
                                    {
                                        var getRate = Macro.ExchangeRate.AnnualValues.Where(x => x.Year == p.Estimate.EstimatePeriod.Start.Year)
                                        .Any() ? Macro.ExchangeRate.AnnualValues.Where(x => x.Year == p.Estimate.EstimatePeriod.Start.Year).FirstOrDefault().Value : 0;
                                        rate = Convert.ToInt32(getRate);
                                    }

                                    BizPlanSummary bisCal = new BizPlanSummary(d.WellName, p.Estimate.RigName, p.ActivityType, d.Country == null ? "" : d.Country == null ? "" : d.Country,
                                    d.ShellShare, p.Estimate.EstimatePeriod, p.Estimate.MaturityLevel == null ? 0 : Convert.ToInt32(p.Estimate.MaturityLevel.Replace("Type", "")),
                                    p.Estimate.Services, p.Estimate.Materials,
                                    p.Estimate.TroubleFreeBeforeLC.Days,
                                    d.ReferenceFactorModel == null ? "" : d.ReferenceFactorModel,
                                    Tools.Div(p.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentDays, 100),
                                    Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100),
                                    p.Estimate.LongLeadMonthRequired, Tools.Div(p.Estimate.PercOfMaterialsLongLead, 100), baseOP: p.Estimate.SaveToOP == null ? "" : p.Estimate.SaveToOP, isGetExcRateByCurrency: true,
                                    lcf: p.Estimate.NewLearningCurveFactor
                                    );

                                    bisCal.GeneratePeriodBucket();
                                    //set object
                                    Excelbusplan eb = new Excelbusplan();
                                    eb.Status = p.Estimate.Status;
                                    eb.LineOfBusiness = d.LineOfBusiness;
                                    eb.RigType = d.RigType;
                                    eb.Region = d.Region;
                                    eb.Country = d.Country == null ? "" : d.Country;
                                    eb.OperatingUnit = d.OperatingUnit;
                                    eb.PerformanceUnit = d.PerformanceUnit;
                                    eb.AssetName = d.AssetName;
                                    eb.ProjectName = d.ProjectName;
                                    eb.RigName = p.Estimate.RigName;
                                    eb.WellName = d.WellName;
                                    eb.ActivityType = p.ActivityType;
                                    eb.RigSeqId = d.UARigSequenceId;
                                    eb.Currency = d.Currency == null ? "" : d.Currency;
                                    eb.ShellShare = d.ShellShare;
                                    eb.SaveToOP = p.Estimate.SaveToOP == null ? "" : p.Estimate.SaveToOP;
                                    //eb.LastSubmitted = d.LastUpdate;
                                    eb.LastSubmitted = p.Estimate.LastUpdate;// d.LastUpdate;
                                    eb.DataInputBy = p.Estimate.LastUpdateBy;
                                    //eb.DataInputBy = p.Estimate.LastUpdateBy;// d.DataInputBy;


                                    eb.InPlan = d.isInPlan ? true : false;
                                    //eb.RFM = d.ReferenceFactorModel;
                                    eb.RFM = d.ReferenceFactorModel == "default" ? p.PhaseInfo.ReferenceFactorModel : d.ReferenceFactorModel;
                                    eb.FundingType = p.FundingType;
                                    eb.Event = new DateRange() { Start = p.Estimate.EventStartDate, Finish = p.Estimate.EventStartDate.Date.AddDays(bisCal.MeanTime) };
                                    eb.EventDays = Math.Round(bisCal.MeanTime, 2);
                                    eb.RigRate = Math.Round(p.Estimate.RigRate);
                                    eb.Services = bisCal.Services; // p.Estimate.Services;
                                    eb.Materials = bisCal.Material; //p.Estimate.Materials;
                                    eb.isMaterialLLSetManually = p.Estimate.isMaterialLLSetManually ? true : false;
                                    eb.PercOfMaterialsLongLead = Tools.Div(p.Estimate.PercOfMaterialsLongLead, 100);
                                    eb.LLRequiredMonth = p.Estimate.LongLeadMonthRequired;
                                    eb.LLCalc = p.Estimate.LongLeadCalc;
                                    eb.BurnRate = bisCal.BurnRate; // p.Estimate.BurnRate;
                                    eb.SpreadRate = bisCal.SpreadRateWRig; // p.Estimate.SpreadRate;
                                    eb.SpreadRateTotal = bisCal.SpreadRateTotal;// p.Estimate.SpreadRateTotal;
                                    eb.IsUsingLCFfromRFM = p.Estimate.IsUsingLCFfromRFM;
                                    ////OP16
                                    eb.UseTAApproved = p.Estimate.IsUsingLCFfromRFM;
                                    eb.TroubleFree = new WellDrillData()
                                    {
                                        Days = bisCal.TroubleFree.Days,// p.Estimate.NewTroubleFree.Days,
                                        Cost = bisCal.TroubleFree.Cost //p.Estimate.NewTroubleFree.Cost
                                    };
                                    eb.LCFParameter = bisCal.LCF;// p.Estimate.NewLearningCurveFactor;
                                    eb.NPT = new WellDrillDataPercent()
                                    {
                                        Days = bisCal.NPT.Days,// p.Estimate.NewNPTTime.PercentDays == 0 ? 0 : p.Estimate.NewNPTTime.Days,
                                        Cost = bisCal.NPT.Cost,// p.Estimate.NewNPTTime.Cost,
                                        PercentTime = bisCal.NPTRate * 100, //p.Estimate.NewNPTTime.PercentDays,
                                        PercentCost = bisCal.NPTRateCost // Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100)
                                    };

                                    eb.LCF = new WellDrillData()
                                    {
                                        Days = bisCal.NewLCFValue.Days, //p.Estimate.NewLCFValue.Days,
                                        Cost = bisCal.NewLCFValue.Cost  //Math.Round(p.Estimate.NewLearningCurveFactor * (p.Estimate.NewTroubleFree.Cost + p.Estimate.NewNPTTime.Cost), 2)
                                    };
                                    eb.Base = new WellDrillData()
                                    {
                                        Days = bisCal.NewBaseValue.Days, //Math.Round(p.Estimate.NewBaseValue.Days, 2),
                                        Cost = bisCal.NewBaseValue.Cost  //bisCal.NewBaseValue.Cost // p.Estimate.NewTroubleFreeUSD + p.Estimate.NPTCostUSD
                                    };
                                    eb.BaseCalc = new WellDrillData()
                                    {
                                        Days = eb.Base.Days - eb.LCF.Days, //Math.Round(p.Estimate.NewBaseValue.Days - p.Estimate.NewLCFValue.Days, 2),
                                        Cost = eb.Base.Cost - eb.LCF.Cost,  //(p.Estimate.NewTroubleFreeUSD + p.Estimate.NPTCostUSD) - p.Estimate.NewLCFValue.Cost
                                    };
                                    eb.TECOP = new WellDrillDataPercent()
                                    {
                                        Days = bisCal.TECOP.Days, //Math.Round(p.Estimate.NewTECOPTime.PercentDays == 0 ? 0 : p.Estimate.NewTECOPTime.Days, 2),
                                        Cost = bisCal.TECOP.Cost,// p.Estimate.NewTECOPTime.Cost,
                                        PercentTime = bisCal.TECOPRate * 100,//  p.Estimate.NewTECOPTime.PercentDays,
                                        PercentCost = bisCal.TECOPRateCost //Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100)
                                    };

                                    eb.Mean = new WellDrillData()
                                    {
                                        Days = bisCal.MeanTime,//Math.Round(p.Estimate.NewMean.Days, 2),
                                        Cost = bisCal.MeanCostEDM,// p.Estimate.NewMean.Cost
                                    };
                                    //convert to usd
                                    eb.TroubleFreeCostUSD = bisCal.TroubleFreeCostUSD;// p.Estimate.NewTroubleFreeUSD;
                                    eb.NptCostUSD = bisCal.NPTCostUSD; //p.Estimate.NPTCostUSD;
                                    eb.TecopCostUSD = bisCal.TECOPCostUSD; //p.Estimate.TECOPCostUSD;
                                    eb.MeanCostUSD = bisCal.MeanCostEDMUSD; //p.Estimate.MeanUSD;


                                    eb.TroubleFreeCost = bisCal.TroubleFree.Cost; //p.Estimate.NewTroubleFree.Cost;
                                    eb.NptCost = bisCal.NPT.Cost; //p.Estimate.NewNPTTime.Cost;
                                    eb.TecopCost = bisCal.TECOP.Cost;//p.Estimate.NewTECOPTime.Cost;
                                    eb.MeanCost = bisCal.MeanCostEDM;//p.Estimate.NewMean.Cost;
                                    #region WVA

                                    eb.MaturityLevel = p.Estimate.MaturityLevel;
                                    ////calc TQ BIC
                                    var _TQGapTotalDays = Math.Round(Math.Round(p.Estimate.NewMean.Days, 2) - p.Estimate.TQValueDriver, 2);
                                    var _TQGapDry = Math.Round(p.Estimate.DryHoleDays, 2) - p.Estimate.TQValueDriver;
                                    var _BICGapDry = Math.Round(p.Estimate.DryHoleDays, 2) - p.Estimate.BICValueDriver;
                                    var _TQGapCost = Math.Round(p.Estimate.NewMean.Cost, 2) - (p.Estimate.TQValueDriver * 1000000);
                                    var _BICGapCost = Math.Round(p.Estimate.NewMean.Cost, 2) - (p.Estimate.BICValueDriver * 1000000);
                                    var _BICGapTotalDays = Math.Round(Math.Round(p.Estimate.NewMean.Days, 2) - p.Estimate.BICValueDriver, 2);
                                    ////Wells Value Driver Total Days
                                    var TQGapCostCalc = Math.Round(Tools.Div(_TQGapTotalDays, rate) * Math.Round(p.Estimate.SpreadRateTotal, 2), 2);
                                    var TQTotalCost = Math.Round(Tools.Div(Math.Round(p.Estimate.NewMean.Cost, 2), rate) - TQGapCostCalc, 2);
                                    var BICGapCostCalc = Math.Round(Tools.Div(_BICGapTotalDays, rate) * Math.Round(p.Estimate.SpreadRateTotal, 2), 2);
                                    var BICTotalCost = Math.Round(Tools.Div(Math.Round(p.Estimate.NewMean.Cost, 2), rate) - BICGapCostCalc, 2);
                                    var TQGapDry = Math.Round(Tools.Div(_TQGapDry, rate) * Math.Round(p.Estimate.SpreadRateTotal, 2), 2);
                                    var TQGapCost = p.Estimate.NewMean.Cost - (p.Estimate.TQValueDriver * 1000000);
                                    var BICGapDry = Math.Round(Tools.Div(_BICGapDry, rate) * Math.Round(p.Estimate.SpreadRateTotal, 2), 2);
                                    var WellValueDriver = p.Estimate.WellValueDriver == null || p.Estimate.WellValueDriver == "" ? "" : p.Estimate.WellValueDriver;
                                    eb.WellValueDriver = WellValueDriver;
                                    eb.WVATotalDays = new WVA();
                                    eb.WVATotalCost = new WVA();
                                    eb.WVADryHoleDays = new WVA();
                                    if (WellValueDriver.Equals("Total Days"))
                                    {
                                        eb.MeanWVA = new WellDrillData()
                                        {
                                            Days = Math.Round(p.Estimate.NewMean.Days, 2)
                                        };
                                        eb.WVATotalDays = new WVA()
                                        {
                                            TQValueDriver = new WellDrillData()
                                            {
                                                Days = Math.Round(p.Estimate.TQValueDriver, 2),
                                                Cost = TQTotalCost
                                            },
                                            TQGap = new WellDrillData()
                                            {
                                                Days = Math.Round(_TQGapTotalDays, 2),
                                                Cost = TQGapCostCalc
                                            },
                                            BICValueDriver = new WellDrillData()
                                            {
                                                Days = p.Estimate.BICValueDriver,
                                                Cost = BICTotalCost
                                            },
                                            BICGap = new WellDrillData()
                                            {
                                                Days = _BICGapTotalDays,
                                                Cost = BICGapCostCalc
                                            }

                                        };
                                    }
                                    else if (WellValueDriver.Equals("Cost"))
                                    {
                                        eb.MeanWVA = new WellDrillData()
                                        {
                                            Cost = p.Estimate.NewMean.Cost
                                        };
                                        eb.WVATotalCost = new WVA()
                                        {
                                            TQValueDriver = new WellDrillData()
                                            {
                                                Cost = p.Estimate.TQValueDriver
                                            },
                                            TQGap = new WellDrillData()
                                            {
                                                Cost = _TQGapCost
                                            },
                                            BICValueDriver = new WellDrillData()
                                            {
                                                Cost = p.Estimate.BICValueDriver
                                            },
                                            BICGap = new WellDrillData()
                                            {
                                                Cost = _BICGapCost
                                            }
                                        };
                                    }
                                    else if (WellValueDriver.Equals("Dry Hole Days"))
                                    {
                                        eb.DryHoleDays = p.Estimate.DryHoleDays;
                                        eb.WVADryHoleDays = new WVA()
                                        {
                                            TQValueDriver = new WellDrillData()
                                            {
                                                Days = Math.Round(p.Estimate.TQValueDriver, 2)
                                            },
                                            TQGap = new WellDrillData()
                                            {
                                                Days = _TQGapDry
                                            },
                                            BICValueDriver = new WellDrillData()
                                            {
                                                //Days = _BICGapDry
                                                Days = Math.Round(p.Estimate.BICValueDriver, 2)
                                            },
                                            BICGap = new WellDrillData()
                                            {
                                                Cost = BICGapDry
                                            }

                                        };
                                    }
                                    #endregion
                                    ////Wells Value Driver Not Selected                                    
                                    eb.MeanCostEDM = bisCal.MeanCostEDM;// p.Allocation == null ? 0.0 : Math.Round(p.Allocation.AnnualyBuckets.Sum(x => x.MeanCostEDM), 2); // Mean Cost EDM
                                    eb.MeanCostRealTerm = bisCal.MeanCostRealTerm;// p.Allocation == null ? 0.0 : Math.Round(p.Allocation.AnnualyBuckets.Sum(x => x.MeanCostRealTerm), 2);
                                    //eb.MeanCostMOD = bisCal.MeanCostMOD;
                                    eb.MeanCostMOD = bisCal.MeanCostMOD;// p.Allocation == null ? 0.0 : Math.Round(p.Allocation.AnnualyBuckets.Sum(x => x.MeanCostMOD), 2);
                                    eb.ShellShareCost = bisCal.ShellShareCost;// p.Allocation == null ? 0.0 : Math.Round(p.Allocation.AnnualyBuckets.Sum(x => x.ShellShare), 2);
                                    //eb.ShellShareCost = bisCal.ShellShareCost;
                                    ////Perfom
                                    eb.PerformanceScore = p.Estimate.PerformanceScore;
                                    eb.WaterDepth = p.Estimate.WaterDepth;
                                    eb.WellDepth = p.Estimate.WellDepth;
                                    eb.NumberOfCasings = p.Estimate.NumberOfCasings;
                                    eb.MechanicalRiskIndex = p.Estimate.MechanicalRiskIndex;

                                    //add new 


                                    eb.EscalationRig = bisCal.MonthlyBuckets == null ? 0 : bisCal.MonthlyBuckets.Sum(x => x.EscCostRig);
                                    eb.EscalationService = bisCal.MonthlyBuckets == null ? 0 : bisCal.MonthlyBuckets.Sum(x => x.EscCostServices);
                                    eb.EscalationMaterials = bisCal.MonthlyBuckets == null ? 0 : bisCal.MonthlyBuckets.Sum(x => x.EscCostMaterial);
                                    eb.EscalationCost = bisCal.MonthlyBuckets == null ? 0 : bisCal.MonthlyBuckets.Sum(x => x.EscCostTotal);
                                    eb.CSOCost = bisCal.MonthlyBuckets == null ? 0 : bisCal.MonthlyBuckets.Sum(x => x.CSOCost);
                                    eb.InflationCost = bisCal.MonthlyBuckets == null ? 0 : bisCal.MonthlyBuckets.Sum(x => x.InflationCost);
                                    edetail.Add(eb);
                                }
                            }
                        }
                    }
                }
            }

            return edetail;
        }

        public List<Excelbusplan> GenerateExportDetailData(List<FilterHelper> raw, string PreviousOP, List<string> accessibleWells, WaterfallBase wb, List<BizPlanActivity> bizplans)
        {
            List<Excelbusplan> edetail = new List<Excelbusplan>();
            if (raw != null && raw.Count > 0)
            {
                foreach (var datax in raw)
                {
                    BizPlanActivity d= new BizPlanActivity();
                    if (bizplans == null)
                        d = BizPlanActivity.Get<BizPlanActivity>(Query.EQ("_id", datax._id));
                    else
                        d = bizplans.FirstOrDefault(x => Convert.ToInt32( x._id) == datax._id);
                    var PreviousPsStart = Tools.DefaultDate;
                    var PreviousPsFinish = Tools.DefaultDate;
                    var PsStart = Tools.DefaultDate;
                    var PsFinish = Tools.DefaultDate;

                    var PlanDuration = 0.0;
                    var PlanCost = 0.0;
                    var PreviousPlanDuration = 0.0;
                    var PreviousPlanCost = 0.0;

                    var checkHistory = d.OPHistories.Where(x => null != x.Type && x.Type.Equals(PreviousOP) && x.PlanSchedule.Start > Tools.DefaultDate && x.PlanSchedule.Finish > Tools.DefaultDate);
                    if (checkHistory.Any())
                    {
                        PreviousPsStart = checkHistory.Min(x => x.PlanSchedule.Start);
                        PreviousPsFinish = checkHistory.Max(x => x.PlanSchedule.Finish);

                        PreviousPlanDuration = checkHistory.Sum(x => x.Plan.Days);
                        PreviousPlanCost = checkHistory.Sum(x => x.Plan.Cost);
                    }
                    else
                    {
                        if (d.Phases.Any(x => x.BaseOP.Contains(PreviousOP)))
                        {
                            var vl = d.Phases.Where(x => x.BaseOP.Contains(PreviousOP) && x.PlanSchedule.Start > Tools.DefaultDate);
                            if (vl.Any())
                            {
                                PreviousPsStart = vl.Min(x => x.PlanSchedule.Start);
                                PreviousPsFinish = vl.Max(x => x.PlanSchedule.Finish);

                                PreviousPlanDuration = vl.Sum(x => x.Plan.Days);
                                PreviousPlanCost = vl.Sum(x => x.Plan.Cost);
                            }
                        }
                    }

                    var basesops = wb.OPs == null ? new List<string>() : wb.OPs;


                    List<BizPlanActivityPhase> phases = new List<BizPlanActivityPhase>();
                    if (basesops.Count() == 1 && basesops.FirstOrDefault().Trim().Equals(""))
                    {
                        phases = d.Phases;
                    }
                    else if (basesops.Count() == 0)
                    {
                        phases = d.Phases;
                    }
                    else
                    {
                        phases = d.Phases.Where(x => basesops.ToList().Contains(x.Estimate.SaveToOP)).ToList();
                    }

                    if (phases.Any())
                    {
                        foreach (var p in phases.Where(x=>x.ActivityType.Equals(datax.ActivityType)))
                        {
                            //check roles
                            var check = WebTools.CheckAccessibleWellAndActivity(accessibleWells, d.WellName, p.ActivityType);
                            if (check)
                            {
                                var OPEstimateCurr = new List<object>();
                                var OPEstimatePrev = new List<object>();
                                var OPEstimate = new List<object>();
                                int i = 0;

                                var isHaveWeeklyReport = WellActivity.isHaveWeeklyReport(d.WellName, d.UARigSequenceId, p.ActivityType);

                                if (!isHaveWeeklyReport)
                                {

                                    p.Estimate.EstimatePeriod.Start = Tools.ToUTC(p.Estimate.EstimatePeriod.Start, true);
                                    p.Estimate.EstimatePeriod.Finish = Tools.ToUTC(p.Estimate.EstimatePeriod.Finish);
                                    p.Estimate.EventStartDate = Tools.ToUTC(p.Estimate.EventStartDate, true);
                                    p.Estimate.EventEndDate = Tools.ToUTC(p.Estimate.EventEndDate);

                                    var Macro = MacroEconomic.Get<MacroEconomic>(
                                    Query.And(
                                    Query.EQ("Currency", d.Currency == null ? "" : d.Currency),
                                    Query.EQ("BaseOP", p.Estimate.SaveToOP == null ? "" : p.Estimate.SaveToOP),
                                    Query.EQ("Country", p.Estimate.Country == null ? "" : d.Country)
                                        )
                                    );
                                    var rate = 0;
                                    if (Macro == null)
                                    {
                                        rate = 0;
                                    }
                                    else
                                    {
                                        var getRate = Macro.ExchangeRate.AnnualValues.Where(x => x.Year == p.Estimate.EstimatePeriod.Start.Year)
                                        .Any() ? Macro.ExchangeRate.AnnualValues.Where(x => x.Year == p.Estimate.EstimatePeriod.Start.Year).FirstOrDefault().Value : 0;
                                        rate = Convert.ToInt32(getRate);
                                    }

                                    BizPlanSummary bisCal = new BizPlanSummary(d.WellName, p.Estimate.RigName, p.ActivityType, d.Country == null ? "" : d.Country == null ? "" : d.Country,
                                    d.ShellShare, p.Estimate.EstimatePeriod, p.Estimate.MaturityLevel == null ? 0 : Convert.ToInt32(p.Estimate.MaturityLevel.Replace("Type", "")),
                                    p.Estimate.Services, p.Estimate.Materials,
                                    p.Estimate.TroubleFreeBeforeLC.Days,
                                    d.ReferenceFactorModel == null ? "" : d.ReferenceFactorModel,
                                    Tools.Div(p.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentDays, 100),
                                    Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100),
                                    p.Estimate.LongLeadMonthRequired, Tools.Div(p.Estimate.PercOfMaterialsLongLead, 100), baseOP: p.Estimate.SaveToOP == null ? "" : p.Estimate.SaveToOP, isGetExcRateByCurrency: true,
                                    lcf: p.Estimate.NewLearningCurveFactor
                                    );

                                    bisCal.GeneratePeriodBucket();
                                    //set object
                                    Excelbusplan eb = new Excelbusplan();
                                    eb.Status = p.Estimate.Status;
                                    eb.LineOfBusiness = d.LineOfBusiness;
                                    eb.RigType = d.RigType;
                                    eb.Region = d.Region;
                                    eb.Country = d.Country == null ? "" : d.Country;
                                    eb.OperatingUnit = d.OperatingUnit;
                                    eb.PerformanceUnit = d.PerformanceUnit;
                                    eb.AssetName = d.AssetName;
                                    eb.ProjectName = d.ProjectName;
                                    eb.RigName = p.Estimate.RigName;
                                    eb.WellName = d.WellName;
                                    eb.ActivityType = p.ActivityType;
                                    eb.RigSeqId = d.UARigSequenceId;
                                    eb.Currency = d.Currency == null ? "" : d.Currency;
                                    eb.ShellShare = d.ShellShare;
                                    eb.SaveToOP = p.Estimate.SaveToOP == null ? "" : p.Estimate.SaveToOP;
                                    //eb.LastSubmitted = d.LastUpdate;
                                    eb.LastSubmitted = p.Estimate.LastUpdate;// d.LastUpdate;
                                    eb.DataInputBy = p.Estimate.LastUpdateBy;
                                    //eb.DataInputBy = p.Estimate.LastUpdateBy;// d.DataInputBy;


                                    eb.InPlan = d.isInPlan ? true : false;
                                    //eb.RFM = d.ReferenceFactorModel;
                                    eb.RFM = d.ReferenceFactorModel == "default" ? p.PhaseInfo.ReferenceFactorModel : d.ReferenceFactorModel;
                                    eb.FundingType = p.FundingType;
                                    eb.Event = new DateRange() { Start = p.Estimate.EventStartDate, Finish = p.Estimate.EventStartDate.Date.AddDays(bisCal.MeanTime) };
                                    eb.EventDays = Math.Round(bisCal.MeanTime, 2);
                                    eb.RigRate = Math.Round(p.Estimate.RigRate);
                                    eb.Services = bisCal.Services; // p.Estimate.Services;
                                    eb.Materials = bisCal.Material; //p.Estimate.Materials;
                                    eb.isMaterialLLSetManually = p.Estimate.isMaterialLLSetManually ? true : false;
                                    eb.PercOfMaterialsLongLead = Tools.Div(p.Estimate.PercOfMaterialsLongLead, 100);
                                    eb.LLRequiredMonth = p.Estimate.LongLeadMonthRequired;
                                    eb.LLCalc = p.Estimate.LongLeadCalc;
                                    eb.BurnRate = bisCal.BurnRate; // p.Estimate.BurnRate;
                                    eb.SpreadRate = bisCal.SpreadRateWRig; // p.Estimate.SpreadRate;
                                    eb.SpreadRateTotal = bisCal.SpreadRateTotal;// p.Estimate.SpreadRateTotal;
                                    eb.IsUsingLCFfromRFM = p.Estimate.IsUsingLCFfromRFM;
                                    ////OP16
                                    eb.UseTAApproved = p.Estimate.IsUsingLCFfromRFM;
                                    eb.TroubleFree = new WellDrillData()
                                    {
                                        Days = bisCal.TroubleFree.Days,// p.Estimate.NewTroubleFree.Days,
                                        Cost = bisCal.TroubleFree.Cost //p.Estimate.NewTroubleFree.Cost
                                    };
                                    eb.LCFParameter = bisCal.LCF;// p.Estimate.NewLearningCurveFactor;
                                    eb.NPT = new WellDrillDataPercent()
                                    {
                                        Days = bisCal.NPT.Days,// p.Estimate.NewNPTTime.PercentDays == 0 ? 0 : p.Estimate.NewNPTTime.Days,
                                        Cost = bisCal.NPT.Cost,// p.Estimate.NewNPTTime.Cost,
                                        PercentTime = bisCal.NPTRate * 100, //p.Estimate.NewNPTTime.PercentDays,
                                        PercentCost = bisCal.NPTRateCost // Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100)
                                    };

                                    eb.LCF = new WellDrillData()
                                    {
                                        Days = bisCal.NewLCFValue.Days, //p.Estimate.NewLCFValue.Days,
                                        Cost = bisCal.NewLCFValue.Cost  //Math.Round(p.Estimate.NewLearningCurveFactor * (p.Estimate.NewTroubleFree.Cost + p.Estimate.NewNPTTime.Cost), 2)
                                    };
                                    eb.Base = new WellDrillData()
                                    {
                                        Days = bisCal.NewBaseValue.Days, //Math.Round(p.Estimate.NewBaseValue.Days, 2),
                                        Cost = bisCal.NewBaseValue.Cost  //bisCal.NewBaseValue.Cost // p.Estimate.NewTroubleFreeUSD + p.Estimate.NPTCostUSD
                                    };
                                    eb.BaseCalc = new WellDrillData()
                                    {
                                        Days = eb.Base.Days - eb.LCF.Days, //Math.Round(p.Estimate.NewBaseValue.Days - p.Estimate.NewLCFValue.Days, 2),
                                        Cost = eb.Base.Cost - eb.LCF.Cost,  //(p.Estimate.NewTroubleFreeUSD + p.Estimate.NPTCostUSD) - p.Estimate.NewLCFValue.Cost
                                    };
                                    eb.TECOP = new WellDrillDataPercent()
                                    {
                                        Days = bisCal.TECOP.Days, //Math.Round(p.Estimate.NewTECOPTime.PercentDays == 0 ? 0 : p.Estimate.NewTECOPTime.Days, 2),
                                        Cost = bisCal.TECOP.Cost,// p.Estimate.NewTECOPTime.Cost,
                                        PercentTime = bisCal.TECOPRate * 100,//  p.Estimate.NewTECOPTime.PercentDays,
                                        PercentCost = bisCal.TECOPRateCost //Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100)
                                    };

                                    eb.Mean = new WellDrillData()
                                    {
                                        Days = bisCal.MeanTime,//Math.Round(p.Estimate.NewMean.Days, 2),
                                        Cost = bisCal.MeanCostEDM,// p.Estimate.NewMean.Cost
                                    };
                                    //convert to usd
                                    eb.TroubleFreeCostUSD = bisCal.TroubleFreeCostUSD;// p.Estimate.NewTroubleFreeUSD;
                                    eb.NptCostUSD = bisCal.NPTCostUSD; //p.Estimate.NPTCostUSD;
                                    eb.TecopCostUSD = bisCal.TECOPCostUSD; //p.Estimate.TECOPCostUSD;
                                    eb.MeanCostUSD = bisCal.MeanCostEDMUSD; //p.Estimate.MeanUSD;


                                    eb.TroubleFreeCost = bisCal.TroubleFree.Cost; //p.Estimate.NewTroubleFree.Cost;
                                    eb.NptCost = bisCal.NPT.Cost; //p.Estimate.NewNPTTime.Cost;
                                    eb.TecopCost = bisCal.TECOP.Cost;//p.Estimate.NewTECOPTime.Cost;
                                    eb.MeanCost = bisCal.MeanCostEDM;//p.Estimate.NewMean.Cost;
                                    #region WVA

                                    eb.MaturityLevel = p.Estimate.MaturityLevel;
                                    ////calc TQ BIC
                                    var _TQGapTotalDays = Math.Round(Math.Round(p.Estimate.NewMean.Days, 2) - p.Estimate.TQValueDriver, 2);
                                    var _TQGapDry = Math.Round(p.Estimate.DryHoleDays, 2) - p.Estimate.TQValueDriver;
                                    var _BICGapDry = Math.Round(p.Estimate.DryHoleDays, 2) - p.Estimate.BICValueDriver;
                                    var _TQGapCost = Math.Round(p.Estimate.NewMean.Cost, 2) - (p.Estimate.TQValueDriver * 1000000);
                                    var _BICGapCost = Math.Round(p.Estimate.NewMean.Cost, 2) - (p.Estimate.BICValueDriver * 1000000);
                                    var _BICGapTotalDays = Math.Round(Math.Round(p.Estimate.NewMean.Days, 2) - p.Estimate.BICValueDriver, 2);
                                    ////Wells Value Driver Total Days
                                    var TQGapCostCalc = Math.Round(Tools.Div(_TQGapTotalDays, rate) * Math.Round(p.Estimate.SpreadRateTotal, 2), 2);
                                    var TQTotalCost = Math.Round(Tools.Div(Math.Round(p.Estimate.NewMean.Cost, 2), rate) - TQGapCostCalc, 2);
                                    var BICGapCostCalc = Math.Round(Tools.Div(_BICGapTotalDays, rate) * Math.Round(p.Estimate.SpreadRateTotal, 2), 2);
                                    var BICTotalCost = Math.Round(Tools.Div(Math.Round(p.Estimate.NewMean.Cost, 2), rate) - BICGapCostCalc, 2);
                                    var TQGapDry = Math.Round(Tools.Div(_TQGapDry, rate) * Math.Round(p.Estimate.SpreadRateTotal, 2), 2);
                                    var TQGapCost = p.Estimate.NewMean.Cost - (p.Estimate.TQValueDriver * 1000000);
                                    var BICGapDry = Math.Round(Tools.Div(_BICGapDry, rate) * Math.Round(p.Estimate.SpreadRateTotal, 2), 2);
                                    var WellValueDriver = p.Estimate.WellValueDriver == null || p.Estimate.WellValueDriver == "" ? "" : p.Estimate.WellValueDriver;
                                    eb.WellValueDriver = WellValueDriver;
                                    eb.WVATotalDays = new WVA();
                                    eb.WVATotalCost = new WVA();
                                    eb.WVADryHoleDays = new WVA();
                                    if (WellValueDriver.Equals("Total Days"))
                                    {
                                        eb.MeanWVA = new WellDrillData()
                                        {
                                            Days = Math.Round(p.Estimate.NewMean.Days, 2)
                                        };
                                        eb.WVATotalDays = new WVA()
                                        {
                                            TQValueDriver = new WellDrillData()
                                            {
                                                Days = Math.Round(p.Estimate.TQValueDriver, 2),
                                                Cost = TQTotalCost
                                            },
                                            TQGap = new WellDrillData()
                                            {
                                                Days = Math.Round(_TQGapTotalDays, 2),
                                                Cost = TQGapCostCalc
                                            },
                                            BICValueDriver = new WellDrillData()
                                            {
                                                Days = p.Estimate.BICValueDriver,
                                                Cost = BICTotalCost
                                            },
                                            BICGap = new WellDrillData()
                                            {
                                                Days = _BICGapTotalDays,
                                                Cost = BICGapCostCalc
                                            }

                                        };
                                    }
                                    else if (WellValueDriver.Equals("Cost"))
                                    {
                                        eb.MeanWVA = new WellDrillData()
                                        {
                                            Cost = p.Estimate.NewMean.Cost
                                        };
                                        eb.WVATotalCost = new WVA()
                                        {
                                            TQValueDriver = new WellDrillData()
                                            {
                                                Cost = p.Estimate.TQValueDriver
                                            },
                                            TQGap = new WellDrillData()
                                            {
                                                Cost = _TQGapCost
                                            },
                                            BICValueDriver = new WellDrillData()
                                            {
                                                Cost = p.Estimate.BICValueDriver
                                            },
                                            BICGap = new WellDrillData()
                                            {
                                                Cost = _BICGapCost
                                            }
                                        };
                                    }
                                    else if (WellValueDriver.Equals("Dry Hole Days"))
                                    {
                                        eb.DryHoleDays = p.Estimate.DryHoleDays;
                                        eb.WVADryHoleDays = new WVA()
                                        {
                                            TQValueDriver = new WellDrillData()
                                            {
                                                Days = Math.Round(p.Estimate.TQValueDriver, 2)
                                            },
                                            TQGap = new WellDrillData()
                                            {
                                                Days = _TQGapDry
                                            },
                                            BICValueDriver = new WellDrillData()
                                            {
                                                //Days = _BICGapDry
                                                Days = Math.Round(p.Estimate.BICValueDriver, 2)
                                            },
                                            BICGap = new WellDrillData()
                                            {
                                                Cost = BICGapDry
                                            }

                                        };
                                    }
                                    #endregion
                                    ////Wells Value Driver Not Selected                                    
                                    eb.MeanCostEDM = bisCal.MeanCostEDM;// p.Allocation == null ? 0.0 : Math.Round(p.Allocation.AnnualyBuckets.Sum(x => x.MeanCostEDM), 2); // Mean Cost EDM
                                    eb.MeanCostRealTerm = bisCal.MeanCostRealTerm;// p.Allocation == null ? 0.0 : Math.Round(p.Allocation.AnnualyBuckets.Sum(x => x.MeanCostRealTerm), 2);
                                    //eb.MeanCostMOD = bisCal.MeanCostMOD;
                                    eb.MeanCostMOD = bisCal.MeanCostMOD;// p.Allocation == null ? 0.0 : Math.Round(p.Allocation.AnnualyBuckets.Sum(x => x.MeanCostMOD), 2);
                                    eb.ShellShareCost = bisCal.ShellShareCost;// p.Allocation == null ? 0.0 : Math.Round(p.Allocation.AnnualyBuckets.Sum(x => x.ShellShare), 2);
                                    //eb.ShellShareCost = bisCal.ShellShareCost;
                                    ////Perfom
                                    eb.PerformanceScore = p.Estimate.PerformanceScore;
                                    eb.WaterDepth = p.Estimate.WaterDepth;
                                    eb.WellDepth = p.Estimate.WellDepth;
                                    eb.NumberOfCasings = p.Estimate.NumberOfCasings;
                                    eb.MechanicalRiskIndex = p.Estimate.MechanicalRiskIndex;

                                    //add new 


                                    eb.EscalationRig = bisCal.MonthlyBuckets == null ? 0 : bisCal.MonthlyBuckets.Sum(x => x.EscCostRig);
                                    eb.EscalationService = bisCal.MonthlyBuckets == null ? 0 : bisCal.MonthlyBuckets.Sum(x => x.EscCostServices);
                                    eb.EscalationMaterials = bisCal.MonthlyBuckets == null ? 0 : bisCal.MonthlyBuckets.Sum(x => x.EscCostMaterial);
                                    eb.EscalationCost = bisCal.MonthlyBuckets == null ? 0 : bisCal.MonthlyBuckets.Sum(x => x.EscCostTotal);
                                    eb.CSOCost = bisCal.MonthlyBuckets == null ? 0 : bisCal.MonthlyBuckets.Sum(x => x.CSOCost);
                                    eb.InflationCost = bisCal.MonthlyBuckets == null ? 0 : bisCal.MonthlyBuckets.Sum(x => x.InflationCost);
                                    edetail.Add(eb);
                                }
                            }
                        }
                    }
                }
            }

            return edetail;
        }

        public JsonResult ExportDetail(WaterfallBase wb)
        {
            string PreviousOP = "";
            string NextOP = "";
            string DefaultOP = getBaseOP(out PreviousOP, out NextOP);

            var division = 1000000.0;
            var now = Tools.ToUTC(DateTime.Now);
            var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
            var email = WebTools.LoginUser.Email;
            wb.PeriodBase = "By Last Sequence";
            var raw = wb.GetActivitiesForBizPlanForFiscalYear(email);
            var accessibleWells = WebTools.GetWellActivities();
            var OPstate = GetMasterOP();

            var MasterActivities = ActivityMaster.Populate<ActivityMaster>();
            var lastDateExecutedLS = WellActivity.GetLastExecuteDateLS();
            var logLast = DataHelper.Populate("LogLatestLS", Query.And(Query.EQ("Executed_At", Tools.ToUTC(lastDateExecutedLS))));
            int startRow = 4; int IndexSheet = 1; int SetFormat = 3;
            //string xlst = Path.Combine(HttpRuntime.AppDomainAppPath + "App_Data\\Temp", "Template_Bisplan_Cals.xlsx");
            string xlst = Path.Combine(HttpRuntime.AppDomainAppPath + "App_Data\\Temp", "Template_Bisplan_Cals.xlsx");
            Workbook wbook = new Workbook(xlst);
            #region Define_Header //wil be used for next

            //wb.Worksheets[IndexSheet].Cells["A" + 0].Value = "Line Of Business";
            //wb.Worksheets[IndexSheet].Cells["B" + 0].Value = "Region";
            //wb.Worksheets[IndexSheet].Cells["C" + 0].Value = "Rig Name";
            //wb.Worksheets[IndexSheet].Cells["D" + 0].Value = "Project Name";
            //wb.Worksheets[IndexSheet].Cells["E" + 0].Value = "OP Start";
            //wb.Worksheets[IndexSheet].Cells["F" + 0].Value = "OP Finish";
            //wb.Worksheets[IndexSheet].Cells["G" + 0].Value = "Ecs Cost Rig";
            //wb.Worksheets[IndexSheet].Cells["H" + 0].Value = "Ecs Cost Services";
            //wb.Worksheets[IndexSheet].Cells["I" + 0].Value = "Ecs Cost Materials";
            //wb.Worksheets[IndexSheet].Cells["J" + 0].Value = "Ecs Cost Total";
            //wb.Worksheets[IndexSheet].Cells["K" + 0].Value = "CSO Cost";
            //wb.Worksheets[IndexSheet].Cells["L" + 0].Value = "Inflation Cost";

            //wb.Worksheets[IndexSheet].Cells["M" + 0].Value = "Mean Cost EDM";
            //wb.Worksheets[IndexSheet].Cells["N" + 0].Value = "Mean Cost EDM With Shell Share";

            //wb.Worksheets[IndexSheet].Cells["O" + 0].Value = "Mean Cost Real Term";
            //wb.Worksheets[IndexSheet].Cells["P" + 0].Value = "Mean Cost Real Term With Shell Share";

            //wb.Worksheets[IndexSheet].Cells["Q" + 0].Value = "Mean Cost MOD";
            //wb.Worksheets[IndexSheet].Cells["R" + 0].Value = "Mean Cost MOD With Shell Share";

            //wb.Worksheets[IndexSheet].Cells.Merge(0, 0, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 1, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 2, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 3, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 4, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 5, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 6, 2, 1);

            //wb.Worksheets[IndexSheet].Cells.Merge(0, 7, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 8, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 9, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 10, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 11, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 12, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 13, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 14, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 15, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 16, 2, 1);
            //wb.Worksheets[IndexSheet].Cells.Merge(0, 17, 2, 1);
            #endregion
            var Messages = "";
            var RFMChecking = new List<object>();
            try
            {
                if (raw != null && raw.Count > 0)
                {
                    //eky's note : function to generate List<ExcelBusplan> is moved to a function GenerateExportDetailData
                    //because it will be used too in checker batch to check the MOD diff between BPIT and Export Detail
                    List<Excelbusplan> edetail = GenerateExportDetailData(raw, PreviousOP, accessibleWells, wb);

                    Worksheet ws = wbook.Worksheets[0];
                    Cells cells = ws.Cells;
                    cells.InsertRows(startRow, edetail.Count);
                    foreach (var r in edetail)
                    {
                        #region filter busplan have no rfm

                        var checkingBisplan = raw.FirstOrDefault(x => x.WellName == r.WellName && x.UARigSequenceId == r.RigSeqId);
                        if (checkingBisplan != null)
                        {
                            var phase = checkingBisplan.Phases.FirstOrDefault(x => x.ActivityType == r.ActivityType);
                            if (phase != null)
                            {
                                if (String.IsNullOrEmpty(checkingBisplan.ReferenceFactorModel) || checkingBisplan.ReferenceFactorModel.ToLower() == "default")
                                {
                                    var t = new
                                    {
                                        WellName = r.WellName,
                                        UARigSequenceId = r.RigSeqId,
                                        ActivityType = r.ActivityType,
                                        r.RigName,
                                        Project = r.ProjectName,
                                        r.Country,
                                        ReferenceFactorModel = checkingBisplan.ReferenceFactorModel

                                    };
                                    RFMChecking.Add(t);
                                }
                            }

                        }

                        #endregion

                        var dataAnyLS = logLast.Where(x => x.GetString("Well_Name") == r.WellName && x.GetString("Activity_Type") == r.ActivityType && x.GetString("Rig_Name") == r.RigName);

                        if (wb.inlastuploadlsBoth != null || wb.inlastuploadlsBoth != "")
                        {
                            if (wb.inlastuploadls == true)
                            {
                                if (dataAnyLS.Any())
                                {
                                    r.LSStartDate = dataAnyLS.FirstOrDefault().GetDateTime("Start_Date");
                                    r.latestLS = "Yes";
                                }
                            }
                            else if (wb.inlastuploadls == false)
                            {
                                var checkAnyLS = logLast.Any(x =>
                                        x.GetString("Well_Name") != r.WellName &&
                                        x.GetString("Activity_Type") != r.ActivityType &&
                                        x.GetString("Rig_Name") != r.RigName
                                   );
                                if (checkAnyLS || !logLast.Any())
                                {
                                    r.LSStartDate = new DateTime(1900, 1, 1);
                                    r.latestLS = "No";
                                }
                            }

                        }
                        else
                        {
                            if (dataAnyLS.Any())
                            {
                                r.LSStartDate = dataAnyLS.FirstOrDefault().GetDateTime("Start_Date");
                                r.latestLS = "Yes";
                            }

                            else
                            {
                                r.LSStartDate = new DateTime(1900, 1, 1);
                                r.latestLS = "No";
                            }
                        }
                        cells["A" + startRow.ToString()].PutValue(r.Status);
                        cells["B" + startRow.ToString()].PutValue(r.LineOfBusiness);
                        cells["C" + startRow.ToString()].PutValue(r.RigType);
                        cells["D" + startRow.ToString()].PutValue(r.Region);
                        cells["E" + startRow.ToString()].PutValue(r.Country == null ? "" : r.Country);
                        cells["F" + startRow.ToString()].PutValue(r.OperatingUnit);
                        cells["G" + startRow.ToString()].PutValue(r.PerformanceUnit);
                        cells["H" + startRow.ToString()].PutValue(r.AssetName);
                        cells["I" + startRow.ToString()].PutValue(r.ProjectName);
                        cells["J" + startRow.ToString()].PutValue(r.RigName);
                        cells["K" + startRow.ToString()].PutValue(r.WellName);
                        cells["L" + startRow.ToString()].PutValue(r.ActivityType);
                        cells["M" + startRow.ToString()].PutValue(r.RigSeqId);
                        cells["N" + startRow.ToString()].PutValue(r.Currency == null ? "" : r.Currency);
                        cells["O" + startRow.ToString()].PutValue(Tools.Div(r.ShellShare, 100));
                        cells["P" + startRow.ToString()].PutValue(r.DataInputBy);
                        cells["Q" + startRow.ToString()].PutValue(r.SaveToOP == null ? "" : r.SaveToOP);
                        Style styleStart = cells["W" + startRow.ToString()].GetStyle();
                        styleStart.Custom = "m/d/yyyy";
                        cells["R" + startRow.ToString()].PutValue(r.LastSubmitted.ToString("MM/dd/yyyy"));

                        cells["S" + startRow.ToString()].PutValue(r.InPlan ? "Yes" : "No");
                        //ls ingo lsInfo
                        cells["T" + startRow.ToString()].PutValue(r.latestLS);

                        if (r.latestLS == "Yes")
                            cells["U" + startRow.ToString()].PutValue(r.LSStartDate);
                        else
                            cells["U" + startRow.ToString()].PutValue(new DateTime(1900, 1, 1));
                        //ref model
                        cells["V" + startRow.ToString()].PutValue(r.RFM);
                        cells["W" + startRow.ToString()].PutValue(r.FundingType);

                        //Uni
                        cells["X" + startRow.ToString()].PutValue(r.Event.Start);
                        cells["Y" + startRow.ToString()].PutValue(r.Event.Finish);
                        cells["Z" + startRow.ToString()].PutValue(Math.Round(r.EventDays, 2));

                        cells["X" + startRow.ToString()].SetStyle(styleStart);
                        cells["Y" + startRow.ToString()].SetStyle(styleStart);
                        cells["S" + startRow.ToString()].SetStyle(styleStart);

                        cells["AA" + startRow.ToString()].PutValue(Math.Round(r.RigRate));
                        cells["AB" + startRow.ToString()].PutValue(r.Services);
                        cells["AC" + startRow.ToString()].PutValue(r.Materials);
                        cells["AD" + startRow.ToString()].PutValue(r.isMaterialLLSetManually ? "Yes" : "No");
                        cells["AE" + startRow.ToString()].PutValue(r.PercOfMaterialsLongLead);
                        cells["AF" + startRow.ToString()].PutValue(r.LLRequiredMonth);
                        cells["AG" + startRow.ToString()].PutValue(r.LLCalc);
                        cells["AH" + startRow.ToString()].PutValue(r.SpreadRate);
                        cells["AI" + startRow.ToString()].PutValue(r.BurnRate);
                        cells["AJ" + startRow.ToString()].PutValue(r.SpreadRateTotal);
                        //OP16 Days

                        cells["AK" + startRow.ToString()].PutValue(r.IsUsingLCFfromRFM ? "Yes" : "No");
                        cells["AL" + startRow.ToString()].PutValue(r.TroubleFree.Days);
                        cells["AM" + startRow.ToString()].PutValue(r.UseTAApproved ? "Yes" : "No");
                        cells["AN" + startRow.ToString()].PutValue(r.LCFParameter);
                        cells["AO" + startRow.ToString()].PutValue(Tools.Div(r.NPT.PercentTime, 100));
                        cells["AP" + startRow.ToString()].PutValue(Tools.Div(r.TECOP.PercentTime, 100));
                        cells["AQ" + startRow.ToString()].PutValue(r.NPT.Days);
                        cells["AR" + startRow.ToString()].PutValue(r.LCF.Days);
                        cells["AS" + startRow.ToString()].PutValue(Math.Round(r.Base.Days, 2));
                        cells["AT" + startRow.ToString()].PutValue(Math.Round(r.Base.Days - r.LCF.Days, 2));
                        cells["AU" + startRow.ToString()].PutValue(Math.Round(r.TECOP.Days, 2));
                        cells["AV" + startRow.ToString()].PutValue(Math.Round(r.Mean.Days, 2));
                        //Cost Estimate p16    B



                        cells["AW" + startRow.ToString()].PutValue(r.TroubleFreeCost);
                        cells["AX" + startRow.ToString()].PutValue(r.NPT.PercentCost);
                        cells["AY" + startRow.ToString()].PutValue(r.TECOP.PercentCost);
                        cells["AZ" + startRow.ToString()].PutValue(r.NPT.Cost);
                        cells["BA" + startRow.ToString()].PutValue(r.LCF.Cost);
                        cells["BB" + startRow.ToString()].PutValue(r.Base.Cost);
                        cells["BC" + startRow.ToString()].PutValue(r.BaseCalc.Cost);
                        cells["BD" + startRow.ToString()].PutValue(r.TECOP.Cost);
                        //cells["BA" + startRow.ToString()].PutValue(r.MeanCost);
                        cells["BE" + startRow.ToString()].PutValue(r.MeanCostEDM);
                        //MeanCostEDM
                        //CONVERSIBN TO USD
                        cells["BF" + startRow.ToString()].PutValue(r.TroubleFreeCostUSD);
                        cells["BG" + startRow.ToString()].PutValue(r.NptCostUSD);
                        cells["BH" + startRow.ToString()].PutValue(r.TecopCostUSD);
                        //cells["BE" + startRow.ToString()].PutValue(r.MeanCostUSD);
                        cells["BI" + startRow.ToString()].PutValue(r.MeanCostEDM);
                        //level   B
                        cells["BJ" + startRow.ToString()].PutValue(r.MaturityLevel);
                        //well value driver Days
                        cells["BK" + startRow.ToString()].PutValue(r.WellValueDriver);
                        cells["BL" + startRow.ToString()].PutValue(r.MeanWVA.Days); //EDM DAYS
                        cells["BM" + startRow.ToString()].PutValue(r.WVATotalDays.TQValueDriver.Days); //TQ For Value Driver
                        cells["BN" + startRow.ToString()].PutValue(r.WVATotalDays.TQGap.Days); // TQ GAP ok
                        cells["BO" + startRow.ToString()].PutValue(r.WVATotalDays.TQGap.Cost); //TQ Gap Cost (calc)  
                        cells["BP" + startRow.ToString()].PutValue(r.WVATotalDays.TQValueDriver.Cost); //TQ Total Cost (calc) (USD)
                        cells["BQ" + startRow.ToString()].PutValue(r.WVATotalDays.BICValueDriver.Days);
                        cells["BR" + startRow.ToString()].PutValue(r.WVATotalDays.BICGap.Days); // BIC Gap
                        cells["BS" + startRow.ToString()].PutValue(r.WVATotalDays.BICGap.Cost);
                        cells["BT" + startRow.ToString()].PutValue(r.WVATotalDays.BICValueDriver.Cost);
                        //Dry Hole Days
                        cells["BU" + startRow.ToString()].PutValue(r.DryHoleDays);
                        cells["BV" + startRow.ToString()].PutValue(r.WVADryHoleDays.TQValueDriver.Days); // TQ For Value Driver
                        cells["BW" + startRow.ToString()].PutValue(r.WVADryHoleDays.TQGap.Days); //TQ GAP
                        cells["BX" + startRow.ToString()].PutValue(r.WVADryHoleDays.BICValueDriver.Days);
                        cells["BY" + startRow.ToString()].PutValue(r.WVADryHoleDays.BICGap.Cost);
                        //Well value Cost
                        cells["BZ" + startRow.ToString()].PutValue(r.MeanWVA.Cost); // EDM COST
                        cells["CA" + startRow.ToString()].PutValue(r.WVATotalCost.TQValueDriver.Cost);//DRIVER
                        cells["CB" + startRow.ToString()].PutValue(r.WVATotalCost.TQGap.Cost); // GAP
                        //bic
                        cells["CC" + startRow.ToString()].PutValue(r.WVATotalCost.BICValueDriver.Cost);
                        cells["CD" + startRow.ToString()].PutValue(r.WVATotalCost.BICGap.Cost);

                        //Perfom
                        cells["CE" + startRow.ToString()].PutValue(r.PerformanceScore);
                        cells["CF" + startRow.ToString()].PutValue(r.WaterDepth);
                        cells["CG" + startRow.ToString()].PutValue(r.WellDepth);
                        cells["CH" + startRow.ToString()].PutValue(r.NumberOfCasings);
                        cells["CI" + startRow.ToString()].PutValue(r.MechanicalRiskIndex);
                        //Escalation
                        cells["CJ" + startRow.ToString()].PutValue(r.EscalationRig);
                        cells["CK" + startRow.ToString()].PutValue(r.EscalationService);
                        cells["CL" + startRow.ToString()].PutValue(r.EscalationMaterials);
                        cells["CM" + startRow.ToString()].PutValue(r.EscalationCost);
                        cells["CN" + startRow.ToString()].PutValue(r.CSOCost);
                        cells["CO" + startRow.ToString()].PutValue(r.InflationCost);
                        //EVENT SUMMARY                      PutValue(
                        cells["CP" + startRow.ToString()].PutValue(r.MeanCostEDM);
                        cells["CQ" + startRow.ToString()].PutValue(r.MeanCostRealTerm);
                        cells["CR" + startRow.ToString()].PutValue(r.MeanCostMOD);
                        cells["CS" + startRow.ToString()].PutValue(r.ShellShareCost);

                        ////Text Align Right
                        cells["O" + SetFormat].SetStyle(new Style() { Number = 10 });
                        cells["R" + SetFormat].SetStyle(new Style() { Number = 14, HorizontalAlignment = TextAlignmentType.Right });
                        cells["U" + SetFormat].SetStyle(new Style() { Number = 14, HorizontalAlignment = TextAlignmentType.Right }); 
                        cells["Z"+SetFormat].SetStyle(new Style() { Number = 4 });                        
                        cells["AA"+SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AB"+SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AC"+SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AE"+SetFormat].SetStyle(new Style() { Number = 10 });
                        cells["AF"+SetFormat].SetStyle(new Style() { Number = 1 });
                        cells["AG"+SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AH"+SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AI"+SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AJ"+SetFormat].SetStyle(new Style() { Number = 4 });
                        //cells["AK" + SetFormat].SetStyle(new Style() { Number = 5 });
                        cells["AN" + SetFormat].SetStyle(new Style() { Custom = "0.00000" });
                        cells["AO" + SetFormat].SetStyle(new Style() { Number = 10 });
                        cells["AP" + SetFormat].SetStyle(new Style() { Number = 10 });
                        cells["AQ" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["AR" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["AS" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AT" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["AU" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["AV" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AW" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["AX" + SetFormat].SetStyle(new Style() { Number = 10 });
                        cells["AY" + SetFormat].SetStyle(new Style() { Number = 10 });
                        cells["AZ" + SetFormat].SetStyle(new Style() { Number = 4 });
                        
                        cells["BA" + SetFormat].SetStyle(new Style() { Number = 4 }); 
                        cells["BB" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BC" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BD" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BE" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BF" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BG" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BH" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BI" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BJ" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["BK" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["BL" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["BM" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["BN" + SetFormat].SetStyle(new Style() { Number = 4 });

                        cells["BO" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BP" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BQ" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["BR" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BS" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BT" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BU" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BV" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["BW" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["BX" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["BY" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["BZ" + SetFormat].SetStyle(new Style() { Number = 4 });

                        cells["CA" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CB" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CC" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CD" + SetFormat].SetStyle(new Style() { Number = 4 });
                        //cells["CD" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["CF" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["CG" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CH" + SetFormat].SetStyle(new Style() { Number = 2 });
                        cells["CI" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CJ" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CK" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CL" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CM" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CN" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CO" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CP" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CQ" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CR" + SetFormat].SetStyle(new Style() { Number = 4 });
                        cells["CS" + SetFormat].SetStyle(new Style() { Number = 4 });
                        startRow++; SetFormat++;
                    }
                    wbook.Worksheets[0].AutoFitColumns();
                }
            }
            //catch (InvalidCastException e)
            catch (InvalidCastException e)
            {
                var cda = Messages;
                throw;
            }


            var newFileNameSingle = Path.Combine(HttpRuntime.AppDomainAppPath + "App_Data\\Temp",
                     String.Format("Business Plan Detail-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("yyyy-MM-dd-hhmmss")));//Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss")));

            //string returnName = String.Format("Business Plan Detail-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss"));
            string returnName = String.Format("Business Plan Detail-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("yyyy-MM-dd-hhmmss"));

            wbook.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            return Json(new { Success = true, Path = returnName, RFMChecking }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ExportCatalog(WaterfallBase wbs)
        {
            var division = 1000000.0;
            var now = Tools.ToUTC(DateTime.Now);
            var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
            var email = WebTools.LoginUser.Email;
            var raw = wbs.GetActivitiesForBizPlan(email);
            var accessibleWells = WebTools.GetWellActivities();
            string previousOP = "";
            string nextOP = "";
            var datas = new List<object>();

            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "BizplanExportTemplateCatalog.xlsx");

            var MasterActivities = ActivityMaster.Populate<ActivityMaster>();

            Workbook wb = new Workbook(xlst);
            var ws = wb.Worksheets[0];
            int startRow = 2;

            if (raw != null && raw.Count > 0)
            {
                foreach (var d in raw)
                {
                    var PreviousPsStart = Tools.DefaultDate;
                    var PreviousPsFinish = Tools.DefaultDate;
                    var PsStart = Tools.DefaultDate;
                    var PsFinish = Tools.DefaultDate;

                    var PlanDuration = 0.0;
                    var PlanCost = 0.0;
                    var PreviousPlanDuration = 0.0;
                    var PreviousPlanCost = 0.0;

                    var checkHistory = d.OPHistories.Where(x => null != x.Type && x.Type.Equals(previousOP) && x.PlanSchedule.Start > Tools.DefaultDate && x.PlanSchedule.Finish > Tools.DefaultDate);
                    if (checkHistory.Any())
                    {
                        PreviousPsStart = checkHistory.Min(x => x.PlanSchedule.Start);
                        PreviousPsFinish = checkHistory.Max(x => x.PlanSchedule.Finish);

                        PreviousPlanDuration = checkHistory.Sum(x => x.Plan.Days);
                        PreviousPlanCost = checkHistory.Sum(x => x.Plan.Cost);
                    }
                    else
                    {
                        if (d.Phases.Any(x => x.BaseOP.Contains(previousOP)))
                        {
                            var vl = d.Phases.Where(x => x.BaseOP.Contains(previousOP) && x.PlanSchedule.Start > Tools.DefaultDate);
                            if (vl.Any())
                            {
                                PreviousPsStart = vl.Min(x => x.PlanSchedule.Start);
                                PreviousPsFinish = vl.Max(x => x.PlanSchedule.Finish);

                                PreviousPlanDuration = vl.Sum(x => x.Plan.Days);
                                PreviousPlanCost = vl.Sum(x => x.Plan.Cost);
                            }
                        }
                    }
                    //d.Phases.Where(x => x.Status != null && x.Status.Length > 0).ToList();
                    if (d.Phases.Any())
                    {
                        foreach (var p in d.Phases.Where(x => x.Estimate.Status != null && !x.Estimate.Status.ToString().Trim().Equals("")).ToList())
                        {
                            var check = WebTools.CheckAccessibleWellAndActivity(accessibleWells, d.WellName, p.ActivityType);
                            if (check)
                            {
                                ws.Cells["A" + startRow.ToString()].Value = p.Estimate.Status;
                                ws.Cells["B" + startRow.ToString()].Value = d.LineOfBusiness;
                                ws.Cells["C" + startRow.ToString()].Value = d.Region;
                                ws.Cells["D" + startRow.ToString()].Value = d.OperatingUnit;
                                ws.Cells["E" + startRow.ToString()].Value = d.PerformanceUnit;
                                ws.Cells["F" + startRow.ToString()].Value = d.AssetName;
                                ws.Cells["G" + startRow.ToString()].Value = d.ProjectName;
                                ws.Cells["H" + startRow.ToString()].Value = d.WellName;
                                ws.Cells["I" + startRow.ToString()].Value = p.ActivityType;
                                ws.Cells["J" + startRow.ToString()].Value = MasterActivities.Where(x => x._id.Equals(p.ActivityType)).FirstOrDefault() == null ? "" : MasterActivities.Where(x => x._id.Equals(p.ActivityType)).FirstOrDefault().ActivityCategory;
                                ws.Cells["K" + startRow.ToString()].Value = p.ActivityDesc; //scope desc
                                ws.Cells["L" + startRow.ToString()].Value = p.Estimate.SpreadRate;
                                ws.Cells["M" + startRow.ToString()].Value = p.Estimate.BurnRate;
                                ws.Cells["N" + startRow.ToString()].Value = ""; //MRI
                                ws.Cells["O" + startRow.ToString()].Value = ""; //Completion Type
                                ws.Cells["P" + startRow.ToString()].Value = p.Estimate.NumberOfCompletionZones;
                                ws.Cells["Q" + startRow.ToString()].Value = p.Estimate.BrineDensity;
                                ws.Cells["R" + startRow.ToString()].Value = ""; //est range tipe
                                ws.Cells["S" + startRow.ToString()].Value = ""; //Deterministic Low Range
                                ws.Cells["T" + startRow.ToString()].Value = ""; //Deterministic High
                                ws.Cells["U" + startRow.ToString()].Value = ""; //Probabilistic P10
                                ws.Cells["V" + startRow.ToString()].Value = ""; //Probabilistic P90
                                ws.Cells["W" + startRow.ToString()].Value = p.Estimate.WaterDepth;
                                ws.Cells["X" + startRow.ToString()].Value = p.Estimate.WellDepth;
                                ws.Cells["Y" + startRow.ToString()].Value = ""; //Learning Curve Factor
                                ws.Cells["Z" + startRow.ToString()].Value = d.ShellShare;
                                ws.Cells["AA" + startRow.ToString()].Value = d.FirmOrOption;
                                ws.Cells["AB" + startRow.ToString()].Value = p.Estimate.MaturityLevel;

                                ws.Cells["AC" + startRow.ToString()].Value = p.FundingType;
                                ws.Cells["AD" + startRow.ToString()].Value = d.ReferenceFactorModel;
                                ws.Cells["AE" + startRow.ToString()].Value = p.Estimate.RigName;
                                ws.Cells["AF" + startRow.ToString()].Value = d.RigType;
                                ws.Cells["AG" + startRow.ToString()].Value = d.UARigSequenceId;
                                ws.Cells["AH" + startRow.ToString()].Value = p.Estimate.EstimatePeriod.Start.ToString("dd-MMM-yyyy");
                                ws.Cells["AI" + startRow.ToString()].Value = p.Estimate.EstimatePeriod.Finish.ToString("dd-MMM-yyyy");
                                ws.Cells["AJ" + startRow.ToString()].Value = p.Estimate.NewTroubleFree.Days;
                                ws.Cells["AK" + startRow.ToString()].Value = p.Estimate.NewNPTTime.PercentDays;
                                ws.Cells["AL" + startRow.ToString()].Value = p.Estimate.NewTECOPTime.PercentDays;
                                ws.Cells["AM" + startRow.ToString()].Value = ""; //Override Time Factors
                                ws.Cells["AN" + startRow.ToString()].Value = p.Estimate.NewNPTTime.Days;
                                ws.Cells["AO" + startRow.ToString()].Value = p.Estimate.NewTECOPTime.Days;
                                ws.Cells["AP" + startRow.ToString()].Value = p.Estimate.NewMean.Days;
                                ws.Cells["AQ" + startRow.ToString()].Value = p.Estimate.NewTroubleFree.Cost;
                                ws.Cells["AR" + startRow.ToString()].Value = p.Estimate.Currency;
                                ws.Cells["AS" + startRow.ToString()].Value = p.Estimate.NewNPTTime.PercentCost;
                                ws.Cells["AT" + startRow.ToString()].Value = p.Estimate.NewTECOPTime.PercentCost;
                                ws.Cells["AU" + startRow.ToString()].Value = ""; //Override Cost Factors
                                ws.Cells["AV" + startRow.ToString()].Value = p.Estimate.NewNPTTime.Cost;
                                ws.Cells["AW" + startRow.ToString()].Value = p.Estimate.NewTECOPTime.Cost;
                                ws.Cells["AX" + startRow.ToString()].Value = p.Estimate.NewMean.Cost;
                                ws.Cells["AY" + startRow.ToString()].Value = p.Estimate.NewTroubleFree.Cost;
                                ws.Cells["AZ" + startRow.ToString()].Value = p.Estimate.NewNPTTime.Cost;
                                ws.Cells["BA" + startRow.ToString()].Value = p.Estimate.NewTECOPTime.Cost;

                                ws.Cells["B" + startRow.ToString()].Value = p.Estimate.NewMean.Cost;

                                ws.Cells["BC" + startRow.ToString()].Value = p.Allocation.AnnualyBuckets.Any() ? p.Allocation.AnnualyBuckets.Sum(x => x.EscCostTotal) : 0.0;
                                ws.Cells["BD" + startRow.ToString()].Value = p.Allocation.AnnualyBuckets.Any() ? p.Allocation.AnnualyBuckets.Sum(x => x.CSOCost) : 0.0;
                                ws.Cells["BE" + startRow.ToString()].Value = p.Allocation.AnnualyBuckets.Any() ? p.Allocation.AnnualyBuckets.Sum(x => x.InflationCost) : 0.0;
                                ws.Cells["BF" + startRow.ToString()].Value = p.Allocation.AnnualyBuckets.Any() ? p.Allocation.AnnualyBuckets.Sum(x => x.MeanCostMOD) : 0.0;
                                ws.Cells["BG" + startRow.ToString()].Value = p.Estimate.ProjectValueDriver;
                                ws.Cells["BH" + startRow.ToString()].Value = ""; //Value Driver Related Estimate
                                ws.Cells["BI" + startRow.ToString()].Value = p.Estimate.TQ.Threshold;
                                ws.Cells["BJ" + startRow.ToString()].Value = p.Estimate.BIC.Threshold;
                                ws.Cells["BK" + startRow.ToString()].Value = p.Estimate.TQ.Gap;
                                ws.Cells["BL" + startRow.ToString()].Value = p.Estimate.BIC.Gap;
                                ws.Cells["BM" + startRow.ToString()].Value = p.Estimate.PerformanceScore;

                                startRow++;
                            }
                        }
                    }
                }
            }

            var newFileNameSingle = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                     String.Format("Business Plan-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("yyyy-MM-dd-hhmmss")));

            string returnName = String.Format("Business Plan-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("yyyy-MM-dd-hhmmss"));

            wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            return Json(new { Success = true, Path = returnName }, JsonRequestBehavior.AllowGet);
        }

        public FileResult DownloadBusPlanFile(string stringName, DateTime date)
        {
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "BusinessPlanExportTemplate.xlsx");
            var res = Path.Combine(Server.MapPath("~/App_Data/Temp"), stringName);
            //rename file
            var retstringName = "Business Plan-" + date.ToString("yyyy-MM-dd-HHmmss") + ".xlsx";

            return File(res, Tools.GetContentType(".xlsx"), retstringName);
        }

        public FileResult DownloadBusPlanDetailFile(string stringName, DateTime date)
        {
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "Template_Bisplan_Cals.xlsx");
            var res = Path.Combine(Server.MapPath("~/App_Data/Temp"), stringName);
            //rename file
            var retstringName = "Business Plan Detail-" + date.ToString("yyyy-MM-dd-HHmmss") + ".xlsx";

            return File(res, Tools.GetContentType(".xlsx"), retstringName);
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

        List<List<string>> GetBaseOP(List<BizPlanActivityPhase> phase)
        {
            var data = new List<List<string>>();
            if (phase != null)
            {
                foreach (var i in phase)
                {
                    data.Add(i.BaseOP.Distinct().ToList());
                }
            }

            return data;
        }

        class FiscalData
        {
            public string Title { get; set; }

        }

        public JsonResult GetDataFiscalYearOnBizPlanEntry2(BizPlanActivityPhase Phase, double ShellShare, string RFM,
            string Country, string Currencyx, string WellName, string ActivityType, int PhaseNo, string SequenceId, string savetoop = "OP15")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                Phase.Estimate.EstimatePeriod.Start = Tools.ToUTC(Phase.Estimate.EstimatePeriod.Start, true);
                Phase.Estimate.EstimatePeriod.Finish = Tools.ToUTC(Phase.Estimate.EstimatePeriod.Finish);
                Phase.Estimate.EventStartDate = Tools.ToUTC(Phase.Estimate.EventStartDate, true);
                Phase.Estimate.EventEndDate = Tools.ToUTC(Phase.Estimate.EventEndDate);


                var maturityLevel = Phase.Estimate.MaturityLevel.Substring(Phase.Estimate.MaturityLevel.Length - 1, 1);
                //BizPlanCalculation res = BizPlanCalculation.calcBizPlan(WellName, Phase.Estimate.RigName, Phase.ActivityType, Country, ShellShare, Phase.Estimate.EstimatePeriod, Convert.ToInt32(maturityLevel), Phase.Estimate.Services, Phase.Estimate.Materials, Phase.Estimate.NewTroubleFree.Days, RFM, Tools.Div(Phase.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(Phase.Estimate.NewTECOPTime.PercentDays, 100), Tools.Div(Phase.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(Phase.Estimate.NewTECOPTime.PercentCost, 100), Phase.Estimate.LongLeadMonthRequired, Tools.Div(Phase.Estimate.PercOfMaterialsLongLead, 100), Phase.Estimate.SpreadRateTotal);

                //convert services dan materials to USD

                double iServices = Phase.Estimate.Services;
                double iMaterials = Phase.Estimate.Materials;

                if (!Currencyx.Trim().ToUpper().Equals("USD"))
                {
                    var datas = MacroEconomic.Populate<MacroEconomic>(
                        Query.And(
                            Query.EQ("Currency", Currencyx),
                            Query.EQ("BaseOP", savetoop)
                            )
                        );
                    if (datas.Any())
                    {
                        var cx = datas.Where(x => x.Currency.Equals(Currencyx)).FirstOrDefault();

                        var rate = cx.ExchangeRate.AnnualValues.Where(x => x.Year == Phase.Estimate.EstimatePeriod.Start.Year).Any() ?
                                cx.ExchangeRate.AnnualValues.Where(x => x.Year == Phase.Estimate.EstimatePeriod.Start.Year).FirstOrDefault().Value
                                : 0;

                        // dibagi rate : untuk jadi USD
                        iServices = Tools.Div(iServices, rate);
                        iMaterials = Tools.Div(iMaterials, rate);
                    }
                    else
                    {
                        iServices = Tools.Div(iServices, 1);
                        iMaterials = Tools.Div(iMaterials, 1);
                    }
                }


                BizPlanSummary res = new BizPlanSummary(WellName, Phase.Estimate.RigName, Phase.ActivityType, Country,
                    ShellShare, Phase.Estimate.EstimatePeriod, Convert.ToInt32(maturityLevel),
                    iServices, iMaterials,
                    Phase.Estimate.TroubleFreeBeforeLC.Days,
                    RFM,
                    Tools.Div(Phase.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(Phase.Estimate.NewTECOPTime.PercentDays, 100),
                    Tools.Div(Phase.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(Phase.Estimate.NewTECOPTime.PercentCost, 100),
                    Phase.Estimate.LongLeadMonthRequired, Tools.Div(Phase.Estimate.PercOfMaterialsLongLead, 100), baseOP: savetoop, isGetExcRateByCurrency: true,
                    lcf: Phase.Estimate.NewLearningCurveFactor
                    );

                res.GeneratePeriodBucket();
                res.GenerateAnnualyBucket(res.MonthlyBuckets);

                var finishDate = res.EventPeriod.Start.Date.AddDays(res.MeanTime);
                res.EventPeriod.Finish = finishDate; //.AddDays(1);

                BizPlanAllocation bpa = new BizPlanAllocation();
                bpa.ActivityType = Phase.ActivityType;
                bpa.WellName = WellName;
                bpa.RigName = Phase.Estimate.RigName;
                bpa.EstimatePeriod = Phase.Estimate.EstimatePeriod;
                bpa.AnnualyBuckets = res.AnnualyBuckets;
                var titleTemplate = new Dictionary<string, string>()
                {
                    { "MeanDaysInYear", "Mean Days In Year" },
                    { "RigCost", "Rig Cost" },
                    { "MaterialCost", "Material Cost" },
                    { "ServiceCost", "Service Cost" },
                    { "TroubleFreeCost", "Trouble Free Cost" },
                    { "NPTCost", "NPT Cost" },
                    { "TECOPCost", "TECOP Cost" },
                    { "EscCostRig", "Escalation Cost Rig" },
                    { "EscCostServices", "Escalation Cost Services" },
                    { "EscCostMaterial", "Escalation Cost Material" },
                    { "EscCostTotal", "Escalation Cost Total" },
                    { "MeanCostEDM", "Mean Cost EDM" },
                    { "CSOCost", "CSO Cost" },
                    { "InflationCost", "Inflation Cost" },
                    { "MeanCostEDM2", "Mean Cost EDM" },
                    { "MeanCostRealTerm", "Mean Cost Real Term" },
                    { "MeanCostMOD", "Mean Cost MOD" },
                    { "ShellShare", "Shell Share" },
                };

                string[] title = titleTemplate.Select(d => d.Key.Equals("MeanCostEDM2") ? "MeanCostEDM" : d.Key).ToArray();
                string[] titleBagus = titleTemplate.Select(d => d.Value).ToArray();

                var periodYears = new List<Int32>();
                var periodMonths = new List<string>();

                var anuls = res.AnnualyBuckets;
                int maxYear = 2015;
                int minYear = 2015;
                if (anuls.Any())
                {
                    maxYear = anuls.Max(x => Convert.ToInt32(x.Key));
                    minYear = anuls.Min(x => Convert.ToInt32(x.Key));
                }

                var ind = 0;
                List<BsonDocument> docsYear = new List<BsonDocument>();
                foreach (var t in title.ToList())
                {
                    BsonDocument bdoc = new BsonDocument();
                    bdoc.Set("Title", t);
                    bdoc.Set("MaxYear", maxYear);
                    bdoc.Set("MinYear", minYear);

                    double value = 0;
                    var prevkey = "";
                    foreach (var a in anuls.OrderBy(x => System.Convert.ToInt32(x.Key)))
                    {
                        var nowKey = "FY" + a.Key;

                        if (!prevkey.Equals(nowKey))
                        {
                            value = 0;
                            value = a.ToBsonDocument().GetDouble(t);
                            bdoc.Set("FY" + a.Key, value);
                            prevkey = "FY" + a.Key;

                        }
                        else
                        {
                            value = value + a.ToBsonDocument().GetDouble(t);
                            bdoc.Set("FY" + a.Key, value);
                        }
                    }
                    bdoc.Set("TitleBagus", titleBagus[ind]);
                    if ((t == "EscCostTotal") || (t == "TroubleFreeCost") || (t == "MeanCostEDM") || (t == "MeanCostRealTerm") || (t == "MeanCostMOD") || (t == "ShellShare"))
                    {
                        bdoc.Set("ColumnClass", "isSummaryField");
                    }
                    else
                    {
                        bdoc.Set("ColumnClass", "");
                    }

                    // add mean days in year value
                    if (t.Equals("MeanDaysInYear"))
                    {
                        var daysEachYear = res.MonthlyBuckets.GroupBy(x => x.Key.Substring(0, 4));// DateRangeToMonth.NumDaysPerYear(new DateRange(Phase.Estimate.EstimatePeriod.Start, Phase.Estimate.EstimatePeriod.Finish));

                        Dictionary<string, double> dayysInyear = new Dictionary<string, double>();
                        foreach (var o in daysEachYear)
                        {
                            dayysInyear.Add(o.Key, o.Sum(x => x.MeanDays));
                        }

                        foreach (var b in bdoc)
                        {
                            if (b.Name.Contains("FY"))
                            {
                                var year = b.Name.Replace("FY", "");
                                if (dayysInyear.Where(x => x.Key.Equals(year)).ToList().Any())
                                {
                                    b.Value = dayysInyear[year];
                                }
                            }
                        }
                    }

                    ind++;
                    docsYear.Add(bdoc);
                }

                var yearStart = 0;
                var yearEnd = 0;
                if (docsYear.Any())
                {
                    var getPeriodYears = docsYear.FirstOrDefault();
                    periodYears.Add(BsonHelper.GetInt32(getPeriodYears, "MaxYear"));
                    periodYears.Add(BsonHelper.GetInt32(getPeriodYears, "MinYear"));
                    yearStart = BsonHelper.GetInt32(getPeriodYears, "MinYear");
                    yearEnd = BsonHelper.GetInt32(getPeriodYears, "MaxYear");
                }
                else
                {
                    periodYears.Add(2015);
                    periodYears.Add(2015);
                    yearStart = 2015;
                    yearEnd = 2015;
                }

                //return docsYear;
                var MeanCostRealTerm = res.MeanCostRealTerm;
                var MeanCostMOD = res.MeanCostMOD;
                var ShellShareMOD = res.ShellShare;

                var TotalMeanDays = 0.0;
                var getMeanDaysInYear = docsYear.Where(x => BsonHelper.GetString(x.ToBsonDocument(), "Title").Equals("MeanDaysInYear")).FirstOrDefault();
                for (var i = yearStart; i <= yearEnd; i++)
                {
                    var FYTitle = "FY" + i.ToString();
                    TotalMeanDays += BsonHelper.GetInt32(getMeanDaysInYear, FYTitle);
                }

                #region currency
                var config3 = Config.Get<Config>("VisibleCurrency");
                List<BizPlanSummaryConv> converts = new List<BizPlanSummaryConv>();

                BizPlanSummaryConv _temp = new BizPlanSummaryConv();
                _temp.BurnRate = res.BurnRate;
                _temp.MeanCostEDM = res.MeanCostEDMUSD;
                _temp.MeanCostMOD = res.MeanCostMOD;
                _temp.MeanCostRealTerm = res.MeanCostRealTerm;
                _temp.NPTCost = res.NPTCostUSD;
                _temp.RigRate = res.RigRate;
                _temp.ShellShareMOD = res.ShellShareCost;
                _temp.SpreadRate = res.SpreadRateWRig;
                _temp.SpreadRateTotal = res.SpreadRateTotal;
                _temp.TECOPCost = res.TECOP.Cost;
                _temp.TroubleFreeCost = res.TroubleFree.Cost;
                _temp.ExchangeRate = "USD";


                //var config3 = Config.Get<Config>("VisibleCurrency");
                //List<BizPlanSummaryConv> converts = new List<BizPlanSummaryConv>();

                //BizPlanSummaryConv _temp = new BizPlanSummaryConv();

                //if (!Currencyx.Trim().ToUpper().Equals("USD"))
                //{
                //    var datas = MacroEconomic.Populate<MacroEconomic>(Query.EQ("Currency", Currencyx));
                //    if (datas.Any())
                //    {
                //        var cx = datas.Where(x => x.Currency.Equals(Currencyx)).FirstOrDefault();

                //        var rate = cx.ExchangeRate.AnnualValues.Where(x => x.Year == res.EventPeriod.Start.Year).Any() ?
                //                cx.ExchangeRate.AnnualValues.Where(x => x.Year == res.EventPeriod.Start.Year).FirstOrDefault().Value
                //                : 0;

                //        // all dibagi rate : untuk jadi USD
                //        _temp.BurnRate = Tools.Div(res.BurnRate, rate);
                //        _temp.MeanCostEDM = Tools.Div(res.MeanCostEDMUSD, rate);
                //        _temp.MeanCostMOD = Tools.Div(res.MeanCostMOD, rate);
                //        _temp.MeanCostRealTerm = Tools.Div( res.MeanCostRealTerm, rate);
                //        _temp.NPTCost = Tools.Div( res.NPTCostUSD, rate);
                //        _temp.RigRate = res.RigRate;
                //        _temp.ShellShareMOD = Tools.Div(res.ShellShareCost, rate);
                //        _temp.SpreadRate = Tools.Div(res.SpreadRateWRig, rate);
                //        _temp.SpreadRateTotal = Tools.Div(res.SpreadRateTotal,rate);
                //        _temp.TECOPCost = Tools.Div(res.TECOP.Cost,rate);
                //        _temp.TroubleFreeCost = Tools.Div(res.TroubleFree.Cost,rate);
                //        _temp.ExchangeRate = "USD";


                //    }
                //}
                //else
                //{
                //    _temp.BurnRate = res.BurnRate;
                //    _temp.MeanCostEDM = res.MeanCostEDMUSD;
                //    _temp.MeanCostMOD = res.MeanCostMOD;
                //    _temp.MeanCostRealTerm = res.MeanCostRealTerm;
                //    _temp.NPTCost = res.NPTCostUSD;
                //    _temp.RigRate = res.RigRate;
                //    _temp.ShellShareMOD = res.ShellShareCost;
                //    _temp.SpreadRate = res.SpreadRateWRig;
                //    _temp.SpreadRateTotal = res.SpreadRateTotal;
                //    _temp.TECOPCost = res.TECOP.Cost;
                //    _temp.TroubleFreeCost = res.TroubleFree.Cost;
                //    _temp.ExchangeRate = "USD";
                //}



                // default already USD
                if (config3 != null)
                {
                    var curs = config3.ConfigValue.ToString().Split(',').ToList();
                    var datas = MacroEconomic.Populate<MacroEconomic>(
                       Query.And(
                        Query.EQ("BaseOP", savetoop),
                        Query.In("Currency", new BsonArray(curs.ToArray()))));
                    foreach (var config in curs)
                    {
                        var cx = datas.Where(x => x.Currency.Equals(config) && x.BaseOP.Equals(savetoop)).FirstOrDefault();



                        if (cx != null)
                        {
                            var rate = cx.ExchangeRate.AnnualValues.Where(x => x.Year == res.EventPeriod.Start.Year).Any() ?
                               cx.ExchangeRate.AnnualValues.Where(x => x.Year == res.EventPeriod.Start.Year).FirstOrDefault().Value
                               : 0;
                            double rateper = rate; //Tools.Div(rate, 100);
                            BizPlanSummaryConv cnv = new BizPlanSummaryConv();
                            if (config.Trim().ToUpper().Equals("USD"))
                            {
                                rateper = 1;
                                cnv.RateVsUSD = 1;
                            }
                            else
                            {
                                cnv.RateVsUSD = rateper;
                            }
                            cnv.ActivityType = res.ActivityType;
                            cnv.BurnRate = rateper * _temp.BurnRate;
                            cnv.EventPeriod = res.EventPeriod;
                            cnv.MeanCostEDM = rateper * _temp.MeanCostEDM;
                            cnv.MeanCostMOD = rateper * _temp.MeanCostMOD;
                            cnv.MeanCostRealTerm = rateper * _temp.MeanCostRealTerm;
                            cnv.MeanTime = res.MeanTime;
                            cnv.NPTCost = rateper * _temp.NPTCost;
                            cnv.RigName = res.RigName;
                            cnv.RigRate = rateper * _temp.RigRate;
                            cnv.ShellShare = res.ShellShare;
                            cnv.ShellShareMOD = rateper * _temp.ShellShareMOD;
                            cnv.SpreadRate = rateper * _temp.SpreadRate;
                            cnv.SpreadRateTotal = rateper * _temp.SpreadRateTotal;
                            cnv.TECOPCost = rateper * _temp.TECOPCost;
                            cnv.TroubleFreeCost = rateper * _temp.TroubleFreeCost;
                            cnv.WellName = res.WellName;
                            cnv.ExchangeRate = config;
                            converts.Add(cnv);
                        }
                    }
                }
                else
                {
                    // only USD
                    BizPlanSummaryConv cnv = new BizPlanSummaryConv();
                    cnv.RateVsUSD = 1;
                    cnv.ActivityType = res.ActivityType;
                    cnv.BurnRate = res.BurnRate;
                    cnv.EventPeriod = res.EventPeriod;
                    cnv.MeanCostEDM = res.MeanCostEDMUSD;
                    cnv.MeanCostMOD = res.MeanCostMOD;
                    cnv.MeanCostRealTerm = res.MeanCostRealTerm;
                    cnv.MeanTime = res.MeanTime;
                    cnv.NPTCost = res.NPTCostUSD;
                    cnv.RigName = res.RigName;
                    cnv.RigRate = res.RigRate;
                    cnv.ShellShare = res.ShellShare;
                    cnv.ShellShareMOD = res.ShellShareCost;
                    cnv.SpreadRate = res.SpreadRateWRig;
                    cnv.SpreadRateTotal = res.SpreadRateTotal;
                    cnv.TECOPCost = res.TECOP.Cost;
                    cnv.TroubleFreeCost = res.TroubleFree.Cost;
                    cnv.WellName = res.WellName;
                    cnv.ExchangeRate = "USD";
                    converts.Add(cnv);
                }


                //get data current OP (left side on the screen)
                var CurrentTroubleFree = new WellDrillData() { Days = 0.0, Cost = 0.0 };
                var CurrentNPTTime = new WellDrillData() { Days = 0.0, Cost = 0.0 };
                var CurrentTECOPTime = new WellDrillData() { Days = 0.0, Cost = 0.0 };
                var CurrentMeanTime = new WellDrillData() { Days = 0.0, Cost = 0.0 };
                var BaseOP = Phase.Estimate.SaveToOP;
                var BaseOPYear = Convert.ToInt32(BaseOP.Substring(BaseOP.Length - 2, 2));
                var previousOP = "OP" + ((BaseOPYear - 1).ToString());

                var qsWellPlan = new List<IMongoQuery>();
                qsWellPlan.Add(Query.EQ("WellName", WellName));
                qsWellPlan.Add(Query.EQ("Phases.ActivityType", ActivityType));
                qsWellPlan.Add(Query.EQ("Phases.PhaseNo", PhaseNo));
                qsWellPlan.Add(Query.EQ("UARigSequenceId", SequenceId));

                var getWellPlan = WellActivity.Get<WellActivity>(Query.And(qsWellPlan));
                if (getWellPlan != null)
                {
                    if (getWellPlan.Phases.Any())
                    {
                        foreach (var x in getWellPlan.Phases)
                        {
                            if (x.PhaseNo == PhaseNo)
                            {
                                //get Phase Info
                                var qsPhaseInfo = new List<IMongoQuery>();
                                qsPhaseInfo.Add(Query.EQ("WellName", getWellPlan.WellName));
                                qsPhaseInfo.Add(Query.EQ("ActivityType", x.ActivityType));
                                qsPhaseInfo.Add(Query.EQ("RigName", getWellPlan.RigName));
                                qsPhaseInfo.Add(Query.EQ("SequenceId", getWellPlan.UARigSequenceId));
                                qsPhaseInfo.Add(Query.EQ("WellActivityId", getWellPlan._id.ToString()));
                                qsPhaseInfo.Add(Query.EQ("OPType", previousOP));
                                var getPhaseInfo = WellActivityPhaseInfo.Get<WellActivityPhaseInfo>(Query.And(qsPhaseInfo));
                                if (getPhaseInfo != null)
                                {
                                    var multiplier = 1000000;
                                    CurrentTroubleFree = new WellDrillData() { Days = getPhaseInfo.TroubleFree.Days, Cost = getPhaseInfo.TroubleFree.Cost * multiplier };
                                    CurrentNPTTime = new WellDrillData() { Days = getPhaseInfo.NPT.Days, Cost = getPhaseInfo.NPT.Cost * multiplier };
                                    CurrentTECOPTime = new WellDrillData() { Days = getPhaseInfo.TECOP.Days, Cost = getPhaseInfo.TECOP.Cost * multiplier };
                                    CurrentMeanTime.Days = getPhaseInfo.Mean.Days;
                                    CurrentMeanTime.Cost = getPhaseInfo.MeanCostEDM.Cost * multiplier;

                                    //get NPT
                                    //var qsWAU = new List<IMongoQuery>();
                                    //qsWAU.Add(Query.EQ("WellName", WellName));
                                    //qsWAU.Add(Query.EQ("Phase.ActivityType", ActivityType));
                                    //qsWAU.Add(Query.EQ("Phase.PhaseNo", PhaseNo));
                                    //qsWAU.Add(Query.EQ("SequenceId", SequenceId));
                                    //qsWAU.Add(Query.In("Phase.BaseOP", new BsonArray(new string[] { previousOP })));
                                    //var getWAU = WellActivityUpdate.Populate<WellActivityUpdate>(Query.And(qsWAU));
                                    //if (getWAU.Any())
                                    //{
                                    //    var wau = getWAU.OrderByDescending(d => d.UpdateVersion).FirstOrDefault();
                                    //    CurrentNPTTime = wau.NPT;
                                    //}
                                }
                            }
                        }
                    }
                }


                #endregion

                return new
                {
                    Data = DataHelper.ToDictionaryArray(docsYear),
                    YearStart = yearStart,
                    YearEnd = yearEnd,
                    MeanCostMOD = MeanCostMOD,
                    ShellShareMOD = res.ShellShareCost,
                    MeanCostRealTerm = MeanCostRealTerm,
                    RigRate = res.RigRate,
                    TroubleFreeCost = res.TroubleFree.Cost,
                    NPTCost = res.NPT.Cost,
                    TECOPCost = res.TECOP.Cost,
                    NPTDays = res.NPT.Days,

                    NewLCFValueDays = Math.Round(res.NewLCFValue.Days, 2),
                    NewLCFValueCost = Math.Round(res.NewLCFValue.Cost, 2),

                    NewBaseValueDays = Math.Round(res.NewBaseValue.Days, 2),
                    NewBaseValueCost = Math.Round(res.NewBaseValue.Cost, 2),

                    TECOPDays = res.TECOP.Days,
                    MeanCostEDM = res.MeanCostEDM,
                    ShellShare = res.ShellShareCost,
                    EventDate = res.EventPeriod,
                    //TroubleFreeCostUSD = res.TroubleFreeCostUSD,
                    //NPTCostUSD = res.NPTCostUSD,
                    //TECOPCostUSD = res.TECOPCostUSD,
                    //MeanCostUSD = res.MeanCostEDMUSD,
                    TroubleFreeCostUSD = res.TroubleFreeCostUSD,
                    NPTCostUSD = res.NPTCostUSD,
                    TECOPCostUSD = res.TECOPCostUSD,
                    MeanCostUSD = res.MeanCostEDMUSD,
                    SpreadRate = res.SpreadRateWRig,
                    SpreadRateTotal = res.SpreadRateTotal,
                    BurnRate = res.BurnRate,
                    MeanTime = res.MeanTime,
                    CurrencyConvert = converts,
                    CurrentTroubleFree,
                    CurrentTECOPTime,
                    CurrentNPTTime,
                    CurrentMeanTime
                };
            });

        }


        //public JsonResult CalcBizPlan(BizPlanActivityPhase Phase, double ShellShare, string RFM, string Country, string Currencyx, string WellName)
        //{
        //    return MvcResultInfo.Execute(null, (BsonDocument doc) =>
        //    {
        //        var maturityLevel = Phase.Estimate.MaturityLevel.Substring(Phase.Estimate.MaturityLevel.Length - 1, 1);
        //        BizPlanCalculation res = BizPlanCalculation.calcBizPlan(WellName, Phase.Estimate.RigName, Phase.ActivityType, Country, ShellShare, 
        //            Phase.Estimate.EstimatePeriod, Convert.ToInt32(maturityLevel), Phase.Estimate.Services, Phase.Estimate.Materials, 
        //            Phase.Estimate.NewTroubleFree.Days, RFM, Tools.Div(Phase.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(Phase.Estimate.NewTECOPTime.PercentDays, 100), Tools.Div(Phase.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(Phase.Estimate.NewTECOPTime.PercentCost, 100), Phase.Estimate.LongLeadMonthRequired, 
        //            Tools.Div(Phase.Estimate.PercOfMaterialsLongLead, 100), 
        //            Phase.Estimate.SpreadRateTotal);


        //        return new
        //        {
        //            RigRate = res.RigRate,
        //            TroubleFreeCost = res.TroubleFreeCost,
        //            NPTCost = res.NPTCost,
        //            TECOPCost = res.TECOPCost,
        //            NPTDays = res.NPTDays,
        //            TECOPDays = res.TECOPDays,
        //            MeanCostEDM = res.MeanCostEDM,
        //            ShellShare = res.ShellShareCost,
        //            MeanCostRealTerm = res.MeanCostRealTerm,
        //            MeanCostMOD = res.MeanCostMOD,
        //            EventDate = res.EventDate,
        //            TroubleFreeCostUSD = res.TroubleFreeCostUSD,
        //            NPTCostUSD = res.NPTCostUSD,
        //            TECOPCostUSD = res.TECOPCostUSD,
        //            MeanCostUSD = res.MeanCostEDMUSD,
        //            SpreadRate = res.SpreadRateWORig,
        //            SpreadRateTotal = res.SpreadRateTotal,
        //            BurnRate = res.BurnRate,
        //            MeanTime = res.MeanTime
        //        };
        //    });

        //}



        public JsonResult GetDataFiscalYear2(WaterfallBase wb, string ViewBy, int yearStart = 0, int yearFinish = 0, List<string> Status = null)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                try
                {
                    var email = WebTools.LoginUser.Email;
                    var getraw = wb.GetActivitiesForBizPlanForFiscalYear(email);

                    #region for debugging purpose
                    //DataHelper.Delete("temp_fyv");
                    //var uw = getraw.SelectMany(x => x.Phases, (b, p) => new { 
                    //    b,p
                    //}).ToList();
                    //foreach (var a in uw)
                    //{
                    //    DataHelper.Save("temp_fyv", a.ToBsonDocument());
                    //}
                    #endregion

                    List<BizPlanAllocation> allocations = new List<BizPlanAllocation>();
                    //if (wb.OPs != null && wb.OPs.Count() > 0)
                    //{
                    //    var qbaseOP = Query.In("SaveToOP", new BsonArray(wb.OPs));
                    //    allocations = BizPlanAllocation.Populate<BizPlanAllocation>(qbaseOP);
                    //}
                    //else
                    //{
                    //    allocations = BizPlanAllocation.Populate<BizPlanAllocation>();
                    //}

                    var periodBase = "";
                    if (ViewBy.ToLower() == "year")
                    {
                        periodBase = "annualy";
                    }
                    else if (ViewBy.ToLower() == "month")
                    {
                        periodBase = "monthly";
                    }
                    else
                    {
                        periodBase = "quarterly";
                    }

                    var periodYears = new List<Int32>();
                    var periodMonths = new List<string>();

                    var detail = BizPlanAllocation.TransformDetailBizAllocationToBson(getraw, periodBase, Status);
                    var res = BizPlanAllocation.TransformSummaryBizAllocationToBson(getraw, periodBase, Status);


                    #region noneed
                    //foreach (var a in detail)
                    //{
                    //    var data = BsonHelper.Get(a.ToBsonDocument(), "Data").AsBsonArray;
                    //    if (data != null && data.Count() > 0)
                    //    {
                    //        foreach (var d in data)
                    //        {
                    //            var WellName = BsonHelper.GetString(d.ToBsonDocument(), "WellName");
                    //            var ActType = BsonHelper.GetString(d.ToBsonDocument(), "ActivityType");
                    //            var RigName = BsonHelper.GetString(d.ToBsonDocument(), "RigName");
                    //            var getEstPeriodOri = allocations.Where(x => x.WellName.Equals(WellName) && x.RigName.Equals(RigName) && x.ActivityType.Equals(ActType)).FirstOrDefault();
                    //            if (getEstPeriodOri != null)
                    //            {
                    //                //a.Set("LESchedule", getEstPeriodOri.EstimatePeriod);
                    //                d.ToBsonDocument().Set("LESchedule.Start", getEstPeriodOri.EstimatePeriod.Start);
                    //                d.ToBsonDocument().Set("LESchedule.Finish", getEstPeriodOri.EstimatePeriod.Finish);
                    //            }
                    //        }
                    //    }
                    //}
                    #endregion
                    periodMonths = detail.Select(x => BsonHelper.GetString(x, "Year")).ToList();
                    if (periodBase != "annualy")
                    {
                        periodMonths = detail.OrderBy(x => BsonHelper.GetDateTime(x, "dkey")).Select(x => BsonHelper.GetString(x, "Year")).ToList();
                    }

                    if (res.Any())
                    {
                        var getPeriodYears = res.FirstOrDefault();
                        periodYears.Add(BsonHelper.GetInt32(getPeriodYears, "MaxYear"));
                        periodYears.Add(BsonHelper.GetInt32(getPeriodYears, "MinYear"));
                    }
                    else
                    {
                        periodYears.Add(2015);
                        periodYears.Add(2015);
                    }
                    var listDataBson = new List<BsonDocument>();
                    if (detail.Count > 0)
                    {
                        foreach (var dt in detail)
                        {
                            var data = dt["Data"].AsBsonArray;
                            if (data.Count > 0)
                            {
                                foreach (var itm in data)
                                {
                                    listDataBson.Add(itm.ToBsonDocument());
                                }
                            }
                        }
                    }
                    var groupBySeq = listDataBson.GroupBy(x => new BsonDocument { x.GetElement("ActivityType"), x.GetElement("UARigSequenceId") }).ToList().Count;

                    return new { totalGroupBySeq = groupBySeq, InLastUploadLS = wb.inlastuploadls, Summary = DataHelper.ToDictionaryArray(res), Detail = DataHelper.ToDictionaryArray(detail), periodYears = periodYears, periodMonths = periodMonths };
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

        public JsonResult GenerateFiscalYearExcel(WaterfallBase wbs, string ViewBy, int yearStart = 0, int yearFinish = 0, List<string> Status = null)
        {
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "FiscalYearExportTemplate.xlsx");
            List<WellActivity> wellactiv = new List<WellActivity>();

            Workbook wb = new Workbook(xlst);
            //define data
            var email = WebTools.LoginUser.Email;
            var getraw = wbs.GetActivitiesForBizPlanForFiscalYear(email);
            //var allocations = BizPlanAllocation.Populate<BizPlanAllocation>();
            //if (wbs.OPs != null && wbs.OPs.Count() > 0)
            //{
            //    var qbaseOP = Query.In("SaveToOP", new BsonArray(wbs.OPs));
            //    allocations = BizPlanAllocation.Populate<BizPlanAllocation>(qbaseOP);
            //}
            //else
            //{
            //    allocations = BizPlanAllocation.Populate<BizPlanAllocation>();
            //}
            var RFMChecking = new List<object>();
            foreach (var rw in getraw)
            {
                if (String.IsNullOrEmpty(rw.ReferenceFactorModel) || rw.ReferenceFactorModel.ToLower() == "default")
                {
                    foreach (var phase in rw.Phases)
                    {
                        var t = new
                        {
                            WellName = rw.WellName,
                            UARigSequenceId = rw.UARigSequenceId,
                            ActivityType = phase.ActivityType,
                            rw.RigName,
                            Project = rw.ProjectName,
                            rw.Country,
                            ReferenceFactorModel = rw.ReferenceFactorModel
                        };
                        RFMChecking.Add(t);
                    }
                }
            }
            var periodBase = "";
            if (ViewBy.ToLower() == "year")
            {
                periodBase = "annualy";
            }
            else if (ViewBy.ToLower() == "month")
            {
                periodBase = "monthly";
            }
            else
            {
                periodBase = "quarterly";
            }

            var periodYears = new List<Int32>();
            var periodMonths = new List<string>();
            var periodValue = new List<string>();

            var res = BizPlanAllocation.TransformSummaryBizAllocationToBson(getraw, periodBase, Status);
            var detail = BizPlanAllocation.TransformDetailBizAllocationToBson(getraw, periodBase, Status);
            detail.Sort();
            periodMonths = detail.Select(x => BsonHelper.GetString(x, "Year")).ToList();
            if (periodBase != "annualy")
            {
                periodMonths = detail.OrderBy(x => BsonHelper.GetDateTime(x, "dkey")).Select(x => BsonHelper.GetString(x, "Year")).ToList();
            }

            if (res.Any())
            {
                var getPeriodYears = res.FirstOrDefault();
                periodYears.Add(BsonHelper.GetInt32(getPeriodYears, "MaxYear"));
                periodYears.Add(BsonHelper.GetInt32(getPeriodYears, "MinYear"));
            }
            else
            {
                periodYears.Add(2015);
                periodYears.Add(2015);
            }

            #region excel for summary
            wb.Worksheets.Add();
            wb.Worksheets[0].Name = "Summary";

            var InitiateCell = new List<string>();
            int cellcolumns = 0;
            int InitiateRowforSummary = 1;
            wb.Worksheets[0].Cells[0, 0].Value = "Title";

            for (var x = 0; x < periodMonths.Count; x++)
            {
                cellcolumns++;

                wb.Worksheets[0].Cells[0, cellcolumns].Value = periodMonths[x] + "";
            }
            var totrowSummary = res.Count();
            int AutoColumns = 1; int IndexDataTake = 3;
            foreach (var z in periodMonths)//for (var z = 0; z < PeriodeCount; z++)
            {
                int AutoRow = 1;
                for (var row = 0; row <= totrowSummary; row++)
                {

                    if (row != totrowSummary)
                    {

                        var vl = 0.0;
                        try
                        {
                            if (ViewBy.ToLower() == "year")
                            {
                                vl = Convert.ToDouble(res[row]["FY" + z]);
                            }
                            else
                            {
                                vl = Convert.ToDouble(res[row][z]);
                            }

                        }
                        catch (Exception)
                        {
                            vl = 0;
                        }

                        wb.Worksheets[0].Cells[AutoRow, 0].Value = res[row][0];
                        wb.Worksheets[0].Cells[AutoRow, AutoColumns].Value = vl; //== 0 ? "0" : vl.ToString();//Convert.ToDouble(res[row][IndexDataTake]);
                        wb.Worksheets[0].Cells[AutoRow, AutoColumns].SetStyle(new Style() { Number = 3 });
                    }
                    else
                    {
                        wb.Worksheets[0].Cells[AutoRow, 0].Value = "Activities Count";
                        var datas = detail.FirstOrDefault(x => x.GetString("Year").ToString().Equals(z.ToString()));
                        BsonValue totrow = null;
                        try
                        {
                            //totrow = detail[0]["Data"];
                            totrow = datas["Data"];
                        }
                        catch (Exception)
                        {
                            totrow = null;
                        }
                        var count = 0;
                        if (totrow != null)
                            count = totrow.AsBsonArray.Count;
                        wb.Worksheets[0].Cells[AutoRow, AutoColumns].Value = Convert.ToInt32(count); //== 0 ? "0" : vl.ToString();//Convert.ToDouble(res[row][IndexDataTake]);
                        wb.Worksheets[0].Cells[AutoRow, AutoColumns].SetStyle(new Style() { Number = 0 });
                    }

                    AutoRow++;
                }
                AutoColumns++;
                IndexDataTake++;
            }
            wb.Worksheets[0].AutoFitColumns();
            #endregion

            #region excel for detail sheet
            int totvar = 0; int IndexSheet = 0;
            for (var i = 0; i < periodMonths.Count; i++)
            {
                IndexSheet++;
                int initiateRow = 1;
                int startRow = 3;
                int FooterRow = 0;
                wb.Worksheets.Add();

                if (periodBase == "annualy")
                    wb.Worksheets[IndexSheet].Name = "FY" + periodMonths[i] + "";
                else
                    wb.Worksheets[IndexSheet].Name = periodMonths[i] + "";

                // wb.Worksheets[0].Cells["A" + startRow.ToString()] = "";
                wb.Worksheets[IndexSheet].FreezePanes(2, 2, 2, 0);

                BsonValue totrow = null;//detail[totvar]["Data"];
                try
                {
                    totrow = detail[totvar]["Data"];
                }
                catch (Exception)
                {
                    totrow = null;
                }

                double EscCostRig = 0;
                double EscCostServices = 0;
                double EscCostMaterial = 0;
                double EscCostTotal = 0;
                double CSOCost = 0;
                double InflationCost = 0;
                double MeanCostEDM = 0;
                double MeanCostRealTerm = 0;
                double MeanCostMOD = 0;
                double MeanCostEDMSS = 0;
                double MeanCostRealTermSS = 0;
                double MeanCostMODSS = 0;
                double ShellShare = 0;
                //Define Header
                //bgColor
                StyleFlag flg = new StyleFlag();
                flg.All = true;
                var initiateRow2 = 2;
                Style style1 = wb.Worksheets[IndexSheet].Cells["A" + initiateRow.ToString()].GetStyle();
                style1.BackgroundColor = System.Drawing.Color.GreenYellow; //System.Drawing.colo
                style1.Font.IsBold = true;
                style1.Borders[BorderType.TopBorder].LineStyle = CellBorderType.Thick;
                style1.Borders[BorderType.BottomBorder].LineStyle = CellBorderType.Thick;
                style1.Borders[BorderType.LeftBorder].LineStyle = CellBorderType.Thick;
                style1.Borders[BorderType.RightBorder].LineStyle = CellBorderType.Thick;
                Style style2 = wb.Worksheets[IndexSheet].Cells["A" + initiateRow2.ToString()].GetStyle();
                style2.BackgroundColor = System.Drawing.Color.GreenYellow; //System.Drawing.colo
                style2.Font.IsBold = true;
                style2.Borders[BorderType.TopBorder].LineStyle = CellBorderType.Thick;
                style2.Borders[BorderType.BottomBorder].LineStyle = CellBorderType.Thick;
                style2.Borders[BorderType.LeftBorder].LineStyle = CellBorderType.Thick;
                style2.Borders[BorderType.RightBorder].LineStyle = CellBorderType.Thick;
                wb.Worksheets[IndexSheet].Cells.CreateRange("A" + initiateRow.ToString(), "R" + initiateRow.ToString()).ApplyStyle(style1, flg);
                wb.Worksheets[IndexSheet].Cells.CreateRange("A" + initiateRow2.ToString(), "R" + initiateRow2.ToString()).ApplyStyle(style2, flg);
                wb.Worksheets[IndexSheet].Cells["A" + initiateRow.ToString()].Value = "Well Name";
                wb.Worksheets[IndexSheet].Cells["B" + initiateRow.ToString()].Value = "Activity Type";
                wb.Worksheets[IndexSheet].Cells["C" + initiateRow.ToString()].Value = "Rig Name";
                wb.Worksheets[IndexSheet].Cells["D" + initiateRow.ToString()].Value = "Project Name";
                wb.Worksheets[IndexSheet].Cells["E" + initiateRow.ToString()].Value = "OP Start";
                wb.Worksheets[IndexSheet].Cells["F" + initiateRow.ToString()].Value = "OP Finish";
                wb.Worksheets[IndexSheet].Cells["G" + initiateRow.ToString()].Value = "Ecs Cost Rig";
                wb.Worksheets[IndexSheet].Cells["H" + initiateRow.ToString()].Value = "Ecs Cost Services";
                wb.Worksheets[IndexSheet].Cells["I" + initiateRow.ToString()].Value = "Ecs Cost Materials";
                wb.Worksheets[IndexSheet].Cells["J" + initiateRow.ToString()].Value = "Ecs Cost Total";
                wb.Worksheets[IndexSheet].Cells["K" + initiateRow.ToString()].Value = "CSO Cost";
                wb.Worksheets[IndexSheet].Cells["L" + initiateRow.ToString()].Value = "Inflation Cost";

                wb.Worksheets[IndexSheet].Cells["M" + initiateRow.ToString()].Value = "Mean Cost EDM";
                wb.Worksheets[IndexSheet].Cells["N" + initiateRow.ToString()].Value = "Mean Cost EDM With Shell Share";

                wb.Worksheets[IndexSheet].Cells["O" + initiateRow.ToString()].Value = "Mean Cost Real Term";
                wb.Worksheets[IndexSheet].Cells["P" + initiateRow.ToString()].Value = "Mean Cost Real Term With Shell Share";

                wb.Worksheets[IndexSheet].Cells["Q" + initiateRow.ToString()].Value = "Mean Cost MOD";
                wb.Worksheets[IndexSheet].Cells["R" + initiateRow.ToString()].Value = "Mean Cost MOD With Shell Share";

                wb.Worksheets[IndexSheet].Cells.Merge(0, 0, 2, 1);
                wb.Worksheets[IndexSheet].Cells.Merge(0, 1, 2, 1);
                wb.Worksheets[IndexSheet].Cells.Merge(0, 2, 2, 1);
                wb.Worksheets[IndexSheet].Cells.Merge(0, 3, 2, 1);
                wb.Worksheets[IndexSheet].Cells.Merge(0, 4, 2, 1);
                wb.Worksheets[IndexSheet].Cells.Merge(0, 5, 2, 1);
                wb.Worksheets[IndexSheet].Cells.Merge(0, 6, 2, 1);

                wb.Worksheets[IndexSheet].Cells.Merge(0, 7, 2, 1);
                wb.Worksheets[IndexSheet].Cells.Merge(0, 8, 2, 1);
                wb.Worksheets[IndexSheet].Cells.Merge(0, 9, 2, 1);
                wb.Worksheets[IndexSheet].Cells.Merge(0, 10, 2, 1);
                wb.Worksheets[IndexSheet].Cells.Merge(0, 11, 2, 1);
                wb.Worksheets[IndexSheet].Cells.Merge(0, 12, 2, 1);
                wb.Worksheets[IndexSheet].Cells.Merge(0, 13, 2, 1);
                wb.Worksheets[IndexSheet].Cells.Merge(0, 14, 2, 1);
                wb.Worksheets[IndexSheet].Cells.Merge(0, 15, 2, 1);
                wb.Worksheets[IndexSheet].Cells.Merge(0, 16, 2, 1);
                wb.Worksheets[IndexSheet].Cells.Merge(0, 17, 2, 1);
                if (totrow != null)
                {
                    for (int getRow = 0; getRow < totrow.AsBsonArray.Count; getRow++)
                    {

                        //var LEStart = totrow[getRow]["LESchedule"]["Start"].ToString();
                        DateTime LEStart = DateTime.Parse(totrow[getRow]["LESchedule"]["Start"].ToString());
                        DateTime LEFinish = DateTime.Parse(totrow[getRow]["LESchedule"]["Finish"].ToString());
                        wb.Worksheets[IndexSheet].Cells["A" + startRow.ToString()].Value = totrow[getRow]["WellName"];
                        wb.Worksheets[IndexSheet].Cells["B" + startRow.ToString()].Value = totrow[getRow]["ActivityType"];
                        wb.Worksheets[IndexSheet].Cells["C" + startRow.ToString()].Value = totrow[getRow]["RigName"];
                        wb.Worksheets[IndexSheet].Cells["D" + startRow.ToString()].Value = totrow[getRow]["ProjectName"];
                        //wb.Worksheets[IndexSheet].Cells["D" + startRow.ToString()].Value = LEStart.ToString("dd-MMM-yyyy");
                        //wb.Worksheets[IndexSheet].Cells["E" + startRow.ToString()].Value = LEFinish.ToString("dd-MMM-yyyy");

                        wb.Worksheets[IndexSheet].Cells["E" + startRow.ToString()].PutValue(LEStart);
                        Style styleD = wb.Worksheets[IndexSheet].Cells["E" + startRow.ToString()].GetStyle();
                        styleD.Custom = "m/d/yyyy";
                        wb.Worksheets[IndexSheet].Cells["E" + startRow.ToString()].SetStyle(styleD);

                        wb.Worksheets[IndexSheet].Cells["F" + startRow.ToString()].PutValue(LEFinish);
                        Style styleE = wb.Worksheets[IndexSheet].Cells["F" + startRow.ToString()].GetStyle();
                        styleE.Custom = "m/d/yyyy";
                        wb.Worksheets[IndexSheet].Cells["F" + startRow.ToString()].SetStyle(styleE);

                        wb.Worksheets[IndexSheet].Cells["G" + startRow.ToString()].Value = Convert.ToDouble(totrow[getRow]["EscCostRig"]);
                        wb.Worksheets[IndexSheet].Cells["G" + startRow.ToString()].SetStyle(new Style() { Number = 3 });
                        wb.Worksheets[IndexSheet].Cells["H" + startRow.ToString()].Value = Convert.ToDouble(totrow[getRow]["EscCostServices"]);
                        wb.Worksheets[IndexSheet].Cells["H" + startRow.ToString()].SetStyle(new Style() { Number = 3 });
                        wb.Worksheets[IndexSheet].Cells["I" + startRow.ToString()].Value = Convert.ToDouble(totrow[getRow]["EscCostMaterial"]);
                        wb.Worksheets[IndexSheet].Cells["I" + startRow.ToString()].SetStyle(new Style() { Number = 3 });
                        wb.Worksheets[IndexSheet].Cells["J" + startRow.ToString()].Value = Convert.ToDouble(totrow[getRow]["EscCostTotal"]);
                        wb.Worksheets[IndexSheet].Cells["J" + startRow.ToString()].SetStyle(new Style() { Number = 3 });
                        wb.Worksheets[IndexSheet].Cells["K" + startRow.ToString()].Value = Convert.ToDouble(totrow[getRow]["CSOCost"]);
                        wb.Worksheets[IndexSheet].Cells["K" + startRow.ToString()].SetStyle(new Style() { Number = 3 });
                        wb.Worksheets[IndexSheet].Cells["L" + startRow.ToString()].Value = Convert.ToDouble(totrow[getRow]["InflationCost"]);
                        wb.Worksheets[IndexSheet].Cells["L" + startRow.ToString()].SetStyle(new Style() { Number = 3 });

                        wb.Worksheets[IndexSheet].Cells["M" + startRow.ToString()].Value = Convert.ToDouble(totrow[getRow]["MeanCostEDM"]);
                        wb.Worksheets[IndexSheet].Cells["M" + startRow.ToString()].SetStyle(new Style() { Number = 3 });
                        wb.Worksheets[IndexSheet].Cells["N" + startRow.ToString()].Value = Convert.ToDouble(totrow[getRow]["MeanCostEDMSS"]);
                        wb.Worksheets[IndexSheet].Cells["N" + startRow.ToString()].SetStyle(new Style() { Number = 3 });

                        wb.Worksheets[IndexSheet].Cells["O" + startRow.ToString()].Value = Convert.ToDouble(totrow[getRow]["MeanCostRealTerm"]);
                        wb.Worksheets[IndexSheet].Cells["O" + startRow.ToString()].SetStyle(new Style() { Number = 3 });
                        wb.Worksheets[IndexSheet].Cells["P" + startRow.ToString()].Value = Convert.ToDouble(totrow[getRow]["MeanCostRealTermSS"]);
                        wb.Worksheets[IndexSheet].Cells["P" + startRow.ToString()].SetStyle(new Style() { Number = 3 });

                        wb.Worksheets[IndexSheet].Cells["Q" + startRow.ToString()].Value = Convert.ToDouble(totrow[getRow]["MeanCostMOD"]);
                        wb.Worksheets[IndexSheet].Cells["Q" + startRow.ToString()].SetStyle(new Style() { Number = 3 });
                        wb.Worksheets[IndexSheet].Cells["R" + startRow.ToString()].Value = Convert.ToDouble(totrow[getRow]["MeanCostMODSS"]);
                        wb.Worksheets[IndexSheet].Cells["R" + startRow.ToString()].SetStyle(new Style() { Number = 3 });

                        EscCostRig += Convert.ToDouble(totrow[getRow]["EscCostRig"]);
                        EscCostServices += Convert.ToDouble(totrow[getRow]["EscCostServices"]);
                        EscCostMaterial += Convert.ToDouble(totrow[getRow]["EscCostMaterial"]);
                        EscCostTotal += Convert.ToDouble(totrow[getRow]["EscCostTotal"]);
                        CSOCost += Convert.ToDouble(totrow[getRow]["CSOCost"]);
                        InflationCost += Convert.ToDouble(totrow[getRow]["InflationCost"]);
                        MeanCostEDM += Convert.ToDouble(totrow[getRow]["MeanCostEDM"]);
                        MeanCostRealTerm += Convert.ToDouble(totrow[getRow]["MeanCostRealTerm"]);
                        MeanCostMOD += Convert.ToDouble(totrow[getRow]["MeanCostMOD"]);
                        MeanCostEDMSS += Convert.ToDouble(totrow[getRow]["MeanCostEDMSS"]);
                        MeanCostRealTermSS += Convert.ToDouble(totrow[getRow]["MeanCostRealTermSS"]);
                        MeanCostMODSS += Convert.ToDouble(totrow[getRow]["MeanCostMODSS"]);
                        ShellShare += Convert.ToDouble(totrow[getRow]["ShellShare"]);
                        startRow++;
                    }
                    wb.Worksheets[IndexSheet].AutoFilter.Range = "A1:R" + startRow.ToString();
                }

                //FooterRow = FooterRow - 1;
                FooterRow = startRow;
                //footer
                wb.Worksheets[IndexSheet].Cells["A" + FooterRow.ToString()].Value = "";
                wb.Worksheets[IndexSheet].Cells["B" + FooterRow.ToString()].Value = "";
                wb.Worksheets[IndexSheet].Cells["C" + FooterRow.ToString()].Value = "";
                wb.Worksheets[IndexSheet].Cells["D" + FooterRow.ToString()].Value = "";
                wb.Worksheets[IndexSheet].Cells["E" + FooterRow.ToString()].Value = "";
                wb.Worksheets[IndexSheet].Cells["F" + FooterRow.ToString()].Value = "";
                wb.Worksheets[IndexSheet].Cells["G" + FooterRow.ToString()].Value = EscCostRig;
                wb.Worksheets[IndexSheet].Cells["G" + FooterRow.ToString()].SetStyle(new Style() { Number = 3 });
                wb.Worksheets[IndexSheet].Cells["H" + FooterRow.ToString()].Value = EscCostServices;
                wb.Worksheets[IndexSheet].Cells["H" + FooterRow.ToString()].SetStyle(new Style() { Number = 3 });
                wb.Worksheets[IndexSheet].Cells["I" + FooterRow.ToString()].Value = EscCostMaterial;
                wb.Worksheets[IndexSheet].Cells["I" + FooterRow.ToString()].SetStyle(new Style() { Number = 3 });
                wb.Worksheets[IndexSheet].Cells["J" + FooterRow.ToString()].Value = EscCostTotal;
                wb.Worksheets[IndexSheet].Cells["I" + FooterRow.ToString()].SetStyle(new Style() { Number = 3 });
                wb.Worksheets[IndexSheet].Cells["K" + FooterRow.ToString()].Value = CSOCost;
                wb.Worksheets[IndexSheet].Cells["K" + FooterRow.ToString()].SetStyle(new Style() { Number = 3 });
                wb.Worksheets[IndexSheet].Cells["L" + FooterRow.ToString()].Value = InflationCost;
                wb.Worksheets[IndexSheet].Cells["L" + FooterRow.ToString()].SetStyle(new Style() { Number = 3 });

                wb.Worksheets[IndexSheet].Cells["M" + FooterRow.ToString()].Value = MeanCostEDM;
                wb.Worksheets[IndexSheet].Cells["M" + FooterRow.ToString()].SetStyle(new Style() { Number = 3 });
                wb.Worksheets[IndexSheet].Cells["N" + FooterRow.ToString()].Value = MeanCostEDMSS;
                wb.Worksheets[IndexSheet].Cells["N" + FooterRow.ToString()].SetStyle(new Style() { Number = 3 });

                wb.Worksheets[IndexSheet].Cells["O" + FooterRow.ToString()].Value = MeanCostRealTerm;
                wb.Worksheets[IndexSheet].Cells["O" + FooterRow.ToString()].SetStyle(new Style() { Number = 3 });
                wb.Worksheets[IndexSheet].Cells["P" + FooterRow.ToString()].Value = MeanCostRealTermSS;
                wb.Worksheets[IndexSheet].Cells["P" + FooterRow.ToString()].SetStyle(new Style() { Number = 3 });

                wb.Worksheets[IndexSheet].Cells["Q" + FooterRow.ToString()].Value = MeanCostMOD;
                wb.Worksheets[IndexSheet].Cells["Q" + FooterRow.ToString()].SetStyle(new Style() { Number = 3 });
                wb.Worksheets[IndexSheet].Cells["R" + FooterRow.ToString()].Value = MeanCostMODSS;
                wb.Worksheets[IndexSheet].Cells["R" + FooterRow.ToString()].SetStyle(new Style() { Number = 3 });
                wb.Worksheets[IndexSheet].AutoFitColumns();
                totvar++;
            }
            #endregion

            var newFileNameSingle = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                     String.Format("FiscalYear-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss")));

            string returnName = String.Format("FiscalYear-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss"));

            wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            return Json(new { Success = true, Path = returnName, RFMChecking }, JsonRequestBehavior.AllowGet);
        }

        public FileResult DownloadBrowserFile(string stringName, DateTime date)
        {
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "FiscalYearExportTemplate.xlsx");
            var res = Path.Combine(Server.MapPath("~/App_Data/Temp"), stringName);
            //rename file
            var retstringName = "FiscalYear-" + date.ToString("yyyy-MM-dd HHmmss") + ".xlsx";

            return File(res, Tools.GetContentType(".xlsx"), retstringName);
        }

        public JsonResult SaveActivity(BizPlanActivity wellActivity, string eventName, bool virtualPhase = false, bool shiftFutureEventDate = false,
            bool isNewWell = false, string saveToOP = "OP15")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                try
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

                    wellActivity.LESchedule = wellActivity.OpsSchedule;

                    wellActivity.Phases = new List<BizPlanActivityPhase>();
                    BizPlanActivityPhase phase = new BizPlanActivityPhase();
                    //phase.VirtualPhase = virtualPhase;
                    //phase.ShiftFutureEventDate = shiftFutureEventDate;
                    phase.ActivityType = eventName;
                    phase.PhaseNo = 1;

                    phase.PlanSchedule = wellActivity.PsSchedule;
                    phase.PhSchedule = wellActivity.OpsSchedule;
                    phase.LESchedule = new DateRange(); // wellActivity.OpsSchedule;

                    phase.Plan = new WellDrillData()
                    {
                        Cost = 0,
                        Days = phase.PlanSchedule.Days
                    };
                    phase.OP = new WellDrillData()
                    {
                        Cost = 0,
                        Days = phase.PhSchedule.Days
                    };
                    phase.LE = new WellDrillData();

                    var services = 0;
                    var materials = 0;
                    phase.Estimate.Services = services;
                    phase.Estimate.Materials = materials;
                    phase.Estimate.EventStartDate = wellActivity.OpsSchedule.Start;
                    phase.Estimate.EventEndDate = wellActivity.OpsSchedule.Finish;
                    phase.Estimate.EstimatePeriod.Start = wellActivity.OpsSchedule.Start;
                    phase.Estimate.EstimatePeriod.Finish = wellActivity.OpsSchedule.Finish;
                    phase.Estimate.MaturityLevel = "Type 0";
                    phase.Estimate.RigName = wellActivity.RigName;
                    phase.Estimate.NewTroubleFreeUSD = phase.Estimate.NewTroubleFree.Cost;
                    phase.Estimate.NPTCostUSD = phase.Estimate.NewNPTTime.Cost;
                    phase.Estimate.TECOPCostUSD = phase.Estimate.NewTECOPTime.Cost;
                    phase.Estimate.MeanUSD = phase.Estimate.NewMean.Cost;
                    phase.Estimate.SaveToOP = saveToOP;
                    phase.Estimate.TroubleFreeBeforeLC.Days = phase.Estimate.EstimatePeriod.Days;
                    //phase.Estimate.NewTroubleFree.Days = phase.Estimate.EstimatePeriod.Days;

                    phase.Estimate.ServicesUSD = services;
                    phase.Estimate.MaterialsUSD = materials;
                    phase.BaseOP.Add(DefaultOP);
                    phase.FundingType = "";


                    phase.Estimate.Status = "Draft";
                    //meta data missing
                    if (String.IsNullOrEmpty(wellActivity.ProjectName) || String.IsNullOrEmpty(wellActivity.Country) || String.IsNullOrEmpty(wellActivity.ReferenceFactorModel))
                    {
                        phase.Estimate.Status = "Meta Data Missing";
                    }

                    List<IMongoQuery> queries = new List<IMongoQuery>();
                    queries.Add(Query.EQ("Title", "Type 0"));
                    queries.Add(Query.EQ("BaseOP", saveToOP));
                    queries.Add(Query.EQ("Year", wellActivity.OpsSchedule.Start.Year));
                    var getMaturityRisk = MaturityRiskMatrix.Get<MaturityRiskMatrix>(Query.And(queries.ToArray())) ?? new MaturityRiskMatrix();
                    var NPTTime = phase.Estimate.NewTroubleFree.Days * Tools.Div(Tools.Div(getMaturityRisk.NPTTime, 100), 1 - Tools.Div(getMaturityRisk.NPTTime, 100));
                    var NPTCost = phase.Estimate.NewTroubleFree.Cost * Tools.Div(Tools.Div(getMaturityRisk.NPTCost, 100), 1 - Tools.Div(getMaturityRisk.NPTCost, 100));

                    phase.Estimate.NewNPTTime = new WellDrillPercentData() { Days = NPTTime, Cost = NPTCost, PercentCost = getMaturityRisk.NPTCost, PercentDays = getMaturityRisk.NPTTime };

                    var getLatest = WellActivityUpdate.GetById(wellActivity.WellName, wellActivity.UARigSequenceId, phase.PhaseNo, null, true);
                    if (getLatest != null)
                    {
                        phase.Estimate.CurrentNPTTime = new WellDrillPercentData() { Days = getLatest.NPT.Days, Cost = getLatest.NPT.Cost };
                    }
                    else
                    {
                        phase.Estimate.CurrentNPTTime = new WellDrillPercentData() { Days = 0, Cost = 0 };
                    }

                    //TECOP
                    var TECOPTime = phase.Estimate.NewTroubleFree.Days * Tools.Div(Tools.Div(getMaturityRisk.TECOPTime, 100), 1 - Tools.Div(getMaturityRisk.TECOPTime, 100));
                    var TECOPCost = phase.Estimate.NewTroubleFree.Cost * Tools.Div(Tools.Div(getMaturityRisk.TECOPCost, 100), 1 - Tools.Div(getMaturityRisk.TECOPCost, 100));

                    phase.Estimate.NewTECOPTime = new WellDrillPercentData() { PercentDays = getMaturityRisk.TECOPTime, PercentCost = getMaturityRisk.TECOPCost, Days = TECOPTime, Cost = TECOPCost };


                    //material long lead
                    var DateEscStartMaterial = phase.Estimate.EventStartDate;
                    var tangibleValue = 0.0;
                    var monthRequired = 0.0;
                    var actType = "";

                    //if (phase.ActivityType.ToUpper().Contains("DRILLING"))
                    //{
                    //    actType = "DRILLING";
                    //}
                    //else if (phase.ActivityType.ToUpper().Contains("COMPLETION"))
                    //{
                    //    actType = "COMPLETION";
                    //}
                    //else if (phase.ActivityType.ToUpper().Contains("ABANDONMENT"))
                    //{
                    //    actType = "ABANDONMENT";
                    //}

                    //getActCategory
                    var getActCategory = ActivityMaster.Get<ActivityMaster>(Query.EQ("_id", phase.ActivityType));
                    if (getActCategory != null)
                    {
                        actType = getActCategory.ActivityCategory;
                    }

                    if (actType == null)
                    {
                        actType = "";
                    }

                    if (actType != "")
                    {
                        var year = phase.Estimate.EventStartDate.Year;
                        var getLongLead = LongLead.Get<LongLead>(Query.EQ("Title", actType));
                        if (getLongLead != null && getLongLead.Details != null && getLongLead.Details.Count > 0)
                        {
                            var getTangible = getLongLead.Details.Where(y => y.Year.Equals(year)).FirstOrDefault();
                            if (getTangible != null)
                            {
                                tangibleValue = getTangible.TangibleValue;
                                monthRequired = getTangible.MonthRequiredValue;
                                var getMonthLongLead = Convert.ToInt32(-1 * Math.Round(monthRequired));
                                DateEscStartMaterial = phase.Estimate.EventStartDate.AddMonths(getMonthLongLead);
                            }
                        }
                    }
                    else
                    {
                        tangibleValue = 0;
                        monthRequired = 0;
                        var getMonthLongLead = Convert.ToInt32(-1 * Math.Round(monthRequired));
                        DateEscStartMaterial = phase.Estimate.EventStartDate.AddMonths(getMonthLongLead);
                    }

                    if (DateEscStartMaterial < DateTime.Now)
                    {
                        DateEscStartMaterial = DateTime.Now;
                    }

                    phase.Estimate.StartEscDateMaterial = DateEscStartMaterial;
                    phase.Estimate.PercOfMaterialsLongLead = tangibleValue;
                    phase.Estimate.LongLeadMonthRequired = monthRequired;
                    phase.Estimate.NewTroubleFree.Days = phase.LESchedule.Days;

                    phase.Estimate.LastUpdateBy = WebTools.LoginUser.UserName;
                    phase.Estimate.LastUpdate = Tools.ToUTC(DateTime.Now);

                    wellActivity.ShellShare = wellActivity.WorkingInterest;
                    wellActivity.Phases.Add(phase);
                    wellActivity.VirtualPhase = virtualPhase;
                    wellActivity.ShiftFutureEventDate = shiftFutureEventDate;

                    var cfg = Config.GetConfigValue("BizPlanConfig");
                    if (cfg != null && Convert.ToBoolean(cfg))
                    {
                        wellActivity.Phases.FirstOrDefault().PushToWellPlan = true;
                        wellActivity.Save(references: new string[] { "updatetowellplan", "addplanfrombizplan" });
                    }
                    else
                        wellActivity.Save();


                    if (isNewWell)
                    {
                        var newWell = new WellInfo();
                        newWell._id = wellActivity.WellName;
                        try
                        {
                            newWell.Save();
                        }
                        catch (Exception e)
                        {

                        }
                    }


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


        public string GenerateBusPlanExcel(WaterfallBase wb)
        {
            var sort = new SortByBuilder().Ascending(new string[] { "RigName", "Phases.Estimate.EstimatePeriod" });
            var email = WebTools.LoginUser.Email;
            var raw = wb.GetActivitiesForBizPlan(email, sort);

            if (raw.Any())
            {
                Classes.GenerateExcel.BusinessPlanExcel excel = new Classes.GenerateExcel.BusinessPlanExcel();
                string FilePath = ConfigurationManager.AppSettings["BizPlanExcelPath"] + "BizPlan_" + DateTime.Now.ToString().Replace(" ", "").Replace("/", "").Replace("-", "").Replace(":", "") + ".xlsx";

                if (excel.GenerateExcel(raw, Server.MapPath(FilePath)))
                    return FilePath;
                else
                    return "Failed to generate BizPlan Excel File";
            }
            else
                return "Failed to generate BizPlan Excel File";
        }

        public JsonResult GetMaturityValue(string title = "", int year = 0, string baseop = "")
        {

            var ri = new ResultInfo();
            try
            {
                IMongoQuery query = null;
                List<IMongoQuery> queries = new List<IMongoQuery>();
                queries.Add(Query.EQ("Title", title));
                queries.Add(Query.EQ("Year", year));
                queries.Add(Query.EQ("BaseOP", baseop));
                query = Query.And(queries.ToArray());
                var data = MacroEconomic.Populate<MaturityRiskMatrix>(query);
                ri.Data = data;
            }
            catch (Exception e)
            {
                ri.Result = "NOK";
                ri.Message = e.Message;
            }
            return MvcTools.ToJsonResult(ri);
        }


        #region Not Used

        public ActionResult EditWellSequenceInfo(string id)
        {
            ViewBag.id = id;
            return View();
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

        public JsonResult DeleteActivity(int id, int PhaseNo, string ActivityType)
        {
            try
            {
                var activity = BizPlanActivity.Get<BizPlanActivity>(id);

                var qsWAU = new List<IMongoQuery>();
                qsWAU.Add(Query.EQ("WellName", activity.WellName));
                qsWAU.Add(Query.EQ("Phase.ActivityType", ActivityType));
                qsWAU.Add(Query.EQ("Phase.PhaseNo", PhaseNo));
                qsWAU.Add(Query.EQ("SequenceId", activity.UARigSequenceId));

                var qsPIP = new List<IMongoQuery>();
                qsPIP.Add(Query.EQ("WellName", activity.WellName));
                qsWAU.Add(Query.EQ("PhaseNo", PhaseNo));
                qsPIP.Add(Query.EQ("ActivityType", ActivityType));
                qsPIP.Add(Query.EQ("SequenceId", activity.UARigSequenceId));

                if (WellActivityUpdate.Populate<WellActivityUpdate>(Query.And(qsWAU)).Count > 0)
                {
                    return Json(new { Success = false, Message = "Activity that have Phase used in Weekly Report cannot be deleted!" }, JsonRequestBehavior.AllowGet);
                }
                else if (WellPIP.Populate<WellPIP>(Query.And(qsPIP)).Count > 0)
                {
                    return Json(new { Success = false, Message = "Activity that have Phase used in PIP Configuration cannot be deleted!" }, JsonRequestBehavior.AllowGet);
                }
                //MLE
                //else if (WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(Query.EQ("SequenceId", activity.UARigSequenceId)).Count > 0)
                //{
                //    return Json(new { Success = false, Message = "Activity that have Phase used in Monthly Report cannot be deleted!" }, JsonRequestBehavior.AllowGet);
                //}

                //DataHelper.Delete(activity.TableName, Query.EQ("_id", id));
                var newPhases = new List<BizPlanActivityPhase>();
                if (activity.Phases.Any())
                {
                    foreach (var act in activity.Phases)
                    {
                        if (act.PhaseNo.Equals(PhaseNo) && act.ActivityType.Equals(ActivityType))
                        {
                            //activity.Phases.Remove(act);
                        }
                        else
                        {
                            newPhases.Add(act);
                        }
                    }
                }
                activity.Phases = newPhases;
                DataHelper.Save(activity.TableName, activity.ToBsonDocument());

                #region delete actv
                var actv = WellActivity.Get<WellActivity>(id);

                if (actv != null)
                {
                    //DataHelper.Delete(actv.TableName, Query.EQ("_id", id));
                    var phases = new List<WellActivityPhase>();
                    if (actv.Phases.Any())
                    {
                        foreach (var act in actv.Phases)
                        {
                            if (act.PhaseNo.Equals(PhaseNo) && act.ActivityType.Equals(ActivityType))
                            {
                                //actv.Phases.Remove(act);
                            }
                            else
                            {
                                phases.Add(act);
                            }
                        }
                    }
                    actv.Phases = phases;
                    DataHelper.Save(actv.TableName, actv.ToBsonDocument());
                }

                #endregion

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }
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

        public JsonResult CalcRigRates(BizPlanActivityPhase Phase, double ShellShare, string RFM, string Country, string Currencyx, string WellName, string BaseOP)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var maturityLevel = Phase.Estimate.MaturityLevel.Substring(Phase.Estimate.MaturityLevel.Length - 1, 1);
                BizPlanCalculation res = BizPlanCalculation.calcBizPlan(WellName, Phase.Estimate.RigName, Phase.ActivityType, Country, ShellShare, Phase.Estimate.EstimatePeriod, Convert.ToInt32(maturityLevel), Phase.Estimate.Services, Phase.Estimate.Materials, Phase.Estimate.NewTroubleFree.Days, RFM,
                    BaseOP,
                    Tools.Div(Phase.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(Phase.Estimate.NewTECOPTime.PercentDays, 100), Tools.Div(Phase.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(Phase.Estimate.NewTECOPTime.PercentCost, 100), Phase.Estimate.LongLeadMonthRequired, Tools.Div(Phase.Estimate.PercOfMaterialsLongLead, 100), Phase.Estimate.SpreadRateTotal);


                return new { RigRate = res.RigRate, TroubleFreeCost = res.TroubleFreeCost };
            });

        }

        #endregion

    }
}