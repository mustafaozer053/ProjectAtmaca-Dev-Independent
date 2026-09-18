using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;

/// <summary>
/// Authorizes each registration attempt before invoking its work, including replay lookup.
/// The registration workflow must place all data access inside the supplied operation.
/// </summary>
public sealed class PersonRegistrationAuthorization
{
    private readonly IActorAuthorizationService _authorizationService;

    public PersonRegistrationAuthorization(IActorAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
    }

    public async Task<Result<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<Result<T>>> registration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);

        var authorization = await _authorizationService.AuthorizeAsync(
            Permissions.Persons.RegisterWithAtmacaCard, cancellationToken);

        if (authorization.IsFailure)
            return Result<T>.Failure(authorization.Error!);

        cancellationToken.ThrowIfCancellationRequested();
        return await registration(cancellationToken);
    }
}
