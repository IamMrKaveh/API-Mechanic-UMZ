namespace MainApi.Controllers.User;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : BaseApiController
{
    private readonly MechanicContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UsersController> _logger;
    private readonly IRateLimitService _rateLimitService;

    public UsersController(
        MechanicContext context,
        IConfiguration configuration,
        ILogger<UsersController> logger,
        IRateLimitService rateLimitService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _rateLimitService = rateLimitService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserProfileDto>>> GetUsers(
        [FromQuery] bool includeDeleted = false)
    {
        try
        {
            var query = _context.TUsers.AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(u => !u.IsDeleted);
            }

            var users = await query
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

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, "An error occurred while retrieving users");
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetUser(
        int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized();
        }

        if (currentUserId != id && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        try
        {
            var user = await _context.TUsers
                .Where(u => u.Id == id && !u.IsDeleted)
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

            if (user == null)
                return NotFound();

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, "An error occurred while retrieving user");
        }
    }

    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var user = await _context.TUsers
                .Where(u => u.Id == userId && !u.IsDeleted)
                .Select(u => new UserProfileDto
                {
                    Id = u.Id,
                    PhoneNumber = u.PhoneNumber,
                    CreatedAt = u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound();

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(500, "An error occurred while retrieving profile");
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserProfileDto>> CreateUser(
        [FromBody] TUsers tUsers)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(tUsers.PhoneNumber))
            return BadRequest("Phone number is required.");

        try
        {
            if (await _context.TUsers.AnyAsync(u => u.PhoneNumber == tUsers.PhoneNumber))
                return Conflict("User with this phone number already exists.");

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

            return CreatedAtAction(nameof(GetUser), new { id = dto.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, "An error occurred while creating user");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(
        int id,
        [FromBody] UpdateProfileDto updateRequest)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            if (currentUserId != id && !isAdmin)
                return Forbid();

            var existingUser = await _context.TUsers.FindAsync(id);
            if (existingUser == null)
                return NotFound();

            if (existingUser.IsDeleted && !isAdmin)
            {
                return Forbid("User account is deleted and cannot be modified.");
            }

            existingUser.FirstName = updateRequest.FirstName;
            existingUser.LastName = updateRequest.LastName;

            _context.Entry(existingUser).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating user {UserId}", id);
            if (!await _context.TUsers.AnyAsync(u => u.Id == id))
                return NotFound();
            return Conflict("User was modified by another process");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, "An error occurred while updating user");
        }
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangeUserStatus(
        int id,
        [FromBody] bool isActive)
    {
        try
        {
            var user = await _context.TUsers.FindAsync(id);
            if (user == null)
                return NotFound();

            user.IsActive = isActive;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing user status {UserId}", id);
            return StatusCode(500, "An error occurred while changing user status");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(
        int id)
    {
        try
        {
            var user = await _context.TUsers.FindAsync(id);
            if (user == null || user.IsDeleted)
                return NotFound();

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.IsActive = false;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, "An error occurred while deleting user");
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var user = await _context.TUsers.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && !u.IsDeleted);

        if (user == null)
        {
            var newUserRateLimitKey = $"new_user_creation_{clientIp}";
            if (await _rateLimitService.IsLimitedAsync(newUserRateLimitKey, 1, 5))
            {
                _logger.LogWarning("Rate limit exceeded for new user creation from IP: {ClientIP}", clientIp);
                return StatusCode(429, "Too many attempts to create new users. Please try again later.");
            }
        }
        else
        {
            var loginRateLimitKey = $"login_{request.PhoneNumber}_{clientIp}";
            if (await _rateLimitService.IsLimitedAsync(loginRateLimitKey, 5, 15))
            {
                _logger.LogWarning("Rate limit exceeded for login attempt from IP: {ClientIP}, Phone: {PhoneNumber}", clientIp, request.PhoneNumber);
                return StatusCode(429, "Too many login attempts. Please try again later.");
            }
        }

        try
        {
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
                return Unauthorized("Account is inactive.");
            }

            await RemoveExpiredOtps(user.Id);

            var activeOtpExists = await _context.TUserOtp.AnyAsync(o => o.UserId == user.Id && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow);

            if (activeOtpExists)
                return BadRequest("An active OTP already exists. Please wait before requesting a new one.");

            var otp = GenerateSecureOtp();

            var userOtp = new TUserOtp
            {
                UserId = user.Id,
                OtpHash = BCrypt.Net.BCrypt.HashPassword(otp),
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                CreatedAt = DateTime.UtcNow,
                IsUsed = false,
                AttemptCount = 0
            };

            _context.TUserOtp.Add(userOtp);
            await _context.SaveChangesAsync();

            //var apiKey = _configuration["Kavenegar:ApiKey"];
            //var template = "verify";

            //if (string.IsNullOrEmpty(apiKey))
            //    throw new InvalidOperationException("SMS service is not configured.");

            //var api = new KavenegarApi(apiKey);
            //await Task.Run(() => api.VerifyLookup(request.PhoneNumber, otp, template));

            _logger.LogInformation("OTP sent successfully to phone: {PhoneNumber} (OTP: {Otp})", request.PhoneNumber, otp);
            return Ok(new { Message = "OTP sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login process for phone: {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, "Failed to send SMS verification code.");
        }
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> VerifyOtp(
        [FromBody] VerifyOtpRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var user = await _context.TUsers.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.IsActive);
        if (user == null)
            return BadRequest("Invalid credentials.");

        var rateLimitKey = $"otp_{clientIp}_{user.Id}";
        if (await _rateLimitService.IsLimitedAsync(rateLimitKey, 3, 5))
            return StatusCode(429, "Too many verification attempts. Please request a new OTP.");

        var storedOtp = await _context.TUserOtp
            .Where(o => o.UserId == user.Id && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (storedOtp == null)
            return BadRequest("Invalid or expired OTP code.");

        if (storedOtp.AttemptCount >= 3)
        {
            storedOtp.IsUsed = true;
            await _context.SaveChangesAsync();
            return BadRequest("Too many failed attempts. Please request a new OTP.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Code, storedOtp.OtpHash))
        {
            storedOtp.AttemptCount++;
            await _context.SaveChangesAsync();
            return BadRequest("Invalid OTP code.");
        }

        storedOtp.IsUsed = true;
        await _context.SaveChangesAsync();

        await RevokeUserRefreshTokens(user.Id);

        var token = GenerateJwtToken(user);
        var refreshTokenValue = GenerateSecureToken();

        var userAgent = Request.Headers.UserAgent.ToString();
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
        await _context.SaveChangesAsync();

        return Ok(new AuthResponseDto
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
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshRequestDto request)
    {
        var storedToken = _context.TRefreshToken
            .Include(x => x.User)
            .AsEnumerable()
            .FirstOrDefault(x => BCrypt.Net.BCrypt.Verify(request.RefreshToken, x.TokenHash));

        if (storedToken == null)
        {
            return Unauthorized(new { message = "Invalid token." });
        }

        if (storedToken.RevokedAt != null)
        {
            _logger.LogWarning("Attempted reuse of a revoked refresh token for user {UserId}. Revoking token family.", storedToken.UserId);
            await RevokeTokenChainAsync(storedToken.ReplacedByTokenHash);
            return Unauthorized(new { message = "Invalid token." });
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Token expired." });
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        var safeUserAgent = SanitizeUserAgent(userAgent);

        if (storedToken.User == null || !storedToken.User.IsActive)
        {
            return Unauthorized(new { message = "User account is inactive." });
        }

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
            UserAgent = safeUserAgent
        };

        _context.TRefreshToken.Add(newRefresh);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            token = newJwt,
            refreshToken = newRefreshValue
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        var query = _context.TRefreshToken
            .Where(x => x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow)
            .AsEnumerable()
            .Where(x => BCrypt.Net.BCrypt.Verify(request.RefreshToken, x.TokenHash));

        var token = query.FirstOrDefault();

        if (token == null)
            return NotFound(new { message = "Token not found" });

        await RevokeTokenChainAsync(token.TokenHash);

        return Ok(new { message = "Logged out successfully" });
    }

    [NonAction]
    private string SanitizeUserAgent(
        string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "unknown";
        var sanitized = new string(userAgent.Where(c => !char.IsControl(c)).ToArray());
        return sanitized.Length > 255 ? sanitized[..255] : sanitized;
    }

    [NonAction]
    private async Task RevokeTokenChainAsync(
        string? tokenHash)
    {
        if (string.IsNullOrEmpty(tokenHash)) return;

        var token = await _context.TRefreshToken.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (token != null)
        {
            var nextTokenHash = token.ReplacedByTokenHash;

            if (token.RevokedAt == null)
            {
                token.RevokedAt = DateTime.UtcNow;
            }

            if (token.ExpiresAt <= DateTime.UtcNow)
            {
                _context.TRefreshToken.Remove(token);
            }

            if (!string.IsNullOrEmpty(nextTokenHash))
            {
                await RevokeTokenChainAsync(nextTokenHash);
            }
        }
    }

    [NonAction]
    public async Task RemoveExpiredOtps(
        int userId)
    {
        var query = _context.TUserOtp
            .Where(o => o.UserId == userId && (o.ExpiresAt <= DateTime.UtcNow || o.IsUsed));

        await query.ExecuteDeleteAsync();
    }

    [NonAction]
    public async Task RevokeUserRefreshTokens(
        int userId)
    {
        var query = _context.TRefreshToken
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow);
        await query.ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.RevokedAt, DateTime.UtcNow));
    }

    [NonAction]
    private string GenerateSecureOtp()
    {
        return RandomNumberGenerator.GetInt32(1000, 9999).ToString();
    }

    [NonAction]
    private string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    [NonAction]
    private string GenerateJwtToken(TUsers user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.PhoneNumber),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
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
}