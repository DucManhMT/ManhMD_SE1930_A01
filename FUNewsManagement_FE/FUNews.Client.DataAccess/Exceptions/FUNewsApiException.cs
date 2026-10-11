using System.Net;
using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.DataAccess.Exceptions;

public class FUNewsApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public ApiProblemDetails? ProblemDetails { get; }
    public IDictionary<string, string[]>? ValidationErrors => ProblemDetails?.Errors;

    public FUNewsApiException(HttpStatusCode statusCode, string message, ApiProblemDetails? problemDetails = null)
        : base(message)
    {
        StatusCode = statusCode;
        ProblemDetails = problemDetails;
    }
}
