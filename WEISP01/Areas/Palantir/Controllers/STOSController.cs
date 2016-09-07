using Aspose.Cells;
using ECIS.Client.WEIS;
using ECIS.Core;
using ECIS.Core.DataSerializer;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
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
    public class STOSController : Controller
    {
        // GET: Palantir/STOS
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GenerateSTOS()
        {
            return View();
        }

        public ActionResult STOSMapping(string[] mapname)
        {
            ViewBag.ListMapName = mapname;
            return View();
        }

        private List<STOSResult> CreateNewMap(IMongoQuery queryElement)
        {
            var tx = DataHelper.Populate("LogLatestLS");
            var raw = BizPlanActivity.Populate<BizPlanActivity>(queryElement);
            var grp = tx.Select(x => x.GetString("Well_Name")).Distinct();//.GroupBy(x => x.GetString("WellName")); //tx.GroupBy(x => x.GetString("Well_Name"));

            List<STOSResult> stos = new List<STOSResult>();
            foreach (string g in grp)
            {
                if (g.Equals("APPO AC006"))
                { }

                STOSResult px = new STOSResult();
                var data = raw.Where(x => x.WellName.Equals(g.ToString())).ToList();
                var gogo = px.Transforms(data, WebTools.LoginUser.UserName);
                foreach (var t in gogo)
                {
                    stos.Add(t);
                }
            }
            return stos;
        }

        private List<STOS> CreateNewMapSTos(IMongoQuery queryElement)
        {
            var tx = DataHelper.Populate("LogLatestLS");
            var raw = BizPlanActivity.Populate<BizPlanActivity>(queryElement);
            var grp = tx.Select(x => x.GetString("Well_Name")).Distinct();//.GroupBy(x => x.GetString("WellName")); //tx.GroupBy(x => x.GetString("Well_Name"));

            List<STOS> stos = new List<STOS>();
            foreach (string g in grp)
            {
                if (g.Equals("APPO AC006"))
                { }

                STOS px = new STOS();
                var data = raw.Where(x => x.WellName.Equals(g.ToString())).ToList();
                var gogo = px.TransformsToMapBizPlan(data, WebTools.LoginUser.UserName);
                foreach (var t in gogo)
                {
                    stos.Add(t);
                }
            }
            return stos;
        }

        private IMongoQuery QueryElm(WaterfallBase wb, ParsePalentirQuery pq)
        {
            List<IMongoQuery> queries = new List<IMongoQuery>();
            var queryElement = pq.ParseQuery();
            if (queryElement != null)
            {
                queries.Add(queryElement);
            }
            if (wb.WellNames != null && wb.WellNames.Count() > 0)
            {
                BsonArray qactivity = new BsonArray();
                foreach (var activity in wb.WellNames)
                    qactivity.Add(activity);
                queries.Add(Query.In("WellName", qactivity));

            }


            if (wb.ProjectNames != null && wb.ProjectNames.Count() > 0)
            {
                BsonArray qactivity = new BsonArray();
                foreach (var activity in wb.ProjectNames)
                    qactivity.Add(activity);
                queries.Add(Query.In("ProjectName", qactivity));

            }

            IMongoQuery query = queries.Count() > 0 ? Query.And(queries.ToArray()) : null;
            return query;
        }

        //GenerateMapStos
        public JsonResult GenerateMapStos(ParsePalentirQuery pq)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                pq.isGenerated = true;
                pq.isSTOS = true;
                pq.InPlan = null;
                var queryElement = pq.ParseQuery();
                //var stos = CreateNewMap(queryElement);
                var stos = CreateNewMapSTos(queryElement);

                //var tx = DataHelper.Populate("LogLatestLS");
                //var raw = BizPlanActivity.Populate<BizPlanActivity>(queryElement);
                //var grp = tx.Select(x => x.GetString("Well_Name")).Distinct();//.GroupBy(x => x.GetString("WellName")); //tx.GroupBy(x => x.GetString("Well_Name"));

                //List<STOS> stos = new List<STOS>();
                //foreach (string g in grp)
                //{
                //    if (g.Equals("APPO AC006"))
                //    { }

                //    STOS px = new STOS();
                //    var data = raw.Where(x => x.WellName.Equals(g.ToString())).ToList();
                //    var gogo = px.Transforms(data, WebTools.LoginUser.UserName);
                //    foreach (var t in gogo)
                //    {
                //        stos.Add(t);
                //    }
                //}
                //SaveSTOSReport(stos, "map1");
                //var dataGrid = SaveSTOSReport();

                return stos;

            });
        }

        public JsonResult GenerateResultTransform(ParsePalentirQuery pq, string mapname) 
        {
            return MvcResultInfo.Execute(null, document =>
            {
                try
                {
                    List<STOSResult> result = new List<STOSResult>();
                    var mpaNames = new List<string>();
                    mpaNames.Add(mapname);
                    pq.MapName = mpaNames;

                    //if (pq.InPlan != "Both")
                    //{
                    //    pq.InPlan = "Yes";
                    //}
                    pq.isGenerated = true;
                    pq.isSTOS = false;
                    var queryElement = pq.ParseQuery();
                    var getListSTOS = STOS.Populate<STOS>(queryElement);
                    
                    getListSTOS.ForEach(x => x.MapName = mapname);
                    if (getListSTOS.Any())
                    {
                        STOS st = new STOS();
                        var getresult = st.TransformsRes(getListSTOS, WebTools.LoginUser.UserName);

                        SSTG sstg = new SSTG();
                        getresult = sstg.SstgSTOS(pq.SSTG, pq.Currency, getresult);
                        foreach (var r in getresult)
                        {
                            r.Currency = pq.Currency;
                            r.MapName = mapname;
                            r.Save();
                            result.Add(r);
                        }
                        
                    }

                    return new { Success = true, listSTOSres = result };
                }
                catch (Exception e)
                {
                    return new { Success = false, Message = e.Message };
                }
            });
        }

        //public JsonResult GetListSTOSMapping(WaterfallBase wb, ParsePalentirQuery pq, string mapname)
        //{
        //    return MvcResultInfo.Execute(null, document =>
        //    {
        //        List<STOS> populateSTOS = new List<STOS>();
        //        if (mapname != "Select Map File")
        //        {
        //            var query = QueryElm(wb, pq);
        //            populateSTOS = STOS.Populate<STOS>(query);
        //        }
        //        return DataHelper.ToDictionaryArray(populateSTOS.Select(x => x.ToBsonDocument()).ToList());
        //    });
        //}
        public JsonResult GetListSTOSMapping(ParsePalentirQuery pq, string mapname)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                List<STOS> populateSTOS = new List<STOS>();
                if (mapname != "Select Map File")
                {
                    var mpaNames = new List<string>();
                    mpaNames.Add(mapname);
                    pq.MapName = mpaNames;
                    pq.UpdateBy = new List<string>(new string[] { WebTools.LoginUser.UserName });
                    pq.isGenerated = true;
                    pq.isSTOS = false;
                    var queryElement = pq.ParseQuery();
                    populateSTOS = STOS.Populate<STOS>(queryElement);
                }
                else
                {
                    populateSTOS = STOS.Populate<STOS>();
                }
                return DataHelper.ToDictionaryArray(populateSTOS.Select(x => x.ToBsonDocument()).ToList());
            });
        }

        //public JsonResult GetSTOSReport(WaterfallBase wb, ParsePalentirQuery pq)
        //{
        //    return MvcResultInfo.Execute(null, (BsonDocument doc) =>
        //    {
        //        try
        //        {
        //            var query = QueryElm(wb, pq);
        //            List<STOSResult> populateSTOS = new List<STOSResult>();
        //            populateSTOS = STOSResult.Populate<STOSResult>(query);

        //            var dataGrid = new List<object>();
        //            var yearMin = populateSTOS.SelectMany(x => x.Details).Select(x => x.DateId).Where(x => x != Tools.DefaultDate.Year).Min();
        //            var yearMax = populateSTOS.SelectMany(x => x.Details).Select(x => x.DateId).Where(x => x != Tools.DefaultDate.Year).Max();

        //            SSTG sstg = new SSTG();
        //            populateSTOS = sstg.SstgSTOS(pq.SSTG, pq.Currency, populateSTOS);
        //            foreach (var t in populateSTOS)
        //            {
        //                List<BsonDocument> detailList = new List<BsonDocument>();
        //                if (t.Details.Count > 0)
        //                {
        //                    //var details = t.Details.OrderBy(x => x.DateId);
        //                    for (int y = yearMin; y <= yearMax; )
        //                    {
        //                        BsonDocument d = new BsonDocument();
        //                        var addVal = t.Details.Where(x => x.DateId == y).Select(x => x.value).ToList();
        //                        if (addVal.Count == 0)
        //                        {
        //                            d.Set("Year_" + y, 0);
        //                        }
        //                        else
        //                        {
        //                            //double value = addVal[0] / 1000000;
        //                            d.Set("Year_" + y, Math.Round(addVal[0], 2));
        //                        }
        //                        detailList.Add(d);
        //                        y++;
        //                    }
        //                }
        //                else
        //                {
        //                    for (int y = yearMin; y <= yearMax; )
        //                    {
        //                        BsonDocument d = new BsonDocument();
        //                        d.Set("Year_" + y, 0);
        //                        detailList.Add(d);
        //                        y++;
        //                    }
        //                }

        //                dataGrid.Add(new
        //                {
        //                    ActivityName = t.ActivityName,
        //                    Project = t.ProjectName,
        //                    SpendType = t.SpendType,
        //                    CostEstimator = t.CostEstimator,
        //                    Status = t.Status,
        //                    EstimateType = t.EstimateType,
        //                    Currency = t.Currency,
        //                    Escalation = t.Escalation,
        //                    OwnersCost = t.OwnersCost,
        //                    Contingency = t.Contingency,
        //                    ActivityDesc = t.ActivityDesc,
        //                    RegretConsequences = t.RegretConsequences,
        //                    Rank = t.Rank,
        //                    PMaster = t.PMaster,
        //                    PMasterCategory = t.PMasterCategory,
        //                    SponsorFunction = t.SponsorFunction,
        //                    Revenue = t.Revenue,
        //                    AssuranceLevel = t.AssuranceLevel,
        //                    Regrets = t.Regrets,
        //                    POE = t.POE,
        //                    Details = detailList,
        //                    id = t._id.ToString()
        //                });
        //            }

        //            return DataHelper.ToDictionaryArray(dataGrid.Select(x => x.ToBsonDocument()).ToList());
        //        }
        //        catch (Exception e)
        //        {
        //            var dataGrid = new List<object>();
        //            dataGrid.Add(new { Success = false, Message = e.Message });
        //            return DataHelper.ToDictionaryArray(dataGrid.Select(x => x.ToBsonDocument()).ToList());
        //        }
        //    });
        //}

        //public JsonResult DeletePlanStos(string mapnames)
        //{
        //    return MvcResultInfo.Execute(null, document =>
        //    {
        //        DataHelper.Delete("WEISPalantirSTOS", Query.EQ("MapName", mapnames));
        //        return new { Success = true, Message = "Record Deleted"};
        //    });
        //}

        public JsonResult DeleteSTOS(string mapname)
        {
            try
            {
                DataHelper.Delete("WEISPalantirSTOS",  Query.EQ("MapName", mapname));
                DataHelper.Delete("WEISPalantirSTOSResult", Query.EQ("MapName", mapname));

                //DataHelper.Delete("WEISPalantirSTOS", Query.And(Query.EQ("UpdateBy", WebTools.LoginUser.UserName), Query.EQ("MapName", mapname)));
                //DataHelper.Delete("WEISPalantirSTOSResult", Query.And(Query.EQ("UpdateBy", WebTools.LoginUser.UserName), Query.EQ("MapName", mapname)));

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult ExportSTOS(bool isDetails, ParsePalentirQuery pq, string mapname)
        {
            string nameExcel;
            if (isDetails)
                nameExcel = "DataSTOSExportTemplate.xlsx";
            else
                nameExcel = "DataMapSTOSExportTemplate.xlsx";

            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), nameExcel);
            List<STOSResult> listSTOS = new List<STOSResult>();
            if (mapname != "Select Map File")
            {
                var mpaNames = new List<string>();
                mpaNames.Add(mapname);
                pq.MapName = mpaNames;
                //pq.UpdateBy = new List<string>(new string[] { WebTools.LoginUser.UserName });
                pq.isGenerated = true;
                pq.isSTOS = false;
                var queryElement = pq.ParseQuery();
                listSTOS = STOSResult.Populate<STOSResult>(queryElement);
            }

            Workbook wb = new Workbook(xlst);
            var ws = wb.Worksheets[0];
            int startRow = 2;
            foreach (var stoslst in listSTOS)
            {
                ws.Cells["A" + startRow.ToString()].Value = stoslst.ActivityName;
                ws.Cells["B" + startRow.ToString()].Value = stoslst.ProjectName;
                ws.Cells["C" + startRow.ToString()].Value = stoslst.SpendType;
                ws.Cells["D" + startRow.ToString()].Value = stoslst.ActivityDesc;

                ws.Cells["E" + startRow.ToString()].Value = stoslst.RegretConsequences;
                ws.Cells["F" + startRow.ToString()].Value = stoslst.Rank;
                ws.Cells["G" + startRow.ToString()].Value = stoslst.PMaster;
                ws.Cells["H" + startRow.ToString()].Value = stoslst.PMasterCategory;
                ws.Cells["I" + startRow.ToString()].Value = stoslst.SponsorFunction;
                ws.Cells["J" + startRow.ToString()].Value = stoslst.Revenue;

                ws.Cells["K" + startRow.ToString()].Value = stoslst.CostEstimator;
                ws.Cells["L" + startRow.ToString()].Value = stoslst.AssuranceLevel;
                ws.Cells["M" + startRow.ToString()].Value = stoslst.Status;
                ws.Cells["N" + startRow.ToString()].Value = stoslst.EstimateType;

                ws.Cells["O" + startRow.ToString()].Value = stoslst.Regrets;
                ws.Cells["P" + startRow.ToString()].Value = stoslst.Currency;
                ws.Cells["Q" + startRow.ToString()].Value = stoslst.POE + "%";
                ws.Cells["R" + startRow.ToString()].Value = stoslst.Escalation;

                ws.Cells["S" + startRow.ToString()].Value = stoslst.OwnersCost + "%";
                ws.Cells["T" + startRow.ToString()].Value = stoslst.Contingency + "%";

                if (isDetails)
                {
                    if (stoslst.Details.Any())
                    {
                        int col = 20;
                        foreach (var detail in stoslst.Details)
                        {
                            if (detail.value != 0.0)
                                ws.Cells[startRow - 1, col].Value = Tools.Div(detail.value, 1000000);
                            else
                                ws.Cells[startRow - 1, col].Value = "";
                            
                            col++;
                        }
                    }
                    else
                    {
                        for (int i = 20; i <= 60; i++)
                        {
                            ws.Cells[startRow - 1, i].Value = "";//0.00;
                        }
                    }
                }
                startRow++;
                //}
            }

            if (isDetails)
            {
                List<STOS> doc = new List<STOS>();
                if (mapname != "Select Map File")
                {
                    var mpaNames = new List<string>();
                    mpaNames.Add(mapname);
                    pq.MapName = mpaNames;
                    //pq.UpdateBy = new List<string>(new string[] { WebTools.LoginUser.UserName });
                    pq.isGenerated = true;
                    pq.isSTOS = false;
                    var queryElement = pq.ParseQuery();
                    doc = STOS.Populate<STOS>(queryElement);
                }

                ws = wb.Worksheets[1];
                startRow = 2;
                foreach (var xx in doc)
                {
                    ws.Cells["A" + startRow.ToString()].Value = xx.ActivityName;
                    ws.Cells["B" + startRow.ToString()].Value = xx.ProjectName;
                    ws.Cells["C" + startRow.ToString()].Value = xx.SpendType;
                    ws.Cells["D" + startRow.ToString()].Value = xx.CostEstimator;
                    ws.Cells["E" + startRow.ToString()].Value = xx.Status;
                    ws.Cells["F" + startRow.ToString()].Value = xx.EstimateType;
                    ws.Cells["G" + startRow.ToString()].Value = xx.Currency;
                    ws.Cells["H" + startRow.ToString()].Value = xx.Contingency + "%";
                    startRow++;
                }

                ws = wb.Worksheets[2];
                startRow = 2;
                ws.Cells["A" + startRow.ToString()].Value = pq.InPlan;
                ws.Cells["B" + startRow.ToString()].Value = pq.Currency;
                ws.Cells["C" + startRow.ToString()].Value = pq.SSTG;
                ws.Cells["D" + startRow.ToString()].Value = WebTools.LoginUser.UserName;
                ws.Cells["E" + startRow.ToString()].Value = Tools.ToUTC(DateTime.Now);
                ws.Cells["F" + startRow.ToString()].Value = mapname;
            }



            var newFileNameSingle = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                     String.Format("STOS-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss")));

            string returnName = String.Format("STOS-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss"));

            wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            return Json(new { Success = true, Path = returnName }, JsonRequestBehavior.AllowGet);
        }

        public FileResult DownloadSTOSFile(string stringName, DateTime date, bool isDetails)
        {
            var res = Path.Combine(Server.MapPath("~/App_Data/Temp"), stringName);
            string retstringName;
            if (isDetails)
                retstringName = "STOS-" + date.ToString("dd-MMM-yyyy") + ".xlsx";
            else
                retstringName = "STOSMap-" + date.ToString("dd-MMM-yyyy") + ".xlsx";

            return File(res, Tools.GetContentType(".xlsx"), retstringName);
        }

        //public JsonResult SavePlanStos(List<STOSResult> list, string mapnames)
        //{
        //    return MvcResultInfo.Execute(null, document =>
        //    {
        //        //DataHelper.Delete("WEISPalantirSTOS", Query.EQ("MapName", mapnames));

        //        foreach (var t in list)
        //        {
        //            t.MapName = mapnames;
        //            t.Save();
        //        }
        //        return new { Success = true, Message = "MapName saved!", listMap = list };
        //    });
        //}
        public JsonResult SavePlanStos(List<STOS> list, string mapnames)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                //DataHelper.Delete("WEISPalantirSTOS", Query.EQ("MapName", mapnames));

                foreach (var t in list)
                {
                    if (string.IsNullOrEmpty(t.ActivityDescription))
                        t.ActivityDescription = "";
                    if (string.IsNullOrEmpty(t.RegretConsequences))
                        t.RegretConsequences = "";
                    if (string.IsNullOrEmpty(t.Rank))
                        t.Rank = "";
                    if (string.IsNullOrEmpty(t.PMaster))
                        t.PMaster = "";
                    if (string.IsNullOrEmpty(t.PMasterCategory))
                        t.PMasterCategory = "";
                    if (string.IsNullOrEmpty(t.SponsorFunction))
                        t.SponsorFunction = "";
                    if (string.IsNullOrEmpty(t.Revenue))
                        t.Revenue = "";
                    if (string.IsNullOrEmpty(t.AssuranceLevel))
                        t.AssuranceLevel = "";
                    if (string.IsNullOrEmpty(t.Regrets))
                        t.Regrets = "";
                    if (string.IsNullOrEmpty(t.POE.ToString()))
                        t.POE = 0.0;


                    t.MapName = mapnames;
                    t.Save();
                }
                return new { Success = true, Message = "MapName saved!", listMap = list };
            });
        }

        public JsonResult UpdateSTOSFromGrid(List<ParamUpdateSTOS> data)
        {

            try
            {
                if (data != null && data.Count > 0)
                {
                    foreach (var d in data)
                    {
                        var getDataSTOS = STOSResult.Get<STOSResult>(d._id);
                        if (getDataSTOS != null)
                        {
                            getDataSTOS.ActivityDesc = d.ActivityDesc;
                            getDataSTOS.RegretConsequences = d.RegretConsequences;
                            getDataSTOS.Rank = d.Rank;
                            getDataSTOS.PMaster = d.PMaster;
                            getDataSTOS.PMasterCategory = d.PMasterCategory;
                            getDataSTOS.SponsorFunction = d.SponsorFunction;
                            getDataSTOS.Revenue = d.Revenue;
                            getDataSTOS.AssuranceLevel = d.AssuranceLevel;
                            getDataSTOS.Regrets = d.Regrets;
                            getDataSTOS.Save();
                        }
                    }
                }
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        //protected override JsonResult Json(object data, string contentType, System.Text.Encoding contentEncoding, JsonRequestBehavior behavior)
        //{
        //    return new JsonResult()
        //    {
        //        Data = data,
        //        ContentType = contentType,
        //        ContentEncoding = contentEncoding,
        //        JsonRequestBehavior = behavior,
        //        MaxJsonLength = Int32.MaxValue
        //    };
        //}

        //GetDataSTOSMapping
        public JsonResult GetDataSTOSMapping(List<string> lMapNames)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                ParsePalentirQuery pq = new ParsePalentirQuery();
                pq.MapName = lMapNames;
                pq.UpdateBy = new List<string>(new string[] { WebTools.LoginUser.UserName });
                var elementQuery = pq.ParseQuery();

                if (elementQuery != null)
                {
                    var pop = STOS.Populate<STOS>(Query.And(elementQuery));
                    return pop;
                }
                else
                {
                    var pop = STOS.Populate<STOS>();
                    return pop;
                }
            });
        }

        [HttpPost]
        public JsonResult UploadMap()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                try
                {
                    HttpPostedFileBase file = Request.Files["UploadedFile"]; //Uploaded file
                    string fileName = file.FileName;

                    //To save file, use SaveAs method
                    file.SaveAs(Server.MapPath("~/App_Data/Temp/") + fileName); //File will be saved in application root

                    string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp/"), fileName);
                    ExtractionHelper e = new ExtractionHelper();
                    IList<BsonDocument> datas = e.Extract(xlst);


                    var tx = DataHelper.Populate("LogLatestLS").Select(x => x.GetString("Well_Name")).Distinct();
                    var bizplans = BizPlanActivity.GetAll(Query.Null);
                    List<STOS> lstos = new List<STOS>();
                    foreach (var item in datas)
                    {
                        //STOS map = new STOS();
                        //map.ActivityName = item.GetString("Activity_Name");
                        //map.ProjectName = item.GetString("Project");
                        //map.SpendType = item.GetString("Spend_Type");
                        //map.ActivityDescription = item.GetString("Activity_Description");
                        //map.RegretConsequences = item.GetString("Regret_Consequences");
                        //map.Rank = item.GetString("Rank");
                        //map.PMaster = item.GetString("PMaster");
                        //map.PMasterCategory = item.GetString("PMaster_Category");
                        //map.SponsorFunction = item.GetString("Sponsor_Function");
                        //map.Revenue = item.GetString("Revenue");
                        //map.CostEstimator = item.GetString("Cost_Estimator");
                        //map.AssuranceLevel = item.GetString("Assurance_Level");
                        //map.EstimateType = item.GetString("Estimate_Type");
                        //map.Regrets = item.GetString("Regrets");
                        //map.Currency = item.GetString("Currency");
                        //map.POE = item.GetDouble("POE_%");
                        //map.Contingency = item.GetDouble("Contingency_%");
                        //map.LineofBusiness = item.GetString("Line_Of_Business");
                        //map.AssetName = item.GetString("Asset_Name");
                        //map.ReferenceFactorModel = item.GetString("Reference_Factor_Model");

                        //map.RigName = item.GetString("Rig_Name");
                        //map.RigType = item.GetString("Rig_Type");
                        //map.UARigSequenceId = item.GetString("UARig_Sequence_Id");

                        //CreateNewMapSTos(elementQuery);
                        //lstos.Add(map);

                        //var qBPstos = new List<IMongoQuery>();
                        //qBPstos.Add(Query.EQ("WellName", item.GetString("Activity_Name")));
                        ////qBPstos.Add(Query.EQ("Phases.ActivityType", map.ActivityType));
                        //qBPstos.Add(Query.EQ("UARigSequenceId", item.GetString("Sequence_Id")));
                        ////qBPstos.Add(Query.EQ("Phases.Estimate.RigName", map.RigName));
                        //qBPstos.Add(Query.EQ("RigName", item.GetString("Rig_Name")));
                        ////qBPstos.Add(Query.EQ("AssetName", item.GetString("Asset_Name")));
                        ////qBPstos.Add(Query.EQ("LineOfBusiness",item.GetString("Line_Of_Business")));
                        ////qBPstos.Add(Query.EQ("Phases.FundingType", item.GetString("Spend_Type")));

                        var getMValue = bizplans.Where(x => (x.WellName != null && x.WellName.Trim() == item.GetString("Activity_Name").Trim()) && (x.UARigSequenceId != null && x.UARigSequenceId.Trim() == item.GetString("Sequence_Id").Trim()) && (x.RigName != null && x.RigName.Trim() == item.GetString("Rig_Name").Trim())).ToList();
                        if (getMValue != null)
                        {
                            foreach (string g in tx)
                            {

                                if (g.Equals("APPO AC006"))
                                { }

                                STOS px = new STOS();
                                var data = getMValue.Where(x => x.WellName.Equals(g.ToString())).FirstOrDefault();
                                if (data != null)
                                {
                                    var gogo = px.TransformsToMapBizPlanAs(data, WebTools.LoginUser.UserName);
                                    foreach (var t in gogo)
                                    {
                                        lstos.Add(t);
                                    }     
                                }
                            }
                        }
                    }

                    ////var stos = datas.Select(x => BsonSerializer.Deserialize<STOS>(x)).ToList();
                    //var grp = lstos.Select(x => new
                    //{
                    //    x.WellName,
                    //    x.RigName,
                    //    x.UARigSequenceId,
                    //    x.AssetName,
                    //    x.LineofBusiness,
                    //    x.SpendType
                    //}).Distinct();


                    //ParsePalentirQuery pq = new ParsePalentirQuery();
                    //pq.isGenerated = true;
                    //pq.isSTOS = true;
                    //pq.FundingType = pq.STOSFilter(grp.Select(x => x.SpendType).Distinct().ToList());
                    //pq.LineOfBusiness = pq.STOSFilter(grp.Select(x => x.LineofBusiness).Distinct().ToList());
                    //pq.Asset = pq.STOSFilter(grp.Select(x => x.AssetName).Distinct().ToList());
                    

                    //var elementQuery = pq.ParseQuery();
                    //var crtMapStos = CreateNewMapSTos(elementQuery);
                    //for (int i = 0; i < crtMapStos.Count; i++)
                    //{
                    //    crtMapStos[i].ActivityDescription = lstos[i].ActivityDescription;
                    //    crtMapStos[i].RegretConsequences = lstos[i].RegretConsequences;
                    //    crtMapStos[i].Rank = lstos[i].Rank;
                    //    crtMapStos[i].PMaster = lstos[i].PMaster;
                    //    crtMapStos[i].PMasterCategory = lstos[i].PMasterCategory;
                    //    crtMapStos[i].SponsorFunction = lstos[i].SponsorFunction;
                    //    crtMapStos[i].Revenue = lstos[i].Revenue;
                    //    crtMapStos[i].AssuranceLevel = lstos[i].AssuranceLevel;
                    //    crtMapStos[i].Regrets = lstos[i].Regrets;
                    //    crtMapStos[i].POE = lstos[i].POE;
                    //}

                    return lstos; //new { Success = true, listMap = stos };
                }
                catch (Exception e)
                {
                    return new { Success = "NOK", Message = e.Message };
                }
            });
        }

        public JsonResult DeleteMap(string mapname)
        {
            try
            {
                DataHelper.Delete("WEISPalantirSTOS", Query.EQ("MapName", mapname));

                List<string> bsonResult = new List<string>();
                var getMapName = STOSResult.Populate<STOSResult>().GroupBy(x => x.MapName).ToList();
                if (getMapName.Count > 0)
                {
                    foreach (var mapNames in getMapName)
                    {
                        bsonResult.Add(mapNames.Key);
                    }
                }
                return Json(new { Success = true, mapList = bsonResult }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }

    [BsonIgnoreExtraElements]
    public class ParamUpdateSTOS
    {

        public string _id { get; set; }
        public string ActivityDesc { get; set; }
        public string RegretConsequences { get; set; }
        public string Rank { get; set; }
        public string PMaster { get; set; }
        public string PMasterCategory { get; set; }
        public string SponsorFunction { get; set; }
        public string Revenue { get; set; }
        public string AssuranceLevel { get; set; }
        public string Regrets { get; set; }
        public double POE { get; set; }
    }
}