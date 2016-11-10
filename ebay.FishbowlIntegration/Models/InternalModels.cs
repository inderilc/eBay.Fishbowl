using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eBay.Service.Core.Soap;
using FishbowlSDK;
namespace eBay.FishbowlIntegration.Models
{
    public class eBayFBOrder
    {
        public String CustomerName { get; set; }
        public OrderType eBayOrder { get; set; }
        public FishbowlSDK.SalesOrder FbOrder { get; set; }
    }



    public class SimpleList
    {
        public String Name { get; set; }
        public Boolean InEB { get; set; }
        public Boolean InFB { get; set; }
    }

}
