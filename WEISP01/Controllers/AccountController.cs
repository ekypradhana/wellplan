using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core;
using ECIS.Identity;
using ECIS.AppServer.Models;
using Owin;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using ECIS.Client.WEIS;

namespace ECIS.AppServer.Controllers
{
    public class AccountController : Controller
    {
        //
        // GET: /Account/
        public ActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return Redirect(Config.GetConfigValue("LandingAction").ToString());
            }
            return View();
        }

        public ActionResult Login()
        {
            return View();
        }

        public RedirectToRouteResult Logout()
        {
            if (WebTools.LoginUser != null)
            {
                string UserName = WebTools.LoginUser.UserName;
                string Email = WebTools.LoginUser.Email;
                SaveUserLog(UserName, Email, UserLogType.Logout);
            }
            IAuthenticationManager auth = HttpContext.GetOwinContext().Authentication;
            auth.SignOut();
            return RedirectToAction("Login", "Account");
        }

        public JsonResult LoginProcess(string UserName, string Password, bool RememberMe)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                UserName = UserName.ToLower();
                IAuthenticationManager auth = HttpContext.GetOwinContext().Authentication;
                if (User.Identity.IsAuthenticated)
                    throw new Exception("Authenticated, please logout first");

                var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                var userMgr = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));
                var user = userMgr.Find(UserName, Password);
                if (user == null) throw new Exception("Invalid UserName or Password");
                ri.Message = "OK";

                var identity = userMgr.CreateIdentity(user, DefaultAuthenticationTypes.ApplicationCookie);
                auth.SignIn(new AuthenticationProperties { IsPersistent = true, 
                    IssuedUtc = Tools.ToUTC(DateTime.Now), 
                    ExpiresUtc = Tools.ToUTC(DateTime.Now).AddDays(1) },
                    identity);

                ri.Data = user;
                var Email = WebTools.LoginUser.Email;

                SaveUserLog(UserName, Email, UserLogType.Login);

            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return Json(ri, JsonRequestBehavior.AllowGet);
        }

        public static void SaveUserLog(string UserName,string Email, UserLogType LogType)
        {
            //Save to UserLog
            var LogUser = new WEISUserLog();
            LogUser.UserName = UserName;
            LogUser.Email = Email;
            LogUser.Time = Tools.ToUTC(DateTime.Now);
            LogUser.LogType = LogType.ToString();
            if (LogUser.UserName != null)
            LogUser.Save(references: null);
        }

        public JsonResult ForgetPassword(string UserEmail)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                UserEmail = UserEmail.ToLower();
                IdentityUser user = new IdentityUser();
                IMongoQuery q = Query.EQ("Email",UserEmail);
                user = DataHelper.Get<IdentityUser>("Users", q);
                if (user == null) throw new Exception("Email not registered");
                string SecretToken = Tools.GenerateRandomString(35);

                var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                var userManager = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));

                var user2 = userManager.FindByName(user.UserName);
                user2.SecretToken = SecretToken;
                userManager.Update(user2);

                //send email
                Dictionary<string, string> variables = new Dictionary<string, string>();
                variables.Add("URLResetPassword", "http://" + Request.Url.Host + Url.Action("ResetPassword", "Account") + "/?SecretToken=" + SecretToken);
                var riSend = Email.Send("UserForgetPassword",
                                    new string[] { UserEmail },
                                    variables: variables,
                                    developerModeEmail: "arief@eaciit.com");
                if (riSend.Result != "OK")
                    throw new Exception(riSend.Message + riSend.Trace);

            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return Json(ri, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ResetPassword(string SecretToken)
        {
            IdentityUser user = new IdentityUser();
            IMongoQuery q = Query.EQ("SecretToken", SecretToken);
            user = DataHelper.Get<IdentityUser>("Users", q);

            if (user == null)
            {
                ViewBag.Status = "NOK";
                ViewBag.SecretToken = "";
            }
            else
            {
                ViewBag.SecretToken = SecretToken;
                ViewBag.Status = "OK";
            }
            
            return View();
        }

        public JsonResult DoResetPassword(string Password, string SecretToken)
        {
            ResultInfo ri = new ResultInfo();
            try
            {

                IdentityUser DetailUser = new IdentityUser();
                IMongoQuery q = Query.EQ("SecretToken", SecretToken);
                DetailUser = DataHelper.Get<IdentityUser>("Users", q);
                if (DetailUser == null) throw new Exception("Secret Token not found!");

                var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                var userManager = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));

                var user = userManager.FindByName(DetailUser.UserName);
                String hashpassword = userManager.PasswordHasher.HashPassword(Password);
                user.PasswordHash = hashpassword;
                user.SecretToken = "";
                userManager.Update(user);

                //send email
                Dictionary<string, string> variables = new Dictionary<string, string>();
                variables.Add("UserName", DetailUser.UserName);
                variables.Add("Password", Password);
                Email.Send("UserChangePassword",
                                    new string[] { DetailUser.Email },
                                    variables: variables,
                                    developerModeEmail: "eky.pradhana@eaciit.com");
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

	}
}