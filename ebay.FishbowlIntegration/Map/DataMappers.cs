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
using eBay.FishbowlIntegration.Models;


namespace eBay.FishbowlIntegration.Map
{
    public static class DataMappers
    {
        public static FishbowlSDK.SalesOrder MapSalesOrder(Config cfg, eBayFBOrder ord, String OrderStatus)
        {
            SalesOrder salesOrder = new SalesOrder();

            var o = ord.eBayOrder;

            salesOrder.CustomerName = ord.CustomerName;

            salesOrder.TotalIncludesTax = true;
            salesOrder.TotalIncludesTaxSpecified = true;



            salesOrder.CustomerPO = o.OrderID.ToString();

            /*
            salesOrder.Salesman = cfg.Store.OrderSettings.Salesman;
            salesOrder.Carrier = MapCarrier(cfg, o.shipping);
            salesOrder.LocationGroup = cfg.Store.OrderSettings.LocationGroup;
            salesOrder.FOB = cfg.Store.OrderSettings.ShipTerms;
            salesOrder.Status = OrderStatus;

            salesOrder.CustomerContact = o.orderid.ToString() + " - " + o.payment_method;

            salesOrder.Note = o.customer_notes ?? ""; // Fishbowl Fix 

            salesOrder.Items = MapItems(cfg, ord.XCOrder.Items).ToList();

            salesOrder.Items.Add(AddSubTotal(salesOrder.Items.First()));

            salesOrder.Items.Add(AddDiscountMiscSale(o.discount, salesOrder.Items.First()));
            if (o.giftcert_discount > 0)
            {
                salesOrder.Items.Add(AddSubTotal(salesOrder.Items.First()));
                salesOrder.Items.Add(AddGiftCertificateDiscount(o.giftcert_discount, o.giftcert_ids, salesOrder.Items.First()));
            }

            salesOrder.Items.Add(AddShipping(MapCarrier(cfg, o.shipping), "Shipping", Math.Round(o.shipping_cost, 2), salesOrder.Items.First()));

            salesOrder.CustomFields = MapCustomFields(ord);
            */

            salesOrder.Ship = new ShipType
            {
                AddressField = o.ShippingAddress?.CompanyName+" "+o.ShippingAddress?.Street+" "+ o.ShippingAddress?.Street1 + " "+ o.ShippingAddress?.Street2,
                City = o.ShippingAddress.CityName,
                Country = o.ShippingAddress.CountryName,
                State = o.ShippingAddress.StateOrProvince,
                Zip = o.ShippingAddress.PostalCode,
                Name = o.ShippingAddress.FirstName + " " + o.ShippingAddress.LastName
            };

            salesOrder.BillTo = new BillType
            {
                AddressField = o.ShippingAddress?.CompanyName + " " + o.ShippingAddress?.Street + " " + o.ShippingAddress?.Street1 + " " + o.ShippingAddress?.Street2,
                City = o.ShippingAddress.CityName,
                Country = o.ShippingAddress.CountryName,
                State = o.ShippingAddress.StateOrProvince,
                Zip = o.ShippingAddress.PostalCode,
                Name = o.ShippingAddress.FirstName + " " + o.ShippingAddress.LastName
            };

            return salesOrder;


        }






        public static List<eBayFBOrder> MapNewOrders(Config cfg, OrderTypeCollection orders)
        {
            var ret = new List<eBayFBOrder>();

            foreach (OrderType o in orders)
            {
                var x = new eBayFBOrder();
                x.eBayOrder = o;
                x.CustomerName = MapCustomerName(cfg, o);
                ret.Add(x);
            }

            return ret;
        }
        private static string MapCustomerName(Config cfg, OrderType o)
        {
            //return StringExtensions.Coalesce(cfg?.Store?.OrderSettings?.DefaultCustomer, o?.ShippingAddress.Name).Trim();
            return "Ebay Sale";
        }
    }

}