using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ECIS.Client.WEIS
{
    public class MetadataConfig
    {
        //public List<string> Fields { get; set; }
        public List<FieldsData> Fields { get; set; }
        public List<string> Rules { get; set; }
    }

    public class FieldsData
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
