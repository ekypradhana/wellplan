using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Builders;

namespace ECIS.Client.WEIS
{
    public class WellActivityDocument : ECISModel
    {
        public override string TableName
        {
            get { return "WEISWellActivityDocuments"; }
        }
        //public ObjectId _id { get; set; }
        public string ActivityUpdateId { get; set; }
        //public string Title { get; set; }
        public List<DocumentItem> Files { get; set; }
        public string ActivityType { get; set; }
        public string ActivityDesc { get; set; }
        public string WellName { get; set; }
        public string Type { get; set; }
        public string Link { get; set; }
        public DateTime UpdateVersion { get; set; }
        public double FileSize { get; set; }
    }

    public class DocumentItem
    {
        public int FileNo { get; set; }
        public string FileName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ContentType { get; set; }
        public string Type { get; set; }
        public double FileSize { get; set; }
        public string Link { get; set; }
    }

    public class DocumentManagementHeader
    {
        public string FileName { get; set; }
        public int FileNo { get; set; }
        public string FileTitle { get; set; }
        public string FileDescription { get; set; }
        public string ContentType { get; set; }
        public string ActivityUpdateId { get; set; }
        public string ActivityType { get; set; }
        public string ActivityDesc { get; set; }
        public string WellName { get; set; }
        public DateTime UpdateVersion { get; set; }
        public string Type { get; set; }
        public string Link { get; set; }
        public double FileSize { get; set; }


    }
}
