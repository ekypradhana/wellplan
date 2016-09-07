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
using MongoDB.Bson.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class MessageStatus
    {
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class PIPUploadError
    {

        public string filename { get; set; }
        public string fullpath { get; set; }
        public string filenameoriginal { get; set; }

        public string WellName { get; set; }
        public string RigName { get; set; }
        public string ActivityType { get; set; }
        public string AssignToOP { get; set; }
        public string PIPType { get; set; }
        public string Idea { get; set; }
        public DateTime ActivityStart { get; set; }
        public DateTime ActivityEnd { get; set; }
        public string Completion { get; set; }
        public string Classification { get; set; }
        public List<MessageStatus> Messages { get; set; }

        public PIPUploadError()
        {
            Messages = new List<MessageStatus>();
        }
    }

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

                //DataHelper.Delete("PIPUploadResult");

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
                CreateUserUpload(fileNameReplace);
                //open the file, and then check if there any classification that other than 4 listed in database.
                List<PIPUploadError> errs = new List<PIPUploadError>();
                ExtractionHelper e = new ExtractionHelper();
                IList<BsonDocument> datas = e.Extract(filepath);

                datas = datas.Where(x =>
                    !String.IsNullOrEmpty(x.GetString("Well_Name").Trim()) &&
                    !String.IsNullOrEmpty(x.GetString("Rig_Name").Trim()) &&
                    !String.IsNullOrEmpty(x.GetString("Activity_Type").Trim())
                    ).ToList();
                var isError = false;

                var classifications = WellPIPClassifications.Populate<WellPIPClassifications>() ?? new List<WellPIPClassifications>();


                var errorMessage = new List<string>();
                var MasterActivity = ActivityMaster.Populate<ActivityMaster>();
                var counter = 1;


                if (datas.Any())
                {
                    if (datas.Any())
                    {
                        var listSt = new List<string>();
                        var errorMsg = "";

                        var ff = datas.Where(x =>
                            !x.GetString("Rig_Name").Equals(""));
                        foreach (var t in datas)
                        {
                            PIPUploadError error = new PIPUploadError();
                            counter++;
                            #region Identify

                            var wellName = t.GetString("Well_Name").Trim();
                            var rigName = t.GetString("Rig_Name").Trim();
                            var activity = t.GetString("Activity_Type").Trim();
                            var ops = t.GetString("Assign_To_OP");
                            var piptype = t.GetString("PIP_Type");
                            var idea = t.GetString("Idea");
                            var start = t.GetString("Activity_Start");
                            var finish = t.GetString("Activity_End");
                            var year = t.GetString("Year");
                            //var complete = t.GetString("%_Complete");
                            var complete = t.GetString("Realized/Not_Realized") == "" ? t.GetString("%_Complete") : t.GetString("Realized/Not_Realized");
                            var classification = t.GetString("High_Level_Classification");
                            var theme = t.GetString("Theme");

                            var oppdays = t.GetDouble("Opportunity__Days_(-)");
                            var riskdays = t.GetDouble("Risk__Days_(+)");
                            var leoppdays = t.GetDouble("LE_Opportunity_Days_(-)");
                            var leriskdays = t.GetDouble("LE_Risk_Days_(+)");

                            var oppcost = t.GetDouble("Opportunity_Cost_MM__(-)");
                            var riskcost = t.GetDouble("Risk_Cost_MM_(+)");
                            var leoppcost = t.GetDouble("LE_Opportunity_Cost_MM_(-)");
                            var leriskcost = t.GetDouble("LE_Risk_Cost_MM_(+)");

                            var actionparty = t.GetString("Action_Party");
                            var performanceuinit = t.GetString("Performance_Unit");
                            var assetname = t.GetString("Asset_Name");
                            var projectname = t.GetString("Project_Name");

                            error.WellName = wellName;
                            error.RigName = rigName;

                            try
                            {
                                error.ActivityStart = DateTime.ParseExact(start, "M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            catch(Exception ex)
                            {

                                try
                                {
                                    error.ActivityStart = DateTime.ParseExact(start, "yyyy-M-d HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                                }
                                catch(Exception ec)
                                {
                                    error.Messages.Add(new MessageStatus { Message = ex.Message, Status = "Error" });
                                }
                            }

                            try
                            {
                                error.ActivityEnd = DateTime.ParseExact(finish, "M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    error.ActivityEnd = DateTime.ParseExact(finish, "yyyy-M-d HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                                }
                                catch (Exception ec)
                                {
                                    error.Messages.Add(new MessageStatus { Message = ex.Message, Status = "Error" });
                                }
                            }
                            
                            error.ActivityType = activity;
                            error.AssignToOP = ops;
                            error.Classification = classification;
                            error.Completion = complete;
                            error.Idea = idea;
                            error.PIPType = piptype;
                            error.filename = fileNameReplace;
                            error.fullpath = filepath;
                            error.filenameoriginal = fileName;
                            #endregion

                            //check empty field
                            if (String.IsNullOrEmpty(wellName))
                            {
                                isError = true;
                                error.Messages.Add(new MessageStatus { Message = "Well Name should be filled - Rows "+counter, Status = "Error" });
                            }

                            if (String.IsNullOrEmpty(rigName))
                            {
                                isError = true;
                                error.Messages.Add(new MessageStatus { Message = "Rig Name should be filled - Rows " + counter, Status = "Error" });
                            }

                            //if (String.IsNullOrEmpty(activity) )
                            //{
                            //    isError = true;
                            //    error.Messages.Add(new MessageStatus { Message = "Activity Type should be filled - Rows " + counter, Status = "Error" });
                            //}

                            if (String.IsNullOrEmpty(ops))
                            {
                                isError = true;
                                error.Messages.Add(new MessageStatus { Message = "Assign To OP should be filled - Rows " + counter, Status = "Error" });
                            }

                            if (String.IsNullOrEmpty(piptype) )
                            {
                                isError = true;
                                error.Messages.Add(new MessageStatus { Message = "PIP Type should be filled - Rows " + counter, Status = "Error" });
                            }

                            if (String.IsNullOrEmpty(idea))
                            {
                                isError = true;
                                error.Messages.Add(new MessageStatus { Message = "Idea should be filled - Rows " + counter, Status = "Error" });
                            }

                            if (String.IsNullOrEmpty(complete))
                            {
                                isError = true;
                                error.Messages.Add(new MessageStatus { Message = "Realized/Not Realized should be filled - Rows " + counter, Status = "Error" });
                            }

                            if (String.IsNullOrEmpty(classification))
                            {
                                isError = true;
                                error.Messages.Add(new MessageStatus { Message = "Classification should be filled - Rows " + counter, Status = "Error" });
                            }

                            if (String.IsNullOrEmpty(theme))
                            {
                                isError = true;
                                error.Messages.Add(new MessageStatus { Message = "Theme should be filled - Rows " + counter, Status = "Error" });
                            }

                            //if (String.IsNullOrEmpty(actionparty))
                            //{
                            //    isError = true;
                            //    error.Messages.Add(new MessageStatus { Message = "Action Party should be filled - Rows " + counter, Status = "Error" });
                            //}

                            if (String.IsNullOrEmpty(start))
                            {
                                isError = true;
                                error.Messages.Add(new MessageStatus { Message = "Start Date should be filled - Rows " + counter, Status = "Error" });
                            }

                            if (String.IsNullOrEmpty(finish))
                            {
                                isError = true;
                                error.Messages.Add(new MessageStatus { Message = "End Date should be filled - Rows " + counter, Status = "Error" });
                            }



                            // cr elements
                            if (!piptype.Replace(" ", "").Equals("") &&
                                    (
                                        piptype.Replace(" ", "").ToLower().Equals("cr") ||
                                        piptype.Replace(" ", "").ToLower().Equals("rig") ||
                                        wellName.Replace(" ", "").Equals("")
                                    )
                                )
                            {
                                //if (!wellName.Trim().Replace(" ", "").Equals(""))
                                //    error.Messages.Add(new MessageStatus { Message = "The Well name value will be left empty for Rig PIP", Status = "Error" });
                                if ((oppdays + riskdays + leoppdays + leriskdays) != 0)
                                {
                                    isError = true;
                                    error.Messages.Add(new MessageStatus { Message = "RIG PIPs can only have cost information", Status = "Error" });
                                }
                                var checkWARig = WellActivity.Get<WellActivity>(Query.EQ("RigName", rigName));
                                if (checkWARig == null)
                                {
                                    isError = true;
                                    error.Messages.Add(new MessageStatus { Message = "Cant find RigName :" + rigName, Status = "Error" });
                                }

                            }

                            //Check new activity
                            if (!activity.Replace(" ", "").Equals(""))
                            {
                                var CheckAct = MasterActivity.FirstOrDefault(x => x._id.ToString().Equals(activity));
                                if (CheckAct == null)
                                {
                                    isError = true;
                                    error.Messages.Add(new MessageStatus { Message = "Activity does not exist on this well - Rows " + counter, Status = "Error" });
                                }
                            }

                            //Check New Well Name
                            if (!wellName.Replace(" ", "").Equals(""))
                            {
                                var checkWell = DataHelper.Populate("WEISWellNames", Query.EQ("_id", wellName));
                                if (checkWell == null || !checkWell.Any())
                                {
                                    if (checkWell.Count == 0)
                                    {
                                        isError = true;
                                        error.Messages.Add(new MessageStatus { Message = "Well Name does not exist in system - Row " + counter, Status = "Error" });
                                    }

                                }
                            }

                            //Check Rig Name
                            if (!rigName.Replace(" ", "").Equals(""))
                            {
                                var queryCheckRig = new List<IMongoQuery>();
                                queryCheckRig.Add(Query.EQ("WellName", wellName));
                                queryCheckRig.Add(Query.EQ("RigName", rigName));
                                var checkRig = WellActivity.Get<WellActivity>(Query.And(queryCheckRig));
                                if (checkRig == null)
                                {
                                    isError = true;
                                    error.Messages.Add(new MessageStatus { Message = "Well Name does not exist on this rig - Row "+counter, Status = "Error" });
                                }
                            }

                            //Check for asset name,project name and performance unit in well plan
                            //if((assetname != null && !assetname.Replace(" ","").Equals("")) ||
                            //    (projectname != null && !projectname.Replace(" ", "").Equals("")) ||
                            //    (performanceuinit != null && !performanceuinit.Replace(" ", "").Equals("")))
                            //{
                            //    var queryCheck = new List<IMongoQuery>();
                            //    queryCheck.Add(Query.EQ("AssetName", assetname));
                            //    queryCheck.Add(Query.EQ("ProjectName", projectname));
                            //    queryCheck.Add(Query.EQ("PerformanceUnit", performanceuinit));
                            //    var check = WellActivity.Get<WellActivity>(Query.And(queryCheck));
                            //    if (check == null)
                            //    {
                            //        isError = true;
                            //        error.Messages.Add(new MessageStatus { Message = "Can't find well plan with Asset Name : " + assetname + ", Project Name : " + projectname + " and Performance Unit : " + performanceuinit });
                            //    }
                            //}

                            ////Check Asset Name
                            //if (!assetname.Replace(" ", "").Equals(""))
                            //{
                            //    var queryCheckAss = new List<IMongoQuery>();
                            //    queryCheckAss.Add(Query.EQ("WellName", wellName));
                            //    queryCheckAss.Add(Query.EQ("RigName", rigName));
                            //    queryCheckAss.Add(Query.EQ("AssetName", assetname));
                            //    queryCheckAss.Add(Query.EQ("Phases.ActivityType", assetname));
                            //    var checkAss = WellActivity.Get<WellActivity>(Query.And(queryCheckAss));
                            //    if (checkAss == null)
                            //    {
                            //        isError = true;
                            //        error.Messages.Add(new MessageStatus { Message = "Asset Name does not exist on this Well - Row " + counter, Status = "Error" });
                            //    }
                            //}

                            ////Check Project Name
                            //if (!projectname.Replace(" ", "").Equals(""))
                            //{
                            //    var queryCheckAss = new List<IMongoQuery>();
                            //    queryCheckAss.Add(Query.EQ("WellName", wellName));
                            //    queryCheckAss.Add(Query.EQ("RigName", rigName));
                            //    queryCheckAss.Add(Query.EQ("AssetName", assetname));
                            //    queryCheckAss.Add(Query.EQ("Phases.ActivityType", activity));
                            //    var checkAss = WellActivity.Get<WellActivity>(Query.And(queryCheckAss));
                            //    if (checkAss == null)
                            //    {
                            //        isError = true;
                            //        error.Messages.Add(new MessageStatus { Message = "Project Name does not exist on this Well - Row " + counter, Status = "Error" });
                            //    }
                            //}

                           

                            if (!piptype.Replace(" ", "").Equals("") &&
                                    (
                                        piptype.Replace(" ", "").ToLower().Equals("ce") ||
                                        piptype.Replace(" ", "").ToLower().Equals("well") 
                                    )
                                )
                            {
                                //if (oppdays == 0 && riskdays == 0 && leoppdays ==0 && leriskdays == 0)
                                //    error.Messages.Add(new MessageStatus { Message = "WEIS Indicate that this Well PIP only have cost information value (all days value is zero)",  Status = "Error" });


                                var queryCheckWA = new List<IMongoQuery>();
                                queryCheckWA.Add(Query.EQ("WellName", wellName));
                                queryCheckWA.Add(Query.EQ("RigName", rigName));
                                queryCheckWA.Add(Query.EQ("Phases.ActivityType", activity));
                                var checkWA = WellActivity.Get<WellActivity>(Query.And(queryCheckWA));
                                if (checkWA == null)
                                {
                                    isError = true;
                                    error.Messages.Add(new MessageStatus { Message = "Cant find WellPlan with wellname : " + wellName + ", rigname : " + rigName + ", activity type:" + activity + " - Rows "+counter, Status = "Error" });
                                }

                            }
                            

                            //Check new project
                            if (!projectname.Replace(" ", "").Equals(""))
                            {
                                var CheckPro = DataHelper.Get<MasterProject>("WEISProjectNames", Query.EQ("_id", projectname));
                                if (CheckPro == null)
                                {
                                    isError = true;
                                    error.Messages.Add(new MessageStatus { Message = "Project Name does not exist in system : " + projectname, Status = "Error" });
                                }
                            }

                            if (!performanceuinit.Replace(" ", "").Equals(""))
                            {
                                var CheckPerform = DataHelper.Populate("WEISPerformanceUnits", Query.EQ("_id", performanceuinit));
                                if (CheckPerform == null || !CheckPerform.Any())
                                {
                                    isError = true;
                                    error.Messages.Add(new MessageStatus { Message = "Performance Unit does not exist in system : " + performanceuinit, Status = "Error" });
                                }
                            }

                            if (classifications.Any())
                            {
                                listSt = classifications.Select(x => x.Name.ToLower()).ToList();
                                var ckg = datas.Where(x => classification != "" && !listSt.Contains(classification.ToLower().Trim()));
                                if (ckg.Any())
                                {
                                    isError = true;
                                    error.Messages.Add(new MessageStatus { Message = "Classification name doesn't recognize as in WEIS Classification Masters", Status = "Error" });
                                }
                            }
                           


                            errs.Add(error);
                        }

                        //foreach (var dt in datas)
                        //{
                        //    #region Check Activity Type
                        //    counter++;
                        //    var activity = dt.GetString("Activity_Type").Trim();
                        //    if (activity != "")
                        //    {
                        //        var CheckAct = MasterActivity.FirstOrDefault(x => x._id.ToString().ToLower().Equals(activity.ToLower()));
                        //        if (CheckAct == null)
                        //        {
                        //            errorMessage.Add("- Row " + counter + " Activity Type '" + activity + "' is not in the system.");
                        //        }
                        //    }
                                
                        //    #endregion

                        //}
                    }
                }

                if (isError)
                {
                    System.IO.File.Delete(filepath);
                }

                int i = 1;

                foreach (var t in errs)
                {
                    BsonDocument doc = new BsonDocument();
                    doc = t.ToBsonDocument();
                    doc.Set("_id", fileNameReplace + "-" + i.ToString());
                    DataHelper.Save("PIPUploadResult", doc);
                    i++;
                }


                if (errs.SelectMany(x => x.Messages).Any())
                {
                    var yy = errs.SelectMany(x => x.Messages).Select(x => x.Status).Any();
                    if (yy)
                    {
                        if (errs.SelectMany(x => x.Messages).Select(x => x.Status).ToList().Contains("Error"))
                        {
                            System.IO.File.Delete(filepath);
                            ri.Data = fileNameReplace;
                            throw new Exception("The PIP upload was stopped because the file contains errors.");//Some pip has error value, Please check the result

                        }
                    }
                }

                ri.Data = fileNameReplace;

                
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            //ri.CalcLapseTime();
            return ri.ToJsonResult();
        }

        public void CreateUserUpload(string filename)
        {
            var uploadStatus = UploadDataMaintenance.Get<UploadDataMaintenance>(q: Query.EQ("FileName", filename));
            if (uploadStatus == null)
            {
                string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["PIPUploadPath"]);
                string files = Directory.GetFiles(WebConfigurationManager.AppSettings["PIPUploadPath"]).Where(x => System.IO.Path.GetFileName(x).Equals(filename)).FirstOrDefault();
                string file = System.IO.Path.GetFileName(files);
                uploadStatus = new UploadDataMaintenance();
                uploadStatus.ListExecute = new List<ExecuteUpdate>();
                uploadStatus.Maintenance = "PIP";
                uploadStatus.Path = System.IO.Path.Combine(folder, file);
                uploadStatus.FileName = filename;
                uploadStatus.UploadDate = System.IO.File.GetCreationTimeUtc(file);
                uploadStatus.UserUpload = WebTools.LoginUser.UserName;
                uploadStatus.Save();
            }
            
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
                string filename = System.IO.Path.GetFileName(file);
                doc.Set("FileName", System.IO.Path.GetFileName(file));
                //doc.Set("LastWrite", System.IO.File.GetLastWriteTimeUtc(file));
                doc.Set("CreateDate", System.IO.File.GetCreationTimeUtc(file));

                var uploadStatus = UploadDataMaintenance.Get<UploadDataMaintenance>(q: Query.And(Query.EQ("FileName", filename), Query.EQ("Maintenance", "PIP")));
                if (uploadStatus == null)
                {
                    
                    doc.Set("UserUpload", " - ");
                    doc.Set("LastExecute", Tools.DefaultDate);
                    doc.Set("Status", " - ");
                }
                else
                {
                    if (uploadStatus.ListExecute.Any() && uploadStatus.ListExecute.Count > 0)
                    {
                        var execute = uploadStatus.ListExecute.OrderByDescending(x => x.ExecuteDate).FirstOrDefault();
                        doc.Set("UserUpload", uploadStatus.UserUpload);
                        doc.Set("LastExecute", execute.ExecuteDate);
                        doc.Set("Status", execute.Status);
                    }
                    else
                    {
                        doc.Set("UserUpload", uploadStatus.UserUpload);
                        doc.Set("LastExecute", Tools.DefaultDate);
                        doc.Set("Status", " - ");
                    }
                }

                bdocs.Add(doc);
            }

            return Json(new { Data = DataHelper.ToDictionaryArray(bdocs.OrderByDescending(x => BsonHelper.GetDateTime(x, "CreateDate")).ToList()), Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
        }

        public void updateexecute(string filename)
        {
            var uploadStatus = UploadDataMaintenance.Get<UploadDataMaintenance>(q: Query.And(Query.EQ("FileName", filename), Query.EQ("Maintenance", "PIP")));
            if (uploadStatus != null)
            {
                ExecuteUpdate update = new ExecuteUpdate() { Status = "Success", Message = "Load PIP Datas Done", UserUpdate = WebTools.LoginUser.UserName, ExecuteDate = DateTime.Now };
                uploadStatus.ListExecute.Add(update);
                uploadStatus.Save();
            }
            else
            {
                CreateUserUpload(filename);
                updateexecute(filename);
            }
        }

        public async Task<JsonResult> BgWorker(string id)
        {
            string dt = DateTime.Now.ToString("yyyyMMdd");
            //DataHelper.Save("temp_pipupload", new BsonDocument().Set("_id", dt).Set("Status", "In Progress"));
            var percentage = "";
            List<BsonDocument> outputdata = new List<BsonDocument>();
            var progressHandler = new Progress<string>(value =>
            {
                percentage = value;
            });
            var progress = progressHandler as IProgress<string>;

            await Task.Run(() =>
            {
                Execute(id);
            });

            await Task.Run(() =>
            {

                LoadPIP(out outputdata, "_PIPMaster_" + dt);
            })
            .ContinueWith(t => DataHelper.Save("temp_pipupload", new BsonDocument().Set("_id", dt).Set("Status", "Done")));
            percentage = "Completed.";

            return Json(new { Data = DataHelper.ToDictionaryArray(outputdata), Result = "OK", Message = "Load PIP Done" }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CollectTemDataUpload(string tmpfile)
        {
            return MvcResultInfo.Execute(() =>
            {
                var t = DataHelper.Populate("PIPUploadResult", Query.EQ("filename", tmpfile));
                return DataHelper.ToDictionaryArray(t);
            });
        }
        public JsonResult CreateTemp()
        {
            string dt = DateTime.Now.ToString("yyyyMMdd");
            DataHelper.Save("temp_pipupload", new BsonDocument().Set("_id", dt).Set("Status", "In Progress").Set("Note", "Execute data is running. Some features will be disabled for a while"));
            return Json("OK");
        }

        public JsonResult DropStatus()
        {
            DataHelper.Delete("temp_pipupload");
            return Json("OK");
        }
        public JsonResult GetProgresStatus()
        {
            var ri = new ResultInfo();
            string dt = DateTime.Now.ToString("yyyyMMdd");
            var collection = "_PIPMaster_" + dt;
            var y = DataHelper.Populate(collection, null, 5, 0, null, SortBy.Descending("_id"));
            ri.Data = y.Select(x => x.ToDictionary());
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult GetStatus()
        {
            var ri = new ResultInfo();
            var y = DataHelper.Populate("temp_pipupload", null, 1, 0, null, SortBy.Descending("_id"));
            if (y.Count == 0)
            {
                var ys = new BsonDocument().Set("_id", 0).Set("Status", "None");
                y.Add(ys);
            }
            ri.Data = y.Select(x => x.ToDictionary());
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult Execute(string id)
        {
            try
            {
                string dt = DateTime.Now.ToString("yyyyMMdd");
                DataHelper.Delete("_PIPMaster_" + dt);
                #region Extract XLS to Bsondoc

                ExtractionHelper e = new ExtractionHelper();
                string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["PIPUploadPath"]);
                string Path = folder + @"\" + id;
                IList<BsonDocument> datas = e.Extract(Path);

                datas = datas.Where(x =>
                    !String.IsNullOrEmpty(x.GetString("Rig_Type").Trim()) &&
                    !String.IsNullOrEmpty(x.GetString("Rig_Name").Trim()) &&
                    !String.IsNullOrEmpty(x.GetString("Activity_Type").Trim()) &&
                    !String.IsNullOrEmpty(x.GetString("Well_Name").Trim())
                    ).ToList();

                int No = 0;
                if (datas.Any())
                {
                    foreach (var y in datas.Where(x=>x.GetString("Rig_Name") != null && !x.GetString("Rig_Name").Trim().Equals("") ))
                    {
                        No++;
                        string genId = DateTime.Now.ToString("yyyyMMddHHmmssfff" + No);
                        string message = ""; string pipType = ""; bool insert = true;
                        //string status = CheckPipElement(y, out message, out insert, out pipType);

                        var Notes = "Well Name:" + y.GetString("Well_Name") + " Rig Name:" + y.GetString("Rig_Name") + " Activity Type:" + y.GetString("Activity_Type");

                        var startDate = y.GetString("Activity_Start");
                        DateTime start = Tools.DefaultDate;

                        var finishDate = y.GetString("Activity_End");
                        DateTime finish = Tools.DefaultDate;
                        try
                        {
                            start = DateTime.ParseExact(startDate, "M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch (Exception ex)
                        {

                            try
                            {
                                start = DateTime.ParseExact(startDate, "yyyy-M-d HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            catch (Exception ec)
                            {
                                //error.Messages.Add(new MessageStatus { Message = ex.Message, Status = "Error" });
                            }
                        }

                        try
                        {
                            finish = DateTime.ParseExact(finishDate, "M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch (Exception ex)
                        {

                            try
                            {
                                finish = DateTime.ParseExact(finishDate, "yyyy-M-d HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            catch (Exception ec)
                            {
                                //error.Messages.Add(new MessageStatus { Message = ex.Message, Status = "Error" });
                            }
                        }

                        y.Set("Activity_Start", Tools.ToUTC(start.Date));
                        y.Set("Activity_End", Tools.ToUTC(finish.Date));

                        DataHelper.Save("_PIPMaster_" + dt, y);
                        //if (insert)
                        //{
                        //    if (pipType.Equals("cr"))
                        //    {
                        //        if (!y.GetString("Rig_Name").Equals(""))
                        //        {
                        //            DataHelper.Save("_PIPMaster_" + dt, y);
                        //        }
                        //    }
                        //    else
                        //    {
                        //        if (!y.GetString("Well_Name").Equals("") && !y.GetString("Activity_Type").Equals(""))
                        //        {
                        //            DataHelper.Save("_PIPMaster_" + dt, y);
                        //        }

                        //    }
                        //}

                    }
                }
                #endregion

                return Json(new { Data = "", Result = "OK", Message = "Load PIP Done" }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message, Trace = ex.StackTrace }, JsonRequestBehavior.AllowGet);
            }
        }

        public string CheckPipElement(BsonDocument y, out string message, out bool insert, out string pipType)
        {

            pipType = y.GetString("PIP_Type").ToLower().Trim();


            var riskDays = y.GetString("Risk__Days_(+)");
            var opportunityDays = y.GetString("Opportunity__Days_(-)");
            var LeOpportunityDays = y.GetString("LE_Opportunity_Days_(-)");
            var LeRiskDays = y.GetString("LE_Risk_Days_(+)");

            var riskCost = y.GetString("Risk_Cost_MM_(+)");
            var opportunityCost = y.GetString("Opportunity_Cost_MM__(-)");
            var LeOpportunityCost = y.GetString("LE_Opportunity_Cost_MM_(-)");
            var LeRiskCost = y.GetString("LE_Risk_Cost_MM_(+)");

            if (pipType.Equals("rig") || pipType.Equals("cr") || pipType.Equals("well") || pipType.Equals("ce"))
            {
                if (riskDays.Equals("") && opportunityDays.Equals("") && LeRiskDays.Equals("") && LeOpportunityDays.Equals(""))
                {
                    if (!riskCost.Equals("") || !opportunityCost.Equals("") || !LeRiskCost.Equals("") || !LeOpportunityCost.Equals(""))
                    {
                        insert = true;
                        message = "succes";
                        pipType = "cr";
                    }
                    else
                    {
                        insert = false;
                        message = "Neither contain information on Cost and Days";
                        pipType = "";
                    }

                }
                else
                {
                    if (pipType.Equals("well") || pipType.Equals("ce"))
                    {
                        if (!riskCost.Equals("") || !opportunityCost.Equals("") || !LeRiskCost.Equals("") || !LeOpportunityCost.Equals(""))
                        {
                            insert = true;
                            message = "succes";
                            pipType = "ce";
                        }
                        else
                        {
                            insert = false;
                            message = "Neither contain information on Cost and Days";
                            pipType = "";
                        }
                    }
                    else
                    {
                        insert = false;
                        message = "Any information for days";
                        pipType = "cr";
                    }

                }
            }

            else
            {
                insert = false;
                message = "Activity PIP Not Identified";
                pipType = "";
            }
            return "OK";

        }

        public JsonResult ShowResult()
        {
            string dt = DateTime.Now.ToString("yyyyMMdd");
            List<BsonDocument> outputdata = new List<BsonDocument>();
            LoadPIP(out outputdata, "_PIPMaster_" + dt);
            return Json(new { Data = DataHelper.ToDictionaryArray(outputdata), Result = "OK", Message = "Load PIP Done" }, JsonRequestBehavior.AllowGet);
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

        public JsonResult ChangeDataInExcel(string id)
        {
            return MvcResultInfo.Execute(() =>
            {
                //string id = "20160621095947-20160504090328-20160503014257-WellPIP-03-May-2016-GA022_Drilling_Completion.xlsx";
                ExtractionHelper e = new ExtractionHelper();
                string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["PIPUploadPath"]);
                byte[] fileBytes = System.IO.File.ReadAllBytes(folder + @"\" + id);
                string Path = folder + @"\" + id;
                IList<BsonDocument> datas = e.Extract(Path);

                Workbook worbbook = new Workbook(Path);
                var ws = worbbook.Worksheets[0];
                var startRow = 2;
                //Getting the Style of the A3 Cell
                
                foreach (var data in datas.Where(x=>!x.GetString("Rig_Name").Replace(" ","").Equals("")))
                {
                    ws.Cells["A" + startRow.ToString()].Value = data.GetString("Well_Name");
                    ws.Cells["B" + startRow.ToString()].Value = data.GetString("Rig_Name");
                    ws.Cells["C" + startRow.ToString()].Value = data.GetString("Activity_Type");
                    ws.Cells["D" + startRow.ToString()].Value = data.GetString("Assign_To_OP");
                    var PipType = data.GetString("PIP_Type");
                    ws.Cells["E" + startRow.ToString()].Value = PipType.ToLower() == "ce" ? "Well/Project PIP" : PipType.ToLower() == "cr" ? "Rig/General SCM" : PipType;
                    ws.Cells["F" + startRow.ToString()].Value = data.GetString("Idea");
                    ws.Cells["G" + startRow.ToString()].Value = String.Format("{0:d/M/yyyy}", data.GetDateTime("Activity_Start"));

                    ws.Cells["H" + startRow.ToString()].Value = String.Format("{0:d/M/yyyy}", data.GetDateTime("Activity_End"));
                    ws.Cells["I" + startRow.ToString()].Value = data.GetString("Year");
                    var realized = data.GetString("%_Complete") == null ? data.GetString("Realized/Not_Realized") : data.GetString("%_Complete");
                    ws.Cells["J" + startRow.ToString()].Value = data.GetString("%_Complete") == "" ? data.GetString("Realized/Not_Realized") : data.GetString("%_Complete");
                    ws.Cells["K" + startRow.ToString()].Value = data.GetString("High_Level_Classification");
                    ws.Cells["L" + startRow.ToString()].Value = data.GetString("Theme");
                    ws.Cells["M" + startRow.ToString()].Value = data.GetDouble("Opportunity_Days_(-)");
                    ws.Cells["N" + startRow.ToString()].Value = data.GetDouble("Risk_Days_(+)");
                    ws.Cells["O" + startRow.ToString()].Value = data.GetDouble("Opportunity_Cost_MM__(-)");
                    ws.Cells["P" + startRow.ToString()].Value = data.GetDouble("Risk_Cost_MM_(+)");
                    ws.Cells["Q" + startRow.ToString()].Value = data.GetDouble("LE_Opportunity_Days_(-)");
                    ws.Cells["R" + startRow.ToString()].Value = data.GetDouble("LE_Risk_Days_(+)");
                    ws.Cells["S" + startRow.ToString()].Value = data.GetDouble("LE_Opportunity_Cost_MM_(-)");
                    ws.Cells["T" + startRow.ToString()].Value = data.GetDouble("LE_Risk_Cost_MM_(+)");
                    ws.Cells["U" + startRow.ToString()].Value = data.GetString("Action_Party");
                    ws.Cells["V" + startRow.ToString()].Value = data.GetString("Performance_Unit");
                    ws.Cells["W" + startRow.ToString()].Value = data.GetString("Asset_Name");
                    ws.Cells["X" + startRow.ToString()].Value = data.GetString("Project_Name");
                    startRow++;
                }
                worbbook.Save(Path, Aspose.Cells.SaveFormat.Xlsx);
                return id;
            });
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

        public FileResult DonwloadResult()
        {
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "pipexporttemplate.xlsx");
            
            return File(xlst, Tools.GetContentType(".xlsx"), "WEISPIPTemplate.xlsx");
        }

        public void LoadPIP(out List<BsonDocument> OutMessage, string tablename = "")
        {
            string dt = DateTime.Now.ToString("yyyyMMdd");
            var note = "";
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
            var foundWA = true;

            foreach (var pip in pips)
            {
                foundWA = true;
                //if (BsonHelper.GetString(pip, "Idea").Equals("Zonal Isolation Success - Eliminate remedial cement risk"))
                //{

                //}
                //bool newStart = false;

                var PIPType = (pip.GetString("PIP_Type").ToLower() == "cr" || pip.GetString("PIP_Type").ToLower() == "rig/general scm") ? "reduction" : "efficient";
                var rigName = pip.GetString("Rig_Name").Trim();
                var wellname = PIPType != "reduction" ? pip.GetString("Well_Name").Trim() : rigName.Trim() + "_CR";
                var actType = PIPType != "reduction" ? pip.GetString("Activity_Type").Trim() : "";
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
                        var qx = Query.And(qwas);
                        var was1 = WellActivity.Populate<WellActivity>(qx);
                        if (was1 == null || was1.Count == 0)
                        {
                            //create new wellplan or add phase
                            var newPlan = new WellActivity();
                            newPlan.WellName = wellname;
                            newPlan.RigName = rigName;

                            var newPhase = new WellActivityPhase();
                            newPhase.ActivityType = actType;

                            var qs99 = new List<IMongoQuery>();
                            qs99.Add(Query.EQ("WellName", wellname));
                            //if(rigName != "")
                            qs99.Add(Query.EQ("RigName", rigName));
                            var singleWellPlan = WellActivity.Get<WellActivity>(Query.And(qs99));

                            if (singleWellPlan != null)
                            {
                                var PhaseNumber = 1;
                                //well plan found, just add the phase
                                if (singleWellPlan.Phases == null)
                                {
                                    singleWellPlan.Phases = new List<WellActivityPhase>();
                                }
                                else
                                {
                                    //count phase no
                                    PhaseNumber = singleWellPlan.Phases.Max(x => x.PhaseNo) + 1;
                                }

                                var baseOPs = pip.GetString("Assign_To_OP").Trim().Equals("") ? "" : pip.GetString("Assign_To_OP");
                                List<string> baseOPnew = baseOPs.Split(new char[] { ',' }).ToList();
                                string DefaultOP = "OP15";
                                if (Config.GetConfigValue("BaseOPConfig") != null)
                                {
                                    DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
                                }
                                List<string> deffOP = new List<string>();
                                deffOP.Add(DefaultOP);

                                newPhase.PhaseNo = PhaseNumber;
                                newPhase.BaseOP = deffOP;
                                newPhase.EventCreatedFrom = "UploadPIP";
                                newPhase.IsPostOP = true;

                                singleWellPlan.Phases.Add(newPhase);
                                note = "Updating activity phase";
                                DataHelper.Save("temp_pipupload", new BsonDocument().Set("_id", dt).Set("Status", "In Progress").Set("Note", note));
                                singleWellPlan.Save();
                                was1.Add(singleWellPlan);
                            }
                            else
                            {
                                //well plan not found, create a new one!
                                newPlan.AssetName = pip.GetString("Asset_Name");
                                newPlan.ProjectName = pip.GetString("Project_Name");
                                newPlan.Phases = new List<WellActivityPhase>();
                                newPlan.Phases.Add(newPhase);
                                newPlan.Save();

                                was1.Add(newPlan);
                            }
                        }

                        //add to wellname master
                        WellInfo wellmaster = new WellInfo();
                        wellmaster._id = wellname;
                        var chck_well = WellInfo.Get<WellInfo>(Query.EQ("_id", wellname));
                        if (chck_well == null)
                        {
                            note = "Updating well master";
                            DataHelper.Save("temp_pipupload", new BsonDocument().Set("_id", dt).Set("Status", "In Progress").Set("Note", note));
                            wellmaster.Save();
                        }

                        //add to rigname master
                        MasterRigName rigmaster = new MasterRigName();
                        rigmaster._id = rigName;
                        var chck_rig = MasterRigName.Get<MasterRigName>(Query.EQ("_id", rigName));
                        if (chck_rig == null)
                        {
                            note = "Updating rig master";
                            DataHelper.Save("temp_pipupload", new BsonDocument().Set("_id", dt).Set("Status", "In Progress").Set("Note", note));
                            rigmaster.Save();
                        }


                        //add to activity master
                        MasterActivity actmaster = new MasterActivity();
                        actmaster._id = actType;
                        var chck_act = MasterActivity.Get<MasterActivity>(Query.EQ("_id", actType));
                        if (chck_act == null)
                        {
                            note = "Updating activity master";
                            DataHelper.Save("temp_pipupload", new BsonDocument().Set("_id", dt).Set("Status", "In Progress").Set("Note", note));
                            actmaster.Save();
                        }

                        var was = was1.Select(d =>
                                    {
                                        d.Phases = d.Phases.Where(e => e.ActivityType.Equals(actType)).ToList();
                                        return d;
                                    })
                            //.OrderBy(d=>d.Phases[0].PlanSchedule.Finish)
                                    .ToList();
                        var list = new List<BsonDocument>();
                        //Dictionary<string, double> list = new Dictionary<string, double>();
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

                                list.Add(new
                                {
                                    UARigSequenceId = w.UARigSequenceId,
                                    datediff = datediff
                                }.ToBsonDocument());
                            }
                            var seqphase = list.OrderBy(x => x.GetDouble("datediff")).FirstOrDefault().GetString("UARigSequenceId");
                            IMongoQuery qry = Query.EQ("UARigSequenceId", seqphase);
                            var data = DataHelper.Populate<WellActivity>("WEISWellActivities", qry).FirstOrDefault();
                            //WellActivity.Get<WellActivity>(seqphase);
                            //var data = was.Where(x => x._id.ToString().Equals(seqphase)).FirstOrDefault();

                            SequenceId = data.UARigSequenceId;
                            PhaseNo = data.Phases[0].PhaseNo;

                            foundWA = true;
                        }
                        else
                        {
                            foundWA = false;
                        }
                    }

                    saveIt = false;
                    wp = PIPType != "reduction" ? WellPIP.GetByWellActivity(wellname, actType) : WellPIP.Get<WellPIP>(Query.EQ("WellName", wellname));
                    //wp = PIPType != "reduction" ?
                    //    WellPIP.GetByOpsActivity(SequenceId, PhaseNo) :
                    //    WellPIP.Get<WellPIP>(Query.EQ("WellName", wellname));
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
                    var acpty = pip.GetString("Action_Party");
                    if (wp.Elements == null) wp.Elements = new List<PIPElement>();
                    var pe = wp.Elements.FirstOrDefault(d => d.Title == idea && d.ActionParty == acpty);
                    if (pe == null)
                    {
                        pe = new PIPElement();
                        pe.ElementId = wp.Elements.Count() + 1;
                        pe.Title = idea;
                        List<string> baseops = new List<string>();
                        if (pip.GetString("Assign_To_OP").Trim().Equals(""))
                        {

                        }
                        else
                        {
                            var spliteds = pip.GetString("Assign_To_OP").Split(',');
                            if (spliteds.Count() > 1)
                            {
                                foreach (var y in spliteds)
                                {
                                    baseops.Add(y.ToUpper().Trim());
                                }
                            }
                            else
                            {
                                baseops.Add(pip.GetString("Assign_To_OP").ToUpper().Trim());
                            }
                        }

                        pe.AssignTOOps = baseops;


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
                        pe.DaysCurrentWeekImprovement = pip.GetDouble("LE_Opportunity_Days_(-)");
                        pe.DaysCurrentWeekRisk = pip.GetDouble("LE_Risk_Days_(+)");
                        pe.CostCurrentWeekImprovement = pip.GetDouble("LE_Opportunity_Cost_MM_(-)");
                        pe.CostCurrentWeekRisk = pip.GetDouble("LE_Risk_Cost_MM_(+)");
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
                        note = "Inserting new PIP Element";
                        DataHelper.Save("temp_pipupload", new BsonDocument().Set("_id", dt).Set("Status", "In Progress").Set("Note", note));
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

                        List<string> baseops = new List<string>();
                        if (pip.GetString("Assign_To_OP").Trim().Equals(""))
                        {

                        }
                        else
                        {
                            var spliteds = pip.GetString("Assign_To_OP").Split(',');
                            if (spliteds.Count() > 1)
                            {
                                foreach (var y in spliteds)
                                {
                                    baseops.Add(y);
                                }
                            }
                            else
                            {
                                baseops.Add(pip.GetString("Assign_To_OP"));
                            }
                        }

                        wp.Elements.FirstOrDefault(d => d.Title.Equals(idea)).AssignTOOps = baseops;

                        //pe.ResetAllocation();
                        saveIt = true;
                        sb.Append("Update PIP Element;");
                        note = "Updating PIP Element";
                        DataHelper.Save("temp_pipupload", new BsonDocument().Set("_id", dt).Set("Status", "In Progress").Set("Note", note));
                    }
                }

                if (wp != null && saveIt)
                {
                    if (!foundWA)
                    {
                        sb.Append("Cant find matched Well Activity;");
                    }
                    sb.Append("Data has been Saved to WEIS;");
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
                var daysCurrentWeekImprovement = BsonHelper.GetDouble(t, "LE_Opportunity_Days_(-)");
                var daysCurrentWeekRisk = BsonHelper.GetDouble(t, "LE_Risk_Days_(+)");
                var costCurrentWeekImprovement = BsonHelper.GetDouble(t, "LE_Opportunity_Cost_MM_(-)");
                var costCurrentWeekRisk = BsonHelper.GetDouble(t, "LE_Risk_Cost_MM_(+)");

                t.Remove("%_Complete");
                t.Remove("Opportunity_Cost_MM__(-)");
                t.Remove("Opportunity__Days_(-)");
                t.Remove("Risk_Cost_MM_(+)");
                t.Remove("Risk__Days_(+)");
                t.Remove("LE_Opportunity_Days_(-)");
                t.Remove("LE_Risk_Days_(+)");
                t.Remove("LE_Opportunity_Cost_MM_(-)");
                t.Remove("LE_Risk_Cost_MM_(+)");

                t.Set("Complete", complete);
                t.Set("OpportunityCostMM", opportunityCostM);
                t.Set("OppoertunityDays", opportunityDays);
                t.Set("RiskCostMM", riskcostMM);
                t.Set("RiskDays", riskdays);
                t.Set("LEOpportunityDays", daysCurrentWeekImprovement);
                t.Set("LERiskDays", daysCurrentWeekRisk);
                t.Set("LEOpportunityCost", costCurrentWeekImprovement);
                t.Set("LERiskCost", costCurrentWeekRisk);

            }
            //return dataChangesResult;
        }
    }

    [BsonIgnoreExtraElements]
    public class UploadDataMaintenance : ECISModel
    {
        public override string TableName
        {
            get { return "UploadDataMaintenance"; }
        }
        public string Maintenance { get; set; }
        public string UserUpload { get; set; }
        public string UserEdit { get; set; }
        public bool OldUserUpload { get; set; }
        public DateTime UploadDate { get; set; }
        public string FileName { get; set; }
        public string Path { get; set; }
        public List<ExecuteUpdate> ListExecute { get; set; }
        public bool isSingle { get; set; }
        public UploadDataMaintenance()
        {
            ListExecute = new List<ExecuteUpdate>();
        }

    }

   

    public class ExecuteUpdate
    {
        public string Status { get; set; }
        public DateTime ExecuteDate { get; set; }
        public string UserUpdate { get; set; }
        public string Message { get; set; }
        public ExecuteUpdate()
        {
            ExecuteDate = new DateTime();
        }
    }

}