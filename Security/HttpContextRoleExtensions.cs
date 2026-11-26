using Microsoft.AspNetCore.Http;

namespace AnimalDatabase.Api.Security;

public static class HttpContextRoleExtensions
{
    public static Role? GetViewerRole(this HttpContext context)
    {
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        var providedKey = context.Request.Headers[AdminKey.HeaderName].ToString();
        return RoleResolver.Resolve(configuration, providedKey);
    }
}