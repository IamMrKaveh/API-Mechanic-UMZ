using Domain.Brand.ValueObjects;
using SharedKernel.Exceptions;

namespace Domain.Brand.Exceptions;

public sealed class BrandNameAlreadyExistsException(BrandName name)
    : AlreadyExistsException<BrandName>(
        "BRAND_NAME_ALREADY_EXISTS",
        name,
        $"برند با نام '{name}' قبلاً وجود دارد.")
{
    public BrandName Name => Value;
}

public sealed class BrandAlreadyActiveException(BrandId brandId)
    : ConflictException<BrandId>(
        "BRAND_ALREADY_ACTIVE",
        brandId,
        $"برند با شناسه {brandId} در حال حاضر فعال است.")
{
    public BrandId BrandId => Value;
}

public sealed class BrandAlreadyDeactivatedException(BrandId brandId)
    : ConflictException<BrandId>(
        "BRAND_ALREADY_DEACTIVATED",
        brandId,
        $"برند با شناسه {brandId} در حال حاضر غیرفعال است.")
{
    public BrandId BrandId => Value;
}
