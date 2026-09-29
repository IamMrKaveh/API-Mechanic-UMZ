using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Application.Inventory.Features.Commands.BulkStockIn;
using Domain.Inventory.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Inv = Domain.Inventory.Aggregates.Inventory;
using SharedKernel.Abstractions.Interfaces;
using NSubstitute;

namespace Tests.Application.Inventory.Features.Commands.BulkStockIn;

public class BulkStockInHandlerTests : HandlerTestBase
{
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>(); private readonly BulkStockInHandler _sut;

    public BulkStockInHandlerTests()
    {
        UnitOfWork
            .ExecuteStrategyAsync(
                Arg.Any<Func<CancellationToken, Task<int>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                var op = ci.Arg<Func<CancellationToken, Task<int>>>();
                return await op!(ci.Arg<CancellationToken>());
            });

        _sut = new BulkStockInHandler(
            _inventoryRepository,
            UnitOfWork,
            AuditService,
            CurrentUserService, DateTimeProvider);
    }

    [Fact]
    public async Task Handle_WithEmptyItems_ReturnsFailure()
    {
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());

        var command = new BulkStockInCommand(Array.Empty<BulkStockInItem>(), "reason");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
    }

    [Fact]
    public async Task Handle_WithAuthenticatedUserAndValidItems_IncreasesEachStockSavesAndAudits()
    {
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());

        var inv = new InventoryBuilder().WithInitialStock(5).Build();
        _inventoryRepository
            .GetByVariantIdAsync(inv.VariantId, Arg.Any<CancellationToken>())
            .Returns(inv);

        var command = new BulkStockInCommand(new[]
        {
        new BulkStockInItem(inv.VariantId.Value, 4, "REF-1")
    }, "restock");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        inv.StockQuantity.Value.ShouldBe(9);
        _inventoryRepository.Received(1).Update(inv);
        await UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await AuditService.Received(1).LogInventoryEventAsync(
            Arg.Any<VariantId>(),
            "BulkStockIn",
            Arg.Any<string>(),
            Arg.Any<UserId?>());
    }

    [Fact]
    public async Task Handle_WhenInventoryMissing_ThrowsDomainException()
    {
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());

        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((Inv?)null);

        var command = new BulkStockInCommand(new[]
        {
        new BulkStockInItem(Guid.NewGuid(), 1, null)
    }, "reason");

        await Should.ThrowAsync<DomainException>(
            () => _sut.Handle(command, CancellationToken.None));
    }
}

