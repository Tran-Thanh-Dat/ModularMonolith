using System.Net.Mail;
using System.Text.Json;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using Settings.Domain.Constants;

namespace Settings.Infrastructure.Services;

internal static class SettingValueConverter
{
    public static void ValidateValue(string dataType, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        switch (dataType)
        {
            case SettingDataTypes.Number:
                if (!int.TryParse(value, out _))
                {
                    throw new BadRequestException(SettingErrors.InvalidValue, "Value must be a valid number.");
                }

                break;
            case SettingDataTypes.Decimal:
                if (!decimal.TryParse(value, out _))
                {
                    throw new BadRequestException(SettingErrors.InvalidValue, "Value must be a valid decimal.");
                }

                break;
            case SettingDataTypes.Boolean:
                if (!bool.TryParse(value, out _))
                {
                    throw new BadRequestException(SettingErrors.InvalidValue, "Value must be true or false.");
                }

                break;
            case SettingDataTypes.Json:
                try
                {
                    JsonDocument.Parse(value);
                }
                catch (JsonException)
                {
                    throw new BadRequestException(SettingErrors.InvalidValue, "Value must be valid JSON.");
                }

                break;
            case SettingDataTypes.TimeSpan:
                if (!TimeSpan.TryParse(value, out _))
                {
                    throw new BadRequestException(SettingErrors.InvalidValue, "Value must be a valid time span.");
                }

                break;
            case SettingDataTypes.Email:
                try
                {
                    _ = new MailAddress(value);
                }
                catch
                {
                    throw new BadRequestException(SettingErrors.InvalidValue, "Value must be a valid email.");
                }

                break;
            case SettingDataTypes.Url:
                if (!Uri.TryCreate(value, UriKind.Absolute, out _))
                {
                    throw new BadRequestException(SettingErrors.InvalidValue, "Value must be a valid URL.");
                }

                break;
        }
    }

    public static int ParseInt(string? value, int defaultValue) =>
        int.TryParse(value, out var parsed) ? parsed : defaultValue;

    public static bool ParseBool(string? value, bool defaultValue) =>
        bool.TryParse(value, out var parsed) ? parsed : defaultValue;

    public static decimal ParseDecimal(string? value, decimal defaultValue) =>
        decimal.TryParse(value, out var parsed) ? parsed : defaultValue;

    public static DateTimeOffset? ParseDateTimeOffset(string? value) =>
        DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;

    public static string MaskSensitive(string? value) =>
        string.IsNullOrEmpty(value) ? value : "***";
}
