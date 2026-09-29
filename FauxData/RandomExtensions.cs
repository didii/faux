namespace FauxData;

internal static class RandomExtensions
{
    public static string GetString(this Random random, char[] allowedChars, int length)
    {
        var result = new char[length];
        for (var i = 0; i < length; i++)
            result[i] = allowedChars.Random();
        return new string(result);
    }
}
