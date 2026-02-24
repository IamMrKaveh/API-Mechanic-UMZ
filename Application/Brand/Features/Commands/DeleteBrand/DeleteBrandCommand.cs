namespace Application.Brand.Features.Commands.DeleteBrand;

public record DeleteBrandCommand(
    int CategoryId,
    int BrandId,
    int? DeletedBy = null
    ) : IRequest<ServiceResult>;