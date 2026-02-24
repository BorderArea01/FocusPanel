using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusPanel.Models;
using FocusPanel.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Linq;
using System.Collections.Generic;
using System;

namespace FocusPanel.ViewModels;

public partial class FileOrganizerViewModel : ObservableObject
{
    private readonly FileOrganizerService _fileService;
    private readonly SettingsService _settingsService;

    // We no longer use FilesView with grouping. We use a list of partitions.
    public ObservableCollection<PartitionViewModel> Partitions { get; } = new();

    [ObservableProperty]
    private bool isPersonalizedView = true;

    [ObservableProperty]
    private string currentViewMode = "Personalized"; // "Personalized" or "Timeline"
    
    [ObservableProperty]
    private string newPartitionName;
    
    [ObservableProperty]
    private DesktopFile selectedFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OrganizeButtonText))]
    [NotifyPropertyChangedFor(nameof(OrganizeButtonIcon))]
    private bool isDesktopHidden;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CardWidth))]
    [NotifyPropertyChangedFor(nameof(CardHeight))]
    [NotifyPropertyChangedFor(nameof(IconImageSize))]
    private double iconScale = 1.0;

    [ObservableProperty]
    private int partitionColumns = 1;

    public double CardWidth => 100 * IconScale;
    public double CardHeight => 120 * IconScale;
    public double IconImageSize => 48 * IconScale;

    public string OrganizeButtonText => IsDesktopHidden ? "Show Desktop" : "Hide Desktop";
    public string OrganizeButtonIcon => IsDesktopHidden ? "Eye" : "EyeOff";

    public FileOrganizerViewModel()
    {
        _settingsService = new SettingsService();
        _fileService = new FileOrganizerService();
        
        // Listen for file updates
        _fileService.FilesChanged += () => 
        {
            // Rebuild partitions on file change
            System.Windows.Application.Current.Dispatcher.Invoke(BuildPartitions);
        };

        // Check initial desktop state
        try
        {
            IsDesktopHidden = !FocusPanel.Helpers.DesktopHelper.IsDesktopIconsVisible();
        }
        catch
        {
            IsDesktopHidden = false; // Default safe value
        }

        // Initial Build
        BuildPartitions();
    }

    private void BuildPartitions()
    {
        Partitions.Clear();
        var allFiles = _fileService.Files;

        // Apply Custom Partition Metadata first (ensure model is up to date)
        ApplyCustomPartitionsMetadata();

        if (IsPersonalizedView)
        {
            // 1. Create User Defined Partitions
            var customNames = _settingsService.CurrentSettings.CustomPartitionNames ?? new List<string>();
            var partitionMap = new Dictionary<string, PartitionViewModel>();

            foreach (var name in customNames)
            {
                var p = new PartitionViewModel(name) { IsCustom = true };
                partitionMap[name] = p;
                Partitions.Add(p);
            }

            // 2. Distribute Files
            var uncategorizedFiles = new List<DesktopFile>();

            foreach (var file in allFiles)
            {
                if (!string.IsNullOrEmpty(file.CustomPartition) && partitionMap.ContainsKey(file.CustomPartition))
                {
                    partitionMap[file.CustomPartition].Files.Add(file);
                }
                else
                {
                    uncategorizedFiles.Add(file);
                }
            }

            // 3. Create Default Categories for Uncategorized Files
            var categoryGroups = uncategorizedFiles.GroupBy(f => f.FileType).OrderBy(g => g.Key);
            foreach (var group in categoryGroups)
            {
                var p = new PartitionViewModel(group.Key) { IsCustom = false };
                foreach (var file in group)
                {
                    p.Files.Add(file);
                }
                Partitions.Add(p);
            }
        }
        else // Timeline View
        {
            var dateGroups = allFiles.GroupBy(f => f.DateGroup)
                                     .OrderBy(g => GetDateGroupSortOrder(g.Key)); // Need a sort helper

            foreach (var group in dateGroups)
            {
                var p = new PartitionViewModel(group.Key) { IsCustom = false };
                foreach (var file in group.OrderByDescending(f => f.CreatedAt))
                {
                    p.Files.Add(file);
                }
                Partitions.Add(p);
            }
        }
    }

    private int GetDateGroupSortOrder(string groupName)
    {
        return groupName switch
        {
            "Today" => 0,
            "Yesterday" => 1,
            "This Week" => 2,
            "This Month" => 3,
            "Older" => 4,
            _ => 5
        };
    }

    private void ApplyCustomPartitionsMetadata()
    {
        var partitions = _settingsService.CurrentSettings.FilePartitions;
        if (partitions == null) return;

        foreach (var file in _fileService.Files)
        {
            if (partitions.TryGetValue(file.Name, out var partition))
            {
                file.CustomPartition = partition;
            }
            else
            {
                file.CustomPartition = null;
            }
        }
    }

    [RelayCommand]
    private void ToggleView()
    {
        IsPersonalizedView = !IsPersonalizedView;
        CurrentViewMode = IsPersonalizedView ? "Personalized" : "Timeline";
        BuildPartitions();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await _fileService.RefreshFiles();
        BuildPartitions();
    }

    [RelayCommand]
    private void ToggleDesktop()
    {
        // Toggle Desktop Icons visibility
        IsDesktopHidden = !IsDesktopHidden;
        _fileService.ToggleDesktopIcons(!IsDesktopHidden);
    }
    
    [RelayCommand]
    private async Task Rescue()
    {
        var result = System.Windows.MessageBox.Show(
            "This will move all loose files on your desktop into a single 'FocusPanel_Recovered' folder.\n\nNo categorization will be applied. Shortcuts and folders will be skipped.\n\nContinue?", 
            "Rescue Desktop", 
            System.Windows.MessageBoxButton.YesNo, 
            System.Windows.MessageBoxImage.Warning);
            
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            await _fileService.RescueFiles();
        }
    }

    [RelayCommand]
    private async Task SmartRescue()
    {
        // 1. Ask user for confirmation
        var result = System.Windows.MessageBox.Show(
            "Smart Rescue will analyze all files in 'FocusPanel_Recovered' and group them by Time Sessions (4h gap) and Name Similarity.\n\nThis is designed to restore project context.\n\nContinue?", 
            "Smart Rescue", 
            System.Windows.MessageBoxButton.YesNo, 
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        // 2. Run Smart Organizer
        var smartOrganizer = new SmartOrganizerService();
        
        // TODO: Bind progress to UI if needed, for now just run
        string recoveredPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
            "FocusPanel_Recovered");
            
        if (!System.IO.Directory.Exists(recoveredPath))
        {
            System.Windows.MessageBox.Show("No 'FocusPanel_Recovered' folder found on Desktop.");
            return;
        }

        await smartOrganizer.OrganizeByRelevance(recoveredPath);
        
        System.Windows.MessageBox.Show("Smart Organization Complete! Check the 'FocusPanel_Recovered' folder.");
    }
    
    [RelayCommand]
    private void CreatePartition(string name = null) // Can be called with parameter or bound property
    {
        string partitionName = name ?? NewPartitionName;
        
        if (string.IsNullOrWhiteSpace(partitionName)) return;
        
        // Add to settings if not exists
        if (!_settingsService.CurrentSettings.CustomPartitionNames.Contains(partitionName))
        {
            _settingsService.CurrentSettings.CustomPartitionNames.Add(partitionName);
            _settingsService.SaveSettings();
            
            // Rebuild to show new empty partition
            if (IsPersonalizedView)
            {
                BuildPartitions();
            }
            else
            {
                // Switch to Personalized view to see it?
                ToggleView(); 
            }
        }
        
        NewPartitionName = string.Empty;
    }
    
    [RelayCommand]
    private void DeletePartition(PartitionViewModel partition)
    {
        if (partition == null) return;

        // Remove from settings
        if (_settingsService.CurrentSettings.CustomPartitionNames.Contains(partition.Name))
        {
            _settingsService.CurrentSettings.CustomPartitionNames.Remove(partition.Name);
        }

        // Unassign files
        foreach (var file in partition.Files)
        {
            if (_settingsService.CurrentSettings.FilePartitions.ContainsKey(file.Name))
            {
                _settingsService.CurrentSettings.FilePartitions.Remove(file.Name);
            }
            file.CustomPartition = null;
        }

        _settingsService.SaveSettings();
        BuildPartitions();
    }

    [RelayCommand]
    private void SelectFile(DesktopFile file)
    {
        if (file == null) return;
        if (SelectedFile != null) SelectedFile.IsSelected = false;
        SelectedFile = file;
        SelectedFile.IsSelected = true;
    }

    [RelayCommand]
    private void OpenFile(DesktopFile file)
    {
        if (file == null || string.IsNullOrEmpty(file.FullPath)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file.FullPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            // Handle error
            System.Diagnostics.Debug.WriteLine($"Failed to open file: {ex.Message}");
        }
    }

    [RelayCommand]
    private void AssignToPartition(string partitionName) // Called from context menu usually
    {
        if (SelectedFile == null) return;
        
        // Update Model
        if (string.IsNullOrEmpty(partitionName))
        {
             if (_settingsService.CurrentSettings.FilePartitions.ContainsKey(SelectedFile.Name))
                _settingsService.CurrentSettings.FilePartitions.Remove(SelectedFile.Name);
        }
        else
        {
             _settingsService.CurrentSettings.FilePartitions[SelectedFile.Name] = partitionName;
             
             // Ensure partition exists in list too (auto-create if assigning to new name)
             if (!_settingsService.CurrentSettings.CustomPartitionNames.Contains(partitionName))
             {
                 _settingsService.CurrentSettings.CustomPartitionNames.Add(partitionName);
             }
        }
        
        _settingsService.SaveSettings();
        BuildPartitions();
    }
}
