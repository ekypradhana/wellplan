using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECIS.Client.WEIS.Output
{
    public class DataBrowser
    {
        public int _id              {get;set;}
        public string Region           {get;set;}
        public string OperatingUnit    {get;set;}
        public string UARigSequenceId  {get;set;}
        public string UARigDescription {get;set;}
        public string RigType          {get;set;}
        public string RigName          {get;set;}
        public string ProjectName      {get;set;}
	    public string WellName         {get;set;}
        public string AssetName        {get;set;}
        public bool NonOP            {get;set;}
        public double WorkingInterest { get; set; }
        public List<OPHistory> OPHistories { get; set; }
        public bool VirtualPhase { get; set; }
        public bool ShiftFutureEventDate { get; set; }
        public DateRange OpsSchedule { get; set; }
        public DateRange PsSchedule { get; set; }
        public DateRange LESchedule { get; set; }
        public List<WellActivityPhase> Phases { get; set; }
        public DataBrowser(){
            Region              = "";
            OperatingUnit       = "";
            UARigSequenceId     = "";
            UARigDescription    = "";
            RigType             = "";
            RigName             = "";
            ProjectName         = "";
            WellName            = "";
            AssetName           = "";
            NonOP               = false;
            WorkingInterest     = 0.0;
            VirtualPhase        = false;
            ShiftFutureEventDate = false;
            Phases = new List<WellActivityPhase>();
        }
    }

    public class DataBrowserOut
    {
       public int  _id                 {get;set;}
       public string  Region              {get;set;}
       public string  OperatingUnit       {get;set;}
       public string  UARigSequenceId     {get;set;}
       public string  RigType             {get;set;}
       public string  RigName             {get;set;}
       public string  ProjectName         {get;set;}
       public string  WellName            {get;set;}
       public string  AssetName           {get;set;}
       public bool  NonOP               {get;set;}
       public double  WorkingInterest     {get;set;}
       public List<string>  IsInPlan            {get;set;}
       public double  PsDuration          {get;set;}
       public double PlanDuration { get; set; }
       public double PlanCost { get; set; }
       public double PreviousPlanDuration { get; set; }
       public double PreviousPlanCost { get; set; }
       public double OpsCost { get; set; }
       public double OpsDuration { get; set; }
       public DateTime  OpsStart            {get;set;}
       public DateTime OpsFinish { get; set; }
       public double  PhDuration          {get;set;}
       public string  UARigDescription    {get;set;}
       public double  PhRiskDuration      {get;set;}
       public DateTime  PhStartForFilter    {get;set;}
       public DateTime  PhFinishForFilter   {get;set;}
       public DateTime  PsStartForFilter    {get;set;}
       public DateTime  PsFinishForFilter   {get;set;}
       public DateTime  PsStart             {get;set;}
       public DateTime  PsFinish            {get;set;}
       public DateTime  PreviousPsStart     {get;set;}
       public DateTime  PreviousPsFinish    {get;set;}
       public double  PhCost              {get;set;}
       public DateTime  AFEStart            {get;set;}
       public DateTime  AFEFinish           {get;set;}
       public double  AFEDuration         {get;set;}
       public double  AFECost             {get;set;}
       public DateTime  LEStart             {get;set;}
       public DateTime  LEFinish            {get;set;}
       public double  LEDuration          {get;set;}
       public double  LECost              {get;set;}
       public bool  VirtualPhase        {get;set;}
       public bool  ShiftFutureEventDate{get;set;}
       public double  TQDays              {get;set;}
       public double  TQCost              {get;set;}
       public List<OPListHelperForDataBrowserGrid> OPList { get; set; }

       public DataBrowserOut()
       {
           Region              ="";
           OperatingUnit       ="";
           UARigSequenceId     ="";
           RigType             ="";
           RigName             ="";
           ProjectName         ="";
           WellName            ="";
           AssetName           ="";
           NonOP               =false;
           WorkingInterest     =0.0;
           IsInPlan            = new List<string>();
           PsDuration           = 0.0;
           PlanDuration         = 0.0;
           PlanCost             = 0.0;
           PreviousPlanDuration = 0.0;
           PreviousPlanCost     = 0.0;
           OpsCost              = 0.0;
           OpsDuration          = 0.0;
           OpsStart            = Tools.DefaultDate;
           OpsFinish           = Tools.DefaultDate;
           PhDuration          = 0.0;
           UARigDescription    = "";
           PhRiskDuration = 0.0;
           PhStartForFilter     = Tools.DefaultDate;
           PhFinishForFilter    = Tools.DefaultDate;
           PsStartForFilter     = Tools.DefaultDate;
           PsFinishForFilter    = Tools.DefaultDate;
           PsStart              = Tools.DefaultDate;
           PsFinish             = Tools.DefaultDate;
           PreviousPsStart      = Tools.DefaultDate;
           PreviousPsFinish     = Tools.DefaultDate;
           PhCost               = 0.0;
           AFEStart            = Tools.DefaultDate;
           AFEFinish           = Tools.DefaultDate;
           AFEDuration         = 0.0 ;    
           AFECost             = 0.0;
           LEStart             = Tools.DefaultDate;
           LEFinish            = Tools.DefaultDate;
           LEDuration          = 0.0;
           LECost              = 0.0;
           VirtualPhase              = false;
           ShiftFutureEventDate      = false;
           TQDays              = 0.0;
           TQCost              = 0.0;
           OPList = new List<OPListHelperForDataBrowserGrid>();
       }
    }
}
