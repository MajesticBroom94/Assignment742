using System.ComponentModel.DataAnnotations;
using Assignment742.Domain.Enums;

namespace Assignment742.Domain.Models;

public class GuestDocument
{
    [Key]
    public int Id { get; set; }

    [Required] 
    public int GuestCheckInId { get; set; }
    
    [StringLength(100)]
    public string DocumentType { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string FileName { get; set; } = string.Empty;
    
    public DocumentStatus Status { get; set; }

    public GuestCheckIn GuestCheckIn { get; set; } = null!;
}