namespace MainApi.Controllers.User;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly MechanicContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UsersController> _logger;
    private readonly IRateLimitService _rateLimitService;

    public UsersController(MechanicContext context, IConfiguration configuration, ILogger<UsersController> logger, IRateLimitService rateLimitService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _rateLimitService = rateLimitService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserProfileDto>>> GetUsers()
    {
        try
        {
            var users = await _context.TUsers
                .Where(u => !u.IsDeleted)
                .Select(u => new UserProfileDto
                {
                    Id = u.Id,
                    PhoneNumber = u.PhoneNumber,
                    CreatedAt = u.CreatedAt
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
    public async Task<ActionResult<UserProfileDto>> GetUser(int id)
    {
        try
        {
            var user = await _context.TUsers
                .Where(u => u.Id == id && !u.IsDeleted)
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
    public async Task<ActionResult<UserProfileDto>> CreateUser([FromBody] TUsers tUsers)
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
    public async Task<IActionResult> UpdateUser(int id, [FromBody] TUsers updateRequest)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != id && !User.IsInRole("Admin"))
                return Forbid();

            var existingUser = await _context.TUsers.FindAsync(id);
            if (existingUser == null || existingUser.IsDeleted)
                return NotFound();

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
    public async Task<IActionResult> ChangeUserStatus(int id, [FromBody] bool isActive)
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
    public async Task<IActionResult> DeleteUser(int id)
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
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
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
            var loginRateLimitKey = $"login_{clientIp}_{request.PhoneNumber}";
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

            var apiKey = _configuration["Kavenegar:ApiKey"];
            var template = "verify";

            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("SMS service is not configured.");

            var api = new KavenegarApi(apiKey);
            await Task.Run(() => api.VerifyLookup(request.PhoneNumber, otp, template));

            _logger.LogInformation("OTP sent successfully to phone: {PhoneNumber}", request.PhoneNumber);
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
    public async Task<ActionResult<AuthResponseDto>> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var rateLimitKey = $"otp_{clientIp}_{request.PhoneNumber}";

        if (await _rateLimitService.IsLimitedAsync(rateLimitKey, 5, 15))
            return StatusCode(429, "Too many verification attempts. Please try again later.");

        var user = await _context.TUsers.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.IsActive);
        if (user == null)
            return BadRequest("Invalid credentials.");

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
                CreatedAt = user.CreatedAt
            },
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            RefreshToken = refreshTokenValue
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequestDto request)
    {
        var storedTokens = _context.TRefreshToken
            .Include(x => x.User)
            .Where(x => x.ExpiresAt > DateTime.UtcNow && x.RevokedAt == null)
            .AsEnumerable()
            .Where(x => BCrypt.Net.BCrypt.Verify(request.RefreshToken, x.TokenHash))
            .ToList();

        var storedToken = storedTokens.FirstOrDefault();

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        var safeUserAgent = SanitizeUserAgent(userAgent);

        if (storedToken == null || storedToken.User == null || !storedToken.User.IsActive)
        {
            if (storedToken != null)
            {
                _logger.LogWarning("Potential token reuse detected for token belonging to user {UserId}. Revoking chain.", storedToken.UserId);
                await RevokeTokenChainAsync(storedToken);
            }
            return Unauthorized(new { message = "Invalid token." });
        }

        if (storedToken.UserAgent != safeUserAgent || storedToken.CreatedByIp != clientIp)
        {
            _logger.LogWarning("Session context mismatch for user {UserId}. IP or UserAgent changed. IP: {StoredIP} vs {CurrentIP}, UA: {StoredUA} vs {CurrentUA}",
                storedToken.UserId, storedToken.CreatedByIp, clientIp, storedToken.UserAgent, safeUserAgent);
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
    public async Task<IActionResult> Logout([FromBody] RefreshRequestDto request)
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

        await RevokeTokenChainAsync(token);

        return Ok(new { message = "Logged out successfully" });
    }

    [NonAction]
    private string SanitizeUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "unknown";
        var sanitized = new string(userAgent.Where(c => !char.IsControl(c)).ToArray());
        return sanitized.Length > 255 ? sanitized[..255] : sanitized;
    }

    [NonAction]
    private async Task RevokeTokenChainAsync(TRefreshToken token)
    {
        if (token == null) return;

        token.RevokedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(token.ReplacedByTokenHash))
        {
            var childTokens = await _context.TRefreshToken
                .Where(t => t.TokenHash == token.ReplacedByTokenHash && t.RevokedAt == null)
                .ToListAsync();

            foreach (var child in childTokens)
            {
                await RevokeTokenChainAsync(child);
            }
        }
    }

    [NonAction]
    public async Task RemoveExpiredOtps(int userId)
    {
        var query = _context.TUserOtp
            .Where(o => o.UserId == userId && (o.ExpiresAt <= DateTime.UtcNow || o.IsUsed));

        await query.ExecuteDeleteAsync();
    }

    [NonAction]
    public async Task RevokeUserRefreshTokens(int userId)
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

    [NonAction]
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return null;
        return userId;
    }
}