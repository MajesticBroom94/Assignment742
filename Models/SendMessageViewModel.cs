using System.ComponentModel.DataAnnotations;

namespace Assignment742.Models;

public class SendMessageViewModel
{
    [Required]
    public string ReceiverId {get; set;} = string.Empty;
    
    [Required]
    public string Content {get; set;} =  string.Empty;
    
}