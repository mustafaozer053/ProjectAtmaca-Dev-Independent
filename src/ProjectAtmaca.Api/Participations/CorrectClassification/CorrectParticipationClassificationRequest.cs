using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Api.Participations.CorrectClassification;

public sealed record CorrectParticipationClassificationRequest(
    ParticipationStatus Status,
    bool IsBta = false);
