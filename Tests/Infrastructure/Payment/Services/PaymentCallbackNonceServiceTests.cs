using Infrastructure.Payment.Services;

namespace Tests.Infrastructure.Payment.Services;

public class PaymentCallbackNonceServiceTests
{
    private sealed class TestContext
    { public IServiceProvider ServiceProvider { get; init; } = null!; public ICacheService CacheService { get; init; } = null!; public IAuditService AuditService { get; init; } = null!; public PaymentCallbackNonceService Sut { get; init; } = null!; }

    private static TestContext BuildContext()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var cacheService = Substitute.For<ICacheService>();
        var auditService = Substitute.For<IAuditService>();
        var sut = new PaymentCallbackNonceService(serviceProvider, cacheService, auditService);
        return new TestContext
        {
            ServiceProvider = serviceProvider,
            CacheService = cacheService,
            AuditService = auditService,
            Sut = sut
        };
    }

    [Fact]
    public async Task IssueAsync_NoExistingCachedNonce_GeneratesAndCachesNewNonce()
    {
        var ctx = BuildContext();
        var transactionId = Guid.NewGuid();
        ctx.CacheService.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));

        var nonce = await ctx.Sut.IssueAsync(transactionId, TimeSpan.FromMinutes(5));

        nonce.ShouldNotBeNullOrWhiteSpace();
        await ctx.CacheService.Received(1).SetAsync<string>(
            Arg.Is<string>(k => k!.Contains(transactionId.ToString("N"))),
            nonce,
            TimeSpan.FromMinutes(5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IssueAsync_ExistingCachedNonce_ReturnsExistingWithoutOverwriting()
    {
        var ctx = BuildContext();
        var transactionId = Guid.NewGuid();
        ctx.CacheService.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("existing-nonce-value"));

        var nonce = await ctx.Sut.IssueAsync(transactionId, TimeSpan.FromMinutes(5));

        nonce.ShouldBe("existing-nonce-value");
        await ctx.CacheService.DidNotReceiveWithAnyArgs().SetAsync<string>(
            default!, default!, default, default);
    }

    [Fact]
    public async Task IssueAsync_NegativeTtl_FallsBackToThirtyMinuteDefault()
    {
        var ctx = BuildContext();
        ctx.CacheService.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));

        await ctx.Sut.IssueAsync(Guid.NewGuid(), TimeSpan.FromMinutes(-1));

        await ctx.CacheService.Received(1).SetAsync<string>(
            Arg.Any<string>(),
            Arg.Any<string>(),
            TimeSpan.FromMinutes(30),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IssueAsync_ZeroTtl_FallsBackToThirtyMinuteDefault()
    {
        var ctx = BuildContext();
        ctx.CacheService.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));

        await ctx.Sut.IssueAsync(Guid.NewGuid(), TimeSpan.Zero);

        await ctx.CacheService.Received(1).SetAsync<string>(
            Arg.Any<string>(),
            Arg.Any<string>(),
            TimeSpan.FromMinutes(30),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAndConsumeAsync_EmptyNonce_ReturnsFalseWithoutTouchingCache(string? nonce)
    {
        var ctx = BuildContext();

        var result = await ctx.Sut.ValidateAndConsumeAsync(Guid.NewGuid(), nonce!);

        result.ShouldBeFalse();
        await ctx.CacheService.DidNotReceiveWithAnyArgs().GetAsync<string>(default!, default);
        await ctx.CacheService.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_NoCachedNonce_LogsWarningAndReturnsFalse()
    {
        var ctx = BuildContext();
        ctx.CacheService.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));

        var result = await ctx.Sut.ValidateAndConsumeAsync(Guid.NewGuid(), "any-nonce");

        result.ShouldBeFalse();
        await ctx.AuditService.Received(1).LogWarningAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await ctx.CacheService.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_MatchingNonce_RemovesFromCacheAndReturnsTrue()
    {
        var ctx = BuildContext();
        var transactionId = Guid.NewGuid();
        ctx.CacheService.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("nonce-abc"));

        var result = await ctx.Sut.ValidateAndConsumeAsync(transactionId, "nonce-abc");

        result.ShouldBeTrue();
        await ctx.CacheService.Received(1).RemoveAsync(
            Arg.Is<string>(k => k!.Contains(transactionId.ToString("N"))),
            Arg.Any<CancellationToken>());
        await ctx.AuditService.DidNotReceiveWithAnyArgs().LogSecurityEventAsync(
            default!, default!, default!, default, default);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_MismatchNonce_RemovesFromCacheLogsSecurityEventAndReturnsFalse()
    {
        var ctx = BuildContext();
        ctx.CacheService.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("nonce-abc"));

        var result = await ctx.Sut.ValidateAndConsumeAsync(Guid.NewGuid(), "nonce-xyz");

        result.ShouldBeFalse();
        await ctx.CacheService.Received(1).RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await ctx.AuditService.Received(1).LogSecurityEventAsync(
            "PaymentCallbackNonceMismatch",
            Arg.Any<string>(),
            IpAddress.Unknown,
            null,
            Arg.Any<CancellationToken>());
    }
}
