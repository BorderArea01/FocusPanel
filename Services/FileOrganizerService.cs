using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FocusPanel.Helpers;
using FocusPanel.Models;

namespace FocusPanel.Services;

public class FileOrganizerService
{
    private readonly string _desktopPath;
    private readonly string _storagePath;
    private FileSystemWatcher _watcher;
    private FileSystemWatcher _storageWatcher;
    public ObservableCollection<DesktopFile> Files { get; private set; } = new ObservableCollection<DesktopFile>();

    public event Action FilesChanged;

    private System.Threading.Timer _debounceTimer;
    private const int DebounceInterval = 500; // ms

    public FileOrganizerService()
    {
        _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        // Deprecated storage path, checking for restoration
        _storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FocusPanel", "DesktopStorage");
        
        // Disable Auto-restore to prevent further damage
        // if (Directory.Exists(_storagePath)) { RestoreFiles(); }

        InitializeWatcher();
        // Initial scan
        RefreshFilesDebounced();
    }

    private void InitializeWatcher()
    {
        _watcher = new FileSystemWatcher(_desktopPath);
        _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite;
        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.EnableRaisingEvents = true;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        RefreshFilesDebounced();
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        RefreshFilesDebounced();
    }

    private void RefreshFilesDebounced()
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Threading.Timer(_ =>
        {
            Application.Current.Dispatcher.Invoke(async () => await RefreshFiles());
        }, null, DebounceInterval, System.Threading.Timeout.Infinite);
    }

    public async Task RefreshFiles()
    {
        try
        {
            var files = await Task.Run(() =>
            {
                var fileList = new List<DesktopFile>();
                
                // Scan Desktop
                if (Directory.Exists(_desktopPath))
                {
                    var dtInfo = new DirectoryInfo(_desktopPath);
                    var allFiles = dtInfo.GetFiles().Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) && !f.Name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)).ToList();
                    
                    // PERFORMANCE GUARD: If too many files, skip icon loading to prevent freeze
                    bool skipIcons = allFiles.Count > 500; 

                    foreach (var file in allFiles)
                    {
                        var desktopFile = CreateDesktopFile(file);
                        if (!skipIcons)
                        {
                            try
                            {
                                // Load icon in background thread
                                desktopFile.Icon = IconHelper.GetIcon(file.FullName, true);
                            }
                            catch {}
                        }
                        
                        fileList.Add(desktopFile);
                    }
                    foreach (var dir in dtInfo.GetDirectories())
                    {
                        if (dir.Attributes.HasFlag(FileAttributes.Hidden)) continue;
                        
                        var folderFile = CreateFolderFile(dir);
                        if (!skipIcons)
                        {
                            try
                            {
                                 folderFile.Icon = IconHelper.GetIcon(dir.FullName, true);
                            }
                            catch {}
                        }
                        
                        fileList.Add(folderFile);
                    }
                }

                return fileList.OrderByDescending(f => f.FileType == "Folder").ThenBy(f => f.Name).ToList();
            });

            // Update on UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                Files.Clear();
                foreach (var file in files)
                {
                    Files.Add(file);
                }
                FilesChanged?.Invoke();
            });
        }
        catch (Exception ex)
        {
            // Handle error (e.g. permission denied)
            System.Diagnostics.Debug.WriteLine($"Error refreshing files: {ex.Message}");
        }
    }

    private DesktopFile CreateDesktopFile(FileInfo file)
    {
        string type = "File";
        string ext = file.Extension.ToLower();
        if (new[] { ".jpg", ".png", ".gif", ".bmp", ".jpeg", ".svg", ".webp" }.Contains(ext)) type = "Image";
        else if (new[] { ".doc", ".docx", ".pdf", ".txt", ".md", ".rtf", ".xls", ".xlsx", ".ppt", ".pptx" }.Contains(ext)) type = "Document";
        else if (new[] { ".exe", ".lnk", ".msi", ".bat", ".cmd" }.Contains(ext)) type = "Application";
        else if (new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv" }.Contains(ext)) type = "Video";
        else if (new[] { ".mp3", ".wav", ".flac", ".aac" }.Contains(ext)) type = "Audio";
        else if (new[] { ".zip", ".rar", ".7z", ".tar", ".gz" }.Contains(ext)) type = "Archive";

        return new DesktopFile
        {
            Name = file.Name,
            FullPath = file.FullName,
            Extension = file.Extension,
            Size = file.Length,
            CreatedAt = file.CreationTime,
            FileType = type
        };
    }

    private DesktopFile CreateFolderFile(DirectoryInfo dir)
    {
        return new DesktopFile
        {
            Name = dir.Name,
            FullPath = dir.FullName,
            Extension = "",
            Size = 0,
            CreatedAt = dir.CreationTime,
            FileType = "Folder"
        };
    }

    public void ToggleDesktopIcons(bool show)
    {
        // Add safety try-catch and check
        try
        {
            DesktopHelper.ToggleDesktopIcons(show);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling desktop icons: {ex.Message}");
        }
    }

    public async Task RescueFiles()
    {
        await Task.Run(() =>
        {
            var desktopDir = new DirectoryInfo(_desktopPath);
            var rescueRoot = Path.Combine(_desktopPath, "FocusPanel_Recovered");
            
            if (!Directory.Exists(rescueRoot))
            {
                Directory.CreateDirectory(rescueRoot);
            }

            // If user already ran the previous "Rescue" and hates the extension folders, 
            // we should probably offer to flatten them back to rescueRoot?
            // For now, let's just ensure NEW rescue operations are flat.
            // And if we find files on desktop, we move them to rescueRoot directly.

            foreach (var file in desktopDir.GetFiles())
            {
                if (file.Attributes.HasFlag(FileAttributes.Hidden)) continue;
                if (file.Name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                if (file.Extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase)) continue; // Don't move shortcuts
                
                // NO categorization by extension. Just move to root.
                string targetPath = Path.Combine(rescueRoot, file.Name);
                
                try
                {
                     if (File.Exists(targetPath))
                     {
                         // Rename
                         string name = Path.GetFileNameWithoutExtension(file.Name);
                         string extension = file.Extension;
                         int count = 1;
                         while (File.Exists(targetPath))
                         {
                             targetPath = Path.Combine(rescueRoot, $"{name} ({count++}){extension}");
                         }
                     }
                     
                     file.MoveTo(targetPath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to rescue {file.Name}: {ex.Message}");
                }
            }
        });
        
        RefreshFilesDebounced();
    }

    private void RestoreFiles()
    {
        try
        {
            var storageDir = new DirectoryInfo(_storagePath);
            foreach (var file in storageDir.GetFiles("*", SearchOption.AllDirectories))
            {
                string targetPath = Path.Combine(_desktopPath, file.Name);
                
                // Handle duplicate names
                if (File.Exists(targetPath) || Directory.Exists(targetPath))
                {
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
                    string ext = file.Extension;
                    int count = 1;
                    while (File.Exists(targetPath) || Directory.Exists(targetPath))
                    {
                        targetPath = Path.Combine(_desktopPath, $"{nameWithoutExt} ({count++}){ext}");
                    }
                }
                
                try 
                {
                    file.MoveTo(targetPath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to restore file {file.Name}: {ex.Message}");
                }
            }

            // Also restore directories if any were moved (e.g. project folders)
            // The previous logic moved folders into "Folders" bin or kept them.
            // My previous implementation: `Directory.Move(file.FullPath, newPath);` for folders.
            // So we need to move directories back too.
            // But `GetFiles(AllDirectories)` only gets files.
            
            // We need to handle directories specifically.
            // The structure was _storagePath/Category/File or _storagePath/Category/Folder
            // So we can just iterate top-level directories in _storagePath (which are categories like Images, Folders...)
            // And then move their content.
            
            // Actually, simpler approach:
            // 1. Move all files from all subdirectories.
            // 2. Move all subdirectories from all subdirectories (that are not the category containers).
            
            // Let's refine RestoreFiles logic to be more robust.
            // Iterate category folders.
            foreach (var categoryDir in storageDir.GetDirectories())
            {
                // Move files
                foreach (var file in categoryDir.GetFiles())
                {
                     string targetPath = Path.Combine(_desktopPath, file.Name);
                     // Collision handling...
                     if (File.Exists(targetPath))
                     {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
                        string ext = file.Extension;
                        int count = 1;
                        while (File.Exists(targetPath))
                        {
                            targetPath = Path.Combine(_desktopPath, $"{nameWithoutExt} ({count++}){ext}");
                        }
                     }
                     file.MoveTo(targetPath);
                }
                
                // Move directories (e.g. Project folders inside "Folders" category)
                foreach (var dir in categoryDir.GetDirectories())
                {
                     string targetPath = Path.Combine(_desktopPath, dir.Name);
                     // Collision handling...
                     if (Directory.Exists(targetPath))
                     {
                        string name = dir.Name;
                        int count = 1;
                        while (Directory.Exists(targetPath))
                        {
                            targetPath = Path.Combine(_desktopPath, $"{name} ({count++})");
                        }
                     }
                     dir.MoveTo(targetPath);
                }
            }

            // Clean up
            storageDir.Delete(true);
        }
        catch (Exception ex)
        {
             System.Diagnostics.Debug.WriteLine($"Failed to restore files: {ex.Message}");
        }
    }
}
