using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices.JavaScript;
using Assignment742.Domain.Enums;

namespace Assignment742.Models;

public class CreateBookingViewModel
{
    [Required]
    public int PropertyId {get; set;}
    
    [Required]
    public DateTime CheckInDate {get; set;}
    
    [Required]
    public DateTime CheckOutDate {get; set;}

    [Required]
    public PaymentStatus PaymentOption { get; set; }
}