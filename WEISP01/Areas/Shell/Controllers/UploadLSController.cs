using System.Threading;
using System.Threading.Tasks;
using ECIS.Core;
using Microsoft.Ajax.Utilities;
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
using Aspose.Cells;
using MongoDB.Bson.Serialization.Attributes;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth(WEISRoles = "Administrators")]
    public class UploadLSController : Controller
    {
        //
        // GET: /Shell/UploadLE/
        public ActionResult Index()
        {
            ViewBag.Offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
            return View();
        }

        public JsonResult LoadGridData()
        {

            return MvcResultInfo.Execute(null, document =>
            {
                List<BsonDocument> bdocs = new List<BsonDocument>();

                string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"]);
                var folder2 = System.IO.Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"], "Single");
                if (!System.IO.Directory.Exists(folder))
                    System.IO.Directory.CreateDirectory(folder);
                if (!System.IO.Directory.Exists(folder2))
                    System.IO.Directory.CreateDirectory(folder2);
                //bool exists = System.IO.Directory.Exists(folder);
                //if (!exists)
                //    return Json(new { Data = "", Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);

                string[] filePaths = Directory.GetFiles(folder);
                string[] filePaths2 = Directory.GetFiles(folder2);

                var logRunningProcess = UploadLsStatus.Populate<UploadLsStatus>();

                #region FilePath1
                foreach (string file in filePaths)
                {
                    BsonDocument doc = new BsonDocument();
                    doc.Set("FileName", System.IO.Path.GetFileName(file));
                    doc.Set("CreateDate", System.IO.File.GetCreationTimeUtc(file));

                    var fds = logRunningProcess.Where(x => x.FileName.Equals(System.IO.Path.GetFileName(file)));
                    if (fds.Any())
                    {
                        var fd = fds.OrderByDescending(x => x.LastUpdate).FirstOrDefault();
                        doc.Set("LastLoadedStatus", fd.Status);
                        doc.Set("LastLoaded", fd.LastUpdate);
                        doc.Set("Message", fd.Message ?? "");
                    }
                    else
                    {
                        doc.Set("LastLoadedStatus", "-");
                        doc.Set("LastLoaded", Tools.DefaultDate);
                        doc.Set("Message", "");
                    }

                    var uploadStatus = UploadDataMaintenance.Populate<UploadDataMaintenance>(q: Query.And(Query.EQ("FileName", System.IO.Path.GetFileName(file)), Query.EQ("Maintenance", "LS")), sort: SortBy.Descending("LastUpdate"));
                    doc.Set("UserUpload", " - ");
                    doc.Set("LastExecute", Tools.DefaultDate);
                    if (uploadStatus.Any())
                    {
                        var fd = uploadStatus.FirstOrDefault();
                        doc.Set("UserUpload", fd.UserUpload);
                        doc.Set("UserEdit", fd.UserEdit ?? fd.UserUpload);
                        if (fd.ListExecute.Any() && fd.ListExecute.Count > 0)
                        {
                            var execute = fd.ListExecute.OrderByDescending(x => x.ExecuteDate).FirstOrDefault();
                            doc.Set("LastExecuteBy", execute.UserUpdate);
                            doc.Set("LastExecute", execute.ExecuteDate);
                        }
                        else
                        {
                            doc.Set("LastExecuteBy", " - ");
                            doc.Set("LastExecute", Tools.DefaultDate);
                        }
                    }
                    doc.Set("FileType", "Full");

                    bdocs.Add(doc);
                }
                #endregion
                #region FilePath2
                foreach (string file in filePaths2)
                {
                    BsonDocument doc = new BsonDocument();
                    doc.Set("FileName", System.IO.Path.GetFileName(file));
                    doc.Set("CreateDate", System.IO.File.GetCreationTimeUtc(file));

                    var fds = logRunningProcess.Where(x => x.FileName.Equals(System.IO.Path.GetFileName(file)));
                    if (fds.Any())
                    {
                        var fd = fds.OrderByDescending(x => x.LastUpdate).FirstOrDefault();
                        doc.Set("LastLoadedStatus", fd.Status);
                        doc.Set("LastLoaded", fd.LastUpdate);
                        doc.Set("Message", fd.Message ?? "");
                    }
                    else
                    {
                        doc.Set("LastLoadedStatus", "-");
                        doc.Set("LastLoaded", Tools.DefaultDate);
                        doc.Set("Message", "");
                    }

                    var uploadStatus = UploadDataMaintenance.Populate<UploadDataMaintenance>(q: Query.And(Query.EQ("FileName", System.IO.Path.GetFileName(file)), Query.EQ("Maintenance", "LS")), sort: SortBy.Descending("LastUpdate"));
                    doc.Set("UserUpload", " - ");
                    doc.Set("LastExecute", Tools.DefaultDate);
                    if (uploadStatus.Any())
                    {
                        var fd = uploadStatus.FirstOrDefault();
                        doc.Set("UserUpload", fd.UserUpload);
                        doc.Set("UserEdit", fd.UserEdit ?? fd.UserUpload);
                        if (fd.ListExecute.Any() && fd.ListExecute.Count > 0)
                        {
                            var execute = fd.ListExecute.OrderByDescending(x => x.ExecuteDate).FirstOrDefault();
                            doc.Set("LastExecuteBy", execute.UserUpdate);
                            doc.Set("LastExecute", execute.ExecuteDate);
                        }
                        else
                        {
                            doc.Set("LastExecuteBy", " - ");
                            doc.Set("LastExecute", Tools.DefaultDate);
                        }
                    }
                    doc.Set("FileType", "Single");

                    bdocs.Add(doc);
                }
                #endregion
                //return Json(new { Data = DataHelper.ToDictionaryArray(bdocs.OrderByDescending(x => BsonHelper.GetDateTime(x, "CreateDate")).ToList()), Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
                return DataHelper.ToDictionaryArray(bdocs.OrderByDescending(x => BsonHelper.GetDateTime(x, "CreateDate")).ToList());
            });
        }

        public void updateexecute(string filename)
        {
            if (filename != null)
            {
                var uploadStatus = UploadDataMaintenance.Get<UploadDataMaintenance>(q: Query.And(Query.EQ("FileName", filename), Query.EQ("Maintenance", "LS")));
                if (uploadStatus != null)
                {
                    ExecuteUpdate update = new ExecuteUpdate() { Status = "Success", Message = "Load LS Datas Done", UserUpdate = WebTools.LoginUser.UserName, ExecuteDate = DateTime.Now };
                    uploadStatus.ListExecute.Add(update);
                    uploadStatus.Save();
                }
            }

        }

        public JsonResult EditExcel(string FileName)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"]);
                string filepath = System.IO.Path.Combine(folder, FileName);
                ExtractionHelper e = new ExtractionHelper();
                IList<BsonDocument> datas = e.Extract(filepath);

                var dt = datas.Select((x, idx) => new
                {
                    Row = (idx + 1).ToString(),
                    Rig_Type = x.GetString("Rig_Type"),
                    Rig_Name = x.GetString("Rig_Name"),
                    Well_Name = x.GetString("Well_Name"),
                    Activity_Type = x.GetString("Activity_Type"),
                    Activity_Description = x.GetString("Activity_Description"),
                    Start_Date = x.GetString("Start_Date"),
                    End_Date = x.GetString("End_Date"),
                    Year__Start = x.GetString("Year_-_Start"),
                    Year__End = x.GetString("Year_-_End")
                }).ToList();

                return dt;
                //return datas.Select(x => x.ToDictionary());
            });
        }

        public JsonResult SaveExcel(List<Dictionary<string, string>> datas, string filename)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"]);
                string filepath = System.IO.Path.Combine(folder, filename);

                var wb = new Workbook(filepath);
                var ws = wb.Worksheets[0];
                var startRow = 2;

                foreach (var xx in datas)
                {
                    var data = xx.ToBsonDocument();
                    ws.Cells["A" + startRow.ToString()].Value = data.GetString("Rig_Type");
                    ws.Cells["B" + startRow.ToString()].Value = data.GetString("Rig_Name");
                    ws.Cells["C" + startRow.ToString()].Value = data.GetString("Well_Name");
                    ws.Cells["D" + startRow.ToString()].Value = data.GetString("Activity_Type");
                    ws.Cells["E" + startRow.ToString()].Value = data.GetString("Activity_Description");

                    ws.Cells["F" + startRow.ToString()].Value = data.GetDateTime("Start_Date");
                    ws.Cells["G" + startRow.ToString()].Value = data.GetDateTime("End_Date");

                    ws.Cells["H" + startRow.ToString()].Value = data.GetString("Year__Start");
                    ws.Cells["I" + startRow.ToString()].Value = data.GetString("Year__End");
                    startRow++;
                }
                wb.Save(filepath);

                Thread.Sleep(TimeSpan.FromSeconds(1));
                ExtractionHelper e = new ExtractionHelper();
                IList<BsonDocument> dataLoad = e.Extract(filepath);
                return dataLoad.Select(x => x.ToDictionary());
            });
        }
        public JsonResult updateEdit(string filename)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                if (filename != null)
                {
                    var uploadStatus = UploadDataMaintenance.Get<UploadDataMaintenance>(q: Query.And(Query.EQ("FileName", filename), Query.EQ("Maintenance", "LS")));
                    if (uploadStatus != null)
                    {
                        uploadStatus.UserEdit = WebTools.LoginUser.UserName;
                        uploadStatus.Save();
                    }
                }
                return null;
            });

        }


        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase files)
        {
            ResultInfo ri = new ResultInfo();
            int fileSize = files.ContentLength;
            string fileName = files.FileName;
            string ContentType = files.ContentType;
            //this is for Full LS. if the file is not single rig, it will be this default.
            string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"]);
            bool exists = System.IO.Directory.Exists(folder);

            var folderSingleRig = Path.Combine(folder, "Single");
            bool singleExists = System.IO.Directory.Exists(folderSingleRig);

            string fileNameReplace = DateTime.Now.ToString("yyyMMddhhmmss") + "-" + fileName;

            if (!exists)
                System.IO.Directory.CreateDirectory(folder);
            if (!singleExists)
                System.IO.Directory.CreateDirectory(folderSingleRig);

            string filepath = System.IO.Path.Combine(folder, fileNameReplace);
            List<string> ErrorMessage = new List<string>();
            try
            {

                files.SaveAs(filepath);
                Thread.Sleep(TimeSpan.FromSeconds(1));

                ExtractionHelper e = new ExtractionHelper();
                IList<BsonDocument> datas = e.Extract(filepath);

                //datas = datas.Where(x =>
                //    !String.IsNullOrEmpty(x.GetString("Rig_Type").Trim()) &&
                //    !String.IsNullOrEmpty(x.GetString("Rig_Name").Trim()) &&
                //    !String.IsNullOrEmpty(x.GetString("Activity_Type").Trim()) &&
                //    !String.IsNullOrEmpty(x.GetString("Well_Name").Trim())
                //    ).ToList();
                var countData = datas.Count();
                var walkingCounter = 0;
                var masterRigType = MasterRigType.Populate<MasterRigType>();
                var masterRigName = MasterRigName.Populate<MasterRigName>();
                var counter = 1;
                var _data = new List<BsonDocument>();


                var distinctRigName = new List<string>();


                foreach (var data in datas)
                {
                    var counterInside = 3;
                    counter++;
                    var dataRigType = data.GetString("Rig_Type");
                    var dataRigName = data.GetString("Rig_Name");
                    var dataActType = data.GetString("Activity_Type");
                    var dataWellName = data.GetString("Well_Name");

                    if (!String.IsNullOrEmpty(dataRigName))
                        distinctRigName.Add(dataRigName);

                    #region Check empty mandatory
                    if (String.IsNullOrEmpty(dataRigType))
                    {
                        ErrorMessage.Add("- Row " + counter + " Rig Type should be filled");
                        counterInside--;
                    }
                    if (String.IsNullOrEmpty(dataRigName))
                    {
                        ErrorMessage.Add("- Row " + counter + " Rig Name should be filled");
                        counterInside--;
                    }
                    if (String.IsNullOrEmpty(dataActType))
                    {
                        ErrorMessage.Add("- Row " + counter + " Activity Type should be filled");
                        counterInside--;
                    }
                    if (String.IsNullOrEmpty(dataWellName))
                    {
                        ErrorMessage.Add("- Row " + counter + " Well Name should be filled");
                        counterInside--;
                    }
                    #endregion

                    #region check well
                    if (!String.IsNullOrEmpty(dataRigType))
                    {
                        var rigTypeCheck = masterRigType.FirstOrDefault(x => x._id.ToString() == dataRigType);
                        if (rigTypeCheck == null)
                        {
                            ErrorMessage.Add("- Row " + counter + " Rig Type '" + dataRigType + "' is not in the system.");
                            counterInside--;
                        }
                    }
                    
                    #endregion

                    #region check Rig
                    if (!String.IsNullOrEmpty(dataRigName))
                    {
                        var rigCheck = masterRigName.FirstOrDefault(x => x._id.ToString() == dataRigName);
                        if (rigCheck == null)
                        {
                            ErrorMessage.Add("- Row " + counter + " Rig Name '" + dataRigName + "' is not in the system.");
                            counterInside--;
                        }
                    }
                    
                    #endregion

                    if (counterInside == 3)
                    {
                        walkingCounter++;
                    }
                    data.Set("Row", counter);
                    _data.Add(data);
                }


                #region check duplicate
                var gg = datas.Select(x => new
                {
                    rigType = x.GetString("Rig_Type"),
                    rigName = x.GetString("Rig_Name"),
                    ActType = x.GetString("Activity_Type"),
                    wellName = x.GetString("Well_Name")
                }).Distinct();

                var listDuplicate = new List<BsonDocument>();
                foreach (var data in gg)
                {
                    var b = new BsonDocument();
                    var listStr = new List<int>();

                    var counter_data = 1;
                    foreach (var xx in _data)
                    {
                        counter_data++;
                        var xxRigType = xx.GetString("Rig_Type");
                        var xxRigName = xx.GetString("Rig_Name");
                        var xxActType = xx.GetString("Activity_Type");
                        var xxWellName = xx.GetString("Well_Name");
                        var xxRow = xx.GetInt32("Row");

                        if (data.rigType == xxRigType && data.rigName == xxRigName && data.ActType == xxActType && data.wellName == xxWellName)
                        {
                            listStr.Add(xxRow);
                        }
                    }


                    if (listStr.Count() > 1)
                    {
                        b.Set("rigType", data.rigType);
                        b.Set("rigName", data.rigName);
                        b.Set("ActType", data.ActType);
                        b.Set("wellName", data.wellName);
                        b.Set("duplicate", String.Join(",", listStr));
                        listDuplicate.Add(b);
                    }
                }

                foreach (var xx in listDuplicate)
                {
                    walkingCounter = 0;
                    var stringCombine =
                        String.Format(
                            "- Rig Type <b>'{0}'</b>, Rig Name <b>'{1}'</b>, Well Name <b>'{3}'</b> and Activity <b>'{2}'</b> is duplicate at row <b>{4}</b>", xx.GetString("rigType"), xx.GetString("rigName"), xx.GetString("wellName"), xx.GetString("ActType"), xx.GetString("duplicate"));
                    ErrorMessage.Add(stringCombine);
                }
                #endregion

                if (countData != walkingCounter)
                {
                    System.IO.File.Delete(filepath);
                    ri.Result = "NOK";
                    ri.Data = ErrorMessage;
                    //throw new Exception("File refuse to upload. Some datas are not exist in the business plan or Currency is not in the list or not OP16.");
                }
                else
                {
                    //identify if distinct rig name is single or not
                    var distinctRig = distinctRigName.Distinct();
                    if (distinctRig.Count() == 1)
                    {
                        // Move the file to folder single.
                        string filePathToMove = System.IO.Path.Combine(folderSingleRig, fileNameReplace);
                        if (System.IO.File.Exists(filePathToMove))
                            System.IO.File.Delete(filePathToMove);

                        System.IO.File.Move(filepath, filePathToMove);

                        CreateUserUpload(fileNameReplace, true);
                    }
                    CreateUserUpload(fileNameReplace);
                }
            }
            catch (Exception e)
            {
                System.IO.File.Delete(filepath);
                ri.PushException(e);
            }
            //ri.CalcLapseTime();
            return ri.ToJsonResult();
        }

        public void CreateUserUpload(string filename, bool isSingle = false)
        {
            var uploadStatus = UploadDataMaintenance.Get<UploadDataMaintenance>(q: Query.EQ("FileName", filename));
            if (uploadStatus == null)
            {
                string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"]);
                if (isSingle)
                    folder = Path.Combine(folder, "Single");
                string files = Directory.GetFiles(folder).Where(x => System.IO.Path.GetFileName(x).Equals(filename)).FirstOrDefault();
                string file = System.IO.Path.GetFileName(files);
                uploadStatus = new UploadDataMaintenance();
                uploadStatus.ListExecute = new List<ExecuteUpdate>();
                uploadStatus.Maintenance = "LS";
                uploadStatus.Path = System.IO.Path.Combine(folder, file);
                uploadStatus.FileName = filename;
                uploadStatus.UploadDate = System.IO.File.GetCreationTimeUtc(file);
                uploadStatus.UserUpload = WebTools.LoginUser.UserName;
                uploadStatus.UserEdit = WebTools.LoginUser.UserName;
                uploadStatus.Save();
            }

        }

        public FileResult Download(string id, string fileType = "Full")
        {
            string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"]);
            if (fileType == "Single")
                folder = Path.Combine(folder, "Single");
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
                var client = new MongoClient("mongodb://" + connectionString);
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
                .Where(d => d.ToLower().StartsWith("op") && d.ToLower().EndsWith("v2"))
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

        public List<BsonDocument> LoadLSNew(Aspose.Cells.Workbook wb)
        {
            ECIS.Core.DataSerializer.ExtractionHelper e = new ExtractionHelper();
            var datas = e.Extract(wb).ToList();
            return datas;
        }
        public List<BsonDocument> LoadLSNew(string Path)
        {
            ECIS.Core.DataSerializer.ExtractionHelper e = new ExtractionHelper();
            var datas = e.Extract(Path);
            return datas.ToList();
        }

        public JsonResult CheckProcess()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var status = UploadLsStatus.Populate<UploadLsStatus>(Query.EQ("Status", "In Progress"));
                var listWorkingFile = new List<string>();
                if (status.Any())
                {
                    var aa = status.Select(x => x.FileName).ToList();
                    listWorkingFile.AddRange(aa);
                }
                return listWorkingFile;
            });
        }

        public JsonResult ShowSuccessedFileCompare()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var res = new List<BsonDocument>();
                var logRunningProcess = UploadLsStatus.Populate<UploadLsStatus>(q: Query.EQ("Status", "Success"), sort: SortBy.Descending("LastUpdate"));
                var cnt = logRunningProcess.Count();
                if (cnt >= 2)
                {
                    for (int i = 0; i < cnt - 1; i++)
                    {
                        var data1 = logRunningProcess[i];
                        var data2 = logRunningProcess[i + 1];

                        var tt = new BsonDocument();
                        tt.Set("FileName1", data1.FileName);
                        tt.Set("FileName2", data2.FileName);
                        tt.Set("LastUpdate1", data1.LastUpdate);
                        tt.Set("LastUpdate2", data2.LastUpdate);
                        tt.Set("UserUpdate1", data1.UserUpdate ?? "");
                        tt.Set("UserUpdate2", data2.UserUpdate ?? "");
                        res.Add(tt);
                    }
                }
                return res.Select(x => x.ToDictionary());
            });
        }

        public JsonResult ShowLog()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var ret = new List<BsonDocument>();
                //var last = DataHelper.Populate("LatestLogLatestLS");

                var latest1 = WellActivity.GetLastExecuteDateLS();//DataHelper.Get("LogLatestLS", null, SortBy.Descending("Executed_At"));


                if (latest1 != Tools.DefaultDate)
                {

                    DateTime d = latest1;//latest1.GetDateTime("Executed_At");
                    var latest = DataHelper.Populate("LogLatestLS", Query.GTE("Executed_At", Tools.ToUTC(d)));
                    foreach (var xx in latest)
                    {
                        DateTime start = xx.GetDateTime("Start_Date");
                        DateTime end = xx.GetDateTime("End_Date");

                        #region not used
                        //try
                        //{
                        //    start = DateTime.ParseExact(xx.GetString("Start_Date").Trim(), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                        //    end = DateTime.ParseExact(xx.GetString("End_Date").Trim(), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                        //}
                        //catch (Exception ec)
                        //{ }
                        #endregion

                        var well = xx.GetString("Well_Name");
                        var rig = xx.GetString("Rig_Name");
                        var act = xx.GetString("Activity_Type");
                        var execFrom = xx.GetString("Executed_From");
                        xx.Set("Year_Start", Tools.ToUTC(start).Year);
                        xx.Set("Year_End", Tools.ToUTC(end).Year);
                        xx.Set("Status", "New");
                        xx.Set("Start_Date", Tools.ToUTC(start));//.ToString("dd-MMM-yyyy"));
                        xx.Set("End_Date", Tools.ToUTC(end));//.ToString("dd-MMM-yyyy"));
                        xx.Set("End_Date", Tools.ToUTC(end));//.ToString("dd-MMM-yyyy"));
                        if (String.IsNullOrEmpty(execFrom))
                            xx.Set("Executed_From", "Full");
                        //xx.Set("Start_Date", start.ToString("dd-MMM-yyyy"));
                        //xx.Set("End_Date", end.ToString("dd-MMM-yyyy"));
                        // xx.Set("Executed_At", xx.GetDateTime("Executed_At").ToString("yyyy-MM-dd HH:mm:ss"));
                        var execAt = xx.GetDateTime("Last_Edited_Date");
                        var execBy = xx.GetString("Last_Edited_By");
                        if (execAt == Tools.DefaultDate)
                        {
                            xx.Set("Executed_At", Tools.ToUTC(xx.GetDateTime("Executed_At")));
                        }
                        else
                        {
                            xx.Set("Executed_By", execBy);
                            xx.Set("Executed_At", Tools.ToUTC(execAt));
                        }

                        ret.Add(xx);
                    }
                }
                else
                {
                    var latest = DataHelper.Populate("LogLatestLS");
                    foreach (var xx in latest)
                    {

                        DateTime start = xx.GetDateTime("Start_Date");
                        DateTime end = xx.GetDateTime("End_Date");

                        #region not used
                        //try
                        //{
                        //    start = DateTime.ParseExact(xx.GetString("Start_Date").Trim(), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                        //    end = DateTime.ParseExact(xx.GetString("End_Date").Trim(), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                        //}
                        //catch (Exception ec)
                        //{ }
                        #endregion


                        var well = xx.GetString("Well_Name");
                        var rig = xx.GetString("Rig_Name");
                        var act = xx.GetString("Activity_Type");
                        var execFrom = xx.GetString("Executed_From");
                        xx.Set("Year_Start", Tools.ToUTC(start).Year);
                        xx.Set("Year_End", Tools.ToUTC(end).Year);
                        xx.Set("Status", "New");
                        xx.Set("Start_Date", Tools.ToUTC(start));//.ToString("dd-MMM-yyyy"));
                        xx.Set("End_Date", Tools.ToUTC(end));//.ToString("dd-MMM-yyyy"));
                        // xx.Set("Executed_At", xx.GetDateTime("Executed_At").ToString("yyyy-MM-dd HH:mm:ss"));
                        if (String.IsNullOrEmpty(execFrom))
                            xx.Set("Executed_From", "Full");

                        var execAt = xx.GetDateTime("Last_Edited_Date");
                        var execBy = xx.GetString("Last_Edited_By");
                        if (execAt == Tools.DefaultDate)
                        {
                            xx.Set("Executed_At", Tools.ToUTC(xx.GetDateTime("Executed_At")));
                        }
                        else
                        {
                            xx.Set("Executed_By", execBy);
                            xx.Set("Executed_At", Tools.ToUTC(execAt));
                        }
                        ret.Add(xx);
                    }
                }
                #region Not Used


                //foreach (var xx in last)
                //{
                //    var well = xx.GetString("Well_Name");
                //    var rig = xx.GetString("Rig_Name");
                //    var act = xx.GetString("Activity_Type");

                //    var checker =
                //        ret.FirstOrDefault(
                //            x =>
                //                x.GetString("Well_Name").Equals(well) && 
                //                x.GetString("Rig_Name").Equals(rig) &&
                //                x.GetString("Activity_Type").Equals(act));
                //    if (checker != null)
                //    {
                //        checker.Set("Status", "Update");
                //    }
                //    else
                //    {
                //        xx.Set("Status", "Removed");
                //        ret.Add(xx);
                //    }
                //}
                #endregion
                return ret.OrderBy(x => x.GetDateTime("Start_Date")).Select(x => x.ToDictionary());
            });
        }

        public JsonResult GenerateFileChangeLog(string file1, string file2)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                //string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"]);
                //byte[] fileBytes = System.IO.File.ReadAllBytes(folder + @"\" + id);

                var partFile1 = Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"], file1);
                var partFile2 = Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"], file2);

                //di balik. 
                ExtractionHelper e = new ExtractionHelper();
                IList<BsonDocument> last = e.Extract(partFile2);
                IList<BsonDocument> latest = e.Extract(partFile1);

                #region Data
                var ret = new List<BsonDocument>();
                //var last = DataHelper.Populate("LatestLogLatestLS");
                //var latest = DataHelper.Populate("LogLatestLS");
                foreach (var xx in latest)
                {
                    var well = xx.GetString("Well_Name");
                    var rig = xx.GetString("Rig_Name");
                    var act = xx.GetString("Activity_Type");
                    xx.Set("Status", "New");
                    ret.Add(xx);
                }

                foreach (var xx in last)
                {
                    var well = xx.GetString("Well_Name");
                    var rig = xx.GetString("Rig_Name");
                    var act = xx.GetString("Activity_Type");

                    var checker =
                        ret.FirstOrDefault(
                            x =>
                                x.GetString("Well_Name").Equals(well) &&
                                x.GetString("Rig_Name").Equals(rig) &&
                                x.GetString("Activity_Type").Equals(act));
                    if (checker != null)
                    {
                        checker.Set("Status", "Update");
                    }
                    else
                    {
                        xx.Set("Status", "Removed");
                        ret.Add(xx);
                    }
                }
                #endregion

                string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "ChangeLogLSTemplate.xlsx");
                Workbook wb = new Workbook(xlst);
                var ws = wb.Worksheets[0];
                int startRow = 2;

                foreach (var xx in ret)
                {
                    ws.Cells["A" + startRow.ToString()].Value = xx.GetString("Rig_Type");
                    ws.Cells["B" + startRow.ToString()].Value = xx.GetString("Rig_Name");
                    ws.Cells["C" + startRow.ToString()].Value = xx.GetString("Well_Name");
                    ws.Cells["D" + startRow.ToString()].Value = xx.GetString("Activity_Type");
                    ws.Cells["E" + startRow.ToString()].Value = xx.GetString("Status");
                    startRow++;
                }

                var fileName = String.Format("Change Log Upload LS-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss"));
                var newFileName = Path.Combine(Server.MapPath("~/App_Data/Temp"), fileName);

                //string returnName = String.Format("Business Plan-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss"));

                wb.Save(newFileName, Aspose.Cells.SaveFormat.Xlsx);
                return fileName;
            });
        }

        public FileResult DownloadLSFile(string stringName)
        {
            var res = Path.Combine(Server.MapPath("~/App_Data/Temp"), stringName);
            //rename file
            var fileName = String.Format("Change Log Upload LS-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss"));

            return File(res, Tools.GetContentType(".xlsx"), fileName);
        }
        public void LatestLSDifferences(List<WellActivity> currentDB, string CollName)
        {
            DataHelper.Delete(new LatestLSDifferences().TableName);
            List<BsonDocument> msts = DataHelper.Populate(CollName).OrderBy(d => d.GetString("_id")).ToList();

            foreach (var mst in msts)
            {

                try
                {
                    //-- init vars
                    var RigType = mst.GetString("Rig_Type");
                    var RigName = mst.GetString("Rig_Name");
                    var WellName = mst.GetString("Well_Name");
                    var ActivityType = mst.GetString("Activity_Type");
                    var DateStart = Tools.ToUTC(mst.GetDateTime("Start_Date", true));
                    var DateEnd = Tools.ToUTC(mst.GetDateTime("End_Date", true));
                    var LSSchedule = new DateRange() { Start = DateStart, Finish = DateEnd };

                    // get wa
                    var qs = new List<IMongoQuery>();
                    qs.Add(Query.EQ("WellName", WellName));
                    qs.Add(Query.EQ("RigName", RigName));
                    qs.Add(Query.EQ("RigType", RigType));
                    qs.Add(Query.EQ("Phases.ActivityType", ActivityType));
                    //var wa = WellActivity.Get<WellActivity>(Query.And(qs));
                    WellActivity wa = null;
                    var was = currentDB.Where(x => x.WellName.Equals(WellName) && x.RigName.Equals(RigName) && x.RigType.Equals(RigType));
                    if (was != null && was.Count() > 0)
                    {
                        foreach (var t in was)
                        {
                            if (t.Phases.Where(x => x.ActivityType.Equals(ActivityType)) != null && t.Phases.Where(x => x.ActivityType.Equals(ActivityType)).Count() > 0)
                            {
                                wa = t;
                            }
                        }
                    }

                    var LSDiff = new LatestLSDifferences();
                    LSDiff.WellName = WellName;
                    LSDiff.RigName = RigName;
                    LSDiff.RigType = RigType;
                    LSDiff.ActivityType = ActivityType;
                    LSDiff.LSDuration = LSSchedule.Days;
                    LSDiff.LSSchedule = LSSchedule;

                    if (wa == null)
                    {
                        LSDiff.DifferentValue = LSSchedule.Days;
                        LSDiff.DifferentType = "new";
                        LSDiff.PrevLSSchedule = new DateRange();
                        LSDiff.PrevLSDuration = 0;
                        LSDiff.isDifferent = true;

                        LSDiff.SequenceId = null;
                        LSDiff.BaseOPs = new List<string>();// wa.Phases.Where(x => x.ActivityType.Equals(ActivityType)).Any() ? wa.Phases.Where(x => x.ActivityType.Equals(ActivityType)).FirstOrDefault().BaseOP : new List<string>();

                        LSDiff.Save();
                    }
                    else
                    {

                        LSDiff.SequenceId = wa.UARigSequenceId;
                        LSDiff.BaseOPs = wa.Phases.Where(x => x.ActivityType.Equals(ActivityType)).Any() ? wa.Phases.Where(x => x.ActivityType.Equals(ActivityType)).FirstOrDefault().BaseOP : new List<string>();

                        // have event already in DB
                        WellActivityPhase matchedOP = wa.Phases.Where(x => x.ActivityType.Equals(ActivityType)).FirstOrDefault();

                        var PrevPhSchedule = matchedOP.PhSchedule;
                        var DiffValue = LSSchedule.Days - PrevPhSchedule.Days;
                        var DiffType = "";

                        LSDiff.PrevLSSchedule = PrevPhSchedule;
                        LSDiff.PrevLSDuration = PrevPhSchedule.Days;

                        if (PrevPhSchedule.Days != LSSchedule.Days)
                        {
                            if (PrevPhSchedule.Days > LSSchedule.Days)
                                DiffType = "decrease";
                            else
                                DiffType = "increase";
                            DiffValue = PrevPhSchedule.Days - LSSchedule.Days;
                            if (DiffType == "increase")
                                DiffValue = Math.Abs(DiffValue);
                            else
                                DiffValue = DiffValue * -1;

                            LSDiff.isDifferent = true;
                            LSDiff.DifferentType = DiffType;
                            LSDiff.DifferentValue = DiffValue;
                        }
                        else
                        {
                            // no of days same
                            DiffType = "equal";
                            LSDiff.isDifferent = false;
                            LSDiff.DifferentType = DiffType;
                            LSDiff.DifferentValue = 0;

                        }
                        LSDiff.Save();

                    }
                }
                catch (Exception ex)
                {

                }
            }

            MasterAlias.MasterCleansing(new LatestLSDifferences().TableName, "ActivityType");

        }

        public void LoadToWeisTaskRun(string FileName, DateTime DateSequence, UploadLsStatus recordStatus, string Path, string CollectionName, string fileType = "Full")
        {
            //as Documentation
            //If Upload file type == Full
            //Last edited by and Last Edited By wil be null. 
            //If Upload file type == Single or Manual
            //Last edited by and Last Edited By wil be recorded as executer and today. 

            try
            {
                DataHelper.Delete(CollectionName);
                if (fileType == "Full")
                    DataHelper.Delete("LogLatestLS");

                var latestsequence = this.LoadLSNew(Path);
                latestsequence = latestsequence.Where(x =>
                    !String.IsNullOrEmpty(x.GetString("Rig_Type").Trim()) &&
                    !String.IsNullOrEmpty(x.GetString("Rig_Name").Trim()) &&
                    !String.IsNullOrEmpty(x.GetString("Activity_Type").Trim()) &&
                    !String.IsNullOrEmpty(x.GetString("Well_Name").Trim())
                    ).ToList();
                //var latestsequence = this.LoadLSWithConfiguration(wb);

                DateTime nowDate = DateTime.Now;
                List<BsonDocument> datax = new List<BsonDocument>();

                if (fileType == "Full")
                {
                    #region If Filetype is Full

                    foreach (var t in latestsequence)
                    {
                        //var fmtId = String.Format("{0}|{1}|{2}|{3}", t.GetString("Rig_Type").Trim(), t.GetString("Rig_Name").Trim(), t.GetString("Well_Name").Trim(), t.GetString("Activity_Type").Trim());
                        //t.Set("_id", fmtId);
                        t.Set("Executed_By", recordStatus.UserUpdate);
                        t.Set("Executed_At", Tools.ToUTC(nowDate));

                        t.Set("Rig_Type", t.GetString("Rig_Type").Trim());
                        t.Set("Rig_Name", t.GetString("Rig_Name").Trim());
                        t.Set("Well_Name", t.GetString("Well_Name").Trim());
                        t.Set("Activity_Type", t.GetString("Activity_Type").Trim());
                        t.Set("Executed_From", "Full");


                        DateTime start = new DateTime();
                        DateTime finish = new DateTime();
                        try
                        {
                            start = DateTime.ParseExact(t.GetString("Start_Date"), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch (Exception ec)
                        {
                            try
                            {
                                start = DateTime.ParseExact(t.GetString("Start_Date"), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            catch(Exception ex)
                            {
                                start = Tools.DefaultDate;

                            }
                        }
                        try
                        {
                            finish = DateTime.ParseExact(t.GetString("End_Date"), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch (Exception ec)
                        {
                            try
                            {
                                finish = DateTime.ParseExact(t.GetString("End_Date"), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            catch (Exception ex)
                            {
                                finish = Tools.DefaultDate;
                            }
                        }

                        t.Set("Start_Date", Tools.ToUTC(start));
                        t.Set("End_Date", Tools.ToUTC(finish));

                        t.Set("Last_Edited_By", "");
                        t.Set("Last_Edited_Date", Tools.DefaultDate);

                        //remove unused field. 
                        t.Remove("Loaded_Via");
                        t.Remove("Loaded_By");
                        t.Remove("Last_Load_Date");

                        datax.Add(t);
                    }
                    DataHelper.Save(CollectionName, datax.ToList());
                    WellActivity.SetLastDateUploadedLS(Tools.ToUTC(nowDate), Tools.ToUTC(DateSequence, true), recordStatus.UserUpdate);

                    #endregion
                }
                else
                {
                    #region if Filetype is Single Rig

                    var getRig = latestsequence.Select(x => x.GetString("Rig_Name")).Distinct();
                    var allDataRig = DataHelper.Populate("LogLatestLS", Query.EQ("Rig_Name", getRig.FirstOrDefault()));
                    foreach (var t in latestsequence)
                    {
                        var getDataFirst = allDataRig.FirstOrDefault(x =>
                                x.GetString("Rig_Type") == t.GetString("Rig_Type").Trim() &&
                                x.GetString("Rig_Name") == t.GetString("Rig_Name").Trim() &&
                                x.GetString("Well_Name") == t.GetString("Well_Name").Trim() &&
                                x.GetString("Activity_Type") == t.GetString("Activity_Type").Trim()
                            );
                        DataHelper.Delete("LogLatestLS", Query.And(
                                Query.EQ("Rig_Type", t.GetString("Rig_Type").Trim()),
                                Query.EQ("Rig_Name", t.GetString("Rig_Name").Trim()),
                                Query.EQ("Well_Name", t.GetString("Well_Name").Trim()),
                                Query.EQ("Activity_Type", t.GetString("Activity_Type").Trim())
                            ));

                        t.Set("Rig_Type", t.GetString("Rig_Type").Trim());
                        t.Set("Rig_Name", t.GetString("Rig_Name").Trim());
                        t.Set("Well_Name", t.GetString("Well_Name").Trim());
                        t.Set("Activity_Type", t.GetString("Activity_Type").Trim());
                        t.Set("Executed_From", "Single");


                        DateTime start = new DateTime();
                        DateTime finish = new DateTime();
                        try
                        {
                            start = DateTime.ParseExact(t.GetString("Start_Date"), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch (Exception ec)
                        {
                            try
                            {
                                start = DateTime.ParseExact(t.GetString("Start_Date"), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            catch (Exception ex)
                            {
                                start = Tools.DefaultDate;

                            }
                        }
                        try
                        {
                            finish = DateTime.ParseExact(t.GetString("End_Date"), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch (Exception ec)
                        {
                            try
                            {
                                finish = DateTime.ParseExact(t.GetString("End_Date"), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            catch (Exception ex)
                            {
                                finish = Tools.DefaultDate;
                            }
                        }

                        t.Set("Start_Date", Tools.ToUTC(start));
                        t.Set("End_Date", Tools.ToUTC(finish));

                        if (getDataFirst != null)
                        {
                            t.Set("Executed_By", getDataFirst.GetString("Executed_By"));
                            t.Set("Executed_At", Tools.ToUTC(getDataFirst.GetDateTime("Executed_At")));
                        }
                        else
                        {
                            t.Set("Executed_By", recordStatus.UserUpdate);
                            t.Set("Executed_At", Tools.ToUTC(nowDate));
                        }
                        t.Set("Last_Edited_By", recordStatus.UserUpdate);
                        t.Set("Last_Edited_Date", Tools.ToUTC(nowDate));

                        //remove unused field. 
                        t.Remove("Loaded_Via");
                        t.Remove("Loaded_By");
                        t.Remove("Last_Load_Date");

                        datax.Add(t);
                    }
                    DataHelper.Delete("LogLatestLS", Query.EQ("Rig_Name", getRig.FirstOrDefault()));
                    DataHelper.Save(CollectionName, datax.ToList());
                    WellActivity.SetLastDateUploadedLS(null, Tools.ToUTC(DateSequence, true), recordStatus.UserUpdate);
                    #endregion
                }
                DataHelper.Save("LogLatestLS", datax.ToList());
                MasterAlias.MasterCleansing("LogLatestLS", "Activity_Type");

                MasterAlias.MasterCleansing(CollectionName, "Activity_Type");

                List<WellActivity> currentDB = new List<WellActivity>();
                currentDB = DataHelper.Populate<WellActivity>("WEISWellActivities");

                Execute(currentDB, CollectionName, recordStatus.UserUpdate);
                LatestLSDifferences(currentDB, "LogLatestLS");

                recordStatus.ExecuteDate = DateSequence;
                recordStatus.CollectionName = CollectionName;

                recordStatus.Status = "Success";
                recordStatus.Save();
            }
            catch (Exception e)
            {
                recordStatus.Message = e.ToString();
                recordStatus.Status = "Failed";
                recordStatus.Save();
                throw;
            }
        }

        public void ManualLoadToWeisTaskRun(List<BsonDocument> Data, DateTime DateSequence, UploadLsStatus recordStatus, string CollectionName)
        {
            try
            {
                //var latestsequence = this.LoadLSWithConfiguration(wb);
                DataHelper.Delete(CollectionName);
                DateTime nowDate = DateTime.Now;
                List<BsonDocument> datax = new List<BsonDocument>();

                #region if Filetype is Single Rig
                foreach (var t in Data)
                {
                    var getDataFirst = DataHelper.Get("LogLatestLS", Query.And(
                                Query.EQ("Rig_Type", t.GetString("Rig_Type").Trim()),
                                Query.EQ("Rig_Name", t.GetString("Rig_Name").Trim()),
                                Query.EQ("Well_Name", t.GetString("Well_Name").Trim()),
                                Query.EQ("Activity_Type", t.GetString("Activity_Type").Trim())
                            ));
                    DataHelper.Delete("LogLatestLS", Query.And(
                            Query.EQ("Rig_Type", t.GetString("Rig_Type").Trim()),
                            Query.EQ("Rig_Name", t.GetString("Rig_Name").Trim()),
                            Query.EQ("Well_Name", t.GetString("Well_Name").Trim()),
                            Query.EQ("Activity_Type", t.GetString("Activity_Type").Trim())
                        ));
                    var tempStart = Tools.ToUTC(t.GetDateTime("Start_Date"), true);
                    var tempEnd = Tools.ToUTC(t.GetDateTime("End_Date"), true);

                    t.Remove("Status");
                    t.Remove("defaults");
                    t.Remove("fields");
                    t.Remove("Start_Date");
                    t.Remove("End_Date");

                    t.Set("Start_Date", tempStart.ToString("yyyy-MM-dd 00:00:00"));
                    t.Set("End_Date", tempEnd.ToString("yyyy-MM-dd 00:00:00"));

                    t.Set("Rig_Type", t.GetString("Rig_Type").Trim());
                    t.Set("Rig_Name", t.GetString("Rig_Name").Trim());
                    t.Set("Well_Name", t.GetString("Well_Name").Trim());
                    t.Set("Activity_Type", t.GetString("Activity_Type").Trim());
                    t.Set("Executed_From", "Manual");

                    if (getDataFirst != null)
                    {
                        t.Set("Executed_By", getDataFirst.GetString("Executed_By"));
                        t.Set("Executed_At", Tools.ToUTC(getDataFirst.GetDateTime("Executed_At")));
                    }
                    else
                    {
                        t.Set("Executed_By", recordStatus.UserUpdate);
                        t.Set("Executed_At", Tools.ToUTC(nowDate));
                    }
                    t.Set("Last_Edited_By", recordStatus.UserUpdate);
                    t.Set("Last_Edited_Date", Tools.ToUTC(nowDate));

                    datax.Add(t);
                }
                DataHelper.Save(CollectionName, datax.ToList());
                DataHelper.Save("LogLatestLS", datax.ToList());
                MasterAlias.MasterCleansing("CollectionName", "Activity_Type");
                MasterAlias.MasterCleansing("LogLatestLS", "Activity_Type");
                #endregion

                List<WellActivity> currentDB = new List<WellActivity>();
                currentDB = DataHelper.Populate<WellActivity>("WEISWellActivities");

                Execute(currentDB, CollectionName, recordStatus.UserUpdate);
                LatestLSDifferences(currentDB, "LogLatestLS");

                recordStatus.ExecuteDate = DateSequence;
                recordStatus.CollectionName = CollectionName;

                recordStatus.Status = "Success";
                recordStatus.Save();
            }
            catch (Exception e)
            {
                recordStatus.Message = e.ToString();
                recordStatus.Status = "Failed";
                recordStatus.Save();
                throw;
            }
        }
        public JsonResult LoadToWEIS(string FileName, string fileType, DateTime DateSequence, string fileLocation = "")
        {
            try
            {
                string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"]);
                if (fileType == "Single" || fileLocation.Equals("Single"))
                    folder = System.IO.Path.Combine(folder, "Single");
                string Path = folder + @"\" + FileName;
                DateTime d = new DateTime();
                d = DateSequence;

                var CollectionName = "OP" + d.ToString("yyyyMMdd") + "V2";
                if (fileType == "Single")
                    CollectionName = "Single" + CollectionName;

                //create collection that will save upload status
                var uploadStatus = new UploadLsStatus() { Status = "In Progress", Path = Path, FileName = FileName, UserUpdate = WebTools.LoginUser.UserName, ExecuteDate = DateSequence, CollectionName = CollectionName };
                uploadStatus.Save();
                var a = UploadLsStatus.Populate<UploadLsStatus>(q: Query.EQ("Path", Path)) ?? new List<UploadLsStatus>();
                uploadStatus = a.OrderByDescending(x => x.LastUpdate).FirstOrDefault() ?? new UploadLsStatus();

                Task.Run(() =>
                {
                    LoadToWeisTaskRun(FileName, DateSequence, uploadStatus, Path, CollectionName, fileType);
                });

                // ECIS.Core.DataSerializer.ExtractionHelper e = new ECIS.Core.DataSerializer.ExtractionHelper();

                // string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"]);
                // string Path = folder + @"\" + FileName;
                // DateTime d = new DateTime();
                // d = DateSequence;

                // var CollectionName = "OP" + d.ToString("yyyyMMdd") + "V2";
                // //Aspose.Cells.Workbook wb = new Aspose.Cells.Workbook(Path);

                // var latestsequence = this.LoadLSNew(Path);
                ////var latestsequence = this.LoadLSWithConfiguration(wb);

                // Execute(CollectionName);
                // DataHelper.Delete(CollectionName);
                // DataHelper.Delete("LogLatestLS");

                // //create collection that will save upload status
                // var uploadStatus = new UploadLsStatus() { Status = "In Progress", Path = Path, FileName = FileName };
                // uploadStatus.Save();
                // var a = UploadLsStatus.Populate<UploadLsStatus>(q: Query.EQ("Path", Path)) ?? new List<UploadLsStatus>();
                // uploadStatus = a.OrderByDescending(x => x.LastUpdate).FirstOrDefault() ?? new UploadLsStatus();

                // foreach (var t in latestsequence)
                // {
                //     DataHelper.Save(CollectionName, t);
                //     DataHelper.Save("LogLatestLS", t);
                // }

                // uploadStatus.Status = "Success";
                // uploadStatus.Save();
                // //});

                // //var rows = DataHelper.Populate(CollectionName);
                // //foreach (var y in rows)
                // //{
                // //    DateTime sd = new DateTime();
                // //    sd = DateTime.ParseExact(BsonHelper.GetString(y, "Start_Date"), "yyyy-MM-dd hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                // //    DateTime ed = new DateTime();
                // //    ed = DateTime.ParseExact(BsonHelper.GetString(y, "End_Date"), "yyyy-MM-dd hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                // //    y.Set("Start_Date", Tools.ToUTC(sd));
                // //    y.Set("End_Date", Tools.ToUTC(ed));
                // //    DataHelper.Save(CollectionName, y);
                // //}

                return Json(new { Data = Path.ToString(), Result = "OK", Message = "Load WellActivity Done", CollectionName = CollectionName }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message, Trace = ex.StackTrace }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult ManualLoadToWEIS(List<Dictionary<string, object>> doc, DateTime DateSequence)
        {
            try
            {
                if (doc == null) doc = new List<Dictionary<string, object>>();
                var datas = doc.Select(c => c.ToBsonDocument()).ToList();
                DateTime d = new DateTime();
                d = DateSequence;
                var CollectionName = "OP" + d.ToString("yyyyMMdd") + "V2";
                CollectionName = "Manual" + CollectionName;

                //create collection that will save upload status
                var AssumeAsPath = "Manual-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                var uploadStatus = new UploadLsStatus() { Status = "In Progress", Path = AssumeAsPath, FileName = AssumeAsPath, UserUpdate = WebTools.LoginUser.UserName, ExecuteDate = DateSequence, CollectionName = CollectionName };
                uploadStatus.Save();
                var a = UploadLsStatus.Populate<UploadLsStatus>(q: Query.EQ("Path", AssumeAsPath)) ?? new List<UploadLsStatus>();
                uploadStatus = a.OrderByDescending(x => x.LastUpdate).FirstOrDefault() ?? new UploadLsStatus();

                Task.Run(() =>
                {
                    ManualLoadToWeisTaskRun(datas, DateSequence, uploadStatus, CollectionName);
                });

                // ECIS.Core.DataSerializer.ExtractionHelper e = new ECIS.Core.DataSerializer.ExtractionHelper();

                // string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["LSUploadPath"]);
                // string Path = folder + @"\" + FileName;
                // DateTime d = new DateTime();
                // d = DateSequence;

                // var CollectionName = "OP" + d.ToString("yyyyMMdd") + "V2";
                // //Aspose.Cells.Workbook wb = new Aspose.Cells.Workbook(Path);

                // var latestsequence = this.LoadLSNew(Path);
                ////var latestsequence = this.LoadLSWithConfiguration(wb);

                // Execute(CollectionName);
                // DataHelper.Delete(CollectionName);
                // DataHelper.Delete("LogLatestLS");

                // //create collection that will save upload status
                // var uploadStatus = new UploadLsStatus() { Status = "In Progress", Path = Path, FileName = FileName };
                // uploadStatus.Save();
                // var a = UploadLsStatus.Populate<UploadLsStatus>(q: Query.EQ("Path", Path)) ?? new List<UploadLsStatus>();
                // uploadStatus = a.OrderByDescending(x => x.LastUpdate).FirstOrDefault() ?? new UploadLsStatus();

                // foreach (var t in latestsequence)
                // {
                //     DataHelper.Save(CollectionName, t);
                //     DataHelper.Save("LogLatestLS", t);
                // }

                // uploadStatus.Status = "Success";
                // uploadStatus.Save();
                // //});

                // //var rows = DataHelper.Populate(CollectionName);
                // //foreach (var y in rows)
                // //{
                // //    DateTime sd = new DateTime();
                // //    sd = DateTime.ParseExact(BsonHelper.GetString(y, "Start_Date"), "yyyy-MM-dd hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                // //    DateTime ed = new DateTime();
                // //    ed = DateTime.ParseExact(BsonHelper.GetString(y, "End_Date"), "yyyy-MM-dd hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                // //    y.Set("Start_Date", Tools.ToUTC(sd));
                // //    y.Set("End_Date", Tools.ToUTC(ed));
                // //    DataHelper.Save(CollectionName, y);
                // //}

                return Json(new { Data = AssumeAsPath.ToString(), Result = "OK", Message = "Load WellActivity Done", CollectionName = CollectionName }, JsonRequestBehavior.AllowGet);
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


        public void Execute(List<WellActivity> currentDB, string id, string user)
        {
            //try
            //{
            var y = DataHelper.Populate(id);

            #region Backup Current WellActivity to _WellActivity_" + nowDate2
            DateTime dateNow = DateTime.Now;
            dateNow = DateTime.Now;
            string nowDate2 = dateNow.ToString("yyyyMMddhhmm");

            MDBHelper.DuplicateAndSave("WEISWellActivities", "_WellActivity_" + nowDate2);


            //List<WellActivity> currentDB = new List<WellActivity>();
            //currentDB = DataHelper.Populate<WellActivity>("WEISWellActivities");

            //List<BsonDocument> bdocs = new List<BsonDocument>();
            //foreach (var data in currentDB)
            //{
            //    bdocs.Add(data.ToBsonDocument());
            //}

            //DataHelper.Save(, bdocs);

            //DataHelper.Delete("BackupListWellActivity", Query.EQ("CollectionName", "_WellActivity_" + nowDate2));
            //DataHelper.Save("BackupListWellActivity",
            //    new BsonDocument()
            //    .Set("CollectionName", "_WellActivity_" + nowDate2)
            //    .Set("User", user)
            //    .Set("Date", DateTime.Now)
            //    );
            #endregion

            #region do logic
            WellActivity.LoadLastSequence(currentDB, id, user);
            #endregion

            // recalculate calculated OP
            //WellActivity.ProcessCalculatedOP();

            AddLatestUploadedLSinBizPlan();
            //BizPlan.RefreshBizplanWithLatestUploadedLS(id);
            //return Json(new { Data = "", Result = "OK", Message = "Load WellActivity Done" }, JsonRequestBehavior.AllowGet);
            //}
            //catch (Exception ex)
            //{
            //    //return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message, Trace = ex.StackTrace }, JsonRequestBehavior.AllowGet);
            //}
        }

        public List<LatestLSUploaded> GetLatestLSPhases(DateTime latestlsdate)
        {
            List<LatestLSUploaded> result = new List<LatestLSUploaded>();
            var logLast = DataHelper.Populate("LogLatestLS", Query.And(
              Query.EQ("Executed_At", Tools.ToUTC(latestlsdate))));

            foreach (var t in logLast)
            {
                DateTime start = new DateTime();
                DateTime finish = new DateTime();
                try
                {
                    start = t.GetDateTime("Start_Date");
                        //DateTime.ParseExact(t.GetString("Start_Date"), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (Exception ec)
                {
                    start = Tools.DefaultDate;
                }
                try
                {
                    finish = t.GetDateTime("End_Date");
                        //DateTime.ParseExact(t.GetString("End_Date"), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (Exception ec)
                {
                    finish = Tools.DefaultDate;
                }
                result.Add(new LatestLSUploaded
                {
                    Activity_Type = t.GetString("Activity_Type"),
                    Rig_Name = t.GetString("Rig_Name"),
                    Well_Name = t.GetString("Well_Name"),
                    LS = new DateRange(start, finish)
                });
            }
            return result;
        }

        public DateTime GetLastDateUploadedLS()
        {
            var latest1 = DataHelper.Get("LogLatestLS", null, SortBy.Descending("Executed_At"));

            DateTime dd = latest1.GetDateTime("Executed_At");

            return dd;
        }
        public void AddLatestUploadedLSinBizPlan()
        {
            var lastDateExecutedLS = GetLastDateUploadedLS();
            var latestPhases = GetLatestLSPhases(lastDateExecutedLS);

            var aa = BizPlanActivity.Populate<BizPlanActivity>();
            foreach (var a in aa)
            {
                if (a.WellName.Equals("DAWN MARIE APPR"))
                {

                }

                foreach (var ph in a.Phases)
                {

                    var ttt = latestPhases.Where(x => x.Well_Name.Equals(a.WellName) && x.Activity_Type.Equals(ph.ActivityType) && x.Rig_Name.Equals(ph.Estimate.RigName));

                    if (ttt.Count() > 0)
                    {
                        ph.isLatestLS = true;
                        ph.LatestLSDate = lastDateExecutedLS;

                        // 
                        ph.PhSchedule = ttt.FirstOrDefault().LS;
                    }
                    else
                    {
                        ph.isLatestLS = false;
                        ph.LatestLSDate = Tools.DefaultDate;
                    }
                    if (ph.Estimate.Status == null)
                    {
                        ph.Estimate.Status = "Draft";
                    }
                    if (ph.Estimate.Status.Equals(""))
                        ph.Estimate.Status = "Draft";

                    if (ph.Estimate.Status == "Draft" && (String.IsNullOrEmpty(a.PerformanceUnit) || String.IsNullOrEmpty(a.AssetName) || String.IsNullOrEmpty(a.Region) || String.IsNullOrEmpty(a.ProjectName) || String.IsNullOrEmpty(a.Country) || String.IsNullOrEmpty(a.ReferenceFactorModel)))
                    {
                        ph.Estimate.Status = "Meta Data Missing";
                    }

                }

                if (a.Phases != null && a.Phases.Count() > 0)
                {

                    a.OpsSchedule.Start = a.Phases.Min(x => x.PhSchedule.Start);
                    a.OpsSchedule.Finish = a.Phases.Max(x => x.PhSchedule.Finish);
                }


                DataHelper.Save("WEISBizPlanActivities", a.ToBsonDocument());
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
            return MvcResultInfo.Execute(null, document =>
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

                return data.Select(x => x.ToDictionary());
            }); 

            //try
            //{
            //    var data = DataHelper.Populate(collName);

            //    foreach (var t in data)
            //    {
            //        var st = BsonHelper.Get(t, "Year_-_Start");
            //        var nd = BsonHelper.Get(t, "Year_-_End");
            //        t.Remove("Year_-_Start");
            //        t.Remove("Year_-_End");

            //        t.Set("YearStart", st);
            //        t.Set("YearEnd", nd);
            //    }

            //    return Json(new { Data = DataHelper.ToDictionaryArray(data), Result = "OK", Message = "GetLatestUploadedLS Success" }, JsonRequestBehavior.AllowGet);
            //}
            //catch (Exception ex)
            //{
            //    return Json(new { Data = "", Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);

            //}

        }

        public JsonResult DownloadAllLatestLS(int offsetClient = 0)
        {
            //follow timezone client when download pre LS
            var localTz = (int)TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalHours;
            var addTime = (localTz - offsetClient) * -1;
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "PreLSTemplate.xlsx");

            var fetchData = new List<BsonDocument>();//
                fetchData = DataHelper.Populate("LogLatestLS");

            Workbook wb = new Workbook(xlst);
            wb.Worksheets.Add();
            wb.Worksheets[0].Name = "Pre LS";
            int AutoRow = 1;
            fetchData = fetchData.OrderBy(x => x.GetDateTime("Start_Date")).ToList();
            foreach (var row in fetchData)
            {
                wb.Worksheets[0].Cells[AutoRow, 0].Value = row.GetString("Rig_Type");
                wb.Worksheets[0].Cells[AutoRow, 1].Value = row.GetString("Rig_Name");
                wb.Worksheets[0].Cells[AutoRow, 2].Value = row.GetString("Well_Name");
                wb.Worksheets[0].Cells[AutoRow, 3].Value = row.GetString("Activity_Type");
                wb.Worksheets[0].Cells[AutoRow, 4].Value = row.GetString("Activity_Description");
                wb.Worksheets[0].Cells[AutoRow, 5].Value = row.GetDateTime("Start_Date");
                wb.Worksheets[0].Cells[AutoRow, 6].Value = row.GetDateTime("End_Date");
                wb.Worksheets[0].Cells[AutoRow, 7].Value = row.GetString("Year_-_Start");
                wb.Worksheets[0].Cells[AutoRow, 8].Value = row.GetString("Year_-_End");
                
                var execFrom = "Full Replacement";
                var execFrom_ = row.GetString("Executed_From");
                if (execFrom_ == "Single")
                    execFrom = "Single Rig Replacement";
                else if (execFrom_ == "Manual")
                    execFrom = "Activity Edit";

                wb.Worksheets[0].Cells[AutoRow, 9].Value = execFrom;
                
                if (!String.IsNullOrEmpty(row.GetString("Last_Edited_By")))
                    wb.Worksheets[0].Cells[AutoRow, 10].Value = row.GetString("Last_Edited_By");
                else
                    wb.Worksheets[0].Cells[AutoRow, 10].Value = row.GetString("Executed_By");

                if (row.GetDateTime("Last_Edited_Date") == Tools.DefaultDate)
                    wb.Worksheets[0].Cells[AutoRow, 11].Value = row.GetDateTime("Executed_At").AddHours(addTime);
                else
                    wb.Worksheets[0].Cells[AutoRow, 11].Value = row.GetDateTime("Last_Edited_Date").AddHours(addTime);
                //
                AutoRow++;
            }
            wb.Worksheets[0].AutoFitColumns();
            string returnName = String.Format("Current LS-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss"));

            var newFileNameSingle = Path.Combine(Server.MapPath("~/App_Data/Temp"), returnName);

            wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            return Json(new { Success = true, Path = returnName }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult DownloadPreLS(string rigName = null, int offsetClient = 0)
        {
            //follow timezone client when download pre LS
            var localTz = (int)TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalHours;
            var addTime = (localTz - offsetClient) * -1;

            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "PreLSTemplate.xlsx");

            var fetchData = new List<BsonDocument>();//
            if (!String.IsNullOrEmpty(rigName))
                fetchData = DataHelper.Populate("LogLatestLS", Query.EQ("Rig_Name", rigName));
            else
                fetchData = DataHelper.Populate("LogLatestLS");

            Workbook wb = new Workbook(xlst);
            wb.Worksheets.Add();
            wb.Worksheets[0].Name = "Pre LS";
            int AutoRow = 1;
            var dataDistinct = fetchData.Select(x => new
            {
                Well_Name = x.GetString("Well_Name"),
                Rig_Name = x.GetString("Rig_Name"),
                Rig_Type = x.GetString("Rig_Type"),
                ActivityType = fetchData.Where(c => c.GetString("Rig_Name") == x.GetString("Rig_Name") && c.GetString("Rig_Type") == x.GetString("Rig_Type")).Select(c => c.GetString("Activity_Type")).Distinct().ToList()//x.GetString("Activity_Type")
            }).GroupBy(x => new { x.Well_Name, x.Rig_Name, x.Rig_Type }).Select(x => x.First()).ToList();
            //dataDistinct = dataDistinct.Distinct().ToList();

            var listWellActivity = new List<WellActivity>();

            foreach (var xx in dataDistinct)
            {
                var qs = new List<IMongoQuery>();
                qs.Add(Query.EQ("WellName", xx.Well_Name));
                qs.Add(Query.EQ("RigName", xx.Rig_Name));
                qs.Add(Query.EQ("RigType", xx.Rig_Type));
                //var actTp = fetchData.Where(x => )
                //qs.Add(Query.EQ("Phases.ActivityType", xx.ActivityType));

                var wa = WellActivity.Get<WellActivity>(Query.And(qs));
                if (wa != null)
                {
                    var phasesMatch = wa.Phases.Where(x => xx.ActivityType.Contains(x.ActivityType)).ToList();
                    wa.Phases = phasesMatch;
                    listWellActivity.Add(wa);
                }
            }

            var yy = listWellActivity.SelectMany(x => x.Phases).ToList();

            foreach (var ccc in listWellActivity)
            {
                ccc.Phases = ccc.Phases.OrderBy(x => x.PhaseNo).ToList();
                var stt = Tools.DefaultDate;//new DateTime();
                var sprev = Tools.DefaultDate;
                foreach (var xx in ccc.Phases)
                {
                    
                    wb.Worksheets[0].Cells[AutoRow, 0].Value = ccc.RigType;//row.GetString("Rig_Type");
                    wb.Worksheets[0].Cells[AutoRow, 1].Value = ccc.RigName;//row.GetString("Rig_Name");
                    wb.Worksheets[0].Cells[AutoRow, 2].Value = ccc.WellName;//row.GetString("Well_Name");
                    wb.Worksheets[0].Cells[AutoRow, 3].Value = xx.ActivityType;//row.GetString("Activity_Type");
                    wb.Worksheets[0].Cells[AutoRow, 4].Value = xx.ActivityDesc;//row.GetString("Activity_Description");
                    //wb.Worksheets[0].Cells[AutoRow, 5].Value = xx.PhSchedule.Start;//.ToString("yyyy-MM-dd");//row.GetDateTime("Start_Date").ToString("yyyy-MM-dd");
                    //wb.Worksheets[0].Cells[AutoRow, 6].Value = xx.PhSchedule.Finish;//.ToString("yyyy-MM-dd");//row.GetDateTime("Start_End").ToString("yyyy-MM-dd");
                    //wb.Worksheets[0].Cells[AutoRow, 7].Value = xx.LE.Days;//.ToString("yyyy-MM-dd");

                    if (stt == Tools.DefaultDate)
                    {
                        wb.Worksheets[0].Cells[AutoRow, 5].Value = xx.PhSchedule.Start;//.ToString("yyyy-MM-dd");//row.GetDateTime("Start_Date").ToString("yyyy-MM-dd");
                        wb.Worksheets[0].Cells[AutoRow, 6].Value = xx.PhSchedule.Start.AddDays(xx.LE.Days);//.ToString("yyyy-MM-dd");
                        wb.Worksheets[0].Cells[AutoRow, 7].Value = xx.PhSchedule.Start.ToString("yyyy");//.ToString("yyyy-MM-dd");//row.GetDateTime("Start_Date").ToString("yyyy-MM-dd");
                        wb.Worksheets[0].Cells[AutoRow, 8].Value = xx.PhSchedule.Start.AddDays(xx.LE.Days).ToString("yyyy");//.ToString("yyyy-MM-dd");
                        stt = xx.PhSchedule.Start;
                        sprev = xx.PhSchedule.Start.AddDays(xx.LE.Days);
                    }
                    else
                    {
                        //stt = xx.PhSchedule.Start.AddDays(xx.LE.Days);
                        wb.Worksheets[0].Cells[AutoRow, 5].Value = sprev;//.ToString("yyyy-MM-dd");//row.GetDateTime("Start_Date").ToString("yyyy-MM-dd");
                        wb.Worksheets[0].Cells[AutoRow, 6].Value = sprev.AddDays(xx.LE.Days);//.ToString("yyyy-MM-dd");
                        wb.Worksheets[0].Cells[AutoRow, 7].Value = sprev.ToString("yyyy");//.ToString("yyyy-MM-dd");//row.GetDateTime("Start_Date").ToString("yyyy-MM-dd");
                        wb.Worksheets[0].Cells[AutoRow, 8].Value = sprev.AddDays(xx.LE.Days).ToString("yyyy");//.ToString("yyyy-MM-dd");
                        //stt = sprev.AddDays(xx.LE.Days);//xx.PhSchedule.Start;
                        sprev = sprev.AddDays(xx.LE.Days);
                    }


                    //find where the activity loaded from
                    var findData = fetchData.FirstOrDefault(x =>
                        x.GetString("Rig_Type") == ccc.RigType &&
                        x.GetString("Rig_Name") == ccc.RigName &&
                        x.GetString("Well_Name") == ccc.WellName &&
                        x.GetString("Activity_Type") == xx.ActivityType
                        );
                    if (findData != null)
                    {
                        var execFrom = "Full Replacement";
                        var execFrom_ = findData.GetString("Executed_From");
                        if (execFrom_ == "Single")
                            execFrom = "Single Rig Replacement";
                        else if (execFrom_ == "Manual")
                            execFrom = "Activity Edit";

                        wb.Worksheets[0].Cells[AutoRow, 9].Value = execFrom;
                        wb.Worksheets[0].Cells[AutoRow, 10].Value = findData.GetString("Executed_By");
                        wb.Worksheets[0].Cells[AutoRow, 11].Value = findData.GetDateTime("Executed_At").AddHours(addTime);

                        if (!String.IsNullOrEmpty(findData.GetString("Last_Edited_By")))
                            wb.Worksheets[0].Cells[AutoRow, 10].Value = findData.GetString("Last_Edited_By");

                        if (findData.GetDateTime("Last_Edited_Date") != Tools.DefaultDate)
                            wb.Worksheets[0].Cells[AutoRow, 11].Value = findData.GetDateTime("Last_Edited_Date").AddHours(addTime);
                    }
                    else
                    {
                        wb.Worksheets[0].Cells[AutoRow, 9].Value = "Not in current LS";
                    }
                    //compare actv
                    //var qs = new List<IMongoQuery>();
                    //qs.Add(Query.EQ("WellName", row.GetString("Rig_Name")));
                    //qs.Add(Query.EQ("RigName", row.GetString("Rig_Name")));
                    //qs.Add(Query.EQ("RigType", row.GetString("Rig_Type")));
                    //qs.Add(Query.EQ("Phases.ActivityType", row.GetString("Activity_Type")));

                    //var wa = WellActivity.Get<WellActivity>(Query.And(qs));
                    //if (wa == null)
                    //{
                    //    wb.Worksheets[0].Cells[AutoRow, 6].Value = row.GetDateTime("Start_End").ToString("yyyy-MM-dd");
                    //    wb.Worksheets[0].Cells[AutoRow, 7].Value = row.GetString("Year_-_Start");
                    //    //wb.Worksheets[0].Cells[AutoRow, 7].Value = row.GetString("Year_-_Start");
                    //    wb.Worksheets[0].Cells[AutoRow, 8].Value = row.GetString("Year_-_End");
                    //}
                    //else
                    //{
                    //    WellActivityPhase matchData = wa.Phases.Where(x => x.ActivityType.Equals(row.GetString("Activity_Type"))).FirstOrDefault();
                    //    var calEndData = row.GetString("End_Date") + matchData.LE.Days;
                    //    wb.Worksheets[0].Cells[AutoRow, 6].Value = row.GetDateTime("Start_End").ToString("yyyy-MM-dd");
                    //    wb.Worksheets[0].Cells[AutoRow, 7].Value = row.GetString("Year_-_Start");
                    //    wb.Worksheets[0].Cells[AutoRow, 8].Value = calEndData;
                    //}
                    AutoRow++;
                }
            }

            //foreach(var row in fetchData){
            //    wb.Worksheets[0].Cells[AutoRow, 0].Value = row.GetString("Rig_Type");
            //    wb.Worksheets[0].Cells[AutoRow, 1].Value = row.GetString("Rig_Name");
            //    wb.Worksheets[0].Cells[AutoRow, 2].Value = row.GetString("Well_Name");
            //    wb.Worksheets[0].Cells[AutoRow, 3].Value = row.GetString("Activity_Type");
            //    wb.Worksheets[0].Cells[AutoRow, 4].Value = row.GetString("Activity_Description");
            //    wb.Worksheets[0].Cells[AutoRow, 5].Value = row.GetDateTime("Start_Date").ToString("yyyy-MM-dd");


            //    //compare actv
            //    var qs = new List<IMongoQuery>();
            //    qs.Add(Query.EQ("WellName",row.GetString("Rig_Name")));
            //    qs.Add(Query.EQ("RigName", row.GetString("Rig_Name")));
            //    qs.Add(Query.EQ("RigType", row.GetString("Rig_Type")));
            //    qs.Add(Query.EQ("Phases.ActivityType", row.GetString("Activity_Type")));

            //    var wa = WellActivity.Get<WellActivity>(Query.And(qs));
            //    if (wa == null)
            //    {
            //        wb.Worksheets[0].Cells[AutoRow, 6].Value = row.GetDateTime("Start_End").ToString("yyyy-MM-dd");
            //        wb.Worksheets[0].Cells[AutoRow, 7].Value = row.GetString("Year_-_Start");
            //        //wb.Worksheets[0].Cells[AutoRow, 7].Value = row.GetString("Year_-_Start");
            //        wb.Worksheets[0].Cells[AutoRow, 8].Value = row.GetString("Year_-_End");
            //    }
            //    else
            //    {
            //        WellActivityPhase matchData = wa.Phases.Where(x => x.ActivityType.Equals(row.GetString("Activity_Type"))).FirstOrDefault();
            //        var calEndData = row.GetString("End_Date") + matchData.LE.Days;
            //        wb.Worksheets[0].Cells[AutoRow, 6].Value = row.GetDateTime("Start_End").ToString("yyyy-MM-dd");
            //        wb.Worksheets[0].Cells[AutoRow, 7].Value = row.GetString("Year_-_Start");
            //        wb.Worksheets[0].Cells[AutoRow, 8].Value = calEndData;
            //    }
            //    AutoRow++;
            //}
            wb.Worksheets[0].AutoFitColumns();
            string returnName = String.Format("PreLS-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss"));

            var newFileNameSingle = Path.Combine(Server.MapPath("~/App_Data/Temp"), returnName);

            wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            return Json(new { Success = true, Path = returnName }, JsonRequestBehavior.AllowGet);
        }

        public FileResult GenerateFilePreLS(string stringName, DateTime date, string rigName = null)
        {
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "PreLSTemplate.xlsx");
            var res = Path.Combine(Server.MapPath("~/App_Data/Temp"), stringName);
            //rename file
            var dt = Tools.ToUTC(date).ToString("yyyy-MM-dd HHmmss");
            var retstringName = "Pre LS - Full Replacement - " + dt + ".xlsx";
            if (!String.IsNullOrEmpty(rigName))
                retstringName = "Pre LS - " + rigName + " - " + dt + ".xlsx";

            return File(res, Tools.GetContentType(".xlsx"), retstringName);
        }

        public FileResult DownloadFile(string stringName, DateTime date)
        {
            var res = Path.Combine(Server.MapPath("~/App_Data/Temp"), stringName);
            //rename file
            var dt = Tools.ToUTC(date).ToString("yyyy-MM-dd");
            var retstringName = "Current LS - " + dt + ".xlsx";

            return File(res, Tools.GetContentType(".xlsx"), retstringName);
        }
    }
}

[BsonIgnoreExtraElements]
public class UploadLsStatus : ECISModel
{
    public override string TableName
    {
        get { return "UploadLsStatus"; }
    }
    public string Message { get; set; }
    public string Status { get; set; }
    public string FileName { get; set; }
    public string Path { get; set; }

    public DateTime ExecuteDate { get; set; }
    public string UserUpdate { get; set; }
    public string CollectionName { get; set; }


}

[BsonIgnoreExtraElements]
public class LatestLSDifferences : ECISModel
{
    public override string TableName
    {
        get { return "LatestLSDifferences"; }
    }
    public string WellName { get; set; }
    public string RigName { get; set; }
    public string RigType { get; set; }
    public string ActivityType { get; set; }
    public string SequenceId { get; set; }
    public List<string> BaseOPs { get; set; }

    public DateRange PrevLSSchedule { get; set; }
    public DateRange LSSchedule { get; set; }
    public double PrevLSDuration { get; set; }
    public double LSDuration { get; set; }
    public bool isDifferent { get; set; }
    public double DifferentValue { get; set; }
    public string DifferentType { get; set; }

    public LatestLSDifferences()
    {
        BaseOPs = new List<string>();
    }
}

