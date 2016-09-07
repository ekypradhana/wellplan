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
    public class AppsConfigController : Controller
    {
        //
        // GET: /Shell/AppsConfig/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult Save(int WordCount)
        {
            try
            {

                WEISAppsConfig conf = new WEISAppsConfig();
                conf.WordCount = WordCount;
                conf._id = 1;
                conf.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult Get()
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                var Data = WEISAppsConfig.Get<WEISAppsConfig>(Query.EQ("_id", 1));
                return Data;
            });
        }

	}

    internal class WEISAppsConfig : ECISModel
    {
        public override string TableName
        {
            get { return "WEISAppsConfig"; }
        }

        //public int _id { get; set; }
        public int WordCount { get; set; }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            doc = base.PreSave(doc);
            this._id = 1;
            doc = this.ToBsonDocument();
            return doc;
        }

    }

}