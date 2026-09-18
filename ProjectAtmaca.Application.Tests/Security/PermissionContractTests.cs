using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Security;

namespace ProjectAtmaca.Application.Tests.Security;

public sealed class PermissionContractTests
{
    [Fact]
    public void Create_Should_PreserveExactCanonicalCode()
    {
        const string code =
            "Participations.Create";

        Permission permission =
            Permission.Create(
                code);

        permission.Code.Should().Be(
            code);
    }

    [Fact]
    public void Create_Should_RejectNullCode()
    {
        Action action =
            () => Permission.Create(
                null!);

        action.Should()
            .Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Create_Should_RejectEmptyOrWhitespaceCode(
        string code)
    {
        Action action =
            () => Permission.Create(
                code);

        action.Should()
            .Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(" Participations.Create")]
    [InlineData("Participations.Create ")]
    [InlineData(" Participations.Create ")]
    public void Create_Should_RejectSurroundingWhitespace(
        string code)
    {
        Action action =
            () => Permission.Create(
                code);

        action.Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_Should_BeCaseSensitive()
    {
        Permission canonical =
            Permission.Create(
                "Participations.Create");

        Permission caseVariant =
            Permission.Create(
                "participations.create");

        canonical.Should().NotBe(
            caseVariant);
    }

    [Fact]
    public void Catalog_Should_ExposeOnePermissionPerExistingUseCase()
    {
        Permission[] permissions =
        {
            Permissions.Decisions
                .ApplyParticipationClassification,

            Permissions.Decisions
                .ListApplicationHistory,

            Permissions.Participations
                .Create,

            Permissions.Participations
                .GetById,

            Permissions.Participations
                .GetSummaryByActivity,

            Permissions.Participations
                .ListByActivity,

            Permissions.Participations
                .ListHistoryByAtmacaCard,

            Permissions.Participations
                .MarkPresent,

            Permissions.Participations
                .RecordArrival,

            Permissions.Persons.RegisterWithAtmacaCard,

            Permissions.Participations
                .RecordDeparture,

            Permissions.Trainings
                .Create,

            Permissions.Trainings
                .GetById,

            Permissions.Trainings
                .Cancel
        };

        string[] expectedCodes =
        {
            "Decisions.ApplyParticipationClassification",
            "Decisions.ListApplicationHistory",
            "Participations.Create",
            "Participations.GetById",
            "Participations.GetSummaryByActivity",
            "Participations.ListByActivity",
            "Participations.ListHistoryByAtmacaCard",
            "Participations.MarkPresent",
            "Participations.RecordArrival",
            "Persons.RegisterWithAtmacaCard",
            "Participations.RecordDeparture",
            "Trainings.Create",
            "Trainings.GetById",
            "Trainings.Cancel"
        };

        permissions
            .Select(
                permission =>
                    permission.Code)
            .Should()
            .Equal(
                expectedCodes);

        permissions
            .Distinct()
            .Should()
            .HaveCount(
                permissions.Length);

        Permissions.All
            .Should()
            .Equal(
                permissions);
    }
}