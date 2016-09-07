using System.Timers;
using ECIS.Core;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ECIS.Client.WEIS;

namespace HealthCheck.Model
{
    public class DataHealthConfig
    {
        //
        public string EDMStatus { get; set; } // 0 = fail, 1 = success
        public string EDMMessage { get; set; }

        //DB Config
        public string DB_ServerHost{get; set;}
        public string DB_ServerDB{get; set;}
        public string DB_Port{get; set;}
        public string DB_UserName { get; set; }
        public string DB_Password { get; set; }
        public string DB_UseSSL { get; set; }
        
        //Proxy Config
        public string Proxy_Host { get; set; }
        public string Proxy_Port { get; set; }
        
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

        //Error Message
        public string Error_Message { get; set; }
    }

    public class DataHealth
    {
        public bool CheckDBConnection(DBConnection DBConfig, out string Error, DateTime? dt = null){
            Error = string.Empty;
            try
            {

                MongoClient client = new MongoClient();
                string isNeedAuthentication = ConfigurationSettings.AppSettings["AuthenticationDatabase"].ToString();
                if (DBConfig.UserName != null && !DBConfig.UserName.Equals(""))
                //if (isNeedAuthentication.ToLower() == "true")
                {
                    new DataHealth().WriteLog("Checking DB WITH authentication ...", dt);
                    //"mongodb://myUserName:myPassword@linus.mongohq.com:myPortNumber/";
                    string conStr = DBConfig.UserName + ":" + DBConfig.Password + "@" + DBConfig.ServerHost + ":" + DBConfig.Port;
                    client = new MongoClient("mongodb://" + conStr);
                }
                else
                {
                    new DataHealth().WriteLog("Checking DB WITHOUT authentication ...", dt);
                    string conStr = "mongodb://" + DBConfig.ServerHost + ":" + DBConfig.Port;
                    client = new MongoClient(conStr);
                    
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
        public void GenData(DateTime? Now = null){
            if (Now == null){
                Now = DateTime.Now;
            }
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


                Config.SMTP_UserName = ConfigurationSettings.AppSettings["SMTP_UserName"].ToString();
                Config.SMTP_Password = ConfigurationSettings.AppSettings["SMTP_Password"].ToString();
                Config.SMTP_From= ConfigurationSettings.AppSettings["SMTP_From"].ToString();
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
            ProxyConn.Port = Convert.ToInt32(Config.Proxy_Port);
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

            var dataEDM = new EDMConnection().TestingEDMConnection(Now.Value);

            var StorageMessage = new StorageConnection().GetStorage(DBConn, Now);//(DBConn.ServerHost, DBConn.ServerDB, DBConn.Port, DBConn.UserName, DBConn.Password);

            #endregion

            var d = new{
                Id = String.Format("{0:yyyyMMddHH}", Now),
                Date = Now,
                DBStatus = CheckDBResult ? 1 : 0,
                DBMessage = err_db,
                ProxyStatus = CheckProxyResult ? 1 : 0,
                ProxyMessage = err_proxy,
                SMTPStatus = CheckSMTPResult ? 1 : 0,
                SMTPMessage = err_smtp,
                EDMStatus = dataEDM.Status,
                EDMMessage = dataEDM.Message,
                StorageMessage = StorageMessage
            };

            string json = JsonConvert.SerializeObject(d);
            string dir = String.Format("{0:yyyyMMdd}", Now);
            string filename = String.Format("{0:yyyyMMddHH}", Now) + ".json";
            if (!Directory.Exists(DataPath+dir))
            {
                Directory.CreateDirectory(DataPath+dir);
            }
            System.IO.File.WriteAllText(DataPath+dir+"\\"+filename, json);
        }
        public void GenLastData(DateTime Now ,SysHealthCheck healthCheck)
        {
            if (Now == null)
            {
                Now = DateTime.Now;
            }
            var DataPath = ConfigurationSettings.AppSettings["HealthCheckDataPath"].ToString();
            
            var ConfigPath = DataPath + "customConfig.json";
            bool isConfigExist = false;
            DataHealthConfig Config = new DataHealthConfig();
            #region Reading Config File or Set the default value
            //if (File.Exists(ConfigPath))
            //{
            //    try
            //    {
            //        var text = System.IO.File.ReadAllText(ConfigPath);
            //        Config = JsonConvert.DeserializeObject<DataHealthConfig>(text);
            //        isConfigExist = true;
            //    }
            //    catch (Exception e)
            //    {
            //        Config.Error_Message = e.Message;
            //        isConfigExist = false;
            //    }
            //}

            if (!isConfigExist)
            {
                Config.DB_ServerHost = healthCheck.DBConHealth.ServerHost;
                Config.DB_ServerDB = healthCheck.DBConHealth.ServerDB;
                Config.DB_Port = healthCheck.DBConHealth.Port.ToString();
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
            ProxyConn.Port = Convert.ToInt32(Config.Proxy_Port);
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

            var dataEDM = new EDMConnection().TestingEDMConnection(Now);

            var StorageMessage = new StorageConnection().GetStorage(DBConn, Now);//(DBConn.ServerHost, DBConn.ServerDB, DBConn.Port, DBConn.UserName, DBConn.Password);

            #endregion

            var d = new
            {
                Id = String.Format("{0:yyyyMMddHH}", Now),
                Date = Now,
                DBStatus = CheckDBResult ? 1 : 0,
                DBMessage = err_db,
                ProxyStatus = CheckProxyResult ? 1 : 0,
                ProxyMessage = err_proxy,
                SMTPStatus = CheckSMTPResult ? 1 : 0,
                SMTPMessage = err_smtp,
                EDMStatus = dataEDM.Status,
                EDMMessage = dataEDM.Message,
                StorageMessage = StorageMessage
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
    }
}
