using Microsoft.AspNetCore.Diagnostics;

namespace ProjectAtmaca.Api.Errors;

internal static class UnexpectedRequestProblem
{
    internal static Task WriteAsync(HttpContext context)
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        int status = exception is BadHttpRequestException badRequest
            ? badRequest.StatusCode : StatusCodes.Status500InternalServerError;
        string code = status switch
        {
            413 => "Api.Request.TooLarge",
            400 => "Api.Request.Invalid",
            _ => "Api.UnexpectedError"
        };
        // Never use exception messages, request data or stack traces in the public response.
        return Results.Problem(statusCode: status,
            title: Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(status),
            detail: status == 500 ? "The request could not be completed." : "The request could not be accepted.",
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code,
                ["traceId"] = context.TraceIdentifier
            }).ExecuteAsync(context);
    }
}
