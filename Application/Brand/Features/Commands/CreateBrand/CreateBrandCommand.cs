using Application.Brand.Features.Shared;

namespace Application.Brand.Features.Commands.CreateBrand;

public record CreateBrandCommand(
    Guid CategoryId,
    string Name,
    string? Slug,
    string? Description,
    Stream? LogoStream,
    string? LogoFileName,
    string? LogoContentType,
    long? LogoFileSize) : ICommand<BrandDetailDto>, IAuditableCommand
{
    public string AuditEventType => "Brand";

    public string AuditAction => "CreateBrand";

    public string? AuditEntityType => "Brand";

    public string? AuditEntityId => null;
}