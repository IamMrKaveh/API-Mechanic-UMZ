using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetInventoryReport;

public sealed record GetInventoryReportQuery : IRequest<ServiceResult<PaginatedResult<InventoryReportDto>>>;