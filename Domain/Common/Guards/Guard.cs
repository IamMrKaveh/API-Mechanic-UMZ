namespace Domain.Common.Gaurd;

/// <summary>
/// Guard Clauses ساده
/// </summary>
public static class Guard
{
    public static class Against
    {
        public static void Null<T>(T value, string paramName) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(paramName);
        }

        public static void NullOrEmpty(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);
        }

        public static void NegativeOrZero(int value, string paramName)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(paramName, $"{paramName} must be greater than zero.");
        }

        public static void NegativeOrZero(decimal value, string paramName)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(paramName, $"{paramName} must be greater than zero.");
        }

        public static void Negative(int value, string paramName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(paramName, $"{paramName} cannot be negative.");
        }

        internal static void NullOrWhiteSpace(string receiverName, string v)
        {
            if (string.IsNullOrWhiteSpace(receiverName))
                throw new ArgumentException($"{v} cannot be null or whitespace.", v);
        }

        public static void Empty<T>(IEnumerable<T> value, string paramName)
        {
            if (value == null || !value.Any())
                throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);
        }
    }
}