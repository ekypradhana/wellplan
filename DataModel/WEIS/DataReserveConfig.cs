using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
namespace ECIS.Client.WEIS
{

    public class DateIslandExt : DateIsland
    {
        public int WeekIdOfMonth { get; set; }
        public DateTime NearDate { get; set; }
        public DateIslandExt(DateTime di)
        {
            var temp = new DateIsland(di);

            this.DateId = temp.DateId;
            this.Month = temp.Month;
            this.MonthId = temp.MonthId;
            this.Qtr = temp.Qtr;
            this.QtrId = temp.QtrId;
            this.Week = temp.Week;
            this.WeekId = temp.WeekId;
            this.Year = temp.Year;

            this.WeekIdOfMonth = temp.Week % 4;
        }
        public DateIslandExt(DateTime di, string nearDay)
        {
            var temp = new DateIsland(di);

            this.DateId = temp.DateId;
            this.Month = temp.Month;
            this.MonthId = temp.MonthId;
            this.Qtr = temp.Qtr;
            this.QtrId = temp.QtrId;
            this.Week = temp.Week;
            this.WeekId = temp.WeekId;
            this.Year = temp.Year;

            this.WeekIdOfMonth = temp.Week % 4;
            NearDate = Tools.ToUTC(this.SearchDateOfThisWeek(nearDay, this.DateId));
        }
        public List<DateTime> ListOfDatebackward { get; set; }
        public DateIslandExt(DateTime di, string nearDay, int noofWeekBackward)
        {
            var temp = new DateIsland(di);
            ListOfDatebackward = new List<DateTime>();
            this.DateId = temp.DateId;
            this.Month = temp.Month;
            this.MonthId = temp.MonthId;
            this.Qtr = temp.Qtr;
            this.QtrId = temp.QtrId;
            this.Week = temp.Week;
            this.WeekId = temp.WeekId;
            this.Year = temp.Year;

            this.WeekIdOfMonth = temp.Week % 4;
            var datenya = this.DateId;
            for (int i = 0; i < noofWeekBackward; i++)
            {
                datenya = this.SearchDateOfThisWeek(nearDay, datenya);
                ListOfDatebackward.Add(Tools.ToUTC(datenya));
            }

        }

        public DateTime SearchDateOfThisWeek(string day, DateTime date)
        {
            DayOfWeek ad = DayOfWeek.Sunday;
            #region parse
            switch (day.ToLower().Trim())
            {

                case "monday":
                    {
                        ad = DayOfWeek.Monday;
                        break;
                    }
                case "tuesday":
                    {
                        ad = DayOfWeek.Tuesday;
                        break;
                    }
                case "wednesday":
                    {
                        ad = DayOfWeek.Wednesday;
                        break;
                    }
                case "thursday":
                    {
                        ad = DayOfWeek.Thursday;
                        break;
                    }
                case "friday":
                    {
                        ad = DayOfWeek.Friday;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            #endregion
            var datenya = Tools.GetNearestDay(date, ad);

            if (datenya.Date >= DateTime.Now.Date)
                // mundur minggu lalu
                datenya = Tools.GetNearestDay(date.AddDays(-7), ad);
            else if (date < DateTime.Now.Date)
                datenya = Tools.GetNearestDay(date.AddDays(-7), ad);

            return Tools.ToUTC(datenya);
        }
    }

    public class DataReserveConfig
    {
        public string DailyRunningTime { get; set; }
        public int NoOfDays { get; set; }
        [BsonIgnore]
        public List<DateTime> DailyReserveDate { get; set; }

        public int NoOfWeek { get; set; }
        public string WeekType { get; set; }
        [BsonIgnore]
        public List<DateTime> WeeklyReserveDate { get; set; }

        public int NoOfMonth { get; set; }
        public string MonthType { get; set; }
        public string LockStatus { get; set; }
        [BsonIgnore]
        public List<DateTime> MonthlyReserveDate { get; set; }

        [BsonIgnore]
        public List<DateTime> AllReservedDate { get; set; }
        private DateTime _DateFilter { get; set; }
        [BsonIgnore]
        public DateTime DateFilter
        {
            get
            {
                if (_DateFilter == null)
                    _DateFilter = DateTime.Now;
                return _DateFilter;
            }
            set { _DateFilter = value; }
        }

        public DataReserveConfig()
        {
            DailyReserveDate = new List<DateTime>();
            MonthlyReserveDate = new List<DateTime>();
            WeeklyReserveDate = new List<DateTime>();
            AllReservedDate = new List<DateTime>();
            DateFilter = DateTime.Now;
            //DailyRunningTime = new TimeSpan();
        }


        public List<DateTime> GetMonthlyReserveDate()
        {
            if (NoOfMonth > 0)
            {
                List<DateTime> resMonths = new List<DateTime>();
                DateTime nowDate = DateTime.Now.Date;
                for (int i = 0; i < NoOfMonth; i++)
                {
                    var curDatePrev = nowDate.AddMonths((-1) * i);
                    if (MonthType.ToLower().Equals("first date of month"))
                    {
                        DateTime resDate = Tools.ToUTC(new DateTime(curDatePrev.Year, curDatePrev.Month, 1));
                        resMonths.Add(resDate);
                    }
                    else
                    {
                        DateTime resDate = Tools.ToUTC(new DateTime(curDatePrev.Year, curDatePrev.Month, DateTime.DaysInMonth(curDatePrev.Year, curDatePrev.Month)));
                        resMonths.Add(resDate);
                    }

                }
                return resMonths;
            }
            else
                return new List<DateTime>();
        }

        public List<DateTime> GetlastQuarter(DateTime startDate)
        {
            if (NoOfMonth > 0)
            {
                List<DateTime> resMonths = new List<DateTime>();
                DateTime nowDate = startDate.Date;
                for (int i = 0; i < NoOfMonth; i++)
                {
                    var curDatePrev = nowDate.AddMonths((-1) * i);
                    if (MonthType.ToLower().Equals("first date of month"))
                    {
                        DateTime resDate = Tools.ToUTC(new DateTime(curDatePrev.Year, curDatePrev.Month, 1));
                        resMonths.Add(resDate);
                    }
                    else
                    {
                        DateTime resDate = Tools.ToUTC(new DateTime(curDatePrev.Year, curDatePrev.Month, DateTime.DaysInMonth(curDatePrev.Year, curDatePrev.Month)));
                        resMonths.Add(resDate);
                    }

                }
                return resMonths;
            }
            else
                return new List<DateTime>();
        }
        public List<DateTime> GetWeeklyReserveDate(string dateIn)
        {
            if (NoOfWeek > 0)
            {
                List<DateTime> resWeeks = new List<DateTime>();
                DateTime nowDate = DateTime.Now.Date;
                DateIslandExt dd = new DateIslandExt(nowDate, dateIn, NoOfWeek);

                return dd.ListOfDatebackward;
            }
            else
                return new List<DateTime>();
        }

        public List<DateTime> GetlastWeeks(string dateIn,DateTime startDate)
        {
            if (NoOfWeek > 0)
            {
                List<DateTime> resWeeks = new List<DateTime>();
                DateTime nowDate = startDate.Date;
                DateIslandExt dd = new DateIslandExt(nowDate, dateIn, NoOfWeek);

                return dd.ListOfDatebackward;
            }
            else
                return new List<DateTime>();
        }
        public List<DateTime> GetDailyReserveDate()
        {
            if (NoOfDays > 0)
            {
                List<DateTime> resDays = new List<DateTime>();
                DateTime nowDate = DateTime.Now.Date;
                for (int i = 0; i < NoOfDays; i++)
                {
                    var curDatePrev = Tools.ToUTC(nowDate.AddDays((-1) * i));
                    resDays.Add(curDatePrev);

                }
                return resDays;
            }
            else
                return new List<DateTime>();
        }

        public List<DateTime> GetlastDates(DateTime StartDate, int noOfDate, bool isBackWard)
        {
            if (noOfDate > 0)
            {
                List<DateTime> resDays = new List<DateTime>();
                DateTime nowDate = StartDate.Date;
                for (int i = 0; i < NoOfDays; i++)
                {
                    var curDatePrev = new DateTime();
                    if (isBackWard)
                        curDatePrev = Tools.ToUTC(nowDate.AddDays((-1) * i));
                    else
                        curDatePrev = Tools.ToUTC(nowDate.AddDays((1) * i));

                    resDays.Add(curDatePrev);
                }
                return resDays;
            }
            else
                return new List<DateTime>();
        }

        public List<DateTime> GetlastMonths(DateTime StartDate, int noOfDate, bool isBackWard)
        {
            if (noOfDate > 0)
            {
                List<DateTime> resMonths = new List<DateTime>();
                DateTime nowDate = StartDate.Date;
                for (int i = 0; i < NoOfMonth; i++)
                {
                    var curDatePrev = nowDate.AddMonths((-1) * i);
                    if (MonthType.ToLower().Equals("first date of month"))
                    {
                        DateTime resDate = Tools.ToUTC(new DateTime(curDatePrev.Year, curDatePrev.Month, 1));
                        resMonths.Add(resDate);
                    }
                    else
                    {
                        DateTime resDate = Tools.ToUTC(new DateTime(curDatePrev.Year, curDatePrev.Month, DateTime.DaysInMonth(curDatePrev.Year, curDatePrev.Month)));
                        resMonths.Add(resDate);
                    }

                }
                return resMonths;
            }
            else
                return new List<DateTime>();
        }


        public List<DateTime> GetListReservedDate(string dateIn)
        {
            var mon = this.GetMonthlyReserveDate();
            var wek = this.GetWeeklyReserveDate(dateIn);
            var dai = this.GetDailyReserveDate();

            List<DateTime> datereserve = new List<DateTime>();
            datereserve.AddRange(mon.ToList());
            datereserve.AddRange(wek.ToList());
            datereserve.AddRange(dai.ToList());

            this.DailyReserveDate = dai;
            this.WeeklyReserveDate = wek;
            this.MonthlyReserveDate = mon;

            var total = datereserve.Select(x => x.Date).Distinct().OrderByDescending(x => x.Date).ToList();
            this.AllReservedDate = total;

            return total;
        }
    }
}
