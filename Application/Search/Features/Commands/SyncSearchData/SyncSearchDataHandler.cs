namespace Application.Search.Features.Commands.SyncSearchData;

public class SyncSearchDataHandler(ISearchDatabaseSyncService syncService) : IRequestHandler<SyncSearchDataCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(SyncSearchDataCommand request, CancellationToken ct)
    {
        await syncService.FullSyncAsync(ct);
        return ServiceResult.Success();
    }
}