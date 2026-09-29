namespace FauxData;

/// <summary>
/// Allows a user to create randomized data
/// </summary>
public class Faux : FauxRuleBuilder
{
    /// <summary>
    /// Construct Faux with default rules or the given ones.
    /// </summary>
    /// <param name="rules"></param>
    public Faux(IEnumerable<FauxRuleConfig>? rules = null)
        : base(rules ?? FauxDefaults.Rules) { }

    /// <summary>
    /// Creates a single model of the given type
    /// </summary>
    /// <param name="type"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public object Model(Type type, Action<FauxTypedRuleBuilder<object, object>>? config = null)
    {
        var rules = BuildRules(config);
        return FauxObject.Create(type, rules);
    }

    /// <summary>
    /// Creates a single model of the given type
    /// </summary>
    /// <param name="config">Additional configuration for specifically this model</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T Model<T>(Action<FauxTypedRuleBuilder<T, T>>? config = null)
    {
        var rules = BuildRules(config);
        return (T)FauxObject.Create(typeof(T), rules);
    }

    /// <summary>
    /// Creates a list of models of the given type
    /// </summary>
    /// <param name="count">Amount of models to generate</param>
    /// <param name="config">Additional configuration for specifically this model</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public List<T> Model<T>(int count, Action<FauxTypedRuleBuilder<T, T>>? config = null)
    {
        var rules = BuildRules(config);

        var result = new List<T>();
        for (var i = 0; i < count; i++)
            result.Add((T)FauxObject.Create(typeof(T), rules));
        return result;
    }

    /// <summary>
    /// Creates a single model of the given type, that initializes all fields with default values
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public object Pas(Type type)
    {
        var rules = FauxDefaults.PasRules;
        return FauxObject.Create(type, rules);
    }

    /// <summary>
    /// Creates a single model of the given type, that initializes all fields with default values
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T Pas<T>() => (T)Pas(typeof(T));

    private List<FauxRuleConfig> BuildRules<T>(Action<FauxTypedRuleBuilder<T, T>>? config = null)
    {
        var ruleBuilder = new FauxRuleBuilder(Array.Empty<FauxRuleConfig>()).ForRoot<T>();
        config?.Invoke(ruleBuilder);

        return ruleBuilder.Rules.Concat(Rules).ToList();
    }
}
