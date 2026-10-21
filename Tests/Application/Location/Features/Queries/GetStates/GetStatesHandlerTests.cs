using Application.Location.Contracts;
using Application.Location.Features.Queries.GetStates;
using Application.Location.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Location.Features.Queries.GetStates;

public class GetStatesHandlerTests
{
    private readonly ILocationService _locationService = Substitute.For<ILocationService>(); private readonly GetStatesHandler _sut;

    public GetStatesHandlerTests()
    {
        _sut = new GetStatesHandler(_locationService);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsProvinces_ReturnsSuccessWithAllProvincesInItems()
    {
        var provinces = new List<ProvinceDto>
    {
        new(1, "Tehran", "THR"),
        new(2, "Alborz", "ALB"),
        new(3, "Isfahan", "ISF")
    };

        _locationService
            .GetProvincesAsync(Arg.Any<CancellationToken>())
            .Returns(provinces);

        var result = await _sut.Handle(new GetStatesQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldNotBeNull();
        result.Value.Items.ShouldBe(provinces);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsProvinces_ReportsTheTotalCountAndTheRequestedPageSize()
    {
        var provinces = new List<ProvinceDto>
        {
            new(1, "A", "A"),
            new(2, "B", "B"),
            new(3, "C", "C"),
            new(4, "D", "D")
        };

        _locationService
            .GetProvincesAsync(Arg.Any<CancellationToken>())
            .Returns(provinces);

        var result = await _sut.Handle(new GetStatesQuery(PageSize: 2), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.TotalCount.ShouldBe(4);
        result.Value.Page.ShouldBe(1);
        result.Value.PageSize.ShouldBe(2);
        result.Value.TotalPages.ShouldBe(2);
        result.Value.HasNextPage.ShouldBeTrue();
        result.Value.HasPreviousPage.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsSingleProvince_ReturnsSuccessWithSinglePage()
    {
        var provinces = new List<ProvinceDto> { new(1, "Tehran", "THR") };

        _locationService
            .GetProvincesAsync(Arg.Any<CancellationToken>())
            .Returns(provinces);

        var result = await _sut.Handle(new GetStatesQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items[0].ShouldBe(provinces[0]);
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Page.ShouldBe(1);
        result.Value.PageSize.ShouldBe(50);
    }

    [Theory]
    [InlineData(1, 50, 2)]
    [InlineData(2, 1, 1)]
    [InlineData(5, 100, 0)]
    public async Task Handle_PaginatesTheProvinces_AccordingToTheRequestedPageAndPageSize(
        int requestedPage,
        int requestedPageSize,
        int expectedItemCount)
    {
        var provinces = new List<ProvinceDto>
        {
            new(1, "A", "A"),
            new(2, "B", "B")
        };

        _locationService
            .GetProvincesAsync(Arg.Any<CancellationToken>())
            .Returns(provinces);

        var result = await _sut.Handle(new GetStatesQuery(requestedPage, requestedPageSize), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Page.ShouldBe(requestedPage);
        result.Value.PageSize.ShouldBe(requestedPageSize);
        result.Value.TotalCount.ShouldBe(provinces.Count);
        result.Value.Items.Count.ShouldBe(expectedItemCount);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToService()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken capturedToken = default;

        _locationService
            .GetProvincesAsync(Arg.Do<CancellationToken>(t => capturedToken = t))
            .Returns(Array.Empty<ProvinceDto>());

        var result = await _sut.Handle(new GetStatesQuery(), cts.Token);

        result.ShouldBeSuccess();
        capturedToken.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task Handle_CallsServiceExactlyOnce()
    {
        _locationService
            .GetProvincesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ProvinceDto> { new(1, "Tehran", "THR") });

        var result = await _sut.Handle(new GetStatesQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        await _locationService.Received(1).GetProvincesAsync(Arg.Any<CancellationToken>());
        await _locationService.DidNotReceiveWithAnyArgs()
            .GetCitiesByProvinceAsync(default!, default);
    }
}
