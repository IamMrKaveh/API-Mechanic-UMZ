using Application.Search.Contracts;
using Application.Search.Events;
using Application.Search.Features.Shared;
using Domain.Brand.Events;
using Domain.Category.Events;
using Domain.Product.Events;
using Infrastructure.Persistence.Context;

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
            ProductId = domainEvent.ProductId.Value,
            Name = domainEvent.Name,
            ChangeType = domainEvent.ChangeType
        };

        var message = ElasticsearchOutboxMessage.Create(
            "Product",
            domainEvent.ProductId.Value,
            System.Text.Json.JsonSerializer.Serialize(document),
            domainEvent.ChangeType);

        await context.ElasticsearchOutboxMessages.AddAsync(message, ct);

        await auditService.LogSystemEventAsync(
            "ElasticsearchProductChanged",
            $"محصول {domainEvent.ProductId.Value} در صف ایندکس‌گذاری قرار گرفت.",
            ct);
    }

    public async Task HandleCategoryChangedAsync(
        CategoryChangedEvent domainEvent, CancellationToken ct = default)
    {
        var document = new CategorySearchDocument
        {
            CategoryId = domainEvent.CategoryId.Value,
            Name = domainEvent.Name
        };

        var message = ElasticsearchOutboxMessage.Create(
            "Category",
            domainEvent.CategoryId.Value,
            System.Text.Json.JsonSerializer.Serialize(document),
            domainEvent.ChangeType);

        await context.ElasticsearchOutboxMessages.AddAsync(message, ct);
    }

    public async Task HandleBrandChangedAsync(
        BrandChangedEvent domainEvent, CancellationToken ct = default)
    {
        var document = new BrandSearchDocument
        {
            BrandId = domainEvent.BrandId.Value,
            Name = domainEvent.Name
        };

        var message = ElasticsearchOutboxMessage.Create(
            "Brand",
            domainEvent.BrandId.Value,
            System.Text.Json.JsonSerializer.Serialize(document),
            domainEvent.ChangeType);

        await context.ElasticsearchOutboxMessages.AddAsync(message, ct);
    }
}