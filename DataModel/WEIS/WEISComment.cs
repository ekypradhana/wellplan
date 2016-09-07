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
    public class WEISComment : ECISModel
    {
        public override string TableName
        {
            get { return "WEISComments"; }
        }

        //public string Title { get; set; }
        public string Comment { get; set; }
        public string User { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string ReferenceType { get; set; }
        public string Reference1 { get; set; }
        public string Reference2 { get; set; }
        public int ParentId { get; set; }
    }
    public class WEISCommentRead : ECISModel
    {
        public override string TableName
        {
            get { return "WEISCommentsRead"; }
        }

        public string User { get; set; }
        public List<CommentsRead> CommentsRead { get; set; }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            _id = User;
            return this.ToBsonDocument();
        }

    }

    public class CommentsRead
    {
        public Int32 CommentId { get; set; }
        public DateTime ReadTime { get; set; }
    }
}
