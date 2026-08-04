using System.Runtime.ExceptionServices;
using Domain.User.ValueObjects;

namespace Application.Common.Behaviors;

public sealed class AuditingBehavior<TRequest, TResponse>(
    IAuditService auditService,
    ICurrentUserService currentUserService,
    IAuditContextEnricher auditContextEnricher,
    ILogger<AuditingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is not IAuditableCommand auditable)
            return await next(ct);

        TResponse response = default!;
        Exception? thrown = null;

        try
        {
            response = await next(ct);
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

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

            auditContextEnricher.Set("actorId", currentUserService.UserId?.ToString() ?? "system");
            auditContextEnricher.Set("actorIp", ipRaw ?? "system");
            auditContextEnricher.Set("actorIsAdmin", currentUserService.IsAdmin.ToString());
            auditContextEnricher.Set("sessionId", currentUserService.SessionId?.ToString());

            var enrichedDetails = auditable.BuildAuditDetails(auditContextEnricher);

            if (thrown is not null)
            {
                var reason = $"{thrown.GetType().Name}: {thrown.Message}";
                var details = string.IsNullOrWhiteSpace(enrichedDetails)
                    ? $"Command {requestName} threw exception. {reason}"
                    : $"Command {requestName} threw exception. {reason} | {enrichedDetails}";

                await auditService.LogAsync(
                    auditable.AuditEventType,
                    $"{auditable.AuditAction}.Exception",
                    ipAddress,
                    userId,
                    auditable.AuditEntityType,
                    auditable.AuditEntityId,
                    details,
                    userAgent,
                    ct);
            }
            else if (response is ServiceResult result && result.IsSuccess)
            {
                var details = string.IsNullOrWhiteSpace(enrichedDetails)
                    ? $"Command {requestName} executed successfully."
                    : $"Command {requestName} executed successfully. | {enrichedDetails}";

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
                var reason = failure.Error.Message;
                var details = string.IsNullOrWhiteSpace(enrichedDetails)
                    ? $"Command {requestName} failed. {reason}"
                    : $"Command {requestName} failed. {reason} | {enrichedDetails}";

                await auditService.LogAsync(
                    auditable.AuditEventType,
                    $"{auditable.AuditAction}.Failed",
                    ipAddress,
                    userId,
                    auditable.AuditEntityType,
                    auditable.AuditEntityId,
                    details,
                    userAgent,
                    ct);
            }
        }
        catch (Exception auditEx)
        {
            logger.LogError(
                auditEx,
                "AuditingBehavior failed to record audit for {RequestName}: {Message}",
                typeof(TRequest).Name,
                auditEx.Message);
        }
        finally
        {
            auditContextEnricher.Clear();
        }

        if (thrown is not null)
            ExceptionDispatchInfo.Capture(thrown).Throw();

        return response;
    }
}
