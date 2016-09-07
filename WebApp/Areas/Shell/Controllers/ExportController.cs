using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core.DataSerializer;
using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.IO;

using Aspose.Cells;
using Aspose.Pdf;
using Aspose.Pdf.Generator;
using System.Text;
using System.Globalization;
using MongoDB.Bson.Serialization;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class ExportController : Controller
    {
        static IMongoQuery ProcessOperand(string fields, string Operand, string Value)
        {
            IMongoQuery qWellAc = Query.Null;
            //List<IMongoQuery> qs1 = new List<IMongoQuery>();
            switch (Operand)
            {
                case "Equal":
                    {
                        qWellAc = Query.EQ(fields, Value);
                        break;
                    }
                case "Not Equal":
                    {
                        qWellAc = Query.NE(fields, Value);
                        break;
                    }
                case "Greater Than":
                    {
                        qWellAc = Query.GT(fields, Value);
                        break;
                    }
                case "Lower Than":
                    {
                        qWellAc = Query.LT(fields, Value);
                        break;
                    }
                default: // Like
                    {
                        qWellAc = Query.Matches(fields, BsonRegularExpression.Create(new Regex(Value, RegexOptions.IgnoreCase)));
                        break;
                    }
            }

            return qWellAc;
        }


        public IMongoQuery GetQuery(string scr)
        {
            IMongoQuery qWellAc = Query.Null;

            List<IMongoQuery> list = new List<IMongoQuery>();

            int i = 1;

            List<string> Operand = new List<string>();
            foreach (string x in scr.Split(','))
            {
                if (i % 2 != 0)
                {
                    if (x.Trim() != string.Empty && !x.Trim().Contains("|||"))
                    {
                        string frst = x.Split('|')[0];
                        string scnd = x.Split('|')[1];
                        string thrd = x.Split('|')[2];

                        qWellAc = ProcessOperand(frst, scnd, thrd);

                        list.Add(qWellAc);
                    }
                }
                else
                {
                    if (x.Trim() != string.Empty)
                    {
                        // get and/or operator
                        Operand.Add(x);
                    }
                }
                i++;
            }

            int y = 0;

            if (Operand.Count > 0)
            {
                IMongoQuery qsNew = Query.Null;
                foreach (string x in Operand)
                {
                    if (x.Equals("AND"))
                    {
                        qsNew = qsNew == null ? Query.And(list[y], list[y + 1]) : Query.And(qsNew, list[y], list[y + 1]);
                    }
                    else
                    {
                        qsNew = qsNew == null ? Query.Or(list[y], list[y + 1]) : Query.Or(qsNew, list[y], list[y + 1]);
                    }

                    y++;
                }

                return qsNew;
            }
            else
                return qWellAc;
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

        //
        // GET: /Shell/Export/
        public ActionResult CreateDefinition()
        {
            ViewBag.isReadOnly = "0";
            if (_isReadOnly())
            {
                ViewBag.isRO = "1";
            }
            return PartialView();
            //return View();
        }
        //
        // GET: /Shell/Export/
        public ActionResult Index()
        {
            ViewBag.isReadOnly = "0";
            if (_isReadOnly())
            {
                ViewBag.isRO = "1";
            }
            return View();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ActionResult Save(string Name, string CollectioName, int? _id = -1, string OutputPath = "", string Query = "",
            List<string> Fields = null, List<string> Rows = null, string DataPoint = null,
            List<string> Columns = null, string Period = null, string PeriodGroup = null,
            string PeriodMode = "", string PeriodValue = "", string Orientation = "", string OrderBy = ""
        )
        {
            Fields = (Fields == null ? new List<string>() : Fields);
            Definition d = new Definition();

            d._id = _id <= 0 ? 0 : _id;
            d.Name = Name;
            d.OutputPath = OutputPath;
            d.CollectioName = "WEISWellActivityUpdates";
            d.Fields = new List<string>().ToArray();
            d.Query = Query;
            d.Period = Period;
            d.OrderBy = OrderBy;
            d.PeriodGroup = PeriodGroup;
            d.Columns = Columns;
            d.Rows = Rows;
            d.DataPoint = DataPoint;
            d.PeriodMode = PeriodMode;
            d.PeriodValue = PeriodValue;
            d.Orientation = Orientation;
            d.OwnerName = WebTools.LoginUser.UserName;

            if (_id == 0)
            {
                if (Definition.Populate<Definition>(MongoDB.Driver.Builders.Query.EQ("Name", Name)).Count > 0)
                    return Json(new { Result = "NOK" }, JsonRequestBehavior.AllowGet);

                DataHelper.Save(d.TableName, d.ToBsonDocument());
                return Json(new
                {
                    Result = "OK",
                    Data = GetDefinition(MongoDB.Driver.Builders.Query.EQ("Name", Name))
                }, JsonRequestBehavior.AllowGet);
            }

            DataHelper.Save(d.TableName, d.PreSave(d.ToBsonDocument()));
            return Json(new
            {
                Result = "OK",
                Data = GetDefinition(MongoDB.Driver.Builders.Query.EQ("Name", Name))
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 
        /// </summary>
        public JsonResult Preview(int id)
        {
            Definition definition = DataHelper.Populate<Definition>("ExportDef", Query.EQ("_id", id)).FirstOrDefault();
            var data = new List<BsonDocument>();

            try
            {
                data = DataExporter.UnwindData(ECIS.Core.DataSerializer.DataExporter.ExecuteDef(definition));
                if (data == null) data = new List<BsonDocument>();
                if (data.Count > 0) data = data.Take(10).ToList();
            }
            catch (Exception e)
            {

            }

            var result = new
            {
                Columns = definition.Fields,
                Data = DataHelper.ToDictionaryArray(data)
            };

            var u = result.ToBsonDocument();

            if (definition.CollectioName.Equals("WEISWellActivityUpdates"))
            {
                DataPointType dataPointType = DataPointType.ALLOCATION;

                if (definition.DataPoint.ToUpper().Equals("EVENT"))
                    dataPointType = DataPointType.EVENT;
                else if (definition.DataPoint.ToUpper().Equals("PIP"))
                    dataPointType = DataPointType.PIP;

                var dprRaw = DataExporterResultGenerator(dataPointType, definition);
                var dpr = new List<Dictionary<string, object>>();

                if (definition.PeriodGroup.Equals("Daily"))
                {
                    dpr = dprRaw.Select(d => d.ToBsonDocument().ToDictionary()).ToList();
                }
                else if (definition.PeriodGroup.Equals("Weekly"))
                {
                    dpr = DataPointResultCalculateWeekly(dprRaw)
                        .Select(d => d.ToBsonDocument().ToDictionary()).ToList();
                }
                else if (definition.PeriodGroup.Equals("Monthly"))
                {
                    dpr = DataPointResultCalculateMonthly(dprRaw)
                        .Select(d => d.ToBsonDocument().ToDictionary()).ToList();
                }
                else if (definition.PeriodGroup.Equals("Annualy"))
                {
                    dpr = DataPointResultCalculateAnnual(dprRaw)
                        .Select(d => d.ToBsonDocument().ToDictionary()).ToList();
                }

                u.Set("Data", new BsonArray(dpr));
                //u.Set("Data", new BsonArray(DataHelper.Populate("DataPointResult2", Query.EQ("DefIdRef", 11))));
            }

            return Json(u.ToDictionary(), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 
        /// </summary>
        public JsonResult CreateQuery(string scratch)
        {

            //string scr = "Gov ID,Greater Than,12312,AND,EDM ID,Greater Than,314234,AND,OPS Sequence ID,Like,235235,,,,,,,,,";
            //var ret = "";
            IMongoQuery qs = GetQuery(scratch);



            return Json(qs.ToString(), JsonRequestBehavior.AllowGet);
        }

        private Dictionary<string, object> GetDefinition(IMongoQuery query)
        {
            var def = DataHelper.Get<Definition>("ExportDef", query) ?? new Definition();
            var canEdit = false;

            if (def.OwnerName == WebTools.LoginUser.UserName)
                canEdit = true;
            
            var user = DataHelper.Get("Users", Query.EQ("UserName", (def.OwnerName ?? ""))) ?? new BsonDocument();
            
            WEISPerson.GetRolesByEmail(user.GetString("Email")).ForEach(d => {
                if (canEdit) return;

                if (d.ToLower().Equals("app-supports") || d.ToLower().Equals("administrators")) {
                    canEdit = true;
                    return;
                }
            });

            if (!canEdit) def = new Definition();

            var res = def.ToBsonDocument();
            res.Set("CanEdit", canEdit);

            return res.ToDictionary();
        }

        public JsonResult Detail(int id)
        {
            var t = GetDefinition(Query.EQ("_id", id));
            return Json(t, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Download
        /// </summary>
        public ActionResult Download(int id)
        {
            Definition definition = DataHelper.Populate<Definition>("ExportDef", Query.EQ("_id", id)).FirstOrDefault();
            var data = new List<BsonDocument>();
            string csv = "";

            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("PsSchedule.Start", "Daily");
            //definition.Series = dic;
            try
            {
                data = DataExporter.UnwindData(ECIS.Core.DataSerializer.DataExporter.ExecuteDef(definition));

                BsonDocument doc = new BsonDocument();
                doc.Contains("");

                if (data == null) data = new List<BsonDocument>();
                if (data.Count > 0) data = data.Take(10).ToList();
            }
            catch (Exception e)
            {

            }

            try
            {
                csv = DataExporter.ToCSVFormat(data, definition.Fields.ToList());
            }
            catch (Exception e)
            {
                csv = string.Join(",", definition.Fields.ToArray());
            }

            Response.Clear();
            Response.ContentType = "text/CSV";

            if (definition.Name == null || definition.Name.Trim() == "")
            {
                Response.AddHeader("Content-Disposition", "attachment;filename=" + DateTime.Now.ToString("yyyyMMdd") + ".csv");
            }
            else
            {
                Response.AddHeader("Content-Disposition", "attachment;filename=" + definition.Name.Trim().Replace(" ", "") + ".csv");
            }

            Response.Write(csv);
            Response.Flush();
            Response.End();

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="definitions"></param>
        public void Delete(int id)
        {
            var data = DataHelper.Populate("ExportDef", Query.EQ("_id", id));
            if (data != null)
                DataHelper.Delete("ExportDef", Query.EQ("_id", id));
        }

        /// <summary>
        /// FilterIn / Contains
        /// </summary>
        /// <param name="filters"></param>
        /// <param name="fieldname"></param>
        /// <returns></returns>
        public static BsonDocument FilterIn(List<string> filters, string fieldname)
        {
            BsonArray arr = new BsonArray();
            foreach (string s in filters)
            {
                arr.Add(s);
            }
            var match = new BsonDocument
                                {
                                    { "$match", new BsonDocument 
                                         {{ fieldname, new BsonDocument {
                                                {"$in", arr }
                                          }}}
                                    }
                                };
            return match;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_id"></param>
        /// <returns></returns>
        public JsonResult Search(string ListName, DateTime? CreateDate)
        {

            List<IMongoQuery> qs = new List<IMongoQuery>();

            List<string> fName = new List<string>();
            if (ListName != null && !string.IsNullOrEmpty(ListName))
                qs.Add(Query.Matches("Name", BsonRegularExpression.Create(new Regex(ListName, RegexOptions.IgnoreCase))));

            if (CreateDate != null)
            {

                qs.Add(

                Query.And(
                    Query.GTE("CreateDate", Tools.ToUTC(new DateTime(Convert.ToDateTime(CreateDate).Year, Convert.ToDateTime(CreateDate).Month, Convert.ToDateTime(CreateDate).Day))),
                    Query.LTE("CreateDate", Tools.ToUTC(new DateTime(Convert.ToDateTime(CreateDate).Year, Convert.ToDateTime(CreateDate).Month, Convert.ToDateTime(CreateDate).AddDays(1).Day)))
                    )
                    );
            }
            var ret = new List<Definition>();
            if (qs.Count > 0)
            {
                ret = DataHelper.Populate<Definition>("ExportDef", Query.Or(qs.ToArray()));
            }
            else
            {
                ret = DataHelper.Populate<Definition>("ExportDef");
            }

            return Json(new { Data = ret, Total = ret.Count() }, JsonRequestBehavior.AllowGet);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="TableName"></param>
        /// <returns></returns>
        public JsonResult GetFields(string TableName)
        {
            if (TableName.Trim() != "")
            {
                var table = DataHelper.Get("TablesAndFields", Query.EQ("_id", TableName));
                var fields = table.Get("Fields").AsBsonArray.Select(d => d.ToString());
                var isHistorical = false;
                var rows = new List<string>();
                var columns = new List<string>();
                var periods = new List<string>();
                var periodGroups = new string[] { "Daily", "Weekly", "Monthly", "Annualy" }.ToList();
                var orientations = new string[] { "Horizontal", "Vertical" }.ToList();

                if (TableName.Equals("WEISWellActivityUpdates"))
                {
                    var dataPointsPhase = Enum.GetNames(typeof(WAUPhaseDataPoint))
                        .Select(d => "Phase-" + d).ToList();
                    var dataPointsElement = Enum.GetNames(typeof(WAUElementDataPoint))
                        .Select(d => "Element-" + d).ToList();
                    var dataPointsElementAllocation = Enum.GetNames(typeof(WAUElementAllocationDataPoint))
                        .Select(d => "ElementAllocation-" + d).ToList();

                    isHistorical = true;
                    periods = new string[] { "UpdateVersion" }.ToList();
                    rows = new string[] { "WellName", "ActivityType" }.ToList();
                    columns = dataPointsPhase.Concat(dataPointsElement).Concat(dataPointsElementAllocation).ToList();
                }

                var res = new
                {
                    IsHistorical = isHistorical,
                    Data = fields,
                    Rows = rows,
                    Columns = columns,
                    Periods = periods,
                    PeriodGroups = periodGroups,
                    Orientations = orientations
                };

                return Json(res, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Data = new List<string>() }, JsonRequestBehavior.AllowGet);
        }

        public static void TestDatExporterWEIS()
        {
            //Definition f = DataHelper.Populate<Definition>("ExportDef", Query.EQ("_id", 6)).FirstOrDefault();
            //DataExporterResultGenerator(DataPointType.EVENT, f); 
            //Definition f = DataHelper.Populate<Definition>("ExportDef", Query.EQ("_id", 9)).FirstOrDefault();
            //DataExporterResultGenerator(DataPointType.PIP, f);
            Definition f = DataHelper.Populate<Definition>("ExportDef", Query.EQ("_id", 10)).FirstOrDefault();
            var t = new ExportController().Requery(f);
            DataPointType dtp = (DataPointType)Enum.Parse(typeof(DataPointType), t.DataPoint);
            new ExportController().DataExporterResultGenerator(dtp, t);
        }



        private List<DateTime> GetDatesInMonth(int year, int month)
        {
            return Enumerable.Range(1, DateTime.DaysInMonth(year, month))
                             .Select(day => new DateTime(year, month, day))
                             .ToList();
        }
        public DateTime FirstDateOfWeek(int year, int weekOfYear, System.Globalization.CultureInfo ci)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = (int)ci.DateTimeFormat.FirstDayOfWeek - (int)jan1.DayOfWeek;
            DateTime firstWeekDay = jan1.AddDays(daysOffset);
            int firstWeek = ci.Calendar.GetWeekOfYear(jan1, ci.DateTimeFormat.CalendarWeekRule, ci.DateTimeFormat.FirstDayOfWeek);
            if (firstWeek <= 1 || firstWeek > 50)
            {
                weekOfYear -= 1;
            }
            return firstWeekDay.AddDays(weekOfYear * 7);
        }
        public Definition Requery(Definition f)
        {
            if (f.PeriodMode == null || f.PeriodMode.Trim().Equals(""))
            {
                var q1 = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(f.Query.Trim().Equals("") ? "{}" : f.Query);
                QueryDocument queryDoc = new QueryDocument(q1);
                f.ResultQuery =  queryDoc;
            }
            else
            {

                PeriodMode pm = (PeriodMode)Enum.Parse(typeof(PeriodMode), f.PeriodMode.ToUpper().Trim().Replace(" ", ""));

                var curQuery = f.Query;
                var perVal = f.PeriodValue;
                var perGroup = f.PeriodGroup;
                var period = f.Period;

                if (pm == PeriodMode.CUSTOM)
                {
                    #region CUSTOM

                    var perValSplit = perVal.Split('|');

                    if (perGroup.ToUpper().Equals("DAILY"))
                    {
                        #region DAILY
                        List<DateTime> listDate = new List<DateTime>();
                        foreach (var t in perValSplit)
                        {
                            DateTime dt = System.Convert.ToDateTime(perValSplit[0]);
                            DateTime getDt = new DateTime(dt.Year, dt.Month, dt.Day);

                            listDate.Add(Tools.ToUTC(getDt));
                        }
                        IMongoQuery qsx = Query.In(f.Period, new BsonArray(listDate)); //Query.And(Query.GTE(f.Period, Tools.ToUTC(ResultRunningPeriod)), Query.LT(f.Period, Tools.ToUTC(RightNow.AddDays(1))));
                        f.ResultQuery = qsx;
                        #endregion
                    }
                    else if (perGroup.ToUpper().Equals("WEEKLY"))
                    {
                        #region WEEKLY
                        List<DateTime> listDateAll = new List<DateTime>();

                        foreach (var t in perValSplit)
                        {
                            var year = System.Convert.ToInt32(t.Substring(0, 4));
                            int week = System.Convert.ToInt32(t.Substring(4, 2));

                            CultureInfo ciCurr = CultureInfo.CurrentCulture;

                            DateTime firstWeekDay = FirstDateOfWeek(year, week, ciCurr);
                            listDateAll.Add(firstWeekDay);
                            for (int i = 1; i < 7; i++) // loop 7
                            {
                                listDateAll.Add(firstWeekDay.AddDays(i));
                            }
                        }
                        IMongoQuery qsx = Query.In(f.Period, new BsonArray(listDateAll));
                        f.ResultQuery = qsx;
                        #endregion

                    }
                    else if (perGroup.ToUpper().Equals("MONTHLY"))
                    {
                        #region MONTHLY
                        List<DateTime> listDateAll = new List<DateTime>();

                        foreach (var t in perValSplit)
                        {
                            var listDate = GetDatesInMonth(System.Convert.ToInt32(t.Substring(0, 4)), System.Convert.ToInt32(t.Substring(4, 2)));
                            foreach (DateTime d in listDate)
                                listDateAll.Add(d);
                        }
                        IMongoQuery qsx = Query.In(f.Period, new BsonArray(listDateAll));
                        f.ResultQuery = qsx;
                        #endregion
                    }
                    else
                    {
                        #region ANNUAL
                        // ANNUAL
                        List<DateTime> listDateAll = new List<DateTime>();
                        List<IMongoQuery> qsxf = new List<IMongoQuery>();
                        foreach (var t in perValSplit)
                        {
                            int year = System.Convert.ToInt32(t);
                            DateTime From = new DateTime(year, 1, 1);
                            DateTime To = new DateTime(year, 12, 31);

                            IMongoQuery qTemp = Query.And(Query.GT(f.Period, From), Query.LTE(f.Period, To));
                            qsxf.Add(qTemp);
                        }
                        IMongoQuery qsx = Query.Null;
                        if (qsxf.Count() > 0)
                            qsx = Query.Or(qsxf.ToArray());

                        f.ResultQuery = qsx;
                        #endregion
                    }

                    #endregion
                }
                else if (pm == PeriodMode.RANGE)
                {
                    #region RANGE

                    var perValSplit = perVal.Split('|');
                    DateTime From = System.Convert.ToDateTime(perValSplit[0]);
                    DateTime To = System.Convert.ToDateTime(perValSplit[1]);

                    IMongoQuery qsx = Query.And(Query.GTE(f.Period, Tools.ToUTC(From)), Query.LT(f.Period, Tools.ToUTC(To.AddDays(1))));
                    f.ResultQuery = qsx;

                    #endregion
                }
                else if (pm == PeriodMode.RUNNINGPERIOD)
                {
                    #region RUNNINGPERIOD
                    DateTime RightNow = DateTime.Now;
                    int PeriodValueInt = System.Convert.ToInt32(perVal);
                    DateTime ResultRunningPeriod = new DateTime();
                    if (perGroup.ToUpper().Equals("DAILY"))
                    {
                        ResultRunningPeriod = RightNow.AddDays(-1 * PeriodValueInt);
                    }
                    else if (perGroup.ToUpper().Equals("WEEKLY"))
                    {
                        //int Year = 0;
                        //int thisweek = GetWeekOfYear(RightNow, out Year);
                        ResultRunningPeriod = RightNow.AddDays(-1 * 7 * PeriodValueInt);

                    }
                    else if (perGroup.ToUpper().Equals("MONTHLY"))
                    {
                        ResultRunningPeriod = RightNow.AddMonths(-1 * PeriodValueInt);
                    }
                    else
                    {
                        // ANNUAL
                        ResultRunningPeriod = RightNow.AddYears(-1 * PeriodValueInt);
                    }

                    IMongoQuery qsx = Query.And(Query.GTE(f.Period, Tools.ToUTC(ResultRunningPeriod)), Query.LT(f.Period, Tools.ToUTC(RightNow.AddDays(1))));
                    f.ResultQuery = qsx;

                    #endregion
                }
                else if (pm == PeriodMode.QTD)
                {
                    #region QTD

                    int quarterNumber = (DateTime.Now.Month - 1) / 3 + 1;
                    DateTime firstDayOfQuarter = new DateTime(DateTime.Now.Year, (quarterNumber - 1) * 3 + 1, 1);

                    DateTime From = firstDayOfQuarter;
                    DateTime To = DateTime.Now;

                    IMongoQuery qsx = Query.And(Query.GTE(f.Period, Tools.ToUTC(From)), Query.LT(f.Period, Tools.ToUTC(To.AddDays(1))));
                    f.ResultQuery = qsx;
                    #endregion
                }
                else
                {
                    #region YTD

                    int Year = DateTime.Now.Year;

                    DateTime From = new DateTime(DateTime.Now.Year, 1, 1);
                    DateTime To = DateTime.Now;

                    IMongoQuery qsx = Query.And(Query.GTE(f.Period, Tools.ToUTC(From)), Query.LT(f.Period, Tools.ToUTC(To.AddDays(1))));
                    f.ResultQuery = qsx;
                    #endregion
                }

                List<IMongoQuery> queries = new List<IMongoQuery>();
                var q1 = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(f.Query.Trim().Equals("") ? "{}" : f.Query);
                QueryDocument queryDoc = new QueryDocument(q1);
                var q2 = f.ResultQuery;
                queries.Add(queryDoc);
                queries.Add(q2);

                IMongoQuery query = Query.Null;
                if (queries.Count() > 0)
                    query = Query.And(queries.ToArray());

                f.ResultQuery = query;
            }

            return f;
        }
        public List<DataPointResult> DataExporterResultGenerator(DataPointType DataPointType, Definition f)
        {
            List<BsonDocument> datas = new List<BsonDocument>();
            f = Requery(f);
            BsonDocument query = (f.ResultQuery == null) ? null : MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(f.ResultQuery.ToString());
            QueryDocument queryDoc = new QueryDocument(query);

            List<string> populatedColls = new List<string>();
            populatedColls.Add(f.Period);
            foreach (var r in f.Rows)
                populatedColls.Add(r);

            List<Helper> GroupOperators = new List<Helper>();
            foreach (var t in f.Columns)
            {
                var ttt = new Helper();
                string[] stringSeparators = new string[] { "Of" };
                var y = t.Split(stringSeparators, StringSplitOptions.None);
                ttt.Key = y[0].Trim();
                ttt.Value = y[1].Trim();
                GroupOperators.Add(ttt);
                //populatedColls.Add(y[1].Trim());
            }

            List<DataPointResult> res = new List<DataPointResult>();

            switch (DataPointType)
            {
                case DataPointType.EVENT:
                    {
                        #region Phases Event
                        /* 
                         - (EVENT) WEISWellActivityUpdates.Phase
	                        TQ
	                        AFE
	                        OP
	                        LE
                         */
                        #endregion
                        #region DataPointType.EVENT
                        var WAU = DataHelper.Populate("WEISWellActivityUpdates", queryDoc);

                        if (WAU != null && WAU.Count > 0)
                        {
                            populatedColls.Add("_id");
                            //var uwind = BsonHelper.Unwind(WAU.Where(x => x.Elements.Count() > 0).ToList(), "Elements", "", populatedColls.ToList<string>());


                            DataHelper.Delete("_temp_dp_event");
                            DataHelper.Save("_temp_dp_event", WAU.ToList());

                            List<BsonDocument> pipes = new List<BsonDocument>();

                            if (populatedColls.Contains("_id"))
                                populatedColls.Remove("_id");

                            string Query = DataPointGroupBuilder(populatedColls, GroupOperators);
                            pipes.Add(BsonSerializer.Deserialize<BsonDocument>(Query));
                            var RigDist = DataHelper.Aggregate("_temp_dp_event", pipes);


                            foreach (var data in RigDist)
                            {
                                DataPointResult dp = new DataPointResult();
                                dp.DefIdRef = f._id;

                                var BsonArr = BsonHelper.GetDoc(data, "_id");

                                List<string> resultBy = new List<string>();
                                List<string> rName = new List<string>();

                                DateTime DateValue = new DateTime();
                                foreach (var y in BsonArr.Elements)
                                {
                                    if (y.Value.BsonType != BsonType.DateTime)
                                    {
                                        resultBy.Add(y.Name);
                                        rName.Add(y.Value.ToString());
                                    }
                                    else if (y.Value.BsonType == BsonType.DateTime)
                                    {
                                        DateValue = Tools.ToUTC(y.Value.AsDateTime);
                                    }
                                }

                                dp.ResultBy = string.Join("|", resultBy);
                                dp.RowName = string.Join("|", rName);

                                // set Period 
                                dp.Periods = new List<DataPointResultPeriod>();

                                DataPointResultPeriod per = new DataPointResultPeriod();
                                per.Values = new List<DataPointResultValue>();

                                per.DateGroup = DateValue;
                                foreach (var go in GroupOperators)
                                {
                                    DataPointResultValue v = new DataPointResultValue();
                                    v.Title = go.Value;
                                    v.Value = BsonHelper.GetDouble(data, go.Key.ToLower() + go.Value);
                                    v.Operand = go.Key.ToLower();
                                    per.Values.Add(v);
                                }
                                dp.Periods.Add(per);

                                dp.DefIdRef = f._id;
                                dp.Type = f.DataPoint;
                                res.Add(dp);
                            }
                        }
                        #region old
                        //var WAU = DataHelper.Populate("WEISWellActivityUpdates", queryDoc);

                        //if (WAU != null && WAU.Count > 0)
                        //{
                        //    foreach (string r in f.Rows)
                        //    {
                        //        var byCategory = WAU.GroupBy(x => BsonHelper.GetString(x, r));
                        //        foreach (var cat in byCategory)
                        //        {
                        //            DataPointResult dp = new DataPointResult();
                        //            dp.Type = DataPointType.EVENT.ToString();
                        //            dp.ResultBy = r.ToString();
                        //            dp.RowName = cat.Key;

                        //            var dataByPeriod = cat.ToList().GroupBy(x => BsonHelper.GetDateTime(x, f.Period));  // group by period
                        //            foreach (var per in dataByPeriod)
                        //            {
                        //                DataPointResultPeriod pi = new DataPointResultPeriod();
                        //                pi.DateGroup = per.Key;

                        //                List<DataPointResultValue> resultPerPeriod = new List<DataPointResultValue>();
                        //                foreach (var y in per.ToList())
                        //                {
                        //                    foreach (var z in GroupOperators)
                        //                    {
                        //                        var item = new DataPointResultValue();
                        //                        item.Title = z.Value;
                        //                        item.Value = BsonHelper.GetDouble(y, z.Value);

                        //                        pi.Values.Add(item);
                        //                        resultPerPeriod.Add(item);
                        //                    }
                        //                }
                        //                dp.Periods.Add(pi);
                        //            }
                        //            var resultWeek = DataPointCalculation.CalculateWeekly(dp, GroupOperators);
                        //            var resultMonth = DataPointCalculation.CalculateMonthly(dp, GroupOperators);
                        //            var ResultAnnual = DataPointCalculation.CalculateAnnual(dp, GroupOperators);
                        //            dp.WeeklyResult = resultWeek;
                        //            dp.MonthlyResult = resultMonth;
                        //            dp.AnnualResult = ResultAnnual;
                        //            dp.DefIdRef = f._id;
                        //            res.Add(dp);
                        //        }

                        //    }
                        //}
                        #endregion
                        #endregion
                        break;
                    }
                case DataPointType.PIP:
                    {
                        #region PIP
                        /* 
                         - (PIP - Elements) WEISWellActivityUpdates.Elements
	                        DaysPlanImprovement
	                        DaysPlanRisk
	                        DaysActualImprovement
	                        DaysActualRisk
	                        DaysLastWeekImprovement
	                        DaysLastWeekRisk
	                        DaysCurrentWeekImprovement
	                        DaysCurrentWeekRisk
	                        CostPlanImprovement
	                        CostPlanRisk
	                        CostActualImprovement
	                        CostActualRisk
	                        CostCurrentWeekImprovement
	                        CostCurrentWeekRisk
	                        CostLastWeekImprovement
	                        CostLastWeekRisk
                         */
                        #endregion
                        #region DataPointType.PIP
                        var WAU = DataHelper.Populate("WEISWellActivityUpdates", queryDoc);
                        List<string> PopulatedColMinDot = new List<string>();
                        if (WAU != null && WAU.Count > 0)
                        {
                            populatedColls.Add("_id");

                            foreach (var t in populatedColls)
                            {
                                if (t.Contains("."))
                                {
                                    var y = t.Split('.');

                                    foreach (var w in WAU)
                                    {
                                        var value = BsonHelper.GetString(w, t);
                                        w.Set(y[1], value);
                                    }
                                    PopulatedColMinDot.Add(y[1]);
                                }
                                else
                                {
                                    PopulatedColMinDot.Add(t);
                                }
                            }

                            var uwind = BsonHelper.Unwind(WAU.Where(x => x.Elements.Count() > 0).ToList(), "Elements", "", PopulatedColMinDot.ToList<string>());


                            DataHelper.Delete("_temp_dp_pip");
                            DataHelper.Save("_temp_dp_pip", uwind.ToList());

                            List<BsonDocument> pipes = new List<BsonDocument>();

                            if (PopulatedColMinDot.Contains("_id"))
                                PopulatedColMinDot.Remove("_id");

                            string Query = DataPointGroupBuilder(PopulatedColMinDot, GroupOperators);
                            pipes.Add(BsonSerializer.Deserialize<BsonDocument>(Query));
                            var RigDist = DataHelper.Aggregate("_temp_dp_pip", pipes);


                            foreach (var data in RigDist)
                            {
                                DataPointResult dp = new DataPointResult();
                                dp.DefIdRef = f._id;

                                var BsonArr = BsonHelper.GetDoc(data, "_id");

                                List<string> resultBy = new List<string>();
                                List<string> rName = new List<string>();

                                DateTime DateValue = new DateTime();
                                foreach (var y in BsonArr.Elements)
                                {
                                    if (y.Value.BsonType != BsonType.DateTime)
                                    {
                                        resultBy.Add(y.Name);
                                        rName.Add(y.Value.ToString());
                                    }
                                    else if (y.Value.BsonType == BsonType.DateTime)
                                    {
                                        DateValue = Tools.ToUTC(y.Value.AsDateTime);
                                    }
                                }

                                dp.ResultBy = string.Join("|", resultBy);
                                dp.RowName = string.Join("|", rName);

                                // set Period 
                                dp.Periods = new List<DataPointResultPeriod>();

                                DataPointResultPeriod per = new DataPointResultPeriod();
                                per.Values = new List<DataPointResultValue>();

                                per.DateGroup = DateValue;
                                foreach (var go in GroupOperators)
                                {
                                    DataPointResultValue v = new DataPointResultValue();
                                    v.Title = go.Value;
                                    v.Value = BsonHelper.GetDouble(data, go.Key.ToLower() + go.Value);
                                    v.Operand = go.Key.ToLower();
                                    per.Values.Add(v);
                                }
                                dp.Periods.Add(per);

                                dp.DefIdRef = f._id;
                                dp.Type = f.DataPoint;
                                res.Add(dp);
                            }
                        }
                        #region old
                        //var WAU = DataHelper.Populate("WEISWellActivityUpdates", queryDoc);

                        //if (WAU != null && WAU.Count > 0)
                        //{
                        //    var uwind = BsonHelper.Unwind(WAU.Where(x => x.Elements.Count() > 0).ToList(), "Elements", "", populatedColls.ToList<string>());

                        //    foreach (string r in f.Rows) // by category (wellname erc)
                        //    {

                        //        var byCategory = uwind.GroupBy(x => BsonHelper.GetString(x, r));
                        //        foreach (var cat in byCategory)
                        //        {
                        //            DataPointResult dp = new DataPointResult();
                        //            dp.Type = DataPointType.PIP.ToString();
                        //            dp.ResultBy = r.ToString();

                        //            dp.RowName = cat.Key;

                        //            var dataByPeriod = cat.ToList().GroupBy(x => BsonHelper.GetDateTime(x, f.Period));  // group by period
                        //            foreach (var per in dataByPeriod)
                        //            {
                        //                DataPointResultPeriod pi = new DataPointResultPeriod();
                        //                pi.DateGroup = per.Key;

                        //                List<DataPointResultValue> resultPerPeriod = new List<DataPointResultValue>();

                        //                var ddrv = new List<DataPointResultValue>();
                        //                foreach (var y in per.ToList())
                        //                {
                        //                    foreach (var z in GroupOperators)
                        //                    {
                        //                        var item = new DataPointResultValue();
                        //                        item.Title = z.Value;
                        //                        item.Value = BsonHelper.GetDouble(y, z.Value);

                        //                        ddrv.Add(item);
                        //                    }
                        //                }

                        //                var ddv = ddrv.GroupBy(x => x.Title);

                        //                foreach (var d in ddv)
                        //                {
                        //                    DataPointResultValue rrr = new DataPointResultValue();
                        //                    rrr.Value = d.ToList().Sum(x => x.Value);
                        //                    rrr.Title = d.Key;
                        //                    //resultPerPeriod.Add(rrr);
                        //                    pi.Values.Add(rrr);
                        //                }
                        //                dp.Periods.Add(pi);

                        //            }
                        //            var resultWeek = DataPointCalculation.CalculateWeekly(dp, GroupOperators);
                        //            var resultMonth = DataPointCalculation.CalculateMonthly(dp, GroupOperators);
                        //            var ResultAnnual = DataPointCalculation.CalculateAnnual(dp, GroupOperators);
                        //            dp.WeeklyResult = resultWeek;
                        //            dp.MonthlyResult = resultMonth;
                        //            dp.AnnualResult = ResultAnnual;
                        //            dp.DefIdRef = f._id;
                        //            res.Add(dp);
                        //        }

                        //    }

                        //}
                        #endregion
                        #endregion
                        break;
                    }
                default: // Allocation
                    {
                        #region Phases Allocation
                        /* 
                         - (PIP - Elements- Allocation) WEISWellActivityUpdates.Elements.Allocations
	                        DaysPlanImprovement
	                        DaysPlanRisk
	                        CostPlanImprovement
	                        CostPlanRisk
	                        LEDays
	                        LECost
                         */
                        #endregion
                        #region DataPointType.Allocation
                        var WAU = DataHelper.Populate("WEISWellActivityUpdates", queryDoc);

                        if (WAU != null && WAU.Count > 0)
                        {
                            populatedColls.Add("_id");

                            List<string> PopulatedColMinDot = new List<string>();

                            foreach (var t in populatedColls)
                            {
                                if (t.Contains("."))
                                {
                                    var y = t.Split('.');

                                    foreach (var w in WAU)
                                    {
                                        var value = BsonHelper.GetString(w, t);
                                        w.Set(y[1], value);
                                    }
                                    PopulatedColMinDot.Add(y[1]);
                                }
                                else
                                {
                                    PopulatedColMinDot.Add(t);
                                }
                            }
                            var uwind = BsonHelper.Unwind(WAU.Where(x => x.Elements.Count() > 0).ToList(), "Elements", "", PopulatedColMinDot.ToList<string>());

                            var uwindAll = BsonHelper.Unwind(uwind, "Allocations", "", PopulatedColMinDot.ToList<string>());

                            DataHelper.Delete("_temp_alloc");
                            DataHelper.Save("_temp_alloc", uwindAll.ToList());

                            List<BsonDocument> pipes = new List<BsonDocument>();

                            if (PopulatedColMinDot.Contains("_id"))
                                PopulatedColMinDot.Remove("_id");

                            string Query = DataPointGroupBuilder(PopulatedColMinDot, GroupOperators);
                            pipes.Add(BsonSerializer.Deserialize<BsonDocument>(Query));
                            var RigDist = DataHelper.Aggregate("_temp_alloc", pipes);


                            foreach (var data in RigDist)
                            {
                                DataPointResult dp = new DataPointResult();
                                dp.DefIdRef = f._id;

                                var BsonArr = BsonHelper.GetDoc(data, "_id");

                                List<string> resultBy = new List<string>();
                                List<string> rName = new List<string>();

                                DateTime DateValue = new DateTime();
                                foreach (var y in BsonArr.Elements)
                                {
                                    if (y.Value.BsonType != BsonType.DateTime)
                                    {
                                        resultBy.Add(y.Name);
                                        rName.Add(y.Value.ToString());
                                    }
                                    else if (y.Value.BsonType == BsonType.DateTime)
                                    {
                                        DateValue = Tools.ToUTC(y.Value.AsDateTime);
                                    }
                                }

                                dp.ResultBy = string.Join("|", resultBy);
                                dp.RowName = string.Join("|", rName);

                                // set Period 
                                dp.Periods = new List<DataPointResultPeriod>();

                                DataPointResultPeriod per = new DataPointResultPeriod();
                                per.Values = new List<DataPointResultValue>();

                                per.DateGroup = DateValue;
                                foreach (var go in GroupOperators)
                                {
                                    DataPointResultValue v = new DataPointResultValue();
                                    v.Title = go.Value;
                                    v.Value = BsonHelper.GetDouble(data, go.Key.ToLower() + go.Value);
                                    v.Operand = go.Key.ToLower();
                                    per.Values.Add(v);
                                }
                                dp.Periods.Add(per);

                                dp.DefIdRef = f._id;
                                dp.Type = f.DataPoint;
                                res.Add(dp);
                            }
                            #region old
                            //foreach (string r in f.Rows) // by category (wellname erc)
                            //{


                            //uwindAll.GroupBy(r=>  new  )

                            ////var byCategory = uwind.GroupBy(x => BsonHelper.GetString(x, r));
                            ////foreach (var cat in byCategory)
                            ////{
                            ////    DataPointResult dp = new DataPointResult();
                            ////    dp.Type = DataPointType.ALLOCATION.ToString();
                            ////    dp.ResultBy = r.ToString();

                            ////    dp.RowName = cat.Key;

                            ////    var dataByPeriod = cat.ToList().GroupBy(x => BsonHelper.GetDateTime(x, f.Period));  // group by period
                            ////    foreach (var per in dataByPeriod)
                            ////    {
                            ////        DataPointResultPeriod pi = new DataPointResultPeriod();
                            ////        pi.DateGroup = per.Key;

                            ////        List<DataPointResultValue> resultPerPeriod = new List<DataPointResultValue>();

                            ////        var ddrv = new List<DataPointResultValue>();
                            ////        foreach (var y in per.ToList())
                            ////        {
                            ////            foreach (var z in GroupOperators)
                            ////            {
                            ////                var item = new DataPointResultValue();
                            ////                item.Title = z.Value;
                            ////                item.Value = BsonHelper.GetDouble(y, z.Value);

                            ////                ddrv.Add(item);
                            ////            }
                            ////        }

                            ////        var ddv = ddrv.GroupBy(x => x.Title);

                            ////        foreach (var d in ddv)
                            ////        {
                            ////            DataPointResultValue rrr = new DataPointResultValue();
                            ////            rrr.Value = d.ToList().Sum(x => x.Value);
                            ////            rrr.Title = d.Key;
                            ////            //resultPerPeriod.Add(rrr);
                            ////            pi.Values.Add(rrr);
                            ////        }
                            ////        dp.Periods.Add(pi);

                            ////    }
                            ////    var resultWeek = DataPointCalculation.CalculateWeekly(dp, GroupOperators);
                            ////    var resultMonth = DataPointCalculation.CalculateMonthly(dp, GroupOperators);
                            ////    var ResultAnnual = DataPointCalculation.CalculateAnnual(dp, GroupOperators);
                            ////    dp.WeeklyResult = resultWeek;
                            ////    dp.MonthlyResult = resultMonth;
                            ////    dp.AnnualResult = ResultAnnual;
                            ////    dp.DefIdRef = f._id;
                            ////    res.Add(dp);
                            ////}

                            //}
                            #endregion

                        }
                        #endregion
                        break;
                    }
            }

            //foreach (var r in res)
            //{
            //    DataHelper.Save("DataPointResult2", r.ToBsonDocument());
            //}
            return res;

        }
        public int GetWeekOfYear(DateTime dt, out int Year)
        {
            CultureInfo ciCurr = CultureInfo.CurrentCulture;
            int weekNum = ciCurr.Calendar.GetWeekOfYear(dt, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            Year = dt.Year;
            return weekNum;
        }
        public List<DataPointAnnualResult> DataPointResultCalculateAnnual(List<DataPointResult> DataPointResultDaily)
        {
            List<BsonDocument> per = new List<BsonDocument>();
            List<DataPointAnnualResult> resW = new List<DataPointAnnualResult>();

            int _id = 1;
            foreach (var dt in DataPointResultDaily)
            {
                int Year = System.Convert.ToInt32(dt.Periods.FirstOrDefault().DateGroup.ToString("yyyy"));
                var outx = dt.ToBsonDocument();

                //Year.ToString() + Year.ToString("00");

                outx.Set("Year", System.Convert.ToInt32(Year.ToString()));
                outx.Set("_id", _id);
                _id++;
                per.Add(outx);
            }

            var per1 = BsonHelper.Unwind(per, "Periods", "", new List<string> { "DefIdRef", "RowName", "ResultBy", "Type", "Year" });

            foreach (var p in per1)
            {
                var _idx = BsonHelper.GetInt32(p, "_parentid");
                p.Remove("_parentid");
                p.Set("_id", _idx);
            }

            var per2 = BsonHelper.Unwind(per1, "Values", "", new List<string> { "DefIdRef", "RowName", "ResultBy", "Type", "Year" });

            var resWeek = per2.GroupBy(x => BsonHelper.GetInt32(x, "Year"));
            foreach (var w in resWeek.ToList())
            {
                //dwpr.
                int key = w.Key;
                var vals = w.ToList();

                foreach (var rn in vals.GroupBy(x => BsonHelper.GetString(x, "RowName")))
                {
                    string keyRowName = rn.Key;
                    var valsRownName = rn.ToList();
                    foreach (var Titles in valsRownName.GroupBy(x => BsonHelper.GetString(x, "Title")))
                    {
                        string kk = Titles.Key;
                        var rrss = Titles.ToList();

                        foreach (var operands in rrss.GroupBy(x => BsonHelper.GetString(x, "Operand"))) // Operand
                        {
                            DataPointAnnualResult dwpr = new DataPointAnnualResult();

                            string opKey = operands.Key;
                            var operandsList = operands.ToList();

                            dwpr.Title = keyRowName + "|" + kk;
                            dwpr.Operand = opKey;

                            if (opKey.Equals("sum"))
                                dwpr.Value = operandsList.ToList().Sum(x => BsonHelper.GetDouble(x, "Value"));
                            else
                                dwpr.Value = operandsList.ToList().Average(x => BsonHelper.GetDouble(x, "Value"));
                            dwpr.ResultBy = BsonHelper.GetString(operandsList.FirstOrDefault(), "ResultBy");
                            dwpr.Type = BsonHelper.GetString(operandsList.FirstOrDefault(), "Type");
                            dwpr.Year = key;// BsonHelper.GetInt32(operandsList.FirstOrDefault(), "WeekId");
                            resW.Add(dwpr);
                            //DataHelper.Save("_sample_annual", dwpr.ToBsonDocument());
                        }

                    }
                }

            }

            return resW;
        }
        public List<DataPointMonthlyResult> DataPointResultCalculateMonthly(List<DataPointResult> DataPointResultDaily)
        {
            List<BsonDocument> per = new List<BsonDocument>();
            List<DataPointMonthlyResult> resW = new List<DataPointMonthlyResult>();

            int _id = 1;
            foreach (var dt in DataPointResultDaily)
            {
                int Year = 0;
                int WeekOfYear = System.Convert.ToInt32(dt.Periods.FirstOrDefault().DateGroup.ToString("yyyyMM"));
                var outx = dt.ToBsonDocument();

                string MonthId = Year.ToString() + WeekOfYear.ToString("00");

                outx.Set("MonthId", System.Convert.ToInt32(MonthId));
                outx.Set("_id", _id);
                _id++;
                per.Add(outx);
            }

            var per1 = BsonHelper.Unwind(per, "Periods", "", new List<string> { "DefIdRef", "RowName", "ResultBy", "Type", "MonthId" });

            foreach (var p in per1)
            {
                var _idx = BsonHelper.GetInt32(p, "_parentid");
                p.Remove("_parentid");
                p.Set("_id", _idx);
            }

            var per2 = BsonHelper.Unwind(per1, "Values", "", new List<string> { "DefIdRef", "RowName", "ResultBy", "Type", "MonthId" });

            var resWeek = per2.GroupBy(x => BsonHelper.GetInt32(x, "MonthId"));
            foreach (var w in resWeek.ToList())
            {
                //dwpr.
                int key = w.Key;
                var vals = w.ToList();

                foreach (var rn in vals.GroupBy(x => BsonHelper.GetString(x, "RowName")))
                {
                    string keyRowName = rn.Key;
                    var valsRownName = rn.ToList();
                    foreach (var Titles in valsRownName.GroupBy(x => BsonHelper.GetString(x, "Title")))
                    {
                        string kk = Titles.Key;
                        var rrss = Titles.ToList();

                        foreach (var operands in rrss.GroupBy(x => BsonHelper.GetString(x, "Operand"))) // Operand
                        {
                            DataPointMonthlyResult dwpr = new DataPointMonthlyResult();

                            string opKey = operands.Key;
                            var operandsList = operands.ToList();

                            dwpr.Title = keyRowName + "|" + kk;
                            dwpr.Operand = opKey;

                            if (opKey.Equals("sum"))
                                dwpr.Value = operandsList.ToList().Sum(x => BsonHelper.GetDouble(x, "Value"));
                            else
                                dwpr.Value = operandsList.ToList().Average(x => BsonHelper.GetDouble(x, "Value"));
                            dwpr.ResultBy = BsonHelper.GetString(operandsList.FirstOrDefault(), "ResultBy");
                            dwpr.Type = BsonHelper.GetString(operandsList.FirstOrDefault(), "Type");
                            dwpr.Month = key;// BsonHelper.GetInt32(operandsList.FirstOrDefault(), "WeekId");
                            resW.Add(dwpr);
                            //DataHelper.Save("_sample_monthly", dwpr.ToBsonDocument());
                        }
                    }
                }

            }

            return resW;
        }
        public List<DataPointWeeklyResult> DataPointResultCalculateWeekly(List<DataPointResult> DataPointResultDaily)
        {
            List<BsonDocument> per = new List<BsonDocument>();
            List<DataPointWeeklyResult> resW = new List<DataPointWeeklyResult>();


            int _id = 1;
            foreach (var dt in DataPointResultDaily)
            {
                int Year = 0;
                int WeekOfYear = GetWeekOfYear(dt.Periods.FirstOrDefault().DateGroup, out Year);
                var outx = dt.ToBsonDocument();

                string YearWeek = Year.ToString() + WeekOfYear.ToString("00");

                outx.Set("WeekId", System.Convert.ToInt32(YearWeek));
                outx.Set("_id", _id);
                _id++;
                per.Add(outx);
            }

            var per1 = BsonHelper.Unwind(per, "Periods", "", new List<string> { "DefIdRef", "RowName", "ResultBy", "Type", "WeekId" });

            foreach (var p in per1)
            {
                var _idx = BsonHelper.GetInt32(p, "_parentid");
                p.Remove("_parentid");
                p.Set("_id", _idx);
            }

            var per2 = BsonHelper.Unwind(per1, "Values", "", new List<string> { "DefIdRef", "RowName", "ResultBy", "Type", "WeekId" });

            var resWeek = per2.GroupBy(x => BsonHelper.GetInt32(x, "WeekId"));
            foreach (var w in resWeek.ToList())
            {
                //dwpr.
                int key = w.Key;
                var vals = w.ToList();

                foreach (var rn in vals.GroupBy(x => BsonHelper.GetString(x, "RowName")))
                {
                    string keyRowName = rn.Key;
                    var valsRownName = rn.ToList();
                    foreach (var Titles in valsRownName.GroupBy(x => BsonHelper.GetString(x, "Title")))
                    {
                        string kk = Titles.Key;
                        var rrss = Titles.ToList();

                        foreach (var operands in rrss.GroupBy(x => BsonHelper.GetString(x, "Operand"))) // Operand
                        {
                            DataPointWeeklyResult dwpr = new DataPointWeeklyResult();

                            string opKey = operands.Key;
                            var operandsList = operands.ToList();

                            dwpr.Title = keyRowName + "|" + kk;
                            dwpr.Operand = opKey;

                            if (opKey.Equals("sum"))
                                dwpr.Value = operandsList.ToList().Sum(x => BsonHelper.GetDouble(x, "Value"));
                            else
                                dwpr.Value = operandsList.ToList().Average(x => BsonHelper.GetDouble(x, "Value"));
                            dwpr.ResultBy = BsonHelper.GetString(operandsList.FirstOrDefault(), "ResultBy");
                            dwpr.Type = BsonHelper.GetString(operandsList.FirstOrDefault(), "Type");

                            dwpr.WeekTitle = key;// BsonHelper.GetInt32(operandsList.FirstOrDefault(), "WeekId");
                            resW.Add(dwpr);
                            //DataHelper.Save("_sample_weekly", dwpr.ToBsonDocument());
                        }

                    }
                }
            }
            return resW;
        }
        public string DataPointGroupBuilder(List<string> rows, List<Helper> clOperands)
        {
            string header = @"{
		                            $group : 
			                            { 
				                            _id : 
				                            { ";
            string bodyHeader = @""; // isi
            string closeHeader = @" }, ";
            string bodyGroup = @""; // isi 
            string closeGroup = @"}
                            }";
            StringBuilder sheader = new StringBuilder();
            foreach (var r in rows)
            {
                sheader.Append(r.Replace(".", "_") + ":" + "'$" + r + "',");
            }
            bodyHeader = sheader.ToString();

            StringBuilder sbody = new StringBuilder();
            foreach (var r in clOperands)
            {
                //clOperands.Where(x=>x.Value)

                sbody.Append(r.Key.ToLower() + r.Value.ToString().Replace(".", "_") + ":{$" + r.Key.ToLower() + ":'$" + r.Value.ToString() + "'},");
            }
            bodyGroup = sbody.ToString();

            StringBuilder sb = new StringBuilder();

            sb.Append(header);
            sb.Append(bodyHeader);
            sb.Append(closeHeader);
            sb.Append(bodyGroup);
            sb.Append(closeGroup);


            return sb.ToString();
        }



        public FileResult ExcelExport(int id = 11)
        {
            Definition f = DataHelper.Populate<Definition>("ExportDef", Query.EQ("_id", id)).FirstOrDefault();
            string type = f.Orientation;
            var DPType = DataPointType.ALLOCATION;
            if (f.DataPoint == "EVENT")
            {
                DPType = DataPointType.EVENT;
            }
            if (f.DataPoint == "PIP")
            {
                DPType = DataPointType.PIP;
            }

            var PeriodGroup = f.PeriodGroup;

            var res = DataExporterResultGenerator(DPType, f);

            List<DataPointAnnualResult> AnnualResult = new List<DataPointAnnualResult>();
            List<DataPointMonthlyResult> MonthlyResult = new List<DataPointMonthlyResult>();
            List<DataPointWeeklyResult> WeeklyResult = new List<DataPointWeeklyResult>();

            List<string> rowKeyTitle = f.Rows;
            List<string> ColumnHeader = f.Columns;
            List<string> ListPeriods = new List<string>();
            List<DataExcelHorizontal> RowContents = new List<DataExcelHorizontal>();
            #region get Daily Data
            if (PeriodGroup == "Daily")
            {
                foreach (var r in res)
                {
                    ListPeriods.Add(r.Periods.FirstOrDefault().DateGroup.ToString("MMM, dd yyyy"));
                    var newRowContent = new DataExcelRowContent();
                    var content = new List<DataPointResultValue>();
                    newRowContent.Period = r.Periods.FirstOrDefault().DateGroup.ToString("MMM, dd yyyy");
                    foreach (var a in r.Periods.FirstOrDefault().Values)
                    {
                        content.Add(a);
                    }
                    newRowContent.Content = content;

                    var checkRowContents = RowContents.Where(x => x.RowName == r.RowName).FirstOrDefault();
                    if (checkRowContents == null)
                    {
                        // add data to list
                        var newRow = new DataExcelHorizontal();
                        newRow.RowName = r.RowName;
                        newRow.Content = new List<DataExcelRowContent>();
                        newRow.Content.Add(newRowContent);
                        RowContents.Add(newRow);
                    }
                    else
                    {
                        // update RowContents
                        checkRowContents.Content.Add(newRowContent);
                    }
                }
            }
            #endregion
            else
            {
                #region get Monthly Data
                if (PeriodGroup == "Monthly")
                {
                    MonthlyResult = DataPointResultCalculateMonthly(res);
                    foreach (var mr in MonthlyResult)
                    {
                        var period = mr.Month.ToString();
                        var month = period.Substring(4, 2);
                        var year = period.Substring(0, 4);
                        var dt = new DateTime(Convert.ToInt32(year), Convert.ToInt32(month), 01);
                        var fixPeriod = dt.ToString("MMM-yyyy");
                        ListPeriods.Add(fixPeriod);

                        var splitTitle = mr.Title.Split('|');
                        var RowName = "";
                        for (var i = 0; i < rowKeyTitle.Count; i++)
                        {
                            RowName += splitTitle[i] + "|";
                        }
                        RowName = RowName.Trim('|');

                        var newRowContent = new DataExcelRowContent();
                        newRowContent.Period = fixPeriod;
                        var newContent = new DataPointResultValue() { Operand = mr.Operand, Title = splitTitle[rowKeyTitle.Count], Value = mr.Value };
                        var content = new List<DataPointResultValue>() { newContent };
                        newRowContent.Content = content;
                        var checkRowContents = RowContents.Where(x => x.RowName == RowName).FirstOrDefault();
                        if (checkRowContents == null)
                        {
                            // add data to list
                            var newRow = new DataExcelHorizontal();
                            newRow.RowName = RowName;
                            newRow.Content = new List<DataExcelRowContent>();
                            newRow.Content.Add(newRowContent);
                            RowContents.Add(newRow);
                        }
                        else
                        {
                            // update RowContents
                            //check the same period
                            var checkRowPeriod = checkRowContents.Content.Where(x => x.Period == fixPeriod).FirstOrDefault();
                            if (checkRowPeriod == null)
                            {
                                checkRowContents.Content.Add(newRowContent);
                            }
                            else
                            {
                                checkRowPeriod.Content.Add(newContent);
                            }
                        }

                    }
                }
                #endregion

                #region get Annually Data
                if (PeriodGroup == "Annually")
                {
                    AnnualResult = DataPointResultCalculateAnnual(res);
                    foreach (var mr in AnnualResult)
                    {
                        var fixPeriod = mr.Year.ToString();
                        ListPeriods.Add(fixPeriod);

                        var splitTitle = mr.Title.Split('|');
                        var RowName = "";
                        for (var i = 0; i < rowKeyTitle.Count; i++)
                        {
                            RowName += splitTitle[i] + "|";
                        }
                        RowName = RowName.Trim('|');

                        var newRowContent = new DataExcelRowContent();
                        newRowContent.Period = fixPeriod;
                        var newContent = new DataPointResultValue() { Operand = mr.Operand, Title = splitTitle[rowKeyTitle.Count], Value = mr.Value };
                        var content = new List<DataPointResultValue>() { newContent };
                        newRowContent.Content = content;
                        var checkRowContents = RowContents.Where(x => x.RowName == RowName).FirstOrDefault();
                        if (checkRowContents == null)
                        {
                            // add data to list
                            var newRow = new DataExcelHorizontal();
                            newRow.RowName = RowName;
                            newRow.Content = new List<DataExcelRowContent>();
                            newRow.Content.Add(newRowContent);
                            RowContents.Add(newRow);
                        }
                        else
                        {
                            // update RowContents
                            //check the same period
                            var checkRowPeriod = checkRowContents.Content.Where(x => x.Period == fixPeriod).FirstOrDefault();
                            if (checkRowPeriod == null)
                            {
                                checkRowContents.Content.Add(newRowContent);
                            }
                            else
                            {
                                checkRowPeriod.Content.Add(newContent);
                            }
                        }

                    }
                }
                #endregion

                #region get Weekly Data
                if (PeriodGroup == "Weekly")
                {
                    WeeklyResult = DataPointResultCalculateWeekly(res);
                    foreach (var mr in WeeklyResult)
                    {
                        var period = mr.WeekTitle.ToString();
                        var week = period.Substring(4, 2);
                        var year = period.Substring(0, 4);
                        var fixPeriod = "Week #" + week + " " + year;

                        ListPeriods.Add(fixPeriod);

                        var splitTitle = mr.Title.Split('|');
                        var RowName = "";
                        for (var i = 0; i < rowKeyTitle.Count; i++)
                        {
                            RowName += splitTitle[i] + "|";
                        }
                        RowName = RowName.Trim('|');

                        var newRowContent = new DataExcelRowContent();
                        newRowContent.Period = fixPeriod;
                        var newContent = new DataPointResultValue() { Operand = mr.Operand, Title = splitTitle[rowKeyTitle.Count], Value = mr.Value };
                        var content = new List<DataPointResultValue>() { newContent };
                        newRowContent.Content = content;
                        var checkRowContents = RowContents.Where(x => x.RowName == RowName).FirstOrDefault();
                        if (checkRowContents == null)
                        {
                            // add data to list
                            var newRow = new DataExcelHorizontal();
                            newRow.RowName = RowName;
                            newRow.Content = new List<DataExcelRowContent>();
                            newRow.Content.Add(newRowContent);
                            RowContents.Add(newRow);
                        }
                        else
                        {
                            // update RowContents
                            //check the same period
                            var checkRowPeriod = checkRowContents.Content.Where(x => x.Period == fixPeriod).FirstOrDefault();
                            if (checkRowPeriod == null)
                            {
                                checkRowContents.Content.Add(newRowContent);
                            }
                            else
                            {
                                checkRowPeriod.Content.Add(newContent);
                            }
                        }
                    }
                }
                #endregion

            }

            var Periods = ListPeriods.Distinct().OrderBy(i => i).ToList();

            List<string> ColumnHeaders = new List<string>();

            foreach (var i in Periods)
            {
                ColumnHeaders.AddRange(ColumnHeader);
            }

            string fileName = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                 String.Format("DE-" + DateTime.Now.ToString("MMM-dd-yyy") + ".xlsx")); ;
            string fileNameDownload = renderExcelFile(type, rowKeyTitle, ColumnHeader, ColumnHeaders, Periods, RowContents, fileName);

            return File(fileName, Tools.GetContentType(".xlsx"), fileNameDownload);
        }

        private string renderExcelFile(string type, List<string> rowKeyTitle, List<string> ColumnHeader, List<string> ColumnHeaders, List<string> Periods, List<DataExcelHorizontal> RowContents, string fileName)
        {
            string template = Path.Combine(Server.MapPath("~/App_Data/Temp"), "DataExtractorTemplate.xlsx");
            Workbook workbookTemplate = new Workbook(template);
            var worksheetTemplate = workbookTemplate.Worksheets.FirstOrDefault();
            if (System.IO.File.Exists(template) == false)
                throw new Exception("Template file is not exist: " + template);

            int sheet = 0;
            var currentDate = DateTime.Now.ToString("MMM, dd yyyy");
            var ws = workbookTemplate.Worksheets[0];
            ws.Cells["D4"].Value = currentDate;

            #region preparing style
            Style style = workbookTemplate.CreateStyle();
            style.VerticalAlignment = TextAlignmentType.Center;
            style.HorizontalAlignment = TextAlignmentType.Center;
            style.ShrinkToFit = true;
            style.Borders[BorderType.BottomBorder].LineStyle = CellBorderType.Thin;
            style.Borders[BorderType.TopBorder].LineStyle = CellBorderType.Thin;
            style.Borders[BorderType.RightBorder].LineStyle = CellBorderType.Thin;
            style.Borders[BorderType.LeftBorder].LineStyle = CellBorderType.Thin;
            style.Font.IsBold = true;

            //Creating StyleFlag
            StyleFlag styleFlagHeader = new StyleFlag();
            styleFlagHeader.HorizontalAlignment = true;
            styleFlagHeader.VerticalAlignment = true;
            styleFlagHeader.ShrinkToFit = true;
            styleFlagHeader.Borders = true;
            styleFlagHeader.FontBold = true;

            //Creating StyleFlag for content
            StyleFlag styleFlagContent = new StyleFlag();
            styleFlagContent.HorizontalAlignment = false;
            styleFlagContent.VerticalAlignment = true;
            styleFlagContent.ShrinkToFit = false;
            styleFlagContent.Borders = true;
            styleFlagContent.FontBold = false;
            #endregion

            int colRowKeyStart = 2;
            int rowKeyTitleStart = 7;
            foreach (var r in rowKeyTitle)
            {
                var cell = GetColNameFromIndex(colRowKeyStart) + rowKeyTitleStart.ToString();
                ws.Cells[cell].Value = r;
                ws.Cells[cell].SetStyle(style, styleFlagHeader);
                if (type.ToLower() == "horizontal")
                {
                    ws.Cells.Merge(rowKeyTitleStart - 1, colRowKeyStart - 1, 2, 1);
                    ws.Cells[GetColNameFromIndex(colRowKeyStart) + (rowKeyTitleStart + 1).ToString()].SetStyle(style, styleFlagHeader);
                }
                colRowKeyStart++;
            }

            //ws.Cells["B7"].Value = rowKeyTitle;
            #region Horizontal
            if (type.ToLower() == "horizontal")
            {
                // first row = Periods
                // second row = ColumnHeaders
                //ws.Cells["F7"].Value = 
                int mergePeriods = ColumnHeader.Count;

                int colPeriodStart = 2 + rowKeyTitle.Count;
                int rowPeriod = 7;
                foreach (var p in Periods)
                {
                    var cell = GetColNameFromIndex(colPeriodStart) + rowPeriod.ToString();
                    ws.Cells[cell].Value = p;
                    ws.Cells.Merge(6, colPeriodStart - 1, 1, mergePeriods);
                    ws.Cells[cell].SetStyle(style, styleFlagHeader);
                    for (var i = 1; i <= mergePeriods - 1; i++)
                    {
                        ws.Cells[GetColNameFromIndex(colPeriodStart + i) + rowPeriod.ToString()].SetStyle(style, styleFlagHeader);
                    }
                    colPeriodStart = colPeriodStart + mergePeriods;
                }

                int colHeaderStart = 2 + rowKeyTitle.Count;
                int rowHeader = 8;
                foreach (var a in ColumnHeaders)
                {
                    var cell = GetColNameFromIndex(colHeaderStart) + rowHeader.ToString();
                    ws.Cells[cell].Value = a;
                    //ws.Cells.Merge(rowPeriod, colPeriodStart, 1, mergePeriods);
                    ws.Cells[cell].SetStyle(style, styleFlagHeader);
                    ws.AutoFitColumn(colHeaderStart - 1);
                    colHeaderStart++;
                }

                int colContentStart = 1;
                int rowContent = 9;
                foreach (var a in RowContents)
                {
                    //row title
                    var splitRowName = a.RowName.Split('|');
                    for (var i = 0; i < splitRowName.Count(); i++)
                    {
                        colContentStart++;
                        var cell = GetColNameFromIndex(colContentStart) + rowContent.ToString();
                        ws.Cells[cell].Value = splitRowName[i];
                        ws.AutoFitColumn(colContentStart - 1);
                        ws.Cells[cell].SetStyle(style, styleFlagContent);
                    }

                    foreach (var p in Periods)
                    {
                        for (var i = 0; i < ColumnHeader.Count; i++)
                        {
                            colContentStart++;
                            var cell = GetColNameFromIndex(colContentStart) + rowContent.ToString();
                            foreach (var s in a.Content)
                            {
                                if (p == s.Period)
                                {
                                    ws.Cells[cell].Value = s.Content[i].Value;
                                }
                            }
                            ws.Cells[cell].SetStyle(style, styleFlagContent);
                        }
                    }
                    rowContent++;
                    colContentStart = 1;

                }
            } //end horizontal type
            #endregion

            #region Vertical
            else
            {

                ws.Cells[GetColNameFromIndex(2 + rowKeyTitle.Count) + "7"].Value = "Data Point";
                ws.Cells[GetColNameFromIndex(2 + rowKeyTitle.Count) + "7"].SetStyle(style, styleFlagHeader);
                int colPeriodStart = 3 + rowKeyTitle.Count;
                int rowPeriod = 7;
                foreach (var p in Periods)
                {
                    var cell = GetColNameFromIndex(colPeriodStart) + rowPeriod.ToString();
                    ws.Cells[cell].Value = p;
                    ws.Cells[cell].SetStyle(style, styleFlagHeader);
                    ws.AutoFitColumn(colPeriodStart - 1);
                    colPeriodStart++;
                }

                int colContentStart = 2 + rowKeyTitle.Count;
                int rowContentStart = 8;
                int colRowTitle = 2;
                int rowTitleStart = 8;
                int mergeRowTitle = ColumnHeader.Count;
                foreach (var r in RowContents)
                {
                    //row title
                    var splitRowName = r.RowName.Split('|');
                    for (var i = 0; i < splitRowName.Count(); i++)
                    {
                        var cell = GetColNameFromIndex(colRowTitle) + rowTitleStart.ToString();
                        ws.Cells[cell].Value = splitRowName[i];
                        ws.Cells[cell].SetStyle(style, styleFlagContent);
                        ws.AutoFitColumn(colRowTitle - 1);
                        ws.Cells.Merge(rowTitleStart - 1, colRowTitle - 1, mergeRowTitle, 1);
                        for (var j = 1; j <= mergeRowTitle - 1; j++)
                        {
                            ws.Cells[GetColNameFromIndex(colRowTitle) + (rowTitleStart + j).ToString()].SetStyle(style, styleFlagContent);
                        }
                        colRowTitle++;
                    }

                    foreach (var a in ColumnHeader)
                    {
                        var cellDataPoint = GetColNameFromIndex(colContentStart) + rowContentStart.ToString();
                        ws.Cells[cellDataPoint].Value = a;
                        ws.Cells[cellDataPoint].SetStyle(style, styleFlagContent);
                        ws.AutoFitColumn(colContentStart - 1);
                        foreach (var p in Periods)
                        {
                            colContentStart++;
                            var cell = GetColNameFromIndex(colContentStart) + rowContentStart.ToString();
                            foreach (var s in r.Content)
                            {
                                if (p == s.Period)
                                {
                                    //ws.Cells[cell].Value = s.Content[i].Value;
                                    //ws.Cells[cell].Value = s.Content.Where(x => x.Title == a).FirstOrDefault().Value;
                                    foreach (var c in s.Content)
                                    {
                                        if (a.ToLower() == c.Operand.ToLower() + "of" + c.Title.ToLower())
                                        {
                                            ws.Cells[cell].Value = c.Value;

                                        }
                                    }
                                }
                            }
                            ws.Cells[cell].SetStyle(style, styleFlagContent);
                            ws.AutoFitColumn(colContentStart - 1);
                        }
                        rowContentStart++;
                        colContentStart = 2 + rowKeyTitle.Count;
                    }
                    rowTitleStart = rowTitleStart + mergeRowTitle;
                    colRowTitle = 2;
                }
            }
            #endregion


            //ws.AutoFitColumn(1);

            workbookTemplate.Save(fileName, Aspose.Cells.SaveFormat.Xlsx);
            string fileNameDownload = "DataExtractor-" + DateTime.Now.ToString("MMM-dd-yyy") + ".xlsx";
            return fileNameDownload;
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

    }

    internal class DataExcelHorizontal
    {
        public string RowName { get; set; }
        public List<DataExcelRowContent> Content { get; set; }

    }
    internal class DataExcelRowContent
    {
        public string Period { get; set; }
        //public int OtherPeriod { get; set; }
        public List<DataPointResultValue> Content { get; set; }
    }
}