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

        public void Log(String msg)
        {
            if (OnLog != null)
            {
                OnLog(msg);
            }
        }
    }
}
