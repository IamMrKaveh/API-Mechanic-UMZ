using Application.Brand.Features.Shared;

namespace Application.Brand.Features.Commands.CreateBrand;

public record CreateBrandCommand(
    Guid CategoryId,
    string Name,
    string? Slug,
    string? Description,
    string? LogoPath) : IRequest<ServiceResult<BrandDetailDto>>;