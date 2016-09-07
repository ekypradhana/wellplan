using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using ECIS.Core;

namespace ECIS.Client.WEIS
{
    public class DataModeler
    {
        public DataModeler()
        {
            Regions = new List<string>();
            OperatingUnits = new List<string>();
            RigTypes = new List<string>();
            RigNames = new List<string>();
            AssetNames = new List<string>();
            ProjectNames = new List<string>();
            WellNames = new List<string>();
            ActivityTypes = new List<string>();
        }
        public List<string> Regions { get; set; }
        public List<string> OperatingUnits { get; set; }
        public List<string> RigTypes { get; set; }
        public List<string> RigNames { get; set; }
        public List<string> AssetNames { get; set; }
        public List<string> WellNames { get; set; }
        public List<string> ProjectNames { get; set; }
        public List<string> ActivityTypes { get; set; }
        private DateTime _from;
        public DateTime PeriodFrom
        {
            get { return _from; }
            set { _from = Tools.ToUTC(value); }
        }
        private DateTime _to;
        public DateTime PeriodTo
        {
            get { return _to; }
            set { _to = Tools.ToUTC(value); }
        }

        public List<object> Calc()
        {
            List<object> ret = new List<object>();
            return ret;
        }
    }
}