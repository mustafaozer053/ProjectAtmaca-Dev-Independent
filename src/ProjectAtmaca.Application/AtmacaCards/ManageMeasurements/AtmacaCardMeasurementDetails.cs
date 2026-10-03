namespace ProjectAtmaca.Application.AtmacaCards.ManageMeasurements;

public sealed record AtmacaCardMeasurementDetails(
    Guid Id,
    DateOnly MeasuredOn,
    decimal? HeightCentimeters,
    decimal? WeightKilograms);
