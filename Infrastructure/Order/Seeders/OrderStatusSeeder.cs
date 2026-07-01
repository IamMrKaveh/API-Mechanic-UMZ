using Domain.Order.Entities;
using Domain.Order.ValueObjects;

namespace Infrastructure.Order.Seeders;

public sealed class OrderStatusSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderStatusSeeder> logger) : IHostedService
{
    private static readonly IReadOnlyList<OrderStatusSeedDefinition> Definitions =
    [
        new(OrderStatusValue.Created.Value, OrderStatusValue.Created.DisplayName, "add_shopping_cart", "#6c757d", 0, true, true, true),
        new(OrderStatusValue.Reserved.Value, OrderStatusValue.Reserved.DisplayName, "inventory_2", "#17a2b8", 1, true, true, false),
        new(OrderStatusValue.Pending.Value, OrderStatusValue.Pending.DisplayName, "schedule", "#ffc107", 2, true, true, false),
        new(OrderStatusValue.Failed.Value, OrderStatusValue.Failed.DisplayName, "error_outline", "#dc3545", 3, true, false, false),
        new(OrderStatusValue.Paid.Value, OrderStatusValue.Paid.DisplayName, "payments", "#28a745", 4, true, false, false),
        new(OrderStatusValue.Processing.Value, OrderStatusValue.Processing.DisplayName, "autorenew", "#007bff", 5, true, false, false),
        new(OrderStatusValue.Shipped.Value, OrderStatusValue.Shipped.DisplayName, "local_shipping", "#6610f2", 6, false, false, false),
        new(OrderStatusValue.Delivered.Value, OrderStatusValue.Delivered.DisplayName, "check_circle", "#198754", 7, false, false, false),
        new(OrderStatusValue.Cancelled.Value, OrderStatusValue.Cancelled.DisplayName, "cancel", "#6c757d", 8, false, false, false),
        new(OrderStatusValue.Returned.Value, OrderStatusValue.Returned.DisplayName, "undo", "#fd7e14", 9, false, false, false),
        new(OrderStatusValue.Refunded.Value, OrderStatusValue.Refunded.DisplayName, "currency_exchange", "#20c997", 10, false, false, false),
        new(OrderStatusValue.Expired.Value, OrderStatusValue.Expired.DisplayName, "hourglass_disabled", "#adb5bd", 11, false, false, false)
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DBContext>();

        try
        {
            foreach (var definition in Definitions)
            {
                await EnsureStatusAsync(context, definition, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OrderStatus seeding failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureStatusAsync(
        DBContext context,
        OrderStatusSeedDefinition definition,
        CancellationToken ct)
    {
        var exists = await context.OrderStatuses
            .AsNoTracking()
            .AnyAsync(s => s.Name == definition.Name, ct);

        if (exists) return;

        var status = OrderStatus.Create(
            definition.Name,
            definition.DisplayName,
            definition.Icon,
            definition.Color,
            definition.SortOrder,
            definition.AllowCancel,
            definition.AllowEdit);

        if (!definition.IsDefault)
            status.Deactivate();

        if (definition.IsDefault)
            status.SetAsDefault();
        else
            status.Activate();

        await context.OrderStatuses.AddAsync(status, ct);
    }

    private sealed record OrderStatusSeedDefinition(
        string Name,
        string DisplayName,
        string Icon,
        string Color,
        int SortOrder,
        bool AllowCancel,
        bool AllowEdit,
        bool IsDefault);
}