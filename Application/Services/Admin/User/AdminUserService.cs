namespace Application.Services.Admin.User;

public class AdminUserService : IAdminUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork; 
    private readonly IMapper _mapper; 
    private readonly IAppLogger<AdminUserService> _logger;
    private readonly IAuditService _auditService; 
    private readonly IHtmlSanitizer _htmlSanitizer;

    public AdminUserService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAppLogger<AdminUserService> logger,
        IAuditService auditService,
        IHtmlSanitizer htmlSanitizer)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _auditService = auditService;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<ServiceResult<PagedResultDto<UserProfileDto>>> GetUsersAsync(bool includeDeleted, int page, int pageSize)
    {
        var (users, totalItems) = await _userRepository.GetUsersAsync(includeDeleted, page, pageSize);

        var dtos = _mapper.Map<List<UserProfileDto>>(users);

        var result = PagedResultDto<UserProfileDto>.Create(dtos, totalItems, page, pageSize);
        return ServiceResult<PagedResultDto<UserProfileDto>>.Ok(result);
    }

    public async Task<ServiceResult<UserProfileDto?>> GetUserByIdAsync(int id)
    {
        var user = await _userRepository.GetUserByIdWithAddressesAsync(id);
        if (user == null)
        {
            return ServiceResult<UserProfileDto?>.Fail("User not found");
        }

        var dto = _mapper.Map<UserProfileDto>(user);
        return ServiceResult<UserProfileDto?>.Ok(dto);
    }

    public async Task<ServiceResult<(UserProfileDto? User, string? Error)>> CreateUserAsync(User tUsers)
    {
        // This method signature seems to accept Domain.User.User directly which is against Clean Architecture for inputs usually,
        // but keeping it as per interface definition provided earlier. Ideally should be a DTO.

        if (await _userRepository.ExistsByPhoneNumberAsync(tUsers.PhoneNumber))
        {
            return ServiceResult<(UserProfileDto?, string?)>.Fail("User with this phone number already exists.");
        }

        tUsers.CreatedAt = DateTime.UtcNow;
        tUsers.IsActive = true;
        // Password handling should be done via a proper service if applicable, 
        // assuming tUsers already has properties set or this is a basic creation.

        await _userRepository.AddAsync(tUsers);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<UserProfileDto>(tUsers);
        return ServiceResult<(UserProfileDto?, string?)>.Ok((dto, null));
    }

    public async Task<ServiceResult> UpdateUserAsync(int id, UpdateProfileDto updateRequest, int currentUserId)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return ServiceResult.Fail("NotFound");

        if (user.IsDeleted) return ServiceResult.Fail("User account is deleted and cannot be modified.");

        if (!string.IsNullOrEmpty(updateRequest.FirstName))
            user.FirstName = _htmlSanitizer.Sanitize(updateRequest.FirstName);

        if (!string.IsNullOrEmpty(updateRequest.LastName))
            user.LastName = _htmlSanitizer.Sanitize(updateRequest.LastName);

        user.UpdatedAt = DateTime.UtcNow;
        _userRepository.Update(user);

        try
        {
            await _unitOfWork.SaveChangesAsync();
            await _auditService.LogAdminEventAsync("UpdateUser", currentUserId, $"Updated profile for user {id}");
            return ServiceResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail("User was modified by another process");
        }
    }

    public async Task<ServiceResult> ChangeUserStatusAsync(int id, bool isActive)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return ServiceResult.Fail("NotFound");

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        _userRepository.Update(user);

        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteUserAsync(int id, int currentUserId)
    {
        if (id == currentUserId)
        {
            return ServiceResult.Fail("Admins cannot delete their own account this way.");
        }

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return ServiceResult.Fail("NotFound");

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false; // Deactivate as well

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAdminEventAsync("DeleteUser", currentUserId, $"Soft-deleted user {id}");

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RestoreUserAsync(int id)
    {
        var user = await _userRepository.GetByIdIncludingDeletedAsync(id);
        if (user == null || !user.IsDeleted) return ServiceResult.Fail("NotFound");

        user.IsDeleted = false;
        user.DeletedAt = null;
        user.IsActive = true;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok();
    }
}