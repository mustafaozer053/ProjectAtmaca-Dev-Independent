using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Api.Persons;

[ApiController]
[Route("api/person-registrations")]
public sealed class PersonRegistrationsController(RegisterPersonWithAtmacaCard handler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterPersonRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(request.OperationId, "D", out var operationId) || operationId == Guid.Empty)
            return ToProblem(PersonRegistrationOperationErrors.OperationRequired, 400);
        var input = request.Map();
        if (input.IsFailure) return ToProblem(input.Error!, 400);
        var result = await handler.Handle(operationId, input.Value!, cancellationToken);
        if (result.IsSuccess) return Ok(result.Value);
        var error = result.Error!;
        int status = error.Code switch
        {
            var code when code == ActorAuthorizationErrors.Forbidden.Code => 403,
            var code when code == ActorIdentityResolutionErrors.NotMapped.Code => 403,
            var code when code == PersonRegistrationOperationErrors.OperationConflict.Code
                || code == PersonRegistrationOperationErrors.IdentityAlreadyRegistered.Code
                || code == PersonRegistrationOperationErrors.PassportDuplicateConfirmationRequired.Code
                || code == PersonRegistrationOperationErrors.CardNumberCapacity.Code => 409,
            _ => 400
        };
        return ToProblem(error, status);
    }

    private ObjectResult ToProblem(Error error, int status)
    {
        var problem = new ProblemDetails { Status = status, Title = ReasonPhrases.GetReasonPhrase(status), Detail = error.Message };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        var response = StatusCode(status, problem);
        response.ContentTypes.Add("application/problem+json");
        return response;
    }
}
