namespace ProjectAtmaca.Application.Organizations.Assignments;

public sealed record OrganizationDutyAssignmentDetails(
    Guid Id,
    Guid AtmacaCardId,
    Guid OrganizationId,
    string Title,
    DateTime StartDate,
    DateTime? EndDate);
