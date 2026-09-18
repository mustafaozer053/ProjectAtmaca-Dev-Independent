using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.Enums;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Scouting.Entities;
using ProjectAtmaca.Domain.Scouting.ValueObjects;

namespace ProjectAtmaca.Domain.Scouting;

public sealed class ScoutingCandidate : AuditableAggregateRoot
{
    private readonly List<ScoutingObservation> _observations = [];

    public PersonName Name { get; private set; }

    public IdentityNumber? IdentityNumber { get; private set; }

    public LicenseNumber? LicenseNumber { get; private set; }

    public BirthDate? BirthDate { get; private set; }

    public Location? BirthPlace { get; private set; }

    public Country? Nationality { get; private set; }

    public PhoneNumber? PrimaryPhoneNumber { get; private set; }

    public PhoneNumber? SecondaryPhoneNumber { get; private set; }

    public Email? Email { get; private set; }

    public Address? Address { get; private set; }

    public InitialScoutingSource InitialSource { get; private set; }

    public ScoutingDecision ScoutingDecision { get; private set; }

    public IReadOnlyCollection<ScoutingObservation> Observations =>
        _observations.AsReadOnly();

    private ScoutingCandidate()
    {
        Name = null!;
        InitialSource = null!;
    }

    private ScoutingCandidate(
        Guid id,
        PersonName name,
        IdentityNumber? identityNumber,
        LicenseNumber? licenseNumber,
        BirthDate? birthDate,
        Location? birthPlace,
        Country? nationality,
        PhoneNumber? primaryPhoneNumber,
        PhoneNumber? secondaryPhoneNumber,
        Email? email,
        Address? address,
        InitialScoutingSource initialSource)
        : base(id)
    {
        Name = name;
        IdentityNumber = identityNumber;
        LicenseNumber = licenseNumber;
        BirthDate = birthDate;
        BirthPlace = birthPlace;
        Nationality = nationality;
        PrimaryPhoneNumber = primaryPhoneNumber;
        SecondaryPhoneNumber = secondaryPhoneNumber;
        Email = email;
        Address = address;
        InitialSource = initialSource;
        ScoutingDecision = ScoutingDecision.ContinueWatching;
    }

    public static Result<ScoutingCandidate> Create(
        PersonName name,
        IdentityNumber? identityNumber,
        LicenseNumber? licenseNumber,
        BirthDate? birthDate,
        Location? birthPlace,
        Country? nationality,
        PhoneNumber? primaryPhoneNumber,
        PhoneNumber? secondaryPhoneNumber,
        Email? email,
        Address? address,
        InitialScoutingSource initialSource,
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
        if (name is null)
        {
            return Result<ScoutingCandidate>.Failure(
                Error.Create(
                    "SCOUTING_CANDIDATE_NAME_REQUIRED",
                    "Scouting candidate name is required."));
        }

        if (initialSource is null)
        {
            return Result<ScoutingCandidate>.Failure(
                Error.Create(
                    "SCOUTING_CANDIDATE_INITIAL_SOURCE_REQUIRED",
                    "Initial scouting source is required."));
        }

        var observationResult = ScoutingObservation.Create(
            observedOn,
            observationType,
            observedEvent,
            observedClub,
            observedTeam,
            ageGroupId,
            dominantFoot,
            observedLocation,
            strengths,
            weaknesses,
            observerRecommendation,
            recommendationNote,
            observerName,
            createdByAssignmentId);

        if (observationResult.IsFailure)
        {
            return Result<ScoutingCandidate>.Failure(
                observationResult.Error!);
        }

        var initialObservation = observationResult.Value!;

        if (!HasDistinctiveReference(
                identityNumber,
                licenseNumber,
                birthDate,
                initialObservation))
        {
            return Result<ScoutingCandidate>.Failure(
                Error.Create(
                    "SCOUTING_CANDIDATE_INSUFFICIENT_IDENTIFICATION",
                    "Insufficient information was provided to identify the scouting candidate."));
        }

        var candidate = new ScoutingCandidate(
            Guid.NewGuid(),
            name,
            identityNumber,
            licenseNumber,
            birthDate,
            birthPlace,
            nationality,
            primaryPhoneNumber,
            secondaryPhoneNumber,
            email,
            address,
            initialSource);

        candidate._observations.Add(initialObservation);

        return Result<ScoutingCandidate>.Success(candidate);
    }

    public Result AddObservation(
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
        var observationResult = ScoutingObservation.Create(
            observedOn,
            observationType,
            observedEvent,
            observedClub,
            observedTeam,
            ageGroupId,
            dominantFoot,
            observedLocation,
            strengths,
            weaknesses,
            observerRecommendation,
            recommendationNote,
            observerName,
            createdByAssignmentId);

        if (observationResult.IsFailure)
        {
            return Result.Failure(
                observationResult.Error!);
        }

        _observations.Add(observationResult.Value!);

        return Result.Success();
    }
    public Result ChangeName(PersonName name)
    {
        if (name is null)
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_CANDIDATE_NAME_REQUIRED",
                    "Scouting candidate name is required."));
        }

        Name = name;

        return Result.Success();
    }

    public void SetIdentityNumber(IdentityNumber? identityNumber)
    {
        IdentityNumber = identityNumber;
    }

    public void SetLicenseNumber(LicenseNumber? licenseNumber)
    {
        LicenseNumber = licenseNumber;
    }

    public void SetBirthDate(BirthDate? birthDate)
    {
        BirthDate = birthDate;
    }

    public void SetBirthPlace(Location? birthPlace)
    {
        BirthPlace = birthPlace;
    }

    public void SetNationality(Country? nationality)
    {
        Nationality = nationality;
    }

    public void SetPrimaryPhoneNumber(
        PhoneNumber? phoneNumber)
    {
        PrimaryPhoneNumber = phoneNumber;
    }

    public void SetSecondaryPhoneNumber(
        PhoneNumber? phoneNumber)
    {
        SecondaryPhoneNumber = phoneNumber;
    }

    public void SetEmail(Email? email)
    {
        Email = email;
    }

    public void SetAddress(Address? address)
    {
        Address = address;
    }
    public Result CorrectObservationDate(
    Guid observationId,
    DateOnly observedOn)
    {
        var observationResult = FindObservation(observationId);

        if (observationResult.IsFailure)
            return Result.Failure(observationResult.Error!);

        return observationResult.Value!
            .CorrectObservedDate(observedOn);
    }

    public Result ChangeObservationEvent(
        Guid observationId,
        string? observedEvent)
    {
        var observationResult = FindObservation(observationId);

        if (observationResult.IsFailure)
            return Result.Failure(observationResult.Error!);

        observationResult.Value!
            .ChangeObservedEvent(observedEvent);

        return Result.Success();
    }

    public Result ChangeObservationClub(
        Guid observationId,
        string? observedClub)
    {
        var observationResult = FindObservation(observationId);

        if (observationResult.IsFailure)
            return Result.Failure(observationResult.Error!);

        observationResult.Value!
            .ChangeObservedClub(observedClub);

        return Result.Success();
    }

    public Result ChangeObservationTeam(
        Guid observationId,
        string? observedTeam)
    {
        var observationResult = FindObservation(observationId);

        if (observationResult.IsFailure)
            return Result.Failure(observationResult.Error!);

        observationResult.Value!
            .ChangeObservedTeam(observedTeam);

        return Result.Success();
    }

    public Result SetObservationAgeGroup(
        Guid observationId,
        Guid? ageGroupId)
    {
        var observationResult = FindObservation(observationId);

        if (observationResult.IsFailure)
            return Result.Failure(observationResult.Error!);

        return observationResult.Value!
            .SetAgeGroup(ageGroupId);
    }

    public Result SetObservationDominantFoot(
        Guid observationId,
        DominantFoot? dominantFoot)
    {
        var observationResult = FindObservation(observationId);

        if (observationResult.IsFailure)
            return Result.Failure(observationResult.Error!);

        return observationResult.Value!
            .SetDominantFoot(dominantFoot);
    }

    public Result SetObservationLocation(
        Guid observationId,
        Location? observedLocation)
    {
        var observationResult = FindObservation(observationId);

        if (observationResult.IsFailure)
            return Result.Failure(observationResult.Error!);

        observationResult.Value!
            .SetObservedLocation(observedLocation);

        return Result.Success();
    }

    public Result UpdateObservationStrengths(
        Guid observationId,
        string? strengths)
    {
        var observationResult = FindObservation(observationId);

        if (observationResult.IsFailure)
            return Result.Failure(observationResult.Error!);

        observationResult.Value!
            .UpdateStrengths(strengths);

        return Result.Success();
    }

    public Result UpdateObservationWeaknesses(
        Guid observationId,
        string? weaknesses)
    {
        var observationResult = FindObservation(observationId);

        if (observationResult.IsFailure)
            return Result.Failure(observationResult.Error!);

        observationResult.Value!
            .UpdateWeaknesses(weaknesses);

        return Result.Success();
    }

    public Result ChangeObservationRecommendation(
    Guid observationId,
    ObserverRecommendation? observerRecommendation)
    {
        var observationResult = FindObservation(observationId);

        if (observationResult.IsFailure)
            return Result.Failure(observationResult.Error!);

        return observationResult.Value!
            .ChangeObserverRecommendation(observerRecommendation);
    }

    public Result UpdateObservationRecommendationNote(
        Guid observationId,
        string? recommendationNote)
    {
        var observationResult = FindObservation(observationId);

        if (observationResult.IsFailure)
            return Result.Failure(observationResult.Error!);

        observationResult.Value!
            .UpdateRecommendationNote(recommendationNote);

        return Result.Success();
    }

    public Result ChangeObservationObserverName(
        Guid observationId,
        PersonName observerName)
    {
        var observationResult = FindObservation(observationId);

        if (observationResult.IsFailure)
            return Result.Failure(observationResult.Error!);

        return observationResult.Value!
            .ChangeObserverName(observerName);
    }

    public Result AddObservationPosition(
        Guid observationId,
        Guid positionId)
    {
        var observationResult = FindObservation(observationId);

        if (observationResult.IsFailure)
            return Result.Failure(observationResult.Error!);

        return observationResult.Value!
            .AddPosition(positionId);
    }

    public Result RemoveObservationPosition(
        Guid observationId,
        Guid positionId)
    {
        var observationResult = FindObservation(observationId);

        if (observationResult.IsFailure)
            return Result.Failure(observationResult.Error!);

        return observationResult.Value!
            .RemovePosition(positionId);
    }
    public Result ChangeScoutingDecision(
        ScoutingDecision scoutingDecision)
    {
        if (!Enum.IsDefined(scoutingDecision))
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_DECISION_INVALID",
                    "Scouting decision is invalid."));
        }

        ScoutingDecision = scoutingDecision;

        return Result.Success();
    }

    public Result<ScoutingConversion> ConvertToPlayer(
        Guid personId,
        Guid clubId,
        Guid academyId,
        Guid? teamId = null,
        DateTime? convertedAtUtc = null,
        string? notes = null)
    {
        if (personId == Guid.Empty)
        {
            return Result<ScoutingConversion>.Failure(
                Error.Create(
                    "SCOUTING_CONVERSION_PERSON_ID_REQUIRED",
                    "Person id is required."));
        }

        if (clubId == Guid.Empty)
        {
            return Result<ScoutingConversion>.Failure(
                Error.Create(
                    "SCOUTING_CONVERSION_CLUB_ID_REQUIRED",
                    "Club id is required."));
        }

        if (academyId == Guid.Empty)
        {
            return Result<ScoutingConversion>.Failure(
                Error.Create(
                    "SCOUTING_CONVERSION_ACADEMY_ID_REQUIRED",
                    "Academy id is required."));
        }

        var conversion = ScoutingConversion.Create(
            Id,
            personId,
            clubId,
            academyId,
            teamId,
            convertedAtUtc,
            notes);

        if (conversion.IsFailure)
        {
            return conversion;
        }

        ScoutingDecision = ScoutingDecision.Positive;

        return conversion;
    }

    private Result<ScoutingObservation> FindObservation(
    Guid observationId)
    {
        if (observationId == Guid.Empty)
        {
            return Result<ScoutingObservation>.Failure(
                Error.Create(
                    "SCOUTING_OBSERVATION_ID_INVALID",
                    "Observation id is invalid."));
        }

        var observation = _observations
            .FirstOrDefault(x => x.Id == observationId);

        if (observation is null)
        {
            return Result<ScoutingObservation>.Failure(
                Error.Create(
                    "SCOUTING_OBSERVATION_NOT_FOUND",
                    "Observation was not found in the scouting candidate."));
        }

        return Result<ScoutingObservation>.Success(observation);
    }
    private static bool HasDistinctiveReference(
        IdentityNumber? identityNumber,
        LicenseNumber? licenseNumber,
        BirthDate? birthDate,
        ScoutingObservation initialObservation)
    {
        return identityNumber is not null ||
               licenseNumber is not null ||
               birthDate is not null ||
               !string.IsNullOrWhiteSpace(initialObservation.ObservedEvent) ||
               !string.IsNullOrWhiteSpace(initialObservation.ObservedClub) ||
               !string.IsNullOrWhiteSpace(initialObservation.ObservedTeam);
    }
}
