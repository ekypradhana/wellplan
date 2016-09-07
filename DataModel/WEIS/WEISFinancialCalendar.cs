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
    public class WEISFinancialCalendar : ECISModel
    {
        public override string TableName
        {
            get { return "WEISFinancialCalendar"; }
        }

        public DateTime MonthYear { set; get; }
        public string Status { set; get; }
    }
}
