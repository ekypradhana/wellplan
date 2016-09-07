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
using System.IO;
using System.Web.Configuration;

using ECIS.Core.DataSerializer;
using System.Text;
using Aspose.Cells;

namespace ECIS.AppServer.Areas.Admin.Controllers
{
    [ECAuth(WEISRoles = "Administrators,APP-SUPPORTS")]
    public class AdminUsersController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult PopulateUsers(string Keyword, List<string> WellName, List<string> Activity, List<string> Role)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                Keyword = (Keyword == null ? "" : Keyword).Trim().ToLower();
                WellName = (WellName == null ? new List<string>() : WellName);
                Activity = (Activity == null ? new List<string>() : Activity);
                Role = (Role == null ? new List<string>() : Role);

                var roles = WEISRole.Populate<WEISRole>(Query.In("RoleName", new BsonArray(Role))).Select(d => d._id);

                IMongoQuery queryForPerson = null;
                var queriesForPerson = new List<IMongoQuery>();
                if (WellName.Count > 0)
                    queriesForPerson.Add(Query.In("WellName", new BsonArray(WellName)));
                if (Activity.Count > 0)
                    queriesForPerson.Add(Query.In("ActivityType", new BsonArray(Activity)));
                if (Role.Count > 0)
                    queriesForPerson.Add(Query.In("PersonInfos.RoleId", new BsonArray(roles)));
                if (queriesForPerson.Count() > 0)
                    queryForPerson = Query.And(queriesForPerson);

                var persons = WEISPerson.Populate<WEISPerson>(queryForPerson)
                    .SelectMany(e => e.PersonInfos, (e, f) => new { WEISPerson = e, WEISPersonInfo = f })
                    .Where(d =>
                    {
                        if (Role.Count > 0)
                            return roles.Contains(d.WEISPersonInfo.RoleId);

                        return true;
                    });

                IMongoQuery queryForUser = null;
                if (!Keyword.Equals(""))
                {
                    var queriesForUser = new List<IMongoQuery>();

                    queriesForUser.Add(Query.Matches("UserName",
                        new BsonRegularExpression(new Regex(Keyword.ToString().ToLower(), RegexOptions.IgnoreCase))));
                    queriesForUser.Add(Query.Matches("FullName",
                        new BsonRegularExpression(new Regex(Keyword.ToString().ToLower(), RegexOptions.IgnoreCase))));
                    queriesForUser.Add(Query.Matches("Email",
                        new BsonRegularExpression(new Regex(Keyword.ToString().ToLower(), RegexOptions.IgnoreCase))));

                    queryForUser = (queriesForUser.Count() > 0 ? Query.Or(queriesForUser) : null);
                }

                var fields = new string[] { "_id", "UserName", "FullName", "Enable", "Email", "ADUser", "ConfirmedAtUtc" };
                var data = DataHelper.Populate<ECIS.Identity.IdentityUser>("Users", fields: fields, q: queryForUser)
                    .Where(d =>
                    {
                        var clause1 = (WellName.Count > 0);
                        var clause2 = (Activity.Count > 0);
                        var clause3 = (Role.Count > 0);

                        if (clause1 || clause2 || clause3)
                        {
                            return (persons.Where(e => e.WEISPersonInfo.Email.Equals(d.Email)).Count() > 0);
                        }

                        return true;
                    });

                ri.Data = data;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            ri.CalcLapseTime();
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult PopulateRoleUsers()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                ri.Data = WEISPerson.Populate<WEISPerson>();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            ri.CalcLapseTime();
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult SaveNewRole(string FullName, string Email, string WellName, string ActivityType, string RoleName)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                string sequenceId = "";
                int phaseNo = 0;

                WEISRole role = WEISRole.Get<WEISRole>(Query.EQ("RoleName", RoleName));

                var activities = WellActivity.Populate<WellActivity>(Query.EQ("WellName", WellName));
                if (activities.Count() > 0)
                {
                    var activity = activities.FirstOrDefault();
                    sequenceId = activity.UARigSequenceId;

                    if (activity.Phases.Where(d => d.ActivityType.Equals(ActivityType)).Count() > 0)
                    {
                        WellActivityPhase phase = activity.Phases.FirstOrDefault(d => d.ActivityType.Equals(ActivityType));
                        phaseNo = phase.PhaseNo;
                    }
                }

                var queriesForPerson = new List<IMongoQuery>();
                queriesForPerson.Add(Query.EQ("SequenceId", sequenceId));
                queriesForPerson.Add(Query.EQ("PhaseNo", phaseNo));
                queriesForPerson.Add(Query.EQ("WellName", WellName));
                queriesForPerson.Add(Query.EQ("ActivityType", ActivityType));
                var persons = WEISPerson.Populate<WEISPerson>(Query.And(queriesForPerson.ToArray()));

                if (persons.Count > 0)
                {
                    WEISPerson oldPerson = persons.FirstOrDefault();
                    WEISPerson newPerson = new WEISPerson()
                    {
                        ActivityType = oldPerson.ActivityType,
                        PersonInfos = (oldPerson.PersonInfos == null ? new List<WEISPersonInfo>() : oldPerson.PersonInfos),
                        PhaseNo = oldPerson.PhaseNo,
                        SequenceId = oldPerson.SequenceId,
                        WellName = oldPerson.WellName
                    };
                    newPerson.PersonInfos.Add(new WEISPersonInfo()
                    {
                        Email = Email,
                        FullName = FullName,
                        RoleId = role._id.ToString()
                    });

                    oldPerson.Delete();
                    newPerson.Save();
                }
                else
                {
                    WEISPerson person = new WEISPerson()
                    {
                        ActivityType = ActivityType,
                        PhaseNo = phaseNo,
                        SequenceId = sequenceId,
                        WellName = WellName,
                        PersonInfos = new List<WEISPersonInfo>()
                    };

                    person.PersonInfos.Add(new WEISPersonInfo()
                    {
                        Email = Email,
                        FullName = FullName,
                        RoleId = role._id.ToString()
                    });
                    person.Save();
                }
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            ri.CalcLapseTime();
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult GetUser(string userid)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var userx = DataHelper.Populate("Users", Query.EQ("UserName", userid),
                    fields: new string[] { "_id", "UserName", "FullName", "Enable", "Email", "ADUser", "ConfirmedAtUtc" });
                var user = userx
                    .Select(d =>
                    {
                        var email = d.GetString("Email");

                        return new
                        {
                            _id = d.GetInt16("_id"),
                            UserName = d.GetString("UserName"),
                            FullName = d.GetString("FullName"),
                            Enable = d.GetBool("Enable"),
                            Email = email,
                            ADUser = d.GetBool("ADUser"),
                            ConfirmedAtUtc = d.GetDateTime("ConfirmedAtUtc"),
                            Roles = (WEISPerson.Populate<WEISPerson>(Query.EQ("PersonInfos.Email", email)) == null && WEISPerson.Populate<WEISPerson>(Query.EQ("PersonInfos.Email", email)).Count <= 0)
                            ? null : WEISPerson.Populate<WEISPerson>(Query.EQ("PersonInfos.Email", email))
                                .SelectMany(e => e.PersonInfos, (e, f) => new { WEISPerson = e, WEISPersonInfo = f })
                                .Select(e => new
                                {
                                    _id = e.WEISPerson._id,
                                    SequenceId = e.WEISPerson.SequenceId,
                                    WellName = e.WEISPerson.WellName,
                                    ActivityType = e.WEISPerson.ActivityType,
                                    PhaseNo = e.WEISPerson.PhaseNo,
                                    Email = e.WEISPersonInfo.Email,
                                    RoleId = e.WEISPersonInfo.RoleId,
                                    RoleName = WEISRole.Get<WEISRole>(e.WEISPersonInfo.RoleId).RoleName
                                })
                                .Where(e => e.Email.Equals(email))
                        };
                    });

                if (user.Count() == 0)
                    throw new Exception(String.Format("User {0} can't be found or access is denied", userid));
                ri.Data = user;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            ri.CalcLapseTime();
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult DeleteRole(string Email, String RoleId, string _id)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                WEISPerson person = WEISPerson.Get<WEISPerson>(Query.EQ("_id", _id));
                person.PersonInfos.RemoveAll(d => d.Email.Equals(Email) && d.RoleId.Equals(RoleId));
                person.Save();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            ri.CalcLapseTime();
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult Save(Dictionary<string, object> userinfo, List<Dictionary<string, object>> roles = null)
        {
            roles = (roles == null ? new List<Dictionary<string, object>>() : roles);

            ResultInfo ri = new ResultInfo();

            try
            {
                string username = userinfo["UserName"].ToString().ToLower();
                string email = userinfo["Email"].ToString().ToLower();
                string fullname = userinfo["FullName"].ToString();
                string password = userinfo.ContainsKey("Password") ? userinfo["Password"].ToString() : "";
                string mode = userinfo["Mode"].ToString();

                var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                var userMgr = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));
                if (mode.Equals("Insert"))
                {
                    var user = userMgr.FindByName(username.ToLower());
                    if (user != null)
                    {
                        ri.Message = "Username \"" + username + "\" is already registered.";
                        ri.Result = "NOK";
                        return MvcTools.ToJsonResult(ri);
                    }

                    if (password.Equals(""))
                    {
                        password = Tools.GenerateRandomString(8);
                    }

                    userMgr.Create(new IdentityUser
                    {
                        UserName = username.ToLower(),
                        Email = email.ToLower(),
                        FullName = userinfo["FullName"].ToString(),
                        Enable = userinfo["Enable"].ToString() == "True",
                        ADUser = userinfo["ADUser"].ToString() == "True",
                        ConfirmedAtUtc = Tools.ToUTC(DateTime.Now),
                        HasChangePassword = false
                    }, password);
                }
                else
                {
                    var user = userMgr.FindByName(username);
                    if (user != null)
                    {
                        user.Email = email.ToLower();
                        user.FullName = userinfo["FullName"].ToString();
                        user.Enable = userinfo["Enable"].ToString() == "True";
                        user.ADUser = userinfo["ADUser"].ToString() == "True";
                        if (password != "")
                        {
                            String hashpassword = userMgr.PasswordHasher.HashPassword(password);
                            user.PasswordHash = hashpassword;
                        }
                        userMgr.Update(user);
                    }
                }

                ri.Data = username;

                foreach (var role in roles)
                {
                    var NewFullName = userinfo["FullName"].ToString();
                    var NewEmail = userinfo["Email"].ToString().ToLower();
                    var _id = role["_id"].ToString();
                    var Email = role["Email"].ToString().ToLower();
                    var RoleId = role["RoleId"].ToString();
                    var NewRoleName = role["RoleName"].ToString();
                    var NewActivityType = role["ActivityType"].ToString();
                    var NewWellName = role["WellName"].ToString();
                    string NewSequenceId = "";
                    int NewPhaseNo = 0;

                    WEISRole newRole = WEISRole.Get<WEISRole>(Query.EQ("RoleName", NewRoleName));

                    var activities = WellActivity.Populate<WellActivity>(Query.EQ("WellName", NewWellName));
                    if (activities.Count() > 0)
                    {
                        var activity = activities.FirstOrDefault();
                        NewSequenceId = activity.UARigSequenceId;

                        if (activity.Phases.Where(d => d.ActivityType.Equals(NewActivityType)).Count() > 0)
                        {
                            WellActivityPhase phase = activity.Phases.FirstOrDefault(d => d.ActivityType.Equals(NewActivityType));
                            NewPhaseNo = phase.PhaseNo;
                        }
                    }


                    WEISPerson oldPerson = WEISPerson.Get<WEISPerson>(Query.EQ("_id", _id));
                    if (oldPerson.PersonInfos == null)
                    {
                        oldPerson.PersonInfos = new List<WEISPersonInfo>();
                    }
                    oldPerson.PersonInfos.RemoveAll(d => d.Email.Equals(Email) && d.RoleId.Equals(RoleId));
                    oldPerson.Delete();
                    oldPerson.Save();


                    var queriesForPerson = new List<IMongoQuery>();
                    queriesForPerson.Add(Query.EQ("SequenceId", NewSequenceId));
                    queriesForPerson.Add(Query.EQ("PhaseNo", NewPhaseNo));
                    queriesForPerson.Add(Query.EQ("WellName", NewWellName));
                    queriesForPerson.Add(Query.EQ("ActivityType", NewActivityType));
                    var persons = WEISPerson.Populate<WEISPerson>(Query.And(queriesForPerson.ToArray()));

                    if (persons.Count > 0)
                    {
                        var newPerson = persons.FirstOrDefault();
                        newPerson.PersonInfos = (newPerson.PersonInfos == null ? new List<WEISPersonInfo>() : newPerson.PersonInfos);
                        newPerson.PersonInfos.Add(new WEISPersonInfo()
                        {
                            FullName = NewFullName,
                            Email = NewEmail,
                            RoleId = newRole._id.ToString()
                        });

                        newPerson.Delete();
                        newPerson.Save();
                    }
                    else
                    {
                        WEISPerson newPerson = new WEISPerson()
                        {
                            ActivityType = NewActivityType,
                            PersonInfos = new List<WEISPersonInfo>(),
                            PhaseNo = NewPhaseNo,
                            SequenceId = NewSequenceId,
                            WellName = NewWellName
                        };
                        newPerson.PersonInfos.Add(new WEISPersonInfo()
                        {
                            FullName = NewFullName,
                            Email = NewEmail,
                            RoleId = newRole._id.ToString()
                        });

                        newPerson.Save();
                    }
                }

                Dictionary<string, string> variables = new Dictionary<string, string>();
                variables["Email"] = email.ToLower();
                variables["UserName"] = username.ToLower();
                variables["FullName"] = fullname;

                var emails = new string[] { email };

                if (mode.Equals("Insert"))
                {
                    variables["Password"] = password;
                    SendMail("UserInsert", emails, variables);
                }
                else if (password != "")
                {
                    variables["Password"] = password;
                    SendMail("UserChangePassword", emails, variables);
                }
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            ri.CalcLapseTime();
            return MvcTools.ToJsonResult(ri);
        }

        public void SendMail(string template, string[] emails, Dictionary<string, string> variables)
        {
            ECIS.Client.WEIS.Email.Send(template,
                toMails: emails,
                variables: variables,
                developerModeEmail: WebTools.LoginUser.Email);
        }


        [HttpPost]
        public JsonResult Upload(HttpPostedFileBase files, string ExportMethod)
        {

            ResultInfo ri = new ResultInfo();
            List<BsonDocument> result = new List<BsonDocument>();
            try
            {
                int fileSize = files.ContentLength;
                string fileName = files.FileName;
                string ContentType = files.ContentType;
                string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["UserUploadPath"]);
                bool exists = System.IO.Directory.Exists(folder);

                string fileNameReplace = DateTime.Now.ToString("yyyMMddhhmmss") + "-" + fileName;

                if (!exists)
                    System.IO.Directory.CreateDirectory(folder);

                string filepath = System.IO.Path.Combine(folder, fileNameReplace);
                files.SaveAs(filepath);

                ExtractionHelper eh = new ExtractionHelper();
                var datas = eh.Extract(filepath, 0);


                var dataNull = datas.Where(x => BsonHelper.GetString(x, "User_Name").Trim().Equals("") || BsonHelper.GetString(x, "Email").Trim().Equals(""));



                if (dataNull.Count() > 0)
                {
                    int counter = 0;
                    do
                    {
                        datas.Remove(dataNull.ToList()[counter]);
                    }
                    while (dataNull.Count() >= counter + 1);

                }
                List<string> wells = new List<string>();
                wells.Add("WellName");
                var wellx = DataHelper.Populate("WEISWellActivities", null, 0, 0, wells).ToList();

                List<string> wellCurrents = new List<string>();
                foreach (var w in wellx)
                    wellCurrents.Add(BsonHelper.GetString(w, "WellName"));

                result = Validation(datas.ToList(), wellCurrents);

                var isContainsError = result.Where(x => BsonHelper.GetString(x, "ErrorMessage").Trim().Length > 0);

                if (isContainsError.Count() > 0)
                {
                    ri.Data = DataHelper.ToDictionaryArray(isContainsError);
                    ri.Message = "Containst error on excel data";
                    ri.Result = "NOK";
                }
                else
                {
                    ri.Data = DataHelper.ToDictionaryArray(result);
                    ri.Message = "SUCCESS";
                    ri.Result = "OK";
                }
            }
            catch (Exception e)
            {
                ri.Result = "NOK";
                ri.Data = DataHelper.ToDictionaryArray(result);
                ri.Message = "ERROR";
                ri.PushException(e);
            }
            //ri.CalcLapseTime();
            return ri.ToJsonResult();
        }

        public void SaveUserTemplate(BsonDocument data)
        {
            string username = BsonHelper.GetString(data, "User_Name").ToLower();// userinfo["UserName"].ToString().ToLower();
            string email = BsonHelper.GetString(data, "Email").ToLower();//userinfo["Email"].ToString().ToLower();
            string fullname = BsonHelper.GetString(data, "Full_Name");// userinfo["FullName"].ToString();
            string password = BsonHelper.GetString(data, "Password");  //userinfo.ContainsKey("Password") ? userinfo["Password"].ToString() : "";
            bool enable = BsonHelper.GetString(data, "Enable").ToUpper() == "TRUE" ? true : false;  //userinfo.ContainsKey("Password") ? userinfo["Password"].ToString() : "";
            bool aduser = BsonHelper.GetString(data, "ADuser").ToUpper() == "TRUE" ? true : false;  //userinfo.ContainsKey("Password") ? userinfo["Password"].ToString() : "";
            int mode = BsonHelper.GetInt32(data, "Action");//userinfo["Mode"].ToString();

            var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
            var userMgr = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));
            if (mode == 0)
            {
                var user = userMgr.FindByName(username.ToLower());

                if (password.Equals(""))
                {
                    password = Tools.GenerateRandomString(8);
                }

                //if (password != "")
                //{
                //    String hashpassword = userMgr.PasswordHasher.HashPassword(password);
                //    password = hashpassword;
                //}
                //else
                //password = Tools.GenerateRandomString(8);
                userMgr.Create(new IdentityUser
                {
                    UserName = username.ToLower(),
                    Email = email.ToLower(),
                    FullName = fullname,
                    Enable = enable,
                    ADUser = aduser,
                    ConfirmedAtUtc = Tools.ToUTC(DateTime.Now),
                    HasChangePassword = true
                }, password);
            }
            else
            {
                // update
                var user = userMgr.FindByName(username.ToLower());
                if (user != null)
                {
                    user.Email = email.ToLower();
                    user.FullName = fullname; //;userinfo["FullName"].ToString();
                    user.Enable = enable;// userinfo["Enable"].ToString() == "True";
                    user.ADUser = aduser;// userinfo["ADUser"].ToString() == "True";
                    if (password != "")
                    {
                        String hashpassword = userMgr.PasswordHasher.HashPassword(password);
                        user.PasswordHash = hashpassword;
                    }
                    userMgr.Update(user);
                }
            }


        }

        public void SavePersonInfoTemplate(BsonDocument userinfo, List<BsonDocument> roles, string ExportMethod)
        {

            if (ExportMethod.Equals("Replace current access for existing user"))
            {
                // delete Existing WEIS Person user
                var weispersons = DataHelper.Populate<WEISPerson>("WEISPersons", Query.EQ("PersonInfos.Email", BsonHelper.GetString(userinfo, "Email").ToLower()));
                //var uw = BsonHelper.Unwind(weispersons.ToList(), "PersonInfos", "", new List<string> { "WellName", "SequenceId", "ActivityType", "PhaseNo" });
                //var personiin = uw.Where(x=>  BsonHelper.Get(x,"Email"))


                if (weispersons != null && weispersons.Count() > 0)
                {
                    foreach (var wp in weispersons)
                    {
                        List<WEISPersonInfo> piBackup = new List<WEISPersonInfo>();
                        piBackup = wp.PersonInfos;

                        piBackup.RemoveAll(x => x.Email.Equals(BsonHelper.GetString(userinfo, "Email").ToLower()));

                        wp.PersonInfos = new List<WEISPersonInfo>();
                        wp.Save();
                        wp.PersonInfos = piBackup;
                        wp.Save();
                    }
                }
            }


            foreach (var role in roles)
            {
                var NewFullName = BsonHelper.GetString(userinfo, "Full_Name");
                var NewEmail = BsonHelper.GetString(userinfo, "Email");
                var roleName = BsonHelper.GetString(role, "Role_Name");

                var activity = BsonHelper.GetString(role, "Activity");
                var wellName = BsonHelper.GetString(role, "Well_Name");
                string seqId = BsonHelper.GetString(role, "Sequence_ID").Trim();
                int phaseNo = 0;


                var activities = WellActivity.Populate<WellActivity>(Query.And(Query.EQ("WellName", wellName), Query.EQ("UARigSequenceId", seqId)));
                //DataHelper.Populate<WellActivity>("WEISWellActivities")
                //.SelectMany(d => d.Phases)
                //.GroupBy(d => d.ActivityType)
                //.Select(d => d.Key)
                //.OrderBy(d => (d.Equals("n/a") ? "" : d));



                if (activities.Count() > 0)
                {
                    var actv = activities.FirstOrDefault();
                    if (actv.Phases != null && actv.Phases.Count > 0)
                    {
                        if (activity.Trim().Equals("*"))
                        {
                            phaseNo = 0;
                        }
                        else
                        {
                            WellActivityPhase phase = actv.Phases.FirstOrDefault(d => d.ActivityType.Equals(activity));
                            phaseNo = phase.PhaseNo;
                        }
                    }

                }

                var weisperson = DataHelper.Populate<WEISPerson>("WEISPersons", Query.And(
                    Query.EQ("WellName", wellName), Query.EQ("SequenceId", seqId), Query.EQ("ActivityType", activity)
                    ));

                if (weisperson != null && weisperson.Count > 0)
                {
                    var wPerson = weisperson.FirstOrDefault();
                    var personinfos = wPerson.PersonInfos;
                    if (personinfos.Where(x => x.Email.Equals(NewEmail.ToLower())).Count() > 0)
                    {
                        // update WEISPersonInfo
                        wPerson.PersonInfos.Where(x => x.Email.Equals(NewEmail.ToLower())).FirstOrDefault().RoleId = WEISRole.Get<WEISRole>(Query.EQ("RoleName", roleName))._id.ToString();
                        wPerson.Save();
                    }
                    else
                    {
                        // insert new WEISPersonInfo
                        WEISPersonInfo wpi = new WEISPersonInfo();
                        wpi.RoleId = WEISRole.Get<WEISRole>(Query.EQ("RoleName", roleName))._id.ToString();
                        wpi.Email = NewEmail.ToLower();
                        wpi.FullName = NewFullName;
                        wPerson.PersonInfos.Add(wpi);
                        wPerson.Save();
                    }

                }
                else
                {
                    // new PersonInfo
                    WEISPerson wpn = new WEISPerson();
                    wpn.WellName = wellName;
                    wpn.SequenceId = seqId;
                    wpn.ActivityType = activity;
                    wpn.PhaseNo = phaseNo;
                    wpn.PersonInfos = new List<WEISPersonInfo>();
                    WEISPersonInfo wpi = new WEISPersonInfo();
                    wpi.RoleId = WEISRole.Get<WEISRole>(Query.EQ("RoleName", roleName))._id.ToString();
                    wpi.Email = NewEmail.ToLower();
                    wpi.FullName = NewFullName;
                    wpn.PersonInfos.Add(wpi);
                    wpn.Save();

                }

            }
        }

        public List<BsonDocument> GroupAndProcess(List<BsonDocument> datas, string ExportMethod)
        {



            List<BsonDocument> sendMailContents = new List<BsonDocument>();
            foreach (var data in datas.GroupBy(x => BsonHelper.GetString(x, "Email")))
            {
                var d = data.ToList();
                var newUserData = d.FirstOrDefault();

                var userExist = DataHelper.Populate("Users",
                            Query.And(
                                Query.EQ("UserName", BsonHelper.GetString(newUserData, "User_Name").ToLower()),
                                Query.EQ("Email", BsonHelper.GetString(newUserData, "Email").ToLower())));

                if (userExist != null && userExist.Count > 0)
                {
                    newUserData.Set("Action", 1);
                    if (ExportMethod.Equals("Replace current access for existing user"))
                    {
                        // delete user
                        DataHelper.Delete("Users", Query.EQ("Email", BsonHelper.GetString(newUserData, "Email").ToLower()));
                        newUserData.Set("Action", 0);
                    }
                }
                else
                    newUserData.Set("Action", 0);


                SaveUserTemplate(newUserData);

                List<BsonDocument> roles = new List<BsonDocument>();
                foreach (var role in d.ToList())
                {
                    BsonDocument rol = new BsonDocument();
                    rol.Set("Role_Name", BsonHelper.GetString(role, "Role_Name"));
                    rol.Set("Well_Name", BsonHelper.GetString(role, "Well_Name"));
                    rol.Set("Activity", BsonHelper.GetString(role, "Activity"));
                    rol.Set("Sequence_ID", BsonHelper.GetString(role, "Sequence_ID"));
                    roles.Add(rol);
                }


                SavePersonInfoTemplate(newUserData, roles, ExportMethod);

                // send email per group email

                var action = BsonHelper.GetInt32(newUserData, "Action");
                var email = BsonHelper.GetString(newUserData, "Email");
                var username = BsonHelper.GetString(newUserData, "User_Name");
                var fullname = BsonHelper.GetString(newUserData, "Full_Name");
                var password = BsonHelper.GetString(newUserData, "Password");

                Dictionary<string, string> variables = new Dictionary<string, string>();
                variables["Email"] = email;
                variables["UserName"] = username;
                variables["FullName"] = fullname;
                variables["Password"] = password;
                var emails = new string[] { email };


                StringBuilder sb = new StringBuilder();
                sb.Append("Added WEIS Person Role : \n");
                sb.Append("--------------------------- \n");
                foreach (var j in roles)
                {
                    sb.Append("Role Name : " + BsonHelper.GetString(j, "Role_Name") + "\n");
                    sb.Append("Well Name : " + BsonHelper.GetString(j, "Well_Name") + "\n");
                    sb.Append("Activity : " + BsonHelper.GetString(j, "Activity") + "\n");
                    sb.Append("Sequence ID : " + BsonHelper.GetString(j, "Sequence_ID") + "\n");
                    sb.Append("\n");

                }

                variables["WEISPersonData"] = sb.ToString();

                SendMail("ExportUser", emails, variables);

                BsonDocument doc = new BsonDocument();

                doc.Set("MailTemplate", "ExportUser");
                doc.Set("Emails", email);


                BsonArray bArray = new BsonArray();
                foreach (var term in variables)
                {
                    BsonDocument bdoc = new BsonDocument();
                    bdoc.Set(term.Key, term.Value);
                    bArray.Add(bdoc);
                }

                doc.Set("EmailVariables", bArray);

                sendMailContents.Add(doc);
            }
            return sendMailContents;
        }

        public JsonResult SendAsynEmail(List<Dictionary<string, object>> Datas, string ExportMethod)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                List<BsonDocument> result = new List<BsonDocument>();
                foreach (var data in Datas)
                {
                    BsonDocument doc = new BsonDocument();
                    doc.Set("User_Name", data["User_Name"].ToString());
                    doc.Set("Full_Name", data["Full_Name"].ToString());
                    doc.Set("ADuser", Convert.ToBoolean(data["ADuser"].ToString()));
                    doc.Set("Action", Convert.ToInt32(data["Action"].ToString()));
                    doc.Set("Activity", data["Activity"].ToString());
                    doc.Set("Confirm_Password", data["Confirm_Password"].ToString());
                    doc.Set("Email", data["Email"].ToString());
                    doc.Set("Enable", Convert.ToBoolean(data["Enable"].ToString()));
                    doc.Set("ErrorMessage", data["ErrorMessage"].ToString());
                    doc.Set("Password", data["Password"].ToString());
                    doc.Set("Role_Name", data["Role_Name"].ToString());
                    doc.Set("Sequence_ID", data["Sequence_ID"].ToString());
                    doc.Set("Well_Name", data["Well_Name"].ToString());
                    result.Add(doc);
                }


                var emailDataAsy = GroupAndProcess(result, ExportMethod);

                //WEISPerson person = WEISPerson.Get<WEISPerson>(Query.EQ("_id", _id));
                //person.PersonInfos.RemoveAll(d => d.Email.Equals(Email) && d.RoleId.Equals(RoleId));
                //person.Save();

                ri.Data = DataHelper.ToDictionaryArray(emailDataAsy);
                ri.Message = "SUCCESS";
                ri.Result = "OK";

            }
            catch (Exception e)
            {
                ri.Result = "NOK";
                ri.Data = "";
                ri.Message = "ERROR";
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public FileResult DonwloadTemplate()
        {
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "userexporttemplate.xlsx");

            return File(xlst, Tools.GetContentType(".xlsx"), "WEISUserTemplate.xlsx");
        }

        public FileResult Donwload()
        {
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "userexporttemplate.xlsx");

            var users = DataHelper.Populate("Users");
            var WeisPersons = DataHelper.Populate("WEISPersons");
            Workbook wb = new Workbook(xlst);
            var ws = wb.Worksheets[0];
            int startRow = 2;

            var roles = DataHelper.Populate("WEISRoles");

            foreach (var u in users)
            {
                //var ff = WeisPersons.SelectMany(x => x.PersonInfos).Where(x => x.Email.ToLower().Equals(BsonHelper.GetString(u, "Email").ToLower())).ToList();
                //var fxf = WeisPersons.Where(x =>
                //                        x.PersonInfos.Where(y => y.Email.ToLower().Equals(BsonHelper.GetString(u, "Email").ToLower()) == true).Count() > 0).ToList();
                var uw = BsonHelper.Unwind(WeisPersons.ToList(), "PersonInfos", "", new List<string> { "WellName", "SequenceId", "ActivityType", "PhaseNo" });

                var uwFilter = uw.Where(x => BsonHelper.GetString(u, "Email").ToLower().Equals(BsonHelper.GetString(x, "Email").ToLower()));



                foreach (var wp in uwFilter)
                {
                    ws.Cells["A" + startRow.ToString()].Value = BsonHelper.GetString(u, "UserName");
                    ws.Cells["B" + startRow.ToString()].Value = BsonHelper.GetString(u, "FullName");
                    ws.Cells["C" + startRow.ToString()].Value = BsonHelper.GetString(u, "Email").ToUpper();
                    ws.Cells["D" + startRow.ToString()].Value = BsonHelper.GetString(u, "ADUser").ToUpper();
                    ws.Cells["E" + startRow.ToString()].Value = BsonHelper.GetString(u, "Enable").ToUpper();

                    string RoleId = BsonHelper.GetString(wp, "RoleId");
                    string RoleName = "";
                    var adaRole = roles.Where(x => BsonHelper.GetString(x, "_id").Equals(RoleId));
                    if (adaRole != null && adaRole.Count() > 0)
                    {
                        RoleName = BsonHelper.GetString(adaRole.FirstOrDefault(), "RoleName");
                    }

                    ws.Cells["H" + startRow.ToString()].Value = RoleName;
                    ws.Cells["I" + startRow.ToString()].Value = BsonHelper.GetString(wp, "WellName").ToUpper();
                    ws.Cells["J" + startRow.ToString()].Value = BsonHelper.GetString(wp, "ActivityType").ToUpper();
                    ws.Cells["K" + startRow.ToString()].Value = BsonHelper.GetString(wp, "SequenceId").ToUpper();


                    startRow++;
                }

            }

            string folder = System.IO.Path.Combine(WebConfigurationManager.AppSettings["UserUploadPath"]);
            var filename = DateTime.Now.ToString("ddMMyyyy") + ("-UserImport.xlsx");
            var newFileName = folder + @"\" + filename;
            wb.Save(newFileName, Aspose.Cells.SaveFormat.Xlsx);

            return File(newFileName, Tools.GetContentType(".xlsx"), filename);
        }

        private List<BsonDocument> Validation(List<BsonDocument> datas, List<string> wellsCurrent)
        {

            var t = DataHelper.Populate<WellActivity>("WEISWellActivities");
            //.SelectMany(d => d.Phases)
            //.GroupBy(d => d.ActivityType)
            //.Select(d => d.Key)
            //.OrderBy(d => (d.Equals("n/a") ? "" : d));


            List<BsonDocument> list = new List<BsonDocument>();
            foreach (var y in datas)
            {
                list.Add(Validation(y, t, wellsCurrent));
            }

            return list;
        }

        private BsonDocument Validation(BsonDocument doc, List<WellActivity> WellActivity, List<string> wellsCurrent)
        {
            StringBuilder sb = new StringBuilder();
            string err = "";
            doc = PasswordEmptyReplace(doc);

            if (!UserNameVal(doc, out err))
                sb.Append(err);
            if (!ValPasswordMatch(doc, out err))
                sb.Append(err);
            if (!ValADUser(doc, out err))
                sb.Append(err);
            if (!ValEnable(doc, out err))
                sb.Append(err);
            if (!ValWellName(doc, wellsCurrent, out err))
                sb.Append(err);
            if (!ValActivity(doc, WellActivity, out err))
                sb.Append(err);
            if (!ValSequenceId(doc, out err))
                sb.Append(err);
            if (!ValRoleName(doc, out err))
                sb.Append(err);

            ValUserNameEmail(doc, out err);
            sb.Append(err);

            doc.Set("ErrorMessage", sb.ToString());

            return doc;
        }

        public JsonResult DeleteUser(string Email, string UserName)
        {
            try { 
                var checkEmail = DataHelper.Populate("Users", Query.EQ("Email", Email));

                if (checkEmail != null && checkEmail.ToList().Count == 1)
                {
                    //update WEIS Persons
                    var getPersons = WEISPerson.Populate<WEISPerson>(Query.EQ("PersonInfos.Email", Email));
                    if (getPersons != null && getPersons.Count > 0)
                    {
                        foreach (var x in getPersons)
                        {
                            var Persons = new WEISPerson();
                            var PersonInfos = new List<WEISPersonInfo>();
                            Persons = x;
                            if (x.PersonInfos != null && x.PersonInfos.Count > 0)
                            {
                                foreach (var i in x.PersonInfos)
                                {
                                    if (i.Email != Email)
                                    {
                                        PersonInfos.Add(i);
                                    }
                                }
                            }
                            Persons.PersonInfos = PersonInfos;
                            Persons.Save();
                        }
                    }
                }

                DataHelper.Delete("Users", Query.EQ("UserName",UserName));

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        #region Template Validation



        private BsonDocument ValUserNameEmail(BsonDocument data, out string ErrMsg)
        {
            string emailRegex = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";
            string email = BsonHelper.GetString(data, "Email");

            if (Regex.IsMatch(email.ToLower(), emailRegex))
            {
                var t = DataHelper.Populate("Users", Query.EQ("Email", BsonHelper.GetString(data, "Email")));
                if (t != null && t.Count > 0)
                {
                    ErrMsg = "";
                    data = data.Set("Action", 1); // update
                    return data;
                }
                else
                {
                    ErrMsg = "";
                    data = data.Set("Action", 0); // insert
                    return data;
                }
            }
            else
            {
                ErrMsg = "Your EMAIL address can contain only letters, numbers, periods (.), hyphens (-), and underscores (_). It can't contain special characters, accented letters, or letters outside the Latin alphabet ;";
                return data;
            }


        }

        private bool UserNameVal(BsonDocument data, out string ErrMsg)
        {
            string userNameRegex = @"^[a-z0-9_\-\.]";
            string userName = BsonHelper.GetString(data, "User_Name");
            if (userName.Trim().Equals(""))
            {
                ErrMsg = "Your USER NAME Cannot be empty;";
                return false;
            }
            else
            {
                if (Regex.IsMatch(userName.ToLower(), userNameRegex))
                {
                    ErrMsg = "";
                    return true;
                }
                else
                {
                    ErrMsg = "Your USER NAME can contain only letters, numbers, periods (.), hyphens (-), and underscores (_). It can't contain special characters, accented letters, or letters outside the Latin alphabet;";
                    return false;
                }
            }


        }

        public static string GetRandomPass(int take)
        {
            Random random = new Random();
            var datas = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(datas.Select(c => datas[random.Next(datas.Length)]).Take(take).ToArray());
        }

        private BsonDocument PasswordEmptyReplace(BsonDocument data)
        {
            string pass1 = BsonHelper.GetString(data, "Password");
            string pass2 = BsonHelper.GetString(data, "Confirm_Password");

            if (pass1.Trim() == "" && pass2.Trim() == "")
            {
                string pass = GetRandomPass(8);// BsonHelper.GetString(data, "User_Name").ToLower() + "123";
                data.Set("Password", pass);
                data.Set("Confirm_Password", pass);
                return data;
            }
            else
                return data;
        }

        private bool ValPasswordMatch(BsonDocument data, out string ErrMsg)
        {
            string pass1 = BsonHelper.GetString(data, "Password");
            string pass2 = BsonHelper.GetString(data, "Confirm_Password");

            if (pass1.Equals(pass2))
            {
                ErrMsg = "";
                return true;
            }
            else
            {
                ErrMsg = "Password do not match;";
                return false;
            }
        }

        private bool ValADUser(BsonDocument data, out string ErrMsg)
        {
            try
            {
                var ADuser = BsonHelper.GetString(data, "ADUser_(True/False)");
                data.Remove("ADUser_(True/False)");
                data.Set("ADuser", Convert.ToBoolean(ADuser));
                ErrMsg = "";
                return true;
            }
            catch
            {
                ErrMsg = "ADUser contains invalid value, change value to (True/False);";
                return false;
            }

        }

        private bool ValEnable(BsonDocument data, out string ErrMsg)
        {
            try
            {
                var Enable = BsonHelper.GetString(data, "Enable__(True/False)");
                data.Remove("Enable__(True/False)");
                data.Set("Enable", Convert.ToBoolean(Enable));

                ErrMsg = "";
                return true;
            }
            catch
            {
                ErrMsg = "Enable contains invalid value, change value to (True/False);";
                return false;
            }

        }

        private bool ValWellName(BsonDocument data, List<string> wellsCurrent, out string ErrMsg)
        {
            string WellName = BsonHelper.GetString(data, "Well_Name");
            if (WellName.Trim().ToUpper().Equals("*"))
            {
                ErrMsg = "";
                return true;
            }
            else
            {

                bool check = false;
                string WellCorrect = "";
                foreach (string h in wellsCurrent)
                {
                    if (h.ToUpper().Trim().Equals(WellName.Trim().ToUpper()))
                    {
                        check = true;
                        WellCorrect = h;
                    }

                }

                if (check)
                {
                    data.Set("Well_Name", WellCorrect);
                    ErrMsg = "";
                    return true;
                }
                else
                {
                    ErrMsg = "WELLNAME : " + WellName + ", in WEIS WellActivity data(s), try to change it to asterix * for all data, or (na) for none;";
                    return false;
                }
            }
        }

        private bool ValActivity(BsonDocument data, List<WellActivity> currentActvs, out string ErrMsg)
        {
            string actv = BsonHelper.GetString(data, "Activity");
            if (actv.Trim().ToUpper().Equals("*"))
            {
                ErrMsg = "";
                return true;
            }
            else
            {
                //var t = DataHelper.Populate("WEISActivities", Query.EQ("_id", actv));

                var t = currentActvs
                        .SelectMany(d => d.Phases)
                        .GroupBy(d => d.ActivityType)
                        .Select(d => d.Key)
                        .OrderBy(d => (d.Equals("n/a") ? "" : d));
                if (t != null && t.Count() > 0 && t.Contains(actv))
                {
                    ErrMsg = "";
                    return true;
                }
                else
                {
                    ErrMsg = "No ActivityType : " + actv + ", in WEIS WellActivity data(s), try to change it to asterix * for all data;";
                    return false;
                }
            }
        }

        private bool ValSequenceId(BsonDocument data, out string ErrMsg)
        {
            string seqId = BsonHelper.GetString(data, "Sequence_ID");
            if (seqId.Trim().ToUpper().Equals(""))
            {
                ErrMsg = "";
                return true;
            }
            else
            {
                var t = DataHelper.Populate("WEISWellActivities", Query.EQ("UARigSequenceId", seqId));
                if (t != null && t.Count() > 0)
                {
                    ErrMsg = "";
                    return true;
                }
                else
                {
                    ErrMsg = "SEQUENCE ID not exist in WEIS, you can leave it blank;";
                    return false;
                }
            }
        }

        private bool ValRoleName(BsonDocument data, out string ErrMsg)
        {
            string role = BsonHelper.GetString(data, "Role_Name");
            if (role.Trim().ToUpper().Equals("*") || role.Trim().Equals("(na)"))
            {
                ErrMsg = "";
                return true;
            }
            else
            {
                var t = DataHelper.Populate("WEISRoles", Query.EQ("RoleName", role));
                if (t != null && t.Count() > 0)
                {
                    ErrMsg = "";
                    return true;
                }
                else
                {
                    ErrMsg = "ROLE ID is not exist in WEIS, type asterix * for all data, or (na) for none;";
                    return false;
                }
            }
        }
        #endregion
    }
}