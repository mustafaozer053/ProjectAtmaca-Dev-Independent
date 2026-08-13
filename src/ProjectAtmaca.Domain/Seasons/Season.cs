using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Domain.Seasons;

public sealed class Season : AuditableAggregateRoot
{
    public SeasonId SeasonId =>
            SeasonId.From(Id);
    public SeasonName Name { get; private set; }

    public DateRange Period { get; private set; }

    private Season(
        SeasonName name,
        DateRange period)
    {
        Name = name;
        Period = period;
    }

    public static Result<Season> Create(
        SeasonName name,
        DateRange period)
    {
        if (name is null)
        {
            return Result<Season>.Failure(
                Error.Create(
                    "SEASON_NAME_REQUIRED",
                    "Season name is required."));
        }

        if (period is null)
        {
            return Result<Season>.Failure(
                Error.Create(
                    "SEASON_PERIOD_REQUIRED",
                    "Season period is required."));
        }

        if (period.StartDate.Year != GetStartYear(name) ||
            period.EndDate.Year != GetEndYear(name))
        {
            return Result<Season>.Failure(
                Error.Create(
                    "SEASON_NAME_PERIOD_MISMATCH",
                    "Season name and period years do not match."));
        }

        return Result<Season>.Success(
            new Season(name, period));
    }

    public SeasonStatus GetStatus(DateTime onDate)
    {
        var date = onDate.Date;

        if (date < Period.StartDate)
            return SeasonStatus.Upcoming;

        if (date > Period.EndDate)
            return SeasonStatus.Completed;

        return SeasonStatus.Current;
    }

    public bool Contains(DateTime date)
    {
        return Period.Contains(date);
    }

    public bool IsCompleted(DateTime onDate)
    {
        return GetStatus(onDate) == SeasonStatus.Completed;
    }

    private static int GetStartYear(SeasonName name)
    {
        return int.Parse(name.Value[..4]);
    }

    private static int GetEndYear(SeasonName name)
    {
        return int.Parse(name.Value[^4..]);
    }
}
