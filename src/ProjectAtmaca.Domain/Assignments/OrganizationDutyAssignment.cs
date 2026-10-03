using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Domain.Assignments;

public sealed class OrganizationDutyAssignment : AuditableAggregateRoot
{
    public Guid AtmacaCardId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public AssignmentTitle Title { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }

    private OrganizationDutyAssignment()
    {
        Title = null!;
    }

    private OrganizationDutyAssignment(
        Guid atmacaCardId,
        Guid organizationId,
        AssignmentTitle title,
        DateTime startDate,
        DateTime? endDate)
    {
        AtmacaCardId = atmacaCardId;
        OrganizationId = organizationId;
        Title = title;
        StartDate = startDate.Date;
        EndDate = endDate?.Date;
    }

    public static Result<OrganizationDutyAssignment> Create(
        Guid atmacaCardId,
        Guid organizationId,
        AssignmentTitle title,
        DateTime startDate,
        DateTime? endDate)
    {
        if (atmacaCardId == Guid.Empty)
            return Result<OrganizationDutyAssignment>.Failure(Error.Create(
                "ORGANIZATION_DUTY_CARD_REQUIRED",
                "AtmacaCard id is required."));
        if (organizationId == Guid.Empty)
            return Result<OrganizationDutyAssignment>.Failure(Error.Create(
                "ORGANIZATION_DUTY_ORGANIZATION_REQUIRED",
                "Organization id is required."));
        if (title is null)
            return Result<OrganizationDutyAssignment>.Failure(Error.Create(
                "ORGANIZATION_DUTY_TITLE_REQUIRED",
                "Duty title is required."));
        if (string.Equals(title.Value, "Sporcu", StringComparison.OrdinalIgnoreCase))
            return Result<OrganizationDutyAssignment>.Failure(Error.Create(
                "ORGANIZATION_DUTY_CLASSIFICATION_REQUIRED",
                "Sporcu must be recorded as a season-team classification, not an organization duty."));
        if (startDate == default || endDate.HasValue && endDate.Value.Date < startDate.Date)
            return Result<OrganizationDutyAssignment>.Failure(Error.Create(
                "ORGANIZATION_DUTY_PERIOD_INVALID",
                "Duty period is invalid."));

        return Result<OrganizationDutyAssignment>.Success(
            new OrganizationDutyAssignment(atmacaCardId, organizationId, title, startDate, endDate));
    }

    public Result End(DateTime endedOn)
    {
        if (EndDate.HasValue)
            return Result.Failure(Error.Create(
                "ORGANIZATION_DUTY_ALREADY_ENDED",
                "Duty assignment has already ended."));
        if (endedOn == default || endedOn.Date < StartDate)
            return Result.Failure(Error.Create(
                "ORGANIZATION_DUTY_PERIOD_INVALID",
                "End date cannot be earlier than the start date."));

        EndDate = endedOn.Date;
        return Result.Success();
    }
}
