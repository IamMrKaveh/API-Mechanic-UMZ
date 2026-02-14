namespace Application.User.Features.Queries.GetUsers;

public class GetAdminUsersHandler : IRequestHandler<GetAdminUsersQuery, ServiceResult<PaginatedResult<UserProfileDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetAdminUsersHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<PaginatedResult<UserProfileDto>>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        var (users, totalItems) = await _userRepository.GetUsersAsync(request.IncludeDeleted, request.Page, request.PageSize);

        var dtos = _mapper.Map<List<UserProfileDto>>(users);

        var result = PaginatedResult<UserProfileDto>.Create(dtos, totalItems, request.Page, request.PageSize);
        return ServiceResult<PaginatedResult<UserProfileDto>>.Success(result);
    }
}