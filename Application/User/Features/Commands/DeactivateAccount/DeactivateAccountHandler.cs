namespace Application.User.Features.Commands.DeactivateAccount;

public class DeactivateAccountHandler : IRequestHandler<DeactivateAccountCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository;
    private readonly ISessionService _sessionManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<DeactivateAccountHandler> _logger;

    public DeactivateAccountHandler(
        IUserRepository userRepository,
        ISessionService sessionManager,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<DeactivateAccountHandler> logger)
    {
        _userRepository = userRepository;
        _sessionManager = sessionManager;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        DeactivateAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return ServiceResult.Failure("کاربر یافت نشد.", 404);

        try
        {
            
            user.Deactivate();

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            
            await _sessionManager.RevokeAllUserSessionsAsync(request.UserId, cancellationToken);

            await _auditService.LogSecurityEventAsync(
                "AccountDeactivated",
                $"حساب کاربر {request.UserId} غیرفعال شد.",
                "system",
                request.UserId);

            _logger.LogInformation("حساب کاربر {UserId} غیرفعال شد.", request.UserId);
            return ServiceResult.Success();
        }
        catch (Domain.Common.Exceptions.DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}