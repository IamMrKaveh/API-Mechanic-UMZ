using Application.Common.Models;

namespace Application.User.Features.Queries.GetUsers;

public class GetUsersHandler : IRequestHandler<GetUsersQuery, ServiceResult<PaginatedResult<UserProfileDto>>>
{
    private readonly IUserQueryService _userQueryService;

    public GetUsersHandler(IUserQueryService userQueryService)
    {
        _userQueryService = userQueryService;
    }

    public async Task<ServiceResult<PaginatedResult<UserProfileDto>>> Handle(
        GetUsersQuery request,
        CancellationToken ct)
    {
        var result = await _userQueryService.GetUsersPagedAsync(
            search: null,
            isActive: null,
            isAdmin: null,
            includeDeleted: request.IncludeDeleted,
            page: request.Page,
            pageSize: request.PageSize,
            ct: ct);

        return ServiceResult<PaginatedResult<UserProfileDto>>.Success(result);
    }
}