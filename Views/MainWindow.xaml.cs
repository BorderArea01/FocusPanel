using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using FocusPanel.ViewModels;
using Microsoft.Win32;

namespace FocusPanel.Views
{
    public partial class MainWindow : Window
    {
        private bool _isExit = false;
        private Screen _currentScreen = null!;

        // Win11 rounded corners for borderless windows
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        public MainWindow()
        {
            InitializeComponent();
            var viewModel = new MainViewModel();
            viewModel.RequestClose += () => this.ForceClose();
            DataContext = viewModel;

            MyNotifyIcon.Icon = SystemIcons.Application;

            WindowStartupLocation = WindowStartupLocation.Manual;
            WindowState = WindowState.Normal;

            // Initial size based on mouse-position screen (refined in Loaded)
            var initScreen = GetScreenFromMouse();
            Height = initScreen.WorkingArea.Height * 0.85;
            Width = 80;
            PositionAtRightEdge(initScreen);

            Topmost = true;
            ShowInTaskbar = false;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            Deactivated += MainWindow_Deactivated;
            LocationChanged += MainWindow_LocationChanged;
            SystemEvents.DisplaySettingsChanged += (s, e) =>
            {
                _currentScreen = null!;
                RefreshScreenAndReposition();
            };
        }

        private Screen GetScreenFromMouse()
        {
            var mousePos = System.Windows.Forms.Control.MousePosition;
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.Bounds.Contains(mousePos.X, mousePos.Y))
                    return screen;
            }
            return Screen.PrimaryScreen!;
        }

        private Screen GetCurrentScreen()
        {
            if (_currentScreen != null)
                return _currentScreen;

            var hwnd = GetWindowHandle();
            if (hwnd != IntPtr.Zero)
            {
                _currentScreen = Screen.FromHandle(hwnd);
                return _currentScreen;
            }

            return GetScreenFromMouse();
        }

        private IntPtr GetWindowHandle()
        {
            try { return new System.Windows.Interop.WindowInteropHelper(this).Handle; }
            catch { return IntPtr.Zero; }
        }

        private void RefreshScreenAndReposition()
        {
            var hwnd = GetWindowHandle();
            if (hwnd != IntPtr.Zero)
                _currentScreen = Screen.FromHandle(hwnd);
            else
                _currentScreen = GetScreenFromMouse();
            PositionAtRightEdge(_currentScreen);
        }

        private void PositionAtRightEdge(Screen screen)
        {
            Left = screen.WorkingArea.Right - Width;
            Top = screen.WorkingArea.Top + (screen.WorkingArea.Height - Height) / 2;
        }

        private double GetRightEdgeTarget()
        {
            var screen = GetCurrentScreen();
            return screen.WorkingArea.Right;
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            double expectedLeft = GetRightEdgeTarget() - ActualWidth;
            if (Math.Abs(Left - expectedLeft) > 10)
            {
                Left = expectedLeft;
            }
        }

        private void MainWindow_Deactivated(object? sender, EventArgs e)
        {
            CollapseSidebar();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Topmost = true;

            // Get accurate screen from window handle and reposition
            var hwnd = GetWindowHandle();
            if (hwnd != IntPtr.Zero)
            {
                _currentScreen = Screen.FromHandle(hwnd);
                PositionAtRightEdge(_currentScreen);

                int cornerPref = DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!_isExit)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        public void ForceClose()
        {
            _isExit = true;
            if (MyNotifyIcon != null) MyNotifyIcon.Dispose();
            Close();
            System.Windows.Application.Current.Shutdown();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Window stays docked — no dragging
        }

        private void Sidebar_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            double rightEdge = GetRightEdgeTarget();

            SidebarBorder.Width = 800;
            Width = 800;
            Left = rightEdge - 800;
            HeaderGrid.Opacity = 1;
            ContentArea.Opacity = 1;
        }

        private void Sidebar_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (SidebarBorder.IsKeyboardFocusWithin) return;
            CollapseSidebar();
        }

        private void CollapseSidebar_Click(object sender, RoutedEventArgs e)
        {
            CollapseSidebar();
        }

        public void CollapseSidebar()
        {
            double rightEdge = GetRightEdgeTarget();

            SidebarBorder.Width = 80;
            Width = 80;
            Left = rightEdge - 80;
            HeaderGrid.Opacity = 0;
            ContentArea.Opacity = 0;
        }

        private void Sidebar_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }

        private void Sidebar_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            Sidebar_MouseEnter(sender, null!);
        }

        private void Sidebar_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            var pos = e.GetPosition(SidebarBorder);
            if (pos.X < 0 || pos.X >= SidebarBorder.ActualWidth || pos.Y < 0 || pos.Y >= SidebarBorder.ActualHeight)
            {
                CollapseSidebar();
            }
        }
    }
}
