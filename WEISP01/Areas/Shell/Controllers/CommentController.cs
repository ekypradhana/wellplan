using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
using ECIS.Biz.Common;
using System.Text;
using System.Text.RegularExpressions;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class CommentController : Controller
    {
        //
        // GET: /Shell/Comment/
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ShowGap()
        {
            return RedirectToAction("GetCommentsGap", "Comment");
        }
        public ActionResult GetCommentsGap()
        {
            return View();
        }
        public JsonResult CheckUserRole()
        {
            var appIssue = new ECIS.AppServer.Areas.Issue.Controllers.AppIssueController();
            bool isAdministrator = appIssue.isRole("ADMINISTRATORS");
            bool isAppSupport = appIssue.isRole("APP-SUPPORTS");
            bool isAdmin = _isAdmin();
            bool isReadOnly = _isReadOnly();

            return Json(new { isAdministrator, isAppSupport, isAdmin, isReadOnly }, JsonRequestBehavior.AllowGet);
        }

        private static bool _isAdmin()
        {
            var Email = WebTools.LoginUser.Email;
            List<string> Roles = WEISPerson.GetRolesByEmail(Email);
            var admin = Roles.Where(x => x.ToUpper() == "ADMINISTRATORS" || x.ToLower().Contains("app-support"));
            bool isAdmin = false;
            if (admin.Count() > 0)
            {
                isAdmin = true;
            }
            return isAdmin;
        }

        private static bool _isReadOnly()
        {
            var Email = WebTools.LoginUser.Email;
            List<string> Roles = WEISPerson.GetRolesByEmail(Email);
            var ro = Roles.Where(x => x.ToUpper() == "RO-ALL");
            bool isRO = false;
            if (ro.Count() > 0)
            {
                isRO = true;
            }
            return isRO;
        }
        public JsonResult GetFilterValueForComments()
        {
            var WellNames = WebTools.GetAccessibleWells().ToArray();
            var Activities = MasterActivity.Populate<MasterActivity>().Select(d => d._id.ToString()).ToList().ToArray();
            var PIPThemes = WellPIPThemes.Populate<WellPIPThemes>()
                        .Where(x => x.Name != null)
                        .Select(x => x.Name).ToArray();
            var PIPClass = WellPIPClassifications.Populate<WellPIPClassifications>()
                        .Where(x => x.Name != null)
                        .Select(x => x.Name).ToArray();
            return Json(new { WellNames,Activities,PIPThemes,PIPClass }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult PopulateUsers()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
               
                var fields = new string[] { "UserName", "FullName" };
                var data = DataHelper.Populate<ECIS.Identity.IdentityUser>("Users", fields: fields).Select(x => new
                {
                    UserName = x.ToBsonDocument().GetString("UserName"),
                    FullName = x.ToBsonDocument().GetString("FullName")
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

        public JsonResult GetRespectivePersons(string WellName, string ActivityType)
        {
            var get = GetRespectivePersons("", new List<string>() { WellName }, new List<string>() { ActivityType }, null);
            return Json(new { Data = get.ToArray() }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCommentsGapData(List<string> PIPClass, List<string> PIPThemes, List<string> WellNames, List<string> Activities, List<string> Users, DateTime? Date = null, string Keywords = "", int take = 0, int skip = 0, List<Dictionary<string, string>> sorts = null)
        {
            var WAUs = WellActivityUpdate.Populate<WellActivityUpdate>();
            var WAUIds = WAUs.Select(x => x._id.ToString()).ToList();
            List<string> WAUElementIds = WAUs.SelectMany(d => d.Elements, (d, e) => new
                                        {
                                            ElementId = e.ElementId,
                                            PIPClass = e.Classification,
                                            PIPTheme = e.Theme
                                        })
                                        .Select(x => x.ElementId.ToString()).Distinct().ToList();

            var qsWAU = new List<IMongoQuery>();
            qsWAU.Add(Query.EQ("ReferenceType", "WeeklyReport"));
            qsWAU.Add(Query.NotIn("Reference1", new BsonArray(WAUIds)));
            qsWAU.Add(Query.NotIn("Reference1", new BsonArray(WAUElementIds)));
            var qWAU = Query.And(qsWAU);

            var PIPs = WellPIP.Populate<WellPIP>();
            var PIPIds = PIPs.Select(x => x._id.ToString()).ToList();
            List<string> PIPElementIds = WAUs.SelectMany(d => d.Elements, (d, e) => new
                                            {
                                                ElementId = e.ElementId,
                                                PIPClass = e.Classification,
                                                PIPTheme = e.Theme
                                            })
                                        .Select(x => x.ElementId.ToString()).Distinct().ToList();

            var qsPIP = new List<IMongoQuery>();
            qsPIP.Add(Query.EQ("ReferenceType", "WellPIP"));
            qsPIP.Add(Query.NotIn("Reference1", new BsonArray(PIPIds)));
            qsPIP.Add(Query.NotIn("Reference1", new BsonArray(PIPElementIds)));
            var qPIP = Query.And(qsPIP);

            var qsComments = new List<IMongoQuery>();
            qsComments.Add(qPIP);
            qsComments.Add(qWAU);
            var qComments = Query.Or(qsComments);

            string sortDir = (Convert.ToString((object)Request.Params["sorts[" + "0" + "][dir]"]) == "asc" ? "1" : "-1");
            string sortField = Convert.ToString((object)Request.Params["sorts[" + "0" + "][field]"]);

            SortByBuilder sort = null;
            var field = "";
            if (sortField != "")
            {
                if (sortField.ToLower() != "_id")
                {
                    if (sortField.ToLower() == "message")
                    {
                        field = "Comment";
                    }
                    else if (sortField.ToLower() == "at")
                    {
                        field = "LastUpdate";
                    }
                    else if (sortField.ToLower() == "name")
                    {
                        field = "User";
                    }
                    else if (sortField.ToLower() == "email")
                    {
                        field = "Email";
                    }
                    if (sortDir == "1")
                        sort = SortBy.Ascending(field);
                    else
                        sort = SortBy.Descending(field);
                }
            }

            var GetComment = WEISComment.Populate<WEISComment>(Query.And(qComments), take, skip, sort)
                                    .Select(x => new
                                    {
                                        id = x._id,
                                        name = x.User,
                                        email = x.Email,
                                        message = x.Comment,
                                        at = x.LastUpdate,
                                        readed = false,
                                        reference1 = x.Reference1,
                                        referencetype = x.ReferenceType
                                    });
            var Total = WEISComment.Populate<WEISComment>(Query.And(qComments), fields: new string[] { "_id" }).Count;
            return Json(new { Data = new { Data = GetComment.OrderByDescending(x => x.at).ToArray(), Total = Total } }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAllComments(List<string> PIPClass, List<string> PIPThemes, List<string> WellNames, List<string> Activities, List<string> Users,DateTime? Date = null, string Keywords="", int take=0,int skip=0, List<Dictionary<string, string>> sorts = null)
        {
            var QueryforPIP = WebTools.GetWellActivitiesQuery("WellName", "ActivityType");
            var qs = new List<IMongoQuery>();
            qs.Add(QueryforPIP);
            if (WellNames != null && WellNames.Count > 0)
            {
                qs.Add(Query.In("WellName", new BsonArray(WellNames)));
            }
            if (Activities != null && Activities.Count > 0)
            {
                qs.Add(Query.In("ActivityType", new BsonArray(Activities)));
            }
            var qPIP = Query.And(qs);
            //List<string> PIPIds = DataHelper.Populate("WEISWellPIPs", qPIP)
                            //.Select(x=>x.GetValue("_id").ToString()).ToList();

            List<WellPIP> WellPIPs = WellPIP.Populate<WellPIP>(qPIP);
            List<string> PIPIds = WellPIPs.Select(x => x._id.ToString()).ToList();
            List<string> PIPElementIds = WellPIPs.SelectMany(d => d.Elements, (d, e) => new
                                        {
                                            ElementId = e.ElementId,
                                            PIPClass = e.Classification,
                                            PIPTheme = e.Theme
                                        })
                                        .Where(x => {
                                            if (PIPClass != null && PIPClass.Count > 0)
                                            {
                                                return (PIPClass.Where(y => y.Equals(x.PIPClass)).Count() > 0);
                                            }
                                            else
                                            {
                                                return true;
                                            }
                                        })
                                        .Where(x => {
                                            if (PIPThemes != null && PIPThemes.Count > 0)
                                            {
                                                return (PIPThemes.Where(y => y.Equals(x.PIPTheme)).Count() > 0);
                                            }
                                            else
                                            {
                                                return true;
                                            }
                                        })
                                        .Select(x => x.ElementId.ToString())
                                        .ToList();

            var QueryforWAU = WebTools.GetWellActivitiesQuery("WellName", "Phase.ActivityType");
            var qs2 = new List<IMongoQuery>();
            qs2.Add(QueryforWAU);
            if (WellNames != null && WellNames.Count > 0)
            {
                qs2.Add(Query.In("WellName", new BsonArray(WellNames.ToArray())));
            }
            if (Activities != null && Activities.Count > 0)
            {
                qs2.Add(Query.In("Phase.ActivityType", new BsonArray(Activities.ToArray())));
            }
            //List<string> WAUIds = DataHelper.Populate("WEISWellActivityUpdates", Query.And(qs2))
            //                .Select(x => x.GetValue("_id").ToString()).ToList();

            List<WellActivityUpdate> WAUs = WellActivityUpdate.Populate<WellActivityUpdate>(Query.And(qs2));
            List<string> WAUIds = WAUs.Select(x => x._id.ToString()).ToList();
            List<string> PIPElementIds2 = WAUs.SelectMany(d => d.Elements, (d, e) => new
                                        {
                                            ElementId = e.ElementId,
                                            PIPClass = e.Classification,
                                            PIPTheme = e.Theme
                                        })
                                        .Where(x =>
                                        {
                                            if (PIPClass != null && PIPClass.Count > 0)
                                            {
                                                return (PIPClass.Where(y => y.Equals(x.PIPClass)).Count() > 0);
                                            }
                                            else
                                            {
                                                return true;
                                            }
                                        })
                                        .Where(x =>
                                        {
                                            if (PIPThemes != null && PIPThemes.Count > 0)
                                            {
                                                return (PIPThemes.Where(y => y.Equals(x.PIPTheme)).Count() > 0);
                                            }
                                            else
                                            {
                                                return true;
                                            }
                                        })
                                        .Select(x => x.ElementId.ToString())
                                        .ToList();

            var AllIds = PIPIds;
            AllIds.AddRange(WAUIds);
            AllIds = AllIds.Distinct().ToList();
            var AllElementIds = PIPElementIds;
            AllElementIds.AddRange(PIPElementIds2);
            AllElementIds = AllElementIds.Distinct().ToList();

            List<Int32> ReadCommentsIds = new List<Int32>();

            WEISCommentRead ReadComments = WEISCommentRead.Get<WEISCommentRead>(Query.EQ("User", WebTools.LoginUser.UserName));
            if (ReadComments != null)
            {
                ReadCommentsIds = ReadComments.CommentsRead.Select(x => x.CommentId).ToList();
            }
            var qs3 = new List<IMongoQuery>();
            qs3.Add(Query.In("Reference1", new BsonArray(AllIds.ToArray())));
            qs3.Add(Query.In("Reference2", new BsonArray(AllElementIds.ToArray())));
            if (Date != null)
            {
                var dt = (DateTime)Date;
                var nextDay = dt.AddDays(1);
                qs3.Add(Query.And(Query.GTE("LastUpdate", dt), Query.LTE("LastUpdate", nextDay)));
            }
            if (Users != null && Users.Count > 0)
            {
                qs3.Add(Query.In("User", new BsonArray(Users.ToArray())));
            }
            if (Keywords != "")
            {
                qs3.Add(Query.Matches("Comment",
                        new BsonRegularExpression(new Regex(Keywords.ToString().ToLower(), RegexOptions.IgnoreCase))));
            }
            var q = Query.And(qs3);

            string sortDir = (Convert.ToString((object)Request.Params["sorts[" + "0" + "][dir]"]) == "asc" ? "1" : "-1");
            string sortField = Convert.ToString((object)Request.Params["sorts[" + "0" + "][field]"]);

            SortByBuilder sort = null;
            var field = "";
            if (sortField != "")
            {
                if (sortField.ToLower() != "_id")
                {
                    if (sortField.ToLower() == "message")
                    {
                        field = "Comment";
                    }
                    else if (sortField.ToLower() == "at")
                    {
                        field = "LastUpdate";
                    }
                    else if (sortField.ToLower() == "name")
                    {
                        field = "User";
                    }
                    else if (sortField.ToLower() == "email")
                    {
                        field = "Email";
                    }
                    if (sortDir == "1")
                        sort = SortBy.Ascending(field);
                    else
                        sort = SortBy.Descending(field);
                }
            }

            var GetComment = WEISComment.Populate<WEISComment>(Query.And(qs3),take,skip,sort)
                                    .Select(x => new
                                    {
                                        id = x._id,
                                        name = x.User,
                                        email = x.Email,
                                        message = x.Comment,
                                        at = x.LastUpdate,
                                        readed = ReadCommentsIds.Where(d => d.Equals(x._id)).Count() > 0,
                                        well = GetWellAndActivity(x.ReferenceType,x.Reference1)
                                    });
            var Total = WEISComment.Populate<WEISComment>(Query.And(qs3), fields: new string[] { "_id" }).Count;
            return Json(new { Data = new { Data = GetComment.OrderByDescending(x => x.at).ToArray(),Total = Total } }, JsonRequestBehavior.AllowGet);
        }



        public static string GetWellAndActivity(string ReferenceType,string ReferenceId)
        {
            if (ReferenceType.ToLower() == "wellpip")
            {
                var getpip = WellPIP.Get<WellPIP>(Query.EQ("_id", ReferenceId));
                return getpip.WellName + " -- " + getpip.ActivityType;
            }
            else
            {
                var getwau = WellActivityUpdate.Get<WellActivityUpdate>(Query.EQ("_id", ReferenceId));
                return getwau.WellName + " -- " + getwau.Phase.ActivityType;
            }
        }

        public void setReadComment(List<WEISComment> wc, bool setReadAll = false)
        {

            if (wc != null && wc.Count > 0)
            {
                var user = WebTools.LoginUser.UserName;
                var OldCommentRead = WEISCommentRead.Get<WEISCommentRead>(Query.EQ("User", user));
                List<CommentsRead> newCommentRead = new List<CommentsRead>();
                WEISCommentRead newComm = new WEISCommentRead();
                if (OldCommentRead != null)
                {
                    newCommentRead = OldCommentRead.CommentsRead;
                    newComm = OldCommentRead;
                }
                else
                {
                    newComm.User = user;
                }
                List<Int32> CommentReadIds = wc.Select(x => Convert.ToInt32(x._id)).ToList();

                if (!setReadAll)
                {
                    foreach (var a in CommentReadIds)
                    {
                        if (newCommentRead.Where(x => x.CommentId.Equals(a)).Count() == 0)
                        {
                            newCommentRead.Add(new CommentsRead { CommentId = a, ReadTime = DateTime.UtcNow });
                        }
                    }
                }

                newComm.CommentsRead = newCommentRead;
                newComm.Save();
            }
        }

        public JsonResult GetComment(int CommentId=0, string ReferenceId = "",string ReferenceType = "", int ElementId = 0)
        {

            string WellName = "";
            string ActivityType = "";
            string Idea = "";
            if (CommentId != 0)
            {
                var wc = new WEISComment();
                wc = WEISComment.Get<WEISComment>(Query.EQ("_id", CommentId));

                WellName = "";
                ActivityType = "";
                Idea = "";
                ReferenceType = wc.ReferenceType;
                ElementId = Convert.ToInt32(wc.Reference2);
                ReferenceId = wc.Reference1;
            }
            
            
            List<WEISComment> datas = new List<WEISComment>();

            if (ReferenceType == "WellPIP")
            {
                //get from WellPIP
                var PIPId = ReferenceId;
                datas = GetCommentFromPIP(PIPId, ElementId);

                var getPIP = WellPIP.Get<WellPIP>(Query.EQ("_id", PIPId));
                WellName = getPIP.WellName;
                ActivityType = getPIP.ActivityType;
                Idea = getPIP.Elements.Where(x => x.ElementId.Equals(ElementId)).FirstOrDefault().Title;
            }
            else
            {
                //get from WAU
                var WAUId = ReferenceId;
                datas = GetCommentFromWAU(WAUId, ElementId.ToString());

                var getWAU = WellActivityUpdate.Get<WellActivityUpdate>(Query.EQ("_id", WAUId));
                WellName = getWAU.WellName;
                ActivityType = getWAU.Phase.ActivityType;
                Idea = getWAU.Elements.Where(x => x.ElementId.Equals(ElementId)).FirstOrDefault().Title;
            }

            new CommentController().setReadComment(datas);

            return Json(new { Data = datas.OrderByDescending(x => x.LastUpdate),WellName,ActivityType,Idea,ReferenceId,ReferenceType,ElementId }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCommentGapDetail(int CommentId = 0, string ReferenceId = "", string ReferenceType = "", int ElementId = 0)
        {

            string WellName = "";
            string ActivityType = "";
            string Idea = "";
            if (CommentId != 0)
            {
                var wc = new WEISComment();
                wc = WEISComment.Get<WEISComment>(Query.EQ("_id", CommentId));

                WellName = "";
                ActivityType = "";
                Idea = "";
                ReferenceType = wc.ReferenceType;
                ElementId = Convert.ToInt32(wc.Reference2);
                ReferenceId = wc.Reference1;
            }


            List<WEISComment> datas = new List<WEISComment>();

            if (ReferenceType == "WellPIP")
            {
                //get from WellPIP
                var PIPId = ReferenceId;
                datas = GetCommentFromPIP(PIPId, ElementId);

                var getPIP = WellPIP.Get<WellPIP>(Query.EQ("_id", PIPId));
                WellName = getPIP.WellName;
                ActivityType = getPIP.ActivityType;
                Idea = getPIP.Elements.Where(x => x.ElementId.Equals(ElementId)).FirstOrDefault().Title;
            }
            else
            {
                //get from WAU
                var WAUId = ReferenceId;
                datas = GetCommentFromWAU(WAUId, ElementId.ToString());

                var getWAU = WellActivityUpdate.Get<WellActivityUpdate>(Query.EQ("_id", WAUId));
                WellName = getWAU.WellName;
                ActivityType = getWAU.Phase.ActivityType;
                Idea = getWAU.Elements.Where(x => x.ElementId.Equals(ElementId)).FirstOrDefault().Title;
            }

            new CommentController().setReadComment(datas);

            return Json(new { Data = datas.OrderByDescending(x => x.LastUpdate), WellName, ActivityType, Idea, ReferenceId, ReferenceType, ElementId }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult SaveComment(string ReferenceId, string ReferenceType, int ParentId, int ElementId, string Comment)
        {
            try
            {
                WEISComment wc = new WEISComment();
                wc.User = WebTools.LoginUser.UserName;
                wc.Email = WebTools.LoginUser.Email;
                wc.FullName = WebTools.LoginUser.UserName;
                wc.ReferenceType = ReferenceType;
                wc.Reference1 = ReferenceId;
                wc.Reference2 = ElementId.ToString();
                wc.Comment = Comment;
                wc.ParentId = ParentId;

                wc.Save();
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public static List<WEISComment> GetCommentFromPIP(string PIPId, int ElementId)
        {
            var q = Query.Null;
            List<IMongoQuery> qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("ReferenceType", "WellPIP"));
            qs.Add(Query.EQ("Reference1", PIPId));
            qs.Add(Query.EQ("Reference2", ElementId.ToString()));
            q = Query.And(qs);
            List<WEISComment> wc = WEISComment.Populate<WEISComment>(q);

            var pip = WellPIP.Get<WellPIP>(Query.EQ("_id", PIPId));
            if (pip != null)
            {
                var wellName = pip.WellName;
                var qwau = Query.And(Query.EQ("WellName", wellName), Query.EQ("Phase.ActivityType", pip.ActivityType));
                var sort = new SortByBuilder().Descending("LastUpdate");
                var au = WellActivityUpdate.Populate<WellActivityUpdate>(q: qwau, sort: sort);
                if (au == null) au = new List<WellActivityUpdate>();

                if (au.Count > 0)
                {
                    var auids = au.Select(d => d._id.ToString());
                    var qpip = new List<IMongoQuery>();
                    qpip.Add(Query.EQ("ReferenceType", "WeeklyReport"));
                    qpip.Add(Query.In("Reference1", new BsonArray(auids)));
                    qpip.Add(Query.EQ("Reference2", ElementId.ToString()));

                    var pc = WEISComment.Populate<WEISComment>(Query.And(qpip));

                    if (pc != null)
                    {
                        if (pc.Count > 0)
                        {
                            wc = wc.Concat(pc).ToList();
                        }
                    }
                }
            }

            return wc;
        }

        public static List<WEISComment> GetCommentFromWAU(string ActivityUpdateId, string ElementID)
        {
            var au = WellActivityUpdate.Get<WellActivityUpdate>(Query.EQ("_id", ActivityUpdateId));
            List<object> WAUIds = new List<object>();
            if (au != null)
            {
                var qGetWAUs = Query.And(new IMongoQuery[] { 
                    Query.EQ("WellName", au.WellName),
                    Query.EQ("SequenceId", au.SequenceId),
                    Query.EQ("Phase.ActivityType", au.Phase.ActivityType),
                }.ToList());
                var ListWAUs = WellActivityUpdate.Populate<WellActivityUpdate>(qGetWAUs);
                if (ListWAUs != null && ListWAUs.Count > 0)
                {
                    foreach (var wau in ListWAUs)
                    {
                        WAUIds.Add(wau._id);
                    }
                }
            }

            var query = Query.And(new IMongoQuery[] { 
                Query.EQ("ReferenceType", "WeeklyReport"),
                Query.In("Reference1", new BsonArray(WAUIds)),
                Query.EQ("Reference2", ElementID),
            }.ToList());
            var data = WEISComment.Populate<WEISComment>(query);


            if (au != null)
            {
                var wellName = au.WellName;
                var qpip = Query.And(Query.EQ("WellName", wellName), Query.EQ("ActivityType", au.Phase.ActivityType));
                var sort = new SortByBuilder().Descending("LastUpdate");
                var pip = WellPIP.Populate<WellPIP>(q: qpip, sort: sort);
                if (pip == null) pip = new List<WellPIP>();

                if (pip.Count > 0)
                {
                    var pipids = pip.Select(d => d._id.ToString());
                    var qau = new List<IMongoQuery>();
                    qau.Add(Query.EQ("ReferenceType", "WellPIP"));
                    qau.Add(Query.In("Reference1", new BsonArray(pipids)));
                    qau.Add(Query.EQ("Reference2", ElementID.ToString()));
                    var qaus = Query.And(qau);
                    var ac = WEISComment.Populate<WEISComment>(Query.And(qau));

                    if (ac != null)
                    {
                        if (ac.Count > 0)
                        {
                            data = data.Concat(ac).ToList();
                        }
                    }
                }
            }

            return data;
        }

        public void DeleteComment(string ReferenceType, string ReferenceId, int ElementId)
        {
            if (ReferenceType.ToLower() == "wellpip")
            {
                // delete all respective comment
                List<WEISComment> AllComments = GetCommentFromPIP(ReferenceId, ElementId);
                List<Int32> CommentIds = AllComments.Select(x => Convert.ToInt32(x._id)).ToList();
                DataHelper.Delete("WEISComments", Query.In("_id",new BsonArray(CommentIds.ToArray())));
            }
            else
            {
                // delete only comment matched with WAUID and ElementID
                var qs = new List<IMongoQuery>();
                qs.Add(Query.EQ("ReferenceType","WeeklyReport"));
                qs.Add(Query.EQ("Reference1", ReferenceId));
                qs.Add(Query.EQ("Reference2", ElementId.ToString()));
                DataHelper.Delete("WEISComments", Query.And(qs));
            }
        }

        public static List<ECIS.Identity.IdentityUser> GetRespectivePersons(string Keyword, List<string> WellName, List<string> Activity, List<string> Role)
        {
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


                return data.ToList();
            }
            catch (Exception e)
            {
                return new List<ECIS.Identity.IdentityUser>();
            }
        }

	}
}