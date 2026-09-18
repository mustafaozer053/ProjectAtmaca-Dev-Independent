using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Trainings.Cancel;
using ProjectAtmaca.Application.Trainings.GetById;
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
    private readonly GetTrainingByIdQueryHandler _getByIdHandler;

    public TrainingsController(
        CancelTrainingCommandHandler cancelHandler,
        GetTrainingByIdQueryHandler getByIdHandler)
    {
        _cancelHandler = cancelHandler ??
            throw new ArgumentNullException(nameof(cancelHandler));
        _getByIdHandler = getByIdHandler ??
            throw new ArgumentNullException(nameof(getByIdHandler));
    }

    [HttpGet("{trainingId}")]
    public async Task<ActionResult<GetTrainingByIdResponse>> GetById(
        string trainingId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(trainingId, "D", out Guid parsedId) ||
            parsedId == Guid.Empty)
        {
            return ToProblem(InvalidTrainingId);
        }

        Result<TrainingDetails> result = await _getByIdHandler.Handle(
            new GetTrainingByIdQuery(TrainingId.From(parsedId)),
            cancellationToken);

        if (result.IsFailure)
            return ToProblem(result.Error!);

        TrainingDetails details = result.Value!;
        return Ok(new GetTrainingByIdResponse(
            details.Id,
            details.Title,
            details.Description,
            details.Location,
            details.Date,
            details.StartTime,
            details.EndTime,
            GetStatusCode(details.Status),
            details.SeasonId,
            details.OrganizationId));
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

    private static string GetStatusCode(TrainingStatus status) =>
        status switch
        {
            TrainingStatus.Planned => "PLANNED",
            TrainingStatus.Confirmed => "CONFIRMED",
            TrainingStatus.Cancelled => "CANCELLED",
            _ => throw new InvalidOperationException(
                $"Unsupported training status '{status}'.")
        };
}
