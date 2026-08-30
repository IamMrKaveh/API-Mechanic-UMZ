using Application.Order.Contracts;
using Application.Order.Sagas.State;
using Domain.Order.Enums;
using Domain.Order.ValueObjects;
using Infrastructure.Order.Repositories;
using Infrastructure.Persistence.Context;

namespace Tests.Infrastructure.Order.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OrderProcessStateRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private IOrderProcessStateRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new OrderProcessStateRepository(_context);
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
    public async Task AddAsync_ValidState_PersistsAcrossContexts()
    {
        var orderId = OrderId.NewId();
        var state = OrderProcessState.Create(orderId, correlationId: "corr-1");

        await _sut.AddAsync(state);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new OrderProcessStateRepository(freshContext);
        var loaded = await freshRepo.GetByOrderIdAsync(orderId);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(state.Id);
        loaded.OrderId.ShouldBe(orderId);
        loaded.CorrelationId.ShouldBe("corr-1");
        loaded.CurrentStep.ShouldBe(ProcessStepEnum.Created);
        loaded.Status.ShouldBe(ProcessStatusEnum.InProgress);
        loaded.RetryCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetByOrderIdAsync_WhenNoStateForOrder_ReturnsNull()
    {
        var loaded = await _sut.GetByOrderIdAsync(OrderId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task GetByOrderIdAsync_AfterMarkFailed_ReturnsUpdatedState()
    {
        var orderId = OrderId.NewId();
        var state = OrderProcessState.Create(orderId);
        state.MarkFailed("insufficient stock");

        await _sut.AddAsync(state);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByOrderIdAsync(orderId);

        loaded.ShouldNotBeNull();
        loaded!.Status.ShouldBe(ProcessStatusEnum.Failed);
        loaded.CurrentStep.ShouldBe(ProcessStepEnum.Failed);
        loaded.FailureReason.ShouldBe("insufficient stock");
    }

    [Fact]
    public async Task AddAsync_DuplicateOrderId_ThrowsOnSaveDueToUniqueIndex()
    {
        var orderId = OrderId.NewId();
        var first = OrderProcessState.Create(orderId);
        var second = OrderProcessState.Create(orderId);

        await _sut.AddAsync(first);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await _sut.AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }
}
