namespace FUNews.BusinessLogic.Exceptions;

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Yêu cầu xác thực để tiếp tục.") : base(message)
    {
    }
}
