namespace ProjectAtmaca.Domain.Common.ValueObjects;

public sealed class PersonName : ValueObject
{
    public string FullName { get; }

    private PersonName(string fullName)
    {
        FullName = fullName;
    }

    public static Result<PersonName> Create(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result<PersonName>.Failure(
                Error.Create(
                    "PERSON_NAME_REQUIRED",
                    "Person name is required."));
        }

        fullName = fullName.Trim();

        if (fullName.Length < 2)
        {
            return Result<PersonName>.Failure(
                Error.Create(
                    "PERSON_NAME_TOO_SHORT",
                    "Person name is too short."));
        }

        if (fullName.Length > 150)
        {
            return Result<PersonName>.Failure(
                Error.Create(
                    "PERSON_NAME_TOO_LONG",
                    "Person name is too long."));
        }

        return Result<PersonName>.Success(
            new PersonName(fullName));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FullName;
    }

    public override string ToString()
    {
        return FullName;
    }
}
