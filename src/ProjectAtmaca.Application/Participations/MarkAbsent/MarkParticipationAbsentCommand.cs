using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application.Participations.MarkAbsent;

public sealed record MarkParticipationAbsentCommand(
    ParticipationId ParticipationId);
