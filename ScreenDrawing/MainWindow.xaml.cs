using System;
using System.Collections.Generic;
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
        readonly List<Color> recents = new List<Color>();

        public MainWindow()
        {
            InitializeComponent();
            Ink.DefaultDrawingAttributes.FitToCurve = true;
            Ink.DefaultDrawingAttributes.Width = 6;
            Ink.DefaultDrawingAttributes.Height = 6;
            UseColor(Colors.Red);
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

        void UseColor(Color c)
        {
            Ink.DefaultDrawingAttributes.Color = c;
            Ink.EditingMode = InkCanvasEditingMode.Ink;
            CurrentColorBtn.Background = new SolidColorBrush(c);
            recents.RemoveAll(x => x == c);
            recents.Insert(0, c);
            if (recents.Count > 5) recents.RemoveAt(5);
            RefreshRecents();
        }

        void RefreshRecents()
        {
            Button[] btns = { R0, R1, R2, R3, R4 };
            for (int i = 0; i < btns.Length; i++)
            {
                if (i < recents.Count)
                {
                    btns[i].Background = new SolidColorBrush(recents[i]);
                    btns[i].Tag = recents[i];
                    btns[i].Visibility = Visibility.Visible;
                }
                else
                {
                    btns[i].Visibility = Visibility.Collapsed;
                }
            }
        }

        void Pick_Click(object sender, RoutedEventArgs e)
        {
            var c = Ink.DefaultDrawingAttributes.Color;
            var dlg = new System.Windows.Forms.ColorDialog
            {
                Color = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B),
                FullOpen = true
            };
            Topmost = false;
            var result = dlg.ShowDialog();
            Topmost = true;
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var d = dlg.Color;
                UseColor(Color.FromArgb(d.A, d.R, d.G, d.B));
            }
        }

        void Recent_Click(object sender, RoutedEventArgs e)
        {
            UseColor((Color)((Button)sender).Tag);
        }

        void Size_Click(object sender, RoutedEventArgs e)
        {
            SizeBox.Text = (string)((Button)sender).Tag;
        }

        void SizeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Ink == null) return;
            if (double.TryParse(SizeBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double s) && s > 0)
            {
                Ink.DefaultDrawingAttributes.Width = s;
                Ink.DefaultDrawingAttributes.Height = s;
            }
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