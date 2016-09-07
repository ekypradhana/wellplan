using System.Threading;
using System.Threading.Tasks;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Text.RegularExpressions;

using ECIS.Client.WEIS;
using ECIS.Core;
using ECIS.Core.DataSerializer;
using Microsoft.Ajax.Utilities;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using Aspose.Cells;



namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth(WEISRoles = "Administrators")]
    public class UploadBizplanController : Controller
    {
        //
        // GET: /Shell/UploadBizplan/
        public ActionResult Index()
        {
            return View();
        }

        public FileResult DonwloadTemplate()
        {
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "BizPlanUploadTemplate.xlsx");

            return File(xlst, Tools.GetContentType(".xlsx"), "BizPlanUploadTemplate.xlsx");
        }

        #region
        //private static string GetColumnExcell(List<int> column)
        //{
        //    var result = "";
        //    foreach (var i in column)
        //    {
        //        if (i != 0)
        //            result += ConvertToString(i);
        //    }
        //    return result.Trim();
        //}

        //public static string ConvertToString(int angka)
        //{
        //    //72
        //    if (angka == 1) return "A";
        //    else if (angka == 2) return "B";
        //    else if (angka == 3) return "C";
        //    else if (angka == 4) return "D";
        //    else if (angka == 5) return "E";
        //    else if (angka == 6) return "F";
        //    else if (angka == 7) return "G";
        //    else if (angka == 8) return "H";
        //    else if (angka == 9) return "I";
        //    else if (angka == 10) return "J";
        //    else if (angka == 11) return "K";
        //    else if (angka == 12) return "L";
        //    else if (angka == 13) return "M";
        //    else if (angka == 14) return "N";
        //    else if (angka == 15) return "O";
        //    else if (angka == 16) return "P";
        //    else if (angka == 17) return "Q";
        //    else if (angka == 18) return "R";
        //    else if (angka == 19) return "S";
        //    else if (angka == 20) return "T";
        //    else if (angka == 21) return "U";
        //    else if (angka == 22) return "V";
        //    else if (angka == 23) return "W";
        //    else if (angka == 24) return "X";
        //    else if (angka == 25) return "Y";
        //    else return "Z";
        //}

        //private static string GetAlphabet(string col)
        //{
        //    var result = "";
        //    if (col != "")
        //    {
        //        var charArray = col.Trim().ToCharArray();
        //        foreach (var ch in charArray)
        //        {
        //            //if(typeof ch == "string"){

        //            //}
        //        }

        //    }
        //    return result;
        //}

        #endregion
        
        
        public FileResult Download(string id)
        {
            //string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), id);

            //Workbook wb = new Workbook(xlst);
            //var ws = wb.Worksheets[0];
            //if (ws.Cells.MaxRow > 0)
            //{
            //    var startRow = 1; string colName = ""; string SaveToOp = "";
            //    for (var x = 0; x < ws.Cells.Columns.Count + 2; x++)
            //    {
            //        colName = GetColumnExcell(new List<int> { 0, x+1 });
            //        var cc = ws.Cells.MergedCells.Count;
            //        foreach (var merge in ws.Cells.MergedCells)
            //        {
            //            var mergedType = merge.GetType();
            //            var merged = merge.ToString().Substring(23,5);
            //            SaveToOp = ws.Cells[merged.Substring(0,2)].Value.ToString().ToLower();
            //            if (SaveToOp == "save to op")
            //            {
            //                colName = merged.Substring(0, 2);
            //                startRow = Convert.ToInt32(merged.Substring(4, 1)) + 1;
            //            }
            //        }
                    
            //    }

            //        for (var i = 0; i < ws.Cells.Rows.Count; i++)
            //        {
            //            if (i != 0)
            //            {
            //                string SaveToOP = ws.Cells["A" + (i + 1)].Value.ToString();
            //                if (SaveToOP != "OP16")
            //                {
            //                    for (var j = 0; j < ws.Cells.Columns.Count + 2; j++)
            //                    {
            //                        Style style = ws.Cells[i, j].GetStyle();
            //                        style.ForegroundColor = System.Drawing.Color.Red;
            //                        style.Pattern = BackgroundType.Solid;
            //                        ws.Cells[i, j].SetStyle(style);
            //                    }

            //                }
            //            }


            //        }
            //}

            //var newFileNameSingle = Path.Combine(Server.MapPath("~/App_Data/Temp"),id);
            //wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);
            string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["BizPlanUploadPath"]);
            byte[] fileBytes = System.IO.File.ReadAllBytes(folder + @"\" + id);
            string fileName = id;
            return File(folder + @"\" + id, Tools.GetContentType(".xlsx"), Path.GetFileName(folder + @"\" + id));

        }

        public JsonResult GenerateFilteredTemplateFile(WaterfallBase wbs, string LineOfBusiness)
        {
            var division = 1000000.0;
            var now = Tools.ToUTC(DateTime.Now);
            var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
            var email = WebTools.LoginUser.Email;
            var raw = wbs.GetActivitiesForBizPlan(email);

            raw = raw.Select(d =>//(x => x.LineOfBusiness.ToLower().Equals(LineOfBusiness.ToLower())).OrderBy(x => x.RigName)
                {
                    var lbaps = new List<BizPlanActivityPhase>();
                    foreach (var e in d.Phases)
                    {
                        if(wbs.Status == null)
                        {
                            lbaps.Add(e);
                        }
                        else if ( wbs.Status != null && wbs.Status.Contains(e.Estimate.Status))
                        {
                            lbaps.Add(e);
                        }
                    }
                    d.Phases = lbaps.OrderBy(e => e.Estimate.EstimatePeriod.Start).ToList();//d.Phases.OrderBy(e => e.Estimate.EstimatePeriod.Start).ToList();
                    return d;
                }).ToList();

            if (!String.IsNullOrEmpty(LineOfBusiness))
            {
                raw = raw.Where(x => x.LineOfBusiness != null).ToList();
                raw = raw.Where(x => x.LineOfBusiness.ToLower().Equals(LineOfBusiness.ToLower())).OrderBy(x => x.RigName).Select(d =>
                {
                    d.Phases = d.Phases.OrderBy(e => e.Estimate.EstimatePeriod.Start).ToList();
                    return d;
                }).ToList();
            }
            var accessibleWells = WebTools.GetWellActivities();
            string previousOP = "";
            string nextOP = "";
            var datas = new List<object>();

            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "BizPlanUploadTemplate.xlsx");

            var MasterActivities = ActivityMaster.Populate<ActivityMaster>();

            Workbook wb = new Workbook(xlst);
            var ws = wb.Worksheets[0];
            int startRow = 2;
            var walkingRigName = "";
            var walkingDate = Tools.DefaultDate;

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
                            
                            var isHaveWeeklyReport = WellActivity.isHaveWeeklyReport(d.WellName, d.UARigSequenceId, p.ActivityType);
                            if (!isHaveWeeklyReport)
                            {
                                var check = WebTools.CheckAccessibleWellAndActivity(accessibleWells, d.WellName, p.ActivityType);
                                if (check)
                                {
                                    Style styleA = ws.Cells["A" + startRow.ToString()].GetStyle();
                                    styleA.ForegroundColor = System.Drawing.Color.FromArgb(1, 191, 191, 191);
                                    styleA.Pattern = BackgroundType.Solid;

                                    //ws.Cells["A" + startRow.ToString()].SetStyle(styleA);
                                    //ws.Cells["K" + startRow.ToString()].SetStyle(styleA);
                                    //ws.Cells["L" + startRow.ToString()].SetStyle(styleA);
                                    //ws.Cells["S" + startRow.ToString()].SetStyle(styleA);
                                    //ws.Cells["U" + startRow.ToString()].SetStyle(styleA);	

                                    ws.Cells["A" + startRow.ToString()].Value = !String.IsNullOrEmpty(p.Estimate.SaveToOP) ? "OP16" : p.Estimate.SaveToOP;
                                    ws.Cells["B" + startRow.ToString()].Value = String.IsNullOrEmpty(p.Estimate.Status) ? "Draft" : p.Estimate.Status;
                                    ws.Cells["C" + startRow.ToString()].Value = String.IsNullOrEmpty(d.LineOfBusiness) ? "DEEPWATER" : d.LineOfBusiness;
                                    ws.Cells["D" + startRow.ToString()].Value = d.Region;
                                    ws.Cells["E" + startRow.ToString()].Value = d.Country;
                                    ws.Cells["F" + startRow.ToString()].Value = d.Currency;
                                    ws.Cells["G" + startRow.ToString()].Value = d.OperatingUnit;
                                    ws.Cells["H" + startRow.ToString()].Value = d.PerformanceUnit;

                                    ws.Cells["I" + startRow.ToString()].Value = d.AssetName;
                                    ws.Cells["J" + startRow.ToString()].Value = d.ProjectName;
                                    ws.Cells["K" + startRow.ToString()].Value = d.WellName;
                                    ws.Cells["L" + startRow.ToString()].Value = p.ActivityType;
                                    ws.Cells["M" + startRow.ToString()].Value = MasterActivities.Where(x => x._id.Equals(p.ActivityType)).FirstOrDefault() == null ? "" : MasterActivities.Where(x => x._id.Equals(p.ActivityType)).FirstOrDefault().ActivityCategory;
                                    ws.Cells["N" + startRow.ToString()].Value = p.ActivityDesc; //scope desc
                                    ws.Cells["O" + startRow.ToString()].Value = d.ShellShare < 1 && d.ShellShare != 0 ? (d.ShellShare) : Tools.Div(d.ShellShare, 100);
                                    ws.Cells["P" + startRow.ToString()].Value = d.isInPlan ? "Yes" : "No";

                                    ws.Cells["Q" + startRow.ToString()].Value = p.FundingType;
                                    ws.Cells["R" + startRow.ToString()].Value = (p.Estimate.isFirstInitiate && d.ReferenceFactorModel.ToLower() == "default") ? "" : d.ReferenceFactorModel;
                                    ws.Cells["S" + startRow.ToString()].Value = p.Estimate.RigName;
                                    ws.Cells["T" + startRow.ToString()].Value = d.RigType;
                                    ws.Cells["U" + startRow.ToString()].Value = d.UARigSequenceId;

                                    //ws.Cells["U" + startRow.ToString()].PutValue(p.Estimate.EstimatePeriod.Start); //.Value = p.Estimate.EstimatePeriod.Start.ToString("dd-MMM-yyyy");
                                    //Style styleU = ws.Cells["U" + startRow.ToString()].GetStyle();
                                    //styleU.Custom = "m/d/yyyy";
                                    //ws.Cells["U" + startRow.ToString()].SetStyle(styleU);

                                    //ws.Cells["V" + startRow.ToString()].PutValue(p.Estimate.EstimatePeriod.Finish); //.Value = p.Estimate.EstimatePeriod.Finish.ToString("dd-MMM-yyyy");
                                    //Style styleV = ws.Cells["V" + startRow.ToString()].GetStyle();
                                    //styleV.Custom = "m/d/yyyy";
                                    //ws.Cells["V" + startRow.ToString()].SetStyle(styleV);

                                    if (p.Estimate.EstimatePeriod.Start == Tools.DefaultDate)
                                    {
                                        ws.Cells["V" + startRow.ToString()].PutValue(p.PhSchedule.Start);//PutValue(p.PhSchedule.Start); //.Value = p.PhSchedule.Start.ToString("dd-MMM-yyyy");

                                    }
                                    else
                                    {
                                        ws.Cells["V" + startRow.ToString()].PutValue(p.Estimate.EstimatePeriod.Start);//PutValue(p.PhSchedule.Start); //.Value = p.PhSchedule.Start.ToString("dd-MMM-yyyy");

                                    }

                                    Style styleW = ws.Cells["V" + startRow.ToString()].GetStyle();
                                    styleW.Custom = "m/d/yyyy";
                                    ws.Cells["V" + startRow.ToString()].SetStyle(styleW);

                                    //ws.Cells["V" + startRow.ToString()].PutValue(p.PlanSchedule.Finish);//PutValue(p.PhSchedule.Finish); //.Value = p.PhSchedule.Finish.ToString("dd-MMM-yyyy");
                                    //Style styleX = ws.Cells["V" + startRow.ToString()].GetStyle();
                                    //styleX.Custom = "m/d/yyyy";
                                    //ws.Cells["V" + startRow.ToString()].SetStyle(styleX);

                                    //calculate calc op start
                                    //if (startRow == 2)
                                    //{
                                    //    walkingDate = p.Estimate.EstimatePeriod.Start;
                                    //}

                                    //var calc = Tools.DefaultDate;
                                    //if (startRow > 2 && !String.IsNullOrEmpty(walkingRigName) && walkingRigName == p.Estimate.RigName)
                                    //{
                                    //    ws.Cells["W" + startRow.ToString()].Value = p.Estimate.EstimatePeriod.Days;
                                    //    calc = walkingDate.AddDays(p.Estimate.EstimatePeriod.Days);
                                    //    ws.Cells["X" + startRow.ToString()].PutValue(calc);
                                    //    walkingDate = calc;
                                    //}
                                    //else
                                    //{
                                    //    calc = p.Estimate.EstimatePeriod.Start.AddDays(p.Estimate.EstimatePeriod.Days);
                                    //    ws.Cells["W" + startRow.ToString()].Value = p.Estimate.EstimatePeriod.Days;
                                    //    ws.Cells["X" + startRow.ToString()].PutValue(calc);//PutValue(p.PhSchedule.Start); //.Value = p.PhSchedule.Start.ToString("dd-MMM-yyyy");
                                    //    walkingDate = calc;
                                    //}
                                    //styleW = ws.Cells["X" + startRow.ToString()].GetStyle();
                                    //styleW.Custom = "m/d/yyyy";
                                    //ws.Cells["X" + startRow.ToString()].SetStyle(styleW);

                                    //W = Mean Days
                                    //X = LS Start Date

                                    ws.Cells["W" + startRow.ToString()].Value = p.Estimate.NewMean.Days; //.EstimatePeriod.Days;
                                    ws.Cells["X" + startRow.ToString()].PutValue(p.PhSchedule.Start);

                                    Style styleLS = ws.Cells["X" + startRow.ToString()].GetStyle();
                                    styleLS.Custom = "m/d/yyyy";
                                    ws.Cells["X" + startRow.ToString()].SetStyle(styleLS);

                                    ws.Cells["Y" + startRow.ToString()].Value = p.Estimate.MaturityLevel;
                                    ws.Cells["Z" + startRow.ToString()].Value = p.Estimate.Services;
                                    ws.Cells["AA" + startRow.ToString()].Value = p.Estimate.Materials;

                                    ws.Cells["AB" + startRow.ToString()].Value = Tools.Div(p.Estimate.PercOfMaterialsLongLead, 100);
                                    ws.Cells["AC" + startRow.ToString()].Value = p.Estimate.LongLeadMonthRequired;
                                    ws.Cells["AD" + startRow.ToString()].Value = p.Estimate.TroubleFreeBeforeLC.Days;//NewTroubleFree.Days;
                                    ws.Cells["AE" + startRow.ToString()].Value = p.Estimate.NewLearningCurveFactor;// < 1 ? (p.Estimate.NewLearningCurveFactor * 100) : p.Estimate.NewLearningCurveFactor;
                                    ws.Cells["AF" + startRow.ToString()].Value = Tools.Div(p.Estimate.NewNPTTime.PercentDays, 100);
                                    ws.Cells["AG" + startRow.ToString()].Value = Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100);
                                    ws.Cells["AH" + startRow.ToString()].Value = Tools.Div(p.Estimate.NewTECOPTime.PercentDays, 100);
                                    ws.Cells["AI" + startRow.ToString()].Value = Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100);
                                    ws.Cells["AJ" + startRow.ToString()].Value = p.Estimate.WellValueDriver;
                                    ws.Cells["AK" + startRow.ToString()].Value = p.Estimate.DryHoleDays;
                                    ws.Cells["AL" + startRow.ToString()].Value = p.Estimate.TQValueDriver;
                                    ws.Cells["AM" + startRow.ToString()].Value = p.Estimate.BICValueDriver;
                                    ws.Cells["AN" + startRow.ToString()].Value = p.Estimate.PerformanceScore;
                                    ws.Cells["AO" + startRow.ToString()].Value = p.Estimate.WaterDepth;
                                    ws.Cells["AP" + startRow.ToString()].Value = p.Estimate.WellDepth;
                                    ws.Cells["AQ" + startRow.ToString()].Value = p.Estimate.NumberOfCasings;
                                    ws.Cells["AR" + startRow.ToString()].Value = p.Estimate.MechanicalRiskIndex;
                                    ws.Cells["AS" + startRow.ToString()].Value = p.Estimate.BrineDensity;
                                    ws.Cells["AT" + startRow.ToString()].Value = p.Estimate.CompletionInfo;
                                    ws.Cells["AU" + startRow.ToString()].Value = p.Estimate.CompletionType;
                                    ws.Cells["AV" + startRow.ToString()].Value = p.Estimate.NumberOfCompletionZones;

                                    startRow++;
                                    walkingRigName = p.Estimate.RigName;
                                }
                            }
                        }
                    }
                }
            }

            var newFileNameSingle = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                     String.Format("Business Plan-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss")));

            string returnName = String.Format("Business Plan-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss"));

            wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            return Json(new { Success = true, Path = returnName }, JsonRequestBehavior.AllowGet);
        }
        public FileResult DownloadFilteredTemplateFile(string stringName, DateTime date)
        {
            var res = Path.Combine(Server.MapPath("~/App_Data/Temp"), stringName);
            //rename file
            var retstringName = "BizPlanUploadTemplate-" + date.ToString("dd-MMM-yyyy-HHmmss") + ".xlsx";

            return File(res, Tools.GetContentType(".xlsx"), retstringName);
        }

        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase files)
        {
            ResultInfo ri = new ResultInfo();
            int fileSize = files.ContentLength;
            string fileName = files.FileName;
            string ContentType = files.ContentType;
            string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["BizPlanUploadPath"]);
            bool exists = System.IO.Directory.Exists(folder);

            string fileNameReplace = DateTime.Now.ToString("yyyMMddhhmmss") + "-" + fileName;

            if (!exists)
                System.IO.Directory.CreateDirectory(folder);
            string filepath = System.IO.Path.Combine(folder, fileNameReplace);
            List<string> ErrorMessage = new List<string>();
            try
            {

                files.SaveAs(filepath);
                CreateUserUpload(fileNameReplace);
                Thread.Sleep(TimeSpan.FromSeconds(1));

                ExtractionHelper e = new ExtractionHelper();
                IList<BsonDocument> datas = e.ExtractMax(filepath,MaxColumn:48);
                var countData = datas.Where(x=>x.GetString("Well_Name") !="").Count();
                var walkingCounter = 0;
                var mac = MacroEconomic.Populate<MacroEconomic>(q:null, fields:new string[] { "Currency" });
                var fcms = ProjectReferenceFactor.Populate<ProjectReferenceFactor>();
                var list = new List<string>();
                if (mac.Any())
                {
                    list = mac.Where(x => !String.IsNullOrEmpty(x.Currency)).Select(x => x.Currency).Distinct().ToList();
                }
                var StatusDefined = new List<string> { "draft", "complete", "modified" };
                var maturityDefined = new List<string> { "Type 0", "Type 1", "Type 2", "Type 3", "Type 4" };
                var counter = 1; int errorCounter = 0;
                foreach (var b in datas.Where(x=>x.GetString("Well_Name") !=""))
                {
                    counter++;
                    var proj = b.GetString("Project");
                    var rfmInput = b.GetString("Reference_Factor_Model");
                    var OP = b.GetString("Save_To_OP");
                    var OPStart = b.GetString("OP16_Start");
                    var wellName = b.GetString("Well_Name");
                    var activityType = b.GetString("Activity_Type");
                    var rigName = b.GetString("Rig_Name");
                    var rigSeqId = b.GetString("UA_Rig_Sequence_Id");
                    var currency = b.GetString("Currency");
                    var status = b.GetString("Status");
                    var region = b.GetString("Region");
                    var performance_unit = b.GetString("Performance_Unit");
                    var asset = b.GetString("Asset");
                    var country = b.GetString("Country");
                    var fundingType = b.GetString("Funding_Type");
                    var rigType = b.GetString("Rig_Type");
                    var operating_unit = b.GetString("Operating_Unit");
                    var maturity = b.GetString("Estimate_Maturity");
                    var troublefreetime = b.GetString("Trouble_Free_Time");
                    var q = Query.And(
                            Query.EQ("WellName", wellName),
                            Query.EQ("Phases.ActivityType", activityType),
                            Query.EQ("Phases.Estimate.RigName", rigName),
                            Query.EQ("UARigSequenceId", rigSeqId)
                        );
                    var bp = BizPlanActivity.Get<BizPlanActivity>(q);
                    var ckCurr = list.FirstOrDefault(x => x.ToLower() == currency.ToLower());
                    var fcm = fcms.FirstOrDefault(x => x.ProjectName.Equals(proj));
                    string rfm = null;
                    //check missing data
                    bool checkMissingData = true;
                    bool IsCorrect = true;
                    if (region == null || region =="") {
                        checkMissingData = false;
                        ErrorMessage.Add("Row "+counter+"- Region is blank. 'Missing Meta Data' Status will be assigned.");
                    }
                    if (performance_unit == null || performance_unit == "")
                    {
                        checkMissingData = false;
                        ErrorMessage.Add("Row " + counter + "- Performance unit is blank. 'Missing Meta Data' Status will be assigned.");
                    }
                    if (asset == null || asset=="")
                    {
                        checkMissingData = false;
                        ErrorMessage.Add("Row " + counter + "- Asset is blank. 'Missing Meta Data' Status will be assigned.");
                    }
                    if (proj == null || proj=="")
                    {
                        checkMissingData = false;
                        ErrorMessage.Add("Row " + counter + "- Project name is blank. 'Missing Meta Data' Status will be assigned.");
                    }

                    if (country == null || country=="")
                    {
                        checkMissingData = false;
                        ErrorMessage.Add("Row " + counter + "- Country is blank. 'Missing Meta Data' Status will be assigned.");
                    }

                    //if (fundingType == null || fundingType=="")
                    //{
                    //    checkMissingData = false;
                    //    ErrorMessage.Add("- Row " + counter + " Funding Type cannot be empty");
                    //}

                    //if (rigType == null || rigType == "")
                    //{
                    //    checkMissingData = false;
                    //    ErrorMessage.Add("- Row " + counter + " Rig Type cannot be empty");
                    //}

                    if (operating_unit == null || operating_unit == "")
                    {
                        checkMissingData = false;
                        ErrorMessage.Add("Row " + counter + "- Operating unit is blank. 'Missing Meta Data' Status will be assigned.");
                    }

                    if (rfmInput == null || rfmInput=="") {
                        checkMissingData = false;
                        ErrorMessage.Add("Row " + counter + "- Reference factor model is blank. 'Missing Meta Data' Status will be assigned.");
                    }

                    if (rfmInput != null && rfmInput.ToLower().Equals("default")) {
                        checkMissingData = false;
                        ErrorMessage.Add("Row " + counter + "' default' is not a valid RFM. This RFM will cause many cost calculations to be wrong. 'Missing Meta Data' Status will be assigned.");
                    }

                    //if (currency == null || currency == "") {
                    //    Ismissing = false;
                    //    ErrorMessage.Add("- Row " + counter + " Currency cannot be empty");
                    //}

                    //if (maturity == null || maturity == "") {
                    //    checkMissingData = false;
                    //    ErrorMessage.Add("- Row " + counter + " Estimate maturity cannot be empty");
                    //}

                    //if ((maturity !=null || maturity !="") && !maturityDefined.Contains(maturity))
                    //{
                    //    checkMissingData = false;
                    //    ErrorMessage.Add("- Row " + counter + " Estimate maturity that allowed are Type 0 or Type 1 or Type 2 or Type 3 or Type 4.");
                    //}


                    //if (troublefreetime == null || troublefreetime == "") {
                    //    checkMissingData = false;
                    //    ErrorMessage.Add("- Row " + counter + " Trouble free time cannot be empty.");
                    //}
                    //double myNum = 0;
                    //if ((troublefreetime != null || troublefreetime != "") && !Double.TryParse(troublefreetime, out myNum))
                    //{
                    //    checkMissingData = false;
                    //    ErrorMessage.Add("- Row " + counter + " Trouble free time cannot be string.");
                    //}

                    //if (checkMissingData)
                    //{
                        //if (fcm != null)
                        //{
                        //    if (fcm.ReferenceFactorModels.FirstOrDefault(x => x.ToLower() == rfmInput.ToLower()) != null)
                        //        rfm = rfmInput;
                        //    else
                        //    {
                        //        ErrorMessage.Add("- Row " + counter + " RFM " + rfmInput + " is not linked to Project " + proj);
                        //    }
                        //}
                        //else
                        //{
                        //    ErrorMessage.Add("- Row " + counter + " RFM " + rfmInput + " is not linked to Project " + proj);
                        //}

                        //check well
                        var qWell = BizPlanActivity.Get<BizPlanActivity>(Query.EQ("WellName",wellName));
                        if (qWell == null)
                        {
                            IsCorrect = false;
                            ErrorMessage.Add("- Row " + counter + " Well Name does not exist in system.");
                        }
                        //activity
                        var qAct = BizPlanActivity.Get<BizPlanActivity>(Query.EQ("Phases.ActivityType", activityType));
                        if (qAct == null)
                        {
                            IsCorrect = false;
                            ErrorMessage.Add("- Row " + counter + " Activity Type does not exist in system.");
                        }

                        //if (bp == null)
                        //{
                        //    ErrorMessage.Add("- Row " + counter + " has no Business Plan recorded.");
                        //}
                         
                        if (OP != "OP16")
                        {
                            ErrorMessage.Add("- Row " + counter + " is not OP16.");
                        }

                        if ((currency != null || currency != "") && ckCurr == null)
                        {
                            ErrorMessage.Add("- Row " + counter + " Currency does not exist in system.");
                        }

                    //}
                    
                    var IsStatus = false;
                    foreach (var sts in StatusDefined)
                    {
                        if (status.ToLower() == sts)
                        {
                            IsStatus = true;
                            break;
                        }
                    }
                    //if (!IsStatus)
                    //{
                    //    ErrorMessage.Add("- Row " + counter + " Status " + status + " doesn't allowed. Please use Draft, Complete or Modified");
                    //}
                    var checkOPStart = true;
                    if (OPStart.Trim() != "")
                    {
                        //var checkPlanStart =
                        //       Tools.ToUTC(DateTime.ParseExact(OPStart.Trim(), "yyyy-MM-dd hh:mm:ss",
                        //           System.Globalization.CultureInfo.InvariantCulture));
                        //if (checkPlanStart == null)
                        //{
                        //    ErrorMessage.Add("- Row " + counter + " OP Start is not valid.");
                        //    checkOPStart = false;
                        //}
                    }
                    else
                    {
                        ErrorMessage.Add("- Row " + counter + " OP Start is not valid.");
                        checkOPStart = false;
                    }

                    //if (checkMissingData && bp != null && ckCurr != null && OP == "OP16" && rfm != null && checkOPStart && bp.ReferenceFactorModel != "Default" && !String.IsNullOrEmpty(bp.ReferenceFactorModel))
                    if (IsCorrect && bp != null && ckCurr != null && OP == "OP16" && checkOPStart)
                        walkingCounter++;
                    if (checkMissingData == false)
                    {
                        errorCounter++;
                    }

                }

                if (errorCounter > 0)
                {
                    ri.Data = ErrorMessage;
                    ri.Result = "OK2";
                }

                if (countData != walkingCounter)
                {
                    System.IO.File.Delete(filepath);
                    ri.Result = "NOK";
                    ri.Data = ErrorMessage;
                    //throw new Exception("File refuse to upload. Some datas are not exist in the business plan or Currency is not in the list or not OP16.");
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

        public void CreateUserUpload(string filename)
        {
            var uploadStatus = UploadDataMaintenance.Get<UploadDataMaintenance>(q: Query.EQ("FileName", filename));
            if (uploadStatus == null)
            {
                string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["BizPlanUploadPath"]);
                string files = Directory.GetFiles(WebConfigurationManager.AppSettings["BizPlanUploadPath"]).Where(x => System.IO.Path.GetFileName(x).Equals(filename)).FirstOrDefault();
                string file = System.IO.Path.GetFileName(files);
                uploadStatus = new UploadDataMaintenance();
                uploadStatus.ListExecute = new List<ExecuteUpdate>();
                uploadStatus.Maintenance = "BizPlan";
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

            string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["BizPlanUploadPath"]);
            bool exists = System.IO.Directory.Exists(folder);
            if (!exists)
                return Json(new { Data = "", Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);

            string[] filePaths = Directory.GetFiles(WebConfigurationManager.AppSettings["BizPlanUploadPath"]);

            foreach (string file in filePaths)
            {
                BsonDocument doc = new BsonDocument();
                string filename = System.IO.Path.GetFileName(file);
                doc.Set("FileName", System.IO.Path.GetFileName(file));
                //doc.Set("LastWrite", System.IO.File.GetLastWriteTimeUtc(file));
                doc.Set("CreateDate", System.IO.File.GetCreationTimeUtc(file));

                var uploadStatus = UploadDataMaintenance.Get<UploadDataMaintenance>(q:Query.And(Query.EQ("FileName", filename), Query.EQ("Maintenance","BizPlan")));
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
            var uploadStatus = UploadDataMaintenance.Get<UploadDataMaintenance>(q: Query.And(Query.EQ("FileName", filename), Query.EQ("Maintenance", "BizPlan")));
            if (uploadStatus != null)
            {
                ExecuteUpdate update = new ExecuteUpdate() { Status = "Success", Message = "Business Plan Bulk upload complete", UserUpdate = WebTools.LoginUser.UserName, ExecuteDate = DateTime.Now };
                uploadStatus.ListExecute.Add(update);
                uploadStatus.Save();
            }
            else
            {
                CreateUserUpload(filename);
                updateexecute(filename);
            }
        }


        


        public JsonResult Execute(string id)
        {
            try
            {
                string dt = DateTime.Now.ToString("yyyyMMdd");
                DataHelper.Delete("_BizPlanMaster_" + dt);

                #region Extract XLS to Bsondoc

                ExtractionHelper e = new ExtractionHelper();
                string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["BizPlanUploadPath"]);
                string Path = folder + @"\" + id;

                IList<BsonDocument> datas = e.ExtractMax(Path, MaxColumn: 48);
                //IList<BsonDocument> datas = e.Extract(Path);
                int No = 0;

                foreach (var y in datas.Where(x=>x.GetString("Well_Name") !=""))
                {
                    No++;
                    y.Set("_id", No);
                    DataHelper.Save("_BizPlanMaster_" + dt, y);
                }

                #endregion

                List<string> errormsg = new List<string>();
                BizPlan.SyncFromUpload(datas.ToList(), out errormsg, WebTools.LoginUser.UserName);

                return Json(new { Data = "_BizPlanMaster_" + dt, Result = "OK", Message = "Load Biz Plan Done" }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message, Trace = ex.StackTrace }, JsonRequestBehavior.AllowGet);
            }
        }

        

        public JsonResult LoadResult(string tableName)
        {
            try
            {
                var t = DataHelper.Populate(tableName);
                return Json(new { Data = DataHelper.ToDictionaryArray(t).ToList(), Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

    }
}