using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Infrastructure.Persistence;

namespace ProjectAtmaca.Infrastructure.Tests.Persistence;

public sealed class AtmacaCardSportsProfilePositionMappingTests
{
    [Fact]
    public void Model_ShouldMapProfilePositionsAsManyToManyNavigation()
    {
        var options = new DbContextOptionsBuilder<ProjectAtmacaDbContext>()
            .UseSqlServer("Server=(local);Database=ProjectAtmacaModelTest;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var dbContext = new ProjectAtmacaDbContext(options);

        var profile = dbContext.Model.FindEntityType(typeof(AtmacaCardSportsProfile));

        profile.Should().NotBeNull();
        profile!.FindSkipNavigation(nameof(AtmacaCardSportsProfile.Positions))
            .Should().NotBeNull();
    }
}
