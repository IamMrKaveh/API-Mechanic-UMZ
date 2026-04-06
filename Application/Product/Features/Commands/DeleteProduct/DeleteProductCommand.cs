using Application.Common.Results;

namespace Application.Product.Features.Commands.DeleteProduct;

public record DeleteProductCommand(int Id, int DeletedByUserId) : IRequest<ServiceResult>;