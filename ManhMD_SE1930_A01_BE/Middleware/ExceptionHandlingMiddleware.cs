using System.Diagnostics;
using System.Text.Json;
using FUNews.BusinessLogic.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData;

namespace ManhMD_SE1930_A01_BE.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var statusCode = StatusCodes.Status500InternalServerError;
        var title = "Lỗi hệ thống";
        var detail = "Đã xảy ra lỗi không mong muốn trên máy chủ.";
        IDictionary<string, string[]>? errors = null;

        switch (exception)
        {
            case ValidationException valEx:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Dữ liệu không hợp lệ";
                detail = valEx.Message;
                errors = valEx.Errors;
                _logger.LogInformation("Validation failure: {Message}", valEx.Message);
                break;

            case ODataException odataEx:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Truy vấn OData không hợp lệ";
                detail = odataEx.Message;
                _logger.LogInformation("OData query failure: {Message}", odataEx.Message);
                break;

            case ArgumentException argEx:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Tham số không hợp lệ";
                detail = argEx.Message;
                _logger.LogInformation("Argument failure: {Message}", argEx.Message);
                break;

            case NotFoundException notFoundEx:
                statusCode = StatusCodes.Status404NotFound;
                title = "Không tìm thấy tài nguyên";
                detail = notFoundEx.Message;
                _logger.LogInformation("Resource not found: {Message}", notFoundEx.Message);
                break;

            case ConflictException conflictEx:
                statusCode = StatusCodes.Status409Conflict;
                title = "Xung đột dữ liệu";
                detail = conflictEx.Message;
                _logger.LogWarning("Conflict: {Message}", conflictEx.Message);
                break;

            case DbUpdateException dbEx:
                statusCode = StatusCodes.Status409Conflict;
                title = "Xung đột ràng buộc dữ liệu";
                detail = "Thao tác không thể hoàn tất do xung đột dữ liệu hoặc ràng buộc khóa ngoại.";
                _logger.LogWarning(dbEx, "Database update constraint failure");
                break;

            case ForbiddenException forbiddenEx:
                statusCode = StatusCodes.Status403Forbidden;
                title = "Không có quyền truy cập";
                detail = forbiddenEx.Message;
                _logger.LogWarning("Forbidden access: {Message}", forbiddenEx.Message);
                break;

            case UnauthorizedException unauthEx:
                statusCode = StatusCodes.Status401Unauthorized;
                title = "Yêu cầu xác thực";
                detail = unauthEx.Message;
                _logger.LogInformation("Unauthorized access: {Message}", unauthEx.Message);
                break;

            default:
                _logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}", traceId);
                break;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        object problemResponse;
        if (errors != null && errors.Count > 0)
        {
            var validationProblem = new HttpValidationProblemDetails(errors)
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };
            validationProblem.Extensions["traceId"] = traceId;
            problemResponse = validationProblem;
        }
        else
        {
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };
            problemDetails.Extensions["traceId"] = traceId;
            problemResponse = problemDetails;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        var json = JsonSerializer.Serialize(problemResponse, jsonOptions);
        await context.Response.WriteAsync(json);
    }
}
