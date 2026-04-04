using Application.Common.Results;

namespace Application.Cart.Features.Commands.MergeGuestCart;

public record MergeGuestCartCommand(
    string GuestToken
    ) : IRequest<ServiceResult>;