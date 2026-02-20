namespace Application.Brand.Features.Commands.DeleteBrand;

public record DeleteBrandCommand(
    int CategoryId,
    int GroupId,
    int? DeletedBy = null) : IRequest<ServiceResult>;