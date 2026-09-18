using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Persons;

public sealed class PersonRole : AuditableAggregateRoot
{
    public Guid PersonId { get; private set; }

    public PersonRoleType RoleType { get; private set; }

    public PersonRoleStatus Status { get; private set; }

    public DateTime StartDateUtc { get; private set; }

    public DateTime? EndDateUtc { get; private set; }

    public Guid? ClubId { get; private set; }

    public Guid? AcademyId { get; private set; }

    public Guid? TeamId { get; private set; }

    public string? Notes { get; private set; }

    private PersonRole(
        Guid id,
        Guid personId,
        PersonRoleType roleType,
        DateTime startDateUtc,
        Guid? clubId,
        Guid? academyId,
        Guid? teamId,
        string? notes)
        : base(id)
    {
        PersonId = personId;
        RoleType = roleType;
        Status = PersonRoleStatus.Active;
        StartDateUtc = startDateUtc;
        ClubId = clubId;
        AcademyId = academyId;
        TeamId = teamId;
        Notes = NormalizeOptionalText(notes);
    }

    public static Result<PersonRole> Create(
        Guid personId,
        PersonRoleType roleType,
        DateTime startDateUtc,
        Guid? clubId = null,
        Guid? academyId = null,
        Guid? teamId = null,
        string? notes = null)
    {
        if (personId == Guid.Empty)
        {
            return Result<PersonRole>.Failure(
                Error.Create(
                    "PERSON_ROLE_PERSON_ID_REQUIRED",
                    "Person id is required."));
        }

        if (!Enum.IsDefined(roleType))
        {
            return Result<PersonRole>.Failure(
                Error.Create(
                    "PERSON_ROLE_TYPE_INVALID",
                    "Person role type is invalid."));
        }

        if (startDateUtc == default)
        {
            return Result<PersonRole>.Failure(
                Error.Create(
                    "PERSON_ROLE_START_DATE_REQUIRED",
                    "Role start date is required."));
        }

        return Result<PersonRole>.Success(
            new PersonRole(
                Guid.NewGuid(),
                personId,
                roleType,
                startDateUtc,
                clubId,
                academyId,
                teamId,
                notes));
    }

    public Result Close(
        DateTime endDateUtc,
        PersonRoleStatus status)
    {
        if (Status != PersonRoleStatus.Active)
        {
            return Result.Failure(
                Error.Create(
                    "PERSON_ROLE_NOT_ACTIVE",
                    "Only an active role can be closed."));
        }

        if (endDateUtc < StartDateUtc)
        {
            return Result.Failure(
                Error.Create(
                    "PERSON_ROLE_END_DATE_INVALID",
                    "Role end date cannot be earlier than the start date."));
        }

        if (status == PersonRoleStatus.Active)
        {
            return Result.Failure(
                Error.Create(
                    "PERSON_ROLE_CANNOT_REMAIN_ACTIVE",
                    "A closed role cannot remain active."));
        }

        EndDateUtc = endDateUtc;
        Status = status;

        return Result.Success();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
