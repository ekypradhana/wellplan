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
using ECIS.Biz.Common;


namespace ECIS.Client.WEIS
{
    public class WEISFreezedOP : ECISModel
    {
        public override string TableName
        {
            get { return "WEISFreezedOPs"; }
        }

        public bool isFreezedOP(string OP)
        {
            var check = WEISFreezedOP.Get<WEISFreezedOP>(OP);
            if (check != null)
            {
                return true;
            }

            return false;
        }
    }
}
