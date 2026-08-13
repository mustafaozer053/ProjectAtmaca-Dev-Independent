using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;

namespace ProjectAtmaca.Domain.Trainings;

public sealed class SeasonOrganization : ValueObject
{
    public SeasonId SeasonId { get; }

    public OrganizationId OrganizationId { get; }

    private SeasonOrganization(
        SeasonId seasonId,
        OrganizationId organizationId)
    {
        SeasonId = seasonId;
        OrganizationId = organizationId;
    }

    public static SeasonOrganization Create(
        SeasonId seasonId,
        OrganizationId organizationId)
    {
        return new SeasonOrganization(
            seasonId,
            organizationId);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SeasonId;
        yield return OrganizationId;
    }

    public override string ToString()
    {
        return $"{SeasonId} / {OrganizationId}";
    }
}
