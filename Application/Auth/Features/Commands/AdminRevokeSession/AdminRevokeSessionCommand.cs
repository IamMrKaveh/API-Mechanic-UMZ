namespace Application.Auth.Features.Commands.AdminRevokeSession;

public record AdminRevokeSessionCommand(Guid TargetUserId, Guid SessionId) : ICommand;