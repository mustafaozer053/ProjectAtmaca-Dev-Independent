using FluentAssertions;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Fixtures;
using Xunit;

namespace ProjectAtmaca.Domain.Tests.Fixtures;

public sealed class FixtureTests
{
    [Fact]
    public void Create_ShouldKeepTrimmedFixtureDetails()
    {
        var date = new DateOnly(2026, 10, 4);
        var time = new TimeOnly(15, 30);

        var result = Fixture.Create(
            Guid.NewGuid(),
            FixtureType.Friendly,
            "  Rakip Akademi ",
            date,
            time,
            "  Ana saha ",
            FixtureVenueSide.Home,
            "  Hazırlık maçı ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Type.Should().Be(FixtureType.Friendly);
        result.Value.Opponent.Should().Be("Rakip Akademi");
        result.Value.Date.Should().Be(date);
        result.Value.StartTime.Should().Be(time);
        result.Value.Venue.Should().Be("Ana saha");
        result.Value.VenueSide.Should().Be(FixtureVenueSide.Home);
        result.Value.Notes.Should().Be("Hazırlık maçı");
    }

    [Fact]
    public void Create_ShouldRejectMissingOpponent()
    {
        var result = Fixture.Create(
            Guid.NewGuid(),
            FixtureType.Official,
            " ",
            new DateOnly(2026, 10, 4),
            new TimeOnly(15, 30),
            "Ana saha",
            FixtureVenueSide.Away,
            null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FixtureErrors.OpponentInvalid);
    }

    [Fact]
    public void Cancel_ShouldPreserveFixtureAndPreventEditing()
    {
        var fixture = Fixture.Create(
            Guid.NewGuid(), FixtureType.Official, "Rakip",
            new DateOnly(2026, 10, 4), new TimeOnly(15, 30),
            "Ana saha", FixtureVenueSide.Home, null).Value!;

        fixture.Cancel().IsSuccess.Should().BeTrue();
        fixture.Status.Should().Be(FixtureStatus.Cancelled);

        var update = fixture.UpdateDetails(
            fixture.SeasonTeamId, FixtureType.Official, "Yeni rakip",
            new DateOnly(2026, 10, 5), new TimeOnly(16, 0),
            "Yeni saha", FixtureVenueSide.Away, null);

        update.Error.Should().Be(FixtureErrors.CancelledCannotBeEdited);
        fixture.Opponent.Should().Be("Rakip");
    }

    [Fact]
    public void Complete_ShouldRequireSavedMatchDetails_AndLockFixture()
    {
        var fixture = CreateFixture();

        fixture.Complete().Error.Should().Be(FixtureErrors.MatchDetailsRequired);

        fixture.UpdateMatchDetails(90, null, null, [], [], [])
            .IsSuccess.Should().BeTrue();
        fixture.Complete().IsSuccess.Should().BeTrue();
        fixture.Status.Should().Be(FixtureStatus.Completed);
        fixture.Complete().IsSuccess.Should().BeTrue();

        fixture.UpdateMatchDetails(90, null, null, [], [], [])
            .Error.Should().Be(FixtureErrors.CompletedCannotBeEdited);
        fixture.UpdateDetails(
            fixture.SeasonTeamId, fixture.Type, "Yeni rakip",
            fixture.Date, fixture.StartTime, fixture.Venue, fixture.VenueSide, null)
            .Error.Should().Be(FixtureErrors.CompletedCannotBeEdited);
        fixture.Cancel().Error.Should().Be(FixtureErrors.CompletedCannotBeCancelled);
    }

    [Fact]
    public void Complete_ShouldRejectCancelledFixture()
    {
        var fixture = CreateFixture();
        fixture.Cancel().IsSuccess.Should().BeTrue();

        fixture.Complete().Error.Should().Be(FixtureErrors.CancelledCannotBeCompleted);
    }

    [Fact]
    public void ReopenForCorrection_ShouldRequireReasonAndCompletedFixture()
    {
        var fixture = CreateFixture();
        fixture.UpdateMatchDetails(90, null, null, [], [], []);
        fixture.Complete();

        fixture.ReopenForCorrection(null, ActorId.New()).Error
            .Should().Be(FixtureErrors.CorrectionReasonRequired);
        fixture.ReopenForCorrection(
            new string('x', FixtureCorrection.MaxReasonLength + 1),
            ActorId.New()).Error.Should().Be(FixtureErrors.CorrectionReasonTooLong);

        fixture.Status.Should().Be(FixtureStatus.Completed);
        fixture.Corrections.Should().BeEmpty();
    }

    [Fact]
    public void ReopenForCorrection_ShouldPreserveFixtureAndRecordReasonAndActor()
    {
        var fixture = CreateFixture();
        fixture.UpdateMatchDetails(
            90, "Hakem", null, [], [],
            [new FixtureScoreEventInput(
                FixtureScoreSide.SeasonTeam, "FOOTBALL_GOAL", 1, 20, null)]);
        fixture.Complete();
        var actorId = ActorId.New();

        var result = fixture.ReopenForCorrection("  Gol dakikası yanlış  ", actorId);

        result.IsSuccess.Should().BeTrue();
        fixture.Status.Should().Be(FixtureStatus.CorrectionInProgress);
        fixture.Opponent.Should().Be("Rakip");
        fixture.DurationMinutes.Should().Be(90);
        fixture.OurScore.Should().Be(1);
        var correction = fixture.Corrections.Should().ContainSingle().Which;
        correction.Reason.Should().Be("Gol dakikası yanlış");
        correction.ReopenedByActorId.Should().Be(actorId);
        correction.ReopenedAtUtc.Should().BeOnOrBefore(DateTime.UtcNow);
        fixture.Cancel().Error.Should()
            .Be(FixtureErrors.CorrectionInProgressCannotBeCancelled);

        fixture.UpdateMatchDetails(
            90, "Hakem", null, [], [],
            [
                new FixtureScoreEventInput(
                    FixtureScoreSide.SeasonTeam, "FOOTBALL_GOAL", 2, 20, null)
            ]).IsSuccess.Should().BeTrue();
        fixture.Complete().IsSuccess.Should().BeTrue();
        fixture.Status.Should().Be(FixtureStatus.Completed);
        fixture.OurScore.Should().Be(2);
        fixture.Corrections.Should().ContainSingle();
    }

    [Fact]
    public void ReopenForCorrection_ShouldOnlyAllowCompletedFixtures()
    {
        var fixture = CreateFixture();

        fixture.ReopenForCorrection("Düzeltme", ActorId.New()).Error
            .Should().Be(FixtureErrors.OnlyCompletedCanBeReopened);

        fixture.Cancel();
        fixture.ReopenForCorrection("Düzeltme", ActorId.New()).Error
            .Should().Be(FixtureErrors.OnlyCompletedCanBeReopened);
    }

    [Fact]
    public void UpdateMatchDetails_ShouldDeriveScoreFromGoalEvents()
    {
        var fixture = CreateFixture();
        var starter = Guid.NewGuid();
        var substitute = Guid.NewGuid();

        var result = fixture.UpdateMatchDetails(
            40,
            "Hakem",
            "Gelişim ligi",
            [
                new FixtureSquadMemberInput(starter, FixtureSquadRole.Starter),
                new FixtureSquadMemberInput(substitute, FixtureSquadRole.Substitute)
            ],
            [
                new FixtureMatchEventInput(
                    FixtureMatchEventType.Substitution, 30, starter, substitute),
                new FixtureMatchEventInput(
                    FixtureMatchEventType.YellowCard, 38, starter)
            ],
            [
                new FixtureScoreEventInput(
                    FixtureScoreSide.SeasonTeam, "FOOTBALL_GOAL", 1, 12, starter),
                new FixtureScoreEventInput(
                    FixtureScoreSide.SeasonTeam, "FOOTBALL_GOAL", 1, 35, substitute),
                new FixtureScoreEventInput(
                    FixtureScoreSide.Opponent, "FOOTBALL_GOAL", 1, 23, null)
            ]);

        result.IsSuccess.Should().BeTrue();
        fixture.DurationMinutes.Should().Be(40);
        fixture.OurScore.Should().Be(2);
        fixture.OpponentScore.Should().Be(1);
        fixture.Referee.Should().Be("Hakem");
        fixture.SquadMembers.Should().HaveCount(2);
        fixture.MatchEvents.Should().HaveCount(2);
        fixture.ScoreEvents.Should().HaveCount(3);
    }

    [Fact]
    public void UpdateMatchDetails_ShouldRejectMatchEventForUnselectedPlayer()
    {
        var fixture = CreateFixture();

        var result = fixture.UpdateMatchDetails(
            90,
            null,
            null,
            [],
            [new FixtureMatchEventInput(
                FixtureMatchEventType.RedCard, 70, Guid.NewGuid())],
            []);

        result.Error.Should().Be(FixtureErrors.MatchEventInvalid);
        fixture.MatchEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateMatchDetails_ShouldRejectSubstitutionBetweenTwoStarters()
    {
        var fixture = CreateFixture();
        var outgoing = Guid.NewGuid();
        var incoming = Guid.NewGuid();

        var result = fixture.UpdateMatchDetails(
            90,
            null,
            null,
            [
                new FixtureSquadMemberInput(outgoing, FixtureSquadRole.Starter),
                new FixtureSquadMemberInput(incoming, FixtureSquadRole.Starter)
            ],
            [
                new FixtureMatchEventInput(
                    FixtureMatchEventType.Substitution, 30, outgoing, incoming)
            ],
            []);

        result.Error.Should().Be(FixtureErrors.MatchEventInvalid);
        fixture.MatchEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateMatchDetails_ShouldRejectSubstitutionWithIncomingStarter()
    {
        var fixture = CreateFixture();
        var outgoing = Guid.NewGuid();
        var incoming = Guid.NewGuid();

        var result = fixture.UpdateMatchDetails(
            90,
            null,
            null,
            [
                new FixtureSquadMemberInput(outgoing, FixtureSquadRole.Substitute),
                new FixtureSquadMemberInput(incoming, FixtureSquadRole.Substitute)
            ],
            [
                new FixtureMatchEventInput(
                    FixtureMatchEventType.Substitution, 30, outgoing, incoming)
            ],
            []);

        result.Error.Should().Be(FixtureErrors.MatchEventInvalid);
        fixture.MatchEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateMatchDetails_ShouldCalculateScoreFromGenericScoreValues()
    {
        var fixture = CreateFixture();

        var result = fixture.UpdateMatchDetails(
            90,
            null,
            null,
            [],
            [],
            [
                new FixtureScoreEventInput(
                    FixtureScoreSide.SeasonTeam,
                    "BASKETBALL_TWO_POINT",
                    2,
                    null,
                    null),
                new FixtureScoreEventInput(
                    FixtureScoreSide.SeasonTeam,
                    "BASKETBALL_FREE_THROW",
                    1,
                    18,
                    null),
                new FixtureScoreEventInput(
                    FixtureScoreSide.Opponent,
                    "BASKETBALL_THREE_POINT",
                    3,
                    22,
                    null)
            ]);

        result.IsSuccess.Should().BeTrue();
        fixture.OurScore.Should().Be(3);
        fixture.OpponentScore.Should().Be(3);
    }

    [Fact]
    public void GetPlayerStatistics_ShouldCalculateMinutesGoalsAssistsAndCards()
    {
        var fixture = CreateFixture();
        var starter = Guid.NewGuid();
        var redCarded = Guid.NewGuid();
        var substitute = Guid.NewGuid();
        var unused = Guid.NewGuid();

        var result = fixture.UpdateMatchDetails(
            40,
            null,
            null,
            [
                new FixtureSquadMemberInput(starter, FixtureSquadRole.Starter),
                new FixtureSquadMemberInput(redCarded, FixtureSquadRole.Starter),
                new FixtureSquadMemberInput(substitute, FixtureSquadRole.Substitute),
                new FixtureSquadMemberInput(unused, FixtureSquadRole.Substitute)
            ],
            [
                new FixtureMatchEventInput(
                    FixtureMatchEventType.Substitution, 30, starter, substitute),
                new FixtureMatchEventInput(FixtureMatchEventType.RedCard, 25, redCarded),
                new FixtureMatchEventInput(FixtureMatchEventType.YellowCard, 10, starter)
            ],
            [
                new FixtureScoreEventInput(
                    FixtureScoreSide.SeasonTeam, "FOOTBALL_GOAL", 1, 35, substitute, starter)
            ]);

        result.IsSuccess.Should().BeTrue();
        var stats = fixture.GetPlayerStatistics().ToDictionary(x => x.AtmacaCardId);
        stats[starter].MinutesPlayed.Should().Be(30);
        stats[starter].Assists.Should().Be(1);
        stats[starter].YellowCards.Should().Be(1);
        stats[redCarded].MinutesPlayed.Should().Be(25);
        stats[redCarded].RedCards.Should().Be(1);
        stats[substitute].MinutesPlayed.Should().Be(10);
        stats[substitute].Goals.Should().Be(1);
        stats[unused].MinutesPlayed.Should().Be(0);
        stats[unused].Appeared.Should().BeFalse();
    }

    [Fact]
    public void UpdateMatchDetails_ShouldRejectAssistByScorer()
    {
        var fixture = CreateFixture();
        var player = Guid.NewGuid();

        var result = fixture.UpdateMatchDetails(
            90,
            null,
            null,
            [new FixtureSquadMemberInput(player, FixtureSquadRole.Starter)],
            [],
            [new FixtureScoreEventInput(
                FixtureScoreSide.SeasonTeam, "FOOTBALL_GOAL", 1, 10, player, player)]);

        result.Error.Should().Be(FixtureErrors.ScoreEventInvalid);
    }

    private static Fixture CreateFixture() =>
        Fixture.Create(
            Guid.NewGuid(), FixtureType.Official, "Rakip",
            new DateOnly(2026, 10, 4), new TimeOnly(15, 30),
            "Ana saha", FixtureVenueSide.Home, null).Value!;
}
