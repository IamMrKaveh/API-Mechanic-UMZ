namespace Application.Auth.Features.Commands.RevokeSession;

public record RevokeSessionCommand(int UserId, int SessionId) : IRequest<ServiceResult>;