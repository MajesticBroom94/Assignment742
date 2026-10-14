namespace Assignment742.Models;

public class MessageViewModel
{
    public string SenderId {get; set;} = string.Empty;
    
    public string SenderDetails { get; set; } = string.Empty;
    
    public string ReceiverId { get; set; } = string.Empty;
    
    public string ReceiverDetails { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

