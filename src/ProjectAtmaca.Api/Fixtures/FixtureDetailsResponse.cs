using ProjectAtmaca.Application.Fixtures;
using ProjectAtmaca.Domain.Fixtures;

namespace ProjectAtmaca.Api.Fixtures;

public sealed record FixtureDetailsResponse(
    FixtureResponse Fixture,
    int? DurationMinutes,
    string? Referee,
    string? MatchNotes,
    int OurScore,
    int OpponentScore,
    IReadOnlyList<FixtureSquadMemberDetails> SquadMembers,
    IReadOnlyList<FixtureMatchEventDetails> MatchEvents,
    IReadOnlyList<FixtureScoreEventDetails> ScoreEvents,
    IReadOnlyList<FixtureCorrectionDetails> Corrections);

public sealed record ReopenFixtureForCorrectionRequest(string? Reason);
