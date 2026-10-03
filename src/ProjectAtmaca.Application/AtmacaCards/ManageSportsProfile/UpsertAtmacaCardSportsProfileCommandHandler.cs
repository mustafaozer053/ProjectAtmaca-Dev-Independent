using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Positions;

namespace ProjectAtmaca.Application.AtmacaCards.ManageSportsProfile;

public sealed class UpsertAtmacaCardSportsProfileCommandHandler(
    IActorAuthorizationService authorizationService,
    IAtmacaCardRepository repository,
    IPositionRepository positionRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<AtmacaCardSportsProfile>> Handle(
        UpsertAtmacaCardSportsProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Persons.ManageAthleteSportsProfile,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<AtmacaCardSportsProfile>.Failure(authorization.Error!);

        if (command.AtmacaCardId.Value == Guid.Empty)
            return Result<AtmacaCardSportsProfile>.Failure(AtmacaCardApplicationErrors.InvalidId);

        var card = await repository.GetByIdAsync(
            command.AtmacaCardId,
            cancellationToken);
        if (card is null)
            return Result<AtmacaCardSportsProfile>.Failure(AtmacaCardApplicationErrors.NotFound);

        IReadOnlyCollection<Position>? positions = null;
        if (command.PositionIds is not null)
        {
            var availablePositions = await positionRepository.ListBySportAsync(
                command.SportName,
                activeOnly: false,
                cancellationToken);
            AtmacaCardSportsProfile? existingProfile = card.SportsProfiles.SingleOrDefault(
                profile => string.Equals(
                    profile.SportName,
                    command.SportName.Trim(),
                    StringComparison.OrdinalIgnoreCase));
            var existingPositionIds = existingProfile?.Positions
                .Select(position => position.Id)
                .ToHashSet() ?? [];
            positions = availablePositions
                .Where(position =>
                    command.PositionIds.Contains(position.Id) &&
                    (position.IsActive || existingPositionIds.Contains(position.Id)))
                .ToList();
            if (positions.Count != command.PositionIds.Count)
            {
                return Result<AtmacaCardSportsProfile>.Failure(
                    Error.Create(
                        "ATMACA_CARD_SPORTS_PROFILE_POSITION_INVALID",
                        "One or more selected positions are inactive or do not belong to this sport."));
            }
        }

        var result = card.UpsertSportsProfile(
            command.SportName,
            command.LicenseNumber,
            command.StartedSportOn,
            command.ClubRegisteredOn,
            command.CompetitionLevel,
            command.IsNationalAthlete,
            positions);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }
}
