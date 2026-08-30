using Application.Inventory.Features.Shared;
using Domain.Inventory.Entities;
using Domain.Variant.ValueObjects;
using Infrastructure.Inventory.QueryServices;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Inventory.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class StockLedgerQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private StockLedgerQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new StockLedgerQueryService(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<VariantId> SeedVariantAsync()
    {
        var product = new ProductBuilder().Build();
        _context.Products.Add(product);

        var variant = new ProductVariantBuilder()
            .WithProductId(product.Id)
            .Build();
        _context.ProductVariants.Add(variant);

        await _context.SaveChangesAsync();
        return variant.Id;
    }

    private async Task<List<StockLedgerEntry>> SeedLedgerEntriesAsync(VariantId variantId, int count)
    {
        var entries = new List<StockLedgerEntry>();
        var balance = 0;
        for (var i = 0; i < count; i++)
        {
            balance += 10;
            var entry = new StockLedgerEntryBuilder()
                .WithVariantId(variantId)
                .WithQuantity(10)
                .WithBalanceAfter(balance)
                .WithReferenceNumber($"REF-{i:D3}")
                .BuildStockIn();
            entries.Add(entry);
            _context.StockLedgerEntries.Add(entry);
            await _context.SaveChangesAsync();
        }
        return entries;
    }

    [Fact]
    public async Task GetByVariantIdAsync_WithNoEntries_ReturnsEmptyPaginatedResult()
    {
        var variantId = await SeedVariantAsync();

        var result = await _sut.GetByVariantIdAsync(variantId, page: 1, pageSize: 10);

        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(10);
    }

    [Fact]
    public async Task GetByVariantIdAsync_WithEntriesForVariant_ReturnsMatchingEntriesOnly()
    {
        var variantIdA = await SeedVariantAsync();
        var variantIdB = await SeedVariantAsync();
        await SeedLedgerEntriesAsync(variantIdA, 3);
        await SeedLedgerEntriesAsync(variantIdB, 5);

        var result = await _sut.GetByVariantIdAsync(variantIdA, page: 1, pageSize: 10);

        result.TotalCount.ShouldBe(3);
        result.Items.Count.ShouldBe(3);
        result.Items.ShouldAllBe(dto => dto.VariantId == variantIdA.Value);
    }

    [Fact]
    public async Task GetByVariantIdAsync_WithManyEntries_OrdersByCreatedAtDescending()
    {
        var variantId = await SeedVariantAsync();
        var seeded = await SeedLedgerEntriesAsync(variantId, 5);

        var result = await _sut.GetByVariantIdAsync(variantId, page: 1, pageSize: 10);

        result.Items.Count.ShouldBe(5);
        var expectedOrder = seeded
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => e.Id.Value)
            .ToList();
        result.Items.Select(dto => dto.Id).ToList().ShouldBe(expectedOrder);
    }

    [Theory]
    [InlineData(1, 2, 2, 5)]
    [InlineData(2, 2, 2, 5)]
    [InlineData(3, 2, 1, 5)]
    [InlineData(1, 10, 5, 5)]
    [InlineData(2, 10, 0, 5)]
    public async Task GetByVariantIdAsync_WithPagination_ReturnsCorrectSliceAndTotals(
        int page, int pageSize, int expectedItemCount, int totalEntries)
    {
        var variantId = await SeedVariantAsync();
        await SeedLedgerEntriesAsync(variantId, totalEntries);

        var result = await _sut.GetByVariantIdAsync(variantId, page, pageSize);

        result.Items.Count.ShouldBe(expectedItemCount);
        result.TotalCount.ShouldBe(totalEntries);
        result.Page.ShouldBe(page);
        result.PageSize.ShouldBe(pageSize);
    }

    [Fact]
    public async Task GetByVariantIdAsync_MapsAllPropertiesFromEntityToDto()
    {
        var variantId = await SeedVariantAsync();
        var entry = new StockLedgerEntryBuilder()
            .WithVariantId(variantId)
            .WithQuantity(25)
            .WithBalanceAfter(75)
            .WithReferenceNumber("REF-XYZ")
            .WithNote("stock replenishment")
            .BuildStockIn();
        _context.StockLedgerEntries.Add(entry);
        await _context.SaveChangesAsync();

        var result = await _sut.GetByVariantIdAsync(variantId, page: 1, pageSize: 10);

        result.Items.Count.ShouldBe(1);
        var dto = result.Items[0];
        dto.Id.ShouldBe(entry.Id.Value);
        dto.VariantId.ShouldBe(variantId.Value);
        dto.QuantityDelta.ShouldBe(entry.QuantityDelta);
        dto.BalanceAfter.ShouldBe(entry.BalanceAfter);
        dto.Note.ShouldBe(entry.Note);
        dto.ReferenceNumber.ShouldBe(entry.ReferenceNumber);
        dto.CreatedAt.ShouldBe(entry.CreatedAt);
    }

    [Fact]
    public async Task GetByVariantIdAsync_WithUnknownVariantId_ReturnsEmptyResult()
    {
        var unknownVariantId = VariantId.NewId();

        var result = await _sut.GetByVariantIdAsync(unknownVariantId, page: 1, pageSize: 10);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetByVariantIdAsync_HonorsCancellationToken()
    {
        var variantId = await SeedVariantAsync();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await _sut.GetByVariantIdAsync(variantId, page: 1, pageSize: 10, cts.Token));
    }
}
