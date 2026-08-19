using Microsoft.Extensions.DependencyInjection;
using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Application.Participations.Create;
using ProjectAtmaca.Application.Participations.MarkPresent;

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

        return services;
    }
}
