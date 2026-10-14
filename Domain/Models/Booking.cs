using System.ComponentModel.DataAnnotations;
using Assignment742.Domain.Enums;

namespace Assignment742.Domain.Models;

public class Booking
{
    [Key]
    public int Id {get; set;}
    
    [Required]
    public int PropertyId {get; set;}
    
    [Required]
    public string GuestId {get; set;} = String.Empty;
    
    public DateTime CheckInDate {get; set;}
    
    public DateTime CheckOutDate {get; set;}
    
    public Decimal TotalPrice {get; set;}
    
    public BookingStatus Status {get; set;}

    public GuestCheckIn? GuestCheckIn {get; set;}
    public HostProperty Property { get; set; } = null!;
    public ApplicationUser Guest { get; set; } = null!;
    public Payment? Payment { get; set; }
}