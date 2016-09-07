using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth(WEISRoles = "Administrators")]
    public class MasterFundingTypeController : Controller
    {
        //
        // GET: /Shell/MasterFundingType/
        public ActionResult Index()
        {
            ViewBag.Title = "Master Funding Type";
            return View();
        }

        public IMongoQuery Like(string Keyword = null)
        {
            Keyword = (Keyword == null ? "" : Keyword).Trim();
            if (Keyword.Equals("")) return null;

            var queries = new List<IMongoQuery>();
            queries.Add(Query.Matches("_id", new BsonRegularExpression(new Regex(Keyword.ToString().ToLower(), RegexOptions.IgnoreCase))));
            queries.Add(Query.Matches("EXType", new BsonRegularExpression(new Regex(Keyword.ToString().ToLower(), RegexOptions.IgnoreCase))));

            return Query.Or(queries.ToArray());
        }

        public JsonResult Populate(string Keyword = null)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                return MasterFundingType.Populate<MasterFundingType>(new MasterFundingTypeController().Like(Keyword)).OrderBy(d => d._id)
                    .Select(d =>
                    {
                        d.Name = Convert.ToString(d._id);
                        return d;
                    });
            });
        }

        public JsonResult Get(object id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var d = MasterFundingType.Get<MasterFundingType>(id);
                d.Name = Convert.ToString(d._id);
                return d;
            });
        }

        public JsonResult Save(string Name)
        {
            try
            {
                if (MasterFundingType.Populate<MasterFundingType>(new MasterFundingTypeController().Like(Name)).Count > 0)
                {
                    return Json(new { Success = false, Message = "Name already exists" }, JsonRequestBehavior.AllowGet);
                }

                new MasterFundingType() { _id = Name }.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult Update(List<MasterFundingType> updated)
        {
            try
            {
                updated = (updated == null ? new List<MasterFundingType>() : updated);
                var counter = 0;
                var success = false;
                var message = "";

                foreach (var each in updated)
                {
                    string id = Convert.ToString(each._id);
                    string name = Convert.ToString(each.Name);

                    var activities = WellActivity.Populate<WellActivity>(Query.EQ("EXType", name));
                    if (activities.Count > 0) continue;

                    var fundingNames = MasterFundingType.Populate<MasterFundingType>(Query.EQ("_id", id));
                    if (fundingNames.Count == 0) continue;

                    var fundingNamesUsingThisNewName = MasterFundingType.Populate<MasterFundingType>(Query.EQ("_id", name));
                    if (fundingNamesUsingThisNewName.Count > 0) continue;

                    DataHelper.Delete(each.TableName, Query.EQ("_id", id));
                    new MasterFundingType() { _id = name }.Save();

                    counter++;
                }

                if (counter == 0)
                {
                    success = false;
                    message = "Data that used in activity cannot be updated";
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

        public JsonResult Delete(string _id)
        {
            try
            {
                var success = false;
                var message = "";

                var activities = WellActivity.Populate<WellActivity>(Query.EQ("EXType", _id));
                if (activities.Count > 0)
                {
                    success = false;
                    message = "Data that used in activity cannot be deleted";
                }
                else
                {
                    DataHelper.Delete(new MasterFundingType().TableName, Query.EQ("_id", _id));
                    success = true;
                }

                return Json(new { Success = success, Message = message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult CreateCollectionFundingType()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                List<BsonDocument> docs = new List<BsonDocument>();

                BsonDocument bs1 = new BsonDocument();
                bs1.Set("_id", "EXPEX");
                bs1.Set("Name", "EXPEX");
                bs1.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
                docs.Add(bs1);

                BsonDocument bs2 = new BsonDocument();
                bs2.Set("_id", "CAPEX");
                bs2.Set("Name", "CAPEX");
                bs2.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
                docs.Add(bs2);

                BsonDocument bs3 = new BsonDocument();
                bs3.Set("_id", "ABEX");
                bs3.Set("Name", "ABEX");
                bs3.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
                docs.Add(bs3);

                BsonDocument bs4 = new BsonDocument();
                bs4.Set("_id", "OPEX");
                bs4.Set("Name", "OPEX");
                bs4.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
                docs.Add(bs4);

                BsonDocument bs5 = new BsonDocument();
                bs5.Set("_id", "C2E");
                bs5.Set("Name", "C2E");
                bs5.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
                docs.Add(bs5);

                BsonDocument bs6 = new BsonDocument();
                bs6.Set("_id", "EXPEX_SUCCESS");
                bs6.Set("Name", "EXPEX SUCCESS");
                bs6.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
                docs.Add(bs6);

                DataHelper.Save("WEISFundingTypes", docs);
                return "Save";
            });
        }
	}
}