using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Persons.Registration;
using System.Security.Cryptography;
using System.Text;

namespace ProjectAtmaca.Infrastructure.Persistence.Persons;

public sealed class SqlPersonRegistrationStore(ProjectAtmacaDbContext context) : IPersonRegistrationStore
{
    public async Task<CompletedPersonRegistration?> FindAsync(ActorId actorId, Guid operationId, CancellationToken cancellationToken)
    {
        var row = await context.Set<PersonRegistrationOperation>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.ActorId == actorId.Value && x.OperationId == operationId, cancellationToken);
        return row is null ? null : new CompletedPersonRegistration(
            RegistrationJson.Read<RegistrationData>(row.InputJson).ToDomain(),
            new RegistrationReceipt(row.PersonId, row.CardId, row.CardNumber, row.IssuedAtUtc));
    }

    public async Task<Result<RegistrationReceipt>> CommitAsync(ActorId actorId, Guid operationId,
        PersonRegistrationInput input, PreparedPersonRegistration draft, CancellationToken cancellationToken)
    {
        if (actorId.Value == Guid.Empty || operationId == Guid.Empty)
            throw new ArgumentException("Actor and operation ids are required.");
        if (context.ChangeTracker.HasChanges() || context.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("Registration requires a context without pending writes or an ambient context transaction.");

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var resource = $"PersonRegistration:{actorId.Value:N}:{operationId:N}";
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive',
                @LockOwner='Transaction', @LockTimeout=15000;
            IF @result < 0 THROW 51000, 'Registration operation lock was not acquired.', 1;
            """, cancellationToken);

        var existing = await FindAsync(actorId, operationId, cancellationToken);
        if (existing is not null)
            return existing.Matches(input)
                ? Result<RegistrationReceipt>.Success(existing.Receipt)
                : Result<RegistrationReceipt>.Failure(PersonRegistrationOperationErrors.OperationConflict);

        var nationalId = draft.Registration.Identity.NationalIdentityNumber;
        if (nationalId is not null)
        {
            // Do not expose the identity number in SQL application lock diagnostics.
            var identityResource = "PersonIdentity:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(nationalId)));
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource={identityResource}, @LockMode='Exclusive',
                    @LockOwner='Transaction', @LockTimeout=15000;
                IF @result < 0 THROW 51002, 'Registration identity lock was not acquired.', 1;
                """, cancellationToken);
            if (await context.Set<PersonRegistration>().AnyAsync(
                x => EF.Property<string>(x, "NationalIdentityNumber") == nationalId, cancellationToken))
                return Result<RegistrationReceipt>.Failure(PersonRegistrationOperationErrors.IdentityAlreadyRegistered);
        }

        var passport = draft.Registration.Identity.PassportNumber;
        // Exact normalized number only; no country or citizenship is inferred.
        var passportKey = passport is null ? null : Convert.ToHexString(SHA256.HashData(Encoding.Unicode.GetBytes(passport)));
        if (passportKey is not null)
        {
            var passportResource = "PersonPassport:" + passportKey;
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource={passportResource}, @LockMode='Exclusive',
                    @LockOwner='Transaction', @LockTimeout=15000;
                IF @result < 0 THROW 51003, 'Registration passport lock was not acquired.', 1;
                """, cancellationToken);
            if (await context.Set<PersonRegistration>().AnyAsync(
                x => EF.Property<string>(x, "PassportMatchKey") == passportKey, cancellationToken))
            {
                if (!input.ConfirmPossiblePassportDuplicate)
                    return Result<RegistrationReceipt>.Failure(PersonRegistrationOperationErrors.PassportDuplicateConfirmationRequired);
                if (string.IsNullOrWhiteSpace(input.PassportDuplicateReason))
                    return Result<RegistrationReceipt>.Failure(PersonRegistrationOperationErrors.PassportDuplicateReasonRequired);
            }
        }

        var receipt = new RegistrationReceipt(draft.Person.Id, draft.Card.Id, draft.Card.CardNumber.Value, draft.Card.IssuedAtUtc);
        var operation = new PersonRegistrationOperation
        {
            ActorId = actorId.Value, OperationId = operationId, InputJson = RegistrationJson.Write(RegistrationData.From(input)),
            PersonId = receipt.PersonId, CardId = receipt.AtmacaCardId, CardNumber = receipt.CardNumber, IssuedAtUtc = receipt.IssuedAtUtc
        };
        AuditableAggregateRoot[] entities = [draft.Person, draft.Registration, draft.Card, .. draft.Citizenships];
        foreach (var entity in entities)
        {
            if (entity.CreatedByActorId is null) entity.SetCreatedBy(actorId);
            else if (entity.CreatedByActorId != actorId) throw new InvalidOperationException("Registration audit actor mismatch.");
        }
        try
        {
            context.AddRange(entities);
            context.Entry(draft.Registration).Property("NationalIdentityNumber").CurrentValue = nationalId;
            context.Entry(draft.Registration).Property("PassportMatchKey").CurrentValue = passportKey;
            context.Add(operation);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<RegistrationReceipt>.Success(receipt);
        }
        catch
        {
            // This context had no pending writes when the operation began.
            foreach (var entity in entities) context.Entry(entity).State = EntityState.Detached;
            context.Entry(operation).State = EntityState.Detached;
            throw;
        }
    }
}
