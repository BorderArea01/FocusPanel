using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using FocusPanel.ViewModels;

namespace FocusPanel.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();

            // Use system icon since App.ico is missing
            MyNotifyIcon.Icon = SystemIcons.Application;
            
            // Set to Maximized to cover the full screen
            WindowStartupLocation = WindowStartupLocation.Manual;
            WindowState = WindowState.Maximized;
            
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Topmost = false; 
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
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
    }
}
