using System.ComponentModel.DataAnnotations;
using Assignment742.Domain.Enums;

namespace Assignment742.Domain.Models;

public class GuestCheckIn
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int BookingId { get; set; }
    
    public int? CheckInProcessId { get; set; }
    
    public CheckInStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Booking Booking { get; set; } = null!;

    public CheckInProcess? CheckInProcess { get; set; }
    
    public GuestDocument? GuestDocument { get; set; }
}