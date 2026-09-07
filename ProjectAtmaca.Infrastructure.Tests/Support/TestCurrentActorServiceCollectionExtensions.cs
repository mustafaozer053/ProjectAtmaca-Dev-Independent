global using ProjectAtmaca.Infrastructure.Tests.Support;

using Microsoft.Extensions.DependencyInjection;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;

namespace ProjectAtmaca.Infrastructure.Tests.Support;

internal static class
    TestCurrentActorServiceCollectionExtensions
{
    private static readonly ActorId CanonicalTestActorId =
        ActorId.From(
            Guid.Parse(
                "7a570000-0000-4000-8000-000000000001"));

    public static IServiceCollection AddTestCurrentActor(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(
            services);

        services.AddScoped<ICurrentActor>(
            _ =>
                new TestCurrentActor(
                    CanonicalTestActorId));

        return services;
    }

    private sealed class TestCurrentActor :
        ICurrentActor
    {
        public TestCurrentActor(
            ActorId actorId)
        {
            ActorId = actorId;
        }

        public ActorId ActorId { get; }
    }
}