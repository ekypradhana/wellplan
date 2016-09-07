using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Globalization;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using System.Collections.Specialized;

using Microsoft.AspNet.Identity;
using ECIS.Identity;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class UserController : Controller
    {
        public ActionResult ChangePassword()
        {
            return View();
        }

        public JsonResult DoChangePassword(string password)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                string currentUserId = User.Identity.GetUserId();

                if (currentUserId == null)
                {
                    ri.Result = "NOK";
                    ri.Message = "User not logged in, please login first";

                    return MvcTools.ToJsonResult(ri);
                }

                var username = WebTools.LoginUser.UserName;
                var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                var userManager = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));

                var user = userManager.FindByName(username);
                var isFirstLogin = !user.HasChangePassword;
                String hashpassword = userManager.PasswordHasher.HashPassword(password);
                user.PasswordHash = hashpassword;
                user.HasChangePassword = true;
                userManager.Update(user);

                if (isFirstLogin)
                {
                    WebTools.LoginUser.HasChangePassword = true;
                    ri.Data = "first login";
                }

                var variables = new Dictionary<string, string>();
                variables["UserName"] = user.UserName;
                variables["Password"] = password;

                new Areas.Admin.Controllers.AdminUsersController().SendMail("UserChangePassword", new string[] { user.Email }, variables);
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }
	}
}