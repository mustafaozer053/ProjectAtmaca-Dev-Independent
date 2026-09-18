using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectAtmaca.Api.Tests.Decisions;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Persons;
using ProjectAtmaca.Domain.Persons.Registration;
using ProjectAtmaca.Domain.Services;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Persons;
using ProjectAtmaca.Infrastructure.Persistence.Security;

namespace ProjectAtmaca.Api.Tests.Persons;

public sealed class RegisterPersonEndpointProductionCompositionTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Post_Should_CommitAndReplayThroughProductionSql_WithIdentityAndDuplicateRules(int status)
    {
        var token = TestContext.Current.CancellationToken;
        string databaseName = $"ProjectAtmaca_Api_Registration_{Guid.NewGuid():N}";
        string connectionString = $"Server=(localdb)\\mssqllocaldb;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True";
        var actor = ActorId.New();
        using var root = new ProjectAtmacaApiFactory();
        using var factory = root.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.ReplaceProjectAtmacaDatabase(connectionString);
            DecisionHistoryTestAuthentication.AddTo(s);
        }));
        try
        {
            await using (var setup = factory.Services.CreateAsyncScope())
            {
                var context = setup.ServiceProvider.GetRequiredService<ProjectAtmacaDbContext>();
                context.Database.GetDbConnection().Database.Should().Be(databaseName);
                context.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.SqlServer");
                setup.ServiceProvider.GetRequiredService<IPersonRegistrationStore>().Should().BeOfType<SqlPersonRegistrationStore>();
                setup.ServiceProvider.GetRequiredService<IAtmacaCardNumberGenerator>().Should().BeOfType<SqlAtmacaCardNumberGenerator>();
                await context.Database.MigrateAsync(token);
            }
            await using (var seed = new ProjectAtmacaDbContext(new DbContextOptionsBuilder<ProjectAtmacaDbContext>().UseSqlServer(connectionString).Options))
            {
                seed.Set<ActorIdentityMapping>().Add(ActorIdentityMapping.Create(
                    ExternalIdentity.Create(ProjectAtmacaApiFactory.AuthenticationAuthority, DecisionHistoryTestAuthentication.Subject), actor));
                await seed.SaveChangesAsync(token);
            }
            using var client = factory.CreateClient();
            var request = RegisterPersonEndpointAuthorizationTests.ValidRequest();
            request["status"] = status;
            if (status == 3) { request.Remove("nationalIdentityNumber"); request["passportNumber"] = "TEST-PASSPORT"; }
            if (status == 2) request["turkishCitizenshipAcquiredOn"] = "2020-02-03";
            request["citizenships"] = JsonNode.Parse("""[{"country":{"code":"DE","name":"Germany"}},{"country":{"code":"FR","name":"France"},"acquiredOn":"2015-03-04"}]""");
            request["motherName"] = "Test Mother"; request["fatherName"] = "Test Father";
            request["email"] = "test@example.invalid"; request["bloodType"] = 1;
            request["primaryPhoneNumber"] = JsonNode.Parse("""{"countryCode":"90","nationalNumber":"5551234567"}""");
            request["secondaryPhoneNumber"] = JsonNode.Parse("""{"countryCode":"49","nationalNumber":"1234567890"}""");
            request["birthPlace"] = JsonNode.Parse("""{"country":{"code":"TR","name":"Türkiye"},"city":"Rize","district":"Merkez"}""");
            request["address"] = JsonNode.Parse("""{"location":{"country":{"code":"TR","name":"Türkiye"},"city":"Rize"},"addressText":"Test address","postalCode":"53000"}""");
            // Client-supplied server fields must not override actor or generated identifiers.
            request["actorId"] = Guid.NewGuid().ToString(); request["personId"] = Guid.Empty.ToString();
            request["cardNumber"] = "ATM-999999";
            using (var denied = await client.PostAsJsonAsync("/api/person-registrations", request, token))
                await RegisterPersonEndpointAuthorizationTests.AssertProblem(denied, 403, ActorAuthorizationErrors.Forbidden.Code);
            await using (var seed = new ProjectAtmacaDbContext(new DbContextOptionsBuilder<ProjectAtmacaDbContext>().UseSqlServer(connectionString).Options))
            {
                (await seed.Set<Person>().CountAsync(token)).Should().Be(0);
                seed.Set<ActorPermissionGrant>().Add(ActorPermissionGrant.Create(actor, Permissions.Persons.RegisterWithAtmacaCard));
                await seed.SaveChangesAsync(token);
            }
            var incomplete = request.DeepClone().AsObject();
            string requiredCode;
            if (status == 2)
            {
                incomplete.Remove("turkishCitizenshipAcquiredOn");
                requiredCode = RegistrationIdentityErrors.AcquisitionDateRequired.Code;
            }
            else
            {
                incomplete.Remove(status == 3 ? "passportNumber" : "nationalIdentityNumber");
                requiredCode = status == 3 ? RegistrationIdentityErrors.PassportNumberRequired.Code
                    : RegistrationIdentityErrors.NationalIdentityNumberRequired.Code;
            }
            using (var invalid = await client.PostAsJsonAsync("/api/person-registrations", incomplete, token))
                await RegisterPersonEndpointAuthorizationTests.AssertProblem(invalid, 400, requiredCode);
            using var first = await client.PostAsJsonAsync("/api/person-registrations", request, token);
            first.StatusCode.Should().Be(HttpStatusCode.OK, await first.Content.ReadAsStringAsync(token));
            first.Headers.Location.Should().BeNull();
            var receipt = (await first.Content.ReadFromJsonAsync<RegistrationReceipt>(token))!;
            receipt.PersonId.Should().NotBeEmpty(); receipt.AtmacaCardId.Should().NotBeEmpty();
            receipt.CardNumber.Should().Be("ATM-000001"); receipt.IssuedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
            var responseJson = JsonNode.Parse(await first.Content.ReadAsStringAsync(token))!.AsObject();
            responseJson.Select(x => x.Key).Should().BeEquivalentTo("personId", "atmacaCardId", "cardNumber", "issuedAtUtc");
            using var replay = await client.PostAsJsonAsync("/api/person-registrations", request, token);
            replay.StatusCode.Should().Be(HttpStatusCode.OK);
            (await replay.Content.ReadFromJsonAsync<RegistrationReceipt>(token)).Should().Be(receipt);
            await using (var verification = factory.Services.CreateAsyncScope())
            {
                var context = verification.ServiceProvider.GetRequiredService<ProjectAtmacaDbContext>();
                var person = await context.Set<Person>().SingleAsync(token);
                person.Id.Should().Be(receipt.PersonId); person.CreatedByActorId.Should().Be(actor);
                person.Name.FullName.Should().Be("Test Person");
                person.BirthDate.Value.Should().Be(new DateTime(2010, 1, 2));
                person.BirthCountry.Code.Should().Be("TR");
                person.MotherName!.FullName.Should().Be("Test Mother"); person.FatherName!.FullName.Should().Be("Test Father");
                person.BirthPlace!.District.Should().Be("Merkez");
                person.Email!.Value.Should().Be("test@example.invalid");
                person.PrimaryPhoneNumber!.FullNumber.Should().Be("+905551234567");
                person.SecondaryPhoneNumber!.FullNumber.Should().Be("+491234567890");
                ((int)person.BloodType).Should().Be(1);
                person.Address!.AddressText.Should().Be("Test address"); person.Address.PostalCode.Should().Be("53000");
                (await context.Set<AtmacaCard>().SingleAsync(token)).PersonId.Should().Be(person.Id);
                (await context.Set<PersonRegistration>().CountAsync(token)).Should().Be(1);
                var citizenships = await context.Set<PersonCitizenship>().ToListAsync(token);
                citizenships.Should().HaveCount(2);
                citizenships.Single(c => c.Country.Code == "DE").AcquiredOn.Should().BeNull();
                citizenships.Single(c => c.Country.Code == "FR").AcquiredOn.Should().Be(new DateOnly(2015, 3, 4));
                var completed = await verification.ServiceProvider.GetRequiredService<IPersonRegistrationStore>()
                    .FindAsync(actor, Guid.Parse(request["operationId"]!.GetValue<string>()), token);
                ((int)completed!.Input.Status).Should().Be(status);
                completed.Input.TurkishCitizenshipAcquiredOn.Should().Be(status == 2 ? new DateOnly(2020, 2, 3) : null);
            }
            var changed = request.DeepClone().AsObject(); changed["fullName"] = "Changed Name";
            using (var conflict = await client.PostAsJsonAsync("/api/person-registrations", changed, token))
                await RegisterPersonEndpointAuthorizationTests.AssertProblem(conflict, 409, PersonRegistrationOperationErrors.OperationConflict.Code);
            changed["operationId"] = Guid.NewGuid().ToString("D");
            using (var duplicate = await client.PostAsJsonAsync("/api/person-registrations", changed, token))
                await RegisterPersonEndpointAuthorizationTests.AssertProblem(duplicate, 409, status == 3
                    ? PersonRegistrationOperationErrors.PassportDuplicateConfirmationRequired.Code
                    : PersonRegistrationOperationErrors.IdentityAlreadyRegistered.Code);
            if (status == 3)
            {
                changed["confirmPossiblePassportDuplicate"] = true;
                using (var missingReason = await client.PostAsJsonAsync("/api/person-registrations", changed, token))
                    await RegisterPersonEndpointAuthorizationTests.AssertProblem(missingReason, 400, PersonRegistrationOperationErrors.PassportDuplicateReasonRequired.Code);
                changed["passportDuplicateReason"] = "  Different person verified by registration staff  ";
                using var confirmed = await client.PostAsJsonAsync("/api/person-registrations", changed, token);
                confirmed.StatusCode.Should().Be(HttpStatusCode.OK, await confirmed.Content.ReadAsStringAsync(token));
                var secondReceipt = (await confirmed.Content.ReadFromJsonAsync<RegistrationReceipt>(token))!;
                secondReceipt.PersonId.Should().NotBe(receipt.PersonId);
                await using var check = factory.Services.CreateAsyncScope();
                var stored = await check.ServiceProvider.GetRequiredService<IPersonRegistrationStore>()
                    .FindAsync(actor, Guid.Parse(changed["operationId"]!.GetValue<string>()), token);
                stored!.Input.PassportDuplicateReason.Should().Be("Different person verified by registration staff");
                stored.Input.ConfirmPossiblePassportDuplicate.Should().BeTrue();
            }
            await using var finalCheck = factory.Services.CreateAsyncScope();
            var finalContext = finalCheck.ServiceProvider.GetRequiredService<ProjectAtmacaDbContext>();
            (await finalContext.Set<Person>().CountAsync(token)).Should().Be(status == 3 ? 2 : 1);
            (await finalContext.Set<AtmacaCard>().CountAsync(token)).Should().Be(status == 3 ? 2 : 1);
            if (status == 1)
            {
                // Exhaust only this isolated test database's sequence, then exercise its HTTP error mapping.
                await finalContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE [dbo].[AtmacaCardNumbers] RESTART WITH 999999", token);
                (await finalCheck.ServiceProvider.GetRequiredService<IAtmacaCardNumberGenerator>().GenerateAsync(token))
                    .Should().Be("ATM-999999");
                using var exhausted = await client.PostAsJsonAsync("/api/person-registrations", changed, token);
                await RegisterPersonEndpointAuthorizationTests.AssertProblem(exhausted, 409, PersonRegistrationOperationErrors.CardNumberCapacity.Code);
                (await finalContext.Set<Person>().CountAsync(token)).Should().Be(1);
            }
        }
        finally
        {
            await using var cleanup = factory.Services.CreateAsyncScope();
            var context = cleanup.ServiceProvider.GetRequiredService<ProjectAtmacaDbContext>();
            context.Database.GetDbConnection().Database.Should().Be(databaseName);
            await context.Database.EnsureDeletedAsync(CancellationToken.None);
        }
    }
}
