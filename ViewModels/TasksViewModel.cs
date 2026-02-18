using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using FocusPanel.Models;

namespace FocusPanel.ViewModels;

public partial class TasksViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<TodoItem> tasks = new();
}
