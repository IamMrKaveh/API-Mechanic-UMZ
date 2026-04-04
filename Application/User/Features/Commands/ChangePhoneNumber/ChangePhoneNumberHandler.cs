using Application.Audit.Contracts;
using Application.Auth.Contracts;
using Application.Common.Results;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.ChangePhoneNumber;

public class ChangePhoneNumberHandler(
    IUserRepository userRepository,
    IOtpService otpService,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ILogger<ChangePhoneNumberHandler> logger) : IRequestHandler<ChangePhoneNumberCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IOtpService _otpService = otpService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ILogger<ChangePhoneNumberHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        ChangePhoneNumberCommand request,
        CancellationToken ct)
    {
        var (phoneSuccess, phoneNumber, phoneError) =
            PhoneNumber.TryCreate(request.NewPhoneNumber);
        if (!phoneSuccess)
            return ServiceResult.Unexpected(phoneError!);

        var normalizedPhone = phoneNumber!.Value;

        if (await _userRepository.PhoneNumberExistsAsync(normalizedPhone, request.UserId, ct))
            return ServiceResult.Conflict("این شماره تلفن قبلاً ثبت شده است.");

        var user = await _userRepository.GetWithOtpsAsync(request.UserId, ct);
        if (user == null)
            return ServiceResult.NotFound("کاربر یافت نشد.");

        var otpHash = _otpService.HashOtp(request.OtpCode);
        if (!user.VerifyOtp(request.OtpCode))
        {
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Unexpected("کد تأیید نادرست است.");
        }

        try
        {
            user.ChangePhoneNumber(normalizedPhone);

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);

            await _auditService.LogSecurityEventAsync(
                "PhoneNumberChanged",
                $"شماره تلفن کاربر {request.UserId} تغییر کرد.",
                "system",
                request.UserId);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Unexpected(ex.Message);
        }
    }
}