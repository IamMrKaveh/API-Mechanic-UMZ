namespace Application.Brand.Features.Commands.DeleteBrand;

public record DeleteBrandCommand(Guid BrandId) : ICommand, IAuditableCommand
{
    public string AuditEventType => "Brand";

    public string AuditAction => "DeleteBrand";

    public string? AuditEntityType => "Brand";

    public string? AuditEntityId => BrandId.ToString();
}