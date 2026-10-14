using System.ComponentModel.DataAnnotations;
using Assignment742.Domain.Enums;

namespace Assignment742.Domain.Models;

public class HostProperty
{
    [Key]
    public int Id { get; set; }
    
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public decimal PricePerNight { get; set; }
    
    [Required]
    public string HostId { get; set; } = string.Empty;
    
    [Required]
    public string Address {get; set;} = string.Empty;
    
    [Required]
    [Range(1, int.MaxValue)]
    public int Beds { get; set; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int Bedrooms { get; set; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int Bathrooms { get; set; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int MaxGuests { get; set; }
    
    public PropertyStatus Status { get; set; }
    
    [Required]
    public PropertyType Type { get; set; }
    
    public CheckInProcess? CheckInProcess { get; set; }
    public ApplicationUser Host { get; set; } = null!;
    
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<HostApplication> HostApplications { get; set; } = new List<HostApplication>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public ICollection<PropertyImage> PropertyImages { get; set; } = new List<PropertyImage>();
    
    public ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
}