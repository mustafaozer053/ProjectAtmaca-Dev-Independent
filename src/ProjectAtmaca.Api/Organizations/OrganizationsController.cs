using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ProjectAtmaca.Application.Organizations.Catalog;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Organizations;

namespace ProjectAtmaca.Api.Organizations;

[ApiController]
[Route("api/organizations")]
public sealed class OrganizationsController(
    OrganizationCatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrganizationResponse>>> List(
        CancellationToken cancellationToken)
    {
        var result = await catalogService.ListAsync(cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        Organization[] organizations = result.Value!.ToArray();
        var organizationsById = organizations.ToDictionary(organization => organization.Id);
        var response = organizations
            .Select(organization => ToResponse(
                organization,
                GetHierarchyPath(organization, organizationsById, [])))
            .OrderBy(organization => organization.HierarchyPath, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<OrganizationResponse>> Create(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await catalogService.CreateAsync(
            request.Name ?? string.Empty,
            request.Code ?? string.Empty,
            request.Description,
            request.ParentOrganizationId,
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        Organization organization = result.Value!;
        var response = ToResponse(organization, organization.Name);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{organizationId:guid}")]
    public async Task<IActionResult> Update(
        Guid organizationId,
        [FromBody] UpdateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await catalogService.UpdateAsync(
            organizationId,
            request.Name ?? string.Empty,
            request.Code ?? string.Empty,
            request.Description,
            request.ParentOrganizationId,
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return NoContent();
    }

    [HttpPatch("{organizationId:guid}/status")]
    public async Task<IActionResult> ChangeStatus(
        Guid organizationId,
        [FromBody] ChangeOrganizationStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await catalogService.ChangeStatusAsync(
            organizationId,
            request.IsActive,
            cancellationToken);
        return result.IsFailure ? ToProblem(result.Error!) : NoContent();
    }

    private ObjectResult ToProblem(Error error)
    {
        int status = error.Code switch
        {
            "ORGANIZATION_PARENT_NOT_FOUND" or "ORGANIZATION_NOT_FOUND" =>
                StatusCodes.Status404NotFound,
            "ORGANIZATION_CODE_DUPLICATE" or "ORGANIZATION_NAME_DUPLICATE" =>
                StatusCodes.Status409Conflict,
            "ORGANIZATION_HAS_ACTIVE_CHILDREN" => StatusCodes.Status409Conflict,
            var code when code == ProjectAtmaca.Application.Abstractions.Security.ActorAuthorizationErrors.Forbidden.Code =>
                StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };
        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = error.Message
        };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        var response = StatusCode(status, problem);
        response.ContentTypes.Add("application/problem+json");
        return response;
    }

    private static string GetHierarchyPath(
        Organization organization,
        IReadOnlyDictionary<Guid, Organization> organizations,
        HashSet<Guid> ancestors)
    {
        if (!ancestors.Add(organization.Id))
            return organization.Name;

        string path = organization.Name;
        if (organization.ParentOrganizationId is Guid parentId &&
            organizations.TryGetValue(parentId, out Organization? parent))
            path = $"{GetHierarchyPath(parent, organizations, ancestors)} / {organization.Name}";

        ancestors.Remove(organization.Id);
        return path;
    }

    private static OrganizationResponse ToResponse(
        Organization organization,
        string hierarchyPath) =>
        new(
            organization.Id,
            organization.Name,
            organization.Code,
            organization.Description,
            organization.ParentOrganizationId,
            organization.IsActive,
            hierarchyPath);
}

public sealed record CreateOrganizationRequest(
    string? Name,
    string? Code,
    string? Description,
    Guid? ParentOrganizationId);

public sealed record UpdateOrganizationRequest(
    string? Name,
    string? Code,
    string? Description,
    Guid? ParentOrganizationId);

public sealed record ChangeOrganizationStatusRequest(bool IsActive);

public sealed record OrganizationResponse(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    Guid? ParentOrganizationId,
    bool IsActive,
    string HierarchyPath);
