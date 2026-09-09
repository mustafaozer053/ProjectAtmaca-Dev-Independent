namespace ProjectAtmaca.Api.Participations.Create;

public sealed record CreateParticipationRequest(
    string ActivityTypeCode,
    Guid ActivityId,
    Guid AtmacaCardId);
