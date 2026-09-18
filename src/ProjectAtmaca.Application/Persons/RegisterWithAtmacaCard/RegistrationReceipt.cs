namespace ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;

public sealed record RegistrationReceipt(Guid PersonId, Guid AtmacaCardId, string CardNumber, DateTime IssuedAtUtc);
