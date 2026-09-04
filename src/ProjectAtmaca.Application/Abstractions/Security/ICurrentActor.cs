using ProjectAtmaca.Domain.Actors;

namespace ProjectAtmaca.Application.Abstractions.Security;

public interface ICurrentActor
{
    ActorId ActorId { get; }
}