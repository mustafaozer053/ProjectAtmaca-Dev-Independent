using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.AtmacaCards.ManageEducation;

public sealed class GetAtmacaCardEducationQueryHandler(
    IActorAuthorizationService authorizationService,
    IAtmacaCardRepository repository)
{
    public async Task<Result<AtmacaCardEducationDetails>> Handle(
        AtmacaCardId atmacaCardId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Persons.ReadAtmacaCardEducation,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<AtmacaCardEducationDetails>.Failure(authorization.Error!);

        if (atmacaCardId.Value == Guid.Empty)
            return Result<AtmacaCardEducationDetails>.Failure(AtmacaCardApplicationErrors.InvalidId);

        var card = await repository.GetByIdAsync(atmacaCardId, cancellationToken);
        if (card is null)
            return Result<AtmacaCardEducationDetails>.Failure(AtmacaCardApplicationErrors.NotFound);

        return Result<AtmacaCardEducationDetails>.Success(
            new AtmacaCardEducationDetails(
                card.IsCurrentlyStudying,
                card.SchoolName,
                card.SchoolGrade,
                card.SchoolNumber));
    }
}
