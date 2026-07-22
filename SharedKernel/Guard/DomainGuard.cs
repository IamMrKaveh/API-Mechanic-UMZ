using SharedKernel.Exceptions;

namespace SharedKernel.Guard;

public static class DomainGuard
{
    public static class Against
    {
        public static void Null<T>(T value, string message) where T : class
        {
            if (value is null)
                throw new DomainException(message);
        }

        public static void NullOrWhiteSpace(string value, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException(message);
        }

        public static void NegativeOrZero(int value, string message)
        {
            if (value <= 0)
                throw new DomainException(message);
        }

        public static void NegativeOrZero(decimal value, string message)
        {
            if (value <= 0)
                throw new DomainException(message);
        }

        public static void Negative(int value, string message)
        {
            if (value < 0)
                throw new DomainException(message);
        }

        public static void Negative(decimal value, string message)
        {
            if (value < 0)
                throw new DomainException(message);
        }

        public static void Empty<T>(IEnumerable<T> value, string message)
        {
            if (value is null || !value.Any())
                throw new DomainException(message);
        }

        public static void OutOfRange(int value, int min, int max, string message)
        {
            if (value < min || value > max)
                throw new DomainException(message);
        }

        public static void OutOfRange(decimal value, decimal min, decimal max, string message)
        {
            if (value < min || value > max)
                throw new DomainException(message);
        }

        public static void LengthExceeds(string value, int maxLength, string message)
        {
            if (value is not null && value.Length > maxLength)
                throw new DomainException(message);
        }

        public static void False(bool condition, string message)
        {
            if (!condition)
                throw new DomainException(message);
        }

        public static void True(bool condition, string message)
        {
            if (condition)
                throw new DomainException(message);
        }
    }
}
