using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusPanel.Data;
using FocusPanel.Models;
using FocusPanel.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using System.IO;

namespace FocusPanel.ViewModels;

public class KanbanColumn : ObservableObject
{
    public string Header { get; set; }
    public ObservableCollection<TodoItem> Tasks { get; set; } = new();
}

public partial class TasksViewModel : ObservableObject
{
    private readonly TaskService _taskService;
    private readonly AppDbContext _context; // Keep context alive
    private readonly SettingsService _settingsService;

    // Unified Items List (Replaces RootItems and ChildItems)
    [ObservableProperty]
    private ObservableCollection<TodoItem> currentViewItems = new();
    
    // Current Context (Parent Item). Null means we are at the Root.
    [ObservableProperty]
    private TodoItem currentParentItem;

    [ObservableProperty]
    private TodoItem selectedTask; // Selected child item for detail view

    [ObservableProperty]
    private string newTaskTitle = string.Empty;

    // View Mode Support
    [ObservableProperty]
    private bool isListView = true;

    [ObservableProperty]
    private bool isBoardView = false;
    
    [ObservableProperty]
    private bool isSettingsView = false;
    
    [ObservableProperty]
    private bool isTaskDetailView = false;
    
    // Window Management Events
    public event System.Action<TodoItem> OpenTaskDetailRequested;
    public event System.Action CloseTaskDetailRequested;
    
    // Navigation Support
    public bool CanGoBack => CurrentParentItem != null;
    public bool IsProjectSelected => CurrentParentItem != null;

    // Custom Fields Support (Context Item)
    [ObservableProperty]
    private ObservableCollection<CustomFieldDefinition> customFieldDefinitions = new();
    
    [ObservableProperty]
    private string newFieldName = string.Empty;

    [ObservableProperty]
    private string newFieldOptions = string.Empty;
    
    [ObservableProperty]
    private CustomFieldType selectedFieldType = CustomFieldType.ShortText;

    partial void OnSelectedFieldTypeChanged(CustomFieldType value)
    {
        OnPropertyChanged(nameof(IsFieldTypeSelect));
    }

    public bool IsFieldTypeSelect => SelectedFieldType == CustomFieldType.SingleSelect || SelectedFieldType == CustomFieldType.MultiSelect;

    // Custom Fields Support (Task Detail)
    [ObservableProperty]
    private ObservableCollection<CustomFieldValueViewModel> currentTaskCustomFields = new();

    // Global Settings
    [ObservableProperty]
    private string imageSavePath;

    public IEnumerable<CustomFieldType> FieldTypes => System.Enum.GetValues(typeof(CustomFieldType)).Cast<CustomFieldType>();

    public TasksViewModel()
    {
        _context = new AppDbContext();
        _taskService = new TaskService(_context);
        _settingsService = new SettingsService();
        ImageSavePath = _settingsService.CurrentSettings.ImageSavePath;
        
        LoadCurrentViewItemsCommand.Execute(null);
    }

    async partial void OnCurrentParentItemChanged(TodoItem value)
    {
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(IsProjectSelected));
        UpdateViewMode();
        await LoadCurrentViewItems();
        LoadCustomFieldDefinitions();
        CloseTaskDetail(); 
    }
    
    async partial void OnSelectedTaskChanged(TodoItem value)
    {
        if (value != null)
        {
            IsTaskDetailView = true;
            LoadCurrentTaskCustomFields(value);
            OpenTaskDetailRequested?.Invoke(value);
        }
        else
        {
            IsTaskDetailView = false;
            CloseTaskDetailRequested?.Invoke();
        }
    }

    private void UpdateViewMode()
    {
        // Reset all
        IsListView = false;
        IsBoardView = false;
        IsSettingsView = false;
        IsTaskDetailView = false;

        // Default to List if at root or parent has no preference
        if (CurrentParentItem == null) 
        {
            IsListView = true;
            return;
        }
        
        // Map enum to boolean flags
        switch (CurrentParentItem.ViewMode)
        {
            case ProjectViewMode.List:
                IsListView = true;
                break;
            case ProjectViewMode.Board:
                IsBoardView = true;
                break;
        }
    }

    [RelayCommand]
    private async Task SwitchViewMode(string mode)
    {
        IsListView = false;
        IsBoardView = false;
        IsSettingsView = false;
        IsTaskDetailView = false;
        SelectedTask = null;

        if (mode == "List")
        {
            if (CurrentParentItem != null) CurrentParentItem.ViewMode = ProjectViewMode.List;
            IsListView = true;
        }
        else if (mode == "Board")
        {
            if (CurrentParentItem != null) CurrentParentItem.ViewMode = ProjectViewMode.Board;
            IsBoardView = true;
        }
        else if (mode == "Settings")
        {
            IsSettingsView = true;
            return; 
        }

        // Save preference if we are in a context
        if (CurrentParentItem != null)
        {
            await _taskService.UpdateItemAsync(CurrentParentItem);
        }
        
        // Reload to refresh UI if needed
        if (!IsSettingsView)
        {
            await LoadCurrentViewItems();
        }
    }
    
    [RelayCommand]
    private void CloseTaskDetail()
    {
        SelectedTask = null;
        IsTaskDetailView = false;
    }

    // --- Custom Fields Logic (Definition) ---

    private void LoadCustomFieldDefinitions()
    {
        CustomFieldDefinitions.Clear();
        string json = string.Empty;

        if (CurrentParentItem != null)
        {
            json = CurrentParentItem.CustomFieldsJson;
        }
        else
        {
            // Global fields
            json = _settingsService.CurrentSettings.GlobalCustomFieldsJson;
        }

        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var fields = JsonSerializer.Deserialize<List<CustomFieldDefinition>>(json);
            if (fields != null)
            {
                foreach (var f in fields) CustomFieldDefinitions.Add(f);
            }
        }
        catch { }
    }

    [RelayCommand]
    private async Task AddCustomField()
    {
        if (string.IsNullOrWhiteSpace(NewFieldName)) return;

        var newField = new CustomFieldDefinition
        {
            Name = NewFieldName,
            Type = SelectedFieldType
        };

        if (IsFieldTypeSelect && !string.IsNullOrWhiteSpace(NewFieldOptions))
        {
            var options = NewFieldOptions.Split(new[] { ',', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries)
                                         .Select(o => o.Trim())
                                         .Where(o => !string.IsNullOrWhiteSpace(o))
                                         .ToList();
            newField.Options = options;
        }

        CustomFieldDefinitions.Add(newField);
        await SaveCustomFields();
        NewFieldName = string.Empty;
        NewFieldOptions = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteCustomField(CustomFieldDefinition field)
    {
        if (field == null) return;
        CustomFieldDefinitions.Remove(field);
        await SaveCustomFields();
    }

    private async Task SaveCustomFields()
    {
        var json = JsonSerializer.Serialize(CustomFieldDefinitions);

        if (CurrentParentItem != null)
        {
            CurrentParentItem.CustomFieldsJson = json;
            await _taskService.UpdateItemAsync(CurrentParentItem);
        }
        else
        {
            // Save Global
            _settingsService.CurrentSettings.GlobalCustomFieldsJson = json;
            _settingsService.SaveSettings();
        }
    }
    
    // --- Custom Fields Logic (Values) ---
    
    private void LoadCurrentTaskCustomFields(TodoItem task)
    {
        CurrentTaskCustomFields.Clear();
        
        // Load definitions (Project or Global)
        LoadCustomFieldDefinitions(); 
        
        // Load values
        Dictionary<string, string> values = new();
        try
        {
            if (!string.IsNullOrEmpty(task.CustomValuesJson))
                values = JsonSerializer.Deserialize<Dictionary<string, string>>(task.CustomValuesJson) ?? new();
        }
        catch { }

        foreach (var def in CustomFieldDefinitions)
        {
            string val = values.ContainsKey(def.Id) ? values[def.Id] : string.Empty;
            CurrentTaskCustomFields.Add(new CustomFieldValueViewModel(def, val, OnCustomFieldValueChanged));
        }
    }

    private async void OnCustomFieldValueChanged(string fieldId, string newValue)
    {
        if (SelectedTask == null) return;
        
        // Update JSON
        Dictionary<string, string> values = new();
        try
        {
            if (!string.IsNullOrEmpty(SelectedTask.CustomValuesJson))
                values = JsonSerializer.Deserialize<Dictionary<string, string>>(SelectedTask.CustomValuesJson) ?? new();
        }
        catch { }
        
        values[fieldId] = newValue;
        SelectedTask.CustomValuesJson = JsonSerializer.Serialize(values);
        
        // Save Task
        await _taskService.UpdateItemAsync(SelectedTask);
    }

    // --- Settings Logic ---
    
    [RelayCommand]
    private void SelectImageSavePath()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            ImageSavePath = dialog.SelectedPath;
            _settingsService.CurrentSettings.ImageSavePath = ImageSavePath;
            _settingsService.SaveSettings();
        }
    }

    // --- Image Handling for Markdown ---
    [RelayCommand]
    private void InsertImageToMarkdown(CustomFieldValueViewModel fieldViewModel)
    {
        if (fieldViewModel == null || !fieldViewModel.IsLongText) return;
        
        // Open File Dialog
        var openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp";
        if (openFileDialog.ShowDialog() == true)
        {
            try 
            {
                string savedPath = SaveImageForMarkdown(openFileDialog.FileName);
                // Insert markdown syntax
                string imageMarkdown = $"![Image]({savedPath})";
                
                // Append or Insert at cursor (cursor position tricky in MVVM, just append for now)
                if (string.IsNullOrEmpty(fieldViewModel.Value))
                    fieldViewModel.Value = imageMarkdown;
                else
                    fieldViewModel.Value += $"\n{imageMarkdown}";
            }
            catch(System.Exception ex)
            {
                MessageBox.Show($"Failed to insert image: {ex.Message}");
            }
        }
    }

    public string SaveImageForMarkdown(string sourceFilePath)
    {
        if (!Directory.Exists(ImageSavePath))
        {
            Directory.CreateDirectory(ImageSavePath);
        }

        string fileName = Path.GetFileName(sourceFilePath);
        string destPath = Path.Combine(ImageSavePath, $"{System.Guid.NewGuid()}_{fileName}");
        
        File.Copy(sourceFilePath, destPath);
        return destPath;
    }


    // --- Core Logic (Unified) ---

    [RelayCommand]
    private async Task LoadCurrentViewItems()
    {
        CurrentViewItems.Clear();
        
        List<TodoItem> items;
        if (CurrentParentItem == null)
        {
            items = await _taskService.GetRootItemsAsync();
        }
        else
        {
            items = await _taskService.GetChildItemsAsync(CurrentParentItem.Id);
        }

        foreach (var t in items)
        {
            CurrentViewItems.Add(t);
        }
    }

    [RelayCommand]
    private async Task AddItem(string status = null)
    {
        if (string.IsNullOrWhiteSpace(NewTaskTitle)) return;
        
        var targetStatus = status ?? "To Do"; 

        var task = new TodoItem
        {
            Title = NewTaskTitle,
            ParentId = CurrentParentItem?.Id, // Null if at root
            IsCompleted = false,
            Status = targetStatus,
            CreatedAt = System.DateTime.Now,
            ViewMode = ProjectViewMode.List // Default
        };

        await _taskService.AddItemAsync(task);
        
        if (IsListView)
        {
            CurrentViewItems.Insert(0, task);
        }
        else if (IsBoardView)
        {
            CurrentViewItems.Insert(0, task);
        }
        
        NewTaskTitle = string.Empty;
    }

    [RelayCommand]
    private async Task ToggleTask(TodoItem task)
    {
        if (task == null) return;
        await _taskService.UpdateItemAsync(task);
    }

    [RelayCommand]
    private async Task DeleteItem(TodoItem item)
    {
        if (item == null) return;
        if (item.Title == "Inbox" && item.ParentId == null) 
        {
            MessageBox.Show("Cannot delete Inbox.");
            return;
        }

        var result = MessageBox.Show($"Are you sure you want to delete '{item.Title}' and all its children?", 
                                     "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        await _taskService.DeleteItemAsync(item);
        
        if (IsListView)
        {
            CurrentViewItems.Remove(item);
        }
        else if (IsBoardView)
        {
            CurrentViewItems.Remove(item);
        }
        
        if (SelectedTask == item) CloseTaskDetail();
    }
    
    // --- Navigation Commands ---
    
    [RelayCommand]
    private void NavigateToItem(TodoItem item)
    {
        if (item == null) return;
        CurrentParentItem = item;
    }

    [RelayCommand]
    private async Task NavigateUp()
    {
        if (CurrentParentItem == null) return;
        
        if (CurrentParentItem.ParentId.HasValue)
        {
            if (CurrentParentItem.Parent != null)
            {
                CurrentParentItem = CurrentParentItem.Parent;
            }
            else
            {
                // Fetch parent from service if not loaded in context
                var parent = await _taskService.GetItemByIdAsync(CurrentParentItem.ParentId.Value);
                CurrentParentItem = parent;
            }
        }
        else
        {
            CurrentParentItem = null;
        }
    }

    [RelayCommand]
    private async Task UpdateCurrentContext()
    {
        if (CurrentParentItem == null) return;
        await _taskService.UpdateItemAsync(CurrentParentItem);
    }

    [RelayCommand]
    private async Task MoveTaskNext(TodoItem task)
    {
        if (task == null) return;
        
        List<string> columns;
        try 
        { 
            string json = CurrentParentItem?.ColumnsJson;
            if (string.IsNullOrEmpty(json)) 
                columns = new List<string> { "To Do", "In Progress", "Done" };
            else
                columns = JsonSerializer.Deserialize<List<string>>(json);
        }
        catch { columns = new List<string> { "To Do", "In Progress", "Done" }; }

        int currentIndex = columns.IndexOf(task.Status);
        if (currentIndex != -1 && currentIndex < columns.Count - 1)
        {
            string newStatus = columns[currentIndex + 1];
            await MoveTaskStatusLogic(task, newStatus);
        }
    }

    [RelayCommand]
    private async Task MoveTaskPrev(TodoItem task)
    {
        if (task == null) return;
        
        List<string> columns;
        try 
        { 
            string json = CurrentParentItem?.ColumnsJson;
            if (string.IsNullOrEmpty(json)) 
                columns = new List<string> { "To Do", "In Progress", "Done" };
            else
                columns = JsonSerializer.Deserialize<List<string>>(json);
        }
        catch { columns = new List<string> { "To Do", "In Progress", "Done" }; }

        int currentIndex = columns.IndexOf(task.Status);
        if (currentIndex > 0)
        {
            string newStatus = columns[currentIndex - 1];
            await MoveTaskStatusLogic(task, newStatus);
        }
    }

    private async Task MoveTaskStatusLogic(TodoItem task, string newStatus)
    {
        if (task.Status == newStatus) return;

        task.Status = newStatus;
        await _taskService.UpdateItemAsync(task);
    }
}
