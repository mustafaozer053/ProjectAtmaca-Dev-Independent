using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;

namespace ProjectAtmaca.Domain.SeasonTeams;

public sealed class SeasonTeam : AuditableAggregateRoot
{
    private readonly List<SeasonTeamMembership> _memberships = [];

    public SeasonTeamId SeasonTeamId =>
        SeasonTeamId.From(Id);

    public SeasonId SeasonId { get; private set; }

    public OrganizationId OrganizationId { get; private set; }

    public Guid AgeGroupId { get; private set; }

    public string Name { get; private set; }

    public SeasonTeamStatus Status { get; private set; }

    public IReadOnlyCollection<SeasonTeamMembership> Memberships =>
        _memberships.AsReadOnly();

    private SeasonTeam()
    {
        Name = null!;
    }

    private SeasonTeam(
        Guid id,
        SeasonId seasonId,
        OrganizationId organizationId,
        Guid ageGroupId,
        string name)
        : base(id)
    {
        SeasonId = seasonId;
        OrganizationId = organizationId;
        AgeGroupId = ageGroupId;
        Name = name;
        Status = SeasonTeamStatus.Active;
    }

    public static Result<SeasonTeam> Create(
        SeasonId seasonId,
        OrganizationId organizationId,
        Guid ageGroupId,
        string name)
    {
        if (seasonId.Value == Guid.Empty)
            return Result<SeasonTeam>.Failure(SeasonTeamErrors.SeasonRequired);

        if (organizationId.Value == Guid.Empty)
            return Result<SeasonTeam>.Failure(SeasonTeamErrors.OrganizationRequired);

        if (ageGroupId == Guid.Empty)
            return Result<SeasonTeam>.Failure(SeasonTeamErrors.AgeGroupRequired);

        if (string.IsNullOrWhiteSpace(name))
            return Result<SeasonTeam>.Failure(SeasonTeamErrors.NameRequired);

        return Result<SeasonTeam>.Success(
            new SeasonTeam(
                Guid.NewGuid(),
                seasonId,
                organizationId,
                ageGroupId,
                name.Trim()));
    }

    public Result<SeasonTeamMembership> AddMembership(
        AtmacaCardId atmacaCardId,
        AssignmentPeriod period)
    {
        if (_memberships.Any(
                x => x.AtmacaCardId == atmacaCardId &&
                     PeriodsOverlap(x.Period, period)))
        {
            return Result<SeasonTeamMembership>.Failure(
                SeasonTeamErrors.DuplicateMembership);
        }

        var membershipResult =
            SeasonTeamMembership.Create(atmacaCardId, period);

        if (membershipResult.IsFailure)
        {
            return Result<SeasonTeamMembership>.Failure(
                membershipResult.Error!);
        }

        var membership = membershipResult.Value!;
        _memberships.Add(membership);

        return Result<SeasonTeamMembership>.Success(membership);
    }

    private static bool PeriodsOverlap(
        AssignmentPeriod first,
        AssignmentPeriod second)
    {
        var firstEnd = first.EndDate ?? DateTime.MaxValue.Date;
        var secondEnd = second.EndDate ?? DateTime.MaxValue.Date;

        return first.StartDate <= secondEnd &&
               second.StartDate <= firstEnd;
    }

    public Result RemoveMembership(SeasonTeamMembershipId membershipId)
    {
        var membership = _memberships
            .SingleOrDefault(x => x.SeasonTeamMembershipId == membershipId);

        if (membership is null)
        {
            return Result.Failure(
                Error.Create(
                    "SEASON_TEAM_MEMBERSHIP_NOT_FOUND",
                    "Season team membership was not found."));
        }

        _memberships.Remove(membership);
        return Result.Success();
    }

    public bool HasActiveMembership(
        AtmacaCardId atmacaCardId,
        DateTime onDate)
    {
        return _memberships.Any(
            x => x.AtmacaCardId == atmacaCardId &&
                 x.IsActiveOn(onDate));
    }

    public void Activate()
    {
        Status = SeasonTeamStatus.Active;
    }

    public void Deactivate()
    {
        Status = SeasonTeamStatus.Inactive;
    }
}
