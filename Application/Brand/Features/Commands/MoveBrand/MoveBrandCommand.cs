namespace Application.Brand.Features.Commands.MoveBrand;

public record MoveBrandCommand(
    int SourceCategoryId,
    int TargetCategoryId,
    int BrandId) : IRequest<ServiceResult>;