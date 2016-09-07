using ECIS.Client.WEIS.Output;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECIS.Client.WEIS
{


    public class OPEstimates
    {
        public string OPEstimate { get; set; }
        public DateTime EstimatePeriodStart { get; set; }
        public DateTime EstimatePeriodEnd { get; set; }
        public double EstimateDays { get; set; }
        public double EstimateCost { get; set; }
    }

    public class FilterHelper
    {
        public int _id { get; set; }
        public string Status { get; set; }
        public string Region { get; set; }
        public string OperatingUnit { get; set; }
        public string UARigSequenceId { get; set; }
        public string UARigDescription { get; set; }
        public string RigType { get; set; }
        public string RigName { get; set; }
        public string ProjectName { get; set; }
        public string WellName { get; set; }
        public string ActivityType { get; set; }
        public string SaveToOP { get; set; }
        public List<OPHistory> OPHistories { get; set; }
        public DateRange PhSchedule { get; set; }
        public DateRange PsSchedule { get; set; }
        public DateRange PlanSchedule { get; set; }
        public DateRange AFESchedule { get; set; }
        public WellDrillData AFE { get; set; }
        public DateRange LESchedule { get; set; }
        public WellDrillData LE { get; set; }
        public WellDrillData Plan { get; set; }
        public WellDrillData OP { get; set; }
        public bool VirtualPhase { get; set; }
        public bool isLatestLS { get; set; }
        public bool ShiftFutureEventDate { get; set; }
        public double ShellShare { get; set; }
        public List<string> BaseOP { get; set; }

        public int PhaseNo { get; set; }
        public string AssetName { get; set; }
        public double WorkingInterest { get; set; }
        public string FirmOrOption { get; set; }
        public string RiskFlag { get; set; }
        public BizPlanActivityEstimate Estimate { get; set; }
        //public List<BizPlanAllocation> Allocations { get; set; }

        public string ReferenceFactorModel { get; set; }
        public string Country { get; set; }
        public string FundingType { get; set; }
        public bool isInPlan { get; set; }

        public List<OPEstimates> OPEstimates { get; set; }

        public OPEstimates PrevOPEst { get; set; }
        public OPEstimates NextOPEst { get; set; }

        public FilterHelper()
        {
            OPEstimates = new List<WEIS.OPEstimates>();
            PrevOPEst = new WEIS.OPEstimates();
            NextOPEst = new WEIS.OPEstimates();

            OPHistories = new List<OPHistory>();
            Estimate = new BizPlanActivityEstimate();
            PhSchedule = new DateRange();
            PsSchedule = new DateRange();
            PlanSchedule = new DateRange();
            AFESchedule = new DateRange();
            AFE = new WellDrillData();
            LESchedule = new DateRange();
            LE = new WellDrillData();
            Plan = new WellDrillData();
            OP = new WellDrillData();
            BaseOP = new List<string>();

        }

        public string buildSort(List<Dictionary<string, string>> sorts = null)
        {
            var sort = sorts.FirstOrDefault();
            var skey = sort.FirstOrDefault().Value;
            var sby = sort.LastOrDefault().Value;
            var ascdesc = sby.Equals("asc") ? "1" : "-1";
            switch (skey)
            {
                case "OpsStart":
                    {
                        return @"{$sort:{'PhSchedule.Start':" + ascdesc + "}}";
                    }
                default:
                    {
                        return @"{$sort:{'" + skey + "': " + ascdesc + " }}";
                    }
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

        private string GetPeriodBaseField(WaterfallBase wb)
        {
            if (wb.GetWhatData != null)
                if (wb.GetWhatData.Equals("OP"))
                    return "PlanSchedule";

            if ("By Last Sequence".ToLower().Equals(wb.PeriodBase.ToLower()) || "PhSchedule".Equals(wb.PeriodBase))
                return "PhSchedule";

            if ("By Last Estimate".ToLower().Equals(wb.PeriodBase.ToLower()) || "LESchedule".Equals(wb.PeriodBase))
                return "LESchedule";


            return "PhSchedule";
        }

        private string ParsePeriodFullQuery(WaterfallBase wb)
        {
            var DateRelation = (wb.DateRelation == null ? "AND" : wb.DateRelation);
            DateRelation = "$" + DateRelation.ToLower();
            var DateStart = (wb.DateStart == null ? "" : wb.DateStart);
            var DateFinish = (wb.DateFinish == null ? "" : wb.DateFinish);
            var DateStart2 = (wb.DateStart2 == null ? "" : wb.DateStart2);
            var DateFinish2 = (wb.DateFinish2 == null ? "" : wb.DateFinish2);

            var queries = new List<IMongoQuery>();
            string format = "yyyy-MM-dd";
            CultureInfo culture = CultureInfo.InvariantCulture;
            var period = GetPeriodBaseField(wb);

            var periodStart = Tools.ToUTC(DateStart != "" ? DateTime.ParseExact(DateStart, format, culture) : new DateTime(1900, 1, 1));
            var periodFinish = Tools.ToUTC(DateFinish != "" ? DateTime.ParseExact(DateFinish, format, culture) : new DateTime(3000, 1, 1));
            var periodStart2 = Tools.ToUTC(DateStart2 != "" ? DateTime.ParseExact(DateStart2, format, culture) : new DateTime(1900, 1, 1));
            var periodFinish2 = Tools.ToUTC(DateFinish2 != "" ? DateTime.ParseExact(DateFinish2, format, culture) : new DateTime(3000, 1, 1));

            var PeriodStartField = "Phases." + period + ".Start";
            var PeriodFinishField = "Phases." + period + ".Finish";
            if (wb.GetWhatData == "OP")
            {
                PeriodStartField = "PsSchedule.Start";
                PeriodFinishField = "PsSchedule.Finish";
            }
            string result = "";

            if (wb.PeriodView != null && wb.PeriodView.ToLower().Contains("fiscal"))
            {
                result = "";
                result = "" +
                   "'$and':[" +
                       "{ '" + PeriodStartField + "' : { '$gte': ISODate('" + periodStart.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000')}}," +
                       "{ '" + PeriodFinishField + "': { '$lte': ISODate('" + periodFinish.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000')}}" +
                   "]";

                return result;
            }

            if (DateRelation.Equals("$and"))
            {
                result = "" +
          "'" + DateRelation + "':[" +
              "{ '" + PeriodStartField + "' : { '$gte': ISODate('" + periodStart.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000'), '$lte': ISODate('" + periodFinish.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000')}}," +
              "{ '" + PeriodFinishField + "': { '$gte': ISODate('" + periodStart.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000'), '$lte': ISODate('" + periodFinish.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000')}}" +
          "]";
            }

            //}

            return result;
        }

        //public DateTime GetLastDateUploadedLS()
        //{
        //    var latest1 = DataHelper.Get("LogLatestLS", null, SortBy.Descending("Executed_At"));

        //    DateTime dd = latest1.GetDateTime("Executed_At");

        //    return dd;
        //}

        public bool FilterPeriod(WaterfallBase wb, DateRange period)
        {
            if (wb.PeriodView != null && wb.PeriodView.ToLower().Contains("fiscal"))
            {
                var DateStart = (wb.DateStart == null ? "" : wb.DateStart);
                var DateFinish = (wb.DateFinish == null ? "" : wb.DateFinish);

                string format = "yyyy-MM-dd";
                CultureInfo culture = CultureInfo.InvariantCulture;

                var periodStart = Tools.ToUTC(DateStart != "" ? DateTime.ParseExact(DateStart, format, culture) : new DateTime(1900, 1, 1));
                var periodFinish = Tools.ToUTC(DateFinish != "" ? DateTime.ParseExact(DateFinish, format, culture) : new DateTime(3000, 1, 1));

                var isPhaseStartFiltered = (period.Start >= periodStart) && (period.Start <= periodFinish);
                var isPhaseFinishFiltered = (period.Finish >= periodStart) && (period.Finish <= periodFinish);

                return isPhaseStartFiltered || isPhaseFinishFiltered;
            }

            return wb.DateBetween(period);
        }

        private DateRange GetPeriodForBizPlanFullQuery(WaterfallBase wb, FilterHelper f)
        {
            var pStart = f.ToBsonDocument().Get(GetPeriodBaseField(wb)).AsBsonDocument.GetDateTime("Start");
            var pFinish = f.ToBsonDocument().Get(GetPeriodBaseField(wb)).AsBsonDocument.GetDateTime("Finish");
            return new DateRange()
            {
                Start = pStart,
                Finish = pFinish
            };
        }


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

        public static List<String> GetWellActivities(string email, string[] roleIds = null)
        {
            List<string> rets = new List<string>();
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("PersonInfos.Email", email));
            if (roleIds != null) qs.Add(Query.In("PersonInfos.RoleId", new BsonArray(roleIds)));
            var q = Query.And(qs);
            List<WEISPerson> wps = WEISPerson.Populate<WEISPerson>(q)
                .Select(d => new WEISPerson { ActivityType = d.ActivityType, WellName = d.WellName })
                .ToList();
            List<string> wellNames = WellInfo.Populate<WellInfo>().GroupBy(d => d._id.ToString()).Select(d => d.Key).ToList();
            List<string> activityTypes = WellActivity.Populate<WellActivity>().GroupBy(d => d._id.ToString())
                .Select(d => d.Key).ToList();
            foreach (var wp in wps)
            {
                rets.Add(String.Format("{0}|{1}", wp.WellName, wp.ActivityType));
            }
            return rets;
        }

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


        public string GetSecurityQuery(string email, WaterfallBase wb)
        {
            var loginsecurity = GetWellActivities(email);
            string joiins = "";
            if (!loginsecurity.Contains("*|*"))
            {
                string result = "";
                foreach (var t in loginsecurity)
                {
                    var y = t.Split('|');
                    #region build

                    switch (y.Count())
                    {
                        case 0:
                            {
                                break;
                            }
                        case 1:
                            {
                                if (!y[0].Trim().Equals(""))
                                {
                                    if (y[0].Trim().Equals("*"))
                                    {
                                        // all well
                                        break;
                                    }
                                    else
                                    {
                                        result = result + @"{ 'WellName' : '" + y[0].Trim() + "'},";
                                        // ada well
                                    }
                                }
                                break;
                            }

                        case 2:
                            {
                                if (!y[0].Trim().Equals(""))
                                {
                                    if (y[0].Trim().Equals("*"))
                                    {
                                        // all well
                                        break;
                                    }
                                    else
                                    {
                                        if (y[1].Trim().Equals("*"))
                                        {
                                            result = result + @"{ 'WellName' : '" + y[0].Trim() + "'},";
                                        }
                                        else
                                        {
                                            result = result + @"{ 'WellName' : '" + y[0].Trim() + "', 'Phases.ActivityType' : '" + y[1].Trim() + "'},";
                                        }
                                        // ada well
                                    }
                                }
                                break;
                            }
                        default:
                            {
                                break;
                            }

                    }
                    if (result != "")
                    {
                        joiins = joiins + joiins;
                    }

                    #endregion
                }
                if (result != "")
                {
                    #region Append Or with Or
                    var queries = new List<IMongoQuery>();
                    string format = "yyyy-MM-dd";
                    CultureInfo culture = CultureInfo.InvariantCulture;
                    var period = GetPeriodBaseField(wb);

                    var DateRelation = (wb.DateRelation == null ? "AND" : wb.DateRelation);
                    DateRelation = "$" + DateRelation.ToLower();
                    var DateStart = (wb.DateStart == null ? "" : wb.DateStart);
                    var DateFinish = (wb.DateFinish == null ? "" : wb.DateFinish);
                    var DateStart2 = (wb.DateStart2 == null ? "" : wb.DateStart2);
                    var DateFinish2 = (wb.DateFinish2 == null ? "" : wb.DateFinish2);

                    var periodStart = Tools.ToUTC(DateStart != "" ? DateTime.ParseExact(DateStart, format, culture) : new DateTime(1900, 1, 1));
                    var periodFinish = Tools.ToUTC(DateFinish != "" ? DateTime.ParseExact(DateFinish, format, culture) : new DateTime(3000, 1, 1));
                    var periodStart2 = Tools.ToUTC(DateStart2 != "" ? DateTime.ParseExact(DateStart2, format, culture) : new DateTime(1900, 1, 1));
                    var periodFinish2 = Tools.ToUTC(DateFinish2 != "" ? DateTime.ParseExact(DateFinish2, format, culture) : new DateTime(3000, 1, 1));

                    var PeriodStartField = "Phases." + period + ".Start";
                    var PeriodFinishField = "Phases." + period + ".Finish";
                    if (wb.GetWhatData == "OP")
                    {
                        PeriodStartField = "PsSchedule.Start";
                        PeriodFinishField = "PsSchedule.Finish";
                    }

                    #endregion
                    string vx = "";
                    if (wb.DateRelation.Equals("OR") && !wb.PeriodView.ToLower().Contains("fiscal"))
                    {
                        vx =
                         "{ '" + PeriodStartField + "' : { '$gte': ISODate('" + periodStart.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000'), '$lte': ISODate('" + periodFinish.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000')}}," +
                         "{ '" + PeriodFinishField + "': { '$gte': ISODate('" + periodStart.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000'), '$lte': ISODate('" + periodFinish.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000')}},";
                        result = "'$or':[" + result + vx + "]";

                    }
                    else
                    {
                        result = "'$or':[" + result + "]";

                    }

                }
                return result;
            }
            else
            {
                // super admin
                #region Append Or with Or
                var queries = new List<IMongoQuery>();
                string format = "yyyy-MM-dd";
                CultureInfo culture = CultureInfo.InvariantCulture;
                var period = GetPeriodBaseField(wb);

                var DateRelation = (wb.DateRelation == null ? "AND" : wb.DateRelation);
                DateRelation = "$" + DateRelation.ToLower();
                var DateStart = (wb.DateStart == null ? "" : wb.DateStart);
                var DateFinish = (wb.DateFinish == null ? "" : wb.DateFinish);
                var DateStart2 = (wb.DateStart2 == null ? "" : wb.DateStart2);
                var DateFinish2 = (wb.DateFinish2 == null ? "" : wb.DateFinish2);

                var periodStart = Tools.ToUTC(DateStart != "" ? DateTime.ParseExact(DateStart, format, culture) : new DateTime(1900, 1, 1));
                var periodFinish = Tools.ToUTC(DateFinish != "" ? DateTime.ParseExact(DateFinish, format, culture) : new DateTime(3000, 1, 1));
                var periodStart2 = Tools.ToUTC(DateStart2 != "" ? DateTime.ParseExact(DateStart2, format, culture) : new DateTime(1900, 1, 1));
                var periodFinish2 = Tools.ToUTC(DateFinish2 != "" ? DateTime.ParseExact(DateFinish2, format, culture) : new DateTime(3000, 1, 1));

                var PeriodStartField = "Phases." + period + ".Start";
                var PeriodFinishField = "Phases." + period + ".Finish";
                if (wb.GetWhatData == "OP")
                {
                    PeriodStartField = "PsSchedule.Start";
                    PeriodFinishField = "PsSchedule.Finish";
                }

                #endregion

                string result = "";
                string vx = "";
                if (wb.DateRelation.Equals("OR") && !wb.PeriodView.ToLower().Contains("fiscal"))
                {
                    vx =
                     "{ '" + PeriodStartField + "' : { '$gte': ISODate('" + periodStart.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000'), '$lte': ISODate('" + periodFinish.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000')}}," +
                     "{ '" + PeriodFinishField + "': { '$gte': ISODate('" + periodStart2.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000'), '$lte': ISODate('" + periodFinish2.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000')}},";
                    result = "'$or':[" + result + vx + "]";
                }
                return result;

            }
        }

        public List<BsonDocument> GetActivitiesForBizPlanServerPagingFullQuery(WaterfallBase wb, int skip, int limit, string userloginemail, out int countdata, List<Dictionary<string, string>> serversorts = null)
        {
            List<string> allStrInFilter = new List<string>();

            string security = GetSecurityQuery(userloginemail, wb);
            if (!security.Trim().Equals(""))
                allStrInFilter.Add(security);

            var mPeriod = ParsePeriodFullQuery(wb);
            if (mPeriod != null && !mPeriod.Trim().Equals(""))
                allStrInFilter.Add(mPeriod);



            if (wb.inlastuploadls == true)
            {
                string middle = "'Phases.isLatestLS' : true";
                allStrInFilter.Add(middle);
            }
            else
            {
                //string middle = "'Phases.isLatestLS' : false";
                //allStrInFilter.Add(middle);
            }

            string mRegion = buildStringIn("Region", wb.Regions);
            if (!string.IsNullOrEmpty(mRegion))
                allStrInFilter.Add(mRegion);
            string mOperatingUnit = buildStringIn("OperatingUnit", wb.OperatingUnits);
            if (!string.IsNullOrEmpty(mOperatingUnit))
                allStrInFilter.Add(mOperatingUnit);
            string mRigType = buildStringIn("RigType", wb.RigTypes);
            if (!string.IsNullOrEmpty(mRigType))
                allStrInFilter.Add(mRigType);
            string mRigName = buildStringIn("RigName", wb.RigNames);
            if (!string.IsNullOrEmpty(mRigName))
                allStrInFilter.Add(mRigName);
            string mProjectName = buildStringIn("ProjectName", wb.ProjectNames);
            if (!string.IsNullOrEmpty(mProjectName))
                allStrInFilter.Add(mProjectName);
            string mWellName = buildStringIn("WellName", wb.WellNames);
            if (!string.IsNullOrEmpty(mWellName))
                allStrInFilter.Add(mWellName);
            string mFundingType = buildStringIn("Phases.FundingType", wb.ExType);
            if (!string.IsNullOrEmpty(mFundingType))
                allStrInFilter.Add(mFundingType);

            string mStatus = buildStringIn("Phases.Estimate.Status", wb.Status);
            if (!string.IsNullOrEmpty(mStatus))
                allStrInFilter.Add(mStatus);
            else
            {
                string mStatusFix = buildStringIn("Phases.Estimate.Status", new List<string> { "Complete", "Modified", "Draft","Meta Data Missing" });
                allStrInFilter.Add(mStatusFix);
            }

            string mLob = buildStringIn("LineOfBusiness", wb.LineOfBusiness);
            if (wb.LineOfBusiness.FirstOrDefault() !=null && wb.LineOfBusiness.FirstOrDefault().Count()>0)
                allStrInFilter.Add(mLob);

            string mActv = buildStringIn("Phases.ActivityType", wb.Activities);
            if (!string.IsNullOrEmpty(mActv))
                allStrInFilter.Add(mActv);


            string mActvCat = buildStringIn("Phases.ActivityCategory", wb.ActivitiesCategory);
            if (!string.IsNullOrEmpty(mActvCat))
                allStrInFilter.Add(mActvCat);

            if (wb.OPs != null)
            {
                string mOPs = "";
                if (wb.OPs.Count == 1)
                {
                    mOPs = "'Phases.Estimate.SaveToOP' : { '$eq' : '" + wb.OPs.FirstOrDefault() + "'}";
                }
                else
                {
                    mOPs = buildStringIn("Phases.Estimate.SaveToOP", wb.OPs);
                }
                if (!string.IsNullOrEmpty(mOPs))
                    allStrInFilter.Add(mOPs);
            }

            string quryMatches = "";
            if (allStrInFilter != null && allStrInFilter.Count() > 0)
            {
                quryMatches = string.Join(",", allStrInFilter);
            }

            string aggUW = @"{'$unwind':'$Phases'}";
            string aggMatch = "";

            if (!string.IsNullOrEmpty(quryMatches))
                aggMatch = @"{ '$match':{ " + quryMatches + " }  }";

            #region Project
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
	                                    'SaveToOP'          : '$Phases.Estimate.SaveToOP',
	                                    'OPHistories'       : '$OPHistories',
                                        'PhSchedule'        : '$Phases.PhSchedule',
                                        'PlanSchedule'      : '$Phases.PlanSchedule',
                                        'AFESchedule'       : '$Phases.AFESchedule',
                                        'AFE'               : '$Phases.AFE',
                                        'Plan'               : '$Phases.Plan',
                                        'LESchedule'        : '$Phases.LESchedule',
                                        'LE'                : '$Phases.LE',
                                        'VirtualPhase'      : '$VirtualPhase',
                                        'ShiftFutureEventDate' : '$ShiftFutureEventDate',
                                        'ShellShare'           : '$ShellShare',
                                        'BaseOP'            : '$Phases.BaseOP',
                                        'Estimate'            : '$Phases.Estimate',
                                        'Country'            : '$Country',
                                        'AssetName'           : '$AssetName',
                                        'ReferenceFactorModel'            : '$ReferenceFactorModel',
                                        'FundingType'       : '$Phases.FundingType',
                                        'isInPlan'          : '$isInPlan',
                                        'PhaseNo'          : '$Phases.PhaseNo',
                                        'isLatestLS'          : '$Phases.isLatestLS',

                                        }   
                              }";
            string aggGroup = @"{ $group: { 
            
                            '_id' : '1',
	                        'total' : {'$sum' : 1} 
                            }}";

            string sorts = @"{$sort:{'WellName':" + 1 + "}}";
            if (serversorts != null)
                sorts = buildSort(serversorts);
            string aggskip = "{ $skip: " + skip + " }";
            string aggLimit = "{ $limit: " + limit + " }";
            #endregion

            List<BsonDocument> pipelines = new List<BsonDocument>();
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggUW));
            if (!string.IsNullOrEmpty(aggMatch))
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggMatch));

            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggProject));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(sorts));
            //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggskip));
            //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggLimit));
            List<BsonDocument> aggregate = DataHelper.Aggregate(new BizPlanActivity().TableName, pipelines);


            #region Data Count
            pipelines = new List<BsonDocument>();
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggUW));
            if (!string.IsNullOrEmpty(aggMatch))
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggMatch));

            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggProject));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggGroup));
            List<BsonDocument> datacount = DataHelper.Aggregate(new BizPlanActivity().TableName, pipelines);
            #endregion
            countdata = datacount.Count() > 0 ? datacount.FirstOrDefault().GetInt32("total") : 0;

            // rebind OP Histories

            var grp = aggregate.GroupBy(x => x.GetString("UARigSequenceId"));
            var wrseq = DataHelper.Populate<WellActivity>("WEISWellActivities", q: Query.In("UARigSequenceId", new BsonArray(grp.ToList())), fields: new List<string> { "_id", "WellName", "UARigSequenceId", "OPHistories", "Phases" });
            foreach (var datagrpbyseqid in grp)
            {
                foreach (var ops in datagrpbyseqid.ToList())
                {
                    // looping phases
                    if (wrseq.Count() <= 0)
                        break;
                    var y = wrseq.FirstOrDefault(x =>
                        x.WellName.Equals(ops.GetString("WellName")) &&
                        x.UARigSequenceId.Equals(datagrpbyseqid.Key)
                        );
                    if (wrseq != null && wrseq.Count() > 0)
                    {
                        BsonArray barr = new BsonArray();
                        foreach (var arr in y.OPHistories)
                            barr.Add(arr.ToBsonDocument());
                        ops.Set("OPHistories", barr);
                        var activity = ops.GetString("ActivityType");

                        if (y.Phases.FirstOrDefault(x => x.ActivityType.Equals(activity)) != null)
                        {
                            var listy = y.Phases.FirstOrDefault(x => x.ActivityType.Equals(ops.GetString("ActivityType"))).BaseOP;

                            BsonArray bsopx = new BsonArray();
                            foreach (var arr in listy)
                                bsopx.Add(arr);

                            ops.Set("BaseOP", bsopx);
                        }


                    }
                }
            }
            return aggregate.ToList();
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
                    start = DateTime.ParseExact(t.GetString("Start_Date"), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (Exception ec)
                {
                    start = Tools.DefaultDate;
                }
                try
                {
                    finish = DateTime.ParseExact(t.GetString("End_Date"), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
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

        public List<FilterHelper> FilterDatahaveWeekly(List<FilterHelper> data)
        {
            List<string> allStrInFilter = new List<string>();

            string mRegion = buildStringIn("WellName", data.Select(x => x.WellName).ToList());
            if (!string.IsNullOrEmpty(mRegion))
                allStrInFilter.Add(mRegion);
            string mWellName = buildStringIn("SequenceId", data.Select(x => x.UARigSequenceId).ToList());
            if (!string.IsNullOrEmpty(mWellName))
                allStrInFilter.Add(mWellName);
            string mFundingType = buildStringIn("Phase.ActivityType", data.Select(x => x.ActivityType).ToList());
            if (!string.IsNullOrEmpty(mFundingType))
                allStrInFilter.Add(mFundingType);

            string quryMatches = "";
            if (allStrInFilter != null && allStrInFilter.Count() > 0)
            {
                quryMatches = string.Join(",", allStrInFilter);
            }

            var aggMatch = "";

            if (!string.IsNullOrEmpty(quryMatches))
                aggMatch = @"{ $match:{ " + quryMatches + " }  }";

            string aggGroup = @"{ $group:
                        {
                          '_id' : {
	                        'WellName' : '$WellName',
		                        'SequenceId' : '$SequenceId',
		                        'ActivityType' : '$Phase.ActivityType',
                          }
                        }
                    }";
            List<BsonDocument> pipelines = new List<BsonDocument>();
            if (!string.IsNullOrEmpty(aggMatch))
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggMatch));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggGroup));
            List<BsonDocument> aggregate = DataHelper.Aggregate(new WellActivityUpdate().TableName, pipelines);
            List<CheckerHelper> docs = new List<CheckerHelper>();
            foreach (var a in aggregate)
            {
                docs.Add(new CheckerHelper
                {
                    ActivityType = a.GetString("_id.ActivityType"),
                    WellName = a.GetString("_id.WellName"),
                    SequenceId = a.GetString("_id.SequenceId")
                });
            }
            if (aggregate.Count() > 0)
            {
                List<FilterHelper> result = new List<FilterHelper>();
                foreach (var t in data)
                {
                    var res = docs.Where(x => x.WellName.Equals(t.WellName) && x.ActivityType.Equals(t.ActivityType) && x.SequenceId.Equals(t.UARigSequenceId));
                    if (res.Count() <= 0)
                        result.Add(t);
                }
                return result;
            }
            else
            {
                return data;
            }

        }

        public List<FilterHelper> GetActivitiesForBizPlanWithServerPage(WaterfallBase wb, out int countdata, int take = 0, int skip = 0, string email = "", SortByBuilder sort = null, List<Dictionary<string, string>> serversorts = null)
        {
            try
            {
                var now = DateTime.Now;
                var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
                // tgl latest
                var lastDateExecutedLS = WellActivity.GetLastExecuteDateLS();
                var latestPhases = GetLatestLSPhases(lastDateExecutedLS);
                var logLast = DataHelper.Populate("LogLatestLS", Query.And(Query.EQ("Executed_At", Tools.ToUTC(lastDateExecutedLS))));
                var inside1 = DateTime.Now;
                int cx = 0;
                var dts = GetActivitiesForBizPlanServerPagingFullQuery(wb, skip, take, email, out cx, serversorts);

                List<FilterHelper> datasx = new List<FilterHelper>();
                foreach (var u in dts)
                    datasx.Add(BsonHelper.Deserialize<FilterHelper>(u));

                List<FilterHelper> fetchData = new List<FilterHelper>();
                //var activityCategories = ActivityMaster.Populate<ActivityMaster>();

                foreach (var d in datasx)
                {
                    var isPeriodFiltered = true;// FilterPeriod(wb, GetPeriodForBizPlanFullQuery(wb, d));
                    var isActivityFiltered = true;
                    var isFilcalFiltered = true;
                    var isActivityCategoryFiltered = true;
                    var isGreaterThanThisMonth = true;
                    var islastesLs = false;
                    var isCOmpleteOfModified = true; // false;


                    var isInFundingType = true;

                    var isMatchedOPs = true;
                    var dataAda = latestPhases.Where(x => x.Rig_Name.Equals(d.RigName) && x.Well_Name.Equals(d.WellName) && x.Activity_Type.Equals(d.ActivityType));
                    bool checkder = false;

                    //if (dataAda != null && dataAda.Count() > 0)
                    //    checkder = true;
                    //if (wb.inlastuploadls == checkder)
                    //    islastesLs = true;
                    bool isReallyCheckLatestLS = false;
                    if (wb.inlastuploadlsBoth != "" || wb.inlastuploadlsBoth != null)
                    {
                        if (wb.inlastuploadls == true)
                        {
                            if (dataAda != null && dataAda.Count() > 0)
                                isReallyCheckLatestLS = true;
                        }
                        else if (wb.inlastuploadls == false)
                        {
                            if (dataAda == null)
                            {
                                var checkAnyLS = logLast.Any(x =>
                                    x.GetString("Well_Name") != d.WellName &&
                                    x.GetString("Activity_Type") != d.ActivityType &&
                                    x.GetString("Rig_Name") != d.RigName
                               );
                                if (checkAnyLS || !logLast.Any())
                                    isReallyCheckLatestLS = true;
                            }
                        }
                    }

                    if (wb.inlastuploadlsBoth == "" || wb.inlastuploadlsBoth ==null)
                    {
                        if (isMatchedOPs && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isGreaterThanThisMonth && isActivityCategoryFiltered && isCOmpleteOfModified && isInFundingType)
                            fetchData.Add(d);
                    }
                    else
                    {
                        if (isReallyCheckLatestLS && isMatchedOPs && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isGreaterThanThisMonth && isActivityCategoryFiltered && isCOmpleteOfModified && isInFundingType)
                            fetchData.Add(d);
                    }
                }

                countdata = cx;
                return fetchData;
            }
            catch (InvalidCastException e)
            {
                var aa = e;
                throw;
            }

        }

        //data browser
        public string ParseQuery(string userloginemail, WaterfallBase wb)
        {
            List<string> allStrInFilter = new List<string>();

            string security = GetSecurityQuery(userloginemail, wb);
            if (!security.Trim().Equals(""))
                allStrInFilter.Add(security);

            string mRegion = buildStringIn("Region", wb.Regions);
            if (!string.IsNullOrEmpty(mRegion))
                allStrInFilter.Add(mRegion);
            string mOperatingUnit = buildStringIn("OperatingUnit", wb.OperatingUnits);
            if (!string.IsNullOrEmpty(mOperatingUnit))
                allStrInFilter.Add(mOperatingUnit);
            string mRigType = buildStringIn("RigType", wb.RigTypes);
            if (!string.IsNullOrEmpty(mRigType))
                allStrInFilter.Add(mRigType);
            string mRigName = buildStringIn("RigName", wb.RigNames);
            if (!string.IsNullOrEmpty(mRigName))
                allStrInFilter.Add(mRigName);
            string mProjectName = buildStringIn("ProjectName", wb.ProjectNames);
            if (!string.IsNullOrEmpty(mProjectName))
                allStrInFilter.Add(mProjectName);
            string mWellName = buildStringIn("WellName", wb.WellNames);
            if (!string.IsNullOrEmpty(mWellName))
                allStrInFilter.Add(mWellName);
            string mPerformanceUnit = buildStringIn("PerformanceUnit", wb.PerformanceUnits);
            if (!string.IsNullOrEmpty(mPerformanceUnit))
                allStrInFilter.Add(mPerformanceUnit);
            //ext type
            string mFundingType = buildStringIn("EXType", wb.ExType);
            if (!string.IsNullOrEmpty(mFundingType))
                allStrInFilter.Add(mFundingType);
            //Firm or Option
            string mFirmOption = "";
            List<string> fOption = new List<string>();
            fOption.Add(wb.firmoption);
            switch (wb.firmoption)
            {
                case null:
                    break;
                case "Firm":
                    mFirmOption = "{'FirmOrOption': {'$eq': 'Firm'}}";
                    break;
                case "Option":
                    mFirmOption = "{'FirmOrOption': {'$eq': 'Option'}}";
                    break;
                case "NotFirm":
                    mFirmOption = "{'FirmOrOption': {'$ne': 'Firm'}}";
                    break;
                case "NotOption":
                    mFirmOption = "{'FirmOrOption': {'$ne': 'Option'}}";
                    break;
                case "Both":
                    mFirmOption = "{'FirmOrOption': {'$in':  [Firm', 'Option']}}";
                    break;
                case "NotBoth":
                    mFirmOption = "{'FirmOrOption': {'$nin':  [Firm', 'Option']}}";
                    break;
                case "All":
                    break;
                default:
                    //queries.Add(Query.EQ("FirmOrOption", firmoption));
                    mFirmOption = "{'FirmOrOption': {'$eq':  " + fOption + "}}";
                    break;
            }
            if (!string.IsNullOrEmpty(mFirmOption))
                allStrInFilter.Add(mFirmOption);

            if (wb.OPs != null)
            {
                string mOPs = "";
                if (wb.OPs.Count == 1)
                {
                    mOPs = "'Phases.BaseOP' : { '$eq' : '" + wb.OPs.FirstOrDefault() + "'}";
                }
                else
                {
                    mOPs = buildStringIn("Phases.BaseOP", wb.OPs);
                }
                if (!string.IsNullOrEmpty(mOPs))
                    allStrInFilter.Add(mOPs);
            }

            var getAct = ActivityMaster.Populate<ActivityMaster>(Query.In("ActivityCategory", new BsonArray(wb.ActivitiesCategory)));
            if (getAct.Any())
            {
                string mActivities = buildStringIn("Phases.ActivityType", getAct.Select(d => d._id.ToString()).ToList());
                if (!string.IsNullOrEmpty(mActivities))
                    allStrInFilter.Add(mActivities);
            }

            //isInPlan WellActivity 
            string mIsInPlan = "";
            if (wb.isInPlan != null && wb.isInPlan.Count() > 0)
            {
                mIsInPlan = "'Phases.IsInPlan' : { '$eq' : '" + wb.isInPlan.FirstOrDefault() + "'}";
                allStrInFilter.Add(mIsInPlan);
            }

            var mPeriod = ParsePeriodFullQuery(wb);
            if (mPeriod != null)
                allStrInFilter.Add(mPeriod);


            string quryMatches = "";
            if (allStrInFilter != null && allStrInFilter.Count() > 0)
            {
                quryMatches = string.Join(",", allStrInFilter);
            }
            return quryMatches;
        }

        private bool isMatchOP(WaterfallBase wb, List<string> BasePhaseOP)
        {
            var isMatchBaseOP = true;
            var BaseOP = BasePhaseOP.ToArray();
            if (wb.opRelation.ToLower() == "and")
            {
                var match = true;
                foreach (var op in wb.OPs)
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
                foreach (var op in wb.OPs)
                {

                    match = Array.Exists(BaseOP, element => element.Equals(op));
                    if (match) break;
                }
            }
            return isMatchBaseOP;
        }

        private DateRange GetPeriodForFullQuery(WaterfallBase wb, BsonDocument f)
        {
            var pStart = f.ToBsonDocument().Get(GetPeriodBaseField(wb)).AsBsonDocument.GetDateTime("Start");
            var pFinish = f.ToBsonDocument().Get(GetPeriodBaseField(wb)).AsBsonDocument.GetDateTime("Finish");
            return new DateRange()
            {
                Start = pStart,
                Finish = pFinish
            };
        }

        public List<BsonDocument> GetActivitiesQuery(WaterfallBase wb, int skip, int take, string email ,out int countdata, List<Dictionary<string, string>> serversorts = null)
        {
            //string aggUW = @"{$unwind:'$Phases'}";
            string aggMatch = "";
            var quryMatches = ParseQuery(email, wb);
            if (!string.IsNullOrEmpty(quryMatches))
                aggMatch = @"{ $match:{ " + quryMatches + " }  }";
            string aggProject = @"{$project: {
                                        '_id'               : '$_id',
                                        'Region'            : '$Region',
                                        'OperatingUnit'     : '$OperatingUnit',
                                        'UARigSequenceId'   : '$UARigSequenceId',
                                        'UARigDescription'  : '$UARigDescription',
                                        'RigType'           : '$RigType',
                                        'RigName'           : '$RigName',
                                        'ProjectName'       : '$ProjectName',
	                                    'WellName'          : '$WellName',
                                        'AssetName'         : '$AssetName',
                                        'NonOP'             : '$NonOP',
                                        'WorkingInterest'   : '$WorkingInterest',
	                                    'OPHistories'       : '$OPHistories',
                                        'VirtualPhase'      : '$VirtualPhase',
                                        'ShiftFutureEventDate' : '$ShiftFutureEventDate',
                                        'OpsSchedule'       : '$OpsSchedule',
                                        'PsSchedule'        : '$PsSchedule',
                                        'LESchedule'        : '$LESchedule',
                                        'Phases'            : '$Phases'
                                  }   
                              }";
            string aggGroup = @"{ $group: { 
            
                            '_id' : '1',
	                        'total' : {'$sum' : 1} 
                            }}";
            string sorts = @"{$sort:{'WellName':" + 1 + "}}";
            if (serversorts != null)
                sorts = buildSort(serversorts);
            string aggskip = "{ $skip: " + skip + " }";
            string aggLimit = "{ $limit: " + take + " }";
            List<BsonDocument> pipelines = new List<BsonDocument>();
            //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggUW));
            if (!string.IsNullOrEmpty(aggMatch))
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggMatch));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggProject));
            //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggskip));
            //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggLimit));

            List<BsonDocument> aggregate = DataHelper.Aggregate(new WellActivity().TableName, pipelines);
            #region Data Count
            pipelines = new List<BsonDocument>();
            if (!string.IsNullOrEmpty(aggMatch))
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggMatch));

            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggProject));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggGroup));
            List<BsonDocument> datacount = DataHelper.Aggregate(new WellActivity().TableName, pipelines);
            #endregion
            countdata = datacount.Count() > 0 ? datacount.FirstOrDefault().GetInt32("total") : 0;
            return aggregate;
        }

        public List<DataBrowser> GetActivities(WaterfallBase wb, out int countdata, int take = 0, int skip = 0, string email = "", SortByBuilder sort = null, List<Dictionary<string, string>> serversorts = null)
        {
            var lastDateExecutedLS = WellActivity.GetLastExecuteDateLS();
            var latestPhases = GetLatestLSPhases(lastDateExecutedLS);
            var dts = GetActivitiesQuery(wb, skip, take, email, out countdata, serversorts);

            List<DataBrowser> datasx = new List<DataBrowser>();
            foreach (var u in dts)
                datasx.Add(BsonHelper.Deserialize<DataBrowser>(u));
            List<DataBrowser> fetchData = new List<DataBrowser>();
            var activityCategories = ActivityMaster.Populate<ActivityMaster>();
            foreach (var d in datasx)
            {
                if (d.Phases != null && d.Phases.Count() > 0)
                {
                    bool checkAllCondition = false;
                    foreach (var e in d.Phases)
                    {
                        var isNotRisk = true;
                        if (wb.GetWhatData != "OP")
                            isNotRisk = !e.ActivityType.ToLower().Contains("risk");
                        var isPeriodFiltered = FilterPeriod(wb, GetPeriodForFullQuery(wb, e.ToBsonDocument()));
                        var isActivityFiltered = true;
                        var isActivityCategoryFiltered = true;
                        var isFilcalFiltered = true;
                        var isLSValid = true;
                        var isMatchedOPs = true;
                        if (wb.OPs != null && wb.OPs.Count() > 0)
                        {
                            isMatchedOPs = isMatchOP(wb, e.BaseOP);

                        }

                        //for waterfall
                        var singleBaseOPFilter = true;
                        if (wb.BaseOP != null)
                        {
                            if (!e.BaseOP.Contains(wb.BaseOP)) singleBaseOPFilter = false;
                        }


                        if (wb.Activities != null && wb.Activities.Count > 0)
                            isActivityFiltered = wb.Activities.Contains(e.ActivityType.Trim());
                        else
                        {
                            if (wb.ActivitiesCategory != null && wb.ActivitiesCategory.Count > 0)
                            {
                                var getActCat = activityCategories.Where(x => x._id.ToString().Trim().Equals(e.ActivityType.Trim())).FirstOrDefault();
                                if (getActCat != null) isActivityCategoryFiltered = wb.ActivitiesCategory.Contains(getActCat.ActivityCategory);
                                else isActivityCategoryFiltered = false;
                            }
                        }

                        if (wb.FiscalYearFinish != 0)
                        {
                            var phasePeriod = GetPeriodForFullQuery(wb, e.ToBsonDocument());
                            var isPhasePeriodInFiscalStart = phasePeriod.Start.Year >= wb.FiscalYearStart && phasePeriod.Start.Year <= wb.FiscalYearFinish;
                            var isPhasePeriodInFiscalFinish = phasePeriod.Finish.Year >= wb.FiscalYearStart && phasePeriod.Finish.Year <= wb.FiscalYearFinish;
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
                        if (wb.ValidLSOnly)
                        {
                            var lsStart = e.PhSchedule.Start;
                            if (lsStart == Tools.DefaultDate) isLSValid = false;
                        }
                        var checkIsInPlan = false;
                        if (wb.isInPlan != null && wb.isInPlan.Count() > 0)
                        {
                            var isinp = wb.isInPlan.FirstOrDefault();
                            var phinp = e.IsInPlan;

                            if (isinp == phinp)
                                checkIsInPlan = true;
                        }
                        else
                        {
                            checkIsInPlan = true;
                        }
                        //check bizplan status here
                        var isMatchedStatus = true;
                        //if (wb.Status != null && wb.Status.Count > 0)
                        //{
                        //    try
                        //    {
                        //        var bizplandata = BizPlanDatas.Where(x => x.WellName == d.WellName && x.UARigSequenceId == d.UARigSequenceId && (x.Phases.Where(z => z.ActivityType == e.ActivityType).Count() > 0)).FirstOrDefault();
                        //        if (bizplandata != null)
                        //        {
                        //            var matchedPhase = bizplandata.Phases.Where(x => x.ActivityType == e.ActivityType).FirstOrDefault();
                        //            if (matchedPhase != null)
                        //            {
                        //                e.BizPlanStatus = matchedPhase.Estimate.Status;
                        //                isMatchedStatus = Status.Contains(e.BizPlanStatus);
                        //            }
                        //        }
                        //    }
                        //    catch
                        //    {
                        //    }
                        //    isMatchedStatus = Status.Contains(e.BizPlanStatus);
                        //}
                        //else
                        //{
                        //    isMatchedStatus = true;
                        //}

                        var islastesLs = false;
                        var dataAda = CheckPhaseInLatestLS(d.WellName, e.ActivityType, d.RigName, lastDateExecutedLS);

                        if (wb.inlastuploadls == dataAda)
                        {
                            islastesLs = true;
                        }

                        if (wb.inlastuploadls == false)
                        {
                            //return checkIsInPlan && isMatchedOPs && isNotRisk && singleBaseOPFilter && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isActivityCategoryFiltered && isLSValid && isMatchedStatus;
                            if (checkIsInPlan && isMatchedOPs && isNotRisk && singleBaseOPFilter && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isActivityCategoryFiltered && isLSValid && isMatchedStatus)
                                checkAllCondition = true;
                        }
                        else
                        {
                            if (islastesLs && checkIsInPlan && isMatchedOPs && isNotRisk && singleBaseOPFilter && isPeriodFiltered && isActivityFiltered && isFilcalFiltered && isActivityCategoryFiltered && isLSValid && isMatchedStatus)
                                checkAllCondition = true;
                        }
                    }
                    if (checkAllCondition)
                        fetchData.Add(d);
                }

            }
            var a = fetchData;
            return fetchData.Where(x => x.Phases != null && x.Phases.Count() > 0)
                .OrderBy(x => x.WellName).ToList();
        }
    }

    public class CheckerHelper
    {
        public string WellName { get; set; }
        public string SequenceId { get; set; }
        public string ActivityType { get; set; }
    }
}
