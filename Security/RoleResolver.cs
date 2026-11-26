namespace AnimalDatabase.Api.Security;

public static class RoleResolver
{
    public static Role? Resolve(IConfiguration configuration, string? providedKey)
    {
        try
        {
            var configuredAdminKey = configuration[AdminKey.ConfigurationKey];
            if (string.IsNullOrWhiteSpace(configuredAdminKey) || string.IsNullOrWhiteSpace(providedKey))
            {
                return null;
            }

            if (AdminKey.Matches(configuredAdminKey, providedKey))
            {
                return Role.Admin;
            }

            var configuredTesterKey = configuration[AdminKey.TesterConfigurationKey];
            return !string.IsNullOrWhiteSpace(configuredTesterKey) && AdminKey.Matches(configuredTesterKey, providedKey)
                ? Role.Tester
                : null;
        }
        catch
        {
            return null;
        }
    }
}