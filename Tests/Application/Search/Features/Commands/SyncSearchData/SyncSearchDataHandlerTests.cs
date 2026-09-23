using Application.Search.Contracts;
using Application.Search.Features.Commands.SyncSearchData;

namespace Tests.Application.Search.Features.Commands.SyncSearchData;

public class SyncSearchDataHandlerTests
{
    private readonly ISearchDatabaseSyncService _syncService = Substitute.For<ISearchDatabaseSyncService>();
    private readonly SyncSearchDataHandler _sut;

    public SyncSearchDataHandlerTests()
    {
        _sut = new SyncSearchDataHandler(_syncService);
    }

    [Fact]
    public async Task Handle_RunsFullSyncAndReturnsSuccess()
    {
        var result = await _sut.Handle(new SyncSearchDataCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        await _syncService.Received(1).FullSyncAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForwardsCancellationToken()
    {
        using var cts = new CancellationTokenSource();

        await _sut.Handle(new SyncSearchDataCommand(), cts.Token);

        await _syncService.Received(1).FullSyncAsync(cts.Token);
    }

    [Fact]
    public async Task Handle_WhenSyncThrows_PropagatesException()
    {
        _syncService.FullSyncAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db down"));

        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.Handle(new SyncSearchDataCommand(), CancellationToken.None));
    }
}
