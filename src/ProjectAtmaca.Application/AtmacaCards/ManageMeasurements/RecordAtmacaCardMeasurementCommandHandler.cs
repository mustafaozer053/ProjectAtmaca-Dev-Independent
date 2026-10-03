using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.AtmacaCards.ManageMeasurements;

public sealed class RecordAtmacaCardMeasurementCommandHandler(
    IActorAuthorizationService authorizationService,
    IAtmacaCardRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<AtmacaCardMeasurementDetails>> Handle(
        RecordAtmacaCardMeasurementCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Persons.RecordAtmacaCardMeasurement,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<AtmacaCardMeasurementDetails>.Failure(authorization.Error!);

        if (command.AtmacaCardId.Value == Guid.Empty)
            return Result<AtmacaCardMeasurementDetails>.Failure(
                AtmacaCardApplicationErrors.InvalidId);

        if (command.MeasuredOn > DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            return Result<AtmacaCardMeasurementDetails>.Failure(
                Error.Create(
                    "ATMACA_CARD_MEASUREMENT_DATE_FUTURE",
                    "Measurement date cannot be in the future."));

        var card = await repository.GetByIdAsync(command.AtmacaCardId, cancellationToken);
        if (card is null)
            return Result<AtmacaCardMeasurementDetails>.Failure(
                AtmacaCardApplicationErrors.NotFound);

        var result = card.AddMeasurement(
            command.MeasuredOn,
            command.HeightCentimeters,
            command.WeightKilograms);
        if (result.IsFailure)
            return Result<AtmacaCardMeasurementDetails>.Failure(result.Error!);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var measurement = result.Value!;
        return Result<AtmacaCardMeasurementDetails>.Success(
            new AtmacaCardMeasurementDetails(
                measurement.Id,
                measurement.MeasuredOn,
                measurement.HeightCentimeters,
                measurement.WeightKilograms));
    }
}
