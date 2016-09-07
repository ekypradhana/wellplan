using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECIS.Core;

namespace ECIS.Client.WEIS
{
    public class SMTPConnection
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool TLS { get; set; }

        //public enum ConType
        public string ConType { get; set; }

        public string Subject { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }


        public static bool SendTestEmail(string From, string Subject, string Message, string Host, int Port, string UserName, string Password, bool TLS, string[] Tos, out string Error)
        {
            try
            {
                Tools.SendMail(From, Subject, Message, Host, Port, UserName, Password, TLS, Tos);
                Error = "";
                return true;

            }
            catch (Exception e)
            {
                Error = Tools.PushException(e);
                return false;
            }
        }


    }
}
