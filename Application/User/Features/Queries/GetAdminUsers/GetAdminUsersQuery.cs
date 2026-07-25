using Application.User.Features.Shared;

namespace Application.User.Features.Queries.GetAdminUsers;

public record GetAdminUsersQuery(
    string? Search = null,
    string? Role = null,
    bool? IsActive = null,
    bool? IsAdmin = null,
    decimal? MinTotalSpent = null,
    DateTime? CreatedAfter = null,
    bool IncludeDeleted = false,
    int Page = 1,
    int PageSize = 20)
    : IPageQuery<AdminUserListItemDto>;
