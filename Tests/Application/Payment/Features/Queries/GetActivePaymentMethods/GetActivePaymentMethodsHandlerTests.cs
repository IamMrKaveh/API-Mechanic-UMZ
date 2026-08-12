using Application.Payment.Contracts;
using Application.Payment.Features.Queries.GetActivePaymentMethods;
using Application.Payment.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Payment.Features.Queries.GetActivePaymentMethods;

public class GetActivePaymentMethodsHandlerTests
{
    private readonly IPaymentMethodQueryService _queryService = Substitute.For<IPaymentMethodQueryService>(); private readonly GetActivePaymentMethodsHandler _sut;

    public GetActivePaymentMethodsHandlerTests()
    {
        _sut = new GetActivePaymentMethodsHandler(_queryService);
    }

    [Fact]
    public async Task Handle_ReturnsItemsFromQueryService()
    {
        IReadOnlyList<AvailablePaymentMethodDto> items =
        [
            new() { Id = Guid.NewGuid(), Name = "Zarinpal", Code = "zarinpal", Fee = 0m, SortOrder = 1 },
        new() { Id = Guid.NewGuid(), Name = "Wallet",   Code = "wallet",   Fee = 0m, SortOrder = 2 }
        ];

        _queryService
            .GetActiveAsync(100m, Arg.Any<CancellationToken>())
            .Returns(items);

        var result = await _sut.Handle(new GetActivePaymentMethodsQuery(100m), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(items);
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsEmptyList_ReturnsSuccessWithEmpty()
    {
        _queryService
            .GetActiveAsync(Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AvailablePaymentMethodDto>());

        var result = await _sut.Handle(new GetActivePaymentMethodsQuery(0m), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeEmpty();
    }
}
