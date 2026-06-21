using Application.Brand.Features.Shared;

namespace Application.Brand.Features.Commands.UpdateBrand;

public record UpdateBrandCommand(
    Guid BrandId,
    Guid CategoryId,
    string Name,
    string? Slug,
    string? Description,
    Stream? LogoStream,
    string? LogoFileName,
    string? LogoContentType,
    long? LogoFileSize,
    string? RowVersion) : ICommand<BrandDetailDto>;