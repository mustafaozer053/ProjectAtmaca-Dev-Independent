using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;

public sealed record CitizenshipInput(Country Country, DateOnly? AcquiredOn = null);
