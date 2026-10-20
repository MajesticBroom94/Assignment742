using Assignment742.Domain.Enums;

namespace Assignment742.Models;

public class ActivityLogViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public ActivityType Type { get; set; }
}