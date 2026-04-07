using Domain.Variant.ValueObjects;
using SharedKernel.Results;

namespace Domain.Inventory.Services.Results;

public sealed class ReservationResult
{
    public bool IsSuccess { get; }
    public VariantId VariantId { get; }
    public int ReservedQuantity { get; }
    public bool IsUnlimited { get; }
    public string? Error { get; }
    public int? AvailableStock { get; }
    public int? RequestedQuantity { get; }

    private ReservationResult(
        bool isSuccess,
        VariantId variantId,
        int reservedQuantity = 0,
        bool isUnlimited = false,
        string? error = null,
        int? availableStock = null,
        int? requestedQuantity = null)
    {
        IsSuccess = isSuccess;
        VariantId = variantId;
        ReservedQuantity = reservedQuantity;
        IsUnlimited = isUnlimited;
        Error = error;
        AvailableStock = availableStock;
        RequestedQuantity = requestedQuantity;
    }

    public static ReservationResult Success(VariantId variantId, int quantity, bool isUnlimited = false)
        => new(true, variantId, quantity, isUnlimited);

    public static ReservationResult InsufficientStock(VariantId variantId, int available, int requested)
        => new(false, variantId, error: $"موجودی کافی نیست. موجودی: {available}، درخواستی: {requested}",
            availableStock: available, requestedQuantity: requested);

    public static ReservationResult Failed(VariantId variantId, string error)
        => new(false, variantId, error: error);

    public int GetShortage() => RequestedQuantity.HasValue && AvailableStock.HasValue
        ? Math.Max(0, RequestedQuantity.Value - AvailableStock.Value) : 0;

    public Result<ReservationResult> ToResult() => IsSuccess
        ? Result<ReservationResult>.Success(this)
        : Result<ReservationResult>.Failure(new SharedKernel.Results.Error("Reservation.Failed", Error ?? string.Empty));
}