using Application.Common.Results;

namespace Application.Cart.Features.Commands.ClearCart;

public record ClearCartCommand : IRequest<ServiceResult>;