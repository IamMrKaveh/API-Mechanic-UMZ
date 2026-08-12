using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Application.Inventory.Features.Commands.BulkAdjustStock;
using Domain.Inventory.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Inv = Domain.Inventory.Aggregates.Inventory;

namespace Tests.Application.Inventory.Features.Commands.BulkAdjustStock;

public class BulkAdjustStockHandlerTests
{
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly BulkAdjustStockHandler _sut;

    public BulkAdjustStockHandlerTests()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _unitOfWork
            .ExecuteStrategyAsync(
                Arg.Any<Func<CancellationToken, Task<int>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                var op = ci.Arg<Func<CancellationToken, Task<int>>>();
                return await op(ci.Arg<CancellationToken>());
            });

        _sut = new BulkAdjustStockHandler(
            _inventoryRepository,
            _unitOfWork,
            _auditService,
            _currentUserService);
    }

    [Fact]
    public async Task Handle_WithEmptyItems_ReturnsFailureAndDoesNotUseUnitOfWork()
    {
        var command = new BulkAdjustStockCommand(Array.Empty<BulkAdjustStockItem>(), "reason");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        await _unitOfWork.DidNotReceiveWithAnyArgs().ExecuteStrategyAsync(default(Func<CancellationToken, Task<int>>)!, default);
    }

    [Fact]
    public async Task Handle_WithValidItems_AdjustsEachAndSavesChanges()
    {
        var v1 = new InventoryBuilder().WithInitialStock(10).Build();
        var v2 = new InventoryBuilder().WithInitialStock(20).Build();

        _inventoryRepository
            .GetByVariantIdAsync(v1.VariantId, Arg.Any<CancellationToken>())
            .Returns(v1);
        _inventoryRepository
            .GetByVariantIdAsync(v2.VariantId, Arg.Any<CancellationToken>())
            .Returns(v2);

        var command = new BulkAdjustStockCommand(new[]
        {
        new BulkAdjustStockItem(v1.VariantId.Value, 5),
        new BulkAdjustStockItem(v2.VariantId.Value, -4)
    }, "audit reason");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        v1.StockQuantity.Value.ShouldBe(15);
        v2.StockQuantity.Value.ShouldBe(16);
        _inventoryRepository.Received(1).Update(v1);
        _inventoryRepository.Received(1).Update(v2);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogInventoryEventAsync(
            Arg.Any<VariantId>(),
            "BulkAdjustStock",
            Arg.Any<string>(),
            Arg.Any<UserId?>());
    }

    [Fact]
    public async Task Handle_WhenAnyInventoryMissing_ThrowsDomainException()
    {
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((Inv?)null);

        var command = new BulkAdjustStockCommand(new[]
        {
        new BulkAdjustStockItem(Guid.NewGuid(), 5)
    }, "reason");

        await Should.ThrowAsync<DomainException>(
            () => _sut.Handle(command, CancellationToken.None));
    }
}
