using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Globalization;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver;

using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.IO;
using System.Data;
using System.Diagnostics;
using System.Configuration;

using Newtonsoft.Json.Serialization;
using System.Text;
using System.Data.OleDb;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class ActualController : Controller
    {

        /* =======  View and Grid =======  */
        //
        // GET: /Shell/Actual/
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SqlQuery()
        {
            return View();
        }

        public JsonResult RunQuery(string query)
        {
            #region remark
            //try
            //{

            //    List<BsonDocument> bsonDocs = new List<BsonDocument>();
            //    #region Oracle Process
            //    string connectionString = ConfigurationManager.AppSettings["OraServerShell"]; //"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=tcp)(HOST=138.57.149.67)(PORT=1648)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=owsh155d)));User Id=DSDQ_INTERFACE;Password=DSDQ;";
            //    List<BsonDocument> allJobs = new List<BsonDocument>();

            //    OracleConnection connection = new OracleConnection(connectionString);
            //    connection.Open();
            //    OracleCommand command = new OracleCommand();
            //    command.Connection = connection;
            //    command.CommandText = query;
            //    command.CommandType = CommandType.Text;
            //    OracleDataReader reader = command.ExecuteReader();
            //    Dictionary<string, string> fields = new Dictionary<string, string>();
            //    while (reader.Read())
            //    {

            //        var document = new BsonDocument();
            //        for (int i = 0; i < reader.FieldCount; i++)
            //        {
            //            String stringValue;
            //            DateTime dateTimeValue;
            //            float floatValue;
            //            stringValue = System.Convert.IsDBNull(reader[i]) ? "" : System.Convert.ToString(reader[i]);

            //            if (DateTime.TryParse(stringValue, out dateTimeValue))
            //            {
            //                string str = dateTimeValue.ToString("yyyy-MMM-dd");
            //                document.Set(reader.GetName(i), str);
            //            }
            //            else if (float.TryParse(stringValue, out floatValue))
            //                document.Set(reader.GetName(i), floatValue);
            //            else
            //                document.Set(reader.GetName(i), stringValue);
            //        }
            //        bsonDocs.Add(document);
            //    }

            //    connection.Close();
            //    connection.Dispose();

            //    int RecordCount = bsonDocs.Count();
            //    object[] objs = DataHelper.ToDictionaryArray(bsonDocs);

            //    #endregion
            //    return MvcResultInfo.Execute(() =>
            //    {
            //        return new
            //        {
            //            RecordCount = RecordCount,
            //            DataSet = objs
            //        };
            //    });

            //}
            //catch (OracleException e)
            //{
            //    return Json(new { DataSet = "", RecordCount = 0, Data = "", Result = "NOK", Message = e.Message }, JsonRequestBehavior.AllowGet);
            //}
            //catch (Exception ex)
            //{
            //    return Json(new { DataSet = "", RecordCount = 0, Data = "", Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);
            //}
          #endregion
            return Json(new { data=""}, JsonRequestBehavior.AllowGet);
        }




        /// <summary>
        /// Get Actual Data From MongoDB
        /// </summary>
        /// <param name="dateStart"></param>
        /// <param name="dateFinish"></param>
        /// <param name="wellNames"></param>
        /// <param name="activity"></param>
        /// <returns></returns>
        public JsonResult GetData(string dateStart, string dateFinish, string[] wellNames, string[] activities)
        {
            var division = 1000000.0;
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                // start filter 

                dateStart = (dateStart == null ? "" : dateStart);
                dateFinish = (dateFinish == null ? "" : dateFinish);

                var baseQuery = new DashboardController().GenerateQueryFromFilter(null, null, null, null, null, wellNames, null, null);
                string format = "yyyy-MM-dd";
                CultureInfo culture = CultureInfo.InvariantCulture;
                IMongoQuery query = null;
                List<IMongoQuery> queries = new List<IMongoQuery>();
                List<IMongoQuery> dateQuery = new List<IMongoQuery>();

                if (!dateStart.Equals("") && !dateFinish.Equals(""))
                    dateQuery.Add(Query.And(
                        Query.GTE("UpdateVersion", Tools.ToUTC( DateTime.ParseExact(dateStart, format, culture))),
                        Query.LTE("UpdateVersion", Tools.ToUTC(DateTime.ParseExact(dateFinish, format, culture)))
                    ));
                else if (!dateStart.Equals(""))
                    dateQuery.Add(
                        Query.GTE("UpdateVersion", Tools.ToUTC(DateTime.ParseExact(dateStart, format, culture)))
                    );
                else if (!dateFinish.Equals(""))
                    dateQuery.Add(
                        Query.LTE("UpdateVersion", Tools.ToUTC(DateTime.ParseExact(dateFinish, format, culture)))
                    );

                if (baseQuery != null)
                    queries.Add(baseQuery);

                if (dateQuery.Count > 0)
                    queries.Add(Query.And(dateQuery.ToArray()));

                if (queries.Count() > 0)
                    query = Query.And(queries.ToArray());
                
                queries.Add(WebTools.GetWellActivitiesQuery("WellName", ""));
                // end filter

                var actual = WellActivityActual.Populate<WellActivityActual>(query);

                var queryIN = new List<BsonValue>();
                foreach (var each in actual.Select(d => d.SequenceId))
                    queryIN.Add(each);

                var current = WellActivity.Populate<WellActivity>(Query.In("UARigSequenceId", queryIN));

                return actual.SelectMany(d => d.Actual, (d, e) => new
                {
                    WellName = d.WellName,
                    UpdateVersion = d.UpdateVersion,
                    SequenceId = d.SequenceId,
                    PhaseNo = e.PhaseNo,
                    Data = e.Data
                }).Select(d =>
                {
                    string activityType = "";
                    WellDrillData kurent = new WellDrillData();

                    try
                    {
                        var phase = current
                            .Where(e => e.UARigSequenceId.Equals(d.SequenceId))
                            .Select(e => e.Phases[d.PhaseNo - 1])
                            .FirstOrDefault();
                        activityType = phase.ActivityType;
                        kurent = phase.LE;
                    }
                    catch (Exception e)
                    {

                    }

                    return new
                    {
                        WellName = d.WellName,
                        UpdateVersion = d.UpdateVersion, //.ToString("dd MMM yy"),
                        Activity = activityType,
                        SequenceId = d.SequenceId,
                        Actual = new WellDrillData()
                        {
                            Days = d.Data.Days,
                            Cost = d.Data.Cost / division
                        },
                        Current = new WellDrillData()
                        {
                            Days = kurent.Days,
                            Cost = kurent.Cost / division
                        },
                    };
                });

                //return actual.Select(d => {
                //    var phase = current
                //            .Where(e => e.UARigSequenceId.Equals(d.SequenceId))
                //            .Select(e => e.Phases[d.Actual.FirstOrDefault().PhaseNo - 1])
                //            .FirstOrDefault();

                //    var aktual = d.Actual.LastOrDefault().Data;

                //    return new
                //    {
                //        WellName = d.WellName,
                //        LastUpdate = d.LastUpdate.ToString("dd MMM yy"),
                //        Activity = phase.ActivityType,
                //        Actual = new WellDrillData()
                //        {
                //            Days = aktual.Days,
                //            Cost = aktual.Cost / division
                //        },
                //        Current = new WellDrillData()d
                //        {
                //            Days = phase.LE.Days,
                //            Cost = phase.LE.Cost / division
                //        },
                //    };
                //});
            });
        }
        /// <summary>
        /// Function to direct load from oracle, Populate data save to Temp, and Build Last Data
        /// </summary>
        /// <param name="dateStart"></param>
        /// <param name="dateFinish"></param>
        /// <returns></returns>
        public JsonResult LoadFromOracle(string dateStart, string dateFinish, string edmwellnames)
        {
            try
            {
                List<WellActivityActual> actuals = new List<WellActivityActual>();
                List<BsonDocument> failed = new List<BsonDocument>();

                #region Datetime parse Parmeter
                string st = string.Empty;
                string nd = string.Empty; //end.ToString("yyyy-MM-dd");

                if (dateStart == null || dateFinish == null)
                {
                    st = DateTime.Now.ToString("yyyy-MM-dd");
                    nd = DateTime.Now.ToString("yyyy-MM-dd");
                }
                else
                {
                    st = Convert.ToDateTime(dateStart).ToString("yyyy-MM-dd");
                    nd = Convert.ToDateTime(dateFinish).ToString("yyyy-MM-dd");
                }
                #endregion

                string strStartDate_yyyyMMdd = st;
                string strEndDate_yyyyMMdd = nd;

                DateTime str = DateTime.ParseExact(strStartDate_yyyyMMdd, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                DateTime nda = DateTime.ParseExact(strEndDate_yyyyMMdd, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);


                DateTime NowData = new DateTime();
                NowData = DateTime.Now;
                List<BsonDocument> bsonDocs = new List<BsonDocument>();
                for (DateTime date = str; date <= nda; date = date.AddDays(1))
                {
                    string qs = "";
                    string qx = "";
                    string OraParamOpsDate = ConfigurationManager.AppSettings["OraParamOpsDate"];

                    #region Query
                    var Query = @"SELECT
	CD_WELL.well_id AS WellId, 
	CD_WELL.well_common_name AS WellName, 
	DM_DAILY.date_report AS ReportDate,
	DM_EVENT.event_id as EventId, 
	DM_EVENT.event_code as EventCode, 
	DM_EVENT.date_ops_start AS DateOps,
	DM_AFE.afe_no as AFENo,
	DM_DAILY.days_on_location AS DaysOnLocation,   
	Sum(DM_DAILYCOST.cost_amount) as ActualCost,
	DM_EVENT.cost_authorized as AFECost,
	DM_AFE.estimated_days AS AFEDays
FROM
	DM_DAILYCOST, 
	DM_AFE, 
	DM_AFE_EVENT_LINK, 
	DM_EVENT, 
	CD_WELL, 
	CD_SITE,
	DM_DAILY
WHERE
	(
		(DM_AFE.afe_id = DM_DAILYCOST.afe_id) AND
		(DM_AFE_EVENT_LINK.afe_id=DM_AFE.afe_id) AND 
		(DM_EVENT.well_id = DM_AFE_EVENT_LINK.well_id AND DM_EVENT.event_id = DM_AFE_EVENT_LINK.event_id) AND 
		(CD_WELL.well_id = DM_EVENT.well_id) AND
		(CD_SITE.site_id = CD_WELL.site_id) AND 
		(DM_EVENT.well_id = DM_DAILYCOST.well_id AND DM_EVENT.event_id = DM_DAILYCOST.event_id) AND 
		(DM_DAILYCOST.daily_id = DM_DAILY.daily_id)
	) AND 
	(trunc(DM_EVENT.date_ops_start) >= TO_DATE(:EventDate,'yyyy-MM-dd') ) AND 
	(trunc(DM_DAILY.date_report) = TO_DATE(:DailyDate,'yyyy-MM-dd') )
GROUP BY
	CD_WELL.well_id,
	CD_WELL.well_common_name, 
	DM_EVENT.event_id,
	DM_EVENT.event_code, 
	DM_AFE.afe_no, 
	DM_DAILY.days_on_location, 
	DM_EVENT.cost_authorized,
	DM_AFE.estimated_days, 
	DM_DAILY.date_report, 
	DM_EVENT.date_ops_start, 
	DM_DAILY.daily_cost
ORDER BY 
	DM_DAILY.date_report desc";

                    var QueryWithParam = @"SELECT
	CD_WELL.well_id AS WellId, 
	CD_WELL.well_common_name AS WellName, 
	DM_DAILY.date_report AS ReportDate,
	DM_EVENT.event_id as EventId, 
	DM_EVENT.event_code as EventCode, 
	DM_EVENT.date_ops_start AS DateOps,
	DM_AFE.afe_no as AFENo,
	DM_DAILY.days_on_location AS DaysOnLocation,   
	Sum(DM_DAILYCOST.cost_amount) as ActualCost,
	DM_EVENT.cost_authorized as AFECost,
	DM_AFE.estimated_days AS AFEDays
FROM
	DM_DAILYCOST, 
	DM_AFE, 
	DM_AFE_EVENT_LINK, 
	DM_EVENT, 
	CD_WELL, 
	CD_SITE,
	DM_DAILY
WHERE
	(
		(DM_AFE.afe_id = DM_DAILYCOST.afe_id) AND
		(DM_AFE_EVENT_LINK.afe_id=DM_AFE.afe_id) AND 
		(DM_EVENT.well_id = DM_AFE_EVENT_LINK.well_id AND DM_EVENT.event_id = DM_AFE_EVENT_LINK.event_id) AND 
		(CD_WELL.well_id = DM_EVENT.well_id) AND
		(CD_SITE.site_id = CD_WELL.site_id) AND 
		(DM_EVENT.well_id = DM_DAILYCOST.well_id AND DM_EVENT.event_id = DM_DAILYCOST.event_id) AND 
		(DM_DAILYCOST.daily_id = DM_DAILY.daily_id)
	) AND 
	(trunc(DM_EVENT.date_ops_start) >= TO_DATE(:EventDate,'yyyy-MM-dd') ) AND 
	(trunc(DM_DAILY.date_report) = TO_DATE(:DailyDate,'yyyy-MM-dd') ) {0}
GROUP BY
	CD_WELL.well_id,
	CD_WELL.well_common_name, 
	DM_EVENT.event_id,
	DM_EVENT.event_code, 
	DM_AFE.afe_no, 
	DM_DAILY.days_on_location, 
	DM_EVENT.cost_authorized,
	DM_AFE.estimated_days, 
	DM_DAILY.date_report, 
	DM_EVENT.date_ops_start, 
	DM_DAILY.daily_cost
ORDER BY 
	DM_DAILY.date_report desc";
                    #endregion
                    int idx = 0;
                    string[] wellnamesparam = edmwellnames.Split(';');
                    if (edmwellnames.Trim().Equals(""))
                    {
                        //using (StreamReader sr = new StreamReader(Server.MapPath("~/ActualQuery/Query.txt")))
                        //{
                        qx = Query;//sr.ReadToEnd();
                        qs = String.Format(qx);//, date.ToString("yyyy-MM-dd"), OraParamOpsDate);
                        //}
                    }
                    else
                    {
                        // do with param
                        //using (StreamReader sr = new StreamReader(Server.MapPath("~/ActualQuery/QueryWithParam.txt")))
                        //{

                        StringBuilder likeQuery = new StringBuilder();
                        foreach (string t in wellnamesparam)
                        {
                            if (idx == wellnamesparam.Count() - 1)
                                likeQuery.Append("CD_WELL.well_common_name like :CommonName" + idx);
                            else
                                likeQuery.Append("CD_WELL.well_common_name like :CommonName" + idx + " OR ");
                            idx++;
                        }

                        string prepQuery = string.Format("AND ({0})", likeQuery.ToString());

                        qx = QueryWithParam;//sr.ReadToEnd();
                        qs = String.Format(qx, prepQuery);//, date.ToString("yyyy-MM-dd"), OraParamOpsDate, prepQuery);
                        //}
                    }

                    #region Oracle Process
                    string connectionString = ConfigurationManager.AppSettings["OraServerShell"]; //"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=tcp)(HOST=138.57.149.67)(PORT=1648)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=owsh155d)));User Id=DSDQ_INTERFACE;Password=DSDQ;";
                    List<BsonDocument> allJobs = new List<BsonDocument>();

                    OracleConnection connection = new OracleConnection(connectionString);
                    connection.Open();
                    OracleCommand command = new OracleCommand();
                    command.Connection = connection;
                    command.Parameters.Add(new OracleParameter("DailyDate", date.ToString("yyyy-MM-dd")));
                    command.Parameters.Add(new OracleParameter("EventDate", OraParamOpsDate));
                    if (!edmwellnames.Trim().Equals(""))
                    {
                        --idx;
                        foreach (string t in wellnamesparam)
                        {
                            command.Parameters.Add(new OracleParameter("CommonName" + idx, "%" + t + "%"));
                        }
                    }
                    command.CommandType = CommandType.Text;
                    command.CommandText = qs;
                    OracleDataReader reader = command.ExecuteReader();
                    Dictionary<string, string> fields = new Dictionary<string, string>();
                    while (reader.Read())
                    {
                        var document = new BsonDocument();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            String stringValue;
                            DateTime dateTimeValue;
                            float floatValue;
                            stringValue = System.Convert.IsDBNull(reader[i]) ? "" : System.Convert.ToString(reader[i]);
                            document.Set("_id", new ObjectId());

                            document.Set("ImportDate", Tools.ToUTC(NowData));
                            if (DateTime.TryParse(stringValue, out dateTimeValue))
                                document.Set(reader.GetName(i), Tools.ToUTC(dateTimeValue));
                            else if (float.TryParse(stringValue, out floatValue))
                                document.Set(reader.GetName(i), floatValue);
                            else
                                document.Set(reader.GetName(i), stringValue);
                        }
                        bsonDocs.Add(document);

                    }

                    connection.Close();
                    connection.Dispose();


                    #endregion
                }

                var bsonDocx = bsonDocs; //DataHelper.Populate("_OracleActual");

                #region Process Read Last Actual

                List<WellActivityActual> activityActuals = new List<WellActivityActual>();
                List<WellActivityActual> actMax = new List<WellActivityActual>();

                var y = bsonDocx;
                List<string> wells = y.Select(x => BsonHelper.GetString(x, "WELLNAME")).Distinct().ToList();
                List<BsonDocument> LastUpdater = new List<BsonDocument>();

                foreach (string x in wells)
                {
                    var wellSameCode = y.Where(n => BsonHelper.GetString(n, "WELLNAME").Equals(x)).ToList();
                    List<string> codes = wellSameCode.Select(u => BsonHelper.GetString(u, "EVENTCODE")).Distinct().ToList();
                    foreach (string code in codes)
                    {
                        var lastwellCode = y.Where(n => BsonHelper.GetString(n, "WELLNAME").Equals(x) && BsonHelper.GetString(n, "EVENTCODE").Equals(code)).OrderByDescending(h => BsonHelper.GetDateTime(h, "REPORTDATE")).FirstOrDefault();
                        LastUpdater.Add(lastwellCode);
                    }
                }

                #endregion

                #region Convert to Model and save
                // Convert to Model and save
                //DataHelper.Delete("_OracleActual_Success");
                //DataHelper.Delete("_OracleActual_Failed");
                foreach (var last in LastUpdater)
                {
                    WellActivityActual act = WellActivityActual.Convert(last);
                    if (act != null)
                    {
                        act._id = new ObjectId();
                        actuals.Add(act);
                    }
                    else
                    {
                        failed.Add(last);
                    }
                }

                #endregion

                var result = new ResultInfo();
                return Json(new
                {
                    Data = DataHelper.ToDictionaryArray(y),
                    LastData = DataHelper.ToDictionaryArray(LastUpdater),
                    SucessData = actuals,
                    FailedData = DataHelper.ToDictionaryArray(failed),
                    Result = "OK",
                    Message = "",
                }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                var result = new ResultInfo();
                string errRes = string.Format("{0}\n{1}\n{2}", ex.Message, ex.InnerException, ex.StackTrace);
                return Json(new { Data = "", Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /* ======= Process Request =======  */
        /// <summary>
        /// Execute Load Data From Oracle (Popup on Form)
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="select"></param>
        /// <returns></returns>
        public JsonResult ExecuteLoadData(DateTime? start, DateTime? end, string select)
        {
            try
            {
                List<WellActivityActual> actuals = new List<WellActivityActual>();
                List<BsonDocument> failed = new List<BsonDocument>();

                #region Datetime parse Parmeter
                string st = string.Empty;
                string nd = string.Empty; //end.ToString("yyyy-MM-dd");

                if (start == null || end == null)
                {
                    st = DateTime.Now.ToString("yyyy-MM-dd");
                    nd = DateTime.Now.ToString("yyyy-MM-dd");
                }
                else
                {
                    st = Convert.ToDateTime(start).ToString("yyyy-MM-dd");
                    nd = Convert.ToDateTime(end).ToString("yyyy-MM-dd");
                }
                #endregion


                string strStartDate_yyyyMMdd = st;
                string strEndDate_yyyyMMdd = nd;


                // Loop Start and end
                // Process Query add Import Date
                // Save to collection

                DateTime str = DateTime.ParseExact(strStartDate_yyyyMMdd, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                DateTime nda = DateTime.ParseExact(strEndDate_yyyyMMdd, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);


                DateTime NowData = new DateTime();
                NowData = DateTime.Now;
                List<BsonDocument> bsonDocs = new List<BsonDocument>();

                for (DateTime date = str; date <= nda; date = date.AddDays(1))
                {
                    string qs = "";
                    string qx = "";
                    string OraParamOpsDate = ConfigurationManager.AppSettings["OraParamOpsDate"];

                    #region Query
                    var Query = @"SELECT
	CD_WELL.well_id AS WellId, 
	CD_WELL.well_common_name AS WellName, 
	DM_DAILY.date_report AS ReportDate,
	DM_EVENT.event_id as EventId, 
	DM_EVENT.event_code as EventCode, 
	DM_EVENT.date_ops_start AS DateOps,
	DM_AFE.afe_no as AFENo,
	DM_DAILY.days_on_location AS DaysOnLocation,   
	Sum(DM_DAILYCOST.cost_amount) as ActualCost,
	DM_EVENT.cost_authorized as AFECost,
	DM_AFE.estimated_days AS AFEDays
FROM
	DM_DAILYCOST, 
	DM_AFE, 
	DM_AFE_EVENT_LINK, 
	DM_EVENT, 
	CD_WELL, 
	CD_SITE,
	DM_DAILY
WHERE
	(
		(DM_AFE.afe_id = DM_DAILYCOST.afe_id) AND
		(DM_AFE_EVENT_LINK.afe_id=DM_AFE.afe_id) AND 
		(DM_EVENT.well_id = DM_AFE_EVENT_LINK.well_id AND DM_EVENT.event_id = DM_AFE_EVENT_LINK.event_id) AND 
		(CD_WELL.well_id = DM_EVENT.well_id) AND
		(CD_SITE.site_id = CD_WELL.site_id) AND 
		(DM_EVENT.well_id = DM_DAILYCOST.well_id AND DM_EVENT.event_id = DM_DAILYCOST.event_id) AND 
		(DM_DAILYCOST.daily_id = DM_DAILY.daily_id)
	) AND 
	(trunc(DM_EVENT.date_ops_start) >= TO_DATE(:EventDate,'yyyy-MM-dd') ) AND 
	(trunc(DM_DAILY.date_report) = TO_DATE(:DailyDate,'yyyy-MM-dd') )
GROUP BY
	CD_WELL.well_id,
	CD_WELL.well_common_name, 
	DM_EVENT.event_id,
	DM_EVENT.event_code, 
	DM_AFE.afe_no, 
	DM_DAILY.days_on_location, 
	DM_EVENT.cost_authorized,
	DM_AFE.estimated_days, 
	DM_DAILY.date_report, 
	DM_EVENT.date_ops_start, 
	DM_DAILY.daily_cost
ORDER BY 
	DM_DAILY.date_report desc";
                    #endregion

                    //using (StreamReader sr = new StreamReader(Server.MapPath("~/ActualQuery/Query.txt")))
                    //{
                    qx = Query;// sr.ReadToEnd();
                    qs = String.Format(qx);//, date.ToString("yyyy-MM-dd"), OraParamOpsDate);
                    //}


                    #region Oracle Process
                    string connectionString = ConfigurationManager.AppSettings["OraServerShell"]; //"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=tcp)(HOST=138.57.149.67)(PORT=1648)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=owsh155d)));User Id=DSDQ_INTERFACE;Password=DSDQ;";
                    List<BsonDocument> allJobs = new List<BsonDocument>();

                    OracleConnection connection = new OracleConnection(connectionString);
                    connection.Open();
                    OracleCommand command = new OracleCommand();
                    command.Connection = connection;
                    command.Parameters.Add(new OracleParameter("DailyDate", date.ToString("yyyy-MM-dd")));
                    command.Parameters.Add(new OracleParameter("EventDate", OraParamOpsDate));
                    command.CommandType = CommandType.Text;
                    command.CommandText = qs;
                    OracleDataReader reader = command.ExecuteReader();
                    Dictionary<string, string> fields = new Dictionary<string, string>();
                    while (reader.Read())
                    {
                        var document = new BsonDocument();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            String stringValue;
                            DateTime dateTimeValue;
                            float floatValue;
                            stringValue = System.Convert.IsDBNull(reader[i]) ? "" : System.Convert.ToString(reader[i]);

                            //document.Set("ImportDate", Tools.ToUTC(NowData));
                            if (DateTime.TryParse(stringValue, out dateTimeValue))
                                document.Set(reader.GetName(i), Tools.ToUTC(dateTimeValue));
                            else if (float.TryParse(stringValue, out floatValue))
                                document.Set(reader.GetName(i), floatValue);
                            else
                                document.Set(reader.GetName(i), stringValue);
                        }

                        /* PAKE ID BERDASARKAN WELL, SEQ, REPORT DATE
                          View Depan Dictionary benerin, tampilkan Report Date buang activityType */
                        string _id = String.Format("W{0}E{1}R{2:yyyyMMdd}", BsonHelper.GetString(document, "WELLID"), BsonHelper.GetString(document, "EVENTCODE"), BsonHelper.GetDateTime(document, "REPORTDATE"));
                        document.Set("_id", _id);

                        bsonDocs.Add(document);

                    }

                    connection.Close();
                    connection.Dispose();


                    #endregion

                    #region Save to Collection, add Import Date

                    foreach (var ImportedData in bsonDocs)
                    {
                        DataHelper.Save("_OracleActual", ImportedData);
                    }

                    #endregion

                }


                #region Process Read Last Actual
                DataHelper.Delete("_OracleActual_LastData");
                List<WellActivityActual> activityActuals = new List<WellActivityActual>();
                List<WellActivityActual> actMax = new List<WellActivityActual>();

                var y = DataHelper.Populate("_OracleActual");//.Where(x => BsonHelper.GetDateTime(x, "REPORTDATE") >= date);

                List<string> wells = y.Select(x => BsonHelper.GetString(x, "WELLNAME")).Distinct().ToList();

                List<BsonDocument> LastUpdater = new List<BsonDocument>();  // last update single Wellname and Code

                foreach (string x in wells)
                {
                    var wellSameCode = y.Where(n => BsonHelper.GetString(n, "WELLNAME").Equals(x)).ToList();
                    List<string> codes = wellSameCode.Select(u => BsonHelper.GetString(u, "EVENTCODE")).Distinct().ToList();
                    foreach (string code in codes)
                    {
                        var lastwellCode = y.Where(n => BsonHelper.GetString(n, "WELLNAME").Equals(x) && BsonHelper.GetString(n, "EVENTCODE").Equals(code)).OrderByDescending(h => BsonHelper.GetDateTime(h, "REPORTDATE")).FirstOrDefault();
                        LastUpdater.Add(lastwellCode);
                        DataHelper.Save("_OracleActual_LastData", lastwellCode.ToBsonDocument());
                    }
                }

                #endregion
                string OutputWellActivityUpdate = string.Empty;
                DataHelper.Delete("_OracleActual_Failed");
                DataHelper.Delete("_OracleActual_Success");
                foreach (var t in y)
                {
                    var actv = WellActivityActual.Convert(t);

                    if (actv == null)
                    {
                        failed.Add(t);
                        DataHelper.Save("_OracleActual_Failed", t);
                    }
                    else
                    {
                        actuals.Add(actv);
                        actv.Save();

                        #region Update WellActivityUpdate
                        bool val = WellActivityActual.UpdateWellActivityUpdate(actv, out OutputWellActivityUpdate);
                        #endregion
                    }
                }

                var result = new ResultInfo();
                return Json(new { Data = actuals, DataFailedConvert = DataHelper.ToDictionaryArray(failed), Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                var result = new ResultInfo();
                return Json(new { Data = "", DataFailedConvert = "", Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);
                //return Json(new { Data = "", Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }


        #region Obsolete
        public List<BsonDocument> DirectLoad(string st, string nd)
        {
            #region Query
            string qs = @"SELECT
	                            CD_WELL.well_common_name AS WellName,
	                            DM_DAILY.date_report AS ReportDate, 
	                            DM_DAILY.days_on_location AS DaysOnLocation, 
		                            (SELECT SUM(D.daily_cost) FROM DM_DAILY D WHERE D.well_id = 
		                            DM_DAILY.well_id AND D.event_id = DM_DAILY.event_id AND D.date_report <= 
		                            DM_DAILY.date_report) AS ActualCost, 
	                            DM_EVENT.event_code AS EventCode, 
	                            DM_EVENT.date_ops_start AS DateOps, 
	                            CD_WELL.well_id AS WellId, 
	                            DM_EVENT.estimated_days as AFEDays, 
	                            DM_EVENT.cost_authorized as AFECost
                            FROM
	                            DM_DAILY, DM_EVENT, CD_WELL, CD_SITE
                            WHERE
	                            ((
	                            ( (trunc(DM_EVENT.date_ops_start) >= TO_DATE(:EventDateST) and trunc(DM_EVENT.date_ops_start) <= TO_DATE(:EventDateND,'yyyy-MM-dd')) ))) AND ((DM_EVENT.well_id = DM_DAILY.well_id AND DM_EVENT.event_id = DM_DAILY.event_id) AND " +
                                @"(CD_WELL.well_id = DM_EVENT.well_id) AND (CD_SITE.site_id = 
	                            CD_WELL.site_id))
                            ORDER BY
	                            2 ASC";
            #endregion

            string connectionString = System.Configuration.ConfigurationManager.AppSettings["OraServerShell"]; //"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=tcp)(HOST=138.57.149.67)(PORT=1648)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=owsh155d)));User Id=DSDQ_INTERFACE;Password=DSDQ;";
            List<BsonDocument> allJobs = new List<BsonDocument>();

            OracleConnection connection = new OracleConnection(connectionString);
            connection.Open();
            OracleCommand command = new OracleCommand();
            command.Connection = connection;
            command.CommandText = qs;
            command.Parameters.Add(new OracleParameter("EventDateST", st));
            command.Parameters.Add(new OracleParameter("EventDateND", nd));
            command.CommandType = CommandType.Text;
            OracleDataReader reader = command.ExecuteReader();

            Dictionary<string, string> fields = new Dictionary<string, string>();

            List<BsonDocument> bsonDocs = new List<BsonDocument>();

            while (reader.Read())
            {
                var document = new BsonDocument();

                for (int i = 0; i < reader.FieldCount; i++)
                {

                    String stringValue;
                    DateTime dateTimeValue;
                    float floatValue;

                    stringValue = System.Convert.IsDBNull(reader[i]) ? "" : System.Convert.ToString(reader[i]);
                    
                    //if (stringValue.Equals(objectid))
                    document.Set("_id", new ObjectId());
                    //document.Set("ImportDate", Tools.ToUTC(DateTime.Now));

                    if (DateTime.TryParse(stringValue, out dateTimeValue))
                        document.Set(reader.GetName(i), dateTimeValue);
                    else if (float.TryParse(stringValue, out floatValue))
                        document.Set(reader.GetName(i), floatValue);
                    else
                        document.Set(reader.GetName(i), stringValue);
                    bsonDocs.Add(document);
                }

            }

            connection.Close();
            connection.Dispose();

            //List<BsonDocument> Bdocs = this.ImportActual(Query);
            return bsonDocs;
        }

        public JsonResult LoadActualAFE(string Path)
        {
            //Path = @"D:\ActualOutput\";
            string[] filePaths = System.IO.Directory.GetFiles(@Path, "*.json", System.IO.SearchOption.AllDirectories);

            List<WellActivityActual> activityActuals = new List<WellActivityActual>();
            List<WellActivityActual> actMax = new List<WellActivityActual>();

            foreach (string file in filePaths)
            {
                if (file.Contains("_Ori"))
                {
                    // original Data
                    using (System.IO.StreamReader r = new System.IO.StreamReader(file))
                    {
                        string json = r.ReadToEnd();
                        BsonDocument t = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);
                        WellActivityActual wel = WellActivityActual.Convert(t);
                        if (wel != null)
                        {
                            activityActuals.Add(wel);
                            if (!Directory.Exists(Path + @"Success\"))
                            {
                                Directory.CreateDirectory(Path + @"Success\");
                            }

                            System.IO.File.Copy(file, Path + @"Success\" + System.IO.Path.GetFileName(file));
                        }
                        else
                        {
                            if (!Directory.Exists(Path + @"Failed\"))
                            {
                                Directory.CreateDirectory(Path + @"Failed\");
                            }

                            System.IO.File.Copy(file, Path + @"Failed\" + System.IO.Path.GetFileName(file));
                        }
                    }
                }
            }
            actMax = WellActivityActual.GetLastActual(activityActuals);

            if (actMax.Count > 0)
            {
                foreach (WellActivityActual act in actMax)
                {
                    act.Save(act.TableName);
                }
            }

            // temp processing failed data



            var result = new ResultInfo();
            return Json(actMax, JsonRequestBehavior.AllowGet);
        }

        public List<WellActivityActual> LoadActualAFE2(string outputPath)
        {
            //Path = @"D:\ActualOutput\";
            string[] filePaths = System.IO.Directory.GetFiles(@outputPath, "*.json", System.IO.SearchOption.AllDirectories);

            List<WellActivityActual> activityActuals = new List<WellActivityActual>();
            List<WellActivityActual> actMax = new List<WellActivityActual>();

            foreach (string file in filePaths)
            {
                if (file.Contains("_Ori"))
                {
                    // original Data
                    using (System.IO.StreamReader r = new System.IO.StreamReader(file))
                    {
                        string json = r.ReadToEnd();
                        BsonDocument t = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);
                        WellActivityActual wel = WellActivityActual.Convert(t);
                        if (wel != null)
                        {
                            activityActuals.Add(wel);
                        }
                    }
                }
            }
            actMax = WellActivityActual.GetLastActual(activityActuals);

            if (actMax.Count > 0)
            {
                foreach (WellActivityActual act in actMax)
                {
                    act.Save(act.TableName);
                }

                return actMax;
            }
            return null;
        }

        // use this function to direct load from oracle
        public JsonResult LoadFromOracleConvertToModel(string strStartDate_yyyyMMdd, string strEndDate_yyyyMMdd)
        {
            #region string Query
            string qs = @"SELECT
	                            CD_WELL.well_common_name AS WellName,
	                            DM_DAILY.date_report AS ReportDate, 
	                            DM_DAILY.days_on_location AS DaysOnLocation, 
		                            (SELECT SUM(D.daily_cost) FROM DM_DAILY D WHERE D.well_id = 
		                            DM_DAILY.well_id AND D.event_id = DM_DAILY.event_id AND D.date_report <= 
		                            DM_DAILY.date_report) AS ActualCost, 
	                            DM_EVENT.event_code AS EventCode, 
	                            DM_EVENT.date_ops_start AS DateOps, 
	                            CD_WELL.well_id AS WellId, 
	                            DM_EVENT.estimated_days as AFEDays, 
	                            DM_EVENT.cost_authorized as AFECost
                            FROM
	                            DM_DAILY, DM_EVENT, CD_WELL, CD_SITE
                            WHERE
	                            ((
	                            ( (trunc(DM_EVENT.date_ops_start) >= TO_DATE(:StrStartDate,'yyyy-MM-dd') and trunc(DM_EVENT.date_ops_start) <= TO_DATE(:StrEndDate,'yyyy-MM-dd')) ))) AND ((DM_EVENT.well_id = DM_DAILY.well_id AND DM_EVENT.event_id = DM_DAILY.event_id) AND " +
                                @"(CD_WELL.well_id = DM_EVENT.well_id) AND (CD_SITE.site_id = 
	                            CD_WELL.site_id))
                            ORDER BY
	                            2 ASC";
            #endregion

            try
            {

                string connectionString = System.Configuration.ConfigurationManager.AppSettings["OraServerShell"]; //"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=tcp)(HOST=138.57.149.67)(PORT=1648)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=owsh155d)));User Id=DSDQ_INTERFACE;Password=DSDQ;";
                List<BsonDocument> allJobs = new List<BsonDocument>();

                OracleConnection connection = new OracleConnection(connectionString);
                connection.Open();
                OracleCommand command = new OracleCommand();
                command.Connection = connection;
                command.CommandText = qs;
                command.Parameters.Add(new OracleParameter("StrStartDate", strStartDate_yyyyMMdd));
                command.Parameters.Add(new OracleParameter("StrEndDate", strEndDate_yyyyMMdd));
                command.CommandType = CommandType.Text;
                OracleDataReader reader = command.ExecuteReader();
                Dictionary<string, string> fields = new Dictionary<string, string>();
                List<BsonDocument> bsonDocs = new List<BsonDocument>();
                while (reader.Read())
                {
                    var document = new BsonDocument();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        String stringValue;
                        DateTime dateTimeValue;
                        float floatValue;
                        stringValue = System.Convert.IsDBNull(reader[i]) ? "" : System.Convert.ToString(reader[i]);
                        document.Set("_id", new ObjectId());

                        if (DateTime.TryParse(stringValue, out dateTimeValue))
                            document.Set(reader.GetName(i), dateTimeValue);
                        else if (float.TryParse(stringValue, out floatValue))
                            document.Set(reader.GetName(i), floatValue);
                        else
                            document.Set(reader.GetName(i), stringValue);
                        bsonDocs.Add(document);
                    }
                }

                connection.Close();
                connection.Dispose();


                List<WellActivityActual> wela = new List<WellActivityActual>();
                foreach (var doc in bsonDocs)
                {
                    wela.Add(WellActivityActual.Convert(doc));
                }

                var result = new ResultInfo();
                return Json(new { Data = wela, Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                var result = new ResultInfo();
                return Json(new { Data = "", Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        private void ExecuteCommand(string command, string arguments)
        {
            int exitCode;
            ProcessStartInfo processInfo;
            Process process;

            processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.Arguments = arguments;
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            // *** Redirect the output ***
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            process = Process.Start(processInfo);
            process.Start();
            process.WaitForExit();
            // *** Read the streams ***

            process.Close();
        }
        #endregion
    }
}