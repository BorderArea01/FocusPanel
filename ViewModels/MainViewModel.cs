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

    public MainViewModel()
    {
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
                CurrentViewModel = new PomodoroViewModel();
                break;
            case "Files":
                CurrentViewModel = new FileOrganizerViewModel();
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
