using System.ComponentModel.DataAnnotations;

namespace Assignment742.Domain.Models;

public class CheckInProcess
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int PropertyId { get; set; }

    [Required] 
    [StringLength(200)] 
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string StepsJson { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public HostProperty Property { get; set; } = null!;
    public ICollection<GuestCheckIn> GuestCheckIns { get; set; } = new List<GuestCheckIn>();
}