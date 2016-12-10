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
using FishbowlSDK;
using FishbowlSDK.Extensions;

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
            this.fb = new FishbowlController(this.cfg);
            context = new ApiContext();
            this.context.ApiCredential.eBayToken = cfg.Store.ApiToken;
            this.context.SoapApiServerUrl = cfg.Store.StoreUrl;
            context.Version = "983";
            context.Site = SiteCodeType.Australia;
            context.ApiLogManager = new ApiLogManager();
            context.ApiLogManager.ApiLoggerList.Add(new FileLogger("log.txt", true, true, true));
            context.ApiLogManager.EnableLogging = false;

        }
        
        public OrderTypeCollection allCompletedOrders(String LastOrderDownload)
        {
            OrderTypeCollection orders=new OrderTypeCollection();
            OrderTypeCollection retOrders = new OrderTypeCollection();
            DateTime dateOut;
            DateTime.TryParse(LastOrderDownload, out dateOut);
            try
            {
             
                GetOrdersCall getOrders = new GetOrdersCall(context);
                getOrders.DetailLevelList = new DetailLevelCodeTypeCollection();
                getOrders.DetailLevelList.Add(DetailLevelCodeType.ReturnAll);

                
                getOrders.CreateTimeFrom = dateOut.AddDays(-15.0);  //According to Zane, typical orders are paid within 3-4 days, but we would give 15 days window (thats when eBay cancells NonPaid orders automatically). We return only orders having PaidTime since our last timestamp.

                

                getOrders.CreateTimeTo = DateTime.Now;
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

            foreach (OrderType o in orders)
            {
                if (o.PaidTime.ToLocalTime() > dateOut)
                {
                    retOrders.Add(o);
                }
            }
            return retOrders;
        }
        
        public bool UpdateOrderStatusComplete(int orderid)
        {
            

            //need to be implemented
            
            return true;
        }

        public bool UpdateShipmentStatus(String orderid, String TrackingNum, String CarrierName)
        {

            String []ids = orderid.Split('-');
            if (ids != null && ids.Length == 2)
            {
                CompleteSaleCall api = new CompleteSaleCall();
                api.ApiContext = context;
                api.ItemID = ids[0];
                api.TransactionID = ids[1];
                api.Paid = true;
                api.Shipped = true;

                api.Shipment = new ShipmentType();
                api.Shipment.ShipmentTrackingDetails = new ShipmentTrackingDetailsTypeCollection();

                ShipmentTrackingDetailsType shpmnt = new ShipmentTrackingDetailsType();
                shpmnt.ShipmentTrackingNumber = TrackingNum;
                shpmnt.ShippingCarrierUsed = CarrierName;

                api.Shipment.ShipmentTrackingDetails.Add(shpmnt);
                api.Shipment.ShippedTime = DateTime.Now;

                //call the Execute method
                api.Execute();
                return (api.ApiResponse.Ack != AckCodeType.Failure);
            }
            return false;
        }

        public ItemTypeCollection GetInventory()
        {
  
            ItemTypeCollection ret = new ItemTypeCollection();


            GetMyeBaySellingCall oGetMyeBaySellingCall = new GetMyeBaySellingCall(context);

            oGetMyeBaySellingCall.ActiveList = new ItemListCustomizationType();
            //oGetMyeBaySellingCall.ActiveList.Pagination = new PaginationType();
            //oGetMyeBaySellingCall.ActiveList.Pagination.EntriesPerPage = 50;
            //oGetMyeBaySellingCall.ActiveList.Pagination.EntriesPerPageSpecified = true;
            //oGetMyeBaySellingCall.ActiveList.Pagination.PageNumber = 1;
            //oGetMyeBaySellingCall.ActiveList.Pagination.PageNumberSpecified = true;
            //oGetMyeBaySellingCall.ActiveList.Sort = ItemSortTypeCodeType.StartTime;
            //oGetMyeBaySellingCall.ActiveList.SortSpecified = true;

            try
            {
                oGetMyeBaySellingCall.Execute();
                foreach (ItemType oItem in oGetMyeBaySellingCall.ActiveListReturn.ItemArray)
                {

                    if (oItem.Variations?.Variation.Count > 0)
                    {
                        foreach (VariationType vr in oItem.Variations.Variation)
                        {
                            ItemType i = new ItemType();
                            i.ItemID = oItem.ItemID;
                            i.SKU = vr.SKU;
                            i.Quantity = vr.Quantity-vr.SellingStatus.QuantitySold;
                            i.BuyItNowPrice = vr.StartPrice;
                            ret.Add(i);
                        }
                    }
                    else
                    {
                        ItemType i = new ItemType();
                        i.ItemID = oItem.ItemID;
                        i.SKU = oItem.SKU;
                        i.Quantity = oItem.QuantityAvailable;
                        i.BuyItNowPrice = oItem.BuyItNowPrice;
                        ret.Add(i);
                    }
                }
            }
            catch (ApiException oApiEx)
            {
                Console.WriteLine(oApiEx.Message);
            }
            catch (SdkException oSdkEx)
            {
                Console.WriteLine(oSdkEx.Message);

            }
            catch (Exception oEx)
            {
                Console.WriteLine(oEx.Message);
            }

            return ret;
        }

        public bool GroupUpdateProductInventory(List<ItemType> group)
        {
            ReviseInventoryStatusCall ris = new ReviseInventoryStatusCall(context);
            ris.InventoryStatuList = new InventoryStatusTypeCollection();
            foreach (var item in group)
            {
                InventoryStatusType InvStatus = new InventoryStatusType();

                /*
                if (item.ItemID == "281938060209")
                {
                    string a = "1";
                    InvStatus.ItemID = item.ItemID;
                    InvStatus.SKU = item.SKU;
                    InvStatus.Quantity = item.Quantity;
                    ris.InventoryStatuList.Add(InvStatus);
                }*/

                
                InvStatus.ItemID = item.ItemID;
                InvStatus.SKU = item.SKU;
                InvStatus.Quantity = item.Quantity;
                ris.InventoryStatuList.Add(InvStatus);
                

            }

            ris.Execute();
            return (ris.ApiResponse.Ack != AckCodeType.Failure);
        }

       
        public ItemTypeCollection GetProductPrice()
        {
            return GetInventory();
        }

        public ItemTypeCollection GetProductWeight()
        {
            
            return GetInventory();
        }

        public bool UpdateProductWeight(string eBayID, string sku, decimal weight)
        {
            
            ReviseFixedPriceItemCall reviseFP = new ReviseFixedPriceItemCall(context);
            ItemType item = new ItemType();
            item.ItemID = eBayID;
            item.SKU = sku;
            item.ShippingDetails.CalculatedShippingRate.WeightMajor.Value = weight;
            
            reviseFP.Item = item;
            reviseFP.Execute();

            return (reviseFP.ApiResponse.Ack != AckCodeType.Failure);
        }

        public bool GroupUpdateProductPrice(List<ItemType> group)
        {
            ReviseInventoryStatusCall ris = new ReviseInventoryStatusCall(context);
            ris.InventoryStatuList = new InventoryStatusTypeCollection();
            foreach (var item in group)
            {
                InventoryStatusType InvStatus = new InventoryStatusType();
                InvStatus.ItemID = item.ItemID;
                InvStatus.SKU = item.SKU;
                InvStatus.StartPrice = item.BuyItNowPrice;
                ris.InventoryStatuList.Add(InvStatus);
            }

            ris.Execute();
            return (ris.ApiResponse.Ack != AckCodeType.Failure);
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
                                eBay.Service.Core.Soap.AddressType address = order.ShippingAddress;
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
