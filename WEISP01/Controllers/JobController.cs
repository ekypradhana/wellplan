using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Driver.Builders;

namespace ECIS.AppServer.Controllers
{
    public class JobController : Controller
    {
        //
        // GET: /Job/
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult QuickLoad(string callback)
        {
            return View();
        }

        public ActionResult JobUpload()
        {
            return View();
        }
	}
}