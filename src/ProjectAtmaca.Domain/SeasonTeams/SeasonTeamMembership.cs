using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Domain.SeasonTeams;

public sealed class SeasonTeamMembership : Entity
{
    public SeasonTeamMembershipId SeasonTeamMembershipId =>
        SeasonTeamMembershipId.From(Id);

    public AtmacaCardId AtmacaCardId { get; private set; }

    public AssignmentPeriod Period { get; private set; }

    private SeasonTeamMembership()
    {
        Period = null!;
    }

    private SeasonTeamMembership(
        Guid id,
        AtmacaCardId atmacaCardId,
        AssignmentPeriod period)
        : base(id)
    {
        AtmacaCardId = atmacaCardId;
        Period = period;
    }

    internal static Result<SeasonTeamMembership> Create(
        AtmacaCardId atmacaCardId,
        AssignmentPeriod period)
    {
        if (atmacaCardId.Value == Guid.Empty)
        {
            return Result<SeasonTeamMembership>.Failure(
                Error.Create(
                    "SEASON_TEAM_MEMBERSHIP_ATMACA_CARD_REQUIRED",
                    "Atmaca card id is required."));
        }

        if (period is null)
        {
            return Result<SeasonTeamMembership>.Failure(
                Error.Create(
                    "SEASON_TEAM_MEMBERSHIP_PERIOD_REQUIRED",
                    "Season team membership period is required."));
        }

        return Result<SeasonTeamMembership>.Success(
            new SeasonTeamMembership(
                Guid.NewGuid(),
                atmacaCardId,
                period));
    }

    public bool IsActiveOn(DateTime date)
    {
        return Period.IsActiveOn(date);
    }

    public Result End(DateTime endDate)
    {
        var result = Period.End(endDate);

        if (result.IsFailure)
        {
            return Result.Failure(result.Error!);
        }

        Period = result.Value!;
        return Result.Success();
    }
}
