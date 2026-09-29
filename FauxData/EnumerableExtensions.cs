namespace FauxData;

internal static class EnumerableExtensions
{
    /// <summary>
    /// Returns a random value of an <c>IEnumerable</c>. Returns <c>default(T)</c> when the list is empty
    /// </summary>
    /// <param name="enumerable"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? Random<T>(this IEnumerable<T> enumerable)
    {
        // Hoist to the list implementation if possible since it's more performant and provides better randomization
        if (enumerable is IList<T> list)
            return list.Random();

        // Otherwise, enumerate and have a constant chance to return each element, note that this prefers items with
        // lower indices
        T? lastItem = default;
        foreach (var item in enumerable)
        {
            lastItem = item;
            if (System.Random.Shared.NextDouble() < 0.3)
                return item;
        }

        return lastItem;
    }

    /// <summary>
    /// Returns a random value of a <c>IList</c> or <c>default(T)</c> when empty
    /// </summary>
    /// <param name="list"></param>
    /// <param name="random">Use this random instance. If not provided, uses the Shared instance</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? Random<T>(this IList<T> list, Random? random = null)
    {
        if (list.Count == 0)
            return default;

        var index = (random ?? System.Random.Shared).Next(list.Count);
        return list[index];
    }
}
