using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ProjectAtmaca.Api.Decisions.ListApplicationHistory;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Decisions.ListApplicationHistory;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Api.Decisions.ApplyParticipationClassification;
using ProjectAtmaca.Application.Decisions.ApplyParticipationClassification;

namespace ProjectAtmaca.Api.Decisions;

[ApiController]
[Route("api/decisions")]
public sealed class DecisionsController(
    ListDecisionApplicationHistoryQueryHandler historyHandler,
    ApplyParticipationClassificationCommandHandler classificationHandler) : ControllerBase
{
    private static readonly Error InvalidDecisionId = Error.Create(
        "Decision.Id.Invalid", "Decision id must be a non-empty GUID in D format.");

    [HttpGet("{decisionId}/applications")]
    public async Task<ActionResult<IReadOnlyList<DecisionApplicationHistoryItemResponse>>> ListApplicationHistory(
        [FromRoute] string decisionId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(decisionId, "D", out Guid parsedId) || parsedId == Guid.Empty)
        {
            return ToProblem(InvalidDecisionId, StatusCodes.Status400BadRequest);
        }

        var result = await historyHandler.Handle(
            new ListDecisionApplicationHistoryQuery(DecisionId.From(parsedId)), cancellationToken);
        if (result.IsFailure)
        {
            return ToProblem(result.Error!,
                result.Error!.Code == ActorAuthorizationErrors.Forbidden.Code
                    ? StatusCodes.Status403Forbidden
                    : StatusCodes.Status400BadRequest);
        }

        return Ok(result.Value!.Select(item => new DecisionApplicationHistoryItemResponse(
            item.DecisionApplicationId.Value,
            item.DecisionId.Value,
            item.Target.TargetType switch
            {
                DecisionTargetType.Participation => "PARTICIPATION",
                _ => throw new InvalidOperationException("Unsupported decision target type.")
            },
            item.Target.TargetId,
            item.AppliedDecisionRevision.Value,
            item.AppliedAtUtc)).ToArray());
    }

    [HttpPost("{decisionId}/apply-participation-classification")]
    public async Task<IActionResult> ApplyParticipationClassification(
        [FromRoute] string decisionId,
        [FromBody] ApplyParticipationClassificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(decisionId, "D", out Guid parsedDecisionId) || parsedDecisionId == Guid.Empty)
            return ToProblem(InvalidDecisionId, StatusCodes.Status400BadRequest);

        if (!Guid.TryParseExact(request.OperationId, "D", out Guid operationId) || operationId == Guid.Empty)
            return ToProblem(Error.Create("DecisionApplication.OperationId.Invalid",
                "Operation id must be a non-empty GUID in D format."), StatusCodes.Status400BadRequest);

        if (request.DecisionRevision < 1)
            return ToProblem(Error.Create("Decision.Revision.Invalid",
                "Decision revision must be greater than zero."), StatusCodes.Status400BadRequest);

        if (!DateTimeOffset.TryParseExact(request.AppliedAtUtc,
                ["yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz"],
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset appliedAtUtc)
            || appliedAtUtc == default || appliedAtUtc.Offset != TimeSpan.Zero)
            return ToProblem(Error.Create("DecisionApplication.AppliedAtUtc.Invalid",
                "Applied time must be a non-default ISO 8601 timestamp with an explicit UTC offset."),
                StatusCodes.Status400BadRequest);

        Result result = await classificationHandler.Handle(new ApplyParticipationClassificationCommand(
            DecisionApplicationOperationId.From(operationId), DecisionId.From(parsedDecisionId),
            DecisionRevision.From(request.DecisionRevision), appliedAtUtc), cancellationToken);
        if (result.IsSuccess) return NoContent();

        Error error = result.Error!;
        int status = error.Code switch
        {
            var code when code == ActorAuthorizationErrors.Forbidden.Code => StatusCodes.Status403Forbidden,
            var code when code == ApplyParticipationClassificationErrors.DecisionNotFound.Code
                || code == ApplyParticipationClassificationErrors.ParticipationNotFound.Code => StatusCodes.Status404NotFound,
            var code when code == ApplyParticipationClassificationErrors.RevisionMismatch.Code
                || code == ApplyParticipationClassificationErrors.DecisionSuperseded.Code
                || code == ApplyParticipationClassificationErrors.DecisionAuthorityLost.Code
                || code == ApplyParticipationClassificationErrors.OperationConflict.Code
                || code == ParticipationErrors.ClassificationCorrectionRequired.Code => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return ToProblem(error, status);
    }

    private ObjectResult ToProblem(Error error, int status)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = error.Message
        };
        problem.Extensions["code"] = error.Code;
        ObjectResult response = StatusCode(status, problem);
        response.ContentTypes.Add("application/problem+json");
        return response;
    }
}
