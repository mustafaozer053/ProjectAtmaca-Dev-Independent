using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application
    .Participations.RecordDeparture;

public sealed record RecordParticipationDepartureCommand(
    ParticipationId ParticipationId,
    DateTimeOffset LeftAt);
