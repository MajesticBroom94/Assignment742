using Assignment742.Domain.Enums;

namespace Assignment742.Models;

public class NotificationsViewModel
{
    public int Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public NotificationType Type { get; set; }
    
    public NotificationStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}