using Application.Brand.Features.Shared;
using Application.Common.Results;

namespace Application.Brand.Features.Commands.CreateBrand;

public record CreateBrandCommand(
    string Name,
    Guid CategoryId,
    string? Slug,
    string? Description,
    string? LogoPath) : IRequest<ServiceResult<BrandDetailDto>>;