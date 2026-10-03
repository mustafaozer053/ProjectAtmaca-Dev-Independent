using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.AtmacaCards;

public static class AtmacaCardApplicationErrors
{
    public static readonly Error InvalidId =
        Error.Create("AtmacaCard.InvalidId", "AtmacaCard id is required.");

    public static readonly Error NotFound =
        Error.Create("AtmacaCard.NotFound", "AtmacaCard was not found.");
}
