namespace SharedKernel.Guard;

public static class ContractGuard
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

        public static void Negative(decimal value, string paramName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(paramName, $"{paramName} cannot be negative.");
        }

        public static void Empty<T>(IEnumerable<T> value, string paramName)
        {
            if (value is null || !value.Any())
                throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);
        }

        public static void OutOfRange(int value, int min, int max, string paramName)
        {
            if (value < min || value > max)
                throw new ArgumentOutOfRangeException(paramName, $"{paramName} must be between {min} and {max}.");
        }

        public static void LengthExceeds(string value, int maxLength, string paramName)
        {
            if (value is not null && value.Length > maxLength)
                throw new ArgumentException($"{paramName} length must be at most {maxLength}.", paramName);
        }

        public static void False(bool condition, string paramName, string message)
        {
            if (!condition)
                throw new ArgumentException(message, paramName);
        }

        public static void True(bool condition, string paramName, string message)
        {
            if (condition)
                throw new ArgumentException(message, paramName);
        }
    }
}
