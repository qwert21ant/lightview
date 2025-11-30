using Microsoft.AspNetCore.Authorization;

namespace WebService.Authentication;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ApiKeyAuthorizationAttribute : AuthorizeAttribute
{
    public ApiKeyAuthorizationAttribute()
    {
        AuthenticationSchemes = ApiKeyAuthenticationSchemeOptions.DefaultScheme;
    }
}