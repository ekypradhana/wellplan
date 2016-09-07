using System.Web.Mvc;

namespace ECIS.AppServer.Areas.Shell
{
    public class ShellAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Shell";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Shell_default",
                "Shell/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional },
                new string[] { "ECIS.AppServer.Areas.Shell.Controllers" }
            );
        }
    }
}