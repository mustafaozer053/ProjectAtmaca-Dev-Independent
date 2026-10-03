using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Positions;

namespace ProjectAtmaca.Domain.AtmacaCards;

public sealed class AtmacaCard : AuditableAggregateRoot
{
    private readonly List<AtmacaCardSportsProfile> _sportsProfiles = [];
    private readonly List<AtmacaCardMeasurement> _measurements = [];
    private readonly List<AtmacaCardDocument> _documents = [];

    public AtmacaCardId AtmacaCardId =>
        AtmacaCardId.From(Id);
    public Guid PersonId { get; private set; }

    public AtmacaCardNumber CardNumber { get; private set; }

    public DateTime IssuedAtUtc { get; private set; }

    public bool IsActive { get; private set; } = true;

    public string? PhotoStorageKey { get; private set; }

    public string? PhotoContentType { get; private set; }

    public bool IsCurrentlyStudying { get; private set; }

    public string? SchoolName { get; private set; }

    public string? SchoolGrade { get; private set; }

    public string? SchoolNumber { get; private set; }

    public IReadOnlyCollection<AtmacaCardSportsProfile> SportsProfiles =>
        _sportsProfiles.AsReadOnly();

    public IReadOnlyCollection<AtmacaCardMeasurement> Measurements =>
        _measurements.AsReadOnly();

    public IReadOnlyCollection<AtmacaCardDocument> Documents =>
        _documents.AsReadOnly();

    private AtmacaCard(
        Guid personId,
        AtmacaCardNumber cardNumber,
        DateTime issuedAtUtc)
    {
        PersonId = personId;
        CardNumber = cardNumber;
        IssuedAtUtc = issuedAtUtc;
    }

    public static Result<AtmacaCard> Issue(
        Guid personId,
        AtmacaCardNumber cardNumber,
        DateTime issuedAtUtc)
    {
        if (personId == Guid.Empty)
            return Result<AtmacaCard>.Failure(Error.Create("ATMACA_CARD_PERSON_REQUIRED", "Person id is required."));

        if (cardNumber is null)
        {
            return Result<AtmacaCard>.Failure(
                Error.Create(
                    "ATMACA_CARD_NUMBER_REQUIRED",
                    "AtmacaCard number is required."));
        }

        if (issuedAtUtc == default)
        {
            return Result<AtmacaCard>.Failure(
                Error.Create("ATMACA_CARD_ISSUED_AT_REQUIRED", "Issuance time is required."));
        }

        if (issuedAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result<AtmacaCard>.Failure(
                Error.Create("ATMACA_CARD_ISSUED_AT_UTC_REQUIRED", "Issuance time must be UTC."));
        }

        var card = new AtmacaCard(
            personId,
            cardNumber,
            issuedAtUtc);

        return Result<AtmacaCard>.Success(card);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public Result SetPhoto(string storageKey, string contentType)
    {
        if (string.IsNullOrWhiteSpace(storageKey) ||
            storageKey.Length > 200 ||
            Path.IsPathRooted(storageKey) ||
            storageKey.Contains("..", StringComparison.Ordinal))
        {
            return Result.Failure(Error.Create(
                "ATMACA_CARD_PHOTO_STORAGE_KEY_INVALID",
                "Photo storage key is invalid."));
        }

        if (contentType is not ("image/jpeg" or "image/png"))
        {
            return Result.Failure(Error.Create(
                "ATMACA_CARD_PHOTO_CONTENT_TYPE_INVALID",
                "Photo content type must be JPEG or PNG."));
        }

        PhotoStorageKey = storageKey;
        PhotoContentType = contentType;
        return Result.Success();
    }

    public Result UpdateEducation(
        bool isCurrentlyStudying,
        string? schoolName,
        string? schoolGrade,
        string? schoolNumber)
    {
        string? normalizedSchoolName = NormalizeOptionalText(schoolName);
        string? normalizedSchoolGrade = NormalizeOptionalText(schoolGrade);
        string? normalizedSchoolNumber = NormalizeOptionalText(schoolNumber);

        if (isCurrentlyStudying &&
            normalizedSchoolName is { Length: > 150 })
        {
            return Result.Failure(Error.Create(
                "ATMACA_CARD_SCHOOL_NAME_INVALID",
                "School name cannot exceed 150 characters."));
        }

        if (isCurrentlyStudying &&
            normalizedSchoolGrade is { Length: > 50 })
        {
            return Result.Failure(Error.Create(
                "ATMACA_CARD_SCHOOL_GRADE_INVALID",
                "School grade cannot exceed 50 characters."));
        }

        if (isCurrentlyStudying &&
            normalizedSchoolNumber is { Length: > 30 })
        {
            return Result.Failure(Error.Create(
                "ATMACA_CARD_SCHOOL_NUMBER_INVALID",
                "School number cannot exceed 30 characters."));
        }

        IsCurrentlyStudying = isCurrentlyStudying;
        SchoolName = isCurrentlyStudying ? normalizedSchoolName : null;
        SchoolGrade = isCurrentlyStudying ? normalizedSchoolGrade : null;
        SchoolNumber = isCurrentlyStudying ? normalizedSchoolNumber : null;
        return Result.Success();
    }

    public Result<AtmacaCardSportsProfile> UpsertSportsProfile(
        string sportName,
        string? licenseNumber,
        DateOnly? startedSportOn,
        DateOnly? clubRegisteredOn,
        AthleteCompetitionLevel? competitionLevel,
        bool? isNationalAthlete,
        IReadOnlyCollection<Position>? positions = null)
    {
        var profileResult = AtmacaCardSportsProfile.Create(
            Id,
            sportName,
            licenseNumber,
            startedSportOn,
            clubRegisteredOn,
            competitionLevel,
            isNationalAthlete);
        if (profileResult.IsFailure)
            return profileResult;

        AtmacaCardSportsProfile profile = profileResult.Value!;
        if (positions is not null)
        {
            var positionsResult = profile.SetPositions(positions);
            if (positionsResult.IsFailure)
                return Result<AtmacaCardSportsProfile>.Failure(positionsResult.Error!);
        }

        AtmacaCardSportsProfile? existing = _sportsProfiles.SingleOrDefault(
            x => string.Equals(x.SportName, profile.SportName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.ReplaceWith(profile, positions is not null);
            return Result<AtmacaCardSportsProfile>.Success(existing);
        }

        _sportsProfiles.Add(profile);
        return Result<AtmacaCardSportsProfile>.Success(profile);
    }

    public Result<AtmacaCardMeasurement> AddMeasurement(
        DateOnly measuredOn,
        decimal? heightCentimeters,
        decimal? weightKilograms)
    {
        if (_measurements.Any(x => x.MeasuredOn == measuredOn))
            return Result<AtmacaCardMeasurement>.Failure(
                Error.Create(
                    "ATMACA_CARD_MEASUREMENT_DATE_DUPLICATE",
                    "A measurement already exists for this date."));

        var result = AtmacaCardMeasurement.Create(
            Id,
            measuredOn,
            heightCentimeters,
            weightKilograms);
        if (result.IsFailure)
            return result;

        _measurements.Add(result.Value!);
        return result;
    }

    public Result<AtmacaCardDocument> AddDocument(
        AtmacaCardDocumentType documentType,
        string title,
        string? issuer,
        DateOnly? issuedOn,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        string storageKey)
    {
        var result = AtmacaCardDocument.Create(
            Id,
            documentType,
            title,
            issuer,
            issuedOn,
            originalFileName,
            contentType,
            fileSizeBytes,
            storageKey);
        if (result.IsFailure)
            return result;

        _documents.Add(result.Value!);
        return result;
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
