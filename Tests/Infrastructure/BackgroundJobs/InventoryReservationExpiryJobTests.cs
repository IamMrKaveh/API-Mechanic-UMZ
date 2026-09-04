using Application.Cache.Contracts;
using Application.Inventory.Contracts;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;
using Infrastructure.BackgroundJobs;
using Infrastructure.BackgroundJobs.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharedKernel.Abstractions.Interfaces;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.BackgroundJobs;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class InventoryReservationExpiryJobTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IInventoryService _inventoryService = Substitute.For<IInventoryService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    private static readonly DateTime FixedNow = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private InventoryReservationExpiryJob BuildJob(int expiryMinutes = 30)
    {
        var handle = Substitute.For<ILockHandle>();
        handle.IsAcquired.Returns(true);
        _distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(handle);
        _dateTimeProvider.UtcNow.Returns(FixedNow);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(provider);
        provider.GetService(typeof(DBContext)).Returns(Context);
        provider.GetService(typeof(IDistributedLock)).Returns(_distributedLock);
        provider.GetService(typeof(IInventoryService)).Returns(_inventoryService);
        provider.GetService(typeof(IAuditService)).Returns(_auditService);

        return new InventoryReservationExpiryJob(
            scopeFactory,
            _distributedLock,
            Microsoft.Extensions.Options.Options.Create(new ReservationExpiryOptions { ExpiryMinutes = expiryMinutes }),
            _dateTimeProvider);
    }

    private async Task<VariantId> SeedExpiredReservationAsync(
        string referenceNumber, DateTime createdAt, CancellationToken ct = default)
    {
        var (brand, category) = await SeedBrandWithCategoryAsync(ct);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var product = new ProductBuilder()
            .WithName($"Expiry Product {suffix}")
            .WithSlug($"expiry-product-{suffix}")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        product.ClearDomainEvents();
        Context.Products.Add(product);
        await Context.SaveChangesAsync(ct);

        var variant = new ProductVariantBuilder()
            .WithProductId(product.Id)
            .WithSku($"SKU-{Guid.NewGuid():N}"[..20])
            .Build();
        variant.ClearDomainEvents();
        Context.ProductVariants.Add(variant);
        await Context.SaveChangesAsync(ct);

        var entry = new StockLedgerEntryBuilder()
            .WithVariantId(variant.Id)
            .WithQuantity(3)
            .WithBalanceAfter(7)
            .WithReferenceNumber(referenceNumber)
            .BuildReserve();
        Context.StockLedgerEntries.Add(entry);
        await Context.SaveChangesAsync(ct);
        await Context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"StockLedgerEntries\" SET \"CreatedAt\" = {createdAt} WHERE \"Id\" = {entry.Id.Value}");
        Context.ChangeTracker.Clear();
        return variant.Id;
    }

    [Fact]
    public async Task ExecuteAsync_LogsStartMessage()
    {
        var job = BuildJob();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await job.StartAsync(cts.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await _auditService.Received(1).LogInformationAsync(
            "Inventory Reservation Expiry Service started.",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenGroupQueryIsUntranslatable_LogsErrorWithoutRollingBack()
    {
        await SeedExpiredReservationAsync("ORDER-EXP-1", FixedNow.AddHours(-2));
        var job = BuildJob(expiryMinutes: 30);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await job.StartAsync(cts.Token); } catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await _inventoryService.DidNotReceiveWithAnyArgs().RollbackReservationsAsync(default!, default);
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("Error processing expired inventory reservations")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithOnlyFreshReservations_DoesNotRelease()
    {
        await SeedExpiredReservationAsync("ORDER-FRESH", FixedNow.AddMinutes(-5));
        var job = BuildJob(expiryMinutes: 30);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await job.StartAsync(cts.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await _inventoryService.DidNotReceiveWithAnyArgs().RollbackReservationsAsync(default!, default);
        await _inventoryService.DidNotReceiveWithAnyArgs().ReleaseReservationAsync(default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_AcquiresLockWithExpectedKeyAndExpiry()
    {
        var job = BuildJob();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await job.StartAsync(cts.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await _distributedLock.Received().AcquireAsync(
            "jobs:inventory-reservation-expiry",
            TimeSpan.FromMinutes(10),
            Arg.Any<CancellationToken>());
    }
}
