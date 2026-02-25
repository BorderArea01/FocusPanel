using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace FocusPanel.Services;

public class DatabaseBackupService
{
    private readonly string _dbPath;
    private readonly string _appDataFolder;
    private readonly string _appDataBackupFolder;
    private readonly string _localBackupFolder;
    private const int MaxBackups = 5;

    public DatabaseBackupService()
    {
        // 1. AppData Location (Main DB)
        _appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FocusPanel");
        if (!Directory.Exists(_appDataFolder)) Directory.CreateDirectory(_appDataFolder);
        
        _dbPath = Path.Combine(_appDataFolder, "focuspanel.db");

        // 2. AppData Backup Location
        _appDataBackupFolder = Path.Combine(_appDataFolder, "Backups");
        
        // 3. Local Project Backup Location (for double safety as requested)
        _localBackupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
    }

    public void PerformStartupBackup()
    {
        try
        {
            if (!File.Exists(_dbPath)) return;

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupFileName = $"focuspanel_backup_{timestamp}.db";

            // Backup 1: To AppData/Backups
            if (!Directory.Exists(_appDataBackupFolder)) Directory.CreateDirectory(_appDataBackupFolder);
            File.Copy(_dbPath, Path.Combine(_appDataBackupFolder, backupFileName), true);

            // Backup 2: To Local Project Directory/Backups
            if (!Directory.Exists(_localBackupFolder)) Directory.CreateDirectory(_localBackupFolder);
            File.Copy(_dbPath, Path.Combine(_localBackupFolder, backupFileName), true);
            
            CleanupOldBackups(_appDataBackupFolder);
            CleanupOldBackups(_localBackupFolder);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Backup failed: {ex.Message}");
        }
    }

    private void CleanupOldBackups(string folderPath)
    {
        try
        {
            var directory = new DirectoryInfo(folderPath);
            var files = directory.GetFiles("focuspanel_backup_*.db")
                               .OrderByDescending(f => f.CreationTime)
                               .ToList();

            if (files.Count > MaxBackups)
            {
                foreach (var file in files.Skip(MaxBackups))
                {
                    file.Delete();
                }
            }
        }
        catch { }
    }

    public bool ArchiveCorruptedDatabase()
    {
        try
        {
            if (File.Exists(_dbPath))
            {
                string corruptedPath = Path.Combine(_appDataFolder, $"focuspanel_corrupted_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                File.Move(_dbPath, corruptedPath);
                return true;
            }
        }
        catch { }
        return false;
    }

    public bool RestoreLatestBackup()
    {
        try
        {
            // Try to find latest backup from AppData first, then Local
            FileInfo latestBackup = GetLatestBackup(_appDataBackupFolder) ?? GetLatestBackup(_localBackupFolder);

            if (latestBackup != null)
            {
                File.Copy(latestBackup.FullName, _dbPath, true);
                return true;
            }
        }
        catch { }
        return false;
    }

    private FileInfo GetLatestBackup(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return null;
        return new DirectoryInfo(folderPath).GetFiles("focuspanel_backup_*.db")
                                          .OrderByDescending(f => f.CreationTime)
                                          .FirstOrDefault();
    }

    public List<string> GetAvailableBackups()
    {
        var backups = new List<string>();
        
        if (Directory.Exists(_appDataBackupFolder))
            backups.AddRange(Directory.GetFiles(_appDataBackupFolder, "focuspanel_backup_*.db"));
            
        if (Directory.Exists(_localBackupFolder))
            backups.AddRange(Directory.GetFiles(_localBackupFolder, "focuspanel_backup_*.db"));

        return backups.OrderByDescending(f => f).Distinct().ToList();
    }
}
