using Application.Common.Results;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.ChangeUserRole;

public record ChangeUserRoleCommand(
    UserId UserId,
    bool IsAdmin,
    UserId AdminUserId) : IRequest<ServiceResult>;