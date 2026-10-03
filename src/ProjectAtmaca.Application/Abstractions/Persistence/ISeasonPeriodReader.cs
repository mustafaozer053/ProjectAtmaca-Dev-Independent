using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Application.Abstractions.Persistence;

public interface ISeasonPeriodReader
{
    Task<DateRange?> GetPeriodAsync(
        Guid seasonId,
        CancellationToken cancellationToken = default);
}
