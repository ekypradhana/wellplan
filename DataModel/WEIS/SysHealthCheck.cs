using ECIS.Core;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace ECIS.Client.WEIS
{
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

        public string SendEmailNotification(string [] Tos,  string From, string Subject, string Message, string Host, int Port, string UserName, string Password, bool TLS)
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

}
