using Application.Media.Contracts;
using Application.Media.Features.Commands.ReorderMedia;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Media.Features.Commands.ReorderMedia;

public class ReorderMediaHandlerTests
{
    private readonly IMediaService _mediaService = Substitute.For<IMediaService>(); private readonly ReorderMediaHandler _sut;

    public ReorderMediaHandlerTests()
    {
        _sut = new ReorderMediaHandler(_mediaService);
    }

    [Fact]
    public async Task Handle_DelegatesToMediaServiceWithVerbatimArguments()
    {
        var entityId = Guid.NewGuid();
        var orderedIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        _mediaService
            .ReorderAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<ICollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.Handle(
            new ReorderMediaCommand("Product", entityId, orderedIds),
            CancellationToken.None);

        result.ShouldBeSuccess();

        await _mediaService.Received(1).ReorderAsync(
            "Product",
            entityId,
            orderedIds,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsWhateverMediaServiceReturns()
    {
        var expected = ServiceResult.Failure(Error.NotFound("Media.Reorder.NotFound", "not-found"));

        _mediaService
            .ReorderAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<ICollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(
            new ReorderMediaCommand("Brand", Guid.NewGuid(), new List<Guid> { Guid.NewGuid() }),
            CancellationToken.None);

        result.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToMediaService()
    {
        using var cts = new CancellationTokenSource();
        _mediaService
            .ReorderAsync(
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<ICollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        _ = await _sut.Handle(
            new ReorderMediaCommand("Product", Guid.NewGuid(), new List<Guid> { Guid.NewGuid() }),
            cts.Token);

        await _mediaService.Received(1).ReorderAsync(
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<ICollection<Guid>>(),
            cts.Token);
    }
}
