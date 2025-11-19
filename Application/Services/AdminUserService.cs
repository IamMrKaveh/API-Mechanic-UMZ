namespace Application.Services;

public class AdminUserService : IAdminUserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<AdminUserService> _logger;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public AdminUserService(
        IUserRepository repository,
        ILogger<AdminUserService> logger,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _logger = logger;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<PagedResultDto<UserProfileDto>>> GetUsersAsync(bool includeDeleted, int page, int pageSize)
    {
        var (users, total) = await _repository.GetUsersAsync(includeDeleted, page, pageSize);
        var dtos = _mapper.Map<IEnumerable<UserProfileDto>>(users);

        var result = new PagedResultDto<UserProfileDto>
        {
            Items = dtos,
            TotalItems = total,
            Page = page,
            PageSize = pageSize
        };
        return ServiceResult<PagedResultDto<UserProfileDto>>.Ok(result);
    }


    public async Task<ServiceResult<UserProfileDto?>> GetUserByIdAsync(int id)
    {
        var user = await _repository.GetUserByIdAsync(id, true);
        if (user == null) return ServiceResult<UserProfileDto?>.Fail("User not found");
        var dto = _mapper.Map<UserProfileDto>(user);
        return ServiceResult<UserProfileDto?>.Ok(dto);
    }

    public async Task<ServiceResult<(UserProfileDto? User, string? Error)>> CreateUserAsync(Domain.User.User tUsers)
    {
        if (string.IsNullOrWhiteSpace(tUsers.PhoneNumber))
            return ServiceResult<(UserProfileDto?, string?)>.Fail("Phone number is required.");

        if (await _repository.PhoneNumberExistsAsync(tUsers.PhoneNumber))
            return ServiceResult<(UserProfileDto?, string?)>.Fail("User with this phone number already exists.");

        tUsers.CreatedAt = DateTime.UtcNow;
        tUsers.IsActive = true;

        await _repository.AddUserAsync(tUsers);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<UserProfileDto>(tUsers);
        return ServiceResult<(UserProfileDto?, string?)>.Ok((dto, null));
    }

    public async Task<ServiceResult> UpdateUserAsync(int id, UpdateProfileDto updateRequest, int currentUserId)
    {
        var existingUser = await _repository.GetUserByIdAsync(id, true);
        if (existingUser == null) return ServiceResult.Fail("NotFound");
        if (existingUser.IsDeleted) return ServiceResult.Fail("User account is deleted and cannot be modified.");

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

    public async Task<ServiceResult> ChangeUserStatusAsync(int id, bool isActive)
    {
        var user = await _repository.GetUserByIdAsync(id, true);
        if (user == null) return ServiceResult.Fail("NotFound");

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteUserAsync(int id, int currentUserId)
    {
        if (id == currentUserId) return ServiceResult.Fail("Admins cannot delete their own account this way.");

        var user = await _repository.GetUserByIdAsync(id, true);
        if (user == null) return ServiceResult.Fail("NotFound");

        if (user.IsAdmin)
        {
            _logger.LogWarning("Security attempt: User {CurrentUserId} tried to delete admin user {TargetUserId}.", currentUserId, id);
            return ServiceResult.Fail("Admins cannot be deleted for security reasons.");
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;
        user.PhoneNumber = $"{user.PhoneNumber}_deleted_{DateTime.UtcNow.Ticks}";

        await _repository.RevokeAllUserSessionsAsync(id);

        _repository.UpdateUser(user);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RestoreUserAsync(int id)
    {
        var user = await _repository.GetUserByIdAsync(id, true);
        if (user == null || !user.IsDeleted) return ServiceResult.Fail("NotFound");

        user.IsDeleted = false;
        user.DeletedAt = null;
        user.IsActive = true;

        _repository.UpdateUser(user);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}