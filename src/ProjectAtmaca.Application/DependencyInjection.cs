using Microsoft.Extensions.DependencyInjection;
using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Application.Participations.Create;
using ProjectAtmaca.Application.Participations.MarkPresent;
using ProjectAtmaca.Application.Participations.RecordArrival;
using ProjectAtmaca.Application.Participations.RecordDeparture;
using ProjectAtmaca.Application.Participations.ListByActivity;
using ProjectAtmaca.Application.Participations.GetSummaryByActivity;
using ProjectAtmaca.Application.Participations.ListHistoryByAtmacaCard;
using ProjectAtmaca.Application.Decisions.ApplyParticipationClassification;
using ProjectAtmaca.Application.Decisions.ListApplicationHistory;

namespace ProjectAtmaca.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddMetrics();

        services.AddSingleton<
            IDecisionApplicationMetrics,
            DecisionApplicationMetrics>();

        services.AddScoped<
            CreateParticipationCommandHandler>();

        services.AddScoped<
            GetParticipationByIdQueryHandler>();

        services.AddScoped<
            MarkParticipationPresentCommandHandler>();

        services.AddScoped<
            RecordParticipationArrivalCommandHandler>();

        services.AddScoped<
            RecordParticipationDepartureCommandHandler>();

        services.AddScoped<
            ListParticipationsByActivityQueryHandler>();

        services.AddScoped<
            GetParticipationSummaryByActivityQueryHandler>();

        services.AddScoped<
            ListParticipationHistoryByAtmacaCardQueryHandler>();

        services.AddScoped<
            ApplyParticipationClassificationCommandHandler>();

        services.AddScoped<
            ListDecisionApplicationHistoryQueryHandler>();

        return services;
    }
}
