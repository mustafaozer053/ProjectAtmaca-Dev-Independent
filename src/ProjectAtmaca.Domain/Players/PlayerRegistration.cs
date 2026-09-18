using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Players;

public sealed class PlayerRegistration : AuditableAggregateRoot
{
    public Guid PersonId { get; private set; }

    public Guid ClubId { get; private set; }

    public Guid AcademyId { get; private set; }

    public Guid? TeamId { get; private set; }

    public DateTime RegisteredAtUtc { get; private set; }

    public DateTime StartDateUtc { get; private set; }

    public DateTime? EndDateUtc { get; private set; }

    public PlayerRegistrationStatus Status { get; private set; }

    public string? Notes { get; private set; }

    private PlayerRegistration(
        Guid id,
        Guid personId,
        Guid clubId,
        Guid academyId,
        Guid? teamId,
        DateTime registeredAtUtc,
        DateTime startDateUtc,
        string? notes)
        : base(id)
    {
        PersonId = personId;
        ClubId = clubId;
        AcademyId = academyId;
        TeamId = teamId;
        RegisteredAtUtc = registeredAtUtc;
        StartDateUtc = startDateUtc;
        Status = PlayerRegistrationStatus.Active;
        Notes = NormalizeOptionalText(notes);
    }

    public static Result<PlayerRegistration> Create(
        Guid personId,
        Guid clubId,
        Guid academyId,
        Guid? teamId = null,
        DateTime? startDateUtc = null,
        DateTime? registeredAtUtc = null,
        string? notes = null)
    {
        if (personId == Guid.Empty)
        {
            return Result<PlayerRegistration>.Failure(
                Error.Create(
                    "PLAYER_REGISTRATION_PERSON_ID_REQUIRED",
                    "Person id is required."));
        }

        if (clubId == Guid.Empty)
        {
            return Result<PlayerRegistration>.Failure(
                Error.Create(
                    "PLAYER_REGISTRATION_CLUB_ID_REQUIRED",
                    "Club id is required."));
        }

        if (academyId == Guid.Empty)
        {
            return Result<PlayerRegistration>.Failure(
                Error.Create(
                    "PLAYER_REGISTRATION_ACADEMY_ID_REQUIRED",
                    "Academy id is required."));
        }

        var finalStartDateUtc = startDateUtc ?? DateTime.UtcNow;
        var finalRegisteredAtUtc = registeredAtUtc ?? DateTime.UtcNow;

        if (finalRegisteredAtUtc < finalStartDateUtc)
        {
            return Result<PlayerRegistration>.Failure(
                Error.Create(
                    "PLAYER_REGISTRATION_DATE_ORDER_INVALID",
                    "Registration date cannot be earlier than the start date."));
        }

        return Result<PlayerRegistration>.Success(
            new PlayerRegistration(
                Guid.NewGuid(),
                personId,
                clubId,
                academyId,
                teamId,
                finalRegisteredAtUtc,
                finalStartDateUtc,
                notes));
    }

    public Result Close(
        DateTime endDateUtc,
        PlayerRegistrationStatus status)
    {
        if (Status != PlayerRegistrationStatus.Active)
        {
            return Result.Failure(
                Error.Create(
                    "PLAYER_REGISTRATION_NOT_ACTIVE",
                    "Only an active player registration can be closed."));
        }

        if (endDateUtc < StartDateUtc)
        {
            return Result.Failure(
                Error.Create(
                    "PLAYER_REGISTRATION_END_DATE_INVALID",
                    "Registration end date cannot be earlier than the start date."));
        }

        if (status == PlayerRegistrationStatus.Active)
        {
            return Result.Failure(
                Error.Create(
                    "PLAYER_REGISTRATION_STATUS_INVALID",
                    "A closed registration cannot remain active."));
        }

        EndDateUtc = endDateUtc;
        Status = status;

        return Result.Success();
    }

    public void ChangeTeam(Guid? teamId)
    {
        TeamId = teamId;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
