namespace Assignment742.Models;

public class HostDashboardViewModel
{
    public string HostName { get; set; } = string.Empty;

    // Property statistics
    public int TotalProperties { get; set; }
    public int ActiveProperties { get; set; }

    // Booking statistics
    public int TotalBookings { get; set; }
    public int MonthlyBookings { get; set; }
    public int PendingBookings { get; set; }

    // Financial statistics
    public decimal TotalEarnings { get; set; }
    public decimal MonthlyEarnings { get; set; }

    // Review statistics
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
}
