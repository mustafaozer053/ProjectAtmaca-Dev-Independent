using ProjectAtmaca.Domain.AtmacaCards;

namespace ProjectAtmaca.Application.AtmacaCards.ManageSportsProfile;

public sealed record UpsertAtmacaCardSportsProfileCommand(
    AtmacaCardId AtmacaCardId,
    string SportName,
    string? LicenseNumber,
    DateOnly? StartedSportOn,
    DateOnly? ClubRegisteredOn,
    AthleteCompetitionLevel? CompetitionLevel,
    bool? IsNationalAthlete,
    IReadOnlyCollection<Guid>? PositionIds = null);
