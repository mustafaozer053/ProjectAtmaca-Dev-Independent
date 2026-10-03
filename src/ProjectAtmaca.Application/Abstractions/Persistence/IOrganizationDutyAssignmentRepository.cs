using ProjectAtmaca.Domain.Assignments;

namespace ProjectAtmaca.Application.Abstractions.Persistence;

public interface IOrganizationDutyAssignmentRepository
{
    Task<bool> OrganizationExistsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> IsOrganizationActiveAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> AtmacaCardExistsAsync(
        Guid atmacaCardId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationDutyAssignment>> ListAsync(
        Guid organizationId,
        Guid atmacaCardId,
        CancellationToken cancellationToken = default);

    Task<OrganizationDutyAssignment?> GetByIdAsync(
        Guid organizationId,
        Guid atmacaCardId,
        Guid assignmentId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        OrganizationDutyAssignment assignment,
        CancellationToken cancellationToken = default);
}
