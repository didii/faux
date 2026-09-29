using System.Reflection;

namespace FauxData;

/// <summary>
/// Useful extensions on <see cref="Faux"/>
/// </summary>
public static class FauxExtensions
{
    /// <summary>
    /// Ignores by default all properties <c>Id</c>
    /// </summary>
    /// <param name="faux"></param>
    /// <returns></returns>
    public static Faux IgnoreIdProps(this Faux faux)
    {
        faux.Ignore((_, props) =>
            {
                if (props.Length == 0)
                    return false;

                // Ignore all "int Id" properties so EF doesn't get confused
                return props[0].Name == "Id"
                       && (props[0].PropertyType == typeof(int) || props[0].PropertyType == typeof(long) ||
                           props[0].PropertyType == typeof(Guid));
            }
        );
        return faux;
    }

    /// <summary>
    /// Ignores all properties that are tagged by the <see cref="ObsoleteAttribute"/>
    /// </summary>
    /// <param name="faux"></param>
    /// <returns></returns>
    public static Faux IgnoreObsoleteProps(this Faux faux)
    {
        faux.Ignore((_, props) =>
            {
                if (props.Length == 0)
                    return false;

                var obsoleteAttribute = props[0].GetCustomAttribute<ObsoleteAttribute>();
                return obsoleteAttribute != null;
            }
        );
        return faux;
    }
}
