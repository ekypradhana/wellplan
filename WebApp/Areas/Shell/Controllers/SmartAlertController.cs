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
using System.Text;
using System.Globalization;
using MongoDB.Bson.Serialization;
namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class SmartAlertController : Controller
    {
        //
        // GET: /Shell/SmartAlert/
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Index2()
        {
            return View();
        }

        public JsonResult GetData(string[] wellNames, string[] rigNames, string dateStart, string dateFinish, bool riskcheck = true)
        {
            var division = 1000000.0;

            DateTime pstart = DateTime.ParseExact(dateStart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            DateTime pend = DateTime.ParseExact(dateFinish, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            var baseQuery = new DashboardController().GenerateQueryFromFilter(null, null, null, rigNames, null, wellNames,null);
            string format = "yyyy-MM-dd";
            CultureInfo culture = CultureInfo.InvariantCulture;
            IMongoQuery query = null;
            List<IMongoQuery> queries = new List<IMongoQuery>();
            List<IMongoQuery> dateQuery = new List<IMongoQuery>();

            //if (!dateStart.Equals("") && !dateFinish.Equals(""))
            //    dateQuery.Add(Query.And(
            //        Query.GTE("LastUpdate", DateTime.ParseExact(dateStart, format, culture)),
            //        Query.LTE("LastUpdate", DateTime.ParseExact(dateFinish, format, culture))
            //    ));
            //else if (!dateStart.Equals(""))
            //    dateQuery.Add(
            //        Query.GTE("LastUpdate", DateTime.ParseExact(dateStart, format, culture))
            //    );
            //else if (!dateFinish.Equals(""))
            //    dateQuery.Add(
            //        Query.LTE("LastUpdate", DateTime.ParseExact(dateFinish, format, culture))
            //    );

            if (baseQuery != null)
                queries.Add(baseQuery);

            //if (dateQuery.Count > 0)
            //    queries.Add(Query.And(dateQuery.ToArray()));

            if (queries.Count() > 0)
                query = Query.And(queries.ToArray());

            // end filter

            queries.Add(WebTools.GetWellActivitiesQuery("WellName", ""));

           

            var actual = this.LoadSmartAlert(queries, pstart, pend, riskcheck);

            List<string> docs = actual.Select(x => BsonHelper.GetString(x, "RigName")).ToList();

            List<BsonDocument> res = new List<BsonDocument>();
            foreach (string d in docs.Distinct())
            {
                BsonDocument doc = new BsonDocument();
                doc.Set("RigName", d);
                var GAP = actual.Where(x=> BsonHelper.GetString(x, "RigName").Equals(d) ).Select(c=> BsonHelper.GetDouble(c,"GAP")).Sum()   ;
                doc.Set("GAP", GAP);

                string datemin = actual.Where(x => BsonHelper.GetString(x, "RigName").Equals(d)).Select(c => BsonHelper.GetDateTime(c, "PhScheduleStart")).Min().ToString("dd-MMM-yy");
                doc.Set("PhScheduleStart", datemin);
                
                string datemax = actual.Where(x => BsonHelper.GetString(x, "RigName").Equals(d)).Select(c => BsonHelper.GetDateTime(c, "PhScheduleFinish")).Max().ToString("dd-MMM-yy");
                doc.Set("PhScheduleFinish", datemax);

                string planmin = actual.Where(x => BsonHelper.GetString(x, "RigName").Equals(d)).Select(c => BsonHelper.GetDateTime(c, "PlanStart")).Min().ToString("dd-MMM-yy");
                doc.Set("PlanStart", planmin);

                string planmax = actual.Where(x => BsonHelper.GetString(x, "RigName").Equals(d)).Select(c => BsonHelper.GetDateTime(c, "PlanFinish")).Max().ToString("dd-MMM-yy");
                doc.Set("PlanFinish", planmax);

                List<string> sequence = actual.Where(x => BsonHelper.GetString(x, "RigName").Equals(d)).Select(x => BsonHelper.GetString(x, "UARigSequenceId")).Distinct().ToList();
                string seq = "";
                for (int i = 0; i <= sequence.Count-1; i++)
                {
                    if (i == sequence.Count - 1)
                        seq = seq + sequence[i].ToString() ; 
                    else
                        seq = seq + sequence[i].ToString() + ", "; 
                }
                doc.Set("Seq", seq);

                    res.Add(doc);
            }

            return Json(new { Data = DataHelper.ToDictionaryArray(res), Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
        }

        public List<BsonDocument> LoadSmartAlert(List<IMongoQuery> query, DateTime? ps = null, DateTime? pn = null, bool riskcheck = true)
        {
            List<BsonDocument> allActivity = new List<BsonDocument>();
            if (query.Count > 0)
                allActivity = DataHelper.Populate("WEISWellActivities", Query.And(query));
            else
                allActivity = DataHelper.Populate("WEISWellActivities");

            
                IMongoQuery q = Query.Not(Query.Matches("ActivityType", new BsonRegularExpression(
                new Regex("RISK", RegexOptions.IgnoreCase))));



            List<BsonDocument> uwAllActivity = new List<BsonDocument>();
            if (riskcheck == false || query.Contains(q))
            {
                uwAllActivity = BsonHelper.Unwind(allActivity, "Phases", "", new List<string> { "_id", "RigType", "RigName", "WellName", "UARigSequenceId", "PsSchedule" })
                    .Where(x => ! BsonHelper.GetString(x,"ActivityType").Contains("RISK")  ).ToList();
            }
            else
            {
                uwAllActivity = BsonHelper.Unwind(allActivity, "Phases", "", new List<string> { "_id", "RigType", "RigName", "WellName", "UARigSequenceId", "PsSchedule" });
            }

            if (ps != null && pn != null)
            {
                uwAllActivity = uwAllActivity.Where(x => BsonHelper.GetDateTime(x, "PlanSchedule.Start") >= ps && BsonHelper.GetDateTime(x, "PlanSchedule.Finish") <= pn).ToList();
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

                doc.Set("ActualDays", BsonHelper.GetDouble(start_one.FirstOrDefault(), "Actual.Days"));
                doc.Set("ActualCost", BsonHelper.GetDouble(start_one.FirstOrDefault(), "Actual.Cost"));
                doc.Set("OPDays", BsonHelper.GetDouble(start_one.FirstOrDefault(), "OP.Days"));
                doc.Set("OPCost", BsonHelper.GetDouble(start_one.FirstOrDefault(), "OP.Cost"));
                lResult.Add(doc);

                DateTime pstart = new DateTime();
                DateTime pfinish = new DateTime();
                pstart = BsonHelper.GetDateTime(start_one.FirstOrDefault(), "PhSchedule.Start");
                pfinish = BsonHelper.GetDateTime(start_one.FirstOrDefault(), "PhSchedule.Finish");

                //DateTime plstart = new DateTime();
                //DateTime plfinish = new DateTime();
                //plstart = BsonHelper.GetDateTime(start_one.FirstOrDefault(), "PlanSchedule.Start");
                //plfinish = BsonHelper.GetDateTime(start_one.FirstOrDefault(), "PlanSchedule.Finish");



                foreach (var order in ts.Where(y => BsonHelper.GetDateTime(y, "PhSchedule.Start") >= new DateTime(2000, 01, 01)).OrderBy(x => BsonHelper.GetDateTime(x, "PhSchedule.Start")).Skip(1))
                {
                    doc = new BsonDocument();
                    DateTime start = BsonHelper.GetDateTime(order, "PhSchedule.Start");
                    DateTime finish = BsonHelper.GetDateTime(order, "PhSchedule.Finish");

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

                    doc.Set("RigName", BsonHelper.GetString(order, "RigName"));
                    doc.Set("WellName", BsonHelper.GetString(order, "WellName"));
                    doc.Set("UARigSequenceId", BsonHelper.GetString(order, "UARigSequenceId"));
                    doc.Set("PhaseNo", BsonHelper.GetString(order, "PhaseNo"));
                    doc.Set("ActivityType", BsonHelper.GetString(order, "ActivityType"));
                    doc.Set("PhScheduleStart", BsonHelper.GetDateTime(order, "PhSchedule.Start"));
                    doc.Set("PhScheduleFinish", BsonHelper.GetDateTime(order, "PhSchedule.Finish"));
                    doc.Set("PlanStart", BsonHelper.GetDateTime(order, "PlanSchedule.Start"));
                    doc.Set("PlanFinish", BsonHelper.GetDateTime(order, "PlanSchedule.Finish"));
                    doc.Set("GAP", (start - pfinish.AddDays(1)).TotalDays);
                    doc.Set("ActualDays", BsonHelper.GetDouble(order, "Actual.Days"));
                    doc.Set("ActualCost", BsonHelper.GetDouble(order, "Actual.Cost"));
                    doc.Set("OPDays", BsonHelper.GetDouble(order, "OP.Days"));
                    doc.Set("OPCost", BsonHelper.GetDouble(order, "OP.Cost"));
                    lResult.Add(doc);

                    pstart = start;
                    pfinish = finish;

                    //plstart = nstart;
                    //plfinish = nfinish;
                }
            }

            return lResult;

        }

        public JsonResult DetilByRigName(string RigName, string WellName, string planstart, string planfinish, string risk = "1")
        {

            string riskType = "RISK";

            DateTime pstart = DateTime.ParseExact(planstart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            DateTime pend = DateTime.ParseExact(planfinish, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            IMongoQuery baseQuery;
            if (WellName == null || WellName.Contains("undefined"))
            {
                string[] r = RigName.Split(',');
                baseQuery = new DashboardController().GenerateQueryFromFilter(null, null, null, r, null, null,null);
            }
            else
            {
                string[] r = RigName.Split(',');
                string[] w = WellName.Split(',');
                baseQuery = new DashboardController().GenerateQueryFromFilter(null, null, null, r, null, w, null);
            }
            List<IMongoQuery> mongQuery = new List<IMongoQuery>();
            mongQuery.Add(baseQuery);

            if (!risk.Equals("1"))
                mongQuery.Add(Query.Not(Query.Matches("ActivityType", new BsonRegularExpression(
                        new Regex(riskType, RegexOptions.IgnoreCase)))));

            mongQuery.Add(WebTools.GetWellActivitiesQuery("WellName", ""));

            List<BsonDocument> bsondoc = new List<BsonDocument>();
            bsondoc = LoadSmartAlert(mongQuery, pstart, pend);

            string datemin = BsonHelper.GetDateTime(bsondoc.OrderBy(x => BsonHelper.GetDateTime(x, "PhScheduleStart")).FirstOrDefault(), "PhScheduleStart").ToString("dd-MMM-yy");
            string datemax = BsonHelper.GetDateTime(bsondoc.OrderByDescending(x => BsonHelper.GetDateTime(x, "PhScheduleFinish")).FirstOrDefault(), "PhScheduleFinish").ToString("dd-MMM-yy");

            string pmin = BsonHelper.GetDateTime(bsondoc.OrderBy(x => BsonHelper.GetDateTime(x, "PlanStart")).FirstOrDefault(), "PlanStart").ToString("dd-MMM-yy");
            string pmax = BsonHelper.GetDateTime(bsondoc.OrderByDescending(x => BsonHelper.GetDateTime(x, "PlanFinish")).FirstOrDefault(), "PlanFinish").ToString("dd-MMM-yy");


            return Json(new { Data = DataHelper.ToDictionaryArray(bsondoc), pmin = pmin, pmax = pmax, MinDate = datemin, MaxDate = datemax, Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
        }


        // Grid 2 




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

        public List<WellActivityUpdate> LoadSmartAlert2(List<string> rigName = null, List<string> wellName = null, List<string> activityType = null, DateTime? from = null, DateTime? to = null)
        {
            List<WellActivityUpdate> resultShow = new List<WellActivityUpdate>();

            List<BsonDocument> qs = new List<BsonDocument>();

            if (from != null || to != null)
                qs.Add(AddFilterRangeDate(from, to, "LastUpdate"));
            if (rigName != null && rigName.Count() > 0)
                qs.Add(AddFilterIn(rigName, "RigName"));
            if (wellName != null && wellName.Count() > 0)
                qs.Add(AddFilterIn(wellName, "WellName"));
            if (activityType != null && activityType.Count() > 0)
                qs.Add(AddFilterIn(activityType, "ActivityType"));




            string AggQuery = @"{ 
                                   $group : 
                                   {
                                        _id: 
                                        {
                                            id : '$_id' ,
                                            WellName : '$WellName' ,
                                            SequenceId : '$SequenceId' ,
                                            ActivityType : '$ActivityType' ,
                    
                                        },
                                       LastUpdate : {$last : '$LastUpdate'},
                                    }
                                }";

            qs.Add(BsonSerializer.Deserialize<BsonDocument>(AggQuery));



            var res = DataHelper.Aggregate("WEISWellActivityUpdates", qs);

            // res = Data aggreagate ID dll

            foreach (var t in res)
            {
                string obj = BsonHelper.GetString(BsonHelper.Get(t, "_id").AsBsonDocument, "id").ToString();
                if (obj != null && obj.Trim() != "")
                {
                    var update = DataHelper.Populate<WellActivityUpdate>("WEISWellActivityUpdates", Query.EQ("_id", obj));

                    if (update != null && update.Count() > 0)
                    {
                        // loop every group data 
                        double planDays = 0;
                        double planCost = 0;

                        planDays = update.FirstOrDefault().Elements.Sum(x => x.DaysPlanImprovement);
                        planCost = update.FirstOrDefault().Elements.Sum(x => x.CostPlanImprovement);

                        if (planDays < 0 || planCost < 0)
                        {
                            //lanjutkan ke proses berikutnya
                            // Apakah AFE Days atau Cost < LE Days atau LE Cost

                            double AFEDays = update.FirstOrDefault().Phase.AFE.Days;
                            double AFECost = update.FirstOrDefault().Phase.AFE.Cost;
                            double LEDays = update.FirstOrDefault().Phase.LE.Days;
                            double LECost = update.FirstOrDefault().Phase.LE.Cost;
                            if ((AFEDays < LEDays) || (AFECost < LECost))
                            {
                                //- Jika iya … tampilkan, jika tidak lanjut ke itrasi berikutnya
                                resultShow.Add(update.FirstOrDefault());
                            }
                            // untuk tester saja 
                            //resultShow.Add(update.FirstOrDefault());
                        }
                        else
                        {
                            //hentikan dan lanjut ke iterasi berikutnya
                        }
                    }
                }
            }

            return resultShow;

        }

        public JsonResult DetilByWellName(string WellName, string id)
        {
            List<string> wellNames = new List<string>();

            if(WellName != "" && WellName.Trim() != "")
                wellNames =  WellName.Split(',').ToList();

            var res =  DataHelper.Populate<WellActivityUpdate>("WEISWellActivityUpdates", Query.EQ("_id", id));

            

            //var t = this.LoadSmartAlert2(null, wellNames, null, null, null);
            


            return Json(new { Data = res.FirstOrDefault().Elements, Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetData2(string[] wellNames, string dateStart = "", string dateFinish = "")
        {

            DateTime? dates = dateStart != string.Empty ? DateTime.ParseExact(dateStart.ToString(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : new DateTime();
            DateTime? daten = dateFinish != string.Empty ? DateTime.ParseExact(dateFinish.ToString(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : new DateTime();

            List<string> str = new List<string>();
            if (wellNames != null)
                str = wellNames.ToList();

            List<WellActivityUpdate> t = new List<WellActivityUpdate>();

            if (dateStart != "" && dateFinish != "")
                t = this.LoadSmartAlert2(null, str, null, dates, daten);
            else
                t = this.LoadSmartAlert2(null, str, null, null, null);
            return Json(new { Data = t, Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
        }
    }
}