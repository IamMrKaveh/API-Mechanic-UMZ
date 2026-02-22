namespace Application.User.Features.Commands.UpdateProfile;

public class UpdateProfileHandler : IRequestHandler<UpdateProfileCommand, ServiceResult<UserProfileDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateProfileHandler> _logger;

    public UpdateProfileHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IMapper mapper,
        ILogger<UpdateProfileHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResult<UserProfileDto>> Handle(
        UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return ServiceResult<UserProfileDto>.Failure("کاربر یافت نشد.", 404);

        try
        {
            // Domain Logic: Update Profile (validation inside aggregate)
            user.UpdateProfile(request.FirstName, request.LastName, request.Email);

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogUserActionAsync(
                request.UserId,
                "UpdateProfile",
                "پروفایل کاربر به‌روزرسانی شد.",
                "system");

            return ServiceResult<UserProfileDto>.Success(_mapper.Map<UserProfileDto>(user));
        }
        catch (Domain.Common.Exceptions.DomainException ex)
        {
            return ServiceResult<UserProfileDto>.Failure(ex.Message);
        }
    }
}