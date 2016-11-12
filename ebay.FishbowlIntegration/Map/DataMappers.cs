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

            salesOrder.CustomerPO = o.BuyerUserID.ToString();
            
            salesOrder.Salesman = cfg.Store.OrderSettings.Salesman;
            salesOrder.Carrier = MapCarrier(cfg, o.ShippingServiceSelected.ShippingService);
            
            salesOrder.LocationGroup = cfg.Store.OrderSettings.LocationGroup;
            salesOrder.FOB = cfg.Store.OrderSettings.ShipTerms;
            salesOrder.Status = OrderStatus;
            
            salesOrder.CustomerContact = "Ebay Sale";
            
            

            salesOrder.Items = MapItems(cfg, ord.eBayOrder.TransactionArray).ToList();
           
            //salesOrder.Items.Add(AddSubTotal(salesOrder.Items.First()));
        
            //salesOrder.Items.Add(AddDiscountMiscSale(o.TransactionArray.Transaction.SellerDiscounts, salesOrder.Items.First()));
            /*
            if (o.giftcert_discount > 0)
            {
                salesOrder.Items.Add(AddSubTotal(salesOrder.Items.First()));
                salesOrder.Items.Add(AddGiftCertificateDiscount(o.giftcert_discount, o.giftcert_ids, salesOrder.Items.First()));
            }
*/


            double ShippingCost = 0.00;
            if (ord.eBayOrder.ShippingServiceSelected.ShippingServiceCost!=null)
            {
                ShippingCost = ord.eBayOrder.ShippingServiceSelected.ShippingServiceCost.Value;
            }

            salesOrder.Items.Add(AddShipping(MapCarrier(cfg, o.ShippingServiceSelected.ShippingService), "Shipping", Math.Round(ShippingCost, 2), salesOrder.Items.First()));


            
            salesOrder.CustomFields = MapCustomFields(cfg, ord); 
            

            salesOrder.Ship = new ShipType
            {
                AddressField = (o.ShippingAddress?.CompanyName??"").Trim()+(" "+o.ShippingAddress?.Street??"").Trim()+(" "+ o.ShippingAddress?.Street1??"").Trim() + (" "+ o.ShippingAddress?.Street2??"").Trim(),
                City = o.ShippingAddress.CityName,
                Country = o.ShippingAddress.CountryName,
                State = o.ShippingAddress.StateOrProvince,
                Zip = o.ShippingAddress.PostalCode,
                Name = o.ShippingAddress.Name
            };

            salesOrder.BillTo = new BillType
            {
                AddressField = (o.ShippingAddress?.CompanyName ?? "").Trim() + (" " + o.ShippingAddress?.Street ?? "").Trim() + (" " + o.ShippingAddress?.Street1 ?? "").Trim() + (" " + o.ShippingAddress?.Street2 ?? "").Trim(),
                City = o.ShippingAddress.CityName,
                Country = o.ShippingAddress.CountryName,
                State = o.ShippingAddress.StateOrProvince,
                Zip = o.ShippingAddress.PostalCode,
                Name = o.ShippingAddress.Name
            };

            return salesOrder;


        }
        
        private static SalesOrderItem AddSubTotal(SalesOrderItem FirstLine)
        {
            return new SalesOrderItem()
            {
                ItemType = "40",
                TaxID = FirstLine.TaxID,
                TaxRate = FirstLine.TaxRate,
                TaxCode = FirstLine.TaxCode,
                Taxable = FirstLine.Taxable,
                TaxRateSpecified = FirstLine.TaxRateSpecified
            };
        }
        private static IEnumerable<SalesOrderItem> MapItems(Config cfg, TransactionTypeCollection items)
        {
            //return items.Select(i => MapSOItem(cfg, i));

            List<SalesOrderItem> ret = new List<SalesOrderItem>();
            foreach (TransactionType i in items)
            {
                ret.Add(MapSOItem(cfg, i));
            }
            return ret;
        }


        private static SalesOrderItem MapSOItem(Config cfg, TransactionType item)
        {
            //var info = p.extra_data.DeserializePHP();

            // Add GST 
            //if (item.Taxes)
                  //item.TransactionPrice.Value = Math.Round(item.TransactionPrice.Value + (item.TransactionPrice.Value * .1), 2, MidpointRounding.AwayFromZero);
            //
            return new SalesOrderItem
            {
                Quantity = (double)item.QuantityPurchased,
                ProductNumber = item.Variation?.SKU ?? item.Item.SKU,
                ProductPrice = item.TransactionPrice.Value,
                TotalPrice = item.QuantityPurchased* item.TransactionPrice.Value,
                SOID = "-1",
                ID = "-1",
                ItemType = "10",
                Status = "10",
                ProductPriceSpecified = true,
                Taxable = true,
                TaxCode = cfg.Store.OrderSettings.TaxName,
                TaxRate = cfg.Store.OrderSettings.TaxRate,
                TaxRateSpecified = true,
                UOMCode = "ea"
            };
        }






        private static List<CustomField> MapCustomFields(Config cfg, eBayFBOrder ord)
        {
            List<CustomField> ret = new List<CustomField>();

            //Notes to Buyer needs to be mapped to custom field "Delivery Instructions", ord.OrderID needs to be mapped to “Ebay Record No”, 
            ret.Add(new CustomField()
            {
                Name = "Ebay Record No",
                Type = "CFT_LONG_TEXT",
                Info = ord.eBayOrder.OrderID.ToString()
            });

            ret.Add(new CustomField()
            {
                Name = "Requested Shipping",
                Type = "CFT_LONG_TEXT",
                Info = MapCarrier(cfg, ord.eBayOrder.ShippingServiceSelected.ShippingService)
            });

            ret.Add(new CustomField()
            {
                Name = "Delivery Instructions:",
                Type = "CFT_LONG_TEXT",
                Info = ord.eBayOrder?.BuyerCheckoutMessage??""
            });

            


            return ret;
        }
        
        private static string MapCarrier(Config cfg, string shipping)
        {
            var dict = cfg.Store.OrderSettings.CarrierSearchNames;

            foreach (var i in dict)
            {
                bool found = shipping.ToUpper().Equals(i.Key.ToUpper());
                if (found)
                {
                    return i.Value;
                }
            }

            return cfg.Store.OrderSettings.DefaultCarrier;
        }

    
        private static SalesOrderItem AddShipping(string shipcode, string desc, Double shippingAmount, SalesOrderItem FirstLine)
        {

            


            //shippingAmount = Math.Round(shippingAmount * 1.1, 2);

            return new SalesOrderItem()
            {
                ItemType = "60",
                ProductNumber = shipcode,
                Description = desc,
                Quantity = 1,

                ProductPrice = shippingAmount,
                ProductPriceSpecified = true,

                TotalPrice = shippingAmount,
                TotalPriceSpecified = true,

                TaxID = FirstLine.TaxID,
                TaxRate = FirstLine.TaxRate,
                TaxCode = FirstLine.TaxCode,
                Taxable = false,
                TaxRateSpecified = FirstLine.TaxRateSpecified,
                UOMCode = "ea"
            };
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