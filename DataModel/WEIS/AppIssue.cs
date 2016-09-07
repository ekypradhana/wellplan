using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using ECIS.Biz.Common;

namespace ECIS.Client.WEIS
{
    public class AppIssue : ECISModel
    {
        public AppIssue()
        {
            Status = string.Empty;
            Type = string.Empty;
            UserId = string.Empty;
            LogDescription = string.Empty;
            Module = string.Empty;
            Priority = string.Empty;
            Logs = new List<AppIssueLog>();
        }

        public override string TableName
        {
            get { return "AppIssue"; }
        }

        public string Status { set; get; }
        public string Type { set; get; }
        public string UserId { set; get; }
        public string LogDescription { set; get; }
        public string Module { set; get; }
        public string Priority { set; get; }
        public List<AppIssueLog> Logs { set; get; }

        public override BsonDocument PostSave(BsonDocument doc, string[] references=null)
        {
            if (Logs == null)
                Logs = new List<AppIssueLog>();

            return base.PostSave(doc);
        }
    }

    public class AppIssueLog : ECISModel
    {
        public override string TableName
        {
            get { return "AppIssueLog"; }
        }
        public string UserId { set; get; }
        public string Comment { set; get; }
    }
}
