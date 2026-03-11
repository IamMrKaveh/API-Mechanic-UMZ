using Application.Common.Models;

namespace Application.User.Features.Commands.ChangeUserRole;

public record ChangeUserRoleCommand(
    int UserId,
    bool IsAdmin,
    int AdminUserId
) : IRequest<ServiceResult>;