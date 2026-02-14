namespace Application.User.Features.Queries.GetUserWishlist;

public record GetUserWishlistQuery(int UserId, int Page = 1, int PageSize = 20)
    : IRequest<ServiceResult<PaginatedResult<WishlistItemDto>>>;

public class WishlistItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }
    public bool IsInStock { get; set; }
    public string? IconUrl { get; set; }
    public DateTime AddedAt { get; set; }
}