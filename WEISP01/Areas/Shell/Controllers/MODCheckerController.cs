using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Driver.Builders;


namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class MODCheckerController : Controller
    {
        //
        // GET: /Shell/MODChecker/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult SendEmailMODChecker(List<string> toMails, bool onlyExport = false)
        {
            return MvcResultInfo.Execute(() =>
            {
                var wb = new WaterfallBase();
                #region wb initiation
                wb.Activities = null;
                wb.ActivitiesCategory = null;
                wb.AllocationYear = 0;
                wb.BaseOP = "OP14";
                wb.CumulativeDataType = null;
                wb.Currency = null;
                wb.DataFor = null;
                wb.DateFinish = null;
                wb.DateFinish2 = null;
                wb.DateRelation = "OR";
                wb.DateStart = null;
                wb.DateStart2 = null;
                wb.DayOrCost = null;
                wb.ExType = null;
                wb.firmoption = "All";
                wb.FiscalYearFinish = 0;
                wb.FiscalYearStart = 0;
                wb.GetWhatData = null;
                wb.GroupBy = null;
                wb.IncludeCR = false;
                wb.IncludeGaps = false;
                wb.IncludeZero = false;
                wb.inlastuploadls = false;
                wb.MoneyType = null;
                wb.OperatingUnits = null;
                wb.opRelation = "AND";
                wb.OPs = null;
                wb.PerformanceUnits = null;
                wb.PeriodBase = "By Last Sequence";
                wb.PeriodView = "Project View";
                wb.ProjectNames = null;
                wb.Regions = null;
                wb.RigNames = null;
                wb.RigTypes = null;
                wb.ShellShare = false;
                wb.showdataby = "0";
                wb.SSorTG = null;
                wb.Status = null;
                wb.ValidLSOnly = false;
                wb.WellNames = null;
                #endregion

                //wb.WellNames  = new List<string>(){"APPO IE004"};

                var accessibleWells = WebTools.GetWellActivities();
                var email = WebTools.LoginUser.Email;
                var raw = wb.GetActivitiesForBizPlanForFiscalYear(email); //BizPlanActivity.Populate<BizPlanActivity>();

                var busplan = new BusplanController();
                var OPstate = busplan.GetMasterOP();
                string PreviousOP = "";
                string NextOP = "";
                string DefaultOP = busplan.getBaseOP(out PreviousOP, out NextOP);

                List<Excelbusplan> edetail = busplan.GenerateExportDetailData(raw, PreviousOP, accessibleWells, wb);
                string xlst = System.IO.Path.Combine(Server.MapPath("~/App_Data/Temp"), "BlankTemplate.xlsx");
                Aspose.Cells.Workbook wbook = new Aspose.Cells.Workbook(xlst);
                Aspose.Cells.Worksheet ws = wbook.Worksheets[0];
                ws.Name = "MOD Checker";
                Aspose.Cells.Worksheet ws2 = wbook.Worksheets[1];
                ws2.Name = "EDM Checker";
                Aspose.Cells.Cells sheet1 = ws.Cells;
                Aspose.Cells.Cells sheet2 = ws2.Cells;

                sheet1["A1"].PutValue("Well Name");
                sheet1["B1"].PutValue("Activity Type");
                sheet1["C1"].PutValue("Rig Name");
                sheet1["D1"].PutValue("MOD in Export Detail");
                sheet1["E1"].PutValue("MOD in Grid Export");
                sheet1["F1"].PutValue("MOD in BPIT");
                sheet1["G1"].PutValue("MOD in FYV");
                sheet1["H1"].PutValue("isDifferent?");
                sheet1["I1"].PutValue("Reason");

                sheet2["A1"].PutValue("Well Name");
                sheet2["B1"].PutValue("Activity Type");
                sheet2["C1"].PutValue("Rig Name");
                sheet2["D1"].PutValue("EDM in Export Detail");
                sheet2["E1"].PutValue("EDM in Grid Export");
                sheet2["F1"].PutValue("EDM in BPIT");
                sheet2["G1"].PutValue("EDM in FYV");
                sheet2["H1"].PutValue("isDifferent?");
                //sheet2["I1"].PutValue("Reason");

                var rownum = 2;

                foreach (var r in edetail)
                {
                    if (r.WellName == "APPO IE004" && r.ActivityType == "WHOLE DRILLING EVENT")
                    {

                    }
                    sheet1["A" + rownum.ToString()].PutValue(r.WellName);
                    sheet1["B" + rownum.ToString()].PutValue(r.ActivityType);
                    sheet1["C" + rownum.ToString()].PutValue(r.RigName);
                    sheet1["D" + rownum.ToString()].PutValue(r.MeanCostMOD);

                    sheet2["A" + rownum.ToString()].PutValue(r.WellName);
                    sheet2["B" + rownum.ToString()].PutValue(r.ActivityType);
                    sheet2["C" + rownum.ToString()].PutValue(r.RigName);
                    sheet2["D" + rownum.ToString()].PutValue(r.MeanCostEDM);


                    var maturityLevel = "0";
                    if (r.MaturityLevel != null)
                    {
                        maturityLevel = r.MaturityLevel.Substring(r.MaturityLevel.Length - 1, 1);
                    }

                    BizPlanSummary res = new BizPlanSummary(r.WellName, r.RigName, r.ActivityType, r.Country,
                    r.ShellShare, r.Event, Convert.ToInt32(maturityLevel), r.Services, r.Materials, r.TroubleFree.Days,
                    r.RFM,
                    Tools.Div(r.NPT.PercentTime, r.NPT.PercentTime <= 1 ? 1 : 100), Tools.Div(r.TECOP.PercentTime, r.TECOP.PercentTime <= 1 ? 1 : 100),
                    Tools.Div(r.NPT.PercentCost, r.NPT.PercentCost <= 1 ? 1 : 100), Tools.Div(r.TECOP.PercentCost, r.TECOP.PercentCost <= 1 ? 1 : 100),
                    r.LLRequiredMonth, Tools.Div(r.PercOfMaterialsLongLead,r.PercOfMaterialsLongLead <= 1 ? 1 : 100), baseOP: r.SaveToOP, isGetExcRateByCurrency: true,
                    lcf: r.LCFParameter
                    );

                    res.GeneratePeriodBucket();
                    res.GenerateAnnualyBucket(res.MonthlyBuckets);

                    sheet1["E" + rownum.ToString()].PutValue(res.MeanCostMOD);
                    sheet1["F" + rownum.ToString()].PutValue(res.MeanCostMOD);

                    sheet2["E" + rownum.ToString()].PutValue(res.MeanCostEDM);
                    sheet2["F" + rownum.ToString()].PutValue(res.MeanCostEDM);

                    var ModFYV = 0.0;
                    var EDMFYV = 0.0;
                    var Reason = "";

                    var bp = raw.Where(x => x.WellName != null && x.WellName == r.WellName && x.UARigSequenceId != null && x.UARigSequenceId == r.RigSeqId && x.RigName != null && x.RigName == r.RigName).ToList();
                    if (bp != null && bp.Count > 0) {
                        var newbp = new BizPlanActivity();
                        newbp = BsonHelper.Deserialize<BizPlanActivity>(bp.FirstOrDefault().ToBsonDocument());
                        var phases = newbp.Phases.Where(x => x.ActivityType == r.ActivityType).ToList();
                        newbp.Phases = phases;
                        var bps = new List<BizPlanActivity>() { newbp };
                        var periodBase = "annualy";
                        var resFYV = BizPlanAllocation.TransformSummaryBizAllocationToBson(bps, periodBase, null);
                        if (resFYV != null)
                        {
                            var getMODFYV = resFYV.Where(x => x.GetString("Title") == "MeanCostMOD").FirstOrDefault();
                            var getEDMFYV = resFYV.Where(x => x.GetString("Title") == "MeanCostEDM").FirstOrDefault();
                            ModFYV = getMODFYV.GetDouble("TotalAllYears");
                            EDMFYV = getEDMFYV.GetDouble("TotalAllYears");
                        }
                    }

                    sheet1["G" + rownum.ToString()].PutValue(ModFYV);
                    sheet2["G" + rownum.ToString()].PutValue(EDMFYV);

                    var diff = "";
                    if ((r.MeanCostMOD - res.MeanCostMOD > 1) || (r.MeanCostMOD - res.MeanCostMOD < -1))
                    {
                        diff = "Different";
                    }

                    if ((r.MeanCostMOD - ModFYV > 1) || (r.MeanCostMOD - ModFYV < -1))
                    {
                        diff = "Different";

                        //check status
                        Reason = "Status is : " + r.Status;
                    }
                    sheet1["H" + rownum.ToString()].PutValue(diff);
                    sheet1["I" + rownum.ToString()].PutValue(Reason);


                    var diff2 = "";
                    var Reason2 = "";
                    if ((r.MeanCostEDM - res.MeanCostEDM > 1) || (r.MeanCostEDM - res.MeanCostEDM < -1))
                    {
                        diff2 = "Different";
                    }

                    if ((r.MeanCostEDM - EDMFYV > 1) || (r.MeanCostEDM - EDMFYV < -1))
                    {
                        diff2 = "Different";

                        //check status
                        Reason2 = "Status is : " + r.Status;
                    }
                    sheet2["H" + rownum.ToString()].PutValue(diff2);
                    //sheet2["I" + rownum.ToString()].PutValue(Reason2);

                    rownum++;
                }
                var filename = String.Format("MOD_Checker-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-HHmmss"));

                var newFileNameSingle = System.IO.Path.Combine(Server.MapPath("~/App_Data/Temp"), filename);

                wbook.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

                if (!onlyExport)
                {
                    //var toMails = new List<string>() { "yoga.bangkit@eaciit.com", "raulie@eaciit.com", "Ian.pirsch@eaciit.com", "akash.jain@eaciit.com" };
                    var attachments = new List<string> { newFileNameSingle };
                    ECIS.Client.WEIS.Email.SendEmailMODChecker("MODDiffChecker", toMails.ToArray(), ccMails: null, variables: null, attachments: attachments, developerModeEmail: "eky.pradhana@eaciit.com");
                }

                return filename;
            });
        }

        public FileResult DownloadModCheckerFile(string stringName)
        {
            var res = System.IO.Path.Combine(Server.MapPath("~/App_Data/Temp"), stringName);
            return File(res, Tools.GetContentType(".xlsx"), stringName);
        }

	}
}