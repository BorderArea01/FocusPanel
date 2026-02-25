using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FocusPanel.Models;
using FocusPanel.ViewModels;

namespace FocusPanel.Views;

public partial class FileOrganizerView : UserControl
{
    public FileOrganizerView()
    {
        InitializeComponent();
        // Removed SizeChanged handler as we are no longer using UniformGrid with dynamic columns
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
            border.Background = (Brush)FindResource("MaterialDesignPaper"); // Ensure background is opaque for hit testing
            // Keep thickness same to avoid jitter
        }
    }

    private void Partition_DragOver(object sender, DragEventArgs e)
    {
        // This is necessary to allow drop
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
        
        // Visual Feedback for Insertion
        if (e.Data.GetData(typeof(PartitionViewModel)) is PartitionViewModel source && sender is Border border)
        {
             // Determine top or bottom half
             Point p = e.GetPosition(border);
             bool isBottom = p.Y > (border.ActualHeight / 2);
             
             if (isBottom)
             {
                 // Insert After (Bottom Line)
                 border.BorderBrush = (Brush)FindResource("PrimaryHueMidBrush");
                 border.BorderThickness = new Thickness(0, 0, 0, 4); 
             }
             else
             {
                 // Insert Before (Top Line)
                 border.BorderBrush = (Brush)FindResource("PrimaryHueMidBrush");
                 border.BorderThickness = new Thickness(0, 4, 0, 0); 
             }
        }
    }

    private void Partition_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border border)
        {
            border.BorderBrush = Brushes.Transparent;
            border.BorderThickness = new Thickness(0, 2, 0, 0); // Restore default
            border.Background = Brushes.Transparent; 
        }
    }

    private void Partition_Drop(object sender, DragEventArgs e)
    {
        if (sender is Border border)
        {
            // Capture drop position BEFORE clearing style
            Point p = e.GetPosition(border);
            bool isBottom = p.Y > (border.ActualHeight / 2);

            border.BorderBrush = Brushes.Transparent;
            border.BorderThickness = new Thickness(0, 2, 0, 0); // Restore default
            border.Background = Brushes.Transparent;

            // Debug
            System.Diagnostics.Debug.WriteLine("Partition_Drop Fired");

            if (border.DataContext is PartitionViewModel partition && DataContext is FileOrganizerViewModel vm)
            {
                // Case 1: Internal File Move
                if (e.Data.GetData(typeof(DesktopFile)) is DesktopFile file)
                {
                    if (vm.AssignToPartitionCommand.CanExecute(partition.Name))
                    {
                        vm.AssignToPartitionCommand.Execute(partition.Name);
                    }
                }
                // Case 2: External File Drop (from Explorer)
                else if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
                    {
                        vm.ImportFiles(files, partition.Name);
                    }
                }
                // Case 3: Partition Reordering (Dropped ONTO another partition)
                else if (e.Data.GetData(typeof(PartitionViewModel)) is PartitionViewModel sourcePartition)
                {
                     if (sourcePartition != partition)
                     {
                         // Pass the insertAfter flag
                         vm.ReorderPartition(sourcePartition, partition, isBottom);
                     }
                }
            }
        }
    }

    // Partition Reordering
    private Point _partitionDragStartPoint;

    private void PartitionHeader_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _partitionDragStartPoint = e.GetPosition(null);
    }

    private void PartitionHeader_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            Point position = e.GetPosition(null);
            if (Math.Abs(position.X - _partitionDragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(position.Y - _partitionDragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (sender is FrameworkElement element && element.DataContext is PartitionViewModel partition)
                {
                    // Start Drag
                    DragDrop.DoDragDrop(element, new DataObject(typeof(PartitionViewModel), partition), DragDropEffects.Move);
                }
            }
        }
    }

    private void PartitionHeader_Drop(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is PartitionViewModel targetPartition &&
            DataContext is FileOrganizerViewModel vm &&
            e.Data.GetData(typeof(PartitionViewModel)) is PartitionViewModel sourcePartition)
        {
            e.Handled = true; // Mark as handled so it doesn't bubble up to Partition_Drop if they overlap
            if (sourcePartition != targetPartition)
            {
                vm.ReorderPartition(sourcePartition, targetPartition);
            }
        }
    }

    private void Column_Drop(object sender, DragEventArgs e)
    {
        if (sender is Border border && border.Tag is string colStr && int.TryParse(colStr, out int targetColumn) &&
            DataContext is FileOrganizerViewModel vm &&
            e.Data.GetData(typeof(PartitionViewModel)) is PartitionViewModel sourcePartition)
        {
            e.Handled = true;
            // Move to end of target column
            vm.MovePartitionToColumn(sourcePartition, targetColumn);
        }
    }
}
