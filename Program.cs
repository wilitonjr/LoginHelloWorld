using Microsoft.Owin.Hosting;
using SQLSafeLoginPoc;
using System;
using System.Windows.Forms;

namespace SQLSafe.Login.Poc
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string baseAddress = "http://localhost:5000/";

            // Start OWIN host
            using (WebApp.Start<Startup>(url: baseAddress))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }
}
