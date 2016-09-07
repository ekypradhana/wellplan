using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using ECIS.Core;
using ECIS.Identity;
using ECIS.Biz.Common;
using Microsoft.Owin.Security;
using Microsoft.AspNet.Identity;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

using ECIS.Core;
using ECIS.Client.WEIS;

namespace ECIS.AppServer
{
    public class WebTools
    {
        public static IdentityUser LoginUser{
            get
            {
                IdentityUser ret = new IdentityUser();
                if(HttpContext.Current.User.Identity.IsAuthenticated){
                    var userId = HttpContext.Current.User.Identity.GetUserId();
                    ret = DataHelper.Get<IdentityUser>("Users", Query.EQ("_id", new ObjectId(userId)));
                }
                return ret;
            }
        }

        public static List<String> GetAccessibleWells(){
            List<string> rets = new List<string>();
            var email = LoginUser.Email;
            List<string> wps = WEISPerson.Populate<WEISPerson>(Query.EQ("PersonInfos.Email", email))
                .GroupBy(d => d.WellName).Select(d => d.Key).ToList();
            if (wps.Contains("*"))
                wps = WellInfo.Populate<WellInfo>().Select(d => d._id.ToString()).ToList();
            rets.AddRange(wps);
            return rets;
        }

        public static List<String> GetAccessibleActivity()
        {
            List<string> rets = new List<string>();
            var email = LoginUser.Email;
            List<string> wps = WEISPerson.Populate<WEISPerson>(Query.EQ("PersonInfos.Email", email))
                .GroupBy(d => d.ActivityType).Select(d => d.Key).ToList();
            if (wps.Contains("*"))
                wps = WellActivity.Populate<WellActivity>().Select(d => d._id.ToString()).ToList();
            rets.AddRange(wps);
            return rets;
        }

        public static List<String> GetWellActivities(string[] roleIds=null)
        {
            List<string> rets = new List<string>();
            var email = LoginUser.Email;
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("PersonInfos.Email", email));
            if (roleIds != null) qs.Add(Query.In("PersonInfos.RoleId",new BsonArray(roleIds)));
            var q = Query.And(qs);
            List<WEISPerson> wps = WEISPerson.Populate<WEISPerson>(q)
                .Select(d => new WEISPerson { ActivityType = d.ActivityType, WellName = d.WellName })
                .ToList();
            List<string> wellNames = WellInfo.Populate<WellInfo>().GroupBy(d => d._id.ToString()).Select(d => d.Key).ToList();
            List<string> activityTypes = WellActivity.Populate<WellActivity>().GroupBy(d => d._id.ToString())
                .Select(d => d.Key).ToList();
            foreach (var wp in wps)
            {
               rets.Add(String.Format("{0}|{1}",wp.WellName,wp.ActivityType));
            }
            return rets;
        }

        public static IMongoQuery GetWellActivitiesQuery(string WellNameField, string ActivityTypeField, string[] roleIds=null)
        {
            List<string> was = GetWellActivities(roleIds);
            List<IMongoQuery> qs = new List<IMongoQuery>();
            IMongoQuery q = Query.Null;
            foreach (var wa in was)
            {
                string[] waParts = wa.Split(new char[]{'|'});
                string wellName = waParts[0];
                string activityType = waParts[1];
                List<IMongoQuery> q1 = new List<IMongoQuery>();
                if (WellNameField != "")
                {
                    if(wellName.Equals("*")==false)
                        q1.Add(Query.EQ(WellNameField, wellName));
                    else
                        q1.Add(Query.GT(WellNameField, "0"));
                }
                if (ActivityTypeField != "")
                {
                    if(activityType.Equals("*")==false)
                        q1.Add(Query.EQ(ActivityTypeField, activityType));
                    else
                        q1.Add(Query.GT(ActivityTypeField, "0"));
                }
                qs.Add(Query.And(q1));
            }
            if (qs.Count > 0) q = Query.Or(qs);
            return q;
        }
    }
}