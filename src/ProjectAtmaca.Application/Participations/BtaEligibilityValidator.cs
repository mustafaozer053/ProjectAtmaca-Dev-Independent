using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.SeasonTeams;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Participations;

public sealed class BtaEligibilityValidator(
    ITrainingRepository trainings,
    ISeasonTeamRepository seasonTeams)
{
    public async Task<Result> ValidateAsync(
        Participation participation,
        CancellationToken cancellationToken = default)
    {
        if (participation.ActivityReference.ActivityType != ActivityTypeCode.Training)
            return Result.Failure(BtaEligibilityErrors.RequiresOtherActiveTeam);

        Training? training = await trainings.GetByIdAsync(
            TrainingId.From(participation.ActivityReference.ActivityId),
            cancellationToken);
        if (training?.SeasonTeamId is not SeasonTeamId currentTeamId)
            return Result.Failure(BtaEligibilityErrors.RequiresOtherActiveTeam);

        SeasonTeam? currentTeam = await seasonTeams.GetByIdAsync(
            currentTeamId,
            cancellationToken);
        if (currentTeam is null)
            return Result.Failure(BtaEligibilityErrors.RequiresOtherActiveTeam);

        DateTime trainingDate = training.Schedule.Date.ToDateTime(TimeOnly.MinValue);
        IReadOnlyList<SeasonTeam> teams = await seasonTeams.ListAsync(cancellationToken);
        bool hasAnotherActiveRoster = teams.Any(team =>
            team.Status == SeasonTeamStatus.Active &&
            team.SeasonId.Value == currentTeam.SeasonId.Value &&
            team.SeasonTeamId != currentTeamId &&
            team.HasActiveAthleteMembership(participation.AtmacaCardId, trainingDate));

        return hasAnotherActiveRoster
            ? Result.Success()
            : Result.Failure(BtaEligibilityErrors.RequiresOtherActiveTeam);
    }
}
