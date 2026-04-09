using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Cart.Interfaces;
using Domain.Cart.Services;
using Domain.Cart.ValueObjects;
using Domain.Common.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Cart.Features.Commands.MergeGuestCart;

public class MergeGuestCartHandler(
    ICartRepository cartRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork,
    CartDomainService cartDomainService,
    ILogger<MergeGuestCartHandler> logger) : IRequestHandler<MergeGuestCartCommand, ServiceResult>
{
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly CartDomainService _cartDomainService = cartDomainService;
    private readonly ILogger<MergeGuestCartHandler> _logger = logger;

    public async Task<ServiceResult> Handle(MergeGuestCartCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return ServiceResult.Unauthorized("کاربر باید وارد شده باشد.");

        var guestToken = GuestToken.Create(request.GuestToken);
        var guestCart = await _cartRepository.FindByGuestTokenAsync(guestToken, ct);
        if (guestCart is null || guestCart.IsEmpty)
            return ServiceResult.Success();

        var userId = UserId.From(_currentUser.UserId.Value);
        var userCart = await _cartRepository.FindByUserIdAsync(userId, ct);

        if (userCart is null)
        {
            guestCart.AssignToUser(userId);
            _cartRepository.Update(guestCart);
        }
        else
        {
            _cartDomainService.MergeCarts(userCart, guestCart);
            _cartRepository.Update(userCart);
            _cartRepository.Remove(guestCart);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "سبد مهمان {GuestToken} با سبد کاربر {UserId} ادغام شد.",
            request.GuestToken, userId.Value);

        return ServiceResult.Success();
    }
}