using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Configuration;

namespace ECIS.ConsoleApp.DataQC
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {

                Console.WriteLine("Starting Data QC Automation Batch...");
                string BaseURL = System.Configuration.ConfigurationManager.AppSettings["BaseURL"];
                string sURL = BaseURL + "Account/LoginProcess/?UserName=eaciit&Password=W31sAdmin&rememberMe=false";


                Console.WriteLine("Login authentication process...");
                //using HttpWebRequest
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(sURL);

                request.AllowAutoRedirect = true;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                var splitHeaderResp = response.Headers.ToString().Split(new string[] { "Set-Cookie: " }, StringSplitOptions.None)[1].Split(';');
                string cookie = splitHeaderResp[0];
                var getECISIdentity = response.Headers.ToString().Split(new string[] { "ECISIdentity=" }, StringSplitOptions.None)[1].Split(';')[0];
                var ECISIdentity = "ECISIdentity=" + getECISIdentity;
                cookie = cookie + "; " + ECISIdentity + ";"; //"; ECISIdentity=q5HLYk618bU3mn0BMXONdOEiBwBzLyjyFhbvMv9pBGW3GHM5whhCF3g1HGnWCnlfyNNTFMIcczb3u2XMRRYXar0KDmrqyVBkdkGom3-7hgwEaGdedDJH53sbnkc5TAI65jbZjaO4aW0IIUw75VWaYnFcpGv6fKwnGXrCwp4_ctJexlc_PSjYLym99ANhgiM0W6HC3KxixyLxRB3A6Whc8NQZIa7da5PHiTyvAAmLhMcPvkD70KD5VBRiMuhwHGdS_bDwzlp_eHn3pdRtFdx_TfewiTHGW6yT_DC53te3pz9L3U3H6UozfxoI1NSDlytxvT5IDa8dXa1sU2nRTanf9ZLK-Fnqs9zNsLD4lMdzdN7hdYwwk5mQDOY4-GGLhdnMIVj4xVjmUb4K6iPEzNk0TPzYO0D0kBQf9y_3VsTwtfIox0SffuMkHlzDdw7MApKtYCRZg44cHIRs8UrehY5JK5pThEHf6s_JdEy9QEX0WFA";
                var toMailString = System.Configuration.ConfigurationManager.AppSettings["Emails"];
                var redirectedUrl = BaseURL + "shell/DataQC/SendEmailMODChecker/?toMailString="+toMailString+"&FileNameDownload=DataQCFromBatch&isByBatch=true";

                Console.WriteLine("Login succeed...");
                Console.WriteLine("Starting to load the QC Data...");

                HttpWebRequest authenticatedRequest = (HttpWebRequest)WebRequest.Create(redirectedUrl);
                authenticatedRequest.Headers["Cookie"] = cookie;
                authenticatedRequest.Referer = "http://localhost/ShellDev/shell/DataQC";
                authenticatedRequest.Host = "localhost";
                authenticatedRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                authenticatedRequest.Headers["Accept-Encoding"] = "gzip, deflate, sdch";
                authenticatedRequest.Headers["Accept-Language"] = "id-ID,id;q=0.8,en-US;q=0.6,en;q=0.4,ms;q=0.2";
                authenticatedRequest.KeepAlive = true;
                authenticatedRequest.Headers["Upgrade-Insecure-Requests"] = "1";

                authenticatedRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36";
                //authenticatedRequest.Headers["Referer"] = "http://localhost/ShellDev/shell/DataQC";
                //authenticatedRequest.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36";
                authenticatedRequest.Timeout = 1200000;


                using (HttpWebResponse response2 = (HttpWebResponse)authenticatedRequest.GetResponse())
                {
                    // Do your processings here....
                    Console.WriteLine("Content length is {0}", response2.ContentLength);
                    Console.WriteLine("Content type is {0}", response2.ContentType);

                    // Get the stream associated with the response.
                    Stream receiveStream = response2.GetResponseStream();

                    // Pipes the stream to a higher level stream reader with the required encoding format. 
                    StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);

                    Console.WriteLine("Response stream received.");
                    Console.WriteLine(readStream.ReadToEnd());
                    response.Close();
                    readStream.Close();
                }
            }
            catch (Exception e)
            {

            }

            //Console.ReadLine();
        }
        
    }
}
