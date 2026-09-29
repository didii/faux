using System.Reflection;

namespace FauxData;

internal static class FauxRuleConfigExtensions
{
    /// <summary>
    /// Returns the first matching rule that is meant to directly set a property value
    /// </summary>
    /// <param name="source"></param>
    /// <param name="type"></param>
    /// <param name="properties"></param>
    /// <returns></returns>
    public static FauxRuleConfig? FindPropertyRule(
        this IEnumerable<FauxRuleConfig> source,
        Type type,
        PropertyInfo[] properties
    ) =>
        source.FirstOrDefault(
            // Find an ignore or fixed value rule for this property
            (provider) =>
                provider is FauxRuleIgnoreConfig or FauxRuleFixedValueConfig or FauxRuleFactoryConfig
                && provider.Selector(type, properties)
        );

    /// <summary>
    /// Finds all assign rules on a specific object. When multiple assign rules are created for the same property, only
    /// the first match is returned.
    /// </summary>
    /// <param name="rules"></param>
    /// <param name="type"></param>
    /// <param name="properties"></param>
    /// <returns></returns>
    public static IEnumerable<FauxRuleAssignConfig> FindAssignRules(
        this IEnumerable<FauxRuleConfig> rules,
        Type type,
        PropertyInfo[] properties
    )
    {
        // Ignore any assign that points to the same field already handled, we'll assume the names of the properties
        // is enough to check against
        var propertiesNamesAlreadyMatched = new List<string>();
        foreach (var rule in rules)
        {
            // Rule must be assign config and the setter should point to this property
            if (rule is not FauxRuleAssignConfig assignConfig || !assignConfig.AssignSelector(type, properties))
                continue;

            var propertiesName = assignConfig.Properties.Aggregate("", (a, b) => a + ":" + b.Name);
            if (propertiesNamesAlreadyMatched.Contains(propertiesName))
                continue;

            yield return assignConfig;
            propertiesNamesAlreadyMatched.Add(propertiesName);
        }
    }

    /// <summary>
    /// Finds the first matching count rule
    /// </summary>
    /// <param name="rules"></param>
    /// <param name="type"></param>
    /// <param name="properties"></param>
    /// <returns></returns>
    public static FauxRuleCountConfig? FindCountRule(
        this IEnumerable<FauxRuleConfig> rules,
        Type type,
        PropertyInfo[] properties
    ) =>
        rules.FirstOrDefault(rule => rule is FauxRuleCountConfig && rule.Selector(type, properties))
            as FauxRuleCountConfig;
}
