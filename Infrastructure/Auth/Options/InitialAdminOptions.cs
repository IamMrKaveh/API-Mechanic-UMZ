using Application.Auth.Contracts;

namespace Infrastructure.Auth.Options;

public sealed class InitialAdminOptions : IInitialAdminOptions
{
    public IReadOnlyList<string> PhoneNumbers { get; init; } = [];
}