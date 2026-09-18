using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Trainings.Cancel;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Api.Trainings;

[ApiController]
[Route("api/trainings")]
public sealed class TrainingsController : ControllerBase
{
    private static readonly Error InvalidTrainingId =
        Error.Create(
            "Training.Id.Invalid",
            "Training id must be a non-empty GUID in D format.");

    private readonly CancelTrainingCommandHandler _cancelHandler;

    public TrainingsController(CancelTrainingCommandHandler cancelHandler)
    {
        _cancelHandler = cancelHandler ??
            throw new ArgumentNullException(nameof(cancelHandler));
    }

    [HttpPost("{trainingId}/cancel")]
    public async Task<IActionResult> Cancel(
        string trainingId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(trainingId, "D", out Guid parsedId) ||
            parsedId == Guid.Empty)
        {
            return ToProblem(InvalidTrainingId);
        }

        Result result = await _cancelHandler.Handle(
            new CancelTrainingCommand(TrainingId.From(parsedId)),
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Error!);
        }

        return NoContent();
    }

    private ObjectResult ToProblem(Error error)
    {
        int statusCode = error.Code == ActorAuthorizationErrors.Forbidden.Code
            ? StatusCodes.Status403Forbidden
            : error.Code == CancelTrainingErrors.NotFound.Code
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;

        var details = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = error.Message
        };
        details.Extensions["code"] = error.Code;

        ObjectResult result = StatusCode(statusCode, details);
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
