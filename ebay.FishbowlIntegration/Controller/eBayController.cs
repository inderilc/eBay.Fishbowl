using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eBay.FishbowlIntegration.Configuration;
using eBay.Service.Call;
using eBay.Service.Core.Sdk;
using eBay.Service.Core.Soap;
using eBay.Service;
using eBay.Service.Util;
using eBay.FishbowlIntegration.Models;
using eBay.FishbowlIntegration.Map;
using eBay.FishbowlIntegration.Controller;

namespace ebay.FishbowlIntegration.Controller
{
    public class eBayController
    {
        private Config cfg;
        private ApiContext context;
        public event LogMsg OnLog;
        public delegate void LogMsg(String msg);
        private FishbowlController fb { get; set; }
        public eBayController(Config cfg)
        {
            this.cfg = cfg;
            context = new ApiContext();
            this.context.ApiCredential.eBayToken = cfg.Store.ApiToken;
            this.context.SoapApiServerUrl = cfg.Store.StoreUrl;
            context.Version = "981";
            context.Site = SiteCodeType.Australia;
            context.ApiLogManager = new ApiLogManager();
            context.ApiLogManager.ApiLoggerList.Add(new FileLogger("log.txt", true, true, true));
            context.ApiLogManager.EnableLogging = true;

        }
        public void DownloadOrders()
        {
            Log("Downloading Orders");
            OrderTypeCollection orders = allCompletedOrders();
            Log("Orders Downloaded: " + orders.Count);
            if (orders.Count > 0)
            {
                List<eBayFBOrder> ofOrders = DataMappers.MapNewOrders(cfg, orders);

                Log("Validating Items in Fishbowl.");
                ValidateItems(ofOrders);
                Log("Items Validated");

                Log("Creating Sales Orders Data.");
                ValidateOrder(ofOrders, (Queue.Equals("P") ? "20" : "10 "));
                Log("Finished Creating Sales Order Data.");


            }
                string s = "1";
        }

        private void ValidateOrder(List<eBayFBOrder> ofOrders, String OrderStatus)
        {
            foreach (var o in ofOrders)
            {
                o.FbOrder = DataMappers.MapSalesOrder(cfg, o, OrderStatus);
            }
        }
        

        private void ValidateItems(List<eBayFBOrder> ofOrders)
        {
       
            var fbProds = fb.GetAllProducts();
            foreach (var i in ofOrders)
            {
                //var list = i.eBayOrder.Items.Select(x => x.productcode);
                var ta = i.eBayOrder.TransactionArray;
                List<String> prods = new List<String>();
               
                foreach (TransactionType t in ta)
                {
                    prods.Add(t.Item.SKU);
                }
                
                var except = prods.Except(fbProds);
                if (except.Any())
                {
                    throw new Exception($"Products Not Found on Order [{i.eBayOrder.OrderID}] Please Create Them: " + String.Join(",", except));
                }
            }
        }

    

        private OrderTypeCollection allCompletedOrders()
        {
            OrderTypeCollection orders=new OrderTypeCollection();
            try
            {
                GetOrdersCall getOrders = new GetOrdersCall(context);
                getOrders.DetailLevelList = new DetailLevelCodeTypeCollection();
                getOrders.DetailLevelList.Add(DetailLevelCodeType.ReturnAll);
                
                getOrders.CreateTimeFrom = new DateTime(2016, 11, 9);
                getOrders.CreateTimeTo = new DateTime(2016, 11, 10);
                getOrders.OrderRole = TradingRoleCodeType.Seller;
                getOrders.OrderStatus = OrderStatusCodeType.Completed;

                getOrders.Execute();
                
                if (getOrders.ApiResponse.Ack != AckCodeType.Failure)
                {
                    //Check if any orders are returned
                    if (getOrders.ApiResponse.OrderArray.Count != 0)
                    {
                        orders = getOrders.ApiResponse.OrderArray;
                    }
                }
            }
            catch (Exception ex)
            {
               
            }
            return orders;
        }

        private void fetchOrders()
        {
            //this is what we got from Ravi...Leaving it here for reference only...we will get rid of it eventually.

            try
            {
                //create the context
                ApiContext context = new ApiContext();
                //set the User token
                context.ApiCredential.eBayToken = cfg.Store.ApiToken;

                //set the server url
                context.SoapApiServerUrl = cfg.Store.StoreUrl;
 
            //enable logging
            context.ApiLogManager = new ApiLogManager();
                context.ApiLogManager.ApiLoggerList.Add(new FileLogger("log.txt", true, true, true));
                context.ApiLogManager.EnableLogging = true;
                //set the version
                context.Version = "817";
                context.Site = SiteCodeType.Australia;
                bool blnHasMore = true;
                DateTime CreateTimeFromPrev, CreateTimeFrom, CreateTimeTo;
                GetOrdersCall getOrders = new GetOrdersCall(context);
                getOrders.DetailLevelList = new DetailLevelCodeTypeCollection();
                getOrders.DetailLevelList.Add(DetailLevelCodeType.ReturnAll);

                //CreateTimeTo set to the current time
                CreateTimeTo = DateTime.Now.ToUniversalTime();
                //Assumption call is made every 15 sec. So CreateTimeFrom of last call was 15 mins
                //prior to now
                TimeSpan ts1 = new TimeSpan(9000000000);
                CreateTimeFromPrev = CreateTimeTo.Subtract(ts1);

                //Set the CreateTimeFrom the last time you made the call minus 2 minutes
                TimeSpan ts2 = new TimeSpan(1200000000);
                CreateTimeFrom = CreateTimeFromPrev.Subtract(ts2);
                getOrders.CreateTimeFrom = new DateTime(2016, 11, 9);
                getOrders.CreateTimeTo = new DateTime(2016, 11, 10);
                getOrders.Execute();

                if (getOrders.ApiResponse.Ack != AckCodeType.Failure)
                {
                    //Check if any orders are returned
                    if (getOrders.ApiResponse.OrderArray.Count != 0)
                    {
                        foreach (OrderType order in getOrders.ApiResponse.OrderArray)
                        {
                            //Update your system with the order information.
                            Console.WriteLine("Order Number: " + order.OrderID);
                            Console.WriteLine("OrderStatus: " + order.OrderStatus);
                            Console.WriteLine("Order Created On: " + order.CreatedTime);

                            //Get Order Details
                            TransactionTypeCollection orderTrans = order.TransactionArray;

                            //Order could be comprised of one or more items
                            foreach (TransactionType transaction in orderTrans)
                            {
                                Console.WriteLine("Order for: " + transaction.Item.ItemID + ", " + transaction.Item.SKU + ", " + transaction.Item.Title);

                                //If you are listing variation items, you will need to retrieve the variation
                                //details as chosen by the buyer
                                if (transaction.Variation.SKU != null)
                                {
                                    Console.WriteLine("Variation: " + transaction.Variation.SKU);
                                }
                                Console.WriteLine("OrderLineItemID: " + transaction.OrderLineItemID);
                                Console.WriteLine("Qty Purchased: " + transaction.QuantityPurchased);
                                Console.WriteLine("Buyer Info: " + order.BuyerUserID + ", " + transaction.Buyer.Email);
                            }

                            if (order.CheckoutStatus.Status == CompleteStatusCodeType.Complete)
                            {
                                //Get Payment Details
                                Console.WriteLine("Order Adjustment Amount: " + order.AdjustmentAmount.Value);
                                Console.WriteLine("Order Amount Paid: " + order.AmountPaid.Value);
                                Console.WriteLine("Payment Method: " + order.CheckoutStatus.PaymentMethod);
                                ExternalTransactionTypeCollection extTrans = order.ExternalTransaction;
                                foreach (ExternalTransactionType extTran in extTrans)
                                {
                                    Console.WriteLine("External TransactionID: " + extTran.ExternalTransactionID);
                                    Console.WriteLine("External Transaction Time: " + extTran.ExternalTransactionTime);
                                    Console.WriteLine("Payment/Refund Amount: " + extTran.PaymentOrRefundAmount.Value);
                                }

                                //Get shipping information
                                ShippingServiceOptionsType shipping;
                                shipping = order.ShippingServiceSelected;
                                Console.WriteLine("Shipping Service Selected: " + order.ShippingServiceSelected.ShippingService);

                                //Get Shipping Address - Address subject to change if the buyer has not completed checkout                      
                                AddressType address = order.ShippingAddress;
                                StringBuilder sAdd = new StringBuilder();
                                sAdd = sAdd.Append(address.Name);
                                if (address.Street != null && address.Street != "")
                                    sAdd.Append(", " + address.Street);

                                if (address.Street1 != null && address.Street1 != "")
                                    sAdd.Append(", " + address.Street1);

                                if (address.Street2 != null && address.Street2 != "")
                                    sAdd.Append(", " + address.Street2);

                                if (address.CityName != null && address.CityName != "")
                                    sAdd.Append(", " + address.CityName);

                                if (address.StateOrProvince != null && address.StateOrProvince != "")
                                    sAdd.Append(", " + address.StateOrProvince);

                                if (address.PostalCode != null && address.PostalCode != "")
                                    sAdd.Append(", " + address.PostalCode);

                                if (address.CountryName != null && address.CountryName != "")
                                    sAdd.Append(", " + address.CountryName);

                                if (address.Phone != null && address.Phone != "")
                                    sAdd.Append(": Phone" + address.Phone);

                                Console.WriteLine("Shipping Address: " + sAdd);
                                double salesTax;

                                //Get the sales tax
                                if (order.ShippingDetails.SalesTax.SalesTaxAmount == null)
                                    salesTax = 0.00;
                                else
                                    salesTax = order.ShippingDetails.SalesTax.SalesTaxAmount.Value;

                                Console.WriteLine("Sales Tax: " + salesTax);
                                if (order.BuyerCheckoutMessage != null)
                                {
                                    Console.WriteLine("Buyer Checkout Message: " + order.BuyerCheckoutMessage);
                                }
                            }
                            Console.WriteLine("********************************************************");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No Order available");
                        Console.WriteLine("TotalNumberOfPages: " + getOrders.ApiResponse.PaginationResult.TotalNumberOfPages.ToString());
                        Console.WriteLine("TotalNumberOfEntries: " + getOrders.ApiResponse.PaginationResult.TotalNumberOfEntries.ToString());
                        Console.WriteLine("********************************************************");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);

            }
        }

        public void Log(String msg)
        {
            if (OnLog != null)
            {
                OnLog(msg);
            }
        }
    }
}
