namespace Assignment742.Models;

public class HostPropertyReviewViewModel
{
    public string PropertyTitle { get; set; } = string.Empty;
    
    public string Reviewer { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}