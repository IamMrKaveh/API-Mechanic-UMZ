using Application.Media.Contracts;
using Application.Media.Features.Queries.GetAllMedia;
using Application.Media.Features.Shared;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Media.Features.Queries.GetAllMedia;

public class GetAllMediaHandlerTests
{
    private readonly IMediaQueryService _mediaQueryService = Substitute.For<IMediaQueryService>(); private readonly GetAllMediaHandler _sut;

    public GetAllMediaHandlerTests()
    {
        _sut = new GetAllMediaHandler(_mediaQueryService);
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsPaginatedResult_ReturnsSuccessWithSameResult()
    {
        var expected = PaginatedResult<MediaDto>.Create(
            Array.Empty<MediaDto>(),
            totalCount: 0,
            page: 1,
            pageSize: 10);

        _mediaQueryService
            .GetAllAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetAllMediaQuery(null), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task Handle_PassesEntityTypePageAndPageSizeVerbatimToQueryService()
    {
        _mediaQueryService
            .GetAllAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<MediaDto>.Create(Array.Empty<MediaDto>(), 0, 2, 25));

        _ = await _sut.Handle(new GetAllMediaQuery("Product", 2, 25), CancellationToken.None);

        await _mediaQueryService.Received(1).GetAllAsync("Product", 2, 25, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEntityTypeIsNull_ForwardsNullToQueryService()
    {
        _mediaQueryService
            .GetAllAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<MediaDto>.Create(Array.Empty<MediaDto>(), 0, 1, 10));

        _ = await _sut.Handle(new GetAllMediaQuery(null, 1, 10), CancellationToken.None);

        await _mediaQueryService.Received(1).GetAllAsync(null, 1, 10, Arg.Any<CancellationToken>());
    }
}
