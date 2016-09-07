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
    public class UploadOP15Controller : Controller
    {
        private Core.DataSerializer.Entity.PositionTitle AddColumn(string Position, string Title)
        {
            var ps = new Core.DataSerializer.Entity.PositionTitle();
            ps.Names.Add(Title);
            ps.Position = Position;
            ps.Column = ExtractionHelper.SplitCell(ps.Position, true);
            return ps;
        }

        public JsonResult Extract()
        {
            var ri = new ResultInfo();

            try
            {
                string folder = System.IO.Path.Combine(@"C:\Users\novalagungprayogo\Desktop");
                string path = folder + @"\" + "DW MODU OP15 Bulk Template.xlsm";
                string tableName = "_OP15_" + DateTime.Now.ToString("yyyyMMdd");

                Aspose.Cells.Workbook wb = new Aspose.Cells.Workbook(path);

                ECIS.Core.DataSerializer.ExtractionHelper e = new ExtractionHelper();
                ECIS.Core.DataSerializer.Entity.ExcelConfig conf = new Core.DataSerializer.Entity.ExcelConfig();
                ECIS.Core.DataSerializer.Entity.PositionTitle ps = new Core.DataSerializer.Entity.PositionTitle();

                conf.Title = "OP15 Config First";

                conf.Headers.Add(AddColumn("C7", "LineOfBusiness"));
                conf.Headers.Add(AddColumn("D7", "Region"));
                conf.Headers.Add(AddColumn("E7", "OperatingUnit"));
                conf.Headers.Add(AddColumn("F7", "Asset"));
                conf.Headers.Add(AddColumn("G7", "Project"));
                conf.Headers.Add(AddColumn("H7", "Space1"));
                conf.Headers.Add(AddColumn("I7", "WellName"));
                conf.Headers.Add(AddColumn("J7", "ActivityType"));
                conf.Headers.Add(AddColumn("K7", "ActivityCategory"));
                conf.Headers.Add(AddColumn("L7", "ScopeDescription"));
                conf.Headers.Add(AddColumn("M7", "WellType"));
                conf.Headers.Add(AddColumn("N7", "DrillingNumberOfCasing"));
                conf.Headers.Add(AddColumn("O7", "OP15BaseSpreadRate"));
                conf.Headers.Add(AddColumn("P7", "OP16BaseBurnRate"));
                conf.Headers.Add(AddColumn("Q7", "MRI"));
                conf.Headers.Add(AddColumn("R7", "CompletionType"));
                conf.Headers.Add(AddColumn("S7", "NumberOfCompletionZone"));
                conf.Headers.Add(AddColumn("T7", "BrineDensity"));
                conf.Headers.Add(AddColumn("U7", "EstimatingRangeType"));
                conf.Headers.Add(AddColumn("V7", "DeterministicLowRange"));
                conf.Headers.Add(AddColumn("W7", "DeterministicHeight"));
                conf.Headers.Add(AddColumn("X7", "ProbabilisticP10"));
                conf.Headers.Add(AddColumn("Y7", "ProbabilisticP90"));
                conf.Headers.Add(AddColumn("Z7", "WaterDepthMD"));
                conf.Headers.Add(AddColumn("AA7", "TotalWellDepthMD"));
                conf.Headers.Add(AddColumn("AB7", "LearningCurveFactor"));
                conf.Headers.Add(AddColumn("AC7", "WorkingInterest"));
                conf.Headers.Add(AddColumn("AD7", "PlanningClassification"));
                conf.Headers.Add(AddColumn("AE7", "EstimateMaturity"));
                conf.Headers.Add(AddColumn("AF7", "FundingType"));
                conf.Headers.Add(AddColumn("AG7", "ReferenceFactorModel"));
                conf.Headers.Add(AddColumn("AH7", "Space2"));
                conf.Headers.Add(AddColumn("AI7", "RigName"));
                conf.Headers.Add(AddColumn("AJ7", "RigType"));
                conf.Headers.Add(AddColumn("AK7", "SequenceNumberOfRig"));
                conf.Headers.Add(AddColumn("AL7", "ActivityStart"));
                conf.Headers.Add(AddColumn("AM7", "ActivityEnd"));
                conf.Headers.Add(AddColumn("AN7", "TroubleFreeTime"));
                conf.Headers.Add(AddColumn("AO7", "NPTTimePercent"));
                conf.Headers.Add(AddColumn("AP7", "TECOPTimePercent"));
                conf.Headers.Add(AddColumn("AQ7", "OverrideFactors"));
                conf.Headers.Add(AddColumn("AR7", "NPTTime"));
                conf.Headers.Add(AddColumn("AS7", "TECOPTime"));
                conf.Headers.Add(AddColumn("AT7", "MeanTime"));
                conf.Headers.Add(AddColumn("AU7", "Space3"));
                conf.Headers.Add(AddColumn("AV7", "Space4"));
                conf.Headers.Add(AddColumn("AW7", "TroubleFreeCostOriginalCurrencyMillions"));
                conf.Headers.Add(AddColumn("AX7", "Currency"));
                conf.Headers.Add(AddColumn("AY7", "NPTCostPercent"));
                conf.Headers.Add(AddColumn("AZ7", "TECOPCostPercent"));
                conf.Headers.Add(AddColumn("BA7", "OverrideCostFactors"));
                conf.Headers.Add(AddColumn("BB7", "NPTCostOriginalCurrencyMillions"));
                conf.Headers.Add(AddColumn("BC7", "TECOPCostOriginalCurrencyMillions"));
                conf.Headers.Add(AddColumn("BD7", "MeanCostEDMOriginalCurrencyMillions"));
                conf.Headers.Add(AddColumn("BE7", "TroubleFreeCostUSDMillions"));
                conf.Headers.Add(AddColumn("BF7", "NPTCostUSDMillions"));
                conf.Headers.Add(AddColumn("BG7", "TECOPCostUSDMillions"));
                conf.Headers.Add(AddColumn("BH7", "MeanCostEDMUSDMillions"));
                conf.Headers.Add(AddColumn("BI7", "EscalationCostUSDMillions"));
                conf.Headers.Add(AddColumn("BJ7", "CSOCostUSDMillions"));
                conf.Headers.Add(AddColumn("BK7", "InflationCostUSDMillions"));
                conf.Headers.Add(AddColumn("BL7", "MeanCostMODUSDMillions"));
                conf.Headers.Add(AddColumn("BM7", "Space5"));
                conf.Headers.Add(AddColumn("BN7", "ProjectValueDriver"));
                conf.Headers.Add(AddColumn("BO7", "ValueDriverRelatedEstimate"));
                conf.Headers.Add(AddColumn("BP7", "TQThreshold"));
                conf.Headers.Add(AddColumn("BQ7", "BICThreshold"));
                conf.Headers.Add(AddColumn("BR7", "TQGap"));
                conf.Headers.Add(AddColumn("BS7", "BICGap"));
                conf.Headers.Add(AddColumn("BT7", "PerformanceScore"));
                conf.Headers.Add(AddColumn("BU7", "Space6"));
                conf.Headers.Add(AddColumn("BV7", "Status"));
                conf.Headers.Add(AddColumn("BW7", "Comments"));
                conf.Headers.Add(AddColumn("BX7", "Space7"));
                conf.Headers.Add(AddColumn("BY7", "EstimateMaturityOP14"));
                conf.Headers.Add(AddColumn("BZ7", "RigNameOP14"));
                conf.Headers.Add(AddColumn("CA7", "TroubleFreeTimeOP14"));
                conf.Headers.Add(AddColumn("CB7", "NPTTimeOP14"));
                conf.Headers.Add(AddColumn("CC7", "TECOPTimeOP14"));
                conf.Headers.Add(AddColumn("CD7", "MeanTimeOP14"));
                conf.Headers.Add(AddColumn("CE7", "TroubleFreeCostUSDOP14"));
                conf.Headers.Add(AddColumn("CF7", "NPTCostUSDOP14"));
                conf.Headers.Add(AddColumn("CG7", "TECOPCostUSDOP14"));
                conf.Headers.Add(AddColumn("CH7", "MeanCostUSDOP14"));

                var startCell = "C7";
                var finishCell = "CH" + e.GetMaxRows(wb, "Activity Estimates").ToString();

                var raw = e.ExtractWithConfiguration(wb, startCell, finishCell, 4, conf);

                DataHelper.Delete(tableName);

                raw.ToList().ForEach(d =>
                {
                    var id = string.Format("R{0}W{1}A{2}", BsonHelper.GetString(d, "RigName").Replace(" ", ""), BsonHelper.GetString(d, "WellName").Replace(" ", ""), BsonHelper.GetString(d, "ActivityType").Replace(" ", ""));
                    var el = new
                    {
                        _id = id,
                        LineOfBusiness = BsonHelper.GetString(d, "LineOfBusiness"),
                        Region = BsonHelper.GetString(d, "Region"),
                        OperatingUnit = BsonHelper.GetString(d, "OperatingUnit"),
                        Asset = BsonHelper.GetString(d, "Asset"),
                        Project = BsonHelper.GetString(d, "Project"),
                        WellName = BsonHelper.GetString(d, "WellName"),
                        ActivityType = BsonHelper.GetString(d, "ActivityType"),
                        ActivityCategory = BsonHelper.GetString(d, "ActivityCategory"),
                        ScopeDescription = BsonHelper.GetString(d, "ScopeDescription"),
                        WellType = BsonHelper.GetString(d, "WellType"),
                        DrillingNumberOfCasing = BsonHelper.GetDouble(d, "DrillingNumberOfCasing"),
                        OP15BaseSpreadRate = BsonHelper.GetDouble(d, "OP15BaseSpreadRate"),
                        OP16BaseBurnRate = BsonHelper.GetDouble(d, "OP16BaseBurnRate"),
                        MRI = BsonHelper.GetDouble(d, "MRI"),
                        CompletionType = BsonHelper.GetString(d, "CompletionType"),
                        NumberOfCompletionZone = BsonHelper.GetDouble(d, "NumberOfCompletionZone"),
                        BrineDensity = BsonHelper.GetDouble(d, "BrineDensity"),
                        EstimatingRangeType = BsonHelper.GetString(d, "EstimatingRangeType"),
                        DeterministicLowRange = Convert.ToDouble(BsonHelper.GetString(d, "DeterministicLowRange", "0").Replace("%", "")),
                        DeterministicHeight = Convert.ToDouble(BsonHelper.GetString(d, "DeterministicHeight", "0").Replace("%", "")),
                        ProbabilisticP10 = BsonHelper.GetDouble(d, "ProbabilisticP10"),
                        ProbabilisticP90 = BsonHelper.GetDouble(d, "ProbabilisticP90"),
                        WaterDepthMD = BsonHelper.GetDouble(d, "WaterDepthMD"),
                        TotalWellDepthMD = BsonHelper.GetDouble(d, "TotalWellDepthMD"),
                        LearningCurveFactor = Convert.ToDouble(BsonHelper.GetString(d, "LearningCurveFactor", "0").Replace("%", "")),
                        WorkingInterest = Convert.ToDouble(BsonHelper.GetString(d, "WorkingInterest", "0").Replace("%", "")),
                        PlanningClassification = BsonHelper.GetString(d, "PlanningClassification"),
                        EstimateMaturity = BsonHelper.GetString(d, "EstimateMaturity"),
                        FundingType = BsonHelper.GetString(d, "FundingType"),
                        ReferenceFactorModel = BsonHelper.GetString(d, "ReferenceFactorModel"),
                        RigName = BsonHelper.GetString(d, "RigName"),
                        RigType = BsonHelper.GetString(d, "RigType"),
                        SequenceNumberOfRig = BsonHelper.GetInt32(d, "SequenceNumberOfRig"),
                        ActivityStart = BsonHelper.GetDateTime(d, "ActivityStart"),
                        ActivityEnd = BsonHelper.GetDateTime(d, "ActivityEnd"),
                        TroubleFreeTime = BsonHelper.GetDouble(d, "TroubleFreeTime"),
                        NPTTimePercent = Convert.ToDouble(BsonHelper.GetString(d, "NPTTimePercent", "0").Replace("%", "")),
                        TECOPTimePercent = Convert.ToDouble(BsonHelper.GetString(d, "TECOPTimePercent", "0").Replace("%", "")),
                        OverrideFactors = BsonHelper.GetString(d, "OverrideFactors"),
                        NPTTime = BsonHelper.GetDouble(d, "NPTTime"),
                        TECOPTime = BsonHelper.GetDouble(d, "TECOPTime"),
                        MeanTime = BsonHelper.GetDouble(d, "MeanTime"),
                        TroubleFreeCostOriginalCurrencyMillions = BsonHelper.GetDouble(d, "TroubleFreeCostOriginalCurrencyMillions"),
                        Currency = BsonHelper.GetString(d, "Currency"),
                        NPTCostPercent = Convert.ToDouble(BsonHelper.GetString(d, "NPTCostPercent", "0").Replace("%", "")),
                        TECOPCostPercent = Convert.ToDouble(BsonHelper.GetString(d, "TECOPCostPercent", "0").Replace("%", "")),
                        OverrideCostFactors = BsonHelper.GetString(d, "OverrideCostFactors"),
                        NPTCostOriginalCurrencyMillions = BsonHelper.GetDouble(d, "NPTCostOriginalCurrencyMillions"),
                        TECOPCostOriginalCurrencyMillions = BsonHelper.GetDouble(d, "TECOPCostOriginalCurrencyMillions"),
                        MeanCostEDMOriginalCurrencyMillions = BsonHelper.GetDouble(d, "MeanCostEDMOriginalCurrencyMillions"),
                        TroubleFreeCostUSDMillions = BsonHelper.GetDouble(d, "TroubleFreeCostUSDMillions"),
                        NPTCostUSDMillions = BsonHelper.GetDouble(d, "NPTCostUSDMillions"),
                        TECOPCostUSDMillions = BsonHelper.GetDouble(d, "TECOPCostUSDMillions"),
                        MeanCostEDMUSDMillions = BsonHelper.GetDouble(d, "MeanCostEDMUSDMillions"),
                        EscalationCostUSDMillions = BsonHelper.GetDouble(d, "EscalationCostUSDMillions"),
                        CSOCostUSDMillions = BsonHelper.GetDouble(d, "CSOCostUSDMillions"),
                        InflationCostUSDMillions = BsonHelper.GetDouble(d, "InflationCostUSDMillions"),
                        MeanCostMODUSDMillions = BsonHelper.GetDouble(d, "MeanCostMODUSDMillions"),
                        ProjectValueDriver = BsonHelper.GetString(d, "ProjectValueDriver"),
                        ValueDriverRelatedEstimate = BsonHelper.GetDouble(d, "ValueDriverRelatedEstimate"),
                        TQThreshold = BsonHelper.GetDouble(d, "TQThreshold"),
                        BICThreshold = BsonHelper.GetDouble(d, "BICThreshold"),
                        TQGap = BsonHelper.GetDouble(d, "TQGap"),
                        BICGap = BsonHelper.GetDouble(d, "BICGap"),
                        PerformanceScore = BsonHelper.GetString(d, "PerformanceScore"),
                        Status = BsonHelper.GetString(d, "Status"),
                        Comments = BsonHelper.GetString(d, "Comments"),
                        EstimateMaturityOP14 = BsonHelper.GetString(d, "EstimateMaturityOP14"),
                        RigNameOP14 = BsonHelper.GetString(d, "RigNameOP14"),
                        TroubleFreeTimeOP14 = BsonHelper.GetDouble(d, "TroubleFreeTimeOP14"),
                        NPTTimeOP14 = BsonHelper.GetDouble(d, "NPTTimeOP14"),
                        TECOPTimeOP14 = BsonHelper.GetDouble(d, "TECOPTimeOP14"),
                        MeanTimeOP14 = BsonHelper.GetDouble(d, "MeanTimeOP14"),
                        TroubleFreeCostUSDOP14 = BsonHelper.GetDouble(d, "TroubleFreeCostUSDOP14"),
                        NPTCostUSDOP14 = BsonHelper.GetDouble(d, "NPTCostUSDOP14"),
                        TECOPCostUSDOP14 = BsonHelper.GetDouble(d, "TECOPCostUSDOP14"),
                        MeanCostUSDOP14 = BsonHelper.GetDouble(d, "MeanCostUSDOP14")
                    };

                    DataHelper.Save(tableName, el.ToBsonDocument());
                });
            }
            catch (Exception ex)
            {
                ri.PushException(ex);
            }

            return MvcTools.ToJsonResult(ri);
        }
	}
}