using Application.Inventory.Contracts;
using Application.Inventory.Features.Queries.GetInventoryTransactions;
using Application.Inventory.Features.Shared;
using Domain.Variant.ValueObjects;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Inventory.Features.Queries.GetInventoryTransactions;

public class GetInventoryTransactionsHandlerTests
{
    private readonly IInventoryQueryService _queryService = Substitute.For<IInventoryQueryService>(); private readonly GetInventoryTransactionsHandler _sut;

    public GetInventoryTransactionsHandlerTests()
    {
        _sut = new GetInventoryTransactionsHandler(_queryService);
    }

    [Fact]
    public async Task Handle_WithVariantIdProvided_PassesTypedVariantIdAndReturnsSuccess()
    {
        var variantId = Guid.NewGuid();
        var page = new PaginatedResult<InventoryTransactionDto>(
            new List<InventoryTransactionDto>(), 0, 1, 20);

        _queryService
            .GetTransactionsPagedAsync(
                Arg.Is<VariantId?>(v => v != null && v.Value == variantId),
                Arg.Any<string?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _sut.Handle(
            new GetInventoryTransactionsQuery(variantId, null, null, null, 1, 20),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(page);
    }

    [Fact]
    public async Task Handle_WithoutVariantId_PassesNullVariantIdAndReturnsSuccess()
    {
        var page = new PaginatedResult<InventoryTransactionDto>(
            new List<InventoryTransactionDto>(), 0, 1, 20);

        _queryService
            .GetTransactionsPagedAsync(
                Arg.Is<VariantId?>(v => v == null),
                Arg.Any<string?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _sut.Handle(
            new GetInventoryTransactionsQuery(null, null, null, null, 1, 20),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(page);
    }
}
