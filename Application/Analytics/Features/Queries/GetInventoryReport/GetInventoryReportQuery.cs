namespace Application.Analytics.Features.Queries.GetInventoryReport;

public sealed record GetInventoryReportQuery : IRequest<ServiceResult<InventoryReportDto>>;