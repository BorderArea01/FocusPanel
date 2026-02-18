using CommunityToolkit.Mvvm.ComponentModel;

namespace FocusPanel.ViewModels;

public partial class PomodoroViewModel : ObservableObject
{
    [ObservableProperty]
    private string timerDisplay = "25:00";
}
