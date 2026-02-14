namespace Domain.Media.ValueObjects;

public sealed class FileSize : ValueObject, IComparable<FileSize>
{
    public long Bytes { get; }

    private const long MaxSizeBytes = 100 * 1024 * 1024; // 100MB

    private FileSize(long bytes)
    {
        Bytes = bytes;
    }

    public static FileSize Create(long bytes)
    {
        if (bytes < 0)
            throw new DomainException("حجم فایل نمی‌تواند منفی باشد.");

        if (bytes > MaxSizeBytes)
            throw new DomainException($"حجم فایل نمی‌تواند بیش از {MaxSizeBytes / (1024 * 1024)} مگابایت باشد.");

        return new FileSize(bytes);
    }

    public static FileSize FromKilobytes(double kb) => Create((long)(kb * 1024));

    public static FileSize FromMegabytes(double mb) => Create((long)(mb * 1024 * 1024));

    public static FileSize Zero() => new(0);

    public double ToKilobytes() => Bytes / 1024.0;

    public double ToMegabytes() => Bytes / (1024.0 * 1024.0);

    public string ToDisplayString()
    {
        string[] sizes = { "بایت", "کیلوبایت", "مگابایت", "گیگابایت" };
        double len = Bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    public bool IsEmpty => Bytes == 0;

    public int CompareTo(FileSize? other)
    {
        if (other == null) return 1;
        return Bytes.CompareTo(other.Bytes);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Bytes;
    }

    public override string ToString() => ToDisplayString();

    public static bool operator >(FileSize left, FileSize right) => left.Bytes > right.Bytes;

    public static bool operator <(FileSize left, FileSize right) => left.Bytes < right.Bytes;

    public static bool operator >=(FileSize left, FileSize right) => left.Bytes >= right.Bytes;

    public static bool operator <=(FileSize left, FileSize right) => left.Bytes <= right.Bytes;

    public static implicit operator long(FileSize size) => size.Bytes;
}