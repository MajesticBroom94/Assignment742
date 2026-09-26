using System.ComponentModel.DataAnnotations;
using Assignment742.Domain.Enums;

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
    public List<IFormFile> Images { get; set; } = new();

    public List<int> AmenityIds { get; set; } = new();
}