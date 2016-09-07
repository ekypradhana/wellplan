using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class DocumentController : Controller
    {
        //
        // GET: /Shell/Document/
        public ActionResult Index()
        {
            return View();
        }
	}
}