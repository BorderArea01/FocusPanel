using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using System.Linq;

namespace FocusPanel.Models;

public enum ProjectViewMode
{
    List,
    Board
}

public class TodoItem
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public bool IsCompleted { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Recursive Relationship (Self-Referencing)
    public int? ParentId { get; set; }
    
    public virtual TodoItem Parent { get; set; }
    
    public virtual ICollection<TodoItem> Children { get; set; } = new List<TodoItem>();

    // --- Task-Specific Properties ---
    
    // Current status in Kanban board (e.g., "To Do", "In Progress")
    public string Status { get; set; } = "To Do";

    // JSON string storing values for custom fields
    // e.g. {"Priority": "High", "Due Date": "2023-12-31"}
    public string CustomValuesJson { get; set; } = "{}";


    // --- Project-Specific Properties (Used when ParentId is null) ---

    public ProjectViewMode ViewMode { get; set; } = ProjectViewMode.List;
    
    // JSON string storing column definitions for Kanban (e.g., ["To Do", "In Progress", "Done"])
    // Also serves as the schema for status options.
    public string ColumnsJson { get; set; } = "[\"To Do\", \"In Progress\", \"Done\"]";

    // JSON string storing custom field definitions
    // e.g. [{"Name":"Priority", "Type":"Select", "Options":["High","Low"]}, {"Name":"Due Date", "Type":"Date"}]
    public string CustomFieldsJson { get; set; } = "[]";

    [NotMapped]
    public string CoverImage
    {
        get
        {
            if (string.IsNullOrEmpty(CustomValuesJson)) return null;
            
            // Very simple extraction: look for the first markdown image syntax
            // Regex to find ![alt](url)
            var regex = new Regex(@"!\[.*?\]\((.*?)\)");
            
            // We need to parse the JSON values to find long text fields
            // For simplicity/performance, we can just search the raw JSON string if we assume the image path structure is unique enough
            // But better to iterate values. However, we don't have definitions here easily.
            // Let's just regex the whole JSON string for markdown image pattern.
            // It might match other things, but likely it's what we want.
            
            var match = regex.Match(CustomValuesJson);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            return null;
        }
    }
}
