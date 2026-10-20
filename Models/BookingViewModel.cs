using System.ComponentModel.DataAnnotations;
using Assignment742.Domain.Enums;
using Assignment742.Domain.Models;

namespace Assignment742.Models;

public class BookingViewModel
{
    public int BookingId {get; set;}
    
    public string HostId {get; set;} = string.Empty;
    
    public DateTime BookingDate {get; set;}
    
    public string guestDetails {get; set;} = string.Empty;
    
    public string guestEmail {get; set;} = string.Empty;
    
    public string PropertyTitle { get; set; } = string.Empty;
    
    public string PropertyAddress {get; set;} = string.Empty;
    
    public DateTime CheckInDate {get; set;}
    
    public DateTime CheckOutDate {get; set;}
    
    public string HostDetails {get; set;} = string.Empty;
    
    public string HostEmail {get; set;} = string.Empty;
    
    public BookingStatus BookingStatus {get; set;}
    
    public PaymentStatus PaymentStatus {get; set;}
    
    public int? CheckInProcessId {get; set;}
    
    public CheckInStatus? CheckInStatus {get; set;}
    
    public bool HasGuestDocument { get; set; }
    
    public bool HasValidGuestDocument { get; set; }
    
    public DocumentStatus? LatestDocumentStatus { get; set; }
}