using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MongoDB;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
using System.Configuration;
using System.Linq.Expressions;

using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Collections.ObjectModel;
using System.Net;
using System.Globalization;
using Newtonsoft.Json;
using System.Collections;
using MongoDB.Bson;
using ECIS.Core;
using System.IO;

namespace ECIS.Client.WEIS
{
    /// <summary>
    /// Instant class and 
    /// Call : 
    /// after _OP15_20160114 : Already Extracted in Database
    /// LoadOP15();
    /// MergeMasterData();
    /// </summary>
    public class OP15
    {


        public static void InitiateAllToOP14()
        {
            var pips = WellPIP.Populate<WellPIP>();

            List<string> ignoiring = new List<string>();
            ignoiring.Add("ignorewau");
            ignoiring.Add("ignoreresetcrelement");

            foreach (var pip in pips)
            {
                foreach (var ele in pip.Elements)
                {
                    ele.AssignTOOps = new List<string>();
                    ele.AssignTOOps.Add("OP14");
                }
                pip.Save(references: ignoiring.ToArray());
            }
        }

        public static List<OPHelper> GetAllHistoriesValue()
        {
            var alldata = WellActivity.Populate<WellActivity>();

            List<OPHelper> result = new List<OPHelper>();
            foreach (var a in alldata)
            {
                foreach (var p in a.Phases)
                {
                    OPHelper op = new OPHelper();
                    op.WellName = a.WellName;
                    op.RigName = a.RigName;
                    op.ActivityType = p.ActivityType;
                    op.SequenceId = a.UARigSequenceId;
                    op.PhaseNo = p.PhaseNo;

                    if (p.BaseOP.Any())
                    {
                        if (p.BaseOP.Count() > 1)
                        {
                            // cek hist
                            foreach (var bo in p.BaseOP)
                            {
                                ValueHelper val = new ValueHelper();
                                if (a.OPHistories.Where(x => x.Type.Equals(bo.ToString())).Any())
                                {
                                    var opHis = a.OPHistories.Where(x => x.Type.Equals(bo.ToString())).FirstOrDefault();
                                    val.Schedule = opHis.PlanSchedule;
                                    val.Type = bo;
                                    val.Value = opHis.Plan;
                                    op.Values.Add(val);
                                }
                                if (bo.Equals("OP15"))
                                {
                                    val.Schedule = p.PlanSchedule;
                                    val.Type = bo;
                                    val.Value = p.Plan;
                                    op.Values.Add(val);
                                }
                            }
                        }
                        else
                        {
                            ValueHelper val = new ValueHelper();
                            val.Schedule = p.PlanSchedule;
                            val.Type = p.BaseOP.FirstOrDefault();
                            val.Value = p.Plan;
                            op.Values.Add(val);
                        }
                    }
                    else
                    {
                        ValueHelper val = new ValueHelper();
                        val.Schedule = p.PlanSchedule;
                        val.Value = p.Plan;
                        op.Values.Add(val);
                    }
                    result.Add(op);
                }
            }
            return result;
        }
        public static void GenerateLogWRComparisonLatestMonth(DateRange daterange, string wrStatus = "In-Progress", string FIleName = @"D:\OP15WeeklyReportComparison.csv")
        {
            var latestmonthly = WellActivityUpdate.GetLatestMonthly(daterange, wrStatus);
            WriteLog(new string[] { "STATUS", "WellName", "ActivityType", "RigName in Current WR", "RigName in OP15", "Last WR Status" }, FIleName);

            foreach (var t in latestmonthly)
            {
                IMongoQuery mq = Query.And(
                                    Query.EQ("WellName", t.WellName),
                                    Query.EQ("Phases.ActivityType", t.Phase.ActivityType)
                                    );
                string rigName = ""; ;
                var actv = WellActivity.Get<WellActivity>(mq, sort: SortBy.Descending("UpdateVersion"));
                var actvs = WellActivity.Populate<WellActivity>(mq);
                if (actvs.Count() > 1)
                {
                    var match = actvs.Where(x => x.RigName.Equals(t.WorkUnit));
                    if (match.Any())
                    {
                        rigName = match.FirstOrDefault().RigName;
                    }
                    else
                    {
                        rigName = actv.RigName;
                    }
                }
                else
                {
                    rigName = actv.RigName;
                }

                IMongoQuery mq2 = Query.And(
                                    Query.EQ("Well_Name", t.WellName),
                                    Query.EQ("Activity_Type", t.Phase.ActivityType),
                                    Query.EQ("Rig_Name", rigName)
                                    );
                var y = DataHelper.Populate("_OP15_20160114", mq2);


                if (y.Any())
                {
                    // Weekly Report ada di OP 15
                    WriteLog(new string[] { "WEEKLY REPORT FOUND in OP15", t.WellName, t.Phase.ActivityType, rigName, y.FirstOrDefault().GetString("Rig_Name"), t.Status }, FIleName);
                }
                else
                {
                    // Weekly Report tidak ada di OP 15
                    IMongoQuery mq3 = Query.And(
                                  Query.EQ("Well_Name", t.WellName),
                                  Query.EQ("Activity_Type", t.Phase.ActivityType)
                                  );
                    var data3 = DataHelper.Populate("_OP15_20160114", mq3);

                    if (data3.Any())
                    {
                        var rigFoundInOP15 = data3.FirstOrDefault().GetString("Rig_Name");
                        WriteLog(new string[] { "WEEKLY REPORT FOUND in OP15 WITH DIFFERENT RIG NAME", t.WellName, t.Phase.ActivityType, rigName, rigFoundInOP15 }, FIleName);
                    }
                    else
                    {
                        WriteLog(new string[] { "WEEKLY REPORT NOT FOUND in OP15", t.WellName, t.Phase.ActivityType, rigName }, FIleName);
                    }

                }
            }
        }

        public static void WriteLog(string[] message, string filePath = "")
        {
            string logDir = "";
            if (filePath.Trim().Equals(""))
                logDir = ConfigurationManager.AppSettings["LogFile"];
            else
            {
                if (!File.Exists(filePath))
                {
                    var t = File.Create(filePath);
                    t.Close();
                }
                logDir = filePath;

            }
            if (!File.Exists(@logDir))
            {
                var t = File.Create(logDir);
                t.Close();
            }
            string mm = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

            for (var i = 0; i <= message.Count() - 1; i++)
            {
                mm = mm + "," + message[i];
            }

            StreamWriter sw = File.AppendText(logDir);
            sw.WriteLine(mm);
            sw.Close();

        }
        public static bool isHaveWeeklyReport(string well, string seq, string events)
        {
            var t = WellActivityUpdate.Populate<WellActivityUpdate>(
                Query.And(
                    Query.EQ("WellName", well),
                    Query.EQ("SequenceId", seq),
                    Query.EQ("Phase.ActivityType", events)
                )
                );
            if (t != null && t.Count() > 0)
                return true;
            return false;
        }
        public static bool isHaveMonthlyLEReport(string well, string seq, string events)
        {
            var t = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(
                Query.And(
                    Query.EQ("WellName", well),
                    Query.EQ("SequenceId", seq),
                    Query.EQ("Phase.ActivityType", events)
                )
                );
            if (t != null && t.Count() > 0)
            {
                var lastMonthly = t.OrderByDescending(x => x.UpdateVersion).FirstOrDefault();
                if (lastMonthly.Phase.IsActualLE == true)
                    return true;
                else
                    return false;
            }
            return false;
        }

        public static void UpdatePhaseInfo(WellActivity availWP, BsonDocument op15)
        {
            var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
            #region Update Phase Info
            //WellActivityPhaseInfo pi = WellActivityPhaseInfo.Get<WellActivityPhaseInfo>(
            //    Query.And(
            //        Query.EQ(
            //        "WellActivityId", availWP._id.ToString()
            //        ),
            //        Query.EQ(
            //        "WellName", availWP.WellName
            //        ),
            //        Query.EQ(
            //        "ActivityType", op15.GetString("Activity_Type")
            //        ),
            //        Query.EQ(
            //        "RigName", availWP.RigName
            //        ),
            //        Query.EQ(
            //        "SequenceId", availWP.UARigSequenceId
            //        ),
            //        Query.EQ(
            //        "OPType", "OP15"
            //        )
            //    )
            //    );
            //if (pi == null)
            //{
            WellActivityPhaseInfo pi = new WellActivityPhaseInfo();
            pi.WellActivityId = availWP._id.ToString();
            pi.WellName = availWP.WellName;
            pi.RigName = availWP.RigName;
            pi.SequenceId = availWP.UARigSequenceId;
            pi.ActivityType = op15.GetString("Activity_Type");
            //}
            pi.OPType = "OP15";
            pi.LineOfBusiness = op15.GetString("Line_of_Business");
            pi.ActivityCategory = op15.GetString("Activity_Category");
            pi.SpreadRate = op15.GetDouble("OP15_Base_Spread_Rate_($k/Day)");
            pi.BurnRate = op15.GetDouble("OP15_Base_Burn_Rate_($k/Day)");
            pi.MRI = op15.GetDouble("MRI");
            pi.CompletionType = op15.GetString("Completion_Type");
            pi.CompletionZone = op15.GetInt32("#_of_Completion_Zones");
            pi.BrineDensity = op15.GetDouble("Brine_Density_(ppg)");
            pi.EstimatingRangeType = op15.GetString("Estimating_Range_Type");
            pi.DeterministicLowRange = op15.GetDouble("Deterministic_Low_Range");
            pi.DeterministicHigh = op15.GetDouble("Deterministic_High");
            pi.ProbabilisticP10 = op15.GetDouble("Probabilistic_P10");
            pi.ProbabilisticP90 = op15.GetDouble("Probabilistic_P90");
            pi.WaterDepthMD = op15.GetDouble("Water_Depth_MD_(ft)");
            pi.TotalWellDepthMD = op15.GetDouble("Total_Well_Depth_MD_(ft)");
            pi.LearningCurveFactor = op15.GetDouble("Learning_Curve_Factor");
            pi.MaturityLevel = maturityLevel;
            pi.ReferenceFactorModel = op15.GetString("Reference_Factor_Model");
            pi.RigSequenceId = op15.GetInt32("Sequence_#_\r\non_Rig");

            pi.TroubleFree.Days = op15.GetDouble("Trouble_Free_\r\nTime");
            pi.NPT.PercentTime = op15.GetDouble("NPT_\r\nTime_%");
            pi.TECOP.PercentTime = op15.GetDouble("TECOP_\r\nTime_%");
            pi.OverrideFactor.Time = op15.GetBool("Override_Time_Factors");
            pi.NPT.Days = op15.GetDouble("NPT_\r\nTime");
            pi.TECOP.Days = op15.GetDouble("TECOP_\r\nTime");
            pi.Mean.Days = op15.GetDouble("Mean_\r\nTime");

            pi.TroubleFree.Cost = op15.GetDouble("Trouble_Free_\r\nCost_\r\n(Original_Currency,_\r\nMillions)");
            pi.Currency = op15.GetString("Currency");

            pi.NPT.PercentCost = op15.GetDouble("NPT_\r\nCost_%");
            pi.TECOP.PercentCost = op15.GetDouble("TECOP_\r\nCost_%");
            pi.OverrideFactor.Cost = op15.GetBool("Override_Cost_Factors");

            pi.NPT.Cost = op15.GetDouble("NPT_Cost_(Original_Currency,_Millions)");
            pi.TECOP.Cost = op15.GetDouble("TECOP_Cost_(Original_Currency,_Millions)");

            pi.MeanCostEDM.Cost = op15.GetDouble("Mean_Cost_EDM__(Original_Currency,_Millions)");
            pi.USDCost.TroubleFree = op15.GetDouble("Trouble_Free\r\nCost_USD_(Millions)");
            pi.USDCost.NPT = op15.GetDouble("NPT_\r\nCost_USD_(Millions)");
            pi.USDCost.TECOP = op15.GetDouble("NPT_TECOP_\r\nCost_USD_(Millions)");
            pi.USDCost.MeanCostEDM = op15.GetDouble("Mean_Cost_EDM_USD_(Millions)");
            pi.USDCost.Escalation = op15.GetDouble("Escalation_\r\nCost_USD_(Millions)");
            pi.USDCost.CSO = op15.GetDouble("CSO_\r\nCost_USD_(Millions)");
            pi.USDCost.Inflation = op15.GetDouble("Inflation_\r\nCost_USD_(Millions)");
            pi.USDCost.MeanCostMOD = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)");

            pi.ProjectValueDriver = op15.GetString("Project_Value_Driver");
            pi.ValueDriverEstimate = op15.GetInt32("Value_Driver_Related_Estimate");

            pi.TQTreshold = op15.GetInt32("TQ_Threshold\r\n");
            pi.BICTreshold = op15.GetInt32("BIC_Threshold\r\n");
            pi.TQGap = op15.GetInt32("TQ_Gap");
            pi.BICGap = op15.GetInt32("BIC_Gap");
            pi.PerformanceScore = op15.GetString("Performance_\r\nScore");
            pi.Save();
            #endregion
        }

        public static void ProcessSingle(List<BsonDocument> singles, string tableName = "_OP15_20160204")
        {
            foreach (var t in singles)
            {
                var wn = t.GetString("_id.Well_Name");
                var rn = t.GetString("_id.Rig_Name");
                var at = t.GetString("_id.Activity_Type");

                var data = DataHelper.Populate(tableName,
                     Query.And(
                     Query.EQ("Well_Name", wn),
                     Query.EQ("Rig_Name", rn),
                     Query.EQ("Activity_Type", at)
                     ));

                ProcessToWellPlan(data.FirstOrDefault());
            }
        }


        public static void DirectlyCreateNewWellPlan(List<BsonDocument> op15s)
        {
            var op15 = op15s.FirstOrDefault();
            #region Totally new WellPlan
            // create new well Plan, [but take information except RIGNAME]
            // add new well + phase
            WellActivity wellActivity = new WellActivity();
            wellActivity.Region = op15.GetString("Region");
            wellActivity.OperatingUnit = op15.GetString("Operating_Unit");
            wellActivity.PerformanceUnit = op15.GetString("Performance_Unit");
            wellActivity.AssetName = op15.GetString("Asset");
            wellActivity.ProjectName = op15.GetString("Project");
            wellActivity.WellName = op15.GetString("Well_Name");
            wellActivity.RigName = op15.GetString("Rig_Name");
            wellActivity.RigType = op15.GetString("Rig_Type");
            wellActivity.FirmOrOption = op15.GetString("Planning_\r\nClassification");
            wellActivity.WorkingInterest = op15.GetDouble("Shell_\r\nWorking_\r\nInterest");

            if ((wellActivity.UARigSequenceId == null ? "" : wellActivity.UARigSequenceId).Trim().Equals(""))
            {
                string aggregateCond1 = "{ $group: { _id: '$_id', maxSequenceId: { $max: '$UARigSequenceId' }}}";
                string aggregateCond2 = "{ $limit: 1 }";
                List<BsonDocument> pipes = new List<BsonDocument>();
                pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
                pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
                var number = String.Format("UA{0}", ECIS.Core.SequenceNo.Get("UARigSequenceId").ClaimAsInt());
                wellActivity.UARigSequenceId = number;
            }

            wellActivity.Phases = new List<WellActivityPhase>();
            int phaseno = 1;
            foreach (var ox in op15s)
            {
                WellActivityPhase phase = new WellActivityPhase();
                bool isShiftFutureDays = ox.GetString("Sequence_#_\r\non_Rig").Trim().Equals("-1") ? true : false;
                int SequenceOnRig = ox.GetInt32("Sequence_#_\r\non_Rig");//.Trim().Equals("-1") ? true : false;
                phase.SequenceOnRig = SequenceOnRig;

                phase.ActivityType = ox.GetString("Activity_Type");
                phase.PhaseNo = phaseno;
                phase.BaseOP.Add("OP15");
                phase.ShiftFutureEventDate = isShiftFutureDays;
                var meanDays = ox.GetDouble("Mean_\r\nTime");
                phase.PlanSchedule = new DateRange(Tools.ToUTC(ox.GetDateTime("Activity_Start")), Tools.ToUTC(ox.GetDateTime("Activity_End")));
                phase.Plan = new WellDrillData()
                {
                    Cost = ox.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000,
                    Days = meanDays//(Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays
                };

                phase.ActivityDesc = ox.GetString("Scope_Description");
                var maturityLevel = ox.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                phase.LevelOfEstimate = maturityLevel;
                phase.FundingType = ox.GetString("Funding_Type");
                wellActivity.Phases.Add(phase);
                wellActivity.Save();
                UpdatePhaseInfo(wellActivity, op15);
                WriteLog(new string[] { "TOTALLY NEW WELL - DOUBLE", phase.ActivityType, wellActivity.WellName, wellActivity.RigName, op15.GetString("_id") });
                phaseno++;
            }
            #endregion
        }

        static List<string> _sequenceIDs { get; set; }

        public static void ProcessToWellPlan(BsonDocument op15)
        {
            var wn = op15.GetString("Well_Name");
            var rn = op15.GetString("Rig_Name");
            var at = op15.GetString("Activity_Type");

            bool isShiftFutureDays = op15.GetString("Sequence_#_\r\non_Rig").Trim().Equals("-1") ? true : false;
            int SequenceOnRig = op15.GetInt32("Sequence_#_\r\non_Rig");
            WellActivity availWP = new WellActivity();
            availWP = WellActivity.Get<WellActivity>(Query.And(Query.EQ("WellName", wn), Query.EQ("RigName", rn), Query.EQ("Phases.ActivityType", at)));
            if (availWP != null)
            {
                // Phase Exist in Current Database
                var op15Actv = op15.GetString("Activity_Type");
                #region additional Update Field
                availWP.Region = op15.GetString("Region");
                availWP.OperatingUnit = op15.GetString("Operating_Unit");
                availWP.PerformanceUnit = op15.GetString("Performance_Unit");
                availWP.AssetName = op15.GetString("Asset");
                availWP.ProjectName = op15.GetString("Project");
                availWP.EXType = op15.GetString("Funding_Type");
                #endregion
                if (availWP.Phases.Where(x => x.ActivityType.Equals(op15Actv)).Any())
                {
                    #region UPDATE PHASE
                    var proPhase = availWP.Phases.Where(x => x.ActivityType.Equals(op15Actv)).FirstOrDefault();

                    proPhase.PlanSchedule = new DateRange(Tools.ToUTC(op15.GetDateTime("Activity_Start")), Tools.ToUTC(op15.GetDateTime("Activity_End")));
                    var meanDays = op15.GetDouble("Mean_\r\nTime");
                    proPhase.Plan.Days = meanDays; //Tools.ToUTC(op15.GetDateTime("Activity_Start")).AddDays(meanDays); // (Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays;
                    proPhase.Plan.Cost = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000;
                    if (!isHaveWeeklyReport(availWP.WellName, availWP.UARigSequenceId, proPhase.ActivityType))
                    {
                        proPhase.LE.Cost = 0;
                        proPhase.LE.Days = 0;
                        proPhase.LESchedule = new DateRange();
                    }
                    //proPhase.OP = new WellDrillData();
                    //proPhase.PhSchedule = new DateRange();
                    proPhase.CalcOP = new WellDrillData();
                    proPhase.CalcOPSchedule = new DateRange();


                    #region additional Update Field
                    proPhase.ActivityDesc = op15.GetString("Scope_Description");
                    var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                    proPhase.LevelOfEstimate = maturityLevel;
                    proPhase.FundingType = op15.GetString("Funding_Type");
                    proPhase.BaseOP.Add("OP15");
                    proPhase.ShiftFutureEventDate = isShiftFutureDays;
                    proPhase.SequenceOnRig = SequenceOnRig;
                    #endregion
                    availWP.Save();

                    UpdatePhaseInfo(availWP, op15);
                    WriteLog(new string[] { "UPDATE PHASE ", op15Actv, wn, rn, op15.GetString("_id") });
                    #endregion
                }
            }
            else
            {
                // cek WellName dan RigName
                availWP = WellActivity.Get<WellActivity>(Query.And(Query.EQ("WellName", wn), Query.EQ("RigName", rn)));
                if (availWP != null)
                {
                    // Well and Rig Available in Current DB but doesnt have Phase
                    #region ADD new Phase
                    var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                    var meanDays = op15.GetDouble("Mean_\r\nTime");

                    var proPhase = new WellActivityPhase()
                    {
                        PlanSchedule = new DateRange(Tools.ToUTC(op15.GetDateTime("Activity_Start")), Tools.ToUTC(op15.GetDateTime("Activity_End"))),
                        Plan = new WellDrillData()
                        {
                            Cost = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000,
                            Days = meanDays// (Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays
                        },
                        ActivityType = at,
                        PhaseNo = (availWP.Phases.Count == 0 ? 0 : availWP.Phases.Max(x => x.PhaseNo)) + 1,
                        ActivityDesc = op15.GetString("Scope_Description"),
                        LevelOfEstimate = maturityLevel,
                        FundingType = op15.GetString("Funding_Type")
                    };
                    proPhase.BaseOP.Add("OP15");
                    proPhase.ShiftFutureEventDate = isShiftFutureDays;
                    proPhase.SequenceOnRig = SequenceOnRig;

                    availWP.Phases.Add(proPhase);
                    availWP.Save();

                    UpdatePhaseInfo(availWP, op15);
                    #endregion
                    WriteLog(new string[] { "ADD NEW IN Existing Well And RIG ", at, wn, rn, op15.GetString("_id") });
                }
                else
                {
                    // cek WellName 
                    var availWPs = WellActivity.Populate<WellActivity>(Query.EQ("WellName", wn));
                    if (availWPs.Any())
                    {
                        // WellName Same // Indicated with Different Rig
                        var oldWell = availWPs.Where(p => p.Phases.Where(x => x.ActivityType.Equals(at) &&
                            x.PhSchedule.Start != Tools.DefaultDate
                            //&&
                            //x.BaseOP.Equals("OP14") // cek hanya Phase yg LS valid dan 
                            ).Count() > 0).FirstOrDefault();

                        var yyy = availWPs.SelectMany(x => x.Phases).Where(x => x.ActivityType.Equals(at) && x.PhSchedule.Start != Tools.DefaultDate);
                        if (yyy.Any())
                        {
                            #region WellName is Exist in Production Data but With Different Rig Name
                            // create new well Plan, [but take information except RIGNAME]
                            var wellOld = yyy.FirstOrDefault();

                            WellActivity wellActivity = new WellActivity();
                            wellActivity.Region = op15.GetString("Region");
                            wellActivity.OperatingUnit = op15.GetString("Operating_Unit");
                            wellActivity.PerformanceUnit = op15.GetString("Performance_Unit");
                            wellActivity.AssetName = op15.GetString("Asset");
                            wellActivity.ProjectName = op15.GetString("Project");
                            wellActivity.WellName = op15.GetString("Well_Name");
                            wellActivity.RigName = op15.GetString("Rig_Name");
                            wellActivity.RigType = op15.GetString("Rig_Type");
                            wellActivity.FirmOrOption = op15.GetString("Planning_\r\nClassification");
                            wellActivity.WorkingInterest = op15.GetDouble("Shell_\r\nWorking_\r\nInterest");

                            if ((wellActivity.UARigSequenceId == null ? "" : wellActivity.UARigSequenceId).Trim().Equals(""))
                            {
                                string aggregateCond1 = "{ $group: { _id: '$_id', maxSequenceId: { $max: '$UARigSequenceId' }}}";
                                string aggregateCond2 = "{ $limit: 1 }";
                                List<BsonDocument> pipes = new List<BsonDocument>();
                                pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
                                pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
                                var number = String.Format("UA{0}", ECIS.Core.SequenceNo.Get("UARigSequenceId").ClaimAsInt());
                                wellActivity.UARigSequenceId = number;
                            }

                            wellActivity.Phases = new List<WellActivityPhase>();

                            WellActivityPhase phase = new WellActivityPhase();

                            phase.ActivityType = op15.GetString("Activity_Type");
                            var chekckPhase = WellActivity.Get<WellActivity>(Query.And(
                                Query.EQ("WellName", op15.GetString("Well_Name")),
                                Query.EQ("RigName", op15.GetString("Rig_Name"))
                                ));

                            phase.PhaseNo = chekckPhase == null ? 1 : (chekckPhase.Phases.Any() ? chekckPhase.Phases.Max(x => x.PhaseNo) : 1);

                            phase.PlanSchedule = new DateRange(Tools.ToUTC(op15.GetDateTime("Activity_Start")), Tools.ToUTC(op15.GetDateTime("Activity_End")));
                            var meanDays = op15.GetDouble("Mean_\r\nTime");

                            phase.Plan = new WellDrillData()
                            {

                                Cost = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000,
                                Days = meanDays //(Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays
                            };
                            phase.ActivityDesc = op15.GetString("Scope_Description");
                            var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                            phase.LevelOfEstimate = maturityLevel;
                            phase.FundingType = op15.GetString("Funding_Type");
                            phase.BaseOP.Add("OP15");
                            phase.ShiftFutureEventDate = isShiftFutureDays;
                            phase.SequenceOnRig = SequenceOnRig;


                            wellActivity.Phases.Add(phase);
                            // add phase info

                            // MAKE ZERO LS in Prev, based on WELLNAME and ACTIVITY TYPE

                            // history

                            oldWell.Save();
                            // LS lama di copy kan ke baru
                            wellActivity.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().OP =
                                oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().OP;

                            // LS Schedule Lama di copykan ke baru
                            wellActivity.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().PhSchedule =
                                oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().PhSchedule;

                            // LS dan LS Schedule Lama reset
                            oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().OP.Days = 0;
                            oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().PhSchedule = new DateRange();

                            // Calc OP Lama Copykan Ke Baru
                            wellActivity.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().CalcOP =
                                oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().CalcOP;

                            // Calc OP Schedule Lama Copykan Ke Baru
                            wellActivity.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().CalcOPSchedule =
                                oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().CalcOPSchedule;

                            wellActivity.Save();
                            oldWell.Save();

                            #region Update Phase Info
                            var pi = WellActivityPhaseInfo.Get<WellActivityPhaseInfo>(
                                Query.And(
                                    Query.EQ(
                                    "WellActivityId", wellActivity._id.ToString()
                                    ),
                                    Query.EQ(
                                    "WellName", wellActivity.WellName
                                    ),
                                    Query.EQ(
                                    "ActivityType", at
                                    ),
                                    Query.EQ(
                                    "RigName", wellActivity.RigName
                                    ),
                                    Query.EQ(
                                    "SequenceId", wellActivity.UARigSequenceId
                                    ),
                                    Query.EQ(
                                    "OPType", "OP15"
                                    )
                                )
                                );
                            if (pi == null)
                            {
                                pi = new WellActivityPhaseInfo();
                                pi.WellActivityId = wellActivity._id.ToString();
                                pi.WellName = wellActivity.WellName;
                                pi.RigName = wellActivity.RigName;
                                pi.SequenceId = wellActivity.UARigSequenceId;
                                pi.ActivityType = wellActivity.ActivityType;
                            }
                            pi.OPType = "OP15";
                            pi.LineOfBusiness = op15.GetString("Line_of_Business");
                            pi.ActivityCategory = op15.GetString("Activity_Category");
                            pi.SpreadRate = op15.GetDouble("OP15_Base_Spread_Rate_($k/Day)");
                            pi.BurnRate = op15.GetDouble("OP15_Base_Burn_Rate_($k/Day)");
                            pi.MRI = op15.GetDouble("MRI");
                            pi.CompletionType = op15.GetString("Completion_Type");
                            pi.CompletionZone = op15.GetInt32("#_of_Completion_Zones");
                            pi.BrineDensity = op15.GetDouble("Brine_Density_(ppg)");
                            pi.EstimatingRangeType = op15.GetString("Estimating_Range_Type");
                            pi.DeterministicLowRange = op15.GetDouble("Deterministic_Low_Range");
                            pi.DeterministicHigh = op15.GetDouble("Deterministic_High");
                            pi.ProbabilisticP10 = op15.GetDouble("Probabilistic_P10");
                            pi.ProbabilisticP90 = op15.GetDouble("Probabilistic_P90");
                            pi.WaterDepthMD = op15.GetDouble("Water_Depth_MD_(ft)");
                            pi.TotalWellDepthMD = op15.GetDouble("Total_Well_Depth_MD_(ft)");
                            pi.LearningCurveFactor = op15.GetDouble("Learning_Curve_Factor");
                            pi.MaturityLevel = maturityLevel;
                            pi.ReferenceFactorModel = op15.GetString("Reference_Factor_Model");
                            pi.RigSequenceId = op15.GetInt32("Sequence_#_\r\non_Rig");

                            pi.TroubleFree.Days = op15.GetDouble("Trouble_Free_\r\nTime");
                            pi.NPT.PercentTime = op15.GetDouble("NPT_\r\nTime_%");
                            pi.TECOP.PercentTime = op15.GetDouble("TECOP_\r\nTime_%");
                            pi.OverrideFactor.Time = op15.GetBool("Override_Time_Factors");
                            pi.NPT.Days = op15.GetDouble("NPT_\r\nTime");
                            pi.TECOP.Days = op15.GetDouble("TECOP_\r\nTime");
                            pi.Mean.Days = op15.GetDouble("Mean_\r\nTime");

                            pi.TroubleFree.Cost = op15.GetDouble("Trouble_Free_\r\nCost_\r\n(Original_Currency,_\r\nMillions)");
                            pi.Currency = op15.GetString("Currency");

                            pi.NPT.PercentCost = op15.GetDouble("NPT_\r\nCost_%");
                            pi.TECOP.PercentCost = op15.GetDouble("TECOP_\r\nCost_%");
                            pi.OverrideFactor.Cost = op15.GetBool("Override_Cost_Factors");

                            pi.NPT.Cost = op15.GetDouble("NPT_Cost_(Original_Currency,_Millions)");
                            pi.TECOP.Cost = op15.GetDouble("TECOP_Cost_(Original_Currency,_Millions)");

                            pi.MeanCostEDM.Cost = op15.GetDouble("Mean_Cost_EDM__(Original_Currency,_Millions)");
                            pi.USDCost.TroubleFree = op15.GetDouble("Trouble_Free\r\nCost_USD_(Millions)");
                            pi.USDCost.NPT = op15.GetDouble("NPT_\r\nCost_USD_(Millions)");
                            pi.USDCost.TECOP = op15.GetDouble("NPT_TECOP_\r\nCost_USD_(Millions)");
                            pi.USDCost.MeanCostEDM = op15.GetDouble("Mean_Cost_EDM_USD_(Millions)");
                            pi.USDCost.Escalation = op15.GetDouble("Escalation_\r\nCost_USD_(Millions)");
                            pi.USDCost.CSO = op15.GetDouble("CSO_\r\nCost_USD_(Millions)");
                            pi.USDCost.Inflation = op15.GetDouble("Inflation_\r\nCost_USD_(Millions)");
                            pi.USDCost.MeanCostMOD = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)");

                            pi.ProjectValueDriver = op15.GetString("Project_Value_Driver");
                            pi.ValueDriverEstimate = op15.GetInt32("Value_Driver_Related_Estimate");

                            pi.TQTreshold = op15.GetInt32("TQ_Threshold\r\n");
                            pi.BICTreshold = op15.GetInt32("BIC_Threshold\r\n");
                            pi.TQGap = op15.GetInt32("TQ_Gap");
                            pi.BICGap = op15.GetInt32("BIC_Gap");
                            pi.PerformanceScore = op15.GetString("Performance_\r\nScore");

                            pi.Save();
                            #endregion
                            //WriteLog(new string[] { "RIG MOVING", t.GetString("Activity_Type"), keyWell, keyRig, t.GetString("_id"), prowel.FirstOrDefault().RigName });

                            #endregion
                            WriteLog(new string[] { "RIG MOVING", at, wn, rn, op15.GetString("_id"), pi.RigName });

                        }
                        else
                        {
                            #region New well because no Valid LS
                            WellActivity wellActivity = new WellActivity();
                            wellActivity.Region = op15.GetString("Region");
                            wellActivity.OperatingUnit = op15.GetString("Operating_Unit");
                            wellActivity.PerformanceUnit = op15.GetString("Performance_Unit");
                            wellActivity.AssetName = op15.GetString("Asset");
                            wellActivity.ProjectName = op15.GetString("Project");
                            wellActivity.WellName = op15.GetString("Well_Name");
                            wellActivity.RigName = op15.GetString("Rig_Name");
                            wellActivity.RigType = op15.GetString("Rig_Type");
                            wellActivity.FirmOrOption = op15.GetString("Planning_\r\nClassification");
                            wellActivity.WorkingInterest = op15.GetDouble("Shell_\r\nWorking_\r\nInterest");

                            if ((wellActivity.UARigSequenceId == null ? "" : wellActivity.UARigSequenceId).Trim().Equals(""))
                            {
                                string aggregateCond1 = "{ $group: { _id: '$_id', maxSequenceId: { $max: '$UARigSequenceId' }}}";
                                string aggregateCond2 = "{ $limit: 1 }";
                                List<BsonDocument> pipes = new List<BsonDocument>();
                                pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
                                pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
                                var number = String.Format("UA{0}", ECIS.Core.SequenceNo.Get("UARigSequenceId").ClaimAsInt());
                                wellActivity.UARigSequenceId = number;
                            }

                            wellActivity.Phases = new List<WellActivityPhase>();

                            WellActivityPhase phase = new WellActivityPhase();

                            phase.ActivityType = op15.GetString("Activity_Type");
                            var chekckPhase = WellActivity.Get<WellActivity>(Query.And(
                                Query.EQ("WellName", op15.GetString("Well_Name")),
                                Query.EQ("RigName", op15.GetString("Rig_Name"))
                                ));

                            phase.PhaseNo = chekckPhase == null ? 1 : (chekckPhase.Phases.Any() ? chekckPhase.Phases.Max(x => x.PhaseNo) : 1);
                            phase.SequenceOnRig = SequenceOnRig;

                            phase.PlanSchedule = new DateRange(Tools.ToUTC(op15.GetDateTime("Activity_Start")), Tools.ToUTC(op15.GetDateTime("Activity_End")));
                            var meanDays = op15.GetDouble("Mean_\r\nTime");

                            phase.Plan = new WellDrillData()
                            {
                                Cost = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000,
                                Days = meanDays// (Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays
                            };
                            phase.ActivityDesc = op15.GetString("Scope_Description");
                            var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                            phase.LevelOfEstimate = maturityLevel;
                            phase.FundingType = op15.GetString("Funding_Type");
                            phase.BaseOP.Add("OP15");
                            phase.ShiftFutureEventDate = isShiftFutureDays;

                            wellActivity.Phases.Add(phase);
                            wellActivity.Save();
                            UpdatePhaseInfo(wellActivity, op15);
                            WriteLog(new string[] { "RIG MOVING - new WELL Plan because no Valid LS ", at, wn, rn, op15.GetString("_id") });
                            #endregion
                        }
                    }
                    else
                    {
                        #region Totally new WellPlan
                        // create new well Plan, [but take information except RIGNAME]
                        // add new well + phase
                        WellActivity wellActivity = new WellActivity();
                        wellActivity.Region = op15.GetString("Region");
                        wellActivity.OperatingUnit = op15.GetString("Operating_Unit");
                        wellActivity.PerformanceUnit = op15.GetString("Performance_Unit");
                        wellActivity.AssetName = op15.GetString("Asset");
                        wellActivity.ProjectName = op15.GetString("Project");
                        wellActivity.WellName = op15.GetString("Well_Name");
                        wellActivity.RigName = op15.GetString("Rig_Name");
                        wellActivity.RigType = op15.GetString("Rig_Type");
                        wellActivity.FirmOrOption = op15.GetString("Planning_\r\nClassification");
                        wellActivity.WorkingInterest = op15.GetDouble("Shell_\r\nWorking_\r\nInterest");

                        if ((wellActivity.UARigSequenceId == null ? "" : wellActivity.UARigSequenceId).Trim().Equals(""))
                        {
                            string aggregateCond1 = "{ $group: { _id: '$_id', maxSequenceId: { $max: '$UARigSequenceId' }}}";
                            string aggregateCond2 = "{ $limit: 1 }";
                            List<BsonDocument> pipes = new List<BsonDocument>();
                            pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
                            pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
                            var number = String.Format("UA{0}", ECIS.Core.SequenceNo.Get("UARigSequenceId").ClaimAsInt());
                            wellActivity.UARigSequenceId = number;
                        }

                        wellActivity.Phases = new List<WellActivityPhase>();

                        int phaseno = 1;
                        WellActivityPhase phase = new WellActivityPhase();

                        phase.ActivityType = op15.GetString("Activity_Type");
                        phase.PhaseNo = phaseno;
                        phase.BaseOP.Add("OP15");
                        phase.ShiftFutureEventDate = isShiftFutureDays;

                        phase.PlanSchedule = new DateRange(Tools.ToUTC(op15.GetDateTime("Activity_Start")), Tools.ToUTC(op15.GetDateTime("Activity_End")));
                        var meanDays = op15.GetDouble("Mean_\r\nTime");
                        phase.SequenceOnRig = SequenceOnRig;

                        phase.Plan = new WellDrillData()
                        {
                            Cost = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000,
                            Days = meanDays//(Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays
                        };

                        phase.ActivityDesc = op15.GetString("Scope_Description");
                        var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                        phase.LevelOfEstimate = maturityLevel;
                        phase.FundingType = op15.GetString("Funding_Type");
                        wellActivity.Phases.Add(phase);
                        wellActivity.Save();
                        UpdatePhaseInfo(wellActivity, op15);
                        WriteLog(new string[] { "TOTALLY NEW WELL ", at, wn, rn, op15.GetString("_id") });
                        #endregion
                    }
                }
            }
        }

        public static void ProcessToWellPlanDouble(BsonDocument op15)
        {
            var wn = op15.GetString("Well_Name");
            var rn = op15.GetString("Rig_Name");
            var at = op15.GetString("Activity_Type");

            bool isShiftFutureDays = op15.GetString("Sequence_#_\r\non_Rig").Trim().Equals("-1") ? true : false;

            WellActivity availWP = new WellActivity();
            availWP = WellActivity.Get<WellActivity>(Query.And(
                Query.EQ("WellName", wn),
                Query.EQ("RigName", rn),
                Query.EQ("Phases.ActivityType", at)
                ));
            if (availWP != null)
            {
                #region additional Update Field
                availWP.Region = op15.GetString("Region");
                availWP.OperatingUnit = op15.GetString("Operating_Unit");
                availWP.PerformanceUnit = op15.GetString("Performance_Unit");
                availWP.AssetName = op15.GetString("Asset");
                availWP.ProjectName = op15.GetString("Project");
                availWP.EXType = op15.GetString("Funding_Type");
                #endregion
                var op15Actv = op15.GetString("Activity_Type");
                // cek apakah Phase
                if (availWP.Phases.Where(x => x.ActivityType.Equals(op15Actv) &&
                    x.PhSchedule.Start != Tools.DefaultDate && // valid ls
                    !x.BaseOP.Contains("OP15")).Any())
                {
                    // hanya yang tidak contain OP15
                    // update Phase
                    #region UPDATE PHASE
                    var proPhase = availWP.Phases.Where(x => x.ActivityType.Equals(op15Actv) &&
                          x.PhSchedule.Start != Tools.DefaultDate && // valid ls
                    !x.BaseOP.Contains("OP15")).FirstOrDefault();

                    proPhase.PlanSchedule = new DateRange(Tools.ToUTC(op15.GetDateTime("Activity_Start")), Tools.ToUTC(op15.GetDateTime("Activity_End")));
                    var meanDays = op15.GetDouble("Mean_\r\nTime");
                    proPhase.Plan.Days = meanDays; // (Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays;
                    proPhase.Plan.Cost = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000;
                    if (!isHaveWeeklyReport(availWP.WellName, availWP.UARigSequenceId, proPhase.ActivityType))
                    {
                        proPhase.LE.Cost = 0;
                        proPhase.LE.Days = 0;
                        proPhase.LESchedule = new DateRange();
                    }
                    //proPhase.OP = new WellDrillData();
                    //proPhase.PhSchedule = new DateRange();
                    proPhase.CalcOP = new WellDrillData();
                    proPhase.CalcOPSchedule = new DateRange();

                    #region additional Update Field
                    proPhase.ActivityDesc = op15.GetString("Scope_Description");
                    var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                    proPhase.LevelOfEstimate = maturityLevel;
                    proPhase.FundingType = op15.GetString("Funding_Type");
                    proPhase.BaseOP.Add("OP15");
                    proPhase.ShiftFutureEventDate = isShiftFutureDays;
                    #endregion
                    availWP.Save();
                    UpdatePhaseInfo(availWP, op15);
                    WriteLog(new string[] { "UPDATE PHASE - double", op15Actv, wn, rn, op15.GetString("_id") });
                    #endregion
                }
                else
                {
                    // tidak ada phase atau ada phase tetapi sudah di assign ke OP15
                    // create new Document
                    #region Create New WellPlan,  duplicate and Cannot found Valid LS
                    WellActivity wellActivity = new WellActivity();
                    wellActivity.Region = op15.GetString("Region");
                    wellActivity.OperatingUnit = op15.GetString("Operating_Unit");
                    wellActivity.PerformanceUnit = op15.GetString("Performance_Unit");
                    wellActivity.AssetName = op15.GetString("Asset");
                    wellActivity.ProjectName = op15.GetString("Project");
                    wellActivity.WellName = op15.GetString("Well_Name");
                    wellActivity.RigName = op15.GetString("Rig_Name");
                    wellActivity.RigType = op15.GetString("Rig_Type");
                    wellActivity.FirmOrOption = op15.GetString("Planning_\r\nClassification");
                    wellActivity.WorkingInterest = op15.GetDouble("Shell_\r\nWorking_\r\nInterest");

                    if ((wellActivity.UARigSequenceId == null ? "" : wellActivity.UARigSequenceId).Trim().Equals(""))
                    {
                        string aggregateCond1 = "{ $group: { _id: '$_id', maxSequenceId: { $max: '$UARigSequenceId' }}}";
                        string aggregateCond2 = "{ $limit: 1 }";
                        List<BsonDocument> pipes = new List<BsonDocument>();
                        pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
                        pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
                        var number = String.Format("UA{0}", ECIS.Core.SequenceNo.Get("UARigSequenceId").ClaimAsInt());
                        wellActivity.UARigSequenceId = number;
                    }

                    wellActivity.Phases = new List<WellActivityPhase>();

                    int phaseno = 1;
                    WellActivityPhase phase = new WellActivityPhase();

                    phase.ActivityType = op15.GetString("Activity_Type");
                    phase.PhaseNo = phaseno;
                    phase.BaseOP.Add("OP15");
                    phase.ShiftFutureEventDate = isShiftFutureDays;
                    var meanDays = op15.GetDouble("Mean_\r\nTime");
                    phase.PlanSchedule = new DateRange(Tools.ToUTC(op15.GetDateTime("Activity_Start")), Tools.ToUTC(op15.GetDateTime("Activity_End")));
                    phase.Plan = new WellDrillData()
                    {
                        Cost = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000,
                        Days = meanDays//(Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays
                    };

                    phase.ActivityDesc = op15.GetString("Scope_Description");
                    var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                    phase.LevelOfEstimate = maturityLevel;
                    phase.FundingType = op15.GetString("Funding_Type");
                    wellActivity.Phases.Add(phase);
                    wellActivity.Save();
                    UpdatePhaseInfo(wellActivity, op15);
                    WriteLog(new string[] { "Create New WellPlan duplicate and Cannot found Valid LS", at, wn, rn, op15.GetString("_id") });
                    #endregion

                }
            }
            else
            {
                // cek WellName dan RigName
                availWP = WellActivity.Get<WellActivity>(Query.And(Query.EQ("WellName", wn), Query.EQ("RigName", rn)));
                if (availWP != null)
                {
                    // Well and Rig Available in Current DB but doesnt have Phase
                    #region ADD new Phase
                    var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                    var meanDays = op15.GetDouble("Mean_\r\nTime");
                    var proPhase = new WellActivityPhase()
                    {
                        PlanSchedule = new DateRange(Tools.ToUTC(op15.GetDateTime("Activity_Start")), Tools.ToUTC(op15.GetDateTime("Activity_End"))),
                        Plan = new WellDrillData()
                        {
                            Cost = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000,
                            Days = meanDays//(Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays
                        },
                        ActivityType = at,
                        PhaseNo = (availWP.Phases.Count == 0 ? 0 : availWP.Phases.Max(x => x.PhaseNo)) + 1,
                        ActivityDesc = op15.GetString("Scope_Description"),
                        LevelOfEstimate = maturityLevel,
                        FundingType = op15.GetString("Funding_Type")
                    };
                    proPhase.ShiftFutureEventDate = isShiftFutureDays;
                    proPhase.BaseOP.Add("OP15");
                    availWP.Phases.Add(proPhase);
                    availWP.Save();

                    UpdatePhaseInfo(availWP, op15);
                    #endregion
                    WriteLog(new string[] { "ADD NEW IN Existing Well And RIG - double", at, wn, rn, op15.GetString("_id") });
                }
                else
                {
                    // cek WellName 
                    var availWPs = WellActivity.Populate<WellActivity>(Query.EQ("WellName", wn));
                    if (availWPs.Any())
                    {
                        // WellName Same // Indicated with Different Rig
                        var oldWells = availWPs.Where(p => p.Phases.Where(x => x.ActivityType.Equals(at) &&
                            x.PhSchedule.Start != Tools.DefaultDate
                            &&
                            !x.BaseOP.Contains("OP15") // cek hanya Phase yg LS valid dan 
                            ).Count() > 0);

                        if (oldWells.Any())
                        {
                            if (_sequenceIDs.Any())
                            {
                                foreach (var sqe in _sequenceIDs)
                                {
                                    var haveSeq = oldWells.Where(x => x.UARigSequenceId.Equals(sqe));
                                    if (haveSeq.Any())
                                    {
                                        oldWells.ToList().Remove(haveSeq.FirstOrDefault());
                                    }
                                }
                            }
                            if (oldWells.Count() > 0)
                            {
                                var oldWell = oldWells.FirstOrDefault();

                                #region WellName is Exist in Production Data but With Different Rig Name
                                // create new well Plan, [but take information except RIGNAME]

                                WellActivity wellActivity = new WellActivity();
                                wellActivity.Region = op15.GetString("Region");
                                wellActivity.OperatingUnit = op15.GetString("Operating_Unit");
                                wellActivity.PerformanceUnit = op15.GetString("Performance_Unit");
                                wellActivity.AssetName = op15.GetString("Asset");
                                wellActivity.ProjectName = op15.GetString("Project");
                                wellActivity.WellName = op15.GetString("Well_Name");
                                wellActivity.RigName = op15.GetString("Rig_Name");
                                wellActivity.RigType = op15.GetString("Rig_Type");
                                wellActivity.FirmOrOption = op15.GetString("Planning_\r\nClassification");
                                wellActivity.WorkingInterest = op15.GetDouble("Shell_\r\nWorking_\r\nInterest");

                                if ((wellActivity.UARigSequenceId == null ? "" : wellActivity.UARigSequenceId).Trim().Equals(""))
                                {
                                    string aggregateCond1 = "{ $group: { _id: '$_id', maxSequenceId: { $max: '$UARigSequenceId' }}}";
                                    string aggregateCond2 = "{ $limit: 1 }";
                                    List<BsonDocument> pipes = new List<BsonDocument>();
                                    pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
                                    pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
                                    var number = String.Format("UA{0}", ECIS.Core.SequenceNo.Get("UARigSequenceId").ClaimAsInt());
                                    wellActivity.UARigSequenceId = number;
                                }

                                wellActivity.Phases = new List<WellActivityPhase>();

                                WellActivityPhase phase = new WellActivityPhase();

                                phase.ActivityType = op15.GetString("Activity_Type");
                                var chekckPhase = WellActivity.Get<WellActivity>(Query.And(
                                    Query.EQ("WellName", op15.GetString("Well_Name")),
                                    Query.EQ("RigName", op15.GetString("Rig_Name"))
                                    ));

                                phase.PhaseNo = chekckPhase == null ? 1 : (chekckPhase.Phases.Any() ? chekckPhase.Phases.Max(x => x.PhaseNo) : 1);
                                var meanDays = op15.GetDouble("Mean_\r\nTime");
                                phase.PlanSchedule = new DateRange(Tools.ToUTC(op15.GetDateTime("Activity_Start")), Tools.ToUTC(op15.GetDateTime("Activity_End")));
                                phase.Plan = new WellDrillData()
                                {

                                    Cost = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000,
                                    Days = meanDays//(Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays
                                };
                                phase.ActivityDesc = op15.GetString("Scope_Description");
                                var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                                phase.LevelOfEstimate = maturityLevel;
                                phase.FundingType = op15.GetString("Funding_Type");
                                phase.BaseOP.Add("OP15");
                                phase.ShiftFutureEventDate = isShiftFutureDays;
                                wellActivity.Phases.Add(phase);
                                // add phase info

                                // MAKE ZERO LS in Prev, based on WELLNAME and ACTIVITY TYPE

                                // history

                                oldWell.Save();
                                // LS lama di copy kan ke baru
                                wellActivity.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().OP =
                                    oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().OP;

                                // LS Schedule Lama di copykan ke baru
                                wellActivity.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().PhSchedule =
                                    oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().PhSchedule;

                                // LS dan LS Schedule Lama reset
                                oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().OP.Days = 0;
                                oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().PhSchedule = new DateRange();

                                // Calc OP Lama Copykan Ke Baru
                                wellActivity.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().CalcOP =
                                    oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().CalcOP;

                                // Calc OP Schedule Lama Copykan Ke Baru
                                wellActivity.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().CalcOPSchedule =
                                    oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().CalcOPSchedule;

                                wellActivity.Save();
                                oldWell.Save();
                                UpdatePhaseInfo(wellActivity, op15);
                                #endregion
                                WriteLog(new string[] { "RIG MOVING - double", at, wn, rn, op15.GetString("_id"), oldWell.RigName });

                                _sequenceIDs.Add(oldWells.FirstOrDefault().UARigSequenceId);
                            }
                            else
                            {
                                // new Documents
                                #region Totally new WellPlan
                                // create new well Plan, [but take information except RIGNAME]
                                // add new well + phase
                                WellActivity wellActivity = new WellActivity();
                                wellActivity.Region = op15.GetString("Region");
                                wellActivity.OperatingUnit = op15.GetString("Operating_Unit");
                                wellActivity.PerformanceUnit = op15.GetString("Performance_Unit");
                                wellActivity.AssetName = op15.GetString("Asset");
                                wellActivity.ProjectName = op15.GetString("Project");
                                wellActivity.WellName = op15.GetString("Well_Name");
                                wellActivity.RigName = op15.GetString("Rig_Name");
                                wellActivity.RigType = op15.GetString("Rig_Type");
                                wellActivity.FirmOrOption = op15.GetString("Planning_\r\nClassification");
                                wellActivity.WorkingInterest = op15.GetDouble("Shell_\r\nWorking_\r\nInterest");

                                if ((wellActivity.UARigSequenceId == null ? "" : wellActivity.UARigSequenceId).Trim().Equals(""))
                                {
                                    string aggregateCond1 = "{ $group: { _id: '$_id', maxSequenceId: { $max: '$UARigSequenceId' }}}";
                                    string aggregateCond2 = "{ $limit: 1 }";
                                    List<BsonDocument> pipes = new List<BsonDocument>();
                                    pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
                                    pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
                                    var number = String.Format("UA{0}", ECIS.Core.SequenceNo.Get("UARigSequenceId").ClaimAsInt());
                                    wellActivity.UARigSequenceId = number;
                                }

                                wellActivity.Phases = new List<WellActivityPhase>();

                                int phaseno = 1;
                                WellActivityPhase phase = new WellActivityPhase();

                                phase.ActivityType = op15.GetString("Activity_Type");
                                phase.PhaseNo = phaseno;
                                phase.BaseOP.Add("OP15");
                                phase.ShiftFutureEventDate = isShiftFutureDays;
                                phase.PlanSchedule = new DateRange(Tools.ToUTC(op15.GetDateTime("Activity_Start")), Tools.ToUTC(op15.GetDateTime("Activity_End")));
                                var meanDays = op15.GetDouble("Mean_\r\nTime");
                                phase.Plan = new WellDrillData()
                                {
                                    Cost = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000,
                                    Days = meanDays//(Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays
                                };

                                phase.ActivityDesc = op15.GetString("Scope_Description");
                                var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                                phase.LevelOfEstimate = maturityLevel;
                                phase.FundingType = op15.GetString("Funding_Type");
                                wellActivity.Phases.Add(phase);
                                wellActivity.Save();
                                UpdatePhaseInfo(wellActivity, op15);
                                WriteLog(new string[] { "NEW WELL from RIG MOVING (doesnt attach to valid LS)", at, wn, rn, op15.GetString("_id") });
                                #endregion
                            }
                        }
                        else
                        {
                            // new Documents
                            #region Totally new WellPlan
                            // create new well Plan, [but take information except RIGNAME]
                            // add new well + phase
                            WellActivity wellActivity = new WellActivity();
                            wellActivity.Region = op15.GetString("Region");
                            wellActivity.OperatingUnit = op15.GetString("Operating_Unit");
                            wellActivity.PerformanceUnit = op15.GetString("Performance_Unit");
                            wellActivity.AssetName = op15.GetString("Asset");
                            wellActivity.ProjectName = op15.GetString("Project");
                            wellActivity.WellName = op15.GetString("Well_Name");
                            wellActivity.RigName = op15.GetString("Rig_Name");
                            wellActivity.RigType = op15.GetString("Rig_Type");
                            wellActivity.FirmOrOption = op15.GetString("Planning_\r\nClassification");
                            wellActivity.WorkingInterest = op15.GetDouble("Shell_\r\nWorking_\r\nInterest");

                            if ((wellActivity.UARigSequenceId == null ? "" : wellActivity.UARigSequenceId).Trim().Equals(""))
                            {
                                string aggregateCond1 = "{ $group: { _id: '$_id', maxSequenceId: { $max: '$UARigSequenceId' }}}";
                                string aggregateCond2 = "{ $limit: 1 }";
                                List<BsonDocument> pipes = new List<BsonDocument>();
                                pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
                                pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
                                var number = String.Format("UA{0}", ECIS.Core.SequenceNo.Get("UARigSequenceId").ClaimAsInt());
                                wellActivity.UARigSequenceId = number;
                            }

                            wellActivity.Phases = new List<WellActivityPhase>();

                            int phaseno = 1;
                            WellActivityPhase phase = new WellActivityPhase();

                            phase.ActivityType = op15.GetString("Activity_Type");
                            phase.PhaseNo = phaseno;
                            phase.BaseOP.Add("OP15");
                            phase.ShiftFutureEventDate = isShiftFutureDays;
                            var meanDays = op15.GetDouble("Mean_\r\nTime");
                            phase.PlanSchedule = new DateRange(Tools.ToUTC(op15.GetDateTime("Activity_Start")), Tools.ToUTC(op15.GetDateTime("Activity_End")));
                            phase.Plan = new WellDrillData()
                            {
                                Cost = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000,
                                Days = meanDays//(Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays
                            };

                            phase.ActivityDesc = op15.GetString("Scope_Description");
                            var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                            phase.LevelOfEstimate = maturityLevel;
                            phase.FundingType = op15.GetString("Funding_Type");
                            wellActivity.Phases.Add(phase);
                            wellActivity.Save();
                            UpdatePhaseInfo(wellActivity, op15);
                            WriteLog(new string[] { "NEW WELL from RIG MOVING (doesnt found any valid LS)", at, wn, rn, op15.GetString("_id") });
                            #endregion
                        }
                    }
                    else
                    {
                        #region Totally new WellPlan
                        // create new well Plan, [but take information except RIGNAME]
                        // add new well + phase
                        WellActivity wellActivity = new WellActivity();
                        wellActivity.Region = op15.GetString("Region");
                        wellActivity.OperatingUnit = op15.GetString("Operating_Unit");
                        wellActivity.PerformanceUnit = op15.GetString("Performance_Unit");
                        wellActivity.AssetName = op15.GetString("Asset");
                        wellActivity.ProjectName = op15.GetString("Project");
                        wellActivity.WellName = op15.GetString("Well_Name");
                        wellActivity.RigName = op15.GetString("Rig_Name");
                        wellActivity.RigType = op15.GetString("Rig_Type");
                        wellActivity.FirmOrOption = op15.GetString("Planning_\r\nClassification");
                        wellActivity.WorkingInterest = op15.GetDouble("Shell_\r\nWorking_\r\nInterest");

                        if ((wellActivity.UARigSequenceId == null ? "" : wellActivity.UARigSequenceId).Trim().Equals(""))
                        {
                            string aggregateCond1 = "{ $group: { _id: '$_id', maxSequenceId: { $max: '$UARigSequenceId' }}}";
                            string aggregateCond2 = "{ $limit: 1 }";
                            List<BsonDocument> pipes = new List<BsonDocument>();
                            pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
                            pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
                            var number = String.Format("UA{0}", ECIS.Core.SequenceNo.Get("UARigSequenceId").ClaimAsInt());
                            wellActivity.UARigSequenceId = number;
                        }

                        wellActivity.Phases = new List<WellActivityPhase>();

                        int phaseno = 1;
                        WellActivityPhase phase = new WellActivityPhase();

                        phase.ActivityType = op15.GetString("Activity_Type");
                        phase.PhaseNo = phaseno;
                        phase.BaseOP.Add("OP15");
                        phase.ShiftFutureEventDate = isShiftFutureDays;
                        var meanDays = op15.GetDouble("Mean_\r\nTime");
                        phase.PlanSchedule = new DateRange(Tools.ToUTC(op15.GetDateTime("Activity_Start")), Tools.ToUTC(op15.GetDateTime("Activity_End")));
                        phase.Plan = new WellDrillData()
                        {
                            Cost = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000,
                            Days = meanDays//(Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays
                        };

                        phase.ActivityDesc = op15.GetString("Scope_Description");
                        var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                        phase.LevelOfEstimate = maturityLevel;
                        phase.FundingType = op15.GetString("Funding_Type");
                        wellActivity.Phases.Add(phase);
                        wellActivity.Save();
                        UpdatePhaseInfo(wellActivity, op15);
                        WriteLog(new string[] { "TOTALLY NEW WELL ", at, wn, rn, op15.GetString("_id") });
                        #endregion
                    }
                }
            }
        }

        public static void ProcessDouble(List<BsonDocument> doubles, string tableName = "_OP15_20160204")
        {
            // setelah single 
            // baseOP untuk Phase sekarang adalah OP14 dan OP15
            // yang diproses disini adalah Phase yg hanya punya OP14
            foreach (var t in doubles)
            {
                var wn = t.GetString("_id.Well_Name");
                var rn = t.GetString("_id.Rig_Name");
                var at = t.GetString("_id.Activity_Type");
                var count = t.GetString("count");

                var data = DataHelper.Populate(tableName,
                   Query.And(
                   Query.EQ("Well_Name", wn),
                   Query.EQ("Rig_Name", rn),
                   Query.EQ("Activity_Type", at)
                   ));

                DirectlyCreateNewWellPlan(data);

                //foreach (var y in data)
                //{
                //    ProcessToWellPlanDouble(y);
                //}
            }
        }


        public static void LoadOP15_New2(string tableName = "_OP15_20160204", string isSetCurDocBaseOp = "OP14", string logPath = @"D:\LoadOP15Log.csv", string grpTablename = "_OP15_GRP2")
        {
            #region Set Default History
            var waall = WellActivity.Populate<WellActivity>();
            // set cur base op
            if (!isSetCurDocBaseOp.Trim().Equals(""))
            {
                foreach (var t in waall)
                {
                    foreach (var proPhase in t.Phases)
                    {
                        if (proPhase.PlanSchedule.Start != Tools.DefaultDate)
                            proPhase.BaseOP.Add(isSetCurDocBaseOp);
                        #region create history

                        WellDrillData CalcOP = new WellDrillData();
                        CalcOP.Cost = proPhase.CalcOP.Cost;
                        CalcOP.Days = proPhase.CalcOP.Days;
                        DateRange CalcOPSchedule = new DateRange(proPhase.CalcOPSchedule.Start, proPhase.CalcOPSchedule.Finish); // proPhase.CalcOPSchedule,

                        WellDrillData LE = new WellDrillData();
                        LE.Cost = proPhase.LE.Cost;
                        LE.Days = proPhase.LE.Days;
                        DateRange LESchedule = new DateRange(proPhase.LESchedule.Start, proPhase.LESchedule.Finish);// proPhase.LESchedule,

                        WellDrillData OP = new WellDrillData();
                        OP.Cost = proPhase.OP.Cost;
                        OP.Days = proPhase.OP.Days;
                        DateRange PhSchedule = new DateRange(proPhase.PhSchedule.Start, proPhase.PhSchedule.Finish);

                        WellDrillData Plan = new WellDrillData();
                        Plan.Cost = proPhase.Plan.Cost;
                        Plan.Days = proPhase.Plan.Days;
                        DateRange PlanSchedule = new DateRange(proPhase.PlanSchedule.Start, proPhase.PlanSchedule.Finish);

                        WellDrillData AFE = new WellDrillData();
                        AFE.Cost = proPhase.AFE.Cost;
                        AFE.Days = proPhase.AFE.Days;

                        WellDrillData Actual = new WellDrillData();
                        Actual.Cost = proPhase.Actual.Cost;
                        Actual.Days = proPhase.Actual.Days;

                        WellDrillData TQ = new WellDrillData();
                        TQ.Cost = proPhase.TQ.Cost;
                        TQ.Days = proPhase.TQ.Days;

                        t.OPHistories.Add(new OPHistory
                        {
                            ActivityType = proPhase.ActivityType,
                            Actual = Actual,
                            AFE = AFE,
                            CalcOP = CalcOP,
                            CalcOPSchedule = CalcOPSchedule,
                            LE = LE,
                            LESchedule = LESchedule,
                            PhSchedule = PhSchedule,
                            OP = OP,

                            PhaseNo = proPhase.PhaseNo,

                            Plan = Plan,
                            PlanSchedule = PlanSchedule,

                            RigName = t.RigName,
                            TQ = TQ,
                            Type = "OP14",
                            UARigSequenceId = t.UARigSequenceId,
                            WellName = t.WellName
                        });
                        #endregion
                    }
                    t.Save();
                }
            }
            #endregion
            _sequenceIDs = new List<string>();
            List<BsonDocument> msts = DataHelper.Populate(grpTablename)
                .OrderBy(d => d.GetString("_id")).ToList();

            var single = msts.Where(x => BsonHelper.GetInt32(x, "count") == 1).ToList();
            var morethanone = msts.Where(x => BsonHelper.GetInt32(x, "count") != 1).ToList();

            // process single
            ProcessSingle(single);

            // process double
            ProcessDouble(morethanone);
        }


        public static void CreateHistoryAllOP14(string isSetCurDocBaseOp = "OP14")
        {
            #region Set Default History
            var waall = WellActivity.Populate<WellActivity>();
            // set cur base op
            if (!isSetCurDocBaseOp.Trim().Equals(""))
            {
                foreach (var t in waall)
                {
                    foreach (var proPhase in t.Phases)
                    {
                        //if (proPhase.PlanSchedule.Start != Tools.DefaultDate)
                        //    proPhase.BaseOP.Add(isSetCurDocBaseOp);
                        #region create history

                        WellDrillData CalcOP = new WellDrillData();
                        CalcOP.Cost = proPhase.CalcOP.Cost;
                        CalcOP.Days = proPhase.CalcOP.Days;
                        DateRange CalcOPSchedule = new DateRange(proPhase.CalcOPSchedule.Start, proPhase.CalcOPSchedule.Finish); // proPhase.CalcOPSchedule,

                        WellDrillData LE = new WellDrillData();
                        LE.Cost = proPhase.LE.Cost;
                        LE.Days = proPhase.LE.Days;
                        DateRange LESchedule = new DateRange(proPhase.LESchedule.Start, proPhase.LESchedule.Finish);// proPhase.LESchedule,

                        WellDrillData OP = new WellDrillData();
                        OP.Cost = proPhase.OP.Cost;
                        OP.Days = proPhase.OP.Days;
                        DateRange PhSchedule = new DateRange(proPhase.PhSchedule.Start, proPhase.PhSchedule.Finish);

                        WellDrillData Plan = new WellDrillData();
                        Plan.Cost = proPhase.Plan.Cost;
                        Plan.Days = proPhase.Plan.Days;
                        DateRange PlanSchedule = new DateRange(proPhase.PlanSchedule.Start, proPhase.PlanSchedule.Finish);

                        WellDrillData AFE = new WellDrillData();
                        AFE.Cost = proPhase.AFE.Cost;
                        AFE.Days = proPhase.AFE.Days;

                        WellDrillData Actual = new WellDrillData();
                        Actual.Cost = proPhase.Actual.Cost;
                        Actual.Days = proPhase.Actual.Days;

                        WellDrillData TQ = new WellDrillData();
                        TQ.Cost = proPhase.TQ.Cost;
                        TQ.Days = proPhase.TQ.Days;

                        t.OPHistories.Add(new OPHistory
                        {
                            ActivityType = proPhase.ActivityType,
                            Actual = Actual,
                            AFE = AFE,
                            CalcOP = CalcOP,
                            CalcOPSchedule = CalcOPSchedule,
                            LE = LE,
                            LESchedule = LESchedule,
                            PhSchedule = PhSchedule,
                            OP = OP,
                            PhaseNo = proPhase.PhaseNo,
                            Plan = Plan,
                            PlanSchedule = PlanSchedule,
                            RigName = t.RigName,
                            TQ = TQ,
                            Type = "OP14",
                            UARigSequenceId = t.UARigSequenceId,
                            WellName = t.WellName
                        });
                        #endregion
                    }
                    t.Save();
                }
            }
            #endregion
        }

        public static WellActivity CreateHistorySingleOP14(WellActivity actv, WellActivityPhase proPhase, string isSetCurDocBaseOp = "OP14", bool isSave = false)
        {
            #region Set Default History

            #region create history

            WellDrillData CalcOP = new WellDrillData();
            CalcOP.Cost = proPhase.CalcOP.Cost;
            CalcOP.Days = proPhase.CalcOP.Days;
            DateRange CalcOPSchedule = new DateRange(proPhase.CalcOPSchedule.Start, proPhase.CalcOPSchedule.Finish); // proPhase.CalcOPSchedule,

            WellDrillData LE = new WellDrillData();
            LE.Cost = proPhase.LE.Cost;
            LE.Days = proPhase.LE.Days;
            DateRange LESchedule = new DateRange(proPhase.LESchedule.Start, proPhase.LESchedule.Finish);// proPhase.LESchedule,

            WellDrillData OP = new WellDrillData();
            OP.Cost = proPhase.OP.Cost;
            OP.Days = proPhase.OP.Days;
            DateRange PhSchedule = new DateRange(proPhase.PhSchedule.Start, proPhase.PhSchedule.Finish);

            WellDrillData Plan = new WellDrillData();
            Plan.Cost = proPhase.Plan.Cost;
            Plan.Days = proPhase.Plan.Days;
            DateRange PlanSchedule = new DateRange(proPhase.PlanSchedule.Start, proPhase.PlanSchedule.Finish);

            WellDrillData AFE = new WellDrillData();
            AFE.Cost = proPhase.AFE.Cost;
            AFE.Days = proPhase.AFE.Days;

            WellDrillData Actual = new WellDrillData();
            Actual.Cost = proPhase.Actual.Cost;
            Actual.Days = proPhase.Actual.Days;

            WellDrillData TQ = new WellDrillData();
            TQ.Cost = proPhase.TQ.Cost;
            TQ.Days = proPhase.TQ.Days;

            actv.OPHistories.Add(new OPHistory
            {
                ActivityType = proPhase.ActivityType,
                Actual = Actual,
                AFE = AFE,
                CalcOP = CalcOP,
                CalcOPSchedule = CalcOPSchedule,
                LE = LE,
                LESchedule = LESchedule,
                PhSchedule = PhSchedule,
                OP = OP,
                PhaseNo = proPhase.PhaseNo,
                Plan = Plan,
                PlanSchedule = PlanSchedule,
                RigName = actv.RigName,
                TQ = TQ,
                Type = isSetCurDocBaseOp,
                UARigSequenceId = actv.UARigSequenceId,
                WellName = actv.WellName
            });
            #endregion

            if (isSave)
            {
                DataHelper.Save("WEISWellActivities", actv.ToBsonDocument());
            }

            return actv;
            #endregion
        }

        public static OPHistory GetHistorySingleOP14(WellActivity actv, WellActivityPhase proPhase, string isSetCurDocBaseOp = "OP14")
        {
            #region Set Default History

            #region create history

            WellDrillData CalcOP = new WellDrillData();
            CalcOP.Cost = proPhase.CalcOP.Cost;
            CalcOP.Days = proPhase.CalcOP.Days;
            DateRange CalcOPSchedule = new DateRange(proPhase.CalcOPSchedule.Start, proPhase.CalcOPSchedule.Finish); // proPhase.CalcOPSchedule,

            WellDrillData LE = new WellDrillData();
            LE.Cost = proPhase.LE.Cost;
            LE.Days = proPhase.LE.Days;
            DateRange LESchedule = new DateRange(proPhase.LESchedule.Start, proPhase.LESchedule.Finish);// proPhase.LESchedule,

            WellDrillData OP = new WellDrillData();
            OP.Cost = proPhase.OP.Cost;
            OP.Days = proPhase.OP.Days;
            DateRange PhSchedule = new DateRange(proPhase.PhSchedule.Start, proPhase.PhSchedule.Finish);

            WellDrillData Plan = new WellDrillData();
            Plan.Cost = proPhase.Plan.Cost;
            Plan.Days = proPhase.Plan.Days;
            DateRange PlanSchedule = new DateRange(proPhase.PlanSchedule.Start, proPhase.PlanSchedule.Finish);

            WellDrillData AFE = new WellDrillData();
            AFE.Cost = proPhase.AFE.Cost;
            AFE.Days = proPhase.AFE.Days;

            WellDrillData Actual = new WellDrillData();
            Actual.Cost = proPhase.Actual.Cost;
            Actual.Days = proPhase.Actual.Days;

            WellDrillData TQ = new WellDrillData();
            TQ.Cost = proPhase.TQ.Cost;
            TQ.Days = proPhase.TQ.Days;

            var ophis = new OPHistory
            {
                ActivityType = proPhase.ActivityType,
                Actual = Actual,
                AFE = AFE,
                CalcOP = CalcOP,
                CalcOPSchedule = CalcOPSchedule,
                LE = LE,
                LESchedule = LESchedule,
                PhSchedule = PhSchedule,
                OP = OP,
                PhaseNo = proPhase.PhaseNo,
                Plan = Plan,
                PlanSchedule = PlanSchedule,
                RigName = actv.RigName,
                TQ = TQ,
                Type = isSetCurDocBaseOp,
                UARigSequenceId = actv.UARigSequenceId,
                WellName = actv.WellName
            };
            #endregion

            return ophis;
            #endregion
        }


        public static void LoadOP15_New3(string tableName, string isSetCurDocBaseOp = "OP14")
        {
            _sequenceIDs = new List<string>();
            List<BsonDocument> msts = DataHelper.Populate(tableName)
                .OrderBy(d => d.GetString("_id")).ToList();

            foreach (var op15 in msts)
            {
                var wn = op15.GetString("Well_Name");
                var rn = op15.GetString("Rig_Name");
                var at = op15.GetString("Activity_Type");

                bool isShiftFutureDays = op15.GetString("Sequence_#_\r\non_Rig").Trim().Equals("-1") ? true : false;
                int SequenceOnRig = op15.GetInt32("Sequence_#_\r\non_Rig");

                WellActivity availWP = new WellActivity();
                availWP = WellActivity.Get<WellActivity>(Query.And(Query.EQ("WellName", wn), Query.EQ("RigName", rn), Query.EQ("Phases.ActivityType", at)));
                if (availWP != null)
                {

                    // Phase Exist in Current Database
                    var op15Actv = op15.GetString("Activity_Type");
                    #region additional Update Field
                    availWP.Region = op15.GetString("Region");
                    availWP.OperatingUnit = op15.GetString("Operating_Unit");
                    availWP.PerformanceUnit = op15.GetString("Performance_Unit");
                    availWP.AssetName = op15.GetString("Asset");
                    availWP.ProjectName = op15.GetString("Project");
                    availWP.EXType = op15.GetString("Funding_Type");
                    #endregion
                    if (availWP.Phases.Where(x => x.ActivityType.Equals(op15Actv)).Any())
                    {
                        #region UPDATE PHASE
                        var proPhase = availWP.Phases.Where(x => x.ActivityType.Equals(op15Actv)).FirstOrDefault();

                        //// create history dulu
                        //availWP = CreateHistorySingleOP14(availWP, proPhase, "OP14");
                        //DataHelper.Save(availWP.TableName, availWP.ToBsonDocument());
                        var meanDays = op15.GetDouble("Mean_\r\nTime");
                        var activityEnd = Tools.ToUTC(op15.GetDateTime("Activity_Start")).AddDays(meanDays);

                        proPhase.PlanSchedule = new DateRange(Tools.ToUTC(op15.GetDateTime("Activity_Start")), activityEnd);
                        proPhase.Plan.Days = meanDays; //Tools.ToUTC(op15.GetDateTime("Activity_Start")).AddDays(meanDays); // (Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays;
                        proPhase.Plan.Cost = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000;

                        if (!isHaveWeeklyReport(availWP.WellName, availWP.UARigSequenceId, proPhase.ActivityType))
                        {
                            proPhase.LE = new WellDrillData();
                            proPhase.LESchedule = new DateRange();
                            //proPhase.LastWeek = Tools.DefaultDate;

                            //if (proPhase.PhSchedule.Start == Tools.DefaultDate)
                            //{
                            proPhase.OP = proPhase.Plan;
                            proPhase.PhSchedule = proPhase.PlanSchedule;
                            //}

                        }
                        else
                        {
                            //if (proPhase.PhSchedule.Start == Tools.DefaultDate)
                            //{
                            proPhase.OP = proPhase.Plan;
                            proPhase.PhSchedule = proPhase.PlanSchedule;
                            //}
                        }
                        proPhase.CalcOP = new WellDrillData();
                        proPhase.CalcOPSchedule = new DateRange();

                        #region additional Update Field
                        proPhase.ActivityDesc = op15.GetString("Scope_Description");
                        var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                        proPhase.LevelOfEstimate = maturityLevel;
                        proPhase.FundingType = op15.GetString("Funding_Type");
                        proPhase.BaseOP.Add("OP15");
                        proPhase.ShiftFutureEventDate = isShiftFutureDays;
                        proPhase.SequenceOnRig = SequenceOnRig;
                        proPhase.TQ = new WellDrillData(); proPhase.TQ.Days = op15.GetDouble("TQ_Threshold\r\n");
                        #endregion
                        var reff = new string[] { "manualDaysValue" };
                        availWP.Save(references: reff);
                        //DataHelper.Save(availWP.TableName, availWP.ToBsonDocument());

                        UpdatePhaseInfo(availWP, op15);
                        WriteLog(new string[] { "UPDATE PHASE ", op15Actv, wn, rn, op15.GetString("_id") });
                        #endregion
                    }
                }
                else
                {
                    // cek WellName dan RigName
                    availWP = WellActivity.Get<WellActivity>(Query.And(Query.EQ("WellName", wn), Query.EQ("RigName", rn)));
                    if (availWP != null)
                    {
                        // Well and Rig Available in Current DB but doesnt have Phase
                        #region ADD new Phase (new Phase in OP15)
                        var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                        var meanDays = op15.GetDouble("Mean_\r\nTime");

                        var activityEnd = Tools.ToUTC(op15.GetDateTime("Activity_Start")).AddDays(meanDays);

                        var proPhase = new WellActivityPhase()
                        {
                            PlanSchedule = new DateRange(Tools.ToUTC(op15.GetDateTime("Activity_Start")), activityEnd),
                            Plan = new WellDrillData()
                            {
                                Cost = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000,
                                Days = meanDays// (Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays
                            },

                            ActivityType = at,
                            PhaseNo = (availWP.Phases.Count == 0 ? 0 : availWP.Phases.Max(x => x.PhaseNo)) + 1,
                            ActivityDesc = op15.GetString("Scope_Description"),
                            LevelOfEstimate = maturityLevel,
                            FundingType = op15.GetString("Funding_Type")
                        };

                        // set LS same with OP
                        proPhase.PhSchedule = proPhase.PlanSchedule;
                        proPhase.OP = proPhase.Plan;

                        proPhase.BaseOP.Add("OP15");
                        proPhase.ShiftFutureEventDate = isShiftFutureDays;
                        proPhase.SequenceOnRig = SequenceOnRig;
                        proPhase.TQ = new WellDrillData(); proPhase.TQ.Days = op15.GetDouble("TQ_Threshold\r\n");
                        availWP.Phases.Add(proPhase);
                        var reff = new string[] { "manualDaysValue" };
                        availWP.Save(references: reff);

                        //DataHelper.Save(availWP.TableName, availWP.ToBsonDocument());
                        UpdatePhaseInfo(availWP, op15);
                        #endregion
                        WriteLog(new string[] { "ADD NEW PHASE ", at, wn, rn, op15.GetString("_id") });
                    }
                    else
                    {
                        // cek WellName 
                        var availWPs = WellActivity.Populate<WellActivity>(Query.EQ("WellName", wn));
                        if (availWPs.Any())
                        {
                            // WellName Same // Indicated with Different Rig
                            var oldWell = availWPs.Where(p => p.Phases.Where(x => x.ActivityType.Equals(at) &&
                                x.PhSchedule.Start != Tools.DefaultDate
                                ).Count() > 0).FirstOrDefault();

                            var yyy = availWPs.SelectMany(x => x.Phases).Where(x => x.ActivityType.Equals(at) && x.PhSchedule.Start != Tools.DefaultDate);
                            if (yyy.Any())
                            {
                                #region WellName is Exist in Production Data but With Different Rig Name
                                // create new well Plan, [but take information except RIGNAME]

                                WellActivity wellActivity = new WellActivity();
                                wellActivity.Region = op15.GetString("Region");
                                wellActivity.OperatingUnit = op15.GetString("Operating_Unit");
                                wellActivity.PerformanceUnit = op15.GetString("Performance_Unit");
                                wellActivity.AssetName = op15.GetString("Asset");
                                wellActivity.ProjectName = op15.GetString("Project");
                                wellActivity.WellName = op15.GetString("Well_Name");
                                wellActivity.RigName = op15.GetString("Rig_Name");
                                wellActivity.RigType = op15.GetString("Rig_Type");
                                wellActivity.FirmOrOption = op15.GetString("Planning_\r\nClassification");
                                wellActivity.WorkingInterest = op15.GetDouble("Shell_\r\nWorking_\r\nInterest");

                                if ((wellActivity.UARigSequenceId == null ? "" : wellActivity.UARigSequenceId).Trim().Equals(""))
                                {
                                    string aggregateCond1 = "{ $group: { _id: '$_id', maxSequenceId: { $max: '$UARigSequenceId' }}}";
                                    string aggregateCond2 = "{ $limit: 1 }";
                                    List<BsonDocument> pipes = new List<BsonDocument>();
                                    pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
                                    pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
                                    var number = String.Format("UA{0}", ECIS.Core.SequenceNo.Get("UARigSequenceId").ClaimAsInt());
                                    wellActivity.UARigSequenceId = number;
                                }

                                wellActivity.Phases = new List<WellActivityPhase>();

                                WellActivityPhase phase = new WellActivityPhase();

                                phase.ActivityType = op15.GetString("Activity_Type");
                                var chekckPhase = WellActivity.Get<WellActivity>(Query.And(
                                    Query.EQ("WellName", op15.GetString("Well_Name")),
                                    Query.EQ("RigName", op15.GetString("Rig_Name"))
                                    ));

                                phase.PhaseNo = chekckPhase == null ? 1 : (chekckPhase.Phases.Any() ? chekckPhase.Phases.Max(x => x.PhaseNo) : 1);
                                var meanDays = op15.GetDouble("Mean_\r\nTime");
                                var activityEnd = Tools.ToUTC(op15.GetDateTime("Activity_Start")).AddDays(meanDays);

                                phase.PlanSchedule = new DateRange(Tools.ToUTC(op15.GetDateTime("Activity_Start")), activityEnd);

                                phase.Plan = new WellDrillData()
                                {

                                    Cost = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000,
                                    Days = meanDays //(Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays
                                };
                                // set LS same with OP
                                phase.PhSchedule = phase.PlanSchedule;
                                phase.OP = phase.Plan;

                                phase.ActivityDesc = op15.GetString("Scope_Description");
                                var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                                phase.LevelOfEstimate = maturityLevel;
                                phase.FundingType = op15.GetString("Funding_Type");
                                phase.BaseOP.Add("OP15");
                                phase.ShiftFutureEventDate = isShiftFutureDays;
                                phase.SequenceOnRig = SequenceOnRig;
                                phase.TQ = new WellDrillData(); phase.TQ.Days = op15.GetDouble("TQ_Threshold\r\n");

                                wellActivity.Phases.Add(phase);
                                // add phase info

                                // MAKE ZERO LS in Prev, based on WELLNAME and ACTIVITY TYPE

                                // history

                                // LS lama di copy kan ke baru
                                wellActivity.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().OP =
                                    oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().OP;

                                // LS Schedule Lama di copykan ke baru
                                wellActivity.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().PhSchedule =
                                    oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().PhSchedule;

                                // LS dan LS Schedule Lama reset
                                oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().OP.Days = 0;
                                oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().PhSchedule = new DateRange();

                                // Calc OP Lama Copykan Ke Baru
                                wellActivity.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().CalcOP =
                                    oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().CalcOP;

                                // Calc OP Schedule Lama Copykan Ke Baru
                                wellActivity.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().CalcOPSchedule =
                                    oldWell.Phases.Where(x => x.ActivityType.Equals(op15.GetString("Activity_Type"))).FirstOrDefault().CalcOPSchedule;


                                var reff = new string[] { "manualDaysValue" };
                                wellActivity.Save(references: reff);
                                oldWell.Save(references: reff);

                                #region Update Phase Info
                                var pi = WellActivityPhaseInfo.Get<WellActivityPhaseInfo>(
                                    Query.And(
                                        Query.EQ(
                                        "WellActivityId", wellActivity._id.ToString()
                                        ),
                                        Query.EQ(
                                        "WellName", wellActivity.WellName
                                        ),
                                        Query.EQ(
                                        "ActivityType", at
                                        ),
                                        Query.EQ(
                                        "RigName", wellActivity.RigName
                                        ),
                                        Query.EQ(
                                        "SequenceId", wellActivity.UARigSequenceId
                                        ),
                                        Query.EQ(
                                        "OPType", "OP15"
                                        )
                                    )
                                    );
                                if (pi == null)
                                {
                                    pi = new WellActivityPhaseInfo();
                                    pi.WellActivityId = wellActivity._id.ToString();
                                    pi.WellName = wellActivity.WellName;
                                    pi.RigName = wellActivity.RigName;
                                    pi.SequenceId = wellActivity.UARigSequenceId;
                                    pi.ActivityType = wellActivity.ActivityType;
                                }
                                pi.OPType = "OP15";
                                pi.LineOfBusiness = op15.GetString("Line_of_Business");
                                pi.ActivityCategory = op15.GetString("Activity_Category");
                                pi.SpreadRate = op15.GetDouble("OP15_Base_Spread_Rate_($k/Day)");
                                pi.BurnRate = op15.GetDouble("OP15_Base_Burn_Rate_($k/Day)");
                                pi.MRI = op15.GetDouble("MRI");
                                pi.CompletionType = op15.GetString("Completion_Type");
                                pi.CompletionZone = op15.GetInt32("#_of_Completion_Zones");
                                pi.BrineDensity = op15.GetDouble("Brine_Density_(ppg)");
                                pi.EstimatingRangeType = op15.GetString("Estimating_Range_Type");
                                pi.DeterministicLowRange = op15.GetDouble("Deterministic_Low_Range");
                                pi.DeterministicHigh = op15.GetDouble("Deterministic_High");
                                pi.ProbabilisticP10 = op15.GetDouble("Probabilistic_P10");
                                pi.ProbabilisticP90 = op15.GetDouble("Probabilistic_P90");
                                pi.WaterDepthMD = op15.GetDouble("Water_Depth_MD_(ft)");
                                pi.TotalWellDepthMD = op15.GetDouble("Total_Well_Depth_MD_(ft)");
                                pi.LearningCurveFactor = op15.GetDouble("Learning_Curve_Factor");
                                pi.MaturityLevel = maturityLevel;
                                pi.ReferenceFactorModel = op15.GetString("Reference_Factor_Model");
                                pi.RigSequenceId = op15.GetInt32("Sequence_#_\r\non_Rig");

                                pi.TroubleFree.Days = op15.GetDouble("Trouble_Free_\r\nTime");
                                pi.NPT.PercentTime = op15.GetDouble("NPT_\r\nTime_%");
                                pi.TECOP.PercentTime = op15.GetDouble("TECOP_\r\nTime_%");
                                pi.OverrideFactor.Time = op15.GetBool("Override_Time_Factors");
                                pi.NPT.Days = op15.GetDouble("NPT_\r\nTime");
                                pi.TECOP.Days = op15.GetDouble("TECOP_\r\nTime");
                                pi.Mean.Days = op15.GetDouble("Mean_\r\nTime");

                                pi.TroubleFree.Cost = op15.GetDouble("Trouble_Free_\r\nCost_\r\n(Original_Currency,_\r\nMillions)");
                                pi.Currency = op15.GetString("Currency");

                                pi.NPT.PercentCost = op15.GetDouble("NPT_\r\nCost_%");
                                pi.TECOP.PercentCost = op15.GetDouble("TECOP_\r\nCost_%");
                                pi.OverrideFactor.Cost = op15.GetBool("Override_Cost_Factors");

                                pi.NPT.Cost = op15.GetDouble("NPT_Cost_(Original_Currency,_Millions)");
                                pi.TECOP.Cost = op15.GetDouble("TECOP_Cost_(Original_Currency,_Millions)");

                                pi.MeanCostEDM.Cost = op15.GetDouble("Mean_Cost_EDM__(Original_Currency,_Millions)");
                                pi.USDCost.TroubleFree = op15.GetDouble("Trouble_Free\r\nCost_USD_(Millions)");
                                pi.USDCost.NPT = op15.GetDouble("NPT_\r\nCost_USD_(Millions)");
                                pi.USDCost.TECOP = op15.GetDouble("NPT_TECOP_\r\nCost_USD_(Millions)");
                                pi.USDCost.MeanCostEDM = op15.GetDouble("Mean_Cost_EDM_USD_(Millions)");
                                pi.USDCost.Escalation = op15.GetDouble("Escalation_\r\nCost_USD_(Millions)");
                                pi.USDCost.CSO = op15.GetDouble("CSO_\r\nCost_USD_(Millions)");
                                pi.USDCost.Inflation = op15.GetDouble("Inflation_\r\nCost_USD_(Millions)");
                                pi.USDCost.MeanCostMOD = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)");

                                pi.ProjectValueDriver = op15.GetString("Project_Value_Driver");
                                pi.ValueDriverEstimate = op15.GetInt32("Value_Driver_Related_Estimate");

                                pi.TQTreshold = op15.GetInt32("TQ_Threshold\r\n");
                                pi.BICTreshold = op15.GetInt32("BIC_Threshold\r\n");
                                pi.TQGap = op15.GetInt32("TQ_Gap");
                                pi.BICGap = op15.GetInt32("BIC_Gap");
                                pi.PerformanceScore = op15.GetString("Performance_\r\nScore");

                                pi.Save();
                                #endregion
                                //WriteLog(new string[] { "RIG MOVING", t.GetString("Activity_Type"), keyWell, keyRig, t.GetString("_id"), prowel.FirstOrDefault().RigName });

                                #endregion
                                WriteLog(new string[] { "RIG MOVING", at, wn, rn, op15.GetString("_id"), pi.RigName });

                            }
                            else
                            {
                                #region New well because no Valid LS
                                WellActivity wellActivity = new WellActivity();
                                wellActivity.Region = op15.GetString("Region");
                                wellActivity.OperatingUnit = op15.GetString("Operating_Unit");
                                wellActivity.PerformanceUnit = op15.GetString("Performance_Unit");
                                wellActivity.AssetName = op15.GetString("Asset");
                                wellActivity.ProjectName = op15.GetString("Project");
                                wellActivity.WellName = op15.GetString("Well_Name");
                                wellActivity.RigName = op15.GetString("Rig_Name");
                                wellActivity.RigType = op15.GetString("Rig_Type");
                                wellActivity.FirmOrOption = op15.GetString("Planning_\r\nClassification");
                                wellActivity.WorkingInterest = op15.GetDouble("Shell_\r\nWorking_\r\nInterest");

                                if ((wellActivity.UARigSequenceId == null ? "" : wellActivity.UARigSequenceId).Trim().Equals(""))
                                {
                                    string aggregateCond1 = "{ $group: { _id: '$_id', maxSequenceId: { $max: '$UARigSequenceId' }}}";
                                    string aggregateCond2 = "{ $limit: 1 }";
                                    List<BsonDocument> pipes = new List<BsonDocument>();
                                    pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
                                    pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
                                    var number = String.Format("UA{0}", ECIS.Core.SequenceNo.Get("UARigSequenceId").ClaimAsInt());
                                    wellActivity.UARigSequenceId = number;
                                }

                                wellActivity.Phases = new List<WellActivityPhase>();

                                WellActivityPhase phase = new WellActivityPhase();

                                phase.ActivityType = op15.GetString("Activity_Type");
                                var chekckPhase = WellActivity.Get<WellActivity>(Query.And(
                                    Query.EQ("WellName", op15.GetString("Well_Name")),
                                    Query.EQ("RigName", op15.GetString("Rig_Name"))
                                    ));

                                phase.PhaseNo = chekckPhase == null ? 1 : (chekckPhase.Phases.Any() ? chekckPhase.Phases.Max(x => x.PhaseNo) : 1);
                                phase.SequenceOnRig = SequenceOnRig;

                                var meanDays = op15.GetDouble("Mean_\r\nTime");
                                var activityEnd = Tools.ToUTC(op15.GetDateTime("Activity_Start")).AddDays(meanDays);
                                phase.PlanSchedule = new DateRange(Tools.ToUTC(op15.GetDateTime("Activity_Start")), activityEnd);

                                phase.Plan = new WellDrillData()
                                {
                                    Cost = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000,
                                    Days = meanDays// (Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays
                                };

                                phase.PhSchedule = phase.PlanSchedule;
                                phase.OP = phase.Plan;

                                phase.ActivityDesc = op15.GetString("Scope_Description");
                                var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                                phase.LevelOfEstimate = maturityLevel;
                                phase.FundingType = op15.GetString("Funding_Type");
                                phase.BaseOP.Add("OP15");
                                phase.ShiftFutureEventDate = isShiftFutureDays;
                                phase.TQ = new WellDrillData(); phase.TQ.Days = op15.GetDouble("TQ_Threshold\r\n");

                                wellActivity.Phases.Add(phase);

                                var reff = new string[] { "manualDaysValue" };
                                wellActivity.Save(references: reff);

                                UpdatePhaseInfo(wellActivity, op15);
                                WriteLog(new string[] { "RIG MOVING - NEW WELL PLAN BECAUSE no Valid LS ", at, wn, rn, op15.GetString("_id") });
                                #endregion
                            }
                        }
                        else
                        {
                            #region Totally new WellPlan
                            // create new well Plan, [but take information except RIGNAME]
                            // add new well + phase
                            WellActivity wellActivity = new WellActivity();
                            wellActivity.Region = op15.GetString("Region");
                            wellActivity.OperatingUnit = op15.GetString("Operating_Unit");
                            wellActivity.PerformanceUnit = op15.GetString("Performance_Unit");
                            wellActivity.AssetName = op15.GetString("Asset");
                            wellActivity.ProjectName = op15.GetString("Project");
                            wellActivity.WellName = op15.GetString("Well_Name");
                            wellActivity.RigName = op15.GetString("Rig_Name");
                            wellActivity.RigType = op15.GetString("Rig_Type");
                            wellActivity.FirmOrOption = op15.GetString("Planning_\r\nClassification");
                            wellActivity.WorkingInterest = op15.GetDouble("Shell_\r\nWorking_\r\nInterest");

                            if ((wellActivity.UARigSequenceId == null ? "" : wellActivity.UARigSequenceId).Trim().Equals(""))
                            {
                                string aggregateCond1 = "{ $group: { _id: '$_id', maxSequenceId: { $max: '$UARigSequenceId' }}}";
                                string aggregateCond2 = "{ $limit: 1 }";
                                List<BsonDocument> pipes = new List<BsonDocument>();
                                pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
                                pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
                                var number = String.Format("UA{0}", ECIS.Core.SequenceNo.Get("UARigSequenceId").ClaimAsInt());
                                wellActivity.UARigSequenceId = number;
                            }

                            wellActivity.Phases = new List<WellActivityPhase>();

                            int phaseno = 1;
                            WellActivityPhase phase = new WellActivityPhase();

                            phase.ActivityType = op15.GetString("Activity_Type");
                            phase.PhaseNo = phaseno;
                            phase.BaseOP.Add("OP15");
                            phase.ShiftFutureEventDate = isShiftFutureDays;
                            var meanDays = op15.GetDouble("Mean_\r\nTime");
                            var activityEnd = Tools.ToUTC(op15.GetDateTime("Activity_Start")).AddDays(meanDays);
                            phase.PlanSchedule = new DateRange(Tools.ToUTC(op15.GetDateTime("Activity_Start")), activityEnd);
                            phase.SequenceOnRig = SequenceOnRig;

                            phase.Plan = new WellDrillData()
                            {
                                Cost = op15.GetDouble("Mean_Cost_MOD_USD_(Millions)") * 1000000,
                                Days = meanDays//(Tools.ToUTC(op15.GetDateTime("Activity_End")) - Tools.ToUTC(op15.GetDateTime("Activity_Start"))).TotalDays
                            };

                            phase.PhSchedule = phase.PlanSchedule;
                            phase.OP = phase.Plan;

                            phase.ActivityDesc = op15.GetString("Scope_Description");
                            var maturityLevel = op15.GetString("Estimate_\r\nMaturity").Substring(5, 1);
                            phase.LevelOfEstimate = maturityLevel;
                            phase.FundingType = op15.GetString("Funding_Type");
                            phase.TQ = new WellDrillData(); phase.TQ.Days = op15.GetDouble("TQ_Threshold\r\n");
                            wellActivity.Phases.Add(phase);

                            var reff = new string[] { "manualDaysValue" };
                            wellActivity.Save(references: reff);

                            UpdatePhaseInfo(wellActivity, op15);
                            WriteLog(new string[] { "TOTALLY NEW WELL ", at, wn, rn, op15.GetString("_id") });
                            #endregion
                        }
                    }
                }
            }
        }

        private static void SyncActivityCategory()
        {

            #region activitiyCategories
            var activitiyCategories = new Dictionary<string, List<string>>()
                {
                    { "ABANDONMENT", new List<string>() { 
                        "ABANDONMENT", 
                        "ABANDONMENT CAT. 1", 
                        "ABANDONMENT CAT. 2", 
                        "ABANDONMENT CAT. 3", 
                        "ABANDONMENT CAT. 4", 
                        "RISK - ABANDONMENT", 
                        "TEMP ABANDONMENT" } },
                    { "COMPLETION", new List<string>() { 
                        "CLEANUP AND FLOW",
                        "DE-COMPLETION",
                        "INITIAL COMPLETION",
                        "LONG LEAD - COMPLETE",
                        "LOWER COMPLETION",
                        "RE-COMPLETION",
                        "RISK - COMPLETION",
                        "RISK - DE-COMPLETION",
                        "RISK - SLOT RECOVERY",
                        "SLOT RECOVERY",
                        "STIMULATION",
                        "TESTING",
                        "UPPER COMPLETION",
                        "WHOLE COMPLETION EVENT",
                        "ZONAL ISOLATION",
                        "FLOWBACK" } },
                    { "DRILLING", new List<string>() { 
                        "CONDUCTOR JETTING",
                        "DEEPEN",
                        "DRILL AND ABANDON",
                        "INTERM. & PROD ONLY",
                        "INTERMEDIATE SECTION ONLY",
                        "LONG LEAD - DRILL",
                        "PILOT HOLE",
                        "PRODUCTION SECTION ONLY",
                        "RE-DRILL",
                        "RISK - APPRAISAL",
                        "RISK - APPRAISAL RISK",
                        "RISK - DRILLING",
                        "SIDE TRACK",
                        "TOP HOLE ONLY",
                        "WHOLE DRILLING EVENT",
                        "BATCH SET 1",
                        "BATCH SET 2",
                        "PRE-DRILL",
                        "RE-ENTRY" } },
                    { "MANAGEMENT ADJUSTMENT", new List<string>() { 
                        "MANAGEMENT ADJUSTMENT" } },
                    { "PROJECTS", new List<string>() { 
                        "LOGISTICS",
                        "PROJECTS",
                        "PROJECTS - OTHER",
                        "SITE PREPARATION",
                        "SUBSEA INSTALLATION",
                        "SURFACE INSTALLATION",
                        "PORTFOLIO RISK",
                        "TECH DEVELOPMENT",
                        "TP EQUIPMENT INTEGRATION",
                        "D&PM -WELLS SYSTEMS",
                        "C&WI EQUIPMENT",
                        "PROJECT LONG LEADS" } },
                    { "RIG", new List<string>() { 
                        "RIG DEMOBILISATION",
                        "RIG IDLE",
                        "RIG MAINTENANCE",
                        "RIG MOBILISATION",
                        "RIG MOVE/PREP",
                        "RIG PREPARATION AND INSPECTION",
                        "RIG RAMP UP/DOWN",
                        "RIG SHUTDOWN",
                        "RISER MAINTENANCE (TLP/SPAR)",
                        "RISK - RIG",
                        "RISK - RIG SHUTDOWN",
                        "RISK - RISER MAINTENANCE",
                        "RISK - SBS (CAISSON)",
                        "SBS (CAISSON)",
                        "WEATHER",
                        "RIG DELAY" } },
                    { "TOTAL WELL", new List<string>() { 
                        "LONG LEAD - TOTAL WELL",
                        "RISK - TOTAL WELL",
                        "TOTAL WELL" } },
                    { "WORKOVER", new List<string>() { 
                        "RISK - WORKOVER",
                        "SUSPEND",
                        "WELL INTERVENTION",
                        "WORKOVER" } },
                    { "WRFM OPTIMIZATION", new List<string>() { 
                        "ACID STIMULATION",
                        "ADDITIONAL PERFORATIONS",
                        "ART. LIFT RE-SIZING",
                        "BEAN UPS/ CHOKE BACK",
                        "CEMENT SHUT-OFFS",
                        "CHEMICAL SHUT-OFFS",
                        "DELIQUIFICATION",
                        "FRACTURING",
                        "GAS / WATER INJECTION",
                        "GAS LIFT VALVE CHANGEOUTS",
                        "MECHANICAL STRADDLE SHUT-OFFS",
                        "PLUG SETTING SHUT-OFFS",
                        "PLUNGER LIFTING",
                        "REPERFORATIONS",
                        "RISK - WRFM OPTIMIZATION",
                        "SHIFTING SLEEVES",
                        "TUB. SIZE OPT.",
                        "VELOCITY STRING/TAIL PIPE EXT.",
                        "WELL CONVERSION E.G. OP>WI",
                        "WRFM OPTIMIZATION - OTHER",
                        "ZONE CHANGE",
                        "ZONE CHANGE" } },
                    { "WRFM RESTORATION", new List<string>() {
                        "ART. LIFT REPAIR",
                        "CT KICK OFFS",
                        "FOAM LIFTING",
                        "GASLIFT VALVE REPAIRS",
                        "INTEGRITY RE-COMPLETION",
                        "RESERVOIR WELLBORE CLEANOUTS",
                        "RISK - WRFM RESTORATION",
                        "SAND CONTROL REPAIR",
                        "SCALE REMOVAL",
                        "SCALE SQUEEZES",
                        "SC-SSSV REPAIRS",
                        "TREE RESTORATIONS",
                        "WATER WASHES",
                        "WRFM RESTORATION - OTHER" } },
                    { "WRFM SURVEILLANCE", new List<string>() {
                        "FLUID PROPERTIES",
                        "LOGGING",
                        "PRESS. MONITORING",
                        "PRESS. TRANSIENT ANALYSIS",
                        "PROD./INJ. PROFILE LOG",
                        "RISK - WRFM SURVEILLANCE",
                        "WELL INTEGR. MONITORING",
                        "WRFM SURVEILLANCE - OTHER" } }
                };
            #endregion

            ActivityMaster.Populate<ActivityMaster>().ForEach(d =>
            {
                var eachDoc = d.ToBsonDocument();
                eachDoc.Remove("ActivityCategory");
                DataHelper.Save(d.TableName, eachDoc);
            });

            foreach (KeyValuePair<string, List<string>> each in activitiyCategories)
            {
                each.Value.ForEach(d =>
                {
                    var act = ActivityMaster.Get<ActivityMaster>(Query.EQ("_id", d));
                    if (act != null)
                    {
                        act.ActivityCategory = each.Key;
                        act.Save();
                    }
                });
            }

            ActivityMaster.Populate<ActivityMaster>(Query.EQ("ActivityCategory", BsonNull.Value)).ForEach(d =>
            {
                var actType = Convert.ToString(d._id);

                if (actType.Contains("DRL") || actType.Contains("DRILL"))
                {
                    d.ActivityCategory = "DRILLING";
                    d.Save();
                }
                else if (actType.Contains("COM"))
                {
                    d.ActivityCategory = "COMPLETION";
                    d.Save();
                }
                else if (actType.Contains("RIG"))
                {
                    d.ActivityCategory = "RIG";
                    d.Save();
                }
                else if (actType.Contains("PROJECTS"))
                {
                    d.ActivityCategory = "PROJECTS";
                    d.Save();
                }
                else if (actType.Contains("SUSPEND"))
                {
                    d.ActivityCategory = "WORKOVER";
                    d.Save();
                }
                else if (actType.Contains("WRFM") && actType.Contains("RISK"))
                {
                    d.ActivityCategory = "WRFM SURVEILLANCE";
                    d.Save();
                }
                else if (actType.Contains("WRFM") && actType.Contains("RST"))
                {
                    d.ActivityCategory = "WRFM RESTORATION";
                    d.Save();
                }
            });
        }

        private static void AddDataActivityCategory()
        {
            var t = ActivityMaster.Populate<ActivityMaster>();
            foreach (var x in t)
            {
                if (x.ActivityCategory != null)
                {
                    var actCat = ActivityCategory.Get<ActivityCategory>(Query.EQ("Title", x.ActivityCategory));
                    if (actCat == null)
                    {
                        var a = new ActivityCategory();
                        a.Title = x.ActivityCategory;
                        a.Save();
                    }
                }
            }

        }

        public static void MergeMasterData()
        {
            var wellActivities = WellActivity.Populate<WellActivity>();

            // region
            var regions = wellActivities.GroupBy(x => x.Region).ToList().Where(x => x.Key != null);
            DataHelper.Delete("WEISRegions");
            foreach (var y in regions)
            {
                BsonDocument d = new BsonDocument();
                d.Set("_id", y.Key);
                DataHelper.Save("WEISRegions", d);
            }

            // Operationg Unit
            var opUnit = wellActivities.GroupBy(x => x.OperatingUnit).ToList().Where(x => x.Key != null);
            DataHelper.Delete("WEISOperatingUnits");
            foreach (var y in opUnit)
            {
                BsonDocument d = new BsonDocument();
                d.Set("_id", y.Key);
                DataHelper.Save("WEISOperatingUnits", d);
            }

            // performance unit 
            var perf = wellActivities.GroupBy(x => x.PerformanceUnit).ToList().Where(x => x.Key != null);
            DataHelper.Delete("WEISPerformanceUnits");
            foreach (var y in perf)
            {
                BsonDocument d = new BsonDocument();
                d.Set("_id", y.Key);
                DataHelper.Save("WEISPerformanceUnits", d);
            }

            // Asset 
            var asset = wellActivities.GroupBy(x => x.AssetName).ToList().Where(x => x.Key != null);
            DataHelper.Delete("WEISAssetNames");
            foreach (var y in asset)
            {
                BsonDocument d = new BsonDocument();
                d.Set("_id", y.Key);
                DataHelper.Save("WEISAssetNames", d);
            }

            // Projects  
            var projs = wellActivities.GroupBy(x => x.ProjectName).ToList().Where(x => x.Key != null);
            DataHelper.Delete("WEISProjectNames");
            foreach (var y in projs)
            {
                BsonDocument d = new BsonDocument();
                d.Set("_id", y.Key);
                DataHelper.Save("WEISProjectNames", d);
            }

            // WellName  
            var wnms = DataHelper.Populate("WEISWellNames");
            var wellNamesExsiting = wnms.Select(x => BsonHelper.GetString(x, "_id"));
            var wells = wellActivities.GroupBy(x => x.WellName).ToList().Where(x => x.Key != null);
            foreach (var y in wells)
            {
                if (!wellNamesExsiting.Contains(y.Key))
                {
                    WellInfo d = new WellInfo();
                    d._id = y.Key;

                    d.Save("WEISWellNames");
                }
            }

            // activityType
            var actvsm = DataHelper.Populate<ActivityMaster>("WEISActivities");
            var activityExist = actvsm.Select(x => x._id);
            var activityType = wellActivities.SelectMany(x => x.Phases).ToList().GroupBy(x => x.ActivityType).Where(x => x.Key != null);
            foreach (var y in activityType)
            {
                if (!activityExist.Contains(y.Key))
                {
                    ActivityMaster d = new ActivityMaster();
                    d._id = y.Key;
                    d.Save("WEISActivities");
                }
            }

            // Rig Name
            var rigAll = DataHelper.Populate("WEISRigNames");
            var rigsExist = rigAll.Select(x => BsonHelper.GetString(x, "_id"));
            var rigIn = wellActivities.GroupBy(x => x.RigName);
            foreach (var y in rigIn)
            {
                if (!rigsExist.Contains(y.Key))
                {
                    MasterRigName d = new MasterRigName();
                    d._id = y.Key;
                    d.Save("WEISRigNames");
                }
            }

            // Rig Type
            var rTypeall = DataHelper.Populate("WEISRigTypes");
            DataHelper.Delete("WEISRigTypes");
            var typeExi = rTypeall.Select(x => BsonHelper.GetString(x, "_id"));
            var types = wellActivities.GroupBy(x => x.RigType);
            foreach (var y in types)
            {
                //if (!typeExi.Contains(y.Key))
                //{
                MasterRigType d = new MasterRigType();
                d._id = y.Key;
                d.Save("WEISRigTypes");
                //}
            }

            // activity Category
            SyncActivityCategory();
            AddDataActivityCategory();

            // hide RSVP menu
            DataHelper.Delete("Main_Menu", Query.EQ("Title", "RSVP"));
        }
    }
    public class GroupHelper
    {
        public string WellName { get; set; }
        public string RigName { get; set; }
    }
    public class ValueHelper
    {
        public string Type { get; set; }
        public DateRange Schedule { get; set; }
        public WellDrillData Value { get; set; }
    }

    public class OPHelper
    {
        public string WellName { get; set; }
        public string RigName { get; set; }
        public string ActivityType { get; set; }
        public string SequenceId { get; set; }
        public int PhaseNo { get; set; }
        public List<ValueHelper> Values { get; set; }
        public OPHelper()
        {
            Values = new List<ValueHelper>();
        }
    }
}
