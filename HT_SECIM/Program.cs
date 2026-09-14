using HT_SECIM.Core;
using System;
using System.Threading;
using System.Windows.Forms;

namespace HT_SECIM
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Yayin sirasinda beklenmedik bir hata uygulamayi kapatmasin, log'a dussun.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Application.Run(new Form1());
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            CLog.Error("UI THREAD HATASI", e.Exception);
            MessageBox.Show(e.Exception.Message, "Hata",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            CLog.Error("YAKALANMAMIS HATA",
                e.ExceptionObject == null ? "" : e.ExceptionObject.ToString());
        }
    }
}
