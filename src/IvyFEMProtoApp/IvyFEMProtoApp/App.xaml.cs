using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace IvyFEMProtoApp
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {

        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Exception exception = e.Exception;
            string CRLF = "\r\n";
            string text = exception.ToString();
            /*Windowが起動しない
            var window = new AlertWindow();
            window.TextBox1.Text = text;
            */
            MessageBox.Show(text);

            System.Environment.Exit(-1);
        }
    }
}
