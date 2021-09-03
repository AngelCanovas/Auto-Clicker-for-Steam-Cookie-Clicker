using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Cookie_Clicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int sleepTimeMillis = 0;
        bool isAutoClickerStart = false;
        long totalClicks = 0;

        public MainWindow()
        {
            InitializeComponent();

        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void changeSleepTime(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.sleepTimeMillis = (int) e.NewValue;
            sleepMillis.Text = this.sleepTimeMillis.ToString();
        }

        private void startAutoClicker(object sender, RoutedEventArgs e)
        {
            if (this.isAutoClickerStart) return;

            this.isAutoClickerStart = true;
            Thread.Sleep(2000);

            while (this.isAutoClickerStart)
            {
                Mouse.Click(MouseButton.Left);
                Thread.Sleep(sleepTimeMillis);
            }

        }

        private void stopAutoClicker(object sender, RoutedEventArgs e)
        {
            this.isAutoClickerStart = false;
        }

        private void incrementTotalClicks(object sender, RoutedEventArgs e)
        {
            totalClicks++;
            sumClicks.Text = totalClicks.ToString();
        }
    }
}
