using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ECIS.Core;
using ECIS.Core.DataSerializer;
using ECIS.Client.WEIS;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using ECIS.Identity;
using System.Text.RegularExpressions;
using ECIS.AppServer.Areas.WebMenu.Models;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class TestEmailController : Controller
    {
        //
        // GET: /Shell/TestEmail/
        public ActionResult Index()
        {
            ViewBag.Title = "Test Email";
            return View();
        }

        public JsonResult SendEmailAdvance(EmailModel email)
        {
            return MvcResultInfo.Execute(() =>
            {
                var s = email.SendEmail();
                if (s != "OK") throw new Exception(s);
                return "OK";
            });
            //return MvcTools.ToJsonResult(email.SendEmail());
        }
        public JsonResult SendEmail()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                //test
                var email = Email.Get<Email>("TestEmail");
                if (email == null)
                {
                    var a = new Email();
                    a._id = "TestEmail";
                    a.Body = "Test Email";
                    a.Subject = "Test Email From WEIS";
                    a.Title = "Test Email";
                    a.SMTPConfig = "Default";
                    email = a;
                    a.Save();
                }

                string[] toMail = new string[] { "arfan.pantua@eaciit.com" };//WebTools.LoginUser.Email
                var e = ECIS.Client.WEIS.Email.Send("TestEmail",
                toMail, null, //persons.Select(d => d.Email).ToArray()
                developerModeEmail: "arfan.pantua@eaciit.com");
                //developerModeEmail: "mas.muhammad@eaciit.com");
                if (e.Message == "Message sent")
                {
                    e.Data = new
                    {
                        To = "arfan.pantua@eaciit.com",//WebTools.LoginUser.Email
                        Subject = email.Subject
                    };
                }
                return e;
            });
        }
	}
}