namespace Ambio.Application.Filters;

/// <summary>
/// Converts enum values sent as query strings, once they are checked by FluentValidation (IsEnumName).
/// </summary>
public static class EnumFilter
{
    /// <summary>Returns null for a missing or empty value, which means "no filter".</summary>
    public static TEnum? Parse<TEnum>(string? value) where TEnum : struct, Enum =>
        string.IsNullOrWhiteSpace(value) ? null : Enum.Parse<TEnum>(value, ignoreCase: true);
}
