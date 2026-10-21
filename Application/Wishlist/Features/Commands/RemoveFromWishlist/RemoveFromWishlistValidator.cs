using Application.Common.Validation;

namespace Application.Wishlist.Features.Commands.RemoveFromWishlist;

public class RemoveFromWishlistValidator : AbstractValidator<RemoveFromWishlistCommand>
{

    public RemoveFromWishlistValidator()
    {
        this.RuleForRequiredId(x => x.ProductId);
    }
}
