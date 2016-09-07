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
    #region Long Lead
    public class LongLead : ECISModel
    {
        public override string TableName
        {
            get { return "WEISLongLeadItems"; }
        }
        public List<FYLongLead> Details { get; set; }
        // FILL TITLE With Drilling or Completion or Abandonment

        public LongLead()
        {
            Details = new List<FYLongLead>();
        }

        public static FYLongLead GetLongLead(string title, int year)
        {
            FYLongLead result = null;
            string tit = "";
            if (title.ToUpper().Contains("DRILLING"))
                tit = "DRILLING";
            else if (title.ToUpper().Contains("COMPLETION"))
                tit = "COMPLETION";
            else if (title.ToUpper().Contains("ABANDONMENT"))
                tit = "ABANDONMENT";
            else
                tit = "";

            if (!tit.Equals(""))
            {
                var t = LongLead.Populate<LongLead>(Query.EQ("Title", tit));
                if (t != null && t.Count() > 0)
                {
                    if (t.FirstOrDefault().Details != null && t.FirstOrDefault().Details.Count() > 0)
                    {
                        var fylead = t.FirstOrDefault().Details.Where(x => x.Year == year);
                        if (fylead != null && fylead.Count() > 0)
                        {
                            result = fylead.FirstOrDefault();
                        }
                    }
                }
            }

            return result;
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            _id = String.Format("T{0}", this.Title);

            return this.ToBsonDocument();
        }
    }

    public class FYLongLead
    {
        public string Title { get; set; }
        public int Year { get; set; }
        public double TangibleValue { get; set; }
        public string TangibleValueType { get; set; }

        public double MonthRequiredValue { get; set; }
        public string MonthRequiredValueType { get; set; }
    }

    #endregion
}
