using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using Aspose.Cells;
using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class WellPIPController : Controller
    {
        // GET: Shell/AdminWell
        public ActionResult Index()
        {
            var appIssue = new ECIS.AppServer.Areas.Issue.Controllers.AppIssueController();
            ViewBag.IsAdministrator = appIssue.isRole("ADMINISTRATORS");
            ViewBag.IsAppSupport = appIssue.isRole("APP-SUPPORTS");

            ViewBag.isReadOnly = "0";
            if (_isReadOnly())
            {
                ViewBag.isRO = "1";
            }

            ViewBag.isAdmin = "0";
            if (_isAdmin())
            {
                ViewBag.isAdmin = "1";
            }
            return View("Index2");
        }
        public JsonResult CheckProcess()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var status = UploadLsStatus.Populate<UploadLsStatus>(Query.EQ("Status", "In Progress"));
                var listWorkingFile = new List<string>();
                if (status.Any())
                {
                    var aa = status.Select(x => x.FileName).ToList();
                    listWorkingFile.AddRange(aa);
                }
                return listWorkingFile;
            });
        }
        private static bool _isAdmin()
        {
            var Email = WebTools.LoginUser.Email;
            List<string> Roles = WEISPerson.GetRolesByEmail(Email);
            var admin = Roles.Where(x => x.ToUpper() == "ADMINISTRATORS" || x.ToLower().Contains("app-support"));
            bool isAdmin = false;
            if (admin.Count() > 0)
            {
                isAdmin = true;
            }
            return isAdmin;
        }

        private static bool _isReadOnly()
        {
            var Email = WebTools.LoginUser.Email;
            List<string> Roles = WEISPerson.GetRolesByEmail(Email);
            var ro = Roles.Where(x => x.ToUpper() == "RO-ALL");
            bool isRO = false;
            if (ro.Count() > 0)
            {
                isRO = true;
            }
            return isRO;
        }
        public JsonResult DownloadPIP(SearchParams ps)
        {
            string xlst = Path.Combine(HttpRuntime.AppDomainAppPath + "App_Data\\Temp", "pipexporttemplate.xlsx");
            Int32 TotalRows = 0;
            var datas = PIPFilter(ps, out TotalRows);//, take, skip, sorts);//GetDataPIPBasedOnSearch(ps);
            string stringName;
            var res = GetCurrentData(datas, xlst, out stringName);


            //return File(res, Tools.GetContentType(".xlsx"), stringName);
            return Json(new { Success = true, Path = stringName }, JsonRequestBehavior.AllowGet);
        }
        public FileResult DownloadPIPFile(string stringName, DateTime date)
        {
            string xlst = Path.Combine(HttpRuntime.AppDomainAppPath + "App_Data\\Temp", "pipexporttemplate.xlsx");
            var res = Path.Combine(HttpRuntime.AppDomainAppPath + "App_Data\\Temp", stringName);
            //rename file
            var retstringName = "WellPIP-" + date.ToString("dd-MMM-yyyy-HHmmss") + ".xlsx";

            return File(res, Tools.GetContentType(".xlsx"), retstringName);
        }
        public static List<WellPIP> GetDataPIPBasedOnSearch(SearchParams ps)
        {

            List<IMongoQuery> qs = new List<IMongoQuery>();
            qs.Add(WebTools.GetWellActivitiesQuery("WellName", "Phases.ActivityType"));
            if (ps.operatingUnits != null && ps.operatingUnits.Count > 0) qs.Add(Query.In("OperatingUnit", new BsonArray(ps.operatingUnits)));
            if (ps.rigTypes != null && ps.rigTypes.Count > 0) qs.Add(Query.In("RigType", new BsonArray(ps.rigTypes)));
            if (ps.rigNames != null && ps.rigNames.Count > 0) qs.Add(Query.In("RigName", new BsonArray(ps.rigNames)));
            if (ps.projectNames != null && ps.projectNames.Count > 0) qs.Add(Query.In("ProjectName", new BsonArray(ps.projectNames)));
            if (ps.wellNames != null && ps.wellNames.Count > 0)
                qs.Add(Query.In("WellName", new BsonArray(ps.wellNames)));
            else
                qs.Add(Query.In("WellName", new BsonArray(WebTools.GetAccessibleWells())));
            if (ps.activities != null && ps.activities.Count > 0)
                qs.Add(Query.In("Phases.ActivityType", new BsonArray(ps.activities)));
            else
                qs.Add(Query.In("Phases.ActivityType", new BsonArray(WebTools.GetAccessibleActivity())));

            string NoneWellMatchWithEXType = "";

            if (ps.exType != null && ps.exType.Count > 0)
            {
                //qs.Add(Query.In("EXType", new BsonArray(ps.exType)));

                if (qs.Contains(Query.In("WellName", new BsonArray(ps.wellNames))) || qs.Contains(Query.In("WellName", new BsonArray(WebTools.GetAccessibleWells()))))
                {
                    qs.Remove(Query.In("WellName", new BsonArray(ps.wellNames)));
                    string[] wellInculde = WellActivity.Populate<WellActivity>(Query.And(Query.In("EXType", new BsonArray(ps.exType)), Query.In("WellName", new BsonArray(WebTools.GetAccessibleWells())))).Select(x => x.WellName).ToArray();
                    List<string> wellPick = new List<string>();
                    foreach (string w in ps.wellNames)
                    {
                        if (wellInculde.Contains(w))
                        {
                            wellPick.Add(w);
                        }
                    }
                    if (wellPick.Count > 0)
                        qs.Add(Query.In("WellName", new BsonArray(wellPick)));
                    else
                    {
                        NoneWellMatchWithEXType = "NoneWellMatchWithEXType";
                        qs.Add(Query.EQ("WellName", NoneWellMatchWithEXType));
                    }
                }
                else
                {
                    string[] wellInculde = WellActivity.Populate<WellActivity>(Query.In("EXType", new BsonArray(ps.exType))).Select(x => x.WellName).ToArray();
                    qs.Add(Query.In("WellName", new BsonArray(wellInculde)));
                }
            }

            var q = qs.Count == 0 ? Query.Null : Query.And(qs);
            var wells = new List<BsonDocument>();
            if (q != Query.Null)
            {
                var aggrArgs = new List<BsonDocument>();
                aggrArgs.Add(new BsonDocument().Set("$match", q.ToBsonDocument()));
                aggrArgs.Add(new BsonDocument().Set("$group", BsonSerializer.Deserialize<BsonDocument>("{_id: {'WellName':'$WellName','RigName':'$RigName','SequenceId':'$UARigSequenceId'} }")));
                wells = DataHelper.Aggregate(new WellActivity().TableName, aggrArgs)
                    .Select(d =>
                        new
                        {
                            WellName = d.GetString("_id.WellName"),
                            RigName = d.GetString("_id.RigName"),
                            SequenceId = d.GetString("_id.SequenceId")
                        }.ToBsonDocument()
                    ).ToList();
            }

            List<string> WellName_CR = new List<string>();
            var ret = new List<WellPIP>();
            if (wells.Count > 0)
            {
                foreach (var x in wells)
                {
                    var w = x.GetString("WellName");
                    var SeqId = x.GetString("SequenceId");
                    List<IMongoQuery> queriesForPIP = new List<IMongoQuery>();
                    queriesForPIP.Add(Query.EQ("WellName", w));
                    queriesForPIP.Add(Query.EQ("SequenceId", SeqId));
                    if (ps.activities != null && ps.activities.Count > 0)
                        queriesForPIP.Add(Query.In("ActivityType", new BsonArray(ps.activities)));
                    if (ps.performanceUnits != null && ps.performanceUnits.Count > 0)
                        queriesForPIP.Add(Query.In("Elements.PerformanceUnit", new BsonArray(ps.performanceUnits)));
                    IMongoQuery query = Query.And(queriesForPIP.ToArray());

                    ret.AddRange(WellPIP.Populate<WellPIP>(query,
                        fields: new string[] { "WellName", "SequenceId", "Version", "Status", "ActivityType", "_id", "PhaseNo", "Type", "Elements" }));

                    WellName_CR.Add(x.GetString("RigName") + "_CR");

                }



            }
            else
            {
                if (!NoneWellMatchWithEXType.Trim().Equals("NoneWellMatchWithEXType"))
                {
                    IMongoQuery queryForPIP = null;
                    if (ps.performanceUnits != null && ps.performanceUnits.Count > 0)
                        queryForPIP = Query.In("Elements.PerformanceUnit", new BsonArray(ps.performanceUnits));
                    ret = WellPIP.Populate<WellPIP>(queryForPIP,
                        fields: new string[] { "WellName", "SequenceId", "Status", "Version", "ActivityType", "_id", "PhaseNo", "Type", "Elements" });
                }
                else
                {
                    ret = new List<WellPIP>();
                }
            }

            if (WellName_CR != null && WellName_CR.Count > 0)
            {
                var query_CR = Query.In("WellName", new BsonArray(WellName_CR));
                List<WellPIP> getPIPCR = WellPIP.Populate<WellPIP>(query_CR);
                if (getPIPCR != null && getPIPCR.Count > 0)
                {
                    foreach (var x in getPIPCR)
                    {
                        ret.Add(x);
                    }
                }
            }

            var final = ret.OrderBy(w => w._id).ToList();
            if (ps.PIPType != "All")
            {
                final = ret.Where(x => x.Type.Equals(ps.PIPType)).OrderBy(w => w._id).ToList();
            }


            //var final = ret.OrderBy(w => w._id).ToList();
            List<WellPIP> wip = new List<WellPIP>();

            foreach (var x in final)
            {
                List<IMongoQuery> abc = new List<IMongoQuery>();
                var WellName = x.WellName;
                var SequenceId = x.SequenceId;
                var ActivityType = x.ActivityType;
                abc.Add(Query.EQ("WellName", WellName));
                //abc.Add(Query.EQ("ActivityType", ActivityType));
                abc.Add(Query.EQ("UARigSequenceId", SequenceId));
                IMongoQuery ab = Query.And(abc.ToArray());
                var getWA = WellActivity.Get<WellActivity>(ab);

                if (getWA != null)
                {
                    var PhaseNo = x.PhaseNo;
                    var getPhase = getWA.Phases.Where(a => a.PhaseNo == PhaseNo).FirstOrDefault();
                    if (getPhase != null)
                    {
                        x.OPSchedule = getPhase != null ? getPhase.PhSchedule : null;
                        x.AFESchedule = getPhase != null ? getPhase.AFESchedule : null;

                        if (getPhase.PhSchedule == null) getPhase.PhSchedule = new DateRange();
                        x.OPStart = getPhase != null ? getPhase.PhSchedule.Start : DateTime.MinValue;
                        x.OPFinish = getPhase != null ? getPhase.PhSchedule.Finish : DateTime.MinValue;

                        if (getPhase.AFESchedule == null) getPhase.AFESchedule = new DateRange();
                        x.AFEStart = getPhase != null ? getPhase.AFESchedule.Start : DateTime.MinValue;
                        x.AFEFinish = getPhase != null ? getPhase.AFESchedule.Finish : DateTime.MinValue;
                    }
                }
                else
                {
                    if (x.OPSchedule == null) x.OPSchedule = new DateRange();
                    if (x.AFESchedule == null) x.AFESchedule = new DateRange();
                    x.OPSchedule.Start = DateTime.MinValue;
                    x.OPSchedule.Finish = DateTime.MinValue;
                    x.AFESchedule.Start = DateTime.MinValue;
                    x.AFESchedule.Finish = DateTime.MinValue;

                    x.OPStart = DateTime.MinValue;
                    x.OPFinish = DateTime.MinValue;
                    x.AFEStart = DateTime.MinValue;
                    x.AFEFinish = DateTime.MinValue;

                }
            }


            return final;
        }
        public string GetCurrentData(List<WellPIP> wellPips, string xlst, out string returnName)
        {
            var wb = new Workbook(xlst);
            var ws = wb.Worksheets[0];
            ws.FreezePanes(1, 1, 1, 0);
            int idx = 2;
            //border
            int brd = 1;
            StyleFlag flgx = new StyleFlag();
            flgx.All = true;

            Style stylex = ws.Cells["A" + brd.ToString()].GetStyle();
            stylex.BackgroundColor = System.Drawing.Color.GreenYellow; //System.Drawing.colo
            stylex.Font.IsBold = true;
            stylex.Borders[BorderType.TopBorder].LineStyle = CellBorderType.Thick;
            stylex.Borders[BorderType.BottomBorder].LineStyle = CellBorderType.Thick;
            stylex.Borders[BorderType.LeftBorder].LineStyle = CellBorderType.Thick;
            stylex.Borders[BorderType.RightBorder].LineStyle = CellBorderType.Thick;
            ws.Cells.CreateRange("A" + brd.ToString(), "X" + brd.ToString()).ApplyStyle(stylex, flgx);
            foreach (var data in wellPips)
            {

                var qs = new List<IMongoQuery>();
                qs.Add(Query.EQ("WellName", data.WellName));
                qs.Add(Query.EQ("UARigSequenceId", data.SequenceId));
                var q = Query.And(qs);
                var actv = WellActivity.Get<WellActivity>(q);

                foreach (var el in data.Elements)
                {
                    var newop = new List<string>();
                    var aa = "";
                    foreach(var x in el.AssignTOOps){
                        newop.Add(x);

                    }
                    var combined = string.Join(", ", newop);
                    
                    ws.Cells["A" + idx.ToString()].Value = data.WellName;
                    ws.Cells["B" + idx.ToString()].Value = data.RigName;
                    ws.Cells["C" + idx.ToString()].Value = data.ActivityType;
                    ws.Cells["D" + idx.ToString()].Value = el.AssignTOOps.Any() ? combined : "";

                    if (data.Type != null && data.Type.Trim() != string.Empty)
                    {
                        //ws.Cells["E" + idx.ToString()].Value = data.Type.Trim().ToLower() == "efficient" ? "CE" : "CR";
                        ws.Cells["E" + idx.ToString()].Value = data.Type.Trim().ToLower() == "efficient" ? "Well/Project PIP" : "Rig/General SCM";

                    }

                    ws.Cells["F" + idx.ToString()].Value = el.Title;
                    ws.Cells["G" + idx.ToString()].Value = el.Period.Start.ToString("M/d/yyyy");
                    ws.Cells["H" + idx.ToString()].Value = el.Period.Finish.ToString("M/d/yyyy"); ;
                    ws.Cells["I" + idx.ToString()].Value = el.Period.Start.ToString("yyyy");

                    ws.Cells["J" + idx.ToString()].Value = el.Completion;
                    ws.Cells["K" + idx.ToString()].Value = el.Classification;
                    ws.Cells["L" + idx.ToString()].Value = el.Theme;


                    ws.Cells["M" + idx.ToString()].Value = el.DaysPlanImprovement;
                    ws.Cells["N" + idx.ToString()].Value = el.DaysPlanRisk;


                    ws.Cells["O" + idx.ToString()].Value = el.CostPlanImprovement;
                    ws.Cells["P" + idx.ToString()].Value = el.CostPlanRisk;

                    ws.Cells["Q" + idx.ToString()].Value = el.DaysCurrentWeekImprovement;
                    ws.Cells["R" + idx.ToString()].Value = el.DaysCurrentWeekRisk;


                    ws.Cells["S" + idx.ToString()].Value = el.CostCurrentWeekImprovement;
                    ws.Cells["T" + idx.ToString()].Value = el.CostCurrentWeekRisk;

                    if (el.ActionParties != null)
                    {
                        ws.Cells["U" + idx.ToString()].Value = string.Join(", ", el.ActionParties.Select(x => x.FullName)); //string.Join(", ", el.ActionParties.ToArray());

                    }
                    ws.Cells["V" + idx.ToString()].Value = el.PerformanceUnit;

                    if (actv != null)
                    {
                        ws.Cells["W" + idx.ToString()].Value = actv.AssetName == null ? "" : actv.AssetName;
                        ws.Cells["X" + idx.ToString()].Value = actv.ProjectName == null ? "" : actv.ProjectName;

                    }




                    StyleFlag flg = new StyleFlag();
                    flg.All = true;


                    if (el.Completion.ToString() == "Realized" || el.Completion.ToString() == "1")
                    {

                        Style style1 = ws.Cells["A" + idx.ToString()].GetStyle();
                        style1.BackgroundColor = System.Drawing.Color.FromArgb(31, 207, 109); //System.Drawing.colo
                        style1.ForegroundColor = System.Drawing.Color.FromArgb(31, 207, 109);
                        style1.Pattern = BackgroundType.Solid;
                        ws.Cells.CreateRange("J" + idx.ToString(), "J" + idx.ToString()).ApplyStyle(style1, flg);

                        //ws.Cells.Rows[idx].Style.BackgroundColor = System.Drawing.Color.LawnGreen;
                        //ws.Cells.Rows[idx].Style.ForegroundColor = System.Drawing.Color.LawnGreen;
                        //ws.Cells.Rows[idx].Style.Pattern = BackgroundType.Solid; //System.Drawing.Color.LawnGreen;
                        //ws.Cells.Rows[idx].Style.ForegroundColor = System.Drawing.Color.LawnGreen;
                    }
                    else
                    {
                        Style style1 = ws.Cells["A" + idx.ToString()].GetStyle();
                        style1.BackgroundColor = System.Drawing.Color.FromArgb(234, 75, 53); //System.Drawing.colo
                        style1.ForegroundColor = System.Drawing.Color.FromArgb(234, 75, 53);
                        style1.Pattern = BackgroundType.Solid;


                        ws.Cells.CreateRange("J" + idx.ToString(), "J" + idx.ToString()).ApplyStyle(style1, flg);
                        //ws.Cells.Rows[idx].Style.BackgroundColor = System.Drawing.Color.Red;
                        //ws.Cells.Rows[idx].Style.ForegroundColor = System.Drawing.Color.Red;
                        //ws.Cells.Rows[idx].Style.Pattern = BackgroundType.Solid;

                    }
                    //    ws.Cells.Rows[idx].Style.BackgroundColor = System.Drawing.Color.Tomato;


                    idx++;

                }

            }
            idx = idx++;
            ws.AutoFilter.Range = "A1:X" + idx.ToString();
            ws.AutoFitColumns();
            var myFileName = Tools.ToUTC(DateTime.Now).ToString("yyyy-MM-dd-HHmmss");
            var newFileNameSingle = Path.Combine(HttpRuntime.AppDomainAppPath + "App_Data\\Temp",
                      String.Format("WellPIP-{0}.xlsx", myFileName));

            returnName = String.Format("WellPIP-{0}.xlsx", myFileName);

            wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            return myFileName;

        }

        public List<WellPIP> PIPFilter(SearchParams ps, out Int32 TotalRows, int take = 0, int skip = 0, List<Dictionary<string, string>> sorts = null)
        {
            List<IMongoQuery> qs = new List<IMongoQuery>();
            qs.Add(WebTools.GetWellActivitiesQuery("WellName", "Phases.ActivityType"));
            if (ps.regions != null && ps.regions.Count > 0) qs.Add(Query.In("Region", new BsonArray(ps.regions)));
            if (ps.operatingUnits != null && ps.operatingUnits.Count > 0) qs.Add(Query.In("OperatingUnit", new BsonArray(ps.operatingUnits)));
            if (ps.rigTypes != null && ps.rigTypes.Count > 0) qs.Add(Query.In("RigType", new BsonArray(ps.rigTypes)));
            if (ps.rigNames != null && ps.rigNames.Count > 0) qs.Add(Query.In("RigName", new BsonArray(ps.rigNames)));
            if (ps.projectNames != null && ps.projectNames.Count > 0) qs.Add(Query.In("ProjectName", new BsonArray(ps.projectNames)));
            if (ps.wellNames != null && ps.wellNames.Count > 0) qs.Add(Query.In("WellName", new BsonArray(ps.wellNames)));
            if (ps.performanceUnits != null && ps.performanceUnits.Count > 0) qs.Add(Query.In("PerformanceUnit", new BsonArray(ps.performanceUnits)));
            if (ps.activities != null && ps.activities.Count > 0)
            {
                qs.Add(Query.In("Phases.ActivityType", new BsonArray(ps.activities)));
            }
            else
            {
                if (ps.activitiesCategory != null && ps.activitiesCategory.Count > 0)
                {
                    var getAct = ActivityMaster.Populate<ActivityMaster>(Query.In("ActivityCategory", new BsonArray(ps.activitiesCategory)));
                    if (getAct.Any())
                    {
                        qs.Add(Query.In("Phases.ActivityType", new BsonArray(getAct.Select(d => d._id.ToString()).ToList())));
                    }
                }
            }

            string NoneWellMatchWithEXType = "";

            if (ps.exType != null && ps.exType.Count > 0)
            {
                //qs.Add(Query.In("EXType", new BsonArray(ps.exType)));

                if (qs.Contains(Query.In("WellName", new BsonArray(ps.wellNames))))
                {
                    qs.Remove(Query.In("WellName", new BsonArray(ps.wellNames)));
                    string[] wellInculde = WellActivity.Populate<WellActivity>(Query.In("EXType", new BsonArray(ps.exType))).Select(x => x.WellName).ToArray();
                    List<string> wellPick = new List<string>();
                    foreach (string w in ps.wellNames)
                    {
                        if (wellInculde.Contains(w))
                        {
                            wellPick.Add(w);
                        }
                    }
                    if (wellPick.Count > 0)
                        qs.Add(Query.In("WellName", new BsonArray(wellPick)));
                    else
                    {
                        NoneWellMatchWithEXType = "NoneWellMatchWithEXType";
                        qs.Add(Query.EQ("WellName", NoneWellMatchWithEXType));
                    }
                }
                else
                {
                    string[] wellInculde = WellActivity.Populate<WellActivity>(Query.In("EXType", new BsonArray(ps.exType))).Select(x => x.WellName).ToArray();
                    qs.Add(Query.In("WellName", new BsonArray(wellInculde)));
                }
            }

            var q = qs.Count == 0 ? Query.Null : Query.And(qs);
            var wells = new List<BsonDocument>();
            if (q != Query.Null)
            {
                var aggrArgs = new List<BsonDocument>();
                aggrArgs.Add(new BsonDocument().Set("$match", q.ToBsonDocument()));
                aggrArgs.Add(new BsonDocument().Set("$group", BsonSerializer.Deserialize<BsonDocument>("{_id: {'WellName':'$WellName','RigName':'$RigName','SequenceId':'$UARigSequenceId'} }")));
                wells = DataHelper.Aggregate(new WellActivity().TableName, aggrArgs)
                    .Select(d =>
                        new
                        {
                            WellName = d.GetString("_id.WellName"),
                            RigName = d.GetString("_id.RigName"),
                            SequenceId = d.GetString("_id.SequenceId")
                        }.ToBsonDocument()
                    ).ToList();
            }

            List<string> WellName_CR = new List<string>();
            TotalRows = 0;
            WellName_CR = wells.Select(x => x.GetString("RigName") + "_CR").ToList();
            var WellNames = wells.Select(x => x.GetString("WellName")).ToList();
            var SequenceIds = wells.Select(x => x.GetString("SequenceId")).ToList();



            var qs1 = new List<IMongoQuery>();
            qs1.Add(WebTools.GetWellActivitiesQuery("WellName", "ActivityType"));
            qs1.Add(Query.In("WellName", new BsonArray(WellNames.ToArray())));
            qs1.Add(Query.In("SequenceId", new BsonArray(SequenceIds.ToArray())));
            if (ps.activities != null && ps.activities.Count > 0)
                qs1.Add(Query.In("ActivityType", new BsonArray(ps.activities)));
            if (ps.performanceUnits != null && ps.performanceUnits.Count > 0)
                qs1.Add(Query.In("Elements.PerformanceUnit", new BsonArray(ps.performanceUnits)));
            if (ps.performanceMetrics != null && ps.performanceMetrics.Count > 0)
                qs1.Add(Query.In("PerformanceMetrics.Title", new BsonArray(ps.performanceMetrics)));
            var QueryPIP1 = Query.And(qs1);
            var QueryForPIP = Query.Null;
            if (ps.PIPType.ToLower() == "all")
            {
                QueryForPIP = Query.Or(QueryPIP1, Query.In("WellName", new BsonArray(WellName_CR)));
            }
            else
            {
                if (ps.PIPType.ToLower() == "reduction")
                {
                    //just CR
                    QueryForPIP = Query.In("WellName", new BsonArray(WellName_CR));
                }
                else
                {
                    //just CE
                    QueryForPIP = QueryPIP1;
                }
            }

            if (ps.OPs != null && ps.OPs.Count > 0)
            {

                if (ps.opRelation.ToLower() == "or")
                {
                    QueryForPIP = Query.And(QueryForPIP, Query.In("Elements.AssignTOOps", new BsonArray(ps.OPs))); ;
                }
                else if (ps.opRelation.ToLower() == "not")
                {
                    QueryForPIP = Query.And(QueryForPIP, Query.NotIn("Elements.AssignTOOps", new BsonArray(ps.OPs))); ;
                }
                else
                {
                    List<IMongoQuery> subQuery = new List<IMongoQuery>();
                    foreach (var op in ps.OPs)
                    {
                        subQuery.Add(Query.EQ("Elements.AssignTOOps", op));
                    }
                    //subQuery.Add((Query.Size("Phases.BaseOP", ps.OPs.Count)));
                    qs.Add(Query.And(subQuery));
                    QueryForPIP = Query.And(QueryForPIP, Query.And(subQuery));
                }


            }

            var field = "";
            SortByBuilder sort = null;
            if (sorts == null || sorts.Count == 0)
            {
                sort = SortBy.Ascending("WellName");
            }
            else
            {
                string sortDir = (Convert.ToString(sorts[0]["dir"]) == "asc" ? "1" : "-1");
                string sortField = Convert.ToString(sorts[0]["field"]);

                if (sortField.ToLower() != "_id")
                {
                    if (sortField.ToLower() == "afestart")
                    {
                        field = "AFE.Start";
                    }
                    else if (sortField.ToLower() == "opstart")
                    {
                        field = "OP.Start";
                    }
                    else
                    {
                        field = sortField;
                    }
                    if (sortDir == "1")
                        sort = SortBy.Ascending(field);
                    else
                        sort = SortBy.Descending(field);
                }
            }


            var ret = new List<WellPIP>();
            var ret2 = new List<WellPIP>();
            if (wells.Count > 0)
            {
                ret = WellPIP.Populate<WellPIP>(QueryForPIP, take, skip, sort, fields: new string[] { "WellName", "SequenceId", "Version", "Status", "ActivityType", "_id", "PhaseNo", "Type", "Elements" });
                TotalRows = WellPIP.Populate<WellPIP>(QueryForPIP, fields: new string[] { "_id" }).Count;
            }
            else
            {
                if (!NoneWellMatchWithEXType.Trim().Equals("NoneWellMatchWithEXType"))
                {
                    IMongoQuery queryForPIP = QueryPIP1;
                    if (ps.performanceUnits != null && ps.performanceUnits.Count > 0)
                        queryForPIP = Query.In("Elements.PerformanceUnit", new BsonArray(ps.performanceUnits));
                    ret = WellPIP.Populate<WellPIP>(queryForPIP, take, skip, sort,
                        fields: new string[] { "WellName", "SequenceId", "Status", "Version", "ActivityType", "_id", "PhaseNo", "Type", "Elements" });
                    TotalRows = WellPIP.Populate<WellPIP>(queryForPIP, fields: new string[] { "_id" }).Count;


                }
                else
                {
                    ret2 = new List<WellPIP>();
                }
            }

            ret2 = ret;
            #region filter base on 
            //if (ret2 != null && ret2.Count > 0)
            //{
            //    if (ps.OPs != null && ps.OPs.Count > 0)
            //    {
            //        ret2 = new List<WellPIP>();
            //        foreach (var x in ret)
            //        {
            //            WellPIP pip = new WellPIP();
            //            pip = x;
            //            List<PIPElement> listElement = new List<PIPElement>();
            //            foreach (var xx in x.Elements)
            //            {
            //                var get = FunctionHelper.CompareBaseOP(ps.OPs.ToArray(), xx.AssignTOOps.ToArray(), ps.opRelation);
            //                if (get)
            //                {
            //                    listElement.Add(xx);
            //                }
            //            }
            //            pip.Elements = listElement;
            //            ret2.Add(pip);
            //        }
            //    }
            //}
            #endregion
            
            var final = ret2.OrderBy(w => w._id).ToList();
            List<WellPIP> wip = new List<WellPIP>();

            foreach (var x in final)
            {
                List<IMongoQuery> abc = new List<IMongoQuery>();
                var WellName = x.WellName;
                var SequenceId = x.SequenceId;
                var ActivityType = x.ActivityType;
                abc.Add(Query.EQ("WellName", WellName));
                //abc.Add(Query.EQ("ActivityType", ActivityType));
                abc.Add(Query.EQ("UARigSequenceId", SequenceId));
                IMongoQuery ab = Query.And(abc.ToArray());
                var getWA = WellActivity.Get<WellActivity>(ab);

                if (getWA != null)
                {
                    var PhaseNo = x.PhaseNo;
                    var getPhase = getWA.Phases.Where(a => a.PhaseNo == PhaseNo).FirstOrDefault();
                    // if phase no doesnt match, try to get it with ActivityName, sample data : APPO AW006, phase no is not same
                    // between phaseno in wellpip and WellactivityPhase
                    if (getPhase == null)
                        getPhase = getWA.Phases.Where(a => a.ActivityType == x.ActivityType).FirstOrDefault();

                    if (getPhase != null)
                    {
                        x.OPSchedule = getPhase != null ? getPhase.PhSchedule : new DateRange();
                        x.AFESchedule = getPhase != null ? getPhase.AFESchedule : new DateRange();

                        if (getPhase.PhSchedule == null) getPhase.PlanSchedule = new DateRange();
                        x.OPStart = getPhase != null ? Tools.ToUTC(getPhase.PlanSchedule.Start) : DateTime.MinValue;
                        x.OPFinish = getPhase != null ? Tools.ToUTC(getPhase.PlanSchedule.Finish) : DateTime.MinValue;

                        if (getPhase.AFESchedule == null) getPhase.AFESchedule = new DateRange();
                        x.AFEStart = getPhase != null ? Tools.ToUTC(getPhase.AFESchedule.Start) : DateTime.MinValue;
                        x.AFEFinish = getPhase != null ? Tools.ToUTC(getPhase.AFESchedule.Finish) : DateTime.MinValue;

                        x.LSStart = x.OPSchedule != null ? Tools.ToUTC(x.OPSchedule.Start) : DateTime.MinValue;
                        x.LSFinish = x.OPSchedule != null ? Tools.ToUTC(x.OPSchedule.Finish) : DateTime.MinValue;

                        //x.LSStart


                        //if (getPhase. == null) getPhase.AFESchedule = new DateRange();
                        //x.AFEStart = getPhase != null ? getPhase.AFESchedule.Start : DateTime.MinValue;
                        //x.AFEFinish = getPhase != null ? getPhase.AFESchedule.Finish : DateTime.MinValue;


                    }
                }
                else
                {
                    if (x.OPSchedule == null) x.OPSchedule = new DateRange();
                    if (x.AFESchedule == null) x.AFESchedule = new DateRange();
                    x.OPSchedule.Start = DateTime.MinValue;
                    x.OPSchedule.Finish = DateTime.MinValue;
                    x.AFESchedule.Start = DateTime.MinValue;
                    x.AFESchedule.Finish = DateTime.MinValue;

                    x.OPStart = DateTime.MinValue;
                    x.OPFinish = DateTime.MinValue;
                    x.AFEStart = DateTime.MinValue;
                    x.AFEFinish = DateTime.MinValue;

                }
            }

            return final;
        }

        public JsonResult Search(SearchParams ps, int take = 10, int skip = 0, List<Dictionary<string, string>> sorts = null)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {

                Int32 TotalRows = 0;
                var final = PIPFilter(ps, out TotalRows, take, skip, sorts);

                return new { Data = final, Total = TotalRows };
            });
        }

        public string GenBaseFromElement(List<PIPElement> Pip)
        {
            string baseop = "";
            
            List<string> data = new List<string>();
            foreach (var i in Pip)
            {
                foreach (var j in i.AssignTOOps)
                {
                    data.Add(j);
                }
            }
            data = data.Distinct().ToList();
            foreach (var i in data)
            {
                if (data.IndexOf(i) == data.Count - 1)
                {
                    baseop += i ;
                }
                else
                {
                    baseop += i + ", ";
                }
                
            }
            return baseop;
        }

        public JsonResult SearchForAddNew(List<string> WellNames, List<string> WellActivityIds, List<string> RigNames, string type)
        {

            List<WellActivity> was = new List<WellActivity>();
            if (type == "Efficient")
            {
                var q = Query.Null;
                List<IMongoQuery> qs = new List<IMongoQuery>();
                if (WellNames != null) qs.Add(Query.In("WellName", new BsonArray(WellNames)));
                if (WellActivityIds != null) qs.Add(Query.In("Phases.ActivityType", new BsonArray(WellActivityIds)));
                if (qs.Count > 0) q = Query.And(qs);

                var qPIP = Query.Null;
                List<IMongoQuery> qsPIP = new List<IMongoQuery>();
                if (WellNames != null) qsPIP.Add(Query.In("WellName", new BsonArray(WellNames)));
                if (WellActivityIds != null) qsPIP.Add(Query.In("ActivityType", new BsonArray(WellActivityIds)));
                if (qsPIP.Count > 0) qPIP = Query.And(qs);

                List<WellActivity> wa = WellActivity.Populate<WellActivity>(q);
                List<WellPIP> pips = WellPIP.Populate<WellPIP>(qPIP);
                foreach (var x in wa)
                {
                    var WellName = x.WellName;
                    var SequenceId = x.UARigSequenceId;
                    foreach (var y in x.Phases)
                    {
                        var PhaseNo = y.PhaseNo;
                        var ActivityType = y.ActivityType;
                        var a = Query.Null;
                        List<IMongoQuery> ab = new List<IMongoQuery>();
                        ab.Add(Query.EQ("WellName", WellName));
                        ab.Add(Query.EQ("SequenceId", SequenceId));
                        ab.Add(Query.EQ("PhaseNo", PhaseNo));
                        ab.Add(Query.EQ("ActivityType", ActivityType));
                        a = Query.And(ab.ToArray());
                        WellPIP CheckPIP = pips.FirstOrDefault(f => f.WellName.Equals(WellName) && f.SequenceId.Equals(SequenceId) && f.PhaseNo.Equals(PhaseNo) && f.ActivityType.Equals(ActivityType));
  //                      WellPIP.Get<WellPIP>(a);
                        if (CheckPIP == null)
                        {
                            WellActivity c = new WellActivity();
                            c._id = x._id;
                            c.PhaseNo = y.PhaseNo;
                            c.WellName = WellName;
                            c.UARigSequenceId = SequenceId;
                            c.ActivityType = ActivityType;
                            was.Add(c);
                        }
                    }
                }
            }
            else
            {
                if (RigNames == null)
                {
                    List<string> Rigs = WellActivity.Populate<WellActivity>().Select(x => x.RigName).Distinct().ToList();
                    RigNames = Rigs;
                }

                foreach (var x in RigNames)
                {
                    WellPIP CheckPIP = WellPIP.Get<WellPIP>(Query.EQ("WellName", x + "_CR"));
                    if (CheckPIP == null)
                    {
                        WellActivity c = new WellActivity();
                        c.WellName = x + "_CR";
                        c.UARigSequenceId = "";
                        c.ActivityType = "";
                        c.PhaseNo = 0;
                        was.Add(c);
                    }
                }
            }
            var jsonResult =  Json(new { Success = true, Data = was.ToArray() }, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public JsonResult CreatePIPDoc(int _id, int PhaseNo)
        {
            var wa = WellActivity.Get<WellActivity>(_id);
            WellPIP wp = new WellPIP();
            WellActivityPhase Phase = new WellActivityPhase();

            Phase = wa.Phases.Where(x => x.PhaseNo == PhaseNo).FirstOrDefault();

            string WellName = wa.WellName;
            string SequenceId = wa.UARigSequenceId;
            string ActivityType = Phase.ActivityType;

            wp.WellName = wa.WellName;
            wp.SequenceId = wa.UARigSequenceId;
            wp.PhaseNo = Phase.PhaseNo;
            wp.ActivityType = Phase.ActivityType;
            wp.Status = "Draft";

            // since it has no element yet, default is Efficient
            wp.Type = "Efficient";
            
            wp.Save();

            //var dataSave = wp.ToBsonDocument();
            //DataHelper.Save(wp.TableName, dataSave);

            //Get the id of new saved PIP
            var q = Query.Null;
            List<IMongoQuery> qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("SequenceId", SequenceId));
            qs.Add(Query.EQ("ActivityType", ActivityType));
            qs.Add(Query.EQ("PhaseNo", PhaseNo));
            q = Query.And(qs);

            var getPIP = WellPIP.Get<WellPIP>(q);

            return Json(new { Success = true, PIPId = getPIP._id, WellName = WellName, ActivityType = ActivityType }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CreatePIPDocFromWeekly(string _id)
        {
            var wa = WellActivityUpdate.Get<WellActivityUpdate>(_id);
           

            WellPIP wp = new WellPIP();
            WellActivityPhase Phase = new WellActivityPhase();

            Phase = wa.Phase;

            string WellName = wa.WellName;
            string SequenceId = wa.SequenceId;
            string ActivityType = Phase.ActivityType;

            wp.WellName = wa.WellName;
            wp.SequenceId = wa.SequenceId;
            wp.PhaseNo = Phase.PhaseNo;
            wp.ActivityType = Phase.ActivityType;
            wp.Status = "Draft";

            // since it has no element yet, default is Efficient
            wp.Type = "Efficient";

            wp.Save();

            //var dataSave = wp.ToBsonDocument();
            //DataHelper.Save(wp.TableName, dataSave);

            //Get the id of new saved PIP
            var q = Query.Null;
            List<IMongoQuery> qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("SequenceId", SequenceId));
            qs.Add(Query.EQ("ActivityType", ActivityType));
            qs.Add(Query.EQ("PhaseNo", wa.Phase.PhaseNo));
            q = Query.And(qs);

            var getPIP = WellPIP.Get<WellPIP>(q);

            return Json(new { Success = true, PIPId = getPIP._id, WellName = WellName, ActivityType = ActivityType }, JsonRequestBehavior.AllowGet);
        }


        public JsonResult CreatePIPDocFromMonthly(string _id)
        {
            var wa = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(_id);


            WellPIP wp = new WellPIP();
            WellActivityPhase Phase = new WellActivityPhase();

            Phase = wa.Phase;

            string WellName = wa.WellName;
            string SequenceId = wa.SequenceId;
            string ActivityType = Phase.ActivityType;

            wp.WellName = wa.WellName;
            wp.SequenceId = wa.SequenceId;
            wp.PhaseNo = Phase.PhaseNo;
            wp.ActivityType = Phase.ActivityType;
            wp.Status = "Draft";

            // since it has no element yet, default is Efficient
            wp.Type = "Efficient";

            wp.Save();

            //var dataSave = wp.ToBsonDocument();
            //DataHelper.Save(wp.TableName, dataSave);

            //Get the id of new saved PIP
            var q = Query.Null;
            List<IMongoQuery> qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("SequenceId", SequenceId));
            qs.Add(Query.EQ("ActivityType", ActivityType));
            qs.Add(Query.EQ("PhaseNo", wa.Phase.PhaseNo));
            q = Query.And(qs);

            var getPIP = WellPIP.Get<WellPIP>(q);

            return Json(new { Success = true, PIPId = getPIP._id, WellName = WellName, ActivityType = ActivityType }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CreatePIPDocForCR(string WellName)
        {
            WellPIP wp = new WellPIP();
            wp.WellName = WellName;
            wp.SequenceId = "";
            wp.PhaseNo = 0;
            wp.ActivityType = "";
            wp.Status = "Draft";
            //wp.Type = "Reduction";

            // since it has no element yet, default is Efficient
            wp.Type = "Efficient";
            wp.Save();

            //var dataSave = wp.ToBsonDocument();
            //DataHelper.Save(wp.TableName, dataSave);

            //Get the id of new saved PIP
            var q = Query.Null;
            List<IMongoQuery> qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            //qs.Add(Query.EQ("SequenceId", SequenceId));
            //qs.Add(Query.EQ("ActivityType", ActivityType));
            //qs.Add(Query.EQ("PhaseNo", PhaseNo));
            q = Query.And(qs);

            var getPIP = WellPIP.Get<WellPIP>(q);

            return Json(new { Success = true, PIPId = getPIP._id, WellName = WellName, ActivityType = "" }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult DeletePIPDoc(string _id)
        {
            try
            {
                var pip = WellPIP.Get<WellPIP>(_id);

                var q = Query.Null;
                List<IMongoQuery> qs = new List<IMongoQuery>();
                qs.Add(Query.EQ("WellName", pip.WellName));
                qs.Add(Query.EQ("SequenceId", pip.SequenceId));
                qs.Add(Query.EQ("Phase.PhaseNo", pip.PhaseNo));
                qs.Add(Query.EQ("Phase.ActivityType", pip.ActivityType));
                q = Query.And(qs);
                var was1 = WellActivityUpdate.Get<WellActivityUpdate>(q);
                if (was1 != null)
                {
                    throw new Exception("Cannot delete data because there is existing weekly report!");
                }
                else
                {
                    DataHelper.Delete(pip.TableName, Query.EQ("_id", _id));
                    string url = (HttpContext.Request).Path.ToString();
                    WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email, LogType.Delete, pip.TableName, url, pip.ToBsonDocument(), null);

                    // Delete CRElements on Respective Well PIP
                    WellPIP.ApplyCRElements(pip);

                }
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }
        public JsonResult GetSummary(string PIPId="", string BaseOP = "OP15")
        {
            var query = Query.EQ("_id", PIPId);
            var wp = WellPIP.Populate<WellPIP>(query);
            var El = wp.SelectMany(d => d.Elements, (d, e) => new
            {
                AssignedToOP = e.AssignTOOps,
                Completion = e.Completion,
                DaysPlanImprovement = e.DaysPlanImprovement,
                DaysPlanRisk  = e.DaysPlanRisk,
                CostPlanImprovement = e.CostPlanImprovement,
                CostPlanRisk = e.CostPlanRisk,
                CostCurrentWeekImprovement = e.CostCurrentWeekImprovement,
                CostCurrentWeekRisk = e.CostCurrentWeekRisk,
                DaysCurrentWeekImprovement = e.DaysCurrentWeekImprovement,
                DaysCurrentWeekRisk = e.DaysCurrentWeekRisk

            });

            var CREl = wp.SelectMany(x => x.CRElements, (x, e) => new 
            {
                AssignedToOP = e.AssignTOOps,
                Completion = e.Completion,
                DaysPlanImprovement = e.DaysPlanImprovement,
                DaysPlanRisk = e.DaysPlanRisk,
                CostPlanImprovement = e.CostPlanImprovement,
                CostPlanRisk = e.CostPlanRisk,
                CostCurrentWeekImprovement = e.CostCurrentWeekImprovement,
                CostCurrentWeekRisk = e.CostCurrentWeekRisk,
                DaysCurrentWeekImprovement = e.DaysCurrentWeekImprovement,
                DaysCurrentWeekRisk = e.DaysCurrentWeekRisk
            });
            int i = 0; int j = 0;
            List<BsonDocument> bslist = new List<BsonDocument>();
            double DaysPlanImprovement_Real = 0; double DaysPlanImprovement_NotReal = 0; double DaysPlanImprovement_Total = 0;
            double DaysPlanRisk_Real = 0; double DaysPlanRisk_NotReal = 0; double DaysPlanRisk_Total= 0;
            double CostPlanImprovement_Real = 0; double CostPlanImprovement_NotReal = 0; double CostPlanImprovement_Total=0;
            double CostPlanRisk_Real = 0; double CostPlanRisk_NotReal = 0; double CostPlanRisk_Total = 0;
            double CostCurrentWeekImprovement_Real = 0; double CostCurrentWeekImprovement_NotReal = 0; double 
                CostCurrentWeekImprovement_Total = 0;
            double CostCurrentWeekRisk_Real = 0; double CostCurrentWeekRisk_NotReal=0;double CostCurrentWeekRisk_Total=0;
            double DaysCurrentWeekImprovement_Real = 0; double DaysCurrentWeekImprovement_NotReal = 0; double DaysCurrentWeekImprovement_Total = 0;
            double DaysCurrentWeekRisk_Real = 0; double DaysCurrentWeekRisk_NotReal = 0; double DaysCurrentWeekRisk_Total = 0;
            if (El.Any())
            {
                foreach (var r in El.Where(x=>x.AssignedToOP.Contains(BaseOP)))
                {
                    if (r.Completion.Equals("Realized"))
                    {
                        DaysPlanImprovement_Real += r.DaysPlanImprovement;
                        DaysPlanRisk_Real += r.DaysPlanRisk;
                        CostPlanImprovement_Real += r.CostPlanImprovement;
                        CostPlanRisk_Real += r.CostPlanRisk;
                        CostCurrentWeekImprovement_Real += r.CostCurrentWeekImprovement;
                        CostCurrentWeekRisk_Real += r.CostCurrentWeekRisk;
                        DaysCurrentWeekImprovement_Real += r.DaysCurrentWeekImprovement;
                        DaysCurrentWeekRisk_Real += r.DaysCurrentWeekRisk;
                    }
                    else
                    {
                        DaysPlanImprovement_NotReal += r.DaysPlanImprovement;
                        DaysPlanRisk_NotReal += r.DaysPlanRisk;
                        CostPlanImprovement_NotReal += r.CostPlanImprovement;
                        CostPlanRisk_NotReal += r.CostPlanRisk;
                        CostCurrentWeekImprovement_NotReal += r.CostCurrentWeekImprovement;
                        CostCurrentWeekRisk_NotReal += r.CostCurrentWeekRisk;
                        DaysCurrentWeekImprovement_NotReal += r.DaysCurrentWeekImprovement;
                        DaysCurrentWeekRisk_NotReal += r.DaysCurrentWeekRisk;
                    }
                    i++;
                }
            }

            DaysPlanImprovement_Total = DaysPlanImprovement_Real + DaysPlanImprovement_NotReal;
            DaysPlanRisk_Total = DaysPlanRisk_Real + DaysPlanRisk_NotReal;
            CostPlanImprovement_Total = CostPlanImprovement_Real + CostPlanImprovement_NotReal;
            CostPlanRisk_Total = CostPlanRisk_Real + CostPlanRisk_NotReal;
            CostCurrentWeekImprovement_Total = CostCurrentWeekImprovement_Real + CostCurrentWeekImprovement_NotReal;
            CostCurrentWeekRisk_Total = CostCurrentWeekRisk_Real + CostCurrentWeekRisk_NotReal;
            DaysCurrentWeekImprovement_Total = DaysCurrentWeekImprovement_Real + DaysCurrentWeekImprovement_NotReal;
            DaysCurrentWeekRisk_Total = DaysCurrentWeekRisk_Real + DaysCurrentWeekRisk_NotReal;
            for (var x = 0; x < 3; x++)
            {
                
                if (x == 0)
                {
                    BsonDocument document = new BsonDocument {
                        {"Completion","Realized"},
                        {"Type_PIP","Well/ _Project PIP"},
                        {"DaysPlanImprovement",DaysPlanImprovement_Real},
                        {"DaysPlanRisk",DaysPlanRisk_Real},
                        {"CostPlanImprovement",CostPlanImprovement_Real},
                        {"CostPlanRisk",CostPlanRisk_Real},
                        {"CostCurrentWeekImprovement",CostCurrentWeekImprovement_Real},
                        {"CostCurrentWeekRisk",CostCurrentWeekRisk_Real},
                        {"DaysCurrentWeekImprovement",DaysCurrentWeekImprovement_Real},
                        {"DaysCurrentWeekRisk",DaysCurrentWeekRisk_Real}
                    };
                    bslist.Add(document);
                }
                else if (x==1)
                {
                    BsonDocument document = new BsonDocument {
                        {"Completion","Not Realized"},
                        {"Type_PIP","Well/ _Project PIP"},
                        {"DaysPlanImprovement",DaysPlanImprovement_NotReal},
                        {"DaysPlanRisk",DaysPlanRisk_NotReal},
                        {"CostPlanImprovement",CostPlanImprovement_NotReal},
                        {"CostPlanRisk",CostPlanRisk_NotReal},
                        {"CostCurrentWeekImprovement",CostCurrentWeekImprovement_NotReal},
                        {"CostCurrentWeekRisk",CostCurrentWeekRisk_NotReal},
                        {"DaysCurrentWeekImprovement",DaysCurrentWeekImprovement_NotReal},
                        {"DaysCurrentWeekRisk",DaysCurrentWeekRisk_NotReal}
                    };
                    bslist.Add(document);
                }
                else
                {
                    BsonDocument document = new BsonDocument {
                        {"Completion","Total"},
                        {"Type_PIP","Well/ _Project PIP"},
                        {"DaysPlanImprovement",DaysPlanImprovement_Total},
                        {"DaysPlanRisk",DaysPlanRisk_Total},
                        {"CostPlanImprovement",CostPlanImprovement_Total},
                        {"CostPlanRisk",CostPlanRisk_Total},
                        {"CostCurrentWeekImprovement",CostCurrentWeekImprovement_Total},
                        {"CostCurrentWeekRisk",CostCurrentWeekRisk_Total},
                        {"DaysCurrentWeekImprovement",DaysCurrentWeekImprovement_Total},
                        {"DaysCurrentWeekRisk",DaysCurrentWeekRisk_Total}
                    };
                    bslist.Add(document);
                }
                
            }
            //CR Element

            //reset variable to zero
            DaysPlanImprovement_Real = 0; DaysPlanImprovement_NotReal = 0; DaysPlanImprovement_Total = 0;
            DaysPlanRisk_Real = 0; DaysPlanRisk_NotReal = 0; DaysPlanRisk_Total = 0;
            CostPlanImprovement_Real = 0; CostPlanImprovement_NotReal = 0; CostPlanImprovement_Total = 0;
            CostPlanRisk_Real = 0; CostPlanRisk_NotReal = 0; CostPlanRisk_Total = 0;
            CostCurrentWeekImprovement_Real = 0; CostCurrentWeekImprovement_NotReal = 0;
            CostCurrentWeekImprovement_Total = 0;
            CostCurrentWeekRisk_Real = 0; CostCurrentWeekRisk_NotReal = 0; CostCurrentWeekRisk_Total = 0;
            DaysCurrentWeekImprovement_Real = 0; DaysCurrentWeekImprovement_NotReal = 0; DaysCurrentWeekImprovement_Total = 0;
            DaysCurrentWeekRisk_Real = 0; DaysCurrentWeekRisk_NotReal = 0; DaysCurrentWeekRisk_Total = 0;

            if (CREl.Any())
            {
                foreach (var r in CREl.Where(x=>x.AssignedToOP.Contains(BaseOP)))
                {
                    if (r.Completion.Equals("Realized"))
                    {
                        DaysPlanImprovement_Real += r.DaysPlanImprovement;
                        DaysPlanRisk_Real += r.DaysPlanRisk;
                        CostPlanImprovement_Real += r.CostPlanImprovement;
                        CostPlanRisk_Real += r.CostPlanRisk;
                        CostCurrentWeekImprovement_Real += r.CostCurrentWeekImprovement;
                        CostCurrentWeekRisk_Real += r.CostCurrentWeekRisk;
                        DaysCurrentWeekImprovement_Real += r.DaysCurrentWeekImprovement;
                        DaysCurrentWeekRisk_Real += r.DaysCurrentWeekRisk;
                    }
                    else
                    {
                        DaysPlanImprovement_NotReal += r.DaysPlanImprovement;
                        DaysPlanRisk_NotReal += r.DaysPlanRisk;
                        CostPlanImprovement_NotReal += r.CostPlanImprovement;
                        CostPlanRisk_NotReal += r.CostPlanRisk;
                        CostCurrentWeekImprovement_NotReal += r.CostCurrentWeekImprovement;
                        CostCurrentWeekRisk_NotReal += r.CostCurrentWeekRisk;
                        DaysCurrentWeekImprovement_NotReal += r.DaysCurrentWeekImprovement;
                        DaysCurrentWeekRisk_NotReal += r.DaysCurrentWeekRisk;
                    }
                    j++;
                }
            }

            DaysPlanImprovement_Total = DaysPlanImprovement_Real + DaysPlanImprovement_NotReal;
            DaysPlanRisk_Total = DaysPlanRisk_Real + DaysPlanRisk_NotReal;
            CostPlanImprovement_Total = CostPlanImprovement_Real + CostPlanImprovement_NotReal;
            CostPlanRisk_Total = CostPlanRisk_Real + CostPlanRisk_NotReal;
            CostCurrentWeekImprovement_Total = CostCurrentWeekImprovement_Real + CostCurrentWeekImprovement_NotReal;
            CostCurrentWeekRisk_Total = CostCurrentWeekRisk_Real + CostCurrentWeekRisk_NotReal;
            DaysCurrentWeekImprovement_Total = DaysCurrentWeekImprovement_Real + DaysCurrentWeekImprovement_NotReal;
            DaysCurrentWeekRisk_Total = DaysCurrentWeekRisk_Real + DaysCurrentWeekRisk_NotReal;
             
             for (var x = 0; x < 3; x++)
             {

                 if (x == 0)
                 {
                     BsonDocument document = new BsonDocument {
                        {"Completion","Realized"},
                        {"Type_PIP","Rig/ _General SCM"},
                        {"DaysPlanImprovement",DaysPlanImprovement_Real},
                        {"DaysPlanRisk",DaysPlanRisk_Real},
                        {"CostPlanImprovement",CostPlanImprovement_Real},
                        {"CostPlanRisk",CostPlanRisk_Real},
                        {"CostCurrentWeekImprovement",CostCurrentWeekImprovement_Real},
                        {"CostCurrentWeekRisk",CostCurrentWeekRisk_Real},
                        {"DaysCurrentWeekImprovement",DaysCurrentWeekImprovement_Real},
                        {"DaysCurrentWeekRisk",DaysCurrentWeekRisk_Real}
                    };
                     bslist.Add(document);
                 }
                 else if (x == 1)
                 {
                     BsonDocument document = new BsonDocument {
                        {"Completion","Not Realized"},
                        {"Type_PIP","Rig/ _General SCM"},
                        {"DaysPlanImprovement",DaysPlanImprovement_NotReal},
                        {"DaysPlanRisk",DaysPlanRisk_NotReal},
                        {"CostPlanImprovement",CostPlanImprovement_NotReal},
                        {"CostPlanRisk",CostPlanRisk_NotReal},
                        {"CostCurrentWeekImprovement",CostCurrentWeekImprovement_NotReal},
                        {"CostCurrentWeekRisk",CostCurrentWeekRisk_NotReal},
                        {"DaysCurrentWeekImprovement",DaysCurrentWeekImprovement_NotReal},
                        {"DaysCurrentWeekRisk",DaysCurrentWeekRisk_NotReal}
                    };
                     bslist.Add(document);
                 }
                 else
                 {
                     BsonDocument document = new BsonDocument {
                        {"Completion","Total"},
                        {"Type_PIP","Rig/ _General SCM"},
                        {"DaysPlanImprovement",DaysPlanImprovement_Total},
                        {"DaysPlanRisk",DaysPlanRisk_Total},
                        {"CostPlanImprovement",CostPlanImprovement_Total},
                        {"CostPlanRisk",CostPlanRisk_Total},
                        {"CostCurrentWeekImprovement",CostCurrentWeekImprovement_Total},
                        {"CostCurrentWeekRisk",CostCurrentWeekRisk_Total},
                        {"DaysCurrentWeekImprovement",DaysCurrentWeekImprovement_Total},
                        {"DaysCurrentWeekRisk",DaysCurrentWeekRisk_Total}
                    };
                     bslist.Add(document);
                 }

             }

            return Json(new { Success = true, Data = DataHelper.ToDictionaryArray(bslist) }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult Get(object id)
        {
            var wp = WellPIP.Get<WellPIP>(id);
            string WellName = wp.WellName;
            string SequenceId = wp.SequenceId;
            string ActivityType = wp.ActivityType;
            int PhaseNo = wp.PhaseNo;

            var q = Query.Null;
            List<IMongoQuery> qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("SequenceId", SequenceId));
            qs.Add(Query.EQ("Phase.ActivityType", ActivityType));
            q = Query.And(qs);

            var checkWAU = WellActivityUpdate.Get<WellActivityUpdate>(q);
            bool wau = true;
            if (checkWAU != null)
            {
                wau = false;
            }

            //(from a in wp.Elements
            // select a).ToList().ForEach((a) =>
            //             {
            //                 a.Completion = a.Completion * 100;
            //             });

            #region getPhaseinWA

            //var q = Query.Null;
            //List<IMongoQuery> qs = new List<IMongoQuery>();
            //qs.Add(Query.EQ("WellName", WellName));
            //qs.Add(Query.EQ("UARigSequenceId", SequenceId));
            //q = Query.And(qs);
            //var wa = WellActivity.Get<WellActivity>(q);
            //if (wa != null)
            //{
            //    var LatestVersion = wau.OrderByDescending(x => x.UpdateVersion).FirstOrDefault();
            //    var MatchedPhase = wa.Phases.Where(x => x.PhaseNo == PhaseNo).FirstOrDefault();
            //    var LEDays = MatchedPhase.LE.Days;
            //    var LECost = MatchedPhase.LE.Cost;
            //    foreach (var x in wp.Elements)
            //    {
            //        if (wp != null)
            //        {
            //            x.LECost = LECost;
            //            x.LEDays = LEDays;
            //        }
            //    }


            //    DateTime UpdateVersion = wau.UpdateVersion;

            //    foreach (var x in wp.Elements)
            //    {
            //        if (wp != null)
            //        {
            //            var a = new DateRange { Start = UpdateVersion, Finish = x.Period.Start };
            //            var b = new DateRange { Start = x.Period.Finish, Finish = x.Period.Start };
            //            x.CompletionPerc = System.Math.Round(Tools.Div(a.Days, b.Days, 0, 2), 1);
            //            if (x.CompletionPerc < 0) x.CompletionPerc = 0;
            //            if (x.CompletionPerc > 1) x.CompletionPerc = 1;
            //        }
            //    }
            //}
            #endregion

            return Json(new { Success = true, Data = wp, WAU = wau }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LoadFromOps()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var was = WellActivity.Populate<WellActivity>();
                WellPIP.UpdateFromWellOps(was);
                return "OK";
            });
        }

        public JsonResult Save(WellPIP w)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                w.Save();
                return w;
            });
        }

        public JsonResult SaveNewPIP(string PIPId, List<string> AssignToOP, string Title, DateTime ActivityStart, DateTime ActivityEnd, double PlanDaysOpp, double PlanDaysRisk, double PlanCostOpp, double PlanCostRisk, string Classification, string PerformanceUnit, string ActionParty, List<WEISPersonInfo> ActionParties, string Theme, bool isPositive)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                //WellPIP wp_update = new WellPIP();

                DateRange Periode = new DateRange(ActivityStart, ActivityEnd);

                var Elements = wp.Elements;

                var oldElements = new List<PIPElement>();
                foreach (var x in Elements)
                {
                    var newElement = x;
                    newElement.isNewElement = false;
                    oldElements.Add(newElement);
                }

                var newPipElement = new PIPElement
                {
                    Title = Title,
                    DaysPlanImprovement = PlanDaysOpp,
                    CostPlanImprovement = PlanCostOpp,
                    DaysPlanRisk = PlanDaysRisk,
                    CostPlanRisk = PlanCostRisk,
                    Period = Periode,
                    Classification = Classification,
                    ActionParty = ActionParty,
                    ActionParties = ActionParties,
                    PerformanceUnit = PerformanceUnit,
                    Theme = Theme,
                    isNewElement = true,
                    AssignTOOps = AssignToOP
                };

                //decide is positive or negative?
                newPipElement.isPositive = true;
                if ((newPipElement.CostPlanImprovement + newPipElement.CostPlanRisk) - (newPipElement.CostCurrentWeekImprovement + newPipElement.CostCurrentWeekRisk) >= 0)
                {
                    newPipElement.isPositive = false;
                }

                oldElements.Add(newPipElement);

                //var dataSave = wp.ToBsonDocument();

                //DataHelper.Save(wp.TableName, dataSave);
                wp.Elements = oldElements;

                foreach(var e in wp.Elements.Where(x=>x.isNewElement == true))
                {
                    if (e.DaysCurrentWeekImprovement == 0) e.DaysCurrentWeekImprovement = e.DaysPlanImprovement;
                    if (e.DaysCurrentWeekRisk == 0) e.DaysCurrentWeekRisk = e.DaysPlanRisk;
                    if (e.CostCurrentWeekImprovement == 0) e.CostCurrentWeekImprovement = e.CostPlanImprovement;
                    if (e.CostCurrentWeekRisk == 0) e.CostCurrentWeekRisk = e.CostPlanRisk;
                }

                wp.Save();
                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email, 
                    LogType.Insert, wp.TableName, url, wp.ToBsonDocument(), null);
     
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult SaveNewPerformanceMetrics(string PIPId, string Title, double Schedule, double Cost)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                WellPIP temp = WellPIP.Get<WellPIP>(query);

                var PerfMetrics = wp.PerformanceMetrics;


                PerfMetrics.Add(new PIPPerformanceMetrics
                {
                    Title = Title,
                    Schedule = Schedule,
                    Cost = Cost
                });

                //var dataSave = wp.ToBsonDocument();

                //DataHelper.Save(wp.TableName, dataSave);

                wp.Save();
                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                    LogType.Update, wp.TableName, url, temp.ToBsonDocument(), wp.ToBsonDocument());
     
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);



                //var result = new ResultInfo();
                //result.Data = new
                //{
                //    Data = updated.ToArray(),
                //    Origin = wa
                //};
                //return MvcTools.ToJsonResult(result.Data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult SaveNewProjectMilestone(string PIPId, string Title, DateTime Period)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);
                WellPIP temp = WellPIP.Get<WellPIP>(query);

                var ProjectMilestone = wp.ProjectMilestones;


                ProjectMilestone.Add(new PIPProjectMilestones
                {
                    Title = Title,
                    Period = Period
                });

                //var dataSave = wp.ToBsonDocument();

                //DataHelper.Save(wp.TableName, dataSave);

                wp.Save();
                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                    LogType.Update, wp.TableName, url, temp.ToBsonDocument(), wp.ToBsonDocument());
     
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);



                //var result = new ResultInfo();
                //result.Data = new
                //{
                //    Data = updated.ToArray(),
                //    Origin = wa
                //};
                //return MvcTools.ToJsonResult(result.Data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult SaveAllocation(string PIPId, int ElementId, List<PIPAllocation> Allocation)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);
                WellPIP temp = WellPIP.Get<WellPIP>(query);

                //WellPIP wp_update = new WellPIP();


                var Elements = wp.Elements.Where(x => x.ElementId == ElementId).FirstOrDefault();
                Elements.Allocations = new List<PIPAllocation>();

                int y = 1;
                if (Allocation != null)
                {
                    foreach (var t in Allocation)
                    {
                        Elements.Allocations.Add(new PIPAllocation
                        {
                            AllocationID = y,
                            //CostPlanImprovement = Math.Round(t.CostPlanImprovement, 2),
                            //CostPlanRisk = Math.Round(t.CostPlanRisk, 2),
                            //DaysPlanImprovement = Math.Round(t.DaysPlanImprovement, 2),
                            //Period = t.Period,
                            //DaysPlanRisk = Math.Round(t.DaysPlanRisk, 2),
                            //LEDays = Math.Round(t.LEDays, 2),
                            //LECost = Math.Round(t.LECost, 2)

                            CostPlanImprovement = t.CostPlanImprovement,
                            CostPlanRisk = t.CostPlanRisk,
                            DaysPlanImprovement = t.DaysPlanImprovement,
                            Period = t.Period,
                            DaysPlanRisk = t.DaysPlanRisk,
                            LEDays = t.LEDays,
                            LECost = t.LECost

                        });
                        y++;
                    }
                }

                wp.Save();
                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                    LogType.Update, wp.TableName, url, temp.ToBsonDocument(), wp.ToBsonDocument());
     
                //var dataSave = wp.ToBsonDocument();

                //DataHelper.Save(wp.TableName, dataSave);

                #region noneed
                ////save allocation to activity update
                //string WellName = wp.WellName;
                //string ActivityType = wp.ActivityType;
                //string SequenceId = wp.SequenceId;


                //WellActivityUpdate NewWau = new WellActivityUpdate();
                //PIPElement NewElement = new PIPElement();
                //List<PIPElement> NewListElement = new List<PIPElement>();

                //List<IMongoQuery> qs = new List<IMongoQuery>();
                //IMongoQuery q = Query.Null;
                //qs.Add(Query.EQ("WellName", WellName));
                //qs.Add(Query.EQ("SequenceId", SequenceId));
                //qs.Add(Query.EQ("Phase.ActivityType", ActivityType));
                //q = Query.And(qs);

                //var wau = WellActivityUpdate.Populate<WellActivityUpdate>(q);
                //List<PIPAllocation> NewAllocation = new List<PIPAllocation>();
                //foreach (var x in wau)
                //{
                //    foreach (var a in x.Elements)
                //    {
                //        //find match element id
                //        if (a.ElementId == ElementId)
                //        {
                //            int b = 1;
                //            List<PIPAllocation> na = new List<PIPAllocation>();
                //            foreach (var t in Allocation)
                //            {
                //                na.Add(new PIPAllocation
                //                {
                //                    AllocationID = b,
                //                    CostPlanImprovement = Math.Round(t.CostPlanImprovement, 1),
                //                    CostPlanRisk = Math.Round(t.CostPlanRisk, 1),
                //                    DaysPlanImprovement = Math.Round(t.DaysPlanImprovement, 1),
                //                    Period = t.Period,
                //                    DaysPlanRisk = Math.Round(t.DaysPlanRisk, 1),
                //                    LEDays = Math.Round(t.LEDays, 1),
                //                    LECost = Math.Round(t.LECost, 1)
                //                });
                //                b++;
                //            }
                //            NewAllocation = na;
                //        }
                //        else
                //        {
                //            NewAllocation = a.Allocations;
                //        }
                //        NewElement = a;
                //        NewElement.Allocations = NewAllocation;
                //        NewListElement.Add(NewElement);
                //    }
                //    NewWau = x;
                //    NewWau.Elements = NewListElement;
                //    DataHelper.Save(NewWau.TableName, NewWau.ToBsonDocument());
                //}
                #endregion

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);



                //var result = new ResultInfo();
                //result.Data = new
                //{
                //    Data = updated.ToArray(),
                //    Origin = wa
                //};
                //return MvcTools.ToJsonResult(result.Data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message + e.StackTrace }, JsonRequestBehavior.AllowGet);
            };
        }
        public static bool Contains(Array a, object val)
        {
            return Array.IndexOf(a, val) != -1;
        }

        public JsonResult UpdatePIP(string PIPId, List<PIPElement> updated)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);
                WellPIP temp = WellPIP.Get<WellPIP>(query);

                string WellName = wp.WellName;
                string ActivityType = wp.ActivityType;
                string SequenceId = wp.SequenceId;


                int[] updatedElementId = new int[200];
                int arrCount = 0;
                foreach (var upd in updated)
                {
                    updatedElementId[arrCount] = upd.ElementId;
                    arrCount++;
                }

                #region update elements on PIP
                var OriginalPIP = wp.Elements;
                List<PIPElement> UpdatedPIP = new List<PIPElement>();

                PIPElement pe = new PIPElement();

                foreach (var i in OriginalPIP)
                {
                    var currentElementId = i.ElementId;
                    if (Contains(updatedElementId, i.ElementId))
                    {
                        // if found match ElementId

                        pe = updated.FirstOrDefault(x => x.ElementId.Equals(i.ElementId));
                        List<PIPAllocation> Allocation = new List<PIPAllocation>();

                        if (i.DaysPlanImprovement != pe.DaysPlanImprovement ||
                            i.CostPlanImprovement != pe.CostPlanImprovement ||
                            i.DaysPlanRisk != pe.DaysPlanRisk ||
                            i.CostPlanRisk != pe.CostPlanRisk ||
                            i.CostCurrentWeekImprovement != pe.CostCurrentWeekImprovement ||
                            i.DaysCurrentWeekImprovement != pe.DaysCurrentWeekImprovement ||
                            i.CostCurrentWeekRisk != pe.CostCurrentWeekRisk ||
                            i.DaysCurrentWeekRisk != pe.DaysCurrentWeekRisk)
                        {
                            Allocation = null;
                        }
                        else
                        {
                            Allocation = i.Allocations;
                        }

                        UpdatedPIP.Add(new PIPElement
                        {
                            ElementId = pe.ElementId,
                            Title = pe.Title,
                            DaysPlanImprovement = pe.DaysPlanImprovement,
                            CostPlanImprovement = pe.CostPlanImprovement,
                            DaysPlanRisk = pe.DaysPlanRisk,
                            CostPlanRisk = pe.CostPlanRisk,
                            Period = pe.Period,
                            Classification = pe.Classification,
                            ActionParty = pe.ActionParty,
                            ActionParties = i.ActionParties,
                            Allocations = Allocation,
                            PerformanceUnit = pe.PerformanceUnit == null ? "" : pe.PerformanceUnit,
                            Completion = pe.Completion,
                            DaysCurrentWeekImprovement = pe.DaysCurrentWeekImprovement,
                            DaysCurrentWeekRisk = pe.DaysCurrentWeekRisk,
                            CostCurrentWeekImprovement = pe.CostCurrentWeekImprovement,
                            CostCurrentWeekRisk = pe.CostCurrentWeekRisk,
                            CostAvoidance = pe.CostAvoidance,
                            //RealizedCost = pe.RealizedCost,
                            //RealizedDays = pe.RealizedDays,
                            CostAvoidanceValue = pe.CostAvoidanceValue,
                            isPositive = pe.isPositive,
                            AssignTOOps = pe.AssignTOOps.Distinct().ToList(),
                            Theme = pe.Theme,
                            isNewElement = false,
                            isUpdate = true
                        });

                        //updatedPhase.Phases.Add(updated.FirstOrDefault(x=>x.PhaseNo.Equals(i.PhaseNo)));
                    }
                    else
                    {
                        UpdatedPIP.Add(i);
                    }
                }

                wp.Elements = UpdatedPIP;
                wp.UserName = WebTools.LoginUser.UserName;
                wp.Save(references: new string[] { "isUpdate" });

             
                #endregion

                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                    LogType.Update, wp.TableName, url, temp.ToBsonDocument(), wp.ToBsonDocument());
     

                #region Update Allocation in activity update
                WellActivityUpdate NewWau = new WellActivityUpdate();
                List<PIPAllocation> NewAllocation = new List<PIPAllocation>();
                PIPElement NewElement = new PIPElement();
                PIPElement WauElement = new PIPElement();
                List<PIPElement> NewListElement = new List<PIPElement>();

                //List<IMongoQuery> qs = new List<IMongoQuery>();
                //IMongoQuery q = Query.Null;
                //qs.Add(Query.EQ("WellName", WellName));
                //qs.Add(Query.EQ("SequenceId", SequenceId));
                //qs.Add(Query.EQ("Phase.ActivityType", ActivityType));
                //q = Query.And(qs);

                //var wau = WellActivityUpdate.Populate<WellActivityUpdate>(q);
                //var wau = WellActivityUpdate.GetById(wp.WellName, wp.SequenceId, wp.PhaseNo);
                //foreach (var a in wau.Elements)
                //{
                //    //find match element id
                //    var currentElementId = a.ElementId;
                //    if (Contains(updatedElementId, a.ElementId))
                //    {
                //        NewElement = updated.FirstOrDefault(c => c.ElementId.Equals(a.ElementId));

                //        List<PIPAllocation> Allocation = new List<PIPAllocation>();
                //        Allocation = UpdatedPIP.Where(d1 => d1.ElementId.Equals(a.ElementId)).Select(d2 => d2.Allocations).FirstOrDefault();
                //        WauElement = a;
                //        WauElement.DaysCurrentWeekImprovement = NewElement.DaysCurrentWeekImprovement;
                //        WauElement.DaysCurrentWeekRisk = NewElement.DaysCurrentWeekRisk;
                //        WauElement.CostCurrentWeekImprovement = NewElement.CostCurrentWeekImprovement;
                //        WauElement.CostCurrentWeekRisk = NewElement.CostCurrentWeekRisk;
                //        WauElement.Period = NewElement.Period;
                //        WauElement.Allocations = Allocation;
                //        NewListElement.Add(WauElement);
                //    }
                //    else
                //    {
                //        NewListElement.Add(a);
                //    }
                //}
                //wau.Elements = NewListElement;
                //wau.Save();
                #endregion



                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = Tools.PushException(e) + e.StackTrace}, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult DeletePIP(string PIPId, int ElementId)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                List<PIPElement> removed = new List<PIPElement>();
                foreach (var t in wp.Elements.Where(x => x.ElementId == ElementId))
                {
                    removed.Add(t);
                }

                wp.Elements.RemoveAll(x => x.ElementId == ElementId);

                wp.Save();

                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email, LogType.Delete, wp.TableName, url, removed.FirstOrDefault().ToBsonDocument(), null);

                //Delete Comments
                new CommentController().DeleteComment("WellPIP", PIPId, ElementId);

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }
        public JsonResult DeletePerformanceMetrics(string PIPId, PIPPerformanceMetrics DataToRemove)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                List<PIPPerformanceMetrics> removed = new List<PIPPerformanceMetrics>();
                foreach (var t in wp.PerformanceMetrics.Where(x => x.Title == DataToRemove.Title
                                           && x.Schedule == DataToRemove.Schedule
                                           && x.Cost == DataToRemove.Cost))
                {
                    removed.Add(t);
                }


                wp.PerformanceMetrics.RemoveAll(x => x.Title == DataToRemove.Title
                                                && x.Schedule == DataToRemove.Schedule
                                                && x.Cost == DataToRemove.Cost);

                wp.Save();
                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email, LogType.Delete, wp.TableName, url, removed.FirstOrDefault().ToBsonDocument(), null);


                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }
        public JsonResult DeleteProjectMilestone(string PIPId, string Title)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);


                List<PIPProjectMilestones> removed = new List<PIPProjectMilestones>();
                foreach (var t in  wp.ProjectMilestones.Where(x => x.Title == Title))
                {
                    removed.Add(t);
                }
             
                wp.ProjectMilestones.RemoveAll(x => x.Title == Title);
                wp.Save();

                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email, LogType.Delete, wp.TableName, url, removed.FirstOrDefault().ToBsonDocument(), null);

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }


        public JsonResult UpdatePerfMetrics(string PIPId, List<PIPPerformanceMetrics> updated)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);
                WellPIP temp = WellPIP.Get<WellPIP>(query);

                wp.PerformanceMetrics = updated;

                wp.Save();

                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                    LogType.Update, wp.TableName, url, temp.ToBsonDocument(), wp.ToBsonDocument());
     
                // check performance metric 
                // <add key="TQTitles" value="Agreed Target,Top Quartile,Performance Target"/>
                // update wellActivityPhase TQ

                //wp.ActivityType
                //wp.WellName
                //wp.PhaseNo   

                string TQTitles = System.Configuration.ConfigurationManager.AppSettings["TQTitles"];
                List<string> tqtitles = TQTitles.Split(',').ToList();
                List<string> titlesLower = new List<string>();

                foreach (string title in tqtitles) titlesLower.Add(title.Trim().ToLower());

                foreach (var t in updated.Where(x => titlesLower.Contains(x.Title.Trim().ToLower())))
                {
                    string utitle = t.Title;
                    double ucost = t.Cost;
                    double udays = t.Schedule;

                    List<IMongoQuery> qs = new List<IMongoQuery>();
                    IMongoQuery q = Query.Null;
                    qs.Add(Query.EQ("WellName", wp.WellName));
                    qs.Add(Query.EQ("UARigSequenceId", wp.SequenceId));
                    WellActivity activity = WellActivity.Get<WellActivity>(Query.And(qs));
                    WellActivity temp1 = WellActivity.Get<WellActivity>(Query.And(qs));
                    var phases = activity.Phases.Where(x => x.PhaseNo == wp.PhaseNo);
                    if (phases != null && phases.Count() > 0)
                    {
                        phases.FirstOrDefault().TQ.Cost = ucost * 1000000;
                        phases.FirstOrDefault().TQ.Days = udays;
                    }
                    activity.Save();

                    WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                        LogType.Update, wp.TableName, url, temp1.ToBsonDocument(), activity.ToBsonDocument());
     
                }


                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult UpdateProjectMilestone(string PIPId, List<PIPProjectMilestones> updated)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);
                WellPIP temp = WellPIP.Get<WellPIP>(query);
                wp.ProjectMilestones = updated;

                wp.Save();
                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                    LogType.Update, wp.TableName, url, temp.ToBsonDocument(), wp.ToBsonDocument());
     
       
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }
        public JsonResult SaveProjectInfos(string PIPId, string Field, string ProjectType, string Scaled, int CostLevel, string OptmzEngEmail = "",string TeamLeadEmail = "",string LeadEngEmail = "")
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);
                WellPIP temp = WellPIP.Get<WellPIP>(query);

                wp.Field = Field;
                wp.ProjectType = ProjectType;
                wp.Scaled = Scaled;
                wp.CostLevel = CostLevel;
                wp.UserName = WebTools.LoginUser.UserName;
                wp.Save();
                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                    LogType.Update, wp.TableName, url, temp.ToBsonDocument(), wp.ToBsonDocument());
     
                #region save personinfos
                
                string WellName = wp.WellName;
                string SequenceId = wp.SequenceId;
                int PhaseNo = wp.PhaseNo;
                string ActivityType = wp.ActivityType;

                List<IMongoQuery> q = new List<IMongoQuery>();
                q.Add(Query.EQ("WellName",WellName));
                q.Add(Query.EQ("SequenceId",SequenceId));
                q.Add(Query.EQ("PhaseNo", PhaseNo));
                //q.Add(Query.EQ("ActivityType", ActivityType));
                IMongoQuery qPerson = Query.And(q);

                WEISPerson OldPerson = WEISPerson.Get<WEISPerson>(qPerson);
                List<WEISPersonInfo> OldPersonInfos = new List<WEISPersonInfo>();
                if (OldPerson != null)
                    OldPersonInfos = OldPerson.PersonInfos;

                List<WEISPersonInfo> NewPersonInfos = new List<WEISPersonInfo>();

                if (TeamLeadEmail == "")
                {
                    //take from old value of persons
                    var getPerson = OldPersonInfos.Where(x => x.RoleId.Equals("TEAM-LEAD")).FirstOrDefault();
                    if (getPerson != null)
                    {
                        WEISPersonInfo person = new WEISPersonInfo();
                        person.FullName = getPerson.FullName;
                        person.Email = getPerson.Email;
                        person.RoleId = "TEAM-LEAD";
                        NewPersonInfos.Add(person);
                    }
                }
                else
                {
                    //take from users
                    WEISPersonInfo person = new WEISPersonInfo();
                    var getPerson = DataHelper.Populate("Users", Query.EQ("Email", TeamLeadEmail));
                    person.FullName = getPerson.Select(d => d.GetString("FullName")).FirstOrDefault();
                    person.Email = getPerson.Select(d=>d.GetString("Email")).FirstOrDefault();
                    person.RoleId = "TEAM-LEAD";
                    NewPersonInfos.Add(person);
                }

                if (LeadEngEmail == "")
                {
                    //take from old value of persons
                    var getPerson = OldPersonInfos.Where(x => x.RoleId.Equals("LEAD-ENG")).FirstOrDefault();
                    WEISPersonInfo person = new WEISPersonInfo();
                    if (getPerson != null)
                    {
                        person.FullName = getPerson.FullName;
                        person.Email = getPerson.Email;
                        person.RoleId = "LEAD-ENG";
                        NewPersonInfos.Add(person);
                    }
                }
                else
                {
                    //take from users
                    WEISPersonInfo person = new WEISPersonInfo();
                    var getPerson = DataHelper.Populate("Users", Query.EQ("Email", LeadEngEmail));
                    person.FullName = getPerson.Select(d => d.GetString("FullName")).FirstOrDefault();
                    person.Email = getPerson.Select(d => d.GetString("Email")).FirstOrDefault();
                    person.RoleId = "LEAD-ENG";
                    NewPersonInfos.Add(person);
                }

                if (OptmzEngEmail == "")
                {
                    //take from old value of persons
                    var getPerson = OldPersonInfos.Where(x => x.RoleId.Equals("OPTMZ-ENG")).FirstOrDefault();
                    WEISPersonInfo person = new WEISPersonInfo();
                    if (getPerson != null)
                    {
                        person.FullName = getPerson.FullName;
                        person.Email = getPerson.Email;
                        person.RoleId = "OPTMZ-ENG";
                        NewPersonInfos.Add(person);

                    }
                }
                else
                {
                    //take from users
                    WEISPersonInfo person = new WEISPersonInfo();
                    var getPerson = DataHelper.Populate("Users", Query.EQ("Email", OptmzEngEmail));
                    person.FullName = getPerson.Select(d => d.GetString("FullName")).FirstOrDefault();
                    person.Email = getPerson.Select(d => d.GetString("Email")).FirstOrDefault();
                    person.RoleId = "OPTMZ-ENG";
                    NewPersonInfos.Add(person);
                }

                // append another exist role in list
                foreach (var x in OldPersonInfos.Where(x => x.RoleId != "TEAM-LEAD" && x.RoleId != "LEAD-ENG" && x.RoleId != "OPTMZ-ENG"))
                {
                    WEISPersonInfo person = new WEISPersonInfo();
                    person.FullName = x.FullName;
                    person.Email = x.Email;
                    person.RoleId = x.RoleId;
                    NewPersonInfos.Add(person);
                }

                //OldPerson.WellName = WellName;
                //OldPerson.ActivityType = ActivityType;
                //OldPerson.PhaseNo = PhaseNo;
                //OldPerson.SequenceId = SequenceId;
                //OldPerson.PersonInfos = NewPersonInfos;
                //OldPerson.Save();

                WEISPerson NewPerson = new WEISPerson();
                NewPerson.WellName = WellName;
                NewPerson.ActivityType = ActivityType;
                NewPerson.PhaseNo = PhaseNo;
                NewPerson.SequenceId = SequenceId;
                NewPerson.PersonInfos = NewPersonInfos;
                NewPerson.Save();

                #endregion

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult GetDataAllocation(string PIPId, int ElementId)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);

                var Elements = wp.Elements.Where(x => x.ElementId == ElementId).FirstOrDefault();

                //double TotalDays = (Elements.Period.Finish - Elements.Period.Start).TotalDays;
                //double diff = Tools.Div(TotalDays, 30);
                //var mthNumber = Math.Ceiling(diff);

                var dt = Elements.Period.Start;
                int mthNumber = 0;
                while (dt < Elements.Period.Finish)
                {
                    mthNumber++;
                    dt = dt.AddMonths(1);
                }

                return Json(new { Success = true, Data = Elements.Allocations.ToArray(), monthDiff = mthNumber }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult getLSInfo(string WellName, string ActivityType = "")
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                IMongoQuery query = null;
                List<IMongoQuery> queries = new List<IMongoQuery>();
                if (ActivityType != "")
                {
                    queries.Add(Query.EQ("WellName", WellName));
                    queries.Add(Query.EQ("ActivityType", ActivityType));
                }
                else
                {
                    queries.Add(Query.EQ("WellName", WellName.Replace("_CR","")));
                }
                query = Query.And(queries.ToArray());
                var lsUpdate = WellPIP.Get<WellPIP>(query);
                var data = new List<object>();
                data.Add(lsUpdate.LastUpdate);
                data.Add(lsUpdate.UserName == null ? "" : lsUpdate.UserName);
                ri.Data = data; 
            }
            catch (Exception e) {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
             
        }

        public JsonResult SelectActivity(string WellName, string ActivityType, string id)
        {

            ResultInfo ri = new ResultInfo();
            try
            {
                IMongoQuery query = null;
                List<IMongoQuery> queries = new List<IMongoQuery>();
                if (id == null || id == "")
                {
                    queries.Add(Query.EQ("WellName", WellName));
                    queries.Add(Query.EQ("Phases.ActivityType", ActivityType));
                }
                else
                {
                    var wp = WellPIP.Get<WellPIP>(id);
                    queries.Add(Query.EQ("UARigSequenceId", wp.SequenceId));
                    queries.Add(Query.EQ("WellName", wp.WellName));
                    /* NEW ADD WELL NAME */

                }
                query = (queries.Count() > 0 ? Query.And(queries.ToArray()) : null);

                DateTime PhaseMin = Tools.DefaultDate, PhaseMax = Tools.DefaultDate;
                WellActivity res = WellActivity.Get<WellActivity>(query);
                if (res != null)
                {
                    res.Phases = res.Phases.Where(d => d.ActivityType.Equals(ActivityType)).ToList();
                    PhaseMin =  res.Phases.Count > 0  ? res.Phases.Min(x => x.PhSchedule.Start) : Tools.DefaultDate;
                    PhaseMax = res.Phases.Count > 0 ? res.Phases.Max(x => x.PhSchedule.Finish) : Tools.DefaultDate;
                }

                ri.Data = new
                {
                    Data = res,
                    PhaseMin = PhaseMin,
                    PhaseMax = PhaseMax
                };

            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }
        public JsonResult SelectActivityForCR(string WellName)
        {

            ResultInfo ri = new ResultInfo();
            try
            {
                DateTime PhaseMin = Tools.DefaultDate, PhaseMax = Tools.DefaultDate;
                var res = new Dictionary<string,string>();

                var getPersons1 = DataHelper.Populate<WEISPerson>("WEISPersons", Query.EQ("WellName", WellName));
                if (getPersons1 != null && getPersons1.Count > 0)
                {
                    string TeamLeadEmail = "";
                    string OptimizationEngineerEmail = "";
                    string LeadEngineerEmail = "";
                    string TeamLead = "";
                    string OptimizationEngineer = "";
                    string LeadEngineer = "";

                    if (getPersons1.FirstOrDefault().PersonInfos != null && getPersons1.FirstOrDefault().PersonInfos.Count > 0)
                    {
                        var getPersons = getPersons1.FirstOrDefault().PersonInfos;

                        TeamLeadEmail = getPersons.FirstOrDefault(d => d.RoleId.Equals("TEAM-LEAD")) == null ? "" : getPersons.FirstOrDefault(d => d.RoleId.Equals("TEAM-LEAD")).Email;

                        OptimizationEngineerEmail = getPersons.FirstOrDefault(d => d.RoleId.Equals("OPTMZ-ENG")) == null ? "" : getPersons.FirstOrDefault(d => d.RoleId.Equals("OPTMZ-ENG")).Email;

                        LeadEngineerEmail = getPersons.FirstOrDefault(d => d.RoleId.Equals("LEAD-ENG")) == null ? "" : getPersons.FirstOrDefault(d => d.RoleId.Equals("LEAD-ENG")).Email;

                        TeamLead = getPersons.FirstOrDefault(d => d.RoleId.Equals("TEAM-LEAD")) == null ? "" : getPersons.FirstOrDefault(d => d.RoleId.Equals("TEAM-LEAD")).FullName;

                        OptimizationEngineer = getPersons.FirstOrDefault(d => d.RoleId.Equals("OPTMZ-ENG")) == null ? "" : getPersons.FirstOrDefault(d => d.RoleId.Equals("OPTMZ-ENG")).FullName;

                        LeadEngineer = getPersons.FirstOrDefault(d => d.RoleId.Equals("LEAD-ENG")) == null ? "" : getPersons.FirstOrDefault(d => d.RoleId.Equals("LEAD-ENG")).FullName;
                        

                        //TeamLeadEmail = getPersons
                        //    .Where(d => d.RoleId.Equals("TEAM-LEAD"))
                        //    .FirstOrDefault()
                        //    .Email;
                        //OptimizationEngineerEmail = getPersons
                        //    .Where(d => d.RoleId.Equals("OPTMZ-ENG"))
                        //    .FirstOrDefault()
                        //    .Email;
                        //LeadEngineerEmail = getPersons
                        //    .Where(d => d.RoleId.Equals("LEAD-ENG"))
                        //    .FirstOrDefault()
                        //    .Email;

                        //TeamLead = getPersons
                        //    .Where(d => d.RoleId.Equals("TEAM-LEAD"))
                        //    .FirstOrDefault()
                        //    .FullName;
                        //OptimizationEngineer = getPersons
                        //    .Where(d => d.RoleId.Equals("OPTMZ-ENG"))
                        //    .FirstOrDefault()
                        //    .FullName;
                        //LeadEngineer = getPersons
                        //    .Where(d => d.RoleId.Equals("LEAD-ENG"))
                        //    .FirstOrDefault()
                        //    .FullName;
                    }
                    res.Add("TeamLeadEmail", TeamLeadEmail);
                    res.Add("OptimizationEngineerEmail", OptimizationEngineerEmail);
                    res.Add("LeadEngineerEmail", LeadEngineerEmail);
                    res.Add("TeamLead", TeamLead);
                    res.Add("OptimizationEngineer", OptimizationEngineer);
                    res.Add("LeadEngineer", LeadEngineer);
                }
                else
                {
                    res.Add("TeamLeadEmail", "");
                    res.Add("OptimizationEngineerEmail", "");
                    res.Add("LeadEngineerEmail", "");
                    res.Add("TeamLead", "");
                    res.Add("OptimizationEngineer", "");
                    res.Add("LeadEngineer", "");
                }

                ri.Data = new
                {
                    Data = res,
                    PhaseMin = PhaseMin,
                    PhaseMax = PhaseMax
                };

            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public Double GetDayOrCost(PIPElement d, string DayorCost = "Day", string element = "Plan")
        {
            double ret = 0;
            switch (element)
            {
                case "Plan":
                    if (DayorCost.Equals("Day")) ret = d.DaysPlanImprovement + d.DaysPlanRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostPlanImprovement + d.CostPlanRisk;
                    break;

                case "Actual":
                    if (DayorCost.Equals("Day")) ret = d.DaysActualImprovement + d.DaysActualRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostActualImprovement + d.CostActualRisk;
                    break;

                case "LE":
                    if (DayorCost.Equals("Day")) ret = d.DaysCurrentWeekImprovement + d.DaysCurrentWeekRisk;
                    if (DayorCost.Equals("Cost")) ret = d.CostCurrentWeekImprovement + d.CostCurrentWeekRisk;
                    break;
            }
            return ret;
        }

        public JsonResult GetWaterfallDataForCR(
            string Layout = "OP2TQ",
            string GroupBy = "Classification", bool IncludeZero = false, bool IncludeCR = false,
            string DayOrCost = "Day",
            string RigName = "",
            List<PIPElement> PIPs = null,
            List<PIPElement> CRPIPs = null,
            bool byRealised = false,
            string WellName = "",
            string SequenceId = "",
            string type = "",
            string ActivityType = "")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                CRPIPs = CRPIPs ?? new List<PIPElement>();
                var division = 1000000.0;

                DateTime today = Tools.ToUTC(DateTime.Today);
                DateTime periodStart = Tools.ToUTC(new DateTime(today.Year, 01, 01));
                DateTime periodFinish = Tools.ToUTC(new DateTime(today.Year, 12, 31));

                DateTime dateStartBegin = periodStart.AddDays(-7);
                DateTime dateStartEnd = periodStart.AddDays(7);
                DateTime dateFinishBegin = periodFinish.AddDays(-7);
                DateTime dateFinishEnd = periodFinish.AddDays(7);

                var queries = new List<IMongoQuery>();
                queries.Add(Query.EQ("RigName", RigName));

                var qPeriods = new List<IMongoQuery>();
                List<IMongoQuery> dateQueries = new List<IMongoQuery>();
                dateQueries.Add(Query.And(
                    Query.GTE("Phases.PhSchedule.Start", dateStartBegin),
                    Query.LTE("Phases.PhSchedule.Start", dateFinishEnd)
                ));
                dateQueries.Add(Query.And(
                    Query.GTE("Phases.PhSchedule.Finish", dateStartBegin),
                    Query.LTE("Phases.PhSchedule.Finish", dateFinishEnd)
                ));
                queries.Add(Query.Or(dateQueries.ToArray()));
                var query = Query.And(queries.ToArray());

                var PopWA1 = WellActivity.Populate<WellActivity>(query)
                            .Where(d =>
                            {
                                var dateRange = new DateRange();
                                dateRange.Start = d.Phases.Min(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Start));
                                dateRange.Finish = d.Phases.Max(f => (f.PhSchedule == null ? Tools.DefaultDate : f.PhSchedule.Finish));
                                return new DashboardController().dateBetween(dateRange, periodStart.ToString("yyyy-MM-dd"), periodFinish.ToString("yyyy-MM-dd"), periodStart.ToString("yyyy-MM-dd"), periodFinish.ToString("yyyy-MM-dd"), "OR");
                            });
                var PopWA2 = PopWA1
                             .Select(d =>
                             {
                                d.Phases = d.Phases.Where(e =>
                                {
                                    var riskFilter = !e.ActivityType.ToLower().Contains("risk");
                                    return (riskFilter);
                                }).ToList();
                                 return d;
                             })
                             .OrderBy(d => d.WellName)
                             .ToList();
                var PopWA = PopWA2.SelectMany(d => d.Phases, (d, e) => new ListWaterfall()
                            {
                                Activity = d,
                                Phase = e
                            })
                            .Where(d => !d.Phase.ActivityType.ToLower().Contains("risk"))
                            .ToList();

                int c = PopWA.Count();
                int i = 0;
                while (i < c)
                {
                    int x = c - i - 1;
                    WellActivity wac = PopWA[x].Activity;
                    WellActivityPhase wap = PopWA[x].Phase;
                    if (!WellActivity.isHasWellPip(wac, wap))
                    {
                        PopWA.Remove(PopWA[x]);
                    }
                    i++;
                }

                double SumOPDays = PopWA.Sum(x => x.Phase.OP.Days);
                double SumOPCost = PopWA.Sum(x => x.Phase.OP.Cost);
                double SumAFEDays = PopWA.Sum(x => x.Phase.AFE.Days);
                double SumAFECost = PopWA.Sum(x => x.Phase.AFE.Cost);
                double SumTQDays = PopWA.Sum(x => x.Phase.TQ.Days);
                double SumTQCost = PopWA.Sum(x => x.Phase.TQ.Cost);
                double SumLEDays = PopWA.Sum(x => x.Phase.LE.Days);
                double SumLECost = PopWA.Sum(x => x.Phase.LE.Cost);

                if (SumAFEDays == 0)
                {
                    SumAFEDays = SumOPDays;
                    SumAFECost = SumOPCost;
                }

                if (SumTQDays == 0)
                {
                    SumTQDays = SumOPDays * 0.75;
                    SumTQCost = SumOPCost * 0.75;
                }

                var TQ = DayOrCost.Equals("Day") ? SumTQDays : SumTQCost;
                var AFE = DayOrCost.Equals("Day") ? SumAFEDays : SumAFECost;
                var OP = DayOrCost.Equals("Day") ? SumOPDays : SumOPCost;
                var allLE = DayOrCost.Equals("Day") ? SumLEDays : SumLECost;

                if (DayOrCost.Equals("Cost"))
                {
                    TQ /= division;
                    AFE /= division;
                    OP /= division;
                    allLE /= division;
                }

                if (byRealised)
                {
                    return GetWaterfallDataByRealised(GroupBy, IncludeZero, IncludeCR, DayOrCost, PIPs, CRPIPs, OP, AFE, TQ, allLE);
                }

                var final = new List<WaterfallItem>();
                if (Layout.Contains("OP"))
                {
                    final.Add(new WaterfallItem(0, "OP", OP, ""));
                }

                if (PIPs != null && PIPs.Count > 0)
                {
                    var groupPIPS = PIPs.GroupBy(d => d.ToBsonDocument().GetString(GroupBy))
                        .Select(d =>
                        {
                            var Plan = d.Sum(e => GetDayOrCost(e, DayOrCost, "Plan"));
                            var LE = d.Sum(e => GetDayOrCost(e, DayOrCost, "LE"));

                            return new WaterfallPIPRaw()
                            {
                                Key = d.Key,
                                Plan = Plan,
                                LE = LE
                            };
                        })
                        .ToList();

                    if (IncludeCR)
                    {
                        var groupCRPIPS = CRPIPs.GroupBy(d => d.ToBsonDocument().GetString(GroupBy))
                            .Select(d => new WaterfallPIPRaw()
                            {
                                Key = d.Key,
                                Plan = d.Sum(e => GetDayOrCost(e, DayOrCost, "Plan")),
                                LE = d.Sum(e => GetDayOrCost(e, DayOrCost, "LE"))
                            })
                            .ToList();

                        groupCRPIPS.ForEach(d =>
                        {
                            var eachPIP = groupPIPS.FirstOrDefault(e => e.Key.Equals(d.Key));
                            if (eachPIP != null)
                            {
                                eachPIP.Plan += d.Plan;
                                eachPIP.LE += d.LE;
                            }
                            else
                            {
                                groupPIPS.Add(new WaterfallPIPRaw()
                                {
                                    Key = d.Key,
                                    Plan = d.Plan,
                                    LE = d.LE
                                });
                            }
                        });
                    }

                    if (Layout.Contains("TQ") || true) // hack, layout is not used
                    {
                        if (Layout.Contains("OP") || true) // hack, layout is not used
                        {
                            double gap = 0;
                            foreach (var gp in groupPIPS
                                .Where(d => (!IncludeZero && d.Plan != 0) || IncludeZero)
                                .OrderByDescending(d => d.Plan))
                            {
                                final.Add(new WaterfallItem(0.1, gp.Key == "BsonNull" ? "(P)" : gp.Key + "(P)", gp.Plan, ""));
                                gap += gp.Plan;
                            }
                            if (GroupBy.Equals("Classification") && IncludeZero)
                            {
                                var visiblePIPs = groupPIPS.Select(d => d.Key).Distinct();
                                WellPIPClassifications.Populate<WellPIPClassifications>().ForEach(d =>
                                {
                                    if (visiblePIPs.Any(e => e.Equals(d.Name)))
                                        return;

                                    final.Add(new WaterfallItem(0.1, d.Name + "(P)", 0, ""));
                                });
                            }
                            if (gap + OP != allLE)
                            {
                                final.Add(new WaterfallItem(0.1, "Gap to LE (P)", allLE - (gap + OP), ""));
                            }
                        }
                        final.Add(new WaterfallItem(1, "LE", allLE, Layout.Contains("OP") ? "total" : ""));
                    }
                }

                //var highestLine = final.Sum(d => d.Value);
                foreach (var f in final)
                {
                    f.TrendLines.Add(AFE);
                    f.TrendLines.Add(TQ);
                }

                return final;
            });
        }

        public JsonResult GetWaterfallData(
            string Layout = "OP2TQ",
            string GroupBy = "Classification", bool IncludeZero = false, bool IncludeCR = false,
            string DayOrCost = "Day",
            string WellName = "",
            string SequenceId = "",
            string ActivityType = "",
            List<PIPElement> PIPs = null,
            List<PIPElement> CRPIPs = null,
            string type = "",
            string BaseOP = "OP15",
            bool byRealised = false)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                string DefaultOP = "OP15";
                if (Config.GetConfigValue("BaseOPConfig") != null)
                {
                    DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
                }

                PIPs = PIPs.Where(x => x.AssignTOOps.Contains(BaseOP)).ToList();

                CRPIPs = CRPIPs ?? new List<PIPElement>();
                var division = 1000000.0;
                IMongoQuery q = Query.Null;
                var qs = new List<IMongoQuery>();
                qs.Add(Query.EQ("WellName", WellName));
                qs.Add(Query.EQ("UARigSequenceId", SequenceId));
                var wa = WellActivity.Get<WellActivity>(Query.And(qs));
                if (wa == null)
                {
                    if (byRealised)
                        return new WaterfallPIPResultByRealised();
                    
                    return new List<WaterfallItem>();
                }

                var TQ = 0.0;
                var AFE = 0.0;
                var OP = 0.0;
                var allLE = 0.0;

                if (wa.Phases.Any())
                {
                    var act = wa.Phases.Where(x => null != x.ActivityType && x.ActivityType.Equals(ActivityType)).FirstOrDefault();
                    //var hst = wa.OPHistories.FirstOrDefault(x => x.ActivityType.Equals(ActivityType) && !String.IsNullOrEmpty(x.Type) && x.Type.Equals(BaseOP));
                    if (act != null)
                    {
                        if (act.BaseOP.OrderByDescending(x => x).FirstOrDefault() == BaseOP)
                        {
                            //take from body
                            if (act.TQ.Days == 0) act.TQ = new WellDrillData
                            {
                                Days = act.OP.Days * 0.75,
                                Cost = act.OP.Cost * 0.75
                            };
                            TQ = DayOrCost.Equals("Day") ? act.TQ.Days : act.TQ.Cost;
                            AFE = DayOrCost.Equals("Day") ? act.AFE.Days : act.AFE.Cost;
                            OP = DayOrCost.Equals("Day") ? act.Plan.Days : act.Plan.Cost;
                            //if (hst != null)
                            //{
                            //    OP = DayOrCost.Equals("Day") ? hst.Plan.Days : hst.Plan.Cost;
                            //}

                            allLE = DayOrCost.Equals("Day") ? act.LE.Days : act.LE.Cost;
                        }
                        else
                        {
                            //take from histories
                            if (wa.OPHistories.Any())
                            {
                                var hst = wa.OPHistories.Where(x => x.Type.Equals(BaseOP) && x.ActivityType.Equals(ActivityType)).FirstOrDefault();
                                if (hst != null)
                                {
                                    //if (act.AFE.Days == 0) act.AFE = act.OP;
                                    if (hst.TQ.Days == 0) hst.TQ = new WellDrillData
                                    {
                                        Days = hst.OP.Days * 0.75,
                                        Cost = hst.OP.Cost * 0.75
                                    };
                                    TQ = DayOrCost.Equals("Day") ? hst.TQ.Days : hst.TQ.Cost;
                                    AFE = DayOrCost.Equals("Day") ? hst.AFE.Days : hst.AFE.Cost;
                                    OP = DayOrCost.Equals("Day") ? hst.Plan.Days : hst.Plan.Cost < 5000 ? hst.Plan.Cost * division : hst.Plan.Cost;
                                    allLE = DayOrCost.Equals("Day") ? act.LE.Days : act.LE.Cost;
                                }
                            }
                        }
                    }
                }


                #region old logic
                //if (BaseOP == DefaultOP)
                //{
                //    if (wa.Phases.Any())
                //    {
                //        var act = wa.Phases.Where(x => x.BaseOP.Contains(BaseOP) && x.ActivityType.Equals(ActivityType)).FirstOrDefault();
                //        var hst = wa.OPHistories.FirstOrDefault(x => x.ActivityType.Equals(ActivityType) && !String.IsNullOrEmpty(x.Type) && x.Type.Equals(BaseOP));
                //        if (act != null)
                //        {
                //            //if (act.AFE.Days == 0) act.AFE = act.OP;
                //            if (act.TQ.Days == 0) act.TQ = new WellDrillData
                //            {
                //                Days = act.OP.Days * 0.75,
                //                Cost = act.OP.Cost * 0.75
                //            };
                //            TQ = DayOrCost.Equals("Day") ? act.TQ.Days : act.TQ.Cost;
                //            AFE = DayOrCost.Equals("Day") ? act.AFE.Days : act.AFE.Cost;
                //            OP = DayOrCost.Equals("Day") ? act.Plan.Days : act.Plan.Cost;
                //            if (hst != null)
                //            {
                //                OP = DayOrCost.Equals("Day") ? hst.Plan.Days : hst.Plan.Cost;
                //            }

                //            allLE = DayOrCost.Equals("Day") ? act.LE.Days : act.LE.Cost;
                //        }
                //    }
                //}
                //else
                //{
                //    if (wa.OPHistories.Any())
                //    {
                //        var act = wa.OPHistories.Where(x => x.Type.Equals(BaseOP) && x.ActivityType.Equals(ActivityType)).FirstOrDefault();
                //        if (act != null)
                //        {
                //            //if (act.AFE.Days == 0) act.AFE = act.OP;
                //            if (act.TQ.Days == 0) act.TQ = new WellDrillData
                //            {
                //                Days = act.OP.Days * 0.75,
                //                Cost = act.OP.Cost * 0.75
                //            };
                //            TQ = DayOrCost.Equals("Day") ? act.TQ.Days : act.TQ.Cost;
                //            AFE = DayOrCost.Equals("Day") ? act.AFE.Days : act.AFE.Cost;
                //            OP = DayOrCost.Equals("Day") ? act.Plan.Days : act.Plan.Cost < 5000 ? act.Plan.Cost * division : act.Plan.Cost;
                //            allLE = DayOrCost.Equals("Day") ? act.LE.Days : act.LE.Cost;
                //        }
                //    }
                //}
                #endregion

                if (DayOrCost.Equals("Cost"))
                {
                    TQ /= division;
                    AFE /= division;
                    OP /= division;
                    allLE /= division;
                }

                if (byRealised)
                {
                    return GetWaterfallDataByRealised(GroupBy, IncludeZero, IncludeCR, DayOrCost, PIPs, CRPIPs, OP, AFE, TQ, allLE);
                }

                var final = new List<WaterfallItem>();
                if (Layout.Contains("OP"))
                {
                    final.Add(new WaterfallItem(0, "OP", OP, ""));
                }

                if (PIPs != null && PIPs.Count > 0)
                {
                    var groupPIPS = PIPs.GroupBy(d => d.ToBsonDocument().GetString(GroupBy))
                        .Select(d =>
                        {
                            var Plan = d.Sum(e => GetDayOrCost(e, DayOrCost, "Plan"));
                            var LE = d.Sum(e => GetDayOrCost(e, DayOrCost, "LE"));

                            return new WaterfallPIPRaw()
                            {
                                Key = d.Key,
                                Plan = Plan,
                                LE = LE
                            };
                        })
                        .ToList();

                    if (IncludeCR)
                    {
                        var groupCRPIPS = CRPIPs.GroupBy(d => d.ToBsonDocument().GetString(GroupBy))
                            .Select(d => new WaterfallPIPRaw()
                            {
                                Key = d.Key,
                                Plan = d.Sum(e => GetDayOrCost(e, DayOrCost, "Plan")),
                                LE = d.Sum(e => GetDayOrCost(e, DayOrCost, "LE"))
                            })
                            .ToList();

                        groupCRPIPS.ForEach(d =>
                        {
                            var eachPIP = groupPIPS.FirstOrDefault(e => e.Key.Equals(d.Key));
                            if (eachPIP != null)
                            {
                                eachPIP.Plan += d.Plan;
                                eachPIP.LE += d.LE;
                            }
                            else
                            {
                                groupPIPS.Add(new WaterfallPIPRaw()
                                {
                                    Key = d.Key,
                                    Plan = d.Plan,
                                    LE = d.LE
                                });
                            }
                        });
                    }

                    if (Layout.Contains("TQ") || true) // hack, layout is not used
                    {
                        if (Layout.Contains("OP") || true) // hack, layout is not used
                        {
                            double gap = 0;
                            foreach (var gp in groupPIPS
                                .Where(d => (!IncludeZero && d.Plan != 0) || IncludeZero)
                                .OrderByDescending(d => d.Plan))
                            {
                                final.Add(new WaterfallItem(0.1, gp.Key == "BsonNull" ? "All Others" : gp.Key, gp.LE, ""));
                                gap += gp.LE;
                            }
                            if (GroupBy.Equals("Classification") && IncludeZero)
                            {
                                var visiblePIPs = groupPIPS.Select(d => d.Key).Distinct();
                                WellPIPClassifications.Populate<WellPIPClassifications>().ForEach(d =>
                                {
                                    if (visiblePIPs.Any(e => e.Equals(d.Name)))
                                        return;

                                    final.Add(new WaterfallItem(0.1, d.Name, 0, ""));
                                });
                            }
                            if (gap + OP != TQ)
                            {
                                final.Add(new WaterfallItem(0.1, "Gap to LE", allLE - (gap + OP), ""));
                            }
                        }
                        final.Add(new WaterfallItem(1, "LE", allLE, Layout.Contains("OP") ? "total" : ""));
                    }
                }

                foreach (var f in final)
                {
                    f.TrendLines.Add(AFE);
                    f.TrendLines.Add(TQ);
                }

                var valUnrisk = final[final.Count() - 1].Value;
                if (valUnrisk > 0)
                    valUnrisk = valUnrisk*-1;
                else
                    valUnrisk = valUnrisk*-1;

                final.Add(new WaterfallItem(0, "Unrisked Upside", valUnrisk, ""));//Layout.Contains("OP") ? "total" : 

                return final;
            });
        }

        private WaterfallPIPResultByRealised GetWaterfallDataByRealised(
            string GroupBy = "Classification", bool IncludeZero = false, bool IncludeCR = false,
            string DayOrCost = "Day",
            List<PIPElement> PIPs = null,
            List<PIPElement> CRPIPs = null,
            double OP = 0,
            double AFE = 0,
            double TQ = 0,
            double allLE = 0)
        {
            if (PIPs == null)
            {
                return new WaterfallPIPResultByRealised()
                {
                    OP = OP,
                    AFE = AFE,
                    LE = allLE,
                    TQ = TQ,
                    Gaps = allLE - (0 + OP),
                    Items = new List<WaterfallItemByRealised>()
                };
            }

            if (IncludeCR)
            {
                CRPIPs.ForEach(d =>
                {
                    var exists = PIPs.FirstOrDefault(e => e.ToBsonDocument().GetString(GroupBy).Equals(d.ToBsonDocument().GetString(GroupBy)));
                    if (exists == null)
                    {
                        PIPs.Add(new PIPElement()
                        {
                            ActionParty = d.ActionParty,
                            Classification = d.Classification,
                            Completion = d.Completion,
                            PerformanceUnit = d.PerformanceUnit,
                            Theme = d.Theme,
                            Title = d.Title
                        });
                    }
                });
            }

            var groupPIPS = PIPs
                .GroupBy(d => new
                {
                    GroupBy = d.ToBsonDocument().GetString(GroupBy),
                    Completion = Convert.ToString(d.Completion)
                })
                .Select(d => new
                {
                    GroupBy = d.Key.GroupBy,
                    Completion = d.Key.Completion,
                    Plan = d.Sum(e => GetDayOrCost(e, DayOrCost, "LE")),
                    IsPositive = (d.FirstOrDefault() ?? new WFPIPElement()).CalculateIsPositive()
                })
                .ToList();

            var gapTQ = 0.0;
            var dataTQ = groupPIPS.GroupBy(d => d.GroupBy)
                .Select(d =>
                {
                    gapTQ += d.Sum(e => e.Plan);

                    var result = new WaterfallItemByRealised()
                    {
                        Title = d.Key == "BsonNull" ? "All Others (P)" : d.Key,
                        IsPositive = d.FirstOrDefault().IsPositive
                    };

                    foreach (var eachSubGroup in d.GroupBy(e => e.Completion == "Realized" ? "Realised" : "Unrealised"))
                    {
                        if (eachSubGroup.Key == "Realised")
                            result.Realised = eachSubGroup.Sum(e => e.Plan);
                        else
                            result.Unrealised = eachSubGroup.Sum(e => e.Plan);
                    }

                    if (IncludeCR)
                    {
                        var crElementsReduction = CRPIPs
                            .Where(f => f.ToBsonDocument().GetString(GroupBy).Equals(d.Key));

                        try
                        {
                            result.CRRealised = crElementsReduction
                                .Where(f => f.Completion.Equals("Realized"))
                                .Sum(f => GetDayOrCost(f, DayOrCost, "LE"));
                        }
                        catch (Exception e) { }
                        try
                        {
                            result.CRUnrealised = crElementsReduction
                                .Where(f => !f.Completion.Equals("Realized"))
                                .Sum(f => GetDayOrCost(f, DayOrCost, "Plan"));
                        }
                        catch (Exception e) { }
                    }

                    result.Realized = (result.CRRealised + result.Realised);
                    result.Unrealized = (result.CRUnrealised + result.Unrealised);

                    return result;
                })
                .Where(d => (!IncludeZero && (d.Realized != 0 || d.Unrealized != 0)) || IncludeZero)
                .ToList();

            if (GroupBy.Equals("Classification") && IncludeZero)
            {
                var visiblePIPs = dataTQ.Select(d => d.Title).Distinct();
                WellPIPClassifications.Populate<WellPIPClassifications>().ForEach(d =>
                {
                    if (visiblePIPs.Any(e => e.Equals(d.Name)))
                        return;

                    dataTQ.Add(new WaterfallItemByRealised() { Title = d.Name });
                });
            }

            var smRls = groupPIPS.Where(x => !x.Completion.ToLower().Equals("realized")).Select(x => x.Plan).Sum();
            var unr = allLE - Math.Abs(smRls);

            return new WaterfallPIPResultByRealised()
            {
                OP = OP,
                AFE = AFE,
                LE = allLE,
                TQ = TQ,
                Unrisk = unr,
                Gaps = allLE - (gapTQ + OP),
                Items = dataTQ
            };
        }

        public JsonResult GetPIPClassification()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                List<string> Name = new List<string>();
                var wpc = WellPIPClassifications.Populate<WellPIPClassifications>();
                foreach (var x in wpc)
                {
                    Name.Add(x.Name);
                }
                return Name.ToArray();
            });
        }

        public JsonResult GetActionParty(string PIPId, int ElementId)
        {
            try
            {
                IMongoQuery q = Query.EQ("_id", PIPId);

                WellPIP wp = WellPIP.Get<WellPIP>(q);

                //PIPElement element = new PIPElement();
                var Elements = wp.Elements.Where(x => x.ElementId == ElementId).FirstOrDefault();

                var ActionParties = new List<WEISPersonInfo>();
                if (Elements.ActionParties != null && Elements.ActionParties.Count > 0)
                {
                    foreach (var x in Elements.ActionParties)
                    {
                        var WPI = new WEISPersonInfo();
                        WPI.FullName = x.FullName == null ? "" : x.FullName;
                        WPI.Email = x.Email == null ? "" : x.Email;
                        WPI.RoleId = x.RoleId == null ? "" : x.RoleId;
                        ActionParties.Add(WPI);
                    }
                }
                Elements.ActionParties = ActionParties;

                return Json(new { Success = true, Data = Elements }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult SaveActionParty(string PIPId, int ElementId, List<WEISPersonInfo> ActionParties)
        {
            try
            {

                var query = Query.EQ("_id", PIPId);
                WellPIP wp = WellPIP.Get<WellPIP>(query);
                WellPIP temp = WellPIP.Get<WellPIP>(query);

                var OriginalPIP = wp.Elements;
                List<PIPElement> UpdatedPIP = new List<PIPElement>();

                PIPElement pe = new PIPElement();

                foreach (var i in OriginalPIP)
                {
                    if (i.ElementId == ElementId)
                    {
                        // if found match ElementId
                        pe = i;
                        pe.ActionParties = ActionParties;
                        UpdatedPIP.Add(pe);
                    }
                    else
                    {
                        UpdatedPIP.Add(i);
                    }
                }

                wp.Elements = UpdatedPIP;

                wp.Save();
                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                    LogType.Update, wp.TableName, url, temp.ToBsonDocument(), wp.ToBsonDocument());
     
            
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult SaveComment(string PIPId, int ElementId, int ParentId, string Comment)
        {
            try
            {
                string User = WebTools.LoginUser.UserName;
                string Email = WebTools.LoginUser.Email;
                string FullName = WebTools.LoginUser.UserName;
                string ReferenceType = "WellPIP";
                string Reference1 = PIPId;
                string Reference2 = ElementId.ToString();


                WEISComment wc = new WEISComment();
                wc.User = User;
                wc.Email = Email;
                wc.FullName = FullName;
                wc.ReferenceType = ReferenceType;
                wc.Reference1 = Reference1;
                wc.Reference2 = Reference2;
                wc.Comment = Comment;
                wc.ParentId = ParentId;

                wc.Save();
                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                    LogType.Insert, wc.TableName, url, wc.ToBsonDocument(), null);
     
            
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult DeleteComment(int id)
        {
            try
            {
                var removed = WEISComment.Get<WEISComment>(Query.EQ("_id", id));
                WEISComment.Get<WEISComment>(Query.EQ("_id", id)).Delete();

                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email, LogType.Delete, removed.TableName, url, removed.ToBsonDocument(), null);
         

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult GetComment(string PIPId, int ElementId, bool isRead = false)
        {
            var q = Query.Null;
            List<IMongoQuery> qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("ReferenceType", "WellPIP"));
            qs.Add(Query.EQ("Reference1", PIPId));
            qs.Add(Query.EQ("Reference2", ElementId.ToString()));
            q = Query.And(qs);
            List<WEISComment> wc = WEISComment.Populate<WEISComment>(q);

            var pip = WellPIP.Get<WellPIP>(Query.EQ("_id", PIPId));
            if (pip != null)
            {
                var wellName = pip.WellName;
                var qwau = Query.And(Query.EQ("WellName", wellName), Query.EQ("Phase.ActivityType", pip.ActivityType));
                var sort = new SortByBuilder().Descending("LastUpdate");
                var au = WellActivityUpdate.Populate<WellActivityUpdate>(q: qwau, sort: sort);
                if (au == null) au = new List<WellActivityUpdate>();

                if (au.Count > 0)
                {
                    var auids = au.Select(d => d._id.ToString());
                    var qpip = new List<IMongoQuery>();
                    qpip.Add(Query.EQ("ReferenceType", "WeeklyReport"));
                    qpip.Add(Query.In("Reference1", new BsonArray(auids)));
                    qpip.Add(Query.EQ("Reference2", ElementId.ToString()));

                    var pc = WEISComment.Populate<WEISComment>(Query.And(qpip));

                    if (pc != null)
                    {
                        if (pc.Count > 0)
                        {
                            wc = wc.Concat(pc).ToList();
                        }
                    }
                }
            }

            if (isRead)
            {
                new CommentController().setReadComment(wc);
            }

            return Json(new { Data = wc.OrderByDescending(x=>x.LastUpdate) }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult ResetSingleAllocation(string PIPId, int ElementId)
        {
            return MvcResultInfo.Execute(() =>
            {
                #region reset single
                //string WellName = "PRINCESS P8";
                //string SequenceId = "3062";
                //string ActivityType = "WHOLE COMPLETION EVENT";
                //int ElementId = 3;

                List<IMongoQuery> qs = new List<IMongoQuery>();
                IMongoQuery q = Query.EQ("_id",PIPId);


                //qs.Add(Query.EQ("WellName", WellName));
                //qs.Add(Query.EQ("ActivityType", ActivityType));
                //qs.Add(Query.EQ("SequenceId", SequenceId));
                //q = Query.And(qs);

                List<PIPElement> ListElement = new List<PIPElement>();
                PIPElement Element = new PIPElement();
                List<PIPAllocation> Allocation = new List<PIPAllocation>();
                WellPIP NewPIP = new WellPIP();

                WellPIP pip = WellPIP.Get<WellPIP>(q);
                NewPIP = pip;
                foreach (var x in pip.Elements)
                {
                    Element = x;
                    if (Element.ElementId == ElementId)
                    {
                        Element.Allocations = null;
                    }
                    ListElement.Add(Element);
                }
                NewPIP.Elements = ListElement;
                NewPIP.Save();

                #endregion
                return "OK";
            });
        }

    }

    class WaterfallPIPResultByRealised
    {
        public double OP { get; set; }
        public double AFE { get; set; }
        public double LE { get; set; }
        public double TQ { get; set; }
        public double Gaps { get; set; }
        public double Unrisk { get; set; }
        public List<WaterfallItemByRealised> Items { get; set; }
    }
}