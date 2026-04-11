using Application.Product.Features.Commands.ActivateProduct;
using Application.Product.Features.Commands.CreateProduct;
using Application.Product.Features.Commands.DeactivateProduct;
using Application.Product.Features.Commands.DeleteProduct;
using Application.Product.Features.Commands.UpdateProduct;
using Application.Product.Features.Queries.GetAdminProducts;
using Application.Product.Features.Queries.GetProduct;
using Elastic.Clients.Elasticsearch;
using Mapster;
using MapsterMapper;
using Presentation.Product.Mapping;
using Presentation.Product.Requests;

namespace Presentation.Product.Endpoints;

[Route("api/admin/products")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminProductsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] AdminProductSearchRequest request)
    {
        var query = new GetAdminProductsQuery(
            request.CategoryId,
            request.BrandId,
            request.Search,
            request.IsActive,
            request.IncludeDeleted,
            request.Page,
            request.PageSize);
        return ToActionResult(await Mediator.Send(query));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id)
        => ToActionResult(await Mediator.Send(new GetProductQuery(id)));

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var command = Mapper
            .Map<CreateProductCommand>(request)
            .Enrich(CurrentUser.UserId);

        return ToActionResult(await Mediator.Send(command));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
    {
        var command = request.BuildAdapter()
            .AddParameters("UserId", CurrentUser.UserId)
            .AdaptToType<UpdateProductCommand>() with
        { Id = id };
        return ToActionResult(await Mediator.Send(command));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
        => ToActionResult(await Mediator.Send(new DeleteProductCommand(id, CurrentUser.UserId)));

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> ActivateProduct(Guid id)
        => ToActionResult(await Mediator.Send(new ActivateProductCommand(id, CurrentUser.UserId)));

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateProduct(Guid id)
        => ToActionResult(await Mediator.Send(new DeactivateProductCommand(id, CurrentUser.UserId)));
}