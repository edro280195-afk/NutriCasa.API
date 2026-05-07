using System.Net;
using System.Text.Json;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Api.Middleware;

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
        catch (Application.Common.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Error de validación");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            var error = new ApiError { Code = "VALIDATION_ERROR", Message = ex.Message, Details = ex.Errors };
            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
        catch (Application.Common.Exceptions.NotFoundException ex)
        {
            _logger.LogWarning(ex, "Recurso no encontrado");
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.ContentType = "application/json";
            var error = new ApiError { Code = "NOT_FOUND", Message = ex.Message };
            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
        catch (Application.Common.Exceptions.ForbiddenException ex)
        {
            _logger.LogWarning(ex, "Acceso prohibido");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/json";
            var error = new ApiError { Code = "FORBIDDEN", Message = ex.Message };
            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no manejado: {Message}", ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var error = new ApiError { Code = "INTERNAL_ERROR", Message = "Ha ocurrido un error interno. Intenta de nuevo más tarde." };
            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
    }
}
