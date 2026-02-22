namespace Application.Audit.Features.Queries.GetAuditLogs;

public sealed class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, GetAuditLogsResult>
{
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<GetAuditLogsHandler> _logger;

    public GetAuditLogsHandler(
        IAuditRepository auditRepository,
        ILogger<GetAuditLogsHandler> logger
        )
    {
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task<GetAuditLogsResult> Handle(
        GetAuditLogsQuery request,
        CancellationToken ct
        )
    {
        var (logs, total) = await _auditRepository.GetAuditLogsAsync(
            request.From,
            request.To,
            request.UserId,
            request.EventType,
            request.Page,
            request.PageSize);

        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);

        return new GetAuditLogsResult(
            logs.Select(l => new AuditDtos
            {
                Id = l.Id,
                UserId = l.UserId,
                EventType = l.EventType,
                Action = l.Action,
                Details = l.Details,
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent,
                Timestamp = l.Timestamp,
                IsArchived = l.IsArchived
            }),
            total,
            request.Page,
            request.PageSize,
            totalPages);
    }
}