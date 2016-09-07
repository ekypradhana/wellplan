using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class PIPAnalysisController : Controller
    {
        //
        // GET: /Shell/PIPAnalysis/
        public ActionResult Index()
        {
            var lts = new PIPAnalysisController().GetLatestLsDate();//new pipana().GetLatestLsDate();
            ViewBag.LatestLS = lts;
            return View();
        }

        public JsonResult GetData(PIPAnalysisFilter filter)
        {
            return MvcResultInfo.Execute(() =>
            {
                var sc = DateIsland.Populate(new DateTime(2000, 1, 1), DateTime.Now);
                var datas = new List<BsonDocument>();
                var scatters = new List<PIPAnalysisScatter>();
                var res = PopulateData(filter, out datas, out scatters);

                var TopRealized = new List<PIPAnalysis>();
                var TopUnRealized = new List<PIPAnalysis>();
                var BottomRealized = new List<PIPAnalysis>();
                var BottomUnRealized = new List<PIPAnalysis>();
                var ValueMaxTopBottomRealized = 0.0;
                var ValueMinTopBottomRealized = 0.0;
                var ValueMaxTopBottomUnRealized = 0.0;
                var ValueMinTopBottomUnRealized = 0.0;
                var TotalRealized = 0.0;
                var TotalUnRealized = 0.0;

                #region Top Bottom Chart Data
                if (res.Any())
                {
                    TotalRealized = res.Where(x => x.isRealized).Sum(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay);
                    TotalUnRealized = res.Where(x => !x.isRealized).Sum(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay);

                    TopRealized = res.Where(x => x.isRealized).OrderBy(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay).Take(20).Select(x => new PIPAnalysis
                    {
                        Type = x.Type,
                        isRealized = x.isRealized,
                        Title = x.Title,
                        LECost = x.LECost * -1,
                        LEDays = x.LEDays * -1,
                        LECostPerDay = x.LECostPerDay * -1,
                        PlanCost = x.PlanCost * -1,
                        PlanDays = x.PlanDays * -1,
                        PlanCostPerDay = x.PlanCostPerDay * -1,
                    }).ToList();

                    if (TopRealized.Any())
                    {
                        ValueMaxTopBottomRealized = TopRealized.Max(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay);
                        ValueMinTopBottomRealized = TopRealized.Min(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay);
                    }

                    BottomRealized = res.Where(x => x.isRealized).OrderByDescending(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay).Take(20).Select(x => new PIPAnalysis
                    {
                        Type = x.Type,
                        isRealized = x.isRealized,
                        Title = x.Title,
                        LECost = x.LECost * -1,
                        LEDays = x.LEDays * -1,
                        LECostPerDay = x.LECostPerDay * -1,
                        PlanCost = x.PlanCost * -1,
                        PlanDays = x.PlanDays * -1,
                        PlanCostPerDay = x.PlanCostPerDay * -1,
                    }).ToList();

                    if (BottomRealized.Any())
                    {
                        if (ValueMaxTopBottomRealized < BottomRealized.Max(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay))
                            ValueMaxTopBottomRealized = BottomRealized.Max(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay);

                        if (ValueMinTopBottomRealized > BottomRealized.Min(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay))
                            ValueMinTopBottomRealized = BottomRealized.Min(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay);
                    }

                    TopUnRealized = res.Where(x => !x.isRealized).OrderBy(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay).Take(20).Select(x => new PIPAnalysis
                    {
                        Type = x.Type,
                        isRealized = x.isRealized,
                        Title = x.Title,
                        LECost = x.LECost * -1,
                        LEDays = x.LEDays * -1,
                        LECostPerDay = x.LECostPerDay * -1,
                        PlanCost = x.PlanCost * -1,
                        PlanDays = x.PlanDays * -1,
                        PlanCostPerDay = x.PlanCostPerDay * -1,
                    }).ToList();

                    if (TopRealized.Any())
                    {
                        if (ValueMaxTopBottomRealized < TopRealized.Max(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay))
                            ValueMaxTopBottomRealized = TopRealized.Max(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay);

                        if (ValueMinTopBottomRealized > TopRealized.Min(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay))
                            ValueMinTopBottomRealized = TopRealized.Min(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay);
                    }

                    if (TopUnRealized.Any())
                    {
                        if (ValueMaxTopBottomUnRealized < TopUnRealized.Max(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay))
                            ValueMaxTopBottomUnRealized = TopUnRealized.Max(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay);

                        if (ValueMinTopBottomUnRealized < TopUnRealized.Max(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay))
                            ValueMinTopBottomUnRealized = TopUnRealized.Min(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay);
                    }


                    BottomUnRealized = res.Where(x => !x.isRealized).OrderByDescending(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay).Take(20).Select(x => new PIPAnalysis
                    {
                        Type = x.Type,
                        isRealized = x.isRealized,
                        Title = x.Title,
                        LECost = x.LECost * -1,
                        LEDays = x.LEDays * -1,
                        LECostPerDay = x.LECostPerDay * -1,
                        PlanCost = x.PlanCost * -1,
                        PlanDays = x.PlanDays * -1,
                        PlanCostPerDay = x.PlanCostPerDay * -1,
                    }).ToList();

                    if (BottomUnRealized.Any())
                    {
                        if (ValueMaxTopBottomRealized < BottomUnRealized.Max(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay))
                            ValueMaxTopBottomRealized = BottomUnRealized.Max(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay);

                        if (ValueMinTopBottomRealized > BottomUnRealized.Min(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay))
                            ValueMinTopBottomRealized = BottomUnRealized.Min(x => filter.DataPoint == "cost" ? x.LECost : filter.DataPoint == "days" ? x.LEDays : x.LECostPerDay);
                    }
                }
                #endregion

                //get latest ls date
                var lsDate = GetLatestLsDate();

                return new
                {
                    Grid = DataHelper.ToDictionaryArray(datas),
                    TopRealized,
                    TopUnRealized,
                    BottomRealized,
                    BottomUnRealized,
                    ValueMaxTopBottomRealized,
                    ValueMinTopBottomRealized,
                    ValueMaxTopBottomUnRealized = ValueMaxTopBottomUnRealized  * 1.1,
                    ValueMinTopBottomUnRealized = Tools.Div(ValueMinTopBottomUnRealized, 1.1),
                    TotalRealized,
                    TotalUnRealized,
                    lsDate
                };
            });
        }

        public string GetLatestLsDate()
        {
            //return MvcResultInfo.Execute(null, document =>
            //{
            //var uls = UploadLsStatus.Populate<UploadLsStatus>();
            var ret = "No Latest Sequence Loaded";

            var date = WellActivity.GetLSDate();
            if(date > Tools.DefaultDate)
                ret = "Latest Sequence : " + Tools.ToUTC(date).ToString("dd-MMM-yyyy");



            //var t = new UploadLSController().GetCollectionName();
            //List<DateTime> docs = new List<DateTime>();
            //var lisSt = new List<string>();
            //foreach (string y in t)
            //{
            //    string yName = y.Substring(2, 6);
            //    lisSt.Add(yName);
            //}


            //var uls = UploadLsStatus.Populate<UploadLsStatus>();
            //if (uls.Any())
            //{
            //    uls = uls.OrderByDescending(x => x.LastUpdate).ToList();
            //    var fd = uls.FirstOrDefault();
            //    //ret = "Latest Sequence : " + Tools.ToUTC(fd.ExecuteDate).ToString("dd-MMM-yyyy") + (String.IsNullOrEmpty(fd.UserUpdate) ? "" : ", Executed by " + fd.UserUpdate);
            //    ret = "Latest Sequence : " + Tools.ToUTC(fd.ExecuteDate).ToString("dd-MMM-yyyy");
            //    if (fd.ExecuteDate <= Tools.DefaultDate)
            //    {
            //        foreach (var cc in lisSt)
            //        {
            //            var dt = DateTime.ParseExact(cc.ToString(), "yyyyMM", System.Globalization.CultureInfo.InvariantCulture);
            //            docs.Add(dt);
            //        }
            //        docs = docs.OrderByDescending(x => x).ToList();
            //        ret = "Latest Sequence : " + Tools.ToUTC(docs.FirstOrDefault()).ToString("dd-MMM-yyyy");
            //    }
            //    //ret = _ret.FirstOrDefault();
            //}
            return ret;
            //});
        }
        private static BsonDocument generateDataMovingChart(List<BsonDocument> Data, DateTime PeriodStart, DateTime PeriodFinish, string DataPoint, string groupBy, string category)
        {
            var bdoc = new BsonDocument();
            bdoc.Set("category", category);
            var getVal = Data.Where(x => BsonHelper.GetDateTime(x, "Finish") >= PeriodStart && BsonHelper.GetDateTime(x, "Finish") <= PeriodFinish);
            if (getVal.Any())
            {
                if (DataPoint.ToLower() == "cost")
                    bdoc.Set("value", getVal.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk")));
                else if (DataPoint.ToLower() == "days")
                    bdoc.Set("value", getVal.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk")));
                else
                {
                    var TotalCostPerDay = 0.0;
                    var groupby = "";
                    switch (groupBy.ToLower())
                    {
                        case "rig":
                            groupby = "RigName";
                            break;
                        case "well":
                            groupby = "RigName";
                            break;
                        case "pipcategory":
                            groupby = "Classification";
                            break;
                        case "idea":
                            groupby = "Title";
                            break;
                    }
                    var grp = getVal.GroupBy(x => BsonHelper.GetString(x, groupby));
                    foreach (var gi in grp)
                    {
                        var LEDays = gi.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                        var LECost = gi.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));
                        TotalCostPerDay += Tools.Div(LECost, LEDays);
                    }
                    bdoc.Set("value", TotalCostPerDay);
                }

            }

            return bdoc;
        }


        public JsonResult GetElementsDetail(DateTime dateStart, DateTime dateFinish, string type, bool isCR)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                DateRange dr = new DateRange(
                dateStart, dateFinish
                );
                List<BsonDocument> pipelines = new List<BsonDocument>();

                string uw;
                if (!isCR)
                    uw = "{ $unwind: '$Elements' }";
                else
                    uw = "{ $unwind: '$CRElements' }";

                string match1 = @"{$match:
                                        { 
              
                                          '$and' : [
              				                    {'Elements.Period.Start':
              	  				                    {'$gte':ISODate('" + dr.Start.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000')}
              				                    },
              				                    {'Elements.Period.Finish':
              	  				                    {'$lte':ISODate('" + dr.Finish.ToString("yyyy-MM-dd") + @"T00:00:00.000+0000')}
              				                    }
                                          ]
              
                                        }
                                    }";

                string project = @"
                                {$project:
                                {
	                                'SequenceId' : '$SequenceId',
	                                'PhaseNo' : '$PhaseNo',
	                                'ActivityType' :'$ActivityType',
	                                'WellName' : '$WellName',
	                                'Title' :'$Elements.Title',
	                                'Completion' : '$Elements.Completion',
	                                'PeriodStart' : '$Elements.Period.Start',
	                                'PeriodFinish' : '$Elements.Period.Finish',
	                                'AssignTOOps' : '$Elements.AssignTOOps',
	                                'Classification' : '$Elements.Classification',
	                                'DaysPlanImprovement'	: '$Elements.DaysPlanImprovement',
                                    'DaysPlanRisk'	: '$Elements.DaysPlanRisk',
	                                'DaysActualImprovement'	: '$Elements.DaysActualImprovement',
	                                'DaysActualRisk'	: '$Elements.DaysActualRisk',
	                                'DaysLastWeekImprovement'	: '$Elements.DaysLastWeekImprovement',
	                                'DaysLastWeekRisk'	: '$Elements.DaysLastWeekRisk',
	                                'DaysCurrentWeekImprovement'	: '$Elements.DaysCurrentWeekImprovement',
	                                'DaysCurrentWeekRisk'	: '$Elements.DaysCurrentWeekRisk',
	                                'CostPlanImprovement'	: '$Elements.CostPlanImprovement',
	                                'CostPlanRisk'	: '$Elements.CostPlanRisk',
	                                'CostActualImprovement'	: '$Elements.CostActualImprovement',
	                                'CostActualRisk'	: '$Elements.CostActualRisk',
	                                'CostCurrentWeekImprovement'	: '$Elements.CostCurrentWeekImprovement',
	                                'CostCurrentWeekRisk'	: '$Elements.CostCurrentWeekRisk',
	                                'CostLastWeekImprovement'	: '$Elements.CostLastWeekImprovement',
	                                'CostLastWeekRisk'	: '$Elements.CostLastWeekRisk'
                                } }
                                ";
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(uw));
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(match1));
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(project));
                List<BsonDocument> aggregates = DataHelper.Aggregate(new WellPIP().TableName, pipelines);

                List<PIPElementDetails> details = new List<PIPElementDetails>();
                if (aggregates.Any())
                {
                    foreach (var t in aggregates)
                    {
                        PIPElementDetails det = BsonHelper.Deserialize<PIPElementDetails>(t.ToBsonDocument());// t.Deserialize();

                        det.LEDays = det.DaysCurrentWeekImprovement + det.DaysCurrentWeekRisk;
                        det.LECost = det.CostCurrentWeekImprovement + det.CostCurrentWeekRisk;

                        details.Add(det);
                    }

                    #region condition

                    switch (type)
                    {
                        case "realized":
                            {
                                var res = details.Where(x => x.Completion.Equals("Realized")).ToList();
                                return res;
                                break;
                            }
                        case "unrealized-completed":
                            {
                                var res = details.Where(x => !x.Completion.Equals("Realized") && x.PeriodFinish < Tools.ToUTC(DateTime.Now.Date)).ToList();
                                return res;
                                break;
                            }
                        case "unrealized-inprogress":
                            {

                                var res = details.Where(x => !x.Completion.Equals("Realized") &&
                                    (x.PeriodStart <= Tools.ToUTC(DateTime.Now.Date) && x.PeriodFinish >= Tools.ToUTC(DateTime.Now.Date))

                                    ).ToList();
                                return res;

                                break;
                            }
                        default:
                            {
                                return new List<PIPElementDetails>();
                                break;
                            }
                    }
                    #endregion

                }
                return new List<PIPElementDetails>();
            });

            

        }

        public JsonResult GetDataScatter(PIPAnalysisFilter filter)
        {
            return MvcResultInfo.Execute(() =>
            {
                var datas = new List<BsonDocument>();
                var scatters = new List<PIPAnalysisScatter>();
                var res = PopulateData(filter, out datas, out scatters);


                #region Scatter Data
                var ScatterData = new List<ScatterModelPIPAnalysis>();
                var ScatterDataRealized = new List<ScatterModelPIPAnalysis>();
                var ScatterDataUnRealizedCompleted = new List<ScatterModelPIPAnalysis>();
                var ScatterDataUnRealizedInProgress = new List<ScatterModelPIPAnalysis>();
                if (scatters.Any())
                {
                    foreach (var s in scatters)
                    {
                        var sd = new ScatterModelPIPAnalysis();
                        sd.ActivityType = s.ActivityType;
                        sd.Element = s.Title;
                        sd.Period = s.Period;
                        sd.WellName = s.WellName;
                        sd.RigName = s.RigName;
                        sd.ValueX = s.Value.ValueX;
                        sd.Status = s.Status;
                        sd.Completion = s.Completion;
                        switch (filter.DataPoint)
                        {
                            case "days":
                                sd.ValueY = s.Value.ValueLEDays;
                                break;
                            case "cost":
                                sd.ValueY = s.Value.ValueLECost;
                                break;
                            case "costperdays":
                                sd.ValueY = s.Value.ValueCostPerDays;
                                break;
                        }
                        ScatterData.Add(sd);
                        if (s.Completion.ToLower() == "realized")
                        {
                            ScatterDataRealized.Add(sd);
                        }
                        else
                        {
                            if (sd.Status == "Completed Event")
                            {
                                ScatterDataUnRealizedCompleted.Add(sd);
                            }
                            else
                            {
                                ScatterDataUnRealizedInProgress.Add(sd);
                            }
                        }
                    }
                }

                var MinXScatter = filter.PeriodStart < DateTime.Now ? (filter.PeriodStart - DateTime.Now).Days : 0.0;
                var MaxXScatter = filter.PeriodFinish > DateTime.Now ? (filter.PeriodFinish - DateTime.Now).Days : 0.0;
                var MinYScatter = 0.0;
                var MaxYScatter = 0.0;


                if (ScatterData.Any())
                {
                    //MinXScatter = ScatterData.Min(x => x.ValueX);
                    //MaxXScatter = ScatterData.Max(x => x.ValueX);
                    MinYScatter = ScatterData.Min(x => x.ValueY);
                    MaxYScatter = ScatterData.Max(x => x.ValueY);
                }


                #endregion

                #region Data Moving Chart

                var UnrealizedData = datas.Where(x => BsonHelper.GetString(x, "Completion") != "Realized").ToList();
                var RealizedData = datas.Where(x => BsonHelper.GetString(x, "Completion").Equals("Realized")).ToList();
                var UnRealizedCompletedData = new List<BsonDocument>();
                var UnRealizedInprogressData = new List<BsonDocument>();

                if (UnrealizedData.Any())
                {
                    foreach (var ud in UnrealizedData)
                    {
                        var finish = ud.GetDateTime("Finish");
                        var start = ud.GetDateTime("Start");
                        if (finish.Date < DateTime.Now.Date)
                        {
                            ud.Set("Status", "Completed Event");
                            UnRealizedCompletedData.Add(ud);
                        }
                        else
                        {
                            ud.Set("Status", "In Progress");
                            UnRealizedInprogressData.Add(ud);
                        }
                    }
                }
                
                var DataMovingChartRealized = new List<BsonDocument>();
                var DataMovingChartUnRealizedCompleted = new List<BsonDocument>();
                var DataMovingChartUnRealizedInProgress = new List<BsonDocument>();
                var MovingChartMax = 0.0;
                var MovingChartMin = 0.0;
                var periodStart = filter.PeriodStart;

                while (periodStart <= filter.PeriodFinish)
                {
                    var category = periodStart.ToString("MMM-yyyy");
                    DateTime endOfMonth = new DateTime(periodStart.Year, periodStart.Month, DateTime.DaysInMonth(periodStart.Year, periodStart.Month));
                    if (RealizedData.Any())
                    {
                        var bdoc = generateDataMovingChart(RealizedData, filter.PeriodStart, endOfMonth, filter.DataPoint, filter.GroupBy, category);
                        DataMovingChartRealized.Add(bdoc);
                    }

                    if (UnRealizedCompletedData.Any())
                    {
                        var bdoc = generateDataMovingChart(UnRealizedCompletedData, filter.PeriodStart, endOfMonth, filter.DataPoint, filter.GroupBy, category);
                        DataMovingChartUnRealizedCompleted.Add(bdoc);
                    }

                    if (UnRealizedInprogressData.Any())
                    {
                        var bdoc = generateDataMovingChart(UnRealizedInprogressData, filter.PeriodStart, endOfMonth, filter.DataPoint, filter.GroupBy, category);
                        DataMovingChartUnRealizedInProgress.Add(bdoc);
                    }

                    periodStart = periodStart.AddMonths(1);
                }

                if (DataMovingChartRealized.Any())
                {
                    MovingChartMax = DataMovingChartRealized.Max(x => BsonHelper.GetDouble(x, "value"));
                    MovingChartMin = DataMovingChartRealized.Min(x => BsonHelper.GetDouble(x, "value"));
                }
                if (DataMovingChartUnRealizedInProgress.Any())
                {
                    if (MovingChartMax < DataMovingChartUnRealizedInProgress.Max(x => BsonHelper.GetDouble(x, "value")))
                    {
                        MovingChartMax = DataMovingChartUnRealizedInProgress.Max(x => BsonHelper.GetDouble(x, "value"));
                    }

                    if (MovingChartMin > DataMovingChartUnRealizedInProgress.Min(x => BsonHelper.GetDouble(x, "value")))
                    {
                        MovingChartMin = DataMovingChartUnRealizedInProgress.Min(x => BsonHelper.GetDouble(x, "value"));
                    }

                }
                if (DataMovingChartUnRealizedCompleted.Any())
                {
                    if (MovingChartMax < DataMovingChartUnRealizedCompleted.Max(x => BsonHelper.GetDouble(x, "value")))
                    {
                        MovingChartMax = DataMovingChartUnRealizedCompleted.Max(x => BsonHelper.GetDouble(x, "value"));
                    }

                    if (MovingChartMin > DataMovingChartUnRealizedCompleted.Min(x => BsonHelper.GetDouble(x, "value")))
                    {
                        MovingChartMin = DataMovingChartUnRealizedCompleted.Min(x => BsonHelper.GetDouble(x, "value"));
                    }

                }

                #endregion

                return new
                {
                    ScatterDataRealized,ScatterDataUnRealizedCompleted,ScatterDataUnRealizedInProgress,
                    MinXScatter,
                    MinYScatter,
                    MaxXScatter,
                    MaxYScatter,
                    DataMovingChartRealized = DataHelper.ToDictionaryArray(DataMovingChartRealized),
                    DataMovingChartUnRealizedCompleted = DataHelper.ToDictionaryArray(DataMovingChartUnRealizedCompleted),
                    DataMovingChartUnRealizedInProgress = DataHelper.ToDictionaryArray(DataMovingChartUnRealizedInProgress),
                    MovingChartMax,
                    MovingChartMin
                };
            });
        }


        public static List<PIPAnalysis> PopulateData(PIPAnalysisFilter filter, out List<BsonDocument> datas, out List<PIPAnalysisScatter> scatters)
        {
            // unwind Phases
            string aggregateCond1 = "{ $unwind: '$Phases' }";
            string projectio = @"{$project :  { 
                'Region' : '$Region',
                'RigName' : '$RigName',
                'WellName' : '$WellName',
                'EXType' : '$EXType',
                'ActivityType' : '$Phases.ActivityType',
                'LSSchedule' : '$Phases.PhSchedule',
                'SequenceId' : '$UARigSequenceId',
'PhaseNo' : '$Phases.PhaseNo',
                } }";

            string m1 = @"{ $match:{ 'LSSchedule.Start' : { '$gt' :  ISODate('1900-01-01T00:00:00.000+0000')   } }  }";

            List<BsonDocument> pipelines = new List<BsonDocument>();
            if (filter.ProjectNames != null)
            {
                if (filter.ProjectNames.Any())
                {
                    var MatchProject = new BsonDocument().Add("$match", new BsonDocument().Set("ProjectName", new BsonDocument().Set("$in", new BsonArray(filter.ProjectNames))));
                    pipelines.Add(MatchProject);
                }
            }
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(projectio));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(m1));
            List<BsonDocument> aggregates = DataHelper.Aggregate(new WellActivity().TableName, pipelines);

            if (filter.Regions.Any())
                aggregates = aggregates.Where(x => filter.Regions.Contains(BsonHelper.GetString(x, "Region"))).ToList();

            if (filter.RigNames.Any())
                aggregates = aggregates.Where(x => filter.RigNames.Contains(BsonHelper.GetString(x, "RigName"))).ToList();

            if (filter.WellNames.Any())
                aggregates = aggregates.Where(x => filter.WellNames.Contains(BsonHelper.GetString(x, "WellName"))).ToList();

            if (filter.ExType.Any())
                aggregates = aggregates.Where(x => filter.ExType.Contains(BsonHelper.GetString(x, "EXType"))).ToList();

            if (filter.Activities.Any())
                aggregates = aggregates.Where(x => filter.Activities.Contains(BsonHelper.GetString(x, "ActivityType"))).ToList();
            else
            {
                // activityCateg
                if (filter.ActivitiesCategories.Any())
                {
                    var getAct = ActivityMaster.Populate<ActivityMaster>(Query.In("ActivityCategory", new BsonArray(filter.ActivitiesCategories)));
                    if (getAct.Any())
                    {
                        var acts = getAct.Select(d => d._id.ToString()).ToList();
                        aggregates = aggregates.Where(x => acts.Contains(BsonHelper.GetString(x, "ActivityType"))).ToList();
                    }
                }
            }

            var whatToTake = "$Elements";
            //if (filter.PIPType.ToLower() == "general scm")
            //{
            //    whatToTake = "$CRElements";
            //}
            //var pips = WellPIP.Populate<WellPIP>(Qi)
            string pc1 = "{ $unwind: '"+ whatToTake +"' }";
            string pc2 = "{$project :  { "+
                    "'ActivityType' : '$ActivityType',"+
                    "'SequenceId' : '$SequenceId',"+
                    "'WellName' : '$WellName',"+
                    "'Realized' : '"+ whatToTake +".Realized',"+
                    "'PhaseNo' : '$PhaseNo',"+
                    "'Title' : '"+ whatToTake +".Title',"+
                    "'DaysPlanImprovement' : '"+ whatToTake +".DaysPlanImprovement', "+
                    "'DaysPlanRisk' : '"+ whatToTake +".DaysPlanRisk', "+
                    "'DaysActualImprovement' : '"+ whatToTake +".DaysActualImprovement', "+
                    "'DaysActualRisk' : '"+ whatToTake +".DaysActualRisk', "+
                    "'DaysLastWeekImprovement' : '"+ whatToTake +".DaysLastWeekImprovement', "+
                    "'DaysLastWeekRisk' : '"+ whatToTake +".DaysLastWeekRisk', "+
                    "'DaysCurrentWeekImprovement' : '"+ whatToTake +".DaysCurrentWeekImprovement', "+
                    "'DaysCurrentWeekRisk' :'"+ whatToTake +".DaysCurrentWeekRisk', "+
                    "'CostPlanImprovement' : '"+ whatToTake +".CostPlanImprovement',  "+
                    "'CostPlanRisk' : '"+ whatToTake +".CostPlanRisk', "+
                    "'CostActualImprovement' : '"+ whatToTake +".CostActualImprovement', "+
                    "'CostActualRisk' : '"+ whatToTake +".CostActualRisk', "+
                    "'CostCurrentWeekImprovement' : '"+ whatToTake +".CostCurrentWeekImprovement', "+
                    "'CostCurrentWeekRisk' : '"+ whatToTake +".CostCurrentWeekRisk', "+
                    "'CostLastWeekImprovement' : '"+ whatToTake +".CostLastWeekImprovement',  "+
                    "'CostLastWeekRisk' : '"+ whatToTake +".CostLastWeekRisk', "+
                    "'Classification' : '"+ whatToTake +".Classification', "+
                    "'Theme' : '"+ whatToTake +".Theme', "+
                    "'Completion' : '"+ whatToTake +".Completion', "+
                    "'PerformanceUnit' : '"+ whatToTake +".PerformanceUnit', "+
			        "'Start' : '"+ whatToTake +".Period.Start', "+
			        "'Finish' : '"+ whatToTake +".Period.Finish', "+
                    "'OPs' : '" + whatToTake + ".AssignTOOps' " +
                    "} }";
            List<BsonDocument> pipagg = new List<BsonDocument>();
            List<BsonDocument> pp = new List<BsonDocument>();

            if (filter.Classifications.Any())
            {
                var arrstr = new List<string>();
                foreach (var t in filter.Classifications)
                {
                    arrstr.Add("'" + t + "'");
                }

                string pc3 = @"{ $match:{ 'Classification' : { '$in' : " + new BsonArray(arrstr) + "   } }  }";
                pp.Add(BsonSerializer.Deserialize<BsonDocument>(pc1));
                pp.Add(BsonSerializer.Deserialize<BsonDocument>(pc2));
                pp.Add(BsonSerializer.Deserialize<BsonDocument>(pc3));
            }
            else
            {
                pp.Add(BsonSerializer.Deserialize<BsonDocument>(pc1));
                pp.Add(BsonSerializer.Deserialize<BsonDocument>(pc2));
            }

            pipagg = DataHelper.Aggregate(new WellPIP().TableName, pp);
            // filter period
            pipagg =
                pipagg.Where(
                    x =>
                        BsonHelper.GetDateTime(x, "Start") >= filter.PeriodStart.Date &&
                        BsonHelper.GetDateTime(x, "Finish") <= filter.PeriodFinish.Date).ToList();// filter.PeriodStart.Date <= BsonHelper.GetDateTime(x, "Finish") && filter.PeriodFinish.Date >= BsonHelper.GetDateTime(x, "Finish")).ToList();

            // filter OPs
            if (filter.OPs.Any())
            {
                List<BsonDocument> docs = new List<BsonDocument>();
                    foreach (var y in pipagg)
                    {
                        var ops1 = BsonHelper.Get(y, "OPs");
                        if (ops1 != BsonNull.Value)
                        {
                            List<string> ops = new List<string>();
                            foreach (var o in ops1.AsBsonArray)
                            {
                                ops.Add(o.AsString);
                            }

                            var BaseOP = ops.ToArray();
                            if (filter.OPRelation.ToLower() == "and")
                            {
                                //match
                                var match = true;
                                foreach (var op in filter.OPs)
                                {
                                    match = Array.Exists(BaseOP, element => element == op);
                                    if (!match) break;
                                }
                                if (match)
                                    docs.Add(y);
                            }
                            else
                            {
                                //contains
                                var match = false;
                                foreach (var op in filter.OPs)
                                {
                                    match = Array.Exists(BaseOP, element => element == op);
                                    if (match) break;
                                }
                                if (match)
                                    docs.Add(y);
                            }

                        }



                    }
                    pipagg = docs;
            }



            // match pip with WP
            List<BsonDocument> resPIP = new List<BsonDocument>();
            foreach (var wp in aggregates)
            {
                var pipInWP = new List<BsonDocument>();

                if (filter.PIPType.ToLower() == "general scm")
                {
                    var wellname = BsonHelper.GetString(wp, "RigName") + "_CR";
                    pipInWP = pipagg.Where(x =>
                        BsonHelper.GetString(x, "WellName").Equals(wellname)
                        ).ToList();
                }
                else
                {
                    var wellname = BsonHelper.GetString(wp, "WellName");
                    var activitytype = BsonHelper.GetString(wp, "ActivityType");
                    var seqId = BsonHelper.GetString(wp, "PhaseNo");
                    pipInWP = pipagg.Where(x =>
                        BsonHelper.GetString(x, "WellName").Equals(wellname) &&
                        BsonHelper.GetString(x, "ActivityType").Equals(activitytype) &&
                        BsonHelper.GetString(x, "PhaseNo").Equals(seqId)
                        ).ToList();
                }
                

                if (pipInWP.Any())
                {
                    foreach (var p in pipInWP)
                    {
                        p.Set("RigName", wp.GetString("RigName"));
                        p.Set("Title", p.GetString("Title"));
                        p.Set("LEDays", p.GetDouble("DaysCurrentWeekImprovement") + p.GetDouble("DaysCurrentWeekRisk"));
                        p.Set("LECost", p.GetDouble("CostCurrentWeekImprovement") + p.GetDouble("CostCurrentWeekRisk"));
                        resPIP.Add(p);
                    }

                }
            }

            // group by 
            List<PIPAnalysis> result = new List<PIPAnalysis>();
            resPIP = resPIP.Distinct().ToList();
            if (filter.GroupBy.Trim().ToLower().Contains("rig"))
            {
                var grp = resPIP.GroupBy(x => BsonHelper.GetString(x, "RigName"));
                foreach (var gi in grp)
                {
                    var realizedgi = gi.ToList().Where(x => BsonHelper.GetString(x, "Completion").Equals("Realized"));
                    PIPAnalysis an = new PIPAnalysis();
                    if (realizedgi.Any())
                    {
                        an.Type = "Rig";
                        an.isRealized = true;
                        an.Title = gi.Key;
                        switch (filter.TakeDataFor)
                        {
                            case "total":
                                an.LECost = realizedgi.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));
                                an.LEDays = realizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                                an.LECostPerDay = Tools.Div(an.LECost, an.LEDays);
                                an.PlanCost = realizedgi.Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                                an.PlanDays = realizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                                an.PlanCostPerDay = Tools.Div(an.PlanCost, an.PlanDays);
                                break;
                            case "risk":
                                an.LECost = realizedgi.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));
                                an.LEDays = realizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                                an.LECostPerDay = Tools.Div(an.LECost, an.LEDays);
                                an.PlanCost = realizedgi.Sum(x => BsonHelper.GetDouble(x, "CostPlanRisk"));
                                an.PlanDays = realizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysPlanRisk"));
                                an.PlanCostPerDay = Tools.Div(an.PlanCost, an.PlanDays);
                                break;
                            case "opportunity":
                                an.LECost = realizedgi.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement"));
                                an.LEDays = realizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement"));
                                an.LECostPerDay = Tools.Div(an.LECost, an.LEDays);
                                an.PlanCost = realizedgi.Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement"));
                                an.PlanDays = realizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement"));
                                an.PlanCostPerDay = Tools.Div(an.PlanCost, an.PlanDays);
                                break;
                        }
                        result.Add(an);
                    }
                    var unrealizedgi = gi.ToList().Where(x => !BsonHelper.GetString(x, "Completion").Equals("Realized"));
                    if (unrealizedgi.Any())
                    {
                        an = new PIPAnalysis();
                        an.Type = "Rig";
                        an.isRealized = false;
                        an.Title = gi.Key;
                        switch (filter.TakeDataFor)
                        {
                            case "total":
                                an.LECost = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));
                                an.LEDays = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                                an.LECostPerDay = Tools.Div(an.LECost, an.LEDays);
                                an.PlanCost = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                                an.PlanDays = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                                an.PlanCostPerDay = Tools.Div(an.PlanCost, an.PlanDays);
                                break;
                            case "risk":
                                an.LECost = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));
                                an.LEDays = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                                an.LECostPerDay = Tools.Div(an.LECost, an.LEDays);
                                an.PlanCost = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "CostPlanRisk"));
                                an.PlanDays = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysPlanRisk"));
                                an.PlanCostPerDay = Tools.Div(an.PlanCost, an.PlanDays);
                                break;
                            case "opportunity":
                                an.LECost = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement"));
                                an.LEDays = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement"));
                                an.LECostPerDay = Tools.Div(an.LECost, an.LEDays);
                                an.PlanCost = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement"));
                                an.PlanDays = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement"));
                                an.PlanCostPerDay = Tools.Div(an.PlanCost, an.PlanDays);
                                break;
                        }
                        result.Add(an);
                    }

                }
            }
            else if (filter.GroupBy.Trim().ToLower().Contains("well"))
            {
                var grp = resPIP.GroupBy(x => BsonHelper.GetString(x, "WellName"));
                foreach (var gi in grp)
                {
                    var realizedgi = gi.ToList().Where(x => BsonHelper.GetString(x, "Completion").Equals("Realized"));
                    PIPAnalysis an = new PIPAnalysis();
                    if (realizedgi.Any())
                    {
                        an.Type = "Rig";
                        an.isRealized = true;
                        an.Title = gi.Key;
                        switch (filter.TakeDataFor)
                        {
                            case "total":
                                an.LECost = realizedgi.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));
                                an.LEDays = realizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                                an.LECostPerDay = Tools.Div(an.LECost, an.LEDays);
                                an.PlanCost = realizedgi.Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                                an.PlanDays = realizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                                an.PlanCostPerDay = Tools.Div(an.PlanCost, an.PlanDays);
                                break;
                            case "risk":
                                an.LECost = realizedgi.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));
                                an.LEDays = realizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                                an.LECostPerDay = Tools.Div(an.LECost, an.LEDays);
                                an.PlanCost = realizedgi.Sum(x => BsonHelper.GetDouble(x, "CostPlanRisk"));
                                an.PlanDays = realizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysPlanRisk"));
                                an.PlanCostPerDay = Tools.Div(an.PlanCost, an.PlanDays);
                                break;
                            case "opportunity":
                                an.LECost = realizedgi.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement"));
                                an.LEDays = realizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement"));
                                an.LECostPerDay = Tools.Div(an.LECost, an.LEDays);
                                an.PlanCost = realizedgi.Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement"));
                                an.PlanDays = realizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement"));
                                an.PlanCostPerDay = Tools.Div(an.PlanCost, an.PlanDays);
                                break;
                        }
                        result.Add(an);
                    }
                    var unrealizedgi = gi.ToList().Where(x => !BsonHelper.GetString(x, "Completion").Equals("Realized"));
                    if (unrealizedgi.Any())
                    {
                        an = new PIPAnalysis();
                        an.Type = "Rig";
                        an.isRealized = false;
                        an.Title = gi.Key;
                        switch (filter.TakeDataFor)
                        {
                            case "total":
                                an.LECost = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));
                                an.LEDays = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                                an.LECostPerDay = Tools.Div(an.LECost, an.LEDays);
                                an.PlanCost = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                                an.PlanDays = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                                an.PlanCostPerDay = Tools.Div(an.PlanCost, an.PlanDays);
                                break;
                            case "risk":
                                an.LECost = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));
                                an.LEDays = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                                an.LECostPerDay = Tools.Div(an.LECost, an.LEDays);
                                an.PlanCost = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "CostPlanRisk"));
                                an.PlanDays = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysPlanRisk"));
                                an.PlanCostPerDay = Tools.Div(an.PlanCost, an.PlanDays);
                                break;
                            case "opportunity":
                                an.LECost = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement"));
                                an.LEDays = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement"));
                                an.LECostPerDay = Tools.Div(an.LECost, an.LEDays);
                                an.PlanCost = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement"));
                                an.PlanDays = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement"));
                                an.PlanCostPerDay = Tools.Div(an.PlanCost, an.PlanDays);
                                break;
                        }
                        result.Add(an);
                    }

                }
            }
            else if (filter.GroupBy.Trim().ToLower().Contains("idea"))
            {
                var grp = resPIP.GroupBy(x => BsonHelper.GetString(x, "Title"));
                foreach (var gi in grp)
                {
                    var realizedgi = gi.ToList().Where(x => BsonHelper.GetString(x, "Completion").Equals("Realized"));
                    PIPAnalysis an = new PIPAnalysis();
                    if (realizedgi.Any())
                    {
                        an.Type = "Rig";
                        an.isRealized = true;
                        an.Title = gi.Key;
                        an.LECost = realizedgi.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));
                        an.LEDays = realizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                        an.LECostPerDay = Tools.Div(an.LECost, an.LEDays);
                        an.PlanCost = realizedgi.Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                        an.PlanDays = realizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                        an.PlanCostPerDay = Tools.Div(an.PlanCost, an.PlanDays);
                        result.Add(an);
                    }
                    var unrealizedgi = gi.ToList().Where(x => !BsonHelper.GetString(x, "Completion").Equals("Realized"));
                    if (unrealizedgi.Any())
                    {
                        an = new PIPAnalysis();
                        an.Type = "Rig";
                        an.isRealized = false;
                        an.Title = gi.Key;
                        an.LECost = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));
                        an.LEDays = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                        an.LECostPerDay = Tools.Div(an.LECost, an.LEDays);
                        an.PlanCost = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                        an.PlanDays = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                        an.PlanCostPerDay = Tools.Div(an.PlanCost, an.PlanDays);
                        result.Add(an);
                    }

                }
            }
            else
            {
                var grp = resPIP.GroupBy(x => BsonHelper.GetString(x, "Classification"));
                foreach (var gi in grp)
                {
                    var realizedgi = gi.ToList().Where(x => BsonHelper.GetString(x, "Completion").Equals("Realized"));
                    PIPAnalysis an = new PIPAnalysis();
                    if (realizedgi.Any())
                    {
                        an.Type = "Rig";
                        an.isRealized = true;
                        an.Title = gi.Key;
                        an.LECost = realizedgi.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));
                        an.LEDays = realizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                        an.LECostPerDay = Tools.Div(an.LECost, an.LEDays);
                        an.PlanCost = realizedgi.Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                        an.PlanDays = realizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                        an.PlanCostPerDay = Tools.Div(an.PlanCost, an.PlanDays);
                        result.Add(an);
                    }
                    var unrealizedgi = gi.ToList().Where(x => !BsonHelper.GetString(x, "Completion").Equals("Realized"));
                    if (unrealizedgi.Any())
                    {
                        an = new PIPAnalysis();
                        an.Type = "Rig";
                        an.isRealized = false;
                        an.Title = gi.Key;
                        an.LECost = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "CostCurrentWeekImprovement") + BsonHelper.GetDouble(x, "CostCurrentWeekRisk"));
                        an.LEDays = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysCurrentWeekImprovement") + BsonHelper.GetDouble(x, "DaysCurrentWeekRisk"));
                        an.LECostPerDay = Tools.Div(an.LECost, an.LEDays);
                        an.PlanCost = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "CostPlanImprovement") + BsonHelper.GetDouble(x, "CostPlanRisk"));
                        an.PlanDays = unrealizedgi.Sum(x => BsonHelper.GetDouble(x, "DaysPlanImprovement") + BsonHelper.GetDouble(x, "DaysPlanRisk"));
                        an.PlanCostPerDay = Tools.Div(an.PlanCost, an.PlanDays);
                        result.Add(an);
                    }

                }
            }

            datas = resPIP;

            #region Scatter
            List<BsonDocument> scatterData = new List<BsonDocument>();
            //foreach (var t in resPIP.Where(x => !BsonHelper.GetString(x, "Completion").Equals("Realized")))
            //    scatterData.Add(t);

            scatterData.AddRange(resPIP);
            List<PIPAnalysisScatter> scatterVal = new List<PIPAnalysisScatter>();
            foreach (var s in scatterData)
            {
                PIPAnalysisScatter v = new PIPAnalysisScatter();
                v.Completion = s.GetString("Completion");
                v.ActivityType = s.GetString("ActivityType");
                v.WellName = s.GetString("WellName");
                v.Title = s.GetString("Title");
                v.RigName = s.GetString("RigName");
                v.Period.Start = s.GetDateTime("Start");
                v.Period.Finish = s.GetDateTime("Finish");
                if (v.Period.Finish.Date < DateTime.Now.Date)
                    v.Status = "Completed Event";
                else if (v.Period.Start.Date > DateTime.Now.Date)
                    v.Status = "Future Event";
                else
                    v.Status = "In Progress";
                v.Value.ValueX = (v.Period.Finish - DateTime.Now).Days;
                v.Value.ValueLECost = s.GetDouble("CostCurrentWeekImprovement") + s.GetDouble("CostCurrentWeekRisk");
                v.Value.ValueLEDays = s.GetDouble("DaysCurrentWeekImprovement") + s.GetDouble("DaysCurrentWeekRisk");
                v.Value.ValueCostPerDays = Tools.Div(v.Value.ValueLECost, v.Value.ValueLEDays);

                scatterVal.Add(v);
            }
            #endregion
            scatters = scatterVal;

            return result;
        }


        public JsonResult Get(PIPAnalysisFilter filter)
        {



            var result = new ResultInfo();
            result.Data = new
            {
                Data = ""
            };
            return MvcTools.ToJsonResult(result.Data, JsonRequestBehavior.AllowGet);

        }
    }

    internal class ScatterModelPIPAnalysis
    {
        public string ActivityType { get; set; }
        public string Completion { get; set; }
        public string WellName { get; set; }
        public string Status { get; set; }
        public string RigName { get; set; }
        public string Element { get; set; }
        public DateRange Period { get; set; }
        public double ValueX { get; set; }
        public double ValueY { get; set; }
    }
}