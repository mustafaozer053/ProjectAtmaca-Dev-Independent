using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application.Participations.CorrectClassification;

public sealed record CorrectParticipationClassificationCommand(
    ParticipationId ParticipationId,
    ParticipationStatus Status,
    bool IsBta = false);
