using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Assignments;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Application.Organizations.Assignments;

public sealed class AddOrganizationDutyAssignmentCommandHandler(
    IActorAuthorizationService authorizationService,
    IOrganizationDutyAssignmentRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<OrganizationDutyAssignmentDetails>> Handle(
        AddOrganizationDutyAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.OrganizationAssignments.Manage,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<OrganizationDutyAssignmentDetails>.Failure(authorization.Error!);

        if (command.OrganizationId == Guid.Empty || command.AtmacaCardId == Guid.Empty)
            return Result<OrganizationDutyAssignmentDetails>.Failure(
                Error.Create("ORGANIZATION_DUTY_ID_INVALID", "Organization and card ids are required."));
        if (!await repository.OrganizationExistsAsync(command.OrganizationId, cancellationToken))
            return Result<OrganizationDutyAssignmentDetails>.Failure(
                Error.Create("ORGANIZATION_NOT_FOUND", "Organization was not found."));
        if (!await repository.IsOrganizationActiveAsync(command.OrganizationId, cancellationToken))
            return Result<OrganizationDutyAssignmentDetails>.Failure(
                Error.Create("ORGANIZATION_INACTIVE", "New duties cannot be assigned to an inactive organization."));
        if (!await repository.AtmacaCardExistsAsync(command.AtmacaCardId, cancellationToken))
            return Result<OrganizationDutyAssignmentDetails>.Failure(
                Error.Create("ATMACA_CARD_NOT_FOUND", "AtmacaCard was not found."));

        var title = AssignmentTitle.Create(command.Title);
        if (title.IsFailure)
            return Result<OrganizationDutyAssignmentDetails>.Failure(title.Error!);
        if (command.StartDate == default ||
            command.EndDate.HasValue && command.EndDate.Value.Date < command.StartDate.Date)
            return Result<OrganizationDutyAssignmentDetails>.Failure(Error.Create(
                "ORGANIZATION_DUTY_PERIOD_INVALID",
                "Duty period is invalid."));

        IReadOnlyList<OrganizationDutyAssignment> existing = await repository.ListAsync(
            command.OrganizationId,
            command.AtmacaCardId,
            cancellationToken);
        bool overlaps = existing.Any(assignment =>
            string.Equals(assignment.Title.Value, title.Value!.Value, StringComparison.OrdinalIgnoreCase) &&
            assignment.StartDate <= (command.EndDate?.Date ?? DateTime.MaxValue.Date) &&
            command.StartDate.Date <= (assignment.EndDate ?? DateTime.MaxValue.Date));
        if (overlaps)
            return Result<OrganizationDutyAssignmentDetails>.Failure(Error.Create(
                "ORGANIZATION_DUTY_DUPLICATE",
                "The same duty already exists for an overlapping period."));

        var assignment = OrganizationDutyAssignment.Create(
            command.AtmacaCardId,
            command.OrganizationId,
            title.Value!,
            command.StartDate,
            command.EndDate);
        if (assignment.IsFailure)
            return Result<OrganizationDutyAssignmentDetails>.Failure(assignment.Error!);

        await repository.AddAsync(assignment.Value!, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<OrganizationDutyAssignmentDetails>.Success(
            ListOrganizationDutyAssignmentsQueryHandler.ToDetails(assignment.Value!));
    }
}

public sealed record AddOrganizationDutyAssignmentCommand(
    Guid OrganizationId,
    Guid AtmacaCardId,
    string Title,
    DateTime StartDate,
    DateTime? EndDate);
