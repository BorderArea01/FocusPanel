using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FocusPanel.ViewModels;
using FocusPanel.Models;

namespace FocusPanel.Views;

public partial class FileOrganizerView : UserControl
{
    public FileOrganizerView()
    {
        InitializeComponent();
        this.SizeChanged += FileOrganizerView_SizeChanged;
    }

    private void FileOrganizerView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is FileOrganizerViewModel vm)
        {
            // Simple responsive logic: 
            // < 500px: 1 column
            // 500-900px: 2 columns
            // > 900px: 3 columns
            // > 1400px: 4 columns
            
            double width = e.NewSize.Width;
            if (width < 500) vm.PartitionColumns = 1;
            else if (width < 900) vm.PartitionColumns = 2;
            else if (width < 1400) vm.PartitionColumns = 3;
            else vm.PartitionColumns = 4;
        }
    }

    private void FileCard_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement card && card.DataContext is DesktopFile file)
        {
            if (DataContext is FileOrganizerViewModel vm)
            {
                vm.SelectedFile = file; // Ensure selected for command
                DragDrop.DoDragDrop(card, file, DragDropEffects.Move);
            }
        }
    }

    private void Partition_DragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border border)
        {
            border.BorderBrush = (Brush)FindResource("PrimaryHueMidBrush");
            border.BorderThickness = new Thickness(2);
        }
    }

    private void Partition_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border border)
        {
            border.BorderBrush = Brushes.Transparent;
            border.BorderThickness = new Thickness(0);
        }
    }

    private void Partition_Drop(object sender, DragEventArgs e)
    {
        if (sender is Border border)
        {
            border.BorderBrush = Brushes.Transparent;
            border.BorderThickness = new Thickness(0);

            if (border.DataContext is PartitionViewModel partition && 
                DataContext is FileOrganizerViewModel vm &&
                e.Data.GetData(typeof(DesktopFile)) is DesktopFile file)
            {
                // Execute move
                if (vm.AssignToPartitionCommand.CanExecute(partition.Name))
                {
                    vm.AssignToPartitionCommand.Execute(partition.Name);
                }
            }
        }
    }

    private void PopupBox_Unchecked(object sender, RoutedEventArgs e)
    {
        // Handle popup close if needed
    }
}
