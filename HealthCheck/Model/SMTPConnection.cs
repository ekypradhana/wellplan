using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthCheck.Model
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

    }
}
