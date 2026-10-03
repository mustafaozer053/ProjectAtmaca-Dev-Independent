using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.AtmacaCards.ManageMeasurements;

public sealed class GetAtmacaCardMeasurementsQueryHandler(
    IActorAuthorizationService authorizationService,
    IAtmacaCardRepository repository)
{
    public async Task<Result<IReadOnlyList<AtmacaCardMeasurementDetails>>> Handle(
        AtmacaCardId atmacaCardId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Persons.ReadAtmacaCardMeasurements,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<IReadOnlyList<AtmacaCardMeasurementDetails>>.Failure(authorization.Error!);

        if (atmacaCardId.Value == Guid.Empty)
            return Result<IReadOnlyList<AtmacaCardMeasurementDetails>>.Failure(
                AtmacaCardApplicationErrors.InvalidId);

        var card = await repository.GetByIdAsync(atmacaCardId, cancellationToken);
        if (card is null)
            return Result<IReadOnlyList<AtmacaCardMeasurementDetails>>.Failure(
                AtmacaCardApplicationErrors.NotFound);

        IReadOnlyList<AtmacaCardMeasurementDetails> measurements = card.Measurements
            .OrderByDescending(x => x.MeasuredOn)
            .ThenByDescending(x => x.Id)
            .Select(x => new AtmacaCardMeasurementDetails(
                x.Id,
                x.MeasuredOn,
                x.HeightCentimeters,
                x.WeightKilograms))
            .ToList();
        return Result<IReadOnlyList<AtmacaCardMeasurementDetails>>.Success(measurements);
    }
}
