using Application.Brand.Features.Shared;

namespace Application.Brand.Features.Commands.UpdateBrand;

public record UpdateBrandCommand(
    Guid Id,
    string Name,
    Guid CategoryId,
    string? Slug,
    string? Description,
    string? LogoPath,
    string RowVersion) : IRequest<ServiceResult<BrandDetailDto>>;