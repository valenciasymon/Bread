using Bread.API.Common.Constants;
using Bread.API.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Bread.API.AuthorizationFilters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireActiveUserAttribute : TypeFilterAttribute
{
    public RequireActiveUserAttribute() : base(typeof(RequireActiveUserFilter))
    {

    }
}

public class RequireActiveUserFilter(AuthDbContext dbcontext) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var httpUser = context.HttpContext.User;

        if (httpUser?.Identity?.IsAuthenticated == false)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        string userId = httpUser?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? httpUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userId))
        {
            context.Result = new ForbidResult();
            return;
        }

        if (httpUser!.IsInRole(RolesConstants.Admin) || httpUser.IsInRole(RolesConstants.User))
        {
            ApplicationUser? user = await dbcontext.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            if (!user.IsActive)
            {
                context.Result = new ForbidResult();
                return;
            }
            else
            {
                return;
            }
        }
        else
        {
            context.Result = new ForbidResult();
            return;
        }

    }
}