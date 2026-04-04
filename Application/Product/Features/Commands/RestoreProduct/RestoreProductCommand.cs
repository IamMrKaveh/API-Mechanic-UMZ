using Application.Common.Results;

namespace Application.Product.Features.Commands.RestoreProduct;

public record RestoreProductCommand(int Id, int UserId) : IRequest<ServiceResult>;