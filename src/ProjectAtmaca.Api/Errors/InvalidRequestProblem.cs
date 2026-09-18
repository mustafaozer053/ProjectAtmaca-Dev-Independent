using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ProjectAtmaca.Api.Errors;

internal static class InvalidRequestProblem
{
    public static IActionResult Create(ActionContext context)
    {
        // Preserve field paths, never serialize parser exceptions or internal CLR names.
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(entry => entry.Key,
                _ => new[] { "The supplied value is missing or invalid." });
        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Detail = "The request could not be read or validated. Check the indicated fields."
        };
        problem.Extensions["code"] = "Api.Request.Invalid";
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        var response = new BadRequestObjectResult(problem);
        response.ContentTypes.Add("application/problem+json");
        return response;
    }
}
