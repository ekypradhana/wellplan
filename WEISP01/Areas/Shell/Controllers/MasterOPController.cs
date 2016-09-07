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
    public class MasterOPController : Controller
    {
        //
        // GET: /Shell/MasterOP/
        public ActionResult Index()
        {
            return View();
        }

        public IMongoQuery Like(string Keyword = null)
        {
            Keyword = (Keyword == null ? "" : Keyword).Trim();
            if (Keyword.Equals("")) return null;

            var queries = new List<IMongoQuery>();
            queries.Add(Query.Matches("_id", new BsonRegularExpression(new Regex(Keyword.ToString().ToLower(), RegexOptions.IgnoreCase))));

            return Query.Or(queries.ToArray());
        }
         
        public JsonResult Populate(string Keyword = null)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                return MasterOP.Populate<MasterOP>(new MasterOPController().Like(Keyword)).OrderBy(d => d._id)
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
                var d = MasterOP.Get<MasterOP>(id);
                d.Name = Convert.ToString(d._id);
                return d;
            });
        }

        public JsonResult Save(string Name)
        {
            try
            {
                if (MasterOP.Populate<MasterOP>(new MasterOPController().Like(Name)).Count > 0)
                {
                    return Json(new { Success = false, Message = "Name already exists" }, JsonRequestBehavior.AllowGet);
                }

                new MasterOP() { _id = Name , Name = Name}.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult Update(List<MasterOP> updated)
        {
            try
            {
                updated = (updated == null ? new List<MasterOP>() : updated);
                var counter = 0;
                var success = false;
                var message = "";

                foreach (var each in updated)
                {
                    string id = Convert.ToString(each._id);
                    string name = Convert.ToString(each.Name);

                    var getOP = MasterOP.Populate<MasterOP>(Query.EQ("_id", id));
                    if (getOP.Count == 0) continue;

                    var rigNamesUsingThisNewName = MasterOP.Populate<MasterOP>(Query.EQ("_id", name));
                    if (rigNamesUsingThisNewName.Count > 0) continue;

                    DataHelper.Delete(each.TableName, Query.EQ("_id", id));
                    new MasterOP() { _id = name, Name = name }.Save();

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

                var actv = WellActivity.Populate<WellActivity>().SelectMany(x => x.Phases, (x, p) => new { BaseOP = p.BaseOP })
                    .Where(x => x.BaseOP.Contains(_id));
                
                if (actv.Count() > 0)
                {
                    success = false;
                    message = "Data that used in activity cannot be deleted";
                }
                else
                {
                    DataHelper.Delete(new MasterOP().TableName, Query.EQ("_id", _id));
                    success = true;
                }
                return Json(new { Success = success, Message = message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }
	}
}