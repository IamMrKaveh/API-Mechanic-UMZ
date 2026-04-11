namespace Application.Auth.Features.Commands.RevokeSession;

public record RevokeSessionCommand(Guid UserId, Guid SessionId) : IRequest<ServiceResult>;