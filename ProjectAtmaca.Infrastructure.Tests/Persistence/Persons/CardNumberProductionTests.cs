using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons.Registration;
using ProjectAtmaca.Domain.Services;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Persons;
using ProjectAtmaca.Infrastructure.Persistence.Security;
using ProjectAtmaca.Infrastructure.Tests.Persistence.Participations;

namespace ProjectAtmaca.Infrastructure.Tests.Persistence.Persons;

[Collection(SqlIntegrationCollection.Name)]
public sealed class CardNumberProductionTests
{
    [Fact]
    public async Task Migration_Should_ResumeAfterExistingCards()
    {
        await using var context = ParticipationPersistenceTestContextFactory.CreateContext();
        var migrator = context.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync("20260917075758_AddPersonRegistrationPersistence");
            var person = Guid.NewGuid();
            var card = Guid.NewGuid();
            var country = "{\"Code\":\"TR\",\"Name\":\"Türkiye\"}";
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO Persons (Id, Name, BirthCountry, BirthDate, BloodType, CreatedAtUtc)
                VALUES ({person}, N'Migration Test', {country}, '20100101', 0, SYSUTCDATETIME());
                INSERT INTO AtmacaCards (Id, PersonId, CardNumber, IssuedAtUtc, CreatedAtUtc)
                VALUES ({card}, {person}, 'ATM-800000', SYSUTCDATETIME(), SYSUTCDATETIME());
                """);
            await migrator.MigrateAsync();
            (await new SqlAtmacaCardNumberGenerator(context).GenerateAsync()).Should().Be("ATM-800001");
        }
        finally { await migrator.MigrateAsync(); }
    }

    [Fact]
    public async Task ConcurrentAllocation_Should_ReturnDistinctValidNumbers()
    {
        var values = await Task.WhenAll(Enumerable.Range(0, 24).Select(async _ =>
        {
            await using var context = ParticipationPersistenceTestContextFactory.CreateContext();
            return await new SqlAtmacaCardNumberGenerator(context).GenerateAsync();
        }));
        values.Distinct().Should().HaveCount(24);
        values.Should().OnlyContain(x => AtmacaCardNumber.Create(x).IsSuccess);
    }

    [Fact]
    public async Task Rollback_Should_NotReuseAllocatedNumber()
    {
        string first;
        await using var context = ParticipationPersistenceTestContextFactory.CreateContext();
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            first = await new SqlAtmacaCardNumberGenerator(context).GenerateAsync();
            await transaction.RollbackAsync();
        }
        var second = await new SqlAtmacaCardNumberGenerator(context).GenerateAsync();
        int.Parse(second[4..]).Should().BeGreaterThan(int.Parse(first[4..]));
    }

    [Fact]
    public async Task Capacity_Should_ReturnLastNumberThenTypedFailure_WithoutCycling()
    {
        await using var context = ParticipationPersistenceTestContextFactory.CreateContext();
        var generator = new SqlAtmacaCardNumberGenerator(context);
        var previous = int.Parse((await generator.GenerateAsync())[4..]);
        try
        {
            await context.Database.ExecuteSqlRawAsync("ALTER SEQUENCE [dbo].[AtmacaCardNumbers] RESTART WITH 999999");
            (await generator.GenerateAsync()).Should().Be("ATM-999999");
            Func<Task> exhausted = async () => await generator.GenerateAsync();
            await exhausted.Should().ThrowAsync<AtmacaCardNumberCapacityException>();
        }
        finally
        {
            // Only the resettable integration database; this test never inserts the boundary number.
            await context.Database.ExecuteSqlRawAsync(
                "ALTER SEQUENCE [dbo].[AtmacaCardNumbers] RESTART WITH " + (previous + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProductionComposition_Should_EnforceSqlPermissionAndReplay(bool granted)
    {
        var actor = new Actor();
        if (granted)
        {
            await using var context = ParticipationPersistenceTestContextFactory.CreateContext();
            context.Add(ActorPermissionGrant.Create(actor.ActorId, Permissions.Persons.RegisterWithAtmacaCard));
            await context.SaveChangesAsync();
        }
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentActor>(actor);
        services.AddApplication();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:ProjectAtmacaDatabase"] = ParticipationPersistenceTestContextFactory.ConnectionString
        }).Build());
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var input = new PersonRegistrationInput(PersonName.Create("Composition Person").Value!,
            BirthDate.Create(new DateTime(2010, 1, 1)), Country.Create("TR", "Türkiye"),
            TurkishCitizenshipStatus.ByBirth, "12345678901");
        var operation = Guid.NewGuid();
        RegistrationReceipt? receipt;
        await using (var scope = provider.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IAtmacaCardNumberGenerator>().Should().BeOfType<SqlAtmacaCardNumberGenerator>();
            scope.ServiceProvider.GetRequiredService<IPersonRegistrationStore>().Should().BeOfType<SqlPersonRegistrationStore>();
            var result = await scope.ServiceProvider.GetRequiredService<RegisterPersonWithAtmacaCard>().Handle(operation, input);
            if (!granted)
            {
                result.Error.Should().BeSameAs(ActorAuthorizationErrors.Forbidden);
                result.Value.Should().BeNull();
                return;
            }
            result.IsSuccess.Should().BeTrue();
            receipt = result.Value;
        }
        await using var replayScope = provider.CreateAsyncScope();
        var replay = await replayScope.ServiceProvider.GetRequiredService<RegisterPersonWithAtmacaCard>().Handle(operation, input);
        replay.Value.Should().Be(receipt);
        var db = replayScope.ServiceProvider.GetRequiredService<ProjectAtmacaDbContext>();
        (await db.Set<PersonRegistrationOperation>().CountAsync(x => x.ActorId == actor.ActorId.Value && x.OperationId == operation)).Should().Be(1);
    }

    private sealed class Actor : ICurrentActor { public ActorId ActorId { get; } = ActorId.New(); }
}
