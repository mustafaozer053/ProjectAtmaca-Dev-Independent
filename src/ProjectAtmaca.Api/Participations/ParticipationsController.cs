using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

using ProjectAtmaca.Api.Participations.Create;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations.Create;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Api.Participations;

[ApiController]
[Route("api/participations")]
public sealed class ParticipationsController
    : ControllerBase
{
    private static readonly Error ActivityIdRequired =
        Error.Create(
            "Participation.ActivityId.Required",
            "Activity id is required.");

    private readonly CreateParticipationCommandHandler
        _createParticipationHandler;

    public ParticipationsController(
        CreateParticipationCommandHandler
            createParticipationHandler)
    {
        ArgumentNullException.ThrowIfNull(
            createParticipationHandler);

        _createParticipationHandler =
            createParticipationHandler;
    }

    [HttpPost]
    public async Task<ActionResult<CreateParticipationResponse>>
        Create(
            [FromBody] CreateParticipationRequest request,
            CancellationToken cancellationToken)
    {
        Result<ActivityTypeCode> activityTypeResult =
            ActivityTypeCode.Create(
                request.ActivityTypeCode);

        if (activityTypeResult.IsFailure)
        {
            return ToProblem(
                activityTypeResult.Error!);
        }

        if (request.ActivityId == Guid.Empty)
        {
            return ToProblem(
                ActivityIdRequired);
        }

        if (request.AtmacaCardId == Guid.Empty)
        {
            return ToProblem(
                ParticipationErrors.AtmacaCardRequired);
        }

        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.From(
                    request.ActivityId));

        CreateParticipationCommand command =
            new(
                activityReference,
                AtmacaCardId.From(
                    request.AtmacaCardId));

        Result<ParticipationId> result =
            await _createParticipationHandler.Handle(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(
                result.Error!);
        }

        ParticipationId participationId =
            result.Value!;

        return Created(
            $"/api/participations/" +
            $"{participationId.Value:D}",
            new CreateParticipationResponse(
                participationId.Value));
    }

    private ObjectResult ToProblem(
        Error error)
    {
        int statusCode =
            GetStatusCode(
                error);

        ProblemDetails problemDetails =
            new()
            {
                Status = statusCode,
                Title =
                    ReasonPhrases.GetReasonPhrase(
                        statusCode),
                Detail = error.Message
            };

        problemDetails.Extensions["code"] =
            error.Code;

        ObjectResult result =
            StatusCode(
                statusCode,
                problemDetails);

        result.ContentTypes.Add(
            "application/problem+json");

        return result;
    }

    private static int GetStatusCode(
        Error error)
    {
        if (
            string.Equals(
                error.Code,
                ActorAuthorizationErrors.Forbidden.Code,
                StringComparison.Ordinal)
        )
        {
            return StatusCodes.Status403Forbidden;
        }

        if (
            string.Equals(
                error.Code,
                CreateParticipationErrors.AlreadyExists.Code,
                StringComparison.Ordinal)
        )
        {
            return StatusCodes.Status409Conflict;
        }

        return StatusCodes.Status400BadRequest;
    }
}
