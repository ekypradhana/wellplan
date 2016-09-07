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
    [BsonIgnoreExtraElements]
    public class WellInfo : ECISModel
    {
        #region override
        public override string TableName
        {
            get { return "WEISWellNames"; }
        }
        #endregion


        private List<string> _LoBs;
        public List<string> LoBs
        {
            get
            {
                if (_LoBs == null) _LoBs = new List<string>();
                return _LoBs;
            }
            set { _LoBs = value; }
        }

        public string Company { get; set; }
        public string Project { get; set; }
        public string Site { get; set; }
        public string WellType { get; set; }
        public string EDMWellId { get; set; }
        public string EDMWellName { get; set; }
        public string Contractor { get; set; }
        public string WorkUnit { get; set; }
        public string Superintendent { get; set; }
        public string WellEngineer { get; set; }
        public string EventType { get; set; }
        public string Objective { get; set; }
        public bool IsVirtualWell { get; set; }
        public int EstimatedDays { get; set; }
        private DateTime _FirstOilDate;
        public DateTime FirstOilDate
        {
            get { return _FirstOilDate; }
            set { _FirstOilDate = Tools.ToUTC(value); }
        }
        private DateTime _EventStartDate;
        public DateTime EventStartDate
        {
            get { return _EventStartDate; }
            set { _EventStartDate = Tools.ToUTC(value); }
        }
        private DateTime _OriginalSpudDate;
        public DateTime OriginalSpudDate
        {
            get { return _OriginalSpudDate; }
            set { _OriginalSpudDate = Tools.ToUTC(value); }
        }

        private List<Person> _weeklyReportPICs;
        public List<Person> WeeklyReportPICs
        {
            get {
                if (_weeklyReportPICs == null) _weeklyReportPICs = new List<Person>();
                return _weeklyReportPICs; }
            set { _weeklyReportPICs = value; }
        }

        private List<Person> _alertPICs;
        public List<Person> AlertPICs
        {
            get
            {
                if (_alertPICs == null) _alertPICs = new List<Person>();
                return _alertPICs;
            }
            set { _alertPICs = value; }
        }
      
    }

    public class WellMatchParam
    {
        public string WellId { set; get; }
        public string WellName { set; get; }
        public string ShellWellName { set; get; }
        public DateTime FirstOilDate { set; get; }
    }

    public class WellJunction : ECISModel
    {
        public override string TableName { get { return "WEISWellJunction"; } }
        public string OUR_WELL_ID { set; get; }
        public string SHELL_WELL_ID { set; get; }
        public string SHELL_WELL_NAME { set; get; }
    }
}