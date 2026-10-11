namespace FUNews.BusinessLogic.Exceptions;

public class PayloadTooLargeException : Exception
{
    public PayloadTooLargeException(string message) : base(message)
    {
    }
}
