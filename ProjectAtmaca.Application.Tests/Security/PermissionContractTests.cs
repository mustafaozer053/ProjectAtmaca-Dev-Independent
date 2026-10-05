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
                .MarkAbsent,

            Permissions.Participations
                .RecordArrival,

            Permissions.Persons.RegisterWithAtmacaCard,

            Permissions.Persons.ChangeAtmacaCardStatus,
            Permissions.Persons.ManageAthleteSportsProfile,
            Permissions.Persons.ReadAthleteSportsProfiles,
            Permissions.Persons.ReadAtmacaCardMeasurements,
            Permissions.Persons.RecordAtmacaCardMeasurement,
            Permissions.Persons.ReadAtmacaCardEducation,
            Permissions.Persons.ManageAtmacaCardEducation,
            Permissions.Persons.ReadAtmacaCardDocuments,
            Permissions.Persons.ManageAtmacaCardDocuments,
            Permissions.Persons.ReadProfessionalTitles,
            Permissions.Persons.ManageProfessionalTitles,
            Permissions.OrganizationAssignments.Read,
            Permissions.OrganizationAssignments.Manage,
            Permissions.Organizations.Manage,
            Permissions.Positions.List,
            Permissions.Positions.Create,
            Permissions.Positions.ChangeStatus,

            Permissions.Participations
                .RecordDeparture,

            Permissions.Trainings
                .Create,

            Permissions.Trainings
                .Confirm,

            Permissions.Trainings
                .Reschedule,

            Permissions.Trainings
                .GetById,

            Permissions.Trainings
                .Cancel,

            Permissions.Trainings.List,

            Permissions.TrainingTypes.Create,
            Permissions.TrainingTypes.List,
            Permissions.TrainingTypes.ChangeStatus,
            Permissions.SeasonTeams.Create,
            Permissions.SeasonTeams.List,
            Permissions.SeasonTeams.AddMembership,
            Permissions.SeasonTeams.EndMembership,
            Permissions.SeasonTeams.ChangeStatus,
            Permissions.SeasonTeams.AddMembershipAssignment,
            Permissions.SeasonTeams.EndMembershipAssignment,
            Permissions.Fixtures.Create,
            Permissions.Fixtures.List,
            Permissions.Fixtures.Update,
            Permissions.Fixtures.Cancel,
            Permissions.Scouting.Create,
            Permissions.Scouting.List,
            Permissions.Scouting.Update
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
            "Participations.MarkAbsent",
            "Participations.RecordArrival",
            "Persons.RegisterWithAtmacaCard",
            "Persons.ChangeAtmacaCardStatus",
            "Persons.ManageAthleteSportsProfile",
            "Persons.ReadAthleteSportsProfiles",
            "Persons.ReadAtmacaCardMeasurements",
            "Persons.RecordAtmacaCardMeasurement",
            "Persons.ReadAtmacaCardEducation",
            "Persons.ManageAtmacaCardEducation",
            "Persons.ReadAtmacaCardDocuments",
            "Persons.ManageAtmacaCardDocuments",
            "Persons.ReadProfessionalTitles",
            "Persons.ManageProfessionalTitles",
            "OrganizationAssignments.Read",
            "OrganizationAssignments.Manage",
            "Organizations.Manage",
            "Positions.List",
            "Positions.Create",
            "Positions.ChangeStatus",
            "Participations.RecordDeparture",
            "Trainings.Create",
            "Trainings.Confirm",
            "Trainings.Reschedule",
            "Trainings.GetById",
            "Trainings.Cancel",
            "Trainings.List",
            "TrainingTypes.Create",
            "TrainingTypes.List",
            "TrainingTypes.ChangeStatus",
            "SeasonTeams.Create",
            "SeasonTeams.List",
            "SeasonTeams.AddMembership",
            "SeasonTeams.EndMembership",
            "SeasonTeams.ChangeStatus",
            "SeasonTeams.AddMembershipAssignment",
            "SeasonTeams.EndMembershipAssignment",
            "Fixtures.Create",
            "Fixtures.List",
            "Fixtures.Update",
            "Fixtures.Cancel",
            "Scouting.Create",
            "Scouting.List",
            "Scouting.Update"
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