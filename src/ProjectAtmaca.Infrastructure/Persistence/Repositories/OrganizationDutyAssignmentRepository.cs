using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Domain.Assignments;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Organizations;

namespace ProjectAtmaca.Infrastructure.Persistence.Repositories;

public sealed class OrganizationDutyAssignmentRepository(
    ProjectAtmacaDbContext dbContext) : IOrganizationDutyAssignmentRepository
{
    public Task<bool> OrganizationExistsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<Organization>()
            .AnyAsync(organization => organization.Id == organizationId, cancellationToken);

    public Task<bool> IsOrganizationActiveAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<Organization>()
            .AnyAsync(
                organization => organization.Id == organizationId && organization.IsActive,
                cancellationToken);

    public Task<bool> AtmacaCardExistsAsync(
        Guid atmacaCardId,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<AtmacaCard>()
            .AnyAsync(card => card.Id == atmacaCardId, cancellationToken);

    public async Task<IReadOnlyList<OrganizationDutyAssignment>> ListAsync(
        Guid organizationId,
        Guid atmacaCardId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<OrganizationDutyAssignment>()
            .AsNoTracking()
            .Where(assignment =>
                assignment.OrganizationId == organizationId &&
                assignment.AtmacaCardId == atmacaCardId)
            .OrderBy(assignment => assignment.StartDate)
            .ToListAsync(cancellationToken);

    public Task<OrganizationDutyAssignment?> GetByIdAsync(
        Guid organizationId,
        Guid atmacaCardId,
        Guid assignmentId,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<OrganizationDutyAssignment>()
            .SingleOrDefaultAsync(assignment =>
                assignment.Id == assignmentId &&
                assignment.OrganizationId == organizationId &&
                assignment.AtmacaCardId == atmacaCardId,
                cancellationToken);

    public async Task AddAsync(
        OrganizationDutyAssignment assignment,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<OrganizationDutyAssignment>()
            .AddAsync(assignment, cancellationToken);
}
