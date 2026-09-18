using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;

public interface IPersonRegistrationStore
{
    Task<CompletedPersonRegistration?> FindAsync(ActorId actorId, Guid operationId, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically commits the records and operation result, keyed by actor and operation.
    /// Must stamp canonical audit with actorId. Concurrent exact replay returns the stored
    /// receipt without inserting the losing draft; different input returns OperationConflict.
    /// On failure none of the draft may remain committed. Success means commit completed.
    /// </summary>
    Task<Result<RegistrationReceipt>> CommitAsync(ActorId actorId, Guid operationId,
        PersonRegistrationInput input, PreparedPersonRegistration draft, CancellationToken cancellationToken);
}
