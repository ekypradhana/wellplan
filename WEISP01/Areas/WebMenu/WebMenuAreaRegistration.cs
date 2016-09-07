using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ECIS.AppServer.Areas.WebMenu
{
    public class WebMenuAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "WebMenu";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "WebMenu_default",
                "WebMenu/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}