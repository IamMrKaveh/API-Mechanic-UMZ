using Application.Inventory.Features.Commands.AdjustStock;
using Application.Inventory.Features.Commands.BulkAdjustStock;
using Application.Inventory.Features.Commands.BulkStockIn;
using Application.Inventory.Features.Commands.ReconcileStock;
using Application.Inventory.Features.Commands.RecordDamage;
using Application.Inventory.Features.Commands.ReverseInventoryTransaction;
using Application.Inventory.Features.Queries.GetInventoryStatistics;
using Application.Inventory.Features.Queries.GetInventoryStatus;
using Application.Inventory.Features.Queries.GetInventoryTransactions;
using Application.Inventory.Features.Queries.GetLowStockProducts;
using Application.Inventory.Features.Queries.GetOutOfStockProducts;
using Application.Inventory.Features.Queries.GetProductInventoryStatuses;
using Application.Inventory.Features.Queries.GetStockLedgerByVariant;
using Application.Inventory.Features.Queries.GetWarehouseStock;
using Application.Inventory.Features.Shared;
using Application.Order.Features.Commands.ApproveReturn;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Inventory.Endpoints;
using Presentation.Inventory.Requests;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Inventory.Endpoints;

public class AdminInventoryControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AdminInventoryController _controller;

    public AdminInventoryControllerTests()
    {
        _controller = new AdminInventoryController(_mediator, Substitute.For<IMapper>());

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetInventoryTransactions_SendsQueryWithFilters_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var paged = new PaginatedResult<InventoryTransactionDto>
        {
            Items = [new InventoryTransactionDto { Id = Guid.NewGuid(), VariantId = variantId }],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mediator.Send(Arg.Any<GetInventoryTransactionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<InventoryTransactionDto>>.Success(paged));

        // Act
        var result = await _controller.GetInventoryTransactions(variantId, "StockIn", null, null, 1, 20);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetInventoryTransactionsQuery>(q => q.VariantId == variantId && q.TransactionType == "StockIn"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStockLedger_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var paged = new PaginatedResult<StockLedgerEntryDto>
        {
            Items = [new StockLedgerEntryDto { Id = Guid.NewGuid(), VariantId = variantId }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetStockLedgerByVariantQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<StockLedgerEntryDto>>.Success(paged));

        // Act
        var result = await _controller.GetStockLedger(variantId, 1, 10);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetStockLedgerByVariantQuery>(q => q.VariantId == variantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetWarehouseStock_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        IEnumerable<WarehouseStockDto> expected = [new WarehouseStockDto { VariantId = variantId, Quantity = 10 }];

        _mediator.Send(Arg.Is<GetWarehouseStockQuery>(q => q.VariantId == variantId), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IEnumerable<WarehouseStockDto>>.Success(expected));

        // Act
        var result = await _controller.GetWarehouseStock(variantId);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ReverseTransaction_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var request = new ReverseInventoryTransactionRequest(variantId, "key-1", "reason");

        _mediator.Send(Arg.Any<ReverseInventoryCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ReverseTransaction(request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ReverseInventoryCommand>(c => c.VariantId == variantId && c.IdempotencyKey == "key-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLowStockItems_WithThreshold_SendsQuery_AndReturnsOk()
    {
        // Arrange
        IEnumerable<LowStockItemDto> expected = [new LowStockItemDto { VariantId = Guid.NewGuid(), StockQuantity = 2 }];

        _mediator.Send(Arg.Any<GetLowStockProductsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IEnumerable<LowStockItemDto>>.Success(expected));

        // Act
        var result = await _controller.GetLowStockItems(5);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetLowStockProductsQuery>(q => q.Threshold == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOutOfStockItems_SendsQuery_AndReturnsOk()
    {
        // Arrange
        IEnumerable<OutOfStockItemDto> expected = [new OutOfStockItemDto { VariantId = Guid.NewGuid() }];

        _mediator.Send(Arg.Any<GetOutOfStockProductsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IEnumerable<OutOfStockItemDto>>.Success(expected));

        // Act
        var result = await _controller.GetOutOfStockItems();

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(Arg.Any<GetOutOfStockProductsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdjustStock_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var request = new AdjustStockRequest(variantId, 5, "correction");

        _mediator.Send(Arg.Any<AdjustStockCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.AdjustStock(request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<AdjustStockCommand>(c => c.VariantId == variantId && c.QuantityChange == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkAdjustStock_MapsItems_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var request = new BulkAdjustStockRequest([new BulkAdjustStockItemRequest(variantId, 3)], "bulk");

        _mediator.Send(Arg.Any<BulkAdjustStockCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.BulkAdjustStock(request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<BulkAdjustStockCommand>(c => c.Items.Count == 1 && c.Items[0].VariantId == variantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReconcileStock_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();

        _mediator.Send(Arg.Any<ReconcileStockCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ReconcileStock(variantId);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ReconcileStockCommand>(c => c.VariantId == variantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordDamage_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var request = new RecordDamageRequest(variantId, 2, "broken");

        _mediator.Send(Arg.Any<RecordDamageCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.RecordDamage(request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<RecordDamageCommand>(c => c.VariantId == variantId && c.Quantity == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStatistics_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var expected = new InventoryStatisticsDto { TotalVariants = 100, OutOfStockVariants = 5 };

        _mediator.Send(Arg.Any<GetInventoryStatisticsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<InventoryStatisticsDto>.Success(expected));

        // Act
        var result = await _controller.GetStatistics();

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<InventoryStatisticsDto>>();
        body.Data!.TotalVariants.ShouldBe(100);
    }

    [Fact]
    public async Task GetInventoryStatus_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var expected = new InventoryStatusDto { VariantId = variantId, StockQuantity = 10 };

        _mediator.Send(Arg.Is<GetInventoryStatusQuery>(q => q.VariantId == variantId), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<InventoryStatusDto>.Success(expected));

        // Act
        var result = await _controller.GetInventoryStatus(variantId);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task BulkStockIn_MapsItems_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var request = new BulkStockInRequest([new BulkStockInItemRequest(variantId, 10, "note")], "import");

        _mediator.Send(Arg.Any<BulkStockInCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.BulkStockIn(request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<BulkStockInCommand>(c => c.Items.Count == 1 && c.Items[0].VariantId == variantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveReturn_WithNullRequest_UsesDefaultReason_AndReturnsOk()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mediator.Send(Arg.Any<ApproveReturnCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ApproveReturn(orderId);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ApproveReturnCommand>(c => c.OrderId == orderId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProductInventoryStatuses_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var productId = Guid.NewGuid();
        IReadOnlyList<InventoryStatusDto> expected =
            [new InventoryStatusDto { VariantId = Guid.NewGuid(), StockQuantity = 7 }];

        _mediator.Send(Arg.Is<GetProductInventoryStatusesQuery>(q => q.ProductId == productId), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<InventoryStatusDto>>.Success(expected));

        // Act
        var result = await _controller.GetProductInventoryStatuses(productId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public void AdminInventoryController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminInventoryController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminInventoryController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminInventoryController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/inventory");
    }
}
