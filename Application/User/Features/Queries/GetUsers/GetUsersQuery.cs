using Application.Common.Results;
using Application.User.Features.Shared;
using SharedKernel.Models;

namespace Application.User.Features.Queries.GetUsers;

public record GetUsersQuery(
    bool IncludeDeleted,
    int Page,
    int PageSize) : IRequest<ServiceResult<PaginatedResult<UserProfileDto>>>;