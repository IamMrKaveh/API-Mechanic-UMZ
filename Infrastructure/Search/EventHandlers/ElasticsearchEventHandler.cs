using Application.Audit.Contracts;
using Application.Search.Contracts;
using Application.Search.Events;
using Application.Search.Features.Shared;
using Infrastructure.Persistence.Context;
using System.Text.Json;

namespace Infrastructure.Search.EventHandlers;

public sealed class ElasticsearchEventHandler(
    DBContext context,
    IAuditService auditService) : IElasticsearchEventHandler
{
    public async Task HandleProductChangedAsync(
        ProductChangedEvent domainEvent, CancellationToken ct = default)
    {
        var document = new ProductSearchDocument
        {
            ProductId = Guid.Parse(domainEvent.EntityId.ToString()),
            Name = domainEvent.Document?.Name ?? string.Empty
        };

        var message = ElasticsearchOutboxMessage.Create(
            "Product",
            document.ProductId,
            JsonSerializer.Serialize(document),
            domainEvent.ChangeType.ToString());

        await context.ElasticsearchOutboxMessages.AddAsync(message, ct);
        await context.SaveChangesAsync(ct);

        await auditService.LogSystemEventAsync(
            "ElasticsearchProductChanged",
            $"محصول {document.ProductId} در صف ایندکس‌گذاری قرار گرفت.",
            ct);
    }

    public async Task HandleCategoryChangedAsync(
        CategoryChangedEvent domainEvent, CancellationToken ct = default)
    {
        var document = new CategorySearchDocument
        {
            CategoryId = Guid.Parse(domainEvent.EntityId.ToString()),
            Name = domainEvent.Document?.Name ?? string.Empty
        };

        var message = ElasticsearchOutboxMessage.Create(
            "Category",
            document.CategoryId,
            JsonSerializer.Serialize(document),
            domainEvent.ChangeType.ToString());

        await context.ElasticsearchOutboxMessages.AddAsync(message, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task HandleBrandChangedAsync(
        BrandChangedEvent domainEvent, CancellationToken ct = default)
    {
        var document = new BrandSearchDocument
        {
            BrandId = Guid.Parse(domainEvent.EntityId.ToString()),
            Name = domainEvent.Document?.Name ?? string.Empty
        };

        var message = ElasticsearchOutboxMessage.Create(
            "Brand",
            document.BrandId,
            JsonSerializer.Serialize(document),
            domainEvent.ChangeType.ToString());

        await context.ElasticsearchOutboxMessages.AddAsync(message, ct);
        await context.SaveChangesAsync(ct);
    }

    void IElasticsearchEventHandler.HandleProductChangedAsync(ProductChangedEvent @event, CancellationToken ct)
        => _ = HandleProductChangedAsync(@event, ct);
}