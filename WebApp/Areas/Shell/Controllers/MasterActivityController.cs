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
    public class MasterActivityController : Controller
    {
        // GET: Shell/AdminWell
        public ActionResult Index()
        {
            return View();
        }
        public JsonResult Populate()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var datas = DataHelper.Populate("WEISActivities");
                List<MasterActivity> activities = new List<MasterActivity>();
                foreach (var t in datas)
                {
                    activities.Add(new MasterActivity
                    {
                        _id = BsonHelper.GetString(t, "_id"),
                        EDMActivityId = BsonHelper.GetString(t, "EDMActivityId"),
                        LastUpdate = BsonHelper.GetDateTime(t, "LastUpdate"),
                    });
                }

                return activities;
            });
        }
        public JsonResult Get(object id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                return MasterActivity.Get<MasterActivity>(id);
            });
        }

        public JsonResult Save(string _id, string EDMActivityId, string isUpdate)
        {
            try
            {
                if (isUpdate.Equals("1"))
                {
                    var datas = DataHelper.Populate("WEISActivities", Query.EQ("_id", _id));
                    if (datas != null && datas.Count > 0)
                    {
                        MasterActivity wpc = new MasterActivity();
                        wpc._id = _id.Trim();
                        wpc.EDMActivityId = EDMActivityId;
                        wpc.LastUpdate = Tools.ToUTC(DateTime.Now);
                        DataHelper.Save("WEISActivities", wpc.ToBsonDocument());
                    }
                    else
                    {
                        throw new Exception("WEIS Activity Name not exist on WEIS, try to Refresh page and reselect well activity data");
                    }
                }
                else
                { 
                    // insert
                    var datas = DataHelper.Populate("WEISActivities", Query.EQ("_id" , _id));
                    if (datas != null && datas.Count > 0)
                    {
                        throw new Exception("WEIS Activity Name already exist on WEIS");
                    }
                    else
                    {
                        MasterActivity wpc = new MasterActivity();
                        wpc._id = _id.Trim();
                        wpc.EDMActivityId = EDMActivityId;
                        wpc.LastUpdate = Tools.ToUTC(DateTime.Now);
                        DataHelper.Save("WEISActivities", wpc.ToBsonDocument());
                    }

                }
                
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
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
                IMongoQuery q = null;
                q = Query.EQ("_id", _id);
                MasterActivity wpc = MasterActivity.Get<MasterActivity>(q);

                string OldName = wpc._id.ToString();

                q = Query.EQ("Phases.ActivityType", OldName);
                var wp = DataHelper.Populate("WEISWellActivities", q);
                if (wp.Count <= 0)
                {
                    DataHelper.Delete(wpc.TableName, Query.EQ("_id", _id));
                    return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    throw new Exception("Activity cannot be delete because it's already used!");
                }
                return null;
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

    }
}