namespace ProjectAtmaca.Api.Participations
    .GetSummaryByActivity;

public sealed record ParticipationActivitySummaryResponse(
    int Total,
    int NotRecorded,
    int Present,
    int Absent);
