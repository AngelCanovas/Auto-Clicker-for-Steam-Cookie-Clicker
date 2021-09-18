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
    public partial class MainWindow : Window
    {
        public int SleepTimeMillis { get; set; }
        public int InitialDelay { get; set; }
        public bool IsFixPosition { get; set; }
        public int XPosition { get; set; }
        public int YPosition { get; set; }
        public int BarrelRollDelay { get; set; }
        public int BarrelRollRadius { get; set; }
        public bool IsBarrelRollCheck { get; set; }
        public bool IsGoldenScanCheck { get; set; }
        public int GoldenScanDelay { get; set; }
        public bool IsAutomaticModeCheck { get; set; }
        public int AutomaticModeDelay { get; set; }
        public bool IsDisabledSwitchBuy { get; set; }

        public bool canAutoClickerStart = false;
        public bool toggleAutoClickerState = false;
        public int screenWidth = (int) SystemParameters.FullPrimaryScreenWidth;
        public int screenHeight = (int) SystemParameters.FullPrimaryScreenHeight;
        public double scaleWidth = SystemParameters.FullPrimaryScreenWidth / 1920;
        public double scaleHeight = SystemParameters.FullPrimaryScreenHeight / 1080;

        // variables for Barrel Roll
        private static bool toggleBarrelRoll = false;
        private Point originalMousePosition;
        private int rotationSteps = 36 * 2;
        private int rotationCliksPerStep = 4;
        private int rotationClickDelay = 5;

        // variables for golden auto scan 
        private static bool toggleGoldenScan = false;
        private int goldenClickDelay = 5;
        private int goldenPixelStepHorizontal;
        private int goldenPixelStepVertical;
        private Point goldenStartPoint;
        private Point goldenEndPoint;

        // variables for auto buy upgrader
        private static bool toggleAutoBuy = false;
        private Point automaticBuyStartPoint;
        private int automaticBuyClickDelay = 10;
        private int automaticBuyWaitDelay = 25;
        private int automaticBuyWaitDelayUpgrades = 50;
        private int distanteBetweenBuildings = 64; // don't change between screen sizes
        private int distanteToUpgrades = 108;
        private int distanceBetweenUpgradeAndSwitches = 76;
        private int distanceWheelScrollForLastBuildings = 76; // same pixels as between upgrades
        private int scrollMaximumDistancePositive;
        private int scrollMaximumDistanceNegative;

        // handle variables for key binding and timers
        private IntPtr _windowHandle;
        private HwndSource _source;
        private DispatcherTimer dispatcherTimer;
        private DispatcherTimer goldenDispatcherTimer;
        private DispatcherTimer automaticModeDispatcherTimer;
        private Thread Clicker;
        private Thread barrelRollThread;
        private Thread goldenCookieThread;
        private Thread autoBuyThread;
        private bool isKeyToggleAllowed = false;
        private Key toggleKey = Key.F6; // default toggle key

        // constants
        private const int HOTKEY_ID = 9000;
        private const uint MOD_NONE = 0x0000; // (none)
        private const uint MOD_ALT = 0x0001; //ALT
        private const uint MOD_CONTROL = 0x0002; //CTRL
        private const uint MOD_SHIFT = 0x0004; //SHIFT
        private const uint MOD_WIN = 0x0008; //WINDOWS
        public const int MOUSEEVENTF_LEFTDOWN = 0x02; // Mouse left click down
        public const int MOUSEEVENTF_LEFTUP = 0x04; // Mouse left click up
        public const int MOUSEEVENTF_WHEEL = 0x0800; // Mouse wheel

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
            SleepTimeMillis = 10;
            InitialDelay = 100;
            IsFixPosition = false;
            BarrelRollDelay = 3600;
            IsBarrelRollCheck = false;
            IsGoldenScanCheck = false;
            GoldenScanDelay = 60;
            IsAutomaticModeCheck = false;
            AutomaticModeDelay = 30;
            IsDisabledSwitchBuy = false;

            InitializeScreenDependantComponents();
        }

        protected void InitializeScreenDependantComponents()
        {
            XPosition = (int) (290 * scaleWidth);
            YPosition = (int) (435 * scaleHeight);
            BarrelRollRadius = 170;
            goldenPixelStepHorizontal = 30; // if maken screen responsive, the final value may be too small or large to function correctly
            goldenPixelStepVertical = 30;
            goldenStartPoint = new Point(5, 180); // don't change with screen resolutions
            goldenEndPoint = new Point(1580 * scaleWidth,  1010 * scaleHeight);
            automaticBuyStartPoint = new Point(1625 * scaleWidth, 1005 * scaleHeight);
            scrollMaximumDistancePositive = screenHeight;
            scrollMaximumDistanceNegative = -screenHeight;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);

            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, (uint) KeyInterop.VirtualKeyFromKey(toggleKey));
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, (uint) KeyInterop.VirtualKeyFromKey(Key.F7));
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, (uint) KeyInterop.VirtualKeyFromKey(Key.F8));
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
                                if (toggleAutoClickerState)
                                {
                                    // if clicker status is running (true), toggle to false to stop it
                                    Stop();

                                    if (dispatcherTimer != null)
                                    {
                                        dispatcherTimer.Stop();
                                    }
                                    if (goldenDispatcherTimer != null)
                                    {
                                        goldenDispatcherTimer.Stop();
                                    }
                                    if (automaticModeDispatcherTimer != null)
                                    {
                                        automaticModeDispatcherTimer.Stop();
                                    }
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

                                    if (IsAutomaticModeCheck)
                                    {
                                        //  DispatcherTimer for Automatic mode
                                        automaticModeDispatcherTimer = new DispatcherTimer();
                                        automaticModeDispatcherTimer.Tick += new EventHandler(handleAutomaticModeDispatcherTimer);

                                        int temp = AutomaticModeDelay;
                                        int hours = temp / 3600;
                                        temp = temp - hours * 3600;
                                        int minutes = temp / 60;
                                        int seconds = temp - minutes * 60;
                                        automaticModeDispatcherTimer.Interval = new TimeSpan(hours, minutes, seconds);

                                        automaticModeDispatcherTimer.Start();
                                    }
                                }
                            }
                            else if (vkey == KeyInterop.VirtualKeyFromKey(Key.F7))
                            {
                                handleGoldenDispatcherTimer(null, null);
                            }
                            else if (vkey == KeyInterop.VirtualKeyFromKey(Key.F8))
                            {
                                handleAutomaticModeDispatcherTimer(null, null);
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
                Stop();
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
            if (toggleBarrelRoll)
            {
                toggleBarrelRoll = false;
                if (barrelRollThread != null) { barrelRollThread.Join(); }
            }
            else
            {
                toggleBarrelRoll = true;
                if (toggleAutoClickerState)
                {
                    Stop();
                    barrelRollThread = new Thread(callABarrelRoll);
                    barrelRollThread.IsBackground = true;
                    barrelRollThread.Start();
                    Start();
                }
                else
                {
                    barrelRollThread = new Thread(callABarrelRoll);
                    barrelRollThread.IsBackground = true;
                    barrelRollThread.Start();
                }
            }
        }

        private void handleGoldenDispatcherTimer(object sender, EventArgs e)
        {
            if (toggleGoldenScan)
            {
                toggleGoldenScan = false;
                if (goldenCookieThread != null) { goldenCookieThread.Join(); }
            }
            else
            {
                toggleGoldenScan = true;
                if (toggleAutoClickerState)
                {
                    Stop();
                    goldenCookieThread = new Thread(callAGoldenScan);
                    goldenCookieThread.IsBackground = true;
                    goldenCookieThread.Start();
                    Start();
                }
                else
                {
                    goldenCookieThread = new Thread(callAGoldenScan);
                    goldenCookieThread.IsBackground = true;
                    goldenCookieThread.Start();
                }
            }
        }

        private void handleAutomaticModeDispatcherTimer(object sender, EventArgs e)
        {
            if (toggleAutoBuy)
            {
                toggleAutoBuy = false;
                if (autoBuyThread != null) { autoBuyThread.Join(); }
            }
            else
            {
                toggleAutoBuy = true;
                if (toggleAutoClickerState)
                {
                    Stop();
                    autoBuyThread = new Thread(callAUpgradeBuy);
                    autoBuyThread.IsBackground = true;
                    autoBuyThread.Start();
                    Start();
                }
                else
                {
                    autoBuyThread = new Thread(callAUpgradeBuy);
                    autoBuyThread.IsBackground = true;
                    autoBuyThread.Start();
                }
            }
        }

        private void doAClick()
        {
            if (IsFixPosition)
            {
                LeftMouseClick(XPosition, YPosition);
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

        // Simulate a wheel scroll
        // Positive amount is going up, negative going down in screen
        // One wheel click is defined as WHEEL_DELTA, which is 120
        public static void ScrollMouse(int xpos, int ypos, int amount)
        {
            SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_WHEEL, xpos, ypos, amount, 0);
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
            XPosition = 290 * screenWidth / 1920;
            YPosition = 435 * screenHeight / 1080;
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
            originalMousePosition = GetMousePosition();
            toggleBarrelRoll = true;
            int increment = 360 / rotationSteps;
            double theta, xPos, yPos;

            for (int i = 0; i < 360 && toggleBarrelRoll; i += increment)
            {
                // if (!toggleBarrelRoll) { break; }
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
            toggleBarrelRoll = false;
        }

        private void callABarrelRoll()
        {
            doABarrelRoll(null, null);
        }

        private void doAGoldenScan(object sender, RoutedEventArgs e)
        {
            originalMousePosition = GetMousePosition();
            toggleGoldenScan = true;

            for (int y = (int)goldenStartPoint.Y; y < (int)goldenEndPoint.Y && toggleGoldenScan; y += goldenPixelStepVertical)
            {
                //if (!toggleGoldenScan) { break; }
                for (int x = (int)goldenStartPoint.X; x < (int)goldenEndPoint.X; x += goldenPixelStepHorizontal)
                {
                    //if (!toggleGoldenScan) { break; }
                    if (x < 90 && y > 880) { continue; } // avoid Klumbor in the left bottom side and season companions

                    Thread.Sleep(goldenClickDelay);
                    LeftMouseClick(x, y);
                }
            }

            SetCursorPos((int)originalMousePosition.X, (int)originalMousePosition.Y);
            toggleGoldenScan = false;
        }

        private void callAGoldenScan()
        {
            doAGoldenScan(null, null);
        }

        private void doAUpgradeBuy(object sender, RoutedEventArgs e)
        {
            originalMousePosition = GetMousePosition();
            toggleAutoBuy = true;
            int xPos = (int) automaticBuyStartPoint.X;
            int yPos = (int) automaticBuyStartPoint.Y;

            // Scroll to the top
            ScrollMouse(xPos, yPos, scrollMaximumDistancePositive);

            // Buy upgrades first to better cookies/sec scaling
            for (int k = 0; k < 15 && toggleAutoBuy; k++)
            {
                //if (!toggleAutoBuy) { break; }
                Thread.Sleep(automaticBuyWaitDelayUpgrades);
                LeftMouseClick(xPos, yPos - 11 * distanteBetweenBuildings - distanteToUpgrades);
            }

            // Buy switches & season starters if not disabled
            if (!IsDisabledSwitchBuy)
            {
                // Scroll to the top
                ScrollMouse(xPos, yPos, scrollMaximumDistancePositive);

                for (int l = 0; l < 3 && toggleAutoBuy; l++)
                {
                    //if (!toggleAutoBuy) { break; }
                    Thread.Sleep(automaticBuyWaitDelayUpgrades);
                    LeftMouseClick(xPos, yPos - 11 * distanteBetweenBuildings - distanteToUpgrades - distanceBetweenUpgradeAndSwitches);
                }
            }

            // Scroll to the end of the buildings
            ScrollMouse(xPos, yPos, scrollMaximumDistanceNegative);

            // Buy last buildings upgrades
            for (int i = 1; i <= 8 && toggleAutoBuy; i++)
            {
                //if (!toggleAutoBuy) { break; }
                Thread.Sleep(automaticBuyWaitDelay);
                ScrollMouse(xPos, yPos, distanceWheelScrollForLastBuildings);

                for (int i2 = 0; i2 < 10; i2++)
                {
                    Thread.Sleep(automaticBuyClickDelay);
                    LeftMouseClick(xPos, yPos);
                }
            }

            // Scroll to the top, in case of scroll displacement
            ScrollMouse(xPos, yPos, scrollMaximumDistancePositive);

            // Buy 11 first buildings
            for (int j = 1; j <= 11 && toggleAutoBuy; j++)
            {
                //if (!toggleAutoBuy) { break; }
                Thread.Sleep(automaticBuyWaitDelay);

                for (int j2 = 0; j2 < 10; j2++)
                {
                    Thread.Sleep(automaticBuyClickDelay);
                    LeftMouseClick(xPos, yPos - j * distanteBetweenBuildings);
                }
            }

            // return scroll to the top and mouse to original position 
            ScrollMouse(xPos, yPos, scrollMaximumDistancePositive);
            SetCursorPos((int) originalMousePosition.X, (int) originalMousePosition.Y);
            toggleAutoBuy = false;
        }

        private void callAUpgradeBuy()
        {
            doAUpgradeBuy(null, null);
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

        private void checkAutomaticMode(object sender, RoutedEventArgs e)
        {
            IsAutomaticModeCheck = true;
        }

        private void uncheckAutomaticMode(object sender, RoutedEventArgs e)
        {
            IsAutomaticModeCheck = false;
        }

        private void checkDisableSwitchBuy(object sender, RoutedEventArgs e)
        {
            IsDisabledSwitchBuy = true;
        }

        private void uncheckDisableSwitchBuy(object sender, RoutedEventArgs e)
        {
            IsDisabledSwitchBuy = false;
        }

        private void doBarrelRollAction(object sender, RoutedEventArgs e)
        {
            dispatcherTimer_Tick(null, null);
        }

        private void doGoldenScanAction(object sender, RoutedEventArgs e)
        {
            handleGoldenDispatcherTimer(null, null);
        }

        private void doAutoBuyAction(object sender, RoutedEventArgs e)
        {
            handleAutomaticModeDispatcherTimer(null, null);
        }
    }
}
