using System.Globalization;
using Application.Localization.Contracts;
using Infrastructure.Localization.Resources;

namespace Infrastructure.Localization.Services;

public sealed class LocalizedErrorMessageProvider : ILocalizedErrorMessageProvider
{
    public string GetMessage(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
            return string.Empty;

        var dictionary = ErrorMessages.ForCulture(CultureInfo.CurrentUICulture.Name);
        return dictionary.TryGetValue(errorCode, out var value) ? value : errorCode;
    }

    public string GetMessage(string errorCode, params object[] arguments)
    {
        var template = GetMessage(errorCode);
        if (arguments is null || arguments.Length == 0)
            return template;

        try
        {
            return string.Format(CultureInfo.CurrentUICulture, template, arguments);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    public bool TryGetMessage(string errorCode, out string message)
    {
        message = string.Empty;
        if (string.IsNullOrWhiteSpace(errorCode))
            return false;

        var dictionary = ErrorMessages.ForCulture(CultureInfo.CurrentUICulture.Name);
        if (dictionary.TryGetValue(errorCode, out var value))
        {
            message = value;
            return true;
        }
        return false;
    }
}
