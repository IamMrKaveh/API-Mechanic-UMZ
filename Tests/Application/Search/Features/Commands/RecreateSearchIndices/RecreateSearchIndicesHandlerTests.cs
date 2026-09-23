using Application.Search.Contracts;
using Application.Search.Features.Commands.RecreateSearchIndices;

namespace Tests.Application.Search.Features.Commands.RecreateSearchIndices;

public class RecreateSearchIndicesHandlerTests
{
    private readonly IElasticIndexManager _indexManager = Substitute.For<IElasticIndexManager>();
    private readonly RecreateSearchIndicesHandler _sut;

    public RecreateSearchIndicesHandlerTests()
    {
        _sut = new RecreateSearchIndicesHandler(_indexManager);
    }

    [Fact]
    public async Task Handle_DeletesIndicesInOrderThenRecreatesAndReturnsSuccess()
    {
        var result = await _sut.Handle(new RecreateSearchIndicesCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        Received.InOrder(() =>
        {
            _indexManager.DeleteIndexAsync("products_v1", Arg.Any<CancellationToken>());
            _indexManager.DeleteIndexAsync("categories_v1", Arg.Any<CancellationToken>());
            _indexManager.DeleteIndexAsync("brands_v1", Arg.Any<CancellationToken>());
            _indexManager.CreateAllIndicesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_ForwardsCancellationToken()
    {
        using var cts = new CancellationTokenSource();

        await _sut.Handle(new RecreateSearchIndicesCommand(), cts.Token);

        await _indexManager.Received(1).DeleteIndexAsync("products_v1", cts.Token);
        await _indexManager.Received(1).CreateAllIndicesAsync(cts.Token);
    }

    [Fact]
    public async Task Handle_WhenDeleteFails_PropagatesException()
    {
        _indexManager.DeleteIndexAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("elastic down"));

        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.Handle(new RecreateSearchIndicesCommand(), CancellationToken.None));

        await _indexManager.DidNotReceiveWithAnyArgs().CreateAllIndicesAsync(Arg.Any<CancellationToken>());
    }
}
