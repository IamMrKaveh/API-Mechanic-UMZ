using Application.Audit.Contracts;
using Application.Common.Results;
using Application.User.Features.Shared;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.User.Interfaces;

namespace Application.User.Features.Commands.UpdateProfile;

public class UpdateProfileHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IMapper mapper,
    ILogger<UpdateProfileHandler> logger) : IRequestHandler<UpdateProfileCommand, ServiceResult<UserProfileDto>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<UpdateProfileHandler> _logger = logger;

    public async Task<ServiceResult<UserProfileDto>> Handle(
        UpdateProfileCommand request,
        CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return ServiceResult<UserProfileDto>.NotFound("کاربر یافت نشد.");

        try
        {
            if (request.FullName is null)
                return ServiceResult<UserProfileDto>.NotFound("نام خالی میباشد");

            user.UpdateProfile(request.FullName, request.PhoneNumber);

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);

            await _auditService.LogUserActionAsync(
                request.UserId,
                "UpdateProfile",
                "پروفایل کاربر به‌روزرسانی شد.",
                "system");

            return ServiceResult<UserProfileDto>.Success(_mapper.Map<UserProfileDto>(user));
        }
        catch (DomainException ex)
        {
            return ServiceResult<UserProfileDto>.Unexpected(ex.Message);
        }
    }
}