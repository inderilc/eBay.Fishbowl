using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eBay.Service.Call;
using eBay.Service.Core.Sdk;
using eBay.Service.Core.Soap;
using eBay.Service;
using eBay.FishbowlIntegration.Controller;
using ebay.FishbowlIntegration.Controller;
using eBay.FishbowlIntegration.Configuration;
using eBay.FishbowlIntegration.Models;
using eBay.FishbowlIntegration.Map;
using FishbowlSDK;
using FishbowlSDK.Extensions;

using System.IO;

namespace ebay.FishbowlIntegration
{
    public class eBayIntegration : IDisposable
    {
        public event LogMsg OnLog;
        public delegate void LogMsg(String msg);

        private Config cfg { get; set; }
        private FishbowlController fb { get; set; }
        private eBayController ebc { get; set; }

        public eBayIntegration(Config cfg)
        {
            this.cfg = cfg;
            if (ebc == null)
            {
                ebc = new eBayController(cfg);
            }
            if (fb == null)
            {
                fb = new FishbowlController(cfg);
            }
        }
        public void Run()
        {

            Log("Starting Integration");
            InitConnections();
            Log("Ready");
            if (cfg.Actions.SyncOrders)
                DownloadOrders();

            if (cfg.Actions.SyncInventory)
                UpdateInventory();

            if (cfg.Actions.SyncShipments)
                UpdateShipments();

            if (cfg.Actions.SyncProductPrice)
                UpdateProductPrice();

            if (cfg.Actions.SyncProductWeight)
                UpdateProductWeight();

        }
        public void DownloadOrders()
        {
            Log("Downloading Orders");
            OrderTypeCollection orders = ebc.allCompletedOrders(cfg.Store.SyncOrder.LastDownloads.ToString());
            Log("Orders Downloaded: " + orders.Count);
            if (orders.Count > 0)
            {
                List<eBayFBOrder> ofOrders = DataMappers.MapNewOrders(cfg, orders);

                Log("Validating Items in Fishbowl.");
                ValidateItems(ofOrders);
                Log("Items Validated");


                Log("Creating Sales Orders Data.");
                ValidateOrder(ofOrders, "20");
                Log("Finished Creating Sales Order Data.");

                Log("Validate Carriers");
                ValidateCarriers(ofOrders);
                Log("Carriers Validated");

                //Log("Kit Items");
                // ValidateKits(ofOrders);
                // Log("Finished Kits.");
                
                var ret = CreateSalesOrders(ofOrders);

                Log("Result: " + String.Join(Environment.NewLine, ret));
                cfg.Store.SyncOrder.LastDownloads = DateTime.Now;
                Config.Save(cfg);
                Log("Downloading Orders Finished");

            }

        }

        private List<String> CreateSalesOrders(List<eBayFBOrder> ofOrders)
        {
            var ret = new List<String>();

            foreach (var o in ofOrders)
            {
                String soNum;

                bool soExists = fb.CheckSoExists(o.eBayOrder.OrderID.ToString());

                if (!soExists)
                {
                    String msg = "";
                    Double ordertotal;
                    var result = fb.SaveSalesOrder(o.FbOrder, out soNum, out msg, out ordertotal);

                    //xc.UpdateXC2FBDownloaded(o.XCOrder.orderid, soNum);

                    try
                    {
                        if (result && o.FbOrder.Status.Equals("20")) // Only apply payments on Issued Orders.
                        {
                            DateTime dtPayment;
                            bool dtParsed = DateTime.TryParse(o.eBayOrder.CreatedTime.Date.ToString(), out dtPayment);

                            var payment = fb.MakePayment(soNum, o.eBayOrder.CheckoutStatus.PaymentMethod.ToString(), ordertotal, cfg.Store.SyncOrder.PaymentMethodsToAccounts, (dtParsed ? dtPayment : DateTime.Now)); // Use the Generated Order Total, and Payment Date
                            ret.Add(payment);
                        }
                        else
                        {
                            ret.Add(msg);
                        }
                    }
                    catch (Exception ex)
                    {
                        ret.Add("Error With Payment. " + ex.Message);
                    }
                }
                else
                {
                    ret.Add("SO Exists.");
                }


                //cfg.Store.SyncOrder.OrderQueue[QueueID] = o.eBayOrder.OrderID;

                Config.Save(cfg);
            }

            return ret;
        }
        private void ValidateKits(List<eBayFBOrder> ofOrders)
        {
            foreach (var o in ofOrders)
            {
                FindUpdateKits(o.FbOrder);
            }
        }

        private void FindUpdateKits(SalesOrder so)
        {
            var originalItems = new List<SalesOrderItem>(so.Items);
            so.Items.Clear();
            foreach (var i in originalItems)
            {
                so.AddItem(fb.api, i);
            }
        }

        private void ValidateCarriers(List<eBayFBOrder> ofOrders)
        {
            // Do nothing.
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
                    prods.Add(t.Variation?.SKU ?? t.Item.SKU);
                }

                var except = prods.Except(fbProds);
                if (except.Any())
                {
                    throw new Exception($"Products Not Found on Order [{i.eBayOrder.OrderID}] Please Create Them: " + String.Join(",", except));
                }
            }
        }

        public List<SimpleList> ItemInFBEB()
        {
            InitConnections();
            ItemTypeCollection eBayProducts = ebc.GetInventory();
            var fbProducts = fb.GetInventory();

            var allItems = new List<String>();

            foreach (ItemType itm in eBayProducts)
            {
                allItems.Add(itm.SKU);
            }

            allItems.AddRange(fbProducts.Keys);

            var distinctProducts = allItems.Distinct();

            List<SimpleList> ret = new List<SimpleList>();

            ret.AddRange(distinctProducts.Select(k => new SimpleList() { Name = k }));

            foreach (var x in ret)
            {
                foreach (ItemType item in eBayProducts)
                {
                    if (item.SKU == x.Name)
                        x.InEB = true;
                }

                x.InFB = fbProducts.ContainsKey(x.Name);
            }

            return ret;
        }

        private void UpdateProductWeight()
        {
            Log("Updating Product Weight");

            ItemTypeCollection eBayProducts = ebc.GetProductWeight();

            var fbProducts = fb.GetProductWeight();

            var toUpdate = new ItemTypeCollection();
            foreach (ItemType kvp in eBayProducts)
            {
                if (fbProducts.ContainsKey(kvp.SKU))
                {
                    double dbl = fbProducts[kvp.SKU];
                    if (!dbl.Equals(kvp.ShippingDetails.CalculatedShippingRate.WeightMajor.Value)) //change of logic needed here
                    {
                     toUpdate.Add(new ItemType() { ItemID = kvp.ItemID, SKU = kvp.SKU, ShippingDetails = new ShippingDetailsType() { CalculatedShippingRate = new CalculatedShippingRateType() { WeightMajor= new MeasureType() { Value = Convert.ToDecimal(dbl) } } } });
                    }
                }
            }
            if (toUpdate.Count > 0)
            {
                Log("Updating Product Weight: " + toUpdate.Count);
                foreach (ItemType i in toUpdate)
                {
                    String sql = "";
                    var updated = ebc.UpdateProductWeight(i.ItemID, i.SKU, i.ShippingDetails.CalculatedShippingRate.WeightMajor.Value); 
                    Log("SQL: " + sql);
                    if (updated)
                    {
                        Log($"Sku/Variant/Productcode: [{i.SKU}] Weight: [{i.ShippingDetails.CalculatedShippingRate.WeightMajor.Value}] OK");
                    }
                    else
                    {
                        Log($"Sku/Variant/Productcode: [{i.SKU}] Weight: [{i.ShippingDetails.CalculatedShippingRate.WeightMajor.Value}] FAILED");
                    }
                }
            }
            else
            {
                Log("No Products to Update!");
            }

            Log("Product Price Finished");
        }

        private void UpdateProductPrice()
        {
            Log("Updating Product Price");

            var eBayProducts = ebc.GetProductPrice();

            var fbProducts = fb.GetProductPrice();

            var toUpdate = new ItemTypeCollection();
            foreach (ItemType kvp in eBayProducts)
            {
                if (fbProducts.ContainsKey(kvp.SKU))
                {
                    var dbl = fbProducts[kvp.SKU];
                    if (dbl != kvp.BuyItNowPrice.Value)
                    {
                        toUpdate.Add(new ItemType() { ItemID = kvp.ItemID, SKU = kvp.SKU, BuyItNowPrice = new AmountType() { Value=dbl} });
                    }
                }
            }
            if (toUpdate.Count > 0)
            {
                Log("Updating Product Price: " + toUpdate.Count);
                foreach (ItemType i in toUpdate)
                {
                    String sql = "";
                    var updated = ebc.UpdateProductPrice(i.ItemID,i.SKU,i.BuyItNowPrice.Value);
                    Log("SQL: " + sql);
                    if (updated)
                    {
                        Log($"Sku/Variant/Productcode: [{i.SKU}] Price: [{i.BuyItNowPrice.Value}] OK");
                    }
                    else
                    {
                        Log($"Sku/Variant/Productcode: [{i.SKU}] Price: [{i.BuyItNowPrice.Value}] FAILED");
                    }
                }
            }
            else
            {
                Log("No Products to Update!");
            }

            Log("Product Price Finished");
        }

        public void UpdateInventory()
        {
            Log("Updating Inventory");

            ItemTypeCollection eBayProducts = ebc.GetInventory();

            var fbProducts = fb.GetInventory();

            var toUpdate = new ItemTypeCollection();
            foreach (ItemType kvp in eBayProducts)
            {
                if (fbProducts.ContainsKey(kvp.SKU))
                {
                    var dbl = fbProducts[kvp.SKU];
                    if (dbl != kvp.Quantity)
                    {
                        toUpdate.Add(new ItemType(){ ItemID = kvp.ItemID, SKU = kvp.SKU, Quantity = Convert.ToInt32(dbl)});
                    }
                }
            }
            if (toUpdate.Count > 0)
            {
                Log("Updating Inventory: " + toUpdate.Count);
                foreach (ItemType i in toUpdate)
                {
                    var updated = ebc.UpdateProductInventory(i.ItemID,i.SKU,i.Quantity);
                    if (updated)
                    {
                        Log($"Sku/Variant/Productcode: [{i.SKU}] Qty: [{i.Quantity}] OK");
                    }
                    else
                    {
                        Log($"Sku/Variant/Productcode: [{i.SKU}] Qty: [{i.Quantity}] FAILED");
                    }
                }
            }
            else
            {
                Log("No Inventory to Update!");
            }

            Log("Inventory Update Finished");
        }

        private void UpdateShipments()
        {
            Log("Updating Shipments.");
            var shipments = fb.GetShipments(cfg.Store.SyncOrder.LastShipments);
            Log("Orders: " + shipments.Count);
            foreach (var s in shipments)
            {

                String orderid = s.ORDERNUM.ToString();
                if (!String.IsNullOrEmpty(orderid))
                {
                    bool updated = ebc.UpdateShipmentStatus(orderid, s.TRACKINGNUM, s.CARRIERNAME);
                    if (updated)
                    {
                        Log($"Updated Order [{s.SONUM}] / [{s.CPO}] / [{s.ORDERNUM}] with Tracking : [{s.TRACKINGNUM}]");
                    }
                    else
                    {
                        Log($"UNABLE TO UPDATE Order [{s.SONUM}] / [{s.CPO}] / [{s.ORDERNUM}] with Tracking : [{s.TRACKINGNUM}]");
                    }

                }
                else
                {
                    Log($"Skipping Order [{s.SONUM}] Customer PO [{s.CPO}] to mark ship, possibly not a Cart Order.");
                }
            }
            cfg.Store.SyncOrder.LastShipments = DateTime.Now;
            Config.Save(cfg);
        }



        public void Log(String msg)
        {
            if (OnLog != null)
            {
                OnLog(msg);
            }
        }

        private void InitConnections()
        {
            if (fb == null)
            {
                Log("Connecting to Fishbowl");
                fb = new FishbowlController(cfg);
            }

            if (ebc == null)
            {
                Log("Connecting to eBay");
                ebc = new eBayController(cfg);
            }
        }

        private void LogException(Exception ex)
        {
            String msg = ex.Message;
            Log(msg);
            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "exception.txt", ex.ToString() + Environment.NewLine);
        }


        public void Dispose()
        {
            if (fb != null)
                fb.Dispose();

        }

    }
}
