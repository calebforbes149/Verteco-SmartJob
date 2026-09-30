using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Domain.Exceptions;

namespace VertecoSmartJob.Api.Middleware;

/// <summary>
/// Single place every unhandled exception passes through. Domain rule violations become the
/// matching 4xx ProblemDetails response; anything else becomes a generic 500 with no stack
/// trace, logged server-side instead.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            InvalidJobStatusTransitionException => (StatusCodes.Status409Conflict, "Invalid job card transition"),
            JobCardModifiedSinceReviewException => (StatusCodes.Status409Conflict, "Job card changed since review"),
            ChecklistNotCompleteException => (StatusCodes.Status422UnprocessableEntity, "Checklist incomplete"),
            ReasonRequiredException => (StatusCodes.Status422UnprocessableEntity, "Reason required"),
            SubmissionInvalidException => (StatusCodes.Status422UnprocessableEntity, "Submission incomplete"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Record changed"),
            Microsoft.EntityFrameworkCore.DbUpdateException => (StatusCodes.Status409Conflict, "Data conflict"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception on {Path}", httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError ? "Please try again or contact support." : exception.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
