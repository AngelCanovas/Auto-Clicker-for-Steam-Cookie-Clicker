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
        int initialDelay = 2000;
        bool isAutoClickerStart = false;
        long totalClicks = 0;
        private AutoClicker clicker;
        Thread Clicker;

        public MainWindow()
        {
            InitializeComponent();

        }


        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        //This simulates a left mouse click
        public static void LeftMouseClick(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };
        public static Point GetMousePosition()
        {
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);

            return new Point(w32Mouse.X, w32Mouse.Y);
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
            Thread.Sleep(initialDelay);

            while (isAutoClickerStart)
            {
                doAClick();
                Thread.Sleep(sleepTimeMillis);
            }

            stopAutoClicker(null, null);
        }

        private void stopAutoClicker(object sender, RoutedEventArgs e)
        {
            this.isAutoClickerStart = false;
        }

        private void doAClick()
        {
            Point currentMousePosition = GetMousePosition();
            LeftMouseClick((int)currentMousePosition.X, (int)currentMousePosition.Y);
        }

        private void incrementTotalClicks(object sender, RoutedEventArgs e)
        {
            totalClicks++;
            sumClicks.Text = totalClicks.ToString();
        }
    }
}
