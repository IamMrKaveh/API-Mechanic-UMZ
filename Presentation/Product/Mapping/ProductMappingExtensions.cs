using Application.Product.Features.Commands.ActivateProduct;
using Application.Product.Features.Commands.BulkUpdatePrices;
using Application.Product.Features.Commands.ChangePrice;
using Application.Product.Features.Commands.CreateProduct;
using Application.Product.Features.Commands.DeactivateProduct;
using Application.Product.Features.Commands.DeleteProduct;
using Application.Product.Features.Commands.RestoreProduct;
using Application.Product.Features.Commands.UpdateProduct;
using Application.Product.Features.Commands.UpdateProductDetails;
using Application.Product.Features.Queries.GetAdminProductById;
using Application.Product.Features.Queries.GetAdminProducts;
using Application.Product.Features.Queries.GetProduct;
using Application.Product.Features.Queries.GetProductDetails;

namespace Presentation.Product.Mapping;

public static class ProductMappingExtensions
{
    public static ActivateProductCommand Enrich(
        this ActivateProductCommand command,
        Guid productId,
        Guid userId) => command with
        {
            ProductId = productId,
            ActivatedByUserId = userId,
        };

    public static BulkUpdatePricesCommand Enrich(
        this BulkUpdatePricesCommand command,
        Guid userId) => command with
        {
            UserId = userId
        };

    public static ChangePriceCommand Enrich(
        this ChangePriceCommand command,
        Guid productId,
        Guid userId) => command with
        {
            ProductId = productId,
            UserId = userId
        };

    public static CreateProductCommand Enrich(
        this CreateProductCommand command,
        Guid userId) => command with
        {
            CreatedByUserId = userId
        };

    public static DeactivateProductCommand Enrich(
        this DeactivateProductCommand command,
        Guid productId,
        Guid userId) => command with
        {
            ProductId = productId,
            DeactivatedByUserId = userId
        };

    public static DeleteProductCommand Enrich(
        this DeleteProductCommand command,
        Guid productId,
        Guid userId) => command with
        {
            ProductId = productId,
            DeletedByUserId = userId
        };

    public static RestoreProductCommand Enrich(
        this RestoreProductCommand command,
        Guid productId,
        Guid userId) => command with
        {
            ProductId = productId,
            UserId = userId
        };

    public static GetAdminProductsQuery Enrich(
        this GetAdminProductsQuery query,
        Guid adminId) => query with
        {
            AdminId = adminId
        };

    public static GetAdminProductByIdQuery Enrich(
        this GetAdminProductByIdQuery query,
        Guid productId,
        Guid userId) => query with
        {
            ProductId = productId,
            UserId = userId
        };

    public static GetProductQuery Enrich(
        this GetProductQuery query,
        Guid productId) => query with
        {
            Id = productId
        };

    public static GetProductDetailsQuery Enrich(
        this GetProductDetailsQuery query,
        Guid productId) => query with
        {
            ProductId = productId
        };

    public static UpdateProductCommand Enrich(
        this UpdateProductCommand command,
        Guid productId,
        Guid userId) => command with
        {
            Id = productId,
            UpdatedByUserId = userId
        };

    public static UpdateProductDetailsCommand Enrich(
        this UpdateProductDetailsCommand command,
        Guid productId,
        Guid userId) => command with
        {
            ProductId = productId,
            UserId = userId
        };
}