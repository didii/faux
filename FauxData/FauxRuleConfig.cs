using System.Reflection;

namespace FauxData;

/// <summary>
/// Represents a single config
/// </summary>
public record FauxRuleConfig
{
    /// <summary>
    /// Delegate that checks if the given items match the selector
    /// </summary>
    public delegate bool SelectorDelegate(Type type, PropertyInfo[] properties);

    internal FauxRuleConfig(SelectorDelegate selector)
    {
        Selector = selector;
    }

    /// <summary>
    /// The rule should apply when it returns <c>true</c>. Note that the properties list is given in reverse order
    /// </summary>
    public SelectorDelegate Selector { get; }
}

/// <summary>
/// Config to ignore something
/// </summary>
public record FauxRuleIgnoreConfig : FauxRuleConfig
{
    internal FauxRuleIgnoreConfig(SelectorDelegate selector)
        : base(selector) { }
}

/// <summary>
/// Config to set a fixed value
/// </summary>
public record FauxRuleFixedValueConfig : FauxRuleConfig
{
    internal FauxRuleFixedValueConfig(SelectorDelegate selector, object? value)
        : base(selector)
    {
        Value = value;
    }

    /// <summary>
    /// The value to set when <see cref="FauxRuleConfig.Selector"/> matches
    /// </summary>
    public object? Value { get; }
}

/// <summary>
/// Config to use a factory function
/// </summary>
public record FauxRuleFactoryConfig : FauxRuleConfig
{
    /// <summary>
    /// Helper function to create a factory config for a type
    /// </summary>
    /// <param name="factory"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static FauxRuleFactoryConfig ForType<T>(Func<T?> factory) =>
        new((type, _) => type == typeof(T), (_, _) => factory());

    internal FauxRuleFactoryConfig(SelectorDelegate selector, Func<Type, PropertyInfo[], object?> factory)
        : base(selector)
    {
        Factory = factory;
    }

    /// <summary>
    /// The factory to obtain the value to set when <see cref="FauxRuleConfig.Selector"/> matches
    /// </summary>
    public Func<Type, PropertyInfo[], object?> Factory { get; }
}

/// <summary>
/// Config to set a count on something
/// </summary>
public record FauxRuleCountConfig : FauxRuleConfig
{
    internal FauxRuleCountConfig(SelectorDelegate selector, int count)
        : base(selector)
    {
        Count = count;
    }

    /// <summary>
    /// The amount of items to generate
    /// </summary>
    public int Count { get; }
}

/// <summary>
/// Config to allow assigning a value after the object has been created. Useful to enforce specific rules.
/// </summary>
public record FauxRuleAssignConfig : FauxRuleIgnoreConfig
{
    internal FauxRuleAssignConfig(
        SelectorDelegate ignoreSelector,
        Func<Type, PropertyInfo[], bool> assignSelector,
        Func<object, IEnumerable<object>> parentSelector,
        PropertyInfo[] properties,
        Func<object, object?> getValue
    )
        : base(ignoreSelector)
    {
        AssignSelector = assignSelector;
        ParentSelector = parentSelector;
        Properties = properties;
        GetValue = getValue;
    }

    /// <summary>
    /// Should return true when the assign config should actually run
    /// </summary>
    public Func<Type, PropertyInfo[], bool> AssignSelector { get; }

    /// <summary>
    /// When given the correct instance, will return a list of parents that need to be assigned to
    /// </summary>
    public Func<object, IEnumerable<object>> ParentSelector { get; }

    /// <summary>
    /// The properties to enumerate from the parent up to the values to assign
    /// </summary>
    public PropertyInfo[] Properties { get; }

    /// <summary>
    /// When given a parent, will return the value that must be assigned to the selected property
    /// </summary>
    public Func<object, object?> GetValue { get; }
}

/// <summary>
/// Config to set a global depth
/// </summary>
public record FauxRuleMaxDepthConfig : FauxRuleConfig
{
    internal FauxRuleMaxDepthConfig(int depth)
        : base((_, _) => false)
    {
        Depth = depth;
    }

    /// <summary>
    /// The maximum depth to avoid infinite recursion
    /// </summary>
    public int Depth { get; }
}
