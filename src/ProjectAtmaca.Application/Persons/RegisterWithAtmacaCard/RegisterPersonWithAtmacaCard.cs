using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;

/// <summary>Coordinates registration; production use requires a transactional store and person matching policy.</summary>
public sealed class RegisterPersonWithAtmacaCard
{
    private readonly PersonRegistrationAuthorization _authorization;
    private readonly ICurrentActor _actor;
    private readonly IPersonRegistrationStore _store;
    private readonly PreparePersonRegistration _prepare;

    public RegisterPersonWithAtmacaCard(PersonRegistrationAuthorization authorization, ICurrentActor actor,
        IPersonRegistrationStore store, PreparePersonRegistration prepare)
    {
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
        _actor = actor ?? throw new ArgumentNullException(nameof(actor));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _prepare = prepare ?? throw new ArgumentNullException(nameof(prepare));
    }

    public Task<Result<RegistrationReceipt>> Handle(Guid operationId, PersonRegistrationInput input,
        CancellationToken cancellationToken = default) => _authorization.ExecuteAsync(async token =>
        {
            if (operationId == Guid.Empty)
                return Result<RegistrationReceipt>.Failure(PersonRegistrationOperationErrors.OperationRequired);
            ArgumentNullException.ThrowIfNull(input);
            var actorId = _actor.ActorId;
            if (actorId.Value == Guid.Empty)
                return Result<RegistrationReceipt>.Failure(ActorIdentityResolutionErrors.NotMapped);

            var normalized = CompletedPersonRegistration.Normalize(input);
            var previous = await _store.FindAsync(actorId, operationId, token);
            if (previous is not null)
                return previous.Matches(normalized)
                    ? Result<RegistrationReceipt>.Success(previous.Receipt)
                    : Result<RegistrationReceipt>.Failure(PersonRegistrationOperationErrors.OperationConflict);

            var draft = await _prepare.PrepareAuthorizedAsync(normalized, token);
            if (draft.IsFailure)
                return Result<RegistrationReceipt>.Failure(draft.Error!);
            token.ThrowIfCancellationRequested();
            return await _store.CommitAsync(actorId, operationId, normalized, draft.Value!, token);
        }, cancellationToken);
}
