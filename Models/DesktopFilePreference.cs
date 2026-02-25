using System.ComponentModel.DataAnnotations;

namespace FocusPanel.Models;

public class DesktopFilePreference
{
    [Key]
    public int Id { get; set; }
    public string FilePath { get; set; }
    public string PartitionName { get; set; }
}
