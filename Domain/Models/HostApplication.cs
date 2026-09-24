using System.ComponentModel.DataAnnotations;
using Assignment742.Domain.Enums;

namespace Assignment742.Domain.Models;

public class HostApplication
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;
    
    [Required]
    public int PropertyId { get; set; }
    
    [Required]
    public ApplicationStatus Status { get; set; }
    
    public DateTime AppliedAt { get; set; }
}