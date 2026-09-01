using Application.Order.Sagas.State;
using Domain.Order.Enums;
using Domain.Order.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Tests.Infrastructure.Order.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OrderProcessStateConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    [Fact]
    public async Task SaveChanges_ThenReload_PreservesAllScalarProperties()
    {
        var orderId = OrderId.NewId();
        var state = OrderProcessState.Create(orderId, correlationId: "corr-42");

        await _context.OrderProcessStates.AddAsync(state);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderProcessStates.FirstAsync(s => s.Id == state.Id);

        loaded.Id.ShouldBe(state.Id);
        loaded.OrderId.Value.ShouldBe(orderId.Value);
        loaded.CorrelationId.ShouldBe("corr-42");
        loaded.CurrentStep.ShouldBe(ProcessStepEnum.Created);
        loaded.Status.ShouldBe(ProcessStatusEnum.InProgress);
        loaded.RetryCount.ShouldBe(0);
        loaded.FailureReason.ShouldBeNull();
        loaded.CreatedAt.ShouldNotBe(default);
        loaded.UpdatedAt.ShouldNotBe(default);
    }

    [Fact]
    public async Task SaveChanges_ThenReload_RoundTripsOrderIdConversion()
    {
        var orderId = OrderId.NewId();
        var state = OrderProcessState.Create(orderId);

        await _context.OrderProcessStates.AddAsync(state);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderProcessStates
            .FirstAsync(s => s.OrderId == orderId);

        loaded.OrderId.ShouldBe(orderId);
    }

    [Theory]
    [InlineData(ProcessStepEnum.Created)]
    [InlineData(ProcessStepEnum.InventoryReserved)]
    [InlineData(ProcessStepEnum.PaymentPending)]
    [InlineData(ProcessStepEnum.PaymentSucceeded)]
    [InlineData(ProcessStepEnum.Completed)]
    [InlineData(ProcessStepEnum.Failed)]
    [InlineData(ProcessStepEnum.Compensating)]
    [InlineData(ProcessStepEnum.Compensated)]
    [InlineData(ProcessStepEnum.Refunded)]
    [InlineData(ProcessStepEnum.RequiresManualReconciliation)]
    public async Task SaveChanges_AfterTransitionTo_PersistsCurrentStepAsString(ProcessStepEnum step)
    {
        var state = OrderProcessState.Create(OrderId.NewId());
        state.TransitionTo(step);

        await _context.OrderProcessStates.AddAsync(state);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderProcessStates.FirstAsync(s => s.Id == state.Id);

        loaded.CurrentStep.ShouldBe(step);
    }

    [Fact]
    public async Task SaveChanges_AfterMarkFailed_PersistsFailureReasonAndFailedStatus()
    {
        var state = OrderProcessState.Create(OrderId.NewId());
        state.MarkFailed("insufficient stock");

        await _context.OrderProcessStates.AddAsync(state);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderProcessStates.FirstAsync(s => s.Id == state.Id);

        loaded.CurrentStep.ShouldBe(ProcessStepEnum.Failed);
        loaded.Status.ShouldBe(ProcessStatusEnum.Failed);
        loaded.FailureReason.ShouldBe("insufficient stock");
    }

    [Fact]
    public async Task SaveChanges_AfterMarkCompleted_PersistsCompletedStep()
    {
        var state = OrderProcessState.Create(OrderId.NewId());
        state.MarkCompleted();

        await _context.OrderProcessStates.AddAsync(state);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderProcessStates.FirstAsync(s => s.Id == state.Id);

        loaded.CurrentStep.ShouldBe(ProcessStepEnum.Completed);
        loaded.Status.ShouldBe(ProcessStatusEnum.Completed);
    }

    [Fact]
    public async Task SaveChanges_AfterIncrementRetry_PersistsUpdatedRetryCount()
    {
        var state = OrderProcessState.Create(OrderId.NewId());
        state.IncrementRetry();
        state.IncrementRetry();
        state.IncrementRetry();

        await _context.OrderProcessStates.AddAsync(state);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderProcessStates.FirstAsync(s => s.Id == state.Id);

        loaded.RetryCount.ShouldBe(3);
    }

    [Fact]
    public async Task SaveChanges_DuplicateOrderId_ThrowsDbUpdateExceptionDueToUniqueIndex()
    {
        var orderId = OrderId.NewId();
        var first = OrderProcessState.Create(orderId);
        var second = OrderProcessState.Create(orderId);

        await _context.OrderProcessStates.AddAsync(first);
        await _context.SaveChangesAsync();

        await Should.ThrowAsync<DbUpdateException>(async () =>
        {
            await _context.OrderProcessStates.AddAsync(second);
            await _context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task SaveChanges_TwoStatesForDifferentOrders_BothPersistSuccessfully()
    {
        var first = OrderProcessState.Create(OrderId.NewId());
        var second = OrderProcessState.Create(OrderId.NewId());

        await _context.OrderProcessStates.AddAsync(first);
        await _context.OrderProcessStates.AddAsync(second);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var firstLoaded = await freshContext.OrderProcessStates.FirstOrDefaultAsync(s => s.Id == first.Id);
        var secondLoaded = await freshContext.OrderProcessStates.FirstOrDefaultAsync(s => s.Id == second.Id);

        firstLoaded.ShouldNotBeNull();
        secondLoaded.ShouldNotBeNull();
    }

    [Fact]
    public async Task Persisted_StateMappedToOrderProcessStatesTable_IsQueryableViaContextDbSet()
    {
        var state = OrderProcessState.Create(OrderId.NewId(), correlationId: "trace-1");

        await _context.OrderProcessStates.AddAsync(state);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var exists = await freshContext.OrderProcessStates.AnyAsync(s => s.Id == state.Id);

        exists.ShouldBeTrue();
    }
}
