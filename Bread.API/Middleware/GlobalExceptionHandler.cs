using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Bread.API.Middleware
{
    public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

            //logging exception
            logger.LogError(exception, "An Unhandled exception Occured while processing Request." +
                " TraceId: {traceId}, Path: {Path}, Method: {Method}",
                traceId,
                httpContext.Request.Path,
                httpContext.Request.Method);

            ProblemDetails problemDetails = new ProblemDetails
            {
                Title = "An Error Occured Whilte proccesing Your Request.",
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
                Instance = httpContext.Request.Path,
                Detail = httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                    ? exception.Message
                    : "An Unexpected Error Ocurred.Please Try Again later."
            };

            problemDetails.Extensions["traceId"] = traceId;

            if (httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
            {
                problemDetails.Extensions["exceptionType"] = exception.GetType().Name;

                problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            }

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails,cancellationToken);

            return true;
        }
    }
}
