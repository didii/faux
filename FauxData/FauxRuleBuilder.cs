using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace FauxData;

/// <summary>
/// The rule engine that <see cref="Faux"/> works on
/// </summary>
public class FauxRuleBuilder
{
    /// <summary>
    /// Optional parent to which the rules should be applied. Is <c>null</c> for the root builder.
    /// </summary>
    private readonly FauxRuleBuilder? _parent;

    /// <summary>
    /// Only the parent has a list of rules, all children pass their rules to their parent
    /// </summary>
    private readonly List<FauxRuleConfig>? _rules;

    /// <summary>
    /// The actual rules this builder represents, regardless of parent
    /// </summary>
    public IReadOnlyList<FauxRuleConfig> Rules => _parent?.Rules ?? _rules!.AsReadOnly();

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="rules"></param>
    /// <param name="parent"></param>
    public FauxRuleBuilder(IEnumerable<FauxRuleConfig> rules, FauxRuleBuilder? parent = null)
    {
        _rules = rules.ToList();
        _parent = parent;
    }

    /// <summary>
    /// A global config where if a property is created at a certain depth, it will stop iterating its properties to
    /// avoid an infinite loop.
    /// </summary>
    /// <param name="depth"></param>
    /// <returns></returns>
    public FauxRuleBuilder MaxDepth(int depth)
    {
        InsertRule(new FauxRuleMaxDepthConfig(depth));
        return this;
    }

    /// <summary>
    /// Rule for ignoring certain properties. The list of properties are given in <b>reverse</b> order, i.e. the most
    /// nested property is the first on in the array.
    /// </summary>
    /// <param name="valueSelector">Should return true if this property should be ignored, false otherwise.</param>
    /// <returns></returns>
    public FauxRuleBuilder Ignore(FauxRuleConfig.SelectorDelegate valueSelector)
    {
        InsertRule(new FauxRuleIgnoreConfig(valueSelector));
        return this;
    }

    /// <summary>
    /// Rule for setting an explicit value for a certain property. The list of properties are given in <b>reverse</b>
    /// order, i.e. the most nested property is the first on in the array.
    /// </summary>
    /// <param name="valueSelector">Should return true if this property should be ignored, false otherwise.</param>
    /// <param name="value">The value to set instead of a generated one</param>
    /// <returns></returns>
    public FauxRuleBuilder Set(FauxRuleConfig.SelectorDelegate valueSelector, object value)
    {
        InsertRule(new FauxRuleFixedValueConfig(valueSelector, value));
        return this;
    }

    /// <summary>
    /// Specifies how to create any type that matches the given <paramref name="typePredicate"/>.
    /// </summary>
    /// <param name="typePredicate"></param>
    /// <param name="factory"></param>
    /// <returns></returns>
    public FauxRuleBuilder TypeFactory(Func<Type, bool> typePredicate, Func<Type, object?> factory)
    {
        InsertRule(new FauxRuleFactoryConfig((t, _) => typePredicate(t), (type, _) => factory(type)));
        return this;
    }

    /// <summary>
    /// Specify how to create a specific type.
    /// </summary>
    /// <param name="factory"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public FauxRuleBuilder TypeFactory<T>(Func<T> factory)
    {
        InsertRule(FauxRuleFactoryConfig.ForType<T>(() => factory()));
        return this;
    }

    /// <summary>
    /// Start configuring all properties of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public FauxTypedRuleBuilder<T, T> ForType<T>()
    {
        return new FauxTypedRuleBuilder<T, T>(this, exact: false, Array.Empty<PropertyInfo>(), x => new[] { x });
    }

    /// <summary>
    /// Start configuring properties of type <typeparamref name="T"/>, but overwrite the root to this type. This means
    /// that any property expression only applies when this type is explicitly built and
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public FauxTypedRuleBuilder<T, T> ForRoot<T>()
    {
        return new FauxTypedRuleBuilder<T, T>(this, exact: true, Array.Empty<PropertyInfo>(), x => new[] { x });
    }

    /// <summary>
    /// Inserts a rule onto its parent, or onto itself when no parent was found. This will always prepend a rule so the
    /// last specified rule wins.
    /// </summary>
    /// <param name="rule"></param>
    protected void InsertRule(FauxRuleConfig rule)
    {
        if (_parent != null)
        {
            _parent.InsertRule(rule);
            return;
        }

        _rules!.Insert(0, rule);
    }
}

/// <summary>
/// The rule engine <see cref="Faux"/> runs on, specifically tailored to work on the type <typeparamref name="TRoot"/>
/// while configuring <typeparamref name="TType"/>.
/// </summary>
/// <typeparam name="TRoot"></typeparam>
/// <typeparam name="TType"></typeparam>
public class FauxTypedRuleBuilder<TRoot, TType> : FauxRuleBuilder
{
    private readonly bool _exact;
    private readonly PropertyInfo[] _fromRootProperties;
    private readonly Func<TRoot, IEnumerable<TType>> _fromRootGetter;

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="parent">Parent rule builder to hoist settings into</param>
    /// <param name="exact">True to compare the entire property tree, otherwise a partial match is accepted</param>
    /// <param name="fromRootProperties"></param>
    /// <param name="fromRootGetter"></param>
    public FauxTypedRuleBuilder(
        FauxRuleBuilder parent,
        bool exact,
        PropertyInfo[] fromRootProperties,
        Func<TRoot, IEnumerable<TType>> fromRootGetter
    )
        : base(Array.Empty<FauxRuleConfig>(), parent)
    {
        _exact = exact;
        _fromRootProperties = fromRootProperties;
        _fromRootGetter = fromRootGetter;
    }

    /// <summary>
    /// Ignore a specific prop on a type. Nested expressions are supported.
    /// </summary>
    /// <param name="expression"></param>
    /// <typeparam name="TProp"></typeparam>
    /// <returns></returns>
    public FauxTypedRuleBuilder<TRoot, TType> Ignore<TProp>(Expression<Func<TType, TProp>> expression)
    {
        var props = expression.GetPropertyInfos().Concat(_fromRootProperties).ToArray();
        InsertRule(new FauxRuleIgnoreConfig(GetPropertySelector(typeof(TProp), props)));
        return this;
    }

    /// <summary>
    /// Set the prop to a specific value. Nested expressions are supported.
    /// </summary>
    /// <example>
    /// This will assign the value <c>5</c> to every <c>Address.CountryId</c> property.
    /// <code>
    /// new FauxRuleBuilder()
    ///     .For&lt;Address>()
    ///     .Set(address => address.CountryId, 5);
    /// </code>
    /// While this code will set the <c>Address.CountryId</c> for departure to 75 and for destination to 25.
    /// <code>
    /// new FauxRuleBuilder()
    ///     .For&lt;Order>()
    ///     .Set(order => order.DepartureAddress.CountryId, 75)
    ///     .Set(order => order.DestinationAddress.CountryId, 25);
    /// </code>
    /// </example>
    /// <param name="expression"></param>
    /// <param name="value"></param>
    /// <typeparam name="TProp"></typeparam>
    /// <returns></returns>
    public FauxTypedRuleBuilder<TRoot, TType> Set<TProp>(Expression<Func<TType, TProp>> expression, TProp value)
    {
        var props = expression.GetPropertyInfos().Concat(_fromRootProperties).ToArray();
        InsertRule(new FauxRuleFixedValueConfig(GetPropertySelector(typeof(TProp), props), value));
        return this;
    }

    /// <summary>
    /// Registers a factory for a specific prop. The factory function will be called each time the prop is assigned.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="factory"></param>
    /// <typeparam name="TProp"></typeparam>
    /// <returns></returns>
    public FauxTypedRuleBuilder<TRoot, TType> Factory<TProp>(
        Expression<Func<TType, TProp>> expression,
        Func<TProp> factory
    )
    {
        var props = expression.GetPropertyInfos().Concat(_fromRootProperties).ToArray();
        InsertRule(new FauxRuleFactoryConfig(GetPropertySelector(typeof(TProp), props), (_, _) => factory()));
        return this;
    }

    /// <summary>
    /// Set the collection prop to a specific size without needing to specify the contents
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="count"></param>
    /// <typeparam name="TProp"></typeparam>
    /// <returns></returns>
    public FauxTypedRuleBuilder<TRoot, TType> Count<TProp>(Expression<Func<TType, TProp>> expression, int count)
        where TProp : IEnumerable
    {
        var props = expression.GetPropertyInfos().Concat(_fromRootProperties).ToArray();
        InsertRule(new FauxRuleCountConfig(GetPropertySelector(typeof(TProp), props), count));
        return this;
    }

    /// <summary>
    /// Set the collection prop to a specific size without needing to specify the contents
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="count">Exact number of characters</param>
    /// <returns></returns>
    public FauxTypedRuleBuilder<TRoot, TType> Count(Expression<Func<TType, string?>> expression, int count)
    {
        var props = expression.GetPropertyInfos().Concat(_fromRootProperties).ToArray();
        InsertRule(
            new FauxRuleFactoryConfig(
                GetPropertySelector(typeof(string), props),
                (_, _) => FauxDefaults.Random.GetString(FauxDefaults.AllowedChars, count)
            )
        );
        return this;
    }

    /// <summary>
    /// Set the collection prop to a specific size without needing to specify the contents
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="minCount"></param>
    /// <param name="maxCount"></param>
    /// <returns></returns>
    public FauxTypedRuleBuilder<TRoot, TType> Count(
        Expression<Func<TType, string?>> expression,
        int minCount,
        int maxCount
    )
    {
        var props = expression.GetPropertyInfos().Concat(_fromRootProperties).ToArray();
        InsertRule(
            new FauxRuleFactoryConfig(
                GetPropertySelector(typeof(string), props),
                (_, _) =>
                    FauxDefaults.Random.GetString(
                        FauxDefaults.AllowedChars,
                        FauxDefaults.Random.Next(minCount, maxCount + 1)
                    )
            )
        );
        return this;
    }

    /// <summary>
    /// Assigns a prop a specific value after it has been created where you have full access to the root object.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="getter"></param>
    /// <typeparam name="TProp"></typeparam>
    /// <returns></returns>
    public FauxTypedRuleBuilder<TRoot, TType> Assign<TProp>(
        Expression<Func<TType, TProp>> expression,
        Func<TRoot, TProp> getter
    )
    {
        var props = expression.GetPropertyInfos();
        InsertRule(
            new FauxRuleAssignConfig(
                ignoreSelector: GetPropertySelector(typeof(TProp), props),
                assignSelector: GetSetterSelector(typeof(TRoot)),
                parentSelector: obj => _fromRootGetter((TRoot)obj).Cast<object>(),
                properties: props,
                getValue: obj => getter((TRoot)obj)
            )
        );
        return this;
    }

    /// <summary>
    /// Start configuration of a specific sub-property.
    /// </summary>
    /// <param name="expression"></param>
    /// <typeparam name="TProp"></typeparam>
    /// <returns></returns>
    public FauxTypedRuleBuilder<TRoot, TProp> For<TProp>(Expression<Func<TType, TProp>> expression)
    {
        return new FauxTypedRuleBuilder<TRoot, TProp>(
            this,
            _exact,
            expression.GetPropertyInfos().Concat(_fromRootProperties).ToArray(),
            root => _fromRootGetter(root).SelectMany<TType, TProp>(item => new[] { expression.Compile()(item) })
        );
    }

    /// <summary>
    /// Start configuration of a specific sub-property as a collection type.
    /// </summary>
    /// <param name="expression"></param>
    /// <typeparam name="TProp"></typeparam>
    /// <returns></returns>
    public FauxTypedRuleBuilder<TRoot, TProp> ForAll<TProp>(Expression<Func<TType, IEnumerable<TProp>>> expression)
    {
        return new FauxTypedRuleBuilder<TRoot, TProp>(
            this,
            _exact,
            expression.GetPropertyInfos().Concat(_fromRootProperties).ToArray(),
            root => _fromRootGetter(root).SelectMany<TType, TProp>(item => expression.Compile()(item))
        );
    }

    /// <summary>
    /// Returns a comparer that will return <c>true</c> when <paramref name="propertiesToMatch"/> matches the given
    /// properties. Example:
    /// <code>
    /// propertiesToMatch: OrganisationAddress.Country.Id -> [Id, Country, OrganisationAddress]
    /// actualProperties:  Country.Id -> [Id, Country]
    /// result:            false
    /// ------------------------------------------------------------------------------------------
    /// propertiesToMatch: OrganisationAddress.Country.Id -> [Id, Country, OrganisationAddress]
    /// actualProperties:  OrganisationAddress.Country.Id -> [Id, Country, OrganisationAddress]
    /// result:            true
    /// ------------------------------------------------------------------------------------------
    /// propertiesToMatch: OrganisationAddress.Country.Id -> [Id, Country, OrganisationAddress]
    /// actualProperties:  Organisation.OrganisationAddress.Country.Id -> [Id, Country, OrganisationAddress, Organisation]
    /// result:            true
    /// ------------------------------------------------------------------------------------------
    /// propertiesToMatch: OrganisationAddress.Country.Id -> [Id, Country, OrganisationAddress]
    /// actualProperties:  CollectionPointAddress.Country.Id -> [Id, Country, CollectionPointAddress]
    /// result:            false
    /// </code>
    /// </summary>
    /// <param name="targetType"></param>
    /// <param name="propertiesToMatch"></param>
    /// <returns></returns>
    private FauxRuleConfig.SelectorDelegate GetPropertySelector(Type targetType, PropertyInfo[] propertiesToMatch)
    {
        if (propertiesToMatch.Length == 0)
            throw new ArgumentException(
                "List of properties cannot be empty. Did you provide a correct property expression?",
                nameof(propertiesToMatch)
            );

        return (type, actualProperties) =>
        {
            if (!FauxObject.IsMatchingType(targetType, type))
                return false;

            // If we require an exact match, the amount of properties need to match exactly
            if (_exact && propertiesToMatch.Length != actualProperties.Length)
                return false;

            // A non-exact match needs to have a partial overlap, but can only be if the actual properties list is
            // smaller, otherwise it's definitely not a match
            if (!_exact && propertiesToMatch.Length > actualProperties.Length)
                return false;

            // Otherwise, iterate simultaneously over both property lists
            for (var i = 0; i < propertiesToMatch.Length; i++)
            {
                var match = propertiesToMatch[i];
                var actual = actualProperties[i];
                if (!actual.Matches(match)) // And stop when any of them are different
                    return false;
            }

            // Partial match found!
            return true;
        };
    }

    private static Func<Type, PropertyInfo[], bool> GetSetterSelector(Type rootType)
    {
        return (type, _) => type == rootType;
    }
}
