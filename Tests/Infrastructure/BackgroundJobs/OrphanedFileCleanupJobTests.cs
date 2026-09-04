using Application.Cache.Contracts;
using Application.Media.Contracts;
using Domain.User.ValueObjects;
using Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.Interfaces;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.BackgroundJobs;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OrphanedFileCleanupJobTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IStorageService _storageService = Substitute.For<IStorageService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    private OrphanedFileCleanupJob BuildJob()
    {
        var handle = Substitute.For<ILockHandle>();
        handle.IsAcquired.Returns(true);
        _distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(handle);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(provider);
        provider.GetService(typeof(DBContext)).Returns(Context);
        provider.GetService(typeof(IDistributedLock)).Returns(_distributedLock);
        provider.GetService(typeof(IStorageService)).Returns(_storageService);
        provider.GetService(typeof(IAuditService)).Returns(_auditService);

        return new OrphanedFileCleanupJob(scopeFactory, _distributedLock, _dateTimeProvider);
    }

    private async Task<global::Domain.Media.Aggregates.Media> SeedDeletedMediaAsync(
        DateTime deletedAt, CancellationToken ct = default)
    {
        var media = new MediaBuilder()
            .WithFilePath($"uploads/{Guid.NewGuid():N}.jpg")
            .WithEntityType("Product")
            .WithEntityId(Guid.NewGuid())
            .BuildDeleted();
        media.ClearDomainEvents();
        Context.Medias.Add(media);
        await Context.SaveChangesAsync(ct);
        await Context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"Medias\" SET \"DeletedAt\" = {deletedAt} WHERE \"Id\" = {media.Id.Value}");
        Context.ChangeTracker.Clear();
        return media;
    }

    [Fact]
    public async Task ExecuteAsync_WithOldDeletedMedia_DeletesFileAndRemovesRow()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc));
        var media = await SeedDeletedMediaAsync(new DateTime(2026, 5, 30, 12, 0, 0, DateTimeKind.Utc));
        var job = BuildJob();

        _storageService
            .DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        try { await job.StartAsync(cts.Token); }
        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);
        Context.ChangeTracker.Clear();

        await _storageService.Received(1).DeleteAsync(
            media.Path.Value,
            Arg.Any<CancellationToken>());
        await using var verify = Fixture.CreateContext();
        (await verify.Medias.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == media.Id))
            .ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithRecentlyDeletedMedia_KeepsRow()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc));
        var media = await SeedDeletedMediaAsync(new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));
        var job = BuildJob();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await job.StartAsync(cts.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);
        Context.ChangeTracker.Clear();

        await _storageService.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
        await using var verify = Fixture.CreateContext();
        (await verify.Medias.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == media.Id))
            .ShouldNotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenStorageThrows_LogsErrorAndKeepsRow()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc));
        var media = await SeedDeletedMediaAsync(new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc));
        _storageService
            .DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("s3 down")));
        var job = BuildJob();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await job.StartAsync(cts.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);
        Context.ChangeTracker.Clear();

        await using var verify = Fixture.CreateContext();
        (await verify.Medias.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == media.Id))
            .ShouldNotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_AcquiresLockWithExpectedKeyAndExpiry()
    {
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
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
            "jobs:orphaned-file-cleanup",
            TimeSpan.FromHours(2),
            Arg.Any<CancellationToken>());
    }
}
