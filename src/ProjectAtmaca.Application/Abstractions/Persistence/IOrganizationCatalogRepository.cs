using ProjectAtmaca.Domain.Organizations;

namespace ProjectAtmaca.Application.Abstractions.Persistence;

public interface IOrganizationCatalogRepository
{
    Task<IReadOnlyList<Organization>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<Organization?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        string code,
        CancellationToken cancellationToken = default,
        Guid? excludingOrganizationId = null);

    Task<bool> NameExistsUnderParentAsync(
        string name,
        Guid? parentOrganizationId,
        CancellationToken cancellationToken = default,
        Guid? excludingOrganizationId = null);

    Task AddAsync(
        Organization organization,
        CancellationToken cancellationToken = default);
}
