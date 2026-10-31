using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AnimalDatabase.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireAdminKeyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var configured = configuration[AdminKey.ConfigurationKey];
        if (string.IsNullOrWhiteSpace(configured))
        {
            context.Result = new ObjectResult("Editing is turned off because no admin key is configured.")
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return;
        }

        var provided = context.HttpContext.Request.Headers[AdminKey.HeaderName].ToString();
        if (!AdminKey.Matches(configured, provided))
            context.Result = new ObjectResult("A valid admin key is required.")
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
    }
}