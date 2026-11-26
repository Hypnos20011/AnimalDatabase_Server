using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AnimalDatabase.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    public const string RoleItemKey = "AnimalDatabase.Api.Security.Role";

    private readonly Role _requiredRole;

    public RequireRoleAttribute(Role requiredRole)
    {
        _requiredRole = requiredRole;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var configuredAdminKey = configuration[AdminKey.ConfigurationKey];
        if (string.IsNullOrWhiteSpace(configuredAdminKey))
        {
            context.Result = new ObjectResult("Editing is turned off because no admin key is configured.")
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return;
        }

        var provided = context.HttpContext.Request.Headers[AdminKey.HeaderName].ToString();
        Role? role = null;
        if (AdminKey.Matches(configuredAdminKey, provided))
        {
            role = Role.Admin;
        }
        else
        {
            var configuredTesterKey = configuration[AdminKey.TesterConfigurationKey];
            if (!string.IsNullOrWhiteSpace(configuredTesterKey) && AdminKey.Matches(configuredTesterKey, provided))
            {
                role = Role.Tester;
            }
        }

        if (role is null)
        {
            context.Result = new ObjectResult("A valid admin key is required.")
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        context.HttpContext.Items[RoleItemKey] = role.Value;
        if (_requiredRole == Role.Admin && role != Role.Admin)
        {
            context.Result = new ObjectResult("Admin access is required.")
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}