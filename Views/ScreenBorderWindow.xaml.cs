using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Controls;

namespace FocusPanel.Views
{
    public partial class ScreenBorderWindow : Window
    {
        public ScreenBorderWindow()
        {
            InitializeComponent();
            this.Loaded += ScreenBorderWindow_Loaded;
            
            // Create the animation programmatically if needed, or use XAML triggers
            // The XAML trigger should work fine for starting the animation
        }

        private void ScreenBorderWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Set click-through
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);
            int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);
            SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)(exStyle | (int)ExtendedWindowStyles.WS_EX_TRANSPARENT | (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW));
        }

        #region Window Styles
        [Flags]
        public enum ExtendedWindowStyles
        {
            WS_EX_TRANSPARENT = 0x00000020,
            WS_EX_TOOLWINDOW = 0x00000080
        }

        public enum GetWindowLongFields
        {
            GWL_EXSTYLE = -20
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        public static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLongPtr32(hWnd, nIndex);
        }

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            SetLastError(0);
            if (IntPtr.Size == 4)
            {
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                // throw new System.ComponentModel.Win32Exception(error);
                // Suppress error for now
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int IntSetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrCode);
        #endregion
    }
}