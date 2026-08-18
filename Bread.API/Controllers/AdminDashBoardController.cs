using Bread.API.Application.Contracts;
using Bread.API.Application.Dtos.AdminDto;
using Bread.API.Application.Dtos.DashBoardDto;
using Bread.API.Common.Constants;
using Bread.API.Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bread.API.Controllers;


[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesConstants.Admin)]
public class AdminDashBoardController(IAdminDashBoardServices adminServices) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<GetAdminDashBoardDto>> GetAdminDashBoard()
    {
        Result<GetAdminDashBoardDto> result = await adminServices.GetDashBoardAsync();

        return ToActionResult(result);
    }

    [HttpGet]
    public async Task<ActionResult<GetUsersAdminDto>> GetUser(string userId)
    {
        Result<GetUsersAdminDto> result = await adminServices.GetUserAdminAsync(userId);

        return ToActionResult(result);
    }

    [HttpGet]
    public async Task<ActionResult<GetUsersAdminDto>> GetUsers()
    {
        Result<IEnumerable<GetUsersAdminDto>> result = await adminServices.GetUsersAdminAsync();

        return ToActionResult(result);
    }
}
