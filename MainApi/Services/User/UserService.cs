namespace MainApi.Services.User;

public class UserService : IUserService
{
    private readonly MechanicContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserService> _logger;
    private readonly IRateLimitService _rateLimitService;
    private readonly IAuditService _auditService;

    public UserService(
        MechanicContext context,
        IConfiguration configuration,
        ILogger<UserService> logger,
        IRateLimitService rateLimitService,
        IAuditService auditService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _rateLimitService = rateLimitService;
        _auditService = auditService;
    }

    public async Task<IEnumerable<UserProfileDto>> GetUsersAsync(bool includeDeleted)
    {
        var query = _context.TUsers.AsQueryable();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                PhoneNumber = u.PhoneNumber,
                CreatedAt = u.CreatedAt,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsAdmin = u.IsAdmin
            })
            .ToListAsync();
    }

    public async Task<UserProfileDto?> GetUserByIdAsync(int id)
    {
        return await _context.TUsers
            .Where(u => u.Id == id)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                PhoneNumber = u.PhoneNumber,
                CreatedAt = u.CreatedAt,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsAdmin = u.IsAdmin
            })
            .FirstOrDefaultAsync();
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(int userId)
    {
        var user = await _context.TUsers
            .Where(u => u.Id == userId)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                PhoneNumber = u.PhoneNumber,
                CreatedAt = u.CreatedAt,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsAdmin = u.IsAdmin
            })
            .FirstOrDefaultAsync();

        return user;
    }

    public async Task<(bool Success, UserProfileDto? User, string? Error)> CreateUserAsync(TUsers tUsers)
    {
        if (string.IsNullOrWhiteSpace(tUsers.PhoneNumber))
            return (false, null, "Phone number is required.");

        if (await _context.TUsers.AnyAsync(u => u.PhoneNumber == tUsers.PhoneNumber))
            return (false, null, "User with this phone number already exists.");

        tUsers.CreatedAt = DateTime.UtcNow;
        tUsers.IsActive = true;

        _context.TUsers.Add(tUsers);
        await _context.SaveChangesAsync();

        var dto = new UserProfileDto
        {
            Id = tUsers.Id,
            PhoneNumber = tUsers.PhoneNumber,
            CreatedAt = tUsers.CreatedAt
        };

        return (true, dto, null);
    }

    public async Task<(bool Success, string? Error)> UpdateUserAsync(int id, UpdateProfileDto updateRequest, int currentUserId, bool isAdmin)
    {
        if (currentUserId != id && !isAdmin)
            return (false, "Forbidden");

        var existingUser = await _context.TUsers.FindAsync(id);
        if (existingUser == null)
            return (false, "NotFound");

        if (existingUser.IsDeleted && !isAdmin)
            return (false, "User account is deleted and cannot be modified.");

        existingUser.FirstName = updateRequest.FirstName;
        existingUser.LastName = updateRequest.LastName;
        existingUser.UpdatedAt = DateTime.UtcNow;

        _context.Entry(existingUser).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.TUsers.AnyAsync(u => u.Id == id))
                return (false, "NotFound");
            return (false, "User was modified by another process");
        }
    }

    public async Task<(bool Success, string? Error)> UpdateProfileAsync(int userId, UpdateProfileDto updateRequest)
    {
        var existingUser = await _context.TUsers.FindAsync(userId);
        if (existingUser == null)
            return (false, "NotFound");

        if (!string.IsNullOrWhiteSpace(updateRequest.FirstName))
            existingUser.FirstName = updateRequest.FirstName.Trim();

        if (!string.IsNullOrWhiteSpace(updateRequest.LastName))
            existingUser.LastName = updateRequest.LastName.Trim();

        existingUser.UpdatedAt = DateTime.UtcNow;

        _context.Entry(existingUser).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
            return (false, "Error updating profile");
        }
    }

    public async Task<(bool Success, string? Error)> ChangeUserStatusAsync(int id, bool isActive)
    {
        var user = await _context.TUsers.FindAsync(id);
        if (user == null)
            return (false, "NotFound");

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteUserAsync(int id, int currentUserId)
    {
        if (id == currentUserId)
            return (false, "Admins cannot delete their own account this way.");

        var user = await _context.TUsers.FindAsync(id);
        if (user == null)
            return (false, "NotFound");

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAccountAsync(int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        var user = await _context.TUsers.FindAsync(userId);
        if (user == null)
        {
            return (false, "NotFound");
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;

        await RevokeAllUserRefreshTokensAsync(userId);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Message, string? Otp)> LoginAsync(LoginRequestDto request, string clientIp)
    {
        var user = await _context.TUsers.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

        if (user != null && user.IsDeleted)
        {
            return (false, "This account has been deleted.", null);
        }

        if (user == null)
        {
            var newUserRateLimitKey = $"new_user_creation_{clientIp}";
            if (await _rateLimitService.IsLimitedAsync(newUserRateLimitKey, 1, 5))
            {
                _logger.LogWarning("Rate limit exceeded for new user creation from IP: {ClientIP}", clientIp);
                return (false, "Too many attempts to create new users. Please try again later.", null);
            }
        }
        else
        {
            var loginRateLimitKey = $"login_{request.PhoneNumber}_{clientIp}";
            if (await _rateLimitService.IsLimitedAsync(loginRateLimitKey, 5, 15))
            {
                _logger.LogWarning("Rate limit exceeded for login attempt from IP: {ClientIP}, Phone: {PhoneNumber}", clientIp, request.PhoneNumber);
                return (false, "Too many login attempts. Please try again later.", null);
            }
        }

        if (user == null)
        {
            user = new TUsers
            {
                PhoneNumber = request.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsAdmin = false,
                IsDeleted = false
            };
            _context.TUsers.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("New user created with phone: {PhoneNumber}", request.PhoneNumber);
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive user: {PhoneNumber}", request.PhoneNumber);
            return (false, "Account is inactive.", null);
        }

        await RemoveExpiredOtps(user.Id);

        var activeOtpExists = await _context.TUserOtp.AnyAsync(o => o.UserId == user.Id && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow);
        if (activeOtpExists)
        {
            return (false, "An active OTP already exists. Please wait before requesting a new one.", null);
        }

        var otp = GenerateSecureOtp();
        var userOtp = new TUserOtp
        {
            UserId = user.Id,
            OtpHash = BCrypt.Net.BCrypt.HashPassword(otp),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            AttemptCount = 0
        };

        _context.TUserOtp.Add(userOtp);
        await _context.SaveChangesAsync();

        _logger.LogInformation("OTP sent successfully to phone: {PhoneNumber} (OTP: {Otp})", request.PhoneNumber, otp);
        return (true, "OTP sent successfully", otp);
    }

    public async Task<(AuthResponseDto? Response, string? Error)> VerifyOtpAsync(VerifyOtpRequestDto request, string clientIp, string userAgent)
    {
        var user = await _context.TUsers.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
        if (user == null)
            return (null, "Invalid credentials.");

        var rateLimitKey = $"otp_{clientIp}_{user.Id}";
        if (await _rateLimitService.IsLimitedAsync(rateLimitKey, 3, 5))
            return (null, "Too many verification attempts. Please request a new OTP.");

        var storedOtp = await _context.TUserOtp
            .Where(o => o.UserId == user.Id && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (storedOtp == null)
            return (null, "Invalid or expired OTP code.");

        if (storedOtp.AttemptCount >= 3)
        {
            storedOtp.IsUsed = true;
            await _context.SaveChangesAsync();
            return (null, "Too many failed attempts. Please request a new OTP.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Code, storedOtp.OtpHash))
        {
            storedOtp.AttemptCount++;
            await _context.SaveChangesAsync();
            return (null, "Invalid OTP code.");
        }

        storedOtp.IsUsed = true;
        await RevokeAllUserRefreshTokensAsync(user.Id);

        var token = GenerateJwtToken(user);
        var refreshTokenValue = GenerateSecureToken();
        var safeUserAgent = SanitizeUserAgent(userAgent);
        var refreshToken = new TRefreshToken
        {
            UserId = user.Id,
            TokenHash = BCrypt.Net.BCrypt.HashPassword(refreshTokenValue),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = clientIp,
            UserAgent = safeUserAgent
        };
        _context.TRefreshToken.Add(refreshToken);

        var session = new TUserSession
        {
            UserId = user.Id,
            SessionToken = refreshToken.TokenHash,
            IpAddress = clientIp,
            UserAgent = safeUserAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshToken.ExpiresAt,
            LastActivityAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.TUserSession.Add(session);

        await _context.SaveChangesAsync();

        await _auditService.LogSecurityEventAsync("LoginSuccess", $"User {user.Id} logged in.", clientIp, user.Id, userAgent);

        return (new AuthResponseDto
        {
            Token = token,
            User = new UserProfileDto
            {
                Id = user.Id,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsAdmin = user.IsAdmin
            },
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            RefreshToken = refreshTokenValue
        }, null);
    }

    public async Task<(object? Response, string? Error)> RefreshTokenAsync(RefreshRequestDto request, string clientIp, string userAgent)
    {
        TRefreshToken? storedToken = null;
        var allTokens = await _context.TRefreshToken.Include(t => t.User).Where(t => t.RevokedAt == null).ToListAsync();

        foreach (var token in allTokens)
        {
            if (BCrypt.Net.BCrypt.Verify(request.RefreshToken, token.TokenHash))
            {
                storedToken = token;
                break;
            }
        }

        if (storedToken == null)
            return (null, "Invalid token.");

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
            return (null, "Token expired.");

        if (storedToken.User == null)
            return (null, "User not found for this token.");

        var newRefreshValue = GenerateSecureToken();
        var newRefreshHash = BCrypt.Net.BCrypt.HashPassword(newRefreshValue);
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = newRefreshHash;
        var newJwt = GenerateJwtToken(storedToken.User);

        var newRefresh = new TRefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = newRefreshHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = clientIp,
            UserAgent = SanitizeUserAgent(userAgent),
            UpdatedAt = DateTime.UtcNow
        };
        _context.TRefreshToken.Add(newRefresh);

        var session = await _context.TUserSession.FirstOrDefaultAsync(s => s.SessionToken == storedToken.TokenHash);
        if (session != null)
        {
            session.IsActive = false;
        }

        var newSession = new TUserSession
        {
            UserId = storedToken.UserId,
            SessionToken = newRefreshHash,
            IpAddress = clientIp,
            UserAgent = SanitizeUserAgent(userAgent),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = newRefresh.ExpiresAt,
            LastActivityAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.TUserSession.Add(newSession);

        await _context.SaveChangesAsync();

        return (new { token = newJwt, refreshToken = newRefreshValue }, null);
    }

    public async Task<(bool Success, string? Error)> LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return (false, "Refresh token is required.");

        var tokens = await _context.TRefreshToken
            .Where(t => t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        var tokenToRevoke = tokens.FirstOrDefault(t => BCrypt.Net.BCrypt.Verify(refreshToken, t.TokenHash));

        if (tokenToRevoke == null)
            return (false, "Active token not found.");

        var session = await _context.TUserSession.FirstOrDefaultAsync(s => s.SessionToken == tokenToRevoke.TokenHash);
        if (session != null)
        {
            session.IsActive = false;
        }

        await RevokeTokenChainAsync(tokenToRevoke);
        return (true, "Logged out successfully.");
    }

    private string SanitizeUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "unknown";
        var sanitized = new string(userAgent.Where(c => !char.IsControl(c)).ToArray());
        return sanitized.Length > 255 ? sanitized[..255] : sanitized;
    }

    private async Task RevokeTokenChainAsync(TRefreshToken? token)
    {
        if (token == null) return;

        if (token.RevokedAt == null)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        if (!string.IsNullOrEmpty(token.ReplacedByTokenHash))
        {
            var nextToken = await _context.TRefreshToken.FirstOrDefaultAsync(t => t.TokenHash == token.ReplacedByTokenHash);
            await RevokeTokenChainAsync(nextToken);
        }

        await _context.SaveChangesAsync();
    }


    private async Task RemoveExpiredOtps(int userId)
    {
        var query = _context.TUserOtp
            .Where(o => o.UserId == userId && (o.ExpiresAt <= DateTime.UtcNow || o.IsUsed));
        await query.ExecuteDeleteAsync();
    }

    private async Task RevokeAllUserRefreshTokensAsync(int userId)
    {
        var query = _context.TRefreshToken
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow);
        await query.ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.RevokedAt, DateTime.UtcNow));

        var sessionQuery = _context.TUserSession.Where(s => s.UserId == userId && s.IsActive);
        await sessionQuery.ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));
    }

    private string GenerateSecureOtp()
    {
        return RandomNumberGenerator.GetInt32(1000, 9999).ToString();
    }

    private string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private string GenerateJwtToken(TUsers user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.PhoneNumber),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };
        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Audience = _configuration["Jwt:Audience"],
            Issuer = _configuration["Jwt:Issuer"]
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<IEnumerable<ProductReviewDto>> GetUserReviewsAsync(int userId)
    {
        return await _context.TProductReview
            .Where(r => r.UserId == userId)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ProductReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                UserId = r.UserId,
                UserName = r.User != null ? (r.User.FirstName + " " + r.User.LastName) : "کاربر",
                Rating = r.Rating,
                Title = r.Title,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
            })
            .ToListAsync();
    }
}