using Application.User.Features.Shared;

namespace Application.User.Features.Queries.GetAdminUsers;

public sealed class GetAdminUsersHandler(IUserQueryService userQueryService)
    : IQueryHandler<GetAdminUsersQuery, PaginatedResult<AdminUserListItemDto>>
{
    public async Task<ServiceResult<PaginatedResult<AdminUserListItemDto>>> Handle(
        GetAdminUsersQuery request,
        CancellationToken ct)
    {
        var filter = new AdminUserFilterParams(
            request.Search,
            request.Role,
            request.IsActive,
            request.IsAdmin,
            request.MinTotalSpent,
            request.CreatedAfter,
            request.IncludeDeleted,
            request.Page,
            request.PageSize);

        var result = await userQueryService.GetAdminUsersPagedAsync(filter, ct);
        return ServiceResult<PaginatedResult<AdminUserListItemDto>>.Success(result);
    }
}
