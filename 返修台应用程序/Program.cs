using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Xml;
using MyLog;
using MyLog.Init;

namespace 返修台应用程序
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
       
        static void Main()
        {
           bool createNew;
            XmlDocument xml = new XmlDocument();
            using (System.Threading.Mutex mutex = new System.Threading.Mutex(true, Application.ProductName, out createNew))
            {
                if (createNew)
                {
                    Log.LogKeepPeriod = LogExpired.Threemonthes;
                    LogEnviromentOperation.Instance.InitializeSetting();

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
                    AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
                    Logo fl = new Logo();
                    if (fl.ShowDialog() == DialogResult.OK)
                    {
                        Application.Run(new Form1());
                    }
                }
                else
                {
                    MessageBox.Show("应用程序已经在运行中!");
                    System.Threading.Thread.Sleep(1000);
                    System.Environment.Exit(1);
                }
            }
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Log.ErrLog.Error(e.Exception.Message);
            MessageBox.Show(e.Exception.Message);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.ErrLog.Error(e.ExceptionObject.ToString());
            MessageBox.Show(e.ExceptionObject.ToString());
        }
    }
}
