using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ProjectAtmaca.Application.TrainingTypes.ChangeStatus;
using ProjectAtmaca.Application.TrainingTypes.Create;
using ProjectAtmaca.Application.TrainingTypes.List;
using ProjectAtmaca.Application.TrainingTypes;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.TrainingTypes;

namespace ProjectAtmaca.Api.TrainingTypes;

[ApiController]
[Route("api/training-types")]
public sealed class TrainingTypesController : ControllerBase
{
    private readonly CreateTrainingTypeCommandHandler _createHandler;
    private readonly ListTrainingTypesQueryHandler _listHandler;
    private readonly ChangeTrainingTypeStatusCommandHandler _statusHandler;

    public TrainingTypesController(
        CreateTrainingTypeCommandHandler createHandler,
        ListTrainingTypesQueryHandler listHandler,
        ChangeTrainingTypeStatusCommandHandler statusHandler)
    {
        _createHandler = createHandler;
        _listHandler = listHandler;
        _statusHandler = statusHandler;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        CreateTrainingTypeRequest request,
        CancellationToken cancellationToken)
    {
        Result<TrainingTypeId> result = await _createHandler.Handle(
            new CreateTrainingTypeCommand(
                request.Code,
                request.Name,
                request.Description,
                request.DisplayOrder),
            cancellationToken);

        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Created(
            $"/api/training-types/{result.Value!.Value:D}",
            result.Value.Value);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TrainingTypeResponse>>> List(
        CancellationToken cancellationToken,
        [FromQuery] bool activeOnly = true)
    {
        Result<IReadOnlyList<TrainingTypeListItem>> result =
            await _listHandler.Handle(
                new ListTrainingTypesQuery(activeOnly),
                cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Ok(result.Value!.Select(x => new TrainingTypeResponse(
            x.Id, x.Code, x.Name, x.Description, x.DisplayOrder, x.IsActive)));
    }

    [HttpPatch("{trainingTypeId}/status")]
    public async Task<IActionResult> ChangeStatus(
        string trainingTypeId,
        ChangeTrainingTypeStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(trainingTypeId, "D", out Guid id) ||
            id == Guid.Empty)
        {
            return ToProblem(Error.Create(
                "TrainingType.Id.Invalid",
                "Training type id must be a non-empty GUID in D format."));
        }

        Result result = await _statusHandler.Handle(
            new ChangeTrainingTypeStatusCommand(
                TrainingTypeId.From(id),
                request.IsActive),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return NoContent();
    }

    private ObjectResult ToProblem(Error error)
    {
        int statusCode = error.Code == ActorAuthorizationErrors.Forbidden.Code
            ? StatusCodes.Status403Forbidden
            : error.Code == TrainingTypeApplicationErrors.NotFound.Code
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
