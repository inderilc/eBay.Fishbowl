using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ebay.FishbowlIntegration;
using eBay.FishbowlIntegration.Configuration;

namespace eBayFBConsole
{
    class Program
    {
        private static bool Debugger = true;
        private static bool FileLog = true;
        private static bool ConsoleLog = true;
        static void Main(string[] args)
        {


            try
            {
                var cfg = Config.Load();
                var ocf = new eBayIntegration(cfg);
                ocf.OnLog += Ocf_OnLog;
                ocf.Run();
                Config.Save(cfg);
            }
            catch (Exception ex)
            {
                ExceptionLog(ex);
            }

            if (Debugger)
            {
                Console.ReadLine();
                Process.Start("log.txt");
            }

        }

        private static void ExceptionLog(Exception exception)
        {
            String msg = exception.Message;
            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "consoleexception.txt", exception.ToString() + Environment.NewLine);
            Ocf_OnLog(msg);
        }

        private static void Ocf_OnLog(string msg)
        {
            String m = DateTime.Now.ToString() + " - " + msg;
            if (Debugger)
            {
                Debug.WriteLine(m);
            }
            if (FileLog)
            {
                File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "log.txt", m + Environment.NewLine);
            }
            if (ConsoleLog)
            {
                Console.WriteLine(m);
            }
        }
    }
}
