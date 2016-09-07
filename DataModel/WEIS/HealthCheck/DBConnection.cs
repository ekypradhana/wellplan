using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using ECIS.Core;
namespace ECIS.Client.WEIS
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


        private static MongoDatabase _db;
        private static MongoDatabase GetDb(string host, string db, string port, out string Error, string username = "", string pass = "")
        {
            try
            {
                if (username == null) username = "";
                if (pass == null) pass = "";

                MongoClient client = new MongoClient();
                if (!username.Equals(""))
                {
                    //"mongodb://myUserName:myPassword@linus.mongohq.com:myPortNumber/";
                    string conStr = username + ":" + pass + "@" + host;
                    client = new MongoClient("mongodb://" + conStr + ":" + port);
                }
                else
                {
                    string conStr = host;
                    client = new MongoClient("mongodb://" + conStr + ":" + port);
                }

                MongoServer server = client.GetServer();
                MongoDatabase t = server.GetDatabase(db);
                _db = t;

                // will throw exception if connection refused
                server.Ping();

                Error = "";
                return _db;
            }
            catch (Exception ex)
            {
                Error = Tools.PushException(ex);
                return null;

            }

        }

        public static bool CheckDBConnection(string host, string db, int port, out string Error, string username = "", string pass = "")
        {
            string er = "";
            if (GetDb(host, db, port.ToString(), out er, username, pass) != null)
            {
                Error = er;
                return true;
            }
            else
            {
                Error = er;
                return false;
            }
        }
    }
}
