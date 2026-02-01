using Application.Common.Interfaces.Product;
using Application.Common.Interfaces.User;
using Application.DTOs.Product;
using MainApi.Controllers.Base;

namespace MainApi.Controllers.Product;

[Route("api/[controller]")]
public class ProductsController : BaseApiController
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService, ICurrentUserService currentUserService) : base(currentUserService)
    {
        _productService = productService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts([FromQuery] ProductSearchDto searchDto)
    {
        var result = await _productService.GetProductsAsync(searchDto);
        return ToActionResult(result);
    }

    [HttpGet("attributes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAttributes()
    {
        var result = await _productService.GetAllAttributesAsync();
        return ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductById(int id)
    {
        var result = await _productService.GetProductByIdAsync(id);
        return ToActionResult(result);
    }
}