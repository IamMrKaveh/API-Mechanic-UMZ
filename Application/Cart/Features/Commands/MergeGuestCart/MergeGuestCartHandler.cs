using Application.Common.Interfaces;
using Domain.Cart.Interfaces;
using Domain.Cart.Services;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Cart.Features.Commands.MergeGuestCart;

public class MergeGuestCartHandler(
    ICartRepository cartRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork,
    CartDomainService cartDomainService,
    ILogger<MergeGuestCartHandler> logger) : IRequestHandler<MergeGuestCartCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(MergeGuestCartCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
            return ServiceResult.Unauthorized("کاربر باید وارد شده باشد.");

        var guestToken = GuestToken.TryCreate(request.GuestToken);
        if (guestToken is null)
            return ServiceResult.Failure("توکن مهمان نامعتبر است.", SharedKernel.Results.ErrorType.Validation);

        var guestCart = await cartRepository.FindByGuestTokenAsync(guestToken, ct);
        if (guestCart is null || guestCart.IsEmpty)
            return ServiceResult.Success();

        var userId = UserId.From(currentUser.UserId.Value);
        var userCart = await cartRepository.FindByUserIdAsync(userId, ct);

        if (userCart is null)
        {
            guestCart.AssignToUser(userId);
            cartRepository.Update(guestCart);
        }
        else
        {
            cartDomainService.MergeCarts(userCart, guestCart);
            cartRepository.Update(userCart);
            cartRepository.Remove(guestCart);
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "سبد مهمان {GuestToken} با سبد کاربر {UserId} ادغام شد.",
            request.GuestToken, userId.Value);

        return ServiceResult.Success();
    }
}