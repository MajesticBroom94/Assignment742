namespace Assignment742.Models;

using System.ComponentModel.DataAnnotations;

public class SubmitReviewViewModel
{
    public int BookingId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    [StringLength(1000)]
    public string Comment { get; set; } = string.Empty;
}
