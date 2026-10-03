using ProjectAtmaca.Domain.Fixtures;

namespace ProjectAtmaca.Api.Fixtures.MatchDetails;

public sealed record UpdateFixtureMatchDetailsRequest(
    int DurationMinutes,
    string? Referee,
    string? MatchNotes,
    IReadOnlyList<FixtureSquadMemberInput> Squad,
    IReadOnlyList<FixtureMatchEventInput> MatchEvents,
    IReadOnlyList<FixtureScoreEventInput> ScoreEvents);
