using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Configuration;
using System.IO;
using System.Text.RegularExpressions;

using ECIS.Core;
using ECIS.Client.WEIS;
using ECIS.AppServer;
using MongoDB.Bson; 
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using Newtonsoft.Json;

using Aspose.Cells;
using Aspose.Pdf;
using Aspose.Pdf.Generator;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class BizPlanSequenceController : Controller
    {
        public ActionResult Index()
        {
            var lsInfo = new PIPAnalysisController().GetLatestLsDate();
            ViewBag.LatestLS = lsInfo;

            return View();
        }

        public JsonResult GetBizPlanActivity(string dateStart, string dateFinish, string dateStart2, string dateFinish2, string dateRelation, string[] operatingUnits, string[] projectNames, string[] regions, string[] rigNames, string[] rigTypes, string[] wellNames, string[] exType)
        {
            var Now = Tools.ToUTC(new DateTime());
            var Today = Tools.ToUTC(new DateTime(Now.Year, Now.Month, Now.Day));
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var dashboardC = new DashboardController();
                var division = 1000000.0;
                var query = dashboardC.GenerateQueryFromFilterAndDate(dateStart, dateFinish, dateStart2, dateFinish2, dateRelation, operatingUnits,
                    projectNames, regions, rigNames, rigTypes, wellNames, null, "OpsSchedule.Start", "OpsSchedule.Finish", exType);
                var raw = BizPlanActivity.Populate<BizPlanActivity>(query);
                var final = raw
                   .Select(d =>
                   {
                       d.Phases = (d.Phases == null ? new List<BizPlanActivityPhase>() : d.Phases);

                       return new
                       {
                           _id = d._id,
                           Region = d.Region,
                           OperatingUnit = d.OperatingUnit,
                           d.UARigSequenceId,
                           RigType = d.RigType,
                           RigName = d.RigName,
                           ProjectName = d.ProjectName,
                           WellName = d.WellName,
                           AssetName = d.AssetName,
                           NonOP = d.NonOP,
                           WorkingInterest = d.WorkingInterest,
                           FirmOrOption = d.FirmOrOption,
                           PsDuration = d.PsSchedule != null ? d.PsSchedule.Days : 0,
                           PlanDuration = d.Phases.Sum(p => p.Plan.Days),
                           PlanCost = Tools.Div(d.Phases.Sum(p => p.Plan.Cost), 1000000),
                           //OpsStart = (d.Phases.Count() > 0 ? d.Phases
                           //     .Where(e=>e.ActivityType.ToLower().Contains("risk")==false)
                           //     .DefaultIfEmpty(new BizPlanActivityPhase())
                           //     .Min(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Start) : Tools.DefaultDate),
                           //OpsFinish = (d.Phases.Count() > 0 ? d.Phases.Max(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Finish) : Tools.DefaultDate),
                           //OpsDuration = (d.Phases.Count() > 0 ? d.Phases.Sum(e => ((DateRange)Tools.Coelesce(new object[] { e.PhSchedule, new DateRange() })).Days) : 0),
                           OpsCost = Tools.Div(d.Phases.Count() > 0 ? d.Phases
                                .Where(e => e.ActivityType.ToLower().Contains("risk") == false)
                                .DefaultIfEmpty(new BizPlanActivityPhase())
                                .Sum(e => e.OP.Cost) : 0, division),
                           OpsStart = d.OpsSchedule != null ? d.OpsSchedule.Start : Tools.DefaultDate,
                           OpsFinish = d.OpsSchedule != null ? d.OpsSchedule.Finish : Tools.DefaultDate,
                           OpsDuration = d.OpsSchedule != null ? d.OpsSchedule.Days : 0,
                           //OpsCost = d
                           PhDuration = (d.Phases.Count() > 0 ? d.Phases.Sum(x => x.PhSchedule == null ? 0 : x.PhSchedule.Days) : 0),
                           UARigDescription = String.Format("{0} - {1}", d.UARigSequenceId, d.UARigDescription == null ? "" : d.UARigDescription),
                           PhRiskDuration = (d.Phases.Count() > 0 ? d.Phases.Where(x => x.RiskFlag == null ? false : !x.RiskFlag.Equals("")).Sum(x => x.PhSchedule == null ? 0 : x.PhSchedule.Days) : 0),
                           PhStartForFilter = (d.Phases.Count() > 0 ? d.Phases.Min(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Start) : Tools.DefaultDate),
                           PhFinishForFilter = (d.Phases.Count() > 0 ? d.Phases.Max(e => e.PhSchedule == null ? Tools.DefaultDate : e.PhSchedule.Finish) : Tools.DefaultDate),
                           PsStartForFilter = (d.PsSchedule == null ? Tools.DefaultDate : d.PsSchedule.Start),
                           PsFinishForFilter = (d.PsSchedule == null ? Tools.DefaultDate : d.PsSchedule.Finish),
                           PsStart = (d.PsSchedule == null ? Tools.DefaultDate : d.PsSchedule.Start),
                           PsFinish = (d.PsSchedule == null ? Tools.DefaultDate : d.PsSchedule.Finish),
                           PhCost = (d.Phases.Count() > 0 ? (d.Phases.Sum(e => e.OP.Cost) / division) : 0),
                           AFEStart = (d.Phases.Count() > 0 ? d.Phases.Min(e => e.AFESchedule == null ? Tools.DefaultDate : e.AFESchedule.Start) : Tools.DefaultDate).ToString("dd-MMM-yy"),
                           AFEFinish = (d.Phases.Count() > 0 ? d.Phases.Max(e => e.AFESchedule == null ? Tools.DefaultDate : e.AFESchedule.Finish) : Tools.DefaultDate).ToString("dd-MMM-yy"),
                           AFEDuration = (d.Phases.Count() > 0 ? d.Phases.Sum(e => ((WellDrillData)Tools.Coelesce(new object[] { e.AFE, new WellDrillData() })).Days) : 0),
                           AFECost = (d.Phases.Count() > 0 ? (d.Phases.Sum(e => ((WellDrillData)Tools.Coelesce(new object[] { e.AFE, new WellDrillData() })).Cost) / division) : 0),
                           LEStart = d.LESchedule != null ? d.LESchedule.Start : Tools.DefaultDate,
                           LEFinish = d.LESchedule != null ? d.LESchedule.Finish : Tools.DefaultDate,
                           LEDuration = d.LESchedule != null ? d.LESchedule.Days : 0,
                           LECost = Tools.Div(d.Phases.Count() > 0 ? d.Phases
                                .Where(e => e.ActivityType.ToLower().Contains("risk") == false)
                                .DefaultIfEmpty(new BizPlanActivityPhase())
                                .Sum(e => e.LE.Cost) : 0, division),
                           VirtualPhase = d.VirtualPhase,
                           ShiftFutureEventDate = d.ShiftFutureEventDate
                       };
                   })
                   .Where(d => d.OpsFinish >= Today)
                    //.Where(d => dashboardC.dateBetween(new DateRange() { Start = d.PsStartForFilter, Finish = d.PsFinishForFilter }, dateStart, dateFinish, dateStart2, dateFinish2, dateRelation))
                   .OrderBy(d => d.RigName)
                   .ThenBy(d => d.PsStartForFilter)
                   .ToList();
                return final;
            });
        }

        private List<BizPlanSequenceData> PrepareData(SequenceParam param,string from)
        {
            var wellByRole = WebTools.GetWellActivitiesQuery("WellName", "");

            var rigTypes = (param.rigTypes == null ? new List<string>() : param.rigTypes).ToArray();
            var rigNames = (param.rigNames == null ? new List<string>() : param.rigNames).ToArray();
            var projectNames = (param.projectNames == null ? new List<string>() : param.projectNames).ToArray();
            var wellNames = (param.wellNames == null ? new List<string>() : param.wellNames).ToArray();

            var results = new List<BizPlanSequenceData>();
            //Export to excel without filter was CAN
            if (projectNames.Count() == 0 && rigNames.Count() == 0 && from == "browser")
                return results;

            string aggregateCond1 = "{ $unwind: '$Phases' }";
            string aggregateCond2 = "{ $group:{_id:'$RigName', psStartDate:{$min:'$PsSchedule.Start'}, psFinishDate:{$max:'$PsSchedule.Finish'}, phStartDate:{$min:'$Phases.PhSchedule.Start'}, phFinishDate:{$max:'$Phases.PhSchedule.Finish'} } }";
            List<BsonDocument> pipelines = new List<BsonDocument>();
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
            List<BsonDocument> aggregates = DataHelper.Aggregate(new BizPlanActivity().TableName, pipelines);
            //var rigThatHaveProjectNames = new List<string>();
            //var rigThatHaveWellNames = new List<string>();

            var rigCheckFilter = new List<string>();
            if (projectNames.Count() > 0 )
            {
                rigCheckFilter = WellActivity.Populate<WellActivity>(Query.And(Query.In("ProjectName", new BsonArray(projectNames))))
                    .GroupBy(d => d.RigName)
                    .Select(d => d.Key)
                    .ToList();
            }
            
            
            foreach (var aggregate in aggregates)
            {
                String RigName = aggregate.GetString("_id");


                if (rigCheckFilter.Count() > 0)
                {
                    if (!rigCheckFilter.Contains(RigName))
                        continue;
                }
                else
                {
                    if (rigNames.Count() > 0 && !rigNames.Contains(RigName))
                        continue;
                }
                

                IMongoQuery query = null;
                List<IMongoQuery> queryAll = new List<IMongoQuery>();
                queryAll.Add(wellByRole);
                List<BsonValue> rigTypesBson = new List<BsonValue>();
                foreach (var rigType in rigTypes) rigTypesBson.Add(rigType);
                if (rigTypesBson.Count() > 0) queryAll.Add(Query.In("RigType", rigTypesBson));
                queryAll.Add(Query.EQ("RigName", RigName));
                if (queryAll.Count() > 0) query = Query.And(queryAll.ToArray());

                var allActivities = WellActivity.Populate<WellActivity>(query);

                if (!allActivities.Select(d => d.RigName).ToArray().Contains(RigName))
                    continue;

                DateTime PSStartDate = aggregate.GetDateTime("psStartDate");
                DateTime PSFinishDate = aggregate.GetDateTime("psFinishDate");
                DateTime PHStartDate = aggregate.GetDateTime("phStartDate");
                DateTime PHFinishDate = aggregate.GetDateTime("phFinishDate");

                DateTime StartDate = (PSStartDate > PHStartDate ? PHStartDate : PSStartDate);
                DateTime FinishDate = (PSFinishDate < PHFinishDate ? PHFinishDate : PSFinishDate);
                //DateTime StartDate = PSStartDate;
                //DateTime FinishDate = PSFinishDate;

                if (StartDate.Date.Equals(FinishDate.Date))
                {
                    FinishDate = FinishDate.AddMonths(12 - FinishDate.Month);
                }

                var allBizPlan = allActivities.Select(d =>
                {
                    var n = new BizPlanActivity()
                    {
                        _id = d._id.ToString(),
                        Region = d.Region,
                        RigName = d.RigName,
                        OperatingUnit = d.OperatingUnit,
                        ProjectName = d.ProjectName,
                        WellName = d.WellName,
                        UARigSequenceId = d.UARigSequenceId,
                        PsSchedule = d.PsSchedule,
                        Phases = new List<BizPlanActivityPhase>(),
                        VirtualPhase = d.VirtualPhase
                    };

                    var bizPlan = BizPlanActivity.Get<BizPlanActivity>(d._id);
                    if (bizPlan != null)
                    {
                        n = bizPlan;
                    }

                    return n;
                });

                var Activities = allBizPlan
                    .Where(d => d.VirtualPhase != true)
                    .Select(d =>
                    {
                        var phases = d.Phases
                            .Where(e =>
                            {
                                if (param.inlastuploadls)
                                {
                                    return e.isLatestLS && e.ActivityType.ToLower().Contains("risk") == false && e.ActivityType.ToLower().Contains("weather") == false && e.VirtualPhase != true && (param.OPs != null && param.OPs.Count > 0) ? isMatchOP(e.BaseOP, param.OpRelation, param.OPs) : true;
                                }
                                else
                                {
                                    return e.ActivityType.ToLower().Contains("risk") == false && e.ActivityType.ToLower().Contains("weather") == false && e.VirtualPhase != true && (param.OPs != null && param.OPs.Count > 0) ? isMatchOP(e.BaseOP, param.OpRelation, param.OPs) : true;
                                }
                            })
                            .Select(e =>
                            {
                                var calcLESchedule = new DateRange();
                                calcLESchedule.Start = e.Estimate.EstimatePeriod.Start;
                                calcLESchedule.Finish = e.Estimate.EstimatePeriod.Start.AddDays(e.Estimate.NewMean.Days);

                                return new BizPlanSequenceActivityPhase()
                                {
                                    ActivityDesc = e.ActivityDesc,
                                    ActivityType = e.ActivityType,
                                    PhSchedule = e.PlanSchedule,
                                    LESchedule = e.LESchedule,
                                    CalcLESchedule = calcLESchedule
                                };
                            })
                            .ToList<BizPlanSequenceActivityPhase>();

                        return new BizPlanSequenceActivity()
                        {
                            _id = d._id.ToString(),
                            Region = d.Region,
                            RigName = d.RigName,
                            OperatingUnit = d.OperatingUnit,
                            ProjectName = d.ProjectName,
                            WellName = d.WellName,
                            UARigSequenceId = d.UARigSequenceId,
                            PsSchedule = d.PsSchedule,
                            Phases = phases
                        };
                    })
                    .Where(d =>
                    {
                        if (wellNames.Count() > 0)
                        {
                            return (wellNames.Contains(d.WellName));
                        }

                        return true;
                    })
                    .ToList();

                results.Add(new BizPlanSequenceData()
                {
                    RigName = RigName,
                    Activities = Activities,
                    DateRange = new DateRange()
                    {
                        Start = StartDate,
                        Finish = FinishDate
                    }
                });
            }
            #region Rig Types Head
            //var rigThatHaveWellNames = new List<string>();
            //if (wellNames.Count() > 0)
            //    rigThatHaveWellNames = WellActivity.Populate<WellActivity>(Query.In("WellName", new BsonArray(wellNames)))
            //        .GroupBy(d => d.RigName)
            //        .Select(d => d.Key)
            //        .ToList();

            //foreach (var aggregate in aggregates)
            //{
            //    String RigName = aggregate.GetString("_id");

            //    if (rigThatHaveWellNames.Count() > 0)
            //    {
            //        if (rigNames.Count() > 0)
            //        {
            //            if (!rigNames.Contains(RigName))
            //                continue;
            //        }
            //        else
            //        {
            //            if (!rigThatHaveWellNames.Contains(RigName))
            //                continue;
            //        }
            //    }
            //    else
            //    {
            //        if (rigNames.Count() > 0 && !rigNames.Contains(RigName))
            //            continue;
            //    }

            //    IMongoQuery query = null;
            //    List<IMongoQuery> queryAll = new List<IMongoQuery>();
            //    queryAll.Add(wellByRole);
            //    List<BsonValue> rigTypesBson = new List<BsonValue>();
            //    foreach (var rigType in rigTypes) rigTypesBson.Add(rigType);
            //    if (rigTypesBson.Count() > 0) queryAll.Add(Query.In("RigType", rigTypesBson));
            //    queryAll.Add(Query.EQ("RigName", RigName));
            //    if (queryAll.Count() > 0) query = Query.And(queryAll.ToArray());

            //    var allActivities = WellActivity.Populate<WellActivity>(query);

            //    if (!allActivities.Select(d => d.RigName).ToArray().Contains(RigName))
            //        continue;

            //    DateTime PSStartDate = aggregate.GetDateTime("psStartDate");
            //    DateTime PSFinishDate = aggregate.GetDateTime("psFinishDate");
            //    DateTime PHStartDate = aggregate.GetDateTime("phStartDate");
            //    DateTime PHFinishDate = aggregate.GetDateTime("phFinishDate");

            //    DateTime StartDate = (PSStartDate > PHStartDate ? PHStartDate : PSStartDate);
            //    DateTime FinishDate = (PSFinishDate < PHFinishDate ? PHFinishDate : PSFinishDate);
            //    //DateTime StartDate = PSStartDate;
            //    //DateTime FinishDate = PSFinishDate;

            //    if (StartDate.Date.Equals(FinishDate.Date))
            //    {
            //        FinishDate = FinishDate.AddMonths(12 - FinishDate.Month);
            //    }

            //    var allBizPlan = allActivities.Select(d =>
            //    {
            //        var n = new BizPlanActivity()
            //        {
            //            _id = d._id.ToString(),
            //            Region = d.Region,
            //            RigName = d.RigName,
            //            OperatingUnit = d.OperatingUnit,
            //            ProjectName = d.ProjectName,
            //            WellName = d.WellName,
            //            UARigSequenceId = d.UARigSequenceId,
            //            PsSchedule = d.PsSchedule,
            //            Phases = new List<BizPlanActivityPhase>(),
            //            VirtualPhase = d.VirtualPhase
            //        };

            //        var bizPlan = BizPlanActivity.Get<BizPlanActivity>(d._id);
            //        if (bizPlan != null)
            //        {
            //            n = bizPlan;
            //        }

            //        return n;
            //    });

            //    var Activities = allBizPlan
            //        .Where(d => d.VirtualPhase != true)
            //        .Select(d =>
            //        {
            //            var phases = d.Phases
            //                .Where(e =>
            //                {
            //                    if (param.inlastuploadls)
            //                    {
            //                        return e.isLatestLS && e.ActivityType.ToLower().Contains("risk") == false && e.ActivityType.ToLower().Contains("weather") == false && e.VirtualPhase != true;
            //                    }
            //                    else
            //                    {
            //                        return e.ActivityType.ToLower().Contains("risk") == false && e.ActivityType.ToLower().Contains("weather") == false && e.VirtualPhase != true;
            //                    }
            //                })
            //                .Select(e =>
            //                {
            //                    var calcLESchedule = new DateRange();
            //                    calcLESchedule.Start = e.Estimate.EstimatePeriod.Start;
            //                    calcLESchedule.Finish = e.Estimate.EstimatePeriod.Start.AddDays(e.Estimate.NewMean.Days);

            //                    return new BizPlanSequenceActivityPhase()
            //                    {
            //                        ActivityDesc = e.ActivityDesc,
            //                        ActivityType = e.ActivityType,
            //                        PhSchedule = e.PlanSchedule,
            //                        LESchedule = e.LESchedule,
            //                        CalcLESchedule = calcLESchedule
            //                    };
            //                })
            //                .ToList<BizPlanSequenceActivityPhase>();

            //            return new BizPlanSequenceActivity()
            //            {
            //                _id = d._id.ToString(),
            //                Region = d.Region,
            //                RigName = d.RigName,
            //                OperatingUnit = d.OperatingUnit,
            //                ProjectName = d.ProjectName,
            //                WellName = d.WellName,
            //                UARigSequenceId = d.UARigSequenceId,
            //                PsSchedule = d.PsSchedule,
            //                Phases = phases
            //            };
            //        })
            //        .Where(d =>
            //        {
            //            if (wellNames.Count() > 0)
            //            {
            //                return (wellNames.Contains(d.WellName));
            //            }

            //            return true;
            //        })
            //        .ToList();

            //    results.Add(new BizPlanSequenceData()
            //    {
            //        RigName = RigName,
            //        Activities = Activities,
            //        DateRange = new DateRange()
            //        {
            //            Start = StartDate,
            //            Finish = FinishDate
            //        }
            //    });
            //}
            #endregion            


            return results.OrderBy(x=>x.RigName).ToList();
        }

        private bool isMatchOP(List<string> BasePhaseOP, string opRelation, List<string> OPs)
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

        public JsonResult GetData(SequenceParam param)
        {
            
            return MvcResultInfo.Execute(null, (BsonDocument doc) => new { Items = PrepareData(param,"browser") });
        }

        public FileResult GetExcel(SequenceParamRaw paramRaw)
        {
            var param = new SequenceParam()
            {
                //rigTypes = (paramRaw.rigTypes.Equals("") ? new List<string>() : paramRaw.rigTypes.Split(',').OrderBy(d => d).ToList<string>()),
                projectNames = (paramRaw.projectNames.Equals("") ? new List<string>() : paramRaw.projectNames.Split(',').OrderBy(d => d).ToList<string>()),
                rigNames = (paramRaw.rigNames.Equals("") ? new List<string>() : paramRaw.rigNames.Split(',').OrderBy(d => d).ToList<string>()),
                wellNames = (paramRaw.wellNames.Equals("") ? new List<string>() : paramRaw.wellNames.Split(',').OrderBy(d => d).ToList<string>()),
                historicalData = paramRaw.historicalData,
                isCalendarModeSameMode = (paramRaw.isCalendarModeSameMode == 1)
            };

            var data = PrepareData(param,"excel");
            string fileName = new SequenceHelper(data, param).Execute(this);
            string fileNameDownload = "SequenceChart-" + DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + ".xlsx";

            return File(fileName, Tools.GetContentType(".xlsx"), fileNameDownload);
        }
	}

    public class BizPlanSequenceData
    {
        public string RigName { set; get; }
        public List<BizPlanSequenceActivity> Activities { set; get; }
        public DateRange DateRange { set; get; }
    }

    public class BizPlanSequenceActivity
    {
        public string _id { set; get; }
        public string Region { set; get; }
        public string RigName { set; get; }
        public string OperatingUnit { set; get; }
        public string ProjectName { set; get; }
        public string WellName { set; get; }
        public string UARigSequenceId { set; get; }
        public DateRange PsSchedule { set; get; }
        public List<BizPlanSequenceActivityPhase> Phases { set; get; }
    }

    public class BizPlanSequenceActivityPhase
    {
        public string ActivityDesc { set; get; }
        public string ActivityType { set; get; }
        public DateRange PhSchedule { set; get; }
        public DateRange LESchedule { set; get; }
        public DateRange CalcLESchedule { set; get; }
        public DateRange LWESchedule { set; get; }
    }
}