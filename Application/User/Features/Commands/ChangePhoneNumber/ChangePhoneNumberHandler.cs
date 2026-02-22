namespace Application.User.Features.Commands.ChangePhoneNumber;

public class ChangePhoneNumberHandler : IRequestHandler<ChangePhoneNumberCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpService _otpService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<ChangePhoneNumberHandler> _logger;

    public ChangePhoneNumberHandler(
        IUserRepository userRepository,
        IOtpService otpService,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<ChangePhoneNumberHandler> logger)
    {
        _userRepository = userRepository;
        _otpService = otpService;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        ChangePhoneNumberCommand request, CancellationToken cancellationToken)
    {
        // 1. اعتبارسنجی شماره تلفن جدید
        var (phoneSuccess, phoneNumber, phoneError) =
            PhoneNumber.TryCreate(request.NewPhoneNumber);
        if (!phoneSuccess)
            return ServiceResult.Failure(phoneError!);

        var normalizedPhone = phoneNumber!.Value;

        // 2. بررسی یکتایی (بین Aggregate‌ها - نیاز به Repository)
        if (await _userRepository.PhoneNumberExistsAsync(normalizedPhone, request.UserId, cancellationToken))
            return ServiceResult.Failure("این شماره تلفن قبلاً ثبت شده است.");

        // 3. دریافت کاربر با OTPها
        var user = await _userRepository.GetWithOtpsAsync(request.UserId, cancellationToken);
        if (user == null)
            return ServiceResult.Failure("کاربر یافت نشد.", 404);

        // 4. تأیید OTP
        var otpHash = _otpService.HashOtp(request.OtpCode);
        if (!user.VerifyOtp(request.OtpCode))
        {
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult.Failure("کد تأیید نادرست است.");
        }

        try
        {
            // 5. تغییر شماره تلفن (Domain Logic)
            user.ChangePhoneNumber(normalizedPhone);

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogSecurityEventAsync(
                "PhoneNumberChanged",
                $"شماره تلفن کاربر {request.UserId} تغییر کرد.",
                "system",
                request.UserId);

            return ServiceResult.Success();
        }
        catch (Domain.Common.Exceptions.DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}