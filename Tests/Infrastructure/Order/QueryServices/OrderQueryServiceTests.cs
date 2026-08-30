using Application.Common.Contracts;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Aggregates;
using Infrastructure.Order.QueryServices;
using Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Http;
using Orders = Domain.Order.Aggregates.Order;
using Products = Domain.Product.Aggregates.Product;
using Users = Domain.User.Aggregates.User;

namespace Tests.Infrastructure.Order.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OrderQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private IUrlResolverService _urlResolver = null!;
    private IHttpContextAccessor _httpContextAccessor = null!;
    private DefaultHttpContext _httpContext = null!;
    private OrderQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _urlResolver = Substitute.For<IUrlResolverService>();
        _urlResolver
            .ResolveMediaUrl(Arg.Any<string>())
            .Returns(callInfo => $"https://cdn.test/{callInfo.Arg<string>()}");

        _httpContext = new DefaultHttpContext();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _httpContextAccessor.HttpContext.Returns(_httpContext);

        _sut = new OrderQueryService(_context, _urlResolver, _httpContextAccessor);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<Users> SeedUserAsync()
    {
        var user = new UserBuilder().Build();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<(Products product, ProductVariant variant)> SeedProductAndVariantAsync()
    {
        var product = new ProductBuilder().Build();
        _context.Products.Add(product);

        var variant = new ProductVariantBuilder()
            .WithProductId(product.Id)
            .WithSellingPrice(100_000m)
            .Build();
        _context.ProductVariants.Add(variant);

        await _context.SaveChangesAsync();
        return (product, variant);
    }

    private OrderItemSnapshot BuildSnapshot(ProductVariant variant, Products product, int quantity = 2, decimal unitPrice = 100_000m)
    {
        return new OrderItemSnapshotBuilder()
            .WithVariantId(variant.Id)
            .WithProductId(product.Id)
            .WithProductName(product.Name)
            .WithSku(variant.Sku)
            .WithUnitPrice(unitPrice)
            .WithQuantity(quantity)
            .Build();
    }

    private async Task<Orders> SeedOrderAsync(
        UserId? userIdOverride = null,
        Action<OrderBuilder>? customize = null,
        params OrderItemSnapshot[] snapshots)
    {
        Users? user = null;
        if (userIdOverride is null)
        {
            user = await SeedUserAsync();
        }

        var effectiveSnapshots = snapshots.Length > 0
            ? snapshots
            : await BuildDefaultSnapshotsAsync();

        var builder = new OrderBuilder()
            .WithUserId(userIdOverride ?? user!.Id)
            .WithItemSnapshots(effectiveSnapshots);

        customize?.Invoke(builder);

        var order = builder.Build();
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    private async Task<OrderItemSnapshot[]> BuildDefaultSnapshotsAsync()
    {
        var (product, variant) = await SeedProductAndVariantAsync();
        return new[] { BuildSnapshot(variant, product) };
    }

    private async Task<PaymentTransactionId> SeedPaymentTransactionAsync(Orders order)
    {
        var transaction = new PaymentTransactionBuilder()
            .WithOrderId(order.Id)
            .WithAmount(order.FinalAmount.Amount)
            .Build();
        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction.Id;
    }

    [Fact]
    public async Task GetUserOrdersAsync_WithNoOrders_ReturnsEmptyPaginatedResult()
    {
        var user = await SeedUserAsync();

        var result = await _sut.GetUserOrdersAsync(user.Id, page: 1, pageSize: 10);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(10);
    }

    [Fact]
    public async Task GetUserOrdersAsync_ReturnsOnlyOrdersOfTheGivenUser()
    {
        var userA = await SeedUserAsync();
        var userB = await SeedUserAsync();

        await SeedOrderAsync(userA.Id);
        await SeedOrderAsync(userA.Id);
        await SeedOrderAsync(userB.Id);

        var result = await _sut.GetUserOrdersAsync(userA.Id, page: 1, pageSize: 10);

        result.TotalCount.ShouldBe(2);
        result.Items.ShouldAllBe(o => o.Id != Guid.Empty);
    }

    [Fact]
    public async Task GetUserOrdersAsync_OrdersByCreatedAtDescending()
    {
        var user = await SeedUserAsync();
        var order1 = await SeedOrderAsync(user.Id);
        await Task.Delay(20);
        var order2 = await SeedOrderAsync(user.Id);
        await Task.Delay(20);
        var order3 = await SeedOrderAsync(user.Id);

        var result = await _sut.GetUserOrdersAsync(user.Id, page: 1, pageSize: 10);

        result.Items.Count.ShouldBe(3);
        result.Items[0].Id.ShouldBe(order3.Id.Value);
        result.Items[1].Id.ShouldBe(order2.Id.Value);
        result.Items[2].Id.ShouldBe(order1.Id.Value);
    }

    [Fact]
    public async Task GetUserOrdersAsync_MapsOrderPropertiesAndItemCount()
    {
        var user = await SeedUserAsync();
        var (product, variant) = await SeedProductAndVariantAsync();
        var snapshotA = BuildSnapshot(variant, product, quantity: 2);
        var snapshotB = BuildSnapshot(variant, product, quantity: 3);

        var order = await SeedOrderAsync(user.Id, null, snapshotA, snapshotB);

        var result = await _sut.GetUserOrdersAsync(user.Id, page: 1, pageSize: 10);

        result.Items.Count.ShouldBe(1);
        var dto = result.Items[0];
        dto.Id.ShouldBe(order.Id.Value);
        dto.OrderNumber.ShouldBe(order.OrderNumber.Value);
        dto.Status.ShouldBe(order.Status.Value);
        dto.StatusDisplayName.ShouldBe(order.Status.DisplayName);
        dto.FinalAmount.ShouldBe(order.FinalAmount.Amount);
        dto.ItemCount.ShouldBe(2);
        dto.CreatedAt.ShouldBe(order.CreatedAt);
    }

    [Theory]
    [InlineData(1, 2, 2, 5)]
    [InlineData(3, 2, 1, 5)]
    [InlineData(1, 10, 5, 5)]
    public async Task GetUserOrdersAsync_PaginatesResultsCorrectly(int page, int pageSize, int expected, int total)
    {
        var user = await SeedUserAsync();
        for (var i = 0; i < total; i++)
        {
            await SeedOrderAsync(user.Id);
            await Task.Delay(10);
        }

        var result = await _sut.GetUserOrdersAsync(user.Id, page, pageSize);

        result.Items.Count.ShouldBe(expected);
        result.TotalCount.ShouldBe(total);
    }

    [Fact]
    public async Task GetAdminOrdersAsync_WithNoFilters_ReturnsAllNonDeletedOrders()
    {
        await SeedOrderAsync();
        await SeedOrderAsync();
        await SeedOrderAsync();

        var result = await _sut.GetAdminOrdersAsync(
            userId: null, status: null, from: null, to: null, isPaid: null,
            page: 1, pageSize: 20);

        result.TotalCount.ShouldBe(3);
        result.Items.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetAdminOrdersAsync_IncludesDeletedOrdersButFiltersThemOut()
    {
        var order = await SeedOrderAsync();
        order.MarkAsDeleted();
        await _context.SaveChangesAsync();
        await SeedOrderAsync();

        var result = await _sut.GetAdminOrdersAsync(
            userId: null, status: null, from: null, to: null, isPaid: null,
            page: 1, pageSize: 20);

        result.TotalCount.ShouldBe(1);
        result.Items.ShouldAllBe(o => o.IsDeleted == false);
    }

    [Fact]
    public async Task GetAdminOrdersAsync_FiltersByUserId()
    {
        var userA = await SeedUserAsync();
        var userB = await SeedUserAsync();
        await SeedOrderAsync(userA.Id);
        await SeedOrderAsync(userA.Id);
        await SeedOrderAsync(userB.Id);

        var result = await _sut.GetAdminOrdersAsync(
            userId: userA.Id, status: null, from: null, to: null, isPaid: null,
            page: 1, pageSize: 20);

        result.TotalCount.ShouldBe(2);
        result.Items.ShouldAllBe(o => o.UserId == userA.Id.Value);
    }

    [Fact]
    public async Task GetAdminOrdersAsync_FiltersByStatus()
    {
        var pendingOrder = await SeedOrderAsync();
        pendingOrder.MoveToPending();
        await _context.SaveChangesAsync();

        await SeedOrderAsync();

        var result = await _sut.GetAdminOrdersAsync(
            userId: null, status: OrderStatusValue.Pending.Value, from: null, to: null, isPaid: null,
            page: 1, pageSize: 20);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(pendingOrder.Id.Value);
    }

    [Fact]
    public async Task GetAdminOrdersAsync_FiltersByDateRange()
    {
        var orderOld = await SeedOrderAsync();
        await Task.Delay(50);
        var orderMid = await SeedOrderAsync();
        await Task.Delay(50);
        var orderNew = await SeedOrderAsync();

        var from = orderMid.CreatedAt.AddMilliseconds(-1);
        var to = orderMid.CreatedAt.AddMilliseconds(1);

        var result = await _sut.GetAdminOrdersAsync(
            userId: null, status: null, from: from, to: to, isPaid: null,
            page: 1, pageSize: 20);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(orderMid.Id.Value);
    }

    [Fact]
    public async Task GetAdminOrdersAsync_WithIsPaidTrue_ReturnsOnlyPaidStatuses()
    {
        var paidOrder = await SeedOrderAsync();
        paidOrder.MoveToPending();
        var tx = await SeedPaymentTransactionAsync(paidOrder);
        paidOrder.MarkAsPaid(tx);
        await _context.SaveChangesAsync();

        await SeedOrderAsync();

        var result = await _sut.GetAdminOrdersAsync(
            userId: null, status: null, from: null, to: null, isPaid: true,
            page: 1, pageSize: 20);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(paidOrder.Id.Value);
        result.Items[0].IsPaid.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAdminOrdersAsync_WithIsPaidFalse_ExcludesPaidStatuses()
    {
        var paidOrder = await SeedOrderAsync();
        paidOrder.MoveToPending();
        var tx = await SeedPaymentTransactionAsync(paidOrder);
        paidOrder.MarkAsPaid(tx);
        await _context.SaveChangesAsync();

        var pendingOrder = await SeedOrderAsync();

        var result = await _sut.GetAdminOrdersAsync(
            userId: null, status: null, from: null, to: null, isPaid: false,
            page: 1, pageSize: 20);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(pendingOrder.Id.Value);
    }

    [Fact]
    public async Task GetAdminOrdersAsync_OrdersByCreatedAtDescending()
    {
        var older = await SeedOrderAsync();
        await Task.Delay(20);
        var newer = await SeedOrderAsync();

        var result = await _sut.GetAdminOrdersAsync(
            userId: null, status: null, from: null, to: null, isPaid: null,
            page: 1, pageSize: 20);

        result.Items[0].Id.ShouldBe(newer.Id.Value);
        result.Items[1].Id.ShouldBe(older.Id.Value);
    }

    [Fact]
    public async Task GetAdminOrdersAsync_ProjectsRowVersionAsBase64FromXmin()
    {
        var order = await SeedOrderAsync();

        var result = await _sut.GetAdminOrdersAsync(
            userId: null, status: null, from: null, to: null, isPaid: null,
            page: 1, pageSize: 20);

        result.Items.Count.ShouldBe(1);
        var dto = result.Items[0];
        dto.Id.ShouldBe(order.Id.Value);
        dto.RowVersion.ShouldNotBeNullOrWhiteSpace();
        Convert.FromBase64String(dto.RowVersion).Length.ShouldBe(4);
    }

    [Fact]
    public async Task GetAdminOrdersAsync_MapsReceiverNameAndOrderItems()
    {
        var user = await SeedUserAsync();
        var (product, variant) = await SeedProductAndVariantAsync();
        var snapshot = BuildSnapshot(variant, product, quantity: 3, unitPrice: 50_000m);

        var order = await SeedOrderAsync(user.Id, null, snapshot);

        var result = await _sut.GetAdminOrdersAsync(
            userId: user.Id, status: null, from: null, to: null, isPaid: null,
            page: 1, pageSize: 20);

        result.Items.Count.ShouldBe(1);
        var dto = result.Items[0];
        dto.ReceiverName.ShouldBe(order.ReceiverInfo.FullName);
        dto.OrderItems.Count.ShouldBe(1);
        dto.OrderItems[0].VariantId.ShouldBe(variant.Id.Value);
        dto.OrderItems[0].ProductId.ShouldBe(product.Id.Value);
        dto.OrderItems[0].Quantity.ShouldBe(3);
        dto.OrderItems[0].UnitPrice.ShouldBe(50_000m);
        dto.OrderItemsCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetOrderDetailsAsync_WithUnknownId_ReturnsNull()
    {
        var user = await SeedUserAsync();

        var result = await _sut.GetOrderDetailsAsync(OrderId.NewId(), user.Id);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetOrderDetailsAsync_ForOrderNotBelongingToUser_ReturnsNull()
    {
        var owner = await SeedUserAsync();
        var stranger = await SeedUserAsync();
        var order = await SeedOrderAsync(owner.Id);

        var result = await _sut.GetOrderDetailsAsync(order.Id, stranger.Id);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetOrderDetailsAsync_ForOwnOrder_ReturnsMappedDto()
    {
        var user = await SeedUserAsync();
        var (product, variant) = await SeedProductAndVariantAsync();
        var snapshot = BuildSnapshot(variant, product, quantity: 2, unitPrice: 75_000m);
        var order = await SeedOrderAsync(user.Id, null, snapshot);

        var result = await _sut.GetOrderDetailsAsync(order.Id, user.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(order.Id.Value);
        result.OrderNumber.ShouldBe(order.OrderNumber.Value);
        result.UserId.ShouldBe(user.Id.Value);
        result.Status.ShouldBe(order.Status.Value);
        result.StatusDisplayName.ShouldBe(order.Status.DisplayName);
        result.SubTotal.ShouldBe(order.SubTotal.Amount);
        result.ShippingCost.ShouldBe(order.ShippingCost.Amount);
        result.DiscountAmount.ShouldBe(order.DiscountAmount.Amount);
        result.FinalAmount.ShouldBe(order.FinalAmount.Amount);
        result.IsPaid.ShouldBe(order.IsPaid);
        result.IsCancelled.ShouldBe(order.IsCancelled);
        result.CreatedAt.ShouldBe(order.CreatedAt);
        result.Items.Count.ShouldBe(1);
        result.Items[0].VariantId.ShouldBe(variant.Id.Value);
        result.Items[0].Quantity.ShouldBe(2);
        result.Items[0].UnitPrice.ShouldBe(75_000m);
        result.ReceiverInfo.ShouldNotBeNull();
        result.ReceiverInfo.FullName.ShouldBe(order.ReceiverInfo.FullName);
        result.ReceiverInfo.PhoneNumber.ShouldBe(order.ReceiverInfo.PhoneNumber);
        result.DeliveryAddress.ShouldNotBeNull();
        result.DeliveryAddress.Province.ShouldBe(order.DeliveryAddress.Province);
        result.DeliveryAddress.City.ShouldBe(order.DeliveryAddress.City);
        result.DeliveryAddress.AddressLine.ShouldBe(order.DeliveryAddress.Street);
        result.DeliveryAddress.PostalCode.ShouldBe(order.DeliveryAddress.PostalCode);
    }

    [Fact]
    public async Task GetOrderDetailsAsync_ForNewOrder_IsCancellableIsTrueAndAllowedTransitionsIncludeCancelled()
    {
        var user = await SeedUserAsync();
        var order = await SeedOrderAsync(user.Id);

        var result = await _sut.GetOrderDetailsAsync(order.Id, user.Id);

        result.ShouldNotBeNull();
        result.IsCancellable.ShouldBeTrue();
        result.AllowedTransitions.ShouldContain(OrderStatusValue.Cancelled.Value);
    }

    [Fact]
    public async Task GetOrderDetailsAsync_ForCancelledOrder_HasEmptyAllowedTransitions()
    {
        var user = await SeedUserAsync();
        var order = await SeedOrderAsync(user.Id);
        order.Cancel("لغو تست");
        await _context.SaveChangesAsync();

        var result = await _sut.GetOrderDetailsAsync(order.Id, user.Id);

        result.ShouldNotBeNull();
        result.Status.ShouldBe(OrderStatusValue.Cancelled.Value);
        result.IsCancellable.ShouldBeFalse();
        result.AllowedTransitions.ShouldBeEmpty();
        result.CancellationReason.ShouldBe("لغو تست");
    }

    [Fact]
    public async Task GetOrderDetailsAsync_WithProductPrimaryImage_ResolvesImageUrl()
    {
        var user = await SeedUserAsync();
        var (product, variant) = await SeedProductAndVariantAsync();
        var snapshot = BuildSnapshot(variant, product);
        var order = await SeedOrderAsync(user.Id, null, snapshot);

        var media = new MediaBuilder()
            .WithEntityType("Product")
            .WithEntityId(product.Id.Value)
            .WithIsPrimary(false)
            .Build();
        _context.Medias.Add(media);
        await _context.SaveChangesAsync();
        media.SetAsPrimary();
        await _context.SaveChangesAsync();

        var result = await _sut.GetOrderDetailsAsync(order.Id, user.Id);

        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.Items[0].ImageUrl.ShouldNotBeNullOrWhiteSpace();
        _urlResolver.Received().ResolveMediaUrl(media.FilePath);
    }

    [Fact]
    public async Task GetOrderDetailsAsync_WhenProductHasNoPrimaryImage_LeavesImageUrlNull()
    {
        var user = await SeedUserAsync();
        var order = await SeedOrderAsync(user.Id);

        var result = await _sut.GetOrderDetailsAsync(order.Id, user.Id);

        result.ShouldNotBeNull();
        result.Items.ShouldAllBe(item => item.ImageUrl == null);
    }

    [Fact]
    public async Task GetAdminOrderDetailsAsync_WithUnknownId_ReturnsNull()
    {
        var result = await _sut.GetAdminOrderDetailsAsync(OrderId.NewId());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAdminOrderDetailsAsync_ReturnsMappedDtoWithRowVersionAndSetsETagHeader()
    {
        var order = await SeedOrderAsync();

        var result = await _sut.GetAdminOrderDetailsAsync(order.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(order.Id.Value);
        result.OrderNumber.ShouldBe(order.OrderNumber.Value);
        result.RowVersion.ShouldNotBeNullOrWhiteSpace();
        Convert.FromBase64String(result.RowVersion).Length.ShouldBe(4);
        _httpContext.Response.Headers.ETag.Count.ShouldBe(1);
        _httpContext.Response.Headers.ETag[0]!.ShouldBe($"\"{result.RowVersion}\"");
    }

    [Fact]
    public async Task GetAdminOrderDetailsAsync_IncludesSoftDeletedOrders()
    {
        var order = await SeedOrderAsync();
        order.MarkAsDeleted();
        await _context.SaveChangesAsync();

        var result = await _sut.GetAdminOrderDetailsAsync(order.Id);

        result.ShouldNotBeNull();
        result.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task GetOrderStatisticsAsync_WithNoOrders_ReturnsAllZeros()
    {
        var result = await _sut.GetOrderStatisticsAsync();

        result.ShouldNotBeNull();
        result.TotalOrders.ShouldBe(0);
        result.PendingOrders.ShouldBe(0);
        result.ProcessingOrders.ShouldBe(0);
        result.CompletedOrders.ShouldBe(0);
        result.CancelledOrders.ShouldBe(0);
        result.TotalRevenue.ShouldBe(0m);
        result.AverageOrderValue.ShouldBe(0m);
    }

    [Fact]
    public async Task GetOrderStatisticsAsync_CountsOrdersByEachStatus()
    {
        var pending = await SeedOrderAsync();
        pending.MoveToPending();

        var paidTx = await SeedPaymentTransactionAsync(pending);

        var paid = await SeedOrderAsync();
        paid.MoveToPending();
        var tx = await SeedPaymentTransactionAsync(paid);
        paid.MarkAsPaid(tx);

        var processing = await SeedOrderAsync();
        processing.MoveToPending();
        var tx2 = await SeedPaymentTransactionAsync(processing);
        processing.MarkAsPaid(tx2);
        processing.StartProcessing();

        var delivered = await SeedOrderAsync();
        delivered.MoveToPending();
        var tx3 = await SeedPaymentTransactionAsync(delivered);
        delivered.MarkAsPaid(tx3);
        delivered.StartProcessing();
        delivered.MarkAsShipped();
        delivered.MarkAsDelivered();

        var cancelled = await SeedOrderAsync();
        cancelled.Cancel("cancelled");

        await _context.SaveChangesAsync();

        var result = await _sut.GetOrderStatisticsAsync();

        result.TotalOrders.ShouldBe(5);
        result.PendingOrders.ShouldBe(1);
        result.ProcessingOrders.ShouldBe(1);
        result.CompletedOrders.ShouldBe(1);
        result.CancelledOrders.ShouldBe(1);
    }

    [Fact]
    public async Task GetOrderStatisticsAsync_ComputesTotalRevenueAndAverageOnlyOverPaidStatuses()
    {
        var paid = await SeedOrderAsync();
        paid.MoveToPending();
        var tx1 = await SeedPaymentTransactionAsync(paid);
        paid.MarkAsPaid(tx1);

        var delivered = await SeedOrderAsync();
        delivered.MoveToPending();
        var tx2 = await SeedPaymentTransactionAsync(delivered);
        delivered.MarkAsPaid(tx2);
        delivered.StartProcessing();
        delivered.MarkAsShipped();
        delivered.MarkAsDelivered();

        await SeedOrderAsync();

        await _context.SaveChangesAsync();

        var result = await _sut.GetOrderStatisticsAsync();

        var expectedRevenue = paid.FinalAmount.Amount + delivered.FinalAmount.Amount;
        result.TotalRevenue.ShouldBe(expectedRevenue);
        result.AverageOrderValue.ShouldBe(Math.Round(expectedRevenue / 2m, 2));
    }
}
