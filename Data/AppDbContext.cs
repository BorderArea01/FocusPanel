using Microsoft.EntityFrameworkCore;
using FocusPanel.Models;

namespace FocusPanel.Data;

public class AppDbContext : DbContext
{
    public DbSet<TodoItem> Todos { get; set; }
    public DbSet<PomodoroSession> PomodoroSessions { get; set; }
    public DbSet<DesktopPartition> DesktopPartitions { get; set; }
    public DbSet<DesktopFilePreference> DesktopFilePreferences { get; set; }
    public DbSet<AppConfig> AppConfigs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string appDataPath = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), 
            "FocusPanel");
            
        if (!System.IO.Directory.Exists(appDataPath))
        {
            System.IO.Directory.CreateDirectory(appDataPath);
        }
            
        string dbPath = System.IO.Path.Combine(appDataPath, "focuspanel.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure self-referencing relationship
        modelBuilder.Entity<TodoItem>()
            .HasOne(t => t.Parent)
            .WithMany(t => t.Children)
            .HasForeignKey(t => t.ParentId)
            .IsRequired(false) // Explicitly make optional
            .OnDelete(DeleteBehavior.Cascade);
            
        // Seed default Inbox project (Root Item)
        modelBuilder.Entity<TodoItem>().HasData(
            new TodoItem 
            { 
                Id = 1, 
                Title = "Inbox", 
                ParentId = null,
                ViewMode = ProjectViewMode.List, 
                ColumnsJson = "[\"To Do\", \"Done\"]",
                IsCompleted = false,
                Status = "Active"
            }
        );
    }

    public void EnsureSchema()
    {
        // Manual migration for existing databases
        // Check if tables exist, if not create them
        try
        {
            Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS PomodoroSessions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT NOT NULL,
                    DurationMinutes INTEGER NOT NULL,
                    Status TEXT
                );
                
                CREATE TABLE IF NOT EXISTS DesktopPartitions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT,
                    OrderIndex INTEGER NOT NULL,
                    ColumnIndex INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS DesktopFilePreferences (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FilePath TEXT,
                    PartitionName TEXT
                );
                
                CREATE TABLE IF NOT EXISTS AppConfigs (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
                );
            ");

            // Migration: Add ColumnIndex if not exists
            try 
            {
                Database.ExecuteSqlRaw("ALTER TABLE DesktopPartitions ADD COLUMN ColumnIndex INTEGER DEFAULT 0;");
            } 
            catch { /* Column likely exists */ }
        }
        catch { }
    }
}
