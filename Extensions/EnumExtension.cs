using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Assignment742.Extensions;

public static class EnumExtension
{
    public static string GetDisplayName(Enum value)
    {
        return value.GetType()
            .GetMember(value.ToString())
            .First()
            .GetCustomAttribute<DisplayAttribute>()?
            .GetName() ?? value.ToString();
    }
}
