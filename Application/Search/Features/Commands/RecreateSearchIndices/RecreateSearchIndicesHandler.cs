namespace Application.Search.Features.Commands.RecreateSearchIndices;

public class RecreateSearchIndicesHandler(IElasticIndexManager indexManager) : IRequestHandler<RecreateSearchIndicesCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(RecreateSearchIndicesCommand request, CancellationToken ct)
    {
        await indexManager.DeleteIndexAsync("products_v1", ct);
        await indexManager.DeleteIndexAsync("categories_v1", ct);
        await indexManager.DeleteIndexAsync("brands_v1", ct);
        await indexManager.CreateAllIndicesAsync(ct);
        return ServiceResult.Success();
    }
}