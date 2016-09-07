using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.IO;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;


namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class FreezedOPController : Controller
    {
        //
        // GET: /Shell/FreezedOP/
        public ActionResult Index()
        {
            var isAdmin = _isAdmin();
            if (isAdmin)
            {
                ViewBag.isAdmin = "1";
            }
            else
            {
                ViewBag.isAdmin = "0";
            }
            return View();
        }

        private static bool _isAdmin()
        {
            var Email = WebTools.LoginUser.Email;
            List<string> Roles = WEISPerson.GetRolesByEmail(Email);
            var admin = Roles.Where(x => x.ToUpper() == "ADMINISTRATORS" || x.ToLower().Contains("app-support"));
            bool isAdmin = false;
            if (admin.Count() > 0)
            {
                isAdmin = true;
            }
            return isAdmin;
        }

        public JsonResult Populate()
        {
            return MvcResultInfo.Execute(() =>
            {
                var MasterOPs = new List<string>() { "OP14", "OP15", "OP16" };
                var FreezedOPs = WEISFreezedOP.Populate<WEISFreezedOP>();
                return new
                {
                    MasterOPs,
                    FreezedOPs
                };
            });
        }

        public JsonResult Freeze(string OP)
        {
            return MvcResultInfo.Execute(() =>
            {
                WEISFreezedOP fop = new WEISFreezedOP();
                fop._id = OP;
                fop.Save();
                return "OK";
            });
        }
        public JsonResult UnFreeze(string OP)
        {
            return MvcResultInfo.Execute(() =>
            {
                DataHelper.Delete(new WEISFreezedOP().TableName, Query.EQ("_id",OP));
                return "OK";
            });
        }
        
	}
}