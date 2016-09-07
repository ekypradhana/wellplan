using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Builders;

namespace ECIS.Client.WEIS
{
    public class ActivityMaster : ECISModel
    {
        public override string TableName
        {
            get { return "WEISActivities"; }
        }

        [BsonIgnoreIfDefault]
        [BsonIgnoreIfNull]
        public string ActivityCategory { get; set; }

        public string EDMActivityId { get; set; }
    }
}
