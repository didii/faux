using System.Linq.Expressions;
using System.Reflection;

namespace FauxData;

internal static class ExpressionExtensions
{
    /// <summary>
    /// Returns the list of properties for a nested property expression in reverse order
    /// </summary>
    /// <param name="expr"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TProp"></typeparam>
    /// <returns></returns>
    public static PropertyInfo[] GetPropertyInfos<T, TProp>(this Expression<Func<T, TProp>> expr)
    {
        var body = expr.Body;

        var result = new List<PropertyInfo>();
        var memberExpr = body as MemberExpression;
        while (memberExpr != null)
        {
            var propInfo = memberExpr.Member as PropertyInfo;
            if (propInfo != null)
                result.Add(propInfo);
            memberExpr = memberExpr.Expression as MemberExpression;
        }

        return result.ToArray();
    }
}
