using ProjectAtmaca.Domain.Fixtures;

namespace ProjectAtmaca.Api.Fixtures;

public sealed record CreateFixtureRequest(
    Guid SeasonTeamId,
    FixtureType Type,
    string Opponent,
    DateOnly Date,
    TimeOnly StartTime,
    string Venue,
    FixtureVenueSide VenueSide,
    string? Notes);
