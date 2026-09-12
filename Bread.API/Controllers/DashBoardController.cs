using Bread.API.Application.Contracts;
using Bread.API.Application.Dtos.DashBoardDto;
using Bread.API.AuthorizationFilters;
using Bread.API.Common.Constants;
using Bread.API.Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Bread.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequireActiveUser]
[ValidateSecurityStamp]
[Authorize]
[EnableRateLimiting(RateLimitingConstants.PerUserPolicy)]
public class DashBoardController(IDashBoardServices dashBoardServices) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<GetDashBoardDto>> GetDashBoard()
    {
        Result<GetDashBoardDto> result = await dashBoardServices.GetDashBoardAsync();

        return ToActionResult(result);
    }
}
