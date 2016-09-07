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
    [ECAuth(WEISRoles="Administrators")]
    public class WeisConfigController : Controller
    {
        // GET: Shell/WeisConfig
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult Get()
        {
            return MvcResultInfo.Execute(() =>
            {
                var pics = Config.GetConfigValue("WEISWRPICRoles", "").ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                var reviewers = Config.GetConfigValue("WEISWRReviewersRoles", "").ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

                return new{
                    Pics = pics,
                    Reviewers = reviewers
                };
            });
        }

        public JsonResult Save(string[] pics=null, string[] reviewers=null)
        {
            return MvcResultInfo.Execute(() =>
            {
                var Pics = pics == null ? "" : String.Join("|",pics);
                var Reviews = reviewers == null ? "" : String.Join("|", reviewers);

                Config cfgPic = new Config
                {
                    _id = "WEISWRPICRoles", ConfigModule = "WEIS", ConfigValue = Pics
                };
                cfgPic.Save();

                Config cfgReviews = new Config
                {
                    _id = "WEISWRReviewersRoles",
                    ConfigModule = "WEIS",
                    ConfigValue = Reviews
                };
                cfgReviews.Save();

                return "OK";
            });
        }

        public JsonResult GetMonthly()
        {
            return MvcResultInfo.Execute(() =>
            {
                var pics = Config.GetConfigValue("WEISMRPICRoles", "").ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                var reviewers = Config.GetConfigValue("WEISMRReviewersRoles", "").ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

                return new
                {
                    Pics = pics,
                    Reviewers = reviewers
                };
            });
        }

        public JsonResult SaveMonthly(string[] pics = null, string[] reviewers = null)
        {
            return MvcResultInfo.Execute(() =>
            {
                var Pics = pics == null ? "" : String.Join("|", pics);
                var Reviews = reviewers == null ? "" : String.Join("|", reviewers);

                Config cfgPic = new Config
                {
                    _id = "WEISMRPICRoles",
                    ConfigModule = "WEIS",
                    ConfigValue = Pics
                };
                cfgPic.Save();

                Config cfgReviews = new Config
                {
                    _id = "WEISMRReviewersRoles",
                    ConfigModule = "WEIS",
                    ConfigValue = Reviews
                };
                cfgReviews.Save();

                return "OK";
            });
        }

        public JsonResult GetHealthCheck()
        {
            return MvcResultInfo.Execute(() =>
            {
                var pics = Config.GetConfigValue("HealthCheckPIC", "").ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                var reviewers = Config.GetConfigValue("HealthCheckReviewer", "").ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

                return new
                {
                    Pics = pics,
                    Reviewers = reviewers
                };
            });
        }

        public JsonResult SaveHealthCheck(string[] pics = null, string[] reviewers = null)
        {
            return MvcResultInfo.Execute(() =>
            {
                var Pics = pics == null ? "" : String.Join("|", pics);
                var Reviews = reviewers == null ? "" : String.Join("|", reviewers);

                Config cfgPic = new Config
                {
                    _id = "HealthCheckPIC",
                    ConfigModule = "HealthCheck",
                    ConfigValue = Pics
                };
                cfgPic.Save();

                Config cfgReviews = new Config
                {
                    _id = "HealthCheckReviewer",
                    ConfigModule = "HealthCheck",
                    ConfigValue = Reviews
                };
                cfgReviews.Save();

                return "OK";
            });
        }
    }
}