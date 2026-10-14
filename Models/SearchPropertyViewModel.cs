using Assignment742.Domain.Enums;

namespace Assignment742.Models;

public class SearchPropertyViewModel
{
    public string? Address { get; set; } = string.Empty;
    public int? GuestCount { get; set; } = 0;
    public PropertyType? Type { get; set; }
}