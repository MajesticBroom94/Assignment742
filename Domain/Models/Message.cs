using System.ComponentModel.DataAnnotations;

namespace Assignment742.Domain.Models;

public class Message
{
    [Key]
    public int Id {get; set;}
    
    [Required]
    public string SenderId {get; set;} = string.Empty;
    
    [Required]
    public string ReceiverId {get; set;} = string.Empty;
    
    [Required]
    public string Content {get; set;} = string.Empty;
    
    public DateTime Timestamp {get; set;} = DateTime.UtcNow;
    
    public ApplicationUser? Sender {get; set;}
    public ApplicationUser? Receiver {get; set;}
}