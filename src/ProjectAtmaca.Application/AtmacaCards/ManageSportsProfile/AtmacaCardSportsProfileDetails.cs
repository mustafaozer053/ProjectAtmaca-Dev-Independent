using ProjectAtmaca.Domain.AtmacaCards;

namespace ProjectAtmaca.Application.AtmacaCards.ManageSportsProfile;

public sealed record AtmacaCardSportsProfileDetails(
    Guid Id,
    string SportName,
    string? LicenseNumber,
    DateOnly? StartedSportOn,
    DateOnly? ClubRegisteredOn,
    AthleteCompetitionLevel? CompetitionLevel,
    bool? IsNationalAthlete,
    IReadOnlyList<AtmacaCardPositionDetails> Positions);

public sealed record AtmacaCardPositionDetails(Guid Id, string Code, string Name);
