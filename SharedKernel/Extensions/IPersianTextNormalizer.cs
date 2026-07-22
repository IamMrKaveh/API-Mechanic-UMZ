namespace SharedKernel.Extensions;

public interface IPersianTextNormalizer
{
    string Normalize(string? text);

    string NormalizeDigitsToLatin(string? text);

    string RemoveDiacritics(string? text);

    string CollapseZeroWidthNonJoiner(string? text);
}
