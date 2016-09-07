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
    public class BizPlanSettingController : Controller
    {
        //
        // GET: /Shell/BizPlanSetting/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetBizConfig()
        {
            return MvcResultInfo.Execute(() =>
            {
                var config = new BsonDocument();

               

                if (Config.Get<Config>("CompoundFactor") == null)
                {
                    var config4 = Config.Get<Config>("CompoundFactor") ?? new Config() { _id = "CompoundFactor", ConfigModule = "BizPlan" };
                    config4.ConfigValue = 2;
                    config4.Save();
                }

                if (Config.Get<Config>("CompoundFactorYear") == null)
                {
                    var config4 = Config.Get<Config>("CompoundFactorYear") ?? new Config() { _id = "CompoundFactorYear", ConfigModule = "BizPlan" };
                    config4.ConfigValue = 2018;
                    config4.Save();
                }

                Config.Populate<Config>(Query.EQ("ConfigModule", "BizPlan")).ForEach(d =>
                {
                    var doc = d.ToBsonDocument();
                    config.Set(doc.GetString("_id"), doc.Get("ConfigValue"));
                });

                return config.ToDictionary();
            });
        }

        public JsonResult SaveBizConfig(bool AlloWUpdateWellPlan, List<string> RolesToEditBizPlan, List<string> VisibleCurrency)//, double CompoundFactor, double CompoundFactorYear
        {
            return MvcResultInfo.Execute(() =>
            {
                var config1 = Config.Get<Config>("BizPlanConfig") ?? new Config() { _id = "BizPlanConfig", ConfigModule = "BizPlan" };
                config1.ConfigValue = AlloWUpdateWellPlan;
                config1.Save();

                if (RolesToEditBizPlan == null) RolesToEditBizPlan = new List<string>();
                var config2 = Config.Get<Config>("RolesToEditBizPlan") ?? new Config() { _id = "RolesToEditBizPlan", ConfigModule = "BizPlan" };
                config2.ConfigValue = string.Join(",", RolesToEditBizPlan);
                config2.Save();

                if (VisibleCurrency == null) VisibleCurrency = new List<string>();
                var config3 = Config.Get<Config>("VisibleCurrency") ?? new Config() { _id = "VisibleCurrency", ConfigModule = "BizPlan" };
                config3.ConfigValue = string.Join(",", VisibleCurrency);
                config3.Save();

                //if (CompoundFactor == null) CompoundFactor = 0;
                //var config4 = Config.Get<Config>("CompoundFactor") ?? new Config() { _id = "CompoundFactor", ConfigModule = "BizPlan" };
                //config4.ConfigValue = CompoundFactor;
                //config4.Save();

                //if (CompoundFactorYear == null) CompoundFactorYear = 0;
                //var config5 = Config.Get<Config>("CompoundFactorYear") ?? new Config() { _id = "CompoundFactorYear", ConfigModule = "BizPlan" };
                //config5.ConfigValue = CompoundFactorYear;
                //config5.Save();


                return "OK";
            });
        }
	}
}