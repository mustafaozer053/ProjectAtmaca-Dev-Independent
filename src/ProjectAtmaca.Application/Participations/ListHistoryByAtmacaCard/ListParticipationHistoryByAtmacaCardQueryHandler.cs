using ProjectAtmaca.Application.Participations;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application
    .Participations.ListHistoryByAtmacaCard;

public sealed class ListParticipationHistoryByAtmacaCardQueryHandler
{
    public const int MinPageSize = 1;

    public const int MaxPageSize = 100;

    private readonly IParticipationReader
        _reader;

    public ListParticipationHistoryByAtmacaCardQueryHandler(
        IParticipationReader reader)
    {
        _reader =
            reader;
    }

    public async Task<Result<ParticipationHistoryPage>>
        Handle(
            ListParticipationHistoryByAtmacaCardQuery query,
            CancellationToken cancellationToken = default)
    {
        if (query.PageSize < MinPageSize ||
            query.PageSize > MaxPageSize)
        {
            return Result<ParticipationHistoryPage>
                .Failure(
                    ParticipationHistoryErrors.InvalidPageSize);
        }

        if (query.Cursor is not null &&
            query.Cursor.AtmacaCardId.Value !=
            query.AtmacaCardId.Value)
        {
            return Result<ParticipationHistoryPage>
                .Failure(
                    ParticipationHistoryErrors
                        .CursorScopeMismatch);
        }

        ParticipationHistoryPage page =
            await _reader.ListHistoryByAtmacaCardAsync(
                query.AtmacaCardId,
                query.PageSize,
                query.Cursor,
                cancellationToken);

        return Result<ParticipationHistoryPage>
            .Success(
                page);
    }
}
