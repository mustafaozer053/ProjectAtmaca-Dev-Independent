using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.AtmacaCards;

public sealed class AtmacaCardMeasurement : Entity
{
    public Guid AtmacaCardId { get; private set; }
    public DateOnly MeasuredOn { get; private set; }
    public decimal? HeightCentimeters { get; private set; }
    public decimal? WeightKilograms { get; private set; }

    private AtmacaCardMeasurement()
    {
    }

    private AtmacaCardMeasurement(
        Guid id,
        Guid atmacaCardId,
        DateOnly measuredOn,
        decimal? heightCentimeters,
        decimal? weightKilograms)
        : base(id)
    {
        AtmacaCardId = atmacaCardId;
        MeasuredOn = measuredOn;
        HeightCentimeters = heightCentimeters;
        WeightKilograms = weightKilograms;
    }

    internal static Result<AtmacaCardMeasurement> Create(
        Guid atmacaCardId,
        DateOnly measuredOn,
        decimal? heightCentimeters,
        decimal? weightKilograms)
    {
        if (atmacaCardId == Guid.Empty)
            return Result<AtmacaCardMeasurement>.Failure(
                Error.Create("ATMACA_CARD_MEASUREMENT_CARD_REQUIRED", "AtmacaCard id is required."));

        if (measuredOn == default)
            return Result<AtmacaCardMeasurement>.Failure(
                Error.Create("ATMACA_CARD_MEASUREMENT_DATE_REQUIRED", "Measurement date is required."));

        if (heightCentimeters is null && weightKilograms is null)
            return Result<AtmacaCardMeasurement>.Failure(
                Error.Create("ATMACA_CARD_MEASUREMENT_VALUE_REQUIRED", "Height or weight must be provided."));

        if (heightCentimeters is < 30 or > 250)
            return Result<AtmacaCardMeasurement>.Failure(
                Error.Create("ATMACA_CARD_MEASUREMENT_HEIGHT_INVALID", "Height must be between 30 and 250 centimeters."));

        if (weightKilograms is < 1 or > 300)
            return Result<AtmacaCardMeasurement>.Failure(
                Error.Create("ATMACA_CARD_MEASUREMENT_WEIGHT_INVALID", "Weight must be between 1 and 300 kilograms."));

        return Result<AtmacaCardMeasurement>.Success(
            new AtmacaCardMeasurement(
                Guid.NewGuid(),
                atmacaCardId,
                measuredOn,
                heightCentimeters,
                weightKilograms));
    }
}
