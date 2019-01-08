using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SerialPortTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private TM2430 _tm2430 = new TM2430();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _tm2430.GetResultStyleI();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var _tm2430config = new List<TM2430Config>();
            for (int i = 0; i < 3; i++)
            {
                TM2430Config tM2430Config = new TM2430Config()
                {
                    Time = 17 + i,
                    Interval = 5 + i*5,
                };
                _tm2430config.Add(tM2430Config);
            }
            _tm2430.SetMode(_tm2430config);
        }
    }
}
