using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBay.FishbowlIntegration.Models
{
    public class FBInventory
    {
        public String NUM { get; set; }
        public Double QTY { get; set; }

        public Double PRICE { get; set; }

        public Double WEIGHT { get; set; }
    }

    public class Stateconst
    {
        public System.Int32 ID { get; set; }
        public System.Int32 COUNTRYCONSTID { get; set; }
        public System.String NAME { get; set; }
        public System.String CODE { get; set; }
    }
    public class Countryconst
    {
        public System.Int32 ID { get; set; }
        public System.String ABBREVIATION { get; set; }
        public System.String NAME { get; set; }
        public System.Int16 UPS { get; set; }
    }
    public class CountryAndState
    {
        public Countryconst Country { get; set; }
        public Stateconst State { get; set; }
    }

    public class Shipment
    {
        public String SONUM { get; set; }
        public String CPO { get; set; }
        public String ORDERNUM { get; set; }
        public String TRACKINGNUM { get; set; }
        public String CARRIERNAME { get; set; }
    }
}
