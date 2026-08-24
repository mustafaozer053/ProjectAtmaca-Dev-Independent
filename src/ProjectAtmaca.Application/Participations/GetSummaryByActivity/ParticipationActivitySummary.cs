namespace ProjectAtmaca.Application
    .Participations.GetSummaryByActivity;

public sealed record ParticipationActivitySummary(
    int Total,
    int NotRecorded,
    int Present,
    int Absent);
