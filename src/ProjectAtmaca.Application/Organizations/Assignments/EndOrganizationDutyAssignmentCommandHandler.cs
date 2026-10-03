using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Organizations.Assignments;

public sealed class EndOrganizationDutyAssignmentCommandHandler(
    IActorAuthorizationService authorizationService,
    IOrganizationDutyAssignmentRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        EndOrganizationDutyAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.OrganizationAssignments.Manage,
            cancellationToken);
        if (authorization.IsFailure)
            return authorization;
        if (command.OrganizationId == Guid.Empty ||
            command.AtmacaCardId == Guid.Empty ||
            command.AssignmentId == Guid.Empty)
            return Result.Failure(Error.Create(
                "ORGANIZATION_DUTY_ID_INVALID",
                "Organization, card and assignment ids are required."));

        var assignment = await repository.GetByIdAsync(
            command.OrganizationId,
            command.AtmacaCardId,
            command.AssignmentId,
            cancellationToken);
        if (assignment is null)
            return Result.Failure(Error.Create(
                "ORGANIZATION_DUTY_ASSIGNMENT_NOT_FOUND",
                "Organization duty assignment was not found."));

        var result = assignment.End(command.EndDate);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record EndOrganizationDutyAssignmentCommand(
    Guid OrganizationId,
    Guid AtmacaCardId,
    Guid AssignmentId,
    DateTime EndDate);
