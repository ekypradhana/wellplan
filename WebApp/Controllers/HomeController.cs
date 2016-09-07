using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core;
using ECIS.AppServer.Models;
using MongoDB.Bson;
using MongoDB.Driver.Builders;

namespace ECIS.AppServer.Controllers
{
    public class HomeController : Controller
    {
        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            base.Initialize(requestContext);
        }

        public ActionResult Index()
        {
            bool authenticated = User.Identity.IsAuthenticated;
            if (!authenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                return Redirect(Config.GetConfigValue("LandingAction").ToString());
            }
        }
    }
}