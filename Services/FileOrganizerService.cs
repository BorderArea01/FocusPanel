using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FocusPanel.Helpers;
using FocusPanel.Models;
using FocusPanel.Data;

namespace FocusPanel.Services;

public class FileOrganizerService
{
    private readonly string _desktopPath;
    private readonly string _storagePath;
    private FileSystemWatcher _watcher;
    private FileSystemWatcher _storageWatcher;

    // 完整的文件列表（包含已收纳的文件，用于面板显示）
    public ObservableCollection<DesktopFile> AllFiles { get; private set; } = new();

    // 仅桌面可见文件（未收纳的）
    public ObservableCollection<DesktopFile> Files { get; private set; } = new();

    public event Action FilesChanged;

    private System.Threading.Timer _debounceTimer;
    private const int DebounceInterval = 500; // ms

    // 用于快速查找的字典
    private readonly Dictionary<string, DesktopFile> _fileMap = new(StringComparer.OrdinalIgnoreCase);

    public FileOrganizerService()
    {
        _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        // Deprecated storage path, checking for restoration
        _storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FocusPanel", "DesktopStorage");

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

    // 从数据库加载已收纳文件的路径集合
    private HashSet<string> GetHiddenFilePaths()
    {
        var hiddenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var context = new AppDbContext();
            context.EnsureSchema();
            var prefs = context.DesktopFilePreferences
                .Where(p => p.IsHiddenFromDesktop)
                .ToList();

            foreach (var pref in prefs)
            {
                hiddenPaths.Add(pref.FilePath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading hidden files: {ex.Message}");
        }
        return hiddenPaths;
    }

    public async Task RefreshFiles()
    {
        try
        {
            var hiddenPaths = await Task.Run(() => GetHiddenFilePaths());

            var files = await Task.Run(() =>
            {
                var fileList = new List<DesktopFile>();

                // Scan Desktop
                if (Directory.Exists(_desktopPath))
                {
                    var dtInfo = new DirectoryInfo(_desktopPath);
                    var allFiles = dtInfo.GetFiles().Where(f =>
                        (!f.Attributes.HasFlag(FileAttributes.Hidden) || hiddenPaths.Contains(f.Name)) &&
                        !f.Name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)).ToList();

                    // PERFORMANCE GUARD: If too many files, skip icon loading to prevent freeze
                    bool skipIcons = allFiles.Count > 500;

                    foreach (var file in allFiles)
                    {
                        var desktopFile = CreateDesktopFile(file);

                        // 检查是否已收纳
                        desktopFile.IsHidden = hiddenPaths.Contains(file.Name);

                        if (!skipIcons)
                        {
                            try
                            {
                                // Load icon in background thread
                                desktopFile.Icon = IconHelper.GetIcon(file.FullName, true);
                            }
                            catch { }
                        }

                        fileList.Add(desktopFile);
                    }
                    foreach (var dir in dtInfo.GetDirectories())
                    {
                        if (dir.Attributes.HasFlag(FileAttributes.Hidden) && !hiddenPaths.Contains(dir.Name)) continue;

                        var folderFile = CreateFolderFile(dir);

                        // 检查是否已收纳
                        folderFile.IsHidden = hiddenPaths.Contains(dir.Name);

                        if (!skipIcons)
                        {
                            try
                            {
                                folderFile.Icon = IconHelper.GetIcon(dir.FullName, true);
                            }
                            catch { }
                        }

                        fileList.Add(folderFile);
                    }
                }

                return fileList.OrderByDescending(f => f.FileType == "Folder").ThenBy(f => f.Name).ToList();
            });

            // 增量更新：找到变化并更新，而不是全量替换
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                UpdateFilesIncremental(files, hiddenPaths);
                FilesChanged?.Invoke();
            });
        }
        catch (Exception ex)
        {
            // Handle error (e.g. permission denied)
            System.Diagnostics.Debug.WriteLine($"Error refreshing files: {ex.Message}");
        }
    }

    // 增量更新文件列表
    private void UpdateFilesIncremental(List<DesktopFile> newFiles, HashSet<string> hiddenPaths)
    {
        var newFileMap = new Dictionary<string, DesktopFile>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in newFiles)
        {
            newFileMap[f.Name] = f;
        }

        // 1. 更新现有文件和添加新文件
        for (int i = AllFiles.Count - 1; i >= 0; i--)
        {
            var existing = AllFiles[i];
            if (newFileMap.TryGetValue(existing.Name, out var newFile))
            {
                // 更新现有文件属性
                existing.Icon = newFile.Icon;
                existing.Size = newFile.Size;
                existing.IsHidden = newFile.IsHidden;
                newFileMap.Remove(existing.Name);
            }
            else
            {
                // 文件已从桌面删除
                AllFiles.RemoveAt(i);
            }
        }

        // 2. 添加新文件
        foreach (var newFile in newFileMap.Values)
        {
            AllFiles.Add(newFile);
        }

        // 3. 更新 Files 集合（仅未收纳的文件）- 桌面可见
        var visibleFiles = AllFiles.Where(f => !f.IsHidden).ToList();

        // 移除不再可见的
        for (int i = Files.Count - 1; i >= 0; i--)
        {
            var f = Files[i];
            if (!visibleFiles.Any(v => v.Name == f.Name))
            {
                Files.RemoveAt(i);
            }
        }

        // 添加新出现的可见文件
        foreach (var vf in visibleFiles)
        {
            if (!Files.Any(f => f.Name == vf.Name))
            {
                Files.Add(vf);
            }
        }

        // 更新映射字典
        _fileMap.Clear();
        foreach (var f in AllFiles)
        {
            _fileMap[f.Name] = f;
        }
    }

    // 收纳文件：设置 IsHiddenFromDesktop = true，并更新桌面图标
    public async Task HideFileFromDesktop(string fileName, string partitionName)
    {
        await Task.Run(() =>
        {
            using var context = new AppDbContext();
            context.EnsureSchema();

            var pref = context.DesktopFilePreferences.FirstOrDefault(p => p.FilePath == fileName);
            if (pref == null)
            {
                pref = new DesktopFilePreference { FilePath = fileName };
                context.DesktopFilePreferences.Add(pref);
            }

            pref.PartitionName = partitionName;
            pref.IsHiddenFromDesktop = true;

            // 如果分区不存在，创建它
            if (!string.IsNullOrEmpty(partitionName) && !context.DesktopPartitions.Any(dp => dp.Name == partitionName))
            {
                int maxOrder = context.DesktopPartitions.Any() ? context.DesktopPartitions.Max(dp => dp.OrderIndex) : -1;
                context.DesktopPartitions.Add(new DesktopPartition { Name = partitionName, OrderIndex = maxOrder + 1 });
            }

            context.SaveChanges();
        });

        // 设置文件 Hidden 属性，真正隐藏桌面图标
        try
        {
            string fullPath = Path.Combine(_desktopPath, fileName);
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
                File.SetAttributes(fullPath, File.GetAttributes(fullPath) | FileAttributes.Hidden);
        }
        catch { }

        // 更新内存中的文件状态
        if (_fileMap.TryGetValue(fileName, out var file))
        {
            file.IsHidden = true;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 从 Files 集合中移除（桌面不可见）
                var visibleFile = Files.FirstOrDefault(f => f.Name == fileName);
                if (visibleFile != null)
                {
                    Files.Remove(visibleFile);
                }

                FilesChanged?.Invoke();
            });
        }

        // 清除该文件的图标缓存，强制重新加载
        IconHelper.ClearCache(fileName);
    }

    // 取消收纳：恢复桌面显示
    public async Task RestoreFileToDesktop(string fileName)
    {
        await Task.Run(() =>
        {
            using var context = new AppDbContext();
            context.EnsureSchema();

            var pref = context.DesktopFilePreferences.FirstOrDefault(p => p.FilePath == fileName);
            if (pref != null)
            {
                pref.IsHiddenFromDesktop = false;
                context.SaveChanges();
            }
        });

        // 移除文件 Hidden 属性，恢复桌面图标
        try
        {
            string fullPath = Path.Combine(_desktopPath, fileName);
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
                File.SetAttributes(fullPath, File.GetAttributes(fullPath) & ~FileAttributes.Hidden);
        }
        catch { }

        // 更新内存中的文件状态
        if (_fileMap.TryGetValue(fileName, out var file))
        {
            file.IsHidden = false;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 添加回 Files 集合（桌面可见）
                if (!Files.Any(f => f.Name == fileName))
                {
                    Files.Add(file);
                }

                FilesChanged?.Invoke();
            });
        }
    }

    // 移动文件到其他分区（保留隐藏状态）
    public async Task MoveToPartition(string fileName, string newPartitionName)
    {
        await Task.Run(() =>
        {
            using var context = new AppDbContext();
            context.EnsureSchema();

            var pref = context.DesktopFilePreferences.FirstOrDefault(p => p.FilePath == fileName);
            if (pref == null)
            {
                pref = new DesktopFilePreference { FilePath = fileName };
                context.DesktopFilePreferences.Add(pref);
            }

            pref.PartitionName = newPartitionName;
            // 保持 IsHiddenFromDesktop 不变

            // 如果分区不存在，创建它
            if (!string.IsNullOrEmpty(newPartitionName) && !context.DesktopPartitions.Any(dp => dp.Name == newPartitionName))
            {
                int maxOrder = context.DesktopPartitions.Any() ? context.DesktopPartitions.Max(dp => dp.OrderIndex) : -1;
                context.DesktopPartitions.Add(new DesktopPartition { Name = newPartitionName, OrderIndex = maxOrder + 1 });
            }

            context.SaveChanges();
        });

        // 触发 UI 更新
        FilesChanged?.Invoke();
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

            foreach (var file in desktopDir.GetFiles())
            {
                if (file.Attributes.HasFlag(FileAttributes.Hidden)) continue;
                if (file.Name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                if (file.Extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase)) continue; // Don't move shortcuts

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

            // Also restore directories
            foreach (var categoryDir in storageDir.GetDirectories())
            {
                // Move files
                foreach (var file in categoryDir.GetFiles())
                {
                    string targetPath = Path.Combine(_desktopPath, file.Name);
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

                // Move directories
                foreach (var dir in categoryDir.GetDirectories())
                {
                    string targetPath = Path.Combine(_desktopPath, dir.Name);
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