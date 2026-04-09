namespace Application.Brand.Features.Commands.DeleteBrand;

public record DeleteBrandCommand(Guid Id, Guid DeletedBy) : IRequest<ServiceResult>;