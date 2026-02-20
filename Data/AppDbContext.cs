using Microsoft.EntityFrameworkCore;
using FocusPanel.Models;

namespace FocusPanel.Data;

public class AppDbContext : DbContext
{
    public DbSet<TodoItem> Todos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=focuspanel.db");
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
}
