using Application.Search.Features.Shared;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using Infrastructure.Search.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Infrastructure.Search.HealthChecks;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class ElasticsearchDLQHealthCheckTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _fixture.ResetAsync();
    }

    private static FailedElasticOperation BuildOperation(string status, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        EntityType = "Product",
        EntityId = Guid.NewGuid().ToString(),
        Document = "{}",
        Error = "test error",
        Status = status,
        RetryCount = 0,
        CreatedAt = createdAt
    };

    private async Task SeedAsync(int pendingCount, int failedCount)
    {
        await using var context = _fixture.CreateContext();
        var now = DateTime.UtcNow;

        for (var i = 0; i < pendingCount; i++)
            context.FailedElasticOperations.Add(BuildOperation("Pending", now.AddMinutes(-i)));

        for (var i = 0; i < failedCount; i++)
            context.FailedElasticOperations.Add(BuildOperation("Failed", now.AddMinutes(-i)));

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenQueueIsEmpty_ReturnsHealthyWithZeroCounts()
    {
        await using var context = _fixture.CreateContext();
        var sut = new ElasticsearchDLQHealthCheck(context, Substitute.For<IAuditService>());

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["pending_count"].ShouldBe(0);
        result.Data["failed_count"].ShouldBe(0);
    }

    [Fact]
    public async Task CheckHealthAsync_WithFewPendingAndFailed_ReturnsHealthy()
    {
        await SeedAsync(pendingCount: 5, failedCount: 2);

        await using var context = _fixture.CreateContext();
        var sut = new ElasticsearchDLQHealthCheck(context, Substitute.For<IAuditService>());

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["pending_count"].ShouldBe(5);
        result.Data["failed_count"].ShouldBe(2);
        result.Data.ShouldContainKey("oldest_pending");
    }

    [Fact]
    public async Task CheckHealthAsync_With101Failed_ReturnsUnhealthy()
    {
        await SeedAsync(pendingCount: 0, failedCount: 101);

        await using var context = _fixture.CreateContext();
        var sut = new ElasticsearchDLQHealthCheck(context, Substitute.For<IAuditService>());

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldContain("Too many permanently failed operations");
        result.Data["failed_count"].ShouldBe(101);
    }

    [Fact]
    public async Task CheckHealthAsync_WithExactly100Failed_StaysHealthy()
    {
        await SeedAsync(pendingCount: 0, failedCount: 100);

        await using var context = _fixture.CreateContext();
        var sut = new ElasticsearchDLQHealthCheck(context, Substitute.For<IAuditService>());

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_With1001Pending_ReturnsDegraded()
    {
        await SeedAsync(pendingCount: 1001, failedCount: 0);

        await using var context = _fixture.CreateContext();
        var sut = new ElasticsearchDLQHealthCheck(context, Substitute.For<IAuditService>());

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldContain("High number of pending operations");
        result.Data["pending_count"].ShouldBe(1001);
    }

    [Fact]
    public async Task CheckHealthAsync_WithExactly1000Pending_StaysHealthy()
    {
        await SeedAsync(pendingCount: 1000, failedCount: 0);

        await using var context = _fixture.CreateContext();
        var sut = new ElasticsearchDLQHealthCheck(context, Substitute.For<IAuditService>());

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithPendingOperations_ReportsOldestPendingTimestamp()
    {
        var oldest = DateTime.UtcNow.AddHours(-3);
        await using (var seed = _fixture.CreateContext())
        {
            seed.FailedElasticOperations.Add(BuildOperation("Pending", oldest));
            seed.FailedElasticOperations.Add(BuildOperation("Pending", DateTime.UtcNow));
            await seed.SaveChangesAsync();
        }

        await using var context = _fixture.CreateContext();
        var sut = new ElasticsearchDLQHealthCheck(context, Substitute.For<IAuditService>());

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        ((DateTime)result.Data["oldest_pending"]).ShouldBe(oldest, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenContextIsBroken_ReturnsUnhealthyAndLogsError()
    {
        var auditService = Substitute.For<IAuditService>();
        var brokenOptions = new DbContextOptionsBuilder<DBContext>()
            .UseNpgsql("Host=localhost;Database=dlq_broken;Username=test;Password=test")
            .Options;
        await using var broken = new DBContext(
            brokenOptions,
            new AuditableEntityInterceptor(Substitute.For<IDateTimeProvider>()),
            new DomainEventInterceptor(Substitute.For<IOutboxEventTypeRegistry>()));
        await broken.DisposeAsync();

        var sut = new ElasticsearchDLQHealthCheck(broken, auditService);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        await auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s.Contains("DLQ health check failed")), Arg.Any<CancellationToken>());
    }
}
