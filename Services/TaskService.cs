using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FocusPanel.Data;
using FocusPanel.Models;
using Microsoft.EntityFrameworkCore;

namespace FocusPanel.Services;

public class TaskService
{
    private readonly AppDbContext _context;

    public TaskService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TodoItem>> GetTasksAsync()
    {
        return await _context.Todos.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task AddTaskAsync(TodoItem task)
    {
        _context.Todos.Add(task);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTaskAsync(TodoItem task)
    {
        _context.Todos.Update(task);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteTaskAsync(TodoItem task)
    {
        _context.Todos.Remove(task);
        await _context.SaveChangesAsync();
    }
}
