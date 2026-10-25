using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MomoPayment.Common.DTOs;
using MomoPayment.Common.Exceptions;

namespace MomoPayment.Common.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);

        var response = context.Response;
        response.ContentType = "application/json";

        var apiResponse = new ApiResponse<object>
        {
            Success = false,
            Timestamp = DateTime.UtcNow
        };

        switch (ex)
        {
            case AppException appEx:
                response.StatusCode = appEx.StatusCode;
                apiResponse.Message = appEx.Message;
                apiResponse.ErrorCode = appEx.ErrorCode;
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse.Message = "Truy cập bị từ chối: không có quyền thực hiện thao tác này";
                apiResponse.ErrorCode = "UNAUTHORIZED";
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Message = "Đã xảy ra lỗi nội bộ máy chủ. Vui lòng thử lại sau.";
                apiResponse.ErrorCode = "INTERNAL_SERVER_ERROR";
                break;
        }

        var json = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(json);
    }
}
