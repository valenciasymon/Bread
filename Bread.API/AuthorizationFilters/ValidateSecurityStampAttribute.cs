using Bread.API.Common.Constants;
using Bread.API.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Bread.API.AuthorizationFilters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ValidateSecurityStampAttribute : TypeFilterAttribute
{
    public ValidateSecurityStampAttribute() : base(typeof(ValidateSecurityStampFilter))
    {
    }
}


public class ValidateSecurityStampFilter(AuthDbContext dbContext) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ClaimsPrincipal httpUser = context.HttpContext.User;

        string securityStamp = httpUser.FindFirstValue(JwtDefaults.SecurityStamp) ?? string.Empty;

        string userId = httpUser.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? httpUser.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? string.Empty;

        ApplicationUser? user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            context.Result = new ForbidResult();
            return;
        }

        if(securityStamp != user.SecurityStamp)
        {
            context.Result = new UnauthorizedResult();
            return;
        }
            
    }
}
