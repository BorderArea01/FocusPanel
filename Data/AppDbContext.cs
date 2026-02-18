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
}
