using System.Collections;
using System.Reflection;

namespace FauxData;

internal static class FauxObject
{
    /// <summary>
    /// Used for randomized data where we can reasonably assume no collisions will happen
    /// </summary>
    private static readonly Random _random = new();

    /// <summary>
    /// Creates randomized data of type <paramref name="type"/> which adheres to the given <paramref name="rules"/>.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="rules"></param>
    /// <returns></returns>
    public static object Create(Type type, IEnumerable<FauxRuleConfig> rules)
    {
        var listRules = rules as List<FauxRuleConfig> ?? rules.ToList();
        var maxDepthRule = listRules.FirstOrDefault(rule => rule is FauxRuleMaxDepthConfig) as FauxRuleMaxDepthConfig;
        var maxDepth = maxDepthRule?.Depth;
        return CreateData(type, listRules, Array.Empty<PropertyInfo>(), maxDepth)!;
    }

    /// <summary>
    /// Recursive function to create randomized data.
    /// </summary>
    /// <param name="dataType">The type to create</param>
    /// <param name="rules">Rules to follow for specific properties</param>
    /// <param name="parentProperties"></param>
    /// <param name="depthLimit"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private static object? CreateData(
        Type dataType,
        List<FauxRuleConfig> rules,
        PropertyInfo[] parentProperties,
        int? depthLimit
    )
    {
        // Immediately check for a fixed or factory rule config
        var rule = rules.FindPropertyRule(dataType, parentProperties);
        switch (rule)
        {
            case FauxRuleFixedValueConfig fixedValueRule:
                return fixedValueRule.Value;
            case FauxRuleFactoryConfig factoryRule:
                return factoryRule.Factory(dataType, parentProperties);
            // Bug: when an ignored property is used in the constructor, it cannot be ignored and should be the only way
            // we pass here -> return the default value instead
            case FauxRuleIgnoreConfig ignoreRule:
                return dataType.IsValueType ? Activator.CreateInstance(dataType) : null;
        }

        // Special behavior for list-types
        if (dataType.IsAssignableTo(typeof(IEnumerable)))
        {
            // Create a list with 2 or 3 elements
            var itemType = GetItemType(dataType) ?? typeof(object);
            var listType = typeof(List<>).MakeGenericType(itemType);
            var list = (IList)Activator.CreateInstance(listType)!;
            // Find a Count rule for this enumerable
            var countRule = rules.FindCountRule(dataType, parentProperties);
            var count = countRule?.Count ?? (depthLimit <= 0 ? 0 : _random.Next(2, 4));
            for (var i = 0; i < count; i++)
            {
                list.Add(CreateData(itemType, rules, parentProperties, depthLimit - 1));
            }

            if (!dataType.IsArray)
                return list;

            // Special behavior for Arrays since they are a bit special in C#
            var array = Array.CreateInstance(itemType, list.Count);
            for (var i = 0; i < list.Count; i++)
                array.SetValue(list[i], i);
            return array;
        }

        // It's probably a class with properties -> try to instantiate it with any available constructor
        // prioritize constructor with the least amount of parameters
        var instance = ConstructObject(dataType, rules, parentProperties, depthLimit, out var ctorProperties);

        if (depthLimit <= 0)
            return instance; // Stop enumerating if we reached a certain depth

        // Enumerate properties and set their values
        var properties = dataType.GetProperties();
        foreach (var property in properties)
        {
            // This property is already handled by the constructor, skip it
            if (ctorProperties.Any(p => p.Name == property.Name && p.PropertyType == property.PropertyType))
                continue;

            // Check how we can set the value of this property, assume we can use the property first
            var setter = GetPropertySetter(dataType, property);
            if (setter == null)
                continue;

            var propertiesList = new[]{property}.Concat(parentProperties).ToArray();
            var propertyRule = rules.FindPropertyRule(property.PropertyType, propertiesList);
            if (propertyRule is FauxRuleIgnoreConfig)
                continue;

            var data = CreateData(property.PropertyType, rules, propertiesList, depthLimit - 1);
            setter(instance, data);
        }

        // Check for setter rules
        var setterRules = rules.FindAssignRules(dataType, parentProperties);
        foreach (var setterRule in setterRules)
        {
            var objectsToSet = setterRule.ParentSelector(instance);
            foreach (var obj in objectsToSet)
            {
                // Get a new instance of the value for each object
                var valueToSet = setterRule.GetValue(instance);

                // A property list is always bottom up, we need to iterate from the parent down to the to-be-configured
                // property, so we need to reverse the array
                var topDownProperties = setterRule.Properties.Reverse().ToArray();

                var current = obj;
                for (var i = 0; i < topDownProperties.Length; i++)
                {
                    if (current == null)
                        break;

                    var property = topDownProperties[i];
                    if (i + 1 < topDownProperties.Length)
                        current = property.GetValue(objectsToSet);
                    else
                        property.SetValue(current, valueToSet);
                }
            }
        }

        return instance;
    }

    /// <summary>
    /// Constructs the given complex type using its constructor with the least amount of arguments.
    /// </summary>
    /// <param name="dataType"></param>
    /// <param name="rules"></param>
    /// <param name="parentProperties"></param>
    /// <param name="depthLimit"></param>
    /// <param name="usedProperties"></param>
    /// <returns></returns>
    private static object ConstructObject(
        Type dataType,
        List<FauxRuleConfig> rules,
        PropertyInfo[] parentProperties,
        int? depthLimit,
        out PropertyInfo[] usedProperties
    )
    {
        var ctors = dataType.GetConstructors();
        if (ctors.Length == 0)
            throw new InvalidOperationException(
                $"No constructor found for type {dataType}. If it is an abstract class, use any non-abstract child class instead or set one of its constructors public."
            );
        var ctor = ctors.MinBy(ctor => ctor.GetParameters().Length)!;
        if (ctor.GetParameters().Length == 0)
        {
            usedProperties = Array.Empty<PropertyInfo>();
            return ctor.Invoke(Array.Empty<object>());
        }

        // No parameterless constructor was found -> inspect properties of given constructor
        var usedPropertiesAsList = new List<PropertyInfo>();
        var parameters = ctor.GetParameters();
        var parameterValues = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            // Try to match the constructor parameter with an existing property by naming convention
            // e.g. the property "string name" will match the property "string Name"
            PropertyInfo? matchingProperty = null;
            if (parameter.Name != null)
            {
                // Pretty ugly code here: when the 'new' keyword is used in property declarations, the GetProperty
                // method will throw an AmbiguousMatchException when BindingFlags.Instance is set, since it will also
                // search for properties declared in base classes. We always want to prioritise properties declared in
                // the current class, so we try to find one with DeclaredOnly first, and only extend to Instance if we
                // haven't found none
                try
                {
                    matchingProperty = dataType.GetProperty(
                        parameter.Name,
                        BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.IgnoreCase
                    );
                }
                catch { }

                if (matchingProperty == null)
                {
                    try
                    {
                        matchingProperty = dataType.GetProperty(
                            parameter.Name,
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
                        );
                    }
                    catch { }
                }
            }
            if (matchingProperty == null || !IsMatchingType(matchingProperty.PropertyType, parameter.ParameterType))
            {
                // No matching property was found: simply create the data without any navigation data
                parameterValues[i] = CreateData(parameter.ParameterType, rules, parentProperties, depthLimit - 1);
                continue;
            }

            // A matching property was found: create the data and act as if the property is getting assigned
            usedPropertiesAsList.Add(matchingProperty);
            parameterValues[i] = CreateData(
                parameter.ParameterType,
                rules,
                new[] { matchingProperty }.Concat(parentProperties).ToArray(),
                depthLimit - 1
            );
        }

        usedProperties = usedPropertiesAsList.ToArray();
        return ctor.Invoke(parameterValues);
    }

    /// <summary>
    /// Get the setter for a specific property, falling back to a matching field when it cannot be written to.
    /// </summary>
    /// <param name="containingType"></param>
    /// <param name="property"></param>
    /// <returns></returns>
    private static Action<object, object?>? GetPropertySetter(Type containingType, PropertyInfo property)
    {
        if (property.CanWrite)
            return property.SetValue;

        // If the property isn't writable, search for a field by naming convention
        // E.g. for the property with name "Value", a field with name "_value" is looked up
        var field = GetField(containingType);
        if (field == null || !IsMatchingType(property.PropertyType, field.FieldType))
            return null;

        return field.SetValue;

        FieldInfo? GetField(Type type)
        {
            var result = type.GetField(
                "_" + property.Name,
                BindingFlags.Instance
                | BindingFlags.FlattenHierarchy
                | BindingFlags.NonPublic
                | BindingFlags.Public
                | BindingFlags.IgnoreCase
            );
            if (result != null)
                return result;
            var baseType = type.BaseType;
            if (baseType == null)
                return null;
            return GetField(baseType);
        }
    }

    /// <summary>
    /// Checks if <paramref name="type"/> can be used to create <paramref name="targetType"/>.
    /// </summary>
    /// <param name="targetType"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsMatchingType(Type targetType, Type type)
    {
        // When the type is assignable to the target, it's fine
        if (type.IsAssignableTo(targetType))
            return true;

        // Special behavior for enumerable types
        var iEnumerableType = typeof(IEnumerable);
        if (!targetType.IsAssignableTo(iEnumerableType) || !type.IsAssignableTo(iEnumerableType))
            return false;

        // Check their underlying type and if this is a match, this is fine too
        var containingTargetType = GetItemType(targetType);
        var containingType = GetItemType(type);
        if (containingTargetType == null && containingType == null)
            return true;
        if (containingTargetType != null && containingType != null)
            return containingType.IsAssignableTo(containingTargetType);

        return false;
    }

    /// <summary>
    /// Tries to get the item type of an <see cref="Array"/> or <see cref="IEnumerable{T}"/>
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static Type? GetItemType(Type type)
    {
        return type.GetElementType() ?? type.GetGenericArguments().FirstOrDefault();
    }

    private static bool Try(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
