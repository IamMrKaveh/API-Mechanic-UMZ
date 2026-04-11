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

namespace Presentation.Product.Mapping;

public static class AdminProductMappingExtensions
{
    public static GetAdminProductsQuery Enrich(
        this GetAdminProductsQuery query,
        Guid adminId) => query with
        {
            UserId = adminId
        };

    public static GetAdminProductByIdQuery Enrich(
        this GetAdminProductByIdQuery query,
        Guid productId,
        Guid userId) => query with
        {
            ProductId = productId,
            UserId = userId
        };

    public static GetAdminProductDetailQuery Enrich(
        this GetAdminProductDetailQuery query,
        Guid productId,
        Guid userId) => query with
        {
            ProductId = productId,
            UserId = userId
        };

    public static CreateProductCommand Enrich(
        this CreateProductCommand command,
        Guid userId) => command with
        {
            UserId = userId
        };

    public static UpdateProductCommand Enrich(
        this UpdateProductCommand command,
        Guid productId,
        Guid userId) => command with
        {
            Id = productId,
            UserId = userId
        };

    public static UpdateProductDetailsCommand Enrich(
        this UpdateProductDetailsCommand command,
        Guid productId,
        Guid userId) => command with
        {
            ProductId = productId,
            UserId = userId
        };

    public static BulkUpdatePricesCommand Enrich(
        this BulkUpdatePricesCommand command,
        Guid userId) => command with
        {
            UserId = userId
        };

    public static DeleteProductCommand Enrich(
        this DeleteProductCommand command,
        Guid productId,
        Guid userId) => command with
        {
            ProductId = productId,
            UserId = userId
        };

    public static ActivateProductCommand Enrich(
        this ActivateProductCommand command,
        Guid productId,
        Guid userId) => command with
        {
            ProductId = productId,
            UserId = userId,
        };

    public static DeactivateProductCommand Enrich(
        this DeactivateProductCommand command,
        Guid productId,
        Guid userId) => command with
        {
            ProductId = productId,
            UserId = userId
        };

    public static RestoreProductCommand Enrich(
        this RestoreProductCommand command,
        Guid productId,
        Guid userId) => command with
        {
            ProductId = productId,
            UserId = userId
        };
}