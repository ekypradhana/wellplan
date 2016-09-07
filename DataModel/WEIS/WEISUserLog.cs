using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization.Attributes;

namespace ECIS.Client.WEIS
{
    public class WEISUserLog : ECISModel
    {
        public override string TableName
        {
            get { return "WEISUserLog"; }
        }

        public string UserName { set; get; }
        public string Email { set; get; }
        public DateTime Time { set; get; }
        public int Duration { set; get; }
        public string LogType { set; get; }

        public override BsonDocument PostSave(BsonDocument doc, string[] references = null)
        {
            var IgnoreBefore = references != null && references[0].ToLower().Equals("ignorebefore");
            if (!IgnoreBefore)
            {
                // Update before
                var before = GetBefore();
                if (before != null && before.LogType == "Login")
                {
                    var TimeBefore = before.Time;
                    var Duration = Convert.ToInt32(Time.Subtract(TimeBefore).TotalSeconds);
                    before.Duration = Duration > 3600 ? 3600 : Duration;
                    before.Save(references: new string[] { "IgnoreBefore" });
                }
            }
            return this.ToBsonDocument();
        }

        public WEISUserLog GetBefore()
        {
            //-- get latest update
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("UserName", UserName));
            qs.Add(Query.LT("Time", Time));
            //qs.Add(Query.EQ("LogType", UserLogType.Login.ToString()));
            var latest = WEISUserLog.Get<WEISUserLog>(Query.And(qs), sort: SortBy.Descending(new string[] { "Time" }));
            return latest;
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            //DateTime createDate = Tools.ToUTC(DateTime.Now, true);
            DateTime createDate = DateTime.Now;
            var UpdateVersion = new DateTime(createDate.Year, createDate.Month, createDate.Day, createDate.Hour, createDate.Minute, createDate.Second, createDate.Millisecond);
            _id = String.Format("T{0}", createDate.ToString("yyyyMMddHHmmssfff"));
            return this.ToBsonDocument();
        }

    }
    public enum UserLogType
    {
        Login,
        Logout
    }

    public class UserLogData
    {
        public string Key { get; set; }
        public double Duration { get; set; }
        public double NumberOfLogin { get; set; }
        public double NumberOfUniqueUser { get; set; }
        public string WellName { get; set; }
        public string RigName { get; set; }
        public string UserName { get; set; }
        public DateTime Time { get; set; }
    }


    public class WEISUserActivityLog : ECISModel
    {
        public override string TableName
        {
            get { return "WEISUserActivityLog"; }
        }

        public string UserName { set; get; }
        public string Email { set; get; }
        public List<DailyLog> Logs { get; set; }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            _id = String.Format("U{0}D{1}", UserName.Replace(" ", ""), LastUpdate.ToString("yyyyMMddhh"));

            var temp = WEISUserActivityLog.Populate<WEISUserActivityLog>(Query.EQ("_id", _id.ToString()));
            if (temp == null || temp.Count() <= 0)
            {
                return this.ToBsonDocument();
            }
            else
            {
                foreach (var t in this.Logs)
                {
                    temp.FirstOrDefault().Logs.Add(t);
                }
                return temp.FirstOrDefault().ToBsonDocument();
            }
        }

        public static List<WEISUserActivityLog> GetUserActivityByDate(DateTime fromDate, DateTime toDate, string userName = "")
        {
            List<WEISUserActivityLog> logs = new List<WEISUserActivityLog>();
            DateTime fdt = new DateTime(fromDate.Year, fromDate.Month, fromDate.Day);
            DateTime tdt = new DateTime(toDate.Year, toDate.Month, toDate.Day);

            if (!userName.Trim().Equals(""))
                logs = WEISUserActivityLog.Populate<WEISUserActivityLog>(Query.And(Query.EQ("UserName", userName), Query.And(Query.GTE("LastUpdate", fdt), Query.LT("LastUpdate", tdt.AddDays(1)))));
            else
                logs = WEISUserActivityLog.Populate<WEISUserActivityLog>(Query.And(Query.GTE("LastUpdate", fdt), Query.LT("LastUpdate", tdt.AddDays(1))));

            return logs;
        }

        public static void SaveUserActivityLog(string userName, string email, LogType Type, string tableName, string ModuleName, BsonDocument data1, BsonDocument data2)
        {
            WEISUserActivityLog lg = new WEISUserActivityLog();
            lg.UserName = "eaciit";
            lg.Email = "eaciit@eaciit.com";
            lg.Logs = new List<DailyLog>();

            DailyLog dl = new DailyLog();
            dl.Module = ModuleName.ToString();
            dl.Desc1 = data1.ToJson();
            if(data2 != null )
                dl.Desc2 = data2.ToJson();
            dl.Collection = tableName;
            dl.LogTime = DateTime.Now;
            dl.Type = Type.ToString();
            lg.Logs.Add(dl);

            lg.Save();
        }

    }
    public enum LogType
    {
        Insert,
        Update,
        Delete,
        UpdateOnDelete,
        UpdateOnReopen,
        UpdateStatusToSubmitted,
        UpdateStatusToDistributed,
        Others,
        AddNewPhase,
        UpdatePhase,
        NewWellActivity,
        DeletePhase,
        DeleteActivity
    }
    public enum Module
    {
        WeeklyReport,
        PIPConfiguration,
        DataBrowser
    }
    public class DailyLog
    {
        public string Module { get; set; }
        public DateTime LogTime { get; set; }
        public string Type { set; get; }
        public string Collection { set; get; }
        public string Desc1 { set; get; }
        public string Desc2 { set; get; }
    }
}
