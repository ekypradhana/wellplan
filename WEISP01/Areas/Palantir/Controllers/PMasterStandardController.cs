using Aspose.Cells;
using ECIS.AppServer.Areas.Shell.Controllers;
using ECIS.Client.WEIS;
using ECIS.Core;
using ECIS.Core.DataSerializer;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ECIS.AppServer.Areas.Palantir.Controllers
{
    public class PMasterStandardController : Controller
    {
        //
        // GET: /Palantir/PMasterStandard/
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GenerateStandardPMaster()
        {
            return View();
        }

        public ActionResult StandardPMasterMapping(string[] mapname)
        {
            var lsInfo = new PIPAnalysisController().GetLatestLsDate();
            ViewBag.LatestLS = lsInfo;
            ViewBag.ListMapName = mapname;

            return View();
        }

        public JsonResult GetListPMasterMapping(ParsePalentirQuery pq, string mapname)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                List<PMasterStandard> doc = new List<PMasterStandard>();
                if (mapname != "Select Map File")
                {
                    var q = new List<IMongoQuery>();
                    q.Add(Query.EQ("MapName", mapname));
                    if (pq.InPlan != "Both")
                    {
                        bool IsInPlan = pq.InPlan == "Yes" ? true : false;
                        q.Add(Query.EQ("IsInPlan", IsInPlan));
                    }
                    doc = PMasterStandard.Populate<PMasterStandard>(Query.And(q));
                }
                else
                {
                    doc = PMasterStandard.Populate<PMasterStandard>();
                }
                return DataHelper.ToDictionaryArray(doc.Select(x => x.ToBsonDocument()).ToList());
            });
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
        //public List<BizPlanActivity> GetBizPlanActivity(WaterfallBase wb, FilterGeneratePlanningReportMap pq)
        public List<BizPlanActivity> GetBizPlanActivity(WaterfallBase wb)
        {
            //List<string> Status = new List<string> { "Modified", "Complete" };
            //string PreviousOP = "";
            //string NextOP = "";
            //string DefaultOP = getBaseOP(out PreviousOP, out NextOP);
            //if (Status == null)
            //{
            //    Status = new List<string>();
            //}
            //var division = 1000000.0;
            //var now = Tools.ToUTC(DateTime.Now);
            //var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
            var email = WebTools.LoginUser.Email;
            wb.PeriodBase = "By Last Sequence";
            var raw = wb.GetActivitiesForPalantir(null, email);

            return raw;
            //var accessibleWells = WebTools.GetWellActivities();

        }

        //public JsonResult GenerateMapPMaster(WaterfallBase wb, FilterGeneratePlanningReportMap pq)
        public JsonResult GenerateMapPMaster(WaterfallBase wb)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                try
                {

                    var res = new List<PMasterStandard>();
                    //var bizplanActivities = GetBizPlanActivity(wb, pq);
                    var bizplanActivities = GetBizPlanActivity(wb);
                    //var actgh = DataHelper.Populate("WEISActivities", q: null, fields: new List<string> { "_id", "ActivityCategory" });

                    foreach (var bz in bizplanActivities)
                    {
                        foreach (var bph in bz.Phases)
                        {
                            //have to check only if the well doesnt have a weekly report, because busplan check that -- busplancontroller : 1577
                            //this is because case on sharepoint : 959
                            var isHaveWeeklyReport = WellActivity.isHaveWeeklyReport(bz.WellName, bz.UARigSequenceId, bph.ActivityType);
                            if (!isHaveWeeklyReport)
                            {
                                PMasterStandard p = new PMasterStandard();
                                p.ActivityType = bph.ActivityType;
                                p.WellName = bz.WellName;
                                p.RigName = bph.Estimate.RigName;

                                var dr = new DateRange(new DateTime(2015, 1, 1), new DateTime(2040, 12, 31));
                                // perf tune up
                                PMasterStandard pms = new PMasterStandard();
                                var resPM = pms.TransformsToMapBizPlan(bz, bph, WebTools.LoginUser.UserName, DateTime.Now.Date, false);
                                res.Add(resPM);
                            }
                        }
                    }
                    return res;
                }
                catch (Exception e)
                {
                    return new { Status = "NOK", Message = e.Message };
                }
            });
        }
        public JsonResult GetPMasterStandardReport(ParsePalentirQuery wb)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var res = DataHelper.Populate<PMasterStandardResult>("WEISPalantirMasterStandardResult");

                var result = new List<BsonDocument>();
                var dataGrid = new List<object>();
                foreach (var t in res)
                {
                    List<BsonDocument> listAnnual = new List<BsonDocument>();
                    if (t.StandardDetails.Count == 0)
                    {
                        var drAnnual = new DateRange(new DateTime(2016, 1, 1), new DateTime(2066, 12, 31));
                        for (int yr = drAnnual.Start.Year; yr <= drAnnual.Finish.Year; )
                        {
                            BsonDocument y = new BsonDocument();
                            y.Set("Year_" + yr.ToString(), 0);
                            listAnnual.Add(y);
                            yr++;
                        }
                    }
                    else
                    {
                        foreach (var annual in t.StandardDetails)
                        {
                            double val = annual.value / 1000000;
                            BsonDocument y = new BsonDocument();
                            y.Set("Year_" + annual.DateId, Math.Round(val, 2));
                            listAnnual.Add(y);
                        }
                    }

                    double avgShellShare = t.AvgShellShare / 100;
                    dataGrid.Add(new
                    {
                        _id = t._id,
                        reportEntity = t.ReportEntity,
                        planningEntity = t.PlanningEntity,
                        planningEntityID = t.PlanningEntityID,
                        activityEntity = t.ActivityEntity,
                        activityEntityID = t.ActivityEntityID,
                        probabilityofSuccess = t.Prob,
                        averageShellShare = Math.Round(avgShellShare, 2),
                        unit = t.Unit,
                        pMasterField = t.PMasterField,
                        pMatserRef = t.PMasterRef,
                        Annual = listAnnual
                    });
                }

                result = dataGrid.Select(x => x.ToBsonDocument()).ToList();

                return DataHelper.ToDictionaryArray(result);
            });
        }

        public JsonResult GenerateResultTransform(ParsePalentirQuery pq, List<PMasterStandard> list, string mapname)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                try
                {
                    //var getPMaster = PMasterStandard.Populate<PMasterStandard>(Query.EQ("ActivityEntityID", activityID));

                    var q = new List<IMongoQuery>();
                    q.Add(Query.EQ("MapName", mapname));
                    var queryElement = pq.ParseQuery();
                    if (queryElement != null)
                    {
                        q.Add(queryElement);
                    }

                    var fieldsMT = pq.MoneyTypeFields();
                    var getListPMaster = PMasterStandard.Populate<PMasterStandard>(Query.And(q), fields: fieldsMT);

                    var getEntity = getListPMaster.GroupBy(x => new
                    {
                        x.ReportEntity,
                        x.PlanningEntity,
                        x.PlanningEntityID,
                        x.ActivityEntity,
                        x.ActivityEntityID,
                        x.Prob
                    }).Select(x => x.Key).ToList();
                    PMasterStandard pm = new PMasterStandard();
                    var result = pm.Transforms(getListPMaster, WebTools.LoginUser.UserName,
                        getEntity.ElementAt(0).ReportEntity,
                        getEntity.ElementAt(0).PlanningEntity,
                        getEntity.ElementAt(0).PlanningEntityID,
                        getEntity.ElementAt(0).ActivityEntity,
                        getEntity.ElementAt(0).ActivityEntityID,
                        getEntity.ElementAt(0).Prob, pq);

                    DataHelper.Delete("WEISPalantirMasterStandardResult", Query.EQ("MapName", mapname));

                    SSTG sstg = new SSTG();
                    result = sstg.GetSSTGPMaster(pq.SSTG, pq.Currency, result);
                    foreach (var r in result)
                    {
                        r.IsGenerated = true;
                        r.MapName = mapname;
                        r.Save();
                    }

                    return new { Success = true, listPmaster = result };
                }
                catch (Exception e)
                {
                    return new { Success = false, Message = e.Message };
                }
            });
        }

        public JsonResult GenerateResultTransform2(ParsePalentirQuery pq, string mapname)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                try
                {
                    //var getPMaster = PMasterStandard.Populate<PMasterStandard>(Query.EQ("ActivityEntityID", activityID));

                    //List<PMasterStandard> list = PMasterStandard.Populate<PMasterStandard>(Query.EQ("MapName", mapname));

                    var q = new List<IMongoQuery>();
                    q.Add(Query.EQ("MapName", mapname));
                    if (pq.InPlan != "Both")
                    {
                        bool IsInPlan = pq.InPlan == "Yes" ? true : false;
                        q.Add(Query.EQ("IsInPlan", IsInPlan));
                    }
                    //q.Add(Query.EQ("UpdateBy", WebTools.LoginUser.UserName));

                    var fieldsMT = pq.MoneyTypeFields();
                    var getListPMaster = PMasterStandard.Populate<PMasterStandard>(Query.And(q), fields: fieldsMT);

                    //pq.InPlan = string.IsNullOrEmpty(pq.InPlan) ? "Both" : pq.InPlan;
                    //switch (pq.InPlan)
                    //{
                    //    case "Yes":
                    //        getListPMaster = getListPMaster.Where(x => x.IsInPlan == true).ToList();
                    //        break;
                    //    case "No":
                    //        getListPMaster = getListPMaster.Where(x => x.IsInPlan == false).ToList();
                    //        break;
                    //    case "Both":
                    //        getListPMaster = getListPMaster.Where(x => x.IsInPlan == true || x.IsInPlan == false).ToList();
                    //        break;
                    //}


                    var getEntity = getListPMaster.GroupBy(x => new
                    {
                        x.ReportEntity,
                        x.PlanningEntity,
                        x.PlanningEntityID,
                        x.ActivityEntity,
                        x.ActivityEntityID,
                        x.Prob
                    }).Select(x => x.Key).ToList();
                    //var getEntity = getListPMaster.GroupBy(x => x.ActivityEntityID).Select(x => x.Key).ToList();
                    PMasterStandard pm = new PMasterStandard();
                    List<PMasterStandardResult> result = new List<PMasterStandardResult>();

                    foreach (var d in getEntity)
                    {
                        if (d.ActivityEntityID != "")
                        {
                            var matchedData = getListPMaster.Where(x => x.ReportEntity == d.ReportEntity && x.PlanningEntity == d.PlanningEntity
                                                && x.PlanningEntityID == d.PlanningEntityID && x.ActivityEntity == d.ActivityEntity &&
                                                x.ActivityEntityID == d.ActivityEntityID && x.Prob == d.Prob
                                                ).ToList();
                            var getresult = pm.Transforms(matchedData, WebTools.LoginUser.UserName,
                            d.ReportEntity,
                            d.PlanningEntity,
                            d.PlanningEntityID,
                            d.ActivityEntity,
                            d.ActivityEntityID,
                            d.Prob, pq);

                            //DataHelper.Delete("WEISPalantirMasterStandardResult", Query.EQ("MapName", mapname));

                            SSTG sstg = new SSTG();
                            getresult = sstg.GetSSTGPMaster(pq.SSTG, pq.Currency, getresult);
                            foreach (var r in getresult)
                            {
                                r.IsGenerated = true;
                                r.MapName = mapname;
                                r.Save();
                                result.Add(r);
                            }
                        }
                    }


                    return new { Success = true, listPmaster = result };
                }
                catch (Exception e)
                {
                    return new { Success = false, Message = e.Message };
                }
            });
        }


        public List<PMasterStandardResult> GeneratePMasterStandardResult(ParsePalentirQuery pq, string mapname)
        {
                try
                {
                    var q = new List<IMongoQuery>();
                    q.Add(Query.EQ("MapName", mapname));
                    if (pq.InPlan != "Both")
                    {
                        bool IsInPlan = pq.InPlan == "Yes" ? true : false;
                        q.Add(Query.EQ("IsInPlan", IsInPlan));
                    }
                    //q.Add(Query.EQ("UpdateBy", WebTools.LoginUser.UserName));

                    var fieldsMT = pq.MoneyTypeFields();
                    var getListPMaster = PMasterStandard.Populate<PMasterStandard>(Query.And(q), fields: fieldsMT);


                    var getEntity = getListPMaster.GroupBy(x => new
                    {
                        x.ReportEntity,
                        x.PlanningEntity,
                        x.PlanningEntityID,
                        x.ActivityEntity,
                        x.ActivityEntityID,
                        x.Prob
                    }).Select(x => x.Key).ToList();
                    //var getEntity = getListPMaster.GroupBy(x => x.ActivityEntityID).Select(x => x.Key).ToList();
                    PMasterStandard pm = new PMasterStandard();
                    List<PMasterStandardResult> result = new List<PMasterStandardResult>();

                    foreach (var d in getEntity)
                    {
                        if (d.ActivityEntityID != "")
                        {
                            var matchedData = getListPMaster.Where(x => x.ReportEntity == d.ReportEntity && x.PlanningEntity == d.PlanningEntity
                                                && x.PlanningEntityID == d.PlanningEntityID && x.ActivityEntity == d.ActivityEntity &&
                                                x.ActivityEntityID == d.ActivityEntityID && x.Prob == d.Prob
                                                ).ToList();
                            var getresult = pm.Transforms(matchedData, WebTools.LoginUser.UserName,
                            d.ReportEntity,
                            d.PlanningEntity,
                            d.PlanningEntityID,
                            d.ActivityEntity,
                            d.ActivityEntityID,
                            d.Prob, pq);

                            //DataHelper.Delete("WEISPalantirMasterStandardResult", Query.EQ("MapName", mapname));

                            SSTG sstg = new SSTG();
                            getresult = sstg.GetSSTGPMaster(pq.SSTG, pq.Currency, getresult);
                            foreach (var r in getresult)
                            {
                                r.IsGenerated = true;
                                r.MapName = mapname;
                                r.Save();
                                result.Add(r);
                            }
                        }
                    }


                    return result;
                }
                catch (Exception e)
                {
                    return new List<PMasterStandardResult>();
                }
        }


        public JsonResult GetMapNames()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var mapfilesmonthly = PMaster.Populate<PMaster>().Select(x => x.MapName).Distinct().ToList();
                List<string> mapOptionMo = new List<string>();
                mapOptionMo.Add("Select Map File");
                mapOptionMo.AddRange(mapfilesmonthly);
                //var s = new BsonDocument();
                //s.Set("text", "Select Map File");
                //s.Set("value", "Select Map File");
                //mapOptionMo.Add(s);
                //foreach (string map in mapfilesmonthly)
                //{
                //    //mapOptionMo.Add(map);
                //    s = new BsonDocument();
                //    s.Set("text", map);
                //    s.Set("value", map);
                //    mapOptionMo.Add(s);
                //}
                return mapOptionMo;
            });
        }

        public JsonResult SavePlanPmasterStandard(List<PMasterStandard> list, string mapnames, bool isUpload, bool fromEdit = false)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                if (list.Count > 0)
                {
                    
                    var _pMaster = list.Select(x => x.ToBsonDocument());
                    var pMasters = _pMaster.Select(x => BsonSerializer.Deserialize<PMaster>(x)).ToList();

                    //for checking purpose
                    var counter = 0;
                    List<string> standardIds = new List<string>();
                    foreach (var g in list)
                    {
                        if (string.IsNullOrEmpty(g.ReportEntity))
                            g.ReportEntity = "";
                        if (string.IsNullOrEmpty(g.PlanningEntity))
                            g.PlanningEntity = "";
                        if (string.IsNullOrEmpty(g.PlanningEntityID))
                            g.PlanningEntityID = "";
                        if (string.IsNullOrEmpty(g.ActivityEntity))
                            g.ActivityEntity = "";
                        if (string.IsNullOrEmpty(g.ActivityEntityID))
                            g.ActivityEntityID = "";

                        if (string.IsNullOrEmpty(g.ActivityCategory))
                            g.ActivityCategory = "";

                        if (string.IsNullOrEmpty(g.FirmOption))
                            g.FirmOption = "";

                        var data = g.ToBsonDocument();
                        var pmasterData = g.GetListPmaster(data, WebTools.LoginUser.UserName, isUpload, g);
                        pmasterData.MapName = mapnames;
                        pmasterData.Allocation = g.Allocation;

                        if (fromEdit)
                        {
                            pmasterData.IsInPlan = g.FirmOption == "In Plan";
                        }
                        
                        pmasterData.Save();
                    }
                    //the Idea is: whenever Pmaster Standard do save, in Pmaster monthly have same data saved. so, loop it 2x. and also all the way around
                    //save for Pmaster Monthly. 

                    foreach (var g in pMasters)
                    {
                        if (string.IsNullOrEmpty(g.ReportEntity))
                            g.ReportEntity = "";
                        if (string.IsNullOrEmpty(g.PlanningEntity))
                            g.PlanningEntity = "";
                        if (string.IsNullOrEmpty(g.PlanningEntityID))
                            g.PlanningEntityID = "";
                        if (string.IsNullOrEmpty(g.ActivityEntity))
                            g.ActivityEntity = "";
                        if (string.IsNullOrEmpty(g.ActivityEntityID))
                            g.ActivityEntityID = "";

                        var data = g.ToBsonDocument();

                        var pmasterData = g.GetListPmaster(data, WebTools.LoginUser.UserName, isUpload,g);
                        pmasterData.MapName = mapnames;
                        pmasterData.Allocation = g.Allocation;
                        if (fromEdit)
                        {
                            pmasterData.IsInPlan = g.FirmOption == "In Plan";
                        }
                        pmasterData.Save();
                    }
                }

                return new { Success = true, listPmaster = list };
            });
        }

        public JsonResult GetDataSPMasterMapping(List<string> lMapNames, List<string> ReportingEntity, List<string> UpdateBy)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                ParsePalentirQuery pq = new ParsePalentirQuery();
                pq.MapName = lMapNames;
                pq.ReportingEntity = ReportingEntity;
                pq.UpdateBy = UpdateBy;
                //pq.DateStart = DateStart;
                //pq.DateFinish = DateFinish;
                var elementQuery = pq.ParseQuery();

                if (elementQuery != null)
                {
                    var pop = PMasterStandard.Populate<PMasterStandard>(Query.And(elementQuery));
                    return pop;
                }
                else
                {
                    var pop = PMasterStandard.Populate<PMasterStandard>();
                    return pop;
                }
            });
        }

        public JsonResult GetDataSPMasterMappingUpdateWithLatestLS(List<string> lMapNames, List<string> ReportingEntity, List<string> UpdateBy)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                ParsePalentirQuery pq = new ParsePalentirQuery();
                pq.MapName = lMapNames;
                pq.ReportingEntity = ReportingEntity;
                pq.UpdateBy = UpdateBy;
                var elementQuery = pq.ParseQuery();

                var pop = new List<PMasterStandard>();
                if (elementQuery != null)
                {
                    pop = PMasterStandard.Populate<PMasterStandard>(Query.And(elementQuery));
                }
                else
                {
                    pop = PMasterStandard.Populate<PMasterStandard>();
                }

                var res = new List<PMasterStandard>();

                //update with latest LS here
                if (pop.Any())
                {
                    foreach (var pm in pop)
                    {
                        //get latest bizplan activities
                        var qBP = new List<IMongoQuery>();
                        qBP.Add(Query.EQ("WellName", pm.WellName));
                        qBP.Add(Query.EQ("Phases.ActivityType", pm.ActivityType));
                        qBP.Add(Query.EQ("UARigSequenceId", pm.UARigSequenceId));
                        qBP.Add(Query.EQ("Phases.Estimate.RigName", pm.RigName));
                        //var bz = BizPlanActivity.Get<BizPlanActivity>(Query.And(qBP));
                        var getBizplan = BizPlanActivity.GetAll(Query.And(qBP));
                        if (getBizplan.Any())
                        {
                            var bz = getBizplan.FirstOrDefault();
                            if (bz.Phases.Any())
                            {
                                foreach (var bph in bz.Phases)
                                {
                                    if (bph.ActivityType == pm.ActivityType)
                                    {
                                        PMasterStandard p = new PMasterStandard();
                                        p.ActivityType = bph.ActivityType;
                                        p.WellName = bz.WellName;
                                        p.RigName = bph.Estimate.RigName;

                                        var dr = new DateRange(new DateTime(2015, 1, 1), new DateTime(2040, 12, 31));
                                        // perf tune up
                                        PMasterStandard pms = new PMasterStandard();
                                        var resPM = pms.TransformsToMapBizPlan(bz, bph, WebTools.LoginUser.UserName, DateTime.Now.Date, false);
                                        res.Add(resPM);
                                    }
                                }
                            }
                        }
                    }
                }

                return res;
            });
        }

        [HttpPost]
        public JsonResult UploadMap()
        {

            return MvcResultInfo.Execute(null, document =>
            {

                    bool success = true;
                    string message = "";

                    HttpPostedFileBase file = Request.Files["UploadedFile"];
                    string fileName = file.FileName;
                    file.SaveAs(Server.MapPath("~/App_Data/Temp/") + fileName);

                    List<PMasterStandard> list = new List<PMasterStandard>();

                    string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp/"), fileName);
                    ExtractionHelper e = new ExtractionHelper();
                    IList<BsonDocument> datas = e.Extract(xlst);
                    foreach (var data in datas)
                    {
                        //if (string.IsNullOrEmpty(data.GetString("Reporting_Entity")) || string.IsNullOrEmpty(data.GetString("Planning_Entity"))
                        //    || string.IsNullOrEmpty(data.GetString("Planning_Entity_ID")) || string.IsNullOrEmpty(data.GetString("Activity_Entity"))
                        //    || string.IsNullOrEmpty(data.GetString("Activity_Entity_ID")) || string.IsNullOrEmpty(data.GetString("Probability_Of_Success")))
                        //{
                        //    success = false;
                        //    message = "Some of your field[s] is empty, please insert first!";
                        //    return Json(new { Success = success, Message = message });
                        //}

                        PMasterStandard map = new PMasterStandard();
                        map.ReportEntity = data.GetString("Reporting_Entity");
                        map.PlanningEntity = data.GetString("Planning_Entity");
                        map.PlanningEntityID = data.GetString("Planning_Entity_ID");
                        map.ActivityEntity = data.GetString("Activity_Entity");
                        map.ActivityEntityID = data.GetString("Activity_Entity_ID");
                        map.Prob = data.GetDouble("Probability_Of_Success");
                        map.ActivityCategory = data.GetString("Activity_Category");
                        map.ActivityType = data.GetString("Activity_Type");
                        map.BaseOP = data.GetString("Base_OP");
                        map.FirmOption = data.GetString("Planning_Classification");
                        map.FundingType = data.GetString("Funding_Type");
                        map.RigName = data.GetString("Rig_Name");
                        map.WellName = data.GetString("Well_Name");
                        map.UARigSequenceId = data.GetString("Sequence_Id");

                        list.Add(map);
                    }

                    var bizplans = BizPlanActivity.GetAll(Query.Null);

                    //insert kan allocationnya sekalian disini,sekalian exclude yang tidak ketemu di bizplan
                    var res = new List<PMasterStandard>();
                    foreach (var data in list)
                    {
                        if (data.Prob <= 1) data.Prob = data.Prob * 100;
                        var getMapValue = bizplans.Where(x => (x.WellName != null && x.WellName.Trim() == data.WellName.Trim()) && (x.UARigSequenceId != null && x.UARigSequenceId.Trim() == data.UARigSequenceId.Trim())).FirstOrDefault();
                        if (getMapValue != null)
                        {
                            var phase = getMapValue.Phases.Where(x => x.ActivityType != null && x.ActivityType.Trim() == data.ActivityType.Trim()).FirstOrDefault();
                            if (phase != null)
                            {
                                var result = data.TransformsToMapBizPlan(getMapValue, phase, WebTools.LoginUser.UserName, DateTime.Now.Date, false, data);
                                res.Add(result);
                            }
                            else
                            {

                            }
                        }
                        else
                        {

                        }
                    }
                    return res;
			});
        }

        //public JsonResult DeleteMap(string[] values, string[] fields, string report, string[] lmapnames)
        //{
        //    string message = "";
        //    List<string> bsonResult = new List<string>();
        //    bool success = deleteMap(values, fields, report, lmapnames, out message, out bsonResult);
        //    return Json(new { Success = success, Message = message, LMapName = bsonResult });
        //}

        public JsonResult DeletePmasterStandard(string mapname)
        {
            try
            {
                //DataHelper.Delete("WEISPalantirMasterStandardMap", Query.And(Query.EQ("UpdateBy", WebTools.LoginUser.UserName), Query.EQ("MapName", mapname)));
                //DataHelper.Delete("WEISPalantirMasterStandardResult", Query.And(Query.EQ("UpdateBy", WebTools.LoginUser.UserName), Query.EQ("MapName", mapname)));

                //DataHelper.Delete("WEISPalantirMasterMap", Query.And(Query.EQ("UpdateBy", WebTools.LoginUser.UserName), Query.EQ("MapName", mapname)));
                //DataHelper.Delete("WEISPalantirMasterResult", Query.And(Query.EQ("UpdateBy", WebTools.LoginUser.UserName), Query.EQ("MapName", mapname)));

                DataHelper.Delete("WEISPalantirMasterMap", Query.EQ("MapName", mapname));
                DataHelper.Delete("WEISPalantirMasterResult", Query.EQ("MapName", mapname));

                DataHelper.Delete("WEISPalantirMasterStandardMap", Query.EQ("MapName", mapname));
                DataHelper.Delete("WEISPalantirMasterStandardResult", Query.EQ("MapName", mapname));

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public List<PMasterStandardResult> GenerateNoFundingTypeReport(ParsePalentirQuery pq, string mapname)
        {
                var q = new List<IMongoQuery>();
                q.Add(Query.EQ("MapName", mapname));
                if (pq.InPlan != "Both")
                {
                    bool IsInPlan = pq.InPlan == "Yes" ? true : false;
                    q.Add(Query.EQ("IsInPlan", IsInPlan));
                }

                var fundingtypes = new List<string>() { "CAPEX", "EXPEX", "ABEX", "C2E", "EXPEX SUCCESS", "OPEX" };
                q.Add(Query.NotIn("FundingType", new BsonArray(fundingtypes.ToArray())));
                var fieldsMT = pq.MoneyTypeFields();
                var getListPMaster = PMasterStandard.Populate<PMasterStandard>(Query.And(q), fields: fieldsMT);

                var getEntity = getListPMaster.GroupBy(x => new
                {
                    x.ReportEntity,
                    x.PlanningEntity,
                    x.PlanningEntityID,
                    x.ActivityEntity,
                    x.ActivityEntityID,
                    x.Prob
                }).Select(x => x.Key).ToList();
                PMasterStandard pm = new PMasterStandard();
                List<PMasterStandardResult> result = new List<PMasterStandardResult>();
                if (getListPMaster.Any())
                {
                    foreach (var d in getEntity)
                    {
                        var getresult = pm.Transforms(getListPMaster, WebTools.LoginUser.UserName,
                        d.ReportEntity,
                        d.PlanningEntity,
                        d.PlanningEntityID,
                        d.ActivityEntity,
                        d.ActivityEntityID,
                        d.Prob, pq, onlyNoFundingType: true);

                        //DataHelper.Delete("WEISPalantirMasterStandardResult", Query.EQ("MapName", mapname));

                        SSTG sstg = new SSTG();
                        getresult = sstg.GetSSTGPMaster(pq.SSTG, pq.Currency, getresult);
                        foreach (var r in getresult)
                        {
                            r.IsGenerated = true;
                            r.MapName = mapname;
                            //r.Save();
                            result.Add(r);
                        }

                    }
                }

                return result;
        }

        public JsonResult ExportPMasterStandard( bool isDetails, ParsePalentirQuery pq, string mapname)
        {
            List<PMasterStandardResult> listPmaster = GeneratePMasterStandardResult(pq, mapname);
            if (!listPmaster.Any())
            {
                return Json(new { Success = false, Path = "", Message = "There is no data to export!" }, JsonRequestBehavior.AllowGet);
            }
            string nameExcel;
            if (isDetails)
                nameExcel = "DataStandardExportTemplate.xlsx";
            else
                nameExcel = "DataMapStandardExportTemplate.xlsx";

            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), nameExcel);
            Workbook wb = new Workbook(xlst);
            var ws = wb.Worksheets[0];
            int startRow = 2;
            foreach (var pmasterd in listPmaster)
            {
                //if (pmasterd.Details != null)
                //{
                ws.Cells["A" + startRow.ToString()].Value = pmasterd.ReportEntity;

                ws.Cells["B" + startRow.ToString()].Value = pmasterd.PlanningEntity;
                ws.Cells["C" + startRow.ToString()].Value = pmasterd.PlanningEntityID;
                ws.Cells["D" + startRow.ToString()].Value = pmasterd.ActivityEntity;
                ws.Cells["E" + startRow.ToString()].Value = pmasterd.ActivityEntityID;
                ws.Cells["F" + startRow.ToString()].Value = pmasterd.Prob + "%";

                ws.Cells["G" + startRow.ToString()].Value = Math.Round(pmasterd.AvgShellShare, 2) + "%";
                ws.Cells["H" + startRow.ToString()].Value = pmasterd.Unit;
                ws.Cells["I" + startRow.ToString()].Value = pmasterd.PMasterField;
                ws.Cells["J" + startRow.ToString()].Value = pmasterd.PMasterRef;

                if (isDetails)
                {
                    if (pmasterd.StandardDetails.Any())
                    {
                        int col = 10;
                        foreach (var detail in pmasterd.StandardDetails)
                        {
                            //double val = monthly.value / 1000000;
                            //var val = Tools.Div(detail.value, 1000000);
                            ws.Cells[startRow - 1, col].Value = Tools.Div(detail.value, 1000000);
                            col++;
                        }
                    }
                    else
                    {
                        for (int i = 10; i <= 60; i++)
                        {
                            ws.Cells[startRow - 1, i].Value = 0.00;
                        }
                    }
                }
                startRow++;
                //}
            }

            if (isDetails)
            {
                //var doc = PMasterStandard.Populate<PMasterStandard>(Query.EQ("MapName", mapname));
                List<PMasterStandard> doc = new List<PMasterStandard>();
                if (mapname != "Select Map File")
                {
                    var mpaNames = new List<string>();
                    mpaNames.Add(mapname);
                    pq.MapName = mpaNames;
                    var queryElement = pq.ParseQuery();
                    doc = PMasterStandard.Populate<PMasterStandard>(queryElement);
                }

                ws = wb.Worksheets[1];
                startRow = 2;
                foreach (var xx in doc)
                {
                    ws.Cells["A" + startRow.ToString()].Value = xx.ReportEntity;
                    ws.Cells["B" + startRow.ToString()].Value = xx.PlanningEntity;
                    ws.Cells["C" + startRow.ToString()].Value = xx.PlanningEntityID;
                    ws.Cells["D" + startRow.ToString()].Value = xx.ActivityEntity;
                    ws.Cells["E" + startRow.ToString()].Value = xx.ActivityEntityID;
                    ws.Cells["F" + startRow.ToString()].Value = xx.Prob + "%";
                    ws.Cells["G" + startRow.ToString()].Value = xx.ActivityCategory;
                    ws.Cells["H" + startRow.ToString()].Value = xx.ActivityType;
                    ws.Cells["I" + startRow.ToString()].Value = xx.BaseOP;
                    ws.Cells["J" + startRow.ToString()].Value = xx.FirmOption;
                    ws.Cells["K" + startRow.ToString()].Value = xx.FundingType;
                    ws.Cells["L" + startRow.ToString()].Value = xx.RigName;
                    ws.Cells["M" + startRow.ToString()].Value = xx.WellName;
                    ws.Cells["N" + startRow.ToString()].Value = xx.UARigSequenceId;
                    startRow++;
                }

                ws = wb.Worksheets[2];
                startRow = 2;
                ws.Cells["A" + startRow.ToString()].Value = pq.InPlan;
                ws.Cells["B" + startRow.ToString()].Value = pq.MoneyType;
                ws.Cells["C" + startRow.ToString()].Value = pq.Currency;
                ws.Cells["D" + startRow.ToString()].Value = pq.SSTG;
                ws.Cells["E" + startRow.ToString()].Value = WebTools.LoginUser.UserName;
                ws.Cells["F" + startRow.ToString()].Value = Tools.ToUTC(DateTime.Now);
                ws.Cells["G" + startRow.ToString()].Value = mapname;
            }


            #region no funding type data
            ws = wb.Worksheets[3];
            startRow = 2;
            var NoFundingTypeData = GenerateNoFundingTypeReport(pq, mapname);
            if (NoFundingTypeData.Any())
            {
                foreach (var pmasterd in NoFundingTypeData)
                {
                    //if (pmasterd.Details != null)
                    //{
                    ws.Cells["A" + startRow.ToString()].Value = pmasterd.ReportEntity;

                    ws.Cells["B" + startRow.ToString()].Value = pmasterd.PlanningEntity;
                    ws.Cells["C" + startRow.ToString()].Value = pmasterd.PlanningEntityID;
                    ws.Cells["D" + startRow.ToString()].Value = pmasterd.ActivityEntity;
                    ws.Cells["E" + startRow.ToString()].Value = pmasterd.ActivityEntityID;
                    ws.Cells["F" + startRow.ToString()].Value = pmasterd.Prob + "%";

                    ws.Cells["G" + startRow.ToString()].Value = Math.Round(pmasterd.AvgShellShare, 2) + "%";
                    ws.Cells["H" + startRow.ToString()].Value = pmasterd.Unit;
                    ws.Cells["I" + startRow.ToString()].Value = pmasterd.PMasterField;
                    ws.Cells["J" + startRow.ToString()].Value = pmasterd.PMasterRef;

                    if (isDetails)
                    {
                        if (pmasterd.StandardDetails.Any())
                        {
                            int col = 10;
                            foreach (var detail in pmasterd.StandardDetails)
                            {
                                //double val = monthly.value / 1000000;
                                //var val = Tools.Div(detail.value, 1000000);
                                ws.Cells[startRow - 1, col].Value = Tools.Div(detail.value, 1000000);
                                col++;
                            }
                        }
                        else
                        {
                            for (int i = 10; i <= 60; i++)
                            {
                                ws.Cells[startRow - 1, i].Value = 0.00;
                            }
                        }
                    }
                    startRow++;
                    //}
                }
            }
            #endregion


            var newFileNameSingle = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                     String.Format("PMasterStandard-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss")));

            string returnName = String.Format("PMasterStandard-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss"));

            wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            return Json(new { Success = true, Path = returnName, Message = "" }, JsonRequestBehavior.AllowGet);
        }

        public FileResult DownloadPMasterFile(string stringName, DateTime date, bool isDetails)
        {
            var res = Path.Combine(Server.MapPath("~/App_Data/Temp"), stringName);
            string retstringName;
            if (isDetails)
                retstringName = "PMasterStandard-" + date.ToString("dd-MMM-yyyy") + ".xlsx";
            else
                retstringName = "PMasterStandardMap-" + date.ToString("dd-MMM-yyyy") + ".xlsx";

            return File(res, Tools.GetContentType(".xlsx"), retstringName);
        }

        //public JsonResult SavePMasterMap(string mapname, List<PMasterStandard> list)
        //{
        //    return MvcResultInfo.Execute(null, document =>
        //    {
        //        try
        //        {
        //            DataHelper.Delete("WEISPalantirMasterStandardMap", Query.EQ("MapName", mapname));
        //            List<PMasterStandard> result = new List<PMasterStandard>();
        //            if (list.Count > 0)
        //            {
        //                DataHelper.Delete("WEISPalantirMasterStandardResult", Query.EQ("MapName", mapname));
        //                DataHelper.Delete("WEISPalantirMasterStandardMap", Query.EQ("MapName", mapname));

        //                PMasterStandardResult map = new PMasterStandardResult();
        //                foreach (var g in list)
        //                {
        //                    if (string.IsNullOrEmpty(g.ReportEntity))
        //                        g.ReportEntity = "";
        //                    if (string.IsNullOrEmpty(g.PlanningEntity))
        //                        g.PlanningEntity = "";
        //                    if (string.IsNullOrEmpty(g.PlanningEntityID))
        //                        g.PlanningEntityID = "";
        //                    if (string.IsNullOrEmpty(g.ActivityEntity))
        //                        g.ActivityEntity = "";
        //                    if (string.IsNullOrEmpty(g.ActivityEntityID))
        //                        g.ActivityEntityID = "";

        //                    if (string.IsNullOrEmpty(g.ActivityCategory))
        //                        g.ActivityCategory = "";

        //                    if (string.IsNullOrEmpty(g.FirmOption))
        //                        g.FirmOption = "";

        //                    var data = g.ToBsonDocument();
        //                    var pmasterData = g.GetListPmaster(data, WebTools.LoginUser.UserName, isUpload);
        //                    result.Add(pmasterData);
        //                    pmasterData.MapName = mapname;
        //                    pmasterData.Save();
        //                }
        //            }

        //            return new { Success = "OK", listPmaster = result };
        //        }
        //        catch (Exception e)
        //        {
        //            return new { Status = "NOK", Message = e.Message };
        //        }
        //    });

        //}

        public JsonResult UpdateLS(List<PMasterStandard> listMap)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var latestSeq = DataHelper.Populate("LogLatestLS");
                foreach (var dataMap in listMap)
                {
                    var index = latestSeq.FindIndex(x => x.GetString("Rig_Name") == dataMap.RigName && x.GetString("Well_Name") == dataMap.WellName &&
                            x.GetString("Activity_Type") == dataMap.ActivityType);
                    latestSeq.RemoveAt(index);
                }

                foreach (var sq in latestSeq)
                {
                    PMasterStandard p = new PMasterStandard();
                    p.RigName = sq.GetString("Rig_Name");
                    p.WellName = sq.GetString("Well_Name");
                    p.ActivityType = sq.GetString("Activity_Type");
                    listMap.Add(p);
                }


                return new { listSeq = listMap };
            });
        }
    }
}