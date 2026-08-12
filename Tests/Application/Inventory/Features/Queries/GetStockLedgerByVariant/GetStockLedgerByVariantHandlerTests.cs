using Application.Inventory.Contracts;
using Application.Inventory.Features.Queries.GetStockLedgerByVariant;
using Application.Inventory.Features.Shared;
using Domain.Variant.ValueObjects;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Inventory.Features.Queries.GetStockLedgerByVariant;

public class GetStockLedgerByVariantHandlerTests
{
    private readonly IStockLedgerQueryService _ledgerQueryService = Substitute.For<IStockLedgerQueryService>(); private readonly GetStockLedgerByVariantHandler _sut;

    public GetStockLedgerByVariantHandlerTests()
    {
        _sut = new GetStockLedgerByVariantHandler(_ledgerQueryService);
    }

    [Fact]
    public async Task Handle_ReturnsSuccessWithLedgerPage()
    {
        var variantId = Guid.NewGuid();
        var page = new PaginatedResult<StockLedgerEntryDto>(
            new List<StockLedgerEntryDto>
            {
            new() { Id = Guid.NewGuid(), VariantId = variantId, QuantityDelta = 5, BalanceAfter = 5 }
            },
            totalCount: 1,
            page: 1,
            pageSize: 20);

        _ledgerQueryService
            .GetByVariantIdAsync(
                Arg.Is<VariantId>(v => v.Value == variantId),
                1,
                20,
                Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _sut.Handle(
            new GetStockLedgerByVariantQuery(variantId, 1, 20),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(page);
    }
}
