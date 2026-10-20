using System.ComponentModel.DataAnnotations;
using Assignment742.Domain.Enums;
using Assignment742.Domain.Models;

namespace Assignment742.Models;

public class HostPropertyViewModel
{
    public int Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string HostId { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public decimal PricePerNight { get; set; }
    
    public string Address {get; set;} = string.Empty;
    
    public PropertyStatus Status { get; set; }
    
    public PropertyType Type { get; set; }
    
    public int Beds { get; set; }
    
    public int Bedrooms { get; set; }
    
    public int Bathrooms { get; set; }
    
    public int MaxGuests { get; set; }
    
    public int Bookings { get; set; }
    public int Reviews { get; set; }

    public string ImageUrl { get; set; } = null!;
}