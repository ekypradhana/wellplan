using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;

namespace ECIS.Client.WEIS
{
    public class ReleaseNote : ECIS.Core.ECISModel
    {
        // Title
        public string Type { get; set; }
        public string Module { get; set; }
        public string EmailReceivers { get; set; }
        public string DependsOn { get; set; }

        public string VersionStr { get; set; }
        public List<string> DocRef { get; set; }

        public string Description { get; set; }
        public string Notes { get; set; }
        public string CreatedBy { get; set; }
        public override string TableName
        {
            get { return "WEISReleaseNotes"; }
        }
        public override MongoDB.Bson.BsonDocument PreSave(MongoDB.Bson.BsonDocument doc, string[] references = null)
        {
            _id = String.Format("U{0}", LastUpdate.ToString("yyMMddhhmmss"));

            if (references != null && references[0].ToLower().Equals("increasever"))
                VersionStr = BuildVersionId(1);
            else if (references != null && references[0].ToLower().Equals("increasesubver"))
                VersionStr = BuildVersionId(2);
            else
                VersionStr = BuildVersionId(3);

            return this.ToBsonDocument();
        }


        public static string BuildVersionId(int ver)
        {
            var latest = ReleaseNote.Get<ReleaseNote>(null, SortBy.Descending("VersionStr"));
            if (latest == null)
                return "1.0.0";
            else
            {
                string version = latest.VersionStr;

                if (ver == 1) // first segmen 
                {
                    string first = version.Split('.')[0];
                    return string.Format("{0}.{1}.{2}", (Convert.ToInt32(first) + 1).ToString(), "0", "0");
                }
                else if (ver == 2) // second segmen
                {
                    string second = version.Split('.')[1];
                    return string.Format("{0}.{1}.{2}", version.Split('.')[0], (Convert.ToInt32(second) + 1).ToString(), "0");
                }
                else
                {
                    string third = version.Split('.')[2];
                    return string.Format("{0}.{1}.{2}", version.Split('.')[0], version.Split('.')[1], (Convert.ToInt32(third) + 1).ToString());
                }
                //string s = "JV-" + string.Format("{0:000000}", no);
                //latest.VersionStr
            }

        }
    }

}
