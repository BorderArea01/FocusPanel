using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusPanel.Services;
using System.Windows;

namespace FocusPanel.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "FocusPanel";

    [ObservableProperty]
    private object currentViewModel;

    [ObservableProperty]
    private DateTime currentTime;

    private PomodoroViewModel _pomodoroViewModel;
    private FileOrganizerViewModel _fileOrganizerViewModel;

    public MainViewModel()
    {
        CurrentTime = DateTime.Now;
        var timer = new System.Windows.Threading.DispatcherTimer();
        timer.Interval = System.TimeSpan.FromSeconds(1);
        timer.Tick += (s, e) => CurrentTime = DateTime.Now;
        timer.Start();

        CurrentViewModel = new TasksViewModel();
        // Enable auto-startup by default
        AutoStartupService.SetStartup(true);
    }

    [RelayCommand]
    private void Navigate(string destination)
    {
        switch (destination)
        {
            case "Dashboard":
                CurrentViewModel = new DashboardViewModel();
                break;
            case "Tasks":
                CurrentViewModel = new TasksViewModel();
                break;
            case "Pomodoro":
                if (_pomodoroViewModel == null) _pomodoroViewModel = new PomodoroViewModel();
                CurrentViewModel = _pomodoroViewModel;
                break;
            case "Files":
                if (_fileOrganizerViewModel == null) _fileOrganizerViewModel = new FileOrganizerViewModel();
                CurrentViewModel = _fileOrganizerViewModel;
                break;
            case "AI":
                CurrentViewModel = new AIAssistantViewModel();
                break;
        }
    }

    public event System.Action RequestClose;

    [RelayCommand]
    private void CloseApp()
    {
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void MinimizeApp()
    {
        Application.Current.MainWindow.WindowState = WindowState.Minimized;
    }

    [RelayCommand]
    private void ShowWindow()
    {
        var window = Application.Current.MainWindow;
        if (window != null)
        {
            window.Show();
            window.WindowState = WindowState.Normal;
            window.Activate();
        }
    }
}
