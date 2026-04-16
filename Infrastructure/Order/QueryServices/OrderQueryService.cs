using Application.Order.Contracts;
using Application.Order.Features.Shared;
using Domain.Order.Entities;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Order.QueryServices;

public sealed class OrderQueryService(DBContext context) : IOrderQueryService
{
    public async Task<OrderDto?> GetOrderByIdAsync(
        OrderId orderId,
        CancellationToken ct = default)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Where(o => o.Id == orderId)
            .FirstOrDefaultAsync(ct);

        return order is null ? null : MapToOrderDto(order);
    }

    public async Task<OrderDto?> GetOrderByNumberAsync(
        OrderNumber orderNumber,
        CancellationToken ct = default)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Where(o => o.OrderNumber == orderNumber)
            .FirstOrDefaultAsync(ct);

        return order is null ? null : MapToOrderDto(order);
    }

    public async Task<PaginatedResult<OrderListItemDto>> GetUserOrdersAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Orders
            .AsNoTracking()
            .Where(o => o.UserId.Value == userId.Value);

        var totalItems = await query.CountAsync(ct);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = orders.Select(o => new OrderListItemDto
        {
            Id = o.Id.Value,
            OrderNumber = o.OrderNumber.Value,
            Status = o.Status.Value,
            StatusDisplayName = o.Status.DisplayName,
            FinalAmount = o.FinalAmount.Amount,
            ItemCount = o.OrderItems.Count,
            CreatedAt = o.CreatedAt
        }).ToList();

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
            .Include(o => o.OrderItems)
            .Where(o => !o.IsDeleted);

        if (userId is not null)
            query = query.Where(o => o.UserId.Value == userId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status.Value == status);

        if (from.HasValue)
            query = query.Where(o => o.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(o => o.CreatedAt <= to.Value);

        if (isPaid.HasValue)
        {
            var paidStatuses = new[] { "Paid", "Processing", "Shipped", "Delivered" };
            query = isPaid.Value
                ? query.Where(o => paidStatuses.Contains(o.Status.Value))
                : query.Where(o => !paidStatuses.Contains(o.Status.Value));
        }

        var totalItems = await query.CountAsync(ct);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = orders.Select(MapToAdminOrderDto).ToList();
        return PaginatedResult<AdminOrderDto>.Create(dtos, totalItems, page, pageSize);
    }

    public async Task<AdminOrderDto?> GetAdminOrderDetailsAsync(
        OrderId orderId,
        CancellationToken ct = default)
    {
        var order = await context.Orders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(o => o.OrderItems)
            .Where(o => o.Id == orderId)
            .FirstOrDefaultAsync(ct);

        return order is null ? null : MapToAdminOrderDto(order);
    }

    public async Task<OrderDto?> GetOrderDetailsAsync(
        OrderId orderId,
        UserId userId,
        CancellationToken ct = default)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Where(o => o.Id == orderId && o.UserId.Value == userId.Value)
            .FirstOrDefaultAsync(ct);

        return order is null ? null : MapToOrderDto(order);
    }

    public async Task<OrderStatisticsDto> GetOrderStatisticsAsync(
        CancellationToken ct = default)
    {
        var paidStatuses = new[] { "Paid", "Processing", "Shipped", "Delivered" };

        var stats = await context.Orders
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalOrders = g.Count(),
                PendingOrders = g.Count(o => o.Status.Value == "Pending"),
                ProcessingOrders = g.Count(o => o.Status.Value == "Processing"),
                CompletedOrders = g.Count(o => o.Status.Value == "Delivered"),
                CancelledOrders = g.Count(o => o.Status.Value == "Cancelled"),
                TotalRevenue = g
                    .Where(o => paidStatuses.Contains(o.Status.Value))
                    .Sum(o => o.FinalAmount.Amount),
                PaidCount = g.Count(o => paidStatuses.Contains(o.Status.Value))
            })
            .FirstOrDefaultAsync(ct);

        if (stats is null)
            return new OrderStatisticsDto();

        return new OrderStatisticsDto
        {
            TotalOrders = stats.TotalOrders,
            PendingOrders = stats.PendingOrders,
            ProcessingOrders = stats.ProcessingOrders,
            CompletedOrders = stats.CompletedOrders,
            CancelledOrders = stats.CancelledOrders,
            TotalRevenue = stats.TotalRevenue,
            AverageOrderValue = stats.PaidCount > 0
                ? Math.Round(stats.TotalRevenue / stats.PaidCount, 2)
                : 0
        };
    }

    private static OrderDto MapToOrderDto(Domain.Order.Aggregates.Order order)
    {
        return new OrderDto
        {
            Id = order.Id.Value,
            OrderNumber = order.OrderNumber.Value,
            UserId = order.UserId.Value,
            Status = order.Status.Value,
            StatusDisplayName = order.Status.DisplayName,
            SubTotal = order.SubTotal.Amount,
            ShippingCost = order.ShippingCost.Amount,
            DiscountAmount = order.DiscountAmount.Amount,
            FinalAmount = order.FinalAmount.Amount,
            IsPaid = order.IsPaid,
            IsCancelled = order.IsCancelled,
            CancellationReason = order.CancellationReason,
            ReceiverInfo = new ReceiverInfoDto
            {
                FullName = order.ReceiverInfo.FullName,
                PhoneNumber = order.ReceiverInfo.PhoneNumber
            },
            DeliveryAddress = new DeliveryAddressDto
            {
                Province = order.DeliveryAddress.Province,
                City = order.DeliveryAddress.City,
                AddressLine = order.DeliveryAddress.Street,
                PostalCode = order.DeliveryAddress.PostalCode
            },
            Items = order.OrderItems.Select(MapToOrderItemDto).ToList(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }

    private static AdminOrderDto MapToAdminOrderDto(Domain.Order.Aggregates.Order order)
    {
        return new AdminOrderDto
        {
            Id = order.Id.Value,
            UserId = order.UserId.Value,
            OrderNumber = order.OrderNumber.Value,
            ReceiverName = order.ReceiverInfo.FullName,
            Status = order.Status.Value,
            StatusDisplayName = order.Status.DisplayName,
            TotalAmount = order.SubTotal.Amount,
            ShippingCost = order.ShippingCost.Amount,
            DiscountAmount = order.DiscountAmount.Amount,
            FinalAmount = order.FinalAmount.Amount,
            DiscountCodeId = order.AppliedDiscountCodeId?.Value,
            CancellationReason = order.CancellationReason,
            IsPaid = order.IsPaid,
            IsCancelled = order.IsCancelled,
            IsDeleted = order.IsDeleted,
            OrderItems = order.OrderItems.Select(MapToOrderItemDto).ToList(),
            OrderItemsCount = order.OrderItems.Count,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }

    private static OrderItemDto MapToOrderItemDto(OrderItem item)
    {
        return new OrderItemDto
        {
            Id = item.Id.Value,
            VariantId = item.VariantId.Value,
            ProductId = item.ProductId.Value,
            ProductName = item.ProductName,
            Sku = item.Sku,
            UnitPrice = item.UnitPrice.Amount,
            Quantity = item.Quantity,
            TotalPrice = item.TotalPrice.Amount
        };
    }
}