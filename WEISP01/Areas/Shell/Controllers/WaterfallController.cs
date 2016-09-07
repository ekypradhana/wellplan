using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class WaterfallController : Controller
    {
        //
        // GET: /Shell/Waterfall/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetData(){
            return MvcResultInfo.Execute(() =>
            {
                return "ok";
            });
        }
	}
}