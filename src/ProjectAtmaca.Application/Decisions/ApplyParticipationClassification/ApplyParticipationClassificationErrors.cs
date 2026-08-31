using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;

public static class ApplyParticipationClassificationErrors
{
    public static readonly Error DecisionNotFound =
        Error.Create(
            "Decision.NotFound",
            "Decision was not found.");

    public static readonly Error RevisionMismatch =
        Error.Create(
            "Decision.RevisionMismatch",
            "The requested decision revision is not current.");

    public static readonly Error ParticipationNotFound =
        Error.Create(
            "Participation.NotFound",
            "The target participation was not found.");

    public static readonly Error DecisionSuperseded =
        Error.Create(
            "Decision.Superseded",
            "A superseded decision cannot be applied.");

    public static readonly Error DecisionAuthorityLost =
        Error.Create(
            "Decision.AuthorityLost",
            "Decision authority was lost before the application could be committed.");
}
