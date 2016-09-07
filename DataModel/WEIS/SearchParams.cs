using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ECIS.Client.WEIS
{
    public class SearchParams
    {
        public List<string> Currency { get; set; }
        public List<string> MoneyType { get; set; }
        public string SSorTG { get; set; }
        public List<string> regions { get; set; }
        public List<string> operatingUnits { get; set; }
        public List<string> rigTypes { get; set; }
        public List<string> rigNames { get; set; }
        public List<string> projectNames { get; set; }
        public List<string> wellNames { get; set; }
        public List<string> performanceUnits { get; set; }
        public List<string> exType { get; set; }
        public List<string> activities { get; set; }
        public List<string> activitiesCategory { get; set; }
        public List<string> performanceMetrics { get; set; }
        public string PIPType { get; set; }
        public List<string> AlreadyAssignTo { get; set; }
        public List<string> OPs { get; set; }
        public string opRelation { get; set; }
    }
}