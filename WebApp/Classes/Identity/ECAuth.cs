using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;

using System.Web.Mvc;
using System.Web;
using System.Web.Security;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;

namespace ECIS.Core
{
    public class ECAuthAttribute : AuthorizeAttribute
    {
        public string OrUsers { get; set; }
        public string WEISRoles { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (base.AuthorizeCore(httpContext))
            {
                if (OrUsers == null && WEISRoles == null) return true;

                var roles = WEISRoles==null ? new List<string>() :
                    WEISRoles.Split(',').Select(d => d.Trim()).ToList();
                var id = httpContext.User.Identity.Name.ToLower();
                var q = MongoDB.Driver.Builders.Query.EQ("UserName",id);
                var user = DataHelper.Get<ECIS.Identity.IdentityUser>("Users", q);
                if (user != null)
                {
                    var userRoles = user.GetRoles().Select(d=>d.GetString("RoleId").ToLower()).ToList();
                    foreach (var role in roles)
                    {
                        if(userRoles.Contains(role.ToLower()))
                            return true;
                    }
                }

                var users = OrUsers == null ? new List<string>() :
                    OrUsers.Split(new char[] { ',' }).Select(d => d.ToLower()).ToList();
                foreach (var u in users)
                {
                    if (id.Equals(u)) return true;
                }
            }
            return false;
        }
        
        //protected  bool AuthorizeCore_(HttpContextBase httpContext)
        //{
        //    if (base.AuthorizeCore(httpContext))
        //    {
        //        /* Return true immediately if the authorization is not 
        //            locked down to any particular AD group */
        //        if (String.IsNullOrEmpty(Groups))
        //            return true;

        //        // Get the AD groups
        //        var groups = Groups.Split(',').Select(d => d.Trim()).ToList();

        //        // Verify that the user is in the given AD group (if any)
        //        var context = new PrincipalContext(
        //                              ContextType.Domain,
        //                              ConfigurationManager.AppSettings["ContextName"].ToString());

        //        var userPrincipal = UserPrincipal.FindByIdentity(
        //                               context,
        //                               IdentityType.SamAccountName,
        //                               httpContext.User.Identity.Name);

        //        foreach (var group in groups)
        //            if (userPrincipal.IsMemberOf(context,
        //                 IdentityType.Name,
        //                 group))
        //                return true;

        //        var id = httpContext.User.Identity.Name.ToLower();
        //        var users = OrUsers.Split(new char[] { ',' }).Select(d => d.ToLower());
        //        foreach (var u in users)
        //        {
        //            if (id.Equals(u)) return true;
        //        }
        //    }
        //    return false;
        //}

        protected override void HandleUnauthorizedRequest(
        AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                var result = new ViewResult();
                result.ViewName = "NotAuthorized";
                result.MasterName = "_common_v1";
                filterContext.Result = result;
            }
            else
                base.HandleUnauthorizedRequest(filterContext);
        }
    }

    public class ADUser
    {
        public static bool IsMemberOf(string userid, string group)
        {
            HttpContext ctx = HttpContext.Current;

            // Verify that the user is in the given AD group (if any)
            var context = new PrincipalContext(
                                    ContextType.Domain,
                                    ConfigurationManager.AppSettings["ContextName"].ToString());

            var userPrincipal = UserPrincipal.FindByIdentity(
                                    context,
                                    IdentityType.SamAccountName,
                                    userid);

            if (userPrincipal.IsMemberOf(context,
                    IdentityType.Name,
                    group))
                return true;
            else
                return false;
        }
    }
}