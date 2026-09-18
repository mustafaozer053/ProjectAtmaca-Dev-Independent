using System.Globalization;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

using ProjectAtmaca.Api.Participations.Create;
using ProjectAtmaca.Api.Participations.GetById;
using ProjectAtmaca.Api.Participations.GetSummaryByActivity;
using ProjectAtmaca.Api.Participations.ListByActivity;
using ProjectAtmaca.Api.Participations.ListHistoryByAtmacaCard;
using ProjectAtmaca.Api.Participations.MarkPresent;
using ProjectAtmaca.Api.Participations.RecordArrival;
using ProjectAtmaca.Api.Participations.RecordDeparture;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations.Create;
using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Application.Participations.GetSummaryByActivity;
using ProjectAtmaca.Application.Participations.ListByActivity;
using ProjectAtmaca.Application.Participations.ListHistoryByAtmacaCard;
using ProjectAtmaca.Application.Participations.MarkPresent;
using ProjectAtmaca.Application.Participations.RecordArrival;
using ProjectAtmaca.Application.Participations.RecordDeparture;
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

    private readonly GetParticipationSummaryByActivityQueryHandler
        _getParticipationSummaryByActivityHandler;

    private readonly ListParticipationsByActivityQueryHandler
        _listParticipationsByActivityHandler;

    private readonly ListParticipationHistoryByAtmacaCardQueryHandler
        _listParticipationHistoryByAtmacaCardHandler;

    private readonly MarkParticipationPresentCommandHandler
        _markParticipationPresentHandler;

    private readonly RecordParticipationArrivalCommandHandler
        _recordParticipationArrivalHandler;

    private readonly RecordParticipationDepartureCommandHandler
        _recordParticipationDepartureHandler;

    public ParticipationsController(
        CreateParticipationCommandHandler
            createParticipationHandler,
        GetParticipationByIdQueryHandler
            getParticipationByIdHandler,
        GetParticipationSummaryByActivityQueryHandler
            getParticipationSummaryByActivityHandler,
        ListParticipationsByActivityQueryHandler
            listParticipationsByActivityHandler,
        ListParticipationHistoryByAtmacaCardQueryHandler
            listParticipationHistoryByAtmacaCardHandler,
        MarkParticipationPresentCommandHandler
            markParticipationPresentHandler,
        RecordParticipationArrivalCommandHandler
            recordParticipationArrivalHandler,
        RecordParticipationDepartureCommandHandler
            recordParticipationDepartureHandler)
    {
        ArgumentNullException.ThrowIfNull(
            createParticipationHandler);

        ArgumentNullException.ThrowIfNull(
            getParticipationByIdHandler);

        ArgumentNullException.ThrowIfNull(
            getParticipationSummaryByActivityHandler);

        ArgumentNullException.ThrowIfNull(
            listParticipationsByActivityHandler);

        ArgumentNullException.ThrowIfNull(
            listParticipationHistoryByAtmacaCardHandler);

        ArgumentNullException.ThrowIfNull(
            markParticipationPresentHandler);

        ArgumentNullException.ThrowIfNull(
            recordParticipationArrivalHandler);

        ArgumentNullException.ThrowIfNull(
            recordParticipationDepartureHandler);

        _createParticipationHandler =
            createParticipationHandler;

        _getParticipationByIdHandler =
            getParticipationByIdHandler;

        _getParticipationSummaryByActivityHandler =
            getParticipationSummaryByActivityHandler;

        _listParticipationsByActivityHandler =
            listParticipationsByActivityHandler;

        _listParticipationHistoryByAtmacaCardHandler =
            listParticipationHistoryByAtmacaCardHandler;

        _markParticipationPresentHandler =
            markParticipationPresentHandler;

        _recordParticipationArrivalHandler =
            recordParticipationArrivalHandler;

        _recordParticipationDepartureHandler =
            recordParticipationDepartureHandler;
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
                ParticipationEndpointErrors
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

    [HttpGet("history")]
    public async Task<ActionResult<
        ParticipationHistoryPageResponse>>
        ListHistoryByAtmacaCard(
            [FromQuery] string? atmacaCardId,
            [FromQuery] int pageSize,
            [FromQuery] string? cursorAtmacaCardId,
            [FromQuery] string? cursorCreatedAtUtc,
            [FromQuery] string? cursorParticipationId,
            CancellationToken cancellationToken)
    {
        if (
            !Guid.TryParseExact(
                atmacaCardId,
                "D",
                out Guid parsedAtmacaCardId) ||
            parsedAtmacaCardId == Guid.Empty
        )
        {
            return ToProblem(
                ParticipationEndpointErrors
                    .InvalidAtmacaCardId);
        }
        ParticipationHistoryCursor? cursor =
            null;

        bool hasCursor =
            cursorAtmacaCardId is not null ||
            cursorCreatedAtUtc is not null ||
            cursorParticipationId is not null;

        if (hasCursor)
        {
            if (
                !Guid.TryParseExact(
                    cursorAtmacaCardId,
                    "D",
                    out Guid parsedCursorAtmacaCardId) ||
                parsedCursorAtmacaCardId == Guid.Empty ||
                !DateTime.TryParseExact(
                    cursorCreatedAtUtc,
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime parsedCursorCreatedAtUtc) ||
                parsedCursorCreatedAtUtc.Kind !=
                    DateTimeKind.Utc ||
                !Guid.TryParseExact(
                    cursorParticipationId,
                    "D",
                    out Guid parsedCursorParticipationId) ||
                parsedCursorParticipationId == Guid.Empty
            )
            {
                return ToProblem(
                    ParticipationEndpointErrors
                        .InvalidHistoryCursor);
            }

            cursor =
                new ParticipationHistoryCursor(
                    AtmacaCardId.From(
                        parsedCursorAtmacaCardId),
                    parsedCursorCreatedAtUtc,
                    parsedCursorParticipationId);
        }
        ListParticipationHistoryByAtmacaCardQuery query =
            new(
                AtmacaCardId.From(
                    parsedAtmacaCardId),
                pageSize,
                cursor);

        Result<ParticipationHistoryPage> result =
            await _listParticipationHistoryByAtmacaCardHandler
                .Handle(
                    query,
                    cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(
                result.Error!);
        }

        ParticipationHistoryPage page =
            result.Value!;

        IReadOnlyList<ParticipationHistoryItemResponse> items =
            page.Items
                .Select(
                    item =>
                        new ParticipationHistoryItemResponse(
                            item.ParticipationId,
                            item.ActivityReference
                                .ActivityType
                                .Value,
                            item.ActivityReference
                                .ActivityId,
                            GetParticipationStatusCode(
                                item.Status),
                            item.ConditionCode,
                            item.JoinedAt,
                            item.LeftAt,
                            item.CreatedAtUtc))
                .ToList();

        ParticipationHistoryCursorResponse? nextCursor =
            null;

        if (page.NextCursor is not null)
        {
            nextCursor =
                new ParticipationHistoryCursorResponse(
                    page.NextCursor.AtmacaCardId.Value,
                    page.NextCursor.CreatedAtUtc,
                    page.NextCursor.ParticipationId);
        }

        ParticipationHistoryPageResponse response =
            new(
                items,
                nextCursor);

        return Ok(
            response);
    }
    [HttpGet("summary")]
    public async Task<ActionResult<
        ParticipationActivitySummaryResponse>>
        GetSummaryByActivity(
            [FromQuery] string? activityTypeCode,
            [FromQuery] string? activityId,
            CancellationToken cancellationToken)
    {
        Result<ActivityTypeCode> activityTypeResult =
            ActivityTypeCode.Create(
                activityTypeCode ??
                    string.Empty);

        if (activityTypeResult.IsFailure)
        {
            return ToProblem(
                activityTypeResult.Error!);
        }

        if (
            !Guid.TryParseExact(
                activityId,
                "D",
                out Guid parsedActivityId) ||
            parsedActivityId == Guid.Empty
        )
        {
            return ToProblem(
                ParticipationEndpointErrors
                    .InvalidActivityId);
        }

        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.From(
                    parsedActivityId));

        GetParticipationSummaryByActivityQuery query =
            new(
                activityReference);

        Result<ParticipationActivitySummary> result =
            await _getParticipationSummaryByActivityHandler
                .Handle(
                    query,
                    cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(
                result.Error!);
        }

        ParticipationActivitySummary summary =
            result.Value!;

        ParticipationActivitySummaryResponse response =
            new(
                summary.Total,
                summary.NotRecorded,
                summary.Present,
                summary.Absent);

        return Ok(
            response);
    }
    [HttpGet]
    public async Task<ActionResult<
        IReadOnlyList<ParticipationListItemResponse>>>
        ListByActivity(
            [FromQuery] string? activityTypeCode,
            [FromQuery] string? activityId,
            CancellationToken cancellationToken)
    {
        Result<ActivityTypeCode> activityTypeResult =
            ActivityTypeCode.Create(
                activityTypeCode ??
                    string.Empty);

        if (activityTypeResult.IsFailure)
        {
            return ToProblem(
                activityTypeResult.Error!);
        }

        if (
            !Guid.TryParseExact(
                activityId,
                "D",
                out Guid parsedActivityId) ||
            parsedActivityId == Guid.Empty
        )
        {
            return ToProblem(
                ParticipationEndpointErrors
                    .InvalidActivityId);
        }

        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.From(
                    parsedActivityId));

        ListParticipationsByActivityQuery query =
            new(
                activityReference);

        Result<IReadOnlyList<ParticipationListItem>> result =
            await _listParticipationsByActivityHandler.Handle(
                query,
                cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(
                result.Error!);
        }

        IReadOnlyList<ParticipationListItemResponse> response =
            result.Value!
                .Select(
                    item =>
                        new ParticipationListItemResponse(
                            item.Id,
                            item.AtmacaCardId,
                            GetParticipationStatusCode(
                                item.Status),
                            item.ConditionCode,
                            item.JoinedAt,
                            item.LeftAt))
                .ToList();

        return Ok(
            response);
    }

    [HttpPost("{participationId}/mark-present")]
    public async Task<IActionResult> MarkPresent(
        string participationId,
        [FromBody] MarkParticipationPresentRequest request,
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
                ParticipationEndpointErrors
                    .InvalidParticipationId);
        }

        ParticipationCondition? condition =
            null;

        if (request.ConditionCode is not null)
        {
            Result<ParticipationCondition> conditionResult =
                ParticipationCondition.Create(
                    request.ConditionCode);

            if (conditionResult.IsFailure)
            {
                return ToProblem(
                    conditionResult.Error!);
            }

            condition =
                conditionResult.Value!;
        }

        MarkParticipationPresentCommand command =
            new(
                ParticipationId.From(
                    parsedParticipationId),
                condition);

        Result result =
            await _markParticipationPresentHandler.Handle(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(
                result.Error!);
        }

        return NoContent();
    }

    [HttpPost("{participationId}/record-arrival")]
    public async Task<IActionResult> RecordArrival(
        string participationId,
        [FromBody] RecordParticipationArrivalRequest request,
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
                ParticipationEndpointErrors
                    .InvalidParticipationId);
        }

        RecordParticipationArrivalCommand command =
            new(
                ParticipationId.From(
                    parsedParticipationId),
                request.JoinedAt);

        Result result =
            await _recordParticipationArrivalHandler.Handle(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(
                result.Error!);
        }

        return NoContent();
    }

    [HttpPost("{participationId}/record-departure")]
    public async Task<IActionResult> RecordDeparture(
        string participationId,
        [FromBody] RecordParticipationDepartureRequest request,
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
                ParticipationEndpointErrors
                    .InvalidParticipationId);
        }

        RecordParticipationDepartureCommand command =
            new(
                ParticipationId.From(
                    parsedParticipationId),
                request.LeftAt);

        Result result =
            await _recordParticipationDepartureHandler.Handle(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(
                result.Error!);
        }

        return NoContent();
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
                StringComparison.Ordinal) ||
            string.Equals(
                error.Code,
                MarkParticipationPresentErrors.NotFound.Code,
                StringComparison.Ordinal) ||
            string.Equals(
                error.Code,
                RecordParticipationArrivalErrors.NotFound.Code,
                StringComparison.Ordinal) ||
            string.Equals(
                error.Code,
                RecordParticipationDepartureErrors.NotFound.Code,
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
            ||
            string.Equals(
                error.Code,
                ParticipationErrors
                    .ClassificationCorrectionRequired.Code,
                StringComparison.Ordinal)
            ||
            string.Equals(
                error.Code,
                ParticipationErrors
                    .ArrivalCorrectionRequired.Code,
                StringComparison.Ordinal)
            ||
            string.Equals(
                error.Code,
                ParticipationErrors
                    .DepartureCorrectionRequired.Code,
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
