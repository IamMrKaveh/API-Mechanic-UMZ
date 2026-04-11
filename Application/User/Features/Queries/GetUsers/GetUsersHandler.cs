using Application.User.Features.Shared;

namespace Application.User.Features.Queries.GetUsers;

public class GetUsersHandler(IUserQueryService userQueryService) : IRequestHandler<GetUsersQuery, ServiceResult<PaginatedResult<UserProfileDto>>>
{
    public async Task<ServiceResult<PaginatedResult<UserProfileDto>>> Handle(
        GetUsersQuery request,
        CancellationToken ct)
    {
        var result = await userQueryService.GetUsersPagedAsync(
            null,
            null,
            null,
            request.IncludeDeleted,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<UserProfileDto>>.Success(result);
    }
}