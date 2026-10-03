using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Organizations.Assignments;

public sealed class ListOrganizationDutyAssignmentsQueryHandler(
    IActorAuthorizationService authorizationService,
    IOrganizationDutyAssignmentRepository repository)
{
    public async Task<Result<IReadOnlyList<OrganizationDutyAssignmentDetails>>> Handle(
        Guid organizationId,
        Guid atmacaCardId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.OrganizationAssignments.Read,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<IReadOnlyList<OrganizationDutyAssignmentDetails>>.Failure(authorization.Error!);

        if (organizationId == Guid.Empty || atmacaCardId == Guid.Empty)
            return Result<IReadOnlyList<OrganizationDutyAssignmentDetails>>.Failure(
                Error.Create("ORGANIZATION_DUTY_ID_INVALID", "Organization and card ids are required."));
        if (!await repository.OrganizationExistsAsync(organizationId, cancellationToken))
            return Result<IReadOnlyList<OrganizationDutyAssignmentDetails>>.Failure(
                Error.Create("ORGANIZATION_NOT_FOUND", "Organization was not found."));
        if (!await repository.AtmacaCardExistsAsync(atmacaCardId, cancellationToken))
            return Result<IReadOnlyList<OrganizationDutyAssignmentDetails>>.Failure(
                Error.Create("ATMACA_CARD_NOT_FOUND", "AtmacaCard was not found."));

        IReadOnlyList<OrganizationDutyAssignmentDetails> assignments =
            (await repository.ListAsync(organizationId, atmacaCardId, cancellationToken))
            .Select(ToDetails)
            .ToList();
        return Result<IReadOnlyList<OrganizationDutyAssignmentDetails>>.Success(assignments);
    }

    internal static OrganizationDutyAssignmentDetails ToDetails(
        ProjectAtmaca.Domain.Assignments.OrganizationDutyAssignment assignment) =>
        new(
            assignment.Id,
            assignment.AtmacaCardId,
            assignment.OrganizationId,
            assignment.Title.Value,
            assignment.StartDate,
            assignment.EndDate);
}
