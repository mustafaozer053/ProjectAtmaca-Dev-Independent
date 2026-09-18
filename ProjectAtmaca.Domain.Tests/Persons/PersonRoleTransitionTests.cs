using FluentAssertions;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons;

namespace ProjectAtmaca.Domain.Tests.Persons;

public sealed class PersonRoleTransitionTests
{
    [Fact]
    public void AddRole_Should_AllowSingleActiveRolePerType()
    {
        var person = Person.Create(
            PersonName.Create("Ali Yılmaz").Value!,
            BirthDate.Create(new DateTime(2005, 1, 10)),
            Country.Create("TR", "Türkiye")).Value!;

        var prospect = PersonRole.Create(
            person.Id,
            PersonRoleType.Prospect,
            new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Value!;

        var secondProspect = PersonRole.Create(
            person.Id,
            PersonRoleType.Prospect,
            new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Value!;

        person.AddRole(prospect).IsSuccess.Should().BeTrue();
        person.AddRole(secondProspect).IsSuccess.Should().BeFalse();
        person.Roles.Should().ContainSingle();
    }

    [Fact]
    public void TransitionRole_Should_RecordHistoryAndActivateNextRole()
    {
        var person = Person.Create(
            PersonName.Create("Ela Demir").Value!,
            BirthDate.Create(new DateTime(2008, 6, 12)),
            Country.Create("TR", "Türkiye")).Value!;

        var prospect = PersonRole.Create(
            person.Id,
            PersonRoleType.Prospect,
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            notes: "Takip başladı").Value!;

        person.AddRole(prospect).IsSuccess.Should().BeTrue();

        var transition = person.TransitionRole(
            PersonRoleType.Prospect,
            PersonRoleType.Player,
            new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            "Kulübe katılım");

        transition.IsSuccess.Should().BeTrue();
        person.CurrentRole!.RoleType.Should().Be(PersonRoleType.Player);
        person.CareerTransitions.Should().ContainSingle();
        person.CareerTransitions.First().FromRole.Should().Be(PersonRoleType.Prospect);
        person.CareerTransitions.First().ToRole.Should().Be(PersonRoleType.Player);
    }

    [Fact]
    public void TransitionRole_Should_RejectSameRoleChange()
    {
        var person = Person.Create(
            PersonName.Create("Mehmet Korkmaz").Value!,
            BirthDate.Create(new DateTime(2010, 3, 5)),
            Country.Create("TR", "Türkiye")).Value!;

        var prospect = PersonRole.Create(
            person.Id,
            PersonRoleType.Prospect,
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Value!;

        person.AddRole(prospect);

        var transition = person.TransitionRole(
            PersonRoleType.Prospect,
            PersonRoleType.Prospect,
            new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        transition.IsFailure.Should().BeTrue();
        transition.Error!.Code.Should().Be("PERSON_ROLE_TRANSITION_SAME_ROLE");
    }
}
