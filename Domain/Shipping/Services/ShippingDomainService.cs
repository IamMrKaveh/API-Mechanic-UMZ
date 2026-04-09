using Domain.Shipping.Results;
using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Services;

public sealed class ShippingDomainService
{
    public ShippingCostCalculationResult CalculateShippingCost(
        Aggregates.Shipping shipping,
        Money orderTotal,
        IEnumerable<ShippingCostItem>? items = null)
    {
        Guard.Against.Null(shipping, nameof(shipping));
        Guard.Against.Null(orderTotal, nameof(orderTotal));

        if (!shipping.IsActive)
            return ShippingCostCalculationResult.NotAvailable(shipping.Id, "روش ارسال غیرفعال است.");

        var validationResult = shipping.ValidateForOrder(orderTotal);
        if (validationResult.IsFailure)
            return ShippingCostCalculationResult.NotAvailable(shipping.Id, validationResult.Error.Message!);

        Money cost;
        bool isFree = shipping.QualifiesForFreeShipping(orderTotal);

        if (items is not null)
        {
            var itemList = items.ToList();
            if (itemList.Count > 0)
            {
                cost = shipping.CalculateCostForCart(orderTotal, itemList);
            }
            else
            {
                cost = shipping.CalculateCost(orderTotal);
            }
        }
        else
        {
            cost = shipping.CalculateCost(orderTotal);
        }

        return ShippingCostCalculationResult.Success(
            shipping.Id,
            cost,
            isFree,
            shipping.GetDeliveryTimeDisplay());
    }

    public Result SetDefaultShipping(
        Aggregates.Shipping newDefault,
        IEnumerable<Aggregates.Shipping> allShippings)
    {
        Guard.Against.Null(newDefault, nameof(newDefault));
        Guard.Against.Null(allShippings, nameof(allShippings));

        if (!newDefault.IsActive)
            return Result.Failure(new Error(
                "400",
                "امکان تنظیم روش ارسال غیرفعال به عنوان پیش‌فرض وجود ندارد.",
                ErrorType.Forbidden));

        foreach (var shipping in allShippings)
        {
            if (shipping.IsDefault && shipping.Id != newDefault.Id)
                shipping.UnsetDefault();
        }

        newDefault.SetAsDefault();

        return Result.Success();
    }

    public ShippingSelectionResult SelectBestShipping(
        IEnumerable<Aggregates.Shipping> availableShippings,
        Money orderTotal)
    {
        Guard.Against.Null(availableShippings, nameof(availableShippings));
        Guard.Against.Null(orderTotal, nameof(orderTotal));

        var eligible = availableShippings
            .Where(s => s.IsActive && s.IsAvailableForOrder(orderTotal))
            .OrderBy(s => s.SortOrder)
            .ToList();

        if (eligible.Count == 0)
            return ShippingSelectionResult.NoAvailableShipping();

        var defaultShipping = eligible.FirstOrDefault(s => s.IsDefault);
        if (defaultShipping is not null)
            return ShippingSelectionResult.Selected(defaultShipping);

        var freeShipping = eligible.FirstOrDefault(s => s.QualifiesForFreeShipping(orderTotal));
        if (freeShipping is not null)
            return ShippingSelectionResult.Selected(freeShipping);

        var cheapest = eligible
            .OrderBy(s => s.CalculateCost(orderTotal).Amount)
            .First();

        return ShippingSelectionResult.Selected(cheapest);
    }

    public IReadOnlyList<ShippingAvailability> GetAvailableShippingsForOrder(
        IEnumerable<Aggregates.Shipping> allShippings,
        Money orderTotal)
    {
        Guard.Against.Null(allShippings, nameof(allShippings));
        Guard.Against.Null(orderTotal, nameof(orderTotal));

        var result = new List<ShippingAvailability>();

        foreach (var shipping in allShippings.Where(s => s.IsActive))
        {
            var isAvailable = shipping.IsAvailableForOrder(orderTotal);
            var cost = isAvailable ? shipping.CalculateCost(orderTotal) : Money.Zero();
            var isFree = isAvailable && shipping.QualifiesForFreeShipping(orderTotal);
            string? unavailableReason = null;

            if (!isAvailable)
            {
                var validation = shipping.ValidateForOrder(orderTotal);
                unavailableReason = validation.Error.Message;
            }

            result.Add(new ShippingAvailability(
                shipping.Id,
                shipping.Name.Value,
                shipping.Description,
                cost,
                isFree,
                isAvailable,
                shipping.IsDefault,
                shipping.GetDeliveryTimeDisplay(),
                unavailableReason));
        }

        return result
            .OrderByDescending(s => s.IsDefault)
            .ThenByDescending(s => s.IsFree)
            .ThenBy(s => s.Cost.Amount)
            .ToList()
            .AsReadOnly();
    }

    public void ReorderShippings(
        IReadOnlyList<Aggregates.Shipping> shippings,
        IReadOnlyList<ShippingId> orderedIds)
    {
        Guard.Against.Null(shippings, nameof(shippings));
        Guard.Against.Empty(orderedIds, nameof(orderedIds));

        var shippingDict = shippings
            .Where(s => s.IsActive)
            .ToDictionary(s => s.Id);

        var activeIdSet = new HashSet<ShippingId>(shippingDict.Keys);
        var providedIdSet = new HashSet<ShippingId>(orderedIds);

        var invalidIds = providedIdSet.Except(activeIdSet).ToList();
        if (invalidIds.Count > 0)
            throw new DomainException($"شناسه‌های روش ارسال نامعتبر: {string.Join(", ", invalidIds)}");

        var missingIds = activeIdSet.Except(providedIdSet).ToList();
        if (missingIds.Count > 0)
            throw new DomainException($"تمام روش‌های ارسال فعال باید در لیست مرتب‌سازی وجود داشته باشند.");

        if (orderedIds.Distinct().Count() != orderedIds.Count)
            throw new DomainException("لیست مرتب‌سازی شامل شناسه‌های تکراری است.");

        for (int i = 0; i < orderedIds.Count; i++)
        {
            shippingDict[orderedIds[i]].SetSortOrder(i);
        }
    }
}