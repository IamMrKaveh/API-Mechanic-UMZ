using Application.Common.Results;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Application.Brand.Features.Commands.MoveBrand;

public record MoveBrandCommand(
    BrandId BrandId,
    CategoryId TargetCategoryId) : IRequest<ServiceResult>;