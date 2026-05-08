namespace BugTestManager.Domain.Common;

internal static class Guard
{
    public static string Required(string? value, string parameterName, string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{displayName} is required.", parameterName);
        }

        return value.Trim();
    }

    public static Guid Required(Guid value, string parameterName, string displayName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException($"{displayName} must not be empty.", parameterName);
        }

        return value;
    }

    public static int NotNegative(int value, string parameterName, string displayName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"{displayName} must not be negative.");
        }

        return value;
    }
}
