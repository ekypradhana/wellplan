using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core;
using ECIS.Client.WEIS;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth(WEISRoles = "Administrators")]
    public class EmailController : Controller
    {
        // GET: Shell/Email
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult Search(string search)
        {
            return MvcResultInfo.Execute(() =>
            {
                return Email.Populate<Email>();
            });
        }

        public JsonResult Get(string id)
        {
            return MvcResultInfo.Execute(() =>
            {
                var email = Email.Get<Email>(id);
                return email;
            });
        }

        public JsonResult Save(Email email, bool isNew)
        {
            return MvcResultInfo.Execute(() =>
            {
                if (isNew)
                {
                    var existingEmail = Email.Get<Email>(email._id);
                    if (existingEmail != null) throw new Exception(String.Format("Email {0} already exist", email._id));
                }
                email.SMTPConfig = "Default";
                email.Save();
                return email;
            });
        }

        public JsonResult Delete(string[] ids)
        {
            return MvcResultInfo.Execute(() =>
            {
                foreach(var id in ids)
                {
                    var existingEmail = Email.Get<Email>(id);
                    if (existingEmail != null) existingEmail.Delete();
                }
                return "OK";
            });
        }
    }
}