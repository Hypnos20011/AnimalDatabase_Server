using AnimalDatabase.Api.Security;
using Microsoft.Extensions.Configuration;

namespace AnimalDatabase.Api.Tests;

public class RoleResolverTests
{
    [Fact]
    public void Resolves_configured_keys_and_returns_null_for_nonmatching_keys()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AdminKey.ConfigurationKey] = ApiFactory.TestKey,
                [AdminKey.TesterConfigurationKey] = ApiFactory.TesterKey
            })
            .Build();

        Assert.Equal(Role.Admin, RoleResolver.Resolve(configuration, ApiFactory.TestKey));
        Assert.Equal(Role.Tester, RoleResolver.Resolve(configuration, ApiFactory.TesterKey));
        Assert.Null(RoleResolver.Resolve(configuration, "wrong-key"));
        Assert.Null(RoleResolver.Resolve(configuration, " "));
        Assert.Null(RoleResolver.Resolve(configuration, null));

        configuration[AdminKey.TesterConfigurationKey] = " ";
        Assert.Null(RoleResolver.Resolve(configuration, ApiFactory.TesterKey));

        var missingAdminConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AdminKey.TesterConfigurationKey] = ApiFactory.TesterKey
            })
            .Build();
        Assert.Null(RoleResolver.Resolve(missingAdminConfiguration, ApiFactory.TesterKey));
    }
}