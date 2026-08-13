using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Common.ValueObjects;

public sealed class AssignmentTitle : ValueObject
{
    public string Value { get; }

    private AssignmentTitle(string value)
    {
        Value = value;
    }

    public static Result<AssignmentTitle> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<AssignmentTitle>.Failure(
                Error.Create("ASSIGNMENT_TITLE_EMPTY", "Assignment title cannot be empty."));

        value = value.Trim();

        if (value.Length < 2)
            return Result<AssignmentTitle>.Failure(
                Error.Create("ASSIGNMENT_TITLE_TOO_SHORT", "Assignment title is too short."));

        if (value.Length > 150)
            return Result<AssignmentTitle>.Failure(
                Error.Create("ASSIGNMENT_TITLE_TOO_LONG", "Assignment title is too long."));

        return Result<AssignmentTitle>.Success(new AssignmentTitle(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
