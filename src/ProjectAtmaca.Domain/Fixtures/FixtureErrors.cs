using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Fixtures;

public static class FixtureErrors
{
    public static readonly Error SeasonTeamRequired = Error.Create("Fixture.SeasonTeam.Required", "A season team is required.");
    public static readonly Error SeasonTeamNotFound = Error.Create("Fixture.SeasonTeam.NotFound", "The season team was not found.");
    public static readonly Error SeasonTeamInactive = Error.Create("Fixture.SeasonTeam.Inactive", "The season team is inactive.");
    public static readonly Error NotFound = Error.Create("Fixture.NotFound", "The fixture was not found.");
    public static readonly Error CancelledCannotBeEdited = Error.Create("Fixture.Cancelled.CannotBeEdited", "A cancelled fixture cannot be edited.");
    public static readonly Error CompletedCannotBeEdited = Error.Create("Fixture.Completed.CannotBeEdited", "A completed fixture cannot be edited.");
    public static readonly Error CompletedCannotBeCancelled = Error.Create("Fixture.Completed.CannotBeCancelled", "A completed fixture cannot be cancelled.");
    public static readonly Error CorrectionInProgressCannotBeCancelled = Error.Create("Fixture.CorrectionInProgress.CannotBeCancelled", "A fixture being corrected cannot be cancelled.");
    public static readonly Error MatchDetailsRequired = Error.Create("Fixture.MatchDetails.Required", "Save match details before marking the fixture completed.");
    public static readonly Error CancelledCannotBeCompleted = Error.Create("Fixture.Cancelled.CannotBeCompleted", "A cancelled fixture cannot be marked completed.");
    public static readonly Error OnlyCompletedCanBeReopened = Error.Create("Fixture.Correction.OnlyCompleted", "Only a completed fixture can be reopened for correction.");
    public static readonly Error CorrectionReasonRequired = Error.Create("Fixture.Correction.ReasonRequired", "A correction reason is required.");
    public static readonly Error CorrectionReasonTooLong = Error.Create("Fixture.Correction.ReasonTooLong", "A correction reason cannot exceed 250 characters.");
    public static readonly Error CorrectionActorRequired = Error.Create("Fixture.Correction.ActorRequired", "A valid actor is required to reopen a fixture.");
    public static readonly Error TypeInvalid = Error.Create("Fixture.Type.Invalid", "The fixture type is invalid.");
    public static readonly Error OpponentInvalid = Error.Create("Fixture.Opponent.Invalid", "Opponent is required and must be at most 120 characters.");
    public static readonly Error DateRequired = Error.Create("Fixture.Date.Required", "A fixture date is required.");
    public static readonly Error VenueSideInvalid = Error.Create("Fixture.VenueSide.Invalid", "Home or away must be selected.");
    public static readonly Error VenueInvalid = Error.Create("Fixture.Venue.Invalid", "Venue is required and must be at most 200 characters.");
    public static readonly Error NotesTooLong = Error.Create("Fixture.Notes.TooLong", "Notes cannot exceed 1000 characters.");
    public static readonly Error DurationInvalid = Error.Create("Fixture.Duration.Invalid", "Match duration must be between 1 and 180 minutes.");
    public static readonly Error RefereeTooLong = Error.Create("Fixture.Referee.TooLong", "Referee information cannot exceed 200 characters.");
    public static readonly Error MatchNotesTooLong = Error.Create("Fixture.MatchNotes.TooLong", "Match notes cannot exceed 2000 characters.");
    public static readonly Error SquadInvalid = Error.Create("Fixture.Squad.Invalid", "The match squad contains an invalid or duplicate player.");
    public static readonly Error SquadMemberNotEligible = Error.Create("Fixture.SquadMember.NotEligible", "Every match squad member must be an active athlete on the season team on the fixture date.");
    public static readonly Error MatchEventInvalid = Error.Create("Fixture.MatchEvent.Invalid", "A match event is invalid or references a player outside the match squad. A substitution must replace a starting player with a substitute.");
    public static readonly Error ScoreEventInvalid = Error.Create("Fixture.ScoreEvent.Invalid", "A score event has an invalid side, score code, value, minute, or athlete.");
}
