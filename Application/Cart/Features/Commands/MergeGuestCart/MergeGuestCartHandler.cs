namespace Application.Cart.Features.Commands.MergeGuestCart;

public class MergeGuestCartHandler : IRequestHandler<MergeGuestCartCommand, ServiceResult>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CartDomainService _cartDomainService;
    private readonly ILogger<MergeGuestCartHandler> _logger;

    public MergeGuestCartHandler(
        ICartRepository cartRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        CartDomainService cartDomainService,
        ILogger<MergeGuestCartHandler> logger
        )
    {
        _cartRepository = cartRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _cartDomainService = cartDomainService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        MergeGuestCartCommand request,
        CancellationToken ct
        )
    {
        if (!_currentUser.UserId.HasValue)
            return ServiceResult.Failure("کاربر باید وارد شده باشد.", 401);

        var guestCart = await _cartRepository.GetByGuestTokenAsync(request.GuestToken, ct);
        if (guestCart == null || guestCart.IsEmpty)
            return ServiceResult.Success(); 

        var userCart = await _cartRepository.GetByUserIdAsync(_currentUser.UserId.Value, ct);

        if (userCart == null)
        {
            
            guestCart.AssignToUser(_currentUser.UserId.Value);
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
            request.GuestToken, _currentUser.UserId.Value);

        return ServiceResult.Success();
    }
}