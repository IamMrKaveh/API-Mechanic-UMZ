namespace Application.Common.Exceptions;

public class ConcurrencyException(string message = "تغییرات همزمان رخ داده است. لطفاً دوباره تلاش کنید.") : Exception(message);