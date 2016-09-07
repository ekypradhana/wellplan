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
using System.Text.RegularExpressions;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth(WEISRoles = "Administrators")]
    public class UploadLSController : Controller
    {
        //
        // GET: /Shell/UploadLE/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult LoadGridData()
        {
            List<BsonDocument> bdocs = new List<BsonDocument>();

            string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"]);
            bool exists = System.IO.Directory.Exists(folder);
            if (!exists)
                return Json(new { Data = "", Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);

            string[] filePaths = Directory.GetFiles(WebConfigurationManager.AppSettings["LSUploadPath"]);

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

        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase files)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                int fileSize = files.ContentLength;
                string fileName = files.FileName;
                string ContentType = files.ContentType;
                string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"]);
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

        public FileResult Download(string id)
        {
            string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"]);
            byte[] fileBytes = System.IO.File.ReadAllBytes(folder + @"\" + id);
            string fileName = id;
            return File(folder + @"\" + id, Tools.GetContentType(".xlsx"), Path.GetFileName(folder + @"\" + id));

        }

        public JsonResult LoadCollection()
        {
            try
            {
                var t = GetCollectionName();
                List<BsonDocument> docs = new List<BsonDocument>();
                foreach (string y in t)
                {
                    string yName = y.Substring(2, 6);

                    DateTime dt = new DateTime();
                    dt = DateTime.ParseExact(yName.ToString(), "yyyyMM", System.Globalization.CultureInfo.InvariantCulture);

                    BsonDocument d = new BsonDocument();
                    d.Set("CollectionName", y.ToString());
                    d.Set("LatestSequenceDate", Tools.ToUTC(dt));
                    docs.Add(d);
                }


                return Json(new { Data = DataHelper.ToDictionaryArray(docs.OrderByDescending(x => BsonHelper.GetDateTime(x, "CreateDate")).ToList()), Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
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

            foreach (string t in mongo.GetCollectionNames()
                .Where(d=>d.ToLower().StartsWith("op") && d.ToLower().EndsWith("v2"))
                .ToList())
            {
                //if (Regex.IsMatch(t, @"OP........V2", RegexOptions.IgnoreCase))
                //{
                    res.Add(t);
                //}
            }

            return res;
        }

        public List<BsonDocument> LoadLSWithConfiguration(Aspose.Cells.Workbook wb)
        {
            ECIS.Core.DataSerializer.ExtractionHelper e = new ExtractionHelper();
            ECIS.Core.DataSerializer.Entity.ExcelConfig conf = new Core.DataSerializer.Entity.ExcelConfig();
            ECIS.Core.DataSerializer.Entity.PositionTitle ps = new Core.DataSerializer.Entity.PositionTitle();

            #region Headers add
            ps = new Core.DataSerializer.Entity.PositionTitle();
            ps.Names.Add("Rig_Type");
            ps.Position = "A1";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            ps.Names.Add("Rig_Name");
            ps.Position = "B1";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            ps.Names.Add("Well_Name");
            ps.Position = "C1";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            ps.Names.Add("Activity_Type");
            ps.Position = "D1";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            ps.Names.Add("Activity_Description");
            ps.Position = "E1";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            ps.Names.Add("Start_Date");
            ps.Position = "F1";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            ps.Names.Add("End_Date");
            ps.Position = "G1";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            ps.Names.Add("Year_-_Start");
            ps.Position = "H1";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            ps.Names.Add("Year_-_End");
            ps.Position = "I1";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);
            #endregion

            var t = wb.Worksheets[0];

            string cellNameFirst = "A2";
            string LastCell = "I" + e.GetMaxRows(wb, t.Name).ToString();
            var datas = e.ExtractWithConfiguration(wb, cellNameFirst, LastCell, 0, conf).ToList();
            return datas;
        }

        public JsonResult LoadToWEIS(string FileName, DateTime DateSequence)
        {
            try
            {
                ECIS.Core.DataSerializer.ExtractionHelper e = new ECIS.Core.DataSerializer.ExtractionHelper();

                string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"]);
                string Path = folder + @"\" + FileName;
                Aspose.Cells.Workbook wb = new Aspose.Cells.Workbook(Path);


                //string first = e.GetFirstDataCell(wb, 0, "Rig Type");
                //string last = e.GetLastDataCell(wb, 0, "Year - End");
                //var latestsequence = e.ExtractWithReplace(wb, first, last, 0);


                var latestsequence = this.LoadLSWithConfiguration(wb);

                DateTime d = new DateTime();
                d = DateSequence;

                string CollectionName = "OP" + d.ToString("yyyyMMdd") + "V2";

                DataHelper.Delete(CollectionName);

                foreach (var t in latestsequence)
                {
                    DataHelper.Save(CollectionName, t);
                }

                //var rows = DataHelper.Populate(CollectionName);
                //foreach (var y in rows)
                //{
                //    DateTime sd = new DateTime();
                //    sd = DateTime.ParseExact(BsonHelper.GetString(y, "Start_Date"), "yyyy-MM-dd hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                //    DateTime ed = new DateTime();
                //    ed = DateTime.ParseExact(BsonHelper.GetString(y, "End_Date"), "yyyy-MM-dd hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                //    y.Set("Start_Date", Tools.ToUTC(sd));
                //    y.Set("End_Date", Tools.ToUTC(ed));
                //    DataHelper.Save(CollectionName, y);
                //}

                return Json(new { Data = Path.ToString(), Result = "OK", Message = "Load WellActivity Done", CollectionName = CollectionName }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message, Trace = ex.StackTrace }, JsonRequestBehavior.AllowGet);
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


        public JsonResult Execute(string id)
        {
            try
            {
                var y = DataHelper.Populate(id);

                #region Backup Current WellActivity to _WellActivity_" + nowDate2
                DateTime dateNow = DateTime.Now;
                dateNow = DateTime.Now;
                string nowDate2 = dateNow.ToString("yyyyMMddhhmm");
                
                var currentDB = DataHelper.Populate("WEISWellActivities");
                foreach (var data in currentDB)
                {
                    DataHelper.Save("_WellActivity_" + nowDate2, data);
                }
                #endregion

                #region do logic 
                WellActivity.LoadLastSequence(id);
                #endregion

                return Json(new { Data = "", Result = "OK", Message = "Load WellActivity Done" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message, Trace = ex.StackTrace }, JsonRequestBehavior.AllowGet);
            }
        }


        public JsonResult GetLatestUploadedLS()
        {
            try
            {
                var list = this.GetCollectionName();

                List<string> stringName = new List<string>();

                List<DateTime> dts = new List<DateTime>();


                if (list.Count > 0)
                {
                    foreach (var t in list)
                    {
                        DateTime dt = Tools.DefaultDate;
                        try
                        {
                            if (t.Substring(2, 8).Length >= 8)
                            {
                                dt = DateTime.ParseExact(t.Substring(2, 8), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                                dts.Add(dt);
                            }
                        }
                        catch (Exception e)
                        {
                        }
                        if (dt == Tools.DefaultDate)
                        {
                            try
                            {
                                if (t.Substring(2, 6).Length >= 6)
                                {
                                    var subs = t.Substring(2, 6) + "01";
                                    dt = DateTime.ParseExact(subs, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                                    dts.Add(dt);
                                }
                            }
                            catch (Exception e)
                            {
                            }
                        }
                    }
                    DateTime fd = dts.Max();
                    return Json(new { Data = fd.ToString("MMM yyyy"), Result = "OK", Message = "GetLatestUploadedLS Success" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { Data = "No LS Uploaded", Result = "OK", Message = "GetLatestUploadedLS Success" }, JsonRequestBehavior.AllowGet);
                }

            }
            catch (Exception ex)
            {
                return Json(new { Data = "No LS Uploaded", Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult Preview(string collName)
        {
            try
            {
                var data = DataHelper.Populate(collName);

                foreach (var t in data)
                {
                    var st = BsonHelper.Get(t, "Year_-_Start");
                    var nd = BsonHelper.Get(t, "Year_-_End");
                    t.Remove("Year_-_Start");
                    t.Remove("Year_-_End");

                    t.Set("YearStart", st);
                    t.Set("YearEnd", nd);
                }

                return Json(new { Data = DataHelper.ToDictionaryArray(data), Result = "OK", Message = "GetLatestUploadedLS Success" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = "", Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);

            }

        }
    }
}