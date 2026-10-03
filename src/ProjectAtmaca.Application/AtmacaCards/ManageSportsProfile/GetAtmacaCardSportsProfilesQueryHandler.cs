using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.AtmacaCards.ManageSportsProfile;

public sealed class GetAtmacaCardSportsProfilesQueryHandler(
    IActorAuthorizationService authorizationService,
    IAtmacaCardRepository repository)
{
    public async Task<Result<IReadOnlyList<AtmacaCardSportsProfileDetails>>> Handle(
        AtmacaCardId atmacaCardId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Persons.ReadAthleteSportsProfiles,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<IReadOnlyList<AtmacaCardSportsProfileDetails>>.Failure(
                authorization.Error!);

        if (atmacaCardId.Value == Guid.Empty)
            return Result<IReadOnlyList<AtmacaCardSportsProfileDetails>>.Failure(
                AtmacaCardApplicationErrors.InvalidId);

        var card = await repository.GetByIdAsync(
            atmacaCardId,
            cancellationToken);
        if (card is null)
            return Result<IReadOnlyList<AtmacaCardSportsProfileDetails>>.Failure(
                AtmacaCardApplicationErrors.NotFound);

        IReadOnlyList<AtmacaCardSportsProfileDetails> profiles = card.SportsProfiles
            .OrderBy(x => x.SportName)
            .Select(x => new AtmacaCardSportsProfileDetails(
                x.Id,
                x.SportName,
                x.LicenseNumber?.Value,
                x.StartedSportOn,
                x.ClubRegisteredOn,
                x.CompetitionLevel,
                x.IsNationalAthlete,
                x.Positions
                    .OrderBy(position => position.Name)
                    .Select(position => new AtmacaCardPositionDetails(
                        position.Id,
                        position.Code,
                        position.Name))
                    .ToList()))
            .ToList();
        return Result<IReadOnlyList<AtmacaCardSportsProfileDetails>>.Success(profiles);
    }
}
