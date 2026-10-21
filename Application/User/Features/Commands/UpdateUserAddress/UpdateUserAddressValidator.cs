using Application.Common.Validation;

namespace Application.User.Features.Commands.UpdateUserAddress;

public class UpdateUserAddressValidator : AbstractValidator<UpdateUserAddressCommand>
{

    public UpdateUserAddressValidator()
    {
        this.RuleForRequiredId(x => x.AddressId);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ReceiverName).NotEmpty().MaximumLength(100);
        this.RuleForIranianMobileNumber(x => x.PhoneNumber);
        RuleFor(x => x.Province).NotEmpty().MaximumLength(50);
        RuleFor(x => x.City).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PostalCode).NotEmpty().Length(10);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}
