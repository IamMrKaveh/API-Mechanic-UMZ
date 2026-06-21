using Application.Common.Interfaces;
using Application.Inventory.Contracts;
using Application.Inventory.Features.Shared;
using Domain.Product.ValueObjects;
using SharedKernel.Results;

namespace Application.Inventory.Features.Queries.GetProductInventoryStatuses;

public sealed class GetProductInventoryStatusesHandler(
    IInventoryQueryService inventoryQueryService)
    : IQueryHandler<GetProductInventoryStatusesQuery, IReadOnlyList<InventoryStatusDto>>
{
    public async Task<ServiceResult<IReadOnlyList<InventoryStatusDto>>> Handle(
        GetProductInventoryStatusesQuery request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);

        var statuses = await inventoryQueryService
            .GetInventoryStatusesByProductAsync(productId, ct);

        return ServiceResult<IReadOnlyList<InventoryStatusDto>>.Success(statuses);
    }
}