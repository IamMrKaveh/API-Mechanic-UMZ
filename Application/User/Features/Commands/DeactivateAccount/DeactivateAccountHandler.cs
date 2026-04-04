using Application.Audit.Contracts;
using Application.Auth.Contracts;
using Application.Common.Results;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.User.Interfaces;

namespace Application.User.Features.Commands.DeactivateAccount;

public class DeactivateAccountHandler(
    IUserRepository userRepository,
    ISessionService sessionService,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ILogger<DeactivateAccountHandler> logger) : IRequestHandler<DeactivateAccountCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ISessionService _sessionService = sessionService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ILogger<DeactivateAccountHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        DeactivateAccountCommand request,
        CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return ServiceResult.NotFound("کاربر یافت نشد.");

        try
        {
            user.Deactivate();

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);

            await _sessionService.RevokeAllUserSessionsAsync(request.UserId, ct);

            await _auditService.LogSecurityEventAsync(
                "AccountDeactivated",
                $"حساب کاربر {request.UserId} غیرفعال شد.",
                "system",
                request.UserId.Value);

            _logger.LogInformation("حساب کاربر {UserId} غیرفعال شد.", request.UserId);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Unexpected(ex.Message);
        }
    }
}