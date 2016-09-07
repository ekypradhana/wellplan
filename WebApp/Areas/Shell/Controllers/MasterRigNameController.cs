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
    public class MasterRigNameController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Master Rig Name";
            return View();
        }

        public IMongoQuery Like(string Keyword = null)
        {
            Keyword = (Keyword == null ? "" : Keyword).Trim();
            if (Keyword.Equals("")) return null;

            var queries = new List<IMongoQuery>();
            queries.Add(Query.Matches("_id", new BsonRegularExpression(new Regex(Keyword.ToString().ToLower(), RegexOptions.IgnoreCase))));
            queries.Add(Query.Matches("RigType", new BsonRegularExpression(new Regex(Keyword.ToString().ToLower(), RegexOptions.IgnoreCase))));

            return Query.Or(queries.ToArray());
        }

        public JsonResult Populate(string Keyword = null, bool ShowOnlyAvailableStreaming = false)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var rigNm = WEISStream.Populate<WEISStream>();
                var ret = MasterRigName.Populate<MasterRigName>(Like(Keyword)).OrderBy(d => d._id)
                    .Select(d => 
                    { 
                        d.Name = Convert.ToString(d._id);
                        d.CountStream = rigNm.Count(x => x.RigName.Equals(Convert.ToString(d._id)));
                        return d; 
                    });
                if (ShowOnlyAvailableStreaming)
                    ret = ret.Where(x => x.CountStream > 0);
                return ret;
            });
        }

        public JsonResult Get(object id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var d = MasterRigName.Get<MasterRigName>(id);
                d.Name = Convert.ToString(d._id); 
                return d;
            });
        }

        public JsonResult Save(string Name, string RigType, bool isOfficeLocation)
        {
            try
            {
                if (MasterRigName.Populate<MasterRigName>(Like(Name)).Count > 0)
                {
                    return Json(new { Success = false, Message = "Name already exists" }, JsonRequestBehavior.AllowGet);
                }

                new MasterRigName() { _id = Name, RigType = RigType, isOfficeLocation = isOfficeLocation }.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult Update(List<MasterRigName> updated)
        {
            try
            {
                updated = (updated == null ? new List<MasterRigName>() : updated);
                var counter = 0;
                var success = false;
                var message = "";

                foreach (var each in updated)
                {
                    string id = Convert.ToString(each._id);
                    string name = Convert.ToString(each.Name);
                    bool isOfficeLocation = each.isOfficeLocation;

                    if (each.Name.Equals(each._id)) {
                        var rigName = MasterRigName.Get<MasterRigName>(Query.EQ("_id", id));

                        rigName.RigType = (rigName.RigType == null ? "" : rigName.RigType);
                        each.RigType = (each.RigType == null ? "" : each.RigType);

                        if ((!rigName.RigType.Equals(each.RigType)) || (!rigName.isOfficeLocation.Equals(each.isOfficeLocation)))
                        {
                            rigName.RigType = each.RigType;
                            rigName.isOfficeLocation = each.isOfficeLocation;
                            rigName.Save();

                            counter++;
                            continue;
                        }
                    }

                    var activities = WellActivity.Populate<WellActivity>(Query.EQ("RigName", id));
                    if (activities.Count > 0) continue;

                    var rigNames = MasterRigName.Populate<MasterRigName>(Query.EQ("_id", id));
                    if (rigNames.Count == 0) continue;

                    var rigNamesUsingThisNewName = MasterRigName.Populate<MasterRigName>(Query.EQ("_id", name));
                    if (rigNamesUsingThisNewName.Count > 0) continue;

                    DataHelper.Delete(each.TableName, Query.EQ("_id", id));
                    new MasterRigName() { _id = name, RigType = each.RigType, isOfficeLocation = isOfficeLocation }.Save();

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

                var activities = WellActivity.Populate<WellActivity>(Query.EQ("RigName", _id));
                if (activities.Count > 0)
                {
                    success = false;
                    message = "Data that used in activity cannot be deleted";
                }
                else
                {
                    DataHelper.Delete(new MasterRigName().TableName, Query.EQ("_id", _id));
                    success = true;
                }

                return Json(new { Success = success, Message = message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult SaveStream(WEISStream record)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                record.Save();
                return "";
            });
        }

        public JsonResult PopulateStream(string rigName)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var ret = WEISStream.Populate<WEISStream>(q: Query.EQ("RigName", rigName));
                return ret;
            });
        }

        public JsonResult SaveChanges(List<WEISStream> records)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var ret = new List<WEISStream>();
                foreach (var x in records)
                    x.Save();
                if (records.Count() > 0)
                    ret = WEISStream.Populate<WEISStream>(q: Query.EQ("RigName", records.FirstOrDefault().RigName));
                return ret;
            });
        }

        public JsonResult DeleteStream(int id)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var rigNm = "";
                var ret = WEISStream.Get<WEISStream>(id);
                var pop = new List<WEISStream>();
                if (ret != null)
                {
                    ret.Delete();
                    rigNm = ret.RigName;
                    pop = WEISStream.Populate<WEISStream>(q: Query.EQ("RigName", rigNm));
                }
                return pop;
            });
        }

	}
}