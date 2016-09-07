using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using System.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.IO;
using System.Data;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth(WEISRoles = "Administrators")]
    public class WellMatchController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Data(string wellName = "", bool edmOnly = false)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var WellJunctions = DataHelper.Populate("WEISWellJunction").ToList();
                var isSearchMode = (wellName == null ? false : !wellName.Trim().Equals(""));

                List<IMongoQuery> queries = new List<IMongoQuery>();
                queries.Add(Query.NE("IsVirtualWell", true));
                
                if (isSearchMode) {
                    queries.Add(Query.Matches("_id", new BsonRegularExpression(
                        new Regex(wellName.ToLower(), RegexOptions.IgnoreCase))));
                }

                if (edmOnly)
                {
                    BsonArray ids = new BsonArray();
                    foreach (BsonDocument b in WellJunctions) ids.Add(b.GetString("OUR_WELL_ID"));
                    queries.Add(Query.In("_id", ids));
                }

                var query = (queries.Count > 0 ? Query.And(queries.ToArray()) : null);

                var OurWells = WellInfo.Populate<WellInfo>(query)
                    .Select(d => new WellMatchParam()
                    {
                        WellId = d._id.ToString(),
                        WellName = d._id.ToString(),
                        ShellWellName = WellJunctions
                            .Where(e => d._id.ToString() == e.GetString("OUR_WELL_ID"))
                            .Select(e => e.GetString("SHELL_WELL_NAME"))
                            .FirstOrDefault(),
                            FirstOilDate = d.FirstOilDate
                    })
                    .OrderBy(d => d.WellName);

                return OurWells;
            });
        }

        public ActionResult SearchJunction()
        {
            try
            { 
                string keyword = Request.QueryString["filter[filters][0][value]"];

                if (keyword == null)
                {
                    return Json(null, JsonRequestBehavior.AllowGet);
                }
                else if (keyword.ToString().Trim() == "")
                {
                    return Json(null, JsonRequestBehavior.AllowGet);
                }

                IMongoQuery query = Query.Matches("WELL_COMMON_NAME", 
                    new BsonRegularExpression(
                        new Regex(keyword.ToString().ToLower(), RegexOptions.IgnoreCase)));

                var Data = DataHelper.Populate("tmpCD_WELL", query)
                    .Select(d => new
                    {
                        WellId = d.GetString("WELL_ID"),
                        WellName = d.GetString("WELL_COMMON_NAME")
                    });

                return Json(Data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult SaveJunction(string OurWellId, string ShellWellId, string ShellWellName)
        {
            var wells = DataHelper.Populate("WEISWellJunction", Query.EQ("OUR_WELL_ID", OurWellId));
            var well = (wells.Count > 0 ? wells[0] : new BsonDocument());

            try
            {
                well.Set("OUR_WELL_ID", OurWellId);
                well.Set("SHELL_WELL_ID", ShellWellId);
                well.Set("SHELL_WELL_NAME", ShellWellName);

                DataHelper.Save("WEISWellJunction", well);

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public ActionResult SaveFirstOilDate(string WellId, DateTime FirstOilDate)
        {
            try
            {
                var wellInfo = WellInfo.Get<WellInfo>(Query.EQ("_id", WellId));
                DataHelper.Delete(new WellInfo().TableName, Query.EQ("_id", WellId));
                wellInfo.FirstOilDate = FirstOilDate;
                wellInfo.Save();
            return Json(new { Success = true, FirstOilDate }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public ActionResult DeleteJunction(string OurWellId)
        {
            try
            {
                DataHelper.Delete("WEISWellJunction", Query.EQ("OUR_WELL_ID", OurWellId));

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult SaveWell(string Name)
        {
            try
            {
                if (WellInfo.Populate<WellInfo>(new MasterRigNameController().Like(Name)).Count > 0)
                {
                    return Json(new { Success = false, Message = "Name already exists" }, JsonRequestBehavior.AllowGet);
                }

                new WellInfo() { _id = Name }.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }


        public JsonResult UpdateWell(List<WellMatchParam> updated)
        {
            try
            {
                updated = (updated == null ? new List<WellMatchParam>() : updated);
                var counter = 0;
                var success = false;
                var message = "";

                foreach (var each in updated)
                {
                    string id = Convert.ToString(each.WellId);
                    string name = Convert.ToString(each.WellName);

                    var activities = WellActivity.Populate<WellActivity>(Query.EQ("WellName", name));
                    if (activities.Count > 0) continue;

                    var rigNames = WellInfo.Populate<WellInfo>(Query.EQ("_id", name));
                    if (rigNames.Count == 0) continue;

                    var rigNamesUsingThisNewName = WellInfo.Populate<WellInfo>(Query.EQ("_id", id));
                    if (rigNamesUsingThisNewName.Count > 0) continue;

                    var wells = DataHelper.Populate("WEISWellJunction", Query.EQ("OUR_WELL_ID", name));
                    if (wells.Count > 0) continue;

                    DataHelper.Delete(new WellInfo().TableName, Query.EQ("_id", name));
                    new WellInfo() { _id = id }.Save();

                    counter++;
                }

                if (counter == 0)
                {
                    success = false;
                    message = "Data that used in activity or pointed to \"Shell's Well\" cannot be updated";
                }
                else if (counter == updated.Count)
                {
                    success = true;
                    message = "Data updated!";
                }
                else
                {
                    success = false;
                    message = "Some data failed to be updated because used on activity";
                }

                return Json(new { Success = success, Message = message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult DeleteWell(string _id)
        {
            try
            {
                var success = false;
                var message = "";

                var activities = WellActivity.Populate<WellActivity>(Query.EQ("WellName", _id));
                if (activities.Count > 0)
                {
                    success = false;
                    message = "Data that used in activity cannot be deleted";
                }
                else
                {
                    var wells = DataHelper.Populate("WEISWellJunction", Query.EQ("OUR_WELL_ID", _id));
                    if (wells.Count > 0)
                    {
                        success = false;
                        message = "Data that pointed to \"Shell's Well\" in cannot be deleted";
                    }
                    else
                    {
                        success = true;
                        DataHelper.Delete(new WellInfo().TableName, Query.EQ("_id", _id));
                    }
                }

                return Json(new { Success = success, Message = message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public ActionResult Import()
        {
            try
            {
                List<BsonDocument> bsonDocs = new List<BsonDocument>();
                #region Oracle Process
                string qs = "";
                using (StreamReader sr = new StreamReader(Server.MapPath("~/ActualQuery/WellQuery.txt")))
                {
                    qs = sr.ReadToEnd();
                }

                string connectionString = ConfigurationManager.AppSettings["OraServerShell"]; //"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=tcp)(HOST=138.57.149.67)(PORT=1648)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=owsh155d)));User Id=DSDQ_INTERFACE;Password=DSDQ;";
                List<BsonDocument> allJobs = new List<BsonDocument>();

                OracleConnection connection = new OracleConnection(connectionString);
                connection.Open();
                OracleCommand command = new OracleCommand();
                command.Connection = connection;
                command.CommandText = qs;
                command.CommandType = CommandType.Text;
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

                DataHelper.Delete("tmpCD_WELL");
                foreach (var t in bsonDocs)
                    DataHelper.Save("tmpCD_WELL", t);

                return Json(new { Success = true, CountLoaded = bsonDocs.Count() }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }
	}
}