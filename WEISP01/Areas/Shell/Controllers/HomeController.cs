﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class HomeController : Controller
    {
        // GET: Shell/Home
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Dashboard");
        }
    }
}