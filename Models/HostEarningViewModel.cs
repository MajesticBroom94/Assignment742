namespace Assignment742.Models;


public class HostEarningsViewModel
{
    public decimal TotalEarnings { get; set; }
    public decimal MonthlyEarnings { get; set; }
    public int CompletedPayments { get; set; }

    public List<MonthlyEarningViewModel> MonthlyEarningsHistory
    { get; set; } = new();

    public List<HostPaymentViewModel> RecentPayments
    { get; set; } = new();
}

public class MonthlyEarningViewModel
{
    public string Month { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int PaymentCount { get; set; }
}

public class HostPaymentViewModel
{
    public int BookingId { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}
