using Application.Location.Contracts;
using Application.Location.Features.Queries.GetCities;
using Application.Location.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Location.Features.Queries.GetCities;

public class GetCitiesHandlerTests
{
    private readonly ILocationService _locationService = Substitute.For<ILocationService>(); private readonly GetCitiesHandler _sut;

    public GetCitiesHandlerTests()
    {
        _sut = new GetCitiesHandler(_locationService);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsCities_ReturnsSuccessWithSameCities()
    {
        var expected = new List<CityDto>
    {
        new(1, "Tehran", "Tehran", 1),
        new(2, "Karaj", "Alborz", 2)
    };

        _locationService
            .GetCitiesByProvinceAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetCitiesQuery(1), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsEmptyList_ReturnsSuccessWithEmptyCollection()
    {
        _locationService
            .GetCitiesByProvinceAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CityDto>());

        var result = await _sut.Handle(new GetCitiesQuery(42), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(1, "1")]
    [InlineData(0, "0")]
    [InlineData(42, "42")]
    [InlineData(-7, "-7")]
    [InlineData(int.MaxValue, "2147483647")]
    [InlineData(int.MinValue, "-2147483648")]
    public async Task Handle_ConvertsStateIdUsingInvariantCulture_ForwardsStringToService(int stateId, string expectedForwarded)
    {
        string? capturedProvince = null;

        _locationService
            .GetCitiesByProvinceAsync(
                Arg.Do<string>(p => capturedProvince = p),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CityDto>());

        var result = await _sut.Handle(new GetCitiesQuery(stateId), CancellationToken.None);

        result.ShouldBeSuccess();
        capturedProvince.ShouldBe(expectedForwarded);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToService()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken capturedToken = default;

        _locationService
            .GetCitiesByProvinceAsync(
                Arg.Any<string>(),
                Arg.Do<CancellationToken>(t => capturedToken = t))
            .Returns(Array.Empty<CityDto>());

        var result = await _sut.Handle(new GetCitiesQuery(5), cts.Token);

        result.ShouldBeSuccess();
        capturedToken.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task Handle_CallsServiceExactlyOnce()
    {
        _locationService
            .GetCitiesByProvinceAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CityDto>());

        var result = await _sut.Handle(new GetCitiesQuery(9), CancellationToken.None);

        result.ShouldBeSuccess();
        await _locationService.Received(1)
            .GetCitiesByProvinceAsync("9", Arg.Any<CancellationToken>());
        await _locationService.DidNotReceiveWithAnyArgs()
            .GetProvincesAsync(default);
    }
}
