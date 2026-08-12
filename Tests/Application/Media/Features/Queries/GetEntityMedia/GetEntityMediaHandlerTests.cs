using Application.Media.Contracts;
using Application.Media.Features.Queries.GetEntityMedia;
using Application.Media.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Media.Features.Queries.GetEntityMedia;

public class GetEntityMediaHandlerTests
{
    private readonly IMediaQueryService _mediaQueryService = Substitute.For<IMediaQueryService>(); private readonly GetEntityMediaHandler _sut;

    public GetEntityMediaHandlerTests()
    {
        _sut = new GetEntityMediaHandler(_mediaQueryService);
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsCollection_ReturnsSuccessWithSameCollection()
    {
        IReadOnlyList<MediaDto> expected = Array.Empty<MediaDto>();

        _mediaQueryService
            .GetByEntityAsync("Product", Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(
            new GetEntityMediaQuery("Product", Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task Handle_PassesEntityTypeAndEntityIdVerbatimToQueryService()
    {
        var entityId = Guid.NewGuid();
        const string entityType = "Brand";

        _mediaQueryService
            .GetByEntityAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<MediaDto>());

        _ = await _sut.Handle(
            new GetEntityMediaQuery(entityType, entityId),
            CancellationToken.None);

        await _mediaQueryService.Received(1).GetByEntityAsync(entityType, entityId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToQueryService()
    {
        using var cts = new CancellationTokenSource();
        _mediaQueryService
            .GetByEntityAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<MediaDto>());

        _ = await _sut.Handle(new GetEntityMediaQuery("Product", Guid.NewGuid()), cts.Token);

        await _mediaQueryService.Received(1).GetByEntityAsync(
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            cts.Token);
    }
}
