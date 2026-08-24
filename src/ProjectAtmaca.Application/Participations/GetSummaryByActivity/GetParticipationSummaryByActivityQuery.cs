using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application
    .Participations.GetSummaryByActivity;

public sealed record GetParticipationSummaryByActivityQuery(
    ActivityReference ActivityReference);
