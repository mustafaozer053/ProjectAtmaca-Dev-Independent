using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application
    .Participations.RecordArrival;

public sealed record RecordParticipationArrivalCommand(
    ParticipationId ParticipationId,
    DateTimeOffset JoinedAt);
