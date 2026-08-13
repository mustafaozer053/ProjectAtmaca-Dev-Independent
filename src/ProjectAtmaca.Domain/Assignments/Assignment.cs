using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using System;

namespace ProjectAtmaca.Domain.Assignments;

public sealed class Assignment : AuditableAggregateRoot
{
    public Guid AtmacaCardId { get; private set; }

    public Guid SeasonId { get; private set; }

    public Guid OrganizationId { get; private set; }

    public AssignmentTitle Title { get; private set; }

    public AssignmentPeriod Period { get; private set; }

    public bool IsActive
    => Period.IsActiveOn(DateTime.UtcNow);

    private Assignment(
        Guid atmacaCardId,
        Guid seasonId,
        Guid organizationId,
        AssignmentTitle title,
        AssignmentPeriod period)
    {
        AtmacaCardId = atmacaCardId;
        SeasonId = seasonId;
        OrganizationId = organizationId;
        Title = title;
        Period = period;
    }

    public static Result<Assignment> Create(
        Guid atmacaCardId,
        Guid seasonId,
        Guid organizationId,
        AssignmentTitle title,
        AssignmentPeriod period)
    {
        if (atmacaCardId == Guid.Empty)
            return Result<Assignment>.Failure(Error.Create("ASSIGNMENT_ATMACA_CARD_REQUIRED", "AtmacaCard id is required."));

        if (seasonId == Guid.Empty)
            return Result<Assignment>.Failure(Error.Create("ASSIGNMENT_SEASON_REQUIRED", "Season id is required."));

        if (organizationId == Guid.Empty)
            return Result<Assignment>.Failure(Error.Create("ASSIGNMENT_ORGANIZATION_REQUIRED", "Organization id is required."));

        if (title is null)
            return Result<Assignment>.Failure(
                Error.Create(
                    "ASSIGNMENT_TITLE_REQUIRED",
                    "Assignment title is required."));

        if (period is null)
            return Result<Assignment>.Failure(Error.Create("ASSIGNMENT_PERIOD_REQUIRED", "Assignment period is required."));

        var assignment = new Assignment(
            atmacaCardId,
            seasonId,
            organizationId,
            title,
            period);

        return Result<Assignment>.Success(assignment);
    }
    public Result End(DateTime endDate)
    {
        var periodResult = Period.End(endDate);

        if (periodResult.IsFailure)
        {
            return Result.Failure(periodResult.Error!);
        }

        Period = periodResult.Value!;

        return Result.Success();
    }
}
