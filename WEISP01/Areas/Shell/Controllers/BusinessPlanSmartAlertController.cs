using ECIS.Client.WEIS;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class BusinessPlanSmartAlertController : Controller
    {
        //
        // GET: /Shell/BusinessPlanSmartAlert/
        public ActionResult Index()
        {
            return View();
        }

        public DateTime GetLastDateUploadedLS()
        {
            var latest1 = DataHelper.Get("LogLatestLS", null, SortBy.Descending("Executed_At"));

            DateTime dd = latest1.GetDateTime("Executed_At");

            return dd;
        }

        public JsonResult GetData(string[] wellNames, string[] rigNames, string dateStart, string dateFinish, bool riskcheck = true, bool inlastuploadls = true, string inlastuploadlsBoth = "",string[] OPs = null, string opRelation = "AND")
        {
            var division = 1000000.0;
            var pstart = Tools.DefaultDate; var pend = Tools.DefaultDate;
            if (dateStart != null)
            {
                pstart = DateTime.ParseExact(dateStart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            }

            if (dateFinish != null)
            {
                pend = DateTime.ParseExact(dateFinish, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            }
            
            string format = "yyyy-MM-dd";
            CultureInfo culture = CultureInfo.InvariantCulture;
            #region old
            //var baseQuery = new DashboardController().GenerateQueryFromFilter(null, null, null, rigNames, null, wellNames, null, null, null);
            
            //IMongoQuery query = null;
            //List<IMongoQuery> queries = new List<IMongoQuery>();
            //List<IMongoQuery> dateQuery = new List<IMongoQuery>();

            ////if (!dateStart.Equals("") && !dateFinish.Equals(""))
            ////    dateQuery.Add(Query.And(
            ////        Query.GTE("LastUpdate", DateTime.ParseExact(dateStart, format, culture)),
            ////        Query.LTE("LastUpdate", DateTime.ParseExact(dateFinish, format, culture))
            ////    ));
            ////else if (!dateStart.Equals(""))
            ////    dateQuery.Add(
            ////        Query.GTE("LastUpdate", DateTime.ParseExact(dateStart, format, culture))
            ////    );
            ////else if (!dateFinish.Equals(""))
            ////    dateQuery.Add(
            ////        Query.LTE("LastUpdate", DateTime.ParseExact(dateFinish, format, culture))
            ////    );

            //if (baseQuery != null)
            //    queries.Add(baseQuery);

            ////if (dateQuery.Count > 0)
            ////    queries.Add(Query.And(dateQuery.ToArray()));

            //if (queries.Count() > 0)
            //    query = Query.And(queries.ToArray());

            //// end filter

            //queries.Add(WebTools.GetWellActivitiesQuery("WellName", ""));

            //new filtr

            #endregion
            List<IMongoQuery> q = new List<IMongoQuery>();
            if (wellNames != null)
            {
                q.Add(Query.In("WellName", new BsonArray(wellNames)));
            }

            if (rigNames != null)
            {
                q.Add(Query.In("RigName", new BsonArray(rigNames)));
            }

            if (OPs != null)
            {
                q.Add(Query.In("Phases.Estimate.SaveToOP", new BsonArray(OPs)));
            }

            if (riskcheck)
            {
                q.Add(Query.EQ("Phases.RiskFlag",true));
            }

            if (dateStart!=null && dateFinish !=null)
                q.Add(Query.And(
                    //Query.GTE("LastUpdate", DateTime.ParseExact(dateStart, format, culture)),
                    //Query.LTE("LastUpdate", DateTime.ParseExact(dateFinish, format, culture))
                    Query.GTE("Phases.PhSchedule.Start", DateTime.ParseExact(dateStart, format, culture)),
                    Query.LTE("Phases.PhSchedule.Finish", DateTime.ParseExact(dateFinish, format, culture))
                ));
            else if (dateStart!=null)
                q.Add(
                    //Query.GTE("LastUpdate", DateTime.ParseExact(dateStart, format, culture))
                    Query.GTE("Phases.PhSchedule.Start", DateTime.ParseExact(dateStart, format, culture))
                );
            else if (dateFinish!=null)
                q.Add(
                    //Query.LTE("LastUpdate", DateTime.ParseExact(dateFinish, format, culture))
                    Query.LTE("Phases.PhSchedule.Finish", DateTime.ParseExact(dateFinish, format, culture))
                );
            //q.Add(WebTools.GetWellActivitiesQuery("WellName", ""));

            
            var _actual = this.LoadSmartAlert2(q, pstart, pend, riskcheck);
            var actual = new List<BsonDocument>();
            var dd =  GetLastDateUploadedLS();
            var lastDateExecutedLS = new TestController().GetLastDateUploadedLS();
            var logLast = DataHelper.Populate("LogLatestLS", Query.And(Query.EQ("Executed_At", Tools.ToUTC(lastDateExecutedLS))));

            if (inlastuploadlsBoth == null || inlastuploadlsBoth == "")
            {
                actual = _actual;
            }
            else
            {
                foreach (var xx in _actual)
                {
                    var well = xx.GetString("WellName");
                    var actType = xx.GetString("ActivityType");
                    var rigNm = xx.GetString("RigName");
                    //var bs = new WaterfallBase().CheckPhaseInLatestLS(well, actType, rigNm, dd);
                    var dataAda = logLast.Any(x =>
                        x.GetString("Well_Name") == well &&
                        x.GetString("Activity_Type") == actType &&
                        x.GetString("Rig_Name") == rigNm
                        );
                    if (inlastuploadls)
                    {
                        if (dataAda)
                        {
                            actual.Add(xx);
                        }
                    }
                    else
                    {
                        if (dataAda == false)
                        {
                            var checkAnyLS = logLast.Any(x =>
                                x.GetString("Well_Name") != well &&
                                x.GetString("Activity_Type") != actType &&
                                x.GetString("Rig_Name") != rigNm
                           );
                            if (checkAnyLS || !logLast.Any())
                                actual.Add(xx);
                        }
                    }
                }
            }
            

            List<string> docs = actual.Select(x => BsonHelper.GetString(x, "RigName")).ToList();
            List<BsonDocument> res = new List<BsonDocument>();
            foreach (string d in docs.Distinct())
            {
                BsonDocument doc = new BsonDocument();
                doc.Set("RigName", d);
                var GAP = actual.Where(x => BsonHelper.GetString(x, "RigName").Equals(d)).Select(c => BsonHelper.GetDouble(c, "GAP")).Sum();
                doc.Set("GAP", GAP);

                var Overlapping = actual.Where(x => BsonHelper.GetString(x, "RigName").Equals(d)).Select(c => BsonHelper.GetDouble(c, "Overlapping")).Sum();
                doc.Set("Overlapping", Overlapping);

                var OPDays = actual.Where(x => BsonHelper.GetString(x, "RigName").Equals(d)).Select(c => BsonHelper.GetDouble(c, "OPDays")).Sum();
                doc.Set("OPDays", OPDays);

                var OPCost = actual.Where(x => BsonHelper.GetString(x, "RigName").Equals(d)).Select(c => BsonHelper.GetDouble(c, "OPCost")).Sum();
                doc.Set("OPCost", OPCost);

                //string datemin = actual.Where(x => BsonHelper.GetString(x, "RigName").Equals(d)).Select(c => BsonHelper.GetDateTime(c, "PhScheduleStart")).Min().ToString("dd-MMM-yy");
                string datemin = actual.Where(x => BsonHelper.GetString(x, "RigName").Equals(d)  ).Select(c => BsonHelper.GetDateTime(c, "PhScheduleStart")).Min().ToString("dd-MMM-yy");
                doc.Set("PhScheduleStart", datemin);

                //string datemax = actual.Where(x => BsonHelper.GetString(x, "RigName").Equals(d) && BsonHelper.GetDateTime(x, "PhScheduleFinish") <= pend).Select(c => BsonHelper.GetDateTime(c, "PhScheduleFinish")).Max().ToString("dd-MMM-yy");
                string datemax = actual.Where(x => BsonHelper.GetString(x, "RigName").Equals(d) ).Select(c => BsonHelper.GetDateTime(c, "PhScheduleFinish")).Max().ToString("dd-MMM-yy");
                doc.Set("PhScheduleFinish", datemax);

                string planmin = actual.Where(x => BsonHelper.GetString(x, "RigName").Equals(d)).Select(c => BsonHelper.GetDateTime(c, "PlanStart")).Min().ToString("dd-MMM-yy");
                doc.Set("PlanStart", planmin);

                string planmax = actual.Where(x => BsonHelper.GetString(x, "RigName").Equals(d)).Select(c => BsonHelper.GetDateTime(c, "PlanFinish")).Max().ToString("dd-MMM-yy");
                doc.Set("PlanFinish", planmax);

                List<string> sequence = actual.Where(x => BsonHelper.GetString(x, "RigName").Equals(d)).Select(x => BsonHelper.GetString(x, "UARigSequenceId")).Distinct().ToList();
                string seq = "";
                for (int i = 0; i <= sequence.Count - 1; i++)
                {
                    if (i == sequence.Count - 1)
                        seq = seq + sequence[i].ToString();
                    else
                        seq = seq + sequence[i].ToString() + ", ";
                }

                doc.Set("Seq", seq);

                res.Add(doc);
            }
            var pmin = Tools.DefaultDate;
            var pmax = Tools.DefaultDate;
            var phmin = Tools.DefaultDate;
            var phmax = Tools.DefaultDate;
            if (res.Count() > 0) {
                phmin = BsonHelper.GetDateTime(res.OrderBy(x => BsonHelper.GetDateTime(x, "PhScheduleStart")).FirstOrDefault(), "PhScheduleStart");
                phmax = BsonHelper.GetDateTime(res.OrderByDescending(x => BsonHelper.GetDateTime(x, "PhScheduleFinish")).FirstOrDefault(), "PhScheduleFinish");

                pmin = BsonHelper.GetDateTime(res.OrderBy(x => BsonHelper.GetDateTime(x, "PlanStart")).FirstOrDefault(), "PlanStart");
                pmax = BsonHelper.GetDateTime(res.OrderByDescending(x => BsonHelper.GetDateTime(x, "PlanFinish")).FirstOrDefault(), "PlanFinish");
            }
            
            return Json(new { Data = DataHelper.ToDictionaryArray(res), pmin = pmin, pmax = pmax, MinDate = phmin, MaxDate = phmax, Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);

           // return Json(new { Data = DataHelper.ToDictionaryArray(res), Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
        }

        public List<BsonDocument> LoadSmartAlert(List<IMongoQuery> query, DateTime? ps = null, DateTime? pn = null, bool riskcheck = true)
        {
            List<BsonDocument> allActivity = new List<BsonDocument>();
            if (query.Count > 0)
            {
                var models = BizPlanActivity.GetAll(Query.And(query));
                foreach (var m in models)
                    allActivity.Add(m.ToBsonDocument());
            }
            else
            {
                var models = BizPlanActivity.GetAll(null);
                foreach (var m in models)
                    allActivity.Add(m.ToBsonDocument());
            }


            IMongoQuery q = Query.Not(Query.Matches("ActivityType", new BsonRegularExpression(
            new Regex("RISK", RegexOptions.IgnoreCase))));



            List<BsonDocument> uwAllActivity = new List<BsonDocument>();
            if (riskcheck == false || query.Contains(q))
            {
                uwAllActivity = BsonHelper.Unwind(allActivity, "Phases", "", new List<string> { "_id", "RigType", "RigName", "WellName", "UARigSequenceId", "PsSchedule" })
                    .Where(x => !BsonHelper.GetString(x, "ActivityType").Contains("RISK")).ToList();
            }
            else
            {
                uwAllActivity = BsonHelper.Unwind(allActivity, "Phases", "", new List<string> { "_id", "RigType", "RigName", "WellName", "UARigSequenceId", "PsSchedule" });
            }

            if (ps != Tools.DefaultDate && pn != Tools.DefaultDate)
            {
                uwAllActivity = uwAllActivity.Where(x => BsonHelper.GetDateTime(x, "PhSchedule.Start") >= ps && BsonHelper.GetDateTime(x, "PhSchedule.Finish") <= pn).ToList();
            }
            //var uwAllActivity = BsonHelper.Unwind(allActivity, "Phases");

            //StringBuilder sb = new StringBuilder();
            //sb.Append("WellActivityId" + "Type" + "," + "RigName" + "," + "WellName" + "," + "UARigSequenceId" + "," + "PhaseNo" + "," + "ActivityType" +
            //    "," + "PhSchedule.Start" +
            //    "," + "PhSchedule.Finish" +
            //    "," + "GAP" +
            //    "," + "Actual.Days" +
            //    "," + "Actual.Cost" +
            //     "," + "OP.Days" +
            //    "," + "OP.Cost" +
            //    "\n");


            List<BsonDocument> lResult = new List<BsonDocument>();

            foreach (var ts in uwAllActivity.Where(y => BsonHelper.GetDateTime(y, "PhSchedule.Start") >= new DateTime(2000, 01, 01)).GroupBy(x => BsonHelper.GetString(x, "RigName")))
            {
                BsonDocument doc = new BsonDocument();

                string prevGapOver = "";

                var start_one = ts.Where(y => BsonHelper.GetDateTime(y, "PhSchedule.Start") >= new DateTime(2000, 01, 01)).OrderBy(x => BsonHelper.GetDateTime(x, "PhSchedule.Start")).Take(1);

                doc.Set("WellActivityId", BsonHelper.GetString(start_one.FirstOrDefault(), "_id"));
                doc.Set("Type", "First");
                doc.Set("RigName", BsonHelper.GetString(start_one.FirstOrDefault(), "RigName"));
                doc.Set("WellName", BsonHelper.GetString(start_one.FirstOrDefault(), "WellName"));
                doc.Set("UARigSequenceId", BsonHelper.GetString(start_one.FirstOrDefault(), "UARigSequenceId"));
                doc.Set("PhaseNo", BsonHelper.GetString(start_one.FirstOrDefault(), "PhaseNo"));
                doc.Set("ActivityType", BsonHelper.GetString(start_one.FirstOrDefault(), "ActivityType"));
                doc.Set("PhScheduleStart", BsonHelper.GetDateTime(start_one.FirstOrDefault(), "PhSchedule.Start"));
                doc.Set("PhScheduleFinish", BsonHelper.GetDateTime(start_one.FirstOrDefault(), "PhSchedule.Finish"));
                doc.Set("PlanStart", BsonHelper.GetDateTime(start_one.FirstOrDefault(), "PlanSchedule.Start"));
                doc.Set("PlanFinish", BsonHelper.GetDateTime(start_one.FirstOrDefault(), "PlanSchedule.Finish"));

                doc.Set("GAP", "0");
                doc.Set("Overlapping", 0);

                doc.Set("ActualDays", BsonHelper.GetDouble(start_one.FirstOrDefault(), "Actual.Days"));
                doc.Set("ActualCost", BsonHelper.GetDouble(start_one.FirstOrDefault(), "Actual.Cost"));
                doc.Set("OPDays", BsonHelper.GetDouble(start_one.FirstOrDefault(), "OP.Days"));
                doc.Set("OPCost", Tools.Div(Convert.ToDouble(BsonHelper.GetDouble(start_one.FirstOrDefault(), "OP.Cost")), 1000000));


                lResult.Add(doc);

                DateTime pstart = new DateTime();
                DateTime pfinish = new DateTime();
                pstart = BsonHelper.GetDateTime(start_one.FirstOrDefault(), "PhSchedule.Start");
                pfinish = BsonHelper.GetDateTime(start_one.FirstOrDefault(), "PhSchedule.Finish");

                //DateTime plstart = new DateTime();
                //DateTime plfinish = new DateTime();
                //plstart = BsonHelper.GetDateTime(start_one.FirstOrDefault(), "PlanSchedule.Start");
                //plfinish = BsonHelper.GetDateTime(start_one.FirstOrDefault(), "PlanSchedule.Finish");


                prevGapOver = string.Format("{0} {1} {2}",
                    BsonHelper.GetString(start_one.FirstOrDefault(), "WellName"),
                    BsonHelper.GetString(start_one.FirstOrDefault(), "UARigSequenceId"),
                    BsonHelper.GetString(start_one.FirstOrDefault(), "ActivityType")
                    );

                DateRange dr = new DateRange(); //Date Range
                DateRange dc = new DateRange(); //Date Range Comparison
                foreach (var order in ts.Where(y => BsonHelper.GetDateTime(y, "PhSchedule.Start") >= new DateTime(2000, 01, 01)).OrderBy(x => BsonHelper.GetDateTime(x, "PhSchedule.Start")).Skip(1))
                {
                    doc = new BsonDocument();
                    DateTime start = BsonHelper.GetDateTime(order, "PhSchedule.Start");
                    DateTime finish = BsonHelper.GetDateTime(order, "PhSchedule.Finish");

                    dr.Start = pstart; dr.Finish = pfinish;
                    dc.Start = start; dc.Finish = finish;
                    double overlappingDays = 0;
                    string msg = "";

                    if (start.Date == pfinish.Date)
                    {
                        // no overlap no gap
                        doc.Set("Overlapping", 0);
                        doc.Set("GAP", 0);
                        doc.Set("OverLapWith", "");
                        doc.Set("GAPWith", "");
                    }
                    else if (start.Date < pfinish.Date)
                    {
                        doc.Set("Overlapping", (-1) * (start - pfinish).TotalDays);
                        doc.Set("GAP", 0);

                        doc.Set("OverLapWith", prevGapOver);
                        doc.Set("GAPWith", "");


                    }
                    else
                    {
                        doc.Set("Overlapping", 0);
                        doc.Set("GAP", (start - pfinish).TotalDays); // ada gap

                        doc.Set("OverLapWith", "");
                        doc.Set("GAPWith", prevGapOver);
                    }

                    //var isOverlapping = DateRangeToMonth.isDateRangeOverlaping(dr, dr,out msg, out overlappingDays);
                    //if (isOverlapping)
                    //{
                    //    doc.Set("Overlapping", overlappingDays);
                    //}
                    //else
                    //{
                    //    doc.Set("Overlapping", 0);
                    //}

                    //DateTime nstart = BsonHelper.GetDateTime(order, "PlanSchedule.Start");
                    //DateTime nfinish = BsonHelper.GetDateTime(order, "PlanSchedule.Finish");

                    doc.Set("WellActivityId", BsonHelper.GetString(order, "_id"));
                    if (start > pfinish.AddDays(1))
                    {
                        doc.Set("Type", "More");
                    }
                    else
                    {
                        doc.Set("Type", "Less");
                    }

                    doc.Set("WellName", BsonHelper.GetString(order, "WellName"));
                    doc.Set("ActivityType", BsonHelper.GetString(order, "ActivityType"));
                    doc.Set("RigName", BsonHelper.GetString(order, "RigName"));
                    doc.Set("PhScheduleStart", BsonHelper.GetDateTime(order, "PhSchedule.Start"));
                    doc.Set("PhScheduleFinish", BsonHelper.GetDateTime(order, "PhSchedule.Finish"));

                    doc.Set("UARigSequenceId", BsonHelper.GetString(order, "UARigSequenceId"));


                    //Days, Cost, Gap, Gap with, Overlap, Overlap With


                    doc.Set("PhaseNo", BsonHelper.GetString(order, "PhaseNo"));

                    doc.Set("PlanStart", BsonHelper.GetDateTime(order, "PlanSchedule.Start"));
                    doc.Set("PlanFinish", BsonHelper.GetDateTime(order, "PlanSchedule.Finish"));

                    doc.Set("ActualDays", BsonHelper.GetDouble(order, "Actual.Days"));
                    doc.Set("ActualCost", BsonHelper.GetDouble(order, "Actual.Cost"));
                    doc.Set("OPDays", BsonHelper.GetDouble(order, "OP.Days"));
                    doc.Set("OPCost", Tools.Div(Convert.ToDouble(BsonHelper.GetDouble(order, "OP.Cost")),1000000));
                    lResult.Add(doc);

                    pstart = start;
                    pfinish = finish;

                    //plstart = nstart;
                    //plfinish = nfinish;

                    prevGapOver = string.Format("{0}\n{1}\n{2}",
                   BsonHelper.GetString(start_one.FirstOrDefault(), "WellName"),
                   BsonHelper.GetString(start_one.FirstOrDefault(), "UARigSequenceId"),
                   BsonHelper.GetString(start_one.FirstOrDefault(), "ActivityType")
                   );
                }
            }

            return lResult;

        }

        public List<BsonDocument> LoadSmartAlert2(List<IMongoQuery> query, DateTime? ps = null, DateTime? pn = null, bool riskcheck = true)
        {
            List<BsonDocument> allActivity = new List<BsonDocument>();
            if (query.Count > 0)
            {
                var models = BizPlanActivity.GetAll(Query.And(query));
                foreach (var m in models)
                    allActivity.Add(m.ToBsonDocument());
            }
            else
            {
                var models = BizPlanActivity.GetAll(null);
                foreach (var m in models)
                    allActivity.Add(m.ToBsonDocument());
            }


            IMongoQuery q = Query.Not(Query.Matches("ActivityType", new BsonRegularExpression(
            new Regex("RISK", RegexOptions.IgnoreCase))));



            List<BsonDocument> uwAllActivity = new List<BsonDocument>();
            if (riskcheck == false || query.Contains(q))
            {
                uwAllActivity = BsonHelper.Unwind(allActivity, "Phases", "", new List<string> { "_id", "RigType", "RigName", "WellName", "UARigSequenceId", "PsSchedule" })
                    .Where(x => !BsonHelper.GetString(x, "ActivityType").Contains("RISK")).ToList();
            }
            else
            {
                uwAllActivity = BsonHelper.Unwind(allActivity, "Phases", "", new List<string> { "_id", "RigType", "RigName", "WellName", "UARigSequenceId", "PsSchedule" });
            }

            if (ps != Tools.DefaultDate && pn != Tools.DefaultDate)
            {
                uwAllActivity = uwAllActivity.Where(x => BsonHelper.GetDateTime(x, "PhSchedule.Start") >= ps && BsonHelper.GetDateTime(x, "PhSchedule.Finish") <= pn).ToList();
            }
            
            List<BsonDocument> lResult = new List<BsonDocument>();

            foreach (var ts in uwAllActivity.Where(y => BsonHelper.GetDateTime(y, "PhSchedule.Start") >= new DateTime(2000, 01, 01)).GroupBy(x => BsonHelper.GetString(x, "RigName")))
            {
                BsonDocument doc = new BsonDocument();

                string prevGapOver = "";

                DateTime pstart = new DateTime();
                DateTime pfinish = new DateTime();

                DateRange dr = new DateRange(); //Date Range
                DateRange dc = new DateRange(); //Date Range Comparison
                var start_one = ts.Where(y => BsonHelper.GetDateTime(y, "PhSchedule.Start") >= new DateTime(2000, 01, 01)).OrderBy(x => BsonHelper.GetDateTime(x, "PhSchedule.Start")).ToList();

                pstart = BsonHelper.GetDateTime(start_one.FirstOrDefault(), "PhSchedule.Start");
                pfinish = BsonHelper.GetDateTime(start_one.FirstOrDefault(), "PhSchedule.Finish");
                var count = 0;
                foreach (var order in ts.Where(y => BsonHelper.GetDateTime(y, "PhSchedule.Start") >= new DateTime(2000, 01, 01)).OrderBy(x => BsonHelper.GetDateTime(x, "PhSchedule.Start")))
                {
                    doc = new BsonDocument();
                    DateTime start = new DateTime();
                    DateTime finish = new DateTime();
                    
                    if (count < ts.Count()-1)
                    {
                        prevGapOver = string.Format("{0}\n{1}\n{2}",
                           BsonHelper.GetString(start_one.Skip(count + 1).FirstOrDefault(), "WellName"),
                           BsonHelper.GetString(start_one.Skip(count + 1).FirstOrDefault(), "UARigSequenceId"),
                           BsonHelper.GetString(start_one.Skip(count + 1).FirstOrDefault(), "ActivityType")
                           );
                        start = BsonHelper.GetDateTime(start_one.Skip(count + 1).FirstOrDefault(), "PhSchedule.Start");
                        finish = BsonHelper.GetDateTime(start_one.Skip(count + 1).FirstOrDefault(), "PhSchedule.Finish");

                        dr.Start = pstart; dr.Finish = pfinish;
                        dc.Start = start; dc.Finish = finish;

                        if (start.Date == pfinish.Date)
                        {
                            // no overlap no gap
                            doc.Set("Overlapping", 0);
                            doc.Set("GAP", 0);
                            doc.Set("OverLapWith", "");
                            doc.Set("GAPWith", "");
                        }
                        else if (start.Date < pfinish.Date)
                        {
                            doc.Set("Overlapping", (-1) * (start - pfinish).TotalDays);
                            doc.Set("GAP", 0);

                            doc.Set("OverLapWith", prevGapOver);
                            doc.Set("GAPWith", "");


                        }
                        else
                        {
                            doc.Set("Overlapping", 0);
                            doc.Set("GAP", (start - pfinish).TotalDays); // ada gap

                            doc.Set("OverLapWith", "");
                            doc.Set("GAPWith", prevGapOver);
                        }
                    }
                    else
                    {
                        prevGapOver = "";
                        start = BsonHelper.GetDateTime(order, "PhSchedule.Start");
                        finish = BsonHelper.GetDateTime(order, "PhSchedule.Finish");

                        doc.Set("Overlapping", 0);
                        doc.Set("GAP", 0);
                        doc.Set("OverLapWith", prevGapOver);
                        doc.Set("GAPWith", prevGapOver);
                    }

                    
                    doc.Set("WellActivityId", BsonHelper.GetString(order, "_id"));
                    if (start > pfinish.AddDays(1))
                    {
                        doc.Set("Type", "More");
                    }
                    else
                    {
                        doc.Set("Type", "Less");
                    }

                    doc.Set("WellName", BsonHelper.GetString(order, "WellName"));
                    doc.Set("ActivityType", BsonHelper.GetString(order, "ActivityType"));
                    doc.Set("RigName", BsonHelper.GetString(order, "RigName"));
                    doc.Set("PhScheduleStart", BsonHelper.GetDateTime(order, "PhSchedule.Start"));
                    doc.Set("PhScheduleFinish", BsonHelper.GetDateTime(order, "PhSchedule.Finish"));

                    doc.Set("UARigSequenceId", BsonHelper.GetString(order, "UARigSequenceId"));


                    //Days, Cost, Gap, Gap with, Overlap, Overlap With


                    doc.Set("PhaseNo", BsonHelper.GetString(order, "PhaseNo"));

                    doc.Set("PlanStart", BsonHelper.GetDateTime(order, "PlanSchedule.Start"));
                    doc.Set("PlanFinish", BsonHelper.GetDateTime(order, "PlanSchedule.Finish"));

                    doc.Set("ActualDays", BsonHelper.GetDouble(order, "Actual.Days"));
                    doc.Set("ActualCost", BsonHelper.GetDouble(order, "Actual.Cost"));
                    doc.Set("OPDays", BsonHelper.GetDouble(order, "OP.Days"));
                    doc.Set("OPCost", Tools.Div(Convert.ToDouble(BsonHelper.GetDouble(order, "OP.Cost")), 1000000));
                    lResult.Add(doc);

                    pstart = start;
                    pfinish = finish;
                    count += 1;
                    
                   
                }
            }

            return lResult;

        }


        public BsonDocument AddFilterRangeDate(DateTime? From, DateTime? To, string AttributeName)
        {
            var match = new BsonDocument 
                    {{ "$match", new BsonDocument 
                         {{ AttributeName, new BsonDocument {
                                {"$gte", Tools.ToUTC( From)},
                                {"$lte", Tools.ToUTC( To)}
                          }}}
                    }};
            return match;
        }

        public BsonDocument AddFilterIn(List<string> ListData, string MongoFieldName)
        {
            BsonArray arr = new BsonArray();
            foreach (string s in ListData)
            {
                arr.Add(s);
            }
            var match = new BsonDocument
                                {
                                    { "$match", new BsonDocument 
                                         {{ MongoFieldName, new BsonDocument {
                                                {"$in", arr }
                                          }}}
                                    }
                                };
            return match;
        }
        public JsonResult DetilByRigName(string RigName, string WellName, string planstart, string planfinish, string risk = "1")
        {
            List<string> WellNames = new List<string>();
            List<string> RigNames = new List<string>();
            string riskType = "RISK";
            #region bk
            //var pstart = Tools.DefaultDate; var pend = Tools.DefaultDate;
            //if (!planstart.Contains("undefined"))
            //{
            //    pstart = DateTime.ParseExact(planstart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            //}

            //if (!planstart.Contains("undefined"))
            //{
            //    pend = DateTime.ParseExact(planfinish, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            //}

            //IMongoQuery baseQuery;
            //if (WellName == null || WellName.Contains("undefined"))
            //{
            //    string[] r = RigName.Split(',');
            //    baseQuery = new DashboardController().GenerateQueryFromFilter(null, null, null, r, null, null, null, null);
            //}
            //else
            //{
            //    string[] r = RigName.Split(',');
            //    string[] w = WellName.Split(',');
            //    baseQuery = new DashboardController().GenerateQueryFromFilter(null, null, null, r, null, w, null, null);
            //}
            //List<IMongoQuery> mongQuery = new List<IMongoQuery>();
            //mongQuery.Add(baseQuery);

            //if (!risk.Equals("1"))
            //    mongQuery.Add(Query.Not(Query.Matches("ActivityType", new BsonRegularExpression(
            //            new Regex(riskType, RegexOptions.IgnoreCase)))));

            //mongQuery.Add(WebTools.GetWellActivitiesQuery("WellName", ""));
            #endregion
            var pstart = Tools.DefaultDate; var pend = Tools.DefaultDate;
            if (!planstart.Contains("undefined"))
            {
                pstart = DateTime.ParseExact(planstart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            }

            if (!planfinish.Contains("undefined"))
            {
                pend = DateTime.ParseExact(planfinish, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            }

            string format = "yyyy-MM-dd";
            CultureInfo culture = CultureInfo.InvariantCulture;

            //new filtr
            List<IMongoQuery> q = new List<IMongoQuery>();
            if (!RigName.Contains("undefined"))
            {
                RigNames = RigName.Split(',').ToList();
                q.Add(Query.In("RigName", new BsonArray(RigNames)));
            }

            if (WellName !=null)
            {
                WellNames = WellName.Split(',').ToList();
                q.Add(Query.In("WellName", new BsonArray(WellNames)));
            }

            if (risk == "1")
            {
                q.Add(Query.EQ("Phases.RiskFlag", true));
            }

            if (planstart != "undefined" && planfinish != "undefined")
                q.Add(Query.And(
                    //Query.GTE("LastUpdate", DateTime.ParseExact(planstart, format, culture)),
                    //Query.LTE("LastUpdate", DateTime.ParseExact(planfinish, format, culture))
                    Query.GTE("Phases.PhSchedule.Start", DateTime.ParseExact(planstart, format, culture)),
                    Query.LTE("Phases.PhSchedule.Finish", DateTime.ParseExact(planfinish, format, culture))
                ));
            else if (planstart != "undefined")
                q.Add(
                    //Query.GTE("LastUpdate", DateTime.ParseExact(planstart, format, culture))
                    Query.GTE("Phases.PhSchedule.Start", DateTime.ParseExact(planstart, format, culture))
                );
            else if (planfinish != "undefined")
                q.Add(
                    //Query.LTE("LastUpdate", DateTime.ParseExact(planfinish, format, culture))
                    Query.LTE("Phases.PhSchedule.Finish", DateTime.ParseExact(planfinish, format, culture))
                );
            List<BsonDocument> bsondoc = new List<BsonDocument>();
            bsondoc = LoadSmartAlert2(q, pstart, pend);

            string datemin = BsonHelper.GetDateTime(bsondoc.OrderBy(x => BsonHelper.GetDateTime(x, "PhScheduleStart")).FirstOrDefault(), "PhScheduleStart").ToString("dd-MMM-yy");
            string datemax = BsonHelper.GetDateTime(bsondoc.OrderByDescending(x => BsonHelper.GetDateTime(x, "PhScheduleFinish")).FirstOrDefault(), "PhScheduleFinish").ToString("dd-MMM-yy");
            
            string pmin = BsonHelper.GetDateTime(bsondoc.OrderBy(x => BsonHelper.GetDateTime(x, "PlanStart")).FirstOrDefault(), "PlanStart").ToString("dd-MMM-yy");
            string pmax = BsonHelper.GetDateTime(bsondoc.OrderByDescending(x => BsonHelper.GetDateTime(x, "PlanFinish")).FirstOrDefault(), "PlanFinish").ToString("dd-MMM-yy");

            return Json(new { Data = DataHelper.ToDictionaryArray(bsondoc), pmin = pmin, pmax = pmax, MinDate = datemin, MaxDate = datemax, Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
        }
    }
}