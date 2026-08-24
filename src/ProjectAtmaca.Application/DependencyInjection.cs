using Microsoft.Extensions.DependencyInjection;
using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Application.Participations.Create;
using ProjectAtmaca.Application.Participations.MarkPresent;
using ProjectAtmaca.Application.Participations.RecordArrival;
using ProjectAtmaca.Application.Participations.RecordDeparture;
using ProjectAtmaca.Application.Participations.ListByActivity;

namespace ProjectAtmaca.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
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

        return services;
    }
}
