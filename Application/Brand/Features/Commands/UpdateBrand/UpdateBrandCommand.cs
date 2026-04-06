using Application.Brand.Features.Shared;
using Application.Common.Results;

namespace Application.Brand.Features.Commands.UpdateBrand;

public record UpdateBrandCommand(
    Guid Id,
    string Name,
    Guid CategoryId,
    string? Slug,
    string? Description,
    string? LogoPath,
    bool IsActive) : IRequest<ServiceResult<BrandDetailDto>>;