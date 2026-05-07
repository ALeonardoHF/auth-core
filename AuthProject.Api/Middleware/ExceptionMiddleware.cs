using Microsoft.AspNetCore.Mvc;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
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
        var statusCode = ex switch
        {
            DomainException       => 400,
            NotFoundException     => 404,
            UnauthorizedException => 401,
            _                     => 500
        };

        var title = statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            404 => "Not Found",
            _   => "Internal Server Error"
        };

        var detail = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title  = title,
            Detail = detail
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode  = statusCode;
        await context.Response.WriteAsJsonAsync(problem);
    }
}