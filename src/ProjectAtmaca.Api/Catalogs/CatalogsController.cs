using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Infrastructure.Persistence;

namespace ProjectAtmaca.Api.Catalogs;

[ApiController]
[Route("api/catalogs")]
public sealed class CatalogsController(ProjectAtmacaDbContext db) : ControllerBase
{
    [HttpGet("seasons")]
    public async Task<ActionResult<IReadOnlyList<CatalogItemResponse>>> Seasons(
        CancellationToken cancellationToken)
    {
        var rows = await db.Seasons.AsNoTracking()
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
        var items = rows
            .OrderByDescending(x => x.Name.Value)
            .Select(x => new CatalogItemResponse(x.Id, x.Name.Value))
            .ToList();
        return Ok(items);
    }

    [HttpGet("organizations")]
    public async Task<ActionResult<IReadOnlyList<CatalogItemResponse>>> Organizations(
        CancellationToken cancellationToken)
    {
        var rows = await db.Organizations.AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
        var rowsById = rows.ToDictionary(organization => organization.Id);
        var items = rows
            .Select(x => new CatalogItemResponse(
                x.Id,
                x.Name,
                x.ParentOrganizationId,
                GetHierarchyPath(x, rowsById, [])))
            .OrderBy(x => x.HierarchyPath, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Ok(items);
    }

    private static string GetHierarchyPath(
        ProjectAtmaca.Domain.Organizations.Organization organization,
        IReadOnlyDictionary<Guid, ProjectAtmaca.Domain.Organizations.Organization> organizations,
        HashSet<Guid> ancestors)
    {
        if (!ancestors.Add(organization.Id))
            return organization.Name;

        string path = organization.Name;
        if (organization.ParentOrganizationId is Guid parentId &&
            organizations.TryGetValue(parentId, out var parent))
            path = $"{GetHierarchyPath(parent, organizations, ancestors)} / {organization.Name}";

        ancestors.Remove(organization.Id);
        return path;
    }

    [HttpGet("age-groups")]
    public async Task<ActionResult<IReadOnlyList<CatalogItemResponse>>> AgeGroups(
        CancellationToken cancellationToken)
    {
        var rows = await db.AgeGroups.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
        var items = rows
            .OrderByDescending(x => int.Parse(x.Code.Value[1..]))
            .Select(x => new CatalogItemResponse(x.Id, x.Code.Value))
            .ToList();
        return Ok(items);
    }
}

public sealed record CatalogItemResponse(
    Guid Id,
    string Name,
    Guid? ParentOrganizationId = null,
    string? HierarchyPath = null);
