using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Enums;

namespace ProjectAtmaca.Domain.Entities;

public sealed class Person : BaseEntity
{
    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public DateOnly BirthDate { get; private set; }

    public Gender Gender { get; private set; }

    public Guid BirthCountryId { get; private set; }

    public static Person Create(
        string firstName,
        string lastName,
        DateOnly birthDate,
        Gender gender,
        Guid birthCountryId)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));

        if (birthDate > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Birth date cannot be in the future.", nameof(birthDate));

        if (birthCountryId == Guid.Empty)
            throw new ArgumentException("Birth country id is required.", nameof(birthCountryId));

        return new Person
        {
            Id = Guid.NewGuid(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            BirthDate = birthDate,
            Gender = gender,
            BirthCountryId = birthCountryId
        };
    }
}
