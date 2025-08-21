namespace MainApi.Controllers.User;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RateLimitService _rateLimitService;
    private readonly MechanicContext _context;
    private readonly IConfiguration _configuration;

    public UsersController(MechanicContext context, IConfiguration configuration, RateLimitService rateLimitService)
    {
        _context = context;
        _configuration = configuration;
        _rateLimitService = rateLimitService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var limiterKey = $"{request.PhoneNumber}:{HttpContext.Connection.RemoteIpAddress}";
        if (_rateLimitService.IsLimited(limiterKey))
            return BadRequest("Too many attempts. Try again later.");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.TUsers
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

        if (user == null)
        {
            user = new TUsers
            {
                PhoneNumber = request.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.TUsers.Add(user);
            await _context.SaveChangesAsync();
        }

        if (!user.IsActive)
        {
            return Unauthorized("User account is inactive.");
        }

        var otp = GenerateOtp();

        var existingOtps = _context.TUserOtps.Where(o => o.UserId == user.Id && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow);
        _context.TUserOtps.RemoveRange(existingOtps);

        var otpHash = BCrypt.Net.BCrypt.HashPassword(otp);

        var userOtp = new TUserOtp
        {
            UserId = user.Id,
            OtpHash = otpHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(2)
        };
        _context.TUserOtps.Add(userOtp);
        await _context.SaveChangesAsync();

        try
        {
            var apiKey = _configuration["Kavenegar:ApiKey"];
            var sender = _configuration["Kavenegar:SenderNumber"];

            var receptor = request.PhoneNumber;
            var message = $"کد تایید شما: {otp}";

            var api = new KavenegarApi(apiKey);
            var result = api.Send(sender, receptor, message);

             Console.WriteLine($"SMS sent to {receptor} with status: {result.StatusText}");
        }
        catch (ApiException ex)
        {
            // Log.Error(ex, "Kavenegar API Error");
            return StatusCode(500, "An error occurred while sending the OTP code.");
        }
        catch (HttpException ex)
        {
            // Log.Error(ex, "Kavenegar Connection Error");
            return StatusCode(500, "An error occurred while sending the OTP code.");
        }

        return Ok(new { Message = "OTP sent successfully" });
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        var limiterKey = $"{request.PhoneNumber}:{HttpContext.Connection.RemoteIpAddress}";
        if (_rateLimitService.IsLimited(limiterKey))
            return BadRequest("Too many attempts. Try again later.");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.TUsers
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.IsActive);

        if (user == null)
            return NotFound("User not found or is inactive.");

        var storedOtp = await _context.TUserOtps
            .FirstOrDefaultAsync(o => o.UserId == user.Id && o.ExpiresAt > DateTime.UtcNow && !o.IsUsed);

        if (storedOtp == null || !BCrypt.Net.BCrypt.Verify(request.Code, storedOtp.OtpHash))
        {
            return BadRequest("Invalid or expired OTP code.");
        }

        storedOtp.IsUsed = true;
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        var refreshToken = new TRefreshToken
        {
            UserId = user.Id,
            TokenHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
            UserAgent = Request.Headers["User-Agent"].ToString()
        };
        _context.Set<TRefreshToken>().Add(refreshToken);
        await _context.SaveChangesAsync();

        var response = new AuthResponseDto
        {
            Token = token,
            User = new UserProfileDto { Id = user.Id, PhoneNumber = user.PhoneNumber, CreatedAt = user.CreatedAt },
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            RefreshToken = refreshToken.TokenHash
        };

        return Ok(response);

    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest("Refresh token is required");

        var storedToken = await _context.Set<TRefreshToken>()
            .Include(t => t.UserId)
            .FirstOrDefaultAsync(t => t.TokenHash == refreshToken);

        if (storedToken == null || storedToken.RevokedAt != null || storedToken.ExpiresAt <= DateTime.UtcNow)
            return BadRequest("Invalid or expired refresh token");

        var user = await _context.TUsers.FirstOrDefaultAsync(u => u.Id == storedToken.UserId && u.IsActive);
        if (user == null)
            return NotFound("User not found");

        storedToken.RevokedAt = DateTime.UtcNow;

        var newRefreshToken = new TRefreshToken
        {
            UserId = user.Id,
            TokenHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
            UserAgent = Request.Headers["User-Agent"].ToString(),
            ReplacedByTokenHash = storedToken.TokenHash
        };

        _context.Set<TRefreshToken>().Add(newRefreshToken);
        await _context.SaveChangesAsync();

        var newAccessToken = GenerateJwtToken(user);

        var response = new AuthResponseDto
        {
            Token = newAccessToken,
            User = new UserProfileDto
            {
                Id = user.Id,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt
            },
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            RefreshToken = newRefreshToken.TokenHash
        };

        return Ok(response);
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<UserProfileDto>>> GetUsers()
    {
        return await _context.TUsers
            .Where(u => u.IsActive)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                PhoneNumber = u.PhoneNumber,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetUser(int id)
    {
        var user = await _context.TUsers
            .Where(u => u.Id == id && u.IsActive)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                PhoneNumber = u.PhoneNumber,
                CreatedAt = u.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return user;
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var user = await _context.TUsers
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                PhoneNumber = u.PhoneNumber,
                CreatedAt = u.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return user;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> CreateUser([FromBody] TUsers tUsers)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(tUsers.PhoneNumber))
            return BadRequest("Phone number is required.");

        if (await _context.TUsers.AnyAsync(u => u.PhoneNumber == tUsers.PhoneNumber))
            return Conflict("User with this phone number already exists.");

        tUsers.CreatedAt = DateTime.UtcNow;
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

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] TUsers tUsers)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (id != tUsers.Id)
            return BadRequest("ID mismatch");

        var existingUser = await _context.TUsers.FindAsync(id);
        if (existingUser == null || !existingUser.IsActive)
            return NotFound();

        var currentUserId = GetCurrentUserId();
        if (currentUserId != id)
            return Forbid();

        existingUser.FirstName = tUsers.FirstName;
        existingUser.LastName = tUsers.LastName;

        try
        {
            _context.Entry(existingUser).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.TUsers.AnyAsync(u => u.Id == id))
                return NotFound();
            throw;
        }

        return NoContent();
    }

    [HttpPatch("{id}/status")]
    [Authorize]
    public async Task<IActionResult> ChangeUserStatus(int id, [FromBody] bool isActive)
    {
        var user = await _context.TUsers.FindAsync(id);
        if (user == null)
            return NotFound();

        user.IsActive = isActive;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.TUsers.FindAsync(id);
        if (user == null)
            return NotFound();

        user.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    
    private string GenerateOtp()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    private string GenerateJwtToken(TUsers user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "YourSecretKeyHere123456789"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("id", user.Id.ToString()),
            new Claim(ClaimTypes.MobilePhone, user.PhoneNumber),
            new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };



        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "YourIssuer",
            audience: _configuration["Jwt:Audience"] ?? "YourAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "YourSecretKeyHere123456789"));

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"] ?? "YourIssuer",
            ValidAudience = _configuration["Jwt:Audience"] ?? "YourAudience",
            IssuerSigningKey = key
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

public class RateLimitService
{
    private static readonly Dictionary<string, (int Count, DateTime ResetAt)> Limits = new();
    private readonly int _maxAttempts = 5;
    private readonly TimeSpan _window = TimeSpan.FromMinutes(5);

    public bool IsLimited(string key)
    {
        if (!Limits.ContainsKey(key) || Limits[key].ResetAt < DateTime.UtcNow)
            Limits[key] = (0, DateTime.UtcNow.Add(_window));

        var entry = Limits[key];
        if (entry.Count >= _maxAttempts)
            return true;

        Limits[key] = (entry.Count + 1, entry.ResetAt);
        return false;
    }
}