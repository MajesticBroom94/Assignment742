using System.ComponentModel.DataAnnotations;

namespace Assignment742.Domain.Models;

public class PropertyImage
{
    [Key]
    public int Id {get; set;}
    
    [Required]
    public int PropertyId {get; set;}
    
    public string ImageUrl {get; set;} = string.Empty;

    public HostProperty Property { get; set; } = null!;
}