using System.ComponentModel.DataAnnotations;
using Assignment742.Domain.Enums;

namespace Assignment742.Models;

public class ChangePropertyStatusRequest
{
    [Required]
    public int? PropertyId { get; set; }
    
    [Required]
    public PropertyStatus? PropertyStatus { get; set; }
}
