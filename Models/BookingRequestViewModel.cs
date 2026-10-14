using System.ComponentModel.DataAnnotations;
using Assignment742.Domain.Enums;

namespace Assignment742.Models;

public class BookingRequestViewModel
{
    public int BookingId {get; set;}
    
    public string GuestId {get; set;} = string.Empty;
    
    public string PropertyTitle { get; set; } = string.Empty;
    
    public string PropertyAddress {get; set;} = string.Empty;
    
    public DateTime CheckInDate {get; set;}
    
    public DateTime CheckOutDate {get; set;}
    
    public string GuestDetails {get; set;} = string.Empty;
    
    public string GuestEmail {get; set;} = string.Empty;
    
    [Display(Name =  "Payment Status")]
    public PaymentStatus PaymentStatus {get; set;}
}