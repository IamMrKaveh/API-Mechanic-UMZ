namespace Application.Localization.Contracts;

public interface ILocalizedErrorMessageProvider
{
    string GetMessage(string errorCode);

    string GetMessage(string errorCode, params object[] arguments);

    bool TryGetMessage(string errorCode, out string message);
}
