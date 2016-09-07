using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson.Serialization.Attributes;

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
    }
}