using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core.DataSerializer;
using ECIS.Core;
using ECIS.Client.WEIS;
using Microsoft.Ajax.Utilities;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.IO;

using Aspose.Cells;
using Aspose.Pdf;
using Aspose.Pdf.Generator;
using System.Text;
using System.Globalization;
using MongoDB.Bson.Serialization;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth]
    public class TreeHelper
    {
        public string text { get; set; }
        public TreeHelper[] items { get; set; }

        public bool isParent { get; set; }

        public string Provider { get; set; }
        public string Streamer { get; set; }
        public string File { get; set; }
        public string EDMRigName { get; set; }
        public bool isActive { get; set; }
        public string RigName { get; set; }

        public int id { get; set; }

        public TreeHelper()
        {
            items = new TreeHelper[] { };
        }

    }
    public class RSVPController : Controller
    {
        //
        // GET: /Shell/RSVP/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult Search()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var y = MasterRigName.GetOnlyRigWithStream();

                List<TreeHelper> helpers = new List<TreeHelper>();

                foreach (var res in y)
                {
                    TreeHelper h = new TreeHelper();
                    h.text = res._id.ToString();
                    List<string> list = new List<string>();

                    List<TreeHelper> lists = new List<TreeHelper>();
                    foreach (var t in res.Streams)
                    {
                        lists.Add(new TreeHelper { 
                            text = t.Title,
                            EDMRigName = t.EDMRigName,
                            File  = t.File,
                            isActive = t.isActive,
                            isParent = false,
                            Provider = t.Provider,
                            Streamer = t.Streamer,
                            RigName = t.RigName ,                          
                            id = Convert.ToInt32 (t._id)
                        
                        }); 
                    }
                    h.isActive = true;
                    h.isParent = true;
                    h.items = lists.ToArray();
                    helpers.Add(h);
                }

                ri.Data = helpers.OrderBy(d => d.text).ToArray();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return Json(ri, JsonRequestBehavior.AllowGet);
        }

        public void Save(List<ListConfig> key)
        {
            var ret = new Dictionary<int, int>();
            if (key != null)
            {
                foreach (var x in key)
                {
                    ret.Add(x.key, x.value);
                }
            }
            WEISStreamConfig.SetStreamAppereances(WebTools.LoginUser.UserName, ret);
        }

        public JsonResult GetConfigStream()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var ret = WEISStreamConfig.GetStreamAppereancesTree(WebTools.LoginUser.UserName);

                return ret.Select(x => x.ToDictionary());
            });
        }



    }

    public class ListConfig
    {
        public int key { get; set; }
        public int value { get; set; }
    }

}