using System.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ECIS.AppServer.Areas.Palantir
{
    public class PalantirAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Palantir";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Palantir_default",
                "Palantir/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}