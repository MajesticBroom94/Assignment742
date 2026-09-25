using System.ComponentModel.DataAnnotations;
using Assignment742.Domain.Enums;

namespace Assignment742.Domain.Models;

public class Notification
{
    [Key]
    public string Id { get; set; } = string.Empty;
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public NotificationType Type { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ApplicationUser User { get; set; } = null!;
}