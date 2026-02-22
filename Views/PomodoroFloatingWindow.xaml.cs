using System.Windows;
using System.Windows.Input;
using FocusPanel.ViewModels;

namespace FocusPanel.Views
{
    public partial class PomodoroFloatingWindow : Window
    {
        public PomodoroFloatingWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}