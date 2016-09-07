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

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth(WEISRoles = "Administrators")]
    public class UploadOPController : Controller
    {
        //
        // GET: /Shell/UploadOP/
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
                string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["OPUploadPath"]);
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

            string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["OPUploadPath"]);
            bool exists = System.IO.Directory.Exists(folder);
            if (!exists)
                return Json(new { Data = "", Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);

            string[] filePaths = Directory.GetFiles(WebConfigurationManager.AppSettings["OPUploadPath"]);

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


        public List<BsonDocument> LoadOPWithConfiguration(Aspose.Cells.Workbook wb)
        {
            ECIS.Core.DataSerializer.ExtractionHelper e = new ExtractionHelper();
            ECIS.Core.DataSerializer.Entity.ExcelConfig conf = new Core.DataSerializer.Entity.ExcelConfig();
            ECIS.Core.DataSerializer.Entity.PositionTitle ps = new Core.DataSerializer.Entity.PositionTitle();

            #region Load to JSON FIle
            //string Path = @"D:\OFFICIAL OP14 WELLS Feb-2015.xlsx";
            //Aspose.Cells.Workbook wb = new Aspose.Cells.Workbook(Path);

            //string jsonstr = conf.ToJson();
            //using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"D:\JSONConfig.json", true))
            //{
            //    file.WriteLine(jsonstr);
            //}

            #endregion
            //string Path = @"D:\OFFICIAL OP14 WELLS Feb-2015.xlsx";
            // TOTAL_OP14_DURATION

            conf.Title = "OP Config First";
            #region Headers add
            ps.Names.Add("Region");
            ps.Position = "A3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add("Rig Type");
            //ps.Names.Add("RigType");
            ps.Names.Add("Rig_Type");
            ps.Position = "B3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add("Rig Name");
            //ps.Names.Add("RigName");
            ps.Names.Add("Rig_Name");
            ps.Position = "C3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add("Operating Unit");
            //ps.Names.Add("OperatingUnit");
            ps.Names.Add("Operating_Unit");
            ps.Position = "D3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add("PerformanceUnit");
            ps.Names.Add("Performance_Unit");
            //ps.Names.Add("Performance Unit");
            ps.Position = "E3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add("GMEngr");
            ps.Names.Add("GM_Engr");
            //ps.Names.Add("GM Engr");
            ps.Position = "F3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add("GMOps");
            //ps.Names.Add("GM Ops");
            ps.Names.Add("GM_Ops");
            ps.Position = "G3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add("Responsible Engineer");
            ps.Names.Add("Responsible_Engineer");
            //ps.Names.Add("ResponsibleEngineer");
            ps.Position = "H3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add("Asset Name");
            ps.Names.Add("Asset_Name");
            //ps.Names.Add("AssetName");
            ps.Position = "I3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            ps.Names.Add("Project_Name");
            //ps.Names.Add("ProjectName");
            //ps.Names.Add("Project Name");
            ps.Position = "J3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add("Well Name");
            ps.Names.Add("Well_Name");
            //ps.Names.Add("WellName");
            ps.Position = "K3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add("WorkingInterest");
            ps.Names.Add("Working_Interest");
            //ps.Names.Add("Working Interest");
            ps.Position = "L3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add("Firm or Option");
            ps.Names.Add("Firm_or_Option");
            //ps.Names.Add("FirmorOption");
            ps.Position = "M3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //            ps.Names.Add("ActivityTypePTWBuckets");
            //            ps.Names.Add(@"Activity Type
            //(PTW Buckets)");
            ps.Names.Add(@"Activity_Type\r\n(PTW_Buckets)");
            ps.Position = "N3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //            ps.Names.Add(@"Activity Desc.
            //(Rig Seq desc)");
            ps.Names.Add(@"Activity_Desc \r\n(Rig_Seq_desc)");
            //ps.Names.Add(@"Activity_DescRig_Seq_desc");
            //ps.Names.Add(@"ActivityDescRigSeqdesc");
            ps.Position = "O3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"Risk Flag");
            //ps.Names.Add(@"RiskFlag");
            ps.Names.Add(@"Risk_Flag");
            ps.Position = "P3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //            ps.Names.Add(@"ActivityDescPTWEstName");
            //            ps.Names.Add(@"Activity Desc.
            //(PTW Est name)");
            ps.Names.Add(@"Activity_Desc \r\n(PTW_Est_name)");
            ps.Position = "Q3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"UARigSequenceID");
            //ps.Names.Add(@"UA Rig Sequence ID");
            //ps.Names.Add(@"UA_Rig_Sequence_ID");
            //ps.Names.Add(@"UARigSequenceID");
            ps.Names.Add(@"ID");
            ps.Position = "R3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"UARigSequenceDescription");
            //ps.Names.Add(@"UA Rig Sequence Description");
            //ps.Names.Add(@"UA_Rig_Sequence_Description");
            //ps.Names.Add(@"UARigSequenceDescription");
            ps.Names.Add(@"Description");
            ps.Position = "S3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);



            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"Ops Dur");
            //ps.Names.Add(@"OpsDur");
            ps.Names.Add(@"Ops_Dur");
            //ps.Names.Add(@"OPS Sequence - Hicks Ops Dur");
            //ps.Names.Add(@"OPS_Sequence_-_Hicks_Ops_Dur");
            //ps.Names.Add(@"OPSSequenceHicksOpsDur");
            //ps.Names.Add(@"OPS_Sequence_Hicks_Ops_Dur");
            ps.Position = "T3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"Ops Start");
            //ps.Names.Add(@"OpsStart");
            ps.Names.Add(@"Ops_Start");
            //ps.Names.Add(@"OPS Sequence - Hicks Ops Start");
            //ps.Names.Add(@"OPS_Sequence_-_Hicks_Ops_Start");
            //ps.Names.Add(@"OPSSequenceHicksOpsStart");
            //ps.Names.Add(@"OPS_Sequence_Hicks_Ops_Start");
            ps.Position = "U3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"Ops Finish");
            //ps.Names.Add(@"OpsFinish");
            ps.Names.Add(@"Ops_Finish");
            //ps.Names.Add(@"OPS Sequence - Hicks Ops Finish");
            //ps.Names.Add(@"OPS_Sequence_-_Hicks_Ops_Finish");
            //ps.Names.Add(@"OPSSequenceHicksOpsFinish");
            //ps.Names.Add(@"OPS_Sequence_Hicks_Ops_Finish");
            ps.Position = "V3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);



            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"PS Start");
            ps.Names.Add(@"PS_Start");
            //ps.Names.Add(@"PSStart");
            //ps.Names.Add(@"Planning Sequence - TranPS Start");
            //ps.Names.Add(@"Planning_Sequence_-_TranPS_Start");
            //ps.Names.Add(@"PlanningSequence-TranPSStart");
            //ps.Names.Add(@"PlanningSequenceTranPSStart");
            ps.Position = "W3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"PS Finish");
            ps.Names.Add(@"PS_Finish");
            //ps.Names.Add(@"PSFinish");
            //ps.Names.Add(@"Planning Sequence - TranPS Finish");
            //ps.Names.Add(@"Planning_Sequence_-_TranPS_Finish");
            //ps.Names.Add(@"PlanningSequence-TranPSFinish");
            //ps.Names.Add(@"PlanningSequenceTranPSFinish");
            ps.Position = "X3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"PS Dur");
            ps.Names.Add(@"PS_Dur");
            //ps.Names.Add(@"PSDur");
            //ps.Names.Add(@"Planning Sequence - TranPS Dur");
            //ps.Names.Add(@"Planning_Sequence_-_TranPS_Dur");
            //ps.Names.Add(@"PlanningSequence-TranPSDur");
            //ps.Names.Add(@"PlanningSequenceTranPSDur");
            ps.Position = "Y3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            ps.Names.Add(@"Delta");
            //ps.Names.Add(@"Planning Sequence - TranDelta");
            //ps.Names.Add(@"Planning_Sequence_-_TranDelta");
            //ps.Names.Add(@"PlanningSequence-TranDelta");
            //ps.Names.Add(@"PlanningSequenceTranDelta");
            ps.Position = "Z3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"Ph Start");
            ps.Names.Add(@"Ph_Start");
            //ps.Names.Add(@"PhStart");
            //ps.Names.Add(@"Cost Phasing Seq. - SirmonPh Start");
            //ps.Names.Add(@"Cost_Phasing_Seq._-_SirmonPh_Start");
            //ps.Names.Add(@"CostPhasingSeqSirmonPhStart");
            ps.Position = "AA3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"Ph Finish");
            ps.Names.Add(@"Ph_Finish");
            //ps.Names.Add(@"PhFinish");
            //ps.Names.Add(@"Cost Phasing Seq. - SirmonPh Finish");
            //ps.Names.Add(@"Cost_Phasing_Seq._-_SirmonPh_Finish");
            //ps.Names.Add(@"CostPhasingSeqSirmonPhFinish");
            ps.Position = "AB3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"Dur Calc");
            ps.Names.Add(@"Dur_Calc");
            //ps.Names.Add(@"DurCalc");
            ps.Position = "AC3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);
            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"Dur Check");
            ps.Names.Add(@"Dur_Check");
            //ps.Names.Add(@"DurCheck");
            ps.Position = "AD3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"StartYearPl");
            ps.Names.Add(@"Start_Year_(Pl)");
            //ps.Names.Add(@"Start Year (Pl)");
            ps.Position = "AE3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"StartMonthPl");
            ps.Names.Add(@"Start_Month_(Pl)");
            //ps.Names.Add(@"Start Month (Pl)");
            ps.Position = "AF3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"FinishYearPl");
            ps.Names.Add(@"Finish_Year_(Pl)");
            //ps.Names.Add(@"Finish Year (Pl)");
            ps.Position = "AG3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"Finish Quarter/Year (Pl)");
            ps.Names.Add(@"Finish_Quarter/Year_(Pl)");
            //ps.Names.Add(@"Finish Quarter/Year (Pl)");
            //ps.Names.Add(@"FinishQuarterYearPl");
            ps.Position = "AH3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"StartYearOps");
            ps.Names.Add(@"Start_Year_(Ops)");
            //ps.Names.Add(@"Start Year (Ops)");
            ps.Position = "AI3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"StartMonthOps");
            ps.Names.Add(@"Start_Month_(Ops)");
            //ps.Names.Add(@"Start Month (Ops)");
            ps.Position = "AJ3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"FinishYearOps");
            ps.Names.Add(@"Finish_Year_(Ops)");
            //ps.Names.Add(@"Finish Year (Ops)");
            ps.Position = "AK3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"Finish Quarter/Year (Ops)");
            ps.Names.Add(@"Finish_Quarter/Year_(Ops)");
            //ps.Names.Add(@"Finish Quarter/Year (Ops)");
            //ps.Names.Add(@"FinishQuarterYearOps");
            ps.Position = "AL3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"FundingType");
            ps.Names.Add(@"Funding_Type");
            //ps.Names.Add(@"Funding Type");
            ps.Position = "AM3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"Grouping (Expl, Major Proj, Other)");
            ps.Names.Add(@"Grouping_(Expl,_Major_Proj,_Other)");
            //ps.Names.Add(@"Grouping(ExplMajorProjOther)");
            //ps.Names.Add(@"GroupingExplMajorProjOther");
            ps.Position = "AN3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"Escalation Group");
            //ps.Names.Add(@"EscalationGroup");
            ps.Names.Add(@"Escalation_Group");
            ps.Position = "AO3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"CSO Group");
            //ps.Names.Add(@"CSOGroup");
            ps.Names.Add(@"CSO_Group");
            ps.Position = "AP3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"Level of Estimate");
            ps.Names.Add(@"Level_of_Estimate");
            //ps.Names.Add(@"LevelofEstimate");
            ps.Position = "AQ3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"DURATIONTOTALDURATION");
            //ps.Names.Add(@"DURATION TOTAL DURATION");
            ps.Names.Add(@"DURATION_TOTAL_DURATION");
            ps.Position = "AR3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"DURATIONTroubleFree");
            //ps.Names.Add(@"DURATION Trouble Free");
            ps.Names.Add(@"DURATION_Trouble_Free");
            ps.Position = "AS3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);



            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"DURATION Trouble");
            //ps.Names.Add(@"DURATIONTrouble");
            ps.Names.Add(@"DURATION_Trouble");
            ps.Position = "AT3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"DURATION Contingency");
            //ps.Names.Add(@"DURATIONContingency");
            ps.Names.Add(@"DURATION_Contingency");
            ps.Position = "AU3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            ps.Names.Add(@"TQ_Duration");
            //ps.Names.Add(@"DURATION_TQ_Duration");
            //ps.Names.Add(@"DURATIONTQDuration");
            ps.Position = "AV3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"DURATION BIC Duration");
            ps.Names.Add(@"BIC_Duration");
            //ps.Names.Add(@"DURATIONBICDuration");
            ps.Position = "AW3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"DURATION LTA2 Duration");
            ps.Names.Add(@"LTA2_Duration");
            //ps.Names.Add(@"DURATIONLTA2Duration");
            ps.Position = "AX3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"DURATION New (Since LTA2)");
            ps.Names.Add(@"DURATION_New_(Since_LTA2)");
            //ps.Names.Add(@"DURATION_New_Since_LTA2");
            //ps.Names.Add(@"DURATIONNewSinceLTA2");
            ps.Position = "AY3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"BurnRate");
            ps.Names.Add(@"Burn_Rate");
            //ps.Names.Add(@"Burn Rate");
            ps.Position = "AZ3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"COSTTOTALCOST");
            //ps.Names.Add(@"COST TOTAL COST");
            ps.Names.Add(@"COST_TOTAL_COST");
            ps.Position = "BA3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"COSTTotalCostSS");
            ps.Names.Add(@"COST_Total_Cost_SS");
            //ps.Names.Add(@"COST Total Cost SS");
            ps.Position = "BB3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"COSTTroubleFree");
            //ps.Names.Add(@"COST Trouble Free");
            ps.Names.Add(@"COST_Trouble_Free");
            ps.Position = "BC3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"COSTTrouble");
            //ps.Names.Add(@"COST Trouble");
            ps.Names.Add(@"COST_Trouble");
            ps.Position = "BD3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"COSTContingency");
            //ps.Names.Add(@"COST Contingency");
            ps.Names.Add(@"COST_Contingency");
            ps.Position = "BE3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"COSTEscalationInflation");
            //ps.Names.Add(@"COST Escalation & Inflation");
            ps.Names.Add(@"COST_Escalation__Inflation");
            //ps.Names.Add(@"COSTEscalation&Inflation");
            ps.Position = "BF3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"COSTCSO");
            //ps.Names.Add(@"COST CSO");
            ps.Names.Add(@"COST_CSO");
            ps.Position = "BG3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"COST Total Cost (Incl Portfolio Risk)");
            ps.Names.Add(@"COST_Total_Cost_(Incl_Portfolio_Risk)");
            //ps.Names.Add(@"COSTTotalCost(InclPortfolioRisk)");
            //ps.Names.Add(@"COSTTotalCostInclPortfolioRisk");
            ps.Position = "BH3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"COST_LTA2_Cost");
            ps.Names.Add(@"COST_LTA2_Cost");
            //ps.Names.Add(@"COST LTA2 Cost");
            ps.Position = "BI3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"TQ Cost");
            //ps.Names.Add(@"TQCost");
            ps.Names.Add(@"TQ_Cost");
            ps.Position = "BJ3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"BIC Cost");
            ps.Names.Add(@"BIC_Cost");
            //ps.Names.Add(@"BICCost");
            ps.Position = "BK3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"LL Month");
            ps.Names.Add(@"LL_Month");
            //ps.Names.Add(@"LLMonth");
            ps.Position = "BL3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add(@"LLAmount");
            ps.Names.Add(@"LL_Amount");
            //ps.Names.Add(@"LL Amount");
            ps.Position = "BM3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            //            ps = new Core.DataSerializer.Entity.PositionTitle();
            //            ps.Names.Add(@"Cost
            //(Escalated 
            //& Inflated)");
            //            ps.Names.Add(@"Cost(Escalated_&_Inflated)");
            //            ps.Names.Add(@"CostEscalated&Inflated");
            //            ps.Names.Add(@"CostEscalatedInflated");
            //            ps.Position = "GP3";
            //            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            //            conf.Headers.Add(ps);

            //            ps = new Core.DataSerializer.Entity.PositionTitle();
            //            ps.Names.Add(@"Total Cost (With Esc, Infl, & CSO)");
            //            ps.Names.Add(@"Total_Cost_(With_Esc,_Infl,_&_CSO)");
            //            ps.Names.Add(@"TotalCost(WithEsc,Infl,&CSO)");
            //            ps.Names.Add(@"TotalCostWithEscInflCSO");
            //            ps.Position = "LS3";
            //            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            //            conf.Headers.Add(ps);

            #endregion
            string cellNameFirst = "A3"; //e.GetFirstDataCell(wb, "Rig Activities", "Region"); 
            string LastCell = "BM" + e.GetMaxRows(wb, "Rig Activities").ToString();  //e.GetLastDataCell(wb, "Rig Activities", "LL Amount");
            IList<BsonDocument> scope1 = e.ExtractWithConfiguration(wb, cellNameFirst, LastCell, 3, conf);

            conf = new Core.DataSerializer.Entity.ExcelConfig();
            conf.Title = "OP Config Second";
            #region Headers add
            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add("MEASURESTQ Duration");
            ps.Names.Add("MEASURES_TQ_Duration");
            //ps.Names.Add("MEASURESTQDuration");
            ps.Position = "QW3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add("MEASURESBIC Duration");
            ps.Names.Add("MEASURES_BIC_Duration");
            //ps.Names.Add("MEASURESBICDuration");
            ps.Position = "QX3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add("MEASURES TQ Cost");
            ps.Names.Add("MEASURES_TQ_Cost");
            //ps.Names.Add("MEASURESTQCost");
            ps.Position = "QY3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add("MEASURESBIC Cost");
            ps.Names.Add("MEASURES_BIC_Cost");
            //ps.Names.Add("MEASURESBICCost");
            ps.Position = "QZ3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);


            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add("MEASURESTQ/Target Per PIP?");
            ps.Names.Add("MEASURES_TQ/Target_Per_PIP");
            //ps.Names.Add("MEASURESTQTargetPerPIP");
            ps.Position = "RA3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            ps = new Core.DataSerializer.Entity.PositionTitle();
            //ps.Names.Add("MEASURESBIC Per PIP?");
            ps.Names.Add("MEASURES_BIC_Per_PIP?");
            //ps.Names.Add("MEASURESBICPerPIP");
            ps.Position = "RB3";
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            conf.Headers.Add(ps);

            #endregion
            string cellNameFirst1 = "QW3"; // e.GetFirstDataCell(wb, "Rig Activities", "TQ Duration", "BIC Per PIP?", 6);
            //string headerCell = e.CellValueUniqueToName(wb, "Rig Activities", "BIC Per PIP?");
            string LastCell1 = "RB" + e.GetMaxRows(wb, "Rig Activities").ToString();   // e.GetLastCellFromLastColumn(wb, "Rig Activities", headerCell);
            IList<BsonDocument> scope2 = e.ExtractWithConfiguration(wb, cellNameFirst1, LastCell1, 3, conf);

            List<BsonDocument> datas = new List<BsonDocument>();
            int i = 0;
            foreach (var d in scope1)
            {
                datas.Add(d.Merge(scope2[i]));
                i++;
            }

            return datas;
        }

        public JsonResult Execute(string id)
        {

            try
            {
                #region Extract XLS to Bsondoc
                ExtractionHelper e = new ExtractionHelper();

                string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["OPUploadPath"]);
                string Path = folder + @"\" + id;

                Aspose.Cells.Workbook wb = new Aspose.Cells.Workbook(Path);
                var datas = this.LoadOPWithConfiguration(wb);
                #endregion

                DateTime dateNow = DateTime.Now;
                dateNow = DateTime.Now;
                string nowDate = dateNow.ToString("yyyyMMdd");
                string nowDate2 = dateNow.ToString("yyyyMMddhhmm");

                #region Reset _OP_" + nowDate and save BsonDoc to _OP_ + nowDate

                DataHelper.Delete("_OP_" + nowDate);

                foreach (var Skema in datas)
                {
                    if (BsonHelper.GetString(Skema, "Well_Name").Equals("SILE - 1"))
                    {
                        Skema.Set("Well_Name", "SILE-1");
                    }

                    DataHelper.Save("_OP_" + nowDate, Skema);
                }

                #endregion

                #region Backup Current WellActivity to _WellActivity_" + nowDate2
                var currentDB = DataHelper.Populate("WEISWellActivities");
                foreach (var data in currentDB)
                {
                    DataHelper.Save("_WellActivity_" + nowDate2, data);
                }
                #endregion

                #region Preparing WEISWellActivities : Copy  _WellActivity_" + nowDate2 to WEISWellActivities
                DataHelper.Delete("WEISWellActivities");
                var NonOPs = DataHelper.Populate("_WellActivity_" + nowDate2, Query.EQ("NonOP", true));
                DataHelper.Save("WEISWellActivities", NonOPs);
                #endregion

                WellActivity.LoadOP("_OP_" + nowDate); // New OP 14 Data

                #region Load cek afe, actual dll from Previous where null
                var wellActvNew = DataHelper.Populate<WellActivity>("WEISWellActivities");  // New OP 14 Data WellActivity
                foreach (var t in wellActvNew)
                {
                    List<IMongoQuery> listMongo = new List<IMongoQuery>();
                    IMongoQuery q = Query.EQ("WellName", t.WellName);
                    listMongo.Add(q);
                    q = Query.EQ("UARigSequenceId", t.UARigSequenceId);
                    listMongo.Add(q);
                    #region Update new WellActivity data (actual, op, afe, le, lwe, PreviousWeek, LastWeek) with previous version
                    var prev = DataHelper.Populate<WellActivity>("_WellActivity_" + nowDate2, Query.And(listMongo));
                    if (prev != null && prev.Count > 0)
                    {
                        foreach (var y in t.Phases)
                        {
                            var ph = prev.FirstOrDefault().Phases.FirstOrDefault(x => x.ActivityType.Equals(y.ActivityType));
                            if (ph != null)
                            {
                                WellDrillData actual = ph.Actual == null ? new WellDrillData() : ph.Actual;
                                WellDrillData op = ph.OP == null ? new WellDrillData() : ph.OP;
                                WellDrillData afe = ph.AFE == null ? new WellDrillData() : ph.AFE;
                                WellDrillData le = ph.LE == null ? new WellDrillData() : ph.LE;
                                WellDrillData lwe = ph.LWE == null ? new WellDrillData() : ph.LWE;

                                y.PhaseNo = ph.PhaseNo;
                                y.Actual = actual;
                                y.OP = op;
                                y.AFE = afe;
                                if ((ph.LE.Days != 0) || (ph.LE.Cost != 0)) y.LE = le;
                                y.LWE = lwe;
                                y.PreviousWeek = ph.PreviousWeek;
                                y.LastWeek = ph.LastWeek;
                                y.PIPs = ph.PIPs;
                            }
                        }
                    }

                    #endregion
                    t.Save();
                }
                #endregion

                #region load last WR and update
                var wauKeys = WellActivityUpdate.Populate<WellActivityUpdate>()
                    .GroupBy(d => new { d.WellName, d.SequenceId, d.Phase.ActivityType })
                    .Select(d => new
                    {
                        Keys = d.Key,
                        UpdateVersion = d.Max(x => x.UpdateVersion)
                    })
                    .ToList();
                foreach (var wauKey in wauKeys)
                {
                    var wau = WellActivityUpdate.Get<WellActivityUpdate>(Query.And(
                            Query.EQ("WellName", wauKey.Keys.WellName),
                            Query.EQ("SequenceId", wauKey.Keys.SequenceId),
                            Query.EQ("Phase.ActivityType", wauKey.Keys.ActivityType),
                            Query.EQ("UpdateVersion", wauKey.UpdateVersion)
                        ));
                    if (wau != null)
                    {
                        wau.Calc();
                        wau.Save();
                    }
                }
                #endregion

                #region Update Reference Data
                var regions = wellActvNew.GroupBy(x => x.Region).Select(x => x.Key).ToList(); foreach (var r in regions) { DataHelper.Save("WEISRegions", new BsonDocument("_id", r)); }
                var rigTypes = wellActvNew.GroupBy(x => x.RigType).Select(x => x.Key).ToList(); foreach (var r in rigTypes) { DataHelper.Save("WEISRigTypes", new BsonDocument("_id", r)); }
                var rigNames = wellActvNew.GroupBy(x => x.RigName).Select(x => x.Key).ToList(); foreach (var r in rigNames) { DataHelper.Save("WEISRigNames", new BsonDocument("_id", r)); }
                var operatingUnits = wellActvNew.GroupBy(x => x.OperatingUnit).Select(x => x.Key).ToList(); foreach (var r in operatingUnits) { DataHelper.Save("WEISOperatingUnits", new BsonDocument("_id", r)); }
                var performanceUnits = wellActvNew.GroupBy(x => x.PerformanceUnit).Select(x => x.Key).ToList(); foreach (var r in performanceUnits) { DataHelper.Save("WEISPerformanceUnits", new BsonDocument("_id", r)); }
                var assetNames = wellActvNew.GroupBy(x => x.AssetName).Select(x => x.Key).ToList(); foreach (var r in assetNames) { DataHelper.Save("WEISAssetNames", new BsonDocument("_id", r)); }
                var projectNames = wellActvNew.GroupBy(x => x.ProjectName).Select(x => x.Key).ToList(); foreach (var r in projectNames) { DataHelper.Save("WEISProjectNames", new BsonDocument("_id", r)); }

                var wellactivitymasters = WellActivity.Populate<WellActivity>().GroupBy(x => x.WellName).Select(x => x.Key).ToList();
                var wellInfo = WellInfo.Populate<WellInfo>(Query.NE("IsVirtualWell", true)).GroupBy(x => x._id).Select(x => x.Key).ToList();
                foreach (var newWel in wellactivitymasters)
                {
                    if (!wellInfo.Contains(newWel))
                    {
                        // insert
                        WellInfo w = new WellInfo();
                        w._id = newWel;
                        w.Save(w.TableName);
                    }
                }

                #endregion

                return Json(new { Data = Path.ToString(), Result = "OK", Message = "Load WellActivity Done" }, JsonRequestBehavior.AllowGet);
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

                string backupname = "_WellActivity_" + DateTime.Now.ToString("yyyyMMddhhmm") + "_" + id;

                mongo.CreateCollection(backupname);
                var source = mongo.GetCollection("WEISWellActivities");
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
            string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["OPUploadPath"]);
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
                if (t.Contains("_WellActivity_"))
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

                mongo.DropCollection("WEISWellActivities");
                mongo.RenameCollection(id, "WEISWellActivities");

                return Json(new { Data = "", Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

    }
}