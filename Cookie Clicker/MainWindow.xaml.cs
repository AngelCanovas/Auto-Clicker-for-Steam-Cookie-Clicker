using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Cookie_Clicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int SleepTimeMillis { get; set; }
        public int InitialDelay { get; set; }
        public bool IsFixPosition { get; set; }
        public double XPosition { get; set; }
        public double YPosition { get; set; }
        public int BarrelRollDelay { get; set; }
        public int BarrelRollRadius { get; set; }
        public bool IsBarrelRollCheck { get; set; }
        public bool IsGoldenScanCheck { get; set; }
        public int GoldenScanDelay { get; set; }

        bool canAutoClickerStart = false;
        bool canSetGoldenCookiePosition = false;
        bool canSetGoldenScan = false;
        bool toggleAutoClickerState = false;

        private int rotationSteps = 36 * 2;
        private int rotationCliksPerStep = 4;
        private int rotationClickDelay = 10;

        private IntPtr _windowHandle;
        private HwndSource _source;
        private DispatcherTimer dispatcherTimer;
        private DispatcherTimer goldenDispatcherTimer;

        private const int HOTKEY_ID = 9000;
        private const uint MOD_NONE = 0x0000; // (none)
        private const uint MOD_ALT = 0x0001; //ALT
        private const uint MOD_CONTROL = 0x0002; //CTRL
        private const uint MOD_SHIFT = 0x0004; //SHIFT
        private const uint MOD_WIN = 0x0008; //WINDOWS
        public const int MOUSEEVENTF_LEFTDOWN = 0x02; // Mouse left click down
        public const int MOUSEEVENTF_LEFTUP = 0x04; // Mouse left click up

        Thread Clicker;
        bool isKeyToggleAllowed = false;
        Key toggleKey = Key.RightCtrl; // default key Right Control

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        // MAIN
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            SleepTimeMillis = 20;
            InitialDelay = 200;
            IsFixPosition = false;
            XPosition = 290;
            YPosition = 420;
            BarrelRollDelay = 30;
            BarrelRollRadius = 170;
            IsBarrelRollCheck = false;
            IsGoldenScanCheck = false;
            GoldenScanDelay = 30;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, (uint) KeyInterop.VirtualKeyFromKey(toggleKey));

            canAutoClickerStart = true;
        }
        protected override void OnClosed(EventArgs e)
        {
            canAutoClickerStart = false;
            _source.RemoveHook(HwndHook);
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            base.OnClosed(e);
        }

        // The magic function
        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            int vkey = ((int)lParam >> 16) & 0xFFFF;

                            // Stop clicker if Windows key press (Fail safe)
                            // Dont work? >>>>> Investigate <<<<<<
                            if (vkey == KeyInterop.VirtualKeyFromKey(Key.LWin))
                            {
                                Stop();
                                clearKeyToggle(null, null);
                            }
                            else if (vkey == KeyInterop.VirtualKeyFromKey(toggleKey))
                            {
                                // Key Press Logic
                                if (canAutoClickerStart)
                                {
                                    if (toggleAutoClickerState)
                                    {
                                        // if clicker status is running (true), toggle to false to stop it 
                                        if (dispatcherTimer != null)
                                        {
                                            dispatcherTimer.Stop();
                                        }
                                        if (goldenDispatcherTimer != null)
                                        {
                                            goldenDispatcherTimer.Stop();
                                        }

                                        Stop();
                                    }
                                    else
                                    {
                                        // if clicker is not running (false), start it
                                        Start();

                                        if (IsBarrelRollCheck)
                                        {
                                            //  DispatcherTimer for Barrel Roll
                                            dispatcherTimer = new DispatcherTimer();
                                            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);

                                            int temp = BarrelRollDelay;
                                            int hours = temp / 3600;
                                            temp = temp - hours * 3600;
                                            int minutes = temp / 60;
                                            int seconds = temp - minutes * 60;
                                            dispatcherTimer.Interval = new TimeSpan(hours, minutes, seconds);

                                            //dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
                                            dispatcherTimer.Start();
                                        }

                                        if (IsGoldenScanCheck)
                                        {
                                            //  DispatcherTimer for Golden Cookie Scan
                                            goldenDispatcherTimer = new DispatcherTimer();
                                            goldenDispatcherTimer.Tick += new EventHandler(handleGoldenDispatcherTimer);

                                            int temp = GoldenScanDelay;
                                            int hours = temp / 3600;
                                            temp = temp - hours * 3600;
                                            int minutes = temp / 60;
                                            int seconds = temp - minutes * 60;
                                            goldenDispatcherTimer.Interval = new TimeSpan(hours, minutes, seconds);

                                            goldenDispatcherTimer.Start();
                                        }
                                    }
                                }
                            }
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window.KeyDown += HandleKeyPress;
        }

        private void HandleKeyPress(object sender, KeyEventArgs e)
        {
            if (isKeyToggleAllowed)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID);
                toggleKey = e.Key;
                keyNameTextBox.Text = toggleKey.ToString();
                RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, (uint) KeyInterop.VirtualKeyFromKey(toggleKey));
                isKeyToggleAllowed = false;
            }
            return;
         }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Stop();
            doABarrelRoll(null, null);
            Start();
        }

        private void handleGoldenDispatcherTimer(object sender, EventArgs e)
        {
            Stop();
            doAGoldenScan(null, null);
            Start();
            
        }

        private void doAClick()
        {
            if (IsFixPosition)
            {
                LeftMouseClick((int) XPosition, (int) YPosition);
            }
            else
            {
                Point currentMousePosition = GetMousePosition();
                LeftMouseClick((int)currentMousePosition.X, (int)currentMousePosition.Y);
            }            
        }
        public static Point GetMousePosition()
        {
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);

            return new Point(w32Mouse.X, w32Mouse.Y);
        }

        //This simulates a left mouse click
        public static void LeftMouseClick(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
        }


        private void setKeyToggle(object sender, RoutedEventArgs e)
        {
            isKeyToggleAllowed = true;
        }

        private void clearKeyToggle(object sender, RoutedEventArgs e)
        {
            isKeyToggleAllowed = false;
            keyNameTextBox.Text = "";
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
        }

        public void Start()
        {
            toggleAutoClickerState = true; // allow clicker ejecution
            Thread.Sleep(InitialDelay);

            // new clicker thread creation
            Clicker = new Thread(MyAutoClicker);
            Clicker.IsBackground = true;
            Clicker.Start();
        }

        public void Stop()
        {
            toggleAutoClickerState = false; // stops clicker ejecution
            if (Clicker != null)
            {
                Clicker.Join(); // wait for the thread to finish
            }
        }

        public void MyAutoClicker()
        {
            try
            {
                while (toggleAutoClickerState)
                {
                    Thread.Sleep(SleepTimeMillis);
                    doAClick();
                }
            }
            catch
            {
                // End silently if error
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void resetGoldenCookiePosition(object sender, RoutedEventArgs e)
        {
            XPosition = 300 / 1920 * SystemParameters.PrimaryScreenWidth;
            YPosition = 400 / 1080 * SystemParameters.PrimaryScreenHeight;
            xPositionTextBox.Text = XPosition.ToString();
            yPositionTextBox.Text = YPosition.ToString();
            IsFixPosition = false;
            fixPositionCheckBox.IsChecked = false;
        }

        private void checkFixPosition(object sender, RoutedEventArgs e)
        {
            IsFixPosition = true;
        }

        private void uncheckFixPosition(object sender, RoutedEventArgs e)
        {
            IsFixPosition = false;
        }

        private void doABarrelRoll(object sender, RoutedEventArgs e)
        {
            // rotationRadius = (int) XPosition / 2;
            int increment = 360 / rotationSteps;
            double theta, xPos, yPos;
            Point originalMousePosition = GetMousePosition();

            for (int i = 0; i < 360; i += increment)
            {
                theta = i * Math.PI / 180;
                xPos = XPosition + BarrelRollRadius * Math.Cos(theta);
                yPos = YPosition + BarrelRollRadius * Math.Sin(theta);

                for (int j=0; j<rotationCliksPerStep; j++)
                {
                    Thread.Sleep(rotationClickDelay);
                    LeftMouseClick((int) xPos, (int) yPos);
                }
            }
            SetCursorPos((int) originalMousePosition.X, (int) originalMousePosition.Y);
        }

        private void doAGoldenScan(object sender, RoutedEventArgs e)
        {
            Point originalMousePosition = GetMousePosition();
            Point goldenStartPoint = new Point(5, 180);
            Point goldenEndPoint = new Point(1570, 1000);
            int pixelStepHorizontal = 25;
            int pixelStepVertical = 25;

            for (int y = (int)goldenStartPoint.Y; y < (int)goldenEndPoint.Y; y += pixelStepVertical)
            {
                for (int x = (int)goldenStartPoint.X; x < (int)goldenEndPoint.X; x += pixelStepHorizontal)
                {
                    if (x < 100 && y > 900) { continue; } // avoid Klumbor in the left bottom side
                    Thread.Sleep(5);
                    LeftMouseClick(x, y);
                }
            }

            SetCursorPos((int)originalMousePosition.X, (int)originalMousePosition.Y);
        }

        private void checkBarrelRoll(object sender, RoutedEventArgs e)
        {
            IsBarrelRollCheck = true;
        }

        private void uncheckBarrelRoll(object sender, RoutedEventArgs e)
        {
            IsBarrelRollCheck = false;
        }

        private void checkGoldenScan(object sender, RoutedEventArgs e)
        {
            IsGoldenScanCheck = true;
        }

        private void uncheckGoldenScan(object sender, RoutedEventArgs e)
        {
            IsGoldenScanCheck = false;
        }
    }
}
