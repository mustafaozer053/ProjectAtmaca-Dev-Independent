using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.Enums;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Positions;
using ProjectAtmaca.Domain.Scouting;
using ProjectAtmaca.Domain.Scouting.Entities;
using ProjectAtmaca.Domain.Scouting.ValueObjects;

namespace ProjectAtmaca.Application.Scouting;

public sealed record ScoutingObservationInput(
    DateOnly ObservedOn,
    ObservationType ObservationType,
    string? ObservedEvent,
    string? ObservedClub,
    string? ObservedTeam,
    DominantFoot? DominantFoot,
    string? Strengths,
    string? Weaknesses,
    ObserverRecommendation? Recommendation,
    string? RecommendationNote,
    string ObserverName,
    int? Rating,
    IReadOnlyList<Guid>? PositionIds = null);

public sealed record CreateScoutingCandidateCommand(
    string Name,
    DateOnly? BirthDate,
    string? PhoneCountryCode,
    string? PhoneNumber,
    string? Email,
    InitialScoutingSourceType SourceType,
    string? ReferrerName,
    string? SourceDescription,
    ScoutingObservationInput Observation,
    IdentityType? IdentityType = null,
    string? IdentityNumber = null,
    string? IdentityCountryCode = null);

public sealed record UpdateScoutingCandidateCommand(
    string Name,
    DateOnly? BirthDate,
    string? PhoneCountryCode,
    string? PhoneNumber,
    string? Email,
    IdentityType? IdentityType = null,
    string? IdentityNumber = null,
    string? IdentityCountryCode = null);
public sealed record ScoutingObservationDetails(
    Guid Id,
    DateOnly ObservedOn,
    ObservationType ObservationType,
    string? ObservedEvent,
    string? ObservedClub,
    string? ObservedTeam,
    DominantFoot? DominantFoot,
    string? Strengths,
    string? Weaknesses,
    ObserverRecommendation? Recommendation,
    string? RecommendationNote,
    string ObserverName,
    int? Rating,
    IReadOnlyList<ScoutingPositionDetails> Positions);

public sealed record ScoutingPositionDetails(Guid Id, string Name);

public sealed record ScoutingCandidateListItem(
    Guid Id,
    string Name,
    DateOnly? BirthDate,
    ScoutingDecision Decision,
    int ObservationCount,
    DateOnly? LastObservedOn,
    double? AverageRating,
    string? LastObservedClub);

public sealed record ScoutingCandidateDetails(
    Guid Id,
    string Name,
    DateOnly? BirthDate,
    string? PhoneNumber,
    string? Email,
    InitialScoutingSourceType SourceType,
    string? ReferrerName,
    string? SourceDescription,
    ScoutingDecision Decision,
    IReadOnlyList<ScoutingObservationDetails> Observations,
    IdentityType? IdentityType = null,
    string? IdentityNumber = null,
    string? IdentityCountryCode = null,
    string? PhoneCountryCode = null,
    string? PhoneNationalNumber = null,
    Guid? RegisteredPersonId = null,
    Guid? RegisteredAtmacaCardId = null,
    string? RegisteredCardNumber = null);

public sealed record ScoutingRegistrationMatch(
    Guid PersonId,
    Guid AtmacaCardId,
    string CardNumber,
    string FullName,
    bool IsExactIdentity);

public interface IScoutingRegistrationLookup
{
    Task<Guid?> FindPersonIdByNationalIdAsync(string nationalIdentityNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> FindSimilarPersonIdsAsync(string fullName, DateOnly? birthDate, CancellationToken cancellationToken = default);
}

public static class ScoutingErrors
{
    public static readonly Error NotFound = Error.Create("Scouting.Candidate.NotFound", "The scouting candidate was not found.");
    public static readonly Error DuplicateIdentity = Error.Create("Scouting.Candidate.DuplicateIdentity", "A scouting candidate with this identity number already exists.");
    public static readonly Error PositionInvalid = Error.Create("Scouting.Position.Invalid", "One or more selected positions are invalid or inactive.");
    public static readonly Error InvalidIdentity = Error.Create("Scouting.Candidate.InvalidIdentity", "The identity number is invalid.");
    public static readonly Error ObservationNotFound = Error.Create("Scouting.Observation.NotFound", "The scouting observation was not found.");
    public static readonly Error InsufficientIdentification = Error.Create("Scouting.Candidate.InsufficientIdentification", "At least one identifying detail (identity number, birth date, event, club or team) is required.");
    public static readonly Error RegisteredPersonNotFound = Error.Create("Scouting.Registration.PersonNotFound", "The registered person or Atmaca Card was not found.");
    public static readonly Error PersonAlreadyLinked = Error.Create("Scouting.Registration.PersonAlreadyLinked", "This person is already linked to another scouting candidate.");
    public static readonly Error InvalidData = Error.Create("Scouting.Candidate.InvalidData", "The scouting candidate data is invalid.");
}

public sealed class ScoutingService(
    IActorAuthorizationService authorization,
    ICurrentActor currentActor,
    IScoutingCandidateRepository candidates,
    IPositionRepository positions,
    IAtmacaCardReader cardReader,
    IScoutingRegistrationLookup registrationLookup,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<Guid>> CreateAsync(
        CreateScoutingCandidateCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = await authorization.AuthorizeAsync(Permissions.Scouting.Create, cancellationToken);
        if (authorized.IsFailure)
            return Result<Guid>.Failure(authorized.Error!);

        Result<ScoutingCandidate> created;
        try
        {
            var name = PersonName.Create(command.Name);
            if (name.IsFailure)
                return Result<Guid>.Failure(name.Error!);
            var observerName = PersonName.Create(command.Observation.ObserverName);
            if (observerName.IsFailure)
                return Result<Guid>.Failure(observerName.Error!);

            PersonName? referrer = null;
            if (!string.IsNullOrWhiteSpace(command.ReferrerName))
            {
                var referrerResult = PersonName.Create(command.ReferrerName);
                if (referrerResult.IsFailure)
                    return Result<Guid>.Failure(referrerResult.Error!);
                referrer = referrerResult.Value;
            }

            IdentityNumber? identity = null;
            if (!string.IsNullOrWhiteSpace(command.IdentityNumber))
            {
                var identityResult = BuildIdentity(command.IdentityType, command.IdentityCountryCode, command.IdentityNumber);
                if (identityResult.IsFailure)
                    return Result<Guid>.Failure(identityResult.Error!);
                identity = identityResult.Value;
                if (await candidates.ExistsByIdentityNumberAsync(identity!, cancellationToken))
                    return Result<Guid>.Failure(ScoutingErrors.DuplicateIdentity);
            }

            var positionCheck = await ValidatePositionsAsync(command.Observation.PositionIds, cancellationToken);
            if (positionCheck.IsFailure)
                return Result<Guid>.Failure(positionCheck.Error!);

            var source = InitialScoutingSource.Create(command.SourceType, referrer, command.SourceDescription);
            if (source.IsFailure)
                return Result<Guid>.Failure(source.Error!);

            var observation = command.Observation;
            created = ScoutingCandidate.Create(
                name.Value!, identity, null,
                command.BirthDate is null ? null : BirthDate.Create(command.BirthDate.Value.ToDateTime(TimeOnly.MinValue)),
                null, null,
                ToPhone(command.PhoneCountryCode, command.PhoneNumber), null,
                string.IsNullOrWhiteSpace(command.Email) ? null : Email.Create(command.Email),
                null, source.Value!,
                observation.ObservedOn, observation.ObservationType, Clean(observation.ObservedEvent),
                Clean(observation.ObservedClub), Clean(observation.ObservedTeam), null, observation.DominantFoot, null,
                Clean(observation.Strengths), Clean(observation.Weaknesses), observation.Recommendation,
                Clean(observation.RecommendationNote), observerName.Value!, currentActor.ActorId.Value, observation.Rating);
        }
        catch (ArgumentException)
        {
            return Result<Guid>.Failure(ScoutingErrors.InvalidData);
        }

        if (created.IsFailure)
            return Result<Guid>.Failure(created.Error!);

        foreach (var positionId in command.Observation.PositionIds?.Distinct() ?? [])
        {
            var attached = created.Value!.AddObservationPosition(created.Value.Observations.Single().Id, positionId);
            if (attached.IsFailure)
                return Result<Guid>.Failure(attached.Error!);
        }

        created.Value!.SetCreatedBy(currentActor.ActorId);
        await candidates.AddAsync(created.Value!, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(created.Value!.Id);
    }

    public async Task<Result> AddObservationAsync(
        Guid candidateId,
        ScoutingObservationInput observation,
        CancellationToken cancellationToken = default)
    {
        var authorized = await authorization.AuthorizeAsync(Permissions.Scouting.Update, cancellationToken);
        if (authorized.IsFailure)
            return Result.Failure(authorized.Error!);

        var candidate = await candidates.GetByIdAsync(candidateId, cancellationToken);
        if (candidate is null)
            return Result.Failure(ScoutingErrors.NotFound);

        var observerName = PersonName.Create(observation.ObserverName);
        if (observerName.IsFailure)
            return Result.Failure(observerName.Error!);

        var positionCheck = await ValidatePositionsAsync(observation.PositionIds, cancellationToken);
        if (positionCheck.IsFailure)
            return positionCheck;

        var existingIds = candidate.Observations.Select(x => x.Id).ToHashSet();
        var added = candidate.AddObservation(
            observation.ObservedOn, observation.ObservationType, Clean(observation.ObservedEvent),
            Clean(observation.ObservedClub), Clean(observation.ObservedTeam), null, observation.DominantFoot, null,
            Clean(observation.Strengths), Clean(observation.Weaknesses), observation.Recommendation,
            Clean(observation.RecommendationNote), observerName.Value!, currentActor.ActorId.Value, observation.Rating);
        if (added.IsFailure)
            return added;

        var newObservation = candidate.Observations.Single(x => !existingIds.Contains(x.Id));
        foreach (var positionId in observation.PositionIds?.Distinct() ?? [])
        {
            var attached = candidate.AddObservationPosition(newObservation.Id, positionId);
            if (attached.IsFailure)
                return attached;
        }

        candidate.MarkAsModified(currentActor.ActorId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdateCandidateAsync(
        Guid candidateId,
        UpdateScoutingCandidateCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = await authorization.AuthorizeAsync(Permissions.Scouting.Update, cancellationToken);
        if (authorized.IsFailure)
            return Result.Failure(authorized.Error!);

        var candidate = await candidates.GetByIdAsync(candidateId, cancellationToken);
        if (candidate is null)
            return Result.Failure(ScoutingErrors.NotFound);

        try
        {
            var name = PersonName.Create(command.Name);
            if (name.IsFailure)
                return Result.Failure(name.Error!);

            IdentityNumber? identity = null;
            if (!string.IsNullOrWhiteSpace(command.IdentityNumber))
            {
                var identityResult = BuildIdentity(command.IdentityType, command.IdentityCountryCode, command.IdentityNumber);
                if (identityResult.IsFailure)
                    return Result.Failure(identityResult.Error!);
                identity = identityResult.Value;
                if (await candidates.ExistsByIdentityNumberAsync(identity!, candidate.Id, cancellationToken))
                    return Result.Failure(ScoutingErrors.DuplicateIdentity);
            }

            var birthDate = command.BirthDate is null
                ? null
                : BirthDate.Create(command.BirthDate.Value.ToDateTime(TimeOnly.MinValue));
            if (identity is null && birthDate is null &&
                !candidate.Observations.Any(x => x.ObservedEvent is not null || x.ObservedClub is not null || x.ObservedTeam is not null))
                return Result.Failure(ScoutingErrors.InsufficientIdentification);

            var renamed = candidate.ChangeName(name.Value!);
            if (renamed.IsFailure)
                return renamed;
            candidate.SetIdentityNumber(identity);
            candidate.SetBirthDate(birthDate);
            candidate.SetPrimaryPhoneNumber(ToPhone(command.PhoneCountryCode, command.PhoneNumber));
            candidate.SetEmail(string.IsNullOrWhiteSpace(command.Email) ? null : Email.Create(command.Email));
        }
        catch (ArgumentException)
        {
            return Result.Failure(ScoutingErrors.InvalidData);
        }

        candidates.RefreshIdentityKey(candidate);
        candidate.MarkAsModified(currentActor.ActorId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdateObservationAsync(
        Guid candidateId,
        Guid observationId,
        ScoutingObservationInput input,
        CancellationToken cancellationToken = default)
    {
        var authorized = await authorization.AuthorizeAsync(Permissions.Scouting.Update, cancellationToken);
        if (authorized.IsFailure)
            return Result.Failure(authorized.Error!);

        var candidate = await candidates.GetByIdAsync(candidateId, cancellationToken);
        if (candidate is null)
            return Result.Failure(ScoutingErrors.NotFound);
        var observation = candidate.Observations.FirstOrDefault(x => x.Id == observationId);
        if (observation is null)
            return Result.Failure(ScoutingErrors.ObservationNotFound);

        var observerName = PersonName.Create(input.ObserverName);
        if (observerName.IsFailure)
            return Result.Failure(observerName.Error!);
        var positionCheck = await ValidatePositionsAsync(input.PositionIds, cancellationToken);
        if (positionCheck.IsFailure)
            return positionCheck;

        var steps = new Func<Result>[]
        {
            () => candidate.CorrectObservationDate(observationId, input.ObservedOn),
            () => candidate.ChangeObservationType(observationId, input.ObservationType),
            () => candidate.ChangeObservationEvent(observationId, Clean(input.ObservedEvent)),
            () => candidate.ChangeObservationClub(observationId, Clean(input.ObservedClub)),
            () => candidate.ChangeObservationTeam(observationId, Clean(input.ObservedTeam)),
            () => candidate.SetObservationDominantFoot(observationId, input.DominantFoot),
            () => candidate.UpdateObservationStrengths(observationId, Clean(input.Strengths)),
            () => candidate.UpdateObservationWeaknesses(observationId, Clean(input.Weaknesses)),
            () => candidate.ChangeObservationRecommendation(observationId, input.Recommendation),
            () => candidate.UpdateObservationRecommendationNote(observationId, Clean(input.RecommendationNote)),
            () => candidate.ChangeObservationObserverName(observationId, observerName.Value!),
            () => candidate.SetObservationRating(observationId, input.Rating)
        };
        foreach (var step in steps)
        {
            var result = step();
            if (result.IsFailure)
                return result;
        }

        var wanted = input.PositionIds?.Distinct().ToHashSet() ?? [];
        foreach (var id in observation.PositionIds.Where(x => !wanted.Contains(x)).ToList())
        {
            var removed = candidate.RemoveObservationPosition(observationId, id);
            if (removed.IsFailure)
                return removed;
        }

        foreach (var id in wanted.Where(x => !observation.PositionIds.Contains(x)))
        {
            var added = candidate.AddObservationPosition(observationId, id);
            if (added.IsFailure)
                return added;
        }

        candidate.MarkAsModified(currentActor.ActorId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
    public async Task<Result> ChangeDecisionAsync(
        Guid candidateId,
        ScoutingDecision decision,
        CancellationToken cancellationToken = default)
    {
        var authorized = await authorization.AuthorizeAsync(Permissions.Scouting.Update, cancellationToken);
        if (authorized.IsFailure)
            return Result.Failure(authorized.Error!);

        var candidate = await candidates.GetByIdAsync(candidateId, cancellationToken);
        if (candidate is null)
            return Result.Failure(ScoutingErrors.NotFound);

        var changed = candidate.ChangeScoutingDecision(decision);
        if (changed.IsFailure)
            return changed;

        candidate.MarkAsModified(currentActor.ActorId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<ScoutingCandidateListItem>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var authorized = await authorization.AuthorizeAsync(Permissions.Scouting.List, cancellationToken);
        if (authorized.IsFailure)
            return Result<IReadOnlyList<ScoutingCandidateListItem>>.Failure(authorized.Error!);

        var records = await candidates.ListAsync(cancellationToken);
        return Result<IReadOnlyList<ScoutingCandidateListItem>>.Success(records.Select(ToListItem).ToList());
    }

    public async Task<Result<ScoutingCandidateDetails>> GetDetailsAsync(
        Guid candidateId,
        CancellationToken cancellationToken = default)
    {
        var authorized = await authorization.AuthorizeAsync(Permissions.Scouting.List, cancellationToken);
        if (authorized.IsFailure)
            return Result<ScoutingCandidateDetails>.Failure(authorized.Error!);

        var candidate = await candidates.GetByIdAsync(candidateId, cancellationToken);
        if (candidate is null)
            return Result<ScoutingCandidateDetails>.Failure(ScoutingErrors.NotFound);

        return Result<ScoutingCandidateDetails>.Success(new ScoutingCandidateDetails(
            candidate.Id, candidate.Name.FullName, ToDate(candidate.BirthDate),
            candidate.PrimaryPhoneNumber?.FullNumber, candidate.Email?.Value,
            candidate.InitialSource.SourceType, candidate.InitialSource.ReferrerName?.FullName,
            candidate.InitialSource.SourceDescription, candidate.ScoutingDecision,
            await ToObservationsAsync(candidate, cancellationToken),
            candidate.IdentityNumber?.IdentityType, candidate.IdentityNumber?.Number, candidate.IdentityNumber?.CountryCode,
            candidate.PrimaryPhoneNumber?.CountryCode, candidate.PrimaryPhoneNumber?.NationalNumber,
            candidate.RegisteredPersonId, candidate.RegisteredAtmacaCardId, candidate.RegisteredCardNumber));
    }

    public async Task<Result<IReadOnlyList<ScoutingRegistrationMatch>>> FindRegistrationMatchesAsync(
        Guid candidateId,
        CancellationToken cancellationToken = default)
    {
        var authorized = await authorization.AuthorizeAsync(Permissions.Scouting.List, cancellationToken);
        if (authorized.IsFailure)
            return Result<IReadOnlyList<ScoutingRegistrationMatch>>.Failure(authorized.Error!);

        var candidate = await candidates.GetByIdAsync(candidateId, cancellationToken);
        if (candidate is null)
            return Result<IReadOnlyList<ScoutingRegistrationMatch>>.Failure(ScoutingErrors.NotFound);

        var matches = new List<ScoutingRegistrationMatch>();
        if (candidate.RegisteredPersonId is not null)
            return Result<IReadOnlyList<ScoutingRegistrationMatch>>.Success(matches);

        var exactPersonId = candidate.IdentityNumber is { IdentityType: IdentityType.NationalId } identity
            ? await registrationLookup.FindPersonIdByNationalIdAsync(identity.Number, cancellationToken)
            : null;
        if (exactPersonId is not null)
            await AddMatchAsync(matches, exactPersonId.Value, true, cancellationToken);

        var birthDate = ToDate(candidate.BirthDate);
        foreach (var personId in await registrationLookup.FindSimilarPersonIdsAsync(
            candidate.Name.FullName, birthDate, cancellationToken))
            if (matches.All(x => x.PersonId != personId))
                await AddMatchAsync(matches, personId, false, cancellationToken);

        return Result<IReadOnlyList<ScoutingRegistrationMatch>>.Success(matches);
    }

    public async Task<Result> LinkRegistrationAsync(
        Guid candidateId,
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        var authorized = await authorization.AuthorizeAsync(Permissions.Scouting.Update, cancellationToken);
        if (authorized.IsFailure)
            return Result.Failure(authorized.Error!);

        var candidate = await candidates.GetByIdAsync(candidateId, cancellationToken);
        if (candidate is null)
            return Result.Failure(ScoutingErrors.NotFound);

        var card = await cardReader.GetByPersonIdAsync(personId, cancellationToken);
        if (card is null)
            return Result.Failure(ScoutingErrors.RegisteredPersonNotFound);

        if (await candidates.ExistsByRegisteredPersonAsync(personId, candidateId, cancellationToken))
            return Result.Failure(ScoutingErrors.PersonAlreadyLinked);

        var linked = candidate.LinkRegisteredPerson(card.PersonId, card.AtmacaCardId, card.CardNumber);
        if (linked.IsFailure)
            return linked;

        candidate.MarkAsModified(currentActor.ActorId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task AddMatchAsync(
        List<ScoutingRegistrationMatch> matches, Guid personId, bool exact, CancellationToken cancellationToken)
    {
        var card = await cardReader.GetByPersonIdAsync(personId, cancellationToken);
        if (card is not null)
            matches.Add(new(card.PersonId, card.AtmacaCardId, card.CardNumber, card.FullName, exact));
    }
    private static ScoutingCandidateListItem ToListItem(ScoutingCandidate x)
    {
        var last = x.Observations.OrderByDescending(o => o.ObservedOn).FirstOrDefault();
        var ratings = x.Observations.Where(o => o.Rating.HasValue).Select(o => (double)o.Rating!.Value).ToList();
        return new(x.Id, x.Name.FullName, ToDate(x.BirthDate), x.ScoutingDecision, x.Observations.Count,
            last?.ObservedOn, ratings.Count == 0 ? null : Math.Round(ratings.Average(), 1), last?.ObservedClub);
    }

    private async Task<IReadOnlyList<ScoutingObservationDetails>> ToObservationsAsync(
        ScoutingCandidate candidate, CancellationToken cancellationToken)
    {
        var names = new Dictionary<Guid, string>();
        foreach (var id in candidate.Observations.SelectMany(x => x.PositionIds).Distinct())
        {
            var position = await positions.GetByIdAsync(id, cancellationToken);
            if (position is not null)
                names[id] = position.Name;
        }

        return candidate.Observations.OrderByDescending(x => x.ObservedOn).Select(x => new ScoutingObservationDetails(
            x.Id, x.ObservedOn, x.ObservationType, x.ObservedEvent, x.ObservedClub, x.ObservedTeam, x.DominantFoot,
            x.Strengths, x.Weaknesses, x.ObserverRecommendation, x.RecommendationNote, x.ObserverName.FullName, x.Rating,
            x.PositionIds.Where(names.ContainsKey).Select(id => new ScoutingPositionDetails(id, names[id])).ToList()))
            .ToList();
    }

    private async Task<Result> ValidatePositionsAsync(IReadOnlyList<Guid>? positionIds, CancellationToken cancellationToken)
    {
        foreach (var id in positionIds?.Distinct() ?? [])
        {
            var position = await positions.GetByIdAsync(id, cancellationToken);
            if (position is null || !position.IsActive)
                return Result.Failure(ScoutingErrors.PositionInvalid);
        }

        return Result.Success();
    }

    private static Result<IdentityNumber> BuildIdentity(IdentityType? type, string? countryCode, string number)
    {
        var identityType = type ?? IdentityType.NationalId;
        var normalized = number.Trim();
        if (identityType == IdentityType.NationalId &&
            (normalized.Length != 11 || normalized.Any(c => c < '0' || c > '9')))
            return Result<IdentityNumber>.Failure(ScoutingErrors.InvalidIdentity);

        return IdentityNumber.Create(
            identityType == IdentityType.NationalId ? "TR" : string.IsNullOrWhiteSpace(countryCode) ? "TR" : countryCode,
            identityType, normalized);
    }

    private static DateOnly? ToDate(BirthDate? value) =>
        value is null ? null : DateOnly.FromDateTime(value.Value);

    private static PhoneNumber? ToPhone(string? countryCode, string? number) =>
        string.IsNullOrWhiteSpace(number) ? null : PhoneNumber.Create(
            string.IsNullOrWhiteSpace(countryCode) ? "90" : countryCode, number);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
