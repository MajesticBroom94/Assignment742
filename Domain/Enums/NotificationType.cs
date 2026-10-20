using System.ComponentModel.DataAnnotations;

namespace Assignment742.Domain.Enums;

public enum NotificationType
{
    [Display(Name = "Booking Requests")]
    BookingRequest,
    
    [Display(Name = "Bookings")]
    BookingUpdate,
    
    [Display(Name = "Review Timeline")]
    Review,
    
    [Display(Name = "Message Center")]
    Message,
    
    [Display(Name = "Host Applications")]
    HostApplication,
    
    [Display(Name = "Properties")]
    PropertyStatus,
    
    [Display(Name = "Booking Details")]
    GuestDocument,
    
    UserPromotion,
    
    CheckIn,
    CheckOut
}