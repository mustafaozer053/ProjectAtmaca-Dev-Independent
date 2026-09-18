using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Persons;

public sealed class CareerTransition : AuditableAggregateRoot
{
    public Guid PersonId { get; private set; }

    public PersonRoleType FromRole { get; private set; }

    public PersonRoleType ToRole { get; private set; }

    public DateTime TransitionDateUtc { get; private set; }

    public string? Notes { get; private set; }

    private CareerTransition(
        Guid id,
        Guid personId,
        PersonRoleType fromRole,
        PersonRoleType toRole,
        DateTime transitionDateUtc,
        string? notes)
        : base(id)
    {
        PersonId = personId;
        FromRole = fromRole;
        ToRole = toRole;
        TransitionDateUtc = transitionDateUtc;
        Notes = NormalizeOptionalText(notes);
    }

    public static Result<CareerTransition> Create(
        Guid personId,
        PersonRoleType fromRole,
        PersonRoleType toRole,
        DateTime transitionDateUtc,
        string? notes = null)
    {
        if (personId == Guid.Empty)
        {
            return Result<CareerTransition>.Failure(
                Error.Create(
                    "CAREER_TRANSITION_PERSON_ID_REQUIRED",
                    "Person id is required."));
        }

        if (!Enum.IsDefined(fromRole) || !Enum.IsDefined(toRole))
        {
            return Result<CareerTransition>.Failure(
                Error.Create(
                    "CAREER_TRANSITION_ROLE_INVALID",
                    "Role transition contains an invalid role value."));
        }

        if (fromRole == toRole)
        {
            return Result<CareerTransition>.Failure(
                Error.Create(
                    "CAREER_TRANSITION_SAME_ROLE",
                    "Transition cannot keep the same role."));
        }

        if (transitionDateUtc == default)
        {
            return Result<CareerTransition>.Failure(
                Error.Create(
                    "CAREER_TRANSITION_DATE_REQUIRED",
                    "Transition date is required."));
        }

        return Result<CareerTransition>.Success(
            new CareerTransition(
                Guid.NewGuid(),
                personId,
                fromRole,
                toRole,
                transitionDateUtc,
                notes));
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
