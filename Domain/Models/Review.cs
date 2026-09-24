using System.ComponentModel.DataAnnotations;

namespace Assignment742.Domain.Models;

public class Review
{
    [Key]
    public int Id {get; set;}
    
    [Required]
    public int PropertyId {get; set;}

    [Required]
    public string ReviewerId { get; set; } = string.Empty;
    
    [Range(1,5)]
    public int Rating {get; set;}
    
    [StringLength(1000)]
    public string Comment {get; set;} =  string.Empty;
    
    public DateTime CreatedAt {get; set;}
}
