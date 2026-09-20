using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Cart.Features.Commands.MergeGuestCart;

public class MergeGuestCartHandler(
    ICartRepository cartRepository,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<MergeGuestCartCommand>
{
    public async Task<ServiceResult> Handle(MergeGuestCartCommand request, CancellationToken ct)
    {
        var userId = UserId.From(currentUserService.UserId!.Value);
        if (string.IsNullOrWhiteSpace(currentUserService.GuestToken))
            return ServiceResult.Success();

        var guestToken = GuestToken.Create(currentUserService.GuestToken);

        var guestCart = await cartRepository.FindByGuestTokenAsync(guestToken, ct);
        if (guestCart is null)
            return ServiceResult.Success();

        var userCart = await cartRepository.FindByUserIdAsync(userId, ct);

        if (userCart is null)
        {
            guestCart.AssignToUser(userId, dateTimeProvider.UtcNow);
            cartRepository.Update(guestCart);
        }
        else
        {
            userCart.MergeFrom(guestCart, dateTimeProvider.UtcNow, request.Strategy);
            cartRepository.Update(userCart);
            cartRepository.Remove(guestCart);
        }

        await auditService.LogAsync(
            "Cart",
            "MergeGuestCart",
            IpAddress.Unknown,
            userId,
            entityType: "Cart",
            ct: ct);

        return ServiceResult.Success();
    }
}
