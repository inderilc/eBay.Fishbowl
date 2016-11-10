using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FishbowlSDK;
using eBay.FishbowlIntegration.Configuration;
//using eBay.FishbowlIntegration.Extensions;
using eBay.Service.Core.Soap;
using ebay.FishbowlIntegration.Extensions;

namespace eBay.FishbowlIntegration.Map
{
    public static class DataMappers
    {
    
        private static string MapCustomerName(Config cfg, OrderType o)
        {
            return StringExtensions.Coalesce(
                cfg?.Store?.OrderSettings?.DefaultCustomer,
                o?.ShippingAddress.Name
                ).Trim();
        }
    }

}