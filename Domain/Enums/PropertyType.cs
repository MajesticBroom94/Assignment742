using System.ComponentModel.DataAnnotations;

namespace Assignment742.Domain.Enums;

public enum PropertyType
{
    House,
    Apartment,
    Cottage,
    [Display(Name = "Guest House")]
    Guesthouse,
    Lodge,
    Farm,
    Villa,
    Bungalow,
    Cabin
}