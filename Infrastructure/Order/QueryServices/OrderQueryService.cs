using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using System.Buffers.Binary;

namespace Infrastructure.Order.QueryServices;

public sealed class OrderQueryService(
    DBContext context,
    IUrlResolverService urlResolver) : IOrderQueryService
{
    private const string ProductEntityType = "Product";

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
                StatusValue = o.Status.Value,
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
                Status = o.StatusValue,
                StatusDisplayName = OrderStatusValue.From(o.StatusValue).DisplayName,
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
            query = query.Where(o => o.Status == status);

        if (from.HasValue)
            query = query.Where(o => o.CreatedAt >= from);

        if (to.HasValue)
            query = query.Where(o => o.CreatedAt <= to);

        if (isPaid.HasValue)
        {
            var paidStatuses = new[] { "Paid", "Processing", "Shipped", "Delivered" };
            query = isPaid.Value
                ? query.Where(o => paidStatuses.Contains(o.Status))
                : query.Where(o => !paidStatuses.Contains(o.Status));
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
                DiscountCodeId = o.AppliedDiscountCodeId != null ? o.AppliedDiscountCodeId.Value : null,
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
        var dto = await context.Orders
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
                DiscountCodeId = o.AppliedDiscountCodeId != null ? o.AppliedDiscountCodeId.Value : null,
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

        if (dto is null)
            return null;

        var xmin = await context.Orders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.Id == orderId)
            .Select(o => EF.Property<uint>(o, "xmin"))
            .FirstOrDefaultAsync(ct);

        var statusValue = OrderStatusValue.From(dto.Status);

        var rowVersionBytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(rowVersionBytes, xmin);
        var rowVersion = Convert.ToBase64String(rowVersionBytes);

        return dto with
        {
            IsCancellable = statusValue.CanBeCancelled(),
            AllowedTransitions = ComputeAllowedTransitions(statusValue),
            RowVersion = rowVersion
        };
    }

    private static IReadOnlyList<string> ComputeAllowedTransitions(OrderStatusValue current)
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