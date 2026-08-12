using Application.Cache.Contracts;
using Application.Common.Behaviors;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using SharedContracts.FeatureManagement;

namespace Tests.Application.Common.Behaviors;

public class IdempotencyBehaviorTests
{
    private readonly IIdempotencyService _idempotency = Substitute.For<IIdempotencyService>(); private readonly IDistributedLock _lock = Substitute.For<IDistributedLock>(); private readonly IFeatureManager _features = Substitute.For<IFeatureManager>();

    [Fact]
    public async Task Handle_WhenPreviouslyProcessed_ReturnsCachedResultAndSkipsNext()
    {
        var key = Guid.NewGuid();
        var request = new IdempotentTestCommand(key);
        var cached = new TestResponse("cached");

        _idempotency.GetResultAsync(key, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(JsonSerializer.Serialize(cached)));

        var sut = new IdempotencyBehavior<IdempotentTestCommand, TestResponse>(
            _idempotency, _lock, _features);

        var invoked = false;

        var result = await sut.Handle(
            request,
            _ =>
            {
                invoked = true;
                return Task.FromResult(new TestResponse("fresh"));
            },
            CancellationToken.None);

        invoked.ShouldBeFalse();
        result.Value.ShouldBe("cached");
        await _features.DidNotReceiveWithAnyArgs().IsEnabledAsync(default!);
    }

    [Fact]
    public async Task Handle_WhenNotCachedAndLockDisabled_CallsNextAndMarksProcessed()
    {
        var key = Guid.NewGuid();
        var request = new IdempotentTestCommand(key);

        _idempotency.GetResultAsync(key, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));

        _features.IsEnabledAsync(FeatureFlags.IdempotencyDistributedLockEnabled)
            .Returns(false);

        var sut = new IdempotencyBehavior<IdempotentTestCommand, TestResponse>(
            _idempotency, _lock, _features);

        var result = await sut.Handle(
            request,
            _ => Task.FromResult(new TestResponse("fresh")),
            CancellationToken.None);

        result.Value.ShouldBe("fresh");
        await _idempotency.Received(1).MarkAsProcessedAsync(
            key,
            Arg.Is<string>(s => s.Contains("fresh")),
            Arg.Any<CancellationToken>());
        await _lock.DidNotReceiveWithAnyArgs().AcquireAsync(default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenLockEnabledAndAcquired_CallsNextAndMarksProcessed()
    {
        var key = Guid.NewGuid();
        var request = new IdempotentTestCommand(key);

        _idempotency.GetResultAsync(key, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));

        _features.IsEnabledAsync(FeatureFlags.IdempotencyDistributedLockEnabled)
            .Returns(true);

        var handle = Substitute.For<ILockHandle>();
        handle.IsAcquired.Returns(true);

        _lock.AcquireAsync(
                Arg.Is<string>(k => k.Contains(key.ToString("N"))),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ILockHandle?>(handle));

        var sut = new IdempotencyBehavior<IdempotentTestCommand, TestResponse>(
            _idempotency, _lock, _features);

        var result = await sut.Handle(
            request,
            _ => Task.FromResult(new TestResponse("fresh-locked")),
            CancellationToken.None);

        result.Value.ShouldBe("fresh-locked");
        await _idempotency.Received(1).MarkAsProcessedAsync(
            key,
            Arg.Is<string>(s => s.Contains("fresh-locked")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLockEnabledAndAcquiredButRecheckReturnsCached_ReturnsCachedAndSkipsNext()
    {
        var key = Guid.NewGuid();
        var request = new IdempotentTestCommand(key);
        var cached = new TestResponse("recheck-cached");

        var getResultCalls = 0;
        _idempotency.GetResultAsync(key, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                getResultCalls++;
                return Task.FromResult<string?>(
                    getResultCalls == 1 ? null : JsonSerializer.Serialize(cached));
            });

        _features.IsEnabledAsync(FeatureFlags.IdempotencyDistributedLockEnabled)
            .Returns(true);

        var handle = Substitute.For<ILockHandle>();
        handle.IsAcquired.Returns(true);

        _lock.AcquireAsync(
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ILockHandle?>(handle));

        var sut = new IdempotencyBehavior<IdempotentTestCommand, TestResponse>(
            _idempotency, _lock, _features);

        var invoked = false;

        var result = await sut.Handle(
            request,
            _ =>
            {
                invoked = true;
                return Task.FromResult(new TestResponse("fresh"));
            },
            CancellationToken.None);

        invoked.ShouldBeFalse();
        result.Value.ShouldBe("recheck-cached");
        await _idempotency.DidNotReceiveWithAnyArgs().MarkAsProcessedAsync(default, default!, default);
    }

    [Fact]
    public async Task Handle_WhenLockEnabledAndNotAcquiredAndNoContentionResult_ThrowsConcurrencyException()
    {
        var key = Guid.NewGuid();
        var request = new IdempotentTestCommand(key);

        _idempotency.GetResultAsync(key, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));

        _features.IsEnabledAsync(FeatureFlags.IdempotencyDistributedLockEnabled)
            .Returns(true);

        var handle = Substitute.For<ILockHandle>();
        handle.IsAcquired.Returns(false);

        _lock.AcquireAsync(
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ILockHandle?>(handle));

        var sut = new IdempotencyBehavior<IdempotentTestCommand, TestResponse>(
            _idempotency, _lock, _features);

        await Should.ThrowAsync<ConcurrencyException>(async () =>
            await sut.Handle(
                request,
                _ => Task.FromResult(new TestResponse("fresh")),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenLockEnabledAndNotAcquiredButContentionYieldsResult_ReturnsContentionResult()
    {
        var key = Guid.NewGuid();
        var request = new IdempotentTestCommand(key);
        var cached = new TestResponse("contention-cached");

        var getResultCalls = 0;
        _idempotency.GetResultAsync(key, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                getResultCalls++;
                return Task.FromResult<string?>(
                    getResultCalls >= 2 ? JsonSerializer.Serialize(cached) : null);
            });

        _features.IsEnabledAsync(FeatureFlags.IdempotencyDistributedLockEnabled)
            .Returns(true);

        var handle = Substitute.For<ILockHandle>();
        handle.IsAcquired.Returns(false);

        _lock.AcquireAsync(
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ILockHandle?>(handle));

        var sut = new IdempotencyBehavior<IdempotentTestCommand, TestResponse>(
            _idempotency, _lock, _features);

        var result = await sut.Handle(
            request,
            _ => Task.FromResult(new TestResponse("fresh")),
            CancellationToken.None);

        result.Value.ShouldBe("contention-cached");
    }

    public sealed record IdempotentTestCommand(Guid IdempotencyKey)
        : IRequest<TestResponse>, IIdempotentCommand;

    public sealed class TestResponse
    {
        public TestResponse()
        { }

        public TestResponse(string value) => Value = value;

        public string Value { get; set; } = string.Empty;
    }
}
