using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using ECIS.Core;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using Aspose.Cells;
using Oracle.ManagedDataAccess.Client;


namespace ECIS.Client.WEIS
{
    public class OverviewHealthCheck
    {
        public OverviewHealthCheck()
        {
            RAM = new Storage()
            {
                Percentage = 0,
                Available = 0,
                Total = 0
            };
            WebAppDrive = new Storage()
            {
                Percentage = 0,
                Available = 0,
                Total = 0
            };
            DBDrive = new Storage()
            {
                Percentage = 0,
                Available = 0,
                Total = 0
            };
        }

        public bool isDBUp { get; set; }
        public bool isSMTPUp { get; set; }
        public bool isPROXYUp { get; set; }
        public bool isEDMUp { get; set; }

        public double DBSize { get; set; }
        public double WebAppSize { get; set; }
        public Storage RAM { get; set; }
        public Storage WebAppDrive { get; set; }
        public Storage DBDrive { get; set; }

        public class Storage
        {
            public Storage()
            {
                Available = 0;
                Total = 0;
                Percentage = 0;
            }
            public double Available { get; set; }
            public double Total { get; set; }
            public double Percentage { get; set; }
        }
        
    }
    public class DataHealthConfig
    {
        public DataHealthConfig()
        {
            StorageInfo = new StorageConnection();
        }
        //
        public string Id { get; set; }
        public DateTime Date { get; set; }

        //
        public string EDMStatus { get; set; } // 0 = fail, 1 = success
        public string EDMMessage { get; set; }

        //DB Config
        public string DB_ServerHost { get; set; }
        public string DB_ServerDB { get; set; }
        public string DB_Port { get; set; }
        public string DB_UserName { get; set; }
        public string DB_Password { get; set; }
        public string DB_UseSSL { get; set; }
        public bool IsDBUp { get; set; }
        public string DBMessage { get; set; }

        //Proxy Config
        public string Proxy_Host { get; set; }
        public string Proxy_Port { get; set; }
        public bool IsProxyUp { get; set; }
        public string ProxyMessage { get; set; }

        //SMTP Config
        public string SMTP_Host { get; set; }
        public string SMTP_UserName { get; set; }
        public string SMTP_Password { get; set; }
        public string SMTP_From { get; set; }
        public string SMTP_To { get; set; }
        public string SMTP_Subject { get; set; }
        public string SMTP_Port { get; set; }
        public string SMTP_TLS { get; set; }
        public string SMTP_Message { get; set; }
        public bool IsSMTPUp { get; set; }
        public string SMTPMessage { get; set; }

        //EDM Connection
        public string EDMConnectionString { get; set; }
        public bool isEDMUp { get; set; }

        //Error Message
        public string Error_Message { get; set; }

        public StorageConnection StorageInfo { get; set; }
        

        //[BsonIgnore]
        public Dictionary<string, double> Series { get; set; }
        //[BsonIgnore]
        public DateTime Day { get; set; }
        //[BsonIgnore]
        public string StorageMessage { get; set; }

    }

    public class DataHealth
    {
        public bool CheckDBConnection(DBConnection DBConfig, out string Error, DateTime? dt = null)
        {
            Error = string.Empty;
            try
            {
                MongoClient client = new MongoClient();
                var isNeedAuthentication = ConfigurationSettings.AppSettings["AuthenticationDatabase"].ToString(); 
                if (DBConfig.UserName != null && !DBConfig.UserName.Equals(""))
                {
                    new DataHealth().WriteLog("Checking DB WITH authentication ...", dt);
                    //"mongodb://myUserName:myPassword@linus.mongohq.com:myPortNumber/";
                    string conStr = DBConfig.UserName + ":" + DBConfig.Password + "@" + DBConfig.ServerHost + ":" + DBConfig.Port;
                    try
                    {
                        client = new MongoClient("mongodb://" + conStr);
                    }
                    catch (Exception ex)
                    {

                        Error = ex.Message + " " + ex.StackTrace.ToString();
                        return false;
                    }
                    
                }
                else
                {
                    new DataHealth().WriteLog("Checking DB WITHOUT authentication ...", dt);
                    
                    string conStr = "mongodb://" + DBConfig.ServerHost + ":" + DBConfig.Port;
                    try
                    {
                        client = new MongoClient(conStr);
                    }
                    catch (Exception ex)
                    {
                        Error = ex.Message + " " + ex.StackTrace.ToString();
                        client = new MongoClient();
                        return false;
                    }
                    

                }

                MongoServer server = client.GetServer();

                // will throw exception if connection refused
                client.GetServer().Ping();
                new DataHealth().WriteLog("Success connect to DB", dt);
            }
            catch (Exception e)
            {
                new DataHealth().WriteLog(e.Message + " " + e.StackTrace.ToString(), dt);
                Error = e.Message + " " + e.StackTrace.ToString();
            }
            if (Error != string.Empty)
            {
                return false;
            }
            return true;
        }

        public static bool CheckProxyConnection(ProxyConnection ProxyConfig, out string Error, DateTime? dt = null)
        {
            Error = "";
            try
            {
                new DataHealth().WriteLog("Checking Proxy Connection...", dt);
                TcpClient tcp = new TcpClient(ProxyConfig.Host, ProxyConfig.Port);
                Error = "";
                new DataHealth().WriteLog("Success Connect to Proxy", dt);
                return true;
            }
            catch (Exception ex)
            {
                new DataHealth().WriteLog(ex.Message + " " + (ex.InnerException == null ? "" : ex.StackTrace.ToString()), dt);
                Error = ex.Message + " " + (ex.InnerException == null ? "" : ex.StackTrace.ToString());
                return false;
            }
        }

        public static bool CheckSmtpConnection(SMTPConnection SMTPConfig, out string Error, DateTime? dt = null)
        {
            Error = "";
            try
            {
                new DataHealth().WriteLog("Checking SMTP Connection", dt);
                Tools.SendMail(SMTPConfig.From, SMTPConfig.Subject, SMTPConfig.Message, SMTPConfig.Host, SMTPConfig.Port, SMTPConfig.UserName, SMTPConfig.Password, SMTPConfig.TLS, SMTPConfig.To.Split(','));
                new DataHealth().WriteLog("Success Connect SMTP", dt);
                return true;
            }
            catch (Exception e)
            {
                new DataHealth().WriteLog(e.Message + " " + (e.InnerException == null ? "" : e.StackTrace.ToString()), dt);
                Error = e.Message + " " + (e.InnerException == null ? "" : e.StackTrace.ToString());
                return false;
            }
        }

        public void WriteLog(string Text = null, DateTime? Now = null)
        {
            if (Now == null)
            {
                Now = DateTime.Now;
            }
            var ST = String.Format("{0} : {1}", String.Format("{0:dd-MMM-yyyy  HH:mm:ss}", Now), Text);
            var DataPath = ConfigurationSettings.AppSettings["HealthCheckDataPath"].ToString();
            string dir = String.Format("{0:yyyyMMdd}", Now);
            string filename = "Log-" + String.Format("{0:yyyyMMddHH}", Now) + ".txt";

            if (!Directory.Exists(DataPath + dir))
                Directory.CreateDirectory(DataPath + dir);

            if (!File.Exists(DataPath + dir + "\\" + filename))
            {
                using (FileStream fs = File.Create(DataPath + dir + "\\" + filename))
                {
                    Byte[] info = new UTF8Encoding(true).GetBytes("");
                    fs.Write(info, 0, info.Length);
                }
            }

            Console.WriteLine(ST);
            File.AppendAllText(DataPath + dir + "\\" + filename, ST + Environment.NewLine);
            //System.IO.File.WriteAllText(DataPath + dir + "\\" + filename, json);
        }

        public void GenData(DateTime? Now = null, bool isSendMail = false, List<string> emailTos = null )
        {
            if (Now == null)
            {
                Now = DateTime.Now;
            }
            var d = new DataHealthConfig();
            var DataPath = ConfigurationSettings.AppSettings["HealthCheckDataPath"].ToString();
            var ConfigPath = DataPath + "config.json";
            bool isConfigExist = false;
            DataHealthConfig Config = new DataHealthConfig();
            #region Reading Config File or Set the default value
            if (File.Exists(ConfigPath))
            {
                try
                {
                    var text = System.IO.File.ReadAllText(ConfigPath);
                    Config = JsonConvert.DeserializeObject<DataHealthConfig>(text);
                    isConfigExist = true;
                }
                catch (Exception e)
                {
                    Config.Error_Message = e.Message;
                    isConfigExist = false;
                }
            }

            if (!isConfigExist)
            {
                    Config.DB_ServerHost = ConfigurationSettings.AppSettings["DB_ServerHost"].ToString();
                    Config.DB_ServerDB = ConfigurationSettings.AppSettings["DB_ServerDB"].ToString();
                    Config.DB_Port = ConfigurationSettings.AppSettings["DB_Port"].ToString();
                    Config.DB_UserName = ConfigurationSettings.AppSettings["DB_UserName"].ToString();
                    Config.DB_Password = ConfigurationSettings.AppSettings["DB_Password"].ToString();

                    Config.Proxy_Host = ConfigurationSettings.AppSettings["Proxy_Host"].ToString();
                    Config.Proxy_Port = ConfigurationSettings.AppSettings["Proxy_Port"].ToString();


                    Config.SMTP_Host = ConfigurationSettings.AppSettings["SMTP_Host"].ToString();
                    Config.SMTP_UserName = ConfigurationSettings.AppSettings["SMTP_UserName"].ToString();
                    Config.SMTP_Password = ConfigurationSettings.AppSettings["SMTP_Password"].ToString();
                    Config.SMTP_From = ConfigurationSettings.AppSettings["SMTP_From"].ToString();
                    Config.SMTP_To = ConfigurationSettings.AppSettings["SMTP_To"].ToString();
                    Config.SMTP_Subject = ConfigurationSettings.AppSettings["SMTP_Subject"].ToString();
                    Config.SMTP_Port = ConfigurationSettings.AppSettings["SMTP_Port"].ToString();
                    Config.SMTP_TLS = ConfigurationSettings.AppSettings["SMTP_TLS"].ToString();
                    Config.SMTP_Message = ConfigurationSettings.AppSettings["SMTP_Message"].ToString();
                

                string config_json = JsonConvert.SerializeObject(Config);
                System.IO.File.WriteAllText(ConfigPath, config_json);
            }
            #endregion

            DBConnection DBConn = new DBConnection();
            DBConn.ServerHost = Config.DB_ServerHost;
            DBConn.ServerDB = Config.DB_ServerDB;
            DBConn.Port = Config.DB_Port;
            DBConn.UserName = Config.DB_UserName;
            DBConn.Password = Config.DB_Password;
            string err_db = "";
            string err_proxy = "";
            string err_smtp = "";
            bool CheckDBResult = CheckDBConnection(DBConn, out err_db, Now);

            ProxyConnection ProxyConn = new ProxyConnection();
            ProxyConn.Host = Config.Proxy_Host;
            ProxyConn.Port = Config.Proxy_Port == "" ?0: Convert.ToInt32(Config.Proxy_Port);
            bool CheckProxyResult = CheckProxyConnection(ProxyConn, out err_proxy, Now);

            SMTPConnection SMTPConn = new SMTPConnection();
            SMTPConn.Host = Config.SMTP_Host;
            SMTPConn.UserName = Config.SMTP_UserName;
            SMTPConn.Password = Config.SMTP_Password;
            SMTPConn.From = Config.SMTP_From;
            SMTPConn.To = Config.SMTP_To;
            SMTPConn.Subject = Config.SMTP_Subject;
            SMTPConn.Port = Config.SMTP_Port == ""?0: Convert.ToInt32(Config.SMTP_Port);
            SMTPConn.TLS = Config.SMTP_TLS == ""?false:  Convert.ToBoolean(Config.SMTP_TLS);
            SMTPConn.Message = Config.SMTP_Message;

            bool CheckSMTPResult = CheckSmtpConnection(SMTPConn, out err_smtp, Now);

            #region check edm connection
            string connectionString = ConfigurationSettings.AppSettings["OraServerShell"];           
           var dataEDM = new EDMConnection().TestingEDMConnection(Now.Value,connectionString);
            Config.EDMConnectionString = connectionString;

            StorageConnection  stcon = new StorageConnection();
            var StorageMessage = new StorageConnection().GetStorage(DBConn ,out stcon ,Now);
            
            #endregion

            //give all config
            d = Config;

            d.Id = String.Format("{0:yyyyMMddHH}", Now);
            d.Date = Now.Value;
            d.IsDBUp = CheckDBResult;// ? 1 : 0;
            d.DBMessage = err_db;
            d.IsProxyUp = CheckProxyResult;// ? 1 : 0;
            d.ProxyMessage = err_proxy;
            d.IsSMTPUp = CheckSMTPResult;// ? 1 : 0;
            d.SMTPMessage = err_smtp;
            d.EDMStatus = dataEDM.Status.ToString();
            d.EDMMessage = dataEDM.Message;
           
            d.StorageInfo = stcon;

            string json = JsonConvert.SerializeObject(d);
            string dir = String.Format("{0:yyyyMMdd}", Now);
            string filename = String.Format("{0:yyyyMMddHH}", Now) + ".json";
            if (!Directory.Exists(DataPath + dir))
            {
                Directory.CreateDirectory(DataPath + dir);
            }
            System.IO.File.WriteAllText(DataPath + dir + "\\" + filename, json);
        
            if(isSendMail && emailTos.Any())
            {

                var template = ConfigurationManager.AppSettings["SummaryTemplate"].ToString();
                var pathReport = CreateReport(template);
                
                SendHealthCheckEmail(pathReport, emailTos);
            }
        
        }

        public void GenLastData(DateTime Now, SysHealthCheck healthCheck)
        {
            if (Now == null)
            {
                Now = DateTime.Now;
            }
            var DataPath = ConfigurationSettings.AppSettings["HealthCheckDataPath"].ToString();
            //var serverhost = ConfigurationSettings.AppSettings["DB_ServerHost"].ToString();
            //var port = ConfigurationSettings.AppSettings["DB_Port"].ToString();
            //var serverdb = ConfigurationSettings.AppSettings["DB_ServerDB"].ToString();
            var ConfigPath = DataPath + "customConfig.json";
            bool isConfigExist = false;
            DataHealthConfig Config = new DataHealthConfig();
            #region Reading Config File or Set the default value
            
            if (!isConfigExist)
            {
                    Config.DB_ServerHost = healthCheck.DBConHealth.ServerHost;
                    Config.DB_ServerDB = healthCheck.DBConHealth.ServerDB;
                    Config.DB_Port = healthCheck.DBConHealth.Port == null ? "" : healthCheck.DBConHealth.Port.ToString();
                    Config.DB_UserName = healthCheck.DBConHealth.UserName;
                    Config.DB_Password = healthCheck.DBConHealth.Password;

                    Config.Proxy_Host = healthCheck.ProxyConHealth.Host;
                    Config.Proxy_Port = healthCheck.ProxyConHealth.Port.ToString();

                    Config.SMTP_UserName = healthCheck.SMTPConHealth.UserName;
                    Config.SMTP_Password = healthCheck.SMTPConHealth.Password;
                    Config.SMTP_From = healthCheck.SMTPConHealth.From;
                    Config.SMTP_To = healthCheck.SMTPConHealth.To;
                    Config.SMTP_Subject = healthCheck.SMTPConHealth.Subject;
                    Config.SMTP_Port = healthCheck.SMTPConHealth.Port.ToString();
                    Config.SMTP_TLS = healthCheck.SMTPConHealth.TLS.ToString();
                    Config.SMTP_Message = healthCheck.SMTPConHealth.Message;
                //var c = new DataHealthConfig
                //{
                //    DB_ServerHost = healthCheck.DBConHealth.ServerHost,
                //    DB_ServerDB = healthCheck.DBConHealth.ServerDB,
                //    DB_Port = healthCheck.DBConHealth.Port == null ? "" : healthCheck.DBConHealth.Port.ToString(),
                //    DB_UserName = healthCheck.DBConHealth.UserName,
                //    DB_Password = healthCheck.DBConHealth.Password,

                //    Proxy_Host = healthCheck.ProxyConHealth.Host,
                //    Proxy_Port = healthCheck.ProxyConHealth.Port.ToString(),

                //    SMTP_UserName = healthCheck.SMTPConHealth.UserName,
                //    SMTP_Password = healthCheck.SMTPConHealth.Password,
                //    SMTP_From = healthCheck.SMTPConHealth.From,
                //    SMTP_To = healthCheck.SMTPConHealth.To,
                //    SMTP_Subject = healthCheck.SMTPConHealth.Subject,
                //    SMTP_Port = healthCheck.SMTPConHealth.Port.ToString(),
                //    SMTP_TLS = healthCheck.SMTPConHealth.TLS.ToString(),
                //    SMTP_Message = healthCheck.SMTPConHealth.Message
                //};

                string config_json = JsonConvert.SerializeObject(Config);
                System.IO.File.WriteAllText(ConfigPath, config_json);
            }
            #endregion

            DBConnection DBConn = new DBConnection();
            DBConn.ServerHost = Config.DB_ServerHost;
            DBConn.ServerDB = Config.DB_ServerDB;
            DBConn.Port = Config.DB_Port;
            DBConn.UserName = Config.DB_UserName;
            DBConn.Password = Config.DB_Password;
            string err_db = "";
            string err_proxy = "";
            string err_smtp = "";
            bool CheckDBResult = CheckDBConnection(DBConn, out err_db, Now);

            ProxyConnection ProxyConn = new ProxyConnection();
            ProxyConn.Host = Config.Proxy_Host;
            ProxyConn.Port =  Config.Proxy_Port == ""?0:  Convert.ToInt32(Config.Proxy_Port);
            bool CheckProxyResult = CheckProxyConnection(ProxyConn, out err_proxy, Now);

            SMTPConnection SMTPConn = new SMTPConnection();
            SMTPConn.Host = Config.SMTP_Host;
            SMTPConn.UserName = Config.SMTP_UserName;
            SMTPConn.Password = Config.SMTP_Password;
            SMTPConn.From = Config.SMTP_From;
            SMTPConn.To = Config.SMTP_To;
            SMTPConn.Subject = Config.SMTP_Subject;
            SMTPConn.Port = Convert.ToInt32(Config.SMTP_Port);
            SMTPConn.TLS = Convert.ToBoolean(Config.SMTP_TLS);
            SMTPConn.Message = Config.SMTP_Message;

            bool CheckSMTPResult = CheckSmtpConnection(SMTPConn, out err_smtp, Now);

            #region check edm connection

            var dataEDM = new EDMConnection().TestingEDMConnection(Now,healthCheck.EDMConHealth.OraServerShell);
            StorageConnection stcon = new StorageConnection();
            var StorageMessage = new StorageConnection().GetStorage(DBConn, out stcon , Now);//(DBConn.ServerHost, DBConn.ServerDB, DBConn.Port, DBConn.UserName, DBConn.Password);

            #endregion

            var d = new DataHealthConfig
            {
                Id = String.Format("{0:yyyyMMddHH}", Now),
                Date = DateTime.Now,
                IsDBUp = CheckDBResult,// ? 1 : 0,
                DBMessage = err_db,
                IsProxyUp = CheckProxyResult,// ? 1 : 0,
                ProxyMessage = err_proxy,
                IsSMTPUp = CheckSMTPResult,// ? 1 : 0,
                SMTPMessage = err_smtp,
                EDMStatus = dataEDM.Status.ToString(),
                EDMMessage = dataEDM.Message,
                StorageInfo = stcon

            };

            string json = JsonConvert.SerializeObject(d);
            string dir = String.Format("{0:yyyyMMddHH}", Now);
            string filename = String.Format("{0:yyyyMMddHH}", Now) + ".json";
            if (!Directory.Exists(DataPath + dir))
            {
                Directory.CreateDirectory(DataPath + dir);
            }
            System.IO.File.WriteAllText(DataPath + dir + "\\" + filename, json);
        }

        public string CreateReport(string templatepath)//
        {
            #region Get Historical Data
            var DataPath = ConfigurationManager.AppSettings["HealthCheckDataPath"].ToString();
            var historicalData = new List<HistoricalData>();
            var PopulateHours = new List<string>();
            var Now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
            for (var i = Now; i <= DateTime.Now; i = i.AddHours(1))
            {
                PopulateHours.Add(i.ToString("yyyyMMdd"));
            }
            
            string folder = @DataPath + DateTime.Now.ToString("yyyyMMdd");
            var path = Directory.GetFiles(folder).FirstOrDefault(); 
            if (path.Any() )
            {
                string[] file_list = Directory.GetFiles(folder).Where(x => 
                    x.Contains(".json") ).ToArray();
                foreach (var f in file_list)
                {
                    string text = "";
                    try
                    {
                        text = System.IO.File.ReadAllText(f);
                        var data = JsonConvert.DeserializeObject<HistoricalData>(text);
                        data.StorageMessage = " DataBase Size : " + data.StorageInfo.DBSize
                            //+ " \nAvailable Physical Memory : " + data.StorageInfo.AvailablePhysicalMemory
                            //+ " \nTotal Physical Memory : " + data.StorageInfo.TotalPhysicalMemory
                            + " \nTotal WebApp Drive Storage : " + data.StorageInfo.TotalWebAppDriveSize
                            + " \nAvailable WebApp Drive Size : " + data.StorageInfo.AvailableWebAppDriveSize
                            + " \nUsed WebApp Drive Size : " + data.StorageInfo.UsedWebAppDriveSize
                        + " \nWebApp Size : " + data.StorageInfo.WebAppSize;
                        //+ " \nDB Drive Available Size : " + MinimalizeStorage(AvailableDBDriveSize)
                        //+ " \nDB Drive Used Size : " + MinimalizeStorage(UsedDBDriveSize);
                        data.Day = new DateTime(data.Date.Year, data.Date.Month, data.Date.Day,data.Date.Hour,0,0);
                        data.DBStatus = data.DBStatus == 0 ? 1.0000000000001 : data.DBStatus;
                        data.EDMStatus = data.EDMStatus == 0 ? 1.0000000000001 : data.EDMStatus;
                        data.SMTPStatus = data.SMTPStatus == 0 ? 1.0000000000001 : data.SMTPStatus;
                        data.ProxyStatus = data.ProxyStatus == 0 ? 1.0000000000001 : data.ProxyStatus;
                        historicalData.Add(data);
                    }
                    catch (Exception e)
                    {
                        //return Tools.PushException(e);
                    }
                }
            }

            #endregion
             //string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "DataBrowserExportTemplate.xlsx");
            //string xlst = Directory.GetFiles(@templatepath).FirstOrDefault();

            Workbook wb = new Workbook(templatepath);
            var ws = wb.Worksheets[0];
            int startRow = 3;
            
            foreach (var hist in historicalData)
            {
                ws.Cells["A" + startRow.ToString()].Value = Tools.ToUTC(hist.Day).ToString("dd-MMM-yyyy HH:MM");
                ws.Cells["B" + startRow.ToString()].Value = hist.StorageMessage == "" ? "UP" : hist.StorageMessage;
                ws.Cells["C" + startRow.ToString()].Value = hist.DBMessage == "" ? "UP" : hist.DBMessage;
                ws.Cells["D" + startRow.ToString()].Value = hist.SMTPMessage == "" ? "UP" : hist.SMTPMessage;


                ws.Cells["E" + startRow.ToString()].Value = hist.EDMMessage == "" ? "UP" : hist.EDMMessage;
                ws.Cells["F" + startRow.ToString()].Value = hist.ProxyMessage == "" ? "UP" : hist.ProxyMessage;
                startRow++;
            }

            string newFileNameSingle = Path.Combine(folder,
                     String.Format("ReportHealthCheck-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy")));
            string returnName = String.Format("ReportHealthCheck-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy"));

            wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);
            return newFileNameSingle;
        }

        public string SendHealthCheckEmail(string attachmentspath,List<string> toEmails)
        {
           
            string response = "";
            List<string> ccEmails = new List<string>();
            List<string> filenames = new List<string> { attachmentspath};
            Dictionary<string, string> variables = new Dictionary<string, string>();
            variables.Add("UpdateVersion", String.Format("{0:dd-MMM-yyyy}", DateTime.Now));
            var developermodeemail = ConfigurationManager.AppSettings["DeveloperModeEmail"].ToString();
            try
            {
                var e = Email.Send("HealthCheckDaily",
                  toEmails.ToArray(),//To Emails
                  ccMails: ccEmails.ToArray(),
                  variables: variables,
                  attachments: filenames,
                  developerModeEmail: developermodeemail);
                //developerModeEmail: WebTools.LoginUser.Email);
                //if (e.Result == "OK") { response = "Message sent"; } else { throw new Exception(); }
                response = e.Message;
            }
            catch (Exception e)
            {
                response = e.Message;

            }
            return response;
        }

        
    }

    public class SysHealthCheck : ECISModel
    {
        public SysHealthCheck()
        {
            DBConHealth = new DBConnection();
            isDBConHealthy = false;
            this.SMTPConHealth = new SMTPConnection();
            isSMTPHealthy = false;
            ProxyConHealth = new ProxyConnection();
            isProxyHealthy = false;
            StorageInfo = new StorageConnection();
            EDMConHealth = new EDMConnection();
            isEDMHealthy = false;
        }
        public override string TableName
        {
            get { return "SysHealthChecks"; }
        }

        public DBConnection DBConHealth { get; set; }
        public bool isDBConHealthy { get; set; }
        public string DBConHealthyFailedMsg { get; set; }

        public SMTPConnection SMTPConHealth { get; set; }
        public bool isSMTPHealthy { get; set; }
        public string SMTPConHealthFailedMsg { get; set; }


        public ProxyConnection ProxyConHealth { get; set; }
        public bool isProxyHealthy { get; set; }
        public string ProxyConHealthFailedMsg { get; set; }

        public StorageConnection StorageInfo { get; set; }
        public string StorageMessage { get; set; }

        public EDMConnection EDMConHealth { get; set; }
        public bool isEDMHealthy { get; set; }
        public string EDMConHealthFailedMsg { get; set; }
        // public enum SourceCheck
        public string CheckFrom { get; set; }

        public string SendEmailNotification(string[] Tos, string From, string Subject, string Message, string Host, int Port, string UserName, string Password, bool TLS)
        {
            String ret = "OK";
            try
            {
                ResultInfo ri = Tools.SendMail(From, Subject, Message, Host, Port, UserName, Password, TLS, Tos);
                if (ri.Result != "OK") ret = ri.Message;
            }
            catch (Exception e)
            {
                ret = Tools.PushException(e);
            }
            return ret;
        }

        
    }

    public enum SourceCheck
    {
        Batch,
        Web
    }

    public enum ConType
    {
        Telnet,
        MailSend
    }
    public class HistoricalData
    {
        public DateTime Date { get; set; }
        public double DBStatus { get; set; }
        public string DBMessage { get; set; }
        public double ProxyStatus { get; set; }
        public string ProxyMessage { get; set; }
        public double SMTPStatus { get; set; }
        public string SMTPMessage { get; set; }
        public string StorageMessage { get; set; }
        public double EDMStatus { get; set; }
        public string EDMMessage { get; set; }
        public DateTime Day { get; set; }
        public StorageConnection StorageInfo { get; set; }
    }

}
