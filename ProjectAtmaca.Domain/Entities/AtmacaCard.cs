using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Enums;

namespace ProjectAtmaca.Domain.Entities;

public sealed class AtmacaCard : BaseEntity
{
    public Guid ClubId { get; private set; }

    public Guid PersonId { get; private set; }

    public string CardNumber { get; private set; } = string.Empty;

    public string? ProfilePhotoPath { get; private set; }

    public AtmacaCardStatus Status { get; private set; }

    public static AtmacaCard Create(
        Guid clubId,
        Guid personId,
        string cardNumber)
    {
        if (clubId == Guid.Empty)
            throw new ArgumentException("Club id is required.", nameof(clubId));

        if (personId == Guid.Empty)
            throw new ArgumentException("Person id is required.", nameof(personId));

        if (string.IsNullOrWhiteSpace(cardNumber))
            throw new ArgumentException("Card number is required.", nameof(cardNumber));

        return new AtmacaCard
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            PersonId = personId,
            CardNumber = cardNumber.Trim(),
            Status = AtmacaCardStatus.Active
        };
    }
}
