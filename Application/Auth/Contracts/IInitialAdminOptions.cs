namespace Application.Auth.Contracts;

public interface IInitialAdminOptions
{
    public const string SectionName = "InitialAdmin";

    public IReadOnlyList<string> PhoneNumbers { get; init; }
}