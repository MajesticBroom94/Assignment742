using System.ComponentModel.DataAnnotations;

namespace Assignment742.Domain.Models;

public class Amenity
{
    [Key]
    public int Id {get; set;}
    
    [StringLength(50)]
    public string Name {get; set;} = string.Empty;
    
    [StringLength(50)]
    public string IconClass {get; set;} = string.Empty;
    
    public ICollection<HostProperty> HostProperties { get; set; } = new List<HostProperty>();
}