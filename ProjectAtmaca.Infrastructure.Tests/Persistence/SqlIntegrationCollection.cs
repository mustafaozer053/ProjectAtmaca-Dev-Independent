using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests.Persistence;

[CollectionDefinition(Name)]
public sealed class SqlIntegrationCollection
    : ICollectionFixture<SqlIntegrationCollectionFixture>
{
    public const string Name = "SQL Integration";
}
