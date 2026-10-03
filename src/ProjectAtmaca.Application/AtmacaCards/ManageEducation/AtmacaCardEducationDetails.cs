namespace ProjectAtmaca.Application.AtmacaCards.ManageEducation;

public sealed record AtmacaCardEducationDetails(
    bool IsCurrentlyStudying,
    string? SchoolName,
    string? SchoolGrade,
    string? SchoolNumber);
