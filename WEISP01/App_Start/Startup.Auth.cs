using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using System.Web.Configuration;

namespace ECIS.AppServer
{
    public partial class Startup
    {
        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
             var CookieDomain = 
                WebConfigurationManager.AppSettings["CookieDomain"] == null || 
                WebConfigurationManager.AppSettings["CookieDomain"] == "" ?
                null :
                WebConfigurationManager.AppSettings["CookieDomain"];

            var CookieName = WebConfigurationManager.AppSettings["CookieName"] == null ||
                WebConfigurationManager.AppSettings["CookieName"] == "" ?
                "ECISIdentity" :
                WebConfigurationManager.AppSettings["CookieName"];

            // Enable the application to use a cookie to store information for the signed in user
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                CookieDomain = CookieDomain,
                CookieName = CookieName, 
                LoginPath = new PathString("/Account/Login")
            });
            
            // Use a cookie to temporarily store information about a user logging in with a third party login provider
            //app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Uncomment the following lines to enable logging in with third party login providers
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            //app.UseTwitterAuthentication(
            //   consumerKey: "",
            //   consumerSecret: "");

            //app.UseFacebookAuthentication(
            //   appId: "",
            //   appSecret: "");

            //app.UseGoogleAuthentication();
        }
    }
}