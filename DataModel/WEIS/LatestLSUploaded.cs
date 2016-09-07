using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ECIS.Client.WEIS
{
    public class LatestLSUploaded
    {
        public string Rig_Name { get; set; }
        public string Well_Name { get; set; }
        public string Activity_Type { get; set; }

        public DateRange LS { get; set; }

        public LatestLSUploaded()
        {
            LS = new DateRange();
        }
    }
}
