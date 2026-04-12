using Application.Audit.Features.Queries.GetAuditLogs;
using Application.Audit.Features.Queries.GetAuditStatistics;
using Mapster;
using Presentation.Audit.Requests;

namespace Presentation.Audit.Mapping;

public sealed class AuditMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GetAuditLogsRequest, GetAuditLogsQuery>();
        config.NewConfig<GetAuditStatisticsRequest, GetAuditStatisticsQuery>();
    }
}