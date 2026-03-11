using Application.Cart;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public sealed class CheckoutOrchestrationService(
    IOrderRepository orderRepository,
    ICartRepository cartRepository,
    ICheckoutAddressResolverService addressResolver,
    ICheckoutShippingValidatorService shippingValidator,
    ICheckoutCartItemBuilderService cartItemBuilder,
    ICheckoutPriceValidatorService priceValidator,
    ICheckoutStockValidatorService stockValidator,
    ICheckoutOrderCreationService orderCreationService,
    ICheckoutDiscountApplicatorService discountApplicator,
    ICheckoutPaymentProcessorService paymentProcessor,
    IUnitOfWork unitOfWork,
    ILogger<CheckoutOrchestrationService> logger) : ICheckoutOrchestrationService
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly ICheckoutAddressResolverService _addressResolver = addressResolver;
    private readonly ICheckoutShippingValidatorService _shippingValidator = shippingValidator;
    private readonly ICheckoutCartItemBuilderService _cartItemBuilder = cartItemBuilder;
    private readonly ICheckoutPriceValidatorService _priceValidator = priceValidator;
    private readonly ICheckoutStockValidatorService _stockValidator = stockValidator;
    private readonly ICheckoutOrderCreationService _orderCreationService = orderCreationService;
    private readonly ICheckoutDiscountApplicatorService _discountApplicator = discountApplicator;
    private readonly ICheckoutPaymentProcessorService _paymentProcessor = paymentProcessor;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<CheckoutOrchestrationService> _logger = logger;

    public async Task<ServiceResult<CheckoutResultDto>> OrchestrateAsync(
        CheckoutFromCartCommand command,
        CancellationToken ct)
    {
        if (await _orderRepository.ExistsByIdempotencyKeyAsync(command.IdempotencyKey, ct))
        {
            var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(
                command.IdempotencyKey, command.UserId, ct);

            if (existingOrder != null)
            {
                _logger.LogInformation(
                    "Duplicate checkout request for idempotency key: {Key}", command.IdempotencyKey);
                return ServiceResult<CheckoutResultDto>.Success(
                    new CheckoutResultDto(existingOrder.Id, null, null, "Order already exists for this request", false));
            }
        }

        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            try
            {
                var cart = await _cartRepository.GetByUserIdAsync(command.UserId, ct);
                if (cart == null || !cart.CartItems.Any())
                    return ServiceResult<CheckoutResultDto>.Failure("سبد خرید خالی است.");

                var addressResult = await _addressResolver.ResolveAsync(
                    command.UserId, command.UserAddressId, command.NewAddress, command.SaveNewAddress, ct);
                if (addressResult.IsFailed)
                    return ServiceResult<CheckoutResultDto>.Failure(addressResult.Error!);

                var shippingResult = await _shippingValidator.ValidateAsync(command.ShippingId, ct);
                if (shippingResult.IsFailed)
                    return ServiceResult<CheckoutResultDto>.Failure(shippingResult.Error!);

                var cartItemsResult = await _cartItemBuilder.BuildAsync(cart, ct);
                if (cartItemsResult.IsFailed)
                    return ServiceResult<CheckoutResultDto>.Failure(cartItemsResult.Error!);

                var priceValidation = _priceValidator.Validate(
                    cartItemsResult.Value!.VariantItems, command.ExpectedItems);
                if (priceValidation.IsFailed)
                    return ServiceResult<CheckoutResultDto>.Failure(priceValidation.Error!);

                var stockValidation = _stockValidator.Validate(cartItemsResult.Value!.VariantItems);
                if (stockValidation.IsFailed)
                    return ServiceResult<CheckoutResultDto>.Failure(stockValidation.Error!);

                var orderResult = await _orderCreationService.CreateAsync(
                    command.UserId,
                    addressResult.Value!,
                    shippingResult.Value!,
                    command.IdempotencyKey,
                    cartItemsResult.Value!.OrderItemSnapshots,
                    ct);
                if (orderResult.IsFailed)
                    return ServiceResult<CheckoutResultDto>.Failure(orderResult.Error!);

                var discountResult = await _discountApplicator.ApplyAsync(
                    orderResult.Value!, command.DiscountCode, command.UserId, ct);
                if (discountResult.IsFailed)
                    return ServiceResult<CheckoutResultDto>.Failure(discountResult.Error!);

                return await _paymentProcessor.ProcessAsync(
                    orderResult.Value!,
                    command.UserId,
                    command.GatewayName,
                    command.CallbackUrl,
                    command.IdempotencyKey,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout for user {UserId}", command.UserId);
                return ServiceResult<CheckoutResultDto>.Failure("خطایی در فرآیند سفارش رخ داد.");
            }
        }, ct);
    }
}