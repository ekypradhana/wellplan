using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECIS.Client.WEIS
{

    public class PMasterStandard : ECISModel
    {
        Dictionary<string, string> PMasterField = new Dictionary<string, string>
        {
            {"Expex: E&A Well Costs (Unrisked)", "UP12aP" } ,
            {"Expex: Success Followup Well Costs (Risked)" , "UP12bP" } ,
            {"Expex: for development activities: C2E", "UP13P" } ,
            {"Capex: Wells", "UP16P" } ,
            {"Opex: Production Operation [Wells Portion]", "UP19cP" } ,
            {"Total Abandonment Costs and Site Restoration [Wells Portion]", "UP21P" } ,
            //{"Number of Unrisked Exploration Wells Drilled (whole number)", "OP_I23a" } ,
            //{"Number of Risked Exploration Wells Drilled (risked number)", "OP_I24a" } ,
            //{"Number of NFE Only Exploration Wells Completed", "OP_I29" } ,
            //{"Number of Development Wells Drilled", "OP_I31" } ,
            //{"Number of Development Wells Completed" , "OP_I32" } ,
        };

        Dictionary<string, string> PMasterFieldNoFundingType = new Dictionary<string, string>
        {
            {"No Funding Type", "" } ,
        };

        public override string TableName
        {
            get { return "WEISPalantirMasterStandardMap"; }
        }

        public string UpdateBy { get; set; }
        public string MapName { get; set; }

        public string ReportEntity { get; set; }
        public string PlanningEntity { get; set; }
        public string PlanningEntityID { get; set; }
        public string ActivityEntity { get; set; }
        public string ActivityEntityID { get; set; }
        public double Prob { get; set; }

        public bool IsInPlan { get; set; }
        public string FirmOption { get; set; }
        public DateTime UpdateVersion { get; set; }

        public string AssetName { get; set; }
        public string LoB { get; set; }
        public string WellName { get; set; }
        public string RigName { get; set; }
        public string ActivityType { get; set; }
        public string UARigSequenceId { get; set; }
        public string ActivityCategory { get; set; }
        public string BaseOP { get; set; }
        public string FundingType { get; set; }
        public string ProjectName { get; set; }
        public double WorkingInterest { get; set; }

        public DateRange PhSchedule { get; set; }
        public WellDrillData OP { get; set; }


        public DateRange PlanSchedule { get; set; }
        public WellDrillData Plan { get; set; }
        public double PlanMODSS { get; set; }
        public double PlanEDM { get; set; }
        public double PlanEDMSS { get; set; }
        public double PlanRT { get; set; }
        public double PlanRTSS { get; set; }
        public BizPlanAllocation Allocation { get; set; }

        public PMasterStandard GetListPmaster(BsonDocument data, string updateby, bool isUpload, PMasterStandard result = null)
        {
            if(result == null) result = new PMasterStandard();
            
            #region ga perlu di transform lagi karena sudah bentuk PMasterStandard lengkap (sudah di transform waktu generate)

            //var query = Query.And(
            //    Query.EQ("WellName", data.GetString("WellName")),
            //    Query.EQ("RigName", data.GetString("RigName")),
            //    Query.EQ("UARigSequenceId", data.GetString("UARigSequenceId")),
            //    Query.EQ("Phases.ActivityType", data.GetString("ActivityType")));
            //var getMapValue = BizPlanActivity.GetAll(query, isMergeWithWellPlan: false).FirstOrDefault();

            ////var getMapValue = MapValue;

            //if (getMapValue != null && getMapValue.Phases.Count > 0)
            //{
            //    foreach (var t in getMapValue.Phases)
            //    {
            //        if (t.ActivityType == data.GetString("ActivityType"))
            //        {
            //            result = result.TransformsToMapBizPlan(getMapValue, t, updateby, DateTime.Now.Date, false);

            //            result.UpdateBy = updateby;
            //            result.WellName = data.GetString("WellName");
            //            result.ActivityType = data.GetString("ActivityType");
            //            result.RigName = data.GetString("RigName");
            //            result.ReportEntity = data.GetString("ReportEntity");
            //            result.PlanningEntity = data.GetString("PlanningEntity");
            //            result.PlanningEntityID = data.GetString("PlanningEntityID");
            //            result.ActivityEntity = data.GetString("ActivityEntity");
            //            result.ActivityEntityID = data.GetString("ActivityEntityID");
            //            result.Prob = data.GetDouble("Prob");
            //            result.ActivityCategory = data.GetString("ActivityCategory");
            //            result.BaseOP = data.GetString("BaseOP");
            //            result.FundingType = data.GetString("FundingType");
            //            result.ProjectName = getMapValue.ProjectName;
            //            result.FirmOption = data.GetString("FirmOption");

            //            if (!isUpload)
            //            {
            //                result.IsInPlan = data.GetBool("IsInPlan");
            //                result.PlanSchedule = new DateRange() { Start = data.GetDateTime("PlanSchedule.Start"), Finish = data.GetDateTime("PlanSchedule.Finish") };
            //                result.PhSchedule = new DateRange() { Start = data.GetDateTime("PhSchedule.Start"), Finish = data.GetDateTime("PhSchedule.Finish") };
            //            }
            //            else { 

            //            }
            //        }
            //    }
            //}
            //else
            //{

            //}
            #endregion

            result.UpdateBy = updateby;
            result.ReportEntity = data.GetString("ReportEntity");
            result.PlanningEntity = data.GetString("PlanningEntity");
            result.PlanningEntityID = data.GetString("PlanningEntityID");
            result.ActivityEntity = data.GetString("ActivityEntity");
            result.ActivityEntityID = data.GetString("ActivityEntityID");
            
            if (!isUpload)
            {
                result.IsInPlan = data.GetBool("IsInPlan");
                result.PlanSchedule = new DateRange() { Start = data.GetDateTime("PlanSchedule.Start"), Finish = data.GetDateTime("PlanSchedule.Finish") };
                result.PhSchedule = new DateRange() { Start = data.GetDateTime("PhSchedule.Start"), Finish = data.GetDateTime("PhSchedule.Finish") };
            }

            return result;
        }
        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            //DateTime createDate = Tools.ToUTC(DateTime.Now, true);
            DateTime createDate = DateTime.Now;
            UpdateVersion = new DateTime(createDate.Year, createDate.Month, createDate.Day, createDate.Hour, createDate.Minute, createDate.Second);
            _id = String.Format("W{0}S{1}A{2}R{3}D{4}U{5}M{6}",
                WellName.Replace(" ", "").Replace("-", ""),
                UARigSequenceId,
                ActivityType.Replace(" ", "").Replace("-", ""),
                RigName.Replace(" ", "").Replace("-", ""),
                UpdateBy,
                this.BaseOP,
                MapName.Replace(" ", "").Replace("-", "")
                );
            return this.ToBsonDocument();
        }


        public PMasterStandard()
        {
            PhSchedule = new DateRange();
            OP = new WellDrillData();
            PlanSchedule = new DateRange();
            Plan = new WellDrillData();
            Allocation = new BizPlanAllocation();

            MapName = "";
            ReportEntity = "";
            PlanningEntity = "";
            PlanningEntityID = "";
            ActivityEntity = "";
            ActivityEntityID = "";
            FirmOption = "";
            AssetName = "";
            LoB = "";
            WellName = "";
            RigName = "";
            ActivityType = "";
            UARigSequenceId = "";
            ActivityCategory = "";
            BaseOP = "";
            FundingType = "";
            ProjectName = "";
        }

        public static bool isHaveBizPlan(string well, string seq, string events, List<string> status = null)
        {
            if (status == null)
            {
                status = new List<string>();
                status.Add("Complete");
                status.Add("Modified");
            }
            var t = BizPlanActivity.Populate<BizPlanActivity>(
                Query.And(
                    Query.EQ("WellName", well),
                    Query.EQ("UARigSequenceId", seq),
                    Query.EQ("Phases.ActivityType", events)
                )
                );
            if (t != null && t.Count() > 0)
            {
                if (t.FirstOrDefault().Phases.Where(x => x.ActivityType.Equals(events)).Count() > 0)
                {
                    var ph = t.FirstOrDefault().Phases.Where(x => x.ActivityType.Equals(events)).FirstOrDefault();

                    if (status.Contains(ph.Estimate.Status.Trim()))
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
            else
            {
                return false;
            }
        }

        public List<PMasterDetails> CalcDetails(List<PMasterStandard> pmasters, string attributetype,string MoneyType, int monthIdStart = 201601, int monthIdFinish = 206612, int onlyYearValueStart = 201601, double probability = 1.0)
        {

            if (probability > 1) probability = Tools.Div(probability, 100);

            List<PMasterDetails> result = new List<PMasterDetails>();
            var FType = "";
            #region Mapping Money Type
            switch (attributetype)
            {
                case "Expex: E&A Well Costs (Unrisked)":
                    {
                        FType = "EXPEX";
                        break;
                    }
                case "Expex: Success Followup Well Costs (Risked)":
                    {
                        FType = "EXPEX SUCCESS";
                        break;
                    }
                case "Expex: for development activities: C2E":
                    {
                        FType = "C2E";
                        break;
                    }
                case "Capex: Wells":
                    {
                        FType = "CAPEX";
                        break;
                    }
                case "Opex: Production Operation [Wells Portion]":
                    {
                        FType = "OPEX";
                        break;
                    }
                case "Total Abandonment Costs and Site Restoration [Wells Portion]":
                    {
                        FType = "ABEX";
                        break;
                    }
                case "No Funding Type":
                    {
                        FType = "NoFundingType";
                        break;
                    }
                case "Number of Unrisked Exploration Wells Drilled (whole number)":
                    {
                        break;
                    }
                case "Number of Risked Exploration Wells Drilled (risked number)":
                    {
                        break;
                    }
                case "Number of NFE Only Exploration Wells Completed":
                    {
                        break;
                    }
                case "Number of Development Wells Drilled":
                    {
                        break;
                    }
                case "Number of Development Wells Completed":
                    {
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            #endregion

            #region calculation monthly
            //do
            //{
            //    var expexdata = pmasters.Where(x => x.FundingType.Equals(FType));
            //    if (expexdata.Any())
            //    {
            //        foreach (var r in expexdata)
            //        {
            //            var splittedmonth = DateRangeToMonth.SplitedDateRangeMonthly(r.PlanSchedule);
            //            foreach (var spl in splittedmonth)
            //            {
            //                if (monthIdStart == spl.MonthId)
            //                {
            //                    double val = 0;
            //                    double valSS = 0;
            //                    var monthlyBucket = r.Allocation.MonthlyBuckets;
            //                    if (monthlyBucket.Any())
            //                    {
            //                        var monthlyBucketValActive = monthlyBucket.Where(x => x.Key.Equals(spl.MonthId.ToString()) && x.Type == "Active").Count();
            //                        if (monthlyBucketValActive != 0)
            //                        {
            //                            var monthlyBucketVal = monthlyBucket.Where(x => x.Key.Equals(spl.MonthId.ToString()) && x.Type == "Active").ToList();
            //                            var monthlyBucketCtgs = monthlyBucket.Where(x => x.Key.Equals(spl.MonthId.ToString()) && x.Type == "Contigency").ToList();
            //                            var isCtgsFound = monthlyBucketCtgs.Count() > 0;
            //                            if (r.Plan.Cost != 0 && r.PlanMODSS != 0)
            //                            {
            //                                val = monthlyBucketVal.Sum(x => x.MeanCostMOD) + (isCtgsFound ? monthlyBucketCtgs.Sum(x => x.MeanCostMOD) : 0); //r.Plan.Cost;
            //                                valSS = monthlyBucketVal.Sum(x => x.MeanCostMODSS) + (isCtgsFound ? monthlyBucketCtgs.Sum(x => x.MeanCostMODSS) : 0); //r.PlanMODSS;
            //                            }
            //                            else if (r.PlanEDM != 0 && r.PlanEDMSS != 0)
            //                            {
            //                                val = monthlyBucketVal.Sum(x => x.MeanCostEDM) + (isCtgsFound ? monthlyBucketCtgs.Sum(x => x.MeanCostEDM) : 0); //r.PlanEDM;
            //                                valSS = monthlyBucketVal.Sum(x => x.MeanCostEDMSS) + (isCtgsFound ? monthlyBucketCtgs.Sum(x => x.MeanCostEDMSS) : 0); //r.PlanEDMSS;
            //                            }
            //                            else
            //                            {
            //                                val = monthlyBucketVal.Sum(x => x.MeanCostRealTerm) + (isCtgsFound ? monthlyBucketCtgs.Sum(x => x.MeanCostRealTerm) : 0); //r.PlanRT;
            //                                valSS = monthlyBucketVal.Sum(x => x.MeanCostRealTermSS) + (isCtgsFound ? monthlyBucketCtgs.Sum(x => x.MeanCostRealTermSS) : 0); //r.PlanRTSS;
            //                            }
            //                        }
            //                    }

            //                    result.Add(new PMasterDetails
            //                    {
            //                        DateId = monthIdStart,
            //                        Type = "Month",
            //                        value = val, //spl.Proportion * val,
            //                        valueSS = valSS, //spl.Proportion * valSS,
            //                        isZeroVal = false
            //                    });
            //                }
            //                else
            //                {
            //                    result.Add(new PMasterDetails
            //                    {
            //                        DateId = monthIdStart,
            //                        Type = "Month",
            //                        value = 0,
            //                        valueSS = 0,
            //                        isZeroVal = true

            //                    });
            //                }
            //            }
            //        }


            //    }

            //    monthIdStart = AddMonthId(monthIdStart, 1);
            //} while (monthIdStart <= monthIdFinish && (monthIdStart != onlyYearValueStart));
            #endregion
            //var lastMoth = aaa.Max();


            #region calculation year only
            var yearonlystart = Convert.ToInt32(onlyYearValueStart.ToString().Substring(0, 4));
            var yearonlyfinish = Convert.ToInt32(monthIdFinish.ToString().Substring(0, 4));
            do
            {
                var expexdata = new List<PMasterStandard>();
                if (FType != "NoFundingType")
                {
                    expexdata = pmasters.Where(x => x.FundingType != null && x.FundingType.Equals(FType)).ToList();
                }
                else
                {
                    expexdata = pmasters.Where(x => x.FundingType == null || (!x.FundingType.Equals("CAPEX") && !x.FundingType.Equals("EXPEX") && !x.FundingType.Equals("ABEX") && !x.FundingType.Equals("C2E") && !x.FundingType.Equals("EXPEX SUCCESS") && !x.FundingType.Equals("OPEX"))).ToList();
                }
                if (expexdata.Any())
                {
                    foreach (var r in expexdata)
                    {
                        var annuallyBucket = r.Allocation.AnnualyBuckets;
                        if (annuallyBucket.Any())
                        {
                            foreach (var an in annuallyBucket.GroupBy(x=>x.Key))
                            {
                                if (yearonlystart.ToString() == an.Key)
                                {
                                    double val = 0;
                                    double valSS = 0;
                                    if (MoneyType == "MOD")
                                    {
                                        val = an.Sum(x => x.MeanCostMOD) * probability; //r.Plan.Cost;
                                        valSS = an.Sum(x => x.MeanCostMODSS) * probability; //r.PlanMODSS;
                                    }
                                    else if (MoneyType == "EDM")
                                    {
                                        val = an.Sum(x => x.MeanCostEDM) * probability; //r.PlanEDM;
                                        valSS = an.Sum(x => x.MeanCostEDMSS) * probability; //r.PlanEDMSS;
                                    }
                                    else
                                    {
                                        val = an.Sum(x => x.MeanCostRealTerm) * probability; //r.PlanRT;
                                        valSS = an.Sum(x => x.MeanCostRealTermSS) * probability; //r.PlanRTSS;
                                    }

                                    result.Add(new PMasterDetails
                                    {
                                        DateId = yearonlystart,
                                        Type = "Year",
                                        value = val,        //spl.Proportion * val,
                                        valueSS = valSS,    //spl.Proportion * valSS,
                                        isZeroVal = false
                                    });
                                }
                                else
                                {
                                    result.Add(new PMasterDetails
                                    {
                                        DateId = yearonlystart,
                                        Type = "Year",
                                        value = 0,
                                        valueSS = 0,
                                        isZeroVal = true

                                    });
                                }
                            }
                        }

                    }

                }
                yearonlystart = yearonlystart + 1;
            } while (yearonlystart <= yearonlyfinish);
            #endregion

            var t = result.GroupBy(x => x.DateId);
            List<PMasterDetails> output = new List<PMasterDetails>();
            foreach (var y in t)
            {
                output.Add(new PMasterDetails
                {
                    DateId = y.Key,
                    Type = y.FirstOrDefault().Type,
                    value = y.Sum(x => x.value),
                    valueSS = y.Sum(x => x.valueSS),
                    attribute = attributetype
                });

            }


            return output;
        }

        public List<PMasterDetails> CalcDetailsOld(List<PMasterStandard> pmasters, string attributetype, int monthIdStart = 201601, int monthIdFinish = 206612, int onlyYearValueStart = 202201)
        {
            List<PMasterDetails> result = new List<PMasterDetails>();
            #region Total
            switch (attributetype)
            {
                case "Expex: E&A Well Costs (Unrisked)":
                    {
                        #region Expex: E&A Well Costs (Unrisked) SS PR USD mln

                        do
                        {
                            var expexdata = pmasters.Where(x => x.FundingType.Equals("EXPEX"));
                            if (expexdata.Any())
                            {
                                foreach (var r in expexdata)
                                {
                                    var splittedmonth = DateRangeToMonth.SplitedDateRangeMonthly(r.PlanSchedule);
                                    foreach (var spl in splittedmonth)
                                    {
                                        if (monthIdStart == spl.MonthId)
                                        {
                                            double val = 0;
                                            double valSS = 0;
                                            if (r.Plan.Cost != 0 && r.PlanMODSS != 0)
                                            {
                                                val = r.Plan.Cost;
                                                valSS = r.PlanMODSS;
                                            }
                                            else if (r.PlanEDM != 0 && r.PlanEDMSS != 0)
                                            {
                                                val = r.PlanEDM;
                                                valSS = r.PlanEDMSS;
                                            }
                                            else
                                            {
                                                val = r.PlanRT;
                                                valSS = r.PlanRTSS;
                                            }

                                            result.Add(new PMasterDetails
                                            {
                                                DateId = monthIdStart,
                                                Type = "Month",
                                                value = spl.Proportion * val,
                                                valueSS = spl.Proportion * valSS,
                                                isZeroVal = false
                                            });
                                        }
                                        else
                                        {
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = monthIdStart,
                                                Type = "Month",
                                                value = 0,
                                                valueSS = 0,
                                                isZeroVal = true

                                            });
                                        }
                                    }
                                }


                            }

                            monthIdStart = AddMonthId(monthIdStart, 1);
                        } while (monthIdStart <= monthIdFinish && (monthIdStart != onlyYearValueStart));

                        //var lastMoth = aaa.Max();

                        var yearonlystart = Convert.ToInt32(onlyYearValueStart.ToString().Substring(0, 4));
                        var yearonlyfinish = Convert.ToInt32(monthIdFinish.ToString().Substring(0, 4));
                        do
                        {
                            var expexdata = pmasters.Where(x => x.FundingType.Equals("EXPEX"));
                            if (expexdata.Any())
                            {
                                foreach (var r in expexdata)
                                {
                                    var splittedyear = DateRangeToMonth.SplitedDateRangeYearly(r.PlanSchedule);
                                    foreach (var spl in splittedyear)
                                    {
                                        if (yearonlystart == spl.Year)
                                        {
                                            double val = 0;
                                            double valSS = 0;
                                            if (r.Plan.Cost != 0 && r.PlanMODSS != 0)
                                            {
                                                val = r.Plan.Cost;
                                                valSS = r.PlanMODSS;
                                            }
                                            else if (r.PlanEDM != 0 && r.PlanEDMSS != 0)
                                            {
                                                val = r.PlanEDM;
                                                valSS = r.PlanEDMSS;
                                            }
                                            else
                                            {
                                                val = r.PlanRT;
                                                valSS = r.PlanRTSS;
                                            }

                                            result.Add(new PMasterDetails
                                            {
                                                DateId = yearonlystart,
                                                Type = "Year",
                                                value = spl.Proportion * val,
                                                valueSS = spl.Proportion * valSS,
                                                isZeroVal = false
                                            });
                                        }
                                        else
                                        {
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = yearonlystart,
                                                Type = "Year",
                                                value = 0,
                                                valueSS = 0,
                                                isZeroVal = true

                                            });
                                        }
                                    }
                                }

                            }

                            yearonlystart = yearonlystart + 1;
                        } while (yearonlystart <= yearonlyfinish);
                        break;
                        #endregion
                    }
                case "Expex: Success Followup Well Costs (Risked)":
                    {
                        #region Expex: Success Followup Well Costs (Risked) SS PR USD mln

                        do
                        {
                            var expexdata = pmasters.Where(x => x.FundingType.Equals("EXPEX SUCCESS"));
                            if (expexdata.Any())
                            {
                                foreach (var r in expexdata)
                                {
                                    var splittedmonth = DateRangeToMonth.SplitedDateRangeMonthly(r.PlanSchedule);
                                    foreach (var spl in splittedmonth)
                                    {
                                        if (monthIdStart == spl.MonthId)
                                        {
                                            double val = 0;
                                            double valSS = 0;
                                            if (r.Plan.Cost != 0 && r.PlanMODSS != 0)
                                            {
                                                val = r.Plan.Cost;
                                                valSS = r.PlanMODSS;
                                            }
                                            else if (r.PlanEDM != 0 && r.PlanEDMSS != 0)
                                            {
                                                val = r.PlanEDM;
                                                valSS = r.PlanEDMSS;
                                            }
                                            else
                                            {
                                                val = r.PlanRT;
                                                valSS = r.PlanRTSS;
                                            }

                                            result.Add(new PMasterDetails
                                            {
                                                DateId = monthIdStart,
                                                Type = "Month",
                                                //value = spl.Proportion * r.Plan.Cost * r.Prob,
                                                value = spl.Proportion * val,
                                                valueSS = spl.Proportion * valSS,
                                                isZeroVal = false
                                            });
                                        }
                                        else
                                        {
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = monthIdStart,
                                                Type = "Month",
                                                value = 0,
                                                valueSS = 0,
                                                isZeroVal = true

                                            });
                                        }
                                    }
                                }


                            }

                            monthIdStart = AddMonthId(monthIdStart, 1);
                        } while (monthIdStart <= monthIdFinish && (monthIdStart != onlyYearValueStart));

                        //var lastMoth = aaa.Max();

                        var yearonlystart = Convert.ToInt32(onlyYearValueStart.ToString().Substring(0, 4));
                        var yearonlyfinish = Convert.ToInt32(monthIdFinish.ToString().Substring(0, 4));
                        do
                        {
                            var expexdata = pmasters.Where(x => x.FundingType.Equals("EXPEX SUCCESS"));
                            if (expexdata.Any())
                            {
                                foreach (var r in expexdata)
                                {
                                    var splittedyear = DateRangeToMonth.SplitedDateRangeYearly(r.PlanSchedule);
                                    foreach (var spl in splittedyear)
                                    {
                                        if (yearonlystart == spl.Year)
                                        {
                                            double val = 0;
                                            double valSS = 0;
                                            if (r.Plan.Cost != 0 && r.PlanMODSS != 0)
                                            {
                                                val = r.Plan.Cost;
                                                valSS = r.PlanMODSS;
                                            }
                                            else if (r.PlanEDM != 0 && r.PlanEDMSS != 0)
                                            {
                                                val = r.PlanEDM;
                                                valSS = r.PlanEDMSS;
                                            }
                                            else
                                            {
                                                val = r.PlanRT;
                                                valSS = r.PlanRTSS;
                                            }
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = yearonlystart,
                                                Type = "Year",
                                                //value = spl.Proportion * r.Plan.Cost * r.Prob,
                                                value = spl.Proportion * val,
                                                valueSS = spl.Proportion * valSS,
                                                isZeroVal = false
                                            });
                                        }
                                        else
                                        {
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = yearonlystart,
                                                Type = "Year",
                                                value = 0,
                                                valueSS = 0,
                                                isZeroVal = true

                                            });
                                        }
                                    }
                                }

                            }

                            yearonlystart = yearonlystart + 1;
                        } while (yearonlystart <= yearonlyfinish);
                        break;
                        #endregion
                    }
                case "Expex: for development activities: C2E":
                    {
                        #region Expex: for development activities: C2E SS PR USD mln

                        do
                        {
                            var expexdata = pmasters.Where(x => x.FundingType.Equals("C2E"));
                            if (expexdata.Any())
                            {
                                foreach (var r in expexdata)
                                {
                                    var splittedmonth = DateRangeToMonth.SplitedDateRangeMonthly(r.PlanSchedule);
                                    foreach (var spl in splittedmonth)
                                    {
                                        if (monthIdStart == spl.MonthId)
                                        {
                                            double val = 0;
                                            double valSS = 0;
                                            if (r.Plan.Cost != 0 && r.PlanMODSS != 0)
                                            {
                                                val = r.Plan.Cost;
                                                valSS = r.PlanMODSS;
                                            }
                                            else if (r.PlanEDM != 0 && r.PlanEDMSS != 0)
                                            {
                                                val = r.PlanEDM;
                                                valSS = r.PlanEDMSS;
                                            }
                                            else
                                            {
                                                val = r.PlanRT;
                                                valSS = r.PlanRTSS;
                                            }

                                            result.Add(new PMasterDetails
                                            {
                                                DateId = monthIdStart,
                                                Type = "Month",
                                                //value = spl.Proportion * r.Plan.Cost * r.Prob,
                                                value = spl.Proportion * val,
                                                valueSS = spl.Proportion * valSS,
                                                
                                                isZeroVal = false
                                            });
                                        }
                                        else
                                        {
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = monthIdStart,
                                                Type = "Month",
                                                value = 0,
                                                valueSS = 0,
                                                isZeroVal = true

                                            });
                                        }
                                    }
                                }


                            }

                            monthIdStart = AddMonthId(monthIdStart, 1);
                        } while (monthIdStart <= monthIdFinish && (monthIdStart != onlyYearValueStart));

                        //var lastMoth = aaa.Max();

                        var yearonlystart = Convert.ToInt32(onlyYearValueStart.ToString().Substring(0, 4));
                        var yearonlyfinish = Convert.ToInt32(monthIdFinish.ToString().Substring(0, 4));
                        do
                        {
                            var expexdata = pmasters.Where(x => x.FundingType.Equals("C2E"));
                            if (expexdata.Any())
                            {
                                foreach (var r in expexdata)
                                {
                                    var splittedyear = DateRangeToMonth.SplitedDateRangeYearly(r.PlanSchedule);
                                    foreach (var spl in splittedyear)
                                    {
                                        if (yearonlystart == spl.Year)
                                        {
                                            double val = 0;
                                            double valSS = 0;
                                            if (r.Plan.Cost != 0 && r.PlanMODSS != 0)
                                            {
                                                val = r.Plan.Cost;
                                                valSS = r.PlanMODSS;
                                            }
                                            else if (r.PlanEDM != 0 && r.PlanEDMSS != 0)
                                            {
                                                val = r.PlanEDM;
                                                valSS = r.PlanEDMSS;
                                            }
                                            else
                                            {
                                                val = r.PlanRT;
                                                valSS = r.PlanRTSS;
                                            }

                                            result.Add(new PMasterDetails
                                            {
                                                DateId = yearonlystart,
                                                Type = "Year",
                                                //value = spl.Proportion * r.Plan.Cost * r.Prob,
                                                value = spl.Proportion * val,
                                                valueSS = spl.Proportion * valSS,
                                                isZeroVal = false
                                            });
                                        }
                                        else
                                        {
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = yearonlystart,
                                                Type = "Year",
                                                value = 0,
                                                valueSS = 0,
                                                isZeroVal = true

                                            });
                                        }
                                    }
                                }

                            }

                            yearonlystart = yearonlystart + 1;
                        } while (yearonlystart <= yearonlyfinish);
                        break;
                        #endregion
                    }
                case "Capex: Wells":
                    {
                        #region Capex: Wells SS PR USD mln
                        do
                        {
                            var expexdata = pmasters.Where(x => x.FundingType.Equals("CAPEX"));
                            if (expexdata.Any())
                            {
                                foreach (var r in expexdata)
                                {
                                    var splittedmonth = DateRangeToMonth.SplitedDateRangeMonthly(r.PlanSchedule);
                                    foreach (var spl in splittedmonth)
                                    {
                                        if (monthIdStart == spl.MonthId)
                                        {
                                            double val = 0;
                                            double valSS = 0;
                                            if (r.Plan.Cost != 0 && r.PlanMODSS != 0)
                                            {
                                                val = r.Plan.Cost;
                                                valSS = r.PlanMODSS;
                                            }
                                            else if (r.PlanEDM != 0 && r.PlanEDMSS != 0)
                                            {
                                                val = r.PlanEDM;
                                                valSS = r.PlanEDMSS;
                                            }
                                            else
                                            {
                                                val = r.PlanRT;
                                                valSS = r.PlanRTSS;
                                            }
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = monthIdStart,
                                                Type = "Month",
                                                //value = spl.Proportion * r.Plan.Cost,
                                                value = spl.Proportion * val,
                                                valueSS = spl.Proportion * valSS,
                                                isZeroVal = false
                                            });
                                        }
                                        else
                                        {
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = monthIdStart,
                                                Type = "Month",
                                                value = 0,
                                                valueSS = 0,
                                                isZeroVal = true

                                            });
                                        }
                                    }
                                }


                            }

                            monthIdStart = AddMonthId(monthIdStart, 1);
                        } while (monthIdStart <= monthIdFinish && (monthIdStart != onlyYearValueStart));

                        //var lastMoth = aaa.Max();

                        var yearonlystart = Convert.ToInt32(onlyYearValueStart.ToString().Substring(0, 4));
                        var yearonlyfinish = Convert.ToInt32(monthIdFinish.ToString().Substring(0, 4));
                        do
                        {
                            var expexdata = pmasters.Where(x => x.FundingType.Equals("CAPEX"));
                            if (expexdata.Any())
                            {
                                foreach (var r in expexdata)
                                {
                                    var splittedyear = DateRangeToMonth.SplitedDateRangeYearly(r.PlanSchedule);
                                    foreach (var spl in splittedyear)
                                    {
                                        if (yearonlystart == spl.Year)
                                        {
                                            double val = 0;
                                            double valSS = 0;
                                            if (r.Plan.Cost != 0 && r.PlanMODSS != 0)
                                            {
                                                val = r.Plan.Cost;
                                                valSS = r.PlanMODSS;
                                            }
                                            else if (r.PlanEDM != 0 && r.PlanEDMSS != 0)
                                            {
                                                val = r.PlanEDM;
                                                valSS = r.PlanEDMSS;
                                            }
                                            else
                                            {
                                                val = r.PlanRT;
                                                valSS = r.PlanRTSS;
                                            }
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = yearonlystart,
                                                Type = "Year",
                                                value = spl.Proportion * val,
                                                valueSS = spl.Proportion * valSS,
                                                isZeroVal = false
                                            });
                                        }
                                        else
                                        {
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = yearonlystart,
                                                Type = "Year",
                                                value = 0,
                                                valueSS = 0,
                                                isZeroVal = true

                                            });
                                        }
                                    }
                                }

                            }
                            yearonlystart = yearonlystart + 1;
                        } while (yearonlystart <= yearonlyfinish);
                        break;
                        #endregion
                    }
                case "Opex: Production Operation [Wells Portion]":
                    {
                        #region Opex: Production Operation SS PR USD mln [Wells Portion]

                        do
                        {
                            var expexdata = pmasters.Where(x => x.FundingType.Equals("OPEX"));
                            if (expexdata.Any())
                            {
                                foreach (var r in expexdata)
                                {
                                    var splittedmonth = DateRangeToMonth.SplitedDateRangeMonthly(r.PlanSchedule);
                                    foreach (var spl in splittedmonth)
                                    {
                                        if (monthIdStart == spl.MonthId)
                                        {
                                            double val = 0;
                                            double valSS = 0;
                                            if (r.Plan.Cost != 0 && r.PlanMODSS != 0)
                                            {
                                                val = r.Plan.Cost;
                                                valSS = r.PlanMODSS;
                                            }
                                            else if (r.PlanEDM != 0 && r.PlanEDMSS != 0)
                                            {
                                                val = r.PlanEDM;
                                                valSS = r.PlanEDMSS;
                                            }
                                            else
                                            {
                                                val = r.PlanRT;
                                                valSS = r.PlanRTSS;
                                            }
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = monthIdStart,
                                                Type = "Month",
                                                //value = spl.Proportion * r.Plan.Cost * r.Prob,
                                                value = spl.Proportion * val,
                                                valueSS = spl.Proportion * valSS,
                                                isZeroVal = false
                                            });
                                        }
                                        else
                                        {
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = monthIdStart,
                                                Type = "Month",
                                                value = 0,
                                                valueSS = 0,
                                                isZeroVal = true

                                            });
                                        }
                                    }
                                }


                            }

                            monthIdStart = AddMonthId(monthIdStart, 1);
                        } while (monthIdStart <= monthIdFinish && (monthIdStart != onlyYearValueStart));

                        //var lastMoth = aaa.Max();

                        var yearonlystart = Convert.ToInt32(onlyYearValueStart.ToString().Substring(0, 4));
                        var yearonlyfinish = Convert.ToInt32(monthIdFinish.ToString().Substring(0, 4));
                        do
                        {
                            var expexdata = pmasters.Where(x => x.FundingType.Equals("OPEX"));
                            if (expexdata.Any())
                            {
                                foreach (var r in expexdata)
                                {
                                    var splittedyear = DateRangeToMonth.SplitedDateRangeYearly(r.PlanSchedule);
                                    foreach (var spl in splittedyear)
                                    {
                                        if (yearonlystart == spl.Year)
                                        {
                                            double val = 0;
                                            double valSS = 0;
                                            if (r.Plan.Cost != 0 && r.PlanMODSS != 0)
                                            {
                                                val = r.Plan.Cost;
                                                valSS = r.PlanMODSS;
                                            }
                                            else if (r.PlanEDM != 0 && r.PlanEDMSS != 0)
                                            {
                                                val = r.PlanEDM;
                                                valSS = r.PlanEDMSS;
                                            }
                                            else
                                            {
                                                val = r.PlanRT;
                                                valSS = r.PlanRTSS;
                                            }
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = yearonlystart,
                                                Type = "Year",
                                                //value = spl.Proportion * r.Plan.Cost * r.Prob,
                                                value = spl.Proportion * val,
                                                valueSS = spl.Proportion * valSS,
                                                isZeroVal = false
                                            });
                                        }
                                        else
                                        {
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = yearonlystart,
                                                Type = "Year",
                                                value = 0,
                                                valueSS = 0,
                                                isZeroVal = true

                                            });
                                        }
                                    }
                                }

                            }

                            yearonlystart = yearonlystart + 1;
                        } while (yearonlystart <= yearonlyfinish);
                        break;
                        #endregion
                    }
                case "Total Abandonment Costs and Site Restoration [Wells Portion]":
                    {
                        #region Total Abandonment Costs and Site Restoration SS PR USD mln [Wells Portion]

                        do
                        {
                            var expexdata = pmasters.Where(x => x.FundingType.Equals("ABEX"));
                            if (expexdata.Any())
                            {
                                foreach (var r in expexdata)
                                {
                                    var splittedmonth = DateRangeToMonth.SplitedDateRangeMonthly(r.PlanSchedule);
                                    foreach (var spl in splittedmonth)
                                    {
                                        if (monthIdStart == spl.MonthId)
                                        {
                                            double val = 0;
                                            double valSS = 0;
                                            if (r.Plan.Cost != 0 && r.PlanMODSS != 0)
                                            {
                                                val = r.Plan.Cost;
                                                valSS = r.PlanMODSS;
                                            }
                                            else if (r.PlanEDM != 0 && r.PlanEDMSS != 0)
                                            {
                                                val = r.PlanEDM;
                                                valSS = r.PlanEDMSS;
                                            }
                                            else
                                            {
                                                val = r.PlanRT;
                                                valSS = r.PlanRTSS;
                                            }
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = monthIdStart,
                                                Type = "Month",
                                                //value = spl.Proportion * r.Plan.Cost * r.Prob,
                                                value = spl.Proportion * val,
                                                valueSS = spl.Proportion * valSS,
                                                isZeroVal = false
                                            });
                                        }
                                        else
                                        {
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = monthIdStart,
                                                Type = "Month",
                                                value = 0,
                                                valueSS = 0,
                                                isZeroVal = true

                                            });
                                        }
                                    }
                                }


                            }

                            monthIdStart = AddMonthId(monthIdStart, 1);
                        } while (monthIdStart <= monthIdFinish && (monthIdStart != onlyYearValueStart));

                        //var lastMoth = aaa.Max();

                        var yearonlystart = Convert.ToInt32(onlyYearValueStart.ToString().Substring(0, 4));
                        var yearonlyfinish = Convert.ToInt32(monthIdFinish.ToString().Substring(0, 4));
                        do
                        {
                            var expexdata = pmasters.Where(x => x.FundingType.Equals("ABEX"));
                            if (expexdata.Any())
                            {
                                foreach (var r in expexdata)
                                {
                                    var splittedyear = DateRangeToMonth.SplitedDateRangeYearly(r.PlanSchedule);
                                    foreach (var spl in splittedyear)
                                    {
                                        if (yearonlystart == spl.Year)
                                        {
                                            double val = 0;
                                            double valSS = 0;
                                            if (r.Plan.Cost != 0 && r.PlanMODSS != 0)
                                            {
                                                val = r.Plan.Cost;
                                                valSS = r.PlanMODSS;
                                            }
                                            else if (r.PlanEDM != 0 && r.PlanEDMSS != 0)
                                            {
                                                val = r.PlanEDM;
                                                valSS = r.PlanEDMSS;
                                            }
                                            else
                                            {
                                                val = r.PlanRT;
                                                valSS = r.PlanRTSS;
                                            }
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = yearonlystart,
                                                Type = "Year",
                                                //value = spl.Proportion * r.Plan.Cost * r.Prob,
                                                value = spl.Proportion * val,
                                                valueSS = spl.Proportion * valSS,
                                                isZeroVal = false
                                            });
                                        }
                                        else
                                        {
                                            result.Add(new PMasterDetails
                                            {
                                                DateId = yearonlystart,
                                                Type = "Year",
                                                value = 0,
                                                valueSS = 0,
                                                isZeroVal = true

                                            });
                                        }
                                    }
                                }

                            }

                            yearonlystart = yearonlystart + 1;
                        } while (yearonlystart <= yearonlyfinish);
                        break;
                        #endregion
                    }
                case "Number of Unrisked Exploration Wells Drilled (whole number)":
                    {
                        break;
                    }
                case "Number of Risked Exploration Wells Drilled (risked number)":
                    {
                        break;
                    }
                case "Number of NFE Only Exploration Wells Completed":
                    {
                        break;
                    }
                case "Number of Development Wells Drilled":
                    {
                        break;
                    }
                case "Number of Development Wells Completed":
                    {
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            #endregion

            var t = result.GroupBy(x => x.DateId);
            List<PMasterDetails> output = new List<PMasterDetails>();
            foreach (var y in t)
            {
                output.Add(new PMasterDetails
                {
                    DateId = y.Key,
                    Type = y.FirstOrDefault().Type,
                    value = y.Sum(x => x.value),
                    valueSS = y.Sum(x => x.valueSS),
                    attribute = attributetype
                });

            }

            return output;
        }

        #region Transform
        //public List<PMasterStandardResult> Transforms(List<WellActivity> was, string updateBy,
        //    string ReportEntity,
        //    string PlanningEntity,
        //    string PlanningEntityID,
        //    string ActivityEntity,
        //    string ActivityEntityID,
        //    double Prob,
        //    int monthIdStart = 201601, int monthIdFinish = 206612, int onlyYearValueStart = 202201)
        //{
        //    DateTime nowDate = DateTime.Now.Date;
        //    List<PMaster> result = new List<PMaster>();
        //    #region Header
        //    foreach (var wa in was)
        //    {
        //        foreach (var t in wa.Phases)
        //        {
        //            if (isHaveBizPlan(wa.WellName, wa.UARigSequenceId, t.ActivityType))
        //            {

        //                // jalankan proses karena status modified dan complete
        //                PMaster p = new PMaster();
        //                p.WellName = wa.WellName;
        //                p.RigName = wa.RigName;
        //                p.UARigSequenceId = wa.UARigSequenceId;
        //                p.ActivityType = t.ActivityType;

        //                p.ReportEntity = ReportEntity;
        //                p.PlanningEntity = PlanningEntity;
        //                p.PlanningEntityID = PlanningEntityID;
        //                p.ActivityEntity = ActivityEntity;
        //                p.ActivityEntityID = ActivityEntityID;
        //                p.Prob = Prob;
        //                p.UpdateVersion = Tools.ToUTC(nowDate, true);

        //                var actcat = DataHelper.Get("WEISActivities", Query.EQ("_id", p.ActivityType)).GetString("ActivityCategory");
        //                p.ActivityCategory = actcat == null ? "" : actcat;
        //                p.BaseOP = t.BaseOP == null ? "" : (t.BaseOP.Count <= 0 ? "" : t.BaseOP.OrderByDescending(x => x).FirstOrDefault());
        //                p.ProjectName = wa.ProjectName;
        //                p.FundingType = t.FundingType;
        //                p.PhSchedule = t.PhSchedule;
        //                p.OP = t.OP;
        //                p.PlanSchedule = t.PlanSchedule;
        //                p.Plan = t.Plan; //MeanCostMOD

        //                //EDM  -- Adanya di Phases.Allocation.annualyBucket.Sum(=>x.meanCostEDM)
        //                //RT -- Adanya di Phases.Allocation
        //                p.UpdateBy = updateBy;
        //                p.IsInPlan = t.IsInPlan;
        //                p.FirmOption = wa.FirmOrOption;
        //                p.WorkingInterest = wa.WorkingInterest;

        //                //if (isSaveMapping)
        //                    p.Save();
        //                result.Add(p);
        //            }
        //        }
        //    }
        //    #endregion
        //    #region Result

        //    List<PMasterStandardResult> results = new List<PMasterStandardResult>();
        //    foreach (var e in PMasterField)
        //    {
        //        PMasterStandardResult res = new PMasterStandardResult();
        //        res.UpdateBy = updateBy;
        //        res.UpdateVersion = Tools.ToUTC(nowDate);

        //        res.ReportEntitiy = ReportEntitiy;
        //        res.PlanningEntity = PlanningEntity;
        //        res.PlanningEntityID = PlanningEntityID;
        //        res.ActivityEntity = ActivityEntity;
        //        res.ActivityEntityID = ActivityEntityID;
        //        res.Prob = Prob;


        //        if (e.Key.Contains("Number"))
        //        {
        //            res.Unit = "Counts";
        //        }
        //        else
        //            res.Unit = "MM USD (MOD)";

        //        res.PMasterField = e.Key;
        //        res.PMasterRef = e.Value;

        //        if (result != null && result.Count() > 0)
        //        {
        //            res.AvgShellShare = result != null ? result.Average(x => x.WorkingInterest) : 0;

        //            #region Total
        //            switch (e.Key)
        //            {
        //                case "Expex: E&A Well Costs (Unrisked) SS PR USD mln":
        //                    {
        //                        res.Total = result.Where(x => x.FundingType.Equals("EXPEX")).Select(x => x.Plan.Cost).Sum();
        //                        var Details = CalcDetails(result, e.Key);
        //                        res.StandardDetails = CalcStandardDetails(Details);
        //                        break;
        //                    }
        //                case "Expex: Success Followup Well Costs (Risked) SS PR USD mln":
        //                    {
        //                        res.Total = result.Where(x => x.FundingType.Equals("EXPEX SUCCESS")).Select(x => x.Plan.Cost).Sum() * Prob;
        //                        var Details = CalcDetails(result, e.Key);
        //                        res.StandardDetails = CalcStandardDetails(Details);


        //                        break;
        //                    }
        //                case "Expex: for development activities: C2E SS PR USD mln":
        //                    {
        //                        res.Total = result.Where(x => x.FundingType.Equals("C2E")).Select(x => x.Plan.Cost).Sum();
        //                        var Details = CalcDetails(result, e.Key);
        //                        res.StandardDetails = CalcStandardDetails(Details);


        //                        break;
        //                    }
        //                case "Capex: Wells SS PR USD mln":
        //                    {
        //                        res.Total = result.Where(x => x.FundingType.Equals("CAPEX")).Select(x => x.Plan.Cost).Sum();
        //                        var Details = CalcDetails(result, e.Key);
        //                        res.StandardDetails = CalcStandardDetails(Details);

        //                        break;
        //                    }
        //                case "Opex: Production Operation SS PR USD mln [Wells Portion]":
        //                    {
        //                        res.Total = result.Where(x => x.FundingType.Equals("OPEX")).Select(x => x.Plan.Cost).Sum();
        //                        var Details = CalcDetails(result, e.Key);
        //                        res.StandardDetails = CalcStandardDetails(Details);


        //                        break;
        //                    }
        //                case "Total Abandonment Costs and Site Restoration SS PR USD mln [Wells Portion]":
        //                    {
        //                        res.Total = result.Where(x => x.FundingType.Equals("ABEX")).Select(x => x.Plan.Cost).Sum();
        //                        var Details = CalcDetails(result, e.Key);
        //                        res.StandardDetails = CalcStandardDetails(Details);

        //                        break;
        //                    }
        //                case "Number of Unrisked Exploration Wells Drilled (whole number)":
        //                    {
        //                        res.Total = 0;
        //                        //CalcDetails(result, e.Key);

        //                        break;
        //                    }
        //                case "Number of Risked Exploration Wells Drilled (risked number)":
        //                    {
        //                        res.Total = 0;
        //                        break;
        //                    }
        //                case "Number of NFE Only Exploration Wells Completed":
        //                    {
        //                        res.Total = 0;
        //                        break;
        //                    }
        //                case "Number of Development Wells Drilled":
        //                    {
        //                        res.Total = 0;
        //                        break;
        //                    }
        //                case "Number of Development Wells Completed":
        //                    {
        //                        res.Total = 0;
        //                        break;
        //                    }
        //                default:
        //                    {
        //                        res.Total = 0;
        //                        break;
        //                    }
        //            }
        //            #endregion
        //        }



        //        results.Add(res);
        //    }


        //    #endregion


        //    return results;
        //}
        #endregion

        public List<PMasterStandardResult> Transforms(List<PMasterStandard> result, string updateBy,
            string ReportEntity,
            string PlanningEntity,
            string PlanningEntityID,
            string ActivityEntity,
            string ActivityEntityID,
            double Prob, ParsePalentirQuery pq, bool onlyNoFundingType = false,
            int monthIdStart = 201601, int monthIdFinish = 206612, int onlyYearValueStart = 202201)
        {
            DateTime nowDate = DateTime.Now.Date;

            #region Result

            List<PMasterStandardResult> results = new List<PMasterStandardResult>();

            if (!onlyNoFundingType)
            {
                #region transform data with valid funding type
                foreach (var e in PMasterField)
                {
                    PMasterStandardResult res = new PMasterStandardResult();
                    res.UpdateBy = updateBy;
                    res.UpdateVersion = Tools.ToUTC(nowDate);

                    res.ReportEntity = ReportEntity;
                    res.PlanningEntity = PlanningEntity;
                    res.PlanningEntityID = PlanningEntityID;
                    res.ActivityEntity = ActivityEntity;
                    res.ActivityEntityID = ActivityEntityID;
                    res.Prob = Prob;

                    var mapname = result.GroupBy(x => x.MapName).Select(x => x.Key).ToList();
                    res.MapName = mapname.ElementAt(0);

                    string sstg = "";
                    switch (pq.SSTG)
                    {
                        case "Shell Share":
                            sstg = "SS";
                            break;
                        case "Total Gross":
                            sstg = "TG";
                            break;
                    }

                    if (e.Key.Contains("Number"))
                    {
                        res.Unit = "Counts";
                    }
                    else
                    {
                        res.Unit = "MM " + pq.Currency + " (" + pq.MoneyType + ") " + sstg;
                    }

                    res.PMasterField = e.Key;
                    res.PMasterRef = e.Value;

                    if (result != null && result.Count() > 0)
                    {
                        res.AvgShellShare = result != null ? result.Average(x => x.WorkingInterest) : 0;

                        #region Total
                        switch (e.Key)
                        {
                            case "Expex: E&A Well Costs (Unrisked)":
                                {
                                    res.Total = result.Where(x => x.FundingType != null && x.FundingType.Equals("EXPEX")).Select(x => x.Plan.Cost).Sum();
                                    var Details = CalcDetails(result, e.Key, pq.MoneyType);
                                    res.StandardDetails = CalcStandardDetails(Details);
                                    break;
                                }
                            case "Expex: Success Followup Well Costs (Risked)":
                                {
                                    res.Total = result.Where(x => x.FundingType != null && x.FundingType.Equals("EXPEX SUCCESS")).Select(x => x.Plan.Cost).Sum() * Prob;
                                    var Details = CalcDetails(result, e.Key, pq.MoneyType, probability: Prob);
                                    res.StandardDetails = CalcStandardDetails(Details);


                                    break;
                                }
                            case "Expex: for development activities: C2E":
                                {
                                    res.Total = result.Where(x => x.FundingType != null && x.FundingType.Equals("C2E")).Select(x => x.Plan.Cost).Sum();
                                    var Details = CalcDetails(result, e.Key, pq.MoneyType);
                                    res.StandardDetails = CalcStandardDetails(Details);


                                    break;
                                }
                            case "Capex: Wells":
                                {
                                    res.Total = result.Where(x => x.FundingType != null && x.FundingType.Equals("CAPEX")).Select(x => x.Plan.Cost).Sum();
                                    var Details = CalcDetails(result, e.Key, pq.MoneyType);
                                    res.StandardDetails = CalcStandardDetails(Details);

                                    break;
                                }
                            case "Opex: Production Operation [Wells Portion]":
                                {
                                    res.Total = result.Where(x => x.FundingType != null && x.FundingType.Equals("OPEX")).Select(x => x.Plan.Cost).Sum();
                                    var Details = CalcDetails(result, e.Key, pq.MoneyType);
                                    res.StandardDetails = CalcStandardDetails(Details);


                                    break;
                                }
                            case "Total Abandonment Costs and Site Restoration [Wells Portion]":
                                {
                                    res.Total = result.Where(x => x.FundingType != null && x.FundingType.Equals("ABEX")).Select(x => x.Plan.Cost).Sum();
                                    var Details = CalcDetails(result, e.Key, pq.MoneyType);
                                    res.StandardDetails = CalcStandardDetails(Details);

                                    break;
                                }
                            case "Number of Unrisked Exploration Wells Drilled (whole number)":
                                {
                                    res.Total = 0;
                                    //CalcDetails(result, e.Key);

                                    break;
                                }
                            case "Number of Risked Exploration Wells Drilled (risked number)":
                                {
                                    res.Total = 0;
                                    break;
                                }
                            case "Number of NFE Only Exploration Wells Completed":
                                {
                                    res.Total = 0;
                                    break;
                                }
                            case "Number of Development Wells Drilled":
                                {
                                    res.Total = 0;
                                    break;
                                }
                            case "Number of Development Wells Completed":
                                {
                                    res.Total = 0;
                                    break;
                                }
                            default:
                                {
                                    res.Total = 0;
                                    break;
                                }
                        }
                        #endregion
                    }



                    results.Add(res);
                }
                #endregion
            }
            else
            {
                #region transform data with invalid funding type
                foreach (var e in PMasterFieldNoFundingType)
                {
                    PMasterStandardResult res = new PMasterStandardResult();
                    res.UpdateBy = updateBy;
                    res.UpdateVersion = Tools.ToUTC(nowDate);

                    res.ReportEntity = ReportEntity;
                    res.PlanningEntity = PlanningEntity;
                    res.PlanningEntityID = PlanningEntityID;
                    res.ActivityEntity = ActivityEntity;
                    res.ActivityEntityID = ActivityEntityID;
                    res.Prob = Prob;

                    var mapname = result.GroupBy(x => x.MapName).Select(x => x.Key).ToList();
                    res.MapName = mapname.ElementAt(0);

                    string sstg = "";
                    switch (pq.SSTG)
                    {
                        case "Shell Share":
                            sstg = "SS";
                            break;
                        case "Total Gross":
                            sstg = "TG";
                            break;
                    }

                    if (e.Key.Contains("Number"))
                    {
                        res.Unit = "Counts";
                    }
                    else
                    {
                        res.Unit = "MM " + pq.Currency + " (" + pq.MoneyType + ") " + sstg;
                    }

                    res.PMasterField = e.Key;
                    res.PMasterRef = e.Value;

                    if (result != null && result.Count() > 0)
                    {
                        res.AvgShellShare = result != null ? result.Average(x => x.WorkingInterest) : 0;

                        #region Total
                        switch (e.Key)
                        {
                            case "No Funding Type":
                                {
                                    res.Total = result.Where(x => x.FundingType == null || (!x.FundingType.Equals("CAPEX") && !x.FundingType.Equals("EXPEX") && !x.FundingType.Equals("ABEX") && !x.FundingType.Equals("C2E") && !x.FundingType.Equals("EXPEX SUCCESS") && !x.FundingType.Equals("OPEX"))).Select(x => x.Plan.Cost).Sum();
                                    var Details = CalcDetails(result, e.Key, pq.MoneyType);
                                    res.StandardDetails = CalcStandardDetails(Details);
                                    break;
                                }
                        }
                        #endregion
                    }



                    results.Add(res);
                }
                #endregion
            }
            


            #endregion


            return results;
        }

        public PMasterStandard TransformsToMapBizPlan(BizPlanActivity wa, BizPlanActivityPhase t, string updateBy, DateTime nowDate,
      bool isSaveMapping, PMasterStandard p = null)
        {
            if(p == null) p = new PMasterStandard();

            try
            {
                #region Header
                p.WellName = wa.WellName;
                p.RigName = t.Estimate.RigName;
                p.UARigSequenceId = wa.UARigSequenceId;
                p.ActivityType = t.ActivityType;

                p.UpdateVersion = Tools.ToUTC(nowDate, true);
                var actgh = DataHelper.Get("WEISActivities", Query.EQ("_id", p.ActivityType));

                var actcat = actgh == null ? "" : actgh.GetString("ActivityCategory");
                p.ActivityCategory = actcat == null ? "" : actcat;

                if (p.ActivityType.Equals("RIG - IDLE"))
                    p.ActivityCategory = p.ActivityType;
                p.BaseOP = t.Estimate.SaveToOP;  // t.BaseOP == null ? "" : (t.BaseOP.Count <= 0 ? "" : t.BaseOP.OrderByDescending(x => x).FirstOrDefault());
                p.ProjectName = wa.ProjectName;
                p.FundingType = t.FundingType;
                p.AssetName = wa.AssetName;
                p.PhSchedule = t.PhSchedule;
                p.OP = t.OP;
                p.PlanSchedule = t.Estimate.EstimatePeriod;
                p.Plan = t.Plan; //MeanCostMOD

                //EDM  -- Adanya di Phases.Allocation.annualyBucket.Sum(=>x.meanCostEDM)
                //RT -- Adanya di Phases.Allocation
                //var getCost = BizPlanAllocation.Get<BizPlanAllocation>(Query.And(
                //        Query.EQ("WellName", p.WellName),
                //        Query.EQ("ActivityType", p.ActivityType),
                //        Query.EQ("UARigSequenceId", p.UARigSequenceId)));

                var getCost = t.Allocation;

                if (getCost != null)
                {
                    p.PlanMODSS = getCost.AnnualyBuckets.Sum(x => x.ShellShare * p.Plan.Cost); //MODSS

                    p.PlanEDM = getCost.AnnualyBuckets.Sum(x => x.MeanCostEDM); //EDM
                    p.PlanEDMSS = getCost.AnnualyBuckets.Sum(x => x.ShellShare * x.MeanCostEDM); //EDMSS

                    p.PlanRT = getCost.AnnualyBuckets.Sum(x => x.MeanCostRealTerm); //REALTERM
                    p.PlanRTSS = getCost.AnnualyBuckets.Sum(x => x.ShellShare * x.MeanCostRealTerm); //REALTERMSS

                }

                p.UpdateBy = updateBy;
                p.IsInPlan = wa.isInPlan;
                p.FirmOption = wa.isInPlan ? "In Plan" :  "Not In Plan";

                p.WorkingInterest = wa.WorkingInterest;
                p.Allocation = t.Allocation;

                if (isSaveMapping)
                    p.Save();

                #endregion
            }
            catch (Exception ex)
            {
                return null;
            }
            return p;
        }


        public PMasterStandard TransformsToMap(WellActivity wa, WellActivityPhase t, string updateBy, DateTime nowDate,
           bool isSaveMapping)
        {
            PMasterStandard p = new PMasterStandard();

            try
            {
                #region Header
                p.WellName = wa.WellName;
                p.RigName = wa.RigName;
                p.UARigSequenceId = wa.UARigSequenceId;
                p.ActivityType = t.ActivityType;

                BizPlanActivity bp = null;
                PCapex.isHaveBizPlan(p.WellName, p.UARigSequenceId, p.ActivityType, out bp);
                if (bp != null)
                {
                    p.LoB = bp.LineOfBusiness;
                }

                p.UpdateVersion = Tools.ToUTC(nowDate, true);
                var actgh = DataHelper.Get("WEISActivities", Query.EQ("_id", p.ActivityType));

                var actcat = actgh == null ? "" : actgh.GetString("ActivityCategory");
                p.ActivityCategory = actcat == null ? "" : actcat;

                if (p.ActivityType.Equals("RIG - IDLE"))
                    p.ActivityCategory = p.ActivityType;
                p.BaseOP = t.BaseOP == null ? "" : (t.BaseOP.Count <= 0 ? "" : t.BaseOP.OrderByDescending(x => x).FirstOrDefault());
                p.ProjectName = wa.ProjectName;
                p.FundingType = t.FundingType;
                p.AssetName = wa.AssetName;
                //p.PhSchedule = t.PhSchedule;
                p.OP = t.OP;
                //p.PlanSchedule = t.PlanSchedule;
                p.Plan = t.Plan; //MeanCostMOD

                //EDM  -- Adanya di Phases.Allocation.annualyBucket.Sum(=>x.meanCostEDM)
                //RT -- Adanya di Phases.Allocation
                //var getCost = BizPlanAllocation.Populate<BizPlanAllocation>(Query.And(
                //    Query.EQ("WellName", p.WellName),
                //    Query.EQ("ActivityType", p.ActivityType),
                //    Query.EQ("UARigSequenceId", p.UARigSequenceId)));
                var getCost = BizPlanAllocation.Get<BizPlanAllocation>(Query.And(
                        Query.EQ("WellName", p.WellName),
                        Query.EQ("ActivityType", p.ActivityType),
                        Query.EQ("UARigSequenceId", p.UARigSequenceId)));

                if (getCost != null)
                {
                    //p.PlanEDM = getCost.FirstOrDefault().AnnualyBuckets.Sum(x => x.MeanCostEDM); //EDM
                    //p.PlanRT = getCost.FirstOrDefault().AnnualyBuckets.Sum(x => x.MeanCostRealTerm); //REALTERM
                    p.PlanMODSS = getCost.AnnualyBuckets.Sum(x => x.ShellShare * p.Plan.Cost); //MODSS

                    p.PlanEDM = getCost.AnnualyBuckets.Sum(x => x.MeanCostEDM); //EDM
                    p.PlanEDMSS = getCost.AnnualyBuckets.Sum(x => x.MeanCostEDMSS); //EDMSS

                    p.PlanRT = getCost.AnnualyBuckets.Sum(x => x.MeanCostRealTerm); //REALTERM
                    p.PlanRTSS = getCost.AnnualyBuckets.Sum(x => x.MeanCostRealTermSS); //REALTERMSS
                    
                }

                p.Allocation = getCost;
                p.PlanSchedule = getCost.EstimatePeriod;
                p.UpdateBy = updateBy;
                p.IsInPlan = t.IsInPlan;
                p.FirmOption = wa.FirmOrOption;

                p.WorkingInterest = wa.WorkingInterest;

                if (isSaveMapping)
                    p.Save();

                #endregion
            }
            catch (Exception ex)
            {
                return null;
            }
            return p;
        }


        public int AddMonthId(int monthId, int increment, int passifvalue = 12)
        {
            var year = Convert.ToInt32(monthId.ToString().Substring(0, 4));
            var month = Convert.ToInt32(monthId.ToString().Substring(4, 2));

            //List<string> list = new List<string>();

            for (int i = 1; i <= increment; i++)
            {

                if ((month + 1) > passifvalue)
                {
                    month = 1;
                    year++;
                }
                else
                {
                    month++;
                }
                // list.Add(year + month.ToString("00"));

            }
            return Convert.ToInt32(year + month.ToString("00"));
        }

        public List<PMasterDetails> CalcStandardDetails(List<PMasterDetails> details)
        {
            List<PMasterDetails> results = new List<PMasterDetails>();
            if (details.Any())
            {
                var grp = details.Where(x => x.Type.Equals("Month")).GroupBy(x => x.DateId.ToString().Substring(0, 4));
                foreach (var t in grp)
                {
                    results.Add(new PMasterDetails
                    {
                        attribute = t.FirstOrDefault().attribute,
                        DateId = Convert.ToInt32(t.Key),
                        isZeroVal = t.FirstOrDefault().isZeroVal,
                        Type = "Year",
                        value = t.Sum(x => x.value),
                        valueSS = t.Sum(x => x.valueSS)
                    });
                }
                var year = details.Where(x => x.Type.Equals("Year"));
                foreach (var t in year)
                {
                    results.Add(t);
                }
            }
            return results;

        }

    }

    public class PMasterStandardResult : ECISModel
    {
        public override string TableName
        {
            get { return "WEISPalantirMasterStandardResult"; }
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            //DateTime createDate = Tools.ToUTC(DateTime.Now, true);
            DateTime createDate = DateTime.Now;
            UpdateVersion = new DateTime(createDate.Year, createDate.Month, createDate.Day, createDate.Hour, createDate.Minute, createDate.Second);
            _id = String.Format("I{0}S{1}A{2}M{3}",
                ActivityEntityID.Replace(" ", "").Replace("-", ""),
                UpdateBy.Replace(" ", "").Replace("-", ""),
                //UpdateVersion.ToString("yyMMddHHmmss"),
                PMasterField.Replace(" ", "").Replace("-", ""),
                MapName.Replace(" ", "").Replace("-", "")
                );
            return this.ToBsonDocument();
        }

        public string UpdateBy { get; set; }
        public DateTime UpdateVersion { get; set; }

        public string ReportEntity { get; set; }
        public string PlanningEntity { get; set; }
        public string PlanningEntityID { get; set; }
        public string ActivityEntity { get; set; }
        public string ActivityEntityID { get; set; }
        public double Prob { get; set; }
        public string MapName { get; set; }
        public double AvgShellShare { get; set; }
        public string Unit { get; set; }
        public string PMasterField { get; set; }
        public string PMasterRef { get; set; }
        public bool IsGenerated { get; set; }
        //public List<PMasterDetails> Details { get; set; }
        public List<PMasterDetails> StandardDetails { get; set; }

        public double Total { get; set; }

        public PMasterStandardResult()
        {
            //Details = new List<PMasterDetails>();
            StandardDetails = new List<PMasterDetails>();
        }

        
    }
}
