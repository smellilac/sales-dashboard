using Microsoft.AspNetCore.Diagnostics;

namespace SalesDashboard.Api.Shared.Errors;

/// <summary>
/// Last-resort handler for unhandled exceptions (D12): logs via source-generated logging and writes a
/// 500 <c>ProblemDetails</c> through <see cref="IProblemDetailsService"/>. Exception details leak into
/// the response only in Development. A client-aborted request is logged at Debug and produces no 500.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var method = httpContext.Request.Method;
        var path = httpContext.Request.Path.Value ?? string.Empty;

        // Client went away mid-request: not a server fault, and the response can no longer be written.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            LogRequestCanceled(method, path);
            return true;
        }

        LogUnhandledException(exception, method, path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                // Only expose the exception to clients in Development; production sees a generic message.
                Detail = environment.IsDevelopment() ? exception.ToString() : null,
            },
        };

        return await problemDetailsService.TryWriteAsync(context).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception processing {Method} {Path}")]
    private partial void LogUnhandledException(Exception exception, string method, string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Request {Method} {Path} canceled by the client")]
    private partial void LogRequestCanceled(string method, string path);
}
