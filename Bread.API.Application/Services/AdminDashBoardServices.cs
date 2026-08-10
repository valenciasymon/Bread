using Bread.API.Application.Dtos.AdminDto;
using Bread.API.Common.Constants;
using Bread.API.Common.Results;
using Bread.API.Domain;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Bread.API.Application.Services;

public class AdminDashBoardServices(
    IHttpContextAccessor httpContext,
    AuthDbContext authContext)
{
    private string userId = 
        httpContext.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? string.Empty;

    public async Task<Result<IEnumerable<GetUsersAdminDto>>> GetUsersAdminsAsync()
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<IEnumerable<GetUsersAdminDto>>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserIs is Required"));

        throw new Exception("testing");
    }
}
