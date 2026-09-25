using System.ComponentModel.DataAnnotations;
using Assignment742.Domain.Enums;

namespace Assignment742.Domain.Models;

public class Payment
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int BookingId {get; set;}
    
    [Required]
    public decimal Amount {get; set;}
    
    public PaymentStatus Status {get; set;}
    
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;

    public Booking Booking { get; set; } = null!;
}