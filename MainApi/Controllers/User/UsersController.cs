[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly MechanicContext _context;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, string> _otpStorage = new();

    public UsersController(MechanicContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.TUsers
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.IsActive);

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

        var otp = GenerateOtp();
        _otpStorage[request.PhoneNumber] = otp;

        return Ok(new { Message = "OTP sent successfully", Code = otp });
    }

    [HttpPost("verify-otp")]
    public async Task<ActionResult<AuthResponseDto>> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!_otpStorage.TryGetValue(request.PhoneNumber, out var storedOtp) ||
            storedOtp != request.Code)
            return BadRequest("Invalid OTP code");

        var user = await _context.TUsers
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.IsActive);

        if (user == null)
            return NotFound("User not found");

        _otpStorage.Remove(request.PhoneNumber);

        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        var response = new AuthResponseDto
        {
            Token = token,
            User = new UserProfileDto
            {
                Id = user.Id,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt
            },
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            RefreshToken = refreshToken
        };

        return Ok(response);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest("Refresh token is required");

        var principal = GetPrincipalFromExpiredToken(refreshToken);
        if (principal == null)
            return BadRequest("Invalid refresh token");

        var phoneNumber = principal.FindFirst(ClaimTypes.MobilePhone)?.Value;
        if (phoneNumber == null)
            return BadRequest("Invalid token claims");

        var user = await _context.TUsers
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && u.IsActive);

        if (user == null)
            return NotFound("User not found");

        var newToken = GenerateJwtToken(user);
        var newRefreshToken = GenerateRefreshToken();

        var response = new AuthResponseDto
        {
            Token = newToken,
            User = new UserProfileDto
            {
                Id = user.Id,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt
            },
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            RefreshToken = newRefreshToken
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
            new Claim(ClaimTypes.MobilePhone, user.PhoneNumber),
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