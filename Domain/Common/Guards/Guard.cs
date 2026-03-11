namespace Domain.Common.Guards;

public static class Guard
{
    public static class Against
    {
        public static void Null<T>(T value, string paramName) where T : class
        {
            if (value is null)
                throw new ArgumentNullException(paramName);
        }

        public static void NullOrWhiteSpace(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{paramName} cannot be null or whitespace.", paramName);
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

        public static void Empty<T>(IEnumerable<T> value, string paramName)
        {
            if (value is null || !value.Any())
                throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);
        }
    }
}