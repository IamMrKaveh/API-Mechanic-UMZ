using Application.Cache.Contracts;
using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Payment.Aggregates;
using Domain.User.ValueObjects;
using Infrastructure.BackgroundJobs;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.BackgroundJobs;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class PaymentReconciliationJobTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IPaymentGatewayFactory _gatewayFactory = Substitute.For<IPaymentGatewayFactory>();
    private readonly IPaymentGateway _gateway = Substitute.For<IPaymentGateway>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    private static readonly DateTime FixedNow = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private PaymentReconciliationJob BuildJob()
    {
        var handle = Substitute.For<ILockHandle>();
        handle.IsAcquired.Returns(true);
        _distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(handle);
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _gatewayFactory.GetGateway(Arg.Any<string>()).Returns(_gateway);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(provider);
        provider.GetService(typeof(DBContext)).Returns(Context);
        provider.GetService(typeof(IDistributedLock)).Returns(_distributedLock);
        provider.GetService(typeof(IPaymentGatewayFactory)).Returns(_gatewayFactory);
        provider.GetService(typeof(IUnitOfWork)).Returns(
            new UnitOfWork(
                Context,
                Substitute.For<ILogger<UnitOfWork>>(),
                Substitute.For<IHostEnvironment>()));
        provider.GetService(typeof(IAuditService)).Returns(_auditService);

        return new PaymentReconciliationJob(scopeFactory, _distributedLock, _dateTimeProvider);
    }

    private async Task<PaymentTransaction> SeedOldPendingTransactionAsync(CancellationToken ct = default)
    {
        var user = await SeedUserAsync(ct: ct);
        var (brand, category) = await SeedBrandWithCategoryAsync(ct);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var product = new ProductBuilder()
            .WithName($"Recon Product {suffix}")
            .WithSlug($"recon-product-{suffix}")
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

        var order = new OrderBuilder()
            .WithUserId(user.Id)
            .WithItemSnapshots(new OrderItemSnapshotBuilder()
                .WithVariantId(variant.Id)
                .WithProductId(product.Id)
                .WithProductName(product.Name)
                .WithSku(variant.Sku)
                .WithQuantity(1)
                .WithUnitPrice(200_000m, "IRT")
                .Build())
            .Build();
        order.ClearDomainEvents();
        Context.Orders.Add(order);
        await Context.SaveChangesAsync(ct);

        var transaction = new PaymentTransactionBuilder()
            .WithOrderId(order.Id)
            .WithUserId(user.Id)
            .WithAuthority("A" + new string('7', 24))
            .WithAmount(200_000m)
            .Build();
        transaction.ClearDomainEvents();
        Context.PaymentTransactions.Add(transaction);
        await Context.SaveChangesAsync(ct);
        await Context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"PaymentTransactions\" SET \"CreatedAt\" = {FixedNow.AddHours(-13)} WHERE \"Id\" = {transaction.Id.Value}");
        Context.ChangeTracker.Clear();
        return transaction;
    }

    [Fact]
    public async Task ExecuteAsync_WhenGatewayConfirmsPayment_MarksSuccessAndWarns()
    {
        var transaction = await SeedOldPendingTransactionAsync();
        _gateway.VerifyAsync(Arg.Any<string>(), Arg.Any<Money>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentVerificationResult(Guid.NewGuid(), true, 555666L, null, 0m));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        _auditService
            .LogInformationAsync(Arg.Is<string>(s => s!.Contains("Complete")), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                cts.Cancel();
                return Task.CompletedTask;
            });
        var job = BuildJob();

        try { await job.StartAsync(cts.Token); } catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);
        Context.ChangeTracker.Clear();

        await _gateway.Received(1).VerifyAsync(
            transaction.Authority.Value,
            Arg.Is<Money>(m => m.Amount == 200_000m),
            Arg.Any<CancellationToken>());
        await using var verify = Fixture.CreateContext();
        var persisted = await verify.PaymentTransactions.FirstAsync(t => t.Id == transaction.Id);
        persisted.IsSuccessful().ShouldBeTrue();
        persisted.RefId.ShouldBe(555666L);
        await _auditService.Received(1).LogWarningAsync(
            Arg.Is<string>(s => s!.Contains("was PAID but showed Pending")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenGatewayThrows_MarksFailedWithoutWarning()
    {
        var transaction = await SeedOldPendingTransactionAsync();
        _gateway.VerifyAsync(Arg.Any<string>(), Arg.Any<Money>(), Arg.Any<CancellationToken>())
            .Throws(new ExternalServiceException("Zarinpal", "timeout"));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        _auditService
            .LogInformationAsync(Arg.Is<string>(s => s!.Contains("Complete")), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                cts.Cancel();
                return Task.CompletedTask;
            });
        var job = BuildJob();

        try { await job.StartAsync(cts.Token); } catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);
        Context.ChangeTracker.Clear();

        await using var verify = Fixture.CreateContext();
        var persisted = await verify.PaymentTransactions.FirstAsync(t => t.Id == transaction.Id);
        persisted.Status.Value.ShouldBe("Failed");
        await _auditService.DidNotReceiveWithAnyArgs().LogWarningAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoStaleTransactions_DoesNotCallGateway()
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

        await _gateway.DidNotReceiveWithAnyArgs().VerifyAsync(default!, default!, default);
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
            "jobs:payment-reconciliation",
            TimeSpan.FromHours(1),
            Arg.Any<CancellationToken>());
    }
}
