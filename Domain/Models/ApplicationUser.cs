using System.ComponentModel.DataAnnotations;

namespace Assignment742.Domain.Models;

public class ApplicationUser
{
    [Key]
    [Required]
    public string Id { get; set; } =  string.Empty;
    
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    public string? ProfilePictureUrl { get; set; }
    
    public DateTime CreatedAt { get; set; }
}