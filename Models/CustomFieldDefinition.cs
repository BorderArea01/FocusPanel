using System.Collections.Generic;

namespace FocusPanel.Models;

public enum CustomFieldType
{
    ShortText,
    MultiSelect,
    SingleSelect,
    LongText // Markdown
}

public class CustomFieldDefinition
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    public string Name { get; set; }
    public CustomFieldType Type { get; set; }
    
    // For Select types: comma-separated or JSON string of options
    public List<string> Options { get; set; } = new List<string>();
}
