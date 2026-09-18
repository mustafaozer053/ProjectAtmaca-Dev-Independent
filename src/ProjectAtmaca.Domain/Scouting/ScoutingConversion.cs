using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Scouting;

public sealed class ScoutingConversion : AuditableAggregateRoot
{
    public Guid ScoutingCandidateId { get; private set; }

    public Guid PersonId { get; private set; }

    public Guid ClubId { get; private set; }

    public Guid? AcademyId { get; private set; }

    public Guid? TeamId { get; private set; }

    public DateTime ConvertedAtUtc { get; private set; }

    public string? Notes { get; private set; }

    private ScoutingConversion(
        Guid id,
        Guid scoutingCandidateId,
        Guid personId,
        Guid clubId,
        Guid? academyId,
        Guid? teamId,
        DateTime convertedAtUtc,
        string? notes)
        : base(id)
    {
        ScoutingCandidateId = scoutingCandidateId;
        PersonId = personId;
        ClubId = clubId;
        AcademyId = academyId;
        TeamId = teamId;
        ConvertedAtUtc = convertedAtUtc;
        Notes = NormalizeOptionalText(notes);
    }

    public static Result<ScoutingConversion> Create(
        Guid scoutingCandidateId,
        Guid personId,
        Guid clubId,
        Guid? academyId = null,
        Guid? teamId = null,
        DateTime? convertedAtUtc = null,
        string? notes = null)
    {
        if (scoutingCandidateId == Guid.Empty)
        {
            return Result<ScoutingConversion>.Failure(
                Error.Create(
                    "SCOUTING_CONVERSION_CANDIDATE_ID_REQUIRED",
                    "Scouting candidate id is required."));
        }

        if (personId == Guid.Empty)
        {
            return Result<ScoutingConversion>.Failure(
                Error.Create(
                    "SCOUTING_CONVERSION_PERSON_ID_REQUIRED",
                    "Person id is required."));
        }

        if (clubId == Guid.Empty)
        {
            return Result<ScoutingConversion>.Failure(
                Error.Create(
                    "SCOUTING_CONVERSION_CLUB_ID_REQUIRED",
                    "Club id is required."));
        }

        var finalConvertedAtUtc = convertedAtUtc ?? DateTime.UtcNow;

        return Result<ScoutingConversion>.Success(
            new ScoutingConversion(
                Guid.NewGuid(),
                scoutingCandidateId,
                personId,
                clubId,
                academyId,
                teamId,
                finalConvertedAtUtc,
                notes));
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
