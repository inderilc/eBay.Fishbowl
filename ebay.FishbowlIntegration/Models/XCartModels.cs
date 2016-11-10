using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XCart.Fishbowl.Extensions;

namespace XCart.Fishbowl.Models
{
    public class XCOrder
    {
        public List<XCItems>  Items { get; set; }
        public Int32 orderid { get; set; }
        public Int32 userid { get; set; }
        public Int32 all_userid { get; set; }
        public string membership { get; set; }
        public double total { get; set; }
        public double giftcert_discount { get; set; }
        public string giftcert_ids { get; set; }
        public double subtotal { get; set; }
        public double discount { get; set; }
        public string coupon { get; set; }
        public double coupon_discount { get; set; }
        public Int32 shippingid { get; set; }
        public string shipping { get; set; }
        public string tracking { get; set; }
        public double shipping_cost { get; set; }
        public double tax { get; set; }
        public string taxes_applied { get; set; }
        public string date { get; set; }
        public string status { get; set; }
        public string payment_method { get; set; }
        public string flag { get; set; }
        public string notes { get; set; }
        public string details { get; set; }
        public string customer_notes { get; set; }
        public string title { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string company { get; set; }
        public string b_title { get; set; }
        public string b_firstname { get; set; }
        public string b_lastname { get; set; }
        public string b_address { get; set; }
        public string b_city { get; set; }
        public string b_county { get; set; }
        public string b_state { get; set; }
        public string b_country { get; set; }
        public string b_zipcode { get; set; }
        public string b_zip4 { get; set; }
        public string b_phone { get; set; }
        public string b_fax { get; set; }
        public string s_title { get; set; }
        public string s_firstname { get; set; }
        public string s_lastname { get; set; }
        public string s_address { get; set; }
        public string s_city { get; set; }
        public string s_county { get; set; }
        public string s_state { get; set; }
        public string s_country { get; set; }
        public string s_zipcode { get; set; }
        public string s_phone { get; set; }
        public string s_fax { get; set; }
        public string s_zip4 { get; set; }
        public string url { get; set; }
        public string email { get; set; }
        public string language { get; set; }
        public string clickid { get; set; }
        public string extra { get; set; }
        public string membershipid { get; set; }
        public string paymentid { get; set; }
        public double payment_surcharge { get; set; }
        public string tax_number { get; set; }
        public string tax_exempt { get; set; }
        public double init_total { get; set; }
        public string access_key { get; set; }
        public string klarna_order_status { get; set; }

        [JsonIgnore]
        public OrderExtra OrderExtra => GetOrderExtra();

        private OrderExtra GetOrderExtra()
        {
            if (!String.IsNullOrEmpty(extra))
            {
                return extra.DeserializePHP<OrderExtra>();
            }
            else
            {
                return new OrderExtra();
            }
        }
    }

    public class XCItems
    {
        public Int32 orderid { get; set; }
        public Int32 productid { get; set; }
        public double price { get; set; }
        public double amount { get; set; }
        public string provider { get; set; }
        public string product_options { get; set; }
        public string extra_data { get; set; }
        public Int32 itemid { get; set; }
        public string productcode { get; set; }
        public string product { get; set; }
    }

    public class XCProductInventory
    {
        public string productid { get; set; }
        public string productcode { get; set; }
        public double avail { get; set; }
        public double weight { get; set; }
        public double list_price { get; set; }
    }


    public class AdditionalField
    {
        public string fieldid { get; set; }
        public string field { get; set; }
        public string type { get; set; }
        public string variants { get; set; }
        public string def { get; set; }
        public string orderby { get; set; }
        public string section { get; set; }
        public string avail { get; set; }
        public string required { get; set; }
        public string value { get; set; }
        public string title { get; set; }
    }

    public class TaxInfo
    {
        public string display_taxed_order_totals { get; set; }
        public string display_cart_products_tax_rates { get; set; }
        public string tax_operation_scheme { get; set; }
        public string taxed_subtotal { get; set; }
        public string taxed_discounted_subtotal { get; set; }
        public string taxed_shipping { get; set; }
    }

    public class OrderExtra
    {
        public List<AdditionalField> additional_fields { get; set; }
        public TaxInfo tax_info { get; set; }
    }
}
