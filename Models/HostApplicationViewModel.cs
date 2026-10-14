using Assignment742.Domain.Enums;

namespace Assignment742.Models;


public class HostApplicationViewModel
{
    public int ApplicationId { get; set; }
    public int PropertyId { get; set; }

    public string PropertyTitle { get; set; } = string.Empty;
    public string PropertyAddress { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;

    public string HostId { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string HostEmail { get; set; } = string.Empty;

    public DateTime AppliedAt { get; set; }
    public ApplicationStatus Status { get; set; }
}
