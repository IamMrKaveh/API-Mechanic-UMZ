namespace Application.User.Features.Queries.GetUsers;

public record GetAdminUsersQuery(bool IncludeDeleted, int Page, int PageSize) : IRequest<ServiceResult<PaginatedResult<UserProfileDto>>>;