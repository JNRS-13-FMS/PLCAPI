using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;

namespace JNRSWebApiOwinHost
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = ConfigurationManager.AppSettings["baseAddress"];

            try
            {
                WebApp.Start<Startup>(url: baseAddress);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
            }

            Console.WriteLine(string.Format("Server started,monitored at {0}", baseAddress));

            Console.ReadLine(); 
        }
    }
}
