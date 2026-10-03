using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Positions;

namespace ProjectAtmaca.Application.Positions;

public sealed class PositionCatalogService(
    IActorAuthorizationService authorizationService,
    IPositionRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<IReadOnlyList<Position>>> ListAsync(
        string sportName,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Positions.List,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<IReadOnlyList<Position>>.Failure(authorization.Error!);

        if (string.IsNullOrWhiteSpace(sportName) || sportName.Trim().Length > 80)
            return Result<IReadOnlyList<Position>>.Failure(InvalidSport);

        return Result<IReadOnlyList<Position>>.Success(
            await repository.ListBySportAsync(sportName.Trim(), activeOnly, cancellationToken));
    }

    public async Task<Result<Position>> CreateAsync(
        string sportName,
        string code,
        string name,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Positions.Create,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<Position>.Failure(authorization.Error!);

        if (string.IsNullOrWhiteSpace(sportName) || sportName.Trim().Length > 80)
            return Result<Position>.Failure(InvalidSport);

        var positionResult = Position.Create(sportName, code, name);
        if (positionResult.IsFailure)
            return positionResult;

        Position position = positionResult.Value!;
        if (await repository.ExistsBySportAndCodeAsync(
                position.SportName,
                position.Code,
                cancellationToken))
        {
            return Result<Position>.Failure(
                Error.Create("POSITION_CODE_DUPLICATE", "This position code already exists for the sport."));
        }

        await repository.AddAsync(position, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Position>.Success(position);
    }

    public async Task<Result> ChangeStatusAsync(
        Guid positionId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Positions.ChangeStatus,
            cancellationToken);
        if (authorization.IsFailure)
            return authorization;

        if (positionId == Guid.Empty)
            return Result.Failure(Error.Create("POSITION_ID_INVALID", "Position id is invalid."));

        Position? position = await repository.GetByIdAsync(positionId, cancellationToken);
        if (position is null)
            return Result.Failure(Error.Create("POSITION_NOT_FOUND", "Position was not found."));

        if (isActive)
            position.Activate();
        else
            position.Deactivate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static readonly Error InvalidSport =
        Error.Create("POSITION_SPORT_INVALID", "Sport name must be between 1 and 80 characters.");
}
