using Domain.Cart.Interfaces;
using Domain.Cart.Services;
using SharedKernel.Contracts;

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

    public async Task<ServiceResult> Handle(
        MergeGuestCartCommand request,
        CancellationToken ct)
    {
        if (_currentUser.IsAuthenticated)
            return ServiceResult.Unauthorized("کاربر باید وارد شده باشد.");

        var guestCart = await _cartRepository.GetByGuestTokenAsync(request.GuestToken, ct);
        if (guestCart == null || guestCart.IsEmpty)
            return ServiceResult.Success();

        var userCart = await _cartRepository.GetByUserIdAsync(_currentUser.CurrentUser.UserId, ct);

        if (userCart == null)
        {
            guestCart.AssignToUser(_currentUser.CurrentUser.UserId);
        }
        else
        {
            var strategy = _cartDomainService.DetermineMergeStrategy(userCart, guestCart);
            userCart.MergeWith(guestCart, strategy);
            _cartRepository.Delete(guestCart);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "سبد مهمان {GuestToken} با سبد کاربر {UserId} ادغام شد.",
            request.GuestToken, _currentUser.CurrentUser.UserId);

        return ServiceResult.Success();
    }
}