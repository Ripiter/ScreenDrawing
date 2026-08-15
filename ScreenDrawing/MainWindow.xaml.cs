using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ScreenDrawing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TRANSPARENT = 0x20;
        const int WS_EX_LAYERED = 0x80000;
        const int WM_HOTKEY = 0x0312;
        const int HOTKEY_ID = 1;
        const uint MOD_ALT = 0x1;
        const uint MOD_CONTROL = 0x2;
        const uint VK_D = 0x44;

        IntPtr hwnd;
        bool passthrough;

        public MainWindow()
        {
            InitializeComponent();
            Ink.DefaultDrawingAttributes.Color = Colors.Red;
            Ink.DefaultDrawingAttributes.Width = 6;
            Ink.DefaultDrawingAttributes.Height = 6;
            Ink.DefaultDrawingAttributes.FitToCurve = true;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd).AddHook(WndProc);
            RegisterHotKey(hwnd, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_D);
        }

        IntPtr WndProc(IntPtr h, int msg, IntPtr w, IntPtr l, ref bool handled)
        {
            if (msg == WM_HOTKEY && w.ToInt32() == HOTKEY_ID)
            {
                TogglePassthrough();
                handled = true;
            }
            return IntPtr.Zero;
        }

        void TogglePassthrough()
        {
            passthrough = !passthrough;
            int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
            if (passthrough) ex |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
            else ex &= ~WS_EX_TRANSPARENT;
            SetWindowLong(hwnd, GWL_EXSTYLE, ex);
            Toolbar.Visibility = passthrough ? Visibility.Hidden : Visibility.Visible;
        }

        void Color_Click(object sender, RoutedEventArgs e)
        {
            var c = (Color)ColorConverter.ConvertFromString((string)((Button)sender).Tag);
            Ink.DefaultDrawingAttributes.Color = c;
            Ink.EditingMode = InkCanvasEditingMode.Ink;
        }

        void Size_Click(object sender, RoutedEventArgs e)
        {
            double s = double.Parse((string)((Button)sender).Tag, CultureInfo.InvariantCulture);
            Ink.DefaultDrawingAttributes.Width = s;
            Ink.DefaultDrawingAttributes.Height = s;
        }

        void Pen_Click(object sender, RoutedEventArgs e) => Ink.EditingMode = InkCanvasEditingMode.Ink;
        void Erase_Click(object sender, RoutedEventArgs e) => Ink.EditingMode = InkCanvasEditingMode.EraseByStroke;
        void Clear_Click(object sender, RoutedEventArgs e) => Ink.Strokes.Clear();

        void Exit_Click(object sender, RoutedEventArgs e)
        {
            UnregisterHotKey(hwnd, HOTKEY_ID);
            Close();
        }
    }

}