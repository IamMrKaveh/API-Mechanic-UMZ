namespace Application.Common.Utilities;

public static class FileSizeFormatter
{
    public static string Format(long bytes)
    {
        string[] sizes = { "بایت", "کیلوبایت", "مگابایت", "گیگابایت" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}