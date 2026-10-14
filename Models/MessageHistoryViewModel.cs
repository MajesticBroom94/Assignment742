namespace Assignment742.Models;

public class MessageHistoryViewModel
{
    public string ContactDetails { get; set; } = string.Empty;
    
    public string ContactProfilePicture { get; set; } = string.Empty;
    
    public string ContactId { get; set; } = string.Empty;

    public DateTime LastMessage { get; set; }
}