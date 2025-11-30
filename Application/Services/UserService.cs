namespace Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;
    private readonly IRateLimitService _rateLimitService;
    private readonly IAuditService _auditService;
    private readonly ICartService _cartService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public UserService(
        IUserRepository repository,
        ILogger<UserService> logger,
        IRateLimitService rateLimitService,
        IAuditService auditService,
        ICartService cartService,
        ICurrentUserService currentUserService,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ITokenService tokenService)
    {
        _repository = repository;
        _logger = logger;
        _rateLimitService = rateLimitService;
        _auditService = auditService;
        _cartService = cartService;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    public async Task<ServiceResult<UserProfileDto?>> GetUserByIdAsync(int id)
    {
        var user = await _repository.GetUserByIdAsync(id, true);
        if (user == null) return ServiceResult<UserProfileDto?>.Fail("User not found");
        var dto = _mapper.Map<UserProfileDto>(user);
        return ServiceResult<UserProfileDto?>.Ok(dto);
    }

    public async Task<ServiceResult<UserProfileDto?>> GetUserProfileAsync(int userId)
    {
        var user = await _repository.GetUserByIdAsync(userId);
        if (user == null) return ServiceResult<UserProfileDto?>.Fail("User not found");
        var dto = _mapper.Map<UserProfileDto>(user);
        return ServiceResult<UserProfileDto?>.Ok(dto);
    }

    public async Task<ServiceResult> UpdateUserAsync(int id, UpdateProfileDto updateRequest, int currentUserId, bool isAdmin)
    {
        if (currentUserId != id && !isAdmin) return ServiceResult.Fail("Forbidden");

        var existingUser = await _repository.GetUserByIdAsync(id, true);
        if (existingUser == null) return ServiceResult.Fail("NotFound");
        if (existingUser.IsDeleted && !isAdmin) return ServiceResult.Fail("User account is deleted and cannot be modified.");

        _mapper.Map(updateRequest, existingUser);
        existingUser.UpdatedAt = DateTime.UtcNow;

        _repository.UpdateUser(existingUser);

        try
        {
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail("User was modified by another process");
        }
    }

    public async Task<ServiceResult> UpdateProfileAsync(int userId, UpdateProfileDto updateRequest)
    {
        var existingUser = await _repository.GetUserByIdAsync(userId);
        if (existingUser == null) return ServiceResult.Fail("NotFound");

        _mapper.Map(updateRequest, existingUser);
        existingUser.UpdatedAt = DateTime.UtcNow;

        _repository.UpdateUser(existingUser);

        try
        {
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
            return ServiceResult.Fail("Error updating profile");
        }
    }

    public async Task<ServiceResult> DeleteAccountAsync(int userId)
    {
        var user = await _repository.GetUserByIdAsync(userId);
        if (user == null) return ServiceResult.Fail("NotFound");

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;
        user.PhoneNumber = $"{user.PhoneNumber}_deleted_{DateTime.UtcNow.Ticks}";

        await _repository.RevokeAllUserSessionsAsync(userId);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<(string? Message, string? Otp)>> LoginAsync(LoginRequestDto request, string clientIp)
    {
        var user = await _repository.GetUserByPhoneNumberAsync(request.PhoneNumber, true);

        if (user != null && user.IsDeleted)
            return ServiceResult<(string?, string?)>.Fail("This account has been deleted.");

        if (user == null)
        {
            var newUserRateLimitKey = $"new_user_creation_{clientIp}";
            (bool isLimited, int retryAfterSeconds) = await _rateLimitService.IsLimitedAsync(newUserRateLimitKey, 1, 5);
            if (isLimited)
            {
                _logger.LogWarning("Rate limit exceeded for new user creation from IP: {ClientIP}", clientIp);
                return ServiceResult<(string?, string?)>.Fail("Too many attempts to create new users. Please try again later.");
            }
        }
        else
        {
            var loginRateLimitKey = $"login_{request.PhoneNumber}_{clientIp}";
            (bool isLimited, int retryAfterSeconds) = await _rateLimitService.IsLimitedAsync(loginRateLimitKey, 5, 15);
            if (isLimited)
            {
                _logger.LogWarning("Rate limit exceeded for login attempt from IP: {ClientIP}, Phone: {PhoneNumber}", clientIp, request.PhoneNumber);
                return ServiceResult<(string?, string?)>.Fail("Too many login attempts. Please try again later.");
            }
        }

        if (user == null)
        {
            user = new Domain.User.User
            {
                PhoneNumber = request.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsAdmin = false,
                IsDeleted = false
            };
            await _repository.AddUserAsync(user);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("New user created with phone: {PhoneNumber}", request.PhoneNumber);
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive user: {PhoneNumber}", request.PhoneNumber);
            return ServiceResult<(string?, string?)>.Fail("Account is inactive.");
        }

        await _repository.InvalidateOtpsAsync(user.Id);

        var activeOtp = await _repository.GetActiveOtpAsync(user.Id);
        if (activeOtp != null)
            return ServiceResult<(string?, string?)>.Fail("An active OTP already exists. Please wait before requesting a new one.");

        var otp = GenerateSecureOtp();
        var userOtp = new UserOtp
        {
            UserId = user.Id,
            OtpHash = BCryptNet.HashPassword(otp),
            ExpiresAt = DateTime.UtcNow.AddMinutes(2),
            IsUsed = false,
            AttemptCount = 0
        };

        await _repository.AddOtpAsync(userOtp);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("OTP sent successfully to phone: {PhoneNumber} (OTP: {Otp})", request.PhoneNumber, otp);
        return ServiceResult<(string?, string?)>.Ok(("OTP sent successfully", otp));
    }

    public async Task<ServiceResult<(AuthResponseDto? Response, string? Error)>> VerifyOtpAsync(VerifyOtpRequestDto request, string clientIp, string userAgent)
    {
        var user = await _repository.GetUserByPhoneNumberAsync(request.PhoneNumber);
        if (user == null)
            return ServiceResult<(AuthResponseDto?, string?)>.Fail("Invalid credentials.");

        var rateLimitKey = $"otp_{clientIp}_{user.Id}";
        (bool isLimited, int retryAfterSeconds) = await _rateLimitService.IsLimitedAsync(rateLimitKey, 3, 5);
        if (isLimited)
            return ServiceResult<(AuthResponseDto?, string?)>.Fail("Too many verification attempts. Please request a new OTP.");

        var storedOtp = await _repository.GetActiveOtpAsync(user.Id);
        if (storedOtp == null)
            return ServiceResult<(AuthResponseDto?, string?)>.Fail("Invalid or expired OTP code.");

        if (storedOtp.AttemptCount >= 3)
        {
            storedOtp.IsUsed = true;
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult<(AuthResponseDto?, string?)>.Fail("Too many failed attempts. Please request a new OTP.");
        }

        if (!BCryptNet.Verify(request.Code, storedOtp.OtpHash))
        {
            storedOtp.AttemptCount++;
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult<(AuthResponseDto?, string?)>.Fail("Invalid OTP code.");
        }

        storedOtp.IsUsed = true;
        await _repository.RevokeAllUserSessionsAsync(user.Id);

        var guestId = _currentUserService.GuestId;
        if (!string.IsNullOrEmpty(guestId))
        {
            await _cartService.MergeCartAsync(user.Id, guestId);
        }

        await _unitOfWork.SaveChangesAsync();

        var token = _tokenService.GenerateJwtToken(user);
        var selector = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var verifier = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var refreshTokenValue = $"{selector}:{verifier}";
        var verifierHash = BCryptNet.HashPassword(verifier);

        var safeUserAgent = SanitizeUserAgent(userAgent);

        var session = new UserSession
        {
            UserId = user.Id,
            TokenSelector = selector,
            TokenVerifierHash = verifierHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = clientIp,
            UserAgent = safeUserAgent
        };
        await _repository.AddSessionAsync(session);

        await _unitOfWork.SaveChangesAsync();
        await _auditService.LogSecurityEventAsync("LoginSuccess", $"User {user.Id} logged in.", clientIp, user.Id, userAgent);

        var response = new AuthResponseDto(
            token,
            _mapper.Map<UserProfileDto>(user),
            DateTime.UtcNow.AddHours(1),
            refreshTokenValue
        );

        return ServiceResult<(AuthResponseDto?, string?)>.Ok((response, null));
    }

    public async Task<ServiceResult<(object? Response, string? Error)>> RefreshTokenAsync(RefreshRequestDto request, string clientIp, string userAgent)
    {
        var storedSession = await _repository.GetActiveSessionByTokenAsync(request.refreshToken);

        if (storedSession == null) return ServiceResult<(object?, string?)>.Fail("Invalid token.");
        if (storedSession.User == null) return ServiceResult<(object?, string?)>.Fail("User not found for this token.");

        var selector = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var verifier = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var newRefreshTokenValue = $"{selector}:{verifier}";
        var newVerifierHash = BCryptNet.HashPassword(verifier);

        await _repository.RevokeSessionAsync(storedSession, newVerifierHash);

        var newJwt = _tokenService.GenerateJwtToken(storedSession.User);

        var newSession = new UserSession
        {
            UserId = storedSession.UserId,
            TokenSelector = selector,
            TokenVerifierHash = newVerifierHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = clientIp,
            UserAgent = SanitizeUserAgent(userAgent),
            UpdatedAt = DateTime.UtcNow,
        };
        await _repository.AddSessionAsync(newSession);

        await _unitOfWork.SaveChangesAsync();

        var response = new { token = newJwt, refreshToken = newRefreshTokenValue };
        return ServiceResult<(object?, string?)>.Ok((response, null));
    }

    public async Task<ServiceResult> LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return ServiceResult.Fail("Refresh token is required.");

        var sessionToRevoke = await _repository.GetActiveSessionByTokenAsync(refreshToken);
        if (sessionToRevoke == null) return ServiceResult.Ok();

        await _repository.RevokeSessionAsync(sessionToRevoke);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<UserAddressDto?>> AddUserAddressAsync(int userId, CreateUserAddressDto addressDto)
    {
        var user = await _repository.GetUserByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<UserAddressDto?>.Fail("User not found.");
        }

        var address = _mapper.Map<UserAddress>(addressDto);
        address.UserId = userId;

        user.UserAddresses.Add(address);

        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<UserAddressDto>(address);
        return ServiceResult<UserAddressDto?>.Ok(resultDto);
    }
    public async Task<ServiceResult<UserAddressDto?>> UpdateUserAddressAsync(int userId, int addressId, UpdateUserAddressDto addressDto)
    {
        var address = await _repository.GetUserAddressAsync(addressId);
        if (address == null || address.UserId != userId)
        {
            return ServiceResult<UserAddressDto?>.Fail("Address not found.");
        }

        _mapper.Map(addressDto, address);
        _repository.UpdateUserAddress(address);

        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<UserAddressDto>(address);
        return ServiceResult<UserAddressDto?>.Ok(resultDto);
    }
    public async Task<ServiceResult> DeleteUserAddressAsync(int userId, int addressId)
    {
        var address = await _repository.GetUserAddressAsync(addressId);
        if (address == null || address.UserId != userId)
        {
            return ServiceResult.Fail("Address not found.");
        }

        _repository.DeleteUserAddress(address);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    private string SanitizeUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "unknown";
        var sanitized = new string(userAgent.Where(c => !char.IsControl(c)).ToArray());
        return sanitized.Length > 500 ? sanitized[..500] : sanitized;
    }

    private string GenerateSecureOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
    }
}