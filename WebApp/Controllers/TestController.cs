using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using Microsoft.AspNet.Identity;
using ECIS.Identity;
using ECIS.AppServer.Models;
using Microsoft.Owin.Security;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.IO;
using System.Web.Configuration;
using Newtonsoft.Json;
namespace ECIS.AppServer.Controllers
{
    public class BatchController : Controller
    {
      
        public JsonResult TestMR()
        {
            MapReduceDocument doc = DataHelper.MapReduce("Usages",
                @"function(){emit(this.USAGE_TIME.getHours(),{count:1});}",
                @"function(key,values){var reduced={count:0};values.forEach(function(obj){reduced.count+=obj.count});return reduced;}"); 
            return MvcTools.ToJsonResult(doc.ToBsonDocument().ToDictionary());
        }

        public JsonResult TestRandom()
        {
            List<String> filenames = new List<string>();
            for (int i = 0; i <= 100; i++)
            {
                filenames.Add(Tools.GenerateRandomString(120,false,"TEMP_{0}"));
            }
            return MvcTools.ToJsonResult(filenames);
        }

        public JsonResult CompileRun()
        {
            Tools.BinaryPath = @"C:\Users\ariefdarmawan\Documents\SVNRepo\ECIS.WebApp.Airtel\bin";
            string code = @"using System; " +
                "namespace ECIS{ " +
                "public class CodeExtention{ " +
                "public static string Main(){return \"Arief Darmawan\";} " +
                "}}";
            ResultInfo ri = Tools.RunCode(code);
            return ri.ToJsonResult();
        }

        public JsonResult Install()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                var userMgr = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));
                userMgr.Create(new IdentityUser
                {
                    UserName = "eaciit",
                    Email = "eaciit@eaciit.com",
                    ConfirmedAtUtc = Tools.ToUTC(DateTime.Now)
                }, "Password.1");

                Config cfg = new Config
                {
                    _id = "LandingAction",
                    Title = "Global first page where user will landing on",
                    ConfigModule = "Global",
                    ConfigValue = "~/BizTrack/dashboard"
                };
                cfg.Save();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            ri.CalcLapseTime();
            return ri.ToJsonResult();
        }

        public JsonResult AddUser(string username, string email, string password)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                var userMgr = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));
                userMgr.Create(new IdentityUser
                {
                    UserName = username,
                    Email = email,
                    ConfirmedAtUtc = Tools.ToUTC(DateTime.Now)
                }, password);
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            ri.CalcLapseTime();
            return ri.ToJsonResult();
        }
 	}
}