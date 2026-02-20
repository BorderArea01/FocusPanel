using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using FocusPanel.ViewModels;

namespace FocusPanel.Views
{
    public partial class MainWindow : Window
    {
        private bool _isExit = false;

        public MainWindow()
        {
            InitializeComponent();
            var viewModel = new MainViewModel();
            viewModel.RequestClose += () => this.ForceClose();
            DataContext = viewModel;

            // Use system icon since App.ico is missing
            MyNotifyIcon.Icon = SystemIcons.Application;
            
            // Set to Maximized to cover the full screen or desired state
            // If ShowInTaskbar is false, users might want it Normal or Maximized depending on design.
            // Keeping it Maximized as per original design.
            WindowStartupLocation = WindowStartupLocation.Manual;
            WindowState = WindowState.Maximized;
            
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Topmost = false; 
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!_isExit)
            {
                e.Cancel = true;
                this.Hide(); // Hide the window
                // Or: this.WindowState = WindowState.Minimized;
            }
        }

        public void ForceClose()
        {
            _isExit = true;
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Allow dragging if not maximized
            if (WindowState != WindowState.Maximized)
            {
                DragMove();
            }
        }

        private void Sidebar_MouseEnter(object sender, MouseEventArgs e)
        {
            DoubleAnimation widthAnimation = new DoubleAnimation
            {
                To = 800,
                Duration = new Duration(TimeSpan.FromSeconds(0.3)),
                AccelerationRatio = 0.2,
                DecelerationRatio = 0.8
            };
            SidebarBorder.BeginAnimation(WidthProperty, widthAnimation);

            DoubleAnimation opacityAnimation = new DoubleAnimation
            {
                To = 1,
                Duration = new Duration(TimeSpan.FromSeconds(0.3))
            };
            HeaderGrid.BeginAnimation(OpacityProperty, opacityAnimation);
            ContentArea.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        private void Sidebar_MouseLeave(object sender, MouseEventArgs e)
        {
            if (SidebarBorder.IsKeyboardFocusWithin)
            {
                return;
            }
            CollapseSidebar();
        }

        private void CollapseSidebar()
        {
            DoubleAnimation widthAnimation = new DoubleAnimation
            {
                To = 80,
                Duration = new Duration(TimeSpan.FromSeconds(0.3)),
                AccelerationRatio = 0.2,
                DecelerationRatio = 0.8
            };
            SidebarBorder.BeginAnimation(WidthProperty, widthAnimation);

            DoubleAnimation opacityAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(0.3))
            };
            HeaderGrid.BeginAnimation(OpacityProperty, opacityAnimation);
            ContentArea.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        private void Sidebar_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue && !SidebarBorder.IsMouseOver)
            {
                CollapseSidebar();
            }
        }
    }
}
