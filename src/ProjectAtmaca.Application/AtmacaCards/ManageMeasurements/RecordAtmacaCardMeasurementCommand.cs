using ProjectAtmaca.Domain.AtmacaCards;

namespace ProjectAtmaca.Application.AtmacaCards.ManageMeasurements;

public sealed record RecordAtmacaCardMeasurementCommand(
    AtmacaCardId AtmacaCardId,
    DateOnly MeasuredOn,
    decimal? HeightCentimeters,
    decimal? WeightKilograms);
