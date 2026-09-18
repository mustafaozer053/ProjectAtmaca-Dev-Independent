using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Domain.AtmacaCards;

public sealed class AtmacaCard : AuditableAggregateRoot
{
    public AtmacaCardId AtmacaCardId =>
        AtmacaCardId.From(Id);
    public Guid PersonId { get; private set; }

    public AtmacaCardNumber CardNumber { get; private set; }

    public DateTime IssuedAtUtc { get; private set; }

    private AtmacaCard(
        Guid personId,
        AtmacaCardNumber cardNumber,
        DateTime issuedAtUtc)
    {
        PersonId = personId;
        CardNumber = cardNumber;
        IssuedAtUtc = issuedAtUtc;
    }

    public static Result<AtmacaCard> Issue(
        Guid personId,
        AtmacaCardNumber cardNumber,
        DateTime issuedAtUtc)
    {
        if (personId == Guid.Empty)
            return Result<AtmacaCard>.Failure(Error.Create("ATMACA_CARD_PERSON_REQUIRED", "Person id is required."));

        if (cardNumber is null)
        {
            return Result<AtmacaCard>.Failure(
                Error.Create(
                    "ATMACA_CARD_NUMBER_REQUIRED",
                    "AtmacaCard number is required."));
        }

        if (issuedAtUtc == default)
        {
            return Result<AtmacaCard>.Failure(
                Error.Create("ATMACA_CARD_ISSUED_AT_REQUIRED", "Issuance time is required."));
        }

        if (issuedAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result<AtmacaCard>.Failure(
                Error.Create("ATMACA_CARD_ISSUED_AT_UTC_REQUIRED", "Issuance time must be UTC."));
        }

        var card = new AtmacaCard(
            personId,
            cardNumber,
            issuedAtUtc);

        return Result<AtmacaCard>.Success(card);
    }
}
