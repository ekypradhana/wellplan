using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ECIS.Core;
using ECIS.Client.WEIS;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Newtonsoft.Json;
using ECIS.Identity;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class HealthCheckController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult SaveDBHealth(DBConnection DBConn)
        {
            return MvcResultInfo.Execute(() =>
            {
                var table = "SharedConfigTable";
                var id = "WEISDBDefault";
                
                DataHelper.Delete(table, Query.EQ("_id", id));

                var doc = DBConn.ToBsonDocument();
                doc.Set("_id", id);
                DataHelper.Save(table, doc);

                return "OK";
            });
        }

        public JsonResult SaveProxyHealth(ProxyConnection ProxyConn)
        {
            return MvcResultInfo.Execute(() =>
            {
                var table = "SharedConfigTable";
                var id = "WEISProxyDefault";
                
                DataHelper.Delete(table, Query.EQ("_id", id));

                var doc = ProxyConn.ToBsonDocument();
                doc.Set("_id", id);
                DataHelper.Save(table, doc);

                return "OK";
            });
        }

        public JsonResult SaveSMTPHealth(SMTPConnection SMTPConn)
        {
            return MvcResultInfo.Execute(() =>
            {
                var table = "SharedConfigTable";
                var id = "WEISSMTPDefault";

                DataHelper.Delete(table, Query.EQ("_id", id));

                var doc = SMTPConn.ToBsonDocument();
                doc.Set("_id", id);
                DataHelper.Save(table, doc);

                return "OK";
            });
        }

        public void CheckHealth(SysHealthCheck healthCheck)
        {
            SysHealthCheck LogHC = new SysHealthCheck();

            string er = "";

            var DBConn = healthCheck.DBConHealth;
            var CheckDB = DBConnection.CheckDBConnection(DBConn.ServerHost, DBConn.ServerDB, DBConn.Port, out er, DBConn.UserName, DBConn.Password);
            LogHC.DBConHealth = DBConn;
            LogHC.DBConHealthyFailedMsg = er;
            LogHC.isDBConHealthy = CheckDB;

            var ProxyConn = healthCheck.ProxyConHealth;
            var CheckProxy = ProxyConnection.CheckProxyConnection(ProxyConn.Host, ProxyConn.Port, out er);
            LogHC.ProxyConHealth = ProxyConn;
            LogHC.isProxyHealthy = CheckProxy;
            LogHC.ProxyConHealthFailedMsg = er;

            var SMTPConn = healthCheck.SMTPConHealth;
            var To = SMTPConn.To.Split(',');
            var CheckSMTP = SMTPConnection.SendTestEmail(SMTPConn.From, SMTPConn.Subject, SMTPConn.Message, SMTPConn.Host, SMTPConn.Port, SMTPConn.UserName, SMTPConn.Password, SMTPConn.TLS, To, out er);
            LogHC.SMTPConHealth = SMTPConn;
            LogHC.isSMTPHealthy = CheckSMTP;
            LogHC.SMTPConHealthFailedMsg = er;

            LogHC.Save();
        }

        private SysHealthCheck GetHealthCheck(SysHealthCheck healthCheck, bool isCheckHealth)
        {
            DBConnection DBConHealth;
            SMTPConnection SMTPConHealth;
            ProxyConnection ProxyConHealth;

            if (healthCheck == null)
            {
                DBConHealth = DataHelper.Get<DBConnection>("SharedConfigTable", Query.EQ("_id", "WEISDBDefault")) ?? new DBConnection();
                SMTPConHealth = DataHelper.Get<SMTPConnection>("SharedConfigTable", Query.EQ("_id", "WEISSMTPDefault")) ?? new SMTPConnection();
                ProxyConHealth = DataHelper.Get<ProxyConnection>("SharedConfigTable", Query.EQ("_id", "WEISProxyDefault")) ?? new ProxyConnection();
            }
            else
            {
                DBConHealth = healthCheck.DBConHealth;
                SMTPConHealth = healthCheck.SMTPConHealth;
                ProxyConHealth = healthCheck.ProxyConHealth;
            }

            if (isCheckHealth)
                CheckHealth(new SysHealthCheck() { DBConHealth = DBConHealth, SMTPConHealth = SMTPConHealth, ProxyConHealth = ProxyConHealth });

            var latestCheckHealth = DataHelper.Populate<SysHealthCheck>(new SysHealthCheck().TableName, sort: SortBy.Descending("LastUpdate"), take: 1)
                .FirstOrDefault() ?? new SysHealthCheck();

            latestCheckHealth.DBConHealth = DBConHealth;
            latestCheckHealth.ProxyConHealth = ProxyConHealth;
            latestCheckHealth.SMTPConHealth = SMTPConHealth;

            return latestCheckHealth;
        }

        private Dictionary<string, List<HistoricalPingData>> GetHistoricalData()
        {
            var date = DateTime.Now;
            var queryForPings = Query.And(
                Query.GTE("LastUpdate", new DateTime(date.Year, date.Month, 1)),
                Query.LT("LastUpdate", new DateTime(date.Year, date.Month + 1, 1))
            );
            var pings = DataHelper.Populate<SysHealthCheck>(new SysHealthCheck().TableName, q: queryForPings);

            var pingsOfDB = pings.Select(d => new
            {
                Date = d.LastUpdate,
                IsHealthy = d.isDBConHealthy
            })
            .GroupBy(d => new DateTime(d.Date.Year, d.Date.Month, d.Date.Day))
            .Select(d => new HistoricalPingData
            {
                Date = d.Key,
                Up = d.Where(e => e.IsHealthy).Count(),
                Down = d.Where(e => !e.IsHealthy).Count(),
                Total = d.Count()
            })
            .ToList();

            var pingsOfSMTP = pings.Select(d => new
            {
                Date = d.LastUpdate,
                IsHealthy = d.isSMTPHealthy
            })
            .GroupBy(d => new DateTime(d.Date.Year, d.Date.Month, d.Date.Day))
            .Select(d => new HistoricalPingData
            {
                Date = d.Key,
                Up = d.Where(e => e.IsHealthy).Count(),
                Down = d.Where(e => !e.IsHealthy).Count(),
                Total = d.Count()
            })
            .ToList();

            var pingsOfProxy = pings.Select(d => new
            {
                Date = d.LastUpdate,
                IsHealthy = d.isProxyHealthy
            })
            .GroupBy(d => new DateTime(d.Date.Year, d.Date.Month, d.Date.Day))
            .Select(d => new HistoricalPingData
            {
                Date = d.Key,
                Up = d.Where(e => e.IsHealthy).Count(),
                Down = d.Where(e => !e.IsHealthy).Count(),
                Total = d.Count()
            })
            .ToList();

            for (var i = 0; i < 5; i++)
            {
                var emptyHistoricalData = new HistoricalPingData() { Date = date.AddDays(-1 * (i + 1)) };

                if (pingsOfDB.Count() < 5)
                    pingsOfDB.Insert(0, emptyHistoricalData);
                if (pingsOfProxy.Count() < 5)
                    pingsOfProxy.Insert(0, emptyHistoricalData);
                if (pingsOfSMTP.Count() < 5)
                    pingsOfSMTP.Insert(0, emptyHistoricalData);
            }

            var res = new Dictionary<string, List<HistoricalPingData>>();
            res.Add("database", pingsOfDB);
            res.Add("proxy", pingsOfProxy);
            res.Add("smtp", pingsOfSMTP);

            return res;
        }

        public JsonResult RefreshData(SysHealthCheck healthCheck, bool isCheckHealth)
        {
            return MvcResultInfo.Execute(() =>
            {
                var latestCheckHealth = GetHealthCheck(healthCheck, isCheckHealth);
                var historicalData = GetHistoricalData();

                var data = new
                {
                    healthCheck = latestCheckHealth,
                    historicalData = new
                    {
                        database = historicalData["database"],
                        smtp = historicalData["smtp"],
                        proxy = historicalData["proxy"]
                    },
                    lastPing = new
                    {
                        database = latestCheckHealth.LastUpdate,
                        smtp = latestCheckHealth.LastUpdate,
                        proxy = latestCheckHealth.LastUpdate,
                    },
                };
                return data;
            });
        }
	}

    public class HistoricalPingData
    {
        public DateTime Date { get; set; }
        public int Up { get; set; }
        public int Down { get; set; }
        public int Total { get; set; }
    }
}