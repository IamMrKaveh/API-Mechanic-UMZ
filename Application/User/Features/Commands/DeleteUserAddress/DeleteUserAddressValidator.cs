using Application.Common.Validation;

namespace Application.User.Features.Commands.DeleteUserAddress;

public class DeleteUserAddressValidator : AbstractValidator<DeleteUserAddressCommand>
{

    public DeleteUserAddressValidator()
    {
        this.RuleForRequiredId(x => x.AddressId);
    }
}
