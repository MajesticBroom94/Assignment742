using System.ComponentModel.DataAnnotations;

namespace Assignment742.Domain.Enums;

public enum PropertyStatus
{
    Active,
    Inactive,
    Suspended,
    Rejected,
    [Display(Name = "Awaiting Approval")]
    AwaitingApproval
}