using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Driver.Builders;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class BaseOPSettingController : Controller
    {
        //
        // GET: /Shell/BaseOPSetting/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetBaseOPConfig()
        {
            return MvcResultInfo.Execute(() =>
            {
                var config = new BsonDocument();

                Config.Populate<Config>(Query.EQ("ConfigModule", "BaseOPDefault")).ForEach(d =>
                {
                    var doc = d.ToBsonDocument();
                    config.Set(doc.GetString("_id"), doc.Get("ConfigValue"));
                });

                return config.ToDictionary();
            });
        }

        public JsonResult PopulateOP()
        {
            return MvcResultInfo.Execute(() => 
            {
                var opdata = MasterOP.Populate<MasterOP>().Select(x=>x._id);
                return opdata;
            });
        }
        public JsonResult SaveBaseOPConfig(string BaseOP)
        {
            return MvcResultInfo.Execute(() =>
            {
                var config1 = Config.Get<Config>("BaseOPConfig") ?? new Config() { _id = "BaseOPConfig", ConfigModule = "BaseOPDefault" };
                config1.ConfigValue = BaseOP;
                config1.Save();

                return "OK";
            });
        }
	}
}