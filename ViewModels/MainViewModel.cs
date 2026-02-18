using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        CurrentViewModel = new DashboardViewModel();
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

    [RelayCommand]
    private void CloseApp()
    {
        Application.Current.Shutdown();
    }

    [RelayCommand]
    private void MinimizeApp()
    {
        Application.Current.MainWindow.WindowState = WindowState.Minimized;
    }
}
