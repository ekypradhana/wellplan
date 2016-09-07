using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthCheck.Model
{
    public class DBConnection
    {
        public string ServerHost { get; set; }
        public string ServerDB { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Port { get; set; }
        //public bool UseSSL { get; set; }
        public string AuthenticationDatabase { get; set; }
    }
}
