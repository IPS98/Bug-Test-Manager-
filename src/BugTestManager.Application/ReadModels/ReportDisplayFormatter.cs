using System.Globalization;
using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.ReadModels;

public static class ReportDisplayFormatter
{
    private const string EmptyDisplayValue = "-";
    private const string DateFormat = "yyyy-MM-dd";
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm";

    public static string FormatDate(DateTimeOffset value)
    {
        return value.ToLocalTime().ToString(DateFormat, CultureInfo.InvariantCulture);
    }

    public static string FormatOptionalDate(DateTimeOffset? value)
    {
        return value is null
            ? EmptyDisplayValue
            : FormatDate(value.Value);
    }

    public static string FormatCustomFieldValue(FieldType fieldType, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return EmptyDisplayValue;
        }

        var trimmedValue = value.Trim();
        return fieldType switch
        {
            FieldType.Date => FormatDateValue(trimmedValue),
            FieldType.DateTime => FormatDateTimeValue(trimmedValue),
            FieldType.Checkbox => FormatCheckboxValue(trimmedValue),
            _ => trimmedValue
        };
    }

    private static string FormatDateValue(string value)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dateTimeOffset))
        {
            return dateTimeOffset.ToLocalTime().ToString(DateFormat, CultureInfo.InvariantCulture);
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
        {
            return dateTime.ToString(DateFormat, CultureInfo.InvariantCulture);
        }

        return value;
    }

    private static string FormatDateTimeValue(string value)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dateTimeOffset))
        {
            return dateTimeOffset.ToLocalTime().ToString(DateTimeFormat, CultureInfo.InvariantCulture);
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
        {
            return dateTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
        }

        return value;
    }

    private static string FormatCheckboxValue(string value)
    {
        return bool.TryParse(value, out var parsedValue) && parsedValue
            ? "Yes"
            : "No";
    }
}
