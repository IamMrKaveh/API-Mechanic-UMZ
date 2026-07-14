namespace Application.Brand.Features.Commands.MoveBrand;

public record MoveBrandCommand(
    Guid BrandId,
    Guid TargetCategoryId) : ICommand, IAuditableCommand
{
    public string AuditEventType => "Brand";

    public string AuditAction => "MoveBrand";

    public string? AuditEntityType => "Brand";

    public string? AuditEntityId => BrandId.ToString();
}