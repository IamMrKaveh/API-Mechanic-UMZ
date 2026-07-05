using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Cart.Features.Commands.MergeGuestCart;

public class MergeGuestCartHandler(
    ICartRepository cartRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : ICommandHandler<MergeGuestCartCommand>
{
    public async Task<ServiceResult> Handle(MergeGuestCartCommand request, CancellationToken ct)
    {
        var userId = UserId.From(currentUserService.UserId!.Value);
        var guestToken = GuestToken.Create(currentUserService.GuestToken);

        var guestCart = await cartRepository.FindByGuestTokenAsync(guestToken, ct);
        if (guestCart is null)
            return ServiceResult.Success();

        var userCart = await cartRepository.FindByUserIdAsync(userId, ct);

        if (userCart is null)
        {
            guestCart.AssignToUser(userId);
            cartRepository.Update(guestCart);
        }
        else
        {
            userCart.MergeFrom(guestCart, request.Strategy);
            cartRepository.Update(userCart);
            cartRepository.Remove(guestCart);
        }

        await unitOfWork.SaveChangesAsync(ct);

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