using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using ebay.FishbowlIntegration;
using eBay.FishbowlIntegration;
using eBay.FishbowlIntegration.Configuration;
using eBay.FishbowlIntegration.Models;

namespace eBayFBDesktop
{
    public class MainViewModel : BaseINPC
    {
        public MainViewModel()
        {
            this.Log = new StringBuilder();
            this.Cfg = Config.Load();
            this.bw = new BackgroundWorker();
            bw.DoWork += Bw_DoWork;

            bw.RunWorkerCompleted += Bw_RunWorkerCompleted;
        }

        private void Bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            PBBarShow = false;
            OnPropertyChanged("PBBarShow");
        }


        private void Bw_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                using (eBayIntegration oci = new eBayIntegration(this.Cfg))
                {
                    oci.OnLog += Oci_OnLog;
                    oci.Run();
                } 
            }
            catch(Exception ex)
            {
                DisplayException(ex);
            }

        }

        private void DisplayException(Exception ex)
        {
            Status = ex.Message;
            Log.AppendLine(ex.ToString());
            OnPropertyChanged("Status");
            OnPropertyChanged("StatusLog");
        }

        private void Oci_OnLog(string msg)
        {
            Status = msg;
            Log.AppendLine(msg);
            OnPropertyChanged("Status");
            OnPropertyChanged("StatusLog");
        }

        public BackgroundWorker bw;

        public Config Cfg { get; set; }
        public Boolean IsOrders { get { return Cfg.Actions.SyncOrders; } set { Cfg.Actions.SyncOrders = value; } }
        public Boolean IsInventory { get { return Cfg.Actions.SyncInventory; } set { Cfg.Actions.SyncInventory = value; } }
        public Boolean IsShipment { get { return Cfg.Actions.SyncShipments; } set { Cfg.Actions.SyncShipments = value; } }
        public Boolean IsProductWeights { get { return Cfg.Actions.SyncProductWeight; } set { Cfg.Actions.SyncProductWeight = value; } }
        public Boolean IsProductPrice { get { return Cfg.Actions.SyncProductPrice; } set { Cfg.Actions.SyncProductPrice = value; } }

        public ICommand Update => new RelayCommand(cmdUpdate);
        public ICommand SaveConfig => new RelayCommand(cmdSaveConfig);

        public ICommand RefreshMissingItems => new RelayCommand(cmdRefreshMissingItems);

        private void cmdRefreshMissingItems()
        {
            using (eBayIntegration oci = new eBayIntegration(this.Cfg))
            {
                MissingItemsData =  oci.ItemInFBEB();
                //OnPropertyChanged("MissingItems");
                OnPropertyChanged("MissingItemsData");
            }
        }

        private void cmdSaveConfig()
        {
            Config.Save(Cfg);
        }

        private void cmdUpdate()
        {
            PBBarShow = true;
            OnPropertyChanged("PBBarShow");

            if (!bw.IsBusy)
            {
                bw.RunWorkerAsync();
            }
        }

        public Boolean PBBarShow { get; set; }

        public String Status { get; set; }
        private StringBuilder Log { get; set; }
        public String StatusLog => Log.ToString();

        public List<String> MissingItems = new List<string>();

        public List<SimpleList> MissingItemsData { get; set; }


    }

    public abstract class BaseINPC : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class RelayCommand : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        private Action methodToExecute;
        private Func<bool> canExecuteEvaluator;
        public RelayCommand(Action methodToExecute, Func<bool> canExecuteEvaluator)
        {
            this.methodToExecute = methodToExecute;
            this.canExecuteEvaluator = canExecuteEvaluator;
        }
        public RelayCommand(Action methodToExecute)
            : this(methodToExecute, null)
        {
        }
        public bool CanExecute(object parameter)
        {
            if (this.canExecuteEvaluator == null)
            {
                return true;
            }
            else
            {
                bool result = this.canExecuteEvaluator.Invoke();
                return result;
            }
        }
        public void Execute(object parameter)
        {
            this.methodToExecute.Invoke();
        }
    }
}
