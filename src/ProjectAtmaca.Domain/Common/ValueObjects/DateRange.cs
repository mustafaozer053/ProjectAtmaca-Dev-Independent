namespace ProjectAtmaca.Domain.Common.ValueObjects;

public sealed class DateRange : ValueObject
{
    public DateTime StartDate { get; }

    public DateTime EndDate { get; }

    public int TotalDays => (EndDate.Date - StartDate.Date).Days + 1;

    private DateRange(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate.Date;
        EndDate = endDate.Date;
    }

    public static DateRange Create(DateTime startDate, DateTime endDate)
    {
        if (endDate.Date < startDate.Date)
            throw new ArgumentException("End date cannot be earlier than start date.");

        return new DateRange(startDate, endDate);
    }

    public bool Contains(DateTime date)
    {
        var targetDate = date.Date;

        return targetDate >= StartDate && targetDate <= EndDate;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }

    public override string ToString()
    {
        return $"{StartDate:yyyy-MM-dd} - {EndDate:yyyy-MM-dd}";
    }
}
