namespace Application.User.Features.Commands.CreateUser;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, ServiceResult<UserProfileDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateUserHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ServiceResult<UserProfileDto>> Handle(CreateUserCommand request, CancellationToken ct)
    {
        if (await _userRepository.PhoneNumberExistsAsync(request.Dto.PhoneNumber, 0, ct))
        {
            return ServiceResult<UserProfileDto>.Failure("User with this phone number already exists.");
        }

        var user = Domain.User.User.Create(
            request.Dto.PhoneNumber);

        await _userRepository.AddUserAsync(user);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = _mapper.Map<UserProfileDto>(user);
        return ServiceResult<UserProfileDto>.Success(dto);
    }
}