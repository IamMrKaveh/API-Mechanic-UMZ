namespace Application.Search.Features.Commands.RecreateSearchIndices;

public class RecreateSearchIndicesHandler : IRequestHandler<RecreateSearchIndicesCommand, ServiceResult>
{
    private readonly IElasticIndexManager _indexManager;

    public RecreateSearchIndicesHandler(IElasticIndexManager indexManager) => _indexManager = indexManager;

    public async Task<ServiceResult> Handle(RecreateSearchIndicesCommand request, CancellationToken ct)
    {
        await _indexManager.DeleteIndexAsync("products_v1", ct);
        await _indexManager.DeleteIndexAsync("categories_v1", ct);
        await _indexManager.DeleteIndexAsync("brands_v1", ct);
        await _indexManager.CreateAllIndicesAsync(ct);
        return ServiceResult.Success();
    }
}