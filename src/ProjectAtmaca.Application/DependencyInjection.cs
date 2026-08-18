using Microsoft.Extensions.DependencyInjection;
using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Application.Participations.Create;

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

        return services;
    }
}
