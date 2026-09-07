using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Abstractions.Security;

public interface IActorAuthorizationService
{
    Task<Result> AuthorizeAsync(
        Permission permission,
        CancellationToken cancellationToken = default);
}
