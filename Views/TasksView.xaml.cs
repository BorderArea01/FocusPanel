using System.Windows.Controls;
using System.Windows;
using FocusPanel.ViewModels;

namespace FocusPanel.Views;

public partial class TasksView : UserControl
{
    private TaskDetailWindow _detailWindow;

    public TasksView()
    {
        InitializeComponent();
        this.DataContextChanged += TasksView_DataContextChanged;
    }

    private void TasksView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is TasksViewModel oldVm)
        {
            oldVm.OpenTaskDetailRequested -= OnOpenTaskDetailRequested;
            oldVm.CloseTaskDetailRequested -= OnCloseTaskDetailRequested;
        }

        if (e.NewValue is TasksViewModel newVm)
        {
            newVm.OpenTaskDetailRequested += OnOpenTaskDetailRequested;
            newVm.CloseTaskDetailRequested += OnCloseTaskDetailRequested;
        }
    }

    private void OnOpenTaskDetailRequested(FocusPanel.Models.TodoItem item)
    {
        if (_detailWindow != null)
        {
            _detailWindow.Activate();
            return;
        }

        _detailWindow = new TaskDetailWindow
        {
            DataContext = this.DataContext // Share VM
        };
        _detailWindow.Closed += (s, args) => 
        {
            _detailWindow = null;
            // Also ensure VM knows it's closed if closed via X
            if (DataContext is TasksViewModel vm && vm.SelectedTask != null)
            {
                vm.SelectedTask = null;
            }
        };
        _detailWindow.Show();
    }

    private void OnCloseTaskDetailRequested()
    {
        _detailWindow?.Close();
        _detailWindow = null;
    }
}