using System.ComponentModel.DataAnnotations;

namespace Assignment742.Domain.Models;

public class HostProperty
{
    [Key]
    public int Id { get; set; }
    
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public decimal PricePerNight { get; set; }
    
    [Required]
    public string HostId { get; set; } = string.Empty;
    
    [Required]
    public string Address {get; set;} = string.Empty;
    
    
    
}