using Application.Payment.Contracts;
using Application.Payment.Features.Queries.GetPaymentMethods;
using Application.Payment.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Payment.Features.Queries.GetPaymentMethods;

public class GetPaymentMethodsHandlerTests
{
    private readonly IPaymentMethodQueryService _queryService = Substitute.For<IPaymentMethodQueryService>(); private readonly GetPaymentMethodsHandler _sut;

    public GetPaymentMethodsHandlerTests()
    {
        _sut = new GetPaymentMethodsHandler(_queryService);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Handle_ForwardsFlagsToQueryService_ReturnsSuccess(bool includeInactive, bool includeDeleted)
    {
        IReadOnlyList<PaymentMethodListItemDto> items =
        [
            new() { Id = Guid.NewGuid(), Name = "Zarinpal", Code = "zarinpal", IsActive = true }
        ];

        _queryService
            .GetAllAsync(includeInactive, includeDeleted, Arg.Any<CancellationToken>())
            .Returns(items);

        var result = await _sut.Handle(
            new GetPaymentMethodsQuery(includeInactive, includeDeleted),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(items);
        await _queryService.Received(1).GetAllAsync(includeInactive, includeDeleted, Arg.Any<CancellationToken>());
    }
}
