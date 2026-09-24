using System.ComponentModel.DataAnnotations;

namespace Assignment742.Domain.Models;

public class WishlistItem
{
    [Key]
    public int Id {get; set;}
    
    [Required]
    public int UserId {get; set;}
    
    [Required]
    public int PropertyId {get; set;}
    
    public DateTime CreatedAt {get; set;}
}