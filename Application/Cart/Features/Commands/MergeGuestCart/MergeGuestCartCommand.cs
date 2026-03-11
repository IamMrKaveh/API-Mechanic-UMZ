using Application.Common.Models;

namespace Application.Cart.Features.Commands.MergeGuestCart;

public record MergeGuestCartCommand(
    string GuestToken
    ) : IRequest<ServiceResult>;