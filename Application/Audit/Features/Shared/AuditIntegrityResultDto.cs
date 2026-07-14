namespace Application.Audit.Features.Shared;

public sealed record AuditIntegrityResultDto
{
    public Guid Id { get; init; }
    public bool IsValid { get; init; }
    public string ExpectedHash { get; init; } = string.Empty;
    public string StoredHash { get; init; } = string.Empty;
    public DateTime VerifiedAt { get; init; }
}