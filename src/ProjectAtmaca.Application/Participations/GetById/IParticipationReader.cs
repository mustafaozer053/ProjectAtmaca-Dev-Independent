namespace ProjectAtmaca.Application
    .Participations.GetById;

public interface IParticipationReader
{
    Task<ParticipationDetails?> GetByIdAsync(
        Guid participationId,
        CancellationToken cancellationToken = default);
}
