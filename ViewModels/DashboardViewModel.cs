using CommunityToolkit.Mvvm.ComponentModel;

namespace FocusPanel.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private string welcomeMessage = "Welcome to FocusPanel";
}
