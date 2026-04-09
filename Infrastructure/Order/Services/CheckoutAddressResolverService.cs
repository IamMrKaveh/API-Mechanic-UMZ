using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Infrastructure.Order.Services;

public class CheckoutAddressResolverService(IUserRepository userRepository)
    : ICheckoutAddressResolverService
{
    public async Task<ServiceResult<(ReceiverInfo ReceiverInfo, DeliveryAddress DeliveryAddress)>> ResolveAsync(
        Guid userId, Guid addressId, CancellationToken ct)
    {
        var user = await userRepository.GetWithAddressesAsync(UserId.From(userId), ct);
        if (user is null)
            return ServiceResult<(ReceiverInfo, DeliveryAddress)>.NotFound("کاربر یافت نشد.");

        var address = user.Addresses.FirstOrDefault(a => a.Id == UserAddressId.From(addressId));
        if (address is null)
            return ServiceResult<(ReceiverInfo, DeliveryAddress)>.NotFound("آدرس یافت نشد.");

        var receiverInfo = ReceiverInfo.Create(address.ReceiverName, address.PhoneNumber.Value);
        var deliveryAddress = DeliveryAddress.Create(
            address.Province, address.City, address.Address, address.PostalCode);

        return ServiceResult<(ReceiverInfo, DeliveryAddress)>.Success((receiverInfo, deliveryAddress));
    }
}