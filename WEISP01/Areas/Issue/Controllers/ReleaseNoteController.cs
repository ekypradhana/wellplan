using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;

using ECIS.Core;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using Microsoft.AspNet.Identity;
using ECIS.Identity;
using ECIS.AppServer.Models;
using ECIS.Client.WEIS;
using Microsoft.Owin.Security;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using ECIS.AppServer.Areas.WebMenu.Models;

using Aspose.Cells;
using Aspose.Pdf;
using Aspose.Pdf.Generator;
using System.Text;
using System.Globalization;
using System.IO;

namespace ECIS.AppServer.Areas.Issue.Controllers
{
    public class ReleaseNoteController : Controller
    {
        //
        // GET: /Issue/ReleaseNote/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult Populate(string Keyword = null,
        List<string> Module = null,
        List<string> Priority = null,
        List<string> Status = null,
        List<string> Type = null)
        {
            Keyword = (Keyword == null ? "" : Keyword).ToString().ToLower();
            Module = (Module == null ? new List<string>() : Module);
            Priority = (Priority == null ? new List<string>() : Priority);
            Status = (Status == null ? new List<string>() : Status);
            Type = (Type == null ? new List<string>() : Type);

            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                IMongoQuery query = null;
                var queries = new List<IMongoQuery>();

                var queriesSearch = new List<IMongoQuery>();
                if (!Keyword.Equals(""))
                {
                    queriesSearch.Add(Query.Matches("VersionStr", new BsonRegularExpression(new Regex(Keyword, RegexOptions.IgnoreCase))));
                    queriesSearch.Add(Query.Matches("Title", new BsonRegularExpression(new Regex(Keyword, RegexOptions.IgnoreCase))));
                    queriesSearch.Add(Query.Matches("UserId", new BsonRegularExpression(new Regex(Keyword, RegexOptions.IgnoreCase))));
                    queriesSearch.Add(Query.Matches("Module", new BsonRegularExpression(new Regex(Keyword, RegexOptions.IgnoreCase))));
                    queriesSearch.Add(Query.Matches("Type", new BsonRegularExpression(new Regex(Keyword, RegexOptions.IgnoreCase))));
                    queriesSearch.Add(Query.Matches("Description", new BsonRegularExpression(new Regex(Keyword, RegexOptions.IgnoreCase))));
                }

                if (queriesSearch.Count > 0)
                    queries.Add(Query.Or(queriesSearch.ToArray()));

                if (Module.Count > 0)
                    queries.Add(Query.In("Module", new BsonArray(Module.ToArray())));

                if (Priority.Count > 0)
                    queries.Add(Query.In("Priority", new BsonArray(Priority.ToArray())));

                if (Status.Count > 0)
                    queries.Add(Query.In("Status", new BsonArray(Status.ToArray())));

                if (Type.Count > 0)
                    queries.Add(Query.In("Type", new BsonArray(Type.ToArray())));

                if (queries.Count > 0)
                    query = Query.And(queries.ToArray());

                return ReleaseNote.Populate<ReleaseNote>(query).OrderByDescending(x => x.LastUpdate);
            });
        }

        public JsonResult Save(string Title,
            string Type,
            List<string> Module,
            bool IsSendEmail,
            string Email,
            string Dependon,
            string Description,
            string Note, bool isIncreaseVersion = false, bool isIncreaseSubversion = false, string Id = null)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                ReleaseNote note = new ReleaseNote();

                if (Id != null && Id != "")
                {
                    note = ReleaseNote.Get<ReleaseNote>(Query.EQ("_id", Id));
                    var prevId = note._id;
                    note.Delete();
                    note._id = prevId;
                }

                string JoinModule = "";
                if (Module != null && Module.Count > 0)
                {
                    JoinModule = String.Join(",", Module.ToArray());
                }

                note.Title = Title.Trim();
                note.Type = Type;
                note.Module = JoinModule;
                note.EmailReceivers = Email;

                note.DependsOn = Dependon;
                note.Description = Description.Trim();
                note.Notes = Note.Trim();
                note.CreatedBy = WebTools.LoginUser.UserName;

                if (Id != null && Id != "")
                {
                    DataHelper.Save(note.TableName, note.ToBsonDocument());
                }
                else
                {
                    if (isIncreaseVersion)
                        note.Save(references: new string[] { "increaseVer" });
                    else if (isIncreaseSubversion)
                        note.Save(references: new string[] { "increaseSubver" });
                    else
                        note.Save();
                }

                if (!note.EmailReceivers.Trim().Equals(""))
                    DoSendMail(note);
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        private void DoSendMail(ReleaseNote note)
        {
            if (note == null)
                return;

            var toMails = new string[] { };
            if (note.EmailReceivers.Contains("Eaciit Account Team"))
            {
                note.EmailReceivers = note.EmailReceivers.Replace("Eaciit Account Team", GetEaciitAccountTeam());
            }
            toMails = note.EmailReceivers.Split(',').Select(d => d.Trim()).ToArray();

            Dictionary<string, string> variables = new Dictionary<string, string>();
            variables.Add("Title", note.Title);
            variables.Add("Type", note.Type);
            variables.Add("Module", note.Module);
            variables.Add("Description", note.Description);
            variables.Add("Note", note.Notes);
            variables.Add("Version", note.VersionStr);

            Email.Send("ReleaseNote",
                toMails: toMails,
                ccMails: null,
                variables: variables,
                developerModeEmail: WebTools.LoginUser.Email);
        }

        private string GetEaciitAccountTeam()
        {
            var data = Config.Get<Config>(Query.EQ("_id", "ReleaseNoteEaciitEmail"));

            if (data == null)
                return "";

            return Convert.ToString(data.ConfigValue);
        }

        public JsonResult SendMail(string id)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var note = ReleaseNote.Get<ReleaseNote>(Query.EQ("_id", id));
                DoSendMail(note);
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult Get(string id)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var note = ReleaseNote.Get<ReleaseNote>(Query.EQ("_id", id));
                ri.Data = note;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult GetLatestVersion()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var latest = ReleaseNote.Get<ReleaseNote>(null, SortBy.Descending("VersionStr"));
                ri.Data = (latest == null) ? "0.0.0" : latest.VersionStr;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult Export(string Keyword = null,
        List<string> Module = null,
        List<string> Priority = null,
        List<string> Status = null,
        List<string> Type = null)
        {
            Keyword = (Keyword == null ? "" : Keyword).ToString().ToLower();
            Module = (Module == null ? new List<string>() : Module);
            Priority = (Priority == null ? new List<string>() : Priority);
            Status = (Status == null ? new List<string>() : Status);
            Type = (Type == null ? new List<string>() : Type);

            IMongoQuery query = null;
            var queries = new List<IMongoQuery>();

            var queriesSearch = new List<IMongoQuery>();
            if (!Keyword.Equals(""))
            {
                queriesSearch.Add(Query.Matches("VersionStr", new BsonRegularExpression(new Regex(Keyword, RegexOptions.IgnoreCase))));
                queriesSearch.Add(Query.Matches("Title", new BsonRegularExpression(new Regex(Keyword, RegexOptions.IgnoreCase))));
                queriesSearch.Add(Query.Matches("UserId", new BsonRegularExpression(new Regex(Keyword, RegexOptions.IgnoreCase))));
                queriesSearch.Add(Query.Matches("Module", new BsonRegularExpression(new Regex(Keyword, RegexOptions.IgnoreCase))));
                queriesSearch.Add(Query.Matches("Type", new BsonRegularExpression(new Regex(Keyword, RegexOptions.IgnoreCase))));
                queriesSearch.Add(Query.Matches("Description", new BsonRegularExpression(new Regex(Keyword, RegexOptions.IgnoreCase))));
            }

            if (queriesSearch.Count > 0)
                queries.Add(Query.Or(queriesSearch.ToArray()));

            if (Module.Count > 0)
                queries.Add(Query.In("Module", new BsonArray(Module.ToArray())));

            if (Priority.Count > 0)
                queries.Add(Query.In("Priority", new BsonArray(Priority.ToArray())));

            if (Status.Count > 0)
                queries.Add(Query.In("Status", new BsonArray(Status.ToArray())));

            if (Type.Count > 0)
                queries.Add(Query.In("Type", new BsonArray(Type.ToArray())));

            if (queries.Count > 0)
                query = Query.And(queries.ToArray());

            var ReleaseNotes =  ReleaseNote.Populate<ReleaseNote>(query).OrderByDescending(x => x.LastUpdate);

            string template = Path.Combine(Server.MapPath("~/App_Data/Temp"), "ReleaseNoteTemplate.xlsx");
            Workbook workbookTemplate = new Workbook(template);
            var worksheetTemplate = workbookTemplate.Worksheets.FirstOrDefault();

            if (System.IO.File.Exists(template) == false)
                throw new Exception("Template file is not exist: " + template);

            var currentDate = DateTime.Now.ToString("MMM, dd yyyy");
            var ws = workbookTemplate.Worksheets[0];
            ws.Cells["D4"].Value = currentDate;

            Style style = workbookTemplate.CreateStyle();
            style.Font.IsBold = true;
            style.Font.Size = 36;

            Style style2 = workbookTemplate.CreateStyle();
            style.IsTextWrapped = true;

            int RowCellVersion = 7;

            if (ReleaseNotes != null && ReleaseNotes.Count() > 0)
            {
                foreach (var rn in ReleaseNotes)
                {
                    var currentRow = RowCellVersion;
                    ws.Cells["C" + currentRow.ToString()].Value = "v" + rn.VersionStr;
                    ws.Cells["C" + currentRow.ToString()].SetStyle(style);
                    ws.Cells.SetRowHeight(currentRow-1 , 47);

                    //Title
                    currentRow++;
                    ws.Cells["C" + currentRow.ToString()].Value = "Title";
                    ws.Cells["D" + currentRow.ToString()].Value = ":";
                    ws.Cells["E" + currentRow.ToString()].Value = rn.Title;

                    //Date
                    currentRow++;
                    ws.Cells["C" + currentRow.ToString()].Value = "Date Time";
                    ws.Cells["D" + currentRow.ToString()].Value = ":";
                    ws.Cells["E" + currentRow.ToString()].Value = rn.LastUpdate.ToString("dd-MMM-yyyy HH:mm:ss");

                    //Module
                    currentRow++;
                    ws.Cells["C" + currentRow.ToString()].Value = "Module";
                    ws.Cells["D" + currentRow.ToString()].Value = ":";
                    ws.Cells["E" + currentRow.ToString()].Value = rn.Module;
                    style2 = ws.Cells["E" + currentRow.ToString()].GetStyle();
                    style2.IsTextWrapped = true;
                    style2.VerticalAlignment = TextAlignmentType.Top;
                    ws.Cells["C" + currentRow.ToString()].SetStyle(style2);
                    ws.Cells["D" + currentRow.ToString()].SetStyle(style2);
                    ws.Cells["E" + currentRow.ToString()].SetStyle(style2);

                    //Type
                    currentRow++;
                    ws.Cells["C" + currentRow.ToString()].Value = "Type";
                    ws.Cells["D" + currentRow.ToString()].Value = ":";
                    ws.Cells["E" + currentRow.ToString()].Value = rn.Type;
                    ws.Cells["C" + currentRow.ToString()].SetStyle(style2);
                    ws.Cells["D" + currentRow.ToString()].SetStyle(style2);
                    ws.Cells["E" + currentRow.ToString()].SetStyle(style2);

                    //Desc
                    currentRow++;
                    ws.Cells["C" + currentRow.ToString()].Value = "Description";
                    ws.Cells["D" + currentRow.ToString()].Value = ":";
                    ws.Cells["E" + currentRow.ToString()].Value = rn.Description;
                    ws.Cells["C" + currentRow.ToString()].SetStyle(style2);
                    ws.Cells["D" + currentRow.ToString()].SetStyle(style2);
                    ws.Cells["E" + currentRow.ToString()].SetStyle(style2);

                    //Note
                    currentRow++;
                    ws.Cells["C" + currentRow.ToString()].Value = "Note";
                    ws.Cells["D" + currentRow.ToString()].Value = ":";
                    ws.Cells["E" + currentRow.ToString()].Value = rn.Notes;
                    ws.Cells["C" + currentRow.ToString()].SetStyle(style2);
                    ws.Cells["D" + currentRow.ToString()].SetStyle(style2);
                    ws.Cells["E" + currentRow.ToString()].SetStyle(style2);

                    ws.AutoFitRows();
                    RowCellVersion = RowCellVersion + 8;
                }
            }

            string returnName = String.Format("ReleaseNotes-{0}.pdf", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss"));
            string fileName = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                 String.Format("ReleaseNotes-{0}.pdf", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss"))); ;
            workbookTemplate.Save(fileName, Aspose.Cells.SaveFormat.Pdf);
            //string fileNameDownload = "ReleaseNotes-" + DateTime.Now.ToString("MMM-dd-yyy") + ".pdf";

            return Json(new { Success = true, Path = returnName }, JsonRequestBehavior.AllowGet);

        }

        public FileResult DownloadFile(string stringName)
        {
            //string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "pipexporttemplate.xlsx");
            var res = Path.Combine(Server.MapPath("~/App_Data/Temp"), stringName);


            return File(res, Tools.GetContentType(".pdf"), stringName);
        }
    }


}