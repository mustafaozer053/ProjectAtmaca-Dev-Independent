using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Common.ValueObjects;

public sealed class AssignmentPeriod : ValueObject
{
    public DateTime StartDate { get; }

    public DateTime? EndDate { get; }

    private AssignmentPeriod(
        DateTime startDate,
        DateTime? endDate)
    {
        StartDate = startDate.Date;
        EndDate = endDate?.Date;
    }

    public static Result<AssignmentPeriod> Create(
        DateTime startDate,
        DateTime? endDate = null)
    {
        startDate = startDate.Date;

        if (endDate.HasValue)
        {
            endDate = endDate.Value.Date;

            if (endDate < startDate)
            {
                return Result<AssignmentPeriod>.Failure(
                    Error.Create(
                        "ASSIGNMENT_PERIOD_INVALID",
                        "End date cannot be earlier than start date."));
            }
        }

        return Result<AssignmentPeriod>.Success(
            new AssignmentPeriod(startDate, endDate));
    }

    public bool IsActiveOn(DateTime date)
    {
        date = date.Date;

        if (date < StartDate)
            return false;

        if (EndDate is null)
            return true;

        return date <= EndDate.Value;
    }

    public bool Contains(DateTime date)
    {
        return IsActiveOn(date);
    }

    public Result<AssignmentPeriod> End(DateTime endDate)
    {
        if (EndDate.HasValue)
        {
            return Result<AssignmentPeriod>.Failure(
                Error.Create(
                    "ASSIGNMENT_ALREADY_ENDED",
                    "Assignment has already ended."));
        }

        endDate = endDate.Date;

        if (endDate < StartDate)
        {
            return Result<AssignmentPeriod>.Failure(
                Error.Create(
                    "ASSIGNMENT_PERIOD_INVALID",
                    "End date cannot be earlier than start date."));
        }

        return Result<AssignmentPeriod>.Success(
            new AssignmentPeriod(StartDate, endDate));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }

    public override string ToString()
    {
        return EndDate is null
            ? $"{StartDate:dd.MM.yyyy} - ..."
            : $"{StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}";
    }
}
