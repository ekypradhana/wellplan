using ECIS.Core;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using ECIS.Client.WEIS;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using ECIS.Core.DataSerializer;
using System.Text;
using Aspose.Cells;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth(WEISRoles = "Administrators")]
    public class UploadPIPController : Controller
    {
        //
        // GET: /Shell/UploadPIP/
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase files)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                int fileSize = files.ContentLength;
                string fileName = files.FileName;
                string ContentType = files.ContentType;
                string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["PIPUploadPath"]);
                bool exists = System.IO.Directory.Exists(folder);

                string fileNameReplace = DateTime.Now.ToString("yyyMMddhhmmss") + "-" + fileName;

                if (!exists)
                    System.IO.Directory.CreateDirectory(folder);

                string filepath = System.IO.Path.Combine(folder, fileNameReplace);
                files.SaveAs(filepath);

            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            //ri.CalcLapseTime();
            return ri.ToJsonResult();
        }

        public JsonResult LoadGridData()
        {
            List<BsonDocument> bdocs = new List<BsonDocument>();

            string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["PIPUploadPath"]);
            bool exists = System.IO.Directory.Exists(folder);
            if (!exists)
                return Json(new { Data = "", Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);

            string[] filePaths = Directory.GetFiles(WebConfigurationManager.AppSettings["PIPUploadPath"]);

            foreach (string file in filePaths)
            {
                BsonDocument doc = new BsonDocument();
                doc.Set("FileName", System.IO.Path.GetFileName(file));
                doc.Set("LastWrite", System.IO.File.GetLastWriteTimeUtc(file));
                doc.Set("CreateDate", System.IO.File.GetCreationTimeUtc(file));

                bdocs.Add(doc);
            }

            return Json(new { Data = DataHelper.ToDictionaryArray(bdocs.OrderByDescending(x => BsonHelper.GetDateTime(x, "CreateDate")).ToList()), Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
        }


        public JsonResult Execute(string id)
        {

            try
            {
                #region Extract XLS to Bsondoc
                ExtractionHelper e = new ExtractionHelper();

                string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["PIPUploadPath"]);
                string Path = folder + @"\" + id;

                IList<BsonDocument> datas = e.Extract(Path);

                string dt = DateTime.Now.ToString("yyyyMMdd");

                DataHelper.Delete("_PIPMaster_" + dt);
                //foreach (var y in datas.Where(x => !BsonHelper.GetString(x, "Well_Name").Equals("") && !BsonHelper.GetString(x, "Activity_Type").Equals("")))
                //{
                //    DataHelper.Save("_PIPMaster_" + dt, y);
                //}

                foreach (var y in datas)
                {
                    if (y.GetString("PIP_Type").ToLower().Equals("cr"))
                    {
                        if(!y.GetString("Rig_Name").Equals(""))
                            DataHelper.Save("_PIPMaster_" + dt, y);
                    }
                    else
                    {
                        if (!y.GetString("Well_Name").Equals("") && !y.GetString("Activity_Type").Equals(""))
                            DataHelper.Save("_PIPMaster_" + dt, y);
                    }
                }

                #endregion

                DateTime dateNow = DateTime.Now;
                dateNow = DateTime.Now;
                string nowDate = dateNow.ToString("yyyyMMdd");
                string nowDate2 = dateNow.ToString("yyyyMMddhhmm");

                List<BsonDocument> outputdata = new List<BsonDocument>();
                LoadPIP(out outputdata, "_PIPMaster_" + dt);

                return Json(new { Data = DataHelper.ToDictionaryArray(outputdata), Result = "OK", Message = "Load PIP Done" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message, Trace = ex.StackTrace }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult Backup(string id)
        {
            try
            {

                string host = System.Configuration.ConfigurationSettings.AppSettings["ServerHost"];
                string db = System.Configuration.ConfigurationSettings.AppSettings["ServerDb"];
                MongoDatabase mongo = GetDb(host, db);

                string backupname = "_PIPMaster_" + DateTime.Now.ToString("yyyyMMddhhmm") + "_" + id;

                mongo.CreateCollection(backupname);
                var source = mongo.GetCollection("WEISPIPs");
                var dest = mongo.GetCollection(backupname);
                dest.InsertBatch(source.FindAll());


                return Json(new { Data = "", Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public FileResult Download(string id)
        {
            string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["PIPUploadPath"]);
            byte[] fileBytes = System.IO.File.ReadAllBytes(folder + @"\" + id);
            string fileName = id;
            return File(folder + @"\" + id, Tools.GetContentType(".xlsx"), Path.GetFileName(folder + @"\" + id));

        }
        internal MongoDatabase _db;
        internal MongoDatabase GetDb(string connection, string database)
        {
            if (_db == null)
            {
                var connectionString = connection;
                var client = new MongoClient("mongodb://" + connectionString + ":27017");
                MongoServer server = client.GetServer();
                MongoDatabase t = server.GetDatabase(database);
                _db = t;
            }
            return _db;
        }

        public List<string> GetCollectionName()
        {
            string host = System.Configuration.ConfigurationSettings.AppSettings["ServerHost"];
            string db = System.Configuration.ConfigurationSettings.AppSettings["ServerDb"];
            MongoDatabase mongo = GetDb(host, db);

            List<string> res = new List<string>();

            foreach (string t in mongo.GetCollectionNames().ToList())
            {
                if (t.Contains("_PIP"))
                {
                    res.Add(t);
                }
            }

            return res;
        }

        public JsonResult LoadCollection()
        {
            try
            {
                var t = GetCollectionName();
                List<BsonDocument> docs = new List<BsonDocument>();
                foreach (string y in t)
                {
                    string[] yName = y.Split('_');

                    DateTime dt = new DateTime();
                    try
                    {
                        dt = DateTime.ParseExact(yName[2].ToString(), "yyyyMMddhhmm", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch (Exception e)
                    {
                        dt = Tools.DefaultDate;
                    }

                    BsonDocument d = new BsonDocument();
                    d.Set("CollectionName", y.ToString());
                    d.Set("CreateDate", Tools.ToUTC(dt));
                    docs.Add(d);
                }


                return Json(new { Data = DataHelper.ToDictionaryArray(docs.OrderByDescending(x => BsonHelper.GetDateTime(x, "CreateDate")).ToList()), Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult DeleteBackup(string id)
        {
            try
            {
                string host = System.Configuration.ConfigurationSettings.AppSettings["ServerHost"];
                string db = System.Configuration.ConfigurationSettings.AppSettings["ServerDb"];
                MongoDatabase mongo = GetDb(host, db);

                mongo.DropCollection(id);

                return Json(new { Data = "", Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult Restore(string id)
        {
            try
            {

                string host = System.Configuration.ConfigurationSettings.AppSettings["ServerHost"];
                string db = System.Configuration.ConfigurationSettings.AppSettings["ServerDb"];
                MongoDatabase mongo = GetDb(host, db);

                mongo.DropCollection("WEISWellPIPs");
                mongo.RenameCollection(id, "WEISWellPIPs");

                return Json(new { Data = "", Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
   

        public FileResult DonwloadTemplate()
        {
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "pipexporttemplate.xlsx");

            return File(xlst, Tools.GetContentType(".xlsx"), "WEISPIPTemplate.xlsx");
        }

        public FileResult DonwloadCurrentPIP()
        {
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "pipexporttemplatecurrent.xlsx");

            var datas = WellPIP.Populate<WellPIP>();
            string stringName;
            var res = GetCurrentData(datas, xlst, out stringName);


            return File(res, Tools.GetContentType(".xlsx"), stringName);
        }

        public string GetCurrentData(List<WellPIP> wellPips, string xlst, out string returnName)
        {
            var wb = new Workbook(xlst);
            var ws = wb.Worksheets[0];

            int idx = 2;
            foreach (var data in wellPips)
            {

                var qs = new List<IMongoQuery>();
                qs.Add(Query.EQ("WellName", data.WellName));
                qs.Add(Query.EQ("UARigSequenceId", data.SequenceId));
                var q = Query.And(qs);
                var actv = WellActivity.Get<WellActivity>(q);

                foreach (var el in data.Elements)
                {
                    ws.Cells["A" + idx.ToString()].Value = data.WellName;
                    ws.Cells["B" + idx.ToString()].Value = data.RigName;
                    ws.Cells["C" + idx.ToString()].Value = data.ActivityType;
                    ws.Cells["D" + idx.ToString()].Value = data.Type;

                    ws.Cells["E" + idx.ToString()].Value = el.Title;
                    ws.Cells["F" + idx.ToString()].Value = el.Period.Start;
                    ws.Cells["G" + idx.ToString()].Value = el.Period.Finish;
                    ws.Cells["H" + idx.ToString()].Value = el.Period.Start.Year;

                    ws.Cells["I" + idx.ToString()].Value = el.Completion;
                    ws.Cells["J" + idx.ToString()].Value = el.Classification;
                    ws.Cells["K" + idx.ToString()].Value = el.Theme;


                    ws.Cells["L" + idx.ToString()].Value = el.DaysPlanImprovement;
                    ws.Cells["M" + idx.ToString()].Value = el.DaysPlanRisk;


                    ws.Cells["N" + idx.ToString()].Value = el.CostPlanImprovement;
                    ws.Cells["O" + idx.ToString()].Value = el.CostPlanRisk;

                    //ws.Cells["P" + idx.ToString()].Value = el.DaysCurrentWeekImprovement;
                    //ws.Cells["Q" + idx.ToString()].Value = el.DaysCurrentWeekRisk;


                    //ws.Cells["R" + idx.ToString()].Value = el.CostCurrentWeekImprovement;
                    //ws.Cells["S" + idx.ToString()].Value = el.CostCurrentWeekRisk;

                    if (el.ActionParties != null)
                    {
                        ws.Cells["P" + idx.ToString()].Value = string.Join(", ", el.ActionParties.Select(x => x.FullName)); //string.Join(", ", el.ActionParties.ToArray());

                    }
                    ws.Cells["Q" + idx.ToString()].Value = el.PerformanceUnit;

                    if (actv != null)
                    {
                        ws.Cells["R" + idx.ToString()].Value = actv.AssetName == null ? "" : actv.AssetName;
                        ws.Cells["S" + idx.ToString()].Value = actv.ProjectName == null ? "" : actv.ProjectName;

                    }

                }

                idx++;
            }

            var newFileNameSingle = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                      String.Format("WellPIP-{0}.xlsx", DateTime.Now.ToString("ddMMMyy")));

            returnName = String.Format("WellPIP-{0}.xlsx", DateTime.Now.ToString("ddMMMyy"));

            wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            return newFileNameSingle;

        }

        public void LoadPIP(out List<BsonDocument> OutMessage, string tablename = "")
        {
            OutMessage = new List<BsonDocument>();
            //List<BsonDocument> dataChangesResult = new List<BsonDocument>();

            StringBuilder sb = new StringBuilder();

            var pips = DataHelper.Populate(tablename)
                .OrderBy(d => d.GetString("Well_Name"))
                .ThenBy(d => d.GetString("Activity_Type"))
                .ToList();
            string prevWellName = "", prevActType = "";
            bool saveIt = false;
            WellPIP wp = null;
            foreach (var pip in pips)
            {
                //bool newStart = false;
                
                var PIPType = pip.GetString("PIP_Type").ToLower() == "cr" ? "reduction" : "efficient";
                var rigName = pip.GetString("Rig_Name").Trim();
                    var wellname = PIPType != "reduction" ? pip.GetString("Well_Name") : rigName+"_CR";
                    var actType = PIPType != "reduction" ? pip.GetString("Activity_Type") : "";
                    if (prevWellName != wellname || prevActType != actType)
                    {
                        if (wp != null && saveIt) wp.Save();
                        //newStart = true;
                        saveIt = true;
                        prevWellName = wellname;
                        prevActType = actType;
                        var qwas = new List<IMongoQuery>();
                        var SequenceId = "";
                        var PhaseNo = 0;

                        if (PIPType != "reduction")
                        {
                            if (rigName != "") qwas.Add(Query.EQ("RigName", rigName));
                            qwas.Add(Query.EQ("WellName", wellname));
                            qwas.Add(Query.EQ("Phases.ActivityType", actType));
                            var was = WellActivity.Populate<WellActivity>(Query.And(qwas))
                                .Select(d =>
                                {
                                    d.Phases = d.Phases.Where(e => e.ActivityType.Equals(actType)).ToList();
                                    return d;
                                })
                                //.OrderBy(d=>d.Phases[0].PlanSchedule.Finish)
                                .ToList();

                            Dictionary<string, double> list = new Dictionary<string, double>();
                            double datediff = 0;
                            if (was.Count > 0)
                            {
                                SequenceId = was[0].UARigSequenceId;
                                PhaseNo = was[0].Phases[0].PhaseNo;
                            }

                            // if found wa > 1, find the nearest LE.Finish
                            if (was.Count() > 1)
                            {
                                foreach (var w in was)
                                {
                                    DateTime LEscFinish = w.LESchedule.Finish;
                                    DateTime nowDay = DateTime.Now;
                                    datediff = (LEscFinish - nowDay).TotalDays;

                                    if (datediff < 0)
                                        datediff = datediff * (-1);

                                    list.Add(w.UARigSequenceId, datediff);
                                }
                                var seqphase = list.OrderBy(x => x.Value).FirstOrDefault().Key;
                                IMongoQuery qry = Query.EQ("UARigSequenceId", seqphase);
                                var data = DataHelper.Populate<WellActivity>("WEISWellActivities", qry).FirstOrDefault();  
                                //WellActivity.Get<WellActivity>(seqphase);
                                //var data = was.Where(x => x._id.ToString().Equals(seqphase)).FirstOrDefault();

                                SequenceId = data.UARigSequenceId;
                                PhaseNo = data.Phases[0].PhaseNo;
                            }
                        }

                        saveIt = false;
                        //wp = PIPType != "reduction" ? WellPIP.GetByWellActivity(wellname, actType) : WellPIP.Get<WellPIP>(Query.EQ("WellName",wellname));
                        wp = PIPType != "reduction" ? 
                            WellPIP.GetByOpsActivity(SequenceId, PhaseNo) : 
                            WellPIP.Get<WellPIP>(Query.EQ("WellName", wellname));
                        if (wp == null)
                        {
                            saveIt = true;
                            wp = new WellPIP();
                            wp.WellName = wellname;
                            wp.SequenceId = SequenceId;
                            wp.PhaseNo = PhaseNo;
                            wp.ActivityType = actType;
                            wp.Status = "Publish";
                            wp.Type = PIPType != "reduction" ? "Efficient" : "Reduction";
                            //wp.Elements.Clear();
                        }

                    }


                    if (wp != null)
                    {
                        var idea = pip.GetString("Idea");
                        var pe = wp.Elements.FirstOrDefault(d => d.Title.Equals(idea));
                        if (pe == null)
                        {
                            pe = new PIPElement();
                            pe.ElementId = wp.Elements.Count() + 1;
                            pe.Title = idea;

                            pe.Completion = pip.GetString("%_Complete"); //pip.GetDouble("%_Complete") / 100;
                            pe.Classification = pip.GetString("High_Level_Classification");
                            pe.ActionParty = pip.GetString("Action_Party");
                            pe.ActionParties = new List<WEISPersonInfo>();
                            pe.ActionParties.Add(new WEISPersonInfo
                            {
                                FullName = pe.ActionParty,
                                Email = "",
                                RoleId = "CE-MGR"
                            });

                            string assetname = pip.GetString("Asset_Name");
                            string projectname = pip.GetString("Project_Name");
                            string rigname = pip.GetString("Rig_Name");

                            pe.PerformanceUnit = pip.GetString("Performance_Unit");
                            pe.Theme = pip.GetString("Theme");
                            pe.Classification = pip.GetString("High_Level_Classification");
                            pe.DaysPlanImprovement = pip.GetDouble("Opportunity__Days_(-)");
                            pe.DaysPlanRisk = pip.GetDouble("Risk__Days_(+)");
                            pe.CostPlanImprovement = pip.GetDouble("Opportunity_Cost_MM__(-)");
                            pe.CostPlanRisk = pip.GetDouble("Risk_Cost_MM_(+)");
                            pe.LECost = pe.CostActualImprovement + pe.CostActualRisk;
                            pe.LEDays = pe.DaysActualImprovement + pe.DaysActualRisk;
                            pe.Period = new DateRange(
                                    pip.GetDateTime("Activity_Start"),
                                    pip.GetDateTime("Activity_End")
                                );
                            pe.Allocations = null;
                            wp.Elements.Add(pe);
                            saveIt = true;
                            sb.Append("Insert new PIP Element;");

                        }
                        else
                        {
                            // update
                            DateRange period = new DateRange(
                                    pip.GetDateTime("Activity_Start"),
                                    pip.GetDateTime("Activity_End"));
                            string completion = pip.GetString("%_Complete"); //pip.GetDouble("%_Complete") / 100;
                            string highlevelclass = pip.GetString("High_Level_Classification");
                            string theme = pip.GetString("Theme");
                            double daysplanimprovement = pip.GetDouble("Opportunity__Days_(-)");
                            double daysplanrisk = pip.GetDouble("Risk__Days_(+)");
                            double costplanimprovement = pip.GetDouble("Opportunity_Cost_MM__(-)");
                            double costplanrisk = pip.GetDouble("Risk_Cost_MM_(+)");
                            double DaysCurrentWeekImprovement = pip.GetDouble("LE_Opportunity_Days_(-)");
                            double DaysCurrentWeekRisk = pip.GetDouble("LE_Risk_Days_(+)");
                            double CostCurrentWeekImprovement = pip.GetDouble("LE_Opportunity_Cost_MM_(-)");
                            double CostCurrentWeekRisk = pip.GetDouble("LE_Risk_Cost_MM_(+)");
                            string actionparty = pip.GetString("Action_Party");



                            string performanceunit = pip.GetString("Performance_Unit");
                            string assetname = pip.GetString("Asset_Name");
                            string projectname = pip.GetString("Project_Name");
                            string rigname = pip.GetString("Rig_Name");

                            wp.Elements.FirstOrDefault(d => d.Title.Equals(idea)).Period = period;
                            wp.Elements.FirstOrDefault(d => d.Title.Equals(idea)).Completion = completion;
                            wp.Elements.FirstOrDefault(d => d.Title.Equals(idea)).Classification = highlevelclass;
                            wp.Elements.FirstOrDefault(d => d.Title.Equals(idea)).Theme = theme;
                            wp.Elements.FirstOrDefault(d => d.Title.Equals(idea)).CostPlanImprovement = costplanimprovement;
                            wp.Elements.FirstOrDefault(d => d.Title.Equals(idea)).CostPlanRisk = costplanrisk;
                            wp.Elements.FirstOrDefault(d => d.Title.Equals(idea)).DaysPlanRisk = daysplanrisk;
                            wp.Elements.FirstOrDefault(d => d.Title.Equals(idea)).DaysPlanImprovement = daysplanimprovement;
                            wp.Elements.FirstOrDefault(d => d.Title.Equals(idea)).DaysCurrentWeekImprovement = DaysCurrentWeekImprovement;
                            wp.Elements.FirstOrDefault(d => d.Title.Equals(idea)).DaysCurrentWeekRisk = DaysCurrentWeekRisk;
                            wp.Elements.FirstOrDefault(d => d.Title.Equals(idea)).CostCurrentWeekImprovement = CostCurrentWeekImprovement;
                            wp.Elements.FirstOrDefault(d => d.Title.Equals(idea)).CostCurrentWeekRisk = CostCurrentWeekRisk;
                            wp.Elements.FirstOrDefault(d => d.Title.Equals(idea)).ActionParty = actionparty;
                            wp.Elements.FirstOrDefault(d => d.Title.Equals(idea)).Allocations = null;
                            //pe.ResetAllocation();
                            saveIt = true;
                            sb.Append("Update PIP Element;");

                        }
                    }

                    if (wp != null && saveIt)
                    {
                        sb.Append("Data has been Save to WEIS;");
                        //dataChangesResult.Add(wp.ToBsonDocument());
                        wp.Save();
                    }
                    else
                    {
                        sb.Append("Saving to WEIS Failed;");
                    }

                    BsonDocument doc = new BsonDocument();
                    doc = pip;
                    doc.Set("Message", sb.ToString());
                    OutMessage.Add(doc);
                    sb = new StringBuilder();

            }

            foreach (var t in OutMessage)
            {
                var complete = BsonHelper.GetString(t, "%_Complete");
                var opportunityCostM = BsonHelper.GetDouble(t, "Opportunity_Cost_MM__(-)");
                var opportunityDays = BsonHelper.GetDouble(t, "Opportunity__Days_(-)");
                var riskcostMM = BsonHelper.GetDouble(t, "Risk_Cost_MM_(+)");
                var riskdays = BsonHelper.GetDouble(t, "Risk__Days_(+)");

                t.Remove("%_Complete");
                t.Remove("Opportunity_Cost_MM__(-)");
                t.Remove("Opportunity__Days_(-)");
                t.Remove("Risk_Cost_MM_(+)");
                t.Remove("Risk__Days_(+)");

                t.Set("Complete", complete);
                t.Set("OpportunityCostMM", opportunityCostM);
                t.Set("OppoertunityDays", opportunityDays);
                t.Set("RiskCostMM", riskcostMM);
                t.Set("RiskDays", riskdays);
            }
            //return dataChangesResult;
        }
    }
}