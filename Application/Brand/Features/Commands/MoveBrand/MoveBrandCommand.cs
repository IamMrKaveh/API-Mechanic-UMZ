namespace Application.Brand.Features.Commands.MoveBrand;

public record MoveBrandCommand(
    Guid BrandId,
    Guid TargetCategoryId) : IRequest<ServiceResult>;