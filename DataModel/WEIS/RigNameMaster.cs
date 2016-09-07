using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using ECIS.Core;
using ECIS.Biz.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ECIS.Client.WEIS
{
    [Obsolete("This class is obsolete; use class MasterRigName instead, inside of Master.cs")]
    public class RigNameMaster
    {
        public RigNameMaster()
        {
            _id = string.Empty;

        }
        public string _id { get; set; } 

    }
}