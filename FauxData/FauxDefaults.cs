using System.Collections;

namespace FauxData;

/// <summary>
/// Contains default values used when <see cref="Faux"/> is created
/// </summary>
public static class FauxDefaults
{
    internal static readonly Random Random = new();

    internal static readonly char[] AllowedChars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_".ToCharArray();

    private static int _currentInt = 0;
    private static long _currentLong = 0;

    /// <summary>
    /// Default max depth
    /// </summary>
    public const int MaxDepth = 10;

    /// <summary>
    /// The default set of rules which mostly contains type factories.
    /// </summary>
    public static readonly IReadOnlyList<FauxRuleConfig> Rules =
    new List<FauxRuleConfig>() {
        new FauxRuleMaxDepthConfig(MaxDepth),
        FauxRuleFactoryConfig.ForType<bool>(() => Random.NextSingle() >= 0.5f),
        FauxRuleFactoryConfig.ForType<int>(() => _currentInt += Random.Next(1, 6)),
        FauxRuleFactoryConfig.ForType<float>(() => Random.NextSingle() * 99 + 1),
        FauxRuleFactoryConfig.ForType<double>(() => Random.NextDouble() * 99 + 1),
        FauxRuleFactoryConfig.ForType<decimal>(() => new decimal(Random.NextDouble() * 99 + 1)),
        FauxRuleFactoryConfig.ForType<long>(() => _currentLong += Random.NextInt64(1, 100)),
        FauxRuleFactoryConfig.ForType<string>(() => Random.GetString(AllowedChars, Random.Next(5, 11))),
        FauxRuleFactoryConfig.ForType<Guid>(() => Guid.NewGuid()),
        FauxRuleFactoryConfig.ForType<TimeOnly>(() => new TimeOnly(Random.Next(0, 24), Random.Next(0, 60))),
        FauxRuleFactoryConfig.ForType<TimeSpan>(() => new TimeSpan(0, Random.Shared.Next(0, 24), Random.Shared.Next(60), Random.Shared.Next(60))),
        FauxRuleFactoryConfig.ForType<DateOnly>(() =>
        {
            // Get any date withing a 2-year range of today
            const int yearInDays = 2 * 365;
            return DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(Random.Next(-yearInDays, yearInDays)));
        }),
        FauxRuleFactoryConfig.ForType<DateTime>(() =>
        {
            // Get any date within a 2-year range of now in UTC time
            const int yearInSeconds = 60 * 60 * 24 * 365;
            return DateTime.UtcNow.AddSeconds(Random.Next(-yearInSeconds, yearInSeconds));
        }),
        FauxRuleFactoryConfig.ForType<TimeZoneInfo>(() =>
            TimeZoneInfo.GetSystemTimeZones().Where(tz => tz.HasIanaId).Random()
        ),
        new FauxRuleFactoryConfig(
            (type, _) => type.IsEnum,
            (type, _) =>
            {
                var enumValues = type.GetEnumValues();
                var index = Random.Next(0, enumValues.Length);
                return enumValues.GetValue(index);
            }
        ),
    };

    /// <summary>
    /// Rules used exclusively for <see cref="Faux.Pas{T}"/> to create a completely empty object
    /// </summary>
    public static readonly IReadOnlyList<FauxRuleConfig> PasRules =
        new List<FauxRuleConfig>()
        {
            new FauxRuleMaxDepthConfig(MaxDepth),
            FauxRuleFactoryConfig.ForType<bool>(() => default),
            FauxRuleFactoryConfig.ForType<int>(() => default),
            FauxRuleFactoryConfig.ForType<float>(() => default),
            FauxRuleFactoryConfig.ForType<double>(() => default),
            FauxRuleFactoryConfig.ForType<decimal>(() => default),
            FauxRuleFactoryConfig.ForType<long>(() => default),
            FauxRuleFactoryConfig.ForType<string>(() => default),
            FauxRuleFactoryConfig.ForType<Guid>(() => default),
            FauxRuleFactoryConfig.ForType<TimeOnly>(() => default),
            FauxRuleFactoryConfig.ForType<TimeSpan>(() => default),
            FauxRuleFactoryConfig.ForType<DateOnly>(() => default),
            FauxRuleFactoryConfig.ForType<DateTime>(() => default),
            FauxRuleFactoryConfig.ForType<TimeZoneInfo>(() => default),
            new FauxRuleFactoryConfig((type, _) => type.IsEnum, (type, _) => type.GetEnumValues().GetValue(0)),
            new FauxRuleFactoryConfig(
                (type, _) => type.IsArray,
                (type, _) => Array.CreateInstance(type.GetElementType()!, 0)
            ),
            new FauxRuleFactoryConfig(
                (type, _) => type.IsAssignableTo(typeof(IEnumerable)),
                (type, _) =>
                {
                    var itemType = type.GetGenericArguments()[0];
                    return Array.CreateInstance(itemType, 0);
                }
            ),
        };
}
