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
    public int GuestId {get; set;}
    
    public DateTime CheckInDate {get; set;}
    
    public DateTime CheckOutDate {get; set;}
    
    public Decimal TotalPrice {get; set;}
    
    public BookingStatus Status {get; set;}
    
    
}