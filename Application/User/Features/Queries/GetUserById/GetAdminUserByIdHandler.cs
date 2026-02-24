namespace Application.User.Features.Queries.GetUserById;

public class GetAdminUserByIdHandler : IRequestHandler<GetAdminUserByIdQuery, ServiceResult<UserProfileDto?>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetAdminUserByIdHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<UserProfileDto?>> Handle(GetAdminUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id); 
        if (user == null)
        {
            return ServiceResult<UserProfileDto?>.Failure("User not found");
        }

        var dto = _mapper.Map<UserProfileDto>(user);
        return ServiceResult<UserProfileDto?>.Success(dto);
    }
}