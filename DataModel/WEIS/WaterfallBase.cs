using ECIS.Client.WEIS;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
//using System.Web.Mvc;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Diagnostics;
namespace ECIS.Client.WEIS
{

    public class WaterFallHelplerValue
    {
        public int Year { get; set; }
        public double NoOfDay { get; set; }
        public DateRange SplitedDate { get; set; }
        public string Identity { get; set; }
        public double Proportion { get; set; }
        public WellDrillData Value { get; set; }

        public WaterFallHelplerValue()
        {
            Value = new WellDrillData();
            SplitedDate = new DateRange();
        }

        // for PIP
        public string PIPIdea { get; set; }
        public string PIPClassification { get; set; }


    }
    public class WaterFallHelper
    {
        public List<WaterFallHelplerValue> Values { get; set; }
        public List<WaterFallHelplerValue> PIPValues { get; set; }
        public WellDrillData OP { get; set; }
        public WellDrillData LE { get; set; }
        //public double Ratio { get; set; }
        public DateRange OPSchedule { get; set; }
        public DateRange LESchedule { get; set; }
        public string WellName { get; set; }
        public string ActivityType { get; set; }
        public string SequenceID { get; set; }
        public int PhaseNo { get; set; }

        public string BaseOP { get; set; }

        public WaterFallHelper()
        {
            OP = new WellDrillData();
            LE = new WellDrillData();
            Values = new List<WaterFallHelplerValue>();
            OPSchedule = new DateRange();
            LESchedule = new DateRange();
            PIPValues = new List<WaterFallHelplerValue>();
        }
    }

    public class WaterfallBase
    {
        public List<string> Status { get; set; }
        public List<string> Currency { get; set; }
        public List<string> MoneyType { get; set; }
        public string SSorTG { get; set; }
        public List<string> Regions { set; get; }
        public List<string> OperatingUnits { set; get; }
        public List<string> RigTypes { set; get; }

        public List<string> RigNames { set; get; }
        public List<string> ProjectNames { set; get; }
        public List<string> WellNames { set; get; }

        public List<string> PerformanceUnits { set; get; }
        public List<string> ExType { set; get; }
        public List<string> Activities { set; get; }
        public List<string> ActivitiesCategory { set; get; }

        public List<string> LineOfBusiness { get; set; }

        public string PeriodBase { set; get; }
        public string PeriodView { set; get; }

        public string DateStart { set; get; }
        public string DateFinish { set; get; }
        public string DateStart2 { set; get; }

        public bool inlastuploadls { get; set; }
        public string inlastuploadlsBoth { get; set; }

        public string DateFinish2 { set; get; }

        public string DateRelation { set; get; }

        public int FiscalYearStart { set; get; }
        public int FiscalYearFinish { set; get; }

        public string DayOrCost { set; get; }
        public string GroupBy { set; get; }
        public string CumulativeDataType { set; get; }
        public bool IncludeCR { set; get; }
        public bool IncludeGaps { set; get; }
        public bool IncludeZero { set; get; }
        public int AllocationYear { set; get; }

        public List<string> OPs { set; get; }

        public string opRelation { get; set; }
        public string firmoption { get; set; }

        public string BaseOP { set; get; }
        public string GetWhatData { set; get; }
        public string DataFor { set; get; }
        public bool ValidLSOnly { set; get; }
        public bool ShellShare { get; set; }
        public List<bool> isInPlan { get; set; }
        public string showdataby { get; set; }

        private string GetPeriodBaseField()
        {
            //if (GetWhatData != null)
            //    if (GetWhatData.Equals("OP"))
            //        return "PlanSchedule";

            if ("By Last Sequence".ToLower().Equals(PeriodBase.ToLower()) || "PhSchedule".Equals(PeriodBase))
                return "PhSchedule";

            if ("By Last Estimate".ToLower().Equals(PeriodBase.ToLower()) || "LESchedule".Equals(PeriodBase))
                return "LESchedule";

            if ("By Plan Schedule".ToLower().Equals(PeriodBase.ToLower()) || "PlanSchedule".Equals(PeriodBase))
                return "PlanSchedule";

            return "PhSchedule";
        }

        private void PareseFilter()
        {
            if (DayOrCost == null || DayOrCost == "")
                DayOrCost = "Cost";

            if (GroupBy == null || GroupBy == "")
                GroupBy = "Classification";
        }

        public DateRange GetFilterDateRange()
        {
            var now = DateTime.Now;
            var result = new DateRange()
            {
                Start = Tools.ToUTC(new DateTime(now.Year, 1, 1)),
                Finish = Tools.ToUTC(new DateTime(now.Year, 12, 31))
            };

            DateStart = (DateStart == null ? "" : DateStart);
            DateFinish = (DateFinish == null ? "" : DateFinish);
            DateStart2 = (DateStart2 == null ? "" : DateStart2);
            DateFinish2 = (DateFinish2 == null ? "" : DateFinish2);

            string format = "yyyy-MM-dd";
            CultureInfo culture = CultureInfo.InvariantCulture;

            if (PeriodView.ToLower().Contains("fiscal"))
            {
                result.Start = new DateTime[] { 
                    DateTime.ParseExact(DateStart, format, culture),
                    DateTime.ParseExact(DateStart2, format, culture)
                }.Min();
                result.Finish = new DateTime[] { 
                    DateTime.ParseExact(DateFinish, format, culture),
                    DateTime.ParseExact(DateFinish2, format, culture)
                }.Min();

                result.Start = DateTime.ParseExact(DateStart, format, culture);
                result.Finish = DateTime.ParseExact(DateFinish, format, culture);
            }

            if (DateStart != "" && DateStart2 != "")
                result.Start = new DateTime[] { DateTime.ParseExact(DateStart, format, culture), DateTime.ParseExact(DateStart2, format, culture) }.Min();
            else if (DateStart != "")
                result.Start = DateTime.ParseExact(DateStart, format, culture);
            else if (DateStart2 != "")
                result.Start = DateTime.ParseExact(DateStart2, format, culture);
            else
                result.Start = new DateTime(2000, 1, 1);

            if (DateFinish != "" && DateFinish2 != "")
                result.Finish = new DateTime[] { DateTime.ParseExact(DateFinish, format, culture), DateTime.ParseExact(DateFinish2, format, culture) }.Min();
            else if (DateStart != "")
                result.Finish = DateTime.ParseExact(DateFinish, format, culture);
            else if (DateStart2 != "")
                result.Finish = DateTime.ParseExact(DateFinish2, format, culture);
            else
                result.Finish = new DateTime(3000, 12, 31);

            return result;
        }

        private IMongoQuery ParsePeriodQuery()
        {
            DateRelation = (DateRelation == null ? "AND" : DateRelation);
            DateStart = (DateStart == null ? "" : DateStart);
            DateFinish = (DateFinish == null ? "" : DateFinish);
            DateStart2 = (DateStart2 == null ? "" : DateStart2);
            DateFinish2 = (DateFinish2 == null ? "" : DateFinish2);

            var queries = new List<IMongoQuery>();
            string format = "yyyy-MM-dd";
            CultureInfo culture = CultureInfo.InvariantCulture;
            var period = GetPeriodBaseField();

            var periodStart = Tools.ToUTC(DateStart != "" ? DateTime.ParseExact(DateStart, format, culture) : new DateTime(1900, 1, 1));
            var periodFinish = Tools.ToUTC(DateFinish != "" ? DateTime.ParseExact(DateFinish, format, culture) : new DateTime(3000, 1, 1));
            var periodStart2 = Tools.ToUTC(DateStart2 != "" ? DateTime.ParseExact(DateStart2, format, culture) : new DateTime(1900, 1, 1));
            var periodFinish2 = Tools.ToUTC(DateFinish2 != "" ? DateTime.ParseExact(DateFinish2, format, culture) : new DateTime(3000, 1, 1));

            var PeriodStartField = "Phases." + period + ".Start";
            var PeriodFinishField = "Phases." + period + ".Finish";
            if (GetWhatData == "OP")
            {
                PeriodStartField = "PsSchedule.Start";
                PeriodFinishField = "PsSchedule.Finish";
            }
            if (PeriodView != null && PeriodView.ToLower().Contains("fiscal"))
            {
                #region old
                //var queryDateStart = Query.And(
                //    Query.GTE(PeriodStartField, periodStart),
                //    Query.LTE(PeriodStartField, periodFinish)
                //);

                //var queryDateFinish = Query.And(
                //    Query.GTE(PeriodFinishField, periodStart),
                //    Query.LTE(PeriodFinishField, periodFinish)
                //);

                //return Query.And(queryDateStart, queryDateFinish);
                #endregion
                return Query.And(Query.GTE(PeriodStartField, periodStart), Query.LT(PeriodStartField, periodFinish.AddDays(1)));
            }

            if (!DateStart.Equals("") && !DateFinish.Equals(""))
                queries.Add(Query.And(
                    Query.GTE(PeriodStartField, periodStart),
                    Query.LTE(PeriodStartField, periodFinish)
                ));
            else if (!DateStart.Equals(""))
                queries.Add(
                    Query.GTE(PeriodStartField, periodStart)
                );
            else if (!DateFinish.Equals(""))
                queries.Add(
                    Query.LTE(PeriodStartField, periodFinish)
                );

            if (!DateStart2.Equals("") && !DateFinish2.Equals(""))
                queries.Add(Query.And(
                    Query.GTE(PeriodFinishField, periodStart2),
                    Query.LTE(PeriodFinishField, periodFinish2)
                ));
            else if (!DateStart2.Equals(""))
                queries.Add(
                    Query.GTE(PeriodFinishField, periodStart2)
                );
            else if (!DateFinish2.Equals(""))
                queries.Add(
                    Query.LTE(PeriodFinishField, periodFinish2)
                );


            if (queries.Count > 0)
                return DateRelation.Equals("AND") ? Query.And(queries.ToArray()) : Query.Or(queries.ToArray());

            return null;
        }

        private string ParsePeriodFullQuery()
        {
            DateRelation = (DateRelation == null ? "AND" : DateRelation);
            DateRelation = "$" + DateRelation.ToLower();
            DateStart = (DateStart == null ? "" : DateStart);
            DateFinish = (DateFinish == null ? "" : DateFinish);
            DateStart2 = (DateStart2 == null ? "" : DateStart2);
            DateFinish2 = (DateFinish2 == null ? "" : DateFinish2);

            var queries = new List<IMongoQuery>();
            string format = "yyyy-MM-dd";
            CultureInfo culture = CultureInfo.InvariantCulture;
            var period = GetPeriodBaseField();

            var periodStart = Tools.ToUTC(DateStart != "" ? DateTime.ParseExact(DateStart, format, culture) : new DateTime(1900, 1, 1));
            var periodFinish = Tools.ToUTC(DateFinish != "" ? DateTime.ParseExact(DateFinish, format, culture) : new DateTime(3000, 1, 1));
            var periodStart2 = Tools.ToUTC(DateStart2 != "" ? DateTime.ParseExact(DateStart2, format, culture) : new DateTime(1900, 1, 1));
            var periodFinish2 = Tools.ToUTC(DateFinish2 != "" ? DateTime.ParseExact(DateFinish2, format, culture) : new DateTime(3000, 1, 1));

            var PeriodStartField = "Phases." + period + ".Start";
            var PeriodFinishField = "Phases." + period + ".Finish";
            if (GetWhatData == "OP")
            {
                PeriodStartField = "PsSchedule.Start";
                PeriodFinishField = "PsSchedule.Finish";
            }
            if (PeriodView != null && PeriodView.ToLower().Contains("fiscal"))
            {
                // return Query.Or(Query.GTE(PeriodStartField, periodStart), Query.LT(PeriodStartField, periodFinish.AddDays(1)));
            }
            string result = "";
            result = "" +
               "'" + DateRelation + "':[" +
                   "{ '" + PeriodStartField + "' : { '$gte': ISODate('" + periodStart.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000'), '$lte': ISODate('" + periodFinish.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000')}}," +
                   "{ '" + PeriodFinishField + "': { '$gte': ISODate('" + periodStart.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000'), '$lte': ISODate('" + periodFinish.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000')}}" +
               "]";
            return result;
        }

        private IMongoQuery ParseQuery()
        {
            var queries = new List<IMongoQuery>();

            if (Regions != null && Regions.Count > 0)
                queries.Add(Query.In("Region", new BsonArray(Regions)));

            if (OperatingUnits != null && OperatingUnits.Count > 0)
                queries.Add(Query.In("OperatingUnit", new BsonArray(OperatingUnits)));

            if (RigTypes != null && RigTypes.Count > 0)
                queries.Add(Query.In("RigType", new BsonArray(RigTypes)));

            if (RigNames != null && RigNames.Count > 0)
                queries.Add(Query.In("RigName", new BsonArray(RigNames)));

            if (ProjectNames != null && ProjectNames.Count > 0)
                queries.Add(Query.In("ProjectName", new BsonArray(ProjectNames)));

            if (WellNames != null && WellNames.Count > 0)
                queries.Add(Query.In("WellName", new BsonArray(WellNames)));

            if (PerformanceUnits != null && PerformanceUnits.Count > 0)
                queries.Add(Query.In("PerformanceUnit", new BsonArray(PerformanceUnits)));

            if (ExType != null && ExType.Count > 0)
                queries.Add(Query.In("EXType", new BsonArray(ExType)));

            //Firm or Option
            switch (firmoption)
            {
                case null:
                    break;
                case "Firm":
                    queries.Add(Query.EQ("FirmOrOption", "Firm"));
                    break;
                case "Option":
                    queries.Add(Query.EQ("FirmOrOption", "Option"));
                    break;
                case "NotFirm":
                    queries.Add(Query.NE("FirmOrOption", "Firm"));
                    break;
                case "NotOption":
                    queries.Add(Query.NE("FirmOrOption", "Option"));
                    break;
                case "Both":
                    queries.Add(Query.In("FirmOrOption", new BsonArray(new string[] { "Firm", "Option" })));
                    break;
                case "NotBoth":
                    queries.Add(Query.NotIn("FirmOrOption", new BsonArray(new string[] { "Firm", "Option" })));
                    break;
                case "All":
                    break;
                default:
                    queries.Add(Query.EQ("FirmOrOption", firmoption));
                    break;
            }

            ////////////////////////////////////////////////////
            if (OPs != null)
            {
                if (OPs.Count == 1)
                {
                    queries.Add(Query.EQ("Phases.BaseOP", OPs.FirstOrDefault()));
                }
                else
                {
                    if (opRelation == "OR")
                    {
                        queries.Add(Query.In("Phases.BaseOP", new BsonArray(OPs)));
                    }
                    else
                    {
                        List<IMongoQuery> queriessss = new List<IMongoQuery>();
                        foreach (var op in OPs)
                        {
                            queriessss.Add(Query.EQ("Phases.BaseOP", op));
                        }

                        queries.Add(Query.And(queriessss));
                    }
                }
            }
            ////////////////////////////////////////////////////





            if (Activities != null && Activities.Count > 0)
            {
                queries.Add(Query.In("Phases.ActivityType", new BsonArray(Activities)));
            }
            else
            {
                if (ActivitiesCategory != null && ActivitiesCategory.Count > 0)
                {
                    var getAct = ActivityMaster.Populate<ActivityMaster>(Query.In("ActivityCategory", new BsonArray(ActivitiesCategory)));
                    if (getAct.Any())
                    {
                        queries.Add(Query.In("Phases.ActivityType", new BsonArray(getAct.Select(d => d._id.ToString()).ToList())));
                    }
                }
            }

            var periodQuery = ParsePeriodQuery();
            if (periodQuery != null)
                queries.Add(periodQuery);
            //isInPlan WellActivity 
            if (isInPlan != null && isInPlan.Count() > 0)
            {
                queries.Add(Query.EQ("Phases.IsInPlan", isInPlan.FirstOrDefault()));
            }

            if (queries.Count > 0)
                return Query.And(queries.ToArray());

            return null;
        }

        public IMongoQuery ParseQueryForBizPlan()
        {
            var queries = new List<IMongoQuery>();

            if (Status != null && Status.Count > 0)
                queries.Add(Query.In("Phases.Estimate.Status", new BsonArray(Status)));

            if (Regions != null && Regions.Count > 0)
                queries.Add(Query.In("Region", new BsonArray(Regions)));

            if (OperatingUnits != null && OperatingUnits.Count > 0)
                queries.Add(Query.In("OperatingUnit", new BsonArray(OperatingUnits)));

            if (RigTypes != null && RigTypes.Count > 0)
                queries.Add(Query.In("RigType", new BsonArray(RigTypes)));

            if (LineOfBusiness != null && LineOfBusiness.Count > 0)
                if (!LineOfBusiness.FirstOrDefault().Equals(""))
                    queries.Add(Query.In("LineOfBusiness", new BsonArray(LineOfBusiness)));

            if (RigNames != null && RigNames.Count > 0)
                queries.Add(Query.In("RigName", new BsonArray(RigNames)));

            if (ProjectNames != null && ProjectNames.Count > 0)
                queries.Add(Query.In("ProjectName", new BsonArray(ProjectNames)));

            if (WellNames != null && WellNames.Count > 0)
                queries.Add(Query.In("WellName", new BsonArray(WellNames)));

            if (PerformanceUnits != null && PerformanceUnits.Count > 0)
            {
                queries.Add(Query.In("PerformanceUnit", new BsonArray(PerformanceUnits)));
            }

            if (ExType != null && ExType.Count > 0)
                queries.Add(Query.In("Phases.FundingType", new BsonArray(ExType)));


            if (Activities != null && Activities.Count > 0)
            {
                queries.Add(Query.In("Phases.ActivityType", new BsonArray(Activities)));
            }
            else
            {
                if (ActivitiesCategory != null && ActivitiesCategory.Count > 0)
                {
                    var getAct = ActivityMaster.Populate<ActivityMaster>(Query.In("ActivityCategory", new BsonArray(ActivitiesCategory)));
                    if (getAct.Any())
                    {
                        queries.Add(Query.In("Phases.ActivityType", new BsonArray(getAct.Select(d => d._id.ToString()).ToList())));
                    }
                }
            }

            if (OPs != null)
            {
                if (OPs.Count == 1)
                {
                    queries.Add(Query.EQ("Phases.Estimate.SaveToOP", OPs.FirstOrDefault()));
                }
                else
                {
                    queries.Add(Query.In("Phases.Estimate.SaveToOP", new BsonArray(OPs)));
                }
            }

            var periodQuery = ParsePeriodQuery();//ParsePeriodQueryForBizPlan();
            if (periodQuery != null)
                queries.Add(Query.And(periodQuery));

            if (isInPlan != null && isInPlan.Count() > 0)
            {
                queries.Add(Query.EQ("isInPlan", isInPlan.FirstOrDefault()));
            }
            if (queries.Count > 0)
                return Query.And(queries.ToArray());

            return null;
        }

        public IMongoQuery ParsePeriodQueryForBizPlan()
        {
            DateRelation = (DateRelation == null ? "AND" : DateRelation);
            DateStart = (DateStart == null ? "" : DateStart);
            DateFinish = (DateFinish == null ? "" : DateFinish);
            DateStart2 = (DateStart2 == null ? "" : DateStart2);
            DateFinish2 = (DateFinish2 == null ? "" : DateFinish2);

            var queries = new List<IMongoQuery>();
            string format = "yyyy-MM-dd";
            CultureInfo culture = CultureInfo.InvariantCulture;
            var period = GetPeriodBaseField();

            var periodStart = Tools.ToUTC(DateStart != "" ? DateTime.ParseExact(DateStart, format, culture) : new DateTime(1900, 1, 1));
            var periodFinish = Tools.ToUTC(DateFinish != "" ? DateTime.ParseExact(DateFinish, format, culture) : new DateTime(3000, 1, 1));
            var periodStart2 = Tools.ToUTC(DateStart2 != "" ? DateTime.ParseExact(DateStart2, format, culture) : new DateTime(1900, 1, 1));
            var periodFinish2 = Tools.ToUTC(DateFinish2 != "" ? DateTime.ParseExact(DateFinish2, format, culture) : new DateTime(3000, 1, 1));

            if (PeriodView.ToLower().Contains("fiscal"))
            {
                var queryDateStart = Query.And(
                    Query.GTE("LESchedule.Start", periodStart),
                    Query.LTE("LESchedule.Start", periodFinish)
                );

                var queryDateFinish = Query.And(
                    Query.GTE("LESchedule.Finish", periodStart),
                    Query.LTE("LESchedule.Finish", periodFinish)
                );

                return Query.And(queryDateStart, queryDateFinish);
            }

            if (!DateStart.Equals("") && !DateFinish.Equals(""))
                queries.Add(Query.And(
                    Query.GTE("LESchedule.Start", periodStart),
                    Query.LTE("LESchedule.Start", periodFinish)
                ));
            else if (!DateStart.Equals(""))
                queries.Add(
                    Query.GTE("LESchedule.Start", periodStart)
                );
            else if (!DateFinish.Equals(""))
                queries.Add(
                    Query.LTE("LESchedule.Start", periodFinish)
                );

            if (!DateStart2.Equals("") && !DateFinish2.Equals(""))
                queries.Add(Query.And(
                    Query.GTE("LESchedule.Finish", periodStart2),
                    Query.LTE("LESchedule.Finish", periodFinish2)
                ));
            else if (!DateStart2.Equals(""))
                queries.Add(
                    Query.GTE("LESchedule.Finish", periodStart2)
                );
            else if (!DateFinish2.Equals(""))
                queries.Add(
                    Query.LTE("LESchedule.Finish", periodFinish2)
                );
            //Period Start and Period Finish are None
            if ((DateStart2.Equals("") && DateFinish2.Equals("")) && (DateStart.Equals("") && DateFinish.Equals("")))
                queries.Add(Query.And(
                    Query.GTE("LESchedule.Start", periodStart),
                    Query.LTE("LESchedule.Start", periodFinish),
                    Query.GTE("LESchedule.Finish", periodStart2),
                    Query.LTE("LESchedule.Finish", periodFinish2)
                ));



            if (queries.Count > 0)
                return DateRelation.Equals("AND") ? Query.And(queries.ToArray()) : Query.Or(queries.ToArray());

            return null;
        }

        public bool DateBetween(DateRange target)
        {
            DateRelation = (DateRelation == null ? "AND" : DateRelation);
            DateStart = (DateStart == null ? "" : DateStart);
            DateFinish = (DateFinish == null ? "" : DateFinish);
            DateStart2 = (DateStart2 == null ? "" : DateStart2);
            DateFinish2 = (DateFinish2 == null ? "" : DateFinish2);

            string format = "yyyy-MM-dd";
            CultureInfo culture = CultureInfo.InvariantCulture;

            var periodStart = Tools.ToUTC(DateStart != "" ? DateTime.ParseExact(DateStart, format, culture) : new DateTime(1900, 1, 1));
            var periodFinish = Tools.ToUTC(DateFinish != "" ? DateTime.ParseExact(DateFinish, format, culture) : new DateTime(3000, 1, 1));
            var periodStart2 = Tools.ToUTC(DateStart2 != "" ? DateTime.ParseExact(DateStart2, format, culture) : new DateTime(1900, 1, 1));
            var periodFinish2 = Tools.ToUTC(DateFinish2 != "" ? DateTime.ParseExact(DateFinish2, format, culture) : new DateTime(3000, 1, 1));

            bool start = false;
            bool finish = false;

            if (!DateStart.Equals("") && !DateFinish.Equals(""))
                start = (target.Start >= periodStart) && (target.Start <= periodFinish);
            else if (!DateStart.Equals(""))
                start = (target.Start >= periodStart);
            else if (!DateFinish.Equals(""))
                start = (target.Start <= periodFinish);
            else
                start = true;

            if (!DateStart2.Equals("") && !DateFinish2.Equals(""))
                finish = (target.Finish >= periodStart2) && (target.Finish <= periodFinish2);
            else if (!DateStart2.Equals(""))
                finish = (target.Finish >= periodStart2);
            else if (!DateFinish2.Equals(""))
                finish = (target.Finish <= periodFinish2);
            else
                finish = true;

            return (DateRelation.Equals("AND") ? (start && finish) : (start || finish));
        }

        public bool FilterPeriod(DateRange period, WellActivity wa = null, WellActivityPhase ph = null, string CurrentOP = "", string BaseOP = "OP15")
        {


            if (PeriodView != null && PeriodView.ToLower().Contains("fiscal"))
            {
                var selectedOP = Convert.ToInt32(this.BaseOP != null ? this.BaseOP.Replace("OP", "") : "0");
                var lastCurOP = (ph != null) ? Convert.ToInt32(ph.BaseOP.OrderByDescending(x => x.ToString()).FirstOrDefault().Replace("OP", "")) : 0;
                var ActiveOP = Convert.ToInt32(BaseOP.Replace("OP", ""));

                if (PeriodBase.Equals("By Plan Schedule"))
                {

                    if (lastCurOP == 0)
                    {
                        DateStart = (DateStart == null ? "" : DateStart);
                        DateFinish = (DateFinish == null ? "" : DateFinish);

                        string format = "yyyy-MM-dd";
                        CultureInfo culture = CultureInfo.InvariantCulture;

                        var periodStart = Tools.ToUTC(DateStart != "" ? DateTime.ParseExact(DateStart, format, culture) : new DateTime(1900, 1, 1));
                        var periodFinish = Tools.ToUTC(DateFinish != "" ? DateTime.ParseExact(DateFinish, format, culture) : new DateTime(3000, 1, 1));

                        var isPhaseStartFiltered = (period.Start >= periodStart) && (period.Start <= periodFinish);
                        var isPhaseFinishFiltered = (period.Finish >= periodStart) && (period.Finish <= periodFinish);

                        return isPhaseStartFiltered && isPhaseFinishFiltered;
                    }
                    if (selectedOP == lastCurOP)
                    {
                        // ambil body

                        DateStart = (DateStart == null ? "" : DateStart);
                        DateFinish = (DateFinish == null ? "" : DateFinish);

                        string format = "yyyy-MM-dd";
                        CultureInfo culture = CultureInfo.InvariantCulture;

                        var periodStart = Tools.ToUTC(DateStart != "" ? DateTime.ParseExact(DateStart, format, culture) : new DateTime(1900, 1, 1));
                        var periodFinish = Tools.ToUTC(DateFinish != "" ? DateTime.ParseExact(DateFinish, format, culture) : new DateTime(3000, 1, 1));

                        var isPhaseStartFiltered = (period.Start >= periodStart) && (period.Start <= periodFinish);
                        var isPhaseFinishFiltered = (period.Finish >= periodStart) && (period.Finish <= periodFinish);

                        return isPhaseStartFiltered && isPhaseFinishFiltered;

                    }
                    else if (selectedOP < lastCurOP)
                    {
                        // hitory

                        var history = wa.OPHistories.FirstOrDefault(x => x.ActivityType.Equals(ph.ActivityType) && x.Type.Equals("OP" + selectedOP));
                        if (history != null)
                        {
                            DateStart = (DateStart == null ? "" : DateStart);
                            DateFinish = (DateFinish == null ? "" : DateFinish);

                            string format = "yyyy-MM-dd";
                            CultureInfo culture = CultureInfo.InvariantCulture;

                            var periodStart = Tools.ToUTC(DateStart != "" ? DateTime.ParseExact(DateStart, format, culture) : new DateTime(1900, 1, 1));
                            var periodFinish = Tools.ToUTC(DateFinish != "" ? DateTime.ParseExact(DateFinish, format, culture) : new DateTime(3000, 1, 1));

                            var isPhaseStartFiltered = (history.PlanSchedule.Start >= periodStart) && (history.PlanSchedule.Start <= periodFinish);
                            var isPhaseFinishFiltered = (history.PlanSchedule.Finish >= periodStart) && (history.PlanSchedule.Finish <= periodFinish);

                            return isPhaseStartFiltered && isPhaseFinishFiltered;
                        }
                        else
                            return false;

                    }
                    else
                        return false;

                }
                else
                {
                    DateStart = (DateStart == null ? "" : DateStart);
                    DateFinish = (DateFinish == null ? "" : DateFinish);

                    string format = "yyyy-MM-dd";
                    CultureInfo culture = CultureInfo.InvariantCulture;

                    var periodStart = Tools.ToUTC(DateStart != "" ? DateTime.ParseExact(DateStart, format, culture) : new DateTime(1900, 1, 1));
                    var periodFinish = Tools.ToUTC(DateFinish != "" ? DateTime.ParseExact(DateFinish, format, culture) : new DateTime(3000, 1, 1));

                    var isPhaseStartFiltered = (period.Start >= periodStart) && (period.Start <= periodFinish);
                    var isPhaseFinishFiltered = (period.Finish >= periodStart) && (period.Finish <= periodFinish);

                    return isPhaseStartFiltered && isPhaseFinishFiltered;
                }

            }

            return DateBetween(period);
        }

        public DateRange GetPeriod(WellActivityPhase phase)
        {
            var phaseStart = phase.ToBsonDocument().Get(GetPeriodBaseField()).AsBsonDocument.GetDateTime("Start");
            var phaseFinish = phase.ToBsonDocument().Get(GetPeriodBaseField()).AsBsonDocument.GetDateTime("Finish");

            return new DateRange()
            {
                Start = phaseStart,
                Finish = phaseFinish
            };
        }

        private DateRange GetPeriodForBizPlan(BizPlanActivityPhase phase)
        {
            var phaseStart = phase.ToBsonDocument().Get(GetPeriodBaseField()).AsBsonDocument.GetDateTime("Start");
            var phaseFinish = phase.ToBsonDocument().Get(GetPeriodBaseField()).AsBsonDocument.GetDateTime("Finish");

            return new DateRange()
            {
                Start = phaseStart,
                Finish = phaseFinish
            };
        }

        private DateRange GetPeriodForBizPlanFullQuery(FilterHelper f)
        {
            var pStart = f.ToBsonDocument().Get(GetPeriodBaseField()).AsBsonDocument.GetDateTime("Start");
            var pFinish = f.ToBsonDocument().Get(GetPeriodBaseField()).AsBsonDocument.GetDateTime("Finish");
            return new DateRange()
            {
                Start = pStart,
                Finish = pFinish
            };
        }

        public double GetRatio(DateRange period)
        {
            if (PeriodView != null && PeriodView.ToLower().Contains("fiscal"))
            {
                DateStart = (DateStart == null ? "" : DateStart);
                DateFinish = (DateFinish == null ? "" : DateFinish);

                string format = "yyyy-MM-dd";
                CultureInfo culture = CultureInfo.InvariantCulture;

                var filter = new DateRange()
                {
                    Start = Tools.ToUTC(DateStart != "" ? DateTime.ParseExact(DateStart, format, culture) : new DateTime(1900, 1, 1)),
                    Finish = Tools.ToUTC(DateFinish != "" ? DateTime.ParseExact(DateFinish, format, culture) : new DateTime(3000, 1, 1))
                };

                var fiscalYears = GetFiscalYears();
                var isInvalidFilter = (filter.Start == Tools.DefaultDate);

                if (isInvalidFilter)
                    filter = period;

                var ratios = DateRangeToMonth.ProportionNumDaysPerYear(period, filter).Where(f =>
                {
                    if (fiscalYears.Count > 0)
                        return fiscalYears.Contains(f.Key);

                    return true;
                });

                return ratios.Select(f => f.Value).DefaultIfEmpty(0).Sum(f => f);
            }

            return 1.0;
        }

        private List<int> GetFiscalYears()
        {
            if (FiscalYearStart == 0 || FiscalYearFinish == 0)
                return new List<int>();

            var res = new List<int>();
            for (var i = FiscalYearStart; i <= FiscalYearFinish; i++)
                res.Add(i);

            return res.ToList();
        }

        private bool isMatchOP(List<string> BasePhaseOP)
        {
            var isMatchBaseOP = true;
            var BaseOP = BasePhaseOP.ToArray();
            if (opRelation.ToLower() == "and")
            {
                var match = true;
                foreach (var op in OPs)
                {
                    match = Array.Exists(BaseOP, element => element.Equals(op));
                    if (!match)
                    {
                        isMatchBaseOP = false;
                        break;
                    }
                }
            }
            else
            {
                //contains
                var match = false;
                foreach (var op in OPs)
                {

                    match = Array.Exists(BaseOP, element => element.Equals(op));
                    if (match) break;
                }
            }
            return isMatchBaseOP;
        }

        public ActivitiesAndPIPsMLE GetActivitiesAndPIPsForMLE(bool isIncludePIPs = true)
        {
            string DefaultOP = "OP15";
            if (Config.GetConfigValue("BaseOPConfig") != null)
            {
                DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
            }

            PareseFilter();
            var query = ParseQuery();
            var pips = new List<PIPElement>();

            var totalOP = 0.0;
            var totalLE = 0.0;
            var totalTQ = 0.0;

            var was = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(query);
            var MLEs = new List<WellActivityUpdateMonthly>();

            foreach (var d in was)
            {
                //filter phase
                var isNotRisk = true;
                if (GetWhatData != "OP")
                    isNotRisk = !d.Phase.ActivityType.ToLower().Contains("risk");

                var isPeriodFiltered = FilterPeriod(GetPeriod(d.Phase));
                var isActivityFiltered = true;
                var isFilcalFiltered = true;
                var isMatchedOPs = true;
                if (OPs != null && OPs.Count() > 0)
                {
                    isMatchedOPs = isMatchOP(d.Phase.BaseOP);
                }

                //for waterfall
                var singleBaseOPFilter = true;
                if (BaseOP != null)
                {
                    if (!d.Phase.BaseOP.Contains(BaseOP)) singleBaseOPFilter = false;
                }

                if (Activities != null && Activities.Count > 0)
                    isActivityFiltered = Activities.Contains(d.Phase.ActivityType);

                if (FiscalYearFinish != 0)
                {
                    var phasePeriod = GetPeriod(d.Phase);
                    var isPhasePeriodInFiscalStart = phasePeriod.Start.Year >= FiscalYearStart && phasePeriod.Start.Year <= FiscalYearFinish;
                    var isPhasePeriodInFiscalFinish = phasePeriod.Finish.Year >= FiscalYearStart && phasePeriod.Finish.Year <= FiscalYearFinish;
                    if (isPhasePeriodInFiscalStart || isPhasePeriodInFiscalFinish)
                    {
                        isFilcalFiltered = true;
                    }
                    else
                    {
                        isFilcalFiltered = false;
                    }
                }

                var thisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var isFutureLS = false;
                if (d.Phase.PhSchedule.Start >= thisMonth) isFutureLS = true;

                if (isFutureLS && isMatchedOPs && isNotRisk && singleBaseOPFilter && isPeriodFiltered && isActivityFiltered && isFilcalFiltered)
                {
                    //calc ratio
                    var e = d.Phase;
                    var period = GetPeriod(d.Phase);
                    var ratio = GetRatio(period);
                    d.Phase.Ratio = ratio;

                    d.Phase.CalcOP.Cost *= ratio;
                    d.Phase.CalcOP.Days *= ratio;
                    d.Phase.Plan.Cost *= ratio;
                    d.Phase.Plan.Days *= ratio;
                    d.Phase.OP.Cost *= ratio;
                    d.Phase.OP.Days *= ratio;
                    d.Phase.LE.Cost *= ratio;
                    d.Phase.LE.Days *= ratio;
                    d.Phase.AFE.Cost *= ratio;
                    d.Phase.AFE.Days *= ratio;
                    d.Phase.TQ.Cost *= ratio;
                    d.Phase.TQ.Days *= ratio;

                    var OP = 0.0;
                    var LE = 0.0;
                    var TQ = 0.0;
                    if (BaseOP != null)
                    {
                        if (BaseOP.ToLower() == DefaultOP.ToLower())
                        {
                            //OP taken from phase
                            if (d.Phase.BaseOP.Contains(BaseOP))
                            {
                                if (DayOrCost.ToLower() == "cost")
                                {
                                    OP = d.Phase.Plan.Cost;
                                    LE = d.Phase.LE.Cost;
                                    TQ = d.Phase.TQ.Cost;
                                }
                                else
                                {
                                    OP = d.Phase.Plan.Days;
                                    LE = d.Phase.LE.Days;
                                    TQ = d.Phase.TQ.Days;
                                }
                            }
                        }
                    }
                    totalOP += OP;
                    totalLE += LE;
                    totalTQ += TQ;

                    if (FiscalYearStart == 0)
                        d.Phase.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(period, period);
                    else
                        d.Phase.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(period, FiscalYearStart, FiscalYearFinish);

                    d.Phase.PIPs = new List<PIPElement>();


                    //filter element
                    if (isIncludePIPs)
                    {

                        if (d.Elements.Any())
                        {
                            var pipInside = d.Elements.Where(g => g.AssignTOOps.Contains(BaseOP))
                                .Select(g =>
                                    {
                                        var ratioElement = GetRatio(g.Period);

                                        g.CostCurrentWeekImprovement *= ratioElement;
                                        g.CostCurrentWeekRisk *= ratioElement;
                                        g.CostPlanImprovement *= ratioElement;
                                        g.CostPlanRisk *= ratioElement;

                                        g.DaysCurrentWeekImprovement *= ratioElement;
                                        g.DaysCurrentWeekRisk *= ratioElement;
                                        g.DaysPlanImprovement *= ratioElement;
                                        g.DaysPlanRisk *= ratioElement;
                                        g.RatioElement = ratioElement;

                                        if (FiscalYearStart == 0)
                                        {
                                            g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, g.Period);
                                            g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, g.Period);
                                        }
                                        else
                                        {
                                            g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                            g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                        }
                                        return g;

                                    }).ToList();
                            if (pipInside.Any()) pips.AddRange(pipInside);
                            d.Elements = pipInside.ToList();
                        }

                        if (d.CRElements.Any())
                        {
                            d.CRElements = d.CRElements.Select(g =>
                                {
                                    var ratioElement = GetRatio(g.Period);

                                    g.CostCurrentWeekImprovement *= ratioElement;
                                    g.CostCurrentWeekRisk *= ratioElement;
                                    g.CostPlanImprovement *= ratioElement;
                                    g.CostPlanRisk *= ratioElement;

                                    g.DaysCurrentWeekImprovement *= ratioElement;
                                    g.DaysCurrentWeekRisk *= ratioElement;
                                    g.DaysPlanImprovement *= ratioElement;
                                    g.DaysPlanRisk *= ratioElement;
                                    g.RatioElement = ratioElement;

                                    if (FiscalYearStart == 0)
                                    {
                                        g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, g.Period);
                                        g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, g.Period);
                                    }
                                    else
                                    {
                                        g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                        g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                    }

                                    return g;
                                }).ToList();
                        }
                    }
                    MLEs.Add(d);
                }
            }

            if ("Cost".Equals(DayOrCost))
            {
                totalOP /= 1000000;
                totalLE /= 1000000;
                totalTQ /= 1000000;
            }

            //var Phases = wellActivities.SelectMany(x => x.Phases).ToList();

            return new ActivitiesAndPIPsMLE()
            {
                TotalOP = totalOP,
                TotalLE = totalLE,
                TotalTQ = totalTQ,
                Activities = MLEs,
                PIPs = pips
            };
        }

        public ActivitiesAndPIPs GetActivitiesAndPIPs(bool isIncludePIPs = true, List<BizPlanActivity> BizPlanDatas = null, bool isIgnoreUploadLS = false) // OP History belum ada BIC nya.
        {
            string DefaultOP = "OP15";
            if (Config.GetConfigValue("BaseOPConfig") != null)
            {
                DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
            }

            PareseFilter();
            var query = ParseQuery();
            var pips = new List<WellPIP>();

            var totalOP = 0.0;
            var totalLE = 0.0;
            var totalTQ = 0.0;
            var totalBIC = 0.0;
            var totalTarget = 0.0;

            if (BizPlanDatas == null)
            {
                BizPlanDatas = new List<BizPlanActivity>();
            }

            var was = WellActivity.Populate<WellActivity>(query);
            //var countphases = 0;
            //var countUnphases = 0;

            var lastDateExecutedLS = WellActivity.GetLastExecuteDateLS();

            var wellActivities = was
                .Select(d =>
                {
                    var ps = d.Phases = d.Phases.Where(e =>
                    {
                        var isNotRisk = true;
                        if (GetWhatData != "OP")
                            isNotRisk = !e.ActivityType.ToLower().Contains("risk");

                        bool isPeriodFiltered = false;


                        string currentOP = e.BaseOP.OrderByDescending(x => x).FirstOrDefault();
                        string selectedOP = this.BaseOP;


                        if (e.BaseOP != null && e.BaseOP.Count > 0)
                        {
                            if (!e.BaseOP.OrderByDescending(x => x).FirstOrDefault().Equals(DefaultOP))
                            {
                                isPeriodFiltered = FilterPeriod(GetPeriod(e), d, e, currentOP, DefaultOP);

                            }
                            else if (DefaultOP != selectedOP)
                            {
                                isPeriodFiltered = FilterPeriod(GetPeriod(e), d, e, currentOP, DefaultOP);

                            }
                            else
                                isPeriodFiltered = FilterPeriod(GetPeriod(e));
                        }
                        else
                            isPeriodFiltered = FilterPeriod(GetPeriod(e));
                        var isActivityFiltered = true;
                        var isActivityCategoryFiltered = true;
                        var isFilcalFiltered = true;
                        var isLSValid = true;
                        var activityCategories = ActivityMaster.Populate<ActivityMaster>();
                        var isMatchedOPs = true;
                        if (OPs != null && OPs.Count() > 0)
                        {
                            isMatchedOPs = isMatchOP(e.BaseOP);

                        }

                        //for waterfall
                        var singleBaseOPFilter = true;
                        if (BaseOP != null)
                        {
                            if (!e.BaseOP.Contains(BaseOP)) singleBaseOPFilter = false;
                        }



                        if (Activities != null && Activities.Count > 0)
                            isActivityFiltered = Activities.Contains(e.ActivityType.Trim());
                        else
                        {
                            if (ActivitiesCategory != null && ActivitiesCategory.Count > 0)
                            {
                                var getActCat = activityCategories.Where(x => x._id.ToString().Trim().Equals(e.ActivityType.Trim())).FirstOrDefault();
                                if (getActCat != null) isActivityCategoryFiltered = ActivitiesCategory.Contains(getActCat.ActivityCategory);
                                else isActivityCategoryFiltered = false;
                            }
                        }

                        if (FiscalYearFinish != 0)
                        {
                            var phasePeriod = GetPeriod(e);
                            var isPhasePeriodInFiscalStart = phasePeriod.Start.Year >= FiscalYearStart && phasePeriod.Start.Year <= FiscalYearFinish;
                            var isPhasePeriodInFiscalFinish = phasePeriod.Finish.Year >= FiscalYearStart && phasePeriod.Finish.Year <= FiscalYearFinish;
                            if (isPhasePeriodInFiscalStart || isPhasePeriodInFiscalFinish)
                            {
                                isFilcalFiltered = true;
                            }
                            else
                            {
                                isFilcalFiltered = false;
                            }
                        }
                        #region risk
                        //if (isNotRisk && isPeriodFiltered && isActivityFiltered && isFilcalFiltered)
                        //{

                        //}
                        //else
                        //{
                        //    var WellName = d.WellName;
                        //    var Act = e.ActivityType;
                        //}
                        #endregion


                        //check only LS valid if DataFor == "Waterfall"
                        if (ValidLSOnly)
                        {
                            var lsStart = e.PhSchedule.Start;
                            if (lsStart == Tools.DefaultDate) isLSValid = false;
                        }

                        var checkIsInPlan = false;

                        if (isInPlan != null && isInPlan.Count() > 0)
                        {
                            var isinp = isInPlan.FirstOrDefault();
                            var phinp = e.IsInPlan;

                            if (isinp == phinp)
                                checkIsInPlan = true;

                        }
                        else
                        {
                            checkIsInPlan = true;
                        }


                        //check bizplan status here
                        var isMatchedStatus = false;
                        if (Status != null && Status.Count > 0)
                        {
                            try
                            {
                                var bizplandata = BizPlanDatas.Where(x => x.WellName == d.WellName && x.UARigSequenceId == d.UARigSequenceId && (x.Phases.Where(z => z.ActivityType == e.ActivityType).Count() > 0)).FirstOrDefault();
                                if (bizplandata != null)
                                {
                                    var matchedPhase = bizplandata.Phases.Where(x => x.ActivityType == e.ActivityType).FirstOrDefault();
                                    if (matchedPhase != null)
                                    {
                                        e.BizPlanStatus = matchedPhase.Estimate.Status;
                                        isMatchedStatus = Status.Contains(e.BizPlanStatus);
                                    }

                                }
                            }
                            catch
                            {

                            }
                            isMatchedStatus = Status.Contains(e.BizPlanStatus);
                        }
                        else
                        {
                            isMatchedStatus = true;
                        }

                        var islastesLs = false;
                        bool dataAda = false;
                        if (isIgnoreUploadLS)
                        {
                            islastesLs = true;
                            dataAda = true;
                        }
                        else
                            dataAda = CheckPhaseInLatestLS(d.WellName, e.ActivityType, d.RigName, lastDateExecutedLS);

                        if (inlastuploadls == dataAda)
                        {
                            islastesLs = true;
                        }

                        if (inlastuploadls == false)
                        {
                            return checkIsInPlan && isMatchedOPs && isNotRisk && singleBaseOPFilter && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isActivityCategoryFiltered && isLSValid && isMatchedStatus;
                        }
                        else
                        {
                            return islastesLs && checkIsInPlan && isMatchedOPs && isNotRisk && singleBaseOPFilter && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isActivityCategoryFiltered && isLSValid && isMatchedStatus;
                        }


                    }).ToList();

                    ps.Select(e =>
                    {
                        var period = GetPeriod(e);
                        var ratio = GetRatio(period);
                        e.Ratio = ratio;
                        var ratioLS = GetRatio(e.PhSchedule);
                        var ratioLE = GetRatio(e.LESchedule);
                        e.RatioLS = ratioLS;
                        e.RatioLE = ratioLE;

                        var WorkingInterest = ShellShare ? d.WorkingInterest > 1 ? Tools.Div(d.WorkingInterest, 100) : d.WorkingInterest : 1;
                        e.CalcOP.Cost *= ratio * WorkingInterest;
                        e.CalcOP.Days *= ratio;
                        e.Plan.Cost *= ratio * WorkingInterest;
                        e.Plan.Days *= ratio;
                        e.OP.Cost *= ratioLS * WorkingInterest;
                        e.OP.Days *= ratioLS;
                        if (e.ActivityType.ToLower().Contains("risk"))
                        {
                            e.LE.Cost = 0.0;
                            e.LE.Days = 0.0;
                        }
                        else
                        {
                            e.LE.Cost *= ratioLE * WorkingInterest;
                            e.LE.Days *= ratioLE;
                        }
                        //e.AFE.Cost *= ratio;
                        //e.AFE.Days *= ratio;
                        //e.TQ.Cost *= ratio;
                        //e.TQ.Days *= ratio;
                        //e.OPHistories = e.OPHistories;
                        //var OP = e.CalcOP.ToBsonDocument().GetDouble(DayOrCost);
                        var OP = 0.0;
                        var LE = 0.0;
                        var TQ = 0.0;
                        var BIC = 0.0;
                        var Target = 0.0;
                        if (BaseOP != null)
                        {
                            var CurrentOP = e.BaseOP.Any() ? e.BaseOP.OrderByDescending(z => z).FirstOrDefault() : DefaultOP;
                            if (BaseOP.ToLower() == CurrentOP.ToLower())
                            {
                                //OP taken from phase
                                if (e.BaseOP.Contains(BaseOP))
                                {
                                    if (DayOrCost.ToLower() == "cost")
                                    {
                                        OP = e.Plan.Cost;
                                        LE = e.LE.Cost;
                                        TQ = e.TQ.Cost;
                                        BIC = e.BIC.Cost;
                                        Target = e.AggredTarget.Cost;
                                    }
                                    else
                                    {
                                        OP = e.Plan.Days;
                                        LE = e.LE.Days;
                                        TQ = e.TQ.Days;
                                        BIC = e.BIC.Days;
                                        Target = e.AggredTarget.Days;
                                    }
                                }
                            }
                            else
                            {
                                //OP taken from histories
                                try
                                {

                                    if (d.OPHistories != null && d.OPHistories.Count() > 0)
                                    {
                                        var OPHists = d.OPHistories.Where(x => !string.IsNullOrEmpty(x.Type) && x.Type.Equals(BaseOP) && x.ActivityType.Equals(e.ActivityType) && x.PhaseNo == e.PhaseNo && x.PlanSchedule.Start != Tools.DefaultDate);
                                        if (OPHists != null && OPHists.Count() > 0)
                                        {
                                            var OPHist = OPHists.FirstOrDefault();

                                            var ratioOPHist = GetRatio(OPHist.PlanSchedule);
                                            var ratioLSHist = GetRatio(OPHist.PhSchedule);
                                            var ratioLEHist = GetRatio(OPHist.LESchedule);

                                            OPHist.CalcOP.Cost *= ratioOPHist * WorkingInterest;
                                            OPHist.CalcOP.Days *= ratioOPHist;
                                            OPHist.Plan.Cost *= ratioOPHist * WorkingInterest;

                                            if (OPHist.Plan.Cost < 5000) OPHist.Plan.Cost = OPHist.Plan.Cost * 1000000;

                                            OPHist.Plan.Days *= ratioOPHist;
                                            OPHist.OP.Cost *= ratioLSHist * WorkingInterest;
                                            OPHist.OP.Days *= ratioLSHist;
                                            OPHist.LE.Cost *= ratioLEHist * WorkingInterest;
                                            OPHist.LE.Days *= ratioLEHist;

                                            if (DayOrCost.ToLower() == "cost")
                                            {
                                                OP = OPHist.Plan.Cost;
                                                TQ = OPHist.TQ.Cost;
                                                BIC = OPHist.BIC.Cost;
                                                Target = OPHist.Target.Cost;
                                                var getLE = e.LE.Cost;//OPHist.LE.Cost;//0.0;
                                                //var wa = new WellActivity();
                                                //if (wa.isHaveWeeklyReport2(d.WellName, d.UARigSequenceId, e.ActivityType)) getLE = OPHist.LE.Cost;
                                                LE = getLE;
                                            }
                                            else
                                            {
                                                OP = OPHist.Plan.Days;
                                                TQ = OPHist.TQ.Days;
                                                BIC = OPHist.BIC.Days;
                                                Target = OPHist.Target.Days;
                                                var getLE = e.LE.Days;//OPHist.LE.Days;//0.0;// Jadi, LE harus tetap ambil dari 16
                                                //var wa = new WellActivity();
                                                //if (wa.isHaveWeeklyReport2(d.WellName, d.UARigSequenceId, e.ActivityType)) getLE = OPHist.LE.Days;
                                                LE = getLE;
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {

                                }


                            }
                        }


                        //LE = e.LE.ToBsonDocument().GetDouble(DayOrCost);
                        //if (LE == 0) LE = e.OP.ToBsonDocument().GetDouble(DayOrCost);
                        //if (LE == 0) LE = e.AFE.ToBsonDocument().GetDouble(DayOrCost);
                        //TQ = e.TQ.ToBsonDocument().GetDouble(DayOrCost);



                        totalOP += OP;
                        totalLE += LE;
                        totalTQ += TQ;
                        totalBIC += BIC;
                        totalTarget += Target;

                        if (FiscalYearStart == 0)
                            e.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(period, period);
                        else
                            e.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(period, FiscalYearStart, FiscalYearFinish);

                        e.PIPs = new List<PIPElement>();

                        var getWAUM = WellActivityUpdateMonthly.GetById(d.WellName, d.UARigSequenceId, e.PhaseNo, null, true);
                        if (getWAUM != null)
                        {
                            var totalWAUMElementsCompetitiveScope = 1;
                            var totalWAUMElementsEfficientExecution = 1;
                            var totalWAUMElementsSupplyChainTransformation = 1;
                            var totalWAUMElementsTechnologyAndInnovation = 1;

                            e.BankedSavingsCompetitiveScope = new WellDrillData() { Days = Tools.Div(getWAUM.BankedSavingsCompetitiveScope.Days, totalWAUMElementsCompetitiveScope), Cost = Tools.Div(getWAUM.BankedSavingsCompetitiveScope.Cost, totalWAUMElementsCompetitiveScope) };
                            e.BankedSavingsEfficientExecution = new WellDrillData() { Days = Tools.Div(getWAUM.BankedSavingsEfficientExecution.Days, totalWAUMElementsEfficientExecution), Cost = Tools.Div(getWAUM.BankedSavingsEfficientExecution.Cost, totalWAUMElementsEfficientExecution) };
                            e.BankedSavingsSupplyChainTransformation = new WellDrillData() { Days = Tools.Div(getWAUM.BankedSavingsSupplyChainTransformation.Days, totalWAUMElementsSupplyChainTransformation), Cost = Tools.Div(getWAUM.BankedSavingsSupplyChainTransformation.Cost, totalWAUMElementsSupplyChainTransformation) };
                            e.BankedSavingsTechnologyAndInnovation = new WellDrillData() { Days = Tools.Div(getWAUM.BankedSavingsTechnologyAndInnovation.Days, totalWAUMElementsTechnologyAndInnovation), Cost = Tools.Div(getWAUM.BankedSavingsTechnologyAndInnovation.Cost, totalWAUMElementsTechnologyAndInnovation) };
                        }
                        else
                        {
                            e.BankedSavingsCompetitiveScope = new WellDrillData();
                            e.BankedSavingsEfficientExecution = new WellDrillData();
                            e.BankedSavingsSupplyChainTransformation = new WellDrillData();

                            e.BankedSavingsTechnologyAndInnovation = new WellDrillData();
                        }

                        if (isIncludePIPs)
                        {
                            var queryForPIP = Query.And(
                                Query.EQ("WellName", d.WellName),
                                Query.EQ("ActivityType", e.ActivityType),
                                Query.EQ("SequenceId", d.UARigSequenceId)
                            );

                            var pipsInsideFu = WellPIP.Populate<WellPIP>(queryForPIP);
                            var pipsInside = pipsInsideFu
                                .Select(f =>
                                {
                                    if (f.Elements.Any())
                                    {
                                        // calculate shellshare to PIP
                                        //double wi = 1;
                                        //if (ShellShare)
                                        //    wi = d.WorkingInterest;
                                        f.Elements = f.Elements
                                        .Where(g => g.AssignTOOps.Contains(BaseOP))
                                        .Select(g =>
                                        {
                                            var ratioElement = GetRatio(g.Period);

                                            g.CostCurrentWeekImprovement *= (ratioElement);
                                            g.CostCurrentWeekRisk *= (ratioElement);
                                            g.CostPlanImprovement *= (ratioElement);
                                            g.CostPlanRisk *= (ratioElement);

                                            g.DaysCurrentWeekImprovement *= ratioElement;
                                            g.DaysCurrentWeekRisk *= ratioElement;
                                            g.DaysPlanImprovement *= ratioElement;
                                            g.DaysPlanRisk *= ratioElement;
                                            g.RatioElement = ratioElement;

                                            if (FiscalYearStart == 0)
                                            {
                                                g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, g.Period);
                                                g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, g.Period);
                                            }
                                            else
                                            {
                                                g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                                g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                            }
                                            return g;

                                        }).ToList();
                                    }

                                    if (f.CRElements != null && f.CRElements.Count > 0)
                                    {

                                        f.CRElements = f.CRElements.Select(g =>
                                        {
                                            var ratioElement = GetRatio(g.Period);

                                            g.CostCurrentWeekImprovement *= (ratioElement);
                                            g.CostCurrentWeekRisk *= (ratioElement);
                                            g.CostPlanImprovement *= (ratioElement);
                                            g.CostPlanRisk *= (ratioElement);

                                            g.DaysCurrentWeekImprovement *= ratioElement;
                                            g.DaysCurrentWeekRisk *= ratioElement;
                                            g.DaysPlanImprovement *= ratioElement;
                                            g.DaysPlanRisk *= ratioElement;
                                            g.RatioElement = ratioElement;

                                            if (FiscalYearStart == 0)
                                            {
                                                g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, g.Period);
                                                g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, g.Period);
                                            }
                                            else
                                            {
                                                g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                                g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                            }

                                            return g;
                                        }).ToList();

                                    }
                                    return f;
                                });

                            if (pipsInside.Any())
                            {
                                e.PIPs = pipsInside.SelectMany(f => f.Elements).ToList();
                                pips.AddRange(pipsInside);
                            }
                        }
                        return e;
                    }).ToList();

                    return d;
                }).ToList();

            var TotPhase = 0;
            foreach (var well in wellActivities)
            {
                if (well.Phases != null)
                {
                    TotPhase += well.Phases.Count;
                }
            }

            if ("Cost".Equals(DayOrCost))
            {
                totalOP /= 1000000;
                totalLE /= 1000000;
                totalTQ /= 1000000;
                totalBIC /= 1000000;
                totalTarget /= 1000000;
            }

            //var Phases = wellActivities.SelectMany(x => x.Phases).ToList();


            double wi = 1;
            if (ShellShare)
            {
                foreach (var p in pips)
                {
                    var wasx = wellActivities.Where(x => x.WellName.Equals(p.WellName) && x.UARigSequenceId.Equals(p.SequenceId) && x.RigName.Equals(p.RigName));
                    if (wasx.Any())
                    {

                        wi = wasx.FirstOrDefault().WorkingInterest;
                        if (wi > 1)
                            wi = Tools.Div(wi, 100);
                        foreach (var ele in p.Elements)
                        {
                            ele.CostCurrentWeekImprovement = ele.CostCurrentWeekImprovement * wi;
                            ele.CostCurrentWeekRisk = ele.CostCurrentWeekRisk * wi;
                            ele.CostPlanImprovement = ele.CostPlanImprovement * wi;
                            ele.CostPlanRisk = ele.CostPlanRisk * wi;
                        }
                        foreach (var ele in p.CRElements)
                        {
                            ele.CostCurrentWeekImprovement = ele.CostCurrentWeekImprovement * wi;
                            ele.CostCurrentWeekRisk = ele.CostCurrentWeekRisk * wi;
                            ele.CostPlanImprovement = ele.CostPlanImprovement * wi;
                            ele.CostPlanRisk = ele.CostPlanRisk * wi;
                        }


                        //foreach (var ele in p.Elements)
                        //{
                        //    ele.CostCurrentWeekImprovement = Tools.Div(ele.CostCurrentWeekImprovement, 1000000) * wi;
                        //    ele.CostCurrentWeekRisk = Tools.Div(ele.CostCurrentWeekRisk, 1000000) * wi;
                        //    ele.CostPlanImprovement = Tools.Div(ele.CostPlanImprovement, 1000000) * wi;
                        //    ele.CostPlanRisk = Tools.Div(ele.CostPlanRisk, 1000000) * wi;
                        //}
                        //foreach (var ele in p.CRElements)
                        //{
                        //    ele.CostCurrentWeekImprovement = Tools.Div(ele.CostCurrentWeekImprovement, 1000000) * wi;
                        //    ele.CostCurrentWeekRisk = Tools.Div(ele.CostCurrentWeekRisk, 1000000) * wi;
                        //    ele.CostPlanImprovement = Tools.Div(ele.CostPlanImprovement, 1000000) * wi;
                        //    ele.CostPlanRisk = Tools.Div(ele.CostPlanRisk, 1000000) * wi;
                        //}

                    }
                }
            }

            var tello = wellActivities;

            return new ActivitiesAndPIPs()
            {
                TotalOP = totalOP,
                TotalLE = totalLE,
                TotalTQ = totalTQ,
                TotalBIC = totalBIC,
                TotalTarget = totalTarget,
                Activities = wellActivities,
                PIPs = pips
            };


        }

        public ActivitiesAndPIPs GetActivitiesAndPIPsSpotFire(bool isIncludePIPs = true) // OP History belum ada BIC nya.
        {
            string DefaultOP = "OP15";
            if (Config.GetConfigValue("BaseOPConfig") != null)
            {
                DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
            }

            PareseFilter();
            var query = ParseQuery();
            var pips = new List<WellPIP>();

            var totalOP = 0.0;
            var totalLE = 0.0;
            var totalTQ = 0.0;
            var totalBIC = 0.0;
            var totalTarget = 0.0;

            var was = WellActivity.Populate<WellActivity>(query);
            //var countphases = 0;
            //var countUnphases = 0;
            var wellActivities = was
                .Select(d =>
                {
                    var ps = d.Phases = d.Phases.Where(e =>
                    {
                        var isNotRisk = true;
                        if (GetWhatData != "OP")
                            isNotRisk = !e.ActivityType.ToLower().Contains("risk");
                        var isPeriodFiltered = FilterPeriod(GetPeriod(e));
                        var isActivityFiltered = true;
                        var isActivityCategoryFiltered = true;
                        var isFilcalFiltered = true;
                        var isLSValid = true;
                        var activityCategories = ActivityMaster.Populate<ActivityMaster>();
                        var isMatchedOPs = true;
                        if (OPs != null && OPs.Count() > 0)
                        {
                            isMatchedOPs = isMatchOP(e.BaseOP);
                        }

                        //for waterfall
                        var singleBaseOPFilter = true;
                        if (BaseOP != null)
                        {
                            if (!e.BaseOP.Contains(BaseOP)) singleBaseOPFilter = false;
                        }




                        if (Activities != null && Activities.Count > 0)
                            isActivityFiltered = Activities.Contains(e.ActivityType);
                        else
                        {
                            if (ActivitiesCategory != null && ActivitiesCategory.Count > 0)
                            {
                                var getActCat = activityCategories.Where(x => x._id.Equals(e.ActivityType)).FirstOrDefault();
                                if (getActCat != null) isActivityCategoryFiltered = ActivitiesCategory.Contains(getActCat.ActivityCategory);
                                else isActivityCategoryFiltered = false;
                            }
                        }

                        if (FiscalYearFinish != 0)
                        {
                            var phasePeriod = GetPeriod(e);
                            var isPhasePeriodInFiscalStart = phasePeriod.Start.Year >= FiscalYearStart && phasePeriod.Start.Year <= FiscalYearFinish;
                            var isPhasePeriodInFiscalFinish = phasePeriod.Finish.Year >= FiscalYearStart && phasePeriod.Finish.Year <= FiscalYearFinish;
                            if (isPhasePeriodInFiscalStart || isPhasePeriodInFiscalFinish)
                            {
                                isFilcalFiltered = true;
                            }
                            else
                            {
                                isFilcalFiltered = false;
                            }
                        }
                        #region risk
                        //if (isNotRisk && isPeriodFiltered && isActivityFiltered && isFilcalFiltered)
                        //{

                        //}
                        //else
                        //{
                        //    var WellName = d.WellName;
                        //    var Act = e.ActivityType;
                        //}
                        #endregion


                        //check only LS valid if DataFor == "Waterfall"
                        if (ValidLSOnly)
                        {
                            var lsStart = e.PhSchedule.Start;
                            if (lsStart == Tools.DefaultDate) isLSValid = false;
                        }

                        var checkIsInPlan = false;

                        if (isInPlan != null && isInPlan.Count() > 0)
                        {
                            var isinp = isInPlan.FirstOrDefault();
                            var phinp = e.IsInPlan;

                            if (isinp == phinp)
                                checkIsInPlan = true;

                        }

                        if (isInPlan != null && isInPlan.Count() > 0)
                        {
                            return checkIsInPlan && isMatchedOPs && isNotRisk && singleBaseOPFilter && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isActivityCategoryFiltered && isLSValid;//

                        }
                        else
                        {
                            return isMatchedOPs && isNotRisk && singleBaseOPFilter && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isActivityCategoryFiltered && isLSValid;//

                        }

                    }).ToList();

                    ps.Select(e =>
                    {
                        var period = GetPeriod(e);
                        var ratio = GetRatio(period);
                        e.Ratio = ratio;
                        var ratioLS = GetRatio(e.PhSchedule);
                        var ratioLE = GetRatio(e.LESchedule);
                        e.RatioLS = ratioLS;
                        e.RatioLE = ratioLE;

                        var WorkingInterest = ShellShare ? d.WorkingInterest > 1 ? Tools.Div(d.WorkingInterest, 100) : d.WorkingInterest : 1;
                        e.CalcOP.Cost *= ratio * WorkingInterest;
                        e.CalcOP.Days *= ratio;
                        e.Plan.Cost *= ratio * WorkingInterest;
                        e.Plan.Days *= ratio;
                        e.OP.Cost *= ratioLS * WorkingInterest;
                        e.OP.Days *= ratioLS;
                        if (e.ActivityType.ToLower().Contains("risk"))
                        {
                            e.LE.Cost = 0.0;
                            e.LE.Days = 0.0;
                        }
                        else
                        {
                            e.LE.Cost *= ratioLE * WorkingInterest;
                            e.LE.Days *= ratioLE;
                        }
                        //e.AFE.Cost *= ratio;
                        //e.AFE.Days *= ratio;
                        //e.TQ.Cost *= ratio;
                        //e.TQ.Days *= ratio;
                        //e.OPHistories = e.OPHistories;
                        //var OP = e.CalcOP.ToBsonDocument().GetDouble(DayOrCost);
                        var OP = 0.0;
                        var LE = 0.0;
                        var TQ = 0.0;
                        var BIC = 0.0;
                        var Target = 0.0;
                        if (BaseOP != null)
                        {
                            var CurrentOP = e.BaseOP.Any() ? e.BaseOP.OrderByDescending(z => z).FirstOrDefault() : DefaultOP;
                            if (BaseOP.ToLower() == CurrentOP.ToLower())
                            {
                                //OP taken from phase
                                if (e.BaseOP.Contains(BaseOP))
                                {
                                    if (DayOrCost.ToLower() == "cost")
                                    {
                                        OP = e.Plan.Cost;
                                        LE = e.LE.Cost;
                                        TQ = e.TQ.Cost;
                                        BIC = e.BIC.Cost;
                                        Target = e.AggredTarget.Cost;
                                    }
                                    else
                                    {
                                        OP = e.Plan.Days;
                                        LE = e.LE.Days;
                                        TQ = e.TQ.Days;
                                        BIC = e.BIC.Days;
                                        Target = e.AggredTarget.Days;
                                    }
                                }
                            }
                            else
                            {
                                //OP taken from histories
                                var OPHist = d.OPHistories.Where(x => x.Type.Equals(BaseOP) && x.ActivityType.Equals(e.ActivityType) && x.PhaseNo == e.PhaseNo && x.PlanSchedule.Start != Tools.DefaultDate).FirstOrDefault();
                                if (OPHist != null)
                                {
                                    var ratioOPHist = GetRatio(OPHist.PlanSchedule);
                                    var ratioLSHist = GetRatio(OPHist.PhSchedule);
                                    var ratioLEHist = GetRatio(OPHist.LESchedule);

                                    OPHist.CalcOP.Cost *= ratioOPHist * WorkingInterest;
                                    OPHist.CalcOP.Days *= ratioOPHist;
                                    OPHist.Plan.Cost *= ratioOPHist * WorkingInterest;
                                    OPHist.Plan.Days *= ratioOPHist;
                                    OPHist.OP.Cost *= ratioLSHist * WorkingInterest;
                                    OPHist.OP.Days *= ratioLSHist;
                                    OPHist.LE.Cost *= ratioLEHist * WorkingInterest;
                                    OPHist.LE.Days *= ratioLEHist;

                                    if (DayOrCost.ToLower() == "cost")
                                    {
                                        OP = OPHist.Plan.Cost;
                                        TQ = OPHist.TQ.Cost;
                                        BIC = OPHist.BIC.Cost;
                                        Target = OPHist.Target.Cost;
                                        var getLE = OPHist.LE.Cost;//0.0;
                                        var wa = new WellActivity();
                                        if (wa.isHaveWeeklyReport2(d.WellName, d.UARigSequenceId, e.ActivityType)) getLE = OPHist.LE.Cost;
                                        LE = getLE;
                                    }
                                    else
                                    {
                                        OP = OPHist.Plan.Days;
                                        TQ = OPHist.TQ.Days;
                                        BIC = OPHist.BIC.Days;
                                        Target = OPHist.Target.Days;
                                        var getLE = OPHist.LE.Days;//0.0;
                                        var wa = new WellActivity();
                                        if (wa.isHaveWeeklyReport2(d.WellName, d.UARigSequenceId, e.ActivityType)) getLE = OPHist.LE.Days;
                                        LE = getLE;
                                    }
                                }
                            }
                        }


                        //LE = e.LE.ToBsonDocument().GetDouble(DayOrCost);
                        //if (LE == 0) LE = e.OP.ToBsonDocument().GetDouble(DayOrCost);
                        //if (LE == 0) LE = e.AFE.ToBsonDocument().GetDouble(DayOrCost);
                        //TQ = e.TQ.ToBsonDocument().GetDouble(DayOrCost);
                        totalOP += OP;
                        totalLE += LE;
                        totalTQ += TQ;
                        totalBIC += BIC;
                        totalTarget += Target;

                        if (FiscalYearStart == 0)
                            e.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(period, period);
                        else
                            e.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(period, FiscalYearStart, FiscalYearFinish);

                        e.PIPs = new List<PIPElement>();

                        var getWAUM = WellActivityUpdateMonthly.GetById(d.WellName, d.UARigSequenceId, e.PhaseNo, null, true);
                        if (getWAUM != null)
                        {
                            var totalWAUMElementsCompetitiveScope = 1;
                            var totalWAUMElementsEfficientExecution = 1;
                            var totalWAUMElementsSupplyChainTransformation = 1;
                            var totalWAUMElementsTechnologyAndInnovation = 1;

                            e.BankedSavingsCompetitiveScope = new WellDrillData() { Days = Tools.Div(getWAUM.BankedSavingsCompetitiveScope.Days, totalWAUMElementsCompetitiveScope), Cost = Tools.Div(getWAUM.BankedSavingsCompetitiveScope.Cost, totalWAUMElementsCompetitiveScope) };
                            e.BankedSavingsEfficientExecution = new WellDrillData() { Days = Tools.Div(getWAUM.BankedSavingsEfficientExecution.Days, totalWAUMElementsEfficientExecution), Cost = Tools.Div(getWAUM.BankedSavingsEfficientExecution.Cost, totalWAUMElementsEfficientExecution) };
                            e.BankedSavingsSupplyChainTransformation = new WellDrillData() { Days = Tools.Div(getWAUM.BankedSavingsSupplyChainTransformation.Days, totalWAUMElementsSupplyChainTransformation), Cost = Tools.Div(getWAUM.BankedSavingsSupplyChainTransformation.Cost, totalWAUMElementsSupplyChainTransformation) };
                            e.BankedSavingsTechnologyAndInnovation = new WellDrillData() { Days = Tools.Div(getWAUM.BankedSavingsTechnologyAndInnovation.Days, totalWAUMElementsTechnologyAndInnovation), Cost = Tools.Div(getWAUM.BankedSavingsTechnologyAndInnovation.Cost, totalWAUMElementsTechnologyAndInnovation) };
                        }
                        else
                        {
                            e.BankedSavingsCompetitiveScope = new WellDrillData();
                            e.BankedSavingsEfficientExecution = new WellDrillData();
                            e.BankedSavingsSupplyChainTransformation = new WellDrillData();

                            e.BankedSavingsTechnologyAndInnovation = new WellDrillData();
                        }

                        if (isIncludePIPs)
                        {
                            var queryForPIP = Query.And(
                                Query.EQ("WellName", d.WellName),
                                Query.EQ("ActivityType", e.ActivityType),
                                Query.EQ("SequenceId", d.UARigSequenceId)
                            );

                            var pipsInsideFu = WellPIP.Populate<WellPIP>(queryForPIP);
                            var pipsInside = pipsInsideFu
                                .Select(f =>
                                {
                                    if (f.Elements.Any())
                                    {
                                        // calculate shellshare to PIP
                                        //double wi = 1;
                                        //if (ShellShare)
                                        //    wi = d.WorkingInterest;
                                        f.Elements = f.Elements
                                        .Where(g => g.AssignTOOps.Contains(BaseOP))
                                        .Select(g =>
                                        {
                                            var ratioElement = GetRatio(g.Period);

                                            g.CostCurrentWeekImprovement *= (ratioElement);
                                            g.CostCurrentWeekRisk *= (ratioElement);
                                            g.CostPlanImprovement *= (ratioElement);
                                            g.CostPlanRisk *= (ratioElement);

                                            g.DaysCurrentWeekImprovement *= ratioElement;
                                            g.DaysCurrentWeekRisk *= ratioElement;
                                            g.DaysPlanImprovement *= ratioElement;
                                            g.DaysPlanRisk *= ratioElement;
                                            g.RatioElement = ratioElement;

                                            if (FiscalYearStart == 0)
                                            {
                                                g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, g.Period);
                                                g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, g.Period);
                                            }
                                            else
                                            {
                                                g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                                g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                            }
                                            return g;

                                        }).ToList();
                                    }

                                    if (f.CRElements != null && f.CRElements.Count > 0)
                                    {

                                        f.CRElements = f.CRElements.Select(g =>
                                        {
                                            var ratioElement = GetRatio(g.Period);

                                            g.CostCurrentWeekImprovement *= (ratioElement);
                                            g.CostCurrentWeekRisk *= (ratioElement);
                                            g.CostPlanImprovement *= (ratioElement);
                                            g.CostPlanRisk *= (ratioElement);

                                            g.DaysCurrentWeekImprovement *= ratioElement;
                                            g.DaysCurrentWeekRisk *= ratioElement;
                                            g.DaysPlanImprovement *= ratioElement;
                                            g.DaysPlanRisk *= ratioElement;
                                            g.RatioElement = ratioElement;

                                            if (FiscalYearStart == 0)
                                            {
                                                g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, g.Period);
                                                g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, g.Period);
                                            }
                                            else
                                            {
                                                g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                                g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                            }

                                            return g;
                                        }).ToList();

                                    }
                                    return f;
                                });

                            if (pipsInside.Any())
                            {
                                e.PIPs = pipsInside.SelectMany(f => f.Elements).ToList();
                                pips.AddRange(pipsInside);
                            }
                        }
                        return e;
                    }).ToList();

                    return d;
                }).ToList();

            var TotPhase = 0;
            foreach (var well in wellActivities)
            {
                if (well.Phases != null)
                {
                    TotPhase += well.Phases.Count;
                }
            }

            if ("Cost".Equals(DayOrCost))
            {
                totalOP /= 1000000;
                totalLE /= 1000000;
                totalTQ /= 1000000;
                totalBIC /= 1000000;
                totalTarget /= 1000000;
            }

            //var Phases = wellActivities.SelectMany(x => x.Phases).ToList();


            double wi = 1;
            if (ShellShare)
            {
                foreach (var p in pips)
                {
                    var wasx = wellActivities.Where(x => x.WellName.Equals(p.WellName) && x.UARigSequenceId.Equals(p.SequenceId) && x.RigName.Equals(p.RigName));
                    if (wasx.Any())
                    {

                        wi = wasx.FirstOrDefault().WorkingInterest;
                        foreach (var ele in p.Elements)
                        {
                            ele.CostCurrentWeekImprovement = ele.CostCurrentWeekImprovement * wi;
                            ele.CostCurrentWeekRisk = ele.CostCurrentWeekRisk * wi;
                            ele.CostPlanImprovement = ele.CostPlanImprovement * wi;
                            ele.CostPlanRisk = ele.CostPlanRisk * wi;
                        }
                        foreach (var ele in p.CRElements)
                        {
                            ele.CostCurrentWeekImprovement = ele.CostCurrentWeekImprovement * wi;
                            ele.CostCurrentWeekRisk = ele.CostCurrentWeekRisk * wi;
                            ele.CostPlanImprovement = ele.CostPlanImprovement * wi;
                            ele.CostPlanRisk = ele.CostPlanRisk * wi;
                        }


                        //foreach (var ele in p.Elements)
                        //{
                        //    ele.CostCurrentWeekImprovement = Tools.Div(ele.CostCurrentWeekImprovement, 1000000) * wi;
                        //    ele.CostCurrentWeekRisk = Tools.Div(ele.CostCurrentWeekRisk, 1000000) * wi;
                        //    ele.CostPlanImprovement = Tools.Div(ele.CostPlanImprovement, 1000000) * wi;
                        //    ele.CostPlanRisk = Tools.Div(ele.CostPlanRisk, 1000000) * wi;
                        //}
                        //foreach (var ele in p.CRElements)
                        //{
                        //    ele.CostCurrentWeekImprovement = Tools.Div(ele.CostCurrentWeekImprovement, 1000000) * wi;
                        //    ele.CostCurrentWeekRisk = Tools.Div(ele.CostCurrentWeekRisk, 1000000) * wi;
                        //    ele.CostPlanImprovement = Tools.Div(ele.CostPlanImprovement, 1000000) * wi;
                        //    ele.CostPlanRisk = Tools.Div(ele.CostPlanRisk, 1000000) * wi;
                        //}

                    }
                }
            }

            var tello = wellActivities;

            return new ActivitiesAndPIPs()
            {
                TotalOP = totalOP,
                TotalLE = totalLE,
                TotalTQ = totalTQ,
                TotalBIC = totalBIC,
                TotalTarget = totalTarget,
                Activities = wellActivities,
                PIPs = pips
            };


        }


        public ActivitiesAndPIPs GetMetadataChecker(bool isIncludePIPs = false)
        {
            string DefaultOP = "OP15";
            if (Config.GetConfigValue("BaseOPConfig") != null)
            {
                DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
            }

            PareseFilter();
            IMongoQuery queries = null;
            var queryBasic = new List<IMongoQuery>();
            queryBasic.Add(ParseQuery());
            if (queryBasic[0] != null)
            {
                queries = Query.And(queryBasic);
            }
            //add query data null
            var config2 = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "MetaDataConfig"));
            var newconfig = BsonHelper.Deserialize<MetadataConfig>(config2.FirstOrDefault().ToBsonDocument().Get("ConfigValue").ToBsonDocument());
            var rules = new List<string>();
            foreach (var rule in newconfig.Rules)
            {
                if (rule.Equals("Null"))
                {
                    rules.Add(null);
                }
                else
                {
                    rules.Add("");
                }
            }

            //var queryAdvance = new List<IMongoQuery>();
            //foreach (var field in newconfig.Fields)
            //{
            //    queryAdvance.Add(Query.In(field.Value, new BsonArray(rules)));
            //}
            //queries = Query.And(queryAdvance);
            //var query = ParseQuery();
            var pips = new List<WellPIP>();

            var totalOP = 0.0;
            var totalLE = 0.0;
            var totalTQ = 0.0;
            var totalBIC = 0.0;
            var totalTarget = 0.0;

            var was = WellActivity.Populate<WellActivity>(queries);
            //var countphases = 0;
            //var countUnphases = 0;
            var wellActivities = was
                .Select(d =>
                {
                    var ps = d.Phases = d.Phases.Where(e =>
                    {
                        var Check = true;
                        //if(rules.Contains( e.FundingType))
                        //{
                        //    Check = true;
                        //}
                        var isPeriodFiltered = FilterPeriod(GetPeriod(e));
                        var isActivityFiltered = true;
                        var isActivityCategoryFiltered = true;
                        var isFilcalFiltered = true;
                        var isLSValid = true;
                        var activityCategories = ActivityMaster.Populate<ActivityMaster>();
                        var isMatchedOPs = true;
                        if (OPs != null && OPs.Count() > 0)
                        {
                            isMatchedOPs = isMatchOP(e.BaseOP);

                        }

                        //for waterfall
                        var singleBaseOPFilter = true;
                        if (BaseOP != null)
                        {
                            if (!e.BaseOP.Contains(BaseOP)) singleBaseOPFilter = false;
                        }



                        if (Activities != null && Activities.Count > 0)
                            isActivityFiltered = Activities.Contains(e.ActivityType);
                        else
                        {
                            if (ActivitiesCategory != null && ActivitiesCategory.Count > 0)
                            {
                                var getActCat = activityCategories.Where(x => x._id.Equals(e.ActivityType)).FirstOrDefault();
                                if (getActCat != null) isActivityCategoryFiltered = ActivitiesCategory.Contains(getActCat.ActivityCategory);
                                else isActivityCategoryFiltered = false;
                            }
                        }

                        if (FiscalYearFinish != 0)
                        {
                            var phasePeriod = GetPeriod(e);
                            var isPhasePeriodInFiscalStart = phasePeriod.Start.Year >= FiscalYearStart && phasePeriod.Start.Year <= FiscalYearFinish;
                            var isPhasePeriodInFiscalFinish = phasePeriod.Finish.Year >= FiscalYearStart && phasePeriod.Finish.Year <= FiscalYearFinish;
                            if (isPhasePeriodInFiscalStart || isPhasePeriodInFiscalFinish)
                            {
                                isFilcalFiltered = true;
                            }
                            else
                            {
                                isFilcalFiltered = false;
                            }
                        }

                        //check only LS valid if DataFor == "Waterfall"
                        if (ValidLSOnly)
                        {
                            var lsStart = e.PhSchedule.Start;
                            if (lsStart == Tools.DefaultDate) isLSValid = false;
                        }

                        return Check && isMatchedOPs && singleBaseOPFilter && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isActivityCategoryFiltered && isLSValid;//
                    }).ToList();

                    ps.Select(e =>
                    {
                        var period = GetPeriod(e);
                        var ratio = GetRatio(period);
                        e.Ratio = ratio;
                        var ratioLS = GetRatio(e.PhSchedule);
                        var ratioLE = GetRatio(e.LESchedule);
                        e.RatioLS = ratioLS;
                        e.RatioLE = ratioLE;

                        var WorkingInterest = ShellShare ? d.WorkingInterest > 1 ? Tools.Div(d.WorkingInterest, 100) : d.WorkingInterest : 1;
                        e.CalcOP.Cost *= ratio * WorkingInterest;
                        e.CalcOP.Days *= ratio;
                        e.Plan.Cost *= ratio * WorkingInterest;
                        e.Plan.Days *= ratio;
                        e.OP.Cost *= ratioLS * WorkingInterest;
                        e.OP.Days *= ratioLS;
                        if (e.ActivityType.ToLower().Contains("risk"))
                        {
                            e.LE.Cost = 0.0;
                            e.LE.Days = 0.0;
                        }
                        else
                        {
                            e.LE.Cost *= ratioLE * WorkingInterest;
                            e.LE.Days *= ratioLE;
                        }
                        //e.AFE.Cost *= ratio;
                        //e.AFE.Days *= ratio;
                        //e.TQ.Cost *= ratio;
                        //e.TQ.Days *= ratio;
                        //e.OPHistories = e.OPHistories;
                        //var OP = e.CalcOP.ToBsonDocument().GetDouble(DayOrCost);
                        var OP = 0.0;
                        var LE = 0.0;
                        var TQ = 0.0;
                        var BIC = 0.0;
                        var Target = 0.0;
                        if (BaseOP != null)
                        {
                            var CurrentOP = e.BaseOP.Any() ? e.BaseOP.OrderByDescending(z => z).FirstOrDefault() : DefaultOP;
                            if (BaseOP.ToLower() == CurrentOP.ToLower())
                            {
                                //OP taken from phase
                                if (e.BaseOP.Contains(BaseOP))
                                {
                                    if (DayOrCost.ToLower() == "cost")
                                    {
                                        OP = e.Plan.Cost;
                                        LE = e.LE.Cost;
                                        TQ = e.TQ.Cost;
                                        BIC = e.BIC.Cost;
                                        Target = e.AggredTarget.Cost;
                                    }
                                    else
                                    {
                                        OP = e.Plan.Days;
                                        LE = e.LE.Days;
                                        TQ = e.TQ.Days;
                                        BIC = e.BIC.Days;
                                        Target = e.AggredTarget.Days;
                                    }
                                }
                            }
                            else
                            {
                                //OP taken from histories
                                var OPHist = d.OPHistories.Where(x => x.Type.Equals(BaseOP) && x.ActivityType.Equals(e.ActivityType) && x.PhaseNo == e.PhaseNo && x.PlanSchedule.Start != Tools.DefaultDate).FirstOrDefault();
                                if (OPHist != null)
                                {
                                    var ratioOPHist = GetRatio(OPHist.PlanSchedule);
                                    var ratioLSHist = GetRatio(OPHist.PhSchedule);
                                    var ratioLEHist = GetRatio(OPHist.LESchedule);

                                    OPHist.CalcOP.Cost *= ratioOPHist * WorkingInterest;
                                    OPHist.CalcOP.Days *= ratioOPHist;
                                    OPHist.Plan.Cost *= ratioOPHist * WorkingInterest;
                                    OPHist.Plan.Days *= ratioOPHist;
                                    OPHist.OP.Cost *= ratioLSHist * WorkingInterest;
                                    OPHist.OP.Days *= ratioLSHist;
                                    OPHist.LE.Cost *= ratioLEHist * WorkingInterest;
                                    OPHist.LE.Days *= ratioLEHist;

                                    if (DayOrCost.ToLower() == "cost")
                                    {
                                        OP = OPHist.Plan.Cost;
                                        TQ = OPHist.TQ.Cost;
                                        BIC = OPHist.BIC.Cost;
                                        Target = OPHist.Target.Cost;
                                        var getLE = OPHist.LE.Cost;//0.0;
                                        var wa = new WellActivity();
                                        if (wa.isHaveWeeklyReport2(d.WellName, d.UARigSequenceId, e.ActivityType)) getLE = OPHist.LE.Cost;
                                        LE = getLE;
                                    }
                                    else
                                    {
                                        OP = OPHist.Plan.Days;
                                        TQ = OPHist.TQ.Days;
                                        BIC = OPHist.BIC.Days;
                                        Target = OPHist.Target.Days;
                                        var getLE = OPHist.LE.Days;//0.0;
                                        var wa = new WellActivity();
                                        if (wa.isHaveWeeklyReport2(d.WellName, d.UARigSequenceId, e.ActivityType)) getLE = OPHist.LE.Days;
                                        LE = getLE;
                                    }
                                }
                            }
                        }


                        //LE = e.LE.ToBsonDocument().GetDouble(DayOrCost);
                        //if (LE == 0) LE = e.OP.ToBsonDocument().GetDouble(DayOrCost);
                        //if (LE == 0) LE = e.AFE.ToBsonDocument().GetDouble(DayOrCost);
                        //TQ = e.TQ.ToBsonDocument().GetDouble(DayOrCost);
                        totalOP += OP;
                        totalLE += LE;
                        totalTQ += TQ;
                        totalBIC += BIC;
                        totalTarget += Target;

                        if (FiscalYearStart == 0)
                            e.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(period, period);
                        else
                            e.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(period, FiscalYearStart, FiscalYearFinish);

                        e.PIPs = new List<PIPElement>();

                        var getWAUM = WellActivityUpdateMonthly.GetById(d.WellName, d.UARigSequenceId, e.PhaseNo, null, true);
                        if (getWAUM != null)
                        {
                            var totalWAUMElementsCompetitiveScope = 1;
                            var totalWAUMElementsEfficientExecution = 1;
                            var totalWAUMElementsSupplyChainTransformation = 1;
                            var totalWAUMElementsTechnologyAndInnovation = 1;

                            e.BankedSavingsCompetitiveScope = new WellDrillData() { Days = Tools.Div(getWAUM.BankedSavingsCompetitiveScope.Days, totalWAUMElementsCompetitiveScope), Cost = Tools.Div(getWAUM.BankedSavingsCompetitiveScope.Cost, totalWAUMElementsCompetitiveScope) };
                            e.BankedSavingsEfficientExecution = new WellDrillData() { Days = Tools.Div(getWAUM.BankedSavingsEfficientExecution.Days, totalWAUMElementsEfficientExecution), Cost = Tools.Div(getWAUM.BankedSavingsEfficientExecution.Cost, totalWAUMElementsEfficientExecution) };
                            e.BankedSavingsSupplyChainTransformation = new WellDrillData() { Days = Tools.Div(getWAUM.BankedSavingsSupplyChainTransformation.Days, totalWAUMElementsSupplyChainTransformation), Cost = Tools.Div(getWAUM.BankedSavingsSupplyChainTransformation.Cost, totalWAUMElementsSupplyChainTransformation) };
                            e.BankedSavingsTechnologyAndInnovation = new WellDrillData() { Days = Tools.Div(getWAUM.BankedSavingsTechnologyAndInnovation.Days, totalWAUMElementsTechnologyAndInnovation), Cost = Tools.Div(getWAUM.BankedSavingsTechnologyAndInnovation.Cost, totalWAUMElementsTechnologyAndInnovation) };
                        }
                        else
                        {
                            e.BankedSavingsCompetitiveScope = new WellDrillData();
                            e.BankedSavingsEfficientExecution = new WellDrillData();
                            e.BankedSavingsSupplyChainTransformation = new WellDrillData();

                            e.BankedSavingsTechnologyAndInnovation = new WellDrillData();
                        }

                        if (isIncludePIPs)
                        {
                            var queryForPIP = Query.And(
                                Query.EQ("WellName", d.WellName),
                                Query.EQ("ActivityType", e.ActivityType),
                                Query.EQ("SequenceId", d.UARigSequenceId)
                            );

                            var pipsInsideFu = WellPIP.Populate<WellPIP>(queryForPIP);
                            var pipsInside = pipsInsideFu
                                .Select(f =>
                                {
                                    if (f.Elements.Any())
                                    {
                                        // calculate shellshare to PIP
                                        //double wi = 1;
                                        //if (ShellShare)
                                        //    wi = d.WorkingInterest;
                                        f.Elements = f.Elements
                                        .Where(g => g.AssignTOOps.Contains(BaseOP))
                                        .Select(g =>
                                        {
                                            var ratioElement = GetRatio(g.Period);

                                            g.CostCurrentWeekImprovement *= (ratioElement);
                                            g.CostCurrentWeekRisk *= (ratioElement);
                                            g.CostPlanImprovement *= (ratioElement);
                                            g.CostPlanRisk *= (ratioElement);

                                            g.DaysCurrentWeekImprovement *= ratioElement;
                                            g.DaysCurrentWeekRisk *= ratioElement;
                                            g.DaysPlanImprovement *= ratioElement;
                                            g.DaysPlanRisk *= ratioElement;
                                            g.RatioElement = ratioElement;

                                            if (FiscalYearStart == 0)
                                            {
                                                g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, g.Period);
                                                g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, g.Period);
                                            }
                                            else
                                            {
                                                g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                                g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                            }
                                            return g;

                                        }).ToList();
                                    }

                                    if (f.CRElements != null && f.CRElements.Count > 0)
                                    {

                                        f.CRElements = f.CRElements.Select(g =>
                                        {
                                            var ratioElement = GetRatio(g.Period);

                                            g.CostCurrentWeekImprovement *= (ratioElement);
                                            g.CostCurrentWeekRisk *= (ratioElement);
                                            g.CostPlanImprovement *= (ratioElement);
                                            g.CostPlanRisk *= (ratioElement);

                                            g.DaysCurrentWeekImprovement *= ratioElement;
                                            g.DaysCurrentWeekRisk *= ratioElement;
                                            g.DaysPlanImprovement *= ratioElement;
                                            g.DaysPlanRisk *= ratioElement;
                                            g.RatioElement = ratioElement;

                                            if (FiscalYearStart == 0)
                                            {
                                                g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, g.Period);
                                                g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, g.Period);
                                            }
                                            else
                                            {
                                                g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                                g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                            }

                                            return g;
                                        }).ToList();

                                    }
                                    return f;
                                });

                            if (pipsInside.Any())
                            {
                                e.PIPs = pipsInside.SelectMany(f => f.Elements).ToList();
                                pips.AddRange(pipsInside);
                            }
                        }
                        return e;
                    }).ToList();
                    return d;
                }).ToList();





            return new ActivitiesAndPIPs()
            {
                Activities = wellActivities,
            };
        }

        public ActivitiesAndPIPs GetActivitiesAndPIPsFromOPHistory(bool isIncludePIPs = true, string BaseOP = "OP14")
        {
            PareseFilter();
            var query = ParseQuery();
            var pips = new List<WellPIP>();

            var totalOP = 0.0;
            var totalLE = 0.0;
            var totalTQ = 0.0;
            var totalBIC = 0.0;

            var wellActivities = WellActivity.Populate<WellActivity>(query)
                .Select(d =>
                {
                    d.Phases = d.Phases.Where(e =>
                    {
                        var isNotRisk = !e.ActivityType.ToLower().Contains("risk");
                        var isPeriodFiltered = FilterPeriod(GetPeriod(e));
                        var isActivityFiltered = true;
                        var isFilcalFiltered = true;

                        if (Activities != null && Activities.Count > 0)
                            isActivityFiltered = Activities.Contains(e.ActivityType);

                        if (FiscalYearFinish != 0)
                        {
                            var phasePeriod = GetPeriod(e);
                            var isPhasePeriodInFiscalStart = phasePeriod.Start.Year >= FiscalYearStart && phasePeriod.Start.Year <= FiscalYearFinish;
                            var isPhasePeriodInFiscalFinish = phasePeriod.Finish.Year >= FiscalYearStart && phasePeriod.Finish.Year <= FiscalYearFinish;
                            if (isPhasePeriodInFiscalStart || isPhasePeriodInFiscalFinish)
                            {
                                isFilcalFiltered = true;
                            }
                            else
                            {
                                isFilcalFiltered = false;
                            }
                        }

                        return isNotRisk && isPeriodFiltered && isActivityFiltered && isFilcalFiltered;
                    })
                    .Select(e =>
                    {
                        var period = GetPeriod(e);
                        var ratio = GetRatio(period);
                        e.Ratio = ratio;

                        e.CalcOP.Cost *= ratio;
                        e.CalcOP.Days *= ratio;
                        e.Plan.Cost *= ratio;
                        e.Plan.Days *= ratio;
                        e.OP.Cost *= ratio;
                        e.OP.Days *= ratio;
                        e.LE.Cost *= ratio;
                        e.LE.Days *= ratio;
                        e.AFE.Cost *= ratio;
                        e.AFE.Days *= ratio;
                        e.TQ.Cost *= ratio;
                        e.TQ.Days *= ratio;

                        // e.OPHistories = e.OPHistories;

                        var OP = e.CalcOP.ToBsonDocument().GetDouble(DayOrCost);

                        var LE = e.LE.ToBsonDocument().GetDouble(DayOrCost);
                        if (LE == 0) LE = e.OP.ToBsonDocument().GetDouble(DayOrCost);
                        if (LE == 0) LE = e.AFE.ToBsonDocument().GetDouble(DayOrCost);

                        var TQ = e.TQ.ToBsonDocument().GetDouble(DayOrCost);
                        var BIC = e.BIC.ToBsonDocument().GetDouble(DayOrCost);

                        totalOP += OP;
                        totalLE += LE;
                        totalTQ += TQ;
                        totalBIC += BIC;

                        if (FiscalYearStart == 0)
                            e.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(period, period);
                        else
                            e.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(period, FiscalYearStart, FiscalYearFinish);

                        e.PIPs = new List<PIPElement>();

                        if (isIncludePIPs)
                        {
                            var queryForPIP = Query.And(
                                Query.EQ("WellName", d.WellName),
                                Query.EQ("ActivityType", e.ActivityType),
                                Query.EQ("SequenceId", d.UARigSequenceId)
                            );

                            var pipsInside = WellPIP.Populate<WellPIP>(queryForPIP)
                                .Select(f =>
                                {
                                    if (f.Elements != null && f.Elements.Count > 0)
                                    {
                                        f.Elements = f.Elements.Select(g =>
                                        {
                                            var ratioElement = GetRatio(g.Period);

                                            g.CostCurrentWeekImprovement *= ratioElement;
                                            g.CostCurrentWeekRisk *= ratioElement;
                                            g.CostPlanImprovement *= ratioElement;
                                            g.CostPlanRisk *= ratioElement;

                                            g.DaysCurrentWeekImprovement *= ratioElement;
                                            g.DaysCurrentWeekRisk *= ratioElement;
                                            g.DaysPlanImprovement *= ratioElement;
                                            g.DaysPlanRisk *= ratioElement;
                                            g.RatioElement = ratioElement;

                                            if (FiscalYearStart == 0)
                                            {
                                                g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, g.Period);
                                                g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, g.Period);
                                            }
                                            else
                                            {
                                                g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                                g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                            }

                                            return g;
                                        }).ToList();
                                    }

                                    if (f.CRElements != null && f.CRElements.Count > 0)
                                    {
                                        f.CRElements = f.CRElements.Select(g =>
                                        {
                                            var ratioElement = GetRatio(g.Period);

                                            g.CostCurrentWeekImprovement *= ratioElement;
                                            g.CostCurrentWeekRisk *= ratioElement;
                                            g.CostPlanImprovement *= ratioElement;
                                            g.CostPlanRisk *= ratioElement;

                                            g.DaysCurrentWeekImprovement *= ratioElement;
                                            g.DaysCurrentWeekRisk *= ratioElement;
                                            g.DaysPlanImprovement *= ratioElement;
                                            g.DaysPlanRisk *= ratioElement;
                                            g.RatioElement = ratioElement;

                                            if (FiscalYearStart == 0)
                                            {
                                                g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, g.Period);
                                                g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, g.Period);
                                            }
                                            else
                                            {
                                                g.AnnualProportions = DateRangeToMonth.ProportionNumDaysPerYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                                g.MonthlyProportions = DateRangeToMonth.ProportionNumDaysPerMonthYear(g.Period, FiscalYearStart, FiscalYearFinish);
                                            }

                                            return g;
                                        }).ToList();
                                    }

                                    return f;
                                });

                            if (pipsInside != null && pipsInside.Count() > 0)
                            {
                                e.PIPs = pipsInside.SelectMany(f => f.Elements).ToList();
                                pips.AddRange(pipsInside);
                            }
                        }

                        return e;
                    }).ToList();

                    return d;
                }).ToList();

            if ("Cost".Equals(DayOrCost))
            {
                totalOP /= 1000000;
                totalLE /= 1000000;
                totalTQ /= 1000000;
                totalBIC /= 1000000;
            }

            return new ActivitiesAndPIPs()
            {
                TotalOP = totalOP,
                TotalLE = totalLE,
                TotalTQ = totalTQ,
                TotalBIC = totalBIC,
                Activities = wellActivities,
                PIPs = pips
            };
        }

        public List<BizPlanActivity> GetActivitiesForBizPlanCatalog(string email = "", SortByBuilder sort = null)
        {

            PareseFilter();
            List<IMongoQuery> queries = new List<IMongoQuery>();
            List<IMongoQuery> queriesWoLE = new List<IMongoQuery>();
            var y = GetWellActivitiesQuery(email, "WellName", "Phases.ActivityType");

            queries.Add(Query.In("Phases.Estimate.Status", new BsonArray { "Modified", "Draft", "Complete" }));

            queries.Add(y);
            queriesWoLE.Add(y);

            var queryBizPlan = ParseQueryForBizPlan();
            if (queryBizPlan != null)
            {
                queries.Add(queryBizPlan);
            }

            var query = Query.And(queries);

            var query2 = Query.And(queriesWoLE);

            var now = Tools.ToUTC(DateTime.Now);
            var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));

            var wellActivities = BizPlanActivity.GetAll(query, sort: sort, isMergeWithWellPlan: true, queryWoLe: query2)
                .Select(d =>
                {
                    d.Phases = d.Phases.Where(e =>
                    {
                        var isPeriodFiltered = FilterPeriod(GetPeriodForBizPlan(e));
                        var isActivityFiltered = true;
                        var isFilcalFiltered = true;
                        var isGreaterThanThisMonth = true; //d.Phases.Where(x => x.LESchedule.Finish >= earlyThisMonth).Count() > 0;

                        if (Activities != null && Activities.Count > 0)
                            isActivityFiltered = Activities.Contains(e.ActivityType);

                        var isMatchedOPs = true;

                        //if (OPs != null && OPs.Count() > 0)
                        //{
                        //    //isMatchedOPs = isMatchOP(e.BaseOP);
                        //    isMatchedOPs = FunctionHelper.CompareBaseOP(OPs.ToArray(), e.BaseOP.ToArray(), opRelation);
                        //}

                        if (FiscalYearFinish != 0)
                        {
                            var phasePeriod = GetPeriodForBizPlan(e);
                            var isPhasePeriodInFiscalStart = phasePeriod.Start.Year >= FiscalYearStart && phasePeriod.Start.Year <= FiscalYearFinish;
                            var isPhasePeriodInFiscalFinish = phasePeriod.Finish.Year >= FiscalYearStart && phasePeriod.Finish.Year <= FiscalYearFinish;
                            if (isPhasePeriodInFiscalStart || isPhasePeriodInFiscalFinish)
                            {
                                isFilcalFiltered = true;
                            }
                            else
                            {
                                isFilcalFiltered = false;
                            }
                        }

                        return isMatchedOPs && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isGreaterThanThisMonth;
                    }).ToList();

                    return d;
                })
                .Where(d => d.Phases != null && d.Phases.Count > 0)
                .OrderBy(d => d.WellName)
                .ToList();

            return wellActivities;
        }

        public List<BizPlanActivity> GetActivitiesForBizPlanForFiscalYear(string email = "", SortByBuilder sort = null)
        {

            PareseFilter();
            List<IMongoQuery> queries = new List<IMongoQuery>();
            List<IMongoQuery> queriesWoLE = new List<IMongoQuery>();
            var y = GetWellActivitiesQuery(email, "WellName", "Phases.ActivityType");
            queries.Add(y);
            queriesWoLE.Add(y);

            var queryBizPlan = ParseQueryForBizPlan();
            if (queryBizPlan != null)
                queries.Add(queryBizPlan);

            var query = Query.And(queries);

            var query2 = Query.And(queriesWoLE);

            var now = Tools.ToUTC(DateTime.Now);
            var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));

            var lastDateExecutedLS = WellActivity.GetLastExecuteDateLS();
            var qLogLS = Query.And(Query.GTE("Executed_At", Tools.ToUTC(lastDateExecutedLS)));
            var logLast = DataHelper.Populate("LogLatestLS", qLogLS);
            var activityCategories = ActivityMaster.Populate<ActivityMaster>();

            var wellActivities = BizPlanActivity.GetAll(query, sort: sort, isMergeWithWellPlan: true, queryWoLe: query2);
            wellActivities = wellActivities.Select(d =>
                {
                    d.Phases = d.Phases.Where(e =>
                    {
                        var isPeriodFiltered = FilterPeriod(GetPeriodForBizPlan(e));
                        var isActivityFiltered = true;
                        var isFilcalFiltered = true;
                        var isCOmpleteOfModified = false;
                        var isActivityCategoryFiltered = true;
                        var isGreaterThanThisMonth = true; //d.Phases.Where(x => x.LESchedule.Finish >= earlyThisMonth).Count() > 0;

                        var isHaveWR = true;

                        var isInsideStatusFilter = true;

                        if (WellActivity.isHaveWeeklyReport(d.WellName, d.UARigSequenceId, e.ActivityType))
                            isHaveWR = false;

                        if (Activities != null && Activities.Count > 0)
                            isActivityFiltered = Activities.Contains(e.ActivityType.Trim());
                        else
                        {
                            if (ActivitiesCategory != null && ActivitiesCategory.Count > 0)
                            {
                                var getActCat = activityCategories.Where(x => x._id.ToString().Trim().Equals(e.ActivityType.Trim())).FirstOrDefault();
                                if (getActCat != null) isActivityCategoryFiltered = ActivitiesCategory.Contains(getActCat.ActivityCategory);
                                else isActivityCategoryFiltered = false;
                            }
                        }

                        if (this.Status != null && this.Status.Count() > 0)
                        {
                            if (!this.Status.Contains(e.Estimate.Status))
                                isInsideStatusFilter = false;
                        }

                        if (e.Estimate.Status != null)
                        {
                            if (e.Estimate.Status.Trim().ToLower() == "complete" || e.Estimate.Status.Trim().ToLower() == "modified" || e.Estimate.Status.Trim().ToLower() == "draft" || e.Estimate.Status.Trim().ToLower() == "meta data missing" || e.Estimate.isFirstInitiate == false)
                            {
                                isCOmpleteOfModified = true;
                            }
                        }

                        var isMatchedOPs = true;

                        if (OPs != null && OPs.Count() > 0)
                        {
                            if (OPs.Contains(e.Estimate.SaveToOP))
                                isMatchedOPs = true;
                            else
                                isMatchedOPs = false;

                            //isMatchedOPs = isMatchOP(e.BaseOP);
                            //isMatchedOPs = FunctionHelper.CompareBaseOP(OPs.ToArray(), e.BaseOP.ToArray(), opRelation);
                        }


                        if (FiscalYearFinish != 0)
                        {
                            var phasePeriod = GetPeriodForBizPlan(e);
                            var isPhasePeriodInFiscalStart = phasePeriod.Start.Year >= FiscalYearStart && phasePeriod.Start.Year <= FiscalYearFinish;
                            var isPhasePeriodInFiscalFinish = phasePeriod.Finish.Year >= FiscalYearStart && phasePeriod.Finish.Year <= FiscalYearFinish;
                            if (isPhasePeriodInFiscalStart || isPhasePeriodInFiscalFinish)
                            {
                                isFilcalFiltered = true;
                            }
                            else
                            {
                                isFilcalFiltered = false;
                            }
                        }


                        var islastesLs = false;


                        //var dataAda = CheckPhaseInLatestLS(d.WellName, e.ActivityType, d.RigName, lastDateExecutedLS);
                        var dataAda = logLast.Any(x =>
                            x.GetString("Well_Name") == d.WellName &&
                            x.GetString("Activity_Type") == e.ActivityType &&
                            x.GetString("Rig_Name") == d.RigName
                            );
                        bool isReallyCheckLatestLS = false;
                        if (!String.IsNullOrEmpty(inlastuploadlsBoth))
                        {
                            if (inlastuploadls == true)
                            {
                                if (dataAda)
                                    isReallyCheckLatestLS = true;
                            }
                            else if (inlastuploadls == false)
                            {
                                if (dataAda == false)
                                {
                                    var checkAnyLS = logLast.Any(x =>
                                        x.GetString("Well_Name") != d.WellName &&
                                        x.GetString("Activity_Type") != e.ActivityType &&
                                        x.GetString("Rig_Name") != d.RigName
                                   );
                                    if (checkAnyLS || !logLast.Any())
                                        isReallyCheckLatestLS = true;
                                }
                            }  
                        }

                        //if (inlastuploadls == dataAda)
                        //{
                        //    islastesLs = true;
                        //}

                        var isInFundingType = true;
                        if (ExType != null && ExType.Count() > 0)
                        {
                            if (!ExType.Contains(e.FundingType))
                            {
                                isInFundingType = false;
                            }
                        }

                        if (String.IsNullOrEmpty(inlastuploadlsBoth))
                            return isInsideStatusFilter && isHaveWR && isMatchedOPs && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isGreaterThanThisMonth && isActivityCategoryFiltered && isCOmpleteOfModified && isInFundingType;
                        else
                            return isReallyCheckLatestLS && isInsideStatusFilter && isHaveWR && isMatchedOPs && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isGreaterThanThisMonth && isActivityCategoryFiltered && isCOmpleteOfModified && isInFundingType;

                    }).ToList();

                    return d;
                })
                .Where(d => d.Phases != null && d.Phases.Count > 0)
                .OrderBy(d => d.WellName)
                .ToList();
            logLast = new List<BsonDocument>();
            activityCategories = new List<ActivityMaster>();
            return wellActivities;
        }

        public List<Excelbusplan> GetActivitiesForBizPlanForFiscalYearSP(string email = "", SortByBuilder sort = null)
        {

            PareseFilter();
            List<IMongoQuery> queries = new List<IMongoQuery>();
            List<IMongoQuery> queriesWoLE = new List<IMongoQuery>();
            var y = GetWellActivitiesQuery(email, "WellName", "Phases.ActivityType");
            queries.Add(y);
            queriesWoLE.Add(y);

            var queryBizPlan = ParseQueryForBizPlan();
            if (queryBizPlan != null)
                queries.Add(queryBizPlan);

            var query = Query.And(queries);

            var query2 = Query.And(queriesWoLE);

            var now = Tools.ToUTC(DateTime.Now);
            var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));

            var lastDateExecutedLS = WellActivity.GetLastExecuteDateLS();

            List<Excelbusplan> edetail = new List<Excelbusplan>();
            var accessibleWells = GetWellActivities(email);
            var wellaloc = BizPlanAllocation.Populate<BizPlanAllocation>(q: query);
            var activityCategories = ActivityMaster.Populate<ActivityMaster>();
            var timeOut = new Stopwatch();
            timeOut.Start();
            var wellact = BizPlanActivity.Populate<BizPlanActivity>(q: query)
            .Select(d =>
                {
                    d.Phases = d.Phases.Where(e =>
                    {

                        var isPeriodFiltered = FilterPeriod(GetPeriodForBizPlan(e));
                        var isActivityFiltered = true;
                        var isFilcalFiltered = true;
                        var isCOmpleteOfModified = false;
                        var isActivityCategoryFiltered = true;
                        var isGreaterThanThisMonth = true; //d.Phases.Where(x => x.LESchedule.Finish >= earlyThisMonth).Count() > 0;

                        if (Activities != null && Activities.Count > 0)
                            isActivityFiltered = Activities.Contains(e.ActivityType.Trim());
                        else
                        {
                            if (ActivitiesCategory != null && ActivitiesCategory.Count > 0)
                            {
                                var getActCat = activityCategories.Where(x => x._id.ToString().Trim().Equals(e.ActivityType.Trim())).FirstOrDefault();
                                if (getActCat != null) isActivityCategoryFiltered = ActivitiesCategory.Contains(getActCat.ActivityCategory);
                                else isActivityCategoryFiltered = false;
                            }
                        }

                        if (e.Estimate.Status != null)
                        {
                            if (e.Estimate.Status.Trim().ToLower() == "complete" || e.Estimate.Status.Trim().ToLower() == "modified" || e.Estimate.Status.Trim().ToLower() == "draft" || e.Estimate.isFirstInitiate == false)
                            {
                                isCOmpleteOfModified = true;
                            }
                        }

                        var isMatchedOPs = true;

                        if (OPs != null && OPs.Count() > 0)
                        {
                            if (OPs.Contains(e.Estimate.SaveToOP))
                                isMatchedOPs = true;
                            else
                                isMatchedOPs = false;

                            //isMatchedOPs = isMatchOP(e.BaseOP);
                            //isMatchedOPs = FunctionHelper.CompareBaseOP(OPs.ToArray(), e.BaseOP.ToArray(), opRelation);
                        }


                        if (FiscalYearFinish != 0)
                        {
                            var phasePeriod = GetPeriodForBizPlan(e);
                            var isPhasePeriodInFiscalStart = phasePeriod.Start.Year >= FiscalYearStart && phasePeriod.Start.Year <= FiscalYearFinish;
                            var isPhasePeriodInFiscalFinish = phasePeriod.Finish.Year >= FiscalYearStart && phasePeriod.Finish.Year <= FiscalYearFinish;
                            if (isPhasePeriodInFiscalStart || isPhasePeriodInFiscalFinish)
                            {
                                isFilcalFiltered = true;
                            }
                            else
                            {
                                isFilcalFiltered = false;
                            }
                        }


                        var islastesLs = false;


                        var dataAda = CheckPhaseInLatestLS(d.WellName, e.ActivityType, d.RigName, lastDateExecutedLS);

                        if (inlastuploadls == dataAda)
                        {
                            islastesLs = true;
                        }

                        var isInFundingType = true;
                        if (ExType != null && ExType.Count() > 0)
                        {
                            if (!ExType.Contains(e.FundingType))
                            {
                                isInFundingType = false;
                            }
                        }
                        bool allcheckList = false;
                        var aloc = wellaloc.Where(x => x.WellName.Equals(d.WellName) && x.ActivityType.Equals(e.ActivityType) && x.RigName.Equals(d.RigName) && x.UARigSequenceId != null && x.UARigSequenceId.Equals(d.UARigSequenceId)
                             && (x.EstimatePeriod.Start.Date == e.Estimate.EstimatePeriod.Start.Date && x.EstimatePeriod.Finish.Date == e.Estimate.EstimatePeriod.Finish.Date));
                        //var aloc2 = BizPlanAllocation.Get<BizPlanAllocation>(Query.And(Query.EQ("WellName", d.WellName), Query.EQ("RigName", d.RigName), Query.EQ("UARigSequenceId", d.UARigSequenceId), Query.EQ("ActivityType", e.ActivityType), Query.EQ("SaveToOP", e.Estimate.Status)));
                        if (inlastuploadls == false)
                            if (isMatchedOPs && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isGreaterThanThisMonth && isActivityCategoryFiltered && isCOmpleteOfModified && isInFundingType)
                                allcheckList = true;
                            else
                                if (islastesLs && isMatchedOPs && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isGreaterThanThisMonth && isActivityCategoryFiltered && isCOmpleteOfModified && isInFundingType)
                                    allcheckList = true;
                        if (allcheckList)
                        {
                            if (aloc != null & aloc.Count() > 0)
                                e.Allocation = aloc.FirstOrDefault();
                            var check = CheckAccessibleWellAndActivity(accessibleWells, d.WellName, e.ActivityType);
                            if (check)
                            {
                                int i = 0;
                                var isHaveWeeklyReport = WellActivity.isHaveWeeklyReport(d.WellName, d.UARigSequenceId, e.ActivityType);
                                if (!isHaveWeeklyReport)
                                {
                                    e.Estimate.EstimatePeriod.Start = Tools.ToUTC(e.Estimate.EstimatePeriod.Start, true);
                                    e.Estimate.EstimatePeriod.Finish = Tools.ToUTC(e.Estimate.EstimatePeriod.Finish);
                                    e.Estimate.EventStartDate = Tools.ToUTC(e.Estimate.EventStartDate, true);
                                    e.Estimate.EventEndDate = Tools.ToUTC(e.Estimate.EventEndDate);

                                    var Macro = MacroEconomic.Get<MacroEconomic>(
                                    Query.And(
                                    Query.EQ("Currency", d.Currency == null ? "" : d.Currency),
                                    Query.EQ("BaseOP", e.Estimate.SaveToOP == null ? "" : e.Estimate.SaveToOP),
                                    Query.EQ("Country", e.Estimate.Country == null ? "" : d.Country)
                                        )
                                    );
                                    var rate = 0;
                                    if (Macro == null)
                                    {
                                        rate = 0;
                                    }
                                    else
                                    {
                                        var getRate = Macro.ExchangeRate.AnnualValues.Where(x => x.Year == e.Estimate.EstimatePeriod.Start.Year)
                                        .Any() ? Macro.ExchangeRate.AnnualValues.Where(x => x.Year == e.Estimate.EstimatePeriod.Start.Year).FirstOrDefault().Value : 0;
                                        rate = Convert.ToInt32(getRate);
                                    }

                                    BizPlanSummary bisCal = new BizPlanSummary(d.WellName, d.RigName, e.ActivityType, d.Country == null ? "" : d.Country,
                                    d.ShellShare, e.Estimate.EstimatePeriod, e.Estimate.MaturityLevel == null ? 0 : Convert.ToInt32(e.Estimate.MaturityLevel.Replace("Type", "")),
                                    e.Estimate.Services, e.Estimate.Materials,
                                    e.Estimate.TroubleFreeBeforeLC.Days,
                                    d.ReferenceFactorModel == null ? "" : d.ReferenceFactorModel,
                                    Tools.Div(e.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(e.Estimate.NewTECOPTime.PercentDays, 100),
                                    Tools.Div(e.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(e.Estimate.NewTECOPTime.PercentCost, 100),
                                    e.Estimate.LongLeadMonthRequired, Tools.Div(e.Estimate.PercOfMaterialsLongLead, 100), baseOP: e.Estimate.SaveToOP == null ? "" : e.Estimate.SaveToOP, isGetExcRateByCurrency: true,
                                    lcf: e.Estimate.NewLearningCurveFactor
                                    );

                                    bisCal.GeneratePeriodBucket();
                                    //set object
                                    Excelbusplan eb = new Excelbusplan();
                                    eb.Status = e.Estimate.Status;
                                    eb.LineOfBusiness = d.LineOfBusiness;
                                    eb.RigType = d.RigType;
                                    eb.Region = d.Region;
                                    eb.Country = d.Country == null ? "" : d.Country;
                                    eb.OperatingUnit = d.OperatingUnit;
                                    eb.PerformanceUnit = d.PerformanceUnit;
                                    eb.AssetName = d.AssetName;
                                    eb.ProjectName = d.ProjectName;
                                    eb.RigName = d.RigName;
                                    eb.WellName = d.WellName;
                                    eb.ActivityType = e.ActivityType;
                                    eb.RigSeqId = d.UARigSequenceId;
                                    eb.Currency = d.Currency == null ? "" : d.Currency;
                                    eb.ShellShare = d.ShellShare;
                                    eb.SaveToOP = e.Estimate.SaveToOP == null ? "" : e.Estimate.SaveToOP;
                                    //eb.LastSubmitted = d.LastUpdate;
                                    eb.LastSubmitted = e.Estimate.LastUpdate;// d.LastUpdate;
                                    eb.DataInputBy = e.Estimate.LastUpdateBy;
                                    //eb.DataInputBy = e.Estimate.LastUpdateBy;// d.DataInputBy;


                                    eb.InPlan = d.isInPlan ? true : false;
                                    //eb.RFM = d.ReferenceFactorModel;
                                    eb.RFM = d.ReferenceFactorModel == "default" ? e.PhaseInfo.ReferenceFactorModel : d.ReferenceFactorModel;
                                    eb.FundingType = e.FundingType;
                                    eb.Event = new DateRange() { Start = e.Estimate.EventStartDate, Finish = e.Estimate.EventStartDate.Date.AddDays(bisCal.MeanTime) };
                                    eb.EventDays = Math.Round(bisCal.MeanTime, 2);
                                    eb.RigRate = Math.Round(e.Estimate.RigRate);
                                    eb.Services = bisCal.Services; // e.Estimate.Services;
                                    eb.Materials = bisCal.Material; //e.Estimate.Materials;
                                    eb.isMaterialLLSetManually = e.Estimate.isMaterialLLSetManually ? true : false;
                                    eb.PercOfMaterialsLongLead = Tools.Div(e.Estimate.PercOfMaterialsLongLead, 100);
                                    eb.LLRequiredMonth = e.Estimate.LongLeadMonthRequired;
                                    eb.LLCalc = e.Estimate.LongLeadCalc;
                                    eb.BurnRate = bisCal.BurnRate; // e.Estimate.BurnRate;
                                    eb.SpreadRate = bisCal.SpreadRateWRig; // e.Estimate.SpreadRate;
                                    eb.SpreadRateTotal = bisCal.SpreadRateTotal;// e.Estimate.SpreadRateTotal;
                                    eb.IsUsingLCFfromRFM = e.Estimate.IsUsingLCFfromRFM;
                                    ////OP16
                                    eb.UseTAApproved = e.Estimate.IsUsingLCFfromRFM;
                                    eb.TroubleFree = new WellDrillData()
                                    {
                                        Days = bisCal.TroubleFree.Days,// e.Estimate.NewTroubleFree.Days,
                                        Cost = bisCal.TroubleFree.Cost //e.Estimate.NewTroubleFree.Cost
                                    };
                                    eb.LCFParameter = bisCal.LCF;// e.Estimate.NewLearningCurveFactor;
                                    eb.NPT = new WellDrillDataPercent()
                                    {
                                        Days = bisCal.NPT.Days,// e.Estimate.NewNPTTime.PercentDays == 0 ? 0 : e.Estimate.NewNPTTime.Days,
                                        Cost = bisCal.NPT.Cost,// e.Estimate.NewNPTTime.Cost,
                                        PercentTime = bisCal.NPTRate * 100, //e.Estimate.NewNPTTime.PercentDays,
                                        PercentCost = bisCal.NPTRateCost // Tools.Div(e.Estimate.NewNPTTime.PercentCost, 100)
                                    };

                                    eb.LCF = new WellDrillData()
                                    {
                                        Days = bisCal.NewLCFValue.Days, //e.Estimate.NewLCFValue.Days,
                                        Cost = bisCal.NewLCFValue.Cost  //Math.Round(e.Estimate.NewLearningCurveFactor * (e.Estimate.NewTroubleFree.Cost + e.Estimate.NewNPTTime.Cost), 2)
                                    };
                                    eb.Base = new WellDrillData()
                                    {
                                        Days = bisCal.NewBaseValue.Days, //Math.Round(e.Estimate.NewBaseValue.Days, 2),
                                        Cost = bisCal.NewBaseValue.Cost  //bisCal.NewBaseValue.Cost // e.Estimate.NewTroubleFreeUSD + e.Estimate.NPTCostUSD
                                    };
                                    eb.BaseCalc = new WellDrillData()
                                    {
                                        Days = eb.Base.Days - eb.LCF.Days, //Math.Round(e.Estimate.NewBaseValue.Days - e.Estimate.NewLCFValue.Days, 2),
                                        Cost = eb.Base.Cost - eb.LCF.Cost,  //(e.Estimate.NewTroubleFreeUSD + e.Estimate.NPTCostUSD) - e.Estimate.NewLCFValue.Cost
                                    };
                                    eb.TECOP = new WellDrillDataPercent()
                                    {
                                        Days = bisCal.TECOP.Days, //Math.Round(e.Estimate.NewTECOPTime.PercentDays == 0 ? 0 : e.Estimate.NewTECOPTime.Days, 2),
                                        Cost = bisCal.TECOP.Cost,// e.Estimate.NewTECOPTime.Cost,
                                        PercentTime = bisCal.TECOPRate * 100,//  e.Estimate.NewTECOPTime.PercentDays,
                                        PercentCost = bisCal.TECOPRateCost //Tools.Div(e.Estimate.NewTECOPTime.PercentCost, 100)
                                    };

                                    eb.Mean = new WellDrillData()
                                    {
                                        Days = bisCal.MeanTime,//Math.Round(e.Estimate.NewMean.Days, 2),
                                        Cost = bisCal.MeanCostEDM,// e.Estimate.NewMean.Cost
                                    };
                                    //convert to usd
                                    eb.TroubleFreeCostUSD = bisCal.TroubleFreeCostUSD;// e.Estimate.NewTroubleFreeUSD;
                                    eb.NptCostUSD = bisCal.NPTCostUSD; //e.Estimate.NPTCostUSD;
                                    eb.TecopCostUSD = bisCal.TECOPCostUSD; //e.Estimate.TECOPCostUSD;
                                    eb.MeanCostUSD = bisCal.MeanCostEDMUSD; //e.Estimate.MeanUSD;


                                    eb.TroubleFreeCost = bisCal.TroubleFree.Cost; //e.Estimate.NewTroubleFree.Cost;
                                    eb.NptCost = bisCal.NPT.Cost; //e.Estimate.NewNPTTime.Cost;
                                    eb.TecopCost = bisCal.TECOP.Cost;//e.Estimate.NewTECOPTime.Cost;
                                    eb.MeanCost = bisCal.MeanCostEDM;//e.Estimate.NewMean.Cost;
                                    #region WVA

                                    eb.MaturityLevel = e.Estimate.MaturityLevel;
                                    ////calc TQ BIC
                                    var _TQGapTotalDays = Math.Round(Math.Round(e.Estimate.NewMean.Days, 2) - e.Estimate.TQValueDriver, 2);
                                    var _TQGapDry = Math.Round(e.Estimate.DryHoleDays, 2) - e.Estimate.TQValueDriver;
                                    var _BICGapDry = Math.Round(e.Estimate.DryHoleDays, 2) - e.Estimate.BICValueDriver;
                                    var _TQGapCost = Math.Round(e.Estimate.NewMean.Cost, 2) - (e.Estimate.TQValueDriver * 1000000);
                                    var _BICGapCost = Math.Round(e.Estimate.NewMean.Cost, 2) - (e.Estimate.BICValueDriver * 1000000);
                                    var _BICGapTotalDays = Math.Round(Math.Round(e.Estimate.NewMean.Days, 2) - e.Estimate.BICValueDriver, 2);
                                    ////Wells Value Driver Total Days
                                    var TQGapCostCalc = Math.Round(Tools.Div(_TQGapTotalDays, rate) * Math.Round(e.Estimate.SpreadRateTotal, 2), 2);
                                    var TQTotalCost = Math.Round(Tools.Div(Math.Round(e.Estimate.NewMean.Cost, 2), rate) - TQGapCostCalc, 2);
                                    var BICGapCostCalc = Math.Round(Tools.Div(_BICGapTotalDays, rate) * Math.Round(e.Estimate.SpreadRateTotal, 2), 2);
                                    var BICTotalCost = Math.Round(Tools.Div(Math.Round(e.Estimate.NewMean.Cost, 2), rate) - BICGapCostCalc, 2);
                                    var TQGapDry = Math.Round(Tools.Div(_TQGapDry, rate) * Math.Round(e.Estimate.SpreadRateTotal, 2), 2);
                                    var TQGapCost = e.Estimate.NewMean.Cost - (e.Estimate.TQValueDriver * 1000000);
                                    var BICGapDry = Math.Round(Tools.Div(_BICGapDry, rate) * Math.Round(e.Estimate.SpreadRateTotal, 2), 2);
                                    var WellValueDriver = e.Estimate.WellValueDriver == null || e.Estimate.WellValueDriver == "" ? "" : e.Estimate.WellValueDriver;
                                    eb.WellValueDriver = WellValueDriver;
                                    eb.WVATotalDays = new WVA();
                                    eb.WVATotalCost = new WVA();
                                    eb.WVADryHoleDays = new WVA();
                                    if (WellValueDriver.Equals("Total Days"))
                                    {
                                        eb.MeanWVA = new WellDrillData()
                                        {
                                            Days = Math.Round(e.Estimate.NewMean.Days, 2)
                                        };
                                        eb.WVATotalDays = new WVA()
                                        {
                                            TQValueDriver = new WellDrillData()
                                            {
                                                Days = Math.Round(e.Estimate.TQValueDriver, 2),
                                                Cost = TQTotalCost
                                            },
                                            TQGap = new WellDrillData()
                                            {
                                                Days = Math.Round(_TQGapTotalDays, 2),
                                                Cost = TQGapCostCalc
                                            },
                                            BICValueDriver = new WellDrillData()
                                            {
                                                Days = e.Estimate.BICValueDriver,
                                                Cost = BICTotalCost
                                            },
                                            BICGap = new WellDrillData()
                                            {
                                                Days = _BICGapTotalDays,
                                                Cost = BICGapCostCalc
                                            }

                                        };
                                    }
                                    else if (WellValueDriver.Equals("Cost"))
                                    {
                                        eb.MeanWVA = new WellDrillData()
                                        {
                                            Cost = e.Estimate.NewMean.Cost
                                        };
                                        eb.WVATotalCost = new WVA()
                                        {
                                            TQValueDriver = new WellDrillData()
                                            {
                                                Cost = e.Estimate.TQValueDriver
                                            },
                                            TQGap = new WellDrillData()
                                            {
                                                Cost = _TQGapCost
                                            },
                                            BICValueDriver = new WellDrillData()
                                            {
                                                Cost = e.Estimate.BICValueDriver
                                            },
                                            BICGap = new WellDrillData()
                                            {
                                                Cost = _BICGapCost
                                            }
                                        };
                                    }
                                    else if (WellValueDriver.Equals("Dry Hole Days"))
                                    {
                                        eb.DryHoleDays = e.Estimate.DryHoleDays;
                                        eb.WVADryHoleDays = new WVA()
                                        {
                                            TQValueDriver = new WellDrillData()
                                            {
                                                Days = Math.Round(e.Estimate.TQValueDriver, 2)
                                            },
                                            TQGap = new WellDrillData()
                                            {
                                                Days = _TQGapDry
                                            },
                                            BICValueDriver = new WellDrillData()
                                            {
                                                Days = _BICGapDry
                                            },
                                            BICGap = new WellDrillData()
                                            {
                                                Cost = BICGapDry
                                            }

                                        };
                                    }
                                    #endregion
                                    ////Wells Value Driver Not Selected                                    
                                    eb.MeanCostEDM = bisCal.MeanCostEDM;// p.Allocation == null ? 0.0 : Math.Round(p.Allocation.AnnualyBuckets.Sum(x => x.MeanCostEDM), 2); // Mean Cost EDM
                                    eb.MeanCostRealTerm = bisCal.MeanCostRealTerm;// p.Allocation == null ? 0.0 : Math.Round(p.Allocation.AnnualyBuckets.Sum(x => x.MeanCostRealTerm), 2);
                                    //eb.MeanCostMOD = bisCal.MeanCostMOD;
                                    eb.MeanCostMOD = bisCal.MeanCostMOD;// p.Allocation == null ? 0.0 : Math.Round(p.Allocation.AnnualyBuckets.Sum(x => x.MeanCostMOD), 2);
                                    eb.ShellShareCost = bisCal.ShellShareCost;// p.Allocation == null ? 0.0 : Math.Round(p.Allocation.AnnualyBuckets.Sum(x => x.ShellShare), 2);
                                    //eb.ShellShareCost = bisCal.ShellShareCost;
                                    ////Perfom
                                    eb.PerformanceScore = e.Estimate.PerformanceScore;
                                    eb.WaterDepth = e.Estimate.WaterDepth;
                                    eb.WellDepth = e.Estimate.WellDepth;
                                    eb.NumberOfCasings = e.Estimate.NumberOfCasings;
                                    eb.MechanicalRiskIndex = e.Estimate.MechanicalRiskIndex;

                                    //add new 
                                    eb.EscalationRig = e.Allocation == null ? 0 : e.Allocation.AnnualyBuckets.Sum(x => x.EscCostRig);
                                    eb.EscalationService = e.Allocation == null ? 0 : e.Allocation.AnnualyBuckets.Sum(x => x.EscCostServices);
                                    eb.EscalationMaterials = e.Allocation == null ? 0 : e.Allocation.AnnualyBuckets.Sum(x => x.EscCostMaterial);
                                    eb.EscalationCost = e.Allocation == null ? 0 : e.Allocation.AnnualyBuckets.Sum(x => x.EscCostTotal);
                                    eb.CSOCost = e.Allocation == null ? 0 : e.Allocation.AnnualyBuckets.Sum(x => x.CSOCost);
                                    eb.InflationCost = e.Allocation == null ? 0 : e.Allocation.AnnualyBuckets.Sum(x => x.InflationCost);
                                    edetail.Add(eb);
                                }
                            }
                        }

                        return allcheckList;
                    }).ToList();

                    return d;
                }).Where(x => x.Phases.Count() > 0 && x.Phases != null).ToList();
            timeOut.Stop();
            var respTime = timeOut.Elapsed.TotalSeconds;
            return edetail;
        }

        //public DateTime WellActivity.GetLastDateUploadedLS()
        //{
        //    var latest1 = DataHelper.Get("LogLatestLS", null, SortBy.Descending("Executed_At"));

        //    DateTime dd = latest1.GetDateTime("Executed_At");

        //    return dd;
        //}

        public bool CheckPhaseInLatestLS(string wellname, string activitytyp, string rigname, DateTime dd)
        {

            if (dd != Tools.DefaultDate)
            {
                var logLast = DataHelper.Get("LogLatestLS", Query.And(
               Query.EQ("Well_Name", wellname),
               Query.EQ("Activity_Type", activitytyp),
               Query.EQ("Rig_Name", rigname),
               Query.EQ("Executed_At", Tools.ToUTC(dd))

               ));

                if (logLast != null)
                    return true;
                else
                    return false;
            }
            else
            {
                var logLast = DataHelper.Get("LogLatestLS", Query.And(
               Query.EQ("Well_Name", wellname),
               Query.EQ("Activity_Type", activitytyp),
               Query.EQ("Rig_Name", rigname)
               )); 

                if (logLast != null)
                    return true;
                else
                    return false;
            }



        }


        public List<BizPlanActivity> GetActivitiesForPalantir(FilterGeneratePlanningReportMap pq, string email = "", SortByBuilder sort = null)
        {

            PareseFilter();
            List<IMongoQuery> queries = new List<IMongoQuery>();
            List<IMongoQuery> queriesWoLE = new List<IMongoQuery>();
            var y = GetWellActivitiesQuery(email, "WellName", "Phases.ActivityType");
            queries.Add(y);
            queriesWoLE.Add(y);

            var queryBizPlan = ParseQueryForBizPlan();
            if (queryBizPlan != null)
            {
                queries.Add(queryBizPlan);
            }

            var query = Query.And(queries);

            var query2 = Query.And(queriesWoLE);

            //var now = Tools.ToUTC(DateTime.Now);
            //var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
            var lastDateExecutedLS = WellActivity.GetLastExecuteDateLS();
            var logLast = DataHelper.Populate("LogLatestLS", Query.And(Query.EQ("Executed_At", Tools.ToUTC(lastDateExecutedLS))));
            var wellActivities = BizPlanActivity.GetAll2(query, sort: sort, isMergeWithWellPlan: false, queryWoLe: query2)
                .Select(d =>
                {
                    d.Phases = d.Phases.Where(e =>
                    {
                        var isPeriodFiltered = FilterPeriod(GetPeriodForBizPlan(e));
                        var isActivityFiltered = true;
                        var isFilcalFiltered = true;
                        var isActivityCategoryFiltered = true;
                        var isGreaterThanThisMonth = true; //d.Phases.Where(x => x.LESchedule.Finish >= earlyThisMonth).Count() > 0;
                        var activityCategories = ActivityMaster.Populate<ActivityMaster>();

                        var islastesLs = false;

                        #region no need this because already filtered by query (in header)
                        //var isInLOB = true;
                        //if (pq.LineOfBusiness != null && pq.LineOfBusiness.Count() > 0)
                        //{
                        //    if (!pq.LineOfBusiness.Contains(d.LineOfBusiness))
                        //    {
                        //        isInLOB = false;
                        //    }
                        //}

                        //var isInAsset = true;
                        //if (pq.Asset != null && pq.Asset.Count() > 0)
                        //{
                        //    if (!pq.Asset.Contains(d.AssetName))
                        //    {
                        //        isInAsset = false;
                        //    }
                        //}
                        #endregion

                        var isCOmpleteOfModified = false;
                        if (e.Estimate.Status != null)
                        {
                            if (e.Estimate.Status.Trim().ToLower() == "complete" || e.Estimate.Status.Trim().ToLower() == "modified" || e.Estimate.Status.Trim().ToLower() == "draft" || e.Estimate.isFirstInitiate == false)
                            {
                                isCOmpleteOfModified = true;
                            }
                        }

                        var isInFundingType = true;
                        //if (pq.FundingType != null && pq.FundingType.Count() > 0)
                        //{
                        //    if (!pq.FundingType.Contains(e.FundingType))
                        //    {
                        //        isInFundingType = false;
                        //    }
                        //}
                        if (ExType != null && ExType.Count() > 0)
                        {
                            if (!ExType.Contains(e.FundingType))
                            {
                                isInFundingType = false;
                            }
                        }

                        //var dataAda = CheckPhaseInLatestLS(d.WellName, e.ActivityType, d.RigName, lastDateExecutedLS);
                        var dataAda = logLast.Any(x =>
                            x.GetString("Well_Name") == d.WellName &&
                            x.GetString("Activity_Type") == e.ActivityType &&
                            x.GetString("Rig_Name") == d.RigName
                            );
                        bool isReallyCheckLatestLS = false;
                        if (inlastuploadlsBoth != "" || inlastuploadlsBoth != null)
                        {
                            if (inlastuploadls == true)
                            {
                                if (dataAda)
                                    isReallyCheckLatestLS = true;
                            }
                            else if (inlastuploadls == false)
                            {
                                if (dataAda == false)
                                {
                                    var checkAnyLS = logLast.Any(x =>
                                        x.GetString("Well_Name") != d.WellName &&
                                        x.GetString("Activity_Type") != e.ActivityType &&
                                        x.GetString("Rig_Name") != d.RigName
                                   );
                                    if (checkAnyLS || !logLast.Any())
                                        isReallyCheckLatestLS = true;
                                }
                            }
                        }
                        //if (inlastuploadls == dataAda)
                        //{
                        //    islastesLs = true;
                        //}

                        if (Activities != null && Activities.Count > 0)
                            isActivityFiltered = Activities.Contains(e.ActivityType.Trim());
                        else
                        {
                            if (ActivitiesCategory != null && ActivitiesCategory.Count > 0)
                            {
                                var getActCat = activityCategories.Where(x => x._id.ToString().Trim().Equals(e.ActivityType.Trim())).FirstOrDefault();
                                if (getActCat != null) isActivityCategoryFiltered = ActivitiesCategory.Contains(getActCat.ActivityCategory);
                                else isActivityCategoryFiltered = false;
                            }
                        }

                        var isMatchedOPs = true;
                        if (OPs != null && OPs.Count() > 0)
                        {
                            var strOP = e.Estimate.SaveToOP == null ? "" : e.Estimate.SaveToOP;

                            if (OPs.Contains(strOP))
                                isMatchedOPs = true;
                            else
                                isMatchedOPs = false;
                        }




                        if (FiscalYearFinish != 0)
                        {
                            var phasePeriod = GetPeriodForBizPlan(e);
                            var isPhasePeriodInFiscalStart = phasePeriod.Start.Year >= FiscalYearStart && phasePeriod.Start.Year <= FiscalYearFinish;
                            var isPhasePeriodInFiscalFinish = phasePeriod.Finish.Year >= FiscalYearStart && phasePeriod.Finish.Year <= FiscalYearFinish;
                            if (isPhasePeriodInFiscalStart || isPhasePeriodInFiscalFinish)
                            {
                                isFilcalFiltered = true;
                            }
                            else
                            {
                                isFilcalFiltered = false;
                            }
                        }

                        if (inlastuploadlsBoth == "" || inlastuploadlsBoth == null)
                            return isMatchedOPs && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isGreaterThanThisMonth && isActivityCategoryFiltered && isCOmpleteOfModified && isInFundingType;
                        else
                            return isReallyCheckLatestLS && isMatchedOPs && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isGreaterThanThisMonth && isActivityCategoryFiltered && isCOmpleteOfModified && isInFundingType;

                    }).ToList();

                    return d;
                })
                .Where(d => d.Phases != null && d.Phases.Count > 0)
                .OrderBy(d => d.WellName)
                .ToList();

            return wellActivities;
        }


        public List<BizPlanActivity> GetActivitiesForBizPlan(string email = "", SortByBuilder sort = null, bool ignoreLSCheck = false)
        {

            PareseFilter();
            List<IMongoQuery> queries = new List<IMongoQuery>();
            List<IMongoQuery> queriesWoLE = new List<IMongoQuery>();
            var y = GetWellActivitiesQuery(email, "WellName", "Phases.ActivityType");
            queries.Add(y);
            queriesWoLE.Add(y);

            var queryBizPlan = ParseQueryForBizPlan();
            if (queryBizPlan != null)
            {
                queries.Add(queryBizPlan);
            }

            var query = Query.And(queries);

            var query2 = Query.And(queriesWoLE);

            var now = Tools.ToUTC(DateTime.Now);
            var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
            var lastDateExecutedLS = WellActivity.GetLastExecuteDateLS();

            var wellActivities = BizPlanActivity.GetAll(query, sort: sort, isMergeWithWellPlan: true, queryWoLe: query2)
                .Select(d =>
                {
                    d.Phases = d.Phases.Where(e =>
                    {
                        var isPeriodFiltered = FilterPeriod(GetPeriodForBizPlan(e));
                        var isActivityFiltered = true;
                        var isFilcalFiltered = true;
                        var isActivityCategoryFiltered = true;
                        var isGreaterThanThisMonth = true; //d.Phases.Where(x => x.LESchedule.Finish >= earlyThisMonth).Count() > 0;
                        var activityCategories = ActivityMaster.Populate<ActivityMaster>();

                        var islastesLs = false;

                        if (ignoreLSCheck)
                        {
                            islastesLs = true;
                        }
                        else
                        {
                            var dataAda = CheckPhaseInLatestLS(d.WellName, e.ActivityType, d.RigName, lastDateExecutedLS);

                            if (inlastuploadls == dataAda)
                            {
                                islastesLs = true;
                            }
                        }


                        if (Activities != null && Activities.Count > 0)
                            isActivityFiltered = Activities.Contains(e.ActivityType.Trim());
                        else
                        {
                            if (ActivitiesCategory != null && ActivitiesCategory.Count > 0)
                            {
                                var getActCat = activityCategories.Where(x => x._id.ToString().Trim().Equals(e.ActivityType.Trim())).FirstOrDefault();
                                if (getActCat != null) isActivityCategoryFiltered = ActivitiesCategory.Contains(getActCat.ActivityCategory);
                                else isActivityCategoryFiltered = false;
                            }
                        }

                        var isMatchedOPs = true;

                        //if (OPs != null && OPs.Count() > 0)
                        //{
                        //    //isMatchedOPs = isMatchOP(e.BaseOP);
                        //    isMatchedOPs = FunctionHelper.CompareBaseOP(OPs.ToArray(), e.BaseOP.ToArray(), opRelation);
                        //}




                        if (FiscalYearFinish != 0)
                        {
                            var phasePeriod = GetPeriodForBizPlan(e);
                            var isPhasePeriodInFiscalStart = phasePeriod.Start.Year >= FiscalYearStart && phasePeriod.Start.Year <= FiscalYearFinish;
                            var isPhasePeriodInFiscalFinish = phasePeriod.Finish.Year >= FiscalYearStart && phasePeriod.Finish.Year <= FiscalYearFinish;
                            if (isPhasePeriodInFiscalStart || isPhasePeriodInFiscalFinish)
                            {
                                isFilcalFiltered = true;
                            }
                            else
                            {
                                isFilcalFiltered = false;
                            }
                        }

                        if (inlastuploadls == false)
                            return isMatchedOPs && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isGreaterThanThisMonth && isActivityCategoryFiltered;
                        else
                            return islastesLs && isMatchedOPs && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isGreaterThanThisMonth && isActivityCategoryFiltered;

                    }).ToList();

                    return d;
                })
                .Where(d => d.Phases != null && d.Phases.Count > 0)
                .OrderBy(d => d.WellName)
                .ToList();

            return wellActivities;
        }

        public List<BizPlanActivity> GetActivitiesForBizPlanServerPage(int take = 0, int skip = 0, string email = "", SortByBuilder sort = null)
        {

            PareseFilter();
            List<IMongoQuery> queries = new List<IMongoQuery>();
            List<IMongoQuery> queriesWoLE = new List<IMongoQuery>();
            var y = GetWellActivitiesQuery(email, "WellName", "Phases.ActivityType");
            queries.Add(y);
            queriesWoLE.Add(y);

            var queryBizPlan = ParseQueryForBizPlan();
            if (queryBizPlan != null)
            {
                queries.Add(queryBizPlan);
            }

            var query = Query.And(queries);

            var query2 = Query.And(queriesWoLE);

            var now = Tools.ToUTC(DateTime.Now);
            var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
            var lastDateExecutedLS = WellActivity.GetLastExecuteDateLS();
            var wellActivities = BizPlanActivity.GetAll(query, take: take, skip: skip, sort: sort, isMergeWithWellPlan: true, queryWoLe: query2)
                .Select(d =>
                {
                    d.Phases = d.Phases.Where(e =>
                    {
                        var isPeriodFiltered = FilterPeriod(GetPeriodForBizPlan(e));
                        var isActivityFiltered = true;
                        var isFilcalFiltered = true;
                        var isActivityCategoryFiltered = true;
                        var isGreaterThanThisMonth = true; //d.Phases.Where(x => x.LESchedule.Finish >= earlyThisMonth).Count() > 0;
                        var activityCategories = ActivityMaster.Populate<ActivityMaster>();

                        var islastesLs = false;


                        var dataAda = CheckPhaseInLatestLS(d.WellName, e.ActivityType, d.RigName, lastDateExecutedLS);

                        if (inlastuploadls == dataAda)
                        {
                            islastesLs = true;
                        }

                        if (Activities != null && Activities.Count > 0)
                            isActivityFiltered = Activities.Contains(e.ActivityType.Trim());
                        else
                        {
                            if (ActivitiesCategory != null && ActivitiesCategory.Count > 0)
                            {
                                var getActCat = activityCategories.Where(x => x._id.ToString().Trim().Equals(e.ActivityType.Trim())).FirstOrDefault();
                                if (getActCat != null) isActivityCategoryFiltered = ActivitiesCategory.Contains(getActCat.ActivityCategory);
                                else isActivityCategoryFiltered = false;
                            }
                        }

                        var isMatchedOPs = true;

                        //if (OPs != null && OPs.Count() > 0)
                        //{
                        //    //isMatchedOPs = isMatchOP(e.BaseOP);
                        //    isMatchedOPs = FunctionHelper.CompareBaseOP(OPs.ToArray(), e.BaseOP.ToArray(), opRelation);
                        //}

                        if (FiscalYearFinish != 0)
                        {
                            var phasePeriod = GetPeriodForBizPlan(e);
                            var isPhasePeriodInFiscalStart = phasePeriod.Start.Year >= FiscalYearStart && phasePeriod.Start.Year <= FiscalYearFinish;
                            var isPhasePeriodInFiscalFinish = phasePeriod.Finish.Year >= FiscalYearStart && phasePeriod.Finish.Year <= FiscalYearFinish;
                            if (isPhasePeriodInFiscalStart || isPhasePeriodInFiscalFinish)
                            {
                                isFilcalFiltered = true;
                            }
                            else
                            {
                                isFilcalFiltered = false;
                            }
                        }
                        if (inlastuploadls == false)
                            return isMatchedOPs && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isGreaterThanThisMonth && isActivityCategoryFiltered;
                        else
                            return islastesLs && isMatchedOPs && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isGreaterThanThisMonth && isActivityCategoryFiltered;
                    }).ToList();

                    return d;
                })
                .Where(d => d.Phases != null && d.Phases.Count > 0)
                .OrderBy(d => d.WellName)
                .ToList();

            return wellActivities;
        }

        public List<FilterHelper> GetActivitiesForBizPlanWithServerPage(int take = 0, int skip = 0, string email = "", SortByBuilder sort = null)
        {
            try
            {
                var now = Tools.ToUTC(DateTime.Now);
                var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
                var lastDateExecutedLS = WellActivity.GetLastExecuteDateLS();

                var dts = this.GetActivitiesForBizPlanServerPagingFullQuery();
                List<FilterHelper> datasx = new List<FilterHelper>();
                foreach (var u in dts)
                {
                    datasx.Add(BsonHelper.Deserialize<FilterHelper>(u));

                }

                //var newdts = DataHelper.ToDictionaryArray(this.GetActivitiesForBizPlanServerPagingFullQuery());

                //int i = 0;
                //List<FilterHelper> details = new List<FilterHelper>();
                List<FilterHelper> fetchData = new List<FilterHelper>();
                //foreach (var t in datasx)
                //{
                //    i++;
                //    var det = new FilterHelper();
                //    det._id = t.ToBsonDocument().GetInt32("_id");                        
                //    det.Status = t.ToBsonDocument().GetString("Status");     
                //    det.Region = t.ToBsonDocument().GetString("Region");               
                //    det.OperatingUnit = t.ToBsonDocument().GetString("OperatingUnit");     
                //    det.UARigSequenceId = t.ToBsonDocument().GetString("UARigSequenceId");     
                //    det.UARigDescription  = t.ToBsonDocument().GetString("UARigDescription");    
                //    det.RigType = t.ToBsonDocument().GetString("RigType");              
                //    det.RigName = t.ToBsonDocument().GetString("RigName");                
                //    det.ProjectName  = t.ToBsonDocument().GetString("ProjectName");           
                //    det.WellName   = t.ToBsonDocument().GetString("WellName");          
                //    det.ActivityType    = t.ToBsonDocument().GetString("ActivityType");          
                //    det.SaveToOP   = t.ToBsonDocument().GetString("SaveToOP");

                //    var ops = t.ToBsonDocument().Get("OPHistories._v").AsBsonArray.ToList();
                //    foreach(var his in ops)
                //    {
                //        var ophis_List = new OPHistory();
                //        ophis_List.Type = his.ToBsonDocument().GetString("_v.Type");
                //        ophis_List.ActivityType = his.ToBsonDocument().GetString("_v.ActivityType");
                //        ophis_List.LESchedule.Start = his.ToBsonDocument().GetDateTime("_v.LESchedule.Start");
                //        ophis_List.LESchedule.Finish = his.ToBsonDocument().GetDateTime("_v.LESchedule.Finish");
                //        det.OPHistories.Add(ophis_List); 

                //    }

                //    det.PhSchedule = new DateRange(){
                //        Start = t.ToBsonDocument().GetDateTime("PhSchedule._v.Start"),
                //        Finish = t.ToBsonDocument().GetDateTime("PhSchedule._v.Finish")
                //    };

                //    det.AFE = new WellDrillData()
                //    {
                //        Days = t.ToBsonDocument().GetDouble("AFE._v.Days"),
                //        Cost = t.ToBsonDocument().GetDouble("AFE._v.Cost")
                //    };

                //    det.PlanSchedule  = new DateRange(){
                //        Start = t.ToBsonDocument().GetDateTime("PlanSchedule._v.Start"),
                //        Finish= t.ToBsonDocument().GetDateTime("PlanSchedule._v.Finish")
                //    };

                //    //det.AFESchedule = new DateRange(){
                //    //    Start = t.ToBsonDocument()["AFESchedule"] == BsonNull.Value ? Tools.DefaultDate : BsonHelper.GetDateTime(t.ToBsonDocument(), "AFESchedule._v.Start").ToUniversalTime(),
                //    //    Finish = t.ToBsonDocument()["AFESchedule"] == BsonNull.Value ? Tools.DefaultDate : BsonHelper.GetDateTime(t.ToBsonDocument(),"AFESchedule._v.Start").ToUniversalTime()
                //    //};
                //    det.LESchedule = new DateRange(){
                //        Start = t.ToBsonDocument().GetDateTime("LESchedule._v.Start"),
                //        Finish= t.ToBsonDocument().GetDateTime("LESchedule._v.Finish")
                //    };
                //    //det.LE = new WellDrillData()
                //    //{
                //    //    Days = t.ToBsonDocument().GetDouble("LE._v.Days"),
                //    //    Cost = t.ToBsonDocument().GetDouble("LE._v.Cost")
                //    //};
                //    det.VirtualPhase =  t.ToBsonDocument().GetBool("VirtualPhase");
                //    det.ShiftFutureEventDate =  t.ToBsonDocument().GetBool("ShiftFutureEventDate");
                //    det.ShellShare = t.ToBsonDocument().GetDouble("ShellShare");
                //    details.Add(det);
                //}
                foreach (var d in datasx)
                {
                    var isPeriodFiltered = FilterPeriod(GetPeriodForBizPlanFullQuery(d));
                    var isActivityFiltered = true;
                    var isFilcalFiltered = true;
                    var isActivityCategoryFiltered = true;
                    var isGreaterThanThisMonth = true;
                    var activityCategories = ActivityMaster.Populate<ActivityMaster>();

                    var islastesLs = false;


                    var dataAda = CheckPhaseInLatestLS(d.WellName, d.ActivityType, d.RigName, lastDateExecutedLS);

                    if (inlastuploadls == dataAda)
                    {
                        islastesLs = true;
                    }

                    if (Activities != null && Activities.Count > 0)
                        isActivityFiltered = Activities.Contains(d.ActivityType.Trim());
                    else
                    {
                        if (ActivitiesCategory != null && ActivitiesCategory.Count > 0)
                        {
                            var getActCat = activityCategories.Where(x => x._id.ToString().Trim().Equals(d.ActivityType.Trim())).FirstOrDefault();
                            if (getActCat != null) isActivityCategoryFiltered = ActivitiesCategory.Contains(getActCat.ActivityCategory);
                            else isActivityCategoryFiltered = false;
                        }
                    }

                    var isMatchedOPs = true;

                    if (FiscalYearFinish != 0)
                    {
                        var phasePeriod = GetPeriodForBizPlanFullQuery(d);
                        var isPhasePeriodInFiscalStart = phasePeriod.Start.Year >= FiscalYearStart && phasePeriod.Start.Year <= FiscalYearFinish;
                        var isPhasePeriodInFiscalFinish = phasePeriod.Finish.Year >= FiscalYearStart && phasePeriod.Finish.Year <= FiscalYearFinish;
                        if (isPhasePeriodInFiscalStart || isPhasePeriodInFiscalFinish)
                        {
                            isFilcalFiltered = true;
                        }
                        else
                        {
                            isFilcalFiltered = false;
                        }
                    }

                    if (inlastuploadls == false)
                    {
                        if (isMatchedOPs && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isGreaterThanThisMonth && isActivityCategoryFiltered)
                        {
                            fetchData.Add(d);
                        }

                    }
                    else
                    {
                        if (islastesLs && isMatchedOPs && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isGreaterThanThisMonth && isActivityCategoryFiltered)
                            fetchData.Add(d);
                    }
                }
                return fetchData;
            }
            catch (InvalidCastException e)
            {
                var aa = e;
                throw;
            }

        }

        public string buildStringIn(string initial, List<string> str)
        {
            // 'WellName' : { '$in' : ['GREAT WHITE GA022']   }
            string first = "'" + initial + "'";
            string middle = " : { '$in' : [";

            List<string> strs = new List<string>();
            if (str != null)
            {
                foreach (var t in str)
                    strs.Add("'" + t + "'"); ;
                string inside = String.Join(",", strs.ToList());
                string outer = "]   }";
                return first + middle + inside + outer;
            }
            else
            {
                return null;
            }

        }

        public List<BsonDocument> GetActivitiesForBizPlanServerPagingFullQuery()
        {
            List<string> allStrInFilter = new List<string>();

            string mRegion = buildStringIn("Region", this.Regions);
            if (!string.IsNullOrEmpty(mRegion))
                allStrInFilter.Add(mRegion);
            string mOperatingUnit = buildStringIn("OperatingUnit", this.OperatingUnits);
            if (!string.IsNullOrEmpty(mRegion))
                allStrInFilter.Add(mOperatingUnit);
            string mRigType = buildStringIn("RigType", this.RigTypes);
            if (!string.IsNullOrEmpty(mRigType))
                allStrInFilter.Add(mRigType);
            string mRigName = buildStringIn("RigName", this.RigNames);
            if (!string.IsNullOrEmpty(mRigName))
                allStrInFilter.Add(mRigName);
            string mProjectName = buildStringIn("ProjectName", this.ProjectNames);
            if (!string.IsNullOrEmpty(mProjectName))
                allStrInFilter.Add(mProjectName);
            string mWellName = buildStringIn("WellName", this.WellNames);
            if (!string.IsNullOrEmpty(mWellName))
                allStrInFilter.Add(mWellName);
            string mFundingType = buildStringIn("Phases.FundingType", this.ExType);
            if (!string.IsNullOrEmpty(mFundingType))
                allStrInFilter.Add(mFundingType);

            string mLob = buildStringIn("LineOfBusiness", this.LineOfBusiness);
            if (!string.IsNullOrEmpty(mLob))
                allStrInFilter.Add(mLob);

            var getAct = ActivityMaster.Populate<ActivityMaster>(Query.In("ActivityCategory", new BsonArray(this.ActivitiesCategory)));
            if (getAct.Any())
            {
                string mActivities = buildStringIn("Phases.ActivityType", getAct.Select(d => d._id.ToString()).ToList());
                if (!string.IsNullOrEmpty(mActivities))
                    allStrInFilter.Add(mActivities);
            }
            if (this.OPs != null)
            {
                string mOPs = "";
                if (this.OPs.Count == 1)
                {
                    mOPs = "'Phases.Estimate.SaveToOP' : { '$eq' : '" + this.OPs.FirstOrDefault() + "'}";
                }
                else
                {
                    mOPs = buildStringIn("Phases.Estimate.SaveToOP", this.OPs);
                }
                if (!string.IsNullOrEmpty(mOPs))
                    allStrInFilter.Add(mOPs);
            }

            var mPeriod = ParsePeriodFullQuery();
            if (mPeriod != null)
                allStrInFilter.Add(mPeriod);
            string quryMatches = "";
            if (allStrInFilter != null && allStrInFilter.Count() > 0)
            {
                quryMatches = string.Join(",", allStrInFilter);
            }

            string aggUW = @"{$unwind:'$Phases'}";
            string aggMatch = "";

            if (!string.IsNullOrEmpty(quryMatches))
                aggMatch = @"{ $match:{ " + quryMatches + " }  }";

            string aggProject = @"{$project: {
                                        '_id'               : '$_id',
                                        'Status'            : '$Phases.Estimate.Status',
                                        'Region'            : '$Region',
                                        'OperatingUnit'     : '$OperatingUnit',
                                        'UARigSequenceId'   : '$UARigSequenceId',
                                        'UARigDescription'  : '$UARigDescription',
                                        'RigType'           : '$RigType',
                                        'RigName'           : '$Phases.Estimate.RigName',
                                        'ProjectName'       : '$ProjectName',
	                                    'WellName'          : '$WellName',
                                        'ActivityType'      : '$Phases.ActivityType',
	                                    'SaveToOP'          : '$SaveToOP',
	                                    'OPHistories'       : '$OPHistories',
                                        'PhSchedule'        : '$Phases.PhSchedule',
                                        'PlanSchedule'      : '$Phases.PlanSchedule',
                                        'AFESchedule'       : '$Phases.AFESchedule',
                                        'AFE'               : '$Phases.AFE',
                                        'LESchedule'        : '$Phases.LESchedule',
                                        'LE'                : '$Phases.LE',
                                        'VirtualPhase'      : '$VirtualPhase',
                                        'ShiftFutureEventDate' : '$ShiftFutureEventDate',
                                        'ShellShare'           : '$ShellShare',
                                        'BaseOP'            : '$Phases.BaseOP'
                                        }   
                              }";
            string aggGroup = @"{ $group: { 
            
                            '_id' : '1',
	                        'total' : {'$sum' : 1} 
                            }}";

            List<BsonDocument> pipelines = new List<BsonDocument>();
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggUW));
            if (!string.IsNullOrEmpty(aggMatch))
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggMatch));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggProject));
            //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggGroup));
            //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(limit));
            List<BsonDocument> aggregate = DataHelper.Aggregate(new BizPlanActivity().TableName, pipelines);
            //List<object> ret = new List<object>();
            //ret.Add(DataHelper.ToDictionaryArray(aggregate));
            return aggregate;
        }

        public static List<String> GetWellActivities(string email, string[] roleIds = null)
        {
            List<string> rets = new List<string>();
            //var email = LoginUser.Email;
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("PersonInfos.Email", email));
            if (roleIds != null) qs.Add(Query.In("PersonInfos.RoleId", new BsonArray(roleIds)));
            var q = Query.And(qs);
            List<WEISPerson> wps = WEISPerson.Populate<WEISPerson>(q)
                .Select(d => new WEISPerson { ActivityType = d.ActivityType, WellName = d.WellName })
                .ToList();
            //List<string> wellNames = WellInfo.Populate<WellInfo>().GroupBy(d => d._id.ToString()).Select(d => d.Key).ToList();
            //List<string> activityTypes = WellActivity.Populate<WellActivity>().GroupBy(d => d._id.ToString())
            //    .Select(d => d.Key).ToList();
            foreach (var wp in wps)
            {
                rets.Add(String.Format("{0}|{1}", wp.WellName, wp.ActivityType));
            }
            return rets;
        }

        public static IMongoQuery GetWellActivitiesQuery(string email, string WellNameField, string ActivityTypeField, string[] roleIds = null)
        {
            List<string> was = GetWellActivities(email, roleIds);
            List<IMongoQuery> qs = new List<IMongoQuery>();
            IMongoQuery q = Query.Null;
            foreach (var wa in was)
            {
                string[] waParts = wa.Split(new char[] { '|' });
                string wellName = waParts[0];
                string activityType = waParts[1];
                List<IMongoQuery> q1 = new List<IMongoQuery>();
                if (WellNameField != "")
                {
                    if (wellName.Equals("*") == false)
                        q1.Add(Query.EQ(WellNameField, wellName));
                    else
                        q1.Add(Query.GT(WellNameField, "0"));
                }
                if (ActivityTypeField != "")
                {
                    if (activityType.Equals("*") == false)
                        q1.Add(Query.EQ(ActivityTypeField, activityType));
                    else
                        q1.Add(Query.GT(ActivityTypeField, "0"));
                }
                qs.Add(Query.And(q1));
            }
            if (qs.Count > 0) q = Query.Or(qs);
            return q;
        }


        public double CalculatePipCumulative(string what, PIPAllocation allocation)
        {
            if (CumulativeDataType.Equals("Total"))
            {
                if (what.Equals("Days"))
                    return -(allocation.DaysPlanImprovement + allocation.DaysPlanRisk);
                else if (what.Equals("LEDays"))
                    return -(allocation.LEDays == 0 ? allocation.DaysPlanImprovement + allocation.DaysPlanRisk : allocation.LEDays);
                else if (what.Equals("LECost"))
                    return -(allocation.LECost == 0 ? allocation.CostPlanImprovement + allocation.CostPlanRisk : allocation.LECost);
                else
                    return -(allocation.CostPlanImprovement + allocation.CostPlanRisk);
            }
            else if (CumulativeDataType.Equals("Improvement"))
            {
                if (what.Equals("Days"))
                    return -allocation.DaysPlanImprovement;
                else if (what.Equals("LEDays"))
                    return -(allocation.LEDays == 0 ? allocation.DaysPlanImprovement + allocation.DaysPlanRisk : allocation.LEDays);
                else if (what.Equals("LECost"))
                    return -(allocation.LECost == 0 ? allocation.CostPlanImprovement + allocation.CostPlanRisk : allocation.LECost);
                else
                    return -allocation.CostPlanImprovement;
            }
            else
            {
                if (what.Equals("Days"))
                    return allocation.DaysPlanRisk;
                else if (what.Equals("LEDays"))
                    return -(allocation.LEDays == 0 ? allocation.DaysPlanImprovement + allocation.DaysPlanRisk : allocation.LEDays);
                else if (what.Equals("LECost"))
                    return -(allocation.LECost == 0 ? allocation.CostPlanImprovement + allocation.CostPlanRisk : allocation.LECost);
                else
                    return allocation.CostPlanRisk;
            }
        }

        #region webtools
        public static bool CheckAccessibleWellAndActivity(List<String> AccessibleWellActivities, string WellName, string ActivityType)
        {
            if (AccessibleWellActivities != null && AccessibleWellActivities.Count > 0)
            {
                foreach (var x in AccessibleWellActivities)
                {
                    string[] waParts = x.Split(new char[] { '|' });
                    string wellNameRole = waParts[0];
                    string activityTypeRole = waParts[1];
                    if (wellNameRole.Equals("*"))
                    {
                        if (activityTypeRole.Equals("*"))
                        {
                            return true;
                        }
                        else
                        {
                            if (activityTypeRole.ToLower().Equals(ActivityType.ToLower()))
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        if (wellNameRole.ToLower().Equals(WellName.ToLower()))
                        {
                            if (activityTypeRole.Equals("*"))
                            {
                                return true;
                            }
                            else
                            {
                                if (activityTypeRole.ToLower().Equals(ActivityType.ToLower()))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
        #endregion
        #region Rangga
        public IMongoQuery GetQueryWaterbaseFall()
        {
            var buidQuery = ParseQuery();
            return buidQuery;
        }
        #endregion
    }

    public class ActivitiesAndPIPs
    {
        public double TotalOP { set; get; }
        public double TotalLE { set; get; }
        public double TotalTQ { set; get; }
        public double TotalBIC { set; get; }
        public double TotalTarget { set; get; }
        public List<WellActivity> Activities { set; get; }
        public List<WellPIP> PIPs { set; get; }
    }

    public class ActivitiesAndPIPsMLE
    {
        public double TotalOP { set; get; }
        public double TotalLE { set; get; }
        public double TotalTQ { set; get; }
        public List<WellActivityUpdateMonthly> Activities { set; get; }
        public List<PIPElement> PIPs { set; get; }
    }

    public class DashbordResultByYear
    {
        public int Year { get; set; }
        public string WellName { get; set; }
        public string RigName { get; set; }
        public string ActivityType { get; set; }
        public DateRange OPSchedule { get; set; }
        public WellDrillData OP { get; set; }
        public DateRange LSSchedule { get; set; }
        public WellDrillData LS { get; set; }
        public DateRange LESchedule { get; set; }
        public WellDrillData LE { get; set; }
        public DateRange AFESchedule { get; set; }
        public WellDrillData AFE { get; set; }
        public DashbordResultByYear()
        {
            OPSchedule = new DateRange();
            LESchedule = new DateRange();
            LSSchedule = new DateRange();
            AFESchedule = new DateRange();

            OP = new WellDrillData();
            LE = new WellDrillData();
            LS = new WellDrillData();
            AFE = new WellDrillData();
        }
    }

}