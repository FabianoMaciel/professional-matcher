using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProfessionalMatcher.Domain.Exceptions;

namespace ProfessionalMatcher.Api;

/// <summary>Turns validation and conflict exceptions thrown by the handlers into 400 / 409 problem responses.</summary>
[ExcludeFromCodeCoverage]
public class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;
        switch (exception)
        {
            case ValidationException validation:
                problem = new ValidationProblemDetails(
                    validation.Errors
                        .GroupBy(e => ToCamelCase(e.PropertyName))
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                };
                break;
            case UnreadableDocumentException unreadable:
                problem = new ValidationProblemDetails(new Dictionary<string, string[]> { ["file"] = [unreadable.Message] })
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "The document could not be processed.",
                };
                break;
            case ConflictException conflict:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict",
                    Detail = conflict.Message,
                };
                break;
            default:
                return false;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    // "Input.StartDate" -> "input.startDate", matching the JSON property names the client sent.
    private static string ToCamelCase(string path) =>
        string.Join('.', path.Split('.').Select(p => p.Length == 0 ? p : char.ToLowerInvariant(p[0]) + p[1..]));
}
