using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;

using ECIS.Core;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using Microsoft.AspNet.Identity;
using ECIS.Identity;
using ECIS.AppServer.Models;
using ECIS.Client.WEIS;
using Microsoft.Owin.Security;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using ECIS.AppServer.Areas.WebMenu.Models;

namespace ECIS.AppServer.Areas.Issue.Controllers
{
    public class AppIssueController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.IsAdministrator = isRole("ADMINISTRATORS");
            ViewBag.IsAppSupport = isRole("APP-SUPPORTS");

            return View();
        }

        public bool isRole(string RoleId)
        {
            RoleId = RoleId.ToLower();

            try
            {
                var username = WebTools.LoginUser.UserName;
                var user = DataHelper.Get("Users", Query.EQ("UserName", username));
                var queries = new List<IMongoQuery>();
                queries.Add(Query.EQ("PersonInfos.Email", user.GetString("Email")));
                queries.Add(Query.Matches("PersonInfos.RoleId", new BsonRegularExpression(new Regex(RoleId.ToLower(), RegexOptions.IgnoreCase))));
                var persons = WEISPerson.Populate<WEISPerson>(Query.And(queries.ToArray()));

                if (persons.Count > 0)
                {
                    return persons.FirstOrDefault().PersonInfos
                        .Where(d => d.Email.Equals(user.GetString("Email")) && d.RoleId.ToLower().Equals(RoleId))
                        .Count() > 0;
                }

                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public JsonResult Populate(string Keyword = null, 
            List<string> Module = null, 
            List<string> Priority = null, 
            List<string> Status = null, 
            List<string> Type = null)
        {
            Keyword = (Keyword == null ? "" : Keyword).ToString().ToLower();
            Module = (Module == null ? new List<string>() : Module);
            Priority = (Priority == null ? new List<string>() : Priority);
            Status = (Status == null ? new List<string>() : Status);
            Type = (Type == null ? new List<string>() : Type);

            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                IMongoQuery query = null;
                var queries = new List<IMongoQuery>();

                var queriesSearch = new List<IMongoQuery>();
                if (!Keyword.Equals(""))
                {
                    queriesSearch.Add(Query.Matches("Title", new BsonRegularExpression(new Regex(Keyword, RegexOptions.IgnoreCase))));
                    queriesSearch.Add(Query.Matches("UserId", new BsonRegularExpression(new Regex(Keyword, RegexOptions.IgnoreCase))));
                }

                if (queriesSearch.Count > 0)
                    queries.Add(Query.Or(queriesSearch.ToArray()));

                if (Module.Count > 0)
                    queries.Add(Query.In("Module", new BsonArray(Module.ToArray())));

                if (Priority.Count > 0)
                    queries.Add(Query.In("Priority", new BsonArray(Priority.ToArray())));

                if (Status.Count > 0)
                    queries.Add(Query.In("Status", new BsonArray(Status.ToArray())));

                if (Type.Count > 0)
                    queries.Add(Query.In("Type", new BsonArray(Type.ToArray())));

                if (queries.Count > 0)
                    query = Query.And(queries.ToArray());

                return AppIssue.Populate<AppIssue>(query);
            });
        }

        public JsonResult Save(string Title, string Type, string Status, string Module, string Description, string Priority)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                AppIssue issue = new AppIssue();
                issue.Title = Title;
                issue.Type = Type;
                issue.Status = Status;
                issue.Module = Module;
                issue.LogDescription = Description;
                issue.Priority = Priority;
                issue.UserId = WebTools.LoginUser.UserName;
                issue.Save();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult GetIssue(int id)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var issue = AppIssue.Get<AppIssue>(Query.EQ("_id", id));

                issue.Logs = issue.Logs.OrderByDescending(d => d.LastUpdate).ToList<AppIssueLog>();

                ri.Data = new 
                {
                    Issue = issue,
                    IsRequestorOfIssue = issue.UserId.Equals(WebTools.LoginUser.UserName)
                };
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult AddUpdate(AppIssue newIssue)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var issue = AppIssue.Get<AppIssue>(Query.EQ("_id", Convert.ToInt32(newIssue._id)));

                bool isAppSupport = isRole("APP-SUPPORTS");
                bool isAdministrator = isRole("ADMINISTRATORS");
                bool isRequestorOfIssue = issue.UserId.Equals(WebTools.LoginUser.UserName);
                bool isStatusToClosed = newIssue.Status.Equals("Closed");

                if (!isAppSupport)
                {
                    if (isStatusToClosed)
                    {
                        ri.Result = "NOK";
                        ri.Message = "Only user with role \"App Support\" can change status to closed!";
                        return MvcTools.ToJsonResult(ri);
                    }
                    if (!(isAdministrator || isRequestorOfIssue))
                    {
                        ri.Result = "NOK";
                        ri.Message = "Only Administrator or Requestor of Issue are allowed to update this issue";
                        return MvcTools.ToJsonResult(ri);
                    }
                }

                issue.LogDescription = newIssue.LogDescription;
                issue.Module = newIssue.Module;
                issue.Type = newIssue.Type;
                issue.Status = newIssue.Status;
                issue.Priority = newIssue.Priority;
                issue.Title = newIssue.Title;
                issue.Save();

                issue.Logs = issue.Logs.OrderByDescending(d => d.LastUpdate).ToList<AppIssueLog>();

                ri.Data = issue;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult AddComment(AppIssue newIssue, string Comment = "")
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var issue = AppIssue.Get<AppIssue>(Query.EQ("_id", Convert.ToInt32(newIssue._id)));
                if (!Comment.Trim().Equals(""))
                {
                    issue.Logs.Add(new AppIssueLog()
                    {
                        LastUpdate = DateTime.Now,
                        UserId = WebTools.LoginUser.UserName,
                        Comment = Comment
                    });
                }
                issue.Save();
                issue.Logs = issue.Logs.OrderByDescending(d => d.LastUpdate).ToList<AppIssueLog>();

                var usernames = issue.Logs.GroupBy(d => d.UserId).Select(d => d.Key).ToList<string>();
                var mails = new List<string>();
                if (usernames.Count > 0) 
                {
                    mails = DataHelper.Populate("Users", Query.In("UserName", new BsonArray(usernames)))
                        .Select(d => d.GetString("Email"))
                        .ToList<string>();
                }

                var variables = new Dictionary<string, string>();
                variables["IssueNo"] = Convert.ToString(issue._id);
                variables["IssueName"] = issue.Title;
                variables["UserName"] = WebTools.LoginUser.UserName;
                variables["Comment"] = Comment;

                Email.Send("IssueComment",
                    toMails: new string[] { WebTools.LoginUser.Email },
                    ccMails: mails.ToArray(),
                    variables: variables,
                    developerModeEmail: WebTools.LoginUser.Email);

                ri.Data = issue;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }
	}
}