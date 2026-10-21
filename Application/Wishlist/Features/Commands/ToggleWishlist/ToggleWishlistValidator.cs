using Application.Common.Validation;

namespace Application.Wishlist.Features.Commands.ToggleWishlist;

public class ToggleWishlistValidator : AbstractValidator<ToggleWishlistCommand>
{

    public ToggleWishlistValidator()
    {
        this.RuleForRequiredId(x => x.ProductId);
    }
}
