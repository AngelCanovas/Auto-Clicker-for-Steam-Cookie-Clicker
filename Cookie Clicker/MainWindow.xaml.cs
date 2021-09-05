using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Cookie_Clicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int SleepTimeMillis { get; set; }
        public int InitialDelay { get; set; }
        bool canAutoClickerStart = false;
        bool toggleAutoClickerState = false;

        private IntPtr _windowHandle;
        private HwndSource _source;

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
            SleepTimeMillis = 15;
            InitialDelay = 100;
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
                            if (vkey == KeyInterop.VirtualKeyFromKey(toggleKey))
                            {
                                // Key Press Logic
                                if (canAutoClickerStart)
                                {
                                    if (toggleAutoClickerState)
                                    {
                                        Stop();
                                    }
                                    else
                                    {
                                        Start();
                                    }
                                }
                                else
                                {

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

        private void doAClick()
        {
            Point currentMousePosition = GetMousePosition();
            LeftMouseClick((int)currentMousePosition.X, (int)currentMousePosition.Y);
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
            Thread.Sleep(InitialDelay);
            Clicker = new Thread(MyAutoClicker);
            Clicker.IsBackground = true;
            Clicker.Start();
            toggleAutoClickerState = true;
        }

        public void Stop()
        {
            if (Clicker != null)
            {
                Clicker.Interrupt(); // kill the thread?
            }
            toggleAutoClickerState = false;
        }

        public void MyAutoClicker()
        {
            try
            {
                bool state = true;
                while (state)
                {
                    Thread.Sleep(SleepTimeMillis);
                    doAClick();
                }
            }
            catch
            {
                // Nothing
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
