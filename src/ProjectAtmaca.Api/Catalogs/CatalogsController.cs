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
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        var items = rows.Select(x => new CatalogItemResponse(x.Id, x.Name)).ToList();
        return Ok(items);
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

public sealed record CatalogItemResponse(Guid Id, string Name);
