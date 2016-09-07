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
    public class ProxyConnection
    {
        public string Host { get; set; }
        public int Port { get; set; }

        public static bool CheckProxyConnection(string Host, int Port, out string Error)
        {
            try
            {
                TcpClient tcp = new TcpClient(Host, Port);
                Error = "";
                return true;
            }
            catch (Exception ex)
            {
                Error = Tools.PushException(ex);
                return false;
            }
        }
    }
}
