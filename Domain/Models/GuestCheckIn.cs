using System.ComponentModel.DataAnnotations;
using Assignment742.Domain.Enums;

namespace Assignment742.Domain.Models;

public class GuestCheckIn
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int BookingId { get; set; }
    
    [Required]
    public int CheckInProcessId { get; set; }
    
    public CheckInStatus Status { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; }
    
}