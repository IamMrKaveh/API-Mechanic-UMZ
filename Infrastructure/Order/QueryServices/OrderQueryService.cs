using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Order.QueryServices;

public sealed class OrderQueryService(DBContext context) : IOrderQueryService
{
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

        var dtos = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderListItemDto
            {
                Id = o.Id.Value,
                OrderNumber = o.OrderNumber.Value,
                Status = o.Status.Value,
                StatusDisplayName = o.Status.DisplayName,
                FinalAmount = o.FinalAmount.Amount,
                ItemCount = o.OrderItems.Count,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(ct);

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

        var dtos = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new AdminOrderDto
            {
                Id = o.Id.Value,
                UserId = o.UserId.Value,
                OrderNumber = o.OrderNumber.Value,
                ReceiverName = o.ReceiverInfo.FullName,
                Status = o.Status.Value,
                StatusDisplayName = o.Status.DisplayName,
                TotalAmount = o.SubTotal.Amount,
                ShippingCost = o.ShippingCost.Amount,
                DiscountAmount = o.DiscountAmount.Amount,
                FinalAmount = o.FinalAmount.Amount,
                DiscountCodeId = o.AppliedDiscountCodeId != null ? (Guid?)o.AppliedDiscountCodeId.Value : null,
                CancellationReason = o.CancellationReason,
                IsPaid = o.IsPaid,
                IsCancelled = o.IsCancelled,
                IsDeleted = o.IsDeleted,
                OrderItems = o.OrderItems.Select(i => new OrderItemDto
                {
                    Id = i.Id.Value,
                    VariantId = i.VariantId.Value,
                    ProductId = i.ProductId.Value,
                    ProductName = i.ProductName,
                    Sku = i.Sku,
                    UnitPrice = i.UnitPrice.Amount,
                    Quantity = i.Quantity,
                    TotalPrice = i.TotalPrice.Amount
                }).ToList(),
                OrderItemsCount = o.OrderItems.Count,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            })
            .ToListAsync(ct);

        return PaginatedResult<AdminOrderDto>.Create(dtos, totalItems, page, pageSize);
    }

    public async Task<AdminOrderDto?> GetAdminOrderDetailsAsync(
        OrderId orderId,
        CancellationToken ct = default)
    {
        return await context.Orders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.Id == orderId)
            .Select(o => new AdminOrderDto
            {
                Id = o.Id.Value,
                UserId = o.UserId.Value,
                OrderNumber = o.OrderNumber.Value,
                ReceiverName = o.ReceiverInfo.FullName,
                Status = o.Status.Value,
                StatusDisplayName = o.Status.DisplayName,
                TotalAmount = o.SubTotal.Amount,
                ShippingCost = o.ShippingCost.Amount,
                DiscountAmount = o.DiscountAmount.Amount,
                FinalAmount = o.FinalAmount.Amount,
                DiscountCodeId = o.AppliedDiscountCodeId != null ? (Guid?)o.AppliedDiscountCodeId.Value : null,
                CancellationReason = o.CancellationReason,
                IsPaid = o.IsPaid,
                IsCancelled = o.IsCancelled,
                IsDeleted = o.IsDeleted,
                OrderItems = o.OrderItems.Select(i => new OrderItemDto
                {
                    Id = i.Id.Value,
                    VariantId = i.VariantId.Value,
                    ProductId = i.ProductId.Value,
                    ProductName = i.ProductName,
                    Sku = i.Sku,
                    UnitPrice = i.UnitPrice.Amount,
                    Quantity = i.Quantity,
                    TotalPrice = i.TotalPrice.Amount
                }).ToList(),
                OrderItemsCount = o.OrderItems.Count,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<OrderDto?> GetOrderDetailsAsync(
        OrderId orderId,
        UserId userId,
        CancellationToken ct = default)
    {
        return await context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId && o.UserId.Value == userId.Value)
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
                ReceiverInfo = new ReceiverInfoDto
                {
                    FullName = o.ReceiverInfo.FullName,
                    PhoneNumber = o.ReceiverInfo.PhoneNumber
                },
                DeliveryAddress = new DeliveryAddressDto
                {
                    Province = o.DeliveryAddress.Province,
                    City = o.DeliveryAddress.City,
                    AddressLine = o.DeliveryAddress.Street,
                    PostalCode = o.DeliveryAddress.PostalCode
                },
                Items = o.OrderItems.Select(i => new OrderItemDto
                {
                    Id = i.Id.Value,
                    VariantId = i.VariantId.Value,
                    ProductId = i.ProductId.Value,
                    ProductName = i.ProductName,
                    Sku = i.Sku,
                    UnitPrice = i.UnitPrice.Amount,
                    Quantity = i.Quantity,
                    TotalPrice = i.TotalPrice.Amount
                }).ToList(),
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);
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
}