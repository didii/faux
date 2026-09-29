using System.Reflection;

namespace FauxData;

internal static class PropertyInfoExtensions
{
    public static bool Matches(this PropertyInfo lhs, PropertyInfo rhs)
    {
        if (lhs == rhs)
            return true;

        return lhs.DeclaringType == rhs.DeclaringType && lhs.PropertyType == rhs.PropertyType && lhs.Name == rhs.Name;
    }
}
