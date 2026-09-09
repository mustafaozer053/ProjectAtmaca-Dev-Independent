using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

using ProjectAtmaca.Api.Participations.Create;
using ProjectAtmaca.Api.Participations.GetById;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations.Create;
using ProjectAtmaca.Application.Participations.GetById;
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

    private readonly GetParticipationByIdQueryHandler
        _getParticipationByIdHandler;

    public ParticipationsController(
        CreateParticipationCommandHandler
            createParticipationHandler,
        GetParticipationByIdQueryHandler
            getParticipationByIdHandler)
    {
        ArgumentNullException.ThrowIfNull(
            createParticipationHandler);

        ArgumentNullException.ThrowIfNull(
            getParticipationByIdHandler);

        _createParticipationHandler =
            createParticipationHandler;

        _getParticipationByIdHandler =
            getParticipationByIdHandler;
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

    [HttpGet("{participationId}")]
    public async Task<ActionResult<GetParticipationByIdResponse>>
        GetById(
            string participationId,
            CancellationToken cancellationToken)
    {
        if (
            !Guid.TryParseExact(
                participationId,
                "D",
                out Guid parsedParticipationId) ||
            parsedParticipationId == Guid.Empty
        )
        {
            return ToProblem(
                GetParticipationByIdEndpointErrors
                    .InvalidParticipationId);
        }

        GetParticipationByIdQuery query =
            new(
                parsedParticipationId);

        Result<ParticipationDetails> result =
            await _getParticipationByIdHandler.Handle(
                query,
                cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(
                result.Error!);
        }

        ParticipationDetails details =
            result.Value!;

        GetParticipationByIdResponse response =
            new(
                details.Id,
                details.AtmacaCardId,
                details.ActivityTypeCode,
                details.ActivityId,
                GetParticipationStatusCode(
                    details.Status),
                details.ConditionCode,
                details.JoinedAt,
                details.LeftAt,
                details.Note);

        return Ok(
            response);
    }

    private ObjectResult ToProblem(
        Error error)
    {
        int statusCode =
            GetErrorStatusCode(
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

    private static int GetErrorStatusCode(
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
                GetParticipationByIdErrors.NotFound.Code,
                StringComparison.Ordinal)
        )
        {
            return StatusCodes.Status404NotFound;
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

    private static string GetParticipationStatusCode(
        ParticipationStatus status)
    {
        return status switch
        {
            ParticipationStatus.NotRecorded =>
                "NOT_RECORDED",

            ParticipationStatus.Present =>
                "PRESENT",

            ParticipationStatus.Absent =>
                "ABSENT",

            _ =>
                throw new InvalidOperationException(
                    $"Unsupported participation status '{status}'.")
        };
    }
}