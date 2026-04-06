using Application.Common.Results;
using Domain.User.ValueObjects;

namespace Application.Cart.Features.Commands.AddToCart;

public record AddToCartCommand(
    UserId? UserId,
    string? GuestToken,
    int VariantId,
    int Quantity) : IRequest<ServiceResult>;