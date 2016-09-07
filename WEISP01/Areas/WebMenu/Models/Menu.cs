using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using ECIS.Biz.Common;

namespace ECIS.AppServer.Areas.WebMenu.Models
{
    [BsonIgnoreExtraElements]
    public class Menu
    {
        public Menu()
        {
            _id = string.Empty;
            Title = string.Empty;
            Url = string.Empty;
            Order = 1000;
            Submenus = new List<Menu>();
        }
        public string _id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public int Order { get; set; }
        public List<Menu> Submenus { get; set; }
        public void RecursiveOrder()
        {
            Submenus = Submenus.OrderBy(x => x.Order)
                .ToList();

            Submenus.ToList().ForEach(c => c.RecursiveOrder());
        }

        public static int getMaxId(Menu menu)
        {
            int maxNumber = 0;
            if (menu.Submenus.Any())
            {
                foreach (var t in menu.Submenus)
                {
                    int num = getMaxId(t);
                    if (maxNumber < num)
                        maxNumber = num;
                }
                return maxNumber;
            }
            else
            {
                return System.Convert.ToInt32(menu._id.Substring(1, menu._id.ToString().Length - 1));
            }
        }

        public static Menu setId(Menu menu, int currentId, out int newId)
        {
            var newI = currentId + 1;
            int outId = 0;
            menu._id = "M" + (newI).ToString("D5");
            if (menu.Submenus.Any())
            {
                int maxfromloop = 0;
                foreach (var t in menu.Submenus)
                {
                    setId(t, newI, out outId);
                    newI = outId;
                    maxfromloop = outId;
                }
                newId = maxfromloop;
            }
            else
            {
                newId = newI;
            }
            return menu;
        }

        public void ReorganizeMenus()
        {
            var menus = DataHelper.Populate<Menu>("Main_Menu");

            List<Menu> newMenus = new List<Menu>();
            int max = 0;

            foreach (var m in menus)
            {
                newMenus.Add(setId(m, max, out max));
            }

            // drop and Save Main_Menu
            DataHelper.Delete("Main_Menu");
            foreach (var mi in newMenus)
            {
                DataHelper.Save("Main_Menu", mi.ToBsonDocument());
            }

            // Save to Sequence Tabel max+1
            var setData = DataHelper.Get("SequenceNos", "Main_Menu");
            setData.Set("NextNo", max + 1);
            DataHelper.Save("SequenceNos", setData);

            var menusNew = DataHelper.Populate<Menu>("Main_Menu");
            List<Menu> menu2 = new List<Menu>();
            foreach (var yyy in menusNew)
            {

                if (yyy.Submenus.Any())
                {
                    foreach (var yx in yyy.Submenus)
                    {
                        if (yx.Submenus.Any())
                        {
                            foreach (var three in yx.Submenus)
                            {
                                menu2.Add(three);
                            }
                        }
                        else
                        {
                            menu2.Add(yx);
                        }
                    }
                }
                else
                {
                    menu2.Add(yyy);
                }
            }

            int maxId = 0;
            foreach (var y in menus)
            {
                int h = getMaxId(y);
                if (h > maxId)
                {
                    maxId = h;
                }
            }
            string yxx = "";
        }
    }
}