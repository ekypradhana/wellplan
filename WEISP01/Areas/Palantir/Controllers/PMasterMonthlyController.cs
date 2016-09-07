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
    [ECAuth()]
    public class PMasterMonthlyController : Controller
    {
        // GET: Palantir/PMasterMonthly
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GeneratePMaster()
        {
            return View();
        }

        public ActionResult PMasterMapping(string[] mapname)
        {
            var lsInfo = new PIPAnalysisController().GetLatestLsDate();
            ViewBag.LatestLS = lsInfo;
            ViewBag.ListMapName = mapname;

            return View();
        }

        public JsonResult GetPMasterMonthlyReport(ParsePalentirQuery pq)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var parseQuery = pq.ParseQuery();
                var res = DataHelper.Populate<PMasterResult>("WEISPalantirMasterResult", parseQuery);
                //var res = DataHelper.Populate<PMasterResult>("WEISPalantirMasterResult");

                var dataGrid = new List<object>();
                var result = new List<BsonDocument>();
                foreach (var t in res)
                {
                    List<BsonDocument> listMonth = new List<BsonDocument>();
                    List<BsonDocument> listAnnual = new List<BsonDocument>();
                    if (t.Details.Count == 0)
                    {
                        var drMonthly = new DateRange(new DateTime(2016, 1, 1), new DateTime(2021, 12, 31));
                        var drAnnual = new DateRange(new DateTime(2022, 1, 1), new DateTime(2066, 12, 31));
                        for (int yr = drMonthly.Start.Year; yr <= drMonthly.Finish.Year; )
                        {
                            for (int mt = drMonthly.Start.Month; mt <= drMonthly.Finish.Month; )
                            {
                                BsonDocument m = new BsonDocument();
                                m.Set("Month_" + mt.ToString() + "_" + yr.ToString(), 0);
                                listMonth.Add(m);
                                mt++;
                            }
                            yr++;
                        }


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
                        foreach (var monthly in t.Details)
                        {
                            BsonDocument m = new BsonDocument();
                            BsonDocument y = new BsonDocument();
                            double val = monthly.value / 1000000;
                            if (monthly.Type == "Month")
                            {
                                m.Set("Month_" + monthly.DateId.ToString().Substring(4, 2) + "_" + monthly.DateId.ToString().Substring(0, 4), Math.Round(val, 2));
                                listMonth.Add(m);
                            }
                            else
                            {
                                y.Set("Year_" + monthly.DateId, Math.Round(val, 2));
                                listAnnual.Add(y);
                            }
                        }
                    }

                    double avgShellShare = t.AvgShellShare / 100;
                    dataGrid.Add(new
                    {
                        Success = true,
                        _id = t._id,
                        ReportEntitiy = t.ReportEntitiy,
                        PlanningEntity = t.PlanningEntity,
                        PlanningEntityID = t.PlanningEntityID,
                        ActivityEntity = t.ActivityEntity,
                        ActivityEntityID = t.ActivityEntityID,
                        Prob = t.Prob,
                        avgShellShare = Math.Round(avgShellShare, 2),
                        Unit = t.Unit,
                        PMasterField = t.PMasterField,
                        PMasterRef = t.PMasterRef,
                        Monthly = listMonth,
                        Annual = listAnnual
                    });
                }

                result = dataGrid.Select(x => x.ToBsonDocument()).ToList();

                return DataHelper.ToDictionaryArray(result);
            });
        }

        public JsonResult GetListPMasterMapping(ParsePalentirQuery pq, string mapname)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                List<PMaster> doc = new List<PMaster>();
                if (mapname != "Select Map File")
                {
                    var q = new List<IMongoQuery>();
                    q.Add(Query.EQ("MapName", mapname));
                    if (pq.InPlan != "Both")
                    {
                        bool IsInPlan = pq.InPlan == "Yes" ? true : false;
                        q.Add(Query.EQ("IsInPlan", IsInPlan));
                    }
                    doc = PMaster.Populate<PMaster>(Query.And(q));
                }
                else
                {
                    doc = PMaster.Populate<PMaster>();
                }

                return DataHelper.ToDictionaryArray(doc.Select(x => x.ToBsonDocument()).ToList());
            });
        }

        //public JsonResult GenerateMapPMaster(WaterfallBase wb, FilterGeneratePlanningReportMap pq)
        public JsonResult GenerateMapPMaster(WaterfallBase wb)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                try
                {
                    var res = new List<PMaster>();
                    PMasterStandardController ps = new PMasterStandardController();
                    //var bizplanActivities = ps.GetBizPlanActivity(wb, pq);
                    var bizplanActivities = ps.GetBizPlanActivity(wb);
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
                                PMaster p = new PMaster();
                                p.ActivityType = bph.ActivityType;
                                p.WellName = bz.WellName;
                                p.RigName = bph.Estimate.RigName;

                                var dr = new DateRange(new DateTime(2015, 1, 1), new DateTime(2040, 12, 31));
                                // perf tune up
                                PMaster pms = new PMaster();
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

        public List<PMasterResult> GenerateNoFundingTypeReport(ParsePalentirQuery pq, string mapname)
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
            var getListPMaster = PMaster.Populate<PMaster>(Query.And(q), fields: fieldsMT);

            var getEntity = getListPMaster.GroupBy(x => new
            {
                x.ReportEntity,
                x.PlanningEntity,
                x.PlanningEntityID,
                x.ActivityEntity,
                x.ActivityEntityID,
                x.Prob
            }).Select(x => x.Key).ToList();
            PMaster pm = new PMaster();
            List<PMasterResult> result = new List<PMasterResult>();
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

        public JsonResult GenerateResultTransform(ParsePalentirQuery pq, string mapname)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                try
                {
                    bool success = false;
                    var q = new List<IMongoQuery>();
                    q.Add(Query.EQ("MapName", mapname));
                    if (pq.InPlan != "Both")
                    {
                        bool IsInPlan = pq.InPlan == "Yes" ? true : false;
                        q.Add(Query.EQ("IsInPlan", IsInPlan));
                    }
                    //var queryElement = pq.ParseQuery();
                    //if (queryElement != null)
                    //{
                    //    q.Add(queryElement);   
                    //}
                    var fieldsMT = pq.MoneyTypeFields();
                    var getListPMaster = PMaster.Populate<PMaster>(Query.And(q), fields: fieldsMT);
                    var result = new List<PMasterResult>();
                    getListPMaster.ForEach(x => x.MapName = mapname);
                    if (getListPMaster.Any())
                    {
                        success = true;
                        var getEntity = getListPMaster.GroupBy(x => new
                        {
                            x.ReportEntity,
                            x.PlanningEntity,
                            x.PlanningEntityID,
                            x.ActivityEntity,
                            x.ActivityEntityID,
                            x.Prob
                        }).Select(x => x.Key).ToList();
                        PMaster pm = new PMaster();
                        foreach (var entity in getEntity)
                        {
                            var d = entity;
                            if (d.ActivityEntityID != "")
                            {
                                var matchedData = getListPMaster.Where(x => x.ReportEntity == d.ReportEntity && x.PlanningEntity == d.PlanningEntity
                                                && x.PlanningEntityID == d.PlanningEntityID && x.ActivityEntity == d.ActivityEntity &&
                                                x.ActivityEntityID == d.ActivityEntityID && x.Prob == d.Prob
                                                ).ToList();
                                var getresult = pm.Transforms(matchedData, WebTools.LoginUser.UserName,
                                    entity.ReportEntity, entity.PlanningEntity, entity.PlanningEntityID, entity.ActivityEntity, entity.ActivityEntityID,
                                    entity.Prob, pq);

                                SSTG sstg = new SSTG();
                                getresult = sstg.GetSSTGPMaster(pq.SSTG, pq.Currency, getresult);
                                foreach (var r in getresult)
                                {
                                    r.IsGenerated = true;
                                    r.Save();
                                    result.Add(r);
                                }
                            }
                        }
                        //result = pm.Transforms(getListPMaster, WebTools.LoginUser.UserName,
                        //    getEntity.ElementAt(0).ReportEntity,
                        //    getEntity.ElementAt(0).PlanningEntity,
                        //    getEntity.ElementAt(0).PlanningEntityID,
                        //    getEntity.ElementAt(0).ActivityEntity,
                        //    getEntity.ElementAt(0).ActivityEntityID,
                        //    getEntity.ElementAt(0).Prob);
                    }
                    
                    return new { Success = success, listPmaster = result };
                }
                catch (Exception e)
                {
                    return new { Success = false, Message = e.Message};
                }
            });
        }


        public List<PMasterResult> GenerateListPMasterMonthlyReport(ParsePalentirQuery pq, string mapname)
        {
                try
                {
                    bool success = false;
                    var q = new List<IMongoQuery>();
                    q.Add(Query.EQ("MapName", mapname));
                    if (pq.InPlan != "Both")
                    {
                        bool IsInPlan = pq.InPlan == "Yes" ? true : false;
                        q.Add(Query.EQ("IsInPlan", IsInPlan));
                    }
                    var fieldsMT = pq.MoneyTypeFields();
                    var getListPMaster = PMaster.Populate<PMaster>(Query.And(q), fields: fieldsMT);
                    var result = new List<PMasterResult>();
                    getListPMaster.ForEach(x => x.MapName = mapname);
                    if (getListPMaster.Any())
                    {
                        success = true;
                        var getEntity = getListPMaster.GroupBy(x => new
                        {
                            x.ReportEntity,
                            x.PlanningEntity,
                            x.PlanningEntityID,
                            x.ActivityEntity,
                            x.ActivityEntityID,
                            x.Prob
                        }).Select(x => x.Key).ToList();
                        PMaster pm = new PMaster();
                        foreach (var entity in getEntity)
                        {
                            var d = entity;
                            if (d.ActivityEntityID != "")
                            {
                                var matchedData = getListPMaster.Where(x => x.ReportEntity == d.ReportEntity && x.PlanningEntity == d.PlanningEntity
                                                && x.PlanningEntityID == d.PlanningEntityID && x.ActivityEntity == d.ActivityEntity &&
                                                x.ActivityEntityID == d.ActivityEntityID && x.Prob == d.Prob
                                                ).ToList();
                                var getresult = pm.Transforms(matchedData, WebTools.LoginUser.UserName,
                                    entity.ReportEntity, entity.PlanningEntity, entity.PlanningEntityID, entity.ActivityEntity, entity.ActivityEntityID,
                                    entity.Prob, pq);

                                SSTG sstg = new SSTG();
                                getresult = sstg.GetSSTGPMaster(pq.SSTG, pq.Currency, getresult);
                                foreach (var r in getresult)
                                {
                                    r.IsGenerated = true;
                                    r.Save();
                                    result.Add(r);
                                }
                            }
                        }
                    }

                    return result;
                }
                catch (Exception e)
                {
                    return new List<PMasterResult>();
                }
        }


        public JsonResult GetDataMPMasterMapping(List<string> lMapNames, List<string> ReportingEntity, List<string> UpdateBy)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                ParsePalentirQuery pq = new ParsePalentirQuery();
                //pq.ActivityEntityId = lActivityEid;
                pq.MapName = lMapNames;
                pq.ReportingEntity = ReportingEntity;
                pq.UpdateBy = UpdateBy;
                //pq.DateStart = DateStart;
                //pq.DateFinish = DateFinish;
                var elementQuery = pq.ParseQuery();

                if (elementQuery != null)
                {
                    var pop = PMaster.Populate<PMaster>(Query.And(elementQuery));
                    return pop;
                }
                else
                {
                    var pop = PMaster.Populate<PMaster>();
                    return pop;
                }
            });
        }

        public JsonResult GetDataMPMasterMappingUpdateWithLatestLS(List<string> lMapNames, List<string> ReportingEntity, List<string> UpdateBy)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                ParsePalentirQuery pq = new ParsePalentirQuery();
                pq.MapName = lMapNames;
                pq.ReportingEntity = ReportingEntity;
                pq.UpdateBy = UpdateBy;
                var elementQuery = pq.ParseQuery();

                var pop = new List<PMaster>();
                if (elementQuery != null)
                {
                    pop = PMaster.Populate<PMaster>(Query.And(elementQuery));
                }
                else
                {
                    pop = PMaster.Populate<PMaster>();
                }

                var res = new List<PMaster>();

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
                                        PMaster p = new PMaster();
                                        p.ActivityType = bph.ActivityType;
                                        p.WellName = bz.WellName;
                                        p.RigName = bph.Estimate.RigName;

                                        var dr = new DateRange(new DateTime(2015, 1, 1), new DateTime(2040, 12, 31));
                                        // perf tune up
                                        PMaster pms = new PMaster();
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

        [HttpPost]
        public JsonResult UploadMap()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                bool success = true;
                string message = "";

                HttpPostedFileBase file = Request.Files["UploadedFile"]; //Uploaded file
                string fileName = file.FileName;

                //To save file, use SaveAs method
                file.SaveAs(Server.MapPath("~/App_Data/Temp/") + fileName); //File will be saved in application root

                List<PMaster> list = new List<PMaster>();

                string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp/"), fileName);
                ExtractionHelper e = new ExtractionHelper();
                IList<BsonDocument> datas = e.Extract(xlst);
                foreach (var data in datas)
                {
                    //var pmasterData = map.GetListPmaster(data, WebTools.LoginUser.UserName);
                    PMaster map = new PMaster();
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
                var res = new List<PMaster>();
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

        public JsonResult ExportPMasterMonthly(bool isDetails, ParsePalentirQuery pq, string mapname)
        {
            List<PMasterResult> listPmaster = GenerateListPMasterMonthlyReport(pq, mapname);
            if (!listPmaster.Any())
            {
                return Json(new { Success = false, Path = "", Message = "There is no data to export!" }, JsonRequestBehavior.AllowGet);
            }

            string nameExcel;
            if (isDetails)
                nameExcel = "DataMonthlyExportTemplate.xlsx";
            else
                nameExcel = "DataMapMonthlyExportTemplate.xlsx";
            
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), nameExcel);
            Workbook wb = new Workbook(xlst);
            var ws = wb.Worksheets[0];
            int startRow = 2;
            foreach (var pmasterd in listPmaster)
            {
                //if (pmasterd.Details != null)
                //{
                    ws.Cells["A" + startRow.ToString()].Value = pmasterd.ReportEntitiy;

                    ws.Cells["B" + startRow.ToString()].Value = pmasterd.PlanningEntity;
                    ws.Cells["C" + startRow.ToString()].Value = pmasterd.PlanningEntityID;
                    ws.Cells["D" + startRow.ToString()].Value = pmasterd.ActivityEntity;
                    //ws.Cells.SetColumnWidth(4, 12); //column E
                    ws.Cells["E" + startRow.ToString()].Value = pmasterd.ActivityEntityID;
                    //ws.Cells.SetColumnWidth(5, 18); //column F
                    ws.Cells["F" + startRow.ToString()].Value = pmasterd.Prob + "%";
                    ws.Cells["G" + startRow.ToString()].Value = Math.Round(pmasterd.AvgShellShare, 2) + "%";
                    ws.Cells["H" + startRow.ToString()].Value = pmasterd.Unit;
                    ws.Cells["I" + startRow.ToString()].Value = pmasterd.PMasterField;
                    ws.Cells["J" + startRow.ToString()].Value = pmasterd.PMasterRef;

                    if (isDetails)
                    {
                        if (pmasterd.Details.Any()){
                            int col = 10;
                            foreach (var monthly in pmasterd.Details)
                            {
                                //double val = monthly.value / 1000000;
                                //var val = Tools.Div(monthly.value, 1000000);
                                ws.Cells.SetColumnWidth(col, 10);
                                if (monthly.Type == "Month")
                                {
                                    ws.Cells[startRow - 1, col].Value = Tools.Div(monthly.value, 1000000);
                                }
                                else
                                {
                                    ws.Cells[startRow - 1, col].Value = Tools.Div(monthly.value, 1000000);
                                }
                                col++;
                            }
                        }
                        else
                        {
                            for (int i = 10; i < 127; i++)
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
                List<PMaster> doc = new List<PMaster>();
                if (mapname != "Select Map File")
                {
                    var mpaNames = new List<string>();
                    mpaNames.Add(mapname);
                    pq.MapName = mpaNames;
                    var queryElement = pq.ParseQuery();
                    doc = PMaster.Populate<PMaster>(queryElement);
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


            #region invalid funding type report

            ws = wb.Worksheets[3];
            startRow = 2;
            var NoFundingTypeData = GenerateNoFundingTypeReport(pq, mapname);
            if (NoFundingTypeData.Any())
            {
                foreach (var pmasterd in NoFundingTypeData)
                {
                    //if (pmasterd.Details != null)
                    //{
                    ws.Cells["A" + startRow.ToString()].Value = pmasterd.ReportEntitiy;

                    ws.Cells["B" + startRow.ToString()].Value = pmasterd.PlanningEntity;
                    ws.Cells["C" + startRow.ToString()].Value = pmasterd.PlanningEntityID;
                    ws.Cells["D" + startRow.ToString()].Value = pmasterd.ActivityEntity;
                    //ws.Cells.SetColumnWidth(4, 12); //column E
                    ws.Cells["E" + startRow.ToString()].Value = pmasterd.ActivityEntityID;
                    //ws.Cells.SetColumnWidth(5, 18); //column F
                    ws.Cells["F" + startRow.ToString()].Value = pmasterd.Prob + "%";
                    ws.Cells["G" + startRow.ToString()].Value = Math.Round(pmasterd.AvgShellShare, 2) + "%";
                    ws.Cells["H" + startRow.ToString()].Value = pmasterd.Unit;
                    ws.Cells["I" + startRow.ToString()].Value = pmasterd.PMasterField;
                    ws.Cells["J" + startRow.ToString()].Value = pmasterd.PMasterRef;

                    if (isDetails)
                    {
                        if (pmasterd.Details.Any())
                        {
                            int col = 10;
                            foreach (var monthly in pmasterd.Details)
                            {
                                //double val = monthly.value / 1000000;
                                //var val = Tools.Div(monthly.value, 1000000);
                                ws.Cells.SetColumnWidth(col, 10);
                                if (monthly.Type == "Month")
                                {
                                    ws.Cells[startRow - 1, col].Value = Tools.Div(monthly.value, 1000000);
                                }
                                else
                                {
                                    ws.Cells[startRow - 1, col].Value = Tools.Div(monthly.value, 1000000);
                                }
                                col++;
                            }
                        }
                        else
                        {
                            for (int i = 10; i < 127; i++)
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
                     String.Format("PMasterMonthly-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss")));

            string returnName = String.Format("PMasterMonthly-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss"));

            wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            return Json(new { Success = true, Path = returnName, Message = "" }, JsonRequestBehavior.AllowGet);
        }

        public FileResult DownloadPMasterFile(string stringName, DateTime date, bool isDetails)
        {
            var res = Path.Combine(Server.MapPath("~/App_Data/Temp"), stringName);
            string retstringName;
            if (isDetails)
                retstringName = "PMasterMonthly-" + date.ToString("dd-MMM-yyyy") + ".xlsx";
            else
                retstringName = "PMasterMonthlyMap-" + date.ToString("dd-MMM-yyyy") + ".xlsx";
            
            return File(res, Tools.GetContentType(".xlsx"), retstringName);
        }

        public JsonResult SavePlanPmaster(List<PMaster> list, string mapnames, bool isUpload, bool fromEdit = false)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                try
                {
                    List<PMaster> listMap = new List<PMaster>();
                    if (list.Count > 0)
                    {
                        var _pMasterStandards = list.Select(x => x.ToBsonDocument());
                        var pMasterStandards = _pMasterStandards.Select(x => BsonSerializer.Deserialize<PMasterStandard>(x)).ToList();

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

                            var data = g.ToBsonDocument();

                            var pmasterData = g.GetListPmaster(data, WebTools.LoginUser.UserName, isUpload,g);
                            pmasterData.MapName = mapnames;

                            if (fromEdit)
                            {
                                pmasterData.IsInPlan = g.FirmOption == "In Plan";
                            }

                            pmasterData.Save();
                            listMap.Add(pmasterData);
                        }

                        //the Idea is: whenever Pmaster monthly do save, in Pmaster standard have same data saved. so, loop it 2x. and also all the way around
                        //save for Pmaster Standard. 
                        foreach (var g in pMasterStandards)
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


                    }
                    return new { Success = "OK", listPmaster = listMap };
                }
                catch (Exception e)
                {
                    return new { Status = "NOK", Message = e.Message };
                }
                
                
               
                
            });
        }

        public JsonResult DeletePmasterMonthly(string mapname)
        {
            try
            {
                //DataHelper.Delete("WEISPalantirMasterMap", Query.And(Query.EQ("UpdateBy", WebTools.LoginUser.UserName), Query.EQ("MapName", mapname)));
                DataHelper.Delete("WEISPalantirMasterMap", Query.EQ("MapName", mapname));
                DataHelper.Delete("WEISPalantirMasterResult", Query.EQ("MapName", mapname));

                //DataHelper.Delete("WEISPalantirMasterStandardMap", Query.And(Query.EQ("UpdateBy", WebTools.LoginUser.UserName), Query.EQ("MapName", mapname)));
                DataHelper.Delete("WEISPalantirMasterStandardMap", Query.EQ("MapName", mapname));
                DataHelper.Delete("WEISPalantirMasterStandardResult", Query.EQ("MapName", mapname));

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        //public JsonResult SavePMasterMap(string mapname)//, List<PMaster> list)
        //{
        //    return MvcResultInfo.Execute(null, document =>
        //    {
        //        try
        //        {
        //            List<PMaster> result = new List<PMaster>();
        //            List<IMongoQuery> queries = new List<IMongoQuery>();
        //            queries.Add(Query.EQ("UpdateBy", WebTools.LoginUser.UserName));
        //            queries.Add(Query.EQ("MapName", mapname));

        //            DataHelper.Delete("WEISPalantirMasterResult", Query.And(queries));
        //            DataHelper.Delete("WEISPalantirMasterMap", Query.And(queries));

        //            var pMasterResult = PMaster.Populate<PMaster>(Query.And(queries));
        //            PMasterResult map = new PMasterResult();
        //            foreach (var g in pMasterResult)
        //            {
        //                if (string.IsNullOrEmpty(g.ReportEntity))
        //                    g.ReportEntity = "";
        //                if (string.IsNullOrEmpty(g.PlanningEntity))
        //                    g.PlanningEntity = "";
        //                if (string.IsNullOrEmpty(g.PlanningEntityID))
        //                    g.PlanningEntityID = "";
        //                if (string.IsNullOrEmpty(g.ActivityEntity))
        //                    g.ActivityEntity = "";
        //                if (string.IsNullOrEmpty(g.ActivityEntityID))
        //                    g.ActivityEntityID = "";

        //                var wa = WellActivity.Get<WellActivity>(Query.And(Query.EQ("WellName", g.WellName), Query.EQ("UARigSequenceId", g.UARigSequenceId)));
        //                var data = g.ToBsonDocument();
        //                var pmasterData = map.GetListPmaster(data, WebTools.LoginUser.UserName);
        //                pmasterData.MapName = mapname;
        //                result.Add(pmasterData);
        //                pmasterData.Save();
        //            }
                    
        //            return new { Success = "OK", listPmaster = result };
        //        }
        //        catch (Exception e)
        //        {
        //            return new { Status = "NOK", Message = e.Message };
        //        }
        //    });
            
        //}

        public JsonResult UpdateLS(List<PMaster> listMap)
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
                    PMaster p = new PMaster();
                    p.RigName = sq.GetString("Rig_Name");
                    p.WellName = sq.GetString("Well_Name");
                    p.ActivityType = sq.GetString("Activity_Type");

                    var dr = new DateRange(new DateTime(2015, 1, 1), new DateTime(2040, 12, 31));

                    WellActivity act = WellActivity.Get<WellActivity>(Query.And(
                        Query.EQ("WellName", p.WellName),
                        Query.EQ("RigName", p.RigName),
                        Query.EQ("Phases.ActivityType", p.ActivityType)
                        ));

                    PMaster pms = new PMaster();
                    var resPM = pms.TransformsToMap(act, act.Phases, WebTools.LoginUser.UserName, DateTime.Now.Date, false);
                    listMap.Add(resPM);
                    //res.Add(resPM);
                }


                return new { listSeq = listMap };
            });
        }
    }
}