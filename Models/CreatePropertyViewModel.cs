using System.ComponentModel.DataAnnotations;
using Assignment742.Domain.Enums;
using Assignment742.Domain.Models;

namespace Assignment742.Models;

public class CreatePropertyViewModel
{
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
    [DataType(DataType.Currency)]
    public decimal PricePerNight { get; set; }

    [Required]
    public string Address { get; set; } = string.Empty;
    
    [Required]
    public PropertyType Type{ get; set; }

    [MinLength(1, ErrorMessage = "Property must have an image")]
    public List<IFormFile> Images { get; set; } = new();
    
    [MinLength(1, ErrorMessage = "No Amenities Selected")]
    public List<int> AmenityIds { get; set; } = new();
    
    [Required]
    [Range(1, 50)]
    public int Beds { get; set; }
    
    [Required]
    [Range(1, 50)]
    public int Bedrooms { get; set; }
    
    [Required]
    [Range(1, 50)]
    public int Bathrooms { get; set; }
    
    [Required]
    [Range(1, 50)]
    [Display(Name = "Maximum Guests")]
    public int MaxGuests { get; set; }
    
    [StringLength(200)]
    [Required]
    public string CheckInProcessTitle { get; set; } = string.Empty;
    
    [Required]
    public string CheckInProcessJson { get; set; } = "[]";
}