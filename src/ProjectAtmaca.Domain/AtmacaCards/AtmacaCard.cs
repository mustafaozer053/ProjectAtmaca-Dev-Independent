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

    public string IssuedBy { get; private set; }

    private AtmacaCard(
        Guid personId,
        AtmacaCardNumber cardNumber,
        DateTime issuedAtUtc,
        string issuedBy)
    {
        PersonId = personId;
        CardNumber = cardNumber;
        IssuedAtUtc = issuedAtUtc;
        IssuedBy = issuedBy;
    }

    public static Result<AtmacaCard> Issue(
        Guid personId,
        AtmacaCardNumber cardNumber,
        string issuedBy)
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

        if (string.IsNullOrWhiteSpace(issuedBy))
            return Result<AtmacaCard>.Failure(Error.Create("ATMACA_CARD_ISSUED_BY_REQUIRED", "Issued by is required."));

        var card = new AtmacaCard(
            personId,
            cardNumber,
            DateTime.UtcNow,
            issuedBy.Trim());

        return Result<AtmacaCard>.Success(card);
    }
}
