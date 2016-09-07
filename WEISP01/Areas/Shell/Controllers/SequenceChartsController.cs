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
    [ECAuth()]
    public class SequenceChartsController : Controller
    {
        public ActionResult Index()
        {
            string aggregateCond1 = "{ $unwind: '$Phases' }";
            string aggregateCond2 = "{ $group:{_id:'$RigName', psStartDate:{$min:'$PsSchedule.Start'}, psFinishDate:{$max:'$PsSchedule.Finish'}, phStartDate:{$min:'$Phases.PhSchedule.Start'}, phFinishDate:{$max:'$Phases.PhSchedule.Finish'} } }";
            List<BsonDocument> pipelines = new List<BsonDocument>();
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));

            var result = DataHelper.Aggregate("WEISWellActivities", pipelines)
                .Select(d => new {
                    RigName = d.GetString("_id"),
                    PSStartDate = d.GetDateTime("psStartDate"),
                    PSFinishDate = d.GetDateTime("psFinishDate"),
                    PHStartDate = d.GetDateTime("phStartDate"),
                    PHFinishDate = d.GetDateTime("phFinishDate")
                })
                //.Where(d => { var now = DateTime.Now; return (now <= d.PHFinishDate && now >= d.PHStartDate); })
                .Select(d => d.RigName)
                .OrderBy(d => d);

            ViewBag.CurrentActiveRig = JsonConvert.SerializeObject(result);

            return View();
        }

        private List<SequenceData> PrepareData(SequenceParam param)
        {
            var wellByRole = WebTools.GetWellActivitiesQuery("WellName", "");

            var rigTypes = (param.rigTypes == null ? new List<string>() : param.rigTypes).ToArray();
            var rigNames = (param.rigNames == null ? new List<string>() : param.rigNames).ToArray();
            var projectNames = (param.projectNames== null ? new List<string>() : param.projectNames).ToArray();
            var wellNames = (param.wellNames == null ? new List<string>() : param.wellNames).ToArray();

            List<SequenceData> results = new List<SequenceData>();

            //if (rigTypes.Count() == 0 && rigNames.Count() == 0 && wellNames.Count() == 0)
            //    return results;

            string aggregateCond1 = "{ $unwind: '$Phases' }";
            string aggregateCond2 = "{ $group:{_id:'$RigName', psStartDate:{$min:'$PsSchedule.Start'}, psFinishDate:{$max:'$PsSchedule.Finish'}, phStartDate:{$min:'$Phases.PhSchedule.Start'}, phFinishDate:{$max:'$Phases.PhSchedule.Finish'} } }";
            List<BsonDocument> pipelines = new List<BsonDocument>();
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
            List<BsonDocument> aggregates = DataHelper.Aggregate("WEISWellActivities", pipelines);

            //List<BsonDocument> aggregates = new List<BsonDocument>();
            //var MinPsStartDate = new DateTime(DateTime.Now.Year,1,1);
            //var MinPhStartDate = new DateTime(DateTime.Now.Year, 1, 1);
            //if (aggregates2.Any())
            //{
            //    foreach (var agg in aggregates2)
            //    {
            //        BsonDocument doc = new BsonDocument();
            //        if (agg.GetDateTime("psStartDate").Year == DateTime.Now.Year)
            //        {

            //        }
            //        doc.Set("_id", agg.GetString("_id"));
            //        doc.Set("psStartDate", agg.GetDateTime("psStartDate"));
            //        doc.Set("phStartDate", agg.GetDateTime("phStartDate"));
            //        if (agg.GetDateTime("psFinishDate") >= agg.GetDateTime("psStartDate").AddYears(5))
            //        {
                        
            //            doc.Set("psFinishDate", agg.GetDateTime("psStartDate").AddYears(5));
            //        }
            //        else
            //        {
            //            doc.Set("psFinishDate", agg.GetDateTime("psFinishDate"));
            //        }
            //        if (agg.GetDateTime("phFinishDate") >= agg.GetDateTime("phStartDate").AddYears(5))
            //        {

            //            doc.Set("phFinishDate", agg.GetDateTime("phStartDate").AddYears(5));
            //        }
            //        else
            //        {
            //            doc.Set("phFinishDate", agg.GetDateTime("phFinishDate"));
            //        }
            //        aggregates.Add(doc);
            //    }
            //}


            //var rigThatHaveWellNames = new List<string>();
            //if (wellNames.Count() > 0)
            //    rigThatHaveWellNames = WellActivity.Populate<WellActivity>(Query.In("WellName", new BsonArray(wellNames)))
            //        .GroupBy(d => d.RigName)
            //        .Select(d => d.Key)
            //        .ToList();

            var rigCheckFilter = new List<string>();
            if (projectNames.Count() > 0 && wellNames.Count() > 0)
            {
                rigCheckFilter = WellActivity.Populate<WellActivity>(Query.And(Query.In("ProjectName", new BsonArray(projectNames))))
                    .GroupBy(d => d.RigName)
                    .Select(d => d.Key)
                    .ToList();
            }
            

            foreach (var aggregate in aggregates)
            {
                String RigName = aggregate.GetString("_id");

                //if (rigThatHaveWellNames.Count() > 0)
                //{
                //    if (!rigThatHaveWellNames.Contains(RigName))
                //        continue;
                //}
                //else
                //{
                //    if (rigNames.Count() > 0 && !rigNames.Contains(RigName))
                //        continue;
                //}
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

                var Activities = allActivities
                    .Where(d => d.VirtualPhase != true)
                    .Select(d => new WellActivity() {
                        _id = d._id.ToString(),
                        Region = d.Region,
                        RigName = d.RigName,
                        OperatingUnit = d.OperatingUnit,
                        ProjectName = d.ProjectName,
                        WellName = d.WellName,
                        UARigSequenceId = d.UARigSequenceId,
                        PsSchedule = d.PsSchedule,
                        OpsSchedule = d.OpsSchedule,
                        Phases = d.Phases
                            .Where(e => e.ActivityType.ToLower().Contains("risk") == false && e.VirtualPhase != true && (param.OPs != null && param.OPs.Count > 0) ? isMatchOP(e.BaseOP, param.OpRelation, param.OPs) : true)
                            .Select(e => new WellActivityPhase()
                            {
                                ActivityDesc = e.ActivityDesc,
                                ActivityType = e.ActivityType,
                                PhSchedule = e.PhSchedule,
                                LESchedule = e.LESchedule,
                                LWESchedule = e.LWESchedule
                            })
                            .ToList<WellActivityPhase>()
                    })
                    .Where(d =>
                    {
                        if (wellNames.Count() > 0)
                        {
                            return (wellNames.Contains(d.WellName));
                        }

                        return true;
                        //var Now = DateTime.Now;
                        //var MinDate = d.Phases.Min(e => e.PhSchedule.Start);
                        //var MaxDate = d.Phases.Max(e => e.PhSchedule.Finish);

                        //return (MaxDate >= Now || d.PsSchedule.Finish >= Now);
                    })
                    .ToList();

                List<BsonValue> SequenceIds = new List<BsonValue>();
                foreach (var a in Activities)
                    SequenceIds.Add(a.UARigSequenceId);

                var ActivitySequences = WellActivityUpdate.Populate<WellActivityUpdate>(Query.In("SequenceId", SequenceIds));

                results.Add(new SequenceData()
                {
                    RigName = RigName,
                    Activities = Activities,
                    //ActivitiySequences = ActivitySequences,
                    DateRange = new DateRange()
                    {
                        Start = StartDate,
                        Finish = FinishDate
                    }
                });
            }

            return results.OrderBy(x=>x.RigName).ToList();
        }
        private List<SequenceData2> PrepareData2(SequenceParam param)
        {
            var wellByRole = WebTools.GetWellActivitiesQuery("WellName", "");

            var rigTypes = (param.rigTypes == null ? new List<string>() : param.rigTypes).ToArray();
            var rigNames = (param.rigNames == null ? new List<string>() : param.rigNames).ToArray();
            var projectNames = (param.projectNames == null ? new List<string>() : param.projectNames).ToArray();
            var wellNames = (param.wellNames == null ? new List<string>() : param.wellNames).ToArray();

            List<SequenceData2> results = new List<SequenceData2>();

            if (projectNames.Count() == 0 && rigNames.Count() == 0 )
                return results;

            string aggregateCond1 = "{ $unwind: '$Phases' }";
            string aggregateCond2 = "{ $group:{_id:'$RigName', psStartDate:{$min:'$PsSchedule.Start'}, psFinishDate:{$max:'$PsSchedule.Finish'}, phStartDate:{$min:'$Phases.PhSchedule.Start'}, phFinishDate:{$max:'$Phases.PhSchedule.Finish'} } }";
            List<BsonDocument> pipelines = new List<BsonDocument>();
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond2));
            List<BsonDocument> aggregates = DataHelper.Aggregate("WEISWellActivities", pipelines);

            //var rigThatHaveWellNames = new List<string>();
            //if (wellNames.Count() > 0)
            //    rigThatHaveWellNames = WellActivity.Populate<WellActivity>(Query.In("WellName", new BsonArray(wellNames)))
            //        .GroupBy(d => d.RigName)
            //        .Select(d => d.Key)
            //        .ToList();
            var rigCheckFilter = new List<string>();
            if (projectNames.Count() > 0)
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

                var Activities = allActivities
                    .Where(d => d.VirtualPhase != true)
                    .Select(d => new WellActivity()
                    {
                        _id = d._id.ToString(),
                        Region = d.Region,
                        RigName = d.RigName,
                        OperatingUnit = d.OperatingUnit,
                        ProjectName = d.ProjectName,
                        WellName = d.WellName,
                        UARigSequenceId = d.UARigSequenceId,
                        PsSchedule = d.PsSchedule,
                        OpsSchedule = d.OpsSchedule,
                        Phases = d.Phases
                            .Where(e => e.ActivityType.ToLower().Contains("risk") == false && e.VirtualPhase != true && (param.OPs != null && param.OPs.Count>0)? isMatchOP(e.BaseOP,param.OpRelation,param.OPs):true)
                            .Select(e => new WellActivityPhase()
                            {
                                ActivityDesc = e.ActivityDesc,
                                ActivityType = e.ActivityType,
                                PhSchedule = e.PhSchedule,
                                LESchedule = e.LESchedule,
                                LWESchedule = e.LWESchedule
                            })
                            .ToList<WellActivityPhase>()
                    })
                    .Where(d =>
                    {
                        if (wellNames.Count() > 0)
                        {
                            return (wellNames.Contains(d.WellName));
                        }

                        return true;
                        //var Now = DateTime.Now;
                        //var MinDate = d.Phases.Min(e => e.PhSchedule.Start);
                        //var MaxDate = d.Phases.Max(e => e.PhSchedule.Finish);

                        //return (MaxDate >= Now || d.PsSchedule.Finish >= Now);
                    })
                    .ToList();

                List<BsonValue> SequenceIds = new List<BsonValue>();
                foreach (var a in Activities)
                    SequenceIds.Add(a.UARigSequenceId);

                var ActivitySequences = WellActivityUpdate.Populate<WellActivityUpdate>(Query.In("SequenceId", SequenceIds));

                var Acts = Activities.Select(x => x.ToBsonDocument()).ToList();
                foreach (var act in Acts)
                {
                    act.Remove("ActivityType");
                    act.Remove("AssetName");
                    act.Remove("AssignTOOps");
                    act.Remove("EXType");
                    act.Remove("FirmOrOption");
                    act.Remove("IntId");
                    act.Remove("LeadEngineer");
                    act.Remove("LeadEngineerEmail");
                    act.Remove("NonOP");
                    act.Remove("OPHistories");
                    act.Remove("OperatingUnit");
                    act.Remove("OptimizationEngineer");
                    act.Remove("OptimizationEngineerEmail");
                    act.Remove("PerformanceUnit");
                    act.Remove("PhaseNo");
                    act.Remove("ProjectName");
                    act.Remove("Region");
                    act.Remove("StringId");
                    act.Remove("TableName");
                    act.Remove("Tags");
                    act.Remove("TeamLead");
                    act.Remove("Targets");
                    act.Remove("TeamLeadEmail");
                    act.Remove("Title");
                    act.Remove("UARigDescription");
                    act.Remove("WorkingInterest");
                    var phases_ = act.Get("Phases").AsBsonArray;
                    var phases = phases_.Select(x => x.ToBsonDocument());
                    var bsonDocList = new List<BsonDocument>();
                    foreach (var phase in phases)
                    {

                        var a = phase.Get("PhSchedule");
                        var b = phase.Get("ActivityType");
                        var c = phase.Get("LESchedule");
                        var d = phase.Get("LWESchedule");
                        
                        bsonDocList.Add(new BsonDocument()
                            .Set("PhSchedule", a)
                            .Set("ActivityType", b)
                            .Set("LESchedule", c)
                            .Set("LWESchedule", d)
                            );
                    }
                    act.Remove("Phases");
                    act.Set("Phases", new BsonArray(bsonDocList));
                }

                results.Add(new SequenceData2()
                {
                    RigName = RigName,
                    Activities = Acts.Select(x => x.ToDictionary()).ToList(),
                    //ActivitiySequences = ActivitySequences,
                    DateRange = new DateRange()
                    {
                        Start = StartDate,
                        Finish = FinishDate
                    }
                });
            }

            return results;
        }

        private bool isMatchOP(List<string> BasePhaseOP,string opRelation,List<string> OPs)
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
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                return new
                {
                    Items = PrepareData2(param)
                };
            });
        }
        
        public FileResult GetExcel(SequenceParamRaw paramRaw)
        {
            var param = new SequenceParam()
            {
                projectNames = (paramRaw.projectNames.Equals("") ? new List<string>() : paramRaw.projectNames.Split(',').OrderBy(d => d).ToList<string>()),
                rigNames = (paramRaw.rigNames.Equals("") ? new List<string>() : paramRaw.rigNames.Split(',').OrderBy(d => d).ToList<string>()),
                wellNames = (paramRaw.wellNames.Equals("") ? new List<string>() : paramRaw.wellNames.Split(',').OrderBy(d => d).ToList<string>()),
                historicalData = paramRaw.historicalData,
                isCalendarModeSameMode = (paramRaw.isCalendarModeSameMode == 1)
            };

            var data = PrepareData(param);
            string fileName = new SequenceHelper(data, param).Execute(this);
            string fileNameDownload = "SequenceChart-" + DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + ".xlsx";

            return File(fileName, Tools.GetContentType(".xlsx"), fileNameDownload);
        }

        public JsonResult UpdateLESchedule()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                WellActivity.UpdateLESchedule();
                return true;
            });
        }
    }

    public class SequenceParamRaw
    {
        public string rigTypes { set; get; }
        public string projectNames { get; set; }
        public string rigNames { set; get; }
        public string wellNames { set; get; }
        public int historicalData { set; get; }
        public int isCalendarModeSameMode { set; get; }

        public SequenceParamRaw()
        {
            rigTypes = "";
            projectNames = "";
            rigNames = "";
            wellNames = "";
            historicalData = 3;
            isCalendarModeSameMode = 0;
        }
    }

    public class SequenceParam
    {
        public List<string> rigTypes { set; get; }
        public List<string> projectNames { get; set; }
        public List<string> rigNames { set; get; }
        public List<string> wellNames { set; get; }
        public List<string> OPs { set; get; }
        public string OpRelation { get; set; }
        public int historicalData { set; get; }
        public bool isCalendarModeSameMode { set; get; }
        public bool inlastuploadls { set; get; }        
    }

    class SequencePhase
    {
        public string ActivityDesc { set; get; }
        public string ActivityType { set; get; }
        public DateRange PhSchedule { set; get; }
        public DateRange LESchedule { set; get; }
        public DateRange LWESchedule { set; get; }
    }

    public class SequenceData
    {
        public string RigName { set; get; }
        public List<WellActivity> Activities { set; get; }
        public List<WellActivityUpdate> ActivitiySequences { set; get; }
        public DateRange DateRange { set; get; }
    }

    public class SequenceData2
    {
        public string RigName { set; get; }
        public List<Dictionary<string, object>> Activities { set; get; }
        public List<WellActivityUpdate> ActivitiySequences { set; get; }
        public DateRange DateRange { set; get; }
    }
    class SequenceMargin
    {
        public int Left { set; get; }
        public int Width { set; get; }
    }

    class SequenceHelper
    {
        public DateRange ConstDate { set; get; }
        private SequenceParam Param { set; get; }
        private List<SequenceData> Data { set; get; }
        private Workbook Book { set; get; }
        private Dictionary<string, System.Drawing.Color> cacheColorForOP { set; get; }
        private int colIndex { set; get; }
        private DateTime dateNull { get { return new DateTime(1900, 1, 1).Date; } }
        private List<string> baseColors { get { return (new string[] { "#45B29D", "#EFC94C", "#29BAD9", "#E2793F", "#334D5C", "#DF5A49", "#AFC034", "#E68074", "#4793DE", "#623029", "#BB496B", "#4D315A", "#9394EE", "#6769E2", "#C08038" }).ToList(); } }
        private string SequenceTarget { set; get; }
        private string Template { set; get; }

        private const int baseIndex = 6;
        private const int baseRowHeight = 18;
        private const int baseColWidth = 4;
        private const int baseTopOffset = 7;
        private const int baseEachOffset = 8;

        public SequenceHelper(List<SequenceData> data, SequenceParam param)
        {
            Data = data;
            Param = param;
            ConstDate = new DateRange() { Start = dateNull, Finish = dateNull };
            Book = new Workbook();
            colIndex = 0;
            SequenceTarget = "DataBrowser";
            Template = "sequencecharttemplate.xlsx";
        }

        public SequenceHelper(List<BizPlanSequenceData> data, SequenceParam param)
        {
            Data = data.Select(d =>
            {
                d.Activities.ForEach(e =>
                {
                    if (e.Phases != null && e.Phases.Count > 0)
                    {
                        e.Phases.ForEach(f =>
                        {
                            f.LWESchedule = f.CalcLESchedule;
                        });
                    }
                });

                return BsonSerializer.Deserialize<SequenceData>(d.ToBsonDocument());
            }).ToList();

            Param = param;
            ConstDate = new DateRange() { Start = dateNull, Finish = dateNull };
            Book = new Workbook();
            colIndex = 0;
            SequenceTarget = "BizPlan";
            Template = "bizplansequencecharttemplate2.xlsx";
        }

        public string Execute(Controller controller)
        {
            //if (Param.rigTypes.Count > 0)
            //    Data = Data.OrderBy(d => d.RigName).ToList();
            if (Param.rigNames.Count > 0)
                Data = Data.OrderBy(d => d.RigName).ToList();


            //if (param.rigNames.Count > 0)
            //    data = data.OrderBy(d => d.RigName).ToList();
            //    items = reorderItems(items, getFilterValues().rigNames);
            
            if (Param.isCalendarModeSameMode)
                GetDateStartFinishFromAllRigs(Data);

            //string fileName = string.Join("", Param.rigTypes) + string.Join("", Param.rigNames) + string.Join("", Param.wellNames);
            string fileName = string.Join("", Param.rigNames) + string.Join("", Param.projectNames) + string.Join("", Param.wellNames);
            string template = Path.Combine(controller.Server.MapPath("~/App_Data/Temp"), Template);
            Workbook workbookTemplate = new Workbook(template);
            var worksheetTemplate = workbookTemplate.Worksheets.FirstOrDefault();

            if (System.IO.File.Exists(template) == false) 
                throw new Exception("Template file is not exist: " + template);

            Book = new Workbook();
            int sheet = 0;
            var currentDate = DateTime.Now.ToString("MMM, dd yyyy");

            Book.Worksheets.First().Cells["E4"].Value = currentDate;
            Book.Worksheets.First().Name = currentDate;
            Book.Worksheets.First().Copy(worksheetTemplate);
            Book.Worksheets.First().ActiveCell = "E3";

            Range range = Book.Worksheets.First().Cells.CreateRange("B" + GetTopIndex(0), "E" + GetTopIndex(0, baseEachOffset - 1));

            foreach (var each in Data)
            {
                if (sheet > 0)
                {
                    var startRange = "B" + GetTopIndex(sheet);
                    var finishRange = "E" + GetTopIndex(sheet, baseEachOffset - 1);
                    Range newRange = Book.Worksheets.First().Cells.CreateRange(startRange, finishRange);
                    newRange.CopyStyle(range);
                    newRange.CopyData(range);
                }

                colIndex = 0;
                cacheColorForOP = new Dictionary<string, System.Drawing.Color>();

                Book.Worksheets.First().Cells["B" + GetTopIndex(sheet)].Value = each.RigName;

                if (!Param.isCalendarModeSameMode)
                {
                    ConstDate = GetDateStartFinish(each, true);
                    TrimByHistoricalDate();
                }
                
                AppendDateHeader(sheet);
                AppendDataContent(sheet);
                FixCellsHeight(sheet);

                sheet++;
            }

            if (SequenceTarget.Equals("BizPlan"))
            {
                var firstRowNeedToBeDeleted = 10;
                var now = DateTime.Now;
                var thisWeek = now.AddDays(DayOfWeek.Monday - now.DayOfWeek);

                foreach (var each in Data)
                {
                    Book.Worksheets.First().Cells.DeleteRow(firstRowNeedToBeDeleted);
                    Book.Worksheets.First().Cells.DeleteRow(firstRowNeedToBeDeleted);

                    Book.Worksheets.First().Cells["B" + (firstRowNeedToBeDeleted + 1)].Value = "Current LE (" + thisWeek.ToString("MMM-dd, yyyy") + ")";

                    firstRowNeedToBeDeleted += 6;
                }
            }

            string path1 = controller.Server.MapPath("~/App_Data/Temp");
            string path2 = String.Format("SC-{0}-{1}.xlsx", fileName, DateTime.Now.ToString("dd-MMM-yyyy"));
            string newFileName = Path.Combine(path1, path2);

            Book.Save(newFileName, Aspose.Cells.SaveFormat.Xlsx);

            return newFileName;
        }
        
        private int GetTopIndex(int sheet, int space = 0)
        {
            return (baseTopOffset + (baseEachOffset * sheet)) + space;
        }

        private DateTime GetClosestDateOnMonday(DateTime startDate) {
            var firstDate = startDate.Date;
            var days = new string[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }.ToList<string>();
            var dayString = firstDate.ToString("dddd");
            var firstDayIndex = days.IndexOf(dayString);

            if (firstDayIndex <= 0) 
                return firstDate.Date;

            firstDate = firstDate.AddDays(days.Count - firstDayIndex);

            return firstDate.Date;
        }

        private DateRange GetDateStartFinish(SequenceData data, bool resetStartDate = false) {
            DateTime now = DateTime.Now.Date;
            DateTime startDate = dateNull;
            DateTime finishDate = dateNull;

            foreach (var o in data.Activities)
            {
                var psSchedule = o.PsSchedule;
                var isValidPsDate = ((psSchedule.Finish.Date - psSchedule.Start.Date).Days > 0);

                
                if (isValidPsDate)
                {
                    if (startDate.Date == dateNull || psSchedule.Start.Date < startDate.Date)
                        startDate = psSchedule.Start.Date;

                    if (finishDate.Date == dateNull || psSchedule.Finish.Date > finishDate.Date)
                        finishDate = psSchedule.Finish.Date;
                }

                foreach (var p in o.Phases)
                {
                    var phSchedule = p.PhSchedule;
                    var isValidPhDate = ((phSchedule.Finish.Date - phSchedule.Start.Date).Days > 0);

                    if (isValidPhDate)
                    {
                        if (startDate.Date == dateNull || phSchedule.Start.Date < startDate.Date)
                            startDate = phSchedule.Start.Date;

                        if (finishDate.Date == dateNull || phSchedule.Finish.Date > finishDate.Date)
                            finishDate = phSchedule.Finish.Date;


                    }

                    if (SequenceTarget.Equals("BizPlan"))
                    {
                        var leSchedule = p.LESchedule;
                        var isValidLEDate = ((leSchedule.Finish.Date - leSchedule.Start.Date).Days > 0);

                        if (isValidLEDate)
                        {
                            if (startDate.Date == dateNull || leSchedule.Start.Date < startDate.Date)
                                startDate = leSchedule.Start.Date;

                            if (finishDate.Date == dateNull || leSchedule.Finish.Date > finishDate.Date)
                                finishDate = leSchedule.Finish.Date;
                        }

                        var calcLESchedule = p.LWESchedule;
                        var isValidCalcLEDate = ((calcLESchedule.Finish.Date - calcLESchedule.Start.Date).Days > 0);

                        if (isValidCalcLEDate)
                        {
                            if (startDate.Date == dateNull || calcLESchedule.Start.Date < startDate.Date)
                                startDate = calcLESchedule.Start.Date;

                            if (finishDate.Date == dateNull || calcLESchedule.Finish.Date > finishDate.Date)
                                finishDate = calcLESchedule.Finish.Date;
                        }
                    }
                }
                    
                
            }

            if (startDate.Date == dateNull && finishDate.Date == dateNull)
            {
                startDate = now.Date;
                finishDate = now.Date;
            }

            var res = new DateRange ()
            {
                Start = GetClosestDateOnMonday(startDate).Date,
                Finish = new DateTime(finishDate.Date.Year, finishDate.Date.Month, 1).AddMonths(2).AddDays(-1)
            };

            if (resetStartDate)
                res.Start = GetClosestDateOnMonday(new DateTime(res.Start.Year, res.Start.Month, 1)).Date;

            return res;
        }

        private string GetDateStartFinishFormattedValue(DateRange date, string format)
        {
            return "(" + date.Start.Date.ToString(format) + " - " + date.Finish.Date.ToString(format) + ")";
        }

        private void TrimByHistoricalDate() {
            var now = DateTime.Now.Date;
            var dateBefore = ConstDate;
            var monthOffset = ((dateBefore.Start.Date.Year - now.Date.Year) * 12) + dateBefore.Start.Date.Month - now.Date.Month;

            if (!(Param.historicalData > 0 && monthOffset < 1))
                return;

            var dateAfter = dateBefore.Start.Date;

            var ymToday = Convert.ToInt64(now.ToString("yyyyMM"));
            var ymStart = Convert.ToInt64(dateBefore.Start.ToString("yyyyMM"));

            if (ymToday > ymStart)
                dateAfter = now;

            dateAfter = new DateTime(dateAfter.Year, dateAfter.Month, 1).AddMonths(-Param.historicalData);
            ConstDate.Start = GetClosestDateOnMonday(dateAfter).Date;
        }

        private void GetDateStartFinishFromAllRigs(List<SequenceData> data)
        {
            var now = DateTime.Now.Date;
            var date = ConstDate;
            var i = 0;

            foreach (var item in data)
            {
                var dateStartFinish = GetDateStartFinish(item);
                
                if (i == 0)
                    date = dateStartFinish;
                
                if (date.Start.Date > dateStartFinish.Start.Date)
                    date.Start = dateStartFinish.Start.Date;
                if (date.Finish.Date < dateStartFinish.Finish.Date)
                    date.Finish = dateStartFinish.Finish.Date;

                i++;
            }

            if (i == 0)
                date = new DateRange() { Start = now, Finish = now };

            ConstDate = date;

            TrimByHistoricalDate();
        }

        private int GetTotalWeeksBetween(DateRange date)
        {
            var lastDate = date.Finish.Date;
            var firstDate = GetClosestDateOnMonday(date.Start.Date);
            var manyWeeks = 0;

            while (firstDate.Date <= lastDate.Date) {
                firstDate = firstDate.AddDays(7);
                manyWeeks++;
            }

            return manyWeeks;
        }

        private string GetColNameFromIndex(int columnNumber)
        {
            int dividend = columnNumber;
            string columnName = String.Empty;
            int modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }

            return columnName;
        }

        public int GetColNumberFromName(string columnName)
        {
            char[] characters = columnName.ToUpperInvariant().ToCharArray();
            int sum = 0;
            for (int i = 0; i < characters.Length; i++)
            {
                sum *= 26;
                sum += (characters[i] - 'A' + 1);
            }
            return sum;
        }

        private Style StyleDateCell(int sheet, string position)
        {
            Style style = Book.Worksheets.First().Cells[position].GetStyle();

            style.HorizontalAlignment = TextAlignmentType.Center;
            style.VerticalAlignment = TextAlignmentType.Center;

            style.Borders[BorderType.TopBorder].LineStyle = CellBorderType.Thin;
            style.Borders[BorderType.TopBorder].Color = System.Drawing.Color.Black;

            style.Borders[BorderType.BottomBorder].LineStyle = CellBorderType.Thin;
            style.Borders[BorderType.BottomBorder].Color = System.Drawing.Color.Black;

            style.Borders[BorderType.LeftBorder].LineStyle = CellBorderType.Thin;
            style.Borders[BorderType.LeftBorder].Color = System.Drawing.Color.Black;

            style.Borders[BorderType.RightBorder].LineStyle = CellBorderType.Thin;
            style.Borders[BorderType.RightBorder].Color = System.Drawing.Color.Black;

            Book.Worksheets.First().Cells[position].SetStyle(style);

            return style;
        }

        private void AppendDateHeader(int sheet)
        {
            var date = ConstDate;
            var iterableDate = GetClosestDateOnMonday(date.Start.Date);
            var firstDateForChart = date.Start;

            for (var i = date.Start.Year; i <= date.Finish.Year; i++)
            {
                var isStartYear = (i == date.Start.Year);
                var isFinishYear = (i == date.Finish.Year);

                var startMonth = (isStartYear ? date.Start.Month : 1);
                var finishMonth = (isFinishYear ? date.Finish.Month : 12);

                var colIndexStartForYear = colIndex;
                var howManyWeekEeachYear = 0;

                for (var j = startMonth; j <= finishMonth; j++)
                {
                    var isStartMonth = (j == date.Start.Month);
                    var isFinishMonth = (j == date.Finish.Month);

                    #region dear future self, please read this what's in this region
                    //var lastDayOfCurrentMonthBetweenDateRange = parseInt(moment(i + '-' + (j + 1), 'YYYY-M').endOf('month').format('D'));
                    //var startDay = ((isStartYear && isStartMonth) ? startDate.getDate() : 1);
                    //startDay = 1; // hack, show start from first week instead
                    //var finishDay = ((isFinishYear && isFinishMonth) ? finishDate.getDate() : lastDayOfCurrentMonthBetweenDateRange);
                    //finishDay = lastDayOfCurrentMonthBetweenDateRange; // hack, coz using lastDayOfCurrentMonthBetweenDateRange causing some ui bug
                    #endregion

                    var startDay = 1;
                    var finishDay = new DateTime(i, j, 1).AddMonths(1).AddDays(-1).Day;

                    var howManyWeek = GetTotalWeeksBetween(new DateRange()
                    {
                        Start = new DateTime(i, j, startDay),
                        Finish = new DateTime(i, j, finishDay)
                    });

                    var colIndexStartForWeek = colIndex;

                    for (var k = 0; k < howManyWeek; k++)
                    {
                        var colIndexWeek = GetColNameFromIndex(baseIndex + colIndex);
                        Book.Worksheets.First().Cells[colIndexWeek + GetTopIndex(sheet, 2)].Value = iterableDate.Day.ToString();
                        StyleDateCell(sheet, colIndexWeek + GetTopIndex(sheet, 2));
                        StyleDateCell(sheet, colIndexWeek + GetTopIndex(sheet, 1));
                        StyleDateCell(sheet, colIndexWeek + GetTopIndex(sheet));

                        iterableDate = iterableDate.AddDays(7);
                        colIndex++;
                    }
                    
                    var cellPositionMonth = GetColNameFromIndex(baseIndex + colIndexStartForWeek) + GetTopIndex(sheet, 1);
                    Book.Worksheets.First().Cells.Merge(GetTopIndex(sheet, 1) - 1, baseIndex + colIndexStartForWeek - 1, 1, howManyWeek);
                    Book.Worksheets.First().Cells[cellPositionMonth].Value = new DateTime(1990, j, 1).ToString("MMM");

                    StyleDateCell(sheet, cellPositionMonth);

                    howManyWeekEeachYear += howManyWeek;
                }

                var cellPositionYear = GetColNameFromIndex(baseIndex + colIndexStartForYear) + GetTopIndex(sheet);
                Book.Worksheets.First().Cells.Merge(GetTopIndex(sheet) - 1, baseIndex + colIndexStartForYear - 1, 1, howManyWeekEeachYear);
                Book.Worksheets.First().Cells[cellPositionYear].Value = i.ToString();

                StyleDateCell(sheet, cellPositionYear);
            }
        }

        private void AppendDataContent(int sheet)
        {
            var i = 0;
            var oData = Data[sheet].Activities
                .OrderByDescending(d => d.PsSchedule.Finish.ToString("yyyyMMdd") + d.PsSchedule.Start.ToString("yyyyMMdd"));

            foreach (var o in oData)
            {
                if (!cacheColorForOP.ContainsKey(o.WellName))
                    cacheColorForOP[o.WellName] = System.Drawing.ColorTranslator.FromHtml(baseColors[i % baseColors.Count]);

                SequenceMargin prevActivityMargin = null;
                var activityDate = o.PsSchedule;
                var activityMargin = CalculateMargin(activityDate, true);
                /** set plan cell */
                if (activityMargin.Width > 0)
                {
                    var color = cacheColorForOP[o.WellName];
                    var title = o.WellName;// + " " + GetDateStartFinishFormattedValue(activityDate, "MMM dd, yyyy");
                    var rowIndex = GetTopIndex(sheet, 3);

                    BuildCell(sheet, activityMargin, color, title, rowIndex, prevActivityMargin);
                    prevActivityMargin = activityMargin;

                    i++;
                }

                SequenceMargin 
                    prevPhaseOpsUpperMargin = null,
                    prevPhaseOpsLowerMargin = null, 
                    prevPhaseCurrentWeekMargin = null, 
                    prevPhasePreviousWeekMargin = null;

                var j = 0;
                var pData = o.Phases
                    .OrderByDescending(d => d.PhSchedule.Finish.ToString("yyyyMMdd") + d.PhSchedule.Start.ToString("yyyyMMdd"));

                foreach (var p in pData)
                {
                    var phaseDate = new DateRange();
                    var phaseMargin = new SequenceMargin();

                    phaseDate = p.PhSchedule;
                    phaseMargin = CalculateMargin(phaseDate, true);
                    /** set ops cell upper */
                    if (phaseMargin.Width > 0)
                    {
                        var color = cacheColorForOP[o.WellName];
                        var title = o.RigName + " | " + o.WellName;// + " " + GetDateStartFinishFormattedValue(phaseDate, "MMM dd, yyyy");
                        var rowIndex = GetTopIndex(sheet, 4);

                        BuildCell(sheet, phaseMargin, color, title, rowIndex, prevPhaseOpsUpperMargin);
                        prevPhaseOpsUpperMargin = phaseMargin;
                    }

                    phaseDate = p.PhSchedule;
                    phaseMargin = CalculateMargin(phaseDate, true);
                    /** set ops cell lower */
                    if (phaseMargin.Width > 0)
                    {
                        var color = GetActivityColor(p.ActivityType);
                        var title = p.ActivityType;// + " " + GetDateStartFinishFormattedValue(phaseDate, "MMM dd, yyyy");
                        var rowIndex = GetTopIndex(sheet, 5);

                        BuildCell(sheet, phaseMargin, color, title, rowIndex, prevPhaseOpsLowerMargin);
                        prevPhaseOpsLowerMargin = phaseMargin;
                    }

                    
                    #region le
                    //phaseDate = p.LESchedule;
                    //phaseMargin = CalculateMargin(phaseDate, true);
                    ///** set current week */
                    //if (phaseMargin.Width > 0)
                    //{
                    //    var color = GetActivityColor(p.ActivityType);
                    //    var title = (phaseDate.Finish.Date - phaseDate.Start.Date).Days.ToString() + " Days ";// + GetDateStartFinishFormattedValue(phaseDate, "MMM dd, yyyy");
                    //    var rowIndex = GetTopIndex(sheet, 6);

                    //    BuildCell(sheet, phaseMargin, color, title, rowIndex, prevPhaseCurrentWeekMargin);
                    //    prevPhaseCurrentWeekMargin = phaseMargin;
                    //}
                    #endregion
                    phaseDate = p.LWESchedule;
                    phaseMargin = CalculateMargin(phaseDate, true);
                    /** set new op */
                    if (phaseMargin.Width > 0)
                    {
                        //var color = GetActivityColor(p.ActivityType);
                        var color = cacheColorForOP[o.WellName];
                        var title = (phaseDate.Finish.Date - phaseDate.Start.Date).Days.ToString() + " Days ";// + GetDateStartFinishFormattedValue(phaseDate, "MMM dd, yyyy");
                        var rowIndex = GetTopIndex(sheet, 6);

                        BuildCell(sheet, phaseMargin, color,o.WellName, rowIndex, prevPhasePreviousWeekMargin);
                        prevPhasePreviousWeekMargin = phaseMargin;
                    }

                    j++;
                }
            }
        }

        private SequenceMargin CalculateMargin(DateRange date, bool isStrict = false)
        {
            var startDay = (date.Start.Date - ConstDate.Start.Date).Days;

            if (!isStrict)
                startDay = (startDay < 0 ? 0 : startDay);

            var lengthDay = (date.Finish.Date - date.Start.Date).Days;

            if (!isStrict)
                lengthDay = (lengthDay < 0 ? 0 : lengthDay);

            var margin = new SequenceMargin() { 
                Left = Convert.ToInt32(Math.Floor(startDay / 7.0)), 
                Width = Convert.ToInt32(Math.Ceiling(lengthDay / 7.0)) 
            };

            if (isStrict && (margin.Left < 0))
                margin = new SequenceMargin() { Left = 0, Width = margin.Left + margin.Width };

            return margin;
        }

        private void BuildCell(int sheet, SequenceMargin margin, System.Drawing.Color color, string title, int rowIndex, SequenceMargin prevMargin = null)
        {
            if (margin.Width < 1)
                return;

            try 
            { 
                Book.Worksheets.First().Cells.Merge(rowIndex - 1, baseIndex + margin.Left - 1, 1, margin.Width); 
            }
            catch (Exception e)
            {
                if (e.Message.ToLower().Contains("have already been merged"))
                {
                    // Sample error message: "Cells in range CT10:DK10 cannot be merged because cells in range DK10:DQ10 have already been merged."

                    // get CT10
                    var currentTakenSeatEndAt = GetColNumberFromName(Regex.Replace(e.Message.Split(new string[] { "Cells in range " }, StringSplitOptions.None).LastOrDefault().Split(' ').FirstOrDefault().Split(':').LastOrDefault(), @"[\d-]", string.Empty));

                    // get DK10
                    var lastTakenSeatStartAt = GetColNumberFromName(Regex.Replace(e.Message.Split(new string[] { "cells in range " }, StringSplitOptions.None).LastOrDefault().Split(' ').FirstOrDefault().Split(':').FirstOrDefault(), @"[\d-]", string.Empty));

                    // calculate margin
                    var newMarginWidth = margin.Width - (currentTakenSeatEndAt - lastTakenSeatStartAt);

                    if (newMarginWidth < 1)
                        return;

                    foreach (var i in new int[] { 0, 1 })
                    {
                        try
                        {
                            Book.Worksheets.First().Cells.Merge(rowIndex - 1, baseIndex + margin.Left - 1, 1, newMarginWidth - i);
                            break;
                        }
                        catch (Exception f) { }
                    }
                }
            }

            var position = GetColNameFromIndex(baseIndex + margin.Left) + rowIndex;

            Style style = Book.Worksheets.First().Cells[position].GetStyle();
            style.Pattern = BackgroundType.Solid;
            style.ForegroundColor = color;
            style.Font.Color = System.Drawing.Color.White;
            style.HorizontalAlignment = TextAlignmentType.Center;
            style.VerticalAlignment = TextAlignmentType.Center;
            Book.Worksheets.First().Cells[position].SetStyle(style);

            Book.Worksheets.First().Cells[position].Value = title;
        }

        private System.Drawing.Color GetActivityColor(string activityType)
        {
            var color = "#333333";
            activityType = activityType.ToLower();

            if (activityType.Contains("abandon"))
                color = "#2c3e50";
            else if (activityType.Contains("risk"))
                color = "#96281b";
            else if (activityType.Contains("completion"))
                color = "#126237";
            else if (activityType.Contains("drilling"))
                color = "#446cb3";

            return System.Drawing.ColorTranslator.FromHtml(color);
        }

        private void FixCellsHeight(int sheet)
        {
            for (var i = 0; i < 8; i++)
            {
                if (i == 3)
                {
                    Book.Worksheets.First().Cells.SetRowHeight(GetTopIndex(sheet, i) - 1, baseRowHeight * 2);
                    continue;
                }

                Book.Worksheets.First().Cells.SetRowHeight(GetTopIndex(sheet, i) - 1, baseRowHeight);
            }

            for (var i = 0; i < colIndex; i++)
                Book.Worksheets.First().Cells.SetColumnWidth(baseIndex + i - 1, baseColWidth);
        }
    }
}