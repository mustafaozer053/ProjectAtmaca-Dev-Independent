using Microsoft.EntityFrameworkCore;

using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Infrastructure.Persistence
    .Security;

public sealed class ActorIdentityResolver
    : IActorIdentityResolver
{
    private readonly ProjectAtmacaDbContext _dbContext;

    public ActorIdentityResolver(
        ProjectAtmacaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ActorId>> ResolveAsync(
        ExternalIdentity externalIdentity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            externalIdentity);

        int issuerByteLength =
            checked(
                externalIdentity.Issuer.Length *
                sizeof(char));

        int subjectByteLength =
            checked(
                externalIdentity.Subject.Length *
                sizeof(char));

        ActorIdentityMapping? mapping =
            await _dbContext
                .Set<ActorIdentityMapping>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.Issuer ==
                            externalIdentity.Issuer &&
                        EF.Property<int>(
                            candidate,
                            "IssuerByteLength") ==
                            issuerByteLength &&
                        candidate.Subject ==
                            externalIdentity.Subject &&
                        EF.Property<int>(
                            candidate,
                            "SubjectByteLength") ==
                            subjectByteLength,
                    cancellationToken);

        if (mapping is null)
        {
            return Result<ActorId>.Failure(
                ActorIdentityResolutionErrors.NotMapped);
        }

        return Result<ActorId>.Success(
            mapping.ActorId);
    }
}