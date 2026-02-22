namespace Application.Audit.Features.Queries.GetAuditStatistics;

public sealed record GetAuditStatisticsQuery(
    DateTime? From,
    DateTime? To
) : IRequest<AuditStatisticsDto>;

public sealed record AuditStatisticsDto(
    int TotalLogs,
    int FinancialLogs,
    int SecurityLogs,
    int AdminLogs,
    IEnumerable<EventTypeCountDto> ByEventType,
    IEnumerable<HourlyCountDto> ByHour
    );

public sealed record EventTypeCountDto(
    string EventType,
    int Count
    );

public sealed record HourlyCountDto(
    int Hour,
    int Count
    );