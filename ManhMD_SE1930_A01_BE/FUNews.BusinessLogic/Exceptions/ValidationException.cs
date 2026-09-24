namespace FUNews.BusinessLogic.Exceptions;

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IDictionary<string, string[]> errors) : base(message)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public ValidationException(string propertyName, string errorMessage)
        : base("Một hoặc nhiều lỗi xác thực đã xảy ra.")
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { errorMessage } }
        };
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("Một hoặc nhiều lỗi xác thực đã xảy ra.")
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }
}
