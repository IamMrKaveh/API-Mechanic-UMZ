using Application.Product.Features.Commands.ActivateProduct;
using Application.Product.Features.Commands.BulkUpdatePrices;
using Application.Product.Features.Commands.CreateProduct;
using Application.Product.Features.Commands.DeactivateProduct;
using Application.Product.Features.Commands.DeleteProduct;
using Application.Product.Features.Commands.RestoreProduct;
using Application.Product.Features.Commands.UpdateProduct;
using Application.Product.Features.Commands.UpdateProductDetails;
using Application.Product.Features.Queries.GetAdminProductById;
using Application.Product.Features.Queries.GetAdminProductDetail;
using Application.Product.Features.Queries.GetAdminProducts;
using MapsterMapper;
using Presentation.Product.Mapping;
using Presentation.Product.Requests;
using SharedKernel.Models;

namespace Presentation.Product.Endpoints;

[Route("api/admin/products")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminProductsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] GetAdminProductsRequest request)
    {
        var query = Mapper
            .Map<GetAdminProductsQuery>(request)
            .Enrich(CurrentUser.UserId);

        var result = await Mediator.Send(query);

        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(
        Guid id,
        [FromBody] GetAdminProductByIdRequest request)
    {
        var query = Mapper
            .Map<GetAdminProductByIdQuery>(request)
            .Enrich(id, CurrentUser.UserId);

        var result = await Mediator.Send(query);

        return ToActionResult(result);
    }

    [HttpGet("/detail/{id:guid}")]
    public async Task<IActionResult> GetProductDetail(
    Guid id,
    [FromBody] GetAdminProductDetailRequest request)
    {
        var query = Mapper
            .Map<GetAdminProductDetailQuery>(request)
            .Enrich(id, CurrentUser.UserId);

        var result = await Mediator.Send(query);

        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var command = Mapper
            .Map<CreateProductCommand>(request)
            .Enrich(CurrentUser.UserId);

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductRequest request)
    {
        var command = Mapper
            .Map<UpdateProductCommand>(request)
            .Enrich(id, CurrentUser.UserId);

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpPut("/bulkupdateprices")]
    public async Task<IActionResult> BulkUpdatePrices(
    [FromBody] BulkUpdatePricesRequest request)
    {
        var command = Mapper
            .Map<BulkUpdatePricesCommand>(request)
            .Enrich(CurrentUser.UserId);

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpPut("/details/{id:guid}")]
    public async Task<IActionResult> UpdateProductDetails(
        Guid id,
        [FromBody] UpdateProductDetailsRequest request)
    {
        var command = Mapper
            .Map<UpdateProductDetailsCommand>(request)
            .Enrich(id, CurrentUser.UserId);

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(
        Guid id,
        [FromBody] DeleteProductRequest request)
    {
        var command = Mapper
            .Map<DeleteProductCommand>(request)
            .Enrich(id, CurrentUser.UserId);

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> ActivateProduct(
        Guid id,
        [FromBody] ActiveProductRequest request)
    {
        var command = Mapper
            .Map<ActivateProductCommand>(request)
            .Enrich(id, CurrentUser.UserId);

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateProduct(
        Guid id,
        [FromBody] DeactiveProductRequest request)
    {
        var command = Mapper
            .Map<DeactivateProductCommand>(request)
            .Enrich(id, CurrentUser.UserId);

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/restore")]
    public async Task<IActionResult> RestoreProduct(
    Guid id,
    [FromBody] RestoreProductRequest request)
    {
        var command = Mapper
            .Map<RestoreProductCommand>(request)
            .Enrich(id, CurrentUser.UserId);

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }
}