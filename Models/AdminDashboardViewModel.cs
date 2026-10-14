using Assignment742.Domain.Enums;

namespace Assignment742.Models;

public class AdminDashboardViewModel
{
    // Platform statistics
    public int TotalUsers { get; set; }
    public int PendingHostApplications { get; set; }
    public int ActiveProperties { get; set; }
    public decimal PlatformRevenue { get; set; }
    public int TotalBookings { get; set; }
    public int PendingBookings { get; set; }
    public int ApprovedHosts { get; set; }

    // Tables
    public List<DashboardActivityViewModel> RecentActivities { get; set; } = new();

    public List<DashboardHostApplicationViewModel> RecentApplications { get; set; } = new();

    public List<DashboardBookingViewModel> UpcomingBookings { get; set; } = new();
}

public class DashboardActivityViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public ActivityType Type { get; set; }
}

public class DashboardHostApplicationViewModel
{
    public int ApplicationId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
    public ApplicationStatus Status { get; set; }
}

public class DashboardBookingViewModel
{
    public int BookingId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public BookingStatus Status { get; set; }
}
