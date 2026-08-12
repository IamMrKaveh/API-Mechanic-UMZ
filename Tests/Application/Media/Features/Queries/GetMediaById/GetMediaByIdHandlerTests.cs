using Application.Media.Contracts;
using Application.Media.Features.Queries.GetMediaById;
using Application.Media.Features.Shared;
using Domain.Media.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Media.Features.Queries.GetMediaById;

public class GetMediaByIdHandlerTests
{
    private readonly IMediaQueryService _mediaQueryService = Substitute.For<IMediaQueryService>(); private readonly GetMediaByIdHandler _sut;

    public GetMediaByIdHandlerTests()
    {
        _sut = new GetMediaByIdHandler(_mediaQueryService);
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsNull_ReturnsNotFound()
    {
        _mediaQueryService
            .GetByIdAsync(Arg.Any<MediaId>(), Arg.Any<CancellationToken>())
            .Returns((MediaDto?)null);

        var result = await _sut.Handle(new GetMediaByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_PassesMediaIdBuiltFromRequestIdToQueryService()
    {
        var id = Guid.NewGuid();
        MediaId? captured = null;

        _mediaQueryService
            .GetByIdAsync(
                Arg.Do<MediaId>(x => captured = x),
                Arg.Any<CancellationToken>())
            .Returns((MediaDto?)null);

        _ = await _sut.Handle(new GetMediaByIdQuery(id), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(id);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToQueryService()
    {
        using var cts = new CancellationTokenSource();
        _mediaQueryService
            .GetByIdAsync(Arg.Any<MediaId>(), Arg.Any<CancellationToken>())
            .Returns((MediaDto?)null);

        _ = await _sut.Handle(new GetMediaByIdQuery(Guid.NewGuid()), cts.Token);

        await _mediaQueryService.Received(1).GetByIdAsync(Arg.Any<MediaId>(), cts.Token);
    }
}
