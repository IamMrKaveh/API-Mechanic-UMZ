using Domain.Payment.ValueObjects;
using Infrastructure.Payment.Seeders;
using Infrastructure.Persistence.Context;
using Microsoft.Extensions.DependencyInjection;
using Tests.TestInfrastructure.Database;

namespace Tests.Infrastructure.Payment.Seeders;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class PaymentMethodSeederTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private (PaymentMethodSeeder seeder, ILogger<PaymentMethodSeeder> logger) BuildSeeder()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(provider);
        provider.GetService(typeof(DBContext)).Returns(_context);

        var logger = Substitute.For<ILogger<PaymentMethodSeeder>>();
        return (new PaymentMethodSeeder(scopeFactory, logger), logger);
    }

    [SkippableFact]
    public async Task StartAsync_EmptyDatabase_SeedsAllFourDefaultPaymentMethods()
    {
        var (seeder, _) = BuildSeeder();

        await seeder.StartAsync(CancellationToken.None);

        await using var freshContext = _fixture.CreateContext();
        var codes = await freshContext.PaymentMethods
            .IgnoreQueryFilters()
            .Select(p => p.Code.Value)
            .ToListAsync();

        codes.Count.ShouldBe(4);
        codes.ShouldContain(PaymentMethodCode.ZarinpalSandbox);
        codes.ShouldContain(PaymentMethodCode.Zarinpal);
        codes.ShouldContain(PaymentMethodCode.CashOnDelivery);
        codes.ShouldContain(PaymentMethodCode.Wallet);
    }

    [SkippableFact]
    public async Task StartAsync_CalledTwice_DoesNotCreateDuplicateMethods()
    {
        var (seederFirst, _) = BuildSeeder();
        await seederFirst.StartAsync(CancellationToken.None);

        await using (var refreshedContext = _fixture.CreateContext())
        {
            _context = refreshedContext;
            var (seederSecond, _) = BuildSeeder();
            await seederSecond.StartAsync(CancellationToken.None);
        }

        await using var verifyContext = _fixture.CreateContext();
        var count = await verifyContext.PaymentMethods
            .IgnoreQueryFilters()
            .CountAsync();

        count.ShouldBe(4);
    }

    [SkippableFact]
    public async Task StopAsync_CompletesWithoutError()
    {
        var (seeder, _) = BuildSeeder();

        await Should.NotThrowAsync(() => seeder.StopAsync(CancellationToken.None));
    }
}
