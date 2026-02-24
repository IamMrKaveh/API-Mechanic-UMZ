namespace Application.User.Features.Queries.GetUsers;

public class GetUsersHandler : IRequestHandler<GetUsersQuery, ServiceResult<PaginatedResult<UserProfileDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUsersHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<PaginatedResult<UserProfileDto>>> Handle(
        GetUsersQuery request,
        CancellationToken ct
        )
    {
        var query = _userRepository.GetUsersQuery(request.IncludeDeleted);

        var totalItems = await query.CountAsync(ct);

        var dtos = await _mapper.ProjectTo<UserProfileDto>(query)
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var result = PaginatedResult<UserProfileDto>.Create(dtos, totalItems, request.Page, request.PageSize);
        return ServiceResult<PaginatedResult<UserProfileDto>>.Success(result);
    }
}