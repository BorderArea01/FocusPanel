using System;
using System.Windows;
using FocusPanel.ViewModels;
using FocusPanel.Views;

namespace FocusPanel.Services
{
    public static class PomodoroWindowManager
    {
        private static PomodoroFloatingWindow? _floatingWindow;
        private static ScreenBorderWindow? _borderWindow;

        public static void OpenWindows(PomodoroViewModel viewModel)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_floatingWindow == null)
                {
                    _floatingWindow = new PomodoroFloatingWindow();
                    _floatingWindow.DataContext = viewModel;
                    _floatingWindow.Closed += (s, e) => _floatingWindow = null;
                    _floatingWindow.Show();
                }
                else
                {
                    _floatingWindow.DataContext = viewModel;
                    if (!_floatingWindow.IsVisible) _floatingWindow.Show();
                }

                if (_borderWindow == null)
                {
                    _borderWindow = new ScreenBorderWindow();
                    _borderWindow.Closed += (s, e) => _borderWindow = null;
                    _borderWindow.Show();
                }
                else
                {
                    if (!_borderWindow.IsVisible) _borderWindow.Show();
                }
            });
        }

        public static void CloseWindows()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _floatingWindow?.Close();
                _floatingWindow = null;

                _borderWindow?.Close();
                _borderWindow = null;
            });
        }
    }
}