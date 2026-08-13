using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.Enums;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Domain.Scouting.Entities;

public sealed class ScoutingObservation : Entity
{
    private readonly List<Guid> _positionIds = [];

    public DateOnly ObservedOn { get; private set; }

    public ObservationType ObservationType { get; private set; }

    public string? ObservedEvent { get; private set; }

    public string? ObservedClub { get; private set; }

    public string? ObservedTeam { get; private set; }

    public Guid? AgeGroupId { get; private set; }

    public DominantFoot? DominantFoot { get; private set; }

    public Location? ObservedLocation { get; private set; }

    public string? Strengths { get; private set; }

    public string? Weaknesses { get; private set; }

    public ObserverRecommendation? ObserverRecommendation { get; private set; }

    public string? RecommendationNote { get; private set; }

    public PersonName ObserverName { get; private set; }

    public Guid CreatedByAssignmentId { get; private set; }

    public IReadOnlyCollection<Guid> PositionIds =>
        _positionIds.AsReadOnly();

    private ScoutingObservation()
    {
        ObserverName = null!;
    }

    private ScoutingObservation(
        Guid id,
        DateOnly observedOn,
        ObservationType observationType,
        string? observedEvent,
        string? observedClub,
        string? observedTeam,
        Guid? ageGroupId,
        DominantFoot? dominantFoot,
        Location? observedLocation,
        string? strengths,
        string? weaknesses,
        ObserverRecommendation? observerRecommendation,
        string? recommendationNote,
        PersonName observerName,
        Guid createdByAssignmentId)
        : base(id)
    {
        ObservedOn = observedOn;
        ObservationType = observationType;
        ObservedEvent = observedEvent;
        ObservedClub = observedClub;
        ObservedTeam = observedTeam;
        AgeGroupId = ageGroupId;
        DominantFoot = dominantFoot;
        ObservedLocation = observedLocation;
        Strengths = strengths;
        Weaknesses = weaknesses;
        ObserverRecommendation = observerRecommendation;
        RecommendationNote = recommendationNote;
        ObserverName = observerName;
        CreatedByAssignmentId = createdByAssignmentId;
    }

    internal static Result<ScoutingObservation> Create(
        DateOnly observedOn,
        ObservationType observationType,
        string? observedEvent,
        string? observedClub,
        string? observedTeam,
        Guid? ageGroupId,
        DominantFoot? dominantFoot,
        Location? observedLocation,
        string? strengths,
        string? weaknesses,
        ObserverRecommendation? observerRecommendation,
        string? recommendationNote,
        PersonName observerName,
        Guid createdByAssignmentId)
    {
        if (observedOn > DateOnly.FromDateTime(DateTime.Now))
        {
            return Result<ScoutingObservation>.Failure(
                Error.Create(
                    "SCOUTING_OBSERVATION_DATE_INVALID",
                    "Observation date cannot be in the future."));
        }

        if (!Enum.IsDefined(observationType))
        {
            return Result<ScoutingObservation>.Failure(
                Error.Create(
                    "SCOUTING_OBSERVATION_TYPE_INVALID",
                    "Observation type is invalid."));
        }

        if (observerRecommendation.HasValue &&
            !Enum.IsDefined(observerRecommendation.Value))
        {
            return Result<ScoutingObservation>.Failure(
                Error.Create(
                    "SCOUTING_OBSERVER_RECOMMENDATION_INVALID",
                    "Observer recommendation is invalid."));
        }

        if (dominantFoot.HasValue &&
            !Enum.IsDefined(dominantFoot.Value))
        {
            return Result<ScoutingObservation>.Failure(
                Error.Create(
                    "SCOUTING_OBSERVATION_DOMINANT_FOOT_INVALID",
                    "Dominant foot is invalid."));
        }

        if (ageGroupId.HasValue &&
            ageGroupId.Value == Guid.Empty)
        {
            return Result<ScoutingObservation>.Failure(
                Error.Create(
                    "SCOUTING_OBSERVATION_AGE_GROUP_INVALID",
                    "Age group id is invalid."));
        }

        if (observerName is null)
        {
            return Result<ScoutingObservation>.Failure(
                Error.Create(
                    "SCOUTING_OBSERVER_NAME_REQUIRED",
                    "Observer name is required."));
        }

        if (createdByAssignmentId == Guid.Empty)
        {
            return Result<ScoutingObservation>.Failure(
                Error.Create(
                    "SCOUTING_OBSERVATION_CREATOR_ASSIGNMENT_REQUIRED",
                    "Observation creator assignment is required."));
        }

        return Result<ScoutingObservation>.Success(
            new ScoutingObservation(
                Guid.NewGuid(),
                observedOn,
                observationType,
                NormalizeOptionalText(observedEvent),
                NormalizeOptionalText(observedClub),
                NormalizeOptionalText(observedTeam),
                ageGroupId,
                dominantFoot,
                observedLocation,
                NormalizeOptionalText(strengths),
                NormalizeOptionalText(weaknesses),
                observerRecommendation,
                NormalizeOptionalText(recommendationNote),
                observerName,
                createdByAssignmentId));
    }
    internal Result CorrectObservedDate(DateOnly observedOn)
    {
        if (observedOn > DateOnly.FromDateTime(DateTime.Now))
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_OBSERVATION_DATE_INVALID",
                    "Observation date cannot be in the future."));
        }

        ObservedOn = observedOn;

        return Result.Success();
    }

    internal void ChangeObservedEvent(string? observedEvent)
    {
        ObservedEvent = NormalizeOptionalText(observedEvent);
    }

    internal void ChangeObservedClub(string? observedClub)
    {
        ObservedClub = NormalizeOptionalText(observedClub);
    }

    internal void ChangeObservedTeam(string? observedTeam)
    {
        ObservedTeam = NormalizeOptionalText(observedTeam);
    }

    internal Result SetAgeGroup(Guid? ageGroupId)
    {
        if (ageGroupId.HasValue &&
            ageGroupId.Value == Guid.Empty)
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_OBSERVATION_AGE_GROUP_INVALID",
                    "Age group id is invalid."));
        }

        AgeGroupId = ageGroupId;

        return Result.Success();
    }

    internal Result SetDominantFoot(DominantFoot? dominantFoot)
    {
        if (dominantFoot.HasValue &&
            !Enum.IsDefined(dominantFoot.Value))
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_OBSERVATION_DOMINANT_FOOT_INVALID",
                    "Dominant foot is invalid."));
        }

        DominantFoot = dominantFoot;

        return Result.Success();
    }

    internal void SetObservedLocation(Location? observedLocation)
    {
        ObservedLocation = observedLocation;
    }

    internal void UpdateStrengths(string? strengths)
    {
        Strengths = NormalizeOptionalText(strengths);
    }

    internal void UpdateWeaknesses(string? weaknesses)
    {
        Weaknesses = NormalizeOptionalText(weaknesses);
    }

    internal Result ChangeObserverRecommendation(
    ObserverRecommendation? observerRecommendation)
    {
        if (observerRecommendation.HasValue &&
            !Enum.IsDefined(observerRecommendation.Value))
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_OBSERVER_RECOMMENDATION_INVALID",
                    "Observer recommendation is invalid."));
        }

        ObserverRecommendation = observerRecommendation;

        return Result.Success();
    }

    internal void UpdateRecommendationNote(string? recommendationNote)
    {
        RecommendationNote = NormalizeOptionalText(recommendationNote);
    }

    internal Result ChangeObserverName(PersonName observerName)
    {
        if (observerName is null)
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_OBSERVER_NAME_REQUIRED",
                    "Observer name is required."));
        }

        ObserverName = observerName;

        return Result.Success();
    }
    internal Result AddPosition(Guid positionId)
    {
        if (positionId == Guid.Empty)
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_OBSERVATION_POSITION_INVALID",
                    "Position id is invalid."));
        }

        if (_positionIds.Contains(positionId))
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_OBSERVATION_POSITION_DUPLICATE",
                    "Position has already been added to the observation."));
        }

        _positionIds.Add(positionId);

        return Result.Success();
    }
    internal Result RemovePosition(Guid positionId)
    {
        if (positionId == Guid.Empty)
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_OBSERVATION_POSITION_INVALID",
                    "Position id is invalid."));
        }

        if (!_positionIds.Contains(positionId))
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_OBSERVATION_POSITION_NOT_FOUND",
                    "Position was not found in the observation."));
        }

        _positionIds.Remove(positionId);

        return Result.Success();
    }
    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
