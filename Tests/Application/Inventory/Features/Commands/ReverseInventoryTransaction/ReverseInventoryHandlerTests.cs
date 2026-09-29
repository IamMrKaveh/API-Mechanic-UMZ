using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Application.Inventory.Features.Commands.ReverseInventoryTransaction;
using Domain.Inventory.Interfaces;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Inv = Domain.Inventory.Aggregates.Inventory;
using SharedKernel.Abstractions.Interfaces;
using NSubstitute;

namespace Tests.Application.Inventory.Features.Commands.ReverseInventoryTransaction;

public class ReverseInventoryHandlerTests : HandlerTestBase
{
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>(); private readonly ReverseInventoryHandler _sut;

    public ReverseInventoryHandlerTests()
    {
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _sut = new ReverseInventoryHandler(_inventoryRepository, AuditService, CurrentUserService, DateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenInventoryNotFound_ReturnsNotFound()
    {
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((Inv?)null);

        var result = await _sut.Handle(
            new ReverseInventoryCommand(Guid.NewGuid(), "key", "reason"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WithUnknownIdempotencyKey_ReturnsFailureAndDoesNotAudit()
    {
        var inventory = new InventoryBuilder().WithInitialStock(10).Build();
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.Handle(
            new ReverseInventoryCommand(inventory.VariantId.Value, "unknown-key", "reason"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        _inventoryRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await AuditService.DidNotReceiveWithAnyArgs().LogInventoryEventAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenInventoryIsUnlimited_ReturnsFailure()
    {
        var inventory = new InventoryBuilder().AsUnlimited().Build();
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.Handle(
            new ReverseInventoryCommand(inventory.VariantId.Value, "any", "reason"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
    }
}

