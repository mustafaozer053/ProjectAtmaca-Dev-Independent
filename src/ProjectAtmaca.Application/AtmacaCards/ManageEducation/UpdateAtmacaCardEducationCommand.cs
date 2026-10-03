using ProjectAtmaca.Domain.AtmacaCards;

namespace ProjectAtmaca.Application.AtmacaCards.ManageEducation;

public sealed record UpdateAtmacaCardEducationCommand(
    AtmacaCardId AtmacaCardId,
    bool IsCurrentlyStudying,
    string? SchoolName,
    string? SchoolGrade,
    string? SchoolNumber);
