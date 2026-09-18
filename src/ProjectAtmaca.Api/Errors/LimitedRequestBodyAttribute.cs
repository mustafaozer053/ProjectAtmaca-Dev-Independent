using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProjectAtmaca.Api.Errors;

/// <summary>Bounds buffering before model binding, including bodies without Content-Length.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LimitedRequestBodyAttribute(int maximumBytes) : Attribute, IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (request.ContentLength > maximumBytes)
        {
            context.Result = TooLarge(context.HttpContext);
            return;
        }

        var original = request.Body;
        using var boundedBody = new MemoryStream();
        var buffer = new byte[8192];
        while (boundedBody.Length <= maximumBytes)
        {
            int read = await original.ReadAsync(buffer.AsMemory(0,
                (int)Math.Min(buffer.Length, maximumBytes + 1L - boundedBody.Length)), context.HttpContext.RequestAborted);
            if (read == 0) break;
            boundedBody.Write(buffer, 0, read);
        }
        if (boundedBody.Length > maximumBytes)
        {
            context.Result = TooLarge(context.HttpContext);
            return;
        }

        boundedBody.Position = 0;
        request.Body = boundedBody;
        try { await next(); }
        finally { request.Body = original; }
    }

    private static ObjectResult TooLarge(HttpContext context)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status413PayloadTooLarge,
            Title = "Payload Too Large",
            Detail = "The request body exceeds the permitted size."
        };
        problem.Extensions["code"] = "Api.Request.TooLarge";
        problem.Extensions["traceId"] = context.TraceIdentifier;
        var result = new ObjectResult(problem) { StatusCode = problem.Status };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
