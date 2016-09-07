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
using ECIS.AppServer.Controllers;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class RestAPIController : RestAPIContainer
    {
        [HttpPost]
        public JsonResult Login(string username, string password)
        {
            IAuthenticationManager auth = HttpContext.GetOwinContext().Authentication;

            if (!IsRestApiEnabled())
            {
                return Json(new RestMessage()
                {
                    success = false,
                    message = "Rest API is disabled"
                });
            }

            if (User.Identity.IsAuthenticated)
            {
                var userToken = DataHelper.Get(tableName, Query.EQ("username", WebTools.LoginUser.UserName));

                if (userToken != null)
                {
                    if (!IsTokenExpired())
                    {
                        return Json(new
                        {
                            success = true,
                            message = "Authenticated",
                            token = userToken.GetString("token")
                        });
                    }
                }

                auth.SignOut();
            }

            username = username.ToLower();
            var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
            var userMgr = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));
            var user = userMgr.Find(username, password);

            if (user == null)
            {
                return Json(new RestMessage()
                {
                    success = false,
                    message = "Invalid username or password"
                });
            }

            auth.SignIn(new AuthenticationProperties
            {
                IsPersistent = true,
                IssuedUtc = Tools.ToUTC(DateTime.Now),
                ExpiresUtc = Tools.ToUTC(DateTime.Now).AddMinutes(GetTokenExpiredDuration())
            }, userMgr.CreateIdentity(user, DefaultAuthenticationTypes.ApplicationCookie));

            //AccountController.SaveUserLog(username, Email, UserLogType.Login);

            return Json(new
            {
                success = true,
                message = "",
                token = GenerateToken(username)
            });
        }

        [HttpPost]
        public JsonResult Logout()
        {
            IAuthenticationManager auth = HttpContext.GetOwinContext().Authentication;
            auth.SignOut();

            return Json(new RestMessage()
            {
                success = true,
                message = ""
            });
        }
        
        [HttpPost]
        public JsonResult GetToken()
        {
            return Json(new
            {
                success = true,
                message = "",
                token = GenerateToken()
            });
        }

        public JsonResult Test(string token = null)
        {
            try
            {
                var authRequest = AuthRequest(token);
                if (authRequest != null) return Json(authRequest);

                return Json("halo");
            }
            catch (Exception e)
            {
                return Json(new
                {
                    success = false,
                    message = e.Message
                });
            }
        }

	}

    public class RestAPIContainer : Controller
    {
        protected string tableName = "RestApiToken";

        protected string GenerateToken(string uname = null)
        {
            var username = (uname == null) ? WebTools.LoginUser.UserName : uname;
            var token = DateTimeToBase64(DateTime.UtcNow);

            DataHelper.Delete(tableName, Query.EQ("username", username));
            DataHelper.Save(tableName, new
            {
                username = username,
                token = token,
                createdAt = Tools.ToUTC(DateTime.Now),
                updateAt = Tools.ToUTC(DateTime.Now),
            }.ToBsonDocument());

            return token;
        }

        protected string DateTimeToBase64(DateTime dateTime)
        {
            byte[] time = BitConverter.GetBytes(dateTime.ToBinary());
            byte[] key = Guid.NewGuid().ToByteArray();
            return Convert.ToBase64String(time.Concat(key).ToArray());
        }

        protected DateTime Base64ToDateTime(string token)
        {
            byte[] data = Convert.FromBase64String(token);
            return DateTime.FromBinary(BitConverter.ToInt64(data, 0));
        }

        protected double GetTokenExpiredDuration()
        {
            return (DataHelper.Get("SharedConfigTable", Query.EQ("_id", "RestApiTokenDuration")) ?? new BsonDocument())
                .GetDouble("Value");
        }

        protected bool IsRestApiEnabled()
        {
            return (DataHelper.Get("SharedConfigTable", Query.EQ("_id", "RestApiEnabled")) ?? new BsonDocument())
                .GetBool("Value");
        }

        protected bool IsTokenExpired()
        {
            var tableName = "RestApiToken";
            var username = WebTools.LoginUser.UserName ?? "";
            var config = DataHelper.Get(tableName, Query.EQ("username", username));
            var now = Tools.ToUTC(DateTime.Now);

            if (config == null)
                return true;

            var expiredDate = config.GetDateTime("updateAt").AddMinutes(GetTokenExpiredDuration());

            return (now > expiredDate);
        }

        protected bool IsTokenValid(string token)
        {
            var username = WebTools.LoginUser.UserName ?? "";
            var config = DataHelper.Get(tableName, Query.And(
                Query.EQ("username", username),
                Query.EQ("token", token)
            ));

            return (config != null);
        }

        protected RestMessage AuthRequest(string token)
        {
            RestMessage res = null;

            if (!IsRestApiEnabled())
            {
                res = new RestMessage()
                {
                    success = false,
                    message = "Rest API is disabled"
                };
            }
            else if (!User.Identity.IsAuthenticated)
            {
                res = new RestMessage()
                {
                    success = false,
                    message = "Do not have permission to access"
                };
            }
            else if (token == null || "".Equals(token))
            {
                res = new RestMessage()
                {
                    success = false,
                    message = "Token required"
                };
            }
            else if (!IsTokenValid(token))
            {
                res = new RestMessage()
                {
                    success = false,
                    message = "Token invalid"
                };
            }
            else if (IsTokenExpired())
            {
                res = new RestMessage()
                {
                    success = false,
                    message = "Token expired"
                };
            }

            return res;
        }
    }

    public class RestMessage
    {
        public bool success { set; get; }
        public string message { set; get; }
    }
}