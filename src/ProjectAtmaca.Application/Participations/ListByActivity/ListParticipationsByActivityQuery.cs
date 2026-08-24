using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application
    .Participations.ListByActivity;

public sealed record ListParticipationsByActivityQuery(
    ActivityReference ActivityReference);
