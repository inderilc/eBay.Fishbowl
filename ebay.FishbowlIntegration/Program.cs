using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eBay.Service.Call;
using eBay.Service.Core.Sdk;
using eBay.Service.Core.Soap;
using eBay.Service;
using eBay.FishbowlIntegration.Configuration;
using ebay.FishbowlIntegration.Controller;


namespace ebay.FishbowlIntegration
{
    public class Program
    {
        static void Main(string[] args)
        {

            var cfg = Config.Load();
            eBayController ebc = new eBayController(cfg);
            ebc.DownloadOrders();
            string a = "0";

        }
    }
}
