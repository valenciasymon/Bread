using Bread.API.Application.Dtos.UserCustomsDto;
using Bread.API.Application.Services;
using Bread.API.AuthorizationFilters;
using Bread.API.Common.Constants;
using Bread.API.Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Bread.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[ValidateSecurityStamp]
[RequireActiveUser]
[Authorize]
[EnableRateLimiting(RateLimitingConstants.FixedPolicy)]
public class AccountController(IUserServices userServices) : BaseApiController
{
    [HttpPut("name")]
    public async Task<ActionResult<Result>> UpdateName(ChangeNameDto changeNameDto)
    {
        Result result = await userServices.UpdateNameAsync(changeNameDto);

        return ToActionResult(result);
    }

    [HttpPut("Email")]
    public async Task<ActionResult<Result>> UpdateEmail(ChangeEmailDto changeEmailDto)
    {
        Result result = await userServices.UpdateEmailAsync(changeEmailDto);

        return ToActionResult(result);
    }

    [HttpPut("Password")]
    public async Task<ActionResult<Result>> UpdatePassword(ChangePasswordDto changePasswordDto)
    {
        Result result = await userServices.UpdatePasswordAsync(changePasswordDto);

        return ToActionResult(result);
    }
}
