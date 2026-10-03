using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Positions;

namespace ProjectAtmaca.Domain.AtmacaCards;

public sealed class AtmacaCardSportsProfile : AuditableAggregateRoot
{
    private readonly List<Position> _positions = [];

    public Guid AtmacaCardId { get; private set; }
    public string SportName { get; private set; }
    public LicenseNumber? LicenseNumber { get; private set; }
    public DateOnly? StartedSportOn { get; private set; }
    public DateOnly? ClubRegisteredOn { get; private set; }
    public AthleteCompetitionLevel? CompetitionLevel { get; private set; }
    public bool? IsNationalAthlete { get; private set; }
    public IReadOnlyCollection<Position> Positions => _positions.AsReadOnly();

    private AtmacaCardSportsProfile()
    {
        SportName = null!;
    }

    private AtmacaCardSportsProfile(
        Guid id,
        Guid atmacaCardId,
        string sportName,
        LicenseNumber? licenseNumber,
        DateOnly? startedSportOn,
        DateOnly? clubRegisteredOn,
        AthleteCompetitionLevel? competitionLevel,
        bool? isNationalAthlete)
        : base(id)
    {
        AtmacaCardId = atmacaCardId;
        SportName = sportName;
        LicenseNumber = licenseNumber;
        StartedSportOn = startedSportOn;
        ClubRegisteredOn = clubRegisteredOn;
        CompetitionLevel = competitionLevel;
        IsNationalAthlete = isNationalAthlete;
    }

    internal static Result<AtmacaCardSportsProfile> Create(
        Guid atmacaCardId,
        string sportName,
        string? licenseNumber,
        DateOnly? startedSportOn,
        DateOnly? clubRegisteredOn,
        AthleteCompetitionLevel? competitionLevel,
        bool? isNationalAthlete)
    {
        if (atmacaCardId == Guid.Empty)
            return Result<AtmacaCardSportsProfile>.Failure(
                Error.Create("ATMACA_CARD_SPORTS_PROFILE_CARD_REQUIRED", "AtmacaCard id is required."));

        string normalizedSportName = sportName?.Trim() ?? string.Empty;
        if (normalizedSportName.Length is < 2 or > 80)
            return Result<AtmacaCardSportsProfile>.Failure(
                Error.Create("ATMACA_CARD_SPORTS_PROFILE_SPORT_INVALID", "Sport name must be between 2 and 80 characters."));

        if (competitionLevel.HasValue &&
            !Enum.IsDefined(competitionLevel.Value))
            return Result<AtmacaCardSportsProfile>.Failure(
                Error.Create("ATMACA_CARD_SPORTS_PROFILE_LEVEL_INVALID", "Competition level is invalid."));

        LicenseNumber? license = null;
        if (!string.IsNullOrWhiteSpace(licenseNumber))
        {
            var licenseResult = LicenseNumber.Create(licenseNumber);
            if (licenseResult.IsFailure)
                return Result<AtmacaCardSportsProfile>.Failure(licenseResult.Error!);
            license = licenseResult.Value;
        }

        return Result<AtmacaCardSportsProfile>.Success(
            new AtmacaCardSportsProfile(
                Guid.NewGuid(),
                atmacaCardId,
                normalizedSportName,
                license,
                startedSportOn,
                clubRegisteredOn,
                competitionLevel,
                isNationalAthlete));
    }

    internal Result SetPositions(IReadOnlyCollection<Position> positions)
    {
        ArgumentNullException.ThrowIfNull(positions);

        if (positions.Any(position =>
                !string.Equals(position.SportName, SportName, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure(
                Error.Create(
                    "ATMACA_CARD_SPORTS_PROFILE_POSITION_SPORT_MISMATCH",
                    "Selected positions must belong to the profile sport."));
        }

        if (positions.Select(position => position.Id).Distinct().Count() != positions.Count)
        {
            return Result.Failure(
                Error.Create(
                    "ATMACA_CARD_SPORTS_PROFILE_POSITION_DUPLICATE",
                    "A position cannot be selected more than once."));
        }

        _positions.Clear();
        _positions.AddRange(positions);
        return Result.Success();
    }

    internal void ReplaceWith(AtmacaCardSportsProfile profile, bool replacePositions)
    {
        SportName = profile.SportName;
        LicenseNumber = profile.LicenseNumber;
        StartedSportOn = profile.StartedSportOn;
        ClubRegisteredOn = profile.ClubRegisteredOn;
        CompetitionLevel = profile.CompetitionLevel;
        IsNationalAthlete = profile.IsNationalAthlete;
        if (replacePositions)
        {
            _positions.Clear();
            _positions.AddRange(profile._positions);
        }
    }
}
