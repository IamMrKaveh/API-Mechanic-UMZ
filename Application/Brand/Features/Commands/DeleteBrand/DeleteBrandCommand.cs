namespace Application.Brand.Features.Commands.DeleteBrand;

public record DeleteBrandCommand(Guid BrandId, Guid DeletedBy) : IRequest<ServiceResult>;