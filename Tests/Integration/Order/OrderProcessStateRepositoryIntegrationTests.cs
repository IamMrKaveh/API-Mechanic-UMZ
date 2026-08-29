using Application.Order.Sagas.State;
using Domain.Order.Enums;
using Domain.Order.ValueObjects;
using Infrastructure.Order.Repositories;
using Infrastructure.Persistence.Context;

namespace Tests.Integration.Order;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OrderProcessStateRepositoryIntegrationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private OrderProcessStateRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");
        _context = _fixture.CreateContext();
        _sut = new OrderProcessStateRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable) return;
        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [Fact]
    public async Task GetByOrderIdAsync_ReturnsState_WhenExists()
    {
        var orderId = OrderId.NewId();
        var state = OrderProcessState.Create(orderId, "corr-1");
        _context.OrderProcessStates.Add(state);
        await _context.SaveChangesAsync();

        var result = await _sut.GetByOrderIdAsync(orderId, CancellationToken.None);

        result.ShouldNotBeNull();
        result!.OrderId.ShouldBe(orderId);
        result.CorrelationId.ShouldBe("corr-1");
        result.CurrentStep.ShouldBe(ProcessStepEnum.Created);
        result.Status.ShouldBe(ProcessStatusEnum.InProgress);
    }

    [Fact]
    public async Task GetByOrderIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _sut.GetByOrderIdAsync(OrderId.NewId(), CancellationToken.None);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task AddAsync_PersistsState_AfterSave()
    {
        var orderId = OrderId.NewId();
        var state = OrderProcessState.Create(orderId);
        await _sut.AddAsync(state, CancellationToken.None);
        await _context.SaveChangesAsync();

        await using var verify = _fixture.CreateContext();
        var loaded = await verify.OrderProcessStates.FirstOrDefaultAsync(x => x.OrderId == orderId);
        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(state.Id);
    }

    [Fact]
    public async Task OrderProcessState_EnforcesUniqueOrderIdIndex()
    {
        var orderId = OrderId.NewId();
        _context.OrderProcessStates.Add(OrderProcessState.Create(orderId));
        await _context.SaveChangesAsync();

        await using var second = _fixture.CreateContext();
        second.OrderProcessStates.Add(OrderProcessState.Create(orderId));
        await Should.ThrowAsync<DbUpdateException>(async () => await second.SaveChangesAsync());
    }

    [Fact]
    public async Task OrderProcessState_StoresEnumsAsStrings()
    {
        var orderId = OrderId.NewId();
        var state = OrderProcessState.Create(orderId);
        state.MarkFailed("payment declined");
        _context.OrderProcessStates.Add(state);
        await _context.SaveChangesAsync();

        await using var verify = _fixture.CreateContext();
        var loaded = await verify.OrderProcessStates.FirstAsync(x => x.OrderId == orderId);
        loaded.CurrentStep.ShouldBe(ProcessStepEnum.Failed);
        loaded.Status.ShouldBe(ProcessStatusEnum.Failed);
        loaded.FailureReason.ShouldBe("payment declined");
    }
}
