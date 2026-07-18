using Domain.User.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Common.Behaviors;

public sealed class AuditingBehavior<TRequest, TResponse>(
    IAuditService auditService,
    ICurrentUserService currentUserService,
    ILogger<AuditingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var response = await next(ct);

        if (request is not IAuditableCommand auditable)
            return response;

        try
        {
            var ipRaw = currentUserService.IpAddress;
            var ipAddress = string.IsNullOrWhiteSpace(ipRaw)
                ? IpAddress.System
                : IpAddress.Create(ipRaw);

            var userId = currentUserService.UserId.HasValue
                ? UserId.From(currentUserService.UserId.Value)
                : null;

            var userAgent = currentUserService.UserAgent;
            var requestName = typeof(TRequest).Name;

            if (response is ServiceResult result && result.IsSuccess)
            {
                var details = $"Command {requestName} executed successfully.";

                await auditService.LogAsync(
                    auditable.AuditEventType,
                    auditable.AuditAction,
                    ipAddress,
                    userId,
                    auditable.AuditEntityType,
                    auditable.AuditEntityId,
                    details,
                    userAgent,
                    ct);
            }
            else if (response is ServiceResult failure && failure.IsFailure)
            {
                var details = $"Command {requestName} failed: {failure.Error.Message}.";
                await auditService.LogWarningAsync(details, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "AuditingBehavior failed to record audit for {RequestName}: {Message}",
                typeof(TRequest).Name,
                ex.Message);
        }

        return response;
    }
}