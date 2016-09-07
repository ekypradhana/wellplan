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
    public class RigRatesNew : ECISModel
    {
        public override string TableName
        {
            get { return "WEISRigRates2"; }
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            _id = String.Format("T{0}", this.Title);

            return this.ToBsonDocument();
        }

        public List<RigRateValue> Values { get; set; }

        public RigRatesNew()
        {
            Values = new List<RigRateValue>();
        }

        public Dictionary<int, double> Calculate(string Type)
        {
            var holder = new Dictionary<int, double[]>();
            var periods = (Values ?? new List<RigRateValue>()).Where(d => Type.Equals(d.Type)).ToList();

            for (var i = 0; i < periods.Count(); i++) {
                var each = periods[i];
                var period = (each.Period ?? new DateRange());

                for (var j = period.Start.Year; j <= period.Finish.Year; j++)
                {
                    var startYear = new DateTime(j, 1, 1); var originalStartYear = startYear;
                    var finishYear = new DateTime(j, 12, 31); var originalFinishYear = finishYear;
                    var totalDaysOfYear = (originalFinishYear - originalStartYear).Days + 1;

                    if (j == period.Start.Year)
                        startYear = period.Start;

                    if (j == period.Finish.Year)
                        finishYear = period.Finish;

                    var totalDaysActual = (finishYear - startYear).Days + 1;

                    var value = totalDaysActual * each.Value / totalDaysOfYear;

                    if (holder.ContainsKey(j))
                    {
                        var temp = holder[j].ToList();
                        temp.Add(value);
                        holder[j] = temp.ToArray();
                    }
                    else
                    {
                        holder[j] = new double[] { value };
                    }
                }
            }

            var result = new Dictionary<int, double>();
            foreach (KeyValuePair<int, double[]> each in holder)
            {
                result[each.Key] = each.Value.Sum();
            }

            return result;
        }
    }

    public class RigRateValue
    {
        // IDLE || ACTIVE
        public string Type { get; set; }
        public DateRange Period { get; set; }
        public double Value { get; set; }
        public string ValueType { get; set; }
    }
}
