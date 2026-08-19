using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application
    .Participations.MarkPresent;

public sealed record MarkParticipationPresentCommand(
    ParticipationId ParticipationId,
    ParticipationCondition? Condition);
