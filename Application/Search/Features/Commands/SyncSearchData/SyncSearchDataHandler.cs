namespace Application.Search.Features.Commands.SyncSearchData;

public class SyncSearchDataHandler : IRequestHandler<SyncSearchDataCommand, ServiceResult>
{
    private readonly ISearchDatabaseSyncService _syncService;

    public SyncSearchDataHandler(ISearchDatabaseSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task<ServiceResult> Handle(SyncSearchDataCommand request, CancellationToken ct)
    {
        await _syncService.FullSyncAsync(ct);
        return ServiceResult.Success();
    }
}