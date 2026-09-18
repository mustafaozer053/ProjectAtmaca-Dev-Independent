namespace ProjectAtmaca.Application.Abstractions.Security;

public static class Permissions
{
    public static class Persons
    {
        public static readonly Permission RegisterWithAtmacaCard =
            Permission.Create("Persons.RegisterWithAtmacaCard");
    }

    public static class Decisions
    {
        public static readonly Permission
            ApplyParticipationClassification =
                Permission.Create(
                    "Decisions.ApplyParticipationClassification");

        public static readonly Permission
            ListApplicationHistory =
                Permission.Create(
                    "Decisions.ListApplicationHistory");
    }

    public static class Participations
    {
        public static readonly Permission Create =
            Permission.Create(
                "Participations.Create");

        public static readonly Permission GetById =
            Permission.Create(
                "Participations.GetById");

        public static readonly Permission
            GetSummaryByActivity =
                Permission.Create(
                    "Participations.GetSummaryByActivity");

        public static readonly Permission
            ListByActivity =
                Permission.Create(
                    "Participations.ListByActivity");

        public static readonly Permission
            ListHistoryByAtmacaCard =
                Permission.Create(
                    "Participations.ListHistoryByAtmacaCard");

        public static readonly Permission MarkPresent =
            Permission.Create(
                "Participations.MarkPresent");

        public static readonly Permission RecordArrival =
            Permission.Create(
                "Participations.RecordArrival");

        public static readonly Permission RecordDeparture =
            Permission.Create(
                "Participations.RecordDeparture");
    }

    public static class Trainings
    {
        public static readonly Permission GetById =
            Permission.Create(
                "Trainings.GetById");

        public static readonly Permission Cancel =
            Permission.Create(
                "Trainings.Cancel");
    }

    private static readonly IReadOnlyList<Permission>
        AllPermissions =
            Array.AsReadOnly(
                new[]
                {
                    Decisions
                        .ApplyParticipationClassification,

                    Decisions
                        .ListApplicationHistory,

                    Participations.Create,
                    Participations.GetById,
                    Participations.GetSummaryByActivity,
                    Participations.ListByActivity,
                    Participations.ListHistoryByAtmacaCard,
                    Participations.MarkPresent,
                    Participations.RecordArrival,
                    Persons.RegisterWithAtmacaCard,
                    Participations.RecordDeparture,
                    Trainings.GetById,
                    Trainings.Cancel
                });

    public static IReadOnlyList<Permission> All
    {
        get
        {
            return AllPermissions;
        }
    }
}