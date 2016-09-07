using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ECIS.Client.WEIS;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class MasterActivityCategoryController : Controller
    {
        // GET: Shell/AdminWell
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult AddData()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var t = ActivityMaster.Populate<ActivityMaster>();
                foreach (var x in t)
                {
                    if (x.ActivityCategory != null)
                    {
                        var actCat = ActivityCategory.Get<ActivityCategory>(Query.EQ("Title", x.ActivityCategory));
                        if (actCat == null)
                        {
                            var a = new ActivityCategory();
                            a.Title = x.ActivityCategory;
                            a.Save();
                        }
                    }
                }
                return null;
            });
        }
        public JsonResult Populate()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                return ActivityCategory.Populate<ActivityCategory>();
            });
        }
        public JsonResult Get(object id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                return ActivityCategory.Get<ActivityCategory>(id);
            });
        }

        public JsonResult Save(string Name)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var a = new ActivityCategory();
                a.Title = Name;
                a.Save();
                return null;
            });
        }
        public JsonResult Update(List<ActivityCategory> updated)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                foreach (var x in updated)
                    x.Save();
                return null;
            });
        }
        public JsonResult Delete(int _id)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var a = ActivityCategory.Get<ActivityCategory>(_id);
                a.Delete();
                return null;
            });
        }
	}
}