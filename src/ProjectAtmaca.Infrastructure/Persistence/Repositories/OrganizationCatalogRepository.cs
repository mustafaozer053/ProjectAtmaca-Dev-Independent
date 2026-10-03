using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Domain.Organizations;

namespace ProjectAtmaca.Infrastructure.Persistence.Repositories;

public sealed class OrganizationCatalogRepository(
    ProjectAtmacaDbContext dbContext) : IOrganizationCatalogRepository
{
    public async Task<IReadOnlyList<Organization>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Organizations.AsNoTracking()
            .OrderBy(organization => organization.Name)
            .ToListAsync(cancellationToken);

    public Task<Organization?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.Organizations.SingleOrDefaultAsync(
            organization => organization.Id == id,
            cancellationToken);

    public Task<bool> CodeExistsAsync(
        string code,
        CancellationToken cancellationToken = default,
        Guid? excludingOrganizationId = null) =>
        dbContext.Organizations.AnyAsync(
            organization =>
                organization.Code.ToUpper() == code.Trim().ToUpper() &&
                (!excludingOrganizationId.HasValue ||
                 organization.Id != excludingOrganizationId.Value),
            cancellationToken);

    public Task<bool> NameExistsUnderParentAsync(
        string name,
        Guid? parentOrganizationId,
        CancellationToken cancellationToken = default,
        Guid? excludingOrganizationId = null) =>
        dbContext.Organizations.AnyAsync(
            organization =>
                organization.Name.ToUpper() == name.Trim().ToUpper() &&
                organization.ParentOrganizationId == parentOrganizationId &&
                (!excludingOrganizationId.HasValue ||
                 organization.Id != excludingOrganizationId.Value),
            cancellationToken);

    public async Task AddAsync(
        Organization organization,
        CancellationToken cancellationToken = default) =>
        await dbContext.Organizations.AddAsync(organization, cancellationToken);
}
