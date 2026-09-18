using Microsoft.Extensions.Options;

namespace ECommerce.BLL.Options;

public sealed class CommerceOptions
{
    public const string SectionName = "Commerce";
    public string Currency { get; set; } = string.Empty;
}

public sealed class CommerceOptionsValidator : IValidateOptions<CommerceOptions>
{
    public ValidateOptionsResult Validate(string? name, CommerceOptions options)
    {
        try
        {
            _ = CurrencyCode.Normalize(options.Currency);
            return ValidateOptionsResult.Success;
        }
        catch (ArgumentException exception)
        {
            return ValidateOptionsResult.Fail(exception.Message);
        }
    }
}

public static class CurrencyCode
{
    public static string Normalize(string? value)
    {
        var currency = value?.Trim().ToUpperInvariant() ?? string.Empty;

        if (currency.Length != 3 || currency.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException(
                "Commerce currency must be exactly three uppercase ASCII letters.");
        }

        return currency;
    }
}
