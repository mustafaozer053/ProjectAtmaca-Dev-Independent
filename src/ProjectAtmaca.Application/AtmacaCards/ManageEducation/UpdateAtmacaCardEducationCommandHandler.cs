using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.AtmacaCards.ManageEducation;

public sealed class UpdateAtmacaCardEducationCommandHandler(
    IActorAuthorizationService authorizationService,
    IAtmacaCardRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<AtmacaCardEducationDetails>> Handle(
        UpdateAtmacaCardEducationCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Persons.ManageAtmacaCardEducation,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<AtmacaCardEducationDetails>.Failure(authorization.Error!);

        if (command.AtmacaCardId.Value == Guid.Empty)
            return Result<AtmacaCardEducationDetails>.Failure(AtmacaCardApplicationErrors.InvalidId);

        var card = await repository.GetByIdAsync(command.AtmacaCardId, cancellationToken);
        if (card is null)
            return Result<AtmacaCardEducationDetails>.Failure(AtmacaCardApplicationErrors.NotFound);

        var updateResult = card.UpdateEducation(
            command.IsCurrentlyStudying,
            command.SchoolName,
            command.SchoolGrade,
            command.SchoolNumber);
        if (updateResult.IsFailure)
            return Result<AtmacaCardEducationDetails>.Failure(updateResult.Error!);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<AtmacaCardEducationDetails>.Success(
            new AtmacaCardEducationDetails(
                card.IsCurrentlyStudying,
                card.SchoolName,
                card.SchoolGrade,
                card.SchoolNumber));
    }
}
