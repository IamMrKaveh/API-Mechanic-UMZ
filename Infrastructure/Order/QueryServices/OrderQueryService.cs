using System.Buffers.Binary;
using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Microsoft.Net.Http.Headers;

namespace Infrastructure.Order.QueryServices;

public sealed class OrderQueryService(
    DBContext context,
    IUrlResolverService urlResolver,
    IHttpContextAccessor httpContextAccessor) : IOrderQueryService
{
    private const string ProductEntityType = "Product";

    private static readonly OrderStatusValue[] PaidStatuses =
    {
        OrderStatusValue.Paid,
        OrderStatusValue.Processing,
        OrderStatusValue.Shipped,
        OrderStatusValue.Delivered
    };

    public async Task<PaginatedResult<OrderListItemDto>> GetUserOrdersAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId);

        var totalItems = await query.CountAsync(ct);

        var rawItems = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                Id = o.Id.Value,
                OrderNumber = o.OrderNumber.Value,
                Status = o.Status,
                FinalAmount = o.FinalAmount.Amount,
                ItemCount = o.OrderItems.Count,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(ct);

        var dtos = rawItems
            .Select(o => new OrderListItemDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status.Value,
                StatusDisplayName = o.Status.DisplayName,
                FinalAmount = o.FinalAmount,
                ItemCount = o.ItemCount,
                CreatedAt = o.CreatedAt
            })
            .ToList();

        return PaginatedResult<OrderListItemDto>.Create(dtos, totalItems, page, pageSize);
    }

    public async Task<PaginatedResult<AdminOrderDto>> GetAdminOrdersAsync(
        UserId? userId,
        string? status,
        DateTime? from,
        DateTime? to,
        bool? isPaid,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Orders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted);

        if (userId is not null)
            query = query.Where(o => o.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusVo = OrderStatusValue.From(status);
            query = query.Where(o => o.Status == statusVo);
        }

        if (from.HasValue)
            query = query.Where(o => o.CreatedAt >= from);

        if (to.HasValue)
            query = query.Where(o => o.CreatedAt <= to);

        if (isPaid.HasValue)
        {
            query = isPaid.Value
                ? query.Where(o => PaidStatuses.Contains(o.Status))
                : query.Where(o => !PaidStatuses.Contains(o.Status));
        }

        var totalItems = await query.CountAsync(ct);

        var projections = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                Dto = new AdminOrderDto
                {
                    Id = o.Id.Value,
                    UserId = o.UserId.Value,
                    OrderNumber = o.OrderNumber.Value,
                    ReceiverName = o.ReceiverInfo != null ? o.ReceiverInfo.FullName : string.Empty,
                    Status = o.Status.Value,
                    StatusDisplayName = o.Status.DisplayName,
                    TotalAmount = o.SubTotal.Amount,
                    ShippingCost = o.ShippingCost.Amount,
                    DiscountAmount = o.DiscountAmount.Amount,
                    FinalAmount = o.FinalAmount.Amount,
                    DiscountCodeId = o.AppliedDiscountCodeId != null ? o.AppliedDiscountCodeId.Value : null,
                    CancellationReason = o.CancellationReason,
                    IsPaid = o.IsPaid,
                    IsCancelled = o.IsCancelled,
                    IsDeleted = o.IsDeleted,
                    OrderItems = o.OrderItems != null
                        ? o.OrderItems.Select(i => new OrderItemDto
                        {
                            Id = i.Id.Value,
                            VariantId = i.VariantId.Value,
                            ProductId = i.ProductId.Value,
                            ProductName = i.ProductName ?? string.Empty,
                            Sku = i.Sku ?? string.Empty,
                            UnitPrice = i.UnitPrice.Amount,
                            Quantity = i.Quantity,
                            TotalPrice = i.TotalPrice.Amount
                        }).ToList()
                        : new List<OrderItemDto>(),
                    OrderItemsCount = o.OrderItems != null ? o.OrderItems.Count : 0,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt
                },
                Xmin = EF.Property<uint>(o, "xmin")
            })
            .ToListAsync(ct);

        var dtos = projections.Select(p =>
        {
            var rowVersionBytes = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(rowVersionBytes, p.Xmin);
            return p.Dto with { RowVersion = Convert.ToBase64String(rowVersionBytes) };
        }).ToList();

        return PaginatedResult<AdminOrderDto>.Create(dtos, totalItems, page, pageSize);
    }

    public async Task<OrderDto?> GetOrderDetailsAsync(
        OrderId orderId,
        UserId userId,
        CancellationToken ct = default)
    {
        var dto = await context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId && o.UserId == userId)
            .Select(o => new OrderDto
            {
                Id = o.Id.Value,
                OrderNumber = o.OrderNumber.Value,
                UserId = o.UserId.Value,
                Status = o.Status.Value,
                StatusDisplayName = o.Status.DisplayName,
                SubTotal = o.SubTotal.Amount,
                ShippingCost = o.ShippingCost.Amount,
                DiscountAmount = o.DiscountAmount.Amount,
                FinalAmount = o.FinalAmount.Amount,
                IsPaid = o.IsPaid,
                IsCancelled = o.IsCancelled,
                CancellationReason = o.CancellationReason,
                ReceiverInfo = o.ReceiverInfo != null
                    ? new ReceiverInfoDto
                    {
                        FullName = o.ReceiverInfo.FullName ?? string.Empty,
                        PhoneNumber = o.ReceiverInfo.PhoneNumber ?? string.Empty
                    }
                    : new ReceiverInfoDto { FullName = string.Empty, PhoneNumber = string.Empty },
                DeliveryAddress = o.DeliveryAddress != null
                    ? new DeliveryAddressDto
                    {
                        Province = o.DeliveryAddress.Province ?? string.Empty,
                        City = o.DeliveryAddress.City ?? string.Empty,
                        AddressLine = o.DeliveryAddress.Street ?? string.Empty,
                        PostalCode = o.DeliveryAddress.PostalCode ?? string.Empty
                    }
                    : new DeliveryAddressDto(),
                Items = o.OrderItems != null
                    ? o.OrderItems.Select(i => new OrderItemDto
                    {
                        Id = i.Id.Value,
                        VariantId = i.VariantId.Value,
                        ProductId = i.ProductId.Value,
                        ProductName = i.ProductName ?? string.Empty,
                        Sku = i.Sku ?? string.Empty,
                        UnitPrice = i.UnitPrice.Amount,
                        Quantity = i.Quantity,
                        TotalPrice = i.TotalPrice.Amount
                    }).ToList()
                    : new List<OrderItemDto>(),
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (dto is null)
            return null;

        var productIds = dto.Items
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();

        var imagePaths = await context.Medias
            .AsNoTracking()
            .Where(m => m.EntityType == ProductEntityType
                        && productIds.Contains(m.EntityId)
                        && m.IsPrimary
                        && m.IsActive)
            .Select(m => new { m.EntityId, m.FilePath })
            .ToDictionaryAsync(x => x.EntityId, x => x.FilePath, ct);

        var itemsWithImage = dto.Items.Select(item =>
        {
            imagePaths.TryGetValue(item.ProductId, out var path);
            var url = !string.IsNullOrWhiteSpace(path)
                ? urlResolver.ResolveMediaUrl(path)
                : null;
            return item with { ImageUrl = url };
        }).ToList();

        var updatedDto = dto with { Items = itemsWithImage };

        var statusValue = OrderStatusValue.From(updatedDto.Status);
        return updatedDto with
        {
            IsCancellable = statusValue.CanBeCancelled(),
            AllowedTransitions = ComputeAllowedTransitions(statusValue)
        };
    }

    public async Task<AdminOrderDto?> GetAdminOrderDetailsAsync(
        OrderId orderId,
        CancellationToken ct = default)
    {
        var projection = await context.Orders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.Id == orderId)
            .Select(o => new
            {
                Dto = new AdminOrderDto
                {
                    Id = o.Id.Value,
                    UserId = o.UserId.Value,
                    OrderNumber = o.OrderNumber.Value,
                    ReceiverName = o.ReceiverInfo != null
                        ? o.ReceiverInfo.FullName
                        : string.Empty,
                    Status = o.Status.Value,
                    StatusDisplayName = o.Status.DisplayName,
                    TotalAmount = o.SubTotal.Amount,
                    ShippingCost = o.ShippingCost.Amount,
                    DiscountAmount = o.DiscountAmount.Amount,
                    FinalAmount = o.FinalAmount.Amount,
                    DiscountCodeId = o.AppliedDiscountCodeId != null
                        ? o.AppliedDiscountCodeId.Value
                        : null,
                    CancellationReason = o.CancellationReason,
                    IsPaid = o.IsPaid,
                    IsCancelled = o.IsCancelled,
                    IsDeleted = o.IsDeleted,
                    OrderItems = o.OrderItems != null
                        ? o.OrderItems.Select(i => new OrderItemDto
                        {
                            Id = i.Id.Value,
                            VariantId = i.VariantId.Value,
                            ProductId = i.ProductId.Value,
                            ProductName = i.ProductName ?? string.Empty,
                            Sku = i.Sku ?? string.Empty,
                            UnitPrice = i.UnitPrice.Amount,
                            Quantity = i.Quantity,
                            TotalPrice = i.TotalPrice.Amount
                        }).ToList()
                        : new List<OrderItemDto>(),
                    OrderItemsCount = o.OrderItems != null
                        ? o.OrderItems.Count
                        : 0,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt
                },
                Xmin = EF.Property<uint>(o, "xmin")
            })
            .FirstOrDefaultAsync(ct);

        if (projection is null)
            return null;

        var rowVersionBytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(rowVersionBytes, projection.Xmin);
        var etag = Convert.ToBase64String(rowVersionBytes);

        httpContextAccessor.HttpContext?.Response.Headers.Append(
            HeaderNames.ETag,
            $"\"{etag}\"");

        return projection.Dto with { RowVersion = etag };
    }

    private static List<string> ComputeAllowedTransitions(OrderStatusValue current)
    {
        var candidates = new[]
        {
            OrderStatusValue.Created,
            OrderStatusValue.Reserved,
            OrderStatusValue.Pending,
            OrderStatusValue.Failed,
            OrderStatusValue.Paid,
            OrderStatusValue.Processing,
            OrderStatusValue.Shipped,
            OrderStatusValue.Delivered,
            OrderStatusValue.Cancelled,
            OrderStatusValue.Returned,
            OrderStatusValue.Refunded,
            OrderStatusValue.Expired
        };

        return candidates
            .Where(s => current.CanTransitionTo(s))
            .Select(s => s.Value)
            .ToList();
    }

    public async Task<OrderStatisticsDto> GetOrderStatisticsAsync(
        CancellationToken ct = default)
    {
        var totalOrders = await context.Orders.AsNoTracking().CountAsync(ct);

        if (totalOrders == 0)
            return new OrderStatisticsDto();

        var pendingOrders = await context.Orders.AsNoTracking()
            .CountAsync(o => o.Status == OrderStatusValue.Pending, ct);

        var processingOrders = await context.Orders.AsNoTracking()
            .CountAsync(o => o.Status == OrderStatusValue.Processing, ct);

        var completedOrders = await context.Orders.AsNoTracking()
            .CountAsync(o => o.Status == OrderStatusValue.Delivered, ct);

        var cancelledOrders = await context.Orders.AsNoTracking()
            .CountAsync(o => o.Status == OrderStatusValue.Cancelled, ct);

        var paidQuery = context.Orders.AsNoTracking()
            .Where(o => PaidStatuses.Contains(o.Status));

        var paidCount = await paidQuery.CountAsync(ct);
        var totalRevenue = paidCount > 0
            ? await paidQuery.SumAsync(o => o.FinalAmount.Amount, ct)
            : 0m;

        return new OrderStatisticsDto
        {
            TotalOrders = totalOrders,
            PendingOrders = pendingOrders,
            ProcessingOrders = processingOrders,
            CompletedOrders = completedOrders,
            CancelledOrders = cancelledOrders,
            TotalRevenue = totalRevenue,
            AverageOrderValue = paidCount > 0
                ? Math.Round(totalRevenue / paidCount, 2)
                : 0m
        };
    }
}
