using Application.Payment.Contracts;
using Application.Payment.Features.Queries.GetAdminPayments;
using Application.Payment.Features.Shared;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Payment.Features.Queries.GetAdminPayments;

public class GetAdminPaymentsHandlerTests
{
    private readonly IPaymentQueryService _paymentQueryService = Substitute.For<IPaymentQueryService>(); private readonly GetAdminPaymentsHandler _sut;

    public GetAdminPaymentsHandlerTests()
    {
        _sut = new GetAdminPaymentsHandler(_paymentQueryService);
    }

    [Fact]
    public async Task Handle_ForwardsAllFiltersToQueryService_ReturnsSuccess()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        var page = new PaginatedResult<PaymentTransactionDto>(
            [new PaymentTransactionDto { Id = Guid.NewGuid(), OrderId = orderId, UserId = userId }],
            totalCount: 1,
            page: 2,
            pageSize: 25);

        _paymentQueryService
            .GetPagedAsync(orderId, userId, "Success", "zarinpal", from, to, 2, 25, Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _sut.Handle(
            new GetAdminPaymentsQuery(orderId, userId, "Success", "zarinpal", from, to, 2, 25),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(page);
        await _paymentQueryService.Received(1).GetPagedAsync(
            orderId, userId, "Success", "zarinpal", from, to, 2, 25, Arg.Any<CancellationToken>());
    }
}
