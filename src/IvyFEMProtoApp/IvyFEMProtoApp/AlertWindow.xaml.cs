using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IvyFEMProtoApp
{
    /// <summary>
    /// AlertWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AlertWindow : Window
    {
        public AlertWindow()
        {
            InitializeComponent();
        }

        public static void ShowDialog(string text, string title = "")
        {
            var win = new AlertWindow();
            win.Title = title;
            win.TextBox1.Text = text;
            win.ShowDialog();
        }
    }
}
