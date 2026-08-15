using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application
    .Participations.Create;

public sealed record CreateParticipationCommand(
    ActivityReference ActivityReference,
    AtmacaCardId AtmacaCardId);