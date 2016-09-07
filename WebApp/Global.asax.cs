using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.IO;
using ECIS.Core;
using ECIS.Client.WEIS;

namespace ECIS.AppServer
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            ECIS.Core.License.ConfigFolder = Server.MapPath("~/App_Data/License/");

            String AsposeLicense = Path.Combine(Server.MapPath("~/App_Data/License"), "Aspose.Total.lic");
            if (File.Exists(AsposeLicense))
            {
                new Aspose.Cells.License().SetLicense(AsposeLicense);
                new Aspose.Pdf.License().SetLicense(AsposeLicense);
                //new Aspose.Words.License().SetLicense(AsposeLicense);
                //new Aspose.Slides.License().SetLicense(AsposeLicense);
            }
        }

        private static void _SaveUserLog(UserLogType LogType)
        {
            //Save to UserLog

            var UserName = WebTools.LoginUser.UserName;
            var Email = WebTools.LoginUser.Email;

            var LogUser = new WEISUserLog();
            LogUser.UserName = UserName;
            LogUser.Email = Email;
            LogUser.Time = Tools.ToUTC(DateTime.Now);
            LogUser.LogType = LogType.ToString();
            if(UserName != null) LogUser.Save(references:null);
        }

        protected void Session_OnStart()
        {
            _SaveUserLog(UserLogType.Login);
        }

        protected void Session_OnEnd()
        {
            _SaveUserLog(UserLogType.Logout);
        }
    }
}
