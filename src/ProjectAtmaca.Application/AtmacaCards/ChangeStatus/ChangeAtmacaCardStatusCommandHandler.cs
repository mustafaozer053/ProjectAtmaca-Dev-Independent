using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.AtmacaCards.ChangeStatus;

public sealed class ChangeAtmacaCardStatusCommandHandler(
    IActorAuthorizationService authorizationService,
    IAtmacaCardRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        ChangeAtmacaCardStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Persons.ChangeAtmacaCardStatus,
            cancellationToken);
        if (authorization.IsFailure)
            return authorization;

        if (command.AtmacaCardId.Value == Guid.Empty)
            return Result.Failure(AtmacaCardApplicationErrors.InvalidId);

        var card = await repository.GetByIdAsync(
            command.AtmacaCardId,
            cancellationToken);
        if (card is null)
            return Result.Failure(AtmacaCardApplicationErrors.NotFound);

        if (command.IsActive)
            card.Activate();
        else
            card.Deactivate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
