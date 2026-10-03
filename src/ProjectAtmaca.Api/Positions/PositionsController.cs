using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Positions;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Api.Positions;

[ApiController]
[Route("api/positions")]
public sealed class PositionsController(PositionCatalogService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PositionResponse>>> List(
        [FromQuery] string sportName,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var result = await service.ListAsync(sportName, activeOnly, cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Ok(result.Value!.Select(ToResponse).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<PositionResponse>> Create(
        [FromBody] CreatePositionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            request.SportName ?? string.Empty,
            request.Code ?? string.Empty,
            request.Name ?? string.Empty,
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        var response = ToResponse(result.Value!);
        return Created($"/api/positions/{response.Id:D}", response);
    }

    [HttpPatch("{positionId:guid}/status")]
    public async Task<IActionResult> ChangeStatus(
        Guid positionId,
        [FromBody] ChangePositionStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ChangeStatusAsync(positionId, request.IsActive, cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return NoContent();
    }

    private ObjectResult ToProblem(Error error)
    {
        var statusCode = error.Code == ActorAuthorizationErrors.Forbidden.Code
            ? StatusCodes.Status403Forbidden
            : error.Code == "POSITION_NOT_FOUND"
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = error.Message
        };
        problem.Extensions["code"] = error.Code;
        var response = StatusCode(statusCode, problem);
        response.ContentTypes.Add("application/problem+json");
        return response;
    }

    private static PositionResponse ToResponse(ProjectAtmaca.Domain.Positions.Position position) =>
        new(position.Id, position.SportName, position.Code, position.Name, position.IsActive);
}

public sealed record CreatePositionRequest(string? SportName, string? Code, string? Name);
public sealed record ChangePositionStatusRequest(bool IsActive);
public sealed record PositionResponse(Guid Id, string SportName, string Code, string Name, bool IsActive);
