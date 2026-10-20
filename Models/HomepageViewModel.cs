namespace Assignment742.Models;

public class HomepageViewModel
{
    public int Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public decimal PricePerNight { get; set; }
    
    public string Address { get; set; } = string.Empty;

    public double? Rating { get; set; }
    
    public int? ReviewCount { get; set; }

    public string ImageUrl { get; set; } = null!;
    
    public bool IsFavorite { get; set; }
}